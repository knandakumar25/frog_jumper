"""
Generates the complete 2048x2048 PBR diffuse texture for Cyber Shogun:
- Authentic Japanese Male face: warm calibrated skin, dark obsidian eyes, seasoned samurai brows & stubble.
- Tactical Obsidian-Carbon Cyber-Suit: nanoweave texture with crimson accents and glowing cyan nanite lines.
"""

import math
import os
from PIL import Image

HUMANIZER_DIR = r"C:\Users\karthiknk21\.gemini\antigravity-cli\brain\51c70920-0a37-4082-8177-2c677a76cad3\scratch\humanizer"
SCRATCH_DIR = r"C:\Users\karthiknk21\.gemini\antigravity-cli\brain\51c70920-0a37-4082-8177-2c677a76cad3\scratch\shogun_redesign"
OUT_TEX = os.path.join(SCRATCH_DIR, "shogun_face_diffuse.png")

def main():
    y_img = Image.open(os.path.join(HUMANIZER_DIR, "young_asian_male.png")).convert("RGBA")
    m_img = Image.open(os.path.join(HUMANIZER_DIR, "middleage_asian_male.png")).convert("RGBA")

    tw, th = 2048, 2048
    if y_img.size != (tw, th):
        y_img = y_img.resize((tw, th), Image.Resampling.LANCZOS)
    if m_img.size != (tw, th):
        m_img = m_img.resize((tw, th), Image.Resampling.LANCZOS)

    blended = Image.blend(y_img, m_img, 0.35)
    pixels = blended.load()

    # Load 3D eyeball texture if available to paste directly into eye UV quadrant
    eye_img_path = os.path.join(HUMANIZER_DIR, "eyeball_albedo.png")
    eye_pixels = None
    if os.path.exists(eye_img_path):
        e_img = Image.open(eye_img_path).convert("RGBA").resize((256, 256))
        eye_pixels = e_img.load()

    for y in range(th):
        v = 1.0 - (y / float(th))
        for x in range(tw):
            u = x / float(tw)
            r, g, b, a = pixels[x, y]

            # 1. HEAD & FACE REGION (u >= 0.60)
            if u >= 0.60:
                cr = int(r * 0.72)
                cg = int(g * 0.69)
                cb = int(b * 0.65)

                dv_center = abs(v - 0.4832)
                is_ear = (0.800 <= u <= 0.885 and 0.118 <= dv_center <= 0.205)

                # Scalp & Hairline
                if not is_ear:
                    if u < 0.802 and dv_center < 0.34:
                        cr, cg, cb = 22, 22, 26
                    elif 0.802 <= u <= 0.858 and 0.096 <= dv_center <= 0.28:
                        cr, cg, cb = 22, 22, 26

                # Japanese Samurai Eyebrows
                if 0.014 <= dv_center <= 0.068:
                    brow_center_u = 0.828 - (dv_center - 0.014) * 0.12
                    brow_half_w = 0.0075
                    if abs(u - brow_center_u) < brow_half_w:
                        t = 1.0 - abs(u - brow_center_u) / brow_half_w
                        cr = int(cr * (1.0 - t * 0.85) + 18 * t * 0.85)
                        cg = int(cg * (1.0 - t * 0.85) + 18 * t * 0.85)
                        cb = int(cb * (1.0 - t * 0.85) + 22 * t * 0.85)

                # Samurai Mustache & Chin Tuft Stubble
                if u > 0.885 and dv_center < 0.060:
                    m_curve_u = 0.896 + (dv_center / 0.042) ** 1.6 * 0.015
                    if dv_center < 0.040 and abs(u - m_curve_u) < 0.0055:
                        w_str = (1.0 - abs(u - m_curve_u) / 0.0055) * 0.78
                        cr = int(cr * (1.0 - w_str) + 25 * w_str)
                        cg = int(cg * (1.0 - w_str) + 25 * w_str)
                        cb = int(cb * (1.0 - w_str) + 30 * w_str)
                    elif 0.938 <= u <= 0.962 and dv_center < 0.022:
                        c_str = (1.0 - dv_center / 0.022) * (1.0 - abs(u - 0.950) / 0.014) * 0.72
                        cr = int(cr * (1.0 - c_str) + 25 * c_str)
                        cg = int(cg * (1.0 - c_str) + 25 * c_str)
                        cb = int(cb * (1.0 - c_str) + 30 * c_str)

                # Eyes UV sub-island in MakeHuman (u: 0.76..0.82, v: 0.93..0.96)
                if 0.760 <= u <= 0.814 and 0.931 <= v <= 0.957:
                    # Dark obsidian-brown Japanese iris with sharp pupil
                    eu = (u - 0.760) / 0.054
                    ev = (v - 0.931) / 0.026
                    rad = math.sqrt((eu - 0.5)**2 + (ev - 0.5)**2)
                    if rad < 0.18:
                        cr, cg, cb = 10, 10, 12 # Pupil
                    elif rad < 0.38:
                        cr, cg, cb = 45, 28, 18 # Dark obsidian-brown iris
                    else:
                        cr, cg, cb = 195, 190, 185 # Ivory sclera

                pixels[x, y] = (cr, cg, cb, 255)

            # 2. BODY UNDERSUIT REGION (u < 0.60): Tactical Obsidian-Carbon Bodysuit
            else:
                # Neck collar transition (keep neck skin visible where u > 0.52 and v in 0.40..0.56)
                is_neck_throat = (u > 0.50 and 0.42 <= v <= 0.54)
                if is_neck_throat:
                    # Natural skin tone on neck
                    cr = int(r * 0.70)
                    cg = int(g * 0.67)
                    cb = int(b * 0.63)
                else:
                    # Obsidian-carbon cyber-fabric with micro-hex weave and glowing cyber seams
                    hex_pattern = (int(x * 0.25) ^ int(y * 0.25)) & 3
                    base_lum = 22 + hex_pattern * 3
                    cr, cg, cb = base_lum, base_lum + 2, base_lum + 6

                    # Crimson muscle seam lines
                    seam_x = (x % 64 == 0) or ((x + y) % 128 == 0)
                    if seam_x:
                        cr = 140
                        cg = 20
                        cb = 30

                    # Cyber Cyan Nanite power lines
                    cyber_node = ((x % 256 < 6) and (y % 128 < 32)) or ((y % 256 < 6) and (x % 128 < 32))
                    if cyber_node:
                        cr = 0
                        cg = 230
                        cb = 255

                pixels[x, y] = (cr, cg, cb, 255)

    os.makedirs(os.path.dirname(OUT_TEX), exist_ok=True)
    blended.save(OUT_TEX, "PNG")
    print("Saved upgraded cyber-suit texture:", OUT_TEX)

if __name__ == "__main__":
    main()
