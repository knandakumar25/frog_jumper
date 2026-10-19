"""Render original planar menu art with Blender; no meshes enter the game.

blender --background --python <absolute script path> -- --output <absolute dir>
Only emission-colored polygons and an orthographic camera are used. Each scene
exports an opaque foundation plus transparent architecture and light layers.
Godot animates these layers independently at runtime, without video decoding.
"""
import argparse
import math
import random
import sys
from pathlib import Path

import bpy

W, H = 1920, 1080
BATCHES = {}
ORDER = 0


def linear(v):
    return v / 12.92 if v < 0.04045 else ((v + 0.055) / 1.055) ** 2.4


def poly(layer, points, color):
    global ORDER
    ORDER += 1
    vertices, faces = BATCHES.setdefault((layer, color), ([], []))
    first = len(vertices)
    vertices.extend(((x - W / 2) / 100, (H / 2 - y) / 100, ORDER * 0.0001) for x, y in points)
    faces.append(tuple(range(first, len(vertices))))


def rect(layer, x, y, w, h, color):
    poly(layer, [(x, y), (x + w, y), (x + w, y + h), (x, y + h)], color)


def line(layer, points, color, width=2):
    for a, b in zip(points, points[1:]):
        dx, dy = b[0] - a[0], b[1] - a[1]
        length = math.hypot(dx, dy)
        if length == 0:
            continue
        nx, ny = -dy / length * width / 2, dx / length * width / 2
        poly(layer, [(a[0] + nx, a[1] + ny), (b[0] + nx, b[1] + ny),
                     (b[0] - nx, b[1] - ny), (a[0] - nx, a[1] - ny)], color)


def arc(layer, x, y, radius, start, end, color, width=2):
    count = max(8, int(abs(end - start) * radius / 9))
    points = [(x + math.cos(start + (end - start) * i / count) * radius,
               y + math.sin(start + (end - start) * i / count) * radius) for i in range(count + 1)]
    line(layer, points, color, width)


def gradient(top, bottom):
    for y in range(0, H, 4):
        t = y / H
        color = tuple(round(a * (1 - t) + b * t, 4) for a, b in zip(top, bottom))
        rect('base', 0, y, W, 4, color)


def city():
    rng = random.Random(417)
    gradient((0.025, 0.044, 0.095), (0.095, 0.21, 0.26))
    # Thin cloud bands, orbital halo, and a restrained star field.
    for i in range(28):
        x, y = rng.uniform(650, W), rng.uniform(70, 580)
        line('base', [(x, y), (min(W, x + rng.uniform(80, 330)), y - 20)], (0.10, 0.17, 0.23), 1)
    for r, c in [(122, (0.20, 0.30, 0.33)), (119, (0.43, 0.52, 0.49))]:
        arc('base', 1450, 260, r, 0, math.tau, c, 2)
    for i in range(90):
        x, y = rng.uniform(620, 1880), rng.uniform(40, 530)
        rect('base', x, y, 1.5, 1.5, (0.26, 0.42, 0.47))
    # Distant skyline. The menu's left-hand text area stays deliberately quiet.
    x = 470
    while x < W:
        width, height = rng.randint(45, 120), rng.randint(95, 340)
        rect('base', x, 790 - height, width, height + 100, (0.075, 0.14, 0.19))
        rect('base', x + 8, 785 - height, width - 16, 2, (0.17, 0.30, 0.34))
        x += width + rng.randint(8, 25)
    # Hero architecture: flat silhouettes with illuminated facade rhythms.
    for x, y, width, height in [(680, 520, 150, 385), (864, 370, 185, 535),
                               (1085, 475, 160, 430), (1282, 322, 224, 583),
                               (1552, 448, 160, 457), (1752, 265, 205, 640)]:
        poly('structure', [(x, y + 20), (x + 20, y), (x + width - 25, y),
                          (x + width, y + 25), (x + width, 1010), (x, 1010)], (0.027, 0.065, 0.105))
        rect('structure', x + width - 35, y + 25, 34, height + 80, (0.017, 0.039, 0.073))
        line('structure', [(x, 950), (x, y + 20), (x + 20, y), (x + width - 25, y)], (0.12, 0.34, 0.39), 2)
        line('structure', [(x + width / 2, y), (x + width / 2, y - 70)], (0.13, 0.26, 0.32), 2)
        rect('lights', x + width / 2 - 2, y - 74, 4, 4, (0.94, 0.39, 0.25))
        for row in range(6, int(height / 15)):
            for col in range(2, int((width - 35) / 15)):
                if rng.random() < 0.58:
                    c = rng.choice([(0.11, 0.34, 0.41), (0.22, 0.56, 0.60), (0.64, 0.43, 0.24)])
                    rect('structure', x + col * 15, y + row * 15, 5, 3, c)
        rect('lights', x + 4, y + 45, 2, height * 0.55, (0.18, 0.77, 0.83))
        for yy in [y + 52, y + 66]:
            rect('lights', x + 18, yy, min(65, width / 2), 2, (0.15, 0.52, 0.59))
        # Geometric signage instead of generated lettering.
        if x > 1200:
            rect('structure', x + width - 26, y + 100, 42, 126, (0.055, 0.10, 0.14))
            for yy in range(0, 4):
                line('lights', [(x + width - 19, y + 114 + yy * 26),
                                (x + width + 4, y + 114 + yy * 26),
                                (x + width + 4, y + 128 + yy * 26)], (0.9, 0.36, 0.26), 3)
    # Elevated transit rail and foreground roof framing.
    poly('structure', [(620, 898), (1920, 802), (1920, 849), (620, 938)], (0.018, 0.041, 0.067))
    line('lights', [(650, 905), (1920, 812)], (0.17, 0.53, 0.59), 2)
    for x in range(760, 1920, 220):
        poly('structure', [(x, 920), (x + 18, 917), (x + 40, 1080), (x + 10, 1080)], (0.018, 0.035, 0.056))
    poly('structure', [(0, 1010), (540, 1000), (720, 956), (1020, 976), (1250, 1030), (1920, 1000), (1920, 1080), (0, 1080)], (0.012, 0.023, 0.041))
    line('structure', [(740, 125), (1100, 177), (1480, 165), (1920, 95)], (0.024, 0.045, 0.07), 4)


