"""2026-11-23 (Yan: "the scythe looked better in the mini version ... make it bigger in hand and the blade less curved"): the Verger's in-hand scythe is the mini sprite again; this straightens its blade: each pixel row of the blade
(rows 1-13, above the snath) is shifted sideways by ALPHA of the distance between its centre and the straight line from the tip to the base, so the sweep keeps its pixels (thickness, edge) and loses its bend.
Usage: python3 ArtSource/weapons/scythe_straighten.py [alpha]  (restores from ArtSource/weapons/backup_weapon_VergerScythe_mini_2026-11-23.png first)"""
import os, sys
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); A = float(sys.argv[1]) if len(sys.argv) > 1 else 0.7
src = Image.open(os.path.join(HERE, "backup_weapon_VergerScythe_mini_2026-11-23.png")).convert("RGBA"); out = Image.new("RGBA", src.size); sp, op = src.load(), out.load()
rows = {}
for y in range(1, 14):
    xs = [x for x in range(src.width) if sp[x, y][3] > 0]
    if xs: rows[y] = (min(xs), max(xs), (min(xs) + max(xs)) / 2)
y0, y1 = min(rows), max(rows); c0, c1 = rows[y0][2], rows[y1][2]
for y in range(src.height):
    d = 0
    if y in rows: d = round(A * ((c0 + (c1 - c0) * (y - y0) / (y1 - y0)) - rows[y][2]))
    for x in range(src.width):
        if sp[x, y][3] > 0 and 0 <= x + d < src.width: op[x + d, y] = sp[x, y]
out.save(os.path.join(HERE, "..", "..", "Assets", "Resources", "Sprites", "Weapons", "weapon_VergerScythe.png"))
for y in range(out.height): print("%2d " % y + "".join("#" if op[x, y][3] > 0 else "." for x in range(out.width)))
