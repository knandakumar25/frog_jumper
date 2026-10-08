"""Email-gated onboarding: Clerk signup -> country + date of birth -> account.

Clerk and Django share no state today. A user who signs up through Clerk's
hosted Account Portal gets a client-side Clerk session and *nothing* on the
Django side. This module is the missing link:

    POST /api/auth/onboarding/start/          -> email a tokenised link
    GET  /api/auth/onboarding/validate-token/ -> is this link still good?
    POST /api/auth/onboarding/complete/       -> create the User, return JWTs

The Django User does not exist until the third step succeeds. The design premise
is that a Clerk session is not proof of anything -- the user must demonstrate
control of the mailbox, so the token lives only in the email body and /start/
never returns it.

NOTE ON authentication_classes
------------------------------
`authentication_classes = []` on all three views is load-bearing, not cosmetic.
`permission_classes = [AllowAny]` does NOT stop DRF from running the configured
authenticators, and JWTAuthentication raises AuthenticationFailed on a stale or
malformed Bearer token -- producing a 401 *before* the permission check is ever
reached. Every visitor whose localStorage still holds an expired accessToken
(client.js only clears it after a *failed* refresh) would be locked out of an
endpoint that is meant to be anonymous.
"""

import hashlib
import ipaddress
import logging
import re
import secrets
from datetime import timedelta

from django.conf import settings
from django.contrib.auth import get_user_model
from django.core.mail import EmailMultiAlternatives
from django.db import IntegrityError, transaction
from django.db.models import Sum
from django.utils import timezone
from rest_framework import permissions, serializers, status
from rest_framework.response import Response
from rest_framework.views import APIView
from rest_framework_simplejwt.tokens import RefreshToken

from . import clerk_client
from .countries import country_common_names
from .models import PendingRegistration
from .serializers import UserProfileSerializer

User = get_user_model()
logger = logging.getLogger(__name__)


# --------------------------------------------------------------------------
# Settings accessors
# --------------------------------------------------------------------------

def _setting(name, default):
    return getattr(settings, name, default)


# --------------------------------------------------------------------------
# Tokens
# --------------------------------------------------------------------------

def _hash_token(raw_token):
    """sha256 hex of a raw token.

    Tokens are 256 bits from secrets.token_urlsafe(32), so a plain SHA-256 is
    sufficient. The slow-KDF argument that forces hashing for a 6-digit 2FA code
    does not apply: there is nothing to brute-force. What this closes is
    concrete in this repo -- DEBUG=True renders tracebacks, so a database dump
    or an over-logged query would otherwise be a set of live account-creation
    links.
    """
    return hashlib.sha256(raw_token.encode("utf-8")).hexdigest()


def _mask_email(email):
    """a***@example.com -- enough for the user to recognise, not to leak."""
    local, _, domain = email.partition("@")
    if not domain:
        return "***"
    if len(local) <= 1:
        return f"*@{domain}"
    return f"{local[0]}***@{domain}"


def _client_ip(request):
    """The last X-Forwarded-For element, else REMOTE_ADDR.

    The *last* element is the one nginx itself appended. The first is supplied
    by the client and is trivially rotated, and port 8000 is published directly,
    so trusting it would make the per-IP cap free to bypass. Returns None rather
    than junk when the header is unusable, because the column is a
    GenericIPAddressField and would raise on save.
    """
    forwarded = request.META.get("HTTP_X_FORWARDED_FOR", "")
    candidate = ""
    if forwarded:
        parts = [part.strip() for part in forwarded.split(",") if part.strip()]
        if parts:
            candidate = parts[-1]
    if not candidate:
        candidate = request.META.get("REMOTE_ADDR") or ""
    try:
        return str(ipaddress.ip_address(candidate))
    except ValueError:
        return None


# --------------------------------------------------------------------------
# Abuse controls
#
# All database-backed: no new dependency, and -- unlike LocMemCache -- they stay
# correct across gunicorn's 3 workers.
# --------------------------------------------------------------------------

def _sends_since(cutoff, **filters):
    """Upper bound on onboarding emails sent since `cutoff`.

    Sums sent_count for every row touched in the window, so a row that sent
    twice yesterday and once now counts three. That over-counts on purpose: the
    only thing these numbers gate is a rate limit, where erring high is the safe
    direction, and it needs no extra table to stay accurate.
    """
    total = (
        PendingRegistration.objects.filter(last_sent_at__gte=cutoff, **filters)
        .aggregate(total=Sum("sent_count"))
        .get("total")
    )
    return total or 0


