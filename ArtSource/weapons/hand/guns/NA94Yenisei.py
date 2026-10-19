# NA-94 Yenisei (AN-94 pattern): black polymer triangular stock/grip/handguard, blued receiver with the tall
# rear-sight drum, ribbed handguard, offset low barrel with front-sight block and muzzle device, curved AK mag.
from common import grid, fill, put
CAT = "assault"

# ---------------- A: 58x16 menu sprite
G = grid(58, 16)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# stock: lit comb, sloping belly, buttpad
BOT = {0: 9, 1: 9, 2: 9, 3: 9, 4: 8, 5: 8, 6: 8, 7: 8, 8: 7, 9: 7, 10: 7, 11: 6, 12: 6, 13: 6, 14: 6, 15: 6}
for x, b in BOT.items():
    for y in range(3, b + 1): G[y][x] = "q"
    G[3][x] = "p"; G[b][x] = "r"
for y in range(3, 10): G[y][0] = "r"
G[3][0] = "q"
f(5, 3, 11, "r"); f(6, 3, 9, "p")                       # lightening groove: shadow over lit lip
put(G, 3, 14, "4"); put(G, 4, 14, "3"); put(G, 5, 14, "4")   # folding hinge
# receiver
put(G, 0, 20, "23"); f(1, 19, 22, "3"); G[1][19] = "2"       # rear sight drum
f(1, 23, 34, "2"); f(1, 24, 30, "1")
f(2, 16, 34, "3"); G[2][16] = "4"
f(3, 15, 34, "4"); f(4, 15, 34, "4"); f(5, 15, 34, "5")
f(3, 25, 31, "5")                                         # ejection port
put(G, 4, 18, "222221")                                   # selector
put(G, 3, 32, "11")                                       # bolt handle
# pistol grip
GR = {6: (16, 20), 7: (16, 20), 8: (16, 19), 9: (15, 19), 10: (15, 19), 11: (15, 18)}
for y, (a, b) in GR.items():
    f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "r"
    if y in (8, 10): G[y][a + 2] = "r"
f(12, 15, 18, "r")
# trigger guard + trigger
put(G, 6, 22, "2"); put(G, 7, 22, "3"); G[6][25] = "4"; G[7][25] = "4"; f(8, 21, 25, "4"); G[8][21] = "3"
# magazine (curved, black polymer, lit spine)
MAG = {6: (26, 31), 7: (26, 31), 8: (27, 32), 9: (27, 32), 10: (28, 32), 11: (28, 33), 12: (29, 33), 13: (29, 33), 14: (30, 33)}
for y, (a, b) in MAG.items():
    f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "r"
f(6, 27, 30, "r"); G[9][28] = "p"; G[10][29] = "p"
f(15, 30, 33, "r")
# handguard: ribbed polymer
f(2, 35, 44, "p"); f(3, 35, 46, "q"); f(4, 35, 46, "q"); f(5, 35, 45, "r")
for x in (37, 39, 41, 43): G[3][x] = "r"; G[4][x] = "r"
G[2][35] = "q"
# barrel, gas/front-sight block, muzzle device
f(3, 47, 51, "2"); f(4, 47, 51, "4")
for y in (2, 3, 4): f(y, 48, 50, "3")
f(2, 48, 50, "2"); put(G, 0, 49, "3"); put(G, 1, 49, "3")
f(5, 47, 49, "4")                                         # bayonet lug
put(G, 3, 52, "211112"); put(G, 4, 52, "353534")          # muzzle device
put(G, 2, 52, "3"); put(G, 5, 52, "4")
A = ["".join(r) for r in G]

# ---------------- B: 40x11 in hand
H = grid(40, 11)
h = lambda y, a, b, c: fill(H, y, a, b, c)
BB = {0: 6, 1: 6, 2: 6, 3: 5, 4: 5, 5: 5, 6: 5, 7: 4, 8: 4, 9: 4, 10: 4}
for x, b in BB.items():
    for y in range(2, b + 1): H[y][x] = "q"
    H[2][x] = "p"; H[b][x] = "r"
for y in range(2, 7): H[y][0] = "r"
h(4, 2, 6, "r")                                           # groove
put(H, 0, 14, "2"); h(1, 13, 15, "3")                     # rear sight drum
h(1, 16, 23, "2"); h(1, 17, 20, "1")
h(2, 11, 23, "3"); h(3, 11, 23, "4"); h(4, 11, 23, "5")
h(3, 17, 21, "5"); put(H, 3, 13, "21")                    # port, selector
for y, (a, b) in {5: (11, 13), 6: (11, 13), 7: (10, 12), 8: (10, 12)}.items():
    h(y, a, b, "q"); H[y][a] = "p"; H[y][b] = "r"
h(9, 10, 12, "r")
put(H, 5, 15, "2"); h(6, 14, 16, "4")                     # trigger + guard (holes at 14,5 / 16,5)
for y, (a, b) in {5: (17, 20), 6: (17, 20), 7: (18, 21), 8: (18, 21), 9: (19, 21)}.items():
    h(y, a, b, "q"); H[y][a] = "p"; H[y][b] = "r"
h(10, 19, 21, "r"); h(5, 18, 19, "r")
h(1, 24, 30, "p"); h(2, 24, 32, "q"); h(3, 24, 32, "q"); h(4, 24, 31, "r")
for x in (26, 28, 30): H[2][x] = "r"
h(1, 33, 35, "2"); h(2, 33, 35, "3"); h(3, 33, 35, "4"); put(H, 0, 34, "3")
put(H, 2, 36, "2112"); put(H, 3, 36, "4535"); h(4, 33, 34, "4")
B = ["".join(r) for r in H]
GRIP = (11.5, 7.0); SUPPORT = (28.0, 5.0); MUZZLE = (40, 3.0)
