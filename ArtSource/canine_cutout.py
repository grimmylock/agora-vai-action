# Cut-out rig for the canine mutants (2026-11-09, second try). Yan rejected the first one ("all of them distort the original and look terrible"): it stretched and bent the
# body and rebuilt the jaw. Rules of this one:
#   * NO resampling. Every pixel in every frame is a pixel of Yan's sprite, moved by whole pixels. The neutral pose reproduces the original sprite EXACTLY (checked: see check_neutral).
#   * The sprite is cut into rigid pieces that overlap by a few pixels (hindquarters, forequarters, head, lower jaw, four legs split at the knee) and each frame only
#     moves/turns the pieces (legs and jaw turn by small angles with nearest-neighbour; body pieces only slide, a few pixels at most).
#   * The jaw is the sprite's own lower jaw (everything under the lip line); it hinges, the gap behind it shows a dark mouth with teeth on the upper lip.
import math, os
from PIL import Image, ImageDraw
import make_mutants_hd as hd
import mutant_from_ref as m
import make_mutant_frames as MF

TEETH = (240, 234, 214, 255); TEETH_D = (200, 190, 170, 255)

class Rig:
    def __init__(self, key, cfg):
        self.key, self.c = key, cfg
        self.base = [it[2] for it in hd.sprites(outlined=False) if it[1] == key][0].copy()
        self.W0, self.H0 = self.base.size; px = self.base.load(); c = cfg
        self.W, self.H, self.L, self.T = self.W0 + 28, self.H0 + 20, 12, 14
        hx, hy = c["hinge"]; tx, ty = c["tip"]
        def lip(x): return hy + (ty - hy) * (x - hx) / max(1, tx - hx)
        self.lip = lip
        def is_jaw(x, y): return x >= hx and y > lip(x) + 0.5 and y <= c["jaw_y1"]
        def leg_of(x, y):
            if y < c["belly"]: return None
            for (xa, xb), name in c["legs"]:
                if xa <= x <= xb: return name
            return None
        parts = {n: Image.new("RGBA", self.base.size, (0, 0, 0, 0)) for n in ("hind", "fore", "head", "jaw")}
        legu = {}; legl = {}
        for (xa, xb), name in c["legs"]:
            legu[name] = Image.new("RGBA", self.base.size, (0, 0, 0, 0)); legl[name] = Image.new("RGBA", self.base.size, (0, 0, 0, 0))
        ov = c["overlap"]; knees = {}
        for (xa, xb), name in c["legs"]:
            ys = [y for x in range(xa, min(xb, self.W0 - 1) + 1) for y in range(c["belly"], self.H0) if px[x, y][3] and not (c.get("keep") and self.kept(x, y))]
            bot = max(ys) if ys else c["belly"] + 6
            knees[name] = c.get("knees", {}).get(name) or (int(round((xa + xb) / 2)), int(round(c["belly"] + (bot - c["belly"]) * c.get("knee", 0.45))))
        for y in range(self.H0):
            for x in range(self.W0):
                p = px[x, y]
                if not p[3]: continue
                if is_jaw(x, y): parts["jaw"].putpixel((x, y), p); continue
                ln = leg_of(x, y)
                if ln and not self.kept(x, y):
                    (legu if y <= knees[ln][1] else legl)[ln].putpixel((x, y), p); continue
                if x >= c["head_x"]: parts["head"].putpixel((x, y), p)
                if c["split"] - ov <= x < c["head_x"] + ov: parts["fore"].putpixel((x, y), p)
                if x <= c["split"] + ov: parts["hind"].putpixel((x, y), p)
        self.parts = parts
        self.legs = {name: (legu[name], legl[name], c.get("hips", {}).get(name) or (knees[name][0], c["belly"]), knees[name], "far" in name) for (_, name) in [(r, n) for r, n in c["legs"]]}

    def kept(self, x, y):
        r = self.c.get("keep"); return bool(r) and r[0] <= x <= r[2] and r[1] <= y <= r[3]

    @staticmethod
    def put(f, img, dx, dy, rot=0.0, pivot=None):
        if rot: img = img.rotate(rot, resample=Image.NEAREST, center=pivot)
        f.alpha_composite(img, (dx, dy))

    def frame(self, p):
        """p: hd/hy hindquarters offset, fd/fy forequarters, nx/ny head offset, nrot head turn (deg, + = nose up), jaw deg (+ = opening), rise, legs fu/fl hu/hl (near), lag, flash"""
        c = self.c; f = Image.new("RGBA", (self.W, self.H), (0, 0, 0, 0)); rise = p.get("rise", 0); L, T = self.L, self.T
        hdx, hdy = p.get("hdx", 0), p.get("hdy", 0); fdx, fdy = p.get("fdx", 0), p.get("fdy", 0)
        ndx, ndy = fdx + p.get("nx", 0), fdy + p.get("ny", 0); nrot = p.get("nrot", 0)
        amp = c.get("amp", 1.0)
        def leg(name):
            up, lo, hip, knee, far = self.legs[name]; hind = "hind" in name
            u, l = (p.get("hu", 0), p.get("hl", 0)) if hind else (p.get("fu", 0), p.get("fl", 0))
            if far: u = u * 0.92 + (5 if hind else -5) * p.get("lag", 0)
            ox, oy = (hdx, hdy) if hind else (fdx, fdy)
            MF.pose_leg(f, (up, lo, hip, knee, far), u * amp, l * amp, L + ox, T + oy + rise, 0)
        for n in c["order_far"]: leg(n)
        self.put(f, self.parts["hind"], L + hdx, T + hdy + rise)
        self.put(f, self.parts["fore"], L + fdx, T + fdy + rise)
        hp = c["hinge"]; piv = (c["head_x"], hp[1] - 2)
        self.put(f, self.parts["head"], L + ndx, T + ndy + rise, nrot, piv) if not nrot else f.alpha_composite(self.parts["head"].rotate(nrot, resample=Image.NEAREST, center=piv), (L + ndx, T + ndy + rise))
        deg = p.get("jaw", 0)
        self.mouth(f, L + ndx, T + ndy + rise, nrot, piv, deg, p.get("flash", False))
        for n in c["order_near"]: leg(n)
        return m.outline(f)

    def head_palette(self):
        c = self.c; x0, y0, x1, y1 = c["head_box"]; px = self.base.load()
        cols = sorted((px[x, y] for x in range(x0, min(x1, self.W0)) for y in range(y0, min(y1, self.H0)) if px[x, y][3] and max(px[x, y][:3]) - min(px[x, y][:3]) < 70),
                      key=lambda q: 0.3 * q[0] + 0.59 * q[1] + 0.11 * q[2])
        return cols[int(len(cols) * 0.9)], cols[len(cols) // 2], cols[int(len(cols) * 0.2)], cols[max(0, int(len(cols) * 0.02))]

    def mouth(self, f, ox, oy, nrot, piv, deg, flash):
        """closed: the sprite's own jaw. open: the jaw is redrawn (its colours are the head's own), hinged at the back of the mouth, with a dark mouth inside, teeth and a tongue"""
        c = self.c; hx, hy = c["hinge"]; tx, ty = c["tip"]
        def tr(x, y):                                                   # a point of the sprite after the head turn
            if not nrot: return (x, y)
            a = math.radians(nrot); dx, dy = x - piv[0], y - piv[1]
            return (piv[0] + dx * math.cos(a) + dy * math.sin(a), piv[1] - dx * math.sin(a) + dy * math.cos(a))
        hx2, hy2 = tr(hx, hy); tx2, ty2 = tr(tx, ty)
        if deg < 6:
            jaw = self.parts["jaw"]
            if nrot: jaw = jaw.rotate(nrot, resample=Image.NEAREST, center=piv)
            f.alpha_composite(jaw, (ox, oy))
        else:
            light, mid, dark, deep = self.head_palette(); th = c.get("jaw_th", 4)
            ang0 = math.atan2(ty2 - hy2, tx2 - hx2); lip = math.hypot(tx2 - hx2, ty2 - hy2); Lj = lip + 1.5
            ang = ang0 + math.radians(deg); ax, ay = math.cos(ang), math.sin(ang); nx, ny = -ay, ax
            H = (ox + hx2 + 0.5, oy + hy2 + 0.5); Tp = (ox + tx2 + 1.0, oy + ty2 + 0.5)
            lay = Image.new("RGBA", f.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
            d.polygon([H, Tp, (H[0] + ax * Lj, H[1] + ay * Lj)], fill=c["inside"]); f.alpha_composite(lay)
            jaw = Image.new("RGBA", f.size, (0, 0, 0, 0)); jp = jaw.load()
            for yy in range(int(H[1] - Lj - 4), int(H[1] + Lj + th + 6)):
                for xx in range(int(H[0] - 3), int(H[0] + Lj + 5)):
                    if not (0 <= xx < f.width and 0 <= yy < f.height): continue
                    rx, ry = xx + 0.5 - H[0], yy + 0.5 - H[1]; tt = rx * ax + ry * ay; dn = rx * nx + ry * ny
                    if tt < -0.5 or tt > Lj: continue
                    thick = th * (1.0 - 0.5 * max(0.0, tt) / Lj)
                    if 0 <= dn <= thick: jp[xx, yy] = light if dn < 0.9 else (mid if dn < thick - 1.0 else dark)
            f.alpha_composite(jaw)
            fp = f.load(); n_t = c.get("teeth", 3)
            def plot(x, y, col):
                x, y = int(x), int(y)
                if 0 <= x < f.width and 0 <= y < f.height: fp[x, y] = col
            for k in range(n_t):                                          # upper fangs: hang from the lip, 2 px (3 when it gapes wide)
                tt = 0.5 + 0.45 * k / max(1, n_t - 1)
                x = ox + hx2 + (tx2 - hx2) * tt; y = oy + hy2 + (ty2 - hy2) * tt + 1
                plot(x, y, TEETH); plot(x, y + 1, TEETH)
                if deg > 34 and k % 2 == 0: plot(x, y + 2, TEETH_D)
            for k in range(n_t - 1):                                      # lower fangs: stand up from the jaw's top edge
                tt = 0.55 + 0.4 * k / max(1, n_t - 2)
                x = H[0] + ax * Lj * tt - nx * 0.8; y = H[1] + ay * Lj * tt - ny * 0.8
                plot(x, y, TEETH); plot(x - nx * 1.0, y - ny * 1.0, TEETH_D if deg < 30 else TEETH)
            if deg > 28:                                                  # a tongue lying along the jaw
                for tt in (0.2, 0.32, 0.44, 0.56):
                    plot(H[0] + ax * Lj * tt + nx * 0.5, H[1] + ay * Lj * tt + ny * 0.5, c.get("tongue", (190, 84, 90, 255)))
            if c.get("drool"):                                            # drool hangs from the jaw's tip
                gx, gy = H[0] + ax * Lj * 0.88 + nx * th, H[1] + ay * Lj * 0.88 + ny * th
                for k in range(3): plot(gx, gy + k, c["drool"])
        if flash:
            sx, sy = int(ox + tx2 - 2), int(oy + ty2 + 2)
            for dx, dy2 in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1), (2, 0), (-2, 0), (0, 2), (0, -2)):
                if 0 <= sx + dx < f.width and 0 <= sy + dy2 < f.height: f.putpixel((sx + dx, sy + dy2), (255, 250, 232, 255))


