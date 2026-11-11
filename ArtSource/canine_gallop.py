# Canine mutants, third try (2026-11-11). Yan: "take a look at the current mutants' animations, the dogs' running and attack animations especially, they do not look right".
# What was wrong with the second try (canine_cutout.py + make_canine_anims.py), judged on its strips:
#   * legs were TURNED with nearest-neighbour rotation: a three-pixel-wide leg turned 45 degrees drops and doubles pixels, so the legs shredded into mush and no pose read;
#   * the run had no key poses (all four legs bunched under the belly), the body did not rock, the feet did not keep to the ground;
#   * the bite sank the whole body flat (it read as lying down) and the jaw opened 17 degrees at most.
# This try keeps the rule that every pixel is one of Yan's, moved by WHOLE pixels and never dropped or doubled:
#   * a leg leans by SHEAR (each row slides sideways by a whole number of pixels, more the further from the hip; the shin again from the knee). Every pixel survives.
#   * the body rocks by shear too (each column slides up or down by a whole pixel, two at most end to end): a tilt, not a bend; hind and fore halves never move against each other.
#   * the jaw drops the same way (columns slide down, more toward the tip), up to a real gape; only a sliver of the sprite's own darkest colour fills the gap.
#   * each frame is grounded: the lowest foot stands on the ground line, except in the two flight frames of the gallop and during a leap.
# The neutral pose is still the original sprite exactly (check_neutral).
#   python3 canine_gallop.py <out_dir> [keys]     -> <key>_{run,bite,pounce}_strip.png, <key>_v3.gif, canine_anims_v3.json
import math, os, sys, json
from PIL import Image, ImageDraw
import mutant_from_ref as m
import canine_cutout as C

MX, MY = 22, 20                                             # margins round the sprite inside a frame

def shear_rows(img, pivot_y, tan):
    """Each row slides sideways by round((y - pivot_y) * tan): a lean with every pixel kept."""
    if abs(tan) < 1e-6: return img
    out = Image.new("RGBA", img.size, (0, 0, 0, 0)); src = img.load(); dst = out.load(); w, h = img.size
    for y in range(h):
        dx = int(round((y - pivot_y) * tan))
        for x in range(w):
            p = src[x, y]
            if p[3] and 0 <= x + dx < w: dst[x + dx, y] = p
    return out

def shear_cols(img, pivot_x, tan):
    """Each column slides up or down by round((x - pivot_x) * tan): a tilt (or a dropped jaw) with every pixel kept."""
    if abs(tan) < 1e-6: return img
    out = Image.new("RGBA", img.size, (0, 0, 0, 0)); src = img.load(); dst = out.load(); w, h = img.size
    for x in range(w):
        dy = int(round((x - pivot_x) * tan))
        for y in range(h):
            p = src[x, y]
            if p[3] and 0 <= y + dy < h: dst[x, y + dy] = p
    return out

def canvas(rig, im):
    out = Image.new("RGBA", (rig.W0 + 2 * MX, rig.H0 + 2 * MY), (0, 0, 0, 0)); out.alpha_composite(im, (MX, MY)); return out

