# Batch 130b (Yan sent a reference per mutant: ArtSource/refs/mutants/): a reference-guided pass, like the weapon workflow.
# The creature is cut from the reference's flat background, cropped, and reduced to sprite size by MODE per block (the most
# common palette colour among the source pixels, a small bright feature - eyes, teeth, acid - wins if it covers enough of the
# block), on a hand-kept palette per mutant. Then the 1px #141617 outline. Hand fixes go on top (FIXES in make_mutants_v3.py).
import numpy as np
from PIL import Image

K = (20, 22, 23, 255)

def hexc(h, a=255):
    h = h.lstrip("#"); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)

def cutout(path, tol=22, margin=0, crop=None):
    """RGBA array of the creature: everything that differs from the corner colour by more than tol, largest blob kept.
    margin: pixels trimmed off every edge first (a frame round the reference)."""
    a = np.array(Image.open(path).convert("RGB")).astype(int)
    if crop: a = a[crop[1]:crop[3], crop[0]:crop[2]]   # (x0, y0, x1, y1): the panel the creature sits on
    if margin: a = a[margin:-margin, margin:-margin]
    bg = np.median(np.concatenate([a[:4, :4].reshape(-1, 3), a[-4:, -4:].reshape(-1, 3)]), axis=0)
    m = np.abs(a - bg).sum(2) > tol
    # keep the biggest 4-connected component (drops stray specks, the reference's watermark corners)
    lab = np.zeros(m.shape, int); best, bestn, n = 0, 0, 0
    for y, x in zip(*np.nonzero(m)):
        if lab[y, x]: continue
        n += 1; stack = [(y, x)]; lab[y, x] = n; cnt = 0
        while stack:
            cy, cx = stack.pop(); cnt += 1
            for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                ny, nx = cy + dy, cx + dx
                if 0 <= ny < m.shape[0] and 0 <= nx < m.shape[1] and m[ny, nx] and not lab[ny, nx]: lab[ny, nx] = n; stack.append((ny, nx))
        if cnt > bestn: best, bestn = n, cnt
    keep = lab == best
    # close small holes inside the body (dark shading that matched the background)
    ys, xs = np.nonzero(keep)
    for y in range(ys.min(), ys.max() + 1):
        row = np.nonzero(keep[y])[0]
        if len(row) > 1:
            gaps = np.diff(row)
            for i, g in enumerate(gaps):
                if 1 < g <= 6: keep[y, row[i]:row[i + 1]] = True
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    return a[y0:y1, x0:x1], keep[y0:y1, x0:x1]

def kmeans_palette(rgb, mask, k=6, seed=1):
    px = rgb[mask].astype(float); rng = np.random.default_rng(seed)
    c = px[rng.choice(len(px), k, replace=False)]
    for _ in range(25):
        d = ((px[:, None, :] - c[None]) ** 2).sum(2); l = d.argmin(1)
        c = np.array([px[l == i].mean(0) if (l == i).any() else c[i] for i in range(k)])
    return ["#%02x%02x%02x" % tuple(int(v) for v in cc) for cc in sorted(c, key=lambda v: v.sum())]

def reduce(rgb, mask, w, h, pal, accents=(), accent_share=0.2, fill=0.45):
    """Mode per block on `pal` (list of hex). accents: hex colours that win a block where they cover >= accent_share."""
    P = np.array([hexc(c)[:3] for c in pal], float)
    H, W = mask.shape
    sx, sy = W / w, H / h
    idx = ((rgb[:, :, None, :] - P[None, None]) ** 2).sum(3).argmin(2)
    acc = [pal.index(a) for a in accents]
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0)); o = out.load()
    for y in range(h):
        for x in range(w):
            ya, yb, xa, xb = int(y * sy), max(int(y * sy) + 1, int((y + 1) * sy)), int(x * sx), max(int(x * sx) + 1, int((x + 1) * sx))
            mm = mask[ya:yb, xa:xb]
            if mm.mean() < fill: continue
            ii = idx[ya:yb, xa:xb][mm]
            counts = np.bincount(ii, minlength=len(pal))
            pick = counts.argmax()
            for a in acc:
                if counts[a] >= accent_share * len(ii): pick = a
            o[x, y] = hexc(pal[pick])
    return out

def outline(im):
    out = im.copy(); a = im.load(); o = out.load(); w, h = im.size
    for y in range(h):
        for x in range(w):
            if not a[x, y][3] and any(0 <= x + dx < w and 0 <= y + dy < h and a[x + dx, y + dy][3] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                o[x, y] = K
    return out

def fit(rgb, mask, cw, ch):
    """Sprite size (w, h) that keeps the reference's aspect inside a cw x ch canvas, 1px margin for the outline."""
    H, W = mask.shape; s = min((cw - 2) / W, (ch - 2) / H)
    return max(1, round(W * s)), max(1, round(H * s))

# ------------------------------------------------------------------ Batch 130c: double resolution, like the weapons' in-hand sprites
# (Yan: "can't we draw them bigger and reduce their size similar to what we did with the guns?"). Mutant sprites are drawn at
# 2x (48x32 hounds, 64x48 Dread Prowler, 64x64 Blight Brute) and shown at 32 px per tile, so they cover the same ground as before
# with four times the pixels.
def sat(c): return max(c) - min(c)

def palette(rgb, mask, k=6, accents=2, sat_min=70):
    """k main tones from the low-saturation body, plus `accents` bright tones (eyes, acid, crystals) from saturated pixels."""
    px = rgb[mask]; s = px.max(1) - px.min(1)
    body = np.zeros(mask.shape, bool); body[mask] = s < sat_min
    acc = np.zeros(mask.shape, bool); acc[mask] = s >= sat_min
    main = kmeans_palette(rgb, body, k)
    extra = kmeans_palette(rgb, acc, accents) if acc.sum() >= accents * 6 else []
    return main, extra

def despeckle(im, keep=()):
    """A lone pixel whose 4 neighbours are all another single colour becomes that colour (accents and the outline stay)."""
    a = im.load(); w, h = im.size; out = im.copy(); o = out.load()
    for y in range(1, h - 1):
        for x in range(1, w - 1):
            c = a[x, y]
            if not c[3] or c[:3] in keep: continue
            n = [a[x + 1, y], a[x - 1, y], a[x, y + 1], a[x, y - 1]]
            if all(v[3] for v in n) and all(v == n[0] for v in n) and n[0] != c and n[0][:3] != K[:3]: o[x, y] = n[0]
    return out

def auto(path, cw, ch, k=6, accents=2, tol=22, margin=0, accent_share=0.15, crop=None, sat_min=70, do_outline=True):
    rgb, mask = cutout(path, tol, margin, crop)
    main, extra = palette(rgb, mask, k, accents, sat_min)
    w, h = fit(rgb, mask, cw, ch)
    im = reduce(rgb, mask, w, h, main + extra, accents=extra, accent_share=accent_share)
    im = despeckle(im, keep={hexc(c)[:3] for c in extra})
    if do_outline: im = outline(im)   # off for animation: each frame is outlined after its parts move (make_mutant_anims.py)
    canvas = Image.new("RGBA", (cw, ch), (0, 0, 0, 0)); canvas.alpha_composite(im, ((cw - w) // 2, ch - h))
    return canvas, main, extra
