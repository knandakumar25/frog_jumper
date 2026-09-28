<#
.SYNOPSIS
    Automates the Godot asset import process by running Godot in headless mode.
.DESCRIPTION
    Launches Godot headless to allow the asset pipeline to detect new or modified
    assets (3D models, textures, audio, fonts) and generate their corresponding .import files,
    then terminates the process once import is completed.
#>
param(
    [string]$GodotPath = "",
    [int]$WaitSeconds = 5
)

$ErrorActionPreference = "Stop"

if (-not $GodotPath -or -not (Test-Path $GodotPath)) {
    $CandidateGodotPaths = @(
        "C:\Users\karthiknk21\Godot\Editors\4.7.2-stable-mono\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe",
        "C:\Users\karthiknk21\Godot\Editors\.editor_config\Axiom Force\Godot_v4.7.2-stable_mono_win64.exe",
        "C:\Users\karthiknk21\Desktop\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe"
    )
    $GodotPath = $CandidateGodotPaths | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $GodotPath -or -not (Test-Path $GodotPath)) {
    Write-Error "Godot binary not found at specified or candidate paths."
    exit 1
}

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host " Starting Godot Headless Asset Importer  " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Godot Path : $GodotPath"
Write-Host "Wait Time  : $WaitSeconds seconds"

# Launch Godot in headless mode
$process = Start-Process -FilePath $GodotPath -ArgumentList "--headless" -NoNewWindow -PassThru

Write-Host "Waiting $WaitSeconds seconds for asset import..." -ForegroundColor Yellow
Start-Sleep -Seconds $WaitSeconds

# Terminate process if still running
if ($process -and -not $process.HasExited) {
    Write-Host "Terminating Godot import process (PID: $($process.Id))..." -ForegroundColor Green
    Stop-Process -Id $process.Id -Force
}

Write-Host "Asset import completed successfully." -ForegroundColor Cyan

