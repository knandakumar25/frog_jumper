"""Recover missing historical ElevenLabs provenance without any new speech generation.

Only exact authored-text/voice matches are eligible. Downloaded history audio must
match the original cached MP3 hash before a receipt is published. Unmatched or
expired history remains an explicit coverage gap. Never log unrelated account text.
"""
import json
from pathlib import Path
import urllib.request
import urllib.parse
import urllib.error
from english_voice_coverage import ROOT, REPORT, read, write, key, sha
from audit_english_voice import all_cues, normalize


def mp3_frames(data):
    """Strip ID3 metadata only; history downloads add account metadata without changing MPEG audio frames."""
    if data.startswith(b'ID3') and len(data)>=10:
        size = sum((data[6+n] & 127) << (7*(3-n)) for n in range(4))
        data = data[10+size+(10 if data[5] & 16 else 0):]
    if len(data)>=128 and data[-128:-125] == b'TAG':
        data = data[:-128]
    return data


def main():
    """Page read-only history, retain only required matching records, then authenticate cached source hashes."""
    secret = key()
    rows = all_cues()
    recovered = read(REPORT/'historical_provenance.json',{})
    report = read(REPORT/'coverage.json')
    wanted = {r['id'] for r in report['cues'] if 'missing ElevenLabs provenance' in r['issues']}
    targets = [r for r in rows if r['id'] in wanted and r['id'] not in recovered]
    if not targets:
        print('No historical receipts remain to recover.')
        return
    # Search only the relevant voices/text. Save matching metadata, never a public dump of account history.
    candidates = read(ROOT/'.validation/english-history-matches.json',{})
    cursor = None
    for page in range(20):
        query = dict(page_size=100)
        if cursor:
            query['start_after_history_item_id'] = cursor
        request = urllib.request.Request('https://api.elevenlabs.io/v1/history?'+urllib.parse.urlencode(query),headers={'xi-api-key':secret})
        try:
            with urllib.request.urlopen(request,timeout=60) as response:
                data = json.load(response)
        except urllib.error.HTTPError as error:
            raise SystemExit(f'History HTTP {error.code}; stopped without retries.') from None
        for item in data.get('history',[]):
            for cue in targets:
                if item.get('voice_id')==cue['voice_id'] and normalize(item.get('text',''))==normalize(cue['text']):
                    candidates.setdefault(cue['id'],[])
                    if not any(r['history_item_id']==item['history_item_id'] for r in candidates[cue['id']]):
                        candidates[cue['id']].append({name:item.get(name) for name in
                            ['history_item_id','request_id','voice_id','model_id','text','date_unix','settings','source']})
        write(ROOT/'.validation/english-history-matches.json',candidates)
        print('Read history page',page+1,'matched cues',len(candidates),flush=True)
        if not data.get('has_more') or all(c['id'] in candidates for c in targets):
            break
        cursor = data.get('last_history_item_id')
        if not cursor:
            break
    folders = {'tokyo':'tokyo/speech','wildfire':'wildfire/speech','wildfire_aftermath':'breach/speech',
               'neon_rust_intro':'neon-rust/speech'}
    for cue in targets:
        if cue['id'].startswith('deployment.'):
            source = ROOT/f'.validation/deployment/voice/cue{int(cue["id"].split(".")[-1])-1}.mp3'
        else:
            folder = Path(cue['audio_path']).parent.name
            filename = cue.get('timing',{}).get('cached_source')
            if folder not in folders or not filename:
                continue
            source = ROOT/'.validation'/folders[folder]/filename
        if not source.exists():
            continue
        digest = sha(source.read_bytes())
        for item in candidates.get(cue['id'],[]):
            cache = ROOT/'.validation/english-history-audio'/(item['history_item_id']+'.mp3')
            if not cache.exists():
                request = urllib.request.Request('https://api.elevenlabs.io/v1/history/'+item['history_item_id']+'/audio',headers={'xi-api-key':secret})
                try:
                    with urllib.request.urlopen(request,timeout=60) as response:
                        audio = response.read()
                except urllib.error.HTTPError as error:
                    raise SystemExit(f'History audio HTTP {error.code}; stopped without retries.') from None
                cache.parent.mkdir(parents=True,exist_ok=True)
                cache.write_bytes(audio)
            frame_hash = sha(mp3_frames(source.read_bytes()))
            if sha(mp3_frames(cache.read_bytes())) != frame_hash:
                continue
            recovered[cue['id']] = dict(text=item['text'],text_sha256=sha(item['text'].encode()),
                voice_id=item['voice_id'],model=item['model_id'],settings=item['settings'],
                request_id=item['request_id'],history_item_id=item['history_item_id'],
                source_sha256=digest,source_cache=source.relative_to(ROOT).as_posix(),
                mpeg_frames_sha256=frame_hash,history_download_sha256=sha(cache.read_bytes()),
                source='ElevenLabs authenticated generation history; MPEG frames match production cache exactly after stripping ID3 metadata')
            write(REPORT/'historical_provenance.json',recovered)
            print('Authenticated historical source',cue['id'],flush=True)
            break


if __name__=='__main__':
    main()
