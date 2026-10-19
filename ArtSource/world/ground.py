# Tileable 64x64 ground textures + small decals for the 3/4 world (Batch 30).
# Quiet, low-contrast, flat patches - the props carry the detail (STYLE_RESEARCH section 3).
import numpy as np
from PIL import Image, ImageDraw
from worldlib import *

S = 64

def base(pal, seed, cuts, cells=(16, 8, 4), weights=(0.55, 0.3, 0.15)):
    n = fbm(S, S, seed, cells, weights)
    n = (n - n.min()) / (n.max() - n.min() + 1e-6)
    return quantize(n, pal, cuts)

def speckle(im, seed, cols, dens):
    r = rng(seed); a = np.array(im)
    for _ in range(int(S * S * dens)):
        x, y = r.randrange(S), r.randrange(S); a[y, x] = r.choice(cols)
    return Image.fromarray(a, "RGBA")

def blades(im, seed, pal, n):
    r = rng(seed)
    for _ in range(n):
        x, y = r.randrange(S), r.randrange(S); c = r.choice(pal)
        px(im, x, y, c); px(im, x, (y - 1) % S, c)
        if r.random() < .4: px(im, (x + 1) % S, (y - 1) % S, c)
    return im

def grass(seed, pal=None):
    pal = pal or P["grass"]
    im = base(pal[:4], seed, [0.25, 0.55, 0.8])
    im = blades(im, seed + 1, [pal[3], pal[4]], 90)
    im = blades(im, seed + 2, [pal[0]], 50)
    return im

def darkgrass(seed):
    pal = P["darkgrass"]
    im = base(pal, seed, [0.3, 0.6, 0.85])
    im = blades(im, seed + 1, [pal[3], P["grass"][3]], 70)
    return speckle(im, seed + 3, [P["forest"][0]], 0.02)

def dirt(seed):
    pal = P["dirt"]
    im = base(pal, seed, [0.2, 0.5, 0.82], cells=(16, 8, 2), weights=(0.5, 0.3, 0.2))
    im = speckle(im, seed + 1, [pal[3], P["gravel"][3]], 0.02)
    return speckle(im, seed + 2, [pal[0]], 0.02)

def mud(seed):
    pal = P["mud"]
    im = base(pal, seed, [0.12, 0.45, 0.93], cells=(32, 16, 8), weights=(0.5, 0.35, 0.15))
    return speckle(im, seed + 2, [pal[3], pal[0]], 0.02)

def water(seed):
    pal = P["water"]
    im = base(pal[:3], seed, [0.15, 0.9], cells=(32, 16, 8))
    r = rng(seed + 5); d = ImageDraw.Draw(im)
    for _ in range(8):                                                             # ripple glints
        x, y = r.randrange(S), r.randrange(S); l = r.randint(2, 4)
        d.line([(x, y), (x + l, y)], fill=pal[3])
    return im

def asphalt(seed):
    pal = P["asphalt"]
    im = base(pal, seed, [0.25, 0.55, 0.85], cells=(16, 4, 2), weights=(0.4, 0.35, 0.25))
    im = speckle(im, seed + 1, [pal[3], P["gravel"][2]], 0.03)
    r = rng(seed + 7); d = ImageDraw.Draw(im)
    for _ in range(2):                                                              # hairline cracks
        x, y = r.randrange(S), r.randrange(S)
        for k in range(r.randint(5, 10)):
            nx, ny = x + r.choice((-1, 0, 1)), y + r.choice((0, 1))
            px(im, nx % S, ny % S, pal[0]); x, y = nx, ny
    return im

def concrete(seed, slab=32):
    pal = P["concrete"]
    im = base(pal[1:5], seed, [0.2, 0.55, 0.88], cells=(32, 8, 4))
    im = speckle(im, seed + 1, [pal[0], pal[4]], 0.02)
    d = ImageDraw.Draw(im)
    for k in range(0, S, slab):                                                     # slab joints
        d.line([(k, 0), (k, S - 1)], fill=pal[0]); d.line([(0, k), (S - 1, k)], fill=pal[0])
    return im

def sand(seed):
    pal = P["sand"]
    im = base(pal, seed, [0.25, 0.55, 0.85], cells=(32, 16, 4))
    return speckle(im, seed + 1, [pal[0], P["gravel"][3]], 0.015)

def gravel(seed):
    pal = P["gravel"]
    im = base(pal, seed, [0.3, 0.5, 0.75], cells=(8, 4, 2), weights=(0.3, 0.35, 0.35))
    return speckle(im, seed + 1, [pal[3], pal[0], P["rock"][4]], 0.06)

def cave(seed):
    pal = P["cave"]
    im = base(pal, seed, [0.3, 0.6, 0.85], cells=(16, 8, 4))
    return speckle(im, seed + 1, [pal[3], pal[0]], 0.03)

