from django.test import TestCase
from django.urls import reverse
from rest_framework import status
from rest_framework.test import APITestCase
from django.contrib.auth import get_user_model
from accounts.models import BrowserSession

User = get_user_model()


class RegisterViewTest(APITestCase):
    def setUp(self):
        self.register_url = reverse('register')

    def test_register_success(self):
        payload = {
            'username': 'cyberfrog',
            'email': 'cyberfrog@example.com',
            'password': 'SecurePassword123!',
            'country': 'Palestine (State of)',
            'first_name': 'Cyber',
            'last_name': 'Frog'
        }
        response = self.client.post(self.register_url, payload, format='json')
        self.assertEqual(response.status_code, status.HTTP_201_CREATED)
        self.assertIn('username', response.data)
        self.assertEqual(response.data['username'], 'cyberfrog')
        self.assertEqual(response.data['country'], 'Palestine (State of)')
        self.assertTrue(User.objects.filter(username='cyberfrog').exists())

    def test_register_missing_country_fails(self):
        payload = {
            'username': 'nocountryfrog',
            'email': 'nocountry@example.com',
            'password': 'SecurePassword123!'
        }
        response = self.client.post(self.register_url, payload, format='json')
        self.assertEqual(response.status_code, status.HTTP_400_BAD_REQUEST)
        self.assertIn('country', response.data)

    def test_register_duplicate_username_fails(self):
        User.objects.create_user(
            username='existingfrog',
            email='existing1@example.com',
            password='Password123!',
            country='Japan'
        )
        payload = {
            'username': 'existingfrog',
            'email': 'existing2@example.com',
            'password': 'Password123!',
            'country': 'Japan'
        }
        response = self.client.post(self.register_url, payload, format='json')
        self.assertEqual(response.status_code, status.HTTP_400_BAD_REQUEST)


class LoginViewTest(APITestCase):
    def setUp(self):
        self.login_url = reverse('login')
        self.user = User.objects.create_user(
            username='loginfrog',
            email='loginfrog@example.com',
            password='CorrectPassword123!',
            country='Canada'
        )

    def test_login_success(self):
        payload = {
            'username': 'loginfrog',
            'password': 'CorrectPassword123!'
        }
        response = self.client.post(self.login_url, payload, format='json')
        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.assertIn('access', response.data)
        self.assertIn('refresh', response.data)
        self.assertIn('user', response.data)
        self.assertEqual(response.data['user']['username'], 'loginfrog')
        self.assertEqual(response.data['user']['country'], 'Canada')

    def test_login_wrong_password_fails(self):
        payload = {
            'username': 'loginfrog',
            'password': 'WrongPassword999!'
        }
        response = self.client.post(self.login_url, payload, format='json')
        self.assertEqual(response.status_code, status.HTTP_401_UNAUTHORIZED)


class ProfileViewTest(APITestCase):
    def setUp(self):
        self.profile_url = reverse('profile')
        self.user = User.objects.create_user(
            username='profiletester',
            email='profiletester@example.com',
            password='Password123!',
            country='Germany'
        )

    def test_profile_authenticated(self):
        self.client.force_authenticate(user=self.user)
        response = self.client.get(self.profile_url)
        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.assertEqual(response.data['username'], 'profiletester')
        self.assertEqual(response.data['country'], 'Germany')

    def test_profile_unauthenticated_fails(self):
        response = self.client.get(self.profile_url)
        self.assertEqual(response.status_code, status.HTTP_401_UNAUTHORIZED)


class CountryListViewTest(APITestCase):
    def setUp(self):
        self.countries_url = reverse('country_list')

    def test_country_list_success(self):
        response = self.client.get(self.countries_url)
        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.assertIsInstance(response.data, list)
        self.assertGreater(len(response.data), 0)


class CountryBasicListViewTest(APITestCase):
    """The lean list the onboarding page uses.

    /countries/ is left alone because Account.jsx depends on its full shape;
    this endpoint exists so onboarding does not ship 788 KB to a phone.
    """

    def setUp(self):
        self.basic_url = reverse('country_list_basic')

    def test_returns_only_the_fields_onboarding_needs(self):
        response = self.client.get(self.basic_url)
        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.assertGreater(len(response.data), 0)

        entry = response.data[0]
        # `translations` (24 languages), `idd`, `currencies`, `borders` and the
        # rest are what make the full list large. Onboarding needs none of them.
        self.assertEqual(set(entry.keys()), {'cca3', 'name', 'flag'})
        self.assertEqual(set(entry['name'].keys()), {'common'})
        full = {c['cca3']: c for c in self.client.get(reverse('country_list')).data}
        for country in response.data:
            self.assertEqual(country['flag'], full[country['cca3']].get('flag', ''))

    def test_covers_exactly_the_same_countries_as_the_full_list(self):
        full = {c['name']['common'] for c in self.client.get(reverse('country_list')).data}
        basic = {c['name']['common'] for c in self.client.get(self.basic_url).data}
        self.assertEqual(full, basic)


# Browser handoff security and single-use regressions live in test_browser_auth.py.
