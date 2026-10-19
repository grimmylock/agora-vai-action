# One 16x16 "avatar" per armor in 02_Armor_and_Mods.md, built from parts (head + body) so every
# piece is unique but stays in the locked agora-vai style. Some visuals come straight from the data:
# belt pouches = Belt Slots, chest filter canister = Anom-DR >= 60%.
# v2: grounded in the S.T.A.L.K.E.R. originals - GP-5 style round gas-mask lenses instead of glowing
# visor bands, steel helmets, hazmat face windows of dark glass, dull hydraulics on exoskeletons.
import os, re, random
from PIL import Image, ImageDraw, ImageFont

OUT = os.path.dirname(os.path.abspath(__file__))
SPR = os.path.join(OUT, "armor_avatars")
os.makedirs(SPR, exist_ok=True)

def hexc(h, a=255):
    h = h.lstrip("#"); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)
def shade(h, f):
    r, g, b, _ = hexc(h); return "#%02x%02x%02x" % tuple(max(0, min(255, int(c * f))) for c in (r, g, b))
def mirror(halves): return [x + x[::-1] for x in halves]

HEADS = {
    "hood":     ["......KK", ".....KHh", "....KHhh", "....KHMM", "....KMEm", "....KMmM", ".....KMm"],  # hood + GP-5 gas mask
    "bare":     ["......KK", ".....KAA", "....KAAA", "....KAFF", "....KFeF", "....KFFf", ".....KFF"],
    "bandit":   ["......KK", ".....KAA", "....KAAA", "....KAFF", "....KFeF", "....KHHH", ".....KHH"],  # bandana over the face
    "hoodface": ["......KK", ".....KHh", "....KHhh", "....KHFF", "....KFeF", "....KNNN", ".....KNN"],  # hood up + balaclava/scarf N over the mouth
    "balaclava":["......KK", ".....KNn", "....KNnN", "....KNNN", "....KFeF", "....KNNN", ".....KNN"],  # full knit ski mask (N), one wide eye slit of skin with dark eyes
    "kokonmask": ["......KK", ".....KHh", "....KHhh", "....KHHH", "....KMEH", "....KHHM", ".....KHM"],  # Kokon: gas mask built into the hood
    "visorhelm":["......KK", "....KKhh", "...KHhhh", "...KHVVV", "....KVVV", "....KMmM", ".....KMm"],  # military helmet, dark visor down
    "sphere":   [".....KKK", "....KHHH", "...KHHHH", "...KHVVV", "...KHVVV", "....KHHH", ".....KHH"],  # glossy full-face helmet; Batch 128: a dark visor so front and back differ
    "helmet":   ["......KK", "....KKhh", "...KHhhh", "...KKHHH", "....KMEm", "....KMmM", ".....KMm"],  # steel helmet + gas mask
    "sealed":   ["......KK", ".....KHh", "....KHhh", "....KHWW", "....KHWW", "....KHWW", "....KHHH"],  # hazmat hood, glass window
    "exo":      [".....KKK", "....KHhh", "...KHhhh", "...KHHMM", "...KHMEm", "...KHMmM", "....KKMm"],  # heavy exo helmet + gas mask
}
# Batch 128 (Yan: "add the back as well so the player can know which way the npc's are looking"): the same heads seen
# from behind - same silhouettes, no face, eyes or lenses; mask straps (M) across the back of the head, hair (A) on bare heads.
HEADS_BACK = {
    "hood":     ["......KK", ".....KHh", "....KHhh", "....KHHH", "....KMMM", "....KHHH", ".....KHH"],
    "bare":     ["......KK", ".....KAA", "....KAAA", "....KAAA", "....KAAA", "....KAAA", ".....Kff"],
    "bandit":   ["......KK", ".....KAA", "....KAAA", "....KAAA", "....KAAA", "....KHHH", ".....KfH"],  # bandana knot
    "hoodface": ["......KK", ".....KHh", "....KHhh", "....KHHH", "....KHHH", "....KNNN", ".....KHH"],
    "balaclava":["......KK", ".....KNn", "....KNnN", "....KNNN", "....KNNN", "....KNNN", ".....KNN"],
    "kokonmask":["......KK", ".....KHh", "....KHhh", "....KHHH", "....KHHH", "....KHHH", ".....KHH"],
    "visorhelm":["......KK", "....KKhh", "...KHhhh", "...KHHHH", "....KHHH", "....KMMM", ".....KMm"],
    "sphere":   [".....KKK", "....KHHH", "...KHHHH", "...KHHHH", "...KHHHH", "....KHHH", ".....KHH"],
    "helmet":   ["......KK", "....KKhh", "...KHhhh", "...KHHHH", "....KHHH", "....KMMM", ".....KMm"],
    "sealed":   ["......KK", ".....KHh", "....KHhh", "....KHHH", "....KHHH", "....KHHH", "....KHHH"],
    "exo":      [".....KKK", "....KHhh", "...KHhhh", "...KHHHH", "...KHHJJ", "...KHHHH", "....KKMm"],   # vents on the back of the helmet
}

