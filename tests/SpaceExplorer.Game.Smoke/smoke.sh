#!/usr/bin/env bash
# Exports the Godot project with the Linux preset and runs the exported build's smoke check.
# Usage: tests/SpaceExplorer.Game.Smoke/smoke.sh
# Requires: Godot 4.7.2 .NET editor (`godot` on PATH or $GODOT), matching export templates, .NET SDK.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
godot="${GODOT:-godot}"
project="$root/src/SpaceExplorer.Game"
out="$root/build/smoke/linux"
bin="$out/SpaceExplorer.Game.x86_64"

rm -rf "$out"
mkdir -p "$out"

"$godot" --headless --path "$project" --import
"$godot" --headless --path "$project" --export-release "Linux" "$bin"

output="$("$bin" --headless -- --smoke 2>&1)" || {
  printf '%s\n' "$output"
  echo "smoke: exported build exited with a non-zero code" >&2
  exit 1
}
printf '%s\n' "$output"
grep -q "SMOKE OK" <<<"$output" || {
  echo "smoke: 'SMOKE OK' marker not found in output" >&2
  exit 1
}
