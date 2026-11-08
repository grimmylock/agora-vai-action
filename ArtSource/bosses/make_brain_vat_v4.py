"""The brain in the vat, v4: every state rebuilt from Yan's reworked picture (ArtSource/refs/brain/yan_brain_vat_ref.png, split into layers by brain_ref_layers.py).
Same sheet as make_brain_vat.py (so the game code does not change): 8 columns x 11 rows of 128x160 (ppu 32, 4x5 tiles, the base on the tile):
    0-3 phase 0-3 eyes open (the brain bobs, bubbles rise, the monitor's trace runs)   4-7 phase 0-3 eyes shut (concentrating; twitches on odd frames)
    8 drained   9 awake (floats up off the floor)   10 the eye (a giant eye on a cord slides out from behind it and turns to look)
What is his: the vat, lid, base, pump, rack, cables, the liquid, the brain, its cord and its two eyes (the pixels of the picture, scaled down and snapped to one palette).
What is added to make the states: the liquid level dropping per phase, cracks, more and bigger eyes (his two eyes, cut out of the picture, scaled and pasted again), shut eyes (the brain's own
texture copied over them, a dark lid line), the slumped drained brain, the animated monitor trace and bubbles, the electrode wires, the giant eye, the psi glow.
    python ArtSource/bosses/make_brain_vat_v4.py [out_dir] [game]     - 'game' also writes Assets/Resources/Sprites/Mutants/boss_brain_vat.png
"""
import sys, os, math, random
import numpy as np
from PIL import Image
from scipy import ndimage as ndi
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import brain_ref_layers as L

W, H, COLS = 128, 160, 8
BX0, BY0, BX1, BY1 = L.BOX
S = min(H / (BY1 - BY0), W / (BX1 - BX0)) * 0.998 * 0.935   # picture -> frame scale (0.468 x 0.935: a little room under the base for the floor pad that seats it, 2026-11-08)
OFFX, OFFY = (W - (BX1 - BX0) * S) / 2.0, 0.0                # centred, the picture's top at the frame's top; the base's foot ~11 pixels above the bottom (the pad goes there)
EYE_REF = [((173.5, 175.0), 9.0), ((209.0, 166.0), 7.0)]     # his two eyes in the picture: centre, radius (measured)


def fx(x): return (x - BX0) * S + OFFX
def fy(y): return (y - BY0) * S + OFFY
def hexc(h, a=255): h = h.lstrip('#'); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


OUT = hexc('#141617')
AIR = hexc('#16201f', 150)
LIQ_S = hexc('#8fd09a', 235)
CRACK, CRACK_D = hexc('#e6f2ee'), hexc('#2a3532')
BRK = hexc('#4a2c2e')
EYE_G = hexc('#ffffff')
CORD_L, CORD, CORD_D = hexc('#b07c84'), hexc('#8a5a62'), hexc('#4a2c2e')
EYE_I, VEIN, PSI = hexc('#c06bff'), hexc('#b04a52'), hexc('#c06bff', 70)
SCREEN, TRACE = hexc('#0c1e16'), hexc('#7fe08a')
WIRE_O, WIRE_M, WIRE_H = hexc('#2a1c1a'), hexc('#7a4a40'), hexc('#a8705e')


def px(im, x, y, c):
    if 0 <= x < W and 0 <= y < H:
        if c[3] == 255: im.putpixel((x, y), c)
        else: im.alpha_composite(Image.new('RGBA', (1, 1), c), (x, y))


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


def scale_rgba(rgb, alpha, factor):
    """Area scaling (premultiplied, so no dark fringes) of an RGB array with an alpha mask; the result's alpha is thresholded (pixel-art edges)."""
    h, w = rgb.shape[:2]; al = alpha.astype(float) / (255.0 if alpha.dtype != bool and alpha.max() > 1 else 1.0)
    pm = np.dstack([rgb[:, :, c] * al for c in range(3)] + [al * 255.0])
    src = Image.fromarray(np.clip(pm, 0, 255).astype(np.uint8), 'RGBA')
    nw, nh = max(1, int(round(w * factor))), max(1, int(round(h * factor)))
    sm = np.array(src.resize((nw, nh), Image.BOX if factor < 1 else Image.BICUBIC)).astype(float)
    out = np.zeros((nh, nw, 4), np.uint8); a = sm[:, :, 3] / 255.0
    for c in range(3): out[:, :, c] = np.clip(np.where(a > 0.01, sm[:, :, c] / np.maximum(a, 0.01), 0), 0, 255)
    out[:, :, 3] = np.where(a >= 0.5, 255, 0)
    return Image.fromarray(out, 'RGBA')


