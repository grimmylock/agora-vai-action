# Batch 31: the world's vegetation and wrecks in the "Zone realism" look Yan picked (concept A,
# ArtSource/concept/concept.py): birches with broken see-through canopies, ragged spruces, dead
# snags, scrub instead of round bushes, rusted cars. Wraps the concept builders as world Props.
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "concept"))
sys.path.insert(0, "/home/claude/art/concept")
import concept
from props import Prop

A = concept.STYLES["A"]
AUTUMN = dict(A, leaf=concept.ramp("4a2a18", "673a20", "84502a", "9e6634", "b8803e"))      # crimson woods: rust-red birches

def _prop(sp, shadow, tall=True, pad=1):
    im, piv = sp
    return Prop(im, piv, shadow, tall)

def birch(seed, S=A): h = 40 + (seed * 7) % 12; return _prop(concept.birch(S, seed, h), (14, 4, 1, 0))
def spruce(seed): return _prop(concept.spruce(A, seed, 44 + (seed * 5) % 14), (14, 5, 1, 0))
def snag(seed): return _prop(concept.deadtree(A, seed, 34 + (seed * 3) % 12), (8, 3, 1, 0))
# Batch 117 (Yan: "make trees bigger but have different sprites and sizes for them, some variety"). The concept's trees were
# 40-52 px (about 5 m; a 16 px person is 1.8 m). Now each kind comes in distinct shapes and sizes, still in concept A's
# palette and broken see-through canopies: birch 70-112 px (8-12 m: tall single, forked twin, slender young, broad old with
# drooping strands), spruce 70-125 px (dense, scraggly, broken-topped), dead snags 50-90 px (forked, snapped, leaning).
import math, random
def _px(im, x, y, c):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < im.width and 0 <= y < im.height: im.putpixel((x, y), c)

def _crown(im, r, S, pal, cx, cy, sx, sy, n, rmin, rmax, droop=0.0):
    """leaf clusters scattered round (cx, cy), lit from the top-left, gaps left open so the canopy stays see-through"""
    for _ in range(n):
        x0 = cx + max(-1.8, min(1.8, r.gauss(0, 1))) * sx; y0 = cy + max(-1.6, min(1.8, r.gauss(0, 1))) * sy; rad = r.uniform(rmin, rmax)   # no stray clusters
        for yy in range(int(y0 - rad), int(y0 + rad * (1 + droop)) + 1):
            for xx in range(int(x0 - rad), int(x0 + rad) + 1):
                d = ((xx - x0) / rad) ** 2 + ((yy - y0) / (rad * (0.8 + droop))) ** 2
                if d <= 1 and r.random() > 0.14:
                    lit = (xx - x0) + (yy - y0) < -0.5
                    i = 3 if lit and d < .5 else 2 if lit else 1 if d < .8 else 0
                    if r.random() < 0.07: i = 4
                    _px(im, xx, yy, S[pal][i])

