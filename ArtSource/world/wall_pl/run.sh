#!/bin/bash
# Pixel Lab pixflux wall faces (trial, 2026-11-21): a 64x40 strip of front-view wall per material, out wall_pl/<name>_<seed>.png. One job at a time.
cd "$(dirname "$0")/../.."
W() { [ -f world/wall_pl/$1_$2.png ] || VIEW="side" DIR=south DETAIL="medium detail" SHADING="medium shading" python3 pixellab_pixflux.py world/wall_pl/$1_$2.png 64 40 $2 "$3" > /dev/null 2>&1; echo "$1 $2 $([ -f world/wall_pl/$1_$2.png ] && echo ok || echo FAIL)"; }
for s in 41 42; do
W plaster $s "front view of a dirty hospital wall: pale grey-green plaster with peeling paint, water stains, a dark baseboard at the bottom, flat orthographic wall texture, horror game"
W cliff $s "front view of a dark grey cave rock wall face: rough cracked stone with damp stains, flat orthographic rock wall texture, horror game"
W concrete $s "front view of a grimy concrete bunker wall with panel seams, rust streaks and stains, flat orthographic wall texture, horror game"
W brick $s "front view of an old dark red brick wall with missing bricks, soot and moss, flat orthographic wall texture, horror game"
done
echo WALLDONE
