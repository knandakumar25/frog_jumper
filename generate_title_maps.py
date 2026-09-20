import bpy
import os
import numpy as np

def generate_maps():
    input_path = os.path.abspath("assets/images/axiom_force_logo_clean.png")
    if not os.path.exists(input_path):
        print("Input file not found:", input_path)
        return

    img = bpy.data.images.load(input_path)
    w, h = img.size
    pix = np.array(img.pixels[:], dtype=np.float32).reshape((h, w, 4))
    
    r = pix[:, :, 0]
    g = pix[:, :, 1]
    b = pix[:, :, 2]
    a = pix[:, :, 3]

    lum = 0.299 * r + 0.587 * g + 0.114 * b
    max_c = np.maximum(np.maximum(r, g), b)
    min_c = np.minimum(np.minimum(r, g), b)
    chroma = max_c - min_c

    # ─────────────────────────────────────────────────────────────────────────────
    # 1. EMISSION MAP
    # Neon cyan: high b and g, moderate/low r
    is_cyan = (b > 0.45) & (g > 0.35) & (chroma > 0.20)
    # Neon magenta / pink: high r and b, low g
    is_magenta = (r > 0.40) & (b > 0.25) & (chroma > 0.20)
    # Intense core glows
    is_intense_glow = (lum > 0.88) & (a > 0.5)

    emission_mask = np.zeros((h, w), dtype=np.float32)
    # Cyan neon glow factor
    emission_mask = np.where(is_cyan, np.clip(chroma * 1.6, 0.0, 1.0), emission_mask)
    # Magenta neon glow factor
    emission_mask = np.where(is_magenta, np.clip(chroma * 1.8, 0.0, 1.0), emission_mask)
    # Intense core white highlights
    emission_mask = np.where(is_intense_glow, 1.0, emission_mask)
    # Zero out transparent background
    emission_mask = emission_mask * (a > 0.1).astype(np.float32)

    emission_rgb = np.zeros((h, w, 4), dtype=np.float32)
    # Multiply original vibrant color by emission mask
    emission_rgb[:, :, 0] = r * emission_mask
    emission_rgb[:, :, 1] = g * emission_mask
    emission_rgb[:, :, 2] = b * emission_mask
    emission_rgb[:, :, 3] = a

    # ─────────────────────────────────────────────────────────────────────────────
    # 2. METALLIC MAP
    # Letters and frame chassis have high metallic
    # Letters are bright chrome (high lum, lower chroma)
    is_chrome = (lum > 0.45) & (chroma < 0.30) & (a > 0.5)
    is_chassis = (lum <= 0.45) & (a > 0.5)

    metallic = np.zeros((h, w, 4), dtype=np.float32)
    m_val = np.zeros((h, w), dtype=np.float32)
    m_val = np.where(is_chassis, 0.75, m_val)
    m_val = np.where(is_chrome, 0.96, m_val)
    # Glowing neon lines have lower metallic so color comes through cleanly
    m_val = np.where(emission_mask > 0.5, 0.1, m_val)
    m_val = m_val * a

    metallic[:, :, 0] = m_val
    metallic[:, :, 1] = m_val
    metallic[:, :, 2] = m_val
    metallic[:, :, 3] = a

    # ─────────────────────────────────────────────────────────────────────────────
    # 3. ROUGHNESS MAP
    # Chrome is smooth / specular (0.16)
    # Chassis is satin (0.42)
    # Neon is smooth (0.12)
    roughness = np.zeros((h, w, 4), dtype=np.float32)
    r_val = np.ones((h, w), dtype=np.float32) * 0.42
    r_val = np.where(is_chrome, 0.16, r_val)
    r_val = np.where(emission_mask > 0.4, 0.12, r_val)
    r_val = np.where(a < 0.1, 1.0, r_val)

    roughness[:, :, 0] = r_val
    roughness[:, :, 1] = r_val
    roughness[:, :, 2] = r_val
    roughness[:, :, 3] = a

    # ─────────────────────────────────────────────────────────────────────────────
    # 4. NORMAL MAP (Sobel gradient of height / lum)
    # Pad image to compute gradients safely
    pad = 1
    lum_padded = np.pad(lum, pad, mode='edge')
    
    # Sobel filters
    # Gx: [[-1, 0, 1], [-2, 0, 2], [-1, 0, 1]]
    # Gy: [[-1, -2, -1], [0, 0, 0], [1, 2, 1]]
    gx = (
        -1 * lum_padded[:-2, :-2] + 1 * lum_padded[:-2, 2:] +
        -2 * lum_padded[1:-1, :-2] + 2 * lum_padded[1:-1, 2:] +
        -1 * lum_padded[2:, :-2] + 1 * lum_padded[2:, 2:]
    )
    gy = (
        -1 * lum_padded[:-2, :-2] - 2 * lum_padded[:-2, 1:-1] - 1 * lum_padded[:-2, 2:] +
         1 * lum_padded[2:, :-2] + 2 * lum_padded[2:, 1:-1] + 1 * lum_padded[2:, 2:]
    )

    normal_strength = 2.5
    nx = -gx * normal_strength
    ny = -gy * normal_strength
    nz = np.ones_like(nx)

    length = np.sqrt(nx**2 + ny**2 + nz**2)
    nx /= length
    ny /= length
    nz /= length

    normal = np.zeros((h, w, 4), dtype=np.float32)
    normal[:, :, 0] = nx * 0.5 + 0.5
    normal[:, :, 1] = ny * 0.5 + 0.5
    normal[:, :, 2] = nz * 0.5 + 0.5
    normal[:, :, 3] = a

    # ─────────────────────────────────────────────────────────────────────────────
    # SAVE ALL MAPS
    def save_img(data, filename):
        out_path = os.path.abspath(os.path.join("assets/images", filename))
        if os.path.exists(out_path):
            os.remove(out_path)
        out_bimg = bpy.data.images.new(filename, width=w, height=h, alpha=True)
        out_bimg.pixels = data.flatten().tolist()
        out_bimg.filepath_raw = out_path
        out_bimg.file_format = 'PNG'
        out_bimg.save()
        print("Saved map to:", out_path)

    save_img(emission_rgb, "axiom_force_emission.png")
    save_img(metallic, "axiom_force_metallic.png")
    save_img(roughness, "axiom_force_roughness.png")
    save_img(normal, "axiom_force_normal.png")
    print("All PBR maps successfully generated!")

if __name__ == '__main__':
    generate_maps()

