# In-hand (B) sprites v2: hand-drawn at the new sizes (pistols ~1.5x, RK-12 0.8x, long guns ~1.4x).
# Each entry: rows (local art), grip = centre of the handle the fist closes on, support = underside of the
# handguard/pump (None = pistol cup), muzzle. Coordinates are local art px; canvas = art + 1 px outline pad.
import sys; sys.path.insert(0, '/home/claude/art/weapons')
from PIL import Image
from weapon_pixel import PAL as WPAL, hexc
PAL = dict(WPAL, p="#5a5f65", q="#3a3e42", r="#26292c", s="#e4e9eb", S="#a3abb0", t="#6e767b", m="#5a3f2b", n="#7d5a3e")
K = (20, 22, 23, 255)

VK74 = dict(grip=(12.5, 6.5), support=(26.5, 4.0), muzzle=(40, 2.5), rows=[
    "....................." + "23" + "." * 12 + "3" + "....",
    "........." + "aa" + "3" + "111111" + "222" + "333" + "aaaaa" + "22" + "...." + "23" + "...",
    "4" + "aaaaaaaa" + "bb" + "3333" + "5555" + "3" + "1" + "333" + "ccccc" + "33" + "2222" + "33" + "221",
    "4" + "bbbbbbbbb" + "c" + "44" + "22221" + "444444" + "bbbbb" + "44" + "4444" + "44" + "434",
    "4" + "b" + "cccccc" + "bb" + "c" + "5" * 13 + "ccccc" + "." * 11,
    "4" + "bbbbbbb" + "dd" + "." + "bccd" + "3" + "." + "2" + "abbbc" + "." * 17,
    "4" + "ddddd" + "...." + "." + "bccd" + "222" + "abbbc" + "." * 17,
    "." * 10 + "bccd" + "....." + "abbbc" + "." * 16,
    "." * 10 + "cddd" + "....." + "abbbc" + "." * 16,
    "." * 20 + "abbc" + "." * 16,
    "." * 21 + "ddd" + "." * 16])

BayouPump = dict(grip=(10.5, 3.5), support=(28.0, 3.5), muzzle=(40, 1.0), rows=[
    "." * 12 + "2" * 8 + "2" * 19 + "0",
    "." * 9 + "333" + "333" + "5115" + "3" + "4444" + "3" * 9 + "44" + "3" + "4444",
    "5" + "3" * 8 + "444" + "4" * 8 + "3333" + "454545454" + "33333" + "2" + ".",
    "5" + "4" * 10 + "4" + "5" + "323" + "...." + "5555" + "5" * 9 + "55555" + "4" + ".",
    "5" + "4" * 10 + "5" + "." + "222" + "." * 24,
    "5" + "4444" + "555555" + "." * 29,
    "5" + "44" + "55" + "." * 35,
    "555" + "." * 37])

# pistols 15% under the first v2 pass (Yan: "a tad smaller"): Makarov 18 px, RK-12 22 px
PSh18Pchela = dict(grip=(4.5, 8.0), support=None, muzzle=(18, 1.5), rows=[
    ".." + "2" * 15 + ".",
    "." + "3" + "5353" + "2" + "1" * 8 + "22" + "4",
    "." + "4" + "5454" + "4" * 11 + "4",
    "." + "3" + "2" + "11" + "2" * 12 + "4",
    ".." + "3" + "4" * 10 + "4" + "555" + ".",
    ".." + "3" + "4" + "bbb" + "4" + "5" + "." + "2" + ".." + "3" + "....",
    ".." + "3" + "4" + "cdc" + "4" + "5" + "4" + "." + "2" + "4" + "3" + "....",
    ".." + "3" + "4" + "c1c" + "4" + "5" + "3333" + ".....",
    "." + "3" + "4" + "dcc" + "4" + "5" + "." * 10,
    "." + "3" + "4" + "ccd" + "4" + "5" + "." * 10,
    "3" + "4" + "cdc" + "4" + "5" + "." * 11,
    "4" + "33333" + "4" + "." * 11])

RK12Kuvalda = dict(grip=(2.0, 9.0), support=None, muzzle=(22, 4.5), rows=[
    "..." + "23" + "." + "22222" + "3.3.3.3.3.3",
    ".." + "245" + "2" + "11111" + "2" * 11,
    "..." + "45" + "1" + "2222" + "3" + "4" + "00" + "4" + "00" + "4" + "00" + "44",
    "..." + "45" + "2" + "3333" + "4" + "4" * 11,
    ".." + "2" + "444" + "44444" + "5" * 10 + "3",
    ".." + "4" * 7 + "3" * 12 + "2",
    ".." + "pqqr" + "4" * 6 + "3" + "." * 9,
    ".." + "pqqr" + "." + "2" + "." + "3" + "." * 12,
    "." + "pqqr" + "." + "2222" + "." * 12,
    "pqrq" + "." * 18,
    "prqr" + "." * 18,
    "rrrr" + "." * 18])

ALL = {"VK74": VK74, "BayouPump": BayouPump, "PSh18Pchela": PSh18Pchela, "RK12Kuvalda": RK12Kuvalda}

def render(rows):
    for r in rows: assert len(r) == len(rows[0]), (len(r), r)
    w, h = len(rows[0]) + 2, len(rows) + 2
    im = Image.new("RGBA", (w, h)); px = im.load()
    for y, r in enumerate(rows):
        for x, ch in enumerate(r):
            if ch != ".": px[x + 1, y + 1] = hexc(PAL[ch]) + (255,)
    out = im.copy(); o = out.load()
    for y in range(h):
        for x in range(w):
            if not px[x, y][3] and any(0 <= x + dx < w and 0 <= y + dy < h and px[x + dx, y + dy][3] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                o[x, y] = K
    return out

def grips(key):
    """WeaponArt.Grips row in canvas px (pad +1): grip, support (NaN = cup), muzzle"""
    d = ALL[key]; g, s, m = d["grip"], d["support"], d["muzzle"]
    nan = float("nan")
    return [g[0] + 1, g[1] + 1, s[0] + 1 if s else nan, s[1] + 1 if s else nan, m[0] + 1, m[1] + 1]

if __name__ == "__main__":
    for k in ALL: print(k, render(ALL[k]["rows"]).size, grips(k))