class Art:
    def __init__(self):
        a, alpha, inner, keep, liq, bm, wires = L.layers()
        self.a, self.bm = a, bm
        gx0, gy0, gx1, gy1 = L.GLASS
        self.GX0, self.GX1 = int(round(fx(gx0 + L.EDGE_L))), int(round(fx(gx1 - L.EDGE_R)))
        self.GY0, self.GY1 = int(round(fy(gy0))), int(round(fy(gy1)))
        self.CX = int(round(fx((gx0 + gx1) / 2)))
        def full(rgb, al):
            im = Image.new('RGBA', (W, H), (0, 0, 0, 0)); sc = scale_rgba(rgb[BY0:BY1, BX0:BX1], al[BY0:BY1, BX0:BX1], S)
            ox, oy = int(round(OFFX)), int(round(OFFY))
            if oy < 0: sc = sc.crop((0, -oy, sc.width, sc.height)); oy = 0
            im.alpha_composite(sc, (ox, oy)); return im
        self.shell = full(a, np.where(alpha & ~inner, 255, 0)); self.liquid = full(liq, np.where(inner, 255, 0)); self.brain_full = full(a, np.where(bm, 255, 0))
        # one palette for everything: median cut over the opaque pixels of the shell, the liquid and the brain
        px_list = np.concatenate([np.array(l).reshape(-1, 4)[np.array(l).reshape(-1, 4)[:, 3] > 0][:, :3] for l in (self.shell, self.liquid, self.brain_full)])
        strip = Image.fromarray(px_list.reshape(1, -1, 3).astype(np.uint8), 'RGB')
        self.pal = np.array(strip.quantize(colors=64, method=Image.MEDIANCUT, dither=Image.NONE).getpalette()[:64 * 3], int).reshape(-1, 3)
        self.pal = np.vstack([self.pal, np.array([(138, 58, 208), (192, 107, 255), (42, 16, 54), (232, 228, 240), (255, 255, 255), (154, 58, 58), (12, 4, 18)], int)])      # the eyes' colours always in the palette (few pixels, would be lost)
        for l in ('shell', 'liquid', 'brain_full'): setattr(self, l, self.snap(getattr(self, l)))
        ys, xs = np.where(bm)
        self.bx0, self.bx1, self.by0 = fx(xs.min()), fx(xs.max()), fy(ys.min()); self.cord_top = fy(212); self.by1 = fy(ys.max()); self.bcx = (self.bx0 + self.bx1) / 2
        self.bh = self.cord_top - self.by0                                    # the brain's body height (frame px); the cord hangs below it
        # his two eyes, cut from the picture as discs (RGBA), for pasting at any size
        self.eye_src = []
        for (ex, ey), er in EYE_REF:
            R = int(er + 2.5); patch = a[int(ey) - R:int(ey) + R + 1, int(ex) - R:int(ex) + R + 1]
            yy, xx = np.mgrid[-R:R + 1, -R:R + 1]; m = np.hypot(xx - (ex - int(ex)), yy - (ey - int(ey))) <= er + 1.0
            self.eye_src.append((patch, m, er))

    def snap(self, im):
        arr = np.array(im); rgb = arr[:, :, :3].reshape(-1, 3).astype(int)
        idx = ((rgb[:, None, :] - self.pal[None, :, :]) ** 2).sum(2).argmin(1)
        out = arr.copy(); out[:, :, :3] = self.pal[idx].reshape(arr.shape[:2] + (3,)).astype(np.uint8)
        return Image.fromarray(out, 'RGBA')

    def eye(self, which, r):
        """His eye `which` (0 = the big one, 1 = the small one) scaled so its radius is r frame px: an RGBA patch and its centre offset."""
        patch, m, er = self.eye_src[which]
        im = self.snap(scale_rgba(patch, m, r / er)); cx, cy = im.width / 2.0, im.height / 2.0
        ioff = ((-1.3, 1.1), (0.2, 0.3))[which]; ix, iy = cx + ioff[0] / er * r, cy + ioff[1] / er * r      # his iris sits slightly off the eye's centre
        def iris(d, x, y):
            return hexc('#2a1036') if d < r * 0.27 else hexc('#c06bff') if d > r * 0.46 else hexc('#8a3ad0')
        disc(im, ix, iy, r * 0.55, iris)                                       # (the downscale loses the few iris pixels: drawn again, in his colours)
        px(im, int(ix - r * 0.25), int(iy - r * 0.3), EYE_G)
        return im, cx, cy