def command_bay():
    gradient((0.028, 0.052, 0.085), (0.018, 0.035, 0.058))
    # Flat technical diagram / cutaway walls: no perspective camera or shading.
    for x in range(0, W, 80):
        line('base', [(x, 0), (x, H)], (0.043, 0.079, 0.11), 1)
    for y in range(0, H, 80):
        line('base', [(0, y), (W, y)], (0.043, 0.079, 0.11), 1)
    for x in [0, 1520]:
        poly('structure', [(x, 0), (x + 400, 0), (x + 400, 100),
                          (x + 300, 200), (x + 300, 890), (x + 400, 990),
                          (x + 400, H), (x, H)], (0.038, 0.067, 0.098))
    for flip in [False, True]:
        def pts(values):
            return [(W - x if flip else x, y) for x, y in values]
        line('structure', pts([(0, 150), (150, 150), (230, 230), (230, 850), (140, 940), (0, 940)]), (0.10, 0.22, 0.27), 3)
        line('lights', pts([(0, 174), (137, 174), (203, 240)]), (0.18, 0.69, 0.74), 3)
        line('lights', pts([(0, 914), (125, 914), (189, 850)]), (0.82, 0.46, 0.23), 3)
        for y in range(315, 775, 26):
            line('structure', pts([(28, y), (126, y), (146, y - 10)]), (0.095, 0.15, 0.19), 5)
    # Broken concentric calibration rings sit behind the operative card.
    for radius in [290, 320, 382, 390, 455]:
        for sector in range(6):
            angle = sector * math.tau / 6
            arc('structure', 960, 580, radius, angle + 0.09, angle + 0.92, (0.09, 0.22, 0.27), 2)
    for sector in range(72):
        a = sector * math.tau / 72
        r = 450
        length = 17 if sector % 6 == 0 else 6
        line('structure', [(960 + math.cos(a) * r, 580 + math.sin(a) * r),
                           (960 + math.cos(a) * (r + length), 580 + math.sin(a) * (r + length))], (0.15, 0.34, 0.38), 2)
    for y in [25, 1050]:
        line('structure', [(40, y), (690, y), (720, y + (15 if y < 500 else -15)), (1200, y + (15 if y < 500 else -15)), (1230, y), (1880, y)], (0.13, 0.27, 0.31), 2)
        for x in range(800, 1120, 28):
            rect('lights', x, y - 2, 12, 3, (0.16, 0.65, 0.70))
    for x, y in [(60, 70), (1755, 70), (60, 1000), (1755, 1000)]:
        for i in range(5):
            rect('lights', x + i * 18, y, 9, 4, (0.72, 0.44, 0.23))


def render(name, output, preview):
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    collections = {}
    for layer in ['base', 'structure', 'lights']:
        collection = bpy.data.collections.new(layer)
        bpy.context.scene.collection.children.link(collection)
        collections[layer] = collection
    for (layer, color), (vertices, faces) in BATCHES.items():
        mesh = bpy.data.meshes.new(layer)
        mesh.from_pydata(vertices, [], faces)
        mesh.update()
        obj = bpy.data.objects.new(layer, mesh)
        collections[layer].objects.link(obj)
        mat = bpy.data.materials.new(layer)
        mat.use_nodes = True
        mat.node_tree.nodes.clear()
        emission = mat.node_tree.nodes.new('ShaderNodeEmission')
        emission.inputs['Color'].default_value = (*[linear(v) for v in color], 1)
        surface = mat.node_tree.nodes.new('ShaderNodeOutputMaterial')
        mat.node_tree.links.new(emission.outputs[0], surface.inputs['Surface'])
        mesh.materials.append(mat)
    camera = bpy.data.cameras.new('Orthographic artwork camera')
    camera.type = 'ORTHO'
    camera.ortho_scale = W / 100
    camera_obj = bpy.data.objects.new('Camera', camera)
    bpy.context.scene.collection.objects.link(camera_obj)
    camera_obj.location = (0, 0, 40)
    scene = bpy.context.scene
    scene.camera = camera_obj
    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x, scene.render.resolution_y = W, H
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGBA'
    scene.view_settings.view_transform = 'Standard'
    scene.view_settings.look = 'None'
    scene.world.color = (0, 0, 0)
    for layer, collection in collections.items():
        for other, value in collections.items():
            value.hide_render = other != layer
        scene.render.filepath = str(output / f'{name}_{layer}.png')
        bpy.ops.render.render(write_still=True)
    for value in collections.values():
        value.hide_render = False
    scene.render.filepath = str(preview / f'{name}_artwork.png')
    bpy.ops.render.render(write_still=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--preview', type=Path, required=True)
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
    for path in [args.output, args.preview]:
        if not path.is_absolute():
            parser.error('Output paths must be absolute')
        path.mkdir(parents=True, exist_ok=True)
    global ORDER
    for name, build in [('city', city), ('command_bay', command_bay)]:
        BATCHES.clear()
        ORDER = 0
        build()
        render(name, args.output, args.preview)


if __name__ == '__main__':
    main()
