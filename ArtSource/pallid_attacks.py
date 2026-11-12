# Pallid Prowler BITE, ALTERNATIVE BITE and POUNCE (2026-11-12), body and head only (Yan: no jaw animation, no impact flash; the pounce and the body motion approved on the Veil).
# Built on the Pallid that is in the game now (the stand sprite, 47x23). The body is one rigid piece with whole-pixel moves (offset, rise, a column-shear tilt); the head (x >= 31) moves on its own by whole
# pixels with a vertical column shear for the nod, the neck gap is filled by repeating the neck column. Legs: when the feet stay planted and the body only leans or dips (bite), the legs are HIS leg pixels
# (rows sheared so the hip follows the body and the foot stays put; rows deleted/duplicated when the body dips/rises); in the air (pounce, alternative bite) they are slim IK tubes in the walk's colours.
# The pose lists are the Veil's (veil_attacks.py), the same timings.    python3 pallid_attacks.py <out_dir>
import math, os, sys, json
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as nd
import mutant_from_ref as m
import veil_attacks as VA            # the pose lists (BITE, BITE_B, POUNCE) and strip/gif helpers

HERE = os.path.dirname(os.path.abspath(__file__))
STAND = Image.open(os.path.join(HERE, "..", "Assets", "Resources", "Sprites", "Mutants", "mutant_hd_pallid_prowler.png")).convert("RGBA")
S = np.array(STAND); ox, oy = 14, 16; W, H = STAND.width + 2 * ox, STAND.height + 2 * oy; LOW = STAND.height - 1; GROUND = oy + LOW; CUT = 14; HX = 31
AXES = {"near_hind": ((9, 14), (5, 22)), "far_hind": ((15, 14), (15, 22)), "near_fore": ((24, 14), (27, 22)), "far_fore": ((33, 14), (38, 22))}
FOOT0 = {"near_hind": 5, "far_hind": 15, "near_fore": 27, "far_fore": 38}; FAR = ("far_hind", "far_fore"); NEAR = ("near_hind", "near_fore")

def _sd(x, y, a, b):
    vx, vy = b[0] - a[0], b[1] - a[1]; u = max(0.0, min(1.0, ((x - a[0]) * vx + (y - a[1]) * vy) / (vx * vx + vy * vy))); return math.hypot(x - (a[0] + u * vx), y - (a[1] + u * vy))
PIECE = {n: np.zeros_like(S) for n in AXES}; _BODY = S.copy(); _BODY[CUT:] = 0
for y in range(CUT, S.shape[0]):
    for x in range(S.shape[1]):
        if S[y, x, 3] and x >= 38 and y <= 17: _BODY[y, x] = S[y, x]                    # the jaw underside belongs to the head
        elif S[y, x, 3]: PIECE[min(AXES, key=lambda k: _sd(x, y, *AXES[k]))][y, x] = S[y, x]
BODY = _BODY