def _global_sends_last_hour():
    return _sends_since(timezone.now() - timedelta(hours=1))


def _ip_sends_last_hour(ip):
    return _sends_since(timezone.now() - timedelta(hours=1), request_ip=ip)


# --------------------------------------------------------------------------
# The email
# --------------------------------------------------------------------------

def send_onboarding_email(to_email, raw_token, first_name=""):
    """Send the tokenised onboarding link.

    The link is deliberately NOT locale-prefixed. The frontend catch-all route
    (LocaleRedirect) discards window.location.search, so any /<locale>/onboarding
    URL would silently drop ?token= and render a blank form with no error.
    """
    frontend_url = getattr(settings, "FRONTEND_URL", "http://localhost:5173").rstrip("/")
    link = f"{frontend_url}/onboarding?token={raw_token}"
    ttl_hours = _setting("ONBOARDING_TOKEN_TTL_HOURS", 24)
    greeting = first_name.strip() or "Operator"

    subject = "SYS_INIT: Complete your Axiom Force registration"

    text_content = (
        "=== AXIOM FORCE // REGISTRATION INCOMPLETE ===\n\n"
        f"Welcome, {greeting}.\n\n"
        "Your identity has been verified, but your operator profile is not yet "
        "on the grid. Two fields remain before your account is created:\n\n"
        "  [1] COUNTRY OF OPERATION\n"
        "  [2] DATE OF BIRTH\n\n"
        "Complete them here:\n\n"
        f"{link}\n\n"
        f"This link expires in {ttl_hours} hours and can only be used once.\n"
        "If you did not register for Frog Jumper, ignore this message -- no "
        "account will be created.\n\n"
        "----------------------------------------\n"
        "Axiom Force Terminal Admin\n"
    )

    html_content = f"""
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset="utf-8">
        <style>
            body {{
                background-color: #05070a;
                color: #e2e8f0;
                font-family: 'Courier New', Courier, monospace;
                margin: 0;
                padding: 20px;
            }}
            .terminal {{
                max-width: 600px;
                margin: 0 auto;
                background-color: #0b0e14;
                border: 2px solid #00f3ff;
                box-shadow: 0 0 20px rgba(0, 243, 255, 0.2);
                padding: 30px;
                border-radius: 4px;
            }}
            .header {{
                border-bottom: 2px solid #ff007f;
                padding-bottom: 15px;
                margin-bottom: 25px;
                text-align: center;
            }}
            .header h1 {{
                color: #00f3ff;
                font-size: 24px;
                margin: 0;
                text-transform: uppercase;
                letter-spacing: 3px;
            }}
            .greeting {{
                font-size: 18px;
                color: #ff007f;
                margin-bottom: 20px;
                font-weight: bold;
            }}
            .details {{
                background-color: #030508;
                border-left: 3px solid #ff007f;
                padding: 15px;
                margin: 20px 0;
                font-size: 14px;
                line-height: 1.6;
            }}
            .cta {{
                display: inline-block;
                margin: 10px 0 20px;
                padding: 14px 28px;
                background-color: #00f3ff;
                color: #05070a;
                font-weight: bold;
                text-transform: uppercase;
                letter-spacing: 2px;
                text-decoration: none;
                border-radius: 3px;
            }}
            .footer {{
                margin-top: 30px;
                font-size: 11px;
                color: #4a5568;
                text-align: center;
                border-top: 1px solid #1a202c;
                padding-top: 15px;
            }}
        </style>
    </head>
    <body>
        <div class="terminal">
            <div class="header">
                <h1>[ AXIOM_FORCE_MAIN_NET ]</h1>
            </div>
            <div class="greeting">
                &gt; IDENTITY VERIFIED: {greeting.upper()}
            </div>
            <p>Your credentials cleared the gate, but your operator profile is not on the grid yet. Two required fields remain before your account is created.</p>

            <div class="details">
                <strong>OUTSTANDING FIELDS:</strong><br>
                ------------------------------<br>
                [1] COUNTRY OF OPERATION<br>
                [2] DATE OF BIRTH<br>
                ------------------------------<br>
                LINK EXPIRY : {ttl_hours} HOURS<br>
                USES        : 1
            </div>

            <p>Resume registration:</p>
            <a class="cta" href="{link}">Finish Setup</a>
            <p style="font-size: 11px; color: #4a5568; word-break: break-all;">{link}</p>
            <p style="font-size: 12px; color: #718096;">If you did not register for Frog Jumper, ignore this message &mdash; no account will be created.</p>

            <div class="footer">
                AXIOM FORCE TERMINAL DIRECTIVE // CONFIDENTIAL TRANSMISSION // DO NOT REPLICATE
            </div>
        </div>
    </body>
    </html>
    """

    message = EmailMultiAlternatives(
        subject,
        text_content,
        _setting("DEFAULT_FROM_EMAIL", "Axiom Force <onboarding@resend.dev>"),
        [to_email],
    )
    message.attach_alternative(html_content, "text/html")
    message.send(fail_silently=False)


