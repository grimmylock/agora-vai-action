# Obrez-66 (sawn-off TOZ-66 side-by-side): stock cut down to a pistol grip, silver boxlock receiver,
# short blued barrels with a rib and bead, splinter fore-end.
from common import grid, fill, put
CAT = "shotgun"

# ---------------- A: 60x17 menu sprite
G = grid(60, 17)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# grip (walnut): row -> x range; shade by neighbours (lit top, dark underside), sparse checkering
STOCK = {2: (16, 18), 3: (14, 19), 4: (11, 19), 5: (9, 19), 6: (7, 19), 7: (5, 18), 8: (4, 14), 9: (3, 12),
         10: (2, 10), 11: (2, 9), 12: (1, 8), 13: (1, 7), 14: (0, 7), 15: (0, 6), 16: (1, 5)}
inn = lambda x, y: y in STOCK and STOCK[y][0] <= x <= STOCK[y][1]
for y, (a, b) in STOCK.items():
    for x in range(a, b + 1):
        c = "b"
        if not inn(x, y + 1) or not inn(x + 1, y): c = "d"
        elif not inn(x, y + 2): c = "c"
        if not inn(x, y - 1) or (not inn(x - 1, y) and y < 14): c = "a"
        if c == "b" and 9 <= y <= 14 and (x + 2 * y) % 4 == 0 and inn(x - 1, y - 1) and inn(x + 2, y): c = "c"
        G[y][x] = c
f(16, 1, 5, "d")                                     # sawn butt end
put(G, 1, 16, "344"); put(G, 2, 18, "4")             # top lever over the tang
# receiver (silver boxlock): lit top, engraving dots, shaded belly, rounded front
for y in range(2, 8): f(y, 19, 30, "2")
f(2, 19, 23, "1"); f(5, 24, 30, "1")
f(7, 19, 29, "3"); G[7][30] = "."; G[6][30] = "3"
for (x, y) in ((21, 4), (23, 6), (26, 6), (20, 6), (28, 6)): G[y][x] = "3"
G[3][19] = G[4][19] = "3"
# barrels: rib, blued body, bead, muzzle
f(1, 24, 59, "2"); f(2, 24, 59, "3"); f(3, 24, 59, "3"); f(4, 24, 59, "4")
for y in range(1, 5): G[y][24] = "4"
f(5, 31, 59, "5"); put(G, 0, 57, "1")
for y in range(1, 6): G[y][59] = "5" if y > 1 else "3"
# splinter fore-end under the barrels
f(5, 31, 51, "b"); f(6, 31, 50, "c"); f(7, 32, 48, "d"); G[5][51] = "c"; G[6][31] = "d"
f(5, 33, 49, "a")
# trigger guard (hole open) + double triggers
for y in range(8, 12): f(y, 14, 20, ".")
for y in range(8, 11): G[y][21] = "3"
G[11][20] = "3"; f(12, 15, 19, "4"); G[11][14] = "4"; G[10][13] = "4"
put(G, 8, 16, "4"); put(G, 9, 16, "4"); put(G, 8, 18, "4"); put(G, 9, 18, "4")
A = ["".join(r) for r in G]

# ---------------- B: 24x7 in hand
B = ["......aab" + "1111" + "22222222222",
     "....aabbb" + "2212" + "3333333333" + "4",
     "..abbbbbc" + "3323" + "aaaaaab" + "4445",
     ".abbcccd3" + "..3." + "cccccd." + "....",
     "abccd..." + "333." + "." * 12,
     "bccd" + "." * 20,
     "ddd." + "." * 20]
GRIP = (4.5, 3.0); SUPPORT = (16.0, 3.0); MUZZLE = (24, 1.0)
