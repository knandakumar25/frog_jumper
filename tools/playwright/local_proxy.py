"""Expose only the isolated test API to the Windows game via a loopback-published container port."""
import http.client
from wsgiref.simple_server import make_server, WSGIRequestHandler


class QuietHandler(WSGIRequestHandler):
    """Keep URLs and transient credential bodies out of transport logs."""
    def log_message(self, format, *args):
        """Test results are reported by the caller, not the proxy."""


def proxy(environ, start_response):
    """Forward API traffic to the test server; fixture and captured-mail routes stay internal."""
    if not environ['PATH_INFO'].startswith('/api/'):
        start_response('404 Not Found', [])
        return [b'']
    conn = http.client.HTTPConnection('backend', 8002, timeout=15)
    body = environ['wsgi.input'].read(int(environ.get('CONTENT_LENGTH') or 0))
    headers = {'Content-Type': environ.get('CONTENT_TYPE', 'application/json')}
    if environ.get('HTTP_AUTHORIZATION'):
        headers['Authorization'] = environ['HTTP_AUTHORIZATION']
    try:
        conn.request(environ['REQUEST_METHOD'], environ['PATH_INFO'], body, headers)
        response = conn.getresponse()
        payload = response.read()
        start_response(f'{response.status} {response.reason}', [('Content-Type', 'application/json'), ('Cache-Control', 'no-store')])
        return [payload]
    finally:
        conn.close()


if __name__ == '__main__':
    with make_server('0.0.0.0', 8002, proxy, handler_class=QuietHandler) as server:
        print('Loopback test API proxy ready', flush=True)
        server.serve_forever()
