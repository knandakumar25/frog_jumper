"""Offline written-localization inventory and structural gate; never calls translation or speech services.

Reports distinguish required bound strings from conservative literal-review candidates.
Run with --write to refresh review artifacts, or --check to return nonzero for gaps.
The CSV remains the authoritative catalog; this tool does not manufacture translations.
"""
import argparse
import collections
import csv
import hashlib
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
CSV = ROOT / 'assets/translations/Translations - Sheet1.csv'
REPORT = ROOT / 'docs/localization'
LITERAL = re.compile(r'//[^\n]*|/\*.*?\*/|(?P<string>\$?"(?:\\.|[^"\\])*")', re.S)


def read_catalog():
    """Read actual CSV columns, including empty cells, without silently dropping duplicate keys."""
    with CSV.open(encoding='utf-8-sig', newline='') as handle:
        reader = csv.DictReader(handle)
        rows = list(reader)
        return reader.fieldnames[1:], rows


def quoted(source):
    """Yield ordinary C#/scene string values and offsets, ignoring comments and interpolated expressions."""
    for match in LITERAL.finditer(source):
        token = match['string']
        if not token or token.startswith('$'):
            continue
        try:
            yield json.loads(token), match.start()
        except json.JSONDecodeError:
            continue


def placeholders(text):
    """Keep named/.NET placeholders, printf slots, BBCode and line breaks as separate structural contracts."""
    return dict(fields=sorted(re.findall(r'(?<!\{)\{[\w]+(?::[^{}]+)?\}(?!\})', text)),
                printf=sorted(re.findall(r'%(?:\d+\$)?[sdif]', text)),
                bbcode=re.findall(r'\[/?(?:center|color|b|i|u|font|url)(?:=[^\]]+)?\]', text),
                newlines=text.count('\n'))


def reachable_sources():
    """Follow resources and project class identifiers from main scene/autoloads; report unreachable sources separately."""
    files = {p.relative_to(ROOT).as_posix(): p for folder in ('scripts', 'scenes')
             for p in (ROOT/folder).rglob('*') if p.suffix in ('.cs', '.gd', '.tscn')}
    classes = {p.stem: key for key, p in files.items() if p.suffix == '.cs'}
    project = (ROOT/'project.godot').read_text(encoding='utf-8-sig')
    pending = re.findall(r'res://([^"\s]+\.(?:cs|gd|tscn))', project)
    # Menus, results and cinematic previews can be opened through UI routing or
    # editor/direct play without a literal reference from the main scene. They
    # remain shipped client surfaces and must never disappear from the gate.
    pending.extend(name for name in files if name.endswith('.tscn') and
                   (name.startswith(('scenes/main_menu/', 'scenes/ui/', 'scenes/cutscenes/'))
                    or re.fullmatch(r'scenes/gameplay/level\d+\.tscn', name)))
    visited = set()
    while pending:
        key = pending.pop()
        if key in visited or key not in files:
            continue
        visited.add(key)
        source = files[key].read_text(encoding='utf-8-sig')
        pending.extend(re.findall(r'res://([^"\s]+\.(?:cs|gd|tscn))', source))
        pending.extend(classes[word] for word in set(re.findall(r'\b\w+\b', source)) if word in classes)
    return files, visited


