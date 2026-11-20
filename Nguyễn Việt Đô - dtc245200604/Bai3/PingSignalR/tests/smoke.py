"""Run with python3 tests/smoke.py; requires the .NET 10 SDK."""
import os
from pathlib import Path
import queue
import socket
import subprocess
import tempfile
import threading
import time
import unittest
from urllib.request import urlopen

ROOT = Path(__file__).resolve().parents[1]
DOTNET = os.getenv("DOTNET_EXE", "dotnet")
PORT = 15200
URL = f"http://127.0.0.1:{PORT}"
GROUP = "lop-hoc-01"


class Client:
    def __init__(self, dll, *args):
        self.process = subprocess.Popen(
            [DOTNET, str(dll), *args], stdin=subprocess.PIPE,
            stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
            text=True, encoding="utf-8", env={**os.environ, "PING_BASE_URL": URL},
        )
        self.lines = queue.Queue()
        self.output = []
        threading.Thread(target=self.read, daemon=True).start()

    def read(self):
        for line in self.process.stdout:
            self.lines.put(line.strip())
        self.lines.put(None)

    def send(self, line):
        self.process.stdin.write(line + "\n")
        self.process.stdin.flush()

    def expect(self, fragment, timeout=20):
        deadline = time.monotonic() + timeout
        while True:
            try:
                line = self.lines.get(timeout=max(0, deadline - time.monotonic()))
            except queue.Empty:
                line = None
            if line is None:
                raise AssertionError(f"Missing {fragment!r}; output: {self.output!r}")
            self.output.append(line)
            if fragment in line:
                return line

    def finish(self):
        status = self.process.wait(timeout=10)
        while (line := self.lines.get(timeout=5)) is not None:
            self.output.append(line)
        return status, "\n".join(self.output)

    def close(self):
        if self.process.poll() is None:
            self.process.kill()
            self.process.wait(timeout=10)
        self.process.stdin.close()
        self.process.stdout.close()


