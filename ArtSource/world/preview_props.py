# Preview: every prop (all variants) on tiled grass with baked shadows, a character for scale. 3x.
import sys
sys.path.insert(0, "/home/claude/art"); sys.path.insert(0, "/home/claude/push/ArtSource")
from PIL import Image, ImageDraw
from worldlib import *
import ground, props

def character():
    import make_armor_avatars as av
    r = [a for a in av.ARMORS if a[0] == "Raider Jacket"][0]
    return av.build(r[0], r[3], r[4]).convert("RGBA")

def place(canvas, p, x, y):
    """put prop p with its pivot at (x, y): shadow first, then the sprite"""
    if p.shadow:
        w, h, dx, dy = p.shadow
        canvas.alpha_composite(shadow_ellipse(w, h), (int(x + dx - w / 2), int(y + dy - h / 2)))
    canvas.alpha_composite(p.im, (int(x - p.pivot[0]), int(y - p.pivot[1])))

def main(out="/tmp/claude-0/props_test.png", scale=3):
    items = []
    for name, fn in props.PROPS.items():
        for v in range(props.VARIANTS.get(name, 1)): items.append((name, fn(1000 + v * 77)))
    W = 640; x, y, rowh = 8, 8, 0; pos = []
    for name, p in items:
        w, h = p.im.size
        if x + w + 8 > W: x = 8; y += rowh + 10; rowh = 0
        pos.append((p, x + p.pivot[0], y + p.pivot[1])); x += w + 6; rowh = max(rowh, h)
    H = y + rowh + 30
    g = ground.GROUNDS["grass"](3); bg = blank(W, H)
    for ty in range(0, H, 64):
        for tx in range(0, W, 64): bg.paste(g, (tx, ty))
    for p, px_, py_ in sorted(pos, key=lambda t: t[2]): place(bg, p, px_, py_)
    ch = character()
    for k in range(6): bg.alpha_composite(ch, (10 + k * 24, H - 22))
    bg = bg.resize((W * scale, H * scale), Image.NEAREST)
    bg.save(out); print(out, bg.size, len(items), "sprites")

if __name__ == "__main__": main()