A = None
LEVEL_FRAC = (0.04, 0.30, 0.58, 0.82)
EXTRA = [(-9, 5, 2.7, 0), (8, 6, 2.4, 1), (-2, -9, 2.2, 1), (-16, -3, 2.0, 1)]        # the 3rd..6th eyes: dx, dy from the brain's centre, radius (frame px), which of his eyes to copy
EYE_COUNT = [2, 3, 4, 6]


def brain_sprite(shut, eyes_n, scale, cord_px, squash=1.0):
    """The brain (RGBA crop, the top of its mask at row 3) with his two eyes (open, grown by `scale`, or shut), more eyes added, the cord kept `cord_px` long."""
    bx0, by0 = int(A.bx0) - 3, int(A.by0) - 3
    crop = A.brain_full.crop((bx0, by0, int(A.bx1) + 4, int(A.by1) + 3)).copy()
    arr = np.array(crop); cut = int(A.cord_top - by0) + max(0, int(cord_px)); arr[cut:, :, 3] = 0; crop = Image.fromarray(arr, 'RGBA')
    spots = [(fx(ex) - bx0, fy(ey) - by0, er * S) for (ex, ey), er in EYE_REF]
    base = np.array(crop); h, w = base.shape[:2]; cx0 = A.bcx - bx0; cy0 = (A.by0 + A.cord_top) / 2 - by0
    for ex, ey, er in spots:                                                  # wipe his eyes with the brain's own texture, copied from beside them
        rr = er + 1.2
        for y in range(int(ey - rr - 1), int(ey + rr + 2)):
            for x in range(int(ex - rr - 1), int(ex + rr + 2)):
                if 0 <= x < w and 0 <= y < h and math.hypot(x + 0.5 - ex, y + 0.5 - ey) <= rr and base[y, x, 3]:
                    for ddy, ddx in ((int(rr * 2.0), 0), (-int(rr * 2.0), 0), (0, int(rr * 2.0)), (0, -int(rr * 2.0))):
                        sy, sx = y + ddy, x + ddx
                        if 0 <= sx < w and 0 <= sy < h and base[sy, sx, 3] and min(math.hypot(sx + 0.5 - e[0], sy + 0.5 - e[1]) - e[2] for e in spots) > 1.5 and sy < cut and sy < int(A.cord_top - by0) - 1:
                            base[y, x] = base[sy, sx]; break
                    else: base[y, x, :3] = (150, 100, 104)
    crop = Image.fromarray(base, 'RGBA')
    placed = [(spots[i][0], spots[i][1], spots[i][2] * scale, i) for i in range(2)][:max(eyes_n, 2)]
    for dx, dy, r, which in EXTRA[:max(0, eyes_n - 2)]: placed.append((cx0 + dx, cy0 - A.bh * 0.12 + dy, r * (0.9 + 0.1 * scale), which))
    for ex, ey, er, which in placed:
        if shut:
            for x in range(int(ex - er * 0.9), int(ex + er * 0.9) + 1): px(crop, x, int(ey), BRK)
            px(crop, int(ex - er * 0.9) - 1, int(ey) - 1, BRK); px(crop, int(ex + er * 0.9) + 1, int(ey) - 1, BRK)
        else:
            p, pcx, pcy = A.eye(which, er); crop.alpha_composite(p, (int(round(ex - pcx)), int(round(ey - pcy))))
    if squash != 1.0: crop = crop.resize((crop.width, max(4, int(round(crop.height * squash)))), Image.NEAREST)
    return crop, cx0