class SignalRSmoke(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        scratch = os.getenv("TMPDIR")
        artifacts = tempfile.TemporaryDirectory(prefix="ping-smoke-", dir=scratch)
        cls.addClassCleanup(artifacts.cleanup)
        subprocess.run([DOTNET, "build", "PingSignalR.slnx", "--artifacts-path",
                        artifacts.name, "--nologo"], cwd=ROOT, check=True)
        cls.bin = Path(artifacts.name) / "bin"

    def start_server(self):
        with socket.socket() as probe:
            probe.bind(("127.0.0.1", PORT))
        server = subprocess.Popen(
            [DOTNET, str(self.bin / "Ping.Api/debug/Ping.Api.dll"), "--urls", URL],
            cwd=ROOT / "src/Ping.Api", stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT,
        )
        self.addCleanup(self.stop_server, server)
        deadline = time.monotonic() + 10
        while time.monotonic() < deadline and server.poll() is None:
            try:
                with urlopen(URL, timeout=0.5) as response:
                    if response.status == 200:
                        return server
            except OSError:
                time.sleep(0.05)
        self.fail(f"Test API did not start at {URL}")

    @staticmethod
    def stop_server(server):
        if server.poll() is None:
            server.kill()
            server.wait(timeout=10)

    def client(self, *args):
        client = Client(self.bin / "Ping.Client/debug/Ping.Client.dll", *args)
        self.addCleanup(client.close)
        return client

    def test_command_line_url_and_name(self):
        self.start_server()
        client = self.client(URL + "/", "Nguyễn Việt Đô")
        client.expect("[EchoResponse] Echo: Xin chao SignalR tu Nguyễn Việt Đô")

    def test_broadcast_reaches_every_client(self):
        self.start_server()
        a = self.client("Alice")
        a.expect("[EchoResponse]")
        b = self.client("Bob")
        b.expect("[EchoResponse]")
        a.send("broadcast public message")
        for client in (a, b):
            client.expect("[BroadcastMessage] Alice: public message")

    def test_group_excludes_nonmembers(self):
        self.start_server()
        a = self.client("Alice")
        a.expect("[EchoResponse]")
        b = self.client("Bob")
        b.expect("[EchoResponse]")
        a.send("join")
        a.expect(f"Đã vào nhóm {GROUP}")
        a.send("group private message")
        a.expect(f"[GroupMessage] [{GROUP}] Alice: private message")
        a.send("broadcast after group")
        b.expect("[BroadcastMessage] Alice: after group")
        self.assertFalse(any("[GroupMessage]" in line for line in b.output), b.output)
        b.send("join")
        b.expect(f"Đã vào nhóm {GROUP}")
        a.send("group both members")
        b.expect(f"[GroupMessage] [{GROUP}] Alice: both members")

    def test_system_messages_announce_connections(self):
        self.start_server()
        a = self.client("Alice")
        a.expect("[SystemMessage] Đã kết nối:")
        b = self.client("Bob")
        announcement = b.expect("[SystemMessage] Đã kết nối:")
        b_id = announcement.rsplit(" ", 1)[-1]
        a.expect(announcement)
        b.send("")
        a.expect(f"[SystemMessage] Đã ngắt kết nối: {b_id}")

    def test_restart_rejoins_group_with_new_id(self):
        server = self.start_server()
        a = self.client("Alice")
        initial = a.expect("Đã kết nối:").rsplit(" ", 1)[-1]
        a.expect("[EchoResponse]")
        a.send("join")
        a.expect(f"Đã vào nhóm {GROUP}")
        self.stop_server(server)
        a.expect(f"[Reconnecting] ID cũ: {initial}", timeout=5)
        self.start_server()
        reconnected = a.expect("[Reconnected] ID mới:").rsplit(" ", 1)[-1]
        self.assertNotEqual(initial, reconnected)
        a.expect(f"Đã vào lại nhóm {GROUP}")
        a.send("group after restart")
        a.expect(f"[GroupMessage] [{GROUP}] Alice: after restart")

    def test_closed_after_retries_are_exhausted(self):
        server = self.start_server()
        a = self.client("Alice")
        a.expect("[EchoResponse]")
        self.stop_server(server)
        a.expect("[Reconnecting]", timeout=5)
        a.expect("[Closed]", timeout=60)

    def test_no_server_reports_connection_refused(self):
        with socket.socket() as probe:
            probe.bind(("127.0.0.1", PORT))
        client = self.client()
        status, output = client.finish()
        self.assertEqual(1, status, output)
        self.assertIn("[Kết nối thất bại] Connection refused", output)

    def test_wrong_hub_reports_http_404(self):
        self.start_server()
        client = self.client("--wrong-hub")
        client.process.stdin.close()
        status, output = client.finish()
        self.assertEqual(1, status, output)
        self.assertIn(f"{URL}/hub/ping", output)
        self.assertIn("[Kết nối thất bại] HTTP 404", output)

    def test_typo_handler_does_not_print_echo(self):
        self.start_server()
        client = self.client("Alice", "--typo-handler")
        client.expect("[Echo] Máy chủ đã xử lý lời gọi.")
        client.send("broadcast still connected")
        client.expect("[BroadcastMessage] Alice: still connected")
        client.send("")
        status, output = client.finish()
        self.assertEqual(0, status, output)
        self.assertNotIn("[EchoResponse]", output)

    def test_echo_is_private(self):
        self.start_server()
        a = self.client()
        a.expect("[EchoResponse] Echo: Xin chao SignalR tu Nguyễn Việt Đô")
        b = self.client("Bob")
        b.expect("[EchoResponse] Echo: Xin chao SignalR tu Bob")
        for client, other_name in ((a, "Bob"), (b, "Nguyễn Việt Đô")):
            client.send("")
            status, output = client.finish()
            self.assertEqual(0, status, output)
            self.assertNotIn(f"[EchoResponse] Echo: Xin chao SignalR tu {other_name}", output)


if __name__ == "__main__":
    unittest.main(verbosity=2)
