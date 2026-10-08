"""Transport-level tests for accounts.clerk_client.

`urllib.request.urlopen` is patched rather than the client's own helpers, so
what gets asserted is the request that would actually go on the wire: URL,
Authorization, User-Agent, timeout. No test here reaches Clerk.
"""

import io
import json
import urllib.error
from unittest import mock

from django.test import SimpleTestCase

from accounts import clerk_client
from accounts.clerk_client import (
    ClerkAPIError,
    ClerkUnavailable,
    ClerkUserNotFound,
)

KEY = 'sk_test_key_not_real'
USER_ID = 'user_2abcDEF123'
API_URL = f'https://api.clerk.com/v1/users/{USER_ID}'


def _response(payload):
    """Stand-in for what `urlopen(...)` yields as a context manager."""
    response = mock.MagicMock()
    response.read.return_value = json.dumps(payload).encode('utf-8')
    # urlopen's result is used via `with ... as response`, and MagicMock's
    # default __enter__ returns a *different* mock -- point it at this one.
    response.__enter__.return_value = response
    return response


def _http_error(code=404, body=b'{"errors":[{"message":"not found"}]}'):
    return urllib.error.HTTPError(
        url=API_URL, code=code, msg='Error', hdrs=None, fp=io.BytesIO(body)
    )


def _payload(**overrides):
    payload = {
        'id': USER_ID,
        'primary_email_address_id': 'idn_1',
        'username': 'kaito',
        'first_name': 'Kaito',
        'last_name': 'Kurogane',
        'email_addresses': [
            {
                'id': 'idn_1',
                'email_address': 'kaito@example.com',
                'verification': {'status': 'verified'},
            }
        ],
    }
    payload.update(overrides)
    return payload


class RequestShapeTest(SimpleTestCase):
    def test_request_url_headers_and_timeout(self):
        with self.settings(CLERK_SECRET_KEY=KEY, CLERK_TIMEOUT=7):
            with mock.patch(
                'urllib.request.urlopen', return_value=_response(_payload())
            ) as urlopen:
                clerk_client.fetch_clerk_user(USER_ID)

        request = urlopen.call_args.args[0]
        self.assertEqual(request.full_url, API_URL)
        self.assertEqual(request.method, 'GET')
        self.assertEqual(request.get_header('Authorization'), f'Bearer {KEY}')
        self.assertEqual(urlopen.call_args.kwargs['timeout'], 7)
        # Load-bearing: Cloudflare in front of api.clerk.com answers urllib's
        # default User-Agent with 403 "error code: 1010" before Clerk sees the
        # request, which reads like a bad API key and is not.
        self.assertEqual(request.get_header('User-agent'), clerk_client.USER_AGENT)
        self.assertNotIn('python-urllib', request.get_header('User-agent').lower())


class EmailResolutionTest(SimpleTestCase):
    def _fetch(self, payload):
        with self.settings(CLERK_SECRET_KEY=KEY):
            with mock.patch('urllib.request.urlopen', return_value=_response(payload)):
                return clerk_client.fetch_clerk_user(USER_ID)

    def test_the_primary_address_wins(self):
        user = self._fetch(_payload())
        self.assertEqual(user.email, 'kaito@example.com')
        self.assertTrue(user.email_verified)
        self.assertEqual(user.username, 'kaito')
        self.assertEqual(user.first_name, 'Kaito')

    def test_falls_back_to_a_verified_address_when_there_is_no_primary(self):
        user = self._fetch(
            _payload(
                primary_email_address_id=None,
                email_addresses=[
                    {
                        'id': 'idn_1',
                        'email_address': 'old@example.com',
                        'verification': {'status': 'unverified'},
                    },
                    {
                        'id': 'idn_2',
                        'email_address': 'new@example.com',
                        'verification': {'status': 'verified'},
                    },
                ],
            )
        )
        self.assertEqual(user.email, 'new@example.com')
        self.assertTrue(user.email_verified)

    def test_falls_back_to_the_first_address_when_none_are_verified(self):
        user = self._fetch(
            _payload(
                primary_email_address_id=None,
                email_addresses=[
                    {
                        'id': 'idn_1',
                        'email_address': 'first@example.com',
                        'verification': {'status': 'unverified'},
                    },
                    {
                        'id': 'idn_2',
                        'email_address': 'second@example.com',
                        'verification': {'status': 'unverified'},
                    },
                ],
            )
        )
        self.assertEqual(user.email, 'first@example.com')
        self.assertFalse(user.email_verified)

    def test_an_unverified_primary_is_reported_as_unverified(self):
        user = self._fetch(
            _payload(
                email_addresses=[
                    {
                        'id': 'idn_1',
                        'email_address': 'kaito@example.com',
                        'verification': {'status': 'unverified'},
                    }
                ]
            )
        )
        self.assertFalse(user.email_verified)

    def test_a_missing_verification_block_counts_as_unverified(self):
        user = self._fetch(
            _payload(email_addresses=[{'id': 'idn_1', 'email_address': 'kaito@example.com'}])
        )
        self.assertFalse(user.email_verified)

    def test_nullable_profile_fields_default_to_empty_strings(self):
        user = self._fetch(_payload(username=None, first_name=None, last_name=None))
        self.assertEqual(user.username, '')
        self.assertEqual(user.first_name, '')
        self.assertEqual(user.last_name, '')

    def test_no_addresses_at_all_yields_an_empty_email(self):
        user = self._fetch(_payload(email_addresses=[], primary_email_address_id=None))
        self.assertEqual(user.email, '')
        self.assertFalse(user.email_verified)


