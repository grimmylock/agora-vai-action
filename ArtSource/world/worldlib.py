# Shared helpers for the 3/4 world art (Batch 30). Locked agora-vai style: 1px #141617 outline on
# props, flat 3-4 tone shading, light from the top-left, muted post-disaster palette.
import math, random
import numpy as np
from PIL import Image

K = (20, 22, 23, 255)
SHADOW = (50, 33, 37)          # #322125, composited at ~60%

def hx(h, a=255):
    h = h.lstrip('#'); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)

def ramp(*hexes): return [hx(h) for h in hexes]

# ---------------- palettes (dark -> light), from STYLE_RESEARCH.md section 8 + game palette
P = {
    "forest": ramp("#2B2A1E", "#3D3B28", "#4F4C32", "#62603F"),
    "grass": ramp("#2f3524", "#3a4130", "#454d37", "#535a40", "#656a4a"),
    "darkgrass": ramp("#262b20", "#2e3427", "#373e2e", "#424a36"),
    "drygrass": ramp("#5c5638", "#6f6745", "#857b52", "#9a8f61"),
    "dirt": ramp("#332b22", "#40372b", "#4e4434", "#5d5240"),
    "mud": ramp("#2E241B", "#3a2e23", "#43352A", "#5A4838"),
    "water": ramp("#1B2422", "#25332F", "#33463F", "#4F6458"),
    "asphalt": ramp("#2c2e31", "#35383b", "#3f4245", "#4c4f53"),
    "concrete": ramp("#4c4b46", "#5d5c56", "#6e6c65", "#807d74", "#939085"),
    "sand": ramp("#6b5f45", "#7d7052", "#8e8160", "#a0936f"),
    "gravel": ramp("#3f3d3a", "#4f4c47", "#605c55", "#757067"),
    "cave": ramp("#232220", "#2c2a27", "#36332f", "#423e39"),
    "floor": ramp("#3a3026", "#463a2e", "#524436", "#5e4f3e"),
    "lab": ramp("#4a5052", "#5a6163", "#6b7375", "#7c8486"),
    "foliage": ramp("#2e3820", "#3d4a29", "#4f5c33", "#667040", "#7e8550"),
    "pine": ramp("#17221c", "#1f2d25", "#283a2f", "#34493a", "#425a48"),
    "redleaf": ramp("#2a1416", "#4a1e1e", "#6e2a24", "#93402e", "#b45c3a"),
    "deadwood": ramp("#2a231e", "#3d3229", "#524436", "#6a5a48"),
    "bark": ramp("#241b14", "#33271c", "#463625", "#5a4630"),
    "rock": ramp("#2c2d2e", "#3d3e3f", "#525353", "#6a6a68", "#83827d"),
    "rust": ramp("#3e2519", "#5a3524", "#77482e", "#93603b"),
    "brick": ramp("#3e271f", "#4a2e25", "#5c3a2e", "#6f4838", "#835a46"),
    "plaster": ramp("#4e4c45", "#605d55", "#726f66", "#858176", "#979284"),
    "steel": ramp("#26292b", "#33373a", "#454a4d", "#5c6265", "#7a8184"),
    "wood": ramp("#2e2116", "#43301f", "#5a4029", "#73543a", "#8c6a4a"),
    "olive": ramp("#2d3220", "#3c4128", "#555c3a", "#6b7349", "#7a8256"),
    "tin": ramp("#3B4245", "#525C5E", "#6B7675", "#88928D"),
    "red": ramp("#4a1612", "#7a1f1a", "#a52c22", "#c84a32"),
    "sandbag": ramp("#4a4232", "#5f5640", "#776c51", "#8f8462"),
    "moss": ramp("#3a4428", "#4c5833"),
    "tire": ramp("#1c1e20", "#2a2d30", "#3a3e42", "#50555a"),
}

def rng(seed): return random.Random(seed)

def blank(w, h): return Image.new("RGBA", (w, h), (0, 0, 0, 0))

