"""RT 7u: the melee weapons from Yan's references (ArtSource/refs/melee/*.png, his set of 2026-10-20), the same way as the Tactical
Hatchet (tactical_hatchet.py): cut the weapon off its grey background, turn it handle-left / head-right with the edge down (the
game's convention), cut the JPEG noise to a small palette, then reduce: each output pixel takes the most common colour under it
(transparent only when under 40% covered), and the dark outline is redrawn round the result. Two sizes from the same drawing:
the in-hand sprite (32x16, 2 sprite px per character px) and the menu one (64x32, filling its canvas).
In-hand lengths are set against the Tactical Hatchet's 20 px: knives ~15, the machete 26, two-handed weapons 28-30.
  python3 ArtSource/weapons/hand/melee_set.py [--install] [key ...]   -> ArtSource/_out/melee/weapon_<Key>.png + _hd.png
                                                             (--install: into Assets/Resources/Sprites/Weapons, after Yan approves)
                                                             + ArtSource/_out/melee_set_sheet.png (reference | in hand x10 | menu x5)"""
import os, sys, math, collections
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
REFS = os.path.join(ROOT, "ArtSource/refs/melee")
WEAP = os.path.join(ROOT, "Assets/Resources/Sprites/Weapons")
K = (20, 22, 23, 255)                                            # the outline every weapon sprite uses

SET = {   # key: (reference, in-hand length, head on the left in the reference, palette size, menu length)
    "HuntingKnife":        ("hunting_knife.png", 15, True, 10, 52),
    "SpetsnazSwitchblade": ("switchblade.png", 15, True, 10, 56),
    "SteppeMachete":       ("machete.png", 26, True, 9, 60),
    "SerratedTrenchKnife": ("serrated_trench_knife.png", 17, True, 12, 56),
    "Hatchet":             ("hatchet.png", 18, True, 10, 48),
    "FireAxe":             ("fire_axe.png", 28, True, 10, 60),
    "WoodenBaseballBat":   ("baseball_bat.png", 29, False, 9, 60),
    "Hammer":              ("hammer.png", 16, True, 10, 46),
    "Sledgehammer":        ("sledge_hammer.png", 30, True, 10, 58),
    "BarbedWireBat":       ("barbed_wire_bat.png", 30, True, 12, 60),
}

def cut(path):
    """The weapon without its flat grey background: colour far from the background (sampled along the border), cropped."""
    im = Image.open(path).convert("RGB"); w, h = im.size; px = im.load()
    border = [px[x, 0] for x in range(0, w, 7)] + [px[x, h - 1] for x in range(0, w, 7)] + [px[0, y] for y in range(0, h, 7)] + [px[w - 1, y] for y in range(0, h, 7)]
    bg = tuple(sorted(c[i] for c in border)[len(border) // 2] for i in range(3))
    out = Image.new("RGBA", (w, h)); o = out.load()
    for y in range(h):
        for x in range(w):
            c = px[x, y]
            if sum(abs(c[i] - bg[i]) for i in range(3)) > 42: o[x, y] = c + (255,)
    # drop specks (the reference's soft shadow, JPEG halo): keep pixels with enough solid neighbours
    a = out.copy(); ap = a.load()
    for y in range(1, h - 1):
        for x in range(1, w - 1):
            if ap[x, y][3] and sum(1 for dx in (-1, 0, 1) for dy in (-1, 0, 1) if ap[x + dx, y + dy][3]) < 5: o[x, y] = (0, 0, 0, 0)
    return out.crop(out.getbbox())

def palette(img, n):
    """n colours (the darkest kept for the outline), alpha untouched."""
    rgb = Image.new("RGB", img.size, (0, 0, 0)); rgb.paste(img, mask=img.split()[3])
    q = rgb.quantize(colors=n, method=Image.Quantize.MEDIANCUT).convert("RGB")
    q.putalpha(img.split()[3])
    return q

def reduce(img, length, w, h):
    """Each output pixel = the most common colour of the img pixels it covers, the weapon `length` px long, centred."""
    scale = img.width / length; px = img.load()
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
    """The reference's own near-black edge becomes the game's outline colour; a ring of it is added where the shape ends."""
    px = im.load(); w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a and r + g + b < 95: px[x, y] = K
    add = [(x, y) for y in range(h) for x in range(w) if not px[x, y][3]
           and any(0 <= x + dx < w and 0 <= y + dy < h and px[x + dx, y + dy][3] and px[x + dx, y + dy] != K for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for p in add: px[p] = K
    return im

def make(key):
    ref, length, head_left, n, menu_len = SET[key]
    img = cut(os.path.join(REFS, ref))
    if head_left: img = img.transpose(Image.FLIP_LEFT_RIGHT)    # handle left, head right (edge stays down)
    img = palette(img, n)
    hand = outline(reduce(img, length, 32, 16))
    menu = outline(reduce(img, menu_len, 64, 32))               # the menu fills its canvas: enough pixels for the details
    return img, hand, menu

if __name__ == "__main__":
    install = "--install" in sys.argv
    keys = [a for a in sys.argv[1:] if a != "--install"] or list(SET)
    dest = WEAP if install else os.path.join(ROOT, "ArtSource/_out/melee"); os.makedirs(dest, exist_ok=True)
    rows = []
    for k in keys:
        img, hand, menu = make(k)
        hand.save(os.path.join(dest, f"weapon_{k}.png")); menu.save(os.path.join(dest, f"weapon_{k}_hd.png"))
        print(k, "in hand", hand.getbbox(), "menu", menu.getbbox())
        rows.append((k, img, hand, menu))
    # review sheet: the reference (cut), in hand x10, menu x5 (the same screen size)
    f = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 16); bg = (46, 42, 34, 255)
    rh = 16 * 10 + 40
    sheet = Image.new("RGBA", (340 + 330 + 330 + 40, 40 + rh * len(rows)), (27, 29, 31, 255)); d = ImageDraw.Draw(sheet)
    for c, t in ((10, "reference"), (360, "in the hand (32x16) x10"), (700, "menu (64x32) x5")): d.text((c, 10), t, font=f, fill=(235, 235, 235))
    for r, (k, img, hand, menu) in enumerate(rows):
        y = 40 + r * rh
        d.text((10, y), k, font=f, fill=(200, 200, 190))
        t = img.copy(); t.thumbnail((320, 140)); sheet.alpha_composite(t, (10, y + 22))
        for im, s, x in ((hand, 10, 360), (menu, 5, 700)):
            tile = Image.new("RGBA", im.size, bg); tile.alpha_composite(im)
            sheet.alpha_composite(tile.resize((im.width * s, im.height * s), Image.NEAREST), (x, y + 20))
    os.makedirs(os.path.join(ROOT, "ArtSource/_out"), exist_ok=True)
    sheet.save(os.path.join(ROOT, "ArtSource/_out/melee_set_sheet.png")); print("sheet ->", "ArtSource/_out/melee_set_sheet.png")
