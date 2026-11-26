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
        "bunkbed_e": "bunkbed_e", "bunkbed_w": "bunkbed_w", "cot_e": "cot_e", "cot_w": "cot_w", "goodsshelf_e": "goodsshelf_e", "goodsshelf_w": "goodsshelf_w"}
def _load(item):
    p = os.path.join(_HERE, f"raw_{item}.png")
    if not os.path.exists(p): return None
    src = Image.open(p).convert("RGBA"); a = np.array(src); ys, xs = np.where(a[..., 3] > 0)
    if len(xs) == 0: return None
    src = src.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1)); arr = np.array(src); al = arr[..., 3] >= 110
    q = Image.fromarray(arr[..., :3]).quantize(colors=28, method=Image.MEDIANCUT, dither=Image.NONE).convert("RGB"); out = np.dstack([np.array(q), np.where(al, 255, 0).astype(np.uint8)]); out[~al] = 0
    return Image.fromarray(out.astype(np.uint8), "RGBA")
K = (20, 22, 23)            # the game's hard dark outline colour (worldlib.K)
def _finish_black(im, pivot, shadow, tall=False):
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
            else: o[y, x] = (K[0], K[1], K[2], 255)
    return Prop(Image.fromarray(o.astype(np.uint8), "RGBA"), (pivot[0] + 1, pivot[1] + 1), shadow, tall, scale=2)
def _make(item):
    def f(seed):
        im = _load(item); return _finish_black(im, (im.width // 2, int(im.height * 0.95)), (max(10, int(im.width * 0.45)), 5, 1, -1))
    return f
NEW = {}; VARIANTS = {}
for item in ITEMS:
    if _load(item) is not None: NEW[item] = _make(item); VARIANTS[item] = 1
for look, f in MORE.items():
    if _load(f) is not None: NEW[look] = _make(f); VARIANTS[look] = 1