def wire(im, x0, y0, x1, y1, seed, wet_from):
    """An electrode cable: a two-pixel dark-brown cord with a lit edge, wavering a little on its way down."""
    n = max(abs(x1 - x0), abs(y1 - y0), 1) * 2
    for i in range(n + 1):
        s = i / n; x = x0 + (x1 - x0) * s + math.sin(s * 5 + seed) * 2.0; y = y0 + (y1 - y0) * s
        xi, yi = int(round(x)), int(round(y)); dim = lambda c: (int(c[0] * 0.75), int(min(255, c[1] * 0.75 + 10)), int(c[2] * 0.8), 255) if yi >= wet_from else c
        px(im, xi - 1, yi, dim(WIRE_O)); px(im, xi, yi, dim(WIRE_M)); px(im, xi + 1, yi, dim(WIRE_O))
        if i % 3 == 0: px(im, xi, yi, dim(WIRE_H))


def giant_eye(im, cx, cy, r, look, open_k, floor):
    for j in range(160):                                                      # the cord, from the eye's underside down behind the brain to the vat floor: a fleshy 5-pixel rope
        t = j / 159; x = cx + int(round(math.sin(t * 3.1) * 6 - t * 4)); y = int(cy + r * 0.6 + t * (floor - cy - r * 0.6))
        px(im, x - 2, y, CORD_D); px(im, x - 1, y, CORD_L); px(im, x, y, CORD); px(im, x + 1, y, CORD); px(im, x + 2, y, CORD_D)
    lx, ly = look[0] * r * 0.32, look[1] * r * 0.32
    rng = random.Random(3); veins = [(rng.uniform(0, math.tau), rng.uniform(0.5, 0.9)) for _ in range(9)]
    def f(d, x, y):
        if d > r - 0.6: return OUT
        if abs((y + 0.5 - cy)) > r * open_k: return CORD_D
        dp = math.hypot(x + 0.5 - cx - lx, y + 0.5 - cy - ly)
        if dp < r * 0.18: return hexc('#0c0412')
        if dp < r * 0.46: return EYE_I if dp > r * 0.3 else hexc('#8a3ad0')
        ang = math.atan2(y + 0.5 - cy, x + 0.5 - cx)
        if d > r * 0.55 and any(abs(((ang - a + math.pi) % math.tau) - math.pi) < 0.06 and d / r > s for a, s in veins): return VEIN
        return hexc('#e8e0c8') if d < r * 0.85 else hexc('#c8bca4')
    disc(im, cx, cy, r, f)
    if open_k > 0.3: px(im, int(cx - r * 0.3 + lx), int(cy - r * 0.3 + ly), EYE_G); px(im, int(cx - r * 0.3 + lx) + 1, int(cy - r * 0.3 + ly), EYE_G)


def monitor(im, t, phase):
    """The rack's monitor: his screen cleared and an animated trace (wilder as it gets angrier)."""
    x0, y0, x1, y1 = int(fx(292)), int(fy(196)), int(fx(312)), int(fy(221))
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1): px(im, x, y, SCREEN)
    amp = 2 + phase
    for x in range(x0, x1 + 1):
        yy = (y0 + y1) // 2 + int(round(math.sin((x + t * 3) * 0.9) * amp * 0.5 + math.sin((x + t * 3) * 2.3) * amp * 0.4))
        px(im, x, max(y0, min(y1, yy)), TRACE)



