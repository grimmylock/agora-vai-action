#!/bin/bash
# Pixel Lab pixflux props for the morgue (2026-11-21), one job at a time. out: world/morgue_pl/<name>_<seed>.png
cd "$(dirname "$0")/../.."
G() { # name w h seed desc
  [ -f world/morgue_pl/$1_$4.png ] || VIEW="high top-down" DIR=south DETAIL="medium detail" SHADING="medium shading" python3 pixellab_pixflux.py world/morgue_pl/$1_$4.png $2 $3 $4 "$5" > /dev/null 2>&1
  echo "$1 $4 $([ -f world/morgue_pl/$1_$4.png ] && echo ok || echo FAIL)"
}
for s in 11 12; do
G gurney 64 80 $s "rusty steel hospital gurney trolley seen from above at an angle, with a body under a stained grey sheet, a pale hand hanging off the side, dark dried blood on the sheet, horror game prop"
G bodybag 64 48 $s "black plastic body bag lying on the floor seen from above, half unzipped, a pale hand sticking out, blood stain, horror game prop"
G wheelchair 56 64 $s "old rusty abandoned hospital wheelchair seen from above at an angle, cracked leather seat, bent wheels, blood stains, horror game prop"
G bedframe 64 80 $s "rusted metal hospital bed frame with bare springs and leather restraint straps seen from above, bloodstains, horror game prop"
G ivstand 32 96 $s "bent metal IV drip pole standing upright with a bag of dark blood and a hanging tube, rusty, horror game prop"
G hangbag 40 128 $s "a black body bag hanging from the ceiling on a rusty chain, zipped, bulging with a body shape inside, blood dripping at the bottom, horror game prop"
done
echo ALLDONE
