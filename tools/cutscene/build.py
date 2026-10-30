"""Render Blender plates and package a 40-second Theora cinematic with FFmpeg.

Docker: docker compose --profile cutscene run --rm cutscene
Use --package-only to reuse rendered plates. Narration is a separate optional
ElevenLabs-generated narration.ogg in assets/cutscenes/deployment.
"""
import argparse
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def run(arguments):
    """Execute an argument vector, failing immediately on a tool error."""
    subprocess.run(arguments, cwd=ROOT, check=True)


def main():
    """Render plates, animate camera moves, synthesize atmosphere, and encode."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--package-only', action='store_true')
    parser.add_argument('--live', action='store_true', help='Generate and validate editable live assets')
    parser.add_argument('--reveal-only', action='store_true', help='Render the representative hero first')
    args = parser.parse_args()
    if args.live or args.reveal_only:
        command = ['blender', '--background', '--python', 'tools/cutscene/render_live.py']
        if args.reveal_only:
            command += ['--', '--reveal-only']
        run(command)
        if not args.reveal_only:
            run(['python3', 'tools/cutscene/validate_live.py'])
        return
    temporary = ROOT / '.validation/deployment'
    output = ROOT / 'assets/cutscenes/deployment'
    temporary.mkdir(parents=True, exist_ok=True)
    output.mkdir(parents=True, exist_ok=True)
    if not args.package_only:
        run(['blender', '--background', '--python', 'tools/cutscene/render.py'])
    for index in range(4):
        # Each shot is ten seconds, including its fade; camera travel stays within the plate.
        effect = "zoompan=z='1.03+on*0.0003':x='iw/2-iw/zoom/2':y='ih/2-ih/zoom/2':d=240:s=1920x1080:fps=24,fade=t=in:d=0.5,fade=t=out:st=9.5:d=0.5"
        run(['ffmpeg', '-y', '-i', str(temporary / f'{index}_artwork.png'),
             '-vf', effect, '-frames:v', '240', '-c:v', 'libtheora', '-q:v', '7',
             '-an', str(temporary / f'shot{index}.ogv')])
    listing = temporary / 'shots.txt'
    listing.write_text(''.join(f"file 'shot{i}.ogv'\n" for i in range(4)), encoding='utf-8')
    run(['ffmpeg', '-y', '-f', 'concat', '-safe', '0', '-i', str(listing),
         '-f', 'lavfi', '-i', 'anoisesrc=color=pink:amplitude=0.03:duration=40:sample_rate=48000',
         '-f', 'lavfi', '-i', 'sine=frequency=65:duration=40:sample_rate=48000',
         '-filter_complex', '[2:a]volume=0.15[b];[1:a][b]amix=inputs=2,afade=t=in:d=2,afade=t=out:st=37:d=3[a]',
         '-map', '0:v', '-map', '[a]', '-c:v', 'copy', '-c:a', 'libvorbis', '-t', '40',
         str(output / 'deployment.ogv')])


if __name__ == '__main__':
    main()
