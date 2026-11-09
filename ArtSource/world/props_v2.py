# Props, second pass (Yan, 2026-11-09, quality pass: "tires, trash, jersey, brush, debris read weakest at game scale"). Same interface as props.py (Prop(im, pivot, shadow));
# NOT in the game atlas: `python3 props_v2.py <out.png>` writes a before/after on real ground tiles with a man for scale.
# Fixes: lifted the near-black rubber and bin-bag tones so the shapes read on dark ground, colour pops on the litter, a painted stripe on the barrier, a bigger fanned scrub, fatter rubble slabs.
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image, ImageDraw
from worldlib import *
import props
from props import Prop, finish

def line(im, x0, y0, x1, y1, c):
    n = max(abs(x1 - x0), abs(y1 - y0), 1)
    for i in range(n + 1): px(im, int(round(x0 + (x1 - x0) * i / n)), int(round(y0 + (y1 - y0) * i / n)), c)

def tires2(seed):
    """dumped tyres: a lopsided stack of three (lit tops, zig-zag tread, dark undersides, the top one open with its hole) and one stood on edge with the steel wheel showing"""
    r = rng(seed); T = ramp("#242627", "#383a3c", "#4f5254", "#787c7e"); im = blank(24, 17)
    def band(cx, ytop, rx, h):
        for y in range(ytop, ytop + h):
            for x in range(int(cx - rx), int(cx + rx) + 1):
                e = abs(x - cx) / rx
                if e > 0.93 and y in (ytop, ytop + h - 1): continue
                v = 3 if y == ytop else 0 if y == ytop + h - 1 else (1 if (x + y) % 2 else 2)
                if e > 0.8 and v == 2: v = 1
                if x == int(cx - rx) and 0 < y - ytop < h - 1: v = 3
                px(im, x, y, T[v])
    for k, off in enumerate((1, -1, 0)): band(8 + off, 13 - k * 4, 7, 4)
    cx, cy, rx, ry = 8, 4.5, 7, 3.2                                       # top tyre seen from above: tread ring, hole
    for y in range(0, 10):
        for x in range(0, 17):
            e = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
            if e <= 1:
                if e < 0.2: px(im, x, y, hx("#0e1012"))
                elif e < 0.36: px(im, x, y, T[1])
                else: px(im, x, y, T[3] if (y < cy and x < cx + 2) else T[2] if e < 0.75 else T[1])
    px(im, 6, 4, T[2]); px(im, 7, 3, T[3])
    ex, ey = 18.5, 10.5                                                   # the one on edge
    for y in range(0, 17):
        for x in range(12, 24):
            e = ((x - ex) / 4.2) ** 2 + ((y - ey) / 6.0) ** 2
            if e <= 1:
                if e < 0.32: px(im, x, y, P["steel"][3] if e > 0.1 else P["steel"][1])
                else: px(im, x, y, T[3] if x < ex - 1.5 and y < ey else T[2] if x < ex + 1 else T[1])
    px(im, 18, 10, hx("#0e1012")); px(im, 19, 10, hx("#0e1012"))
    return finish(im, (11, 16), shadow=(22, 5, 1, -1))

def trash2(seed):
    """dumped rubbish: two or three grey-sheened bin bags slumped together (tied necks, one split open), paper, a red can, a blue bottle, a green one"""
    r = rng(seed); im = blank(22, 14); B = ramp("#222529", "#363a40", "#555c64", "#848c94")
    bags = [(7, 7, 5, 4), (14, 8, 5.2, 3.8), (10, 10, 4.4, 3)][: r.randint(2, 3)]
    for (cx, cy, rx, ry) in bags:
        for y in range(int(cy - ry) - 2, int(cy + ry) + 2):
            for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
                e = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
                if e <= 1:
                    v = 3 if (e < 0.5 and x < cx and y < cy) else 2 if (x < cx + 1 and y < cy + 1) else 1 if y < cy + ry * 0.5 else 0
                    px(im, x, y, B[v])
        px(im, int(cx), int(cy - ry) - 1, B[1]); px(im, int(cx) + 1, int(cy - ry) - 2, B[2])   # the tied neck
    for _ in range(6): px(im, r.randrange(3, 19), r.randrange(8, 13), hx("#d8d2b8"))              # paper
    for x, y in ((3, 11), (4, 11), (3, 12), (4, 12)): px(im, x, y, hx("#b03c30"))                 # a can
    px(im, 3, 11, hx("#e07a60"))
    for x, y in ((18, 11), (19, 11), (20, 12)): px(im, x, y, hx("#3f78a8"))                       # a bottle
    px(im, 17, 12, hx("#5a9a58")); px(im, 16, 12, hx("#5a9a58"))
    return finish(im, (11, 13), shadow=(21, 5, 1, -1))

