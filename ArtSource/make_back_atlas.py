# Batch 128 (Yan: "add the back as well so the player can know which way the npc's are looking"): writes
#   Resources/Sprites/armor_back_atlas.png - every armor seen from behind, same cells as armor_atlas.png (SpriteAtlasIndex.Armor)
# and re-bakes armor_atlas.png's cells from the generator (the faction armband exactly as export_to_unity.py does it), so
# a front change (the round helmet's visor) lands without re-running the whole exporter. Then run make_walk_atlas.py.
#   python3 make_back_atlas.py [path/to/Sprites]
import os, re, sys, io, contextlib
from PIL import Image
with contextlib.redirect_stdout(io.StringIO()):
    import make_faction_armors as fa
av = fa.av
HERE = os.path.dirname(os.path.abspath(__file__))
SPR = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, '..', 'Assets', 'Resources', 'Sprites')
IDX = os.path.join(HERE, '..', 'Assets', 'Scripts', 'GameData', 'SpriteAtlasIndex.cs')
order = re.findall(r'"([^"]+)"', re.search(r'Armor\s*=\s*\{(.*?)\};', open(IDX).read(), re.S).group(1))
rows = {r[3]: r for r in fa.rows}
CELL, COLS = 16, 16

def band(img, fac, spots):
    c = av.hexc(fa.FACTION[fac][0])
    for x, y in spots:
        r, g, b, a = img.getpixel((x, y))
        if a and r + g + b > 80: img.putpixel((x, y), c); break
    return img

old = Image.open(os.path.join(SPR, 'armor_atlas.png')).convert('RGBA')
front = Image.new('RGBA', old.size, (0, 0, 0, 0)); back = Image.new('RGBA', old.size, (0, 0, 0, 0))
changed = []
for i, name in enumerate(order):
    fac, tier, arch, _, st, look, ex = rows[name]
    xy = ((i % COLS) * CELL, (i // COLS) * CELL)
    f = band(av.build(name, st[1] / 100, st[2], L=look), fac, ((5, 8), (4, 7)))
    b = band(av.build(name, st[1] / 100, st[2], L=look, back=True), fac, ((10, 8), (11, 7)))
    if f.tobytes() != old.crop(xy + (xy[0] + CELL, xy[1] + CELL)).tobytes(): changed.append(name)
    front.paste(f, xy); back.paste(b, xy)
front.save(os.path.join(SPR, 'armor_atlas.png')); back.save(os.path.join(SPR, 'armor_back_atlas.png'))
print('fronts changed:', changed)
