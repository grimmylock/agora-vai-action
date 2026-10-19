# Halden lever shotgun (1887-pattern): tall blued receiver with hammer, big finger-lever loop,
# walnut stock with steep drop, wood fore-end over the magazine tube, short barrel.
from common import grid, fill, put
CAT = "shotgun"

# ---------------- A: 60x16 menu sprite
G = grid(60, 16)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# stock (walnut): row -> x range, lit comb, dark belly, steel buttplate
STOCK = {3: (19, 21), 4: (13, 21), 5: (1, 21), 6: (1, 21), 7: (1, 21), 8: (1, 21), 9: (1, 19), 10: (1, 17),
         11: (1, 14), 12: (1, 10), 13: (1, 6)}
inn = lambda x, y: y in STOCK and STOCK[y][0] <= x <= STOCK[y][1]
for y, (a, b) in STOCK.items():
    for x in range(a, b + 1):
        c = "b"
        if not inn(x, y + 1): c = "d"
        elif not inn(x, y + 2): c = "c"
        if not inn(x, y - 1): c = "a"
        G[y][x] = c
for y in range(5, 14): G[y][0] = "4"
G[5][0] = "3"; G[13][0] = "5"
# receiver (blued, case colours): rounded top, hammer, breech-bolt line, screws
REC = {1: (24, 29), 2: (22, 31), 3: (21, 31), 4: (21, 31), 5: (21, 31), 6: (21, 31), 7: (21, 30), 8: (21, 29), 9: (22, 28)}
for y, (a, b) in REC.items(): f(y, a, b, "3")
f(1, 24, 29, "2"); f(2, 22, 23, "2"); f(2, 24, 30, "1")
f(8, 21, 29, "4"); f(9, 22, 28, "5")
f(3, 22, 30, "2"); G[3][21] = "3"
for y in (4, 5, 6, 7): G[y][28] = "4"
G[6][24] = "5"; G[4][26] = "5"; G[7][25] = "2"
put(G, 0, 19, "44"); put(G, 1, 20, "4444"); put(G, 2, 21, "4")   # hammer spur
# barrel + bead, wood fore-end, magazine tube with cap, barrel band
f(2, 32, 59, "2"); f(3, 32, 59, "4"); put(G, 1, 58, "1"); G[3][59] = "5"
f(4, 32, 47, "a"); f(5, 32, 47, "b"); f(6, 32, 46, "c"); G[6][47] = "."; G[5][47] = "c"
f(4, 48, 55, "3"); f(5, 48, 55, "5"); put(G, 4, 56, "2"); put(G, 5, 56, "4")
for y in range(2, 6): G[y][51] = "3"
G[2][51] = "2"; G[5][51] = "4"
# finger lever loop (hole open) + trigger
LEVER = [(27, 10), (26, 11), (25, 12), (24, 13), (23, 14), (22, 14), (21, 14), (20, 14), (19, 14), (18, 13),
         (17, 12), (17, 11), (18, 10)]
for (x, y) in LEVER: G[y][x] = "3"
for (x, y) in ((28, 10), (23, 14), (22, 14), (21, 14), (20, 14)): G[y][x] = "2" if y == 14 else "4"
G[10][24] = "4"; G[11][23] = "4"
A = ["".join(r) for r in G]

# ---------------- B: 36x8 in hand
B = ["............34" + "2221" + "." * 18,
     "............" + "42111112" + "2" * 15 + "1",
     "........." + "aaa" + "33335333" + "4" * 16,
     "4aaaaaaab" + "bbb" + "33333334" + "aaaaaaaaab" + "3333" + "2.",
     "4bbbbbbbbbbc" + "4445444" + "." + "cccccccccd" + "5555" + "4.",
     "4bbbbbbbcccd" + "." + "455554" + "." * 17,
     "4cccccdd" + ".." + "3..." + "2" + "33" + "." * 19,
     "4ddd" + "......" + "33333" + "." * 21]
GRIP = (10.5, 4.5); SUPPORT = (25.0, 4.0); MUZZLE = (36, 1.5)
