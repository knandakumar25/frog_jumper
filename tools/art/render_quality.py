"""Shared Blender render-quality settings for the project's Blender art scripts.

Two kinds of scene use it:

* Flat emission art (``apply_flat``): the menu-backdrop ``render()`` in
  ``render_menu_backdrops.py`` and every script that calls it (cutscene plates,
  later-campaign environments), and ``tools/gameplay/render_wildfire.render``, which the
  gameplay layer scripts share. These draw emission-only polygons through an
  orthographic camera, so samples exist only to anti-alias polygon edges. They used to
  render at one sample, which gives hard, stair-stepped alpha edges. Character renders
  go through ``tools/art/draw_flat_characters.py``, which keeps its own 16-sample
  settings and does not use this module.
* Lit, shaded models (``apply_lit``): the city kit (``tools/world/build_kit.py``) and the
  title logos (``tools/art/render_title_logo.py``).

The Cycles properties set here exist under the same names in Blender 4.3 (cutscene
Docker image) and 5.2 (host). Output from the two versions has not been compared pixel
for pixel, so re-render a whole family on one version rather than mixing them.
"""

# Edge anti-aliasing samples per pixel. 16 removes the stair-stepping that one sample
# left on 1-2 px lines in the cutscene and environment layers.
FLAT_SAMPLES = 16
# Width of the reconstruction filter in pixels; 1.5 is Cycles' default and keeps
# 1 px details legible while blending edge pixels.
FILTER_WIDTH = 1.5


def apply_flat(scene, samples=FLAT_SAMPLES):
    """Configure ``scene`` for anti-aliased, noise-free rendering of emission polygons.

    Sets engine and device (Cycles, CPU), sample count, adaptive sampling (off),
    denoising (off), pixel filter, light bounces (0) and seed on ``scene``. Colour
    management and film settings are left to the caller.
    """
    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.samples = samples
    # A fixed sample count gives every pixel the same edge anti-aliasing; adaptive
    # sampling could stop early on edge pixels it judges converged.
    scene.cycles.use_adaptive_sampling = False
    # Emission has no lighting noise, so a denoiser would only smear flat colour.
    scene.cycles.use_denoising = False
    scene.cycles.pixel_filter_type = 'BLACKMAN_HARRIS'
    scene.cycles.filter_width = FILTER_WIDTH
    # Emission needs no light bounces; zero keeps renders fast.
    scene.cycles.max_bounces = 0
    scene.cycles.seed = 0


# Lit scenes have real lighting noise. 512 samples is 8x the city kit's old 64, which
# cuts noise to about a third (noise falls with the square root of the sample count).
# No denoiser is used: the host's Blender 5.2 has OpenImageDenoise but the container's
# Debian Blender 4.3 does not, and using it only where available would make the same
# script produce visibly different files on different machines.
LIT_SAMPLES = 512


def apply_lit(scene):
    """Configure ``scene`` for low-noise Cycles rendering of lit, shaded models.

    Sets engine and device (Cycles, CPU), seed, pixel filter, sample count and denoising
    (off) on ``scene``. Adaptive sampling keeps Cycles' default (on); with the fixed seed
    the result is repeatable on one Blender build.
    """
    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.seed = 0
    scene.cycles.pixel_filter_type = 'BLACKMAN_HARRIS'
    scene.cycles.filter_width = FILTER_WIDTH
    scene.cycles.samples = LIT_SAMPLES
    scene.cycles.use_denoising = False
