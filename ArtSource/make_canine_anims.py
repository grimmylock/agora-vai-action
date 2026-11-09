# Canine-like mutants: RUN, BITE (close) and POUNCE (leaps from a distance, bites on landing) - frame by frame (2026-11-09).
# Yan: "now it looks like it's running! next: we'll need an actual attack animation, 2 in this case. When it runs at you from a distance it'll have a
# 'pounce' attack, it jumps at you and bites. And when it's already close enough, it bites. For that we'll need to improve its mouth in the sprite and make it
# legible that it's doing those things. Apply the same logic to the other canine-like enemies."
#
# Method (same as the approved knee-rig walk, make_mutant_frames.py): the original sprite pixels stay. Legs are cut at the belly into an upper and a lower part
# and turned at hip and knee per frame (M.pose_leg); the body is bent as ONE piece (every column slides along a smooth curve, stretched about its middle, the head
# shoved forward: no seams). The MOUTH is new: the old lower jaw is cut away along a clean lip line and redrawn per frame as a hinged jaw with teeth, a dark mouth
# inside and a tongue, so a bite reads at a glance: shut / gaping / snapping.
#   python3 make_canine_anims.py <out_dir> [keys]   -> strips, JSON (timings, travel) and one review GIF per animal
import math, os, sys, json
from PIL import Image, ImageDraw
import make_mutants_hd as hd
import mutant_from_ref as m
import make_mutant_frames as M

TEETH = (238, 232, 212, 255); WHITE = (255, 250, 232, 255)
HERE = os.path.dirname(os.path.abspath(__file__))

# per animal: where the head hinges, where the snout's lip ends, the jaw's size, the legs' column ranges (rear to front) and how hard it gallops
SPECS = {
    "pallid_prowler": dict(hinge=(38, 22), tip=(47, 21), jaw=(10, 4), belly=22, head_box=(34, 12, 48, 27), amp=1.0,
                           legs=[((0, 13), "near_hind"), ((14, 21), "far_hind"), ((22, 31), "near_fore"), ((32, 43), "far_fore")], inside=(46, 16, 20, 255), tongue=(196, 92, 98, 255), drool=True, keep=(0, 22, 9, 28)),
    "veil_prowler": dict(hinge=(38, 17), tip=(47, 16), jaw=(10, 4), belly=20, head_box=(34, 8, 48, 22), amp=1.0,
                         legs=[((0, 10), "near_hind"), ((11, 20), "far_hind"), ((21, 31), "near_fore"), ((32, 46), "far_fore")], inside=(70, 14, 22, 255), tongue=(176, 70, 84, 255), drool=False),
    "dread_prowler": dict(hinge=(50, 28), tip=(63, 27), jaw=(14, 6), belly=35, head_box=(46, 18, 64, 36), amp=0.9,
                          legs=[((0, 12), "near_hind"), ((12, 24), "far_hind"), ((34, 50), "near_fore"), ((50, 63), "far_fore")], inside=(60, 12, 14, 255), tongue=(190, 80, 70, 255), drool=False),
    "blind_maw": dict(hinge=(34, 15), tip=(47, 14), jaw=(14, 7), belly=24, head_box=(32, 5, 48, 26), amp=0.55,
                      legs=[((2, 10), "near_hind"), ((11, 22), "far_hind"), ((23, 32), "near_fore"), ((33, 46), "far_fore")], inside=(44, 8, 12, 255), tongue=(214, 104, 112, 255), drool=False),
}


def lum(c): return 0.3 * c[0] + 0.59 * c[1] + 0.11 * c[2]