def glass_crack(im, x0, y0, x1, y1, seed):
    """A crack in the vat glass (replaces the straight white lines): a jagged path from the impact outward, a bright core, a dark edge on one side, a pale glint every few pixels and small branches off it.
    Deterministic per crack, so it does not shimmer between frames."""
    r = random.Random(seed); n = max(4, int(math.hypot(x1 - x0, y1 - y0) / 2.2)); pts = [(x0, y0)]
    dx, dy = (x1 - x0) / n, (y1 - y0) / n; nx, ny = -dy, dx; nl = math.hypot(nx, ny) or 1.0; nx, ny = nx / nl, ny / nl
    for i in range(1, n + 1):
        j = r.uniform(-1.6, 1.6) if i < n else 0.0; pts.append((x0 + dx * i + nx * j, y0 + dy * i + ny * j))
    for a, b in zip(pts, pts[1:]):
        line(im, int(a[0]) + 1, int(a[1]) + 1, int(b[0]) + 1, int(b[1]) + 1, CRACK_D)                    # the dark edge, down and right
    for k, (a, b) in enumerate(zip(pts, pts[1:])):
        line(im, int(a[0]), int(a[1]), int(b[0]), int(b[1]), CRACK)
        if k % 3 == 1: px(im, int(a[0]) - 1, int(a[1]), hexc('#ffffff', 200))                            # a glint on the glass beside it
        if k % 4 == 2 and k < n - 1:                                                                      # a short branch
            bl = r.uniform(2.5, 5.0); ba = math.atan2(dy, dx) + r.choice((-1, 1)) * r.uniform(0.7, 1.2)
            line(im, int(a[0]), int(a[1]), int(a[0] + math.cos(ba) * bl), int(a[1] + math.sin(ba) * bl), CRACK)
    px(im, int(x0), int(y0), hexc('#ffffff')); px(im, int(x0) + 1, int(y0), hexc('#cfe8e0', 220)); px(im, int(x0), int(y0) + 1, hexc('#cfe8e0', 220))      # the impact point


import colorsys
def harmonize(im):
    """Seats the vat in the world (Yan, 2026-11-08: "the brain looks good but really out of place"): (1) the greens (glass, liquid, lamps) lose a fifth of their saturation and a little light - the world is muted olive and wet concrete, the
    picture's glass was the one loud thing in the room; (2) a thin dark outline round the whole silhouette, like the props have; (3) under it a steel floor pad (a dark plate with bolts and drain slots) and a soft contact shadow, so it
    stands ON the floor instead of floating over it. The eyes (violet) and the brain (pinks) are left alone."""
    a = np.array(im).astype(np.float32); rgb = a[:, :, :3] / 255.0; al = a[:, :, 3] > 0
    mx, mn = rgb.max(2), rgb.min(2); d = mx - mn + 1e-6; r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    hue = np.where(mx == g, 60 * ((b - r) / d) + 120, np.where(mx == b, 60 * ((r - g) / d) + 240, 60 * (((g - b) / d) % 6)))
    greenish = (hue > 85) & (hue < 190) & (d > 0.08) & al
    sat = d / (mx + 1e-6); sat2 = np.where(greenish, sat * 0.78, sat); v2 = np.where(greenish, mx * 0.95, mx)
    c = v2 * sat2; m = v2 - c; hp = (hue % 360) / 60.0; xx = c * (1 - np.abs(hp % 2 - 1)); z = np.zeros_like(c)
    sel = [(c, xx, z), (xx, c, z), (z, c, xx), (z, xx, c), (xx, z, c), (c, z, xx)]; idx = np.clip(hp.astype(int), 0, 5); out = np.zeros_like(rgb)
    for k, (rr, gg, bb) in enumerate(sel):
        mk = idx == k; out[..., 0] = np.where(mk, rr + m, out[..., 0]); out[..., 1] = np.where(mk, gg + m, out[..., 1]); out[..., 2] = np.where(mk, bb + m, out[..., 2])
    a[:, :, :3] = np.where(greenish[..., None], out * 255.0, a[:, :, :3]); a = a.astype(np.uint8); vat = Image.fromarray(a, 'RGBA')
    ys, xs = np.nonzero(a[:, :, 3] > 40); x0, x1 = xs.min(), xs.max(); yb = min(H - 12, int(fy(BY1)) - 1)       # the base's foot (not the drained spill's row)
    base = [x for x in range(W) if a[yb - 3, x, 3] > 40]; bx0, bx1 = (min(base), max(base)) if base else (x0, x1)
    # the outline: one pixel of near-black round every opaque pixel's empty neighbours
    sil = a[:, :, 3] > 40; ring = np.zeros_like(sil)
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)): ring |= np.roll(np.roll(sil, dx, 1), dy, 0)
    ring &= ~sil; ring[:, 0] = ring[:, -1] = False; ring[0, :] = False
    ol = np.zeros_like(a); ol[ring] = (20, 22, 23, 190); outlined = Image.fromarray(ol, 'RGBA'); outlined.alpha_composite(vat)
    pad = Image.new('RGBA', (W, H), (0, 0, 0, 0)); cx = (bx0 + bx1) / 2.0; rx = (bx1 - bx0) / 2.0 + 9; cy = yb - 1.0; ry = 6.0
    for y in range(max(0, int(cy - ry - 1)), H):
        for x in range(W):
            e = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
            if e < 1.0:                                                                  # the steel pad: lit on top, darker toward its front edge, a bolt at each end, drain slots
                t = (y - (cy - ry)) / (2 * ry); col = (74, 79, 84) if t < 0.35 else (58, 62, 67) if t < 0.7 else (40, 43, 47)
                if e > 0.78: col = (30, 32, 35)
                pad.putpixel((x, y), col + (255,))
            elif e < 1.55 and y >= cy - 1: pad.putpixel((x, y), (10, 11, 12, int(120 * (1.55 - e) / 0.55)))      # the contact shadow, soft, a little wider than the pad
    pp = np.array(pad)
    for bx in (cx - rx + 4, cx + rx - 4):
        for dx, dy in ((0, 0), (1, 0)): pad.putpixel((int(bx) + dx, int(cy + 1) + dy), (122, 128, 134, 255))
    for k in range(-3, 4, 1):
        xk = int(cx + k * 9)
        for dy in (3, 4): pad.putpixel((xk, int(cy) + dy), (22, 24, 26, 255)); pad.putpixel((xk + 1, int(cy) + dy), (22, 24, 26, 255))
    pad.alpha_composite(outlined); return pad


