"""The brain in the vat (Yan, 2026-11-03), v3: animated. Drawn at double resolution like the HD mutants (128x160, ppu 32 ->
4x5 tiles: the 3-tile vat plus its machines). An 8-column sheet, one row per state:
    0-3  phase 0-3, eyes open       (the brain bobs in the liquid, bubbles rise, the monitor's trace runs)
    4-7  phase 0-3, eyes shut       (concentrating before an attack: it twitches on odd frames)
    8    drained                    (slumped, still, eyes shut)
    9    awake                      (the eyes open and it floats up off the floor of the vat)
    10   the eye                    (a giant eye on a cord slides out from behind it and turns to look at you)
Eyes grow with its anger: 2 at the start, 3, 4, then 6, each a little bigger (Yan: "starts the fight with 2 eyes and sprouts
more, they grow as he gets angrier"). Machines (Yan: "there needs to be some electronics connected to it"): a pump on the left,
a rack with a monitor and blinking lights on the right, a cable bundle out of the lid, electrode wires down to the brain, a
lit panel on the base. Procedural shapes snapped to the pixel grid, the locked palette rules (near-black outline, flat
3-tone shading, saturated colour only in the eyes, the psi glow and the lights).
    python3 ArtSource/bosses/make_brain_vat.py [out_dir] [game]     - 'game' also writes the sprites into Assets/Resources
"""
import sys, os, math, random
from PIL import Image

W, H, COLS = 128, 160, 8
OX, OY = 16, 8                                  # the old 96x144 vat sits here inside the canvas
K = 1.5
def k(v): return int(round(v * K))
def hexc(h, a=255): h = h.lstrip('#'); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)
OUT = hexc('#141617')
STEEL = [hexc(c) for c in ('#26292c', '#3a3e42', '#5a5f65', '#7d848a', '#a9b0b5')]
RUST, STRIPE = hexc('#6a4a32'), hexc('#b8962e')
LIQ = {'d': hexc('#22331d', 235), 'b': hexc('#34502c', 225), 'l': hexc('#4f7440', 225), 's': hexc('#7fa45e', 240)}
AIR = hexc('#1c2326', 150)
GLASS_HI, GLASS_RIM = hexc('#c4dcd6', 200), hexc('#5d7470', 230)
CRACK, CRACK_D = hexc('#e6f2ee'), hexc('#2a3532')
BR = {'d': hexc('#6e4e56'), 'b': hexc('#a07c84'), 'l': hexc('#c9a2a8'), 'k': hexc('#4a3038')}
EYE_W, EYE_I, EYE_P, EYE_G = hexc('#e8e0c8'), hexc('#c06bff'), hexc('#2a1036'), hexc('#ffffff')
VEIN, CORD, CORD_L, CORD_D = hexc('#b04a52'), hexc('#8a5a62'), hexc('#b07c84'), hexc('#5a343c')
PSI = hexc('#c06bff', 70)
SCREEN, TRACE, LED_G, LED_R, LED_A = hexc('#10241a'), hexc('#7fe08a'), hexc('#5ee07a'), hexc('#e0503a'), hexc('#f0c040')
WIRE = [hexc('#2a2420'), hexc('#4a3a2a'), hexc('#7a2a26')]

GX0, GX1, GY0, GY1 = OX + k(10), OX + k(53), OY + k(14), OY + k(80)
CX = OX + 48                                    # the vat's middle column
LEVELS = [OY + k(v) for v in (17, 34, 52, 68)]
EYES = [(k(dx), k(dy), r * K) for dx, dy, r in [(-5, 0, 3.4), (6, -3, 2.4), (2, 5, 2.0), (-10, 4, 1.6), (9, 4, 1.6), (-1, -6, 1.4)]]
EYE_COUNT = [2, 3, 4, 6]
CRACKS = [(OX + k(x), OY + k(y), [(k(a), k(b)) for a, b in legs]) for x, y, legs in
          [(14, 46, [(-1, -6), (3, -9), (2, 5), (5, 8), (-2, 9)]), (49, 30, [(-4, -5), (-3, 6), (2, 7), (-7, 2)]), (47, 62, [(-3, -7), (3, -4), (-5, 3), (1, 6), (-2, 8)])]]

def px(im, x, y, c):
    if 0 <= x < W and 0 <= y < H:
        if c[3] == 255: im.putpixel((x, y), c)
        else: im.alpha_composite(Image.new('RGBA', (1, 1), c), (x, y))

def tint(c, kk, g): return (int(c[0] * kk), int(min(255, c[1] * kk + g)), int(c[2] * kk * 0.9), c[3])

