# Obelisk ritual blade: faction ceremonial kris. Dark pattern-welded wavy blade, raised asymmetric guard,
# dark wood pistol-hilt, red ferrule ring + red stone on the guard.
import math
from common import grid, fill, put
CAT = "melee"

# ---------------- A: 58x12 menu sprite
G = grid(58, 12)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# hilt: curved pistol-like grip, dark wood
f(6, 4, 10, "b"); f(7, 2, 10, "c"); f(8, 1, 10, "c"); f(9, 0, 10, "c"); f(10, 0, 6, "d"); f(11, 1, 3, "d")
f(9, 7, 10, "d"); G[6][3] = "b"; G[7][1] = "b"; G[8][0] = "b"; G[7][2] = "b"
for (x, y) in ((4, 8), (6, 7), (8, 8), (2, 9)): G[y][x] = "d"
f(6, 11, 12, "R"); f(7, 11, 12, "R"); f(8, 11, 12, "R"); f(9, 11, 12, "R"); G[6][12] = "y"     # red ferrule (selut), gold rim
f(10, 11, 12, "4")
# blade: wavy centre line, tapering half-width
for x in range(13, 58):
    c = 7.4 + 0.9 * math.sin(2 * math.pi * (x - 17) / 13) * min(1, (x - 13) / 6) + max(0, x - 48) * 0.12
    w = 2.3 - 1.8 * (x - 13) / 44
    t, b = round(c - w), round(c + w)
    for y in range(t, b + 1): G[y][x] = "3"
    G[t][x] = "2"; G[b][x] = "4"
    if b - t >= 3 and (x * 7) % 5 < 2: G[(t + b) // 2][x] = "2"        # pamor flecks
    if b - t >= 4 and (x * 3) % 4 == 0: G[(t + b) // 2 + 1][x] = "4"
# ganja (raised guard) at the blade root, spike forward, toothed underside
for y in range(0, 6):
    for x in range(13, 14 + y): G[y][x] = "4"
    G[y][13] = "3"; G[y][13 + y] = "2"
G[0][13] = "2"; G[0][12] = "2"; G[1][12] = "3"; G[2][12] = "3"
for y in range(6, 11): f(y, 13, 15, "4")
f(10, 13, 17, "5"); G[11][14] = "5"; G[11][16] = "5"; G[10][16] = "."
G[4][14] = "R"                                                           # red stone
G[7][58 - 1] = "."
A = ["".join(r) for r in G]

# ---------------- B: 21x7 in hand
COLS = {0: (3, "bccd"), 1: (3, "bccd"), 2: (3, "bcd"), 3: (3, "bcd"), 4: (3, "RRR"), 5: (0, "2R3334"), 6: (1, "23344"),
        7: (2, "2334"), 8: (2, "234"), 9: (2, "234"), 10: (3, "23"), 11: (3, "24"), 12: (3, "23"), 13: (2, "24"),
        14: (2, "23"), 15: (2, "24"), 16: (3, "23"), 17: (3, "24"), 18: (3, "23"), 19: (4, "2"), 20: (4, "1")}
_B = grid(21, 7)
for x, (y0, s) in COLS.items():
    for i, ch in enumerate(s): _B[y0 + i][x] = ch
_B[3][4] = "y"
B = ["".join(r) for r in _B]
GRIP = (2.0, 4.5); SUPPORT = None; MUZZLE = (21, 4.5)
