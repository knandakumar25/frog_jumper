"""Generate cached ElevenLabs cues and assemble the deployment voice track.

Run with host Python from the project root. Reads the gitignored .env without
printing secrets; uses Docker's media tools. Existing cues are never regenerated
automatically, preventing duplicate charges on reruns. API failures stop the run.
"""
import json
import os
import subprocess
import urllib.error
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
VOICE = 'onwK4e9ZLuTAKqWW03F9'
CUES = [
    (1, 9.5, 'Beyond the neon, the city waits. Every rooftop is watched. Every signal is traced.'),
    (11, 19.5, 'But one operative moves beyond their reach. Cyber Shogun. Your deployment begins tonight.'),
    (21, 31, 'Follow the rooftops. Keep your blade ready. Somewhere beneath these lights, the resistance is still alive.'),
    (33, 39, 'This is AXIOM-7. Secure channel established. Stand by for your mission briefing.'),
]


def api_key():
    """Read the local key with .env precedence; never include it in diagnostics."""
    for line in (ROOT / '.env').read_text(encoding='utf-8-sig').splitlines():
        name, separator, value = line.partition('=')
        if separator and name.strip() == 'ELEVENLABS_API_KEY':
            return value.strip().strip('"\'')
    return os.environ.get('ELEVENLABS_API_KEY', '')


def media(tool, *arguments):
    """Run media tools in the opt-in container without forwarding any secrets."""
    docker = os.environ.get('DEPLOYMENT_DOCKER', 'docker')
    return subprocess.check_output([
        docker, 'compose', '--profile', 'cutscene', 'run', '--rm', '-T',
        '--entrypoint', tool, 'cutscene', *arguments,
    ], cwd=ROOT, text=True)


def main():
    """Generate four cues once, validate durations, and mix a forty-second track."""
    key = api_key()
    if not key:
        raise SystemExit('ELEVENLABS_API_KEY is missing from .env/environment.')
    cache = ROOT / '.validation/deployment/voice'
    cache.mkdir(parents=True, exist_ok=True)
    # Verify the selected broadcaster voice is accessible before spending credits.
    request = urllib.request.Request('https://api.elevenlabs.io/v1/voices', headers={'xi-api-key': key})
    with urllib.request.urlopen(request, timeout=60) as response:
        voices = json.load(response)['voices']
    if VOICE not in {voice['voice_id'] for voice in voices}:
        raise SystemExit('Selected voice is unavailable; choose an accessible voice first.')
    for index, (_, _, text) in enumerate(CUES):
        path = cache / f'cue{index}.mp3'
        if path.exists() and path.stat().st_size:
            print(f'Cue {index + 1}: using cached generation', flush=True)
            continue
        body = json.dumps({'text': text, 'model_id': 'eleven_multilingual_v2',
                           'voice_settings': {'stability': 0.75, 'similarity_boost': 0.75,
                                              'style': 0, 'speed': 1.1}}).encode()
        request = urllib.request.Request(
            f'https://api.elevenlabs.io/v1/text-to-speech/{VOICE}?output_format=mp3_44100_128',
            data=body, headers={'xi-api-key': key, 'Content-Type': 'application/json'})
        try:
            with urllib.request.urlopen(request, timeout=120) as response:
                audio = response.read()
        except urllib.error.HTTPError as error:
            # Avoid printing response bodies or headers that might contain account data.
            raise SystemExit(f'ElevenLabs generation failed: HTTP {error.code}; no automatic retry.') from None
        path.write_bytes(audio)
        print(f'Cue {index + 1}: generated {len(audio)} bytes', flush=True)
    inputs, filters = [], []
    for index, (start, end, _) in enumerate(CUES):
        path = f'.validation/deployment/voice/cue{index}.mp3'
        duration = float(media('ffprobe', '-v', 'error', '-show_entries', 'format=duration',
                               '-of', 'default=nw=1:nk=1', path).strip())
        # Small tempo adjustments preserve every word without a second paid request.
        speed = max(1.0, duration / (end - start - 0.15))
        if speed > 1.2:
            raise SystemExit(f'Cue {index + 1} needs editorial shortening ({duration:.2f}s).')
        inputs.extend(['-i', path])
        filters.append(f'[{index}:a]atempo={speed:.6f},highpass=f=180,lowpass=f=4200,adelay={start * 1000}:all=1[a{index}]')
        print(f'Cue {index + 1}: {duration:.2f}s; tempo {speed:.3f}', flush=True)
    filters.append(''.join(f'[a{i}]' for i in range(4)) + 'amix=inputs=4:normalize=0,alimiter=limit=0.9,apad,atrim=duration=40[out]')
    output = ROOT / 'assets/cutscenes/deployment'
    output.mkdir(parents=True, exist_ok=True)
    media('ffmpeg', '-y', *inputs, '-filter_complex', ';'.join(filters), '-map', '[out]',
          '-c:a', 'libvorbis', '-q:a', '5', 'assets/cutscenes/deployment/narration.ogg')
    print('Narration assembled: assets/cutscenes/deployment/narration.ogg')


if __name__ == '__main__':
    main()
