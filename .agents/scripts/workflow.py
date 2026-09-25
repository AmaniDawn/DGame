#!/usr/bin/env python3
"""DGame project-bound Unity CLI workflow entrypoint."""

from __future__ import annotations

import argparse
from pathlib import Path
import sys

from workflow_lib import checks, domains, evaluations, luban
from workflow_lib.core import Context, Run, WorkflowError, approve, blocked, project_lock, read_json, render_report
from workflow_lib.unity import Unity, decode_response


def parser():
    root = argparse.ArgumentParser(description=__doc__)
    root.add_argument("--project", type=Path, help="Unity project root; defaults to GameUnity.")
    commands = root.add_subparsers(dest="command", required=True)
    commands.add_parser("doctor", help="Read-only environment and project preflight.")
    commands.add_parser("check", help="Validate DGame workflow structure and references.")
    evaluation = commands.add_parser("eval", help="Check recorded skill answers; does not generate model responses.")
    evaluation.add_argument("--responses", required=True, type=Path)
    verify = commands.add_parser("verify", help="Run a validation profile.")
    verify.add_argument("--profile", choices=("docs", "code", "unity", "full"), required=True)
    verify.add_argument("--test-filter", default="DGame", help="Explicit test filter.")
    verify.add_argument("--filter-type", choices=("assembly", "testName", "category"), default="assembly")
    verify.add_argument("--timeout", type=int, default=300)
    approval = commands.add_parser("approve", help="Record reviewed approval for a preview plan.")
    approval.add_argument("--plan", required=True, type=Path)
    approval.add_argument("--by", required=True)
    approval.add_argument("--reason", required=True)
    approval.add_argument("--high-risk", action="store_true")
    report = commands.add_parser("report", help="Render already recorded evidence.")
    report.add_argument("--run", required=True, type=Path)
    unity = commands.add_parser("unity", help="Audited project-bound Unity operations.")
    sub = unity.add_subparsers(dest="action", required=True)
    sub.add_parser("list")
    for action in ("query", "preview"):
        command = sub.add_parser(action)
        command.add_argument("name")
        command.add_argument("--params-file", type=Path)
    apply = sub.add_parser("apply")
    apply.add_argument("--plan", type=Path, required=True)
    apply.add_argument("--approval", type=Path, required=True)
    status = sub.add_parser("runtime-status")
    status.add_argument("--runtime-path", type=Path, required=True)
    luban = commands.add_parser("luban", help="Validate or explicitly authorize the existing DGame client export.")
    sub = luban.add_subparsers(dest="action", required=True)
    sub.add_parser("validate", help="Validate registered tables without writing production outputs.")
    sub.add_parser("test", help="Run standard and LazyLoad exporters in isolation.")
    preview = sub.add_parser("preview", help="Preview a production export and create an approval-bound plan.")
    preview.add_argument("--mode", choices=("lazyload", "standard"), default="lazyload")
    preview.add_argument("--editmode-filter", help="Assembly filter for explicitly required EditMode tests.")
    preview.add_argument("--playmode-filter", help="Assembly filter for explicitly required PlayMode tests.")
    apply = sub.add_parser("apply", help="Apply an approved production export plan.")
    apply.add_argument("--plan", type=Path, required=True)
    apply.add_argument("--approval", type=Path, required=True)
    return root


def dispatch(run, args):
    if args.command == "doctor":
        run.step("environment", lambda: checks.doctor(run))
    elif args.command == "check":
        run.step("workflow-structure", lambda: checks.structure(run.context))
        run.step("workflow-unit-tests", lambda: checks.unit_tests(run))
    elif args.command == "eval":
        run.step("skill-answer-evaluation", lambda: evaluations.evaluate(run, args.responses))
    elif args.command == "verify":
        if args.timeout < 1 or not args.test_filter.strip():
            raise WorkflowError("Timeout and test filter must be nonempty/positive.")
        checks.verify(run, args.profile, args.test_filter, args.filter_type, args.timeout)
    elif args.command == "approve":
        run.step("approval-record", lambda: approve(run, args.plan, args.by, args.reason, args.high_risk))
    elif args.command == "report":
        return render_report(args.run)
    elif args.command == "unity":
        unity = Unity(run)
        with project_lock(run.context):
            if args.action == "list":
                run.step("registered-commands", unity.discover)
            elif args.action == "runtime-status":
                run.step("runtime-status", lambda: runtime_status(unity, args.runtime_path))
            elif args.action == "apply":
                run.step("unity-mutation", lambda: unity.apply(read_json(args.plan), args.approval))
            else:
                params = read_json(args.params_file) if args.params_file else {}
                method = unity.query if args.action == "query" else unity.preview
                run.step("unity-" + args.action, lambda: method(args.name, params))
    elif args.command == "luban":
        with project_lock(run.context):
            if args.action == "validate":
                run.step("luban-validation", lambda: domains.luban_validate(run))
            elif args.action == "test":
                run.step("luban-regression", lambda: luban.isolated_export(run))
            elif args.action == "preview":
                tests = {mode: query for mode, query in (("editor", args.editmode_filter),
                         ("playmode", args.playmode_filter)) if query is not None}
                if any(not query.strip() for query in tests.values()):
                    raise WorkflowError("Test filters must not be blank.")
                run.step("luban-preview", lambda: domains.luban_preview(run, args.mode, tests))
            else:
                plan = read_json(args.plan)
                export = run.step("luban-export", lambda: domains.luban_apply(run, plan, args.approval))
                if export["status"] == "passed":
                    post_luban_validation(run, plan["parameters"].get("tests", {}))