def check_neutral(rig):
    """the neutral pose must be the original sprite, pixel for pixel (inside the frame's offset)"""
    f = rig.frame({}); ref = m.outline(rig.base.copy()); L, T = rig.L, rig.T
    crop = f.crop((L, T, L + rig.W0, T + rig.H0)); diff = 0
    a, b = crop.load(), ref.load()
    for y in range(rig.H0):
        for x in range(rig.W0):
            if a[x, y] != b[x, y] and (a[x, y][3] or b[x, y][3]): diff += 1
    return diff


SPECS = {
    "pallid_prowler": dict(belly=22, split=24, head_x=37, overlap=4, hinge=(40, 20), tip=(47, 20), jaw_y1=26, inside=(66, 20, 26, 255), teeth=3, head_box=(34, 12, 48, 21), jaw_th=3, drool=(137, 226, 56, 255),
                           hips={"near_hind": (11, 22), "far_hind": (17, 22), "near_fore": (26, 22), "far_fore": (33, 22)}, knees={"near_hind": (6, 25), "far_hind": (15, 26), "near_fore": (27, 26), "far_fore": (35, 26)},
                           legs=[((0, 13), "near_hind"), ((14, 21), "far_hind"), ((22, 31), "near_fore"), ((32, 43), "far_fore")]),
    "veil_prowler": dict(belly=20, split=24, head_x=36, overlap=4, hinge=(38, 17), tip=(47, 16), jaw_y1=21, inside=(40, 8, 14, 255), teeth=3, head_box=(34, 8, 47, 18), jaw_th=4,
                         legs=[((0, 10), "near_hind"), ((11, 20), "far_hind"), ((21, 31), "near_fore"), ((32, 46), "far_fore")]),
    "dread_prowler": dict(belly=35, split=34, head_x=48, overlap=5, hinge=(50, 28), tip=(63, 27), jaw_y1=34, inside=(44, 10, 12, 255), teeth=4, amp=0.9, head_box=(48, 20, 64, 29), jaw_th=6,
                          legs=[((0, 12), "near_hind"), ((12, 24), "far_hind"), ((34, 50), "near_fore"), ((50, 63), "far_fore")]),
    "blind_maw": dict(belly=26, split=26, head_x=32, overlap=4, hinge=(34, 15), tip=(47, 14), jaw_y1=25, inside=(36, 6, 10, 255), teeth=4, amp=0.55, head_box=(32, 6, 48, 15), jaw_th=7,
                      legs=[((2, 10), "near_hind"), ((11, 22), "far_hind"), ((23, 31), "near_fore"), ((32, 46), "far_fore")]),
}
for _c in SPECS.values(): _c.setdefault("order_far", ["far_hind", "far_fore"]); _c.setdefault("order_near", ["near_hind", "near_fore"])

