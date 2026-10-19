# Halden AM-50 Sledge (Barrett M82A1 pattern). The reference photo is tilted, so this is a clean level
# profile: slab-sided steel upper/lower receiver with rubber butt pad and rear monopod grip, top rail with a
# big scope, barrel shroud with oval lightening holes, folded bipod, 10-rd mag ahead of the pistol grip,
# heavy barrel and the double-chamber muzzle brake.
from common import grid, fill, put
CAT = "sniper"

# ---------------- A: 62x15 menu sprite
G = grid(62, 15)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# receiver body: butt (x0-9), upper/lower (x10-30), shroud (x31-42)
for x in range(0, 43):
    bot = 12 if x < 2 else 11 if x < 10 else 10 if x < 31 else 7
    for y in range(4, bot + 1): G[y][x] = "4"
    G[4][x] = "2"; G[5][x] = "3"; G[bot][x] = "5"
    if bot > 8: G[7][x] = "5"; G[8][x] = "3"
for y in range(4, 13): G[y][0] = "q"; G[y][1] = "q" if y > 4 else "p"
G[4][0] = "p"; G[12][0] = G[12][1] = "r"
f(4, 14, 22, "1")
for x in (32, 33, 36, 37, 40, 41): G[5][x] = "5"; G[6][x] = "5"   # lightening holes
for y in range(4, 8): G[y][42] = "4"
G[4][42] = "3"
put(G, 9, 12, "2"); put(G, 9, 27, "2")                    # pins
put(G, 12, 4, "qq"); put(G, 13, 3, "qr"); put(G, 14, 2, "rr")   # rear monopod grip
# top rail + scope
for x in range(12, 31): G[3][x] = "2" if x % 2 else "3"
for x in (13, 14, 15, 25, 26, 27, 28, 29): G[0][x] = "p"; G[1][x] = "q"; G[2][x] = "r"
f(1, 16, 24, "3"); f(2, 16, 24, "4"); f(1, 17, 20, "2"); f(0, 19, 21, "q")
put(G, 0, 30, "k"); put(G, 1, 30, "l"); put(G, 2, 30, "L")
G[3][17] = G[3][23] = "5"
# pistol grip, trigger + guard, magazine
GR = {11: (17, 20), 12: (17, 20), 13: (16, 19), 14: (16, 19)}
for y, (a, b) in GR.items(): f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "r"
G[14][19] = "."; G[13][18] = "r"
put(G, 11, 22, "2"); G[11][24] = "4"; f(12, 21, 24, "4")
for y in range(11, 15): f(y, 25, 30, "4"); G[y][25] = "3"; G[y][30] = "5"
f(14, 25, 30, "5"); G[12][27] = G[12][28] = "5"
# heavy barrel + double-chamber brake, folded bipod
f(5, 43, 55, "2"); f(6, 43, 55, "4"); f(5, 43, 47, "1")
put(G, 3, 56, "333.33"); put(G, 4, 56, "212.12"); put(G, 5, 56, "323.32"); put(G, 6, 56, "434.43")
put(G, 7, 56, "434.43"); put(G, 8, 56, "555.55")
f(8, 32, 44, "4"); put(G, 8, 45, "3"); G[8][31] = "5"
A = ["".join(r) for r in G]

# ---------------- B: 46x12 in hand
H = grid(46, 12)
h = lambda y, a, b, c: fill(H, y, a, b, c)
for x in range(0, 32):
    bot = 9 if x < 7 else 8 if x < 22 else 6
    for y in range(4, bot + 1): H[y][x] = "4"
    H[4][x] = "2"; H[bot][x] = "5"
    if bot > 6: H[6][x] = "5"; H[7][x] = "3"
for y in range(4, 10): H[y][0] = "q"
H[4][0] = "p"; h(4, 9, 14, "1")
for x in (24, 27, 30): H[5][x] = "5"
put(H, 10, 2, "qq"); put(H, 11, 1, "rr")
for x in range(9, 23): H[3][x] = "2" if x % 2 else "3"
for x in (9, 10, 18, 19, 20, 21): H[0][x] = "p"; H[1][x] = "q"; H[2][x] = "r"
h(1, 11, 17, "3"); h(2, 11, 17, "4"); h(1, 12, 14, "2")
put(H, 0, 22, "k"); put(H, 1, 22, "l"); put(H, 2, 22, "L")
for y, (a, b) in {9: (12, 14), 10: (11, 13), 11: (11, 13)}.items(): h(y, a, b, "q"); H[y][a] = "p"; H[y][b] = "r"
put(H, 9, 16, "2"); h(10, 15, 17, "4"); H[9][17] = "4"
for y in range(9, 12): h(y, 18, 21, "4"); H[y][18] = "3"; H[y][21] = "5"
h(11, 18, 21, "5")
h(4, 32, 40, "2"); h(5, 32, 40, "4")
put(H, 3, 41, "33.33"); put(H, 4, 41, "21.12"); put(H, 5, 41, "43.34"); put(H, 6, 41, "55.55")
h(7, 23, 31, "4")
B = ["".join(r) for r in H]
GRIP = (12.5, 10.0); SUPPORT = (27.0, 8.0); MUZZLE = (46, 5.0)
