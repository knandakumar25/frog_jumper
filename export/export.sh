#!/bin/sh
# Axiom Force — headless export driver used by the `export` compose service.
#
# Usage: export.sh [linux|windows|web|android|all]
#
# Runs inside the image built from export/Dockerfile, with the repository
# bind-mounted at /workspace. It prepares the toolchain state that the Godot
# editor normally creates through its UI (Android keystore, editor settings),
# imports the project, builds the C# assemblies, then runs the requested
# export presets into ./build.
set -e

# An export must never start as a side effect of `docker compose up` or of a
# bare container start. Without an explicit target the container explains how
# to request one and exits without touching ./build.
case "${1:-}" in
    ""|help|--help|-h)
        echo "No export requested — nothing was built."
        echo ""
        echo "Exports never run automatically. Request one explicitly:"
        echo "  docker compose run --rm export linux"
        echo "  docker compose run --rm export windows|web|android|all"
        echo ""
        echo "Game exports are exclusive: run them with no other build or edit in flight."
        exit 0
        ;;
esac

TARGET="$1"
BUILD_DIR="/workspace/build"
ANDROID_DEBUG_KEYSTORE="/root/.android/debug.keystore"

echo "=========================================================="
echo "  AXIOM FORCE — Headless Exporter"
echo "  Target        : ${TARGET}"
echo "  Godot         : $(godot --version 2>/dev/null || echo unknown)"
echo "  .NET SDK      : $(dotnet --version 2>/dev/null || echo unknown)"
echo "  Java          : $(java -version 2>&1 | head -n 1 || echo unknown)"
echo "  Android SDK   : ${ANDROID_HOME}"
echo "=========================================================="

if [ ! -f /workspace/project.godot ]; then
    echo "ERROR: project.godot not found at /workspace — the repository must be"
    echo "       bind-mounted there (docker compose run --rm export <target>)."
    exit 1
fi

# --- Android keystore -------------------------------------------------------
# export_presets.cfg points at ~/.android/debug.keystore with the standard
# androiddebugkey/android credentials. Generate it once per container so the
# Android preset has something to sign with.
setup_android() {
    echo "--> [Android] Preparing debug keystore and SDK paths..."
    mkdir -p /root/.android
    if [ ! -f "${ANDROID_DEBUG_KEYSTORE}" ]; then
        keytool -keyalg RSA -genkeypair -alias androiddebugkey -keypass android \
            -keystore "${ANDROID_DEBUG_KEYSTORE}" -storepass android \
            -dname "CN=Android Debug,O=Android,C=US" -validity 9999
    fi

    # Godot reads SDK/keystore locations from its editor settings file; seeding
    # it avoids the interactive "missing Android SDK" prompt.
    mkdir -p /root/.config/godot
    cat > /root/.config/godot/editor_settings-4.tres <<EOF
[gd_resource type="EditorSettings" format=3]

[resource]
export/android/android_sdk_path = "${ANDROID_HOME}"
export/android/debug_keystore = "${ANDROID_DEBUG_KEYSTORE}"
export/android/debug_keystore_user = "androiddebugkey"
export/android/debug_keystore_pass = "android"
export/android/java_sdk_path = "${JAVA_HOME}"
EOF
    cp /root/.config/godot/editor_settings-4.tres /root/.config/godot/editor_settings-4.7.tres
}

# --- Export helpers ---------------------------------------------------------
export_preset() {
    preset="$1"
    output="$2"
    mkdir -p "$(dirname "${output}")"
    echo "----------------------------------------------------------"
    echo "Exporting: [${preset}] -> ${output}"
    echo "----------------------------------------------------------"
    if godot --headless --export-release "${preset}" "${output}"; then
        echo "SUCCESS: ${preset} exported."
    elif godot --headless --export-debug "${preset}" "${output}"; then
        echo "SUCCESS (debug fallback): ${preset} exported."
    else
        echo "WARNING: ${preset} could not be exported."
        return 1
    fi
}

# --- Prepare project --------------------------------------------------------
cd /workspace
mkdir -p "${BUILD_DIR}"

echo "--> [1/3] Restoring and building C# assemblies (Release)..."
dotnet restore AxiomForce.csproj
dotnet build AxiomForce.csproj -c Release --no-restore

echo "--> [2/3] Importing project assets (headless)..."
godot --headless --editor --quit || true

echo "--> [3/3] Running export preset(s) for '${TARGET}'..."
case "${TARGET}" in
    linux)
        export_preset "Linux/X11" "${BUILD_DIR}/linux/AxiomForce.x86_64"
        ;;
    windows|win)
        export_preset "Windows Desktop" "${BUILD_DIR}/windows/AxiomForce.exe"
        ;;
    web)
        export_preset "Web" "${BUILD_DIR}/web/index.html"
        ;;
    android)
        setup_android
        export_preset "Android" "${BUILD_DIR}/android/AxiomForce.apk"
        ;;
    all)
        # Presets that cannot be produced on Linux without the platform SDKs
        # (macOS/iOS/visionOS/Xbox) are attempted last and tolerated as failures.
        export_preset "Linux/X11" "${BUILD_DIR}/linux/AxiomForce.x86_64" || true
        export_preset "Windows Desktop" "${BUILD_DIR}/windows/AxiomForce.exe" || true
        export_preset "Web" "${BUILD_DIR}/web/index.html" || true
        setup_android
        export_preset "Android" "${BUILD_DIR}/android/AxiomForce.apk" || true
        ;;
    *)
        echo "Unknown target '${TARGET}'. Use: linux, windows, web, android, all."
        exit 2
        ;;
esac

echo "=========================================================="
echo "  Export finished — outputs under ./build"
echo "=========================================================="
ls -lhR "${BUILD_DIR}" 2>/dev/null || true
