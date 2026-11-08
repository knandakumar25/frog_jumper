"""Audit every reachable recording and cinematic interval; never equate file existence with verified words.

Use --transcribe on the host for cached ElevenLabs Scribe evidence. Use --media in
the cutscene container to decode all audio and measure each required cue window.
Neither mode generates speech or repairs based solely on ASR disagreements.
"""
import argparse
import array
import csv
import difflib
import json
import math
from pathlib import Path
import re
import subprocess
import io
import tarfile
import urllib.request
import urllib.error
from english_voice_coverage import ROOT, OUT, REPORT, CAST, read, write, sha, key, literals, speaker_text


def all_cues():
    """Combine LevelCatalog gameplay, reachable overlays, deployment and four briefing lines."""
    rows = read(OUT/'cues.json')
    reachable = (ROOT/'scripts/gameplay/CodeLevel.cs').read_text()
    reachable += ''.join(p.read_text(encoding='utf-8-sig') for p in (ROOT/'scripts/gameplay').glob('*Mission.cs'))
    # Menu arrivals are reached before the corresponding mission rather than from its own script.
    reachable += ''.join(p.read_text(encoding='utf-8-sig') for p in (ROOT/'scripts/main_menu').glob('*.cs'))
    # Follow resource references through scorecard/menu scenes and the central world launch catalog.
    # Code-built overlays are already named in the mission and shared base source above.
    # Static C# type calls do not contain a res:// path to their declaration, so name that routing root explicitly.
    pending = ['scenes/ui/cyber_victory_scorecard.tscn', 'scripts/world/WorldSave.cs']
    visited = set()
    while pending:
        resource = pending.pop()
        if resource in visited or not (ROOT/resource).is_file():
            continue
        visited.add(resource)
        text = (ROOT/resource).read_text(encoding='utf-8-sig')
        reachable += text
        pending.extend(p for p in re.findall(r'res://([^"\s]+\.(?:tscn|cs|gd))', text) if p not in visited)
    for path in sorted((ROOT/'scripts/ui').glob('*.cs')):
        source = path.read_text(encoding='utf-8-sig')
        track = re.search(r'SpeechPath.*?= "res://([^"]+)"', source)
        lines = re.search(r'string\[\] Lines =\s*\{(.*?)\};', source, re.S)
        starts = re.search(r'double\[\] Starts =\s*\{(.*?)\};', source, re.S)
        if not track or not lines or not starts or path.stem not in reachable:
            continue
        durations = re.search(r'const double Duration = ([\d.]+)', source)
        if not durations:
            continue
        times = [float(v.strip()) for v in starts[1].split(',') if v.strip()]
        folder = ROOT/track[1]
        authored = read(folder.parent/'cues.json', [])
        timing = read(folder.parent/'dialogue_timing.json', [])
        if isinstance(timing, dict):
            timing = timing.get('cues', [])
        provenance = read(folder.parent/'speech_provenance.json', [])
        spoken_index = 0
        for n, (display, pos) in enumerate(literals(lines[1])):
            if not display or n >= len(times):
                continue
            speaker, text = speaker_text(display)
            if 'SURYA // COMMS:' in display:
                speaker, text = 'SURYA', display.split(': ', 1)[1]
            cue = authored[spoken_index] if spoken_index < len(authored) else {}
            if ': ' in display:
                # Cinematic labels also include Voss, civilian names and comms aliases.
                text = display.split(': ', 1)[1]
                speaker = cue.get('speaker', speaker)
            speaker = speaker.replace(' // COMMS','')
            subtitle_key = None
            mapping = re.search(r'Localized\(\$"([^"]+)"\s*,\s*Lines',source)
            if mapping:
                subtitle_key = re.sub(r'\{cue\s*(?:\+\s*(\d+))?\}',lambda m:str(n+int(m[1] or 0)),mapping[1])
            receipt = provenance[spoken_index] if isinstance(provenance, list) and spoken_index < len(provenance) else {}
            measured = timing[spoken_index] if spoken_index < len(timing) else {}
            rows.append(dict(id='cinematic.'+folder.parent.name+f'.{n+1:02d}', mission=mission_for(folder.parent.name),
                source=path.relative_to(ROOT).as_posix(), source_line=source[:lines.start(1)+pos].count('\n')+1,
                trigger='cinematic', speaker=speaker, text=text, display_text=display,
                subtitle_key=subtitle_key, audio_path=track[1], start=times[n],
                end=times[n+1] if n+1 < len(times) else float(durations[1]),
                text_sha256=sha(text.encode()), voice_id=cue.get('voice_id', CAST.get(speaker)), model=cue.get('model','eleven_multilingual_v2'),
                manifest_text=cue.get('text'), provenance=receipt, timing=measured,
                classification='non-spoken next mission title' if display.startswith('NEXT:') else 'speech'))
            if not display.startswith('NEXT:'):
                spoken_index += 1
    deployment = (ROOT/'scripts/main_menu/deployment_cutscene.gd').read_text()
    for n, m in enumerate(re.finditer(r'\[([\d.]+), ([\d.]+), "([^"]+)"\]', deployment), 1):
        rows.append(dict(id=f'deployment.{n}', mission=0, source='scripts/main_menu/deployment_cutscene.gd',
            trigger='new game deployment', speaker='AXIOM-7', text=m[3], text_sha256=sha(m[3].encode()),
            subtitle_key=m[3], audio_path='assets/cutscenes/deployment/narration.ogg', start=float(m[1]), end=float(m[2]),
            voice_id=CAST['AXIOM-7'], classification='speech'))
    with (ROOT/'assets/translations/Translations - Sheet1.csv').open(encoding='utf-8-sig', newline='') as handle:
        english = {r['']:r['english'] for r in csv.DictReader(handle)}
    for n in range(1,5):
        text = english[f'STORY_LINE_{n}']
        if n == 1:
            # Transmission header is interface telemetry, not spoken dialogue.
            text = english['STORY_LINE_1_INTRO']
        rows.append(dict(id=f'briefing.{n}', mission=0, source='scripts/main_menu/StoryIntro.cs', trigger='briefing Continue',
            speaker='AXIOM-7', text=text, text_sha256=sha(text.encode()), subtitle_key=f'STORY_LINE_{n}',
            audio_path=f'assets/speech/AXIOM-7/english/AXIOM-7-{n}.mp3', voice_id=CAST['AXIOM-7'], classification='speech'))
    # Use the runtime subtitle map rather than historical inferred aliases.
    bindings = read(ROOT/'assets/translations/subtitle_bindings.json', {})
    for row in rows:
        row['subtitle_key'] = bindings.get(row['id'], row.get('subtitle_key'))
    return rows


