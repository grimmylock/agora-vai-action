# Demo sprite sheet for "agora vai": 16x16 sprites drawn from text grids, same style/size the
# project already uses (Resources/Sprites, PPU 16, Point filter).
import os, random
from PIL import Image, ImageDraw, ImageFont

OUT = os.path.dirname(os.path.abspath(__file__))
SPR = os.path.join(OUT, "sprites")
os.makedirs(SPR, exist_ok=True)

def hexc(h, a=255):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)

def mirror(halves):
    return [h + h[::-1] for h in halves]

def grid_to_img(rows, pal):
    assert len(rows) == 16, len(rows)
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    for y, row in enumerate(rows):
        assert len(row) == 16, (y, row, len(row))
        for x, ch in enumerate(row):
            if ch != ".":
                img.putpixel((x, y), pal[ch])
    return img

# ---------- Human base (front view), recolored per faction ----------
HUMAN = mirror([
    "......KK",
    ".....KHh",
    "....KHhh",
    "....KHMM",
    "....KMEm",
    "....KMmM",
    ".....KMm",
    "....KSSS",
    "...KSsSS",
    "..KSsKSS",
    "..KgKKSS",
    "...KKbbB",
    "....KSSK",
    "....KSSK",
    "....KBBK",
    "....KKKK",
])

def with_rifle(img, gun="#2e3134", hi="#6b7177"):
    g, h = hexc(gun), hexc(hi)
    for y in range(4, 14):
        img.putpixel((13, y), g)
    for y in range(5, 9):
        img.putpixel((14, y), h)
    img.putpixel((12, 10), g)
    return img

BASE = {"K": "#141617", "M": "#2b2f33", "m": "#5c636b", "E": "#9fd8e8", "b": "#3d3226", "B": "#2a2218"}
FACTIONS = {
    #             hood dark, hood light, suit, suit light, glove, lens
    "wanderer":  ("#4a5a3a", "#6b7f4f", "#5a4a36", "#7a6548", "#3a3a3a", "#9fd8e8"),
    "raider":    ("#7a1f1f", "#a83232", "#3b3530", "#57504a", "#5a3a2a", "#d9c27a"),
    "sentinel":  ("#2d3f5c", "#46618c", "#3c4654", "#5a6778", "#262b33", "#7fe0ff"),
    "obelisk":   ("#8a8f94", "#c9cdd1", "#4b4f54", "#6e7378", "#2b2d30", "#ff5a3c"),
    "surveyor": ("#b08a1e", "#e0b83a", "#c9a23a", "#e6c766", "#5a5a5a", "#a8ffb0"),
}
humans = {}
for name, (hd, hl, s, sl, gl, lens) in FACTIONS.items():
    pal = {k: hexc(v) for k, v in BASE.items()}
    pal.update({"H": hexc(hd), "h": hexc(hl), "S": hexc(s), "s": hexc(sl), "g": hexc(gl), "E": hexc(lens)})
    humans[name] = with_rifle(grid_to_img(HUMAN, pal))

# ---------- Mutants ----------
BLIND_MAW = [
    "................",
    "................",
    "................",
    "........KKKKK...",
    "..KKKK.KDFFFFK..",
    ".KFfffKDFFfFFFK.",
    "KfffffFFFFfFFFFK",
    "KTRTRfFFFFFFFFFK",
    "KRRRRKFFFFFFFFDK",
    "KTRTKDFFFFFFFDDK",
    ".KKK.KDDFFFFDDK.",
    ".....KDK.KDDKDK.",
    ".....KDK..KDKDK.",
    ".....KDK..KDKDK.",
    ".....KKK..KKKKK.",
    "................",
]
maw = grid_to_img(BLIND_MAW, {"K": hexc("#141617"), "F": hexc("#7a4a46"), "f": hexc("#a5655c"),
                              "D": hexc("#4e2c2b"), "T": hexc("#e8e0c8"), "R": hexc("#b02a2a")})

