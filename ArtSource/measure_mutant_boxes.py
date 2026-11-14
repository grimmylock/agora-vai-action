#!/usr/bin/env python3
"""Prints each mutant sprite's hit box as Ballistics.MutantBox wants it: opaque height in tiles and half the median opaque row width in tiles.
HD sprites (Assets/Resources/Sprites/Mutants/mutant_hd_*.png) are 32 px a tile; the old atlas cells (mutant_atlas.png, order = SpriteAtlasIndex.Mutants) are 16 px a tile."""
import os, numpy as np
from PIL import Image
R = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'Assets', 'Resources', 'Sprites')
def meas(a, ppt):
    ys, xs = np.where(a); rows = a.sum(1); return (ys.max() - ys.min() + 1) / ppt, np.median(rows[rows > 0]) / 2 / ppt
for k in ['pallid_prowler', 'blind_maw', 'veil_prowler', 'dread_prowler', 'blight_brute']:
    h, w = meas(np.array(Image.open(f'{R}/Mutants/mutant_hd_{k}.png').convert('RGBA'))[..., 3] > 0, 32); print(f'{k:16s} height {h:.2f}  half {w:.2f}')
at = np.array(Image.open(f'{R}/Mutants/mutant_atlas.png' if os.path.exists(f'{R}/Mutants/mutant_atlas.png') else f'{R}/mutant_atlas.png').convert('RGBA'))[..., 3] > 0
for i, n in {2: 'rot_swarm', 5: 'alpha_prowler', 7: 'mind_weaver', 10: 'void_weaver'}.items():
    h, w = meas(at[:, i * 16:(i + 1) * 16], 16); print(f'{n:16s} height {h:.2f}  half {w:.2f}')
