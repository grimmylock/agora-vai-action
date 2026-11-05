"""Poses of Yan's eel (eel_native.py recovers his drawing to 480x268 game pixels) bent along any path, without redrawing it: his
spine is traced (the silhouette's midline, walked from the neck to the tail tip), and every pose pixel takes the colour of the source
pixel at the same place along the spine and the same distance across it (back up the screen, as in his drawing). The head is cut out
whole and only flipped, never rotated. Only the straight stretches of the trunk are used, repeated to make it as long as a pose needs
(his one S is 34 tiles of spine; the trailing tail is his own).
    python3 ArtSource/bosses/eel_poses.py <depths_render.png>   -> maps/renders/boss_eel/ (reviews, 1:1 in the real basin)
"""
import sys, os, math
import numpy as np
from PIL import Image, ImageDraw
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_eel as M

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
NATIVE = np.array(Image.open(os.path.join(ROOT, "ArtSource", "refs", "eel", "eel_native.png")))
MASK = NATIVE[:, :, 3] > 0; SH, SW = MASK.shape
# the stretches of his spine used for the body, as indices into the traced spine (found by looking at the trace: A the top trunk, B the
# lower trunk, C the tail); HEAD is the cut-out and the spine point it grows from
A, B, C = (0, 232), (362, 625), (690, 1085)
FADE = 10                                     # half the dithered cross-fade where his upper trunk meets the lower one
HEAD_BOX = (28, 8, 152, 107)

def _ray(p, n, maxd=90, gap=5):
    last = 0
    for k in np.arange(0, maxd, 0.5):
        x, y = int(round(p[0] + n[0] * k)), int(round(p[1] + n[1] * k))
        if not (0 <= x < SW and 0 <= y < SH): break
        if MASK[y, x]: last = k
        elif k - last > gap: break
    return last

def trace():
    col = 150; ys = np.nonzero(MASK[:, col])[0]; runs = np.split(ys, np.nonzero(np.diff(ys) > 1)[0] + 1)
    p = np.array([col, (runs[0][0] + runs[0][-1]) / 2.0]); d = np.array([1.0, 0.0]); pts = []
    for _ in range(2600):
        n = np.array([d[1], -d[0]]); a, b = _ray(p, -n), _ray(p, n)
        if a + b < 4: break
        c = p + n * (b - a) / 2; pts.append(c)
        if len(pts) >= 8:
            dd = c - pts[-8]; dd /= np.linalg.norm(dd) + 1e-9; d = 0.7 * d + 0.3 * dd; d /= np.linalg.norm(d)
        p = c + d
    P = np.array(pts); k = 9                                                         # smooth it: a moving average
    pad = np.pad(P, ((k, k), (0, 0)), mode="edge"); Ps = np.stack([np.convolve(pad[:, i], np.ones(2 * k + 1) / (2 * k + 1), "valid") for i in (0, 1)], 1)
    return Ps

def frames(P):
    """tangent and the normal that points up the screen (right when vertical), per point"""
    T = np.gradient(P, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True) + 1e-9
    n = np.stack([T[:, 1], -T[:, 0]], 1)
    flip = (n[:, 1] > 1e-3) | ((np.abs(n[:, 1]) <= 1e-3) & (n[:, 0] < 0)); n[flip] *= -1
    return T, n

SRC = trace(); SRC_T, SRC_N = frames(SRC)

def extents():
    """how far his silhouette reaches from the spine on the back side (up) and the belly side, per spine point: a pose never samples
    beyond it, so it can't pick up the neighbouring trunk of his S"""
    up = np.array([_ray(SRC[k], SRC_N[k], 60, 4) for k in range(len(SRC))]); dn = np.array([_ray(SRC[k], -SRC_N[k], 60, 4) for k in range(len(SRC))])
    return up, dn

UP_EXT, DN_EXT = extents()

