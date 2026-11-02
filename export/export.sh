#!/bin/sh
# Axiom Force — headless export driver used by the `export` compose service.
#
# Usage: export.sh [linux|windows|web|android|macos|ios|all]
# macos prepares an unsigned ZIP; ios prepares source for native Mac/Xcode completion.
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
        echo "  docker compose run --rm export windows|web|android|macos|ios|all"
        echo ""
        echo "Game exports are exclusive: run them with no other build or edit in flight."
        exit 0
        ;;
esac

# Reject invalid requests before toolchain setup, imports, or writes to build/.
case "$1" in
    linux|windows|win|web|android|macos|ios|all) ;;
    *) echo "ERROR: Unknown target. Use linux, windows, web, android, macos, ios, or all." >&2; exit 2 ;;
esac

# Compose forwards this marker so running the exporter from Actions is refused too.
if [ "${GITHUB_ACTIONS:-false}" = "true" ]; then
    echo "ERROR: Game exports are disabled in GitHub Actions. Use local Docker." >&2
    exit 1
fi
# This driver belongs to the Docker image; host callers must use a root launcher.
if [ ! -f /.dockerenv ]; then
    echo "ERROR: Run exports through the local Docker Compose export service." >&2
    exit 1
fi

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

# iOS C# compilation needs Xcode/macOS. Prepare a source handoff without Linux imports or builds.
if [ "${TARGET}" = "ios" ]; then
    exec python3 /opt/axiom-export/apple_release.py prepare-ios /workspace
fi
# Validate Apple identity before any expensive build or output, including an all-target request.
if [ "${TARGET}" = "macos" ] || [ "${TARGET}" = "all" ]; then
    python3 /opt/axiom-export/apple_release.py check-macos /workspace
fi

# --- Android keystore -------------------------------------------------------
# export_presets.cfg points at ~/.android/debug.keystore with the standard
# androiddebugkey/android credentials. Generate it once per container so the
# Android preset has something to sign with.
# Create container-local debug signing/configuration using the image's Android SDK and JDK.
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
# Export the named preset to the supplied path; retain the existing debug fallback and failure status.
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

# Produce an unsigned macOS intermediate with no silent debug fallback; Mac signing is still required.
export_macos() {
    mkdir -p "${BUILD_DIR}/macos"
    godot --headless --export-release "macOS" "${BUILD_DIR}/macos/AxiomForce-unsigned.zip"
    cp /workspace/export/APPLE_RELEASE.md "${BUILD_DIR}/macos/APPLE_RELEASE.md"
    echo "UNSIGNED macOS ZIP prepared. Complete signing/notarization on a Mac before release."
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
    macos)
        export_macos
        ;;
    all)
        # Preserve legacy best-effort targets, then require the new Apple outputs to succeed.
        export_preset "Linux/X11" "${BUILD_DIR}/linux/AxiomForce.x86_64" || true
        export_preset "Windows Desktop" "${BUILD_DIR}/windows/AxiomForce.exe" || true
        export_preset "Web" "${BUILD_DIR}/web/index.html" || true
        setup_android
        export_preset "Android" "${BUILD_DIR}/android/AxiomForce.apk" || true
        export_macos
        python3 /opt/axiom-export/apple_release.py prepare-ios /workspace
        ;;
    *)
        echo "Unknown target '${TARGET}'. Use: linux, windows, web, android, macos, ios, all."
        exit 2
        ;;
esac

echo "=========================================================="
echo "  Export finished — outputs under ./build"
echo "=========================================================="
ls -lhR "${BUILD_DIR}" 2>/dev/null || true
