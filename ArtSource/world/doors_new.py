# New door faces (2026-11-20; Yan: "we need variety", "everything has the same wooden door", "big double door sprites ... lab doors, dungeon doors, double doors, a lot of doors").
# Each door is drawn into the 19-row door area of a built wall tile (walls.py: the lower storey, rows H-19..H-1 of the face; a single door fills x 3..12; a double door spans two tiles, 26 px, x 3..15 on the left
# tile and x 0..12 on the right). Drawn in the game's own wall palettes. References (drawing only, CC0): ArtSource/external/SOURCES.md "Door references". PREVIEW ONLY until Yan approves: not in the atlas yet.
#   python3 ArtSource/world/doors_new.py   -> /tmp/doors_new_preview.png
import math, random
import numpy as np
from PIL import Image, ImageDraw
from worldlib import *

K = (20, 22, 23, 255)
STEEL = P["steel"]; WOOD = P["wood"]; RUST = P["rust"]; CONC = P["concrete"]; LAB = P["lab"] + [hx("#a4acae")]
OAK = ramp("#1c120c", "#2e1d12", "#44291a", "#5e3a24", "#7a4d30")
GLASS = ramp("#0e1c20", "#1c363c", "#2e5560", "#4f8290", "#9fd0d8")
IRON = ramp("#0b0c0d", "#181b1d", "#2b3033", "#464e53", "#6c777e")
GOTHS = ramp("#1c1e24", "#272a31", "#353941", "#474c56", "#5f6570")
YEL = hx("#c8a02a"); BLK = hx("#16171a"); GREEN = hx("#58c860"); RED = hx("#d04a3a"); CYAN = hx("#5fd8e8")
def canvas(w): return Image.new("RGBA", (w, 19), (0, 0, 0, 0))
def rect(d, x0, y0, x1, y1, c):
    if x1 >= x0 and y1 >= y0: d.rectangle([x0, y0, x1, y1], fill=c)