# --------------------------------------------------------------------------
# Serializers
# --------------------------------------------------------------------------

class OnboardingCompleteSerializer(serializers.Serializer):
    token = serializers.CharField()
    country = serializers.CharField(max_length=100)
    # DateField rejects malformed input natively ("2024-13-45", "not a date").
    date_of_birth = serializers.DateField()

    def validate_country(self, value):
        # Only `name.common` is accepted -- see accounts/countries.py.
        if value not in country_common_names():
            raise serializers.ValidationError("Unknown country.")
        return value

    def validate_date_of_birth(self, value):
        # localdate(), not date.today(): with TIME_ZONE configured, a user east
        # of UTC would otherwise be rejected on their own birthday.
        if value > timezone.localdate():
            raise serializers.ValidationError("Date of birth cannot be in the future.")
        if value.year < 1900:
            raise serializers.ValidationError("Date of birth is not plausible.")
        # No age gate: sanity only, by decision.
        return value


# --------------------------------------------------------------------------
# Views
# --------------------------------------------------------------------------

class OnboardingStartView(APIView):
    """Kick off onboarding: look the user up in Clerk, then email a link."""

    authentication_classes = []
    permission_classes = [permissions.AllowAny]

    def post(self, request):
        clerk_user_id = request.data.get("clerk_user_id")

        # Validated before anything else: a fabricated id must never reach the
        # network, and this is the first abuse control.
        if not clerk_client.is_valid_user_id(clerk_user_id):
            return Response(
                {"error": "invalid_clerk_user_id"},
                status=status.HTTP_400_BAD_REQUEST,
            )

        if not getattr(settings, "CLERK_SECRET_KEY", ""):
            return Response(
                {"error": "clerk_not_configured"},
                status=status.HTTP_503_SERVICE_UNAVAILABLE,
            )

        # Global cap first: it is the billing circuit breaker, and it must hold
        # even while every per-row and per-IP counter is being rotated.
        if _global_sends_last_hour() >= _setting("ONBOARDING_GLOBAL_HOURLY_LIMIT", 100):
            return Response({"error": "rate_limited"}, status=status.HTTP_429_TOO_MANY_REQUESTS)

        existing = PendingRegistration.objects.filter(clerk_user_id=clerk_user_id).first()
        if existing is not None and existing.completed_at is not None:
            return Response({"error": "already_registered"}, status=status.HTTP_409_CONFLICT)

        ip = _client_ip(request)
        if ip and _ip_sends_last_hour(ip) >= _setting("ONBOARDING_IP_HOURLY_LIMIT", 5):
            return Response({"error": "rate_limited"}, status=status.HTTP_429_TOO_MANY_REQUESTS)

        try:
            clerk_user = clerk_client.fetch_clerk_user(clerk_user_id)
        except clerk_client.ClerkUserNotFound:
            return Response({"error": "unknown_clerk_user"}, status=status.HTTP_404_NOT_FOUND)
        except clerk_client.ClerkUnavailable:
            # Almost always this machine rather than Clerk: antivirus TLS
            # interception re-signs outbound HTTPS, and a missing .local-ca
            # bundle surfaces here as CERTIFICATE_VERIFY_FAILED.
            return Response({"error": "clerk_unreachable"}, status=status.HTTP_502_BAD_GATEWAY)
        except clerk_client.ClerkError:
            return Response({"error": "clerk_error"}, status=status.HTTP_502_BAD_GATEWAY)

        if not clerk_user.email:
            return Response({"error": "clerk_email_missing"}, status=status.HTTP_403_FORBIDDEN)

        # The entire trust argument is "Clerk proved this mailbox is controlled".
        # Mailing an unverified address could deliver an account-creation token
        # to a third party. Operational dependency: email verification must stay
        # enabled in the Clerk dashboard, or every signup answers 403.
        if not clerk_user.email_verified:
            return Response({"error": "email_not_verified"}, status=status.HTTP_403_FORBIDDEN)

        # Already linked to a User, so there is nothing left to onboard.
        #
        # Deliberately NOT checked here: whether some other account already uses
        # this email. The caller has proved nothing at this point -- that is the
        # whole reason the token gate exists -- so answering "that address is
        # registered" here would be an enumeration oracle. That collision is
        # handled at /complete/, where mailbox control has been demonstrated.
        if User.objects.filter(clerk_user_id=clerk_user_id).exists():
            return Response({"error": "already_registered"}, status=status.HTTP_409_CONFLICT)

        now = timezone.now()
        cooldown = _setting("ONBOARDING_RESEND_COOLDOWN", 60)

        if existing is not None and existing.last_sent_at:
            elapsed = (now - existing.last_sent_at).total_seconds()
            if elapsed < cooldown:
                return Response(
                    {
                        "status": "already_sent",
                        "email": _mask_email(existing.email),
                        "expires_at": existing.expires_at,
                        "retry_after": int(cooldown - elapsed),
                    },
                    status=status.HTTP_200_OK,
                )

        if existing is not None and existing.sent_count >= _setting("ONBOARDING_MAX_SENDS", 5):
            return Response({"error": "too_many_sends"}, status=status.HTTP_429_TOO_MANY_REQUESTS)

        raw_token = secrets.token_urlsafe(32)
        expires_at = now + timedelta(hours=_setting("ONBOARDING_TOKEN_TTL_HOURS", 24))

        # Sent BEFORE the row is written. A failed send therefore leaves the
        # previous row -- and its still-valid link -- untouched, and the cooldown
        # and send cap only ever count deliveries rather than attempts.
        try:
            send_onboarding_email(clerk_user.email, raw_token, clerk_user.first_name)
        except Exception:
            logger.exception("Failed to send onboarding email to %s", clerk_user.email)
            return Response({"error": "email_send_failed"}, status=status.HTTP_502_BAD_GATEWAY)

        if existing is not None:
            # Rotate rather than resend: the stored value is a hash, so the
            # previous link cannot be reproduced -- and rotating kills it
            # immediately, which is the behaviour we want anyway.
            existing.email = clerk_user.email
            existing.token_hash = _hash_token(raw_token)
            existing.first_name = clerk_user.first_name
            existing.last_name = clerk_user.last_name
            existing.clerk_username = clerk_user.username
            existing.request_ip = ip
            existing.sent_count += 1
            existing.last_sent_at = now
            existing.expires_at = expires_at
            existing.save(
                update_fields=[
                    "email",
                    "token_hash",
                    "first_name",
                    "last_name",
                    "clerk_username",
                    "request_ip",
                    "sent_count",
                    "last_sent_at",
                    "expires_at",
                ]
            )
            pending = existing
        else:
            try:
                pending = PendingRegistration.objects.create(
                    clerk_user_id=clerk_user_id,
                    email=clerk_user.email,
                    token_hash=_hash_token(raw_token),
                    first_name=clerk_user.first_name,
                    last_name=clerk_user.last_name,
                    clerk_username=clerk_user.username,
                    request_ip=ip,
                    sent_count=1,
                    last_sent_at=now,
                    expires_at=expires_at,
                )
            except IntegrityError:
                # A concurrent first request for the same Clerk id won the
                # insert. Its row holds the live token; ours was never stored,
                # so the link just mailed is already dead. Report the same thing
                # the cooldown would, rather than a 500.
                return Response(
                    {
                        "status": "already_sent",
                        "email": _mask_email(clerk_user.email),
                        "retry_after": cooldown,
                    },
                    status=status.HTTP_200_OK,
                )

        # The token itself is deliberately absent from this response. Returning
        # it to the requesting browser would make the email decorative and
        # remove the proof-of-mailbox gate entirely.
        return Response(
            {
                "status": "email_sent",
                "email": _mask_email(pending.email),
                "expires_at": pending.expires_at,
            },
            status=status.HTTP_200_OK,
        )


