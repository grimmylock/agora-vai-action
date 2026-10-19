# Concept "Zone" grade: overcast, sickly, desaturated - what a post-process pass in Unity would do.
import sys, colorsys
import numpy as np
from PIL import Image
def grade(im, sat=0.55, dry=0.35, vig=0.6, grain=5, seed=1):
    a = np.asarray(im.convert("RGB")).astype(np.float32) / 255
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    # dry the greens toward olive/straw: pull green down, red up where green dominates
    gd = np.clip(g - np.maximum(r, b), 0, 1)
    r = r + gd * dry * 0.9; g = g - gd * dry * 0.25
    lum = 0.3 * r + 0.59 * g + 0.11 * b
    r, g, b = [lum + (c - lum) * sat for c in (r, g, b)]
    # split tone: cool grey-green shadows, warm dusty highlights
    sh = np.clip(1 - lum * 2, 0, 1)[..., None]; hi = np.clip(lum * 2 - 1, 0, 1)[..., None]
    rgb = np.stack([r, g, b], -1)
    rgb = rgb + sh * (np.array([-0.02, 0.0, 0.01])) + hi * np.array([0.05, 0.03, -0.03])
    rgb = 0.035 + rgb * 0.93                                  # lifted blacks, softer whites = overcast
    rgb = rgb * 0.94 + np.array([0.62, 0.6, 0.52]) * 0.06    # haze
    H, W = lum.shape
    yy, xx = np.mgrid[0:H, 0:W]; d = np.sqrt(((xx - W / 2) / (W / 2)) ** 2 + ((yy - H / 2) / (H / 2)) ** 2) / 1.414
    rgb = rgb * (1 - vig * np.clip(d - 0.35, 0, 1) ** 1.4)[..., None]
    rgb = rgb + np.random.RandomState(seed).normal(0, grain / 255, (H, W, 1))
    return Image.fromarray((np.clip(rgb, 0, 1) * 255).astype(np.uint8))
if __name__ == "__main__":
    grade(Image.open(sys.argv[1])).save(sys.argv[2]); print("ok")
