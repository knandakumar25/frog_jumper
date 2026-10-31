"""Render original Wildfire gameplay art with Blender/Python inside the opt-in cutscene Docker image.

Run --preview for the representative composition before producing the five 512px RGBA family members.
Orthographic emission meshes reuse the arrival's navy/cyan/rust palette. No external source artwork is used.
"""
import json
import sys
from pathlib import Path

import bpy

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools/art'))
import render_menu_backdrops as art

OUT = ROOT / 'assets/environment/wildfire'
REVIEW = ROOT / '.validation/wildfire-gameplay'
INK, STEEL, EDGE = (.025, .047, .071), (.12, .19, .24), (.24, .35, .40)
CYAN, RUST, WARM = (.38, .85, .88), (.48, .23, .20), (.84, .66, .42)


def rectangle(x, y, w, h, color):
    """Append an emission rectangle using the shared arrival/menu planar geometry vocabulary."""
    art.rect('structure', x, y, w, h, color)


def facade():
    """Damaged inhabited housing with fractured roofline, cool windows and warm fire cavities."""
    art.poly('structure', [(24, 486), (24, 130), (95, 80), (157, 122), (230, 65), (260, 154),
                           (332, 105), (393, 150), (482, 114), (488, 486)], INK)
    for x in [56, 180, 304, 428]:
        rectangle(x, 178, 16, 290, STEEL)
        for y in [195, 278, 365]:
            rectangle(x+25, y, 58, 42, STEEL)
            rectangle(x+31, y+7, 44, 24, (.08, .25, .29))
            art.line('structure', [(x+31, y+34), (x+72, y+34)], CYAN, 3)
    art.line('structure', [(155, 119), (138, 233), (177, 274), (163, 376)], (.012, .025, .04), 8)
    rectangle(233, 381, 70, 105, (.2, .11, .1))
    art.poly('structure', [(237, 479), (245, 416), (266, 442), (283, 401), (300, 479)], RUST)
    art.line('structure', [(35, 481), (478, 481)], EDGE, 7)


def shelter():
    """Fortified shelter entrance; bright cyan lintel and human-scale doorway identify the safe destination."""
    art.poly('structure', [(36, 483), (63, 151), (143, 94), (374, 94), (449, 152), (478, 483)], INK)
    rectangle(74, 154, 365, 319, STEEL)
    rectangle(155, 225, 210, 255, (.015, .035, .052))
    rectangle(155, 215, 210, 14, CYAN)
    rectangle(154, 229, 12, 248, CYAN)
    rectangle(352, 229, 12, 248, CYAN)
    for y in [270, 328, 386]:
        rectangle(91, y, 35, 14, EDGE)
        rectangle(390, y, 31, 14, EDGE)
    art.line('structure', [(224, 164), (288, 164)], CYAN, 9)
    art.line('structure', [(256, 136), (256, 190)], CYAN, 9)
    rectangle(30, 478, 452, 16, EDGE)


def civilians():
    """Three readable, varied civilian silhouettes with small warm clothing accents; no combat gear or hurtboxes."""
    for x, baseline, scale, coat in [(117, 476, 1.0, (.21, .32, .37)), (257, 476, .72, (.45, .31, .24)), (395, 476, .94, (.26, .36, .32))]:
        art.poly('structure', [(x-35*scale, baseline), (x-39*scale, baseline-193*scale),
                               (x-22*scale, baseline-234*scale), (x+27*scale, baseline-234*scale),
                               (x+43*scale, baseline-187*scale), (x+31*scale, baseline)], INK)
        rectangle(x-26*scale, baseline-197*scale, 55*scale, 122*scale, coat)
        rectangle(x-22*scale, baseline-278*scale, 43*scale, 48*scale, (.55, .40, .32))
        rectangle(x-27*scale, baseline-287*scale, 51*scale, 18*scale, INK)
        art.line('structure', [(x, baseline-65*scale), (x, baseline)], INK, 9*scale)
        art.line('structure', [(x-26*scale, baseline-185*scale), (x+25*scale, baseline-185*scale)], WARM, 7*scale)


def gunner():
    """Low armored stationary gunner with a distinct cyan optic, grounded feet and a horizontal weapon silhouette."""
    art.poly('structure', [(48, 476), (83, 424), (125, 275), (181, 234), (298, 234), (354, 290), (405, 443), (464, 476)], INK)
    art.poly('structure', [(112, 422), (150, 281), (300, 281), (346, 420)], STEEL)
    rectangle(174, 151, 139, 137, STEEL)
    rectangle(181, 184, 127, 48, INK)
    rectangle(202, 196, 96, 15, CYAN)
    rectangle(80, 314, 347, 44, EDGE)
    rectangle(64, 320, 169, 30, INK)
    rectangle(60, 328, 27, 14, CYAN)
    rectangle(176, 362, 127, 47, RUST)
    rectangle(58, 462, 162, 22, EDGE)
    rectangle(290, 462, 166, 22, EDGE)


