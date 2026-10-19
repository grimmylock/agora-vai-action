"""Reference photo -> weapon sprites in the locked agora-vai style.
A = 64x32 (menus/inventory), B = 32x16 (in hand, PPU 32).

Pipeline per weapon (config in WEAPONS):
  cut-out (rembg isnet, or a white-background flood fill)  -> flip/rotate so the muzzle points right
  -> crop -> edge-preserving smoothing (kills checkering / photo noise)  -> area-downscale to the
  weapon's in-game length  -> each pixel snapped to the weapon palette by material (gunmetal ramp,
  wood, olive polymer, brass) -> majority filter -> lit top edge / shaded bottom edge -> outline.
B = 2x2 reduction of A (majority, a bright/thin feature wins at >=25%) -> re-outline.
Per-weapon hand fixes go in FIXES (pixel edits on the A grid, before outline)."""
import os, sys, json
import numpy as np, cv2
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
K = (20, 22, 23)
def hexc(h): h = h.lstrip('#'); return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))
PAL = {"0": "#d5dadd", "1": "#a9b0b5", "2": "#727980", "3": "#4c5258", "4": "#33373b", "5": "#1f2225",
       "a": "#c98a4e", "b": "#a0602f", "c": "#743f1c", "d": "#4c2812",
       "o": "#7a8256", "O": "#555c3a", "e": "#3c4128", "Y": "#c9a63e", "y": "#f0d878",
       "L": "#1c2a30", "l": "#4f9ac0", "k": "#bfeaf8", "R": "#c0302a"}
RAMPS = {"metal": "012345", "wood": "abcd", "olive": "oOe", "brass": "yY", "optic": "kLl", "red": "R"}
RAMP_OF = {ch: r for r, s in RAMPS.items() for ch in s}

def load_cut(name, cfg):
    im = Image.open(os.path.join(HERE, "cut", name + ".png")).convert("RGBA")
    if cfg.get("mask") == "white":                                  # plain white background: flood fill from the border
        src = Image.open([os.path.join(HERE, "refs", f) for f in os.listdir(os.path.join(HERE, "refs")) if f.startswith(name + ".")][0]).convert("RGBA")
        bg = Image.new("RGBA", src.size, (255, 255, 255, 255)); bg.alpha_composite(src); rgb = np.array(bg.convert("RGB"))
        h, w = rgb.shape[:2]
        near = (np.abs(rgb.astype(int) - 255).max(axis=2) <= cfg.get("tol", 18)).astype(np.uint8)
        ff = near.copy(); m = np.zeros((h + 2, w + 2), np.uint8)
        for x, y in [(0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)] + [(x, 0) for x in range(0, w, 7)] + [(x, h - 1) for x in range(0, w, 7)] + [(0, y) for y in range(0, h, 7)] + [(w - 1, y) for y in range(0, h, 7)]:
            if ff[y, x] == 1: cv2.floodFill(ff, m, (x, y), 2)
        alpha = np.where(ff == 2, 0, 255).astype(np.uint8)
        arr = np.dstack([rgb, alpha]); im = Image.fromarray(arr, "RGBA")
    if "drop" in cfg or "drop_dark" in cfg or "erase" in cfg:         # remove slings / sheaths / tags by colour or box
        a_ = np.array(im).astype(np.float32); rgb_ = a_[..., :3] / 255
        mx = rgb_.max(axis=2); mn = rgb_.min(axis=2); sat = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-3), 0)
        hsv = cv2.cvtColor((rgb_ * 255).astype(np.uint8), cv2.COLOR_RGB2HSV).astype(np.float32); hue = hsv[..., 0] * 2
        kill = np.zeros(mx.shape, bool)
        if "drop" in cfg:
            h0, h1, smin = cfg["drop"]; kill |= (hue >= h0) & (hue <= h1) & (sat >= smin)
        if "drop_dark" in cfg: kill |= mx < cfg["drop_dark"]
        for (l, t, r, b_) in cfg.get("erase", []): kill[t:b_, l:r] = True
        a_[kill, 3] = 0
        a_[..., 3] = cv2.morphologyEx(a_[..., 3].astype(np.uint8), cv2.MORPH_OPEN, np.ones((3, 3), np.uint8))
        im = Image.fromarray(a_.astype(np.uint8), "RGBA")
    if "crop" in cfg: im = im.crop(cfg["crop"])                    # (l, t, r, b) in source pixels: drop slings, stands, sheaths
    a = np.array(im)
    if cfg.get("biggest", True):                                   # keep only the largest blob (stray bits of background)
        n, lab, st, _ = cv2.connectedComponentsWithStats((a[..., 3] > 100).astype(np.uint8), 8)
        if n > 2:
            keep = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA]))
            big = st[keep, cv2.CC_STAT_AREA]
            for i in range(1, n):
                if i != keep and st[i, cv2.CC_STAT_AREA] < big * cfg.get("keep_frac", 0.02): a[lab == i, 3] = 0
        im = Image.fromarray(a, "RGBA")
    if cfg.get("flip"): im = im.transpose(Image.FLIP_LEFT_RIGHT)
    if cfg.get("rot"): im = im.rotate(cfg["rot"], resample=Image.BICUBIC, expand=True)
    bb = im.getchannel("A").point(lambda v: 255 if v > 100 else 0).getbbox()
    return im.crop(bb)