def floor(seed):
    pal = P["floor"]
    im = Image.new("RGBA", (S, S), pal[2]); d = ImageDraw.Draw(im); r = rng(seed)
    for y in range(0, S, 8):                                                        # worn planks
        d.line([(0, y + 7), (S - 1, y + 7)], fill=pal[0])
        off = (y * 13 + seed) % 24
        for x in range(off, S, 24): d.line([(x, y), (x, y + 6)], fill=pal[1])
        for x in range(S):
            if r.random() < .06: px(im, x, y + r.randrange(7), pal[3])
            if r.random() < .03: px(im, x, y + r.randrange(7), pal[1])
    return im

def lab(seed):
    pal = P["lab"]
    im = Image.new("RGBA", (S, S), pal[2]); d = ImageDraw.Draw(im); r = rng(seed)
    for k in range(0, S, 32):
        d.line([(k, 0), (k, S - 1)], fill=pal[1]); d.line([(0, k), (S - 1, k)], fill=pal[1])
    for _ in range(40): px(im, r.randrange(S), r.randrange(S), r.choice((pal[3], pal[1])))
    return im

GROUNDS = {
    "grass": grass, "darkgrass": darkgrass, "dirt": dirt, "mud": mud, "asphalt": asphalt, "concrete": concrete,
    "floor": floor, "labtile": lab, "sand": sand, "gravel": gravel, "water": water, "cavefloor": cave,
}

# ---------------- decals (walkable decoration, baked into the ground layer)
def tuft(seed, pal=None):
    pal = pal or P["grass"]; r = rng(seed); im = blank(8, 7)
    for k in range(r.randint(4, 6)):
        x = 1 + r.randrange(6); h = r.randint(3, 6)
        for y in range(h): px(im, x + (1 if y > h - 2 and r.random() < .5 else 0), 6 - y, pal[4] if y > h - 3 else pal[3] if y > 1 else pal[2])
    return outline(im, (*P["grass"][0][:3], 255))

def flowers(seed):
    r = rng(seed); im = tuft(seed + 3)
    for _ in range(r.randint(1, 3)):
        px(im, 1 + r.randrange(6), r.randrange(3), r.choice((hx("#c9b458"), hx("#d8d2c0"), hx("#a8606a"))))
    return im

def pebbles(seed):
    r = rng(seed); im = blank(8, 6)
    for _ in range(3):
        x, y = r.randrange(6), r.randrange(4)
        px(im, x, y, P["rock"][3]); px(im, x + 1, y, P["rock"][2]); px(im, x, y + 1, P["rock"][1])
    return im

def leaves(seed):
    r = rng(seed); im = blank(10, 8)
    for _ in range(7): px(im, r.randrange(10), r.randrange(8), r.choice((P["redleaf"][3], P["drygrass"][2], P["dirt"][3])))
    return im

def litter(seed):
    r = rng(seed); im = blank(8, 6)
    c = r.choice((hx("#8a8270"), hx("#6b5f4c"), P["tin"][2], P["rust"][2]))
    x, y = r.randrange(0, 5), r.randrange(0, 4)
    for dx in range(3): px(im, x + dx, y, c); px(im, x + dx, y + 1, tuple(int(v * .75) for v in c[:3]) + (255,))
    px(im, r.randrange(8), r.randrange(6), c)
    return im

def bones(seed):
    im = blank(10, 6); c = hx("#cfc6ae"); s = hx("#8f8774")
    for x in range(1, 8): px(im, x, 2, c)
    for p in ((0, 1), (0, 3), (8, 1), (8, 3)): px(im, *p, c)
    for x in range(2, 7): px(im, x, 3, s)
    disc(im, 6, 4, 1.5, c)
    return outline(im, (*s[:3], 255))

def puddle(seed):
    r = rng(seed); im = blank(14, 7)
    for y in range(7):
        for x in range(14):
            if ((x - 6.5) / 6.5) ** 2 + ((y - 3) / 3) ** 2 <= 1 - r.random() * 0.15: px(im, x, y, P["water"][2])
    for x in range(4, 8): px(im, x, 2, P["water"][3])
    return im

def crack(seed):
    r = rng(seed); im = blank(12, 8); x, y = 1, r.randrange(2, 6)
    for _ in range(11):
        px(im, x, y, P["asphalt"][0]); x += 1; y = max(0, min(7, y + r.choice((-1, 0, 0, 1))))
    return im

def oilstain(seed):
    r = rng(seed); im = blank(12, 7)
    for y in range(7):
        for x in range(12):
            if ((x - 5.5) / 5.5) ** 2 + ((y - 3) / 3) ** 2 <= 1 - r.random() * 0.3: px(im, x, y, (24, 24, 26, 120))
    return im

