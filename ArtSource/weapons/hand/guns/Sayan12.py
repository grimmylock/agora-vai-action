# Sayan-12 (Saiga-12 sporting pattern): black polymer Monte-Carlo stock, AK receiver with dust cover,
# polymer handguard, short straight box mag, long thin barrel with gas tube, choke-tube muzzle.
from common import grid, fill, put
CAT = "shotgun"

# ---------------- A: 60x11 menu sprite
G = grid(60, 11)
f = lambda y, a, b, c: fill(G, y, a, b, c)
TOP = {x: 2 if x < 12 else 1 for x in range(0, 16)}
BOT = {0: 7, 1: 7, 2: 8, 3: 8, 4: 8, 5: 7, 6: 7, 7: 6, 8: 6, 9: 6, 10: 5, 11: 5, 12: 5, 13: 5, 14: 4, 15: 4}
for x in range(16):
    t, b = TOP[x], BOT[x]
    for y in range(t, b + 1): G[y][x] = "q"
    G[t][x] = "p"; G[b][x] = "r"
for y in range(2, 8): G[y][0] = "r"
f(3, 1, 10, "p"); G[2][0] = "q"                     # cheek-piece roll
# receiver: dust cover, port, charging handle, selector, mag well
f(0, 17, 30, "2"); f(1, 15, 30, "3"); f(2, 15, 30, "4"); f(3, 15, 30, "4"); f(4, 16, 30, "5")
f(0, 19, 25, "1"); G[1][16] = "2"
f(1, 21, 26, "5"); put(G, 2, 27, "11"); put(G, 2, 17, "2222221")
G[3][16] = G[3][18] = "3"; G[4][30] = "."
# trigger guard (hole open) + trigger
for y in (5, 6): G[y][22] = "3"
f(7, 17, 22, "3"); f(7, 18, 21, "2"); G[6][16] = "3"; put(G, 5, 19, "4")
# 5-round box magazine, slight forward cant
MG = {5: (24, 29), 6: (24, 29), 7: (25, 30), 8: (25, 30), 9: (25, 30), 10: (26, 30)}
for y, (a, b) in MG.items():
    f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "r"
f(10, 26, 30, "r"); f(5, 25, 28, "r")
# handguard, gas block, barrel + gas tube, front sight, choke
for y in range(0, 4): f(y, 31, 42, "q")
f(0, 31, 42, "p"); f(3, 32, 41, "r"); G[3][31] = "."; G[3][42] = "."
for x in (34, 37, 40): G[2][x] = "r"
for y in range(0, 3): f(y, 43, 44, "3")
f(0, 43, 44, "2")
f(0, 45, 51, "3"); f(1, 45, 57, "2"); f(2, 45, 57, "4")
put(G, 0, 52, "2")
for y in range(0, 4): f(y, 57, 59, "3")
f(0, 57, 59, "2"); f(3, 57, 59, "4"); G[1][59] = "4"; G[2][59] = "5"
A = ["".join(r) for r in G]

# ---------------- B: 39x7 in hand
B = ["." * 11 + "2" * 10 + "p" * 7 + "3" + "." * 7 + "323",
     "q" + "pppppppp" + "qq" + "3355555133" + "qqqqqqq" + "3" + "2" * 7 + "333",
     "rqqqqqqqqq" + "q" + "4222224444" + "rrrrrrr" + "4" + "4" * 7 + "434",
     "rqqqqqqqq" + "r" + "." + "3.23" + "5" + "pqqq" + "5" + "." * 18,
     ".rqqqqqqr" + ".." + "3333" + "." + "pqqq" + "." * 19,
     ".rqqqr" + "." * 11 + "pqqq" + "." * 18,
     "..rr" + "." * 13 + "rrrr" + "." * 18]
GRIP = (9.0, 3.0); SUPPORT = (24.0, 2.0); MUZZLE = (39, 1.5)
