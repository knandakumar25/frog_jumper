# Controller button images

These are the button images shown in place of text when a controller is in use. `scripts/input/ControllerGlyphCatalog.cs` maps each controller input to a file here, and `scripts/input/InputGlyphText.cs` draws them inline in prompt labels and buttons.

## Source and licence

- **Pack:** Kenney "Input Prompts" 1.5A (www.kenney.nl), created 2026-07-11. Downloaded on 2026-11-17 from https://kenney.nl/assets/input-prompts.
- **Licence:** Creative Commons Zero (CC0). The pack's own licence file is [`LICENSE.txt`](LICENSE.txt). Crediting Kenney is optional.
- **Files used:** the 128x128 PNGs from each family's `Double` folder, copied unchanged and renamed.
- **Import settings:** mipmaps are on, and rich labels draw them with `LinearWithMipmaps`, so the images stay smooth when shrunk to text height.

## Owner exception to the art rule

These images are **not** SpriteCook art and **not** stand-ins. On 2026-11-17 the owner chose a free CC0 icon pack for controller button images, as a deliberate exception to the SpriteCook-only rule in `CLAUDE.md`. Do not delete or regenerate them unless the owner asks.

## Sets

| Folder | Used for |
|---|---|
| `xbox_360/` | Xbox 360 pads; the "Xbox 360" style |
| `xbox_one/` | Xbox One pads; the "Xbox One" style |
| `xbox_series/` | Xbox Series X\|S pads and any other Microsoft or XInput pad; the "Xbox Series X\|S" style; Xbox console builds (`xbox` feature tag) |
| `ps5/` | Every Sony pad, DualShock 4 included; the "PlayStation 5" style; the future `ps5` feature tag |
| `switch2/` | Every Nintendo pad; the "Nintendo Switch 2" style; the future `switch2` feature tag |

Unknown controllers and the keyboard keep text labels.

**What actually differs between the three Xbox sets:**
- Xbox 360 uses the BACK and START buttons.
- Xbox One and Xbox Series both use View and Menu, and their button images are identical. The pack has no art that differs between those two controllers for the buttons the game uses. The Series Share button has an image in the pack, but the game binds nothing to it.
- All three Xbox sets show the coloured A/B/X/Y letters.

## File mapping (project name → Kenney file)

The face buttons are named by position (`face_south`, `face_east`, `face_west`, `face_north`), following Godot's `JoyButton.A`, `B`, `X`, `Y`. A Nintendo pad's south button is therefore its **B** button.

| Project file | Xbox 360 | Xbox One / Series | PS5 | Switch 2 |
|---|---|---|---|---|
| face_south | xbox_button_color_a | xbox_button_color_a | playstation_button_cross | switch_button_b |
| face_east | xbox_button_color_b | xbox_button_color_b | playstation_button_circle | switch_button_a |
| face_west | xbox_button_color_x | xbox_button_color_x | playstation_button_square | switch_button_y |
| face_north | xbox_button_color_y | xbox_button_color_y | playstation_button_triangle | switch_button_x |
| back | xbox_button_back | xbox_button_view | playstation5_button_create | switch_button_minus |
| start | xbox_button_start | xbox_button_menu | playstation5_button_options | switch_button_plus |
| guide | xbox_guide | xbox_guide | (none: text) | switch_button_home |
| shoulder_left / right | xbox_lb / xbox_rb | xbox_lb / xbox_rb | playstation_trigger_l1 / r1 | switch_button_l / r |
| trigger_left / right | xbox_lt / xbox_rt | xbox_lt / xbox_rt | playstation_trigger_l2 / r2 | switch_button_zl / zr |
| stick_left_press / right | xbox_ls / xbox_rs | xbox_ls / xbox_rs | playstation_button_l3 / r3 | switch_stick_l_press / r_press |
| dpad_{up,down,left,right} | xbox_dpad_* | xbox_dpad_* | playstation_dpad_* | switch_dpad_* |
| stick_left_{dir} | xbox_stick_l_* | xbox_stick_l_* | playstation_stick_l_* | switch_stick_l_* |
| stick_right_{dir} | xbox_stick_r_* | xbox_stick_r_* | playstation_stick_r_* | switch_stick_r_* |

`ControllerGlyphCatalogTests` (NUnit) checks that every mapped file exists and is 128x128.