# Batch 92 (Yan: "make it dark, gritty, dirty, bloody"): lab grime. Old blood is brown-red, never bright; it reads on
# lab tile and concrete under the dark wash.
BLOOD = (hx("#3a0f0c"), hx("#55160f"), hx("#6e2219"), hx("#84301f"))

def blob(im, cx, cy, rx, ry, col, r, rough=0.3, a=255):
    w, h = im.size
    for y in range(h):
        for x in range(w):
            if ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1 - r.random() * rough: px(im, x, y, (*col[:3], a))

def blood(seed):                                   # a pool: dark dried rim, wetter middle, a lobe or two that ran off
    r = rng(seed); im = blank(18, 12)
    blob(im, 8.5, 6, 7.6, 4.6, BLOOD[0], r, 0.4)
    for _ in range(r.randint(1, 3)): blob(im, r.randint(3, 14), r.randint(3, 8), r.uniform(2, 3.5), r.uniform(1.5, 2.5), BLOOD[1], r, 0.35)
    blob(im, 8.5, 6, 5.2, 3.0, BLOOD[1], r, 0.35)
    blob(im, 8, 5.5, 3.2, 1.8, BLOOD[2], r, 0.3)
    for _ in range(5): px(im, r.randrange(18), r.randrange(12), BLOOD[0])
    px(im, 7, 5, BLOOD[3]); px(im, 8, 5, BLOOD[3])
    return im

def splat(seed):                                   # spatter: drops flung one way, a few big ones
    r = rng(seed); im = blank(16, 12); ang = r.uniform(0, 6.28)
    import math
    for _ in range(r.randint(12, 18)):
        d = r.uniform(0, 7); x = int(8 + math.cos(ang) * d + r.uniform(-2, 2)); y = int(6 + math.sin(ang) * d * 0.7 + r.uniform(-1.5, 1.5))
        px(im, x, y, r.choice(BLOOD[:3]))
        if r.random() < .3: px(im, x + 1, y, BLOOD[0])
    blob(im, 8, 6, 2.2, 1.6, BLOOD[1], r, 0.2)
    return im

def bloodtrail(seed):                              # a dragged smear: overlapping smudges and drips, reads in any direction
    r = rng(seed); im = blank(16, 14)
    for _ in range(r.randint(3, 5)):
        blob(im, r.uniform(4, 12), r.uniform(4, 10), r.uniform(1.5, 3.2), r.uniform(1.2, 2.4), r.choice(BLOOD[:2]), r, 0.45, r.choice((200, 230, 255)))
    for _ in range(r.randint(4, 8)): px(im, r.randrange(16), r.randrange(14), r.choice(BLOOD[:3]))
    return im

def grime(seed):                                   # a broad dark stain: soot, mould, old water
    r = rng(seed); im = blank(22, 14)
    blob(im, 10.5, 7, 10.5, 6.5, (28, 26, 20), r, 0.5, 80)
    blob(im, r.randint(6, 15), r.randint(4, 9), 5, 3, (22, 20, 15), r, 0.45, 100)
    for _ in range(r.randint(2, 4)): blob(im, r.randint(2, 19), r.randint(2, 11), 1.6, 1.2, (40, 44, 30), r, 0.3, 120)   # mould spots
    return im

def scorch(seed):                                  # burnt floor with a grey ash ring
    r = rng(seed); im = blank(14, 9)
    blob(im, 6.5, 4.5, 6.5, 4.2, (40, 38, 34), r, 0.3, 140)
    blob(im, 6.5, 4.5, 4.0, 2.6, (14, 13, 12), r, 0.25, 210)
    return im

def papers(seed):                                  # dropped sheets and a folder
    r = rng(seed); im = blank(12, 8)
    for _ in range(r.randint(2, 4)):
        x, y = r.randrange(9), r.randrange(6); c = r.choice((hx("#b9b3a0"), hx("#a39d88"), hx("#8f8a78")))
        for dx in range(3):
            for dy in range(2): px(im, x + dx, y + dy, c)
        px(im, x + 1, y, hx("#6b675a"))
    return im

def glass(seed):                                   # broken glass: pale glints
    r = rng(seed); im = blank(10, 7)
    for _ in range(r.randint(6, 9)): px(im, r.randrange(10), r.randrange(7), r.choice((hx("#a7c0c4"), hx("#7f9599"), hx("#d6e2e0"))))
    return im

DECALS = {"tuft": tuft, "flowers": flowers, "pebbles": pebbles, "leaves": leaves, "litter": litter, "bones": bones,
          "puddle": puddle, "crack": crack, "oil": oilstain,
          "blood": blood, "splat": splat, "bloodtrail": bloodtrail, "grime": grime, "scorch": scorch, "papers": papers, "glass": glass}
