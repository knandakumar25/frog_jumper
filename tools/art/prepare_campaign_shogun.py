"""Pack the approved AI-generated Shogun pose sheets into a stable game atlas.

Run with Python 3 and Pillow from the project root. The source art is preserved;
only transparent crops and padding are added for consistent AnimatedSprite2D pivots.
"""
from pathlib import Path
from PIL import Image


PROJECT = Path(__file__).resolve().parents[2]
POSES = PROJECT / "assets/characters/shogun_study/poses.png"
LOCOMOTION = PROJECT / "assets/characters/shogun_study/locomotion.png"
OUTPUT = PROJECT / "assets/characters/shogun_study/campaign_atlas.png"
CELL = 256
BASELINE = 226
MAX_WIDTH = 220
MAX_HEIGHT = 224

# These are the same explicit character bounds used by the selection preview.
POSE_REGIONS = [
    (40, 70, 240, 260), (360, 70, 220, 260),
    (680, 70, 230, 260), (990, 70, 240, 260),
    (20, 400, 260, 230), (350, 400, 260, 230),
    (660, 420, 260, 210), (990, 390, 240, 235),
    (20, 670, 270, 260), (350, 700, 320, 230),
    (690, 680, 230, 250), (970, 680, 270, 250),
    (20, 1010, 330, 200), (370, 950, 270, 280),
    (680, 950, 250, 280), (950, 1010, 300, 244),
]


def visible_crop(source: Image.Image, bounds: tuple[int, int, int, int]) -> Image.Image:
    """Clip one generated pose to its hand-authored bounds and trim transparent gutters."""
    x, y, width, height = bounds
    crop = source.crop((x, y, x + width, y + height))
    alpha = crop.getchannel("A")
    used = alpha.getbbox()
    return crop.crop(used) if used else crop


def anchored_cell(crop: Image.Image) -> Image.Image:
    """Fit a pose into one transparent frame while sharing a fixed foot baseline."""
    scale = min(MAX_WIDTH / crop.width, MAX_HEIGHT / crop.height)
    fitted = crop.resize((round(crop.width * scale), round(crop.height * scale)), Image.Resampling.LANCZOS)
    cell = Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
    left = (CELL - fitted.width) // 2
    top = BASELINE - fitted.height
    cell.alpha_composite(fitted, (left, top))
    return cell


def locomotion_cells(source: Image.Image) -> list[Image.Image]:
    """Crop the eight AI-generated run poses using the selection preview's row geometry."""
    cell_width = source.width / 4
    row_tops = (40, 370, 700, 975)
    row_heights = (310, 310, 270, 279)
    result = []
    for frame in range(8, 16):
        row = frame // 4
        x0 = round((frame % 4) * cell_width)
        x1 = round((frame % 4 + 1) * cell_width)
        crop = source.crop((x0, row_tops[row], x1, row_tops[row] + row_heights[row]))
        used = crop.getchannel("A").getbbox()
        result.append(anchored_cell(crop.crop(used) if used else crop))
    return result


def main() -> None:
    """Build idle, run, jump, dash and two combat rows from the final AI artwork."""
    poses = Image.open(POSES).convert("RGBA")
    locomotion = Image.open(LOCOMOTION).convert("RGBA")
    pose_cells = [anchored_cell(visible_crop(poses, region)) for region in POSE_REGIONS]
    idle = [pose_cells[0], pose_cells[1]] * 4
    jump = [pose_cells[6], pose_cells[7]] * 4
    dash = [pose_cells[12]] * 8
    sword = [pose_cells[8], pose_cells[9]] * 4
    martial = [pose_cells[10], pose_cells[11]] * 4
    rows = [idle, locomotion_cells(locomotion), jump, dash, sword, martial]
    atlas = Image.new("RGBA", (CELL * 8, CELL * len(rows)), (0, 0, 0, 0))
    for row_index, row in enumerate(rows):
        for column, cell in enumerate(row):
            atlas.alpha_composite(cell, (column * CELL, row_index * CELL))
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    atlas.save(OUTPUT, optimize=True)
    print(f"CAMPAIGN_SHOGUN_ATLAS={OUTPUT}")


if __name__ == "__main__":
    main()