class Dog:
    def __init__(self, key):
        self.key = key; self.rig = r = C.Rig(key, C.SPECS[key]); c = r.c
        self.W, self.H = r.W0 + 2 * MX, r.H0 + 2 * MY
        body = Image.new("RGBA", r.base.size, (0, 0, 0, 0))
        for n in ("hind", "fore", "head"): body.alpha_composite(r.parts[n])
        self.body = canvas(r, body); self.jaw = canvas(r, r.parts["jaw"]); self.ground = canvas(r, r.parts["ground"])
        self.legs = {n: (canvas(r, up), canvas(r, lo), (hip[0] + MX, hip[1] + MY), (knee[0] + MX, knee[1] + MY), far) for n, (up, lo, hip, knee, far) in r.legs.items()}
        self.cx = MX + r.W0 // 2; self.hinge = (c["hinge"][0] + MX, c["hinge"][1] + MY); self.tip = (c["tip"][0] + MX, c["tip"][1] + MY)
        px = r.base.load(); cols = sorted((px[x, y] for x in range(r.W0) for y in range(r.H0) if px[x, y][3]), key=lambda q: q[0] + q[1] + q[2]); self.deep = cols[max(0, len(cols) // 50)]
        self.amp = {"blind_maw": 0.6, "dread_prowler": 0.8}.get(key, 1.0)
        f0 = self.compose({}); self.floor = max(y for y in range(self.H) for x in range(self.W) if f0.getpixel((x, y))[3])

    def leg(self, f, name, ang_u, ang_l, bx, by, pitch):
        up, lo, hip, knee, far = self.legs[name]
        tu = math.tan(math.radians(max(-42, min(42, ang_u * self.amp)))); tl = math.tan(math.radians(max(-55, min(55, (ang_u + ang_l) * self.amp))))
        hy = by + int(round((hip[0] - self.cx) * pitch))                      # the hip rides the body's tilt
        kdx = int(round((knee[1] - hip[1]) * tu))                             # where the knee went
        f.alpha_composite(shear_rows(lo, knee[1], tl), (bx + kdx, hy))
        f.alpha_composite(shear_rows(up, hip[1], tu), (bx, hy))

    def compose(self, p):
        """p: bx/by body offset (px), pitch (tan; + = nose down), nx/ny head... (the head is part of the body here: it only moves with it), jaw (tan of the gape), fu/fl hu/hl leg lean (deg, + = foot
        forward) and shin, lag (deg: how far the far pair trails the near), far_only / near legs can be offset with fu2 hu2. flash."""
        f = Image.new("RGBA", (self.W, self.H), (0, 0, 0, 0)); bx, by, pitch = p.get("bx", 0), p.get("by", 0), p.get("pitch", 0.0); lag = p.get("lag", 0)
        f.alpha_composite(self.ground)
        for n in self.rig.c["order_far"]:
            hind = "hind" in n; self.leg(f, n, (p.get("hu", 0) if hind else p.get("fu", 0)) - lag, p.get("hl", 0) if hind else p.get("fl", 0), bx, by, pitch)
        body = shear_cols(self.body, self.cx, pitch); f.alpha_composite(body, (bx, by))
        jt = p.get("jaw", 0.0); jaw = self.jaw
        if jt > 0.01:
            hx, hy = self.hinge; tx, ty = self.tip
            gap = Image.new("RGBA", (self.W, self.H), (0, 0, 0, 0)); gd = ImageDraw.Draw(gap)
            gd.polygon([(hx, hy + 1), (tx + 1, ty + 1), (tx + 1, ty + 1 + int(round((tx - hx) * jt)))], fill=self.deep)
            gap = shear_cols(gap, self.cx, pitch); f.alpha_composite(gap, (bx, by))
            jaw = shear_cols(jaw, hx, jt)
        f.alpha_composite(shear_cols(jaw, self.cx, pitch), (bx, by))
        for n in self.rig.c["order_near"]:
            hind = "hind" in n; self.leg(f, n, p.get("hu", 0) if hind else p.get("fu", 0), p.get("hl", 0) if hind else p.get("fl", 0), bx, by, pitch)
        if p.get("flash"):
            sx, sy = self.tip[0] + bx - 1, self.tip[1] + by + 2
            for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1), (2, 0), (-2, 0), (0, 2), (0, -2)):
                if 0 <= sx + dx < self.W and 0 <= sy + dy < self.H: f.putpixel((sx + dx, sy + dy), (255, 250, 232, 255))
        return f

    def frame(self, p):
        """Composed, then stood on the ground: the lowest pixel of the animal sits on the floor line, `air` pixels above it when it is off the ground."""
        f = self.compose(p)
        low = max((y for y in range(self.H) for x in range(0, self.W, 1) if f.getpixel((x, y))[3]), default=self.floor)
        dy = self.floor - low - p.get("air", 0)
        if dy:
            g = Image.new("RGBA", (self.W, self.H), (0, 0, 0, 0)); g.alpha_composite(f, (0, 0)) if False else None
            g = Image.new("RGBA", (self.W, self.H + 40), (0, 0, 0, 0)); g.alpha_composite(f, (0, 20 + dy)); f = g.crop((0, 20, self.W, 20 + self.H))
        return m.outline(f)

