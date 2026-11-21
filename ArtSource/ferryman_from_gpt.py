# 2026-11-21: Yan's ChatGPT Ferryman (boss_concepts/gpt/ferryman_gpt_raw.jpg, ~8 screen px per art pixel, JPEG) -> a clean game sprite:
# key the flat grey background out (flood from the border), sample each 8x8 art cell by its median, reduce to a small palette, hard alpha.
# Writes boss_concepts/gpt/ferryman_native.png (the art pixel grid) and bosses_v3/ferryman_v2/stand.png (on the Pixel Lab canvas, feet 6 px off the bottom).
import os, numpy as np
from PIL import Image
from collections import deque
H = os.path.dirname(os.path.abspath(__file__)); G = os.path.join(H, "mutants_anim", "boss_concepts", "gpt")
P, OX, OY = 8, 7, 7
a = np.array(Image.open(os.path.join(G, "ferryman_gpt_raw.jpg")).convert("RGB")).astype(int)
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
cw, ch = (w - OX) // P, (h - OY) // P
out = np.zeros((ch, cw, 4), np.uint8)
for cy in range(ch):
    for cx in range(cw):
        blk = a[OY + cy * P + 2: OY + cy * P + P - 2, OX + cx * P + 2: OX + cx * P + P - 2]
        bgm = seen[OY + cy * P + 2: OY + cy * P + P - 2, OX + cx * P + 2: OX + cx * P + P - 2]
        if bgm.mean() > 0.5: continue
        out[cy, cx, :3] = np.median(blk[~bgm], 0); out[cy, cx, 3] = 255
im = Image.fromarray(out, "RGBA")
ys, xs = np.where(out[..., 3] > 0); im = im.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
# small palette (keeps the lantern green: quantize the opaque pixels only)
rgb = im.convert("RGB").quantize(colors=40, method=Image.MEDIANCUT, dither=Image.NONE).convert("RGB")
arr = np.array(rgb); src = np.array(im); al = src[..., 3]
glow = (src[..., 1].astype(int) > src[..., 0].astype(int) + 45) & (src[..., 1] > 120)    # the lantern's green: kept as drawn (the palette pass greyed it)
arr[glow] = src[glow][:, :3]; arr = np.dstack([arr, al]); arr[al == 0] = 0
im = Image.fromarray(arr.astype(np.uint8), "RGBA"); im.save(os.path.join(G, "ferryman_native.png")); print("native", im.size)
cv = im   # 2026-11-21: run_plan centres the stand on its canvas itself

os.makedirs(os.path.join(H, "mutants_anim", "bosses_v3", "ferryman_v2"), exist_ok=True); cv.save(os.path.join(H, "mutants_anim", "bosses_v3", "ferryman_v2", "stand.png")); print("canvas", cv.size)