def inventory():
    """Collect catalog, bound fallbacks, spoken cues and unbound display candidates with reviewable source evidence."""
    languages, rows = read_catalog()
    entries = {}
    for row in rows:
        entries.setdefault(row[''], dict(key=row[''], english=row['english'], locations=[],
                                        trigger='catalog reference', subsystem='interface', required=False))
    files, reachable = reachable_sources()
    for name in sorted(reachable):
        source = files[name].read_text(encoding='utf-8-sig')
        # Direct key/fallback pairs are authoritative only inside the same call.
        # Browser status/button keys flow through event callbacks rather than
        # literal Tr calls. Keep every referenced key inside the normal gate.
        for value, offset in quoted(source):
            if value.startswith('AUTH_') and value in entries:
                entries[value]['required'] = True
                entries[value]['locations'].append(dict(source=name, line=source[:offset].count('\n')+1))
        for match in re.finditer(r'(?:WildfireText\.(?:Get|Format|KeyValue)|Localized)\(\s*"([^"{}]+)"\s*,\s*("(?:\\.|[^"\\])*")', source):
            key, english = match[1], json.loads(match[2])
            entry = entries.setdefault(key, dict(key=key, english=english, locations=[], trigger='runtime fallback call',
                                                 subsystem='gameplay' if '/gameplay/' in name else 'interface', required=True))
            entry['required'] = True
            entry['locations'].append(dict(source=name, line=source[:match.start()].count('\n')+1))
        for match in re.finditer(r'(?:\bTr|\btr|\bTranslate)\(\s*"([^"{}]+)"', source):
            key = match[1]
            entry = entries.setdefault(key, dict(key=key, english=key, locations=[], trigger='translation call',
                                                 subsystem='interface', required=True))
            entry['required'] = True
            entry['locations'].append(dict(source=name, line=source[:match.start()].count('\n')+1))
    # Roslyn understands nested interpolation and excludes logging/resource IDs.
    # Static source aliases use native Label/Button auto-translation; dynamic
    # sentences are explicitly bound through WildfireText.Interpolate.
    syntax_path = REPORT/'syntax_inventory.json'
    known_sources = {entry['english']: key for key, entry in entries.items()}
    if syntax_path.exists():
        for row in json.loads(syntax_path.read_text(encoding='utf-8')):
            if row['source'] not in reachable:
                continue
            key = known_sources.get(row['english'], row['key']) if row['kind'] == 'source alias' else row['key']
            entry = entries.setdefault(key, dict(key=key, english=row['english'], locations=[],
                                                 trigger=row['kind'], subsystem='gameplay' if '/gameplay/' in row['source'] else 'interface', required=True))
            entry['required'] = True
            entry['locations'].append(dict(source=row['source'], line=row['line'], binding=row['kind']))
            known_sources.setdefault(row['english'], key)
    for name in sorted(reachable):
        if not name.endswith(('.tscn', '.gd')):
            continue
        source = files[name].read_text(encoding='utf-8-sig')
        for value, offset in quoted(source):
            prefix = source[max(0, offset-80):offset]
            if re.search(r'(?:\btext|\btooltip_text|\bplaceholder_text)\s*=\s*$', prefix) and re.search(r'[A-Za-z]{2}', value):
                digest = hashlib.sha256(value.encode()).hexdigest()[:8].upper()
                key = value if value in entries else known_sources.get(value, f'SCENE_{Path(name).stem.upper()}_TEXT_{digest}')
                entry = entries.setdefault(key, dict(key=key, english=value, locations=[],
                                                     trigger='native source alias', subsystem='interface', required=True))
                # Seeding a row must never make its still-visible scene source
                # optional on the next run merely because its English is known.
                entry['required'] = True
                entry['locations'].append(dict(source=name, line=source[:offset].count('\n')+1))
                known_sources[value] = key
    cues = verified_cues(files)
    narrative = json.loads((ROOT/'assets/narrative/english.json').read_text(encoding='utf-8-sig'))
    for number, card in narrative.get('mission_cards', {}).items():
        for key in card.values():
            entry = entries.setdefault(key, dict(key=key, english=key, locations=[], trigger=f'mission {number} title card',
                                                 subsystem='mission cards', required=True))
            entry['required'] = True
            entry['subsystem'] = 'mission cards'
            entry['locations'].append(dict(source='assets/narrative/english.json', mission=number))
    for cue in cues:
        key = cue['subtitle_key']
        entry = entries.setdefault(key, dict(key=key, english=cue['display_text'], locations=[],
                                             trigger=cue['trigger'], subsystem='subtitles', required=True))
        entry['required'] = True
        entry['subsystem'] = 'subtitles'
        entry['locations'].append(dict(source=cue['source'], cue=cue['id']))
    english_to_key = {entry['english']: key for key, entry in entries.items()}
    candidates = []
    for name in sorted(reachable):
        source = files[name].read_text(encoding='utf-8-sig')
        for value, offset in quoted(source):
            if value in entries:
                entries[value]['required'] = True
            if value in english_to_key or value in entries or not re.search(r'[A-Za-z]{3}', value):
                continue
            prefix = source[max(0, offset-100):offset]
            if value in ('Text', 'MainText', 'TooltipText', 'PlaceholderText') and re.search(r'WildfireText\.(?:SetText|Bind)\(', prefix):
                continue
            visible = re.search(r'(?:\bText|\bTooltipText|\bPlaceholderText|\btext)\s*=\s*$', prefix)
            visible = visible or re.search(r'(?:ShowDialogue|ShowPrompt|DrawString|NewLabel|SetText)\([^;]*$', prefix)
            # Retain candidate evidence; classification requires human review rather than claiming regex reachability proof.
            if visible and not value.startswith(('res://', '#')) and not re.fullmatch(r'[A-Z0-9_]+_', value) and (len(value.split()) > 1 or value[:1].isupper()):
                candidates.append(dict(source=name, line=source[:offset].count('\n')+1, english=value,
                                       status='needs runtime binding/review'))
    return languages, rows, list(entries.values()), cues, candidates, sorted(set(files)-reachable)


