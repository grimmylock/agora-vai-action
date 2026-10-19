# Batch 130b (Yan: "they do not look good, i'll send some references so you can redo them"; references in ArtSource/refs/mutants/,
# "sorry the white one is the pallid prowler"): the mutants redrawn from Yan's references - hand-placed silhouettes and features over
# a value map of each reference (mutant_from_ref.py prints it), shaded with the armor polish and outlined. Side views face right.
# Sizes: 24x16 for the hounds and the swarm, 32x24 Dread Prowler, 32x32 Blight Brute.
#   python3 make_mutants_v3.py <sheet.png>
import os, sys
from PIL import Image, ImageDraw, ImageFont
from make_mutants_v2 import Canvas, render
from mutant_from_ref import outline, hexc

def draw(rows, pal):
    w = len(rows[0]); im = Image.new("RGBA", (w, len(rows)), (0, 0, 0, 0))
    for y, r in enumerate(rows):
        assert len(r) == w, (y, r)
        for x, ch in enumerate(r):
            if ch != ".": im.putpixel((x, y), hexc(pal[ch]))
    return outline(im)

# ------------------------------------------------------------------ Pallid Prowler (24x16): pale, starved, hunched, acid drool
PALLID = [
"........................",
"...........W.W..........",
".......W.WWWWWWW........",
".....WWWWBBBBBBBWW......",
"...WWBBBBBBBBBBBBBW.WWW.",
"...BBBBBBSBBSBBSBBBWBBBW",
"...SBBBBBBSBBSBBSBBBBXBB",
"....SBBS..BSBSBSBSSSBBBB",
"....LBS...SSSSSSSS.XXXXT",
"...LL.....SS..LL..STXTX.",
"..LL.......F...L...SSG..",
"..L........F...LL...GG..",
"..L........F....L....G..",
"..L.......F.....L.......",
"..LL.....FF.....LLL.....",
"........................",
]
PALLID_PAL = {"W": "#e6dfcc", "B": "#c2baa6", "S": "#938b7a", "L": "#ada593", "F": "#6e6b60", "f": "#55524a",
     "X": "#2a2622", "T": "#f0ead8", "G": "#8fe03a"}

# ------------------------------------------------------------------ Blind Maw (24x16): a hunched red hound that's all jaw
HEAD = [   # x 14..23, y 0..12 - the jaws in profile, hand-placed
"....HHHH..",
"..HHBBBBHH",
".HBBBBBBBB",
"HBBRRRRRRR",
"BBRTXTwTXT",
"BBXXXXwXXw",
"BXXPPPwXXw",
"BBXXPPPXXw",
"BBRTXTXTXT",
".BBRRRRRRR",
".BBBBBBBBB",
"..DDDDDDD.",
"..........",
]
def maw():
    c = Canvas(24, 16)
    c.ell(8.6, 7.6, 6.4, 3.3, "B")              # body
    c.ell(10, 5.4, 4.4, 2.4, "B")               # shoulder hump
    c.ell(3.8, 7.0, 3.0, 2.8, "B")              # rump
    c.line([(1.2, 5.6), (0.4, 4.6)], "B")       # stub tail
    for (x0, x1, ch) in ((3, 4, "B"), (7, 7, "F"), (11, 12, "B"), (14, 14, "F")):
        c.rect(x0, 10, x1, 13, ch)              # legs, near ones 2 px
    c.rect(2, 13, 4, 13, "D"); c.rect(10, 13, 12, 13, "D")
    for x, y in ((6, 4), (9, 3), (12, 3)): c.px(x, y, "H")   # fur tufts
    for y, row in enumerate(HEAD):
        for i, ch in enumerate(row):
            if ch != ".": c.g[y][14 + i] = ch
    return c, {"B": "#6b2a2d", "H": "#97514e", "D": "#43131c", "F": "#3a1216", "R": "#b0303a", "P": "#d86a72",
               "X": "#1a0608", "T": "#f0ead8", "w": "#c8c0b8"}, "RPXTwFDH"

# ------------------------------------------------------------------ Veil Prowler (24x16) and Dread Prowler (32x24)
def veil():
    c = Canvas(24, 16)
    c.poly([(4, 5.6), (9, 4.6), (13, 4.0), (16, 4.6), (16.6, 7), (13, 8.4), (9, 8), (5.6, 8.2), (3.6, 7)], "B")   # lean body
    c.ell(5.2, 6.4, 2.2, 1.8, "B")
    c.poly([(15.4, 4.4), (18, 5.2), (19.4, 7.2), (17, 7.8)], "B")      # neck, low
    c.poly([(18, 6.0), (21, 6.2), (23.6, 8.0), (23.4, 8.8), (20, 9.0), (18.4, 8.6)], "B")   # long narrow head
    c.px(20, 7, "E"); c.px(21, 7, "e")                                  # red eye
    c.line([(3.8, 6), (1.6, 6.4), (0.6, 8), (0.8, 10)], "B")            # long tail, hanging
    c.line([(5.2, 8), (3.6, 10.4), (4.2, 13)], "B"); c.px(5, 13, "B")   # near hind leg, hock back
    c.line([(8, 8.4), (8.4, 13)], "F"); c.px(7.6, 13, "F")              # far hind
    c.line([(15, 8), (16, 10.6), (16.4, 13)], "B"); c.px(17.4, 13, "B") # near foreleg
    c.line([(13, 8.4), (12.4, 13)], "F"); c.px(11.6, 13, "F")           # far foreleg
    for x in (10, 12, 14): c.px(x, 6.4, "S")                            # ribs
    for pts in (((9, 4), (8, 3), (7, 2), (7, 1)), ((12, 3.4), (11, 2), (10, 1), (10, 0)), ((15, 3.6), (14, 2.6), (13, 1.6))):
        for x, y in pts: c.px(x, y, "m")                                # smoke curling back off it
    return c, {"B": "#4a4856", "S": "#363442", "F": "#2c2a34", "E": "#ff3b3b", "e": "#ff9a8a", "m": "#8a88a0"}, "SFEem"
