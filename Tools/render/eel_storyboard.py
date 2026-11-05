"""STORYBOARD of the eel's new fight style (Yan, 2026-11-05: "the whole body out of the water doesn't work during the bossfight itself ... several deep pools where it
can come up from ... the head attacks one player while the tail comes out of another pool ... it comes fully out of a pool and goes down to another in a snake-like
motion ... IMPACT, heavy and strong like the hydra in God of War"). Scripted, slow, no physics wiggle: the body is a 35-tile spine laid out by hand-made curves in 3D
(x, y, height); only the part above the water is painted (Yan's drawing bent along it by the real painter), the rest is under the surface. Three attacks on the current Sump
render with three extra pools painted in (four pools: the original big one plus west, east and north):
  1  HEAD STRIKE (west pool) + TAIL SWEEP (north pool) at the same time
  2  ARCH SLAM: it rises out of the east pool, arcs over the arena and crashes into the big pool
  3  SNAKE DASH: fully out of the west pool, across the arena in an S, down into the east pool
Weight comes from: a long readable wind-up, a strike that ACCELERATES into the hit (full speed at the moment of impact), a few frames of hit-stop, screen shake and a
zoom punch, shockwave rings, the game's own splash/ripple/thunderclap strips, debris, dust, cracks that stay, afterimages on the fast parts, shadows on the ground, and bodies flung.
    python3 Tools/render/eel_storyboard.py maps/renders/boss_eel/eel_storyboard.mp4 [--probe 2.0,3.2,...]   (probe: a contact sheet of those moments instead)
This is a storyboard: no game code, no sound, placeholder timings and damage; the pools are painted over the render, not in the map."""
import sys, os, math, random, tempfile
import numpy as np
from PIL import Image, ImageDraw
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE); sys.path.insert(0, os.path.join(HERE, '..', '..', 'ArtSource', 'bosses'))
from eel_common import *
import eel_poses as E, make_eel as M

random.seed(7); np.random.seed(7)
FPS, VW, VH, PAD, NS = 30, 1280, 720, 24, 1120            # 32 spine points a tile: 35 tiles of eel
OX, OY = int(30.5 * T - VW / 2) - PAD, int(20.0 * T - VH / 2) - PAD
WW, WH = VW + 2 * PAD, VH + 2 * PAD
def sp(x, y, z=0.0): return (x * T - OX, (y - z) * T - OY)       # a point in the world (tiles, height) -> pixels on the padded canvas

# ------------------------------------------------------------------ easing
def c01(u): return max(0.0, min(1.0, u))
def lerp(a, b, u): return a + (b - a) * u
def smr(u): u = c01(u); return u * u * u * (u * (6 * u - 15) + 10)       # slow start, slow end: heavy
def ein(u, p=3.0): return c01(u) ** p                                   # accelerates the whole way: it hits at full speed
def eout(u, p=3.0): return 1 - (1 - c01(u)) ** p                        # fast then settling: momentum

# ------------------------------------------------------------------ the world: the real Sump render with extra pools painted in
S3full = render('sub_station_depths_3'); WORLD = S3full.crop((OX, OY, OX + WW, OY + WH)).convert('RGBA')
POOLS = {'W': (14.5, 17.0, 3.7, 2.4), 'E': (46.5, 17.0, 3.7, 2.4), 'N': (30.5, 14.0, 3.5, 2.3)}; C = (30.5, 24.5)
tex = S3full.crop((26 * T, 23 * T, 35 * T, 27 * T)).convert('RGBA')                         # the real pool's dark water, tiled into the new pools
big = Image.new('RGBA', (WW + 288, WH + 128))
for ty in range(0, big.height, tex.height):
    for tx in range(0, big.width, tex.width): big.paste(tex, (tx, ty))
for name, (cx, cy, ra, rb) in POOLS.items():
    m = Image.new('L', (WW, WH), 0); d = ImageDraw.Draw(m)
    for ty in range(int(cy - rb - 2), int(cy + rb + 3)):
        for tx in range(int(cx - ra - 2), int(cx + ra + 3)):
            if ((tx + 0.5 - cx) / ra) ** 2 + ((ty + 0.5 - cy) / rb) ** 2 <= 1 + 0.2 * math.sin(tx * 1.7 + ty * 2.3 + len(name)):
                x0, y0 = tx * T - OX, ty * T - OY; d.rectangle((x0, y0, x0 + T - 1, y0 + T - 1), fill=255)
    WORLD.paste(big.crop((0, 0, WW, WH)), (0, 0), m)

