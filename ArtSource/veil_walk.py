# Veil Prowler walk (2026-11-11), the second one after the Pallid, "based on" the Pallid walk Yan recovered from an online animator (mutants_anim/pallid_online). Yan: "can't you use this
# one as a reference and try animating another one based on it? the black dog that disappears could be next".
# What is taken from the reference: 8 pictures a cycle at 125 ms, a diagonal gait (near hind + far fore, then the other pair), the body dipping twice a cycle (the reference's back drops
# about one sprite pixel at two points of the cycle), the head riding the body, the planted feet sliding back at one speed, the legs lifted and swung forward in between, only two feet on
# the ground in the two passing frames. What is kept from Yan's own Veil sprite: every pixel of the body, head, jaw and tail (the cut-out rig of canine_cutout.py, legs removed; moved only
# by whole pixels, never resampled). What is new: the four legs, drawn fresh each frame in the sprite's own colours (thin tubes with a lit edge and a flat clawed foot, positioned by a two-bone solve)
# because rotating or shearing a 3 px leg shreds it (the second and third tries).
#   python3 veil_walk.py <out_dir>
import math, os, sys, json
import numpy as np
from PIL import Image, ImageDraw
import canine_cutout as C
import mutant_from_ref as m

ox, oy = 8, 4                           # the sprite's place inside the frame
N = 8
rig = C.Rig("veil_prowler", C.SPECS["veil_prowler"])
W, H = rig.W0 + 2 * ox, rig.H0 + 2 * oy
GROUND = oy + 31                        # the original's feet end on its row 31
LEGS = {"far_hind": ((15, 19), 0.50, True, False), "near_hind": ((10, 19), 0.00, False, False), "far_fore": ((31, 19), 0.00, True, True), "near_fore": ((25, 19), 0.50, False, True)}
L1, L2 = 6.5, 6.5
STRIDE, LIFT = 5.5, 3.5
BODY = Image.new("RGBA", rig.base.size, (0, 0, 0, 0))
for n in ("hind", "fore"): BODY.alpha_composite(rig.parts[n])
HEAD = Image.new("RGBA", rig.base.size, (0, 0, 0, 0)); HEAD.alpha_composite(rig.parts["head"]); HEAD.alpha_composite(rig.parts["jaw"])
# the legs' own colours (the original's leg pieces: black contour, violet body, a pale lit edge)
DARK, MID, LIT, LIT2 = (26, 24, 36), (34, 32, 45), (87, 88, 103), (55, 54, 64)
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

