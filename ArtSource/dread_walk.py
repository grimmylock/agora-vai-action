# Dread Prowler stand-still sprite and walk (2026-11-11), built the way the Veil's was (veil_walk.py, which Yan approved): the cut-out body, head, jaw and tail of Yan's sprite are kept pixel for pixel,
# the four legs are drawn fresh each frame. Yan: "next do the dread prowler, the one of the 'leaping' sprite, it'll need a new sprite for it just standing there and then walking, the leaping will come into play when animating the attacks".
# His original sprite is mid-leap (kept as mutants_anim/dread_leap_original.png for the attack animations); the standing pose is the same body with the legs planted.
# What is taken from the reference: 8 pictures a cycle at 125 ms, a diagonal gait (near hind + far fore, then the other pair), the body dipping twice a cycle (the reference's back drops
# about one sprite pixel at two points of the cycle), the head riding the body, the planted feet sliding back at one speed, the legs lifted and swung forward in between, only two feet on
# the ground in the two passing frames. What is kept from Yan's own Veil sprite: every pixel of the body, head, jaw and tail (the cut-out rig of canine_cutout.py, legs removed; moved only
# by whole pixels, never resampled). What is new: the four legs, drawn fresh each frame in the sprite's own colours (thin tubes with a lit edge and a flat clawed foot, positioned by a two-bone solve)
# because rotating or shearing a 3 px leg shreds it (the second and third tries).
#   python3 veil_walk.py <out_dir>
import math, os, sys, json
import numpy as np
from scipy import ndimage as nd
from PIL import Image, ImageDraw
import canine_cutout as C
import mutant_from_ref as m

ox, oy = 8, 4                           # the sprite's place inside the frame
N = 8
rig = C.Rig("dread_prowler", C.SPECS["dread_prowler"])
W, H = rig.W0 + 2 * ox, rig.H0 + 2 * oy
GROUND = oy + 47                        # the original's feet end on its row 47
LEGS = {"far_hind": ((17, 36), 0.50, True, False), "near_hind": ((11, 36), 0.00, False, False), "far_fore": ((50, 35), 0.00, True, True), "near_fore": ((44, 35), 0.50, False, True)}
L1, L2 = 6.2, 6.0
STRIDE, LIFT = 6.5, 4.0
BODY = Image.new("RGBA", rig.base.size, (0, 0, 0, 0))
for n in ("hind", "fore"): BODY.alpha_composite(rig.parts[n])
HEAD = Image.new("RGBA", rig.base.size, (0, 0, 0, 0)); HEAD.alpha_composite(rig.parts["head"]); HEAD.alpha_composite(rig.parts["jaw"])
# the leg colours: the sprite's own browns (the body is mostly the tan (99,81,68) with dark brown hollows (66,51,44) and pale streaks (142,128,108))
C_MAIN, C_HOLLOW, C_PALE, C_DARK = (99, 81, 68), (66, 51, 44), (142, 128, 108), (37, 29, 27)
OUT = (4, 1, 2, 255)

def foot(ph):
    if ph < 0.55: return (STRIDE - 2 * STRIDE * ph / 0.55, 0.0)
    u = (ph - 0.55) / 0.45
    return (-STRIDE + 2 * STRIDE * (0.5 - 0.5 * math.cos(math.pi * u)), LIFT * math.sin(math.pi * u))

def solve(hip, ft, fwd):
    dx, dy = ft[0] - hip[0], ft[1] - hip[1]; d = min(math.hypot(dx, dy), L1 + L2 - 0.2); a = math.atan2(dy, dx)
    ang = math.acos(max(-1, min(1, (L1 * L1 + d * d - L2 * L2) / (2 * L1 * d)))); k = a + (ang if fwd else -ang)
    return (hip[0] + L1 * math.cos(k), hip[1] + L1 * math.sin(k))

def seg(d, p, q, w, col):
    d.line([p, q], fill=col, width=w)

def leg_img(name, i, bob, stand=False):
    """One leg, tapered like a hyena's: a heavy thigh that narrows to the hock/knee, a thinner shin, a broad paw. Shaded in the body's own dark browns (lit edge on the left, shadow on the right)."""
    (hx, hy), ph0, far, fwd = LEGS[name]; ph = (i / N + ph0) % 1.0
    fo, fl = (0.0, 0.0) if stand else foot(ph)
    if stand: fo = {"far_hind": -2.0, "near_hind": 1.0, "far_fore": 1.0, "near_fore": -1.0}[name]
    hip = (hx + ox, hy + oy + bob - 1); ft = (hx + ox + fo + (2 if fwd else -1), GROUND - fl); kn = solve(hip, ft, fwd)
    hind = name.endswith("hind")
    R = [(3.0 if hind else 2.6, 1.9), (1.5, 1.1)]                  # radius at the start and end of the thigh, then of the shin
    a = np.zeros((H, W, 4), np.uint8)
    segs = [(hip, kn), (kn, ft)]
    fx, fy = int(round(ft[0])), int(round(ft[1]))
    for y in range(H):
        for x in range(W):
            best = None
            for si, (p, q) in enumerate(segs):
                vx, vy = q[0] - p[0], q[1] - p[1]; L2_ = vx * vx + vy * vy + 1e-9
                u = max(0.0, min(1.0, ((x - p[0]) * vx + (y - p[1]) * vy) / L2_)); cx, cy = p[0] + u * vx, p[1] + u * vy
                dist = math.hypot(x - cx, y - cy); r = R[si][0] + (R[si][1] - R[si][0]) * u
                if dist <= r and (best is None or dist / r < best[0]): best = (dist / r, si, u, (x - cx) * (-vy) + (y - cy) * vx)
            if best: a[y, x] = (*C_MAIN, 255); a[y, x, 3] = 255; a[y, x, :3] = 0; a[y, x, 0] = 1; a[y, x, 1] = best[1]; a[y, x, 2] = int(best[2] * 100)   # scratch: seg, u
    # the paw: a broad flat foot with a claw
    paw = [(fx + dx, fy + dy) for dx in range(-2, 4) for dy in (0,)] + [(fx - 1 + dx, fy - 1) for dx in range(0, 4)]
    for (x, y) in paw:
        if 0 <= x < W and 0 <= y < H: a[y, x] = (1, 1, 100, 255)
    out = np.zeros((H, W, 4), np.uint8); k = 0.78 if far else 1.0
    ys, xs = np.where(a[..., 3] > 0)
    for y, x in zip(ys, xs):
        si, u = int(a[y, x, 1]), a[y, x, 2] / 100.0; t = (si + u) / 2.0
        left = x < (hip[0] + (kn[0] - hip[0]) * min(1, t * 2)) if si == 0 else x < kn[0] + (ft[0] - kn[0]) * u
        if y >= fy - 1 and x >= fx - 2: col = C_DARK                         # the paw: dark
        elif si == 0 and u < 0.35: col = C_MAIN if left else C_HOLLOW         # the thigh's muscle: lit left, shadow right
        elif si == 0: col = C_MAIN if left and (x + y) % 2 == 0 else C_HOLLOW
        else: col = C_HOLLOW if (x + y) % 3 else C_DARK                       # the shin: dark with a little variation
        if si == 0 and u < 0.15 and left and (x + y) % 3 == 0: col = C_PALE      # a few pale hairs where the thigh catches the light
        if (x, y) == (fx + 3, fy): col = (210, 197, 162)                        # the claw: bone
        out[y, x, :3] = tuple(int(v * k) for v in col); out[y, x, 3] = 255
    return Image.fromarray(out)

