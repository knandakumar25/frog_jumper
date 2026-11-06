"""Offline localization contracts for coverage, formatting and unchanged English speech."""
import hashlib
import importlib.util
import json
from pathlib import Path
import sys
import unittest
import tempfile

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT/'tools/localization'))
import catalog


class WrittenLocalizationTests(unittest.TestCase):
    """Check source/format/recording invariants without claiming translations have native-language approval."""

    def test_tokyo_and_mission_cards_have_every_target_cell(self):
        """Authored Tokyo subtitles and all twelve live mission cards must stay structurally complete."""
        languages, rows = catalog.read_catalog()
        data = {row['']: row for row in rows}
        keys = [f'TOKYO_CUE_{i}' for i in range(1, 7)] + ['TOKYO_SKIP']
        keys += [f'MISSION_{i:02d}_{field}' for i in range(1, 13) for field in ('TITLE', 'BRIEFING')]
        for key in keys:
            for language in languages:
                with self.subTest(key=key, language=language):
                    self.assertTrue(data[key][language].strip())
                    self.assertEqual(catalog.placeholders(data[key]['english']), catalog.placeholders(data[key][language]))

    def test_subtitle_keys_are_derived_from_callers(self):
        """Palace logs cannot resolve to signs; briefing speech starts with the actual spoken introduction."""
        cues = {cue['id']: cue for cue in catalog.verified_cues()}
        self.assertEqual(cues['m01.fragment1']['subtitle_key'], 'SUBTITLE_M01_FRAGMENT1')
        self.assertEqual(cues['briefing.1']['subtitle_key'], 'STORY_LINE_1_INTRO')
        self.assertEqual(cues['cinematic.cyber_grave_intro.02']['subtitle_key'], 'CYBER_GRAVE_CUE_1')
        bindings = json.loads((ROOT/'assets/translations/subtitle_bindings.json').read_text(encoding='utf-8'))
        self.assertEqual(bindings, {key: value['subtitle_key'] for key, value in cues.items()})
        self.assertTrue(all('{' not in value for value in bindings.values()))

    def test_english_audio_bytes_are_unchanged(self):
        """Every shipped English track matches the baseline captured before localization edits."""
        baseline = json.loads((ROOT/'docs/localization/english_audio_baseline.json').read_text(encoding='utf-8'))
        self.assertTrue(baseline)
        for path, expected in baseline.items():
            with self.subTest(path=path):
                self.assertEqual(hashlib.sha256((ROOT/path).read_bytes()).hexdigest(), expected)

    def test_original_english_catalog_sources_are_unchanged(self):
        """Keep the pre-localization English script/UI sources while adding new exact-source keys."""
        baseline = json.loads((ROOT/'docs/localization/original_english_catalog.json').read_text(encoding='utf-8'))
        _, rows = catalog.read_catalog()
        actual = {row['']: row['english'] for row in rows}
        for key, source in baseline.items():
            self.assertEqual(actual[key], source, key)

    def test_audio_cue_order_words_and_intervals_are_unchanged(self):
        """Compare current offline callers with the pre-localization voice evidence; only subtitle keys may change."""
        sys.path.insert(0, str(ROOT/'tools/gameplay'))
        from audit_english_voice import all_cues
        previous = json.loads((ROOT/'docs/voice-coverage/coverage.json').read_text(encoding='utf-8'))['cues']
        current = all_cues()
        self.assertEqual(len(previous), len(current))
        for old, new in zip(previous, current, strict=True):
            for field in ('id', 'audio_path', 'text', 'start', 'end'):
                with self.subTest(cue=old['id'], field=field):
                    self.assertEqual(old.get(field), new.get(field))

    def test_narrative_files_reference_only_the_twelve_live_cards(self):
        """Four newly added locales contain card metadata without invented dialogue events."""
        languages, _ = catalog.read_catalog()
        for language in languages:
            data = json.loads((ROOT/f'assets/narrative/{language}.json').read_text(encoding='utf-8'))
            self.assertEqual(set(data['mission_cards']), {str(i) for i in range(1, 13)})
            if language in ('traditionalchinese', 'indonesian', 'vietnamese', 'thai'):
                self.assertEqual(data['levels'], {})

    def test_report_never_passes_known_missing_cells(self):
        """Coverage cannot report success merely because English fallbacks are readable."""
        data = catalog.report()
        gaps = any(row['missing'] or row['malformed'] for row in data['languages'].values())
        if gaps or data['unbound_display_candidates']:
            self.assertFalse(data['structural_pass'])

    def test_shipped_required_catalog_has_no_translation_gaps(self):
        """Prevent an empty cell, unreviewed English equality or damaged field from silently returning to the client."""
        data = catalog.report()
        for language, coverage in data['languages'].items():
            with self.subTest(language=language):
                self.assertEqual(coverage['missing'], [])
                self.assertEqual(coverage['malformed'], [])
                self.assertEqual(coverage['identical_requires_review'], [])
        self.assertTrue(data['structural_pass'])

    def test_translations_do_not_drop_a_leading_label_or_sentence(self):
        """Catch the draft-provider failure that left a bare colon or sentence stop after dropping source words."""
        languages, rows = catalog.read_catalog()
        for row in rows:
            source = row['english']
            if not source or not source[0].isalnum():
                continue
            for language in languages[1:]:
                target = row[language].lstrip()
                with self.subTest(source=source, language=language):
                    self.assertFalse(target.startswith((':', '.', ';', '。', '،')))

    def test_login_results_and_all_twelve_missions_are_inventory_roots(self):
        """Direct-play/previews remain localization requirements even without a literal main-scene reference."""
        _, reachable = catalog.reachable_sources()
        self.assertIn('scenes/main_menu/login_scene.tscn', reachable)
        self.assertIn('scenes/ui/cyber_victory_scorecard.tscn', reachable)
        for number in range(1, 13):
            self.assertIn(f'scenes/gameplay/level{number}.tscn', reachable)

    def test_seeded_login_scene_labels_stay_required(self):
        """Catalog existence cannot hide English-only native Label text from the next coverage run."""
        data = catalog.report()
        for source in ('// RECLAIM EARTH - SECURE AUTHENTICATION TERMINAL',
                       'Access tactical databases, synchronize operative sessions, and deploy to active operational sectors across Earth.'):
            entry = next(item for item in data['entries'] if item['english'] == source)
            self.assertTrue(entry['required'], source)

    def test_syntax_inventory_excludes_lookup_key_templates_and_colors(self):
        """Translation-key construction and render colors must never be localized as visible prose."""
        import re
        rows = json.loads((ROOT/'docs/localization/syntax_inventory.json').read_text(encoding='utf-8'))
        for row in rows:
            with self.subTest(key=row['key']):
                self.assertFalse(re.fullmatch(r'#?[0-9a-fA-F]{6}(?:[0-9a-fA-F]{2})?', row['english']))
                self.assertFalse('_' in row['english'] and re.fullmatch(r'[A-Z0-9_]+(?:\{[^{}]+\}[A-Z0-9_]*)+', row['english']))
                self.assertFalse(re.match(r'\s*(?:SELECT\s.+\sFROM\b|INSERT\s+INTO\b|UPDATE\s+\w+\s+SET\b|DELETE\s+FROM\b|CREATE\s+TABLE\b)', row['english'], re.I | re.S))

    def test_authoring_import_rejects_changed_fields_before_catalog_writes(self):
        """A broken submitted translation must fail before any production catalog is opened for writing."""
        from build_assets import load_authored_sources
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'spanish.json'
            path.write_text(json.dumps({'language': 'spanish', 'translations': {'Score {count}': 'Puntos {total}'}}), encoding='utf-8')
            with self.assertRaisesRegex(ValueError, 'format mismatch'):
                load_authored_sources(['english', 'spanish'], directory)
            path.write_text(json.dumps({'language': 'spanish', 'translations': {'Score {count}': 'Puntos {count}'}}), encoding='utf-8')
            self.assertEqual(load_authored_sources(['english', 'spanish'], directory)['spanish']['Score {count}'], 'Puntos {count}')

    def test_draft_token_round_trip_preserves_numbers_names_and_line_breaks(self):
        """Draft transport cannot change controls, runtime fields, named identities, numbers or intentional newlines."""
        from prepare_drafts import protect, restore, meaningful
        source = 'SURYA: [Q] {count} / 17\nShield 10%'
        protected, values = protect(source, {'SURYA': 'Surya'})
        self.assertEqual(restore(protected, values), source.replace('SURYA', 'Surya'))
        with self.assertRaisesRegex(ValueError, 'Protected token'):
            restore(protected.replace('ZXP0000XZ', ''), values)
        self.assertFalse(meaningful('+16% Shockwave Radius', '+16%', {}))
        self.assertFalse(meaningful('FINAL SCORE: {0} PTS', ': {0} PTS', {}))
        self.assertFalse(meaningful('Save could not be confirmed. Retry Save.', '. Retry Save.', {}))
        self.assertTrue(meaningful('[ AWAITING INPUT... ]', '[ ESPERANDO ENTRADA... ]', {}))


if __name__ == '__main__':
    unittest.main()
