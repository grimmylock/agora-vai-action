"""The Verger's Hook chain (CombatFxView.cs, case "hookchain"), redrawn here with the same numbers so it can be seen without Unity: links alternating flat (3x2 px) and edge-on (2x3 px), one per 0.19 tile along a sagging line from his hands (0.5 tile up) to your chest (0.45 up),
a hook head (5x5 steel + 3x3 dark bite) at the end, every piece sliding back toward his hands over 0.5 s and fading; a caught hook (Hit) is taut (sag 0.06) and you slide in 2.0 -> 0.9 tiles over 0.3 s (Rt.Shove eased out); a miss hangs slack (sag 0.24).
Keep the numbers in step with the C#. python3 Tools/render/hook_chain.py out_dir -> hook_chain_sheet.png, hook_chain.gif"""
import os, sys
from PIL import Image, ImageDraw
HERE = os.path.dirname(os.path.abspath(__file__)); SPR = os.path.join(HERE, "..", "..", "Assets", "Resources", "Sprites")
S = 8; PX = 1 / 16.0
STEEL, DARK = (158, 173, 189), (41, 46, 51); BG = (52, 50, 44)
def cell(path, i=0):
    im = Image.open(path).convert("RGBA"); return im.crop((i * 16, 0, i * 16 + 16, 16))
def frame(t, hit, boss, you, W=22 * 8 * 1, Hh=12 * 8 * 1):
    gap = 2.0; slide = 0.0
    if hit:
        u = min(1.0, t / 0.3); slide = 1.1 * (1 - (1 - u) * (1 - u))
    dist = gap - slide
    ox = 3.0                                                  # tiles: the boss's x
    bx, px_ = ox, ox + dist; gy = 8.0                          # the ground line (tile units, y down)
    img = Image.new("RGBA", (int(18 * 16 * S / 8 * 1.0) * 1, int(10 * 16 * S / 8)), BG + (255,)); d = ImageDraw.Draw(img)
    sc = S
    def P(x, y): return (x * 16 * sc / 8 * 1, y * 16 * sc / 8 * 1)
    unit = 16 * sc / 8                                          # px per tile = 16 sprite px * (S/8) ... drawn at 8x -> 128 px a tile? keep 1 sprite px = sc/8*... see below
    return img
def render(hit, times, ground=0):
    """one row: the scene at each time"""
    cells = []
    boss = cell(os.path.join(SPR, "armor_unique_verger_coat.png")); you = Image.open(os.path.join(SPR, "armor_atlas.png")).convert("RGBA").crop((16, 0, 32, 16))
    Z = 6; TILE = 16 * Z                                         # 6x: a tile is 96 px
    for t in times:
        W, Hh = int(7 * TILE), int(2.6 * TILE); im = Image.new("RGBA", (W, Hh), BG + (255,)); d = ImageDraw.Draw(im)
        slide = 1.1 * (1 - (1 - min(1.0, t / 0.3)) ** 2) if hit else 0.0
        dist = 2.0 - slide; bx = 1.3 * TILE; byc = 1.7 * TILE      # boss centre x, ground y (his feet)
        px_ = bx + dist * TILE
        for who, x in ((boss, bx), (you, px_)):
            big = who.resize((TILE, TILE), Image.NEAREST); pron = hit and who is you and t > 0.3 and t < 0.8
            if pron: big = big.rotate(90, resample=Image.NEAREST)
            im.alpha_composite(big, (int(x - TILE / 2), int(byc - TILE)))
        # the chain, exactly as the view builds it (units: tiles, y up; the sprites are 16 px a tile)
        a = (bx / TILE, 0.5); b = (px_ / TILE, 0.45)             # his hands, your chest (above their feet), world units
        L = ((a[0] - b[0]) ** 2 + (a[1] - b[1]) ** 2) ** .5; n = max(4, round(L / 0.19)); sag = (0.06 if hit else 0.24) * min(1.0, L / 2.0)
        k_t = t / 0.5; alpha = max(0.0, 1 - k_t)
        if alpha > 0:
            lay = Image.new("RGBA", im.size, (0, 0, 0, 0)); dl = ImageDraw.Draw(lay)
            def bit(pos, col, w, h):
                x, y = pos[0] + (a[0] - pos[0]) * k_t, pos[1] + (a[1] - pos[1]) * k_t            # vel = (a - pos) / 0.5 over t seconds
                cx, cy = x * TILE, byc - y * TILE; hw, hh = w * Z / 2, h * Z / 2
                dl.rectangle((cx - hw, cy - hh, cx + hw - 1, cy + hh - 1), fill=col + (int(255 * alpha),))
            for k in range(1, n + 1):
                u = k / n; pos = (a[0] + (b[0] - a[0]) * u, a[1] + (b[1] - a[1]) * u - sag * 4 * u * (1 - u)); flat = k % 2 == 0
                bit(pos, STEEL if flat else DARK, 3 if flat else 2, 2 if flat else 3)
            bit((b[0] + 0.0625, b[1] - 0.0625), DARK, 2, 2); bit(b, STEEL, 5, 2); bit((b[0] + 0.125, b[1] - 0.125), STEEL, 2, 3); bit((b[0] + 0.0625, b[1] - 0.25), STEEL, 2, 1)
            if hit and t < 0.15:                                                              # the catch: sparks
                import random; random.seed(int(t * 1000))
                for _ in range(14):
                    ang = random.random() * 6.28; r = random.random() * (0.25 + t * 3) * TILE; cx, cy = b[0] * TILE + r * __import__("math").cos(ang), byc - b[1] * TILE + r * __import__("math").sin(ang)
                    dl.rectangle((cx, cy, cx + Z * 1.5, cy + Z * 1.5), fill=(255, 238, 180, int(255 * (1 - t / 0.15))))
            im.alpha_composite(lay)
        d = ImageDraw.Draw(im); d.text((6, 4), f"{'caught' if hit else 'missed'}  t = {t:.2f}s", fill=(235, 235, 235))
        cells.append(im)
    return cells
def main(out):
    os.makedirs(out, exist_ok=True)
    hit = render(True, [0.0, 0.08, 0.16, 0.24, 0.32, 0.40, 0.48]); miss = render(False, [0.0, 0.12, 0.25, 0.38])
    W, Hh = hit[0].size; sheet = Image.new("RGBA", (W * 3, Hh * 4), (27, 29, 31, 255))
    for i, c in enumerate(hit): sheet.alpha_composite(c, ((i % 3) * W, (i // 3) * Hh))
    for i, c in enumerate(miss[:3]): sheet.alpha_composite(c, (i * W, 3 * Hh))
    sheet.convert("RGB").save(os.path.join(out, "hook_chain_sheet.png"))
    frames = [c.convert("RGB") for c in render(True, [i / 30 for i in range(0, 18)]) + render(False, [i / 30 for i in range(0, 18)])]
    frames[0].save(os.path.join(out, "hook_chain.gif"), save_all=True, append_images=frames[1:], duration=50, loop=0); print("wrote", out)
if __name__ == "__main__": main(sys.argv[1] if len(sys.argv) > 1 else "maps/renders/verger")
