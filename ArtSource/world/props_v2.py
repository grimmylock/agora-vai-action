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
def _tires4(seed):                                   # two flat tyres with a third leaning against them, tilted
    T = _T(); im = blank(28, 19)
    _tband(im, T, 8, 12, 7, 4); _tband(im, T, 8, 9, 7, 3); _ttop(im, T, 8, 8.5, 7, 2.8, 0.2)
    _tring_rot(im, T, 19.5, 9, 5, 7.5, -14)
    return finish(im, (14, 18), shadow=(24, 5, 1, -1))
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
V2_VARIANTS = {"tires": 5, "trash": 5, "jersey": 5, "brush": 5, "debris": 5}

NEW = {"tires": tires2, "trash": trash2, "jersey": jersey2, "brush": brush2, "debris": debris2}

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
