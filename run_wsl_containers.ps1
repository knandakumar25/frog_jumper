<#
.SYNOPSIS
    Starts the Axiom Force stack using native Windows 11 26H2 WSL Containers (container.exe).
.DESCRIPTION
    Launches:
      - frog_jumper_db (PostgreSQL 16)
      - frog_jumper_backend (Django REST Framework on Python 3.14)
      - frog_jumper_website (React + Vite served via Nginx)

    Configuration comes from a gitignored '.env' in the repo root, if present;
    see '.env.example' for the recognised names. A variable already set in the
    calling PowerShell session wins over the file, so a one-off override works
    without editing .env:

        $env:RESEND_API_KEY = "re_..."; .\run_wsl_containers.ps1

    Values are only applied when a container is CREATED. To pick up a change,
    run .\stop_wsl_containers.ps1 first -- `container start` reuses the existing
    environment and silently ignores -e.

    If a '.local-ca' directory (gitignored, machine-local) holds system-ca.pem
    and norton-root.pem, they are concatenated and mounted into the backend as
    a CA bundle, with SSL_CERT_FILE pointing at it. This exists because
    antivirus TLS interception on this machine re-signs outbound HTTPS with a
    root the image does not trust; see the comment on $caArgs below.
#>

$ErrorActionPreference = "Stop"
$ContainerCli = "C:\Program Files\WSL\container.exe"

if (-not (Test-Path $ContainerCli)) {
    Write-Error "WSL container CLI not found at '$ContainerCli'. Ensure Windows 11 26H2 WSL Containers are enabled."
}

$RepoRoot = $PSScriptRoot

# Load a gitignored .env, if one exists.
#
# Nothing else in the stack reads a .env file -- the containers receive their
# configuration as environment variables (backend/entrypoint.sh and
# core/settings.py both read os.environ), so this script is the single place
# that turns a file into -e flags. See .env.example for the list of names.
#
# A variable already set in the calling session wins over the file, so a
# one-off `$env:RESEND_API_KEY = "..."` still overrides without editing .env.
$envFile = Join-Path $RepoRoot ".env"
if (Test-Path -LiteralPath $envFile) {
    foreach ($line in (Get-Content -LiteralPath $envFile)) {
        $entry = $line.Trim()
        if (-not $entry -or $entry.StartsWith("#")) { continue }
        $splitAt = $entry.IndexOf("=")
        if ($splitAt -lt 1) { continue }

        $name = $entry.Substring(0, $splitAt).Trim()
        $value = $entry.Substring($splitAt + 1).Trim()

        # Strip one layer of surrounding quotes, the way .env files are
        # conventionally written (`KEY="value with spaces"`).
        if ($value.Length -ge 2 -and (
                ($value.StartsWith('"') -and $value.EndsWith('"')) -or
                ($value.StartsWith("'") -and $value.EndsWith("'")))) {
            $value = $value.Substring(1, $value.Length - 2)
        }

        if ([string]::IsNullOrEmpty([Environment]::GetEnvironmentVariable($name, "Process"))) {
            [Environment]::SetEnvironmentVariable($name, $value, "Process")
        }
    }
    Write-Host "Loaded environment overrides from $envFile" -ForegroundColor Green
}

# Falls back to the value this script used to hardcode, so behaviour is
# unchanged when there is no .env and no session variable.
function Get-EnvOrDefault {
    param([string]$Name, [string]$Default)
    $value = [Environment]::GetEnvironmentVariable($Name, "Process")
    if ([string]::IsNullOrEmpty($value)) { return $Default }
    return $value
}

Write-Host "=== Starting Axiom Force Stack (WSL Containers) ===" -ForegroundColor Cyan

# 1. Ensure custom bridge network exists
$networks = (& $ContainerCli network list 2>$null) -join "`n"
if ($networks -notmatch "frog_network") {
    Write-Host "[1/5] Creating network 'frog_network'..." -ForegroundColor Yellow
    & $ContainerCli network create frog_network 2>$null | Out-Null
} else {
    Write-Host "[1/5] Network 'frog_network' ready." -ForegroundColor Green
}

