# Blight Brute (2026-11-12): the NEW Brute, Yan's AI-generated pose sheet (refs/brute/brute_poses_ai.jpg) recovered to game pixels by brute_recover.py (mutants_anim/brute_poses/*.png), replaces the old sprite everywhere.
# Builds: the 64x64 stand sprite, the 8-frame walk (the idle's body + two leg pieces, whole-pixel moves only), the slam / swipe / throw strips (his poses held with timings, crest-aligned and feet on one row), and the
# rock, rock-shatter and slam-dust strips (code-drawn).  python3 brute_build.py   ->  mutants_anim/brute/ (strips + json) and Assets/Resources/Sprites/Mutants/mutant_hd_blight_brute.png
import os, json, math
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as nd
HERE = os.path.dirname(os.path.abspath(__file__)); D = os.path.join(HERE, "mutants_anim", "brute_poses"); OUT = os.path.join(HERE, "mutants_anim", "brute"); os.makedirs(OUT, exist_ok=True)
NAMES = ["idle", "slam_up", "slam_hit", "reach", "swipe", "wind", "throw_up", "throw"]
P = {}
for n in NAMES:                                                          # drop the baked-in ground line (a solid dark bottom row) if any is left
    a = np.array(Image.open(os.path.join(D, n + ".png")))
    for _ in range(3):
        r = a[-1]; al = r[:, 3] > 0
        if al.sum() > 0.6 * a.shape[1] and (r[al, :3].sum(1) < 90).mean() > 0.6 and (a[-2][:, 3] > 0).sum() < 0.8 * al.sum() + 4: a = a[:-1]
        else: break
    ys, xs = np.where(a[..., 3] > 0); P[n] = a[ys.min(): ys.max() + 1, xs.min(): xs.max() + 1]
