"""The brain boss's attack effects, drawn by hand in the model's palette (Yan, 2026-11-08: "re-do them ... make the brain effects be on par with the model's quality, and it will need the
variations"). Four VARIATIONS, one per wound phase (the vat's phases 0-3: the psi gets angrier as the brain is hurt - violet, violet with flesh-pink, magenta with the brain's own vein red, then
blood-hot with a white-yellow core), and every effect is ANIMATED (4 frames):
    orbs   16x16 x 8 frames  ring orb 0-3 (a pale psi sphere with a swirl inside), void orb 4-7 (a dark core with an accretion ring)          (ppu 20)
    bolt   28x10 x 4         the aimed shot: a hot head and a flickering tail of wisps, the head at x=21                                      (ppu 16, pivot 0.75)
    ring   64x64 x 4         the pulse's wave front, scaled to its radius: a ragged bright rim with motes, a soft trailing skirt              (ppu 32)
    beam   32x12 x 4         the laser: one tile of beam (core, jagged hot edge, glow) that tiles end to end                                 (ppu 32)
    beamcap 16x12 x 4        the flare at the beam's start                                                                                     (ppu 32)
    glow   32x32 x 4         the gathering at the brain while it concentrates: a dithered halo with motes orbiting                              (ppu 20)
The palette is the picture's (ArtSource/refs/brain/yan_brain_vat_ref.png): the iris violet #c06bff, the vein red #b04a52, the brain's pinks. Drawn with PIL and numpy, no generated images.
    python ArtSource/bosses/make_brain_fx.py [out_dir] [game]     ('game' also writes Assets/Resources/Sprites/Fx/boss_brain_*_p0..p3.png)
"""
import sys, os, math
import numpy as np
from PIL import Image


def hexc(h, a=255):
    h = h.lstrip('#'); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


# one palette per phase: core (white-hot), hot, mid, rim (the iris colour, escalating), dark (the shadow side), vein (the brain's flesh showing through), spark
PAL = [
    dict(core='#ffffff', hot='#f6ecff', mid='#d8a8ff', rim='#c06bff', dark='#4a1a6a', deep='#1a0a28', vein='#9a7ad8', spark='#f0e0ff'),
    dict(core='#ffffff', hot='#fdeaff', mid='#e0a0f0', rim='#c860e8', dark='#5a1a70', deep='#1e0a2a', vein='#e58fb8', spark='#ffe0f4'),
    dict(core='#fff4f4', hot='#ffd8e8', mid='#f08ac8', rim='#d04a9a', dark='#661a52', deep='#240a1c', vein='#b04a52', spark='#ffc8d8'),
    dict(core='#fffbe8', hot='#ffe0b8', mid='#ff8f8f', rim='#d8344a', dark='#6a1224', deep='#2a060e', vein='#ff6a5a', spark='#ffe8c0'),
]
PHASES = len(PAL)


def col(p, k, a=255): return hexc(PAL[p][k], a)


class Canvas:
    def __init__(self, w, h): self.w, self.h = w, h; self.a = np.zeros((h, w, 4), np.uint8)
    def put(self, x, y, c, over=True):
        x, y = int(round(x)), int(round(y))
        if 0 <= x < self.w and 0 <= y < self.h:
            if over or self.a[y, x, 3] == 0: self.a[y, x] = c
    def get(self, x, y): return tuple(self.a[y, x]) if 0 <= x < self.w and 0 <= y < self.h else (0, 0, 0, 0)
    def img(self): return Image.fromarray(self.a)


def noise2(seed, w, h, scale):
    """a smooth value noise 0..1 (bilinear over a coarse grid), deterministic"""
    r = np.random.RandomState(seed); gw, gh = int(w / scale) + 3, int(h / scale) + 3; g = r.rand(gh, gw)
    ys, xs = np.mgrid[0:h, 0:w]; fx, fy = xs / scale, ys / scale; x0, y0 = fx.astype(int), fy.astype(int); tx, ty = fx - x0, fy - y0
    tx, ty = tx * tx * (3 - 2 * tx), ty * ty * (3 - 2 * ty)
    return (g[y0, x0] * (1 - tx) * (1 - ty) + g[y0, x0 + 1] * tx * (1 - ty) + g[y0 + 1, x0] * (1 - tx) * ty + g[y0 + 1, x0 + 1] * tx * ty)


BAYER = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16.0


def dither(x, y, v): return v > BAYER[y % 4, x % 4]


