"""The brain in the vat (Yan, 2026-11-03): a psychic mutant brain with several eyes, floating in murky liquid in a lab vat.
Drawn at double resolution like the HD mutants (64x96, ppu 32 -> 2x3 tiles). States: full, crack1-3 (liquid drains), drained
(eyes shut), awake (eyes open, before the void pull takes it away). Procedural shapes snapped to the pixel grid, the locked
palette rules (near-black outline, flat 3-tone shading, saturated colour only in the eyes/psi glow).
    python3 ArtSource/bosses/make_brain_vat.py [out_dir]
"""
import sys, os, math, random
from PIL import Image

W, H = 64, 96
def hexc(h, a=255): h = h.lstrip('#'); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)
OUT = hexc('#141617')
STEEL = [hexc(c) for c in ('#26292c', '#3a3e42', '#5a5f65', '#7d848a', '#a9b0b5')]
RUST = hexc('#6a4a32')
LIQ = {'d': hexc('#22331d', 235), 'b': hexc('#34502c', 225), 'l': hexc('#4f7440', 225), 's': hexc('#7fa45e', 240)}
AIR = hexc('#1c2326', 150)                 # the empty glass: dark, see-through
GLASS_HI, GLASS_RIM = hexc('#c4dcd6', 200), hexc('#5d7470', 230)
CRACK, CRACK_D = hexc('#e6f2ee'), hexc('#2a3532')
BR = {'d': hexc('#6e4e56'), 'b': hexc('#a07c84'), 'l': hexc('#c9a2a8'), 'k': hexc('#4a3038')}   # brain: shade / base / lit / sulci
EYE_W, EYE_I, EYE_P, EYE_G, LID = hexc('#e8e0c8'), hexc('#c06bff'), hexc('#2a1036'), hexc('#ffffff'), hexc('#5a3a44')
PSI = hexc('#c06bff', 70)

GX0, GX1, GY0, GY1 = 10, 53, 14, 80        # glass interior
LEVELS = {'full': 17, 'crack1': 34, 'crack2': 52, 'crack3': 68, 'drained': 81, 'awake': 81}
EYES = [(-5, 0, 3.4), (6, -3, 2.4), (2, 5, 2.0), (-10, 4, 1.6), (9, 4, 1.6)]
CRACKS = [(14, 46, [(-1, -6), (3, -9), (2, 5), (5, 8), (-2, 9)]), (49, 30, [(-4, -5), (-3, 6), (2, 7), (-7, 2)]), (47, 62, [(-3, -7), (3, -4), (-5, 3), (1, 6), (-2, 8)])]

def px(im, x, y, c):
    if 0 <= x < W and 0 <= y < H:
        if c[3] == 255: im.putpixel((x, y), c)
        else: im.alpha_composite(Image.new('RGBA', (1, 1), c), (x, y))

def tint(c, k, g):                         # under the liquid: darker and greener
    return (int(c[0] * k), int(min(255, c[1] * k + g)), int(c[2] * k * 0.9), c[3])

def brain(im, cx, cy, level, eyes_open, rng):
    rx, ry = 13, 10
    for y in range(cy - ry - 1, cy + ry + 2):
        for x in range(cx - rx - 1, cx + rx + 2):
            u, v = (x - cx + 0.5) / rx, (y - cy + 0.5) / ry
            d = u * u + v * v
            if d > 1: continue
            c = BR['l'] if (u < -0.15 and v < -0.2 and d < 0.75) else BR['d'] if (u > 0.45 or v > 0.55) else BR['b']
            # folds: a wavy dark line pattern, the midline split
            w = math.sin((x - cx) * 0.62 + math.sin((y - cy) * 0.55) * 2.6) * math.cos((y - cy) * 0.5)
            if abs(x - cx) < 1 and v < 0.6: c = BR['k']
            elif abs(w) < 0.13 and 0.12 < d < 0.82: c = BR['k']
            if y >= level: c = tint(c, 0.78, 10)
            px(im, x, y, c)
    for dx, dy, r in EYES:                 # eyes sunk in the folds
        ex, ey = cx + dx, cy + dy
        for y in range(int(ey - r - 1), int(ey + r + 2)):
            for x in range(int(ex - r - 1), int(ex + r + 2)):
                d = math.hypot(x + 0.5 - ex, y + 0.5 - ey)
                if d > r + 0.5: continue
                if not eyes_open:
                    if abs(y + 0.5 - ey) < 0.6 and d < r: px(im, x, y, BR['k'])     # a shut eye: one dark slit
                    continue
                elif d > r - 0.4: c = BR['k']
                elif d < r * 0.35: c = EYE_P
                elif d < r * 0.75: c = EYE_I
                else: c = EYE_W
                if y >= level: c = tint(c, 0.85, 8)
                px(im, x, y, c)
        if eyes_open and r >= 1.5: px(im, int(ex - r * 0.4), int(ey - r * 0.4), EYE_G)
    for k in range(14):                    # the stem, swaying down
        x = cx + 1 + int(round(math.sin(k * 0.6) * 1.5)); y = cy + ry - 1 + k
        if y > GY1 - 1: break
        for dx in (0, 1): px(im, x + dx, y, tint(BR['d'], 0.78, 10) if y >= level else BR['d'])

