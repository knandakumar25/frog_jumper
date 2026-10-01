<#
.SYNOPSIS
    Starts the Axiom Force stack using native Windows 11 26H2 WSL Containers (container.exe).
.DESCRIPTION
    Launches:
      - frog_jumper_db (PostgreSQL 16)
      - frog_jumper_backend (Django REST Framework on Python 3.14)
      - frog_jumper_website (React + Vite served via Nginx)
#>

$ErrorActionPreference = "Stop"
$ContainerCli = "C:\Program Files\WSL\container.exe"

if (-not (Test-Path $ContainerCli)) {
    Write-Error "WSL container CLI not found at '$ContainerCli'. Ensure Windows 11 26H2 WSL Containers are enabled."
}

$RepoRoot = $PSScriptRoot

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
        -e POSTGRES_DB=accounts_db `
        -e POSTGRES_USER=postgres `
        -e POSTGRES_PASSWORD=postgres `
        -e PGDATA=/var/lib/postgresql/data `
        -v "frog_jumper_postgres_data:/var/lib/postgresql/data" `
        -v "$RepoRoot\scripts\db\init-db.sql:/docker-entrypoint-initdb.d/init-db.sql" `
        @portArgs `
        postgres:18-alpine | Out-Null
}

# 5. Start backend container
$running = & $ContainerCli ps -a
if ($running -match "frog_jumper_backend") {
    Write-Host "[4/5] Starting existing 'frog_jumper_backend'..." -ForegroundColor Yellow
    & $ContainerCli start frog_jumper_backend | Out-Null
} else {
    Write-Host "[4/5] Running 'frog_jumper_backend' (Python 3.14)..." -ForegroundColor Yellow
    & $ContainerCli run -d `
        --name frog_jumper_backend `
        --network frog_network `
        --network-alias backend `
        --pull never `
        -e DB_HOST=db `
        -e DB_PORT=5432 `
        -e DB_NAME=accounts_db `
        -e DB_USER=postgres `
        -e DB_PASSWORD=postgres `
        -e SECRET_KEY=django-insecure-change-this-in-production `
        -e DEBUG=True `
        -e ALLOWED_HOSTS="*" `
        -e FRONTEND_URL=http://localhost:5173 `
        -e EPIC_CLIENT_ID=xyza7891g76bMoT7LRbXiajbp1ZynDUf `
        -e EPIC_CLIENT_SECRET=DvsZrhk6V1YxjWFgBZ8c1EJZqigg2kyF0kxL+9Xk2RA `
        -e DISCORD_CLIENT_ID=1519971724869894197 `
        -e DISCORD_CLIENT_SECRET=m0Zs7KUvEW49tqisij-cQs4caSY9wEC0 `
        -e STEAM_API_KEY="" `
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
