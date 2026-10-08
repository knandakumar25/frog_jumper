"""End-to-end coverage for the email-gated onboarding flow.

`fetch_clerk_user` is patched where it is *used*. accounts.onboarding_views
reaches it through the `clerk_client` module (`from . import clerk_client`), so
patching the module attribute is what the view actually calls. No test here
talks to Clerk, and mail is asserted through `mail.outbox` (locmem).
"""

import hashlib
import re
from datetime import timedelta
from unittest import mock

from django.contrib.auth import get_user_model
from django.core import mail
from django.db import IntegrityError
from django.test import override_settings
from django.urls import reverse
from django.utils import timezone
from rest_framework import status
from rest_framework.test import APITestCase

from accounts.clerk_client import ClerkUnavailable, ClerkUser, ClerkUserNotFound
from accounts.models import PendingRegistration

User = get_user_model()

CLERK_ID = 'user_2abcDEF123'
EMAIL = 'operator@example.com'
RAW_TOKEN = 'raw-onboarding-token'
TOKEN_RE = re.compile(r'/onboarding\?token=([A-Za-z0-9_\-]+)')

FETCH = 'accounts.clerk_client.fetch_clerk_user'
SEND = 'accounts.onboarding_views.send_onboarding_email'


def _hash(raw):
    return hashlib.sha256(raw.encode('utf-8')).hexdigest()


def _clerk_user(**overrides):
    defaults = dict(
        id=CLERK_ID,
        email=EMAIL,
        email_verified=True,
        username='kaito',
        first_name='Kaito',
        last_name='Kurogane',
    )
    defaults.update(overrides)
    return ClerkUser(**defaults)


@override_settings(CLERK_SECRET_KEY='sk_test_not_real')
class OnboardingTestCase(APITestCase):
    def setUp(self):
        self.start_url = reverse('onboarding_start')
        self.validate_url = reverse('onboarding_validate_token')
        self.complete_url = reverse('onboarding_complete')

    def _make_pending(self, raw_token=RAW_TOKEN, **overrides):
        defaults = dict(
            clerk_user_id=CLERK_ID,
            email=EMAIL,
            token_hash=_hash(raw_token),
            clerk_username='kaito',
            first_name='Kaito',
            last_name='Kurogane',
            last_sent_at=timezone.now(),
            expires_at=timezone.now() + timedelta(hours=1),
            sent_count=1,
        )
        defaults.update(overrides)
        return PendingRegistration.objects.create(**defaults)

    def _start(self, clerk_user=None, exc=None, clerk_user_id=CLERK_ID, **kwargs):
        with mock.patch(FETCH) as fetch:
            if exc is not None:
                fetch.side_effect = exc
            else:
                fetch.return_value = clerk_user if clerk_user is not None else _clerk_user()
            response = self.client.post(
                self.start_url, {'clerk_user_id': clerk_user_id}, format='json', **kwargs
            )
        return response, fetch

    def _complete(self, country='Japan', date_of_birth='1995-04-12', token=RAW_TOKEN, **kwargs):
        return self.client.post(
            self.complete_url,
            {'token': token, 'country': country, 'date_of_birth': date_of_birth},
            format='json',
            **kwargs,
        )


