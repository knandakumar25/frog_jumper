"""Ephemeral E2E backend: isolated PostgreSQL database, memory-only mail and test-only fixture endpoints.

Run only inside the existing backend container, with PYTHONPATH=/app. Nothing is
added to production URL configuration. Ctrl-C/termination destroys the test DB.
"""
import json
import os
import signal
from wsgiref.simple_server import make_server, WSGIRequestHandler

os.environ.setdefault('DJANGO_SETTINGS_MODULE', 'core.settings')
import django
django.setup()
from django.core import mail
from django.core.wsgi import get_wsgi_application
from django.test.utils import setup_test_environment, setup_databases, teardown_databases
from django.conf import settings
from accounts.models import User


class QuietHandler(WSGIRequestHandler):
    """Suppress URLs, request bodies and credentials from the test transport logs."""
    def log_message(self, format, *args):
        """Only aggregate runner results are written to logs."""


def main():
    """Serve the real API on a container-internal port with disposable fixture/mail access."""
    if not settings.CLERK_SECRET_KEY.startswith('sk_test_'):
        raise RuntimeError('A Clerk development instance is required')
    setup_test_environment()
    # Keep normal Django unit-test database names independent of this long-lived test server.
    settings.DATABASES['default'].setdefault('TEST', {})['NAME'] = 'test_browser_handoff'
    settings.ALLOWED_HOSTS = ['*']
    original = setup_databases(verbosity=0, interactive=False)
    application = get_wsgi_application()

    def wrapped(environ, start_response):
        """Expose fixtures only in this isolated test process; never persist mail tokens."""
        path = environ['PATH_INFO']
        if path == '/__test__/mail':
            data = [{'to': msg.to, 'body': msg.body} for msg in mail.outbox]
        elif path == '/__test__/seed' and environ['REQUEST_METHOD'] == 'POST':
            payload = json.loads(environ['wsgi.input'].read(int(environ.get('CONTENT_LENGTH') or 0)))
            if not payload['email'].endswith('@example.com') or '+clerk_test' not in payload['email']:
                raise ValueError('Only disposable Clerk test addresses are allowed')
            user, _ = User.objects.get_or_create(clerk_user_id=payload['id'], defaults={
                'username': payload['username'], 'email': payload['email'], 'country': 'Japan', 'date_of_birth': '1995-04-12'})
            data = {'username': user.username}
        elif path == '/__test__/ready':
            data = {'ready': True}
        elif path == '/__test__/stop' and environ['REQUEST_METHOD'] == 'POST':
            # Signal after returning the response so the caller can observe cleanup.
            data = {'stopping': True}
            import threading
            threading.Timer(0.2, lambda: os.kill(os.getpid(), signal.SIGTERM)).start()
        else:
            return application(environ, start_response)
        start_response('200 OK', [('Content-Type', 'application/json'), ('Cache-Control', 'no-store')])
        return [json.dumps(data).encode()]

    def stop(signum, frame):
        """Unwind the server and destroy only the disposable test database."""
        raise KeyboardInterrupt()

    signal.signal(signal.SIGTERM, stop)
    try:
        with make_server('0.0.0.0', 8002, wrapped, handler_class=QuietHandler) as server:
            print('Isolated browser test backend ready', flush=True)
            server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        teardown_databases(original, verbosity=0)
        print('Isolated browser test database removed', flush=True)


if __name__ == '__main__':
    main()
