# Axiom Force — headless game exporter.
#
# Linux-hosted Godot 4.7.2 .NET export tools, with nothing borrowed from the
# host: the .NET 11 SDK, a JDK 17 (the version Godot's Android Gradle step
# expects — NOT the JDK 25 in eclipse-temurin:latest), the Android SDK, the
# headless Godot .NET editor and matching templates. macOS ZIPs are unsigned;
# iOS source handoffs need a native Mac/Xcode for final project generation.
#
# Built and run only through the `export` compose profile, which is excluded
# from `docker compose up`: exports are exclusive and must not race game or
# website work.
# Bookworm's Python binaries use an older glibc baseline than the .NET runtime
# image, avoiding a dependency on Trixie's newer glibc in the Apple helper.
FROM python:3.15.2-slim-bookworm AS python-runtime
FROM mcr.microsoft.com/dotnet/sdk:11.0 AS python-verified

ENV DEBIAN_FRONTEND=noninteractive \
    GODOT_VERSION=4.7.2 \
    ANDROID_HOME=/opt/android-sdk \
    ANDROID_SDK_ROOT=/opt/android-sdk \
    JAVA_HOME=/usr/lib/jvm/java-17-openjdk-amd64 \
    PATH=/usr/local/bin:/usr/bin:/bin:/usr/sbin:/sbin:/usr/lib/jvm/java-17-openjdk-amd64/bin:/opt/android-sdk/cmdline-tools/latest/bin:/opt/android-sdk/platform-tools

# unzip/wget fetch the Godot artifacts; openjdk-17 is what Godot's Android
# export invokes. libicu backs the .NET editor binary and fontconfig/GL are
# resolved by Godot at startup — without libfontconfig1 it warns and fails to
# resolve fonts, and without libgl1 the binary cannot initialise a context.
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        openjdk-17-jdk-headless \
        python3 \
        unzip \
        wget \
        ca-certificates \
        libicu-dev \
        libfontconfig1 \
        libgl1 \
    && rm -rf /var/lib/apt/lists/*

# Keep export orchestration on the same application Python version. Distribution
# python3 remains available for OS tools; /usr/local/bin wins on the explicit PATH.
# Validate native standard-library dependencies when the opt-in image is built.
COPY --from=python-runtime /usr/local/ /usr/local/
RUN python3 -c "import sys, ssl, ctypes, zlib, bz2, lzma, sqlite3; assert sys.version_info[:3] == (3, 15, 2), sys.version"

# The Python-only stage can be checked without downloading export tools or
# generating any game artifacts. Ordinary exporter builds continue below.
FROM python-verified AS exporter

# Android command-line tools, then the packages the exporter preset targets
# (platform-tools, the android-34 platform and build-tools 34.0.0). Licenses
# are accepted here so no export run stops on an interactive prompt.
ARG CMDLINE_TOOLS_VERSION=11076708
RUN mkdir -p ${ANDROID_HOME}/cmdline-tools \
    && wget -q "https://dl.google.com/android/repository/commandlinetools-linux-${CMDLINE_TOOLS_VERSION}_latest.zip" -O /tmp/cmdline-tools.zip \
    && unzip -q /tmp/cmdline-tools.zip -d /tmp/cmdline-tools \
    && mv /tmp/cmdline-tools/cmdline-tools ${ANDROID_HOME}/cmdline-tools/latest \
    && rm -rf /tmp/cmdline-tools /tmp/cmdline-tools.zip \
    && yes | sdkmanager --licenses > /dev/null \
    && sdkmanager --install "platform-tools" "platforms;android-34" "build-tools;34.0.0" > /dev/null \
    && rm -rf ${ANDROID_HOME}/.temp ${ANDROID_HOME}/.downloadIntermediates

# Headless Godot .NET editor. `godot` on PATH is the only entry point the
# export script uses.
RUN wget -q "https://github.com/godotengine/godot/releases/download/${GODOT_VERSION}-stable/Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64.zip" -O /tmp/godot.zip \
    && unzip -q /tmp/godot.zip -d /opt \
    && mv "/opt/Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64" /opt/godot \
    && ln -sf "/opt/godot/Godot_v${GODOT_VERSION}-stable_mono_linux.x86_64" /usr/local/bin/godot \
    && rm -f /tmp/godot.zip

# Matching export templates, unpacked where Godot looks for them
# (~/.local/share/godot/export_templates/<version>.<type>).
RUN wget -q "https://github.com/godotengine/godot/releases/download/${GODOT_VERSION}-stable/Godot_v${GODOT_VERSION}-stable_mono_export_templates.tpz" -O /tmp/templates.tpz \
    && mkdir -p /root/.local/share/godot/export_templates \
    && unzip -q /tmp/templates.tpz -d /tmp/templates \
    && mv /tmp/templates/templates "/root/.local/share/godot/export_templates/${GODOT_VERSION}.stable.mono" \
    && rm -rf /tmp/templates /tmp/templates.tpz

COPY export.sh /usr/local/bin/export.sh
COPY apple_release.py /opt/axiom-export/apple_release.py
RUN chmod +x /usr/local/bin/export.sh

WORKDIR /workspace

ENTRYPOINT ["/usr/local/bin/export.sh"]
# Inert default: a bare `docker run` of this image must never start an export.
# A target has to be passed explicitly, e.g. `docker compose run --rm export linux`.
CMD ["help"]
