# Pallid Prowler RUN, legs redrawn as clean tubes in the style of the WALK (2026-11-12). Yan: "keep the stride and movement but define the legs better, they look blurry and I can't differentiate
# between the back legs, compare it to the walking animation that is very well defined".
# Why the previous legs were blurry: they were his thin stand-sprite legs sheared sideways (a row shear turns a 2 px leg into a 1 px diagonal line and merges near and far contours), and the walk's legs
# are thicker (a pale 3-4 px thigh, a greying shin, a dark foot, a black contour) with the far legs clearly darker than the near ones.
# Here: the stride, the stagger, the body rise and the head nod are exactly pallid_run2's (its KEYS/RISE/NOD); the four legs are drawn as fixed-width tubes by two-bone IK from the hips down to the
# foot targets (FOOT0 + dx, ground - lift), with one fixed palette per leg (no per-frame sampling, so nothing shifts), a black contour, near legs pale and far legs dark.   python3 pallid_run_legs.py <out_dir>
import math, os, sys
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as nd
import pallid_run2 as R2
import mutant_from_ref as m

STAND, N, ox, oy, W, H, LOW, AXES, BODY = R2.STAND, R2.N, R2.ox, R2.oy, R2.W, R2.H, R2.LOW, R2.AXES, R2.BODY
GROUND = oy + LOW
FOOT0 = {"near_hind": 5, "far_hind": 15, "near_fore": 27, "far_fore": 38}
FAR = {"far_hind", "far_fore"}
L1, L2 = 7.0, 6.6
# palettes sampled from the walk's legs: near legs pale to grey, far legs dark
PAL = {"near": dict(thigh=(193, 186, 168), thigh_sh=(160, 152, 138), shin=(168, 166, 150), shin_sh=(120, 118, 106), foot=(78, 78, 72)),
       "far":  dict(thigh=(132, 128, 116), thigh_sh=(96, 94, 86), shin=(104, 102, 92), shin_sh=(72, 70, 66), foot=(50, 50, 48))}

def solve(hip, ft, bend_fwd):
    dx, dy = ft[0] - hip[0], ft[1] - hip[1]; d = min(math.hypot(dx, dy), L1 + L2 - 0.2); a = math.atan2(dy, dx)
    ang = math.acos(max(-1, min(1, (L1 * L1 + d * d - L2 * L2) / (2 * L1 * d)))); k = a + (ang if bend_fwd else -ang)
    return (hip[0] + L1 * math.cos(k), hip[1] + L1 * math.sin(k))

def leg_img(name, i, drop=0):
    dx, kx, lift, air = R2.KEYS[name][i]; rise = R2.RISE[i]; hind = name.endswith("hind"); hx, hy = AXES[name][0]
    hip = (hx + ox, hy + oy - rise)                                           # the hip rides the body
    ft = (FOOT0[name] + ox + dx, GROUND - 2 - drop - lift - (rise if air else 0))     # the foot target: one row above the ground row (the contour below it is the lowest row)
    d = math.hypot(ft[0] - hip[0], ft[1] - hip[1]); mx = 0.9 * (L1 + L2)
    if d > mx: vy = ft[1] - hip[1]; ft = (hip[0] + math.copysign(math.sqrt(max(0.0, mx * mx - vy * vy)), ft[0] - hip[0]), ft[1])
    knee = solve(hip, ft, not hind)
    segs = [(hip, knee, 1.8 if hind else 1.6, 1.25), (knee, ft, 1.25, 1.0)]
    pal = PAL["far" if name in FAR else "near"]; out = np.zeros((H, W, 4), np.uint8); tot = sum(math.hypot(q[0] - p[0], q[1] - p[1]) for p, q, _, _ in segs); fx, fy = int(round(ft[0])), int(round(ft[1]))
    for y in range(H):
        for x in range(W):
            best = None; acc = 0.0
            for si, (p, q, r0, r1) in enumerate(segs):
                vx, vy = q[0] - p[0], q[1] - p[1]; ln = math.hypot(vx, vy) + 1e-9
                u = max(0.0, min(1.0, ((x - p[0]) * vx + (y - p[1]) * vy) / (ln * ln))); cx, cy = p[0] + u * vx, p[1] + u * vy
                dist = math.hypot(x - cx, y - cy); r = r0 + (r1 - r0) * u
                if dist <= r and (best is None or dist / r < best[0]): best = (dist / r, (acc + u * ln) / tot, ((x - cx) * (-vy) + (y - cy) * vx) / ln)
                acc += ln
            if best is None and y == fy and fx < x <= fx + 2: best = (1.0, 1.0, 0.0)              # a short toe forward of the foot
            if best is None: continue
            t, side = best[1], best[2]; lit = side < 0.2
            if t < 0.4: col = pal["thigh"] if lit else pal["thigh_sh"]
            elif t < 0.8: col = pal["shin"] if lit else pal["shin_sh"]
            else: col = pal["foot"]
            out[y, x, :3] = col; out[y, x, 3] = 255
    res = m.outline(Image.fromarray(out))
    if drop < 2 and int(np.where(np.array(res)[..., 3] > 0)[0].max()) > GROUND: return leg_img(name, i, drop + 1)       # a foot that ended below the ground row is raised one row
    return res

def frame(i, only=None):
    if only: return leg_img(only, i)
    rise = R2.RISE[i]; im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    for n in ("far_hind", "far_fore"): im.alpha_composite(leg_img(n, i))
    b = Image.new("RGBA", (W, H), (0, 0, 0, 0)); b.alpha_composite(R2.shear_cols(R2.haunch_cols(BODY, R2.HAUNCH[i]), 28, 38, R2.NOD[i]), (ox, oy - rise)); im.alpha_composite(b)
    for n in ("near_hind", "near_fore"): im.alpha_composite(leg_img(n, i))
    a = np.array(im); l, k = nd.label(a[..., 3] > 0, structure=np.ones((3, 3)))
    for j in range(1, k + 1):
        if (l == j).sum() < 6: a[l == j] = 0
    return Image.fromarray(a)

def save(out, ms=100):
    os.makedirs(out, exist_ok=True); fr = [frame(i) for i in range(N)]
    for i, f in enumerate(fr): f.save(os.path.join(out, f"frame{i}.png"))
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
    out = sys.argv[1] if len(sys.argv) > 1 else "mutants_anim/pallid_run_legs"; fr = save(out)
    print("lowest rows", [int(np.where(np.array(f)[..., 3] > 0)[0].max()) for f in fr], "ground", GROUND, "components", [nd.label(np.array(f)[..., 3] > 0, structure=np.ones((3, 3)))[1] for f in fr])
