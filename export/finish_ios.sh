#!/usr/bin/env bash
# Explicit native-Mac completion for a Docker-prepared source handoff.
# --check validates prerequisites only. project generates an Xcode project, never an IPA.
set -eu
case "${1:-help}" in
    help|--help|-h)
        echo "Usage: bash export/finish_ios.sh [--check|project]"
        echo "Run on a Mac after the steps in export/APPLE_RELEASE.md."
        exit 0 ;;
    --check|project) ACTION="$1" ;;
    *) echo "ERROR: Choose --check or project." >&2; exit 2 ;;
esac
if [ "${GITHUB_ACTIONS:-false}" = "true" ]; then
    echo "ERROR: GitHub Actions Apple exports are disabled." >&2; exit 1
fi
if [ "$(uname -s)" != Darwin ]; then
    echo "ERROR: iOS export requires a Mac with Xcode, outside the Linux Docker container." >&2; exit 1
fi
cd -- "$(dirname -- "$0")/.."
GODOT_BIN="${GODOT_BIN:-/Applications/Godot_mono.app/Contents/MacOS/Godot}"
if [ ! -x "$GODOT_BIN" ]; then
    echo "ERROR: Set GODOT_BIN to the Godot 4.7.2 .NET executable." >&2; exit 1
fi
case "$("$GODOT_BIN" --version)" in
    4.7.2*mono*) ;;
    *) echo "ERROR: Godot 4.7.2 .NET is required." >&2; exit 1 ;;
esac
command -v python3 >/dev/null
command -v dotnet >/dev/null
dotnet --list-sdks | grep '^10\.' >/dev/null || {
    echo "ERROR: Install the .NET 10 SDK." >&2; exit 1;
}
xcodebuild -version
xcrun --sdk iphoneos --show-sdk-path
dotnet workload list | grep -E '(^|[[:space:]])ios([[:space:]]|$)' >/dev/null || {
    echo "ERROR: Install the compatible .NET iOS workload (see the release guide)." >&2; exit 1;
}
python3 export/apple_release.py check-ios .
if [ "$ACTION" = --check ]; then
    echo "Prerequisites passed. Matching Godot export templates and signing must still be configured."
    exit 0
fi
# Only the explicit project subcommand reaches engine import/export.
mkdir -p build/ios/xcode
"$GODOT_BIN" --headless --path . --editor --import
"$GODOT_BIN" --headless --path . --export-release "iOS" build/ios/xcode/AxiomForce.zip
echo "Xcode project generation finished. Open the generated .xcodeproj; archive and distribute in Xcode."
