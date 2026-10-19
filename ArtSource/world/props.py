# 3/4 props for the world (Batch 30). Each builder returns Prop(image, pivot, shadow):
#   image  - RGBA, outlined, drawn in 3/4 (top face + front face, never side faces)
#   pivot  - (x, y) pixel of the ground contact point (bottom-centre of the footprint) - sorting + placement
#   shadow - (w, h, dx, dy) ellipse baked into the ground under/beside the prop (dx/dy from pivot)
# Sizes are against a 16 px tall character (STYLE_RESEARCH 1-2).
from dataclasses import dataclass
import numpy as np
from PIL import Image, ImageDraw
from worldlib import *

@dataclass
class Prop:
    im: Image.Image
    pivot: tuple
    shadow: tuple = None       # (w, h, dx, dy)
    tall: bool = False         # occludes units standing behind it (fades)

def finish(im, pivot, shadow=None, tall=False, diag=False):
    im = pad(im, 1); p = (pivot[0] + 1, pivot[1] + 1)
    # Batch 31: world props get a selective outline (a darker shade of their own colour), not black
    import os
    return Prop(outline(im, diag=diag) if os.environ.get("WORLD_BLACK_OUTLINE") else outline_sel(im), p, shadow, tall)

# ------------------------------------------------------------------ vegetation
def tree(seed, pal=None, w=40, canopy_h=30, trunk_h=12):
    pal = pal or P["foliage"]; r = rng(seed)
    H = canopy_h + trunk_h; im = blank(w, H)
    cx = w / 2; tx = int(cx) - 2
    # trunk with root flare, lit left edge
    d = ImageDraw.Draw(im)
    for y in range(canopy_h - 6, H):
        for x in range(tx, tx + 4): px(im, x, y, P["bark"][2] if x == tx else P["bark"][1] if x < tx + 3 else P["bark"][0])
    for x in (tx - 1, tx + 4): px(im, x, H - 1, P["bark"][1])
    px(im, tx - 2, H - 1, P["bark"][0]); px(im, tx + 5, H - 1, P["bark"][0])
    # canopy: staggered leaf bundles, back ones first
    blobs = []
    for k in range(r.randint(5, 7)):
        bx = cx + r.uniform(-w * 0.28, w * 0.28); by = canopy_h * 0.45 + r.uniform(-canopy_h * 0.25, canopy_h * 0.22)
        blobs.append((by, bx, r.uniform(w * 0.18, w * 0.26)))
    blobs.append((canopy_h * 0.48, cx, w * 0.3))
    for by, bx, br in sorted(blobs):
        shade_blob(im, bx, by, br, pal, r.randrange(1 << 30), squash=0.85)
    # a few highlight leaf clusters and dark gaps
    for _ in range(8):
        x, y = r.randrange(int(w * .2), int(w * .7)), r.randrange(2, int(canopy_h * .5))
        if im.getpixel((x, y))[3]: px(im, x, y, pal[4]); px(im, x + 1, y, pal[3])
    return finish(im, (int(cx), H - 1), shadow=(int(w * 0.7), 10, 3, -1), tall=True)

def pine(seed, w=28, h=50):
    pal = P["pine"]; r = rng(seed); im = blank(w, h); cx = w / 2
    trunk_top = h - 9
    for y in range(trunk_top, h):
        for x in range(int(cx) - 1, int(cx) + 2): px(im, x, y, P["bark"][2] if x == int(cx) - 1 else P["bark"][0])
    tiers = 5
    for t in range(tiers):                          # layered skirts, wider toward the bottom
        y0 = 2 + t * (h - 12) / tiers; yb = y0 + (h - 12) / tiers + 4
        half = 3 + (t + 1) * (w / 2 - 3) / tiers
        for y in range(int(y0), int(yb)):
            f = (y - y0) / (yb - y0); hw = half * (0.35 + 0.65 * f)
            for x in range(int(cx - hw), int(cx + hw) + 1):
                rel = (x - (cx - hw)) / (2 * hw + 1e-6)
                v = 3 if rel < 0.3 else 2 if rel < 0.62 else 1
                if f > 0.8: v -= 1                     # underside of each skirt in shadow
                if r.random() < 0.08: v = max(0, v - 1)
                px(im, x, y, pal[max(0, v)])
        for x in range(int(cx - half * 0.9), int(cx + half * 0.9)):     # ragged hem
            if r.random() < 0.5: px(im, x, int(yb), pal[0])
    px(im, int(cx), 1, pal[3]); px(im, int(cx), 0, pal[2])
    return finish(im, (int(cx), h - 1), shadow=(int(w * 0.8), 8, 3, -1), tall=True)

def deadtree(seed, w=30, h=42):
    r = rng(seed); im = blank(w, h); pal = P["deadwood"]; cx = w // 2
    def branch(x, y, ang, length, thick):
        for i in range(length):
            xx = int(round(x + np.cos(ang) * i)); yy = int(round(y - np.sin(ang) * i))
            for t in range(thick): px(im, xx + t, yy, pal[2 if t == 0 else 1])
            if i == length - 1: return xx, yy
        return x, y
    for y in range(h - 26, h):                                   # trunk, slightly leaning
        lean = (h - y) // 14
        for x in range(cx - 2 + lean, cx + 2 + lean): px(im, x, y, pal[2] if x == cx - 2 + lean else pal[1] if x < cx + 1 + lean else pal[0])
    px(im, cx - 3, h - 1, pal[1]); px(im, cx + 2, h - 1, pal[0])
    for k in range(r.randint(4, 6)):
        y = h - 26 + r.randint(-2, 10); ang = r.uniform(0.5, 2.6); ln = r.randint(7, 13)
        ex, ey = branch(cx + 1, y, ang, ln, 2 if ln > 10 else 1)
        branch(ex, ey, ang + r.uniform(-0.8, 0.8), r.randint(3, 6), 1)
    return finish(im, (cx, h - 1), shadow=(18, 6, 3, -1), tall=True)