class OnboardingValidateTokenView(APIView):
    """Report whether a token from the emailed link is still usable."""

    authentication_classes = []
    permission_classes = [permissions.AllowAny]

    def get(self, request):
        raw_token = request.query_params.get("token") or ""
        if not raw_token:
            return Response({"error": "token_required"}, status=status.HTTP_400_BAD_REQUEST)

        pending = PendingRegistration.objects.filter(
            token_hash=_hash_token(raw_token)
        ).first()
        if pending is None:
            return Response({"error": "invalid_token"}, status=status.HTTP_404_NOT_FOUND)
        if pending.completed_at is not None:
            return Response({"error": "already_completed"}, status=status.HTTP_409_CONFLICT)
        if pending.expires_at <= timezone.now():
            return Response({"error": "token_expired"}, status=status.HTTP_410_GONE)

        # Deliberately does NOT consume the token: refreshing the page or opening
        # the link twice must not destroy it. Only /complete/ consumes.
        return Response(
            {
                "status": "valid",
                "email": pending.email,
                "first_name": pending.first_name,
                "last_name": pending.last_name,
                "expires_at": pending.expires_at,
            },
            status=status.HTTP_200_OK,
        )


def _derive_username(pending):
    """A unique, validator-safe username for the new account.

    Clerk's username is nullable, so this falls back to the email local-part.
    The uniquifier mirrors the `_counter` loop already written three times in
    views.py. It has to succeed: a None or a collision inside the atomic block
    would raise, and the rollback would undo the compare-and-swap -- leaving a
    "completed" token that never created a user.
    """
    base = (pending.clerk_username or "").strip() or pending.email.split("@")[0]
    # Django's UsernameValidator allows \w . @ + -
    base = re.sub(r"[^\w.@+-]", "", base) or "operator"
    base = base[:150]

    final = base
    counter = 1
    while User.objects.filter(username=final).exists():
        suffix = f"_{counter}"
        final = f"{base[: 150 - len(suffix)]}{suffix}"
        counter += 1
    return final


