# Smerch-3 (9A-91 / SR-3 pattern compact 9x39) with an integral suppressor: strut stock with tall plate,
# stamped black receiver with rear sight hump, polymer handguard, round charging knob, polymer grip,
# short ribbed mag, fat suppressor can on the muzzle.
from common import grid, fill, put
CAT = "assault"

# ---------------- A: 58x18 menu sprite
G = grid(58, 18)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# strut stock + buttplate
for y in range(3, 13): f(y, 0, 1, "4")
for y in range(3, 13): G[y][0] = "3"
G[3][1] = "3"; G[12][0] = "5"; G[12][1] = "5"
for x in range(2, 20):
    y = 7 - round((x - 2) * 2 / 17)
    G[y][x] = "2"; G[y + 1][x] = "4"
# receiver: lit top, rear-sight hump, port, bolt, selector
for y in range(2, 8): f(y, 19, 33, "3")
f(2, 19, 33, "2"); f(5, 19, 33, "4"); f(6, 19, 33, "4"); f(7, 20, 33, "5")
put(G, 0, 30, "3"); put(G, 1, 29, "333"); G[1][29] = "2"
f(3, 24, 28, "5"); put(G, 3, 29, "1"); put(G, 4, 20, "22221"); G[2][19] = "3"
# polymer handguard with vent slots, front sight
for y in range(2, 8): f(y, 34, 43, "q")
f(2, 34, 43, "p"); f(7, 34, 43, "r")
for x in range(36, 43, 2): G[4][x] = "r"; G[5][x] = "r"
put(G, 0, 42, "3"); put(G, 1, 42, "33"); G[1][42] = "2"
# charging knob disk
for (y, a, b) in ((7, 38, 40), (8, 37, 41), (9, 37, 41), (10, 38, 40)): f(y, a, b, "4")
G[7][38] = "3"; G[8][37] = "3"; G[8][38] = "2"; G[10][40] = "5"; G[9][41] = "5"; G[9][39] = "5"
# polymer pistol grip
GR = {8: (21, 24), 9: (21, 24), 10: (20, 24), 11: (20, 23), 12: (20, 23), 13: (19, 23), 14: (19, 22)}
for y, (a, b) in GR.items(): f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "r"
f(14, 19, 22, "r"); G[10][22] = "r"; G[12][21] = "r"
# trigger guard (hole open) + trigger
for y in (8, 9): G[y][30] = "3"
f(10, 25, 30, "3"); put(G, 8, 26, "4"); put(G, 9, 26, "4")
# ribbed box mag, slight forward cant
for y in range(8, 17):
    a = 31 + (y - 8) // 4; b = a + 5
    f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "r"
    if y % 2 == 0 and 9 < y < 16: f(y, a + 2, b - 1, "r")
f(17, 33, 38, "r")
# muzzle stub + long fat suppressor can: lit top, end caps, seam rings
f(4, 44, 44, "2"); f(5, 44, 44, "4")
for y in range(2, 8):
    c = "2" if y < 3 else "3" if y < 5 else "4" if y < 7 else "5"
    f(y, 45, 57, c)
f(2, 47, 55, "1"); G[2][57] = "."; G[7][57] = "."
for y in range(2, 8): G[y][45] = "4" if y < 7 else "5"; G[y][46] = "3" if y < 5 else G[y][46]
for y in range(3, 7): G[y][57] = "4"; G[y][50] = "4" if y > 2 else "3"; G[y][54] = "4"
G[2][45] = "3"
A = ["".join(r) for r in G]

# ---------------- B: 27x10 in hand
B = ["." * 12 + "3" + "..." + "3" + "." * 10,
     ".." + "......" + "222222" + "pppp" + "3" + "21111112",
     "34" + "......" + "355513" + "qrqr" + "4" + "33343334",
     "34" + "222222" + "444444" + "qqqq" + "4" + "33343334",
     "34" + "444444" + "555555" + "rrrr" + "4" + "54454445",
     "34" + "......." + "pq" + ".3" + "pqr" + "44" + "." * 9,
     "34" + "......." + "pq" + "33" + "pqr" + "." * 11,
     "55" + "......" + "pqr" + ".." + "pqr" + "." * 11,
     "." * 8 + "rr" + "...." + "pqr" + "." * 10,
     "." * 14 + "rrr" + "." * 10]
GRIP = (9.5, 6.5); SUPPORT = (16.0, 4.0); MUZZLE = (27, 2.5)