def line(im, x0, y0, x1, y1, c, w=1):
    n = max(abs(x1 - x0), abs(y1 - y0), 1)
    for i in range(n + 1):
        x = round(x0 + (x1 - x0) * i / n); y = round(y0 + (y1 - y0) * i / n)
        for d in range(w): px(im, x + d, y, c)

def disc(im, cx, cy, r, f):
    for y in range(int(cy - r - 1), int(cy + r + 2)):
        for x in range(int(cx - r - 1), int(cx + r + 2)):
            d = math.hypot(x + 0.5 - cx, y + 0.5 - cy)
            if d <= r + 0.5:
                c = f(d, x, y)
                if c: px(im, x, y, c)

def brain(im, cx, cy, level, eyes, eye_scale, shut, look=(0, 0)):
    rx, ry = k(13), k(10)
    for y in range(cy - ry - 1, cy + ry + 2):
        for x in range(cx - rx - 1, cx + rx + 2):
            u, v = (x - cx + 0.5) / rx, (y - cy + 0.5) / ry
            d = u * u + v * v; ang = math.atan2(v, u)
            if d > 1 + 0.07 * math.sin(ang * 11): continue
            c = BR['l'] if (u < -0.15 and v < -0.2 and d < 0.75) else BR['d'] if (u > 0.45 or v > 0.55) else BR['b']
            w = math.sin(math.sqrt(d) * 16 + math.sin(ang * 6 + (1 if x > cx else 0) * 2) * 1.7)
            if abs(x - cx) < 1 and v < 0.6: c = BR['k']
            elif abs(w) < 0.32 and 0.05 < d < 0.86: c = BR['k']
            if y >= level: c = tint(c, 0.78, 10)
            px(im, x, y, c)
    for dx, dy, r in EYES[:eyes]:
        r *= eye_scale; ex, ey = cx + dx, cy + dy
        if shut:
            for x in range(int(ex - r), int(ex + r) + 1): px(im, x, int(ey), BR['k'])   # a shut eye: one dark slit
            continue
        lx, ly = look[0] * r * 0.25, look[1] * r * 0.25
        def eye(d, x, y):
            if d > r - 0.4: c = BR['k']
            elif math.hypot(x + 0.5 - ex - lx, y + 0.5 - ey - ly) < r * 0.35: c = EYE_P
            elif math.hypot(x + 0.5 - ex - lx, y + 0.5 - ey - ly) < r * 0.72: c = EYE_I
            else: c = EYE_W
            return tint(c, 0.85, 8) if y >= level else c
        disc(im, ex, ey, r, eye)
        if r >= 1.5: px(im, int(ex - r * 0.4 + lx), int(ey - r * 0.4 + ly), EYE_G)
    for j in range(k(14)):                      # the stem
        x = cx + 1 + int(round(math.sin(j * 0.4) * 2)); y = cy + ry - 1 + j
        if y > GY1 - 1: break
        for d in (0, 1): px(im, x + d, y, tint(BR['d'], 0.78, 10) if y >= level else BR['d'])

def giant_eye(im, cx, cy, r, look, open_k):
    """The thing behind it: an eye as big as the brain, veined, on a fleshy cord that runs down behind the brain."""
    for j in range(40):                         # the cord, from the eye's underside down behind the brain to the vat floor
        t = j / 39; x = cx + int(round(math.sin(t * 3.1) * 6 - t * 4)); y = int(cy + r * 0.6 + t * (GY1 - cy - r * 0.6))
        for d in range(-2, 3): px(im, x + d, y, CORD_L if d < 0 else CORD_D if d == 2 else CORD)
    lx, ly = look[0] * r * 0.32, look[1] * r * 0.32
    rng = random.Random(3)
    veins = [(rng.uniform(0, math.tau), rng.uniform(0.5, 0.9)) for _ in range(9)]
    def f(d, x, y):
        if d > r - 0.6: return OUT
        if abs((y + 0.5 - cy)) > r * open_k: return CORD_D                 # the lids, opening
        dp = math.hypot(x + 0.5 - cx - lx, y + 0.5 - cy - ly)
        if dp < r * 0.18: return hexc('#0c0412')
        if dp < r * 0.46: return EYE_I if dp > r * 0.3 else hexc('#8a3ad0')
        ang = math.atan2(y + 0.5 - cy, x + 0.5 - cx)
        if d > r * 0.55 and any(abs(((ang - a + math.pi) % math.tau) - math.pi) < 0.06 and d / r > s for a, s in veins): return VEIN
        return EYE_W if d < r * 0.85 else hexc('#c8bca4')
    disc(im, cx, cy, r, f)
    if open_k > 0.3: px(im, int(cx - r * 0.3 + lx), int(cy - r * 0.3 + ly), EYE_G); px(im, int(cx - r * 0.3 + lx) + 1, int(cy - r * 0.3 + ly), EYE_G)

