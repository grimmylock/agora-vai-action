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

# RT 6j (Yan: "some of them take a part of the weapon with it ... the pistol magazine looks like a square"): the cut is the
# magazine's own colour columns x0..x1 from row y0 down (in-hand sprite pixels), nothing else. The magazine keeps its hand-drawn
# pixels and gets a closed outline and a round at its lips; the gun's emptied well is closed with outline.
MAGS = {
    # SMGs
    "PPZZubr": (10, 25, 6), "CorvanHornet45": (19, 21, 6), "VosaVz62": (11, 17, 9), "CorvanGnatPDW": (6, 8, 14),
    # rifles
    "VKS74U": (17, 23, 5), "Smerch3": (14, 17, 7), "VK74": (19, 24, 6), "VKM": (19, 26, 6), "TS9Tishina": (17, 21, 6),
    "TR30": (16, 21, 6), "HaldenHR36K": (17, 21, 8), "VK103": (19, 26, 6), "BR86Bulldog": (8, 12, 9),
    "VK74Obelisk": (19, 24, 8), "NA94Yenisei": (18, 22, 6), "CorvanBullpup": (6, 9, 14), "CorvanBattleRifle": (18, 21, 6),
    # marksman and sniper rifles with a box magazine
    "VSNShepot": (18, 21, 8), "CorvanMarksman": (16, 19, 7), "SV2Strela": (16, 20, 7), "Sever98": (17, 19, 8),
    "VK127Grom": (7, 10, 10), "HaldenAM50Sledge": (17, 22, 10),
    # magazine shotguns, machine-gun drums and boxes
    "Sayan12": (17, 21, 5), "Kaban12": (17, 25, 5), "PL62Kobza": (19, 25, 6), "CorvanLMG": (19, 23, 9),
    "RL44": (16, 21, 6), "PL74": (17, 21, 7),
}
# magazine in the grip: (grip x, grip y) from WeaponArt.Grips. A drawn magazine (steel body lit on the left, a round at the
# lips, a polymer base plate) sits flush with the bottom of the grip, behind the gun.
HIDDEN = {"PSh18Pchela": (5.5, 9), "WaldenP9": (5, 10), "Halden45Service": (4.5, 9.5), "SR21Gadyuka": (4, 10.5),
          "BlackHarrier": (4.5, 9.5), "Corvan57": (4.5, 10), "SteppeEagle": (4.5, 10)}
PISTOL_MAG = [".4y4.",
              ".234.",
              ".234.",
              ".234.",
              ".234.",
              "rqqqr"]

# RT 6i: break-actions - the barrels (pixels at x >= hinge x, above `top` or at x >= `fore`) swing open around the hinge:
# key: (hinge x, hinge y, top row, forend x) -> weapon_<Key>_body.png (stock and action) + weapon_<Key>_barrels.png
BREAK = {"Obrez66": (9, 4, 3, 13), "Sever34": (16, 4, 5, 19)}

K = (20, 22, 23, 255)
WPAL = {"0": "#d5dadd", "1": "#a9b0b5", "2": "#727980", "3": "#4c5258", "4": "#33373b", "5": "#1f2225",
        "p": "#5a5f65", "q": "#3a3e42", "r": "#26292c", "Y": "#c9a63e", "y": "#f0d878"}
def hexc(h): h = h.lstrip("#"); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)
PAD = 4                                                    # room under the gun for a magazine's outline

def is_k(c): return c[3] and c[0] < 30 and c[1] < 30 and c[2] < 30

def ring(img, inside=lambda x, y: True):
    """Outline: transparent pixels (where `inside` allows) next to a non-outline pixel become outline."""
    px = img.load(); w, h = img.size; add = []
    for y in range(h):
        for x in range(w):
            if px[x, y][3] or not inside(x, y): continue
            if any(0 <= x + dx < w and 0 <= y + dy < h and px[x + dx, y + dy][3] and not is_k(px[x + dx, y + dy])
                   for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))): add.append((x, y))
    for p in add: px[p] = K