def big_birch(seed, S=A):
    r = random.Random(seed * 31 + 7); kind = seed % 4
    h = [104, 92, 76, 100][kind] + r.randint(-6, 8); w = int(h * (0.42 if kind == 2 else 0.6 if kind != 3 else 0.7))
    im = concept.blank(w, h); base = w // 2 - 1
    trunks = {0: [(0, r.uniform(-.06, .06), 3, 0.66)], 1: [(-2, -.11, 3, 0.6), (2, .09, 2, 0.55)],
              2: [(0, r.uniform(-.05, .05), 2, 0.7)], 3: [(0, r.uniform(-.04, .04), 4, 0.55)]}[kind]
    tops = []
    for (dx, lean, th, frac) in trunks:
        ln = int(h * frac)
        for k in range(ln):                                                   # white bark, black lenticels, dark base
            y = h - 1 - k; x0 = base + dx + int(k * lean)
            for t in range(th):
                c = S["bark"][0] if t == 0 else S["bark"][1] if t < th - 1 else S["bark"][2]
                if k < 4: c = S["bark"][2] if t else S["mark"]
                elif r.random() < 0.16 and y % 3 == 0: c = S["mark"]
                _px(im, x0 + t, y, c)
        tops.append((base + dx + int(ln * lean) + th / 2, h - ln))
    n = int(28 * (h / 46) ** 1.6)
    for (tx, ty) in tops:
        _crown(im, r, S, "leaf", tx, ty + h * 0.16, w * (0.14 if kind == 2 else 0.19), h * 0.16, n // len(tops), 1.8, 3.4, droop=0.45 if kind == 3 else 0.0)
        for _ in range(6):                                                    # twigs showing through
            x, y = tx + r.randint(-6, 6), ty + h * 0.12
            for _s in range(r.randint(4, 9)): _px(im, x, y, S["bark"][2]); x += r.choice((-1, 0, 1)); y -= 1
    if kind == 3:                                                             # the old birch's hanging strands
        for _ in range(14):
            x, y = tops[0][0] + r.gauss(0, w * 0.22), tops[0][1] + h * r.uniform(0.15, 0.3)
            for _s in range(r.randint(4, 9)): _px(im, x, y, S["leaf"][r.choice((0, 1))]); y += 1
    return Prop(concept.outline(im, S["outline"]), (base + 1, h - 1), (int(w * 0.5), 5, 1, 0), True)

def big_spruce(seed):
    S = A; r = random.Random(seed * 17 + 3); kind = seed % 3
    h = [118, 96, 84][kind] + r.randint(-8, 8); w = int(h * 0.44); im = concept.blank(w, h); cx = w / 2
    for y in range(h - 8, h): _px(im, cx - 1, y, S["dead"][1]); _px(im, cx, y, S["dead"][0])
    top = 3 if kind != 2 else int(h * 0.22)                                   # kind 2: the top snapped off
    if kind == 2:
        for y in range(top - 6, top + 2): _px(im, cx, y, S["dead"][1])
    starts, y = [], top                                                       # branch tiers: each widens downward, then the next starts
    while y < h - 9: starts.append(y); y += r.randint(4, 7)
    for y0 in range(top, h - 9):
        t0 = max(st for st in starts if st <= y0); nxt = min([st for st in starts if st > y0] + [h - 9])
        phase = (y0 - t0) / max(1, nxt - t0)
        reach = (1.5 + (y0 - top) * (w * 0.5) / (h - top)) * (0.45 + 0.55 * phase) + r.uniform(-1.6, 1.6)
        gap = (kind == 1 and y0 > top + (h - top) * 0.3 and random.Random(seed * 7 + t0).random() < 0.22) or r.random() < 0.04   # kind 1: scraggly, a few tiers thin out (never the top)
        for side in (-1, 1):
            if gap or r.random() < 0.12: continue
            ln = int(reach + r.uniform(-1, 1.5))
            for s_ in range(max(1, ln)):
                x = cx + side * s_; y = y0 + s_ * 0.45; lit = side < 0
                i = 3 if lit and s_ < ln - 1 and r.random() < .5 else 2 if lit else 1 if r.random() < .7 else 0
                _px(im, x, y, S["spruce"][i]); _px(im, x, y + 1, S["spruce"][max(0, i - 1)])
    return Prop(concept.outline(im, S["outline"]), (int(cx), h - 1), (int(w * 0.6), 6, 1, 0), True)

def big_snag(seed):
    S = A; r = random.Random(seed * 13 + 5); kind = seed % 3
    h = [86, 64, 54][kind] + r.randint(-6, 6); w = int(h * 0.62); im = concept.blank(w, h)
    def branch(x, y, ang, ln, th):
        for _s in range(int(ln)):
            for k in range(th): _px(im, x + k, y, S["dead"][2 if k == 0 else 1])
            x += math.cos(ang); y -= math.sin(ang)
            if r.random() < 0.06 and ln > 6: branch(x, y, ang + r.choice((-.7, .7)), ln * .5, max(1, th - 1))
        if ln > 5 and kind != 1: branch(x, y, ang + r.uniform(-.6, .6), ln * .55, max(1, th - 1))
    lean = math.pi / 2 + (r.uniform(-.15, .15) if kind != 2 else r.choice((-.35, .35)))   # kind 2: leaning hard
    branch(w / 2, h - 1, lean, h * (0.55 if kind != 1 else 0.85), 4 if kind == 0 else 3)   # kind 1: snapped, a bare pole
    return Prop(concept.outline(im, S["outline"]), (w // 2, h - 1), (int(w * 0.3), 4, 1, 0), True)

def scrub(seed, S=A): return _prop(concept.bush(S, seed), (16, 4, 1, 0), tall=False)
PAINT = [None, concept.ramp("2a2f33", "3b4348", "4e575c", "64676a"), concept.ramp("33372a", "454a36", "585d45", "6c7055"),
         concept.ramp("4a4740", "625e54", "7c776a", "938d7e")]          # rust, faded blue, faded olive, old white
def rustcar(seed):
    S = A if PAINT[seed % 4] is None else dict(A, rust=PAINT[seed % 4])
    im, piv = concept.car(S, seed)
    if seed % 2: im = im.transpose(0); piv = (im.width - 1 - piv[0], piv[1])     # facing the other way
    return Prop(im, piv, (32, 6, 2, -1), False)
def grass(seed, dry=False): return concept.grass_clump(A, seed, dry=dry, tall=seed % 3 == 0)[0]

# Batch 33: the swamp concept's pieces (ArtSource/concept/biomes.py) as world props and decals
import biomes
def cattails(seed): return _prop(biomes.cattails(seed, n=8 + seed % 5), (14, 4, 0, -1), tall=False)
def lily(seed): return biomes.lily(seed)[0]
def sunktruck(seed):
    im, piv = biomes.sunk_truck(seed)
    if seed % 2: im = im.transpose(0); piv = (im.width - 1 - piv[0], piv[1])
    return Prop(im, piv, None, False)                   # half under water: no ground shadow

# Batch 33: city street furniture from the city concept
def _flip(im, piv, on): return (im.transpose(0), (im.width - 1 - piv[0], piv[1])) if on else (im, piv)
def lamp(seed):
    im, piv = _flip(*biomes.lamp(seed), seed % 2); return Prop(im, piv, None, True)
def busstop(seed): im, piv = biomes.bus_stop(seed); return Prop(im, piv, (36, 5, 0, -1), True)
def bus(seed):
    im, piv = _flip(*biomes.bus(seed), seed % 2); return Prop(im, piv, (74, 7, 0, -1), True)
def dumpster(seed): im, piv = _flip(*biomes.dumpster(seed), seed % 2); return Prop(im, piv, (16, 4, 0, -1), False)
def trash(seed): im, piv = biomes.trash(seed); return Prop(im, piv, None, False)

# Batch 33: industrial concept pieces. Multi-tile things are centred on their footprint, which the map
# puts to the right of the anchor cell (tank: 2 tiles, anchor = left; flatcar: 4 tiles, anchor = 2nd).
def tank(seed):
    im, (cx, cy) = biomes.tank(seed, 15); return Prop(im, (cx - 8, cy), (34, 8, 8, -2), True)
def flatcar(seed):
    im, (cx, cy) = biomes.flatcar(seed); return Prop(im, (cx - 8, cy), (68, 6, 8, -1), False)
def pipe(seed, post):
    """one tile of an overhead pipeline (walk under it); `post` adds a concrete support"""
    r = concept.rr(seed); S = biomes.S; h = 14; im = concept.blank(16, h + 8)
    for x in range(16):
        for y in range(4): concept.px(im, x, y + 2, S["steel"][3 - y] if r.random() > .08 else S["rust"][2])
    if post:
        for y in range(6, h + 7):
            for k in range(3): concept.px(im, 6 + k, y, S["panel"][2 - (k == 2)])
    import numpy as np
    a = np.array(im)                                            # outline top/bottom only: the run continues sideways
    for x in range(16):
        for y in (1, 6):
            if a[y, x, 3] == 0: a[y, x] = biomes.DK(a[y + (1 if y == 1 else -1), x])
    from PIL import Image as _I
    return Prop(_I.fromarray(a, "RGBA"), (8, h + 7), None, False)
