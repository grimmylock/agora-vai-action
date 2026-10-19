# Hunting knife (drop point, burl-wood scales, polished bolster with finger guard).
from common import grid, fill, put
CAT = "melee"

# ---------------- A: 54x9 menu sprite
G = grid(54, 9)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# handle: burl wood, lit top, dark belly, lanyard hole, two steel pins
f(1, 4, 18, "a"); G[2][2] = G[2][3] = "a"
for y in range(2, 7): f(y, 1 if y > 2 else 2, 18, "b")
f(6, 1, 18, "c"); f(7, 2, 12, "d"); f(6, 13, 18, "d"); G[6][1] = "d"
for (x, y) in ((5, 3), (6, 4), (10, 2), (11, 5), (12, 5), (15, 4), (16, 3), (4, 5), (8, 5), (17, 2)): G[y][x] = "c"
for (x, y) in ((7, 2), (13, 3), (3, 4)): G[y][x] = "a"
G[3][2] = G[4][2] = "."                                                  # lanyard hole
G[2][8] = "1"; G[3][14] = "1"                                            # pins
# bolster + finger guard
for y in range(1, 7): f(y, 19, 22, "0" if y in (2, 3) else "1"); G[y][23] = "2"
f(5, 19, 22, "2"); f(6, 19, 23, "3"); G[1][19] = G[6][19] = "2"
f(7, 20, 24, "3"); f(8, 23, 25, "4"); G[7][20] = "4"; G[8][25] = "."
# blade: per column top/bottom; flat s, grind S, dark bottom edge t
TOP = {x: 1 for x in range(24, 45)}; TOP.update({45: 2, 46: 2, 47: 2, 48: 2, 49: 3, 50: 3, 51: 3, 52: 4, 53: 4})
BOT = {x: 8 for x in range(26, 45)}; BOT.update({24: 6, 25: 7, 45: 7, 46: 7, 47: 7, 48: 7, 49: 6, 50: 6, 51: 6, 52: 5, 53: 4})
for x in range(24, 54):
    t, b = TOP[x], BOT[x]; mid = (t + b + 1) // 2 if x < 45 else t + 2
    for y in range(t, b + 1): G[y][x] = "s" if y < mid else "S"
    G[b][x] = "t"
    if x >= 45 and b - t >= 2: G[t][x] = "s"
f(1, 24, 44, "s"); G[4][53] = "s"
A = ["".join(r) for r in G]

# ---------------- B: 16x4 in hand
B = [".aaaa.1ssssss...",
     "abcbbc1ssssssss.",
     "bbcbbd2SSSSSSSSs",
     ".dddd.3SSSSSSt..",
     ".....44tttt.....",]
GRIP = (3.0, 2.0); SUPPORT = None; MUZZLE = (16, 2.5)