def dread():
    c = Canvas(32, 24)
    c.ell(14, 10.6, 8.0, 3.0, "B")                                      # stretched body, mid-leap, belly tucked
    c.ell(8, 10.6, 4.2, 3.4, "B")                                       # hindquarters
    c.ell(21, 9.4, 3.6, 3.2, "B")                                       # shoulders
    c.poly([(22, 7.6), (26, 7.0), (31, 8.6), (31, 10.4), (27.6, 12.4), (23, 12)], "B")   # head thrust forward
    c.rect(26, 10, 30, 10, "X"); c.px(31, 10, "X")                      # snarling mouth
    for x in (26, 28, 30): c.px(x, 9.6, "T")
    for x in (27, 29): c.px(x, 10.6, "T")
    c.px(26, 8, "E"); c.px(27, 8, "e")                                  # burning eye
    c.line([(5, 10), (2.6, 8.4), (1.0, 7.6)], "B", 2)                   # tail out behind
    c.line([(8, 12.6), (5.6, 15)], "B", 2); c.line([(5, 15.4), (2.4, 17.2), (0.6, 17.6)], "B")      # hind legs flung back: thigh, shin
    c.line([(10.4, 13), (8.6, 15.6)], "F", 2); c.line([(8, 16), (5.6, 18.4), (4, 18.6)], "F")
    c.line([(21, 12.0), (23.4, 14.4)], "B", 2); c.line([(24, 15), (27, 16.6), (29, 16.6)], "B")    # forelegs reaching forward
    c.line([(19, 12.6), (20.6, 15.4)], "F", 2); c.line([(21, 16), (23.6, 18.2), (25.4, 18.4)], "F")
    for x, h in ((9, 3), (11, 4), (13, 5), (15, 5), (17, 4), (19, 3)):  # bone spines along the back
        c.line([(x, 7.6), (x - 1.2, 7.6 - h)], "G")
    for x in (13, 15, 17): c.line([(x, 10), (x - 0.4, 13)], "G")        # exposed ribs
    for (x, y) in ((10, 11), (20, 11), (6, 9)): c.px(x, y, "M")         # mange patches
    return c, {"B": "#5a4e46", "F": "#3a322c", "G": "#e2d6b4", "X": "#3a0c0c", "T": "#f0ead8", "E": "#ff8a1a", "e": "#ffe080",
               "M": "#7a3a2e"}, "GXTEeMF"

# ------------------------------------------------------------------ Blight Brute (32x32) and Rot Swarm (24x16)
def brute():
    c = Canvas(32, 32)
    c.ell(18, 13, 8.6, 6.6, "B")                        # hump, leaning forward
    c.ell(18.4, 19.6, 6.2, 5.0, "B")                    # gut
    c.poly([(4, 8), (12, 7), (13.6, 17), (12, 24), (2, 26), (0.6, 16)], "C")   # rock-crusted arm
    c.ell(6.4, 26.4, 5.4, 4.0, "C")                     # its boulder-like end on the ground
    for (x, y) in ((4, 11), (9, 13), (2, 18), (7, 17), (11, 21), (4, 23), (8, 26), (3, 27)): c.px(x, y, "c")   # glowing crystals
    for (x, y) in ((5, 10), (10, 16), (3, 21), (9, 23), (6, 28)): c.px(x, y, "Y")   # crystal cores
    for (x, y) in ((7, 12), (2, 15), (10, 19), (5, 25)): c.px(x, y, "k")            # cracks
    c.line([(26, 12), (28.6, 17), (29.4, 22), (28.6, 26)], "A", 3)                 # long right arm
    for dx in (-1.4, 0, 1.4): c.line([(28.6, 26), (28.6 + dx, 29)], "T")          # claws
    c.rect(13, 24, 16, 29, "D"); c.rect(20, 24, 23, 29, "D")                       # short bowed legs
    c.rect(12, 29, 16, 30, "f"); c.rect(19, 29, 24, 30, "f")
    for (x, h) in ((13, 5), (15, 7), (17, 6), (19, 7), (21, 5), (23, 4), (25, 3)):  # fungal stalks with glowing tips
        c.line([(x, 8), (x - 0.4, 8 - h)], "S"); c.px(x - 0.4, 7 - h, "Y")
    c.ell(20, 14.4, 4.4, 3.6, "H")                      # head sunk forward
    c.px(18, 12, "E"); c.px(22, 12, "E")                # small yellow eyes
    c.rect(17, 15, 23, 17, "R")                         # a wide mouth
    for x in (17, 19, 21, 23): c.px(x, 15, "T")         # teeth top
    for x in (18, 20, 22): c.px(x, 17, "T")             # teeth bottom
    for (x, y) in ((24, 19), (14, 20), (21, 22)): c.px(x, y, "P")   # boils
    return c, {"B": "#6c6742", "A": "#66613f", "C": "#5e574a", "c": "#a8b83a", "Y": "#e0f070", "k": "#2e2a22",
               "D": "#4e4a30", "f": "#38351f", "H": "#7c7650", "E": "#f0d040", "R": "#3a1210", "T": "#e8e0c0",
               "S": "#8e8b54", "P": "#a8a050"}, "cYkETRSP"
