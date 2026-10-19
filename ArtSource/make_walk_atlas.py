"""Walk frames for every armor (Batch 24): armor_walk_a.png lifts the left foot one pixel,
armor_walk_b.png the right one. Same cell layout as armor_atlas.png, so UnitSprites slices them
with the same index. Only the legs change - no bobbing of the whole body (Yan: at this size a
1-pixel body shift reads as dancing). Run after export_to_unity.py:
    python make_walk_atlas.py [path/to/Sprites]"""
import os, sys
from PIL import Image

K = (20, 22, 23)
CELL = 16

def leg_top(px, ox, oy):
    """First row where the two leg columns are split by outline (the gap between the legs)."""
    for y in range(9, 15):
        a, b = px[ox + 7, oy + y], px[ox + 8, oy + y]
        if a[3] and b[3] and a[:3] == K and b[:3] == K: return y
    return None

def lift(src, ox, oy, top, left):
    """Shift one leg (left half or right half of the cell) up by a pixel below `top`."""
    out = src.load()
    xs = range(ox, ox + 8) if left else range(ox + 8, ox + 16)
    snap = {(x, y): src.getpixel((x, y)) for x in xs for y in range(oy + top, oy + CELL)}
    for x in xs:
        for y in range(oy + top + 1, oy + CELL):
            out[x, y - 1] = snap[(x, y)]
        out[x, oy + CELL - 1] = (0, 0, 0, 0)

def build(sprites, src='armor_atlas', prefix='armor_walk'):
    """Batch 128: also run for the back views (src='armor_back_atlas', prefix='armor_back_walk')."""
    atlas = Image.open(os.path.join(sprites, src + '.png')).convert('RGBA')
    px = atlas.load(); w, h = atlas.size
    frames = {'a': atlas.copy(), 'b': atlas.copy()}
    for oy in range(0, h, CELL):
        for ox in range(0, w, CELL):
            top = leg_top(px, ox, oy)
            if top is None: continue
            lift(frames['a'], ox, oy, top, left=True)
            lift(frames['b'], ox, oy, top, left=False)
    for k, im in frames.items(): im.save(os.path.join(sprites, f'{prefix}_{k}.png'))
    return atlas, frames

if __name__ == '__main__':
    here = os.path.dirname(os.path.abspath(__file__))
    sprites = sys.argv[1] if len(sys.argv) > 1 else os.path.join(here, '..', 'Assets', 'Resources', 'Sprites')
    build(sprites); build(sprites, 'armor_back_atlas', 'armor_back_walk'); print('wrote armor_walk_a/b.png and armor_back_walk_a/b.png to', sprites)