def lid_top(im):
    """The lid seen from above (Yan, 2026-11-08: "redo the lid and top of the glass from above"): the picture shows the lid as a front-on band; the room is seen from above at 3/4, so its TOP face shows too - a steel plate
    above the band, a little narrower at the far edge (perspective), lit from the top left, with a bevel highlight, a dark outline, rivets along the far edge, a gland where each cable goes in and a service hatch. Drawn BEHIND the
    shell so the cables run over it. Rows 17-23, over the cables' feet; the front band (rows 24-34) is his drawing."""
    lx0, lx1 = 33, 97; y0, y1 = 16, 23; ports = (44, 52, 60, 68, 76, 83)
    for y in range(y0, y1 + 1):
        t = (y - y0) / float(y1 - y0); inset = int(round(4 * (1 - t))); xa, xb = lx0 + inset, lx1 - inset
        for x in range(xa, xb + 1):
            u = (x - xa) / float(max(1, xb - xa))                                                  # 0 left .. 1 right
            if y == y0: c = hexc('#1c1e20')                                                         # the far edge's outline
            elif x == xa or x == xb: c = hexc('#23262a')                                            # the side edges
            elif y == y0 + 1: c = hexc('#a9afb2') if u < 0.7 else hexc('#8a9094')                    # the bevel catching light
            elif y == y1: c = hexc('#3a3e42')                                                       # the shadow where it meets the front band
            else: c = hexc('#838a8e') if u < 0.33 else hexc('#6b7276') if u < 0.7 else hexc('#585e62')
            px(im, x, y, c)
    for x in range(lx0 + 9, lx1 - 6, 11):                                                           # rivets along the far edge
        px(im, x, y0 + 2, hexc('#c4cacd')); px(im, x, y0 + 3, hexc('#2c3034'))
    for x in ports:                                                                                 # a gland round each cable's entry: dark mouth, a lit lip above it
        px(im, x - 1, y1 - 2, hexc('#b3b9bc')); px(im, x, y1 - 2, hexc('#b3b9bc')); px(im, x + 1, y1 - 2, hexc('#b3b9bc'))
        px(im, x - 1, y1 - 1, hexc('#0c0e10')); px(im, x, y1 - 1, hexc('#0c0e10')); px(im, x + 1, y1 - 1, hexc('#0c0e10'))
    hx = lx1 - 26                                                                                  # a service hatch with its handle
    for yy in (y0 + 3, y0 + 4):
        for xx in range(hx, hx + 11): px(im, xx, yy, hexc('#4a5054') if yy == y0 + 3 else hexc('#3a3e42'))
    for xx in range(hx + 3, hx + 8): px(im, xx, y0 + 3, hexc('#c4cacd'))