# 2. Ensure persistent database volume exists
$volumes = (& $ContainerCli volume list 2>$null) -join "`n"
if ($volumes -notmatch "frog_jumper_postgres_data") {
    Write-Host "[2/5] Creating volume 'frog_jumper_postgres_data'..." -ForegroundColor Yellow
    & $ContainerCli volume create frog_jumper_postgres_data 2>$null | Out-Null
} else {
    Write-Host "[2/5] Volume 'frog_jumper_postgres_data' ready." -ForegroundColor Green
}

# 3. Verify host port 5432 availability
$port5432Occupied = $false
try {
    $conn = New-Object System.Net.Sockets.TcpClient
    $iar = $conn.BeginConnect("127.0.0.1", 5432, $null, $null)
    if ($iar.AsyncWaitHandle.WaitOne(300)) {
        $conn.EndConnect($iar)
        $port5432Occupied = $true
        $conn.Close()
    }
} catch {
    $port5432Occupied = $false
}

if ($port5432Occupied) {
    Write-Host "  [WARNING] Port 5432 is currently in use by the host Windows service ('postgresql-x64-18')." -ForegroundColor Red
    Write-Host "  To allow the container to bind to port 5432 exactly, stop the Windows service:" -ForegroundColor Yellow
    Write-Host "    Stop-Service postgresql-x64-18  (Run in Administrator PowerShell)" -ForegroundColor White
}

# 4. Start database container
$running = & $ContainerCli ps -a
if ($running -match "frog_jumper_db") {
    Write-Host "[3/5] Starting existing 'frog_jumper_db'..." -ForegroundColor Yellow
    & $ContainerCli start frog_jumper_db | Out-Null
} else {
    $portArgs = if ($port5432Occupied) {
        Write-Host "  (Host port 5432 occupied by Windows PostgreSQL service; running DB container internally on frog_network)" -ForegroundColor Yellow
        @()
    } else {
        Write-Host "[3/5] Running 'frog_jumper_db' on port 5432..." -ForegroundColor Yellow
        @("-p", "5432:5432")
    }
    & $ContainerCli run -d `
        --name frog_jumper_db `
        --network frog_network `
        --network-alias db `
        --pull never `
        -e "POSTGRES_DB=$(Get-EnvOrDefault 'POSTGRES_DB' 'accounts_db')" `
        -e "POSTGRES_USER=$(Get-EnvOrDefault 'POSTGRES_USER' 'postgres')" `
        -e "POSTGRES_PASSWORD=$(Get-EnvOrDefault 'POSTGRES_PASSWORD' 'postgres')" `
        -e "PGDATA=/var/lib/postgresql/data" `
        -v "frog_jumper_postgres_data:/var/lib/postgresql/data" `
        -v "$RepoRoot\scripts\db\init-db.sql:/docker-entrypoint-initdb.d/init-db.sql" `
        @portArgs `
        postgres:18-alpine | Out-Null
}

# 5. Start backend container
#
# Optional Resend mail transport, sourced from .env or the session -- never
# hardcoded, so a key never lands in a tracked file. Passing nothing is the
# supported default: settings.py falls back to the console backend and prints
# mail to the container log instead of sending it.
#
# Built conditionally rather than always passed -- settings.py reads these with
# os.environ.get(KEY, default), and an empty-but-present value overrides the
# default (an empty DEFAULT_FROM_EMAIL, in particular, is not something Django
# can send from).
$mailArgs = @()
if ($env:RESEND_API_KEY) {
    $mailArgs += @("-e", "RESEND_API_KEY=$env:RESEND_API_KEY")
}
if ($env:DEFAULT_FROM_EMAIL) {
    $mailArgs += @("-e", "DEFAULT_FROM_EMAIL=$env:DEFAULT_FROM_EMAIL")
}
if ($env:RESEND_TIMEOUT) {
    $mailArgs += @("-e", "RESEND_TIMEOUT=$env:RESEND_TIMEOUT")
}