def mission_for(folder):
    """Map cinematic resource families to the mission they introduce or conclude."""
    families = ['tokyo','wildfire','neon_rust','shattered_aegis','names_without_numbers','cyber_grave',
                'foundry_forge','stratos_docks','spire_outer_hull','cyber_nexus','apex_bastion','judgment_day']
    if folder == 'act_ii_opening':
        return 5
    return next((n for n, name in enumerate(families,1) if folder.startswith(name)), 0)


def transcribe(rows):
    """Cache unique full-recording Scribe transcripts by audio hash; stop without retry on any API error."""
    secret = key()
    cache = REPORT/'transcripts'
    for audio in sorted({r['audio_path'] for r in rows if r['classification']=='speech'}):
        path = ROOT/audio
        if not path.exists():
            continue
        data = path.read_bytes()
        target = cache/(sha(data)+'.json')
        if target.exists():
            continue
        boundary = 'axiom-english-audit-'+sha(data)[:16]
        fields = dict(model_id='scribe_v2', language_code='eng', timestamps_granularity='word', tag_audio_events='false')
        body = b''
        for name, value in fields.items():
            body += f'--{boundary}\r\nContent-Disposition: form-data; name="{name}"\r\n\r\n{value}\r\n'.encode()
        body += f'--{boundary}\r\nContent-Disposition: form-data; name="file"; filename="{path.name}"\r\nContent-Type: application/octet-stream\r\n\r\n'.encode()+data+f'\r\n--{boundary}--\r\n'.encode()
        request = urllib.request.Request('https://api.elevenlabs.io/v1/speech-to-text', data=body,
            headers={'xi-api-key':secret, 'Content-Type':'multipart/form-data; boundary='+boundary})
        try:
            with urllib.request.urlopen(request, timeout=180) as response:
                result = json.load(response)
                result['request_id'] = response.headers.get('request-id')
        except urllib.error.HTTPError as error:
            write(REPORT/'transcription_failure.json', dict(audio=audio, http_status=error.code, retry=False))
            raise SystemExit(f'Scribe HTTP {error.code}; stopped without retry.') from None
        result.update(audio_path=audio, audio_sha256=sha(data), model='scribe_v2')
        write(target, result)
        print('Transcribed',audio, flush=True)


