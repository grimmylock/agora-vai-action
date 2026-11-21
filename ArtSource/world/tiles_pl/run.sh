#!/bin/bash
# the trial tilesets: a floor and its wall-top / second material per call (the base tiles are what the ground pattern is made of)
cd "$(dirname "$0")"
until grep -q "PLAN DONE" /tmp/choir_run.log 2>/dev/null; do sleep 20; done
T() { [ -d "$1" ] || python3 make_tileset.py "$1" "$2" "$3" "${4:-1}" > /tmp/tile_$1.log 2>&1; echo "$1 $([ -d $1 ] && echo ok || echo FAIL)"; }
T cave "dark damp cave floor of packed grey dirt and small stones" "rough dark grey cave rock wall top with cracks"
T lab "grimy pale green lab floor tiles with dark grout and stains" "dirty concrete wall top with seams and rust streaks"
T concrete "worn grey poured concrete floor with cracks and stains" "rusty corrugated steel plate"
T hospital "dirty cracked white and grey hospital floor tiles with blood stains" "peeling dark green painted plaster wall top"
touch /tmp/tiles_done
echo ALLDONE
