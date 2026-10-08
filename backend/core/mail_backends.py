"""Django mail backend that delivers via Resend's HTTP API.

Deliberately uses only the standard library: this adds no dependency to
requirements.txt (which would force a container image rebuild), and outbound
HTTP elsewhere in this project already uses `urllib.request` -- see the
Epic/Discord/Steam OAuth callbacks in accounts/views.py.

Nothing here touches the network at import time or in __init__ -- only inside
send_messages(). That is load-bearing: `manage.py migrate`, `manage.py check`
and the test suite must all run without a Resend key present.

Django 6.1 note: EMAIL_BACKEND and the rest of the EMAIL_* settings are
deprecated (see django/conf/__init__.py DEPRECATED_EMAIL_SETTINGS) in favour of
MAILERS. Defining both raises ImproperlyConfigured, so this backend is wired up
through MAILERS in core/settings.py and EMAIL_BACKEND must stay deleted.
"""

import base64
import json
import logging
import urllib.error
import urllib.request

from django.conf import settings
from django.core.exceptions import ImproperlyConfigured
from django.core.mail.backends.base import BaseEmailBackend

logger = logging.getLogger(__name__)

DEFAULT_API_URL = "https://api.resend.com/emails"

# Cloudflare fronts api.resend.com and rejects Python's default
# "Python-urllib/3.x" User-Agent with HTTP 403 and a body of
# "error code: 1010" -- before the request ever reaches Resend. Verified by
# sending the same request twice: with urllib's default it 403s, with this
# header Resend answers properly (401 for a bad key). Without it, every send
# fails in a way that looks like an auth or domain problem but is neither.
USER_AGENT = "AxiomForce-Mailer/1.0"


class ResendAPIError(Exception):
    """Raised when the Resend API rejects or cannot deliver a message."""


class ResendEmailBackend(BaseEmailBackend):
    """Sends EmailMessage objects through https://api.resend.com/emails.

    Wired up from MAILERS, so Django constructs it as
    ``backend_class(alias=alias, **OPTIONS)`` -- see
    django/core/mail/handler.py MailersHandler.create_connection. The alias and
    any _ignore_unknown_kwargs are forwarded to super() untouched, because
    BaseEmailBackend raises InvalidMailer for options it does not recognise.
    """

    def __init__(
        self,
        *,
        api_key=None,
        api_url=DEFAULT_API_URL,
        timeout=10.0,
        fail_silently=False,
        **kwargs,
    ):
        self.api_key = (
            api_key if api_key is not None else getattr(settings, "RESEND_API_KEY", "")
        )
        self.api_url = api_url
        self.timeout = timeout
        # BaseEmailBackend.fail_silently is deprecated; a subclass that supports
        # it must keep its own attribute (see base.py __getattr__).
        self.fail_silently = fail_silently
        super().__init__(**kwargs)

    def send_messages(self, email_messages):
        """Send each message, returning the number accepted by Resend."""
        if not email_messages:
            return 0

        sent = 0
        for message in email_messages:
            try:
                self._send(message)
            except Exception:
                if not self.fail_silently:
                    raise
                # Swallowed deliberately, but never silently: log the whole
                # traceback so the Resend error body is recoverable.
                logger.exception("Resend: failed to send %r", message.subject)
            else:
                sent += 1
        return sent

    def _send(self, message):
        if not self.api_key:
            raise ImproperlyConfigured(
                "RESEND_API_KEY is not set, so the Resend mail backend cannot "
                "send. Set it in the environment (see .env.example)."
            )

        payload = self._build_payload(message)
        request = urllib.request.Request(
            self.api_url,
            data=json.dumps(payload).encode("utf-8"),
            headers={
                "Authorization": f"Bearer {self.api_key}",
                "Content-Type": "application/json",
                "User-Agent": USER_AGENT,
            },
            method="POST",
        )

        # NOTE: module-attribute call (not `from urllib.request import urlopen`)
        # so that mock.patch("urllib.request.urlopen") stays a valid target.
        try:
            with urllib.request.urlopen(request, timeout=self.timeout) as response:
                body = response.read().decode("utf-8", errors="replace")
        except urllib.error.HTTPError as exc:
            detail = exc.read().decode("utf-8", errors="replace")
            raise ResendAPIError(
                f"Resend rejected the message (HTTP {exc.code}): {detail}"
            ) from exc
        except urllib.error.URLError as exc:
            raise ResendAPIError(f"Could not reach Resend: {exc.reason}") from exc

        try:
            message_id = json.loads(body).get("id") if body else None
        except ValueError:
            message_id = None

        # A 200 means the message was *accepted*, not delivered -- delivery is
        # asynchronous, so the id is the handle for chasing it in the dashboard.
        logger.info(
            "Resend accepted %r for %s (id=%s)", message.subject, message.to, message_id
        )
        return message_id

    def _build_payload(self, message):
        """Map a Django EmailMessage onto Resend's documented JSON body."""
        # Mirror EmailMessage.message(), which lets extra_headers override From.
        from_email = message.extra_headers.get("From") or message.from_email

        payload = {
            "from": from_email,
            "to": list(message.to),
            "subject": message.subject,
        }

        if message.cc:
            payload["cc"] = list(message.cc)
        if message.bcc:
            payload["bcc"] = list(message.bcc)
        if message.reply_to:
            payload["reply_to"] = list(message.reply_to)

        # attach_alternative() stores (content, mimetype) namedtuples -- but
        # only on EmailMultiAlternatives. A plain EmailMessage has no
        # `alternatives` attribute at all, so default to no HTML rather than
        # trusting it to exist. (django.core.mail.send_mail() builds an
        # EmailMultiAlternatives, so both existing call sites do have it.)
        html = next(
            (
                alternative.content
                for alternative in getattr(message, "alternatives", ())
                if alternative.mimetype == "text/html"
            ),
            None,
        )
        if html is not None:
            payload["html"] = html
        if message.body:
            payload["text"] = message.body

        attachments = self._encode_attachments(message)
        if attachments:
            payload["attachments"] = attachments

        return payload

    def _encode_attachments(self, message):
        encoded = []
        for attachment in message.attachments:
            if not isinstance(attachment, tuple):
                # Django 6.1 also accepts email.message.MIMEPart here. Resend
                # takes base64 bytes, so fail loudly rather than drop the file.
                raise ResendAPIError(
                    "The Resend backend does not support MIMEPart attachments. "
                    "Attach bytes content instead."
                )
            filename, content, mimetype = attachment
            if isinstance(content, str):
                content = content.encode("utf-8")
            encoded.append(
                {
                    "filename": filename,
                    "content": base64.b64encode(content).decode("ascii"),
                    "content_type": mimetype,
                }
            )
        return encoded
