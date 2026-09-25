"""Offline regression tests; answers here are fixtures, not model evaluation evidence."""

import contextlib
import copy
import io
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "scripts"))
import workflow
from workflow_lib import checks, evaluations
from workflow_lib.core import Context, WorkflowError, read_json, write_json


class EvaluationTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.repo = Path(self.directory.name)
        self.project = self.repo / "GameUnity"
        for name in ("Assets", "Packages", "ProjectSettings"):
            (self.project / name).mkdir(parents=True)
        self.context = Context(self.project)
        self.path = evaluations.suite_path(self.context)
        self.case = {"id": 3, "name": "entrance", "description": "Fixture", "prompt": "Fixture prompt",
                     "expected_literals": ["Entrance(object[]", "objects[0]", "GameEventLauncher.Init"],
                     "forbidden_literals": ["Entrance(List<Assembly>"],
                     "expected_patterns": [r"List<\w+>"], "forbidden_patterns": [r"Wrong\w+"]}
        self.suite = {"schemaVersion": 1, "skill_name": "dgame-dev", "evals": [self.case]}
        write_json(self.path, self.suite)
        self.output = "Entrance(object[] objects) List<Assembly> objects[0] GameEventLauncher.Init"

    def execute(self, responses):
        answers = self.repo / "answers.json"
        write_json(answers, {"skill_name": "dgame-dev", "responses": responses})
        with contextlib.redirect_stdout(io.StringIO()):
            code = workflow.main(["--project", str(self.project), "eval", "--responses", str(answers)])
        reports = list(self.context.runs.glob("*/report.json"))
        self.assertEqual(len(reports), 1)
        return code, read_json(reports[0]), reports[0].parent / "eval-results.json"

    def test_actual_suite_valid(self):
        result = evaluations.validate(Context())
        self.assertEqual(result["cases"], 15)
        self.assertEqual(result["scope"], "format-only")
        self.assertGreater(result["rules"]["expected_patterns"], 0)
        case = next(c for c in evaluations.load_suite(evaluations.suite_path(Context()))["evals"] if c["id"] == 3)
        self.assertIn("Entrance(object[]", case["expected_literals"])
        self.assertIn("Entrance(List<Assembly>", case["forbidden_literals"])

    def test_rejects_bad_cases(self):
        mutations = [
            lambda s: s["evals"].append(copy.deepcopy(s["evals"][0])),
            lambda s: s["evals"][0].update(id=True),
            lambda s: s["evals"][0].update(prompt=" "),
            lambda s: s["evals"][0].update(expected_patterns=["Entrance(object[]"]),
            lambda s: s["evals"][0].update(expected_literals="not a list"),
            lambda s: s["evals"][0].update(expected_literals=[None]),
            lambda s: s["evals"][0].update(expected_literals=[], expected_patterns=[]),
            lambda s: s["evals"][0].update(expected_literal=["typo"]),
            lambda s: s.update(evals=[]),
        ]
        for index, mutate in enumerate(mutations):
            with self.subTest(index=index):
                suite = copy.deepcopy(self.suite)
                mutate(suite)
                write_json(self.path, suite)
                with self.assertRaises(WorkflowError):
                    evaluations.load_suite(self.path)

    def test_missing_and_malformed_suite_fail(self):
        self.path.write_text("{", encoding="utf-8")
        with self.assertRaises(WorkflowError):
            evaluations.validate(self.context)
        self.path.unlink()
        with self.assertRaises(WorkflowError):
            evaluations.validate(self.context)

    def test_structure_calls_evaluation_validator(self):
        with patch.object(evaluations, "validate", side_effect=WorkflowError("Invalid evaluation suite")) as validator:
            with self.assertRaisesRegex(WorkflowError, "Invalid evaluation suite"):
                checks.structure(Context())
            validator.assert_called_once()

    def test_success_records_answer_and_rule_evidence(self):
        answer = {"id": 3, "source": "unit-test fixture", "output": self.output}
        code, report, artifact = self.execute([answer])
        self.assertEqual(code, 0)
        self.assertEqual(report["status"], "passed")
        record = read_json(artifact)
        self.assertEqual(record["responses"]["responses"], [answer])
        self.assertEqual(record["scope"], "text-match-only")
        self.assertEqual(record["counts"], {"passed": 1, "failed": 0, "skipped": 0})
        self.assertTrue(all(c["passed"] for c in record["cases"][0]["checks"]))

    def test_brackets_and_dots_are_literal(self):
        output = self.output.replace("objects[0]", "objects0").replace("GameEventLauncher.Init", "GameEventLauncherXInit")
        code, report, artifact = self.execute([{"id": 3, "source": "unit-test fixture", "output": output}])
        self.assertEqual((code, report["status"]), (1, "failed"))
        missed = [c["rule"] for c in read_json(artifact)["cases"][0]["checks"] if not c["passed"]]
        self.assertEqual(missed, ["objects[0]", "GameEventLauncher.Init"])

    def test_forbidden_rules_fail(self):
        output = self.output + " Entrance(List<Assembly> WrongAPI"
        code, report, artifact = self.execute([{"id": 3, "source": "unit-test fixture", "output": output}])
        self.assertEqual((code, report["status"]), (1, "failed"))
        self.assertEqual(read_json(artifact)["counts"]["failed"], 1)

    def test_missing_responses_block(self):
        code, report, artifact = self.execute([])
        self.assertEqual((code, report["status"]), (2, "blocked"))
        self.assertEqual(read_json(artifact)["cases"][0]["status"], "skipped")

    def test_unknown_response_id_fails(self):
        code, report, _ = self.execute([{"id": 999, "source": "fixture", "output": self.output}])
        self.assertEqual((code, report["status"]), (1, "failed"))

    def test_duplicate_response_id_fails(self):
        answer = {"id": 3, "source": "fixture", "output": self.output}
        code, report, _ = self.execute([answer, answer])
        self.assertEqual((code, report["status"]), (1, "failed"))

    def test_missing_source_fails(self):
        code, report, _ = self.execute([{"id": 3, "output": self.output}])
        self.assertEqual((code, report["status"]), (1, "failed"))


if __name__ == "__main__":
    unittest.main()
