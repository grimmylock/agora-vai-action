# The cathedral set, v2 with real shading (2026-11-20; Yan: "is something preventing them from being higher quality? seems very low-pixel count" -> "redo the props with real shading").
# Same double density as the furniture (scale 2: a tile is 32 sprite px) but every surface is lit: cylinders shaded across their width, wood with grain and planks, stone with weathering and moss,
# cloth with folds, bevels on carved panels, ordered dithering at the tone borders. Looks: pew_l / pew_m / pew_r, column, altar, candelabra, statue, font, confessional, tomb.
# Registered into props.PROPS by props.py. NOT in the shipped atlas until Yan approves the preview (maps/renders/cathedral_props_preview.png).
import math
import numpy as np
from PIL import Image, ImageDraw
import props
from props import Prop
from worldlib import *
from props_x2 import DARK

STONE = P["rock"]; WOOD = P["wood"]
BRASS = ramp("#2a1c08", "#5a3e10", "#92681c", "#c8a034", "#f4d878"); CLOTH = ramp("#1e0809", "#3c1012", "#5e1618", "#86201f", "#b0332d")
MOSS = ramp("#1e2a14", "#2e3e1e", "#44582c", "#5e7a3a"); WAX = ramp("#6a6048", "#9a8e6c", "#c8bd98", "#e8dfbc", "#fff6d8")
BAYER = np.array([[0, 2], [3, 1]]) / 4.0

