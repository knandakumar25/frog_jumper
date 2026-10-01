<#
.SYNOPSIS
    Exports Axiom Force for Android or other targets using WSL Containers (container.exe).
.DESCRIPTION
    Runs the headless Godot 4.7.2 .NET exporter container with the project mounted.
    Targets include: android, windows, linux, web, macos, ios, all.
.PARAMETER Target
    The export target (default: 'android'). Options: android, windows, linux, web, macos, ios, all.
.EXAMPLE
    .\export_game_wsl.ps1 -Target android
.EXAMPLE
    .\export_game_wsl.ps1 -Target all
#>
param(
    [string]$Target = "android"
)

$ErrorActionPreference = "Stop"
$ContainerCli = "C:\Program Files\WSL\container.exe"

if (-not (Test-Path $ContainerCli)) {
    Write-Error "WSL container CLI not found at '$ContainerCli'. Ensure Windows 11 26H2 WSL Containers are enabled."
}

$RepoRoot = $PSScriptRoot

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   AXIOM FORCE — Headless Exporter (WSL Containers)       " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Target platform : $Target" -ForegroundColor Yellow
Write-Host "Workspace root  : $RepoRoot" -ForegroundColor Gray

# 1. Build exporter image if not already built
$existingImages = (& $ContainerCli image list 2>$null) -join "`n"
if ($existingImages -notmatch "frog_jumper_export") {
    Write-Host "`n--> Building exporter container 'frog_jumper_export'..." -ForegroundColor Yellow
    & $ContainerCli build -t frog_jumper_export -f "$RepoRoot\export\Containerfile" "$RepoRoot"
} else {
    Write-Host "`nExporter image 'frog_jumper_export' is ready." -ForegroundColor Green
}

# 2. Run the export container with repo mounted to /workspace
Write-Host "`n--> Launching export pipeline for target: '$Target'..." -ForegroundColor Yellow
& $ContainerCli run --rm -v "${RepoRoot}:/workspace" frog_jumper_export $Target

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host "   EXPORT PROCESS FINISHED                                " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
Write-Host "Build artifacts are located in: $RepoRoot\build" -ForegroundColor Cyan