def leg_img(name, i, bob):
    """Same geometry as before (the leg's shape and place are untouched); only the colours are decided differently: by where a pixel sits ON the leg (along the bone and which side of it), so the
    colouring is attached to the leg and does not shimmer as it moves. The body's own palette is mostly pale violet-grey (55,54,64) with darker violet (34,32,45) in its hollows and a few bright
    streaks (87,88,103) at its joints; the legs now use that mix instead of one flat dark violet."""
    (hx, hy), ph0, far, fwd = LEGS[name]; ph = (i / N + ph0) % 1.0; fo, fl = foot(ph)
    hip = (hx + ox, hy + oy + bob); ft = (hx + ox + fo + (2 if fwd else -1), GROUND - fl); kn = solve(hip, ft, fwd)
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    d.line([hip, kn], fill=(255, 255, 255, 255), width=2); d.line([kn, ft], fill=(255, 255, 255, 255), width=1)
    d.line([(hip[0] - 0.7, hip[1]), (kn[0] - 0.7, kn[1])], fill=(255, 255, 255, 255), width=1); d.line([(kn[0] - 0.5, kn[1]), (ft[0] - 0.5, ft[1])], fill=(255, 255, 255, 255), width=1)
    fx, fy = int(round(ft[0])), int(round(ft[1]))
    d.rectangle([fx - 1, fy, fx + 3, fy], fill=(255, 255, 255, 255))
    d.point((int(round(kn[0] - 1)), int(round(kn[1]))), fill=(255, 255, 255, 255))                       # the bright speck at the knee (kept: it is part of the silhouette Yan approved)
    KNEE = (int(round(kn[0] - 1)), int(round(kn[1])))
    a = np.array(im); a[..., 3] = np.where(a[..., 3] > 120, 255, 0)
    k = 0.86 if far else 1.0
    def sh(c): return tuple(int(v * k) for v in c)
    segs = [(hip, kn), (kn, ft)]
    ys, xs = np.where(a[..., 3] > 0)
    for y, x in zip(ys, xs):
        best = None
        for si, (p, q) in enumerate(segs):
            vx, vy = q[0] - p[0], q[1] - p[1]; L2 = vx * vx + vy * vy + 1e-9
            u = max(0.0, min(1.0, ((x - p[0]) * vx + (y - p[1]) * vy) / L2)); cx, cy = p[0] + u * vx, p[1] + u * vy
            dist = math.hypot(x - cx, y - cy); side = (x - cx) * (-vy) + (y - cy) * vx          # sign of which side of the bone (negative = the left, lit one)
            if best is None or dist < best[0]: best = (dist, si, u, side)
        dist, si, u, side = best; t = (si + u) / 2.0                                          # 0 at the hip .. 1 at the foot
        lit_side = side > 0.15 if (hip[1] - kn[1]) > 0 or True else side < 0
        if y >= fy and x >= fx - 1: col = (55, 54, 64) if t < 2 else (34, 32, 45)                  # the foot: the pale violet, its toe a bright speck below
        elif side < -0.2: col = (55, 54, 64)                                                   # the lit left edge: pale violet-grey, like the body's lit planes
        elif (int(t * 9) % 4) == 0 and dist < 0.8: col = (87, 88, 103)                           # a bright streak across the leg at intervals (the body has them at its joints)
        elif t < 0.55: col = (55, 54, 64) if (int(t * 12) + (1 if side > 0 else 0)) % 3 != 0 else (34, 32, 45)    # the thigh: mostly pale, a hollow of dark violet
        else: col = (34, 32, 45) if (int(t * 12)) % 3 == 0 else (55, 54, 64)                   # the shin: dark and pale in turn
        if x == fx + 3 and y == fy: col = (87, 88, 103)
        if (x, y) == KNEE: col = (87, 88, 103)
        a[y, x, :3] = sh(col)
    return Image.fromarray(a.astype(np.uint8))

def frame(i, only=None):
    bob = int(round(0.9 * math.sin(2 * math.pi * (i / N * 2) + 0.6))); sway = int(round(0.9 * math.sin(2 * math.pi * i / N + 2.0)))
    far = [n for n in LEGS if LEGS[n][2]]; near = [n for n in LEGS if not LEGS[n][2]]
    if only: return leg_img(only, i, bob)
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    for n in far: im.alpha_composite(leg_img(n, i, bob))
    im.alpha_composite(BODY, (ox, oy + bob)); im.alpha_composite(HEAD, (ox, oy + bob + sway))
    for n in near: im.alpha_composite(leg_img(n, i, bob))
    return m.outline(im)

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
    strip.save(os.path.join(out, "veil_walk_strip.png"))
    json.dump({"veil_prowler": {"walk": {"frame": [W, H], "ms": [125] * N, "loop": True, "travel": [0] * N, "ground_row": GROUND}}}, open(os.path.join(out, "veil_walk.json"), "w"), indent=1)
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
    out = sys.argv[1] if len(sys.argv) > 1 else "_out/veil_walk"; fr = save(out); gif(os.path.join(out, "loop_125ms.gif"), fr, 125); gif(os.path.join(out, "loop_220ms.gif"), fr, 220)
    chk = leg_check()
    for k, v in chk.items(): print(k, v)
