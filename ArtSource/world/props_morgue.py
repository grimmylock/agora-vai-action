# The morgue set, v2 (2026-11-21, Yan: "the silent hill props do not look great right now"): Pixel Lab pixflux drawings (world/morgue_pl/run.sh, picks below), area-shrunk to prop size and re-palettised. The first, hand-drawn set is props_morgue_handdrawn.py. (v1 note:  Yan: "make the lab/hospital where he's located be something scary too, straight out of silent hill"). Drawn like the cathedral set (props_cathedral.py helpers,
# double density: a tile is 32 sprite px, every surface lit). Looks: gurney, bodybag, wheelchair, bedframe, ivstand, hangbag. Registered into props.PROPS by props.py.
import numpy as np
from PIL import Image, ImageDraw
from props import Prop
from worldlib import *
from props_cathedral import mask_of, put, grid, finish2, _noise, _stretch

STEEL = P["steel"]; RUST = P["rust"]
BLOOD = ramp("#1a0506", "#3a0a0c", "#5e1012", "#8a1a1a", "#b82a24")
BAG = ramp("#050506", "#101114", "#1b1d22", "#2a2d34", "#40444d")
SHEET = ramp("#3a3630", "#5e574c", "#8a8272", "#b4ab98", "#d8cfba")
SKIN = ramp("#2a2422", "#4a403a", "#6e6258", "#948678", "#b8a898")

def stain(im, mask, seed, amount=0.5, cx=None, cy=None, rad=None):
    """a dark, uneven blood stain over the masked pixels (a blob round (cx, cy), noise-edged)"""
    h, w = mask.shape; yy, xx = grid(w, h); n = _noise(w, h, 3, seed)
    cx = w / 2 if cx is None else cx; cy = h / 2 if cy is None else cy; rad = min(w, h) / 3 if rad is None else rad
    d = np.sqrt((xx - cx) ** 2 + ((yy - cy) * 1.2) ** 2) / rad
    s = (d + (n - 0.5) * 0.9) < amount * 1.6
    put(im, mask & s, 0.25 + n * 0.5 - d * 0.2, BLOOD)

def gurney(seed):
    W_, H_ = 32, 46; im = blank(W_, H_); yy, xx = grid(W_, H_)
    for x in (4, 26):                                                   # wheels
        ImageDraw.Draw(im).ellipse([x, 38, x + 3, 42], fill=STEEL[0]); ImageDraw.Draw(im).point((x + 1, 39), fill=STEEL[3])
    leg = mask_of(W_, H_, lambda d: (d.rectangle([5, 30, 7, 40], fill=255), d.rectangle([24, 30, 26, 40], fill=255))); put(im, leg, 0.4 - (xx % 3) * 0.1, STEEL)
    bed = mask_of(W_, H_, lambda d: d.polygon([(2, 14), (29, 14), (31, 31), (0, 31)], fill=255)); put(im, bed, 0.5 + (yy < 16) * 0.3 - (yy - 14) / 17 * 0.3 + (_noise(W_, H_, 3, seed) - 0.5) * 0.2, STEEL)
    body = mask_of(W_, H_, lambda d: (d.ellipse([6, 8, 25, 28], fill=255), d.ellipse([20, 6, 30, 16], fill=255)))                                       # a body under a sheet: shoulders, chest, a head
    put(im, body, 0.6 + 0.25 * np.sin(xx * 0.7 + yy * 0.2) - (yy - 8) / 20 * 0.4 + (_noise(W_, H_, 4, seed + 1) - 0.5) * 0.3, SHEET)
    d = ImageDraw.Draw(im); d.arc([8, 10, 24, 26], 200, 330, fill=SHEET[1]); d.line([(9, 20), (22, 18)], fill=SHEET[1])
    stain(im, body, seed + 2, 0.55, 15, 18, 7)
    hand = mask_of(W_, H_, lambda dd: dd.rectangle([1, 24, 4, 33], fill=255)); put(im, hand, 0.5 - (xx - 1) * 0.1, SKIN)                                  # one grey hand hanging off the side
    for y in (28, 30, 32): im.putpixel((2, y), BLOOD[3])
    d.line([(1, 34), (1, 38)], fill=BLOOD[2])
    return finish2(im, (16, 44), (30, 6, 1, -1))

