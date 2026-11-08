# material trace: each output pixel = the most common material under it (nearest of the given colours; '.' = background)
import sys, numpy as np
from PIL import Image
f, L, flip = sys.argv[1], int(sys.argv[2]), sys.argv[3] == "1"
mats = [kv.split("=") for kv in sys.argv[4].split(",")]
im = Image.open(f).convert("RGB"); a = np.asarray(im).astype(int)
bg = np.median(np.concatenate([a[0], a[-1]]), axis=0)
ys, xs = np.where(np.abs(a - bg).sum(axis=2) > 120)
a = a[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
if flip: a = a[:, ::-1]
names = ["."] + [k for k, _ in mats]
cols = np.array([bg] + [[int(h[i:i + 2], 16) for i in (1, 3, 5)] for _, h in mats])
lab = np.argmin(((a[:, :, None, :] - cols[None, None]) ** 2).sum(-1), axis=2)
s = a.shape[1] / L; H = int(round(a.shape[0] / s))
for j in range(H):
    row = ""
    for i in range(L):
        blk = lab[int(j * s):max(int(j * s) + 1, int((j + 1) * s)), int(i * s):max(int(i * s) + 1, int((i + 1) * s))].ravel()
        c = np.bincount(blk, minlength=len(names)); 
        row += "." if c[0] > 0.5 * blk.size else names[int(np.argmax(c[1:])) + 1]
    print('"%s",' % row)