class StartViewTest(OnboardingTestCase):
    def test_sends_one_email_and_never_returns_the_token(self):
        response, _ = self._start()

        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.assertEqual(response.data['status'], 'email_sent')
        self.assertEqual(response.data['email'], 'o***@example.com')
        # Returning the token to the requesting browser would make the email
        # decorative and remove the proof-of-mailbox gate entirely.
        self.assertNotIn('token', response.data)

        self.assertEqual(len(mail.outbox), 1)
        match = TOKEN_RE.search(mail.outbox[0].body)
        self.assertIsNotNone(match, 'the email body must carry the link')

        # Proves hashing works end to end: what was stored is the digest of what
        # was mailed, and never the raw value.
        pending = PendingRegistration.objects.get(clerk_user_id=CLERK_ID)
        self.assertEqual(pending.token_hash, _hash(match.group(1)))
        self.assertNotEqual(pending.token_hash, match.group(1))
        self.assertEqual(len(pending.token_hash), 64)
        self.assertEqual(pending.sent_count, 1)

    def test_the_mailed_link_is_not_locale_prefixed(self):
        """The frontend "*" catch-all drops the query string, so a locale
        segment would silently discard ?token=."""
        self._start()
        link = TOKEN_RE.search(mail.outbox[0].body).group(0)
        self.assertTrue(link.startswith('/onboarding?token='))

    def test_a_malformed_id_is_rejected_before_any_network_call(self):
        response, fetch = self._start(clerk_user_id='user_a/../../x')

        self.assertEqual(response.status_code, status.HTTP_400_BAD_REQUEST)
        self.assertEqual(response.data['error'], 'invalid_clerk_user_id')
        fetch.assert_not_called()
        self.assertEqual(len(mail.outbox), 0)

    def test_an_unknown_clerk_user_is_a_404(self):
        response, _ = self._start(exc=ClerkUserNotFound('nope'))

        self.assertEqual(response.status_code, status.HTTP_404_NOT_FOUND)
        self.assertEqual(response.data['error'], 'unknown_clerk_user')
        self.assertEqual(len(mail.outbox), 0)

    def test_an_unreachable_clerk_is_a_502(self):
        response, _ = self._start(exc=ClerkUnavailable('certificate verify failed'))

        self.assertEqual(response.status_code, status.HTTP_502_BAD_GATEWAY)
        self.assertEqual(response.data['error'], 'clerk_unreachable')
        self.assertEqual(len(mail.outbox), 0)

    def test_an_unverified_email_is_refused_and_nothing_is_mailed(self):
        response, _ = self._start(clerk_user=_clerk_user(email_verified=False))

        self.assertEqual(response.status_code, status.HTTP_403_FORBIDDEN)
        self.assertEqual(response.data['error'], 'email_not_verified')
        self.assertEqual(len(mail.outbox), 0)
        self.assertFalse(PendingRegistration.objects.exists())

    @override_settings(CLERK_SECRET_KEY='')
    def test_without_a_key_the_endpoint_is_inert(self):
        with mock.patch(FETCH) as fetch:
            response = self.client.post(
                self.start_url, {'clerk_user_id': CLERK_ID}, format='json'
            )

        self.assertEqual(response.status_code, status.HTTP_503_SERVICE_UNAVAILABLE)
        self.assertEqual(response.data['error'], 'clerk_not_configured')
        fetch.assert_not_called()
        self.assertEqual(len(mail.outbox), 0)

    def test_a_second_request_inside_the_cooldown_does_not_mail_again(self):
        self._start()
        before = PendingRegistration.objects.get(clerk_user_id=CLERK_ID).token_hash

        response, _ = self._start()

        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.assertEqual(response.data['status'], 'already_sent')
        self.assertGreater(response.data['retry_after'], 0)
        self.assertEqual(len(mail.outbox), 1)
        # The existing link is untouched.
        self.assertEqual(
            PendingRegistration.objects.get(clerk_user_id=CLERK_ID).token_hash, before
        )

    def test_after_the_cooldown_the_token_rotates_and_the_old_link_dies(self):
        self._start()
        old_token = TOKEN_RE.search(mail.outbox[0].body).group(1)

        PendingRegistration.objects.filter(clerk_user_id=CLERK_ID).update(
            last_sent_at=timezone.now() - timedelta(minutes=5)
        )

        response, _ = self._start()

        self.assertEqual(response.data['status'], 'email_sent')
        self.assertEqual(len(mail.outbox), 2)
        new_token = TOKEN_RE.search(mail.outbox[1].body).group(1)
        self.assertNotEqual(old_token, new_token)

        # A hash cannot be reproduced, so rotating is the only option -- and it
        # kills the superseded link immediately.
        self.assertEqual(
            self.client.get(self.validate_url, {'token': old_token}).status_code,
            status.HTTP_404_NOT_FOUND,
        )
        self.assertEqual(
            self.client.get(self.validate_url, {'token': new_token}).status_code,
            status.HTTP_200_OK,
        )
        self.assertEqual(
            PendingRegistration.objects.get(clerk_user_id=CLERK_ID).sent_count, 2
        )

    def test_the_send_cap_stops_further_mail(self):
        self._make_pending(sent_count=99, last_sent_at=timezone.now() - timedelta(minutes=5))

        response, _ = self._start()

        self.assertEqual(response.status_code, status.HTTP_429_TOO_MANY_REQUESTS)
        self.assertEqual(response.data['error'], 'too_many_sends')
        self.assertEqual(len(mail.outbox), 0)

    @override_settings(ONBOARDING_GLOBAL_HOURLY_LIMIT=1)
    def test_the_global_cap_is_a_circuit_breaker(self):
        self._make_pending(last_sent_at=timezone.now())

        response, fetch = self._start()

        self.assertEqual(response.status_code, status.HTTP_429_TOO_MANY_REQUESTS)
        self.assertEqual(response.data['error'], 'rate_limited')
        # Checked before Clerk is called, so a capped hour costs nothing.
        fetch.assert_not_called()
        self.assertEqual(len(mail.outbox), 0)

    @override_settings(ONBOARDING_IP_HOURLY_LIMIT=1)
    def test_the_per_ip_cap_applies(self):
        self._make_pending(request_ip='203.0.113.7', last_sent_at=timezone.now())

        response, _ = self._start(REMOTE_ADDR='203.0.113.7')

        self.assertEqual(response.status_code, status.HTTP_429_TOO_MANY_REQUESTS)
        self.assertEqual(len(mail.outbox), 0)

    def test_a_failed_send_does_not_consume_the_cooldown_or_the_send_cap(self):
        with mock.patch(SEND, side_effect=RuntimeError('provider down')):
            with self.assertLogs('accounts.onboarding_views', level='ERROR'):
                response, _ = self._start()

        self.assertEqual(response.status_code, status.HTTP_502_BAD_GATEWAY)
        self.assertEqual(response.data['error'], 'email_send_failed')
        # Nothing was persisted, so the retry is a genuine second attempt rather
        # than a cooldown that would swallow it.
        self.assertFalse(PendingRegistration.objects.exists())

        retry, _ = self._start()

        self.assertEqual(retry.status_code, status.HTTP_200_OK)
        self.assertEqual(retry.data['status'], 'email_sent')
        self.assertEqual(len(mail.outbox), 1)

    def test_a_concurrent_first_request_does_not_500(self):
        """Two /start/ calls racing for the same Clerk id: one wins the insert.

        The loser's token was never stored, so its link is already dead -- the
        response must say so rather than surfacing an IntegrityError.
        """
        with mock.patch(
            'accounts.onboarding_views.PendingRegistration.objects.create',
            side_effect=IntegrityError('duplicate key value violates unique constraint'),
        ):
            response, _ = self._start()

        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.assertEqual(response.data['status'], 'already_sent')

    def test_a_completed_registration_cannot_start_again(self):
        self._make_pending(completed_at=timezone.now())

        response, fetch = self._start()

        self.assertEqual(response.status_code, status.HTTP_409_CONFLICT)
        self.assertEqual(response.data['error'], 'already_registered')
        fetch.assert_not_called()

    def test_a_clerk_id_already_linked_to_a_user_is_a_409(self):
        User.objects.create_user(
            username='existing',
            email='other@example.com',
            country='Japan',
            clerk_user_id=CLERK_ID,
        )
        mail.outbox.clear()  # drop the welcome mail the signal just sent

        response, _ = self._start()

        self.assertEqual(response.status_code, status.HTTP_409_CONFLICT)
        self.assertEqual(len(mail.outbox), 0)


