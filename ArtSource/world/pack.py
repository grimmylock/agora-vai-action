# Packs every world sprite into one atlas for the 3/4 renderer (WorldArt.cs):
#   world_atlas.png  - the pixels (shipped as Resources/Sprites/world_atlas.bytes)
#   world_atlas.txt  - one line per sprite: name x y w h pivotX pivotY shadowW shadowH shadowDX shadowDY tall
#                      (x, y from the atlas's top-left; pivot in sprite px from its top-left; no shadow = 0 0 0 0)
#   world_atlas.raw  - the harness's RAW1 copy (bottom-up rows)
import struct, sys, os
from PIL import Image
from worldlib import *
import ground, props, walls, flats

def collect():
    items = []                                     # (name, img, pivot, shadow, tall)
    for n, fn in ground.GROUNDS.items(): items.append((f"g_{n}", fn(3), (0, 0), None, False))
    for n, im in flats.all_flats().items(): items.append((n, im, (im.width // 2, im.height - 1), None, False))
    for n, fn in props.PROPS.items():
        for v in range(props.VARIANTS.get(n, 1)):
            p = fn(1000 + v * 77); items.append((f"p_{n}_{v}", p.im, p.pivot, p.shadow, p.tall))
    for n, im in walls.all_tiles().items(): items.append((n, im, (8, im.height - 1), None, False))
    return items

def pack(items, W=1024):
    order = sorted(range(len(items)), key=lambda i: -items[i][1].height)
    x = y = rowh = 0; pos = {}
    for i in order:
        w, h = items[i][1].size
        if x + w > W: x = 0; y += rowh + 1; rowh = 0
        pos[i] = (x, y); x += w + 1; rowh = max(rowh, h)
    H = 1
    while H < y + rowh: H *= 2
    atlas = blank(W, H)
    for i, (x, y) in pos.items(): atlas.paste(items[i][1], (x, y))
    return atlas, pos

def main(out):
    os.makedirs(out, exist_ok=True)
    items = collect(); atlas, pos = pack(items)
    atlas.save(f"{out}/world_atlas.png")
    lines = []
    for i, (name, im, pv, sh, tall) in enumerate(items):
        x, y = pos[i]; s = sh or (0, 0, 0, 0)
        lines.append(f"{name} {x} {y} {im.width} {im.height} {pv[0]} {pv[1]} {s[0]} {s[1]} {s[2]} {s[3]} {1 if tall else 0}")
    open(f"{out}/world_atlas.txt", "w").write("\n".join(lines) + "\n")
    a = atlas.transpose(Image.FLIP_TOP_BOTTOM).tobytes()
    open(f"{out}/world_atlas.raw", "wb").write(b"RAW1" + struct.pack("<ii", *atlas.size) + a)
    print(out, atlas.size, len(items), "sprites")

if __name__ == "__main__": main(sys.argv[1] if len(sys.argv) > 1 else "/tmp/claude-0/world_out")
