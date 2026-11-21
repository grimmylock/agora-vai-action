# 2026-11-21 (Yan: "the eye behind him is connected through a cord and moves around and fires as well"): the giant eye becomes its own creature (bosses_v3/specimen_eye), so it is cut out of
# every frame of the Specimen v2 clips (bosses_v3/specimen_v2/<clip>/orig -> <clip>/): the cream disc in the upper right of the body (also when tinted by the mind ring), with the iris and
# pupil blobs that touch it. The brain's own eyes do not touch the disc and stay.   python3 ArtSource/specimen_cut_eye.py   then  python3 ArtSource/export_boss_clips.py
import glob, os, shutil, numpy as np
from PIL import Image
from scipy import ndimage
D = os.path.join(os.path.dirname(os.path.abspath(__file__)), "mutants_anim", "bosses_v3", "specimen_v2")
def cut(a):
    h, w = a.shape[:2]; ys, xs = np.where(a[..., 3] > 0)
    if not len(xs): return a
    x0, x1, y0, y1 = xs.min(), xs.max(), ys.min(), ys.max()
    reg = np.zeros((h, w), bool); reg[y0:y0 + int((y1 - y0) * 0.45), x0 + int((x1 - x0) * 0.5):x1 + 1] = True
    r, g, b = a[..., 0], a[..., 1], a[..., 2]; op = a[..., 3] > 0
    cream = reg & op & (((r > 165) & (g > 150) & (b > 110) & (np.abs(r - g) < 60)) | ((r > 150) & (b > 150) & (g > 120)))
    lab, n = ndimage.label(ndimage.binary_closing(cream, iterations=2))
    if not n: return a
    sizes = ndimage.sum(cream, lab, range(1, n + 1)); k = int(np.argmax(sizes))
    if sizes[k] < 8: return a
    disc = ndimage.binary_fill_holes(lab == k + 1); touch = ndimage.binary_dilation(disc, iterations=2)
    iris = reg & op & (((b > r + 10) & (b > g + 20)) | ((r + g + b) < 120)); l2, n2 = ndimage.label(iris)
    kill = ndimage.binary_dilation(disc, iterations=1) & op & reg
    for j in range(1, n2 + 1):
        comp = l2 == j
        if (comp & touch).any() and comp.sum() < 400: kill |= comp
    a[kill] = 0; return a
for cd in sorted(glob.glob(os.path.join(D, "*", ""))):
    if os.path.basename(os.path.dirname(cd)) in ("orig",): continue
    od = os.path.join(cd, "orig")
    if not os.path.isdir(od): os.makedirs(od); [shutil.copy(f, od) for f in glob.glob(os.path.join(cd, "f_*.png"))]
    for f in sorted(glob.glob(os.path.join(od, "f_*.png"))):
        Image.fromarray(cut(np.array(Image.open(f).convert("RGBA")).astype(int)).astype(np.uint8)).save(os.path.join(cd, os.path.basename(f)))
    print(os.path.basename(os.path.dirname(cd)))
