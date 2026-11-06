"""Expiring browser approval requests; public IDs never authorize redemption."""
import hashlib
import secrets
import uuid
from datetime import timedelta
from functools import lru_cache
from urllib.parse import urlencode

import jwt
from django.conf import settings
from django.db import transaction
from django.utils import timezone
from rest_framework.exceptions import AuthenticationFailed
from rest_framework.permissions import AllowAny
from rest_framework.response import Response
from rest_framework.throttling import AnonRateThrottle
from rest_framework.views import APIView
from rest_framework_simplejwt.authentication import JWTAuthentication
from rest_framework_simplejwt.tokens import RefreshToken
from .models import BrowserSession, User
from .serializers import UserProfileSerializer


def secret_hash(value):
    """Hash a bounded opaque redemption secret without persisting its plaintext."""
    return hashlib.sha256(value.encode()).hexdigest()


@lru_cache(maxsize=4)
def jwks_client(url):
    """Cache trusted issuer keys; never use token-supplied key URLs."""
    return jwt.PyJWKClient(url, timeout=5)


def clerk_subject(token):
    """Verify RS256 signature, issuer, lifetime and explicitly permitted origin."""
    issuer = settings.CLERK_ISSUER
    if not issuer or not settings.CLERK_AUTHORIZED_PARTIES:
        raise AuthenticationFailed('Clerk verification is not configured')
    try:
        key = jwks_client(settings.CLERK_JWKS_URL).get_signing_key_from_jwt(token)
        claims = jwt.decode(token, key.key, algorithms=['RS256'], issuer=issuer,
                            options={'require': ['exp', 'iat', 'nbf', 'iss', 'sub', 'azp'],
                                     'verify_aud': False})
        if claims['azp'] not in settings.CLERK_AUTHORIZED_PARTIES or claims.get('sts') == 'pending':
            raise ValueError('Unapproved session')
        if not isinstance(claims['sub'], str) or not claims['sub'].startswith('user_'):
            raise ValueError('Invalid subject')
        return claims['sub']
    except (jwt.PyJWTError, ValueError, TypeError, KeyError) as exc:
        raise AuthenticationFailed('Invalid Clerk session') from exc


class BrowserThrottle(AnonRateThrottle):
    """Bound anonymous device traffic, including invalid-secret guesses."""
    rate = '90/min'


class BrowserView(APIView):
    """Device endpoints ignore unrelated browser bearer credentials."""
    authentication_classes = []
    permission_classes = [AllowAny]
    throttle_classes = [BrowserThrottle]

    def finalize_response(self, request, response, *args, **kwargs):
        """Prevent browsers and intermediary caches retaining device secrets or credentials."""
        response = super().finalize_response(request, response, *args, **kwargs)
        response['Cache-Control'] = 'no-store'
        response['Pragma'] = 'no-cache'
        return response


def live_request(data, private=False):
    """Lock a live request inside the caller's transaction; reject legacy rows."""
    try:
        sid = uuid.UUID(str(data.get('session_id', '')))
    except ValueError:
        return None
    row = BrowserSession.objects.select_for_update().filter(session_id=sid).first()
    if not row or not row.secret_hash or not row.expires_at or row.expires_at <= timezone.now():
        return None
    if row.consumed_at or row.cancelled_at:
        return None
    if private:
        secret = data.get('secret', '')
        if not isinstance(secret, str) or len(secret) > 128 or not secrets.compare_digest(row.secret_hash, secret_hash(secret)):
            return None
    return row


class BrowserStartView(BrowserView):
    """Create a fifteen-minute device request and return its private secret once."""
    def post(self, request):
        """Validate display intent/locale and construct a public-only browser URL."""
        intent = request.data.get('intent', 'login')
        locale = request.data.get('locale', 'en-US')
        if intent not in ('login', 'signup') or not isinstance(locale, str) or len(locale) > 32:
            return Response({'error': 'invalid_request'}, status=400)
        secret = secrets.token_urlsafe(32)
        row = BrowserSession.objects.create(secret_hash=secret_hash(secret), intent=intent,
                    locale=locale, expires_at=timezone.now() + timedelta(minutes=15))
        query = urlencode({'session_id': row.session_id, 'intent': intent, 'locale': locale})
        return Response({'session_id': str(row.session_id), 'secret': secret,
                         'expires_at': row.expires_at,
                         'auth_url': settings.FRONTEND_URL.rstrip('/') + '/auth/browser-confirm?' + query})


class BrowserConfirmView(BrowserView):
    """Approve one account using a verified Clerk or onboarding Django session."""
    def post(self, request):
        """Bind once; a Clerk user without a game profile must complete onboarding."""
        clerk_token = request.data.get('clerk_token')
        if clerk_token:
            subject = clerk_subject(clerk_token)
            user = User.objects.filter(clerk_user_id=subject, is_active=True).first()
            if user is None:
                return Response({'error': 'onboarding_required'}, status=409)
        else:
            result = JWTAuthentication().authenticate(request)
            user = result[0] if result else None
            if user is None:
                raise AuthenticationFailed('Sign in to approve this request')
        with transaction.atomic():
            row = live_request(request.data)
            if not row:
                return Response({'error': 'request_expired'}, status=410)
            if row.is_authorized:
                return Response({'error': 'already_authorized'}, status=409)
            row.user, row.is_authorized = user, True
            row.save(update_fields=['user', 'is_authorized'])
        return Response({'status': 'authorized'})


class BrowserPollView(BrowserView):
    """Redeem an approved request exactly once under a database row lock."""
    def post(self, request):
        """Return pending or the existing Django token/profile response, never in URLs."""
        with transaction.atomic():
            row = live_request(request.data, private=True)
            if not row:
                return Response({'error': 'request_expired'}, status=410)
            if not row.is_authorized or not row.user_id:
                return Response({'status': 'pending'})
            if not row.user.is_active:
                return Response({'error': 'inactive_account'}, status=403)
            # Conditional update additionally protects SQLite regression runs.
            if not BrowserSession.objects.filter(pk=row.pk, consumed_at=None, cancelled_at=None).update(consumed_at=timezone.now()):
                return Response({'error': 'request_expired'}, status=410)
            refresh = RefreshToken.for_user(row.user)
            return Response({'access': str(refresh.access_token), 'refresh': str(refresh),
                             'user': UserProfileSerializer(row.user).data})


class BrowserCancelView(BrowserView):
    """Invalidate pending or approved requests using the device's private secret."""
    def post(self, request):
        """Serialize cancellation against confirmation and redemption."""
        with transaction.atomic():
            row = live_request(request.data, private=True)
            if not row:
                return Response({'error': 'request_expired'}, status=410)
            row.cancelled_at = timezone.now()
            row.save(update_fields=['cancelled_at'])
        return Response({'status': 'cancelled'})
