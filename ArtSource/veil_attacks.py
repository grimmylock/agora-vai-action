# Veil Prowler BITE and POUNCE from the new art (2026-11-12). Yan: "we'll need them from the new sprites ... the biting attack doesn't need to move their legs as if they were walking, we need a motion
# from the full body like it was leaning forward to bite ... start with veil prowler" / "bite and pounce".
# Same pieces as the walk (veil_walk.py): the cut-out body (hindquarters + forequarters, always moved together as ONE piece), the head and the lower jaw (hinged, canine_cutout.Rig.mouth), and the legs
# drawn fresh by two-bone IK in the approved leg colours. For the bite the FEET STAY PLANTED on the stand pose's spots while the body leans, dips and lunges, so the legs only flex (they are not
# walking). For the pounce the feet leave the ground: hind legs push back, fore legs reach, everything folds at the apex and the fore feet land first.
#   python3 veil_attacks.py <out_dir>
import math, os, sys, json
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as nd
import veil_walk as V
import mutant_from_ref as m

rig = V.rig
V.oy = 16; V.H = rig.H0 + 2 * V.oy; V.GROUND = V.oy + 31; V.ox = 14; V.W = rig.W0 + 2 * V.ox          # more room above for the leap (veil_walk's globals are read at call time)
ox, oy, W, H, GROUND = V.ox, V.oy, V.W, V.H, V.GROUND
STAND_FEET = {"far_hind": (-1.5, 0.0), "near_hind": (1.0, 0.0), "far_fore": (1.0, 0.0), "near_fore": (-1.0, 0.0)}
FAR = ("far_hind", "far_fore"); NEAR = ("near_hind", "near_fore")

MOUTH_IN, MOUTH_BACK, TOOTH = (118, 22, 34, 255), (58, 8, 16, 255), (196, 192, 208, 255)       # inside of the mouth: the eye's red, darker toward the throat; teeth: the sprite's palest violet-white

def open_head(g, flash=False):
    """The head with the lower jaw dropped g pixels at its tip. Yan (frame 3 circled): only the lower jaw may move, 'anything outside of it should not move; separate the upper face and snout/upper jaw from
    the lower jaw first, then do the animation'. So the head part (face, snout, upper jaw) is drawn exactly as the sprite has it, and the sprite's own lower-jaw pixels (rig.parts['jaw']) are the only
    thing that moves: each column of the jaw drops by whole pixels, 0 at its back end (where it joins the head) growing to g at the tip, so it opens like a hinged jaw and stays attached. No fill, no
    rotation, no resampling. g = 0 is the sprite as drawn."""
    head = np.array(rig.parts["head"]).copy(); jaw = np.array(rig.parts["jaw"]).copy(); h0, w0 = head.shape[:2]; tx, ty = rig.c["tip"]
    out = np.zeros((h0 + 8, w0 + 4, 4), np.uint8); out[4:4 + h0, :w0] = head
    xs = np.where((jaw[..., 3] > 0).any(axis=0))[0]; x0, x1 = int(xs.min()), int(xs.max())
    for x in range(w0):
        f = min(1.0, max(0.0, (x - x0) / float(max(1, x1 - x0)))) if g > 0 else 0.0; dl = int(round(f * g))
        for y in range(h0):
            if jaw[y, x, 3]:
                if dl == 0 or not out[4 + y, x, 3] or True: out[4 + y + dl, x] = jaw[y, x]
    if g == 0:                                                   # closed: the jaw sits over the head exactly like the sprite
        pass
    if flash:
        sx, sy = tx - 2, ty + 2 + 4
        for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1), (2, 0), (-2, 0), (0, 2), (0, -2)):
            if 0 <= sx + dx < out.shape[1] and 0 <= sy + dy < out.shape[0]: out[sy + dy, sx + dx] = (255, 250, 232, 255)
    return Image.fromarray(out)

def tilt_shift(x, tilt):
    """whole-pixel vertical shift of the body at image column x for a tilt of `tilt` px across the body (negative: the nose end up)"""
    return int(round(tilt * (x - (ox + 24)) / 24.0))

def tilt_layer(img, tilt):
    if tilt == 0: return img
    a = np.array(img); src = a.copy(); a[:] = 0
    for x in range(a.shape[1]):
        sh = tilt_shift(x, tilt)
        for y in range(a.shape[0]):
            ny = y + sh
            if 0 <= ny < a.shape[0] and src[y, x, 3]: a[ny, x] = src[y, x]
    return Image.fromarray(a)

