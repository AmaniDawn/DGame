"""Offline checks for the DGame Unity CLI workflow."""

from __future__ import annotations

import re
import shutil
import sys
import xml.etree.ElementTree as ET
from urllib.parse import unquote, urlsplit

from .core import WorkflowError, blocked, file_hash, project_lock, read_json, require_success
from .unity import Unity
from . import evaluations
from . import luban

SKILLS = ("dgame-dev", "unity-cli", "luban-dev", "fantasy-net")
# skill-creator is a bundled auxiliary skill used to maintain skills; it is not a
# DGame routing domain and is intentionally excluded from project skill discovery.
AUXILIARY_SKILLS = {"skill-creator"}


def behavior_scenarios(context):
    path = context.scripts.parent / "evals" / "scenarios.json"
    data = read_json(path)
    if data.get("schemaVersion") != 1 or not isinstance(data.get("cases"), list) or not data["cases"]:
        raise WorkflowError(f"Invalid behavior scenario file: {path}")
    ids = set()
    for case in data["cases"]:
        if not isinstance(case, dict) or not isinstance(case.get("id"), str) or not case["id"].strip():
            raise WorkflowError(f"Invalid behavior scenario id in {path}")
        if case["id"] in ids:
            raise WorkflowError(f"Duplicate behavior scenario id: {case['id']}")
        ids.add(case["id"])
        if not isinstance(case.get("prompt"), str) or not case["prompt"].strip():
            raise WorkflowError(f"Scenario {case['id']} has no prompt")
        if not isinstance(case.get("skills"), list) or any(skill not in SKILLS for skill in case["skills"]):
            raise WorkflowError(f"Scenario {case['id']} references an unknown skill")
        if not isinstance(case.get("checks"), list) or not case["checks"]:
            raise WorkflowError(f"Scenario {case['id']} has no checks")
    return {"cases": len(ids), "ids": sorted(ids), "scope": "routing-and-evidence"}