BODIES = {   # rows 7..15, ARMLESS (hands are separate floating sprites). Light = 6 wide with rounded
             # shoulders; vest = chest plate; heavy/exo = 8 wide with shoulder pads (+1 leg row, see _taller)
    "jacket": [".....KSS", "....KsSS", "....KsSS", "....KSSS", "....KbbB", "....KDDK", "....KDDK", "....KBBK", "....KKKK"],
    "coat":   [".....KSS", "....KsSS", "....KsSS", "....KSSS", "....KbbB", "...KSSSS", "...KSSSK", "....KBBK", "....KKKK"],
    "suit":   [".....KSS", "....KsSS", "....KsSS", "....KSSS", "....KbbB", "....KSSK", "....KSSK", "....KBBK", "....KKKK"],
    "vest":   [".....KSS", "....KsPP", "....KsPP", "....KSSS", "....KbbB", "....KDDK", "....KDpK", "....KBBK", "....KKKK"],
    "heavy":  ["....KSSS", "...KpSPP", "...KpsPP", "....KSPP", "....KbbB", "...KDDDK", "...KDpDK", "...KBBBK", "...KKKKK"],
    "exo":    ["....KSSS", "...KpSSS", "...KpsSS", "....KSSS", "....KbbB", "...KDDDK", "...KJpDK", "...KBBBK", "...KKKKK"],
}
POUCH_COLS = [5, 10, 6, 9, 7]

# name, tier, physDR, anomDR, belt slots  (02_Armor_and_Mods.md)
ARMORS = [
    ("Leather Jacket", 1, .10, .05, 1), ("Raider Jacket", 1, .15, .10, 1), ("Heavy Trenchcoat", 1, .15, .10, 2),
    ("Rookie Field Suit", 1, .12, .20, 2), ("Scrap-Plated Vest", 1, .20, .05, 0),
    ("Liberator Windbreaker", 2, .15, .25, 3), ("Marsh Recon Suit", 2, .20, .30, 3), ("Contractor Light", 2, .20, .15, 2),
    ("Daybreak Suit", 2, .25, .25, 2), ("Raider Stitched Coat", 2, .25, .15, 1), ("Sentinel Service Suit", 2, .30, .10, 1),
    ("NBC Suit", 2, .25, .50, 1), ("Obelisk Patrol", 2, .35, .40, 1),
    ("HZ-99 Survey Suit", 3, .10, .70, 4), ("IZC Scout", 3, .25, .30, 3), ("Kokon Suit", 3, .30, .80, 3),
    ("Liberator Guard Suit", 3, .35, .35, 3), ("Contractor ACE Base", 3, .35, .30, 3), ("Obelisk Scientific", 3, .35, .60, 2),
    ("Sentinel Armored Suit", 3, .45, .15, 1), ("Basalt-5", 3, .50, .15, 1), ("Raider Veteran Coat", 3, .35, .20, 2),
    ("HZ-99M Survey Suit", 4, .20, .80, 5), ("Marsh Pathfinder Suit", 4, .30, .60, 4), ("Contractor ACE Scientific", 4, .40, .45, 4),
    ("Sentinel Vanguard", 4, .45, .30, 2), ("IZC Operative", 4, .42, .35, 3), ("Cartel Rad-Suit", 4, .40, .65, 4),
    ("Granit-9 Assault Suit", 4, .55, .40, 2), ("Proto-Exoskeleton", 4, .65, .20, 2), ("Obelisk Heavy", 4, .62, .80, 1),
    ("Raider Boss Coat", 4, .45, .25, 3),
    ("Radiant Kokon", 5, .40, .80, 4), ("Liberator Exoskeleton", 5, .65, .35, 2), ("Wanderer Exoskeleton", 5, .70, .25, 3),
    ("Obelisk Exoskeleton", 5, .75, .90, 1), ("Sentinel Proto-Armor", 5, .85, .20, 1),
]

