# VK-103 (AK-103 pattern), built on the VK-74 pilot: all-black polymer stock, ribbed handguards, grip and
# deep-curved ribbed 7.62 mag; AK-74-style two-port muzzle brake. Drawn in wood letters, then mapped to
# polymer (b->p, c->q, d->r).
from common import grid, fill, put
CAT = "assault"

# ---------------- A: 56x17 menu sprite
G = grid(56, 17)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# stock: buttplate, lit comb, lightening groove, sloping belly up to the wrist
BOT = {0: 9, 1: 9, 2: 9, 3: 9, 4: 9, 5: 8, 6: 8, 7: 8, 8: 8, 9: 7, 10: 7, 11: 7, 12: 7, 13: 6, 14: 6, 15: 5}
for x, b in BOT.items():
    top = 2 if x >= 13 else 3
    for y in range(top, b + 1): G[y][x] = "c"
    G[top][x] = "b"; G[b][x] = "d"
for x in range(2, 11): G[5][x] = "d"; G[6][x] = "b" if x < 10 else "c"
f(4, 1, 12, "c"); G[3][13] = "c"
for y in range(3, 10): G[y][0] = "4"
G[3][0] = "3"; G[9][0] = "5"
# receiver: lit top cover, rear sight, port, selector lever, bolt handle, rivets
f(2, 15, 16, "4"); G[1][16] = "3"
f(1, 17, 27, "2"); f(1, 18, 23, "1")
f(2, 17, 27, "3"); f(2, 28, 33, "3"); f(1, 28, 31, "2"); put(G, 0, 29, "23")
for y in (3, 4): f(y, 15, 33, "4")
f(3, 22, 27, "5")
put(G, 4, 19, "2222221")
put(G, 3, 28, "11")
f(5, 15, 33, "5"); G[5][15] = "4"
G[3][17] = G[3][19] = "2"; G[5][24] = "3"
# plum pistol grip
GRIP_ = {6: (16, 20), 7: (16, 19), 8: (15, 19), 9: (15, 19), 10: (15, 18), 11: (15, 18)}
for y, (a, b) in GRIP_.items():
    f(y, a, b, "c"); G[y][a] = "b"; G[y][b] = "d"
    if y in (8, 10): G[y][a + 2] = "d"
f(11, 15, 18, "d"); G[11][15] = "c"
# trigger + guard
put(G, 6, 21, "3"); put(G, 6, 24, "2"); f(7, 20, 24, "2"); G[7][24] = "3"
# 7.62 mag: deep curve, lit spine, dark front, rib, floorplate
MAG = {6: (25, 31), 7: (25, 31), 8: (26, 31), 9: (26, 32), 10: (27, 32), 11: (27, 33), 12: (28, 33), 13: (29, 34),
       14: (30, 35), 15: (31, 35)}
for y, (a, b) in MAG.items():
    f(y, a, b, "c"); G[y][a] = "b"; G[y][b] = "d"
f(6, 26, 30, "d")
for y in (8, 9, 10): G[y][MAG[y][0] + 1] = "b"
f(16, 32, 35, "d")
# handguards: upper gas-tube cover + lower guard with finger swell
f(1, 34, 40, "b"); f(2, 34, 40, "d")
f(3, 33, 41, "b"); f(4, 33, 41, "c"); f(5, 33, 41, "d"); f(6, 38, 40, "d"); f(5, 38, 40, "c")
for y in range(1, 6): G[y][33] = "4" if y > 2 else G[y][33]
G[2][33] = "3"
# gas block, barrel over cleaning rod, hooded front sight, slant compensator
for y in range(1, 5): f(y, 41, 42, "3")
f(1, 41, 42, "2"); f(4, 41, 42, "4")
f(2, 43, 48, "2"); f(3, 43, 48, "4"); f(4, 43, 50, "3")
for y in range(1, 5): f(y, 49, 51, "3")
f(1, 49, 51, "2"); put(G, 0, 49, "3.3"); G[1][50] = "1"
put(G, 2, 52, "2221"); put(G, 3, 52, "3533"); put(G, 4, 52, "4444")
for x in (35, 37, 39): G[4][x] = "d"                    # ribbed lower handguard
for y in (9, 11, 13): f(y, MAG[y][0] + 2, MAG[y][1] - 1, "d")   # mag ribs
G[2][29] = "d"; G[2][30] = "d"
T = str.maketrans("bcd", "pqr")
A = ["".join(r).translate(T) for r in G]

# ---------------- B: 40x12 in hand
B = ["." * 21 + "23" + "." * 12 + "3" + "....",
     "........." + "bb" + "3" + "111111" + "222" + "333" + "bbbbb" + "22" + "...." + "23" + "...",
     "4" + "bbbbbbbb" + "cc" + "3333" + "5555" + "3" + "1" + "333" + "ddddd" + "33" + "2222" + "33" + "221",
     "4" + "ccccccccc" + "d" + "44" + "22221" + "444444" + "bcbcb" + "44" + "4444" + "44" + "434",
     "4" + "c" + "dddddd" + "cc" + "d" + "5" * 13 + "ccccc" + "." * 11,
     "4" + "ccccccc" + "dd" + "." + "bcdd" + "3" + "." + "2" + "bcccd" + "." * 17,
     "4" + "ddddd" + "....." + "bcdd" + "222" + "bcccd" + "." * 17,
     "." * 10 + "bcdd" + "....." + "bcccd" + "." * 16,
     "." * 10 + "dddd" + "......" + "bcccd" + "." * 15,
     "." * 21 + "bcccd" + "." * 14,
     "." * 22 + "bdcd" + "." * 14,
     "." * 23 + "ddd" + "." * 14]
B = [r.translate(T) for r in B]
GRIP = (12.5, 6.5); SUPPORT = (26.5, 4.0); MUZZLE = (40, 2.5)
