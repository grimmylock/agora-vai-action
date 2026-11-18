"""Husks (Yan, 2026-11-18): "desaturated colour stalkers with grey eyes and some damage marks in the body ... they kinda drag themselves around
instead of walking normally, maybe a sprite variation where they are hunched over".
Reads the 16 px human atlases (armor_atlas, armor_walk_a/b, armor_back_atlas, armor_back_walk_a/b: every suit of every faction) and writes a Husk copy of
each, in the same layout, at the doubled density (32 px cells; wounds and eyes are drawn at that density):
  husk_atlas_x2.png, husk_walk_a_x2.png, husk_walk_b_x2.png, husk_back_atlas_x2.png, husk_back_walk_a_x2.png, husk_back_walk_b_x2.png
 - the head and shoulders drop one pixel and lean (hunched), the walk frames drag one foot (the second walk frame keeps the lean the other way)
 - colour mostly gone (the faction colours fade but stay readable), skin paler
 - the front views get two pale grey eyes
 - damage: dark dried blood, a fresh smear, and a gash or two on the torso, arms and legs, the same places in every frame of a suit
UnitSprites loads them for faction "Husks". Re-run after the 16 px atlases change.   python3 ArtSource/make_husks.py"""
import os, random
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); DIR = os.path.join(HERE, "..", "Assets", "Resources", "Sprites")
SETS = [("armor_atlas", "husk_atlas", True, 0), ("armor_walk_a", "husk_walk_a", True, 1), ("armor_walk_b", "husk_walk_b", True, 2),
        ("armor_back_atlas", "husk_back_atlas", False, 0), ("armor_back_walk_a", "husk_back_walk_a", False, 1), ("armor_back_walk_b", "husk_back_walk_b", False, 2)]
COLS = 16
def lum(p): return 0.3 * p[0] + 0.59 * p[1] + 0.11 * p[2]
def husk_cell(cell16, front, frame, seed):
    # 1. hunch at 16 px: the head (top rows of the silhouette) drops a pixel and leans; frame 1/2 lean opposite ways so the walk looks dragged
    px = cell16.load(); W, H = cell16.size
    rows = [y for y in range(H) if any(px[x, y][3] > 0 for x in range(W))]
    if not rows: return Image.new("RGBA", (W * 2, H * 2), (0, 0, 0, 0))
    top = rows[0]; lean = 1 if frame != 2 else -1
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0)); o = out.load()
    for y in range(H):
        for x in range(W):
            p = px[x, y]
            if p[3] == 0: continue
            ny, nx = y, x
            if y < top + 7: ny = y + 1; nx = x + lean            # head and shoulders: down and sideways
            elif y < top + 9: nx = x + (lean if frame else 0)     # chest follows a little on the walk frames
            if 0 <= nx < W and 0 <= ny < H: o[nx, ny] = p
    # 2. double it
    big = out.resize((W * 2, H * 2), Image.NEAREST); b = big.load(); BW, BH = big.size
    # 3. desaturate and pale
    for y in range(BH):
        for x in range(BW):
            p = b[x, y]
            if p[3] == 0: continue
            l = lum(p); k = 0.30                                   # how much colour stays
            r, g, bl = [int(l * (1 - k) + c * k) for c in p[:3]]
            r, g, bl = int(r * 0.92 + 14), int(g * 0.95 + 14), int(bl * 0.97 + 16)   # a cold, sick cast
            b[x, y] = (min(255, r), min(255, g), min(255, bl), p[3])
    rng = random.Random(seed)
    # body pixels (not the black outline): candidates for wounds
    def body(x, y): return 0 <= x < BW and 0 <= y < BH and b[x, y][3] > 0 and lum(b[x, y]) > 30
    ys = [y for y in range(BH) if any(b[x, y][3] > 0 for x in range(BW))]
    t = ys[0]; bot = ys[-1]; hgt = bot - t
    def spots(y0, y1): return [(x, y) for y in range(y0, y1) for x in range(BW) if body(x, y)]
    torso = spots(t + int(hgt * 0.38), t + int(hgt * 0.72)); legs = spots(t + int(hgt * 0.72), bot - 1); head = spots(t + 2, t + int(hgt * 0.36))
    def blot(pts, col, n, size):
        for _ in range(n):
            if not pts: return
            x, y = rng.choice(pts)
            for dx in range(size):
                for dy in range(size):
                    if body(x + dx, y + dy) and rng.random() < 0.85: b[x + dx, y + dy] = col + (255,)
    blot(torso, (78, 30, 28), 3, 3)                                 # old blood
    blot(torso, (120, 40, 34), 1, 2)                                # a fresh smear
    blot(legs, (70, 28, 26), 2, 2)
    blot(head, (84, 34, 30), 1, 2)
    for _ in range(2):                                              # gashes: a dark diagonal two pixels long, pale edge
        if torso:
            x, y = rng.choice(torso)
            for i in range(4):
                if body(x + i, y + i): b[x + i, y + i] = (38, 16, 16, 255)
                if body(x + i + 1, y + i): b[x + i + 1, y + i] = (150, 120, 118, 255)
    # 4. grey eyes (front views): two pale pixels where the face is, the head being the top of the silhouette
    if front:
        row = t + 8 if t + 8 < bot else t + 4                       # the face row of the (dropped) head
        xs = [x for x in range(BW) if body(x, row)]
        if len(xs) >= 6:
            cx = (xs[0] + xs[-1]) // 2
            for ex in (cx - 3, cx + 2):
                for dx in range(2): b[ex + dx, row] = (196, 202, 206, 255)
                b[ex, row + 1] = (80, 84, 88, 255); b[ex + 1, row + 1] = (80, 84, 88, 255)
    return big
def main():
    for src, dst, front, frame in SETS:
        path = os.path.join(DIR, src + ".png")
        if not os.path.exists(path): print("missing", src); continue
        im = Image.open(path).convert("RGBA"); W, H = im.size; cells = (W // 16, H // 16)
        out = Image.new("RGBA", (W * 2, H * 2), (0, 0, 0, 0))
        for cy in range(cells[1]):
            for cx in range(cells[0]):
                cell = im.crop((cx * 16, cy * 16, cx * 16 + 16, cy * 16 + 16))
                out.paste(husk_cell(cell, front, frame, cy * COLS + cx), (cx * 32, cy * 32))
        out.save(os.path.join(DIR, dst + "_x2.png")); print("wrote", dst + "_x2.png")
if __name__ == "__main__": main()
