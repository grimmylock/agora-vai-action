# Blight Brute attacks (2026-11-12, Yan: "lateral swipe", "ground slam with the biiig arm, no jumps, he's too heavy", boulder throw with a boulder sprite): a lateral SWIPE, a GROUND SLAM and a BOULDER THROW on the
# Brute that is in the game (64x64), whole-pixel moves only. The body dips and leans (offset + a column-shear tilt), the legs stay planted as his own pixels, the big arm is cut out and posed by a row shear from the
# shoulder; raised overhead it is the same piece flipped upside down (a lossless flip), so the fist is on top. The boulder, the rip's rubble, the rock's shatter and the slam's dust ring are drawn here in code.
#   python3 brute_attacks.py <out_dir>
import math, os, sys, json
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as nd
import brute_walk as BW

S, BODY, PIECE, PARTS = BW.S, BW.BODY, BW.PIECE, BW.PARTS
ox, oy = 26, 30; W, H = S.shape[1] + 2 * ox, S.shape[0] + 2 * oy; LAST = S.shape[0] - 1; GROUND = oy + LAST
BW.ox, BW.oy, BW.W, BW.H = ox, oy, W, H
SHOULDER_X, ARM_TOP, ARM_BOT = 57, PARTS["arm"][1], LAST - 1                 # the arm piece hangs from (57, 30) to its fist near row 62

