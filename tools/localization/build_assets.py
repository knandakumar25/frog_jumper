"""Materialize deterministic subtitle/source bindings and offline-authored Tokyo cells.

Only --seed-english adds discovered source rows to the main CSV. Missing target
cells stay empty and fail the coverage gate; English is never presented as a translation.
"""
import argparse
import csv
import hashlib
import json
import re
from pathlib import Path
from catalog import ROOT, CSV, REPORT, inventory, read_catalog, write_reports, report, placeholders


def write_json(path, value):
    """Write UTF-8 reviewable data with stable ordering and no platform-dependent escaping."""
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')


def build(seed_english=False):
    """Preserve catalog translations, import authored cells, and bind cue identities separately from audio paths."""
    languages, rows = read_catalog()
    by_key = {row['']: row for row in rows}
    card_sources = mission_card_sources()
    common_sources = {'HUD_OBJECTIVE': 'OBJECTIVE / {objective}',
                      'HUD_FRAGMENTS': 'MEMORY FRAGMENTS {found} / {total}',
                      'MISSION_HEADING': 'MISSION {number}: {title}',
                      'CONTINUE_TO_ACT_II': 'CONTINUE TO ACT II',
                      'CONTINUE_TO_ACT_III': 'SECTOR CLEARED // ACT II COMPLETE',
                      'CITY_VOICE_NEXT': 'NEXT: SHATTERED AEGIS', **card_sources}
    for key, source in common_sources.items():
        if key not in by_key:
            row = {column: '' for column in ['', *languages]}
            row[''], row['english'] = key, source
            rows.append(row)
            by_key[key] = row
        elif by_key[key]['english'] == key and source != key:
            # Previously discovered symbolic lookups had no English catalog cell.
            # Use their actual authored fallback instead of translating a key name.
            by_key[key]['english'] = source
    authored = json.loads(Path(__file__).with_name('tokyo.json').read_text(encoding='utf-8'))
    for language, values in authored.items():
        for index, value in enumerate(values):
            key = f'TOKYO_CUE_{index+1}' if index < 6 else 'TOKYO_SKIP'
            if not by_key[key].get(language, '').strip():
                by_key[key][language] = value
    interface = json.loads(Path(__file__).with_name('interface.json').read_text(encoding='utf-8'))
    for language in languages[1:]:
        for key, value in zip(interface['keys'], interface[language], strict=True):
            if not by_key[key].get(language, '').strip():
                by_key[key][language] = value
    briefings = json.loads(Path(__file__).with_name('briefings.json').read_text(encoding='utf-8'))
    for language, values in briefings.items():
        for number, value in enumerate(values, 1):
            key = f'MISSION_{number:02d}_BRIEFING'
            if not by_key[key].get(language, '').strip():
                by_key[key][language] = value
    # Human-readable authoring files can be prepared independently by locale.
    # Validate all contracts before opening the CSV, then fill empty cells only.
    # Exact source identity permits reuse across aliases without fuzzy matching.
    authored_sources = load_authored_sources(languages)
    fixes = json.loads(Path(__file__).with_name('fixes.json').read_text(encoding='utf-8'))
    for key, values in fixes.items():
        for language, value in values.items():
            by_key[key][language] = value
    _, _, entries, cues, _, _ = inventory()
    if seed_english:
        for entry in entries:
            if entry['required'] and entry['key'] not in by_key:
                row = {column: '' for column in ['', *languages]}
                row[''], row['english'] = entry['key'], entry['english']
                rows.append(row)
                by_key[entry['key']] = row
    # Seeded rows participate in this same import; a second invocation must not
    # be necessary to materialize their already-authored translations.
    for row in rows:
        for language, translations in authored_sources.items():
            if not row.get(language, '').strip() and row['english'] in translations:
                row[language] = translations[row['english']]
    # Reuse only exact English sentence matches; no fuzzy matching of retired
    # narrative prose or word-by-word machine substitutions is allowed.
    groups = {}
    for row in rows:
        groups.setdefault(row['english'], []).append(row)
    for group in groups.values():
        for language in languages[1:]:
            available = next((row[language] for row in group if row.get(language, '').strip()), '')
            if available:
                for row in group:
                    if not row.get(language, '').strip():
                        row[language] = available
    with CSV.open('w', encoding='utf-8', newline='') as handle:
        writer = csv.DictWriter(handle, fieldnames=['', *languages])
        writer.writeheader()
        writer.writerows(rows)
    bindings = {cue['id']: cue['subtitle_key'] for cue in cues}
    write_json(CSV.parent/'subtitle_bindings.json', bindings)
    # English source aliases allow Godot's native auto-translation to update static
    # labels without mutating their English source or ever translating user input.
    aliases = {}
    for row in rows:
        if row['english'] and row['english'] != row['']:
            aliases.setdefault(row['english'], row[''])
    write_json(CSV.parent/'source_aliases.json', aliases)
    write_mission_cards(languages)
    baseline = REPORT/'english_audio_baseline.json'
    if not baseline.exists():
        write_json(baseline, {path: hashlib.sha256((ROOT/path).read_bytes()).hexdigest()
                             for path in sorted({cue['audio_path'] for cue in cues}) if (ROOT/path).exists()})
    write_reports(report())


