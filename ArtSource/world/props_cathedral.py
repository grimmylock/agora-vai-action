# The cathedral set (2026-11-20; Yan: "we'll probably need new sprites or stuff for a cathedral, right now it's a warehouse with like 30 desks"): double-density props (scale 2, a tile is 32 sprite px)
# for the Abandoned Cathedral (MISSION_CHOIR.md). Front-oblique like the rest of the world. Looks: pew_l / pew_m / pew_r (one pew in three tiles: left end, middle, right end), column, altar, candelabra, statue,
# font, confessional, tomb. Registered into props.PROPS by props.py (see the hook at its end). NOT in the shipped atlas until Yan approves the preview (cathedral_props_preview.png).
import math
from PIL import Image, ImageDraw
import props
from props import Prop, finish
from worldlib import *
from props_x2 import X2, sh, boxx, DARK

STONE = P["rock"]; WOOD = P["wood"]; BRASS = ramp("#3a2a0c", "#6a4c14", "#a07a24", "#d0a63c", "#f2d06a"); CLOTH = ramp("#2a0c0e", "#4a1416", "#6e1c1e", "#92282a", "#b4403c")
WAX = hx("#d8cfae"); FLAME = hx("#ffd860"); FLAME2 = hx("#ff9a30")
def grain(d, x0, y0, x1, y1, col, r, n=14):
    for _ in range(n): x = r.randint(x0, x1); y = r.randint(y0, y1); d.point((x, y), fill=col)

# ---------------------------------------------------------------- pews: dark wood, a high back, a seat, a plinth; one pew spans three tiles, ends carry a carved post
def _outline_pew(im, left, right):
    """the selective outline, but NOT on the sides that join the next pew tile (so a row reads as one bench); no side padding there either."""
    import numpy as np
    a = np.array(im).astype(np.int32); H, W = a.shape[:2]; al = a[..., 3] > 0; o = a.copy()
    dirs = [(0, -1), (0, 1)] + ([(-1, 0)] if left else []) + ([(1, 0)] if right else [])
    for y in range(H):
        for x in range(W):
            if al[y, x]: continue
            best = None
            for dx, dy in dirs:
                nx, ny = x + dx, y + dy
                if 0 <= nx < W and 0 <= ny < H and al[ny, nx]:
                    c = a[ny, nx]; lum = c[0] + c[1] + c[2]
                    if best is None or lum < best[0]: best = (lum, c)
            if best is not None: c = best[1]; o[y, x] = (int(c[0] * 0.42) + 6, int(c[1] * 0.42) + 5, int(c[2] * 0.42) + 4, 255)
    return Image.fromarray(o.astype(np.uint8), 'RGBA')
def _pew(seed, end):
    r = rng(seed); im = blank(32, 36); d = ImageDraw.Draw(im); W = WOOD
    d.rectangle([0, 4, 31, 17], fill=W[2]); d.line([(0, 4), (31, 4)], fill=W[4]); d.line([(0, 17), (31, 17)], fill=W[0])         # the back rest, seen from the front of the row
    for x in range(3, 31, 5): d.line([(x, 6), (x, 15)], fill=W[1])                                                                  # planks
    d.rectangle([0, 18, 31, 22], fill=W[3]); d.line([(0, 18), (31, 18)], fill=W[4]); d.line([(0, 22), (31, 22)], fill=W[0])         # the seat's edge
    d.rectangle([0, 23, 31, 31], fill=W[1]); d.line([(0, 31), (31, 31)], fill=W[0])                                                  # the front panel
    for x in range(4, 31, 8): d.rectangle([x, 25, x + 4, 29], outline=W[0]); d.line([(x + 1, 26), (x + 3, 26)], fill=W[2])       # carved panels
    grain(d, 0, 4, 31, 31, W[0], r)
    if end:                                                                                                                         # the end post: taller than the back, a carved finial
        x = 0 if end == 'l' else 28
        d.rectangle([x, 0, x + 3, 31], fill=W[3]); d.line([(x, 0), (x, 31)], fill=W[4]); d.line([(x + 3, 2), (x + 3, 31)], fill=W[0])
        d.ellipse([x - 1, -1, x + 4, 4], fill=W[4], outline=W[0])
    pad_l = 1 if end == 'l' else 0; pad_r = 1 if end == 'r' else 0
    big = blank(im.width + pad_l + pad_r, im.height + 2); big.alpha_composite(im, (pad_l, 1))
    big = _outline_pew(big, end == 'l', end == 'r')
    p = Prop(big, (16 + pad_l, 35), (32, 5, 0, -1), False, scale=2); return p
