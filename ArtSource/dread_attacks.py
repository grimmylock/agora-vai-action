# Dread Prowler BITE, ALTERNATIVE BITE and POUNCE on the new sprite (2026-11-12), body and head only (no jaw animation, no impact flash).
# Same method as pallid_attacks.py with the Dread's own pieces: the body is one rigid piece (offset, rise, a whole-pixel column-shear tilt), the head (x >= 44) moves on its own by whole pixels with a nod shear,
# the legs are HIS leg pixels (rows sheared so the hip follows the body and the foot stays planted; rows deleted or repeated for a dip or a rise). The pounce's flight frames are the ORIGINAL leaping sprite
# (Yan: "he needs to be in a similar position mid pounce to the original Dread prowler sprite"), his own drawing, lifted, tilted and moved forward by whole pixels.    python3 dread_attacks.py <out_dir>
import math, os, sys, json
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as nd
import pallid_attacks as PA
import veil_attacks as VA

HERE = os.path.dirname(os.path.abspath(__file__))
NEW = Image.open(os.path.join(HERE, "mutants_anim", "dread_new_clean.png")).convert("RGBA")           # his new standing sprite, 63x37
LEAP = Image.open(os.path.join(HERE, "mutants_anim", "stand_originals", "mutant_hd_dread_prowler.png")).convert("RGBA")   # his original leaping sprite, 64x48
S = np.array(NEW); ox, oy = 16, 18; W, H = NEW.width + 2 * ox, NEW.height + 2 * oy; LOW = NEW.height - 1; GROUND = oy + LOW; HX = 44
# the leg pieces: (x range, first row) in his stand sprite; hind thighs and everything above stay in the body
PARTS = {"near_hind": ((0, 8), 29), "far_hind": ((8, 18), 31), "near_fore": ((24, 44), 25), "far_fore": ((44, 63), 25)}
PIECE = {}; _BODY = S.copy()
for n, ((x0, x1), top) in PARTS.items():
    a = np.zeros_like(S); a[top:, x0:x1] = S[top:, x0:x1]; PIECE[n] = a; _BODY[top:, x0:x1] = 0
# the jaw underside / chin pixels hanging below row 25 at the head stay with the head (x >= 47 and rows 25..28 are body, not leg): move them back
for y in range(25, 30):
    for x in range(47, 63):
        if S[y, x, 3] and PIECE["far_fore"][y, x, 3] and y <= 27: _BODY[y, x] = S[y, x]; PIECE["far_fore"][y, x] = 0
FAR = ("far_hind", "far_fore"); NEAR = ("near_hind", "near_fore")
# make the Pallid module's helpers (head thrust, nod, tilt) work on this sprite
PA.S, PA.BODY, PA.ox, PA.oy, PA.W, PA.H, PA.HX, PA.STAND = S, _BODY, ox, oy, W, H, HX, NEW