def vlines(d, x0, x1, y0, y1, step, c): [d.line([(x, y0), (x, y1)], fill=c) for x in range(x0, x1 + 1, step)]
def hazard(d, x0, x1, y):
    for x in range(x0, x1 + 1): d.point((x, y), fill=YEL if ((x + y) // 2) % 2 == 0 else BLK)

# ---------------------------------------------------------------- singles (16 px canvas, the door in x 3..12)
def glass_single():
    im = canvas(16); d = ImageDraw.Draw(im)
    rect(d, 3, 0, 12, 18, IRON[2]); rect(d, 4, 1, 11, 13, GLASS[1])                                        # frame, glass
    for (x0, x1) in ((4, 7), (8, 11)):
        rect(d, x0, 1, x1, 13, GLASS[2]); d.line([(x0 + 1, 12), (x1, 4)], fill=GLASS[4]); d.line([(x0, 10), (x0 + 2, 6)], fill=GLASS[3])   # a diagonal glint
    rect(d, 3, 14, 12, 14, IRON[1]); rect(d, 4, 15, 11, 18, IRON[3]); d.line([(4, 15), (11, 15)], fill=IRON[4]); d.line([(3, 0), (12, 0)], fill=IRON[4])
    rect(d, 7, 6, 8, 9, IRON[4]); d.point((7, 6), fill=IRON[0])                                              # the push bar
    return im
def barred_single():
    im = canvas(16); d = ImageDraw.Draw(im)
    rect(d, 3, 0, 12, 18, hx("#08090a"))
    for x in (4, 6, 8, 10, 12 - 1): d.line([(x, 0), (x, 18)], fill=IRON[3]); d.line([(x - 1 if x > 4 else x, 0), (x - 1 if x > 4 else x, 18)], fill=IRON[1]) if False else None
    rect(d, 3, 0, 4, 18, IRON[2]); rect(d, 11, 0, 12, 18, IRON[1])                                         # the jambs
    for x in (5, 7, 9): d.line([(x, 1), (x, 18)], fill=IRON[3]); d.point((x, 0), fill=IRON[4])
    for y in (5, 13): d.line([(3, y), (12, y)], fill=IRON[2]); d.line([(3, y - 1), (12, y - 1)], fill=IRON[4])   # cross bars
    rect(d, 9, 9, 11, 11, YEL); d.line([(9, 9), (11, 9)], fill=hx("#e8c84a")); d.point((10, 10), fill=BLK)         # a padlock
    return im
def roller_single():
    im = canvas(16); d = ImageDraw.Draw(im); r = random.Random(3)
    rect(d, 1, 0, 14, 18, STEEL[2])
    for y in range(0, 17, 2): d.line([(1, y), (14, y)], fill=STEEL[3]); d.line([(1, y + 1), (14, y + 1)], fill=STEEL[1])        # slats
    rect(d, 1, 16, 14, 18, STEEL[1]); d.line([(1, 16), (14, 16)], fill=STEEL[4]); rect(d, 6, 17, 9, 17, STEEL[4])               # the bottom bar and its handle
    for _ in range(14): d.point((r.randint(2, 13), r.randint(0, 15)), fill=RUST[2])                                              # rust
    rect(d, 0, 0, 0, 18, STEEL[0]); rect(d, 15, 0, 15, 18, STEEL[0])
    return im
def cell_single():
    im = canvas(16); d = ImageDraw.Draw(im)
    rect(d, 3, 0, 12, 18, STEEL[2]); d.line([(3, 0), (12, 0)], fill=STEEL[4]); d.line([(3, 0), (3, 18)], fill=STEEL[3]); d.line([(12, 1), (12, 18)], fill=STEEL[0])
    rect(d, 5, 3, 10, 7, hx("#08090a")); [d.line([(x, 3), (x, 7)], fill=IRON[3]) for x in (6, 8)]                      # the barred window
    rect(d, 5, 11, 10, 11, hx("#08090a")); d.line([(5, 12), (10, 12)], fill=STEEL[4])                              # the food slot
    for (x, y) in ((4, 2), (11, 2), (4, 16), (11, 16), (4, 9), (11, 9)): d.point((x, y), fill=STEEL[4])             # rivets
    rect(d, 6, 14, 9, 15, STEEL[1]); d.point((7, 14), fill=STEEL[4])                                               # a number plate
    return im
def lab_single():
    im = canvas(16); d = ImageDraw.Draw(im)
    rect(d, 3, 0, 12, 18, LAB[3]); d.line([(3, 0), (12, 0)], fill=LAB[4]); d.line([(3, 0), (3, 18)], fill=LAB[4]); d.line([(12, 1), (12, 18)], fill=LAB[1])
    rect(d, 4, 2, 9, 8, GLASS[2]); d.line([(5, 7), (8, 3)], fill=GLASS[4]); d.rectangle([4, 2, 9, 8], outline=LAB[1])        # the porthole window
    rect(d, 10, 9, 11, 13, BLK); d.point((10, 10), fill=GREEN); d.point((11, 10), fill=GREEN); d.point((10, 12), fill=YEL); d.point((11, 12), fill=RED)   # keypad and lights
    d.line([(7, 10), (7, 14)], fill=LAB[1])                                                                         # the handle
    hazard(d, 3, 12, 17); hazard(d, 3, 12, 18)
    return im
def arched_single():
    im = canvas(16); d = ImageDraw.Draw(im)
    for (x0, x1, y) in ((5, 10, 0), (4, 11, 1), (3, 12, 2)): rect(d, x0, y, x1, y, CONC[4] if y == 0 else CONC[3])      # the arch's voussoir stones
    for y in range(3, 19): d.point((3, y), fill=CONC[3]); d.point((12, y), fill=CONC[1]); d.point((4, y), fill=CONC[2]); d.point((11, y), fill=CONC[0])   # the jambs
    rect(d, 5, 1, 10, 18, OAK[2]); rect(d, 4, 3, 11, 18, OAK[2]); [d.line([(x, 1 if x in range(5, 11) else 3), (x, 18)], fill=OAK[1]) for x in (6, 8, 10)]
    for (x, y) in ((5, 5), (5, 10), (5, 15), (10, 5), (10, 10), (10, 15)): d.point((x, y), fill=hx("#9a8a5a"))      # studs
    d.ellipse([8, 9, 10, 11], outline=IRON[3])                                                                         # a ring pull
    return im
def plank_single():
    im = canvas(16); d = ImageDraw.Draw(im); r = random.Random(5)
    rect(d, 3, 0, 12, 18, WOOD[1])
    for x in range(3, 13, 2): d.line([(x, 0), (x, 18)], fill=WOOD[3] if (x // 2) % 3 else WOOD[2]); d.line([(x + 1, 0), (x + 1, 18)], fill=WOOD[0])
    d.line([(3, 3), (12, 15)], fill=WOOD[4]); d.line([(3, 4), (12, 16)], fill=WOOD[0]); d.line([(12, 3), (3, 15)], fill=WOOD[4]); d.line([(12, 4), (3, 16)], fill=WOOD[0])     # the X brace
    rect(d, 10, 9, 11, 10, IRON[3]); d.rectangle([3, 0, 12, 18], outline=WOOD[0])
    return im

# ---------------------------------------------------------------- doubles (a 26 px canvas, x 3..28; the left tile takes columns 0..15, the right 16..31)
def _dbl(w=32): return Image.new("RGBA", (w, 19), (0, 0, 0, 0))
def gothic_double():
    im = _dbl(); d = ImageDraw.Draw(im)
    rect(d, 3, 6, 28, 18, GOTHS[4])                                                                                   # the stone reveal
    for y in range(0, 7):                                                                                            # the pointed arch: the opening narrows to the apex
        half = 13 * (1 - ((6 - y) / 6.0) ** 1.6); x0 = int(round(15.5 - half)); x1 = int(round(16 + half))
        rect(d, x0, y, x1, y, GOTHS[4] if y < 2 else GOTHS[3])
    rect(d, 5, 7, 26, 18, OAK[1])
    for y in range(0, 7):
        half = 11 * (1 - ((6 - y) / 6.0) ** 1.6); x0 = int(round(15.5 - half)); x1 = int(round(16 + half)); rect(d, x0 + 1, y + 1, x1 - 1, y + 1, OAK[2]) if y < 6 else None
    for x in range(6, 26, 3): d.line([(x, 1 if 11 < x < 21 else 7), (x, 18)], fill=OAK[3] if x < 16 else OAK[1]); d.line([(x + 1, 7), (x + 1, 18)], fill=OAK[0])
    d.line([(15, 1), (15, 18)], fill=OAK[0]); d.line([(16, 1), (16, 18)], fill=OAK[0])                                # the seam
    for y in (9, 14): rect(d, 5, y, 25, y + 1, IRON[2]); d.line([(5, y), (25, y)], fill=IRON[3])                         # iron straps with hinge ends
    for x in (6, 24): d.ellipse([x - 1, 8, x + 1, 10], fill=IRON[3]); d.ellipse([x - 1, 13, x + 1, 15], fill=IRON[3])
    for x in (13, 18): d.ellipse([x - 1, 11, x + 1, 13], outline=IRON[4])                                             # ring handles
    return im
def barn_double():
    im = _dbl(); d = ImageDraw.Draw(im)
    rect(d, 3, 0, 28, 18, WOOD[1])
    for x in range(3, 29, 2): d.line([(x, 0), (x, 18)], fill=WOOD[3] if (x // 2) % 4 else WOOD[2]); d.line([(x + 1, 0), (x + 1, 18)], fill=WOOD[0])
    for (x0, x1) in ((3, 15), (16, 28)):                                                                              # a big X on each leaf
        d.line([(x0, 1), (x1, 17)], fill=WOOD[4], width=1); d.line([(x0, 2), (x1, 18)], fill=WOOD[0]); d.line([(x1, 1), (x0, 17)], fill=WOOD[4]); d.line([(x1, 2), (x0, 18)], fill=WOOD[0])
        d.rectangle([x0, 0, x1, 18], outline=WOOD[0])
    rect(d, 12, 9, 19, 10, IRON[3]); d.line([(12, 9), (19, 9)], fill=IRON[4])                                         # the latch bar
    return im
def steel_double():
    im = _dbl(); d = ImageDraw.Draw(im)
    rect(d, 3, 0, 28, 18, STEEL[3]); d.line([(3, 0), (28, 0)], fill=STEEL[4])
    d.line([(15, 0), (15, 18)], fill=BLK); d.line([(16, 0), (16, 18)], fill=BLK)                                      # the sliding leaves' seam
    for (x0, x1) in ((4, 14), (17, 27)):
        for y in (4, 8, 12): d.line([(x0, y), (x1, y)], fill=STEEL[1]); d.line([(x0, y + 1), (x1, y + 1)], fill=STEEL[4])      # ribs
        rect(d, x0 + 2, 15, x1 - 2, 16, GLASS[2])                                                                  # a window slit
    hazard(d, 3, 28, 0); hazard(d, 3, 28, 1)
    d.point((14, 3), fill=GREEN); d.point((17, 3), fill=RED)
    return im
def glass_double():
    im = _dbl(); d = ImageDraw.Draw(im)
    rect(d, 3, 0, 28, 18, IRON[2]); d.line([(3, 0), (28, 0)], fill=IRON[4])
    for (x0, x1) in ((4, 14), (17, 27)):
        rect(d, x0, 1, x1, 13, GLASS[1]); d.line([(x0 + 1, 12), (x1 - 2, 3)], fill=GLASS[4]); d.line([(x0, 8), (x0 + 3, 4)], fill=GLASS[3]); rect(d, x0, 14, x1, 18, IRON[3]); d.line([(x0, 14), (x1, 14)], fill=IRON[4])
    d.line([(15, 0), (16, 18)], fill=BLK); rect(d, 13, 6, 14, 9, IRON[4]); rect(d, 17, 6, 18, 9, IRON[4])
    return im
def gate_double():
    im = _dbl(); d = ImageDraw.Draw(im)
    rect(d, 3, 0, 28, 18, hx("#08090a"))
    for x in range(4, 28, 2): d.line([(x, 3), (x, 18)], fill=IRON[3]); d.point((x, 2), fill=IRON[4]); d.point((x, 1), fill=IRON[4]) if x % 4 == 0 else None     # spear-topped bars
    for y in (5, 13): d.line([(3, y), (28, y)], fill=IRON[2]); d.line([(3, y - 1), (28, y - 1)], fill=IRON[4])
    d.ellipse([12, 6, 19, 12], outline=IRON[4]); rect(d, 14, 8, 17, 11, YEL)                                          # a scroll boss and a padlock
    rect(d, 3, 0, 4, 18, IRON[2]); rect(d, 27, 0, 28, 18, IRON[1]); rect(d, 3, 0, 28, 0, IRON[2])
    return im

def curtain_single():
    """a fitting-room curtain (2026-11-28, Yan: the cubicle doors are curtains): a wooden rail, a heavy faded-red curtain in folds, pulled a little aside so a dark slit shows the cubicle"""
    im = canvas(16); d = ImageDraw.Draw(im)
    CUR = ramp("#2a0d10", "#4a1a1e", "#6a262b", "#86363b", "#a24a4e")
    rect(d, 3, 0, 12, 18, CUR[1]); rect(d, 3, 0, 12, 1, OAK[3]); d.line([(3, 0), (12, 0)], fill=OAK[4])             # rail
    for x in range(4, 12):
        c = CUR[2 + (x % 3 == 0) - (x % 3 == 2)]; rect(d, x, 2, x, 18, c)                                       # folds
    rect(d, 7, 3, 8, 18, (12, 10, 12, 255)); d.line([(7, 3), (8, 3)], fill=CUR[0])                                # the slit onto the cubicle
    for x in (4, 11): d.point((x, 6), fill=OAK[4]); d.point((x, 7), fill=OAK[2])                                    # rings
    rect(d, 3, 17, 12, 18, CUR[0])                                                                               # hem in shadow
    return im
SINGLES = {"curtain": curtain_single, "glass": glass_single, "barred": barred_single, "roller": roller_single, "cell": cell_single, "lab": lab_single, "arched": arched_single, "plank": plank_single}
DOUBLES = {"gothic": gothic_double, "barn": barn_double, "steel": steel_double, "glass": glass_double, "gate": gate_double}

FACES = {}
for _n, _f in SINGLES.items(): FACES["x_" + _n] = (lambda f: lambda: f())(_f)
for _n, _f in DOUBLES.items():
    _k = _n + "d" if _n == "glass" else _n
    FACES["x_" + _k + "_l"] = (lambda f: lambda: f().crop((0, 0, 16, 19)))(_f); FACES["x_" + _k + "_r"] = (lambda f: lambda: f().crop((16, 0, 32, 19)))(_f)
def face(name):
    """the 16 x 19 door art for an atlas extra name (x_glass, x_gothic_l, ...) as an RGBA numpy array"""
    return np.array(FACES[name]())

if __name__ == "__main__":
    import walls
    def wall(mat, mask=10, extra=None):
        a = walls.build(mat, mask, 0, extra, 0); return Image.fromarray(np.array(a).astype(np.uint8), "RGBA") if hasattr(a, "shape") else a
    def seg(mat, door_tiles):
        n = len(door_tiles) + 2; W = 16 * n; base = Image.new("RGBA", (W, 56), (0, 0, 0, 0)); x = 0
        base.alpha_composite(wall(mat, 2), (0, 0)); x = 16                                                          # a wall end, then the door tiles, then a wall end
        for t in door_tiles:
            b = wall(mat, 10, "door"); b.alpha_composite(t, (0, 34)) if False else None
            base.alpha_composite(b, (x, 0)); base.alpha_composite(t, (x, 34)); x += 16
        base.alpha_composite(wall(mat, 8), (x, 0)); return base
    cells = []
    for nm, f in SINGLES.items():
        mat = {"glass": "plaster", "barred": "concrete", "roller": "concrete", "cell": "concrete", "lab": "concrete", "arched": "gothic", "plank": "wall"}[nm]
        cells.append((nm, seg(mat, [f()])))
    for nm, f in DOUBLES.items():
        mat = {"gothic": "gothic", "barn": "wall", "steel": "concrete", "glass": "plaster", "gate": "gothic"}[nm]; im = f()
        cells.append(("double " + nm, seg(mat, [im.crop((0, 0, 16, 19)), im.crop((16, 0, 32, 19))])))
    K_ = 5; pad = 10; colw = max(c[1].width for c in cells) * K_ + pad; cols = 4; rows = (len(cells) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * colw + pad, rows * (56 * K_ + 26) + pad), (36, 34, 40, 255)); dr = ImageDraw.Draw(sheet)
    for i, (nm, im) in enumerate(cells):
        x = pad + (i % cols) * colw; y = pad + (i // cols) * (56 * K_ + 26)
        sheet.alpha_composite(im.resize((im.width * K_, im.height * K_), Image.NEAREST), (x, y + 18)); dr.text((x, y), nm, fill=(235, 235, 235, 255))
    sheet.convert("RGB").save("/tmp/doors_new_preview.png"); print(sheet.size)
