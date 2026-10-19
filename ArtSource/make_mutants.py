# All 9 mutants from 04_Mutants_and_Factions.md in the locked agora-vai 16x16 style.
# Blind Maw, Mind Weaver and Veil Prowler are the approved demo sprites (unchanged); the other six
# are new. Cloak mutants (Veil, Alpha, Void) get both a revealed and a cloaked sprite, since the
# game shows them differently (file 04, Cloak).
import os
from PIL import Image, ImageDraw, ImageFont

OUT = os.path.dirname(os.path.abspath(__file__))
SPR = os.path.join(OUT, "mutants")
os.makedirs(SPR, exist_ok=True)
K = "#141617"

def hexc(h, a=255):
    h = h.lstrip("#"); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)
def mirror(halves): return [x + x[::-1] for x in halves]
def grid(rows, pal, alpha=None):
    assert len(rows) == 16, len(rows)
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    for y, row in enumerate(rows):
        assert len(row) == 16, (y, row, len(row))
        for x, ch in enumerate(row):
            if ch != ".":
                img.putpixel((x, y), hexc(pal[ch], (alpha or {}).get(ch, 255)))
    return img

def cloaked(rows, pal, keep, light):
    """Cloak look from the style guide (same as the Veil Prowler demo): body recolored to the cloak
    gray-blue at alpha ~110, highlights (chars in `light`) ~140, outline 150, eyes/glow kept at full."""
    cp, a = {}, {}
    for ch in pal:
        if ch in keep: cp[ch], a[ch] = pal[ch], 255
        elif ch == "K": cp[ch], a[ch] = K, 150
        elif ch in light: cp[ch], a[ch] = "#c8dade", 140
        else: cp[ch], a[ch] = "#8fa3a8", 110
    return grid(rows, cp, a)

# ---------------- approved demo sprites (unchanged) ----------------
BLIND_MAW = [
    "................", "................", "................", "........KKKKK...",
    "..KKKK.KDFFFFK..", ".KFfffKDFFfFFFK.", "KfffffFFFFfFFFFK", "KTRTRfFFFFFFFFFK",
    "KRRRRKFFFFFFFFDK", "KTRTKDFFFFFFFDDK", ".KKK.KDDFFFFDDK.", ".....KDK.KDDKDK.",
    ".....KDK..KDKDK.", ".....KDK..KDKDK.", ".....KKK..KKKKK.", "................",
]
MAW_PAL = {"K": K, "F": "#7a4a46", "f": "#a5655c", "D": "#4e2c2b", "T": "#e8e0c8", "R": "#b02a2a"}

MIND_WEAVER = mirror([
    "....KKKK", "...KpppP", "..KppPpP", ".KpPpPPp", ".KPpPPpP", ".KPPPPPP", "..KPWvPP", "..KPvvPP",
    "...KPPPP", "....KPPK", "...KPKPK", "..KP.KPK", "..K..KP.", ".....KP.", "......K.", "........",
])
WEAVER_PAL = {"K": K, "P": "#6b5a7a", "p": "#9a86a8", "W": "#f0e0ff", "v": "#c06bff"}
WEAVER_SPECKS = [(1, 2), (14, 3), (0, 8), (15, 9), (2, 14), (13, 13)]

VEIL = [
    "................", "................", "..........KK....", ".........KGGK...",
    "........KGgGGK..", "..KKKKKKGGGGEK..", ".KGGgGGGGGGGGK..", "KGGGGGgGGGGGK...",
    ".KGGGGGGGGGK....", "..KGK.KGK.KGK...", "..KGK.KGK..KGK..", "..KK..KK....KK..",
    "................", "................", "................", "................",
]
VEIL_PAL = {"K": K, "G": "#8fa3a8", "g": "#c8dade", "E": "#ff3b3b"}
VEIL_REVEALED_PAL = {"K": K, "G": "#6a6058", "g": "#8a7e72", "E": "#ff3b3b"}

