# Pallid Prowler RUN cycle, third draft (2026-11-12). Yan: the first draft "had the right movement but the wrong art, the second one has bad movement but accurate art".
# This one takes the first draft's way of moving - legs posed by two-bone IK (bent, long strides, big lift, flight phase) - and gives them his art: every leg pixel's colour is taken from his
# standing sprite's own legs, unwrapped along each leg (position along the leg, side offset) like the Dread's front legs. Body, head, haunch and the jaw underside are his pixels, shifted by whole pixels.
# Frame 0 is the stand pose (his own leg pixels); the key table starts and ends near it.   python3 pallid_run3.py <out_dir>
import math, os, sys
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as nd
import pallid_run2 as R2
import mutant_from_ref as m          # his stand sprite, BODY (rows < 14 + jaw underside), PIECE (his four legs), AXES, shears

STAND, S, N, ox, oy, W, H, CUT, LOW, AXES, PIECE, BODY = R2.STAND, R2.S, R2.N, R2.ox, R2.oy, R2.W, R2.H, R2.CUT, R2.LOW, R2.AXES, R2.PIECE, R2.BODY
GROUND = oy + LOW
FAR = {"far_hind", "far_fore"}
FOOT0 = {"near_hind": 5, "far_hind": 15, "near_fore": 27, "far_fore": 38}          # where his feet stand (x), hips are AXES[...][0]
# (foot dx, lift in px, in the air)
KEYS = {"near_hind": [(0, 0, 0), (-3, 0, 0), (-8, 1, 1), (-9, 1, 1), (-2, 3, 1), (4, 2, 0), (5, 0, 0), (2, 0, 0)],
        "far_hind":  [(0, 0, 0), (-2, 0, 0), (-7, 1, 1), (-9, 1, 1), (-1, 3, 1), (3, 2, 0), (5, 0, 0), (3, 0, 0)],
        "near_fore": [(0, 0, 0), (2, 1, 0), (7, 1, 1), (9, 1, 1), (4, 3, 1), (6, 0, 0), (3, 0, 0), (-1, 0, 0)],
        "far_fore":  [(0, 0, 0), (1, 1, 0), (6, 1, 1), (9, 1, 1), (5, 3, 1), (7, 0, 0), (4, 0, 0), (-1, 0, 0)]}
RISE = [0, 0, 2, 3, 2, 0, 0, 0]
NOD = [0, 0, -1, -1, 0, 1, 1, 0]
HAUNCH = [0, 0, 1, 1, 0, -1, -1, 0]
L1, L2 = 6.8, 6.4
MAX_REACH = 0.82                                      # a leg never stretches past this share of its length (Yan: too stretched in the extension frames)

# ---- his legs unwrapped: (position along the axis 0..1, side offset px, colour)
def _axis_param(name, x, y):
    a, b = AXES[name]; vx, vy = b[0] - a[0], b[1] - a[1]; ln = math.hypot(vx, vy)
    u = ((x - a[0]) * vx + (y - a[1]) * vy) / (ln * ln); side = ((x - a[0]) * (-vy) + (y - a[1]) * vx) / ln
    return u, side, ln
TEX = {}
for n in AXES:
    pts, cols = [], []
    for y in range(CUT, LOW + 1):
        for x in range(S.shape[1]):
            if PIECE[n][y, x, 3]:
                u, side, ln = _axis_param(n, x, y); pts.append((u * ln, side)); cols.append(tuple(int(v) for v in PIECE[n][y, x, :3]))
    TEX[n] = (np.array(pts), cols, math.hypot(AXES[n][1][0] - AXES[n][0][0], AXES[n][1][1] - AXES[n][0][1]))

def tex_col(n, arc_frac, side):
    """nearest of his leg pixels in (along, side); inside the leg (|side| < 1.1) only his non-black pixels are used, so the black outline stays on the edges"""
    pts, cols, ln = TEX[n]; d = (pts[:, 0] - arc_frac * ln) ** 2 + (pts[:, 1] - side) ** 2
    d = d + np.array([0.0 if sum(c) > 150 else 1e6 for c in cols])           # colour only from his non-black pixels; the black contour is added after (m.outline), as on his own legs
    return cols[int(np.argmin(d))]

def solve(hip, ft, bend_fwd):
    dx, dy = ft[0] - hip[0], ft[1] - hip[1]; d = min(math.hypot(dx, dy), L1 + L2 - 0.2); a = math.atan2(dy, dx)
    ang = math.acos(max(-1, min(1, (L1 * L1 + d * d - L2 * L2) / (2 * L1 * d)))); k = a + (ang if bend_fwd else -ang)
    return (hip[0] + L1 * math.cos(k), hip[1] + L1 * math.sin(k))