def rats():
    c = Canvas(24, 16)
    def rat(x, y, l, ch):
        c.ell(x, y, l, l * 0.7, ch)                                  # plump body
        c.ell(x + l + 0.6, y + 0.4, 1.6, 1.2, ch)                    # head
        c.px(x + l + 2.2, y + 0.8, "n")                              # pink nose
        c.px(x + l + 1.0, y - 0.2, "E")                              # red eye
        c.px(x + l + 0.2, y - 1.2, "e")                              # ear
        c.line([(x - l, y + 0.6), (x - l - 1.8, y + 1.4)], "t")      # short tail
        c.px(x - l * 0.3, y + l * 0.7, "D"); c.px(x + l * 0.6, y + l * 0.7, "D")   # feet
    rat(12.4, 4.6, 2.6, "G")                                         # the sick one, behind
    rat(18.0, 8.0, 2.4, "C")
    rat(5.4, 9.8, 3.0, "B")                                          # the big one in front
    rat(13.4, 11.6, 2.6, "C")
    for (x, y) in ((11, 4), (13, 5), (12, 3)): c.px(x, y, "s")       # mange spots on the sick one
    return c, {"B": "#5a4236", "C": "#6a5040", "G": "#6a7056", "D": "#2e221c", "E": "#ff3b3b", "e": "#a87060",
               "n": "#d89a90", "t": "#9a6458", "s": "#8a9068"}, "EentDs"

def sprites():
    cv, pv, fv = veil()
    return [("Pallid Prowler", "pallid_prowler", draw(PALLID, PALLID_PAL)),
            ("Blind Maw", "blind_maw", render(*maw())),
            ("Rot Swarm", "rot_swarm", render(*rats())),
            ("Veil Prowler", "veil_prowler", render(cv, pv, fv, alpha={"m": 120})),
            ("Dread Prowler", "dread_prowler", render(*dread())),
            ("Blight Brute", "blight_brute", render(*brute()))]

def sheet(path):
    here = os.path.dirname(os.path.abspath(__file__))
    human = Image.open(os.path.join(here, "..", "Assets", "Resources", "Sprites", "armor_atlas.png")).convert("RGBA").crop((0, 0, 16, 16))
    items = sprites(); Z = 8
    W = 1500; H = 70 + 3 * 330 + 170
    img = Image.new("RGBA", (W, H), hexc("#1b1d1f")); d = ImageDraw.Draw(img)
    f = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 15)
    tf = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 22)
    d.text((30, 20), "Mutants from Yan's references - reference | sprite (8x); bottom: 3x and true size next to a human", font=tf, fill=(230, 230, 230))
    for i, (name, key, im) in enumerate(items):
        col, row = i % 2, i // 2
        x0, y0 = 30 + col * 740, 70 + row * 330
        ref = os.path.join(here, "refs", "mutants", key + ".png")
        if os.path.exists(ref):
            r = Image.open(ref).convert("RGBA"); r.thumbnail((300, 280)); img.alpha_composite(r, (x0, y0))
        d.rectangle([x0 + 310, y0, x0 + 310 + 32 * Z, y0 + 32 * Z], fill=hexc("#2a2620"))
        big = im.resize((im.width * Z, im.height * Z), Image.NEAREST)
        img.alpha_composite(big, (x0 + 310 + (32 * Z - big.width) // 2, y0 + 32 * Z - big.height))
        d.text((x0, y0 + 32 * Z + 8), f"{name} ({im.width}x{im.height})", font=f, fill=(184, 188, 192))
    y = H - 160; d.rectangle([30, y - 10, W - 30, H - 20], fill=hexc("#2a2620")); x = 50
    for im in [human] + [it[2] for it in items]:
        img.alpha_composite(im.resize((im.width * 3, im.height * 3), Image.NEAREST), (x, y + 96 - im.height * 3))
        img.alpha_composite(im, (x + im.width, y + 128 - im.height)); x += im.width * 3 + 24
    img.convert("RGB").save(path)

if __name__ == "__main__":
    sheet(sys.argv[1] if len(sys.argv) > 1 else "mutants_v3_sheet.png")
