"""Unit tests for core.mail_backends.ResendEmailBackend.

The backend is constructed *directly* rather than resolved through
`django.core.mail.get_connection`. During tests
`setup_test_environment()` rewrites MAILERS to the locmem backend, so going
through settings would silently hand back locmem and make every assertion here
vacuous.
"""

import io
import json
import urllib.error
from unittest import mock

from django.core.exceptions import ImproperlyConfigured
from django.core.mail import EmailMessage, EmailMultiAlternatives
from django.test import SimpleTestCase

from core.mail_backends import USER_AGENT, ResendAPIError, ResendEmailBackend

API_URL = 'https://api.resend.com/emails'
KEY = 're_test_key_not_real'


def _accepted(message_id='msg_123'):
    """Stand-in for what `urlopen(...)` yields as a context manager."""
    response = mock.MagicMock()
    response.read.return_value = json.dumps({'id': message_id}).encode('utf-8')
    # urlopen's result is used via `with ... as response`, and MagicMock's
    # default __enter__ returns a *different* mock -- point it at this one so
    # the .read() above is what the backend actually sees.
    response.__enter__.return_value = response
    return response


def _http_error(code=403, body=b'{"name":"validation_error"}'):
    return urllib.error.HTTPError(
        url=API_URL, code=code, msg='Forbidden', hdrs=None, fp=io.BytesIO(body)
    )


class PayloadTest(SimpleTestCase):
    def setUp(self):
        self.backend = ResendEmailBackend(api_key=KEY)

    def _message(self):
        message = EmailMultiAlternatives(
            subject='SYS_INIT: Welcome',
            body='plain text body',
            from_email='Axiom Force <welcome@example.com>',
            to=['operator@example.com'],
            cc=['cc@example.com'],
            reply_to=['reply@example.com'],
        )
        message.attach_alternative('<p>html body</p>', 'text/html')
        return message

    def test_payload_and_request_shape(self):
        with mock.patch(
            'urllib.request.urlopen', return_value=_accepted()
        ) as urlopen:
            sent = self.backend.send_messages([self._message()])

        self.assertEqual(sent, 1)
        urlopen.assert_called_once()

        request = urlopen.call_args.args[0]
        self.assertEqual(request.full_url, API_URL)
        self.assertEqual(request.method, 'POST')
        self.assertEqual(request.get_header('Authorization'), f'Bearer {KEY}')
        self.assertEqual(request.get_header('Content-type'), 'application/json')
        self.assertEqual(urlopen.call_args.kwargs['timeout'], 10.0)
        # Load-bearing: Cloudflare in front of api.resend.com rejects urllib's
        # default User-Agent with 403 "error code: 1010" before Resend sees the
        # request, so omitting this header breaks every send.
        self.assertEqual(request.get_header('User-agent'), USER_AGENT)
        self.assertNotIn('python-urllib', request.get_header('User-agent').lower())

        payload = json.loads(request.data.decode('utf-8'))
        self.assertEqual(payload['from'], 'Axiom Force <welcome@example.com>')
        self.assertEqual(payload['to'], ['operator@example.com'])
        self.assertEqual(payload['subject'], 'SYS_INIT: Welcome')
        self.assertEqual(payload['text'], 'plain text body')
        self.assertEqual(payload['html'], '<p>html body</p>')
        self.assertEqual(payload['cc'], ['cc@example.com'])
        self.assertEqual(payload['reply_to'], ['reply@example.com'])
        # Empty lists are omitted rather than sent as [], which Resend rejects.
        self.assertNotIn('bcc', payload)
        self.assertNotIn('attachments', payload)

    def test_html_is_omitted_when_there_is_no_alternative(self):
        message = EmailMessage(
            subject='Plain',
            body='just text',
            from_email='Axiom Force <welcome@example.com>',
            to=['operator@example.com'],
        )

        with mock.patch(
            'urllib.request.urlopen', return_value=_accepted()
        ) as urlopen:
            self.backend.send_messages([message])

        payload = json.loads(urlopen.call_args.args[0].data.decode('utf-8'))
        self.assertNotIn('html', payload)
        self.assertEqual(payload['text'], 'just text')

    def test_extra_headers_from_overrides_from_email(self):
        """Mirrors EmailMessage.message(), which lets extra_headers win."""
        message = EmailMessage(
            subject='Overridden',
            body='body',
            from_email='Axiom Force <welcome@example.com>',
            to=['operator@example.com'],
            headers={'From': 'Someone Else <other@example.com>'},
        )

        with mock.patch(
            'urllib.request.urlopen', return_value=_accepted()
        ) as urlopen:
            self.backend.send_messages([message])

        payload = json.loads(urlopen.call_args.args[0].data.decode('utf-8'))
        self.assertEqual(payload['from'], 'Someone Else <other@example.com>')

    def test_attachments_are_base64_encoded(self):
        message = EmailMessage(
            subject='With attachment',
            body='body',
            from_email='Axiom Force <welcome@example.com>',
            to=['operator@example.com'],
        )
        message.attach('notes.txt', b'hello bytes', 'text/plain')

        with mock.patch(
            'urllib.request.urlopen', return_value=_accepted()
        ) as urlopen:
            self.backend.send_messages([message])

        payload = json.loads(urlopen.call_args.args[0].data.decode('utf-8'))
        self.assertEqual(
            payload['attachments'],
            [
                {
                    'filename': 'notes.txt',
                    'content': 'aGVsbG8gYnl0ZXM=',  # base64('hello bytes')
                    'content_type': 'text/plain',
                }
            ],
        )

    def test_accepted_id_is_returned_and_logged(self):
        with mock.patch('urllib.request.urlopen', return_value=_accepted('abc-9')):
            with self.assertLogs('core.mail_backends', level='INFO') as logs:
                self.backend.send_messages([self._message()])

        self.assertTrue(any('abc-9' in line for line in logs.output))

    def test_multiple_messages_report_the_count_accepted(self):
        with mock.patch('urllib.request.urlopen', return_value=_accepted()) as urlopen:
            sent = self.backend.send_messages([self._message(), self._message()])

        self.assertEqual(sent, 2)
        self.assertEqual(urlopen.call_count, 2)