def _noise(w, h, cell, seed): return value_noise(w, h, cell, seed, wrap=False)
def _stretch(w, h, cx, cy, seed):
    """noise stretched along one axis (wood grain, stone streaks): cell sizes cx, cy"""
    r = np.random.RandomState(seed); gw, gh = max(2, w // cx + 2), max(2, h // cy + 2); g = r.rand(gh, gw)
    ys = np.arange(h) / cy; xs = np.arange(w) / cx; iy = np.minimum(np.floor(ys).astype(int), gh - 2); ix = np.minimum(np.floor(xs).astype(int), gw - 2)
    ty = ys - iy; tx = xs - ix; a = g[iy][:, ix]; b = g[iy][:, ix + 1]; c = g[iy + 1][:, ix]; d = g[iy + 1][:, ix + 1]
    top = a + (b - a) * tx[None, :]; bot = c + (d - c) * tx[None, :]; return top + (bot - top) * ty[:, None]

def mask_of(w, h, draw_fn):
    m = Image.new("L", (w, h), 0); draw_fn(ImageDraw.Draw(m)); return np.array(m) > 0

def put(im, mask, v, pal, dither=True):
    """fill the masked pixels of `im` with ramp colours chosen by the value field v (0..1), with a 2x2 ordered dither at the borders"""
    h, w = mask.shape; k = len(pal) - 1; yy, xx = np.mgrid[0:h, 0:w]
    th = (BAYER[yy % 2, xx % 2] - 0.375) * (0.9 if dither else 0.0)
    idx = np.clip(np.floor(np.clip(v, 0, 1) * k + 0.5 + th), 0, k).astype(int)
    a = np.array(im)
    for i, c in enumerate(pal): a[(idx == i) & mask] = c
    im.paste(Image.fromarray(a, "RGBA"))

def grid(w, h): return np.mgrid[0:h, 0:w]

def finish2(im, pivot, shadow, tall=False, sides=(True, True)):
    """pad 1, selective outline (darkened own colour), double density; sides=(left, right) lets a tiling piece skip its joins"""
    import numpy as np
    a = np.array(im).astype(np.int32); H, W = a.shape[:2]; pl = 1 if sides[0] else 0; pr = 1 if sides[1] else 0
    big = np.zeros((H + 2, W + pl + pr, 4), np.int32); big[1:H + 1, pl:pl + W] = a; al = big[..., 3] > 0; o = big.copy(); HH, WW = al.shape
    dirs = [(0, -1), (0, 1)] + ([(-1, 0)] if sides[0] else []) + ([(1, 0)] if sides[1] else [])
    for y in range(HH):
        for x in range(WW):
            if al[y, x]: continue
            best = None
            for dx, dy in dirs:
                nx, ny = x + dx, y + dy
                if 0 <= nx < WW and 0 <= ny < HH and al[ny, nx]:
                    c = big[ny, nx]; lum = c[0] + c[1] + c[2]
                    if best is None or lum < best[0]: best = (lum, c)
            if best is not None: c = best[1]; o[y, x] = (int(c[0] * .38) + 6, int(c[1] * .38) + 5, int(c[2] * .38) + 4, 255)
    return Prop(Image.fromarray(o.astype(np.uint8), "RGBA"), (pivot[0] + pl, pivot[1] + 1), shadow, tall, scale=2)

def weather(im, mask, seed, moss_at=0.8, moss_amt=0.2, streak=True, bottom_bias=1.0):
    """moss in the low, damp places and dark rain streaks down stone"""
    h, w = mask.shape; yy, xx = grid(w, h); n = _noise(w, h, 5, seed); m2 = _noise(w, h, 3, seed + 9)
    low = np.clip((yy / h - 0.55) / 0.45, 0, 1) ** 2 * bottom_bias; mv = n * 0.55 + low * 0.45 + m2 * 0.1
    a = np.array(im)
    for y in range(h):
        for x in range(w):
            if mask[y, x] and mv[y, x] > moss_at: a[y, x] = MOSS[int(min(3, max(0, (mv[y, x] - moss_at) * 10)))]
    if streak:
        s = _stretch(w, h, 3, 28, seed + 3)
        for y in range(h):
            for x in range(w):
                if mask[y, x] and s[y, x] > 0.72: a[y, x] = tuple(int(c * 0.8) for c in a[y, x][:3]) + (255,)
    im.paste(Image.fromarray(a, "RGBA"))

# ---------------------------------------------------------------- column: fluted shaft shaded as a cylinder, a moulded base and capital, springing of the vault, cracks, moss
def column(seed):
    W_, H_ = 32, 88; im = blank(W_, H_); S = STONE; yy, xx = grid(W_, H_)
    def lit(x0, x1, base=0.5):                                                                           # cylinder across [x0, x1): lit from the left, flutes as ripples
        u = np.clip((xx - x0 + 0.5) / (x1 - x0), 0.01, 0.99); nx = (u - 0.5) * 2; lam = 0.5 - 0.42 * nx + 0.22 * np.sqrt(1 - nx * nx)
        return base * 0.35 + lam * 0.75 + 0.07 * np.sin(u * 2 * math.pi * 3.5) + (_stretch(W_, H_, 3, 22, seed) - 0.5) * 0.18
    shaft = mask_of(W_, H_, lambda d: d.rectangle([8, 14, 23, 70], fill=255)); put(im, shaft, lit(8, 24), S)
    base1 = mask_of(W_, H_, lambda d: d.rectangle([2, 76, 29, 87], fill=255)); v = 0.42 + (xx < 5) * 0.2 - (xx > 26) * 0.18 - (yy - 76) / 11 * 0.22 + (_noise(W_, H_, 4, seed + 1) - 0.5) * 0.25
    v = np.where(yy < 79, v + 0.3, v); put(im, base1, v, S)                                                # the plinth: a lit top lip, a darker face
    torus = mask_of(W_, H_, lambda d: d.rectangle([5, 70, 26, 76], fill=255)); put(im, torus, lit(5, 27) * 0.9 + (yy - 70) / 6 * -0.1 + 0.1, S)
    cap = mask_of(W_, H_, lambda d: d.rectangle([4, 6, 27, 14], fill=255)); put(im, cap, lit(4, 28) * 0.85 + 0.12 - (yy - 6) / 8 * 0.18, S)
    spring = mask_of(W_, H_, lambda d: d.polygon([(2, 6), (29, 6), (24, 0), (7, 0)], fill=255)); put(im, spring, 0.55 + (yy < 3) * 0.25 - (xx > 22) * 0.2 + (_noise(W_, H_, 3, seed + 2) - 0.5) * 0.2, S)
    allm = shaft | base1 | torus | cap | spring; weather(im, allm, seed + 4, moss_at=0.8, bottom_bias=1.4)
    d = ImageDraw.Draw(im)                                                                                # cracks and chips by hand
    for pts in (((15, 24), (17, 34), (14, 44), (18, 58)), ((22, 80), (24, 84))): d.line(pts, fill=S[0])
    d.line([(16, 24), (18, 34), (15, 44)], fill=S[4])
    d.line([(7, 6), (24, 0)], fill=S[4]); d.line([(4, 14), (27, 14)], fill=S[0]); d.line([(8, 70), (23, 70)], fill=S[0])
    return finish2(im, (16, 86), (30, 6, 1, -1), tall=True)

# ---------------------------------------------------------------- pews: one long bench cut into three tiles (left end, tileable middle, right end): planks, grain, a bevelled front panel, a high back
_PEWCACHE = {}
def _pew_sheet(seed):
    if seed in _PEWCACHE: return _PEWCACHE[seed]
    W_, H_ = 96, 36; im = blank(W_, H_); W = WOOD; yy, xx = grid(W_, H_)
    grain = _stretch(W_, H_, 14, 2, seed); grain2 = _stretch(W_, H_, 5, 1, seed + 7)
    def planks(y0, y1, base):                                                                                  # vertical-planked region (period 8 so the middle tile repeats): per-plank tone, grain, dark gaps
        m = mask_of(W_, H_, lambda d: d.rectangle([0, y0, 95, y1], fill=255)); pl = (xx // 8); tone = np.array([0.0, 0.08, -0.06, 0.05, -0.03, 0.09, -0.07, 0.02, 0.06, -0.05, 0.04, -0.02])[pl % 12]
        v = base + tone + (grain - 0.5) * 0.28 + (grain2 - 0.5) * 0.12 - ((xx % 8) == 7) * 0.3 + ((xx % 8) == 0) * 0.08
        put(im, m, v, W); return m
    back = planks(4, 16, 0.55); put(im, mask_of(W_, H_, lambda d: d.rectangle([0, 4, 95, 5], fill=255)), 0.9 + (grain - 0.5) * 0.1, W)           # the lit top rail of the back
    put(im, mask_of(W_, H_, lambda d: d.rectangle([0, 16, 95, 17], fill=255)), 0.18, W)                                                           # the shadow under the back
    seat = mask_of(W_, H_, lambda d: d.rectangle([0, 18, 95, 22], fill=255)); put(im, seat, 0.78 - (yy - 18) / 5 * 0.25 + (grain - 0.5) * 0.2, W)  # the seat edge: a lit lip
    front = planks(23, 31, 0.36)
    for x0 in range(2, 94, 8):                                                                                  # bevelled panels on the front: light top-left, dark bottom-right
        d = ImageDraw.Draw(im); d.rectangle([x0, 25, x0 + 4, 29], fill=W[0]); d.line([(x0 + 1, 26), (x0 + 3, 26)], fill=W[2]); d.line([(x0 + 1, 26), (x0 + 1, 28)], fill=W[2]); d.line([(x0 + 4, 26), (x0 + 4, 29)], fill=W[3])
    d = ImageDraw.Draw(im); d.line([(0, 31), (95, 31)], fill=W[0]); d.line([(0, 32), (95, 32)], fill=tuple(int(c * 0.5) for c in W[0][:3]) + (255,))
    for (kx, ky) in ((11, 10), (37, 8), (58, 12), (82, 9)): d.ellipse([kx - 1, ky - 1, kx + 1, ky + 1], outline=W[0]); im.putpixel((kx, ky), W[1])   # knots
    for (px_, y_) in ((3, 29), (91, 29)):                                                                        # end posts
        pm = mask_of(W_, H_, lambda dd: dd.rectangle([px_ - 3 if px_ < 10 else px_ - 1, 0, px_ + 2 if px_ < 10 else px_ + 3, 31], fill=255)); put(im, pm, 0.62 + (xx - (px_ - 3 if px_ < 10 else px_ - 1)) * (-0.12 if px_ < 10 else -0.12) + (grain - 0.5) * 0.2, W)
    for cx in (1, 94):                                                                                           # the carved finials: a shaded ball
        bm = mask_of(W_, H_, lambda dd: dd.ellipse([cx - 3, -1, cx + 3, 5], fill=255)); bm = bm & (yy >= 0)
        v = 0.55 + (-(xx - cx) * 0.09) + (-(yy - 2) * 0.09) + 0.1; put(im, bm, v, W)
    _PEWCACHE[seed] = im; return im
def _pew(seed, part):
    sheet = _pew_sheet(1000); x0 = {'l': 0, 'm': 32, 'r': 64}[part]; im = sheet.crop((x0, 0, x0 + 32, 36))
    return finish2(im, (16, 34), (32, 5, 0, -1), sides=(part == 'l', part == 'r'))
def pew_l(seed): return _pew(seed, 'l')
def pew_m(seed): return _pew(seed, 'm')
def pew_r(seed): return _pew(seed, 'r')

# ---------------------------------------------------------------- altar: a weathered stone table under a deep red cloth with folds and old stains, a cracked stone cross, melted candle stubs with a lit flame
def altar(seed):
    W_, H_ = 32, 50; im = blank(W_, H_); S = STONE; yy, xx = grid(W_, H_); r = rng(seed)
    body = mask_of(W_, H_, lambda d: d.rectangle([0, 32, 31, 46], fill=255)); put(im, body, 0.45 - (yy - 32) / 14 * 0.2 + (xx < 3) * 0.22 + (_noise(W_, H_, 4, seed) - 0.5) * 0.3, S)
    slab = mask_of(W_, H_, lambda d: d.rectangle([0, 20, 31, 31], fill=255)); put(im, slab, 0.72 + (_noise(W_, H_, 3, seed + 1) - 0.5) * 0.22 - (yy - 20) / 11 * 0.1 - (xx > 28) * 0.15, S)
    cloth = mask_of(W_, H_, lambda d: d.rectangle([3, 20, 28, 40], fill=255)); folds = 0.5 + 0.3 * np.sin(xx * 0.95 + 0.6 * np.sin(xx * 0.31)) - (yy - 20) / 20 * 0.18 + (_stretch(W_, H_, 6, 9, seed + 2) - 0.5) * 0.15
    put(im, cloth, folds, CLOTH)
    d = ImageDraw.Draw(im); d.line([(3, 40), (28, 40)], fill=CLOTH[0])
    for x in range(4, 28, 3): d.point((x, 41 + (x % 2)), fill=CLOTH[2])                                           # a ragged hem
    for _ in range(26): im.putpixel((r.randint(4, 27), r.randint(26, 39)), hx("#2a0808"))                          # old stains
    cross = mask_of(W_, H_, lambda dd: (dd.rectangle([14, 5, 17, 20], fill=255), dd.rectangle([10, 9, 21, 12], fill=255))); put(im, cross, 0.62 - (xx - 14) * 0.05 + (_noise(W_, H_, 3, seed + 4) - 0.5) * 0.3, S)
    d.line([(15, 6), (16, 19)], fill=S[0]); d.line([(11, 10), (13, 11)], fill=S[0])                                  # the crack
    for cx in (5, 25):                                                                                             # melted stubs: wax with a drip, a lit flame and a glow
        stub = mask_of(W_, H_, lambda dd, c=cx: dd.rectangle([c, 14, c + 2, 20], fill=255)); put(im, stub, 0.7 - (xx - cx) * 0.18, WAX)
        d.line([(cx + 2, 15), (cx + 2, 18)], fill=WAX[1]); im.putpixel((cx + 1, 13), hx("#ffe890")); im.putpixel((cx + 1, 12), hx("#ffc050")); im.putpixel((cx + 1, 11), hx("#ff8a28"))
        for gx, gy in ((cx, 12), (cx + 2, 12), (cx + 1, 10)): im.putpixel((gx, gy), hx("#ffb04a")) if im.getpixel((gx, gy))[3] == 0 else None
    weather(im, body | slab, seed + 6, moss_at=0.8, bottom_bias=1.0, streak=True)
    return finish2(im, (16, 48), (30, 5, 1, -1))

# ---------------------------------------------------------------- candelabra: a turned brass stem shaded as a cylinder, five arms with cups, five lit candles with a glow, a spreading foot
def candelabra(seed):
    W_, H_ = 24, 64; im = blank(W_, H_); B = BRASS; yy, xx = grid(W_, H_)
    def cyl(x0, x1, base=0.6):
        u = np.clip((xx - x0 + 0.5) / (x1 - x0), 0.02, 0.98); nx = (u - 0.5) * 2; return base * 0.3 + (0.5 - 0.5 * nx + 0.25 * np.sqrt(1 - nx * nx)) * 0.85
    for (y0, y1, x0, x1) in ((24, 56, 10, 14), (38, 41, 8, 16), (26, 28, 8, 16), (46, 48, 8, 16)):               # the stem and its turned collars
        m = mask_of(W_, H_, lambda d, a=(y0, y1, x0, x1): d.rectangle([a[2], a[0], a[3], a[1]], fill=255)); put(im, m, cyl(x0, x1 + 1), B)
    foot = mask_of(W_, H_, lambda d: d.polygon([(3, 62), (20, 62), (16, 56), (7, 56)], fill=255)); put(im, foot, 0.55 + (xx < 10) * 0.2 - (yy - 56) / 6 * 0.2, B)
    d = ImageDraw.Draw(im); d.line([(3, 62), (20, 62)], fill=B[0]); d.line([(7, 56), (16, 56)], fill=B[4])
    for (x0, y0) in ((3, 14), (7, 10), (11, 8), (15, 10), (19, 14)):
        d.line([(11, 24), (11, 20)], fill=B[3]); d.line([(11, 20), (x0 + 1, y0 + 4)], fill=B[3]); d.line([(12, 21), (x0 + 2, y0 + 5)], fill=B[1])         # an arm
        cup = mask_of(W_, H_, lambda dd, a=(x0, y0): dd.rectangle([a[0] - 1, a[1] + 4, a[0] + 3, a[1] + 6], fill=255)); put(im, cup, 0.6 - (xx - x0) * 0.1, B)
        c = mask_of(W_, H_, lambda dd, a=(x0, y0): dd.rectangle([a[0], a[1], a[0] + 1, a[1] + 3], fill=255)); put(im, c, 0.85 - (xx - x0) * 0.35, WAX)
        im.putpixel((x0, y0 - 1), hx("#ffe890")); im.putpixel((x0, y0 - 2), hx("#ff9a30")); im.putpixel((x0 + 1, y0 - 1), hx("#ffb04a"))
        for gx, gy in ((x0 - 1, y0 - 1), (x0 + 1, y0 - 2), (x0, y0 - 3)):
            if 0 <= gx < W_ and 0 <= gy < H_ and im.getpixel((gx, gy))[3] == 0: im.putpixel((gx, gy), hx("#a8601f"))
    return finish2(im, (12, 62), (18, 4, 1, -1), tall=True)

# ---------------------------------------------------------------- statue: a weathered saint, deep drapery folds, a shaded head with the face worn flat, folded hands, a chipped crown, moss and rain streaks
def statue(seed):
    W_, H_ = 32, 76; im = blank(W_, H_); S = STONE; yy, xx = grid(W_, H_); r = rng(seed)
    plinth = mask_of(W_, H_, lambda d: d.rectangle([3, 60, 28, 75], fill=255)); put(im, plinth, 0.42 + (xx < 7) * 0.18 - (xx > 25) * 0.15 - (yy - 60) / 15 * 0.18 + (yy < 63) * 0.3 + (_noise(W_, H_, 4, seed) - 0.5) * 0.25, S)
    robe = mask_of(W_, H_, lambda d: d.polygon([(8, 16), (23, 16), (26, 60), (5, 60)], fill=255))
    cx = (np.clip(xx, 6, 26) - 15.5) / 10.0; folds = 0.52 - 0.35 * cx + 0.22 * np.sin(xx * 1.15 + yy * 0.06) + 0.12 * np.sin(xx * 0.5 - yy * 0.05) + (_noise(W_, H_, 4, seed + 2) - 0.5) * 0.2
    put(im, robe, folds, S)
    arms = mask_of(W_, H_, lambda d: (d.polygon([(7, 20), (11, 20), (13, 36), (9, 44)], fill=255), d.polygon([(24, 20), (20, 20), (18, 36), (22, 44)], fill=255))); put(im, arms, 0.5 - (xx - 15) * 0.02 + 0.2 * np.sin(yy * 0.6) + (xx < 12) * 0.12, S)
    hands = mask_of(W_, H_, lambda d: d.rectangle([12, 29, 19, 35], fill=255)); put(im, hands, 0.7 - (yy - 29) / 7 * 0.25 - (xx - 12) * 0.03, S)
    head = mask_of(W_, H_, lambda d: d.ellipse([11, 2, 20, 14], fill=255)); hv = 0.62 - (xx - 15) * 0.07 - (yy - 8) * 0.04 + (_noise(W_, H_, 3, seed + 3) - 0.5) * 0.2; put(im, head, hv, S)
    d = ImageDraw.Draw(im); im.putpixel((13, 8), S[1]); im.putpixel((17, 8), S[1]); d.line([(15, 9), (15, 11)], fill=S[1]); d.line([(14, 12), (17, 12)], fill=S[1])      # the face worn to a few marks
    d.polygon([(11, 3), (14, 0), (20, 4), (16, 3)], fill=S[0]); d.line([(12, 3), (14, 1)], fill=S[3])                                                          # a chip out of the crown
    d.line([(21, 40), (23, 56)], fill=S[0]); d.line([(10, 44), (9, 58)], fill=S[0])
    weather(im, plinth | robe | head | arms | hands, seed + 5, moss_at=0.8, bottom_bias=1.5, streak=True)
    return finish2(im, (16, 74), (30, 6, 1, -1), tall=True)

# ---------------------------------------------------------------- font: a stone basin on a fluted pedestal, dark still water with a highlight, worn lip
def font(seed):
    W_, H_ = 24, 44; im = blank(W_, H_); S = STONE; yy, xx = grid(W_, H_)
    def cyl(x0, x1):
        u = np.clip((xx - x0 + 0.5) / (x1 - x0), 0.02, 0.98); nx = (u - 0.5) * 2; return 0.15 + (0.5 - 0.45 * nx + 0.22 * np.sqrt(1 - nx * nx)) * 0.8 + (_noise(W_, H_, 3, seed) - 0.5) * 0.2
    ped = mask_of(W_, H_, lambda d: d.rectangle([8, 22, 15, 38], fill=255)); put(im, ped, cyl(8, 16), S)
    foot = mask_of(W_, H_, lambda d: d.rectangle([4, 38, 19, 43], fill=255)); put(im, foot, cyl(4, 20) * 0.9 - (yy - 38) / 5 * 0.1, S)
    bowl = mask_of(W_, H_, lambda d: d.ellipse([1, 10, 22, 26], fill=255)); bv = cyl(1, 23) * 0.95 + (yy - 10) / 16 * -0.2; put(im, bowl, bv, S)
    water = mask_of(W_, H_, lambda d: d.ellipse([3, 12, 20, 22], fill=255)); put(im, water, 0.18 + 0.12 * np.sin(xx * 0.9) + (_noise(W_, H_, 3, seed + 3) - 0.5) * 0.12, ramp("#0a1416", "#122024", "#1c3236", "#2c4c50", "#5a8284"))
    d = ImageDraw.Draw(im); d.arc([1, 10, 22, 26], 200, 340, fill=S[4]); d.line([(6, 15), (11, 14)], fill=hx("#7aa0a2")); d.point((8, 14), fill=hx("#cfe6e6"))
    weather(im, ped | foot | bowl, seed + 4, moss_at=0.8, bottom_bias=1.6, streak=False)
    return finish2(im, (12, 42), (22, 5, 1, -1))

# ---------------------------------------------------------------- confessional: dark wood booth, grain, a bevelled panelled door half open on a velvet curtain, a carved lattice window, a cornice
def confessional(seed):
    W_, H_ = 32, 80; im = blank(W_, H_); W = WOOD; yy, xx = grid(W_, H_); r = rng(seed)
    grain = _stretch(W_, H_, 9, 2, seed); box = mask_of(W_, H_, lambda d: d.rectangle([0, 8, 31, 74], fill=255))
    put(im, box, 0.34 + (xx < 3) * 0.2 - (xx > 28) * 0.1 + (grain - 0.5) * 0.3 + ((xx % 8) == 0) * -0.06, W)
    cornice = mask_of(W_, H_, lambda d: d.polygon([(0, 8), (4, 0), (27, 0), (31, 8)], fill=255)); put(im, cornice, 0.62 + (yy < 2) * 0.3 - (yy - 2) / 6 * 0.25 + (grain - 0.5) * 0.2, W)
    d = ImageDraw.Draw(im); d.line([(0, 8), (31, 8)], fill=W[0])
    for x in range(3, 30, 4): d.line([(x, 2), (x, 7)], fill=W[1])                                              # dentils
    win = mask_of(W_, H_, lambda dd: dd.rectangle([4, 16, 27, 28], fill=255)); put(im, win, np.full((H_, W_), 0.0), ramp("#060606", "#0c0a0a"))
    for x in range(6, 27, 3): d.line([(x, 16), (x, 28)], fill=W[3]); d.line([(x + 1, 17), (x + 1, 28)], fill=W[1])
    for y in range(18, 28, 3): d.line([(4, y), (27, y)], fill=W[3])
    d.rectangle([4, 16, 27, 28], outline=W[0]); d.line([(4, 16), (27, 16)], fill=W[4])
    door = mask_of(W_, H_, lambda dd: dd.rectangle([4, 34, 27, 72], fill=255)); put(im, door, 0.3 + (grain - 0.5) * 0.2, W)
    for (x0, y0, x1, y1) in ((6, 36, 14, 52), (6, 56, 14, 70), (17, 36, 26, 52), (17, 56, 26, 70)):                # bevelled door panels
        d.rectangle([x0, y0, x1, y1], outline=W[0]); d.line([(x0 + 1, y0 + 1), (x1 - 1, y0 + 1)], fill=W[3]); d.line([(x0 + 1, y0 + 1), (x0 + 1, y1 - 1)], fill=W[3]); d.line([(x1, y0 + 1), (x1, y1)], fill=W[1])
    cur = mask_of(W_, H_, lambda dd: dd.polygon([(14, 34), (27, 34), (27, 72), (14, 72), (20, 56)], fill=255)); put(im, cur, 0.45 + 0.3 * np.sin(xx * 1.3 + yy * 0.05) - (yy - 34) / 38 * 0.2, CLOTH)    # the curtain, hanging in folds
    dark = mask_of(W_, H_, lambda dd: dd.polygon([(5, 35), (13, 35), (5, 71)], fill=255)); put(im, dark, np.zeros((H_, W_)), ramp("#050303", "#0a0505"))
    d.line([(0, 74), (31, 74)], fill=W[0])
    return finish2(im, (16, 78), (30, 5, 1, -1), tall=True)

# ---------------------------------------------------------------- tomb: a stone box with bevelled panels, a lid carved with a recumbent figure (relief), weathering, moss
def tomb(seed):
    W_, H_ = 32, 40; im = blank(W_, H_); S = STONE; yy, xx = grid(W_, H_); r = rng(seed)
    box = mask_of(W_, H_, lambda d: d.rectangle([0, 23, 31, 37], fill=255)); put(im, box, 0.44 - (yy - 23) / 14 * 0.2 + (xx < 3) * 0.2 - (xx > 28) * 0.12 + (_noise(W_, H_, 4, seed) - 0.5) * 0.3, S)
    lid = mask_of(W_, H_, lambda d: d.rectangle([1, 12, 30, 22], fill=255)); put(im, lid, 0.7 + (yy < 14) * 0.2 - (xx > 27) * 0.18 + (_noise(W_, H_, 3, seed + 1) - 0.5) * 0.25, S)
    d = ImageDraw.Draw(im)
    fig = mask_of(W_, H_, lambda dd: (dd.ellipse([12, 13, 19, 19], fill=255), dd.rectangle([13, 18, 18, 22], fill=255))); put(im, fig, 0.8 - (xx - 12) * 0.06 - (yy - 13) * 0.05, S)                      # the relief figure
    d.line([(13, 22), (18, 22)], fill=S[1]); im.putpixel((14, 16), S[1]); im.putpixel((17, 16), S[1])
    for y in (27, 31): d.line([(5, y), (26, y)], fill=S[1]); d.line([(5, y + 1), (26, y + 1)], fill=S[3])                                                                   # the inscription, cut and lit
    d.rectangle([3, 25, 28, 35], outline=S[1]); d.line([(4, 26), (27, 26)], fill=S[3])
    d.line([(24, 12), (26, 22), (23, 30)], fill=S[0])
    weather(im, box | lid, seed + 4, moss_at=0.8, bottom_bias=1.4)
    return finish2(im, (16, 38), (30, 5, 1, -1))

NEW = {"pew_l": pew_l, "pew_m": pew_m, "pew_r": pew_r, "column": column, "altar": altar, "candelabra": candelabra, "statue": statue, "font": font, "confessional": confessional, "tomb": tomb}
VARIANTS = {k: 1 for k in NEW}

if __name__ == "__main__":
    import ground, os, sys
    T = 16; W_, H_ = T * 14, T * 9
    bg = blank(W_, H_); g = ground.GROUNDS["concrete"](3)
    for ty in range(0, H_, g.height):
        for tx in range(0, W_, g.width): bg.paste(g, (tx, ty))
    big = bg.resize((W_ * 2, H_ * 2), Image.NEAREST)
    items = []
    def at(name, tx, ty): items.append((ty * T + T - 1, NEW[name](1000), tx * T + T // 2, ty * T + T))
    for r_ in (4, 6):
        for i, nm in enumerate(['pew_l', 'pew_m', 'pew_m', 'pew_m', 'pew_r']): at(nm, 1 + i, r_)
    at('column', 7, 4); at('column', 11, 4); at('altar', 8, 2); at('altar', 9, 2); at('candelabra', 7, 2); at('candelabra', 10, 2); at('statue', 12, 3); at('font', 12, 5); at('confessional', 12, 7); at('tomb', 8, 7); at('statue', 3, 2)
    for _, p, x, y in sorted(items, key=lambda t: t[0]):
        if p.shadow:
            w, h, dx, dy = p.shadow; w2, h2 = w * 2, h * 2
            big.alpha_composite(shadow_ellipse(w2, h2), (int(x * 2 + dx * 2 - w2 / 2), int(y * 2 + dy * 2 - h2 / 2)))
        big.alpha_composite(p.im, (int(x * 2 - p.pivot[0]), int(y * 2 - p.pivot[1])))
    big = big.resize((big.width * 3, big.height * 3), Image.NEAREST)
    big.convert('RGB').save('/tmp/cathedral_props_preview.png'); print(big.size)
