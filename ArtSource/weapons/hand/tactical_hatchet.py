"""Tactical Hatchet (Yan's reference picture, 2026-10-20: a tomahawk with a bearded blade, a hooked back spike, a cog-edged
hole in the head and a black handle with a grey panel, three rivets and a lanyard hole). Turned so the handle lies along the
aim: blade on top, spike underneath, head on the right. Shapes are laid out in sprite pixels, filled at 8x and snapped to
the grid (a pixel is in when half of it is covered), then shaded with the weapon palette and outlined.
  python3 ArtSource/weapons/hand/tactical_hatchet.py  -> Assets/Resources/Sprites/Weapons/weapon_TacticalHatchet(_hd).png
                                                         + ArtSource/_out/tactical_hatchet_sheet.png"""
import os
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
WEAP = os.path.join(ROOT, "Assets/Resources/Sprites/Weapons")
PAL = {"K": "#141617", "s": "#e4e9eb", "S": "#a3abb0", "t": "#6e767b", "0": "#d5dadd", "1": "#a9b0b5", "2": "#727980",
       "p": "#5a5f65", "q": "#3a3e42", "r": "#26292c"}
def hexc(h): h = h.lstrip("#"); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)

# in-hand sprite space (32 x 16, y down); the menu sprite is the same shapes at 2x
HANDLE = [(1.2, 7.4), (20.5, 6.9), (20.5, 10.4), (1.2, 11.0)]
BLADE = [(19.6, 6.9), (17.0, 4.6), (14.6, 1.9), (14.4, 0.8), (21.0, 0.7), (28.6, 0.6), (28.2, 3.6), (27.2, 6.4), (25.0, 6.9)]
NECK = [(19.4, 6.4), (27.2, 6.4), (27.2, 10.6), (19.4, 10.6)]
SPIKE = [(20.4, 10.2), (27.2, 10.2), (25.4, 11.6), (21.8, 13.6), (18.6, 15.5), (19.8, 13.0)]
HOLE = (23.4, 8.5, 1.5)                     # centre x, y, radius
LANYARD = (2.6, 9.2, 0.6)
RIVETS = [7.0, 12.0, 17.0]

def mask(shapes, scale, w, h, holes=()):
    ss = 8 * scale
    im = Image.new("L", (w * ss, h * ss), 0); d = ImageDraw.Draw(im)
    for poly in shapes: d.polygon([(x * ss, y * ss) for x, y in poly], fill=255)
    for cx, cy, r in holes: d.ellipse([(cx - r) * ss, (cy - r) * ss, (cx + r) * ss, (cy + r) * ss], fill=0)
    small = im.resize((w * scale, h * scale), Image.BOX)
    return {(x, y) for y in range(h * scale) for x in range(w * scale) if small.getpixel((x, y)) >= 128}

def draw(scale):
    w, h = 32 * scale, 16 * scale
    head = mask([BLADE, NECK, SPIKE], scale, 32, 16, [HOLE])
    handle = mask([HANDLE], scale, 32, 16, [LANYARD]) - head
    img = Image.new("RGBA", (w, h)); px = img.load()
    top = {x: min((y for (xx, y) in head if xx == x), default=None) for x in range(w)}
    for (x, y) in head:
        # polished edge along the top of the blade, bright body, darker toward the neck and the spike's underside
        if y == top[x] and y <= 2 * scale: c = "s"
        elif y < 6.4 * scale: c = "S" if x > 18 * scale else "t"
        elif y >= 10.4 * scale: c = "S" if x >= 22 * scale - (y - 10.4 * scale) else "t"     # the spike: lit face, dark back edge
        else: c = "t" if x < 20 * scale else "1"
        px[x, y] = hexc(PAL[c])
    for (x, y) in handle:
        c = "r"
        if 7.9 * scale <= y < 9.9 * scale and 4 * scale <= x < 19 * scale: c = "p" if y < 8.9 * scale else "q"   # the grey panel
        px[x, y] = hexc(PAL[c])
    for rx in RIVETS:                                                         # three rivets in the panel
        for dx in range(scale):
            for dy in range(scale):
                px[int(rx * scale) + dx, int(8.4 * scale) + dy] = hexc(PAL["0"])
    if scale >= 2:                                                            # the cog teeth round the hole (menu sprite only)
        cx, cy, r = HOLE
        for i in range(10):
            import math
            a = i / 10 * 2 * math.pi
            x, y = int((cx + (r + 0.45) * math.cos(a)) * scale), int((cy + (r + 0.45) * math.sin(a)) * scale)
            if (x, y) in head: px[x, y] = hexc(PAL["2"])
    # outline: every empty pixel next to the weapon (the hole and the lanyard hole too)
    out = img.copy(); o = out.load()
    for y in range(h):
        for x in range(w):
            if px[x, y][3]: continue
            if any(0 <= x + dx < w and 0 <= y + dy < h and px[x + dx, y + dy][3] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                o[x, y] = hexc(PAL["K"])
    return out

if __name__ == "__main__":
    b, a = draw(1), draw(2)
    b.save(os.path.join(WEAP, "weapon_TacticalHatchet.png")); a.save(os.path.join(WEAP, "weapon_TacticalHatchet_hd.png"))
    S = 10; bg = (46, 42, 34, 255)
    sheet = Image.new("RGBA", (64 * 6 + 40, 32 * 6 + 16 * S + 40), (27, 29, 31, 255))
    t = Image.new("RGBA", a.size, bg); t.alpha_composite(a); sheet.alpha_composite(t.resize((64 * 6, 32 * 6), Image.NEAREST), (10, 10))
    t = Image.new("RGBA", b.size, bg); t.alpha_composite(b); sheet.alpha_composite(t.resize((32 * S, 16 * S), Image.NEAREST), (10, 32 * 6 + 30))
    os.makedirs(os.path.join(ROOT, "ArtSource/_out"), exist_ok=True)
    sheet.save(os.path.join(ROOT, "ArtSource/_out/tactical_hatchet_sheet.png")); print("ok")
