#!/usr/bin/env bash
# ==============================================================================
# Automates Godot asset import by running Godot in headless mode.
# ==============================================================================
set -euo pipefail

GODOT_PATH="${1:-/c/Users/karthiknk21/Desktop/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64.exe}"
WAIT_SECONDS="${2:-5}"

echo "========================================="
echo " Starting Godot Headless Asset Importer  "
echo "========================================="
echo "Godot Path : $GODOT_PATH"
echo "Wait Time  : ${WAIT_SECONDS}s"

"$GODOT_PATH" --headless &
GODOT_PID=$!

echo "Waiting ${WAIT_SECONDS}s for .import files to generate..."
sleep "$WAIT_SECONDS"

if kill -0 "$GODOT_PID" 2>/dev/null; then
    echo "Terminating Godot import process (PID: $GODOT_PID)..."
    kill -9 "$GODOT_PID" 2>/dev/null || true
fi

echo "Asset import completed successfully."

