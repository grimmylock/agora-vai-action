# VK-127 Grom (VSSK Vychlop pattern: 12.7 mm suppressed bullpup sniper). The reference photo is weak, so
# this is a clean profile from the real gun: squared polymer butt, stamped receiver with top rail, big
# 5-rd mag behind the pistol grip, scope on a side bracket, and the huge integral suppressor that makes
# up nearly half the length, with a folded bipod under it.
from common import grid, fill, put
CAT = "sniper"

# ---------------- A: 56x16 menu sprite
G = grid(56, 17)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# scope
f(1, 15, 17, "3"); G[1][16] = "2"
put(G, 2, 9, "qq"); put(G, 3, 9, "rr"); f(2, 11, 24, "2"); f(2, 12, 15, "1"); f(3, 11, 24, "4")
f(2, 16, 19, "3"); f(3, 16, 19, "4"); put(G, 2, 25, "k"); put(G, 3, 25, "l")
f(4, 14, 20, "5"); f(5, 15, 19, "5")
# butt (polymer) + receiver with top rail
for x in range(0, 7):
    for y in range(7, 14): G[y][x] = "q"
    G[7][x] = "p"; G[13][x] = "r"
for y in range(7, 14): G[y][0] = "r"
f(10, 2, 5, "r"); f(11, 2, 5, "p")
for x in range(8, 30): G[6][x] = "2" if x % 2 else "3"
f(7, 7, 29, "3"); f(8, 7, 29, "4"); f(9, 7, 29, "4"); f(10, 7, 29, "4"); f(11, 7, 29, "5")
f(8, 20, 26, "5"); f(9, 21, 25, "3")                      # ejection port + bolt carrier
put(G, 9, 10, "2"); put(G, 9, 14, "2")                    # pins
# magazine behind the grip
for y in range(12, 16): f(y, 8, 13, "4"); G[y][8] = "3"; G[y][13] = "5"
f(16, 8, 13, "5"); G[13][10] = G[14][10] = "5"
# pistol grip + trigger guard
GR = {12: (17, 20), 13: (17, 20), 14: (16, 19), 15: (16, 19)}
for y, (a, b) in GR.items(): f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "r"
f(16, 16, 18, "r"); G[14][18] = "r"
put(G, 12, 22, "2"); G[12][24] = "4"; G[13][24] = "4"; f(14, 21, 24, "4")
# integral suppressor (fat, slight rear taper), front sight, folded bipod
for x in range(30, 56):
    t = 6 if x < 34 else 5
    for y in range(t, 11): G[y][x] = "3"
    G[t][x] = "2"; G[t + 1][x] = "1" if 36 <= x <= 52 else "3"; G[9][x] = "4"; G[10][x] = "5"
for x in (34, 45):
    for y in range(6, 10): G[y][x] = "4"
for y in range(5, 11): G[y][55] = "4"
G[5][55] = "3"; put(G, 4, 52, "3"); put(G, 4, 31, "3")
f(11, 31, 41, "4"); put(G, 11, 42, "3"); put(G, 11, 30, "5")
A = ["".join(r) for r in G if "".join(r).strip(".")]   # drop empty rows

# ---------------- B: 42x13 in hand
H = grid(42, 13)
h = lambda y, a, b, c: fill(H, y, a, b, c)
put(H, 1, 6, "qq"); put(H, 2, 6, "rr"); h(1, 8, 18, "2"); h(1, 9, 10, "1"); h(2, 8, 18, "4"); h(0, 11, 13, "3")
put(H, 1, 19, "k"); put(H, 2, 19, "l"); h(3, 10, 15, "5")
for x in range(6, 23): H[4][x] = "2" if x % 2 else "3"
h(5, 4, 22, "3"); h(6, 4, 22, "4"); h(7, 4, 22, "4"); h(8, 4, 22, "5"); h(6, 15, 19, "5")
for x in range(0, 5):
    for y in range(5, 11): H[y][x] = "q"
    H[5][x] = "p"; H[10][x] = "r"
for y in range(5, 11): H[y][0] = "r"
h(7, 1, 3, "r")
for y in range(9, 12): h(y, 6, 9, "4"); H[y][6] = "3"; H[y][9] = "5"
h(12, 6, 9, "5")
for y, (a, b) in {9: (12, 14), 10: (12, 14), 11: (11, 13)}.items(): h(y, a, b, "q"); H[y][a] = "p"; H[y][b] = "r"
h(12, 11, 13, "r")
put(H, 9, 16, "2"); h(10, 15, 18, "4"); H[9][18] = "4"
for x in range(23, 42):
    t = 4 if x < 26 else 3
    for y in range(t, 9): H[y][x] = "3"
    H[t][x] = "2"; H[t + 1][x] = "1" if 27 <= x <= 38 else "3"; H[7][x] = "4"; H[8][x] = "5"
for y in range(4, 8): H[y][31] = "4"
for y in range(3, 9): H[y][41] = "4"
put(H, 2, 39, "3"); h(9, 24, 31, "4")
B = ["".join(r) for r in H]
GRIP = (13.0, 10.5); SUPPORT = (25.0, 9.5); MUZZLE = (42, 6.0)
