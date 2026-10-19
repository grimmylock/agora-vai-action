# Batch 32: concept frames per biome, in the quality bar Yan approved (concept A/B/C forest scene).
# Same tools as concept.py; each biome gets its own ground, props and a signature mood.
#   swamp      - overcast day / dawn fog
#   city       - overcast day / rain          (shows roofs + a roof cut open by line of sight)
#   industrial - overcast day / golden dusk with long shadows
#   lab        - underground: flashlight + emergency lights
# Characters are drawn into the scene BEFORE the light/grade, so lighting affects them too.
# python3 biomes.py [swamp city industrial lab] -> /tmp/claude-0/biome_<name>_<mood>.png
import sys, math, random
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont
sys.path.insert(0, "/home/claude/art/concept")
import concept as cc
from concept import W, H, hx, ramp, rr, noise, blank, px, outline, noise_pt

S = dict(cc.STYLES["A"])
S.update(
    asphalt=ramp("2a2c2e", "323436", "3a3c3e", "434547"), curb=ramp("5e5d58", "77756e", "8f8c83"),
    steel=ramp("2a2e31", "3b4145", "4f565a", "666d70"), tin=ramp("4a4f4e", "5f6664", "77807c", "8f9893"),
    wood=ramp("2e2318", "3f3122", "54412d", "6a533a"), tile=ramp("6e706c", "80827d", "92948e", "a4a69f"),
    glow=ramp("1f4a2a", "2f7a3c", "58c060", "a8f0a0"), panel=ramp("5b5a55", "6b6a64", "7b7a73", "8c8a82"),
    cattail=ramp("3d2c1c", "5a4128"),
)
DK = lambda c, k=.45: (int(c[0] * k), int(c[1] * k), int(c[2] * k), 255)