def machines(im, t, phase):
    blink = t % 2
    # the pump on the left: a ribbed cylinder with a gauge, a pipe into the base
    for y in range(GY1 - 34, GY1 + 18):
        for x in range(1, 15):
            c = STEEL[3] if x < 5 else STEEL[1] if x > 11 else STEEL[2]
            if (y - (GY1 - 34)) % 6 == 0: c = STEEL[0]
            px(im, x, y, c)
    disc(im, 8, GY1 - 24, 3.5, lambda d, x, y: STEEL[4] if d > 2.6 else hexc('#e8e4d8'))
    line(im, 8, GY1 - 24, 8 + round(2.5 * math.cos(t * 0.8 + 2)), GY1 - 24 + round(2.5 * math.sin(t * 0.8 + 2)), LED_R)
    for y in range(GY1 + 6, GY1 + 11): line(im, 15, y, GX0 - 6, y, STEEL[3] if y == GY1 + 7 else STEEL[1])
    # the rack on the right: a monitor with the brain's trace, rows of lights
    RX0, RX1 = W - 18, W - 2
    for y in range(GY1 - 50, GY1 + 18):
        for x in range(RX0, RX1):
            c = STEEL[2] if x < RX0 + 3 else STEEL[1]
            px(im, x, y, c)
    for y in range(GY1 - 46, GY1 - 30):
        for x in range(RX0 + 2, RX1 - 2): px(im, x, y, SCREEN)
    amp = 2 + phase
    for x in range(RX0 + 2, RX1 - 2):           # an EEG-like trace, scrolling with t, wilder as it gets angrier
        yy = GY1 - 38 + int(round(math.sin((x + t * 3) * 0.9) * amp * 0.5 + math.sin((x + t * 3) * 2.3) * amp * 0.4))
        px(im, x, max(GY1 - 45, min(GY1 - 31, yy)), TRACE)
    for row in range(4):
        for col in range(4):
            on = (row * 4 + col + t) % 3 != 0
            colr = LED_R if (row == 0 and phase >= 2 and blink) else LED_G if (row + col) % 3 else LED_A
            px(im, RX0 + 3 + col * 3, GY1 - 24 + row * 4, colr if on else STEEL[0])
    for y in range(GY1 + 6, GY1 + 11): line(im, GX1 + 7, y, RX0, y, STEEL[3] if y == GY1 + 7 else STEEL[1])
    # the cable bundle out of the lid, up and away to the ceiling; a thinner one down to the rack
    for i, (x0, col) in enumerate(((CX - 12, WIRE[0]), (CX - 4, WIRE[1]), (CX + 4, WIRE[2]), (CX + 12, WIRE[0]))):
        for y in range(0, OY + k(6) + 1):
            x = x0 + int(round(math.sin(y * 0.35 + i) * 1.5))
            px(im, x, y, col); px(im, x + 1, y, col)
    for j in range(30):
        x = GX1 + 3 + j * (RX0 - GX1 - 3) // 30; y = OY + k(9) + int(j * j * 0.05)
        px(im, x, y, WIRE[2]); px(im, x, y + 1, WIRE[0])

