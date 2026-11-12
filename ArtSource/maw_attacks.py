# Blind Maw BITE, ALTERNATIVE BITE and POUNCE on the new sprite (2026-11-12), body and head only. The Maw's face IS its mouth, so there is no jaw animation to add or leave out: the strike is the whole-body
# lunge with the head thrust and nod (Yan: "the harder one"). Same method as veil_attacks.py: the cut-out body (hindquarters + forequarters) moves as ONE piece (offset, rise, a whole-pixel column-shear tilt),
# the head with its mouth (head + jaw pixels) moves on its own by whole pixels with a nod shear (the forequarters overlap the head by 4 px so a thrust of up to 4 px leaves no gap), the legs are the walk's
# generated stubby legs (maw_hybrid.leg_img) with the feet planted on the stand pose's spots; in the air they stretch only as far as 8-px legs can (about half the Veil's reach).   python3 maw_attacks.py <out_dir>
import math, os, sys, json
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as nd
import maw_hybrid as MH
import mutant_from_ref as m
import veil_attacks as VA

rig = MH.rig
MH.ox, MH.oy = 14, 16; MH.W = rig.W0 + 2 * MH.ox; MH.H = rig.H0 + 2 * MH.oy; MH.GROUND = MH.oy + 30
ox, oy, W, H, GROUND = MH.ox, MH.oy, MH.W, MH.H, MH.GROUND
STAND_FEET = {"far_hind": (-1.0, 0.0), "near_hind": (1.0, 0.0), "far_fore": (1.0, 0.0), "near_fore": (-1.0, 0.0)}
FAR = ("far_hind", "far_fore"); NEAR = ("near_hind", "near_fore")
HEADX = rig.c["head_x"]
# the mouth: a CHOMP with the sprite's own lower-jaw piece (rig.parts['jaw'], its own pixels) moved up and down by whole pixels, no rotation: open a little wider in the wind-up, SNAP shut in one frame at the
# strike (the lower teeth meet the upper ones), then relax. Yan: the Maw's face is basically just his maw, so with no mouth movement it looks wrong.
HEAD_ONLY = Image.fromarray(np.array(rig.parts["head"]).copy()); JAW_ONLY = Image.fromarray(np.array(rig.parts["jaw"]).copy())
for _im in (HEAD_ONLY, JAW_ONLY):
    _a = np.array(_im); _a[26:] = 0; _im.paste(Image.fromarray(_a), (0, 0))
def jaw_dy(p):
    if p.get("flash"): return -3                      # the snap: shut
    j = p.get("jaw", 0)
    if j >= 20: return 2                              # wide open (never more: past 3 px the jaw floats off the face)
    if j >= 8: return 1
    if j > 0: return -1                               # relaxing after the snap
    return 0                                              # 32: the head and mouth start here

def shear(img, x0, amount):
    """vertical whole-pixel shear of the columns from x0 on: 0 at x0 growing to `amount` over 10 columns"""
    if amount == 0: return img
    a = np.array(img); src = a.copy(); a[:] = 0
    for x in range(a.shape[1]):
        f = 0.0 if x <= x0 else min(1.0, (x - x0) / 10.0); sh = int(round(amount * f))
        for y in range(a.shape[0]):
            ny = y + sh
            if 0 <= ny < a.shape[0] and src[y, x, 3]: a[ny, x] = src[y, x]
    return Image.fromarray(a)

def tilt_shift(x, tilt): return int(round(tilt * (x - (ox + 24)) / 24.0))
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
    bx, by, rise = p.get("bx", 0), p.get("by", 0), p.get("rise", 0); nx, ny, nrot, tilt = p.get("nx", 0), p.get("ny", 0), p.get("nrot", 0), p.get("tilt", 0)
    feet = dict(STAND_FEET)
    for n, (fo, fl) in p.get("feet", {}).items(): feet[n] = (fo * 0.3, fl * 0.5)                   # stubby legs reach about a third as far as the Veil's (at half they stretched into long sticks)
    bob = by - rise; im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    def leg(n):
        hx_ = MH.LEGS[n][0][0] + ox + bx; return MH.leg_img(n, 0, bob + tilt_shift(hx_, tilt), False, bx, feet[n])
    for n in FAR: im.alpha_composite(leg(n))
    up = Image.new("RGBA", (W, H), (0, 0, 0, 0)); up.alpha_composite(MH.BODY, (ox + bx, oy + bob))
    hd = Image.new("RGBA", MH.HEAD.size, (0, 0, 0, 0)); hd.alpha_composite(HEAD_ONLY); hd.alpha_composite(JAW_ONLY, (0, jaw_dy(p)))
    hd = shear(hd, HEADX, -int(round(nrot / 3.0)))                                                    # nose up (+nrot) raises the nose end
    up.alpha_composite(hd, (ox + bx + nx, oy + bob + ny)); im.alpha_composite(tilt_layer(up, tilt))
    for n in NEAR: im.alpha_composite(leg(n))
    im = m.outline(im); a = np.array(im); l, k = nd.label(a[..., 3] > 0, structure=np.ones((3, 3)))
    for j in range(1, k + 1):
        if (l == j).sum() < 6: a[l == j] = 0
    return Image.fromarray(a)

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "mutants_anim/maw_attacks"; os.makedirs(out, exist_ok=True)
    VA.W, VA.H, VA.GROUND = W, H, GROUND; res = {}
    for name, poses in (("bite", VA.BITE), ("bite2", VA.BITE_B), ("pounce", VA.POUNCE)):
        fr = [render(p) for p, _, _ in poses]; ms = [t for _, t, _ in poses]; tr = [x for _, _, x in poses]
        for i, f in enumerate(fr): f.save(os.path.join(out, f"{name}_{i}.png"))
        VA.strip_of(fr).save(os.path.join(out, f"blind_maw_{name}_strip.png")); VA.gif(os.path.join(out, f"{name}.gif"), fr, ms)
        res[name] = {"frame": [W, H], "ms": ms, "travel": tr, "ground_row": GROUND}
        print(name, len(fr), "frames; components", [nd.label(np.array(f)[..., 3] > 0, structure=np.ones((3, 3)))[1] for f in fr], "lowest rows", [int(np.where(np.array(f)[..., 3] > 0)[0].max()) for f in fr], "ground", GROUND)
        sc = 7; per = 5; rows = (len(fr) + per - 1) // per; sheet = Image.new("RGBA", (W * per * sc, H * rows * sc), (105, 115, 90, 255))
        for k, f in enumerate(fr): sheet.alpha_composite(f.resize((W * sc, H * sc), Image.NEAREST), ((k % per) * W * sc, (k // per) * H * sc))
        sheet.convert("RGB").save(os.path.join(out, f"{name}_sheet.png"))
    json.dump({"blind_maw": res}, open(os.path.join(out, "maw_attacks.json"), "w"), indent=1)
    st = np.array(render({}))[..., 3] > 0; ref = np.array(MH.frame(0, stand=True))[..., 3] > 0
    print("neutral pose vs the walk's stand pose (alpha px differing):", int((st ^ ref).sum()))
