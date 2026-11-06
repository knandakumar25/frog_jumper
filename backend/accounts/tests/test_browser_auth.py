"""Device proof, real JWT verification and isolated PostgreSQL redemption regressions."""
from concurrent.futures import ThreadPoolExecutor
from datetime import timedelta
from types import SimpleNamespace
from unittest.mock import patch
import time

import jwt
from cryptography.hazmat.primitives.asymmetric import rsa
from django.core.cache import cache
from django.db import close_old_connections, connection
from django.test import TransactionTestCase, override_settings
from django.utils import timezone
from rest_framework.test import APITestCase, APIClient
from rest_framework_simplejwt.tokens import RefreshToken
from accounts.browser_auth import clerk_subject
from rest_framework.exceptions import AuthenticationFailed
from accounts.models import BrowserSession, User, PendingRegistration


class BrowserAuthTests(APITestCase):
    """All profiles and outbound email live only in the Django test database/transport."""
    def setUp(self):
        """Create a disposable onboarded identity and clear process-local throttles."""
        cache.clear()
        self.user = User.objects.create_user(username='device', email='device@example.invalid', clerk_user_id='user_device')

    def start(self):
        """Return a new private device request without logging its response."""
        result = self.client.post('/api/auth/browser-start/', {'intent': 'signup', 'locale': 'ja'}, format='json')
        self.assertEqual(result.status_code, 200)
        self.assertEqual(result['Cache-Control'], 'no-store')
        return result.data

    def post(self, action, data):
        """Submit device data as a JSON body, never a query parameter."""
        return self.client.post('/api/auth/browser-' + action + '/', data, format='json')

    def approve(self, data):
        """Approve with a signed Django JWT representing completed onboarding."""
        self.client.credentials(HTTP_AUTHORIZATION='Bearer ' + str(RefreshToken.for_user(self.user).access_token))
        result = self.post('confirm', {'session_id': data['session_id']})
        self.client.credentials()
        return result

    def test_pending_approval_redemption_and_replay(self):
        """The private secret is necessary before/after approval and redemption is single use."""
        data = self.start()
        row = BrowserSession.objects.get(session_id=data['session_id'])
        self.assertNotEqual(row.secret_hash, data['secret'])
        self.assertNotIn(data['secret'], data['auth_url'])
        self.assertIn('intent=signup', data['auth_url'])
        self.assertLess(abs((row.expires_at - timezone.now()).total_seconds() - 900), 3)
        self.assertEqual(self.post('poll', data).data, {'status': 'pending'})
        self.assertEqual(self.client.get('/api/auth/browser-poll/', {'session_id': data['session_id']}).status_code, 405)
        self.assertEqual(self.post('poll', {'session_id': data['session_id']}).status_code, 410)
        self.assertEqual(self.approve(data).status_code, 200)
        wrong = dict(data, secret='wrong')
        self.assertEqual(self.post('poll', wrong).status_code, 410)
        self.assertEqual(self.post('cancel', wrong).status_code, 410)
        result = self.post('poll', data)
        self.assertEqual(result.status_code, 200)
        self.assertEqual(result.data['user']['username'], 'device')
        self.assertTrue(result.data['access'])
        self.assertEqual(self.post('poll', data).status_code, 410)
        self.assertEqual(self.approve(data).status_code, 410)

    def test_cancel_expiry_and_legacy_requests(self):
        """Terminal or secretless requests cannot be approved or redeemed."""
        for approved in (False, True):
            data = self.start()
            if approved:
                self.approve(data)
            self.assertEqual(self.post('cancel', data).status_code, 200)
            self.assertEqual(self.post('poll', data).status_code, 410)
            self.assertEqual(self.approve(data).status_code, 410)
        data = self.start()
        BrowserSession.objects.filter(session_id=data['session_id']).update(expires_at=timezone.now() - timedelta(seconds=1))
        self.assertEqual(self.post('poll', data).status_code, 410)
        self.assertEqual(self.approve(data).status_code, 410)
        legacy = BrowserSession.objects.create()
        self.assertEqual(self.approve({'session_id': str(legacy.session_id)}).status_code, 410)

    def test_confirmation_needs_proof_and_binds_once(self):
        """A submitted Clerk ID alone never signs in, and approval cannot switch accounts later."""
        data = self.start()
        self.assertIn(self.post('confirm', dict(data, clerk_user_id='user_device')).status_code, (401, 403))
        self.assertEqual(self.approve(data).status_code, 200)
        self.assertEqual(self.approve(data).status_code, 409)

    @patch('accounts.browser_auth.clerk_subject', return_value='user_device')
    def test_clerk_profile_and_missing_onboarding(self, subject):
        """Identity matching uses only Clerk ID; matching email cannot merge a legacy account."""
        data = self.start()
        self.assertEqual(self.post('confirm', dict(data, clerk_token='verified-by-unit-fixture')).status_code, 200)
        subject.return_value = 'user_missing'
        data = self.start()
        self.assertEqual(self.post('confirm', dict(data, clerk_token='verified-by-unit-fixture')).data['error'], 'onboarding_required')

    def test_bad_input_and_throttle(self):
        """Malformed handles are rejected and request creation is bounded."""
        self.assertEqual(self.post('poll', {'session_id': 'bad', 'secret': 'x'}).status_code, 410)
        self.assertEqual(self.post('start', {'intent': 'execute'}).status_code, 400)
        with patch('accounts.browser_auth.BrowserThrottle.rate', '1/min'):
            cache.clear()
            self.assertEqual(self.post('start', {}).status_code, 200)
            self.assertEqual(self.post('start', {}).status_code, 429)

    @override_settings(CLERK_SECRET_KEY='sk_test_fixture')
    def test_email_completion_preserves_request_without_approving_it(self):
        """A new tab gets the durable public association; mailbox proof never auto-authorizes the game."""
        from accounts.clerk_client import ClerkUser
        from django.core import mail
        import re
        data = self.start()
        identity = ClerkUser(id='user_fresh', email='fresh@example.invalid', email_verified=True,
                             username='fresh', first_name='Fresh', last_name='Test')
        with patch('accounts.browser_auth.clerk_subject', return_value=identity.id), patch('accounts.clerk_client.fetch_clerk_user', return_value=identity):
            result = self.client.post('/api/auth/onboarding/start/', {'session_id': data['session_id'],
                        'clerk_token': 'test-proof', 'clerk_user_id': identity.id}, format='json')
        self.assertEqual(result.status_code, 200)
        pending = PendingRegistration.objects.get(clerk_user_id=identity.id)
        self.assertEqual(str(pending.browser_session.session_id), data['session_id'])
        token = re.search(r'/onboarding\?token=([\w-]+)', mail.outbox[-1].body)[1]
        validation = self.client.get('/api/auth/onboarding/validate-token/', {'token': token})
        self.assertEqual(validation.data['browser_locale'], 'ja')
        result = self.client.post('/api/auth/onboarding/complete/', {'token': token,
                    'country': 'Japan', 'date_of_birth': '1995-04-12'}, format='json')
        self.assertEqual(result.status_code, 200)
        self.assertEqual(result.data['browser_session_id'], data['session_id'])
        self.assertEqual(result.data['browser_locale'], 'ja')
        self.assertEqual(self.post('poll', data).data['status'], 'pending')
        self.client.credentials(HTTP_AUTHORIZATION='Bearer ' + result.data['access'])
        self.assertEqual(self.post('confirm', {'session_id': data['session_id']}).status_code, 200)
        self.client.credentials()
        self.assertEqual(self.post('poll', data).data['user']['email'], identity.email)


