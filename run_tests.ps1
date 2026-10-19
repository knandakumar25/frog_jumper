<#
.SYNOPSIS
    Runs all unit test suites for Frog Jumper / Axiom Force.
    Supports Django (DRF), NUnit (.NET 10 / C#), and GdUnit4 (Godot 4).

.PARAMETER Suite
    Which test suite to execute: 'all', 'django', 'nunit', or 'gdunit'. Default is 'all'.
#>
param(
    [ValidateSet("all", "django", "nunit", "gdunit")]
    [string]$Suite = "all"
)

$ErrorActionPreference = "Stop"
$ProjectRoot = $PSScriptRoot

Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "   Frog Jumper / Axiom Force Test Runner Suite       " -ForegroundColor Cyan
Write-Host "======================================================" -ForegroundColor Cyan

$Results = @{}

# 1. Django Unit Tests
if ($Suite -eq "all" -or $Suite -eq "django") {
    Write-Host "`n[1/3] Running Django (Python) Unit Tests..." -ForegroundColor Yellow
    $PythonExe = Join-Path $ProjectRoot "backend\.venv\Scripts\python.exe"
    $ManagePy = Join-Path $ProjectRoot "backend\manage.py"

    if (Test-Path $PythonExe) {
        & $PythonExe $ManagePy test accounts
        if ($LASTEXITCODE -eq 0) {
            $Results["Django"] = "PASSED"
        } else {
            $Results["Django"] = "FAILED"
        }
    } else {
        Write-Warning "Python venv not found at $PythonExe. Skipping Django tests."
        $Results["Django"] = "SKIPPED"
    }
}

# 2. NUnit Unit Tests
if ($Suite -eq "all" -or $Suite -eq "nunit") {
    Write-Host "`n[2/3] Running NUnit (.NET 10 C#) Unit Tests..." -ForegroundColor Yellow
    $TestCsproj = Join-Path $ProjectRoot "tests\AxiomForce.Tests\AxiomForce.Tests.csproj"

    & dotnet test $TestCsproj --verbosity normal
    if ($LASTEXITCODE -eq 0) {
        $Results["NUnit"] = "PASSED"
    } else {
        $Results["NUnit"] = "FAILED"
    }
}

# 3. GdUnit4 Unit Tests
if ($Suite -eq "all" -or $Suite -eq "gdunit") {
    $CandidateGodotPaths = @(
        "C:\Users\karthiknk21\Godot\Editors\4.7.2-stable-mono\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe",
        "C:\Users\karthiknk21\Godot\Editors\4.7.2-stable-mono\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe",
        "C:\Users\karthiknk21\Godot\Editors\.editor_config\Axiom Force\Godot_v4.7.2-stable_mono_win64.exe",
        "C:\Users\karthiknk21\Desktop\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe"
    )
    $GodotExe = $CandidateGodotPaths | Where-Object { Test-Path $_ } | Select-Object -First 1

    if ($GodotExe -and (Test-Path $GodotExe)) {
        $proc = Start-Process -FilePath $GodotExe -ArgumentList "--headless --path `"$ProjectRoot`" -s addons/gdUnit4/bin/GdUnitCmdTool.gd --add res://tests/ --ignoreHeadlessMode" -Wait -PassThru -NoNewWindow
        if ($proc.ExitCode -eq 0) {
            $Results["GdUnit4"] = "PASSED"
        } else {
            $Results["GdUnit4"] = "FAILED"
        }
    } else {
        Write-Warning "Godot binary not found at $GodotExe. Skipping GdUnit4 tests."
        $Results["GdUnit4"] = "SKIPPED"
    }
}

Write-Host "`n======================================================" -ForegroundColor Cyan
Write-Host "                 Test Suite Summary                   " -ForegroundColor Cyan
Write-Host "======================================================" -ForegroundColor Cyan
foreach ($key in $Results.Keys) {
    $status = $Results[$key]
    $color = if ($status -eq "PASSED") { "Green" } elseif ($status -eq "SKIPPED") { "Yellow" } else { "Red" }
    Write-Host ("{0,-15} : {1}" -f $key, $status) -ForegroundColor $color
}
Write-Host "======================================================`n" -ForegroundColor Cyan