def debug_sheet(path):
    cols = {"hind": (200, 80, 80), "fore": (80, 160, 220), "head": (230, 200, 70), "jaw": (230, 90, 220)}
    rows = []
    for key, cfg in SPECS.items():
        r = Rig(key, cfg); im = Image.new("RGBA", (r.W0, r.H0), (30, 28, 24, 255))
        for n, c in cols.items():
            pm = r.parts[n].copy(); px = pm.load()
            for y in range(r.H0):
                for x in range(r.W0):
                    if px[x, y][3]: px[x, y] = tuple(int(0.4 * a + 0.6 * b) for a, b in zip(px[x, y][:3], c)) + (255,)
            im.alpha_composite(pm)
        for name, (up, lo, hip, knee, far) in r.legs.items():
            for part, cc in ((up, (90, 220, 120)), (lo, (240, 140, 40))):
                pm = part.copy(); px = pm.load()
                for y in range(r.H0):
                    for x in range(r.W0):
                        if px[x, y][3]: px[x, y] = cc + (255,)
                im.alpha_composite(pm)
        orig = m.outline(r.base.copy()); both = Image.new("RGBA", (r.W0 * 2 + 4, r.H0), (30, 28, 24, 255)); both.alpha_composite(orig, (0, 0)); both.alpha_composite(im, (r.W0 + 4, 0))
        rows.append(both.resize((both.width * 8, both.height * 8), Image.NEAREST)); print(key, "neutral diff px:", check_neutral(r))
    H = sum(r.height for r in rows); W = max(r.width for r in rows); out = Image.new("RGBA", (W, H), (20, 18, 16, 255)); y = 0
    for r in rows: out.alpha_composite(r, (0, y)); y += r.height
    out.save(path)

if __name__ == "__main__":
    import sys; debug_sheet(sys.argv[1])
