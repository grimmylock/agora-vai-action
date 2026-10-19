# Halden HR-36K (G36K pattern): dark olive polymer, skeleton folding stock with big cutout, carry handle
# with integral optic and top rail, vented handguard, raked grip, smoky translucent mag, flash hider.
from common import grid, fill, put
CAT = "assault"

# ---------------- A: 58x20 menu sprite
G = grid(58, 20)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# skeleton stock: tall butt, top bar, diagonal lower bar (cutout open)
for y in range(5, 15): f(y, 0, 2, "O")
for y in range(5, 15): G[y][0] = "o"
f(14, 0, 2, "e"); G[5][2] = "o"
f(5, 0, 17, "o"); f(6, 3, 17, "O"); G[7][3] = "e"
for x in range(3, 17):
    y = 13 - round((x - 3) * 5 / 13)
    G[y][x] = "O"; G[y + 1][x] = "e"
put(G, 7, 15, "e"); put(G, 8, 15, "Oe")
# receiver body + handguard: lit top, port, vent slots, dark belly
for y in range(4, 10): f(y, 16, 51, "O")
f(4, 16, 51, "o"); f(9, 17, 50, "e"); f(8, 17, 51, "e"); G[9][16] = "."; G[9][51] = "."
f(6, 25, 30, "e"); G[5][32] = "2"; put(G, 7, 18, "eoe")
for x in range(41, 50, 3): f(6, x, x + 1, "e")
f(5, 36, 51, "O"); f(4, 39, 51, "o"); G[4][51] = "."
# carry handle: optic block with rail, arch, front leg (hole open)
f(0, 19, 37, "e")
for x in range(19, 38, 2): G[0][x] = "O"
for y in range(1, 4): f(y, 18, 26, "O")
f(1, 18, 26, "o"); put(G, 2, 25, "l"); put(G, 3, 25, "L"); f(3, 18, 24, "e")
f(1, 27, 37, "o"); f(2, 27, 36, ".")
for (x, y) in ((37, 2), (38, 2), (38, 3), (39, 3)): G[y][x] = "O"
G[1][38] = "o"; G[2][39] = "e"; G[3][40] = "e"
# raked pistol grip + guard (hole open)
GR = {10: (19, 22), 11: (18, 22), 12: (18, 21), 13: (17, 21), 14: (17, 20), 15: (17, 20)}
for y, (a, b) in GR.items(): f(y, a, b, "O"); G[y][a] = "o"; G[y][b] = "e"
f(15, 17, 20, "e")
for y in (10, 11): G[y][27] = "O"
f(12, 22, 27, "O"); f(12, 23, 26, "e"); put(G, 10, 24, "4")
# smoky translucent mag, slight curve, base plate
for y in range(10, 19):
    a = 28 + (y - 10) // 4; b = a + 5
    f(y, a, b, "p"); G[y][a] = "2"; G[y][b] = "q"
    if y in (12, 15): f(y, a + 2, b - 1, "q")
f(19, 30, 35, "e"); f(10, 29, 32, "q")
# barrel + flash hider
f(6, 52, 53, "2"); f(7, 52, 53, "4")
for y in range(5, 9): f(y, 54, 57, "3")
f(5, 54, 57, "2"); f(8, 54, 57, "4"); G[6][55] = "5"; G[7][55] = "5"; G[6][57] = "5"; G[7][57] = "5"
A = ["".join(r) for r in G]

# ---------------- B: 35x12 in hand
B = ["." * 11 + "eOeOeOeOeOeOeO" + "." * 10,
     "." * 11 + "ooool" + "." * 7 + "oO" + "." * 10,
     "." * 11 + "OOOOL" + "." * 7 + "Oe" + "." * 10,
     "oooooooooo" + "o" + "oooooooooooo" + "oooooooo" + "....",
     "Oe" + "......" + "OO" + "O" + "OOOOeeeeOOOO" + "OeOeOeOO" + "2" + "333",
     "Oe" + "...." + "OO" + ".." + "e" + "OOOOOOOOOOOO" + "OOOOOOOO" + "4" + "454",
     "Oe" + ".." + "OO" + "...." + "e" + "eeeeeeeeeeee" + "eeeeeeee" + "....",
     "Oe" + "OO" + "......." + "oOe" + "4O" + "2ppq" + "." * 15,
     "ee" + "........" + "oOe" + "OOO" + "2ppq" + "." * 15,
     "." * 9 + "oOe" + "...." + "2qqq" + "." * 15,
     "." * 9 + "eee" + "....." + "2ppq" + "." * 14,
     "." * 17 + "eeee" + "." * 14]
GRIP = (11.0, 8.0); SUPPORT = (27.0, 6.0); MUZZLE = (35, 4.5)