# ---------------------------------------------------------------- the gallop: eight frames, four key poses and their in-betweens
# fu / hu: lean of the fore / hind pair (deg, + = foot forward); fl / hl: the shin against the thigh (- = folded back); pitch: + nose down; air: pixels off the ground; lag: the far pair trails.
RUN = [
    dict(fu=-34, fl=-30, hu=36, hl=-34, pitch=0.00, air=4, lag=6),     # 0 gathered, in the air: fore legs swept back under the chest, hind legs drawn forward under the belly
    dict(fu=-18, fl=-40, hu=30, hl=-10, pitch=-0.03, air=1, lag=8),    # 1 the hind feet come down ahead of where the fore feet left
    dict(fu=14, fl=-30, hu=6, hl=4, pitch=-0.06, air=0, lag=8),        # 2 the hind legs drive: nose up, fore legs reaching
    dict(fu=34, fl=-6, hu=-26, hl=8, pitch=-0.03, air=0, lag=6),       # 3 the push ends: hind legs straight out behind
    dict(fu=40, fl=6, hu=-38, hl=10, pitch=0.00, air=4, lag=4),        # 4 stretched out, in the air: fore legs out ahead, hind legs trailing
    dict(fu=30, fl=8, hu=-30, hl=-14, pitch=0.04, air=1, lag=8),       # 5 the fore feet come down: nose dips
    dict(fu=6, fl=4, hu=-8, hl=-34, pitch=0.06, air=0, lag=8),         # 6 the fore legs carry the weight, the hind legs fold and swing forward
    dict(fu=-22, fl=-12, hu=22, hl=-40, pitch=0.03, air=0, lag=6),     # 7 the fore legs push off behind the shoulder
]
RUN_MS = [48, 44, 44, 44, 48, 44, 44, 44]

# (pose, ms, travel). The bite: a flinch back (the tell), the strike with the jaw wide, the snap, a hold, the recovery. The body stays level and on its feet.
BITE = [
    (dict(bx=0, fu=4, hu=4), 80, 0),
    (dict(bx=-2, pitch=-0.04, fu=16, fl=-6, hu=18, hl=-14, jaw=0.15), 80, 0),          # weight rocks back, the head lifts, the lips part
    (dict(bx=-3, pitch=-0.06, fu=22, fl=-8, hu=24, hl=-20, jaw=0.45), 90, 0),          # coiled: the tell
    (dict(bx=2, pitch=0.02, fu=-6, fl=4, hu=-14, hl=6, jaw=0.75), 45, 0),              # the strike: everything goes forward, the jaw at full gape
    (dict(bx=6, pitch=0.06, fu=-24, fl=6, hu=-30, hl=10, jaw=0.80), 45, 0),
    (dict(bx=7, pitch=0.07, fu=-28, fl=6, hu=-34, hl=10, jaw=0.0, flash=True), 70, 0), # the snap
    (dict(bx=7, pitch=0.06, fu=-28, fl=6, hu=-34, hl=10, jaw=0.08), 90, 0),            # it holds on and wrenches
    (dict(bx=6, pitch=0.03, fu=-24, fl=4, hu=-30, hl=8, jaw=0.0), 90, 0),
    (dict(bx=3, pitch=0.0, fu=-10, hu=-14, jaw=0.2), 80, 0),                           # lets go, steps back
    (dict(bx=0, fu=4, hu=4), 80, 0),
]
POUNCE = [
    (dict(bx=-1, pitch=0.04, fu=18, fl=-30, hu=30, hl=-44), 110, 0.0),                 # sinks: chest down, haunches loaded
    (dict(bx=-3, pitch=0.07, fu=24, fl=-40, hu=36, hl=-52, jaw=0.15), 120, 0.0),       # the tell: a long crouch
    (dict(bx=1, pitch=-0.07, fu=20, fl=-20, hu=-20, hl=6, air=2, jaw=0.3), 55, 0.10),  # the hind legs fire: nose up, off the ground
    (dict(bx=2, pitch=-0.05, fu=36, fl=-10, hu=-38, hl=10, air=9, jaw=0.5), 60, 0.34), # stretched out, rising
    (dict(bx=2, pitch=0.00, fu=40, fl=0, hu=-40, hl=10, air=13, jaw=0.7), 70, 0.62),   # the top of the leap
    (dict(bx=2, pitch=0.05, fu=38, fl=8, hu=-30, hl=-10, air=8, jaw=0.8), 60, 0.86),   # coming down, fore feet reaching, the jaw wide
    (dict(bx=3, pitch=0.07, fu=24, fl=6, hu=-10, hl=-30, air=1, jaw=0.8), 50, 1.0),    # the fore feet hit
    (dict(bx=5, pitch=0.07, fu=-10, fl=6, hu=-26, hl=6, jaw=0.0, flash=True), 80, 1.0),# the snap as its weight comes over the fore legs
    (dict(bx=5, pitch=0.05, fu=-14, fl=6, hu=-30, hl=8, jaw=0.08), 100, 1.0),
    (dict(bx=4, pitch=0.03, fu=-12, fl=4, hu=-24, hl=6, jaw=0.0), 100, 1.0),
    (dict(bx=2, fu=-6, hu=-10, jaw=0.2), 80, 1.0),
    (dict(bx=0, fu=4, hu=4), 80, 1.0),
]

