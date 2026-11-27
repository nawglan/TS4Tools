#!/usr/bin/env bash
# Scan a Sims 4 game folder with `ts4tools scan` and write the wrapper coverage report:
#   docs/unsupported-resources.md      (committed)
#   artifacts/unsupported-resources.json (gitignored)
#
# Usage: scripts/scan-unsupported.sh [game-folder] [extra scan options...]
#   game-folder defaults to "/mnt/ai/SteamLibrary/steamapps/common/The Sims 4"
#   extra options are passed to `ts4tools scan` (default: --roundtrip)
#
# The game folder is only read. Package paths in the report are relative to it.
set -euo pipefail

repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
game="${1:-/mnt/ai/SteamLibrary/steamapps/common/The Sims 4}"
shift || true
if [[ $# -eq 0 ]]; then
  set -- --roundtrip
fi

if [[ ! -d "$game" ]]; then
  echo "error: game folder not found: $game" >&2
  exit 2
fi

cli="$repo/src/TS4Tools.Cli/bin/Release/net10.0/ts4tools.dll"
# Build in Release if the CLI is missing or any source is newer than it.
if [[ ! -f "$cli" ]] || [[ -n "$(find "$repo/src" -name '*.cs' -newer "$cli" -print -quit)" ]]; then
  echo "Building ts4tools (Release)..." >&2
  dotnet build "$repo/src/TS4Tools.Cli/TS4Tools.Cli.csproj" -c Release --nologo -v quiet >&2
fi

mkdir -p "$repo/artifacts" "$repo/docs"
status=0
dotnet "$cli" scan "$game" "$@" \
  --markdown "$repo/docs/unsupported-resources.md" \
  --json "$repo/artifacts/unsupported-resources.json" || status=$?

# Exit code 1 only means "problems were found", which is the expected result of a coverage scan.
if [[ $status -gt 1 ]]; then
  exit "$status"
fi
echo "Wrote docs/unsupported-resources.md and artifacts/unsupported-resources.json" >&2