@override_settings(CLERK_ISSUER='https://issuer.example', CLERK_JWKS_URL='https://issuer.example/.well-known/jwks.json', CLERK_AUTHORIZED_PARTIES=['http://localhost:5173'])
class ClerkVerificationTests(APITestCase):
    """Sign real RS256 test JWTs; only the trusted JWKS network lookup is replaced."""
    @classmethod
    def setUpClass(cls):
        """Generate a test-only signing key, never a configured Clerk key."""
        super().setUpClass()
        cls.key = rsa.generate_private_key(public_exponent=65537, key_size=2048)

    def test_claims_signature_expiry_and_configuration(self):
        """Reject tampering, foreign issuer/origin, missing lifetime and pending sessions."""
        now = int(time.time())
        good = dict(iss='https://issuer.example', sub='user_test', exp=now+60, nbf=now-1, iat=now-1, azp='http://localhost:5173')
        with patch('accounts.browser_auth.jwks_client') as client:
            client.return_value.get_signing_key_from_jwt.return_value = SimpleNamespace(key=self.key.public_key())
            self.assertEqual(clerk_subject(jwt.encode(good, self.key, algorithm='RS256')), 'user_test')
            variants = [dict(good, exp=now-10), dict(good, iss='https://foreign'), dict(good, azp='https://foreign'), dict(good, nbf=now+100), dict(good, sts='pending')]
            missing = dict(good)
            del missing['exp']
            variants.append(missing)
            for claims in variants:
                with self.assertRaises(AuthenticationFailed):
                    clerk_subject(jwt.encode(claims, self.key, algorithm='RS256'))
            with self.assertRaises(AuthenticationFailed):
                clerk_subject(jwt.encode(good, 'wrong', algorithm='HS256'))
            with self.settings(CLERK_ISSUER=''):
                with self.assertRaises(AuthenticationFailed):
                    clerk_subject('anything')


class ConcurrentRedemptionTests(TransactionTestCase):
    """Exercise actual PostgreSQL row locking with separate connections."""
    def test_only_one_parallel_redeemer_receives_tokens(self):
        """Two simultaneous polls must produce exactly one credential response."""
        if connection.vendor != 'postgresql':
            self.skipTest('Requires PostgreSQL row locks')
        cache.clear()
        user = User.objects.create_user(username='parallel', email='parallel@example.invalid')
        data = APIClient().post('/api/auth/browser-start/', {}, format='json').data
        BrowserSession.objects.filter(session_id=data['session_id']).update(user=user, is_authorized=True)

        def redeem(_):
            """Use a thread-local database connection and discard token bodies."""
            close_old_connections()
            try:
                return APIClient().post('/api/auth/browser-poll/', data, format='json').status_code
            finally:
                close_old_connections()

        with ThreadPoolExecutor(max_workers=2) as pool:
            results = list(pool.map(redeem, range(2)))
        self.assertEqual(sorted(results), [200, 410])