# ---------------- new ----------------
# Pallid Prowler (T1, pack): lumpy pig-like flesh mutant, side view; toxic drool = Toxic Bite.
PALLID_PROWLER = [
    "................", "................", "................", "........KKK.....",
    "...KKK.KfffKK...", "..KfffKFFfFFFK..", ".KfWeFFFFFFDFFK.", "KFFfFFFFFFFFFFK.",
    "KNNFFFFFFDFFFDK.", "KTGFFFFFFFFFDDK.", ".KGKDFFFFFFDDK..", "..G.KDKKKKDDK...",
    "....KDK..KDK....", "....KDK..KDK....", "....KKK..KKK....", "................",
]
FLESH_PAL = {"K": K, "F": "#9a7a70", "f": "#c09a8c", "D": "#6a4a44", "W": "#e8e0c8", "e": "#2a1a14",
             "N": "#4a2a28", "T": "#e8e0c8", "G": "#8acb3a"}

# Rot Swarm (T1, swarm): mangy rat-thing, small and low; comes in groups of 3+.
ROT_SWARM = [
    "................", "................", "................", "................",
    "................", "................", "................", "...KK...........",
    "..KFFK.KfKfK....", ".KFRFFKFFfFFKK..", "KPFFFFFFFFFFFFK.", ".KTFFFFFFFDFFFK.",
    "..KKDFFFFFDDDKPP", "...KDK.KDK.KK...", "...KK..KK.......", "................",
]
SWARM_PAL = {"K": K, "F": "#6a5a4a", "f": "#8a7a64", "D": "#4a3e32", "R": "#ff3b3b", "P": "#c08a80", "T": "#e8e0c8"}

# Alpha Prowler (T3, ambush, cloak): hunched bloodsucker-type, mouth tentacles, long clawed arms.
ALPHA_PROWLER = mirror([
    "........", "......KK", ".....KSs", "....KSsS", "....KSES", "....KSST", "..KKKKTT", ".KSsSKTT",
    "KSsSSKT.", "KSKKSSSS", "KSKKSSSS", "KCK.KSSS", ".C..KSSK", "....KSSK", "...KSSK.", "...KKKK.",
])
ALPHA_PAL = {"K": K, "S": "#6a5a5a", "s": "#8a7676", "T": "#9a4a4a", "E": "#ff3b3b", "C": "#e8e0c8"}

# Blight Brute (T4, berserk, 40% Phys-DR, Ground Slam): huge, calloused bone plates, tiny head.
BLIGHT_BRUTE = mirror([
    "........", "........", "......KK", "..KKKKFf", ".KPpPKEF", "KPpPPKFM", "KPPFFFFF", "KFFFFfFF",
    "KFfFFFFF", "KFFKFFFF", "KFFKFFDF", "KCCKDFFD", "...KFFFK", "...KFFFK", "...KDDDK", "...KKKKK",
])
BRUTE_PAL = {"K": K, "F": "#7a6a5a", "f": "#9a8a72", "D": "#4e4236", "P": "#5a5650", "p": "#8a867c",
             "E": "#ffb347", "M": "#b02a2a", "C": "#c8c0a0"}

# Dread Prowler (T4, berserk, Leap): two-headed chimera-type beast, side view, crouched to leap.
DREAD_PROWLER = [
    "................", "................", ".KKKK...........", "KHHhHK..........",
    "KHEHHHK.KKKKK...", "KTRTHHKKFFfFFKK.", ".KKKKHHFFFFfFFFK", "KFFKKHFFFFFFFFFK",
    "KEFFFFFFFFFFFFDK", "KTRTFFFFFFFFFDDK", ".KKKFFDFFFFFDDK.", "...KFDKKKKKFDK..",
    "..KCDK....KDDK..", "..KCK.....KDCK..", "..KK......KKK...", "................",
]
DREAD_PAL = {"K": K, "F": "#5a463c", "f": "#7a6252", "D": "#3a2c26", "H": "#7a5a44", "h": "#9a7a5e", "E": "#ffcc3a", "T": "#e8e0c8", "R": "#b02a2a", "C": "#e8e0c8"}

# Void Weaver (T5, ranged psi, cloak, Neural Overload): swollen-headed robed thing, psi-lit hands.
VOID_WEAVER = mirror([
    "....KKKK", "...KSsss", "..KSsSsS", "..KSSSSS", "..KSSWSS", "...KSSSS", "....KSSR", "...KRRRR",
    "..KRrRRR", "..KRrRRR", ".KRRRRRR", ".KGRRRRR", "..KRRRRR", "..KRRRRR", "...KRRRR", "...KKKKK",
])
VOID_PAL = {"K": K, "S": "#5a5078", "s": "#7a70a0", "R": "#241f34", "r": "#3a3450", "W": "#e0ecff", "G": "#6a8aff"}
VOID_SPECKS = [(0, 3), (15, 5), (1, 13), (14, 12), (2, 0)]