class Animal:
    def __init__(self, key):
        self.key = key; self.cfg = SPECS[key]; c = self.cfg
        base = [it[2] for it in hd.sprites(outlined=False) if it[1] == key][0].copy()
        self.W0, self.H0 = base.size; px = base.load()
        # the head's own colours (skin: not the eyes or other bright accents) for the new jaw
        x0, y0, x1, y1 = c["head_box"]
        cols = sorted((px[x, y] for x in range(x0, min(x1, self.W0)) for y in range(y0, min(y1, self.H0)) if px[x, y][3] and max(px[x, y][:3]) - min(px[x, y][:3]) < 60), key=lum)
        self.light, self.mid, self.dark = cols[int(len(cols) * 0.85)], cols[len(cols) // 2], cols[int(len(cols) * 0.15)]
        # cut the old lower jaw (and any drool) away along the lip line: everything under the line from the hinge to the snout's end
        hx, hy = c["hinge"]; tx, ty = c["tip"]
        for x in range(hx, self.W0):
            ly = hy + (ty - hy) * (x - hx) / max(1, tx - hx)
            for y in range(int(round(ly)) + 1, self.H0):
                if y < c["belly"] + 8 and px[x, y][3]: px[x, y] = (0, 0, 0, 0)
        # legs: the pixels under the belly in each leg's columns, split at the knee
        self.legs = {}; self.cx = (c["legs"][0][0][0] + c["legs"][-1][0][1]) / 2
        body = base.copy(); bp = body.load()
        for (xa, xb), name in c["legs"]:
            ys = [y for x in range(xa, min(xb, self.W0 - 1) + 1) for y in range(c["belly"], self.H0) if px[x, y][3]]
            bot = max(ys) if ys else c["belly"] + 6
            hipx = (xa + xb) / 2; knee = (int(round(hipx)), int(round(c["belly"] + (bot - c["belly"]) * 0.45)))
            up = Image.new("RGBA", base.size, (0, 0, 0, 0)); lo = Image.new("RGBA", base.size, (0, 0, 0, 0)); pu, pl = up.load(), lo.load()
            for y in range(c["belly"], self.H0):
                for x in range(xa, min(xb, self.W0 - 1) + 1):
                    if not px[x, y][3] or self.kept(x, y): continue
                    (pu if y <= knee[1] else pl)[x, y] = px[x, y]
            self.legs[name] = (up, lo, (int(round(hipx)), c["belly"]), knee, "far" in name)
        for (xa, xb), name in c["legs"]:
            for y in range(c["belly"], self.H0):
                for x in range(xa, min(xb, self.W0 - 1) + 1):
                    if not self.kept(x, y): bp[x, y] = (0, 0, 0, 0)
        self.body = body
        self.W, self.H, self.L, self.T = self.W0 + 24, self.H0 + 16, 10, 12

    def kept(self, x, y):                                               # pixels under the belly that stay with the body (the tail)
        r = self.cfg.get("keep")
        return bool(r) and r[0] <= x <= r[2] and r[1] <= y <= r[3]

    # ---- the body bent as one piece
    def bend(self, st, hs, dyf, dyb):
        c = self.cfg; hx = c["hinge"][0]; img = self.body
        sp = img.load(); out = Image.new("RGBA", (self.W, self.H), (0, 0, 0, 0)); op = out.load()
        xr0, xr1 = c["legs"][0][0][1] - 4, hx
        def sm(u): u = min(1.0, max(0.0, u)); return u * u * (3 - 2 * u)
        def ox(sx): return self.cx + (sx - self.cx) * st + hs * sm((sx - (hx - 16)) / 16.0)
        def dy(sx): return dyb + (dyf - dyb) * sm((sx - xr0) / max(1.0, xr1 - xr0))
        for sx in range(self.W0):
            a, b = int(round(ox(sx))), int(round(ox(sx + 1)))
            cols = range(a, max(a + 1, b))
            d = int(round(dy(sx)))
            for y in range(self.H0):
                p = sp[sx, y]
                if not p[3]: continue
                yy = y + d + self.T
                if not 0 <= yy < self.H: continue
                for xx in cols:
                    xx += self.L
                    if 0 <= xx < self.W: op[xx, yy] = p
        self._map = (ox, dy)
        return out

    def at(self, x, y, rise=0):                                         # where a point of the sprite is in the frame after the bend
        ox, dy = self._map
        return (ox(x) + self.L, y + dy(x) + self.T + rise)

    # ---- the jaw: hinged at the back of the mouth, teeth, a dark mouth, a tongue
    def mouth(self, f, deg, rise, flash=False, drool=False):
        c = self.cfg; hx, hy = self.at(*c["hinge"], rise); tx, ty = self.at(*c["tip"], rise)
        L0, th = c["jaw"]; ang0 = math.atan2(ty - hy, tx - hx); lip_len = math.hypot(tx - hx, ty - hy)
        deg = deg * 0.62                                                # the poses are written 0..90; a real jaw opens about 55 at most
        ang = ang0 + math.radians(deg)                                  # opens downward (the animal faces right, y is down)
        ax, ay = math.cos(ang), math.sin(ang); nx, ny = -ay, ax          # n points down the jaw (toward its underside)
        Lj = lip_len + 0.5                                              # the jaw ends where the snout does
        lay = Image.new("RGBA", f.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
        if deg > 4:                                                     # inside of the mouth: the wedge between the lip line and the jaw's top edge
            jtip = (hx + ax * Lj, hy + ay * Lj)
            d.polygon([(hx, hy), (tx, ty), jtip], fill=c["inside"])
        out = Image.new("RGBA", f.size, (0, 0, 0, 0)); op = out.load()
        for yy in range(int(hy - Lj - 6), int(hy + Lj + th + 6)):       # the jaw itself: tapered, light on top, dark underneath
            for xx in range(int(hx - 4), int(hx + Lj + 6)):
                if not (0 <= xx < f.width and 0 <= yy < f.height): continue
                rx, ry = xx + 0.5 - hx, yy + 0.5 - hy
                t = rx * ax + ry * ay; dn = rx * nx + ry * ny           # along the jaw, and across it (0 = the top edge, th = the underside)
                if t < -1.0 or t > Lj: continue
                thick = th * (1.0 - 0.45 * max(0.0, t) / Lj)
                if 0 <= dn <= thick: op[xx, yy] = self.light if dn < thick * 0.3 else self.mid if dn < thick * 0.7 else self.dark
        px = out.load()
        if deg > 10:                                                    # teeth: fangs up from the jaw, fangs down from the upper lip; a tongue between them when it gapes
            for tt in (0.5, 0.78, 0.97):
                for k in (1, 2):
                    xx, yy = int(hx + ax * Lj * tt - nx * k), int(hy + ay * Lj * tt - ny * k)
                    if k == 2 and deg < 40: continue
                    if 0 <= xx < f.width and 0 <= yy < f.height: px[xx, yy] = TEETH
            for tt in (0.55, 0.85):
                for k in (1, 2):
                    if k == 2 and deg < 30: continue
                    xx, yy = int(hx + (tx - hx) * tt), int(hy + (ty - hy) * tt + k)
                    if 0 <= xx < f.width and 0 <= yy < f.height: px[xx, yy] = TEETH
            if deg > 32:
                for tt in (0.25, 0.4, 0.55, 0.7):
                    xx, yy = int(hx + ax * Lj * tt + nx * 0.2), int(hy + ay * Lj * tt + ny * 0.2)
                    if 0 <= xx < f.width and 0 <= yy < f.height: px[xx, yy] = c["tongue"]
        else:                                                           # shut: a dark mouth line along the lips and the tips of the fangs showing under the lip
            ImageDraw.Draw(lay).line([(hx, hy), (tx, ty)], fill=c["inside"])
            for tt in (0.62, 0.92):
                xx, yy = int(hx + (tx - hx) * tt), int(hy + (ty - hy) * tt + 1)
                if 0 <= xx < f.width and 0 <= yy < f.height: px[xx, yy] = TEETH
        f.alpha_composite(lay); f.alpha_composite(out)
        if drool and c.get("drool"):                                    # the pallid prowler's acid drool hangs from the jaw
            jx, jy = int(hx + ax * Lj * 0.8 + nx * th), int(hy + ay * Lj * 0.8 + ny * th)
            for k, col in enumerate(((120, 220, 40, 255), (120, 220, 40, 255), (90, 170, 40, 255))):
                if 0 <= jx < f.width and 0 <= jy + k < f.height: f.putpixel((jx, jy + k), col)
        if flash:                                                       # the snap: a white star where the jaws meet
            sx, sy = int(tx - 3), int(ty + 1)
            for dx, dy2 in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1), (2, 0), (-2, 0), (0, 2), (0, -2), (2, 2), (-2, -2), (2, -2), (-2, 2)):
                if 0 <= sx + dx < f.width and 0 <= sy + dy2 < f.height: f.putpixel((sx + dx, sy + dy2), WHITE)

    def frame(self, p):
        """one frame from a pose dict: st stretch, hs head shove, dyf/dyb front and rear height, rise (whole body, up is negative), deg jaw opening, fu/fl fore upper angle/knee flex,
        hu/hl hind, lagk far-leg lag, flash, shx/shy shake"""
        amp = self.cfg["amp"]
        f = Image.new("RGBA", (self.W, self.H), (0, 0, 0, 0))
        b = self.bend(p.get("st", 1.0), p.get("hs", 0), p.get("dyf", 0), p.get("dyb", 0)); rise = p.get("rise", 0)
        ox, dy = self._map
        shx, shy = p.get("shx", 0), p.get("shy", 0)
        def place(name):
            up, lo, hip, knee, far = self.legs[name]
            hind = "hind" in name; u, l = (p.get("hu", 0), p.get("hl", 0)) if hind else (p.get("fu", 0), p.get("fl", 0))
            if far: u = u * 0.9 + (4 if hind else -4) * p.get("lagk", 0)
            hxp = self.L + int(round(ox(hip[0]) - hip[0])) + shx; hyp = self.T + int(round(dy(hip[0]))) + rise + shy
            M.pose_leg(f, (up, lo, hip, knee, far), u * amp, l * amp, hxp, hyp, 0)
        for n in ("far_hind", "far_fore"): place(n)
        if shx or shy or rise: b = shift(b, shx, rise + shy)
        f.alpha_composite(b)
        self.mouth(f, p.get("deg", 0), rise + shy, p.get("flash", False), True)
        for n in ("near_hind", "near_fore"): place(n)
        return m.outline(despeckle(f))


