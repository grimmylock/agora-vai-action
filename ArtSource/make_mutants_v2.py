# Batch 130 (Yan: "we'll need to rework the mutants to be in line with the rest of the game's quality now, they are still using the
# initial 'placeholder' designs"; sizes: "yes to both, make the blight brute even bigger, like a tank from left4dead").
# Every mutant redrawn in the locked style: shapes blocked in per material, then the same polish as the armor sprites (lit top/left,
# shaded bottom/right, 1px #141617 outline), signature features in 1-3 bright pixels. Four-legged ones are side views facing right
# (the game mirrors them); upright ones are front views. Sizes: Blight Brute 32x32, Dread Prowler 24x24, the rest 16x16.
#   python3 make_mutants_v2.py <sheet.png>      -> review sheet (old | new, a human for scale, true size)
import math, os, sys
from PIL import Image, ImageDraw, ImageFont

K = "#141617"
def hexc(h, a=255):
    h = h.lstrip("#"); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)
def scale(c, f): return tuple(max(0, min(255, int(v * f))) for v in c[:3]) + (c[3],)

class Canvas:
    def __init__(self, w, h): self.w, self.h = w, h; self.g = [["."] * w for _ in range(h)]
    def px(self, x, y, ch):
        x, y = int(round(x)), int(round(y))
        if 0 <= x < self.w and 0 <= y < self.h: self.g[y][x] = ch
    def ell(self, cx, cy, rx, ry, ch):
        for y in range(self.h):
            for x in range(self.w):
                if ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1.0: self.g[y][x] = ch
    def rect(self, x0, y0, x1, y1, ch):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1): self.px(x, y, ch)
    def line(self, pts, ch, w=1):
        for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
            n = max(abs(x1 - x0), abs(y1 - y0), 1)
            for i in range(int(n) + 1):
                x, y = x0 + (x1 - x0) * i / n, y0 + (y1 - y0) * i / n
                for dx in range(w):
                    for dy in range(w): self.px(x + dx - (w - 1) / 2, y + dy - (w - 1) / 2, ch)
    def poly(self, pts, ch):
        for y in range(self.h):
            for x in range(self.w):
                inside, j = False, len(pts) - 1
                for i in range(len(pts)):
                    (xi, yi), (xj, yj) = pts[i], pts[j]
                    if (yi > y + .5) != (yj > y + .5) and x + .5 < (xj - xi) * (y + .5 - yi) / (yj - yi) + xi: inside = not inside
                    j = i
                if inside: self.g[y][x] = ch

