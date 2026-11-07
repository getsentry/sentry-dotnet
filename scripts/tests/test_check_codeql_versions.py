import importlib.util
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


spec = importlib.util.spec_from_file_location(
    "check_codeql_versions", Path(__file__).resolve().parents[1] / "check-codeql-versions.py"
)
checker = importlib.util.module_from_spec(spec)
spec.loader.exec_module(checker)


class CodeQlVersionTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.root = Path(self.directory.name)

    def write(self, name, content):
        path = self.root / ".github" / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(content, encoding="utf-8")

    def test_matching_pinned_refs(self):
        ref = "a" * 40
        self.write("workflows/codeql.yml", f"- uses: github/codeql-action/init@{ref}\n"
                   f"  uses: github/codeql-action/analyze@{ref}\n")
        self.assertEqual(checker.check_versions(self.root), ref)

    def test_mismatched_versions_report_locations(self):
        self.write("workflows/codeql.yml", "- uses: github/codeql-action/init@v4.37.1\n"
                   "- uses: github/codeql-action/analyze@v4.37.0\n")
        with self.assertRaisesRegex(ValueError, "CodeQL actions must use the same ref") as raised:
            checker.check_versions(self.root)
        self.assertIn("codeql.yml:1", str(raised.exception))
        self.assertIn("codeql.yml:2", str(raised.exception))

    def test_mismatched_shas_with_matching_version_comments_fail(self):
        self.write("workflows/codeql.yml", f"- uses: github/codeql-action/init@{'a' * 40} # v4.39.2\n"
                   f"- uses: github/codeql-action/analyze@{'b' * 40} # v4.39.2\n")
        with self.assertRaises(ValueError):
            checker.check_versions(self.root)

    def test_quotes_comments_and_crlf(self):
        self.write("workflows/codeql.yaml", "  - uses: 'github/codeql-action/init@v4' # initialize\r\n"
                   '    uses: "github/codeql-action/analyze@v4"\r\n')
        self.assertEqual(checker.check_versions(self.root), "v4")

    def test_other_actions_and_commented_references_are_ignored(self):
        self.write("workflows/codeql.yml", "- uses: actions/checkout@v7\n"
                   "# - uses: github/codeql-action/init@old\n"
                   "- uses: github/codeql-action/init@v4\n")
        self.assertEqual(checker.check_versions(self.root), "v4")

    def test_composite_actions_and_other_sub_actions_are_checked(self):
        self.write("workflows/codeql.yml", "- uses: github/codeql-action/init@v4\n")
        self.write("actions/upload/action.yaml", "- uses: github/codeql-action/upload-sarif@v3\n")
        with self.assertRaisesRegex(ValueError, "upload-sarif@v3"):
            checker.check_versions(self.root)

    def test_missing_references_fail(self):
        self.write("workflows/other.yml", "- uses: actions/checkout@v7\n")
        with self.assertRaisesRegex(ValueError, "No CodeQL action references"):
            checker.check_versions(self.root)

    def run_cli(self):
        script = self.root / "scripts" / "check-codeql-versions.py"
        script.parent.mkdir()
        shutil.copyfile(spec.origin, script)
        return subprocess.run([sys.executable, "-B", str(script)], capture_output=True, text=True)

    def test_cli_succeeds_for_matching_versions(self):
        self.write("workflows/codeql.yml", "- uses: github/codeql-action/init@v4\n"
                   "- uses: github/codeql-action/analyze@v4\n")
        result = self.run_cli()
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("All CodeQL actions use v4", result.stdout)

    def test_cli_fails_for_mismatched_versions(self):
        self.write("workflows/codeql.yml", "- uses: github/codeql-action/init@v4.37.1\n"
                   "- uses: github/codeql-action/analyze@v4.37.0\n")
        result = self.run_cli()
        self.assertEqual(result.returncode, 1)
        self.assertIn("CodeQL actions must use the same ref", result.stderr)


if __name__ == "__main__":
    unittest.main()
