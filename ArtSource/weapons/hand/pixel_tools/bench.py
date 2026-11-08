"""Drafting helpers: build a grid, print it as strings, preview it outlined at x8 next to the reference."""
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from PIL import Image
class G:
    def __init__(s, w, h): s.w, s.h = w, h; s.g = [["."] * w for _ in range(h)]
    def set(s, x, y, c):
        if 0 <= x < s.w and 0 <= y < s.h: s.g[y][x] = c
    def get(s, x, y): return s.g[y][x] if 0 <= x < s.w and 0 <= y < s.h else "."
    def col(s, x, top, ramp):
        """ramp: the letters from top to bottom, placed from row `top`"""
        for k, c in enumerate(ramp): s.set(x, top + k, c)
    def span(s, x0, x1, top, ramp):
        for x in range(x0, x1 + 1): s.col(x, top, ramp)
    def row(s, y, x0, txt):
        for i, c in enumerate(txt):
            if c != " ": s.set(x0 + i, y, c)
    def rows(s): return ["".join(r) for r in s.g]
def show(name, grids, pal):
    import melee_px as m
    for g in grids: print(name); [print('    "%s",' % r) for r in g.rows()]
    ims = [m.paint(g.rows(), pal, g.w + 4, g.h + 4) for g in grids]
    W = sum(i.width * 8 + 20 for i in ims); H = max(i.height * 8 for i in ims)
    o = Image.new("RGBA", (W, H), (46, 42, 34, 255)); x = 0
    for i in ims: o.alpha_composite(i.resize((i.width * 8, i.height * 8), Image.NEAREST), (x, 0)); x += i.width * 8 + 20
    out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "../../../_out"); os.makedirs(out, exist_ok=True)
    o.save(os.path.join(out, f"draft_{name}.png"))