def verified_cues(files=None):
    """Derive cinematic keys from actual Lines[index] expressions; never inherit an unrelated preceding fallback key."""
    historical = json.loads((ROOT/'docs/voice-coverage/coverage.json').read_text(encoding='utf-8'))['cues']
    result = []
    for item in historical:
        cue = {k: item[k] for k in ('id', 'source', 'trigger', 'classification', 'audio_path')}
        cue['display_text'] = item.get('display_text', item['text'])
        source = (ROOT/item['source']).read_text(encoding='utf-8-sig')
        key = item.get('subtitle_key')
        if item['id'].startswith('cinematic.'):
            index = int(item['id'].rsplit('.', 1)[1])-1
            mapping = re.search(r'(?:Localized|WildfireText.Get)\(\$"([^"\n]+)"\s*,\s*Lines', source)
            if mapping:
                key = re.sub(r'\{(?:cue|index|cueIndex)\s*(?:\+\s*(\d+))?(?::D(\d+))?\}',
                             lambda m: str(index+int(m[1] or 0)).zfill(int(m[2] or 0)), mapping[1])
            else:
                key = 'SUBTITLE_' + re.sub(r'[^A-Za-z0-9]+', '_', item['id']).upper()
        elif item['id'].startswith('m'):
            # The former heuristic incorrectly assigned PALACE_ARCHIVE_ROOF to unrelated fragment logs.
            # Exact paired fallback calls are usable; other gameplay lines receive cue-owned semantic keys.
            matching = []
            for match in re.finditer(r'WildfireText.Get\(\s*"([^"{}]+)"\s*,\s*("(?:\\.|[^"\\])*")', source):
                if json.loads(match[2]) == cue['display_text']:
                    matching.append(match[1])
            key = matching[0] if matching else 'SUBTITLE_' + re.sub(r'[^A-Za-z0-9]+', '_', item['id']).upper()
            if '.fragment' in item['id']:
                method = re.search(r'protected override string FragmentLog\([^)]*\)(.*?)(?=\n\s*///)', source, re.S)
                dynamic = re.search(r'WildfireText.Get\(\$"([^"\n]+)"', method[1]) if method else None
                if dynamic:
                    key = dynamic[1].replace('{found}', item['id'].rsplit('fragment', 1)[1])
        elif item['id'] == 'briefing.1':
            key = 'STORY_LINE_1_INTRO'
        cue['subtitle_key'] = key or 'SUBTITLE_' + re.sub(r'[^A-Za-z0-9]+', '_', item['id']).upper()
        cue['previous_inventory_key'] = item.get('subtitle_key')
        result.append(cue)
    return result