# ---------------------------------------------------------------- orbs
def orb(p, frame, void):
    """16x16. Ring orb: a pale psi sphere lit from the top left, a darker rim on the far side, a spiral wisp turning inside (a quarter turn a frame), a few motes round it, the vein colour in the
    shadow. Void orb: a near-black core, a bright thin rim that thickens on the lit side, an accretion ring of dots turning round it, and wisps being pulled in."""
    C = Canvas(16, 16); cx = cy = 7.5; R = 5.6 if not void else 5.0; ang0 = frame * math.pi / 2
    for y in range(16):
        for x in range(16):
            dx, dy = x - cx, y - cy; d = math.hypot(dx, dy)
            if d > R + 0.4: continue
            nx, ny = dx / R, dy / R; nz = math.sqrt(max(0.0, 1 - nx * nx - ny * ny)); lit = max(0.0, -0.55 * nx - 0.6 * ny + 0.58 * nz)             # light from the top left
            swirl = math.sin(3 * math.atan2(dy, dx) - 2.2 * d + ang0 * (1 if not void else -1)) * 0.5 + 0.5
            if not void:
                v = 0.30 + 0.62 * lit + 0.22 * swirl * (1 - d / R)
                if d > R - 0.9: c = col(p, 'dark') if lit < 0.45 else col(p, 'rim')
                elif v > 0.98: c = col(p, 'core')
                elif v > 0.82: c = col(p, 'hot')
                elif v > 0.58: c = col(p, 'mid')
                elif v > 0.40: c = col(p, 'rim')
                else: c = col(p, 'vein') if (p >= 1 and swirl > 0.72) else col(p, 'dark')
            else:
                rim = d > R - 1.1
                if rim: c = col(p, 'core') if lit > 0.6 else col(p, 'rim') if lit > 0.2 else col(p, 'dark')
                elif d > R - 2.1: c = col(p, 'dark') if swirl > 0.45 else col(p, 'deep')
                else: c = col(p, 'deep')
            C.put(x, y, c)
    if void:                                                                                      # the accretion ring: dots on a tilted ellipse, turning
        for k in range(8):
            a = ang0 + k * math.pi / 4; ex, ey = cx + math.cos(a) * 7.0, cy + math.sin(a) * 2.6 - 0.5
            if 0 <= ex < 16 and 0 <= ey < 16: C.put(ex, ey, col(p, 'mid') if k % 2 == 0 else col(p, 'rim'))
    else:                                                                                         # motes orbiting outside the sphere
        for k in range(3):
            a = ang0 + k * 2.09; ex, ey = cx + math.cos(a) * 7.3, cy + math.sin(a) * 6.8
            if 0 <= ex < 16 and 0 <= ey < 16: C.put(ex, ey, col(p, 'spark'))
    # a soft pixel glow round the outside (one ring of translucent pixels), stronger on the lit side
    for y in range(16):
        for x in range(16):
            if C.get(x, y)[3]: continue
            d = math.hypot(x - cx, y - cy)
            if R + 0.4 <= d < R + 1.7 and dither(x, y, 0.55 if not void else 0.4): C.put(x, y, col(p, 'rim', 120))
    return C.img()


def orb_sheet(p):
    sheet = Image.new('RGBA', (16 * 8, 16))
    for i in range(8): sheet.alpha_composite(orb(p, i % 4, i >= 4), (i * 16, 0))
    return sheet


# ---------------------------------------------------------------- bolt
def bolt(p, frame):
    """28x10, flying right, the head at x=21: a hot teardrop head, the tail a flickering stream of wisps that change every frame (so the shot crackles), a dark outline under the bright parts
    for the brain's heavy look, and sparks thrown off the sides."""
    C = Canvas(28, 10); r = np.random.RandomState(100 + p * 7 + frame); cy = 4.5
    for x in range(28):
        u = (x - 21) / 7.0 if x > 21 else (x - 21) / 21.0                                          # <0 on the tail, >0 beyond the head
        half = 3.3 * math.sqrt(max(0.0, 1 - u * u)) if u > 0 else 3.3 * (1 + u) ** 0.8 * (0.8 + 0.2 * math.sin(x * 0.9 + frame * 1.7))
        for y in range(10):
            dy = abs(y - cy); wob = math.sin(x * 0.55 + frame * 1.9) * 0.9 * max(0.0, -u)                # the tail waves
            dd = abs(y - cy - wob)
            if dd > half + 0.3 or half <= 0.2: continue
            e = dd / max(0.4, half)
            if u > -0.12 and e < 0.35 and x >= 18: c = col(p, 'core')
            elif e < 0.55 and u > -0.5: c = col(p, 'hot')
            elif e < 0.8: c = col(p, 'mid')
            else: c = col(p, 'rim')
            C.put(x, y, c)
    for x in range(28):                                                                            # the dark under-edge: one pixel of 'dark' under the lowest bright pixel of each column
        ys = [y for y in range(10) if C.get(x, y)[3]]
        if ys: yb = max(ys); C.put(x, yb + 1, col(p, 'dark', 200), over=False)
    for k in range(6):                                                                             # sparks and loose wisps behind the head
        sx = 1 + r.randint(0, 17); sy = int(round(cy + r.uniform(-4.2, 4.2)))
        if C.get(sx, sy)[3] == 0: C.put(sx, sy, col(p, 'spark') if k % 2 == 0 else col(p, 'rim', 200))
    return C.img()


