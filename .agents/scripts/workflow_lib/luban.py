"""Isolated DGame Luban generation checks."""

import os
from pathlib import Path
import shutil
import re

from .core import WorkflowError, blocked, file_hash, require_success


def _paths(context, mode="lazyload"):
    if mode not in ("standard", "lazyload"):
        raise WorkflowError(f"Unsupported DGame Luban export mode: {mode}")
    config = context.repo / "GameConfig"
    scripts = config / "GenerateTool_Binary"
    suffix = "_lazyload" if mode == "lazyload" else ""
    script = scripts / (f"gen_bin_client{suffix}.bat" if os.name == "nt" else f"gen_bin_client{suffix}.sh")
    dll = config / "Tools" / "LubanTools" / "Luban" / "Luban.dll"
    templates = [config / "CustomTemplate" / "Client" / "Bin" / name
                 for name in ("ConfigSystem.cs", "ExternalTypeUtil.cs")]
    if not script.is_file():
        blocked(f"DGame Luban export script is missing: {script}")
    if not dll.is_file():
        blocked(f"DGame Luban tool is missing: {dll}")
    missing = [str(path) for path in templates if not path.is_file()]
    if missing:
        blocked("DGame Luban template files are missing: " + ", ".join(missing))
    return config, script, dll, templates


def _manifest(root, config=None):
    required = [
        root / "GameUnity/Assets/Scripts/HotFix/GameProto/ConfigSystem.cs",
        root / "GameUnity/Assets/Scripts/HotFix/GameProto/ExternalTypeUtil.cs",
    ]
    missing = [str(path.relative_to(root)) for path in required if not path.is_file()]
    if missing:
        raise WorkflowError("Isolated Luban export is missing required files: " + ", ".join(missing))
    output_dirs = [
        root / "GameUnity/Assets/Scripts/HotFix/GameProto/LubanConfig",
        root / "GameUnity/Assets/BundleAssets/Configs/Binary",
        root / "GameUnity/Configs/Json",
    ]
    missing_dirs = [str(path.relative_to(root)) for path in output_dirs if not path.is_dir()]
    if missing_dirs:
        raise WorkflowError("Isolated Luban export is missing output directories: " + ", ".join(missing_dirs))
    empty_dirs = [str(path.relative_to(root)) for path in output_dirs
                  if not any(item.is_file() for item in path.rglob("*"))]
    if empty_dirs:
        raise WorkflowError("Isolated Luban export has empty output directories: " + ", ".join(empty_dirs))
    # Unity owns .meta sidecars; they are not generator outputs.
    output_files = [path for directory in output_dirs for path in directory.rglob("*")
                    if path.is_file() and path.suffix != ".meta"]
    if not any(path.suffix == ".cs" and "LubanConfig" in path.parts for path in output_files):
        raise WorkflowError("Luban export produced no generated C# table files.")
    if not any(path.suffix == ".bytes" for path in output_files):
        raise WorkflowError("Luban export produced no binary config files.")
    if not any(path.suffix == ".json" for path in output_files):
        raise WorkflowError("Luban export produced no JSON config files.")
    tables = output_dirs[0] / "Tables.cs"
    if not tables.is_file():
        raise WorkflowError("Luban export is missing Tables.cs.")
    names = set(re.findall(r'm_defaultLoader\("([^"\\]+)"\)', tables.read_text(encoding="utf-8-sig")))
    if not names:
        raise WorkflowError("Tables.cs contains no DGame table loader entries.")
    for directory, suffix in ((output_dirs[1], ".bytes"), (output_dirs[2], ".json")):
        expected = {name + suffix for name in names}
        actual = {p.relative_to(directory).as_posix() for p in directory.rglob("*")
                  if p.is_file() and p.suffix != ".meta"}
        if expected != actual:
            raise WorkflowError(f"Table data set mismatch in {directory.name}: missing={sorted(expected-actual)}, stale={sorted(actual-expected)}")
    if config is not None:
        template_root = config / "CustomTemplate" / "Client" / "Bin"
        for name in ("ConfigSystem.cs", "ExternalTypeUtil.cs"):
            template = template_root / name
            generated = root / "GameUnity/Assets/Scripts/HotFix/GameProto" / name
            if not template.is_file() or not generated.is_file() or file_hash(template) != file_hash(generated):
                raise WorkflowError(f"Generated {name} does not match the DGame client template.")
    files = sorted(required + output_files)
    if any(path.stat().st_size == 0 for path in files):
        raise WorkflowError("Isolated Luban export produced empty files.")
    return {str(path.relative_to(root)).replace("\\", "/"): file_hash(path) for path in files}


def validate_manifest(actual, expected):
    if actual != expected:
        missing = sorted(set(expected) - set(actual))
        stale = sorted(set(actual) - set(expected))
        changed = sorted(key for key in set(actual) & set(expected) if actual[key] != expected[key])
        raise WorkflowError(f"Export manifest mismatch. Missing: {missing}; stale: {stale}; changed: {changed}")
    return {"verifiedFiles": len(expected)}


def isolated_export(run, modes=("standard", "lazyload"), repeats=2):
    results = []
    base = run.path / "luban-isolated"
    for mode in modes:
        config, script, dll, templates = _paths(run.context, mode)
        manifests = []
        for index in range(1, repeats + 1):
            root = base / f"{mode}-run-{index}"
            shutil.copytree(config, root / "GameConfig", ignore=shutil.ignore_patterns(".git", "__pycache__", ".luban-cache"))
            isolated_script = root / "GameConfig" / "GenerateTool_Binary" / script.name
            isolated_dll = root / "GameConfig" / "Tools" / "LubanTools" / "Luban" / dll.name
            bridge_dir = root / "GameUnity/Assets/Scripts/HotFix/GameProto"
            bridge_dir.mkdir(parents=True, exist_ok=True)
            for template in templates:
                shutil.copy2(root / "GameConfig" / template.relative_to(config), bridge_dir / template.name)
            if os.name == "nt":
                # The repository scripts use xcopy for interactive local use. Replace it in the
                # isolated copy with file-only copy so missing file/directory prompts cannot pass.
                script_text = isolated_script.read_text(encoding="utf-8-sig")
                script_text = script_text.replace("xcopy /s /e /i /y", "copy /y")
                isolated_script.write_text(script_text, encoding="utf-8")
                args = ["cmd.exe", "/d", "/c", isolated_script.name]
            else:
                args = ["bash", isolated_script.name]
            env = {**os.environ, "AI_MODE": "1", "AUTO_CONTINUE": "1", "LUBAN_DLL": str(isolated_dll)}
            require_success(run.execute(args, cwd=isolated_script.parent, timeout=600, env=env),
                            f"Isolated DGame Luban {mode} export {index}")
            manifests.append(_manifest(root, root / "GameConfig"))
        for manifest in manifests[1:]:
            validate_manifest(manifest, manifests[0])
        results.append({"mode": mode, "runs": repeats, "files": len(manifests[0]), "manifest": manifests[0]})
    return {"modes": results, "scope": "isolated"}
