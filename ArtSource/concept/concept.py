# Three art-direction concepts for agora vai, same scene drawn three ways (Batch 31).
#   A "Zone realism"   (Zero Sievert): naturalistic muted olive/straw, no black outlines on the world,
#                       soft blob shadows, overcast, darkness past the vision radius
#   B "Grim wood"      (Darkwood): near-monochrome sick green-grey, the world is dark except a flashlight
#                       cone, dense rot, heavy grain
#   C "Golden decay"   (Tarkovsky / Roadside Picnic): warm low sun, long cast shadows, lush overgrowth over
#                       ruins, limited painterly palette, no outlines, haze
# python3 concept.py -> /tmp/claude-0/concept_{A,B,C}.png + concept_sheet.png
import sys, math, random
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
sys.path.insert(0, "/home/claude/push/ArtSource")

W, H = 384, 216            # world px in view (24 x 13.5 tiles, the game's zoom)
SC = 5                     # -> 1920 x 1080

def hx(h, a=255): h = h.lstrip("#"); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)
def ramp(*h): return [hx(c) for c in h]

STYLES = {
 "A": dict(
    grass=ramp("2f3524", "3a4130", "454d37", "535a40", "656a4a"), dry=ramp("5c5638", "6f6745", "857b52", "9a8f61"),
    dirt=ramp("332b22", "40372b", "4e4434", "5d5240"), mud=ramp("2a241d", "332c23", "3b3328"), water=ramp("1f2826", "2b3532", "4a5752"),
    bark=ramp("c4c0b0", "9d998b", "6f6c62"), mark=hx("2a2622"), leaf=ramp("2e3820", "3d4a29", "4f5c33", "667040", "7e8550"),
    spruce=ramp("17221c", "1f2d25", "283a2f", "34493a"), dead=ramp("2c2620", "3e352c", "554a3e"),
    conc=ramp("4c4b46", "5d5c56", "6e6c65", "807d74", "939085"), brick=ramp("4a2e25", "5c3a2e", "6f4838", "835a46"),
    rust=ramp("3e2519", "5a3524", "77482e", "93603b"), tar=ramp("26262a", "302f33", "3b3a3d"), glass=ramp("141a1c", "26302f"),
    moss=ramp("3a4428", "4c5833"), outline="sel", sun=(1, 1), shadow="blob", grade="zone"),
 "B": dict(
    grass=ramp("1c211b", "232a21", "2b3328", "343c2f", "3f4636"), dry=ramp("33332a", "3d3c31", "48463a", "55523f"),
    dirt=ramp("211e1a", "2a2621", "332e27", "3c362d"), mud=ramp("1a1815", "211e1a", "28241f"), water=ramp("121615", "1a201e", "2c3431"),
    bark=ramp("7c7d74", "5f6059", "45463f"), mark=hx("161614"), leaf=ramp("161c15", "1e261c", "283223", "33402c", "414d36"),
    spruce=ramp("0f1512", "151d18", "1c261f", "253128"), dead=ramp("1c1a17", "26231f", "322e28"),
    conc=ramp("2f302d", "3a3b37", "464641", "53524c", "605f57"), brick=ramp("2e2420", "3a2c26", "47362e", "543f35"),
    rust=ramp("2a1d16", "3b281d", "4d3424", "5e402c"), tar=ramp("19191a", "202022", "28282a"), glass=ramp("0c0f0f", "161b1a"),
    moss=ramp("27301f", "333e27"), outline="sel", sun=(1, 1), shadow="none", grade="cone"),
 "C": dict(
    grass=ramp("34391f", "434a25", "56602c", "6c7334", "868840"), dry=ramp("857242", "9f8a50", "b89f60", "cdb373"),
    dirt=ramp("4a3826", "5e4830", "75593b", "8b6c49"), mud=ramp("3a2c1f", "463526", "52402e"), water=ramp("2b3a3a", "3f5250", "8a9a86"),
    bark=ramp("e6dcc2", "b8ad93", "857c68"), mark=hx("3a3026"), leaf=ramp("4a2a16", "6a3c1e", "8e5426", "b06e30", "d08e40"),
    spruce=ramp("1e2a20", "2a3a2a", "3a4c34", "4e6040"), dead=ramp("3e3428", "54473a", "6e6050"),
    conc=ramp("6a645a", "7f786b", "958d7e", "aba292", "c4baa6"), brick=ramp("6b3a2a", "844a34", "9e5c40", "b8714f"),
    rust=ramp("5a3020", "7c4428", "a15c32", "c27a44"), tar=ramp("3a3430", "48413b", "574f47"), glass=ramp("2a2e2c", "4a524c"),
    moss=ramp("5a6030", "77793c"), outline="none", sun=(1.6, 0.32), shadow="cast", grade="gold"),
}

