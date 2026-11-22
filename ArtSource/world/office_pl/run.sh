#!/bin/bash
# Pixel Lab office set + wooden floor (2026-11-22; Yan: "the office doesn't look like an office, create new sprites for office furniture and a wooden floor"): low detail, small, straight front / back views (his earlier complaints: too detailed, diagonal angles).
cd "$(dirname "$0")/../.."
F() { # name dir w h desc
  [ -f world/office_pl/$1_$2.png ] || VIEW="side" DIR=$2 DETAIL="low detail" SHADING="basic shading" python3 pixellab_pixflux.py world/office_pl/$1_$2.png $3 $4 6 "$5" > /dev/null 2>&1
  echo "$1 $2 $([ -f world/office_pl/$1_$2.png ] && echo ok || echo FAIL)"
}
[ -d world/tiles_pl/wood ] || (cd world/tiles_pl && python3 make_tileset.py wood "dark worn wooden plank floor with grain and scuffs, planks running left to right" "dark wood panel wall top with grain" 3 > /tmp/tile_wood.log 2>&1); echo "wood $([ -d world/tiles_pl/wood ] && echo ok || echo FAIL)"
for d in south north; do
F odesk $d 40 40 "plain wooden office desk with two drawer pedestals, a few papers on top"
F ochair $d 24 32 "simple office swivel chair with a dark fabric seat"
F opc $d 40 40 "wooden office desk with an old beige crt computer monitor and keyboard on it, papers"
F ofile $d 28 44 "tall grey metal four-drawer filing cabinet"
F oshelf $d 40 48 "tall wooden bookshelf full of books and binders"
F oplant $d 24 36 "dead dried-up potted plant in a pot"
F ocooler $d 24 44 "office water cooler with an empty water bottle on top"
F osofa $d 48 32 "worn brown leather two-seat office sofa"
done
echo OFFICEDONE
