# reference scaled so the weapon is L px long (handle left), drawn x10 with a pixel grid and labels every 5: for tracing by eye
import sys, numpy as np
from PIL import Image, ImageDraw
f, L, flip, out = sys.argv[1], int(sys.argv[2]), sys.argv[3] == "1", sys.argv[4]
im = Image.open(f).convert("RGB"); a = np.asarray(im).astype(int)
bg = np.median(np.concatenate([a[0], a[-1]]), axis=0); m = np.abs(a - bg).sum(axis=2) > 45
# drop the soft shadow: keep only pixels that differ strongly OR are dark-outlined region; crude: bbox of strong mask
ys, xs = np.where(np.abs(a - bg).sum(axis=2) > 120)
im = im.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
if flip: im = im.transpose(Image.FLIP_LEFT_RIGHT)
s = im.width / L; H = int(np.ceil(im.height / s))
sm = im.resize((L, H), Image.BOX)
Z = 10; big = sm.resize((L * Z, H * Z), Image.NEAREST); d = ImageDraw.Draw(big)
for x in range(L + 1): d.line([(x * Z, 0), (x * Z, H * Z)], fill=(255, 0, 255) if x % 5 == 0 else (90, 90, 90))
for y in range(H + 1): d.line([(0, y * Z), (L * Z, y * Z)], fill=(255, 0, 255) if y % 5 == 0 else (90, 90, 90))
canvas = Image.new("RGB", (L * Z + 30, H * Z + 30), (255, 255, 255)); canvas.paste(big, (30, 30)); d = ImageDraw.Draw(canvas)
for x in range(0, L, 5): d.text((30 + x * Z + 2, 10), str(x), fill=(0, 0, 0))
for y in range(0, H, 5): d.text((2, 30 + y * Z + 2), str(y), fill=(0, 0, 0))
canvas.save(out); print(L, H)