def check_neutral(dog):
    f = dog.frame({}); ref = m.outline(dog.rig.base.copy()); a, b = f.load(), ref.load(); diff = 0
    for y in range(dog.rig.H0):
        for x in range(dog.rig.W0):
            p, q = a[x + MX, y + MY], b[x, y]
            if p != q and (p[3] or q[3]): diff += 1
    return diff

def build(key):
    dog = Dog(key)
    return dog, {"run": ([dog.frame(p) for p in RUN], RUN_MS, [0] * len(RUN)),
                 "bite": ([dog.frame(p) for p, _, _ in BITE], [t for _, t, _ in BITE], [0] * len(BITE)),
                 "pounce": ([dog.frame(p) for p, _, _ in POUNCE], [t for _, t, _ in POUNCE], [tr for _, _, tr in POUNCE])}

def gif(path, dog, res, sc=4):
    """run | bite over pounce, on a ground line, the old review's layout without its effects (the motion has to carry itself)."""
    W, H = dog.W, dog.H; gy = dog.floor + 2; frames = []; durs = []
    PW = W + 130
    def at(ms, t, loop):
        tot = sum(ms); t = t % tot if loop else t % (tot + 500); acc = 0
        for i, d_ in enumerate(ms):
            if t < acc + d_: return i
            acc += d_
        return len(ms) - 1
    prev = None
    for tm in range(0, 5200, 10):
        ir, ib, ip = at(res["run"][1], tm, True), at(res["bite"][1], tm, False), at(res["pounce"][1], tm, False)
        if (ir, ib, ip) == prev: durs[-1] += 10; continue
        prev = (ir, ib, ip)
        full = Image.new("RGBA", (W * 2 + 8, H * 2 + 8), (42, 38, 32, 255)); full = Image.new("RGBA", (max(W * 2 + 8, PW), H * 2 + 8), (42, 38, 32, 255)); d = ImageDraw.Draw(full)
        for oy in (0, H + 8): d.rectangle([0, oy + gy, full.width, oy + H], fill=(54, 49, 41, 255))
        off = int(tm * 0.30) % 24
        for gx in range(-24, W, 24): d.rectangle([gx - off + 24, gy + 4, gx - off + 30, gy + 5], fill=(70, 64, 54, 255))
        full.alpha_composite(res["run"][0][ir], (0, 0)); full.alpha_composite(res["bite"][0][ib], (W + 8, 0))
        full.alpha_composite(res["pounce"][0][ip], (int((PW - W) * res["pounce"][2][ip]), H + 8))
        frames.append(full.resize((full.width * sc, full.height * sc), Image.NEAREST).convert("P", palette=Image.ADAPTIVE)); durs.append(10)
    frames[0].save(path, save_all=True, append_images=frames[1:], duration=durs, loop=0)

def save(out, only=None):
    os.makedirs(out, exist_ok=True); meta = {}
    for key in C.SPECS:
        if only and key not in only: continue
        dog, res = build(key); meta[key] = {"ground": dog.H - 1 - dog.floor}
        for name, (fr, ms, tv) in res.items():
            w, h = fr[0].size; strip = Image.new("RGBA", (w * len(fr), h), (0, 0, 0, 0))
            for i, f in enumerate(fr): strip.alpha_composite(f, (i * w, 0))
            strip.save(os.path.join(out, f"{key}_{name}_strip.png")); meta[key][name] = dict(frame=[w, h], ms=ms, travel=tv)
        gif(os.path.join(out, f"{key}_v3.gif"), dog, res); print(key, "neutral diff px:", check_neutral(dog))
    json.dump(meta, open(os.path.join(out, "canine_anims_v3.json"), "w"), indent=1)

if __name__ == "__main__":
    save(sys.argv[1] if len(sys.argv) > 1 else "_out/canine_v3", set(sys.argv[2].split(",")) if len(sys.argv) > 2 else None)
