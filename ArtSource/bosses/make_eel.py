"""The eel of Sub-Station Depths (Yan, 2026-11-04: "moray eel but reeeeealy long, darker green swamp like color with colored spots
around, white eyes as if blind"), art round 1 - not in the game yet. Drawn at double resolution like the HD mutants and the brain
(32 px a tile, ppu 32). Procedural shapes snapped to the pixel grid, the locked rules: near-black outline, flat 3-4 tone shading,
saturated colour only in the spots, the eyes and the electricity.
    python3 ArtSource/bosses/make_eel.py [depths_render.png]   -> maps/renders/boss_eel/
Poses: the head (side, the name card), swimming (top-down), lurking under black water, wrapped round a platform, half out of the water.
"""
import sys, os, math, random
import numpy as np
from PIL import Image, ImageDraw

T = 32                                                           # px a tile at HD
def hexc(h, a=255): h = h.lstrip('#'); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)
OUT = hexc('#141617')
SKIN = [hexc(c) for c in ('#1b2414', '#2b3820', '#3e4d2b', '#56653a')]      # shade, base, lit, highlight: dark swamp green
BELLY = [hexc('#4e5230'), hexc('#6b6a3a'), hexc('#868248')]
SPOTS = [hexc('#a8792c'), hexc('#94502a'), hexc('#3a7a70'), hexc('#a9a24a')]   # ochre, rust, teal, sick yellow (muted)
EYE_W, EYE_R, EYE_C = hexc('#eef0ea'), hexc('#aeb6ae'), hexc('#d3d8d0')      # milky, blind: no pupil
MOUTH, MOUTH_L, TOOTH = hexc('#3e1416'), hexc('#6e2426'), hexc('#e8e0c8')
ZAP, ZAP_C, ZAP_G = hexc('#ffd23c'), hexc('#fff27a'), hexc('#ffd23c', 70)
SHADOW = hexc('#081008', 165)
WATER = [hexc('#141c1a'), hexc('#1a2422'), hexc('#24302c')]
LIGHT = np.array([-0.55, -0.6, 0.58]); LIGHT /= np.linalg.norm(LIGHT)

def outline(im, col=OUT):
    a = np.array(im)[:, :, 3] > 0
    n = np.zeros_like(a)
    n[1:] |= a[:-1]; n[:-1] |= a[1:]; n[:, 1:] |= a[:, :-1]; n[:, :-1] |= a[:, 1:]
    o = np.array(im); o[n & ~a] = col
    return Image.fromarray(o)

