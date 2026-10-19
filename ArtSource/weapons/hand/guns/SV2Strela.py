# SV-2 Strela (SVD Dragunov pattern): wooden thumbhole skeleton stock with cheek rest, blued receiver,
# PSO-1-style scope on the side bracket, 10-rd steel box mag, vented wooden handguard, gas block, long thin
# barrel with front-sight block and slotted flash hider.
from common import grid, fill, put
CAT = "sniper"

# ---------------- A: 62x13 menu sprite
G = grid(62, 13)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# stock: butt, comb + cheek rest, thumbhole, bottom bar, raked grip, wrist
for y in range(5, 12): f(y, 0, 2, "c"); G[y][0] = "4"
G[5][0] = "3"; G[5][1] = G[5][2] = "b"
f(4, 4, 9, "b"); G[4][4] = "c"; f(5, 3, 18, "b"); f(6, 3, 18, "c"); f(5, 4, 9, "c"); f(6, 4, 9, "d")
f(10, 1, 12, "c"); f(11, 1, 12, "d"); G[10][1] = "b"; f(10, 3, 9, "b")
GP = {7: (13, 16), 8: (13, 16), 9: (12, 15), 10: (12, 15), 11: (12, 14)}
for y, (a, b) in GP.items(): f(y, a, b, "c"); G[y][a] = "b"; G[y][b] = "d"
f(12, 12, 14, "d")
for x in range(14, 19): G[6][x] = "c"
G[6][14] = "b"
# receiver
f(4, 19, 29, "2"); f(4, 20, 25, "1"); f(5, 19, 29, "3"); f(6, 19, 29, "4"); f(7, 19, 29, "5")
put(G, 6, 21, "22222"); put(G, 6, 28, "1")                # safety lever, bolt handle
# PSO-1 scope on side bracket
put(G, 1, 9, "q"); put(G, 2, 9, "r"); put(G, 1, 10, "q"); put(G, 2, 10, "r")
f(1, 11, 31, "2"); f(1, 13, 17, "1"); f(2, 11, 31, "4")
f(0, 20, 22, "3"); G[0][21] = "2"; f(1, 19, 24, "3"); f(2, 19, 24, "4")
put(G, 1, 32, "k"); put(G, 2, 32, "l"); f(3, 21, 26, "5")
# trigger + guard
put(G, 8, 18, "2"); G[8][21] = "4"; f(9, 17, 21, "4"); G[9][17] = "3"
# magazine
MAG = {8: (22, 27), 9: (22, 27), 10: (23, 28), 11: (23, 28)}
for y, (a, b) in MAG.items(): f(y, a, b, "4"); G[y][a] = "3"; G[y][b] = "5"
f(12, 23, 28, "5"); G[9][25] = G[10][25] = G[11][26] = "5"
# vented wooden handguard
f(4, 31, 44, "b"); f(5, 30, 45, "c"); f(6, 30, 45, "d")
for x in (33, 34, 37, 38, 41, 42): G[5][x] = "d"
G[4][32] = "a"; G[4][33] = "a"
# gas block, barrel, front sight, flash hider
f(4, 46, 48, "2"); f(5, 46, 48, "3"); f(6, 46, 48, "4")
f(5, 49, 56, "3")
put(G, 3, 57, "3"); put(G, 4, 56, "333"); put(G, 2, 57, "3")
put(G, 5, 57, "21212"); put(G, 6, 57, "45454")
A = ["".join(r) for r in G]

# ---------------- B: 44x9 in hand
H = grid(44, 9)
h = lambda y, a, b, c: fill(H, y, a, b, c)
for y in range(4, 9): H[y][0] = "4"; H[y][1] = "c"
H[4][1] = "b"
h(3, 3, 6, "b"); h(4, 2, 12, "b"); h(4, 3, 6, "c")
h(7, 1, 8, "c"); h(8, 1, 8, "d")
for y, (a, b) in {5: (9, 11), 6: (9, 11), 7: (8, 10), 8: (8, 10)}.items(): h(y, a, b, "c"); H[y][a] = "b"; H[y][b] = "d"
put(H, 0, 6, "q"); put(H, 1, 6, "r"); h(0, 7, 22, "2"); h(0, 9, 11, "1"); h(1, 7, 22, "4"); h(0, 13, 16, "3"); h(1, 13, 16, "4")
put(H, 0, 23, "k"); put(H, 1, 23, "l"); h(2, 14, 17, "5")
h(3, 13, 20, "2"); h(3, 14, 16, "1"); h(4, 13, 20, "4"); h(5, 13, 20, "5"); put(H, 4, 19, "1")
put(H, 6, 13, "2"); h(7, 12, 14, "4")                     # trigger + guard
for y in range(6, 9): h(y, 15, 19, "4"); H[y][15] = "3"; H[y][19] = "5"
h(8, 16, 19, "5")
h(3, 22, 30, "b"); h(4, 21, 31, "c"); h(5, 21, 31, "d")
for x in (23, 26, 29): H[4][x] = "d"
h(3, 32, 33, "2"); h(4, 32, 33, "3"); h(5, 32, 33, "4")
h(4, 34, 40, "3"); put(H, 3, 40, "3"); put(H, 2, 40, "3")
put(H, 4, 41, "212"); put(H, 5, 41, "454")
B = ["".join(r) for r in H]
GRIP = (9.5, 6.5); SUPPORT = (26.0, 6.5); MUZZLE = (44, 5.0)
