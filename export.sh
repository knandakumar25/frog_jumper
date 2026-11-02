#!/usr/bin/env bash
# Local Docker-only launcher. Help never builds; an explicit supported target is required.
# Resolve the project relative to this script so callers may run it from any directory.
set -eu

# Describe supported container targets without invoking Docker or the game engine.
usage() {
    echo "Usage: ./export.sh [linux|windows|web|android|macos|ios|all]"
    echo "No target or --help prints this message without building or exporting."
    echo "Exports are local Docker operations; GitHub Actions exports are disabled."
}

if [ "$#" -eq 0 ]; then usage; exit 0; fi
if [ "$#" -ne 1 ]; then usage >&2; exit 2; fi
case "$1" in
    help|--help|-h) usage; exit 0 ;;
    linux|windows|web|android|macos|ios|all) TARGET="$1" ;;
    *) echo "ERROR: Unsupported export target." >&2; usage >&2; exit 2 ;;
esac

# Refuse CI even when a caller supplies a valid target; never dispatch an Actions workflow.
if [ "${GITHUB_ACTIONS:-false}" = "true" ]; then
    echo "ERROR: Game exports are disabled in GitHub Actions. Use local Docker." >&2
    exit 1
fi

cd -- "$(dirname -- "$0")"
# Both commands name the actual opt-in Compose service. The build alone never exports.
docker compose --profile export build export
docker compose --profile export run --rm export "$TARGET"
echo "Export complete. Outputs: ./build/"
