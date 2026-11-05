"""Create aspect-preserving contact sheets from the native Godot MCP review captures.

Runs inside the media container using FFmpeg; originals remain available for text
and fine-detail inspection. Every authored cue is included in its overlay sheet.
"""
from pathlib import Path
import subprocess


def main():
    """Tile each overlay/aspect independently so no shot can disappear from visual review."""
    root = Path('/workspace/.validation/later-campaign/review')
    for ratio in ['1920x1080', '1920x1200', '2560x1080', '800x600']:
        names = sorted({p.name.split('_')[0] for p in root.glob(f'*_{ratio}_*.png')})
        for name in names:
            inputs = sorted(root.glob(f'{name}_{ratio}_*.png'))
            arguments = ['ffmpeg', '-v', 'error', '-y', '-filter_complex_threads', '1']
            for source in inputs:
                arguments += ['-i', str(source)]
            filters = [f'[{i}:v]scale=480:270:force_original_aspect_ratio=decrease,pad=480:270:(ow-iw)/2:(oh-ih)/2[v{i}]' for i in range(len(inputs))]
            layout = '|'.join(f'{i % 3 * 480}_{i // 3 * 270}' for i in range(len(inputs)))
            filters.append(''.join(f'[v{i}]' for i in range(len(inputs))) + f'xstack=inputs={len(inputs)}:layout={layout}:fill=black[out]')
            arguments += ['-filter_complex', ';'.join(filters), '-map', '[out]', '-frames:v', '1', str(root / f'contact-{name}-{ratio}.png')]
            subprocess.run(arguments, check=True)
            print(name, ratio, len(inputs), flush=True)


if __name__ == '__main__':
    main()