def value_noise(w, h, cell, seed, wrap=True):
    """smooth value noise in [0,1], tileable when wrap (cell must divide w and h)"""
    r = np.random.RandomState(seed)
    gw, gh = max(1, w // cell), max(1, h // cell)
    g = r.rand(gh + 1, gw + 1)
    if wrap: g[-1, :] = g[0, :]; g[:, -1] = g[:, 0]
    ys = np.arange(h) / cell; xs = np.arange(w) / cell
    iy = np.floor(ys).astype(int); ix = np.floor(xs).astype(int)
    ty = ys - iy; tx = xs - ix
    ty = ty * ty * (3 - 2 * ty); tx = tx * tx * (3 - 2 * tx)
    iy = np.minimum(iy, gh - 1); ix = np.minimum(ix, gw - 1)
    a = g[iy][:, ix]; b = g[iy][:, ix + 1]; c = g[iy + 1][:, ix]; d = g[iy + 1][:, ix + 1]
    top = a + (b - a) * tx[None, :]; bot = c + (d - c) * tx[None, :]
    return top + (bot - top) * ty[:, None]

def fbm(w, h, seed, cells=(16, 8, 4), weights=(0.55, 0.3, 0.15)):
    n = sum(value_noise(w, h, c, seed + i * 101) * wt for i, (c, wt) in enumerate(zip(cells, weights)))
    return n / sum(weights)

def quantize(field, pal, cuts):
    """field [h,w] in 0..1 -> RGBA image picking pal[i] by thresholds cuts (len(pal)-1)"""
    idx = np.digitize(field, cuts)
    arr = np.array([pal[i] for i in range(len(pal))], dtype=np.uint8)[idx]
    return Image.fromarray(arr, "RGBA")

def outline(im, col=K, diag=False):
    a = np.array(im); al = a[..., 3] > 0; H, W = al.shape
    o = a.copy()
    nb = np.zeros_like(al)
    nb[1:, :] |= al[:-1, :]; nb[:-1, :] |= al[1:, :]; nb[:, 1:] |= al[:, :-1]; nb[:, :-1] |= al[:, 1:]
    if diag:
        nb[1:, 1:] |= al[:-1, :-1]; nb[:-1, :-1] |= al[1:, 1:]; nb[1:, :-1] |= al[:-1, 1:]; nb[:-1, 1:] |= al[1:, :-1]
    m = nb & ~al
    o[m] = col
    return Image.fromarray(o, "RGBA")

def pad(im, p=1):
    out = blank(im.width + 2 * p, im.height + 2 * p); out.alpha_composite(im, (p, p)); return out

def px(im, x, y, c):
    if 0 <= x < im.width and 0 <= y < im.height: im.putpixel((x, y), c)

def disc(im, cx, cy, r, c, squash=1.0):
    for y in range(int(cy - r * squash) - 1, int(cy + r * squash) + 2):
        for x in range(int(cx - r) - 1, int(cx + r) + 2):
            if ((x - cx) / r) ** 2 + ((y - cy) / (r * squash)) ** 2 <= 1.0: px(im, x, y, c)

def shade_blob(im, cx, cy, r, pal, seed, squash=1.0, light=(-0.55, -0.6)):
    """a round leaf/rock bundle: dark crescent bottom-right, light spot top-left (3-4 tones), stippled edge"""
    rr = rng(seed)
    lx, ly = light
    for y in range(int(cy - r * squash) - 1, int(cy + r * squash) + 2):
        for x in range(int(cx - r) - 1, int(cx + r) + 2):
            dx, dy = (x - cx) / r, (y - cy) / (r * squash)
            d = dx * dx + dy * dy
            if d > 1.0 + rr.uniform(-0.12, 0.06): continue
            dl = (dx - lx * 0.6) ** 2 + (dy - ly * 0.6) ** 2      # distance to the lit spot
            v = 1.0 - dl * 0.55 + rr.uniform(-0.1, 0.1)
            if d > 0.72 and dx + dy > 0.2: v -= 0.35                 # crescent on the far side
            i = 0 if v < 0.2 else 1 if v < 0.45 else 2 if v < 0.72 else 3 if v < 0.9 else min(4, len(pal) - 1)
            px(im, x, y, pal[min(i, len(pal) - 1)])

def shadow_ellipse(w, h, alpha=150):
    im = blank(w, h)
    for y in range(h):
        for x in range(w):
            if ((x + 0.5 - w / 2) / (w / 2)) ** 2 + ((y + 0.5 - h / 2) / (h / 2)) ** 2 <= 1: im.putpixel((x, y), SHADOW + (alpha,))
    return im


def outline_sel(im, k=0.42):
    """selective outline: each edge pixel takes the darkened colour of the sprite pixel it borders
    (hue-preserving, softer than flat black - reads less 'cartoon/blocky')"""
    a = np.array(im).astype(np.int32); al = a[..., 3] > 0; H, W = al.shape
    o = a.copy()
    for y in range(H):
        for x in range(W):
            if al[y, x]: continue
            best = None
            for dx, dy in ((0, -1), (-1, 0), (1, 0), (0, 1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < W and 0 <= ny < H and al[ny, nx]:
                    c = a[ny, nx]; lum = c[0] + c[1] + c[2]
                    if best is None or lum < best[0]: best = (lum, c)
            if best is not None:
                c = best[1]; o[y, x] = (int(c[0] * k) + 6, int(c[1] * k) + 5, int(c[2] * k) + 4, 255)
    return Image.fromarray(o.astype(np.uint8), "RGBA")
