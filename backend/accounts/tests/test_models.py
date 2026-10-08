from datetime import timedelta

from django.contrib.auth import get_user_model
from django.db import IntegrityError, transaction
from django.test import TestCase
from django.utils import timezone

from accounts.models import BrowserSession, PendingRegistration, TwoFactorCode

User = get_user_model()


class UserModelTest(TestCase):
    def setUp(self):
        self.user = User.objects.create_user(
            username='frogtester',
            email='frogtester@example.com',
            password='SecretPassword123!',
            country='Palestine (State of)',
            phone_number='+1234567890'
        )

    def test_user_creation(self):
        self.assertEqual(self.user.username, 'frogtester')
        self.assertEqual(self.user.email, 'frogtester@example.com')
        self.assertEqual(self.user.country, 'Palestine (State of)')
        self.assertEqual(self.user.phone_number, '+1234567890')
        self.assertFalse(self.user.is_2fa_enabled)
        self.assertTrue(self.user.check_password('SecretPassword123!'))
        self.assertFalse(self.user.check_password('WrongPassword!'))

    def test_user_str(self):
        self.assertEqual(str(self.user), 'frogtester')


class BrowserSessionModelTest(TestCase):
    def setUp(self):
        self.user = User.objects.create_user(
            username='sessionuser',
            email='sessionuser@example.com',
            password='Password123!',
            country='Canada'
        )
        self.session = BrowserSession.objects.create(user=self.user)

    def test_session_creation(self):
        self.assertIsNotNone(self.session.session_id)
        self.assertEqual(self.session.user, self.user)
        self.assertFalse(self.session.is_authorized)
        self.assertIn(str(self.session.session_id), str(self.session))


class TwoFactorCodeModelTest(TestCase):
    def setUp(self):
        self.user = User.objects.create_user(
            username='twofauser',
            email='twofauser@example.com',
            password='Password123!',
            country='Japan'
        )
        self.code = TwoFactorCode.objects.create(
            user=self.user,
            code='123456',
            purpose='login'
        )

    def test_two_factor_code_creation(self):
        self.assertEqual(self.code.user, self.user)
        self.assertEqual(self.code.code, '123456')
        self.assertEqual(self.code.purpose, 'login')
        self.assertFalse(self.code.is_verified)
        self.assertIn('twofauser', str(self.code))


class PendingRegistrationModelTest(TestCase):
    def setUp(self):
        self.now = timezone.now()
        self.pending = PendingRegistration.objects.create(
            clerk_user_id='user_abc123',
            email='operator@example.com',
            token_hash='a' * 64,
            last_sent_at=self.now,
            expires_at=self.now + timedelta(hours=24),
        )

    def _extra(self, clerk_user_id, token_hash):
        return PendingRegistration.objects.create(
            clerk_user_id=clerk_user_id,
            email='operator@example.com',
            token_hash=token_hash,
            last_sent_at=self.now,
            expires_at=self.now + timedelta(hours=24),
        )

    def test_defaults(self):
        self.assertEqual(self.pending.sent_count, 0)
        self.assertIsNone(self.pending.completed_at)
        self.assertIsNone(self.pending.request_ip)
        self.assertEqual(self.pending.first_name, '')
        self.assertEqual(self.pending.clerk_username, '')
        self.assertIn('operator@example.com', str(self.pending))

    def test_clerk_user_id_is_the_idempotency_key(self):
        with self.assertRaises(IntegrityError):
            # atomic() so the failed INSERT rolls back to a savepoint instead of
            # poisoning the surrounding test transaction.
            with transaction.atomic():
                self._extra('user_abc123', 'b' * 64)

    def test_email_is_deliberately_not_unique(self):
        # Two Clerk users can legitimately claim one address; the collision is
        # resolved at completion (a 409), not here (an IntegrityError at signup).
        self._extra('user_def456', 'c' * 64)
        self.assertEqual(
            PendingRegistration.objects.filter(email='operator@example.com').count(), 2
        )

