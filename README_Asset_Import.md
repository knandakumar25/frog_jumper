# Asset Import Workflow for Godot 4.7.2 .NET

## Overview
Godot requires `.import` files for all non-native assets (such as textures, fonts, audio, and atlases). When adding or editing assets outside of the Godot Editor, these `.import` files are not generated automatically. Without them, runtime calls like `GD.Load<PackedScene>("res://...")` will fail to find the resources.

## Recommended Background Import Command

After creating, modifying, or deleting asset files, run the background import refresh from the project root using the console executable:

```powershell
& "C:\Users\karthiknk21\Desktop\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe" --headless --path . --editor --import
```

> [!TIP]
> Always use `Godot_v4.7.2-stable_mono_win64_console.exe` on Windows to prevent stdout deadlocks during headless execution.

## Automated Import Scripts

You can also run the provided project helper scripts:

### PowerShell (Windows)
```powershell
.\run_godot_import.ps1
```

### Bash (Linux / Git Bash / WSL)
```bash
./run_godot_import.sh
```

## Verification Checklist
- Verify that every new/modified asset has a corresponding `<asset_name>.<ext>.import` file.
- Run `dotnet build AxiomForce.csproj` to confirm the C# solution builds cleanly.
- Run `.\run_tests.ps1` to ensure all unit and integration tests pass (0 failures, 0 errors, 0 orphans).
