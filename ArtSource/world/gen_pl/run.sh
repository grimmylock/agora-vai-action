#!/bin/bash
# Pixel Lab pixflux world props, trial batch (2026-11-21): out world/gen_pl/<name>_<seed>.png, one job at a time. view "high top-down", medium detail.
cd "$(dirname "$0")/../.."
G() { # name w h seed desc
  [ -f world/gen_pl/$1_$4.png ] || VIEW="high top-down" DIR=south DETAIL="medium detail" SHADING="medium shading" python3 pixellab_pixflux.py world/gen_pl/$1_$4.png $2 $3 $4 "$5" > /dev/null 2>&1
  echo "$1 $4 $([ -f world/gen_pl/$1_$4.png ] && echo ok || echo FAIL)"
}
for s in 21 22; do
G stalagmite 40 56 $s "tall grey limestone stalagmite rock spike growing from a cave floor, wet, dark, horror game cave prop"
G crystals 48 48 $s "cluster of glowing pale blue-green crystals growing out of dark cave rock, cave prop"
G bonepile 56 40 $s "pile of old yellowed bones and a skull on a dark cave floor, horror game prop"
G mushrooms 48 40 $s "cluster of pale glowing cave mushrooms with spotted caps, dark cave floor, horror game prop"
G roots 56 56 $s "tangle of thick dead tree roots and dirt hanging into a cave, horror game prop"
G monitor 40 48 $s "broken old computer monitor on a metal stand with a cracked screen and loose cables, abandoned lab, horror game prop"
G pipes 64 40 $s "bundle of rusty industrial pipes with valves and drips lying across the floor, horror game prop"
G barrel 32 40 $s "rusty steel oil drum barrel, dented, with dark stains, horror game prop"
G hospcart 48 48 $s "abandoned hospital supply cart with a tray of scalpels and bandages, rusty, bloodstained, horror game prop"
G rubble 56 40 $s "pile of broken concrete chunks with rebar sticking out, dusty, horror game prop"
G deadtree 72 112 $s "tall dead twisted leafless tree with grey bark, horror game prop"
G pine 72 112 $s "dark tall pine tree with drooping branches seen from above at an angle, horror game prop"
G birch 64 104 $s "sickly pale birch tree with thin grey-white bark and few yellow leaves, horror game prop"
done
echo ALLDONE