def structure(context):
    root = context.scripts.parent
    errors = []
    skill_root = root / "skills"
    discovered = sorted(path.parent.name for path in skill_root.glob("*/SKILL.md")
                        if path.parent.name not in AUXILIARY_SKILLS)
    if discovered != sorted(SKILLS):
        errors.append(f"Expected project skills {SKILLS}, got {discovered}")
    try:
        import yaml
    except ImportError:
        blocked("PyYAML is required for skill metadata validation.")
    for name in SKILLS:
        directory = root / "skills" / name
        skill = directory / "SKILL.md"
        if not skill.is_file():
            errors.append(f"Missing skill: {skill}")
            continue
        text = skill.read_text(encoding="utf-8-sig")
        sections = text.split("---", 2)
        try:
            metadata = yaml.safe_load(sections[1]) if len(sections) == 3 and not sections[0].strip() else None
            if not isinstance(metadata, dict) or metadata.get("name") != name or not metadata.get("description"):
                errors.append(f"{name}: missing or invalid frontmatter")
            interface_path = directory / "agents" / "openai.yaml"
            interface = yaml.safe_load(interface_path.read_text(encoding="utf-8")) if interface_path.is_file() else None
            if not isinstance(interface, dict):
                errors.append(f"{name}: missing or invalid interface metadata: {interface_path}")
            else:
                values = interface.get("interface", {})
                if "$" + name not in values.get("default_prompt", ""):
                    errors.append(f"{name}: default_prompt must name the skill")
                if not 20 <= len(values.get("short_description", "")) <= 64:
                    errors.append(f"{name}: short_description must be 20-64 characters")
                if name == "luban-dev" and interface.get("policy", {}).get("allow_implicit_invocation") is not True:
                    errors.append("luban-dev: implicit invocation policy must be enabled")
        except (OSError, ValueError, yaml.YAMLError) as exc:
            errors.append(f"{name}: invalid metadata: {exc}")
    docs = [context.repo / "AGENTS.md", context.project / "AGENTS.md", context.repo / "README.md"]
    for name in SKILLS:
        docs.extend((root / "skills" / name).rglob("*.md"))
    for path in docs:
        if not path.is_file():
            errors.append(f"Missing document: {path}")
            continue
        text = path.read_text(encoding="utf-8-sig")
        if re.search(r"(?:^|\s)(?:[A-Za-z]:[\\/](?:Users|WorkSpace)[\\/]|/Users/[^\s/]+/)", text):
            errors.append(f"Machine-specific path in {path}")
        if re.search(r"\bsk-[A-Za-z0-9_-]{20,}\b", text):
            errors.append(f"Credential-like token in {path}")
        if re.search(r"\.claude/|openspec/(?:specs|changes)|mcp-tools\.md|mcp-visual\.md|mcp__unity|unity-mcp-orchestrator", text):
            errors.append(f"Retired workflow reference in {path}")
        if path == context.repo / "README.md":
            match = re.search(r"^## .*AI.*(?:\n.*)*", text, re.M)
            workflow_text = match[0].split("\n## ", 1)[0] if match else ""
            if not workflow_text or "AGENTS.md" not in workflow_text:
                errors.append("README workflow entry is missing")
        for target in re.findall(r"\]\(([^)]+)\)", text):
            target = target.strip().split(None, 1)[0].strip("<>")
            parts = urlsplit(target)
            if parts.scheme or parts.netloc:
                continue
            destination = (path.parent / unquote(parts.path)).resolve() if parts.path else path.resolve()
            if not destination.exists():
                errors.append(f"Broken reference: {path} -> {target}")
            if parts.fragment and destination.is_file():
                target_text = destination.read_text(encoding="utf-8-sig")
                headings = {
                    re.sub(r"[^\w\u4e00-\u9fff- ]", "", heading).strip().lower().replace(" ", "-")
                    for heading in re.findall(r"^#+\s+(.+)$", target_text, re.M)
                }
                fragment = unquote(parts.fragment).lower()
                if fragment not in headings and fragment.lstrip("-") not in headings:
                    errors.append(f"Broken anchor: {path} -> {target}")
    package = context.project / "Packages" / "com.unity.pipeline" / "package.json"
    if not package.is_file():
        errors.append(f"Missing embedded Unity Pipeline package: {package}")
    if errors:
        raise WorkflowError("\n".join(errors))
    return {"skills": list(SKILLS), "package": str(package),
            "evaluations": evaluations.validate(context), "behaviorScenarios": behavior_scenarios(context)}


def solution_inputs(context):
    if not context.solution.is_file():
        blocked(f"Generated solution missing: {context.solution}")
    projects = re.findall(r'Project\("[^"]+"\)\s*=\s*"[^"]+",\s*"([^"]+\.csproj)"',
                          context.solution.read_text(encoding="utf-8-sig"))
    if not projects:
        blocked("Solution contains no C# projects.")
    sources = set()
    for name in projects:
        path = context.project / name.replace("\\", "/")
        if not path.is_file():
            blocked(f"Generated project missing: {name}")
        for item in ET.parse(path).getroot().iter():
            if item.tag.rsplit("}", 1)[-1] == "Compile" and item.get("Include"):
                sources.add((path.parent / item.get("Include").replace("\\", "/")).resolve())
    missing = [str(path.relative_to(context.project)) for path in (context.project / "Assets").rglob("*.cs")
               if path.resolve() not in sources]
    if missing:
        blocked("Sources absent from the generated solution; regenerate project files: " + ", ".join(missing[:30]))
    return {"projects": len(projects), "sources": len(sources)}


