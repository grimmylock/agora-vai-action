# TR-30 (AR-15 pattern carbine), from the line drawing + own knowledge: ported polymer stock on a buffer
# tube, black anodised upper/lower with Picatinny rail and flip sights, long M-LOK handguard with slots,
# ergo polymer grip, curved polymer mag, birdcage flash hider.
from common import grid, fill, put
CAT = "assault"

# ---------------- A: 60x18 menu sprite
G = grid(60, 18)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# stock: body with ports, angled toe, buttpad; castle nut
ST = {3: (1, 12), 4: (0, 12), 5: (0, 12), 6: (0, 12), 7: (0, 5), 8: (0, 4), 9: (0, 3), 10: (0, 2), 11: (0, 1)}
for y, (a, b) in ST.items(): f(y, a, b, "q")
f(3, 1, 12, "p"); f(6, 6, 12, "r"); f(7, 4, 5, "r"); G[8][4] = "r"; G[9][3] = "r"; G[10][2] = "r"; G[11][1] = "r"
for x in (3, 5, 7, 9): G[4][x] = "r"; G[5][x] = "r"
for y in range(3, 12): G[y][0] = "r"
f(4, 13, 14, "3"); f(5, 13, 14, "4"); G[4][13] = "2"; put(G, 6, 13, "4")
# upper: Picatinny rail, flip rear sight, charging handle, port, forward assist
f(2, 16, 27, "3")
for x in range(17, 28, 2): G[2][x] = "4"
put(G, 0, 18, "3"); put(G, 1, 17, "434")
for y in range(3, 6): f(y, 15, 27, "3")
f(3, 15, 27, "2"); f(5, 15, 27, "4"); G[3][15] = "3"
f(4, 20, 24, "5"); put(G, 4, 26, "2"); put(G, 3, 14, "3")
# lower: receiver, mag well, selector, pivot pins
for y in range(6, 9): f(y, 15, 27, "4")
f(6, 15, 27, "3"); G[7][17] = "2"; G[7][26] = "5"; G[6][16] = "5"
f(9, 23, 28, "4"); f(8, 27, 28, "4"); f(8, 15, 27, "5")
# ergo pistol grip
GR = {8: (15, 18), 9: (15, 18), 10: (14, 18), 11: (14, 17), 12: (14, 17), 13: (13, 17), 14: (13, 16)}
for y, (a, b) in GR.items(): f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "r"
f(14, 13, 16, "r"); G[10][17] = "r"; G[12][16] = "r"
# trigger guard (hole open) + trigger
for y in (9, 10): G[y][22] = "3"
f(11, 18, 22, "3"); put(G, 9, 20, "4"); G[10][18] = "3"
# curved polymer mag with base plate
for y in range(10, 18):
    a = 23 + (y - 10) // 3; b = a + 5
    f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "r"
    if y in (12, 14): f(y, a + 2, b - 1, "r")
f(17, 25, 31, "r")
# M-LOK handguard: top rail, slots, flip front sight
f(2, 28, 46, "3")
for x in range(29, 47, 2): G[2][x] = "4"
for y in range(3, 7): f(y, 28, 46, "4")
f(3, 28, 46, "3"); f(6, 29, 46, "5"); G[6][28] = "4"
for x in range(30, 45, 5): f(4, x, x + 2, "5")
for x in range(32, 45, 5): G[5][x] = "5"
put(G, 0, 44, "3"); put(G, 1, 43, "434")
# barrel + birdcage flash hider
f(4, 47, 54, "2"); f(5, 47, 54, "4")
for y in range(3, 7): f(y, 55, 59, "3")
f(3, 55, 59, "2"); f(6, 55, 59, "4")
for x in (56, 58): G[4][x] = "5"; G[5][x] = "5"
A = ["".join(r) for r in G]

# ---------------- B: 38x11 in hand
B = ["." * 11 + "3" + "." * 16 + "3" + "." * 9,
     "pppppppp" + "." + "343434343" + "343434343434" + "." * 8,
     "qrqrqrqq" + "3" + "222255522" + "444544454445" + "22222" + "333",
     "qqqqqqqq" + "4" + "444444443" + "444444444444" + "44444" + "353",
     "qqrrrrrr" + "." + "443444444" + "555555555555" + "." * 8,
     "qr" + "......." + "pqr" + ".43" + "pqqr" + "." * 19,
     "r" + "........" + "pqr" + "333" + "pqqr" + "." * 19,
     "." * 8 + "pqr" + "...." + "pqqr" + "." * 19,
     "." * 8 + "rrr" + "....." + "pqqr" + "." * 18,
     "." * 16 + "pqqr" + "." * 18,
     "." * 16 + "rrrrr" + "." * 17]
GRIP = (10.0, 6.5); SUPPORT = (24.0, 4.0); MUZZLE = (38, 2.5)
