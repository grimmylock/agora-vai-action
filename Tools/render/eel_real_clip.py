"""The Sump fight, drawn from the REAL boss code (EelSump.cs / EelBoss.cs): the harness runs the whole fight (dotnet /tmp/test.dll eelsump frames.json) and writes, every frame, the eel's spine in 3D,
the players (with their height and spin when thrown), the telegraph zones, the impacts and hits, the flood and the tail's HP. This only DRAWS them: Yan's drawing bent along the spine by the real painter,
on the real Sump render (four pools), with the impact treatment of the storyboards (hit-stop, shake, zoom punch, rings, splashes, debris, cracks, afterimages, shadows). No move is scripted here.
    Tools/compile_check/runtest.sh ../../Assets/Scripts ; dotnet /tmp/test.dll eelsump frames.json ; Tools/render/render.sh sub_station_depths_3
    python3 Tools/render/eel_real_clip.py frames.json maps/renders/boss_eel/eel_real.mp4        (--probe 3.0,12.0 for a contact sheet)
Placeholders: the body that is swallowed and brought back is the armour sprite lying down; the thrown/hurled players are bots at fixed spots; no sound; no Unity view exists yet (this is a preview)."""
import sys, os, json, math, tempfile, random
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import eel_storyboard as SB
from eel_storyboard import pieces, shadow, stamp, SPLASH, RIPPLES, WHIRL, MAN, FNT, WW, WH, VW, VH, PAD, NS, T, OX, OY, sp, c01
from eel_common import render, plan, banner, video, dead_body, font
import eel_poses as E
import eel_bake_jaw as J
import eel_jaw_tips as JT
random.seed(3)
H0, A0 = E.HEAD.copy(), tuple(E.HEAD_ANCHOR); _jawc = {}
def jaw_head(o):
    """the head with its mouth open by `o` (0 shut .. 1 as drawn .. 1.4 forced wide), cut from his drawing (eel_bake_jaw.py), cropped, with its anchor"""
    key = round(min(1.4, max(0.0, o)) * 10) / 10
    if key not in _jawc:
        fr_ = J.jaw_frame(H0, key); ys_, xs_ = np.nonzero(fr_[:, :, 3] > 0); bx0, by0 = xs_.min(), ys_.min()
        _jawc[key] = (fr_[ys_.min():ys_.max() + 1, xs_.min():xs_.max() + 1], (A0[0] + J.PAD - bx0, A0[1] + J.PAD - by0))
    return _jawc[key]
def zigzag(p0, p1, n=7, j=14):
    pts = [p0]
    for i in range(1, n): u = i / n; pts.append((p0[0] + (p1[0] - p0[0]) * u + random.uniform(-j, j), p0[1] + (p1[1] - p0[1]) * u + random.uniform(-j, j)))
    pts.append(p1); return pts

BODYMASK = None
def aura(S, k, t, n_strands, big=False):
    """lightning that COILS AROUND the eel (Yan, 2026-11-07: 'around it, coiling, not on the sprite, much smaller'): thin 1 px strands winding round the body like a helix, floating a little off the skin.
    The body's silhouette (the painted layers) gives the centre line and the width at every row, so the coils follow the real sprite. The back of each turn is hidden behind the body (drawn only where it is
    outside the outline), the front crosses it. Tiny forks and sparks. Native resolution, 1 px core."""
    if BODYMASK is None: return None
    ys_, xs_ = np.nonzero(BODYMASK)
    if len(ys_) < 50: return None
    top, bot = ys_.min(), ys_.max(); y_a = top + int(0.28 * (bot - top)); y_b = bot - 2                             # below the head
    cx = {}; hw = {}
    for y in range(y_a, y_b + 1):
        xr = np.nonzero(BODYMASK[y])[0]
        if len(xr): cx[y] = (xr.min() + xr.max()) / 2.0; hw[y] = max(4.0, (xr.max() - xr.min()) / 2.0)
    if len(cx) < 20: return None
    lo = Image.new('RGBA', (WW, WH), (0, 0, 0, 0)); d = ImageDraw.Draw(lo)
    for _ in range(n_strands):
        L = int(random.uniform(0.45, 0.8 + 0.2 * k) * (y_b - y_a)); y0 = random.randint(y_a, max(y_a, y_b - L)); ph = random.uniform(0, 6.283)
        pitch = random.uniform(80, 130); direction = random.choice((-1, 1)); prev = None; y = y0
        while y < min(y_b, y0 + L):
            if y in cx:
                a = ph + direction * 6.283 * (y - y0) / pitch; sn = math.sin(a); front = math.cos(a) > 0; R = hw[y] * 1.28                                    # a little wider than the skin
                x = cx[y] + R * sn + random.uniform(-1.0, 1.0); yy = y + random.uniform(-1.0, 1.0); shown = front or abs(sn) * 1.28 > 1.02
                if shown and prev is not None: d.line((prev[0], prev[1], x, yy), fill=(120, 200, 255, 80), width=3); d.line((prev[0], prev[1], x, yy), fill=(238, 251, 255, 255), width=1)
                prev = (x, yy) if shown else None
                if shown and random.random() < 0.05 * (1 + k) and abs(sn) > 0.5:                                  # a tiny fork off the outer loops
                    sg = 1 if sn > 0 else -1; fx = x + sg * random.uniform(4, 10); fy = yy + random.uniform(-7, 7); d.line((x, yy, (x + fx) / 2, (yy + fy) / 2 + random.uniform(-2, 2), fx, fy), fill=(238, 251, 255, 235), width=1); d.point((fx, fy), fill=(255, 255, 255, 255))
            y += 1
    return lo
