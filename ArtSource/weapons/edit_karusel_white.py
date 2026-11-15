#!/usr/bin/env python3
"""Karusel-5, variation B (Yan, 2026-11-15: "revolver shotgun B, just clean up the white pixels"): a 5 px light-grey streak on the edge of the cylinder is painted over with the cylinder's own dark colour (median of the dark pixels around it).
Works on the still (batch8/Karusel5_B.png, the generator's original kept as Karusel5_B_orig.png); then:  python3 ArtSource/weapons/batch_native.py install ArtSource/weapons/spritecook_pilot/batch8.json B,A,B,A"""
import os, shutil, numpy as np
from PIL import Image
from scipy import ndimage as nd
D = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'spritecook_pilot', 'batch8'); P = os.path.join(D, 'Karusel5_B.png'); ORIG = os.path.join(D, 'Karusel5_B_orig.png')
if not os.path.exists(ORIG): shutil.copy(P, ORIG)
a = np.array(Image.open(ORIG).convert('RGBA')); rgb = a[..., :3].astype(int); lum = rgb.mean(2); op = a[..., 3] > 0
white = op & (lum > 150) & ((rgb.max(2) - rgb.min(2)) < 40)
lab, n = nd.label(white, structure=np.ones((3, 3)))
for i in range(1, n + 1):
    m = lab == i
    if m.sum() > 12: continue
    ring = nd.binary_dilation(m, iterations=2) & ~m & op & (lum < 90)
    if ring.sum(): a[m, :3] = np.median(a[ring][:, :3], axis=0).astype(np.uint8); print('painted', int(m.sum()), 'px')
Image.fromarray(a, 'RGBA').save(P)
