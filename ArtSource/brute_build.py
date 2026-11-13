# Blight Brute (2026-11-12): the NEW Brute, Yan's AI-generated pose sheet (refs/brute/brute_poses_ai.jpg) recovered to game pixels by brute_recover.py (mutants_anim/brute_poses/*.png), replaces the old sprite everywhere.
# Builds: the 64x64 stand sprite, the 8-frame walk (the idle's body + two leg pieces, whole-pixel moves only), the slam / swipe / throw strips (his poses held with timings, crest-aligned and feet on one row), and the
# rock, rock-shatter and slam-dust strips (code-drawn).  python3 brute_build.py   ->  mutants_anim/brute/ (strips + json) and Assets/Resources/Sprites/Mutants/mutant_hd_blight_brute.png
import os, json, math
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as nd
HERE = os.path.dirname(os.path.abspath(__file__)); D = os.path.join(HERE, "mutants_anim", "brute_poses"); OUT = os.path.join(HERE, "mutants_anim", "brute"); os.makedirs(OUT, exist_ok=True)
SRC = {"idle": "idle", "slam_up": "slam_up", "slam_hit": "slam_hit", "out": "wind", "cock": "set3_f1", "across": "set3_f3"}       # round-2 poses (brute_recover2.py): his idle, slam wind-up, slam hit, arm out, arm cocked
P = {}
def spine(a):
    yel = (a[..., 1] > 180) & (a[..., 2] < 120) & (a[..., 3] > 0); ys, xs = np.where(yel[: int(a.shape[0] * 0.45)]); return xs.max() - xs.min() + 1
I = np.array(Image.open(os.path.join(D, "idle.png"))); TARGET = spine(I)
for k, f in SRC.items():                                                    # every pose is brought to the idle's size by its spine crest (nearest-neighbour, Yan approved the ~17 % enlargement of the slam poses)
    a = np.array(Image.open(os.path.join(D, f + ".png"))); r = TARGET / float(spine(a))
    if abs(r - 1.0) > 0.015: im = Image.fromarray(a).resize((round(a.shape[1] * r), round(a.shape[0] * r)), Image.NEAREST); a = np.array(im)
    ys, xs = np.where(a[..., 3] > 0); P[k] = a[ys.min(): ys.max() + 1, xs.min(): xs.max() + 1]