class ValidateTokenViewTest(OnboardingTestCase):
    def test_a_missing_token_is_a_400(self):
        response = self.client.get(self.validate_url)
        self.assertEqual(response.status_code, status.HTTP_400_BAD_REQUEST)

    def test_an_unknown_token_is_a_404(self):
        response = self.client.get(self.validate_url, {'token': 'nope'})
        self.assertEqual(response.status_code, status.HTTP_404_NOT_FOUND)

    def test_a_valid_token_returns_the_profile_without_creating_a_user(self):
        self._make_pending()

        response = self.client.get(self.validate_url, {'token': RAW_TOKEN})

        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.assertEqual(response.data['status'], 'valid')
        self.assertEqual(response.data['email'], EMAIL)
        self.assertEqual(response.data['first_name'], 'Kaito')
        # Validating is read-only: the User is not created until /complete/.
        self.assertFalse(User.objects.exists())

    def test_an_expired_token_is_a_410(self):
        self._make_pending(expires_at=timezone.now() - timedelta(minutes=1))

        response = self.client.get(self.validate_url, {'token': RAW_TOKEN})

        self.assertEqual(response.status_code, status.HTTP_410_GONE)
        self.assertEqual(response.data['error'], 'token_expired')

    def test_validating_twice_leaves_the_token_usable(self):
        """Only /complete/ consumes: a page refresh must not destroy the link."""
        self._make_pending()

        for _ in range(2):
            self.assertEqual(
                self.client.get(self.validate_url, {'token': RAW_TOKEN}).status_code,
                status.HTTP_200_OK,
            )

        self.assertIsNone(PendingRegistration.objects.get().completed_at)

    def test_an_already_completed_token_is_a_409(self):
        self._make_pending(completed_at=timezone.now())

        response = self.client.get(self.validate_url, {'token': RAW_TOKEN})

        self.assertEqual(response.status_code, status.HTTP_409_CONFLICT)
        self.assertEqual(response.data['error'], 'already_completed')


