"""Offline regression checks for reachable English cue coverage and exact new recording receipts.

Historical listening/provenance exceptions remain visible in the stricter coverage
report; these tests enforce inventory structure and the repaired playable resources.
"""
import json
from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools/gameplay'))
from audit_english_voice import all_cues, normalize
from english_voice_coverage import sha, read, OUT


class EnglishVoiceInventoryTests(unittest.TestCase):
    """Protect campaign reachability, stable IDs and exact paid-generation provenance."""

    def test_transcript_normalization_preserves_meaningful_word_and_unit_differences(self):
        """Numeric typography is equivalent; missing units, polarity and different authored words must still fail review."""
        self.assertEqual(normalize('Elevation 1,200m.'),normalize('Elevation twelve hundred meters.'))
        self.assertEqual(normalize('AXIOM-7'),normalize('Axiom Seven'))
        for expected,actual in [('route','road'),('flak','flat'),('Border','Order'),('minus 400 meters','400 meters'),('1200 meters','1200')]:
            self.assertNotEqual(normalize(expected),normalize(actual))

    def test_every_mission_has_three_optional_spoken_cues(self):
        """All twelve playable scenes expose exactly three uniquely identified testimony/log recordings."""
        rows = all_cues()
        for mission in range(1,13):
            fragments = [r for r in rows if r['id'].startswith(f'm{mission:02d}.fragment')]
            self.assertEqual(3,len(fragments),mission)
            self.assertTrue(all(r['classification']=='speech' for r in fragments))

    def test_ids_are_unique_and_cinematic_windows_do_not_overlap(self):
        """Stable IDs cannot alias different lines, and nonempty runtime subtitles own nonoverlapping intervals."""
        rows = all_cues()
        self.assertEqual(len(rows),len({r['id'] for r in rows}))
        for path in {r['audio_path'] for r in rows if 'start' in r}:
            cues = sorted([r for r in rows if r['audio_path']==path],key=lambda r:r['start'])
            for index,cue in enumerate(cues):
                self.assertLess(cue['start'],cue['end'])
                if index:
                    self.assertLessEqual(cues[index-1]['end'],cue['start'])

    def test_new_recordings_match_exact_text_and_audio_receipts(self):
        """No missing gameplay MP3 or stale voice/text/recording hash can silently pass the inventory checks."""
        receipts = read(OUT/'speech_provenance.json')
        for cue in read(OUT/'cues.json'):
            if cue['classification']!='speech' or not cue['audio_path'].startswith('assets/audio/english/'):
                continue
            with self.subTest(cue=cue['id']):
                receipt = receipts[cue['id']]
                self.assertEqual(cue['text'],receipt['text'])
                self.assertEqual(cue['voice_id'],receipt['voice_id'])
                self.assertEqual(cue['text_sha256'],sha(cue['text'].encode()))
                self.assertEqual(receipt['audio_sha256'],sha((ROOT/cue['audio_path']).read_bytes()))

    def test_all_campaign_arrivals_and_aftermaths_are_inventoried(self):
        """External scene navigation must retain all seven later arrival and aftermath speech families."""
        paths = {r['audio_path'] for r in all_cues()}
        for family in ['cyber_grave','foundry_forge','stratos_docks','spire_outer_hull','cyber_nexus','apex_bastion','judgment_day']:
            for phase in ['intro','aftermath']:
                self.assertIn(f'assets/cutscenes/{family}_{phase}/dialogue.ogg',paths)


if __name__=='__main__':
    unittest.main()