def crest(a):
    yel = (a[..., 1] > 180) & (a[..., 2] < 120) & (a[..., 3] > 0); ys, xs = np.where(yel[: a.shape[0] * 2 // 5]); return int(round(xs.mean()))
W, H = 108, 88; CX = 58; ground = H - 1
def put(a, dx=0, dy=0):
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); im.alpha_composite(Image.fromarray(a), (CX - crest(a) + dx, H - a.shape[0] + dy)); return im
def clean(im):
    a = np.array(im); lab, k = nd.label(a[..., 3] > 0, structure=np.ones((3, 3)))
    for j in range(1, k + 1):
        if (lab == j).sum() < 6: a[lab == j] = 0
    return Image.fromarray(a)
# ---- walk, from his idle: the body dips by DELETING rows at the waist (so it is one piece, nothing stays behind), leans by a column shear, the big fist and the right foot are cut-out pieces that lift and come down,
# the small right arm swings by a row shear.
I = P["idle"]; BIGX, BIGTOP, RLX0, RLX1, RLTOP, WAIST = 31, 51, 36, 50, 49, 44; ARMX = 50
def piece(x0, x1, top):
    p = np.zeros_like(I); p[top:, x0:x1] = I[top:, x0:x1]; return p
PBIG, PRL = piece(0, BIGX, BIGTOP), piece(RLX0, RLX1, RLTOP)
BODY = I.copy(); BODY[BIGTOP:, :BIGX] = 0; BODY[RLTOP:, RLX0:RLX1] = 0
def dip(a, rows):                                                           # delete `rows` rows at the waist: everything above moves down
    if rows == 0: return a
    out = np.zeros_like(a); out[rows + 0: WAIST + rows] = a[0: WAIST]; out[WAIST + rows:] = a[WAIST + rows:]; return out
def shift(a, dx=0, dy=0):
    out = np.zeros_like(a); h, w = a.shape[:2]
    ys, xs = np.where(a[..., 3] > 0); ny, nx = ys + dy, xs + dx; ok = (ny >= 0) & (ny < h) & (nx >= 0) & (nx < w); out[ny[ok], nx[ok]] = a[ys[ok], xs[ok]]; return out
def lean(a, amount):
    if amount == 0: return a
    out = np.zeros_like(a); h, w = a.shape[:2]
    for x in range(w):
        sh = int(round(amount * (x - w // 2) / (w / 2.0)))
        for y in range(h):
            if a[y, x, 3] and 0 <= y + sh < h: out[y + sh, x] = a[y, x]
    return out
def armshear(a, swing):
    out = a.copy(); top = 28; h = a.shape[0]; out[top:, ARMX:] = 0
    for y in range(top, h):
        s = int(round(swing * (y - top) / (h - top)))
        for x in range(ARMX, a.shape[1]):
            if a[y, x, 3] and 0 <= x + s < a.shape[1]: out[y, x + s] = a[y, x]
    return out
BOB = [3, 2, 0, 1, 3, 2, 0, 1]; LIFT_BIG = [0, 2, 3, 2, 0, 0, 0, 0]; LIFT_R = [0, 0, 0, 0, 0, 2, 4, 2]; LEAN = [0, 1, 1, 0, 0, -1, -1, 0]; ARM = [0, -1, -2, -1, 0, 1, 2, 1]; FWD_BIG = [0, 1, 2, 1, 0, 0, 0, 0]
def walk_frame(i):
    pad = 6
    def big(a): c = np.zeros((a.shape[0] + pad, a.shape[1] + 2 * pad, 4), np.uint8); c[pad:, pad: pad + a.shape[1]] = a; return c
    body = lean(dip(big(armshear(BODY, ARM[i])), BOB[i]), LEAN[i]); pb = shift(big(PBIG), FWD_BIG[i], -LIFT_BIG[i]); pr = shift(big(PRL), 0, -LIFT_R[i])
    im = np.zeros_like(body)
    for l in (pb, pr, body):
        m = l[..., 3] > 0; im[m] = l[m]
    f = Image.new("RGBA", (W, H), (0, 0, 0, 0)); f.alpha_composite(Image.fromarray(im[pad:]), (CX - crest(I) - pad, H - I.shape[0]))
    return clean(f)
walk = [walk_frame(i) for i in range(8)]
for n_, r_ in ((1, 1), (2, 2), (3, 3), (4, 4), (5, 5), (6, 6)): P[f"dip{n_}"] = dip(I, r_)
P["crouch"] = P["dip4"]
# ---- the rock (boulder), its shatter and the slam dust, drawn here
STONE = {"OL": (22, 20, 28), "DK": (58, 56, 64), "MID": (112, 110, 114), "HI": (150, 146, 134), "HI2": (178, 174, 160), "DIRT": (96, 74, 50)}
def rock_frames(n=6, size=26):
    """a BOULDER of GROUND stone (Yan: it looked like a fidget spinner and like it came from the Brute): an irregular angular polygon (9 corners, flat faces, no lobes), grey-brown stone, light from the upper left in three flat tones, dark cracks, a little dirt clinging at the bottom, no yellow; it tumbles over 6 frames"""
    rng = np.random.RandomState(11); k = 9; base = [(2 * math.pi * i / k + rng.uniform(-0.25, 0.25), 0.74 + 0.26 * rng.rand()) for i in range(k)]
    cracks = [(rng.uniform(-0.5, 0.5), rng.uniform(-0.5, 0.5), rng.uniform(0, 6.28), rng.uniform(0.25, 0.5)) for _ in range(3)]; out = []
    c = size / 2.0 - 0.5; r0 = size * 0.46
    for i in range(n):
        rot = i * (2 * math.pi / n) * 1.0; big = 4; im = Image.new("L", (size * big, size * big), 0); dr = ImageDraw.Draw(im)
        pts = [((c + math.cos(a + rot) * rr * r0) * big + big / 2, (c + math.sin(a + rot) * rr * r0) * big + big / 2) for a, rr in base]; dr.polygon(pts, fill=255)
        m = np.array(im.resize((size, size), Image.NEAREST)) > 127; edge = m & ~nd.binary_erosion(m); arr = np.zeros((size, size, 4), np.uint8)
        for y in range(size):
            for x in range(size):
                if not m[y, x]: continue
                if edge[y, x]: arr[y, x] = (*STONE["OL"], 255); continue
                lit = ((x - c) * -0.65 + (y - c) * -0.75) / r0; d = math.hypot(x - c, y - c) / r0
                col = STONE["HI2"] if (lit > 0.62 and d > 0.25) else STONE["HI"] if lit > 0.2 else STONE["MID"] if lit > -0.35 else STONE["DK"]; arr[y, x] = (*col, 255)
        for (ox, oy, ang, ln) in cracks:                                         # cracks stay on the rock and turn with it
            ca, sa = math.cos(rot), math.sin(rot); px, py = (ox * ca - oy * sa) * r0 + c, (ox * sa + oy * ca) * r0 + c
            for st in range(int(ln * r0 * 1.6)):
                x, y = int(round(px + math.cos(ang + rot) * st)), int(round(py + math.sin(ang + rot) * st))
                if 0 <= x < size and 0 <= y < size and arr[y, x, 3] and not edge[y, x]: arr[y, x] = (*STONE["DK"], 255)
        for y in range(size - 1, -1, -1):                                         # a little dirt clings to the underside of the rock
            for x in range(size):
                if arr[y, x, 3] and not edge[y, x] and ((y - c) / r0 > 0.45 + 0.2 * math.sin(x * 1.3 + i)) and rng.rand() < 0.35: arr[y, x] = (*STONE["DIRT"], 255)
        out.append(Image.fromarray(arr))
    return out
def hit_frames(n=6, w=60, h=40):
    """the boulder bursts on the target: a flash of dust, big chunks flying out and falling, a ground shockwave ring, pebbles left behind"""
    OL, MID, DK, DUST, DUST2 = STONE["OL"], STONE["MID"], STONE["DK"], (176, 160, 128), (120, 104, 80); rng = np.random.RandomState(9); ch = [(rng.uniform(-1, 1), rng.uniform(0.5, 1.0), rng.choice([4, 5, 6])) for _ in range(9)]; out = []
    for i in range(n):
        img = Image.new("RGBA", (w, h), (0, 0, 0, 0)); dr = ImageDraw.Draw(img); cx, cy = w / 2.0, h - 8.0; t = i / (n - 1.0)
        if i < 5:
            pr = 6 + 6 * i; al = 255 - 45 * i; dr.ellipse([cx - pr, cy - pr * 0.5, cx + pr, cy + pr * 0.5], fill=(*DUST, al)); dr.ellipse([cx - pr * 0.6, cy - pr * 0.6, cx + pr * 0.6, cy + pr * 0.1], fill=(*DUST2, int(al * 0.6)))
        rx = 5 + 24 * t; dr.ellipse([cx - rx, cy - rx * 0.3, cx + rx, cy + rx * 0.3], outline=(*DUST, max(0, 230 - 45 * i)), width=2)       # the ground shockwave
        for k, (dx, up, sz) in enumerate(ch):
            x = cx + dx * (6 + 22 * t); y = cy - up * 20 * t + 24 * t * t; s = sz if i < 3 else max(2, sz - 2)
            if i < n - 1: dr.rectangle([x - s / 2, y - s / 2, x + s / 2, y + s / 2], fill=(*(MID if k % 2 else DK), 255), outline=(*OL, 255))
        out.append(img)
    return out
def dust_frames(n=6, w=64, h=24):
    DUST, DK = (160, 150, 118), (110, 102, 78); out = []
    for i in range(n):
        img = Image.new("RGBA", (w, h), (0, 0, 0, 0)); dr = ImageDraw.Draw(img); cx, cy = w / 2.0, h - 5.0; rx = 6 + 5.2 * i; ry = 2 + 1.7 * i; al = max(40, 255 - 42 * i)
        dr.ellipse([cx - rx, cy - ry, cx + rx, cy + ry], outline=(*DUST, al), width=2)
        if i < 4: dr.ellipse([cx - rx * 0.6, cy - ry * 0.5, cx + rx * 0.6, cy + ry * 0.5], fill=(*DK, int(al * 0.55)))
        for k in range(5):
            ang = k * 1.26 + 0.4; x = cx + math.cos(ang) * rx * 0.9; y = cy - abs(math.sin(ang)) * (3 + 6 * i / n) - 2 * i
            if i < n - 1: dr.rectangle([x - 1, y - 1, x + 1, y + 1], fill=(*DK, 255))
        out.append(img)
    return out
def boulder_frames(n=6):
    """Yan's generated boulder (boulder_recover.py -> boulder.png), the same image 6 times (the game spins it in flight), padded to a square"""
    b = Image.open(os.path.join(OUT, "boulder.png")).convert("RGBA"); sz = max(b.size); sq = Image.new("RGBA", (sz, sz), (0, 0, 0, 0)); sq.alpha_composite(b, ((sz - b.width) // 2, (sz - b.height) // 2)); return [sq.copy() for _ in range(n)]
ROCK, HIT, DUST = boulder_frames(), hit_frames(), dust_frames()
# ---- the attacks: his poses held with timings; the arc (swipe) and the rock in the fist (throw) are drawn here
def hand(a):
    """the right fist: the rightmost opaque pixels of the pose -> (x, y) in the pose's own cells"""
    ys, xs = np.where(a[..., 3] > 0); xm = xs.max(); sel = xs >= xm - 6; return int(xm), int(ys[sel].mean()), int(ys[sel].min())
def arc_over(im, a, i):
    hx, hy, _ = hand(a); ox = CX - crest(a); oy = H - a.shape[0]; cx, cy = ox + hx - 20, oy + hy + 2; d = ImageDraw.Draw(im)
    spans = [(-80, -25), (-80, 15), (-55, 60)]; a0, a1 = spans[i]
    for rr, col, wd in ((22, (235, 238, 190, 255), 2), (20, (200, 208, 70, 255), 1)):
        pts = [(cx + math.cos(math.radians(g)) * rr * 1.15, cy + math.sin(math.radians(g)) * rr) for g in range(a0, a1, 4)]
        if len(pts) > 1: d.line(pts, fill=col, width=wd)
    return im
def rock_in_fist(im, a, fr):
    hx, hy, ytop = hand(a); ox = CX - crest(a); oy = H - a.shape[0]; rk = ROCK[fr % 6]; im.alpha_composite(rk, (ox + hx - 11, oy + ytop - 12)); return im
def build(seq):
    out = []
    for key, ms, fx, n, *dxs in seq:
        im = put(P[key], dxs[0] if dxs else 0)
        if fx == "arc": im = arc_over(im, P[key], n)
        elif fx == "rock": im = rock_in_fist(im, P[key], n)
        out.append(clean(im))
    return out
SEQ = {
    "slam":  [("idle", 100, None, 0), ("dip2", 140, None, 0), ("dip3", 200, None, 0), ("dip2", 90, None, 0), ("dip6", 70, None, 0), ("dip5", 90, None, 0), ("dip3", 120, None, 0), ("dip1", 130, None, 0), ("idle", 200, None, 0)],      # squash only (deleting rows never smears): wind down, hold, drop hard
    "swipe": [("idle", 100, None, 0), ("cock", 280, None, 0), ("cock", 50, None, 0, -1), ("out", 50, None, 0, 1), ("across", 70, None, 0, 2), ("across", 130, None, 0, 2), ("across", 100, None, 0, 1), ("idle", 220, None, 0)],      # Yan: "keep that for the swipe/punch"
    "throw": [("idle", 120, None, 0), ("dip2", 150, None, 0), ("dip4", 220, None, 0), ("dip1", 90, None, 0), ("idle", 120, None, 0), ("idle", 250, None, 0)],      # no arm pose (Yan scrapped the cocked-arm throw): he heaves, the rock appears in flight (drawn by the game)
}
CONTACT = {"slam": 4, "swipe": 4, "throw": 3}
def strip_of(fr):
    s = Image.new("RGBA", (W * len(fr), H), (0, 0, 0, 0))
    for i, f in enumerate(fr): s.alpha_composite(f, (i * W, 0))
    return s
def gif(path, frames, ms, sc=6):
    fs = []
    for f in frames:
        bg = Image.new("RGBA", (W, H), (54, 49, 41, 255)); ImageDraw.Draw(bg).rectangle([0, ground + 1, W, H], fill=(60, 55, 46, 255)); bg.alpha_composite(f); fs.append(bg.resize((W * sc, H * sc), Image.NEAREST).convert("RGB").quantize(64, dither=Image.NONE))
    d = list(ms); d[-1] += 500; fs[0].save(path, save_all=True, append_images=fs[1:], duration=d, loop=0)
meta = {}
for name, seq in SEQ.items():
    fr = build(seq); ms = [t[1] for t in seq]
    strip_of(fr).save(os.path.join(OUT, f"blight_brute_{name}_strip.png")); gif(os.path.join(OUT, f"{name}.gif"), fr, ms)
    meta[name] = {"frame": [W, H], "ms": ms, "travel": [0.0] * len(fr)}
for i, f in enumerate(walk): f.save(os.path.join(OUT, f"frame{i}.png"))
strip_of(walk).save(os.path.join(OUT, "blight_brute_walk_strip.png")); gif(os.path.join(OUT, "walk.gif"), walk, [200] * 8)
json.dump({"blight_brute": meta}, open(os.path.join(OUT, "brute_attacks.json"), "w"), indent=1)
fx = os.path.join(OUT, "fx"); os.makedirs(fx, exist_ok=True)
fxmeta = {}
for nm, fr in (("boulder", ROCK), ("rock_hit", HIT), ("slam_dust", DUST)):
    w, h = fr[0].size; s = Image.new("RGBA", (w * len(fr), h), (0, 0, 0, 0))
    for i, f in enumerate(fr): s.alpha_composite(f, (i * w, 0))
    s.save(os.path.join(fx, f"brute_{nm}_strip.png")); fxmeta[nm] = {"w": w, "h": h, "frames": len(fr)}
json.dump(fxmeta, open(os.path.join(OUT, "brute_fx.json"), "w"), indent=1)
# ---- the 64x64 stand sprite (feet on the bottom row, centred on the crest like the frames)
st = Image.new("RGBA", (64, 64), (0, 0, 0, 0)); st.alpha_composite(Image.fromarray(I), ((64 - I.shape[1]) // 2, 64 - I.shape[0])); st.save(os.path.join(OUT, "stand_64.png"))
print("poses", {n: P[n].shape for n in P}); print("walk lowest rows", [int(np.where(np.array(f)[..., 3].any(1))[0].max()) for f in walk], "ground", ground)
for name in SEQ:
    sp = np.array(Image.open(os.path.join(OUT, f"blight_brute_{name}_strip.png"))); print(name, "lowest rows", [int(np.where(sp[:, i * W:(i + 1) * W, 3].any(1))[0].max()) for i in range(sp.shape[1] // W)])