def crest(a):
    yel = (a[..., 1] > 180) & (a[..., 2] < 120) & (a[..., 3] > 0); ys, xs = np.where(yel[: a.shape[0] * 2 // 5]); return int(round(xs.mean()))
W, H = 104, 76; CX = 56; ground = H - 1                                     # frame size, crest column, ground row (the feet's lowest row)
def put(a, dx=0, dy=0):
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); im.alpha_composite(Image.fromarray(a), (CX - crest(a) + dx, H - a.shape[0] + dy)); return im
def clean(im):
    a = np.array(im); lab, k = nd.label(a[..., 3] > 0, structure=np.ones((3, 3)))
    for j in range(1, k + 1):
        if (lab == j).sum() < 6: a[lab == j] = 0
    return Image.fromarray(a)
# ---- walk: the idle in two pieces' worth of motion: the body (everything above the legs) leans and dips, each foot is lifted (a cut-out piece, top overlapped by the body), the small arm swings
I = P["idle"]; LEGTOP = 49; LX = (0, 33); RX = (38, 53); ARMX = (54, 62)
def piece(x0, x1, top):
    p = np.zeros_like(I); p[top:, x0:x1] = I[top:, x0:x1]; return p
PL, PR = piece(*LX, LEGTOP + 2), piece(*RX, LEGTOP)
BODY = I.copy(); BODY[LEGTOP + 2:, LX[0]:LX[1]] = 0; BODY[LEGTOP:, RX[0]:RX[1]] = 0
def shift(a, dx=0, dy=0):
    out = np.zeros_like(a); h, w = a.shape[:2]
    for y in range(h):
        for x in range(w):
            if a[y, x, 3] and 0 <= y + dy < h and 0 <= x + dx < w: out[y + dy, x + dx] = a[y, x]
    return out
def lean(a, amount):                                                      # column shear around the middle: the head end rises/falls by up to `amount` rows
    if amount == 0: return a
    out = np.zeros_like(a); h, w = a.shape[:2]
    for x in range(w):
        sh = int(round(amount * (x - w // 2) / (w / 2.0)))
        for y in range(h):
            if a[y, x, 3] and 0 <= y + sh < h: out[y + sh, x] = a[y, x]
    return out
def armshear(a, swing):                                                   # the small arm (right edge) swings by a row shear from the shoulder
    out = a.copy(); top = 22; h = a.shape[0]; x0, x1 = ARMX
    out[top:, x0:x1] = 0
    for y in range(top, h):
        s = int(round(swing * (y - top) / (h - top)))
        for x in range(x0, x1):
            if a[y, x, 3] and 0 <= x + s < a.shape[1]: out[y, x + s] = a[y, x]
    return out
BOB = [3, 2, 0, 1, 3, 2, 0, 1]; LIFT_L = [0, 2, 4, 2, 0, 0, 0, 0]; LIFT_R = [0, 0, 0, 0, 0, 2, 4, 2]; BX = [0, -1, -1, 0, 0, 1, 1, 0]; LEAN = [0, 1, 1, 0, 0, -1, -1, 0]; ARM = [0, -1, -2, -1, 0, 1, 2, 1]
def walk_frame(i):
    pad = 8; hh, ww = I.shape[0] + pad, I.shape[1] + 2 * pad
    def big(a): c = np.zeros((hh, ww, 4), np.uint8); c[pad:, pad: pad + a.shape[1]] = a; return c
    body = big(armshear(BODY, ARM[i])); body = lean(shift(body, BX[i], BOB[i]), LEAN[i])
    legL = shift(big(PL), 0, -LIFT_L[i]); legR = shift(big(PR), 0, -LIFT_R[i])
    im = np.zeros_like(body)
    for l in (legL, legR, body):
        m = l[..., 3] > 0; im[m] = l[m]
    ys, xs = np.where(im[..., 3] > 0); im = im[pad:]; im[im.shape[0] - 1:, :] = im[im.shape[0] - 1:, :]
    f = Image.new("RGBA", (W, H), (0, 0, 0, 0)); sub = Image.fromarray(im[: I.shape[0] + 0]); f.alpha_composite(sub, (CX - crest(I) - pad, H - I.shape[0]))
    a = np.array(f); a[ground + 1:] = 0
    return clean(Image.fromarray(a))
walk = [walk_frame(i) for i in range(8)]
# ---- the rock (boulder), its shatter and the slam dust, drawn here
def rock_frames(n=6, size=16):
    OL, MID, DK, HI, FL = (14, 14, 8), (88, 84, 44), (46, 44, 24), (132, 128, 70), (200, 208, 70); rng = np.random.RandomState(5); out = []
    for i in range(n):
        a = np.zeros((size, size, 4), np.uint8); c = size / 2.0 - 0.5; ph = i * 1.05; r0 = size * 0.36
        for y in range(size):
            for x in range(size):
                ang = math.atan2(y - c, x - c); r = r0 * (1 + 0.16 * math.sin(3 * ang + ph) + 0.1 * math.sin(5 * ang - 2 * ph)); d = math.hypot(x - c, y - c)
                if d <= r:
                    lit = (x - c) * -0.6 + (y - c) * -0.8; t = d / r; col = HI if (lit > r * 0.35 and t > 0.5) else DK if (lit < -r * 0.35 and t > 0.4) else MID; a[y, x] = (*col, 255)
                elif d <= r + 1.0: a[y, x] = (*OL, 255)
        for _ in range(3):
            x, y = int(c + rng.uniform(-3, 3)), int(c + rng.uniform(-3, 3))
            if a[y, x, 3] and tuple(a[y, x, :3]) != OL: a[y, x] = (*FL, 255)
        out.append(Image.fromarray(a))
    return out
def hit_frames(n=6, w=36, h=24):
    OL, MID, DK, DUST = (14, 14, 8), (88, 84, 44), (46, 44, 24), (150, 142, 112); rng = np.random.RandomState(9); ch = [(rng.uniform(-1, 1), rng.uniform(0.4, 1.0)) for _ in range(7)]; out = []
    for i in range(n):
        img = Image.new("RGBA", (w, h), (0, 0, 0, 0)); dr = ImageDraw.Draw(img); cx, cy = w / 2.0, h - 6.0
        if i < 4: pr = 3 + 3 * i; dr.ellipse([cx - pr, cy - pr * 0.45, cx + pr, cy + pr * 0.45], fill=(*DUST, 255 - 55 * i))
        for k, (dx, up) in enumerate(ch):
            t = i / (n - 1.0); x = cx + dx * (4 + 11 * t); y = cy - up * 12 * t + 14 * t * t; s = 3 if i < 3 else 2
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
ROCK, HIT, DUST = rock_frames(), hit_frames(), dust_frames()
# ---- the attacks: his poses held with timings (name, ms, dx, dy); CONTACT = the frame where the damage lands
SEQ = {
    "slam":  [("idle", 140, 0, 0), ("slam_up", 480, 0, 0), ("slam_hit", 70, 0, 0), ("slam_hit", 330, 0, 0), ("slam_hit", 260, 0, 0), ("idle", 220, 0, 0)],
    "swipe": [("idle", 100, 0, 0), ("wind", 300, 0, 0), ("swipe", 80, 0, 0), ("reach", 200, 0, 0), ("reach", 140, 0, 0), ("idle", 200, 0, 0)],
    "throw": [("idle", 120, 0, 0), ("throw_up", 480, 0, 0), ("throw", 90, 0, 0), ("throw", 260, 0, 0), ("idle", 240, 0, 0)],
}
CONTACT = {"slam": 2, "swipe": 2, "throw": 2}
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
    fr = [clean(put(P[n], dx, dy)) for n, _, dx, dy in seq]; ms = [t for _, t, _, _ in seq]
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
print("poses", {n: P[n].shape for n in NAMES}); print("walk lowest rows", [int(np.where(np.array(f)[..., 3].any(1))[0].max()) for f in walk], "ground", ground)
for name in SEQ:
    s = np.array(Image.open(os.path.join(OUT, f"blight_brute_{name}_strip.png"))); print(name, "lowest rows", [int(np.where(s[:, i * W:(i + 1) * W, 3].any(1))[0].max()) for i in range(s.shape[1] // W)])