def pew_l(seed): return _pew(seed, 'l')
def pew_m(seed): return _pew(seed, None)
def pew_r(seed): return _pew(seed, 'r')

# ---------------------------------------------------------------- column: a fluted stone column with a base and a capital, a crack, moss at the foot
def column(seed):
    r = rng(seed); im = blank(32, 88); d = ImageDraw.Draw(im); S = STONE
    d.rectangle([2, 76, 29, 87], fill=S[1]); d.rectangle([2, 76, 29, 78], fill=S[3]); d.line([(2, 87), (29, 87)], fill=S[0])        # the base block
    d.rectangle([5, 70, 26, 76], fill=S[2]); d.line([(5, 70), (26, 70)], fill=S[4])                                                    # the torus
    d.rectangle([8, 14, 23, 70], fill=S[2])                                                                                            # the shaft
    for x, c in ((8, 4), (11, 3), (14, 2), (17, 3), (20, 2), (23, 1)): d.line([(x, 14), (x, 70)], fill=S[c])                            # the flutes: lit left, dark right
    d.line([(9, 16), (9, 68)], fill=S[4])
    d.rectangle([4, 6, 27, 14], fill=S[3]); d.line([(4, 6), (27, 6)], fill=S[4]); d.line([(4, 14), (27, 14)], fill=S[0])               # the capital
    d.polygon([(2, 6), (29, 6), (24, 0), (7, 0)], fill=S[2]); d.line([(7, 0), (24, 0)], fill=S[4])                                     # the springing of the vault
    d.line([(15, 22), (17, 34), (14, 44), (18, 56)], fill=S[0])                                                                        # a crack
    for _ in range(26): d.point((r.randint(4, 27), r.randint(72, 86)), fill=hx("#3e4a2a"))                                              # moss
    return X2(im, (16, 86), (30, 6, 1, -1), tall=True)

# ---------------------------------------------------------------- altar: a stone table under a faded red cloth, a cracked cross, candle stubs
def altar(seed):
    r = rng(seed); im = blank(32, 50); d = ImageDraw.Draw(im); S = STONE
    d.rectangle([0, 20, 31, 31], fill=S[3]); d.line([(0, 20), (31, 20)], fill=S[4]); d.line([(0, 20), (0, 31)], fill=S[4])            # the slab top
    d.rectangle([0, 32, 31, 46], fill=S[2]); d.line([(0, 46), (31, 46)], fill=S[0]); d.line([(0, 33), (0, 45)], fill=S[3])             # the stone body
    d.rectangle([3, 20, 28, 40], fill=CLOTH[2]); d.line([(3, 20), (28, 20)], fill=CLOTH[3]); d.line([(3, 40), (28, 40)], fill=CLOTH[0])  # the altar cloth hanging over the front
    for x in (7, 15, 23): d.line([(x, 22), (x, 39)], fill=CLOTH[1])
    for _ in range(16): d.point((r.randint(3, 28), r.randint(30, 40)), fill=hx("#3a0c0c"))                                              # old stains
    d.rectangle([14, 6, 17, 20], fill=S[4]); d.rectangle([10, 10, 21, 12], fill=S[4]); d.line([(15, 8), (16, 18)], fill=S[0])         # a cracked stone cross
    for x in (5, 25): d.rectangle([x, 14, x + 2, 20], fill=WAX); d.point((x + 1, 13), fill=FLAME); d.point((x + 1, 12), fill=FLAME2)  # candle stubs
    return X2(im, (16, 48), (30, 5, 1, -1))