def shift(img, dx, dy):
    out = Image.new("RGBA", img.size, (0, 0, 0, 0)); out.alpha_composite(img, (dx, dy)); return out


def despeckle(img, min_px=10):
    """drop leftover bits: any clump of fewer than min_px pixels"""
    p = img.load(); Wd, Hd = img.size; seen = set()
    for y0 in range(Hd):
        for x0 in range(Wd):
            if (x0, y0) in seen or not p[x0, y0][3]: continue
            comp = [(x0, y0)]; seen.add((x0, y0)); k = 0
            while k < len(comp):
                x, y = comp[k]; k += 1
                for dx in (-1, 0, 1):
                    for dy in (-1, 0, 1):
                        q = (x + dx, y + dy)
                        if 0 <= q[0] < Wd and 0 <= q[1] < Hd and q not in seen and p[q][3]: seen.add(q); comp.append(q)
            if len(comp) < min_px:
                for q in comp: p[q] = (0, 0, 0, 0)
    return img


# ---------------------------------------------------------------- the animations
def run_poses(n=8):
    out = []
    for i in range(n):
        ph = 2 * math.pi * i / n; c = math.cos(ph); ch = math.cos(ph - 0.9); g = max(0.0, -c)
        fore = lambda cc: (46 * cc - 4, -48 * max(0.0, -cc) - 8 * max(0.0, cc)); hind = lambda cc: (-44 * cc + 8, -34 * max(0.0, -cc) + 10 * max(0.0, cc))
        fu, fl = fore(c); hu, hl = hind(ch)
        out.append(dict(st=1.0 + 0.09 * c, dyb=-int(round(1.8 * g)), dyf=int(round(0.8 * g)) - int(round(0.8 * max(0.0, c))), rise=-int(round(3 * (math.cos(2 * ph) + 1) / 2)) + 1,
                        fu=fu, fl=fl, hu=hu, hl=hl, lagk=0.5 * math.sin(ph), deg=0))
    return out, [42] * n


