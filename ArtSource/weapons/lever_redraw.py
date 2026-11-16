#!/usr/bin/env python3
"""Lever loops redrawn (2026-11-16, Yan: "rough art: the lever loops, incl. the thin Nightwalker one -> try and fix it").
lever_arc.py cuts the loop out of the picture as a 1-2 px ragged ring. This keeps its silhouette and hinge (HammerPivot, so the game code is unchanged) and rebuilds it as a bolder
steel loop: the ring grows one pixel outward (never into its own hole), then gets a dark outline outside and in, a lit upper-left edge, a shaded lower-right edge and a
glint, in the steel colour the gun's own lever already had. Run after lever_arc.py:  python3 ArtSource/weapons/lever_redraw.py [Key ...]"""
import sys, numpy as np
from PIL import Image
from scipy import ndimage as ndi
D = 'Assets/Resources/Sprites/Weapons/'
KEYS = ['FrontierLeverRifle', 'Nightwalker4570', 'HaldenLeverShotgun']
OUT = np.array([14, 14, 18, 255], np.uint8)
def run(k, grow):
    p = np.array(Image.open(D + f'weapon_{k}_part_orig.png' if False else D + f'weapon_{k}_part.png').convert('RGBA')); a = p[..., 3] > 0
    lum = p[..., :3].astype(int).sum(-1)
    steel = a & (lum > 150); base = np.median(p[steel][:, :3], axis=0) if steel.any() else np.array([90, 95, 105])
    filled = ndi.binary_fill_holes(a); hole = filled & ~a
    big = hole.copy()
    for _ in range(2): big = ndi.binary_dilation(big, ndi.generate_binary_structure(2, 1)) | big     # the loop's hole, a little larger: nothing grows into it
    s4 = ndi.generate_binary_structure(2, 1)
    thick = a.copy()
    for _ in range(grow): thick = ndi.binary_dilation(thick, s4) & ~hole | thick
    thick &= ~ndi.binary_erosion(hole, s4, iterations=0) if False else thick
    out = np.zeros_like(p)
    inner = ndi.binary_erosion(thick, s4)
    edge = thick & ~inner
    # the hole's rim counts as an edge too (an inner outline)
    rim = thick & ndi.binary_dilation(hole, s4)
    outline = edge | rim
    hi = np.clip(base * 1.30 + 12, 0, 255).astype(np.uint8); lo = np.clip(base * 0.62, 0, 255).astype(np.uint8); mid = np.clip(base, 0, 255).astype(np.uint8)
    H, W = a.shape
    for y in range(H):
        for x in range(W):
            if not thick[y, x]: continue
            if outline[y, x]: out[y, x, :3] = OUT[:3]; out[y, x, 3] = 255; continue
            up = outline[y - 1, x] if y > 0 else False; lf = outline[y, x - 1] if x > 0 else False
            dn = outline[y + 1, x] if y < H - 1 else False; rt = outline[y, x + 1] if x < W - 1 else False
            c = hi if (up or lf) and not (dn or rt) else lo if (dn or rt) and not (up or lf) else mid
            out[y, x, :3] = c; out[y, x, 3] = 255
    Image.fromarray(out).save(D + f'weapon_{k}_part.png')
    print(k, 'loop px', int(a.sum()), '->', int(thick.sum()), 'base', mid.tolist())
if __name__ == '__main__':
    for k in (sys.argv[1:] or KEYS): run(k, 1)
