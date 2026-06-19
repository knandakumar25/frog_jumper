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
    python3 \
    python3-pip \
    && rm -rf /var/lib/apt/lists/*

# Install graphify for analysis
COPY requirements.txt .
RUN pip3 install --no-cache-dir -r requirements.txt

# Verify Java installation
RUN java --version

# Install Godot 4.6.3 MONO (C# Version)
ENV GODOT_VERSION=4.6.3
RUN wget -q https://github.com/godotengine/godot-builds/releases/download/${GODOT_VERSION}-stable/Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64.zip \
    && unzip -q Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64.zip \
    && mv Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64/Godot_v${GODOT_VERSION}-stable_mono_linux.x86_64 /usr/local/bin/godot \
    && mv Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64/GodotSharp /usr/local/bin/GodotSharp \
    && chmod +x /usr/local/bin/godot \
    && rm -rf Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64.zip Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64

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

# Export stage - export the game for Linux, Web, and Android
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS exporter

# Install Godot Mono for Export Execution
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

# Install Godot Mono for Export Execution
RUN wget -q https://github.com/godotengine/godot-builds/releases/download/${GODOT_VERSION}-stable/Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64.zip \
    && unzip -q Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64.zip \
    && mv Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64/Godot_v${GODOT_VERSION}-stable_mono_linux.x86_64 /usr/local/bin/godot \
    && mv Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64/GodotSharp /usr/local/bin/GodotSharp \
    && chmod +x /usr/local/bin/godot \
    && rm -rf Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64.zip Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64

# Install and Correctly Extract Export Templates
RUN wget -q https://github.com/godotengine/godot-builds/releases/download/${GODOT_VERSION}-stable/Godot_v${GODOT_VERSION}-stable_mono_export_templates.tpz \
    && mkdir -p /tmp/templates \
    && unzip -q Godot_v${GODOT_VERSION}-stable_mono_export_templates.tpz -d /tmp/templates \
    && mkdir -p /root/.local/share/godot/export_templates/${GODOT_VERSION}.stable \
    && mv /tmp/templates/templates/* /root/.local/share/godot/export_templates/${GODOT_VERSION}.stable/ \
    && rm -rf Godot_v${GODOT_VERSION}-stable_mono_export_templates.tpz /tmp/templates

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

# ==========================================
# ANDROID EXPORT STAGE
# ==========================================
FROM exporter AS exporter-android

# Android SDK/NDK versions (from config.gradle)
ENV ANDROID_SDK_ROOT=/opt/android-sdk
ENV ANDROID_NDK_VERSION=28.1.13356709
ENV ANDROID_SDK_VERSION=35
ENV ANDROID_BUILD_TOOLS_VERSION=35.0.1
ENV ANDROID_CMDLINE_TOOLS_VERSION=13114758
ENV GRADLE_VERSION=8.13.2
ENV PATH=${PATH}:${ANDROID_SDK_ROOT}/cmdline-tools/latest/bin:${ANDROID_SDK_ROOT}/platform-tools:${ANDROID_SDK_ROOT}/build-tools/${ANDROID_BUILD_TOOLS_VERSION}:/opt/gradle/gradle-${GRADLE_VERSION}/bin

# Install Android SDK command line tools
RUN mkdir -p ${ANDROID_SDK_ROOT}/cmdline-tools \
    && wget -q https://dl.google.com/android/repository/commandlinetools-linux-${ANDROID_CMDLINE_TOOLS_VERSION}_latest.zip -O /tmp/cmdline-tools.zip \
    && unzip -q /tmp/cmdline-tools.zip -d ${ANDROID_SDK_ROOT}/cmdline-tools \
    && mv ${ANDROID_SDK_ROOT}/cmdline-tools/cmdline-tools ${ANDROID_SDK_ROOT}/cmdline-tools/latest \
    && rm /tmp/cmdline-tools.zip

# Accept licenses and install SDK components
RUN yes | sdkmanager --licenses \
    && sdkmanager "platforms;android-${ANDROID_SDK_VERSION}" \
    && sdkmanager "build-tools;${ANDROID_BUILD_TOOLS_VERSION}" \
    && sdkmanager "platform-tools" \
    && sdkmanager "ndk;${ANDROID_NDK_VERSION}"

# Install Gradle
RUN wget -q https://services.gradle.org/distributions/gradle-${GRADLE_VERSION}-bin.zip -O /tmp/gradle.zip \
    && unzip -q /tmp/gradle.zip -d /opt/gradle \
    && rm /tmp/gradle.zip \
    && ln -s /opt/gradle/gradle-${GRADLE_VERSION}/bin/gradle /usr/local/bin/gradle

# Verify installations
RUN sdkmanager --list_installed \
    && gradle --version \
    && ${ANDROID_SDK_ROOT}/ndk/${ANDROID_NDK_VERSION}/ndk-build --version

# Copy Android gradle project
COPY --from=builder /app/android ./android

# Export Android (requires Godot to generate gradle project first, then build with gradle)
# Godot exports to android/build which uses the gradle wrapper
RUN mkdir -p /app/build/android \
    && cd /app/android/build \
    && chmod +x gradlew \
    && ./gradlew assembleRelease -Pexport_package_name=com.godot.game -Pexport_version_code=1 -Pexport_version_name=1.0 -Pexport_enabled_abis="armeabi-v7a|arm64-v8a" -Pexport_format=apk -Pexport_build_type=release -Pexport_edition=mono \
    && cp app/build/outputs/apk/mono/release/android_mono-release.apk /app/build/android/AxiomForceReclaimEarth.apk

# ==========================================
# Final stage - minimal runtime for running the exported game client
FROM ubuntu:22.04 AS runtime-client

# Add .NET 10 Runtime to execute the C# game binary assemblies
RUN apt-get update && apt-get install -y \
    libglib2.0-0 libx11-6 libxcursor1 libxi6 libxrandr2 libxrender1 libxxf86vm1 libgl1-mesa-glx \
    ca-certificates libc6 libgcc1 libgssapi-krb5-2 libicu70 libssl3 libstdc++6 zlib1g \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy exported client game from exporter
COPY --from=exporter /app/build/linux ./build/linux

# Set entrypoint for client
ENTRYPOINT ["./build/linux/AxiomForceReclaimEarth.x86_64"]

# Dedicated server runtime stage
FROM ubuntu:22.04 AS runtime-server

# Install minimal dependencies for headless server
# Add .NET 10 Runtime dependencies to execute the C# server binary
RUN apt-get update && apt-get install -y \
    libglib2.0-0 \
    ca-certificates libc6 libgcc1 libgssapi-krb5-2 libicu70 libssl3 libstdc++6 zlib1g \
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

# Android artifact stage - just copies the APK for artifact upload
FROM scratch AS android-artifact

# Copy APK from exporter-android
COPY --from=exporter-android /app/build/android/AxiomForceReclaimEarth.apk /AxiomForceReclaimEarth.apk