def bodybag(seed):
    W_, H_ = 32, 26; im = blank(W_, H_); yy, xx = grid(W_, H_)
    bag = mask_of(W_, H_, lambda d: d.polygon([(2, 12), (6, 5), (24, 4), (30, 10), (29, 20), (22, 24), (6, 24), (1, 19)], fill=255))
    put(im, bag, 0.5 + 0.35 * np.sin(xx * 0.55 + 0.6) - (yy - 4) / 20 * 0.5 + (_noise(W_, H_, 3, seed) - 0.5) * 0.3, BAG)
    d = ImageDraw.Draw(im); d.line([(4, 14), (27, 12)], fill=BAG[0]); d.line([(5, 13), (27, 11)], fill=BAG[4])                                               # the zip, half open
    for x in range(8, 26, 3): im.putpixel((x, 13 - (x - 8) // 9), (150, 150, 155, 255))
    gap = mask_of(W_, H_, lambda dd: dd.polygon([(5, 15), (14, 14), (13, 17), (6, 18)], fill=255)); put(im, gap, np.zeros((H_, W_)), ramp("#040202", "#120606"))
    d.line([(7, 16), (12, 15)], fill=SKIN[3])
    stain(im, bag, seed + 2, 0.5, 20, 17, 6); d.line([(18, 21), (17, 25)], fill=BLOOD[2])
    return finish2(im, (16, 24), (28, 5, 1, -1))

def wheelchair(seed):
    W_, H_ = 30, 40; im = blank(W_, H_); yy, xx = grid(W_, H_); d = ImageDraw.Draw(im)
    for cx in (6, 23):
        d.ellipse([cx - 5, 18, cx + 5, 36], outline=STEEL[1]); d.ellipse([cx - 4, 19, cx + 4, 35], outline=RUST[2])
        for a in range(0, 180, 45): d.line([(cx - 4 + a // 45, 22), (cx + 4 - a // 45, 32)], fill=STEEL[2])
    seat = mask_of(W_, H_, lambda dd: dd.rectangle([8, 22, 21, 29], fill=255)); put(im, seat, 0.35 + (_noise(W_, H_, 3, seed) - 0.5) * 0.3, ramp("#0a0707", "#1c1210", "#32221c", "#4a342a"))
    back = mask_of(W_, H_, lambda dd: dd.polygon([(8, 8), (21, 6), (22, 22), (8, 22)], fill=255)); put(im, back, 0.35 + (_noise(W_, H_, 3, seed + 1) - 0.5) * 0.3 + (xx < 11) * 0.15, ramp("#0a0707", "#1c1210", "#32221c", "#4a342a"))
    d.line([(8, 5), (8, 9)], fill=STEEL[3]); d.line([(21, 4), (21, 8)], fill=STEEL[3]); d.line([(8, 5), (21, 4)], fill=STEEL[3])
    d.line([(10, 25), (19, 25)], fill=STEEL[4]); d.line([(9, 27), (13, 38)], fill=STEEL[2]); d.line([(20, 27), (17, 38)], fill=STEEL[2]); d.line([(13, 38), (17, 38)], fill=STEEL[1])
    for i in range(6):
        im.putpixel((5 + i * 2, 19 + (i * 5) % 14), RUST[3]); im.putpixel((22 - i * 2, 21 + (i * 3) % 12), RUST[3])
    d.line([(10, 22), (11, 27)], fill=BLOOD[2]); d.line([(15, 26), (16, 29)], fill=BLOOD[3])
    return finish2(im, (15, 38), (26, 5, 1, -1))

def bedframe(seed):
    W_, H_ = 32, 42; im = blank(W_, H_); yy, xx = grid(W_, H_); d = ImageDraw.Draw(im)
    spring = mask_of(W_, H_, lambda dd: dd.polygon([(3, 14), (28, 14), (30, 30), (1, 30)], fill=255)); put(im, spring, 0.3 + (((xx + yy) % 4) < 1) * 0.35 + (_noise(W_, H_, 3, seed) - 0.5) * 0.2, RUST)
    for x in range(4, 30, 4): d.line([(x, 15), (x - 1, 29)], fill=RUST[1])
    for (x, y0, y1) in ((1, 8, 36), (30, 8, 36)): d.line([(x, y0), (x, y1)], fill=STEEL[2]); d.line([(x + 1, y0), (x + 1, y1)], fill=STEEL[0])
    d.line([(1, 8), (30, 8)], fill=STEEL[3]); d.line([(1, 9), (30, 9)], fill=STEEL[1]); d.line([(1, 31), (30, 31)], fill=STEEL[2])
    for (x0, y0) in ((6, 15), (22, 15), (4, 25), (24, 25)):                                    # leather restraints
        d.rectangle([x0, y0, x0 + 3, y0 + 7], fill=ramp("#1a110c", "#2c1c14", "#46301f")[1]); d.line([(x0, y0), (x0 + 3, y0)], fill=(90, 66, 44, 255)); im.putpixel((x0 + 1, y0 + 4), (170, 150, 110, 255))
    for x, y in ((6, 20), (22, 18), (8, 26)): d.line([(x, y), (x + 2, y + 3)], fill=BLOOD[3])
    stain(im, spring, seed + 2, 0.3, 14, 22, 6)
    return finish2(im, (16, 40), (30, 5, 1, -1))

def ivstand(seed):
    W_, H_ = 16, 54; im = blank(W_, H_); d = ImageDraw.Draw(im)
    d.line([(8, 6), (8, 48)], fill=STEEL[2]); d.line([(9, 6), (9, 48)], fill=STEEL[0]); d.line([(3, 49), (14, 49)], fill=STEEL[2]); d.line([(5, 52), (12, 52)], fill=STEEL[1]); d.line([(3, 49), (5, 52)], fill=STEEL[1]); d.line([(14, 49), (12, 52)], fill=STEEL[1])
    d.line([(8, 5), (3, 3)], fill=STEEL[3]); d.line([(9, 5), (13, 3)], fill=STEEL[3])
    bag = mask_of(W_, H_, lambda dd: dd.rounded_rectangle([1, 6, 6, 20], 2, fill=255)); yy, xx = grid(W_, H_); put(im, bag, 0.35 + (xx - 1) * 0.07 - (yy - 6) / 14 * 0.2, BLOOD)
    im.putpixel((2, 8), (200, 110, 100, 255)); im.putpixel((3, 8), (170, 70, 64, 255))
    d.line([(4, 20), (5, 30), (8, 36), (12, 44)], fill=BLOOD[2])
    for y in (30, 40): im.putpixel((5, y), BLOOD[4])
    return finish2(im, (8, 52), (14, 4, 1, -1), tall=True)

def hangbag(seed):
    W_, H_ = 22, 74; im = blank(W_, H_); yy, xx = grid(W_, H_); d = ImageDraw.Draw(im)
    d.line([(11, 0), (11, 14)], fill=STEEL[2]); d.line([(12, 0), (12, 14)], fill=STEEL[0])
    for y in range(2, 14, 3): d.ellipse([10, y, 13, y + 2], outline=STEEL[3])
    d.arc([6, 12, 17, 20], 180, 360, fill=STEEL[3])
    bag = mask_of(W_, H_, lambda dd: dd.polygon([(5, 18), (17, 18), (20, 36), (19, 58), (14, 66), (8, 66), (3, 58), (2, 36)], fill=255))
    cx = (xx - 11) / 9.0; put(im, bag, 0.45 - 0.35 * cx + 0.12 * np.sin(yy * 0.35 + xx * 0.2) + (_noise(W_, H_, 3, seed) - 0.5) * 0.25, BAG)
    d.line([(11, 20), (11, 64)], fill=BAG[0]); d.line([(12, 20), (12, 64)], fill=BAG[4])                      # the zip, down the middle
    for y in range(22, 62, 4): im.putpixel((11, y), (150, 150, 155, 255))
    bulge = mask_of(W_, H_, lambda dd: (dd.ellipse([6, 24, 12, 31], fill=255), dd.ellipse([9, 56, 15, 63], fill=255))); put(im, bulge, 0.7 - (xx - 6) * 0.05, BAG)   # a shape inside: a face pressing out, a pair of feet
    stain(im, bag, seed + 2, 0.45, 11, 52, 7)
    for y in range(66, 72): im.putpixel((11, y), BLOOD[3]) if y % 2 == 0 else None
    d.ellipse([8, 70, 15, 73], fill=BLOOD[1])
    return finish2(im, (11, 71), (16, 4, 1, 0), tall=True)


import os
_HERE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "morgue_pl")
# name: (picked take, width in sprite px at scale 2 (32 px a tile), pivot y as the share of the height, shadow, tall)
PICKS = {"gurney": (12, 40, 0.93, (30, 6, 1, -1), False), "bodybag": (11, 38, 0.9, (28, 5, 1, -1), False), "wheelchair": (12, 32, 0.93, (26, 5, 1, -1), False),
         "bedframe": (12, 36, 0.95, (30, 5, 1, -1), False), "ivstand": (12, 16, 0.97, (14, 4, 1, -1), True), "hangbag": (12, 24, 0.97, (16, 4, 1, 0), True)}

def _prop(name):
    take, wpx, py, shadow, tall = PICKS[name]; src = Image.open(os.path.join(_HERE, f"{name}_{take}.png")).convert("RGBA")
    a = np.array(src); ys, xs = np.where(a[..., 3] > 0); src = src.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
    h = max(2, round(src.height * wpx / src.width)); im = src.resize((wpx, h), Image.BOX)
    arr = np.array(im); al = arr[..., 3] >= 110
    q = Image.fromarray(arr[..., :3]).quantize(colors=28, method=Image.MEDIANCUT, dither=Image.NONE).convert("RGB"); out = np.dstack([np.array(q), np.where(al, 255, 0).astype(np.uint8)]); out[~al] = 0
    im = Image.fromarray(out.astype(np.uint8), "RGBA")
    return finish2(im, (im.width // 2, int(im.height * py)), shadow, tall=tall)

def gurney(seed): return _prop("gurney")
def bodybag(seed): return _prop("bodybag")
def wheelchair(seed): return _prop("wheelchair")
def bedframe(seed): return _prop("bedframe")
def ivstand(seed): return _prop("ivstand")
def hangbag(seed): return _prop("hangbag")

NEW = {"gurney": gurney, "bodybag": bodybag, "wheelchair": wheelchair, "bedframe": bedframe, "ivstand": ivstand, "hangbag": hangbag}
VARIANTS = {k: 1 for k in NEW}

if __name__ == "__main__":
    import ground
    T = 16; W_, H_ = T * 12, T * 6
    bg = blank(W_, H_); g = ground.GROUNDS["lab"](3) if "lab" in ground.GROUNDS else ground.GROUNDS["concrete"](3)
    for ty in range(0, H_, g.height):
        for tx in range(0, W_, g.width): bg.paste(g, (tx, ty))
    big = bg.resize((W_ * 2, H_ * 2), Image.NEAREST); items = []
    for i, nm in enumerate(NEW): items.append((nm, NEW[nm](1000), 1 + i * 2, 4))
    for nm, p, tx, ty in items:
        x, y = tx * T + T // 2, ty * T + T
        if p.shadow:
            w, h, dx, dy = p.shadow; w2, h2 = w * 2, h * 2; big.alpha_composite(shadow_ellipse(w2, h2), (int(x * 2 + dx * 2 - w2 / 2), int(y * 2 + dy * 2 - h2 / 2)))
        big.alpha_composite(p.im, (int(x * 2 - p.pivot[0]), int(y * 2 - p.pivot[1])))
    big = big.resize((big.width * 3, big.height * 3), Image.NEAREST); big.convert("RGB").save("/tmp/morgue_props_preview.png"); print(big.size)
