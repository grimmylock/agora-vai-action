"""2026-11-24: heat maps of the emptiness audit (`EMPTYAUDIT=<dir> bash runtest.sh ...` writes <dir>/<map>.dist). Green = within 8 tiles of a landmark / door / stash / exit, yellow 8-15, orange 15-25, red > 25, black = not walkable.
Usage: python3 Tools/render/empty_heat.py <dist dir> <out.png>"""
import sys, os
from PIL import Image, ImageDraw, ImageFont
d, out = sys.argv[1], sys.argv[2]
names = ["perimeter", "scrapyard", "scavengers_hub", "vanguard_barracks", "quarry_basin", "abandoned_city"]
Z = 3; tiles = []
font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 14)
for n in names:
    p = os.path.join(d, n + ".dist")
    if not os.path.exists(p): continue
    lines = open(p).read().split("\n"); w, h = map(int, lines[0].split()); im = Image.new("RGB", (w * Z, h * Z + 20), (20, 20, 20)); px = im.load()
    for y in range(h):
        row = list(map(int, lines[1 + y].split()))
        for x, v in enumerate(row):
            c = (12, 12, 12) if v == 255 else (60, 150, 70) if v <= 8 else (210, 190, 60) if v <= 15 else (225, 130, 40) if v <= 25 else (210, 50, 40)
            for dy in range(Z):
                for dx in range(Z): px[x * Z + dx, y * Z + dy + 20] = c
    ImageDraw.Draw(im).text((4, 2), n, font=font, fill=(240, 220, 120)); tiles.append(im)
W = tiles[0].width; H = tiles[0].height; cols = 3; rows = (len(tiles) + cols - 1) // cols
sheet = Image.new("RGB", (W * cols, H * rows), (20, 20, 20))
for i, t in enumerate(tiles): sheet.paste(t, ((i % cols) * W, (i // cols) * H))
sheet.save(out); print("ok", sheet.size)