def battery():
    """Broad stationary mortar with segmented armor, heavy support legs and warm shelling accents."""
    art.poly('structure', [(24, 478), (67, 363), (124, 314), (395, 314), (450, 363), (490, 478)], INK)
    art.poly('structure', [(70, 432), (133, 331), (386, 331), (441, 432)], STEEL)
    art.line('structure', [(229, 333), (132, 104)], INK, 78)
    art.line('structure', [(229, 333), (132, 104)], EDGE, 52)
    art.line('structure', [(153, 150), (141, 122)], INK, 65)
    rectangle(185, 306, 163, 73, RUST)
    rectangle(213, 327, 110, 15, WARM)
    for x in [100, 190, 280, 370]:
        rectangle(x, 395, 63, 54, EDGE)
        rectangle(x+7, 403, 49, 8, (.37, .42, .43))
    rectangle(23, 470, 465, 23, EDGE)


def render(path, width=512, height=512):
    """Render the accumulated original emission meshes with transparent film and deterministic CPU settings."""
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for (_, color), (vertices, faces) in art.BATCHES.items():
        mesh = bpy.data.meshes.new('Planar original artwork')
        mesh.from_pydata(vertices, [], faces)
        mesh.update()
        obj = bpy.data.objects.new('Artwork', mesh)
        bpy.context.scene.collection.objects.link(obj)
        material = bpy.data.materials.new('Emission palette')
        material.use_nodes = True
        material.node_tree.nodes.clear()
        emission = material.node_tree.nodes.new('ShaderNodeEmission')
        emission.inputs['Color'].default_value = (*[art.linear(v) for v in color], 1)
        surface = material.node_tree.nodes.new('ShaderNodeOutputMaterial')
        material.node_tree.links.new(emission.outputs[0], surface.inputs['Surface'])
        mesh.materials.append(material)
    camera = bpy.data.cameras.new('Side view orthographic')
    camera.type = 'ORTHO'
    camera.ortho_scale = width / 100
    camera_object = bpy.data.objects.new('Camera', camera)
    bpy.context.scene.collection.objects.link(camera_object)
    camera_object.location = (0, 0, 40)
    scene = bpy.context.scene
    scene.camera = camera_object
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 1
    scene.cycles.use_denoising = False
    scene.render.resolution_x, scene.render.resolution_y = width, height
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGBA'
    scene.view_settings.view_transform = 'Standard'
    scene.view_settings.look = 'None'
    path.parent.mkdir(parents=True, exist_ok=True)
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def composition():
    """Compose representative scenery, safe family, gunner and battery at gameplay-relative scales for visual review."""
    art.W, art.H = 1280, 720
    rectangle(0, 0, 1280, 720, (.035, .065, .095))
    rectangle(0, 604, 1280, 116, (.13, .18, .21))
    original_poly = art.poly
    for draw, x, y, scale in [(facade, 20, 12, 1.15), (shelter, 820, 215, .8),
                               (civilians, 790, 440, .32), (gunner, 550, 450, .30), (battery, 940, 440, .34)]:
        def transformed(layer, points, color):
            """Place one 512px source drawing into the review composition without changing its palette."""
            original_poly(layer, [(x+px*scale, y+py*scale) for px, py in points], color)
        art.poly = transformed
        draw()
    art.poly = original_poly
    render(REVIEW / 'art-target.png', 1280, 720)


def main():
    """Render the review target first, or the approved fixed-size family plus reproducible provenance manifest."""
    art.BATCHES.clear()
    art.ORDER = 0
    if '--preview' in sys.argv:
        composition()
        return
    manifest = {'generator': 'tools/gameplay/render_wildfire.py', 'source': 'Original project-authored Blender emission meshes',
                'license': 'Same terms as project source', 'native_size': [512, 512], 'filter': 'linear',
                'view': 'orthographic side', 'pivot': 'bottom center; explicitly scaled by WildfireVisual', 'assets': []}
    art.W = art.H = 512
    for name, draw in [('facade', facade), ('shelter', shelter), ('civilians', civilians), ('gunner', gunner), ('battery', battery)]:
        art.BATCHES.clear()
        art.ORDER = 0
        draw()
        render(OUT / f'{name}.png')
        manifest['assets'].append({'file': f'{name}.png', 'rgba': True, 'collision': 'none; authored separately in C#'})
    (OUT / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')


if __name__ == '__main__':
    main()
