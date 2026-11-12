# Recover Yan's AI-made Brute pose sheet (refs/brute/brute_poses_ai.jpg, 2000x2000 JPG, checkerboard baked in, ~9.2 px blocks) to game pixels:
# cut each pose, find the block grid, sample block centres, drop checkerboard/white blocks, snap colours to the game Brute's own palette.  python3 brute_recover.py <out_dir>
import os, sys, numpy as np
from PIL import Image
from scipy import ndimage as nd
HERE = os.path.dirname(os.path.abspath(__file__))
im = np.array(Image.open(os.path.join(HERE, "refs", "brute", "brute_poses_ai.jpg")).convert("RGB")).astype(int)
sat = im.max(2) - im.min(2); lum = im.mean(2); bg = (sat < 14) & (lum > 150)
lab, n = nd.label(nd.binary_closing(nd.binary_opening(~bg, iterations=2), iterations=6)); sl = nd.find_objects(lab)
NAMES = {2: "idle", 1: "slam_up", 3: "slam_hit", 6: "reach", 7: "swipe", 5: "wind", 13: "throw_up", 14: "throw"}
G = np.array(Image.open(os.path.join(HERE, "..", "Assets", "Resources", "Sprites", "Mutants", "mutant_hd_blight_brute.png")).convert("RGBA"))
pal = np.unique(G[G[..., 3] > 0][:, :3], axis=0); cnt = [(np.all(G[..., :3] == c, 2) & (G[..., 3] > 0)).sum() for c in pal]
pal = pal[np.argsort(cnt)[::-1][:24]]
def period(v, lo=8.0, hi=10.5):
    best = None
    for p in np.arange(lo, hi, 0.02):
        for ph in np.arange(0, p, 0.5):
            idx = (np.arange(ph, len(v) - 1, p)).astype(int); s = np.abs(v[np.minimum(idx + 1, len(v) - 1)] - v[idx]).sum() - 0.0
            sc = s
            if best is None or sc > best[0]: best = (sc, p, ph)
    return best[1], best[2]
out = sys.argv[1] if len(sys.argv) > 1 else "mutants_anim/brute_poses"; os.makedirs(out, exist_ok=True)
for k, name in NAMES.items():
    s = sl[k - 1]; y0, y1, x0, x1 = max(0, s[0].start - 20), s[0].stop + 20, max(0, s[1].start - 20), s[1].stop + 20
    sub = im[y0:y1, x0:x1]; mask = bg[y0:y1, x0:x1]; g = sub.mean(2)
    px, fx = period(np.abs(np.diff(g, axis=1)).sum(0)); py, fy = period(np.abs(np.diff(g, axis=0)).sum(1))
    xs = np.arange(fx, sub.shape[1] - px, px); ys = np.arange(fy, sub.shape[0] - py, py)
    cell = np.zeros((len(ys), len(xs), 4), np.uint8)
    for j, y in enumerate(ys):
        for i, x in enumerate(xs):
            c = sub[int(y + py * 0.3): int(y + py * 0.7) + 1, int(x + px * 0.3): int(x + px * 0.7) + 1].reshape(-1, 3); m = mask[int(y + py * 0.3): int(y + py * 0.7) + 1, int(x + px * 0.3): int(x + px * 0.7) + 1].mean()
            if m > 0.4: continue
            col = np.median(c, 0); d = ((pal - col) ** 2).sum(1); cell[j, i] = (*pal[d.argmin()], 255)
    a = cell[..., 3] > 0; lb, nn = nd.label(a, structure=np.ones((3, 3)))
    if nn > 1:
        sz = nd.sum(a, lb, range(1, nn + 1)); keep = 1 + int(np.argmax(sz)); cell[lb != keep] = 0
    ys_, xs_ = np.where(cell[..., 3] > 0); cell = cell[ys_.min(): ys_.max() + 1, xs_.min(): xs_.max() + 1]
    Image.fromarray(cell).save(os.path.join(out, f"{name}.png")); print(name, "period %.2f/%.2f" % (px, py), "cells", cell.shape[1], "x", cell.shape[0])
