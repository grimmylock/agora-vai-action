# The Verger's scythe (2026-11-20; BOSS_ROSTER #14, ArtSource/make_verger_concept.py): menu sprite (64x32) and in-hand sprite (32x16) in the melee set's own letter-grid format, built from polygons with
# a light from the top-left and written to Assets/Resources/Sprites/Weapons/weapon_VergerScythe(.png|_hd.png). Snath left, the blade rises from its right end and sweeps forward, edge on the inside of the curve. python3 ArtSource/weapons/hand/verger_axe.py
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
def bez(p0, p1, p2, t): return ((1-t)**2*p0[0]+2*(1-t)*t*p1[0]+t*t*p2[0], (1-t)**2*p0[1]+2*(1-t)*t*p1[1]+t*t*p2[1])
def build(W, H, haft_y, haft_th, haft_x1, wrap, ring_x, p0, p1, p2, w0, cap, nib, blood):
    g = [["."] * W for _ in range(H)]
    for t in range(haft_th):                                                                   # the snath: lit top row, dark bottom row
        ch = "dcba"[min(3, t * 4 // haft_th)] if haft_th > 1 else "c"
        for x in range(cap, haft_x1): g[haft_y + t][x] = ch
    for x in range(wrap[0], wrap[1]):                                                          # the leather wrap
        for t in range(haft_th): g[haft_y + t][x] = "l" if (x + t) % 3 == 0 else ("n" if t < haft_th // 2 else "m")
    for t in range(haft_th): g[haft_y + t][ring_x] = "Y" if t < haft_th // 2 else "y"        # a brass ring
    for x in range(0, cap):
        for t in range(haft_th): g[haft_y + t][x] = "3" if t < haft_th // 2 else "2"          # the steel butt cap
    nx, ny, nl = nib                                                                           # the scythe's second grip: a short peg under the snath
    for k in range(nl):
        for dx in (0, 1):
            if haft_y + haft_th + k < H: g[haft_y + haft_th + k][nx + dx] = "c" if dx == 0 else "b"
    L, R = [], []                                                                              # the blade: a tapering strip along a bezier, the inside (right, below) is the edge
    N = 60
    for i in range(N + 1):
        t = i / N; x, y = bez(p0, p1, p2, t); x2, y2 = bez(p0, p1, p2, min(1, t + 0.01)); x1, y1 = bez(p0, p1, p2, max(0, t - 0.01))
        dx, dy = x2 - x1, y2 - y1; d = math.hypot(dx, dy) or 1; nxv, nyv = dy / d, -dx / d      # the normal pointing to the inside of the curve
        w = w0 * (1 - t) ** 0.75 + 0.4
        L.append((x - nxv * w * 0.35, y - nyv * w * 0.35)); R.append((x + nxv * w * 0.65, y + nyv * w * 0.65))
    poly = L + R[::-1]
    xs = [p[0] for p in poly]; ys = [p[1] for p in poly]
    for y in range(H):
        for x in range(W):
            if not inside(poly, x + 0.5, y + 0.5): continue
            v = 0.55 - 0.3 * (y / H) - 0.25 * (x / W) + 0.18
            g[y][x] = "5" if v > 0.78 else "4" if v > 0.56 else "3" if v > 0.38 else "2" if v > 0.2 else "1"
    inn = lambda x, y: 0 <= x < W and 0 <= y < H and inside(poly, x + 0.5, y + 0.5)
    for y in range(H):
        for x in range(W):
            if inn(x, y) and not (inn(x + 1, y) and inn(x, y + 1)) and g[y][x] in "12345": g[y][x] = "5"      # the bright cutting edge on the inside
    for y in range(H):
        for x in range(W):
            if inn(x, y) and not inn(x - 1, y - 1) and g[y][x] in "345": g[y][x] = "2"                         # a dark spine on the outside
    for (x, y, c) in blood:
        if 0 <= y < H and 0 <= x < W and g[y][x] in "12345": g[y][x] = c
    for t in range(haft_th + 2):                                                                # the socket collar where the blade meets the snath
        for x in range(int(p0[0]) - 3, int(p0[0]) + 3):
            if 0 <= haft_y - 1 + t < H: g[haft_y - 1 + t][x] = ("4" if t == 0 else "3" if t < (haft_th + 2) // 2 else "2" if t < haft_th + 1 else "1")
    return ["".join(r) for r in g]
MENU = build(62, 30, 22, 4, 44, (10, 27), 29, (46.5, 24), (46, 5), (61, 1.5), 8, 3, (28, 0, 4),
             [(58, 4, "R"), (55, 6, "r"), (50, 12, "R"), (49, 18, "r")])
HAND = build(30, 14, 10, 2, 21, (5, 12), 14, (22.5, 11), (22, 2), (29.5, 0.8), 4, 2, (13, 0, 2),
             [(28, 1, "R"), (25, 3, "r")])
if __name__ == "__main__":
    mp.paint(HAND, PAL, 32, 16).save(os.path.join(mp.WEAP, "weapon_VergerScythe.png")); mp.paint(MENU, PAL, 64, 32).save(os.path.join(mp.WEAP, "weapon_VergerScythe_hd.png"))