def render(p):
    """p: bx/by whole-body offset (right/down), rise (up), tilt (px the nose end is raised over the hips, negative = up), nx/ny head offset, nrot head turn (+ = nose up), jaw degrees (-> gape px), flash, feet: {leg: (fo, fl)} overrides"""
    bx, by, rise = p.get("bx", 0), p.get("by", 0), p.get("rise", 0); nx, ny, nrot, tilt = p.get("nx", 0), p.get("ny", 0), p.get("nrot", 0), p.get("tilt", 0)
    feet = dict(STAND_FEET); feet.update(p.get("feet", {})); g = 0; bob = by - rise      # the jaw does not animate (Yan: the view is not exactly a profile, so an opening jaw looked wrong); the poses' 'jaw' values are ignored, the head still moves (nx, ny, nrot)
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    def leg(n):
        hx_ = V.LEGS[n][0][0] + ox + bx; return V.leg_img(n, 0, bob + tilt_shift(hx_, tilt), False, bx, feet[n])
    for n in FAR: im.alpha_composite(leg(n))
    up = Image.new("RGBA", (W, H), (0, 0, 0, 0)); up.alpha_composite(rig.parts["hind"], (ox + bx, oy + bob)); up.alpha_composite(rig.parts["fore"], (ox + bx, oy + bob))
    hd = open_head(g, False)      # no impact flash (Yan: blood and damage effects belong to the target that is hit); piv = (rig.c["head_x"], rig.c["hinge"][1] - 2 + 4)
    if nrot: hd = hd.rotate(nrot, resample=Image.NEAREST, center=piv)
    up.alpha_composite(hd, (ox + bx + nx, oy + bob + ny - 4)); im.alpha_composite(tilt_layer(up, tilt))
    for n in NEAR: im.alpha_composite(leg(n))
    im = m.outline(im); a = np.array(im); l, k = nd.label(a[..., 3] > 0, structure=np.ones((3, 3)))
    for j in range(1, k + 1):
        if (l == j).sum() < 6: a[l == j] = 0
    return Image.fromarray(a)

# (pose, ms, travel 0..1 across the jump). The bite leans the whole body: back and down (the wind-up), then forward and down into the lunge, the jaw gapes, snaps (flash), holds, recovers.
BITE = [
    (dict(), 90, 0),
    (dict(bx=-1, by=1, ny=-1, nrot=3, jaw=10), 90, 0),
    (dict(bx=-3, by=1, ny=-1, nrot=7, jaw=26), 80, 0),
    (dict(bx=0, by=2, nx=1, jaw=42), 50, 0),
    (dict(bx=4, by=3, nx=2, ny=1, nrot=-4, jaw=52), 45, 0),
    (dict(bx=5, by=3, nx=2, ny=1, nrot=-4, jaw=0, flash=True), 80, 0),
    (dict(bx=4, by=3, nx=2, ny=1, nrot=-3, jaw=3), 90, 0),
    (dict(bx=2, by=2, nx=1, jaw=10), 80, 0),
    (dict(bx=1, by=1, jaw=6), 80, 0),
    (dict(), 90, 0),
]
BITE_B = [      # the earlier pounce, kept as a second bite (a short hop-and-bite) as Yan asked
    (dict(bx=-1, by=2, ny=1), 110, 0.0),
    (dict(bx=-2, by=4, ny=1, nrot=4, jaw=10), 100, 0.0),
    (dict(bx=0, by=1, rise=2, jaw=26, feet={"far_hind": (-5, 1), "near_hind": (-4, 1), "far_fore": (2, 2), "near_fore": (1, 2)}), 55, 0.12),
    (dict(bx=2, rise=6, nx=1, jaw=38, feet={"far_hind": (-6, 5), "near_hind": (-5, 4), "far_fore": (5, 5), "near_fore": (4, 5)}), 55, 0.36),
    (dict(bx=3, rise=9, nx=2, nrot=-3, jaw=48, feet={"far_hind": (-2, 8), "near_hind": (-1, 7), "far_fore": (5, 8), "near_fore": (4, 8)}), 65, 0.62),
    (dict(bx=3, rise=6, nx=2, nrot=-5, jaw=52, feet={"far_hind": (-1, 5), "near_hind": (0, 5), "far_fore": (5, 5), "near_fore": (4, 5)}), 55, 0.86),
    (dict(bx=4, by=1, rise=1, nx=2, ny=1, nrot=-4, jaw=52, feet={"far_hind": (-2, 2), "near_hind": (-1, 2), "far_fore": (3, 0), "near_fore": (3, 0)}), 55, 1.0),
    (dict(bx=5, by=3, nx=2, ny=1, nrot=-4, jaw=0, flash=True, feet={"far_fore": (3, 0), "near_fore": (3, 0)}), 80, 1.0),
    (dict(bx=4, by=3, nx=2, ny=1, nrot=-3, jaw=3), 100, 1.0),
    (dict(bx=3, by=2, nx=1, jaw=4), 100, 1.0),
    (dict(bx=1, by=1, jaw=14), 80, 1.0),
    (dict(), 80, 1.0),
]

