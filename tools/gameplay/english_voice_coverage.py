"""Inventory reachable English speech and produce honest, reproducible coverage evidence.

Inventory is offline. Generation and transcription require explicit CLI switches,
cache exact requests/responses, and stop on the first API failure without retrying.
Run media audit inside the existing cutscene container for FFmpeg availability.
"""
import argparse
import array
import difflib
import hashlib
import json
import math
from pathlib import Path
import re
import subprocess
import sys
import urllib.request
import urllib.error

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'assets/audio/english'
REPORT = ROOT / 'docs/voice-coverage'
CAST = {'AXIOM-7': 'onwK4e9ZLuTAKqWW03F9', 'KAITO': 'pNInz6obpgDQGcFmaJgB',
        'OCEANUS': 'EXAVITQu4vr4xnSDxMaL', 'AURA': 'XrExE9yKIg1WjnnlVkGX',
        'TITAN': 'JBFqnCBsd6RMkjVDRZzb', 'MIRAGE': 'cjVigY5qzO86Huf0OWal',
        'SURYA': 'nPczCjzI2devNBz1zQrb', 'CIVILIAN TESTIMONY': 'pFZP5JQG7iQjIQuC4Bku'}
ALIASES = {'AURA PHOENICIA': 'AURA', 'APEX TITAN': 'TITAN', 'MIRAGE BLADE': 'MIRAGE',
           'CYBER SHOGUN': 'KAITO', 'SURYA PRIME': 'SURYA'}
SETTINGS = dict(stability=.70, similarity_boost=.75, style=.10, speed=1, use_speaker_boost=True)
LITERAL = re.compile(r'"((?:[^"\\]|\\.)*)"')


def read(path, default=None):
    """Read UTF-8 JSON without assuming historical manifests exist."""
    return json.loads(path.read_text(encoding='utf-8-sig')) if path.exists() else default


def write(path, value):
    """Write reviewable deterministic UTF-8 JSON, creating its parent if needed."""
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')


def sha(data):
    """Hash bytes for exact text, request, recording and transcript identity."""
    return hashlib.sha256(data).hexdigest()


def literals(text):
    """Return C# ordinary string contents and source offsets (no interpolated evaluation)."""
    return [(json.loads('"'+m[1]+'"'), m.start()) for m in LITERAL.finditer(text)]


def speaker_text(text):
    """Preserve log headings; remove only recognized character labels from spoken words."""
    if text == 'The dojo falls, but the resistance rises. Tokyo remembers.':
        return 'KAITO', text
    label, sep, body = text.partition(': ')
    label = ALIASES.get(label.upper(), label.upper())
    if sep and label in CAST:
        return label, body
    return 'AXIOM-7', text


def inventory():
    """Trace LevelCatalog scenes and their actual mission scripts, excluding retired narrative events."""
    rows = []
    catalog = (ROOT/'scripts/managers/LevelCatalog.cs').read_text()
    for match in re.finditer(r'new\((\d+), "([^"]+)", "res://([^"]+)"', catalog):
        mission, title, scene = int(match[1]), match[2], match[3]
        scene_text = (ROOT/scene).read_text()
        script = re.search(r'path="res://(scripts/gameplay/[^"]+\.cs)"', scene_text)[1]
        source = (ROOT/script).read_text(encoding='utf-8-sig')
        sections = []
        # Methods end at the next documentation block; string extraction below is deliberately scoped.
        for method, trigger in [('IntroLine', 'arrival'), ('FragmentLog', 'optional collectible')]:
            m = re.search(r'protected override string '+method+r'\([^)]*\)(.*?)(?=\n\s*///)', source, re.S)
            if m:
                values = literals(m[1])
                if method == 'IntroLine':
                    values = values[-1:]
                else:
                    values = [(s, p) for s, p in values if re.search(r'(?:=>|:)\s*$', m[1][:p])]
                for n, (text, pos) in enumerate(values, 1):
                    sections.append((text, m.start(1)+pos, trigger, f'm{mission:02d}.intro' if method == 'IntroLine' else f'm{mission:02d}.fragment{n}'))
        for m in re.finditer(r'Lines\s*=\s*new\[\]\s*\{(.*?)\}', source, re.S):
            for text, pos in literals(m[1]):
                if ': ' in text:
                    sections.append((text, m.start(1)+pos, 'recruitment', None))
        for m in re.finditer(r'ShowDialogue\((.*?);', source, re.S):
            for text, pos in literals(m[1]):
                if len(text) > 35 and ('AXIOM-7:' in text or ': ' in text or text.startswith('The dojo falls')):
                    sections.append((text, m.start(1)+pos, 'story event', None))
        for text, pos, trigger, cue_id in sections:
            if any(r['mission'] == mission and r['display_text'] == text for r in rows):
                continue
            silent = text.startswith('SECTOR ') or text == 'AXIOM-7: Target secured. Proceed to the next objective.'
            speaker, spoken = speaker_text(text)
            cue_id = cue_id or f'm{mission:02d}.event.'+sha(text.encode())[:12]
            rows.append(dict(id=cue_id, mission=mission, mission_title=title, source=script,
                             source_line=source[:pos].count('\n')+1, trigger=trigger, speaker=speaker,
                             text=spoken, display_text=text, text_sha256=sha(spoken.encode()),
                             subtitle_key=None, audio_path=f'assets/audio/english/{cue_id}.mp3',
                             voice_id=CAST[speaker], model='eleven_multilingual_v2',
                             classification=('non-spoken sector title' if text.startswith('SECTOR ') else 'non-spoken objective status') if silent else 'speech'))
    # Mission-owned existing manifests already define exact speaker, key and clip.
    for folder, mission in [('aegis', 4), ('names', 5)]:
        for cue in read(ROOT/f'assets/audio/{folder}/cues.json', []):
            rows.append(dict(id=f'{folder}.{cue["id"]}', mission=mission, source=f'assets/audio/{folder}/cues.json',
                             trigger='mission scheduler', speaker=cue['speaker'], text=cue['text'],
                             display_text=cue['speaker']+': '+cue['text'], text_sha256=sha(cue['text'].encode()),
                             subtitle_key=cue.get('localization_key'), audio_path=f'assets/audio/{folder}/{cue["id"]}.ogg',
                             voice_id=cue['voice_id'], model=cue['model'], classification='speech'))
    # Recover localization aliases from authored calls, including interpolated fragment keys.
    for row in rows:
        if not row.get('source_line'):
            continue
        source = (ROOT/row['source']).read_text(encoding='utf-8-sig')
        prefix = source[:source.find('"'+row['display_text'].replace('"', '\\"')+'"')]
        key = re.search(r'WildfireText.Get\(\$?"([^"]+)"\s*,\s*$', prefix)
        if key:
            row['subtitle_key'] = key[1]
        elif '.fragment' in row['id']:
            method = source[source.index('protected override string FragmentLog'):]
            key = re.search(r'WildfireText.Get\(\$?"([^"]+)"', method)
            if key:
                row['subtitle_key'] = key[1].replace('{found}', row['id'][-1])
                if row['subtitle_key'].endswith('_'):
                    row['subtitle_key'] += row['id'][-1]
    write(OUT/'cues.json', rows)
    return rows