def head_sprite():
    h = NATIVE.copy(); x0, y0, x1, y1 = HEAD_BOX
    out = np.zeros_like(h); out[y0:y1, x0:x1] = h[y0:y1, x0:x1]
    ys, xs = np.nonzero(out[:, :, 3]); bx0, by0 = xs.min(), ys.min()
    img = out[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    return img, (SRC[A[0]][0] - bx0, SRC[A[0]][1] - by0)                                  # the sprite and where the body grows from it

HEAD, HEAD_ANCHOR = head_sprite()

def body_order(length):
    """source indices for a body `length` points long: the top trunk, then the lower trunk (repeated as needed), then his tail"""
    tail = list(range(C[0], C[1])); a = list(range(A[0], A[1])); b = list(range(B[0], B[1]))
    order = a[:]; fwd = True
    while len(order) + len(tail) < length:            # back and forth over the lower trunk: every seam is a mirror, so the texture runs on
        order += b if fwd else b[::-1]; fwd = not fwd
    order = order[:max(0, length - len(tail))] + tail
    order = order[:length] if len(order) >= length else order + [C[1] - 1] * (length - len(order))
    order2 = list(order); w = [0.0] * len(order)
    sj = A[1] - A[0]                                    # where the top trunk ends: both streams run on a little way, B's from before its start
    for j in range(max(0, sj - FADE), min(len(order), sj + FADE)):
        order[j] = A[1] + (j - sj) if j >= sj else order[j]; order2[j] = B[0] + (j - sj); w[j] = (j - (sj - FADE) + 0.5) / (2 * FADE)
    return order, order2, w

BAYER = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16.0

def paint(canvas, P, order, chunk=12, reach=48):
    """bend the body along path P (head end first, 1 px apart): chunks back to front by height, so it coils over and under itself"""
    n = len(P); T, U = frames(P); H, W = canvas.shape[:2]; order, order2, wmix = (np.array(x) for x in order)
    ch = list(range(0, n, chunk)); ch.sort(key=lambda c: P[min(n - 1, c + chunk // 2)][1])
    for c0 in ch:
        c1 = min(n, c0 + chunk); lo, hi = max(0, c0 - 24), min(n, c1 + 24); seg = P[lo:hi]
        x0, x1 = int(seg[:, 0].min()) - reach, int(seg[:, 0].max()) + reach; y0, y1 = int(seg[:, 1].min()) - reach, int(seg[:, 1].max()) + reach
        x0, y0, x1, y1 = max(0, x0), max(0, y0), min(W, x1), min(H, y1)
        if x1 <= x0 or y1 <= y0: continue
        gy, gx = np.mgrid[y0:y1, x0:x1]; px = gx.ravel() + 0.5; py = gy.ravel() + 0.5
        d2 = (px[:, None] - seg[None, :, 0]) ** 2 + (py[:, None] - seg[None, :, 1]) ** 2
        j = np.argmin(d2, axis=1) + lo; own = (j >= c0) & (j < c1) & (d2.min(axis=1) < reach * reach)
        v = (px - P[j, 0]) * U[j, 0] + (py - P[j, 1]) * U[j, 1]
        along = (px - P[j, 0]) * T[j, 0] + (py - P[j, 1]) * T[j, 1]
        own &= ~(((j == 0) & (along < -0.5)) | ((j == n - 1) & (along > 0.5)))        # nothing past the ends of the spine (it would smear the end column out)
        jj = np.clip(j, 0, n - 1); k = np.where(BAYER[(py.astype(int)) % 4, (px.astype(int)) % 4] < wmix[jj], order2[jj], order[jj])
        sx = SRC[k, 0] + SRC_N[k, 0] * v; sy = SRC[k, 1] + SRC_N[k, 1] * v
        ix, iy = np.rint(sx).astype(int), np.rint(sy).astype(int)
        ok = own & (ix >= 0) & (ix < SW) & (iy >= 0) & (iy < SH) & (v <= UP_EXT[k] + 1) & (-v <= DN_EXT[k] + 1)
        ix, iy = np.where(ok, ix, 0), np.where(ok, iy, 0); ok &= NATIVE[iy, ix, 3] > 0
        sel = np.nonzero(ok)[0]
        canvas[py[sel].astype(int), px[sel].astype(int)] = NATIVE[iy[sel], ix[sel]]

def put_head(canvas, at, face):
    """paste the head so the body grows from `at` (a point on the path): face=-1 as drawn (looking left), +1 mirrored"""
    h = HEAD if face == -1 else HEAD[:, ::-1]; ax, ay = HEAD_ANCHOR; ax = ax if face == -1 else h.shape[1] - 1 - ax
    X, Y = int(round(at[0] - ax)), int(round(at[1] - ay)); H, W = canvas.shape[:2]
    for j in range(h.shape[0]):
        for i in range(h.shape[1]):
            if h[j, i, 3] and 0 <= Y + j < H and 0 <= X + i < W: canvas[Y + j, X + i] = h[j, i]

def outline_layer(layer):
    m = layer[:, :, 3] > 0; nb = np.zeros_like(m)
    nb[1:] |= m[:-1]; nb[:-1] |= m[1:]; nb[:, 1:] |= m[:, :-1]; nb[:, :-1] |= m[:, 1:]
    o = layer.copy(); o[nb & ~m] = (20, 22, 23, 255); return o

LEAD = 26                                       # a straight neck behind the head, as he drew it, before the body bends
def lead(at): return [(at[0] + k, at[1]) for k in range(0, LEAD)]

def eel_layer(W, H, P, face):
    """a transparent W x H layer holding the eel along path P: the head at its start, facing `face`"""
    layer = np.zeros((H, W, 4), np.uint8); P = np.array(P)
    paint(layer, P, body_order(len(P))); put_head(layer, P[0], face)
    return outline_layer(layer)

def shadow(layer):
    m = layer[:, :, 3] > 0; o = np.zeros_like(layer); o[m] = M.SHADOW; return o

if __name__ == "__main__":
    out = os.path.join(ROOT, "maps", "renders", "boss_eel"); os.makedirs(out, exist_ok=True)
    print("spine", len(SRC), "steps; head", HEAD.shape[:2][::-1], "anchor", tuple(np.round(HEAD_ANCHOR, 1)), "body sections", A, B, C)
    ar = Image.open(os.path.join(ROOT, "Assets", "Resources", "Sprites", "armor_atlas.png")).convert("RGBA")
    man = ar.crop((0, 0, 16, 16)).resize((32, 32), Image.NEAREST)
    Bg = M.basin(sys.argv[1]); W, H = Bg.size; tile = M.tile
    # 1. his drawing as it is, laid in the basin at 1:1: 15 tiles across, with a player for scale
    nat = Image.fromarray(NATIVE); a = Bg.copy(); a.alpha_composite(nat, (30, 50)); a.alpha_composite(man, tile(33, 26.5))
    M.label(a, "1:1 - Yan's eel exactly as he drew it (480x268 art pixels = 15 tiles), a player for scale")
    a.convert("RGB").save(os.path.join(out, "eel_ref_basin.png"))
    # 2. the strike: wrapped 1.5 times round a stand-in 3x3 platform, bent from his own pixels
    px0, py0 = tile(44, 34); pw = 3 * 32; cx, cy = px0 + pw / 2, py0 + pw / 2; rx, ry = pw / 2 + 36, pw / 2 + 31
    ring = [(cx + math.cos(t) * (rx + t * 1.2), cy + math.sin(t) * (ry + t * 1.0)) for t in np.linspace(-0.35, -0.35 + 2 * math.pi * 1.5, 150)]
    h0 = tile(47.3, 32.2); neck = lead(h0) + M.bez((h0[0] + LEAD, h0[1]), (h0[0] + LEAD + 14, h0[1] + 20), ring[0])[1:]
    tail = M.bez(ring[-1], tile(41.5, 35.5), tile(37.5, 34.2), tile(36.2, 37.8), tile(40, 39.4), tile(48, 38.8))
    P = M.resample(neck + ring + tail[1:])
    lay = eel_layer(W, H, P, -1)
    c = Bg.copy(); c.alpha_composite(Image.fromarray(platform_back := np.zeros((H, W, 4), np.uint8)))
    c.alpha_composite(Image.fromarray(M.platform(pw, pw)), (px0, py0 - 6)); c.alpha_composite(man, (int(cx - 16), int(cy - 26)))
    # the platform is in the middle of the coils: the body's far half goes behind it
    behind = np.zeros_like(lay); inside = (np.arange(H)[:, None] < cy) & np.ones((1, W), bool)
    behind[inside] = lay[inside]; front = lay.copy(); front[inside] = 0
    c2 = Bg.copy(); c2.alpha_composite(Image.fromarray(behind)); c2.alpha_composite(Image.fromarray(M.platform(pw, pw)), (px0, py0 - 6))
    c2.alpha_composite(man, (int(cx - 16), int(cy - 26))); c2.alpha_composite(Image.fromarray(front))
    M.label(c2, f"1:1 - the strike, bent from his pixels: wrapped 1.5 times round a 3x3 platform (stand-in), {len(P) // 32} tiles of body")
    c2.convert("RGB").save(os.path.join(out, "eel_strike_ref.png"))
    # 3. half out of the water: coiled at the base, the neck up the screen, the head over the player
    base = tile(47, 37.6)
    coil = [(base[0] + math.cos(t) * (76 - t * 5), base[1] + math.sin(t) * (40 - t * 3)) for t in np.linspace(0, 2 * math.pi * 1.4, 130)]
    h1 = tile(44.0, 33.4); col = lead(h1) + M.bez((h1[0] + LEAD, h1[1]), tile(46.3, 34.4), tile(46.8, 35.8), tile(46.0, 36.8), tile(47.4, 37.4), base)[1:]
    tl = M.bez(coil[-1], tile(41, 38.6), tile(37, 38.8))
    P3 = M.resample(col + coil + tl[1:]); lay3 = eel_layer(W, H, P3, -1)
    r = Bg.copy(); r.alpha_composite(man, tile(41.5, 34.8)); r.alpha_composite(Image.fromarray(lay3))
    M.label(r, "1:1 - half out of the water, about to strike (bent from his pixels); the player in front")
    r.convert("RGB").save(os.path.join(out, "eel_rear_ref.png"))
    # 4. lurking: only its shadow under the black water ("the water version looks fine"), the blind eyes glimmering
    h2 = tile(41.0, 33.6); sw = lead(h2) + M.bez((h2[0] + LEAD, h2[1]), tile(45.0, 35.2), tile(48.5, 33.8), tile(52.0, 36.0), tile(55.0, 34.6), tile(56.6, 36.4), tile(53.5, 38.0), tile(47.5, 37.2), tile(42.5, 38.4), tile(38.5, 37.8))[1:]
    lay4 = eel_layer(W, H, M.resample(sw), -1)
    sh = shadow(lay4); k = Bg.copy(); k.alpha_composite(man, tile(42, 28.0)); k.alpha_composite(Image.fromarray(sh))
    M.label(k, "1:1 - lurking: only its shadow under the black water (the player on the ledge)")
    k.convert("RGB").save(os.path.join(out, "eel_lurk_ref.png"))
    print("written", out)
