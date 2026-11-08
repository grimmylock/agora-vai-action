"""Yan's eel (ArtSource/refs/eel/yan_eel_reference.jpg, 2752x1536, 2026-11-05: "a higher quality image of the eel boss, just like the
ones we used for the mutants") recovered to the pixel grid it was drawn on: 5.73 source px per art pixel in both directions (found by
the periodicity of the edges), so 480x268 art pixels. One art pixel = one game pixel (32 px a tile, like the HD mutants): no resampling
blur. Each cell takes the median of its centre 3x3 source px; the colours are snapped to a flat palette (the JPEG noise goes), the
silhouette is the flood-filled background's complement. The reference may be AI-generated (its file name says Gemini): per Batch 130c
Yan supplied it as the source and the mutants were reduced the same way (his call, rule 2 of CLAUDE.md).
    python3 ArtSource/bosses/eel_native.py   -> ArtSource/refs/eel/eel_native.png (RGBA 480x268) + maps/renders/boss_eel/eel_native_review.png
"""
import os, numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.join(HERE, "..", "..")
SRC = os.path.join(ROOT, "ArtSource", "refs", "eel", "yan_eel_reference.jpg")
P = 5.73; OX, OY = 3.12, 2.59                       # cell size and the first cell centre (x, y), measured
COLORS = 48; MINCELLS = 5

def load():
    im = Image.open(SRC).convert("RGB"); W, H = im.size
    work = im.copy(); MARK = (255, 0, 255)
    for seed in ((2, 2), (W - 3, 2), (2, H - 3), (W - 3, H - 3)): ImageDraw.floodfill(work, seed, MARK, thresh=22)
    eel = ~(np.array(work) == np.array(MARK)).all(2)
    return np.array(im).astype(np.int16), eel

def native():
    a, eel = load(); H, W, _ = a.shape
    nx, ny = int((W - OX) / P), int((H - OY) / P)
    rgb = np.zeros((ny, nx, 3), np.uint8); alpha = np.zeros((ny, nx), bool)
    for j in range(ny):
        y = int(round(OY + j * P))
        for i in range(nx):
            x = int(round(OX + i * P))
            blk = a[max(0, y - 1):y + 2, max(0, x - 1):x + 2].reshape(-1, 3)
            rgb[j, i] = np.median(blk, axis=0)
            alpha[j, i] = eel[max(0, y - 1):y + 2, max(0, x - 1):x + 2].mean() > 0.5
    # snap to a flat palette that keeps the rare colours (the teeth, the eye, the spots, the mouth): the next colour is always the one
    # farthest from those chosen (among colours at least MINCELLS cells use), so a median cut can't flatten them into the greens
    flat = rgb[alpha].astype(np.int32); q = (flat // 4) * 4 + 2
    cols, cnt = np.unique(q, axis=0, return_counts=True)
    keep = cols[cnt >= MINCELLS]; kc = cnt[cnt >= MINCELLS]
    pal = [keep[np.argmax(kc)]]
    while len(pal) < COLORS:
        d = np.min(np.linalg.norm(keep[:, None, :] - np.array(pal)[None], axis=2), axis=1)
        if d.max() < 15: break
        pal.append(keep[np.argmax(d)])
    pal = np.array(pal); full = rgb.astype(np.int32).reshape(-1, 3)
    idx = np.argmin(np.linalg.norm(full[:, None, :] - pal[None], axis=2), axis=1)
    snapped = pal[idx].reshape(rgb.shape).astype(np.uint8)
    out = np.dstack([snapped, (alpha * 255).astype(np.uint8)])
    # specks: a lone opaque cell with no opaque 4-neighbour goes; a lone transparent hole inside goes (takes its neighbours' colour)
    al = out[:, :, 3] > 0
    nb = np.zeros(al.shape, int); nb[1:] += al[:-1]; nb[:-1] += al[1:]; nb[:, 1:] += al[:, :-1]; nb[:, :-1] += al[:, 1:]
    out[al & (nb == 0), 3] = 0
    hole = ~al & (nb == 4)
    for j, i in zip(*np.nonzero(hole)): out[j, i, :3] = out[j - 1, i, :3]; out[j, i, 3] = 255
    print("palette", len(pal))
    return Image.fromarray(out, "RGBA")

if __name__ == "__main__":
    n = native(); n.save(os.path.join(ROOT, "ArtSource", "refs", "eel", "eel_native.png"))
    cols = sorted(set(map(tuple, np.array(n).reshape(-1, 4)[np.array(n)[:, :, 3].reshape(-1) > 0][:, :3])))
    print("native", n.size, "colours", len(cols))
