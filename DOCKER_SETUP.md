# Docker Setup for Axiom Force: Reclaim Earth

This document describes the Docker setup for building, exporting, and running the Godot 4.6.3 C# project.

## Dockerfile Overview

The `Dockerfile` uses a **multi-stage build** with three stages:

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
  - Exports game for Linux/X11 using `export_presets.cfg`

### 3. Runtime Stage (`runtime`)
- **Base**: `ubuntu:22.04` (minimal)
- **Installs**: Only runtime dependencies for Godot exported game
- **Actions**: Copies exported game binary from exporter

## Files Created

| File | Purpose |
|------|---------|
| `Dockerfile` | Multi-stage build definition |
| `docker-compose.yml` | Service definitions for build/run/dev |
| `.dockerignore` | Optimized build context exclusions |
| `export_presets.cfg` | Godot Linux/X11 export configuration |
| `.github/workflows/docker-build.yml` | CI/CD pipeline for GitHub Actions |

## Quick Start

### Build and Export
```bash
# Build the exporter stage and export the game
docker compose build build
docker compose run build
```

### Run Exported Game (Headless)
```bash
# Run the exported game in headless mode (for testing/CI)
docker compose run run ./build/linux/AxiomForceReclaimEarth.x86_64 --headless --quit
```

### Development Environment
```bash
# Start a development container with source mounted
docker compose run dev

# Inside container, run Godot editor:
godot --editor

# Or run headless commands:
godot --headless --export-release "Linux/X11" /app/build/linux/AxiomForceReclaimEarth.x86_64
```

## Docker Compose Services

| Service | Purpose | Target Stage |
|---------|---------|--------------|
| `build` | Build and export game | `exporter` |
| `run` | Run exported game | `runtime` |
| `dev` | Development environment | `builder` |

## CI/CD Pipeline (`.github/workflows/docker-build.yml`)

**Triggers**: Push to `main`/`develop`, PRs to `main`/`develop`, manual dispatch

**Jobs**:
1. **build-and-export**: Builds builder & exporter stages, exports game, uploads artifact, runs headless test
2. **publish** (optional): On push to `main`, publishes `runtime` image to GHCR

## Requirements

- Docker Desktop / Docker Engine 20.10+
- Docker Compose v2+
- For `dev` service: X11 display (Linux) or X11 forwarding (WSL/macOS/Windows)

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `DISPLAY` | `:0` | X11 display for GUI apps (dev service) |
| `GODOT_VERSION` | `4.6.3` | Godot version (in Dockerfile) |

## Customization

### Change Godot Version
Edit `GODOT_VERSION` in `Dockerfile` (both builder and exporter stages)

### Add Export Platforms
1. Add new preset to `export_presets.cfg`
2. Update exporter stage `RUN` command to export additional platforms

### Java Version
Currently uses `openjdk-21-jdk-headless`. Change in both `apt-get install` lines in Dockerfile.

## Troubleshooting

### Godot Export Fails
- Ensure export templates match Godot version exactly (4.6.3.stable)
- Check `export_presets.cfg` name matches `--export-release` argument ("Linux/X11")

### Headless Run Fails
- Export must complete successfully first
- Binary must be executable: `chmod +x build/linux/AxiomForceReclaimEarth.x86_64`

### Large Image Size
- Use `docker system prune` to clean unused layers
- Consider `docker-slim` for production runtime images

## Verification Steps

1. **Build verification**:
   ```bash
   docker compose build builder
   ```

2. **Export verification**:
   ```bash
   docker compose run build
   ls -la build/linux/
   ```

3. **Runtime verification**:
   ```bash
   docker compose build runtime
   docker compose run run ./build/linux/AxiomForceReclaimEarth.x86_64 --headless --quit
   ```

4. **CI verification**: Push to GitHub and check Actions tab