"""Elder Gaunt key poses from SpriteCook "donor" arms (2026-11-16).

SpriteCook's edit mode cannot MOVE a limb: asked to throw the arm forward it keeps the hanging arms (re-drawn
straight, the zigzag elbows lost) and ADDS a third arm in the new pose. That added arm is good, so we keep only it:
the base sprite stays pixel for pixel, its FAR arm (the dark one on the right) is erased, and the donor arm is pasted
behind the head. The near arm (the one with the big hand at the bottom centre) keeps its zigzag.
Everything is in the 192x192 padded frame (base sprite at offset 20,62; room above and right for a raised or thrown arm).
Donor files + their SpriteCook asset ids: keyposes/README.md.   python3 gaunt_donor_arm.py  -> keyposes/*_composite.png
"""
import os, json
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi

HERE = os.path.dirname(os.path.abspath(__file__))
KP = os.path.join(HERE, 'mutants_anim', 'gaunt', 'spritecook', 'keyposes')
BASE_STILL = os.path.join(HERE, 'mutants_anim', 'gaunt', 'spritecook', 'redesign_v1_arms.png')
OX, OY, CANVAS = 20, 62, 192
NEAR = [(69, 40), (70, 56), (72, 70), (75, 82), (79, 94), (82, 106), (85, 116), (87, 126)]
NEAR_R = [5, 5, 5, 5, 5, 5, 8, 10]

def far_arm_mask():
    """Opaque pixels right of the near arm below the jaw = the dark far arm (zigzag spur, band, hand), still coordinates."""
    b = np.array(Image.open(BASE_STILL).convert('RGBA')); op = b[..., 3] > 0
    yy, xx = np.mgrid[0:128, 0:128]; N = np.zeros((128, 128), bool)
    for (p, r), (q, r2) in zip(zip(NEAR, NEAR_R), zip(NEAR[1:], NEAR_R[1:])):
        for t in np.linspace(0, 1, 40):
            cx = p[0] + (q[0] - p[0]) * t; cy = p[1] + (q[1] - p[1]) * t; rr = r + (r2 - r) * t
            N |= ((xx - cx) ** 2 + (yy - cy) ** 2) <= rr * rr
    return op & ~N & (yy >= 64) & (xx >= 76)

def base_without_far_arm():
    cv = Image.new('RGBA', (CANVAS, CANVAS), (0, 0, 0, 0)); cv.alpha_composite(Image.open(BASE_STILL).convert('RGBA'), (OX, OY))
    a = np.array(cv); F = np.zeros((CANVAS, CANVAS), bool); F[OY:OY + 128, OX:OX + 128] = far_arm_mask(); a[F] = 0
    return Image.fromarray(a)

def compose(donor_file, shift, polygon, drop_specks=30):
    """donor_file: SpriteCook edit result; shift: (dx, dy) that lays its head on the base head; polygon: the donor arm, padded frame."""
    don = Image.open(os.path.join(KP, donor_file)).convert('RGBA')
    d = Image.new('RGBA', (CANVAS, CANVAS), (0, 0, 0, 0)); d.alpha_composite(don, shift) if shift[0] >= 0 and shift[1] >= 0 else d.paste(don, shift)
    m = Image.new('L', (CANVAS, CANVAS), 0); ImageDraw.Draw(m).polygon(polygon, fill=255)
    da = np.array(d); layer = np.zeros_like(da); sel = np.array(m) > 0; layer[sel] = da[sel]
    comp = Image.new('RGBA', (CANVAS, CANVAS), (0, 0, 0, 0)); comp.alpha_composite(Image.fromarray(layer)); comp.alpha_composite(base_without_far_arm())
    a = np.array(comp); lab, n = ndi.label(a[..., 3] > 0, structure=np.ones((3, 3)))
    for i, s in enumerate(ndi.sum(a[..., 3] > 0, lab, range(1, n + 1)), 1):
        if s < drop_specks: a[lab == i] = 0                    # leftovers of the erased arm
    return Image.fromarray(a)

POSES = {
    'strike': dict(donor='strike_donor_48a2f6ee.png', shift=(-2, 1),
                   polygon=[(96, 106), (104, 103), (118, 116), (126, 125), (140, 121), (156, 117), (192, 98), (192, 140), (156, 131), (128, 139), (118, 139), (106, 128), (98, 116)]),
}

if __name__ == '__main__':
    for name, p in POSES.items():
        out = compose(p['donor'], p['shift'], p['polygon']); out.save(os.path.join(KP, name + '_composite.png')); print(name, 'ok', out.getbbox())
