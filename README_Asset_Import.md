# Asset Import Workflow for Godot 4.7.2 .NET

## Overview
Godot requires `.import` files for all non-native assets (such as textures, fonts, and audio). When adding or editing assets outside of the Godot Editor, these `.import` files are not generated automatically. Without them, runtime calls like `GD.Load<PackedScene>("res://...")` will fail to find the resources.

## Quick Automated Import

You can run the provided automation helper script:

### PowerShell (Windows)
```powershell
.\run_godot_import.ps1
```

### Bash (Linux / Git Bash / WSL)
```bash
./run_godot_import.sh
```

These scripts execute the following steps automatically:
1. Start Godot in headless mode:
   `"C:\Users\karthiknk21\Desktop\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe" --headless`
2. Sleep for 5 seconds while Godot imports the new assets and generates their `.import` files.
3. Terminate the headless Godot background process.

## Manual Import Procedure

If running manually in PowerShell or Command Prompt:

1. Launch Godot in the background:
   ```powershell
   Start-Process -FilePath "C:\Users\karthiknk21\Desktop\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe" -ArgumentList "--headless" -NoNewWindow
   ```
2. Wait a few seconds until corresponding `.import` files appear in the asset directories (e.g., `assets/images/operatives/*.import`).
3. Terminate the process:
   ```powershell
   Stop-Process -Name "Godot_v4.7.2-stable_mono_win64" -Force
   ```

## Verification
- Verify that your asset has a corresponding `<asset_name>.<ext>.import` file.
- Run `dotnet build` to ensure the C# solution compiles cleanly.
- Check runtime logs to ensure no missing-resource errors appear.