# look: head, body, head color, body color, lens, extras
LOOK = {   # v3: every armor gets its own color identity (suit + head two-tone), still grim and grounded
    # Tier 1 - civilian/improvised gear
    "Leather Jacket":       dict(head="balaclava", body="jacket", H="#a08a64", N="#1c1c1c", S="#6b3f22", D="#2f4a7a"),   # rookie: hood + ski mask
    "Raider Jacket":        dict(head="hoodface", body="jacket", H="#9a8462", N="#1c1c1c", S="#262425", D="#2c3f8a", stripes=True),  # bandit: hood, balaclava, black leather
    "Heavy Trenchcoat":     dict(head="hoodface", body="coat", H="#4a3a28", S="#6e5a3a", A="#3b2a1e"),  # dark brown coat
    "Rookie Field Suit":        dict(head="hood", body="suit", H="#4a7890", S="#6a9ab2"),                              # sky blue
    "Scrap-Plated Vest":     dict(head="hoodface", body="vest", H="#6b6658", S="#55554c", P="#b8642a"),            # rust plates
    # Tier 2
    "Liberator Windbreaker":       dict(head="hood", body="jacket", H="#7a9a42", S="#6f9a3a", camo=True),                # lime green
    "Marsh Recon Suit":            dict(head="hood", body="suit", H="#3a95a0", S="#2f7f86", camo=True),                  # teal
    "Contractor Light":            dict(head="hood", body="jacket", H="#2a3550", S="#8090ae", camo=True),                # periwinkle slate
    "Daybreak Suit":          dict(head="hood", body="jacket", H="#5a6e3a", S="#7a6040"),                           # classic olive hood + tan
    "Raider Stitched Coat":  dict(head="bandit", body="coat", H="#b02a2a", S="#5e2424", A="#3b2a1e"),              # maroon + red bandana
    "Sentinel Service Suit":       dict(head="hood", body="vest", H="#1f2023", S="#2e2f33", P="#7a2222", arm="#b02a2a"),  # black + red plates
    "NBC Suit":              dict(head="hood", body="suit", H="#7a7a2e", S="#7a7a2e", anom_canister=True),        # olive-yellow rubber
    "Obelisk Patrol":        dict(head="hood", body="jacket", H="#aaa290", S="#7a7468", E="#c46a5a", camo=True),   # light gray
    # Tier 3
    "HZ-99 Survey Suit":     dict(head="sealed", body="suit", H="#a6b42e", S="#a6b42e", rebreather=True, belt="#1c1c1c"),  # lime hazmat, black visor
    "IZC Scout":             dict(head="helmet", body="jacket", H="#4f86b8", S="#c8d0d8", D="#8a96a2"),            # white + UN blue helmet
    "Kokon Suit":            dict(head="kokonmask", body="suit", H="#a89a72", S="#a89a72", quilted=True, belt="#6a4a2a"),  # sand, mask in the hood
    "Liberator Guard Suit":   dict(head="helmet", body="vest", H="#2f4f28", S="#3f6a35", P="#4f7a40", camo=True),   # forest green
    "Contractor ACE Base":        dict(head="hood", body="vest", H="#2a3448", S="#4a5568", P="#8a7a58", camo=True),  # urban blue-gray, tan carrier
    "Obelisk Scientific":    dict(head="sealed", body="suit", H="#b8bcc0", S="#dcdfe2"),                           # white hazmat
    "Sentinel Armored Suit":       dict(head="helmet", body="vest", H="#233a5c", S="#3a5a8a", P="#4a6c9c", arm="#b02a2a"),  # steel blue
    "Basalt-5":            dict(head="visorhelm", body="vest", H="#262626", S="#7a7652", P="#4f5a34", camo=True),  # army: camo + olive vest, visor helmet
    "Raider Veteran Coat":  dict(head="hoodface", body="coat", H="#8a6a48", N="#1c1c1c", S="#3a3430", A="#3b2a1e"),  # bandit veteran: tan hood, balaclava, charcoal coat
    # Tier 4
    "HZ-99M Survey Suit":       dict(head="sealed", body="suit", H="#2f8a5e", S="#2f8a5e"),                           # emerald
    "Marsh Pathfinder Suit":      dict(head="helmet", body="vest", H="#1f5578", S="#2a6f9a", P="#3a86b0"),              # cerulean
    "Contractor ACE Scientific":   dict(head="sealed", body="suit", H="#2e2b5e", S="#3d3a78"),                           # indigo
    "Sentinel Vanguard":     dict(head="helmet", body="heavy", H="#5a6a80", S="#20283a", P="#5a6a80", arm="#b02a2a"),  # black + steel plates
    "IZC Operative":         dict(head="helmet", body="vest", H="#d0d6dc", S="#7d95ab", P="#9fb2c4"),              # gray-blue + white helmet
    "Cartel Rad-Suit":       dict(head="sealed", body="suit", H="#c9a23a", S="#232323", arm="#c9a23a"),            # black + hazard yellow
    "Granit-9 Assault Suit":        dict(head="sphere", body="suit", H="#3a3f48", S="#34426a", quilted=True),  # navy padded + glossy helmet
    "Proto-Exoskeleton":    dict(head="exo", body="exo", H="#2a2a2a", S="#8a4e2c", D="#3a3a30"),  # rusty plates, black helmet
    "Obelisk Heavy":         dict(head="helmet", body="heavy", H="#d0d4d8", S="#3c4046", P="#d0d4d8", E="#c46a5a"),  # gray + white plates
    "Raider Boss Coat":      dict(head="bare", body="coat", S="#8a1e1e", A="#2a2420", arm="#d4a62a"),              # crimson coat
    # Tier 5
    "Radiant Kokon":         dict(head="kokonmask", body="suit", H="#ddd2a4", S="#ddd2a4", quilted=True, belt="#6a4a2a"),  # pale cream Kokon
    "Liberator Exoskeleton": dict(head="exo", body="exo", H="#5f8a34", S="#4f7a35", camo=True),                    # Freedom green
    "Wanderer Exoskeleton": dict(head="exo", body="exo", H="#7e766c", S="#8a8078", D="#5a6040", camo=True),  # SoC exo: gray metal, olive legs
    "Obelisk Exoskeleton":   dict(head="exo", body="exo", H="#ece4d0", S="#c8bfa8", E="#c46a5a"),                  # white
    "Sentinel Proto-Armor":      dict(head="exo", body="exo", H="#3a5a8a", S="#1e2128", arm="#b02a2a"),                # black + blue helmet
}

