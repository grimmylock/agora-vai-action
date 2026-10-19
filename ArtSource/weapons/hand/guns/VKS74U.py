# VKS-74U (AKS-74U pattern): triangular folding stock (extended), short receiver with the hinged top
# cover and rear sight up front, plum wood handguards, tall front block, flared muzzle booster,
# plum grip and curved orange bakelite 5.45 mag.
from common import grid, fill, put
CAT = "assault"

# ---------------- A: 56x21 menu sprite
G = grid(56, 21)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# triangle stock: buttplate, top strut, diagonal lower strut, hinge block
for y in range(5, 15): f(y, 0, 1, "4")
for y in range(5, 15): G[y][0] = "3"
G[14][0] = "5"; G[14][1] = "5"
f(5, 2, 17, "2"); f(6, 2, 17, "4")
for x in range(2, 18):
    y = 13 - round((x - 2) * 6 / 15)
    G[y][x] = "3"; G[y + 1][x] = "4"
for y in range(3, 9): f(y, 16, 18, "3")
f(3, 16, 18, "2"); f(8, 16, 18, "4"); G[5][17] = "5"
# receiver: top cover, rear sight at its front hinge, port, selector, bolt handle, rivets
f(1, 19, 37, "2"); f(1, 21, 30, "1")
for y in range(2, 8): f(y, 18, 38, "3")
f(5, 18, 38, "4"); f(6, 18, 38, "4"); f(7, 19, 38, "5")
put(G, 0, 35, "233"); put(G, 1, 35, "4"); put(G, 2, 36, "44")        # rear sight block
f(3, 26, 31, "5"); put(G, 4, 20, "22222221"); put(G, 3, 32, "11")
G[5][21] = G[5][23] = "2"; G[6][31] = "3"
# plum pistol grip, raked back
GR = {8: (19, 22), 9: (19, 22), 10: (18, 22), 11: (18, 21), 12: (18, 21), 13: (17, 21), 14: (17, 20)}
for y, (a, b) in GR.items(): f(y, a, b, "c"); G[y][a] = "b"; G[y][b] = "d"
f(14, 17, 20, "d"); G[10][20] = "d"; G[12][19] = "d"
# trigger guard (hole open) + trigger
for y in (8, 9): G[y][28] = "2"
f(10, 23, 28, "2"); G[10][28] = "3"; put(G, 8, 24, "3"); put(G, 9, 24, "3")
# curved bakelite mag: lit spine, darker front, rib, floorplate
MG = {8: (29, 34), 9: (29, 34), 10: (29, 35), 11: (30, 35), 12: (30, 35), 13: (30, 36), 14: (31, 36), 15: (31, 36),
      16: (32, 37), 17: (32, 37), 18: (33, 37), 19: (33, 37)}
for y, (a, b) in MG.items():
    f(y, a, b, "b"); G[y][a] = "a"; G[y][b] = "c"
f(8, 30, 33, "c"); f(19, 33, 37, "d"); f(20, 34, 36, "d")
for y in (10, 11, 12): G[y][MG[y][0] + 1] = "a"
# handguards: upper gas-tube cover + lower guard
f(1, 39, 47, "a"); f(2, 39, 47, "c"); f(3, 39, 47, "a"); f(4, 39, 47, "b"); f(5, 39, 47, "c"); f(6, 40, 46, "d")
G[2][39] = "4"; G[2][47] = "d"
# front sight block, barrel, flared muzzle booster
for y in range(1, 7): f(y, 48, 49, "3")
put(G, 0, 48, "3.3"); f(1, 48, 49, "2"); f(6, 48, 49, "4"); G[0][49] = "."
put(G, 0, 48, "33")
f(3, 50, 51, "2"); f(4, 50, 51, "4")
for y in range(2, 6): f(y, 52, 55, "3")
f(2, 52, 55, "2"); f(5, 52, 55, "4"); put(G, 1, 54, "22"); put(G, 6, 54, "44"); G[3][55] = "4"; G[4][55] = "5"
A = ["".join(r) for r in G]

# ---------------- B: 32x12 in hand
B = ["." * 10 + "2" * 10 + "." + "32" + "...." + "3" + "....",
     "." * 9 + "3" + "1111222223" + "33" + "abbbb" + "3" + ".." + "23",
     "4" + "22222222" + "4" + "3335555133" + "33" + "ccccc" + "4" + "4433",
     "4" + "......" + "33" + "4" + "4222214444" + "44" + "ddddd" + "4" + ".." + "45",
     "4" + "...." + "33" + "..." + "bcd" + ".23" + "abbbc" + "." * 11,
     "4" + ".." + "33" + "....." + "bcd" + "333" + "abbbc" + "." * 11,
     "433" + "......" + "bcd" + "....." + "abbbc" + "." * 10,
     "." * 9 + "bcd" + "....." + "abbbc" + "." * 10,
     "." * 9 + "ddd" + "." * 6 + "abbbc" + "." * 9,
     "." * 18 + "abbbc" + "." * 9,
     "." * 19 + "abbc" + "." * 9,
     "." * 19 + "ddd" + "." * 10]
GRIP = (10.5, 6.0); SUPPORT = (24.0, 3.0); MUZZLE = (32, 2.5)