def specks(img, pts, color):
    for x, y in pts:
        if img.getpixel((x, y))[3] == 0:
            img.putpixel((x, y), hexc(color, 170))
    return img

# name, file key, image, label
SPRITES = [
    ("Blind Maw", "blind_maw", grid(BLIND_MAW, MAW_PAL), "T1 Blind Maw"),
    ("Pallid Prowler", "pallid_prowler", grid(PALLID_PROWLER, FLESH_PAL), "T1 Pallid Prowler"),
    ("Rot Swarm", "rot_swarm", grid(ROT_SWARM, SWARM_PAL), "T1 Rot Swarm"),
    ("Veil Prowler", "veil_prowler", grid(VEIL, VEIL_REVEALED_PAL), "T2 Veil Prowler"),
    ("Veil Prowler (cloaked)", "veil_prowler_cloaked", grid(VEIL, VEIL_PAL, {"K": 150, "G": 110, "g": 140}), "T2 Veil (cloaked)"),
    ("Alpha Prowler", "alpha_prowler", grid(ALPHA_PROWLER, ALPHA_PAL), "T3 Alpha Prowler"),
    ("Alpha Prowler (cloaked)", "alpha_prowler_cloaked", cloaked(ALPHA_PROWLER, ALPHA_PAL, "E", "sC"), "T3 Alpha (cloaked)"),
    ("Mind Weaver", "mind_weaver", specks(grid(MIND_WEAVER, WEAVER_PAL), WEAVER_SPECKS, "#c06bff"), "T3 Mind Weaver"),
    ("Blight Brute", "blight_brute", grid(BLIGHT_BRUTE, BRUTE_PAL), "T4 Blight Brute"),
    ("Dread Prowler", "dread_prowler", grid(DREAD_PROWLER, DREAD_PAL), "T4 Dread Prowler"),
    ("Void Weaver", "void_weaver", specks(grid(VOID_WEAVER, VOID_PAL), VOID_SPECKS, "#6a8aff"), "T5 Void Weaver"),
    ("Void Weaver (cloaked)", "void_weaver_cloaked", cloaked(VOID_WEAVER, VOID_PAL, "WG", "sr"), "T5 Void (cloaked)"),
]
for _, k, img, _ in SPRITES:
    img.save(os.path.join(SPR, f"mutant_{k}.png"))

# preview sheet
S8, PAD, LABEL, COLS = 8, 40, 24, 4
CELL = 16 * S8
rows_n = (len(SPRITES) + COLS - 1) // COLS
sheet = Image.new("RGBA", (COLS * (CELL + PAD) + PAD, 74 + rows_n * (CELL + PAD + LABEL)), hexc("#1b1d1f"))
d = ImageDraw.Draw(sheet)
font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 14)
d.text((PAD, 22), "agora vai - mutants", font=ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 22), fill=hexc("#e6e6e6"))
for i, (_, _, img, label) in enumerate(SPRITES):
    x0 = PAD + (i % COLS) * (CELL + PAD); y0 = 74 + (i // COLS) * (CELL + PAD + LABEL)
    d.rectangle([x0, y0, x0 + CELL - 1, y0 + CELL - 1], fill=hexc("#222527"))
    for yy in range(0, CELL, 2 * S8):
        for xx in range(0, CELL, 2 * S8):
            for dx, dy in [(0, 0), (S8, S8)]:
                d.rectangle([x0 + xx + dx, y0 + yy + dy, x0 + xx + dx + S8 - 1, y0 + yy + dy + S8 - 1], fill=hexc("#26292c"))
    sheet.alpha_composite(img.resize((CELL, CELL), Image.NEAREST), (x0, y0))
    d.text((x0, y0 + CELL + 5), label, font=font, fill=hexc("#b8bcc0"))
sheet.convert("RGB").save(os.path.join(OUT, "mutants_sheet.png"))
print("ok", len(SPRITES))
