#!/usr/bin/env python3
"""Claw and call frames for the two Gaunts made IN CODE from their clean SpriteCook stills (2026-11-16; Yan: the SpriteCook claw and call strips "look way too messy: the bones disappear, the hands are messed up,
the antlers"). No generated limbs: every frame is the still itself, its rows shifted sideways in proportion to their height above the feet (a LEAN: a shear, so no pixel is resampled or blurred), plus a
whole-body shift and a one-pixel shake on the held scream. The wind-up leans back, the strike throws the body forward; the impact is the game's own hit effect, the call's landing a thunderclap ring.
   python3 ArtSource/gaunt_codeframes.py"""
import os, numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); SC = os.path.join(HERE, 'mutants_anim', 'gaunt', 'spritecook'); A = os.path.join(HERE, '..', 'Assets', 'Resources', 'Sprites', 'Mutants', 'anim')
STILLS = {'elder_gaunt': (os.path.join(SC, 'redesign_v1_arms.png'), 'gaunt_elder'), 'gaunt': (os.path.join(SC, 'redesign_v2_arms.png'), 'gaunt_wild')}
# (lean = how many pixels the top of the sprite is shifted forward (+) or back (-), dx = whole-body shift, shake = a one-pixel jitter)
CLAW = [(0, 0), (-3, -2), (-6, -4), (-8, -4), (3, 3), (11, 10), (7, 5), (2, 1)]                                   # contact on frame 5 (the strike), 540 ms with MS below
CALL = [(0, 0), (-3, 0), (-7, -1), (-9, -1), (-9, -2), (6, 2), (5, 2), (1, 0)]                                    # head back and held (3-4, shaking), the snap forward lands the call on frame 5
def shear(im, lean, dx, shake=0):
    a = np.array(im); H, W = a.shape[:2]; ys = np.where(a[..., 3].any(1))[0]; bot = ys.max(); top = ys.min(); pad = 16
    out = np.zeros((H, W + 2 * pad, 4), np.uint8)
    for y in range(H):
        if not a[y, :, 3].any(): continue
        h = max(0, bot - y) / max(1, bot - top)                                                                  # 0 at the feet, 1 at the top of the antlers
        s = int(round(dx * min(1.0, h * 3) + lean * h * h + (shake if (y // 3) % 2 else -shake) * h))           # the feet stay planted; the body leans from the hips up
        x0 = pad + s; seg = a[y]; lo = max(0, -x0); hi = min(W, W + 2 * pad - x0)
        out[y, x0 + lo: x0 + hi] = seg[lo:hi]
    return Image.fromarray(out)
def build(who, still, folder, ms_claw, ms_call):
    im = Image.open(still).convert('RGBA'); od = os.path.join(HERE, 'mutants_anim', folder); os.makedirs(od, exist_ok=True)
    for name, seq, shake in (('claw', CLAW, 0), ('scream', CALL, 1)):
        fs = [shear(im, l, d, shake if name == 'scream' and 2 <= i <= 4 else 0) for i, (l, d) in enumerate(seq)]
        bb = [f.getbbox() for f in fs]; y0 = min(b[1] for b in bb); y1 = max(b[3] for b in bb); xc = (fs[0].getbbox()[0] + fs[0].getbbox()[2]) // 2; half = max(max(abs(b[0] - xc), abs(b[2] - xc)) for b in bb) + 2
        fs = [f.crop((xc - half, y0, xc + half, y1)) for f in fs]; W, H = fs[0].size
        strip = Image.new('RGBA', (W * 8, H), (0, 0, 0, 0))
        for i, f in enumerate(fs): strip.alpha_composite(f, (i * W, 0))
        if name == 'claw':
            strip.save(os.path.join(od, f'{who}_claw_strip.png'))
            import json; json.dump({who: {'claw': {'frame': [W, H], 'ms': ms_claw, 'travel': [0] * 8}}}, open(os.path.join(od, f'{who}_attacks.json'), 'w'), indent=1)
        else:
            strip.save(os.path.join(A, f'{who}_scream.png')); os.replace(os.path.join(A, f'{who}_scream.png'), os.path.join(A, f'{who}_scream.bytes'))
        strip.save(os.path.join(od, f'{who}_{name}_code_strip.png')); print(who, name, W, 'x', H)
if __name__ == '__main__':
    for who, (still, folder) in STILLS.items(): build(who, still, folder, [110, 150, 140, 70, 70, 60, 110, 110], None)