class Scene:
    def __init__(self, ground):
        self.im = ground; self.items = []; self.wires = []; self.lights = []
    def add(self, sp, at, sh=None):
        self.items.append((sp[0], sp[1], at, sh))
    def render(self, shadow="blob", sun=(1.6, .35)):
        im = self.im.copy(); shade = Image.new("RGBA", (W, H)); sd = ImageDraw.Draw(shade)
        scol = (24, 20, 26, 120) if shadow == "blob" else (34, 26, 44, 150)
        for spr, piv, (x, y), sh in self.items:
            if not sh: continue
            if shadow == "blob":
                sd.ellipse([x - sh[0] // 2 + 2, y - sh[1] // 2, x + sh[0] // 2 + 2, y + sh[1] // 2], fill=scol)
            elif shadow == "cast":
                a = np.array(spr)[..., 3] > 0; hh, ww = a.shape
                ys, xs = np.nonzero(a)
                for yy, xx in zip(ys, xs):
                    up = hh - 1 - yy
                    sx = x + (xx - piv[0]) + int(up * sun[0]); sy = y + int(up * sun[1])
                    if 0 <= sx < W and 0 <= sy < H: shade.putpixel((sx, sy), scol)
        if shadow != "none": im.alpha_composite(shade.filter(ImageFilter.BoxBlur(0.6)) if shadow == "blob" else shade)
        for (x0, y0, x1, y1, sag) in self.wires:
            for t in np.linspace(0, 1, 500):
                x = x0 + (x1 - x0) * t; y = y0 + (y1 - y0) * t + sag * math.sin(math.pi * t)
                if 0 <= x < W and 0 <= y < H: im.putpixel((int(x), int(y)), (22, 22, 22, 255))
        for spr, piv, (x, y), sh in sorted(self.items, key=lambda it: it[2][1]):
            im.alpha_composite(spr, (int(x - piv[0]), int(y - piv[1])))
        return im.convert("RGB")

# ------------------------------------------------------------------ ground helpers
def field(pal, seed, cells=(28, 9, 3), wts=(.45, .3, .25), grain=.18, dry=None, dry_amt=.5):
    """per-pixel ground: tone noise + grain, with a second ramp (straw) mixed in pixel by pixel"""
    n = sum(noise(W, H, c, seed + i) * w for i, (c, w) in enumerate(zip(cells, wts)))
    rnd = np.random.RandomState(seed + 7).rand(H, W); rnd2 = np.random.RandomState(seed + 8).rand(H, W)
    t = n + (rnd - .5) * grain
    out = np.zeros((H, W, 4), np.uint8)
    use_dry = np.zeros((H, W), bool)
    if dry:
        dm = noise(W, H, 40, seed + 5) * .7 + noise(W, H, 7, seed + 6) * .3
        use_dry = rnd2 < np.clip((dm - .45) / .2, 0, 1) * dry_amt
    for p, m in ((pal, ~use_dry), (dry, use_dry)):
        if p is None: continue
        idx = np.clip(((t - .22) / .56 * len(p)).astype(int), 0, len(p) - 1)
        for i, c in enumerate(p): out[m & (idx == i)] = c
    return out

def paint(arr, mask, pal, seed, cells=(9, 3), wts=(.6, .4), grain=.15):
    n = sum(noise(W, H, c, seed + i) * w for i, (c, w) in enumerate(zip(cells, wts)))
    rnd = np.random.RandomState(seed + 3).rand(H, W)
    idx = np.clip(((n + (rnd - .5) * grain - .22) / .56 * len(pal)).astype(int), 0, len(pal) - 1)
    for i, c in enumerate(pal): arr[mask & (idx == i)] = c
    return arr

def blobmask(shapes, jag=4, seed=1):
    ys, xs = np.mgrid[0:H, 0:W]; m = np.zeros((H, W), bool)
    jn = noise(W, H, 7, seed) * jag
    for (cx, cy, rx, ry) in shapes: m |= ((xs - cx) / rx) ** 2 + ((ys - cy) / ry) ** 2 < 1 - jn / max(rx, ry)
    return m

# ------------------------------------------------------------------ new props
def cattails(seed, n=10):
    """reed bed: blades of different heights bending away, mixed green and straw, a few cattail heads"""
    r = rr(seed); im = blank(18, 22)
    for k in range(n):
        x = r.uniform(3, 14); h = r.randint(7, 19); bend = r.uniform(-.25, .25)
        pal = S["grass"] if r.random() < .55 else S["dry"]
        for y in range(h):
            xx = x + bend * (y ** 1.6) / 6
            c = pal[min(len(pal) - 1, 1 + y * (len(pal) - 2) // max(1, h))]
            px(im, int(round(xx)), 21 - y, c)
        if h > 13 and r.random() < .35:
            hxp = int(round(x + bend * ((h - 3) ** 1.6) / 6))
            for y in range(h - 4, h - 1): px(im, hxp, 21 - y, S["cattail"][1 if y == h - 4 else 0])
    return outline(im, "sel"), (9, 21)

def lily(seed):
    r = rr(seed); im = blank(7, 4)
    for x in range(1, 6): px(im, x, 1, S["leaf"][3]); px(im, x, 2, S["leaf"][2])
    px(im, 0, 2, S["leaf"][1]); px(im, 6, 2, S["leaf"][1]); px(im, 3, 1, S["water"][0])
    if r.random() < .3: px(im, 2, 1, hx("c8c0a8"))
    return im, (3, 3)

def boardwalk(seed, n=9, w=13):
    r = rr(seed); im = blank(n * 5, w + 5)
    for i in range(n):
        if r.random() < .12: continue                                     # a missing plank
        x0 = i * 5; sag = r.randint(0, 1)
        for x in range(x0, x0 + 4):
            for y in range(sag, w + sag):
                c = S["wood"][2] if y % 6 else S["wood"][1]
                if x == x0: c = S["wood"][3]
                if r.random() < .06: c = S["moss"][r.randrange(2)]
                px(im, x, y, c)
        for y in range(w + sag, w + sag + 3): px(im, x0 + 1, y, S["wood"][0]); px(im, x0 + 2, y, S["wood"][1])     # plank edge / front
    for i in range(0, n, 3):                                              # posts
        for y in range(w, w + 5): px(im, i * 5 + 2, y, S["wood"][0])
    return outline(im, "sel"), (n * 5 // 2, w + 4)

def stilt_hut(seed):
    """fisherman's shack on stilts: plank walls, rusty tin roof seen from above, a dark doorway"""
    r = rr(seed); w, h = 46, 44; im = blank(w, h); d = ImageDraw.Draw(im)
    # roof (tin, corrugated, rust patches) - this is what you see from outside
    for y in range(0, 18):
        for x in range(1, w - 1):
            c = S["tin"][1 + (x % 4 == 0) + (x % 4 == 1)] if r.random() > .14 else S["rust"][r.randrange(1, 4)]
            if y == 0: c = S["tin"][3]
            px(im, x, y, c)
    for k in range(3):
        x, y = r.randint(4, 38), r.randint(3, 12); d.rectangle([x, y, x + r.randint(2, 5), y + r.randint(1, 3)], fill=S["rust"][2])
    # front wall (planks) with door
    for y in range(18, 34):
        for x in range(2, w - 2):
            c = S["wood"][2] if x % 4 else S["wood"][0]
            if y > 30: c = DK(c, .7)
            px(im, x, y, c)
    d.rectangle([18, 22, 26, 33], fill=S["glass"][0]); d.rectangle([32, 22, 38, 27], fill=S["glass"][0])
    for x in (4, 20, w - 6):                                              # stilts into the water
        for y in range(34, h): px(im, x, y, S["wood"][0]); px(im, x + 1, y, S["wood"][1])
    return outline(im, "sel"), (w // 2, h - 1)

def sunk_truck(seed):
    """the cab and front of a lorry sinking into the bog, water line across it"""
    r = rr(seed); w, h = 44, 24; im = blank(w, h); d = ImageDraw.Draw(im)
    d.rectangle([4, 4, 20, 16], fill=S["rust"][2]); d.rectangle([6, 5, 18, 9], fill=S["glass"][0])     # cab
    d.rectangle([20, 7, 42, 16], fill=S["steel"][1]); d.rectangle([20, 7, 42, 8], fill=S["steel"][2])  # cargo bed
    for k in range(60): px(im, r.randint(4, 42), r.randint(4, 16), S["rust"][r.randrange(4)])
    for x in range(0, w): px(im, x, 16, S["water"][2]); px(im, x, 17, S["water"][1])               # water line
    for y in range(17, h):
        for x in range(w): im.putpixel((x, y), (0, 0, 0, 0))
    return outline(im, "sel"), (22, 17)

def lamp(seed):
    w, h = 16, 60; im = blank(w, h)
    for y in range(8, h): px(im, 7, y, S["steel"][2]); px(im, 8, y, S["steel"][1])
    for x in range(7, 15): px(im, x, 7, S["steel"][2]); px(im, x, 8, S["steel"][1])
    for x in range(11, 16): px(im, x, 9, S["steel"][0])
    px(im, 13, 10, S["glass"][1])
    return outline(im, "sel"), (7, h - 1)

def bus_stop(seed):
    r = rr(seed); w, h = 40, 30; im = blank(w, h); d = ImageDraw.Draw(im)
    d.rectangle([1, 0, w - 2, 7], fill=S["panel"][3]); d.line([(1, 0), (w - 2, 0)], fill=S["panel"][3])   # roof slab
    d.rectangle([2, 8, w - 3, 22], fill=S["panel"][1])                                                  # back wall
    for x in range(4, w - 4, 7): d.rectangle([x, 10, x + 4, 15], fill=S["panel"][2])                     # mosaic
    d.rectangle([6, 20, w - 7, 23], fill=S["wood"][1])                                                  # bench
    for k in range(25): px(im, r.randint(2, w - 3), r.randint(8, 22), S["moss"][0] if r.random() < .3 else S["panel"][0])
    for x in (2, w - 3):
        for y in range(8, h): px(im, x, y, S["panel"][2])
    return outline(im, "sel"), (w // 2, h - 1)

def bus(seed):
    """burned-out city bus (side view in 3/4): roof, window band, rust"""
    r = rr(seed); w, h = 78, 26; im = blank(w, h); d = ImageDraw.Draw(im)
    d.rectangle([1, 0, w - 2, 7], fill=S["tin"][2])                                                      # roof
    d.rectangle([1, 8, w - 2, 21], fill=hx("6b6340")); d.rectangle([1, 8, w - 2, 9], fill=hx("857b52"))   # faded yellow body
    for x in range(4, w - 6, 9): d.rectangle([x, 11, x + 6, 15], fill=S["glass"][0])
    for k in range(260):                                                                                 # fire and rust
        x, y = r.randint(1, w - 2), r.randint(0, 21)
        if noise_pt(x * 2, y * 3) > .45: px(im, x, y, (S["rust"] + S["tar"])[r.randrange(7)])
    for x in (10, w - 16): d.rectangle([x, 21, x + 7, 24], fill=S["tar"][0])
    return outline(im, "sel"), (w // 2, h - 2)

def dumpster(seed):
    im = blank(18, 14); d = ImageDraw.Draw(im)
    d.rectangle([1, 2, 16, 12], fill=hx("3e4a3a")); d.rectangle([1, 1, 16, 4], fill=hx("4e5c48"))
    for k in range(20): px(im, rr(seed).randint(1, 16), rr(seed + k).randint(2, 12), S["rust"][2])
    return outline(im, "sel"), (9, 13)

def trash(seed):
    r = rr(seed); im = blank(16, 8)
    for k in range(5):
        cx, cy = r.randint(3, 12), r.randint(3, 5)
        c = r.choice((hx("1e2122"), hx("2a2e2c"), hx("4a4236")))
        for y in range(-2, 2):
            for x in range(-2, 3):
                if x * x + y * y * 2 < 6: px(im, cx + x, cy + y, c if y > -1 else (c[0] + 18, c[1] + 18, c[2] + 18, 255))
    return outline(im, "sel"), (8, 7)

def tank(seed, r_=16):
    """rusty cylindrical tank seen in 3/4: round top, curved front"""
    r = rr(seed); w, h = r_ * 2 + 2, r_ + 28; im = blank(w, h)
    cx = r_ + 1; top = r_ * .55
    for y in range(h):
        for x in range(w):
            dx = (x - cx) / r_
            if abs(dx) > 1: continue
            cap_edge = top * math.sqrt(max(0, 1 - dx * dx))
            if y < top * 2 and abs(y - top) <= cap_edge:                       # lid ellipse
                c = S["steel"][3] if (x - cx) + (y - top) < 0 else S["steel"][2]
            elif y >= top and y < h - 2 - (top - cap_edge) * .0 and y <= top + 24 + cap_edge:
                shade = 3 if dx < -.4 else 2 if dx < .3 else 1                  # cylinder side, lit left
                c = S["steel"][shade] if r.random() > .1 else S["rust"][r.randrange(1, 4)]
                if (y - int(top)) % 8 == 0: c = DK(c, .75)                      # weld seams
            else: continue
            px(im, x, y, c)
    for k in range(40):                                                       # rust streaks down from the seams
        x = r.randint(3, w - 4); y0 = r.randint(int(top) + 2, int(top) + 16)
        for y in range(y0, y0 + r.randint(3, 9)): px(im, x, y, S["rust"][2])
    return outline(im, "sel"), (cx, int(top + 24 + top))

def pipe_run(length, seed, height=14):
    """a pipeline on concrete supports"""
    r = rr(seed); im = blank(length, height + 8)
    for x in range(length):
        for y in range(4):
            c = S["steel"][3 - y] if r.random() > .08 else S["rust"][2]
            px(im, x, y + 2, c)
    for x in range(4, length, 24):
        for y in range(6, height + 7):
            for k in range(3): px(im, x + k, y, S["panel"][2 - (k == 2)])
    return outline(im, "sel"), (length // 2, height + 7)

def rail_h(length):
    im = blank(length, 14)
    for x in range(length):
        for y in range(1, 13):
            c = S["dirt"][1] if (x * 7 + y) % 5 else S["dirt"][2]
            if x % 5 in (0, 1) and 2 <= y <= 11: c = S["wood"][1 if x % 5 == 0 else 2]
            if y in (3, 10): c = S["steel"][3] if y == 3 else S["steel"][2]
            px(im, x, y, c)
    return im, (length // 2, 13)

def flatcar(seed):
    r = rr(seed); w, h = 70, 20; im = blank(w, h); d = ImageDraw.Draw(im)
    d.rectangle([1, 4, w - 2, 10], fill=S["rust"][1]); d.rectangle([1, 4, w - 2, 5], fill=S["rust"][2])
    d.rectangle([1, 11, w - 2, 14], fill=S["steel"][0])
    for x in (6, 20, w - 26, w - 12): d.rectangle([x, 14, x + 6, 17], fill=S["steel"][1])
    d.rectangle([10, 0, 32, 8], fill=hx("3e4a3a")); d.rectangle([10, 0, 32, 2], fill=hx("4e5c48"))     # a crate on it
    for k in range(60): px(im, r.randint(1, w - 2), r.randint(4, 14), S["rust"][r.randrange(4)])
    return outline(im, "sel"), (w // 2, h - 2)

def chainfence(length, seed, torn_at=None):
    r = rr(seed); im = blank(length, 18)
    for x in range(length):
        if torn_at and torn_at[0] <= x < torn_at[1]: continue
        for y in range(3, 16):
            if (x + y) % 3 == 0 or (x - y) % 3 == 0: px(im, x, y, (110, 112, 108, 170))
        if x % 2 == 0 and r.random() < .5: px(im, x, 2 - (x // 2) % 2, S["steel"][3])   # barbed wire
    for x in range(0, length, 14):
        for y in range(1, 18): px(im, x, y, S["steel"][2]); px(im, x + 1, y, S["steel"][1])
    return im, (length // 2, 17)

def crate(seed):
    r = rr(seed); im = blank(14, 14); d = ImageDraw.Draw(im)
    d.rectangle([1, 4, 12, 12], fill=S["wood"][2]); d.rectangle([1, 1, 12, 4], fill=S["wood"][3])
    d.line([(1, 4), (12, 12)], fill=S["wood"][1]); d.line([(1, 8), (12, 8)], fill=S["wood"][1])
    return outline(im, "sel"), (7, 13)

def drums(seed):
    im = blank(20, 14)
    for i, (x, y) in enumerate(((2, 3), (9, 1), (13, 4))):
        c = S["rust"] if i != 1 else ramp("2c3a3e", "3c4e52", "4e6266", "5f7478")
        d = ImageDraw.Draw(im); d.rectangle([x, y + 2, x + 5, y + 9], fill=c[1]); d.ellipse([x, y, x + 5, y + 3], fill=c[2])
        d.line([(x + 1, y + 3), (x + 1, y + 9)], fill=c[3])
    return outline(im, "sel"), (10, 13)

# ------------------------------------------------------------------ 3/4 buildings with roofs
def panel_block(w, seed, floors=3, roof_h=34, face_floor=16, entrance_at=None, hole=None):
    """Soviet panel apartment block: tar roof with parapet, vents and an antenna on top; a tall
    3/4 facade of prefab panels with dark/boarded windows and rusty balconies"""
    r = rr(seed); fh = floors * face_floor; h = roof_h + fh + 1; im = blank(w, h); d = ImageDraw.Draw(im)
    for y in range(roof_h):                                               # roof
        for x in range(w):
            c = S["tar"][1] if r.random() > .25 else S["tar"][2]
            if noise_pt(x * 2 + seed, y * 2) > .82: c = S["water"][1]        # standing rainwater
            if noise_pt(x + 77, y * 3 + seed) > .9: c = S["moss"][0]
            px(im, x, y, c)
    d.rectangle([0, 0, w - 1, 2], fill=S["panel"][3]); d.rectangle([0, 0, 2, roof_h], fill=S["panel"][3]); d.rectangle([w - 3, 0, w - 1, roof_h], fill=S["panel"][2])
    d.rectangle([0, roof_h - 3, w - 1, roof_h - 1], fill=S["panel"][3])
    for k in range(3):                                                    # vents / stair heads
        x = r.randint(10, w - 30); d.rectangle([x, 8, x + 14, 18], fill=S["panel"][2]); d.rectangle([x, 8, x + 14, 10], fill=S["panel"][3]); d.rectangle([x + 3, 15, x + 9, 18], fill=S["glass"][0])
    if hole:                                                              # collapsed roof: you look down into a flat
        hx0, hy0, hx1, hy1 = hole
        hm = np.zeros((roof_h, w), bool)
        for y in range(hy0, hy1):
            for x in range(hx0, hx1):
                e = min(x - hx0, hx1 - 1 - x, y - hy0, hy1 - 1 - y)
                if e > noise_pt(x * 3, y * 3) * 4: hm[y, x] = True
        for y in range(roof_h):
            for x in range(w):
                if not hm[y, x]: continue
                c = S["wood"][2] if (x // 3 + y) % 7 else S["wood"][1]      # parquet
                if x % 26 in (0, 1): c = S["panel"][1]                       # room walls seen from above
                if noise_pt(x * 5, y * 5 + 3) > .8: c = (S["panel"] + S["brick"])[r.randrange(8)]   # rubble
                px(im, x, y, c)
                if y > 0 and not hm[y - 1, x]: px(im, x, y, S["glass"][0])   # the roof's thickness in shadow
        for k in range(12):                                               # rebar along the broken edge
            x, y = r.randint(hx0, hx1), r.choice((hy0, hy1 - 1))
            for s_ in range(r.randint(2, 5)): px(im, x + s_ // 2, y + (s_ if y == hy0 else -s_), S["rust"][2])
    ax = r.randint(30, w - 30)                                            # TV antenna
    for y in range(4, 16): px(im, ax, y, S["steel"][1])
    for x in range(ax - 5, ax + 6): px(im, x, 7, S["steel"][2])
    for fl in range(floors):                                              # facade
        y0 = roof_h + fl * face_floor
        for y in range(y0, y0 + face_floor):
            for x in range(w):
                c = S["panel"][2] if (x // 24 + fl) % 2 else S["panel"][1]
                if y == y0 or x % 24 == 0: c = S["panel"][0]
                if r.random() < .05: c = S["panel"][0]
                px(im, x, y, c)
        for x in range(6, w - 10, 24):
            boarded = r.random() < .3
            d.rectangle([x, y0 + 4, x + 8, y0 + 11], fill=S["wood"][2] if boarded else S["glass"][0])
            if not boarded and r.random() < .5: px(im, x + 3, y0 + 6, S["glass"][1])
            if r.random() < .5:                                           # balcony
                d.rectangle([x + 11, y0 + 9, x + 21, y0 + 14], fill=S["rust"][1]); d.line([(x + 11, y0 + 9), (x + 21, y0 + 9)], fill=S["rust"][3])
        for x in range(w):                                                # streaks below each floor
            if (x * 7 + fl) % 13 == 0:
                for y in range(y0 + 1, y0 + r.randint(4, 12)): px(im, x, y, DK(im.getpixel((x, y)), .8))
    if entrance_at is not None:
        ex = entrance_at; yb = roof_h + fh
        d.rectangle([ex, yb - 13, ex + 12, yb], fill=S["glass"][0]); d.rectangle([ex - 3, yb - 16, ex + 15, yb - 14], fill=S["panel"][3])
    for x in range(w): px(im, x, h - 1, S["moss"][0] if r.random() < .3 else S["panel"][0])
    return outline(im, "sel"), (w // 2, h - 1)

def factory(w, seed, roof_h=40, face_h=28):
    """factory hall: sawtooth roof with skylights, brick plinth + corrugated upper wall, big door"""
    r = rr(seed); h = roof_h + face_h + 1; im = blank(w, h); d = ImageDraw.Draw(im)
    for y in range(roof_h):
        for x in range(w):
            tooth = y % 10
            c = S["tin"][3] if tooth < 2 else S["tin"][2] if tooth < 6 else S["tin"][0]        # lit slope / shaded slope
            if tooth in (7, 8) and x % 30 > 3: c = S["glass"][0] if r.random() > .25 else S["glass"][1]   # skylight glazing
            if noise_pt(x // 6 + seed, y // 2) > .8: c = S["rust"][1 + (tooth < 6)]                # rust in patches, not noise
            if noise_pt(x + seed, y * 2) > .93: c = (0, 0, 0, 0)                                      # holes in the roof
            px(im, x, y, c)
    for y in range(roof_h, h - 1):
        for x in range(w):
            k = y - roof_h
            if k < face_h - 10: c = (S["tin"][2], S["tin"][1], S["tin"][1], S["tin"][0])[x % 4] if r.random() > .07 else S["rust"][2]
            else:
                c = S["brick"][2] if (k % 3 != 2 and (x + (k // 3) * 3) % 6 != 5) else S["brick"][0]
            if k > face_h - 4: c = DK(c, .75)
            px(im, x, y, c)
    dx = w // 2 - 14; d.rectangle([dx, roof_h + 4, dx + 28, h - 2], fill=S["steel"][1])
    for x in range(dx, dx + 29, 3): d.line([(x, roof_h + 4), (x, h - 2)], fill=S["steel"][0])
    d.rectangle([dx + 9, roof_h + 12, dx + 19, h - 2], fill=S["glass"][0])                         # the door is half open
    return outline(im, "sel"), (w // 2, h - 1)

# ------------------------------------------------------------------ post (same moods as concept.py, player-aware)
def post(img, mood, player, aim=0.0, fog=0.0, rain=0.0, lights=()):
    a = np.asarray(img).astype(np.float32) / 255
    Hh, Ww = a.shape[:2]; yy, xx = np.mgrid[0:Hh, 0:Ww]
    pxp, pyp = player[0], player[1] - 8
    dist = np.sqrt((xx - pxp) ** 2 + (yy - pyp) ** 2)
    lum = a.mean(-1, keepdims=True)
    if mood in ("A", "fog", "rain"):
        a = lum + (a - lum) * (0.82 if mood == "A" else 0.7)
        a = a * 0.95 + np.array([0.04, 0.04, 0.035])
        if mood == "rain":
            a = a * np.array([.86, .9, .98]) * .9                         # wet, cold, darker
        v = np.sqrt(((xx - Ww / 2) / (Ww / 2)) ** 2 + ((yy - Hh / 2) / (Hh / 2)) ** 2)
        a = a * (1 - 0.35 * np.clip(v - .6, 0, 1))[..., None]
        grain = 2.5
    elif mood == "C":
        a = a * np.array([1.08, 1.0, 0.86])
        hi = np.clip(lum - .45, 0, 1); a = a + hi * np.array([0.25, 0.18, 0.05])
        a = a * 0.9 + np.array([0.09, 0.08, 0.06])
        v = np.sqrt(((xx - Ww * .3) / Ww) ** 2 + ((yy - Hh * .2) / Hh) ** 2)
        a = a * (1 - 0.28 * np.clip(v - .25, 0, 1))[..., None]
        grain = 2
    else:                                                                  # "B": darkness, flashlight, extra lights
        a = lum + (a - lum) * 0.6
        ang = np.arctan2(yy - pyp, xx - pxp)
        da = np.abs((ang - aim + np.pi) % (2 * np.pi) - np.pi)
        cone = np.clip(1 - da / 0.55, 0, 1) ** 0.6 * np.clip(1 - dist / 260, 0, 1)
        near = np.clip(1 - dist / 30, 0, 1)
        light = np.maximum(cone ** 0.7, near)[..., None]
        tint = light * np.array([0.06, 0.05, 0.02])
        for (lx, ly, rad, col) in lights:                                  # emergency lights: coloured pools
            l = np.clip(1 - np.sqrt((xx - lx) ** 2 + (yy - ly) ** 2) / rad, 0, 1)[..., None] ** 1.5
            light = np.maximum(light, l * .8); tint = tint + l * np.array(col) * .35
        a = a * (0.22 + 1.05 * light) + tint
        grain = 6
    if fog:
        n = noise(Ww, Hh, 40, 11) * .7 + noise(Ww, Hh, 13, 12) * .3
        f = (np.clip((n - .3) / .5, 0, 1) ** 1.5 * .5 * fog)[..., None]
        a = a * (1 - f) + np.array([.7, .72, .68]) * f
        far = np.clip((dist - 90) / 90, 0, 1)[..., None] * .55 * fog     # what you can't see fades into mist
        a = a * (1 - far) + np.array([.62, .64, .6]) * far
    if rain:
        r = np.zeros((Hh, Ww)); rnd = np.random.RandomState(3)
        for k in range(int(700 * rain)):
            x, y, ln = rnd.randint(Ww), rnd.randint(Hh), rnd.randint(4, 8)
            for s in range(ln):
                xx_, yy_ = x - s // 4, y + s
                if 0 <= xx_ < Ww and 0 <= yy_ < Hh: r[yy_, xx_] = .25 + .3 * s / ln
        a = a * (1 - r[..., None]) + np.array([.7, .76, .86]) * r[..., None]
    a = a + np.random.RandomState(1).normal(0, grain / 255, (Hh, Ww, 1))
    return Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))

def unit(armor, flip=False): return cc.unit(armor, flip), (8, 15)


# ================================================================== scenes
SW = ramp("1d2624", "243030", "2e3b39", "6c7a74")

def swamp():
    g = field(S["grass"], 41, dry=S["dry"], dry_amt=.45)
    mud = blobmask([(190, 120, 150, 70)], jag=10, seed=3)
    g = paint(g, mud & (noise(W, H, 20, 9) > .45), S["mud"], 5)
    water = blobmask([(110, 150, 95, 45), (260, 70, 70, 28), (330, 170, 60, 35), (40, 40, 50, 25)], jag=9, seed=7)
    wn = np.asarray(Image.fromarray((noise(W // 3 + 2, H, 9, 21) * 255).astype(np.uint8)).resize((W, H), Image.BILINEAR)) / 255.0   # stretched sideways: water, not moss
    for y in range(H):
        for x in range(W):
            if not water[y, x]: continue
            shore = y > 0 and not water[y - 1, x]
            if shore: g[y, x] = S["mud"][2]; continue
            if y > 2 and not water[y - 3:y - 1, x].all(): g[y, x] = DK(SW[1], .75); continue   # bank shadow
            c = SW[2] if wn[y, x] > .6 else SW[1] if wn[y, x] > .38 else SW[0]       # dark still water
            if noise_pt(x // 2 + 50, y // 2) > .9: c = S["moss"][0]                         # duckweed
            if (y * 5 + (x // 7) * 3) % 11 == 0 and noise_pt(x // 4, y) > .55: c = SW[3]   # ripple lines / sky glints
            g[y, x] = c
            continue
            g[y, x] = c
    sc = Scene(Image.fromarray(g, "RGBA"))
    rnd = random.Random(5)
    # shoreline reeds and cattails, dense
    ys, xs = np.nonzero(water)
    edge = [(x, y) for y, x in zip(ys, xs) if not water[min(H - 1, y + 2), x] or not water[max(0, y - 3), x] or not water[y, min(W - 1, x + 3)] or not water[y, max(0, x - 3)]]
    for i in range(46):
        x, y = edge[rnd.randrange(len(edge))]
        sc.add(cattails(300 + i, rnd.randint(5, 9)), (x + rnd.randint(-3, 3), y + rnd.randint(-2, 3)))
    for i in range(26):
        x, y = xs[k := rnd.randrange(len(xs))], ys[k]
        if not edge or rnd.random() < .5: sc.add(lily(400 + i), (x, y))
    for i in range(110):
        x, y = rnd.randrange(W), rnd.randrange(H)
        if water[y, x]: continue
        sc.add(cc.grass_clump(S, 500 + i, dry=rnd.random() < .45, tall=rnd.random() < .5), (x, y))
    # dead trees standing in the water, a few living birches on the dry land
    for i, (x, y) in enumerate(((90, 140), (140, 165), (270, 64), (340, 180), (30, 44))):
        sc.add(cc.deadtree(S, 60 + i, 36 + i * 3), (x, y), (6, 3))
    for i, (x, y) in enumerate(((200, 40), (230, 30), (372, 110), (12, 200), (60, 214))):
        sc.add(cc.birch(S, 70 + i, 42), (x, y), (12, 4))
    for i, (x, y) in enumerate(((176, 196), (300, 120), (20, 110))): sc.add(cc.bush(S, 80 + i), (x, y), (14, 4))
    sc.add(boardwalk(1, 12), (130, 112), (40, 4))
    sc.add(stilt_hut(2), (300, 118), (40, 6))
    sc.add(sunk_truck(3), (60, 165))
    sc.add(drums(4), (226, 112), (12, 3))
    sc.add(unit("Rookie Field Suit"), (196, 134), (11, 3))
    sc.add(unit("Raider Jacket", True), (282, 150), (11, 3))
    return sc, (196, 134)

def city():
    g = np.zeros((H, W, 4), np.uint8)
    g[:] = field(S["grass"], 51, dry=S["dry"], dry_amt=.7)
    road = np.zeros((H, W), bool); road[120:170, :] = True
    walk_n = np.zeros((H, W), bool); walk_n[108:118, :] = True
    walk_s = np.zeros((H, W), bool); walk_s[172:184, :] = True
    g = paint(g, road, S["asphalt"], 9, cells=(14, 3), grain=.25)
    for m in (walk_n, walk_s):
        idx = np.nonzero(m)
        for y, x in zip(*idx):
            c = S["curb"][1] if (x % 10 and (y - 108) % 10) else S["curb"][0]
            if noise_pt(x * 2, y * 2) > .8: c = S["curb"][0]
            if noise_pt(x + 9, y * 5) > .93: c = S["grass"][3]                   # weeds in the joints
            g[y, x] = c
    g[118:120, :] = S["curb"][2]; g[170:172, :] = S["curb"][0]                  # curbs
    for x in range(0, W, 26):                                                 # faded lane paint
        for k in range(14):
            if noise_pt(x + k, 7) > .35: g[144:146, x + k] = hx("8f8a72")
    for (cx, cy, rw) in ((70, 160, 10), (240, 132, 7), (330, 158, 12)):       # potholes with water
        for y in range(-4, 5):
            for x in range(-rw, rw + 1):
                if (x / rw) ** 2 + (y / 3.5) ** 2 < 1 - noise_pt(cx + x, cy + y) * .4: g[cy + y, cx + x] = S["water"][1] if y > -2 else S["water"][0]
    for k in range(30):                                                       # cracks with grass
        x, y = random.Random(k).randrange(W), random.Random(k + 99).randrange(120, 170)
        for s in range(random.Random(k).randint(4, 12)):
            if 0 <= x < W and 120 <= y < 170: g[y, x] = S["asphalt"][0] if s % 4 else S["grass"][2]
            x += random.Random(k * 7 + s).choice((-1, 0, 1)); y += random.Random(k * 3 + s).choice((0, 1))
    sc = Scene(Image.fromarray(g, "RGBA"))
    rnd = random.Random(9)
    sc.add(panel_block(250, 3, floors=2, roof_h=40, face_floor=18, entrance_at=150, hole=(40, 6, 110, 36)), (125, 107))
    sc.add(panel_block(120, 4, floors=2, roof_h=40, face_floor=18), (330, 105))
    for i, x in enumerate((20, 120, 220, 320)): sc.add(lamp(i), (x, 118), (5, 2))
    sc.wires += [(0, 62, 384, 60, 5)]
    sc.add(bus(5), (90, 166), (70, 6))
    sc.add(cc.car(S, 11), (300, 140), (30, 6))
    sc.add(bus_stop(6), (250, 116), (36, 4))
    sc.add(dumpster(7), (40, 112), (16, 4)); sc.add(trash(8), (60, 114)); sc.add(trash(9), (210, 182))
    for i, (x, y) in enumerate(((170, 200), (30, 205), (360, 200))): sc.add(cc.birch(S, 90 + i, 44), (x, y), (12, 4))
    sc.add(cc.bush(S, 95), (120, 206), (14, 4)); sc.add(cc.bush(S, 96), (300, 210), (14, 4))
    for i in range(70):
        x, y = rnd.randrange(W), rnd.randrange(186, H)
        sc.add(cc.grass_clump(S, 600 + i, dry=rnd.random() < .6, tall=rnd.random() < .4), (x, y))
    for i in range(12):
        x, y = rnd.randrange(W), rnd.choice((rnd.randrange(108, 118), rnd.randrange(172, 184)))
        sc.add(cc.grass_clump(S, 700 + i, dry=True), (x, y))
    sc.add(unit("Rookie Field Suit"), (190, 150), (11, 3))
    sc.add(unit("Raider Jacket", True), (330, 178), (11, 3))
    sc.add(unit("Raider Stitched Coat", True), (262, 112), (11, 3))
    return sc, (190, 150)

def industrial():
    g = paint(field(S["grass"], 61, dry=S["dry"], dry_amt=.8), np.ones((H, W), bool), ramp("585751", "5f5e58", "66655e"), 13, cells=(30, 7), grain=.3)
    for y in range(H):                                                        # slab joints, weeds, oil
        for x in range(W):
            if (x % 32 == 0 or y % 32 == 0) and noise_pt(x, y) > .25: g[y, x] = S["conc"][0]
            if (x % 32 == 0 or y % 32 == 0) and noise_pt(x * 3, y) > .8: g[y, x] = S["grass"][3]
    oil = blobmask([(250, 160, 18, 6), (60, 140, 14, 5), (330, 140, 10, 4)], jag=3, seed=8)
    g[oil] = (np.array(g[oil], np.float32) * .55).astype(np.uint8); g[oil, 3] = 255
    pud = blobmask([(150, 100, 20, 5), (320, 196, 16, 4)], jag=3, seed=9); g[pud] = SW[2]
    grass = blobmask([(20, 190, 70, 40), (380, 30, 60, 50)], jag=10, seed=4)
    g[grass] = field(S["grass"], 62, dry=S["dry"], dry_amt=.6)[grass]
    sc = Scene(Image.fromarray(g, "RGBA"))
    rnd = random.Random(13)
    sc.add(factory(220, 5), (120, 76))
    sc.add(tank(6, 18), (300, 96), (36, 8)); sc.add(tank(7, 14), (346, 104), (28, 6))
    sc.add(pipe_run(384, 8), (192, 122))
    sc.add(rail_h(384), (192, 176)); sc.add(flatcar(9), (100, 178), (66, 6))
    sc.add(chainfence(150, 10, torn_at=(60, 76)), (300, 206))
    sc.add(drums(11), (250, 140), (14, 3)); sc.add(crate(12), (226, 142), (10, 3)); sc.add(crate(13), (232, 150), (10, 3))
    for i, (x, y) in enumerate(((10, 214), (40, 200), (370, 60))): sc.add(cc.birch(S, 110 + i, 42), (x, y), (12, 4))
    sc.add(cc.deadtree(S, 120), (200, 210), (6, 3))
    for i in range(45):
        x, y = rnd.randrange(W), rnd.randrange(H)
        if 120 < y < 184 and 0 < x: continue
        sc.add(cc.grass_clump(S, 800 + i, dry=rnd.random() < .7), (x, y))
    sc.add(unit("Rookie Field Suit"), (170, 150), (11, 3))
    sc.add(unit("Raider Jacket", True), (280, 150), (11, 3))
    return sc, (170, 150)

def lab():
    g = np.zeros((H, W, 4), np.uint8)
    rnd = random.Random(17)
    for y in range(H):                                                        # tiled floor, dirty
        for x in range(W):
            c = S["tile"][2] if (x // 12 + y // 12) % 2 else S["tile"][1]
            if x % 12 == 0 or y % 12 == 0: c = S["tile"][0]
            c = c if (x // 12 + y // 12) % 2 else S["tile"][1]
            if noise_pt(x // 2, y // 2 + 40) > .8: c = DK(c, .8)
            g[y, x] = c
    sc = Scene(Image.fromarray(g, "RGBA"))
    im = sc.im; d = ImageDraw.Draw(im)
    for k in range(16):                                                       # broken glass, papers, blood
        x, y = rnd.randrange(W), rnd.randrange(60, H)
        d.point([(x + i, y + rnd.randint(-1, 1)) for i in range(3)], fill=(170, 200, 200, 255))
    for k in range(10): x, y = rnd.randrange(W), rnd.randrange(60, H); d.rectangle([x, y, x + 3, y + 2], fill=(200, 196, 180, 255))
    for t in np.linspace(0, 1, 26):                                           # dragged blood: smears and drops
        x = 230 + 90 * t + rnd.uniform(-2, 2); y = 172 + 4 * math.sin(t * 6) + rnd.uniform(-1, 1)
        d.ellipse([x, y, x + rnd.randint(2, 5), y + rnd.randint(1, 2)], fill=(64, 20, 18, 255))
    for y in range(60, H, 1):                                                 # a cable run across the floor
        x = int(120 + 10 * math.sin(y / 14) + 4 * math.sin(y / 5)); d.point([(x, y)], fill=(34, 34, 32, 255))
    for k in range(4):                                                        # chairs knocked over, papers
        x, y = rnd.randrange(30, 350), rnd.randrange(80, 200)
        d.rectangle([x, y, x + 6, y + 3], fill=S["steel"][1]); d.line([(x, y + 4), (x + 6, y + 4)], fill=S["steel"][0])
    # back wall (3/4 face) with tiles, stains, a blast door; rooms walls
    def wall(x0, x1, ytop, fh=22):
        for x in range(x0, x1):
            for y in range(ytop, ytop + 5): im.putpixel((x, y), S["conc"][3])
            for y in range(ytop + 5, ytop + 5 + fh):
                k = y - ytop - 5
                c = S["tile"][3] if (x % 6 and k % 6) else S["tile"][1]
                if k > fh - 5: c = DK(c, .7)
                if noise_pt(x * 2, y) > .88: c = hx("5a5a3a")
                im.putpixel((x, y), c)
    wall(0, W, 0, 40)
    d.rectangle([150, 14, 190, 45], fill=S["steel"][1]); d.rectangle([150, 14, 190, 16], fill=S["steel"][3])
    for y in range(18, 45, 4): d.line([(152, y), (188, y)], fill=S["steel"][0])
    d.rectangle([164, 22, 176, 26], fill=hx("6a1a12"))                       # dead warning lamp
    wall(260, W, 110, 22)
    d.rectangle([300, 115, 316, 137], fill=S["glass"][0])                     # doorway into the next lab
    sc.add(labbench(1), (70, 90), (40, 5)); sc.add(labbench(2), (70, 140), (40, 5))
    sc.add(specimen_tank(3), (230, 104), (20, 5))
    sc.add(lockers(4), (20, 52), (20, 4))
    sc.add(unit("Rookie Field Suit"), (160, 160), (11, 3))
    sc.add(unit("Obelisk Patrol", True), (330, 200), (11, 3))
    sc.lights = [(230, 96, 70, (0.2, 0.9, 0.3)), (372, 112, 90, (1.0, 0.15, 0.1))]   # tank glow, emergency light
    return sc, (160, 160)

def labbench(seed):
    r = rr(seed); w, h = 44, 18; im = blank(w, h); d = ImageDraw.Draw(im)
    d.rectangle([1, 2, w - 2, 8], fill=S["tile"][3]); d.rectangle([1, 9, w - 2, 15], fill=S["steel"][2])
    for x in range(4, w - 4, 10): d.rectangle([x, 10, x + 7, 14], fill=S["steel"][1])
    for k in range(5):                                                        # flasks, a microscope, papers
        x = r.randint(3, w - 6)
        d.rectangle([x, 0, x + 2, 4], fill=r.choice((hx("88b8a8"), hx("c8c0a8"), S["steel"][0])))
    return outline(im, "sel"), (w // 2, h - 1)

def specimen_tank(seed):
    r = rr(seed); w, h = 24, 44; im = blank(w, h); d = ImageDraw.Draw(im)
    d.ellipse([1, 0, w - 2, 8], fill=S["steel"][3])
    d.rectangle([2, 4, w - 3, h - 8], fill=S["glow"][1])
    for y in range(4, h - 8):
        for x in range(2, w - 2):
            if x < 6: px(im, x, y, S["glow"][2])
            if r.random() < .04: px(im, x, y, S["glow"][3])                     # bubbles
    for k in range(9):                                                         # crack
        px(im, 14 + k // 2, 12 + k * 2, (200, 240, 220, 255))
    d.rectangle([0, h - 9, w - 1, h - 1], fill=S["steel"][1]); d.line([(0, h - 9), (w - 1, h - 9)], fill=S["steel"][3])
    return outline(im, "sel"), (w // 2, h - 1)

def lockers(seed):
    r = rr(seed); w, h = 30, 24; im = blank(w, h); d = ImageDraw.Draw(im)
    for i in range(3):
        x = 1 + i * 9; d.rectangle([x, 0, x + 8, h - 2], fill=hx("4a5a5e")); d.line([(x, 0), (x, h - 2)], fill=hx("62767a"))
        for y in range(3, 7, 2): d.line([(x + 2, y), (x + 6, y)], fill=hx("2e3a3c"))
        if r.random() < .4: d.rectangle([x + 1, 8, x + 7, h - 3], fill=S["glass"][0])        # door hanging open
    return outline(im, "sel"), (w // 2, h - 1)

SCENES = {
    "swamp": (swamp, [("day", "A", 0, 0, "blob"), ("dawnfog", "fog", 1, 0, "blob")]),
    "city": (city, [("day", "A", 0, 0, "blob"), ("rain", "rain", 0, 1, "blob")]),
    "industrial": (industrial, [("day", "A", 0, 0, "blob"), ("dusk", "C", 0, 0, "cast")]),
    "lab": (lab, [("underground", "B", 0, 0, "none")]),
}

if __name__ == "__main__":
    f = ImageFont.truetype("/home/claude/art/fonts/Silkscreen-Regular.ttf", 24)
    for name in sys.argv[1:] or SCENES:
        build, moods = SCENES[name]
        sc, player = build()
        for mood_name, mood, fog, rain, shadow in moods:
            raw = sc.render(shadow=shadow)
            out = post(raw, mood, player, aim=0.0, fog=fog, rain=rain, lights=sc.lights).resize((W * 5, H * 5), Image.NEAREST)
            d = ImageDraw.Draw(out); label = f"{name.upper()}  {mood_name.upper()}"
            d.rectangle([0, 0, 24 + int(f.getlength(label)), 40], fill=(15, 17, 19)); d.text((12, 7), label, font=f, fill=(230, 220, 190))
            out.save(f"/tmp/claude-0/biome_{name}_{mood_name}.png"); print(name, mood_name)
