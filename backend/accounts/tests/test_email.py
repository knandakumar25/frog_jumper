"""Coverage for the two email paths the backend already had.

Neither had a test before, which is why the hardcoded from-address in
`accounts/utils.py` went unnoticed. Everything here reads `mail.outbox`, which
the locmem backend fills during tests: `SimpleTestCase._pre_setup`
(django/test/testcases.py) resets it before *every* test, and
`setup_test_environment()` swaps the mailer to locmem for the whole run.

Consequence: no test asserts on the *configured* MAILERS backend, because
during tests it is locmem no matter what settings.py says.
"""

from unittest import mock

from django.conf import settings
from django.contrib.auth import get_user_model
from django.core import mail
from django.test import TestCase
from django.urls import reverse
from rest_framework import status
from rest_framework.test import APITestCase

from accounts.models import TwoFactorCode

User = get_user_model()


class WelcomeEmailTest(TestCase):
    """The welcome mail rides the post_save signal on User."""

    def test_welcome_email_sent_on_user_create(self):
        User.objects.create_user(
            username='mailfrog',
            email='mailfrog@example.com',
            password='Password123!',
            country='Japan',
        )

        self.assertEqual(len(mail.outbox), 1)
        message = mail.outbox[0]

        self.assertEqual(message.to, ['mailfrog@example.com'])
        self.assertEqual(
            message.subject, 'SYS_INIT: Welcome to Frog Jumper, mailfrog'
        )
        self.assertIn('Operator mailfrog', message.body)
        self.assertIn('LEVEL 1 OPERATOR', message.body)

        # The signal builds an EmailMultiAlternatives, so the HTML lives in
        # alternatives rather than in .body.
        html = [a for a in message.alternatives if a.mimetype == 'text/html']
        self.assertEqual(len(html), 1)
        # The template upper-cases the username, so match it as rendered.
        self.assertIn('OPERATOR_MAILFROG', html[0].content)

    def test_welcome_email_uses_the_configured_from_address(self):
        """Pins the utils.py fix: the from-address must come from settings.

        It used to be hardcoded to an apex domain that was never verified,
        which Resend answers with a 403.
        """
        User.objects.create_user(
            username='fromfrog',
            email='fromfrog@example.com',
            password='Password123!',
            country='Japan',
        )

        self.assertEqual(len(mail.outbox), 1)
        self.assertEqual(mail.outbox[0].from_email, settings.DEFAULT_FROM_EMAIL)

    def test_no_email_when_the_user_has_no_address(self):
        """The signal returns early rather than sending to ''."""
        User.objects.create_user(
            username='noaddressfrog',
            email='',
            password='Password123!',
            country='Japan',
        )

        self.assertEqual(len(mail.outbox), 0)

    def test_registration_never_touches_the_network(self):
        """Guard against bypassing the mailer layer.

        If a future change calls the Resend API directly from the signal, then
        every test run would make live HTTPS calls and send real mail to real
        addresses -- exactly what routing through a mail backend prevents.
        """
        with mock.patch('urllib.request.urlopen') as urlopen:
            User.objects.create_user(
                username='offlinefrog',
                email='offlinefrog@example.com',
                password='Password123!',
                country='Japan',
            )

        urlopen.assert_not_called()
        self.assertEqual(len(mail.outbox), 1)


class TwoFactorEmailTest(APITestCase):
    """Covers utils.send_2fa_email via both of its call sites."""

    def setUp(self):
        self.login_url = reverse('login')
        self.user = User.objects.create_user(
            username='twofafrog',
            email='twofafrog@example.com',
            password='CorrectPassword123!',
            country='Canada',
            is_2fa_enabled=True,
        )
        # _pre_setup cleared the outbox before setUp ran, so drop the welcome
        # message here to keep the counts below unambiguous.
        mail.outbox.clear()

    def test_login_sends_a_code_when_2fa_is_enabled(self):
        response = self.client.post(
            self.login_url,
            {'username': 'twofafrog', 'password': 'CorrectPassword123!'},
            format='json',
        )

        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.assertEqual(response.data['status'], '2fa_required')

        self.assertEqual(len(mail.outbox), 1)
        message = mail.outbox[0]
        self.assertEqual(message.to, ['twofafrog@example.com'])
        self.assertEqual(message.from_email, settings.DEFAULT_FROM_EMAIL)
        self.assertIn('Login Code', message.subject)

        # The mailed code is the one stored against the login session.
        stored = TwoFactorCode.objects.get(user=self.user, purpose='login')
        self.assertIn(stored.code, message.body)

    def test_opt_in_sends_a_code(self):
        self.client.force_authenticate(user=self.user)
        response = self.client.post(reverse('two_factor_enable'))

        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.assertEqual(len(mail.outbox), 1)
        message = mail.outbox[0]
        self.assertEqual(message.to, ['twofafrog@example.com'])
        self.assertEqual(message.from_email, settings.DEFAULT_FROM_EMAIL)
        self.assertIn('2FA Setup', message.subject)

        stored = TwoFactorCode.objects.get(user=self.user, purpose='opt_in')
        self.assertIn(stored.code, message.body)

    def test_login_without_2fa_sends_nothing(self):
        self.user.is_2fa_enabled = False
        self.user.save()

        response = self.client.post(
            self.login_url,
            {'username': 'twofafrog', 'password': 'CorrectPassword123!'},
            format='json',
        )

        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.assertIn('access', response.data)
        self.assertEqual(len(mail.outbox), 0)
