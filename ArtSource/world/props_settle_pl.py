# 2026-11-24 (Yan: "looks good"): the props of the settled places, drawn by Pixel Lab (create_object_pro_flash, view low top-down, the atlas desk as the style anchor; settle_pl/raw_*.png, kept at the size they were drawn).
# Looks: cot, barcounter, bottleshelf, medcabinet, stool, bench.
import os
import numpy as np
from PIL import Image
from props import Prop
from worldlib import *
from props_cathedral import finish2
_HERE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "settle_pl")
ITEMS = ["cot", "barcounter", "bottleshelf", "medcabinet", "stool", "bench"]
# 2026-11-24 (the three test buildings): look name -> the raw file's suffix (settle_pl/raw_<file>.png)
MORE = {"bed": "bed", "wardrobe": "wardrobe", "kitchensink": "kitchen", "stove": "stove", "fridge": "fridge", "diningset": "dining", "sofa": "sofa", "toilet": "toilet", "bathtub": "bathtub",
        "washbasin": "washbasin", "bookshelf": "bookshelf", "goodsshelf": "goodsshelf", "shopcounter": "shopcounter", "bunkbed": "bunkbed", "footlocker": "footlocker", "weaponrack": "weaponrack", "maptable": "maptable",
        "nightstand": "nightstand", "kitchencounter": "kcounter", "plant": "plant", "floorlamp": "lamp", "noticeboard": "board", "boxstack": "boxes",
        "stall_green": "stall_green", "stall_red": "stall_red", "stall_blue": "stall_blue", "stall_ochre": "stall_ochre",
        "pooltable": "pooltable", "jukebox": "jukebox", "cardtable": "cardtable", "kegs": "kegs",
        "barcounter_e": "barcounter_e", "barcounter_w": "barcounter_w", "barcounter_n": "barcounter_n", "sofa_e": "sofa_e", "sofa_w": "sofa_w", "bench_e": "bench_e", "bench_w": "bench_w", "jukebox_e": "jukebox_e", "jukebox_w": "jukebox_w", "bottleshelf_e": "bottleshelf_e", "bottleshelf_w": "bottleshelf_w",
        "bunkbed_e": "bunkbed_e", "bunkbed_w": "bunkbed_w", "cot_e": "cot_e", "cot_w": "cot_w", "goodsshelf_e": "goodsshelf_e", "goodsshelf_w": "goodsshelf_w",
        "fuelpump": "fuelpump", "haybale": "haybale", "trough": "trough", "tractor": "tractor", "workbench": "workbench", "radiotable": "radiotable", "outhouse": "outhouse",
        "canopy": "canopy", "cooler": "cooler", "stationsign": "stationsign", "snackrack": "snackrack", "icechest": "icechest"}