def normalize(text):
    """Compare spoken words with equivalent number/unit spelling and compounds, preserving lexical disagreements."""
    text = text.casefold().replace('’', "'").replace('there�s', "there's")
    text = re.sub(r'(?<!\w)(-?[\d,]+)\s*m\b',lambda m:('minus ' if m[1].startswith('-') else '')+m[1].lstrip('-').replace(',','')+' meters',text)
    text = re.sub(r'(?<=\d),(?=\d)','',text)
    text = text.replace('-', ' ')
    for phrase, replacement in {
        'forty five hundred':'4500','twenty five hundred':'2500','twelve hundred':'1200',
        'fourteen thousand':'14000','four thousand':'4000','four hundred':'400','one hundred':'100','ten million':'10000000',
        'sixty five':'65','forty five':'45','forty two':'42','ninety four':'94',
        'zero one':'1','zero two':'2','zero three':'3','thirty':'30','forty':'40','four':'4',
        'seven':'7','bio synthetic':'biosynthetic','bio synthesis':'biosynthesis',
        'sub surface':'subsurface','city wide':'citywide','sight lines':'sightlines','cloud line':'cloudline',
        'cyber grave':'cybergrave','meter':'meters','neighborhoods':'neighbourhoods',
        "nobody's":'nobody is',"there's":'there is',
    }.items():
        text = re.sub(r'\b'+re.escape(phrase)+r'\b',replacement,text)
    text = re.sub(r'\b0+(\d+)\b',r'\1',text)
    text = re.sub(r'(?<=\d)\s*k\b',' k',text)
    text = re.sub(r'\baxiom\s+7\b','axiom7',text)
    text = re.sub(r'\bv\s+9\b','v9',text)
    return re.findall(r'\w+',text)


def recover_provenance():
    """Restore only cryptographically matching historical Cyber-Grave request receipts from the production cache."""
    target = ROOT/'assets/cutscenes/cyber_grave_intro/speech_provenance.json'
    if target.exists():
        return
    cues = read(target.parent/'cues.json')
    receipts = []
    for n, cue in enumerate(cues, 1):
        cache = ROOT/f'.validation/cyber-grave/speech/cue{n}'
        receipt = read(cache.with_suffix('.json'),{})
        source = cache.with_suffix('.mp3')
        body = dict(text=cue['text'],model_id=cue['model'],voice_settings=receipt.get('settings'))
        request_hash = sha(json.dumps(body,sort_keys=True).encode()+cue['voice_id'].encode())
        if not source.exists() or receipt.get('audio_sha256')!=sha(source.read_bytes()) or receipt.get('request_sha256')!=request_hash:
            raise SystemExit('Historical Cyber-Grave receipt cannot be verified; no provenance fabricated.')
        receipts.append(dict(receipt,text=cue['text'],text_sha256=sha(cue['text'].encode()),
            recovery='Exact source and request hashes checked against existing generation cache'))
    write(target,receipts)


