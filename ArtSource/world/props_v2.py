# Props, second pass (Yan, 2026-11-09, quality pass: "tires, trash, jersey, brush, debris read weakest at game scale"). Same interface as props.py (Prop(im, pivot, shadow));
# NOT in the game atlas: `python3 props_v2.py <out.png>` writes a before/after on real ground tiles with a man for scale.
# Fixes: lifted the near-black rubber and bin-bag tones so the shapes read on dark ground, colour pops on the litter, a painted stripe on the barrier, a bigger fanned scrub, fatter rubble slabs.
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image, ImageDraw
from worldlib import *
import props
from props import Prop, finish

def line(im, x0, y0, x1, y1, c):
    n = max(abs(x1 - x0), abs(y1 - y0), 1)
    for i in range(n + 1): px(im, int(round(x0 + (x1 - x0) * i / n)), int(round(y0 + (y1 - y0) * i / n)), c)

def _tires0(seed):
    """dumped tyres: a lopsided stack of three (lit tops, zig-zag tread, dark undersides, the top one open with its hole) and one stood on edge with the steel wheel showing"""
    r = rng(seed); T = ramp("#242627", "#383a3c", "#4f5254", "#787c7e"); im = blank(24, 17)
    def band(cx, ytop, rx, h):
        for y in range(ytop, ytop + h):
            for x in range(int(cx - rx), int(cx + rx) + 1):
                e = abs(x - cx) / rx
                if e > 0.93 and y in (ytop, ytop + h - 1): continue
                v = 3 if y == ytop else 0 if y == ytop + h - 1 else (1 if (x + y) % 2 else 2)
                if e > 0.8 and v == 2: v = 1
                if x == int(cx - rx) and 0 < y - ytop < h - 1: v = 3
                px(im, x, y, T[v])
    for k, off in enumerate((1, -1, 0)): band(8 + off, 13 - k * 4, 7, 4)
    cx, cy, rx, ry = 8, 4.5, 7, 3.2                                       # top tyre seen from above: tread ring, hole
    for y in range(0, 10):
        for x in range(0, 17):
            e = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
            if e <= 1:
                if e < 0.2: px(im, x, y, hx("#0e1012"))
                elif e < 0.36: px(im, x, y, T[1])
                else: px(im, x, y, T[3] if (y < cy and x < cx + 2) else T[2] if e < 0.75 else T[1])
    px(im, 6, 4, T[2]); px(im, 7, 3, T[3])
    ex, ey = 18.5, 10.5                                                   # the one on edge
    for y in range(0, 17):
        for x in range(12, 24):
            e = ((x - ex) / 4.2) ** 2 + ((y - ey) / 6.0) ** 2
            if e <= 1:
                if e < 0.32: px(im, x, y, P["steel"][3] if e > 0.1 else P["steel"][1])
                else: px(im, x, y, T[3] if x < ex - 1.5 and y < ey else T[2] if x < ex + 1 else T[1])
    px(im, 18, 10, hx("#0e1012")); px(im, 19, 10, hx("#0e1012"))
    return finish(im, (11, 16), shadow=(22, 5, 1, -1))

def _trash0(seed):
    """dumped rubbish: two or three grey-sheened bin bags slumped together (tied necks, one split open), paper, a red can, a blue bottle, a green one"""
    r = rng(seed); im = blank(22, 14); B = ramp("#222529", "#363a40", "#555c64", "#848c94")
    bags = [(7, 7, 5, 4), (14, 8, 5.2, 3.8), (10, 10, 4.4, 3)][: r.randint(2, 3)]
    for (cx, cy, rx, ry) in bags:
        for y in range(int(cy - ry) - 2, int(cy + ry) + 2):
            for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
                e = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
                if e <= 1:
                    v = 3 if (e < 0.5 and x < cx and y < cy) else 2 if (x < cx + 1 and y < cy + 1) else 1 if y < cy + ry * 0.5 else 0
                    px(im, x, y, B[v])
        px(im, int(cx), int(cy - ry) - 1, B[1]); px(im, int(cx) + 1, int(cy - ry) - 2, B[2])   # the tied neck
    for _ in range(6): px(im, r.randrange(3, 19), r.randrange(8, 13), hx("#d8d2b8"))              # paper
    for x, y in ((3, 11), (4, 11), (3, 12), (4, 12)): px(im, x, y, hx("#b03c30"))                 # a can
    px(im, 3, 11, hx("#e07a60"))
    for x, y in ((18, 11), (19, 11), (20, 12)): px(im, x, y, hx("#3f78a8"))                       # a bottle
    px(im, 17, 12, hx("#5a9a58")); px(im, 16, 12, hx("#5a9a58"))
    return finish(im, (11, 13), shadow=(21, 5, 1, -1))

