# Optimize the website's display GLBs for the web.
#
# Recipe (Phase 0 of the 3D website redesign):
#   Draco geometry + WebP textures at 1024px + solid-texture pruning,
#   NO mesh simplification (fidelity-safe; the models are display-only and
#   carry no animations in website/public/models).
#
# Usage:
#   .\tools\optimize_web_models.ps1                          # optimize in place
#   .\tools\optimize_web_models.ps1 -Source assets\models\hq # from the game set
#   .\tools\optimize_web_models.ps1 -OutDir .validation\opt  # dry run elsewhere
#
# Requires: node/npm (uses npx @gltf-transform/cli). Originals stay in git
# history; this rewrites the files in place by default.

param(
    [string]$Source = "website\public\models",
    [string]$OutDir = "",
    [int]$TextureSize = 1024,
    [switch]$OverwriteSource
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$SourcePath = Join-Path $RepoRoot $Source
if (-not $OutDir) { $OutDir = $SourcePath } else { $OutDir = Join-Path $RepoRoot $OutDir }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

Write-Host "Optimizing GLBs: $SourcePath -> $OutDir" -ForegroundColor Cyan
$results = @()

Get-ChildItem -Path $SourcePath -Filter *.glb | ForEach-Object {
    $in = $_.FullName
    $before = $_.Length
    $tmp = Join-Path $env:TEMP ("opt_" + $_.Name)
    $out = if ($OutDir -eq $SourcePath) { $tmp } else { Join-Path $OutDir $_.Name }

    Write-Host ("  {0} ({1:N1} MB)..." -f $_.Name, ($before / 1MB)) -ForegroundColor Yellow
    & npx --yes "@gltf-transform/cli@latest" optimize $in $out `
        --compress draco `
        --texture-compress webp `
        --texture-size $TextureSize `
        --simplify false `
        --join true --flatten true 2>&1 | Select-Object -Last 1 | Out-Null

    if ($OutDir -eq $SourcePath) { Move-Item -Force $tmp $in }
    $final = if ($OutDir -eq $SourcePath) { $in } else { $out }
    if (-not (Test-Path $final)) { Write-Warning "  no output produced for $($_.Name)"; return }
    $after = (Get-Item $final).Length
    $results += [pscustomobject]@{
        File   = $_.Name
        Before = [math]::Round($before / 1MB, 2)
        After  = [math]::Round($after / 1MB, 2)
        Ratio  = [math]::Round($before / [math]::Max($after, 1), 1)
    }
}

Write-Host ""
$results | Format-Table -AutoSize
$tb = ($results | Measure-Object -Property Before -Sum).Sum
$ta = ($results | Measure-Object -Property After -Sum).Sum
Write-Host ("TOTAL: {0:N1} MB -> {1:N1} MB  ({2:N1}x smaller)" -f $tb, $ta, ($tb / [math]::Max($ta, 0.01))) -ForegroundColor Green