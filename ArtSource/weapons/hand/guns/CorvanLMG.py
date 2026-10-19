# Corvan LMG (M249 / Mk46 pattern). The reference photo is tilted, so this is a clean level profile:
# collapsible black stock, boxy receiver with the feed-tray cover and a compact prism optic, polymer
# handguard over the gas tube, barrel carry handle, front sight, birdcage hider, folded bipod, pistol grip
# and the OD 100-rd soft ammo pouch hanging under the feed.
from common import grid, fill, put
CAT = "heavy"

# ---------------- A: 60x17 menu sprite
G = grid(60, 17)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# collapsible stock + buffer tube
BOT = {0: 13, 1: 13, 2: 12, 3: 12, 4: 12, 5: 11, 6: 11, 7: 11, 8: 10, 9: 10}
for x, b in BOT.items():
    for y in range(6, b + 1): G[y][x] = "q"
    G[6][x] = "p"; G[b][x] = "r"
for y in range(6, 14): G[y][0] = "r"
f(9, 2, 7, "r"); f(10, 2, 6, "p")
f(7, 10, 12, "3"); f(8, 10, 12, "4")                      # buffer tube
# receiver + feed-tray cover
f(3, 13, 30, "2"); f(4, 13, 30, "3")
for y in range(5, 10): f(y, 13, 30, "4")
f(10, 13, 30, "5"); f(5, 13, 27, "5"); G[5][28] = "1"
f(7, 17, 27, "5"); G[7][20] = "2"; f(8, 22, 26, "3")
# compact prism optic
f(0, 17, 23, "p"); f(1, 15, 24, "q"); f(2, 16, 24, "r"); G[1][15] = "r"
put(G, 0, 24, "l"); put(G, 1, 25, "l"); put(G, 0, 25, "k"); f(2, 18, 21, "5")
# barrel, carry handle, handguard, gas tube/block, front sight, hider
f(5, 31, 54, "2"); f(6, 31, 54, "4")
f(2, 33, 40, "3"); G[2][33] = G[2][40] = "4"
for y in (3, 4): G[y][33] = "4"; G[y][40] = "4"
f(7, 31, 42, "p"); f(8, 31, 42, "q"); f(9, 31, 42, "r")
for x in range(32, 42, 2): G[7][x] = "q"
f(8, 43, 47, "3"); f(9, 43, 47, "5")
f(7, 48, 50, "3"); f(8, 48, 50, "4"); f(9, 48, 50, "5")
put(G, 2, 49, "3"); put(G, 3, 49, "3"); put(G, 4, 48, "333")
put(G, 4, 55, "3333"); put(G, 5, 55, "21212"); put(G, 6, 55, "45454"); put(G, 7, 55, "4444")
# folded bipod
f(10, 36, 49, "4"); put(G, 10, 50, "3"); G[10][35] = "5"
# pistol grip, trigger + guard
GR = {11: (17, 20), 12: (17, 20), 13: (16, 19), 14: (16, 19), 15: (15, 18)}
for y, (a, b) in GR.items(): f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "r"
f(16, 15, 17, "r"); G[13][18] = G[14][17] = "r"
put(G, 11, 22, "2"); G[11][24] = "4"; G[12][24] = "4"; f(13, 21, 24, "4")
# soft ammo pouch
for y in range(11, 17): f(y, 25, 31, "O"); G[y][25] = "o"; G[y][31] = "e"
f(11, 25, 31, "o"); f(16, 26, 30, "e"); G[16][25] = G[16][31] = "."; f(13, 26, 30, "e"); G[12][28] = "4"
A = ["".join(r) for r in G]

# ---------------- B: 41x13 in hand
H = grid(41, 13)
h = lambda y, a, b, c: fill(H, y, a, b, c)
BB = {0: 10, 1: 9, 2: 9, 3: 9, 4: 8, 5: 8, 6: 8, 7: 7, 8: 7}
for x, b in BB.items():
    for y in range(4, b + 1): H[y][x] = "q"
    H[4][x] = "p"; H[b][x] = "r"
for y in range(4, 11): H[y][0] = "r"
h(6, 1, 4, "r")
h(0, 11, 16, "p"); h(1, 10, 17, "r"); put(H, 0, 17, "k"); put(H, 1, 18, "l"); h(2, 12, 15, "5")
h(3, 9, 21, "2"); h(4, 9, 21, "3"); h(5, 9, 21, "4"); h(6, 9, 21, "4"); h(7, 9, 21, "5")
h(5, 9, 19, "5"); H[5][20] = "1"; put(H, 6, 13, "2")
h(4, 22, 37, "2"); h(5, 22, 37, "4")
h(2, 24, 29, "3"); H[3][24] = "4"; H[3][29] = "4"
h(6, 22, 29, "p"); h(7, 22, 29, "r"); h(6, 30, 33, "3"); put(H, 2, 34, "3"); put(H, 3, 34, "3")
put(H, 4, 38, "212"); put(H, 5, 38, "454")
h(8, 25, 33, "4")
for y, (a, b) in {8: (11, 13), 9: (11, 13), 10: (10, 12), 11: (10, 12)}.items(): h(y, a, b, "q"); H[y][a] = "p"; H[y][b] = "r"
h(12, 10, 11, "r")
put(H, 8, 15, "2"); H[8][17] = "4"; h(9, 14, 17, "4")
for y in range(8, 12): h(y, 18, 22, "O"); H[y][18] = "o"; H[y][22] = "e"
h(8, 18, 22, "o"); h(12, 19, 21, "e"); h(10, 19, 21, "e")
B = ["".join(r) for r in H]
GRIP = (11.5, 10.0); SUPPORT = (27.0, 8.5); MUZZLE = (41, 5.0)