class CompleteViewTest(OnboardingTestCase):
    def test_completing_creates_the_user_and_returns_a_token_pair(self):
        self._make_pending()

        response = self._complete()

        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.assertIn('access', response.data)
        self.assertIn('refresh', response.data)
        self.assertEqual(response.data['user']['username'], 'kaito')
        self.assertEqual(response.data['user']['country'], 'Japan')

        user = User.objects.get(clerk_user_id=CLERK_ID)
        self.assertEqual(user.email, EMAIL)
        self.assertEqual(user.country, 'Japan')
        self.assertEqual(str(user.date_of_birth), '1995-04-12')
        self.assertEqual(user.first_name, 'Kaito')
        # Clerk owns credentials, so there is no password to log in with.
        self.assertFalse(user.has_usable_password())
        self.assertIsNotNone(PendingRegistration.objects.get().completed_at)

    def test_the_welcome_email_still_fires_on_completion(self):
        self._make_pending()

        self._complete()

        # The only mail in the outbox here is the post_save welcome mail --
        # _make_pending writes the row directly, and the onboarding mail is sent
        # by /start/. Keeping it for Clerk users is deliberate.
        self.assertEqual(len(mail.outbox), 1)
        self.assertIn('Welcome', mail.outbox[0].subject)

    def test_the_created_account_refuses_password_login(self):
        self._make_pending()
        self._complete()

        response = self.client.post(
            reverse('login'), {'username': 'kaito', 'password': 'anything'}, format='json'
        )

        self.assertEqual(response.status_code, status.HTTP_401_UNAUTHORIZED)

    def test_replaying_the_link_is_a_409_and_creates_nothing_extra(self):
        self._make_pending()
        self._complete()

        response = self._complete()

        self.assertEqual(response.status_code, status.HTTP_409_CONFLICT)
        self.assertEqual(response.data['error'], 'already_completed')
        self.assertEqual(User.objects.count(), 1)

    def test_an_unknown_token_is_a_404(self):
        response = self._complete(token='nope')
        self.assertEqual(response.status_code, status.HTTP_404_NOT_FOUND)

    def test_an_expired_token_is_a_410(self):
        self._make_pending(expires_at=timezone.now() - timedelta(minutes=1))
        response = self._complete()
        self.assertEqual(response.status_code, status.HTTP_410_GONE)

    def test_an_unknown_country_is_a_400(self):
        self._make_pending()

        response = self._complete(country='Atlantis')

        self.assertEqual(response.status_code, status.HTTP_400_BAD_REQUEST)
        self.assertIn('country', response.data)
        self.assertFalse(User.objects.exists())

    def test_iso_country_codes_are_not_accepted_as_names(self):
        """Only name.common is stored: Account.jsx already writes 'Japan', and
        accepting codes too would leave the column a mix of two formats."""
        self._make_pending()
        self.assertEqual(
            self._complete(country='JP').status_code, status.HTTP_400_BAD_REQUEST
        )

    def test_a_future_date_of_birth_is_a_400(self):
        self._make_pending()
        future = (timezone.localdate() + timedelta(days=1)).isoformat()

        response = self._complete(date_of_birth=future)

        self.assertEqual(response.status_code, status.HTTP_400_BAD_REQUEST)
        self.assertIn('date_of_birth', response.data)

    def test_todays_date_is_accepted_because_there_is_no_age_gate(self):
        self._make_pending()

        response = self._complete(date_of_birth=timezone.localdate().isoformat())

        self.assertEqual(response.status_code, status.HTTP_200_OK)

    def test_a_pre_1900_date_of_birth_is_a_400(self):
        self._make_pending()
        response = self._complete(date_of_birth='0202-01-01')
        self.assertEqual(response.status_code, status.HTTP_400_BAD_REQUEST)

    def test_a_malformed_date_of_birth_is_a_400(self):
        self._make_pending()
        response = self._complete(date_of_birth='not-a-date')
        self.assertEqual(response.status_code, status.HTTP_400_BAD_REQUEST)

    def test_an_email_already_on_a_legacy_account_is_a_409_not_a_500(self):
        User.objects.create_user(
            username='legacy', email=EMAIL, password='Password123!', country='Japan'
        )
        self._make_pending()

        response = self._complete()

        self.assertEqual(response.status_code, status.HTTP_409_CONFLICT)
        self.assertEqual(response.data['error'], 'already_registered')
        self.assertEqual(User.objects.count(), 1)

    def test_a_username_collision_gets_a_suffix(self):
        User.objects.create_user(
            username='kaito', email='someone@example.com', password='Password123!', country='Japan'
        )
        self._make_pending()

        response = self._complete()

        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.assertEqual(response.data['user']['username'], 'kaito_1')

    def test_an_absent_clerk_username_falls_back_to_the_email_local_part(self):
        self._make_pending(clerk_username='')

        response = self._complete()

        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.assertEqual(response.data['user']['username'], 'operator')