def unit_tests(run):
    result = run.execute([sys.executable, "-m", "unittest", "discover", "-s",
                          run.context.repo / ".agents" / "tests", "-p", "test_*.py", "-v"],
                         cwd=run.context.repo, timeout=300)
    output = result.stdout + result.stderr
    require_success(result, "Workflow unit tests")
    skipped = re.search(r"(?:OK|FAILED)\s*\(.*skipped\s*=\s*(\d+)", output)
    if skipped and int(skipped.group(1)) > 0:
        blocked("Workflow unit suite contains skipped tests; coverage is incomplete.")
    match = re.search(r"Ran (\d+) tests?", output)
    if not match or int(match.group(1)) == 0:
        blocked("Workflow unit suite ran no tests.")
    count = int(match.group(1))
    # Verbose unittest emits one terminal '... ok' outcome per successful test,
    # including tests with multi-line docstrings. An exit code alone is insufficient.
    outcomes = re.findall(r"\.\.\. ok\s*$", output, re.M)
    if len(outcomes) != count or not re.search(r"^OK\s*$", output, re.M):
        raise WorkflowError("Workflow unit summary disagrees with individual successful outcomes.")
    return {"tests": count, "passed": len(outcomes), "skipped": 0, "runner": "unittest"}


def build(run):
    if not shutil.which("dotnet"):
        blocked(".NET SDK is missing.")
    inputs = solution_inputs(run.context)
    result = run.execute(["dotnet", "build", run.context.solution, "--nologo", "-v:q", "-clp:ErrorsOnly"], timeout=300)
    require_success(result, "Full DGame solution build")
    return {"solution": str(run.context.solution), "inputs": inputs}


def doctor(run):
    context = run.context
    package = context.project / "Packages" / "com.unity.pipeline" / "package.json"
    if not package.is_file() or not context.cli.is_file() or not context.solution.is_file():
        missing = [str(path) for path in (package, context.cli, context.solution) if not path.is_file()]
        blocked("Project preflight files missing: " + ", ".join(missing))
    package_info = read_json(package)
    version = (context.project / "ProjectSettings" / "ProjectVersion.txt").read_text(encoding="utf-8-sig").strip()
    tools = {name: shutil.which(name) for name in ("git", "dotnet", "python")}
    missing = [name for name, path in tools.items() if not path]
    if missing:
        blocked("Required tools missing: " + ", ".join(missing))
    cli_version = require_success(run.execute([context.cli, "--version"], timeout=15), "Unity CLI version").strip()
    dotnet_version = require_success(run.execute(["dotnet", "--version"], timeout=15), ".NET version").strip()
    cli_hash = file_hash(context.cli) if context.cli.is_file() else None
    cli_help = require_success(run.execute([context.cli, "--help"], timeout=15), "Unity CLI help")
    required_commands = ("status", "command", "list", "pipeline")
    missing_commands = [name for name in required_commands
                        if not re.search(r"^\s*" + name + r"\b", cli_help, re.M)]
    if missing_commands:
        blocked("CLI lacks Pipeline command surface: " + ", ".join(missing_commands))
    return {"python": sys.version, "dotnet": dotnet_version, "git": tools["git"],
            "unity": version, "pipeline": package_info.get("version"), "cli": cli_version,
            "cliPath": str(context.cli), "cliSha256": cli_hash,
            "cliCommands": list(required_commands),
            "solution": str(context.solution), "tools": tools,
            "pipelineCapabilityCheck": "run workflow.py unity list against this project before Unity operations"}


def verify(run, profile, test_filter, filter_type, timeout):
    if profile in ("docs", "code", "full"):
        run.step("workflow-structure", lambda: structure(run.context))
        run.step("workflow-unit-tests", lambda: unit_tests(run))
    if profile in ("code", "full"):
        run.step("solution-build", lambda: build(run))
    if profile == "full":
        run.step("luban-isolated-export", lambda: luban.isolated_export(run))
    if profile in ("unity", "full"):
        with project_lock(run.context):
            unity = Unity(run)
            connection = run.step("editor-capabilities", unity.discover)
            if connection["status"] != "passed":
                for name in ("editor-compilation", "editmode-tests", "playmode-tests"):
                    run.skip(name, "Requires the project Editor connection.", required=True)
                return
            compilation = run.step("editor-compilation", lambda: unity.recompile(timeout))
            if compilation["status"] != "passed":
                for name in ("editmode-tests", "playmode-tests"):
                    run.skip(name, "Editor compilation did not pass.", required=True)
                return
            for mode, name in (("editor", "editmode-tests"), ("playmode", "playmode-tests")):
                run.step(name, lambda mode=mode: unity.tests(mode, test_filter, filter_type, timeout))