MIND_WEAVER = mirror([
    "....KKKK",
    "...KpppP",
    "..KppPpP",
    ".KpPpPPp",
    ".KPpPPpP",
    ".KPPPPPP",
    "..KPWvPP",
    "..KPvvPP",
    "...KPPPP",
    "....KPPK",
    "...KPKPK",
    "..KP.KPK",
    "..K..KP.",
    ".....KP.",
    "......K.",
    "........",
])
weaver = grid_to_img(MIND_WEAVER, {"K": hexc("#141617"), "P": hexc("#6b5a7a"), "p": hexc("#9a86a8"),
                                   "W": hexc("#f0e0ff"), "v": hexc("#c06bff")})
for x, y in [(1, 2), (14, 3), (0, 8), (15, 9), (2, 14), (13, 13)]:   # psi aura specks
    weaver.putpixel((x, y), hexc("#c06bff", 170))

# Veil Prowler: same idea as cloak - drawn at partial alpha
VEIL = [
    "................",
    "................",
    "..........KK....",
    ".........KGGK...",
    "........KGgGGK..",
    "..KKKKKKGGGGEK..",
    ".KGGgGGGGGGGGK..",
    "KGGGGGgGGGGGK...",
    ".KGGGGGGGGGK....",
    "..KGK.KGK.KGK...",
    "..KGK.KGK..KGK..",
    "..KK..KK....KK..",
    "................",
    "................",
    "................",
    "................",
]
veil = grid_to_img(VEIL, {"K": hexc("#141617", 150), "G": hexc("#8fa3a8", 110),
                          "g": hexc("#c8dade", 140), "E": hexc("#ff3b3b")})

# ---------- Artifacts (icons) ----------
def orb(core, glow, rim, tendrils=False):
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    for y in range(16):
        for x in range(16):
            d = ((x - 7.5) ** 2 + (y - 7.0) ** 2) ** 0.5
            if d < 2.2: img.putpixel((x, y), hexc(core))
            elif d < 4.2: img.putpixel((x, y), hexc(glow))
            elif d < 5.0: img.putpixel((x, y), hexc(rim))
            elif d < 6.4: img.putpixel((x, y), hexc(glow, 70))
    if tendrils:
        for x, y in [(5, 12), (5, 13), (4, 14), (8, 12), (8, 13), (8, 14), (10, 12), (11, 13), (11, 14)]:
            img.putpixel((x, y), hexc(rim))
    return img

drift_bubble = orb("#e8f6ff", "#6fb6e8", "#2c5a8a", tendrils=True)

PULSE_PEBBLE = [
    "................",
    "......KK........",
    ".....KrRK.......",
    "....KrRRRKK.....",
    "...KrRRRRDRK....",
    "...KrRRRRRDK....",
    "..KrRWRRRRDDK...",
    "..KrWRRRRRDDK...",
    "..KRRRRRRDDDK...",
    "...KRRRRDDDK....",
    "...KRRRDDDK.....",
    "....KRRDDK......",
    ".....KDDK.......",
    "......KK........",
    "................",
    "................",
]
pulse_pebble = grid_to_img(PULSE_PEBBLE, {"K": hexc("#1a0c0c"), "R": hexc("#b3212a"), "r": hexc("#e0525a"),
                                        "D": hexc("#6e1218"), "W": hexc("#ffd0d0")})

static_wisp = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
for (x, y) in [(7, 1), (8, 2), (7, 3), (6, 4), (7, 5), (8, 6), (9, 7), (8, 8), (7, 9), (6, 10), (7, 11),
               (8, 12), (7, 13), (4, 6), (5, 7), (11, 6), (10, 7), (3, 11), (4, 10), (12, 10), (11, 11)]:
    static_wisp.putpixel((x, y), hexc("#fff27a"))
for y in range(5, 10):
    for x in range(6, 10):
        if static_wisp.getpixel((x, y))[3] == 0:
            static_wisp.putpixel((x, y), hexc("#ffd23c", 200))

