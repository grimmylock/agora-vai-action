# RL-44 (RPD pattern belt-fed LMG): blonde wooden stock with the deep butt and scooped belly, wooden
# pistol grip, blued receiver with raised feed cover, 100-rd drum hanging under the feed, humped wooden
# handguard, heavy barrel over the gas cylinder with brackets, folded bipod, front sight.
from common import grid, fill, put
CAT = "heavy"

def wood(G, x, t, b):
    for y in range(t, b + 1): G[y][x] = "b"
    G[t][x] = "a"; G[b][x] = "d"
    if b - 1 > t: G[b - 1][x] = "c"

# ---------------- A: 60x16 menu sprite
G = grid(60, 17)
f = lambda y, a, b, c: fill(G, y, a, b, c)
BOT = {1: 11, 2: 11, 3: 11, 4: 11, 5: 10, 6: 9, 7: 8, 8: 7, 9: 6, 10: 6, 11: 6, 12: 6}
for x, b in BOT.items(): wood(G, x, 4, b)
for y in range(5, 12): G[y][0] = "4"
G[4][1] = "b"; f(6, 2, 5, "c"); f(7, 2, 4, "a")            # butt flute
# receiver + feed cover + rear sight
f(3, 13, 28, "2"); f(3, 15, 19, "1"); f(4, 13, 28, "4"); f(5, 13, 28, "4"); f(6, 13, 28, "5")
f(2, 21, 27, "3"); f(1, 22, 26, "2"); G[2][27] = "4"; put(G, 2, 16, "3")
put(G, 4, 26, "11"); f(5, 18, 22, "3")
# pistol grip (wood) + trigger guard
GR = {7: (14, 17), 8: (14, 17), 9: (13, 16), 10: (13, 16), 11: (12, 15)}
for y, (a, b) in GR.items(): f(y, a, b, "b"); G[y][a] = "a"; G[y][b] = "c"
f(11, 12, 15, "d"); G[9][15] = G[10][14] = "c"
put(G, 7, 19, "2"); G[7][21] = "4"; f(8, 18, 21, "4")
# drum magazine (round face, lit rim, hub)
DR = {7: (22, 26), 8: (21, 27), 9: (20, 28), 10: (20, 28), 11: (20, 28), 12: (20, 28), 13: (20, 28), 14: (21, 27), 15: (22, 26)}
for y, (a, b) in DR.items():
    f(y, a, b, "4"); G[y][a] = "3"; G[y][b] = "5"
f(7, 22, 26, "3"); f(15, 22, 26, "5"); f(8, 22, 25, "3")
put(G, 11, 23, "323"); put(G, 10, 24, "3"); put(G, 12, 24, "5")
# humped wooden handguard
for x in range(29, 36): wood(G, x, 2 if 30 <= x <= 32 else 3, 6)
# barrel, gas cylinder + brackets, front sight, folded bipod
f(3, 36, 59, "2"); f(4, 36, 59, "4"); f(3, 36, 40, "1")
f(6, 36, 49, "3"); f(7, 36, 49, "5")
for x in (42, 49):
    for y in range(3, 8): G[y][x] = "3"
    G[3][x] = "2"; G[7][x] = "5"
put(G, 2, 57, "33"); put(G, 1, 57, "3"); put(G, 0, 57, "3"); G[1][58] = "3"
f(9, 37, 50, "4"); put(G, 9, 51, "3"); G[8][37] = "5"
A = ["".join(r) for r in G if "".join(r).strip(".")]   # drop empty rows

# ---------------- B: 42x12 in hand
H = grid(42, 12)
h = lambda y, a, b, c: fill(H, y, a, b, c)
BB = {1: 8, 2: 8, 3: 8, 4: 7, 5: 6, 6: 5, 7: 4, 8: 4}
for x, b in BB.items(): wood(H, x, 3, b)
for y in range(3, 9): H[y][0] = "4"
h(5, 1, 3, "c")
h(2, 9, 19, "2"); h(2, 10, 12, "1"); h(3, 9, 19, "4"); h(4, 9, 19, "5"); h(1, 15, 19, "3")
for y, (a, b) in {5: (10, 12), 6: (10, 12), 7: (9, 11), 8: (9, 11)}.items(): h(y, a, b, "b"); H[y][a] = "a"; H[y][b] = "c"
h(8, 9, 11, "d")
put(H, 5, 13, "2"); h(6, 12, 14, "4")
for y, (a, b) in {5: (16, 18), 6: (15, 19), 7: (14, 20), 8: (14, 20), 9: (14, 20), 10: (15, 19), 11: (16, 18)}.items():
    h(y, a, b, "4"); H[y][a] = "3"; H[y][b] = "5"
h(5, 16, 18, "3"); h(11, 16, 18, "5"); put(H, 8, 17, "2")
for x in range(20, 26): wood(H, x, 1 if x in (21, 22) else 2, 4)
h(2, 26, 41, "2"); h(3, 26, 41, "4")
h(5, 26, 34, "3")
for y in range(2, 6): H[y][34] = "3"
put(H, 1, 40, "3"); put(H, 0, 40, "3")
h(6, 26, 34, "4")
B = ["".join(r) for r in H]
GRIP = (10.5, 6.5); SUPPORT = (23.0, 5.0); MUZZLE = (42, 3.0)
