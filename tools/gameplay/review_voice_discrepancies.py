"""Independently transcribe Scribe discrepancies with local Whisper; never regenerate from ASR disagreement alone.

Run via `uv --system-certs run --python 3.12 --with faster-whisper --with av==15.1.0 tools/gameplay/review_voice_discrepancies.py`.
Model weights are downloaded once into the user's normal Hugging Face cache.
This is automated corroboration, not subjective listening acceptance.
"""
import difflib
import json
from pathlib import Path
from faster_whisper import WhisperModel
from faster_whisper.audio import decode_audio

ROOT = Path(__file__).resolve().parents[2]
REPORT = ROOT/'docs/voice-coverage'


def main():
    """Decode the actual shipped cue windows and cache independent transcripts by recording hash and cue ID."""
    coverage = json.loads((REPORT/'coverage.json').read_text(encoding='utf-8'))['cues']
    target = REPORT/'independent_review.json'
    results = json.loads(target.read_text()) if target.exists() else {}
    model = WhisperModel('small.en',device='cpu',compute_type='int8',cpu_threads=4)
    for cue in coverage:
        if 'transcript discrepancy requires review' not in cue['issues']:
            continue
        if results.get(cue['id'],{}).get('recording_sha256')==cue['recording_sha256']:
            continue
        audio = decode_audio(str(ROOT/cue['audio_path']),sampling_rate=16000)
        if 'start' in cue:
            audio = audio[round(cue['start']*16000):round(cue['end']*16000)]
        # No authored initial prompt: the recognizer must hear independently rather than echo the expected words.
        segments, _ = model.transcribe(audio,language='en',beam_size=5,vad_filter=False,condition_on_previous_text=False)
        text = ' '.join(s.text.strip() for s in segments)
        results[cue['id']] = dict(recording_sha256=cue['recording_sha256'],expected=cue['text'],
            scribe=cue.get('transcript'),whisper=text,model='Systran/faster-whisper-small.en',
            acceptance='automated independent evidence; subjective listening pending')
        target.write_text(json.dumps(results,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
        print('Independently reviewed',cue['id'],flush=True)


if __name__=='__main__':
    main()
