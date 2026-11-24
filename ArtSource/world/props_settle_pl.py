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
        "nightstand": "nightstand", "kitchencounter": "kcounter", "plant": "plant", "floorlamp": "lamp", "noticeboard": "board", "boxstack": "boxes"}
def _load(item):
    p = os.path.join(_HERE, f"raw_{item}.png")
    if not os.path.exists(p): return None
    src = Image.open(p).convert("RGBA"); a = np.array(src); ys, xs = np.where(a[..., 3] > 0)
    if len(xs) == 0: return None
    src = src.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1)); arr = np.array(src); al = arr[..., 3] >= 110
    q = Image.fromarray(arr[..., :3]).quantize(colors=28, method=Image.MEDIANCUT, dither=Image.NONE).convert("RGB"); out = np.dstack([np.array(q), np.where(al, 255, 0).astype(np.uint8)]); out[~al] = 0
    return Image.fromarray(out.astype(np.uint8), "RGBA")
def _make(item):
    def f(seed):
        im = _load(item); return finish2(im, (im.width // 2, int(im.height * 0.95)), (max(10, int(im.width * 0.45)), 5, 1, -1))
    return f
NEW = {}; VARIANTS = {}
for item in ITEMS:
    if _load(item) is not None: NEW[item] = _make(item); VARIANTS[item] = 1
for look, f in MORE.items():
    if _load(f) is not None: NEW[look] = _make(f); VARIANTS[look] = 1