# ---------------------------------------------------------------- candelabra: a tall brass stand, five arms, lit candles
def candelabra(seed):
    r = rng(seed); im = blank(24, 64); d = ImageDraw.Draw(im); B = BRASS
    d.rectangle([11, 20, 12, 56], fill=B[3]); d.line([(11, 20), (11, 56)], fill=B[4]); d.line([(12, 22), (12, 56)], fill=B[1])        # the stem
    d.polygon([(4, 62), (19, 62), (16, 56), (7, 56)], fill=B[2]); d.line([(4, 62), (19, 62)], fill=B[0]); d.line([(7, 56), (16, 56)], fill=B[4])    # the foot
    d.rectangle([9, 38, 14, 40], fill=B[2]); d.rectangle([9, 26, 14, 27], fill=B[2])                                                  # collars
    for (x0, y0) in ((3, 14), (7, 10), (11, 8), (15, 10), (19, 14)):
        d.line([(11, 22), (x0 + 1, y0 + 4)], fill=B[3]); d.rectangle([x0 - 1, y0 + 4, x0 + 3, y0 + 5], fill=B[2])                      # an arm and its cup
        d.rectangle([x0, y0, x0 + 1, y0 + 3], fill=WAX); d.point((x0, y0 - 1), fill=FLAME); d.point((x0, y0 - 2), fill=FLAME2)
    return X2(im, (12, 62), (18, 4, 1, -1), tall=True)

# ---------------------------------------------------------------- statue: a weathered saint on a plinth, hands folded, the head chipped, the face worn away
def statue(seed):
    r = rng(seed); im = blank(32, 76); d = ImageDraw.Draw(im); S = STONE
    d.rectangle([3, 60, 28, 75], fill=S[2]); d.rectangle([3, 60, 28, 62], fill=S[4]); d.line([(3, 75), (28, 75)], fill=S[0]); d.line([(4, 63), (4, 74)], fill=S[3])   # the plinth
    d.polygon([(8, 16), (23, 16), (26, 60), (5, 60)], fill=S[3])                                                                       # the robe
    for x in (10, 14, 18, 22): d.line([(x, 18), (x - 1 + (x % 3), 59)], fill=S[1])                                                     # folds
    d.line([(9, 18), (6, 58)], fill=S[4])
    d.ellipse([11, 2, 20, 14], fill=S[3]); d.line([(12, 4), (13, 11)], fill=S[4]); d.point((15, 8), fill=S[1]); d.point((18, 8), fill=S[1])   # the head, the eyes worn to dots
    d.polygon([(11, 3), (14, 0), (20, 4), (17, 3)], fill=S[1])                                                                         # a chip out of the crown
    d.rectangle([12, 28, 19, 34], fill=S[2]); d.line([(12, 28), (19, 28)], fill=S[4])                                                  # the folded hands
    d.line([(21, 36), (23, 52)], fill=S[0]); d.line([(10, 40), (9, 56)], fill=S[0])
    for _ in range(24): d.point((r.randint(4, 27), r.randint(40, 74)), fill=hx("#3e4a2a"))                                              # moss
    return X2(im, (16, 74), (30, 6, 1, -1), tall=True)

# ---------------------------------------------------------------- font: a stone basin on a pedestal, dark water, a worn lip
def font(seed):
    im = blank(24, 44); d = ImageDraw.Draw(im); S = STONE
    d.rectangle([8, 22, 15, 38], fill=S[2]); d.line([(8, 22), (8, 38)], fill=S[4]); d.line([(15, 22), (15, 38)], fill=S[0])             # the pedestal
    d.rectangle([4, 38, 19, 43], fill=S[1]); d.line([(4, 38), (19, 38)], fill=S[3]); d.line([(4, 43), (19, 43)], fill=S[0])             # the foot
    d.ellipse([1, 10, 22, 26], fill=S[3]); d.ellipse([3, 12, 20, 22], fill=hx("#162224")); d.arc([1, 10, 22, 26], 200, 340, fill=S[4])  # the bowl, its dark water
    d.line([(6, 15), (11, 14)], fill=hx("#3a5a5c"))
    return X2(im, (12, 42), (22, 5, 1, -1))

