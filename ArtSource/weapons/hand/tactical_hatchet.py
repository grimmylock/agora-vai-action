"""Tactical Hatchet from Yan's own pixel art (ArtSource/refs/tactical_hatchet/yan_hatchet.png: his drawing, recovered from his
screenshot - pixel grid 5.76 x 5.375 screen px - and his palette, 12 colours by k-means). His drawing is diagonal (head top left,
handle down to the right); the in-hand sprite lies along the aim, so it is turned until the handle points right with the head at
the right end (blade up, spike down), then reduced: each output pixel takes the most common colour under it (transparent only
when under 40% covered), and the dark outline is redrawn round the result.
  python3 ArtSource/weapons/hand/tactical_hatchet.py  -> weapon_TacticalHatchet.png (in hand, 32x16) + _hd.png (menus, 64x32)
                                                         + ArtSource/_out/tactical_hatchet_sheet.png (his | in hand | menu)"""
import os, math, collections
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
WEAP = os.path.join(ROOT, "Assets/Resources/Sprites/Weapons")
SRC = os.path.join(ROOT, "ArtSource/refs/tactical_hatchet/yan_hatchet.png")
BUTT, HEAD = (75.0, 64.0), (24.0, 22.0)      # handle end and head centre in his drawing
K = (20, 22, 23, 255)

def turned(up=8):
    art = Image.open(SRC).convert("RGBA")
    ang = math.degrees(math.atan2(HEAD[1] - BUTT[1], HEAD[0] - BUTT[0]))          # the handle's direction, y down
    big = art.resize((art.width * up, art.height * up), Image.NEAREST)
    return big.rotate(ang, expand=True, resample=Image.NEAREST), up             # CCW by the angle: handle -> pointing right

def merge_darks(img):
    """His handle uses six near-black shades; at sprite size they turn to speckle - fold them into two."""
    px = img.load()
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, a = px[x, y]
            if a and r + g + b < 120: px[x, y] = (8, 9, 11, 255) if r + g + b < 40 else (24, 24, 29, 255)
    return img

def reduce(img, scale, w, h):
    """Each output pixel = the most common colour of the img pixels it covers (scale img px per output px), centred."""
    bbox = img.getbbox(); img = img.crop(bbox); px = img.load()
    ow, oh = math.ceil(img.width / scale), math.ceil(img.height / scale)
    out = Image.new("RGBA", (w, h)); o = out.load()
    x0, y0 = (w - ow) // 2, (h - oh) // 2
    for j in range(oh):
        for i in range(ow):
            cnt = collections.Counter(); n = 0
            for y in range(int(j * scale), min(img.height, int((j + 1) * scale))):
                for x in range(int(i * scale), min(img.width, int((i + 1) * scale))):
                    n += 1; c = px[x, y]
                    if c[3]: cnt[c] += 1
            if n and sum(cnt.values()) >= 0.4 * n and 0 <= x0 + i < w and 0 <= y0 + j < h:
                o[x0 + i, y0 + j] = cnt.most_common(1)[0][0]
    return out

def outline(im):
    px = im.load(); w, h = im.size; add = []
    for y in range(h):
        for x in range(w):
            if px[x, y][3]: continue
            if any(0 <= x + dx < w and 0 <= y + dy < h and px[x + dx, y + dy][3] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))): add.append((x, y))
    for p in add: px[p] = K
    return im

if __name__ == "__main__":
    img, up = turned(); img = merge_darks(img)
    length = img.getbbox()[2] - img.getbbox()[0]                                 # in up-scaled px
    b = outline(reduce(img, length / 28.0, 32, 16))                              # ~26 px long in hand (knives 17-24, rifles 28)
    a = outline(reduce(img, length / 56.0, 64, 32))
    b.save(os.path.join(WEAP, "weapon_TacticalHatchet.png")); a.save(os.path.join(WEAP, "weapon_TacticalHatchet_hd.png"))
    src = Image.open(SRC).convert("RGBA"); bg = (46, 42, 34, 255)
    def tile(im, s): t = Image.new("RGBA", im.size, bg); t.alpha_composite(im); return t.resize((im.width * s, im.height * s), Image.NEAREST)
    parts = [tile(src, 4), tile(b, 10), tile(a, 5)]
    sheet = Image.new("RGBA", (sum(p.width for p in parts) + 40, max(p.height for p in parts) + 20), (27, 29, 31, 255)); x = 10
    for p in parts: sheet.alpha_composite(p, (x, 10)); x += p.width + 10
    os.makedirs(os.path.join(ROOT, "ArtSource/_out"), exist_ok=True)
    sheet.save(os.path.join(ROOT, "ArtSource/_out/tactical_hatchet_sheet.png")); print("ok", b.getbbox(), a.getbbox())
