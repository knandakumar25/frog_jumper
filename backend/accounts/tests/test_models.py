from django.test import TestCase
from django.contrib.auth import get_user_model
from accounts.models import BrowserSession, TwoFactorCode

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
