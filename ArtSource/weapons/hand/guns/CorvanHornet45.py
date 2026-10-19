# Corvan Hornet .45 (UMP45 pattern, stock extended): skeleton side-folding stock, slab polymer receiver with a
# full-length top rail, hooded front sight, vented forend, short barrel with muzzle nut, vertical foregrip,
# integral trigger guard, raked grip, straight box magazine.
from common import grid, fill, put
CAT = "smg"

# ---------------- A: 62x29 menu sprite
G = grid(62, 29)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# skeleton stock: top bar, butt plate, diagonal brace, centre rib
f(5, 0, 20, "p"); f(6, 0, 20, "q")
for y in range(5, 14): f(y, 0, 1, "q"); G[y][0] = "p"
f(13, 0, 3, "r"); G[13][0] = "q"
for x in range(2, 20):
    y = round(12.5 - (x - 2) * 5 / 17)
    G[y][x] = "q"; G[y + 1][x] = "r"
for y in range(7, 10): G[y][11] = "q"
# receiver + top rail
f(1, 22, 45, "3"); f(2, 21, 46, "4")
for x in range(23, 45, 2): G[1][x] = "."
for y in range(3, 10): f(y, 19 if y > 4 else 21, 52, "q")
f(3, 21, 52, "p"); f(9, 21, 46, "r")
for y in range(5, 10): G[y][19] = "q"; G[y][20] = "q"
put(G, 6, 31, "pp"); put(G, 7, 32, "r"); G[5][26] = "r"; G[7][38] = "r"    # pins / selector
f(5, 46, 51, "r"); f(6, 47, 50, "5")                                       # forend vent
put(G, 4, 44, "3")
# front sight hood + barrel + muzzle nut
for y in range(0, 5): f(y, 52, 54, "4")
f(0, 52, 54, "3"); G[1][53] = "."; G[2][53] = "."
f(3, 52, 54, "q"); f(4, 52, 54, "q"); f(5, 52, 54, "q"); f(6, 52, 54, "q"); f(7, 52, 54, "r"); f(8, 52, 54, "r")
f(5, 55, 61, "3"); f(6, 55, 61, "4"); f(4, 57, 60, "2"); f(7, 57, 60, "4"); f(5, 57, 60, "2"); G[6][61] = "5"
# vertical foregrip
for y in range(10, 19): f(y, 46, 49, "q"); G[y][46] = "p"; G[y][49] = "r"
for y in range(12, 18, 2): f(y, 47, 48, "r")
f(18, 46, 49, "r")
# trigger guard (integral) + trigger
f(10, 24, 37, "q"); f(10, 29, 36, ".")
for y, row in {11: "q......q", 12: "q......q", 13: ".rrrrrrr"}.items():
    for i, ch in enumerate(row):
        if ch != ".": G[y][28 + i] = ch
put(G, 10, 31, "3"); put(G, 11, 31, "2"); put(G, 12, 32, "2")
# raked grip
for y in range(10, 20):
    a = 24 - (y - 10) * 3 // 9; b = a + 4
    f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "r"
    if 12 <= y <= 18 and y % 2 == 0: G[y][a + 2] = "r"
f(19, 21, 25, "r")
# straight magazine, slight forward cant
for y in range(10, 29):
    a = 37 + (y - 10) * 3 // 18
    f(y, a, a + 4, "4"); G[y][a] = "3"; G[y][a + 4] = "5"
    if y > 11: G[y][a + 2] = "3"
f(28, 40, 44, "5")
A = ["".join(r) for r in G]

# ---------------- B: 30x14 in hand
H = grid(30, 14)
g = lambda y, a, b, c: fill(H, y, a, b, c)
g(2, 0, 9, "p")
for y in range(2, 7): H[y][0] = "q"
for (y, a, b) in ((6, 0, 2), (5, 3, 5), (4, 6, 8), (3, 8, 9)): g(y, a, b, "r")
H[3][5] = "q"; H[4][5] = "q"                                               # centre rib
g(0, 11, 21, "3")
for x in range(12, 21, 2): H[0][x] = "."
for y in range(1, 5): g(y, 9, 25, "q")
g(1, 10, 25, "p"); g(4, 10, 21, "r"); g(3, 21, 24, "r")
H[2][15] = "p"; H[3][16] = "r"
put(H, 0, 25, "34"); put(H, 1, 25, "4")                                    # front sight
for y in range(1, 5): H[y][25] = "q"
g(2, 26, 29, "3"); g(3, 26, 29, "4"); H[1][28] = "2"; H[4][28] = "4"; H[2][28] = "2"
for y in range(5, 10): g(y, 22, 23, "q"); H[y][22] = "p"
g(9, 22, 23, "r")
put(H, 5, 13, "q...q"); put(H, 6, 14, "rrrr"); H[5][15] = "2"
for (y, a) in ((5, 11), (6, 11), (7, 10), (8, 10), (9, 9)):
    g(y, a, a + 2, "q"); H[y][a] = "p"; H[y][a + 2] = "r"
g(9, 9, 11, "r")
for y in range(5, 14):
    a = 18 + (y - 5) // 6
    g(y, a, a + 2, "4"); H[y][a] = "3"; H[y][a + 2] = "5"
B = ["".join(r) for r in H]
GRIP = (11.0, 7.0); SUPPORT = (22.5, 7.0); MUZZLE = (30, 3.0)
