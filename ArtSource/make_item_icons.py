"""Pack 1 (Yan: "a real [inventory] like the case from re4 using our detailed sprites for items"): the pack's item icons, 16 px a
grid cell, so an icon is its footprint (Pack.Size) x 16: a 2x1 medkit is 32x16. Locked style: 1 px #141617 outline, flat 2-3 tones
(lit top/left, shade bottom/right). Weapons use their 64x32 _hd sprites; artifacts their own sprite where there is one.
- Sprites/Items/icon_<id>.png for every consumable, icon_<type>.png for the rest (loot, junk, mutantPart, key, cash, detector,
  armorMod, artifact_<kind>), icon_backpack_<id>.png in each bag's colour;
- Sprites/armor_icon_atlas.png: every suit "without a person" (Yan: "turning off the eyes in the suits and putting the gloves in a
  resting position above them"), 48x48 cells (3x3), in armor_atlas.png's order.
    python3 ArtSource/make_item_icons.py"""
import os, re
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.join(HERE, "..")
SPR = os.path.join(ROOT, "Assets", "Resources", "Sprites"); OUT = os.path.join(SPR, "Items")
K = (20, 22, 23, 255)
def hexc(h, a=255): h = h.lstrip('#'); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)
def sc(c, f): return tuple(max(0, min(255, int(v * f))) for v in c[:3]) + (255,)
def outline(im):
    out = im.copy(); a = im.load(); o = out.load(); w, h = im.size
    for y in range(h):
        for x in range(w):
            if not a[x, y][3] and any(0 <= x + dx < w and 0 <= y + dy < h and a[x + dx, y + dy][3] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                o[x, y] = K
    return out
class C:
    """A tiny canvas: shaded boxes and lines on the pixel grid, outlined at the end."""
    def __init__(s, w, h): s.im = Image.new("RGBA", (w, h), (0, 0, 0, 0)); s.p = s.im.load()
    def px(s, x, y, c):
        if 0 <= x < s.im.width and 0 <= y < s.im.height: s.p[x, y] = c
    def box(s, x0, y0, x1, y1, col, lit=1.25, dark=0.72):
        c = hexc(col) if isinstance(col, str) else col
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                s.px(x, y, sc(c, lit) if (y == y0 or x == x0) else sc(c, dark) if (y == y1 or x == x1) else c)
    def line(s, x0, y0, x1, y1, col):
        c = hexc(col) if isinstance(col, str) else col; n = max(abs(x1 - x0), abs(y1 - y0), 1)
        for i in range(n + 1): s.px(round(x0 + (x1 - x0) * i / n), round(y0 + (y1 - y0) * i / n), c)
    def disc(s, cx, cy, r, col):
        c = hexc(col)
        for y in range(cy - r, cy + r + 1):
            for x in range(cx - r, cx + r + 1):
                d = (x - cx) ** 2 + (y - cy) ** 2
                if d <= r * r + r: s.px(x, y, sc(c, 1.25) if (x - cx) + (y - cy) < -r * 0.6 else sc(c, 0.72) if (x - cx) + (y - cy) > r * 0.6 else c)
    def done(s): return outline(s.im)

def medkit(case, cross):
    c = C(32, 16); c.box(4, 4, 27, 13, case); c.box(13, 2, 18, 3, "#4c5258")                  # case and handle
    c.box(14, 6, 17, 11, cross, 1, 1); c.box(11, 7, 20, 10, cross, 1, 1); c.box(12, 8, 19, 9, cross, 1, 1); return c.done()
def syringe(fluid):
    c = C(16, 16); c.line(2, 13, 4, 11, "#4c5258"); c.line(1, 12, 3, 14, "#4c5258")             # plunger
    for i in range(6): c.line(5 + i, 10 - i, 6 + i, 11 - i, fluid); c.line(4 + i, 10 - i, 5 + i, 9 - i, "#d5dadd")
    c.line(11, 4, 14, 1, "#a9b0b5"); return c.done()
def grenade(body, band):
    c = C(16, 16); c.disc(8, 9, 5, body); c.box(6, 2, 9, 4, "#727980"); c.line(10, 3, 12, 6, "#a9b0b5"); c.line(4, 9, 12, 9, band); return c.done()
ICONS = {
    "basicMedkit": lambda: medkit("#d8d4c4", "#b02a2a"),
    "militaryMedkit": lambda: medkit("#6b7350", "#e4dcc8"),
    "scientificMedkit": lambda: medkit("#8aa0b0", "#3a9a52"),
    "basicStim": lambda: syringe("#8fd06a"), "militaryStim": lambda: syringe("#e0b83a"), "scientificStim": lambda: syringe("#6fc8e8"),
    "splint": lambda: (lambda c: (c.box(2, 4, 29, 6, "#a0602f"), c.box(2, 9, 29, 11, "#a0602f"), c.box(8, 3, 10, 12, "#d8d4c4"), c.box(20, 3, 22, 12, "#d8d4c4"), c.done())[-1])(C(32, 16)),
    "bandage": lambda: (lambda c: (c.disc(7, 8, 5, "#e4dcc8"), c.disc(7, 8, 2, "#b8ae96"), c.box(11, 9, 14, 11, "#e4dcc8"), c.done())[-1])(C(16, 16)),
    "radBlockPill": lambda: (lambda c: (c.box(4, 5, 11, 14, "#c46a28"), c.box(4, 2, 11, 4, "#e4e9eb"), c.box(5, 8, 10, 11, "#e4dcc8", 1, 1), c.done())[-1])(C(16, 16)),
    "mre": lambda: (lambda c: (c.box(3, 3, 28, 13, "#7d5a3e"), c.box(3, 3, 28, 4, "#5a3f2b", 1, 1), c.box(8, 6, 23, 10, "#c9b28a", 1, 1), c.line(10, 8, 21, 8, "#5a3f2b"), c.done())[-1])(C(32, 16)),
    "fragGrenade": lambda: grenade("#5a6438", "#3c4128"),
    "smokeGrenade": lambda: (lambda c: (c.box(4, 4, 11, 14, "#8a8f94"), c.box(4, 7, 11, 8, "#c9cdd1", 1, 1), c.box(5, 2, 9, 3, "#4c5258"), c.line(10, 2, 12, 5, "#a9b0b5"), c.done())[-1])(C(16, 16)),
    "flashbang": lambda: (lambda c: (c.box(4, 4, 11, 14, "#33373b"), [c.px(x, y, hexc("#a9b0b5")) for x in (6, 9) for y in (7, 10, 13)], c.box(5, 2, 9, 3, "#727980"), c.line(10, 2, 12, 5, "#a9b0b5"), c.done())[-1])(C(16, 16)),
    "fieldRepairKit": lambda: (lambda c: (c.box(3, 9, 28, 27, "#8a3a2a"), c.box(11, 5, 20, 8, "#33373b"), c.box(3, 15, 28, 16, "#5a2418", 1, 1), c.line(8, 21, 23, 21, "#d5dadd"), c.box(6, 19, 9, 23, "#a9b0b5"), c.done())[-1])(C(32, 32)),
    "energyDrink": lambda: (lambda c: (c.box(4, 2, 11, 14, "#2e7ab0"), c.box(4, 2, 11, 3, "#a9b0b5", 1.1, 0.9), c.line(5, 8, 10, 8, "#f0d878"), c.line(5, 9, 10, 9, "#f0d878"), c.done())[-1])(C(16, 16)),
    # the rest by type
    "loot": lambda: (lambda c: (c.box(3, 5, 12, 13, "#7a6548"), c.line(3, 9, 12, 9, "#4c3a26"), c.line(7, 5, 7, 13, "#4c3a26"), c.done())[-1])(C(16, 16)),
    "junk": lambda: (lambda c: (c.box(2, 8, 9, 13, "#727980"), c.box(7, 4, 13, 10, "#8a6a4a"), c.line(3, 6, 6, 3, "#a9b0b5"), c.done())[-1])(C(16, 16)),
    "mutantPart": lambda: (lambda c: ([c.line(4 + i, 13, 9 + i, 3 + i // 2, "#e8e0c8") for i in range(3)], c.box(3, 11, 9, 14, "#7a4a46"), c.done())[-1])(C(16, 16)),
    "key": lambda: (lambda c: (c.disc(5, 8, 3, "#c9a63e"), c.box(8, 7, 14, 8, "#c9a63e", 1.1, 0.8), c.box(12, 9, 13, 11, "#c9a63e"), c.done())[-1])(C(16, 16)),
    "cash": lambda: (lambda c: (c.box(2, 5, 13, 11, "#6e8a5a"), c.box(5, 7, 10, 9, "#a8c890", 1, 1), c.box(6, 4, 8, 12, "#c9b28a"), c.done())[-1])(C(16, 16)),
    "detector": lambda: (lambda c: (c.box(3, 3, 28, 13, "#3a3e42"), c.box(6, 5, 17, 11, "#1c2a30"), c.line(8, 9, 15, 7, "#8fd06a"), c.disc(23, 8, 2, "#b02a2a"), c.done())[-1])(C(32, 16)),
    "armorMod": lambda: (lambda c: (c.box(3, 3, 28, 12, "#5a6778"), c.line(3, 7, 28, 7, "#3c4654"), [c.px(x, y, hexc("#a9b0b5")) for x in (6, 25) for y in (5, 10)], c.done())[-1])(C(32, 16)),
}
ARTIFACTS = {"Gravitational": "#6fb6e8", "Chemical": "#8fd06a", "Thermal": "#f2741c", "Electrical": "#fff27a", "Legendary": "#c06bff", "Psi": "#c06bff"}
def artifact(col):
    c = C(16, 16); c.disc(8, 8, 4, col); c.px(6, 6, hexc("#ffffff")); return c.done()
def backpack(col):
    c = C(32, 32); c.box(6, 6, 25, 28, col); c.box(9, 3, 22, 7, sc(hexc(col), 0.8)); c.box(9, 16, 22, 25, sc(hexc(col), 0.85)); c.line(9, 16, 22, 16, sc(hexc(col), 0.6))
    c.box(14, 18, 17, 20, "#727980"); return c.done()

def armor_icons():
    """The suit without a person: each armor cell with its eyes put out (the pair of pixels that sits mirrored on one row of the
    head and appears nowhere else there - lenses or bare eyes - takes the colour under it), drawn 2x in a 48x48 cell with the
    suit's two gloves laid above its shoulders."""
    atlas = Image.open(os.path.join(SPR, "armor_atlas.png")).convert("RGBA")
    idx = open(os.path.join(ROOT, "Assets", "Scripts", "GameData", "SpriteAtlasIndex.cs")).read()
    arr = lambda n: re.findall(r'"([^"]*)"', re.search(n + r'\s*=\s*\{(.*?)\};', idx, re.S).group(1))
    names, cuffs, gloves = arr(r'public static readonly string\[\] Armor'), arr('ArmorCuff'), arr('ArmorGlove')
    out = Image.new("RGBA", (16 * 48, (atlas.height // 16) * 48), (0, 0, 0, 0))
    for i in range(len(names)):
        cell = atlas.crop(((i % 16) * 16, (i // 16) * 16, (i % 16) * 16 + 16, (i // 16) * 16 + 16)); p = cell.load()
        head = [(x, y) for y in range(1, 8) for x in range(16) if p[x, y][3]]
        for y in range(1, 8):
            for x in range(8):
                a, b = p[x, y], p[15 - x, y]
                if not a[3] or a != b or a[:3] == K[:3]: continue
                lum = lambda c: 0.3 * c[0] + 0.59 * c[1] + 0.11 * c[2]
                below = p[x, y + 1]
                if 3 <= y <= 6 and sum(1 for (hx, hy) in head if p[hx, hy] == a) <= 4 and below[3] and abs(lum(a) - lum(below)) > 35:   # a small mirrored spot unlike the face under it: an eye
                    for ex in (x, 15 - x):
                        below = p[ex, y + 1]; p[ex, y] = below if below[3] and below[:3] != K[:3] else p[ex, y - 1]
        g = hexc(gloves[i]); cu = hexc(cuffs[i]); col = {"K": K, "c": cu, "l": sc(g, 1.45), "g": g, "d": sc(g, 0.65)}
        gl = Image.new("RGBA", (5, 4))
        for y, r in enumerate([".KKK.", "KcllK", "KcgdK", ".KKK."]):
            for x, ch in enumerate(r):
                if ch != ".": gl.putpixel((x, y), col[ch])
        icon = Image.new("RGBA", (48, 48), (0, 0, 0, 0))
        icon.alpha_composite(cell.resize((32, 32), Image.NEAREST), (8, 14))
        icon.alpha_composite(gl.rotate(90, expand=True).resize((8, 10), Image.NEAREST), (5, 3))            # gloves resting, fingers up
        icon.alpha_composite(gl.transpose(Image.FLIP_LEFT_RIGHT).rotate(-90, expand=True).resize((8, 10), Image.NEAREST), (35, 3))
        out.alpha_composite(icon, ((i % 16) * 48, (i // 16) * 48))
    out.save(os.path.join(SPR, "armor_icon_atlas.png"))
    return out

if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    made = {}
    for k, f in ICONS.items(): made[k] = f(); made[k].save(os.path.join(OUT, f"icon_{k}.png"))
    for t, col in ARTIFACTS.items(): made["artifact_" + t] = artifact(col); made["artifact_" + t].save(os.path.join(OUT, f"icon_artifact_{t}.png"))
    for bid, col in (("backpack_small", "#7d6e4e"), ("backpack_tourist", "#465a6e")): made[bid] = backpack(col); made[bid].save(os.path.join(OUT, f"icon_backpack_{bid}.png"))
    armor = armor_icons()
    # preview: every icon at 4x on the bag cloth, plus a row of armor icons
    sheet = Image.new("RGBA", (1400, 760), hexc("#1b1d1f")); x = y = 10; row_h = 0
    for k, im in made.items():
        big = im.resize((im.width * 4, im.height * 4), Image.NEAREST)
        if x + big.width > 1390: x = 10; y += row_h + 10; row_h = 0
        bg = Image.new("RGBA", big.size, hexc("#7d6e4e")); bg.alpha_composite(big); sheet.alpha_composite(bg, (x, y)); x += big.width + 10; row_h = max(row_h, big.height)
    y += row_h + 20
    for n, i in enumerate((0, 3, 17, 40, 115, 150, 66, 90, 130)):
        a = armor.crop(((i % 16) * 48, (i // 16) * 48, (i % 16) * 48 + 48, (i // 16) * 48 + 48)).resize((144, 144), Image.NEAREST)
        bg = Image.new("RGBA", a.size, hexc("#7d6e4e")); bg.alpha_composite(a); sheet.alpha_composite(bg, (10 + n * 152, y))
    sheet.save(os.path.join(HERE, "_out", "item_icons_preview.png")); print("icons:", len(made), "+ armor atlas")
