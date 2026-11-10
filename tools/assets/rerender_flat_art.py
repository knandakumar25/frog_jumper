"""Re-render the flat emission art families that use ``tools/art/render_quality.apply_flat``.

Usage (host Python; Blender is called as a subprocess):

    python tools/assets/rerender_flat_art.py [--blender <path to blender.exe>] [--only name ...]

Each job runs one existing generator script with its normal output arguments, so outputs
land in their usual ``assets/`` folders. Later-campaign art needs two jobs: ``--targets``
renders the environment layers into ``.validation/later-campaign/art/`` and ``--produce``
copies them into ``assets/`` and renders the boss and aftermath plates. Quality comes
from ``tools/art/render_quality.apply_flat`` (cutscene and later-campaign plates through the
menu-backdrop ``render()``, gameplay layers through ``tools/gameplay/render_wildfire.render``).
Character renders (``draw_flat_characters.py``) are not included. The menu backdrops render
into ``.validation/menu-backdrops/output``, not ``assets/``: the committed EEVEE versions were
kept. This script does not check names or sizes; export the old PNGs with
``git show HEAD:<path>`` into a folder and run ``tools/assets/compare_images.py`` on it.

The shipped files were last rendered with host Blender 5.2. The generators can also run
in the cutscene container's Blender 4.3, but re-render a whole family on one version.

Side effects: overwrites the PNGs (and manifests) each generator writes. Exit status is
non-zero if any job fails; other jobs still run.
"""
import argparse
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
# Host Blender install on the development machine; override with --blender elsewhere.
DEFAULT_BLENDER = r'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'

# name -> (script, extra arguments after "--"), run in this order.
JOBS = {
    # The committed menu backdrops are the older EEVEE renders: the Cycles re-render on
    # 2026-11-09 looked identical (edge softness 1.0 both), so they were kept and this job
    # writes to .validation only. Point --output at assets/ only to replace them on purpose.
    'menu_backdrops': ('tools/art/render_menu_backdrops.py',
                       ['--output', str(ROOT / '.validation/menu-backdrops/output'),
                        '--preview', str(ROOT / '.validation/menu-backdrops')]),
    'wildfire': ('tools/cutscene/render_wildfire.py', []),
    'tokyo': ('tools/cutscene/render_tokyo.py', []),
    'deployment_live': ('tools/cutscene/render_live.py', []),
    'act_ii_opening': ('tools/cutscene/render_unregistered.py', []),
    'neon_rust_intro': ('tools/cutscene/render_neon_rust.py', []),
    'neon_rust_aftermath': ('tools/cutscene/render_city_voice.py', []),
    'wildfire_aftermath': ('tools/cutscene/render_breach.py', []),
    'shattered_aegis_intro': ('tools/cutscene/render_inward_wall.py', []),
    'names_without_numbers': ('tools/cutscene/render_names_without_numbers.py', []),
    'env_wildfire': ('tools/gameplay/render_wildfire.py', []),
    'env_neon_rust': ('tools/gameplay/render_neon_rust.py', []),
    'env_aegis': ('tools/gameplay/render_aegis.py', []),
    'env_names': ('tools/gameplay/render_names.py', []),
    # --produce copies the environment layers --targets rendered, so targets must run first.
    'later_campaign_targets': ('tools/cutscene/render_later_campaign.py', ['--targets']),
    'later_campaign': ('tools/cutscene/render_later_campaign.py', ['--produce']),
}


def main():
    """Run the selected jobs in order and report each one's result."""
    parser = argparse.ArgumentParser()
    parser.add_argument('--blender', default=DEFAULT_BLENDER)
    parser.add_argument('--only', nargs='*', choices=sorted(JOBS))
    args = parser.parse_args()
    failed = []
    for name, (script, extra) in JOBS.items():
        if args.only and name not in args.only:
            continue
        # --python-exit-code makes Blender exit 1 when the script raises (including SystemExit).
        command = [args.blender, '--background', '--factory-startup', '--python-exit-code', '1',
                   '--python', str(ROOT / script)]
        if extra:
            command += ['--', *extra]
        result = subprocess.run(command, cwd=ROOT, capture_output=True, text=True, errors='replace')
        # Also look for a traceback, in case a generator catches its own error and carries on.
        broken = result.returncode != 0 or 'Traceback (most recent call last)' in result.stdout + result.stderr
        print(f'{"FAILED" if broken else "ok":6s} {name}', flush=True)
        if broken:
            failed.append(name)
            print((result.stdout + result.stderr)[-1500:], flush=True)
    print(f'{len(failed)} failed: {failed}')
    sys.exit(1 if failed else 0)


if __name__ == '__main__':
    main()
