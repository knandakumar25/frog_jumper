# Dockerfile for Godot 4.6.3 C# Project
# Multi-stage build for building and exporting the game

# Build stage - uses .NET 10 SDK since project targets net10.0
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder

# Install dependencies for Godot and Java
RUN apt-get update && apt-get install -y \
    wget \
    unzip \
    openjdk-21-jdk-headless \
    libglib2.0-0 \
    libx11-6 \
    libxcursor1 \
    libxi6 \
    libxrandr2 \
    libxrender1 \
    libxxf86vm1 \
    libgl1-mesa-glx \
    && rm -rf /var/lib/apt/lists/*

# Verify Java installation
RUN java --version

# Install Godot 4.6.3
ENV GODOT_VERSION=4.6.3
RUN wget -q https://github.com/godotengine/godot-builds/releases/download/${GODOT_VERSION}-stable/Godot_v${GODOT_VERSION}-stable_linux.x86_64.zip \
    && unzip -q Godot_v${GODOT_VERSION}-stable_linux.x86_64.zip \
    && mv Godot_v${GODOT_VERSION}-stable_linux.x86_64 /usr/local/bin/godot \
    && chmod +x /usr/local/bin/godot \
    && rm Godot_v${GODOT_VERSION}-stable_linux.x86_64.zip

# Verify Godot installation
RUN godot --version

WORKDIR /app

# Copy project files
COPY *.csproj ./
COPY *.sln ./

# Copy the rest of the project
COPY . .

# Restore NuGet packages
RUN dotnet restore "2d Platformer game.csproj"

# Build the project
RUN dotnet build "2d Platformer game.csproj" -c Release --no-restore

# Export stage - export the game for Linux
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS exporter

# Install Godot for export
ENV GODOT_VERSION=4.6.3
RUN apt-get update && apt-get install -y \
    wget \
    unzip \
    openjdk-21-jdk-headless \
    libglib2.0-0 \
    libx11-6 \
    libxcursor1 \
    libxi6 \
    libxrandr2 \
    libxrender1 \
    libxxf86vm1 \
    libgl1-mesa-glx \
    && rm -rf /var/lib/apt/lists/*

# Verify Java installation
RUN java --version

RUN wget -q https://github.com/godotengine/godot-builds/releases/download/${GODOT_VERSION}-stable/Godot_v${GODOT_VERSION}-stable_linux.x86_64.zip \
    && unzip -q Godot_v${GODOT_VERSION}-stable_linux.x86_64.zip \
    && mv Godot_v${GODOT_VERSION}-stable_linux.x86_64 /usr/local/bin/godot \
    && chmod +x /usr/local/bin/godot \
    && rm Godot_v${GODOT_VERSION}-stable_linux.x86_64.zip

# Install export templates
RUN wget -q https://github.com/godotengine/godot-builds/releases/download/${GODOT_VERSION}-stable/Godot_v${GODOT_VERSION}-stable_export_templates.tpz \
    && mkdir -p /root/.local/share/godot/export_templates/${GODOT_VERSION}.stable \
    && unzip -q Godot_v${GODOT_VERSION}-stable_export_templates.tpz -d /root/.local/share/godot/export_templates/${GODOT_VERSION}.stable \
    && rm Godot_v${GODOT_VERSION}-stable_export_templates.tpz

WORKDIR /app

# Copy built project from builder
COPY --from=builder /app ./

# Export the game for Linux (headless client)
RUN mkdir -p /app/build/linux \
    && godot --headless --export-release "Linux/X11" /app/build/linux/AxiomForceReclaimEarth.x86_64

# Export the dedicated server (headless)
RUN mkdir -p /app/build/linux_server \
    && godot --headless --export-release "Linux Server" /app/build/linux_server/AxiomForceReclaimEarth_Server.x86_64

# Export the Web build (HTML5)
RUN mkdir -p /app/build/web \
    && godot --headless --export-release "Web" /app/build/web/index.html

# Final stage - minimal runtime for running the exported game client
FROM ubuntu:22.04 AS runtime-client

# Install minimal dependencies to run Godot exported game
RUN apt-get update && apt-get install -y \
    libglib2.0-0 \
    libx11-6 \
    libxcursor1 \
    libxi6 \
    libxrandr2 \
    libxrender1 \
    libxxf86vm1 \
    libgl1-mesa-glx \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy exported client game from exporter
COPY --from=exporter /app/build/linux ./build/linux

# Set entrypoint for client
ENTRYPOINT ["./build/linux/AxiomForceReclaimEarth.x86_64"]

# Dedicated server runtime stage
FROM ubuntu:22.04 AS runtime-server

# Install minimal dependencies for headless server
RUN apt-get update && apt-get install -y \
    libglib2.0-0 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy exported server binary from exporter
COPY --from=exporter /app/build/linux_server ./build/linux_server

# Ensure executable
RUN chmod +x ./build/linux_server/AxiomForceReclaimEarth_Server.x86_64

# Set entrypoint for dedicated server
ENTRYPOINT ["./build/linux_server/AxiomForceReclaimEarth_Server.x86_64"]

# Web runtime stage - Nginx serving HTML5 export with COOP/COEP headers
FROM nginx:alpine AS runtime-web

# Copy custom nginx config with COOP/COEP headers for SharedArrayBuffer
COPY docker/nginx/nginx.conf /etc/nginx/nginx.conf

# Copy exported web build from exporter
COPY --from=exporter /app/build/web /usr/share/nginx/html

# Expose port 8080
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

# Default command
CMD ["nginx", "-g", "daemon off;"]

# Install minimal dependencies to run Godot exported game
RUN apt-get update && apt-get install -y \
    libglib2.0-0 \
    libx11-6 \
    libxcursor1 \
    libxi6 \
    libxrandr2 \
    libxrender1 \
    libxxf86vm1 \
    libgl1-mesa-glx \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy exported game from exporter
COPY --from=exporter /app/build/linux ./build/linux

# Set entrypoint
ENTRYPOINT ["./build/linux/AxiomForceReclaimEarth.x86_64"]