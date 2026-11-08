"""STORYBOARD 2: the eel's PHASE 2 moves (Yan, 2026-11-05). Same style, same speed (0.8x), same impact treatment as eel_storyboard.py (it is imported for the world, the painter helpers and the effects).
  A  TAIL SNARE (revised by Yan): the TAIL comes out of a pool, catches a player, drags him back to the pool with it, then throws him out of ANOTHER free pool (not the one the head is in):
     he flies up into the air and crashes down. The head keeps striking at another player meanwhile; the tail is targetable (a damage bar): shoot it enough and it lets go.
  B  FLOOD SURGE: all four pools overflow, the floor between them turns to deep water, only the dry patches stay as islands; the eel glides through it as a shadow and bursts out under whoever is standing in it.
    python3 Tools/render/eel_storyboard2.py maps/renders/boss_eel/eel_storyboard2.mp4 [--probe 2.0,3.2,...]
A storyboard: scripted, no game code, no sound, placeholder timings and damage; the pools are painted over the render."""
import sys, os, math, random, tempfile
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import eel_storyboard as SB
from eel_storyboard import (Curve, bez, sp, pieces, shadow, kf, smr, ein, eout, lerp, c01, stamp, SPLASH, RIPPLES, THUNDER, WHIRL, WORLD, POOLS, C, MAN, FNT, WW, WH, VW, VH, PAD, NS, T, OX, OY, SPEED, FPS)
from eel_common import banner, video
import eel_poses as E
random.seed(11)
NX, NY = POOLS['N'][0], POOLS['N'][1]; EX, EY = POOLS['E'][0], POOLS['E'][1]; WX, WY = POOLS['W'][0], POOLS['W'][1]

# ------------------------------------------------------------------ A: the tail snare
GP = (36.0, 19.2); dG = math.hypot(GP[0] - NX, GP[1] - NY); UX, UY = (GP[0] - NX) / dG, (GP[1] - NY) / dG; RIM = 3.3      # where the player is grabbed; the tail goes straight out of the north pool to him
def dq(t):                                                # how far the tail's tip is from the pool's centre
    if t < 0.3: return 0.0
    if t < 1.9: return RIM * smr((t - 0.3) / 1.6)                                 # it surfaces slowly and slides out onto the rim: menacing, heavy
    if t < 2.25: return lerp(RIM, dG, ein((t - 1.9) / 0.35, 2.2))                 # the lunge at him: it accelerates into the grab
    if t < 2.75: return dG                                                        # it coils round him
    if t < 5.0: return lerp(dG, 0.6, ein((t - 2.75) / 2.25, 1.5))                  # the drag: it starts slowly against his weight, then speeds up
    return 0.6
def wrap_h(t): return 0.0 if t < 2.25 else smr((t - 2.25) / 0.5) if t < 2.75 else 1.0 if t < 5.0 else c01(1 - (t - 5.0) / 0.2)
def zq(d): return 0.45 if d >= RIM else -3.6 + 4.05 * smr(d / RIM)                 # the body is above ground out on the floor, under water inside the pool
def snare_curve(t):
    d = dq(t); h = wrap_h(t)
    if d < 0.05: return Curve([(NX, NY, -3.6), (NX, NY, -4.6)])
    s = np.linspace(0, 1, 150); px_, py_ = -UY, UX; dist = d * s
    xs_ = NX + UX * dist + px_ * 0.45 * np.sin(s * 2 * math.pi * 1.1) * np.sin(s * math.pi); ys_ = NY + UY * dist + py_ * 0.45 * np.sin(s * 2 * math.pi * 1.1) * np.sin(s * math.pi)
    zs_ = np.where(dist >= RIM, 0.45, -3.6 + 4.05 * np.vectorize(smr)(dist / RIM)); pts = np.stack([xs_, ys_, zs_], -1)
    if h > 0.02:                                                                  # the tip winds up round him: a helix, 1.6 turns, climbing
        a = np.linspace(0, 2 * math.pi * 1.6, 70); r = 0.62 * h; hx = xs_[-1] + r * np.cos(a + 1.0) - r * math.cos(1.0); hy = ys_[-1] + r * np.sin(a + 1.0) - r * math.sin(1.0); hz = zs_[-1] + 0.15 + 0.55 * h * (a / a[-1])
        pts = np.vstack([pts, np.stack([hx, hy, hz], -1)[1:]])
    return Curve(pts)