# The new POUNCE leaps from a distance (Yan: "it doesn't seem like he leaped from a distance, he needs to be in a similar position mid pounce to the original Dread sprite"): a slow coil, an explosive
# launch, a flight held with the legs fully extended (fore forward and down, hind trailing back and down, the body flat and tilted nose-up, the head thrust out, the mouth open), then the fore feet
# reach for the ground, the landing compresses, the jaw snaps and the body slides on. Momentum: slow in, fast launch, a long flight, a skid.
POUNCE = [
    (dict(), 110, 0.0),
    (dict(bx=-3, by=4, tilt=3, ny=2, feet={"far_hind": (2, 0), "near_hind": (3, 0), "far_fore": (-2, 0), "near_fore": (-3, 0)}), 140, 0.0),
    (dict(bx=-4, by=6, tilt=4, ny=2, nrot=3, jaw=12, feet={"far_hind": (3, 0), "near_hind": (4, 0), "far_fore": (-1, 0), "near_fore": (-2, 0)}), 120, 0.0),
    (dict(bx=2, by=2, rise=1, tilt=-3, nx=1, jaw=24, feet={"far_hind": (-8, 0), "near_hind": (-9, 0), "far_fore": (5, 5), "near_fore": (4, 5)}), 40, 0.04),
    (dict(bx=5, rise=6, tilt=-4, nx=2, jaw=40, feet={"far_hind": (-13, 3), "near_hind": (-11, 2), "far_fore": (12, 9), "near_fore": (10, 8)}), 45, 0.22),
    (dict(bx=6, rise=10, tilt=-4, nx=3, nrot=-2, jaw=52, feet={"far_hind": (-16, 8), "near_hind": (-13, 6), "far_fore": (14, 11), "near_fore": (11, 9)}), 90, 0.5),
    (dict(bx=7, rise=8, tilt=-2, nx=3, nrot=-3, jaw=52, feet={"far_hind": (-13, 7), "near_hind": (-10, 5), "far_fore": (13, 9), "near_fore": (10, 6)}), 55, 0.76),
    (dict(bx=8, rise=3, tilt=0, nx=3, ny=1, nrot=-4, jaw=52, feet={"far_hind": (-9, 4), "near_hind": (-7, 3), "far_fore": (10, 3), "near_fore": (8, 2)}), 50, 0.92),
    (dict(bx=9, by=3, tilt=1, nx=3, ny=1, nrot=-4, jaw=48, feet={"far_hind": (-6, 2), "near_hind": (-5, 2), "far_fore": (5, 0), "near_fore": (4, 0)}), 55, 1.0),
    (dict(bx=9, by=4, nx=3, ny=1, nrot=-4, jaw=0, flash=True, feet={"far_hind": (-4, 1), "near_hind": (-3, 1), "far_fore": (4, 0), "near_fore": (4, 0)}), 70, 1.0),
    (dict(bx=7, by=3, nx=2, ny=1, jaw=6, feet={"far_hind": (-2, 0), "near_hind": (-1, 0)}), 110, 1.0),
    (dict(bx=3, by=1, jaw=0), 90, 1.0),
    (dict(), 90, 1.0),
]

def strip_of(frames):
    s = Image.new("RGBA", (W * len(frames), H), (0, 0, 0, 0))
    for i, f in enumerate(frames): s.alpha_composite(f, (i * W, 0))
    return s

def gif(path, frames, ms, sc=9):
    fs = []
    for f in frames:
        bg = Image.new("RGBA", (W, H), (54, 49, 41, 255)); d = ImageDraw.Draw(bg); d.rectangle([0, GROUND + 1, W, H], fill=(60, 55, 46, 255)); bg.alpha_composite(f)
        fs.append(bg.resize((W * sc, H * sc), Image.NEAREST).convert("RGB").quantize(colors=64, method=Image.MEDIANCUT, dither=Image.NONE))
    d = list(ms); d[-1] += 500
    fs[0].save(path, save_all=True, append_images=fs[1:], duration=d, loop=0)

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "mutants_anim/veil_attacks"; os.makedirs(out, exist_ok=True)
    res = {}
    for name, poses in (("bite", BITE), ("bite2", BITE_B), ("pounce", POUNCE)):
        fr = [render(p) for p, _, _ in poses]; ms = [t for _, t, _ in poses]; tr = [x for _, _, x in poses]
        for i, f in enumerate(fr): f.save(os.path.join(out, f"{name}_{i}.png"))
        strip_of(fr).save(os.path.join(out, f"veil_prowler_{name}_strip.png")); gif(os.path.join(out, f"{name}.gif"), fr, ms)
        res[name] = {"frame": [W, H], "ms": ms, "travel": tr, "ground_row": GROUND}
        comp = [nd.label(np.array(f)[..., 3] > 0, structure=np.ones((3, 3)))[1] for f in fr]
        low = [int(np.where(np.array(f)[..., 3] > 0)[0].max()) for f in fr]
        print(name, "frames", len(fr), "components", comp, "lowest rows", low, "ground", GROUND)
        sc = 8; per = 6; rows = (len(fr) + per - 1) // per; sheet = Image.new("RGBA", (W * per * sc, H * rows * sc), (105, 115, 90, 255))
        for k, f in enumerate(fr): sheet.alpha_composite(f.resize((W * sc, H * sc), Image.NEAREST), ((k % per) * W * sc, (k // per) * H * sc))
        sheet.convert("RGB").save(os.path.join(out, f"{name}_sheet.png"))
    json.dump({"veil_prowler": res}, open(os.path.join(out, "veil_attacks.json"), "w"), indent=1)
    # the neutral pose must equal the stand sprite of the walk
    st = V.frame(0, stand=True) if False else None
