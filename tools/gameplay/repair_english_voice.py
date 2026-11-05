"""Repair confirmed content defects only, caching replacements before touching shipped audio.

The four briefing files contain retired dialogue; Foundry aftermath cue two repeats
Oceanus instead of Titan. Preserve all other takes, natural speed and subtitle timing.
HTTP failures stop immediately; reruns reuse exact hashed responses.
"""
import argparse
import json
import shutil
import subprocess
import urllib.request
import urllib.error
from audit_english_voice import all_cues
from english_voice_coverage import ROOT, REPORT, CAST, SETTINGS, read, write, sha, key

CACHE = ROOT/'.validation/english-voice-repairs'


def repairs():
    """Return only authored lines with confirmed wrong words/speaker, never raw ASR near-matches."""
    ids = {f'briefing.{n}' for n in range(1,5)} | {'cinematic.foundry_forge_aftermath.02', 'm01.event.3f776f8eee75',
        'm06.intro','m08.intro','m06.fragment1','m06.fragment2','m08.fragment1','m08.fragment3','m10.fragment1','m11.fragment2'}
    return [r for r in all_cues() if r['id'] in ids]


def generate():
    """Cache full replacement responses and exact input, settings, voice and API request receipts."""
    secret = key()
    for cue in repairs():
        target = CACHE/(cue['id']+'.mp3')
        receipt_path = CACHE/(cue['id']+'.json')
        # Numeric/unit expansion and punctuation pauses preserve the authored words,
        # while avoiding the engine voicing slash separators or swallowing a unit.
        spoken = cue['text']
        if cue['id'] in {'m06.intro','m08.intro','m06.fragment1','m06.fragment2','m08.fragment1','m08.fragment3','m10.fragment1','m11.fragment2'}:
            spoken = spoken.replace(' // ', ': ').replace('-400m','minus four hundred meters').replace('1,200m','twelve hundred meters')
        body = dict(text=spoken,model_id='eleven_multilingual_v2',voice_settings=SETTINGS)
        digest = sha(json.dumps(body,sort_keys=True).encode()+cue['voice_id'].encode())
        receipt = read(receipt_path,{})
        if target.exists():
            if receipt.get('request_sha256') != digest or receipt.get('audio_sha256') != sha(target.read_bytes()):
                raise SystemExit('Replacement cache mismatch: '+cue['id'])
            continue
        request = urllib.request.Request(f'https://api.elevenlabs.io/v1/text-to-speech/{cue["voice_id"]}?output_format=mp3_44100_128',
            data=json.dumps(body).encode(),headers={'xi-api-key':secret,'Content-Type':'application/json'})
        try:
            with urllib.request.urlopen(request,timeout=120) as response:
                data = response.read()
                request_id = response.headers.get('request-id')
        except urllib.error.HTTPError as error:
            write(REPORT/'repair_failure.json',dict(cue=cue['id'],http_status=error.code,retry=False))
            raise SystemExit(f'ElevenLabs HTTP {error.code}; stopped without retry.') from None
        target.parent.mkdir(parents=True,exist_ok=True)
        target.write_bytes(data)
        write(receipt_path,dict(id=cue['id'],text=cue['text'],input_text=spoken,text_sha256=sha(cue['text'].encode()),
            voice_id=cue['voice_id'],model=body['model_id'],settings=SETTINGS,request_sha256=digest,
            audio_sha256=sha(data),request_id=request_id,source='ElevenLabs HTTPS TTS; complete replacement, speed 1'))
        print('Generated replacement',cue['id'],flush=True)


def install():
    """Install verified cached MP3s and replace just the defective cinematic interval, without retiming any other take."""
    provenance = read(REPORT/'repair_provenance.json',{})
    for cue in repairs():
        source = CACHE/(cue['id']+'.mp3')
        receipt = read(CACHE/(cue['id']+'.json'),{})
        if not source.exists() or sha(source.read_bytes()) != receipt.get('audio_sha256'):
            raise SystemExit('Missing or invalid cached replacement: '+cue['id'])
        target = ROOT/cue['audio_path']
        previous = provenance.get(cue['id'],{})
        if previous.get('audio_sha256') == receipt['audio_sha256'] and previous.get('shipped_audio_sha256') == sha(target.read_bytes()):
            continue
        backup = CACHE/('original-'+target.parent.name+'-'+target.name)
        if not backup.exists():
            shutil.copyfile(target,backup)
        if cue['id'].startswith('briefing.') or cue['id'].startswith('m'):
            shutil.copyfile(source,target)
            if cue['id'].startswith('m'):
                receipts = read(ROOT/'assets/audio/english/speech_provenance.json')
                receipts[cue['id']] = receipt
                write(ROOT/'assets/audio/english/speech_provenance.json',receipts)
        else:
            start = cue['start']+.15
            duration = float(subprocess.check_output(['ffprobe','-v','error','-show_entries','format=duration','-of','default=nw=1:nk=1',str(source)],text=True))
            if start+duration > cue['end']-.15:
                raise SystemExit('Replacement needs a longer subtitle/shot window; no truncation performed.')
            # Use the untouched backup on every rerun. Silence only the wrong slot;
            # every other performance keeps its original samples and timeline position.
            temporary = CACHE/'foundry-repaired.ogg'
            filters = (f'[0:a]volume=0:enable=between(t\\,{cue["start"]}\\,{cue["end"]})[original];'
                f'[1:a]highpass=f=80,lowpass=f=7500,loudnorm=I=-18:TP=-2:LRA=7,adelay={round(start*1000)}:all=1[replacement];'
                '[original][replacement]amix=inputs=2:normalize=0:duration=first[out]')
            subprocess.run(['ffmpeg','-v','error','-y','-i',str(backup),'-i',str(source),'-filter_complex',filters,
                '-map','[out]','-c:a','libvorbis','-q:a','5',str(temporary)],check=True)
            shutil.copyfile(temporary,target)
            receipts = read(target.parent/'speech_provenance.json')
            receipts[1].update(receipt,cue=2,speaker='TITAN',source_sha256=receipt['audio_sha256'])
            write(target.parent/'speech_provenance.json',receipts)
            timing = read(target.parent/'dialogue_timing.json')
            timing['cues'][1].update(start=start,end=start+duration,complete_take=True,tempo=1.0)
            write(target.parent/'dialogue_timing.json',timing)
        provenance[cue['id']] = dict(receipt,shipped_audio_sha256=sha(target.read_bytes()),
            prior_audio_sha256=sha(backup.read_bytes()),reason='Confirmed authored words differ from full-recording Scribe transcript')
        print('Installed replacement',cue['id'],flush=True)
    write(REPORT/'repair_provenance.json',provenance)


def main():
    """Keep paid requests on the host and offline FFmpeg installation in the media container."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--generate',action='store_true')
    parser.add_argument('--install',action='store_true')
    args = parser.parse_args()
    if args.generate:
        generate()
    if args.install:
        install()


if __name__ == '__main__':
    main()
