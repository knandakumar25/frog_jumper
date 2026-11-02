"""Generate and cache the Mission 5 civilian/cast cues with the existing ElevenLabs voice catalog."""
import hashlib
import json
import urllib.error
import urllib.request
from pathlib import Path
from narrate import api_key

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'assets/cutscenes/names_without_numbers'
CACHE = ROOT/'.validation/names-without-numbers/speech'
SETTINGS = dict(stability=.65, similarity_boost=.75, style=.12, speed=.94, use_speaker_boost=True)


def digest_request(cue):
    """Hash only nonsecret request content so a cue cache cannot silently drift from its authored line."""
    body = dict(text=cue['text'], model_id=cue['model'], voice_settings=SETTINGS)
    return hashlib.sha256(json.dumps(body, sort_keys=True).encode()+cue['voice_id'].encode()).hexdigest()


def main():
    """Verify the established cast, reuse exact cache entries and make no automatic retry after API errors."""
    key = api_key()
    if not key:
        raise SystemExit('ELEVENLABS_API_KEY is missing; no requests made.')
    cues = json.loads((OUT/'cues.json').read_text(encoding='utf-8'))
    voices_request = urllib.request.Request('https://api.elevenlabs.io/v1/voices', headers={'xi-api-key':key})
    with urllib.request.urlopen(voices_request, timeout=60) as response:
        voice_ids = {voice['voice_id'] for voice in json.load(response)['voices']}
    if not all(cue['voice_id'] in voice_ids for cue in cues):
        raise SystemExit('One or more authored voice IDs are not available; no speech was generated.')
    CACHE.mkdir(parents=True, exist_ok=True)
    for index, cue in enumerate(cues, 1):
        body = dict(text=cue['text'], model_id=cue['model'], voice_settings=SETTINGS)
        request_hash = digest_request(cue)
        audio_path = CACHE/f'cue{index}.mp3'
        receipt_path = CACHE/f'cue{index}.json'
        if audio_path.exists() and receipt_path.exists():
            receipt = json.loads(receipt_path.read_text(encoding='utf-8'))
            exact_audio = receipt.get('audio_sha256') == hashlib.sha256(audio_path.read_bytes()).hexdigest()
            matching_authorship = receipt.get('voice_id') == cue['voice_id'] and receipt.get('model') == cue['model'] and receipt.get('settings') == SETTINGS
            # Existing MCP-generated files are adopted once when their exact audio and authored voice settings agree.
            if exact_audio and matching_authorship and receipt.get('request_sha256') is None:
                receipt['request_sha256'] = request_hash
                receipt_path.write_text(json.dumps(receipt, indent=2)+'\n', encoding='utf-8')
                print('Recorded exact cache hash for cue', index, flush=True)
                continue
            if receipt.get('request_sha256') == request_hash and exact_audio and matching_authorship:
                print('Cached cue', index, flush=True)
                continue
            raise SystemExit(f'Cue {index} cache does not match the authored request; no overwrite performed.')
        request = urllib.request.Request(
            f"https://api.elevenlabs.io/v1/text-to-speech/{cue['voice_id']}?output_format=mp3_44100_128",
            data=json.dumps(body).encode(), headers={'xi-api-key':key,'Content-Type':'application/json'})
        try:
            with urllib.request.urlopen(request, timeout=120) as response:
                audio = response.read()
        except urllib.error.HTTPError as error:
            raise SystemExit(f'ElevenLabs HTTP {error.code}; no automatic retry.') from None
        audio_path.write_bytes(audio)
        receipt = dict(cue=index, request_sha256=request_hash, audio_sha256=hashlib.sha256(audio).hexdigest(),
                       voice_id=cue['voice_id'], model=cue['model'], settings=SETTINGS,
                       source='ElevenLabs HTTPS TTS; exact unmodified MP3 cache')
        receipt_path.write_text(json.dumps(receipt, indent=2)+'\n', encoding='utf-8')
        print('Generated cue', index, len(audio), 'bytes', flush=True)


if __name__ == '__main__':
    main()
