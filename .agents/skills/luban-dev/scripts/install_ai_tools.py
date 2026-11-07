#!/usr/bin/env python3
"""Build the pinned official Luban AI tools inside this project."""

import argparse
import json
from pathlib import Path
import shutil
import subprocess
import tempfile
import uuid


REPO = Path(__file__).resolve().parents[4]
LOCK = Path(__file__).with_name("ai-tools.lock.json")
DESTINATION = REPO / "GameConfig/Tools/LubanTools/AI"
TOOLS = ("Luban", "Luban.Agent", "Luban.Mcp")


def run(args, cwd=None):
    subprocess.run([str(arg) for arg in args], cwd=cwd, check=True)


def verify_source(source, expected):
    actual = subprocess.check_output(
        ["git", "-C", str(source), "rev-parse", "HEAD"], text=True
    ).strip()
    if actual != expected:
        raise ValueError(f"Expected Luban commit {expected}, found {actual}")
    run(["git", "-C", source, "diff", "--exit-code", "HEAD", "--", "src", "ai"])


def installed_tools_usable(lock):
    """Check presence and basic runtime health without comparing file hashes."""
    paths = {name: DESTINATION / name / f"{name}.dll" for name in TOOLS}
    if any(not path.is_file() or path.stat().st_size == 0 for path in paths.values()):
        return False
    if any(path.read_bytes()[:2] != b"MZ" for path in paths.values()):
        return False

    agent = subprocess.run(
        ["dotnet", paths["Luban.Agent"], "capabilities"],
        capture_output=True, text=True, encoding="utf-8", timeout=30,
    )
    if agent.returncode != 0:
        return False
    try:
        if not json.loads(agent.stdout).get("ok"):
            return False
    except (ValueError, TypeError):
        return False

    generator = subprocess.run(
        ["dotnet", paths["Luban"], "--version"],
        capture_output=True, text=True, encoding="utf-8", timeout=30,
    )
    version_output = generator.stdout + generator.stderr
    return lock["version"] in version_output and lock["commit"] in version_output


def already_installed(lock):
    if not DESTINATION.exists():
        return False
    manifest_path = DESTINATION / "install.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8")) if manifest_path.is_file() else {}
    if all(manifest.get(key) == value for key, value in lock.items()) and installed_tools_usable(lock):
        print(f"Luban AI {lock['version']} is already installed at {DESTINATION}")
        return True
    raise FileExistsError(f"Inspect the existing tool installation before replacing it: {DESTINATION}")


def install(source, lock):
    if already_installed(lock):
        return
    verify_source(source, lock["commit"])
    for name in TOOLS:
        if not (source / f"src/{name}/{name}.csproj").is_file():
            raise ValueError(f"Missing source project: {name}")
    DESTINATION.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="luban-ai-build-") as temp:
        stage = Path(temp)
        for name in TOOLS:
            run(["dotnet", "build", source / f"src/{name}/{name}.csproj",
                 "-c", "Release", "-o", stage / name, "--nologo", "-v:q"], cwd=source)
        capability = subprocess.run(
            ["dotnet", str(stage / "Luban.Agent/Luban.Agent.dll"), "capabilities"],
            capture_output=True, text=True, encoding="utf-8", check=True, cwd=source,
        )
        if not json.loads(capability.stdout).get("ok"):
            raise ValueError("Luban.Agent capability probe failed")
        shutil.copy2(source / "LICENSE", stage / "LICENSE")
        manifest = {
            **lock,
        }
        (stage / "install.json").write_text(
            json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
        )
        # A successful build is staged before installing this directory.
        # The existing production generator at ../Luban is outside this target.
        if DESTINATION.exists():
            raise FileExistsError(f"Tools already exist at {DESTINATION}; inspect them before reinstalling")
        shutil.copytree(stage, DESTINATION)
    print(f"Installed Luban AI {lock['version']} at {DESTINATION}")


def activate_generator():
    """Switch the existing export scripts to the matching generator, preserving the old tool."""
    parent = DESTINATION.parent.resolve()
    target = parent / "Luban"
    staged = parent / ("Luban-staged-" + uuid.uuid4().hex)
    backup = parent / "Backups" / ("Luban-" + uuid.uuid4().hex)
    for path in (target, staged, backup):
        if not path.resolve().is_relative_to(parent):
            raise ValueError(f"Generator path escaped the tool directory: {path}")
    incoming = DESTINATION / "Luban"
    def version(path):
        if not (path / "Luban.dll").is_file():
            return ""
        result = subprocess.run(
            ["dotnet", path / "Luban.dll", "--version"],
            capture_output=True, text=True, encoding="utf-8",
        )
        return (result.stdout + result.stderr).strip()

    target_version = version(target)
    incoming_version = version(incoming)
    if target_version and target_version == incoming_version:
        print("The production export scripts already use the pinned generator")
        return
    shutil.copytree(incoming, staged)
    if target.exists():
        backup.parent.mkdir(exist_ok=True)
        target.rename(backup)
        print(f"Previous generator preserved at {backup}")
    staged.rename(target)
    print(f"Activated pinned generator at {target}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, help="Existing clean checkout at the pinned commit")
    parser.add_argument("--activate-generator", action="store_true",
                        help="After compatibility checks, back up and replace the local export generator")
    args = parser.parse_args()
    lock = json.loads(LOCK.read_text(encoding="utf-8"))
    if already_installed(lock):
        if args.activate_generator:
            activate_generator()
        return
    if not shutil.which("dotnet") or not shutil.which("git"):
        parser.error("Git and .NET SDK are required")
    if args.source:
        install(args.source.resolve(), lock)
    else:
        with tempfile.TemporaryDirectory(prefix="dgame-luban-source-") as temp:
            source = Path(temp) / "luban"
            run(["git", "clone", "--depth", "1", "--branch", lock["ref"],
                 lock["repository"], source])
            install(source, lock)
    if args.activate_generator:
        activate_generator()


if __name__ == "__main__":
    main()