def post_luban_validation(run, tests=None):
    """Validate production Luban output before declaring an apply complete."""
    tests = tests or {}
    selected = [(mode, name, tests[mode]) for mode, name in
                (("editor", "editmode-tests"), ("playmode", "playmode-tests")) if mode in tests]
    build = run.step("solution-build", lambda: checks.build(run))
    if build["status"] != "passed":
        run.skip("editor-compilation", "Solution build did not pass.", required=True)
        for _, name, _ in selected:
            run.skip(name, "Solution build did not pass.", required=True)
        return

    unity = Unity(run)
    connection = run.step("editor-capabilities", unity.discover)
    if connection["status"] != "passed":
        run.skip("editor-compilation", "Requires the current DGame Editor connection.", required=True)
        for _, name, _ in selected:
            run.skip(name, "Requires the current DGame Editor connection.", required=True)
        return

    compilation = run.step("editor-compilation", lambda: unity.recompile(300))
    if compilation["status"] != "passed":
        for _, name, _ in selected:
            run.skip(name, "Editor compilation did not pass.", required=True)
        return

    for mode, name, query in selected:
        run.step(name, lambda mode=mode, query=query: unity.tests(mode, query, "assembly", 300))
    if not selected:
        run.skip("unity-tests", "No test assemblies selected in the approved preview; only compilation verified.")


def runtime_status(unity, path):
    path = path.resolve()
    descriptor_path = path / ".unity-pipeline-runtime-port"
    descriptor = read_json(descriptor_path)
    if not isinstance(descriptor, dict):
        blocked("Runtime descriptor is not an object.")
    port = descriptor.get("port")
    token = descriptor.get("evalToken")
    if isinstance(port, bool) or not isinstance(port, int) or not 1 <= port <= 65535:
        blocked("Runtime descriptor has no valid port.")
    if not isinstance(token, str) or not token.strip():
        blocked("Runtime descriptor has no authentication token.")
    descriptor_directory = descriptor.get("workingDirectory") or descriptor.get("WorkingDirectory")
    if not isinstance(descriptor_directory, str) or not descriptor_directory.strip():
        blocked("Runtime descriptor has no working directory.")
    value = decode_response(unity.raw(["command", "--runtime-path", str(path), "runtime_status"]))
    if not isinstance(value, dict):
        blocked("Runtime status response is not an object.")
    return {"runtimePath": str(path), "workingDirectory": descriptor_directory,
            "port": port, "status": value}


def main(argv=None):
    args = parser().parse_args(argv)
    if sys.version_info < (3, 11):
        print("Python 3.11 or newer is required.", file=sys.stderr)
        return 2
    if args.command == "report":
        try:
            status = dispatch(None, args)
            return {"passed": 0, "failed": 1, "blocked": 2}[status]
        except (OSError, WorkflowError, KeyError) as exc:
            print(str(exc), file=sys.stderr)
            return 2
    try:
        context = Context(args.project)
    except WorkflowError as exc:
        print(str(exc), file=sys.stderr)
        return 2
    run = Run(context, " ".join(argv if argv is not None else sys.argv[1:]))
    try:
        dispatch(run, args)
    except WorkflowError as exc:
        run.data["steps"].append({"name": "dispatch", "status": exc.status, "message": str(exc), "required": True})
    except KeyboardInterrupt:
        run.data["steps"].append({"name": "interrupted", "status": "blocked", "message": "Inspect the in-flight operation before retrying.", "required": True})
    except Exception as exc:
        run.data["steps"].append({"name": "unexpected-error", "status": "failed", "message": f"{type(exc).__name__}: {exc}", "required": True})
    code = run.finish()
    print(f"{run.data['status']}: {run.path / 'report.md'}")
    for step in run.data["steps"]:
        print(f"  {step['name']}: {step['status']}")
        if step.get("message"):
            print("    " + step["message"])
    return code


if __name__ == "__main__":
    raise SystemExit(main())
