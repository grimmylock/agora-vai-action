#!/usr/bin/env python3
"""SKN Karabin: removes the folded bayonet from the SpriteCook still (Yan, 2026-11-15: "if it can't be unfolded it's better to remove it", the game has no bayonet melee). Works on the STILL, in its own pixels
(244x59): clears the blade, its mount and the lug under the barrel, and the stray speck below the gun, then closes the cut with a 1 px dark outline. Run BEFORE make_gun_x2.py SKNKarabin ... --native:
   python3 ArtSource/weapons/edit_sks_no_bayonet.py && python3 ArtSource/weapons/make_gun_x2.py SKNKarabin ArtSource/weapons/spritecook_pilot/longguns/SKNKarabin_spritecook.png --native"""
import os, shutil, numpy as np
from PIL import Image
from scipy import ndimage as nd
P = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'spritecook_pilot', 'longguns', 'SKNKarabin_spritecook.png'); ORIG = P.replace('.png', '_with_bayonet.png')
if not os.path.exists(ORIG): shutil.copy(P, ORIG)
a = np.array(Image.open(ORIG).convert('RGBA')); a[22:, 168:] = 0; a[17:, 178:] = 0
m = a[..., 3] > 0; lab, n = nd.label(m, structure=np.ones((3, 3))); big = lab == (1 + int(np.argmax(nd.sum(m, lab, range(1, n + 1))))); a[~big] = 0
m = a[..., 3] > 0; edge = m & ~nd.binary_erosion(m, structure=np.array([[0, 1, 0], [1, 1, 1], [0, 1, 0]]))
a[edge, :3] = (14, 14, 18); Image.fromarray(a, 'RGBA').save(P); print('saved', P)