class ErrorMappingTest(SimpleTestCase):
    def test_404_becomes_user_not_found(self):
        with self.settings(CLERK_SECRET_KEY=KEY):
            with mock.patch('urllib.request.urlopen', side_effect=_http_error(404)):
                with self.assertRaises(ClerkUserNotFound):
                    clerk_client.fetch_clerk_user(USER_ID)

    def test_other_http_errors_do_not_echo_the_body(self):
        with self.settings(CLERK_SECRET_KEY=KEY):
            with mock.patch(
                'urllib.request.urlopen',
                side_effect=_http_error(500, b'{"detail":"leak-me"}'),
            ):
                with self.assertRaises(ClerkAPIError) as ctx:
                    clerk_client.fetch_clerk_user(USER_ID)

        # The body is logged, never returned: with DEBUG=True the response is
        # rendered into the browser.
        self.assertNotIn('leak-me', str(ctx.exception))
        self.assertIn('500', str(ctx.exception))

    def test_a_key_is_never_put_in_an_exception_message(self):
        with self.settings(CLERK_SECRET_KEY=KEY):
            with mock.patch('urllib.request.urlopen', side_effect=_http_error(500)):
                with self.assertRaises(ClerkAPIError) as ctx:
                    clerk_client.fetch_clerk_user(USER_ID)
        self.assertNotIn(KEY, str(ctx.exception))

    def test_a_connection_error_becomes_unavailable(self):
        with self.settings(CLERK_SECRET_KEY=KEY):
            with mock.patch(
                'urllib.request.urlopen',
                side_effect=urllib.error.URLError('certificate verify failed'),
            ):
                with self.assertRaises(ClerkUnavailable):
                    clerk_client.fetch_clerk_user(USER_ID)

    def test_a_bare_timeout_becomes_unavailable(self):
        with self.settings(CLERK_SECRET_KEY=KEY):
            with mock.patch(
                'urllib.request.urlopen', side_effect=TimeoutError('timed out')
            ):
                with self.assertRaises(ClerkUnavailable):
                    clerk_client.fetch_clerk_user(USER_ID)

    def test_a_non_json_body_becomes_an_api_error(self):
        response = mock.MagicMock()
        response.read.return_value = b'<html>gateway error</html>'
        response.__enter__.return_value = response
        with self.settings(CLERK_SECRET_KEY=KEY):
            with mock.patch('urllib.request.urlopen', return_value=response):
                with self.assertRaises(ClerkAPIError):
                    clerk_client.fetch_clerk_user(USER_ID)

    def test_a_json_list_body_becomes_an_api_error(self):
        with self.settings(CLERK_SECRET_KEY=KEY):
            with mock.patch('urllib.request.urlopen', return_value=_response([1, 2])):
                with self.assertRaises(ClerkAPIError):
                    clerk_client.fetch_clerk_user(USER_ID)


class ConfigurationTest(SimpleTestCase):
    def test_a_missing_key_raises_without_any_network_call(self):
        with self.settings(CLERK_SECRET_KEY=''):
            with mock.patch('urllib.request.urlopen') as urlopen:
                with self.assertRaises(ClerkAPIError):
                    clerk_client.fetch_clerk_user(USER_ID)
        urlopen.assert_not_called()

    def test_a_non_numeric_timeout_falls_back_rather_than_raising(self):
        with self.settings(CLERK_SECRET_KEY=KEY, CLERK_TIMEOUT='not-a-number'):
            with mock.patch(
                'urllib.request.urlopen', return_value=_response(_payload())
            ) as urlopen:
                clerk_client.fetch_clerk_user(USER_ID)
        self.assertEqual(urlopen.call_args.kwargs['timeout'], 10.0)


class UserIdValidationTest(SimpleTestCase):
    BAD_IDS = [
        '',
        'user_',
        'user_a/../../x',
        'user_a?x=1',
        'user_' + 'a' * 61,  # over the 60-char cap
        'notuser_abc',
        'user_a b',
        None,
        42,
    ]

    def test_malformed_ids_never_reach_the_network(self):
        with self.settings(CLERK_SECRET_KEY=KEY):
            with mock.patch('urllib.request.urlopen') as urlopen:
                for bad in self.BAD_IDS:
                    with self.assertRaises(ClerkUserNotFound):
                        clerk_client.fetch_clerk_user(bad)
        urlopen.assert_not_called()

    def test_the_same_ids_are_rejected_by_the_predicate(self):
        for bad in self.BAD_IDS:
            self.assertFalse(clerk_client.is_valid_user_id(bad))

    def test_a_well_formed_id_is_accepted(self):
        self.assertTrue(clerk_client.is_valid_user_id(USER_ID))
