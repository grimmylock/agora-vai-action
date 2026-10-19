# Terrain tileset for the tactical map, in the locked agora-vai 16x16 style.
# Writes:
#   terrain_atlas.png          - every cell, 16 per row (also copied as terrain_atlas.bytes for Unity:
#                                TacticalGridView loads it as a TextAsset + LoadImage, so it's always
#                                CPU-readable no matter how the importer is set up)
#   terrain_names.txt          - cell order, one name per line ("tree#2" = 3rd variant of look "tree")
#   terrain_sheet.png          - labelled x4 preview sheet
# Cell kinds:
#   g_<ground>#n   opaque floor tiles (GroundType)
#   <look>#n       transparent object sprites drawn over the floor (TerrainTile.Look / type defaults)
#   fx_*           overlays (cracks for damaged cover, highlight frame)
import os, random, shutil
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
T = 16
K = "#141617"

def hx(h, a=255):
    h = h.lstrip("#"); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)

def blank(): return Image.new("RGBA", (T, T), (0, 0, 0, 0))

def grid(rows, pal):
    assert len(rows) == 16 and all(len(r) == 16 for r in rows), rows
    im = blank()
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch != ".": im.putpixel((x, y), hx(pal[ch]) if isinstance(pal[ch], str) else pal[ch])
    return im

CELLS = []   # (name, image)
def add(name, im): CELLS.append((name, im))

# ------------------------------------------------------------------ grounds
def noise(base, dark, light, seed, dens=.18, ldens=.08, blades=0, pebbles=0, pebble_col=None):
    r = random.Random(seed); im = Image.new("RGBA", (T, T), hx(base))
    for y in range(T):
        for x in range(T):
            v = r.random()
            if v < dens: im.putpixel((x, y), hx(dark))
            elif v > 1 - ldens: im.putpixel((x, y), hx(light))
    for _ in range(blades):
        x, y = r.randrange(T), r.randrange(1, T)
        im.putpixel((x, y), hx(light)); im.putpixel((x, y - 1), hx(light))
    for _ in range(pebbles):
        x, y = r.randrange(T - 1), r.randrange(T - 1)
        im.putpixel((x, y), hx(pebble_col or light)); im.putpixel((x + 1, y + 1), hx(dark))
    return im

def planks(seed, base="#5a4632", dark="#46362a", light="#6e5840"):
    r = random.Random(seed); im = noise(base, dark, light, seed, dens=.08, ldens=.05)
    d = ImageDraw.Draw(im)
    for y in (3, 7, 11, 15): d.line([(0, y), (15, y)], fill=hx(dark))
    for row, y in enumerate((0, 4, 8, 12)):
        x = (row * 5 + seed * 3) % 16; d.line([(x, y), (x, y + 2)], fill=hx(dark))
    if r.random() < .5: im.putpixel((r.randrange(16), r.choice((1, 5, 9, 13))), hx("#2e241a"))
    return im

def tiles_floor(seed):
    im = noise("#6e7270", "#5e6260", "#7e8280", seed, dens=.08, ldens=.05); d = ImageDraw.Draw(im)
    d.line([(0, 7), (15, 7)], fill=hx("#4e5250")); d.line([(7, 0), (7, 15)], fill=hx("#4e5250"))
    d.line([(0, 15), (15, 15)], fill=hx("#4e5250")); d.line([(15, 0), (15, 15)], fill=hx("#4e5250"))
    r = random.Random(seed)
    if r.random() < .6:   # a crack or a stain - abandoned lab
        x, y = r.randrange(1, 14), r.randrange(1, 14); im.putpixel((x, y), hx("#3e4240")); im.putpixel((x + 1, y + 1), hx("#3e4240"))
    return im

def water(seed):
    im = noise("#2e4248", "#263a40", "#3e5860", seed, dens=.12, ldens=.08); d = ImageDraw.Draw(im); r = random.Random(seed)
    for _ in range(2):
        x, y = r.randrange(1, 11), r.randrange(2, 14); d.line([(x, y), (x + 3, y)], fill=hx("#5e8088"))
    return im

GROUNDS = {
    "grass":     lambda s: noise("#4a5a32", "#3e4c2a", "#5e6e3c", s, blades=5),
    "darkgrass": lambda s: noise("#34442a", "#2a3822", "#46583a", s + 100, blades=4),
    "dirt":      lambda s: noise("#5e4e38", "#4e402e", "#6e5e44", s + 20, pebbles=2),
    "mud":       lambda s: noise("#3e3a2c", "#2e2c22", "#4a4636", s + 30, dens=.25),
    "asphalt":   lambda s: noise("#3a3c3e", "#323436", "#46484a", s + 40, dens=.12, ldens=.05),
    "concrete":  lambda s: noise("#5e5e58", "#525250", "#6c6c66", s + 50, dens=.1, ldens=.06, pebbles=1, pebble_col="#4a4a46"),
    "floor":     lambda s: planks(s),
    "labtile":   lambda s: tiles_floor(s),
    "sand":      lambda s: noise("#8a7a56", "#7a6a4a", "#9a8a64", s + 70, dens=.14, ldens=.08),
    "gravel":    lambda s: noise("#5a5852", "#46443e", "#76746c", s + 80, dens=.3, ldens=.2),
    "water":     lambda s: water(s),
    "cavefloor": lambda s: noise("#3a3632", "#2e2a26", "#4a4640", s + 90, dens=.22, ldens=.06, pebbles=3),
}
for gname, fn in GROUNDS.items():
    for v in range(3): add(f"g_{gname}#{v}", fn(v))

