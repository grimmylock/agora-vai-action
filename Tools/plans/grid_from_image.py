#!/usr/bin/env python3
"""First-draft plan grid from a top-down floor-plan picture (Gemini etc.), so the wall/door/window counting is done by a script, not by eye.
  python3 Tools/plans/grid_from_image.py IMAGE [--cols N --rows M] [--out DRAFT.txt] [--overlay OVERLAY.png]
It finds the thick dark wall bands, the interior box, cuts the interior into --cols x --rows squares (when the picture says '5 x 4 squares inside' give them; without them it
estimates the square size from the thin grid lines), and prints an ASCII plan: '#' wall, '.' floor, 'n' a window-sized gap in a wall, 'D' a door-sized gap (>= 0.8 square), with wall
rows/columns inserted where a thick divider sits on a grid line. Furniture is NOT read (the labels are for a person or Claude to place); put the glyphs on the draft by hand and feed it to
Tools/plans/plan.py. The overlay PNG shows the detected squares numbered so the draft can be checked against the picture. Needs numpy, scipy, Pillow."""
import sys, argparse
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi
def thick_walls(gray, R=None):
    dark = gray < 110; dt = ndi.distance_transform_edt(dark)
    if R is None: R = max(3, 0.5 * np.percentile(dt[dark], 99.7)) if dark.any() else 5
    core = dt >= R; wall = ndi.binary_dilation(core, structure=np.ones((3, 3)), iterations=int(R) + 1) & dark
    return wall, R