def load_authored_sources(languages, directory=None):
    """Load locale-specific exact-source translations, rejecting conflicts and changed format contracts."""
    result = {}
    folder = Path(directory) if directory is not None else Path(__file__).with_name('authored')
    for path in sorted(folder.glob('*.json')):
        data = json.loads(path.read_text(encoding='utf-8'))
        language = data['language']
        if language == 'english' or language not in languages:
            raise ValueError(f'{path.name}: unknown target language {language}')
        target = result.setdefault(language, {})
        for source, translated in data['translations'].items():
            if not isinstance(translated, str) or not translated.strip():
                raise ValueError(f'{path.name}: empty translation for {source!r}')
            if placeholders(source) != placeholders(translated):
                raise ValueError(f'{path.name}: format mismatch for {source!r}')
            if source in target and target[source] != translated:
                raise ValueError(f'{path.name}: conflicting translation for {source!r}')
            target[source] = translated
    return result


def mission_card_sources():
    """Use the current twelve-mission catalog and actual authored briefing overrides, never retired story events."""
    catalog = (ROOT/'scripts/managers/LevelCatalog.cs').read_text(encoding='utf-8-sig')
    english = json.loads((ROOT/'assets/narrative/english.json').read_text(encoding='utf-8-sig'))
    sources = {}
    for match in re.finditer(r'new\((\d+), "([^"]+)", "res://([^"\n]+)"', catalog):
        number = int(match[1])
        scene = (ROOT/match[3]).read_text(encoding='utf-8-sig')
        script = re.search(r'path="res://(scripts/gameplay/[^"\n]+\.cs)"', scene)[1]
        source = (ROOT/script).read_text(encoding='utf-8-sig')
        sources[f'MISSION_{number:02d}_TITLE'] = match[2]
        briefing = re.search(r'protected override string TitleCardObjective\([^)]*\)\s*=>\s*(.*?);', source, re.S)
        if briefing:
            values = re.findall(r'"((?:\\.|[^"\\])*)"', briefing[1])
            objective = json.loads('"'+values[-1]+'"')
        elif number == 1:
            objective = english['levels']['1']['objective']
        else:
            # The final missions previously had absent/retired metadata. Reuse their
            # first actual objective verbatim rather than inventing narrative events.
            method = re.search(r'protected override string ComputeObjective\(\)\s*=>\s*Progress.Current switch\s*\{\s*"[^"\n]+"\s*=>\s*"([^"\n]+)"', source)
            objective = method[1] if method else ''
        sources[f'MISSION_{number:02d}_BRIEFING'] = objective
    return sources


def write_mission_cards(languages):
    """Add live card references in all locale files while preserving historical dialogue without declaring it reachable."""
    for language in languages:
        path = ROOT/f'assets/narrative/{language}.json'
        data = json.loads(path.read_text(encoding='utf-8-sig')) if path.exists() else {
            'meta': {'language': language, 'description': 'Mission card metadata; no additional story events.'},
            'levels': {}, 'acts': {}}
        data['mission_cards'] = {str(number): {'title_key': f'MISSION_{number:02d}_TITLE',
                                              'objective_key': f'MISSION_{number:02d}_BRIEFING'}
                                 for number in range(1, 13)}
        write_json(path, data)


def main():
    """Explicitly seed English source rows only when requested; never generate network translations."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--seed-english', action='store_true')
    build(parser.parse_args().seed_english)


if __name__ == '__main__':
    main()
