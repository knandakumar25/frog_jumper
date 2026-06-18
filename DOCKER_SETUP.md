# Docker Setup for Axiom Force: Reclaim Earth

This document describes the Docker setup for building, exporting, and running the Godot 4.6.3 C# project.

## 🎯 Three Major Use Cases (Why Docker for Godot?)

> **Important**: You should develop your game directly on your host machine using the native Godot editor. Docker is for **automation and deployment**, not for daily development.

| Use Case | Description | Docker Target |
|----------|-------------|---------------|
| **1. CI/CD Build Pipelines** | Automated builds on every push, multi-platform exports, artifact uploads | `exporter`, `runtime-client` |
| **2. Dedicated Multiplayer Servers** | Headless server binaries in lightweight containers for cloud hosting | `runtime-server` |
| **3. Local Web Testing (HTML5)** | Nginx container serving Web export with COOP/COEP headers for SharedArrayBuffer | `runtime-web` |

---

## Dockerfile Overview

The `Dockerfile` uses a **multi-stage build** with **five stages**:

### 1. Builder Stage (`builder`)
- **Base**: `mcr.microsoft.com/dotnet/sdk:10.0` (matches project's `net10.0` target)
- **Installs**:
  - Godot 4.6.3 (Linux x86_64)
  - OpenJDK 21 (headless)
  - Required X11/OpenGL libraries
- **Actions**:
  - Restores NuGet packages
  - Builds the C# project in Release configuration

### 2. Exporter Stage (`exporter`)
- **Base**: `mcr.microsoft.com/dotnet/runtime:10.0`
- **Installs**:
  - Godot 4.6.3 (Linux x86_64)
  - Godot Export Templates 4.6.3
  - OpenJDK 21 (headless)
  - Required X11/OpenGL libraries
- **Actions**:
  - Copies built project from builder
  - Exports **three targets** using `export_presets.cfg`:
    - **Linux/X11 Client** → `build/linux/AxiomForceReclaimEarth.x86_64`
    - **Linux Server** (dedicated) → `build/linux_server/AxiomForceReclaimEarth_Server.x86_64`
    - **Web (HTML5)** → `build/web/index.html`

### 3. Client Runtime Stage (`runtime-client`)
- **Base**: `ubuntu:22.04` (minimal)
- **Installs**: Only runtime dependencies for Godot exported game
- **Actions**: Copies exported client binary from exporter

### 4. Server Runtime Stage (`runtime-server`)
- **Base**: `ubuntu:22.04` (minimal, no X11 deps)
- **Installs**: Only `libglib2.0-0` for headless server
- **Actions**: Copies exported server binary from exporter

### 5. Web Runtime Stage (`runtime-web`)
- **Base**: `nginx:alpine` (tiny ~20MB)
- **Config**: Custom `nginx.conf` with **COOP/COEP headers** required for Godot 4 Web exports
- **Actions**: Copies exported web build, serves on port 8080

---

## Files Created

| File | Purpose |
|------|---------|
| `Dockerfile` | 5-stage multi-target build definition |
| `docker-compose.yml` | Service definitions for all use cases |
| `.dockerignore` | Optimized build context exclusions |
| `export_presets.cfg` | Godot export configs (Client, Server, Web) |
| `docker/nginx/nginx.conf` | Nginx config with COOP/COEP headers |
| `.github/workflows/docker-build.yml` | CI/CD pipeline for GitHub Actions |
| `DOCKER_SETUP.md` | This documentation |

---

## Quick Start

### Build All Targets (CI/CD - Use Case 1)
```bash
# Build exporter stage and export all three targets
docker compose build build
docker compose run build

# Verify outputs
ls -la build/
# build/
# ├── linux/
# │   └── AxiomForceReclaimEarth.x86_64
# ├── linux_server/
# │   └── AxiomForceReclaimEarth_Server.x86_64
# └── web/
#     ├── index.html
#     ├── index.js
#     ├── index.wasm
#     ├── index.pck
#     └── ...
```

### Test Client (Headless)
```bash
# Run headless test (exits immediately)
docker compose run test
```

### Run Dedicated Server (Use Case 2)
```bash
# Build server runtime
docker compose build server

# Run server (ports 7777 UDP/TCP exposed)
docker compose up server

# Or run directly with custom args
docker compose run server ./build/linux_server/AxiomForceReclaimEarth_Server.x86_64 --port 7777
```

### Test Web Export Locally (Use Case 3)
```bash
# Build web runtime
docker compose build web

# Start nginx serving web export with COOP/COEP headers
docker compose up web

# Open http://localhost:8080 in browser
# ✅ SharedArrayBuffer works! No CORS errors!
```

### Development Environment
```bash
# Start dev container with source mounted
docker compose run dev

# Inside container:
godot --editor                    # Launch editor (needs X11)
godot --headless --export-release "Linux/X11" /app/build/linux/...
```

---

## Docker Compose Services

| Service | Target Stage | Purpose | Ports |
|---------|--------------|---------|-------|
| `build` | `exporter` | Build & export all targets | - |
| `test` | `runtime-client` | CI headless test | - |
| `server` | `runtime-server` | Dedicated multiplayer server | 7777/tcp, 7777/udp |
| `web` | `runtime-web` | Local HTML5 testing (nginx) | 8080 |
| `dev` | `builder` | Full dev environment | - |
| `client` | `runtime-client` | Production client runtime | - |

---

## CI/CD Pipeline (`.github/workflows/docker-build.yml`)

**Triggers**: Push to `main`/`develop`, PRs to `main`/`develop`, manual dispatch

**Jobs**:
1. **build-and-export**: Builds all stages, exports 3 targets, uploads artifacts, runs headless client test
2. **test-server**: Builds `runtime-server`, verifies server binary starts
3. **test-web**: Builds `runtime-web`, starts nginx, verifies:
   - HTTP 200 response
   - Game content loads
   - **COOP/COEP headers present** for SharedArrayBuffer
4. **publish** (on `main` push): Pushes all 3 runtime images to GHCR

---

## Export Presets (`export_presets.cfg`)

Three presets configured:

| Preset | Platform | Dedicated Server | Output Path |
|--------|----------|------------------|-------------|
| `Linux/X11` | Linux/X11 | `false` | `build/linux/AxiomForceReclaimEarth.x86_64` |
| `Linux Server` | Linux/X11 | `true` | `build/linux_server/AxiomForceReclaimEarth_Server.x86_64` |
| `Web` | Web | `false` | `build/web/index.html` |

**Web preset options**:
- `web/compression=1` (gzip)
- `web/load_file_system=true` (virtual file system)
- `web/gdscript_obfuscation=false`

---

## Nginx Config (`docker/nginx/nginx.conf`)

Critical headers for **Godot 4 Web exports**:

```nginx
# REQUIRED for SharedArrayBuffer (Godot 4 threading)
add_header Cross-Origin-Opener-Policy "same-origin" always;
add_header Cross-Origin-Embedder-Policy "require-corp" always;
```

Without these headers, browsers block `SharedArrayBuffer` and your game fails to load with errors like:
> `SharedArrayBuffer is not defined` or `Cannot use SharedArrayBuffer without COOP/COEP`

---

## Requirements

- Docker Desktop / Docker Engine 20.10+
- Docker Compose v2+
- For `dev` service: X11 display (Linux) or X11 forwarding (WSL/macOS/Windows)

---

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `DISPLAY` | `:0` | X11 display for GUI apps (dev service) |
| `GODOT_VERSION` | `4.6.3` | Godot version (in Dockerfile) |

---

## Customization

### Change Godot Version
Edit `GODOT_VERSION` in `Dockerfile` (both builder and exporter stages)

### Add Export Platforms
1. Add new preset to `export_presets.cfg`
2. Update exporter stage `RUN` commands to export additional platforms

### Java Version
Currently uses `openjdk-21-jdk-headless`. Change in both `apt-get install` lines in Dockerfile.

### Server Port
Edit `docker-compose.yml` server service ports (default 7777)

---

## Troubleshooting

### Godot Export Fails
- Ensure export templates match Godot version exactly (4.6.3.stable)
- Check `export_presets.cfg` name matches `--export-release` argument

### Headless Run Fails
- Export must complete successfully first
- Binary must be executable: `chmod +x build/linux/...`

### Web Export Doesn't Load in Browser
- **Must use nginx container** (or any server with COOP/COEP headers)
- `file://` protocol will NOT work
- Basic `python -m http.server` will NOT work (no COOP/COEP)
- Check browser console for `Cross-Origin-Opener-Policy` errors

### Large Image Size
- Use `docker system prune` to clean unused layers
- Consider `docker-slim` for production runtime images
- `runtime-server` and `runtime-web` are already minimal (~50MB, ~20MB)

---

## Verification Steps

1. **Build verification**:
   ```bash
   docker compose build builder
   ```

2. **Export all targets**:
   ```bash
   docker compose run build
   ls -la build/linux/ build/linux_server/ build/web/
   ```

3. **Client runtime verification**:
   ```bash
   docker compose run test
   ```

4. **Server runtime verification**:
   ```bash
   docker compose build server
   docker compose run server ./build/linux_server/AxiomForceReclaimEarth_Server.x86_64 --help
   ```

5. **Web runtime verification**:
   ```bash
   docker compose up -d web
   curl -I http://localhost:8080/ | grep -i "cross-origin"
   # Should show:
   # cross-origin-opener-policy: same-origin
   # cross-origin-embedder-policy: require-corp
   ```

6. **CI verification**: Push to GitHub and check Actions tab