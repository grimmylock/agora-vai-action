#!/usr/bin/env python3
"""A still of the Barrier siege (Yan, 2026-11-18: "can you generate a 'it would look like this with the battle in progress' still"): the real map render of the Vanguard Barracks (Tools/render/render.sh
vanguard_barracks) with the game's own armour, Husk, weapon and effect sprites placed on it where Tier4Story/Barrier.cs would put them: the Liberators on the catwalk's front row, the Obelisk horde
outside, Husks at the foot of the wall tearing at it, the Preacher with his violet core and a pulse, a gap in the barrier, grenade blasts, tracers, the HUD bar. NOT a Unity capture: no lighting, camera,
shadows beyond a flat one, UI beyond the bar; the cracks, rubble and dust are painted here.
    python3 Tools/render/barrier_battle.py out.png"""
import sys, os, math, random
from PIL import Image, ImageDraw, ImageFilter, ImageFont
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import squad_scene as S
import taken_stills as T
random.seed(11)
RAISE = len(sys.argv) > 2 and sys.argv[2] == 'raised'   # the WHAT-IF: the barrier and the catwalk on a raised steel deck (not built)
RH = 14
CX, APEX, R, X0, X1, END = 156, 106, 35, 128, 184, 120
def arc_y(x): return round(APEX + R - math.sqrt(R * R - (x - CX) ** 2))
cols = {}
for x in range(X0, X1 + 1):
    y0 = arc_y(x); y1 = arc_y(x + 1) if x < X1 else y0; cols[x] = (min(y0, y1), max(y0, y1))
TW, TH, VX, VY = 36, 26, 156, 105
sc = T.mapcrop('vanguard_barracks', VX, VY, TW, TH)
def foot(x, y): return T.tile(sc, TW, TH, VX, VY, x, y)
def px(x, y): c = foot(x, y); return (c[0], c[1] - 14)
d_over = Image.new('RGBA', sc.size, (0, 0, 0, 0)); D = ImageDraw.Draw(d_over, 'RGBA')
# ---- the wall: cracks, scorch, a gap
def tile_rect(x, y): ox, oy = int((x - (VX - TW / 2)) * 32), int((y - (VY - TH / 2)) * 32); return ox, oy, ox + 32, oy + 32
rnd = random.Random(5)
dmg = {x: rnd.random() for x in range(X0, X1 + 1)}
for x in range(146, 168): dmg[x] = 0.55 + rnd.random() * 0.4            # the hardest-hit stretch
GAP = (154, 155)
for x in range(X0, X1 + 1):
    lo, hi = cols[x]
    for y in range(lo, hi + 1):
        x0, y0, x1, y1 = tile_rect(x, y)
        if x in GAP and y == hi:
            D.rectangle((x0 - 2, y0 - 2, x1 + 2, y1 + 2), fill=(74, 68, 58, 255))                     # the gap: the block gone
            for _ in range(18):
                rx, ry = rnd.randint(x0, x1), rnd.randint(y0 - 2, y1 + 4); s = rnd.randint(3, 9); c = rnd.choice([(110, 104, 96), (140, 134, 126), (84, 78, 70), (160, 152, 140)])
                D.polygon([(rx, ry), (rx + s, ry + 1), (rx + s - 2, ry + s - 2), (rx - 1, ry + s - 1)], fill=c + (255,))
            continue
        if dmg[x] > 0.35 and rnd.random() < dmg[x]:
            for _ in range(int(2 + dmg[x] * 4)):                                                           # cracks
                cx_, cy_ = rnd.randint(x0 + 3, x1 - 3), rnd.randint(y0 + 2, y1 - 6)
                pts = [(cx_, cy_)]
                for k in range(rnd.randint(3, 6)): pts.append((pts[-1][0] + rnd.randint(-4, 4), pts[-1][1] + rnd.randint(1, 4)))
                D.line(pts, fill=(34, 30, 28, 230), width=2)
            if rnd.random() < 0.6: D.ellipse((x0 + 2, y0 + 4, x0 + 30, y0 + 22), fill=(30, 26, 24, 90))     # scorch
