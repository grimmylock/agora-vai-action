# G7 rail prototype: sci-fi railgun rifle. The reference photo is a home-built coilgun box, so this is an
# original profile in the same spirit: dark-metal skeleton stock, boxy capacitor receiver with vent slots
# and one cyan charge indicator, compact box optic, polymer grip, slug cell ahead of the trigger, and two
# parallel rails (open projectile channel between them) held by copper coil clamps, open muzzle bracket.
from common import grid, fill, put
CAT = "sniper"

def coil(G, x0, t, b, w=3):
    """copper winding: lit top, alternating turns, dark foot"""
    for x in range(x0, x0 + w):
        for y in range(t, b + 1): G[y][x] = "b" if y % 2 else "c"
        G[t][x] = "a"; G[b][x] = "4"

# ---------------- A: 58x16 menu sprite
G = grid(58, 16)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# skeleton stock
for y in range(4, 13): G[y][0] = "q"; G[y][1] = "r"
G[4][0] = "p"; G[4][1] = "q"
for x in range(2, 14):
    b = 12 if x < 6 else 11 if x < 10 else 10
    G[4][x] = "3"; G[5][x] = "4"; G[b - 1][x] = "3"; G[b][x] = "5"
for y in range(4, 11): G[y][13] = "4"
G[4][13] = "3"
# receiver: capacitor box with vents + cyan charge indicator
f(3, 14, 30, "2"); f(4, 14, 30, "3")
for y in range(5, 10): f(y, 14, 30, "4")
f(10, 14, 30, "5"); f(6, 15, 29, "5")
for x in range(20, 29, 2): G[8][x] = "5"; G[7][x] = "5"
f(7, 21, 27, "3"); f(8, 21, 27, "3")
for x in range(20, 29, 2): G[7][x] = "5"; G[8][x] = "5"
put(G, 5, 16, "llk")
# box optic on a mount
f(0, 19, 27, "p"); f(1, 18, 27, "q"); put(G, 0, 28, "L"); put(G, 1, 28, "L"); put(G, 2, 20, "55"); put(G, 2, 25, "55")
# grip, trigger + guard, slug cell
GR = {11: (16, 19), 12: (16, 19), 13: (15, 18), 14: (15, 18)}
for y, (a, b) in GR.items(): f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "r"
f(15, 15, 17, "r"); G[12][18] = G[14][17] = "r"
put(G, 11, 21, "2"); G[11][24] = "4"; f(12, 20, 24, "4")
f(11, 25, 29, "p"); f(12, 25, 29, "q"); f(13, 25, 29, "r")
# twin rails with coil clamps, open muzzle bracket
f(4, 31, 54, "2"); f(5, 31, 54, "4"); f(8, 31, 54, "3"); f(9, 31, 54, "5")
for y in range(3, 11): G[y][30] = "4"
for x0 in (33, 40, 47): coil(G, x0, 3, 10)
for y in (3, 4, 5, 8, 9, 10): put(G, y, 55, "333")
put(G, 3, 55, "222"); put(G, 10, 55, "555")
A = ["".join(r) for r in G]

# ---------------- B: 40x12 in hand
H = grid(40, 12)
h = lambda y, a, b, c: fill(H, y, a, b, c)
for y in range(3, 10): H[y][0] = "q"
H[3][0] = "p"
for x in range(1, 10):
    b = 9 if x < 5 else 8
    H[3][x] = "3"; H[b][x] = "5"
for y in range(3, 9): H[y][9] = "4"
h(2, 10, 21, "2"); h(3, 10, 21, "3")
for y in range(4, 7): h(y, 10, 21, "4")
h(7, 10, 21, "5"); put(H, 4, 11, "lk")
for x in (15, 17, 19): H[5][x] = "5"
h(0, 13, 19, "p"); h(1, 12, 19, "r"); put(H, 0, 20, "L"); put(H, 1, 20, "L")
for y, (a, b) in {8: (11, 13), 9: (11, 13), 10: (10, 12), 11: (10, 12)}.items(): h(y, a, b, "q"); H[y][a] = "p"; H[y][b] = "r"
h(11, 10, 12, "r")
put(H, 8, 15, "2"); h(9, 14, 16, "4"); H[8][16] = "4"
h(8, 17, 20, "p"); h(9, 17, 20, "r")
h(3, 22, 36, "2"); h(4, 22, 36, "4"); h(6, 22, 36, "3"); h(7, 22, 36, "5")
for y in range(2, 9): H[y][22] = "4"
for x0 in (24, 29, 34):
    coil(H, x0, 2, 8, 2)
for y in (2, 3, 4, 6, 7, 8): h(y, 37, 39, "3")
h(2, 37, 39, "2"); h(8, 37, 39, "5")
B = ["".join(r) for r in H]
GRIP = (11.5, 9.5); SUPPORT = (27.0, 8.0); MUZZLE = (40, 5.5)