def docker_media():
    """Decode via a disposable stdin-fed media container when Docker Desktop cannot bind the Windows drive.

    Send only nonsecret source/manifests/audio and existing evidence. No .env,
    credentials, running service mounts or host Docker configuration are changed.
    """
    stream = io.BytesIO()
    with tarfile.open(fileobj=stream,mode='w') as archive:
        archive.add(ROOT/'tools/cutscene/package_later_campaign.py',arcname='tools/cutscene/package_later_campaign.py')
        for folder in ['scripts','scenes','tools/gameplay','docs/voice-coverage','assets/audio','assets/cutscenes','assets/speech/AXIOM-7/english','assets/translations']:
            for path in sorted((ROOT/folder).rglob('*')):
                if path.is_file() and path.suffix in {'.cs','.gd','.tscn','.py','.json','.mp3','.ogg','.import','.csv'}:
                    archive.add(path,arcname=path.relative_to(ROOT).as_posix())
    command = ('tar -xf - -C /workspace && python3 tools/gameplay/audit_english_voice.py --media && '
               'python3 tools/cutscene/package_later_campaign.py --validate-only && '
               'printf "\\nAXIOM_MEDIA_JSON\\n" && python3 -c \'import json; '
               'print(json.dumps(dict(media=json.load(open("docs/voice-coverage/media.json")), '
               'later=json.load(open("assets/cutscenes/later_campaign_validation.json")))))\'')
    result = subprocess.run(['docker','run','--rm','-i','--entrypoint','sh','axiom-force-cutscene','-c',command],
                            input=stream.getvalue(),capture_output=True,check=True)
    _, separator, payload = result.stdout.partition(b'\nAXIOM_MEDIA_JSON\n')
    if not separator:
        raise SystemExit('Container produced no decoded media evidence.')
    evidence = json.loads(payload)
    write(REPORT/'media.json',evidence['media'])
    write(ROOT/'assets/cutscenes/later_campaign_validation.json',evidence['later'])


def listening_acceptance_records():
    """Load human sign-offs from docs/voice-coverage/listening_acceptance.json.

    Returns a dict of cue ID to record. A missing or empty file means nobody has
    signed anything off, so every flagged cue stays flagged.
    """
    return {r['cue']: r for r in read(REPORT/'listening_acceptance.json', dict(records=[])).get('records', [])}


def apply_listening_acceptance(cue_id, digest, issues, records):
    """Return (remaining issues, status text) after applying one human sign-off.

    Only a recognizer disagreement can be cleared, and only by an 'accepted'
    record whose audio_sha256 equals the current recording. Re-recording or
    editing the audio changes the hash, which voids the old sign-off. Other
    issues (missing audio, bad hashes, clipping) are never cleared here.
    """
    record = records.get(cue_id)
    if not record or not digest or record.get('decision') != 'accepted' or record.get('audio_sha256') != digest:
        return issues, 'pending human listening; ASR is not subjective acceptance'
    remaining = [issue for issue in issues if issue != 'transcript discrepancy requires review']
    return remaining, f"accepted by {record.get('reviewer','unknown')} on {record.get('date','unknown')}"


