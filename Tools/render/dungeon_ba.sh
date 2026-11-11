#!/bin/bash
# Renders dungeons and stitches before/after sheets: Tools/render/dungeon_ba.sh <before dir> <zone>...   -> maps/renders/dungeon/<zone>_before_after.png
HERE="$(cd "$(dirname "$0")" && pwd)"; ROOT="$(cd "$HERE/../.." && pwd)"; B="$1"; shift
bash "$HERE/render.sh" "$@" | tail -n "$#"
for z in "$@"; do python3 "$HERE/before_after.py" "$B/$z.png" "$HERE/_render/$z.png" "$ROOT/maps/renders/dungeon/${z}_before_after.png"; done
