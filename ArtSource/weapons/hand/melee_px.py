"""RT 7v: melee weapons pixelled by hand (Yan, after the RT 7u attempt: "all of them are weird looking"). Each sprite is a grid
of letters, one per pixel, typed here: the silhouette traced from his reference at game size (ArtSource/refs/melee/), every
pixel then placed by hand, colours picked from the reference. The grids are the inside only; the #141617 outline is added round
them. The menu sprite (64x32) is drawn first and the in-hand one (32x16) from it, keeping the same features.
Handle on the left, head on the right (the game's convention).
  python3 ArtSource/weapons/hand/melee_px.py [--install]   -> ArtSource/_out/melee_px/ (or the game's Weapons folder)
                                                            + ArtSource/_out/melee_px_sheet.png"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
WEAP = os.path.join(ROOT, "Assets/Resources/Sprites/Weapons")
K = "#141617"

BAT = {"l": "#e6b57b", "f": "#f9e5cc", "m": "#d5a171", "d": "#c08658", "e": "#a06a48",       # wood, light -> dark
       "g": "#7e5e4a", "G": "#5a4034",                                                       # end grain
       "t": "#ebe2d1", "T": "#cbb59d", "S": "#a8937c"}                                       # tape

BAT_MENU = [
    "...............................llllllllllllllllllllllllllee",
    ".ll.................lllllllllllffffffffffffffffffffffffffgG",
    "lfflttTtttTtttTtlllfffffffffffmmmmmmlllllllllllllldddllllgG",
    "mmmmtTtttTtttTttmmmmmmmmmmmmmmmmmmmmmmmmdddddmmmmdmmmdmmmgG",
    "ddddSTTTSTTTSTTTSddddddddddddddddddddmmmmmmmmmmmmmdddmmmmgG",
    ".ee.......................eeeeeeeeeeeddddddddddddddddddddgG",
    ".....................................eeeeeeeeeeeeeeeeeeeeee",
]
BAT_HAND = [
    "l.........llllllllffffffllle",
    "mtTtTtmmmmddddddmmmmddmmmmmg",
    "d...............ddddddddddde",
]

HAMMER = {"0": "#2e2935", "1": "#52555e", "2": "#757883", "3": "#a0a4af", "4": "#c8cad2",   # steel
          "a": "#6a4c3e", "b": "#7d5340", "c": "#9d714e", "d": "#cba880",                   # wood
          "n": "#0e1225", "o": "#21273a", "p": "#293144", "q": "#44516a"}                   # rubber grip

HAMMER_MENU = [
    "...........................................23443......",
    "............................................123442....",
    "..............................................123342..",
    "...............................................123341.",
    "...............................................1223341",
    "...............................................1223331",
    "...............................................12223321",
    "...............................................12222321",
    "............................................34432222221",
    "cqqqqqqqqqqqqqqqqqqqqqq.....................32223222221",
    "dpnpnpnpnpnpnpnpnpnpnpnccccccccbcccccccccccc32222221c21",
    "cpnpnpnpnpnpnpnpnpnpnpnddddcdddddddddcdddddd32232221d21",
    "cpnpnpnpnpnpnpnpnpnpnpncbccccccccbbccccccbcc32222221c21",
    "boooooooooooooooooooooaaaaaaaaaaaaaaaaaaaaaa32222221a21",
    "annnnnnnnnnnnnnnnnnnnnn.....................21111111110",
    "................................................211110..",
    ".................................................2431...",
    ".................................................2431...",
    ".................................................2431...",
    "...............................................3444432..",
    "...............................................1244321..",
    "...............................................1244321..",
    "...............................................0122210..",
]
HAMMER_HAND = [
    "...............34...",
    "................234.",
    ".................231",
    "qqpqpqpqccccccccd321",
    "nnnnnnnnaaaaaaaa3221",
    ".................210",
    ".................32.",
    "................3442",
    "................1221",
]

SET = {"WoodenBaseballBat": (BAT, BAT_MENU, BAT_HAND, "baseball_bat"), "Hammer": (HAMMER, HAMMER_MENU, HAMMER_HAND, "hammer")}

def hx(h): v = int(h.lstrip("#"), 16); return ((v >> 16) & 255, (v >> 8) & 255, v & 255, 255)

def paint(grid, pal, w, h):
    """The grid's pixels, outlined, centred on a w x h canvas."""
    gw, gh = max(map(len, grid)), len(grid)
    im = Image.new("RGBA", (gw + 2, gh + 2)); px = im.load()
    for y, row in enumerate(grid):
        for x, ch in enumerate(row):
            if ch != ".": px[x + 1, y + 1] = hx(pal[ch])
    filled = lambda x, y: 0 <= x < im.width and 0 <= y < im.height and px[x, y][3] and px[x, y] != hx(K)
    for y in range(im.height):
        for x in range(im.width):
            if not px[x, y][3] and any(filled(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))): px[x, y] = hx(K)
    assert im.width <= w and im.height <= h, (im.size, w, h)
    out = Image.new("RGBA", (w, h)); out.alpha_composite(im, ((w - im.width) // 2, (h - im.height) // 2))
    return out

if __name__ == "__main__":
    install = "--install" in sys.argv
    dest = WEAP if install else os.path.join(ROOT, "ArtSource/_out/melee_px"); os.makedirs(dest, exist_ok=True)
    for name, (pal, menu, hand, _) in SET.items():
        paint(hand, pal, 32, 16).save(os.path.join(dest, f"weapon_{name}.png")); paint(menu, pal, 64, 32).save(os.path.join(dest, f"weapon_{name}_hd.png"))
    # review sheet per weapon: the reference | menu x6 | in hand x6, then the in-hand sprite x6 next to the Tactical Hatchet and a
    # character at the same scale (a size check laid side by side, not a game render)
    f = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 16); bg = (46, 42, 34, 255)
    man = Image.open(os.path.join(ROOT, "Assets/Resources/Sprites/armor_atlas.png")).convert("RGBA").crop((0, 0, 16, 16))
    man = man.resize((32, 32), Image.NEAREST)                     # a weapon sprite pixel is half a character pixel
    hatchet = Image.open(os.path.join(WEAP, "weapon_TacticalHatchet.png")).convert("RGBA")
    rows = len(SET); sheet = Image.new("RGBA", (1500, 40 + rows * 230), (27, 29, 31, 255)); d = ImageDraw.Draw(sheet)
    for x, t in ((10, "reference"), (360, "menu (64x32) x6"), (760, "in hand (32x16) x6"), (1000, "size check: hatchet in game, new, x6")):
        d.text((x, 10), t, font=f, fill=(235, 235, 235))
    for r, (name, (pal, menu, hand, ref)) in enumerate(SET.items()):
        y = 40 + r * 230; d.text((10, y), name, font=f, fill=(200, 200, 190))
        im = Image.open(os.path.join(ROOT, "ArtSource/refs/melee", ref + ".png")).convert("RGBA"); im.thumbnail((330, 190)); sheet.alpha_composite(im, (10, y + 24))
        for spr, x in ((paint(menu, pal, 64, 32), 360), (paint(hand, pal, 32, 16), 760)):
            t = Image.new("RGBA", spr.size, bg); t.alpha_composite(spr); sheet.alpha_composite(t.resize((spr.width * 6, spr.height * 6), Image.NEAREST), (x, y + 24))
        strip = Image.new("RGBA", (80, 32), bg); strip.alpha_composite(hatchet, (0, 0)); strip.alpha_composite(paint(hand, pal, 32, 16), (0, 16))
        strip.alpha_composite(man, (44, 0))
        sheet.alpha_composite(strip.resize((480, 192), Image.NEAREST), (1000, y + 24))
    out = os.path.join(ROOT, "ArtSource/_out/melee_px_sheet.png"); sheet.save(out); print("->", dest, "+", out)