WC = Curve(bez((WX, WY, -3.6), (WX, WY, 2.0), (WX + 0.4, WY - 0.3, 5.0), (WX + 0.5, WY - 0.4, 8.2)))      # the west pool's free tail: it comes straight up and throws
T_THROW = 6.95; LAND = (22.5, 22.8)
def sigW(t):
    if t < 6.5: return 0.0
    if t < T_THROW: return WC.L * 0.97 * ein((t - 6.5) / 0.45, 2.0)                # it comes up fast with him on the tip ...
    if t < 7.1: return WC.L * 0.97                                                # ... stops dead (he does not)
    if t < 8.1: return WC.L * 0.97 * (1 - smr((t - 7.1) / 1.0))
    return 0.0
HE = Curve(bez((EX, EY, -3.2), (EX, EY, 6.5), (44.0, 19.0, 8.0), (41.5, 21.6, 0.45)))                          # the head, meanwhile, strikes at the second player from the east pool
def sigE(t):
    t0 = 1.2
    if t < t0: return 0.0
    if t < t0 + 1.6: return 0.62 * HE.L * smr((t - t0) / 1.6)
    if t < t0 + 2.1: return 0.62 * HE.L + 0.04 * HE.L * (t - t0 - 1.6) / 0.5
    if t < t0 + 2.3: return lerp(0.66 * HE.L, HE.L, ein((t - t0 - 2.1) / 0.2, 2.6))
    if t < t0 + 3.0: return HE.L
    if t < t0 + 4.1: return HE.L * (1 - smr((t - t0 - 3.0) / 1.1))
    return 0.0
TH_E = 1.2 + 2.3

