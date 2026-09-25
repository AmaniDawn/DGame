"""DGame-specific adapters for validated Luban operations."""

import contextlib
import importlib.util
import io
import os
import sys

from . import luban
from .core import blocked, consume_approval, file_hash, make_plan, require_success, validate_plan, write_json


def _load_helper(path):
    spec = importlib.util.spec_from_file_location("dgame_workflow_luban_helper", path)
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def luban_validate(run, mode="lazyload"):
    """Validate current registered tables without writing production outputs."""
    source = run.context.repo / "GameConfig" / "Datas"
    helper_path = run.context.repo / ".agents/skills/luban-dev/scripts/luban_helper.py"
    if not (source / "Defines" / "__tables__.xlsx").is_file():
        blocked(f"Missing table registry: {source / 'Defines' / '__tables__.xlsx'}")
    if not helper_path.is_file():
        blocked(f"Missing Luban helper: {helper_path}")
    module = _load_helper(helper_path)
    with contextlib.redirect_stdout(io.StringIO()):
        helper = module.LubanConfigHelper(str(source), str(run.path / "luban-cache"))
        result = helper.validate_all()
    script = luban._paths(run.context, mode)[1]
    generated = luban.isolated_export(run, modes=(mode,), repeats=1)["modes"][0]
    helper_artifact = run.path / "luban-helper-diagnostic.json"
    write_json(helper_artifact, result)
    details = result.get("details", [])
    error_count = sum(len(item.get("errors", [])) for item in details)
    warning_count = sum(len(item.get("warnings", [])) for item in details)
    return {
        "mode": mode,
        "script": str(script),
        "helperDiagnostic": {
            "total": result.get("total", 0),
            "valid": result.get("valid", 0),
            "invalid": result.get("invalid", 0),
            "errorCount": error_count,
            "warningCount": warning_count,
            "artifact": str(helper_artifact),
        },
        "lubanValidation": {"status": "passed", "mode": mode},
        "manifestValidation": {"status": "passed", "files": generated["files"]},
        "_manifest": generated["manifest"],
        "helperScope": "Advisory only; helper results never replace Luban validation.",
    }


def _inputs(context):
    config = context.repo / "GameConfig"
    return [p for p in config.rglob("*") if p.is_file()
            and not any(part in ("__pycache__", ".luban_cache", ".luban-cache") for part in p.parts)]


def luban_preview(run, mode, tests=None):
    inputs = _inputs(run.context)
    before = {str(p): file_hash(p) for p in inputs}
    validation = luban_validate(run, mode)
    if before != {str(p): file_hash(p) for p in _inputs(run.context)}:
        blocked("Luban inputs changed during preview; generate a fresh preview.")
    _, script, _, _ = luban._paths(run.context, mode)
    tests = tests or {}
    manifest = validation.pop("_manifest")
    return make_plan(
        run, "luban", "export", {"mode": mode, "tests": tests}, high_risk=True,
        inputs=inputs,
        preview={
            "script": str(script), "mode": mode, "validation": validation,
            "isolatedManifest": {"mode": mode, **validation["manifestValidation"], "manifest": manifest},
            "postExport": {"solutionBuild": True, "editorCompilation": True, "tests": tests},
            "writes": [
                "GameUnity/Assets/Scripts/HotFix/GameProto/LubanConfig",
                "GameUnity/Assets/Scripts/HotFix/GameProto/ConfigSystem.cs",
                "GameUnity/Assets/Scripts/HotFix/GameProto/ExternalTypeUtil.cs",
                "GameUnity/Assets/BundleAssets/Configs/Binary",
                "GameUnity/Configs/Json",
            ],
            "notes": "生产导表会清理并重建脚本和数据输出；代码与 Binary 必须来自同一次导表。",
        },
    )


def luban_apply(run, plan, approval_file):
    if plan.get("kind") != "luban" or plan.get("command") != "export":
        blocked("Not a supported DGame Luban export plan.")
    mode = plan.get("parameters", {}).get("mode", "")
    _, script, dll, _ = luban._paths(run.context, mode)
    validate_plan(run.context, plan)
    validation = luban_validate(run, mode)
    expected = validation.pop("_manifest")
    preview_manifest = plan.get("preview", {}).get("isolatedManifest", {}).get("manifest")
    if not preview_manifest:
        blocked("Luban preview has no validated output manifest; generate a fresh preview.")
    luban.validate_manifest(expected, preview_manifest)
    consume_approval(run, plan, approval_file)
    env = {**os.environ, "AI_MODE": "1", "AUTO_CONTINUE": "1", "LUBAN_DLL": str(dll)}
    args = ["cmd.exe", "/d", "/c", script.name] if os.name == "nt" else ["bash", script.name]
    require_success(run.execute(args, cwd=script.parent, timeout=600, env=env, mutating=True), "DGame Luban export")
    actual = luban._manifest(run.context.repo, run.context.repo / "GameConfig")
    luban.validate_manifest(actual, expected)
    return {"mode": mode, "verifiedFiles": len(actual), "scope": "production export with isolated manifest comparison"}
