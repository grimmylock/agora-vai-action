# Flat looks drawn into the ground layer (Batch 30): line autotiles (rail, roadline, trench, fence
# shadows are props) as 16x16 NESW-mask tiles, plus single-cell flats (platform, tallgrass...).
import numpy as np
from PIL import Image
from worldlib import *
import ground

C = 16

def line_tile(kind, mask):
    im = blank(C, C); a = 5; b = 11
    def band(fn):
        for y in range(C):
            for x in range(C):
                inx = a <= x < b; iny = a <= y < b
                on = (inx and iny) or (inx and (y < a and mask & 1 or y >= b and mask & 4)) or (iny and (x < a and mask & 8 or x >= b and mask & 2))
                if on: c = fn(x, y)
                else: continue
                if c: px(im, x, y, c)
    if kind == "rail":
        horiz = bool(mask & 10) or not (mask & 5)
        for y in range(C):
            for x in range(C):
                u, v = (x, y) if horiz else (y, x)                    # u along the track, v across
                if v in (3, 12): c = P["steel"][3] if v == 3 else P["steel"][2]
                elif 2 <= v <= 13 and u % 4 in (0, 1): c = P["wood"][1] if u % 4 == 0 else P["wood"][2]   # sleepers
                elif 1 <= v <= 14: c = P["gravel"][1] if (u * 7 + v) % 5 else P["gravel"][2]
                else: continue
                px(im, x, y, c)
        return im
    if kind == "roadline":
        horiz = bool(mask & 10) or not (mask & 5)
        for k in range(C):
            if k % 8 < 5:
                for w in (7, 8):
                    x, y = (k, w) if horiz else (w, k)
                    px(im, x, y, hx("#b8ac7a", 190))
        return im
    if kind == "trench":
        a, b = 4, 12
        band(lambda x, y: P["mud"][1])
        arr = np.array(im)
        al = arr[..., 3] > 0
        for y in range(C):
            for x in range(C):
                if not al[y, x]: continue
                up = y > 0 and not al[y - 1, x]
                dn = y < C - 1 and not al[y + 1, x]
                if up: arr[y, x] = P["dirt"][3]                         # lit far lip
                elif y > 0 and al[y - 1, x] and (y < 2 or not al[y - 2, x]): arr[y, x] = P["mud"][0]   # shadowed inner wall
                if dn: arr[y, x] = P["dirt"][2]
        return Image.fromarray(arr, "RGBA")
    raise ValueError(kind)

def platform(seed):
    im = blank(C, C); r = rng(seed)
    for y in range(C):
        for x in range(C):
            c = P["wood"][3] if y % 5 else P["wood"][1]
            if x == 0: c = P["wood"][4]
            if r.random() < .05: c = P["wood"][2]
            px(im, x, y, c)
    return im

def boardwalk(mask, seed):
    """Batch 33: marsh causeway deck (the swamp concept's boardwalk). Weathered planks 4 px wide run across
    the walk with dark gaps; mask = NESW neighbours that are deck too. An open side shows the deck's
    edge, and the south side its front face and a post every other tile."""
    r = rng(seed + mask); im = blank(C, C); ew = bool(mask & 10) or not (mask & 5)
    W = ramp("#2e2318", "#3f3122", "#54412d", "#6a533a")             # concept wood: darker, greyer than furniture
    for y in range(C):
        for x in range(C):
            u, v = (x, y) if ew else (y, x)                             # u along the walk, v across
            plank = u // 4; k = u % 4
            c = W[0] if k == 3 else W[3] if k == 0 else W[2]
            if (plank * 7 + seed) % 5 == 0 and k != 3: c = W[1]         # an older board
            if k != 3 and (v + plank * 5) % 11 == 0: c = W[1]           # board ends / nail lines
            if r.random() < .04: c = P["moss"][r.randrange(2)]
            im.putpixel((x, y), c)
    for bit, edge in ((1, lambda i: (i, 0)), (4, lambda i: (i, C - 1)), (8, lambda i: (0, i)), (2, lambda i: (C - 1, i))):
        if mask & bit: continue
        for i in range(C): im.putpixel(edge(i), W[0])
        if bit == 4:
            for i in range(C): im.putpixel((i, C - 2), W[1]); im.putpixel((i, C - 3), W[1] if i % 4 == 3 else W[2])
            if seed % 2 == 0 or True:
                for yy in range(C - 3, C): im.putpixel((6, yy), W[0]); im.putpixel((7, yy), W[1])    # a post
    return im

def tallgrass(seed, pal=None):
    pal = pal or P["grass"]; r = rng(seed); im = blank(14, 12)
    for k in range(r.randint(7, 10)):
        x = r.randrange(1, 13); h = r.randint(5, 10); lean = r.choice((-1, 0, 0, 1))
        for y in range(h):
            xx = x + (lean if y > h * .6 else 0)
            px(im, xx, 11 - y, pal[4] if y > h - 3 else pal[3] if y > h // 3 else pal[2])
    return outline(im, (*P["grass"][0][:3], 255))

def door_open(seed):
    """threshold and the door leaf swung inward against the frame"""
    im = blank(C, C)
    for x in range(2, 14): px(im, x, 14, P["wood"][1]); px(im, x, 15, P["wood"][2])
    for y in range(2, 13): px(im, 2, y, P["wood"][3]); px(im, 3, y, P["wood"][2])
    return im

def door_broken(seed):
    r = rng(seed); im = door_open(seed)
    for _ in range(9): px(im, r.randrange(3, 14), r.randrange(4, 15), r.choice((P["wood"][2], P["wood"][1], P["wood"][3])))
    return im

def all_flats():
    t = {}
    import zone_style
    for kind in ("rail", "roadline", "trench"):
        for m in range(16): t[f"f_{kind}_{m}"] = line_tile(kind, m)
    for v in range(2): t[f"f_platform_{v}"] = platform(v)
    for m in range(16): t[f"f_boardwalk_{m}"] = boardwalk(m, 5)
    for v in range(3): t[f"d_lily_{v}"] = zone_style.lily(50 + v)
    import zone_style                                  # Batch 31: concept-A grass clumps
    for v in range(6): t[f"d_tallgrass_{v}"] = zone_style.grass(30 + v)
    for v in range(4): t[f"d_tallgrass_dry_{v}"] = zone_style.grass(40 + v, dry=True)
    t["f_door_open_0"] = door_open(0); t["f_door_broken_0"] = door_broken(1)
    for name, fn in ground.DECALS.items():
        for v in range(3): t[f"d_{name}_{v}"] = fn(100 + v * 13)
    return t

if __name__ == "__main__":
    t = all_flats(); print(len(t))