def jersey2(seed):
    """a precast road barrier, 3/4: the lit top face, a short upright band with a faded yellow warning stripe, the sloped band, the foot with its lifting slot, rain streaks"""
    c = P["concrete"]; r = rng(seed); W = 22; im = blank(W, 14)
    for y in range(0, 3):                                                 # top face
        for x in range(3 + (2 - y) // 2 * 0, W - 3):
            px(im, x, y, c[4] if y < 2 else c[3])
    for y in range(3, 14):
        inset = 2 if y < 6 else 2 - (y - 5) // 2 if y < 10 else 0
        inset = max(0, inset)
        v = 3 if y < 6 else 2 if y < 8 else 1 if y < 10 else 2 if y < 13 else 0
        for x in range(inset, W - inset):
            px(im, x, y, c[v] if x > inset else c[min(4, v + 1)])
    for x in range(2, W - 2): px(im, x, 6, c[1])                          # crease between upright and slope
    for x in range(1, W - 1): px(im, x, 10, c[0] if x % 3 else c[1])      # crease above the foot
    y0 = 3                                                                # the warning stripe: diagonal yellow ticks on the upright band
    for x in range(4, 15):
        if (x // 2) % 2 == 0:
            for y in range(y0, y0 + 3): px(im, x + (y - y0) // 2 * 0, y, hx("#a58a3a") if (x + y) % 3 else hx("#c9a63e"))
    for x in range(8, 13): px(im, x, 11, c[0]); px(im, x, 12, c[0])      # the lifting slot
    for _ in range(4):
        x = r.randrange(3, W - 3)
        for y in range(7, r.randint(9, 11)): px(im, x, y, c[0])
    if seed % 2: px(im, W - 3, 0, (0, 0, 0, 0)); px(im, W - 4, 0, (0, 0, 0, 0)); px(im, W - 3, 1, (0, 0, 0, 0))
    for _ in range(3): px(im, r.randrange(2, W - 2), r.randrange(10, 13), P["moss"][0])
    return finish(im, (W // 2, 13), shadow=(W, 4, 1, -1))

def brush2(seed):
    """a compact clump of dead scrub: woody stems fanned from one root, bleached straw tips, live green blades at the foot"""
    r = rng(seed); im = blank(22, 16); W = P["wood"]; D = P["drygrass"]
    for k in range(15):
        x0 = 7 + r.randrange(8); ang = (k - 7) / 7.0 * 0.7 + r.uniform(-0.12, 0.12); ln = r.randint(6, 11)
        x1 = x0 + ang * ln * 0.9; y1 = 15 - ln
        n = int(ln)
        for i in range(n + 1):
            t = i / n; x = x0 + (x1 - x0) * t; y = 15 - (15 - y1) * t
            px(im, int(round(x)), int(round(y)), W[2] if t < 0.35 else W[3] if t < 0.65 else D[2] if t < 0.9 else hx("#c4b684"))
        if r.random() < .5: px(im, int(round(x1)) + r.choice((-1, 1)), int(round(y1)) + 1, D[3])      # a side twig
    for _ in range(14): px(im, r.randrange(4, 18), r.randrange(12, 16), P["grass"][r.randint(2, 4)])
    return finish(im, (11, 15), shadow=(16, 4, 1, -1))

def debris2(seed):
    """a heap of demolition rubble: fat tilted concrete slabs (lit top edge, shaded face, a broken corner), rebar sticking out rusty and bent, a snapped plank, rubble and pale dust at the foot"""
    r = rng(seed); c = P["concrete"]; im = blank(26, 16); R = P["rust"]
    for _ in range(18):
        x, y = r.randrange(1, 25), r.randrange(11, 16)
        px(im, x, y, r.choice((c[2], c[3], c[4], c[1])))
    slabs = [(1, 7, 11, 6, 1), (9, 4, 10, 6, -1), (14, 8, 10, 5, 1), (5, 9, 8, 5, 0)]
    d = ImageDraw.Draw(im)
    for (x0, y0, w, th, tilt) in slabs:
        top = [(x0, y0 + tilt * 2), (x0 + w, y0 - tilt * 2), (x0 + w, y0 - tilt * 2 + 2), (x0, y0 + tilt * 2 + 2)]
        d.polygon(top, fill=c[4])
        for i in range(w):
            yy = y0 + tilt * 2 - (tilt * 4 * i) // w + 2
            for j in range(th - 2): px(im, x0 + i, yy + j, c[2] if j < th - 3 else c[1])
            px(im, x0 + i, yy + th - 2, c[0])
        px(im, x0 + r.randrange(2, w - 2), y0 + 3, c[0])                                              # a pit
        px(im, x0, y0 + tilt * 2 + 3, (0, 0, 0, 0)); px(im, x0 + w - 1, y0 - tilt * 2 + 3, (0, 0, 0, 0))   # chipped corners
    for k in range(3):
        x, y = r.randrange(4, 21), r.randrange(4, 8)
        for i in range(r.randint(5, 8)): px(im, x + (i // 3) * r.choice((1, -1)), y - i + 3, R[1 + (i % 2)] if i < 6 else R[3])
    for i in range(8): px(im, 16 + i, 11 - i // 3, P["wood"][3] if i < 7 else P["wood"][2]); px(im, 16 + i, 12 - i // 3, P["wood"][1])
    for _ in range(6): px(im, r.randrange(2, 24), r.randrange(12, 15), hx("#a8a496"))                 # dust
    return finish(im, (13, 15), shadow=(24, 5, 1, -1))

NEW = {"tires": tires2, "trash": trash2, "jersey": jersey2, "brush": brush2, "debris": debris2}

def place(canvas, p, x, y):
    if p.shadow:
        w, h, dx, dy = p.shadow; canvas.alpha_composite(shadow_ellipse(w, h), (int(x + dx - w / 2), int(y + dy - h / 2)))
    canvas.alpha_composite(p.im, (int(x - p.pivot[0]), int(y - p.pivot[1])))

def sheet(out, scale=6):
    sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
    import ground, make_armor_avatars as av
    a = [a for a in av.ARMORS if a[0] == "Raider Jacket"][0]; man = av.build(a[0], a[3], a[4]).convert("RGBA")
    names = list(NEW); cw = 34; W = 24 + len(names) * 2 * cw; RH = 44
    bg = blank(W, RH * 2); gr = ground.GROUNDS["grass"](3); dt = ground.GROUNDS["dirt"](3)
    for ty in range(0, RH * 2, 64):
        for tx in range(0, W, 64): bg.paste(gr, (tx, ty))
    for ty in range(RH, RH * 2, 64):
        for tx in range(0, W, 64): bg.paste(dt, (tx, ty))
    for k, n in enumerate(names):
        for row in range(2):
            for j, fn in enumerate((props.OLD_PROPS[n], NEW[n])):
                place(bg, fn(1000), 36 + (k * 2 + j) * cw // 1 - 6, 34 + row * RH)
    bg.alpha_composite(man, (4, 20)); bg.alpha_composite(man, (4, 20 + RH))
    bg.resize((bg.width * scale, bg.height * scale), Image.NEAREST).convert("RGB").save(out); print(out, bg.size)

if __name__ == "__main__": sheet(sys.argv[1] if len(sys.argv) > 1 else "/tmp/props_v2.png")
