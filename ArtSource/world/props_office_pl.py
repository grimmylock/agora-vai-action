# 2026-11-22 (Yan: "the office doesn't look like an office, create new sprites for office furniture and a wooden floor"): the office set drawn by Pixel Lab pixflux (office_pl/run.sh) at the size they are used (no shrinking), low detail,
# straight front (dir south) and back (dir north) views only (the east / west views came out diagonal). Looks: odesk, opc (desk with a computer), ochair, ofile, oshelf, osofa, oplant, ocooler (+ _fn = the back view).
import os
import numpy as np
from PIL import Image
from props import Prop
from worldlib import *
from props_cathedral import finish2
_HERE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "office_pl")
ITEMS = {"odesk": "odesk", "opc": "opc", "ochair": "ochair", "ofile": "ofile", "oshelf": "oshelf", "osofa": "osofa", "oplant": "oplant", "ocooler": "ocooler"}
def _load(item, d):
    p = os.path.join(_HERE, f"{item}_{d}.png")
    if not os.path.exists(p): return None
    src = Image.open(p).convert("RGBA"); a = np.array(src); ys, xs = np.where(a[..., 3] > 0)
    if len(xs) == 0: return None
    src = src.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1)); arr = np.array(src); al = arr[..., 3] >= 110
    q = Image.fromarray(arr[..., :3]).quantize(colors=24, method=Image.MEDIANCUT, dither=Image.NONE).convert("RGB"); out = np.dstack([np.array(q), np.where(al, 255, 0).astype(np.uint8)]); out[~al] = 0
    return Image.fromarray(out.astype(np.uint8), "RGBA")
def _make(item, d):
    def f(seed):
        im = _load(item, d); return finish2(im, (im.width // 2, int(im.height * 0.95)), (max(10, int(im.width * 0.45)), 5, 1, -1))
    return f
NEW = {}; VARIANTS = {}
for look, item in ITEMS.items():
    for suf, d in (("", "south"), ("_fn", "north")):
        if _load(item, d) is not None: NEW[look + suf] = _make(item, d); VARIANTS[look + suf] = 1
if __name__ == "__main__":
    import ground
    T = 16; W_, H_ = T * 14, T * 6
    bg = blank(W_, H_); g = ground.GROUNDS["floor"](3)
    for ty in range(0, H_, g.height):
        for tx in range(0, W_, g.width): bg.paste(g, (tx, ty))
    big = bg.resize((W_ * 2, H_ * 2), Image.NEAREST)
    for i, nm in enumerate(sorted(NEW)):
        p = NEW[nm](1); x, y = (1 + (i % 7) * 2) * T + T // 2, (2 + (i // 7) * 3) * T; w, h, dx, dy = p.shadow
        big.alpha_composite(shadow_ellipse(w * 2, h * 2), (int(x * 2 + dx * 2 - w), int(y * 2 + dy * 2 - h))); big.alpha_composite(p.im, (int(x * 2 - p.pivot[0]), int(y * 2 - p.pivot[1])))
    big.resize((big.width * 3, big.height * 3), Image.NEAREST).convert("RGB").save("/tmp/office_props_preview.png"); print(sorted(NEW))