def spineA(t):
    i = np.arange(NS); S_head = HE.at(sigE(t) - i / 32.0)
    if t < 6.2: cur = snare_curve(t); S_tail = cur.at(cur.L - (NS - 1 - i) / 32.0)
    else: S_tail = WC.at(sigW(t) - (NS - 1 - i) / 32.0)
    return np.where((i < NS // 2)[:, None], S_head, S_tail)
def tip(t):
    if t < 6.2: d = dq(t); return (NX + UX * d, NY + UY * d, zq(d)) if d >= RIM - 0.5 else None
    p = WC.at(sigW(t))[0]; return tuple(p)

def PA(t):   # the grabbed player (t, x, y, z, rot, flash): stands, is snared, dragged, goes under, is thrown out of the west pool, flies, crashes, gets up
    if t < 2.25: return (GP[0], GP[1], 0, 0, 0.0 + (0.6 if t > 1.6 else 0))
    if t < 5.0:
        d = dq(t)
        if d >= RIM: return (NX + UX * d, NY + UY * d, 0.55, 25 * math.sin(t * 9), 0.6)
        return (NX + UX * d, NY + UY * d, -3, 0, 0)                                  # under the water
    if t < 6.5: return (WX, WY, -3, 0, 0)
    if t < T_THROW: p = WC.at(sigW(t))[0]; return (p[0], p[1], max(p[2] - 0.3, -3), 0, 0.5)
    pr = WC.at(WC.L * 0.97)[0]
    if t < T_THROW + 1.45: u = (t - T_THROW) / 1.45; return (lerp(pr[0], LAND[0], u), lerp(pr[1], LAND[1], u), pr[2] * (1 - u) + 7.5 * 4 * u * (1 - u), 720 * u, 0.5 * (1 - u))
    if t < T_THROW + 2.2: return (LAND[0], LAND[1], 0, 720 + 80, 0.9 * c01(1 - (t - T_THROW - 1.45) / 0.5))
    if t < 9.6: return (LAND[0], LAND[1], 0, 0, 0)
    return (LAND[0], LAND[1], 0, 0, 0)
PB_A = kf([(0, 41.5, 21.6, 0, 0, 0), (TH_E - 0.55, 41.5, 21.6, 0, 0, 0), (TH_E - 0.3, 40.2, 23.0, 1.2, 180, 0), (TH_E + 0.05, 38.8, 24.4, 0, 360, 0), (TH_E + 0.4, 38.8, 24.4, 0, 360, 0), (5.2, 38.8, 24.4, 0, 360, 0), (9.0, 38.8, 24.4, 0, 360, 0), (99, 38.8, 24.4, 0, 360, 0)])

# ------------------------------------------------------------------ B: the flood
ISLANDS = [(8, 12, 6, 4), (46, 12, 6, 4), (21, 19, 4, 3), (36, 19, 4, 3), (28, 18, 5, 2)]
FLOOD_POOLS = [(WX, WY, 3.7, 2.4), (EX, EY, 3.7, 2.4), (NX, NY, 3.5, 2.3), (C[0], C[1], 8.2, 4.0)]
def flood_r(t):
    if t < 11.4: return 0.0
    if t < 13.0: return 17.0 * smr((t - 11.4) / 1.6)
    if t < 16.3: return 17.0
    if t < 17.6: return 17.0 * (1 - smr((t - 16.3) / 1.3))
    return 0.0
def flood_mask(r):
    m = Image.new('L', (WW, WH), 0); d = ImageDraw.Draw(m)
    for cx, cy, ra, rb in FLOOD_POOLS:
        x0, y0 = sp(cx - ra - r, cy - rb - r * 0.85); x1, y1 = sp(cx + ra + r, cy + rb + r * 0.85); d.ellipse((x0, y0, x1, y1), fill=255)
    room = Image.new('L', (WW, WH), 0); ImageDraw.Draw(room).rectangle((*sp(5, 9), *sp(55, 31)), fill=255); m = Image.fromarray(np.minimum(np.array(m), np.array(room)))
    d = ImageDraw.Draw(m)
    for x, y, w, h in ISLANDS: d.rectangle((*sp(x - 0.35, y - 0.35), *sp(x + w + 0.35, y + h + 0.35)), fill=0)
    return m
tex = WORLD.crop((int(C[0] * T - OX) - 140, int(C[1] * T - OY) - 60, int(C[0] * T - OX) + 140, int(C[1] * T - OY) + 60)).convert('RGBA')
BIGW = Image.new('RGBA', (WW + 280, WH + 120))
for ty in range(0, BIGW.height, tex.height):
    for tx in range(0, BIGW.width, tex.width): BIGW.paste(tex, (tx, ty))
BIGW = BIGW.crop((0, 0, WW, WH))
PTS_B = bez((EX, EY, -3.2), (EX - 0.5, EY, 2.0), (EX - 1.5, EY + 0.2, 0.2), (EX - 2.6, EY + 0.6, 0.02), 120)
sw_t = np.linspace(0, 1, 260); SW = np.stack([lerp(EX - 2.6, 38.5, sw_t), lerp(EY + 0.6, 24.5, sw_t) + 0.9 * np.sin(sw_t * math.pi * 3), 0.02 + 0 * sw_t], -1)
ARCH = bez((38.5, 24.5, 0.02), (38.0, 24.8, 6.5), (36.4, 25.2, 7.2), (35.5, 25.4, 0.3), 200)
HB = Curve(np.vstack([PTS_B, SW[1:], ARCH[1:]])); L_SW = Curve(np.vstack([PTS_B, SW[1:]])).L
def sigB(t):
    t0 = 13.0
    if t < t0: return 0.0
    if t < t0 + 1.6: return L_SW * smr((t - t0) / 1.6)                                   # glides through the flood
    if t < t0 + 2.4: return L_SW + 0.0
    if t < t0 + 3.0: return L_SW + 0.42 * (HB.L - L_SW) * smr((t - t0 - 2.4) / 0.6)          # it rears up out of the water
    if t < t0 + 3.2: return lerp(L_SW + 0.42 * (HB.L - L_SW), HB.L, ein((t - t0 - 3.0) / 0.2, 2.6))
    if t < t0 + 3.9: return HB.L
    if t < t0 + 5.0: return HB.L * (1 - 0.0) - (HB.L) * smr((t - t0 - 3.9) / 1.1)
    return 0.0
TB_HIT = 13.0 + 3.2
PB_B = kf([(0, 38.8, 24.4, 0, 360, 0), (9.6, 38.8, 24.4, 0, 360, 0), (10.0, 33.5, 27.5, 0, 0, 0), (11.4, 33.5, 27.5, 0, 0, 0), (11.9, 35.0, 25.6, 0, 0, 0), (12.5, 35.5, 25.4, -0.3, 0, 0), (15.0, 35.5, 25.4, -0.3, 0, 0),
          (TB_HIT, 35.5, 25.4, -0.3, 0, 0), (TB_HIT + 0.08, 33.4, 28.0, 3.0, 400, 1), (TB_HIT + 0.9, 30.2, 28.6, -0.3, 760, 1), (99, 30.2, 28.6, -0.3, 760, 0)])
PA_B = kf([(0, 22.5, 20.0, 0, 0, 0), (99, 22.5, 20.0, 0, 0, 0)])

def spineB(t):
    i = np.arange(NS); return HB.at(sigB(t) - i / 32.0)

# ------------------------------------------------------------------ the run
CAPS = [(0.0, 1.9, '4  TAIL SNARE (phase 2)', 'WIND-UP: the tail surfaces from the north pool - red ring on the target'), (1.9, 2.8, '4  TAIL SNARE (phase 2)', 'IT LUNGES AND COILS ROUND HIM'),
        (2.8, 5.3, '4  TAIL SNARE (phase 2)', 'DRAG - shoot the tail (teammates!) and it lets go'), (5.3, 6.6, '4  TAIL SNARE (phase 2)', 'under the water... another pool swells'), (6.6, 7.1, '4  TAIL SNARE (phase 2)', 'THROWN out of the west pool'),
        (7.1, 9.6, '4  TAIL SNARE (phase 2)', 'CRASH - the head keeps striking at the others the whole time'), (9.6, 11.4, '5  FLOOD SURGE (phase 2)', 'WIND-UP: all four pools swell, the dry patches light up'),
        (11.4, 13.0, '5  FLOOD SURGE (phase 2)', 'THE FLOOR TURNS TO DEEP WATER - get onto a patch'), (13.0, 15.5, '5  FLOOD SURGE (phase 2)', 'it swims through the flood as a shadow'), (15.5, 16.4, '5  FLOOD SURGE (phase 2)', 'BURSTS OUT UNDER HIM'),
        (16.4, 99, '5  FLOOD SURGE (phase 2)', 'the water drains away')]
def caption(t):
    for a, b, ttl, ph in CAPS:
        if a <= t < b: return ttl, ph
    return '', ''
def red(c, f, alpha):
    ov = Image.new('RGBA', c.size, (0, 0, 0, 0)); f(ImageDraw.Draw(ov), (255, 50, 35, int(alpha))); c.alpha_composite(ov)
def draw_player(c, q):
    x, y, z, rot, fl = q
    if z <= -1.0: return
    im = MAN.rotate(-rot, expand=True, resample=Image.NEAREST) if rot % 360 else MAN
    if fl > 0.05:
        im = im.copy(); px = im.load()
        for a in range(im.width):
            for b in range(im.height):
                r, g, bl, al = px[a, b]
                if al > 0: px[a, b] = (255, int(g * (1 - fl) + 80 * fl), int(bl * (1 - fl)), al)
    if z < 0 and z > -1.0: im = im.crop((0, 0, im.width, int(im.height * 0.62)))                       # wading: the water hides the legs
    sx, sy = sp(x, y, max(z, 0)); c.alpha_composite(im, (int(sx - im.width / 2), int(sy - im.height / 2 - 6 + (6 if z < 0 else 0))))

def run(out, probe=None):
    tmp = tempfile.mkdtemp(); simt = 0.0; rt = 0; dtf = 1.0 / FPS; outn = 0; prev = [None, None]; sheet = []; probes = sorted(probe) if probe else None
    def A(): SB.impact(2.25, GP[0], GP[1], 0.55, stop=3, crackn=3)
    events = sorted([(2.25, A), (TH_E, lambda: SB.impact(TH_E, 41.5, 21.6, 1.0, stop=3)), (5.0, lambda: SB.impact(5.0, NX + UX * RIM, NY + UY * RIM, 0.8, water=True, stop=2)),
                     (T_THROW, lambda: SB.impact(T_THROW, WX, WY, 1.0, water=True, stop=3)), (T_THROW + 1.45, lambda: SB.impact(T_THROW + 1.45, LAND[0], LAND[1], 1.3, stop=5, crackn=9)),
                     (TB_HIT, lambda: SB.impact(TB_HIT, 35.5, 25.4, 1.3, water=True, stop=4)), (9.6, lambda: (SB.cracks.clear(), SB.scorch.clear(), SB.rings.clear(), SB.parts.clear(), SB.splashes.clear()))], key=lambda e: e[0])
    maxt = 17.9
    while simt < maxt:
        if SB.frozen > 0: SB.frozen -= 1; step = 0.0
        else: step = dtf * SPEED
        for e in list(events):
            if simt < e[0] <= simt + step: e[1](); events.remove(e)
        simt += step; t = simt; beatA = t < 9.6
        c = WORLD.copy()
        if beatA:
            p = 0.65 + 0.35 * math.sin(rt * 0.5)
            if 0.3 < t < 2.25: red(c, lambda d, col: d.ellipse((*(np.array(sp(*GP)) - 58), *(np.array(sp(*GP)) + 58)), fill=col[:3] + (int(70 * p),), outline=col, width=3), 255)
            if 5.7 < t < T_THROW + 1.45: red(c, lambda d, col: d.ellipse((*(np.array(sp(*LAND)) - 40), *(np.array(sp(*LAND)) + 40)), outline=col, width=3), 190 * p)
            for nm, on in (('N', 0.3 < t < 2.4 or 3.0 < t < 5.2), ('E', 1.2 < t < 3.6), ('W', 5.4 < t < 6.9)):
                cx, cy = POOLS[nm][0], POOLS[nm][1]
                if on: stamp(c, WHIRL[int(t * 15) % len(WHIRL)], cx, cy, 1.9, 0.55)
        else:
            if 9.7 < t < 11.4:
                p = 0.65 + 0.35 * math.sin(rt * 0.5)
                for nm in ('W', 'E', 'N'): stamp(c, WHIRL[int(t * 15) % len(WHIRL)], POOLS[nm][0], POOLS[nm][1], 1.9, 0.6)
                stamp(c, WHIRL[int(t * 15) % len(WHIRL)], C[0], C[1], 2.6, 0.5)
                ov = Image.new('RGBA', c.size, (0, 0, 0, 0)); d = ImageDraw.Draw(ov)
                for x, y, w, h in ISLANDS: d.rectangle((*sp(x - 0.3, y - 0.3), *sp(x + w + 0.3, y + h + 0.3)), outline=(255, 255, 255, int(120 + 110 * p)), width=3)
                c.alpha_composite(ov)
            r = flood_r(t)
            if r > 0.05:                                                                                    # the flood: the real pool's water over the floor, foam at its edge, ripples
                m = flood_mask(r); wlay = BIGW.copy(); wlay.putalpha(m.point(lambda v: int(v * 0.94))); c.alpha_composite(wlay)
                edge = Image.fromarray(np.clip(np.array(m).astype(int) - np.array(m.filter(ImageFilter.MinFilter(7))).astype(int), 0, 255).astype(np.uint8)); fo = Image.new('RGBA', c.size, (210, 230, 235, 0)); fo.putalpha(edge.point(lambda v: int(v * 0.65))); c.alpha_composite(fo)
                for k in range(14):
                    gx = 8 + (k * 37 % 46); gy = 11 + (k * 53 % 19)
                    if m.getpixel((int(sp(gx, gy)[0]), int(sp(gx, gy)[1]))) > 200: stamp(c, RIPPLES[(int(t * 15) + k * 3) % len(RIPPLES)], gx, gy, 0.8, 0.45)
        for r_ in list(SB.rings):
            age = t - r_['t0']
            if age > r_['life']: SB.rings.remove(r_); continue
            if age < 0: continue
            k = age / r_['life']; rad = r_['r1'] * T * (1 - (1 - k) ** 2.2); x, y = sp(r_['x'], r_['y']); a = int(235 * (1 - k)); ov = Image.new('RGBA', c.size, (0, 0, 0, 0)); d = ImageDraw.Draw(ov)
            d.ellipse((x - rad, y - rad * 0.8, x + rad, y + rad * 0.8), outline=(235, 240, 240, a), width=max(2, int(r_['w'] * (1 - k)) + 1)); d.ellipse((x - rad * 0.92, y - rad * 0.74, x + rad * 0.92, y + rad * 0.74), outline=(120, 160, 175, a // 2), width=3); c.alpha_composite(ov)
        ov = Image.new('RGBA', c.size, (0, 0, 0, 0)); d = ImageDraw.Draw(ov)
        for (qx, qy, qr) in SB.scorch:
            gx, gy = sp(qx, qy)
            for kk, al in ((1.0, 40), (0.7, 55), (0.4, 70)): d.ellipse((gx - qr * T * kk, gy - qr * T * kk * 0.8, gx + qr * T * kk, gy + qr * T * kk * 0.8), fill=(18, 20, 18, al))
        for cr in SB.cracks: d.line([sp(*q) for q in cr['pts']], fill=(22, 24, 22, 215), width=3)
        c.alpha_composite(ov)
        S = spineA(t) if beatA else spineB(t); face = (-1 if beatA else -1)
        if beatA: pl = [PA(t), PB_A(t)]
        else: pl = [PB_B(t), PA_B(t)]
        c.alpha_composite(shadow(S))
        for q in pl:
            if q[2] <= 0.3: draw_player(c, q)
        if beatA:
            layers, P, vis = pieces(S, -1)
            cur = np.zeros((WH, WW, 4), np.uint8)
            for a, ly in layers: m_ = ly[..., 3] > 0; cur[m_] = ly[m_]
            speed = (abs(sigE(t + 0.03) - sigE(t - 0.03)) + abs(dq(t + 0.03) - dq(t - 0.03)) + abs(sigW(t + 0.03) - sigW(t - 0.03))) / 0.06 if step else 0.0
            if speed > 16 and prev[0] is not None:
                for g, alpha in ((prev[0], 0.16), (prev[1], 0.3)):
                    if g is not None: gi = Image.fromarray(g); gi.putalpha(gi.getchannel('A').point(lambda v, al=alpha: int(v * al))); c.alpha_composite(gi)
            if step: prev = [prev[1], cur]
            for a, ly in sorted(layers, key=lambda z: -z[0]): c.alpha_composite(Image.fromarray(ly))
        else:
            Sr = S.copy(); Sr[:, 2] = np.where(S[:, 2] > 0.5, S[:, 2], -1.0); layers, P, vis = pieces(Sr, -1)           # the raised part is the real body ...
            Ss = S.copy(); Ss[:, 2] = np.where(S[:, 2] > -0.2, 0.02, -1.0); lsh, _, _ = pieces(Ss, -1); m_f = flood_mask(flood_r(t)) if flood_r(t) > 0.05 else None   # ... the glide is a shadow, only where there is water
            for a, ly in lsh:
                sh = E.shadow(ly); im = Image.fromarray(sh); im2 = Image.fromarray(ly)
                if m_f is not None: im.putalpha(Image.fromarray(np.minimum(np.array(im.getchannel('A')), (np.array(m_f) * 0.9).astype(np.uint8)))); im2.putalpha(Image.fromarray(np.minimum(np.array(im2.getchannel('A')), (np.array(m_f) * 0.35).astype(np.uint8))))
                else: im.putalpha(0); im2.putalpha(0)
                c.alpha_composite(im); c.alpha_composite(im2)
            for a, ly in sorted(layers, key=lambda z: -z[0]): c.alpha_composite(Image.fromarray(ly))
        for i in range(NS - 1):                                                                              # foam and ripples where the body goes through the surface (in a pool)
            if (S[i, 2] >= 0) != (S[i + 1, 2] >= 0) and not (not beatA and flood_r(t) > 0.05):
                stamp(c, RIPPLES[int(t * 15) % len(RIPPLES)], S[i, 0], S[i, 1], 0.9, 0.85)
        for s_ in list(SB.splashes):
            idx = int((t - s_['t0']) * 15)
            if idx < 0: continue
            if idx >= len(s_['fr']): SB.splashes.remove(s_); continue
            stamp(c, s_['fr'][idx], s_['x'], s_['y'], s_['s'], 1.0, up=20 * s_['s'])
        if beatA and 2.8 < t < 5.0 and step:                                                                  # the drag: dust behind him on the floor
            d_ = dq(t)
            if d_ >= RIM and random.random() < 0.8: SB.dust(NX + UX * d_, NY + UY * d_ + 0.3, 1, 2.0)
        SB.draw_particles(c, t, step)
        tp = tip(t) if beatA else None
        if beatA and tp is not None and 2.2 < t < 5.2:                                                         # the tail is a target: a damage bar, and the second player shoots it
            hp = c01(1 - (t - 2.7) / 3.2) * 0.55 + 0.45 if t < 5.0 else 0.45; bx, by = sp(tp[0], tp[1], max(tp[2], 0) + 1.6); od = ImageDraw.Draw(c, 'RGBA')
            od.rectangle((bx - 46, by - 22, bx + 46, by - 8), fill=(0, 0, 0, 190)); od.rectangle((bx - 44, by - 20, bx - 44 + 88 * hp, by - 10), fill=(205, 80, 60, 255)); od.text((bx - 44, by - 38), 'TAIL - shoot it', font=FNT, fill=(255, 235, 200, 255))
            if 3.7 < t < 5.0 and rt % 3 == 0:
                q = pl[1]; x0_, y0_ = sp(q[0], q[1], 0.7); x1_, y1_ = sp(tp[0], tp[1], max(tp[2], 0) + 0.3); od.line((x0_, y0_, x1_, y1_), fill=(255, 235, 150, 230), width=3); od.rectangle((x1_ - 4, y1_ - 4, x1_ + 4, y1_ + 4), fill=(255, 255, 255, 255))
        for q in pl:
            if q[2] > 0.3: draw_player(c, q)
        pops = [(2.3, 'snared'), (T_THROW + 1.45, '-420  DROWNED')] if beatA else [(TB_HIT + 0.05, '-380')]
        for tpp, txt in pops:
            if 0 <= t - tpp < 0.9:
                q = pl[0]; x, y = sp(q[0], q[1], 2.0 + (t - tpp) * 2.2); ov = Image.new('RGBA', c.size, (0, 0, 0, 0)); ImageDraw.Draw(ov).text((x - 40, y), txt, font=FNT, fill=(255, 120, 100, int(255 * (1 - (t - tpp) / 0.9)))); c.alpha_composite(ov)
        SB.flash = min(SB.flash, 0.3)                                                                          # a flash, not a white-out
        if SB.flash > 0.02: c.alpha_composite(Image.new('RGBA', c.size, (255, 255, 255, int(255 * min(SB.flash, 0.7)))))
        sx_ = sy_ = 0
        if SB.shake_amp > 0.3: sx_ = int(random.uniform(-1, 1) * SB.shake_amp); sy_ = int(random.uniform(-1, 1) * SB.shake_amp)
        z_ = 1.0 + SB.zoom; w_, h_ = int(VW / z_), int(VH / z_); cx_, cy_ = PAD + sx_ + VW // 2, PAD + sy_ + VH // 2
        cropped = c.crop((cx_ - w_ // 2, cy_ - h_ // 2, cx_ - w_ // 2 + w_, cy_ - h_ // 2 + h_)); fr = cropped.resize((VW, VH), Image.NEAREST) if z_ > 1.001 else cropped
        ttl, ph = caption(t)
        if ttl: banner(fr, ttl, 12, size=24)
        if ph: banner(fr, ph, 50, size=22, fill=(255, 140, 110) if 'WIND' in ph else (255, 235, 150) if any(k in ph for k in ('LUNGES', 'THROWN', 'CRASH', 'BURSTS', 'DEEP WATER')) else (225, 225, 215))
        banner(fr, 'storyboard (phase 2): scripted keyframes on the current Sump + 3 painted pools; no game code, no sound, placeholder numbers', VH - 30, size=14, fill=(210, 205, 190), bg=(0, 0, 0, 150))
        SB.shake_amp *= 0.82; SB.flash *= 0.6; SB.zoom *= 0.82; rt += 1
        if probe:
            while probes and simt >= probes[0]: sheet.append((probes.pop(0), fr.copy()))
            if not probes: break
        else: fr.convert('RGB').save(os.path.join(tmp, f'{outn:05d}.png')); outn += 1
    if probe:
        w, h, cols = 640, 360, 3; rows = (len(sheet) + cols - 1) // cols; sh = Image.new('RGB', (cols * w, rows * h))
        for k, (pt, im) in enumerate(sheet): sh.paste(im.resize((w, h)).convert('RGB'), ((k % cols) * w, (k // cols) * h))
        sh.save(out); print(out, len(sheet), 'moments')
    else: video(tmp, out, FPS); print(out, outn, 'frames', round(outn / FPS, 1), 's')

if __name__ == '__main__':
    pr = None
    if '--probe' in sys.argv: pr = [float(v) for v in sys.argv[sys.argv.index('--probe') + 1].split(',')]
    run(sys.argv[1], pr)
