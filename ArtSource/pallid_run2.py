# Pallid Prowler RUN cycle, second draft (2026-11-12). The first draft drew new legs and "changed his legs completely" (Yan). Analysis of that draft: its legs held 111 px against 208 px in his
# standing sprite (about half the leg mass, thinner, a different shading), they spanned 22 px under the body where his span 37 px, I had cut the body at row 14 and thrown away 59 px of haunch/shoulder
# that belong to the legs' top, and no frame resembled his standing pose, so the run started from nowhere.
# This draft uses HIS leg pixels (rows 14..22 of the standing sprite) split into four legs, each posed only by whole-pixel moves like the Dread's hind legs: the rows shear sideways from the hip
# (0) to the foot (dx), a lifted foot is made by deleting rows (the leg shortens, the foot pixels stay), and the hand-set key table below starts and ends at his standing pose.
#   python3 pallid_run2.py <out_dir>
import math, os, sys
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as nd

HERE = os.path.dirname(os.path.abspath(__file__))
STAND = Image.open(os.path.join(HERE, "..", "Assets", "Resources", "Sprites", "Mutants", "mutant_hd_pallid_prowler.png")).convert("RGBA")
N = 8
ox, oy = 8, 6
W, H = STAND.width + 2 * ox, STAND.height + 2 * oy
CUT = 14                                            # legs are the rows from here down (his standing sprite: rows 14..22)
LOW = STAND.height - 1                              # 22
S = np.array(STAND)
AXES = {"near_hind": ((9, 14), (5, 22)), "far_hind": ((15, 14), (15, 22)), "near_fore": ((24, 14), (27, 22)), "far_fore": ((33, 14), (38, 22))}   # a line down each of his legs
FAR = {"far_hind", "far_fore"}

def seg_dist(x, y, a, b):
    vx, vy = b[0] - a[0], b[1] - a[1]; u = max(0.0, min(1.0, ((x - a[0]) * vx + (y - a[1]) * vy) / (vx * vx + vy * vy))); return math.hypot(x - (a[0] + u * vx), y - (a[1] + u * vy))

# split his leg pixels between the four legs: each goes to the nearest axis
PIECE = {n: np.zeros_like(S) for n in AXES}
_BODY = S.copy(); _BODY[CUT:] = 0
for y in range(CUT, S.shape[0]):
    for x in range(S.shape[1]):
        if S[y, x, 3] and x >= 38 and y <= 17: _BODY[y, x] = S[y, x]      # the underside of the jaw (16 px) hangs below the cut line: it belongs to the head, not to the far fore leg
        elif S[y, x, 3]:
            n = min(AXES, key=lambda k: seg_dist(x, y, *AXES[k])); PIECE[n][y, x] = S[y, x]
BODY = Image.fromarray(_BODY)

# key table, 8 frames, per leg: (dx forward in px at the foot, lift in rows). Frame 0 and 7 are his standing pose or close to it; 1-2 push off, 3-4 flight (fore reaching, hind trailing), 5-6 landing.
# One stride per pair of legs, then each leg is that stride shifted in time (Yan: "the legs all move together ... the front legs should move first, alternating by a short offset, and then the back legs kick, that's how dogs run"):
# near fore leads, far fore follows one frame later, then the hind pair lands 3 and 4 frames after the near fore and kicks off in turn.
# Stride entries are (foot dx, knee dx, lift rows): planted and sliding back -> push off -> lifted/tucked -> swung forward -> reaching -> landed.
def _stride(front, back, lift_max):
    """8 evenly spaced steps of one stride: 4 on the ground sliding from `front` to `back` (about the same distance every frame), then 4 in the air swinging back to `front` with the foot lifted in an arc"""
    out = []
    for k in range(8):
        if k < 4: dx = front + (back - front) * (k + 0.5) / 4.0; lift = 0
        else: t = (k - 4 + 0.5) / 4.0; dx = back + (front - back) * t; lift = int(round(lift_max * math.sin(math.pi * t)))
        out.append((int(round(dx)), int(round(dx * 0.45)), lift))
    return out
FORE = _stride(4, -3, 3)
HIND = _stride(3, -4, 3)      # short strides: on 8-10 px legs a +-9 px stride lays the legs almost flat (Yan: the run looked sprawled next to the walk's upright legs)
SHIFT = {"near_fore": 0, "far_fore": 1, "near_hind": 3, "far_hind": 4}
def key(n, i):
    dx, kx, lift = (FORE if n.endswith("fore") else HIND)[(i - SHIFT[n]) % 8]
    return (dx, kx, lift, 1 if lift >= 2 else 0)
KEYS = {n: [key(n, i) for i in range(8)] for n in AXES}       # evenly spread over all 8 frames (his stand sprite is a separate picture)
RISE = [min(3, sum(KEYS[n][i][3] for n in AXES)) for i in range(8)]               # the body is up as far as the legs that are off the ground
NOD = [0, 0, -1, -1, 1, 2, 1, 0]                    # head up in the extension, low in the landing
HAUNCH = [0, 0, 0, 0, 0, 0, 0, 0]      # no haunch shear: it tore thin lines across the belly where the legs join                    # head

