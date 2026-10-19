# Steppe Eagle (.44 gas-operated big-bore pistol, Desert-Eagle pattern), brushed steel with a black rubber
# wrap grip. Reference photo is angled, so this is a clean side profile drawn from the real gun: long slab
# slide/barrel, top rail with sights, raked rear serrations, round hammer, slide-mounted safety, squared
# hooked trigger guard, steel magazine base.
from common import grid, fill, put
CAT = "sidearm"

# ---------------- A: 50x29 menu sprite
G = grid(50, 29)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# slide (rear) + barrel (front), one long slab
put(G, 0, 9, "33"); put(G, 0, 46, "33")
f(1, 4, 49, "0"); f(1, 15, 49, "0")
for y in range(2, 9): f(y, 3, 49, "1")
f(2, 3, 49, "0"); f(2, 4, 14, "1")
f(1, 4, 14, "1"); G[1][4] = "2"                                            # rear section sits lower
f(1, 15, 49, "0"); G[1][14] = "2"
f(6, 30, 49, "2"); f(7, 30, 49, "2"); f(8, 30, 48, "3"); G[8][49] = "."    # lower barrel bevel
f(8, 3, 29, "3"); f(5, 30, 48, "1"); G[5][30] = "2"
for y in range(1, 9): G[y][30] = "2" if y > 2 else G[y][30]                # slide / barrel joint
for y in range(3, 8):                                                     # raked serrations
    for x in range(8, 16, 2): G[y][x] = "2"
put(G, 2, 4, "33"); put(G, 3, 4, "313"); put(G, 4, 5, "3")                # ambi safety
# hammer + tang
put(G, 1, 1, "33"); put(G, 2, 0, "343"); put(G, 3, 1, "44")
f(9, 0, 4, "1"); G[9][0] = "0"
# frame
f(9, 3, 33, "1"); f(10, 3, 33, "2"); f(11, 13, 33, "2")
G[9][3] = "0"; put(G, 9, 19, "33"); G[10][20] = "3"                        # slide stop
# trigger guard: squared front with a hook
for y in range(12, 18): G[y][26] = "1"; G[y][27] = "2"
G[17][27] = "1"; G[18][28] = "2"; G[17][28] = "2"
f(18, 16, 26, "2"); G[19][17] = "."
for y in range(12, 18): G[y][15] = "1"
put(G, 12, 19, "3"); put(G, 13, 19, "4"); put(G, 14, 20, "4"); put(G, 15, 20, "3")   # trigger
# grip: black rubber wrap, steel front strap
BK = {y: 4 if y < 13 else 3 if y < 18 else 2 if y < 24 else 1 for y in range(10, 29)}
FR = {y: 14 if y < 16 else 13 if y < 21 else 12 for y in range(10, 29)}
for y in range(11, 27):
    a, b = BK[y], FR[y]
    f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "1" if y < 18 else "r"; G[y][b - 1] = "r" if y >= 18 else "q"
    for x in range(a + 2, b - 1):
        if (x + 2 * y) % 4 == 0: G[y][x] = "r"
f(27, 1, 12, "1"); f(28, 1, 12, "3"); G[27][1] = "0"                       # steel magazine base
A = ["".join(r) for r in G]

# ---------------- B: 21x13 in hand
B = ["...3" + "." * 15 + "3.",
     ".3" + "1" * 9 + "0" * 10,
     "34" + "13131" + "1" * 13 + "2",
     ".1" + "31313" + "1" * 6 + "2" * 7 + "2",
     "." + "3" * 11 + "2" + "3" * 7 + "3",
     "0" + "1" * 13 + "2" + "." * 6,
     "." + "pqrq" + "q" + "1" + "." + "0" + ".." + "1" + "2" + "." * 8,
     "." + "pqqr" + "q" + "1" + "." + "1" + ".." + "1" + "2" + "." * 8,
     "." + "prqq" + "r" + "1" + "...." + "12" + "." * 8,
     "." + "pqrq" + "r" + "." + "22222" + "." * 9,
     "p" + "qqrq" + "r" + "." * 15,
     "p" + "rqqq" + "r" + "." * 15,
     "0" + "1111" + "2" + "." * 15]
GRIP = (3.5, 9.0); SUPPORT = None; MUZZLE = (21, 2.5)