def glass_top(im):
    """The top of the glass from above: under the lid's overhang the glass's inside is in the lid's shadow (a dark band that fades down the glass), the front rim of the glass catches a thin pale line, and a dark line under it
    is the glass's thickness. Drawn over the liquid and the brain, under the vat's shell (its glints stay on top)."""
    gx0, gx1, gy0 = A.GX0, A.GX1, A.GY0
    for k in range(7):
        al = int(150 * (1 - k / 7.0) ** 1.6)
        for x in range(gx0, gx1 + 1):
            if al > 0: px(im, x, gy0 + k, (8, 14, 13, al))
    for x in range(gx0, gx1 + 1):
        px(im, x, gy0, hexc('#d6ebe3', 190)); px(im, x, gy0 + 1, hexc('#0d1815', 150))

def frame(state, t):
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0)); lid_top(im)                  # the lid's top face first, behind everything (the cables run over it)
    phase = state[1] if isinstance(state, tuple) else 3
    GX0, GX1, GY0, GY1, CX = A.GX0, A.GX1, A.GY0, A.GY1, A.CX
    span = GY1 - GY0; bh = A.bh
    level = GY0 + int(LEVEL_FRAC[phase] * span) if isinstance(state, tuple) else GY1 + 1
    liq = np.array(A.liquid)
    for y in range(GY0, GY1 + 1):                                             # his water below the level, thin dark air above, a bright surface line
        for x in range(GX0, GX1 + 1):
            if y < level: px(im, x, y, AIR)
            elif y == level and level <= GY1: px(im, x, y, LIQ_S)
            elif liq[y, x, 3]: im.putpixel((x, y), tuple(int(v) for v in liq[y, x]))
    if level < GY1:                                                           # bubbles: each rises at its own pace, wraps at the surface
        sp = GY1 - level - 3
        for i in range(12):
            bx = GX0 + 3 + (i * 37) % (GX1 - GX0 - 6); by = GY1 - 3 - (i * 13 + t * (3 + i % 3)) % max(1, sp)
            px(im, bx, by, hexc('#9fe0b0', 200))
            if i % 4 == 0: px(im, bx + 1, by, hexc('#9fe0b0', 160))
    bob = int(round(math.sin(t / COLS * math.tau) * (2 if phase < 2 else 1)))
    eyes, scale = EYE_COUNT[phase], 1.0 + 0.12 * phase
    drained = state == 'drained'; shut = (state[0] == 'shut') if isinstance(state, tuple) else drained
    squash = 0.78 if drained else 1.0
    if isinstance(state, tuple):
        top = (GY0 + 0.45 * span - bh / 2 if level < GY0 + 0.3 * span else min(GY1 - bh - 3, level + 3 - bh / 2)) + bob
        cxo = CX + (1 if shut and t % 2 else 0); top += (-1 if shut and t % 4 == 1 else 0)
    elif drained: top, cxo = GY1 - 1 - bh * squash, CX
    elif state == 'awake': floor_top = GY1 - 1 - bh; top, cxo = floor_top - (floor_top - (GY0 + 6 + 0.12 * span)) * (t / (COLS - 1)) ** 1.6, CX
    else: top, cxo = GY0 + 28, CX
    top = int(round(top)); cxo = int(round(cxo))
    if isinstance(state, tuple) or state in ('drained', 'awake'):             # electrode cables from the lid to the top of the brain
        for i, dx in enumerate((-6, 0, 6)): wire(im, CX + dx * 2, GY0, cxo + dx, top + 3, i * 2.1, level)
    if state == 'eye':                                                        # the giant eye slides out from behind, opens, then turns to look at you (down-left)
        s = min(1.0, t / 4); look = (-0.8, 0.7) if t >= 5 else (0, 0)
        giant_eye(im, CX + int(9 * s), top + int(bh / 2) - int(23 * s), 14, look, min(1.0, max(0.05, (t - 1) / 3)), GY1)
    cord_px = 0 if drained else max(0, min(48, GY1 - (top + bh) - 1))
    spr, sx0 = brain_sprite(shut, eyes, scale, cord_px, squash)
    ox, oy = int(round(cxo - sx0)), int(round(top - 3 * squash))
    clip = Image.new('RGBA', (W, H), (0, 0, 0, 0)); sp = spr
    if ox < 0: sp = sp.crop((-ox, 0, sp.width, sp.height)); ox = 0
    if oy < 0: sp = sp.crop((0, -oy, sp.width, sp.height)); oy = 0
    clip.alpha_composite(sp, (ox, oy))
    ca = np.array(clip); ca[GY1 + 1:, :, 3] = 0; ca[:, :GX0, 3] = 0; ca[:, GX1 + 1:, 3] = 0
    im.alpha_composite(Image.fromarray(ca, 'RGBA'))
    glass_top(im)
    im.alpha_composite(A.shell)                                               # the vat over everything in it: lid, glass reflections, base, machines, cables
    cracks = [(fx(150), fy(150), [(-3, -12), (7, -18), (4, 11), (10, 17), (-5, 18)]), (fx(228), fy(122), [(-9, -10), (-6, 12), (4, 14), (-14, 4)]), (fx(222), fy(235), [(-6, -14), (6, -8), (-10, 6), (2, 12), (-4, 16)])]
    for ci, (sx, sy, legs) in enumerate(cracks[:3 if not isinstance(state, tuple) else phase]):
        for li, (lx, ly) in enumerate(legs): glass_crack(im, sx, sy, sx + lx, sy + ly, 1000 + ci * 17 + li * 3)
    monitor(im, t, phase)
    for i in range(6):                                                        # the base panel's lights blink
        on = (i + t) % 4 != 0
        px(im, int(fx(170)) + i * 4, int(fy(297)), hexc('#5ee07a') if on and i % 3 else hexc('#e0503a') if on else hexc('#3a3e42'))
    if state in ('awake', 'eye'):                                             # psi stirring
        rng = random.Random(t)
        for i in range(26):
            ang = rng.random() * math.tau; r = rng.uniform(14, 28)
            px(im, int(cxo + math.cos(ang) * r * 1.2), int(top + bh / 2 + math.sin(ang) * r * 0.7), PSI if i % 3 else hexc('#c06bff', 170))
    if drained:                                                               # the spill
        for x in range(GX0 - 6, GX1 + 4):
            if (x * 7) % 5: px(im, x, H - 1, hexc('#34502c', 225))
    return im


