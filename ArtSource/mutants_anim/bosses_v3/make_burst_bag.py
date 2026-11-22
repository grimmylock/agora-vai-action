# The Orderly's burst blood bag (2026-11-22): for every clip, a copy <clip>_b whose bag is torn open and dripping. The ORIGINAL pixels are edited (no regeneration): the bag is found per frame
# as the blood-red blob near the top of the pole, its lower right is ripped off in a jagged edge, the plastic rim stays, and a thread of blood hangs from the tear with a drop at the end.
# usage: python3 make_burst_bag.py   (needs orderly/<clip>/f_*.png; writes orderly/<clip>_b/)
import glob, os, shutil
import numpy as np
from PIL import Image
from scipy import ndimage
BASE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "orderly")
BLOOD = [(120, 18, 28), (84, 12, 22), (52, 8, 14)]
def find_bag(a):
    red = (a[..., 3] > 0) & (a[..., 0] - a[..., 1] > 35) & (a[..., 0] < 150) & (a[..., 1] < 70)
    red[75:] = False                                         # the bag hangs high, never below the pole's middle
    lab, n = ndimage.label(red, structure=np.ones((3, 3)))
    best = None
    for i in range(1, n + 1):
        ys, xs = np.where(lab == i)
        if len(ys) < 14: continue
        h = ys.max() - ys.min() + 1; w = xs.max() - xs.min() + 1
        if h > 22 or w > 14 or h < 6: continue
        if best is None or len(ys) > best[0]: best = (len(ys), xs.min(), xs.max(), ys.min(), ys.max())
    return best
def tear(im):
    a = np.array(im.convert("RGBA")).astype(int); b = find_bag(a)
    if b is None: return im, False
    _, x0, x1, y0, y1 = b; h = y1 - y0 + 1; w = x1 - x0 + 1
    rng = np.random.RandomState(x0 * 31 + y0)
    cut = y0 + max(3, h // 2)                                # the bag is half gone: below this line the right part is ripped off in a jagged edge
    for x in range(x0, x1 + 2):
        edge = cut + int(round(2.0 * np.sin((x - x0) * 1.9) + rng.randint(-1, 2))) + (x - x0) // 3
        for y in range(max(y0 - 1, edge), y1 + 3):
            if y < a.shape[0] and x < a.shape[1] and a[y, x, 3] > 0 and (a[y, x, 0] - a[y, x, 1] > 20 or y > y1 - 2 or x >= x1 - 1): a[y, x] = (0, 0, 0, 0)
    for k, y in enumerate(range(max(y0, cut - 2), y1 + 1)):  # what stays of the fill darkens: dried, dull
        for x in range(x0, x1 + 1):
            if a[y, x, 3] > 0 and a[y, x, 0] - a[y, x, 1] > 35: a[y, x, :3] = BLOOD[1]
    mid = x0 + w // 2 + 1; ty = cut + 2                      # the thread of blood from the tear, down to a drop; the line is two pixels wide at the top
    length = 9
    for k in range(length):
        y = ty + k; x = mid + (1 if k > 5 else 0)
        if y < a.shape[0]:
            a[y, x] = (*BLOOD[0], 255) if k < length - 2 else (*BLOOD[1], 255)
            if k < 3 and x + 1 < a.shape[1]: a[y, x + 1] = (*BLOOD[1], 255)
    if ty + length + 1 < a.shape[0]: a[ty + length + 1, mid + 1] = (*BLOOD[0], 255)   # the drop
    for k in range(3):                                        # a few specks on the ragged edge
        x = x0 + 1 + rng.randint(0, max(1, w - 2)); y = cut + rng.randint(1, 4)
        if a[y, x, 3] == 0: a[y, x] = (*BLOOD[2], 255)
    return Image.fromarray(a.astype(np.uint8), "RGBA"), True
if __name__ == "__main__":
    tot = miss = 0
    for cd in sorted(glob.glob(os.path.join(BASE, "*", ""))):
        clip = os.path.basename(os.path.dirname(cd))
        if clip.endswith("_b") or clip in ("drag", "overhead") or not glob.glob(cd + "f_*.png"): continue
        out = os.path.join(BASE, clip + "_b"); shutil.rmtree(out, ignore_errors=True); os.makedirs(out)
        for f in sorted(glob.glob(cd + "f_*.png")):
            im, ok = tear(Image.open(f)); im.save(os.path.join(out, os.path.basename(f))); tot += 1; miss += (not ok)
        print(clip, len(glob.glob(cd + "f_*.png")), "frames")
    print("frames", tot, "bag not found in", miss)