def _jersey0(seed):
    """a precast road barrier, 3/4: the lit top face, a short upright band with a faded yellow warning stripe, the sloped band, the foot with its lifting slot, rain streaks"""
    c = P["concrete"]; r = rng(seed); W = 22; im = blank(W, 14)
    for y in range(0, 3):                                                 # top face
        for x in range(3 + (2 - y) // 2 * 0, W - 3):
            px(im, x, y, c[4] if y < 2 else c[3])
    for y in range(3, 14):
        inset = 2 if y < 6 else 2 - (y - 5) // 2 if y < 10 else 0
        inset = max(0, inset)
        v = 3 if y < 6 else 2 if y < 8 else 1 if y < 10 else 2 if y < 13 else 0
        for x in range(inset, W - inset):
            px(im, x, y, c[v] if x > inset else c[min(4, v + 1)])
    for x in range(2, W - 2): px(im, x, 6, c[1])                          # crease between upright and slope
    for x in range(1, W - 1): px(im, x, 10, c[0] if x % 3 else c[1])      # crease above the foot
    y0 = 3                                                                # the warning stripe: diagonal yellow ticks on the upright band
    for x in range(4, 15):
        if (x // 2) % 2 == 0:
            for y in range(y0, y0 + 3): px(im, x + (y - y0) // 2 * 0, y, hx("#a58a3a") if (x + y) % 3 else hx("#c9a63e"))
    for x in range(8, 13): px(im, x, 11, c[0]); px(im, x, 12, c[0])      # the lifting slot
    for _ in range(4):
        x = r.randrange(3, W - 3)
        for y in range(7, r.randint(9, 11)): px(im, x, y, c[0])
    if seed % 2: px(im, W - 3, 0, (0, 0, 0, 0)); px(im, W - 4, 0, (0, 0, 0, 0)); px(im, W - 3, 1, (0, 0, 0, 0))
    for _ in range(3): px(im, r.randrange(2, W - 2), r.randrange(10, 13), P["moss"][0])
    return finish(im, (W // 2, 13), shadow=(W, 4, 1, -1))

def _brush0(seed):
    """a compact clump of dead scrub: woody stems fanned from one root, bleached straw tips, live green blades at the foot"""
    r = rng(seed); im = blank(22, 16); W = P["wood"]; D = P["drygrass"]
    for k in range(15):
        x0 = 7 + r.randrange(8); ang = (k - 7) / 7.0 * 0.7 + r.uniform(-0.12, 0.12); ln = r.randint(6, 11)
        x1 = x0 + ang * ln * 0.9; y1 = 15 - ln
        n = int(ln)
        for i in range(n + 1):
            t = i / n; x = x0 + (x1 - x0) * t; y = 15 - (15 - y1) * t
            px(im, int(round(x)), int(round(y)), W[2] if t < 0.35 else W[3] if t < 0.65 else D[2] if t < 0.9 else hx("#c4b684"))
        if r.random() < .5: px(im, int(round(x1)) + r.choice((-1, 1)), int(round(y1)) + 1, D[3])      # a side twig
    for _ in range(14): px(im, r.randrange(4, 18), r.randrange(12, 16), P["grass"][r.randint(2, 4)])
    return finish(im, (11, 15), shadow=(16, 4, 1, -1))

def _debris0(seed):
    """a heap of demolition rubble: fat tilted concrete slabs (lit top edge, shaded face, a broken corner), rebar sticking out rusty and bent, a snapped plank, rubble and pale dust at the foot"""
    r = rng(seed); c = P["concrete"]; im = blank(26, 16); R = P["rust"]
    for _ in range(18):
        x, y = r.randrange(1, 25), r.randrange(11, 16)
        px(im, x, y, r.choice((c[2], c[3], c[4], c[1])))
    slabs = [(1, 7, 11, 6, 1), (9, 4, 10, 6, -1), (14, 8, 10, 5, 1), (5, 9, 8, 5, 0)]
    d = ImageDraw.Draw(im)
    for (x0, y0, w, th, tilt) in slabs:
        top = [(x0, y0 + tilt * 2), (x0 + w, y0 - tilt * 2), (x0 + w, y0 - tilt * 2 + 2), (x0, y0 + tilt * 2 + 2)]
        d.polygon(top, fill=c[4])
        for i in range(w):
            yy = y0 + tilt * 2 - (tilt * 4 * i) // w + 2
            for j in range(th - 2): px(im, x0 + i, yy + j, c[2] if j < th - 3 else c[1])
            px(im, x0 + i, yy + th - 2, c[0])
        px(im, x0 + r.randrange(2, w - 2), y0 + 3, c[0])                                              # a pit
        px(im, x0, y0 + tilt * 2 + 3, (0, 0, 0, 0)); px(im, x0 + w - 1, y0 - tilt * 2 + 3, (0, 0, 0, 0))   # chipped corners
    for k in range(3):
        x, y = r.randrange(4, 21), r.randrange(4, 8)
        for i in range(r.randint(5, 8)): px(im, x + (i // 3) * r.choice((1, -1)), y - i + 3, R[1 + (i % 2)] if i < 6 else R[3])
    for i in range(8): px(im, 16 + i, 11 - i // 3, P["wood"][3] if i < 7 else P["wood"][2]); px(im, 16 + i, 12 - i // 3, P["wood"][1])
    for _ in range(6): px(im, r.randrange(2, 24), r.randrange(12, 15), hx("#a8a496"))                 # dust
    return finish(im, (13, 15), shadow=(24, 5, 1, -1))

# ---------------------------------------------------------------- variants (Yan: "draw at least 3 to 5 variants of each, otherwise the world will look all the same")
# The game picks a variant by tile hash from however many p_<name>_<k> the atlas holds (WorldArt.Pick), so each prop just needs VARIANTS[name] distinct builders. Variant k = (seed - 1000) // 77.
def _v(seed): return (seed - 1000) // 77

def _T(): return ramp("#242627", "#383a3c", "#4f5254", "#787c7e")
def _tband(im, T, cx, ytop, rx, h):
    for y in range(ytop, ytop + h):
        for x in range(int(cx - rx), int(cx + rx) + 1):
            e = abs(x - cx) / rx
            if e > 0.93 and y in (ytop, ytop + h - 1): continue
            v = 3 if y == ytop else 0 if y == ytop + h - 1 else (1 if (x + y) % 2 else 2)
            if e > 0.8 and v == 2: v = 1
            if x == int(cx - rx) and 0 < y - ytop < h - 1: v = 3
            px(im, x, y, T[v])
def _ttop(im, T, cx, cy, rx, ry, hole=0.2):
    for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
        for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
            e = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
            if e <= 1:
                if e < hole: px(im, x, y, hx("#0e1012"))
                elif e < hole * 1.8: px(im, x, y, T[1])
                else: px(im, x, y, T[3] if (y < cy and x < cx + 2) else T[2] if e < 0.75 else T[1])
def _tedge(im, T, ex, ey, rx, ry, lugs=False):
    for y in range(int(ey - ry) - 1, int(ey + ry) + 2):
        for x in range(int(ex - rx) - 1, int(ex + rx) + 2):
            e = ((x - ex) / rx) ** 2 + ((y - ey) / ry) ** 2
            if e <= 1:
                if e < 0.32: px(im, x, y, P["steel"][3] if e > 0.1 else P["steel"][1])
                else: px(im, x, y, T[3] if x < ex - 1.5 and y < ey else T[2] if x < ex + 1 else T[1])
    px(im, int(ex), int(ey), hx("#0e1012"))
    if lugs:
        for dx, dy in ((0, -2), (2, -1), (2, 1), (0, 2), (-2, 1), (-2, -1)): px(im, int(ex) + dx, int(ey) + dy, P["steel"][4])
def _tires1(seed):                                   # a low heap: two flat tyres side by side and a third lying across them
    T = _T(); im = blank(26, 15)
    for cx in (7, 18): _tband(im, T, cx, 9, 6.5, 4)
    for cx in (7, 18): _ttop(im, T, cx, 8.5, 6.5, 2.6, 0.18)
    _tband(im, T, 12.5, 6, 6, 4); _ttop(im, T, 12.5, 5.5, 6, 2.6, 0.2)
    return finish(im, (13, 14), shadow=(24, 5, 1, -1))
def _tires2(seed):                                   # two flat tyres, the top one slid off to the side, mud on the lower one
    T = _T(); im = blank(24, 15)
    _tband(im, T, 9, 9, 7, 4); _ttop(im, T, 9, 8.5, 7, 2.8, 0.2)
    _tband(im, T, 15, 6, 6, 4); _ttop(im, T, 15, 5.5, 6, 2.6, 0.2)
    for x in range(3, 14, 3): px(im, x, 12, hx("#3a3226"))
    return finish(im, (12, 14), shadow=(22, 5, 1, -1))
def _tires3(seed):                                   # a tall, tidy stack of five, the top one open
    T = _T(); im = blank(16, 24)
    for k in range(4): _tband(im, T, 8 + (k % 2) * 0.5, 20 - k * 3, 6.5, 3)
    _tband(im, T, 8, 8, 6.5, 3); _ttop(im, T, 8, 7, 6.5, 2.8, 0.2)
    return finish(im, (8, 23), shadow=(14, 4, 1, -1))
def _tring_rot(im, T, cx, cy, rx, ry, ang, hole=0.22):
    ca, sa = math.cos(math.radians(ang)), math.sin(math.radians(ang))
    for y in range(int(cy - rx - ry) - 1, int(cy + rx + ry) + 2):
        for x in range(int(cx - rx - ry) - 1, int(cx + rx + ry) + 2):
            u = ((x - cx) * ca + (y - cy) * sa) / rx; w = (-(x - cx) * sa + (y - cy) * ca) / ry
            e = u * u + w * w
            if e <= 1:
                if e < hole: px(im, x, y, hx("#0e1012"))
                elif e < hole * 1.9: px(im, x, y, T[1])
                else: px(im, x, y, T[3] if (u < -0.15 and w < 0.1) else T[2] if e < 0.7 else T[1])
def _tires4(seed):                                   # one big old tyre lying flat, weeds and a dead stalk growing up through its hole, dirt splashed on the tread
    r = rng(seed); T = _T(); im = blank(22, 13)
    _tband(im, T, 11, 6, 9, 5); _ttop(im, T, 11, 5.5, 9, 4.2, 0.26)
    for x, y in ((9, 5), (10, 4), (11, 5), (12, 4), (13, 5), (10, 6), (12, 6)): px(im, x, y, P["grass"][r.randint(2, 4)])
    for y in range(1, 5): px(im, 11, y, P["drygrass"][2])
    px(im, 11, 0, hx("#7a5a3a"))
    for _ in range(9): px(im, r.randrange(3, 19), r.randrange(7, 11), hx("#3a3226"))
    for x in range(6, 17, 3): px(im, x, 10, T[0])
    return finish(im, (11, 12), shadow=(20, 4, 1, -1))
TIRES = [_tires0, _tires1, _tires2, _tires3, _tires4]

def _bag(im, B, cx, cy, rx, ry):
    for y in range(int(cy - ry) - 2, int(cy + ry) + 2):
        for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
            e = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
            if e <= 1: px(im, x, y, B[3 if (e < 0.5 and x < cx and y < cy) else 2 if (x < cx + 1 and y < cy + 1) else 1 if y < cy + ry * 0.5 else 0])
    px(im, int(cx), int(cy - ry) - 1, B[1]); px(im, int(cx) + 1, int(cy - ry) - 2, B[2])
def _litter(im, r, n=6):
    for _ in range(n): px(im, r.randrange(2, im.width - 2), r.randrange(im.height - 5, im.height - 1), hx("#d8d2b8"))
    for x, y in ((3, im.height - 3), (4, im.height - 3), (3, im.height - 2), (4, im.height - 2)): px(im, x, y, hx("#b03c30"))
    px(im, 3, im.height - 3, hx("#e07a60")); px(im, im.width - 4, im.height - 2, hx("#3f78a8")); px(im, im.width - 5, im.height - 2, hx("#3f78a8")); px(im, im.width - 6, im.height - 2, hx("#5a9a58"))
def _trash1(seed):                                   # one big split bag, the rubbish spilling out of it
    r = rng(seed); im = blank(22, 14); B = ramp("#222529", "#363a40", "#555c64", "#848c94")
    _bag(im, B, 9, 6, 6.5, 4.5)
    for x in range(8, 13): px(im, x, 5, hx("#0d0e10")); px(im, x, 6, hx("#0d0e10"))               # the tear
    for x in range(10, 18): px(im, x, 8 + (x % 3 == 0), hx("#8a7a58") if x % 2 else hx("#5a4a34"))   # spilled food rubbish
    for _ in range(9): px(im, r.randrange(8, 20), r.randrange(8, 12), r.choice((hx("#d8d2b8"), hx("#b8b090"), hx("#6a8a5a"), hx("#3f78a8"))))
    _litter(im, r, 3)
    return finish(im, (11, 13), shadow=(20, 5, 1, -1))
def _trash2(seed):                                   # cardboard boxes, one split, soggy at the foot
    r = rng(seed); im = blank(22, 15); C = ramp("#4e4030", "#725f44", "#92795a", "#b09470")
    for (x0, y0, w, h) in ((2, 6, 10, 8), (11, 8, 9, 6), (5, 2, 8, 5)):
        for y in range(y0, y0 + h):
            for x in range(x0, x0 + w):
                top = y < y0 + 3; px(im, x, y, C[3] if top and y == y0 else C[2] if top else C[1] if x > x0 + w - 3 else C[2] if y < y0 + h - 2 else C[0])
        for x in range(x0 + w // 2 - 1, x0 + w // 2 + 1):
            for y in range(y0, y0 + h - 1): px(im, x, y, hx("#c8b27a"))                              # packing tape
    for x in range(12, 20): px(im, x, 8, C[3]); px(im, x, 9, C[0])                                   # a flap
    for _ in range(6): px(im, r.randrange(2, 20), r.randrange(12, 15), hx("#6a5a44"))
    _litter(im, r, 3)
    return finish(im, (11, 14), shadow=(21, 5, 1, -1))
def _trash3(seed):                                   # scattered litter: bottles, tins, paper, a flattened bag; no big heap
    r = rng(seed); im = blank(22, 9)
    for _ in range(11): px(im, r.randrange(1, 21), r.randrange(2, 8), r.choice((hx("#d8d2b8"), hx("#b8b090"), hx("#e8e4d0"))))
    for k, (x, y, col) in enumerate(((3, 5, "#5a9a58"), (9, 3, "#8a5a2a"), (15, 6, "#3f78a8"), (19, 3, "#5a9a58"))):
        px(im, x, y, hx(col)); px(im, x + 1, y, hx(col)); px(im, x + 2, y, hx("#b8b8b0")) if k % 2 == 0 else px(im, x + 1, y + 1, hx(col))
    for x, y in ((6, 6), (7, 6), (6, 7), (7, 7)): px(im, x, y, hx("#b03c30"))
    px(im, 6, 6, hx("#e07a60")); px(im, 12, 7, hx("#a8a8a0")); px(im, 13, 7, hx("#a8a8a0")); px(im, 12, 6, hx("#d0d0c8"))
    for x in range(14, 19): px(im, x, 7, hx("#3a3d42")); px(im, x - 1, 8, hx("#2c2e32"))                # a flattened bag
    return finish(im, (11, 8), shadow=(18, 3, 1, -1))
def _trash4(seed):                                   # a stained old mattress with a spring poking out
    r = rng(seed); im = blank(24, 14)
    for y in range(0, 5):
        for x in range(2 + (4 - y) // 2, 22 - (4 - y) // 2): px(im, x, y, hx("#a8a89a") if (x // 2) % 2 == 0 else hx("#7a8088"))
    for y in range(5, 11):
        for x in range(2, 22): px(im, x, y, hx("#5a5e66") if y < 9 else hx("#34373c")); 
        for x in range(2, 22, 4): px(im, x, y, hx("#6e747c") if y < 9 else hx("#34373c"))
    for _ in range(14): px(im, r.randrange(3, 21), r.randrange(0, 9), hx("#6a5238"))               # stains
    for k in range(5): px(im, 15 + k // 2, 2 - k % 2, hx("#9a9a94")); px(im, 16, 1 - k // 3, hx("#c8c8c0"))   # the spring
    return finish(im, (12, 12), shadow=(22, 4, 1, -1))
TRASH = [_trash0, _trash1, _trash2, _trash3, _trash4]

def _jersey_base(seed, paint=None, broken=False, mossy=False, tag=False):
    c = P["concrete"]; r = rng(seed); W = 22; im = blank(W, 14)
    def seg(x_lo, x_hi, drop=0):
        for y in range(0, 3):
            for x in range(x_lo + 3, x_hi - 3): px(im, x, y + drop, c[4] if y < 2 else c[3])
        for y in range(3, 14 - drop):
            inset = 2 if y < 6 else max(0, 2 - (y - 5) // 2) if y < 10 else 0
            v = 3 if y < 6 else 2 if y < 8 else 1 if y < 10 else 2 if y < 13 else 0
            for x in range(x_lo + inset, x_hi - inset): px(im, x, y + drop, c[v] if x > x_lo + inset else c[min(4, v + 1)])
        for x in range(x_lo + 2, x_hi - 2): px(im, x, 6 + drop, c[1])
        for x in range(x_lo + 1, x_hi - 1): px(im, x, 10 + drop, c[0] if x % 3 else c[1])
    if broken: seg(0, 11, 0); seg(11, W, 3)
    else: seg(0, W)
    if paint == "chev":
        for x in range(3, W - 3):
            for y in range(3, 6): px(im, x, y, hx("#c9a63e") if (x + y) % 6 < 3 else hx("#2a2a28"))
    elif paint == "redwhite":
        for x in range(3, W - 3):
            for y in range(3, 6): px(im, x, y, hx("#b8402e") if ((x + y) // 3) % 2 == 0 else hx("#d8d4c4"))
    elif paint == "stripe":
        for x in range(4, 15):
            if (x // 2) % 2 == 0:
                for y in range(3, 6): px(im, x, y, hx("#a58a3a") if (x + y) % 3 else hx("#c9a63e"))
    for x in range(8, 13): px(im, x, 11, c[0]); px(im, x, 12, c[0])
    for _ in range(4):
        x = r.randrange(3, W - 3)
        for y in range(7, r.randint(9, 11)): px(im, x, y, c[0])
    if mossy:
        for _ in range(26): px(im, r.randrange(2, W - 2), r.randrange(4, 13), P["moss"][r.randint(0, 1)])
        for x in range(4, 18, 5):
            for y in range(7, 11): px(im, x, y, P["rust"][1])
    if tag:
        for k in range(5): px(im, 14 + k, 4 + k % 3, hx("#d86a1e")); px(im, 18 - k, 4 + k % 3, hx("#d86a1e")) if k < 3 else None
    if broken:
        for k in range(5): px(im, 9 + k % 2, 11 - k, P["rust"][1 + k % 3])                          # rebar from the break
    return finish(im, (W // 2, 13), shadow=(W, 4, 1, -1))
def _jersey1(seed): return _jersey_base(seed, "chev")
def _jersey2(seed): return _jersey_base(seed, "redwhite")
def _jersey3(seed): return _jersey_base(seed, None, broken=True)
def _jersey4(seed): return _jersey_base(seed, None, mossy=True, tag=True)
JERSEY = [_jersey0, _jersey1, _jersey2, _jersey3, _jersey4]

def _brush1(seed):                                   # tall dead stalks with seed heads
    r = rng(seed); im = blank(20, 20); W = P["wood"]; D = P["drygrass"]
    for k in range(13):
        x = 3 + r.randrange(14); h = r.randint(8, 18); lean = r.choice((-1, 0, 0, 1))
        for y in range(h):
            xx = x + (lean * y) // 5; px(im, xx, 19 - y, W[2] if y < h // 2 else D[2] if y < h - 2 else D[3])
        px(im, x + (lean * h) // 5, 19 - h, hx("#7a5a3a")); px(im, x + (lean * h) // 5, 18 - h, hx("#7a5a3a"))
    for _ in range(8): px(im, r.randrange(3, 17), r.randrange(17, 20), P["grass"][r.randint(2, 4)])
    return finish(im, (10, 19), shadow=(14, 4, 1, -1))
def _brush2(seed):                                   # a tumbleweed: a round tangle of pale twigs
    r = rng(seed); im = blank(16, 14); D = P["drygrass"]
    for k in range(18):
        a = r.uniform(0, 3.1416); rr = r.uniform(4, 5.6)
        for t in range(-10, 11):
            x = 8 + (t / 10) * rr * math.cos(a); y = 7 + (t / 10) * rr * math.sin(a)
            px(im, int(round(x)), int(round(y)), D[1 + (k % 3)])
    for _ in range(10): px(im, r.randrange(4, 13), r.randrange(3, 12), hx("#c4b684"))
    return finish(im, (8, 12), shadow=(12, 4, 1, -1))
def _brush3(seed):                                   # a bramble: a dark green mound with thorny canes and a few red berries
    r = rng(seed); im = blank(22, 15); G = P["foliage"]
    for y in range(15):
        for x in range(22):
            e = ((x - 11) / 9.5) ** 2 + ((y - 8) / 6) ** 2 + r.uniform(-0.12, 0.12)
            if e <= 1 and y > 2: px(im, x, y, G[3] if (y < 7 and x < 11 and e < .8) else G[2] if y < 10 else G[0] if y > 12 else G[1])
    for k in range(6):
        x0 = 4 + r.randrange(14); x1 = x0 + r.choice((-6, -4, 4, 6)); line(im, x0, 8, x1, 1 + r.randrange(3), P["bark"][2])
        px(im, x1, 0 if False else 1, P["bark"][3])
    for _ in range(5): px(im, r.randrange(4, 18), r.randrange(4, 11), hx("#a8342a"))
    return finish(im, (11, 14), shadow=(18, 4, 1, -1))
def _brush4(seed):                                   # a fallen dead branch, a few sticks crossed, dry leaves caught in it
    r = rng(seed); im = blank(24, 13); W = P["wood"]
    line(im, 2, 9, 20, 4, W[2]); line(im, 2, 10, 20, 5, W[1]); line(im, 20, 4, 23, 2, W[3])
    line(im, 5, 3, 17, 11, W[2]); line(im, 6, 3, 18, 11, W[1]); line(im, 10, 11, 21, 8, W[2])
    for x0, y0, dx, dy in ((9, 7, 3, -3), (14, 6, 2, -4), (7, 8, -3, -2)): line(im, x0, y0, x0 + dx, y0 + dy, W[3])
    for _ in range(9): px(im, r.randrange(4, 20), r.randrange(4, 12), r.choice((P["drygrass"][2], hx("#8a6a3a"), P["redleaf"][3])))
    return finish(im, (12, 12), shadow=(20, 4, 1, -1))
BRUSH = [_brush0, _brush1, _brush2, _brush3, _brush4]

def _slab(im, c, x0, y0, w, th, tilt, r):
    d = ImageDraw.Draw(im)
    d.polygon([(x0, y0 + tilt * 2), (x0 + w, y0 - tilt * 2), (x0 + w, y0 - tilt * 2 + 2), (x0, y0 + tilt * 2 + 2)], fill=c[4])
    for i in range(w):
        yy = y0 + tilt * 2 - (tilt * 4 * i) // w + 2
        for j in range(th - 2): px(im, x0 + i, yy + j, c[2] if j < th - 3 else c[1])
        px(im, x0 + i, yy + th - 2, c[0])
    px(im, x0 + r.randrange(2, max(3, w - 2)), y0 + 3, c[0])
def _debris1(seed):                                  # a low spread of flat broken slabs and gravel
    r = rng(seed); c = P["concrete"]; im = blank(26, 11)
    for _ in range(30): px(im, r.randrange(1, 25), r.randrange(6, 11), r.choice((c[1], c[2], c[3], c[4])))
    for (x0, y0, w, th, tilt) in ((1, 3, 10, 5, 0), (11, 4, 9, 5, 1), (6, 5, 8, 4, -1), (17, 2, 8, 5, 0)): _slab(im, c, x0, y0, w, th, tilt, r)
    for _ in range(5): px(im, r.randrange(2, 24), r.randrange(3, 9), P["rust"][2])
    return finish(im, (13, 10), shadow=(24, 4, 1, -1))
def _debris2(seed):                                  # a crumbling brick wall stub with loose bricks round it
    r = rng(seed); B = P["brick"]; c = P["concrete"]; im = blank(24, 17)
    for y in range(2, 14):
        lo = 4 + (y < 6) * (3 + (y % 2)) ; hi = 19 - (y < 8) * (2 + r.randrange(2))
        for x in range(lo, hi):
            course = (y // 2) % 2; mortar = (y % 2 == 1) or ((x + course * 2) % 4 == 0)
            px(im, x, y, c[2] if mortar else B[2 + (x * 3 + y) % 3 // 2] if y > 3 else B[3])
    for x in range(lo, hi): pass
    for _ in range(9): bx, by = r.randrange(1, 22), r.randrange(13, 17); px(im, bx, by, B[3]); px(im, bx + 1, by, B[2])
    for _ in range(14): px(im, r.randrange(1, 23), r.randrange(13, 17), r.choice((c[2], c[3], hx("#a8a496"))))
    return finish(im, (12, 16), shadow=(22, 5, 1, -1))
def _debris3(seed):                                  # one big tilted slab leaning on rubble, a bundle of rebar fanned out of it
    r = rng(seed); c = P["concrete"]; R = P["rust"]; im = blank(24, 17)
    for _ in range(16): px(im, r.randrange(1, 23), r.randrange(12, 17), r.choice((c[1], c[2], c[3], hx("#a8a496"))))
    _slab(im, c, 3, 7, 16, 6, 1, r); _slab(im, c, 1, 11, 9, 4, -1, r)
    for k in range(7): line(im, 17, 6, 17 + k - 2, 0 + (k % 3), R[1 + k % 3])
    return finish(im, (12, 16), shadow=(22, 5, 1, -1))
def _debris4(seed):                                  # collapsed timber: planks and a beam crossed, nails, white dust
    r = rng(seed); W = P["wood"]; im = blank(26, 14)
    for (x0, y0, x1, y1) in ((1, 10, 20, 5), (3, 6, 23, 11), (6, 12, 24, 9), (2, 4, 14, 12)):
        for off, v in ((0, 3), (1, 2), (2, 1)): line(im, x0, y0 + off, x1, y1 + off, W[v])
    for _ in range(8): px(im, r.randrange(2, 24), r.randrange(4, 12), W[4])
    for _ in range(5): px(im, r.randrange(2, 24), r.randrange(4, 12), hx("#9a9a94"))
    for _ in range(16): px(im, r.randrange(1, 25), r.randrange(10, 14), hx("#c4c0b0"))
    return finish(im, (13, 13), shadow=(24, 4, 1, -1))
DEBRIS = [_debris0, _debris1, _debris2, _debris3, _debris4]

def tires2(seed): return TIRES[_v(seed) % 5](seed)
def trash2(seed): return TRASH[_v(seed) % 5](seed)
def jersey2(seed): return JERSEY[_v(seed) % 5](seed)
def brush2(seed): return BRUSH[_v(seed) % 5](seed)
def debris2(seed): return DEBRIS[_v(seed) % 5](seed)
V2_VARIANTS = {"tires": 5, "trash": 5, "jersey": 5, "brush": 5, "debris": 5, "jersey_v": 5, "jersey_c": 5, "block": 5, "block_v": 5, "block_c": 5, "scrap": 5, "fountain": 5, "hatch": 5, "pillar": 5, "culvert": 5, "pipe": 5, "pipepost": 5, "rockfall": 3, "rockfall_on": 3, "irongate": 2, "irongate_on": 2, "sealslab": 2, "sealslab_on": 2}


def _jv(seed, kind):
    """a north-south road barrier, 3/4 from the south: nine px wide so it reads as a barrier and not a post. Top face 3 px (lit), a sloped shoulder either side, a flared foot, a dark base line; a joint across the
    tile (a gap you can see into) and lifting slots in the shoulders; the end face at the south is the barrier's profile (narrow top, step, wide foot) with its own paint stripe."""
    c = P["concrete"]; r = rng(seed); W, T, Hf = 9, 16, 8; im = blank(W, T + Hf)
    for y in range(T):                                                                                  # foot strips either side, a dark seam, then the raised top block (lit left, shaded right)
        for x in range(W):
            v = c[3] if x == 0 else c[2] if x == 8 else c[0] if x in (1, 7) else c[4] if x in (2, 3) else c[3] if x in (4, 5) else c[2]
            px(im, x, y, v)
    for x in range(W): px(im, x, 15, c[0]); px(im, x, 0, c[4] if 2 <= x <= 5 else c[3] if x == 0 else c[2] if x == 8 else c[0])   # the joint between tiles, lit lip after it
    for x in range(2, 7): px(im, x, 14, c[1])
    for y in (7, 8): px(im, 0, y, c[0]); px(im, 8, y, c[0])                                               # lifting slots in the feet
    for y in range(T, T + Hf):                                                                         # the end face: the barrier's profile
        inset = 3 if y < T + 2 else 2 if y < T + 3 else 1 if y < T + 5 else 0
        for x in range(inset, W - inset):
            lit = x == inset
            px(im, x, y, c[0] if y == T + Hf - 1 else c[4] if (y < T + 2 and lit) else c[3] if y < T + 2 else c[3] if lit else c[2] if x < W - inset - 1 else c[1])
    for _ in range(3): px(im, r.randrange(W), r.randrange(1, T - 2), c[1])
    if kind == 0:                                                         # yellow ticks
        for y in (4, 11): px(im, 2, y, hx("#c9a63e")); px(im, 3, y, hx("#a58a3a")); px(im, 5, y, hx("#c9a63e"))
        for x in range(3, 6): px(im, x, T + 3, hx("#c9a63e") if x % 2 else hx("#a58a3a"))
    elif kind == 1:                                                       # yellow-black chevrons on the top and shoulders
        for y in list(range(2, 6)) + list(range(10, 13)):
            for x in range(2, 7): px(im, x, y, hx("#c9a63e") if (y + x) % 4 < 2 else hx("#2a2a28"))
        for x in range(2, 7): px(im, x, T + 3, hx("#c9a63e") if x % 2 else hx("#2a2a28"))
    elif kind == 2:                                                       # red and white
        for y in list(range(2, 6)) + list(range(10, 13)):
            for x in range(2, 7): px(im, x, y, hx("#b8402e") if ((y + x) // 2) % 2 == 0 else hx("#d8d4c4"))
        for x in range(2, 7): px(im, x, T + 3, hx("#b8402e") if x % 2 else hx("#d8d4c4"))
    elif kind == 3:                                                       # broken through: a gap across the tile, rebar and rubble in it
        for y in range(6, 11):
            for x in range(W): px(im, x, y, (0, 0, 0, 0))
        for k in range(5): px(im, 2 + k % 4, 5 - k // 3, P["rust"][1 + k % 3]); px(im, 3 + k % 3, 11 + k // 4, P["rust"][1 + k % 3])
        for _ in range(9): px(im, r.randrange(1, W - 1), r.randrange(7, 10), r.choice((c[1], c[2], c[3])))
    else:                                                                 # moss, rust streaks and an orange tag
        for _ in range(22): px(im, r.randrange(W), r.randrange(1, 14), P["moss"][r.randint(0, 1)])
        for y in (3, 4, 10, 11): px(im, 6, y, P["rust"][1])
        px(im, 3, 12, hx("#d86a1e")); px(im, 4, 11, hx("#d86a1e")); px(im, 5, 12, hx("#d86a1e")); px(im, 4, 13, hx("#d86a1e"))
    return finish(im, (W // 2, T + Hf - 1), shadow=(13, 30, 3, -10))
def jersey_v2(seed): return _jv(seed, _v(seed) % 5)
def jersey_c2(seed): return props.corner(jersey2, jersey_v2, seed)


# ---------------------------------------------------------------- blocks: precast foundation blocks (props.block): a boxy slab with a lit top, formwork lines on the face, steel lifting loops
def _loop(im, x, y):
    S = P["steel"]; px(im, x, y, S[3]); px(im, x + 1, y, S[3]); px(im, x - 1, y + 1, S[2]); px(im, x + 2, y + 1, S[1]); px(im, x, y + 1, S[0]); px(im, x + 1, y + 1, S[0])
def _slab2(im, c, x0, y0, w, th=3, fh=7, chamfer=True, seams=True):
    for y in range(th):
        for x in range(w):
            if chamfer and y == 0 and x in (0, w - 1): continue
            px(im, x0 + x, y0 + y, c[4] if y == 0 else c[3] if y < th - 1 else c[2])
    for y in range(th, th + fh):
        for x in range(w):
            v = c[3] if x == 0 else c[0] if y == th + fh - 1 else c[1] if x == w - 1 else c[2]
            if seams and (y - th) % 3 == 2 and 0 < x < w - 1 and y < th + fh - 1: v = c[1]
            px(im, x0 + x, y0 + y, v)
def _blk0(seed):
    r = rng(seed); c = P["concrete"]; im = blank(24, 15)
    _slab2(im, c, 0, 6, 23); _slab2(im, c, 3 + seed % 2, 0, 20)
    for lx in (7 + seed % 2, 18 + seed % 2): _loop(im, lx, 0)
    for _ in range(4): px(im, r.randrange(1, 22), r.randrange(4, 14), c[1])
    return finish(im, (12, 14), shadow=(23, 5, 1, -1))
def _blk1(seed):                                     # one big block, stencilled with a white number
    r = rng(seed); c = P["concrete"]; im = blank(24, 12)
    _slab2(im, c, 0, 1, 24, 3, 7)
    for lx in (4, 18): _loop(im, lx, 0)
    for x0 in (8, 10, 14, 16):                       # a painted stock mark: two pairs of strokes
        for y in range(6, 9): px(im, x0, y, hx("#d8d4c4"))
    px(im, 12, 7, hx("#d8d4c4"))
    return finish(im, (12, 11), shadow=(23, 5, 1, -1))
def _blk2(seed):                                     # two blocks side by side, one a little lower and set back
    r = rng(seed); c = P["concrete"]; im = blank(28, 13)
    _slab2(im, c, 0, 2, 13, 3, 7); _slab2(im, c, 14, 0, 14, 3, 9)
    _loop(im, 4, 2); _loop(im, 20, 0)
    for _ in range(5): px(im, r.randrange(1, 27), r.randrange(5, 12), c[1])
    return finish(im, (14, 12), shadow=(26, 5, 1, -1))
def _blk3(seed):                                     # cracked, one corner broken off with the rebar mesh showing, rubble beside it
    r = rng(seed); c = P["concrete"]; R = P["rust"]; im = blank(24, 13)
    _slab2(im, c, 0, 1, 22, 3, 8)
    for k in range(9): px(im, 13 + k // 2 - k % 2, 3 + k, c[0])                                # a crack
    for y in range(1, 6):
        for x in range(16, 22): px(im, x, y, (0, 0, 0, 0))                                   # the broken corner
    for x in range(16, 22, 2):
        for y in range(1, 4): px(im, x, y, R[1 + y % 3])
    for _ in range(8): px(im, r.randrange(15, 24), r.randrange(8, 13), r.choice((c[1], c[2], c[3])))
    _loop(im, 3, 1)
    return finish(im, (11, 12), shadow=(22, 5, 1, -1))
def _blk4(seed):                                     # mossy and rusted, weeds at the foot, one loop bent flat
    r = rng(seed); c = P["concrete"]; im = blank(24, 14)
    _slab2(im, c, 0, 2, 23, 3, 8)
    for _ in range(26): px(im, r.randrange(1, 22), r.randrange(2, 13), P["moss"][r.randint(0, 1)])
    for x in (5, 12, 18):
        for y in range(6, r.randint(9, 12)): px(im, x, y, P["rust"][1])
    _loop(im, 6, 1); px(im, 17, 2, P["steel"][2]); px(im, 18, 2, P["steel"][1]); px(im, 19, 2, P["steel"][2])
    for x in range(1, 22, 3): px(im, x, 12, P["grass"][r.randint(2, 4)]); px(im, x, 11, P["grass"][3])
    return finish(im, (11, 13), shadow=(23, 5, 1, -1))
BLOCK = [_blk0, _blk1, _blk2, _blk3, _blk4]
def block2(seed): return BLOCK[_v(seed) % 5](seed)

def _bv(seed, kind):
    """foundation blocks laid north-south, to match block2: the lit top with steel lifting loops, a joint with a visible gap between blocks, formwork lines, the end face (a boxy slab face) at the south"""
    c = P["concrete"]; r = rng(seed); W, T, Hf = 9, 16, 8; im = blank(W, T + Hf)
    for y in range(T):
        for x in range(W): px(im, x, y, c[3] if x == 0 else c[4] if x in (1, 2, 3, 4, 5) else c[3] if x == 6 else c[2] if x == 7 else c[1])
    for x in range(W): px(im, x, 15, c[0]); px(im, x, 0, c[4] if 0 < x < 7 else c[3])
    for y in (3, 11): px(im, 3, y, P["steel"][3]); px(im, 4, y, P["steel"][3]); px(im, 3, y + 1, P["steel"][0]); px(im, 4, y + 1, P["steel"][0])
    for y in range(T, T + Hf):
        for x in range(W): px(im, x, y, c[0] if y == T + Hf - 1 else c[3] if x == 0 else c[1] if x == W - 1 else c[1] if (y - T) % 3 == 2 else c[2])
    for _ in range(3): px(im, r.randrange(W), r.randrange(1, T - 2), c[2])
    if kind == 1:
        for x0 in (2, 4, 6): [px(im, x0, T + 2 + dy, hx("#d8d4c4")) for dy in range(3)]
    elif kind == 2:
        for y in range(5, 11):
            for x in range(W): px(im, x, y, (0, 0, 0, 0)) if (y + x) % 5 == 0 else None
        for x in range(1, 7): px(im, x, 15, c[0])
        for x in range(0, W, 2): px(im, x, 5 + (x % 3), c[1])
    elif kind == 3:
        for y in range(6, 10):
            for x in range(W): px(im, x, y, (0, 0, 0, 0))
        for k in range(5): px(im, 1 + k, 5 - k % 2, P["rust"][1 + k % 3]); px(im, 2 + k, 10, P["rust"][1 + k % 3])
        for _ in range(8): px(im, r.randrange(W), r.randrange(6, 10), r.choice((c[1], c[2], c[3])))
    elif kind == 4:
        for _ in range(24): px(im, r.randrange(W), r.randrange(1, 14), P["moss"][r.randint(0, 1)])
        for y in (4, 5, 11): px(im, 6, y, P["rust"][1])
    return finish(im, (W // 2, T + Hf - 1), shadow=(13, 30, 3, -10))
def block_v2(seed): return _bv(seed, _v(seed) % 5)
def block_c2(seed): return props.corner(block2, block_v2, seed)


# ---------------------------------------------------------------- scrap: five heaps that read as different things
def _scrap0(seed): return props.OLD_PROPS["scrap"](seed)
def _scrap1(seed):                                   # corrugated tin sheets stacked and leaning, rust at the edges
    r = rng(seed); T = P["tin"]; R = P["rust"]; im = blank(26, 15)
    for k, (x0, w, lean) in enumerate(((1, 11, 3), (6, 12, 2), (11, 12, 1), (15, 10, 0))):
        for i in range(w):
            for j in range(10):
                x = x0 + i + (lean * (9 - j)) // 5; y = 2 + j + k // 2
                v = 3 if j == 0 else (1 if (x // 2) % 2 else 2) if j < 9 else 0
                if r.random() < 0.05 and j < 6: v = None
                if v is not None: px(im, x, y, (R[1 + (i + j) % 3] if (i < 2 or j > 7) and r.random() < 0.5 else T[v]))
    for _ in range(6): px(im, r.randrange(2, 24), r.randrange(3, 12), P["steel"][4])
    return finish(im, (13, 14), shadow=(24, 5, 1, -1))
def _scrap2(seed):                                   # car parts: a wheel with its hub, a door panel with a window hole, a bumper bar
    r = rng(seed); S = P["steel"]; R = P["rust"]; T = _T(); im = blank(28, 16)
    for y in range(3, 12):                                                # the door
        for x in range(1, 15): px(im, x, y, R[3] if y == 3 else R[2] if (x + y) % 7 else R[1])
    for y in range(5, 8):
        for x in range(4, 11): px(im, x, y, hx("#1a1d20") if y > 5 else S[2])
    for x in range(1, 15): px(im, x, 11, R[0])
    _tring_rot(im, T, 21, 9, 5.5, 5.5, 0, 0.12)                           # the wheel
    for y in range(3, 16):
        for x in range(15, 28):
            if ((x - 21) ** 2 + (y - 9) ** 2) ** 0.5 < 2.6: px(im, x, y, S[3] if ((x - 21) ** 2 + (y - 9) ** 2) ** 0.5 > 1.0 else S[1])
    line(im, 2, 14, 17, 12, S[3]); line(im, 2, 15, 17, 13, S[1])         # a bumper bar
    for _ in range(5): px(im, r.randrange(2, 26), r.randrange(4, 15), S[4])
    return finish(im, (14, 15), shadow=(26, 5, 1, -1))
def _scrap3(seed):                                   # a stack of steel pipes of different lengths, the round ends showing
    r = rng(seed); S = P["steel"]; R = P["rust"]; im = blank(26, 14)
    for k, (y0, x0, ln) in enumerate(((8, 1, 22), (5, 3, 20), (8, 2, 18), (2, 4, 17))):
        for y in range(3):
            for x in range(ln): px(im, x0 + x, y0 + y, S[4] if y == 0 else S[3] if y == 1 else S[1])
        ex = x0 + ln
        for dy in range(-1, 4):
            for dx in range(0, 3):
                if (dy, dx) in ((-1, 2), (3, 2)): continue
                px(im, ex + dx - 1, y0 + dy, S[3] if dy == 0 else hx("#0e1012") if (dy, dx) in ((1, 1),) else S[2])
        for _ in range(3): px(im, x0 + r.randrange(ln), y0 + r.randrange(3), R[1 + r.randrange(3)])
    return finish(im, (13, 13), shadow=(24, 5, 1, -1))
def _scrap4(seed):                                   # crushed oil drums, one on its side, a coil of chain and a length of angle iron
    r = rng(seed); R = P["rust"]; S = P["steel"]; B = ramp("#1c2a3a", "#2a3e54", "#3e5a78", "#5a7a98"); im = blank(26, 15)
    for (cx, cy, pal) in ((7, 8, R), (17, 9, B)):
        for y in range(cy - 6, cy + 5):
            for x in range(cx - 4, cx + 5):
                e = ((x - cx) / 4.2) ** 2 + ((y - cy + 1) / 5.4) ** 2 + ((x * 7 + y * 3) % 5 - 2) * 0.02
                if e <= 1: px(im, x, y, pal[3] if (y < cy - 3) else pal[2] if x < cx else pal[1] if y < cy + 3 else pal[0])
        for y in (cy - 1, cy + 2): [px(im, x, y, S[2]) for x in range(cx - 4, cx + 5) if ((x - cx) / 4.2) ** 2 < 0.95]      # the bands
        px(im, cx - 1, cy - 4, S[4]); px(im, cx, cy - 4, S[3])
    for k in range(7): px(im, 11 + k % 4, 12 + (k % 2), S[3]); px(im, 12 + k % 3, 13, S[1])
    line(im, 1, 13, 10, 14, S[3]); line(im, 1, 14, 10, 14, S[1])
    return finish(im, (13, 14), shadow=(24, 5, 1, -1))
SCRAP = [_scrap0, _scrap1, _scrap2, _scrap3, _scrap4]
def scrap2(seed): return SCRAP[_v(seed) % 5](seed)


# ---------------------------------------------------------------- the rest of the "reads as nothing" list: fountain, hatch, pillar, culvert, pipe and pipepost (five variants each)
def _ell(im, cx, cy, rx, ry, f):
    for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
        for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
            e = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
            if e <= 1:
                v = f(x, y, e)
                if v is not None: px(im, x, y, v)
def _fountain(seed, kind):
    """a dry town-square fountain, 3 m across: a round rim, the basin, and (new) a centre pedestal with a bowl so it stops reading as an eye"""
    c = P["concrete"]; r = rng(seed); W, H = 34, 23; im = blank(W, H); cx, cy, rx, ry = 16.5, 11, 16, 8.5
    wat = ramp("#243230", "#2f423e", "#3f5a52", "#6a8a80") if kind == 1 else ramp("#27301f", "#34412a", "#465636", "#5f7048")
    _ell(im, cx, cy, rx, ry, lambda x, y, e: c[4] if y < cy - 1 else c[3] if e > 0.86 else c[3])                       # the rim's top, lit at the back
    _ell(im, cx, cy, rx - 2.2, ry - 1.8, lambda x, y, e: (wat[1] if y < cy else wat[2]) if kind == 1 else (c[1] if y < cy else c[2]))   # basin: water or dry floor
    for x in range(W):                                                                                                    # the rim's front face
        for y in range(H):
            if im.getpixel((x, y))[3] and y > cy and (y + 1 >= H or not im.getpixel((x, y + 1))[3]):
                for k in range(1, 4):
                    if y + k < H: px(im, x, y + k, c[2] if k < 3 else c[0])
    if kind == 3:                                                                                                         # filled with rubble and rubbish
        for _ in range(70): 
            x, y = int(cx + r.uniform(-12, 12)), int(cy + r.uniform(-5, 5))
            if ((x - cx) / 13.5) ** 2 + ((y - cy) / 6.4) ** 2 < 1: px(im, x, y, r.choice((c[1], c[3], c[4], P["rust"][2], hx("#d8d2b8"), P["wood"][2])))
    elif kind == 4:                                                                                                       # overgrown: grass in the basin and a sapling
        for _ in range(90):
            x, y = int(cx + r.uniform(-12, 12)), int(cy + r.uniform(-5, 5))
            if ((x - cx) / 13.5) ** 2 + ((y - cy) / 6.4) ** 2 < 1: px(im, x, y, P["grass"][r.randint(1, 4)])
        for y in range(2, 11): px(im, 17, y, P["bark"][2]); px(im, 18 if y < 5 else 17, y, P["bark"][1])
        _ell(im, 17, 4, 4.2, 3.2, lambda x, y, e: P["foliage"][3] if e < 0.5 else P["foliage"][2])
    else:
        top = 2 if kind == 2 else -4
        for y in range(int(cy) - 6 if kind != 2 else int(cy) - 2, int(cy) + 1):                                            # the pedestal
            for x in range(int(cx) - 1, int(cx) + 2): px(im, x, y, c[4] if x == int(cx) - 1 else c[3] if x == int(cx) else c[2])
        if kind != 2: _ell(im, cx, cy - 6, 4.8, 1.9, lambda x, y, e: c[4] if y < cy - 6 else c[3])                         # the bowl on top
        else:
            for _ in range(12): px(im, int(cx + r.uniform(-6, 7)), int(cy + r.uniform(0, 4)), r.choice((c[3], c[4], c[2])))
            for y in range(int(cy - 3), int(cy)): px(im, int(cx) + 3, y, c[0])                                              # a crack and the missing piece of rim
            for y in range(int(cy) + 3, int(cy) + 7):
                for x in range(26, 31): px(im, x, y, (0, 0, 0, 0))
        if kind == 1:
            for _ in range(14): px(im, int(cx + r.uniform(-11, 11)), int(cy + r.uniform(-4, 5)), wat[3])
            for x, y in ((8, 12), (24, 13), (11, 15)): px(im, x, y, P["grass"][3]); px(im, x, y - 1, P["grass"][2])
        else:
            for _ in range(10): px(im, int(cx + r.uniform(-11, 11)), int(cy + r.uniform(-4, 6)), P["drygrass"][1])        # dead leaves
    return finish(im, (W // 2, H - 3), shadow=(32, 6, 1, -1))
def fountain2(seed): return _fountain(seed, _v(seed) % 5)

def _hatch0(seed): return props.OLD_PROPS["hatch"](seed)
def _hatch1(seed):                                   # a round steel manhole cover on a concrete pad, shoved half aside
    c = P["concrete"]; S = P["steel"]; im = blank(20, 17)
    for y in range(5, 15):
        for x in range(1, 19): px(im, x, y, c[4] if y == 5 else c[3] if y < 7 else c[2] if y < 13 else c[0])
    _ell(im, 10, 8, 7, 3.2, lambda x, y, e: hx("#0b0c0d") if e < 0.85 else S[1])
    _ell(im, 12.5, 7, 7, 3.2, lambda x, y, e: S[4] if (y < 7 and e > 0.2) else S[3] if e > 0.75 else S[2])
    for k in range(-5, 6, 2): px(im, 12 + k, 7 + (k % 3 == 0), S[1])
    return finish(im, (10, 14), shadow=(18, 4, 1, -1))
def _hatch2(seed):                                   # a rusted hatch box chained shut with a yellow padlock
    c = P["concrete"]; S = P["steel"]; R = P["rust"]; im = blank(20, 17); r = rng(seed)
    for y in range(9, 15):
        for x in range(1, 19): px(im, x, y, c[4] if y == 9 else c[3] if y < 11 else c[1] if y < 14 else c[0])
    for y in range(3, 11):
        for x in range(3, 17): px(im, x, y, R[3] if y == 3 else R[2] if y < 7 else R[1] if y < 9 else R[0])
    for _ in range(8): px(im, r.randrange(3, 17), r.randrange(4, 10), S[2])
    for x in range(3, 17): px(im, x, 7, S[1])
    for k in range(4): px(im, 9 + k % 2, 7 + k, S[3]); px(im, 11 + k % 2, 7 + k, S[2])         # a chain hanging down
    px(im, 10, 11, hx("#c9a63e")); px(im, 11, 11, hx("#c9a63e")); px(im, 10, 12, hx("#a58a3a")); px(im, 11, 12, hx("#a58a3a"))
    return finish(im, (10, 15), shadow=(18, 4, 1, -1))
def _hatch3(seed):                                   # a wooden trapdoor in the ground: planks, an iron ring, a rim of dirt
    r = rng(seed); W_ = P["wood"]; D = P["dirt"]; im = blank(20, 15)
    for y in range(1, 13):
        for x in range(1, 19): px(im, x, y, D[2] if (y < 3 or y > 10 or x < 3 or x > 16) else W_[3] if (x - 3) % 4 == 0 else W_[2] if y < 4 else W_[1])
    for x in range(3, 17): px(im, x, 3, W_[4]); px(im, x, 10, W_[0])
    for x in (4, 9, 14): [px(im, x, y, W_[0]) for y in range(4, 10)]
    for x, y in ((9, 7), (10, 7), (9, 8), (10, 8)): px(im, x, y, P["steel"][3] if (x + y) % 2 else P["steel"][1])
    px(im, 9, 7, hx("#0e1012")); px(im, 10, 8, hx("#0e1012"))
    for _ in range(10): px(im, r.randrange(1, 19), r.randrange(0, 14), D[3])
    return finish(im, (10, 13), shadow=(18, 3, 1, -1))
def _hatch4(seed):                                   # an open bunker hatch: the lid flipped up, a ladder going down into the dark
    c = P["concrete"]; S = P["steel"]; g = (hx("#2f4436"), hx("#3f5848"), hx("#4f6b5a"), hx("#6a8a74")); im = blank(20, 19)
    for y in range(8, 17):
        for x in range(1, 19): px(im, x, y, c[4] if y == 8 else c[3] if y < 10 else c[1] if y < 15 else c[0])
    for y in range(9, 14):
        for x in range(5, 15): px(im, x, y, hx("#0b0c0d"))
    for y in (10, 12): [px(im, x, y, S[3]) for x in range(7, 13)]
    px(im, 7, 11, S[2]); px(im, 12, 11, S[2])
    for y in range(1, 8):                                                              # the lid standing open on its hinges
        for x in range(4, 16): px(im, x, y, g[3] if y == 1 else g[2] if y < 4 else g[1] if y < 6 else g[0])
    px(im, 9, 4, S[4]); px(im, 10, 4, S[3])
    return finish(im, (10, 16), shadow=(18, 4, 1, -1))
HATCH = [_hatch0, _hatch1, _hatch2, _hatch3, _hatch4]
def hatch2(seed): return HATCH[_v(seed) % 5](seed)

def _pillar0(seed): return props.OLD_PROPS["pillar"](seed)
def _pillar1(seed):                                  # two columns, a snapped beam lying across their tops
    c = P["concrete"]; R = P["rust"]; r = rng(seed); im = blank(28, 30)
    for x0 in (3, 18):
        for x in range(x0, x0 + 6):
            for y in range(7, 27): px(im, x, y, c[4] if x == x0 else c[3] if x < x0 + 3 else c[2] if x < x0 + 5 else c[1])
            px(im, x, 7, c[4])
    for y in range(3, 8):
        for x in range(1, 27):
            if y == 7 and 11 < x < 16: continue
            px(im, x, y, c[4] if y == 3 else c[3] if y < 6 else c[1])
    for k in range(4): px(im, 12 + k % 3, 8 + k // 2, R[1 + k % 3]); px(im, 14 - k % 2, 5 + k, c[0])
    for x in (2, 9, 10, 20, 24, 25): px(im, x, 27, c[2]); px(im, x, 28, c[1])
    return finish(im, (14, 28), shadow=(26, 5, 1, -1), tall=True)
def _pillar2(seed):                                  # an intact column with a square capital and base, cracked
    c = P["concrete"]; r = rng(seed); im = blank(16, 30)
    for y in range(2, 5):
        for x in range(1, 13): px(im, x, y, c[4] if y == 2 else c[3] if y == 3 else c[1])           # capital
    for y in range(5, 25):
        for x in range(3, 11): px(im, x, y, c[4] if x == 3 else c[3] if x < 6 else c[2] if x < 10 else c[1])
    for y in range(25, 28):
        for x in range(1, 13): px(im, x, y, c[3] if y == 25 else c[2] if y == 26 else c[0])         # base
    for k in range(9): px(im, 6 + (k % 3) - (k // 3), 8 + k, c[0])                                   # a crack
    for _ in range(4): px(im, r.randrange(3, 11), r.randrange(6, 24), c[1])
    return finish(im, (7, 28), shadow=(13, 5, 1, -1), tall=True)
def _pillar3(seed):                                  # a short broken stump in a heap of its own rubble, rebar out of the top
    c = P["concrete"]; R = P["rust"]; r = rng(seed); im = blank(20, 20)
    for x in range(5, 13):
        top = (10, 9, 11, 10, 8, 10, 11, 12)[x - 5]
        for y in range(top, 17): px(im, x, y, c[4] if x == 5 else c[3] if x < 8 else c[2] if x < 12 else c[1])
        px(im, x, top, c[4])
    for x, ln in ((6, 4), (10, 3), (8, 2)): [px(im, x + (k > 1), 10 - k, R[1 + k % 3]) for k in range(ln)]
    for _ in range(30): px(im, r.randrange(1, 19), r.randrange(13, 19), r.choice((c[1], c[2], c[3], c[4])))
    return finish(im, (10, 18), shadow=(18, 5, 1, -1), tall=True)
def _pillar4(seed):                                  # a mossy column with a snapped top, vines and rust streaks, leaning a little
    c = P["concrete"]; R = P["rust"]; r = rng(seed); im = blank(16, 30)
    for y in range(5, 27):
        sh = (27 - y) // 9
        for x in range(3 + sh, 10 + sh): px(im, x, y, c[3] if x < 6 + sh else c[2] if x < 9 + sh else c[1])
    for x in range(4, 9): px(im, x + 1, 5 + x % 3, c[4])
    for x, ln in ((5, 4), (8, 3)): [px(im, x + 1, 5 - k, R[1 + k % 3]) for k in range(ln)]
    for _ in range(40): px(im, r.randrange(3, 14), r.randrange(6, 26), P["moss"][r.randint(0, 1)])
    for y in range(8, 24, 2): px(im, 6 + (y % 4 == 0), y, P["foliage"][3])
    for y in (9, 10, 17, 18): px(im, 9, y, R[1])
    return finish(im, (7, 28), shadow=(13, 5, 1, -1), tall=True)
PILLAR = [_pillar0, _pillar1, _pillar2, _pillar3, _pillar4]
def pillar2(seed): return PILLAR[_v(seed) % 5](seed)

def _culvert0(seed): return props.OLD_PROPS["culvert"](seed)
def _bank(im, w, rows=4):
    for y in range(0, rows):
        for x in range(1, w - 1): px(im, x, y, P["grass"][2] if (x + y) % 4 else P["grass"][1])
def _culvert1(seed):                                 # a concrete box culvert with iron bars across the mouth
    c = P["concrete"]; S = P["steel"]; im = blank(24, 18); _bank(im, 24)
    for y in range(4, 15):
        for x in range(2, 22): px(im, x, y, c[4] if y == 4 else c[2] if x > 2 else c[3])
    for y in range(6, 14):
        for x in range(6, 18): px(im, x, y, hx("#0b0c0d"))
    for x in range(7, 17, 2): [px(im, x, y, S[3]) for y in range(6, 14)]
    for y in range(13, 18):
        for x in range(7, 17):
            if abs(x - 12) <= (y - 12) + 1: px(im, x, y, P["water"][1] if abs(x - 12) < 2 else P["mud"][1])
    return finish(im, (12, 16), shadow=(22, 3, 1, -1))
def _culvert2(seed):                                 # a rusty corrugated-steel pipe poking out of the bank, water trickling
    R = P["rust"]; S = P["steel"]; im = blank(24, 18); _bank(im, 24, 5)
    for y in range(4, 15):
        for x in range(3, 21): px(im, x, y, R[2] if (x // 2) % 2 else R[1])
    _ell(im, 12, 10, 6.4, 5.4, lambda x, y, e: hx("#0b0c0d") if e < 0.62 else R[3] if (y < 10 and x < 12) else (S[2] if e < 0.8 else R[1]))
    for y in range(13, 18):
        for x in range(8, 17):
            if abs(x - 12) <= (y - 12): px(im, x, y, P["water"][2] if abs(x - 12) < 2 else P["mud"][1])
    return finish(im, (12, 16), shadow=(22, 3, 1, -1))
def _culvert3(seed):                                 # an old stone arch drain, mossy, with a dark mouth
    c = P["rock"]; M = P["moss"]; im = blank(24, 18); r = rng(seed); _bank(im, 24)
    for y in range(4, 15):
        for x in range(2, 22):
            px(im, x, y, c[3] if y == 4 else c[2] if (x + y // 2) % 4 else c[1])
    for _ in range(26): px(im, r.randrange(2, 22), r.randrange(4, 15), M[r.randint(0, 1)])
    _ell(im, 12, 11, 5.6, 6.2, lambda x, y, e: hx("#0b0c0d") if (y <= 14 and e < 0.78) else c[4] if e < 1 and y < 11 else None)
    for k in range(7): px(im, 7 + k * 2 // 1 % 11, 4 + (k % 2), c[0])
    for y in range(13, 18):
        for x in range(9, 15):
            if abs(x - 12) <= (y - 12): px(im, x, y, P["water"][1] if abs(x - 12) < 2 else P["mud"][1])
    return finish(im, (12, 16), shadow=(22, 3, 1, -1))
def _culvert4(seed):                                 # a blocked culvert: branches, a tyre and reeds jammed in the mouth, a dark puddle spreading
    c = P["concrete"]; im = blank(24, 18); r = rng(seed); _bank(im, 24)
    for y in range(4, 15):
        for x in range(2, 22): px(im, x, y, c[4] if y == 4 else c[2] if x > 2 else c[3])
    disc(im, 12, 10, 5, c[3]); disc(im, 12, 10, 4, hx("#0b0c0d"))
    for _ in range(14): line(im, r.randrange(7, 17), r.randrange(7, 13), r.randrange(7, 17), r.randrange(7, 13), P["wood"][r.randint(1, 3)])
    for x in (8, 10, 14, 16): [px(im, x, y, P["drygrass"][2]) for y in range(8, 13)]
    for y in range(14, 18):
        for x in range(4, 20):
            if ((x - 12) / 8) ** 2 + ((y - 15.5) / 2.3) ** 2 < 1: px(im, x, y, P["water"][0] if (x + y) % 5 else P["water"][1])
    return finish(im, (12, 16), shadow=(22, 3, 1, -1))
CULVERT = [_culvert0, _culvert1, _culvert2, _culvert3, _culvert4]
def culvert2(seed): return CULVERT[_v(seed) % 5](seed)

PIPE_FOIL = {0: ("#5f6264", "#7d8082", "#9a9d9e", "#b9bbbb"), 1: ("#3a2418", "#58341f", "#77482e", "#93603b"), 2: ("#3a4a50", "#52666e", "#6a8590", "#8aa6b0"),
             3: ("#6a561c", "#8a7226", "#b09434", "#d0b248"), 4: ("#2c3a2a", "#3c503a", "#4f6a4c", "#668a60")}
def _heat(seed, post, kind):
    """one tile of the overhead heating line. A run is ONE pipe, so every variant keeps the foil look (a first version changed material tile by tile and the line turned into a patchwork); the five variants differ in
    the details a long pipe really has: plain, foil torn back to rusty steel, a mended patch, a small red valve wheel on top, moss and dirt on top. Supports: plain T, braced, rust-stained and cracked, with a cable clamp, mossy. A shadow on the ground says it is overhead."""
    r = rng(seed); h = 13; im = blank(16, h + 9); col = ramp(*PIPE_FOIL[0]); R = P["rust"]; S = P["steel"]; v = _v(seed) % 5
    for k, y0 in enumerate((1, 6)):
        for x in range(16):
            for j in range(4):
                vv = (3 if j == 0 else 2 if j == 1 else 1 if j == 2 else 0) - k
                c = col[max(0, vv)]
                if (x + 3 * k) % 8 == 0 and j < 3: c = col[max(0, vv - 1)]
                px(im, x, y0 + j, c)
        n = 2 if v == 1 else 0 if v in (0, 2) else 1
        for _ in range(n):
            x = r.randrange(1, 13)
            for dx in range(r.randint(2, 4)):
                if x + dx < 16: px(im, x + dx, y0 + 1, R[2]); px(im, x + dx, y0 + 2, R[1])
    if v == 2:                                           # a mended patch: a darker wrapped band across both pipes
        for k, y0 in enumerate((1, 6)):
            for x in range(5, 9):
                for j in range(4): px(im, x, y0 + j, hx("#4a4d50") if j > 0 and (x + j) % 2 else hx("#5a5d60"))
    if v == 3:                                           # a small red valve wheel on the upper pipe
        for x in range(6, 10): px(im, x, 0, hx("#a52c22") if x in (6, 9) else hx("#c84a32"))
        px(im, 7, 1, S[2]); px(im, 8, 1, S[2])
    if v == 4:
        for _ in range(8): px(im, r.randrange(16), 1 + (0 if r.random() < .6 else 5), P["moss"][r.randint(0, 1)])
    if post:
        cn = P["concrete"]
        for x in range(3, 13): px(im, x, 9, cn[3]); px(im, x, 10, cn[1])
        for y in range(11, h + 9):
            for k in range(3): px(im, 6 + k, y, cn[3 - k])
        if v == 1:
            for k in range(5): px(im, 5 - k // 2, 12 + k, cn[2]); px(im, 10 + k // 2, 12 + k, cn[1])
        elif v == 2:
            for y in range(12, 20): px(im, 7, y, cn[0]) if y % 3 else px(im, 8, y, cn[0])
            for y in (12, 13, 17): px(im, 6, y, R[1])
        elif v == 3:
            for x in range(5, 11): px(im, x, 12, S[2])
            px(im, 7, 13, S[1]); px(im, 8, 13, S[1])
        elif v == 4:
            for _ in range(10): px(im, r.randrange(6, 9), r.randrange(12, h + 8), P["moss"][r.randint(0, 1)])
    import numpy as np
    a = np.array(im)
    for x in range(16):
        for y in range(16):
            if a[y, x, 3] == 0 and ((y + 1 < a.shape[0] and a[y + 1, x, 3]) or (y > 0 and a[y - 1, x, 3])):
                a[y, x] = (24, 25, 26, 255) if y < 10 else a[y, x]
    return Prop(Image.fromarray(a, "RGBA"), (8, h + 8), (18, 3, 0, 0), False)
def pipe2(seed): return _heat(seed, False, _v(seed) % 5)
def pipepost2(seed): return _heat(seed, True, _v(seed) % 5)


# ---------------------------------------------------------------- the rockfall lock (dungeons/secret_cave.mmd): three tiles across a tunnel, each a heap of fallen rock and timber; "_on" = the same tile after the pry bar has lifted it
def _rockfall(seed, cleared):
    r = rng(seed); R = P["rock"]; W_ = P["wood"]; im = blank(18, 18)
    if not cleared:
        for k in range(7):                                     # big stones, back to front
            cx, cy, rr = r.randint(4, 14), r.randint(5, 13), r.randint(3, 5)
            _ell(im, cx, cy, rr, rr * 0.8, lambda x, y, e, cy=cy, cx=cx: R[4] if (y < cy - 1 and x < cx + 1) else R[3] if e < 0.7 else R[2] if y < cy + 2 else R[1])
        for _ in range(26): px(im, r.randrange(1, 17), r.randrange(8, 17), r.choice((R[0], R[1], R[2], R[3])))
        for x in range(2, 16): px(im, x, 16, R[0])
        line(im, 3, 6, 14, 10, W_[2]); line(im, 3, 7, 14, 11, W_[1])     # a snapped timber jammed in
        for _ in range(5): px(im, r.randrange(2, 16), r.randrange(10, 16), hx("#0e1012"))   # black gaps between the stones
    else:
        for _ in range(7): _ell(im, r.randint(3, 14), r.randint(11, 15), r.randint(1, 2), 1.1, lambda x, y, e: R[3] if e < 0.5 else R[1])
        for _ in range(18): px(im, r.randrange(1, 17), r.randrange(10, 17), r.choice((R[1], R[2], R[3])))
        line(im, 2, 14, 9, 15, W_[1])
    return finish(im, (9, 16), shadow=(15, 4, 1, -1), tall=not cleared)
def rockfall2(seed): return _rockfall(seed, False)
def rockfall_on2(seed): return _rockfall(seed, True)


# ---------------------------------------------------------------- the iron gate lock (dungeons/smugglers_tunnel.mmd): one tile of a gate across a 2-wide tunnel; "_on" = the same tile once the key is held (swung open)
def _irongate(seed, opened):
    r = rng(seed); S = P["steel"]; R = P["rust"]; im = blank(18, 20)
    for y in range(2, 18):                                         # the two posts
        for x in (0, 1, 16, 17): px(im, x, y, S[3] if x in (0, 16) else S[1])
    if not opened:
        for x in range(2, 16): px(im, x, 3, S[3]); px(im, x, 4, S[1])                      # the top rail
        for x in range(2, 16): px(im, x, 15, S[2]); px(im, x, 16, S[1])                    # the bottom rail
        for x in range(3, 16, 3):
            for y in range(5, 15): px(im, x, y, S[4] if y < 9 else S[3]); px(im, x + 1, y, S[1])    # the bars
        for k in range(3): px(im, 8 + k % 2, 8 + k, hx("#c9a63e") if k < 2 else hx("#a58a3a"))   # a padlock on a chain
        px(im, 8, 7, S[2]); px(im, 9, 7, S[2])
        for _ in range(8): px(im, r.randrange(2, 16), r.randrange(5, 15), R[1 + r.randrange(3)])
    else:                                                          # swung open against the right post: two bars showing, the lock hanging
        for y in range(3, 16): px(im, 14, y, S[3]); px(im, 15, y, S[1])
        for x in range(10, 14): px(im, x, 3, S[3]); px(im, x, 15, S[1])
        for y in range(6, 12): px(im, 3, y, S[2])
        px(im, 3, 12, hx("#c9a63e")); px(im, 3, 13, hx("#a58a3a"))
    return finish(im, (9, 19), shadow=(16, 4, 1, -1), tall=not opened)
def irongate2(seed): return _irongate(seed, False)
def irongate_on2(seed): return _irongate(seed, True)


# ---------------------------------------------------------------- the sealed-slab lock (dungeons/chapel_crypt.mmd): one tile of a stone slab sealing a passage, with a carved combination dial; "_on" = pushed aside once the code is known
def _sealslab(seed, opened):
    r = rng(seed); K = P["rock"]; im = blank(18, 20)
    if not opened:
        for y in range(2, 18):
            for x in range(0, 18):
                px(im, x, y, K[4] if y == 2 else K[3] if x == 0 else K[1] if (x == 17 or y == 17) else K[2])
        for _ in range(14): px(im, r.randrange(1, 17), r.randrange(3, 17), K[1])
        for (cx, cy, rr) in ((9, 9, 5), (9, 9, 3)):                              # the carved dial: two rings and ticks
            for a in range(0, 360, 20):
                import math
                px(im, int(round(cx + math.cos(math.radians(a)) * rr)), int(round(cy + math.sin(math.radians(a)) * rr)), K[0] if rr == 5 else K[4])
        for k in range(8): px(im, 9 + (k - 4) * 0, 5 + k // 4, K[0]) if False else None
        px(im, 9, 9, K[4]); px(im, 9, 4, hx("#c9a63e")); px(im, 9, 3, hx("#a58a3a"))     # the marker at the top of the dial
        for x in range(1, 17): px(im, x, 17, K[0])
    else:                                                      # pushed aside: the slab leaning against the left wall, the dark doorway open
        for y in range(3, 18):
            for x in range(6, 17): px(im, x, y, hx("#0b0c0d"))
        for y in range(2, 18):
            for x in range(0, 6): px(im, x, y, K[4] if y == 2 else K[3] if x == 0 else K[2])
        for x in range(6, 17): px(im, x, 17, K[1]); px(im, x, 2, K[3])
        for _ in range(6): px(im, r.randrange(6, 16), r.randrange(14, 17), K[2])
    return finish(im, (9, 19), shadow=(16, 4, 1, -1), tall=not opened)
def sealslab2(seed): return _sealslab(seed, False)
def sealslab_on2(seed): return _sealslab(seed, True)


# ---------------------------------------------------------------- furniture facings (Yan, 2026-11-10: "every object is placed facing the camera, it doesn't look natural")
# Every prop below has one drawn orientation: its front faces the camera (south). New families `<look>_fn` (faces north: we see its BACK), `<look>_fe` (faces east: we see its SIDE, the long axis now runs
# north-south so the sprite is a slim column) and `<look>_fw` (faces west: the mirror of fe). WorldArt.StandingAt picks the family from the wall the object stands against (Facing): back to the
# north wall = the default sprite, back to the south wall = fn, against the west wall = fe, against the east wall = fw; free-standing pieces get a stable pick by position.
def _mirror(p):
    from PIL import ImageOps
    im = ImageOps.mirror(p.im); return Prop(im, (im.width - 1 - p.pivot[0], p.pivot[1]), p.shadow, p.tall)
def _lockers_f(seed, view):
    s = P["olive"] if seed % 2 else P["steel"]
    top = props.flat(s[3], edge=s[4])
    if view == "n":
        def back(im, x0, y0, w, h):
            props.flat(s[1], bottom=s[0], left=s[2])(im, x0, y0, w, h)
            for x in (x0 + 5, x0 + 10): [px(im, x, y0 + y, s[2]) for y in range(h)]
            for x, y in ((x0 + 2, y0 + 3), (x0 + 12, y0 + 3), (x0 + 2, y0 + 14), (x0 + 12, y0 + 14)): px(im, x, y, s[3])      # rivets
            px(im, x0 + 7, y0 + 8, hx("#c8c0a8")); px(im, x0 + 8, y0 + 8, hx("#c8c0a8"))                                      # a paper tag
        return finish(props.box(15, 4, 18, top, back), (7, 21), shadow=(15, 5, 2, -1), tall=True)
    def ltop(im, x0, y0, w, h):                                       # the lid seen from above: three door tops side by side along the (now north-south) length, the lit door edge on the east (front) side
        props.flat(s[3], edge=s[4])(im, x0, y0, w, h)
        for y in (y0 + 4, y0 + 8): [px(im, x, y, s[1]) for x in range(x0, x0 + w)]
        for y in range(h): px(im, x0 + w - 1, y0 + y, s[4]) if y > 0 else None
    def side(im, x0, y0, w, h):                                       # the south END of the bank: a plain end panel, a lit rear edge, vent slits
        props.flat(s[2], bottom=s[0], left=s[3])(im, x0, y0, w, h)
        for y in (y0 + 2, y0 + 3): px(im, x0 + 1, y, s[0]); px(im, x0 + 2, y, s[0])
        px(im, x0 + w - 1, y0 + 6, s[1]); px(im, x0 + w - 1, y0 + 12, s[1])
    return finish(props.box(4, 11, 18, ltop, side), (2, 28), shadow=(6, 7, 1, -2), tall=True)
def _desk_f(seed, view):
    c = P["wood"]; top = props.flat(c[3], edge=c[4])
    if view == "n":
        def back(im, x0, y0, w, h):
            props.flat(c[1], bottom=c[0])(im, x0, y0, w, h)
            ImageDraw.Draw(im).rectangle([x0 + 2, y0 + 1, x0 + w - 3, y0 + h - 2], outline=c[2])        # the modesty panel
            for y in range(y0 + 4, y0 + h): px(im, x0 + w - 2, y, c[0])
        im = props.box(16, 6, 7, top, back); px(im, 4, 1, hx("#c8c0a8")); px(im, 5, 1, hx("#c8c0a8"))
        return finish(im, (8, 12), shadow=(16, 4, 2, -1))
    def dtop(im, x0, y0, w, h):                                       # the desk top seen from above, long way up the screen: the drawer pedestal on the north half (darker rim), the kneehole south of it, a blotter, papers
        props.flat(c[3], edge=c[4])(im, x0, y0, w, h)
        for y in range(y0 + 1, y0 + 6): px(im, x0 + 1, y, c[2]); px(im, x0 + w - 2, y, c[2])
        for x in range(x0 + 1, x0 + w - 1): px(im, x, y0 + 6, c[2])
        for x in range(x0 + 2, x0 + 5):
            for y in range(y0 + 7, y0 + 10): px(im, x, y, c[2])         # the blotter
        px(im, x0 + 3, y0 + 8, hx("#c8c0a8")); px(im, x0 + 4, y0 + 8, hx("#c8c0a8")); px(im, x0 + 1, y0 + 2, hx("#c8c0a8"))
    def side(im, x0, y0, w, h):                                       # the south end: a panelled end
        props.flat(c[1], bottom=c[0], left=c[2])(im, x0, y0, w, h)
        ImageDraw.Draw(im).rectangle([x0 + 1, y0 + 1, x0 + w - 2, y0 + h - 2], outline=c[2])
    im = props.box(6, 11, 7, dtop, side)
    return finish(im, (3, 17), shadow=(7, 7, 1, -2))
def _cabinet_f(seed, view):
    s = P["steel"]; top = props.flat(s[3], edge=s[4])
    if view == "n":
        def back(im, x0, y0, w, h):
            props.flat(s[1], bottom=s[0], left=s[2])(im, x0, y0, w, h)
            for y in (y0 + 5, y0 + 11): [px(im, x, y, s[0]) for x in range(x0 + 1, x0 + w - 1)]
            px(im, x0 + 2, y0 + 2, s[3]); px(im, x0 + w - 3, y0 + 2, s[3])
        return finish(props.box(12, 4, 17, top, back), (6, 20), shadow=(12, 4, 2, -1), tall=True)
    def ctop(im, x0, y0, w, h):                                       # the lid from above with a paper stack and a pen tray
        props.flat(s[3], edge=s[4])(im, x0, y0, w, h)
        px(im, x0 + 1, y0 + 3, hx("#d8d0b8")); px(im, x0 + 2, y0 + 3, hx("#d8d0b8")); px(im, x0 + 1, y0 + 4, hx("#c8c0a8")); px(im, x0 + 2, y0 + 7, s[1]); px(im, x0 + 1, y0 + 7, s[1])
        for y in range(1, h): px(im, x0 + w - 1, y0 + y, s[4])
    def side(im, x0, y0, w, h):                                       # the south end: a plain panel with rivets
        props.flat(s[2], bottom=s[0], left=s[3])(im, x0, y0, w, h)
        for y in (y0 + 2, y0 + h - 4): px(im, x0 + 1, y, s[1]); px(im, x0 + w - 2, y, s[1])
    return finish(props.box(4, 10, 17, ctop, side), (2, 26), shadow=(6, 6, 1, -2), tall=True)
def _console_f(seed, view):
    s = P["steel"]; im = blank(20, 18) if view == "n" else blank(11, 18); d = ImageDraw.Draw(im)
    if view == "n":                                                  # from behind: the monitors' dark backs with vents, cables, the desk's plain back panel
        d.rectangle([0, 9, 19, 12], fill=s[2]); d.line([(0, 9), (19, 9)], fill=s[3])
        d.rectangle([0, 13, 19, 17], fill=s[1]); d.line([(0, 17), (19, 17)], fill=s[0])
        for x in range(2, 19, 4): px(im, x, 15, s[0])
        for mx in (1, 10):
            d.rectangle([mx, 0, mx + 8, 7], fill=s[0]); d.rectangle([mx + 1, 1, mx + 7, 6], fill=s[1])
            for x in range(mx + 2, mx + 7, 2): px(im, x, 2, s[0]); px(im, x, 4, s[0])
            px(im, mx + 4, 8, s[0]); px(im, mx + 4, 7, s[0])
        for x in (4, 8, 14): px(im, x, 10, s[0]); px(im, x, 11, s[0])
        return finish(im, (10, 17), shadow=(20, 4, 2, -1))
    # the console from the east, 3/4: the long way runs up the screen. Desk top in plan with the two monitors' tops (dark blocks, a lit rim, each with its small south face) and the keyboard between them
    im = blank(11, 30); d = ImageDraw.Draw(im)
    d.rectangle([0, 0, 10, 21], fill=s[2]); d.line([(0, 0), (10, 0)], fill=s[3]); d.line([(0, 1), (0, 21)], fill=s[3])        # desk top
    d.rectangle([0, 22, 10, 29], fill=s[1]); d.line([(0, 29), (10, 29)], fill=s[0]); d.line([(0, 22), (0, 29)], fill=s[3])    # the south end panel
    for my in (2, 13):                                                # a monitor's top (west side of the desk) with its front face below it
        d.rectangle([1, my, 5, my + 5], fill=s[1]); d.line([(1, my), (5, my)], fill=s[3]); d.rectangle([1, my + 6, 5, my + 7], fill=s[0])
        px(im, 3, my + 7, props.SCREEN); px(im, 2, my + 7, props.TRACE) if seed % 2 else None
    d.rectangle([7, 9, 9, 12], fill=s[0]); [px(im, 8, y, s[1]) for y in (10, 12)]                                          # the keyboard between them
    px(im, 9, 26, props.LED[seed % 3]); px(im, 7, 26, props.LED[(seed + 1) % 3])
    return finish(im, (5, 29), shadow=(11, 6, 1, -2))
def _machine_f(seed, view):
    g = P["olive"] if seed % 2 else ramp("#2f3a3c", "#3f4e50", "#536567", "#6b7f80", "#8a9c9c"); S = P["steel"]; R = P["rust"]; r = rng(seed)
    if view == "n":                                                  # the lathe from behind: the motor housing, belt guard, the bed's back
        im = blank(25, 15); d = ImageDraw.Draw(im)
        d.rectangle([3, 10, 8, 14], fill=g[1]); d.rectangle([17, 10, 22, 14], fill=g[1]); d.line([(3, 14), (8, 14)], fill=g[0]); d.line([(17, 14), (22, 14)], fill=g[0])
        d.rectangle([2, 6, 23, 9], fill=g[1]); d.line([(2, 6), (23, 6)], fill=g[2])
        d.rectangle([1, 1, 7, 9], fill=g[2]); d.line([(1, 1), (7, 1)], fill=g[3]); d.line([(1, 2), (1, 9)], fill=g[3])
        d.rectangle([3, 3, 6, 8], fill=g[1])                          # the motor housing
        for y in range(4, 8, 2): px(im, 4, y, g[0]); px(im, 5, y, g[0])
        d.rectangle([12, 3, 15, 6], fill=g[2]); d.rectangle([19, 3, 22, 6], fill=g[2])
        for x in range(8, 22, 2): px(im, x, 8, S[1])                  # the way cover's slots
        for _ in range(4): px(im, r.randrange(2, 22), r.randrange(2, 9), R[2])
        return finish(im, (12, 14), shadow=(23, 5, 1, -1), tall=True)
    # the lathe from the east, 3/4: the long way up the screen, in plan: the headstock block at the north end with the chuck's disc, the bed with the bright ways, the carriage and the tailstock; the south end panel below
    im = blank(11, 32); d = ImageDraw.Draw(im)
    d.rectangle([1, 0, 9, 7], fill=g[2]); d.line([(1, 0), (9, 0)], fill=g[3]); d.line([(1, 1), (1, 7)], fill=g[3])        # headstock
    disc(im, 5, 4, 2.2, S[3]); disc(im, 5, 4, 1, S[1]); px(im, 4, 3, S[4])                                                  # the chuck, seen from above
    d.rectangle([2, 8, 8, 23], fill=g[1]); d.line([(2, 8), (8, 8)], fill=g[2])                                              # the bed
    d.line([(5, 9), (5, 22)], fill=S[3]); d.line([(4, 9), (4, 22)], fill=S[1])                                              # the ways, bare metal
    d.rectangle([2, 12, 8, 15], fill=g[2]); d.line([(2, 12), (8, 12)], fill=g[3]); px(im, 5, 13, S[4])                      # the carriage and tool post
    d.rectangle([2, 19, 8, 22], fill=g[2]); d.line([(2, 19), (8, 19)], fill=g[3]); disc(im, 5, 21, 1, S[3])               # the tailstock and its handwheel
    d.rectangle([1, 24, 9, 31], fill=g[1]); d.line([(1, 31), (9, 31)], fill=g[0]); d.line([(1, 24), (1, 31)], fill=g[2])      # the south end panel (the stand)
    px(im, 8, 27, hx("#c84a32")); px(im, 3, 27, S[4])
    for _ in range(4): px(im, r.randrange(2, 9), r.randrange(1, 30), R[2])
    return finish(im, (5, 31), shadow=(10, 6, 1, -2), tall=True)

FACING_BASE = {"lockers": _lockers_f, "desk": _desk_f, "cabinet": _cabinet_f, "console": _console_f, "machine": _machine_f}
def _facing_prop(base, view):
    def f(seed):
        p = FACING_BASE[base](seed, "n" if view == "n" else "e")
        return _mirror(p) if view == "w" else p
    return f
SIDE_BASES = ("lockers", "desk", "cabinet")      # Yan (2026-11-10): the console's and the lathe's side views "do not read as the object" and do not look like their front: they only get a back view (`_fn`) now
FACING_VIEWS = {b: (("n", "e", "w") if b in SIDE_BASES else ("n",)) for b in FACING_BASE}
FACINGS = {f"{b}_f{v}": _facing_prop(b, v) for b in FACING_BASE for v in FACING_VIEWS[b]}
FACING_VARIANTS = {f"{b}_f{v}": (2 if b in ("lockers", "machine") else 1) for b in FACING_BASE for v in FACING_VIEWS[b]}
V2_VARIANTS.update(FACING_VARIANTS)

NEW = {"tires": tires2, "trash": trash2, "jersey": jersey2, "brush": brush2, "debris": debris2, "jersey_v": jersey_v2, "jersey_c": jersey_c2, "block": block2, "block_v": block_v2, "block_c": block_c2, "scrap": scrap2, "fountain": fountain2, "hatch": hatch2, "pillar": pillar2, "culvert": culvert2, "pipe": pipe2, "pipepost": pipepost2, "rockfall": rockfall2, "rockfall_on": rockfall_on2, "irongate": irongate2, "irongate_on": irongate_on2, "sealslab": sealslab2, "sealslab_on": sealslab_on2, **FACINGS}

if props.PROPS.get("tires") is not NEW["tires"]: props.PROPS.update(NEW); props.VARIANTS.update(V2_VARIANTS)

def place(canvas, p, x, y):
    if p.shadow:
        w, h, dx, dy = p.shadow; canvas.alpha_composite(shadow_ellipse(w, h), (int(x + dx - w / 2), int(y + dy - h / 2)))
    canvas.alpha_composite(p.im, (int(x - p.pivot[0]), int(y - p.pivot[1])))

def sheet(out, scale=6):
    sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
    import ground, make_armor_avatars as av
    a = [a for a in av.ARMORS if a[0] == "Raider Jacket"][0]; man = av.build(a[0], a[3], a[4]).convert("RGBA")
    names = list(NEW); cw = 34; W = 24 + len(names) * 2 * cw; RH = 44
    bg = blank(W, RH * 2); gr = ground.GROUNDS["grass"](3); dt = ground.GROUNDS["dirt"](3)
    for ty in range(0, RH * 2, 64):
        for tx in range(0, W, 64): bg.paste(gr, (tx, ty))
    for ty in range(RH, RH * 2, 64):
        for tx in range(0, W, 64): bg.paste(dt, (tx, ty))
    for k, n in enumerate(names):
        for row in range(2):
            for j, fn in enumerate((props.OLD_PROPS[n], NEW[n])):
                place(bg, fn(1000), 36 + (k * 2 + j) * cw // 1 - 6, 34 + row * RH)
    bg.alpha_composite(man, (4, 20)); bg.alpha_composite(man, (4, 20 + RH))
    bg.resize((bg.width * scale, bg.height * scale), Image.NEAREST).convert("RGB").save(out); print(out, bg.size)

if __name__ == "__main__": sheet(sys.argv[1] if len(sys.argv) > 1 else "/tmp/props_v2.png")