class OnboardingCompleteView(APIView):
    """Consume the token, create the User, hand back a JWT pair."""

    authentication_classes = []
    permission_classes = [permissions.AllowAny]

    def post(self, request):
        serializer = OnboardingCompleteSerializer(data=request.data)
        serializer.is_valid(raise_exception=True)
        data = serializer.validated_data

        pending = PendingRegistration.objects.filter(
            token_hash=_hash_token(data["token"])
        ).first()
        if pending is None:
            return Response({"error": "invalid_token"}, status=status.HTTP_404_NOT_FOUND)
        if pending.completed_at is not None:
            return Response({"error": "already_completed"}, status=status.HTTP_409_CONFLICT)
        if pending.expires_at <= timezone.now():
            return Response({"error": "token_expired"}, status=status.HTTP_410_GONE)

        # Checked before the insert so a legacy RegisterView account holding the
        # same address surfaces as a clean 409 rather than an IntegrityError 500.
        if (
            User.objects.filter(clerk_user_id=pending.clerk_user_id).exists()
            or User.objects.filter(email=pending.email).exists()
        ):
            return Response({"error": "already_registered"}, status=status.HTTP_409_CONFLICT)

        try:
            with transaction.atomic():
                # Compare-and-swap. This single conditional UPDATE is the real
                # concurrency guard: select_for_update() is a no-op on SQLite
                # (has_select_for_update is False), so under the test database
                # row locks would silently protect nothing.
                consumed = PendingRegistration.objects.filter(
                    pk=pending.pk, completed_at__isnull=True
                ).update(completed_at=timezone.now())
                if not consumed:
                    return Response(
                        {"error": "already_completed"}, status=status.HTTP_409_CONFLICT
                    )

                # create_user() with no password calls set_unusable_password(),
                # so Clerk stays the only credential authority. RefreshToken
                # works regardless of password usability.
                user = User.objects.create_user(
                    username=_derive_username(pending),
                    email=pending.email,
                    first_name=pending.first_name,
                    last_name=pending.last_name,
                    country=data["country"],
                    date_of_birth=data["date_of_birth"],
                    clerk_user_id=pending.clerk_user_id,
                )
        except IntegrityError:
            # Two concurrent submissions of the same link, or two Clerk users
            # racing for one address.
            return Response({"error": "already_registered"}, status=status.HTTP_409_CONFLICT)

        # The same shape every other success path returns, so AuthContext needs
        # no new branch. The post_save signal sends the welcome mail.
        refresh = RefreshToken.for_user(user)
        return Response(
            {
                "refresh": str(refresh),
                "access": str(refresh.access_token),
                "user": UserProfileSerializer(user).data,
            },
            status=status.HTTP_200_OK,
        )
