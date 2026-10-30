"""Invoke the configured local Godot AI MCP using its supported handshake revision.

Uses only Python's standard library; reads the private capability without printing it.
Pass a tool name and JSON arguments, or `list` to inspect callable tool schemas.
"""
import argparse
import http.client
import json
import os
from pathlib import Path


class GodotMcp:
    """Short-lived authenticated MCP session pinned to protocol 2025-11-25."""
    def __init__(self):
        """Load the configured local capability and initialize an MCP session."""
        record = Path(os.environ['LOCALAPPDATA']) / 'godot-ai/capabilities/http-8001.json'
        self.headers = {'Authorization': 'Bearer ' + json.loads(record.read_text())['http'],
                        'Content-Type': 'application/json', 'Accept': 'application/json, text/event-stream',
                        'MCP-Protocol-Version': '2025-11-25'}
        self.request('initialize', {'protocolVersion': '2025-11-25', 'capabilities': {},
                                   'clientInfo': {'name': 'cinematic-qa', 'version': '1'}})
        self.request('notifications/initialized', {}, notification=True)

    def request(self, method, params, notification=False):
        """Send one JSON-RPC request and decode its JSON or event-stream response."""
        conn = http.client.HTTPConnection('127.0.0.1', 8001, timeout=120)
        payload = {'jsonrpc': '2.0', 'method': method, 'params': params}
        if not notification:
            payload['id'] = 1
        conn.request('POST', '/mcp', json.dumps(payload), self.headers)
        response = conn.getresponse()
        session = response.getheader('mcp-session-id')
        if session:
            self.headers['Mcp-Session-Id'] = session
        if notification and response.status == 202:
            conn.close()
            return {}
        raw = response.read().decode('utf-8')
        conn.close()
        if response.status >= 400:
            raise RuntimeError(f'MCP {response.status}: {raw}')
        if not raw:
            return {}
        for line in raw.splitlines():
            if line.startswith('data: '):
                return json.loads(line[6:])
        return json.loads(raw)

    def close(self):
        """Release the short-lived MCP session so repeated QA does not exhaust its limit."""
        conn = http.client.HTTPConnection('127.0.0.1', 8001, timeout=10)
        conn.request('DELETE', '/mcp', headers=self.headers)
        response = conn.getresponse()
        # This server closes bodyless DELETE replies without a terminal chunk.
        # The successful status is sufficient; consuming that absent body raises.
        if response.status >= 400:
            raise RuntimeError(f'MCP session close failed: {response.status}')
        conn.close()

    def call(self, name, arguments):
        """Invoke one tool; callers must verify editor identity before mutations."""
        return self.request('tools/call', {'name': name, 'arguments': arguments})


def main():
    """Invoke the requested tool and print its reviewable response."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('tool', nargs='?', default='session_manage')
    parser.add_argument('arguments', nargs='?', default='{}')
    args = parser.parse_args()
    client = GodotMcp()
    result = client.request('tools/list', {}) if args.tool == 'list' else client.call(args.tool, (json.loads(args.arguments) or ({'op':'list'} if args.tool == 'session_manage' else {})))
    print(json.dumps(result, ensure_ascii=True))
    client.close()


if __name__ == '__main__':
    main()
