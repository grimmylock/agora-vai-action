# SP-14 Warden (SPAS-12-pattern): skeleton stock folded over the receiver with the arm hook up,
# black alloy receiver, raked polymer pistol grip, vented heat shield, big ribbed pump fore-end,
# barrel over magazine tube with a tall front sight.
from common import grid, fill, put
CAT = "shotgun"

# ---------------- A: 58x24 menu sprite
G = grid(58, 24)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# folded stock: hook arc, butt plate, skeleton arm with lightening holes
for (x, y) in ((16, 1), (17, 0), (18, 0), (19, 0), (20, 0), (21, 0), (22, 1), (15, 2), (14, 3), (14, 4), (14, 5), (14, 6)):
    G[y][x] = "3"
for (x, y) in ((17, 0), (18, 0), (19, 0), (20, 0)): G[y][x] = "2"
G[1][16] = "2"; G[2][15] = "2"; G[6][14] = "4"; G[7][14] = "4"
for y in range(1, 11): f(y, 21, 23, "4")
for y in range(1, 11): G[y][21] = "3"
G[1][21] = "2"; G[1][23] = "."; G[10][23] = "5"
for y in range(8, 11): f(y, 4, 22, "3")
f(8, 10, 22, "2"); f(10, 4, 22, "4"); f(8, 4, 9, "."); G[9][4] = "."; G[9][5] = "2"; f(9, 6, 9, "2")
for x in (12, 15, 18): f(9, x, x + 1, ".")
# receiver with the stock pivot
for y in range(11, 17): f(y, 0, 20, "4")
f(11, 1, 20, "3"); f(12, 0, 20, "3"); f(15, 0, 20, "5"); f(16, 1, 19, "5"); G[11][0] = "."
put(G, 12, 1, "212"); put(G, 13, 1, "121"); put(G, 14, 2, "2")
put(G, 13, 13, "2"); put(G, 13, 16, "2")
# raked polymer pistol grip with finger grooves
GR = {17: (2, 7), 18: (1, 6), 19: (1, 6), 20: (0, 5), 21: (0, 5), 22: (0, 4), 23: (0, 4)}
for y, (a, b) in GR.items():
    f(y, a, b, "q"); G[y][a] = "p"; G[y][a + 1] = "p"; G[y][b] = "r"
for y in (18, 20, 22): G[y][GR[y][1]] = "."
f(23, 0, 4, "r"); G[23][0] = "p"
# trigger guard (hole open) + trigger
for y in (17, 18): G[y][12] = "3"
f(19, 8, 12, "3"); put(G, 17, 9, "4")
# vented heat shield
for y in range(11, 17): f(y, 21, 25, "2")
f(11, 21, 25, "1"); f(16, 21, 25, "3")
for y in (13, 14): put(G, y, 21, "25252")
# ribbed pump fore-end
for y in range(11, 18): f(y, 26, 43, "q")
f(11, 27, 43, "p"); f(17, 27, 42, "r"); G[11][26] = "."; G[17][26] = "."; G[17][43] = "."
for x in range(29, 42, 2): f(14, x, x, "r"); f(15, x, x, "r")
f(12, 27, 42, "p"); G[12][26] = "q"
# barrel + front sight, magazine tube + cap
f(10, 44, 57, "2"); f(11, 44, 57, "3"); f(12, 44, 57, "4"); G[11][57] = "5"; G[12][57] = "5"
put(G, 8, 52, "33"); put(G, 9, 51, "3443"); G[8][52] = "2"
f(13, 44, 55, "3"); f(14, 44, 55, "5"); put(G, 13, 56, "2"); put(G, 14, 56, "4")
f(13, 48, 49, "4"); f(14, 48, 49, "5")
A = ["".join(r) for r in G]

# ---------------- B: 38x15 in hand
H = grid(38, 15)
h = lambda y, a, b, c: fill(H, y, a, b, c)
for (x, y) in ((8, 0), (9, 0), (10, 0), (11, 0), (7, 1), (7, 2), (7, 3), (12, 1)): H[y][x] = "2" if y == 0 else "3"
for y in range(1, 7): h(y, 12, 14, "4")
for y in range(1, 7): H[y][12] = "3"
h(5, 4, 14, "3"); h(5, 4, 11, "2"); h(6, 2, 14, "4"); H[5][7] = "."; H[5][9] = "."
for y in range(7, 11): h(y, 0, 12, "4")
h(7, 0, 12, "3"); h(10, 1, 11, "5"); H[7][0] = "."; put(H, 8, 1, "21")
GB = {11: (1, 4), 12: (1, 3), 13: (0, 3), 14: (0, 2)}
for y, (a, b) in GB.items(): h(y, a, b, "q"); H[y][a] = "p"; H[y][b] = "r"
h(14, 0, 2, "r")
put(H, 11, 7, "3"); h(12, 5, 7, "3"); put(H, 11, 5, "4")
for y in range(7, 11): h(y, 13, 15, "2")
H[8][14] = "5"; H[9][14] = "5"; h(10, 13, 15, "3")
for y in range(7, 12): h(y, 16, 27, "q")
h(7, 16, 27, "p"); h(11, 17, 26, "r"); H[11][16] = "."; H[11][27] = "."
for x in (18, 20, 22, 24): H[9][x] = "r"
h(6, 28, 37, "2"); h(7, 28, 37, "4"); put(H, 5, 34, "3")
h(8, 28, 35, "3"); h(9, 28, 35, "5"); put(H, 8, 36, "2"); put(H, 9, 36, "4")
B = ["".join(r) for r in H]
GRIP = (2.0, 12.0); SUPPORT = (22.0, 11.0); MUZZLE = (38, 6.5)