ROWS = [('open', p) for p in range(4)] + [('shut', p) for p in range(4)] + ['drained', 'awake', 'eye']
if __name__ == '__main__':
    A = Art()
    out = sys.argv[1] if len(sys.argv) > 1 and sys.argv[1] != 'game' else os.path.join(os.path.dirname(os.path.abspath(__file__)), '_out', 'brain_v4')
    os.makedirs(out, exist_ok=True)
    sheet = Image.new('RGBA', (W * COLS, H * len(ROWS)))
    for r, st in enumerate(ROWS):
        for t in range(COLS): sheet.alpha_composite(harmonize(frame(st, t)), (t * W, r * H))
    sheet.save(os.path.join(out, 'boss_brain_vat.png'))
    strip = Image.new('RGBA', (W * 4 * 3 + 30, H * 3 * 3 + 20), (22, 24, 26, 255))      # a review sheet: one frame of every state at 3x, in the sheet's order
    for i in range(11):
        f = frame(ROWS[i], 3 if i in (9, 10) else 2).resize((W * 3, H * 3), Image.NEAREST); strip.alpha_composite(f, ((i % 4) * (W * 3 + 10), (i // 4) * (H * 3 + 10)))
    strip.save(os.path.join(out, 'review_states.png'))
    if 'game' in sys.argv[1:]:
        game = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Assets', 'Resources', 'Sprites', 'Mutants')
        sheet.save(os.path.join(game, 'boss_brain_vat.png'))
    print('wrote', os.path.join(out, 'boss_brain_vat.png'), sheet.size, 'glass', A.GX0, A.GX1, A.GY0, A.GY1, 'body height', round(A.bh, 1))