# (pose, ms, travel) - travel = how far across the whole pounce the animal has got (0..1)
BITE = [
    (dict(st=1.0, fu=6), 90, 0),
    (dict(st=0.94, hs=-2, dyf=1, dyb=-1, deg=20, fu=-10, fl=-20, hu=20, hl=-25), 70, 0),
    (dict(st=0.96, hs=-1, dyf=-1, deg=58, fu=0, fl=-10, hu=22, hl=-20), 70, 0),
    (dict(st=1.10, hs=4, dyf=1, rise=-1, deg=76, fu=40, fl=-10, hu=-35, hl=6), 45, 0),
    (dict(st=1.15, hs=6, dyf=2, rise=-1, deg=84, fu=48, fl=0, hu=-40, hl=8), 45, 0),
    (dict(st=1.12, hs=6, dyf=2, deg=0, fu=46, hu=-38, flash=True), 80, 0),
    (dict(st=1.10, hs=5, dyf=2, deg=3, fu=44, hu=-36, shy=1), 90, 0),
    (dict(st=1.10, hs=5, dyf=2, deg=5, fu=44, hu=-36, shy=-1, shx=1), 90, 0),
    (dict(st=1.0, hs=1, deg=22, fu=20, hu=-10), 80, 0),
    (dict(st=1.0, deg=6, fu=8), 80, 0),
]
POUNCE = [
    (dict(st=0.92, hs=-1, dyf=2, dyb=1, rise=1, fu=10, fl=-20, hu=25, hl=-30), 110, 0.0),
    (dict(st=0.86, hs=-2, dyf=3, dyb=2, rise=2, deg=14, fu=20, fl=-30, hu=30, hl=-40), 100, 0.0),
    (dict(st=1.12, hs=2, dyf=-1, rise=-2, deg=36, fu=40, hu=-50, hl=6), 55, 0.12),
    (dict(st=1.18, hs=3, dyf=-2, dyb=-1, rise=-6, deg=56, fu=48, hu=-52, hl=8), 55, 0.36),
    (dict(st=1.20, hs=4, dyf=-1, dyb=-1, rise=-9, deg=78, fu=50, hu=-50, hl=8), 65, 0.62),
    (dict(st=1.18, hs=5, dyf=2, dyb=-2, rise=-6, deg=86, fu=52, hu=-46, hl=8), 55, 0.86),
    (dict(st=1.14, hs=6, dyf=3, dyb=-1, rise=-2, deg=88, fu=50, hu=-44), 55, 1.0),
    (dict(st=1.12, hs=6, dyf=3, deg=0, fu=46, hu=-40, flash=True), 80, 1.0),
    (dict(st=1.10, hs=5, dyf=2, deg=3, fu=44, hu=-36, shy=1), 100, 1.0),
    (dict(st=1.10, hs=5, dyf=2, deg=5, fu=44, hu=-36, shy=-1, shx=1), 100, 1.0),
    (dict(st=1.0, hs=1, deg=22, fu=20, hu=-10), 80, 1.0),
    (dict(st=1.0, deg=6, fu=8), 80, 1.0),
]


