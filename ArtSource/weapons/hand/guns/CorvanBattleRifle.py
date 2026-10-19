# Corvan battle rifle (FN FAL pattern): black polymer stock, raked pistol grip and vented handguard,
# blued receiver with the folded carry handle on top, straight steel 20-rd box mag, gas block with
# front sight, long barrel with a slotted flash hider.
from common import grid, fill, put
CAT = "assault"

# ---------------- A: 60x14 menu sprite
G = grid(60, 14)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# stock: butt drops slightly, belly sweeps up into the wrist
TOP = {**{x: 4 for x in range(0, 10)}, **{x: 3 for x in range(10, 15)}}
BOT = {0: 10, 1: 10, 2: 10, 3: 9, 4: 9, 5: 9, 6: 8, 7: 8, 8: 8, 9: 7, 10: 7, 11: 7, 12: 6, 13: 6, 14: 6}
for x in TOP:
    t, b = TOP[x], BOT[x]
    for y in range(t, b + 1): G[y][x] = "q"
    G[t][x] = "p"; G[b][x] = "r"
for y in range(4, 11): G[y][0] = "4"
G[4][0] = "3"; f(6, 2, 8, "r"); f(7, 2, 7, "p")           # butt plate, stock flute
# receiver
f(2, 15, 29, "2"); f(2, 18, 23, "1"); put(G, 1, 15, "23")  # top, rear sight
f(3, 15, 29, "4"); f(4, 15, 29, "4"); f(5, 15, 29, "5"); f(6, 21, 29, "5")
f(3, 24, 28, "3"); put(G, 4, 20, "3333"); put(G, 4, 30, "")  # dust-cover bevel
put(G, 4, 27, "1")                                        # charging handle knob
f(0, 24, 29, "p"); put(G, 1, 24, "r"); put(G, 1, 29, "q") # folded carry handle
# pistol grip (raked)
GR = {6: (17, 20), 7: (17, 20), 8: (16, 19), 9: (15, 18), 10: (15, 18)}
for y, (a, b) in GR.items(): f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "r"
f(11, 15, 17, "r"); G[8][18] = "r"; G[9][17] = "r"
# trigger + guard
put(G, 7, 22, "2"); put(G, 8, 22, "3"); G[7][24] = "4"; f(9, 20, 24, "4"); G[8][24] = "4"; G[9][20] = "3"
# straight steel box magazine
for y in range(7, 13): f(y, 25, 29, "3"); G[y][25] = "2"; G[y][29] = "4"
f(13, 25, 29, "5"); G[9][27] = "4"; G[10][27] = "4"; G[11][27] = "4"
# handguard: vented polymer
f(2, 30, 44, "p"); f(3, 30, 44, "q"); f(4, 30, 44, "q"); f(5, 30, 44, "r")
for x in (34, 36, 38, 40): G[3][x] = "r"; G[4][x] = "r"
# gas regulator + front sight, barrel, flash hider
f(2, 45, 47, "2"); f(3, 45, 47, "3"); f(4, 45, 47, "4"); put(G, 1, 46, "3"); put(G, 0, 46, "3")
f(3, 48, 55, "2"); f(4, 48, 55, "4")
put(G, 2, 56, "2222"); put(G, 3, 56, "3535"); put(G, 4, 56, "4545"); put(G, 5, 56, "5555")
A = ["".join(r) for r in G]

# ---------------- B: 42x9 in hand
H = grid(42, 9)
h = lambda y, a, b, c: fill(H, y, a, b, c)
BOT = {0: 7, 1: 7, 2: 7, 3: 6, 4: 6, 5: 5, 6: 5, 7: 4, 8: 4, 9: 4}
for x, b in BOT.items():
    for y in range(3, b + 1): H[y][x] = "q"
    H[3][x] = "p"; H[b][x] = "r"
for y in range(3, 8): H[y][0] = "4"
h(5, 1, 4, "r")
h(2, 9, 20, "2"); h(2, 12, 15, "1"); put(H, 1, 10, "3")
h(3, 9, 20, "4"); h(4, 10, 20, "5"); h(3, 16, 19, "3"); put(H, 3, 19, "1")
h(0, 16, 20, "p"); put(H, 1, 20, "q")                     # carry handle
for y, (a, b) in {5: (12, 14), 6: (11, 13), 7: (10, 12)}.items(): h(y, a, b, "q"); H[y][a] = "p"; H[y][b] = "r"
h(8, 10, 11, "r")
put(H, 5, 15, "2"); h(6, 14, 16, "4")                     # trigger + guard
for y in range(5, 8): h(y, 17, 20, "3"); H[y][17] = "2"; H[y][20] = "4"
h(8, 17, 20, "5")
h(1, 21, 31, "p"); h(2, 21, 31, "q"); h(3, 21, 31, "r")
for x in (25, 27, 29): H[2][x] = "r"
h(1, 32, 34, "2"); h(2, 32, 34, "3"); h(3, 32, 34, "4"); put(H, 0, 33, "3")
h(2, 35, 38, "3"); put(H, 2, 39, "212"); put(H, 3, 39, "535")
B = ["".join(r) for r in H]
GRIP = (12.0, 6.5); SUPPORT = (26.0, 4.5); MUZZLE = (42, 3.0)
