"""Pack 1: the pack screen drawn from the harness (`dotnet /tmp/test.dll packdump p.json`: a kit + loot packed by the real
Pack.Settle through Player.AddItem, for each bag) in HudView.Pack.cs's layout and colours, with the real icons. ponytail: the
panel is uGUI in the game; this draws the same rectangles and sprites outside Unity, so fonts and the frame are approximate.
    python3 Tools/render/pack_screen.py p.json out_dir"""
import json, os, re, sys
from PIL import Image, ImageDraw, ImageFont
R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SPR = os.path.join(R, "Assets/Resources/Sprites"); FONTS = os.path.join(R, "Assets/Resources/Fonts")
CELL = 56
def hexc(h): h = h.lstrip('#'); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)
def mul(c, f): return tuple(int(v * f) for v in c[:3]) + (255,)
TITLE = ImageFont.truetype(os.path.join(FONTS, "Silkscreen-Bold.ttf"), 20); BODY = ImageFont.truetype(os.path.join(FONTS, "PixelifySans-SemiBold.ttf"), 15)
idx = open(os.path.join(R, "Assets/Scripts/GameData/SpriteAtlasIndex.cs")).read()
NAMES = re.findall(r'"([^"]*)"', re.search(r'public static readonly string\[\] Armor\s*=\s*\{(.*?)\};', idx, re.S).group(1))
ARMOR = Image.open(os.path.join(SPR, "armor_icon_atlas.png")).convert("RGBA")
def icon(path):
    if path.startswith("armor:"):
        i = NAMES.index(path[6:]); return ARMOR.crop(((i % 16) * 48, (i // 16) * 48, (i % 16) * 48 + 48, (i // 16) * 48 + 48))
    f = os.path.join(SPR, path)
    if not os.path.exists(f): f = os.path.join(SPR, path.replace("_hd.png", ".png"))
    if not os.path.exists(f): f = os.path.join(SPR, "Items/icon_loot.png")
    return Image.open(f).convert("RGBA")
def fit(im, w, h):
    k = max(1, min(w // im.width, h // im.height))                     # integer scale (preserveAspect, kept crisp)
    return im.resize((im.width * k, im.height * k), Image.NEAREST)
def screen(bag):
    bw, bh = bag["cols"] * CELL, bag["rows"] * CELL; W, H = int(bw + 48 + 420), int(max(bh + 48 + 150, 560))
    c = Image.new("RGBA", (W, H), hexc("#1a1c1d")); d = ImageDraw.Draw(c)
    d.rectangle((0, 0, W - 1, H - 1), outline=hexc("#5a5448"), width=2)
    title = "POCKETS" if bag["id"] == "none" else bag["name"].split("(")[0].strip().upper()
    d.text((18, 12), title, font=TITLE, fill=hexc("#e8dcc0"))
    cloth = hexc(bag["colour"])
    d.rectangle((18, 48, 18 + bw + 12, 48 + bh + 12), fill=mul(cloth, 0.55)); d.rectangle((24, 54, 24 + bw - 1, 54 + bh - 1), fill=cloth)
    for y in range(bag["rows"]):
        for x in range(bag["cols"]): d.rectangle((24 + x * CELL + 2, 54 + y * CELL + 2, 24 + (x + 1) * CELL - 3, 54 + (y + 1) * CELL - 3), fill=mul(cloth, 0.82))
    out = [i for i in bag["items"] if i["x"] < 0]
    for it in bag["items"]:
        im = icon(it["icon"]); w, h = it["w"], it["h"]
        if it["x"] < 0: continue
        im = fit(im, w * CELL - 8, h * CELL - 8)
        if it["rot"]: im = im.rotate(90, expand=True)
        fw, fh = (h, w) if it["rot"] else (w, h)
        x0, y0 = 24 + it["x"] * CELL, 54 + it["y"] * CELL
        c.alpha_composite(im, (x0 + (fw * CELL - im.width) // 2, y0 + (fh * CELL - im.height) // 2))
        if it["qty"] > 1: d.text((x0 + fw * CELL - 30, y0 + fh * CELL - 22), f"x{it['qty']}", font=BODY, fill=hexc("#e8dcc0"))
    oy = 54 + bh + 18
    d.rectangle((18, oy, 18 + bw + 12, oy + 130), fill=(0, 0, 0, 60))
    if out: d.text((26, oy + 4), "DOESN'T FIT - MAKE ROOM, OR X TO DROP", font=BODY, fill=hexc("#e0c060"))
    ox = 26
    for it in out:
        im = fit(icon(it["icon"]), it["w"] * CELL - 8, it["h"] * CELL - 8); c.alpha_composite(im, (ox, oy + 26)); ox += it["w"] * CELL + 8
    rx = bw + 48
    d.text((rx, 12), "EQUIPPED", font=TITLE, fill=hexc("#e8dcc0"))
    eq = [("WEAPON", "Weapons/weapon_PSh18Pchela_hd.png", "PSh-18 Pchela"), ("SECOND", None, "-"), ("ARMOR", "armor:Leather Jacket", "Leather Jacket"),
          ("BACKPACK", f"Items/icon_backpack_{bag['id']}.png" if bag["id"] != "none" else None, bag["name"] if bag["id"] != "none" else "-")]
    for n, (slot, ic, name) in enumerate(eq):
        y = 46 + n * 72; d.rectangle((rx, y, rx + 150, y + 64), fill=(0, 0, 0, 80))
        if ic: im = fit(icon(ic), 142, 58); c.alpha_composite(im, (rx + 75 - im.width // 2, y + 32 - im.height // 2))
        d.text((rx + 160, y + 6), slot, font=BODY, fill=hexc("#8a8478")); d.text((rx + 160, y + 28), name, font=BODY, fill=hexc("#e8dcc0"))
    d.text((rx, H - 70), "DRAG to move    R turn\nRIGHT-CLICK use / equip    X drop    I close", font=BODY, fill=hexc("#8a8478"))
    return c
d = json.load(open(sys.argv[1])); out = sys.argv[2]; os.makedirs(out, exist_ok=True)
for bag in d["bags"]:
    screen(bag).convert("RGB").save(os.path.join(out, f"pack_{bag['id']}.png"))
print("pack screens ->", out)