def build(name, anom, belt, L=None, back=False):
    L = L or LOOK[name]
    S, H = L["S"], L.get("H", L["S"])
    E = L.get("E", "#9fd8e8")
    pal = {
        "K": "#141617", "M": "#2b2f33", "m": "#5c636b", "b": L.get("belt", "#3d3226"), "B": "#2a2218", "g": "#2b2d30",
        "H": H, "h": shade(H, 1.35), "S": S, "s": shade(S, 1.35), "D": L.get("D", shade(S, 0.7)),
        "P": L.get("P", shade(S, 1.2)), "p": shade(L.get("P", shade(S, 1.2)), 1.4),
        "E": E, "e": shade(E, 1.25), "v": shade(E, 1.3), "V": "#1c1f22", "J": "#7a7f84", "W": "#2a3a40", "w": "#9fb8c0",
        "A": L.get("A", "#3b2a1e"), "F": "#c8916a", "f": "#9e6e4f", "N": L.get("N", H),
        "n": shade(L.get("N", H), 1.9),   # knit highlight on the balaclava
    }
    rows = mirror((HEADS_BACK if back else HEADS)[L["head"]] + BODIES[L["body"]])
    grid = [list(r) for r in rows]
    if L["head"] in ("bare", "bandit", "hoodface", "balaclava"):          # eyes are dark, not lens-colored
        pal["e"] = "#2a2018"
    if back: return _back(grid, pal, L, name, belt)
    if L["head"] == "helmet":                     # glint only on the left side of the visor
        grid[4][10] = "E"
    # data-driven details
    for c in POUCH_COLS[:belt]:
        if grid[11][c] in "bB":
            grid[11][c] = "O"
    if (anom >= 0.6 or L.get("anom_canister")) and not L.get("rebreather"):     # GP-5 style filter canister on the chest
        grid[8][7], grid[8][8] = "Q", "q"
    if L["head"] == "sealed":                     # one glint on the window, top-left
        grid[4][6] = "w"
    if L["head"] == "visorhelm":                  # glint on the dark visor
        grid[3][5] = "w"
    if L["head"] == "sphere":                     # glossy highlight, top-left
        grid[2][5] = "h"; grid[1][6] = "h"; grid[3][6] = "w"   # Batch 128: shine on the visor
    if L.get("quilted"):                          # padded/quilted suit seams (Kokon, Granit)
        for y in (8, 10):
            for x in range(4, 12):
                if grid[y][x] in "Ss": grid[y][x] = "C"
    if L["body"] == "exo":                        # ribbed chest plates + red indicator on the belt
        for x in range(4, 12):
            if grid[8][x] == "S": grid[8][x] = "s"
        grid[11][10] = "X"
    if L.get("rebreather"):                       # Surveyor breathing unit on the chest
        grid[8][7] = grid[8][8] = "Y"; grid[9][7] = grid[9][8] = "y"
    if L.get("stripes"):                          # tracksuit stripes (bandits)
        for y in (12, 13):
            for x in (5, 10):
                if grid[y][x] == "D": grid[y][x] = "Z"
    if L.get("camo"):                             # a few darker blotches = camo pattern
        for x, y in [(6, 8), (10, 9), (8, 10), (5, 12), (10, 13), (7, 9)]:
            if grid[y][x] in "SsDP": grid[y][x] = "C"
    if L.get("arm"):                              # faction patch on the left shoulder
        grid[8][4 if L["body"] in ("heavy", "exo") else 5] = "R"
    pal.update({"X": "#c03030", "Y": "#e6e6de", "y": "#a8a8a0", "O": "#9a7a4e", "Q": "#3a3f44", "q": "#5c636b", "Z": "#a8b0c8",
                "C": shade(L["S"], 0.72), "R": L.get("arm", "#7a1f1f")})
    # grit pass: a few seeded scuffs (darker) and worn edges (lighter) on cloth/plates, so nothing
    # reads as a clean, factory-new surface. Seeded by name -> stable across reruns.
    rnd = random.Random(name)
    cloth = [(x, y) for y in range(7, 14) for x in range(16) if grid[y][x] in "SsDP"]
    for x, y in rnd.sample(cloth, min(3, len(cloth))):
        grid[y][x] = "G"
    for x, y in rnd.sample(cloth, min(1, len(cloth))):
        if grid[y][x] != "G": grid[y][x] = "U"
    pal["G"], pal["U"] = shade(L["S"], 0.8), shade(L["S"], 1.18)
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    for y, row in enumerate(grid):
        assert len(row) == 16, (name, y)
        for x, ch in enumerate(row):
            if ch != ".":
                img.putpixel((x, y), hexc(pal[ch]))
    return _polish(img, L)

