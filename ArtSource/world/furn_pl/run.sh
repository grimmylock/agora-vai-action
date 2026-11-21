#!/bin/bash
# Pixel Lab pixflux furniture in four facings (2026-11-21, Yan: "computers, desks, chairs ... other angle versions, from the side in 3/4"): furn_pl/<item>_<dir>_<seed>.png, one job at a time.
cd "$(dirname "$0")/../.."
F() { [ -f world/furn_pl/$1_$2_$4.png ] || VIEW="high top-down" DIR=$2 DETAIL="medium detail" SHADING="medium shading" python3 pixellab_pixflux.py world/furn_pl/$1_$2_$4.png $5 $6 $4 "$3" > /dev/null 2>&1; echo "$1 $2 $([ -f world/furn_pl/$1_$2_$4.png ] && echo ok || echo FAIL)"; }
S=31
for d in south east north west; do
F desk $d "old wooden office desk with drawers, scratched, papers on top, abandoned, horror game prop" $S 64 48
F chair $d "old worn office swivel chair on wheels, torn fabric, abandoned, horror game prop" $S 40 48
F computer $d "old beige desktop computer with a bulky crt monitor and keyboard on a desk, dusty, cracked screen, abandoned, horror game prop" $S 48 48
F labbench $d "steel laboratory bench with beakers, a microscope and clutter, grimy, abandoned, horror game prop" $S 64 48
F cabinet $d "tall grey metal filing cabinet with drawers, dented, rusty, abandoned, horror game prop" $S 40 56
done
echo FURNDONE
