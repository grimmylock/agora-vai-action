"""RT 6m (Yan: "the rinaldi r6 barrel is looking a little chonky, both it and the rk-12 are bottom firing"): the two bottom-
barrel revolvers get their bore at the level of the bottom chamber. Redraws the part of the in-hand sprite in front of the
cylinder (x >= FROM) from scratch, so re-running is safe. The menu sprites (_hd) are untouched.
  python3 ArtSource/weapons/hand/bottom_barrels.py   (then ArtSource/weapons/mags.py, to re-cut the cylinders)"""
import os
from PIL import Image
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
WEAP = os.path.join(ROOT, "Assets/Resources/Sprites/Weapons")
PAL = {"0": "#d5dadd", "1": "#a9b0b5", "2": "#727980", "3": "#4c5258", "4": "#33373b", "5": "#1f2225", "K": "#141617"}
def hexc(h): h = h.lstrip("#"); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)

# key: (first redrawn column, {row: text from that column}); '.' = empty. Outline (K) is drawn by hand here.
FRONT = {
    # Rinaldi R6: the shroud in front of the cylinder is 3 rows instead of 5 (lit top, the cut-outs, the shaded underside),
    # level with the bottom chamber; the top strap stops over the cylinder; the bore is the dark pixel low at the front
    "RinaldiR6": (11, {1: "KK",
                       2: "0K",
                       3: "11KKKKKKKK",
                       4: "1000000000K",
                       5: "1144114412K",
                       6: "2222222225K",
                       7: "KKKKKKKKKK"}),
    # RK-12 Kuvalda: the big housing stays (as on the real gun), but the top rail stops short and the barrel stands out
    # at the bottom front with the bore in its lowest row
    "RK12Kuvalda": (20, {0: "K...", 1: "3K..", 2: "2K..", 3: "4KKK",
                         4: "222K", 5: "333K", 6: "225K", 7: "KKK."}),
}

for key, (x0, rows) in FRONT.items():
    p = os.path.join(WEAP, f"weapon_{key}.png"); im = Image.open(p).convert("RGBA"); px = im.load()
    for y in range(im.height):
        for x in range(x0, im.width): px[x, y] = (0, 0, 0, 0)
    for y, row in rows.items():
        for i, ch in enumerate(row):
            if ch != "." and x0 + i < im.width: px[x0 + i, y] = hexc(PAL[ch])
    im.save(p); print(key, "front redrawn from x", x0)