cfg_olive_s = 0.16
def classify(rgb, mat_hint=None):
    r, g, b = [c / 255 for c in rgb]
    mx, mn = max(r, g, b), min(r, g, b); v = mx; s = 0 if mx == 0 else (mx - mn) / mx
    if mx == mn: h = 0
    elif mx == r: h = (60 * ((g - b) / (mx - mn)) + 360) % 360
    elif mx == g: h = 60 * ((b - r) / (mx - mn)) + 120
    else: h = 60 * ((r - g) / (mx - mn)) + 240
    L = 0.299 * rgb[0] + 0.587 * rgb[1] + 0.114 * rgb[2]
    if s > 0.42 and 42 <= h <= 62 and v > 0.45: return "y" if L > 170 else "Y"
    if s > 0.3 and (h <= 40 or h >= 345) and v > 0.18:
        return "a" if L > 145 else "b" if L > 100 else "c" if L > 62 else "d"
    if s > cfg_olive_s and 55 <= h <= 175 and v > 0.15:
        return "o" if L > 115 else "O" if L > 72 else "e"
    return "0" if L > 205 else "1" if L > 158 else "2" if L > 112 else "3" if L > 76 else "4" if L > 46 else "5"

def shift(ch, d):
    ramp = RAMPS[RAMP_OF[ch]]; i = ramp.index(ch)
    return ramp[max(0, min(len(ramp) - 1, i + d))]

