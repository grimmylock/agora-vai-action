# Dread Prowler walk from Yan's NEW standing sprite (2026-11-11, recovered from his JPG: dread_new_clean.png, 63x37, 9 colours). Method copied from maw_hybrid.py: was: Blind Maw walk, hybrid (Yan: 'try blind maw next, but its sprite is also problematic so keep that in mind'). Same method as the Veil/Dread script this was copied from: body, head, jaw kept pixel for pixel, four stubby legs drawn fresh. Was: Dread Prowler stand-still sprite and walk (2026-11-11), built the way the Veil's was (veil_walk.py, which Yan approved): the cut-out body, head, jaw and tail of Yan's sprite are kept pixel for pixel,
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
import mutant_from_ref as m

ox, oy = 6, 4                           # the sprite's place inside the frame
N = 8
NEW = Image.open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "mutants_anim", "dread_new_clean.png")).convert("RGBA")
class _R: pass
rig = _R(); rig.W0, rig.H0 = NEW.size
W, H = rig.W0 + 2 * ox, rig.H0 + 2 * oy
GROUND = oy + 36                        # his feet end on row 36
CUT = 25                                # everything below this row is leg (redrawn); the belly outline sits on row 24
LEGS = {"far_hind": ((15, 22), 0.50, True, False), "near_hind": ((10, 23), 0.00, False, False), "far_fore": ((47, 21), 0.00, True, True), "near_fore": ((37, 22), 0.50, False, True)}
L1, L2 = 6.0, 5.6
STRIDE, LIFT = 4.4, 3.2
BODY = Image.new("RGBA", NEW.size, (0, 0, 0, 0)); _a = np.array(NEW); _a[CUT:] = 0; BODY = Image.fromarray(_a)
HEAD = Image.new("RGBA", NEW.size, (0, 0, 0, 0))     # head rides the body (no separate sway for this sprite)
# the leg colours: the sprite's own browns (the body is mostly the tan (99,81,68) with dark brown hollows (66,51,44) and pale streaks (142,128,108))
C_MAIN, C_HOLLOW, C_PALE, C_DARK = (99, 81, 71), (63, 48, 43), (135, 122, 105), (37, 27, 26)
OUT = (4, 1, 2, 255)

def foot(ph):
    if ph < 0.55: return (STRIDE - 2 * STRIDE * ph / 0.55, 0.0)
    u = (ph - 0.55) / 0.45
    return (-STRIDE + 2 * STRIDE * (0.5 - 0.5 * math.cos(math.pi * u)), LIFT * math.sin(math.pi * u))

def solve(hip, ft, fwd, l1=None, l2=None):
    l1 = l1 or L1; l2 = l2 or L2
    dx, dy = ft[0] - hip[0], ft[1] - hip[1]; d = min(math.hypot(dx, dy), l1 + l2 - 0.2); a = math.atan2(dy, dx)
    ang = math.acos(max(-1, min(1, (l1 * l1 + d * d - l2 * l2) / (2 * l1 * d)))); k = a + (ang if fwd else -ang)
    return (hip[0] + l1 * math.cos(k), hip[1] + l1 * math.sin(k))

# --- the legs' look comes from Yan's own legs (2026-11-11: "the legs lost too much detail in the animation, make them closer to the original").
# His four legs in the standing sprite are unwrapped along their bones: every pixel of his leg (rows below the belly cut) is stored as (position along the bone 0..1, side offset in px, colour).
# A redrawn leg takes, for each of its pixels, the colour of the nearest stored pixel in that (along, side) space, so its shading, black shadows and pale highlights are his, moved with the bones.
HIS_BONES = {"near_fore": [(34, 22), (34, 27), (36, 32), (37, 36)], "far_fore": [(43, 22), (44, 27), (47, 32), (50, 36)],
             "near_hind": [(13, 24), (6, 27), (3, 32), (3, 36)], "far_hind": [(14, 25), (12, 29), (11, 32), (11, 36)]}

def _arcs(pts):
    L = [0.0]
    for j in range(3): L.append(L[-1] + math.hypot(pts[j + 1][0] - pts[j][0], pts[j + 1][1] - pts[j][1]))
    return L

def _proj(pts, x, y):
    """(arc length along the polyline, signed side in px, distance) of point x, y"""
    L = _arcs(pts); best = None
    for j in range(3):
        p, q = pts[j], pts[j + 1]; vx, vy = q[0] - p[0], q[1] - p[1]; ln = math.hypot(vx, vy) + 1e-9
        u = max(0.0, min(1.0, ((x - p[0]) * vx + (y - p[1]) * vy) / (ln * ln))); cx, cy = p[0] + u * vx, p[1] + u * vy
        d = math.hypot(x - cx, y - cy); side = ((x - cx) * (-vy) + (y - cy) * vx) / ln
        if best is None or d < best[2]: best = (L[j] + u * ln, side, d)
    return best

