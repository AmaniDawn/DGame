"""Deployment and project-configuration boundaries for the official Luban tools."""

import contextlib
import importlib.util
import io
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import tomllib
import unittest
from unittest.mock import patch


REPO = Path(__file__).resolve().parents[2]


def load(name):
    path = REPO / ".agents/skills/luban-dev/scripts" / f"{name}.py"
    spec = importlib.util.spec_from_file_location(f"test_{name}", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class LubanAiTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.launcher = load("luban_ai")
        self.installer = load("install_ai_tools")

    def test_mcp_configuration_preserves_settings_and_repeats_without_changes(self):
        directory = self.root / ".codex"
        directory.mkdir()
        config = directory / "config.toml"
        config.write_text('model = "existing-model"\n# keep this comment\n', encoding="utf-8")
        with patch.object(self.launcher, "REPO", self.root), contextlib.redirect_stdout(io.StringIO()):
            self.launcher.configure_mcp()
            first = config.read_bytes()
            self.launcher.configure_mcp()
        self.assertEqual(first, config.read_bytes())
        data = tomllib.loads(first.decode("utf-8"))
        self.assertEqual(data["model"], "existing-model")
        self.assertIn("# keep this comment", first.decode("utf-8"))
        server = data["mcp_servers"]["luban"]
        self.assertEqual(server["command"], "python")
        self.assertEqual(set(server["enabled_tools"]), {"list_tables", "get_schema", "describe", "validate"})
        self.assertEqual(server["args"], [".agents/skills/luban-dev/scripts/luban_ai.py", "mcp"])

    def test_existing_different_mcp_server_is_not_overwritten(self):
        directory = self.root / ".codex"
        directory.mkdir()
        config = directory / "config.toml"
        original = b'[mcp_servers.luban]\ncommand = "custom-server"\n'
        config.write_bytes(original)
        with patch.object(self.launcher, "REPO", self.root), self.assertRaises(ValueError):
            self.launcher.configure_mcp()
        self.assertEqual(config.read_bytes(), original)

    def test_queries_bind_this_checkout_instead_of_the_callers_directory(self):
        with patch.object(self.launcher, "REPO", self.root), \
                patch.object(self.launcher, "check_install"), \
                patch.object(sys, "argv", ["luban_ai.py", "validate", "--target", "server"]), \
                patch.object(self.launcher.subprocess, "call", return_value=0) as call:
            self.assertEqual(self.launcher.main(), 0)
        args, kwargs = call.call_args
        command = args[0]
        self.assertEqual(command[command.index("--conf") + 1], str(self.root / "GameConfig/luban.conf"))
        self.assertEqual(command[command.index("-t") + 1], "server")
        self.assertEqual(kwargs["cwd"], self.root)

    def test_generator_switch_preserves_the_old_directory_and_is_idempotent(self):
        tools = self.root / "LubanTools"
        incoming = tools / "AI/Luban"
        incoming.mkdir(parents=True)
        (incoming / "Luban.dll").write_bytes(b"new-generator")
        target = tools / "Luban"
        target.mkdir()
        (target / "Luban.dll").write_bytes(b"old-generator")
        (target / "custom-file.txt").write_text("preserve me", encoding="utf-8")
        with patch.object(self.installer, "DESTINATION", tools / "AI"), \
                patch.object(self.installer.subprocess, "run", side_effect=[
                    type("Result", (), {"stdout": "Luban 4.5.0", "stderr": ""})(),
                    type("Result", (), {"stdout": "Luban 5.1.0", "stderr": ""})(),
                    type("Result", (), {"stdout": "Luban 5.1.0", "stderr": ""})(),
                    type("Result", (), {"stdout": "Luban 5.1.0", "stderr": ""})(),
                ]), \
                contextlib.redirect_stdout(io.StringIO()):
            self.installer.activate_generator()
            self.installer.activate_generator()
        backups = list((tools / "Backups").iterdir())
        self.assertEqual(len(backups), 1)
        self.assertEqual((backups[0] / "Luban.dll").read_bytes(), b"old-generator")
        self.assertEqual((backups[0] / "custom-file.txt").read_text(encoding="utf-8"), "preserve me")
        self.assertEqual((target / "Luban.dll").read_bytes(), b"new-generator")

    def test_mcp_uses_the_official_stderr_logger_and_matching_tool_paths(self):
        tools = self.root / "AI"
        with patch.object(self.launcher, "TOOLS", tools), \
                patch.object(self.launcher, "check_install"), \
                patch.object(sys, "argv", ["luban_ai.py", "mcp"]), \
                patch.object(self.launcher.subprocess, "call", return_value=0) as call:
            self.assertEqual(self.launcher.main(), 0)
        _, kwargs = call.call_args
        self.assertEqual(kwargs["cwd"], tools / "Luban.Agent")
        self.assertEqual(kwargs["env"]["LUBAN_AGENT_DLL"], str(tools / "Luban.Agent/Luban.Agent.dll"))
        self.assertEqual(kwargs["env"]["LUBAN_DLL"], str(tools / "Luban/Luban.dll"))

    def test_build_failure_does_not_install_or_modify_the_active_generator(self):
        source = self.root / "source"
        for name in self.installer.TOOLS:
            project = source / f"src/{name}/{name}.csproj"
            project.parent.mkdir(parents=True)
            project.touch()
        tools = self.root / "LubanTools"
        old = tools / "Luban/Luban.dll"
        old.parent.mkdir(parents=True)
        old.write_bytes(b"active-generator")
        with patch.object(self.installer, "DESTINATION", tools / "AI"), \
                patch.object(self.installer, "verify_source"), \
                patch.object(self.installer, "run", side_effect=subprocess.CalledProcessError(1, "dotnet")), \
                self.assertRaises(subprocess.CalledProcessError):
            self.installer.install(source, {"commit": "pinned"})
        self.assertFalse((tools / "AI").exists())
        self.assertEqual(old.read_bytes(), b"active-generator")

    def test_a_corrupt_installation_is_not_silently_accepted(self):
        destination = self.root / "AI"
        destination.mkdir()
        lock = {"commit": "pinned", "version": "test"}
        manifest = {**lock}
        for name in self.installer.TOOLS:
            file = destination / f"{name}/{name}.dll"
            file.parent.mkdir()
            file.write_bytes(b"original")
        (destination / "install.json").write_text(json.dumps(manifest), encoding="utf-8")
        changed = destination / "Luban/Luban.dll"
        changed.write_bytes(b"corrupt dll")
        with patch.object(self.installer, "DESTINATION", destination), self.assertRaises(FileExistsError):
            self.installer.already_installed(lock)
        self.assertEqual(changed.read_bytes(), b"corrupt dll")


if __name__ == "__main__":
    unittest.main()