# ------------------------------------------------------------------ the body: discs along a path, later discs on top
def body(path, radius, w, h, seed=1, head=True):
    """Renders a tube along `path` (list of (x, y), 1 px apart, head first). Returns an RGBA layer and the owner index per pixel."""
    P = np.array(path, float); N = len(P)
    tang = np.gradient(P, axis=0); tang /= np.linalg.norm(tang, axis=1, keepdims=True) + 1e-9
    nrm = np.stack([-tang[:, 1], tang[:, 0]], 1)
    own = np.full((h, w), -1, int); S = np.zeros((h, w))
    order = range(N - 1, -1, -1)                                 # draw tail -> head: the head ends on top
    for i in order:
        r = radius(i, N); x, y = P[i]
        x0, x1, y0, y1 = int(x - r - 1), int(x + r + 2), int(y - r - 1), int(y + r + 2)
        for yy in range(max(0, y0), min(h, y1)):
            for xx in range(max(0, x0), min(w, x1)):
                dx, dy = xx + 0.5 - x, yy + 0.5 - y
                if dx * dx + dy * dy <= r * r:
                    own[yy, xx] = i; S[yy, xx] = (dx * nrm[i, 0] + dy * nrm[i, 1]) / r
    rnd = random.Random(seed)
    spots = []
    i = 22 if head else 4
    while i < N - 6:                                             # spots every few px, either side of the ridge
        spots.append((i, rnd.uniform(-0.7, 0.7), rnd.uniform(2.2, 3.6) * max(0.5, radius(i, N) / 22), rnd.choice(SPOTS)))
        i += rnd.randint(9, 16)
    img = np.zeros((h, w, 4), np.uint8)
    ys, xs = np.nonzero(own >= 0)
    for yy, xx in zip(ys, xs):
        i = own[yy, xx]; s = max(-1, min(1, S[yy, xx])); r = radius(i, N)
        n3 = np.array([nrm[i, 0] * s, nrm[i, 1] * s, math.sqrt(max(0, 1 - s * s))])
        L = float(n3 @ LIGHT)
        c = SKIN[3] if L > 0.86 else SKIN[2] if L > 0.6 else SKIN[1] if L > 0.25 else SKIN[0]
        if abs(s) < 0.1 and i > 14: c = SKIN[0]                  # the dorsal fin's ridge
        for (si, ss, sr, sc) in spots:
            if abs(si - i) < sr + 1 and (si - i) ** 2 + ((ss - s) * r) ** 2 <= sr * sr:
                c = sc if L > 0.25 else tuple(int(v * 0.65) for v in sc[:3]) + (255,)
        img[yy, xx] = c
    if head:                                                     # blind white eyes either side of the head, two nostril tubes
        for side in (-1, 1):
            i = 9; cx, cy = P[i] + nrm[i] * side * radius(i, N) * 0.62
            for yy in range(int(cy) - 2, int(cy) + 3):
                for xx in range(int(cx) - 2, int(cx) + 3):
                    d = (xx + 0.5 - cx) ** 2 + (yy + 0.5 - cy) ** 2
                    if 0 <= xx < w and 0 <= yy < h and d <= 5.2: img[yy, xx] = EYE_R if d > 3.2 else EYE_W
            img[int(cy), int(cx)] = EYE_C
            nx, ny = P[1] + nrm[1] * side * 2.2
            if 0 <= int(nx) < w and 0 <= int(ny) < h: img[int(ny), int(nx)] = OUT
    lay = Image.fromarray(img)
    lay = outline(lay)
    o = np.array(lay)                                             # where a body crosses over itself, a line between them
    for yy, xx in zip(ys, xs):
        i = own[yy, xx]
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            y2, x2 = yy + dy, xx + dx
            if 0 <= y2 < h and 0 <= x2 < w and own[y2, x2] >= 0 and own[y2, x2] < i - 2 * radius(i, N) - 4:
                o[y2, x2] = OUT
    return Image.fromarray(o), own

def taper(R, head_len=16):
    """Radius along the body: a blunt snout, the head a bit wider behind the eyes, a long even body, a thin tail."""
    def r(i, N):
        if i < head_len: return 4 + (R * 1.08 - 4) * min(1, i / (head_len * 0.8))
        t = (i - head_len) / max(1, N - head_len)
        return max(2.0, R * (1.0 if t < 0.55 else 1 - ((t - 0.55) / 0.45) ** 1.3 * 0.92))
    return r

def resample(pts, step=1.0):
    out = [pts[0]]; acc = 0
    for a, b in zip(pts, pts[1:]):
        d = math.dist(a, b); k = 0
        while acc + d >= step * (k + 1):
            t = (step * (k + 1) - acc) / d; out.append((a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t)); k += 1
        acc = acc + d - step * k
    return out

# ------------------------------------------------------------------ the head from the side (name card, the strike)
HEAD_ANCHOR = (8, 30)        # the back of the head's middle, where the body joins (facing right, scale 1)