def bush(seed, pal=None, w=20, h=14):
    pal = pal or P["foliage"]; r = rng(seed); im = blank(w, h)
    for k in range(4):
        shade_blob(im, w / 2 + r.uniform(-w * .25, w * .25), h * 0.55 + r.uniform(-2, 1), r.uniform(4.5, 6.5), pal, r.randrange(1 << 30), squash=0.8)
    for _ in range(4):
        x, y = r.randrange(3, w - 3), r.randrange(1, h // 2)
        if im.getpixel((x, y))[3]: px(im, x, y, pal[4])
    return finish(im, (w // 2, h - 1), shadow=(w - 2, 5, 1, -1))

def reeds(seed):
    r = rng(seed); im = blank(14, 18); pal = P["drygrass"]
    for k in range(9):
        x = 1 + r.randrange(12); hh = r.randint(9, 17)
        for y in range(hh): px(im, x + (1 if y > hh * 0.7 and k % 2 else 0), 17 - y, pal[3] if y > hh - 3 else pal[2] if y > hh * 0.4 else pal[1])
        if r.random() < .5: px(im, x, 17 - hh, hx("#5a3f2b")); px(im, x, 18 - hh, hx("#5a3f2b"))     # cattail heads
    return finish(im, (7, 17), shadow=(12, 4, 1, -1))

def brush(seed):
    """Batch 99: a clump of dry scrub - dark woody stems, pale dead tips, so it reads on dirt and grass alike."""
    r = rng(seed); im = blank(16, 12); pal = P["drygrass"]
    for k in range(10):
        x = 2 + r.randrange(12); hh = r.randint(4, 10); lean = r.choice((-1, 0, 1))
        for y in range(hh):
            xx = x + (lean * y) // 4
            px(im, xx, 11 - y, P["wood"][1] if y < hh // 2 else pal[1] if y < hh - 2 else pal[3])
    return finish(im, (8, 11), shadow=(12, 4, 1, -1))

# ------------------------------------------------------------------ rocks, piles
def _boulder(im, ox, oy, w, h, seed, pal, r, flat=1.0, crack=False):
    """one lumpy stone at (ox, oy) in im: lit top face, darker front face"""
    shape = value_noise(w, h, 4, seed, wrap=False)
    for y in range(h):
        for x in range(w):
            dx = (x - w / 2 + 0.5) / (w / 2); dy = (y - h * 0.58) / (h * 0.5)
            if dx * dx * flat + dy * dy * 1.1 > 1 - shape[y, x] * 0.35: continue
            top = y < h * 0.55 - abs(dx) * h * 0.18 + shape[y, x] * 2
            v = (3 if dx < 0.15 else 2) if top else (2 if dx < -0.4 else 1 if dx < 0.35 else 0)
            if top and dx < -0.2 and dy < -0.45: v = 4
            if r.random() < 0.07: v = max(0, v - 1)
            px(im, ox + x, oy + y, pal[v])
    if crack:                                                    # a split down the middle
        x = ox + w // 2 + r.randint(-2, 2)
        for y in range(oy + 1, oy + h - 1):
            if im.getpixel((x, y))[3]: px(im, x, y, pal[0])
            x += r.choice((-1, 0, 0, 1))

def rock(seed, w=None, h=None):
    """Batch 127 (Yan: "we need at least a few sprites for each stuff like those otherwise everything looks the same"): seven
    kinds of rock instead of one round boulder - boulder, flat slab, split boulder, a cluster, a tall shard, a mossy one, one
    half sunk in grass - in three stone tints."""
    r = rng(seed); v = (seed - 1000) // 77
    kind = v % 7
    tint = ((1.0, 1.0, 1.0), (1.06, 1.0, 0.9), (0.93, 0.98, 0.96))[(v // 7 + v) % 3]   # grey, warm, cool
    pal = [(min(255, int(c[0] * tint[0])), min(255, int(c[1] * tint[1])), min(255, int(c[2] * tint[2])), 255) for c in P["rock"]]
    if kind == 3:                                                # a cluster: one stone and two small ones
        w, h = 24, 14; im = blank(w, h)
        _boulder(im, 6, 2, 13, 10, seed, pal, r)
        _boulder(im, 0, 7, 8, 6, seed + 1, pal, r)
        _boulder(im, 17, 8, 7, 5, seed + 2, pal, r)
    elif kind == 4:                                              # a tall, leaning shard
        w, h = 14, 16; im = blank(w, h); lean = r.choice((-1, 1)); top = r.randint(0, 3); jag = 0
        for y in range(top, h):
            jag = max(-1, min(1, jag + r.choice((-1, 0, 1))))
            half = max(2, int(2.5 + (y - top) / (h - top) * 3.5) + jag); cx = w // 2 + lean * (h - y) // 5
            for x in range(cx - half, cx + half + 1):
                if 0 <= x < w: px(im, x, y, pal[3 if x < cx - half // 3 else 2 if x < cx + half // 2 else 1])
        for x in range(w):
            if im.getpixel((x, h - 1))[3]: px(im, x, h - 1, pal[0])
    else:
        w = r.randint(14, 24) if kind != 1 else r.randint(22, 30)
        h = int(w * (0.72 if kind != 1 else 0.42)); im = blank(w, h)
        _boulder(im, 0, 0, w, h, seed, pal, r, flat=1.0 if kind != 1 else 0.85, crack=kind == 2)
    mosses = 2 if kind != 5 else 14
    for _ in range(mosses):                                      # moss on the top
        x, y = r.randrange(2, w - 2), r.randrange(0, max(1, h // 2))
        if im.getpixel((x, y))[3]: px(im, x, y, P["grass"][2]); px(im, x + 1, y, P["grass"][1])
    if kind == 6:                                                # sunk in grass: tufts across its foot
        for x in range(w):
            for k in range(r.randint(1, 3)):
                if r.random() < .7: px(im, x, h - 1 - k, P["grass"][r.randint(1, 3)])
    return finish(im, (w // 2, h - 1), shadow=(w, 5, 2, -1))

def debris(seed):
    """Batch 99 (refs: ArtSource/refs/debris): a heap of demolition debris, ~2 m - broken concrete slabs tilted on each other,
    bent rebar sticking out, a snapped plank, dust and grit round its foot."""
    r = rng(seed); c = P["concrete"]; im = blank(22, 14)
    for _ in range(14):                                          # grit and small chunks at the foot
        px(im, r.randrange(1, 21), r.randrange(10, 14), r.choice((c[1], c[2], P["dirt"][2] if "dirt" in P else c[0])))
    for k in range(4):                                           # tilted slabs: a lit top edge, a shaded face
        x0, y0, w, th = r.randrange(1, 12), r.randrange(3, 9), r.randint(5, 9), r.randint(3, 4)
        tilt = r.choice((-1, 0, 1))
        for i in range(w):
            yy = y0 + (i * tilt) // 4
            px(im, x0 + i, yy, c[4])
            for j in range(1, th): px(im, x0 + i, yy + j, c[2] if j < th - 1 else c[1])
        if r.random() < .5: px(im, x0 + r.randrange(w), y0 + 1, c[0])                         # a hole
    for k in range(r.randint(2, 3)):                             # rebar, bent
        x, y = r.randrange(3, 19), r.randrange(4, 9)
        for i in range(r.randint(3, 6)): px(im, x + (i // 2) * r.choice((1, -1)), y - i, P["rust"][1 + (i % 2)])
    for i in range(7): px(im, 13 + i, 9 - i // 3, P["wood"][2] if i < 6 else P["wood"][1])     # a snapped plank
    return finish(im, (11, 13), shadow=(20, 5, 1, -1))

def scrap(seed):
    """Batch 99 (refs: ArtSource/refs/scrap): a pile of scrap metal - rusty sheets bent over each other, a pipe, angle iron,
    a wheel rim; the odd bit of bare steel catching the light."""
    r = rng(seed); im = blank(26, 17)
    for k in range(7):                                           # bent sheets, back to front
        x0, y0, w, h = r.randrange(0, 16), r.randrange(2, 10), r.randint(6, 10), r.randint(3, 5)
        pal = r.choice((P["rust"], P["rust"], P["steel"], P["tin"]))
        bend = r.randrange(w)
        for i in range(w):
            for j in range(h):
                yy = y0 + j + (1 if i > bend else 0)
                v = 3 if j == 0 else 2 if j < h - 1 else 0
                if i == bend: v = max(0, v - 1)
                px(im, x0 + i, yy, pal[min(v, len(pal) - 1)])
    for i in range(10): px(im, 4 + i, 13 - i // 4, P["steel"][3]); px(im, 4 + i, 14 - i // 4, P["steel"][1])   # a pipe
    disc(im, 20, 12, 3, P["steel"][1]); disc(im, 20, 12, 1.6, (0, 0, 0, 0)); px(im, 19, 10, P["steel"][4])     # a wheel rim
    for _ in range(4): px(im, r.randrange(26), r.randrange(4, 16), P["steel"][4])
    return finish(im, (13, 16), shadow=(24, 5, 1, -1))

def logs(seed):
    im = blank(20, 11); c = P["wood"]
    for i, (x, y) in enumerate(((2, 5), (9, 5), (5, 1))):
        d = ImageDraw.Draw(im); d.rectangle([x, y, x + 10, y + 4], fill=c[2]); d.line([(x, y), (x + 10, y)], fill=c[3])
        d.line([(x, y + 4), (x + 10, y + 4)], fill=c[1])
        disc(im, x + 10, y + 2, 2.2, hx("#a88a62")); px(im, x + 10, y + 2, c[1])        # cut end rings
    return finish(im, (10, 10), shadow=(18, 4, 1, -1))

def logs_v(seed):
    """Batch 96: logs laid north-south - two trunks side by side seen from above (bark, a knot), the cut ends at the south."""
    c = P["wood"]; W, T, Hf = 10, 16, 5; im = blank(W, T + Hf)
    for x0 in (0, 5):
        for y in range(T + Hf - 3):
            for x in range(x0, x0 + 5):
                v = 3 if x == x0 + 1 else 1 if x == x0 + 4 else 2
                px(im, x, y, c[v])
        px(im, x0 + 2, 5 + (seed + x0) % 7, c[1])                  # a knot
        disc(im, x0 + 2, T + Hf - 3, 2.2, hx("#a88a62")); px(im, x0 + 2, T + Hf - 3, c[1])   # cut end with its rings
    return finish(im, (W // 2, T + Hf - 1), shadow=(11, 4, 1, -1))

def trash(seed):
    """Batch 99 (refs: ArtSource/refs/trash): dumped rubbish - two or three black bin bags slumped together, one split open,
    paper, a bottle and tins spilled round them."""
    r = rng(seed); im = blank(18, 12)
    bag = ramp("#141516", "#1e2022", "#2b2e31", "#3d4246")
    for k, (cx, cy, rx, ry) in enumerate(((6, 6, 4, 3.4), (11, 7, 4.2, 3.2), (8, 9, 3.6, 2.6))[: r.randint(2, 3)]):
        for y in range(int(cy - ry) - 1, int(cy + ry) + 1):
            for x in range(int(cx - rx) - 1, int(cx + rx) + 1):
                e = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
                if e <= 1: px(im, x, y, bag[3] if e < .25 and y < cy else bag[2] if y < cy else bag[1] if e < .7 else bag[0])
        px(im, int(cx), int(cy - ry), bag[3])                    # the knot
    for _ in range(9):                                           # spilled litter
        x, y = r.randrange(0, 18), r.randrange(7, 12)
        px(im, x, y, r.choice((hx("#b9b3a0"), hx("#8f8a78"), hx("#6b7f5a"), P["rust"][3], hx("#a7c0c4"))))
    return finish(im, (9, 11), shadow=(16, 4, 1, -1))

def heatpipe(seed, post):
    """Batch 99: one tile of an overhead district-heating line (walk under it) - two pipes side by side, foil-wrapped with
    bands every half metre, the foil torn back to rusty steel in places; `post` adds the concrete T support under them."""
    r = rng(seed); h = 13; im = blank(16, h + 9)
    foil = ramp("#5f6264", "#7d8082", "#9a9d9e", "#b9bbbb")
    for k, y0 in enumerate((1, 6)):                              # two pipes with a gap, the far one darker
        for x in range(16):
            for j in range(4):
                v = (3 if j == 0 else 2 if j == 1 else 1 if j == 2 else 0) - k
                c = foil[max(0, v)]
                if (x + 3 * k) % 8 == 0 and j < 3: c = foil[max(0, v - 1)]     # faint bands, staggered between the pipes
                px(im, x, y0 + j, c)
        if r.random() < 0.35:                                    # here and there the foil is torn back: rusty pipe
            x = r.randrange(1, 13)
            for dx in range(r.randint(2, 3)):
                if x + dx < 16: px(im, x + dx, y0 + 1, P["rust"][2]); px(im, x + dx, y0 + 2, P["rust"][1])
    if post:
        for x in range(3, 13): px(im, x, 9, P["concrete"][3]); px(im, x, 10, P["concrete"][1])   # crossbeam
        for y in range(11, h + 9):
            for k in range(3): px(im, 6 + k, y, P["concrete"][3 - k])
    import numpy as np
    a = np.array(im)                                            # outline top/bottom only: the run continues sideways
    for x in range(16):
        for y in range(16):
            if a[y, x, 3] == 0 and ((y + 1 < a.shape[0] and a[y + 1, x, 3]) or (y > 0 and a[y - 1, x, 3])):
                a[y, x] = (24, 25, 26, 255) if y < 10 else a[y, x]
    return Prop(Image.fromarray(a, "RGBA"), (8, h + 8), None, False)

def floorpipe(seed):
    """Batch 99: a pipe lying along a duct floor on brackets (floor plans 'p'; the overhead line is `pipe`)."""
    r = rng(seed); im = blank(16, 7)
    for x in range(16):
        for j in range(3): px(im, x, 2 + j, P["steel"][3 - j] if r.random() > .1 else P["rust"][2])
    for x in (3, 11): px(im, x, 1, P["steel"][1]); px(im, x, 5, P["steel"][0]); px(im, x, 6, P["steel"][0])
    return Prop(im, (8, 6), None, False)

def roots(seed):
    """Batch 100: roots hanging from a cave roof (Root Hall) - dark strands down to the floor, pale tips; you push through them."""
    r = rng(seed); im = blank(14, 22)
    for k in range(r.randint(5, 8)):
        x = r.randrange(1, 13); top = r.randint(0, 6); ln = r.randint(12, 21 - top)
        for y in range(top, top + ln):
            xx = x + (1 if (y // 5) % 2 and k % 2 else 0)
            px(im, xx, y, P["wood"][1] if y < top + ln - 2 else P["drygrass"][2])
    return finish(im, (7, 21), shadow=(10, 3, 1, -1), tall=True)

def tires(seed):
    """Batch 93 (refs: ArtSource/refs/tires): dumped car tyres, 0.65 m across - a stack of three (round-ended bands with
    tread lines, the top one showing its hole) and one stood on edge against it."""
    c = P["tire"]; im = blank(20, 15)
    def band(cx, ytop, rx, h):                                     # one stacked tyre: a short wide cylinder
        for y in range(ytop, ytop + h):
            for x in range(int(cx - rx), int(cx + rx) + 1):
                edge = abs(x - cx) / rx
                if edge > 0.92 and (y == ytop or y == ytop + h - 1): continue        # rounded ends
                v = 1 if edge > 0.75 else 3 if y == ytop else 2
                if y == ytop + h - 1: v = 0
                if y == ytop + 1 and edge < 0.75 and x % 2: v = 1                      # tread
                px(im, x, y, c[v])
    def ring(cx, cy, rx, ry, holeA, lit):                          # a ring seen face-on or from above
        for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
            for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
                e = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
                if e <= 1: px(im, x, y, (0, 0, 0, 0) if e < holeA else c[3] if (y < cy if lit == "top" else x < cx) else c[2] if e < 0.7 else c[1])
    for k in range(3): band(7, 12 - k * 3, 6, 3)                   # stack of three
    ring(7, 4.5, 6, 2.6, 0.22, "top")                               # top tyre, hole showing the dark inside
    for y in range(4, 6):
        for x in range(5, 10):
            if ((x - 7) / 6) ** 2 + ((y - 4.5) / 2.6) ** 2 < 0.22: px(im, x, y, c[0])
    ring(15.5, 9.5, 3.4, 5, 0.18, "side")                           # one stood on edge against the stack
    return finish(im, (9, 14), shadow=(20, 5, 1, -1))

# ------------------------------------------------------------------ boxes (top face + front face)
def box(w, d, h, top, front, detail=None):
    """w wide, top face d deep, front face h tall. top/front: callable (im, x0, y0, w, h) painters"""
    im = blank(w, d + h)
    top(im, 0, 0, w, d); front(im, 0, d, w, h)
    if detail: detail(im, d)
    return im

def flat(c, edge=None, bottom=None, left=None):
    def f(im, x0, y0, w, h):
        for y in range(h):
            for x in range(w):
                col = c
                if edge and y == 0: col = edge
                if bottom and y == h - 1: col = bottom
                if left and x == 0 and y > 0: col = left
                px(im, x0 + x, y0 + y, col)
    return f

def crate(seed):
    c = P["wood"]; r = rng(seed)
    def top(im, x0, y0, w, h):
        flat(c[3], edge=c[4])(im, x0, y0, w, h)
        for x in range(x0 + 3, x0 + w, 4): [px(im, x, y0 + y, c[2]) for y in range(1, h)]
    def front(im, x0, y0, w, h):
        flat(c[2], bottom=c[0], left=c[3])(im, x0, y0, w, h)
        for y in (y0 + 3, y0 + 6): [px(im, x0 + x, y, c[1]) for x in range(w)]
        for k in range(1, h - 1): px(im, x0 + k * (w - 1) // (h - 1), y0 + k, c[3])       # diagonal brace
        for (x, y) in ((1, 1), (w - 2, 1), (1, h - 2), (w - 2, h - 2)): px(im, x0 + x, y0 + y, P["steel"][3])
    im = box(14, 6, 10, top, front)
    return finish(im, (7, 15), shadow=(14, 5, 2, -1))

def lockers(seed):
    s = P["olive"] if seed % 2 else P["steel"]
    def top(im, x0, y0, w, h): flat(s[3], edge=s[4])(im, x0, y0, w, h)
    def front(im, x0, y0, w, h):
        flat(s[2], bottom=s[0], left=s[3])(im, x0, y0, w, h)
        for x in (x0 + 5, x0 + 10): [px(im, x, y0 + y, s[0]) for y in range(h)]
        for x in (x0 + 2, x0 + 7, x0 + 12):
            for y in (y0 + 2, y0 + 3): px(im, x, y, s[0])                               # vents
            px(im, x + 1, y0 + 9, s[4])                                                   # handles
    return finish(box(15, 4, 18, top, front), (7, 21), shadow=(15, 5, 2, -1), tall=True)

def block(seed):
    """Batch 93: precast foundation blocks (2.4 x 0.6 x 0.6 m), the grey bricks every abandoned building site has - two
    stacked out of line, steel lifting loops on top, the lower one sunk in the dirt."""
    c = P["concrete"]; r = rng(seed); im = blank(22, 13)
    def slab(x0, y0, w):
        for y in range(3): [px(im, x0 + x, y0 + y, c[4] if y == 0 else c[3]) for x in range(w)]
        for y in range(3, 8): [px(im, x0 + x, y0 + y, c[3] if x == 0 else c[1] if y == 7 else c[2]) for x in range(w)]
        for lx in (x0 + 3, x0 + w - 4):                            # lifting loops
            px(im, lx, y0, P["rust"][2]); px(im, lx + 1, y0, P["rust"][1])
        for _ in range(3): px(im, x0 + r.randrange(w), y0 + r.randrange(3, 7), c[1])
    slab(0, 5, 20); slab(2 + seed % 2, 0, 19)
    for x in range(0, 21, 2): px(im, x, 12, P["mud"][1] if "mud" in P else c[0])
    return finish(im, (11, 12), shadow=(21, 5, 1, -1))

def jersey(seed):
    """Batch 93 (refs: ArtSource/refs/jersey): a precast road barrier segment, 2 m long, 0.8 m tall - a narrow top, a short
    upright face, then the wide sloped foot that catches the light; a lifting slot, rain streaks, a chipped corner."""
    c = P["concrete"]; r = rng(seed); W = 18; im = blank(W, 11)
    rows = [4, 4, 3, 2, 2, 2, 3, 3, 3, 2, 1]                     # top (2 rows), upright face, sloped foot, ground line
    for y, v in enumerate(rows):
        inset = 0 if y > 5 else 2 if y < 2 else 1                 # the top is narrower than the foot
        for x in range(inset, W - inset):
            px(im, x, y, c[v] if not (x == inset and 1 < y < 10) else c[min(4, v + 1)])
    for x in range(7, 11): px(im, x, 9, c[0]); px(im, x, 10, c[0])   # the lifting slot through the foot
    for _ in range(4):                                            # rain streaks down the face
        x = r.randrange(2, W - 2)
        for y in range(2, r.randint(5, 9)): px(im, x, y, c[1])
    if seed % 2: px(im, W - 1, 0, (0, 0, 0, 0)); px(im, W - 2, 0, (0, 0, 0, 0)); px(im, W - 1, 1, (0, 0, 0, 0))   # chipped corner
    for _ in range(3): px(im, r.randrange(W), r.randrange(6, 10), P["moss"][0])
    return finish(im, (W // 2, 10), shadow=(W, 4, 1, -1))

def sandbags(seed):
    c = P["sandbag"]; im = blank(16, 11); r = rng(seed)
    for row, (y, off) in enumerate(((6, 0), (3, 3), (0, 1))):
        for x0 in range(-off, 16, 6):
            for yy in range(4):
                for xx in range(6):
                    x = x0 + xx
                    if 0 <= x < 16 and not (xx in (0, 5) and yy in (0, 3)):
                        v = 3 if yy == 0 else 2 if yy < 3 else 1
                        if xx == 5: v = 0
                        px(im, x, y + yy + (1 if row == 0 else 0), c[v])
    return finish(im, (8, 10), shadow=(16, 4, 2, -1))

def sandbags_v(seed):
    """Batch 95 (Yan: "cover like this needs to be drawn sideways too"): a north-south run of sandbags. Seen in 3/4 a wall
    running away from the camera shows its top running up the screen (bags laid lengthwise, two side by side, staggered);
    only the southern end shows a front face, and each tile's top covers the face of the one north of it."""
    c = P["sandbag"]; W, T, Hf = 10, 16, 7; im = blank(W, T + Hf)
    for col, (x0, off) in enumerate(((0, 0), (5, 3))):
        for y0 in range(-6 + off + (seed % 2) * 3, T, 6):
            for y in range(y0, y0 + 6):
                if not 0 <= y < T: continue
                for xx in range(5):
                    if xx in (0, 4) and y in (y0, y0 + 5): continue      # rounded ends
                    v = 0 if y == y0 + 5 else 3 if xx < 2 and col == 0 or xx == 0 else 2
                    if xx == 4: v = 1
                    px(im, x0 + xx, y, c[v])
    for y in range(T, T + Hf):                                       # the south end's face: three courses of bag ends
        for x in range(W):
            k = (y - T) % 3
            v = 2 if k == 1 else 3 if k == 0 else 1
            if y == T + Hf - 1: v = 0
            if x in (0, W - 1) and k == 0: continue
            px(im, x, y, c[v])
    return finish(im, (W // 2, T + Hf - 1), shadow=(11, 4, 1, -1))

def jersey_v(seed):
    """Batch 95: a road barrier running north-south: the narrow lit top down the middle, the sloped sides either side, the
    end face at the south."""
    c = P["concrete"]; W, T, Hf = 7, 16, 8; im = blank(W, T + Hf); r = rng(seed)
    for y in range(T):
        for x in range(W):
            px(im, x, y, c[4] if x in (2, 3) else c[3] if x < 2 else c[2])
        if y % 16 == 15: [px(im, x, y, c[1]) for x in range(W)]  # the joint between segments
    for y in range(T, T + Hf):                                   # end face: the jersey profile
        for x in range(W):
            inset = 2 if y < T + 3 else 1 if y < T + 5 else 0
            if inset <= x < W - inset: px(im, x, y, c[2] if y < T + Hf - 1 else c[0])
    for _ in range(3): px(im, r.randrange(W), r.randrange(T), c[1])
    return finish(im, (W // 2, T + Hf - 1), shadow=(8, 4, 1, -1))

def block_v(seed):
    """Batch 95: foundation blocks laid north-south: their tops up the screen, joints between blocks, lifting loops."""
    c = P["concrete"]; W, T, Hf = 8, 16, 6; im = blank(W, T + Hf); r = rng(seed)
    for y in range(T):
        for x in range(W): px(im, x, y, c[4] if x == 0 else c[3])
    for y in (5 + seed % 3,): [px(im, x, y, c[1]) for x in range(W)]       # joint between two blocks
    for y in (2, 11): px(im, 3, y, P["rust"][2]); px(im, 4, y, P["rust"][1])  # lifting loops
    for y in range(T, T + Hf):
        for x in range(W): px(im, x, y, c[2] if y < T + Hf - 1 else c[0])
    for _ in range(3): px(im, r.randrange(W), r.randrange(T), c[2])
    return finish(im, (W // 2, T + Hf - 1), shadow=(9, 4, 1, -1))

def corner(h_fn, v_fn, seed):
    """Batch 96: where a north-south run meets an east-west one, the corner tile draws the east-west piece with the
    north-south top rising out of it, so the two runs join instead of leaving a gap."""
    h, v = h_fn(seed), v_fn(seed)
    hw, hh = h.im.size; vw, vh = v.im.size
    top = vh - 8                                          # how far the north-south top rises above the east-west one
    im = blank(max(hw, vw), hh + top)
    vx = h.pivot[0] - v.pivot[0]
    im.alpha_composite(v.im, (max(0, vx), 0))
    im.alpha_composite(h.im, (0, top))
    return Prop(im, (h.pivot[0], h.pivot[1] + top), h.shadow, h.tall)

def rustbarrel(seed, explosive=False):
    c = P["red"] if explosive else P["rust"]; im = blank(10, 15)
    for y in range(15):
        for x in range(10):
            if y < 3:                                            # elliptic lid
                if ((x - 4.5) / 5) ** 2 + ((y - 1.5) / 1.6) ** 2 <= 1: px(im, x, y, c[3] if y < 2 else c[2])
                continue
            v = 3 if x < 3 else 2 if x < 6 else 1 if x < 8 else 0
            if y in (6, 11): v = max(0, v - 1)                   # hoops
            px(im, x, y, c[v])
    if explosive:
        for x in range(2, 8): px(im, x, 8, hx("#d8c050"))
    else:
        for _ in range(4): px(im, rng(seed).randrange(10), rng(seed + _).randrange(4, 15), P["rust"][0])
    return finish(im, (5, 14), shadow=(10, 4, 1, -1))

def machine(seed):
    s = P["olive"] if seed % 2 else P["steel"]
    def top(im, x0, y0, w, h):
        flat(s[3], edge=s[4])(im, x0, y0, w, h)
        for x in range(x0 + 3, x0 + w - 3, 2): px(im, x, y0 + 3, s[1])                   # top grille
    def front(im, x0, y0, w, h):
        flat(s[2], bottom=s[0], left=s[3])(im, x0, y0, w, h)
        ImageDraw.Draw(im).rectangle([x0 + 3, y0 + 3, x0 + 9, y0 + 8], fill=s[1])
        px(im, x0 + 13, y0 + 4, hx("#c84a32")); px(im, x0 + 15, y0 + 4, hx("#7fc060"))      # indicator lamps
        for x in range(x0 + 12, x0 + 18): px(im, x, y0 + 7, s[0])
    return finish(box(20, 8, 13, top, front), (10, 20), shadow=(20, 6, 2, -1), tall=True)

def engine(seed):
    s = P["steel"]; im = blank(16, 12)
    ImageDraw.Draw(im).rectangle([1, 3, 14, 11], fill=s[2])
    for x in range(2, 14, 3): ImageDraw.Draw(im).rectangle([x, 0, x + 1, 3], fill=s[3])      # cylinders
    for y in range(4, 11): px(im, 1, y, s[3]); px(im, 14, y, s[1])
    ImageDraw.Draw(im).line([(1, 11), (14, 11)], fill=s[0])
    px(im, 6, 7, P["rust"][2]); px(im, 7, 7, P["rust"][2])
    return finish(im, (8, 11), shadow=(16, 4, 2, -1))

def pipes(seed):
    s = P["rust"] if seed % 2 else P["steel"]; im = blank(16, 10)
    for y0 in (1, 5):
        for x in range(16):
            for yy in range(4): px(im, x, y0 + yy, s[3] if yy == 0 else s[2] if yy < 3 else s[0])
    for x in (3, 12):                                              # flanges + support
        for y in range(1, 10): px(im, x, y, s[1])
    return finish(im, (8, 9), shadow=(16, 4, 2, -1))

def pillar(seed):
    """Batch 93 (refs: ArtSource/refs/pillar): what's left of a reinforced-concrete column (0.4 m, 2.4 m tall) - a broken
    top with the rebar bent out of it, spalled corners, chunks at its foot."""
    c = P["concrete"]; r = rng(seed); im = blank(14, 30)
    top = [5, 4, 6, 5, 3, 4, 6]                                   # the broken top edge, per column of pixels
    for x in range(3, 10):
        for y in range(top[x - 3], 27):
            v = 3 if x < 5 else 2 if x < 8 else 1
            px(im, x, y, c[v])
        px(im, x, top[x - 3], c[4])
    for x, ln, lean in ((4, 5, -1), (8, 4, 1), (6, 3, 0)):         # rebar sticking up, bent
        y0 = top[x - 3]
        for k in range(ln): px(im, x + (lean if k > 2 else 0), y0 - k, P["rust"][2] if k % 2 else P["rust"][1])
    for _ in range(5):                                             # spalled patches
        x, y = r.randrange(3, 10), r.randrange(8, 25); px(im, x, y, c[0]); px(im, x, y + 1, c[1])
    for x in (2, 3, 10, 11, 12):                                   # chunks at the foot
        px(im, x, 27, c[2]); px(im, x, 28, c[1])
    return finish(im, (7, 28), shadow=(13, 5, 1, -1), tall=True)

def desk(seed):
    c = P["wood"]
    def top(im, x0, y0, w, h): flat(c[3], edge=c[4])(im, x0, y0, w, h)
    def front(im, x0, y0, w, h):
        flat(c[1], bottom=c[0])(im, x0, y0, w, h)
        ImageDraw.Draw(im).rectangle([x0 + 2, y0 + 1, x0 + 7, y0 + 3], fill=c[2])          # drawer
        px(im, x0 + 4, y0 + 2, P["steel"][4])
        for y in range(y0 + 4, y0 + h): px(im, x0 + w - 2, y, c[0])
    im = box(16, 6, 7, top, front)
    px(im, 9, 1, hx("#c8c0a8")); px(im, 10, 1, hx("#c8c0a8"))        # papers
    return finish(im, (8, 12), shadow=(16, 4, 2, -1))

def desk_flip(face):
    """Batch 103: the desk shoved onto its side; `face` = the side its top now faces (the side it shields from).
    s: the top stands as a slab facing the camera, the drawers lie behind it; n: we see the underside, drawers and a leg
    toward us; e/w: the slab seen edge-on along the east/west side, the drawers lying on the other side."""
    c = P["wood"]; im = blank(16, 13); d = ImageDraw.Draw(im); paper = hx("#c8c0a8")
    if face in "ns":
        if face == "s":
            d.rectangle([0, 0, 15, 3], fill=c[1]); d.rectangle([2, 1, 7, 2], fill=c[2]); px(im, 4, 1, P["steel"][4])   # drawers, lying
            d.line([(0, 4), (15, 4)], fill=c[4])                                     # the table's edge, up top now
            d.rectangle([0, 5, 15, 12], fill=c[3])                                   # the top, standing
            for x in (3, 8, 12): px(im, x, 6 + x % 3, c[2]); px(im, x + 1, 9 + x % 2, c[2])   # grain
            d.line([(0, 12), (15, 12)], fill=c[1]); px(im, 13, 7, c[0]); px(im, 13, 8, c[0])   # a bullet-worn dent
        else:
            d.line([(0, 0), (15, 0)], fill=c[4]); d.rectangle([0, 1, 15, 7], fill=c[2])     # the underside, standing
            for x in (1, 14): d.line([(x, 1), (x, 7)], fill=c[1])                    # the rails
            d.rectangle([1, 7, 7, 12], fill=c[1]); d.rectangle([2, 8, 6, 9], fill=c[3]); px(im, 4, 10, P["steel"][4])   # the drawer box toward us
            d.line([(1, 12), (7, 12)], fill=c[0])
            d.line([(13, 8), (13, 12)], fill=c[1]); px(im, 13, 12, c[0])             # a leg sticking out
        px(im, 10 if face == "s" else 9, 12, paper)
    else:
        # like Enter the Gungeon's side-flipped tables: the top seen edge-on as a thin upright strip on the shielded side,
        # and the same leg and drawer pedestal the north view shows sticking out of its back as stubs. Drawn facing east,
        # mirrored for west.
        d.rectangle([12, 0, 14, 12], fill=c[3])
        d.line([(12, 1), (12, 12)], fill=c[2])                                   # the underside, in its own shade
        d.line([(14, 0), (14, 12)], fill=c[4]); d.line([(12, 0), (14, 0)], fill=c[4])   # the lit edge
        d.line([(12, 12), (14, 12)], fill=c[0])
        d.line([(9, 2), (11, 2)], fill=c[1]); d.line([(9, 3), (11, 3)], fill=c[0])        # the leg, a stub
        d.rectangle([7, 7, 11, 10], fill=c[1]); d.line([(7, 7), (11, 7)], fill=c[2])     # the drawer pedestal
        px(im, 8, 9, P["steel"][4]); d.line([(7, 11), (11, 11)], fill=c[0])
        px(im, 4, 12, paper); px(im, 5, 12, paper)
        if face == "w": im = im.transpose(Image.FLIP_LEFT_RIGHT)
    return finish(im, (8, 12), shadow=(16, 4, 2, -1))

def armchair(seed):
    c = ramp("#3a2a2a", "#4e3434", "#6a4440", "#7e5650"); im = blank(14, 13)
    ImageDraw.Draw(im).rectangle([1, 0, 12, 5], fill=c[2]); ImageDraw.Draw(im).rectangle([0, 4, 13, 12], fill=c[1])
    ImageDraw.Draw(im).rectangle([2, 5, 11, 8], fill=c[3]); ImageDraw.Draw(im).line([(0, 12), (13, 12)], fill=c[0])
    px(im, 5, 6, hx("#c8b890")); px(im, 6, 6, hx("#c8b890"))         # stuffing poking out
    return finish(im, (7, 12), shadow=(14, 4, 1, -1))

def car(seed, w=34):
    """wreck seen from the side (long axis horizontal): roof strip on top, body band, wheels"""
    body = [P["rust"], ramp("#2c3a40", "#3c4e56", "#4f6670", "#66808a"), ramp("#3a3a2a", "#4e4e38", "#66664a", "#7e7e5c"),
            ramp("#4a2422", "#62302c", "#7e3e36", "#9a5040")][seed % 4]
    im = blank(w, 20); d = ImageDraw.Draw(im)
    d.rectangle([1, 7, w - 2, 15], fill=body[2])                          # body side
    d.line([(1, 7), (w - 2, 7)], fill=body[3])
    d.rectangle([7, 1, w - 10, 7], fill=body[1])                           # cabin
    d.line([(8, 1), (w - 11, 1)], fill=body[3])
    d.rectangle([9, 2, 15, 6], fill=P["water"][1]); d.rectangle([17, 2, w - 12, 6], fill=P["water"][1])    # windows (dark, one broken)
    px(im, 10, 2, P["water"][3]); px(im, 18, 2, P["water"][3])
    d.line([(1, 15), (w - 2, 15)], fill=body[0])
    for x in (16,): d.line([(x, 8), (x, 14)], fill=body[1])                 # door seam
    for wx in (7, w - 9):                                                    # wheels (one flat)
        disc(im, wx, 16, 3.2, P["tire"][1]); disc(im, wx, 16, 1.4, P["steel"][3])
    for _ in range(6): px(im, rng(seed).randrange(2, w - 2) if _ == 0 else rng(seed + _).randrange(2, w - 2), rng(seed * 3 + _).randrange(8, 15), P["rust"][1])
    px(im, 1, 9, hx("#c8a040")); px(im, w - 2, 9, hx("#8a2a20"))            # lamps
    return finish(im, (w // 2, 19), shadow=(w, 6, 2, -1), tall=True)

def container(seed, w=46):
    c = [P["rust"], ramp("#252f31", "#323f42", "#435255", "#566668"), ramp("#2c3224", "#3a4130", "#4a523d", "#5d644d")][seed % 3]   # faded, weathered
    def top(im, x0, y0, ww, h):
        flat(c[3], edge=c[3])(im, x0, y0, ww, h)
        for x in range(x0 + 2, x0 + ww, 6): [px(im, x, y0 + y, c[2]) for y in range(h)]
    def front(im, x0, y0, ww, h):
        flat(c[2], bottom=c[0], left=c[3])(im, x0, y0, ww, h)
        for x in range(x0 + 1, x0 + ww - 1, 2): [px(im, x, y0 + y, c[1]) for y in range(1, h - 1)]       # corrugation
        r = rng(seed)
        for _ in range(8): px(im, x0 + r.randrange(ww), y0 + r.randrange(h), P["rust"][1])
    im = box(w, 8, 18, top, front)
    return finish(im, (w // 2, 25), shadow=(w, 6, 3, -1), tall=True)

def culvert(seed):
    """Batch 93 (refs: ArtSource/refs/culvert): a 1 m concrete drain pipe coming out of the bank - a headwall slab, the
    pipe's lip, darkness inside, a trickle and silt below."""
    c = P["concrete"]; im = blank(22, 18)
    for y in range(0, 4):                                          # grass bank over the headwall
        for x in range(1, 21): px(im, x, y, P["grass"][2] if (x + y) % 4 else P["grass"][1])
    for y in range(4, 15):                                         # headwall
        for x in range(2, 20): px(im, x, y, c[4] if y == 4 else c[2] if x > 2 else c[3])
    disc(im, 11, 10, 5, c[3]); disc(im, 11, 10, 4, P["cave"][0]); disc(im, 11, 11.5, 2.5, hx("#101214"))   # the pipe's lip, the dark inside
    for y in range(13, 18):                                        # trickle and silt
        for x in range(8, 15):
            if abs(x - 11) <= (y - 12): px(im, x, y, P["water"][1] if abs(x - 11) < 2 else P["mud"][1] if "mud" in P else c[0])
    return finish(im, (11, 16), shadow=(20, 3, 1, -1))

def hatch(seed):
    """Batch 93 (refs: ArtSource/refs/hatch, a Cold-War observer post): a bunker's way down - a low concrete collar with the
    steel hatch box sitting on it (flaking green paint, hinges at the back, a handle), and a vent pipe with a mushroom cap."""
    c = P["concrete"]; s = P["steel"]; im = blank(18, 16)
    for y in range(9, 15):                                          # the collar: top face, then front face in shade
        for x in range(0, 15): px(im, x, y, c[4] if y == 9 else c[3] if y < 11 else c[1] if y < 14 else c[0])
    g = (hx("#2f4436"), hx("#3f5848"), hx("#4f6b5a"), hx("#6a8a74"))
    for y in range(4, 11):                                          # the hatch box: lid on top, its front below
        for x in range(3, 12):
            v = 3 if y == 4 else 2 if y < 8 else 1 if y < 10 else 0
            if (x * 5 + y * 3) % 11 == 0: px(im, x, y, s[2])        # paint flaked to bare steel
            else: px(im, x, y, g[v])
    for x in (4, 10): px(im, x, 3, s[1])                            # hinges at the back
    px(im, 7, 8, s[4]); px(im, 8, 8, s[3])                          # handle on the front
    for y in range(3, 13): px(im, 15, y, s[2]); px(im, 16, y, s[1])     # vent pipe
    for x in range(14, 18): px(im, x, 2, s[3]); px(im, x, 3, s[1])     # mushroom cap
    return finish(im, (8, 15), shadow=(17, 4, 1, -1))

def campfire(seed):
    im = blank(14, 12); r = rng(seed)
    for k in range(8):
        a = k / 8 * 6.283; disc(im, 7 + np.cos(a) * 5, 8 + np.sin(a) * 2.5, 1.4, P["rock"][2 + k % 2])
    for (x, y) in ((5, 8), (7, 7), (9, 8)): px(im, x, y, P["wood"][1]); px(im, x + 1, y, P["wood"][1])
    for (x, y, c) in ((6, 5, "#f2741c"), (7, 4, "#ffc845"), (8, 5, "#f2741c"), (7, 6, "#fff4c0"), (6, 6, "#b8360c"), (8, 6, "#b8360c"), (7, 3, "#f2741c")):
        px(im, x, y, hx(c))
    return finish(im, (7, 11))

def stones(seed):
    im = blank(14, 8); r = rng(seed)
    for _ in range(4): disc(im, r.uniform(3, 11), r.uniform(3, 6), r.uniform(1.4, 2.4), P["rock"][r.randint(2, 3)])
    return finish(im, (7, 7))

def antenna(seed):
    s = P["steel"]; im = blank(13, 50)
    for y in range(50):
        hw = 1 + (y / 49) * 5
        xl, xr = int(6 - hw), int(6 + hw)
        px(im, xl, y, s[3]); px(im, xr, y, s[1])
        if y % 6 == 0: [px(im, x, y, s[2]) for x in range(xl, xr + 1)]
        if y % 6 == 3: px(im, (xl + xr) // 2, y, s[2])
    px(im, 6, 0, hx("#c84a32"))
    return finish(im, (6, 49), shadow=(14, 5, 3, -1), tall=True)

def fountain(seed):
    """Batch 93 (refs: ArtSource/refs/fountain): a dry town-square fountain, 3 m across - a low round rim, the empty basin
    with a black puddle and dead leaves, the nozzle hub in the middle with its spokes."""
    c = P["concrete"]; r = rng(seed); W, H = 32, 20; im = blank(W, H)
    cx, cy, rx, ry = 15.5, 8.5, 15, 8
    for y in range(H):
        for x in range(W):
            e = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
            if e <= 1:
                px(im, x, y, c[4] if y < cy else c[3])             # the rim's top
            if e <= 0.72: px(im, x, y, c[1] if y < cy - 2 else c[2])   # the basin floor, its far side in the rim's shade
    for x in range(W):                                             # the rim's front face below the ellipse
        for y in range(H):
            if im.getpixel((x, y))[3] and (y + 1 >= H or not im.getpixel((x, y + 1))[3]) and y > cy:
                for k in range(1, 4):
                    if y + k < H: px(im, x, y + k, c[2] if k < 3 else c[0])
    for y in range(H):                                             # a black puddle in the low corner
        for x in range(W):
            if ((x - 19) / 5) ** 2 + ((y - 11) / 2.2) ** 2 < 1 and im.getpixel((x, y))[:3] in (c[1][:3], c[2][:3]): px(im, x, y, P["water"][0])
    for a in range(0, 360, 60):                                    # spokes and nozzles
        import math
        for k in range(2, 6): px(im, int(cx + math.cos(math.radians(a)) * k), int(cy + math.sin(math.radians(a)) * k * 0.55), c[3])
    disc(im, cx, cy, 1.8, c[4]); px(im, int(cx), int(cy) - 2, P["steel"][3])
    for _ in range(9): px(im, r.randrange(6, 26), r.randrange(4, 13), r.choice((P["rust"][2], P["rust"][3], hx("#7a6a3a"))))   # dead leaves
    return finish(im, (16, 19), shadow=(32, 6, 1, -1))

def barrierpole(seed):
    im = blank(18, 12)
    ImageDraw.Draw(im).rectangle([1, 3, 3, 11], fill=P["steel"][2])
    for x in range(3, 18): px(im, x, 4, hx("#c8c0a8") if (x // 3) % 2 else hx("#a52c22")); px(im, x, 5, P["steel"][1])
    return finish(im, (9, 11), shadow=(16, 3, 2, -1))

def fence(seed, torn=False):
    """chain-link / plank fence segment for a horizontal run (one tile): post on the left, mesh to the right"""
    s = P["steel"]; im = blank(16, 15); r = rng(seed)
    for y in range(15): px(im, 0, y, s[3]); px(im, 1, y, s[1])                 # post
    for y in range(2, 13):
        for x in range(2, 16):
            if torn and (y > 9 - (x % 5) and x > 8): continue
            if (x + y) % 3 == 0 or (x - y) % 3 == 0: px(im, x, y, (110, 116, 118, 220))
    for x in range(2, 16): px(im, x, 1, s[3])                                   # top rail
    return finish(im, (8, 14))

def fence_v(seed):
    """a fence running north-south is seen edge-on: a thin rail with posts"""
    s = P["steel"]; im = blank(4, 22)
    for y in range(22): px(im, 1, y, s[2]); px(im, 2, y, s[1])
    for y in (0, 11): [px(im, x, y + k, s[3]) for x in range(4) for k in range(2)]
    return finish(im, (2, 21))

def rubble(seed):
    r = rng(seed); im = blank(16, 10); c = P["concrete"]
    for _ in range(9):
        x, y, s = r.randrange(0, 13), r.randrange(1, 8), r.randint(2, 4)
        for yy in range(s - 1):
            for xx in range(s): px(im, x + xx, y + yy, c[3] if yy == 0 else c[1])
    for _ in range(3): px(im, r.randrange(16), r.randrange(10), P["brick"][2])
    return finish(im, (8, 9))

def pole(seed):
    """concrete power pole with a crossbar (Batch 31, roadsides)"""
    w, h = 18, 64; im = blank(w, h)
    for y in range(6, h):
        px(im, 8, y, P["concrete"][3]); px(im, 9, y, P["concrete"][2]); px(im, 10, y, P["concrete"][1])
    for x in range(1, 17): px(im, x, 9, P["deadwood"][2]); px(im, x, 10, P["deadwood"][1])
    for x in (2, 15): px(im, x, 8, P["concrete"][4])
    return finish(im, (9, h - 1), shadow=(6, 3, 1, 0), tall=True)

def watchtower(seed):
    """Batch 73 (Yan's reference: a timber watchtower): four tall legs with X-bracing, a zig-zag stair up the front,
    a railed deck and a plank cabin with a window under a hipped roof. 3 tiles wide, ~6 tall; the map gives it a
    3x2 footprint (the legs) and the deck/cabin stand above it."""
    W, H = 50, 100; im = blank(W, H); w = P["wood"]; d = P["deadwood"]; r = rng(seed)
    dr = ImageDraw.Draw(im)
    def line(x0, y0, x1, y1, c, t=1):
        dr.line([x0, y0, x1, y1], fill=c, width=t)
    # back legs (further, darker), then bracing between them
    for x0, x1 in ((10, 12), (39, 37)):
        line(x0, 98, x1, 40, d[1], 2)
    for y0, y1 in ((92, 68), (68, 44)):
        line(11, y0, 38, y1, d[1]); line(38, y0, 11, y1, d[1])
    # deck underside beams
    for y in (41, 42): line(5, y, 44, y, d[2])
    # front legs with a lit left edge
    for (xb, xt) in ((4, 8), (45, 41)):
        line(xb, 99, xt, 42, w[1], 3); line(xb - 1 if xb < 20 else xb + 1, 99, xt - 1 if xb < 20 else xt + 1, 42, w[3] if xb < 20 else w[0])
    # front X-bracing and ring beams
    for y in (70, 96): line(5, y, 44, y, w[2])
    for y0, y1 in ((96, 70), (70, 44)):
        line(6, y0, 43, y1, d[2]); line(43, y0, 6, y1, d[1])
    # the stair: two flights zig-zagging up the front with a landing
    def flight(x0, y0, x1, y1):
        line(x0, y0 + 1, x1, y1 + 1, w[1], 2)                # stringer
        n = max(3, abs(y1 - y0) // 3)
        for i in range(n + 1):
            t = i / n; sx, sy = int(x0 + (x1 - x0) * t), int(y0 + (y1 - y0) * t)
            line(sx - 3, sy, sx + 3, sy, w[4]); line(sx - 3, sy + 1, sx + 3, sy + 1, w[2])   # treads
        line(x0, y0 - 6, x1, y1 - 6, w[2])                   # hand rail
    flight(40, 98, 12, 72)
    dr.rectangle([8, 70, 17, 72], fill=w[2]); line(8, 70, 17, 70, w[4])
    flight(14, 70, 38, 44)
    # deck: top face (lit), front edge, railing
    dr.rectangle([2, 34, 47, 38], fill=w[3])
    for x in range(3, 47, 4): line(x, 34, x, 38, w[2])
    dr.rectangle([2, 39, 47, 41], fill=w[1])
    for x in (2, 13, 24, 36, 47): line(x, 28, x, 38, w[2])
    line(2, 28, 47, 28, w[4]); line(2, 31, 47, 31, w[3])
    # cabin: plank walls, a dark window, a door gap on the right
    dr.rectangle([8, 14, 41, 33], fill=w[2])
    for x in range(9, 41, 3): line(x, 14, x, 33, w[1] if (x // 3) % 2 else w[2])
    dr.rectangle([17, 19, 30, 26], fill=d[0]); line(17, 19, 30, 19, d[2]); line(23, 19, 23, 26, w[3])
    dr.rectangle([34, 20, 39, 33], fill=d[0])
    line(8, 14, 8, 33, w[3]); line(41, 14, 41, 33, w[0])
    # hipped roof with an overhang: lit top slope, shaded eave
    for y in range(2, 15):
        t = (y - 2) / 12; xl, xr = int(20 - t * 18), int(29 + t * 18)
        c = w[1] if y < 13 else w[0]
        line(xl, y, xr, y, c)
        if y < 13 and y % 3 == 0: line(xl + 1, y, xr - 1, y, d[2])
    line(20, 2, 29, 2, w[3]); line(2, 14, 47, 14, d[1])
    return finish(im, (25, 99), shadow=(52, 9, 0, -2), tall=True)

def _zone():
    import zone_style as z
    return z

# ------------------------------------------------------------------ Batch 109: redraws from the reference library
# (ArtSource/refs/<name>/sheet.jpg), at real size: a 16 px person is 1.8 m, so a pixel is about 11 cm.

def busstop_soviet(seed):
    """refs/busstop: a Soviet roadside shelter - a cast concrete slab roof on two side walls, the back wall a faded tile
    mosaic (a sun over waves), a plank bench, moss creeping up from the ground. 3 m wide, 2.4 m to the roof."""
    c = P["concrete"]; r = rng(seed); W, H = 30, 24; im = blank(W, H); d = ImageDraw.Draw(im)
    d.rectangle([0, 0, W - 1, 3], fill=c[3]); d.line([(0, 0), (W - 1, 0)], fill=c[4])            # roof slab, top
    d.rectangle([0, 4, W - 1, 5], fill=c[2]); d.line([(0, 5), (W - 1, 5)], fill=c[1])             # its front edge
    d.rectangle([3, 6, W - 4, 20], fill=c[2]); d.rectangle([3, 6, W - 4, 8], fill=c[1])           # back wall, shaded under the roof
    sky, sun, sea, grout = hx("#5d7480"), hx("#b89a52"), hx("#3f5a66"), hx("#8e8a7e")
    for y in range(9, 17):                                                                        # the mosaic, 2 px tiles
        for x in range(6, W - 6):
            cx, cy = (W - 1) / 2, 15.5
            col = sun if (x - cx) ** 2 + ((y - cy) * 1.4) ** 2 < 16 else sea if y >= 14 else sky
            if (x % 2 == 0 or y % 2 == 1) and r.random() < .18: col = grout
            if r.random() < .06: col = c[2]                                                     # tiles fallen out
            px(im, x, y, col)
    for x in (1, 2): d.line([(x, 6), (x, 21)], fill=c[4] if x == 1 else c[3])                     # side walls
    for x in (W - 3, W - 2): d.line([(x, 6), (x, 21)], fill=c[2] if x == W - 3 else c[1])
    d.rectangle([5, 17, W - 6, 18], fill=P["wood"][2]); d.line([(5, 17), (W - 6, 17)], fill=P["wood"][3])   # bench
    for x in (6, W - 7): d.line([(x, 19), (x, 20)], fill=P["steel"][1])
    d.line([(1, 21), (W - 2, 21)], fill=c[0])
    for _ in range(14): px(im, r.randrange(1, W - 1), r.randrange(18, 21), P["olive"][1 + r.randrange(2)])   # moss at the foot
    for _ in range(3): x = r.randrange(4, W - 4); [px(im, x, y, c[1]) for y in range(6, 6 + r.randrange(3, 8))]   # damp streaks
    return finish(im, (W // 2, 21), shadow=(30, 5, 0, -1), tall=True)

def engine_block(seed):
    """refs/engine: a dumped tractor engine - valve cover with its bolts, four manifold stubs, the pulley, the oil pan,
    rust running down over old green paint. 1.4 m long."""
    paint = ramp("#2e3a2c", "#3f4d3a", "#53614a", "#6a775c"); rust = P["rust"]; r = rng(seed)
    im = blank(14, 12); d = ImageDraw.Draw(im)
    d.rectangle([2, 4, 12, 9], fill=paint[1]); d.line([(2, 4), (12, 4)], fill=paint[2])           # block
    d.rectangle([3, 1, 11, 3], fill=P["tin"][2]); d.line([(3, 1), (11, 1)], fill=P["tin"][3])     # valve cover
    for x in (4, 7, 10): px(im, x, 2, P["steel"][4])
    for x in (4, 6, 8, 10): px(im, x, 5, P["steel"][0]); px(im, x, 6, rust[1])                    # manifold stubs
    disc(im, 2, 7, 2, P["steel"][2]); px(im, 2, 7, P["steel"][0]); px(im, 1, 6, P["steel"][4])     # pulley
    d.rectangle([3, 10, 11, 10], fill=paint[0]); d.line([(4, 11), (10, 11)], fill=P["steel"][0])  # oil pan
    for _ in range(10): x, y = r.randrange(3, 12), r.randrange(4, 9); px(im, x, y, rust[2]); px(im, x, y + 1, rust[1])   # rust runs
    return finish(im, (7, 11), shadow=(14, 4, 2, -1))

def armchair_worn(seed):
    """refs/armchair: a dumped armchair - high back, rolled arms, the seat cushion sagging, stuffing out of a tear, faded
    fabric. 0.9 m wide, 1 m tall (was drawn twice too big)."""
    f = [ramp("#3a2624", "#523432", "#6a4642", "#83605a"), ramp("#2c3328", "#3d4636", "#515b47", "#69725c"),
         ramp("#3c3428", "#544a38", "#6c614a", "#857a60")][seed % 3]
    r = rng(seed); im = blank(11, 11); d = ImageDraw.Draw(im)
    d.rectangle([1, 0, 9, 5], fill=f[2]); d.line([(2, 0), (8, 0)], fill=f[3])                     # back
    for x in (0, 9): d.rectangle([x, 3, x + 1, 8], fill=f[1]); d.line([(x, 3), (x + 1, 3)], fill=f[3]); d.line([(x + 1 if x == 0 else x, 4), (x + 1 if x == 0 else x, 8)], fill=f[0])   # arms, a dark inner edge
    d.rectangle([2, 5, 8, 6], fill=f[3]); px(im, 5, 6, f[2])                                       # seat, sagging in the middle
    d.rectangle([1, 7, 9, 9], fill=f[1]); d.line([(1, 10), (9, 10)], fill=f[0])                   # front
    st = hx("#cfc4a4"); px(im, 3 + seed % 4, 2, st); px(im, 4 + seed % 4, 2, st); px(im, 4 + seed % 4, 3, hx("#a89c7e"))   # stuffing
    for _ in range(4): px(im, r.randrange(1, 10), r.randrange(1, 9), f[0])                        # stains
    return finish(im, (5, 10), shadow=(11, 3, 1, -1))

def dumpster_steel(seed):
    """refs/dumpster: a steel waste container - sloped lid (one left ajar), ribbed sides, side pockets for the lifting arms,
    rust eating up from the bottom and down from the rim. 2 m wide, 1.3 m tall."""
    c = [ramp("#26332a", "#33443a", "#46594b", "#5d7260"), ramp("#2a3440", "#36434f", "#4a5a68", "#627584"),
         ramp("#3a3328", "#4d4433", "#655a43", "#7d7155")][seed % 3]; rust = P["rust"]; r = rng(seed)
    im = blank(19, 15); d = ImageDraw.Draw(im)
    ajar = seed % 2 == 1
    d.rectangle([1, 1 if ajar else 2, 17, 5], fill=c[3]); d.line([(1, 1 if ajar else 2), (17, 1 if ajar else 2)], fill=c[3])
    if ajar: d.line([(2, 5), (16, 5)], fill=hx("#141614"))                                       # the gap under the lid
    d.line([(1, 6), (17, 6)], fill=c[3])                                                          # rim
    d.rectangle([1, 7, 17, 12], fill=c[2])
    for x in range(3, 17, 3): d.line([(x, 7), (x, 12)], fill=c[1])                               # ribs
    for x in (0, 18): d.rectangle([x, 8, x, 9], fill=c[0])                                        # lifting pockets
    d.line([(1, 13), (17, 13)], fill=c[0]); px(im, 3, 14, P["steel"][0]); px(im, 15, 14, P["steel"][0])   # skids
    for _ in range(9): x = r.randrange(1, 18); px(im, x, 12, rust[2]); px(im, x, 11, rust[1 + r.randrange(2)])   # rust from below
    for _ in range(4): x = r.randrange(2, 17); [px(im, x, y, rust[1]) for y in range(7, 7 + r.randrange(2, 5))]   # and runs from the rim
    return finish(im, (9, 14), shadow=(19, 5, 1, -1))

def lathe(seed):
    """refs/machine: a factory lathe left where it stood - the headstock with its big chuck, the long bed and the carriage,
    the tailstock, a cabinet stand; flaking machine-green paint, rust on the ways. 2.6 m long."""
    g = P["olive"] if seed % 2 else ramp("#2f3a3c", "#3f4e50", "#536567", "#6b7f80", "#8a9c9c"); rust = P["rust"]; r = rng(seed)
    im = blank(25, 15); d = ImageDraw.Draw(im)
    d.rectangle([3, 10, 8, 14], fill=g[1]); d.rectangle([17, 10, 22, 14], fill=g[1])            # cabinet stand
    d.line([(3, 14), (8, 14)], fill=g[0]); d.line([(17, 14), (22, 14)], fill=g[0])
    d.rectangle([2, 6, 23, 9], fill=g[2]); d.line([(2, 6), (23, 6)], fill=g[3])                   # bed
    d.line([(9, 7), (22, 7)], fill=P["steel"][3])                                                  # the ways, bare metal
    d.rectangle([1, 1, 7, 9], fill=g[2]); d.line([(1, 1), (7, 1)], fill=g[3]); d.line([(1, 2), (1, 9)], fill=g[3])   # headstock
    disc(im, 9, 5, 2.2, P["steel"][3]); disc(im, 9, 5, 1, P["steel"][1]); px(im, 8, 4, P["steel"][4])   # chuck, bright steel
    d.rectangle([12, 4, 15, 7], fill=g[1]); px(im, 13, 3, P["steel"][3]); px(im, 14, 3, P["steel"][1])   # carriage + tool post
    d.rectangle([19, 4, 22, 7], fill=g[2]); d.line([(18, 5), (19, 5)], fill=P["steel"][3])        # tailstock
    px(im, 3, 4, hx("#c84a32")); px(im, 5, 4, P["steel"][4])                                      # stop button, a dial
    for _ in range(4): x, y = r.randrange(2, 8), r.randrange(2, 9); px(im, x, y, rust[2])          # flaking paint, a few spots
    d.line([(2, 9), (23, 9)], fill=g[0])                                                           # the bed's shadowed lip
    return finish(im, (12, 14), shadow=(25, 6, 2, -1), tall=True)

def lamp_concrete(seed):
    """refs/lamp: a Soviet street lamp - a tapered precast concrete pole with the square holes cast through it, a steel arm
    bent out over the road, a shallow lamp head with its glass long gone dark; some lean. 6.5 m tall."""
    c = P["concrete"]; s = P["steel"]; r = rng(seed); W, H = 17, 60; im = blank(W, H)
    lean = (seed % 3) - 1                                                     # -1, 0 or 1 px over the height
    for y in range(8, H):
        x = 6 + round(lean * (1 - (y - 8) / (H - 9)))
        wide = 3 if y > H * 0.55 else 2
        for k in range(wide): px(im, x + k, y, c[4] if k == 0 else c[3] if k < wide - 1 else c[2])
        if (y - 12) % 7 == 0 and 12 < y < H - 6: px(im, x + (1 if wide == 3 else 0), y, c[0]); px(im, x + (1 if wide == 3 else 0), y + 1, c[0])   # cast holes
    for x in range(7, 15): px(im, x, 7 - (1 if x > 12 else 0), s[2])          # the arm, bending out
    px(im, 7, 8, s[1]); px(im, 8, 7, s[3])
    for x in range(12, 17): px(im, x, 7, s[1])                                # lamp head
    for x in range(13, 16): px(im, x, 8, hx("#3b3d33"))                       # dead glass
    for y in range(H - 4, H): px(im, 6 + (1 if lean > 0 else 0), y, c[1])      # damp at the foot
    return finish(im, (7, H - 1), tall=True)

def stones_pile(seed):
    """refs/stones: field stones cleared off a meadow into a low pile - six or seven rounded stones, lit tops, a darker
    one or two, grass between them. 1.4 m across."""
    rk = P["rock"]; r = rng(seed); im = blank(14, 9)
    spots = [(3, 6), (6, 6.5), (9.5, 6), (11.5, 6.8), (5, 4), (8.5, 4), (7, 2.5)]
    for i, (x, y) in enumerate(spots[: 6 + seed % 2]):
        x += r.uniform(-.5, .5); rad = r.uniform(1.4, 2.1) if y > 5 else r.uniform(1.2, 1.7)
        disc(im, x, y, rad, rk[0]); disc(im, x - .3, y - .3, rad - .8, rk[r.choice((2, 2, 3))])   # a dark rim keeps the stones apart
        px(im, int(x - .5), int(y - rad + .9), rk[4] if len(rk) > 4 else rk[3])   # lit top
    for _ in range(3): px(im, r.randrange(2, 12), r.randrange(6, 9), P["olive"][2])
    return finish(im, (7, 8))

# ------------------------------------------------------------------ Batch 124: stashes (Yan: "they are a tiny wooden dot instead of a
# chest, box, bag"). Each has a closed look and an "_open" look the tile switches to once you've been through it. Sizes against
# the 16 px person (1.8 m): a footlocker is 0.9 m long, a safe 0.8 m tall.
DARK = hx("#141412")

def footlocker(seed, open_=False):
    """an army footlocker: olive drab, darker bands round the ends, two latches, a stencilled patch. Open: the lid stands up
    behind it, the inside dark with a rag left in it."""
    c = P["olive"]; s = P["steel"]; im = blank(13, 13); d = ImageDraw.Draw(im)
    top = 6
    if open_:
        d.rectangle([2, 1, 10, 4], fill=c[1]); d.line([(2, 1), (10, 1)], fill=c[3])                  # the lid tipped back: its underside
        d.rectangle([3, 2, 9, 3], fill=c[0])
        d.rectangle([1, top - 1, 11, top + 2], fill=c[3]); d.rectangle([2, top, 10, top + 2], fill=DARK)   # rim, the empty inside
        px(im, 4, top + 2, hx("#5a5040")); px(im, 5, top + 2, hx("#6a604c"))                                 # a rag left behind
    else:
        d.rectangle([1, top - 3, 11, top + 2], fill=c[3]); d.line([(1, top - 3), (11, top - 3)], fill=c[4])   # lid top
        d.line([(3, top - 3), (3, top + 2)], fill=c[2]); d.line([(9, top - 3), (9, top + 2)], fill=c[2])       # bands over the lid
    d.rectangle([1, top + 3, 11, top + 6], fill=c[2]); d.line([(1, top + 3), (11, top + 3)], fill=c[3])      # front
    d.line([(1, top + 6), (11, top + 6)], fill=c[0])
    for x in (3, 9): d.line([(x, top + 3), (x, top + 6)], fill=c[1])                                        # end bands
    for x in (5, 7): px(im, x, top + 3, s[4]); px(im, x, top + 4, s[2])                                     # latches
    px(im, 6, top + 5, hx("#a09a7a"))                                                                        # stencil
    return finish(im, (6, top + 6), shadow=(13, 4, 1, -1))

def chest(seed, open_=False):
    """a wooden trunk with iron straps and a rounded lid (refs: old village chests). Open: the lid tipped back, straw inside."""
    w = P["wood"]; s = P["steel"]; im = blank(14, 14); d = ImageDraw.Draw(im)
    top = 6
    if open_:
        d.rectangle([2, 1, 11, 4], fill=w[1]); d.line([(2, 1), (11, 1)], fill=w[3]); d.rectangle([3, 2, 10, 3], fill=w[0])   # lid back, underside
        for x in (2, 11): d.line([(x, 1), (x, 4)], fill=s[1])
        d.rectangle([1, top - 1, 12, top + 2], fill=w[3]); d.rectangle([2, top, 11, top + 2], fill=DARK)
        for x in (3, 5, 8, 10): px(im, x, top + 2, hx("#8a7a48"))                                        # straw
    else:
        d.rectangle([1, top - 3, 12, top + 2], fill=w[3]); d.line([(2, top - 4), (11, top - 4)], fill=w[4])   # the rounded lid
        for y in (top - 2, top): d.line([(1, y), (12, y)], fill=w[2])                                    # lid planks
        for x in (2, 11): d.line([(x, top - 4), (x, top + 2)], fill=s[2])                                 # iron straps
    d.rectangle([1, top + 3, 12, top + 7], fill=w[2]); d.line([(1, top + 3), (12, top + 3)], fill=w[3])
    d.line([(1, top + 5), (12, top + 5)], fill=w[1]); d.line([(1, top + 7), (12, top + 7)], fill=w[0])
    for x in (2, 11): d.line([(x, top + 3), (x, top + 7)], fill=s[2])
    px(im, 6, top + 4, s[4]); px(im, 7, top + 4, s[4]); px(im, 6, top + 5, s[1]); px(im, 7, top + 5, s[1])  # hasp
    return finish(im, (6, top + 7), shadow=(14, 4, 1, -1))

def duffel(seed, open_=False):
    """a canvas duffel bag lying on its side: a soft round body, end caps, a strap over it, a zip along the top.
    Open: flattened, the zip gaping dark."""
    c = [P["olive"], ramp("#2e2a22", "#3e382c", "#544b3a", "#6b604a", "#7e7258")][seed % 2]; im = blank(14, 9); d = ImageDraw.Draw(im)
    h = 4 if open_ else 6; y0 = 8 - h
    d.ellipse([0, y0, 13, 8], fill=c[2]); d.line([(2, y0), (11, y0)], fill=c[3])                       # body, lit top
    d.line([(2, 8), (11, 8)], fill=c[0])
    for x in (1, 12): d.line([(x, y0 + 1), (x, 7)], fill=c[1])                                         # end caps
    if open_: d.line([(3, y0 + 1), (10, y0 + 1)], fill=DARK); d.line([(4, y0 + 2), (9, y0 + 2)], fill=DARK)   # unzipped
    else:
        d.line([(3, y0 + 1), (10, y0 + 1)], fill=c[1]); px(im, 10, y0 + 1, P["steel"][4])               # zip and its pull
        d.line([(5, y0), (5, 8)], fill=c[0]); d.line([(8, y0), (8, 8)], fill=c[0])                      # the strap's loops
    return finish(im, (6, 8), shadow=(14, 4, 1, -1))

def toolbox(seed, open_=False):
    """a steel toolbox, red paint gone to rust, a handle on top. Open: the lid swung back, the tray empty."""
    r_ = P["red"]; rust = P["rust"]; s = P["steel"]; im = blank(11, 10); d = ImageDraw.Draw(im); rr = rng(seed)
    if open_:
        d.rectangle([2, 1, 8, 3], fill=r_[0]); d.line([(2, 1), (8, 1)], fill=r_[2])                         # lid swung back
        d.rectangle([1, 4, 9, 5], fill=r_[2]); d.line([(2, 5), (8, 5)], fill=DARK); px(im, 3, 5, s[3])
    else:
        d.rectangle([1, 3, 9, 5], fill=r_[2]); d.line([(1, 3), (9, 3)], fill=r_[3])
        d.line([(3, 1), (7, 1)], fill=s[3]); px(im, 3, 2, s[2]); px(im, 7, 2, s[2])                      # handle
    d.rectangle([1, 6, 9, 9], fill=r_[1]); d.line([(1, 6), (9, 6)], fill=r_[2]); d.line([(1, 9), (9, 9)], fill=r_[0])
    px(im, 5, 7, s[4])                                                                                      # catch
    for _ in range(5): px(im, rr.randrange(1, 10), rr.randrange(6, 10), rust[rr.randrange(1, 3)])
    return finish(im, (5, 9), shadow=(11, 3, 1, -1))

def cache(seed, open_=False):
    """a stash someone hid: a box under a grey-green tarp, a rope over it, two stones holding the corners.
    Open: the tarp thrown back, the box's dark inside showing."""
    t = ramp("#2c3328", "#3a4334", "#4c5643", "#5f6a53"); w = P["wood"]; st = P["rock"]; im = blank(15, 11); d = ImageDraw.Draw(im)
    if open_:
        d.rectangle([2, 3, 11, 9], fill=w[2]); d.rectangle([3, 4, 10, 6], fill=DARK); d.line([(2, 9), (11, 9)], fill=w[0])
        d.polygon([(9, 1), (14, 3), (14, 10), (11, 10)], fill=t[1]); d.line([(10, 2), (13, 3)], fill=t[3])  # the tarp, pulled aside
    else:
        d.polygon([(1, 4), (3, 1), (12, 1), (14, 4), (14, 10), (1, 10)], fill=t[2])                       # tarp over the box
        d.line([(3, 1), (12, 1)], fill=t[3]); d.line([(1, 10), (14, 10)], fill=t[0])
        for x in (4, 9): d.line([(x, 2), (x - 1, 10)], fill=t[1])                                      # folds
        d.line([(1, 6), (14, 5)], fill=hx("#8a7a58"))                                                   # the rope
    disc(im, 1.5, 9.5, 1.3, st[2]); disc(im, 13, 9.5, 1.3, st[3])                                          # weights
    return finish(im, (7, 10), shadow=(15, 4, 1, -1))

def wallsafe(seed, open_=False):
    """a squat steel safe with a keypad (the Electronic Lock): dark green-grey, a lit keypad, a bolt handle.
    Open: the door swung out to the left, a shelf inside."""
    c = ramp("#22292a", "#2f383a", "#3f4a4c", "#566265", "#6d7a7c"); im = blank(15, 15); d = ImageDraw.Draw(im)
    d.rectangle([3, 0, 13, 2], fill=c[3]); d.line([(3, 0), (13, 0)], fill=c[4])                       # top
    d.rectangle([3, 3, 13, 13], fill=c[2]); d.line([(3, 13), (13, 13)], fill=c[0]); d.line([(3, 3), (3, 13)], fill=c[3])
    if open_:
        d.rectangle([5, 4, 12, 12], fill=DARK); d.line([(5, 8), (12, 8)], fill=c[1])                   # inside, a shelf
        d.rectangle([0, 3, 2, 13], fill=c[3]); d.line([(0, 3), (0, 13)], fill=c[4])                    # the door, open
        px(im, 1, 7, hx("#7a1f1a"))                                                                    # its dead keypad light
    else:
        d.rectangle([5, 4, 12, 12], fill=c[2]); d.line([(5, 4), (12, 4)], fill=c[1]); d.line([(5, 4), (5, 12)], fill=c[1])   # door seam
        d.rectangle([7, 6, 9, 9], fill=c[0])                                                            # keypad
        for (x, y) in ((7, 6), (9, 6), (8, 7), (7, 8), (9, 8)): px(im, x, y, c[4])
        px(im, 10, 6, hx("#62d25a")); px(im, 11, 9, c[4]); px(im, 11, 10, c[4])                          # live LED, handle
    return finish(im, (8, 13), shadow=(13, 4, 1, -1), tall=False)

def pda(seed, open_=False):
    """a dropped handheld (the Encrypted PDA): a small dark case, its screen glowing cyan. Open (wiped): screen dead, cracked."""
    im = blank(7, 5); d = ImageDraw.Draw(im)
    d.rectangle([0, 0, 6, 4], fill=hx("#24292b")); d.line([(0, 4), (6, 4)], fill=hx("#15191a"))
    d.rectangle([1, 1, 4, 3], fill=hx("#1d2a2c") if open_ else hx("#4fb6c9"))
    if open_: px(im, 2, 2, hx("#6b7a7c")); px(im, 3, 1, hx("#6b7a7c"))
    else: px(im, 1, 1, hx("#a8e4ef")); px(im, 5, 2, hx("#c8443a"))
    return finish(im, (3, 4), shadow=(7, 2, 0, -1))

STASHES = {"footlocker": footlocker, "chest": chest, "duffel": duffel, "toolbox": toolbox, "cache": cache, "safe": wallsafe, "pda": pda}

PROPS = {
    # Batch 117: big trees in several shapes, the last two variants of each kept small (saplings) for a mix of sizes
    "tree": lambda s: _zone().big_birch(s) if (s - 1000) // 77 < 8 else _zone().birch(s),
    "pine": lambda s: _zone().big_spruce(s) if (s - 1000) // 77 < 6 else _zone().spruce(s),
    "deadtree": lambda s: _zone().big_snag(s) if (s - 1000) // 77 < 4 else _zone().snag(s),
    "redtree": lambda s: _zone().big_birch(s, _zone().AUTUMN) if (s - 1000) // 77 < 4 else _zone().birch(s, _zone().AUTUMN), "bush": lambda s: _zone().scrub(s), "redbush": lambda s: _zone().scrub(s, _zone().AUTUMN),
    "reeds": lambda s: _zone().cattails(s), "sunktruck": lambda s: _zone().sunktruck(s), "brush": brush, "rock": rock, "debris": debris, "scrap": scrap, "logs": logs, "tires": tires,
    "crate": crate, "lockers": lockers, "block": block, "jersey": jersey, "sandbags": sandbags,
    "sandbags_v": sandbags_v, "jersey_v": jersey_v, "block_v": block_v,
    "sandbags_c": lambda s: corner(sandbags, sandbags_v, s), "jersey_c": lambda s: corner(jersey, jersey_v, s), "block_c": lambda s: corner(block, block_v, s),
    "logs_v": logs_v, "logs_c": lambda s: corner(logs, logs_v, s),
    "rustbarrel": lambda s: rustbarrel(s), "barrel": lambda s: rustbarrel(s, True), "machine": lathe, "engine": engine_block,
    "pipes": pipes, "pillar": pillar, "desk": desk, "armchair": armchair_worn,
    "desk_n": lambda s: desk_flip("n"), "desk_s": lambda s: desk_flip("s"), "desk_e": lambda s: desk_flip("e"), "desk_w": lambda s: desk_flip("w"), "wreck": lambda s: _zone().rustcar(s), "container": lambda s: container(s),
    "culvert": culvert, "hatch": hatch, "campfire": campfire, "stones": stones_pile, "antenna": antenna, "fountain": fountain,
    "barrierpole": barrierpole, "fence": lambda s: fence(s), "fence_torn": lambda s: fence(s, True), "fence_v": fence_v,
    "rubble": rubble, "pole": pole, "watchtower": watchtower,
    "lamp": lamp_concrete, "busstop": busstop_soviet, "bus": lambda s: _zone().bus(s),
    "dumpster": dumpster_steel, "trash": trash,
    "tank": lambda s: _zone().tank(s), "flatcar": lambda s: _zone().flatcar(s),
    "pipe": lambda s: heatpipe(s, False), "pipepost": lambda s: heatpipe(s, True), "floorpipe": floorpipe, "roots": roots,
}
for _k, _f in STASHES.items():   # Batch 124
    PROPS["stash_" + _k] = (lambda f: lambda s: f(s))(_f)
    PROPS["stash_" + _k + "_open"] = (lambda f: lambda s: f(s, True))(_f)
VARIANTS = {"tree": 10, "pine": 8, "deadtree": 6, "redtree": 6, "bush": 4, "redbush": 2, "reeds": 6, "sunktruck": 2, "lamp": 2, "bus": 2, "tank": 3, "pipe": 2, "dumpster": 3, "trash": 3, "armchair": 3, "brush": 2, "rock": 14, "debris": 3,
            "scrap": 3, "wreck": 4, "stash_duffel": 2, "stash_duffel_open": 2, "container": 3, "crate": 2, "rustbarrel": 2, "machine": 2, "lockers": 2, "pipes": 2, "rubble": 3, "stones": 2}