def bolt_sheet(p):
    sheet = Image.new('RGBA', (28 * 4, 10))
    for i in range(4): sheet.alpha_composite(bolt(p, i), (i * 28, 0))
    return sheet


# ---------------------------------------------------------------- ring (the pulse's wave front)
def ring(p, frame):
    """64x64, centred; scaled by the pulse's radius (the sprite is two tiles across, so the scale is the radius). A ragged bright rim whose raggedness changes every frame, a bright inner lip,
    a long soft skirt behind it (dithered), and psi motes riding the rim. Light sits slightly more on the upper part of the rim (it is a wave seen from above at an angle)."""
    C = Canvas(64, 64); c0 = 31.5; r = np.random.RandomState(300 + p * 11 + frame); nz = noise2(400 + p * 5 + frame, 64, 64, 5.0)
    for y in range(64):
        for x in range(64):
            dx, dy = x - c0, y - c0; d = math.hypot(dx, dy); a = math.atan2(dy, dx)
            rag = (nz[y, x] - 0.5) * 3.4 + 1.1 * math.sin(a * 9 + frame * 1.6)                  # the rim wobbles
            e = d - (28.2 + rag * 0.55)
            up = 0.5 + 0.5 * math.sin(a - math.pi / 2 * 0 + math.pi * 0.0)                        # brighter on the upper rim
            if -1.2 <= e < 0.9: c = col(p, 'core') if e < -0.1 and up > 0.35 else col(p, 'hot')
            elif -2.6 <= e < -1.2: c = col(p, 'mid')
            elif -4.4 <= e < -2.6: c = col(p, 'rim') if dither(x, y, 0.8) else col(p, 'mid', 140)
            elif 0.9 <= e < 2.0: c = col(p, 'rim', 150) if dither(x, y, 0.65) else None
            elif -12 <= e < -4.4:
                k = (e + 12) / 7.6
                c = col(p, 'rim', 70) if dither(x, y, k * 0.55) and nz[y, x] > 0.42 else (col(p, 'dark', 90) if dither(x + 1, y + 2, k * 0.25) else None)
            else: c = None
            if c: C.put(x, y, c)
    for k in range(10):                                                                             # motes riding the rim, a different set each frame
        a = r.uniform(0, 2 * math.pi); rr = 28.5 + r.uniform(-1.5, 3.5); x, y = c0 + math.cos(a) * rr, c0 + math.sin(a) * rr
        C.put(x, y, col(p, 'spark')); C.put(x + 1, y, col(p, 'rim', 200))
    return C.img()


def ring_sheet(p):
    sheet = Image.new('RGBA', (64 * 4, 64))
    for i in range(4): sheet.alpha_composite(ring(p, i), (i * 64, 0))
    return sheet