def frames_of(name):
    im = Image.open(os.path.join(FXD, f'px_{name}.png')).convert('RGBA'); h = im.height
    return [im.crop((i * h, 0, i * h + h, h)) for i in range(im.width // h)]
SPLASH, RIPPLES, THUNDER, SMOKE, WHIRL = (frames_of(n) for n in ('splash', 'ripples', 'thunderclap', 'smoke_bomb', 'whirlpool'))
MAN = player(); FNT = font(15)
def stamp(c, fr, wx, wy, scale=1.0, alpha=1.0, up=0.0):
    im = fr.resize((int(fr.width * scale), int(fr.height * scale)), Image.NEAREST)
    if alpha < 1: im = im.copy(); im.putalpha(im.getchannel('A').point(lambda a: int(a * alpha)))
    x, y = sp(wx, wy); c.alpha_composite(im, (int(x - im.width / 2), int(y - im.height / 2 - up)))

# ------------------------------------------------------------------ curves (tiles; x, y, height): the body follows the head along them
class Curve:
    def __init__(s, pts):
        s.p = np.asarray(pts, float); d = np.linalg.norm(np.diff(s.p, axis=0), axis=1); keep = np.concatenate([[True], d > 1e-6]); s.p = s.p[keep]
        d = np.linalg.norm(np.diff(s.p, axis=0), axis=1); s.c = np.concatenate([[0], np.cumsum(d)]); s.L = s.c[-1]
        s.d0 = (s.p[1] - s.p[0]) / np.linalg.norm(s.p[1] - s.p[0]); s.d1 = (s.p[-1] - s.p[-2]) / np.linalg.norm(s.p[-1] - s.p[-2])
    def at(s, sig):                                   # before the start and past the end it carries straight on (down into the pool)
        sig = np.atleast_1d(np.asarray(sig, float)); out = np.stack([np.interp(sig, s.c, s.p[:, k]) for k in range(3)], -1)
        lo, hi = sig < 0, sig > s.L; out[lo] = s.p[0] + s.d0 * sig[lo, None]; out[hi] = s.p[-1] + s.d1 * (sig[hi, None] - s.L); return out
def bez(p0, p1, p2, p3, n=700):
    t = np.linspace(0, 1, n)[:, None]; p0, p1, p2, p3 = (np.array(v, float) for v in (p0, p1, p2, p3)); return (1 - t) ** 3 * p0 + 3 * (1 - t) ** 2 * t * p1 + 3 * (1 - t) * t ** 2 * p2 + t ** 3 * p3

T1 = (20.5, 19.8)                                                                           # attack 1: where the head comes down
H1 = Curve(bez((14.5, 17.0, -3.2), (14.5, 17.0, 6.5), (19.0, 18.6, 8.6), (T1[0], T1[1], 0.45)))
H2 = Curve(bez((46.5, 17.0, -3.2), (47.8, 16.4, 10.0), (40.0, 19.0, 12.0), (33.2, 23.4, -3.5)))   # attack 2: the arch, east pool to the big pool
xs = np.linspace(20.0, 43.0, 900); ys = 17 + 2.3 * np.sin((xs - 20.0) / 14.0 * 2 * math.pi); zs = 0.45 + 0.2 * np.sin((xs - 20.0) / 7.0 * 2 * math.pi)
H3 = Curve(np.vstack([bez((14.5, 17.0, -4.0), (15.0, 17.0, 3.6), (17.5, 17.0, 3.6), (20.0, 17.0, 0.45)), np.stack([xs, ys, zs], -1)[1:],     # attack 3: out of the west pool (rearing), the S across, down into the east pool
                      bez((43.0, ys[-1], zs[-1]), (45.0, 17.0, 0.45), (46.0, 17.0, -0.5), (47.0, 17.0, -4.0))[1:]]))
print('curve lengths (tiles):', round(H1.L, 1), round(H2.L, 1), round(H3.L, 1))

# ------------------------------------------------------------------ the scripts: where the head (or the tail) is at every moment
def sig1(t):   # head strike: emerge slowly, rear, creep, STRIKE (accelerating), press, withdraw slowly
    if t < 0.9: return 0.0
    if t < 2.5: return 8.8 * smr((t - 0.9) / 1.6)
    if t < 3.0: return 8.8 + 0.5 * (t - 2.5) / 0.5
    if t < 3.2: return lerp(9.3, H1.L, ein((t - 3.0) / 0.2, 2.6))
    if t < 3.9: return H1.L
    if t < 5.0: return H1.L * (1 - smr((t - 3.9) / 1.1))
    return 0.0
def sig2(t):   # arch slam: rise, rear at the top, then crash down into the pool; the whole body slides after the head on its momentum
    if t < 5.4: return 0.0
    if t < 6.9: return 7.5 * smr((t - 5.4) / 1.5)
    if t < 7.4: return 7.5 + 1.0 * (t - 6.9) / 0.5
    if t < 7.85: return lerp(8.5, H2.L + 1.5, ein((t - 7.4) / 0.45, 2.4))
    if t < 9.9: return lerp(H2.L + 1.5, H2.L + 37.0, eout((t - 7.85) / 2.05, 2.2))
    return H2.L + 37.0
def sig3(t):   # snake dash: out of the pool, rear up, hold, then the whole length goes across
    if t < 10.0: return 0.0
    if t < 11.5: return 6.0 * smr((t - 10.0) / 1.5)
    if t < 11.95: return 6.0 + 0.8 * (t - 11.5) / 0.45
    if t < 14.5: return lerp(6.8, H3.L + 37.0, smr((t - 11.95) / 2.55))
    return H3.L + 37.0
# the tail sweep (north pool): a column rises, trembles, topples over into a low arm and swings round the pool, ending in a slam; then it withdraws
NX, NY, NR = POOLS['N'][0], POOLS['N'][1], 8.4; TH0, TURN = -0.2, 1.15 * 2 * math.pi
def tail(t):   # -> (extension u, topple k, angle of the tip, tip height)
    if t < 1.3: return 0.0, 0.0, TH0, 0.4
    if t < 2.5: return smr((t - 1.3) / 1.2), 0.0, TH0, 0.4
    if t < 2.9: return 1.0, 0.0, TH0, 0.4
    if t < 3.2: return 1.0, ein((t - 2.9) / 0.3, 2.2), TH0, 0.4
    if t < 4.3: tau = (t - 3.2) / 1.1; return 1.0, 1.0, TH0 + TURN * tau ** 1.7, lerp(0.5, 0.12, c01((tau - 0.8) / 0.2))
    if t < 5.2: return 1.0 - smr((t - 4.3) / 0.9), 1.0, TH0 + TURN, 0.12
    return 0.0, 1.0, TH0 + TURN, 0.12
def arm(u, k, th, tz, om):
    r = np.linspace(0, 1, 260); th_r = th - 0.1 * om * (1 - r) ** 1.4; rho = NR * u * r * k
    z = (1 - k) * 7.4 * u * r + k * (2.4 * u * np.sin(math.pi * r) + tz * r ** 2) - 0.5 * (1 - r) ** 2
    return np.stack([NX + rho * np.cos(th_r), NY + rho * np.sin(th_r), z], -1)
def t_P2(): # when the tail's tip passes the second player's bearing
    for i in range(0, 4000):
        t = 3.2 + i * 0.00028
        if tail(t)[2] >= 2.05: return t
    return 3.9

def spine(t):
    """-> S (NS x 3, tiles, head first), face, which attack"""
    i = np.arange(NS); att = 1 if t < 5.3 else 2 if t < 9.95 else 3
    cur, sg, face = {1: (H1, sig1(t), 1), 2: (H2, sig2(t), -1), 3: (H3, sig3(t), 1)}[att]
    S = cur.at(sg - i / 32.0)
    if att == 1:                                           # the tail comes up out of the other pool; the middle of the body is under the water between them
        u, k, th, tz = tail(t); om = (tail(t + 0.02)[2] - tail(t - 0.02)[2]) / 0.04
        a = arm(u, k, th, tz, om); seg = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(a, axis=0), axis=1))]); tot = seg[-1]
        j = (NS - 1 - i) / 32.0; A = np.stack([np.interp(tot - j, seg, a[:, q]) for q in range(3)], -1)
        beyond = j > tot; A[beyond] = np.array([NX, NY, a[0, 2]]) + np.array([0, 0, -1.0]) * (j[beyond, None] - tot)
        S = np.where((i < NS // 2)[:, None], S, A)
    return S, face, att

# ------------------------------------------------------------------ painting: only what is above the water, Yan's pixels bent along it
ORDER = E.body_order(NS)
def pieces(S, face):
    vis = S[:, 2] >= 0.0; P = np.stack([S[:, 0] * T - OX, (S[:, 1] - S[:, 2]) * T - OY], -1); out = []; i = 0
    while i < NS:
        if not vis[i]: i += 1; continue
        j = i
        while j < NS and vis[j]: j += 1
        if j - i >= 10: out.append((i, j))
        i = j
    layers = []
    for a, b in out:
        pts = P[a:b].copy()
        if a == 0:                                          # the head piece: the first stretch leaves the head level (it is a profile), then bends into the neck
            back = 1 if face < 0 else -1
            for k in range(min(E.LEAD, len(pts))): u = c01(k / E.LEAD); u = u * u * (3 - 2 * u); pts[k] = (pts[0][0] + back * k * (1 - u) + (pts[k][0] - pts[0][0]) * u, pts[0][1] + (pts[k][1] - pts[0][1]) * u)
        order = tuple(np.array(o[a:b]) for o in ORDER); layer = np.zeros((WH, WW, 4), np.uint8)
        try: E.paint(layer, pts, order, bulge=((0, (b - a) * 0.8, 0.85) if b == NS else None))      # the tail arm: thickened toward the pool (it is a massive thing, not a whip of string)
        except Exception as e: continue
        if a == 0: E.put_head(layer, pts[0], face)
        layers.append((a, E.outline_layer(layer)))
    return layers, P, vis

# ------------------------------------------------------------------ the fight's moments: telegraphs, impacts, bodies thrown
TP2 = t_P2()
def a2_cross():  # the moment the head goes into the big pool
    for i in range(0, 6000):
        t = 7.4 + i * 0.0002; z = H2.at(sig2(t) - 0.0)[0, 2]
        if sig2(t) > 14 and z <= 0.0: return t
    return 7.8
TC = a2_cross(); EC = H2.at(sig2(TC))[0]
def a3_hit(xh):  # when the head passes x = xh
    for i in range(0, 6000):
        t = 11.95 + i * 0.0004
        if H3.at(sig3(t))[0, 0] >= xh: return t
    return 13.0
P1X = 31.0; TH1 = a3_hit(P1X); P1Y = float(np.interp(P1X, xs, ys))
TH2 = a3_hit(38.5)
def kf(points):
    def f(t):
        if t <= points[0][0]: return points[0][1:]
        for a, b in zip(points, points[1:]):
            if t <= b[0]: u = (t - a[0]) / (b[0] - a[0]); return tuple(lerp(p, q, u) for p, q in zip(a[1:], b[1:]))
        return points[-1][1:]
    return f
# (t, x, y, z, rotation, flash)
PA = kf([(0, 20.5, 21.0, 0, 0, 0), (3.1, 20.5, 19.8, 0, 0, 0), (3.2, 20.5, 19.8, 0, 0, 0), (3.28, 22.5, 20.5, 1.6, 140, 1), (3.55, 25.2, 22.6, 0.0, 320, 1), (3.62, 25.6, 23.0, 0, 340, 0.3), (4.5, 25.6, 23.0, 0, 350, 0),
         (4.9, 25.6, 23.0, 0, 360, 0), (5.4, 31.5, 26.0, 0, 360, 0), (7.45, 33.0, 25.8, 0, 360, 0), (7.52, 33.0, 25.8, 0, 360, 0), (TC + 0.0, 33.0, 25.8, 0, 360, 0),
         (TC + 0.08, 30.0, 27.0, 2.2, 520, 1), (TC + 0.4, 25.5, 29.0, 0, 700, 1), (TC + 1.2, 25.5, 29.0, 0, 710, 0), (TC + 1.7, 25.5, 29.0, 0, 720, 0), (10.2, 27.5, 19.5, 0, 720, 0), (TH1 - 0.5, P1X, P1Y + 0.2, 0, 720, 0),
         (TH1, P1X, P1Y, 0, 720, 0), (TH1 + 0.08, P1X + 1.0, P1Y - 2.6, 2.4, 900, 1), (TH1 + 0.45, P1X + 2.0, P1Y - 6.5, 0, 1080, 1), (TH1 + 1.3, P1X + 2.0, P1Y - 6.5, 0, 1080, 0), (99, P1X + 2.0, P1Y - 6.5, 0, 1080, 0)])
P2a = tail(TP2)
PB = kf([(0, 27.6, 18.4, 0, 0, 0), (TP2 - 0.75, 27.6, 18.4, 0, 0, 0), (TP2 - 0.45, 27.6, 18.4, 0.0, 0, 0), (TP2 - 0.32, 26.4, 19.6, 1.3, 180, 0), (TP2 + 0.05, 25.2, 21.4, 0.0, 360, 0), (TP2 + 0.35, 25.2, 21.4, 0, 360, 0),
         (5.4, 38.0, 20.5, 0, 360, 0), (6.3, 40.0, 20.6, 0, 360, 0), (7.15, 40.0, 20.6, 0, 360, 0), (7.6, 41.8, 25.6, 0, 360, 0), (9.0, 41.8, 25.6, 0, 360, 0), (10.1, 33.5, 20.0, 0, 360, 0), (TH2 - 0.55, 38.2, float(np.interp(38.2, xs, ys)) + 0.8, 0, 360, 0),
         (TH2 - 0.3, 38.4, float(np.interp(38.4, xs, ys)) + 1.0, 1.5, 720, 0), (TH2 + 0.1, 38.6, float(np.interp(38.6, xs, ys)) - 2.6, 0, 1080, 0), (99, 38.6, float(np.interp(38.6, xs, ys)) - 2.6, 0, 1080, 0)])
HITS = {'A': [(3.2, 'head strike'), (TC, 'shock'), (TH1, 'body')]}                       # the bodies that get hit and when (for the damage numbers)
POPUPS = [(3.28, 'A', 'head strike -260'), (TC + 0.08, 'A', 'shockwave -190'), (TH1 + 0.08, 'A', 'the body -310')]

# ------------------------------------------------------------------ effects
rings, parts, cracks, splashes, whirls, pops, scorch = [], [], [], [], [], [], []
shake_amp = 0.0; flash = 0.0; zoom = 0.0; frozen = 0
def crack_set(x, y, n, length):
    for k in range(n):
        a = random.uniform(0, 2 * math.pi); pts = [(x, y)]; r = 0.0
        for s in range(random.randint(4, 7)):
            r += random.uniform(0.5, length / 5); a += random.uniform(-0.45, 0.45); pts.append((x + math.cos(a) * r, y + math.sin(a) * r * 0.9))
        cracks.append(dict(pts=pts))
def debris(x, y, n, spd, kind='rock'):
    for _ in range(n):
        a = random.uniform(0, 2 * math.pi); v = random.uniform(0.4, 1.0) * spd
        parts.append(dict(kind=kind, x=x, y=y, z=0.3, vx=math.cos(a) * v, vy=math.sin(a) * v * 0.8, vz=random.uniform(3.0, 9.0) * (spd / 8), age=0.0, life=random.uniform(0.7, 1.4), size=random.choice((2, 3, 3, 4, 5))))
def dust(x, y, n, spd):
    for _ in range(n):
        a = random.uniform(0, 2 * math.pi); v = random.uniform(0.2, 1.0) * spd
        parts.append(dict(kind='dust', x=x, y=y, z=0.2, vx=math.cos(a) * v, vy=math.sin(a) * v * 0.7, vz=random.uniform(0.2, 1.4), age=0.0, life=random.uniform(0.6, 1.3), size=random.randint(8, 18)))
def impact(t, x, y, power, water=False, stop=4, crackn=7):
    """power ~1 = a slam, 2 = the arch crash. Rings, splash, debris/droplets, dust, cracks (on ground), shake, flash, zoom punch, hit-stop"""
    global shake_amp, flash, zoom, frozen
    rings.append(dict(x=x, y=y, t0=t, r1=5.5 * power, life=0.55, w=7)); rings.append(dict(x=x, y=y, t0=t + 0.07, r1=8.5 * power, life=0.8, w=4))
    if water: splashes.append(dict(fr=SPLASH, x=x, y=y, s=2.0 * power, t0=t)); splashes.append(dict(fr=SPLASH, x=x - 0.5 * power, y=y + 0.2, s=1.3 * power, t0=t + 0.12))
    else: splashes.append(dict(fr=THUNDER, x=x, y=y, s=1.1 * power, t0=t)); crack_set(x, y, crackn, 5.5 * power); scorch.append((x, y, 1.5 * power))
    debris(x, y, int(26 * power), 8 * power, 'drop' if water else 'rock'); dust(x, y, int(10 * power), 3.5 * power)
    shake_amp = max(shake_amp, 11 * power); flash = max(flash, 0.55 * min(power, 1.4)); zoom = max(zoom, 0.045 * power); frozen = max(frozen, stop)

def draw_particles(c, simt, dt):
    ov = Image.new('RGBA', c.size, (0, 0, 0, 0)); d = ImageDraw.Draw(ov)
    for p in list(parts):
        p['age'] += dt
        if p['age'] > p['life']: parts.remove(p); continue
        p['vz'] -= (14.0 if p['kind'] != 'dust' else -0.4) * dt; p['x'] += p['vx'] * dt; p['y'] += p['vy'] * dt; p['z'] += p['vz'] * dt
        if p['z'] < 0 and p['kind'] in ('rock', 'drop'): p['z'] = 0; p['vz'] *= -0.35; p['vx'] *= 0.6; p['vy'] *= 0.6
        x, y = sp(p['x'], p['y'], max(p['z'], 0)); a = 1 - p['age'] / p['life']
        if p['kind'] == 'dust': r = p['size'] * (1 + p['age'] * 1.6); d.ellipse((x - r, y - r * 0.7, x + r, y + r * 0.7), fill=(150, 145, 130, int(110 * a)))
        elif p['kind'] == 'drop': s = p['size']; d.rectangle((x, y, x + s, y + s), fill=(200, 225, 235, int(230 * min(1, a * 1.6))))
        else: s = p['size']; d.rectangle((x, y, x + s, y + s), fill=(95, 90, 82, 255)); d.rectangle((x, y, x + s - 1, y + s - 1), fill=(130, 124, 112, 255))
    c.alpha_composite(ov)

# ------------------------------------------------------------------ the run
CAPS = [(0.0, 0.9, 'STORYBOARD - the new fight style: four deep pools, scripted attacks', 'the eel is under the water: only what is out is drawn'),
        (0.9, 3.0, '1  HEAD STRIKE (west pool) + TAIL SWEEP (north pool)', 'WIND-UP: slow and readable'), (3.0, 3.35, '1  HEAD STRIKE (west pool) + TAIL SWEEP (north pool)', 'STRIKE + TOPPLE'),
        (3.35, 4.4, '1  HEAD STRIKE (west pool) + TAIL SWEEP (north pool)', 'IMPACT - the tail swings round the pool'), (4.4, 5.3, '1  HEAD STRIKE (west pool) + TAIL SWEEP (north pool)', 'it drags itself back down'),
        (5.3, 7.4, '2  ARCH SLAM (east pool -> the big pool)', 'WIND-UP: it rises over the arena, the shadow marks the landing'), (7.4, 7.9, '2  ARCH SLAM (east pool -> the big pool)', 'SLAM'),
        (7.9, 9.9, '2  ARCH SLAM (east pool -> the big pool)', 'IMPACT - the whole body pours in after the head'), (9.9, 11.95, '3  SNAKE DASH (west pool -> east pool)', 'WIND-UP: fully out, rearing'),
        (11.95, 14.6, '3  SNAKE DASH (west pool -> east pool)', 'IT GOES - anything on the path is flung'), (14.6, 99, '', '')]
def caption(t):
    for a, b, ttl, ph in CAPS:
        if a <= t < b: return ttl, ph
    return '', ''
def red(c, f, alpha):
    ov = Image.new('RGBA', c.size, (0, 0, 0, 0)); f(ImageDraw.Draw(ov), (255, 50, 35, int(alpha))); c.alpha_composite(ov)
def telegraph(c, t, k):
    pulse = 0.65 + 0.35 * math.sin(k * 0.5)
    if 1.0 < t < 3.2: red(c, lambda d, col: (d.ellipse((*(np.array(sp(*T1)) - 62), *(np.array(sp(*T1)) + 62)), fill=col[:3] + (int(70 * pulse),), outline=col, width=3)), 255)
    if 1.4 < t < 3.3:
        x, y = sp(NX, NY); red(c, lambda d, col: d.ellipse((x - NR * T, y - NR * T, x + NR * T, y + NR * T), outline=col, width=3), 200 * pulse)
    if 5.6 < t < 7.85:
        pts = [sp(*p[:2]) for p in H2.at(np.linspace(H2.L * 0.35, H2.L, 40))]; red(c, lambda d, col: d.line(pts, fill=col[:3] + (int(80 * pulse),), width=96), 255)
        x, y = sp(*EC[:2]); red(c, lambda d, col: d.ellipse((x - 150, y - 150, x + 150, y + 150), outline=col, width=3), 230 * pulse)
    if 10.3 < t < 11.95:
        pts = [sp(x_, y_) for x_, y_ in zip(xs[::20], ys[::20])]; red(c, lambda d, col: d.line(pts, fill=col[:3] + (int(70 * pulse),), width=128), 255)
def shadow(P3):
    acc = np.zeros((WH, WW), np.float32); kx, ky = np.meshgrid(np.arange(-24, 25), np.arange(-14, 15)); ker = np.exp(-((kx / 15.0) ** 2 + (ky / 8.5) ** 2))
    for q in range(0, NS, 7):
        x, y, z = P3[q]
        if z < 0.5: continue
        gx, gy = sp(x, y); gx, gy = int(gx), int(gy)
        if 24 <= gx < WW - 24 and 14 <= gy < WH - 14: acc[gy - 14:gy + 15, gx - 24:gx + 25] += ker * 0.16 * min(1.0, z / 2.5)
    a = (np.clip(acc, 0, 0.62) * 255).astype(np.uint8); lay = np.zeros((WH, WW, 4), np.uint8); lay[..., 3] = a; return Image.fromarray(lay)

def player_sprite(c, pos, front):
    x, y, z, rot, fl = pos; im = MAN.rotate(-rot, expand=True, resample=Image.NEAREST) if rot % 360 else MAN
    if fl > 0.05: im = im.copy(); px = im.load(); [px.__setitem__((a, b), (255, int(px[a, b][1] * (1 - fl) + 255 * fl * 0.3), int(px[a, b][2] * (1 - fl)), px[a, b][3])) for a in range(im.width) for b in range(im.height) if px[a, b][3] > 0]
    sx, sy = sp(x, y, z); c.alpha_composite(im, (int(sx - im.width / 2), int(sy - im.height / 2 - 6)))

def run(out, probe=None):
    global shake_amp, flash, zoom, frozen
    tmp = tempfile.mkdtemp(); simt = 0.0; rt = 0; dtf = 1.0 / FPS; prev = [None, None]; outn = 0
    events = sorted([(3.2, lambda: impact(3.2, T1[0], T1[1], 1.0)), (4.28, lambda: impact(4.28, NX + NR * math.cos(TH0 + TURN) * 0.95, NY + NR * math.sin(TH0 + TURN) * 0.95, 0.75, stop=3, crackn=5)),
                    (TC, lambda: impact(TC, EC[0], EC[1], 2.0, water=True, stop=5)), (TH1, lambda: (dust(P1X, P1Y, 14, 5), rings.append(dict(x=P1X, y=P1Y, t0=TH1, r1=4.0, life=0.4, w=6)), globals().__setitem__('shake_amp', max(shake_amp, 13)), globals().__setitem__('frozen', max(frozen, 3)), globals().__setitem__('flash', 0.4))),
                    (3.62, lambda: globals().__setitem__('shake_amp', max(shake_amp, 6)))], key=lambda e: e[0])
    probes = sorted(probe) if probe else None; sheet = []; maxt = 15.2
    while simt < maxt:
        if frozen > 0: frozen -= 1; step = 0.0
        else: step = dtf
        for e in list(events):
            if simt < e[0] <= simt + step: e[1](); events.remove(e)
        simt += step; t = simt
        S, face, att = spine(t); layers, P, vis = pieces(S, face)
        c = WORLD.copy(); telegraph(c, t, rt)
        for (cx, cy), nm in ((C, 'C'),) + tuple(((v[0], v[1]), k) for k, v in POOLS.items()):   # the pool that is about to be used swirls
            on = (nm == 'W' and 0.9 < t < 3.4) or (nm == 'N' and 1.3 < t < 3.4) or (nm == 'E' and (5.4 < t < 7.9 or 11.9 < t < 14.8)) or (nm == 'C' and 6.5 < t < 7.9) or (nm == 'W' and 10.0 < t < 12.0)
            if on: stamp(c, WHIRL[int(t * 15) % len(WHIRL)], cx, cy, 2.6 if nm == 'C' else 1.9, 0.55)
        for r_ in list(rings):                                                                  # shockwave rings, flat on the ground
            age = t - r_['t0']
            if age > r_['life']: rings.remove(r_); continue
            if age < 0: continue
            k = age / r_['life']; rad = r_['r1'] * T * (1 - (1 - k) ** 2.2); x, y = sp(r_['x'], r_['y']); a = int(235 * (1 - k)); ov = Image.new('RGBA', c.size, (0, 0, 0, 0)); d = ImageDraw.Draw(ov)
            d.ellipse((x - rad, y - rad * 0.8, x + rad, y + rad * 0.8), outline=(235, 240, 240, a), width=max(2, int(r_['w'] * (1 - k)) + 1)); d.ellipse((x - rad * 0.92, y - rad * 0.74, x + rad * 0.92, y + rad * 0.74), outline=(120, 160, 175, a // 2), width=3); c.alpha_composite(ov)
        ov = Image.new('RGBA', c.size, (0, 0, 0, 0)); d = ImageDraw.Draw(ov)                      # cracks that stay
        for (qx, qy, qr) in scorch:
            gx, gy = sp(qx, qy)
            for kk, al in ((1.0, 40), (0.7, 55), (0.4, 70)): d.ellipse((gx - qr * T * kk, gy - qr * T * kk * 0.8, gx + qr * T * kk, gy + qr * T * kk * 0.8), fill=(18, 20, 18, al))
        for cr in cracks: d.line([sp(*q) for q in cr['pts']], fill=(22, 24, 22, 215), width=3)
        c.alpha_composite(ov); c.alpha_composite(shadow(S))
        pl = [PA(t), PB(t)]
        for q in pl:
            if q[2] <= 0.3: player_sprite(c, q, False)
        if layers:                                                                              # afterimages on the fast parts
            cur = np.zeros((WH, WW, 4), np.uint8)
            for a, ly in layers: m = ly[..., 3] > 0; cur[m] = ly[m]
            speed = abs(sig1(t + 0.03) - sig1(t - 0.03)) + abs(sig2(t + 0.03) - sig2(t - 0.03)) + abs(sig3(t + 0.03) - sig3(t - 0.03)) if step else 0.0
            if speed / 0.06 > 18 and prev[0] is not None:
                for g, alpha in ((prev[0], 0.16), (prev[1], 0.3)):
                    if g is not None:
                        gi = Image.fromarray(g); gi.putalpha(gi.getchannel('A').point(lambda v, al=alpha: int(v * al))); c.alpha_composite(gi)
            if step: prev = [prev[1], cur]
            for a, ly in sorted(layers, key=lambda z: -z[0]): c.alpha_composite(Image.fromarray(ly))
        for i in range(NS - 1):                                                                 # foam and ripples where the body goes through the surface
            if (S[i, 2] >= 0) != (S[i + 1, 2] >= 0) and i % 1 == 0:
                stamp(c, RIPPLES[int(t * 15) % len(RIPPLES)], S[i, 0], S[i, 1], 0.9, 0.85)
        for s_ in list(splashes):
            idx = int((t - s_['t0']) * 15)
            if idx < 0: continue
            if idx >= len(s_['fr']): splashes.remove(s_); continue
            stamp(c, s_['fr'][idx], s_['x'], s_['y'], s_['s'], 1.0, up=20 * s_['s'])
        if probe is None or True:
            if att == 3 and 12.0 < t < 14.6 and step:                                          # the belly drags: dust all along the body that is on the ground
                for q in range(0, NS, 90):
                    if 0 <= S[q, 2] < 0.9 and random.random() < 0.5: dust(S[q, 0], S[q, 1] + 0.3, 1, 2.0)
        draw_particles(c, t, step)
        if att == 1 and 2.7 < t < 4.6 and S[NS - 1, 2] > -0.4:
            tx_, ty_ = sp(S[NS - 1, 0], S[NS - 1, 1], S[NS - 1, 2] + 1.2); hp = c01(1 - (t - 3.0) / 1.3); ovb = ImageDraw.Draw(c, 'RGBA')
            ovb.rectangle((tx_ - 46, ty_ - 22, tx_ + 46, ty_ - 8), fill=(0, 0, 0, 190)); ovb.rectangle((tx_ - 44, ty_ - 20, tx_ - 44 + 88 * hp, ty_ - 10), fill=(205, 80, 60, 255)); ovb.text((tx_ - 44, ty_ - 38), 'TAIL - shoot it down' if hp > 0.02 else 'TAIL DOWN', font=FNT, fill=(255, 235, 200, 255))
        for q in pl:
            if q[2] > 0.3: player_sprite(c, q, True)
        for (tp, who, txt) in POPUPS:
            if 0 <= t - tp < 0.9:
                q = pl[0]; x, y = sp(q[0], q[1], 2.0 + (t - tp) * 2.2); ov = Image.new('RGBA', c.size, (0, 0, 0, 0)); ImageDraw.Draw(ov).text((x - 55, y), txt, font=FNT, fill=(255, 120, 100, int(255 * (1 - (t - tp) / 0.9)))); c.alpha_composite(ov)
        if flash > 0.02: c.alpha_composite(Image.new('RGBA', c.size, (255, 255, 255, int(255 * min(flash, 0.7)))))
        sx_ = sy_ = 0
        if shake_amp > 0.3: sx_ = int(random.uniform(-1, 1) * shake_amp); sy_ = int(random.uniform(-1, 1) * shake_amp)
        z_ = 1.0 + zoom; w_, h_ = int(VW / z_), int(VH / z_); cx_, cy_ = PAD + sx_ + VW // 2, PAD + sy_ + VH // 2
        cropped = c.crop((cx_ - w_ // 2, cy_ - h_ // 2, cx_ - w_ // 2 + w_, cy_ - h_ // 2 + h_)); fr = cropped.resize((VW, VH), Image.NEAREST) if z_ > 1.001 else cropped
        ttl, ph = caption(t)
        if ttl: banner(fr, ttl, 12, size=24)
        if ph: banner(fr, ph, 50, size=22, fill=(255, 140, 110) if 'WIND' in ph else (255, 235, 150) if any(k in ph for k in ('STRIKE', 'SLAM', 'IMPACT', 'GOES')) else (225, 225, 215))
        banner(fr, 'storyboard: scripted keyframes on the current Sump + 3 painted pools; no game code, no sound, placeholder numbers', VH - 30, size=14, fill=(210, 205, 190), bg=(0, 0, 0, 150))
        shake_amp *= 0.82; flash *= 0.6; zoom *= 0.82; rt += 1
        if probe:
            while probes and simt >= probes[0]: sheet.append((probes.pop(0), fr.copy()))
            if not probes: break
        else: fr.convert('RGB').save(os.path.join(tmp, f'{outn:05d}.png')); outn += 1
    if probe:
        w = 640; h = 360; cols = 3; rows = (len(sheet) + cols - 1) // cols; sh = Image.new('RGB', (cols * w, rows * h))
        for k, (pt, im) in enumerate(sheet): sh.paste(im.resize((w, h)).convert('RGB'), ((k % cols) * w, (k // cols) * h))
        sh.save(out); print(out, len(sheet), 'moments')
    else: video(tmp, out, FPS); print(out, outn, 'frames', round(outn / FPS, 1), 's')

if __name__ == '__main__':
    pr = None
    if '--probe' in sys.argv: pr = [float(v) for v in sys.argv[sys.argv.index('--probe') + 1].split(',')]
    run(sys.argv[1], pr)