# 2026-11-26 (Yan: "the artstyle is not consistent, some have vibrant colors and hard black outlines, others are muted like the game; it looks way too clean for something abandoned"):
# the place-1 props are muted toward the game's palette (less saturation, a warm grey-brown cast), dirtied (dust, grime at the foot, rust streaks) and outlined in a dark shade of their own colour
WEATHERED = {"fuelpump", "canopy", "cooler", "stationsign", "snackrack", "icechest", "haybale", "trough", "tractor", "workbench", "radiotable", "outhouse"}
def _weather(im, seed):
    import colorsys
    a = np.array(im).astype(np.float32); H, W = a.shape[:2]; rng = np.random.RandomState(seed)
    rgb = a[..., :3] / 255.0; out = np.zeros_like(rgb)
    for y in range(H):
        for x in range(W):
            if a[y, x, 3] == 0: continue
            h, s, v = colorsys.rgb_to_hsv(*rgb[y, x]); s *= 0.50; v = v * 0.80 + 0.03
            r, g, b = colorsys.hsv_to_rgb(h, s, v)
            r, g, b = r * 0.86 + 0.40 * 0.14, g * 0.86 + 0.36 * 0.14, b * 0.86 + 0.28 * 0.14      # a warm grey-brown cast
            k = 1.0
            if y > H * 0.70: k -= 0.16 * (y - H * 0.70) / (H * 0.30)                              # wet, dirty foot
            k -= 0.20 * rng.rand() * (rng.rand() < 0.35)                                           # grime flecks
            if rng.rand() < 0.04: r, g, b = r * 1.10 + 0.05, g * 1.08 + 0.05, b * 1.04 + 0.05      # dust
            out[y, x] = (r * k, g * k, b * k)
    for x in rng.choice(W, size=max(2, W // 9), replace=False):                                    # rust streaks running down from the top
        ys = np.where(a[:, x, 3] > 0)[0]
        if len(ys) < 6: continue
        y0 = ys.min() + rng.randint(1, max(2, len(ys) // 3)); y1 = min(ys.max(), y0 + rng.randint(4, max(5, len(ys) // 2)))
        for y in range(y0, y1):
            if a[y, x, 3] > 0: out[y, x] = out[y, x] * 0.55 + np.array([0.36, 0.20, 0.12]) * 0.45
    res = np.dstack([np.clip(out, 0, 1) * 255, a[..., 3]]).astype(np.uint8)
    return Image.fromarray(res, "RGBA")
def _load(item):
    p = os.path.join(_HERE, f"raw_{item}.png")
    if not os.path.exists(p): return None
    src = Image.open(p).convert("RGBA"); a = np.array(src); ys, xs = np.where(a[..., 3] > 0)
    if len(xs) == 0: return None
    src = src.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1)); arr = np.array(src); al = arr[..., 3] >= 110
    q = Image.fromarray(arr[..., :3]).quantize(colors=28, method=Image.MEDIANCUT, dither=Image.NONE).convert("RGB"); out = np.dstack([np.array(q), np.where(al, 255, 0).astype(np.uint8)]); out[~al] = 0
    img = Image.fromarray(out.astype(np.uint8), "RGBA")
    return _weather(img, sum(map(ord, item))) if item in WEATHERED else img
K = (20, 22, 23)            # the game's hard dark outline colour (worldlib.K)
def _finish_black(im, pivot, shadow, tall=False, soft=False):
    """2026-11-24 (Yan: "some have hard black outlines and others don't, all of them should"): every settled-place prop gets a hard near-black 1 px outline all round, like the other furniture. An edge that is already dark (the Pixel Lab sprite drew its own outline there) is left alone,
    so no prop gets a double-thick edge. Pad 1, double density, as finish2 does."""
    a = np.array(im).astype(np.int32); H, W = a.shape[:2]
    big = np.zeros((H + 2, W + 2, 4), np.int32); big[1:H + 1, 1:W + 1] = a; al = big[..., 3] > 0; o = big.copy(); HH, WW = al.shape
    for y in range(HH):
        for x in range(WW):
            if al[y, x]: continue
            nb = [big[y + dy, x + dx] for dx, dy in ((0, -1), (0, 1), (-1, 0), (1, 0)) if 0 <= x + dx < WW and 0 <= y + dy < HH and al[y + dy, x + dx]]
            if not nb: continue
            if all(int(c[0]) + int(c[1]) + int(c[2]) < 150 for c in nb): o[y, x] = (int(min(nb, key=lambda c: c[0] + c[1] + c[2])[0]), int(min(nb, key=lambda c: c[0] + c[1] + c[2])[1]), int(min(nb, key=lambda c: c[0] + c[1] + c[2])[2]), 255)   # it already has its own dark edge: keep that colour
            elif soft:                                                                              # the game's own look: a dark shade of the edge's own colour
                d = min(nb, key=lambda c: c[0] + c[1] + c[2]); o[y, x] = (int(d[0] * 0.40), int(d[1] * 0.38), int(d[2] * 0.36), 255)
            else: o[y, x] = (K[0], K[1], K[2], 255)
    return Prop(Image.fromarray(o.astype(np.uint8), "RGBA"), (pivot[0] + 1, pivot[1] + 1), shadow, tall, scale=2)
def _make(item):
    def f(seed):
        im = _load(item); return _finish_black(im, (im.width // 2, int(im.height * 0.95)), (max(10, int(im.width * 0.45)), 5, 1, -1), soft=item in WEATHERED)
    return f
NEW = {}; VARIANTS = {}
for item in ITEMS:
    if _load(item) is not None: NEW[item] = _make(item); VARIANTS[item] = 1
for look, f in MORE.items():
    if _load(f) is not None: NEW[look] = _make(f); VARIANTS[look] = 1