def _back(grid, pal, L, name, belt):
    """Batch 128: the back view. Same suit, belt pouches, camo, quilting, stripes and scuffs as the front; no zip, collar,
    chest canister or glints. A small pack between the shoulders (a rebreather tank on Survey suits, a spine plate on exos),
    and the faction patch on the shoulder that's on the left from behind (x 10/11, the mirror of the front's)."""
    for c in POUCH_COLS[:belt]:
        if grid[11][c] in "bB": grid[11][c] = "O"
    if L.get("quilted"):
        for y in (8, 10):
            for x in range(4, 12):
                if grid[y][x] in "Ss": grid[y][x] = "C"
    if L.get("stripes"):
        for y in (12, 13):
            for x in (5, 10):
                if grid[y][x] == "D": grid[y][x] = "Z"
    if L.get("camo"):
        for x, y in [(9, 8), (5, 9), (7, 10), (10, 12), (5, 13), (8, 9)]:
            if grid[y][x] in "SsDP": grid[y][x] = "C"
    if L["body"] == "exo":                                  # a spine plate down the back
        for y in (8, 9, 10): grid[y][7] = grid[y][8] = "J"
        grid[8][7] = "j"
    elif L.get("rebreather"):                               # the breathing unit's tank
        for y in (8, 9, 10): grid[y][7] = grid[y][8] = "Y"
        grid[10][7] = grid[10][8] = "y"
    else:                                                   # a small pack, flap lit on top
        for y in (8, 9, 10):
            for x in (6, 7, 8, 9): grid[y][x] = "T"
        grid[8][6] = grid[8][7] = grid[8][8] = grid[8][9] = "t"
        grid[10][9] = "u"
    if L.get("arm"):
        grid[8][11 if L["body"] in ("heavy", "exo") else 10] = "R"
    pack = shade(L.get("A", "#3b2a1e"), 1.25) if L.get("A") else shade(L["S"], 0.62)
    pal.update({"X": "#c03030", "Y": "#e6e6de", "y": "#a8a8a0", "O": "#9a7a4e", "Z": "#a8b0c8", "C": shade(L["S"], 0.72),
                "R": L.get("arm", "#7a1f1f"), "T": pack, "t": shade(pack, 1.3), "u": shade(pack, 0.7), "j": "#9a9fa4"})
    rnd = random.Random(name + "/back")
    cloth = [(x, y) for y in range(7, 14) for x in range(16) if grid[y][x] in "SsDP"]
    for x, y in rnd.sample(cloth, min(3, len(cloth))): grid[y][x] = "G"
    pal["G"] = shade(L["S"], 0.8)
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    for y, row in enumerate(grid):
        for x, ch in enumerate(row):
            if ch != ".": img.putpixel((x, y), hexc(pal[ch]))
    return _polish(img, L, back=True)

