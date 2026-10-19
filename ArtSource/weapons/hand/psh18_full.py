# PSh-18 menu sprite option 2: fills the 64x32 canvas like the RK-12 (44x30), over trace_PSh18_44.png.
from common import *
G = grid(44, 30)
f = lambda y, a, b, c: fill(G, y, a, b, c)

# --- slide: lit top edge, long specular streak, rear serrations, dark underside, sights
put(G, 0, 9, "33"); put(G, 0, 40, "3")
f(1, 5, 42, "2"); G[1][5] = G[1][42] = "3"
f(2, 4, 43, "2"); f(2, 16, 37, "1"); G[2][4] = "3"
f(3, 4, 43, "3"); f(4, 4, 43, "4"); f(5, 4, 43, "5")
for y in (2, 3, 4): G[y][43] = "4"
for x in (7, 9, 11, 13):
    for y in (2, 3, 4): G[y][x] = "5"
put(G, 4, 2, "34"); put(G, 5, 2, "34"); put(G, 6, 2, "4")           # hammer

# --- frame: lit seam under the slide, dust cover, rear tang
f(6, 3, 42, "2"); f(6, 7, 11, "1"); f(7, 3, 42, "4"); f(8, 4, 31, "4"); f(8, 32, 41, "5")
G[6][43] = "4"; G[7][43] = "5"; G[7][3] = "3"

# --- grip: raked, backstrap lit, front strap dark, checkered brown panel, screw, magazine base
for y in range(9, 30):
    a = max(1, round(7 - (y - 8) * 0.33)); b = 19 if y < 15 else round(19 - (y - 15) * 0.31)
    if y >= 27: a = 1
    f(y, a, b, "4"); G[y][a] = "3"; G[y][b] = "5"
    if 12 <= y <= 26:
        pa, pb = a + 2, b - 2
        if y == 12: pa, pb = a + 4, b - 3
        for x in range(pa, pb + 1):
            G[y][x] = "d" if (x + y) % 4 == 0 or (x - y) % 4 == 0 else "c"   # diamond checkering
        G[y][pa] = "b" if y > 12 else G[y][pa]
        if y in (12, 13): f(y, pa, pb, "b")
G[19][11] = "1"; G[19][12] = "2"                                    # grip screw
f(27, 1, 15, "4"); f(28, 1, 15, "3"); f(29, 2, 14, "5"); G[28][0] = "3"; G[29][0] = "3"

# --- trigger guard: 2px steel loop, joins the grip front
for y in range(8, 13): put(G, y, 32, "34")
put(G, 13, 31, "334"); put(G, 14, 21, "3" * 11 + "4"); f(15, 20, 30, "4")
for y in range(12, 15): put(G, y, 19, "445")
# trigger: curved blade, pivot at the top, tip pointing forward
for (y, x, c) in ((9, 23, "3"), (9, 24, "3"), (10, 24, "2"), (11, 24, "2"), (11, 25, "3"), (12, 25, "2"), (12, 26, "2"), (12, 27, "3")):
    G[y][x] = c
G[9][31] = "4"; G[13][30] = "4"; G[13][22] = "4"                    # round the loop's inner corners

A = place(G, 64, 32)