sc.alpha_composite(d_over)
if RAISE:
    base = sc.copy(); stairs = (148, 149, 156, 163, 164)
    for x in range(X0, X1 + 1):
        lo, hi = cols[x]; ty0, ty1 = lo, hi + 3
        x0 = int((x - (VX - TW / 2)) * 32); y0 = int((ty0 - (VY - TH / 2)) * 32); y1 = int((ty1 - (VY - TH / 2)) * 32)
        if x0 < 0 or x0 + 32 > sc.width or y1 > sc.height: continue
        sh = Image.new('RGBA', (32, 20), (0, 0, 0, 0)); ImageDraw.Draw(sh).rectangle((0, 0, 32, 20), fill=(8, 8, 10, 0))
        for k in range(14): sc.alpha_composite(Image.new('RGBA', (32, 1), (6, 6, 8, int(110 * (1 - k / 14)))), (x0, y1 + k))      # the deck's shadow on the ground
        sc.alpha_composite(base.crop((x0, y0, x0 + 32, y1)), (x0, y0 - RH))                                                         # the deck and the barrier, lifted
        fd = ImageDraw.Draw(sc, 'RGBA'); fy = y1 - RH
        for k in range(RH):                                                                                                         # the deck's front face
            t = k / (RH - 1); c = (int(104 - 58 * t), int(108 - 60 * t), int(116 - 62 * t), 255); fd.line((x0, fy + k, x0 + 31, fy + k), fill=c)
        fd.line((x0, fy, x0 + 31, fy), fill=(176, 180, 186, 255))
        for rx in (x0 + 5, x0 + 26): fd.rectangle((rx, fy + 4, rx + 1, fy + 5), fill=(30, 32, 36, 255))
        if x in stairs:                                                                                                              # steps down to the yard
            for k in range(3): fd.rectangle((x0 + 3, fy + RH - 5 * (k + 1) + 1, x0 + 28, fy + RH - 5 * k), fill=(120 - 18 * k, 124 - 18 * k, 130 - 18 * k, 255)); fd.line((x0 + 3, fy + RH - 5 * (k + 1) + 1, x0 + 28, fy + RH - 5 * (k + 1) + 1), fill=(190, 194, 200, 255))
        else:                                                                                                                        # a railing along the deck's south edge
            fd.line((x0, fy - 9, x0 + 31, fy - 9), fill=(170, 170, 164, 210), width=1); fd.line((x0, fy - 4, x0 + 31, fy - 4), fill=(150, 150, 146, 200), width=1)
            for rx in (x0 + 1, x0 + 30): fd.rectangle((rx, fy - 10, rx + 1, fy), fill=(120, 120, 116, 235))
S.shadow  # (kept for the import's sake)
# ---- units
ARM_LIB = [98, 101, 96, 91]; ARM_OBE = [124, 127, 131, 124, 134]; WEAPONS = [('BR86Bulldog', 'assault'), ('Corvan57', 'assault'), ('BlackHarrier', 'smg'), ('BayouPump', 'shotgun'), ('VK74', 'assault')]
def aim(fx, fy, tx, ty): return math.degrees(math.atan2(-(ty - fy), tx - fx))
def husk(scene, f, idx, flip=False):
    S.shadow(scene, f); c = S.cell('husk_atlas', idx); c = c.transpose(Image.FLIP_LEFT_RIGHT) if flip else c
    scene.alpha_composite(c, (int(f[0] - 16), int(f[1] - 30)))
units = []   # (y for ordering, fn)
foes = []
# the Obelisk horde, north of the wall
tries = 0
while len(foes) < 26 and tries < 4000:
    tries += 1; x = rnd.randint(141, 171); top = cols[min(max(x, X0), X1)][0]; y = top - rnd.randint(3, 12)
    if y < VY - TH // 2 + 3 or any(abs(x - a) < 2 and abs(y - b) < 2 for a, b in foes): continue
    foes.append((x, y))
for i, (x, y) in enumerate(foes):
    f = foot(x, y); a = ARM_OBE[i % len(ARM_OBE)]; w, cat = WEAPONS[i % len(WEAPONS)]
    tgt = (CX + rnd.randint(-8, 8), cols[CX][1] + 2); ang = aim(x, y, tgt[0], tgt[1])
    units.append((y, lambda sc_=sc, f=f, a=a, w=w, cat=cat, ang=ang, i=i: (S.shadow(sc_, f), S.soldier(sc_, f, a, w, cat, ang, 'a' if i % 2 else 'b', shoot=(i % 3 == 0)))))
