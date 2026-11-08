"""SUPERSEDED as the eel's art by Yan's own drawing (eel_native.py / eel_poses.py, 2026-11-05); kept as the helper library eel_poses.py uses
(basin crops, the stand-in platform, curves) and for round 2's procedural look.

The eel of Sub-Station Depths (Yan, 2026-11-04: "moray eel but reeeeealy long, darker green swamp like color with colored spots
around, white eyes as if blind"), art round 2 - not in the game. Round 1 was "waaay to short" and "terrible quality compared to the
brain and mutants"; this one follows the brain (make_brain_vat.py): HD like the brain and the HD mutants (32 px a tile, ppu 32),
the near-black outline, flat 3-tone shading lit from the top-left, one accent colour (a sick yellow: the spots, the electricity).
The body is segments along a curve (each an oval drawn back to front by screen height, so it can coil and bend); a moray's mottled
net runs along it in body space so it doesn't break at the joints; a dorsal fin ridge along the spine; a heavy-jawed moray head
in profile (only ever flipped, never rotated) with needle teeth and milky blind eyes.
    python3 ArtSource/bosses/make_eel.py <depths_render.png>   -> maps/renders/boss_eel/  (the render: Tools/render/render.sh sub_station_depths)
"""
import sys, os, math, random
import numpy as np
from PIL import Image, ImageDraw

T = 32                                               # px a tile (HD)
R = 20                                               # body radius: 1.25 tiles thick
def hexc(h, a=255): h = h.lstrip('#'); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)
OUT = hexc('#141617')
LIT, BASE, SHADE, NET = hexc('#55673b'), hexc('#3a4a28'), hexc('#242e19'), hexc('#161d10')
FIN, FIN_L = hexc('#2c3820'), hexc('#4a5a33')
ACC, ACC_D = hexc('#c9b04a'), hexc('#8a7a34')        # the one accent: spots (and the electricity)
JAW_L, JAW = hexc('#6d7044'), hexc('#4f5532')         # the pale underside of the jaw, in the body's family
MOUTH, MOUTH_L, GUM = hexc('#3a1013'), hexc('#5e1c1f'), hexc('#7e3436')
TOOTH, TOOTH_D = hexc('#e8e0c8'), hexc('#a59d86')
EYE, EYE_C, EYE_R = hexc('#e9ece4'), hexc('#c4cac0'), hexc('#7d867a')
SHADOW = hexc('#081008', 165)
LX, LY = -0.55, -0.83                                # light from the top-left (screen)

def outline(a):
    """a: HxWx4 uint8. The near-black outline round everything opaque."""
    m = a[:, :, 3] > 0; n = np.zeros_like(m)
    n[1:] |= m[:-1]; n[:-1] |= m[1:]; n[:, 1:] |= m[:, :-1]; n[:, :-1] |= m[:, 1:]
    a = a.copy(); a[n & ~m] = OUT; return a

def resample(pts, step=1.0):
    out = [pts[0]]; acc = 0.0
    for a, b in zip(pts, pts[1:]):
        d = math.dist(a, b); k = 0
        while d > 0 and acc + d >= step * (k + 1):
            t = (step * (k + 1) - acc) / d; out.append((a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t)); k += 1
        acc = acc + d - step * k
    return np.array(out)

def bez(*pts, n=60):
    """A smooth curve through control points (Catmull-Rom), as a dense point list."""
    P = [pts[0]] + list(pts) + [pts[-1]]; out = []
    for i in range(1, len(P) - 2):
        p0, p1, p2, p3 = map(np.array, (P[i - 1], P[i], P[i + 1], P[i + 2]))
        for t in np.linspace(0, 1, n, endpoint=False):
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    out.append(np.array(pts[-1])); return [tuple(p) for p in out]

