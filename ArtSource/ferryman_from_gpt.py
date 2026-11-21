# 2026-11-21: Yan's ChatGPT boss drawing (python3 ferryman_from_gpt.py <name> <px> <offx> <offy>: ferryman 8 7 7, orderly 10 9 5) (boss_concepts/gpt/" + NAME + "_gpt_raw.jpg, ~8 screen px per art pixel, JPEG) -> a clean game sprite:
# key the flat grey background out (flood from the border), sample each 8x8 art cell by its median, reduce to a small palette, hard alpha.
# Writes boss_concepts/gpt/" + NAME + "_native.png (the art pixel grid) and bosses_v3/<name>_v2/stand.png (on the Pixel Lab canvas, feet 6 px off the bottom).
import os, numpy as np
from PIL import Image
from collections import deque
H = os.path.dirname(os.path.abspath(__file__)); G = os.path.join(H, "mutants_anim", "boss_concepts", "gpt")
import sys
NAME = sys.argv[1] if len(sys.argv) > 1 else "ferryman"; P, OX, OY = (float(v) for v in (sys.argv[2:5] if len(sys.argv) > 4 else (8, 7, 7)))   # the period may be fractional (grinder 9.63, choir 8.33: the drawings were scaled by a non-integer)
a = np.array(Image.open(os.path.join(G, "" + NAME + "_gpt_raw.jpg")).convert("RGB")).astype(int)
if os.environ.get("BLANK"):   # "x0,y0,x1,y1;..." in raw px: painted over with the background (a caption, a scale figure)
    for box in os.environ["BLANK"].split(";"):
        x0, y0, x1, y1 = (int(v) for v in box.split(",")); a[y0:y1, x0:x1] = (55, 54, 60)
h, w, _ = a.shape; bg = np.array([55, 54, 60])
near = np.abs(a - bg).sum(2) < 30
seen = np.zeros((h, w), bool); q = deque()
for x in range(w):
    for y in (0, h - 1):
        if near[y, x]: seen[y, x] = True; q.append((y, x))
for y in range(h):
    for x in (0, w - 1):
        if near[y, x] and not seen[y, x]: seen[y, x] = True; q.append((y, x))
while q:
    y, x = q.popleft()
    for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        ny, nx = y + dy, x + dx
        if 0 <= ny < h and 0 <= nx < w and not seen[ny, nx] and near[ny, nx]: seen[ny, nx] = True; q.append((ny, nx))
from scipy import ndimage   # enclosed background (between a saw and the torso) is not reached by the border flood: drop every large flat-background patch too
lab, nl = ndimage.label(near); sizes = ndimage.sum(near, lab, range(1, nl + 1))
for i, sz in enumerate(sizes, 1):
    if sz > 1500: seen |= lab == i
cw, ch = int((w - OX) // P), int((h - OY) // P)
out = np.zeros((ch, cw, 4), np.uint8)
for cy in range(ch):
    for cx in range(cw):
        y0, y1 = int(OY + cy * P + P * 0.25), int(OY + cy * P + P * 0.75) + 1; x0, x1 = int(OX + cx * P + P * 0.25), int(OX + cx * P + P * 0.75) + 1
        blk = a[y0:y1, x0:x1]; bgm = seen[y0:y1, x0:x1]
        if bgm.mean() > 0.5: continue
        out[cy, cx, :3] = np.median(blk[~bgm], 0); out[cy, cx, 3] = 255
lab2, n2 = ndimage.label(out[..., 3] > 0, structure=np.ones((3, 3)))   # dust specks and stray pixels: drop pieces under 8 art pixels
for i in range(1, n2 + 1):
    if (lab2 == i).sum() < 8: out[lab2 == i] = 0
im = Image.fromarray(out, "RGBA")
ys, xs = np.where(out[..., 3] > 0); im = im.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
# small palette (keeps the lantern green: quantize the opaque pixels only)
rgb = im.convert("RGB").quantize(colors=40, method=Image.MEDIANCUT, dither=Image.NONE).convert("RGB")
arr = np.array(rgb); src = np.array(im); al = src[..., 3]
glow = (src[..., 1].astype(int) > src[..., 0].astype(int) + 45) & (src[..., 1] > 120)    # the lantern's green: kept as drawn (the palette pass greyed it)
arr[glow] = src[glow][:, :3]; arr = np.dstack([arr, al]); arr[al == 0] = 0
im = Image.fromarray(arr.astype(np.uint8), "RGBA"); im.save(os.path.join(G, "" + NAME + "_native.png")); print("native", im.size)
cv = im   # 2026-11-21: run_plan centres the stand on its canvas itself

os.makedirs(os.path.join(H, "mutants_anim", "bosses_v3", NAME + "_v2"), exist_ok=True); cv.save(os.path.join(H, "mutants_anim", "bosses_v3", NAME + "_v2", "stand.png")); print("canvas", cv.size)