def _outline(im):
    out = im.copy(); a = im.load(); o = out.load(); w, h = im.size; k = hexc("#141617")
    for y in range(h):
        for x in range(w):
            if not a[x, y][3] and any(0 <= x+dx < w and 0 <= y+dy < h and a[x+dx, y+dy][3] and a[x+dx, y+dy][:3] != k[:3]
                                      for dx, dy in ((1,0),(-1,0),(0,1),(0,-1))):
                o[x, y] = k
    return out

def _polish(img, L, back=False):
    """v4 (locked): lit-left / shaded-right torso, per-style chest detail, and heavy/exo one leg-row taller."""
    px = img.load(); k = hexc("#141617")[:3]; style = L["body"]
    def dk(c, f): return tuple(max(0, min(255, int(v * f))) for v in c[:3]) + (255,)
    S = hexc(L["S"])
    rx = 11 if style in ("heavy", "exo") else 10
    for y in (8, 9, 10):
        if px[rx, y][3] and px[rx, y][:3] != k: px[rx, y] = dk(px[rx, y], 0.8)
    if back and style == "coat":                          # Batch 128: the back vent of a coat
        for y in (12, 13):
            if px[8, y][3] and px[8, y][:3] != k: px[8, y] = dk(px[8, y], 0.65)
    if back and style in ("jacket", "coat"):              # the collar from behind
        px[7, 7] = px[8, 7] = dk(S, 0.75)
    if not back and style in ("jacket", "coat"):          # zip + collar
        for y in (8, 9, 10):
            if px[8, y][3] and px[8, y][:3] != k: px[8, y] = dk(S, 0.62)
        px[7, 7] = dk(S, 1.3); px[8, 7] = dk(S, 0.75)
    if style == "suit" and not back:                      # chest harness strap
        for x, y in ((6, 8), (7, 9), (8, 10)):
            if px[x, y][:3] == S[:3]: px[x, y] = dk(S, 0.7)
    if style in ("vest", "heavy"):                        # plate top edge catches the light
        for x in range(6, 10):
            if px[x, 8][3] and px[x, 8][:3] != k: px[x, 8] = dk(px[x, 8], 1.2)
    if style in ("heavy", "exo"):                         # taller: head+torso up 1px, top leg row repeated
        t = Image.new("RGBA", (16, 16))
        t.alpha_composite(img.crop((0, 12, 16, 16)), (0, 12))
        t.alpha_composite(img.crop((0, 12, 16, 13)), (0, 11))
        t.alpha_composite(img.crop((0, 1, 16, 12)), (0, 0))
        tp = t.load()
        for x in range(16):
            if tp[x, 0][:3] == k: tp[x, 0] = (0, 0, 0, 0)
        img = _outline(t)
    return img