def audit(rows, media=False):
    """Check provenance, hashes, ASR discrepancies and decoded per-cue silence/clipping/bounds."""
    acceptance = listening_acceptance_records()
    metrics = read(REPORT/'media.json', {})
    if media:
        for row in rows:
            path = ROOT/row['audio_path']
            if row['classification'] != 'speech' or not path.exists():
                continue
            digest = sha(path.read_bytes())
            window = [row.get('start'),row.get('end')]
            previous = metrics.get(row['id'],{})
            if previous.get('audio_sha256') == digest and previous.get('window',window) == window:
                continue
            args = ['ffmpeg','-v','error','-i',str(path)]
            if 'start' in row:
                args += ['-ss',str(row['start']),'-t',str(row['end']-row['start'])]
            result = subprocess.run(args+['-ac','1','-ar','16000','-f','f32le','-'], capture_output=True)
            samples = array.array('f', result.stdout)
            if not samples or result.returncode:
                metrics[row['id']] = dict(error='decode failed', stderr=result.stderr.decode(errors='replace'))
                continue
            peak = max(abs(s) for s in samples)
            rms = math.sqrt(sum(s*s for s in samples)/len(samples))
            audible = [n for n in range(0,len(samples),320) if max(abs(s) for s in samples[n:n+320]) > .006]
            metrics[row['id']] = dict(audio_sha256=digest, window=window, duration=len(samples)/16000,
                peak=peak, rms=rms, clipped_samples=sum(abs(s)>=.999 for s in samples),
                first_signal=audible[0]/16000 if audible else None,
                last_signal=min(len(samples)/16000,(audible[-1]+320)/16000) if audible else None)
        write(REPORT/'media.json',metrics)
    generated = read(OUT/'speech_provenance.json',{})
    replacements = read(REPORT/'repair_provenance.json',{})
    historical = read(REPORT/'historical_provenance.json',{})
    independent = read(REPORT/'independent_review.json',{})
    result = []
    for row in rows:
        item = dict(row)
        issues = []
        if row['classification'] != 'speech':
            item['issues'] = []
            result.append(item)
            continue
        if row.get('text_sha256') != sha(row['text'].encode()):
            issues.append('authored text hash mismatch')
        if row['speaker'] in CAST and row.get('voice_id') != CAST[row['speaker']]:
            issues.append('established speaker alias mismatch')
        path = ROOT/row['audio_path']
        if not path.exists():
            issues.append('missing audio')
        else:
            digest = sha(path.read_bytes())
            item['recording_sha256'] = digest
            if independent.get(row['id'],{}).get('recording_sha256') == digest:
                item['independent_review'] = independent[row['id']]
            transcript = read(REPORT/'transcripts'/f'{digest}.json')
            if transcript:
                text = transcript['text']
                if 'start' in row:
                    text = ''.join(w['text'] for w in transcript.get('words',[]) if w.get('start',-1)>=row['start'] and w.get('start',1e9)<row['end'])
                item['transcript'] = text
                similarity = difflib.SequenceMatcher(None,normalize(row['text']),normalize(text)).ratio()
                item['transcript_similarity'] = similarity
                if similarity < 1:
                    alternate = item.get('independent_review',{}).get('whisper')
                    if alternate and normalize(alternate)==normalize(row['text']):
                        item['text_verification'] = 'Independent recognizer matches authored words after number/unit/typography normalization'
                    else:
                        issues.append('transcript discrepancy requires review')
            else:
                issues.append('not transcribed')
            if not Path(str(path)+'.import').exists():
                issues.append('missing Godot import')
            receipt = generated.get(row['id'],replacements.get(row['id'],historical.get(row['id'],row.get('provenance',{}))))
            if row['id'].startswith(('aegis.','names.')):
                folder, cue_id = row['id'].split('.',1)
                receipt = next((r for r in read(ROOT/f'assets/audio/{folder}/speech_provenance.json',[]) if r.get('id')==cue_id),{})
            item['provenance'] = receipt
            if not receipt or not receipt.get('voice_id'):
                issues.append('missing ElevenLabs provenance')
            elif receipt['voice_id'] != row['voice_id']:
                issues.append('speaker provenance mismatch')
            if receipt.get('model') and receipt['model'] != row.get('model','eleven_multilingual_v2'):
                issues.append('model provenance mismatch')
            if receipt.get('text') and receipt['text'] != row['text']:
                issues.append('receipt text mismatch')
            expected = receipt.get('ogg_sha256') if path.suffix == '.ogg' and 'start' not in row else receipt.get('audio_sha256') if path.suffix == '.mp3' else None
            if expected and expected != digest:
                issues.append('recording hash mismatch')
            metric = metrics.get(row['id'],{})
            item['media'] = metric
            if metric.get('audio_sha256') != digest:
                issues.append('not decoded at current hash')
            elif metric.get('rms',0)<.001:
                issues.append('silent or nearly silent cue')
            elif metric.get('clipped_samples',0)>0:
                issues.append('clipping review')
        if row.get('manifest_text') and normalize(row['manifest_text']) != normalize(row['text']):
            issues.append('runtime and manifest text mismatch')
        timing = row.get('timing',{})
        if timing and (timing.get('start',row['start'])<row['start']-.05 or timing.get('end',row['end'])>row['end']+.05):
            issues.append('mixed-track cue exceeds subtitle window')
        issues, item['listening_acceptance'] = apply_listening_acceptance(row['id'],item.get('recording_sha256'),issues,acceptance)
        item['issues'] = issues
        result.append(item)
    write(REPORT/'coverage.json', dict(cues=result, exclusions=[
        'Ordinary HUD/objective/status text and sector titles remain silent.',
        'assets/narrative/english.json solo_monologues/dialogue are unused by playable CodeLevel missions; no new events added.',
        'IsometricMission and legacy narrative runners are outside the LevelCatalog campaign route.'
    ]))
    lines = ['# English voice coverage', '', 'Automated evidence and listening acceptance are separate. See coverage.json for every cue and discrepancy.', '',
             '| Mission | Speech cues | Missing audio | Transcribed | Decoded |', '| --- | ---: | ---: | ---: | ---: |']
    for mission in range(13):
        subset = [r for r in result if r['mission']==mission and r['classification']=='speech']
        lines.append(f'| {mission} | {len(subset)} | {sum("missing audio" in r["issues"] for r in subset)} | {sum("transcript" in r for r in subset)} | {sum(bool(r.get("media")) for r in subset)} |')
    lines += ['', '| Speaker | Cues |', '| --- | ---: |']
    for speaker in sorted({r['speaker'] for r in result}):
        lines.append(f'| {speaker} | {sum(r["speaker"]==speaker and r["classification"]=="speech" for r in result)} |')
    lines += ['', '## Outstanding evidence', '']
    for issue in sorted({i for r in result for i in r['issues']}):
        lines.append(f'- {issue}: {sum(issue in r["issues"] for r in result)} cues')
    lines += ['', '## Reproduce', '', 'Host: `python tools/gameplay/english_voice_coverage.py` (inventory); add `--generate` only to produce missing recordings.',
              'Host: `python tools/gameplay/audit_english_voice.py --transcribe` (cached paid Scribe audit; no retries).',
              'Media: `docker compose --profile cutscene run --rm -T --entrypoint python3 cutscene tools/gameplay/audit_english_voice.py --media`.',
              'Host: `python tools/gameplay/audit_english_voice.py --check` exits nonzero while required evidence remains incomplete.', '']
    lines += ['For Docker Desktop drive-mount failures: `python tools/gameplay/audit_english_voice.py --docker-media` streams only nonsecret media/source files into the existing media image.',
              'Independent ASR: `uv --system-certs run --python 3.12 --with faster-whisper --with av==15.1.0 tools/gameplay/review_voice_discrepancies.py`.',
              'Natural production playback: run Godot headless with `-s tools/gameplay/review_english_speech.gd`; evidence is saved in `playback.json`.', '']
    (REPORT/'README.md').write_text('\n'.join(lines),encoding='utf-8')
    print('Coverage:',len(result),'cues;',sum(bool(r['issues']) for r in result),'with outstanding evidence',flush=True)
    return result


def main():
    """Explicitly select paid transcription, offline media decoding or strict acceptance checks."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--transcribe',action='store_true')
    parser.add_argument('--media',action='store_true')
    parser.add_argument('--check',action='store_true')
    parser.add_argument('--recover-provenance',action='store_true')
    parser.add_argument('--docker-media',action='store_true')
    args = parser.parse_args()
    if args.recover_provenance:
        recover_provenance()
    if args.docker_media:
        docker_media()
    rows = all_cues()
    if args.transcribe:
        transcribe(rows)
    results = audit(rows,args.media)
    if args.check and any(row['issues'] for row in results):
        raise SystemExit(1)


if __name__=='__main__':
    main()
