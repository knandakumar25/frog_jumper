"""Render the Level 1 isometric palace map from original Blender geometry.

Run from PowerShell with Blender 5.2 in background mode. The script does not
read previous game art: it builds the floor, dojo, garden, demolition yard,
and Data-Blade altar from flat, ink-outlined polygons in Blender.
"""
import bpy
import math
import os
import sys
from pathlib import Path


PROJECT = Path(__file__).resolve().parents[2]
OUTPUT = PROJECT / "assets" / "images" / "level1" / "ghost_gridlock.png"
PREVIEW = PROJECT / ".validation" / "level1_iso_art.png"
MATERIALS = {}
VERTICES = []
FACES = []
FACE_MATERIALS = []


def linear_channel(value):
    """Convert an sRGB channel to Blender's scene-linear color space."""
    return value / 12.92 if value <= .04045 else ((value + .055) / 1.055) ** 2.4


def material(color):
    """Return a reusable, unlit material for consistent 2D map colors."""
    if color in MATERIALS:
        return MATERIALS[color]
    rgb = [int(color[index:index + 2], 16) / 255 for index in (1, 3, 5)]
    rgb = [linear_channel(channel) for channel in rgb]
    result = bpy.data.materials.new("Ink " + color)
    result.diffuse_color = (*rgb, 1)
    result.use_nodes = True
    nodes = result.node_tree.nodes
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    emission = nodes.new("ShaderNodeEmission")
    emission.inputs["Color"].default_value = (*rgb, 1)
    result.node_tree.links.new(emission.outputs[0], output.inputs["Surface"])
    MATERIALS[color] = result
    return result


def polygon(points, color, z, outline=None):
    """Add a colored flat polygon and optional ink silhouette to the map mesh."""
    if outline:
        center_x = sum(point[0] for point in points) / len(points)
        center_y = sum(point[1] for point in points) / len(points)
        expanded = []
        for x, y in points:
            dx, dy = x - center_x, y - center_y
            distance = max(0.001, math.hypot(dx, dy))
            expanded.append((x + outline * dx / distance, y + outline * dy / distance))
        polygon(expanded, "#10131f", z - .002)
    first = len(VERTICES)
    VERTICES.extend((x, y, z) for x, y in points)
    FACES.append(tuple(range(first, len(VERTICES))))
    FACE_MATERIALS.append(color)


def rect(x, y, width, height, color, z, outline=None):
    """Add an axis-aligned screen-space rectangle."""
    polygon([(x, y), (x + width, y), (x + width, y + height), (x, y + height)],
            color, z, outline)


def diamond(cx, cy, rx, ry, color, z, outline=None):
    """Add a diamond tile or iso roof centered at a screen coordinate."""
    polygon([(cx, cy + ry), (cx + rx, cy), (cx, cy - ry), (cx - rx, cy)],
            color, z, outline)


def disc(cx, cy, radius, color, z, outline=None, sides=12):
    """Add a faceted circular ornament or blossom cluster."""
    polygon([(cx + math.cos(i * math.tau / sides) * radius,
              cy + math.sin(i * math.tau / sides) * radius) for i in range(sides)],
            color, z, outline)


def line(x1, y1, x2, y2, width, color, z):
    """Add a thick outlined stroke as a small quadrilateral."""
    dx, dy = x2 - x1, y2 - y1
    length = max(.001, math.hypot(dx, dy))
    px, py = -dy * width / (2 * length), dx * width / (2 * length)
    polygon([(x1 + px, y1 + py), (x2 + px, y2 + py),
             (x2 - px, y2 - py), (x1 - px, y1 - py)], color, z, width * .24)


