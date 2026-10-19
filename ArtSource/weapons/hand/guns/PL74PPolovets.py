# PL-74P Polovets (PKP Pecheneg pattern). The reference photo is weak (slings, bipod deployed), so this
# is a clean profile from the real gun: black polymer skeleton stock and grip, black receiver, OD 100-rd
# box, finned barrel shroud with vent holes and the long loop carry handle on top, heavy barrel with
# front sight and flash hider, bipod folded back under the muzzle end.
# ZULU = True adds a 4x scope on the receiver and a laser module (red emitter) under the shroud.
from common import grid, fill, put
CAT = "heavy"
ZULU = False

def poly(G, x, t, b):
    for y in range(t, b + 1): G[y][x] = "q"
    G[t][x] = "p"; G[b][x] = "r"

# ---------------- A: 60x15 menu sprite
G = grid(60, 15)
f = lambda y, a, b, c: fill(G, y, a, b, c)
BOT = {1: 11, 2: 11, 3: 11, 4: 11, 5: 11, 6: 11, 7: 11, 8: 11, 9: 10, 10: 9, 11: 8, 12: 7, 13: 7}
for x, b in BOT.items(): poly(G, x, 5, b)
for y in range(5, 12): G[y][0] = "r"
f(7, 4, 9, "."); f(8, 3, 10, "."); f(9, 4, 9, "."); f(6, 3, 10, "r")      # stock slot
# receiver
f(3, 14, 30, "2"); f(3, 16, 22, "1"); f(4, 14, 30, "4"); f(5, 14, 30, "4"); f(6, 14, 30, "4"); f(7, 14, 30, "5")
f(4, 16, 28, "3"); f(5, 16, 28, "5"); put(G, 6, 29, "1"); G[3][14] = "3"
if not ZULU: put(G, 2, 22, "33")
# pistol grip, trigger guard, ammo box
GR = {8: (15, 18), 9: (15, 18), 10: (14, 17), 11: (14, 17), 12: (13, 16)}
for y, (a, b) in GR.items(): f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "r"
f(13, 13, 15, "r"); G[10][16] = "r"
put(G, 8, 20, "2"); G[8][22] = "4"; f(9, 19, 22, "4")
for y in range(8, 15): f(y, 23, 29, "O"); G[y][23] = "o"; G[y][29] = "e"
f(8, 23, 29, "o"); f(14, 23, 29, "e"); f(11, 24, 28, "e"); G[8][29] = "O"
# finned shroud with vent holes + loop carry handle
f(3, 31, 47, "2"); f(4, 31, 47, "4"); f(5, 31, 47, "4"); f(6, 31, 47, "5")
for x in range(32, 47, 3): G[4][x] = "5"; G[5][x] = "5"; G[4][x + 1] = "3"
f(0, 33, 41, "q"); G[0][33] = "p"
for y in (1, 2): put(G, y, 32, "q"); put(G, y, 41, "qr")
G[0][41] = "q"
# heavy barrel, front sight, hider, folded bipod
f(3, 48, 55, "2"); f(4, 48, 55, "4"); f(3, 48, 50, "3")
put(G, 2, 54, "33"); put(G, 1, 54, "3"); put(G, 0, 54, "3")
put(G, 3, 56, "2121"); put(G, 4, 56, "4545")
f(5, 46, 55, "4"); put(G, 5, 45, "3"); G[5][55] = "5"
if ZULU:
    # 4x scope on a side mount + laser module
    put(G, 0, 15, "pp"); put(G, 1, 15, "qq"); put(G, 2, 15, "r.")
    f(0, 17, 27, "3"); f(0, 18, 22, "2"); f(1, 17, 27, "4"); f(0, 21, 23, "q")
    put(G, 0, 28, "k"); put(G, 1, 28, "l"); f(2, 19, 25, "5")
    f(7, 41, 46, "q"); f(8, 41, 46, "r"); put(G, 7, 47, "R"); G[7][41] = "p"
A = ["".join(r) for r in G]

# ---------------- B: 43x11 in hand
H = grid(43, 11)
h = lambda y, a, b, c: fill(H, y, a, b, c)
BB = {1: 8, 2: 8, 3: 8, 4: 8, 5: 8, 6: 7, 7: 6, 8: 5, 9: 5}
for x, b in BB.items(): poly(H, x, 3, b)
for y in range(3, 9): H[y][0] = "r"
h(5, 2, 5, "."); h(6, 3, 5, "."); h(4, 2, 6, "r")
h(2, 10, 21, "2"); h(2, 11, 14, "1"); h(3, 10, 21, "4"); h(4, 10, 21, "4"); h(5, 10, 21, "5"); h(3, 12, 18, "5")
for y, (a, b) in {6: (11, 13), 7: (11, 13), 8: (10, 12), 9: (10, 12)}.items(): h(y, a, b, "q"); H[y][a] = "p"; H[y][b] = "r"
h(9, 10, 12, "r")
put(H, 6, 14, "2"); h(7, 13, 15, "4")
for y in range(6, 11): h(y, 16, 20, "O"); H[y][16] = "o"; H[y][20] = "e"
h(6, 16, 20, "o"); h(10, 16, 20, "e")
h(2, 22, 33, "2"); h(3, 22, 33, "4"); h(4, 22, 33, "4"); h(5, 22, 33, "5")
for x in (24, 27, 30): H[3][x] = "5"; H[4][x] = "5"
h(0, 23, 29, "q"); H[1][23] = "q"; H[1][29] = "r"
h(3, 34, 39, "2"); h(4, 34, 39, "4"); put(H, 2, 38, "3"); put(H, 1, 38, "3")
put(H, 3, 40, "212"); put(H, 4, 40, "454")
h(5, 33, 39, "4")
if ZULU:
    put(H, 0, 10, "p"); put(H, 1, 10, "r"); h(0, 11, 18, "3"); h(0, 12, 14, "2"); h(1, 11, 18, "4")
    put(H, 0, 19, "k"); put(H, 1, 19, "l")
    h(6, 29, 32, "q"); put(H, 6, 33, "R")
B = ["".join(r) for r in H]
GRIP = (11.5, 7.5); SUPPORT = (27.0, 6.5); MUZZLE = (43, 4.0)
