#!/bin/bash
# furniture facing test (2026-11-21, Yan: "too detailed", "they're all facing a diagonal angle, they need to be 3/4 angle but face up, down, left and right"): desk east, two views, low detail
cd "$(dirname "$0")/../.."
until grep -q "PLAN DONE" /tmp/walk_run.log 2>/dev/null; do sleep 20; done
T() { [ -f world/furn2_pl/$1.png ] || VIEW="$2" DIR=$3 DETAIL="low detail" SHADING="basic shading" python3 pixellab_pixflux.py world/furn2_pl/$1.png 40 40 5 "$4" > /dev/null 2>&1; echo "$1 $([ -f world/furn2_pl/$1.png ] && echo ok || echo FAIL)"; }
D="plain wooden office desk with drawers, seen straight from the side"
T desk_east_lowtd "low top-down" east "$D"
T desk_east_side "side" east "$D"
T desk_south_lowtd "low top-down" south "plain wooden office desk with drawers, seen from the front"
T desk_south_side "side" south "plain wooden office desk with drawers, seen from the front"
echo TESTDONE