def arm_layer(p):
    """the big arm: sheared from the shoulder by `ashear` px at the fist, moved by (adx, ady); aflip = raised overhead (the piece flipped upside down, the shoulder end stays put)"""
    a = np.zeros((H, W, 4), np.uint8); piece = PIECE["arm"]; span = ARM_BOT - ARM_TOP; sh = p.get("ashear", 0); adx, ady = p.get("adx", 0), p.get("ady", 0) + p.get("by", 0) // 2; flip = p.get("aflip", False)
    for y in range(ARM_TOP, S.shape[0]):
        d = y - ARM_TOP; s = int(round(sh * d / span))
        yy = (ARM_TOP - d - 2 + oy + ady + p.get("by", 0) // 2) if flip else (y + oy + ady)                  # flipped: the fist end goes up, the shoulder end stays just above the shoulder
        for x in range(S.shape[1]):
            if piece[y, x, 3]:
                xx = x + s + adx + ox + p.get("bx", 0) // 2
                if 0 <= xx < W and 0 <= yy < H: a[yy, xx] = piece[y, x]
    return a

def fist(p):
    """where the fist is, in frame pixels (x, y)"""
    sh = p.get("ashear", 0); adx, ady = p.get("adx", 0), p.get("ady", 0) + p.get("by", 0) // 2; span = ARM_BOT - ARM_TOP
    x = SHOULDER_X + sh + adx + ox + p.get("bx", 0) // 2
    y = (ARM_TOP - span - 2 + oy + ady + p.get("by", 0) // 2) if p.get("aflip") else (ARM_BOT + oy + ady)
    return x, y

def body_layers(p):
    bx, by, tilt = p.get("bx", 0), p.get("by", 0), p.get("tilt", 0)
    lower = np.zeros((H, W, 4), np.uint8); upper = np.zeros((H, W, 4), np.uint8)
    for y in range(S.shape[0]):
        if y >= BW.SPLIT: lower[y + oy, ox: ox + S.shape[1]] = BODY[y]
        elif y + by + oy < H: upper[y + by + oy, ox + bx: ox + bx + S.shape[1]] = BODY[y]
    return BW.tilt(upper, tilt), lower

# ---- the effects
def rock_frames(n=6, size=20):
    """a boulder tumbling: a lumpy blob in the Brute's own olive and dark greens with yellow flecks, a different lump pattern per frame"""
    OL, MID, DK, HI, FL = (14, 14, 8), (88, 84, 44), (46, 44, 24), (132, 128, 70), (200, 208, 70)
    rng = np.random.RandomState(5); out = []
    for i in range(n):
        a = np.zeros((size, size, 4), np.uint8); c = size / 2.0 - 0.5; ph = i * 1.05; r0 = size * 0.36
        for y in range(size):
            for x in range(size):
                ang = math.atan2(y - c, x - c); r = r0 * (1 + 0.16 * math.sin(3 * ang + ph) + 0.1 * math.sin(5 * ang - 2 * ph)); d = math.hypot(x - c, y - c)
                if d <= r:
                    t = d / r; lit = (x - c) * -0.6 + (y - c) * -0.8
                    col = HI if (lit > r * 0.35 and t > 0.5) else DK if (lit < -r * 0.35 and t > 0.4) else MID
                    a[y, x] = (*col, 255)
                elif d <= r + 1.0: a[y, x] = (*OL, 255)
        for _ in range(3):
            x, y = int(c + rng.uniform(-3, 3)), int(c + rng.uniform(-3, 3))
            if a[y, x, 3] and tuple(a[y, x, :3]) != OL: a[y, x] = (*FL, 255)
        out.append(Image.fromarray(a))
    return out

def hit_frames(n=6, w=36, h=24):
    """the rock shatters on the target: chunks flying out, a dust puff, then a few pebbles"""
    OL, MID, DK, DUST = (14, 14, 8), (88, 84, 44), (46, 44, 24), (150, 142, 112); rng = np.random.RandomState(9); ch = [(rng.uniform(-1, 1), rng.uniform(0.4, 1.0)) for _ in range(7)]; out = []
    for i in range(n):
        a = np.zeros((h, w, 4), np.uint8); cx, cy = w / 2.0, h - 6.0; d = ImageDraw.Draw(Image.fromarray(a)) if False else None
        img = Image.fromarray(a); dr = ImageDraw.Draw(img)
        if i < 4:
            pr = 3 + 3 * i; al = 255 - 55 * i
            dr.ellipse([cx - pr, cy - pr * 0.45, cx + pr, cy + pr * 0.45], fill=(*DUST, al))
        for k, (dx, up) in enumerate(ch):
            t = i / (n - 1.0); x = cx + dx * (4 + 11 * t); y = cy - up * 12 * t + 14 * t * t; s = 3 if i < 3 else 2
            if i < n - 1: dr.rectangle([x - s / 2, y - s / 2, x + s / 2, y + s / 2], fill=(*(MID if k % 2 else DK), 255), outline=(*OL, 255))
        out.append(img)
    return out

def dust_frames(n=6, w=64, h=24):
    """the slam's shockwave: a dust ring racing out along the ground, fading, with a few pebbles thrown up"""
    DUST, DK = (160, 150, 118), (110, 102, 78); rng = np.random.RandomState(3); out = []
    for i in range(n):
        img = Image.new("RGBA", (w, h), (0, 0, 0, 0)); dr = ImageDraw.Draw(img); cx, cy = w / 2.0, h - 5.0; rx = 6 + 5.2 * i; ry = 2 + 1.7 * i; al = max(40, 255 - 42 * i)
        dr.ellipse([cx - rx, cy - ry, cx + rx, cy + ry], outline=(*DUST, al), width=2)
        if i < 4: dr.ellipse([cx - rx * 0.6, cy - ry * 0.5, cx + rx * 0.6, cy + ry * 0.5], fill=(*DK, int(al * 0.55)))
        for k in range(5):
            ang = k * 1.26 + 0.4; x = cx + math.cos(ang) * rx * 0.9; y = cy - abs(math.sin(ang)) * (3 + 6 * i / n) - 2 * i
            if i < n - 1: dr.rectangle([x - 1, y - 1, x + 1, y + 1], fill=(*DK, 255))
        out.append(img)
    return out

ROCK = rock_frames(); HIT = hit_frames(); DUST = dust_frames()

def render(p):
    upper, lower = body_layers(p)
    layers = [lower, upper, BW.leg("leg_l", 0, p.get("lift_l", 0), 0), BW.leg("leg_r", 0, p.get("lift_r", 0), 0), arm_layer(p)]
    im = np.zeros((H, W, 4), np.uint8)
    for l in layers:
        m = l[..., 3] > 0; im[m] = l[m]
    out = Image.fromarray(im)
    r = p.get("rock")                                                # (frame, dx, dy): a boulder held in the fist
    if r is not None:
        fx, fy = fist(p); rk = ROCK[r[0] % 6]; out.alpha_composite(rk, (int(fx - rk.width / 2 + r[1]), int(fy - rk.height / 2 + r[2])))
    if p.get("dust"): d = DUST[p["dust"] - 1]; fx, fy = fist(p); out.alpha_composite(d, (int(fx - d.width / 2), GROUND - d.height + 6))
    a = np.array(out); lab, k = nd.label(a[..., 3] > 0, structure=np.ones((3, 3)))
    for j in range(1, k + 1):
        if (lab == j).sum() < 6: a[lab == j] = 0
    return Image.fromarray(a)

SWIPE = [
    (dict(), 110, 0),
    (dict(ashear=-8, bx=-2, by=1, tilt=-1), 100, 0),
    (dict(ashear=-13, adx=-2, bx=-3, by=2, tilt=-1), 90, 0),
    (dict(ashear=3, adx=1, bx=1, by=3, tilt=1), 50, 0),
    (dict(ashear=12, adx=3, bx=5, by=4, tilt=2), 50, 0),
    (dict(ashear=14, adx=3, bx=6, by=4, tilt=2), 100, 0),
    (dict(ashear=6, adx=1, bx=3, by=2, tilt=1), 110, 0),
    (dict(ashear=2, bx=1, by=1), 120, 0),
]
SLAM = [
    (dict(), 120, 0),
    (dict(ashear=-9, bx=-2, by=1, tilt=-1), 130, 0),
    (dict(aflip=True, ashear=-5, adx=-2, bx=-3, by=2, tilt=-2), 120, 0),
    (dict(aflip=True, ashear=1, adx=2, bx=-4, by=3, tilt=-3), 150, 0),
    (dict(ashear=6, adx=2, bx=2, by=4, tilt=1), 40, 0),
    (dict(ashear=11, adx=6, bx=6, by=7, tilt=2, dust=1), 60, 0),
    (dict(ashear=11, adx=6, bx=6, by=7, tilt=2, dust=2), 70, 0),
    (dict(ashear=10, adx=6, bx=6, by=6, tilt=2, dust=3), 90, 0),
    (dict(ashear=8, adx=4, bx=4, by=4, tilt=1, dust=4), 120, 0),
    (dict(ashear=4, adx=2, bx=2, by=2, dust=5), 130, 0),
    (dict(ashear=1, bx=1, by=1, dust=6), 140, 0),
]
THROW = [
    (dict(), 110, 0),
    (dict(ashear=4, ady=2, bx=3, by=6, tilt=2, rock=(0, 0, -2)), 130, 0),
    (dict(ashear=3, ady=2, bx=2, by=5, tilt=1, rock=(1, 0, -4)), 110, 0),
    (dict(aflip=True, ashear=-2, adx=1, bx=0, by=3, rock=(2, 0, -5)), 110, 0),
    (dict(aflip=True, ashear=-9, adx=-3, bx=-4, by=1, tilt=-3, rock=(3, -1, -6)), 120, 0),
    (dict(ashear=8, adx=3, bx=3, by=3, tilt=2), 45, 0),
    (dict(ashear=12, adx=4, bx=5, by=4, tilt=2), 90, 0),
    (dict(ashear=6, adx=2, bx=2, by=2, tilt=1), 110, 0),
    (dict(ashear=2, bx=1, by=1), 110, 0),
]

def strip_of(frames):
    s = Image.new("RGBA", (W * len(frames), H), (0, 0, 0, 0))
    for i, f in enumerate(frames): s.alpha_composite(f, (i * W, 0))
    return s

def gif(path, frames, ms, sc=8):
    fs = []
    for f in frames:
        bg = Image.new("RGBA", (W, H), (54, 49, 41, 255)); d = ImageDraw.Draw(bg); d.rectangle([0, GROUND + 1, W, H], fill=(60, 55, 46, 255)); bg.alpha_composite(f)
        fs.append(bg.resize((W * sc, H * sc), Image.NEAREST).convert("RGB").quantize(colors=64, method=Image.MEDIANCUT, dither=Image.NONE))
    d = list(ms); d[-1] += 500; fs[0].save(path, save_all=True, append_images=fs[1:], duration=d, loop=0)

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "mutants_anim/brute_attacks"; fx = os.path.join(out, "fx"); os.makedirs(fx, exist_ok=True); res = {}
    for name, poses in (("swipe", SWIPE), ("slam", SLAM), ("throw", THROW)):
        fr = [render(p) for p, _, _ in poses]; ms = [t for _, t, _ in poses]
        for i, f in enumerate(fr): f.save(os.path.join(out, f"{name}_{i}.png"))
        strip_of(fr).save(os.path.join(out, f"blight_brute_{name}_strip.png")); gif(os.path.join(out, f"{name}.gif"), fr, ms)
        res[name] = {"frame": [W, H], "ms": ms, "ground_row": GROUND}
        print(name, len(fr), "frames; components", [nd.label(np.array(f)[..., 3] > 0, structure=np.ones((3, 3)))[1] for f in fr], "lowest", [int(np.where(np.array(f)[..., 3] > 0)[0].max()) for f in fr], "ground", GROUND)
        sc = 5; per = 6; rows = (len(fr) + per - 1) // per; sheet = Image.new("RGBA", (W * per * sc, H * rows * sc), (105, 115, 90, 255))
        for k, f in enumerate(fr): sheet.alpha_composite(f.resize((W * sc, H * sc), Image.NEAREST), ((k % per) * W * sc, (k // per) * H * sc))
        sheet.convert("RGB").save(os.path.join(out, f"{name}_sheet.png"))
    for nm, fr in (("boulder", ROCK), ("rock_hit", HIT), ("slam_dust", DUST)):
        w, h = fr[0].size; s = Image.new("RGBA", (w * len(fr), h), (0, 0, 0, 0))
        for i, f in enumerate(fr): s.alpha_composite(f, (i * w, 0))
        s.save(os.path.join(fx, f"brute_{nm}_strip.png")); res[nm] = {"frame": [w, h], "frames": len(fr)}
    fxs = Image.new("RGBA", (64 * 6 * 6, 3 * 40 * 6), (105, 115, 90, 255)); y = 0
    for fr in (ROCK, HIT, DUST):
        s = Image.new("RGBA", (fr[0].width * len(fr), fr[0].height), (0, 0, 0, 0))
        for i, f in enumerate(fr): s.alpha_composite(f, (i * f.width, 0))
        fxs.alpha_composite(s.resize((s.width * 6, s.height * 6), Image.NEAREST), (0, y)); y += 40 * 6
    fxs.convert("RGB").save(os.path.join(out, "fx_sheet.png"))
    json.dump({"blight_brute": res}, open(os.path.join(out, "brute_attacks.json"), "w"), indent=1)
