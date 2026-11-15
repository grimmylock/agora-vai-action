#!/usr/bin/env python3
"""Colour-matches the warm wood of a double-size gun picture to its reference photo (Yan, 2026-11-15: the VK-74 and the Ranger P8 "become orange-ish when resized"). The generated stills are more saturated than the photos
and the 16-colour step flattens the grain; measured on the stock (photo vs picture): VK-74 hue 13 vs 19 deg, saturation 0.72 vs 0.87, shade spread 42 vs 25; Ranger saturation 0.50 vs 0.61, spread 35 vs 25.
Run AFTER make_gun_x2.py <Key>:   python3 ArtSource/weapons/grade_wood.py VK74 | RangerP8
Warm pieces are found as connected warm-pixel groups; `keep` lists groups left alone (the AK's orange plastic magazine is orange in the photo too: the group whose box starts at x >= 70 and y >= 20)."""
import os, sys, colorsys
import numpy as np
from PIL import Image
from scipy import ndimage as nd
P = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Assets', 'Resources', 'Sprites', 'Weapons')
GRADE = {'VK74': dict(dh=-6, sat=0.83, spread=1.5, keep=lambda b: b[0] >= 70 and b[1] >= 20), 'RangerP8': dict(dh=-3, sat=0.85, spread=1.7, vmul=0.88, keep=lambda b: False)}
def main(key):
    g = GRADE[key]; path = f'{P}/weapon_{key}_hd.png'; a = np.array(Image.open(path).convert('RGBA')); rgb = a[..., :3].astype(float) / 255
    mx = rgb.max(2); mn = rgb.min(2); sat = (mx - mn) / np.maximum(mx, 1e-3)
    warm = (a[..., 3] > 200) & (rgb[..., 0] > rgb[..., 1] * 1.15) & (rgb[..., 1] >= rgb[..., 2]) & (sat > 0.35) & (mx > 0.25)
    lab, n = nd.label(warm, structure=np.ones((3, 3))); wood = np.zeros_like(warm)
    for i in range(1, n + 1):
        ys, xs = np.where(lab == i)
        if len(xs) >= 25 and not g['keep']((xs.min(), ys.min())): wood |= lab == i
    mv = np.mean([colorsys.rgb_to_hsv(*rgb[y, x])[2] for y, x in zip(*np.where(wood))])
    for y, x in zip(*np.where(wood)):
        h, s, v = colorsys.rgb_to_hsv(*rgb[y, x]); h = (h + g['dh'] / 360.0) % 1.0; s = min(1.0, s * g['sat']); v = min(1.0, max(0.12, (mv + (v - mv) * g['spread']) * g.get('vmul', 1.0)))
        a[y, x, :3] = [int(round(c * 255)) for c in colorsys.hsv_to_rgb(h, s, v)]
    Image.fromarray(a, 'RGBA').save(path); print(key, 'graded', int(wood.sum()), 'pixels')
if __name__ == '__main__': main(sys.argv[1])
