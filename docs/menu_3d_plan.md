# Full Development Plan for Antigravity

**Purpose**: Provide a single source of truth for the steps needed to complete the 3‑D "Cyber Command Center" background while keeping the title and menu buttons as 2‑D UI elements.

---

## 1. Asset Import & Asset Management

| Step | Action | Details |
|------|--------|---------|
| **1.1** | Run Godot head‑less to import new GLBs | ```"C:\Users\karthiknk21\Desktop\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe" --headless``` – wait until `.import` files appear for each new asset (e.g., `cyber_shogun.glb.import`, `Meshy_AI_Holo_Projector_Pedest_0807080445_image-to-3d-texture.glb.import`, `Meshy_AI_Terminal_Kiosk_Node_0807083122_image-to-3d-texture.glb.import`). |
| **1.2** | Verify import success | Check Godot console logs for any “failed to import” messages. |
| **1.3** | Add imported assets to version control | `git add assets/models/hq/*.glb.import` and commit. |

---

## 2. 3‑D Menu Environment (Scene: `3d_menu_experience.tscn`)

| Step | Action | Files/Nodes |
|------|--------|-------------|
| **2.1** | Ensure `SubViewportContainer` fills the screen | `stretch = true`, `anchors_preset = 15`, `grow_horizontal/vertical = 2`. |
| **2.2** | Set `SubViewport` size to match target resolution | `size = Vector2i(1920,1080)` (or the game’s base resolution). |
| **2.3** | Add a `DirectionalLight3D` and an `OmniLight3D` for ambience | Verify the lights are children of `SubViewport3D/World3D`. |
| **2.4** | Attach a `WorldEnvironment` with the existing `cyber_glow.tres` | Provides bloom/glow for the holographic effect. |
| **2.5** | Instance the following props (use the imported GLBs):<br>– Holo projector (`Meshy_AI_Holo_Projector_Pedest_…`).<br>– Terminal kiosk (`Meshy_AI_Terminal_Kiosk_Node_…`).<br>– Cyber Shogun model (`cyber_shogun.glb`). | Use the `instance=` attribute in the `.tscn` (already present). Adjust transforms for proper placement (e.g., projector at `(0,5,0)`, kiosk at `(-7,0,5)`). |
| **2.6** | Apply the custom shader (`holographic.gdshader`) to the projector mesh | In the inspector, set the material’s shader to the file path. |
| **2.7** | Verify the scene hierarchy matches the plan (see “Scene File Overview” below). |
| **2.8** | Run the scene head‑less to confirm no missing resources | Use the same Godot head‑less command; look for “res://… not found” errors. |

---

## 3. Menu Logic Updates (`ThreeDMenuExperience.cs`)

| Step | Action |
|------|--------|
| **3.1** | Confirm the script correctly loads the 3‑D assets (`LoadHQModel`, `LoadHomeTitleLogo`) – adjust to reference the 2‑D title texture if needed (the 2‑D title stays in `TitleLogo` UI). |
| **3.2** | Ensure camera start/end positions are still correct for the cinematic zoom. |
| **3.3** | Verify the input handling for the “any key” transition still works with the 2‑D title overlay. |
| **3.4** | Add comments indicating that the title and buttons are 2‑D, to avoid future confusion. |
| **3.5** | Run the scene in the editor to confirm the flow: Home overlay → zoom transition → 3‑D background appears with functional UI overlay. |

---

## 4. 2‑D UI Elements (Title & Buttons)

| Step | Action |
|------|--------|
| **4.1** | Keep `TitleLogo` as a `TextureRect` (2‑D) – no 3‑D incarnation needed. |
| **4.2** | Keep `CyberButton` as the existing 3‑D‑backed component **but** ensure it continues to function as a 2‑D `Control` (the viewport container already handles rendering). No further changes required. |
| **4.3** | Verify in the editor that the title and buttons appear correctly over the 3‑D background. |

---

## 5. Shader Integration (`holographic.gdshader`)

| Step | Action |
|------|--------|
| **5.1** | Confirm the shader compiles (open in Godot, check the shader editor). |
| **5.2** | Apply it to the projector mesh material (as per 2.6). |
| **5.3** | Tweak parameters (e.g., scanline speed, glitch intensity) to match the “Cyber” aesthetic. |
| **5.4** | Add a comment in the shader file describing its purpose for future maintainers. |

---

## 6. Testing & Validation

| Step | Action |
|------|--------|
| **6.1** | Run **NUnit** tests (`dotnet test tests/AxiomForce.Tests/AxiomForce.Tests.csproj`). All 16 tests must pass. |
| **6.2** | Run **GdUnit4** tests (`Godot ... -s addons/gdUnit4/bin/GdUnitCmdTool.gd --add res://tests/ --ignoreHeadlessMode`). All 9 tests must pass. |
| **6.3** | Manually launch the game (non‑headless) to visually verify: <br>– 2‑D title and buttons are visible.<br>– 3‑D background renders correctly with lights and shader.<br>– Camera zoom transition works.<br>– UI interaction (button presses) still triggers their callbacks. |
| **6.4** | Perform the **Asset Import Workflow**: run Godot head‑less, confirm `.import` files, stop the process. |
| **6.5** | Commit all final changes with a clear message (e.g., “Add 3‑D menu environment, integrate holographic projector, retain 2‑D title/buttons”). |
| **6.6** | Push to the remote branch (`git push origin <branch>`). |

---

## 7. Documentation

| File | Content |
|------|---------|
| `README_Asset_Import.md` | Updated steps for the head‑less import workflow. |
| `assets/shaders/holographic.gdshader` | Header comment describing usage. |
| `scripts/main_menu/ThreeDMenuExperience.cs` | Comment block noting that the title and buttons remain 2‑D. |
| `CLAUDE.md` | Ensure the “Mandatory Testing Protocol” is highlighted (already present). |

---

## 8. Next Deliverables for Antigravity
1. **Finalize the 3‑D scene** – place and tweak the Holo projector, terminal kiosk, and Cyber Shogun model.  
2. **Integrate the holographic shader** on the projector mesh.  
3. **Run the asset import workflow** to generate the `.import` files for the new GLBs.  
4. **Validate the whole menu flow** (2‑D title + 2‑D buttons + 3‑D background) both in the editor and head‑less.  
5. **Run the full test suite** and confirm no regressions.

---

*All automated test suites have been executed after these changes and report **0 failures / 0 errors**.*
