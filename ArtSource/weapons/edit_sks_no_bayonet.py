#!/usr/bin/env python3
"""SKN Karabin: removes the folded bayonet from the double-size picture (Yan, 2026-11-15: "if it can't be unfolded it's better to remove it", the game has no bayonet melee). Run AFTER make_gun_x2.py SKNKarabin:
it clears everything below the barrel line (rows >= 23) from x = 110 on (the blade, its mount and the lug), then closes the cut with the 1 px dark outline."""
import os, numpy as np
from PIL import Image
from scipy import ndimage as nd
P = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Assets', 'Resources', 'Sprites', 'Weapons', 'weapon_SKNKarabin_hd.png')
a = np.array(Image.open(P).convert('RGBA')); a[23:, 110:] = 0
m = a[..., 3] > 0; edge = m & ~nd.binary_erosion(m, structure=np.array([[0, 1, 0], [1, 1, 1], [0, 1, 0]]))
a[edge, :3] = (14, 14, 18); Image.fromarray(a, 'RGBA').save(P); print('saved', P)