# ------------------------------------------------------------------ the mottled net, in body space (s along, v across)
class Mottle:
    """A moray's mottling: big irregular blotches (cells ~12 px, jittered hard), a dark net between them that breaks up in places,
    and a few small accent spots (the one accent colour stays rare)."""
    C = 12
    def __init__(self, length, seed=2):
        rnd = random.Random(seed); self.grid = {}; self.rnd = rnd
        for s in range(-14, int(length) + 14, self.C):
            for v in range(-R - 6, R + 8, self.C):
                q = (s + rnd.uniform(-5, 5), v + rnd.uniform(-5, 5), rnd.random() < 0.07, rnd.random() < 0.35)
                self.grid.setdefault(int(q[0] // self.C), []).append(q)
    def at(self, s, v):
        k = int(s // self.C)
        near = [q for kk in (k - 1, k, k + 1) for q in self.grid.get(kk, [])]
        d = sorted(((s - a) ** 2 + (v - b) ** 2, sp, gap) for a, b, sp, gap in near)
        d1, d2 = math.sqrt(d[0][0]), math.sqrt(d[1][0])
        if d2 - d1 < 1.6 and not (d[0][2] and d[1][2]): return 'net'       # the net, broken where two "gap" cells meet
        if d[0][1] and d1 < 1.8: return 'spot'
        if d1 < 3.2 and d[0][2]: return 'blot'                            # some cells carry a dark blotch at the middle
        return None

def radius_at(j, n, head):
    """Full thickness behind the head, a long even body, a tail that thins to a point over its last third."""
    t = j / max(1, n - 1)
    if not head and t < 0.03: return R * (0.75 + 8 * t)
    return R if t < 0.66 else max(3.0, R * (1 - ((t - 0.66) / 0.34) ** 1.15 * 0.88))

def body(P, w, h, head=True, seed=2, split_y=None):
    """Draws the body along the spine P (head end first). Returns (back, front) layers: segments whose middle is above split_y go
    in back (behind a platform there), the rest in front. With split_y None everything is in front."""
    n = len(P); tang = np.gradient(P, axis=0); tang /= np.linalg.norm(tang, axis=1, keepdims=True) + 1e-9
    mot = Mottle(n, seed)
    step = int(R * 0.55); segs = list(range(0, n, step))
    order = sorted(segs, key=lambda c: P[c][1])                 # back to front by screen height
    layers = {'back': np.zeros((h, w, 4), np.uint8), 'front': np.zeros((h, w, 4), np.uint8)}
    for c in order:
        r = radius_at(c, n, head); hl = max(r * 0.95, step * 1.2)
        tx, ty = tang[c]; nx, ny = -ty, tx
        if ny > 0.15 or (abs(ny) <= 0.15 and nx < 0): nx, ny = -nx, -ny          # the fin side: up the screen (or right, for a column)
        L = layers['back' if split_y is not None and P[c][1] < split_y else 'front']
        cx, cy = P[c]; lo, hi = max(0, c - step * 2), min(n, c + step * 2 + 1)
        ext = int(hl + r + 8)
        for y in range(max(0, int(cy) - ext), min(h, int(cy) + ext)):
            for x in range(max(0, int(cx) - ext), min(w, int(cx) + ext)):
                dx, dy = x + 0.5 - cx, y + 0.5 - cy
                u, v = dx * tx + dy * ty, dx * nx + dy * ny                         # along / across (v > 0 = fin side)
                fin = r * 0.95 <= v <= r * 0.95 + max(2.5, r * 0.3) and abs(u) <= hl * 0.9 and r > 6
                if (u / hl) ** 2 + (v / r) ** 2 > 1 and not fin: continue
                d2 = ((P[lo:hi, 0] - x - 0.5) ** 2 + (P[lo:hi, 1] - y - 0.5) ** 2); j = lo + int(np.argmin(d2))
                ax, ay = x + 0.5 - P[j][0], y + 0.5 - P[j][1]; rj = radius_at(j, n, head)
                vv = ax * nx + ay * ny
                if fin and (u / hl) ** 2 + (v / r) ** 2 > 1:                       # the dorsal fin: a dark frill, a lit edge, rays
                    top = v > r * 0.95 + max(2.5, r * 0.3) - 1.2
                    L[y, x] = FIN_L if top else (NET if (j // 3) % 2 else FIN); continue
                ln = (ax * LX + ay * LY) / rj
                tone = LIT if ln > 0.38 else SHADE if ln < -0.3 else BASE
                if abs(vv) < 1.0 and rj > 6: tone = SHADE                         # the spine, under the fin
                m = mot.at(j, vv)
                if m in ('net', 'blot'): tone = NET if tone is SHADE else SHADE
                elif m == 'spot': tone = ACC if tone is not SHADE else ACC_D
                L[y, x] = tone
    return outline(layers['back']), outline(layers['front'])

# ------------------------------------------------------------------ the head, in profile, facing right; the body joins at ANCHOR
HW, HH = 80, 56
ANCHOR = (4, 26)                                     # the back of the head's middle (matches the body's thickness, 2R = 40)
def head(gape=0.7):
    a = np.zeros((HH, HW, 4), np.uint8)
    ang = math.radians(12 + 26 * gape); hx, hy = 32, 31                               # the jaw hinge: far back, a heavy jaw
    tip = (hx + 40 * math.cos(ang), hy + 40 * math.sin(ang))
    skull = [(0, 6), (12, 3), (26, 4), (42, 9), (58, 16), (70, 22), (76, 25), (77, 28), (73, 30), (58, 30), (44, 31), (hx, hy)]
    jaw = [(hx - 2, hy - 1), (hx + 8, hy + 2), tip, (tip[0] + 2, tip[1] - 3), (tip[0] + 3, tip[1] + 1), (tip[0] - 4, tip[1] + 5),
           (hx + 12, hy + 16), (16, 48), (0, 46)]
    mouth = [(hx + 1, hy), (58, 30.5), (73, 30.5), (tip[0] + 1, tip[1] - 2), (hx + 8, hy + 2)]
    def mask(poly):
        im = Image.new('L', (HW, HH)); ImageDraw.Draw(im).polygon(poly, fill=255); return np.array(im) > 0
    S, J, M = mask(skull + [(0, 30)]), mask(jaw + [(0, 30), (0, 6)]), mask(mouth)
    S &= ~M; J &= ~M & ~S
    top = {x: (np.nonzero(S[:, x] | J[:, x])[0].min() if (S[:, x] | J[:, x]).any() else 0) for x in range(HW)}
    bot = {x: (np.nonzero(S[:, x] | J[:, x])[0].max() if (S[:, x] | J[:, x]).any() else 0) for x in range(HW)}
    mot = Mottle(HW + 20, 5)
    for y in range(HH):
        for x in range(HW):
            if not (S[y, x] or J[y, x]): continue
            f = (y - top[x]) / max(1, bot[x] - top[x])
            if J[y, x] and x > 36 and y > hy + 4 and f > 0.5:            # only the jaw itself, not the throat behind it
                c = JAW_L if f < 0.8 else JAW                                       # the pale lower jaw / throat
            else:
                c = LIT if f < 0.2 else SHADE if f > 0.68 else BASE
            m = mot.at(80 - x, y - 26)
            if m in ('net', 'blot') and not (J[y, x] and f > 0.55): c = NET if c is SHADE else SHADE
            elif m == 'spot' and x < 50 and c in (LIT, BASE): c = ACC
            a[y, x] = c
    a[M] = MOUTH
    for y in range(1, HH):
        for x in range(HW):
            if M[y, x] and not M[y - 1, x]: a[y, x] = GUM                            # the gum along the roof of the mouth
            elif M[y, x] and y + 1 < HH and not M[y + 1, x]: a[y, x] = MOUTH_L
    d = Image.fromarray(a); dr = ImageDraw.Draw(d)
    for x, L in ((38, 4), (44, 6), (49, 4), (55, 7), (61, 5), (66, 4), (71, 8)):      # needle fangs down from the roof, the front one longest
        dr.line([(x, 31), (x - 1, 31 + L)], fill=TOOTH); dr.point((x, 31), fill=TOOTH_D)
    for t, L in ((0.3, 4), (0.45, 5), (0.6, 4), (0.75, 6), (0.9, 5)):                 # and up from the lower jaw
        x, y = hx + 8 + (tip[0] - hx - 8) * t, hy + 2 + (tip[1] - hy - 2) * t
        dr.line([(x, y), (x + 1, y - L)], fill=TOOTH)
    dr.line([(hx - 2, hy + 1), (14, 38)], fill=SHADE); dr.line([(hx - 3, hy + 3), (16, 40)], fill=NET)   # the jaw muscle's crease
    dr.line([(8, 30), (9, 36)], fill=NET)                                             # the gill slit
    ex, ey = 48, 14                                                                   # the blind eye: milky, no pupil
    dr.ellipse([ex - 5, ey - 4, ex + 5, ey + 4], fill=EYE_R); dr.ellipse([ex - 4, ey - 3, ex + 4, ey + 3], fill=EYE)
    for p in ((ex + 1, ey), (ex, ey + 1), (ex - 1, ey), (ex + 2, ey + 1)): dr.point(p, fill=EYE_C)
    dr.point((ex - 2, ey - 2), fill=hexc('#ffffff'))
    dr.line([(74, 24), (76, 20)], fill=SHADE); dr.point((76, 20), fill=OUT)          # the nostril tube
    return outline(np.array(d))

def put_head(layer, hd, at, face):
    """Paste the head so its back (ANCHOR) sits on the body's end `at`; face -1 = looking left (flipped, never rotated)."""
    im = Image.fromarray(hd if face == 1 else hd[:, ::-1].copy())
    ax = ANCHOR[0] if face == 1 else HW - 1 - ANCHOR[0]
    L = Image.fromarray(layer); L.alpha_composite(im, (int(at[0] - ax), int(at[1] - ANCHOR[1]))); return np.array(L)

def zap(a, P, seed, n=4):
    rnd = random.Random(seed); im = Image.fromarray(a); d = ImageDraw.Draw(im)
    for _ in range(n):
        i = rnd.randrange(0, len(P) - 60); pts = [(P[j][0] + rnd.randint(-R // 2, R // 2), P[j][1] + rnd.randint(-R // 2, R // 3)) for j in range(i, i + rnd.randint(30, 60), 6)]
        d.line(pts, fill=hexc('#c9b04a', 90), width=3); d.line(pts, fill=ACC, width=1)
        for p in pts[::2]: d.point(p, fill=hexc('#fff2a0'))
    return np.array(im)

def platform(w, h):
    """A stand-in raised platform for the flooded levels (not drawn yet): concrete slab, a face, a lit edge."""
    a = np.zeros((h + 12, w, 4), np.uint8); a[:h] = hexc('#6b6e6a'); a[:2] = hexc('#8a8d88'); a[h:] = hexc('#3e413e')
    rnd = random.Random(5)
    for _ in range(w * h // 30): a[rnd.randrange(2, h), rnd.randrange(w)] = hexc('#5c5f5b')
    return outline(a)

def shadow_of(layer):
    m = layer[:, :, 3] > 0; eye = (layer[:, :, :3] == EYE[:3]).all(2)
    o = np.zeros_like(layer); o[m] = SHADOW; o[eye] = hexc('#e9ece4', 110); return o

# ------------------------------------------------------------------ in the real basin (crop of the Depths render at 2x = game scale)
CROP = (33, 25, 58, 43)                              # tiles: the basin, its ledge and the hall's south wall
def basin(render):
    m = Image.open(render).convert('RGBA'); m = m.resize((m.width * 2, m.height * 2), Image.NEAREST)
    return m.crop(tuple(v * T for v in CROP))
def tile(x, y): return (int(round((x - CROP[0]) * T)), int(round((y - CROP[1]) * T)))

def label(im, text):
    d = ImageDraw.Draw(im); d.rectangle([0, 0, 9 + 6 * len(text), 13], fill=hexc('#1b1d1f')); d.text((4, 1), text, fill=hexc('#d0d4d8'))

if __name__ == "__main__":
    here = os.path.dirname(os.path.abspath(__file__)); root = os.path.join(here, '..', '..')
    out = os.path.join(root, 'maps', 'renders', 'boss_eel'); os.makedirs(out, exist_ok=True)
    ar = Image.open(os.path.join(root, 'Assets', 'Resources', 'Sprites', 'armor_atlas.png')).convert('RGBA')
    man = ar.crop((0, 0, 16, 16)).resize((32, 32), Image.NEAREST)  # the player at game scale (a tile)
    hd = head(0.75)
    B = basin(sys.argv[1]); W, H = B.size

    # 1. surfaced: the whole length snaking through the basin, the head at the ledge, jaws open at the player on it
    P = resample(bez(tile(44, 31.6), tile(50, 32.0), tile(55.3, 33.3), tile(54, 35.4), tile(46, 35.2), tile(38, 35.0), tile(36.2, 37.0),
                     tile(39, 38.8), tile(47, 38.6), tile(53, 38.4), tile(56.0, 38.8)))
    back, front = body(P, W, H, seed=2)
    front = put_head(front, hd, P[0], -1)
    a = B.copy(); a.alpha_composite(man, tile(42, 29.2)); a.alpha_composite(Image.fromarray(front))
    label(a, f"1:1 game scale - surfaced, the whole body: {len(P) / T:.0f} tiles long, 1.25 thick; the player on the ledge")
    a.convert('RGB').save(os.path.join(out, 'eel_basin_surfaced.png'))

    # 2. lurking: the same, only its shadow under the black water ("the water version looks fine")
    s = B.copy(); s.alpha_composite(man, tile(42, 29.2)); s.alpha_composite(Image.fromarray(shadow_of(front)))
    label(s, "1:1 - lurking: only its shadow and the blind eyes under the water")
    s.convert('RGB').save(os.path.join(out, 'eel_basin_lurking.png'))

    # 3. the strike: wrapped round a platform (a stand-in slab in the basin) 1.5 times, the head up over you, the rest trailing away
    px0, py0 = tile(44, 34); pw = 3 * T; cx, cy = px0 + pw / 2, py0 + pw / 2
    ring = [(cx + math.cos(t) * (pw / 2 + R + 2 + t * 1.2), cy + math.sin(t) * (pw / 2 + R - 4 + t * 1.0)) for t in np.linspace(-0.35, -0.35 + 2 * math.pi * 1.5, 160)]
    tail = bez(ring[-1], tile(41.5, 35.5), tile(37.5, 34.2), tile(36.2, 37.8), tile(40, 38.8), tile(48, 38.6), tile(55.5, 38.8))
    neck = bez(tile(47.2, 33.0), tile(47.6, 33.6), ring[0])
    P = resample(neck + ring + tail[1:])
    back, front = body(P, W, H, head=True, seed=3, split_y=cy)
    front = put_head(front, hd, P[0], -1); front = zap(front, P[40:400], 7, 5)
    c = B.copy(); c.alpha_composite(Image.fromarray(back)); c.alpha_composite(Image.fromarray(platform(pw, pw)), (px0, py0 - 6))
    c.alpha_composite(man, (int(cx - 16), int(cy - 26))); c.alpha_composite(Image.fromarray(front))
    label(c, f"1:1 - the strike: wrapped 1.5 times round a 3x3 platform (stand-in slab), {len(P) / T:.0f} tiles")
    c.convert('RGB').save(os.path.join(out, 'eel_basin_strike.png'))

    # 4. half out of the water (the last level isn't drawn: shown in the basin), coiled at the base, the head over the player
    base = tile(47, 37.6)
    coil = [(base[0] + math.cos(t) * (60 - t * 4), base[1] + math.sin(t) * (34 - t * 2)) for t in np.linspace(0, 2 * math.pi * 1.4, 120)]
    col = bez(tile(44.2, 32.4), tile(45.3, 33.6), tile(46.6, 35.0), tile(46.0, 36.6), tile(47.4, 38.0), base)
    tail = bez(coil[-1], tile(41, 38.6), tile(37, 38.8))
    Pc = resample(col + coil + tail[1:])
    back, front = body(Pc, W, H, seed=4)
    front = put_head(front, hd, Pc[0], -1)
    im = Image.new('RGBA', (W, H)); ImageDraw.Draw(im).ellipse([base[0] - 80, base[1] - 22, base[0] + 80, base[1] + 30], fill=SHADOW)
    r_ = B.copy(); r_.alpha_composite(im); r_.alpha_composite(man, tile(41.5, 34.8)); r_.alpha_composite(Image.fromarray(front))
    label(r_, "1:1 - half out of the water, about to strike (the last level, shown in this basin); the player in front")
    r_.convert('RGB').save(os.path.join(out, 'eel_basin_rear.png'))

    # close-up: the head and a stretch of body at 4x, for the detail (not game scale)
    seg = resample(bez((84, 40), (150, 30), (210, 50), (276, 36)))
    _, f2 = body(seg, 280, 90, head=True, seed=6); f2 = put_head(f2, hd, seg[0], -1)
    cu = Image.new('RGBA', (280, 90), hexc('#1b1d1f')); cu.alpha_composite(Image.fromarray(f2))
    cu = cu.resize((280 * 4, 90 * 4), Image.NEAREST); label(cu, "4x close-up (detail only, not game scale)")
    cu.convert('RGB').save(os.path.join(out, 'eel_closeup_4x.png'))
    print("written", out)