def side_head(scale=1.0, gape=0.55, face=1):
    """A moray's head in profile: a tall bulky back, a long snout tapering to a point, the long lower jaw hinged open, needle
    teeth, a milky blind eye high above the mouth's corner, a nostril tube at the tip. RGBA, facing right if face=1."""
    s = scale; W, H = int(124 * s), int(84 * s)
    ang = math.radians(14 + gape * 32)
    hx, hy = 46, 33                                                    # the jaw's hinge (the mouth's corner)
    tip = (hx + 50 * math.cos(ang), hy + 50 * math.sin(ang))   # shorter than the upper jaw: the snout overhangs to a point
    dorsal = [(0, 5), (18, 3), (36, 5), (58, 11), (80, 19), (98, 26), (114, 31)]            # top of the head to the snout tip
    upper = [(114, 31), (104, 32), (84, 31), (62, 31), (hx, hy - 1)]                         # the upper jaw's edge
    lower_top = [(hx, hy + 1), ((hx + tip[0]) / 2, (hy + tip[1]) / 2 + 2), (tip[0] - 2, tip[1] + 1), (tip[0] + 2, tip[1] - 3)]   # the jaw's tip hooks up               # the lower jaw's edge
    lower_bot = [(tip[0] + 3, tip[1] + 1), ((hx + tip[0]) / 2 + 2, (hy + tip[1]) / 2 + 9), (hx - 4, hy + 18), (22, 56), (0, 58)]
    def S(pts): return [(x * s, y * s) for x, y in pts]
    head = Image.new('L', (W, H)); ImageDraw.Draw(head).polygon(S(dorsal + upper + lower_top + lower_bot), fill=255)
    mouth = Image.new('L', (W, H)); ImageDraw.Draw(mouth).polygon(S(upper[1:] + lower_top[::-1][:-1] + [lower_top[-1]]), fill=255)
    hm, mm = np.array(head) > 0, np.array(mouth) > 0
    hm &= ~mm
    a = np.zeros((H, W, 4), np.uint8)
    top = np.full(W, H); bot = np.full(W, -1)
    for x in range(W):
        ys = np.nonzero(hm[:, x])[0]
        if len(ys): top[x], bot[x] = ys.min(), ys.max()
    jaw = np.zeros((H, W), bool)                                       # the lower jaw: below the line hinge -> tip
    for y in range(H):
        for x in range(W):
            if hm[y, x]:
                X, Y = x / s, y / s
                if X > hx - 6 and Y > hy + (X - hx) * math.tan(ang) * 0.98: jaw[y, x] = True
    for y in range(H):
        for x in range(W):
            if not hm[y, x]: continue
            d = (y - top[x]) / s; e = (bot[x] - y) / s
            if jaw[y, x]:
                a[y, x] = BELLY[2] if e < 2.5 else BELLY[1] if e < 5 else SKIN[1] if d > 0 else SKIN[2]
            else:
                a[y, x] = SKIN[3] if d < 1.6 else SKIN[2] if d < 6 else SKIN[1] if (bot[x] - y) / s > 9 or x / s > hx else \
                          (BELLY[0] if e < 6 else SKIN[0])
    a[mm] = MOUTH
    for y in range(H):                                                 # the mouth's lit inside along the upper jaw
        for x in range(W):
            if mm[y, x] and y > 0 and not mm[y - 1, x]: a[y, x] = MOUTH_L
    im = Image.fromarray(a); d = ImageDraw.Draw(im)
    rnd = random.Random(7)
    for k in range(7):                                                 # needle fangs: upper row down, lower row up, uneven
        X = hx + 8 + k * 8 + rnd.randint(-1, 1); Y = 31
        L = rnd.choice((3, 4, 5, 6)); d.line(S([(X, Y), (X - 1, Y + L)]), fill=TOOTH, width=max(1, int(s)))
    for k in range(6):
        t = (k + 1) / 7.5; X = hx + (tip[0] - hx) * t; Y = hy + (tip[1] - hy) * t + 1
        L = rnd.choice((3, 4, 5)); d.line(S([(X, Y), (X + 1, Y - L)]), fill=TOOTH, width=max(1, int(s)))
    ex, ey, er = 64 * s, 17 * s, 4.6 * s                               # the blind eye: milky, no pupil, a grey rim
    d.ellipse([ex - er - s, ey - er - s, ex + er + s, ey + er + s], fill=EYE_R); d.ellipse([ex - er, ey - er, ex + er, ey + er], fill=EYE_W)
    for (dx, dy) in ((1, 1), (-1, 0), (0, -1)): im.putpixel((int(ex + dx * s), int(ey + dy * s)), EYE_C)
    for (X, Y, r, c) in ((16, 16, 3.2, SPOTS[0]), (30, 30, 2.6, SPOTS[2]), (40, 12, 2.4, SPOTS[3]), (10, 40, 3, SPOTS[1]), (52, 22, 2, SPOTS[1]), (26, 46, 2.4, SPOTS[0])):
        d.ellipse([(X - r) * s, (Y - r) * s, (X + r) * s, (Y + r) * s], fill=c)
    d.line(S([(104, 25), (108, 21)]), fill=OUT, width=max(1, int(s)))  # the nostril tube
    im = outline(im)
    return im if face == 1 else im.transpose(Image.FLIP_LEFT_RIGHT)

