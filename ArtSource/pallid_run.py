# Pallid Prowler RUN cycle, first draft (2026-11-12). Yan: "try making a running cycle for the pallid prowler now".
# Built on the Pallid that is in the game now (the stand sprite = walk frame 1, 47x23): its body, head and tail are kept pixel for pixel (rows above the belly), the four legs are drawn fresh
# (hybrid method of veil_walk.py / dread_hybrid.py) with a gallop: hind pair pushing back, fore pair reaching forward, short ground contact, big lift, a flight phase where the body rises
# 2 px with the legs gathered, the head nodding and the haunch counter-moving by whole-pixel column shears.  python3 pallid_run.py <out_dir>
import math, os, sys, json
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as nd
import mutant_from_ref as m

HERE = os.path.dirname(os.path.abspath(__file__))
STAND = Image.open(os.path.join(HERE, "..", "Assets", "Resources", "Sprites", "Mutants", "mutant_hd_pallid_prowler.png")).convert("RGBA")
N = 8
ox, oy = 8, 6
W, H = STAND.width + 2 * ox, STAND.height + 2 * oy
GROUND = oy + STAND.height - 1                     # the stand's lowest row
CUT = 14                                           # rows from here down are legs (redrawn)
_a = np.array(STAND); _a[CUT:] = 0; BODY = Image.fromarray(_a)
LEGS = {"far_hind": ((14, 13), 0.10, True, False), "near_hind": ((9, 13), 0.00, False, False), "far_fore": ((31, 13), 0.50, True, True), "near_fore": ((24, 13), 0.60, False, True)}
STANCE = 0.38                                      # fraction of the cycle a foot is on the ground
STRIDE, LIFT = 7.0, 4.2
C_PALE, C_PALE_SH, C_MAIN, C_SHADE, C_DARK = (193, 186, 168), (144, 132, 120), (144, 144, 120), (96, 96, 84), (60, 60, 60)      # the sprite's own: body pale, legs greying down to the feet

def foot(ph, hind):
    bias = -1.0 if hind else 2.0
    if ph < STANCE: return (STRIDE - 2 * STRIDE * ph / STANCE + bias, 0.0)
    u = (ph - STANCE) / (1 - STANCE)
    return (-STRIDE + 2 * STRIDE * (0.5 - 0.5 * math.cos(math.pi * u)) + bias, LIFT * math.sin(math.pi * u) ** 0.8)

def solve(hip, ft, bend_fwd, l1, l2):
    dx, dy = ft[0] - hip[0], ft[1] - hip[1]; d = min(math.hypot(dx, dy), l1 + l2 - 0.2); a = math.atan2(dy, dx)
    ang = math.acos(max(-1, min(1, (l1 * l1 + d * d - l2 * l2) / (2 * l1 * d)))); k = a + (ang if bend_fwd else -ang)
    return (hip[0] + l1 * math.cos(k), hip[1] + l1 * math.sin(k))

def body_motion(i):
    rise = int(round(1.8 * (0.5 + 0.5 * math.sin(2 * math.pi * i / N + 0.6))))        # 0..2 px up once a cycle
    nod = int(round(1.0 * math.sin(2 * math.pi * i / N + 2.2)))                         # head
    haunch = -nod                                                                       # the haunch moves the other way
    return -rise, nod, haunch

def shear_cols(img, x0, x1, amount):
    """move columns x0..x1 (x1 may be > x0 or < x0: the end that moves fully) vertically by whole pixels, growing from 0 at x0 to `amount` at x1 (the rest beyond x1 moves by `amount`)"""
    if amount == 0: return img
    a = np.array(img); src = a.copy(); h, w = a.shape[:2]; step = 1 if x1 > x0 else -1
    xs = range(x0, w) if step > 0 else range(x0, -1, -1)
    for x in xs:
        f = min(1.0, abs(x - x0) / max(1, abs(x1 - x0))); sh = int(round(amount * f))
        if sh == 0: continue
        a[:, x] = 0
        for y in range(h):
            ny = y + sh
            if 0 <= ny < h and src[y, x, 3]: a[ny, x] = src[y, x]
    return Image.fromarray(a)

