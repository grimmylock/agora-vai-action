# PP-Z Zubr (PP-19 Bizon pattern): AK-style receiver with top cover and rear sight, side-folding tubular stock
# laid along the receiver, slotted upper handguard, tall front-sight post, slotted muzzle device, and the big
# helical magazine running under the barrel as the forend; raked stippled pistol grip.
from common import grid, fill, put
CAT = "smg"

# ---------------- A: 60x22 menu sprite
G = grid(60, 22)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# receiver + top cover + rear hinge block
f(2, 3, 30, "2")
for y in range(3, 10): f(y, 1, 30, "3" if y < 4 else "4")
f(9, 1, 30, "5"); f(1, 22, 24, "3"); G[1][23] = "4"                        # rear sight
for y in range(3, 11): f(y, 0, 2, "4"); G[y][0] = "3"
# folded tubular stock along the side: two struts + butt pad
f(4, 4, 26, "p"); f(7, 4, 26, "q"); f(5, 4, 26, "4"); f(6, 4, 26, "4")
for y in range(3, 9): f(y, 26, 28, "q"); G[y][26] = "p"
G[3][26] = G[3][28] = "."; G[8][26] = G[8][28] = "."
put(G, 5, 5, "11"); put(G, 6, 5, "22")                                     # hinge pin
put(G, 8, 16, "2"); put(G, 8, 10, "222")                                   # selector
# upper handguard with vent slots
f(2, 31, 51, "2")
for y in range(3, 8): f(y, 31, 51, "3" if y < 5 else "4")
for a in (36, 42, 48): f(4, a, a + 2, "5"); f(5, a, a + 2, "5")
G[4][36] = G[5][38] = "4"
# front sight post + barrel + slotted muzzle device
for y in range(0, 4): f(y, 50, 52, "3"); G[y][51] = "4" if y < 2 else "3"
G[0][51] = "."
f(4, 52, 53, "3"); f(5, 52, 53, "4")
for y in (3, 4, 5, 6): f(y, 54, 59, "3")
f(3, 54, 59, "2"); f(6, 54, 59, "4"); G[4][56] = G[5][56] = G[4][58] = G[5][58] = "5"
# helical magazine under the barrel (the forend)
f(8, 21, 52, "2")
for y in range(9, 14): f(y, 20, 53, "3" if y < 11 else "4")
f(13, 21, 52, "5"); f(14, 26, 34, "5"); f(14, 27, 33, "4")
for x in range(24, 50, 5): G[10][x] = "4"; G[11][x] = "5"                   # helix ribs
for y in range(8, 14): G[y][53] = "4"; G[y][52] = "3" if y < 13 else "5"
G[8][53] = G[13][53] = "."
# trigger guard + trigger
f(10, 6, 19, "5")
for y, row in {11: "4......4", 12: "4......4", 13: "4......4", 14: ".4444444"}.items():
    for i, ch in enumerate(row):
        if ch != ".": G[y][11 + i] = ch
put(G, 11, 14, "3"); put(G, 12, 14, "2"); put(G, 13, 15, "2")
# raked pistol grip, stippled
BK = {11: 5, 12: 4, 13: 4, 14: 3, 15: 3, 16: 2, 17: 2, 18: 2, 19: 1, 20: 1, 21: 0}
FR = {11: 10, 12: 10, 13: 10, 14: 9, 15: 9, 16: 8, 17: 8, 18: 8, 19: 7, 20: 7, 21: 6}
for y in range(11, 22):
    a, b = BK[y], FR[y]
    f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "r"
    for x in range(a + 1, b):
        if (x + 2 * y) % 3 == 0: G[y][x] = "r"
f(21, 0, 6, "r")
A = ["".join(r) for r in G]

# ---------------- B: 28x10 in hand
B = ["." * 10 + "3" + "." * 12 + "3" + "." * 4,
     "." + "2" * 22 + "3" + "....",
     "3" + "4" + "p" * 10 + "q" + "3" + "4" + "5" + "44" + "5" + "44" + "5" + "4" + "4" + "2121",
     "3" + "4" + "q" * 10 + "44" + "4" * 10 + "3434",
     "4" + "5" * 8 + "2" * 14 + "33" + "...",
     "." + "pqqq" + "." + "3" + "." + "4" + "3" * 14 + "34" + "...",
     "pqrqr" + "..." + "4" + "4545" + "4" * 10 + "44" + "...",
     "pqrq" + "." + "4444" + "." * 19,
     "pqqr" + "." * 24,
     "rrr" + "." * 25]
GRIP = (2.5, 7.0); SUPPORT = (17.0, 6.0); MUZZLE = (28, 3.0)