def cut(key):
    gun = Image.open(os.path.join(WEAP, f"weapon_{key}.png")).convert("RGBA")
    w, h = gun.size
    body = Image.new("RGBA", (w, h + PAD)); body.alpha_composite(gun)
    mag = Image.new("RGBA", (w, h + PAD))
    b, m = body.load(), mag.load()
    if key in HIDDEN:
        gx, gy = HIDDEN[key]
        near = [(x, y) for y in range(h) for x in range(w) if b[x, y][3] and not is_k(b[x, y]) and abs(x + 0.5 - gx) <= 3 and y > gy]
        bottom = max(y for _, y in near); xs = [x for x, y in near if y == bottom]
        cx = round((min(xs) + max(xs)) / 2) - 2                     # left column of the 5-wide drawing
        for j, row in enumerate(PISTOL_MAG):
            for i, ch in enumerate(row):
                if ch != ".": m[cx + i, bottom - len(PISTOL_MAG) + 1 + j] = hexc(WPAL[ch])
        ring(mag)
        return body.crop((0, 0, w, h)), mag
    x0, x1, y0 = MAGS[key]
    for y in range(y0, h):
        for x in range(x0, x1 + 1):
            if b[x, y][3] and not is_k(b[x, y]): m[x, y] = b[x, y]; b[x, y] = (0, 0, 0, 0)
    # the magazine's top row: a round at the lips
    top = [x for x in range(x0, x1 + 1) if m[x, y0][3]]
    if top:
        mid = (min(top) + max(top)) // 2
        m[mid, y0 - 1] = hexc(WPAL["Y"]); m[min(mid + 1, w - 1), y0 - 1] = hexc(WPAL["y"])
    ring(mag)
    # the gun: drop outline pixels the magazine left behind, close the well
    for _ in range(2):
        for y in range(y0, h):
            for x in range(max(0, x0 - 1), min(w, x1 + 2)):
                if is_k(b[x, y]) and not any(0 <= x + dx < w and 0 <= y + dy < h + PAD and b[x + dx, y + dy][3] and not is_k(b[x + dx, y + dy])
                                             for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))): b[x, y] = (0, 0, 0, 0)
    ring(body, lambda x, y: x0 - 1 <= x <= x1 + 1 and y >= y0 - 1)
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
    cw = max(r[1].width for r in rows) * S + 12; ch = (max(r[1].height for r in rows) + PAD + 8) * S + 8
    sheet = Image.new("RGBA", (180 + 4 * cw, len(rows) * ch + 30), (27, 29, 31, 255)); d = ImageDraw.Draw(sheet)
    for i, t in enumerate(("gun", "body", "magazine cut (red)", "pulled out")): d.text((180 + i * cw, 8), t, fill=(220, 220, 220))
    for j, (k, gun, body, mag) in enumerate(rows):
        y = 26 + j * ch; d.text((8, y + 8), k + (" (hidden)" if k in HIDDEN else ""), fill=(220, 220, 220))
        red = Image.new("RGBA", mag.size); red.alpha_composite(gun); r = red.load(); mp = mag.load()
        for yy in range(mag.height):
            for xx in range(mag.width):
                if mp[xx, yy][3]: r[xx, yy] = (220, 60, 50, 255)
        pulled = Image.new("RGBA", (gun.width, mag.height + 8)); pulled.alpha_composite(mag, (0, 8)); pulled.alpha_composite(body)
        for i, img in enumerate((gun, body, red, pulled)):
            bg = Image.new("RGBA", img.size, (46, 42, 34, 255)); bg.alpha_composite(img)
            sheet.alpha_composite(bg.resize((img.width * S, img.height * S), Image.NEAREST), (180 + i * cw, y))
    sheet.convert("RGB").save(os.path.join(out, "mags_sheet.png")); print(len(rows), "guns ->", os.path.join(out, "mags_sheet.png"))
    # RT 6j: where each magazine's top (it goes into the well there) and middle are, for the game -> WeaponMags.cs
    rows_cs = []
    for k, gun, body, mag in rows:
        pts = [(x, y) for y in range(mag.height) for x in range(mag.width) if mag.getpixel((x, y))[3]]
        top = min(y for _, y in pts); tx = [x for x, y in pts if y == top]
        x0, x1 = min(x for x, _ in pts), max(x for x, _ in pts); y1 = max(y for _, y in pts)
        rows_cs.append(f'            ["{k}"] = new[] {{ {(min(tx) + max(tx) + 1) / 2}f, {top}f, {(x0 + x1 + 1) / 2}f, {(top + y1 + 1) / 2}f }},')
    open(os.path.join(ROOT, "Assets/Scripts/GameCore/WeaponMags.cs"), "w").write(
        "// Generated by ArtSource/weapons/mags.py (RT 6j) - do not edit by hand.\n"
        "// Per gun with a magazine sprite: the top-middle of the magazine (where it meets the well) and its middle, in the\n"
        "// in-hand sprite's pixels: well x, well y, centre x, centre y.\n"
        "using System.Collections.Generic;\n\nnamespace GameCore\n{\n    public static class WeaponMags\n    {\n"
        "        public static readonly Dictionary<string, float[]> Points = new Dictionary<string, float[]>\n        {\n"
        + "\n".join(rows_cs) + "\n        };\n    }\n}\n")
    for k in BREAK:
        src = os.path.join(WEAP, f"weapon_{k}.png")
        for part, img in zip(("body", "barrels"), split(k)):
            p = os.path.join(WEAP, f"weapon_{k}_{part}.png"); img.save(p); meta(p, src)
        print(k, "split at the hinge")

if __name__ == "__main__":
    main()