def key():
    """Reuse the established secret loader without exposing credentials in command lines."""
    sys.path.insert(0, str(ROOT/'tools/cutscene'))
    from narrate import api_key
    value = api_key()
    if not value:
        raise SystemExit('ELEVENLABS_API_KEY unavailable; no API requests made.')
    return value


def generate(rows):
    """Generate only missing gameplay recordings; exact receipts prevent duplicate paid requests."""
    secret = key()
    receipts = read(OUT/'speech_provenance.json', {})
    for cue in rows:
        if cue['classification'] != 'speech' or not cue['audio_path'].startswith('assets/audio/english/'):
            continue
        path = ROOT/cue['audio_path']
        receipt = receipts.get(cue['id'], {})
        spoken = receipt.get('input_text',cue['text']) if receipt.get('text')==cue['text'] else cue['text']
        body = dict(text=spoken, model_id=cue['model'], voice_settings=SETTINGS)
        digest = sha(json.dumps(body, sort_keys=True).encode()+cue['voice_id'].encode())
        if path.exists():
            if receipt.get('request_sha256') != digest or receipt.get('audio_sha256') != sha(path.read_bytes()):
                raise SystemExit('Existing recording differs from receipt: '+cue['id'])
            continue
        request = urllib.request.Request(f'https://api.elevenlabs.io/v1/text-to-speech/{cue["voice_id"]}?output_format=mp3_44100_128',
                    data=json.dumps(body).encode(), headers={'xi-api-key': secret, 'Content-Type': 'application/json'})
        try:
            with urllib.request.urlopen(request, timeout=120) as response:
                audio = response.read()
                request_id = response.headers.get('request-id')
        except urllib.error.HTTPError as error:
            write(REPORT/'generation_failure.json', dict(cue=cue['id'], http_status=error.code, retry=False))
            raise SystemExit(f'ElevenLabs HTTP {error.code} at {cue["id"]}; stopped without retry.') from None
        path.write_bytes(audio)
        receipts[cue['id']] = dict(text=cue['text'], text_sha256=cue['text_sha256'], voice_id=cue['voice_id'],
            model=cue['model'], settings=SETTINGS, request_sha256=digest, audio_sha256=sha(audio),
            request_id=request_id, source='ElevenLabs HTTPS TTS; complete natural-speed MP3')
        write(OUT/'speech_provenance.json', receipts)
        print('Generated', cue['id'], flush=True)


def main():
    """Select deterministic offline inventory or explicitly requested paid generation."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--generate', action='store_true')
    args = parser.parse_args()
    rows = inventory()
    print('Gameplay inventory:', len(rows), flush=True)
    if args.generate:
        generate(rows)


if __name__ == '__main__':
    main()