def leg_img(name, i, rise):
    (hx, hy), ph0, far, fwd = LEGS[name]; ph = (i / N + ph0) % 1.0; fo, fl = foot(ph, name.endswith("hind"))
    hind = name.endswith("hind")
    hip = (hx + ox, hy + oy + rise); ft = (hx + ox + fo + (1 if fwd else 0), GROUND - 1 - fl)
    if hind:
        hock = (ft[0] - 0.8, ft[1] - 2.4); st = solve(hip, hock, False, 6.0, 6.2); pts = [hip, st, hock, ft]; rad = [1.35, 1.05, 0.85, 0.8]
    else:
        wr = (ft[0] - 0.2, ft[1] - 2.2); el = solve(hip, wr, True, 6.6, 6.4); pts = [hip, el, wr, ft]; rad = [1.25, 1.0, 0.85, 0.8]
    segs = [(pts[j], pts[j + 1], rad[j], rad[j + 1]) for j in range(3)]
    out = np.zeros((H, W, 4), np.uint8); k = 0.82 if far else 1.0; fx, fy = int(round(ft[0])), int(round(ft[1]))
    for y in range(H):
        for x in range(W):
            best = None
            for si, (p, q, r0, r1) in enumerate(segs):
                vx, vy = q[0] - p[0], q[1] - p[1]; ln = math.hypot(vx, vy) + 1e-9
                u = max(0.0, min(1.0, ((x - p[0]) * vx + (y - p[1]) * vy) / (ln * ln))); cx, cy = p[0] + u * vx, p[1] + u * vy
                dist = math.hypot(x - cx, y - cy); r = r0 + (r1 - r0) * u
                if dist <= r and (best is None or dist / r < best[0]): best = (dist / r, si, u, ((x - cx) * (-vy) + (y - cy) * vx) / ln)
            if (x, y) in {(fx + 1, fy), (fx + 2, fy)} and best is None: best = (1.0, 2, 1.0, 0.0)        # a short flat toe
            if best is None: continue
            _, si, u, side = best; t = (si + u) / 3.0
            lit = side < 0.1
            if t < 0.34: col = C_PALE if lit else C_PALE_SH                    # thigh: the body's pale tones
            elif t < 0.7: col = C_MAIN if lit else C_SHADE                      # shin: greying
            else: col = C_SHADE if lit else C_DARK                              # foot: dark
            out[y, x, :3] = tuple(int(v * k) for v in col); out[y, x, 3] = 255
    return Image.fromarray(out)

def frame(i, only=None):
    rise, nod, haunch = body_motion(i)
    if only: return leg_img(only, i, rise)
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    for n in LEGS:
        if LEGS[n][2]: im.alpha_composite(m.outline(leg_img(n, i, rise)))
    b = shear_cols(BODY, 28, 38, nod); b = shear_cols(b, 16, 6, haunch)
    im.alpha_composite(b, (ox, oy + rise))
    for n in LEGS:
        if not LEGS[n][2]: im.alpha_composite(m.outline(leg_img(n, i, rise)))
    a = np.array(im); l, k = nd.label(a[..., 3] > 0, structure=np.ones((3, 3)))
    for j in range(1, k + 1):
        if (l == j).sum() < 6: a[l == j] = 0
    return Image.fromarray(a)

def leg_check():
    res = {}
    for n in LEGS:
        fr = [np.array(frame(i, only=n))[..., 3] > 0 for i in range(N)]
        res[n] = dict(distinct_states=len({f.tobytes() for f in fr}), min_changed=min(int((fr[i] ^ fr[(i + 1) % N]).sum()) for i in range(N)))
    return res

def save(out, ms=100):
    os.makedirs(out, exist_ok=True); fr = [frame(i) for i in range(N)]
    for i, f in enumerate(fr): f.save(os.path.join(out, f"frame{i}.png"))
    strip = Image.new("RGBA", (W * N, H), (0, 0, 0, 0))
    for i, f in enumerate(fr): strip.alpha_composite(f, (i * W, 0))
    strip.save(os.path.join(out, "pallid_run_strip.png"))
    sc = 12; sheet = Image.new("RGBA", (W * 4 * sc, H * 2 * sc), (105, 115, 90, 255))
    for i, f in enumerate(fr): sheet.alpha_composite(f.resize((W * sc, H * sc), Image.NEAREST), ((i % 4) * W * sc, (i // 4) * H * sc))
    sheet.convert("RGB").save(os.path.join(out, "sheet.png"))
    fs = []
    for k in range(32):
        im = Image.new("RGBA", (W + 12, H + 4), (54, 49, 41, 255)); d = ImageDraw.Draw(im); d.rectangle([0, GROUND + 2, im.width, im.height], fill=(60, 55, 46, 255)); off = int(k * 5.5) % 12
        for gx in range(-12, im.width, 12): d.point((gx - off + 12, GROUND + 4), fill=(80, 74, 62, 255))
        im.alpha_composite(fr[k % N], (6, 0)); fs.append(im.resize((im.width * 10, im.height * 10), Image.NEAREST).convert("RGB").quantize(colors=64, method=Image.MEDIANCUT, dither=Image.NONE))
    fs[0].save(os.path.join(out, f"run_{ms}ms.gif"), save_all=True, append_images=fs[1:], duration=ms, loop=0)
    return fr

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "mutants_anim/pallid_run"; fr = save(out)
    for k, v in leg_check().items(): print(k, v)
    print("lowest rows", [int(np.where(np.array(f)[..., 3] > 0)[0].max()) for f in fr], "ground", GROUND, "components", [nd.label(np.array(f)[..., 3] > 0, structure=np.ones((3, 3)))[1] for f in fr])