def build(key):
    a = Animal(key); res = {}
    p, ms = run_poses(); res["run"] = ([a.frame(q) for q in p], ms, [0] * len(p))
    res["bite"] = ([a.frame(q) for q, _, _ in BITE], [t for _, t, _ in BITE], [0] * len(BITE))
    res["pounce"] = ([a.frame(q) for q, _, _ in POUNCE], [t for _, t, _ in POUNCE], [tr for _, _, tr in POUNCE])
    return a, res


def human():
    im = Image.open(os.path.join(HERE, "..", "Assets", "Resources", "Sprites", "armor_atlas.png")).convert("RGBA").crop((0, 0, 16, 16))
    return im.resize((32, 32), Image.NEAREST)


def idx_at(ms, t):
    total = sum(ms); t = t % (total + 400); acc = 0
    for i, d in enumerate(ms):
        if t < acc + d: return i
        acc += d
    return len(ms) - 1


def panel(res_item, kind, sc, hum, W, H, tm):
    fr_list, ms, trav = res_item
    idx = (int(tm // ms[0]) % len(fr_list)) if kind == "run" else idx_at(ms, tm)
    fr = fr_list[idx]; im = Image.new("RGBA", (W, H), (42, 38, 32, 255)); d = ImageDraw.Draw(im)
    gy = H - 14; d.rectangle([0, gy, W, H], fill=(54, 49, 41, 255)); fw, fh = fr.size
    if kind == "run":
        off = int(tm * 0.35) % 24
        for gx in range(-24, W, 24): d.rectangle([gx - off + 24, gy + 4, gx - off + 32, gy + 5], fill=(70, 64, 54, 255))
        x = (W - fw) // 2
    else:
        hx = W - 40
        if kind == "pounce": x = int(4 + (hx - fw + 14 - 4) * trav[idx])
        else: x = hx - fw + 14
        hf = hum.copy()
        if idx >= (7 if kind == "pounce" else 5) and idx <= (9 if kind == "pounce" else 7):
            tint = Image.new("RGBA", hf.size, (200, 40, 40, 255)); hf = Image.composite(tint, hf, hf.split()[3].point(lambda v: 110 if v else 0))
        im.alpha_composite(hf, (hx, gy - 30))
    sh = Image.new("RGBA", (W, H), (0, 0, 0, 0)); ImageDraw.Draw(sh).ellipse([x + 8, gy - 3, x + fw - 12, gy + 4], fill=(20, 18, 14, 130)); im.alpha_composite(sh)
    im.alpha_composite(fr, (x, gy - fh + 12))
    return im.resize((W * sc, H * sc), Image.NEAREST)


def review_gif(path, a, res, hum):
    sc = 4; fw, fh = a.W, a.H
    Wr, Wb, Wp, H = fw + 6, fw + 40, fw + 150, fh + 18
    frames = []; durs = []; prev = None
    for tm in range(0, 4800, 10):
        key = (int(tm // res["run"][1][0]) % len(res["run"][0]), idx_at(res["bite"][1], tm), idx_at(res["pounce"][1], tm))
        if key == prev: durs[-1] += 10; continue
        prev = key
        r0 = panel(res["run"], "run", sc, hum, Wr, H, tm); r1 = panel(res["bite"], "bite", sc, hum, Wb, H, tm); r2 = panel(res["pounce"], "pounce", sc, hum, Wp, H, tm)
        full = Image.new("RGBA", (max((Wr + Wb) * sc + 8, Wp * sc), H * sc * 2 + 8), (28, 26, 22, 255))
        full.alpha_composite(r0, (0, 0)); full.alpha_composite(r1, (Wr * sc + 8, 0)); full.alpha_composite(r2, (0, H * sc + 8))
        frames.append(full.convert("P", palette=Image.ADAPTIVE)); durs.append(10)
    frames[0].save(path, save_all=True, append_images=frames[1:], duration=durs, loop=0)


def save(out, only=None):
    os.makedirs(out, exist_ok=True); meta = {}; hum = human()
    for key in SPECS:
        if only and key not in only: continue
        a, res = build(key); meta[key] = {}
        for name, (fr, ms, tv) in res.items():
            w, h = fr[0].size; strip = Image.new("RGBA", (w * len(fr), h), (0, 0, 0, 0))
            for i, f in enumerate(fr): strip.alpha_composite(f, (i * w, 0))
            strip.save(os.path.join(out, f"{key}_{name}_strip.png")); meta[key][name] = dict(frame=[w, h], ms=ms, travel=tv)
        review_gif(os.path.join(out, f"{key}_run_bite_pounce.gif"), a, res, hum)
        sc = 5; w, h = a.W, a.H
        sheet = Image.new("RGBA", (w * sc * 6, h * sc * 4), (42, 38, 32, 255))
        for r, frs in enumerate([res["bite"][0], res["pounce"][0]]):
            for i, f in enumerate(frs): sheet.alpha_composite(f.resize((w * sc, h * sc), Image.NEAREST), ((i % 6) * w * sc, (r * 2 + i // 6) * h * sc))
        sheet.save(os.path.join(out, f"{key}_attacks_sheet.png")); print(key, "done")
    json.dump(meta, open(os.path.join(out, "canine_anims.json"), "w"), indent=1)


if __name__ == "__main__":
    save(sys.argv[1] if len(sys.argv) > 1 else "_out/canine", set(sys.argv[2].split(",")) if len(sys.argv) > 2 else None)
