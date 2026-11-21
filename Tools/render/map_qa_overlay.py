"""2026-11-21: draws a map's QA marks (map_qa_<id>.tsv from MAP_QA=1 render.sh) on its render: red boxes for pockets, doors, blocked doorways, pillars, lost things;
   python3 Tools/render/map_qa_overlay.py <id> [x y r]   -> maps/renders/qa/<id>_qa.png (whole map), or a 4x crop round (x, y) of radius r tiles to /tmp/qa_<id>_<x>_<y>.png"""
import os, sys
from PIL import Image, ImageDraw
R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))); D = os.path.join(R, "Tools/render/_render")
mid = sys.argv[1]; im = Image.open(os.path.join(D, mid + ".png")).convert("RGB"); T = 16; d = ImageDraw.Draw(im)
col = {"POCKET": (255, 60, 60), "DOOR": (255, 160, 0), "BLOCKED": (255, 0, 255), "PILLAR": (0, 200, 255), "LOST": (255, 255, 0), "DIAG": (120, 255, 120), "EXIT": (255, 255, 255), "JAG": (255, 120, 40)}
p = os.path.join(D, "map_qa_%s.tsv" % mid)
for l in (open(p).read().splitlines() if os.path.exists(p) else []):
    k, x, y, w, h = l.split("\t")[:5]; x, y, w, h = int(x), int(y), int(w), int(h)
    if k == "JAGSUM": continue
    d.rectangle([x * T - 1, y * T - 1, (x + w) * T, (y + h) * T], outline=col.get(k, (255, 255, 255)), width=2)
if len(sys.argv) > 4:
    x, y, r = map(int, sys.argv[2:5]); c = im.crop(((x - r) * T, (y - r) * T, (x + r + 1) * T, (y + r + 1) * T)); c = c.resize((c.width * 3, c.height * 3), Image.NEAREST)
    dd = ImageDraw.Draw(c)
    for i in range(2 * r + 2): dd.text((i * T * 3 + 2, 2), str(x - r + i), fill=(255, 255, 0)); dd.text((2, i * T * 3 + 2), str(y - r + i), fill=(0, 255, 255))
    c.save("/tmp/qa_%s_%d_%d.png" % (mid, x, y)); print("/tmp/qa_%s_%d_%d.png" % (mid, x, y))
else:
    os.makedirs(os.path.join(R, "maps/renders/qa"), exist_ok=True); im.save(os.path.join(R, "maps/renders/qa", mid + "_qa.png"))