def build_floor():
    """Create the connected 4-by-2 mission footprint with room-specific paving."""
    diamond_colors = ["#182b31", "#1b3037", "#203039", "#22343a"]
    for row in range(32):
        cy = .25 + row * .5
        for column in range(96):
            cx = .25 + column * .5
            room_x, room_y = int(cx // 12), int(cy // 8)
            # The six connected rooms form an eastward route, then turn south and west.
            active = (room_y == 1 and room_x < 4) or (room_y == 0 and room_x in (2, 3))
            if not active:
                continue
            color = diamond_colors[(column + row * 2) % len(diamond_colors)]
            diamond(cx, cy, .255, .14, color, 0)
    # Broad walkways visually join the six rooms while leaving clear combat space.
    for x in (11.8, 23.8, 35.8):
        rect(x, 9.0, .45, 5.4, "#294047", .006)
    rect(37.2, 7.75, 5.2, .5, "#294047", .006)
    rect(25.2, 7.75, 5.2, .5, "#294047", .006)
    # A worn amber route guides the player without a minimap or hard timer.
    for i in range(44):
        diamond(1.2 + i * .82, 10.9, .105, .06, "#d88f53", .012)
    for i in range(11):
        diamond(37.1, 10.9 - i * .82, .105, .06, "#d88f53", .012)
    for i in range(15):
        diamond(37.1 - i * .52, 2.3, .105, .06, "#d88f53", .012)


def build_gate():
    """Draw the broken palace entrance and its first objective marker."""
    for x in (1.2, 3.8):
        rect(x, 9.5, .28, 3.4, "#6c3e42", .04, .1)
        rect(x - .18, 9.45, .64, .22, "#ee9b65", .05, .06)
    rect(1.15, 12.8, 2.95, .25, "#ee9b65", .05, .07)
    rect(4.7, 9.7, 1.8, .18, "#bf5f50", .03, .05)
    for i in range(8):
        disc(2.2 + i * .31, 13.15 + math.sin(i) * .08, .09,
             "#d6b287", .045, .03, 6)


def build_lantern_court():
    """Place damaged stone lanterns, cracked paving, and old garden moss."""
    for x, y in ((14.4, 10.1), (17.9, 13.4), (21.5, 10.4), (19.8, 15.1)):
        diamond(x, y, .72, .35, "#10131f", .025, .08)
        diamond(x, y + .02, .55, .27, "#68716c", .035, .05)
        rect(x - .17, y + .18, .34, .56, "#9b856c", .045, .06)
        rect(x - .36, y + .64, .72, .16, "#c7ab83", .05, .05)
        diamond(x, y + .86, .5, .24, "#b89b77", .06, .05)
        disc(x, y + .89, .12, "#f39c4a", .07, .04)
    for x, y in ((13.1, 14.8), (16.1, 9.2), (22.5, 14.8)):
        disc(x, y, .36, "#315148", .016)
        disc(x + .2, y + .16, .19, "#54745a", .02)


def build_blossom_walk():
    """Draw cherry trees, petals, and broken sterile monolith foundations."""
    for x, y in ((26.7, 10.0), (29.4, 14.2), (33.7, 10.3)):
        diamond(x, y, .57, .28, "#10131f", .025, .05)
        rect(x - .13, y + .08, .26, 1.35, "#6e4740", .04, .04)
        for dx, dy, radius in ((-.5, 1.4, .62), (.1, 1.9, .72), (.58, 1.35, .58), (.03, 1.25, .78)):
            disc(x + dx, y + dy, radius, "#ba637d", .045, .05)
        for dx, dy in ((-.43, .9), (.44, 1.0), (.12, 2.38), (-.1, 2.0)):
            disc(x + dx, y + dy, .11, "#f3a8be", .06, .025, 8)
    for x, y in ((25.6, 14.5), (31.3, 9.4), (34.5, 14.4)):
        diamond(x, y, .55, .26, "#10131f", .025, .04)
        rect(x - .35, y + .1, .7, .85, "#262a34", .035, .05)
        diamond(x, y + .95, .55, .25, "#3c4651", .05, .04)


def build_archive():
    """Add the sealed archive hall and several readable memory alcoves."""
    rect(38.0, 9.5, 1.25, 3.2, "#171d2b", .025, .1)
    rect(38.3, 9.85, .65, 2.5, "#46535e", .04, .06)
    rect(38.48, 10.0, .28, 2.15, "#85d3c4", .05, .03)
    for x in (40.6, 44.8, 47.1):
        rect(x, 9.4, .95, 2.6, "#303441", .03, .06)
        for index in range(4):
            rect(x + .12, 9.7 + index * .52, .7, .08,
                 "#ba8f63" if index % 2 else "#668d91", .05)
    for x, y in ((40.0, 14.1), (46.8, 14.1)):
        diamond(x, y, .42, .21, "#10131f", .025, .04)
        disc(x, y + .35, .21, "#58c8c4", .045, .045)
        disc(x, y + .37, .085, "#c6fff0", .06, .025)


def build_demolition_yard():
    """Stage the non-combat Terra-Walker threat, beacon, and sweep-beam lanes."""
    for x, y in ((37.1, 1.1), (47.1, 1.1)):
        rect(x, y, .25, 5.4, "#4b4f5b", .04, .06)
        rect(x - .08, y + .12, .42, .4, "#92949c", .05, .05)
        diamond(x + .05, y + 5.4, .46, .22, "#74717b", .06, .05)
    # Walker silhouette remains beyond the arena, visibly crushing the garden wall.
    rect(42.0, 5.2, 3.8, .56, "#292938", .03, .11)
    diamond(43.7, 5.85, 2.0, .84, "#292938", .045, .15)
    rect(42.4, 4.1, .5, 1.4, "#292938", .05, .08)
    rect(45.6, 4.1, .5, 1.4, "#292938", .05, .08)
    for x in (42.65, 45.85):
        line(x, 4.15, x - .55, 2.2, .28, "#292938", .05)
        line(x - .55, 2.2, x - .2, 1.4, .22, "#292938", .05)
    disc(43.95, 5.88, .28, "#9a66d4", .07, .05)
    # Beacon rings, drawn as layered diamonds, read clearly beneath the bossless set piece.
    diamond(42.0, 3.4, 1.35, .68, "#10131f", .04, .13)
    diamond(42.0, 3.45, 1.04, .53, "#682e61", .05, .09)
    diamond(42.0, 3.52, .58, .3, "#cf557e", .06, .07)
    rect(41.84, 3.0, .32, .95, "#f28375", .08, .05)
    # Violet sweep lanes visually communicate the moving terraforming hazard.
    for y in (2.35, 4.45):
        line(36.5, y, 47.5, y, .055, "#b76ee7", .028)
        for x in (38.0, 44.0, 46.5):
            diamond(x, y, .18, .095, "#e5a6ff", .04)


def build_dojo_core():
    """Frame the Data-Blade as the story's emotional and visual finale."""
    diamond(30.2, 2.5, 3.25, 1.6, "#111722", .025, .12)
    diamond(30.2, 2.6, 2.75, 1.34, "#3c3b48", .04, .1)
    for x in (27.8, 32.6):
        rect(x, 2.7, .28, 3.15, "#6e514e", .05, .07)
        rect(x - .28, 5.8, .84, .22, "#e4ad76", .06, .05)
    rect(28.0, 5.98, 2.7, .24, "#e4ad76", .06, .05)
    diamond(30.2, 3.65, .95, .46, "#10131f", .07, .08)
    disc(30.2, 4.05, .2, "#74f2e5", .09, .04)
    line(30.2, 3.05, 30.2, 4.1, .08, "#e7ffff", .1)
    line(30.2, 3.05, 30.44, 3.32, .04, "#83f6ec", .11)


def finish_mesh():
    """Combine the map into one lightweight mesh with one material slot per swatch."""
    mesh = bpy.data.meshes.new("Ghost Gridlock isometric map")
    mesh.from_pydata(VERTICES, [], FACES)
    mesh.update()
    obj = bpy.data.objects.new("Ghost Gridlock rendered map", mesh)
    bpy.context.collection.objects.link(obj)
    colors = list(dict.fromkeys(FACE_MATERIALS))
    for color in colors:
        mesh.materials.append(material(color))
    slots = {color: index for index, color in enumerate(colors)}
    for polygon_face, color in zip(mesh.polygons, FACE_MATERIALS):
        polygon_face.material_index = slots[color]


def render(path):
    """Render a deterministic orthographic map with Blender's real renderer."""
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 4608
    scene.render.resolution_y = 1536
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.world.use_nodes = True
    world_background = scene.world.node_tree.nodes.get("Background")
    world_background.inputs["Color"].default_value = (.002, .006, .016, 1)
    world_background.inputs["Strength"].default_value = .45
    camera_data = bpy.data.cameras.new("Map orthographic camera")
    camera = bpy.data.objects.new("Map orthographic camera", camera_data)
    scene.collection.objects.link(camera)
    camera.location = (24, 8, 30)
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = 48
    camera.rotation_euler = (0, 0, 0)
    scene.camera = camera
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "Medium High Contrast"
    scene.view_settings.exposure = 0
    scene.view_settings.gamma = 1
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def main():
    """Build the six-room map and render its game asset and review preview."""
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    build_floor()
    build_gate()
    build_lantern_court()
    build_blossom_walk()
    build_archive()
    build_demolition_yard()
    build_dojo_core()
    finish_mesh()
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    render(OUTPUT)
    # The preview is an identical game render kept outside the resource tree.
    import shutil
    shutil.copyfile(OUTPUT, PREVIEW)
    print("LEVEL1_ISOMETRIC_RENDER=" + str(OUTPUT))


if __name__ == "__main__":
    main()