# ---------------------------------------------------------------- confessional: a dark wood booth, a curtain, a carved lintel, a lattice window
def confessional(seed):
    r = rng(seed); im = blank(32, 80); d = ImageDraw.Draw(im); W = WOOD
    d.rectangle([0, 8, 31, 74], fill=W[1]); d.line([(0, 8), (0, 74)], fill=W[3]); d.line([(31, 8), (31, 74)], fill=W[0])                # the box
    d.polygon([(0, 8), (4, 0), (27, 0), (31, 8)], fill=W[3]); d.line([(4, 0), (27, 0)], fill=W[4]); d.line([(0, 8), (31, 8)], fill=W[0])   # the carved cornice
    for x in range(3, 30, 5): d.line([(x, 2), (x, 7)], fill=W[2])
    d.rectangle([4, 16, 27, 28], fill=hx("#101010"))                                                                                    # the window
    for x in range(6, 27, 3): d.line([(x, 16), (x, 28)], fill=W[2])
    for y in range(18, 28, 3): d.line([(4, y), (27, y)], fill=W[2])
    d.rectangle([5, 34, 26, 72], fill=CLOTH[1]); d.line([(5, 34), (26, 34)], fill=CLOTH[3])                                             # the curtain, drawn half back
    for x in (9, 13, 17, 21, 25): d.line([(x, 36), (x - 1 + (x % 2), 72)], fill=CLOTH[0])
    d.polygon([(5, 34), (14, 34), (5, 72)], fill=hx("#0c0606"))                                                                         # the dark inside, where the curtain parts
    d.line([(0, 74), (31, 74)], fill=W[0])
    grain(d, 0, 8, 31, 74, W[0], r, 24)
    return X2(im, (16, 78), (30, 5, 1, -1), tall=True)

# ---------------------------------------------------------------- tomb: a stone box with a carved lid (a recumbent figure), a cracked corner, a name worn smooth
def tomb(seed):
    r = rng(seed); im = blank(32, 40); d = ImageDraw.Draw(im); S = STONE
    d.rectangle([1, 12, 30, 22], fill=S[4]); d.line([(1, 12), (30, 12)], fill=S[4]); d.line([(1, 12), (1, 22)], fill=S[4])               # the lid
    d.ellipse([12, 14, 19, 20], fill=S[3]); d.rectangle([13, 18, 18, 22], fill=S[3]); d.line([(13, 22), (18, 22)], fill=S[1])           # a recumbent head and hands
    d.rectangle([0, 23, 31, 37], fill=S[2]); d.line([(0, 37), (31, 37)], fill=S[0]); d.line([(0, 24), (0, 36)], fill=S[3])              # the box
    for y in (27, 31): d.line([(4, y), (27, y)], fill=S[1])                                                                             # a worn inscription
    d.line([(24, 12), (26, 22), (23, 30)], fill=S[0])                                                                                   # a crack
    for _ in range(14): d.point((r.randint(2, 29), r.randint(26, 36)), fill=hx("#3e4a2a"))
    return X2(im, (16, 38), (30, 5, 1, -1))

NEW = {"pew_l": pew_l, "pew_m": pew_m, "pew_r": pew_r, "column": column, "altar": altar, "candelabra": candelabra, "statue": statue, "font": font, "confessional": confessional, "tomb": tomb}
VARIANTS = {k: 1 for k in NEW}

if __name__ == "__main__":
    # preview: a slice of the nave on a stone floor with a character for scale; a TILE IS 16 MAP PX (32 sprite px), drawn at 4x
    import ground, os, sys
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    T = 16; W_, H_ = T * 14, T * 9
    bg = blank(W_, H_); g = ground.GROUNDS["concrete"](3)
    for ty in range(0, H_, g.height):
        for tx in range(0, W_, g.width): bg.paste(g, (tx, ty))
    big = bg.resize((W_ * 2, H_ * 2), Image.NEAREST)               # 1 map px = 2 sprite px
    items = []
    def at(name, tx, ty): items.append((ty * T + T - 1, NEW[name](1000), tx * T + T // 2, ty * T + T))
    for r_ in (4, 6):
        for i, nm in enumerate(['pew_l', 'pew_m', 'pew_m', 'pew_m', 'pew_r']): at(nm, 1 + i, r_)
    at('column', 7, 4); at('column', 11, 4); at('altar', 8, 2); at('altar', 9, 2); at('candelabra', 7, 2); at('candelabra', 10, 2); at('statue', 12, 3); at('font', 12, 5); at('confessional', 12, 7); at('tomb', 8, 7); at('statue', 3, 2)
    for _, p, x, y in sorted(items, key=lambda t: t[0]):
        if p.shadow:
            w, h, dx, dy = p.shadow; w2, h2 = w * 2, h * 2
            big.alpha_composite(shadow_ellipse(w2, h2), (int(x * 2 + dx * 2 - w2 / 2), int(y * 2 + dy * 2 - h2 / 2)))
        big.alpha_composite(p.im, (int(x * 2 - p.pivot[0]), int(y * 2 - p.pivot[1])))
    big = big.resize((big.width * 3, big.height * 3), Image.NEAREST)
    big.convert('RGB').save('/tmp/cathedral_props_preview.png'); print(big.size)
