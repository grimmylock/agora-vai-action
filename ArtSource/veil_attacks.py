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
V.oy = 16; V.H = rig.H0 + 2 * V.oy; V.GROUND = V.oy + 31          # more room above for the leap (veil_walk's globals are read at call time)
ox, oy, W, H, GROUND = V.ox, V.oy, V.W, V.H, V.GROUND
STAND_FEET = {"far_hind": (-1.5, 0.0), "near_hind": (1.0, 0.0), "far_fore": (1.0, 0.0), "near_fore": (-1.0, 0.0)}
FAR = ("far_hind", "far_fore"); NEAR = ("near_hind", "near_fore")

def render(p):
    """p: bx/by whole-body offset (right/down), rise (up), nx/ny head offset, nrot head turn (+ = nose up), jaw degrees, flash, feet: {leg: (fo, fl)} overrides"""
    bx, by, rise = p.get("bx", 0), p.get("by", 0), p.get("rise", 0); nx, ny, nrot = p.get("nx", 0), p.get("ny", 0), p.get("nrot", 0)
    feet = dict(STAND_FEET); feet.update(p.get("feet", {}))
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); bob = by - rise
    def leg(n): return V.leg_img(n, 0, bob, False, bx, feet[n])
    for n in FAR: im.alpha_composite(leg(n))
    im.alpha_composite(rig.parts["hind"], (ox + bx, oy + bob)); im.alpha_composite(rig.parts["fore"], (ox + bx, oy + bob))
    hx, hy = ox + bx + nx, oy + bob + ny; piv = (rig.c["head_x"], rig.c["hinge"][1] - 2)
    head = rig.parts["head"].rotate(nrot, resample=Image.NEAREST, center=piv) if nrot else rig.parts["head"]
    im.alpha_composite(head, (hx, hy)); rig.mouth(im, hx, hy, nrot, piv, p.get("jaw", 0), p.get("flash", False))
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
POUNCE = [
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
    for name, poses in (("bite", BITE), ("pounce", POUNCE)):
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