ZAPS = []; close = 0.0; HROT = 0.0; CHG0 = None
def grip_points(S, face, mo):
    """the jaw-hold in the preview: the two gloves on the jaw tips (eel_jaw_tips.py), and where he stands, in front of the face: (upper, lower, stand) as (x, y, z) tile points"""
    jk = 6 if mo > 1.2 else int(round(min(max(mo, 0.0), 1.0) * 5)) if mo >= 0 else 5
    ux, uy, lx, ly = JT.TIPS[jk]; mir = 1 if face == -1 else -1
    a_ = math.radians(HROT) * (1 if face == -1 else -1); ca, sa = math.cos(a_), math.sin(a_)                  # the head is turned about its anchor (screen y down): turn the tips with it
    def turn(x, y): x *= mir; return (x * ca - y * sa, x * sa + y * ca)
    (tux, tuy), (tlx, tly) = turn(ux, uy), turn(lx, ly)
    up = (S[0, 0] + tux / 32.0, S[0, 1], S[0, 2] - tuy / 32.0); lo = (S[0, 0] + tlx / 32.0, S[0, 1], S[0, 2] - tly / 32.0)
    return up, lo, None
def glove(c, p, flip):
    """the game's big glove (5x4 character pixels: HeldWeaponView.GloveBig, the suit's cuff and glove colours) at the body's own pixel scale, fingers toward the head"""
    cp = max(1, MAN.width // 16); cols = {'K': (20, 22, 23, 255), 'c': (38, 36, 37, 255), 'l': (88, 94, 104, 255), 'g': (61, 65, 72, 255), 'd': (39, 42, 46, 255)}      # the suit's own cuff #262425 and glove #3d4148 (SpriteAtlasIndex, the first armor: what the preview's player wears)
    img = Image.new('RGBA', (5, 4))
    for y, row in enumerate([".KKK.", "KcllK", "KcgdK", ".KKK."]):
        for x, ch in enumerate(row):
            if ch != '.': img.putpixel((x, y), cols[ch])
    if flip: img = img.transpose(Image.FLIP_LEFT_RIGHT)
    img = img.resize((5 * cp, 4 * cp), Image.NEAREST); x, y = sp(*p); c.alpha_composite(img, (int(x - img.width / 2), int(y - img.height / 2)))
def stars(c, S, t):
    hx, hy = sp(S[0, 0], S[0, 1], max(S[0, 2], 0) + 1.3); d = ImageDraw.Draw(c, 'RGBA')
    for q in range(4):
        an = t * 3.2 * 1.0 + q * math.pi / 2; x = hx + math.cos(an) * 30; y = hy + math.sin(an) * 9
        d.line((x - 6, y, x + 6, y), fill=(255, 216, 64, 255), width=3); d.line((x, y - 6, x, y + 6), fill=(255, 216, 64, 255), width=3); d.rectangle((x - 1, y - 1, x + 1, y + 1), fill=(255, 255, 230, 255))
d = json.load(open(sys.argv[1])); out = sys.argv[2]; FR = d['frames']; FPS = 15
W3 = render('sub_station_depths_3').crop((OX, OY, OX + WW, OY + WH)).convert('RGBA'); PLAN = plan('sub_station_depths_3')
POOLS = [(p[1], p[2], p[3], p[4], p[0], p[5]) for p in d['pools']]; SLAB = d['slab']; BODY = dead_body(d['armor']); TAILMAX, MAXHP = d['tailmax'], d['maxhp']
SHALLOW = [(x, y, min(math.hypot(x + 0.5 - px, y + 0.5 - py) - (rx + ry) / 2 for px, py, rx, ry, nm, bg in POOLS)) for y in range(9, 31) for x in range(5, 55) if PLAN[y][x] == 'w']   # the tiles the flood can take (the game's rule)
tex = W3.crop((int(30.5 * T - OX) - 150, int(24.5 * T - OY) - 50, int(30.5 * T - OX) + 150, int(24.5 * T - OY) + 50)).convert('RGBA'); WATER = Image.new('RGBA', (WW + 300, WH + 100))
for ty in range(0, WATER.height, tex.height):
    for tx in range(0, WATER.width, tex.width): WATER.paste(tex, (tx, ty))
WATER = WATER.crop((0, 0, WW, WH))
POOLM = Image.fromarray(((np.array(W3)[..., :3].max(axis=2) < 34) & (np.array(W3)[..., 3] > 0)).astype(np.uint8) * 255)       # the base render's black holes = the pools
ISL = [(8, 12, 6, 4), (46, 12, 6, 4), (21, 19, 4, 3), (36, 19, 4, 3), (28, 18, 5, 2)]
def interp(spine):
    a = np.array(spine, float).reshape(-1, 3); n = len(a); idx = np.linspace(0, n - 1, NS); i0 = np.floor(idx).astype(int).clip(0, n - 2); u = (idx - i0)[:, None]; return a[i0] * (1 - u) + a[i0 + 1] * u
def flood_mask(r):
    m = Image.new('L', (WW, WH), 0); dr = ImageDraw.Draw(m)
    for x, y, dd in SHALLOW:
        if dd <= r: x0, y0 = sp(x, y); dr.rectangle((x0, y0, x0 + T - 1, y0 + T - 1), fill=255)
    return m
def red(c, fn, alpha):
    ov = Image.new('RGBA', c.size, (0, 0, 0, 0)); fn(ImageDraw.Draw(ov), (255, 50, 35, int(alpha))); c.alpha_composite(ov)
def draw_player(c, q, flash):
    x, y, z, rot, hp, prone = q
    if z <= -1.0: return                                                                        # under the water, in the tail's grip
    im = MAN.rotate(-rot, expand=True, resample=Image.NEAREST) if rot % 360 > 1 else MAN
    if prone and z <= 0.05: im = MAN.rotate(80, expand=True, resample=Image.NEAREST)
    if flash > 0.05:
        im = im.copy(); px = im.load()
        for a in range(im.width):
            for b in range(im.height):
                r, g, bl, al = px[a, b]
                if al > 0: px[a, b] = (255, int(g * (1 - flash) + 80 * flash), int(bl * (1 - flash)), al)
    sx, sy = sp(x, y, max(z, 0)); c.alpha_composite(im, (int(sx - im.width / 2), int(sy - im.height / 2 - 6)))

def run(probe=None):
    global close
    tmp = tempfile.mkdtemp(); fi = 0; n = 0; shown = -1; pops = []; hitat = {}; sheet = []; probes = sorted(probe) if probe else None; prev = [None, None]; prevhead = None
    while fi < len(FR):
        f = FR[fi]; t = f['t']; adv = SB.frozen == 0
        if SB.frozen > 0: SB.frozen -= 1
        if fi != shown:                                                                      # the events of this frame, once
            shown = fi
            for e in f['ev']:
                if e[0] == 'impact': SB.impact(t, e[1], e[2], e[3], bool(e[4]), stop=2, crackn=7); SB.frozen = min(SB.frozen, 2)
                elif e[0] == 'hit': pops.append((t, e[5], e[6], e[7])); hitat[e[5]] = t
                elif e[0] == 'spark': ZAPS.append(('spark', t, e[1], e[2], e[3]))
                elif e[0] == 'storm': ZAPS.append(('burst', t, e[1], e[2], 1.0)); SB.flash = 0.6; SB.shake_amp = max(SB.shake_amp, 14); SB.zoom = max(SB.zoom, 0.08)
                elif e[0] == 'parry': ZAPS.append(('parry', t, e[1], e[2], 1.0)); SB.flash = 0.4; SB.shake_amp = max(SB.shake_amp, 8)
                elif e[0] == 'glint': ZAPS.append(('glint', t, e[1], e[2], 1.0)); SB.flash = 0.25
        step = 1.0 / FPS if adv else 0.0
        c = W3.copy(); S = interp(f['spine']); fl = f['flood']
        if f.get('bite') in ('grapple', 'lift', 'smash'): S[:, 0] += random.uniform(-0.09, 0.09) * (0.5 + f.get('tw', 0)); S[:, 1] += random.uniform(-0.06, 0.06) * (0.5 + f.get('tw', 0))   # the whole body shakes with the strain
        mo = f.get('mouth', -1); E.HEAD, E.HEAD_ANCHOR = jaw_head(mo) if mo >= 0 else (H0, A0)                       # the mouth: shut, opening, held wide, closing with every press
        global HROT
        HROT = 0.0; SB.HEAD_ROT = 0.0
        if f.get('rear', 0) > 2:                                                                           # thrown back / reared: the head turns with its neck, the neck's first stretch follows it (no break at the joint)
            p0x, p0y = S[0, 0] * T, (S[0, 1] - S[0, 2]) * T; p8x, p8y = S[8, 0] * T, (S[8, 1] - S[8, 2]) * T
            HROT = max(-88.0, min(88.0, math.degrees(math.atan2(p8y - p0y, abs(p8x - p0x)))))                # + = the neck leaves downward = nose up
            hh, aa = E.HEAD, E.HEAD_ANCHOR; P_ = 40; big = Image.new('RGBA', (hh.shape[1] + 2 * P_, hh.shape[0] + 2 * P_), (0, 0, 0, 0)); big.alpha_composite(Image.fromarray(hh), (P_, P_))
            big = np.array(big.rotate(-HROT, center=(aa[0] + P_, aa[1] + P_), resample=Image.NEAREST)); ys_, xs_ = np.nonzero(big[:, :, 3] > 0)
            E.HEAD, E.HEAD_ANCHOR = big[ys_.min():ys_.max() + 1, xs_.min():xs_.max() + 1], (aa[0] + P_ - xs_.min(), aa[1] + P_ - ys_.min()); SB.HEAD_ROT = HROT
        if f.get('tw', 0) > 0.3: SB.shake_amp = max(SB.shake_amp, 3.0 * f['tw'])                                      # every press of the struggle jolts it
        if 'swell' in f['label'] or ('wind-up' in f['label'] and f['move'] in ('arch', 'dash', 'snare', 'combo')) and False:
            pass
        if 'swell' in f['label']:
            for px, py, rx, ry, nm, bg in POOLS: stamp(c, WHIRL[int(t * 15) % len(WHIRL)], px, py, 2.6 if bg else 1.9, 0.6)
        if fl > 0.05:                                                                         # the flood: the game's own tiles, the pool's water over them, foam at the edge
            m = flood_mask(fl); arr_ = np.array(W3).astype(float); arr_[..., :3] = np.clip(arr_[..., :3] * np.array([0.9, 0.92, 0.92]), 0, 255)    # the room's own water, just a little deeper: SAME colour (no tint)
            wl = Image.fromarray(arr_.astype(np.uint8)); wl.putalpha(m.point(lambda v: int(v * 0.9))); c.alpha_composite(wl)
            dp = Image.new('RGBA', c.size, (4, 12, 12, 0)); dp.putalpha(POOLM.point(lambda v: int(v * 0.5))); c.alpha_composite(dp)                  # the pools are deep water: a darker, bluer one
            hl = Image.new('RGBA', c.size, (0, 0, 0, 0)); hd = ImageDraw.Draw(hl)                                      # light moving on the surface
            for _k in range(70):
                gx = random.uniform(5, 55); gy = random.uniform(9, 31); sx_, sy_ = sp(gx, gy)
                if 0 <= sx_ < m.width and 0 <= sy_ < m.height and m.getpixel((int(sx_), int(sy_))) > 200: hd.line((sx_, sy_, sx_ + random.randint(6, 16), sy_), fill=(190, 235, 245, 120), width=2)
            c.alpha_composite(hl)
            for k in range(22):
                gx = 8 + (k * 37 % 46); gy = 11 + (k * 53 % 19)
                if m.getpixel((int(sp(gx, gy)[0]), int(sp(gx, gy)[1]))) > 200: stamp(c, RIPPLES[(int(t * 15) + k * 3) % len(RIPPLES)], gx, gy, 0.9, 0.7)
        if fl > 0.05 and f['move'] == 'storm' and ('charging' in f['label'] or 'ELECTRIC' in f['label']):                  # the water crackles while it charges
            m_ = flood_mask(fl); el = Image.new('RGBA', c.size, (120, 220, 255, 0)); el.putalpha(m_.point(lambda v: int(v * (0.03 + 0.05 * random.random())))); c.alpha_composite(el)
        for z in f['zones']:                                                                  # telegraphs (red) and live zones (white), exactly where the code will hurt
            x, y = sp(z[0], z[1]); live = z[4]; col = (255, 255, 255, 235) if live else (255, 60, 40, 150 + 80 * (n // 2 % 2))
            if z[3] == 'band' and len(z) > 5:
                pts = [sp(z[5][i], z[5][i + 1]) for i in range(0, len(z[5]), 2)]; red(c, lambda dr, co: dr.line(pts, fill=co[:3] + (80,), width=96 if f['move'] == 'arch' else 128), 255)
                if f['move'] == 'arch': red(c, lambda dr, co: dr.ellipse((x - z[2] * T, y - z[2] * T, x + z[2] * T, y + z[2] * T), outline=co, width=3), 220)
            elif z[3] == 'sweep': red(c, lambda dr, co: dr.ellipse((x - z[2] * T, y - z[2] * T, x + z[2] * T, y + z[2] * T), outline=co, width=3), 160 if not live else 230)
            else:
                ov = Image.new('RGBA', c.size, (0, 0, 0, 0)); ImageDraw.Draw(ov).ellipse((x - z[2] * T, y - z[2] * T, x + z[2] * T, y + z[2] * T), fill=col[:3] + (60,), outline=col, width=3); c.alpha_composite(ov)
        for r_ in list(SB.rings):
            age = t - r_['t0']
            if age > r_['life']: SB.rings.remove(r_); continue
            if age < 0: continue
            k = age / r_['life']; rad = r_['r1'] * T * (1 - (1 - k) ** 2.2); x, y = sp(r_['x'], r_['y']); a = int(235 * (1 - k)); ov = Image.new('RGBA', c.size, (0, 0, 0, 0)); dr = ImageDraw.Draw(ov)
            dr.ellipse((x - rad, y - rad * 0.8, x + rad, y + rad * 0.8), outline=(235, 240, 240, a), width=max(2, int(r_['w'] * (1 - k)) + 1)); dr.ellipse((x - rad * 0.92, y - rad * 0.74, x + rad * 0.92, y + rad * 0.74), outline=(120, 160, 175, a // 2), width=3); c.alpha_composite(ov)
        ov = Image.new('RGBA', c.size, (0, 0, 0, 0)); dr = ImageDraw.Draw(ov)
        for (qx, qy, qr) in SB.scorch:
            gx, gy = sp(qx, qy)
            for kk, al in ((1.0, 40), (0.7, 55), (0.4, 70)): dr.ellipse((gx - qr * T * kk, gy - qr * T * kk * 0.8, gx + qr * T * kk, gy + qr * T * kk * 0.8), fill=(18, 20, 18, al))
        for cr in SB.cracks: dr.line([sp(*q) for q in cr['pts']], fill=(22, 24, 22, 215), width=3)
        c.alpha_composite(ov); c.alpha_composite(shadow(S))
        if not f['swallowed'] and f['stage'] not in ('vomit',) or f['ejected']:
            c.alpha_composite(BODY, (int(sp(SLAB[0], SLAB[1])[0] - BODY.width / 2), int(sp(SLAB[0], SLAB[1])[1] - BODY.height / 2)))
        pl = [list(q) for q in f['pl']]
        grip_on = f.get('bite') in ('grapple', 'lift', 'smash')
        if grip_on and pl:
            tw_ = f.get('tw', 0); sgn_ = 1 if face == -1 else -1                                              # he leans toward the head (it is on his right when the head looks left)
            pl[0][0] += random.uniform(-0.07, 0.07) * tw_ - sgn_ * 0.2 * (1 - f.get('grip', 0)); pl[0][1] += random.uniform(-0.04, 0.04) * tw_
            pl[0][3] = sgn_ * (14 if (n // 2) % 2 == 0 else 6) * -1                                           # pressing in, alternating
        for i, q in enumerate(pl):
            if q[2] <= 0.3: draw_player(c, q, 0.9 * c01(1 - (t - hitat.get(i, -9)) / 0.5))
        face = -1 if S[min(8, NS - 1), 0] > S[0, 0] else 1
        if f.get('bite') in ('grapple', 'lift', 'smash'): face = 1 if f['pl'][0][0] >= S[0, 0] else -1                   # held: it faces HIM                                    # the head looks away from its neck
        flooding = fl > 0.5 and f['move'] == 'flood'
        if flooding:
            Sr = S.copy(); Sr[:, 2] = np.where(S[:, 2] > 0.5, S[:, 2], -1.0); layers, P, vis = pieces(Sr, face)
            Ss = S.copy(); Ss[:, 2] = np.where(S[:, 2] > -0.2, 0.02, -1.0); Ss[40:, 2] = -1.0
            if S[0, 2] > 0.5: Ss[:, 2] = -1.0   # rearing/bursting: the rest of the body is the shadow no more
            # the swim is a shadow of the first 10 tiles only (a 35-tile smear cut by the dry islands read as two eels)
            lsh, _, _ = pieces(Ss, face); mf = flood_mask(fl)
            for a, ly in lsh:
                sh = Image.fromarray(E.shadow(ly)); im2 = Image.fromarray(ly)
                sh.putalpha(Image.fromarray(np.minimum(np.array(sh.getchannel('A')), (np.array(mf) * 0.9).astype(np.uint8)))); im2.putalpha(Image.fromarray(np.minimum(np.array(im2.getchannel('A')), (np.array(mf) * 0.35).astype(np.uint8)))); c.alpha_composite(sh); c.alpha_composite(im2)
        else: layers, P, vis = pieces(S, face)
        cur = np.zeros((WH, WW, 4), np.uint8)
        for a, ly in layers: mk = ly[..., 3] > 0; cur[mk] = ly[mk]
        head = S[0]; hv = 0.0 if prevhead is None else math.hypot(head[0] - prevhead[0], head[1] - prevhead[1]) * FPS; prevhead = head.copy() if adv else prevhead
        if hv > 14 and prev[0] is not None:                                                    # afterimages while it moves fast
            for g, alpha in ((prev[0], 0.16), (prev[1], 0.3)):
                if g is not None: gi = Image.fromarray(g); gi.putalpha(gi.getchannel('A').point(lambda v, al=alpha: int(v * al))); c.alpha_composite(gi)
        if adv: prev = [prev[1], cur]
        for a, ly in sorted(layers, key=lambda z: -z[0]): c.alpha_composite(Image.fromarray(ly))
        global BODYMASK
        BODYMASK = np.zeros((WH, WW), bool)
        for a, ly in layers: BODYMASK |= ly[..., 3] > 0
        if f.get('bite') in ('grapple', 'lift', 'smash'):                                                   # Yan's sketch: a glove on each jaw tip - and he stays where he stood
            gu, gl, _ = grip_points(S, face, f.get('mouth', -1)); glove(c, gu, face == 1); glove(c, gl, face == 1)
            if os.environ.get('EELDBG') and abs(t - 33.0) < 0.04: print('DBG face', face, 'player', sp(pl[0][0], pl[0][1], 0), 'glove up', sp(*gu), 'glove lo', sp(*gl), 'head', sp(S[0,0], S[0,1], S[0,2]), 'T', T, 'MAN', MAN.size)
        if f.get('bite') == 'slump': stars(c, S, t)                                                       # stunned: stars circle its head
        zo = Image.new('RGBA', c.size, (0, 0, 0, 0)); zd = ImageDraw.Draw(zo); hx_, hy_ = sp(S[0, 0], S[0, 1], max(S[0, 2], 0) + 0.6)
        for z in list(ZAPS):
            kind, z0, zx, zy, zp = z; age = t - z0; life = 0.22 if kind == 'spark' else 0.7 if kind == 'burst' else 0.45 if kind == 'glint' else 0.5
            if age > life: ZAPS.remove(z); continue
            if age < 0: continue
            vis_ = [i for i in range(NS) if S[i, 2] > 0.3]
            def crawl(i0, ln, wd):                                                                    # electricity that follows the body's outline
                pts = []
                for i in range(i0, min(NS, i0 + ln), 3):
                    if S[i, 2] <= 0.3: pts = []; continue
                    q = sp(S[i, 0], S[i, 1], S[i, 2]); pts.append((q[0] + random.uniform(-9, 9), q[1] + random.uniform(-9, 9)))
                    if len(pts) >= 2: zd.line(pts[-2:], fill=(120, 210, 255, 130), width=wd + 4); zd.line(pts[-2:], fill=(235, 250, 255, 245), width=max(2, wd - 2))
            def drop(i):                                                                              # a bolt from the body down to the water beneath it
                if S[i, 2] <= 0.6: return
                q0 = sp(S[i, 0], S[i, 1], S[i, 2]); q1 = sp(S[i, 0] + random.uniform(-0.8, 0.8), S[i, 1] + random.uniform(-0.8, 0.8), 0.0); pts = zigzag(q0, q1, 5, 10)
                zd.line(pts, fill=(120, 210, 255, 130), width=8); zd.line(pts, fill=(235, 250, 255, 245), width=3)
            if kind == 'spark' and vis_:
                pass                                                                                          # the body's lightning is aura() now
            elif kind == 'burst':
                for _ in range(8):
                    tx_, ty_, dd_ = random.choice([q for q in SHALLOW if q[2] <= max(fl, 1.0)] or SHALLOW); p0 = sp(tx_ + 0.5, ty_ + 0.5); p1 = (p0[0] + random.uniform(-60, 60), p0[1] + random.uniform(-40, 40)); pts = zigzag(p0, p1, 4, 9)
                    zd.line(pts, fill=(120, 210, 255, int(120 * (1 - age / life))), width=4); zd.line(pts, fill=(240, 252, 255, int(255 * (1 - age / life))), width=1)
                m_ = flood_mask(max(fl, 1.0)); el = Image.new('RGBA', c.size, (150, 230, 255, 0)); el.putalpha(m_.point(lambda v, k=age / life: int(v * 0.35 * (1 - k) * (0.5 + 0.5 * random.random())))); c.alpha_composite(el)
            elif kind == 'glint':
                gx_, gy_ = sp(S[0, 0], S[0, 1], max(S[0, 2], 0) + 0.5); rr_ = 14 + 70 * (age / life)
                for ang_ in range(0, 360, 45): zd.line((gx_ - math.cos(math.radians(ang_)) * rr_, gy_ - math.sin(math.radians(ang_)) * rr_, gx_ + math.cos(math.radians(ang_)) * rr_, gy_ + math.sin(math.radians(ang_)) * rr_), fill=(255, 255, 255, int(255 * (1 - age / life))), width=4)
            elif kind == 'parry':
                px_, py_ = sp(zx, zy, 0.8); rr_ = 20 + 150 * (age / life); zd.ellipse((px_ - rr_, py_ - rr_ * 0.8, px_ + rr_, py_ + rr_ * 0.8), outline=(255, 255, 255, int(255 * (1 - age / life))), width=5)
        c.alpha_composite(zo)
        global CHG0
        lab_ = f['label']
        if f['move'] == 'storm' and 'charging' in lab_:
            if CHG0 is None: CHG0 = t
            kk = c01((t - CHG0) / 4.7); au = aura(S, kk, t, int(1 + 5 * kk), False)                                 # builds slowly, then crackles all over
        elif f['move'] == 'storm' and ('ELECTRIC' in lab_ or 'SCREAMS' in lab_):
            au = aura(S, 1.0, t, 9, True)                                                                              # at the scream: everywhere
        else: CHG0 = None; au = None
        if au is not None: c.alpha_composite(au)
        for i in range(NS - 1):                                                                # foam and ripples where the body goes through the water: always a pool's centre
            if (S[i, 2] > 0.02) != (S[i + 1, 2] > 0.02) and not (fl > 0.05): stamp(c, RIPPLES[int(t * 15) % len(RIPPLES)], S[i, 0], S[i, 1], 0.9, 0.85)
        for s_ in list(SB.splashes):
            idx = int((t - s_['t0']) * 15)
            if idx < 0: continue
            if idx >= len(s_['fr']): SB.splashes.remove(s_); continue
            stamp(c, s_['fr'][idx], s_['x'], s_['y'], s_['s'], s_.get('a', 1.0), up=20 * s_['s'])
        if f['move'] == 'dash' and 'DASH!' in f['label'] and adv:
            for q in range(0, NS, 90):
                if 0.02 < S[q, 2] < 0.9 and random.random() < 0.5: SB.dust(S[q, 0], S[q, 1] + 0.3, 1, 2.0)
        SB.draw_particles(c, t, step)
        if f['tout'] and f['move'] in ('snare', 'combo'):                                       # the tail is a target: its own bar, and the tracer when a teammate shoots it
            tx, ty, tz = S[NS - 1]; bx, by = sp(tx, ty, max(tz, 0) + 1.6); od = ImageDraw.Draw(c, 'RGBA'); hp = c01(f['thp'] / TAILMAX)
            od.rectangle((bx - 46, by - 22, bx + 46, by - 8), fill=(0, 0, 0, 190)); od.rectangle((bx - 44, by - 20, bx - 44 + 88 * hp, by - 10), fill=(205, 80, 60, 255)); od.text((bx - 44, by - 38), 'TAIL - shoot it', font=FNT, fill=(255, 235, 200, 255))
            if f['shoot'] and len(pl) > 1: x0_, y0_ = sp(pl[1][0], pl[1][1], 0.7); x1_, y1_ = sp(tx, ty, max(tz, 0) + 0.3); od.line((x0_, y0_, x1_, y1_), fill=(255, 235, 150, 230), width=3); od.rectangle((x1_ - 4, y1_ - 4, x1_ + 4, y1_ + 4), fill=(255, 255, 255, 255))
        for i, q in enumerate(pl):
            if q[2] > 0.3: draw_player(c, q, 0.9 * c01(1 - (t - hitat.get(i, -9)) / 0.5))
        for (tp_, who, dmg, txt) in pops:
            if 0 <= t - tp_ < 0.9 and who < len(pl):
                q = pl[who]; x, y = sp(q[0], q[1], 2.0 + (t - tp_) * 2.2); ov = Image.new('RGBA', c.size, (0, 0, 0, 0)); ImageDraw.Draw(ov).text((x - 55, y), f'-{dmg}  {txt}', font=FNT, fill=(255, 120, 100, int(255 * (1 - (t - tp_) / 0.9)))); c.alpha_composite(ov)
        SB.flash = min(SB.flash, 0.18)
        if SB.flash > 0.02: c.alpha_composite(Image.new('RGBA', c.size, (255, 255, 255, int(255 * SB.flash))))
        sx_ = sy_ = 0
        if SB.shake_amp > 0.3: sx_ = int(random.uniform(-1, 1) * SB.shake_amp); sy_ = int(random.uniform(-1, 1) * SB.shake_amp)
        close += (1.0 - close) * 0.18 if f.get('bite') in ('grapple', 'lift', 'smash') else (0.0 - close) * 0.25                                  # the jaw-hold: the camera leans right in on the player holding the mouth
        z_ = 1.0 + SB.zoom + 2.6 * close; w_, h_ = int(VW / z_), int(VH / z_); cx_, cy_ = PAD + sx_ + VW // 2, PAD + sy_ + VH // 2
        if close > 0.01:
            _gu, _gl, _gs = grip_points(S, face, f.get('mouth', -1)); spx, spy = sp(((_gu[0] + _gl[0]) / 2 + pl[0][0]) / 2, S[0, 1], (_gu[2] + _gl[2]) / 2 * 0.8); cx_ = int(cx_ + (spx - cx_) * 0.95 * close); cy_ = int(cy_ + (spy - cy_) * 0.95 * close)
        cx_ = max(w_ // 2, min(WW - w_ // 2, cx_)); cy_ = max(h_ // 2, min(WH - h_ // 2, cy_))
        fr = c.crop((cx_ - w_ // 2, cy_ - h_ // 2, cx_ - w_ // 2 + w_, cy_ - h_ // 2 + h_)); fr = fr.resize((VW, VH), Image.NEAREST) if z_ > 1.001 else fr
        ttl = {'combo': '1  COMBO: head strike + tail sweep', 'arch': '2  ARCH SLAM', 'dash': '3  SNAKE DASH', 'snare': '4  TAIL SNARE (' + f['var'] + ')', 'flood': '5  FLOOD SURGE', 'storm': '6  STORM: electric flood + the bite'}.get(f['move'], 'REVEAL' if f['stage'] == 'reveal' else 'DEATH: it brings the body back up' if f['stage'] == 'vomit' else '')
        if ttl: banner(fr, ttl, 12, size=24)
        lab = f['label']; hot = 'wind-up' in lab or 'swell' in lab
        banner(fr, lab, 50, size=22, fill=(255, 140, 110) if hot else (255, 235, 150) if any(k in lab for k in ('STRIKE', 'SLAM', 'SWEEP', 'DASH!', 'CAUGHT', 'DRAGGED', 'THROWN', 'HURLED', 'BURSTS', 'TURNS', 'ELECTRIC', 'SPRINGS', 'PARRIED', 'SLAM', 'THROWS', 'SCREAMS', 'PARRY on the glint')) else (225, 225, 215))
        if f.get('bite') == 'grapple':                                                                                       # the struggle bar and the prompt
            gb = ImageDraw.Draw(fr, 'RGBA'); gp = c01(f['grip']); gb.rectangle((VW // 2 - 182, VH - 128, VW // 2 + 182, VH - 100), fill=(0, 0, 0, 200)); gb.rectangle((VW // 2 - 178, VH - 124, VW // 2 - 178 + 356 * gp, VH - 104), fill=(255, 235, 150, 255))
            banner(fr, 'MASH THE PARRY KEY!', VH - 168, size=28, fill=(255, 245, 190))
        od = ImageDraw.Draw(fr, 'RGBA'); od.rectangle((VW // 2 - 262, VH - 56, VW // 2 + 262, VH - 40), fill=(0, 0, 0, 190)); od.rectangle((VW // 2 - 260, VH - 54, VW // 2 - 260 + 520 * c01(f['hp'] / MAXHP), VH - 42), fill=(150, 40, 40, 255))
        od.text((VW // 2 - 256, VH - 54), f"THE DROWNER (placeholder name)  head {f['hp']}   tail {f['thp']}", font=font(12), fill=(255, 245, 225))
        banner(fr, 'drawn from the real fight code (EelSump.cs) by the harness | preview only: no Unity view, silent here (the game has the sounds) | players are bots', VH - 30, size=14, fill=(210, 205, 190), bg=(0, 0, 0, 150))
        SB.shake_amp *= 0.67; SB.flash *= 0.4; SB.zoom *= 0.67
        if probe:
            while probes and t >= probes[0]: sheet.append((probes.pop(0), fr.copy()))
            if not probes: break
        else: fr.convert('RGB').save(os.path.join(tmp, f'{n:05d}.png'))
        n += 1
        if adv: fi += 1
    if probe:
        w, h, cols = 640, 360, 3; rows = (len(sheet) + cols - 1) // cols; sh = Image.new('RGB', (cols * w, rows * h))
        for k, (pt, im) in enumerate(sheet):
            if os.environ.get("PROBE_FULL"): im.convert("RGB").save(os.path.splitext(out)[0] + "_%d.png" % k)
            sh.paste(im.resize((w, h)).convert('RGB'), ((k % cols) * w, (k // cols) * h))
        sh.save(out); print(out, len(sheet), 'moments')
    else: video(tmp, out, FPS); print(out, n, 'frames', round(n / FPS, 1), 's')

if __name__ == '__main__':
    pr = [float(v) for v in sys.argv[sys.argv.index('--probe') + 1].split(',')] if '--probe' in sys.argv else None
    run(pr)
