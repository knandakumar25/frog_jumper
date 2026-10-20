"""Package newly drawn 2D frames. No legacy images or models are inputs."""
import json
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "assets/characters/rendered"
IDS = ("cyber_shogun", "surya_prime", "oceanus", "mirage_blade", "apex_titan", "phoenicia")
ACTIONS = ("idle", "running", "jumping", "blade_dash", "sword_attack", "karate_attack")

checked = 0
for path in OUT.glob("*_atlas.png"):
    with Image.open(path) as source:
        atlas = source.convert("RGBA")
    for row in range(atlas.height // 256):
        frames = []
        for col in range(8):
            frame = atlas.crop((col*256, row*256, (col+1)*256, (row+1)*256))
            bounds = frame.getchannel("A").getbbox()
            assert bounds and min(bounds[:2]) > 1 and max(bounds[2:]) < 255, (
                path.name, row, col, "drawing crosses frame boundary", bounds)
            frames.append(frame.tobytes())
            checked += 1
        assert len(set(frames)) > 1, (path.name, row, "animation has no movement")
print(f"Validated {checked} transparent, unclipped animated frames.")


def resource(kind, actions):
    lines = [
        f'[gd_resource type="SpriteFrames" load_steps={len(actions)*8+2} format=3]',
        f'[ext_resource type="Texture2D" path="res://assets/characters/rendered/{kind}_atlas.png" id="1"]',
    ]
    for row, action in enumerate(actions):
        for i in range(8):
            lines.extend([
                f'[sub_resource type="AtlasTexture" id="{action}_{i}"]',
                'atlas = ExtResource("1")',
                f'region = Rect2({i*256}, {row*256}, 256, 256)',
            ])
    aliases = {a:a for a in actions}
    if len(actions)>1:
        aliases.update(default="idle", finished="idle", falling="jumping",
                       double_jump="jumping", hit="idle")
    animations = []
    for name, source in aliases.items():
        frames = ",".join(f'{{"duration":1.0,"texture":SubResource("{source}_{i}")}}' for i in range(8))
        speed = {"blade_dash":56.0,"sword_attack":32.0,"karate_attack":26.666667}.get(
            name,8.0 if source=="idle" else 12.0)
        loop = "false" if name in ("blade_dash","sword_attack","karate_attack","hit") else "true"
        animations.append(f'{{"frames":[{frames}],"loop":{loop},"name":&"{name}","speed":{speed}}}')
    lines.extend(["[resource]", "animations = [" + ",\n".join(animations) + "]"])
    (OUT/f"{kind}.tres").write_text("\n\n".join(lines)+"\n",encoding="utf-8")


for kind in IDS:
    resource(kind,ACTIONS)
for kind in ("axiom7","vanguard","heavy"):
    resource(kind,("idle",))
    with Image.open(OUT/f"{kind}_atlas.png") as atlas:
        atlas.crop((0,0,256,256)).save(OUT/f"{kind}_side.png")

manifest = {
    "style": "Original flat 2D cutout drawings: outlined planar polygons, solid colors.",
    "generator": "tools/art/draw_flat_characters.py",
    "inputs": [],
    "models_used": False,
    "artwork_references": [],
    "method": "Python-authored 2D joints and silhouettes rendered by Blender with an orthographic camera and emission colors.",
    "operatives": list(IDS),
    "animations": list(ACTIONS),
    "frame_size": [256,256],
    "frames_per_animation": 8,
}
(OUT/"provenance.json").write_text(json.dumps(manifest,indent=2)+"\n",encoding="utf-8")

# Review images use only this task's newly generated drawings.
preview_dir=ROOT/".validation/flat_characters"
preview_dir.mkdir(exist_ok=True,parents=True)
lineup=Image.new("RGB",(1536,340),"#20293e")
draw=ImageDraw.Draw(lineup)
for i,kind in enumerate(IDS):
    with Image.open(OUT/f"{kind}_atlas.png") as atlas:
        sprite=atlas.crop((0,0,256,256))
        lineup.paste(sprite,(i*256,20),sprite)
    draw.text((i*256+30,292),kind.replace("_"," ").upper(),fill="#fff3d9")
lineup.save(preview_dir/"lineup.png")
frames=[]
for action_index,action in enumerate(ACTIONS):
    for frame in range(8):
        canvas=Image.new("RGB",(1536,340),"#20293e")
        draw=ImageDraw.Draw(canvas)
        for i,kind in enumerate(IDS):
            with Image.open(OUT/f"{kind}_atlas.png") as atlas:
                sprite=atlas.crop((frame*256,action_index*256,(frame+1)*256,(action_index+1)*256))
                canvas.paste(sprite,(i*256,20),sprite)
            draw.text((i*256+30,292),kind.replace("_"," ").upper(),fill="#fff3d9")
        draw.text((20,320),action.upper(),fill="#fff3d9")
        frames.append(canvas)
frames[0].save(preview_dir/"animations.gif",save_all=True,append_images=frames[1:],duration=125,loop=0)
print("Packaged original flat 2D characters and review previews.")
