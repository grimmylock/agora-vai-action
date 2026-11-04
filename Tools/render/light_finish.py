"""The Octopath-ish finish on a lit render (CPU preview): bloom (bright areas bleed light), a tilt-shift depth of field (sharp in a
band across the middle, soft toward the top and bottom edges), a slight warm/cool grade and a vignette. Usage: lightfinish.py in out"""
import sys, numpy as np
from PIL import Image, ImageFilter
src = Image.open(sys.argv[1]).convert('RGB'); a = np.asarray(src).astype(np.float32) / 255
# bloom: threshold the bright parts, blur wide, add back
lum = a.max(axis=2)
bright = np.clip((a - 0.8) * 3.0, 0, 1) * (lum > 0.8)[..., None]
bl = Image.fromarray((bright * 255).astype(np.uint8))
b1 = np.asarray(bl.filter(ImageFilter.GaussianBlur(5))).astype(np.float32) / 255
b2 = np.asarray(bl.filter(ImageFilter.GaussianBlur(16))).astype(np.float32) / 255
out = np.clip(a + b1 * 0.9 + b2 * 0.7, 0, 1)
# tilt-shift: blend toward a blurred copy by distance from the centre row
h = out.shape[0]
blur = np.asarray(Image.fromarray((out * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(3.2))).astype(np.float32) / 255
y = np.abs((np.arange(h) + 0.5) / h - 0.5) * 2
k = np.clip((y - 0.38) / 0.55, 0, 1)[:, None, None] ** 1.5
out = out * (1 - k) + blur * k
# grade: a little contrast, cool shadows
out = np.clip((out - 0.5) * 1.08 + 0.5 + np.array([-0.005, 0.0, 0.012]), 0, 1)
# vignette
x = (np.arange(out.shape[1]) + 0.5) / out.shape[1] - 0.5
v = 1 - 0.38 * (x[None, :] ** 2 * 4 + ((np.arange(h) + 0.5) / h - 0.5)[:, None] ** 2 * 4) / 2
out *= v[..., None]
Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8)).save(sys.argv[2])