R = random.Random(3)
def rr(seed): return random.Random(seed)

def noise(w, h, cell, seed):
    g = np.random.RandomState(seed).rand(h // cell + 2, w // cell + 2)
    ys, xs = np.mgrid[0:h, 0:w]; fx, fy = xs / cell, ys / cell
    ix, iy = fx.astype(int), fy.astype(int); tx, ty = fx - ix, fy - iy
    tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty)
    a, b, c, d = g[iy, ix], g[iy, ix + 1], g[iy + 1, ix], g[iy + 1, ix + 1]
    return (a + (b - a) * tx) * (1 - ty) + (c + (d - c) * tx) * ty

def blank(w, h): return Image.new("RGBA", (w, h), (0, 0, 0, 0))
def px(im, x, y, c):
    if 0 <= x < im.width and 0 <= y < im.height: im.putpixel((int(x), int(y)), c)

def outline(im, mode, k=0.45):
    if mode == "none": return im
    a = np.array(im).astype(np.int32); al = a[..., 3] > 0; Hh, Ww = al.shape; o = a.copy()
    for y in range(Hh):
        for x in range(Ww):
            if al[y, x]: continue
            best = None
            for dx, dy in ((0, -1), (-1, 0), (1, 0), (0, 1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < Ww and 0 <= ny < Hh and al[ny, nx]:
                    c = a[ny, nx]; l = c[:3].sum()
                    if best is None or l < best[0]: best = (l, c)
            if best is not None:
                c = best[1]; o[y, x] = (int(c[0] * k), int(c[1] * k), int(c[2] * k), 255)
    return Image.fromarray(o.astype(np.uint8), "RGBA")

# ------------------------------------------------------------------ ground
def ground(S, seed=1):
    n1, n2, n3 = noise(W, H, 28, seed), noise(W, H, 9, seed + 1), noise(W, H, 3, seed + 2)
    tone = n1 * 0.45 + n2 * 0.3 + n3 * 0.25
    a = np.zeros((H, W, 4), np.uint8)
    g, d = S["grass"], S["dry"]
    dryness = noise(W, H, 40, seed + 5) * 0.7 + noise(W, H, 7, seed + 6) * 0.3
    rnd = np.random.RandomState(seed + 7).rand(H, W)
    for y in range(H):
        for x in range(W):
            t = tone[y, x] + (rnd[y, x] - .5) * 0.18                    # fine grain: blades, not blotches
            p_dry = min(1, max(0, (dryness[y, x] - 0.45) / 0.2))        # straw blends in, pixel by pixel
            pal = d if rnd[(y * 7) % H, (x * 13) % W] < p_dry * 0.85 else g
            i = min(len(pal) - 1, max(0, int((t - 0.22) / 0.56 * len(pal))))
            a[y, x] = pal[i]
    im = Image.fromarray(a, "RGBA")
    # the road: a curving dirt track with ruts and puddles
    road = np.zeros((H, W), bool); ruts = []
    for x in range(-10, W + 10):
        cy = 150 - x * 0.28 + 10 * math.sin(x / 45)
        hw = 15 + 3 * math.sin(x / 23)
        for y in range(int(cy - hw), int(cy + hw)):
            jag = noise_pt(x, y) * 4
            if 0 <= x < W and 0 <= y < H and abs(y - cy) < hw - jag: road[y, x] = True
        ruts.append((x, cy - 6)); ruts.append((x, cy + 6))
    dn = noise(W, H, 6, seed + 9)
    for y in range(H):
        for x in range(W):
            if road[y, x]:
                i = min(3, int(dn[y, x] * 4)); im.putpixel((x, y), S["dirt"][i])
    for x, y in ruts:
        for k in (-1, 0):
            if 0 <= x < W and 0 <= int(y + k) < H and road[int(y + k), x]: im.putpixel((x, int(y + k)), S["mud"][0 if k == 0 else 1])
    for (cx, cy, rw) in ((70, 132, 9), (210, 95, 7), (300, 75, 6)):
        for y in range(-4, 5):
            for x in range(-rw, rw + 1):
                if (x / rw) ** 2 + (y / 3.5) ** 2 < 1 - noise_pt(cx + x, cy + y) * 0.4:
                    im.putpixel((cx + x, cy + y), S["water"][2] if y == -2 and abs(x) < rw // 2 else S["water"][1] if y > -3 else S["mud"][2])
    # worn path off the road toward the ruin
    for t in range(0, 60):
        x = 230 + t * 0.6; y = 92 - t * 0.9
        for k in range(-3, 4):
            if R.random() < 0.7: px(im, x + k, y, S["dirt"][R.randrange(1, 4)])
    return im, road

_np = np.random.RandomState(4).rand(512, 512)
def noise_pt(x, y): return _np[int(y) % 512, int(x) % 512]

# ------------------------------------------------------------------ sprites (pivot = ground contact)
def grass_clump(S, seed, dry=False, tall=False):
    r = rr(seed); pal = S["dry"] if dry else S["grass"]; im = blank(13, 14 if tall else 10)
    for k in range(r.randint(7, 12)):
        x = r.randint(2, 10); h = r.randint(4, im.height - 1); lean = r.choice((-1, 0, 0, 1))
        for y in range(h):
            xx = x + (lean if y > h * .55 else 0)
            px(im, xx, im.height - 1 - y, pal[min(len(pal) - 1, 1 + y * (len(pal) - 1) // max(1, h))])
    return im, (6, im.height - 1)

def birch(S, seed, h=46):
    r = rr(seed); w = 34; im = blank(w, h); tx = w // 2 - 1
    lean = r.uniform(-0.12, 0.12)
    for y in range(h - 30, h):                                          # white trunk with black lenticels
        x0 = int(tx + (h - y) * lean)
        for x in range(x0, x0 + 3):
            c = S["bark"][0] if x == x0 else S["bark"][1] if x == x0 + 1 else S["bark"][2]
            if r.random() < 0.18 and y % 3 == 0: c = S["mark"]
            px(im, x, y, c)
    # sparse canopy: many small leaf clusters with gaps and a few twigs showing
    top = int(tx + 30 * lean)
    for k in range(r.randint(26, 34)):
        cx = top + r.gauss(0, 6.5); cy = h - 34 + r.gauss(0, 6)
        rad = r.uniform(1.8, 3.2)
        cx = min(max(cx, rad + 1), w - rad - 2); cy = max(cy, rad + 1)     # keep every cluster on the canvas (no flat-cut tops)
        for y in range(int(cy - rad), int(cy + rad) + 1):
            for x in range(int(cx - rad), int(cx + rad) + 1):
                d = ((x - cx) / rad) ** 2 + ((y - cy) / (rad * .8)) ** 2
                if d <= 1 and r.random() > 0.12:
                    lit = (x - cx) + (y - cy) < -0.5
                    i = 3 if lit and d < .5 else 2 if lit else 1 if d < .8 else 0
                    if r.random() < 0.08: i = 4
                    px(im, x, y, S["leaf"][i])
    for k in range(4):                                                  # twigs
        x, y = top + r.randint(-6, 6), h - 30
        for s in range(r.randint(3, 7)): px(im, x, y, S["bark"][2]); x += r.choice((-1, 0, 1)); y -= 1
    return outline(im, S["outline"]), (tx + 1, h - 1)

def spruce(S, seed, h=52):
    r = rr(seed); w = 28; im = blank(w, h); cx = w / 2
    for y in range(h - 6, h): px(im, int(cx) - 1, y, S["dead"][1]); px(im, int(cx), y, S["dead"][0])
    # drooping branches: short strokes falling away from the trunk, longer toward the bottom
    for y0 in range(3, h - 7):
        reach = 1.5 + (y0 - 3) * 0.26 + r.uniform(-1.2, 1.2)
        for side in (-1, 1):
            if r.random() < 0.15: continue
            ln = int(reach + r.uniform(-1, 1.5))
            for s_ in range(max(1, ln)):
                x = cx + side * s_; y = y0 + s_ * 0.45
                lit = side < 0
                i = 3 if lit and s_ < ln - 1 and r.random() < .5 else 2 if lit else 1 if r.random() < .7 else 0
                px(im, x, y, S["spruce"][i]); px(im, x, y + 1, S["spruce"][max(0, i - 1)])
    return outline(im, S["outline"]), (int(cx), h - 1)

def deadtree(S, seed, h=40):
    r = rr(seed); w = 30; im = blank(w, h)
    def branch(x, y, ang, ln, th):
        for s in range(int(ln)):
            for k in range(th): px(im, x + k, y, S["dead"][2 if k == 0 else 1])
            x += math.cos(ang); y -= math.sin(ang)
            if r.random() < 0.06 and ln > 6: branch(x, y, ang + r.choice((-.7, .7)), ln * .5, max(1, th - 1))
        if ln > 5: branch(x, y, ang + r.uniform(-.6, .6), ln * .55, max(1, th - 1))
    branch(w / 2, h - 1, math.pi / 2 + r.uniform(-.15, .15), h * .55, 3)
    return outline(im, S["outline"]), (w // 2, h - 1)

def bush(S, seed, w=18, h=12, pal="leaf"):
    r = rr(seed); im = blank(w, h)
    for k in range(r.randint(10, 16)):                                 # irregular scrub, not a ball
        rad = r.uniform(1.4, 2.8); cx, cy = r.uniform(rad + 1, w - rad - 2), r.uniform(max(h * .3, rad + 1), h - 2)
        for y in range(int(cy - rad), int(cy + rad) + 1):
            for x in range(int(cx - rad), int(cx + rad) + 1):
                if ((x - cx) / rad) ** 2 + ((y - cy) / rad) ** 2 <= 1 and r.random() > 0.15:
                    px(im, x, y, S[pal][2 if (x - cx) + (y - cy) < 0 else 1 if r.random() > .3 else 0])
    for k in range(5): px(im, r.randrange(w), r.randrange(h // 2, h), S["dead"][1])
    return outline(im, S["outline"]), (w // 2, h - 1)

def ruin(S, seed):
    """corner of a Soviet two-room brick block: 3/4 walls (cap + face), a collapsed section,
    broken windows, rebar, rubble spilling out, moss along the foot of the walls"""
    r = rr(seed); w, h = 132, 86; im = blank(w, h); d = ImageDraw.Draw(im)
    FH = 20                                              # wall face height
    def face(x0, x1, ytop, mat, windows=()):
        for x in range(x0, x1):
            for y in range(ytop, ytop + FH):
                k = y - ytop
                if mat == "brick":
                    row = k // 3; off = 3 if row % 2 else 0
                    c = S["brick"][0] if k % 3 == 2 or (x + off) % 6 == 5 else S["brick"][2 if r.random() > .25 else 1]
                else:
                    c = S["conc"][2] if (x - x0) % 22 else S["conc"][0]
                    if r.random() < .12: c = S["conc"][1]
                if k > FH - 5 and r.random() < (k - FH + 5) / 6: c = tuple(int(v * .7) for v in c[:3]) + (255,)   # damp base
                if k > FH - 3 and r.random() < .3: c = S["moss"][r.randrange(2)]
                px(im, x, y, c)
            if r.random() < .05:                                        # rain streak
                for y in range(ytop + 2, ytop + r.randint(6, FH)): px(im, x, y, tuple(int(v * .78) for v in im.getpixel((x, y))[:3]) + (255,))
        for wx in windows:                                              # broken dark windows with frames
            d.rectangle([wx, ytop + 5, wx + 7, ytop + 13], fill=S["glass"][0])
            d.line([(wx, ytop + 5), (wx + 7, ytop + 5)], fill=S["conc"][3])
            d.line([(wx, ytop + 14), (wx + 7, ytop + 14)], fill=S["conc"][3])
            for k in range(3): px(im, wx + r.randint(1, 6), ytop + r.randint(6, 12), S["glass"][1])
    def cap(x0, y0, x1, y1):
        d.rectangle([x0, y0, x1, y1], fill=S["conc"][3])
        d.line([(x0, y0), (x1, y0)], fill=S["conc"][4])
    # floor inside (cracked, rubble, a weed)
    for y in range(10, 58):
        for x in range(8, 124):
            c = S["conc"][2] if noise_pt(x * 3, y * 3) > .3 else S["conc"][1]
            if r.random() < .08: c = S["conc"][0]
            if noise_pt(x + 40, y) > .93: c = S["moss"][r.randrange(2)]
            px(im, x, y, c)
    for k in range(40): d.rectangle([r.randint(10, 118), r.randint(12, 54)] * 2, fill=S["brick"][r.randrange(4)])
    # back (north) wall: face visible inside, cap on top
    cap(4, 0, 128, 5); face(8, 124, 6, "brick", windows=(20, 46, 92))
    # west wall (cap only, vertical), east wall partly collapsed
    cap(4, 0, 9, 62)
    cap(122, 0, 128, 30)
    for k in range(60):                                                 # collapsed east corner: rubble heap
        x, y = 104 + r.gauss(10, 8), 48 + r.gauss(10, 7)
        d.rectangle([x, y, x + r.randint(1, 3), y + r.randint(1, 2)], fill=(S["brick"] + S["conc"])[r.randrange(9)])
    for k in range(6):                                                  # rebar sticking out
        x, y = 124 + r.randint(-2, 2), 30 + r.randint(0, 8)
        for s in range(r.randint(4, 8)): px(im, x, y, S["rust"][2]); x += r.choice((0, 1)); y -= 1
    # south facade: face with a doorway and windows, cap above
    cap(4, 58, 104, 63); face(4, 104, 64, "conc", windows=(14, 70))
    d.rectangle([40, 68, 50, 83], fill=S["glass"][0])                  # doorway
    for k in range(25): px(im, r.randint(4, 110), r.randint(83, 85), (S["brick"] + S["conc"])[r.randrange(9)])
    return outline(im, S["outline"]), (60, 84)

def fence(S, seed, n=5):
    """Soviet concrete panel fence (diamond relief), one panel fallen, posts"""
    r = rr(seed); pw = 18; w = pw * n + 2; h = 22; im = blank(w, h)
    for p in range(n):
        x0 = p * pw
        if p == 2:                                                      # fallen flat
            for y in range(h - 7, h):
                for x in range(x0 + 1, x0 + pw):
                    px(im, x, y, S["conc"][3] if (x + y) % 6 else S["conc"][2])
            continue
        for y in range(4, h - 1):
            for x in range(x0 + 1, x0 + pw):
                k = (abs((x - x0) % 6 - 3) + abs((y - 4) % 6 - 3))
                c = S["conc"][3] if k < 2 else S["conc"][2] if k < 4 else S["conc"][1]
                if y > h - 4: c = S["moss"][0] if r.random() < .5 else S["conc"][0]
                px(im, x, y, c)
        for x in range(x0 + 1, x0 + pw): px(im, x, 3, S["conc"][4])
    for p in range(n + 1):
        for y in range(1, h): px(im, p * pw, y, S["conc"][1]); px(im, p * pw + 1, y, S["conc"][2])
    return outline(im, S["outline"]), (w // 2, h - 1)

def pole(S):
    w, h = 18, 70; im = blank(w, h)
    for y in range(6, h):
        px(im, 8, y, S["conc"][3]); px(im, 9, y, S["conc"][2]); px(im, 10, y, S["conc"][1])
    for x in range(1, 17): px(im, x, 9, S["dead"][1]); px(im, x, 10, S["dead"][0])
    for x in (2, 15): px(im, x, 8, S["conc"][4])
    return outline(im, S["outline"]), (9, h - 1)

def car(S, seed):
    """rusted sedan wreck, 3/4: roof, glass, side, wheels gone"""
    r = rr(seed); w, h = 38, 22; im = blank(w, h); d = ImageDraw.Draw(im)
    d.rectangle([2, 6, 35, 17], fill=S["rust"][1]); d.rectangle([2, 6, 35, 8], fill=S["rust"][2])
    d.rectangle([10, 2, 27, 9], fill=S["rust"][2]); d.rectangle([12, 3, 25, 8], fill=S["glass"][0])
    d.rectangle([2, 15, 35, 19], fill=S["rust"][0])
    for x, y in ((6, 18), (29, 18)): d.rectangle([x, y, x + 4, y + 2], fill=S["tar"][0])
    for k in range(50): px(im, r.randint(2, 35), r.randint(6, 17), S["rust"][r.randrange(4)])
    for k in range(6): px(im, r.randint(4, 33), r.randint(5, 9), S["moss"][1])
    return outline(im, S["outline"]), (19, h - 2)

def barrel(S, seed):
    im = blank(9, 12); d = ImageDraw.Draw(im)
    d.rectangle([1, 2, 7, 11], fill=S["rust"][1]); d.rectangle([1, 1, 7, 3], fill=S["rust"][2])
    d.line([(1, 7), (7, 7)], fill=S["rust"][0]); d.line([(2, 2), (2, 10)], fill=S["rust"][3])
    return outline(im, S["outline"]), (4, 11)

def debris(S, seed):
    r = rr(seed); im = blank(14, 6)
    for k in range(8): px(im, r.randrange(14), r.randrange(6), (S["conc"] + S["rust"] + S["brick"])[r.randrange(13)])
    return im, (7, 5)

def unit(armor, flip=False):
    import make_armor_avatars as av
    rw = [a for a in av.ARMORS if a[0] == armor][0]
    b = av.build(rw[0], rw[3], rw[4]).convert("RGBA")
    return b.transpose(Image.FLIP_LEFT_RIGHT) if flip else b

# ------------------------------------------------------------------ compose
def scene(key, shadow=None, sun=None, post_key=None, raw=False):
    S = dict(STYLES[key]); R.seed(3)
    if shadow: S["shadow"] = shadow
    if sun: S["sun"] = sun
    im, road = ground(S)
    items = []                                     # (sprite, pivot, at, shadowsize)
    def add(spr_piv, at, sh=None): items.append((spr_piv[0], spr_piv[1], at, sh))
    # forest on the left / bottom
    for i, (x, y) in enumerate(((22, 60), (48, 44), (14, 98), (60, 82), (34, 120), (96, 40), (8, 178), (40, 206), (118, 212))):
        add(birch(S, 10 + i, 40 + (i * 7) % 12), (x, y), (12, 4))
    for i, (x, y) in enumerate(((74, 30), (6, 140), (88, 196), (150, 214))): add(spruce(S, 30 + i, 44 + i * 4), (x, y), (12, 4))
    for i, (x, y) in enumerate(((128, 70), (176, 206))): add(deadtree(S, 40 + i), (x, y), (6, 3))
    for i in range(140):
        r = rr(100 + i); x, y = r.randrange(W), r.randrange(H)
        if road[min(H - 1, y), min(W - 1, x)]: continue
        add(grass_clump(S, 200 + i, dry=r.random() < .5, tall=r.random() < .4), (x, y))
    for i, (x, y) in enumerate(((112, 100), (66, 160), (190, 182), (360, 140), (330, 200))): add(bush(S, 60 + i), (x, y), (14, 4))
    # ruin, fence, pole, car, junk
    add(ruin(S, 7), (300, 92), (110, 8))
    add(fence(S, 8), (300, 128), (80, 3))
    add(pole(S), (186, 74), (5, 3))
    add(car(S, 9), (142, 150), (30, 6))
    add(barrel(S, 1), (252, 108), (8, 3)); add(barrel(S, 2), (260, 111), (8, 3))
    for i, (x, y) in enumerate(((236, 104), (268, 100), (120, 160), (330, 116))): add(debris(S, 70 + i), (x, y))
    # people
    add((unit("Rookie Field Suit"), (8, 15)), (150, 116), (11, 3))
    add((unit("Raider Jacket", True), (8, 15)), (262, 98), (11, 3))
    add((unit("Raider Stitched Coat", True), (8, 15)), (330, 150), (11, 3))
    # shadows
    shade = Image.new("RGBA", (W, H))
    sd = ImageDraw.Draw(shade); scol = (24, 20, 26, 120) if key != "C" else (34, 26, 44, 150)
    for spr, piv, (x, y), sh in items:
        if S["shadow"] == "blob" and sh:
            sd.ellipse([x - sh[0] // 2 + 2, y - sh[1] // 2, x + sh[0] // 2 + 2, y + sh[1] // 2], fill=scol)
        elif S["shadow"] == "cast" and sh:
            # long shadow: the silhouette squashed and sheared away from a low sun at the top-left
            a = np.array(spr)[..., 3] > 0; hh, ww = a.shape
            for yy in range(hh):
                for xx in range(ww):
                    if a[yy, xx]:
                        up = hh - 1 - yy
                        sx = x + (xx - piv[0]) + int(up * S["sun"][0]); sy = y + int(up * S["sun"][1])
                        if 0 <= sx < W and 0 <= sy < H: shade.putpixel((sx, sy), scol)
    if S["shadow"] != "none": im.alpha_composite(shade.filter(ImageFilter.BoxBlur(0.6)) if S["shadow"] == "blob" else shade)
    # wires between the pole and off-screen poles (sagging)
    for (x0, y0, x1, y1) in ((-10, 20, 186, 8), (186, 8, 400, 30)):
        for t in np.linspace(0, 1, 400):
            x = x0 + (x1 - x0) * t; y = y0 + (y1 - y0) * t + 9 * math.sin(math.pi * t)
            if 0 <= x < W and 0 <= y < H: im.putpixel((int(x), int(y)), (20, 20, 20, 255))
    for spr, piv, (x, y), sh in sorted(items, key=lambda it: it[2][1]):
        im.alpha_composite(spr, (int(x - piv[0]), int(y - piv[1])))
    if raw: return im.convert("RGB")
    return post(im.convert("RGB"), post_key or key)

def post(im, key):
    a = np.asarray(im).astype(np.float32) / 255
    Hh, Ww = a.shape[:2]; yy, xx = np.mgrid[0:Hh, 0:Ww]
    pxp, pyp = 150, 108                               # player
    dist = np.sqrt((xx - pxp) ** 2 + (yy - pyp) ** 2)
    lum = a.mean(-1, keepdims=True)
    if key == "A":
        a = lum + (a - lum) * 0.82
        a = a * 0.95 + np.array([0.04, 0.04, 0.035])
        fog = np.clip((dist - 150) / 80, 0, 1)[..., None]                    # past the vision radius: darker
        a = a * (1 - 0.5 * fog)
        v = np.sqrt(((xx - Ww / 2) / (Ww / 2)) ** 2 + ((yy - Hh / 2) / (Hh / 2)) ** 2)
        a = a * (1 - 0.35 * np.clip(v - .6, 0, 1))[..., None]
        grain = 2.5
    elif key == "B":
        a = lum + (a - lum) * 0.55
        ang = np.arctan2(yy - pyp, xx - pxp); aim = math.atan2(-10, 110)
        da = np.abs((ang - aim + np.pi) % (2 * np.pi) - np.pi)
        cone = np.clip(1 - da / 0.55, 0, 1) ** 0.6 * np.clip(1 - dist / 300, 0, 1)
        near = np.clip(1 - dist / 34, 0, 1)
        light = np.clip(np.maximum(cone ** 0.7, near) , 0, 1)[..., None]
        aim = 0
        a = a * (0.34 + 1.1 * light) + light * np.array([0.06, 0.05, 0.02])
        grain = 6
    else:
        a = a * np.array([1.08, 1.0, 0.86])                                 # warm low sun
        hi = np.clip(lum - .45, 0, 1); a = a + hi * np.array([0.25, 0.18, 0.05])
        a = a * 0.9 + np.array([0.09, 0.08, 0.06])                         # haze
        v = np.sqrt(((xx - Ww * .3) / Ww) ** 2 + ((yy - Hh * .2) / Hh) ** 2)
        a = a * (1 - 0.28 * np.clip(v - .25, 0, 1))[..., None]
        grain = 2
    a = a + np.random.RandomState(1).normal(0, grain / 255, (Hh, Ww, 1))
    out = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))
    return out

if __name__ == "__main__":
    from PIL import ImageFont
    f = ImageFont.truetype("/home/claude/art/fonts/Silkscreen-Regular.ttf", 28)
    names = {"A": "A  ZONE REALISM  (ZERO SIEVERT)", "B": "B  GRIM WOOD  (DARKWOOD)", "C": "C  GOLDEN DECAY  (TARKOVSKY / ROADSIDE PICNIC)"}
    for k in sys.argv[1:] or "ABC":
        im = scene(k).resize((W * SC, H * SC), Image.NEAREST)
        im.save(f"/tmp/claude-0/concept_{k}.png"); print(k)