def render(cv, pal, flat="", alpha=None, outline_a=255):
    """pal: char -> hex. Chars not in `flat` get the armor polish: top/left edge of their region lit (x1.25), bottom/right shaded
    (x0.72). Then a 1px outline round the whole silhouette."""
    w, h, g = cv.w, cv.h, cv.g
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0)); p = img.load()
    def same(x, y, ch): return 0 <= x < w and 0 <= y < h and g[y][x] == ch
    for y in range(h):
        for x in range(w):
            ch = g[y][x]
            if ch == ".": continue
            c = hexc(pal[ch], (alpha or {}).get(ch, 255))
            if ch not in flat:
                if not same(x, y - 1, ch): c = scale(c, 1.25)
                elif not same(x, y + 1, ch): c = scale(c, 0.72)
                elif not same(x - 1, y, ch): c = scale(c, 1.12)
                elif not same(x + 1, y, ch): c = scale(c, 0.82)
            p[x, y] = c
    out = img.copy(); o = out.load()
    for y in range(h):
        for x in range(w):
            if p[x, y][3]: continue
            if any(0 <= x + dx < w and 0 <= y + dy < h and p[x + dx, y + dy][3] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                o[x, y] = hexc(K, outline_a)
    return out

def cloak(cv, pal, keep, light):
    """The approved cloak look: body #8fa3a8 at alpha 110, highlights #c8dade at 140, outline at 150, eyes kept."""
    cp, a = {}, {}
    for ch in pal:
        if ch in keep: cp[ch], a[ch] = pal[ch], 255
        elif ch in light: cp[ch], a[ch] = "#c8dade", 140
        else: cp[ch], a[ch] = "#8fa3a8", 110
    return render(cv, cp, flat=pal.keys(), alpha=a, outline_a=150)

# ------------------------------------------------------------------ T1 Blind Maw: an eyeless hound that's mostly mouth (pack biter)
def blind_maw():
    c = Canvas(16, 16)
    c.ell(6.5, 8.5, 4.6, 2.6, "B")                      # lean body
    c.ell(11.5, 7.5, 3.0, 2.6, "B")                     # big head
    c.line([(2, 8), (0.6, 6.4)], "B")                   # rat tail
    for x, top in ((3, 10), (5, 10), (8, 10), (10, 9)): c.rect(x, top, x, 13, "D")   # legs
    c.rect(9, 9, 15, 10, "R")                           # the open maw
    for x in (9, 11, 13, 15): c.px(x, 9, "T")           # upper teeth
    for x in (10, 12, 14): c.px(x, 10, "T")             # lower teeth
    c.px(14, 8, "B"); c.px(15, 8, "B")                  # snout over the jaw
    for x in (5, 7): c.px(x, 9, "r")                    # ribs showing
    c.px(10, 5, "S"); c.px(12, 5, "S")                  # scar stitches where eyes were
    return c, {"B": "#7a4a46", "D": "#4e2c2b", "R": "#b02a2a", "T": "#e8e0c8", "r": "#5e3634", "S": "#3a2220"}, "RTrSD"

# ------------------------------------------------------------------ T1 Pallid Prowler: pale hairless crawler, toxic drool
def pallid_prowler():
    c = Canvas(16, 16)
    c.ell(7, 9.5, 5.2, 2.4, "B")                        # low, long body
    c.ell(4, 8.6, 2.4, 2.0, "B")                        # hunched haunch
    c.ell(12.6, 9.6, 2.3, 1.9, "B")                     # small head held low
    c.line([(2, 11), (1, 13), (1, 14)], "D"); c.line([(5, 11), (5, 14)], "D")
    c.line([(9, 11), (10, 14)], "D"); c.line([(11, 11), (12, 13), (12, 14)], "D")   # long thin legs
    c.px(14, 9, "E")                                    # beady eye
    c.px(14, 11, "A"); c.px(14, 12, "A"); c.px(13, 12, "a")   # toxic drool
    for x, y in ((5, 8), (8, 8), (7, 10)): c.px(x, y, "P")    # boils
    return c, {"B": "#b8ad9a", "D": "#857a68", "E": "#d8402a", "A": "#8fd03a", "a": "#5e9a26", "P": "#c9a46a"}, "EAaPD"

# ------------------------------------------------------------------ T1 Rot Swarm: a seething pile of vermin
def rot_swarm():
    c = Canvas(16, 16)
    rats = [(4.0, 12.4, 2.8, 1.5, "B"), (11.6, 12.6, 2.8, 1.5, "C"), (8.0, 8.6, 2.6, 1.4, "C"), (13.2, 8.2, 1.8, 1.2, "B")]   # apart, so each reads
    for x, y, rx, ry, ch in rats:
        c.ell(x, y, rx, ry, ch)
        c.px(x + rx + 0.4, y + 0.2, ch)                 # pointed snout
        c.px(x + rx - 0.4, y - 0.4, "E")                # red eye
        c.px(x + rx - 1.4, y - ry, "e")                 # ear
        c.line([(x - rx, y + 0.4), (x - rx - 1.6, y + 1.2)], "t")   # tail
    return c, {"B": "#5a4e42", "C": "#7a6a54", "E": "#ff3b3b", "e": "#b07a70", "t": "#b07a70"}, "Eet"

# ------------------------------------------------------------------ T2 Veil Prowler: a lean long-limbed stalker that bends light
def veil_body():
    c = Canvas(16, 16)
    c.ell(7, 8.4, 4.8, 2.0, "B")                        # lean torso
    c.ell(12.4, 7.0, 2.2, 1.7, "B")                     # narrow head, held high
    c.line([(2.5, 8), (1, 6), (1, 4)], "B")             # tail curled up
    c.line([(4, 10), (3, 13), (4, 14)], "D"); c.line([(6, 10), (6, 14)], "D")
    c.line([(9, 10), (10, 13), (10, 14)], "D"); c.line([(11, 9), (13, 12), (13, 14)], "D")   # long jointed legs
    c.px(13, 6, "E")                                    # single red eye
    c.px(14, 8, "T"); c.px(15, 7, "B")
    for x in (5, 7, 9): c.px(x, 7, "s")                 # back ridge
    return c

VEIL_PAL = {"B": "#6a6058", "D": "#4a423c", "E": "#ff3b3b", "T": "#e8e0c8", "s": "#8a7e72"}

# ------------------------------------------------------------------ T3 Alpha Prowler: the pack leader - bone plates, spines, armor-piercing
def alpha_body():
    c = Canvas(16, 16)
    c.ell(6.8, 8.6, 5.0, 2.5, "B")
    c.ell(12.3, 7.4, 2.5, 2.1, "B")
    c.line([(2, 9), (0.6, 7.6)], "B")
    c.line([(3.6, 10.5), (3, 13), (3.6, 14)], "D"); c.line([(6, 11), (6, 14)], "D")
    c.line([(9, 11), (9.6, 14)], "D"); c.line([(11, 10), (12.6, 12.6), (12.6, 14)], "D")
    for x, h in ((4, 2), (6, 3), (8, 3), (10, 2)): c.line([(x, 6.2), (x - 0.6, 6.2 - h)], "G")   # back spines
    c.rect(11, 6, 13, 6, "G")                           # bone brow plate
    c.px(12, 7, "E"); c.px(14, 7, "E")                  # two red eyes
    c.rect(13, 9, 15, 9, "T")                           # fangs
    return c

ALPHA_PAL = {"B": "#4e4a52", "D": "#36323a", "G": "#c8bca0", "E": "#ff3b3b", "T": "#e8e0c8"}

# ------------------------------------------------------------------ T3 Mind Weaver: gaunt upright psychic - a tall swollen skull, long
# arms hanging past its knees, jaw tendrils, eyes glowing violet
def mind_weaver():
    c = Canvas(16, 16)
    c.ell(7.5, 3.6, 3.0, 3.2, "H")                      # tall swollen cranium
    c.rect(6, 6, 9, 7, "B")                             # narrow jaw
    for x in (6, 7.5, 9): c.line([(x, 8), (x + (x - 7.5) * 0.5, 10)], "t")   # jaw tendrils
    c.poly([(5, 8), (10, 8), (10.6, 14), (4.4, 14)], "R")   # thin wrapped body
    c.line([(4.6, 8.4), (3.4, 11), (3, 14)], "B"); c.line([(10.4, 8.4), (11.6, 11), (12, 14)], "B")   # long arms
    c.px(2.6, 15, "B"); c.px(12.4, 15, "B")             # long fingers
    c.rect(6, 14, 6, 15, "D"); c.rect(9, 14, 9, 15, "D")   # thin legs
    c.px(6, 4, "E"); c.px(9, 4, "E"); c.px(6, 5, "e"); c.px(9, 5, "e")   # glowing eyes
    for x, y in ((6, 1), (9, 2)): c.px(x, y, "v")       # veins on the skull
    return c, {"H": "#9a86a8", "B": "#6b5a7a", "t": "#6b5a7a", "R": "#3e3448", "D": "#2e2638", "E": "#f0e0ff", "e": "#c06bff", "v": "#c06bff"}, "EetvD"

WEAVER_SPECKS = [(1, 3), (14, 2), (0, 9), (15, 10), (1, 14), (14, 13)]

# ------------------------------------------------------------------ T4 Blight Brute (32x32): a lopsided hulk (not a copy of any game's
# tank, Yan: "make sure its different enough") - its left arm has grown into a crusted club of blight and rock it drags on the ground,
# the right is long and clawed, fungal stalks sprout from its hump and its low head splits into a two-part jaw (berserk, ground slam,
# boulder throw)
def blight_brute():
    c = Canvas(32, 32)
    c.ell(16.5, 13, 9.5, 7.0, "B")                      # the hump, leaning forward
    c.ell(17, 20, 6.5, 5.0, "B")                        # gut
    c.poly([(5, 9), (11, 8), (12, 18), (10, 24), (2, 25), (1, 16)], "C")   # the crusted club arm
    c.ell(5.5, 26.5, 5.0, 3.6, "C")                     # its rock-heavy end, on the ground
    for (x, y) in ((4, 12), (8, 15), (3, 20), (7, 22), (5, 27), (9, 26)): c.px(x, y, "c")   # glowing blight crystals in the crust
    for (x, y) in ((6, 11), (2, 17), (8, 19), (3, 25), (7, 28)): c.px(x, y, "k")   # cracks
    c.line([(25, 12), (28, 17), (28.6, 22), (27.6, 25)], "A", 3)   # long right arm
    for dx in (-1.4, 0, 1.4): c.line([(27.6, 25), (27.6 + dx, 28)], "T")   # three claws
    c.rect(12, 24, 15, 29, "D"); c.rect(19, 24, 22, 29, "D")   # bowed short legs
    c.rect(11, 29, 15, 30, "f"); c.rect(18, 29, 23, 30, "f")
    for (x, h) in ((12, 5), (15, 7), (18, 6), (21, 4), (23, 3)):   # fungal stalks on the hump, capped
        c.line([(x, 8), (x - 0.5, 8 - h)], "S"); c.px(x - 0.5, 7 - h, "s"); c.px(x + 0.5, 7 - h, "s")
    c.ell(18.5, 13.6, 3.2, 2.6, "H")                    # low head, pushed forward under the hump
    c.px(17, 12, "E")                                   # one yellow eye; the other is crusted over
    c.px(19.6, 12, "c")
    c.rect(17, 15, 20, 16, "R"); c.line([(18.5, 15), (18.5, 17)], "H")   # the split jaw
    for x in (17, 20): c.px(x, 17, "T")
    for (x, y, r) in ((22, 18, 1.4), (13, 19, 1.2)): c.ell(x, y, r, r, "P")   # tumours
    return c, {"B": "#6e7458", "A": "#6a7054", "C": "#6e665a", "c": "#c8d64a", "k": "#3a342c", "D": "#4e523d", "f": "#3a3d2e",
               "H": "#7a7f62", "E": "#e8c040", "R": "#5a1a1a", "T": "#d8d0b0", "S": "#9a9a6a", "s": "#c8b878", "P": "#a8a050"}, "EcRTksS"

# ------------------------------------------------------------------ T4 Dread Prowler (24x24): a big mangy leaper, ribs and spine
# showing, burning eyes (berserk, leap)
def dread_prowler():
    c = Canvas(24, 24)
    c.ell(10.5, 12.5, 7.0, 3.6, "B")                    # deep chest, tucked waist
    c.ell(5.0, 11.8, 3.6, 3.2, "B")                     # haunches
    c.ell(18.6, 10.5, 3.6, 3.0, "B")                    # heavy head
    c.line([(2, 11), (0.8, 8.6), (1.6, 6.6)], "B", 2)   # tail
    c.line([(3.6, 15), (2.6, 18), (3.6, 21)], "D", 2); c.line([(7, 15.5), (7, 21)], "D", 2)    # hind legs
    c.line([(13, 15.5), (13.6, 21)], "D", 2); c.line([(16, 14.5), (17.6, 18), (17.6, 21)], "D", 2)   # forelegs
    for x in (8, 10, 12): c.line([(x, 11), (x, 14)], "G")        # ribs
    for x in range(4, 16, 2): c.px(x, 8.6, "G")                  # spine knuckles
    c.px(19, 9, "E"); c.px(21, 9, "E"); c.px(20, 9, "e")         # burning eyes
    c.rect(19, 12, 23, 12, "R"); c.px(19, 12, "T"); c.px(21, 12, "T"); c.px(23, 11, "T")   # jaw and fangs
    return c, {"B": "#5a4636", "D": "#3e3026", "G": "#d8ccb0", "E": "#f0c030", "e": "#fff0a0", "R": "#7a1e1e", "T": "#e8e0c8"}, "GEeRTD"

# ------------------------------------------------------------------ T5 Void Weaver: a floating hollow shape, tattered, cold glow
def void_body():
    c = Canvas(16, 16)
    c.ell(7.5, 4.6, 3.4, 3.4, "H")                      # hooded head
    c.poly([(3, 6), (12, 6), (13.6, 14), (1.4, 14)], "B")    # tall tapering shroud
    for x in (2, 4, 7, 10, 12): c.line([(x, 14), (x, 15)], ".")   # tattered hem, floating
    c.rect(6, 4, 9, 6, "V")                             # the void under the hood
    c.px(6, 5, "W"); c.px(9, 5, "W")                    # two cold eyes
    c.line([(7.5, 8), (7.5, 12)], "r")                  # a seam of light down the front
    return c

VOID_PAL = {"H": "#34345a", "B": "#2a2a44", "V": "#0c0c18", "W": "#e0ecff", "r": "#6a8aff"}
VOID_SPECKS = [(1, 2), (14, 3), (0, 10), (15, 11), (3, 15), (12, 15)]

def specks(img, pts, col):
    img = img.copy()
    for x, y in pts:
        if not img.getpixel((x, y))[3]: img.putpixel((x, y), hexc(col, 170))
    return img

def sprites():
    """(game name, key, image) in the old mutant_atlas order (SpriteAtlasIndex.Mutants)."""
    out = []
    def add(name, key, built):
        cv, pal, flat = built; out.append((name, key, render(cv, pal, flat)))
    add("Blind Maw", "blind_maw", blind_maw())
    add("Pallid Prowler", "pallid_prowler", pallid_prowler())
    add("Rot Swarm", "rot_swarm", rot_swarm())
    v = veil_body(); out.append(("Veil Prowler", "veil_prowler", render(v, VEIL_PAL, "EDTs")))
    out.append(("Veil Prowler (cloaked)", "veil_prowler_cloaked", cloak(v, VEIL_PAL, "E", "sT")))
    a = alpha_body(); out.append(("Alpha Prowler", "alpha_prowler", render(a, ALPHA_PAL, "GETD")))
    out.append(("Alpha Prowler (cloaked)", "alpha_prowler_cloaked", cloak(a, ALPHA_PAL, "E", "GT")))
    cv, pal, flat = mind_weaver(); out.append(("Mind Weaver", "mind_weaver", specks(render(cv, pal, flat), WEAVER_SPECKS, "#c06bff")))
    add("Blight Brute", "blight_brute", blight_brute())
    add("Dread Prowler", "dread_prowler", dread_prowler())
    vb = void_body(); out.append(("Void Weaver", "void_weaver", specks(render(vb, VOID_PAL, "VWr"), VOID_SPECKS, "#6a8aff")))
    out.append(("Void Weaver (cloaked)", "void_weaver_cloaked", cloak(vb, VOID_PAL, "W", "r")))
    return out

def sheet(path):
    here = os.path.dirname(os.path.abspath(__file__))
    old = Image.open(os.path.join(here, "..", "Assets", "Resources", "Sprites", "mutant_atlas.png")).convert("RGBA")
    human = Image.open(os.path.join(here, "..", "Assets", "Resources", "Sprites", "armor_atlas.png")).convert("RGBA").crop((0, 0, 16, 16))
    new = sprites()
    Z, CW = 6, 34 * 6
    W = 40 + 6 * (CW + 30); H = 90 + 2 * (CW + 2 * 16 * Z // 2 + 60) + 160
    img = Image.new("RGBA", (W, H), hexc("#1b1d1f")); d = ImageDraw.Draw(img)
    f = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 14)
    tf = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 22)
    d.text((40, 24), "Mutants - old (small, top-left) vs new (6x), a human for scale; bottom: true size and 3x on the ground", font=tf, fill=(230, 230, 230))
    for i, (name, key, im) in enumerate(new):
        col, row = i % 6, i // 6
        x0 = 40 + col * (CW + 30); y0 = 80 + row * (CW + 120)
        d.rectangle([x0, y0, x0 + CW - 1, y0 + CW - 1], fill=hexc("#2a2620"))
        o = old.crop((i * 16, 0, i * 16 + 16, 16)).resize((48, 48), Image.NEAREST)
        img.alpha_composite(o, (x0 + 4, y0 + 4))
        big = im.resize((im.width * Z, im.height * Z), Image.NEAREST)
        img.alpha_composite(big, (x0 + CW - big.width - 4, y0 + CW - big.height - 4))
        hz = human.resize((16 * Z, 16 * Z), Image.NEAREST)
        d.text((x0, y0 + CW + 6), name, font=f, fill=(184, 188, 192))
    y = H - 150; d.rectangle([40, y - 10, W - 40, H - 20], fill=hexc("#2a2620"))
    x = 60
    for name, key, im in [("human", "", human)] + new:
        img.alpha_composite(im.resize((im.width * 3, im.height * 3), Image.NEAREST), (x, y + 96 - im.height * 3))
        img.alpha_composite(im, (x + im.width, y + 120 - im.height))
        x += im.width * 3 + 14
    img.convert("RGB").save(path)

if __name__ == "__main__":
    sheet(sys.argv[1] if len(sys.argv) > 1 else "mutants_v2_sheet.png")
