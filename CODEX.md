# Axiom Force - Project Guide (Codex)

## Project Overview
A high-fidelity 3D action game developed in **Godot 4.7.2 .NET (C#)** with a **Django REST Framework** backend and React web client. The current focus is the "Neon Pulse" visual pass, transforming the Main Menu into a high-fidelity 3D "Cyber Command Center".

## Technical Stack
- **Engine**: Godot 4.7.2 .NET
- **Language**: C#
- **Backend**: Django REST Framework (Python 3.14)
- **Frontend Web**: React + Vite
- **Architecture**: Component-based with specialized Managers (e.g., `SessionManager`, `UISoundManager`).
- **UI Approach**: Mix of 2D `Control` nodes and 3D-in-2D using `SubViewportContainer`.

## Development Workflows

### Asset Import Workflow
Whenever 3D assets, textures, or fonts are added/modified:
1. Run Godot in the background: `"C:\Users\karthiknk21\Godot\Editors\4.7.2-stable-mono\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe" --headless`
2. Wait for `.import` files to be generated.
3. Stop the Godot process.
*This ensures assets are visible in the runtime.*

### Build & Test
- **Build project**: `dotnet build AxiomForce.csproj`
- **Verification**: Check Godot runtime logs for "Vector3 cannot be normalized" warnings or missing asset errors.

## Mandatory Testing Protocol (End of Every Prompt)
> [!IMPORTANT]
> At the conclusion of EVERY prompt or task, you MUST execute the unit test suites and verify that all tests pass (0 failures, 0 errors) before concluding your turn:
>
> ```powershell
> .\run_tests.ps1
> ```
>
> You can also run the individual test suites:
> 1. **Django Unit Tests (Backend)**:
>    ```powershell
>    python backend/manage.py test accounts
>    # Or directly using the virtual environment:
>    backend\.venv\Scripts\python.exe backend\manage.py test accounts
>    ```
> 2. **NUnit Tests (.NET 10 C#)**:
>    ```powershell
>    dotnet test tests/AxiomForce.Tests/AxiomForce.Tests.csproj
>    ```
> 3. **GdUnit4 Tests (Godot 4 GDScript)**:
>    ```powershell
>    & "C:\Users\karthiknk21\Godot\Editors\4.7.2-stable-mono\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe" --headless --path . -s addons/gdUnit4/bin/GdUnitCmdTool.gd --add res://tests/ --ignoreHeadlessMode
>    ```

## Coding Guidelines
- **Naming**: Match existing C# idioms (PascalCase for methods/properties, `_camelCase` for private fields).
- **Godot API**: 
    - Use `GetNodeOrNull<T>()` for safer node retrieval.
    - Prefer `Callable.From()` for signal connections in C#.
    - When using `SubViewport` for UI, ensure a `DirectionalLight3D` and `WorldEnvironment` are present within the viewport for visibility.
- **Safety**: Always check `LengthSquared() > 0` before calling `.Normalized()` on vectors to avoid runtime warnings.

## Core Components
- `MainMenu.cs`: Central coordinator for the menu UI.
- `CyberButton.cs`: 3D-backed interactive buttons with parallax tilting.
- `3d_menu_experience.tscn`: The immersive 3D environment containing the Holo Projector and Terminal Kiosk.
- `holographic.gdshader`: Custom spatial shader for holographic projections.

# Permissions Denylist
(Add any restricted actions here)