class ErrorHandlingTest(SimpleTestCase):
    def setUp(self):
        self.backend = ResendEmailBackend(api_key=KEY)
        self.message = EmailMessage(
            subject='SYS_INIT: Welcome',
            body='body',
            from_email='Axiom Force <welcome@example.com>',
            to=['operator@example.com'],
        )

    def test_http_error_becomes_resend_api_error_carrying_the_body(self):
        with mock.patch(
            'urllib.request.urlopen',
            side_effect=_http_error(403, b'{"message":"domain is not verified"}'),
        ):
            with self.assertRaises(ResendAPIError) as ctx:
                self.backend.send_messages([self.message])

        # The decoded Resend body is the only clue about *why* it failed.
        self.assertIn('403', str(ctx.exception))
        self.assertIn('domain is not verified', str(ctx.exception))

    def test_connection_error_becomes_resend_api_error(self):
        with mock.patch(
            'urllib.request.urlopen',
            side_effect=urllib.error.URLError('connection refused'),
        ):
            with self.assertRaises(ResendAPIError):
                self.backend.send_messages([self.message])

    def test_fail_silently_swallows_the_error(self):
        backend = ResendEmailBackend(api_key=KEY, fail_silently=True)

        with mock.patch('urllib.request.urlopen', side_effect=_http_error()):
            sent = backend.send_messages([self.message])

        self.assertEqual(sent, 0)

    def test_a_non_json_success_body_is_tolerated(self):
        """A 200 with an unparseable body still counts as accepted."""
        response = mock.MagicMock()
        response.read.return_value = b'not json at all'
        response.__enter__.return_value = response

        with mock.patch('urllib.request.urlopen', return_value=response):
            sent = self.backend.send_messages([self.message])

        self.assertEqual(sent, 1)


class ConfigurationTest(SimpleTestCase):
    def test_constructing_without_a_key_does_not_raise(self):
        """Load-bearing: migrate, check, and the test suite all build mailers
        with no RESEND_API_KEY present, so __init__ must stay inert."""
        ResendEmailBackend(api_key='')

    def test_an_empty_key_is_respected_rather_than_falling_back(self):
        # api_key='' must not be treated as "not supplied" -- the settings
        # fallback would otherwise resurrect a key the caller deliberately
        # blanked.
        with self.settings(RESEND_API_KEY=KEY):
            self.assertEqual(ResendEmailBackend(api_key='').api_key, '')

    def test_sending_without_a_key_raises(self):
        backend = ResendEmailBackend(api_key='')

        with mock.patch('urllib.request.urlopen') as urlopen:
            with self.assertRaises(ImproperlyConfigured):
                backend.send_messages([self._message()])
        urlopen.assert_not_called()

    def test_an_empty_message_list_returns_zero_without_network(self):
        backend = ResendEmailBackend(api_key=KEY)

        with mock.patch('urllib.request.urlopen') as urlopen:
            self.assertEqual(backend.send_messages([]), 0)

        urlopen.assert_not_called()

    def _message(self):
        return EmailMessage(
            subject='s',
            body='b',
            from_email='Axiom Force <welcome@example.com>',
            to=['operator@example.com'],
        )