def pose_leg(name, dx, kx, lift, stretch=0):
    """his leg pixels, rows sheared by dx (0 at the hip row, dx at the foot row), `lift` rows deleted from the middle (top-anchored: the foot rises)"""
    p = PIECE[name]; rows = list(range(CUT, LOW + 1)); n = len(rows)
    cut = set(rows[n // 2: n // 2 + lift]) if lift else set(); keep = [CUT] * stretch + [y for y in rows if y not in cut]
    out = np.zeros((H, W, 4), np.uint8)
    for k, y in enumerate(keep):
        yy = CUT - stretch + k; u = k / max(1, len(keep) - 1); sh = int(round(kx * u / 0.5)) if u <= 0.5 else int(round(kx + (dx - kx) * (u - 0.5) / 0.5))
        for x in range(S.shape[1]):
            if p[y, x, 3]:
                nx = x + sh + ox
                if 0 <= nx < W: out[yy + oy, nx] = p[y, x]
    return Image.fromarray(out)

def shear_cols(img, x0, x1, amount):
    if amount == 0: return img
    a = np.array(img); src = a.copy(); h, w = a.shape[:2]
    for x in range(x0, w):
        sh = int(round(amount * min(1.0, (x - x0) / max(1, x1 - x0))))
        if sh == 0: continue
        a[:, x] = 0
        for y in range(h):
            ny = y + sh
            if 0 <= ny < h and src[y, x, 3]: a[ny, x] = src[y, x]
    return Image.fromarray(a)

def haunch_cols(img, amount):
    """the haunch (columns 0..16) moves up/down against the head: whole columns by whole pixels, 0 at x=16 growing to `amount` at x<=6"""
    if amount == 0: return img
    a = np.array(img); src = a.copy(); h = a.shape[0]
    for x in range(0, 16):
        sh = int(round(amount * min(1.0, (16 - x) / 10.0)))
        if sh == 0: continue
        a[:, x] = 0
        for y in range(h):
            ny = y + sh
            if 0 <= ny < h and src[y, x, 3]: a[ny, x] = src[y, x]
    return Image.fromarray(a)

def frame(i, only=None):
    legs = {n: pose_leg(n, KEYS[n][i][0], KEYS[n][i][1], KEYS[n][i][2], 0 if KEYS[n][i][3] else RISE[i]) for n in AXES}
    if only: return legs[only]
    rise = RISE[i]; im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    def up(img): a = Image.new("RGBA", (W, H), (0, 0, 0, 0)); a.alpha_composite(img, (0, -rise)); return a       # a leg that is in the air rises with the body; its foot is already lifted by its key
    for n in ("far_hind", "far_fore"): im.alpha_composite(up(legs[n]) if KEYS[n][i][3] else legs[n])
    b = Image.new("RGBA", (W, H), (0, 0, 0, 0)); b.alpha_composite(shear_cols(haunch_cols(BODY, HAUNCH[i]), 28, 38, NOD[i]), (ox, oy - rise)); im.alpha_composite(b)
    for n in ("near_hind", "near_fore"): im.alpha_composite(up(legs[n]) if KEYS[n][i][3] else legs[n])
    a = np.array(im); l, k = nd.label(a[..., 3] > 0, structure=np.ones((3, 3)))
    for j in range(1, k + 1):
        if (l == j).sum() < 6: a[l == j] = 0
    return Image.fromarray(a)

def leg_check():
    res = {}
    for n in AXES:
        fr = [np.array(frame(i, only=n))[..., 3] > 0 for i in range(N)]
        res[n] = dict(pixels_frame0=int(fr[0].sum()), distinct_states=len({f.tobytes() for f in fr}), min_changed=min(int((fr[i] ^ fr[(i + 1) % N]).sum()) for i in range(N)))
    return res

def save(out, ms=100):
    os.makedirs(out, exist_ok=True); fr = [frame(i) for i in range(N)]
    for i, f in enumerate(fr): f.save(os.path.join(out, f"frame{i}.png"))
    st = Image.new("RGBA", (W, H), (0, 0, 0, 0)); st.alpha_composite(STAND, (ox, oy)); st.save(os.path.join(out, "stand.png"))
    strip = Image.new("RGBA", (W * N, H), (0, 0, 0, 0))
    for i, f in enumerate(fr): strip.alpha_composite(f, (i * W, 0))
    strip.save(os.path.join(out, "pallid_run_strip.png"))
    sc = 12; sheet = Image.new("RGBA", (W * 5 * sc, H * 2 * sc), (105, 115, 90, 255))
    for k, f in enumerate([st] + fr): sheet.alpha_composite(f.resize((W * sc, H * sc), Image.NEAREST), ((k % 5) * W * sc, (k // 5) * H * sc))
    sheet.convert("RGB").save(os.path.join(out, "sheet.png"))
    fs = []
    for k in range(32):
        im = Image.new("RGBA", (W + 12, H + 4), (54, 49, 41, 255)); d = ImageDraw.Draw(im); g = oy + LOW; d.rectangle([0, g + 2, im.width, im.height], fill=(60, 55, 46, 255)); off = int(k * 5.5) % 12
        for gx in range(-12, im.width, 12): d.point((gx - off + 12, g + 4), fill=(80, 74, 62, 255))
        im.alpha_composite(fr[k % N], (6, 0)); fs.append(im.resize((im.width * 10, im.height * 10), Image.NEAREST).convert("RGB").quantize(colors=64, method=Image.MEDIANCUT, dither=Image.NONE))
    fs[0].save(os.path.join(out, f"run_{ms}ms.gif"), save_all=True, append_images=fs[1:], duration=ms, loop=0)
    return fr

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "mutants_anim/pallid_run2"; fr = save(out)
    for k, v in leg_check().items(): print(k, v)
    diff0 = int((np.array(fr[0])[..., 3] != np.array(Image.open(os.path.join(out, "stand.png")))[..., 3]).sum())
    print("frame 0 vs his stand sprite: alpha pixels differing =", diff0, "| lowest rows", [int(np.where(np.array(f)[..., 3] > 0)[0].max()) for f in fr], "ground", oy + LOW, "| components", [nd.label(np.array(f)[..., 3] > 0, structure=np.ones((3, 3)))[1] for f in fr])
