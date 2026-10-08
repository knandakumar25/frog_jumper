"""Minimal Clerk Backend API client.

Shape deliberately mirrors core/mail_backends.py:

  * standard library only, so requirements.txt gains no entry and the container
    image needs no rebuild;
  * `urllib.request.urlopen` is called as a module attribute, never imported
    with `from ... import urlopen`, so `mock.patch("urllib.request.urlopen")`
    stays a valid target;
  * nothing touches the network or reads a secret at import time -- `manage.py
    migrate`, `manage.py check` and the test suite all run with no key present.

One endpoint is needed: fetch a user by id, to turn a Clerk user id into a
verified email address. Clerk owns credentials; Django owns the profile.
"""

import json
import logging
import re
import urllib.error
import urllib.parse
import urllib.request
from dataclasses import dataclass

from django.conf import settings

logger = logging.getLogger(__name__)

DEFAULT_API_BASE = "https://api.clerk.com/v1"

# Cloudflare fronts api.clerk.com exactly as it fronts api.resend.com, and the
# trap is the same: urllib's default "Python-urllib/3.x" User-Agent is answered
# with HTTP 403 and a body of "error code: 1010", before the request reaches
# Clerk. That reads like a bad API key and is not. See core/mail_backends.
USER_AGENT = "AxiomForce-Mailer/1.0"

# Clerk user ids look like "user_2abc...". Checked before the id is
# interpolated into a URL path -- without it, "user_a/../../x" or "user_a?x=1"
# reshapes the request. It is also the cheapest abuse control on /start/: a
# fabricated id never reaches the network.
USER_ID_RE = re.compile(r"^user_[A-Za-z0-9]{1,60}$")


class ClerkError(Exception):
    """Base class for Clerk transport failures."""


class ClerkUserNotFound(ClerkError):
    """Clerk answered 404 for a well-formed user id."""


class ClerkUnavailable(ClerkError):
    """The request never produced an answer: DNS, TLS, timeout, connection."""


class ClerkAPIError(ClerkError):
    """Clerk answered with an unexpected status, or an unreadable body."""


@dataclass(frozen=True)
class ClerkUser:
    """The subset of a Clerk user record this project cares about."""

    id: str
    email: str
    email_verified: bool
    username: str = ""
    first_name: str = ""
    last_name: str = ""


def is_valid_user_id(user_id):
    """True when `user_id` is a well-formed Clerk id.

    Callers check this before calling fetch_clerk_user so a malformed id costs
    nothing; fetch_clerk_user re-checks as a backstop.
    """
    return isinstance(user_id, str) and USER_ID_RE.match(user_id) is not None


def _api_base():
    return getattr(settings, "CLERK_API_BASE", DEFAULT_API_BASE).rstrip("/")


def _timeout():
    # A missing timeout is a hung gunicorn worker, which is worse than a failed
    # request, so this never returns None.
    try:
        return float(getattr(settings, "CLERK_TIMEOUT", 10))
    except (TypeError, ValueError):
        return 10.0


def fetch_clerk_user(user_id):
    """Return the ClerkUser for `user_id`.

    Raises ClerkUserNotFound for a unknown id, ClerkUnavailable when the request
    could not be completed, ClerkAPIError for anything else.
    """
    if not is_valid_user_id(user_id):
        raise ClerkUserNotFound("Malformed Clerk user id.")

    api_key = getattr(settings, "CLERK_SECRET_KEY", "")
    if not api_key:
        raise ClerkAPIError("CLERK_SECRET_KEY is not configured.")

    url = f"{_api_base()}/users/{urllib.parse.quote(user_id, safe='')}"
    request = urllib.request.Request(
        url,
        headers={
            "Authorization": f"Bearer {api_key}",
            "Accept": "application/json",
            "User-Agent": USER_AGENT,
        },
        method="GET",
    )

    try:
        with urllib.request.urlopen(request, timeout=_timeout()) as response:
            body = response.read().decode("utf-8", errors="replace")
    except urllib.error.HTTPError as exc:
        # The body is read before logging because an HTTPError's fp is a
        # consumed stream, and this detail is the only clue about why Clerk
        # refused. It is logged, never returned: it can echo request details,
        # and with DEBUG=True the response would be rendered into the browser.
        detail = exc.read().decode("utf-8", errors="replace")
        logger.warning(
            "Clerk refused GET /users/<id> (HTTP %s): %s", exc.code, detail
        )
        if exc.code == 404:
            raise ClerkUserNotFound("No such Clerk user.") from exc
        raise ClerkAPIError(f"Clerk returned HTTP {exc.code}.") from exc
    except urllib.error.URLError as exc:
        raise ClerkUnavailable(f"Could not reach Clerk: {exc.reason}") from exc
    except OSError as exc:
        # URLError is an OSError subclass and is caught above; this catches a
        # bare socket timeout, which urlopen can raise on its own.
        raise ClerkUnavailable(f"Could not reach Clerk: {exc}") from exc

    try:
        payload = json.loads(body)
    except ValueError as exc:
        raise ClerkAPIError("Clerk returned a non-JSON body.") from exc

    if not isinstance(payload, dict):
        raise ClerkAPIError("Clerk returned an unexpected body.")

    return _parse_user(payload)


def _verification_status(address):
    verification = address.get("verification") or {}
    return verification.get("status") or ""


def _parse_user(payload):
    addresses = payload.get("email_addresses") or []
    primary_id = payload.get("primary_email_address_id")

    # Resolution order, most authoritative first: the primary address, then any
    # verified address, then whatever Clerk listed first. The dashboard can be
    # configured without a primary, so the fallbacks are load-bearing.
    chosen = None
    if primary_id:
        for address in addresses:
            if address.get("id") == primary_id:
                chosen = address
                break
    if chosen is None:
        chosen = next(
            (a for a in addresses if _verification_status(a) == "verified"), None
        )
    if chosen is None and addresses:
        chosen = addresses[0]

    chosen = chosen or {}
    return ClerkUser(
        id=payload.get("id") or "",
        email=chosen.get("email_address") or "",
        email_verified=_verification_status(chosen) == "verified",
        username=payload.get("username") or "",
        first_name=payload.get("first_name") or "",
        last_name=payload.get("last_name") or "",
    )
