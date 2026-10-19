# Corvan 5.7 (Five-seveN pattern): long flat polymer-clad slide with rear serrations, polymer frame with
# accessory rail, big squared trigger guard, humped backstrap, stippled grip panels, pale takedown lever.
from common import grid, fill, put
CAT = "sidearm"

# ---------------- A: 44x31 menu sprite
G = grid(44, 31)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# slide
put(G, 0, 3, "33"); G[0][40] = "3"
f(1, 2, 43, "2")
for y in (2, 3, 4): f(y, 1, 43, "3" if y == 2 else "4")
f(5, 1, 43, "5")
for x in range(3, 12, 2):
    for y in (2, 3, 4): G[y][x] = "5"
f(2, 19, 26, "4"); f(3, 19, 26, "5"); f(2, 20, 25, "5")                    # ejection port
# frame + rail
f(6, 0, 41, "p")
for y in (7, 8): f(y, 0, 40, "q")
f(9, 1, 40, "q"); f(9, 29, 40, "r")
for x in range(30, 41, 2): G[10][x] = "r"                                 # rail slots
G[6][42] = "."; G[6][41] = "q"
put(G, 7, 18, "pp"); put(G, 8, 18, "2p"); put(G, 7, 20, "2")              # takedown lever
G[7][2] = "r"; G[8][11] = "r"; G[8][27] = "r"
# trigger guard (squared front, open hole)
f(10, 13, 28, "q"); f(10, 29, 29, "r")
for y in range(11, 16): G[y][27] = "q"; G[y][28] = "r"
f(16, 15, 27, "r"); G[15][28] = "."; G[16][27] = "."
put(G, 11, 20, "p"); put(G, 12, 20, "p"); put(G, 13, 20, "q"); put(G, 14, 21, "q")   # trigger
# grip with humped backstrap
BK = {10: 1, 11: 2, 12: 3, 13: 3, 14: 3, 15: 3, 16: 2, 17: 2, 18: 2, 19: 2, 20: 2, 21: 1}
FR = {y: 14 if y < 22 else 13 if y < 28 else 12 for y in range(10, 31)}
for y in range(10, 30):
    a, b = BK.get(y, 1), FR[y]
    f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "r"
    if 18 <= y <= 28:
        for x in range(a + 2, b - 1):
            if (x + 2 * y) % 4 == 0 and x != 7: G[y][x] = "r"
    if 17 <= y <= 28: G[y][7] = "r"                                       # panel seam
f(30, 1, 12, "r"); f(29, 2, 12, "r"); G[29][1] = "p"
put(G, 15, 13, "22")                                                      # mag release
A = ["".join(r) for r in G]

# ---------------- B: 19x13 in hand
B = ["..3" + "." * 14 + "3.",
     "." + "2" * 18,
     "." + "53535" + "3" * 13,
     "." + "53535" + "4" * 13,
     "." + "5" * 18,
     "p" * 18 + ".",
     "q" * 12 + "r.r.r.r",
     "." + "pqqqq" + "q" + "." + "p" + "..." + "q" + "." * 6,
     "." + "pqqqq" + "2" + "." + "q" + "..." + "r" + "." * 6,
     ".." + "pqrq" + "rrrrrrr" + "." * 6,
     "." + "pqrqqr" + "." * 12,
     "." + "pqqrqr" + "." * 12,
     "." + "rrrrr" + "." * 13]
GRIP = (3.5, 9.0); SUPPORT = None; MUZZLE = (19, 2.5)
