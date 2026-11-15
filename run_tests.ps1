<#
.SYNOPSIS
    Runs all unit test suites for Frog Jumper / Axiom Force.
    Supports the accounts API (Express.js, node:test), NUnit (.NET 10 / C#), and GdUnit4 (Godot 4).

.PARAMETER Suite
    Which test suite to execute: 'all', 'backend', 'nunit', or 'gdunit'. Default is 'all'.
#>
param(
    [ValidateSet("all", "backend", "nunit", "gdunit")]
    [string]$Suite = "all"
)

$ErrorActionPreference = "Stop"
$ProjectRoot = $PSScriptRoot

Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "   Frog Jumper / Axiom Force Test Runner Suite       " -ForegroundColor Cyan
Write-Host "======================================================" -ForegroundColor Cyan

$Results = @{}

function Invoke-BackendTestsInContainer {
    <#
    .SYNOPSIS
        Runs the accounts API tests (npm test) inside the running backend container.
    .DESCRIPTION
        Requires the normal Compose stack; the backend image's `dev` target includes the test
        packages. The tests create and drop their own `accounts_test` database on the `db`
        service and never touch `accounts_db`. Does not install packages, copy credentials or
        create disposable containers. Sets $script:ContainerBackendExitCode.
    #>
    $script:ContainerBackendExitCode = 1
    $DockerExe = (Get-Command docker.exe -ErrorAction SilentlyContinue).Source
    if (-not $DockerExe) {
        $DockerCandidate = Join-Path $env:LOCALAPPDATA "Programs\DockerDesktop\resources\bin\docker.exe"
        if (Test-Path $DockerCandidate) { $DockerExe = $DockerCandidate }
    }
    if (-not $DockerExe) {
        Write-Warning "Docker CLI not found. Start the normal Compose backend before running the backend tests."
        return
    }

    & $DockerExe compose --project-directory $ProjectRoot exec -T backend npm test
    $script:ContainerBackendExitCode = $LASTEXITCODE
}

# 1. Accounts API tests (Express.js, node:test) inside the running backend container.
if ($Suite -eq "all" -or $Suite -eq "backend") {
    Write-Host "`n[1/3] Running accounts API (Express.js) tests in Docker..." -ForegroundColor Yellow
    Invoke-BackendTestsInContainer
    $Results["Backend"] = if ($ContainerBackendExitCode -eq 0) { "PASSED" } else { "FAILED" }
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
        "C:\Users\karthiknk21\Desktop\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe",
        "C:\Users\karthiknk21\Desktop\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe",
        "C:\Users\karthiknk21\Godot\Editors\4.7.2-stable-mono\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe",
        "C:\Users\karthiknk21\Godot\Editors\4.7.2-stable-mono\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe",
        "C:\Users\karthiknk21\Godot\Editors\.editor_config\Axiom Force\Godot_v4.7.2-stable_mono_win64.exe"
    )
    $GodotExe = $CandidateGodotPaths | Where-Object { Test-Path $_ } | Select-Object -First 1

    if ($GodotExe -and (Test-Path $GodotExe)) {
        # Invoke the console directly so the child engine's diagnostics and exit status reach this runner.
        & $GodotExe --headless --path $ProjectRoot -s addons/gdUnit4/bin/GdUnitCmdTool.gd --add res://tests/ --ignoreHeadlessMode
        if ($LASTEXITCODE -eq 0) {
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

# A readable summary must also fail automation when any required suite failed or was skipped.
if (@($Results.Values | Where-Object { $_ -ne "PASSED" }).Count -gt 0) { exit 1 }
exit 0
