# Walden P9 (P99-pattern polymer 9mm): black slide, OD-green frame, stippled grip, black backstrap.
from common import grid, fill, put
CAT = "sidearm"

# ---------------- A: 42x32 menu sprite
G = grid(42, 32)
f = lambda y, a, b, c: fill(G, y, a, b, c)
put(G, 0, 2, "33"); put(G, 0, 38, "33")                       # sights
f(1, 1, 40, "2"); f(2, 0, 41, "3"); f(2, 6, 38, "3")
for y in (3, 4, 5): f(y, 0, 41, "4")
f(6, 1, 41, "5")
f(1, 20, 26, "1"); f(2, 20, 26, "5"); f(3, 20, 26, "5")       # raised ejection-port step
for x in (4, 6, 8, 10, 12):                                   # rear serrations
    for y in (3, 4, 5): G[y][x] = "5"
for x in (5, 7, 9, 11): G[3][x] = "2"
G[2][41] = G[5][41] = "5"
# frame (olive): lit top, accessory-rail groove, dust cover
f(7, 0, 41, "o"); f(8, 0, 41, "O"); f(9, 1, 40, "O"); f(9, 29, 40, "e"); f(8, 31, 40, "e")
for y in (7, 8, 9): f(y, 22, 25, "4")                         # slide-stop / takedown block
G[9][0] = "."; G[9][41] = "."
# trigger guard
for y in range(10, 17): put(G, y, 29, "Oe")
f(16, 19, 29, "O"); f(17, 19, 29, "e"); G[17][29] = "."; G[16][30] = "."
for (y, x, c) in ((10, 20, "3"), (11, 20, "3"), (11, 21, "4"), (12, 21, "4"), (12, 22, "4"), (13, 22, "4"),
                  (13, 23, "4"), (14, 23, "4"), (14, 24, "4"), (15, 24, "3")):
    G[y][x] = c
# grip: lit back edge, black backstrap insert, stippling, dark front edge, black mag base
BACK = {10: 4, 11: 4, 12: 4, 13: 3, 14: 3, 15: 3, 16: 2, 17: 2, 18: 2, 19: 1, 20: 1, 21: 1, 22: 1}
FRONT = {16: 18, 17: 17, 18: 16, 19: 16, 20: 15, 21: 15, 22: 14, 23: 14, 24: 14, 25: 13, 26: 13, 27: 13, 28: 13, 29: 12}
for y in range(10, 30):
    a = BACK.get(y, 0 if y < 29 else 1); b = FRONT.get(y, 19)
    f(y, a, b, "O"); G[y][a] = "o"; G[y][b] = "e"
    if 12 <= y <= 25: G[y][a + 1] = "4"; G[y][a + 2] = "4" if y > 13 else "O"
    for x in range(a + 4, b - 1):
        if (x + 2 * y) % 4 == 0 and 13 <= y <= 27: G[y][x] = "e"
f(30, 2, 12, "4"); f(31, 3, 12, "5")
A = ["".join(r) for r in G]

# ---------------- B: 19x14 in hand
B = [".3...............3.",
     "3" + "2" * 8 + "1111" + "2" * 6,
     "4" + "53535" + "333" + "5555" + "33333" + "4",
     "5" + "4" * 17 + "5",
     "o" * 10 + "44" + "oooooo" + "e",
     ".." + "OOOOOOO" + "." + "3" + "..." + "Oe" + "...",
     "." + "o" + "4" + "OeOOO" + "e" + "." + "." + "3" + ".." + "Oe" + "...",
     "." + "o4" + "OOeOO" + "OO" + ".." + "3" + "." + "Oe" + "...",
     "." + "o4" + "OeOOO" + "O" + "OOOOOe" + "....",
     "." + "o4" + "OOOeO" + "e" + "." * 10,
     "o" + "44" + "OeOO" + "e" + "." * 11,
     "o" + "44" + "OOeO" + "e" + "." * 11,
     "o4OOeOe" + "." * 12,
     "." + "455554" + "." * 12]
GRIP = (4.0, 9.0); SUPPORT = None; MUZZLE = (19, 2.5)
