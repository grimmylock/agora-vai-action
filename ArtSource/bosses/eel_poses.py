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
OVERLAP_ARC = 45                              # two parts of the body this far apart along it are different passes when they touch on screen
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

def frames(P, screen_up=True):
    """tangent and the back-side normal per point. screen_up: the normal that points up the screen (right when vertical) - for his own
    near-horizontal trunks, where up is the fin side. Not for a pose: there the fin stays on the LEFT of the way the body goes, all the
    way round (screen_up flips sides the moment the body heads straight down, tearing the texture along that line)"""
    T = np.gradient(P, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True) + 1e-9
    n = np.stack([T[:, 1], -T[:, 0]], 1)
    if screen_up:
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

def paint(canvas, P, order, ds=0.35, dv=0.5):
    """Bend the body along path P (head end first, 1 px apart) by painting ALONG the spine: every tiny step (ds px) lays down its own
    cross-section line of his pixels (v from the belly side to the back side, dv apart). All the samples go through one depth test, and
    the rule is the simplest that can't patch: the part of the body nearer the head is always in front of what comes after it, so where one
    coil passes over another (or the neck over the loops) the same one is on top along the whole crossing. (Ordering by height instead put some
    of a coil in front of the other and some behind: patches with straight edges. A nearest-spine-point test per pixel tore the texture
    wherever the body bent tighter than it is thick.) On the inside of a tight bend the body is thinned to the bend's radius, so the cross-sections can't fold over each other."""
    n = len(P); T, U = frames(P, screen_up=False); H, W = canvas.shape[:2]; order, order2, wmix = (np.array(x) for x in order)
    dT = np.gradient(T, axis=0); curv = np.linalg.norm(dT, axis=1)                         # 1/radius
    k9 = 5; curv = np.convolve(np.pad(curv, k9, mode="edge"), np.ones(2 * k9 + 1) / (2 * k9 + 1), "valid")
    toward_up = (dT * U).sum(1)                                                            # > 0: the path curves toward the back side
    rad = 1.0 / np.maximum(curv, 1e-3)
    sel = np.arange(0.0, n - 1.0001, ds)
    j0 = np.floor(sel).astype(int); f = (sel - j0)[:, None]; j1 = np.minimum(j0 + 1, n - 1)
    pos = P[j0] * (1 - f) + P[j1] * f; nrm = U[j0] * (1 - f) + U[j1] * f; nrm /= np.linalg.norm(nrm, axis=1, keepdims=True) + 1e-9
    kk = order[j0]; up = UP_EXT[kk] + 1.0; dn = DN_EXT[kk] + 1.0
    r = rad[j0]; inner_up = toward_up[j0] > 0
    up = np.where(inner_up, np.minimum(up, 0.85 * r), up); dn = np.where(~inner_up, np.minimum(dn, 0.85 * r), dn)       # thin the inside of a tight bend
    vmax = int(np.ceil(max(up.max(), dn.max()) / dv)); vs = np.arange(-vmax, vmax + 1) * dv
    vv = np.broadcast_to(vs[None, :], (len(sel), len(vs)))
    okv = (vv <= up[:, None]) & (-vv <= dn[:, None])
    ox = pos[:, None, 0] + nrm[:, None, 0] * vv; oy = pos[:, None, 1] + nrm[:, None, 1] * vv
    ixo, iyo = np.floor(ox).astype(int), np.floor(oy).astype(int)
    mix = BAYER[np.clip(iyo, 0, H - 1) % 4, np.clip(ixo, 0, W - 1) % 4] < wmix[j0][:, None]
    k = np.where(mix, order2[j0][:, None], kk[:, None])
    sx = SRC[k, 0] + SRC_N[k, 0] * vv; sy = SRC[k, 1] + SRC_N[k, 1] * vv
    ix, iy = np.rint(sx).astype(int), np.rint(sy).astype(int)
    ok = okv & (ixo >= 0) & (ixo < W) & (iyo >= 0) & (iyo < H) & (ix >= 0) & (ix < SW) & (iy >= 0) & (iy < SH)
    ix, iy = np.where(ok, ix, 0), np.where(ok, iy, 0); ok &= NATIVE[iy, ix, 3] > 0
    depth = np.broadcast_to((-sel)[:, None], ok.shape)                                          # the part nearer the head is always in front
    m = ok.ravel(); idx = np.nonzero(m)[0]; idx = idx[np.argsort(depth.ravel()[idx], kind="stable")]  # far first, so the nearest is written last
    canvas[iyo.ravel()[idx], ixo.ravel()[idx]] = NATIVE[iy.ravel()[idx], ix.ravel()[idx]]
    # where one part of the body passes in front of another (their places along the body are far apart), the one behind gets a dark outline
    # along the front one's edge, as any outline does: without it their textures butt together and read as a pasted patch
    arc = np.full((H, W), -1.0); dep = np.full((H, W), -1e9)
    arc[iyo.ravel()[idx], ixo.ravel()[idx]] = np.broadcast_to(sel[:, None], ok.shape).ravel()[idx]
    dep[iyo.ravel()[idx], ixo.ravel()[idx]] = depth.ravel()[idx]
    behind = np.zeros((H, W), bool)
    for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        an = np.roll(np.roll(arc, dy, 0), dx, 1); dn_ = np.roll(np.roll(dep, dy, 0), dx, 1)
        behind |= (arc >= 0) & (an >= 0) & (np.abs(arc - an) > OVERLAP_ARC) & (dn_ > dep + 0.05)
    canvas[behind] = (20, 22, 23, 255)
    paint.last_arc = arc                                                    # for debugging: where along the body each pixel came from

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
    ring = [(cx + math.cos(t) * (rx + t * 4.0), cy + math.sin(t) * (ry + t * 3.4)) for t in np.linspace(-0.35, -0.35 + 2 * math.pi * 1.5, 150)]   # loops 25 px apart
    h0 = tile(47.3, 32.2); neck = lead(h0) + M.bez((h0[0] + LEAD, h0[1]), (h0[0] + LEAD + 40, h0[1] + 6), ring[0])[1:]
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
    # 5. the cases that broke it: the body crossing itself (a figure-eight) and a tight hairpin; looked at on every change
    SW2, SH2 = 700, 420; flat = Image.new("RGBA", (SW2, SH2), (14, 20, 18, 255))
    e0 = (150, 110); eight = lead(e0) + M.bez((e0[0] + LEAD, e0[1]), (260, 150), (330, 210), (420, 270), (480, 210), (420, 150), (330, 210), (240, 270), (190, 210), (240, 150), (330, 210), (400, 300), (520, 330))[1:]
    h0_ = (120, 70); hairpin = lead(h0_) + M.bez((h0_[0] + LEAD, h0_[1]), (300, 75), (400, 110), (380, 170), (300, 185), (180, 180), (150, 230), (220, 280), (400, 285), (560, 260))[1:]
    for name, path in (("eight", eight), ("hairpin", hairpin)):
        t = flat.copy(); t.alpha_composite(Image.fromarray(eel_layer(SW2, SH2, M.resample(path), -1)))
        t.resize((SW2 * 2, SH2 * 2), Image.NEAREST).convert("RGB").save(os.path.join(out, f"eel_stress_{name}.png"))
    print("written", out)
