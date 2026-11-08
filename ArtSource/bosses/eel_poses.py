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

# ------------------------------------------------------------------ damage marks (Yan: "damage marks, similar to how the brain has its phases split by the sprite
# changing ... at 75%, 50%, 25%"). Wounds are drawn onto his pixels at fixed places along his spine and on the head, so every pose and animation carries them.
# Stage 1 (75%): gashes and scratches; 2 (50%): the gashes open, a cracked eye, a bleeding cheek; 3 (25%): open wounds with bone, the eye bloodied, drips.
# set_stage(n) swaps NATIVE and HEAD for the damaged ones; the animations render each stage and keep what differs from stage 0 as a thin overlay.
import random
BLOOD = {"ex": (28, 12, 14), "dk": (58, 12, 16), "bl": (104, 20, 24), "br": (152, 36, 36), "fl": (178, 108, 100), "bn": (210, 200, 174)}
NATIVE_CLEAN, HEAD_CLEAN = NATIVE.copy(), HEAD.copy()

def _dot(a, x, y, c, only_light=False):
    x, y = int(round(x)), int(round(y))
    if 0 <= y < a.shape[0] and 0 <= x < a.shape[1] and a[y, x, 3] > 0 and (not only_light or int(a[y, x, :3].sum()) > 330): a[y, x, :3] = BLOOD[c]

def _line(a, p0, p1, c, shadow=True, width=1):
    n = int(max(abs(p1[0] - p0[0]), abs(p1[1] - p0[1]), 1))
    for i in range(n + 1):
        x = p0[0] + (p1[0] - p0[0]) * i / n; y = p0[1] + (p1[1] - p0[1]) * i / n
        if shadow: _dot(a, x, y + 1, "dk")
        for w in range(width): _dot(a, x, y + w, c)

def _blob(a, cx, cy, rx, ry, drip=0, bone=True, ang=0.0):
    for dy in range(-int(ry) - 2, int(ry) + 3):
        for dx in range(-int(rx) - 2, int(rx) + 3):
            ux, uy = (dx * math.cos(ang) + dy * math.sin(ang)) / rx, (-dx * math.sin(ang) + dy * math.cos(ang)) / ry; d = ux * ux + uy * uy
            if d <= 1.0: _dot(a, cx + dx, cy + dy, "ex" if d < 0.55 else "bl")
            elif d <= 1.45: _dot(a, cx + dx, cy + dy, "dk")
    if bone:
        _dot(a, cx - 1, cy, "bn"); _dot(a, cx, cy, "bn"); _dot(a, cx + 1, cy - 1, "fl")
    for j in range(drip): _dot(a, cx + (1 if j % 4 == 0 else 0), cy + ry + 1 + j, "bl")

