"""Tests for the hash-bound human listening sign-off in the English voice audit."""
import importlib.util
import pathlib
import sys
import unittest

# The audit imports its sibling module by bare name, so its folder must be importable.
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / 'tools/gameplay'))

SPEC = importlib.util.spec_from_file_location(
    'audit_english_voice',
    pathlib.Path(__file__).resolve().parents[1] / 'tools/gameplay/audit_english_voice.py',
)
AUDIT = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(AUDIT)
FLAG = 'transcript discrepancy requires review'


class ListeningAcceptanceTests(unittest.TestCase):
    """Check that only a matching human record clears only a recognizer flag."""

    def record(self, **changes):
        """Build one accepted record for cue 'c' and hash 'h', with optional changes."""
        return {'c': dict({'cue': 'c', 'audio_sha256': 'h', 'decision': 'accepted', 'reviewer': 'r', 'date': 'd'}, **changes)}

    def test_matching_record_clears_flag(self):
        """An accepted record for the current audio removes the flag."""
        issues, status = AUDIT.apply_listening_acceptance('c', 'h', [FLAG], self.record())
        self.assertEqual(issues, [])
        self.assertIn('accepted by r', status)

    def test_changed_audio_voids_record(self):
        """A record for older audio leaves the flag in place."""
        issues, status = AUDIT.apply_listening_acceptance('c', 'new', [FLAG], self.record())
        self.assertEqual(issues, [FLAG])
        self.assertIn('pending', status)

    def test_rejected_record_keeps_flag(self):
        """A rejected decision never clears the flag."""
        self.assertEqual(AUDIT.apply_listening_acceptance('c', 'h', [FLAG], self.record(decision='rejected'))[0], [FLAG])

    def test_other_issues_survive(self):
        """Acceptance cannot clear technical failures such as missing audio."""
        self.assertEqual(AUDIT.apply_listening_acceptance('c', 'h', [FLAG, 'missing audio'], self.record())[0], ['missing audio'])


if __name__ == '__main__':
    unittest.main()
