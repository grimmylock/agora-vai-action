# Kaban-12 (Saiga-12 tactical pattern), drawn as a clean side profile (the photo is angled):
# M4-style collapsible stock on a buffer tube, AK receiver with railed dust cover, polymer pistol grip,
# long ribbed 10-round mag, quad-rail handguard, barrel with gas block, big ported muzzle brake.
from common import grid, fill, put
CAT = "shotgun"

# ---------------- A: 60x19 menu sprite
G = grid(60, 19)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# collapsible stock + buffer tube
for y in range(1, 6): f(y, 0, 12, "q")
f(1, 1, 11, "p"); G[1][12] = "."; f(5, 2, 10, "r"); G[5][11] = "."; G[5][12] = "."
for y in range(1, 9): G[y][0] = "r"
G[1][0] = "q"; put(G, 6, 1, "qr"); put(G, 7, 1, "r"); G[8][0] = "."; G[7][0] = "r"
f(3, 3, 8, "r"); f(4, 4, 8, "p")                   # lightening slot
put(G, 6, 6, "rrr")                                # adjust lever
f(2, 12, 17, "3"); f(3, 12, 17, "4"); G[2][13] = "2"; G[2][14] = "2"
# receiver: rail on the dust cover, port, charging handle, selector lever
f(0, 20, 33, "3")
for x in range(21, 33, 2): G[0][x] = "4"
f(1, 18, 33, "2"); f(2, 17, 33, "3"); f(3, 17, 33, "4"); f(4, 17, 33, "4"); f(5, 18, 33, "5")
f(2, 24, 29, "5"); put(G, 3, 30, "11"); put(G, 3, 19, "222222"); G[3][25] = "1"
G[4][18] = "3"; G[4][32] = "3"
# pistol grip, raked back
GR = {6: (17, 20), 7: (16, 20), 8: (16, 19), 9: (15, 19), 10: (15, 18), 11: (14, 18)}
for y, (a, b) in GR.items(): f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "r"
f(11, 14, 18, "r"); G[8][18] = "r"; G[10][17] = "r"
# trigger guard (hole open) + trigger
for y in (6, 7): G[y][25] = "3"
f(8, 21, 25, "3"); put(G, 6, 22, "4")
# long ribbed mag, curving forward
MG = {6: (26, 31), 7: (26, 31), 8: (27, 32), 9: (27, 32), 10: (27, 32), 11: (28, 33), 12: (28, 33), 13: (28, 33),
      14: (29, 34), 15: (29, 34), 16: (30, 34), 17: (30, 35)}
for y, (a, b) in MG.items():
    f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "r"
    if y % 2 == 1 and y > 7: f(y, a + 2, b - 1, "r")
f(18, 30, 35, "r"); f(6, 27, 30, "r")
# quad-rail handguard
f(0, 34, 46, "3")
for x in range(35, 46, 2): G[0][x] = "4"
for y in range(1, 5): f(y, 34, 46, "4")
f(1, 34, 46, "3"); f(5, 34, 46, "3")
for x in range(35, 46, 2): G[5][x] = "5"
for x in range(36, 46, 3): G[2][x] = "5"; G[3][x] = "5"
# gas block, gas tube stub, barrel, ported muzzle brake
for y in range(1, 5): f(y, 47, 48, "3")
f(1, 47, 48, "2")
f(3, 49, 53, "2"); f(4, 49, 53, "4")
for y in range(2, 6): f(y, 54, 59, "3")
f(2, 54, 59, "2"); f(5, 54, 59, "4")
for x in (55, 57): G[3][x] = "5"; G[4][x] = "5"
G[3][59] = "4"; G[4][59] = "5"
A = ["".join(r) for r in G]

# ---------------- B: 38x13 in hand
B = ["pppppppp" + "..." + "34343434343" + "34343434" + "." * 8,
     "qqqqqqqqq" + "33" + "22222222222" + "44444444" + "3" + "...." + "333",
     "qrrrrrqqq" + "44" + "33355551333" + "4545454" + "4" + "4" + "2222" + "353",
     "qqqqqqqqr" + ".." + "42222144444" + "55555555" + "." + "4444" + "343",
     "qqqqqqqr" + "." + "pqqr" + ".23" + "pqqqqr" + "." * 16,
     "qr" + "......" + "pqqr" + "3333" + "pqqqqr" + "." * 16,
     "........" + "pqqr" + "....." + "pqqqqr" + "." * 15,
     "........" + "rrrr" + "....." + "pqrrrr" + "." * 15,
     "." * 17 + "pqqqqr" + "." * 15,
     "." * 18 + "pqrrrr" + "." * 14,
     "." * 18 + "pqqqqr" + "." * 14,
     "." * 19 + "pqrrrr" + "." * 13,
     "." * 19 + "rrrrrr" + "." * 13]
GRIP = (10.0, 5.5); SUPPORT = (26.0, 3.0); MUZZLE = (38, 2.5)
