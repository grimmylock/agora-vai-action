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
    "..................................lllllllllllllllllllllllee",
    ".ll...................llllllllllllfffffffffffffffffffffffgG",
    "lfflttTtttTtttTttlllllfffffffffffflllllllllllllllldddllllgG",
    "mmmmtTtttTtttTtttmmmmmmmmmmmmmmmmmmmmmmmdddddmmmmdmmmdmmmgG",
    "ddddSTTTSTTTSTTTSdddddddddddddddddmmmmmmmmmmmmmmmmdddmmmmgG",
    ".ee...................eeeeeeeeeeeedddddddddddddddddddddddgG",
    "..................................eeeeeeeeeeeeeeeeeeeeeeeee",
]
BAT_HAND = [
    "l...........lllllfffffflllle",
    "mtTtTtmmmmmmmmmmmmmddmmmmmmg",
    "d...........ddddddddddddddde",
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

# the other nine (RT 7v, Yan: "do the rest using the same logic"); the Ritual Blade has no reference: its old sprite's design
HUNTINGKNIFE = {"a": "#4f3022", "b": "#7a4b30", "c": "#9f6a46", "d": "#c49064", "e": "#dcb48a", "0": "#3c3c44", "1": "#5a5a62", "2": "#7a7a82", "3": "#a9a9b0", "4": "#e4e4ea", "K": "#141617"}
HUNTINGKNIFE_MENU = [
    "...........................43.............................",
    "...........................31.2.2.2.2.....................",
    "..........ddddddddddddddddd31333333333333333333...........",
    "..33ddddddcccccccccdddccccc312222222222222222224444.......",
    ".322cccccbbbbcccccccbbb4ccc312222222222222222222222444444.",
    ".2K2cbc4cccbbbeeebcccbb1bbc312222222222222222222222222224.",
    ".222ccc1bbbaaaaaaabbbaaaaab31331111111111111111133433334..",
    ".122bbbaaaa.......aaa.....a3133333333333334433333433344...",
    "..11aaa....................31444444444444444444444444.....",
    "...........................31.............................",
    "...........................31.............................",
    "...........................10.............................",
]
HUNTINGKNIFE_HAND = [
    ".......4.2.2...",
    "3ddeddd32222443",
    "1bbbbbb244444..",
    ".......0.......",
]

SPETSNAZSWITCHBLADE = {"0": "#3c3d47", "1": "#62646f", "2": "#9193a0", "3": "#bfc2cc", "4": "#eceef2", "x": "#0e0c12", "y": "#1f1a22", "z": "#30272e", "w": "#4a3d46", "r": "#5a2420", "K": "#141617"}
SPETSNAZSWITCHBLADE_MENU = [
    "...........................4..............................",
    ".442......................3...............................",
    "3331rrrrrrrrrrrrrrrrrrrrr44444333333333443333344333333....",
    "2221zzzz222zzz43wzzzzz2wz33213333333334433333443333333333.",
    "2221yy2y111yyy21yyyyyyyyy2220222222222222222222222244444..",
    "1221rrrrrrrrrrrrrrrrrrrrr11111441444444444444444444.......",
    ".110......................1...............................",
    "...........................0..............................",
]
SPETSNAZSWITCHBLADE_HAND = [
    ".......4.......",
    "3zzz3zz34444433",
    "1xxxxxx222222..",
    ".......1.......",
]

STEPPEMACHETE = {"x": "#0b0b14", "y": "#1c1c24", "z": "#2c2c34", "w": "#4a4a55", "0": "#17181e", "1": "#24252d", "2": "#33343d", "3": "#4c4d57", "S": "#6a6b75", "L": "#b4b4bc", "r": "#8a8b95", "K": "#141617"}
STEPPEMACHETE_MENU = [
    ".................S................3333333333333333333333333L",
    ".wwwwwwwwwwwwwwww3333333333333333322232223222222222222222SL.",
    "zKzzzrzzzrzzzrzzz322222222222222222222322232222222222222SL..",
    "yyyyyyyyyyyyyyyyy21111111111111111111113111311111111SSSSL...",
    ".xxxxxxxxxxxxxxxx2SSSSSS111111111111111131113SSSSSSSLLLL....",
    "..............yyx1LLLLLLSSSSSSSSSSSSSSSSSSSSLLLLLLLL........",
    "................xx......LLLLLLLLLLLLLLLLLLLL................",
    "............................................................",
]
STEPPEMACHETE_HAND = [
    ".......S.......33333333333",
    "zzzrzzz33333333113111111L.",
    "yxxxxxx2LL111111113LLLLL..",
    ".......1..LLLLLLLLL.......",
    "..........................",
]

SERRATEDTRENCHKNIFE = {"0": "#2e3036", "1": "#4a4e56", "2": "#6a6e76", "3": "#9aa0a8", "4": "#d8dce0", "b": "#4a3618", "c": "#7a5a20", "d": "#a8842c", "e": "#d4b048", "f": "#f0d878", "g": "#2a2a2e", "h": "#4a4a50", "i": "#6e6e74", "j": "#9a9aa0", "K": "#141617"}
SERRATEDTRENCHKNIFE_MENU = [
    ".......................f..................................",
    "......................e...................................",
    "......................d...................................",
    "......................d.2.2.2.2.2.2.2.2.2.2...............",
    "....................fec3333333333333333333333.............",
    "...feihjjihjjihjjihjed.11111112111111121111114444.........",
    "ijiedhjjihjjihjjihjjdd.111121111112111111211111114444.....",
    "jKhddihggihggihggihgdd.3300000000000000000000003333334444.",
    ".h.ddhggihggihggihggdc.33333333333333333443333344333344444",
    "...dcgghggghggghggghdc.333333333333333333343344444444.....",
    "...eeeeeeeeeeeeeeeeeeee4444444444444444444444.............",
    "...db_bddb_bddb_bddb_bd...................................",
    "...c___cc___cc___cc___c...................................",
    "...c___cc___cc___cc___c...................................",
    "...db_bddb_bddb_bddb_bd...................................",
    "...cdbdccdbdccdbdccdbdc...................................",
    "....cbc..cbc..cbc..cbc....................................",
]
SERRATEDTRENCHKNIFE_HAND = [
    "......f.2.2.2....",
    "jjijijd222222244.",
    "ihghghd4444444443",
    ".dddddc..........",
    ".c_c_c...........",
    ".bcbcb...........",
]

HATCHET = {"0": "#4a4c55", "1": "#6a6c75", "2": "#8a8c94", "3": "#b0b2b8", "4": "#dcdee2", "a": "#8a5a38", "b": "#b07a50", "c": "#d4a070", "d": "#e8c090", "e": "#f2d4a8"}
HATCHET_MENU = [
    "................................................333333333...",
    "...............................................3322222221...",
    ".....................................dddddddddd3222222221...",
    "................................dddddeeeeeeeeee3222222221d..",
    "......dddddddddddddddddddddddddeeeeeecccccccccc3222222221cc.",
    "ddddddeeeeeeeeeeeeeeeeeeeeeeeeeeccccccccbbbcccc3222222221cb.",
    "eeeeeeccccbbbbbbcccccccccccbbbbcccccccccccccccc3222222221bb.",
    "ccccccccccccccccccccbbbbbccccccccccbbbbbbbbbbbb3222222221ba.",
    "bcccccccbbbbbbbbbbbbbbbbbbbbbbbbbbbbbaaaaaaaaaa3222222221a..",
    "accccccbaaaaaaaaaaaaaaaaaaaaaaaaaaaaa..........1111111111...",
    ".bccccba.......................................1111111111...",
    ".abccba.......................................21111111111...",
    "..abba........................................11111111111...",
    "...aa........................................211111111111...",
    ".............................................1111111111110..",
    "............................................22222222222220..",
    "............................................22222222222220..",
    "...........................................222222222222220..",
    "...........................................333222222222220..",
    "..........................................43333333322222200.",
    "...........................................4443333333333333.",
    "..............................................4444433333333.",
    "...................................................44444444.",
]
HATCHET_HAND = [
    "................332.",
    "............ddd.322c",
    "ddddddddddddeeed321b",
    "bbbbbbbbbbbbbbbb321.",
    "aa..............112.",
    "...............2112.",
    "..............33331.",
    "...............44444",
]

FIREAXE = {"A": "#6a0a18", "B": "#a01020", "C": "#d01c1c", "D": "#ee3a20", "E": "#f87a5c", "o": "#8a3a14", "p": "#c05a20", "q": "#e07a30", "r": "#f0a050", "x": "#221c1e", "y": "#3a3032", "z": "#a81818", "Z": "#e04040", "3": "#a8a8b0", "4": "#e0e0e6", "K": "#141617"}
FIREAXE_MENU = [
    ".......................................................4....",
    ".......................................................44...",
    ".......................................................DB...",
    ".......................................................DDB..",
    ".......................................................DDB..",
    "......................................................DDDB..",
    "......................................................DDDDB.",
    "....................................................EDDDDDB.",
    ".............................................rrrrrqEDDDDDDBp",
    ".........................................rrrrqqqqqqEDDDDDDBp",
    ".ZyyyyyyyyyyyyyyyyyyyyrrrrrrrrrrrrrrrrrrrqqqqqqqqqqEDDDDDDBp",
    "ZzzxxxxxxxxxxxxxxxxxxxqqqqqqqqqqqqqqqqqqqqqqqqqqqqqEDDDDDDBp",
    "zKzzzzzzzzzzzzzzzxxxxxqqqqqqqqqqqqqqqqqqqqqqqqqqqqqEDDDDDDBp",
    "zzzxxxxxxxxxxxxxxxxxxxppppppppppppppppppppppppppppqEDDDDDDBp",
    "zzzAAA................oooooooooooooooooooooooooooo.CCCCCCCA.",
    "zzz................................................CCCCCCCA.",
    ".AA...............................................CCCCCCCCA.",
    "..................................................CCCCCCCCA.",
    ".................................................CBBBBBBBBBA",
    ".................................................CBBBBBBBBBA",
    "................................................CBBBBBBBBBBA",
    "................................................333333333333",
    "................................................444444444444",
]
FIREAXE_HAND = [
    ".........................4..",
    ".........................D..",
    ".........................DB.",
    "........................EDB.",
    "...................rrrrqEDBp",
    "zyyyyyyyyyqqqqqqqqqqqqqqEDBp",
    "zzzzzzzzzzoooooooooooooqEDBp",
    "A.......................CCA.",
    "........................CBA.",
    ".......................CBBBA",
    "......................333333",
    "......................444444",
]

SLEDGEHAMMER = {"0": "#1e1c22", "1": "#34323a", "2": "#4e4c55", "3": "#74727c", "4": "#a8a6ae", "5": "#dcdce0", "R": "#5e4638", "a": "#4a3020", "b": "#6e4a30", "c": "#8c5e3c", "d": "#a8784e", "e": "#c49464", "g": "#1e1e22", "h": "#2e2e34", "i": "#45454c", "j": "#606068", "K": "#141617"}
SLEDGEHAMMER_MENU = [
    ".................................................3333333333.",
    "................................................255555555552",
    "................................................333333333333",
    "................................................111111111111",
    ".................................................3422222221.",
    ".................................................3422222R21.",
    ".4............................................43.34211111R1.",
    "33jghjjghjjghjjghjjeeeeeeeedeeeeeeeedeeeeeeeed32.3421222121d",
    "22iighiighiighiighiddddddddcdddbbbbdcddddddddc21.3421000121c",
    "22hiighiighiighiighcccbbbbbccccccccccccccccccc21.34210K0121c",
    "12ghhhghhhghhhghhhgbbbbbbbbbbbbbbbbbbbbbbbbbbb21.3421000121b",
    "01hghhhghhhghhhghhhaaaaaaaaaaaaaaaaaaaaaaaaaaa10.3421222121a",
    ".0............................................00.3421111121.",
    ".................................................34R222R221.",
    ".................................................3422222221.",
    "................................................333333333333",
    "................................................244444444442",
    "................................................222222222222",
    ".................................................1111111111.",
]
SLEDGEHAMMER_HAND = [
    ".........................3333.",
    "........................255552",
    ".........................3221.",
    "3......................4.3221.",
    "2ijijijijiddddddddddddd2.3001.",
    "1hghghghghbbbbbabbbbbbb1.3001.",
    "0......................0.3221.",
    ".........................3221.",
    "........................244442",
    ".........................1111.",
]

BARBEDWIREBAT = {"a": "#4a2a1a", "b": "#6e4024", "c": "#8c5634", "d": "#a86e40", "e": "#c48a56", "f": "#d8a470", "g": "#3a2418", "G": "#24160e", "t": "#c8c0b0", "T": "#9a9080", "S": "#6e665c", "w": "#5e524c", "W": "#b0a49a", "K": "#141617"}
BARBEDWIREBAT_MENU = [
    "....................................w.w..w.w..w.w..w.w.....",
    "...............................w.w...W....W....W....W......",
    "................................W.dddwddddwddddwddddwddddcc",
    ".ee...................eeeeeeeeeeweeeeWeeeeWeeeeWeeeeWeeeegG",
    "dffdStttStttStttStddddffffffffffWfffffwafffwffffwffffwfffgG",
    "ccccTttSTttSTttSTtccccccaacccccccwccccWccccWccccWccccWcacgG",
    "abbaTTSSTTSSTTSSTTaaaabbbbbbbabbbWccccwccccwcaccwccccwcccgG",
    ".aa............TT.....aaaaaaaaaaaawbbbbWbbbbWbbbbWbbbbWbbgG",
    "...............ST.................Waaaawaaaawaaaawaaaawaabb",
    "................S................w.w...W....W....W....W....",
    "......................................w.w..w.w..w.w..w.w...",
]
BARBEDWIREBAT_HAND = [
    ".............W...W...W......",
    "e...........eeweeeweeeweeeeg",
    "ctTtTtTccccccccWcccWcccWcccg",
    "a....S......aaawaaawaaawaaag",
    "................W...W...W...",
]

OBELISKRITUALBLADE = {"0": "#22262a", "1": "#33373b", "2": "#4c5258", "3": "#727980", "4": "#a9b0b5", "a": "#4c2812", "b": "#743f1c", "c": "#a0602f", "d": "#b97a45", "r": "#7a1a17", "R": "#c0302a", "s": "#e06a50", "y": "#f0d878", "K": "#141617"}
OBELISKRITUALBLADE_MENU = [
    "...............3............................................",
    "..............43............................................",
    "..............32............................................",
    "..............332.......334.................................",
    "............yR2R44...333333444...33333444.......3...........",
    ".dddddddddddrR22334333332223334333333333344..33323444.......",
    "cccccccccaccRr.222333222222222333222222223333222111114...33.",
    "bccaccccacccRr..22222222111222222211111112222111......433...",
    "abbbababbbbbRr..11222111...1112221.......1111...............",
    ".aaaaaaaaaaaRr.2..111.........111...........................",
    "............rr.1............................................",
    "...............1............................................",
    "............................................................",
]
OBELISKRITUALBLADE_HAND = [
    ".......3...............",
    "......yR2..............",
    "cdddddR2444...444...33.",
    ".bbabbr2111333111333...",
    "......r1...111...1.....",
    ".......................",
    ".......................",
]

SET = {"HuntingKnife": (HUNTINGKNIFE, HUNTINGKNIFE_MENU, HUNTINGKNIFE_HAND, "hunting_knife"),
       "SpetsnazSwitchblade": (SPETSNAZSWITCHBLADE, SPETSNAZSWITCHBLADE_MENU, SPETSNAZSWITCHBLADE_HAND, "switchblade"),
       "SteppeMachete": (STEPPEMACHETE, STEPPEMACHETE_MENU, STEPPEMACHETE_HAND, "machete"),
       "SerratedTrenchKnife": (SERRATEDTRENCHKNIFE, SERRATEDTRENCHKNIFE_MENU, SERRATEDTRENCHKNIFE_HAND, "serrated_trench_knife"),
       "ObeliskRitualBlade": (OBELISKRITUALBLADE, OBELISKRITUALBLADE_MENU, OBELISKRITUALBLADE_HAND, None),
       "Hatchet": (HATCHET, HATCHET_MENU, HATCHET_HAND, "hatchet"),
       "FireAxe": (FIREAXE, FIREAXE_MENU, FIREAXE_HAND, "fire_axe"),
       "Hammer": (HAMMER, HAMMER_MENU, HAMMER_HAND, "hammer"),
       "Sledgehammer": (SLEDGEHAMMER, SLEDGEHAMMER_MENU, SLEDGEHAMMER_HAND, "sledge_hammer"),
       "WoodenBaseballBat": (BAT, BAT_MENU, BAT_HAND, "baseball_bat"),
       "BarbedWireBat": (BARBEDWIREBAT, BARBEDWIREBAT_MENU, BARBEDWIREBAT_HAND, "barbed_wire_bat")}

def hx(h): v = int(h.lstrip("#"), 16); return ((v >> 16) & 255, (v >> 8) & 255, v & 255, 255)

def paint(grid, pal, w, h):
    """The grid's pixels, outlined, centred on a w x h canvas. '_' is a see-through hole that gets no outline (knuckle rings)."""
    gw, gh = max(map(len, grid)), len(grid)
    im = Image.new("RGBA", (gw + 2, gh + 2)); px = im.load()
    for y, row in enumerate(grid):
        for x, ch in enumerate(row):
            if ch not in "._": px[x + 1, y + 1] = hx(pal[ch])
    holes = {(x + 1, y + 1) for y, row in enumerate(grid) for x, ch in enumerate(row) if ch == "_"}
    filled = lambda x, y: 0 <= x < im.width and 0 <= y < im.height and px[x, y][3] and px[x, y] != hx(K)
    for y in range(im.height):
        for x in range(im.width):
            if not px[x, y][3] and (x, y) not in holes and any(filled(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))): px[x, y] = hx(K)
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
        im = Image.open(os.path.join(ROOT, "ArtSource/refs/melee", ref + ".png") if ref else os.path.join(WEAP, "weapon_ObeliskRitualBlade_hd.png")).convert("RGBA")
        if not ref: im = im.resize((im.width * 5, im.height * 5), Image.NEAREST)                     # no reference: the old sprite
        im.thumbnail((330, 190)); sheet.alpha_composite(im, (10, y + 24))
        for spr, x in ((paint(menu, pal, 64, 32), 360), (paint(hand, pal, 32, 16), 760)):
            t = Image.new("RGBA", spr.size, bg); t.alpha_composite(spr); sheet.alpha_composite(t.resize((spr.width * 6, spr.height * 6), Image.NEAREST), (x, y + 24))
        strip = Image.new("RGBA", (80, 32), bg); strip.alpha_composite(hatchet, (0, 0)); strip.alpha_composite(paint(hand, pal, 32, 16), (0, 16))
        strip.alpha_composite(man, (44, 0))
        sheet.alpha_composite(strip.resize((480, 192), Image.NEAREST), (1000, y + 24))
    out = os.path.join(ROOT, "ArtSource/_out/melee_px_sheet.png"); sheet.save(out); print("->", dest, "+", out)