def put_head(im, hd, at, scale, face):
    """Pastes a side head so its back (HEAD_ANCHOR) sits on `at`, the end of the body it grows from."""
    ax, ay = HEAD_ANCHOR[0] * scale, HEAD_ANCHOR[1] * scale
    if face == -1: ax = hd.width - ax
    im.alpha_composite(hd, (int(at[0] - ax), int(at[1] - ay)))

def zap(im, pts, seed, n=3):
    """Electric arcs crawling along the coils: jagged pixel lines with a soft glow."""
    rnd = random.Random(seed); d = ImageDraw.Draw(im)
    for k in range(n):
        i = rnd.randrange(0, len(pts) - 30); seg = pts[i:i + rnd.randint(14, 30):3]
        jag = [(x + rnd.randint(-5, 5), y + rnd.randint(-5, 5)) for x, y in seg]
        d.line(jag, fill=ZAP_G, width=5); d.line(jag, fill=ZAP, width=1)
        for x, y in jag[::2]: im.putpixel((int(x), int(y)), ZAP_C)

def water(w, h, seed=3):
    rnd = random.Random(seed); a = np.zeros((h, w, 4), np.uint8); a[:] = WATER[1]
    for _ in range(w * h // 60):
        x, y = rnd.randrange(w), rnd.randrange(h); a[y, x:x + rnd.randint(2, 6)] = WATER[rnd.choice((0, 2))]
    return Image.fromarray(a)

def platform(w, h):
    """A stand-in raised platform (the flooded levels aren't built): a concrete slab with a face."""
    im = Image.new('RGBA', (w, h + 12)); d = ImageDraw.Draw(im)
    d.rectangle([0, 0, w - 1, h - 1], fill=hexc('#6b6e6a')); d.rectangle([0, h, w - 1, h + 11], fill=hexc('#3e413e'))
    d.rectangle([0, 0, w - 1, 2], fill=hexc('#8a8d88'))
    rnd = random.Random(5)
    for _ in range(40): x, y = rnd.randrange(w), rnd.randrange(h); d.point((x, y), fill=hexc('#5a5d59'))
    return outline(im)

# ------------------------------------------------------------------ the poses
def swim(w=560, h=200, R=22):
    pts = resample([(40 + x, h / 2 + math.sin(x / 70) * 42 + math.sin(x / 23) * 6) for x in range(0, w - 80, 2)])
    return body(pts, taper(R), w, h)[0]

def lurk(w=560, h=200, R=22):
    im = swim(w, h, R); a = np.array(im); m = a[:, :, 3] > 0
    eye = (a[:, :, :3] == EYE_W[:3]).all(2)
    out = np.zeros_like(a); out[m] = SHADOW; out[eye] = hexc('#eef0ea', 110)       # only a shadow, and the eyes glimmer
    return Image.fromarray(out)

def wrap(R=22):
    w, h = 340, 300; cx, cy = 170, 160; pw, ph = 112, 84
    # path: the head reared up at the top right, the body round the slab 1.25 times, then away down-left into the water
    pts = []
    for k in range(0, 470):
        a = -0.6 + k / 470 * 2 * math.pi * 1.25
        rx, ry = pw * 0.62 + 6 + k * 0.03, ph * 0.62 + 4 + k * 0.03
        pts.append((cx + math.cos(a) * rx, cy + math.sin(a) * ry))
    tail = [(pts[-1][0] - k * 1.6, pts[-1][1] + k * 0.5) for k in range(1, 100)]
    pts = resample(pts + tail)
    eel, own = body(pts, taper(R, head_len=1), w, h, seed=4, head=False)
    P = np.array(pts)
    bg = water(w, h); plat = platform(pw, ph)
    ea = np.array(eel); back = ea.copy(); front = ea.copy()
    behind = np.zeros(own.shape, bool); ok = own >= 0; behind[ok] = P[own[ok], 1] < cy
    a3 = ea[:, :, 3] > 0
    back[~behind] = 0; front[behind] = 0
    # outline pixels (own = -1) go with whichever body part is next to them: put them in both, the platform covers the back ones
    od = own.copy()                                             # each outline pixel takes the owner of the body pixel next to it
    for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)): od = np.maximum(od, np.roll(np.roll(own, dy, 0), dx, 1))
    edge = a3 & ~ok; eb = np.zeros(own.shape, bool); eb[edge] = P[np.clip(od[edge], 0, None), 1] < cy
    back[edge & ~eb] = 0; front[edge & eb] = 0
    bg.alpha_composite(Image.fromarray(back)); bg.alpha_composite(plat, (cx - pw // 2, cy - ph // 2 - 6)); bg.alpha_composite(Image.fromarray(front))
    sc = R * 0.95 / 29                                          # the head's back is as tall as the body is wide
    hd = side_head(sc, gape=0.7, face=-1)                       # the head rises off the coil and strikes at you
    neck = resample([(pts[0][0], pts[0][1]), (pts[0][0] + 14, pts[0][1] - 36), (pts[0][0] + 2, pts[0][1] - 70)])
    nk, _ = body(neck[::-1], lambda i, N: R * 0.95, w, h, seed=9, head=False)
    bg.alpha_composite(nk); put_head(bg, hd, neck[-1], sc, -1)
    zap(bg, pts[:320], 11, 5)
    return bg

def rear(R=24):
    w, h = 340, 420; bx, by = 170, 330
    coil = resample([(bx + math.cos(a) * (70 - a * 4), by + math.sin(a) * (40 - a * 2.2)) for a in np.linspace(0, 2 * math.pi * 1.6, 260)])
    eel, _ = body(coil, taper(R, head_len=1), w, h, seed=6, head=False)
    im = water(w, h)
    d = ImageDraw.Draw(im); d.ellipse([bx - 52, by - 20, bx + 52, by + 20], fill=SHADOW)          # its shadow on the water
    for k in range(0, 360, 9):                                                                     # a ring of disturbed water
        a = math.radians(k); im.putpixel((int(bx + math.cos(a) * 96), int(by + math.sin(a) * 50)), hexc('#7d8d86'))
    im.alpha_composite(eel)
    col = resample([(bx, by - 6)] + [(bx + math.sin(t * 5.2) * 34 * min(1, t * 1.6), by - 6 - t * 220) for t in np.linspace(0.05, 1, 30)])   # an S up the screen
    up, _ = body(col[::-1], lambda i, N: R * (0.88 + 0.12 * i / N), w, h, seed=8, head=False)   # thicker at the water
    im.alpha_composite(up)
    sc = R * 0.88 / 29
    hd = side_head(sc * 1.15, gape=0.9, face=-1)
    put_head(im, hd, col[-1], sc * 1.15, -1)
    zap(im, col, 13, 3)
    return im

def label(im, text):
    d = ImageDraw.Draw(im); d.rectangle([4, 4, 8 + 6 * len(text), 16], fill=hexc('#1b1d1f')); d.text((6, 5), text, fill=hexc('#b8bcc0'))

def x(im, k): return im.resize((im.width * k, im.height * k), Image.NEAREST)

if __name__ == "__main__":
    here = os.path.dirname(os.path.abspath(__file__)); root = os.path.join(here, '..', '..')
    out = os.path.join(root, 'maps', 'renders', 'boss_eel'); os.makedirs(out, exist_ok=True)
    ar = Image.open(os.path.join(root, 'Assets', 'Resources', 'Sprites', 'armor_atlas.png')).convert('RGBA')
    man = x(ar.crop((0, 0, 16, 16)), 2)                                                            # a character at HD scale (1 tile = 32 px)
    head = side_head(1.0); sw = swim(); lk = lurk(); wr = wrap(); re_ = rear()
    for n, im in (('eel_head', head), ('eel_swim', sw), ('eel_lurk', lk), ('eel_wrap', wr), ('eel_rear', re_)): im.save(os.path.join(out, n + '.png'))
    # the sheet: everything at 2x (so a game pixel is 2x2), a character beside each for scale
    bgc = hexc('#1b1d1f')
    swb = water(sw.width, sw.height); swb.alpha_composite(sw); swb.alpha_composite(man, (8, 20))
    lkb = water(lk.width, lk.height); lkb.alpha_composite(lk); lkb.alpha_composite(man, (8, 20))
    wr.alpha_composite(man, (wr.width // 2 + 10, 150)); re_.alpha_composite(man, (40, 300))
    hb = Image.new('RGBA', (head.width + 16, head.height + 16), bgc); hb.alpha_composite(head, (8, 8))
    tiles = [(x(hb, 3), "1  the head, side view (name card) - 3x"), (x(swb, 2), "2  swimming, from above (part of it) - 2x"),
             (x(lkb, 2), "3  lurking under black water: only its shadow and the blind eyes - 2x"),
             (x(wr, 2), "4  the strike: wrapped round a platform (stand-in slab) - 2x"), (x(re_, 2), "5  half out of the water, about to strike - 2x")]
    W = max(t[0].width for t in tiles[:3]) + tiles[3][0].width + tiles[4][0].width + 60
    H = max(sum(t[0].height + 30 for t in tiles[:3]), tiles[3][0].height + 30, tiles[4][0].height + 30) + 10
    sheet = Image.new('RGBA', (W, H), bgc); y = 10
    for im, t in tiles[:3]: sheet.alpha_composite(im, (10, y + 20)); ImageDraw.Draw(sheet).text((10, y + 4), t, fill=hexc('#b8bcc0')); y += im.height + 30
    xx = max(t[0].width for t in tiles[:3]) + 30
    for im, t in tiles[3:]: sheet.alpha_composite(im, (xx, 30)); ImageDraw.Draw(sheet).text((xx, 14), t, fill=hexc('#b8bcc0')); xx += im.width + 20
    sheet.convert('RGB').save(os.path.join(out, 'eel_sheet.png'))
    # in the real map: the Depths render (unlit), the basin at HD scale, the eel lurking and half out, a character on the ledge
    if len(sys.argv) > 1:
        m = Image.open(sys.argv[1]).convert('RGBA'); m = x(m, 2)
        crop = m.crop((33 * T, 25 * T, 58 * T, 43 * T)).copy()                                    # the basin and the hall's south wall
        lk2 = lurk(520, 200, 22); crop.alpha_composite(lk2, (90, 190))
        crop.alpha_composite(man, (6 * T + 8, 4 * T))                                             # on the ledge
        label(crop, "in the real Depths basin: a character on the ledge, the eel lurking under the black water")
        crop.convert('RGB').save(os.path.join(out, 'eel_in_basin.png'))
    print("written", out)