TEX = {n: [] for n in HIS_BONES}
_na = np.array(NEW)
for _y in range(CUT, NEW.size[1]):
    for _x in range(NEW.size[0]):
        if not _na[_y, _x, 3]: continue
        _c = [(_proj(HIS_BONES[n], _x, _y), n) for n in HIS_BONES]; (_arc, _side, _d), _n = min(_c, key=lambda t: t[0][2])
        if _d <= 4.6: TEX[_n].append((_arc / _arcs(HIS_BONES[_n])[-1], _side, tuple(int(v) for v in _na[_y, _x, :3])))
TEXA = {n: (np.array([[t[0], t[1]] for t in v]), [t[2] for t in v]) for n, v in TEX.items()}

def tex_col(name, tn, side, total):
    xy, cols = TEXA[name]; ref = _arcs(HIS_BONES[name])[-1]
    d = ((xy[:, 0] - tn) * ref) ** 2 + (xy[:, 1] - side) ** 2
    return cols[int(np.argmin(d))]

def leg_img(name, i, bob, stand=False):
    """One leg: the bone from the walk rig (hind: thigh to the stifle, shank back to the hock, foot bone; fore: shoulder, elbow back, forearm, pastern), the colours from Yan's own leg texture (TEX)."""
    (hx, hy), ph0, far, fwd = LEGS[name]; ph = (i / N + ph0) % 1.0
    fo, fl = (0.0, 0.0) if stand else foot(ph)
    if stand: fo = {"far_hind": -3.0, "near_hind": -9.0, "far_fore": 3.0, "near_fore": 0.0}[name]
    hind = name.endswith("hind")
    hip = (hx + ox, hy + oy + bob - 1); ft = (hx + ox + fo + (2 if fwd else -1), GROUND - 2 - fl)   # the stub plus its outline reaches the same bottom row as his standing sprite
    if hind:
        hock = (ft[0] - 1.0, ft[1] - 3.4); st = solve(hip, hock, False, 7.0, 6.6); pts = [hip, st, hock, ft]; rad = [3.2, 2.5, 2.1, 1.9]
    else:
        wr = (ft[0] - 0.3, ft[1] - 2.6); el = solve(hip, wr, True, 7.8, 7.4); pts = [hip, el, wr, ft]; rad = [3.0, 2.5, 2.1, 1.9]
    segs = [(pts[j], pts[j + 1], rad[j], rad[j + 1]) for j in range(3)]
    L = _arcs(pts); total = L[-1]
    fx, fy = int(round(ft[0])), int(round(ft[1]))
    out = np.zeros((H, W, 4), np.uint8)
    paw = set()                                                          # no paws: his legs end in blunt stubs (Yan)
    for y in range(H):
        for x in range(W):
            best = None
            for si, (p, q, r0, r1) in enumerate(segs):
                vx, vy = q[0] - p[0], q[1] - p[1]; ln = math.hypot(vx, vy) + 1e-9
                u = max(0.0, min(1.0, ((x - p[0]) * vx + (y - p[1]) * vy) / (ln * ln))); cx, cy = p[0] + u * vx, p[1] + u * vy
                dist = math.hypot(x - cx, y - cy); r = r0 + (r1 - r0) * u
                if dist <= r and (best is None or dist / r < best[0]): best = (dist / r, L[si] + u * ln, ((x - cx) * (-vy) + (y - cy) * vx) / ln)
            if best is None and (x, y) in paw: best = (1.0, total, 0.0)
            if best is None: continue
            out[y, x, :3] = tex_col(name, best[1] / total, best[2], total); out[y, x, 3] = 255
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
    strip.save(os.path.join(out, "dread_prowler_walk_strip.png"))
    json.dump({"dread_prowler": {"walk": {"frame": [W, H], "ms": [125] * N, "loop": True, "travel": [0] * N, "ground_row": GROUND}}}, open(os.path.join(out, "dread_prowler_walk.json"), "w"), indent=1)
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
    out = sys.argv[1] if len(sys.argv) > 1 else "mutants_anim/dread_new"; fr = save(out); gif(os.path.join(out, "loop_125ms.gif"), fr, 125); gif(os.path.join(out, "loop_220ms.gif"), fr, 220)
    st = Image.new('RGBA', (W, H), (0, 0, 0, 0)); st.alpha_composite(NEW, (ox, oy)); st.save(os.path.join(out, 'stand.png'))      # his drawing, untouched, padded to the frame width
    chk = leg_check()
    for k, v in chk.items(): print(k, v)
