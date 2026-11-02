"""Generate the ten authorized ElevenLabs cues once, using the exact production manifest.

Host standard-library HTTPS keeps the local key out of Docker and logs. Cached
MP3s are reused only with matching request hashes. HTTP failures are not retried.
"""
import hashlib
import json
import urllib.request
import urllib.error
from pathlib import Path
from narrate import api_key

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'assets/cutscenes/act_ii_opening'
CACHE = ROOT/'.validation/unregistered/speech'


def main():
    """Validate voices first; cache complete responses and nonsecret generation provenance."""
    key = api_key()
    if not key:
        raise SystemExit('ELEVENLABS_API_KEY is missing; no requests made.')
    cues = json.loads((OUT/'cues.json').read_text(encoding='utf-8'))
    request = urllib.request.Request('https://api.elevenlabs.io/v1/voices',headers={'xi-api-key':key})
    with urllib.request.urlopen(request,timeout=60) as response:
        available = {v['voice_id'] for v in json.load(response)['voices']}
    assert all(c['voice_id'] in available for c in cues), 'A required cast voice is unavailable'
    CACHE.mkdir(parents=True,exist_ok=True)
    records=[]
    for n,cue in enumerate(cues,1):
        body=dict(text=cue['text'],model_id=cue['model'],voice_settings=dict(stability=.70,similarity_boost=.75,style=.10,speed=1))
        digest=hashlib.sha256(json.dumps(body,sort_keys=True).encode()+cue['voice_id'].encode()).hexdigest()
        path=CACHE/f'cue{n}.mp3'
        receipt=CACHE/f'cue{n}.json'
        if path.exists():
            assert json.loads(receipt.read_text())['request_sha256']==digest, 'Cache differs from authored request'
            records.append(json.loads(receipt.read_text()))
            print('Cached cue',n,flush=True)
            continue
        request=urllib.request.Request(f"https://api.elevenlabs.io/v1/text-to-speech/{cue['voice_id']}?output_format=mp3_44100_128",
            data=json.dumps(body).encode(),headers={'xi-api-key':key,'Content-Type':'application/json'})
        try:
            with urllib.request.urlopen(request,timeout=120) as response:
                audio=response.read()
        except urllib.error.HTTPError as error:
            raise SystemExit(f'ElevenLabs HTTP {error.code}; no automatic retry.') from None
        path.write_bytes(audio)
        record=dict(cue=n,request_sha256=digest,audio_sha256=hashlib.sha256(audio).hexdigest(),voice_id=cue['voice_id'],model=cue['model'],settings=body['voice_settings'],source='ElevenLabs HTTPS TTS; cached unmodified MP3')
        receipt.write_text(json.dumps(record,indent=2)+'\n')
        records.append(record)
        print('Generated cue',n,len(audio),'bytes',flush=True)
    (OUT/'speech_provenance.json').write_text(json.dumps(records,indent=2)+'\n')


if __name__=='__main__':
    main()
