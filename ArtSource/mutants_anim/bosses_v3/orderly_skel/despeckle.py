# drops floating specks (connected pieces under 25 px that touch nothing big) from the assembled Orderly attack frames
import glob, numpy as np
from PIL import Image
from scipy import ndimage
tot = 0
for c in ("saw_rush", "double_saw", "lash", "lamp_tear"):
    for f in sorted(glob.glob(f"../orderly/{c}/f_*.png")):
        a = np.array(Image.open(f).convert("RGBA")); m = a[..., 3] > 0
        lab, n = ndimage.label(ndimage.binary_dilation(m, iterations=2))   # pieces within 2 px belong together
        sizes = ndimage.sum(m, lab, range(1, n + 1)); big = max(sizes) if n else 0
        for i, sz in enumerate(sizes, 1):
            if sz < 25 and sz < big * 0.05: a[(lab == i) & m] = 0; tot += int(sz)
        Image.fromarray(a, "RGBA").save(f)
print("removed px", tot)