def key(name): return re.sub(r"[^a-z0-9]+", "_", name.lower()).strip("_")

def main():
    items = []
    for name, tier, phys, anom, belt in ARMORS:
        img = build(name, anom, belt)
        img.save(os.path.join(SPR, f"armor_{key(name)}.png"))
        items.append((img, f"T{tier} {name}"))

    # preview sheet
    S8, PAD, LABEL, COLS = 8, 40, 24, 6
    CELL = 16 * S8
    rows_n = (len(items) + COLS - 1) // COLS
    W = COLS * (CELL + PAD) + PAD
    H = 74 + rows_n * (CELL + PAD + LABEL) + 10
    sheet = Image.new("RGBA", (W, H), hexc("#1b1d1f"))
    d = ImageDraw.Draw(sheet)
    font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 13)
    title = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 22)
    d.text((PAD, 22), "agora vai - armor avatars (37)", font=title, fill=hexc("#e6e6e6"))
    for i, (img, label) in enumerate(items):
        x0 = PAD + (i % COLS) * (CELL + PAD)
        y0 = 74 + (i // COLS) * (CELL + PAD + LABEL)
        d.rectangle([x0, y0, x0 + CELL - 1, y0 + CELL - 1], fill=hexc("#222527"))
        for yy in range(0, CELL, 2 * S8):
            for xx in range(0, CELL, 2 * S8):
                for dx, dy in [(0, 0), (S8, S8)]:
                    d.rectangle([x0 + xx + dx, y0 + yy + dy, x0 + xx + dx + S8 - 1, y0 + yy + dy + S8 - 1], fill=hexc("#26292c"))
        sheet.alpha_composite(img.resize((CELL, CELL), Image.NEAREST), (x0, y0))
        d.text((x0, y0 + CELL + 5), label, font=font, fill=hexc("#b8bcc0"))
    sheet.convert("RGB").save(os.path.join(OUT, "armor_avatars_sheet.png"))
    print("ok", len(items))


if __name__ == "__main__":
    main()