# Optional Clerk identity provider, sourced from .env or the session.
#
# Without CLERK_SECRET_KEY the onboarding endpoints are inert -- /start/ answers
# 503 rather than sending anything -- so omitting these is a supported state,
# not a broken one. Built the same conditional way as $mailArgs: settings.py
# reads each with os.environ.get(KEY, default), and an empty-but-present
# CLERK_TIMEOUT would raise ValueError on float() and take the whole boot with
# it.
$clerkArgs = @()
if ($env:CLERK_SECRET_KEY) {
    $clerkArgs += @("-e", "CLERK_SECRET_KEY=$env:CLERK_SECRET_KEY")
}
if ($env:CLERK_TIMEOUT) {
    $clerkArgs += @("-e", "CLERK_TIMEOUT=$env:CLERK_TIMEOUT")
}
foreach ($name in @(
        "ONBOARDING_TOKEN_TTL_HOURS",
        "ONBOARDING_RESEND_COOLDOWN",
        "ONBOARDING_MAX_SENDS",
        "ONBOARDING_IP_HOURLY_LIMIT",
        "ONBOARDING_GLOBAL_HOURLY_LIMIT")) {
    $value = [Environment]::GetEnvironmentVariable($name, "Process")
    if (-not [string]::IsNullOrEmpty($value)) {
        $clerkArgs += @("-e", "$name=$value")
    }
}

# Optional local CA trust.
#
# This machine's antivirus intercepts TLS and re-signs every connection with
# its own root CA, so anything running inside a container fails certificate
# verification -- which is also why the Containerfile passes --trusted-host to
# pip. If that root has been staged in .local-ca (gitignored), it is appended
# to the image's system bundle and pointed at with SSL_CERT_FILE. Only the
# container trusts it; the host's own trust store is untouched.
#
# Everything is skipped when .local-ca is absent, so a machine without the
# interception behaves exactly as before.
$LocalCaDir = Join-Path $RepoRoot ".local-ca"
$caSources = @(
    (Join-Path $LocalCaDir "system-ca.pem"),
    (Join-Path $LocalCaDir "norton-root.pem")
)
$caBundle = Join-Path $LocalCaDir "combined-ca.pem"
$caPathInContainer = "/etc/ssl/certs/local-ca-bundle.pem"

$caArgs = @()
$haveAllSources = ($caSources | Where-Object { Test-Path -LiteralPath $_ }).Count -eq $caSources.Count
if ($haveAllSources) {
    # Rebuilt on every run: it is only a byte concatenation, and that keeps the
    # bundle in step if either source is refreshed.
    $stream = New-Object System.IO.MemoryStream
    foreach ($src in $caSources) {
        $bytes = [System.IO.File]::ReadAllBytes($src)
        $stream.Write($bytes, 0, $bytes.Length)
    }
    [System.IO.File]::WriteAllBytes($caBundle, $stream.ToArray())
    $stream.Dispose()

    $caArgs = @(
        "-v", "${caBundle}:${caPathInContainer}:ro",
        "-e", "SSL_CERT_FILE=$caPathInContainer"
    )
    Write-Host "Trusting local CA bundle inside the backend container" -ForegroundColor Green
}