# husks at the foot of the wall, tearing at it
hx = [149, 151, 153, 156, 158, 160, 162, 164]
for i, x in enumerate(hx):
    y = cols[x][0] - 1; f = foot(x, y); idx = [124, 31, 98, 127, 40, 124, 56, 31][i]
    units.append((y, lambda sc_=sc, f=f, idx=idx, i=i: husk(sc_, f, idx, i % 2 == 1)))
# a few more Husks behind them
for i, (x, y) in enumerate([(148, 98), (153, 99), (160, 98), (164, 100), (167, 102), (145, 102)]):
    units.append((y, lambda sc_=sc, f=foot(x, y), idx=[31, 124, 98, 40, 127, 56][i], i=i: husk(sc_, f, idx, i % 2 == 0)))
# through the gap: three Obelisk pouring in
for i, (x, y) in enumerate([(154, 108), (155, 110), (154, 112)]):
    f = foot(x, y); units.append((y, lambda sc_=sc, f=f, i=i: (S.shadow(sc_, f), S.soldier(sc_, f, [124, 131, 127][i], 'VK74', 'assault', -80 - i * 10, 'a' if i % 2 else 'b', shoot=(i == 0)))))
# the Preacher: white exoskeleton, a violet glow, a pulse ring
pre = foot(157, 96)
glow = Image.new('RGBA', sc.size, (0, 0, 0, 0)); G = ImageDraw.Draw(glow)
for r_, a_ in [(300, 18), (240, 26), (180, 34), (120, 46), (70, 70)]: G.ellipse((pre[0] - r_, pre[1] - r_ * 0.62, pre[0] + r_, pre[1] + r_ * 0.62), fill=(150, 70, 220, a_))
G.ellipse((pre[0] - 288, pre[1] - 180, pre[0] + 288, pre[1] + 180), outline=(190, 120, 255, 200), width=4)
G.ellipse((pre[0] - 270, pre[1] - 168, pre[0] + 270, pre[1] + 168), outline=(190, 120, 255, 90), width=2)
glow = glow.filter(ImageFilter.GaussianBlur(2)); sc.alpha_composite(glow)
S.shadow(sc, pre); sc.alpha_composite(S.cell('armor_atlas', 134), (pre[0] - 16, pre[1] - 30))
core = Image.new('RGBA', sc.size, (0, 0, 0, 0)); ImageDraw.Draw(core).ellipse((pre[0] - 9, pre[1] - 34, pre[0] + 9, pre[1] - 18), fill=(190, 110, 255, 255)); core = core.filter(ImageFilter.GaussianBlur(3)); sc.alpha_composite(core)
ImageDraw.Draw(sc).ellipse((pre[0] - 5, pre[1] - 30, pre[0] + 5, pre[1] - 21), fill=(235, 200, 255, 255))
# defenders on the catwalk's front row
lib_x = [140, 143, 146, 149, 152, 158, 161, 164, 167, 170]
for i, x in enumerate(lib_x):
    y = cols[x][1] + 1; f = foot(x, y); f = (f[0], f[1] - (RH if RAISE else 0)); a = ARM_LIB[i % 4]; w, cat = WEAPONS[(i + 1) % len(WEAPONS)]
    tx, ty = foes[i % len(foes)]; ang = aim(x, y, tx, ty)
    units.append((y, lambda sc_=sc, f=f, a=a, w=w, cat=cat, ang=ang, i=i: (S.shadow(sc_, f), S.soldier(sc_, f, a, w, cat, ang, None, shoot=(i % 2 == 0)))))
for _, fn in sorted(units, key=lambda u: u[0]): fn()
# ---- tracers, impacts, blasts
fx = Image.new('RGBA', sc.size, (0, 0, 0, 0)); F = ImageDraw.Draw(fx)
for _ in range(40):
    x, y = foes[rnd.randrange(len(foes))]; a = foot(x, y); tx = x + rnd.randint(-5, 5); ty = cols[min(max(tx, X0), X1)][0]; b = foot(tx, ty)
    dx, dy = b[0] - a[0], b[1] - 8 - (a[1] - 18); L = math.hypot(dx, dy) or 1; s0 = rnd.uniform(0.15, 0.7); s1 = min(1.0, s0 + rnd.uniform(0.12, 0.25))
    F.line((a[0] + dx * s0, a[1] - 18 + dy * s0, a[0] + dx * s1, a[1] - 18 + dy * s1), fill=(255, 226, 150, 190), width=2)
    if rnd.random() < 0.5: F.ellipse((b[0] - 3, b[1] - 12, b[0] + 3, b[1] - 6), fill=(255, 240, 190, 230))                  # an impact on the wall