def frame(i, only=None, stand=False):
    bob = 0 if stand else int(round(0.9 * math.sin(2 * math.pi * (i / N * 2) + 0.6))); sway = 0 if stand else int(round(0.9 * math.sin(2 * math.pi * i / N + 2.0)))
    far = [n for n in LEGS if LEGS[n][2]]; near = [n for n in LEGS if not LEGS[n][2]]
    if only: return leg_img(only, i, bob, stand)
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    for n in far: im.alpha_composite(leg_img(n, i, bob, stand))
    im.alpha_composite(BODY, (ox, oy + bob)); im.alpha_composite(HEAD, (ox, oy + bob + sway))
    for n in near: im.alpha_composite(leg_img(n, i, bob, stand))
    a = np.array(im); l, k = nd.label(a[..., 3] > 0, structure=np.ones((3, 3)))      # a far knee peeking out past the head as a loose speck: dropped
    for j in range(1, k + 1):
        if (l == j).sum() < 6: a[l == j] = 0
    return m.outline(Image.fromarray(a))

def leg_check():
    res = {}
    for n in LEGS:
        fr = [np.array(frame(i, only=n))[..., 3] > 0 for i in range(N)]
        xs = [np.where(f.any(axis=0))[0] for f in fr]
        res[n] = dict(distinct_states=len({f.tobytes() for f in fr}), min_pixels_changed_in_a_step=min(int((fr[i] ^ fr[(i + 1) % N]).sum()) for i in range(N)), foot_reach_px=int(max(x.max() for x in xs) - min(x.max() for x in xs)))
    return res

def save(out):
    os.makedirs(out, exist_ok=True); fr = [frame(i) for i in range(N)]
    for i, f in enumerate(fr): f.save(os.path.join(out, f"frame{i}.png"))
    strip = Image.new("RGBA", (W * N, H), (0, 0, 0, 0))
    for i, f in enumerate(fr): strip.alpha_composite(f, (i * W, 0))
    strip.save(os.path.join(out, "dread_walk_strip.png"))
    json.dump({"dread_prowler": {"walk": {"frame": [W, H], "ms": [125] * N, "loop": True, "travel": [0] * N, "ground_row": GROUND}}}, open(os.path.join(out, "dread_walk.json"), "w"), indent=1)
    sc = 12; sheet = Image.new("RGBA", (W * 4 * sc, H * 2 * sc), (105, 115, 90, 255))
    for i, f in enumerate(fr): sheet.alpha_composite(f.resize((W * sc, H * sc), Image.NEAREST), ((i % 4) * W * sc, (i // 4) * H * sc))
    sheet.convert("RGB").save(os.path.join(out, "sheet.png")); return fr

def gif(path, fr, ms, px=2.27, n=48, sc=10):
    fs = []; ds = []
    for k in range(n):
        im = Image.new("RGBA", (W + 12, H + 4), (54, 49, 41, 255)); d = ImageDraw.Draw(im); d.rectangle([0, GROUND + 1, im.width, im.height], fill=(60, 55, 46, 255)); off = int(k * px) % 12
        for gx in range(-12, im.width, 12): d.rectangle([gx - off + 12, GROUND + 3, gx - off + 14, GROUND + 3], fill=(80, 74, 62, 255))
        im.alpha_composite(fr[k % N], (6, 0)); fs.append(im.resize((im.width * sc, im.height * sc), Image.NEAREST).convert("RGB").quantize(colors=64, method=Image.MEDIANCUT, dither=Image.NONE)); ds.append(ms)
    fs[0].save(path, save_all=True, append_images=fs[1:], duration=ds, loop=0)

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "mutants_anim/dread_walk"; fr = save(out); gif(os.path.join(out, "loop_125ms.gif"), fr, 125); gif(os.path.join(out, "loop_220ms.gif"), fr, 220)
    frame(0, stand=True).save(os.path.join(out, 'stand.png'))
    chk = leg_check()
    for k, v in chk.items(): print(k, v)
