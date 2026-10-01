<#
.SYNOPSIS
    Builds and prepares Axiom Force WSL Containers without Docker Desktop.
.DESCRIPTION
    1. Ensures base images (PostgreSQL, Python 3.14, Node, Nginx) are loaded into WSL Containers.
    2. Builds 'frog_jumper_backend' and 'frog_jumper_website' using container.exe.
    3. Initializes the internal bridge network ('frog_network') and persistent volume ('frog_jumper_postgres_data').
    Does NOT start the containers. Use .\run_wsl_containers.ps1 when ready to run.
#>

param(
    [switch]$IncludeExporter = $false
)

$ErrorActionPreference = "Stop"
$ContainerCli = "C:\Program Files\WSL\container.exe"

if (-not (Test-Path $ContainerCli)) {
    Write-Error "WSL container CLI not found at '$ContainerCli'. Ensure Windows 11 26H2 WSL Containers are enabled."
}

$RepoRoot = $PSScriptRoot
$PythonExe = if (Test-Path "$RepoRoot\backend\.venv\Scripts\python.exe") {
    "$RepoRoot\backend\.venv\Scripts\python.exe"
} else {
    "python"
}
$FetchScript = "$RepoRoot\scripts\wsl_containers\fetch_image.py"
$StagingDir = "$RepoRoot\.container_cache"

Write-Host "=== Setting Up Axiom Force WSL Containers ===" -ForegroundColor Cyan

# 1. Check existing images
$existingImages = (& $ContainerCli image list 2>$null) -join "`n"

$baseImages = @(
    @{ Name = "postgres:18-alpine"; File = "postgres_18_alpine.tar" },
    @{ Name = "python:3.14-slim";  File = "python_3_14_slim.tar" },
    @{ Name = "node:20-alpine";     File = "node_20_alpine.tar" },
    @{ Name = "nginx:alpine";       File = "nginx_alpine.tar" }
)

New-Item -ItemType Directory -Path $StagingDir -Force | Out-Null

try {
    foreach ($img in $baseImages) {
        $imgName = $img.Name
        if ($existingImages -notmatch [regex]::Escape($imgName)) {
            Write-Host "`n--> Fetching and loading base image: $imgName..." -ForegroundColor Yellow
            $tarPath = Join-Path $StagingDir $img.File
            & $PythonExe $FetchScript $imgName $tarPath
            & $ContainerCli image load -i $tarPath
            Remove-Item $tarPath -Force -ErrorAction SilentlyContinue
        } else {
            Write-Host "Base image '$imgName' already loaded in WSL Containers." -ForegroundColor Green
        }
    }

    # 2. Build Backend Container
    Write-Host "`n--> Building 'frog_jumper_backend' (Python 3.14)..." -ForegroundColor Yellow
    & $ContainerCli build -t frog_jumper_backend -f "$RepoRoot\backend\Containerfile" "$RepoRoot\backend"

    # 3. Build Website Container
    Write-Host "`n--> Building 'frog_jumper_website' (React + Vite on Nginx)..." -ForegroundColor Yellow
    & $ContainerCli build -t frog_jumper_website -f "$RepoRoot\website\Containerfile" "$RepoRoot\website"

    # 4. (Optional) Build Exporter Container
    if ($IncludeExporter) {
        Write-Host "`n--> Building 'frog_jumper_export' (Headless Godot 4.7.2 + Android SDK)..." -ForegroundColor Yellow
        & $ContainerCli build -t frog_jumper_export -f "$RepoRoot\export\Containerfile" "$RepoRoot"
    }

    # 5. Create Network and Volume
    Write-Host "`n--> Ensuring network and volume exist..." -ForegroundColor Yellow
    $networks = (& $ContainerCli network list 2>$null) -join "`n"
    if ($networks -notmatch "frog_network") {
        & $ContainerCli network create frog_network 2>$null | Out-Null
    }
    $volumes = (& $ContainerCli volume list 2>$null) -join "`n"
    if ($volumes -notmatch "frog_jumper_postgres_data") {
        & $ContainerCli volume create frog_jumper_postgres_data 2>$null | Out-Null
    }

    Write-Host "`n=== WSL Containers Setup Complete ===" -ForegroundColor Green
    Write-Host "Images and infrastructure are ready."
    Write-Host "To start the development stack, run:" -ForegroundColor Cyan
    Write-Host "  .\run_wsl_containers.ps1`n" -ForegroundColor White
} finally {
    Remove-Item $StagingDir -Recurse -Force -ErrorAction SilentlyContinue
}
