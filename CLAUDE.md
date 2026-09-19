# Axiom Force - Project Guide

## Project Overview
A high-fidelity 3D action game developed in **Godot 4.7.2 .NET (C#)**. The current focus is the "Neon Pulse" visual pass, transforming the Main Menu into a high-fidelity 3D "Cyber Command Center".

## Technical Stack
- **Engine**: Godot 4.7.2 .NET
- **Language**: C#
- **Architecture**: Component-based with specialized Managers (e.g., `SessionManager`, `UISoundManager`).
- **UI Approach**: Mix of 2D `Control` nodes and 3D-in-2D using `SubViewportContainer`.

## Development Workflows

### Asset Import Workflow
Whenever 3D assets, textures, or fonts are added/modified:
1. Run Godot in the background: `"C:\Users\karthiknk21\Desktop\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe" --headless`
2. Wait for `.import` files to be generated.
3. Stop the Godot process.
*This ensures assets are visible in the runtime.*

### Build & Test
- **Build project**: `dotnet build`
- **Verification**: Check Godot runtime logs for "Vector3 cannot be normalized" warnings or missing asset errors.

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
