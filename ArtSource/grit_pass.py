"""A light grit pass for sprites that look cleaner than the world (Yan, 2026-11-09: the eel and brain "look weirdly HD"; on a real render they are cleaner, smoother
than the speckled ground and foliage). Deterministic: scatters single-pixel light and dark speckles (a bit more dark than light) over the opaque, non-outline pixels, a few
neighbours at a time, never touching the outline or the transparent edge, so silhouettes and the artist's own details stay. Usage:
    python3 ArtSource/grit_pass.py in.png out.png [amount 0.24] [seed 1]"""
import sys, random
from PIL import Image

OUT_MAX = 40            # pixels this dark are outline: leave them
HI_MIN = 190            # and these bright ones are eyes, teeth, glints: leave them too

def lum(p): return 0.3 * p[0] + 0.59 * p[1] + 0.11 * p[2]

def grit(im, amount=0.24, seed=1):
    im = im.convert("RGBA"); px = im.load(); w, h = im.size; rnd = random.Random(seed); out = im.copy(); po = out.load()
    def solid(x, y): return 0 <= x < w and 0 <= y < h and px[x, y][3] > 200
    for y in range(h):
        for x in range(w):
            p = px[x, y]
            if p[3] < 200 or lum(p) < OUT_MAX or lum(p) > HI_MIN: continue
            if not all(solid(x + dx, y + dy) and lum(px[x + dx, y + dy]) >= OUT_MAX for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))): continue     # keep the rim
            if rnd.random() < amount:
                f = rnd.choice((0.80, 0.80, 0.86, 1.12, 1.16))                                          # mostly darker, some lighter
                po[x, y] = tuple(max(0, min(255, int(v * f))) for v in p[:3]) + (p[3],)
                if rnd.random() < 0.35:                                                                # a speckle sometimes takes a neighbour with it
                    dx, dy = rnd.choice(((1, 0), (0, 1)))
                    if solid(x + dx, y + dy) and lum(px[x + dx, y + dy]) >= OUT_MAX: po[x + dx, y + dy] = tuple(max(0, min(255, int(v * f))) for v in px[x + dx, y + dy][:3]) + (255,)
    return out

if __name__ == "__main__":
    a = sys.argv; grit(Image.open(a[1]), float(a[3]) if len(a) > 3 else 0.24, int(a[4]) if len(a) > 4 else 1).save(a[2])