def planted_leg(name, bx, by, rise, tilt=0):
    """his leg pixels, the hip following the body: sideways by bx (the foot stays put as far as the leg can lean: a short shin cannot lean more than about 0.8 px per row, past that the foot slides along
    with the body instead of smearing), up or down by the body's dip/rise plus the tilt at the leg's own x (rows are repeated when the hip is higher, deleted when it is lower)"""
    p = PIECE[name]; top = PARTS[name][1]; x0, x1 = PARTS[name][0]; hx = (x0 + min(x1, 40)) / 2.0 + ox + bx
    dy = by - rise + int(round(tilt * (hx - (ox + 24)) / 24.0)); down, up_ = max(0, dy), max(0, -dy)
    rows = list(range(top, LOW + 1)); n = len(rows); cut = set(rows[n // 2: n // 2 + down]); keep = [top] * up_ + [y for y in rows if y not in cut]
    lim = 0.8 * max(1, len(keep) - 1); lean = max(-lim, min(lim, bx))
    out = np.zeros((H, W, 4), np.uint8)
    for k, y in enumerate(keep):
        yy = top - up_ + k + down; u = k / max(1, len(keep) - 1); sh = int(round(bx - lean * u))
        for x in range(S.shape[1]):
            if p[y, x, 3]:
                nx = x + sh + ox
                if 0 <= nx < W and 0 <= yy + oy < H: out[yy + oy, nx] = p[y, x]
    return Image.fromarray(out)

def leap_layer(p):
    """the original leaping sprite as one flight frame: bottom row at the ground minus `rise`, `bx` forward, the whole-pixel tilt (negative: nose up)"""
    a = np.zeros((H, W, 4), np.uint8); L = np.array(LEAP); lh, lw = L.shape[:2]; rise = p.get("rise", 0); bx = p.get("bx", 0); tilt = p.get("tilt", 0)
    y0 = GROUND + 1 - lh - rise; x0 = ox + bx - 1
    for y in range(lh):
        for x in range(lw):
            if L[y, x, 3]:
                sh = int(round(tilt * (x - lw / 2.0) / (lw / 2.0))); yy, xx = y0 + y + sh, x0 + x
                if 0 <= yy < H and 0 <= xx < W: a[yy, xx] = L[y, x]
    return Image.fromarray(a)

def render(p):
    if p.get("leap"):
        im = leap_layer(p)
    else:
        bx, by, rise, tilt = p.get("bx", 0), p.get("by", 0), p.get("rise", 0), p.get("tilt", 0)
        up = Image.fromarray(PA.upper(p)); body = Image.new("RGBA", (W, H), (0, 0, 0, 0)); body.alpha_composite(up, (bx, by - rise))
        im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        for n in FAR: im.alpha_composite(planted_leg(n, bx, by, rise, tilt))
        im.alpha_composite(body)
        for n in NEAR: im.alpha_composite(planted_leg(n, bx, by, rise, tilt))
    a = np.array(im); l, k = nd.label(a[..., 3] > 0, structure=np.ones((3, 3)))
    for j in range(1, k + 1):
        if (l == j).sum() < 6: a[l == j] = 0
    return Image.fromarray(a)

BITE = [(q, t, 0) for q, t, _ in VA.BITE]
BITE_B = [   # a rear-up bite: the body lifts and lunges over the target, then slams down (the legs only lengthen and shorten)
    (dict(), 100, 0),
    (dict(bx=-2, by=2, ny=1, nrot=4), 110, 0),
    (dict(bx=-3, by=3, nrot=6, ny=1), 100, 0),
    (dict(bx=0, by=0, rise=2, tilt=-3, nx=1), 60, 0),
    (dict(bx=3, rise=3, tilt=-3, nx=2, ny=-1, nrot=6), 70, 0),
    (dict(bx=6, by=2, rise=0, tilt=1, nx=3, ny=2, nrot=-8), 45, 0),
    (dict(bx=6, by=4, nx=3, ny=2, nrot=-8), 80, 0),
    (dict(bx=5, by=3, nx=2, ny=1, nrot=-4), 100, 0),
    (dict(bx=3, by=2, nx=1), 90, 0),
    (dict(bx=1, by=1), 90, 0),
    (dict(), 90, 0),
]
POUNCE = [
    (dict(), 110, 0.0),
    (dict(bx=-3, by=4, tilt=3, ny=2), 140, 0.0),
    (dict(bx=-4, by=6, tilt=4, ny=2, nrot=3), 120, 0.0),
    (dict(leap=1, bx=-2, rise=2, tilt=-1), 40, 0.04),
    (dict(leap=1, bx=3, rise=7, tilt=-2), 45, 0.22),
    (dict(leap=1, bx=6, rise=11, tilt=-2), 90, 0.5),
    (dict(leap=1, bx=9, rise=8, tilt=0), 55, 0.76),
    (dict(leap=1, bx=11, rise=3, tilt=1), 50, 0.93),
    (dict(bx=11, by=3, tilt=1, nx=3, ny=1, nrot=-4), 55, 1.0),
    (dict(bx=11, by=4, nx=3, ny=1, nrot=-4), 90, 1.0),
    (dict(bx=8, by=3, nx=2, ny=1), 110, 1.0),
    (dict(bx=3, by=1), 90, 1.0),
    (dict(), 90, 1.0),
]

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "mutants_anim/dread_attacks"; os.makedirs(out, exist_ok=True)
    VA.W, VA.H, VA.GROUND = W, H, GROUND; res = {}
    for name, poses in (("bite", BITE), ("bite2", BITE_B), ("pounce", POUNCE)):
        fr = [render(p) for p, _, _ in poses]; ms = [t for _, t, _ in poses]; tr = [x for _, _, x in poses]
        for i, f in enumerate(fr): f.save(os.path.join(out, f"{name}_{i}.png"))
        VA.strip_of(fr).save(os.path.join(out, f"dread_prowler_{name}_strip.png")); VA.gif(os.path.join(out, f"{name}.gif"), fr, ms)
        res[name] = {"frame": [W, H], "ms": ms, "travel": tr, "ground_row": GROUND}
        print(name, len(fr), "frames; components", [nd.label(np.array(f)[..., 3] > 0, structure=np.ones((3, 3)))[1] for f in fr], "lowest rows", [int(np.where(np.array(f)[..., 3] > 0)[0].max()) for f in fr], "ground", GROUND)
        sc = 6; per = 5; rows = (len(fr) + per - 1) // per; sheet = Image.new("RGBA", (W * per * sc, H * rows * sc), (105, 115, 90, 255))
        for k, f in enumerate(fr): sheet.alpha_composite(f.resize((W * sc, H * sc), Image.NEAREST), ((k % per) * W * sc, (k // per) * H * sc))
        sheet.convert("RGB").save(os.path.join(out, f"{name}_sheet.png"))
    json.dump({"dread_prowler": res}, open(os.path.join(out, "dread_attacks.json"), "w"), indent=1)
    st = np.array(render({}))[..., 3] > 0; ref = np.zeros((H, W), bool); ref[oy:oy + S.shape[0], ox:ox + S.shape[1]] = S[..., 3] > 0
    print("neutral pose vs his new stand sprite (alpha px differing):", int((st ^ ref).sum()))
