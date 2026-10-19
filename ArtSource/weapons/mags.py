"""RT 6g: magazines as their own sprites, so a reload can take them out (Yan: "rig them up to the actual weapons").
For every gun in MAGS: the opaque pixels inside its box are the magazine -> weapon_<Key>_mag.png, the rest -> weapon_<Key>_body.png
(both the gun's own size, so they share its pivot). HIDDEN guns keep the magazine inside the grip: the mag is the grip's shape
from the grip point down, in magazine steel, drawn behind the gun (only the part that slides out shows); their body is the gun.
Boxes are (x0, y0, x1, y1), inclusive, in the in-hand sprite's pixels (Assets/Resources/Sprites/Weapons/weapon_<Key>.png).
  python3 ArtSource/weapons/mags.py            -> writes the pngs (+ Unity .meta files) and ArtSource/_out/mags_sheet.png"""
import os, re, uuid
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
WEAP = os.path.join(ROOT, "Assets/Resources/Sprites/Weapons")

MAGS = {
    # SMGs
    "PPZZubr": (9, 6, 26, 8), "CorvanHornet45": (18, 7, 22, 15), "VosaVz62": (10, 9, 18, 15), "CorvanGnatPDW": (5, 12, 9, 17),
    # rifles
    "VKS74U": (14, 6, 24, 13), "Smerch3": (14, 8, 18, 11), "VK74": (16, 7, 25, 12), "VKM": (16, 7, 27, 13), "TS9Tishina": (17, 8, 22, 10),
    "TR30": (16, 8, 22, 12), "HaldenHR36K": (17, 10, 22, 13), "VK103": (16, 7, 27, 13), "BR86Bulldog": (7, 9, 12, 15),
    "VK74Obelisk": (16, 9, 25, 14), "NA94Yenisei": (18, 8, 23, 12), "CorvanBullpup": (5, 13, 9, 16), "CorvanBattleRifle": (17, 7, 22, 10),
    # marksman and sniper rifles with a box magazine
    "VSNShepot": (14, 9, 22, 11), "CorvanMarksman": (16, 8, 21, 11), "SV2Strela": (14, 7, 21, 10), "Sever98": (14, 9, 20, 10),
    "VK127Grom": (6, 10, 10, 14), "HaldenAM50Sledge": (16, 10, 23, 13),
    # magazine shotguns, machine-gun drums and boxes
    "Sayan12": (17, 6, 22, 8), "Kaban12": (17, 8, 26, 14), "PL62Kobza": (18, 7, 26, 14), "CorvanLMG": (18, 10, 24, 14),
    "RL44": (14, 8, 22, 13), "PL74": (16, 8, 22, 11),
}
# magazine in the grip: (grip x, grip y) from WeaponArt.Grips - the mag is the grip's pixels within 3 px of it, from its row down
HIDDEN = {"PSh18Pchela": (5.5, 9), "WaldenP9": (5, 10), "Halden45Service": (4.5, 9.5), "SR21Gadyuka": (4, 10.5),
          "BlackHarrier": (4.5, 9.5), "Corvan57": (4.5, 10), "SteppeEagle": (4.5, 10)}

# RT 6i: break-actions - the barrels (pixels at x >= hinge x, above `top` or at x >= `fore`) swing open around the hinge:
# key: (hinge x, hinge y, top row, forend x) -> weapon_<Key>_body.png (stock and action) + weapon_<Key>_barrels.png
BREAK = {"Obrez66": (9, 4, 3, 13), "Sever34": (16, 4, 5, 19)}

STEEL, STEEL_LIT, EDGE = (61, 65, 72, 255), (96, 102, 110, 255), (20, 22, 23, 255)