class StaleBearerTokenTest(OnboardingTestCase):
    """`permission_classes = [AllowAny]` does NOT disable DRF authenticators.

    Without `authentication_classes = []`, JWTAuthentication raises on a stale
    or malformed token and DRF answers 401 before the permission check is ever
    reached -- locking out every visitor whose localStorage holds an expired
    accessToken, on endpoints meant to be anonymous.
    """

    STALE = {'HTTP_AUTHORIZATION': 'Bearer not.a.real.token'}

    def test_start_accepts_a_stale_bearer_token(self):
        with mock.patch(FETCH, return_value=_clerk_user()):
            response = self.client.post(
                self.start_url, {'clerk_user_id': CLERK_ID}, format='json', **self.STALE
            )
        self.assertEqual(response.status_code, status.HTTP_200_OK)

    def test_validate_accepts_a_stale_bearer_token(self):
        self._make_pending()
        response = self.client.get(self.validate_url, {'token': RAW_TOKEN}, **self.STALE)
        self.assertEqual(response.status_code, status.HTTP_200_OK)

    def test_complete_accepts_a_stale_bearer_token(self):
        self._make_pending()
        response = self._complete(**self.STALE)
        self.assertEqual(response.status_code, status.HTTP_200_OK)

    def test_the_country_list_accepts_a_stale_bearer_token(self):
        """CountryListView carried this bug before onboarding existed."""
        response = self.client.get(reverse('country_list'), **self.STALE)
        self.assertEqual(response.status_code, status.HTTP_200_OK)
