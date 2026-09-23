#!/usr/bin/env bash
set -e

TARGET="${1:-all}"

echo "=========================================================="
echo "  AXIOM FORCE — On-Demand Godot Exporter (Docker)"
echo "=========================================================="
echo "  Target: ${TARGET}"
echo "  Usage : ./export.sh [windows|linux|android|macos|ios|visionos|xbox|web|all]"
echo "=========================================================="

docker compose build exporter
docker compose --profile export run --rm -e TARGET="${TARGET}" exporter "${TARGET}"

echo "=========================================================="
echo "  Export complete! Outputs located in ./build/"
echo "=========================================================="