# ---------------------------------------------------------------- beam
def beam(p, frame):
    """32x12, one tile of the laser, tiling end to end (the profile at x=0 and x=31 is the same): a white core, a hot band, a jagged outer band that flickers (a different jag each frame, wrapped so it
    tiles), a dark under-edge, and a halo of dithered glow."""
    C = Canvas(32, 12); cy = 5.5; r = np.random.RandomState(500 + p * 13 + frame)
    jag = [r.uniform(-0.9, 0.9) for _ in range(32 // 2)]; jag = jag + jag[:1]
    for x in range(32):
        j = jag[x // 2] * (1 if x % 2 == 0 else 0.6)
        for y in range(12):
            dy = abs(y - cy)
            half_core, half_hot, half_mid, half_rim = 0.9, 1.8, 2.7 + j * 0.6, 3.6 + j
            if dy < half_core: c = col(p, 'core')
            elif dy < half_hot: c = col(p, 'hot')
            elif dy < half_mid: c = col(p, 'mid')
            elif dy < half_rim: c = col(p, 'rim')
            elif dy < half_rim + 1.5 and dither(x, y, 0.5): c = col(p, 'rim', 110)
            else: continue
            C.put(x, y, c)
        yb = int(cy + 3.6 + j + 0.5); C.put(x, yb, col(p, 'dark', 180), over=False)
    for k in range(4):                                                                              # little arcs of psi off the edge, one tile's worth
        x = r.randint(2, 30); y = int(cy + (4.6 if k % 2 == 0 else -4.6)); C.put(x, y, col(p, 'spark')); C.put(x + 1, y + (1 if k % 2 == 0 else -1), col(p, 'rim', 200))
    return C.img()


def beam_cap(p, frame):
    """16x12 at the start of the beam (drawn once at the brain): a flare, a ring of sparks, a hot centre."""
    C = Canvas(16, 12); r = np.random.RandomState(600 + p * 3 + frame); cx, cy = 8.0, 5.5
    for y in range(12):
        for x in range(16):
            d = math.hypot((x - cx) * 0.75, y - cy)
            if d < 1.6: c = col(p, 'core')
            elif d < 2.8: c = col(p, 'hot')
            elif d < 4.0: c = col(p, 'mid')
            elif d < 5.3 and dither(x, y, 0.7): c = col(p, 'rim')
            else: continue
            C.put(x, y, c)
    for k in range(6):
        a = r.uniform(0, 2 * math.pi); C.put(cx + math.cos(a) * 6.5, cy + math.sin(a) * 5.0, col(p, 'spark'))
    return C.img()


def beam_sheet(p):
    sheet = Image.new('RGBA', (32 * 4, 12))
    for i in range(4): sheet.alpha_composite(beam(p, i), (i * 32, 0))
    return sheet


def cap_sheet(p):
    sheet = Image.new('RGBA', (16 * 4, 12))
    for i in range(4): sheet.alpha_composite(beam_cap(p, i), (i * 16, 0))
    return sheet


# ---------------------------------------------------------------- glow (gathering)
def glow(p, frame):
    """32x32: the brain concentrating - a dithered halo that breathes (radius changes per frame), a brighter inner ring, and motes drawn in toward the centre."""
    C = Canvas(32, 32); c0 = 15.5; r = np.random.RandomState(700 + p * 17 + frame); breathe = 1 + 0.12 * math.sin(frame * math.pi / 2)
    for y in range(32):
        for x in range(32):
            d = math.hypot(x - c0, y - c0) / (14.0 * breathe)
            if d >= 1: continue
            k = 1 - d
            if k > 0.72: c = col(p, 'hot', 90)
            elif dither(x, y, k * 0.9) and k > 0.35: c = col(p, 'mid', 120)
            elif dither(x + 2, y + 1, k * 0.8): c = col(p, 'rim', 90)
            else: continue
            C.put(x, y, c)
    for k in range(8):                                                                              # motes spiralling in: each a bit nearer every frame
        a = k * math.pi / 4 + 0.35 * k; rr = 14.0 - ((frame + k % 4) % 4) * 3.0
        C.put(c0 + math.cos(a) * rr, c0 + math.sin(a) * rr, col(p, 'spark')); C.put(c0 + math.cos(a) * (rr + 1), c0 + math.sin(a) * (rr + 1), col(p, 'rim', 160))
    return C.img()


def glow_sheet(p):
    sheet = Image.new('RGBA', (32 * 4, 32))
    for i in range(4): sheet.alpha_composite(glow(p, i), (i * 32, 0))
    return sheet


MAKERS = [('orbs', orb_sheet), ('bolt', bolt_sheet), ('ring', ring_sheet), ('beam', beam_sheet), ('beamcap', cap_sheet), ('glow', glow_sheet)]


def main(out, game=False):
    os.makedirs(out, exist_ok=True)
    for name, fn in MAKERS:
        for p in range(PHASES):
            im = fn(p); im.save(os.path.join(out, f'boss_brain_{name}_p{p}.png'))
            if game:
                gd = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Assets', 'Resources', 'Sprites', 'Fx'); im.save(os.path.join(gd, f'boss_brain_{name}_p{p}.png'))
    # a review sheet: every effect, every phase, every frame, at 4x on the dark of the vat's backdrop
    S = 4; rows = []
    for name, fn in MAKERS:
        for p in range(PHASES): rows.append((f'{name} p{p}', fn(p)))
    W = max(im.width for _, im in rows) * S + 8; H = sum(im.height * S + 6 for _, im in rows) + 6
    sheet = Image.new('RGBA', (W, H), (20, 22, 23, 255)); y = 4
    for _, im in rows:
        sheet.alpha_composite(im.resize((im.width * S, im.height * S), Image.NEAREST), (4, y)); y += im.height * S + 6
    sheet.save(os.path.join(out, 'brain_fx_review.png')); print('wrote', out, len(rows), 'sheets')


if __name__ == '__main__':
    main(sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(os.path.abspath(__file__)), '_out_fx'), len(sys.argv) > 2 and sys.argv[2] == 'game')