# ---------- Animated anomaly (Pyre), 6 frames ----------
def pyre_frame(seed):
    rnd = random.Random(seed)
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    cols = ["#5a1a08", "#b8360c", "#f2741c", "#ffc845", "#fff4c0"]
    for y in range(16):
        for x in range(16):
            # flame column narrows upward, jittered per frame
            width = max(0, (y - 2) * 0.45 + rnd.uniform(-1.2, 1.2))
            dist = abs(x - 7.5)
            if dist < width and y > 1:
                heat = 1 - dist / max(width, 0.01)
                heat = heat * (y / 15) + rnd.uniform(-0.15, 0.15)
                idx = min(4, max(0, int(heat * 5)))
                img.putpixel((x, y), hexc(cols[idx]))
    for x in range(3, 13):           # scorched ground ring
        img.putpixel((x, 15), hexc("#2a1a12"))
    return img

pyre_frames = [pyre_frame(i) for i in range(6)]

# ---------- Save individual 16x16 sprites ----------
sprites = [("unit_wanderer", humans["wanderer"], "Wanderer (player)"),
           ("unit_raider", humans["raider"], "Raider"),
           ("unit_sentinel", humans["sentinel"], "Sentinel"),
           ("unit_obelisk", humans["obelisk"], "Obelisk"),
           ("unit_surveyor", humans["surveyor"], "Surveyors"),
           ("mutant_blind_maw", maw, "Blind Maw"),
           ("mutant_mind_weaver", weaver, "Mind Weaver"),
           ("mutant_veil_prowler", veil, "Veil Prowler (cloaked)"),
           ("artifact_drift_bubble", drift_bubble, "Drift Bubble"),
           ("artifact_pulse_pebble", pulse_pebble, "Pulse Pebble"),
           ("artifact_static_wisp", static_wisp, "Static Wisp"),
           ("anomaly_pyre", pyre_frames[0], "Pyre (animated)")]
for fname, img, _ in sprites:
    img.save(os.path.join(SPR, fname + ".png"))
# animation as a horizontal strip (Unity slices it into frames)
strip = Image.new("RGBA", (16 * len(pyre_frames), 16), (0, 0, 0, 0))
for i, f in enumerate(pyre_frames):
    strip.paste(f, (16 * i, 0))
strip.save(os.path.join(SPR, "anomaly_pyre_strip.png"))

# ---------- Showcase sheet ----------
S = 8          # upscale
CELL = 16 * S
PAD = 44
LABEL = 26
cols = 4
rows = (len(sprites) + cols - 1) // cols
W = cols * (CELL + PAD) + PAD
H = 70 + rows * (CELL + PAD + LABEL) + PAD
sheet = Image.new("RGBA", (W, H), hexc("#1b1d1f"))
d = ImageDraw.Draw(sheet)
font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 15)
title = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 22)
d.text((PAD, 22), "agora vai - 16x16 sprite demo", font=title, fill=hexc("#e6e6e6"))

def checker(x0, y0):
    for yy in range(0, CELL, S * 2):
        for xx in range(0, CELL, S * 2):
            for (dx, dy) in [(0, 0), (S, S)]:
                d.rectangle([x0 + xx + dx, y0 + yy + dy, x0 + xx + dx + S - 1, y0 + yy + dy + S - 1], fill=hexc("#26292c"))

for i, (_, img, label) in enumerate(sprites):
    cx, cy = i % cols, i // cols
    x0 = PAD + cx * (CELL + PAD)
    y0 = 70 + cy * (CELL + PAD + LABEL)
    d.rectangle([x0, y0, x0 + CELL - 1, y0 + CELL - 1], fill=hexc("#222527"))
    checker(x0, y0)
    big = img.resize((CELL, CELL), Image.NEAREST)
    sheet.alpha_composite(big, (x0, y0))
    d.text((x0, y0 + CELL + 6), label, font=font, fill=hexc("#b8bcc0"))
sheet.convert("RGB").save(os.path.join(OUT, "sprite_demo_sheet.png"))

# animated preview (upscaled GIF)
gif = [f.resize((128, 128), Image.NEAREST) for f in pyre_frames]
bg = [Image.alpha_composite(Image.new("RGBA", (128, 128), hexc("#1b1d1f")), g).convert("RGB") for g in gif]
bg[0].save(os.path.join(OUT, "pyre_anomaly.gif"), save_all=True, append_images=bg[1:], duration=110, loop=0)
print("ok", len(sprites))
