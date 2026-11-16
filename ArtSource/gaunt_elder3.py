"""Elder Gaunt v3 (three arms): poses built on ONE pixel-exact body (2026-11-16).

Yan chose SpriteCook's "version 2" (the strike edit with three arms, straight arms, no zigzag elbows) as the Elder.
Body = `elder3_base_b3.png` (that edit with its thrown arm erased, 192x192, SpriteCook asset ee3314dc... when uploaded).
The third arm comes from three SpriteCook ADD-an-arm edits of that body; only the arm's pixels are kept (everything
that is not already body), so the body never shimmers between frames:
  fold   = rest pose, a mantis fold under the jaw          (asset e1e7f873, shift -1,0)
  wind   = cocked up over the back like a scorpion's tail  (asset cd799152, shift -3,+1)
  strike = thrown forward, hand wide open                  (asset 48a2f6ee, shift  0,0; the body's parent image)
python3 gaunt_elder3.py -> keyposes/arm_<pose>.png (arm pixels only), keyposes/elder3_<pose>.png (body + arm)
"""
import os
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

HERE = os.path.dirname(os.path.abspath(__file__))
KP = os.path.join(HERE, 'mutants_anim', 'gaunt', 'spritecook', 'keyposes')
C = 192
DONORS = {'fold': ('fold_donor_e1e7f873.png', (-1, 0)), 'wind': ('wind_donor_cd799152.png', (-3, 1)), 'strike': ('strike_donor_48a2f6ee.png', (0, 0))}

def load_body():
    return Image.open(os.path.join(KP, 'elder3_base_b3.png')).convert('RGBA')

def arm_layer(pose, min_px=40):
    """RGBA 192x192 holding only the third arm of `pose`, in the body's frame."""
    f, (dx, dy) = DONORS[pose]
    d = np.array(Image.open(os.path.join(KP, f)).convert('RGBA'))
    canvas = np.zeros((C, C + 16, 4), np.uint8); canvas[:d.shape[0], :d.shape[1]] = d
    canvas = np.roll(np.roll(canvas, dy, 0), dx, 1)[:, :C + 8]
    body = np.array(load_body())[..., 3] > 0
    new = (canvas[..., 3] > 0); new[:, :C] &= ~body
    lab, n = ndi.label(new, structure=np.ones((3, 3)))
    keep = np.zeros_like(new)                               # the arm = the biggest piece; the rest are slivers of the donor's re-drawn body outline
    sizes = ndi.sum(new, lab, range(1, n + 1)); keep |= lab == (int(np.argmax(sizes)) + 1)
    out = np.zeros_like(canvas); out[keep] = canvas[keep]
    return Image.fromarray(out[:, :C + 8])           # 8 px of spare width for a far reach

def compose(pose):
    arm = arm_layer(pose); body = load_body()
    cv = Image.new('RGBA', arm.size, (0, 0, 0, 0)); cv.alpha_composite(body); cv.alpha_composite(arm)
    return cv, arm

if __name__ == '__main__':
    for p in DONORS:
        cv, arm = compose(p); cv.save(os.path.join(KP, 'elder3_%s.png' % p)); arm.save(os.path.join(KP, 'arm_%s.png' % p))
        print(p, 'bbox', cv.getbbox(), 'arm px', int((np.array(arm)[..., 3] > 0).sum()))
