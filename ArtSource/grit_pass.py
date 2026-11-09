"""A light grit pass for sprites that look cleaner than the world (Yan, 2026-11-09: the eel and brain "look weirdly HD"; on a real render they are cleaner and smoother than the
speckled ground and foliage). Single-pixel light and dark speckles (mostly dark) over the opaque, non-outline, non-bright pixels (never the outline, eyes, teeth, glints or the
transparent edge), so silhouettes and the artist's own details stay.

STABLE, not random: whether a pixel is speckled comes from a hash of its position inside its animation cell (`cell`), so every frame of a strip, and every damage overlay of the
same frame, gets the SAME speckle where the art is the same - nothing boils from frame to frame.
    python3 ArtSource/grit_pass.py in.png out.png [amount 0.24] [cell_w cell_h]
"""
import sys
from PIL import Image

OUT_MAX = 40            # pixels this dark are outline: leave them
HI_MIN = 190            # and these bright ones are eyes, teeth, glints: leave them too

def lum(p): return 0.3 * p[0] + 0.59 * p[1] + 0.11 * p[2]

def hsh(x, y, salt):
    n = (x * 374761393 + y * 668265263 + salt * 2147483647) & 0xFFFFFFFF
    n = (n ^ (n >> 13)) * 1274126177 & 0xFFFFFFFF
    return ((n ^ (n >> 16)) & 0xFFFF) / 65536.0

def grit(im, amount=0.24, cell=None):
    im = im.convert("RGBA"); px = im.load(); w, h = im.size; cw, ch = cell if cell else (w, h); out = im.copy(); po = out.load()
    def solid(x, y): return 0 <= x < w and 0 <= y < h and px[x, y][3] > 200 and OUT_MAX <= lum(px[x, y])
    FACTORS = (0.80, 0.80, 0.86, 1.12, 1.16)
    for y in range(h):
        for x in range(w):
            p = px[x, y]
            if p[3] < 200 or lum(p) < OUT_MAX or lum(p) > HI_MIN: continue
            if not all(solid(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))): continue          # keep the rim
            cx, cy = x % cw, y % ch
            if hsh(cx, cy, 1) < amount:
                f = FACTORS[int(hsh(cx, cy, 2) * len(FACTORS))]
                po[x, y] = tuple(max(0, min(255, int(v * f))) for v in p[:3]) + (p[3],)
                if hsh(cx, cy, 3) < 0.35:                                                                         # a speckle sometimes takes a neighbour with it
                    dx, dy = ((1, 0), (0, 1))[int(hsh(cx, cy, 4) * 2)]
                    if solid(x + dx, y + dy): po[x + dx, y + dy] = tuple(max(0, min(255, int(v * f))) for v in px[x + dx, y + dy][:3]) + (px[x + dx, y + dy][3],)
    return out

if __name__ == "__main__":
    a = sys.argv
    cell = (int(a[4]), int(a[5])) if len(a) > 5 else None
    grit(Image.open(a[1]), float(a[3]) if len(a) > 3 else 0.24, cell).save(a[2])
