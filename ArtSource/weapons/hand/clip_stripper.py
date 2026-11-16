"""Yan's own stripper-clip picture (2026-11-16, ArtSource/weapons/hand/clip_stripper_source.jpg: an upscaled pixel drawing, 7.51 x 7.60 screen px per drawn pixel): read back to its own pixels by sampling the centre of each block
(no resampling, no colour change), white background -> transparent, cropped, and MIRRORED so the bullets point right (the guns face right in the game; his drawing has them pointing left).
Writes Assets/Resources/Sprites/Weapons/clip_stripper.png. Run: python3 ArtSource/weapons/hand/clip_stripper.py"""
import os
import numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
im = np.array(Image.open(os.path.join(HERE, 'clip_stripper_source.jpg')).convert('RGB'))
H, W = im.shape[:2]; px, py, ox, oy = 7.51, 7.60, 6.0, 0.5
nx, ny = int((W - ox) // px), int((H - oy) // py)
out = np.zeros((ny, nx, 4), np.uint8)
for j in range(ny):
    for i in range(nx):
        x, y = int(ox + (i + 0.5) * px), int(oy + (j + 0.5) * py)
        c = im[y, x]
        out[j, i] = (*c, 0 if c.min() > 205 else 255)
ys, xs = np.where(out[..., 3] > 0); out = out[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
out = out[:, ::-1]                                                                                   # bullets forward (right)
Image.fromarray(out).save(os.path.join(ROOT, 'Assets/Resources/Sprites/Weapons/clip_stripper.png')); print('clip', out.shape[1], 'x', out.shape[0])
