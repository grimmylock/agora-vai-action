# Frontier lever rifle (Winchester 1892 carbine pattern). The reference photo is tilted, so this is a
# clean side profile: straight-grip walnut stock with crescent butt plate, blued receiver with hammer and
# loading gate, finger-loop lever, short walnut forend, barrel over the tube magazine, barrel band, sights.
from common import grid, fill, put
CAT = "sniper"

def wood(G, x, t, b):
    for y in range(t, b + 1): G[y][x] = "c"
    G[t][x] = "b"; G[b][x] = "d"

# ---------------- A: 58x12 menu sprite
G = grid(58, 12)
f = lambda y, a, b, c: fill(G, y, a, b, c)
BOT = {1: 11, 2: 11, 3: 10, 4: 10, 5: 10, 6: 9, 7: 9, 8: 9, 9: 8, 10: 8, 11: 8, 12: 7, 13: 7, 14: 6, 15: 6, 16: 6, 17: 6}
for x, b in BOT.items(): wood(G, x, 4 if x < 10 else 3, b)
for y in range(4, 12): G[y][0] = "4"
G[4][0] = "2"; G[11][0] = "3"; G[3][1] = "."               # crescent butt plate
f(6, 3, 10, "d"); f(7, 3, 9, "b")                         # grain line
# receiver
f(1, 19, 28, "2"); f(1, 21, 26, "1"); G[2][18] = "3"
for y in range(2, 7): f(y, 18, 28, "4")
f(2, 19, 28, "3"); f(6, 18, 28, "5")
f(4, 23, 27, "5"); f(3, 23, 27, "3")                      # loading gate
put(G, 1, 17, "4"); put(G, 0, 17, "3"); put(G, 2, 17, "4")   # hammer
# lever loop + trigger
f(7, 12, 16, "4"); G[7][19] = "2"; f(7, 20, 21, "3")
G[8][12] = "3"; G[9][12] = "3"; f(8, 21, 21, "3"); G[9][21] = "3"
f(10, 12, 21, "3"); f(10, 13, 20, "4"); G[10][12] = G[10][21] = "."
f(9, 13, 13, "3"); G[9][13] = "."
# barrel, forend, tube magazine, band, sights
f(1, 29, 57, "2"); f(2, 29, 57, "4"); f(1, 29, 33, "1")
for x in range(29, 41): wood(G, x, 3, 6)
G[3][29] = "c"
f(4, 41, 53, "3"); f(5, 41, 53, "4"); put(G, 4, 54, "2"); put(G, 5, 54, "3")   # tube, gap above
for y in range(1, 7): G[y][41] = "3"
G[1][41] = "2"; G[6][41] = "4"
for y in range(1, 6): G[y][49] = "3"
G[5][49] = "4"
G[0][33] = "4"; put(G, 0, 56, "3")
A = ["".join(r) for r in G]

# ---------------- B: 38x8 in hand
H = grid(38, 8)
h = lambda y, a, b, c: fill(H, y, a, b, c)
BB = {1: 7, 2: 7, 3: 6, 4: 6, 5: 6, 6: 5, 7: 5, 8: 5, 9: 4, 10: 4, 11: 4}
for x, b in BB.items(): wood(H, x, 3 if x < 7 else 2, b)
for y in range(3, 8): H[y][0] = "4"
H[3][0] = "2"
h(1, 13, 18, "2"); h(1, 14, 16, "1"); h(2, 12, 18, "3"); h(3, 12, 18, "4"); h(4, 12, 18, "5")
h(3, 15, 17, "5"); put(H, 1, 11, "4"); put(H, 0, 11, "3")
put(H, 5, 9, "44"); put(H, 5, 13, "23"); put(H, 6, 9, "3"); put(H, 6, 14, "3"); h(7, 9, 14, "3")   # lever loop
h(1, 19, 37, "2"); h(2, 19, 37, "4")
for x in range(19, 27): wood(H, x, 3, 4)
h(4, 27, 34, "3"); put(H, 4, 35, "2")
for y in range(1, 5): H[y][27] = "3"
H[3][32] = "3"
H[0][21] = "4"; H[0][37] = "3"
B = ["".join(r) for r in H]
GRIP = (10.5, 4.0); SUPPORT = (23.0, 5.5); MUZZLE = (38, 2.0)