def vat(state, frame=0):
    rng = random.Random(7 + frame)
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    level = LEVELS[state]
    # glass interior: air above, liquid below
    for y in range(GY0, GY1):
        for x in range(GX0, GX1 + 1):
            if y < level: c = AIR
            elif y == level: c = LIQ['s']
            else: c = LIQ['d'] if x > GX1 - 6 else LIQ['l'] if x < GX0 + 5 else LIQ['b']
            px(im, x, y, c)
    if level < GY1:                         # bubbles rising
        for i in range(10):
            bx = rng.randint(GX0 + 3, GX1 - 3); by = rng.randint(level + 2, GY1 - 2)
            px(im, bx, by, LIQ['s'])
    awake = state in ('full', 'crack1', 'crack2', 'crack3', 'awake')
    cy = 44 if level < 30 else min(GY1 - 9, level + 2)   # half under the surface as it drops
    if state in ('drained', 'awake'): cy = GY1 - 9   # slumped on the floor of the vat
    brain(im, 32, cy, level, awake, rng)
    # glass: the lit left edge, the darker right rim, a reflection streak
    for y in range(GY0, GY1):
        px(im, GX0, y, GLASS_RIM); px(im, GX1, y, GLASS_RIM)
        px(im, GX0 + 2, y, GLASS_HI if y % 9 not in (0, 1) else GLASS_RIM)
        if 22 < y < 60: px(im, GX0 + 4, y, hexc('#c4dcd6', 90))
        px(im, GX1 - 2, y, hexc('#c4dcd6', 70))
    # cracks, one per stage, with liquid running out of each down the glass
    n = {'crack1': 1, 'crack2': 2, 'crack3': 3, 'drained': 3, 'awake': 3}.get(state, 0)
    for (sx, sy, legs) in CRACKS[:n]:
        for (lx, ly) in legs:
            steps = max(abs(lx), abs(ly))
            for t in range(steps + 1):
                x = sx + round(lx * t / steps); y = sy + round(ly * t / steps)
                px(im, x, y, CRACK); px(im, x + 1, y + 1, CRACK_D)
        if level <= sy and state != 'drained' and state != 'awake':
            for y in range(sy + 2, GY1 + 2): px(im, sx + (1 if sx > 32 else -1), y, LIQ['l'])
    # top cap with cables, base with bolts and pipes
    for y in range(6, GY0):
        for x in range(GX0 - 3, GX1 + 4):
            c = STEEL[4] if y == 6 else STEEL[3] if x < 20 else STEEL[1] if x > 46 else STEEL[2]
            if y == GY0 - 1: c = STEEL[0]
            px(im, x, y, c)
    for cx0, side in ((20, -1), (31, 0), (44, 1)):          # hoses out of the lid, curving off to the sides
        for y in range(0, 6):
            x = cx0 + side * (5 - y) // 2
            px(im, x, y, STEEL[1]); px(im, x + 1, y, STEEL[0])
    for y in range(GY1, H - 1):
        x0 = GX0 - 4 - (y - GY1) // 4; x1 = GX1 + 4 + (y - GY1) // 4
        for x in range(x0, x1 + 1):
            c = STEEL[3] if x < x0 + 6 else STEEL[1] if x > x1 - 8 else STEEL[2]
            if y == GY1: c = STEEL[4]
            if y > H - 4: c = STEEL[0]
            if (x - x0) % 9 == 4 and y == GY1 + 3: c = STEEL[4]   # bolts
            if y in (GY1 + 7, GY1 + 8) and (x0 + 10 < x < x0 + 16): c = RUST
            px(im, x, y, c)
    for y in range(GY1 + 4, GY1 + 8):        # pipes off the base
        for x in range(0, GX0 - 5): px(im, x, y, STEEL[2] if y == GY1 + 5 else STEEL[1])
        for x in range(GX1 + 6, W): px(im, x, y, STEEL[2] if y == GY1 + 5 else STEEL[1])
    if state in ('drained', 'awake', 'crack3'):    # the spill on the floor in front
        for x in range(GX0 - 6, GX1 + 4):
            if rng.random() < 0.8: px(im, x, H - 1, LIQ['b'])
    # outline the silhouette
    a = im.copy().load(); o = im.load()
    for y in range(H):
        for x in range(W):
            if a[x, y][3] == 0 and any(0 <= x + dx < W and 0 <= y + dy < H and a[x + dx, y + dy][3] > 100 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                o[x, y] = OUT
    if state == 'awake':                      # the psi stirring round the eyes
        for i in range(14):
            ang = rng.random() * math.tau; r = rng.uniform(8, 17)
            px(im, int(32 + math.cos(ang) * r * 1.3), int(GY1 - 9 + math.sin(ang) * r * 0.7), PSI if i % 3 else hexc('#c06bff', 170))
    return im

STATES = ['full', 'crack1', 'crack2', 'crack3', 'drained', 'awake']
if __name__ == '__main__':
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(os.path.abspath(__file__)), '_out')
    os.makedirs(out, exist_ok=True)
    strip = Image.new('RGBA', (W * len(STATES), H))
    for i, s in enumerate(STATES): strip.alpha_composite(vat(s), (i * W, 0))
    strip.save(os.path.join(out, 'boss_brain_vat_strip.png'))
    print('wrote', os.path.join(out, 'boss_brain_vat_strip.png'))