# ------------------------------------------------------------------ solids (walls are edge-outlined at runtime)
def brick(base, mortar, seed=0):
    im = Image.new("RGBA", (T, T), hx(base)); d = ImageDraw.Draw(im)
    for yy in range(3, T, 4):
        d.line([(0, yy), (15, yy)], fill=hx(mortar))
        off = 0 if (yy // 4) % 2 else 4
        for xx in range(off, T, 8): d.line([(xx, yy - 3), (xx, yy - 1)], fill=hx(mortar))
    r = random.Random(seed)
    for _ in range(2): im.putpixel((r.randrange(16), r.randrange(16)), hx(mortar))
    return im
add("wall#0", brick("#7a6e5e", "#5a5044", 1)); add("wall#1", brick("#76685a", "#5a5044", 2))
def plaster(seed):
    im = noise("#8a8678", "#7a7668", "#9a968a", seed + 300, dens=.1, ldens=.06); r = random.Random(seed)
    d = ImageDraw.Draw(im)
    x, y = r.randrange(2, 10), r.randrange(2, 10)       # flaked plaster showing brick
    d.rectangle([x, y, x + 4, y + 2], fill=hx("#7a5a46")); d.line([(x, y + 1), (x + 4, y + 1)], fill=hx("#5a4034"))
    return im
add("plaster#0", plaster(0)); add("plaster#1", plaster(1))
def concrete_wall(seed):
    im = noise("#6a6a64", "#5a5a54", "#7a7a72", seed + 310, dens=.1, ldens=.05); d = ImageDraw.Draw(im)
    d.line([(0, 7), (15, 7)], fill=hx("#52524c")); r = random.Random(seed)
    if r.random() < .7: d.line([(r.randrange(2, 13), 8), (r.randrange(2, 13), 15)], fill=hx("#46463f"))  # rebar stain / crack
    return im
add("concrete#0", concrete_wall(0)); add("concrete#1", concrete_wall(1))
def window_wall():
    im = brick("#7a6e5e", "#5a5044", 3); d = ImageDraw.Draw(im)
    d.rectangle([3, 5, 12, 11], fill=hx("#1e2a30")); d.rectangle([3, 5, 12, 11], outline=hx("#5a5044"))
    d.line([(4, 6), (8, 6)], fill=hx("#7f98a0")); d.point([(10, 9)], fill=hx("#7f98a0"))  # cracked glass glint
    return im
add("window#0", window_wall())

def tree(seed):
    r = random.Random(seed); im = blank(); d = ImageDraw.Draw(im)
    d.ellipse([0, 0, 15, 15], fill=hx(K)); d.ellipse([1, 1, 14, 14], fill=hx("#2c4022"))
    for _ in range(10):
        x, y = r.randrange(3, 12), r.randrange(2, 11)
        d.rectangle([x, y, x + 1, y + 1], fill=hx(r.choice(["#3e5a2a", "#4a6a32", "#56783a"])))
    for _ in range(4):
        x, y = r.randrange(4, 12), r.randrange(9, 13); d.point([(x, y)], fill=hx("#22321c"))
    return im
for s in range(4): add(f"tree#{s}", tree(s))
def dead_tree(seed):
    r = random.Random(seed); im = blank(); d = ImageDraw.Draw(im)
    trunk = [((8, 15), (8, 4)), ((8, 8), (3, 3)), ((8, 7), (13, 2)), ((8, 11), (13, 9)), ((8, 10), (2, 8))]
    for a, b in trunk: d.line([a, b], fill=hx(K), width=3)
    for a, b in trunk: d.line([a, b], fill=hx("#4a3a2a"), width=1)
    d.point([(8, 5), (8, 9)], fill=hx("#6a5440"))
    return im
add("deadtree#0", dead_tree(0)); add("deadtree#1", dead_tree(1).transpose(Image.FLIP_LEFT_RIGHT))
def red_tree(seed):   # Crimson Woods
    r = random.Random(seed); im = blank(); d = ImageDraw.Draw(im)
    d.ellipse([0, 0, 15, 15], fill=hx(K)); d.ellipse([1, 1, 14, 14], fill=hx("#5a2418"))
    for _ in range(10):
        x, y = r.randrange(3, 12), r.randrange(2, 11)
        d.rectangle([x, y, x + 1, y + 1], fill=hx(r.choice(["#7a3220", "#8e4228", "#a04a2a"])))
    return im
add("redtree#0", red_tree(0)); add("redtree#1", red_tree(1))

ROCK = {"K": K, "r": "#6e6a62", "R": "#8a867c", "d": "#4e4a44"}
add("rock#0", grid(["................", "................", ".....KKKKK......", "...KKrRRRrKK....", "..KrRRRRRRrrK...",
                    ".KrRRRRRrrrrrK..", ".KrRRRrrrrrrrdK.", ".KrrRrrrrrrrddK.", ".KdrrrrrrrrrddK.", ".KddrrrrrrrdddK.",
                    "..KddddrrdddddK.", "...KKdddddddKK..", ".....KKKKKKK....", "................", "................", "................"], ROCK))
add("rock#1", grid(["................", "................", "................", "........KKKK....", "..KKK..KrRRrK...",
                    ".KrRrK.KRRrrdK..", ".KRRrrKKrrrrdK..", ".KrrrrrKrrrddK..", "..KdrrrrrrddK...", "..KKdddrrdddK...",
                    ".KrRrKKddddK....", ".KRRrrdKKKK.....", ".KdrrddK........", "..KKKKK.........", "................", "................"], ROCK))
add("cliff#0", noise("#4e4a44", "#3e3a34", "#625e56", 400, dens=.25, ldens=.12, pebbles=3))   # cave / embankment wall (edge-outlined)
add("cliff#1", noise("#4a463e", "#3a3630", "#5e5a52", 401, dens=.25, ldens=.12, pebbles=3))

# connecting "mass" textures for big solid regions (edge-outlined at runtime like walls)
def foliage(seed, base="#2c4022", cols=("#3e5a2a", "#4a6a32", "#22321c")):
    r = random.Random(seed); im = Image.new("RGBA", (T, T), hx(base)); d = ImageDraw.Draw(im)
    for _ in range(14):
        x, y = r.randrange(0, 15), r.randrange(0, 15)
        d.rectangle([x, y, x + 1, y + r.randrange(0, 2)], fill=hx(r.choice(cols)))
    for _ in range(3):
        x, y = r.randrange(1, 14), r.randrange(1, 14); d.point([(x, y)], fill=hx("#5a7a3a"))
    return im
add("foliage#0", foliage(1)); add("foliage#1", foliage(2))
add("redfoliage#0", foliage(3, "#5a2418", ("#7a3220", "#8e4228", "#3e180e"))); add("redfoliage#1", foliage(4, "#5a2418", ("#7a3220", "#8e4228", "#3e180e")))
def junk(seed):
    r = random.Random(seed); im = Image.new("RGBA", (T, T), hx("#4a3e34")); d = ImageDraw.Draw(im)
    for _ in range(12):
        x, y = r.randrange(0, 14), r.randrange(0, 15); w = r.randrange(1, 5)
        d.rectangle([x, y, x + w, y + r.randrange(0, 2)], fill=hx(r.choice(["#7a3a22", "#6a6e72", "#8a5a32", "#3a4a5a", "#2e2a26", "#5a5e54"])))
    for _ in range(4): d.point([(r.randrange(16), r.randrange(16))], fill=hx("#9a9e92"))
    return im
add("junk#0", junk(1)); add("junk#1", junk(2))
def container(base, rib, light, door=False):
    im = blank(); d = ImageDraw.Draw(im)
    d.rectangle([0, 1, 15, 15], fill=hx(K)); d.rectangle([1, 2, 14, 14], fill=hx(base))
    for x in range(2, 14, 2): d.line([(x, 3), (x, 13)], fill=hx(rib))
    d.line([(1, 2), (14, 2)], fill=hx(light))
    if door: d.rectangle([5, 6, 10, 9], fill=hx("#c9a23a")); d.rectangle([5, 6, 10, 9], outline=hx(K))
    return im
add("container#0", container("#3a5a6a", "#2e4a58", "#4e7282"))
add("container#1", container("#5a3022", "#4a2418", "#74402e"))
add("container#2", container("#4a5a32", "#3a4826", "#5e7040"))
def car(seed):
    r = random.Random(seed); im = blank(); d = ImageDraw.Draw(im)
    body = r.choice(["#4a5a6a", "#6a5a3a", "#5a3a3a", "#5a6050"]); roof = "#2a3038"
    d.rounded_rectangle([0, 1, 15, 14], radius=2, fill=hx(K)); d.rounded_rectangle([1, 2, 14, 13], radius=2, fill=hx(body))
    d.rectangle([4, 3, 11, 12], fill=hx(roof)); d.rectangle([5, 4, 10, 11], fill=hx("#3a424c"))
    d.line([(1, 7), (3, 7)], fill=hx(K)); d.line([(12, 7), (14, 7)], fill=hx(K))
    for _ in range(4): d.point([(r.randrange(1, 15), r.randrange(2, 14))], fill=hx("#7a3a22"))   # rust
    if seed % 2: d.line([(6, 5), (9, 9)], fill=hx("#8aa0a8"))                                   # smashed windscreen
    return im
add("wreck#0", car(0)); add("wreck#1", car(1)); add("wreck#2", car(2))
def scrap(seed):
    r = random.Random(seed); im = blank(); d = ImageDraw.Draw(im)
    d.polygon([(1, 15), (2, 6), (6, 2), (11, 3), (14, 8), (15, 15)], fill=hx(K))
    d.polygon([(2, 14), (3, 7), (6, 3), (10, 4), (13, 8), (14, 14)], fill=hx("#5a4a3e"))
    for _ in range(9):
        x, y = r.randrange(3, 13), r.randrange(4, 14)
        d.rectangle([x, y, x + r.randrange(1, 3), y], fill=hx(r.choice(["#7a3a22", "#6a6e72", "#8a5a32", "#3a4a5a", "#2e2a26"])))
    return im
add("scrap#0", scrap(0)); add("scrap#1", scrap(1))
def machine():
    im = blank(); d = ImageDraw.Draw(im)
    d.rectangle([0, 0, 15, 15], fill=hx(K)); d.rectangle([1, 1, 14, 14], fill=hx("#4a5248"))
    d.rectangle([1, 1, 14, 2], fill=hx("#5e6a5c")); d.rectangle([3, 5, 7, 9], fill=hx("#2e3430")); d.rectangle([3, 5, 7, 9], outline=hx(K))
    d.ellipse([9, 4, 13, 8], fill=hx("#6a6e72")); d.ellipse([9, 4, 13, 8], outline=hx(K)); d.point([(11, 6)], fill=hx(K))
    d.line([(2, 12), (13, 12)], fill=hx("#c9a23a")); d.point([(3, 12), (6, 12), (9, 12), (12, 12)], fill=hx(K))   # hazard stripe
    return im
add("machine#0", machine())
def hull(seed):
    im = noise("#5a3a2a", "#4a2e20", "#6e4a34", seed + 500, dens=.2, ldens=.1); d = ImageDraw.Draw(im)
    for y in (4, 11): d.line([(0, y), (15, y)], fill=hx("#3a2418"))
    for x in (2, 13): d.point([(x, 2), (x, 7), (x, 13)], fill=hx("#8a6a50"))   # rivets
    return im
add("hull#0", hull(0)); add("hull#1", hull(1))
def pipes():
    im = blank(); d = ImageDraw.Draw(im)
    for y, c in ((2, "#6a6e72"), (9, "#5a5e62")):
        d.rectangle([0, y, 15, y + 4], fill=hx(K)); d.rectangle([0, y + 1, 15, y + 3], fill=hx(c)); d.line([(0, y + 1), (15, y + 1)], fill=hx("#8a8e92"))
    d.rectangle([6, 1, 9, 14], fill=hx(K)); d.rectangle([7, 1, 8, 14], fill=hx("#7a3a22"))   # rusty flange
    return im
add("pipes#0", pipes())
def pillar():
    im = blank(); d = ImageDraw.Draw(im)
    d.rectangle([3, 3, 12, 12], fill=hx(K)); d.rectangle([4, 4, 11, 11], fill=hx("#6a6a64")); d.rectangle([4, 4, 11, 5], fill=hx("#86867e"))
    d.line([(6, 7), (8, 10)], fill=hx("#52524c")); return im
add("pillar#0", pillar())

# ------------------------------------------------------------------ light cover (C)
def bush(seed):
    r = random.Random(seed); im = blank(); d = ImageDraw.Draw(im)
    for _ in range(5):
        x, y, rr = r.randrange(4, 12), r.randrange(5, 11), r.randrange(3, 5)
        d.ellipse([x - rr - 1, y - rr - 1, x + rr + 1, y + rr + 1], fill=hx(K))
    for _ in range(5):
        x, y, rr = r.randrange(5, 11), r.randrange(6, 10), r.randrange(2, 4)
        d.ellipse([x - rr, y - rr, x + rr, y + rr], fill=hx("#3e5a2a"))
    for _ in range(7):
        x, y = r.randrange(4, 12), r.randrange(4, 11)
        if im.getpixel((x, y))[:3] == hx("#3e5a2a")[:3]: im.putpixel((x, y), hx("#5a7a3a"))
    return im
for s in range(3): add(f"bush#{s}", bush(s))
CRATE = {"K": K, "w": "#6a4e30", "W": "#8a6a44", "d": "#4a3420", "s": "#2e2418"}
add("crate#0", grid(["................", "................", "..KKKKKKKKKKKK..", "..KWWWWWWWWWWK..", "..KWdddddddwWK..",
                     "..KWdWwwwwdwWK..", "..KWdwWwwwdwWK..", "..KWdwwWwwdwWK..", "..KWdwwwWwdwWK..", "..KWdwwwwWdwWK..",
                     "..KWdwwwwwWwWK..", "..KWdddddddwWK..", "..KWwwwwwwwwWK..", "..KKKKKKKKKKKK..", "................", "................"], CRATE))
add("crate#1", grid(["................", "................", "................", ".KKKKKKKKK......", ".KWWWWWWWK......",
                     ".KWsWsWsWK.KKKK.", ".KwwwwwwwKKWWWK.", ".KWsWsWsWKKwswK.", ".KwwwwwwwKKWWWK.", ".KWsWsWsWKKwswK.",
                     ".KddddddK.KdddK.", ".KKKKKKKK..KKKK.", "................", "................", "................", "................"], CRATE))
def fence(vertical=False, torn=False):
    im = blank(); d = ImageDraw.Draw(im); mesh = hx("#8a8e92", 190); wire = hx("#6a6e72")
    if not torn:
        for xx in range(0, T, 3): d.line([(xx, 4), (xx + 2, 11)], fill=mesh)
        for xx in range(2, T + 2, 3): d.line([(xx, 4), (xx - 2, 11)], fill=mesh)
        d.line([(0, 4), (15, 4)], fill=wire); d.line([(0, 11), (15, 11)], fill=wire)
        for xx in range(0, T, 4): d.point([(xx, 3), (xx + 2, 2)], fill=hx("#b0b4b8"))   # razor wire
    else:
        d.line([(0, 4), (5, 9)], fill=mesh); d.line([(0, 11), (3, 13)], fill=mesh); d.line([(12, 10), (15, 4)], fill=mesh)
        d.line([(0, 4), (4, 4)], fill=wire); d.line([(12, 4), (15, 4)], fill=wire)
    d.rectangle([7, 2, 8, 13], fill=hx(K)); d.line([(7, 3), (7, 12)], fill=wire)
    return im.rotate(90) if vertical else im
add("fence#0", fence()); add("fence_v#0", fence(vertical=True)); add("fence_torn#0", fence(torn=True))
def tires():
    im = blank(); d = ImageDraw.Draw(im)
    for (x, y, r) in [(5, 9, 4), (11, 10, 4), (8, 5, 4)]:
        d.ellipse([x - r - 1, y - r - 1, x + r + 1, y + r + 1], fill=hx(K))
        d.ellipse([x - r, y - r, x + r, y + r], fill=hx("#2a2a2c"))
        d.arc([x - r, y - r, x + r, y + r], 200, 290, fill=hx("#46464a"))
        d.ellipse([x - 1, y - 1, x + 1, y + 1], fill=hx("#0e0f10"))
    return im
add("tires#0", tires())
def debris(seed):
    r = random.Random(seed); im = blank(); d = ImageDraw.Draw(im)
    for _ in range(4):
        x, y, w, h = r.randrange(1, 10), r.randrange(3, 11), r.randrange(3, 6), r.randrange(2, 4)
        c = r.choice(["#6a6a64", "#7a6e5e", "#5a5a54", "#6a4e30"])
        d.rectangle([x - 1, y - 1, x + w, y + h], fill=hx(K)); d.rectangle([x, y, x + w - 1, y + h - 1], fill=hx(c))
        d.line([(x, y), (x + w - 1, y)], fill=hx("#8a8a82"))
    return im
add("debris#0", debris(3)); add("debris#1", debris(7))
def reeds(seed):
    r = random.Random(seed); im = blank(); d = ImageDraw.Draw(im)
    for _ in range(9):
        x = r.randrange(1, 15); top = r.randrange(1, 7)
        d.line([(x, 15), (x, top)], fill=hx("#5a6a3a")); d.point([(x, top)], fill=hx("#8a7a4a"))
        if r.random() < .5: d.point([(x + 1, top + 1)], fill=hx("#6e5a32"))
    for x in range(0, 16, 2): d.point([(x, 15)], fill=hx("#2e3a22"))
    return im
add("reeds#0", reeds(1)); add("reeds#1", reeds(2))
LOGS = {"K": K, "b": "#6a4a2e", "B": "#8a6a44", "d": "#4a3220", "o": "#9a7a54", "O": "#c0a070"}
add("logs#0", grid(["................", "................", "................", "................", "..KKKKKKKKKKKK..",
                    ".KbBBBBBBBBBBoK.", ".KbbbbbbbbbbbOK.", ".KdddddddddddoK.", "..KKKKKKKKKKKK..", ".KbBBBBBBBBBoK..",
                    ".KbbbbbbbbbbOK..", ".KddddddddddoK..", "..KKKKKKKKKKK...", "................", "................", "................"], LOGS))
add("rustbarrel#0", grid(["................", "................", "....KKKKKKKK....", "...KrRRRRRRrK...", "...KrRKKKKRrK...",
                          "...KRrK..KrRK...", "...KrrKKKKrrK...", "...KdddddddDK...", "...KrrrrrrrrK...", "...KrrrrrrrrK...",
                          "...KdddddddDK...", "...KrrrrrrrrK...", "...KrrrrrrrrK...", "....KKKKKKKK....", "................", "................"],
                         {"K": K, "r": "#7a3a22", "R": "#9a5a32", "d": "#5a2a18", "D": "#3a1a10"}))
add("lockers#0", grid(["................", "..KKKKKKKKKKKK..", "..KgGGGKgGGGgK..", "..KgKKgKgKKggK..", "..KggggKggggg K..".replace(" ", ""),
                       "..KgKKgKgKKggK..", "..KggggKgggggK..", "..KggigKggiggK..", "..KggggKgggggK..", "..KggggKgggggK..",
                       "..KggggKgggggK..", "..KggggKgggggK..", "..KddddKdddddK..", "..KKKKKKKKKKKK..", "................", "................"],
                      {"K": K, "g": "#4a5a52", "G": "#5e7066", "d": "#3a4640", "i": "#9a9a92"}))
add("desk#0", grid(["................", "................", "................", ".KKKKKKKKKKKKKK.", ".KWWWWWWWWWWWWK.",
                    ".KwppppwwwwgwwK.", ".KwppppwwwwwwwK.", ".KwppppwwwwwwwK.", ".KwwwwwwwwwwwwK.", ".KddddddddddddK.",
                    ".KdK........KdK.", ".KKK........KKK.", "................", "................", "................", "................"],
                   {"K": K, "W": "#8a6a44", "w": "#6a4a2e", "d": "#4a3220", "p": "#d8d0b0", "g": "#3a8a3a"}))

# ------------------------------------------------------------------ hard cover (=)
SAND = {"K": K, "s": "#7a6a4a", "S": "#9a8a62", "d": "#5a4c34"}
add("sandbags#0", grid(["................", "................", "................", "..KKKK.KKKK.KK..", ".KsSSsKsSSsKsSK.",
                        ".KsssdKsssdKssK.", "KKKKKKKKKKKKKKKK", "KsSSsKsSSsKsSSsK", "KsssdKsssdKsssdK", "KddddKddddKddddK",
                        "KKKKKKKKKKKKKKKK", ".KsSSsKsSSsKsSK.", ".KsssdKsssdKssK.", "..KKKK.KKKK.KK..", "................", "................"], SAND))
add("sandbags#1", grid(["................", "................", "................", "................", "..KKKKK.KKKKK...",
                        ".KsSSSsKsSSSsK..", ".KssssdKssssdK..", "KKKKKKKKKKKKKKK.", "KsSSSsKsSSSsKsK.", "KssssdKssssdKsK.",
                        "KddddddddddddddK", "KKKKKKKKKKKKKKKK", "................", "................", "................", "................"], SAND))
add("jersey#0", grid(["................", "................", "................", "................", "..KKKKKKKKKKKK..",
                      ".KCCCCCCCCCCCCK.", ".KcCCCCCCCCCCcK.", ".KccRRccRRcccK..".ljust(16, "."), ".KcRRccRRccRRcK.", ".KddddddddddddK.",
                      ".KKKKKKKKKKKKKK.", "................", "................", "................", "................", "................"],
                     {"K": K, "c": "#8a8a84", "C": "#aaaaa2", "d": "#6a6a64", "R": "#9a4a2a"}))
add("block#0", grid(["................", "................", "..KKKKKKKKKKKK..", ".KcCCCCCCCCCCcK.", ".KcCCCCCCCCCCcK.",
                     ".KcCcccccccCcdK.", ".KcccccccccccdK.", ".KccccccccccddK.", ".KcccccccccdddK.", ".KccccccccccddK.",
                     ".KcccccdccccddK.", ".KddddddddddddK.", ".KKKKKKKKKKKKKK.", "................", "................", "................"],
                    {"K": K, "c": "#7a7a74", "C": "#9a9a92", "d": "#5a5a54"}))
def engine():
    im = blank(); d = ImageDraw.Draw(im)
    d.rectangle([1, 2, 14, 13], fill=hx(K)); d.rectangle([2, 3, 13, 12], fill=hx("#3a3c3e")); d.rectangle([2, 3, 13, 4], fill=hx("#56585a"))
    for x in (4, 7, 10): d.ellipse([x - 1, 6, x + 1, 8], fill=hx("#6a6e72")); d.point([(x, 7)], fill=hx(K))
    d.point([(3, 11), (12, 10)], fill=hx("#7a3a22")); return im
add("engine#0", engine())

# ------------------------------------------------------------------ difficult terrain (~)
def rubble(seed):
    r = random.Random(seed); im = blank(); d = ImageDraw.Draw(im)
    for _ in range(9):
        x, y = r.randrange(0, 14), r.randrange(1, 14); w = r.randrange(1, 4); h = r.randrange(1, 3)
        c = r.choice(["#6a6a64", "#7a6e5e", "#5a5a54", "#86867e", "#4e4a44"])
        d.rectangle([x, y, x + w, y + h], fill=hx(K)); d.rectangle([x, y, x + w - 1, y + h - 1], fill=hx(c))
    for _ in range(10): d.point([(r.randrange(16), r.randrange(16))], fill=hx(r.choice(["#5a5a54", "#3a3632"])))
    return im
for s in range(3): add(f"rubble#{s}", rubble(s + 11))
def brush(seed):
    r = random.Random(seed); im = blank(); d = ImageDraw.Draw(im)
    for _ in range(16):
        x, y = r.randrange(0, 16), r.randrange(2, 16)
        d.line([(x, y), (x + r.choice((-1, 1)), y - r.randrange(2, 4))], fill=hx(r.choice(["#56643a", "#6e7a44", "#44522e"])))
    return im
add("brush#0", brush(1)); add("brush#1", brush(2))
add("puddle#0", Image.new("RGBA", (T, T), (0, 0, 0, 0)))   # placeholder kept empty: water ground draws itself

def trench(seed):
    im = noise("#3a3226", "#2e281e", "#4a4032", seed + 600, dens=.22, ldens=.06); d = ImageDraw.Draw(im); r = random.Random(seed)
    for y in (0, 15): d.line([(0, y), (15, y)], fill=hx("#5a4632"))          # revetment boards
    for x in range(1, 16, 5): d.line([(x, 1), (x + 1, 1)], fill=hx("#6e5840")); d.line([(x + 2, 14), (x + 3, 14)], fill=hx("#6e5840"))
    for _ in range(2): d.point([(r.randrange(2, 14), r.randrange(3, 13))], fill=hx("#5e5240"))
    return im
add("trench#0", trench(0)); add("trench#1", trench(1))
def fountain():
    im = blank(); d = ImageDraw.Draw(im)
    d.ellipse([0, 0, 15, 15], fill=hx(K)); d.ellipse([1, 1, 14, 14], fill=hx("#7a7a74")); d.ellipse([3, 3, 12, 12], fill=hx("#3e4a4c"))
    d.ellipse([6, 6, 9, 9], fill=hx("#9a9a92")); d.point([(4, 9), (10, 5)], fill=hx("#5e7a66")); return im
add("fountain#0", fountain())
def antenna():
    im = blank(); d = ImageDraw.Draw(im)
    d.rectangle([3, 3, 12, 12], fill=hx(K)); d.rectangle([4, 4, 11, 11], fill=hx("#5a5e62"))
    d.line([(4, 4), (11, 11)], fill=hx("#8a8e92")); d.line([(11, 4), (4, 11)], fill=hx("#8a8e92"))
    d.rectangle([6, 6, 9, 9], fill=hx("#3a3e42")); d.point([(7, 7)], fill=hx("#c06bff"))     # faint anomalous hum
    return im
add("antenna#0", antenna())
# ------------------------------------------------------------------ doors
def door(state):
    im = blank(); d = ImageDraw.Draw(im)
    if state == "closed":
        d.rectangle([0, 0, 15, 15], fill=hx("#5a3e26")); d.rectangle([0, 0, 15, 15], outline=hx(K))
        d.line([(8, 1), (8, 14)], fill=hx("#3e2a18")); d.line([(1, 1), (14, 1)], fill=hx("#7a5a3a")); d.point([(11, 8)], fill=hx("#c0a060"))
    elif state == "open":
        d.rectangle([0, 0, 2, 15], fill=hx(K)); d.rectangle([13, 0, 15, 15], fill=hx(K))       # frame
        d.rectangle([3, 0, 5, 12], fill=hx(K)); d.rectangle([3, 1, 4, 11], fill=hx("#5a3e26"))  # door swung in, seen edge-on
    else:   # broken: frame + splinters on the floor
        d.rectangle([0, 0, 2, 15], fill=hx(K)); d.rectangle([13, 0, 15, 15], fill=hx(K))
        for (x, y, w) in [(4, 9, 4), (8, 12, 5), (6, 5, 3)]:
            d.rectangle([x - 1, y - 1, x + w, y + 1], fill=hx(K)); d.line([(x, y), (x + w - 1, y)], fill=hx("#5a3e26"))
    return im
add("door#0", door("closed")); add("door_open#0", door("open")); add("door_broken#0", door("broken"))
def metal_door():
    im = blank(); d = ImageDraw.Draw(im)
    d.rectangle([0, 0, 15, 15], fill=hx("#4a5052")); d.rectangle([0, 0, 15, 15], outline=hx(K))
    d.rectangle([3, 3, 12, 6], outline=hx("#3a3e40")); d.line([(1, 1), (14, 1)], fill=hx("#6a7072")); d.point([(12, 9)], fill=hx("#c0a060"))
    d.point([(2, 12), (13, 3)], fill=hx("#7a3a22")); return im
add("metaldoor#0", metal_door())

# ------------------------------------------------------------------ explosive barrel
add("barrel#0", grid(["................", "................", "....KKKKKKKK....", "...KrRRRRRRrK...", "...KrRKKKKRrK...",
                      "...KRrK..KrRK...", "...KrrKKKKrrK...", "...KyKyKyKyKK...", "...KKyKyKyKyK...", "...KrrrrrrrrK...",
                      "...KrrrYYrrrK...", "...KrrYKKYrrK...", "...KdddddddDK...", "....KKKKKKKK....", "................", "................"],
                     {"K": K, "r": "#9a2a1e", "R": "#c4432e", "d": "#6a1a12", "D": "#3a100a", "y": "#d9b02a", "Y": "#e8e0c8"}))

# ------------------------------------------------------------------ decorative props (Look on walkable tiles, no rules)
def campfire():
    im = blank(); d = ImageDraw.Draw(im)
    for x0, y0, x1, y1 in [(3, 10, 12, 12), (5, 9, 10, 13)]: d.rectangle([x0, y0, x1, y1], fill=hx("#4a3220"))
    for i, c in enumerate(["#b8360c", "#f2741c", "#ffc845", "#fff4c0"]): d.ellipse([3 + i, 3 + i * 2, 12 - i, 12], fill=hx(c))
    return im
add("campfire#0", campfire())
add("stones#0", grid(["................", "................", "................", "................", ".....KKKK.......", "....KsSSsK......",
                      "...KsSSssK......", "...KssssdK..KK..", "....KdddK..KsSK.", ".....KKK...KsdK.", "............KK..", "................",
                      "................", "................", "................", "................"], {"K": K, "s": "#6e6a62", "S": "#8e8a80", "d": "#4e4a44"}))
add("armchair#0", grid(["................", "................", "...KKKKKKKKKK...", "..KaAAAAAAAAaK..", "..KaAAAAAAAAaK..", "..KaaaaaaaaaaK..",
                        "..KKKKKKKKKKKK..", "..KaKsSSSSSKaK..", "..KaKssssssKaK..", "..KaKsssssdKaK..", "..KaKddddddKaK..", "..KaaaaaaaaaaK..",
                        "...KKKKKKKKKK...", "................", "................", "................"],
                       {"K": K, "a": "#5a2e2e", "A": "#7a4040", "s": "#6a3838", "S": "#8a5050", "d": "#4a2424"}))
def barrier_pole():
    im = blank(); d = ImageDraw.Draw(im); d.rectangle([0, 6, 15, 9], fill=hx(K))
    for xx in range(0, T, 4): d.rectangle([xx, 7, xx + 1, 8], fill=hx("#b02a2a")); d.rectangle([xx + 2, 7, xx + 3, 8], fill=hx("#d8d8d0"))
    return im
add("barrierpole#0", barrier_pole())
def tower_platform(seed):
    im = Image.new("RGBA", (T, T), hx("#5a4a3a")); d = ImageDraw.Draw(im)
    for i in range(3, 16, 4): d.line([(0, i), (15, i)], fill=hx("#4a3a2c"))
    r = random.Random(seed); d.point([(r.randrange(16), r.randrange(16))], fill=hx("#3a2e22"))
    return im
add("platform#0", tower_platform(0)); add("platform#1", tower_platform(1))
def culvert():
    im = blank(); d = ImageDraw.Draw(im)
    d.rectangle([0, 3, 15, 15], fill=hx("#8a8a82")); d.rectangle([2, 6, 13, 15], fill=hx("#0e0f10"))
    d.rectangle([2, 6, 13, 6], fill=hx("#2a2c2e")); d.rectangle([0, 3, 15, 3], fill=hx(K)); return im
add("culvert#0", culvert())
def hatch():
    im = blank(); d = ImageDraw.Draw(im)
    d.ellipse([2, 2, 13, 13], fill=hx(K)); d.ellipse([3, 3, 12, 12], fill=hx("#4a5052")); d.line([(4, 8), (11, 8)], fill=hx("#2e3234"))
    d.line([(8, 4), (8, 11)], fill=hx("#2e3234")); d.point([(5, 5)], fill=hx("#6a7072")); return im
add("hatch#0", hatch())
def bones():
    im = blank(); d = ImageDraw.Draw(im)
    for a, b in [((3, 10), (9, 7)), ((7, 12), (12, 11))]:
        d.line([a, b], fill=hx(K), width=3); d.line([a, b], fill=hx("#d8d0b8"))
    d.ellipse([10, 4, 13, 7], fill=hx(K)); d.ellipse([11, 5, 12, 6], fill=hx("#d8d0b8")); return im
add("bones#0", bones())
def railtrack(vertical=False):
    im = blank(); d = ImageDraw.Draw(im)
    for x in range(1, 16, 4): d.rectangle([x, 1, x + 1, 14], fill=hx("#4a3a2a"))   # sleepers
    for y in (4, 11): d.line([(0, y), (15, y)], fill=hx(K)); d.line([(0, y - 1), (15, y - 1)], fill=hx("#7a7e82"))
    return im.rotate(90) if vertical else im
add("rail#0", railtrack()); add("rail_v#0", railtrack(True))
def road_line():
    im = blank(); d = ImageDraw.Draw(im); d.rectangle([7, 2, 8, 7], fill=hx("#8a8460", 200)); return im
add("roadline#0", road_line())

# ------------------------------------------------------------------ overlays
def cracks(n):
    r = random.Random(n); im = blank(); d = ImageDraw.Draw(im)
    for _ in range(n):
        x, y = r.randrange(3, 13), r.randrange(3, 13)
        for _ in range(4):
            nx, ny = x + r.choice((-2, -1, 1, 2)), y + r.choice((-1, 1))
            d.line([(x, y), (nx, ny)], fill=hx(K, 210)); x, y = max(0, min(15, nx)), max(0, min(15, ny))
    return im
add("fx_crack#0", cracks(1)); add("fx_crack#1", cracks(3))

# ------------------------------------------------------------------ write
COLS = 16
rows = (len(CELLS) + COLS - 1) // COLS
atlas = Image.new("RGBA", (COLS * T, rows * T), (0, 0, 0, 0))
for i, (name, im) in enumerate(CELLS):
    assert im.size == (T, T), name
    atlas.paste(im, ((i % COLS) * T, (i // COLS) * T))
names = [n for n, _ in CELLS]
assert len(set(names)) == len(names), "duplicate cell names"
out = os.path.join(HERE, "unity_out"); os.makedirs(out, exist_ok=True)
atlas.save(os.path.join(out, "terrain_atlas.png"))
shutil.copy(os.path.join(out, "terrain_atlas.png"), os.path.join(out, "terrain_atlas.bytes"))
open(os.path.join(out, "terrain_names.txt"), "w").write("\n".join(names) + "\n")

# preview sheet
S = 4; cw, ch = T * S + 8, T * S + 18
sheet = Image.new("RGB", (COLS * cw, rows * ch), hx("#1b1d1f")[:3]); sd = ImageDraw.Draw(sheet)
for i, (name, im) in enumerate(CELLS):
    cx, cy = (i % COLS) * cw, (i // COLS) * ch
    for yy in range(0, T * S, 8):
        for xx in range(0, T * S, 8):
            sd.rectangle([cx + 4 + xx, cy + 4 + yy, cx + 11 + xx, cy + 11 + yy], fill=hx("#222527" if (xx + yy) // 8 % 2 else "#26292c")[:3])
    sheet.paste(im.resize((T * S, T * S), Image.NEAREST), (cx + 4, cy + 4), im.resize((T * S, T * S), Image.NEAREST))
    sd.text((cx + 3, cy + T * S + 5), name[:12], fill=hx("#b8bcc0")[:3])
sheet.save(os.path.join(HERE, "terrain_sheet.png"))
print(len(CELLS), "cells")
