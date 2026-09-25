"""Validate skill evaluation cases and record text checks against supplied answers."""

import re
from pathlib import Path

from .core import WorkflowError, digest, read_json, redact, utcnow, write_json


RULE_FIELDS = ("expected_patterns", "forbidden_patterns", "expected_literals", "forbidden_literals")


def suite_path(context):
    return context.scripts.parent / "skills/dgame-dev/evals/evals.json"


def load_suite(path):
    data = read_json(path)
    if (not isinstance(data, dict) or data.get("schemaVersion") != 1
            or data.get("skill_name") != "dgame-dev"
            or not isinstance(data.get("evals"), list) or not data["evals"]):
        raise WorkflowError(f"Invalid evaluation suite: {path}")
    ids, names = set(), set()
    for index, case in enumerate(data["evals"]):
        label = f"Evaluation case {index + 1}"
        if not isinstance(case, dict) or set(case) - {"id", "name", "description", "prompt", *RULE_FIELDS}:
            raise WorkflowError(f"{label}: invalid case fields")
        case_id = case.get("id")
        if type(case_id) is not int or case_id < 1 or case_id in ids:
            raise WorkflowError(f"{label}: id must be a unique positive integer")
        ids.add(case_id)
        for field in ("name", "description", "prompt"):
            if not isinstance(case.get(field), str) or not case[field].strip():
                raise WorkflowError(f"{label}: {field} must be nonempty text")
        if case["name"] in names:
            raise WorkflowError(f"{label}: duplicate name {case['name']}")
        names.add(case["name"])
        for field in RULE_FIELDS:
            rules = case.get(field, [])
            if not isinstance(rules, list) or any(not isinstance(rule, str) or not rule.strip() for rule in rules):
                raise WorkflowError(f"{label}: {field} must be a list of nonempty strings")
            if field.endswith("_patterns"):
                for rule in rules:
                    try:
                        re.compile(rule)
                    except re.error as exc:
                        raise WorkflowError(f"{label}: invalid regex in {field}: {rule!r}: {exc}") from exc
        if not case.get("expected_patterns") and not case.get("expected_literals"):
            raise WorkflowError(f"{label}: at least one expected rule is required")
    return data


def validate(context):
    data = load_suite(suite_path(context))
    return {"skill": data["skill_name"], "cases": len(data["evals"]),
            "rules": {field: sum(len(case.get(field, [])) for case in data["evals"]) for field in RULE_FIELDS},
            "scope": "format-only", "suiteHash": digest(data)}


def evaluate(run, responses_file):
    suite = load_suite(suite_path(run.context))
    answers = read_json(responses_file)
    if (not isinstance(answers, dict) or answers.get("skill_name") != suite["skill_name"]
            or not isinstance(answers.get("responses"), list)):
        raise WorkflowError("Responses must contain skill_name and a responses list.")
    known_ids = {case["id"] for case in suite["evals"]}
    responses = {}
    for answer in answers["responses"]:
        if (not isinstance(answer, dict) or type(answer.get("id")) is not int
                or answer["id"] not in known_ids or answer["id"] in responses):
            raise WorkflowError("Response ids must be unique and belong to the evaluation suite.")
        if any(not isinstance(answer.get(key), str) or not answer[key].strip() for key in ("output", "source")):
            raise WorkflowError(f"Response {answer['id']} requires nonempty output and source.")
        responses[answer["id"]] = answer
    results = []
    for case in suite["evals"]:
        result = {"id": case["id"], "name": case["name"], "status": "skipped", "checks": []}
        answer = responses.get(case["id"])
        if answer is not None:
            for field in RULE_FIELDS:
                for rule in case.get(field, []):
                    matched = (re.search(rule, answer["output"]) is not None if field.endswith("_patterns")
                               else rule in answer["output"])
                    passed = matched if field.startswith("expected_") else not matched
                    result["checks"].append({"field": field, "rule": rule, "matched": matched, "passed": passed})
            result["status"] = "passed" if all(item["passed"] for item in result["checks"]) else "failed"
        else:
            result["reason"] = "No answer supplied; not evaluated."
        results.append(result)
    counts = {status: sum(item["status"] == status for item in results) for status in ("passed", "failed", "skipped")}
    status = "failed" if counts["failed"] else "blocked" if counts["skipped"] else "passed"
    record = {"schemaVersion": 1, "scope": "text-match-only", "createdAt": utcnow(),
              "suiteHash": digest(suite), "responsesHash": digest(answers),
              "responsesFile": str(Path(responses_file).resolve()), "suite": suite, "responses": answers,
              "status": status, "counts": counts, "cases": results}
    artifact = run.path / "eval-results.json"
    write_json(artifact, redact(record))
    if status != "passed":
        raise WorkflowError(f"Text evaluation {status}: {counts}; see {artifact}", status)
    return {"scope": "text-match-only", "counts": counts, "artifact": str(artifact)}
