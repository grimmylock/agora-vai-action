# Recover ONE Brute pose that Yan generated against the idle reference (second round, 2026-11-12: one pose per image on a black background, 640x640, ~8 px blocks) to game pixels:
# flood the black background from the border, find the block grid, sample block centres, snap to the palette of the first recovered idle, put back the 1-cell dark outline the black background swallowed.
#   python3 brute_recover2.py <image> <name>     -> mutants_anim/brute_poses/<name>.png
import os, sys, numpy as np
from PIL import Image
from scipy import ndimage as nd
HERE = os.path.dirname(os.path.abspath(__file__)); D = os.path.join(HERE, "mutants_anim", "brute_poses")
src, name = sys.argv[1], sys.argv[2]
im = np.array(Image.open(src).convert("RGB")).astype(int); lum = im.mean(2)
dark = lum < 36; lab, n = nd.label(dark); border = set(lab[0].tolist() + lab[-1].tolist() + lab[:, 0].tolist() + lab[:, -1].tolist()) - {0}
bg = np.isin(lab, list(border)); fg = ~bg; fg = nd.binary_opening(fg, iterations=1)
ys, xs = np.where(fg); y0, y1, x0, x1 = ys.min() - 6, ys.max() + 7, xs.min() - 6, xs.max() + 7
sub = im[max(0, y0):y1, max(0, x0):x1]; fgs = fg[max(0, y0):y1, max(0, x0):x1]; g = sub.mean(2)
def period(v, lo=6.0, hi=12.0):
    best = None
    for p in np.arange(lo, hi, 0.02):
        for ph in np.arange(0, p, 0.5):
            idx = (np.arange(ph, len(v) - 1, p)).astype(int); sc = np.abs(v[np.minimum(idx + 1, len(v) - 1)] - v[idx]).sum()
            if best is None or sc > best[0]: best = (sc, p, ph)
    return best[1], best[2]
px, fx = period(np.abs(np.diff(g, axis=1)).sum(0)); py, fy = period(np.abs(np.diff(g, axis=0)).sum(1))
idle = np.array(Image.open(os.path.join(D, "idle.png"))); pal = np.unique(idle[idle[..., 3] > 0][:, :3], axis=0)
cx = np.arange(fx, sub.shape[1] - px, px); cy = np.arange(fy, sub.shape[0] - py, py); cell = np.zeros((len(cy), len(cx), 4), np.uint8)
for j, y in enumerate(cy):
    for i, x in enumerate(cx):
        sl = (slice(int(y + py * 0.3), int(y + py * 0.7) + 1), slice(int(x + px * 0.3), int(x + px * 0.7) + 1))
        if fgs[sl].mean() < 0.5: continue
        col = np.median(sub[sl].reshape(-1, 3), 0); cell[j, i] = (*pal[((pal - col) ** 2).sum(1).argmin()], 255)
a = cell[..., 3] > 0; lb, k = nd.label(a, structure=np.ones((3, 3)))
if k > 1:
    sz = nd.sum(a, lb, range(1, k + 1))
    for j in range(k):
        if sz[j] < 40: cell[lb == j + 1] = 0                                   # keep every real part (a raised arm is split from the torso by a dark gap), drop specks
a = cell[..., 3] > 0; ring = nd.binary_dilation(a, structure=np.array([[0, 1, 0], [1, 1, 1], [0, 1, 0]])) & ~a
cell = np.pad(cell, ((1, 1), (1, 1), (0, 0))); a = np.pad(a, 1); ring = nd.binary_dilation(a, structure=np.array([[0, 1, 0], [1, 1, 1], [0, 1, 0]])) & ~a; cell[ring] = (14, 14, 8, 255)
ys, xs = np.where(cell[..., 3] > 0); cell = cell[ys.min(): ys.max() + 1, xs.min(): xs.max() + 1]
os.makedirs(D, exist_ok=True)
if os.path.exists(os.path.join(D, name + ".png")): os.replace(os.path.join(D, name + ".png"), os.path.join(D, name + "_round1.png"))
Image.fromarray(cell).save(os.path.join(D, name + ".png")); print(name, "period %.2f/%.2f" % (px, py), "cells", cell.shape[1], "x", cell.shape[0])
