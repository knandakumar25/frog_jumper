<#
.SYNOPSIS
    Stops and cleans up Axiom Force containers in Windows 11 26H2 WSL Containers (container.exe).
.DESCRIPTION
    Stops:
      - frog_jumper_website
      - frog_jumper_backend
      - frog_jumper_db
    Keeps database volume intact unless -CleanVolumes is specified.
#>

param(
    [switch]$CleanVolumes,
    [switch]$RemoveImages
)

$ErrorActionPreference = "Continue"
$ContainerCli = "C:\Program Files\WSL\container.exe"

if (-not (Test-Path $ContainerCli)) {
    Write-Error "WSL container CLI not found at '$ContainerCli'."
    exit 1
}

Write-Host "=== Stopping Axiom Force Stack (WSL Containers) ===" -ForegroundColor Cyan

$services = @("frog_jumper_website", "frog_jumper_backend", "frog_jumper_db")

foreach ($svc in $services) {
    Write-Host "Stopping and removing container '$svc'..." -ForegroundColor Yellow
    & $ContainerCli rm -f $svc 2>$null | Out-Null
}

if ($CleanVolumes) {
    Write-Host "Removing persistent volume 'frog_jumper_postgres_data'..." -ForegroundColor Red
    & $ContainerCli volume remove frog_jumper_postgres_data 2>$null | Out-Null
}

if ($RemoveImages) {
    Write-Host "Removing project images..." -ForegroundColor Red
    & $ContainerCli image remove frog_jumper_website frog_jumper_backend 2>$null | Out-Null
}

Write-Host "=== All WSL Containers Stopped ===" -ForegroundColor Green
& $ContainerCli ps
