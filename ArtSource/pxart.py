# Small toolkit for hand-built pixel art in Python/PIL + numpy (never generated). A body part is a mask built from shapes (ellipses, polygons, tapering tubes), then shaded as
# VOLUME: a height field from the distance to the part's edge gives a surface normal, a light from the top-left picks one of a 5-colour ramp, with a 2x2 ordered dither on the band
# borders, a rim light on the lit edge and a darker rim on the far edge. Details (eyes, teeth, scars, spines) are placed pixel by pixel by the artist's script afterwards.
import math
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi

def rgb(h, a=255):
    h = h.lstrip("#"); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)

def ramp(*hexes): return [rgb(h) for h in hexes]          # dark -> light

BAYER = np.array([[0, 2], [3, 1]]) / 4.0

class Mask:
    def __init__(self, size):
        self.size = size; self.im = Image.new("L", (size[0] * 4, size[1] * 4), 0); self.d = ImageDraw.Draw(self.im)   # drawn at 4x then thresholded: smooth silhouettes, pixel-snapped
    def ell(self, cx, cy, rx, ry, rot=0.0):
        if rot:
            pts = [(cx + rx * math.cos(t) * math.cos(math.radians(rot)) - ry * math.sin(t) * math.sin(math.radians(rot)),
                    cy + rx * math.cos(t) * math.sin(math.radians(rot)) + ry * math.sin(t) * math.cos(math.radians(rot))) for t in np.linspace(0, 2 * math.pi, 40)]
            self.d.polygon([(x * 4, y * 4) for x, y in pts], fill=255)
        else: self.d.ellipse([(cx - rx) * 4, (cy - ry) * 4, (cx + rx) * 4, (cy + ry) * 4], fill=255)
        return self
    def poly(self, pts): self.d.polygon([(x * 4, y * 4) for x, y in pts], fill=255); return self
    def tube(self, pts, widths):
        """a limb: a chain of points with a width at each (linear taper between)"""
        for (x0, y0), (x1, y1), w0, w1 in zip(pts, pts[1:], widths, widths[1:]):
            n = int(max(abs(x1 - x0), abs(y1 - y0), 1) * 6)
            for i in range(n + 1):
                u = i / n; x, y, w = x0 + (x1 - x0) * u, y0 + (y1 - y0) * u, w0 + (w1 - w0) * u
                self.d.ellipse([(x - w / 2) * 4, (y - w / 2) * 4, (x + w / 2) * 4, (y + w / 2) * 4], fill=255)
        return self
    def cut(self, other):                                                   # carve another mask out of this one
        a = np.array(self.im); b = np.array(other.im); a[b > 0] = 0; self.im = Image.fromarray(a); self.d = ImageDraw.Draw(self.im); return self
    def bits(self):
        small = self.im.resize(self.size, Image.BOX); return np.array(small) >= 128

def shade(mask, colours, light=(-0.55, -0.75), depth=2.6, tex=0.0, seed=0, rim=True, bias=0.0, gain=1.0):
    """-> RGBA image of the part. colours: dark..light ramp (5 is best)."""
    m = mask.bits() if isinstance(mask, Mask) else mask
    H, W = m.shape; out = np.zeros((H, W, 4), np.uint8)
    if not m.any(): return Image.fromarray(out)
    dist = ndi.distance_transform_edt(np.pad(m, 1))[1:-1, 1:-1]
    h = np.minimum(dist, depth) / depth                                     # 0 at the edge .. 1 in the middle: a dome
    h = ndi.gaussian_filter(h, 0.6)
    gy, gx = np.gradient(h)
    nz = 0.55; nx, ny = -gx * 2.2, -gy * 2.2; ln = np.sqrt(nx * nx + ny * ny + nz * nz)
    lx, ly = light; ll = math.sqrt(lx * lx + ly * ly + 0.6 * 0.6)
    lam = (nx * lx + ny * ly + nz * 0.6) / (ln * ll)                         # -1..1
    v = np.clip((lam + 0.25) * gain + bias, 0, 1)
    if tex:
        rng = np.random.RandomState(seed); n = ndi.gaussian_filter(rng.randn(H, W), 0.8); n /= (np.abs(n).max() + 1e-6); v = np.clip(v + tex * n, 0, 1)
    k = len(colours) - 1
    yy, xx = np.mgrid[0:H, 0:W]; th = BAYER[yy % 2, xx % 2] * 0.5 - 0.25      # ordered dither at the band borders
    idx = np.clip(np.floor(v * k + 0.5 + th * 0.9), 0, k).astype(int)
    if rim:
        edge = m & ~ndi.binary_erosion(m)
        lit = edge & ((ndi.shift(m.astype(float), (1, 0), order=0) < 0.5) | (ndi.shift(m.astype(float), (0, 1), order=0) < 0.5))   # the edge whose upper/left neighbour is empty
        far = edge & ~lit
        idx[lit] = np.minimum(k, idx[lit] + 1); idx[far] = np.maximum(0, idx[far] - 1)
    for i, c in enumerate(colours): out[(idx == i) & m] = c
    return Image.fromarray(out, "RGBA")

def paint(size, layers):
    im = Image.new("RGBA", size, (0, 0, 0, 0))
    for l in layers: im.alpha_composite(l)
    return im

def px(im, pts, col):
    for x, y in pts:
        if 0 <= x < im.width and 0 <= y < im.height: im.putpixel((int(x), int(y)), col)

def line(im, x0, y0, x1, y1, col):
    n = int(max(abs(x1 - x0), abs(y1 - y0), 1))
    for i in range(n + 1): px(im, [(round(x0 + (x1 - x0) * i / n), round(y0 + (y1 - y0) * i / n))], col)

def outline(im, col=(20, 22, 23, 255)):
    a = np.array(im)[:, :, 3] > 0; grown = ndi.binary_dilation(a, structure=np.array([[0, 1, 0], [1, 1, 1], [0, 1, 0]])) & ~a
    out = np.array(im).copy(); out[grown] = col; return Image.fromarray(out, "RGBA")

def crop(im, pad=1):
    bb = im.getbbox(); return im.crop((max(0, bb[0] - pad), max(0, bb[1] - pad), min(im.width, bb[2] + pad), min(im.height, bb[3] + pad)))
