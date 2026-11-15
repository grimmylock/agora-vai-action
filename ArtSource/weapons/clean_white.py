#!/usr/bin/env python3
"""Cleans light-grey / white pixels off a SpriteCook still by hand (Yan, 2026-11-15, TR-30 and AKS-74U: "clean up the white pixels"): python3 ArtSource/weapons/clean_white.py <still.png> [lum_threshold=150]
- small clusters (up to 12 px: glints in slots, single bright dots) are painted over with the median of the darker opaque pixels round them;
- big clusters (a light magazine base...) are darkened to 42 % so they match the dark body but keep their shading.
The original is kept next to the still as <name>_orig.png (never overwritten); run it on the _A still, then `batch_native.py install ...` again."""
import os, sys, shutil, numpy as np
from PIL import Image
from scipy import ndimage as nd
p = sys.argv[1]; thr = float(sys.argv[2]) if len(sys.argv) > 2 else 150
orig = p.replace('.png', '_orig.png')
if not os.path.exists(orig): shutil.copy(p, orig)
a = np.array(Image.open(orig).convert('RGBA')); rgb = a[..., :3].astype(float); lum = rgb.mean(2); op = a[..., 3] > 0
light = op & (lum > thr) & ((rgb.max(2) - rgb.min(2)) < 45); lab, n = nd.label(light, structure=np.ones((3, 3)))
for i in range(1, n + 1):
    m = lab == i
    if m.sum() > 12: a[m, :3] = (rgb[m] * 0.42).astype(np.uint8); print('darkened', int(m.sum()), 'px')
    else:
        ring = nd.binary_dilation(m, iterations=2) & ~m & op & (lum < thr)
        if ring.sum(): a[m, :3] = np.median(rgb[ring], axis=0).astype(np.uint8); print('painted', int(m.sum()), 'px')
Image.fromarray(a, 'RGBA').save(p)
