# 2026-11-21 (Yan: "use it to create better props, like the computers, desks, chairs ... and other angle versions of them, like from the side in 3/4's"): the desk, filing cabinet, computer (the 'console') and lab bench
# redrawn by Pixel Lab pixflux in four facings (furn_pl/run.sh -> furn_pl/<item>_<south|east|north|west>_31.png), area-shrunk to the old sizes and re-palettised, registered OVER the old
# props of the same names (props.PROPS), so WorldArt.Facing's existing picks (front / _fn back / _fe / _fw) show them. NO_PL=1 keeps the old art. Also new looks: chair, chair_fn / _fe / _fw (not in any plan yet).
import os
import numpy as np
from PIL import Image
from props import Prop
from worldlib import *
from props_cathedral import finish2
_HERE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "furn_pl")
DIRS = {"": "south", "_fn": "north", "_fe": "east", "_fw": "west"}
# look prefix: (item file, width of the front picture in sprite px, width of an east/west picture)
SPEC = {"desk": ("desk", 44, 44), "cabinet": ("cabinet", 26, 30), "console": ("computer", 38, 38), "labbench": ("labbench", 46, 46), "labtable": ("labbench", 46, 46), "chair": ("chair", 24, 24)}
def _load(item, d, wpx):
    p = os.path.join(_HERE, f"{item}_{d}_31.png")
    if not os.path.exists(p): return None
    src = Image.open(p).convert("RGBA"); a = np.array(src); ys, xs = np.where(a[..., 3] > 0); src = src.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
    h = max(2, round(src.height * wpx / src.width)); im = src.resize((wpx, h), Image.BOX); arr = np.array(im); al = arr[..., 3] >= 110
    q = Image.fromarray(arr[..., :3]).quantize(colors=28, method=Image.MEDIANCUT, dither=Image.NONE).convert("RGB"); out = np.dstack([np.array(q), np.where(al, 255, 0).astype(np.uint8)]); out[~al] = 0
    return Image.fromarray(out.astype(np.uint8), "RGBA")
def _make(item, d, wpx):
    def f(seed):
        im = _load(item, d, wpx)
        return finish2(im, (im.width // 2, int(im.height * 0.94)), (max(10, int(im.width * 0.42)), 5, 1, -1))
    return f
NEW = {}; VARIANTS = {}
for look, (item, wf, ws) in SPEC.items():
    for suf, d in DIRS.items():
        wpx = wf if d in ("south", "north") else ws
        if _load(item, d, wpx) is None: continue
        if look in ("labbench", "labtable") and suf: continue
        if look == "console" and suf in ("_fe", "_fw"): continue        # the console only has a front and a back (its sides never read)
        NEW[look + suf] = _make(item, d, wpx); VARIANTS[look + suf] = 2   # (both variants the same picture until there is a second take)
if __name__ == "__main__":
    import ground
    T = 16; W_, H_ = T * 14, T * 8
    bg = blank(W_, H_); g = ground.GROUNDS["labtile"](3)
    for ty in range(0, H_, g.height):
        for tx in range(0, W_, g.width): bg.paste(g, (tx, ty))
    big = bg.resize((W_ * 2, H_ * 2), Image.NEAREST); i = 0
    for nm in sorted(NEW):
        p = NEW[nm](1); x, y = (1 + (i % 7) * 2) * T + T // 2, (1 + (i // 7) * 3) * T + T + 8; i += 1
        w, h, dx, dy = p.shadow; w2, h2 = w * 2, h * 2; big.alpha_composite(shadow_ellipse(w2, h2), (int(x * 2 + dx * 2 - w2 / 2), int(y * 2 + dy * 2 - h2 / 2)))
        big.alpha_composite(p.im, (int(x * 2 - p.pivot[0]), int(y * 2 - p.pivot[1])))
    big.resize((big.width * 3, big.height * 3), Image.NEAREST).convert("RGB").save("/tmp/furn_props_preview.png"); print(sorted(NEW))
