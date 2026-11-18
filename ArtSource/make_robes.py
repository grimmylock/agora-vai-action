"""'Those' (Yan, 2026-11-18): "they'll probably need a more sophisticated sprite, but not really bigger than the player ... they devolved into mutants and re-evolved into an
advanced thinking form like humans". The robed figures of STORY.md (the Grey Robes: Tier 1's watchers with the hand-held device). First version, drawn straight at the doubled
density (32 px cells, nothing resampled): a tall thin figure in a long grey hooded robe, the face a dark void with two pale eyes, long pale hands holding a small device.
Writes Assets/Resources/Sprites/armor_unique_grey_robe_x2.png: a 192x32 strip - front, walk a, walk b, back, back walk a, back walk b (UnitSprites.Unique reads it by the suit
name "Grey Robe"), and a preview into ArtSource/_out/grey_robe_preview.png.   python3 ArtSource/make_robes.py"""
import os
from PIL import Image, ImageDraw
HERE = os.path.dirname(os.path.abspath(__file__)); SPR = os.path.join(HERE, "..", "Assets", "Resources", "Sprites"); OUT = os.path.join(HERE, "_out"); os.makedirs(OUT, exist_ok=True)
OL = (18, 20, 24, 255); LIT = (150, 157, 166, 255); MID = (112, 119, 128, 255); SHD = (80, 86, 94, 255); DEEP = (56, 61, 68, 255); VOID = (14, 16, 18, 255)
EYE = (196, 232, 226, 255); HAND = (170, 174, 172, 255); HANDS = (128, 132, 132, 255); DEV = (40, 44, 48, 255); GLOW = (120, 220, 200, 255)
def poly(d, pts, c): d.polygon(pts, fill=c)
def figure(back=False, sway=0, bob=0):
    im = Image.new("RGBA", (32, 32), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    b = bob
    # robe: shoulders to the ground, a flared skirt; the left half lit, the right in shade
    body = [(11, 14 + b), (21, 14 + b), (22, 22 + b), (24 + sway, 29), (8 + sway, 29), (10, 22 + b)]
    poly(d, body, MID)
    poly(d, [(11, 14 + b), (16, 14 + b), (16, 29), (8 + sway, 29), (10, 22 + b)], LIT)
    poly(d, [(16, 14 + b), (21, 14 + b), (22, 22 + b), (24 + sway, 29), (16, 29)], SHD)
    for x, c in ((13, SHD), (19, DEEP), (22, DEEP)): d.line([(x, 20 + b), (x + (1 if x > 16 else -1) * (1 + sway // 2 if sway else 1), 28)], fill=c)   # folds
    d.line([(9 + sway, 28), (23 + sway, 28)], fill=DEEP)                                            # the hem
    # hood: a tall soft point, the face void inside it
    hood = [(16, 1 + b), (20, 5 + b), (22, 10 + b), (21, 15 + b), (11, 15 + b), (10, 10 + b), (12, 5 + b)]
    poly(d, hood, MID); poly(d, [(16, 1 + b), (12, 5 + b), (10, 10 + b), (11, 15 + b), (16, 15 + b)], LIT)
    poly(d, [(16, 2 + b), (20, 6 + b), (22, 10 + b), (21, 15 + b), (16, 15 + b)], SHD)
    if not back:
        d.ellipse([(13, 6 + b), (19, 15 + b)], fill=VOID)                                            # the face is shadow
        for x in (14, 17): d.rectangle([(x, 10 + b), (x + 1, 10 + b)], fill=EYE); d.point((x + (0 if x < 16 else 1), 9 + b), fill=(110, 150, 146, 255))                    # two pale eyes
        d.line([(15, 14 + b), (17, 14 + b)], fill=(60, 66, 70, 255))                                 # a thin, wrong mouth line
        # long pale hands holding a small device at the belly
        poly(d, [(12, 18 + b), (14, 18 + b), (15, 23 + b), (13, 23 + b)], SHD); poly(d, [(18, 18 + b), (20, 18 + b), (19, 23 + b), (17, 23 + b)], SHD)   # sleeves
        for yy in range(23, 27): d.point((10, yy + b), fill=HAND if yy < 26 else HANDS); d.point((22, yy + b), fill=HANDS if yy < 26 else HAND)   # long pale hands hanging at the sides
        d.rectangle([(14, 22 + b), (18, 24 + b)], fill=DEV); d.point((16, 23 + b), fill=GLOW)           # the device and its light
        for x, y in ((14, 24), (15, 24), (17, 24), (18, 24)): d.point((x, y + b), fill=HAND)
    else:
        d.line([(16, 16 + b), (16, 28)], fill=DEEP)                                                  # the back seam
        d.line([(13, 8 + b), (19, 8 + b)], fill=SHD)
    # outline: one pixel round the whole silhouette
    a = im.split()[3]; px = im.load(); w, h = im.size; out = im.copy(); o = out.load()
    for y in range(h):
        for x in range(w):
            if a.getpixel((x, y)) == 0 and any(0 <= x + dx < w and 0 <= y + dy < h and a.getpixel((x + dx, y + dy)) > 0 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))): o[x, y] = OL
    return out
def strip():
    cells = [figure(False), figure(False, sway=1, bob=-1), figure(False, sway=-1, bob=-1), figure(True), figure(True, sway=1, bob=-1), figure(True, sway=-1, bob=-1)]
    s = Image.new("RGBA", (192, 32), (0, 0, 0, 0))
    for i, c in enumerate(cells): s.alpha_composite(c, (i * 32, 0))
    return s
if __name__ == "__main__":
    s = strip(); s.save(os.path.join(SPR, "armor_unique_grey_robe_x2.png"))
    ref = Image.open(os.path.join(SPR, "armor_atlas_x2.png")).convert("RGBA").crop((0, 0, 32, 32))
    pv = Image.new("RGBA", (192 * 5 + 40, 32 * 5), (70, 70, 70, 255)); pv.alpha_composite(s.resize((192 * 5, 160), Image.NEAREST), (0, 0)); pv.alpha_composite(ref.resize((160, 160), Image.NEAREST), (192 * 5 + 40 - 160, 0))
    pv.save(os.path.join(OUT, "grey_robe_preview.png")); print("wrote the strip and the preview (the Wanderer on the right for scale)")