def _sites():
    r = random.Random(77); out = []
    for (lo, hi), n in ((A, 10), (B, 11), (C, 8)):
        for j in range(n):
            k = max(lo, min(hi - 1, int(lo + (j + 0.5) * (hi - lo) / n + r.uniform(-6, 6))))
            if UP_EXT[k] + DN_EXT[k] < 18: continue                                          # too thin (the tail's tip)
            out.append((k, r.uniform(-DN_EXT[k] * 0.55, UP_EXT[k] * 0.3), r.uniform(-1.0, 1.0), r.choice(("gash", "gash", "patch", "fin"))))
    r.shuffle(out); n = len(out)
    return [(k, v, ang, kind, 1 if i < n // 3 else 2 if i < 2 * n // 3 else 3) for i, (k, v, ang, kind) in enumerate(out)]
SITES = _sites()

def damaged_native(stage):
    a = NATIVE_CLEAN.copy()
    for (k, v, ang, kind, onset) in SITES:
        if stage < onset: continue
        lvl = stage - onset + 1; c = SRC[k] + SRC_N[k] * v; t = SRC_T[k]; nrm = SRC_N[k]
        d = np.array([t[0] * math.cos(ang * 0.9) - t[1] * math.sin(ang * 0.9), t[0] * math.sin(ang * 0.9) + t[1] * math.cos(ang * 0.9)])
        if kind == "fin":                                                                    # the fin torn: a ragged bloody bite out of its edge
            c = SRC[k] + nrm * (UP_EXT[k] - 5); L = 8 + 6 * lvl
            for q in range(-L // 2, L // 2 + 1):
                for w in range(0, 3 + lvl * 2):
                    col = "dk" if w > 2 + lvl else "bl" if (q + w) % 3 else "br"
                    _dot(a, *(c + t * q + nrm * (w - 2)), col)
        elif kind == "patch":                                                                # scales lost, the flesh showing
            rx, ry = 4 + 2 * lvl, 2.5 + 1.2 * lvl
            for dy in range(-12, 13):
                for dx in range(-12, 13):
                    u = dx * t[0] + dy * t[1]; w_ = -dx * t[1] + dy * t[0]; dd = (u / rx) ** 2 + (w_ / ry) ** 2
                    if dd <= 1.0: _dot(a, c[0] + dx, c[1] + dy, "fl" if dd < 0.5 and lvl > 1 else "br" if dd < 0.7 else "bl")
                    elif dd <= 1.4: _dot(a, c[0] + dx, c[1] + dy, "dk")
        else:
            if lvl == 1: _line(a, c, c + d * 12, "bl", width=2); _dot(a, c[0], c[1], "br")
            elif lvl == 2:
                _line(a, c, c + d * 16, "dk", width=3); _line(a, c + (0, -1), c + d * 16 + (0, -1), "br", shadow=False)
                for q in (3, 8, 13): _dot(a, *(c + d * q + (0, 2)), "fl")
            else: _blob(a, c[0], c[1], 8.5, 5.0, drip=14, ang=math.atan2(d[1], d[0]))
        if lvl >= 2 and kind != "fin":                                                       # a streak of blood running down the flank toward the belly
            for q in range(0, 10 + 5 * lvl):
                _dot(a, *(c - nrm * (3 + q) + (0, 0)), "bl" if q % 5 else "br")
    return a

def damaged_head(stage):
    h = HEAD_CLEAN.copy(); light = (h[:, :, :3].astype(int).sum(2) > 420) & (h[:, :, 3] > 0); light[60:] = False
    ys, xs = np.nonzero(light); ex, ey = (xs.mean(), ys.mean()) if len(xs) else (40, 40)                    # the blind eye's centre, found
    if stage >= 1: _line(h, (ex + 12, ey + 2), (ex + 26, ey + 14), "bl"); _line(h, (ex + 16, ey - 6), (ex + 24, ey - 2), "br")
    if stage >= 2:
        for pa, pb in (((ex - 6, ey - 5), (ex, ey)), ((ex, ey), (ex + 5, ey + 6)), ((ex, ey), (ex + 6, ey - 4))): _line(h, pa, pb, "dk", shadow=False)   # the eye cracks
        for j in range(10): _dot(h, ex + 1, ey + 7 + j, "bl")                                              # a bloody tear
        _line(h, (ex - 30, ey + 14), (ex - 18, ey + 18), "br")
    if stage >= 3:
        for dy in range(0, 8):
            for dx in range(-7, 8):
                if dx * dx + dy * dy <= 49 and (dy > 0 or dx > 0): _dot(h, ex + dx, ey + dy, "br" if (dx + dy) % 3 else "bl", only_light=True)   # the eye goes bloodshot
        _blob(h, ex + 22, ey + 10, 5.5, 3.4, drip=9, ang=0.4)
    return h

def _mouth():
    """the inside of the mouth in the head sprite (the maroon), as an offset from the point the body grows from: where a swallowed or coughed-up body goes"""
    r, g, b = (HEAD_CLEAN[:, :, i].astype(int) for i in range(3)); m = (HEAD_CLEAN[:, :, 3] > 0) & (r > g + 22) & (r > 50) & (r < 175) & (b < 90)
    ys, xs = np.nonzero(m); return (xs.mean() - HEAD_ANCHOR[0], ys.mean() - HEAD_ANCHOR[1])
MOUTH = _mouth()
def mouth_at(at, face=-1):
    """where the mouth is on the canvas for a head placed at `at` (the end of the path)"""
    return (at[0] + (MOUTH[0] if face == -1 else -MOUTH[0]), at[1] + MOUTH[1])

def set_stage(stage):
    """swap in the stage-0..3 body and head (0 = clean); the pose functions read NATIVE and HEAD"""
    global NATIVE, HEAD
    NATIVE = NATIVE_CLEAN if stage == 0 else damaged_native(stage); HEAD = HEAD_CLEAN if stage == 0 else damaged_head(stage)

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

def paint(canvas, P, order, ds=0.35, dv=0.5, bulge=None):
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
    # a swallowed (or coughed-up) lump travelling along the body: bulge = (arc position, half-width, amount); the body swells there and his texture stretches over it
    bb = 1.0 + bulge[2] * np.exp(-((sel - bulge[0]) / bulge[1]) ** 2) if bulge else np.ones(len(sel))
    up = up * bb; dn = dn * bb
    vmax = int(np.ceil(max(up.max(), dn.max()) / dv)); vs = np.arange(-vmax, vmax + 1) * dv
    vv = np.broadcast_to(vs[None, :], (len(sel), len(vs)))
    okv = (vv <= up[:, None]) & (-vv <= dn[:, None])
    ox = pos[:, None, 0] + nrm[:, None, 0] * vv; oy = pos[:, None, 1] + nrm[:, None, 1] * vv
    ixo, iyo = np.floor(ox).astype(int), np.floor(oy).astype(int)
    mix = BAYER[np.clip(iyo, 0, H - 1) % 4, np.clip(ixo, 0, W - 1) % 4] < wmix[j0][:, None]
    k = np.where(mix, order2[j0][:, None], kk[:, None])
    vs_ = vv / bb[:, None]; sx = SRC[k, 0] + SRC_N[k, 0] * vs_; sy = SRC[k, 1] + SRC_N[k, 1] * vs_
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

def eel_layer(W, H, P, face, bulge=None):
    """a transparent W x H layer holding the eel along path P: the head at its start, facing `face`"""
    layer = np.zeros((H, W, 4), np.uint8); P = np.array(P)
    paint(layer, P, body_order(len(P)), bulge=bulge); put_head(layer, P[0], face)
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