$running = & $ContainerCli ps -a
if ($running -match "frog_jumper_backend") {
    Write-Host "[4/5] Starting existing 'frog_jumper_backend'..." -ForegroundColor Yellow
    # `container start` cannot change a container's environment or mounts: -e
    # and -v are only read at creation. Warning here because the failure mode is
    # otherwise silent -- the new key or CA would simply appear to be ignored.
    if ($mailArgs.Count -gt 0 -or $clerkArgs.Count -gt 0 -or $caArgs.Count -gt 0) {
        Write-Warning "Environment/CA changes are configured, but 'container start' ignores -e and -v. Run .\stop_wsl_containers.ps1 first to recreate the backend container with them."
    }
    & $ContainerCli start frog_jumper_backend | Out-Null
} else {
    Write-Host "[4/5] Running 'frog_jumper_backend' (Python 3.14)..." -ForegroundColor Yellow
    & $ContainerCli run -d `
        --name frog_jumper_backend `
        --network frog_network `
        --network-alias backend `
        --pull never `
        -e "DB_HOST=$(Get-EnvOrDefault 'DB_HOST' 'db')" `
        -e "DB_PORT=$(Get-EnvOrDefault 'DB_PORT' '5432')" `
        -e "DB_NAME=$(Get-EnvOrDefault 'DB_NAME' 'accounts_db')" `
        -e "DB_USER=$(Get-EnvOrDefault 'DB_USER' 'postgres')" `
        -e "DB_PASSWORD=$(Get-EnvOrDefault 'DB_PASSWORD' 'postgres')" `
        -e "SECRET_KEY=$(Get-EnvOrDefault 'SECRET_KEY' 'django-insecure-change-this-in-production')" `
        -e "DEBUG=$(Get-EnvOrDefault 'DEBUG' 'True')" `
        -e "ALLOWED_HOSTS=$(Get-EnvOrDefault 'ALLOWED_HOSTS' '*')" `
        -e "FRONTEND_URL=$(Get-EnvOrDefault 'FRONTEND_URL' 'http://localhost:5173')" `
        -e "EPIC_CLIENT_ID=$(Get-EnvOrDefault 'EPIC_CLIENT_ID' 'xyza7891g76bMoT7LRbXiajbp1ZynDUf')" `
        -e "EPIC_CLIENT_SECRET=$(Get-EnvOrDefault 'EPIC_CLIENT_SECRET' 'DvsZrhk6V1YxjWFgBZ8c1EJZqigg2kyF0kxL+9Xk2RA')" `
        -e "DISCORD_CLIENT_ID=$(Get-EnvOrDefault 'DISCORD_CLIENT_ID' '1519971724869894197')" `
        -e "DISCORD_CLIENT_SECRET=$(Get-EnvOrDefault 'DISCORD_CLIENT_SECRET' 'm0Zs7KUvEW49tqisij-cQs4caSY9wEC0')" `
        -e "STEAM_API_KEY=$(Get-EnvOrDefault 'STEAM_API_KEY' '')" `
        @mailArgs `
        @clerkArgs `
        @caArgs `
        -v "$RepoRoot\backend:/app" `
        -p 8000:8000 `
        frog_jumper_backend | Out-Null
}

# 6. Start website container
$running = & $ContainerCli ps -a
if ($running -match "frog_jumper_website") {
    Write-Host "[5/5] Starting existing 'frog_jumper_website'..." -ForegroundColor Yellow
    & $ContainerCli start frog_jumper_website | Out-Null
} else {
    Write-Host "[5/5] Running 'frog_jumper_website'..." -ForegroundColor Yellow
    & $ContainerCli run -d `
        --name frog_jumper_website `
        --network frog_network `
        --network-alias website `
        --pull never `
        -p 5173:80 `
        frog_jumper_website | Out-Null
}

Write-Host "`n=== Stack Status ===" -ForegroundColor Green
& $ContainerCli ps

Write-Host "`nService Endpoints:" -ForegroundColor Cyan
Write-Host "  - Website (Frontend): http://localhost:5173" -ForegroundColor White
Write-Host "  - Backend API:        http://localhost:8000/api/" -ForegroundColor White
Write-Host "  - PostgreSQL:         localhost:5432 (accounts_db)" -ForegroundColor White
Write-Host "`nAll services running natively via WSL Containers. Docker Desktop is not required." -ForegroundColor Green
