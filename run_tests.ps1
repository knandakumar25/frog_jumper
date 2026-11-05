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

function Invoke-DjangoWithRootPosixVenv {
    <#
    .SYNOPSIS
        Runs the root Linux virtualenv in a temporary unmounted backend-image container.
    .DESCRIPTION
        Windows cannot execute the repository's POSIX `.venv/bin/python` directly. This packages
        regular venv/backend files, copies them into an owned helper container without the broken
        host bind mount, recreates Python links, runs accounts tests, and removes that helper.
    #>
    $script:RootVenvDjangoExitCode = 1
    $DockerExe = (Get-Command docker.exe -ErrorAction SilentlyContinue).Source
    if (-not $DockerExe) {
        $DockerCandidate = Join-Path $env:LOCALAPPDATA "Programs\DockerDesktop\resources\bin\docker.exe"
        if (Test-Path $DockerCandidate) { $DockerExe = $DockerCandidate }
    }
    if (-not $DockerExe) {
        Write-Warning "Docker CLI not found; cannot run the root POSIX .venv from Windows."
        return
    }

    $BackendId = (& $DockerExe compose --project-directory $ProjectRoot ps -q backend).Trim()
    if ($LASTEXITCODE -ne 0 -or -not $BackendId) {
        Write-Warning "The Compose backend must be running to execute the root POSIX .venv."
        return
    }

    $NetworksJson = & $DockerExe inspect --format '{{json .NetworkSettings.Networks}}' $BackendId
    $BackendEnvJson = & $DockerExe inspect --format '{{json .Config.Env}}' $BackendId
    if ($LASTEXITCODE -ne 0) { Write-Warning "Could not inspect backend test connection settings."; return }
    $Networks = $NetworksJson | ConvertFrom-Json
    $NetworkName = @($Networks.PSObject.Properties.Name | Select-Object -First 1)[0]
    if (-not $NetworkName) { Write-Warning "Could not find the Compose network for the backend."; return }
    $BackendEnv = @{}
    foreach ($Entry in ($BackendEnvJson | ConvertFrom-Json)) {
        $Parts = $Entry -split "=", 2
        if ($Parts.Count -eq 2 -and $Parts[0] -in @("DB_HOST", "DB_PORT", "DB_NAME", "DB_USER", "DB_PASSWORD")) {
            $BackendEnv[$Parts[0]] = $Parts[1]
        }
    }

    $HostPythonCandidates = @(
        (Get-Command python.exe -ErrorAction SilentlyContinue).Source,
        (Join-Path $env:LOCALAPPDATA "Programs\Python\Python314\python.exe")
    ) | Where-Object { $_ -and (Test-Path $_) }
    $HostPython = $HostPythonCandidates | Select-Object -First 1
    if (-not $HostPython) {
        Write-Warning "Host Python is needed to package the root venv for Docker."
        return
    }

    $Bundle = Join-Path $ProjectRoot ".validation\django-root-venv-$([Guid]::NewGuid().ToString('N')).tar.gz"
    $HelperName = "axiom-force-django-test-$([Guid]::NewGuid().ToString('N'))"
    $ContainerTemp = "/tmp/axiom-force-django-$([Guid]::NewGuid().ToString('N'))"
    $Packager = Join-Path $ProjectRoot "tools\testing\package_django_test_context.py"
    try {
        & $HostPython $Packager --root $ProjectRoot --output $Bundle
        if ($LASTEXITCODE -ne 0) { Write-Warning "Could not package the root .venv and backend source."; return }

        $RunArgs = @("run", "-d", "--name", $HelperName, "--network", $NetworkName, "--entrypoint", "tail")
        foreach ($Name in @("DB_HOST", "DB_PORT", "DB_NAME", "DB_USER", "DB_PASSWORD")) {
            if ($BackendEnv.ContainsKey($Name)) { $RunArgs += @("--env", "$Name=$($BackendEnv[$Name])") }
        }
        $RunArgs += @("axiom-force-backend", "-f", "/dev/null")
        & $DockerExe @RunArgs | Out-Null
        if ($LASTEXITCODE -ne 0) { Write-Warning "Could not start the isolated Django test container."; return }

        & $DockerExe exec $HelperName mkdir -p $ContainerTemp
        if ($LASTEXITCODE -ne 0) { Write-Warning "Could not create the isolated Django test directory."; return }
        & $DockerExe cp $Bundle "${HelperName}:${ContainerTemp}/context.tar.gz"
        if ($LASTEXITCODE -ne 0) { Write-Warning "Could not copy the root .venv test bundle into the isolated container."; return }

        $PrepareCode = "import pathlib,sys,tarfile;root=pathlib.Path(sys.argv[1]);tarfile.open(root/'context.tar.gz','r:gz').extractall(root,filter='data');v=root/'.venv/bin';[(v/name).symlink_to('/usr/local/bin/python3.14') for name in ('python','python3','python3.14') if not (v/name).exists()]"
        & $DockerExe exec $HelperName python -c $PrepareCode $ContainerTemp
        if ($LASTEXITCODE -ne 0) { Write-Warning "Could not prepare the root virtualenv inside Docker."; return }

        # The root venv may not include this backend's dependencies; install its declared requirements into the disposable copy.
        & $DockerExe exec -w "$ContainerTemp/backend" $HelperName "$ContainerTemp/.venv/bin/python" -m pip install -r requirements.txt
        if ($LASTEXITCODE -ne 0) { Write-Warning "Could not install backend requirements into the disposable root-venv copy."; return }

        & $DockerExe exec -w "$ContainerTemp/backend" $HelperName "$ContainerTemp/.venv/bin/python" manage.py test accounts
        $script:RootVenvDjangoExitCode = $LASTEXITCODE
    }
    finally {
        & $DockerExe rm -f $HelperName 2>$null | Out-Null
        if (Test-Path $Bundle) { Remove-Item -LiteralPath $Bundle -Force }
    }
}

# 1. Django Unit Tests
if ($Suite -eq "all" -or $Suite -eq "django") {
    Write-Host "`n[1/3] Running Django (Python) Unit Tests..." -ForegroundColor Yellow
    $PosixPython = Join-Path $ProjectRoot ".venv\bin\python"
    if (Test-Path $PosixPython) {
        # Use only the root Linux virtualenv, executing it inside an unmounted Linux helper container.
        Invoke-DjangoWithRootPosixVenv
        if ($RootVenvDjangoExitCode -eq 0) {
            $Results["Django"] = "PASSED"
        } else {
            $Results["Django"] = "FAILED"
        }
    } else {
        Write-Warning "Root Linux Python venv not found at $PosixPython. Skipping Django tests."
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
