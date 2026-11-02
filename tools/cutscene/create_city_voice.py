"""Author the requested immutable English cue manifest and matching overlay from the established lifecycle.

Run once during implementation; production reproduction uses the render/narrate/package tools.
No existing arrival dialogue or export configuration is changed.
"""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'assets/cutscenes/neon_rust_aftermath'
CUES = [
    ('AXIOM-7', 'Daniel', 'onwK4e9ZLuTAKqWW03F9', 0, 9, 'Three shipments secured. Both uplinks are down. Regional patrols have lost their shared targeting feed.'),
    ('AURA', 'Matilda', 'XrExE9yKIg1WjnnlVkGX', 9, 18, 'Listen. No coordinates. No orders. Just a little room between the noise. I can work with that.'),
    ('MIRAGE', 'Eric', 'cjVigY5qzO86Huf0OWal', 18, 27, 'Enjoy it while it lasts. Their eyes are down, not their weapons. I’ve marked a way out.'),
    ('OCEANUS', 'Sarah', 'EXAVITQu4vr4xnSDxMaL', 27, 36, 'Send the recovered power back toward the neighborhoods. They’ve been keeping people cold to keep these fences burning.'),
    ('TITAN', 'George', 'JBFqnCBsd6RMkjVDRZzb', 36, 45, 'Put those crates on the service line. We hauled enough weight for their machines. Let it carry something useful.'),
    ('KAITO', 'Adam', 'pNInz6obpgDQGcFmaJgB', 45, 54, 'They’ll come looking for this depot. Make sure the supplies leave before we do.'),
    ('AURA', 'Matilda', 'XrExE9yKIg1WjnnlVkGX', 54, 64, 'A short signal. No names, no coordinates. Just this: the checkpoint is open, and you are not alone.'),
    ('MIRAGE', 'Eric', 'cjVigY5qzO86Huf0OWal', 64, 74, 'The route reaches the Border Fortress. I can get us to its approaches. Getting through that wall is another matter.'),
    ('AXIOM-7', 'Daniel', 'onwK4e9ZLuTAKqWW03F9', 74, 82, 'The fortress runs on its own power. Its guns will still be waiting.'),
    ('KAITO', 'Adam', 'pNInz6obpgDQGcFmaJgB', 82, 88, 'Then we go together. This city has waited long enough.'),
]


def main():
    """Write cue provenance, English fallbacks and the runtime timeline without altering shared cinematics."""
    OUT.mkdir(parents=True, exist_ok=True)
    cues = [dict(speaker=s, voice=v, voice_id=i, start=a, end=b, text=t, model='eleven_multilingual_v2') for s,v,i,a,b,t in CUES]
    (OUT/'cues.json').write_text(json.dumps(cues, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    source = (ROOT/'scripts/ui/NeonRustIntro.cs').read_text(encoding='utf-8')
    source = source.replace('NeonRustIntro', 'NeonRustAftermath').replace('NeonRustStage', 'NeonRustAftermathStage')
    source = source.replace('seventy-five-second Level 3 covert arrival', 'ninety-second Level 3 extraction aftermath')
    source = source.replace('every fresh launch restores the complete mission snapshot', 'results retain the immutable extraction snapshot')
    source = source.replace('neon_rust_intro', 'neon_rust_aftermath').replace('Duration = 75', 'Duration = 90')
    source = source.replace('{ 0, 9, 18, 27, 36, 45, 55, 64, 72 }', '{ 0, 9, 18, 27, 36, 45, 54, 64, 74, 82, 88 }')
    begin = source.index('    private static readonly string[] Lines = {')
    end = source.index('    private NeonRustAftermathStage?', begin)
    lines = ['        '+json.dumps(c['speaker']+': '+c['text'], ensure_ascii=False)+',' for c in cues]
    source = source[:begin]+'    private static readonly string[] Lines = {\n'+'\n'.join(lines)+'\n        "" // Silent final title.\n    };\n'+source[end:]
    source = source.replace('SILENT_FREIGHT', 'CITY_VOICE').replace('cue < 8', 'cue < 10')
    source = source.replace('DeliverGameplay', 'DeliverResults').replace('gameplay callback', 'results callback').replace('resume gameplay twice', 'show results twice')
    (ROOT/'scripts/ui/NeonRustAftermath.cs').write_text(source, encoding='utf-8')
    (OUT/'localization.json').write_text(json.dumps({f'CITY_VOICE_CUE_{n+1}': c['speaker']+': '+c['text'] for n,c in enumerate(cues)}, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')


if __name__ == '__main__':
    main()