def frame(state, t):
    """state: ('open'|'shut', phase) | 'drained' | 'awake' | 'eye'"""
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    phase = state[1] if isinstance(state, tuple) else 3
    level = LEVELS[phase] if isinstance(state, tuple) else GY1 + 1
    for y in range(GY0, GY1):
        for x in range(GX0, GX1 + 1):
            c = AIR if y < level else LIQ['s'] if y == level else LIQ['d'] if x > GX1 - 6 else LIQ['l'] if x < GX0 + 5 else LIQ['b']
            px(im, x, y, c)
    if level < GY1:                             # bubbles: each rises at its own pace, wraps at the surface
        span = GY1 - level - 3
        for i in range(14):
            bx = GX0 + 4 + (i * 37) % (GX1 - GX0 - 8)
            by = GY1 - 3 - (i * 13 + t * (3 + i % 3)) % max(1, span)
            px(im, bx, by, LIQ['s'])
            if i % 4 == 0: px(im, bx + 1, by, LIQ['s']); px(im, bx, by - 1, hexc('#9fc47e', 200))
    bob = int(round(math.sin(t / COLS * math.tau) * (2 if phase < 2 else 1)))
    eyes, scale = EYE_COUNT[phase], 1.0 + 0.12 * phase
    if isinstance(state, tuple):
        cy = (OY + k(44) if level < OY + k(30) else min(GY1 - k(9), level + k(2))) + bob
        shut = state[0] == 'shut'
        cx = CX + (1 if shut and t % 2 else 0)  # concentrating: a twitch on odd frames
        cy += (-1 if shut and t % 4 == 1 else 0)
        brain_at = (cx, cy)
    elif state == 'drained': brain_at, shut = (CX, GY1 - k(9)), True
    elif state == 'awake': brain_at, shut = (CX, int(GY1 - k(9) - (GY1 - k(9) - (GY0 + 44)) * (t / (COLS - 1)) ** 1.6)), False
    else: brain_at, shut = (CX, GY0 + 44), False
    # electrode wires from the lid down to the top of the brain
    if isinstance(state, tuple) or state in ('drained', 'awake'):
        for i, dx in enumerate((-9, 0, 9)):
            x0, y0, x1, y1 = CX + dx * 2, GY0, brain_at[0] + dx, brain_at[1] - k(9)
            for j in range(16):
                s = j / 15; x = round(x0 + (x1 - x0) * s + math.sin(s * 3 + i) * 2); y = round(y0 + (y1 - y0) * s)
                px(im, x, y, WIRE[i % 3] if y < level else tint(WIRE[i % 3], 0.8, 8))
    if state == 'eye':                          # the giant eye slides out from behind, opens, then turns to look at you (down-left)
        s = min(1.0, t / 4); look = (-0.8, 0.7) if t >= 5 else (0, 0)
        giant_eye(im, CX + int(7 * s), brain_at[1] - int(19 * s), k(11), look, min(1.0, max(0.05, (t - 1) / 3)))
    brain(im, brain_at[0], brain_at[1], level, eyes, scale, shut, look=(-0.6, 0.5) if state == 'eye' and t >= 5 else (0, 0))
    for y in range(GY0, GY1):                   # glass
        px(im, GX0, y, GLASS_RIM); px(im, GX1, y, GLASS_RIM)
        px(im, GX0 + 2, y, GLASS_HI if y % 13 not in (0, 1) else GLASS_RIM); px(im, GX0 + 3, y, hexc('#c4dcd6', 110))
        if GY0 + 12 < y < GY0 + 70: px(im, GX0 + 6, y, hexc('#c4dcd6', 90))
        px(im, GX1 - 2, y, hexc('#c4dcd6', 70))
    for (sx, sy, legs) in CRACKS[:phase]:
        for (lx, ly) in legs:
            line(im, sx, sy, sx + lx, sy + ly, CRACK); line(im, sx + 1, sy + 1, sx + lx + 1, sy + ly + 1, CRACK_D)
        if isinstance(state, tuple) and level <= sy:
            for y in range(sy + 2, GY1 + 2): px(im, sx + (1 if sx > CX else -1), y, LIQ['l'])
    for y in range(OY + k(6), GY0):             # lid
        for x in range(GX0 - 4, GX1 + 5):
            c = STEEL[4] if y == OY + k(6) else STEEL[3] if x < OX + k(20) else STEEL[1] if x > OX + k(46) else STEEL[2]
            if y == OY + k(10) and x % 8 == 3: c = LED_G if (x // 8 + t) % 4 else LED_A
            if y == GY0 - 1: c = STEEL[0]
            px(im, x, y, c)
    for y in range(GY1, H - 1):                 # base with a lit panel and hazard stripes
        x0 = GX0 - 6 - (y - GY1) // 4; x1 = GX1 + 6 + (y - GY1) // 4
        for x in range(x0, x1 + 1):
            c = STEEL[3] if x < x0 + 6 else STEEL[1] if x > x1 - 8 else STEEL[2]
            if y == GY1: c = STEEL[4]
            if y > H - 4: c = STEEL[0]
            if y in (H - 6, H - 5) and (x - x0) % 6 < 3: c = STRIPE
            px(im, x, y, c)
        if GY1 + 4 <= y <= GY1 + 12:            # the panel: a dark plate with lights
            for x in range(CX - 14, CX + 15): px(im, x, y, STEEL[0])
    for i in range(7):
        on = (i + t) % 4 != 0
        px(im, CX - 12 + i * 4, GY1 + 7, (LED_R if phase >= 2 and i % 2 else LED_G) if on else STEEL[1])
        px(im, CX - 12 + i * 4, GY1 + 10, LED_A if (i * 3 + t) % 5 == 0 else STEEL[1])
    machines(im, t, phase)
    if not isinstance(state, tuple):            # the spill
        for x in range(GX0 - 6, GX1 + 4):
            if (x * 7) % 5: px(im, x, H - 1, LIQ['b'])
    a = im.copy().load(); o = im.load()          # outline the silhouette
    for y in range(H):
        for x in range(W):
            if a[x, y][3] == 0 and any(0 <= x + dx < W and 0 <= y + dy < H and a[x + dx, y + dy][3] > 100 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                o[x, y] = OUT
    if state in ('awake', 'eye'):               # psi stirring
        rng = random.Random(t)
        for i in range(26):
            ang = rng.random() * math.tau; r = rng.uniform(14, 28)
            px(im, int(brain_at[0] + math.cos(ang) * r * 1.3), int(brain_at[1] + math.sin(ang) * r * 0.7), PSI if i % 3 else hexc('#c06bff', 170))
    return im

ROWS = [('open', p) for p in range(4)] + [('shut', p) for p in range(4)] + ['drained', 'awake', 'eye']
if __name__ == '__main__':
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(os.path.abspath(__file__)), '_out')
    os.makedirs(out, exist_ok=True)
    sheet = Image.new('RGBA', (W * COLS, H * len(ROWS)))
    for r, st in enumerate(ROWS):
        for t in range(COLS): sheet.alpha_composite(frame(st, t), (t * W, r * H))
    sheet.save(os.path.join(out, 'boss_brain_vat.png'))
    # the attacks' sprites (Yan: "the orbs just spawn in and look exactly the same for every attack"), 12x12 at ppu 16 unless noted:
    # orbs: ring orbs (pale, a soft pulse) x2, void orbs (dark core, violet rim) x2; bolt: 18x8, an elongated aimed shot with a
    # hot head, x2; ring: 64x64, the pulse's wave front (drawn scaled up to its radius)
    orbs = Image.new('RGBA', (12 * 4, 12))
    for fr in range(4):
        void = fr >= 2; ph = fr % 2
        for y in range(12):
            for x in range(12):
                d = math.hypot(x - 5.5, y - 5.5); c = None
                if void:
                    if d < 2.2: c = hexc('#120818')
                    elif d < 3.4: c = hexc('#4a1a6a')
                    elif d < 4.4: c = hexc('#c06bff')
                    elif d < 5.5: c = hexc('#c06bff', 60 + ph * 50)
                else:
                    if d < 1.6 + ph * 0.5: c = hexc('#ffffff')
                    elif d < 2.8: c = hexc('#f0e0ff')
                    elif d < 3.8: c = hexc('#d8a8ff')
                    elif d < 5.0: c = hexc('#c06bff', 90 + ph * 40)
                if c: orbs.putpixel((fr * 12 + x, y), c)
    bolt = Image.new('RGBA', (18 * 2, 8))
    for fr in range(2):
        for y in range(8):
            for x in range(18):
                u = (x - 13) / (5 if x > 13 else 13); v = (y - 3.5) / (3.6 - 2.4 * max(0, -(x - 13) / 13))
                e = u * u + v * v; c = None
                if e < 0.25: c = hexc('#ffffff')
                elif e < 0.55: c = hexc('#f0d0ff')
                elif e < 1.0: c = hexc('#c06bff', 230 - fr * 40)
                if c: bolt.putpixel((fr * 18 + x, y), c)
    ring = Image.new('RGBA', (64, 64))
    for y in range(64):
        for x in range(64):
            d = math.hypot(x - 31.5, y - 31.5)
            if 29 <= d < 31: ring.putpixel((x, y), hexc('#f0e0ff', 230))
            elif 27 <= d < 29: ring.putpixel((x, y), hexc('#c06bff', 170))
            elif 23 <= d < 27: ring.putpixel((x, y), hexc('#c06bff', 60))
    for n, im in (('boss_brain_orbs', orbs), ('boss_brain_bolt', bolt), ('boss_brain_ring', ring)): im.save(os.path.join(out, n + '.png'))
    if len(sys.argv) > 2 and sys.argv[2] == 'game':
        game = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Assets', 'Resources', 'Sprites')
        sheet.save(os.path.join(game, 'Mutants', 'boss_brain_vat.png'))
        for n, im in (('boss_brain_orbs', orbs), ('boss_brain_bolt', bolt), ('boss_brain_ring', ring)): im.save(os.path.join(game, 'Fx', n + '.png'))
    print('wrote', os.path.join(out, 'boss_brain_vat.png'), sheet.size)