def planted_leg(name, bx, by, rise):
    """his leg pixels: the hip row follows the body (shift bx), the foot row stays; the body down by `by` deletes rows, up by `rise` repeats the top row"""
    p = PIECE[name]; rows = list(range(CUT, LOW + 1)); n = len(rows)
    cut = set(rows[n // 2: n // 2 + max(0, by)]); keep = [CUT] * max(0, rise) + [y for y in rows if y not in cut]
    out = np.zeros((H, W, 4), np.uint8)
    for k, y in enumerate(keep):
        yy = CUT - max(0, rise) + k + max(0, by) - 0; u = k / max(1, len(keep) - 1); sh = int(round(bx * (1 - u)))
        for x in range(S.shape[1]):
            if p[y, x, 3]:
                nx = x + sh + ox
                if 0 <= nx < W and 0 <= yy + oy < H: out[yy + oy, nx] = p[y, x]
    return Image.fromarray(out)

RAMP_NEAR = [(196, 189, 170), (190, 183, 165), (178, 172, 156), (162, 158, 144), (146, 143, 130), (124, 122, 112), (96, 95, 88), (64, 64, 60)]
RAMP_FAR = [(150, 146, 133), (142, 138, 126), (130, 127, 116), (118, 115, 106), (104, 102, 94), (88, 86, 80), (70, 69, 65), (48, 48, 46)]
L1, L2 = 7.0, 6.6
def _solve(hip, ft, bend_fwd):
    dx, dy = ft[0] - hip[0], ft[1] - hip[1]; d = min(math.hypot(dx, dy), L1 + L2 - 0.2); a = math.atan2(dy, dx)
    ang = math.acos(max(-1, min(1, (L1 * L1 + d * d - L2 * L2) / (2 * L1 * d)))); k = a + (ang if bend_fwd else -ang)
    return (hip[0] + L1 * math.cos(k), hip[1] + L1 * math.sin(k))

def tube_leg(name, hip, ft):
    hind = name.endswith("hind"); knee = _solve(hip, ft, not hind); ramp = RAMP_FAR if name in FAR else RAMP_NEAR
    segs = [(hip, knee, 1.5 if hind else 1.25, 0.95), (knee, ft, 0.95, 0.75)]; tot = sum(math.hypot(q[0] - p[0], q[1] - p[1]) for p, q, _, _ in segs); fx, fy = int(round(ft[0])), int(round(ft[1]))
    out = np.zeros((H, W, 4), np.uint8)
    for y in range(H):
        for x in range(W):
            best = None; acc = 0.0
            for p, q, r0, r1 in segs:
                vx, vy = q[0] - p[0], q[1] - p[1]; ln = math.hypot(vx, vy) + 1e-9
                u = max(0.0, min(1.0, ((x - p[0]) * vx + (y - p[1]) * vy) / (ln * ln))); cx, cy = p[0] + u * vx, p[1] + u * vy
                dist = math.hypot(x - cx, y - cy); r = r0 + (r1 - r0) * u
                if dist <= r and (best is None or dist / r < best[0]): best = (dist / r, (acc + u * ln) / tot, ((x - cx) * (-vy) + (y - cy) * vx) / ln)
                acc += ln
            if best is None and y == fy and fx < x <= fx + 1: best = (1.0, 1.0, 0.0)
            if best is None: continue
            col = ramp[min(7, int(best[1] * 8))]
            if best[2] > 0.25: col = tuple(int(v * 0.82) for v in col)
            out[y, x, :3] = col; out[y, x, 3] = 255
    return m.outline(Image.fromarray(out))

def shift_cols(a, x0, x1, amount, fromleft=True):
    """vertical whole-pixel shear of columns x0..x1 of array a: 0 at x0 growing to `amount` at x1 (columns past x1 move by `amount`)"""
    if amount == 0: return a
    src = a.copy(); a = np.zeros_like(a); h, w = a.shape[:2]
    for x in range(w):
        f = 0.0 if x <= x0 else min(1.0, (x - x0) / float(max(1, x1 - x0))); sh = int(round(amount * f))
        for y in range(h):
            ny = y + sh
            if 0 <= ny < h and src[y, x, 3]: a[ny, x] = src[y, x]
    return a

def upper(p):
    """body + head as one array at the frame size, before the offset: head thrust/dip/turn (nx, ny, nrot -> a nod shear), then the whole-body tilt"""
    nx, ny, nrot = p.get("nx", 0), p.get("ny", 0), p.get("nrot", 0)
    a = np.zeros((H, W, 4), np.uint8); a[oy:oy + S.shape[0], ox:ox + S.shape[1]] = BODY
    if nx or ny or nrot:
        body = a.copy(); head = a.copy(); body[:, ox + HX:] = 0; head[:, :ox + HX] = 0
        if nrot: head = shift_cols(head, ox + HX, ox + HX + 10, -int(round(nrot / 3.0)))            # nose up (+) raises the nose end
        hv = np.zeros_like(head)
        for y in range(H):
            for x in range(W):
                if head[y, x, 3]:
                    nxx, nyy = x + nx, y + ny
                    if 0 <= nxx < W and 0 <= nyy < H: hv[nyy, nxx] = head[y, x]
        for k in range(nx):                                                                          # the neck stretches to meet the moved head
            for y in range(H):
                if body[y, ox + HX - 1, 3] and not hv[y + ny if 0 <= y + ny < H else y, ox + HX + k, 3]: hv[min(H - 1, max(0, y + ny)), ox + HX + k] = body[y, ox + HX - 1]
        a = body; m_ = hv[..., 3] > 0; a[m_] = hv[m_]
    tilt = p.get("tilt", 0)
    if tilt:
        src = a.copy(); a = np.zeros_like(a)
        for x in range(W):
            sh = int(round(tilt * (x - (ox + 24)) / 24.0))
            for y in range(H):
                ny2 = y + sh
                if 0 <= ny2 < H and src[y, x, 3]: a[ny2, x] = src[y, x]
    return a

def render(p):
    bx, by, rise = p.get("bx", 0), p.get("by", 0), p.get("rise", 0); tilt = p.get("tilt", 0); feet = p.get("feet", {})
    air = bool(feet) or rise > 0
    up = Image.fromarray(upper(p)); body_layer = Image.new("RGBA", (W, H), (0, 0, 0, 0)); body_layer.alpha_composite(up, (bx, by - rise))
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    def leg(n):
        if air:
            fo, fl = feet.get(n, (0, 0)); hx, hy = AXES[n][0]; hxx = hx + ox + bx; hip = (hxx, hy + oy + by - rise + int(round(tilt * (hxx - (ox + 24)) / 24.0)) - (0 if n in FAR else 2))
            return tube_leg(n, hip, (FOOT0[n] + ox + fo, GROUND - 2 - fl))
        return planted_leg(n, bx, by, 0)
    for n in FAR: im.alpha_composite(leg(n))
    im.alpha_composite(body_layer)
    for n in NEAR: im.alpha_composite(leg(n))
    a = np.array(im); l, k = nd.label(a[..., 3] > 0, structure=np.ones((3, 3)))
    for j in range(1, k + 1):
        if (l == j).sum() < 6: a[l == j] = 0
    return Image.fromarray(a)

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "mutants_anim/pallid_attacks"; os.makedirs(out, exist_ok=True)
    VA.W, VA.H, VA.GROUND = W, H, GROUND; res = {}
    for name, poses in (("bite", VA.BITE), ("bite2", VA.BITE_B)):       # the Pallid has no pounce any more (Yan: it gets a ranged spit instead, pallid_spit.py)
        fr = [render(p) for p, _, _ in poses]; ms = [t for _, t, _ in poses]; tr = [x for _, _, x in poses]
        for i, f in enumerate(fr): f.save(os.path.join(out, f"{name}_{i}.png"))
        VA.strip_of(fr).save(os.path.join(out, f"pallid_prowler_{name}_strip.png")); VA.gif(os.path.join(out, f"{name}.gif"), fr, ms)
        res[name] = {"frame": [W, H], "ms": ms, "travel": tr, "ground_row": GROUND}
        print(name, len(fr), "frames; components", [nd.label(np.array(f)[..., 3] > 0, structure=np.ones((3, 3)))[1] for f in fr], "lowest rows", [int(np.where(np.array(f)[..., 3] > 0)[0].max()) for f in fr], "ground", GROUND)
        sc = 8; per = 5; rows = (len(fr) + per - 1) // per; sheet = Image.new("RGBA", (W * per * sc, H * rows * sc), (105, 115, 90, 255))
        for k, f in enumerate(fr): sheet.alpha_composite(f.resize((W * sc, H * sc), Image.NEAREST), ((k % per) * W * sc, (k // per) * H * sc))
        sheet.convert("RGB").save(os.path.join(out, f"{name}_sheet.png"))
    json.dump({"pallid_prowler": res}, open(os.path.join(out, "pallid_attacks.json"), "w"), indent=1)
    st = np.array(render({}))[..., 3] > 0; ref = np.zeros((H, W), bool); ref[oy:oy + S.shape[0], ox:ox + S.shape[1]] = S[..., 3] > 0
    print("neutral pose vs his stand sprite (alpha px differing):", int((st ^ ref).sum()))
