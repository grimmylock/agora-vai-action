#!/usr/bin/env python3
"""Rough magazines repainted (2026-11-16, Yan: "rough art: ... the PPZ Zubr and Tambour drums, the G-7 cell -> try and fix it"). Same silhouettes and files as before (so the reload code, WeaponMags rows and
pivots are untouched); only the pixels inside are repainted: shaded barrels with end caps, ribs, a feed port and a glint.
   python3 ArtSource/weapons/rough_art.py        (run mags_hd.py first when the cuts change; this repaints weapon_<Key>_hdmag / _hdcyl)"""
import os, numpy as np
from PIL import Image
from scipy import ndimage as ndi
W = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Assets', 'Resources', 'Sprites', 'Weapons') + '/'
K = np.array([14, 14, 18], np.uint8)
def mix(c, f): return np.clip(np.array(c, float) * f, 0, 255).astype(np.uint8)
def barrel(k, fn, base, helical):
    im = np.array(Image.open(W + fn).convert('RGBA')); m = im[..., 3] > 0; ys, xs = np.where(m); x0, x1, y0, y1 = xs.min(), xs.max(), ys.min(), ys.max()
    inner = ndi.binary_erosion(m, np.ones((3, 3), bool)); out = im.copy(); out[~m] = 0
    cap = 3 if helical else 4
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if not m[y, x]: continue
            if not inner[y, x]: out[y, x, :3] = K; continue
            col = np.where(m[:, x])[0]; v = (y - col.min()) / max(1, col.max() - col.min())            # 0 at the top of this column, 1 at the bottom (the curved ends follow the outline)
            shade = 1.35 if v < 0.18 else 1.12 if v < 0.38 else 0.95 if v < 0.62 else 0.72 if v < 0.85 else 0.52   # lit from above: a rounded tube
            c = mix(base, shade)
            if x - x0 < cap + 1 or x1 - x < cap + 1: c = mix(base, shade * 0.62)                      # the end caps: darker
            if x - x0 == cap + 1 or x1 - x == cap + 1: c = mix(base, 1.5 if v < 0.5 else 0.4)          # ... with a rim line
            elif cap + 1 < x - x0 < x1 - x - cap:
                rib = ((x + (y - y0) * 1.0) % 8 < 1.0) if helical else ((x - x0) % 5 == 0)             # a helical magazine: slanted ribs; a drum: upright ones
                if rib: c = mix(base, shade * (0.55 if helical else 0.38))
            out[y, x, :3] = c
    if helical:                                                                                       # the feed lips at the top and a glint along the barrel
        for x in range(x0 + 12, x1 - 12): 
            if m[y0 + 1, x] and inner[y0 + 1, x]: out[y0 + 1, x, :3] = mix(base, 1.7)
    Image.fromarray(out).save(W + fn); print(k, fn, 'repainted', int(m.sum()), 'px')
def cell(k, fn):
    im = np.array(Image.open(W + fn).convert('RGBA')); m = im[..., 3] > 0; ys, xs = np.where(m); x0, x1, y0, y1 = xs.min(), xs.max(), ys.min(), ys.max()
    out = np.zeros_like(im); w = x1 - x0 + 1; h = y1 - y0 + 1
    cy_lo, cy_hi = (14, 232, 255), (170, 250, 255)
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            lx, ly = x - x0, y - y0; edge = lx in (0, w - 1) or ly in (0, h - 1) or (ly >= h - 2 and lx in (1, w - 2))
            if ly == h - 1 and lx in (0, 1, w - 2, w - 1): continue                                     # the bottom corners round off
            if edge: c = K
            elif ly >= h - 3: c = (62, 66, 74)                                                         # the contact end: dull steel
            elif ly <= 1: c = (96, 102, 112)                                                           # the cap
            elif 2 <= lx <= w - 3 and 3 <= ly <= h - 5:                                                 # the glowing core: bright in the middle, a hot centre line
                c = cy_hi if lx in (w // 2 - 1, w // 2) and ly % 3 != 0 else cy_lo if ly % 3 != 0 else (8, 120, 150)
            elif lx <= 2: c = (84, 90, 100)                                                           # the lit side
            else: c = (36, 40, 48)                                                                      # the shaded side
            out[y, x] = (*c, 255)
    Image.fromarray(out).save(W + fn); print(k, fn, 'cell repainted', w, 'x', h)
if __name__ == '__main__':
    barrel('PPZZubr', 'weapon_PPZZubr_hdmag.png', (66, 70, 78), True)
    barrel('Tambour12', 'weapon_Tambour12_hdcyl.png', (58, 66, 82), False)
    cell('G7RailPrototype', 'weapon_G7RailPrototype_hdmag.png')
