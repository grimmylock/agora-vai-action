# The Verger's greataxe (2026-11-20; BOSS_ROSTER #14, ArtSource/make_verger_concept.py): menu sprite (64x32) and in-hand sprite (32x16) in the melee set's own letter-grid format, built from polygons with
# a light from the top-left and written to Assets/Resources/Sprites/Weapons/weapon_VergerGreataxe(.png|_hd.png). Handle left, head right, edge on the right. python3 ArtSource/weapons/hand/verger_axe.py
import os, sys, math
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import melee_px as mp
PAL = {"a": "#2a1a10", "b": "#44291a", "c": "#5e3a24", "d": "#7a4d30", "e": "#946440", "l": "#241810", "m": "#3a2618", "n": "#553824",
       "1": "#1e2226", "2": "#343b42", "3": "#566068", "4": "#8a969e", "5": "#c6d0d6", "y": "#946a1c", "Y": "#c8a034", "r": "#5e1016", "R": "#9a1a22", "K": "#141617"}
def inside(pts, x, y):
    c = False; n = len(pts)
    for i in range(n):
        (x0, y0), (x1, y1) = pts[i], pts[(i + 1) % n]
        if (y0 > y) != (y1 > y) and x < (x1 - x0) * (y - y0) / (y1 - y0) + x0: c = not c
    return c
def build(W, H, haft_y, haft_th, haft_x1, wrap, ring_x, head, spike, socket, cap, serr, blood, engr):
    g = [["."] * W for _ in range(H)]
    for t in range(haft_th):                                                                   # the haft: lit top row, dark bottom row
        ch = "dcba"[min(3, t * 4 // haft_th)] if haft_th > 1 else "c"
        for x in range(cap, haft_x1): g[haft_y + t][x] = ch
    for x in range(wrap[0], wrap[1]):                                                          # the leather wrap with diagonal turns
        for t in range(haft_th):
            g[haft_y + t][x] = "l" if (x + t) % 3 == 0 else ("n" if t < haft_th // 2 else "m")
    for t in range(haft_th): g[haft_y + t][ring_x] = "Y" if t < haft_th // 2 else "y"        # the brass ring
    for x in range(0, cap):                                                                    # the steel butt cap
        for t in range(haft_th): g[haft_y + t][x] = "3" if t < haft_th // 2 else "2"
    xs = [p[0] for p in head]; ys = [p[1] for p in head]; x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if not inside(head, x + 0.5, y + 0.5): continue
            v = 0.62 - 0.34 * (y - y0) / max(1, y1 - y0) - 0.3 * (x - x0) / max(1, x1 - x0) + 0.1                  # light from the top-left
            ch = "5" if v > 0.78 else "4" if v > 0.58 else "3" if v > 0.38 else "2" if v > 0.2 else "1"
            g[y][x] = ch
    for y in range(y0, y1 + 1):                                                                # the cutting edge: the right-most steel pixel of each row is bright, a few notched
        row = [x for x in range(x0, x1 + 1) if g[y][x] in "12345"]
        if row:
            xr = max(row); g[y][xr] = "5"
            if (y - y0) % serr == 0 and xr - 1 >= x0: g[y][xr] = "1"
    for (xa, ya, xb, yb) in engr:
        n = max(abs(xb - xa), abs(yb - ya), 1)
        for i in range(n + 1):
            x = round(xa + (xb - xa) * i / n); y = round(ya + (yb - ya) * i / n)
            if g[y][x] in "345": g[y][x] = "1"
    for (x, y, c) in blood:
        if 0 <= y < H and 0 <= x < W and g[y][x] in "12345": g[y][x] = c
    for y in range(H):                                                                         # the back spike and the socket
        for x in range(W):
            if inside(spike, x + 0.5, y + 0.5) and g[y][x] == ".": g[y][x] = "3" if y <= haft_y else "2"
    sx0, sx1 = socket
    for t in range(haft_th + 2):
        for x in range(sx0, sx1): g[haft_y - 1 + t][x] = ("4" if t == 0 else "3" if t < (haft_th + 2) // 2 else "2" if t < haft_th + 1 else "1")
    return ["".join(r) for r in g]
MENU = build(62, 30, 13, 4, 44, (10, 27), 29,
             head=[(45, 9), (46, 5), (43, 2), (48, 0), (55, 0), (60, 3), (61, 9), (61, 20), (60, 26), (55, 29), (48, 29), (43, 27), (46, 23), (45, 18)],
             spike=[], socket=(42, 48), cap=3, serr=3,
             blood=[(58, 8, "R"), (59, 9, "r"), (60, 14, "R"), (57, 20, "r"), (53, 25, "R"), (52, 27, "r")], engr=[(50, 5, 57, 13), (49, 12, 55, 18)])
HAND = build(30, 14, 6, 2, 19, (5, 12), 14,
             head=[(21, 5), (21, 3), (18, 1), (22, 0), (26, 0), (29, 3), (29, 10), (27, 13), (22, 13), (18, 12), (21, 10), (21, 8)],
             spike=[], socket=(18, 22), cap=2, serr=3,
             blood=[(27, 4, "R"), (28, 6, "r"), (26, 10, "R")], engr=[(22, 2, 26, 6)])
if __name__ == "__main__":
    mp.paint(HAND, PAL, 32, 16).save(os.path.join(mp.WEAP, "weapon_VergerGreataxe.png")); mp.paint(MENU, PAL, 64, 32).save(os.path.join(mp.WEAP, "weapon_VergerGreataxe_hd.png"))
    from PIL import Image
    m = mp.paint(MENU, PAL, 64, 32); h = mp.paint(HAND, PAL, 32, 16); bg = Image.new("RGBA", (64 * 6 + 32 * 6 + 60, 32 * 6 + 20), (46, 42, 34, 255))
    bg.alpha_composite(m.resize((64 * 6, 32 * 6), Image.NEAREST), (10, 10)); bg.alpha_composite(h.resize((32 * 6, 16 * 6), Image.NEAREST), (64 * 6 + 40, 10)); bg.convert("RGB").save("/tmp/verger_axe_sprite.png"); print("ok", len(MENU[0]), len(MENU), len(HAND[0]), len(HAND))
