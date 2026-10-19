# Sever-98 (Accuracy International AW pattern). The reference photo is tilted, so this is a clean level
# profile: OD-green thumbhole chassis stock with cheek riser and black butt pad, steel action, long black
# scope with big objective bell on rings, detachable box mag, folded bipod, long barrel with muzzle brake.
from common import grid, fill, put
CAT = "sniper"

def od(G, x, t, b):
    for y in range(t, b + 1): G[y][x] = "O"
    G[t][x] = "o"; G[b][x] = "e"

# ---------------- A: 60x12 menu sprite
G = grid(60, 12)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# butt: pad, cheek riser, body
for x in range(1, 12): od(G, x, 4, 11 if x < 9 else 10)
for y in range(4, 12): G[y][0] = "q"
G[4][0] = "p"; G[11][0] = "r"
f(3, 3, 9, "o"); f(4, 3, 9, "O"); G[4][2] = "e"; G[4][10] = "e"   # cheek riser
f(7, 2, 8, "e"); f(8, 2, 8, "o")                             # groove
# thumbhole: top bar, raked grip, bottom bar
for x in range(12, 18): od(G, x, 5, 6)
GP = {7: (16, 19), 8: (16, 19), 9: (15, 18), 10: (15, 18)}
for y, (a, b) in GP.items(): f(y, a, b, "O"); G[y][a] = "o"; G[y][b] = "e"
f(11, 15, 17, "e")
f(10, 11, 14, "O"); f(11, 11, 14, "e")
# action bedding + forend
for x in range(18, 31): od(G, x, 5, 8)
for x in range(31, 43): od(G, x, 5, 7)
G[7][42] = "."; G[6][42] = "e"
# receiver + bolt knob
f(3, 17, 32, "2"); f(3, 19, 26, "1"); f(4, 17, 32, "4"); G[4][17] = "5"
put(G, 5, 30, "3"); put(G, 6, 30, "1")
# scope: black bells, steel tube, turret, lens, rings
for x in range(13, 18): G[0][x] = "p"; G[1][x] = "q"; G[2][x] = "q"; G[3][x] = "r" if x > 14 else "."
f(1, 18, 29, "3"); f(2, 18, 29, "4"); f(1, 19, 23, "2"); f(0, 22, 24, "q"); G[0][23] = "p"
for x in range(30, 36): G[0][x] = "p"; G[1][x] = "q"; G[2][x] = "q"; G[3][x] = "r"
put(G, 0, 36, "k"); put(G, 1, 36, "l"); put(G, 2, 36, "l"); put(G, 3, 36, "L")
G[3][19] = G[3][27] = "5"
# trigger + guard, magazine
put(G, 9, 21, "2"); G[9][24] = "4"; f(10, 20, 24, "4"); G[10][20] = "3"
f(9, 25, 29, "4"); f(10, 25, 29, "4"); f(11, 25, 29, "5"); G[9][25] = "3"; G[10][25] = "3"
# barrel, muzzle brake, folded bipod
f(3, 33, 54, "2"); f(4, 33, 54, "4"); f(3, 33, 38, "1")
put(G, 2, 55, "33.33"); put(G, 3, 55, "21521"); put(G, 4, 55, "45454"); put(G, 5, 55, "44.44")
f(8, 34, 45, "4"); put(G, 8, 46, "3"); f(8, 32, 33, "5")
A = ["".join(r) for r in G]

# ---------------- B: 42x9 in hand
H = grid(42, 9)
h = lambda y, a, b, c: fill(H, y, a, b, c)
for x in range(1, 8): od(H, x, 3, 8 if x < 6 else 7)
for y in range(3, 9): H[y][0] = "q"
H[3][0] = "p"; h(2, 2, 5, "o")
for x in range(8, 10): od(H, x, 4, 4)
for y, (a, b) in {5: (10, 12), 6: (10, 12), 7: (9, 11), 8: (9, 11)}.items(): h(y, a, b, "O"); H[y][a] = "o"; H[y][b] = "e"
h(7, 7, 8, "O"); h(8, 7, 8, "e")
for x in range(12, 22): od(H, x, 5, 6)
for x in range(22, 30): od(H, x, 4, 6)
h(3, 11, 22, "2"); h(3, 13, 17, "1"); h(4, 10, 21, "4")
for x in (8, 9, 10, 19, 20, 21, 22, 23): H[0][x] = "p"; H[1][x] = "q"; H[2][x] = "r"
h(1, 11, 18, "3"); h(2, 11, 18, "4"); H[0][14] = "q"
put(H, 0, 24, "k"); put(H, 1, 24, "l"); put(H, 2, 24, "L")
put(H, 7, 14, "2"); h(8, 13, 15, "4")
h(7, 16, 18, "4"); h(8, 16, 18, "5")
h(3, 23, 37, "3"); put(H, 3, 38, "2121"); put(H, 4, 38, "4545")
h(7, 23, 29, "4")
B = ["".join(r) for r in H]
GRIP = (10.5, 6.5); SUPPORT = (25.0, 7.0); MUZZLE = (42, 3.5)