for i, x in enumerate(lib_x):
    if i % 2: continue
    a = foot(x, cols[x][1] + 1); tx, ty = foes[i % len(foes)]; b = foot(tx, ty); dx, dy = b[0] - a[0], b[1] - 16 - (a[1] - 20); s0 = rnd.uniform(0.2, 0.5)
    F.line((a[0] + dx * s0, a[1] - 20 + dy * s0, a[0] + dx * (s0 + 0.2), a[1] - 20 + dy * (s0 + 0.2)), fill=(255, 236, 170, 220), width=2)
sc.alpha_composite(fx)
exp = Image.open(os.path.join(S.SP, 'Fx/px_small_explosion.png')).convert('RGBA'); fw = exp.height
for (x, y, k, sz) in [(146, 101, 9, 170), (166, 104, 10, 150), (150, 108, 8, 140)]:
    fr = exp.crop((k * fw, 0, (k + 1) * fw, fw)).resize((sz, sz), Image.NEAREST); f = foot(x, y); sc.alpha_composite(fr, (f[0] - sz // 2, f[1] - sz // 2 - 10))
# dust over the gap
dust = Image.new('RGBA', sc.size, (0, 0, 0, 0)); dd = ImageDraw.Draw(dust); gf = foot(154, 105)
for _ in range(14): r_ = rnd.randint(22, 46); ox, oy = rnd.randint(-40, 60), rnd.randint(-30, 26); dd.ellipse((gf[0] + ox - r_, gf[1] + oy - r_, gf[0] + ox + r_, gf[1] + oy + r_), fill=(170, 160, 140, 34))
sc.alpha_composite(dust.filter(ImageFilter.GaussianBlur(8)))
# ---- the light: a cold dawn, darker at the edges
shade = Image.new('RGBA', sc.size, (60, 70, 110, 34)); sc.alpha_composite(shade)
vig = Image.new('L', sc.size, 0); ImageDraw.Draw(vig).ellipse((-sc.width * 0.15, -sc.height * 0.25, sc.width * 1.15, sc.height * 1.25), fill=255); vig = vig.filter(ImageFilter.GaussianBlur(120))
dark = Image.new('RGBA', sc.size, (6, 8, 14, 150)); dark.putalpha(Image.eval(vig, lambda v: 150 - int(v * 150 / 255))); sc.alpha_composite(dark)
# ---- the HUD bar (as HudView.Siege.cs draws it)
W = 560; bx = (sc.width - W) // 2; H = ImageDraw.Draw(sc, 'RGBA')
H.rectangle((bx, 16, bx + W, 32), fill=(150, 120, 80, 255)); H.rectangle((bx + 2, 18, bx + W - 2, 30), fill=(20, 18, 18, 255))
frac = 0.61; H.rectangle((bx + 2, 18, bx + 2 + int((W - 4) * 0.74), 30), fill=(242, 217, 153, 255)); H.rectangle((bx + 2, 18, bx + 2 + int((W - 4) * frac), 30), fill=(208, 160, 64, 255))
txt = 'THE BARRIER  61%    HORDE  48 / 90'; f = T.font(18); tw = H.textlength(txt, font=f); H.text(((sc.width - tw) / 2 + 1, 37), txt, fill=(0, 0, 0, 255), font=f); H.text(((sc.width - tw) / 2, 36), txt, fill=(236, 226, 200, 255), font=f)
T.caption(sc, 'WHAT-IF (not built): the catwalk and barrier on a raised steel deck, with railings and steps. A composite, not a Unity capture' if RAISE else 'Barrier siege: a still made from the game sprites and the real map render (not a Unity capture)')
sc = sc.resize((int(sc.width * 1.5), int(sc.height * 1.5)), Image.NEAREST)
out = sys.argv[1] if len(sys.argv) > 1 else 'barrier_battle.png'; sc.convert('RGB').save(out); print(out, sc.size)