def cut(key):
    gun = Image.open(os.path.join(WEAP, f"weapon_{key}.png")).convert("RGBA")
    w, h = gun.size; px = gun.load()
    body, mag = gun.copy(), Image.new("RGBA", gun.size)
    if key in HIDDEN:
        gx, gy = HIDDEN[key]
        cells = {(x, y) for y in range(int(gy), h) for x in range(w) if px[x, y][3] and abs(x + 0.5 - gx) <= 3}
        for (x, y) in cells:                                   # steel, outlined, a lit top edge; the base plate darker
            edge = any((x + dx, y + dy) not in cells for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
            mag.putpixel((x, y), EDGE if edge else STEEL_LIT if (x, y - 1) not in cells or y == int(gy) else STEEL)
        return body, mag
    x0, y0, x1, y1 = MAGS[key]; b, m = body.load(), mag.load()
    for y in range(y0, min(y1, h - 1) + 1):
        for x in range(x0, min(x1, w - 1) + 1):
            if px[x, y][3]: m[x, y] = px[x, y]; b[x, y] = (0, 0, 0, 0)
    return body, mag

def split(key):
    gun = Image.open(os.path.join(WEAP, f"weapon_{key}.png")).convert("RGBA"); px = gun.load()
    hx, hy, top, fore = BREAK[key]
    body, bar = gun.copy(), Image.new("RGBA", gun.size); b, r = body.load(), bar.load()
    for y in range(gun.height):
        for x in range(gun.width):
            if px[x, y][3] and x >= hx and (y <= top or x >= fore): r[x, y] = px[x, y]; b[x, y] = (0, 0, 0, 0)
    return body, bar

def meta(png, like):
    """Unity import settings copied from the gun's own .meta (point filter, no compression), with a fresh guid."""
    src = open(like + ".meta").read()
    name = os.path.splitext(os.path.basename(png))[0]; old = os.path.splitext(os.path.basename(like))[0]
    src = re.sub(r"guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, src, count=1).replace(old + "_", name + "_")
    if not os.path.exists(png + ".meta"): open(png + ".meta", "w").write(src)

def main():
    out = os.path.join(ROOT, "ArtSource/_out"); os.makedirs(out, exist_ok=True)
    keys = list(MAGS) + list(HIDDEN); S = 6; rows = []
    for k in keys:
        body, mag = cut(k)
        src = os.path.join(WEAP, f"weapon_{k}.png")
        for part, img in (("body", body), ("mag", mag)):
            if part == "body" and k in HIDDEN: continue        # a hidden magazine leaves the gun as it is
            p = os.path.join(WEAP, f"weapon_{k}_{part}.png"); img.save(p); meta(p, src)
        rows.append((k, Image.open(src).convert("RGBA"), body, mag))
    # review sheet: gun | body | magazine (red = cut) | magazine pulled out
    cw = max(r[1].width for r in rows) * S + 12; ch = max(r[1].height for r in rows) * S + 8
    sheet = Image.new("RGBA", (180 + 4 * cw, len(rows) * ch + 30), (27, 29, 31, 255)); d = ImageDraw.Draw(sheet)
    for i, t in enumerate(("gun", "body", "magazine cut (red)", "pulled out")): d.text((180 + i * cw, 8), t, fill=(220, 220, 220))
    for j, (k, gun, body, mag) in enumerate(rows):
        y = 26 + j * ch; d.text((8, y + 8), k + (" (hidden)" if k in HIDDEN else ""), fill=(220, 220, 220))
        red = gun.copy(); r = red.load(); mp = mag.load()
        for yy in range(gun.height):
            for xx in range(gun.width):
                if mp[xx, yy][3]: r[xx, yy] = (220, 60, 50, 255)
        pulled = Image.new("RGBA", (gun.width, gun.height + 8)); pulled.alpha_composite(mag, (0, 6)); pulled.alpha_composite(body)
        for i, img in enumerate((gun, body, red, pulled)):
            bg = Image.new("RGBA", img.size, (46, 42, 34, 255)); bg.alpha_composite(img)
            sheet.alpha_composite(bg.resize((img.width * S, img.height * S), Image.NEAREST), (180 + i * cw, y))
    sheet.convert("RGB").save(os.path.join(out, "mags_sheet.png")); print(len(rows), "guns ->", os.path.join(out, "mags_sheet.png"))
    for k in BREAK:
        src = os.path.join(WEAP, f"weapon_{k}.png")
        for part, img in zip(("body", "barrels"), split(k)):
            p = os.path.join(WEAP, f"weapon_{k}_{part}.png"); img.save(p); meta(p, src)
        print(k, "split at the hinge")

if __name__ == "__main__":
    main()
