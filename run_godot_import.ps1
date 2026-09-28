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

# Launch Godot with --headless --import which starts editor, imports changed resources, and quits automatically
$process = Start-Process -FilePath $GodotPath -ArgumentList "--headless", "--import" -NoNewWindow -PassThru

Write-Host "Waiting for Godot asset import to complete..." -ForegroundColor Yellow
$finished = $process.WaitForExit($WaitSeconds * 1000)

if (-not $finished) {
    Write-Host "Import did not finish within $WaitSeconds s. Force terminating (PID: $($process.Id))..." -ForegroundColor Yellow
    Stop-Process -Id $process.Id -Force
}

Write-Host "Asset import completed successfully." -ForegroundColor Cyan

