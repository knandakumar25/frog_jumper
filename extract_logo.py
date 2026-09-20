import bpy
import os
import numpy as np

# Load original logo image
img_path = os.path.abspath('assets/images/axiom_force_title_logo.jpg')
img = bpy.data.images.load(img_path)
w, h = img.size
pix = np.array(img.pixels[:], dtype=np.float32).reshape((h, w, 4))

# Polygon around the logo emblem in image coordinates (row 0 is bottom in Blender)
# Symmetrical around center X = 688
poly = [
    (688, 616), (670, 600), (650, 576), (620, 580), (600, 608), (580, 614), (565, 555), (545, 526),
    (485, 520), (245, 520), (170, 435), (165, 345), (245, 252), (335, 245),
    (375, 210), (645, 210), (665, 175), (688, 172), (711, 175), (731, 210),
    (1001, 210), (1041, 245), (1131, 252), (1211, 345), (1206, 435), (1131, 520),
    (891, 520), (830, 526), (810, 555), (796, 614), (776, 608), (756, 580), (726, 576), (706, 600)
]

# Scanline fill algorithm to generate binary mask
mask = np.zeros((h, w), dtype=np.float32)
n = len(poly)

for y in range(h):
    intersections = []
    for i in range(n):
        x1, y1 = poly[i]
        x2, y2 = poly[(i + 1) % n]
        if min(y1, y2) <= y < max(y1, y2):
            # Calculate x intersection
            x_int = x1 + (y - y1) * (x2 - x1) / (y2 - y1)
            intersections.append(x_int)
    intersections.sort()
    for j in range(0, len(intersections) - 1, 2):
        x_start = max(0, int(np.floor(intersections[j])))
        x_end = min(w, int(np.ceil(intersections[j + 1])))
        mask[y, x_start:x_end] = 1.0

# 3-pixel smooth anti-aliased edge
kernel = np.ones((5, 5), dtype=np.float32) / 25.0
# Fast box blur for alpha
smooth_mask = np.zeros_like(mask)
pad = 2
padded = np.pad(mask, pad, mode='edge')
for dy in range(-pad, pad + 1):
    for dx in range(-pad, pad + 1):
        smooth_mask += padded[pad + dy : pad + dy + h, pad + dx : pad + dx + w]
smooth_mask /= ((2 * pad + 1) ** 2)

# Set alpha channel
pix_out = pix.copy()
pix_out[:, :, 3] = np.clip(smooth_mask, 0.0, 1.0)

# Crop to tight bounding box with comfortable 20px padding
min_x = 135
max_x = 1241
min_y = 145
max_y = 665
cropped = pix_out[min_y:max_y, min_x:max_x, :]
ch, cw, _ = cropped.shape

# Save transparent PNG
out_img = bpy.data.images.new('AxiomForceLogoClean', width=cw, height=ch, alpha=True)
out_img.pixels = cropped.flatten().tolist()
out_path = os.path.abspath('assets/images/axiom_force_logo_clean.png')
out_img.filepath_raw = out_path
out_img.file_format = 'PNG'
out_img.save()
print(f'Successfully exported clean logo PNG ({cw}x{ch}) to: {out_path}')