def to_grid(im, length, cfg):
    w, h = im.size
    th = max(2, round(length * h / w * cfg.get("hscale", 1.0)))
    arr = np.array(im).astype(np.float32)
    rgb = arr[..., :3]; al = arr[..., 3:] / 255
    sm = cv2.bilateralFilter(rgb.astype(np.uint8), 0, cfg.get("smooth_c", 40), max(3, w / 120)).astype(np.float32)
    # colour only from well inside the cut-out: the soft edge carries background halo (beige, wood, white)
    er = cv2.erode((al[..., 0] > 0.9).astype(np.uint8), np.ones((3, 3), np.uint8), iterations=max(1, int(w / 160))).astype(np.float32)[..., None]
    cw = cv2.resize(np.dstack([sm * er, er[..., 0]]), (length, th), interpolation=cv2.INTER_AREA)
    cf = cv2.resize(np.dstack([sm * al, al[..., 0]]), (length, th), interpolation=cv2.INTER_AREA)
    col = np.where(cw[..., 3:] > 0.05, cw[..., :3] / np.maximum(cw[..., 3:], 1e-3), cf[..., :3] / np.maximum(cf[..., 3:], 1e-3))
    A = cf[..., 3]
    # enclosed see-through gaps (trigger guards, stock holes, lever loops) stay open even when small
    tr = (al[..., 0] < 0.4).astype(np.uint8); ff = tr.copy(); mk = np.zeros((tr.shape[0] + 2, tr.shape[1] + 2), np.uint8)
    for (px_, py_) in [(x_, 0) for x_ in range(0, w, 3)] + [(x_, h - 1) for x_ in range(0, w, 3)] + [(0, y_) for y_ in range(0, h, 3)] + [(w - 1, y_) for y_ in range(0, h, 3)]:
        if ff[py_, px_] == 1: cv2.floodFill(ff, mk, (px_, py_), 2)
    holes = cv2.resize((ff == 1).astype(np.float32), (length, th), interpolation=cv2.INTER_AREA)
    A = np.where(holes >= cfg.get("hole", 0.28), 0, A)
    cov = cfg.get("cov", 0.5)
    grid = [["." for _ in range(length)] for _ in range(th)]
    for y in range(th):
        for x in range(length):
            if A[y, x] >= cov:
                grid[y][x] = classify(tuple(col[y, x] * cfg.get("bright", 1.0)))
                if cfg.get("recolor") == "dark": grid[y][x] = "4" if (0.299 * col[y, x][0] + 0.587 * col[y, x][1] + 0.114 * col[y, x][2]) > 120 else "5"
                for (x0, x1, y0, y1, mat) in cfg.get("force", []):        # e.g. a rusty barrel is still metal
                    if x0 <= x <= x1 and y0 <= y <= y1 and RAMP_OF[grid[y][x]] != mat:
                        L = 0.299 * col[y, x][0] + 0.587 * col[y, x][1] + 0.114 * col[y, x][2]
                        ramp = RAMPS[mat]; grid[y][x] = ramp[min(len(ramp) - 1, int((255 - L) / 256 * len(ramp) * 1.2))]
    # majority filter inside each material (speckle cleanup)
    out = [row[:] for row in grid]
    for y in range(th):
        for x in range(length):
            c = grid[y][x]
            if c == ".": continue
            neigh = [grid[yy][xx] for yy in range(max(0, y - 1), min(th, y + 2)) for xx in range(max(0, x - 1), min(length, x + 2)) if grid[yy][xx] != "." and RAMP_OF[grid[yy][xx]] == RAMP_OF[c]]
            vals = sorted(neigh, key=lambda ch: RAMPS[RAMP_OF[c]].index(ch))
            out[y][x] = vals[len(vals) // 2]
    grid = out
    # light from the top: top edge one step lighter, bottom edge one step darker
    lit = [row[:] for row in grid]
    for y in range(th):
        for x in range(length):
            c = grid[y][x]
            if c == ".": continue
            if y == 0 or grid[y - 1][x] == ".": lit[y][x] = shift(c, -1)
            elif y == th - 1 or grid[y + 1][x] == ".": lit[y][x] = shift(c, +1)
    return lit

def place(grid, W, H):
    th, tw = len(grid), len(grid[0])
    G = [["." for _ in range(W)] for _ in range(H)]
    ox = (W - tw) // 2; oy = (H - th) // 2
    for y in range(th):
        for x in range(tw):
            if grid[y][x] != ".": G[oy + y][ox + x] = grid[y][x]
    return G

def render(G, outline=True):
    H, W = len(G), len(G[0])
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); px = im.load()
    for y in range(H):
        for x in range(W):
            if G[y][x] != ".": px[x, y] = hexc(PAL[G[y][x]]) + (255,)
    if outline:
        out = im.copy(); o = out.load()
        for y in range(H):
            for x in range(W):
                if px[x, y][3] == 0 and any(0 <= x + dx < W and 0 <= y + dy < H and px[x + dx, y + dy][3] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                    o[x, y] = K + (255,)
        im = out
    return im

def bright(ch): return ch in "01ayYkl"
def reduce_b(G):
    H, W = len(G) // 2, len(G[0]) // 2
    B = [["." for _ in range(W)] for _ in range(H)]
    for y in range(H):
        for x in range(W):
            cells = [G[2 * y + j][2 * x + i] for j in (0, 1) for i in (0, 1)]
            solid = [c for c in cells if c != "."]
            if len(solid) < 2: continue                                 # under half covered: empty
            br = [c for c in solid if bright(c)]
            if br and len(br) >= 1 and len(solid) <= 3: B[y][x] = br[0]   # thin bright feature survives
            else: B[y][x] = max(set(solid), key=solid.count)
    return B

def make(name, cfg):
    global cfg_olive_s
    cfg_olive_s = cfg.get("olive_s", 0.16)
    im = load_cut(name, cfg)
    g = to_grid(im, cfg["A"], cfg)
    for (x, y, ch) in cfg.get("fix", []):                          # hand fixes, grid coords before placement
        if 0 <= y < len(g) and 0 <= x < len(g[0]): g[y][x] = ch
    A = place(g, 64, 32)
    for (x, y, ch) in cfg.get("after", []):                        # attachments drawn on top, in A canvas coords
        if 0 <= y < 32 and 0 <= x < 64: A[y][x] = ch
    B = place(reduce_b(A), 32, 16)
    return A, B
