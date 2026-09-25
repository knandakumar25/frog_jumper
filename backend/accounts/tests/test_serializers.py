from django.test import TestCase
from django.contrib.auth import get_user_model
from accounts.serializers import UserSerializer, UserProfileSerializer

User = get_user_model()


class UserSerializerTest(TestCase):
    def test_valid_user_serializer(self):
        payload = {
            'username': 'newfrog',
            'email': 'newfrog@example.com',
            'password': 'StrongPassword123!',
            'country': 'Palestine (State of)',
            'first_name': 'Ribbit',
            'last_name': 'Jumper'
        }
        serializer = UserSerializer(data=payload)
        self.assertTrue(serializer.is_valid(), serializer.errors)
        user = serializer.save()

        self.assertEqual(user.username, 'newfrog')
        self.assertEqual(user.email, 'newfrog@example.com')
        self.assertEqual(user.country, 'Palestine (State of)')
        self.assertTrue(user.check_password('StrongPassword123!'))
        # Password should not be exposed in serializer representation
        self.assertNotIn('password', serializer.data)

    def test_missing_country_fails_validation(self):
        payload = {
            'username': 'nocountryfrog',
            'email': 'nocountry@example.com',
            'password': 'StrongPassword123!'
        }
        serializer = UserSerializer(data=payload)
        self.assertFalse(serializer.is_valid())
        self.assertIn('country', serializer.errors)

    def test_blank_country_fails_validation(self):
        payload = {
            'username': 'blankcountryfrog',
            'email': 'blankcountry@example.com',
            'password': 'StrongPassword123!',
            'country': ''
        }
        serializer = UserSerializer(data=payload)
        self.assertFalse(serializer.is_valid())
        self.assertIn('country', serializer.errors)


class UserProfileSerializerTest(TestCase):
    def setUp(self):
        self.user = User.objects.create_user(
            username='profilefrog',
            email='profilefrog@example.com',
            password='Password123!',
            country='Germany',
            phone_number='+49123456789'
        )

    def test_profile_serializer_fields(self):
        serializer = UserProfileSerializer(instance=self.user)
        data = serializer.data

        self.assertEqual(data['username'], 'profilefrog')
        self.assertEqual(data['email'], 'profilefrog@example.com')
        self.assertEqual(data['country'], 'Germany')
        self.assertEqual(data['phone_number'], '+49123456789')
        self.assertFalse(data['is_2fa_enabled'])
        self.assertNotIn('password', data)
