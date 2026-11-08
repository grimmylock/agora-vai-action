# Boss 1 (Yan, 2026-10-29: "draw him a black suit with a red visor and a gas mask"): one-of-a-kind suits that aren't in the
# faction atlases - a boss's look. Built from the same parts and rules as make_armor_avatars.py (16x16, armless, the heavy
# body one leg-row taller, outlined, lit left / shaded right), with the walk frames from make_walk_atlas.py.
# Writes Assets/Resources/Sprites/armor_unique_<key>.png: a 96x16 strip - front, walk a, walk b, back, back walk a, back walk b
# (UnitSprites.Unique reads it by the suit's name), and a preview sheet into ArtSource/_out/.
#   python3 ArtSource/make_unique_armor.py
import os, sys
from PIL import Image, ImageDraw, ImageFont
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import make_armor_avatars as av
import make_walk_atlas as wa

SPR = os.path.join(HERE, "..", "Assets", "Resources", "Sprites")
OUT = os.path.join(HERE, "_out"); os.makedirs(OUT, exist_ok=True)

# the Visor's head: a black helmet, a red visor band across the eyes, a gas mask under it with a filter at the chin
av.HEADS["visormask"] =      ["......KK", "....KKhh", "...KHhhh", "...KHRRR", "....KMMM", "....KMmQ", ".....KMQ"]
av.HEADS_BACK["visormask"] = ["......KK", "....KKhh", "...KHhhh", "...KHHHH", "....KHHH", "....KMMM", ".....KMm"]

UNIQUE = {
    # name: (look, physDR, anomDR, belt slots) - the stats live with the boss (Boss.cs); this is the look
    "Black Visor Suit": dict(head="visormask", body="heavy", H="#1b1d20", S="#1f2226", P="#2d3136", D="#17181a", belt="#26231f", arm="#8a1414"),
}
RED, GLINT = "#b81c1c", "#ff6a52"

def front(name, L):
    av.LOOK[name] = L
    img = av.build(name, 0.2, 2, L)
    px = img.load()
    # the visor band: red, a hot glint on its lit left end (the build pass painted R as the faction patch colour)
    for x in range(16):
        for y in range(16):
            if px[x, y][:3] == av.hexc(L["arm"])[:3] and y <= 4: px[x, y] = av.hexc(RED)
    for y in range(16):
        row = [x for x in range(16) if px[x, y][:3] == av.hexc(RED)[:3]]
        if row: px[row[0], y] = av.hexc(GLINT); break
    return img

def strip(name, L):
    f = front(name, L)
    b = av.build(name, 0.2, 2, L, back=True)
    cells = [f, f.copy(), f.copy(), b, b.copy(), b.copy()]
    for base, a, bb in ((0, 1, 2), (3, 4, 5)):
        top = wa.leg_top(cells[base].load(), 0, 0)
        if top is not None:
            wa.lift(cells[a], 0, 0, top, left=True); wa.lift(cells[bb], 0, 0, top, left=False)
    s = Image.new("RGBA", (96, 16), (0, 0, 0, 0))
    for i, c in enumerate(cells): s.alpha_composite(c, (i * 16, 0))
    return s

def key(n): return av.key(n)

if __name__ == "__main__":
    font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 13)
    for name, L in UNIQUE.items():
        s = strip(name, L)
        s.save(os.path.join(SPR, f"armor_unique_{key(name)}.png"))
        # preview: the strip at 8x, then front at true 1080p size (x4.8 ~ x5) and 3x beside a Wanderer for scale
        ref = Image.open(os.path.join(SPR, "armor_atlas.png")).convert("RGBA").crop((0, 0, 16, 16))
        sheet = Image.new("RGBA", (96 * 8 + 40, 16 * 8 + 160), av.hexc("#1b1d1f"))
        sheet.alpha_composite(s.resize((96 * 8, 128), Image.NEAREST), (20, 30))
        d = ImageDraw.Draw(sheet)
        d.text((20, 8), f"{name}: front, walk a, walk b, back, back walk a, back walk b", font=font, fill=av.hexc("#b8bcc0"))
        for i, im in enumerate((ref, s.crop((0, 0, 16, 16)))):
            sheet.alpha_composite(im.resize((80, 80), Image.NEAREST), (20 + i * 90, 170))
            sheet.alpha_composite(im.resize((48, 48), Image.NEAREST), (220 + i * 60, 186))
            sheet.alpha_composite(im, (360 + i * 24, 210))
        d.text((20, 254), "x5 (1080p)     x3          x1  - beside a Wanderer", font=font, fill=av.hexc("#b8bcc0"))
        sheet.save(os.path.join(OUT, f"unique_{key(name)}_preview.png"))
        print("wrote", f"armor_unique_{key(name)}.png")
