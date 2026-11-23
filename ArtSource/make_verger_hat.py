#!/usr/bin/env python3
"""2026-11-23 (Yan: 'use the sprite'): the Verger's hat, cut from his own art: the pixels the coat has and the bare coat (hat off) has not, front cell of armor_unique_verger_coat_x2 / _bare_x2 -> Assets/Resources/Sprites/Fx/fx_verger_hat.png (the cutscene lays it on the flagstones)."""
import os, numpy as np
from PIL import Image
R = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'Assets', 'Resources', 'Sprites')
a = np.array(Image.open(f'{R}/armor_unique_verger_coat_x2.png').convert('RGBA')); b = np.array(Image.open(f'{R}/armor_unique_verger_coat_bare_x2.png').convert('RGBA'))
cell = min(a.shape[0], a.shape[1]); ca, cb = a[:cell, :cell], b[:cell, :cell]
diff = (ca[..., 3] > 0) & ((cb[..., 3] == 0) | (np.abs(ca[..., :3].astype(int) - cb[..., :3].astype(int)).sum(-1) > 24))
print('cell', cell, 'hat pixels', int(diff.sum()))
from scipy import ndimage
lab, n = ndimage.label(diff, structure=np.ones((3,3)))
if n:
    top = min(range(1, n + 1), key=lambda k: np.where(lab == k)[0].min()); big = max(range(1, n + 1), key=lambda k: (lab == k).sum())
    diff = (lab == big)
ys, xs = np.where(diff)
if len(xs):
    hat = np.zeros_like(ca); hat[diff] = ca[diff]; im = Image.fromarray(hat, 'RGBA').crop((xs.min() - 1, ys.min() - 1, xs.max() + 2, ys.max() + 2))
    os.makedirs(f'{R}/Fx', exist_ok=True); im.save(f'{R}/Fx/fx_verger_hat.png'); print('hat', im.size)
    im.resize((im.width * 8, im.height * 8), Image.NEAREST).save('/tmp/verger_hat_x8.png')
