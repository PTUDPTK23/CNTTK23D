"""Build with dotnet build, then run: python tests/integration.py [-v]."""
import json
import os
from pathlib import Path
import re
import socket
import subprocess
import time
import unittest
from urllib.error import HTTPError, URLError
from urllib.parse import urlencode
from urllib.request import Request, urlopen

ROOT = Path(__file__).resolve().parents[1]
DOTNET = os.getenv('DOTNET_EXE', r'C:\Program Files\dotnet\dotnet.exe' if os.name == 'nt' else 'dotnet')


def dll(name):
    return ROOT / 'src' / name / 'bin/Debug/net10.0' / f'{name}.dll'


def unused_url():
    with socket.socket() as sock:
        sock.bind(('127.0.0.1', 0))
        return f'http://127.0.0.1:{sock.getsockname()[1]}'


class Integration(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = unused_url()
        api = subprocess.Popen([DOTNET, str(dll('Submission.RealtimeApi')), '--urls', cls.base,
                                '--EnableFailed=true'],
                               cwd=ROOT, stdout=subprocess.DEVNULL)
        cls.addClassCleanup(api.wait, timeout=5)
        cls.addClassCleanup(api.terminate)
        for _ in range(100):
            if api.poll() is not None:
                raise RuntimeError(f'API exited with {api.returncode}')
            try:
                cls.request('GET', '/')
                return
            except URLError:
                time.sleep(.05)
        raise RuntimeError('API did not start')

    @classmethod
    def request(cls, method, path, body=None):
        headers = {'Content-Type': 'application/json'} if body is not None else {}
        data = json.dumps(body).encode() if body is not None else None
        req = Request(cls.base + path, data=data, method=method, headers=headers)
        try:
            res = urlopen(req, timeout=5)
        except HTTPError as exc:
            res = exc
        with res:
            text = res.read()
            return res.status, res.headers, json.loads(text) if text else None

    def create(self, student, repo='https://example.com/edu'):
        return self.request('POST', '/api/assignments/1/submissions',
                            {'studentCode': student, 'repositoryUrl': repo})

    def client(self, *args, url=None):
        return subprocess.Popen([DOTNET, str(dll('Submission.RealtimeClient')), url or self.base, *args],
                                cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                                encoding='utf-8')

    def test_two_clients_receive_only_their_own_events(self):
        with self.client('SV001') as one, self.client('SV002') as two:
            results = [(proc, proc.communicate(timeout=20)[0]) for proc in (one, two)]
        ids = set()
        for proc, output in results:
            self.assertEqual(proc.returncode, 0, output)
            self.assertIn('Trang thai REST ban dau: received', output)
            self.assertIn('Trang thai doc lai bang REST: graded', output)
            created = re.search(r'Da tao luot nop: ([0-9a-f-]+)', output)
            self.assertIsNotNone(created, output)
            events = re.findall(r'Thong bao SignalR: ([0-9a-f-]+) -> (\w+)', output)
            self.assertEqual(events, [(created[1], 'processing'), (created[1], 'graded')], output)
            ids.add(created[1])
        self.assertEqual(len(ids), 2)

    def test_client_reports_no_api_and_wrong_hub(self):
        for url, args, expected in [(unused_url(), (), r'(?i)refused'),
                                    (self.base, ('--wrong-hub',), r'404')]:
            with self.subTest(expected=expected), self.client(*args, url=url) as proc:
                output = proc.communicate(timeout=10)[0]
                self.assertNotEqual(proc.returncode, 0, output)
                self.assertIn('HttpRequestException', output)
                self.assertRegex(output, expected)
                self.assertNotIn('Da tao luot nop:', output)

    def test_client_falls_back_to_rest_after_missed_events(self):
        for case in ('--wrong-event', '--late-watch'):
            with self.subTest(case=case), self.client(case[2:], case) as proc:
                start = time.monotonic()
                output = proc.communicate(timeout=20)[0]
                self.assertEqual(proc.returncode, 0, output)
                self.assertIn('Qua thoi gian cho thong bao graded.', output)
                self.assertNotIn('Thong bao SignalR:', output)
                self.assertIn('Trang thai doc lai bang REST: graded', output)
                self.assertGreaterEqual(time.monotonic() - start, 14 if case == '--late-watch' else 10)

    def test_bonus_watches_two_submissions_on_one_connection(self):
        with self.client('BONUS', '--bonus') as proc:
            output = proc.communicate(timeout=20)[0]
            self.assertEqual(proc.returncode, 0, output)
        self.assertEqual(output.count('Da ket noi SignalR.'), 1, output)
        first, second = re.findall(r'Da tao luot nop: ([0-9a-f-]+)', output)
        events = re.findall(r'Thong bao SignalR: ([0-9a-f-]+) -> (\w+)', output)
        self.assertNotEqual(first, second)
        self.assertEqual([status for sid, status in events if sid == first], ['processing', 'graded'], output)
        self.assertEqual([status for sid, status in events if sid == second], ['processing', 'failed'], output)
        self.assertIn('CANH BAO', output)
        self.assertIn('Trang thai doc lai bang REST: graded', output)
        self.assertIn('Trang thai doc lai bang REST: failed', output)

    def test_all_nine_requests_in_http_file(self):
        variables = {'host': self.base}
        cases = []
        expected = [200, 200, 201, 200, 400, 404, 404, 200, 404]
        for block in (ROOT / 'requests.http').read_text(encoding='utf-8').split('###'):
            for key, value in re.findall(r'^@(\w+) = (.+)$', block, re.M):
                variables.setdefault(key, value)
            match = re.search(r'^(GET|POST) (\S+)\n+(.*)$', block + '\n', re.M | re.S)
            if match:
                cases.append(match.groups())
        self.assertEqual(len(cases), len(expected))
        for index, ((method, url, rest), wanted) in enumerate(zip(cases, expected), 1):
            with self.subTest(case=index):
                def expand(text):
                    return re.sub(r'\{\{([^\}]+)\}\}', lambda m: variables[m[1]], text)
                body = rest[rest.index('{'):] if '{' in rest else ''
                actual, headers, payload = self.request(method, expand(url).removeprefix(self.base),
                                                       json.loads(expand(body)) if body else None)
                self.assertEqual(actual, wanted)
                if index == 3:
                    self.assertEqual(payload['status'], 'received')
                    variables['createSubmission.response.body.$.id'] = payload['id']
                    self.assertEqual(headers['Location'], '/api/submissions/' + payload['id'])
                if index == 8:
                    self.assertTrue(payload['connectionId'])

    def test_rest_progresses_and_timestamps_increase(self):
        status, headers, created = self.create('REST-flow')
        self.assertEqual((status, created['status']), (201, 'received'))
        location = headers['Location']
        self.assertEqual(location, '/api/submissions/' + created['id'])
        self.assertEqual(created.get('updatedAt'), created['receivedAt'], 'POST must initialize UpdatedAt')
        self.assertEqual(self.request('GET', location)[2]['status'], 'received')
        time.sleep(1.25)
        processing = self.request('GET', location)[2]
        self.assertEqual(processing['status'], 'processing')
        time.sleep(2)
        graded = self.request('GET', location)[2]
        self.assertEqual(graded['status'], 'graded')
        from datetime import datetime
        parse = datetime.fromisoformat
        self.assertLess(parse(created['updatedAt']), parse(processing['updatedAt']))
        self.assertLess(parse(processing['updatedAt']), parse(graded['updatedAt']))

    def test_hub_negotiates_only_at_correct_route(self):
        status, _, body = self.request('POST', '/hubs/submissions/negotiate?negotiateVersion=1')
        self.assertEqual(status, 200)
        self.assertTrue(body['connectionId'])
        self.assertEqual(self.request('POST', '/hub/submissions/negotiate?negotiateVersion=1')[0], 404)

    def test_hub_pushes_only_watched_submission_after_persisting(self):
        hub = '/hubs/submissions'
        _, _, negotiated = self.request('POST', hub + '/negotiate?negotiateVersion=1')
        url = self.base + hub + '?' + urlencode({'id': negotiated['connectionToken']})

        def poll():
            with urlopen(url, timeout=5) as res:
                return [json.loads(frame) for frame in res.read().split(b'\x1e') if frame]

        def send(frame):
            with urlopen(Request(url, data=json.dumps(frame).encode() + b'\x1e'), timeout=5):
                pass

        self.assertEqual(poll(), [])  # Open Long Polling transport before sending handshake.
        self.addCleanup(lambda: urlopen(Request(url, method='DELETE'), timeout=5).close())
        send({'protocol': 'json', 'version': 1})
        self.assertEqual(poll(), [{}])
        self.create('Not-watched')  # Its changes must not reach this connection.
        _, headers, created = self.create('Watched')
        send({'type': 1, 'invocationId': '1', 'target': 'WatchSubmission', 'arguments': [created['id']]})
        events = []
        deadline = time.monotonic() + 5
        while len(events) < 2 and time.monotonic() < deadline:
            for frame in poll():
                if frame.get('type') == 3:
                    self.assertNotIn('error', frame)
                if frame.get('type') != 1:
                    continue
                self.assertEqual(frame['target'], 'SubmissionStatusChanged')
                event, = frame['arguments']
                self.assertEqual(set(event), {'submissionId', 'status', 'updatedAt'})
                self.assertEqual(event['submissionId'], created['id'])
                stored = self.request('GET', headers['Location'])[2]
                self.assertEqual((event['status'], event['updatedAt']), (stored['status'], stored['updatedAt']))
                events.append(event['status'])
        self.assertEqual(events, ['processing', 'graded'])

    def test_bai2_contracts_including_delete_and_duplicate(self):
        self.assertEqual(self.request('GET', '/')[0], 200)
        self.assertEqual(self.request('GET', '/api/assignments')[0], 200)
        self.assertEqual(self.request('GET', '/api/assignments/1')[0], 200)
        self.assertEqual(self.request('GET', '/api/assignments/99')[0], 404)
        for assignment, body, expected, code in [
            (1, {}, 400, 'INVALID_SUBMISSION'),
            (99, {}, 404, 'ASSIGNMENT_NOT_FOUND'),
            (999, {}, 404, 'ASSIGNMENT_NOT_FOUND'),
            (3, {}, 400, 'ASSIGNMENT_CLOSED'),
        ]:
            status, _, error = self.request('POST', f'/api/assignments/{assignment}/submissions', body)
            self.assertEqual((status, error['code']), (expected, code))
        self.assertEqual(self.request('GET', '/api/submissions/00000000-0000-0000-0000-000000000000')[0], 404)
        status, headers, created = self.create(' Contract ')
        self.assertEqual((status, created['studentCode']), (201, 'Contract'))
        status, _, error = self.create('contract')
        self.assertEqual((status, error['code']), (409, 'DUPLICATE_SUBMISSION'))
        location = headers['Location']
        self.assertEqual(self.request('DELETE', location)[0], 204)
        self.assertEqual(self.request('GET', location)[0], 404)
        self.assertEqual(self.request('DELETE', location)[0], 404)
        self.assertEqual(self.create('contract')[0], 201)


if __name__ == '__main__':
    unittest.main()