def leg_img(name, i):
    dx, lift, air = KEYS[name][i]; rise = RISE[i] if air else 0
    hind = name.endswith("hind"); hx, hy = AXES[name][0]
    hip = (hx + ox, hy + oy - RISE[i]) if air else (hx + ox, hy + oy)
    ft = (FOOT0[name] + ox + dx, GROUND - 2 - lift - (rise if air else 0))      # the outline row below the foot is the ground row
    d = math.hypot(ft[0] - hip[0], ft[1] - hip[1]); mx = MAX_REACH * (L1 + L2)
    if d > mx:                                                              # shorten the stride, keep the foot's height: the leg never stretches past MAX_REACH of its length
        vy = ft[1] - hip[1]; hx_ = math.sqrt(max(0.0, mx * mx - vy * vy)); ft = (hip[0] + math.copysign(hx_, ft[0] - hip[0]), ft[1])
    knee = solve(hip, ft, not hind)                                       # hind: stifle forward; fore: elbow back
    segs = [(hip, knee, 1.3, 1.15), (knee, ft, 1.15, 1.0)]
    out = np.zeros((H, W, 4), np.uint8); ln_tot = sum(math.hypot(q[0] - p[0], q[1] - p[1]) for p, q, _, _ in segs); fx, fy = int(round(ft[0])), int(round(ft[1]))
    for y in range(H):
        for x in range(W):
            best = None; acc = 0.0
            for si, (p, q, r0, r1) in enumerate(segs):
                vx, vy = q[0] - p[0], q[1] - p[1]; ln = math.hypot(vx, vy) + 1e-9
                u = max(0.0, min(1.0, ((x - p[0]) * vx + (y - p[1]) * vy) / (ln * ln))); cx, cy = p[0] + u * vx, p[1] + u * vy
                dist = math.hypot(x - cx, y - cy); r = r0 + (r1 - r0) * u
                if dist <= r and (best is None or dist / r < best[0]): best = (dist / r, (acc + u * ln) / ln_tot, ((x - cx) * (-vy) + (y - cy) * vx) / ln)
                acc += ln
            if best is None and y == fy and fx < x <= fx + 2: best = (1.0, 1.0, 0.0)          # a short toe forward of the foot
            if best is None: continue
            out[y, x, :3] = tex_col(name, best[1], best[2]); out[y, x, 3] = 255
    return m.outline(Image.fromarray(out))

def frame(i, only=None):
    if i == 0 and not only:
        im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); im.alpha_composite(STAND, (ox, oy)); return im           # his stand sprite, exactly
    if only: return leg_img(only, i)
    rise = RISE[i]; im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    for n in ("far_hind", "far_fore"): im.alpha_composite(leg_img(n, i))
    b = Image.new("RGBA", (W, H), (0, 0, 0, 0)); b.alpha_composite(R2.shear_cols(R2.haunch_cols(BODY, HAUNCH[i]), 28, 38, NOD[i]), (ox, oy - rise)); im.alpha_composite(b)
    for n in ("near_hind", "near_fore"): im.alpha_composite(leg_img(n, i))
    a = np.array(im); l, k = nd.label(a[..., 3] > 0, structure=np.ones((3, 3)))
    for j in range(1, k + 1):
        if (l == j).sum() < 6: a[l == j] = 0
    return Image.fromarray(a)

def save(out, ms=100):
    os.makedirs(out, exist_ok=True); fr = [frame(i) for i in range(N)]
    for i, f in enumerate(fr): f.save(os.path.join(out, f"frame{i}.png"))
    st = Image.new("RGBA", (W, H), (0, 0, 0, 0)); st.alpha_composite(STAND, (ox, oy)); st.save(os.path.join(out, "stand.png"))
    strip = Image.new("RGBA", (W * N, H), (0, 0, 0, 0))
    for i, f in enumerate(fr): strip.alpha_composite(f, (i * W, 0))
    strip.save(os.path.join(out, "pallid_run_strip.png"))
    sc = 12; sheet = Image.new("RGBA", (W * 4 * sc, H * 2 * sc), (105, 115, 90, 255))
    for k, f in enumerate(fr): sheet.alpha_composite(f.resize((W * sc, H * sc), Image.NEAREST), ((k % 4) * W * sc, (k // 4) * H * sc))
    sheet.convert("RGB").save(os.path.join(out, "sheet.png"))
    fs = []
    for k in range(32):
        im = Image.new("RGBA", (W + 12, H + 4), (54, 49, 41, 255)); d = ImageDraw.Draw(im); g = GROUND; d.rectangle([0, g + 2, im.width, im.height], fill=(60, 55, 46, 255)); off = int(k * 5.5) % 12
        for gx in range(-12, im.width, 12): d.point((gx - off + 12, g + 4), fill=(80, 74, 62, 255))
        im.alpha_composite(fr[k % N], (6, 0)); fs.append(im.resize((im.width * 10, im.height * 10), Image.NEAREST).convert("RGB").quantize(colors=64, method=Image.MEDIANCUT, dither=Image.NONE))
    fs[0].save(os.path.join(out, f"run_{ms}ms.gif"), save_all=True, append_images=fs[1:], duration=ms, loop=0)
    return fr

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "mutants_anim/pallid_run3"; fr = save(out)
    for n in AXES:
        f = [np.array(frame(i, only=n))[..., 3] > 0 for i in range(1, N)]
        print(n, "pixels", [int(x.sum()) for x in f], "min step change", min(int((f[k] ^ f[k + 1]).sum()) for k in range(len(f) - 1)))
    print("lowest rows", [int(np.where(np.array(f)[..., 3] > 0)[0].max()) for f in fr], "ground", GROUND, "components", [nd.label(np.array(f)[..., 3] > 0, structure=np.ones((3, 3)))[1] for f in fr])