def report():
    """Validate required cells/formatting while keeping linguistic review and unbound candidates visibly separate."""
    languages, rows, entries, cues, candidates, unused = inventory()
    catalog = {row['']: row for row in rows}
    duplicates = [key for key, count in collections.Counter(row[''] for row in rows).items() if count > 1]
    errors = []
    totals = {}
    exemptions = json.loads(Path(__file__).with_name('exemptions.json').read_text(encoding='utf-8'))
    for language in languages:
        missing, malformed, identical, shared = [], [], [], []
        for entry in entries:
            if not entry['required']:
                continue
            value = catalog.get(entry['key'], {}).get(language, '')
            if not value.strip():
                missing.append(entry['key'])
            elif placeholders(value) != placeholders(entry['english']):
                malformed.append(entry['key'])
            elif language != 'english' and value == entry['english']:
                if entry['key'] in exemptions['proper_names']['keys'] or language in exemptions['shared_words'].get(entry['key'], []):
                    shared.append(entry['key'])
                else:
                    identical.append(entry['key'])
        totals[language] = dict(required=sum(e['required'] for e in entries), missing=missing,
                                malformed=malformed, identical_requires_review=identical, explained_shared=shared)
        if missing or malformed or identical:
            errors.append(language)
    for entry in entries:
        entry['formatting'] = placeholders(entry['english'])
        entry['coverage'] = {lang: bool(catalog.get(entry['key'], {}).get(lang, '').strip()) for lang in languages}
    subsystems = {}
    for subsystem in sorted({entry['subsystem'] for entry in entries}):
        required = [entry for entry in entries if entry['required'] and entry['subsystem'] == subsystem]
        subsystems[subsystem] = {language: dict(required=len(required), missing=sum(not row['coverage'][language] for row in required))
                                 for language in languages}
    unused_narrative = {}
    for language in languages:
        data = json.loads((ROOT/f'assets/narrative/{language}.json').read_text(encoding='utf-8-sig'))
        unused_narrative[language] = dict(dialogue_events=sum(len(level.get('dialogue', [])) for level in data.get('levels', {}).values()),
                                         reason='No runtime callers of NarrativeManager.get_dialogue/get_monologues/get_arc; metadata is not speech.')
    return dict(structural_pass=not errors and not duplicates and not candidates,
                languages=totals, duplicate_keys=duplicates, entries=entries, subtitle_mappings=cues,
                subsystems=subsystems, unused_narrative_events=unused_narrative,
                unbound_display_candidates=candidates, unreachable_sources=unused,
                limitations=['Literal inventory is conservative; visual/artwork and native-language review remain required.',
                             'Unused narrative dialogue is not runtime dialogue and is not counted as translated speech.'])


def write_reports(data):
    """Produce deterministic JSON, readable coverage and a TSV authoring queue without changing the catalog."""
    REPORT.mkdir(parents=True, exist_ok=True)
    _, reachable = reachable_sources()
    (REPORT/'reachable_sources.json').write_text(json.dumps(sorted(reachable), indent=2)+'\n', encoding='utf-8')
    (REPORT/'coverage.json').write_text(json.dumps(data, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    lines = ['# Written localization coverage', '',
             'Structural gate: **'+('PASS' if data['structural_pass'] else 'FAIL — incomplete')+'**.', '',
             '| Language | Required | Missing | Format mismatches | Identical: review |',
             '| --- | ---: | ---: | ---: | ---: |']
    for language, row in data['languages'].items():
        lines.append(f"| {language} | {row['required']} | {len(row['missing'])} | {len(row['malformed'])} | {len(row['identical_requires_review'])} |")
    lines += ['', f"Unbound display candidates: {len(data['unbound_display_candidates'])}.", '',
              'Shared words and proper-name exceptions are explicitly listed in tools/localization/exemptions.json.',
              'This is structural evidence, not native-speaker approval. See coverage.json for per-key source and trigger details.']
    (REPORT/'coverage.md').write_text('\n'.join(lines)+'\n', encoding='utf-8')
    with (REPORT/'authoring.tsv').open('w', encoding='utf-8', newline='') as handle:
        writer = csv.writer(handle, delimiter='\t')
        writer.writerow(['key', 'english', 'subsystem', 'required'])
        writer.writerows((row['key'], row['english'], row['subsystem'], row['required']) for row in data['entries'])


def main():
    """Refresh artifacts on request and return an honest failing acceptance gate while work remains."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--write', action='store_true')
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    data = report()
    if args.write:
        write_reports(data)
    print(json.dumps(dict(pass_=data['structural_pass'], entries=len(data['entries']),
                          cues=len(data['subtitle_mappings']), unbound=len(data['unbound_display_candidates']))))
    if args.check and not data['structural_pass']:
        raise SystemExit(1)


if __name__ == '__main__':
    main()
