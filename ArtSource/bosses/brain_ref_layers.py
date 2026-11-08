"""Splits Yan's reworked brain-in-the-vat picture (ArtSource/refs/brain/yan_brain_vat_ref.png, 366x363, a smooth painted render) into the layers
make_brain_vat_v4.py needs to rebuild every state of the boss from it:
    shell   - the vat, lid, base, pump, rack and cables WITHOUT the glass contents (transparent where the picture's dark backdrop was)
    liquid  - the vat's green contents with the brain, its wires and its bubbles painted out (filled row by row from the clean liquid either side)
    brain   - the brain and its cord cut out (RGBA), with the two eyes' centres and radii
All in the picture's own pixels (the generator scales them). Run it on its own to write the layers and a debug sheet to _out/brain_layers/."""
import os
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

HERE = os.path.dirname(os.path.abspath(__file__))
REF = os.path.join(HERE, '..', 'refs', 'brain', 'yan_brain_vat_ref.png')
BOX = (56, 0, 326, 342)                       # the art inside the picture (x0, y0, x1, y1)
GLASS = (127, 82, 250, 282)                   # the glass interior in the picture (x0, y0, x1, y1); the outer 4 columns are the glass's reflections
EDGE_L, EDGE_R = 5, 4


def load():
    a = np.array(Image.open(REF).convert('RGB')).astype(int)
    return a


def art_alpha(a):
    """Transparent = the dark backdrop outside the vat (connected to the border and dark)."""
    dark = a.max(2) < 33
    lab, n = ndi.label(dark)
    border = np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))
    bg = np.isin(lab, border[border != 0])
    return ~bg


def layers():
    a = load(); H, W = a.shape[:2]
    alpha = art_alpha(a)
    gx0, gy0, gx1, gy1 = GLASS
    inner = np.zeros((H, W), bool); inner[gy0:gy1, gx0 + EDGE_L:gx1 - EDGE_R] = True
    r, g, b = a[:, :, 0], a[:, :, 1], a[:, :, 2]
    liquidish = (g > r + 12) & (g >= b)                              # the green water
    obj = inner & ~liquidish                                          # brain, wires, bubble glints, anything not green
    obj = ndi.binary_opening(obj, iterations=1)
    lab, n = ndi.label(obj)
    keep = np.zeros_like(obj)
    for i in range(1, n + 1):
        m = lab == i
        if m.sum() >= 120: keep |= m                                  # the brain and the wires; bubbles are small
    core = ndi.binary_closing(keep, iterations=2)
    keep = ndi.binary_dilation(core, iterations=2) & inner            # the outline round them (for painting them out of the liquid)
    # the liquid with those painted out: each row filled between the clean liquid on its left and right
    liq = a.copy()
    for y in range(gy0, gy1):
        row = keep[y]
        if not row.any(): continue
        xs = np.where(inner[y])[0]; x0, x1 = xs[0], xs[-1]
        x = x0
        while x <= x1:
            if row[x]:
                s = x
                while x <= x1 and row[x]: x += 1
                e = x - 1
                lc = liq[y, s - 1] if s - 1 >= x0 else liq[y, e + 1]
                rc = liq[y, e + 1] if e + 1 <= x1 else lc
                for xx in range(s, e + 1):
                    t = (xx - s + 1) / (e - s + 2); liq[y, xx] = (np.array(lc) * (1 - t) + np.array(rc) * t).round()
            else: x += 1
    # smooth the filled areas a little so no horizontal streaks show, then copy
    sm = np.stack([ndi.gaussian_filter(liq[:, :, c].astype(float), 1.2) for c in range(3)], 2)
    fill = ndi.binary_dilation(keep, iterations=2) & inner
    liq = np.where(fill[:, :, None], sm, liq).round().astype(int)
    # the brain: the part of the cut-out below the wires' attachment (y >= BRAIN_TOP), plus its cord
    BRAIN_TOP = 142
    bm = core.copy(); bm[:BRAIN_TOP] = False
    bm = ndi.binary_fill_holes(bm)                                     # the eyes' whites are not 'non-green' (they are white): they are inside the hull
    lab2, n2 = ndi.label(bm); sizes = ndi.sum(bm, lab2, range(1, n2 + 1)); bm = lab2 == (1 + int(np.argmax(sizes)))
    bm = ndi.binary_fill_holes(bm)
    rim = ndi.binary_dilation(bm, iterations=2) & (a.max(2) < 95) & ~liquidish & inner     # only the dark outline round the brain, never the green water next to it
    bm = bm | rim
    wires = keep & ~bm
    return a, alpha, inner, keep, liq, bm, wires


def shell_of(a, alpha, inner, liq):
    """The vat without its contents: the interior is left transparent (the generator fills it), the glass reflections stay."""
    out = np.zeros(a.shape[:2] + (4,), np.uint8)
    out[:, :, :3] = a; out[:, :, 3] = np.where(alpha, 255, 0)
    out[inner, 3] = 0
    return out


if __name__ == '__main__':
    a, alpha, inner, keep, liq, bm, wires = layers()
    od = os.path.join(HERE, '_out', 'brain_layers'); os.makedirs(od, exist_ok=True)
    Image.fromarray(shell_of(a, alpha, inner, liq), 'RGBA').crop(BOX).save(os.path.join(od, 'shell.png'))
    Image.fromarray(liq.astype(np.uint8)).crop(BOX).save(os.path.join(od, 'liquid_clean.png'))
    br = np.zeros(a.shape[:2] + (4,), np.uint8); br[:, :, :3] = a; br[:, :, 3] = np.where(bm, 255, 0)
    Image.fromarray(br, 'RGBA').crop(BOX).save(os.path.join(od, 'brain.png'))
    wi = np.zeros(a.shape[:2] + (4,), np.uint8); wi[:, :, :3] = a; wi[:, :, 3] = np.where(wires, 255, 0)
    Image.fromarray(wi, 'RGBA').crop(BOX).save(os.path.join(od, 'wires.png'))
    # a debug sheet: the picture | shell | liquid without the brain | brain | wires, on a grey checker
    def on_bg(im):
        bg = Image.new('RGBA', im.size, (90, 90, 96, 255)); bg.alpha_composite(im); return bg
    parts = [Image.open(os.path.join(od, n)).convert('RGBA') for n in ('shell.png', 'liquid_clean.png', 'brain.png', 'wires.png')]
    ref = Image.open(REF).convert('RGBA').crop(BOX)
    sheet = Image.new('RGBA', (ref.width * 5 + 40, ref.height), (40, 40, 44, 255))
    for i, p in enumerate([ref] + parts): sheet.alpha_composite(on_bg(p), (i * (ref.width + 10), 0))
    sheet.save(os.path.join(od, 'debug.png'))
    print('brain mask bbox', np.where(bm.any(1))[0][[0, -1]], np.where(bm.any(0))[0][[0, -1]])


