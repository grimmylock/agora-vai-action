# Corvan marksman (SR-25 / Mk11 pattern): black A2-style fixed stock, AR upper/lower, OD-green variable
# scope on rings, free-float quad-rail handguard with a folded bipod, 20-rd 7.62 mag, can suppressor.
from common import grid, fill, put
CAT = "sniper"

# ---------------- A: 60x15 menu sprite
G = grid(60, 15)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# A2 stock
BOT = {0: 9, 1: 10, 2: 10, 3: 10, 4: 9, 5: 9, 6: 8, 7: 8, 8: 8, 9: 7, 10: 7, 11: 7, 12: 6}
for x, b in BOT.items():
    for y in range(5, b + 1): G[y][x] = "q"
    G[5][x] = "p"; G[b][x] = "r"
for y in range(5, 10): G[y][0] = "4"
G[5][0] = "3"; f(7, 2, 7, "r"); put(G, 11, 2, "4")       # butt plate, flute, sling swivel
# scope (OD green): ocular bell, tube, turret, objective bell, lens, rings
for x in list(range(12, 16)) + list(range(26, 31)):
    G[0][x] = "o"; G[1][x] = "O"; G[2][x] = "O"; G[3][x] = "e"
f(1, 16, 25, "o"); f(2, 16, 25, "e"); f(0, 19, 21, "O"); G[0][20] = "o"
put(G, 0, 31, "L"); put(G, 1, 31, "k"); put(G, 2, 31, "l"); put(G, 3, 31, "L")
f(0, 12, 12, "."); f(3, 12, 12, ".")
G[3][17] = G[3][24] = "4"
# upper receiver with rail, lower receiver
for x in range(13, 29): G[4][x] = "2" if x % 2 else "3"
f(5, 13, 28, "4"); f(6, 13, 28, "4"); f(5, 18, 23, "5"); G[5][13] = "3"
f(7, 13, 25, "3"); f(8, 13, 25, "4"); put(G, 6, 26, "1")  # lower, forward assist
# pistol grip, trigger + guard, magazine
GR = {9: (14, 17), 10: (14, 17), 11: (13, 16), 12: (13, 16), 13: (12, 15)}
for y, (a, b) in GR.items(): f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "r"
G[11][15] = "r"; G[10][16] = "r"; f(14, 12, 15, "r")
put(G, 9, 19, "2"); f(10, 18, 20, "4"); G[10][18] = "3"
for y in range(9, 14):
    f(y, 21, 25, "4"); G[y][21] = "3"; G[y][25] = "5"
f(14, 21, 25, "5"); G[10][23] = "5"; G[12][23] = "5"
# quad-rail handguard + folded bipod
for x in range(29, 45):
    G[3][x] = "2" if x % 2 else "4"; G[4][x] = "3"; G[5][x] = "4"; G[6][x] = "3" if x % 2 else "5"; G[7][x] = "4"
    G[8][x] = "3" if x % 2 else "5"
f(9, 33, 43, "4"); f(9, 31, 32, "5"); put(G, 9, 44, "3")
# gas block, barrel, suppressor
f(4, 45, 47, "3"); f(5, 45, 47, "4"); f(4, 48, 49, "2"); f(5, 48, 49, "4")
f(3, 50, 59, "2"); f(4, 50, 59, "3"); f(5, 50, 59, "3"); f(6, 50, 59, "4"); f(3, 52, 57, "1"); G[4][50] = G[5][50] = "4"
A = ["".join(r) for r in G]

# ---------------- B: 42x10 in hand
H = grid(42, 10)
h = lambda y, a, b, c: fill(H, y, a, b, c)
BB = {0: 6, 1: 7, 2: 7, 3: 6, 4: 6, 5: 6, 6: 5, 7: 5, 8: 5}
for x, b in BB.items():
    for y in range(4, b + 1): H[y][x] = "q"
    H[4][x] = "p"; H[b][x] = "r"
for y in range(4, 7): H[y][0] = "4"
for x in (9, 10, 18, 19, 20, 21): H[0][x] = "o"; H[1][x] = "O"; H[2][x] = "e"
h(1, 11, 17, "o"); h(2, 11, 17, "e"); H[0][14] = "O"
put(H, 0, 22, "L"); put(H, 1, 22, "k"); put(H, 2, 22, "l")
for x in range(9, 20): H[3][x] = "2" if x % 2 else "3"
h(4, 9, 19, "4"); h(4, 13, 16, "5"); h(5, 9, 17, "4")
for y, (a, b) in {6: (10, 12), 7: (9, 11), 8: (9, 11)}.items(): h(y, a, b, "q"); H[y][a] = "p"; H[y][b] = "r"
h(9, 9, 10, "r")
put(H, 6, 13, "2"); h(7, 12, 14, "4")
for y in range(6, 10): h(y, 15, 18, "4"); H[y][15] = "3"; H[y][18] = "5"
h(9, 15, 18, "5")
for x in range(20, 31): H[3][x] = "2" if x % 2 else "4"; H[4][x] = "4"; H[5][x] = "3" if x % 2 else "5"
h(6, 22, 30, "4")
h(4, 31, 33, "3"); h(3, 34, 41, "2"); h(4, 34, 41, "3"); h(5, 34, 41, "4"); h(3, 36, 39, "1")
B = ["".join(r) for r in H]
GRIP = (10.5, 7.0); SUPPORT = (25.0, 6.5); MUZZLE = (42, 4.5)
