#!/usr/bin/env bash
set -e

TARGET="${1:-${TARGET:-all}}"

echo "=========================================================="
echo "    AXIOM FORCE — On-Demand Godot Multi-Platform Exporter"
echo "=========================================================="
echo "Requested Target: ${TARGET}"
echo "Godot Version   : $(godot --version 2>/dev/null || echo 'Unknown')"
echo "Dotnet Version  : $(dotnet --version 2>/dev/null || echo 'Unknown')"
echo "=========================================================="

cd /workspace

if [ ! -f "project.godot" ]; then
    echo "ERROR: project.godot not found in /workspace!"
    exit 1
fi

mkdir -p build/windows build/linux build/linux_server build/android build/macos build/ios build/visionos build/xbox build/web

echo "--> [1/3] Restoring and building C# project (Release)..."
dotnet restore AxiomForce.csproj
dotnet build AxiomForce.csproj -c Release --no-restore

echo "--> [2/3] Importing Godot project assets in headless mode..."
godot --headless --editor --quit || true

export_preset() {
    local preset_name="$1"
    local output_path="$2"
    local dir
    dir=$(dirname "$output_path")
    mkdir -p "$dir"
    echo "----------------------------------------------------------"
    echo "Exporting: [${preset_name}] -> ${output_path}"
    echo "----------------------------------------------------------"
    if godot --headless --export-release "${preset_name}" "${output_path}"; then
        echo "SUCCESS: ${preset_name} exported successfully."
    else
        echo "Release export failed, attempting debug export..."
        if godot --headless --export-debug "${preset_name}" "${output_path}"; then
            echo "SUCCESS (DEBUG): ${preset_name} exported successfully."
        else
            echo "WARNING: Could not export ${preset_name}."
            return 1
        fi
    fi
}

echo "--> [3/3] Performing requested exports..."

case "${TARGET}" in
    windows|win)
        export_preset "Windows Desktop" "build/windows/AxiomForce.exe"
        ;;
    linux)
        export_preset "Linux/X11" "build/linux/AxiomForce.x86_64"
        export_preset "Linux Server" "build/linux_server/AxiomForce_Server.x86_64" || true
        ;;
    android)
        mkdir -p ~/.android
        if [ ! -f ~/.android/debug.keystore ]; then
            keytool -keyalg RSA -genkeypair -alias androiddebugkey -keypass android -keystore ~/.android/debug.keystore -storepass android -dname "CN=Android Debug,O=Android,C=US" -validity 9999 2>/dev/null || true
        fi
        export_preset "Android" "build/android/AxiomForce.apk"
        ;;
    macos|mac)
        export_preset "macOS" "build/macos/AxiomForce.zip"
        ;;
    ios)
        export_preset "iOS" "build/ios/AxiomForce.zip"
        ;;
    visionos)
        export_preset "visionOS" "build/visionos/AxiomForce_visionOS.zip"
        ;;
    xbox)
        export_preset "Xbox / Windows GDK" "build/xbox/AxiomForce_Xbox.exe"
        ;;
    web)
        export_preset "Web" "build/web/index.html"
        ;;
    all)
        echo "Exporting all configured platforms..."
        export_preset "Windows Desktop" "build/windows/AxiomForce.exe" || true
        export_preset "Linux/X11" "build/linux/AxiomForce.x86_64" || true
        export_preset "Linux Server" "build/linux_server/AxiomForce_Server.x86_64" || true
        export_preset "macOS" "build/macos/AxiomForce.zip" || true
        export_preset "iOS" "build/ios/AxiomForce.zip" || true
        export_preset "visionOS" "build/visionos/AxiomForce_visionOS.zip" || true
        export_preset "Xbox / Windows GDK" "build/xbox/AxiomForce_Xbox.exe" || true
        export_preset "Web" "build/web/index.html" || true
        mkdir -p ~/.android
        if [ ! -f ~/.android/debug.keystore ]; then
            keytool -keyalg RSA -genkeypair -alias androiddebugkey -keypass android -keystore ~/.android/debug.keystore -storepass android -dname "CN=Android Debug,O=Android,C=US" -validity 9999 2>/dev/null || true
        fi
        export_preset "Android" "build/android/AxiomForce.apk" || true
        ;;
    *)
        echo "Custom preset requested: ${TARGET}"
        export_preset "${TARGET}" "build/${TARGET}/AxiomForce"
        ;;
esac

echo "=========================================================="
echo "    EXPORT COMPLETE! Build outputs available in ./build:"
echo "=========================================================="
ls -lhR build/ || true