def estimate_cell(gray, wall, axis):
    line = (gray < 215) & ~ndi.binary_dilation(wall, iterations=6); proj = line.sum(axis=1 - axis).astype(float)   # axis 0: columns -> vertical lines
    proj = proj - proj.mean(); ac = np.correlate(proj, proj, "full")[len(proj) - 1:]; ac[:25] = 0
    return int(np.argmax(ac[: len(ac) // 2])) if len(ac) > 60 else 0
def main():
    ap = argparse.ArgumentParser(); ap.add_argument("image"); ap.add_argument("--cols", type=int); ap.add_argument("--rows", type=int); ap.add_argument("--out"); ap.add_argument("--overlay"); a = ap.parse_args()
    gray = np.array(Image.open(a.image).convert("L")).astype(float); wall, R = thick_walls(gray)
    H, W = gray.shape; ys, xs = np.where(wall); oy0, oy1, ox0, ox1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    def run(line, start):   # wall thickness: length of the first wall run from the outer edge, taken as the median over several lines
        n = 0
        for v in line[start:]:
            if v: n += 1
            elif n: break
        return n
    th = int(np.median([run(wall[int(oy0 + (oy1 - oy0) * f), :], ox0) for f in (.2, .35, .5, .65, .8)] + [run(wall[:, int(ox0 + (ox1 - ox0) * f)], oy0) for f in (.2, .35, .5, .65, .8)]))
    iy0, iy1, ix0, ix1 = oy0 + th, oy1 - th, ox0 + th, ox1 - th
    cell = estimate_cell(gray, wall, 0)
    cols = a.cols or max(1, round((ix1 - ix0) / cell)) if cell else (a.cols or 1)
    rows = a.rows or max(1, round((iy1 - iy0) / ((ix1 - ix0) / cols)))   # squares are square: rows follow from the columns
    cw, ch = (ix1 - ix0) / cols, (iy1 - iy0) / rows; t = th
    print(f"image {W}x{H}; wall thickness {th}px; interior box x {ix0}..{ix1}, y {iy0}..{iy1}; {cols} x {rows} squares of {cw:.0f} x {ch:.0f} px")
    def frac(y0, y1, x0, x1): s = wall[max(0, int(y0)):int(y1), max(0, int(x0)):int(x1)]; return float(s.mean()) if s.size else 0.0
    # wall runs on the four outer sides and on grid lines inside: per cell, is the band across it mostly wall
    def hband(yc, c): return frac(yc - t / 2, yc + t / 2, ix0 + c * cw + cw * 0.15, ix0 + (c + 1) * cw - cw * 0.15) > 0.6
    def vband(xc, r): return frac(iy0 + r * ch + ch * 0.15, iy0 + (r + 1) * ch - ch * 0.15, xc - t / 2, xc + t / 2) > 0.6
    hdiv = [k for k in range(1, rows) if np.mean([hband(iy0 + k * ch, c) for c in range(cols)]) > 0.4]
    vdiv = [k for k in range(1, cols) if np.mean([vband(ix0 + k * cw, r) for r in range(rows)]) > 0.4]
    top_y, bot_y, left_x, right_x = iy0 - th / 2, iy1 + th / 2, ix0 - th / 2, ix1 + th / 2
    # plan grid: wall ring + inserted divider rows/cols
    rmap = []; cmap = []
    for r in range(rows):
        if r in hdiv: rmap.append(("div", r))
        rmap.append(("cell", r))
    for c in range(cols):
        if c in vdiv: cmap.append(("div", c))
        cmap.append(("cell", c))
    Hh, Ww = len(rmap) + 2, len(cmap) + 2; g = [["." for _ in range(Ww)] for _ in range(Hh)]
    for x in range(Ww): g[0][x] = "#"; g[Hh - 1][x] = "#"
    for y in range(Hh): g[y][0] = "#"; g[y][Ww - 1] = "#"
    for yi, (kind, r) in enumerate(rmap, 1):
        for xi, (kind2, c) in enumerate(cmap, 1):
            if kind == "div" and kind2 == "div": g[yi][xi] = "#"
            elif kind == "div": g[yi][xi] = "#" if hband(iy0 + r * ch, c) else "D"
            elif kind2 == "div": g[yi][xi] = "#" if vband(ix0 + c * cw, r) else "D"
    def gaps(profile, start, size, ncell, cellpx, cmapping):   # 1D wall profile along a side -> glyphs at the cells holding each gap's centre
        open_ = profile < 0.5; i = 0; n = len(profile)
        while i < n:
            if open_[i]:
                j = i
                while j < n and open_[j]: j += 1
                length = j - i
                if length >= 0.25 * cellpx:
                    ci = int(((i + j) / 2 - start) // cellpx)
                    if 0 <= ci < ncell: yield cmapping[ci], "D" if length >= 0.8 * cellpx else "n"
                i = j
            else: i += 1
    cellcols = [xi for xi, (k, c) in enumerate(cmap, 1) if k == "cell"]; cellrows = [yi for yi, (k, r) in enumerate(rmap, 1) if k == "cell"]
    for yy, row in ((top_y, 0), (bot_y, Hh - 1)):
        band = wall[int(yy - t / 3):int(yy + t / 3), int(ix0):int(ix1)].mean(axis=0)
        for xi, ch_ in gaps(band, 0, cols, cols, cw, cellcols): g[row][xi] = ch_
    for xx, col in ((left_x, 0), (right_x, Ww - 1)):
        band = wall[int(iy0):int(iy1), int(xx - t / 3):int(xx + t / 3)].mean(axis=1)
        for yi, ch_ in gaps(band, 0, rows, rows, ch, cellrows): g[yi][col] = ch_
    text = "\n".join("".join(r) for r in g)
    print(text); print(f"\n{Ww}x{Hh} plan; divider rows {hdiv}, divider columns {vdiv} (grid-line indexes). 'D' = gap >= 0.8 square, 'n' = shorter gap (window or a door narrower than drawn). Furniture not read.")
    if a.out: open(a.out, "w").write(text + "\n")
    if a.overlay:
        im = Image.open(a.image).convert("RGB"); d = ImageDraw.Draw(im)
        for c in range(cols + 1): d.line([(ix0 + c * cw, iy0), (ix0 + c * cw, iy1)], fill=(220, 0, 0), width=2)
        for r in range(rows + 1): d.line([(ix0, iy0 + r * ch), (ix1, iy0 + r * ch)], fill=(220, 0, 0), width=2)
        for r in range(rows):
            for c in range(cols): d.text((ix0 + c * cw + 6, iy0 + r * ch + 4), f"{c},{r}", fill=(0, 0, 220))
        im.save(a.overlay); print("overlay ->", a.overlay)
if __name__ == "__main__": main()
