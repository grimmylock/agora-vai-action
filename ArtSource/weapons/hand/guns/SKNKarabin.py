# SKN karabin (SKS pattern): one-piece walnut stock with semi-pistol wrist, blued receiver and bolt
# carrier, wooden gas-tube cover, fixed box magazine, bright trigger guard, folded blade bayonet under the
# barrel and the hooded front-sight post at the muzzle.
from common import grid, fill, put
CAT = "sniper"

def wood(G, x, t, b):
    for y in range(t, b + 1): G[y][x] = "c"
    G[t][x] = "b"; G[b][x] = "d"

# ---------------- A: 60x11 menu sprite
G = grid(60, 12)
f = lambda y, a, b, c: fill(G, y, a, b, c)
BOT = {1: 10, 2: 10, 3: 10, 4: 9, 5: 9, 6: 9, 7: 9, 8: 8, 9: 8, 10: 8, 11: 7, 12: 7, 13: 7, 14: 6, 15: 6, 16: 6, 17: 6}
for x, b in BOT.items(): wood(G, x, 4 if x < 14 else 3, b)
for x in range(18, 32): wood(G, x, 3, 5)                  # stock under the receiver
for x in range(32, 45): wood(G, x, 3, 4)   # lower handguard
for y in range(4, 11): G[y][0] = "4"
G[4][0] = "3"; G[10][0] = "5"
f(6, 2, 9, "d"); f(7, 2, 8, "b"); G[3][14] = "b"                           # comb shadow line
# receiver + bolt carrier + rear sight
f(1, 17, 31, "2"); f(1, 19, 25, "1"); f(2, 17, 31, "4"); f(2, 23, 30, "3")
put(G, 2, 29, "11"); G[2][17] = "5"
put(G, 0, 30, "3"); put(G, 0, 31, "3")
# upper wooden handguard (gas-tube cover)
f(1, 32, 44, "b"); f(2, 32, 44, "d")
# gas block, barrel, front sight
f(1, 45, 47, "2"); f(2, 45, 47, "4")
f(2, 48, 59, "3"); f(1, 56, 59, "2"); G[0][58] = "3"; G[1][57] = "3"
# folded bayonet under the barrel
f(3, 45, 53, "1"); G[3][45] = "2"; put(G, 3, 54, "33"); put(G, 4, 54, "45"); put(G, 5, 54, "4")
# trigger guard (bright steel loop) + fixed magazine
put(G, 6, 20, "2"); put(G, 7, 20, "3"); G[6][23] = "2"; G[7][23] = "2"; f(8, 19, 23, "2"); G[8][19] = "3"
f(6, 25, 30, "3"); f(7, 25, 30, "4"); f(8, 26, 29, "5"); G[6][25] = "2"
A = ["".join(r) for r in G if "".join(r).strip(".")]   # drop empty rows

# ---------------- B: 41x8 in hand
H = grid(41, 8)
h = lambda y, a, b, c: fill(H, y, a, b, c)
BB = {1: 7, 2: 7, 3: 7, 4: 6, 5: 6, 6: 6, 7: 5, 8: 5, 9: 5, 10: 4, 11: 4, 12: 4}
for x, b in BB.items(): wood(H, x, 3, b)
for x in range(13, 31): wood(H, x, 3, 4); H[3][x] = "c"
for y in range(3, 8): H[y][0] = "4"
h(1, 11, 21, "2"); h(1, 13, 16, "1"); h(2, 11, 21, "4"); h(2, 16, 20, "3"); put(H, 2, 20, "1")
put(H, 0, 21, "3")
h(1, 22, 30, "b"); h(2, 22, 30, "d")
h(1, 31, 32, "2"); h(2, 31, 32, "4")
h(2, 33, 40, "3"); put(H, 1, 39, "33"); put(H, 0, 40, "3")
h(3, 31, 36, "1"); put(H, 3, 37, "3"); put(H, 4, 37, "4")
put(H, 5, 14, "3"); put(H, 5, 16, "2"); h(6, 13, 16, "2")   # trigger guard
h(5, 18, 21, "3"); h(6, 18, 21, "5")                      # fixed magazine
B = ["".join(r) for r in H]
GRIP = (11.5, 4.0); SUPPORT = (26.0, 5.0); MUZZLE = (41, 2.5)
