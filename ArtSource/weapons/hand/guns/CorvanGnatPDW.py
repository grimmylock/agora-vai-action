# Corvan Gnat PDW (MP7 pattern, stock collapsed): tube red-dot sight with flip caps on the top rail, slab
# polymer receiver, side rail, folded front grip, magazine through the pistol grip, long can suppressor.
from common import grid, fill, put
CAT = "smg"

# ---------------- A: 53x32 menu sprite
G = grid(53, 32)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# red-dot: tube, flip caps, mount
for y in range(3, 8): f(y, 8, 21, "4")
f(3, 9, 20, "3"); f(7, 9, 20, "5"); G[3][8] = G[3][21] = G[7][8] = G[7][21] = "."
for y in range(4, 7): G[y][8] = "L"; G[y][21] = "L"
G[4][21] = "k"; G[4][8] = "l"
f(4, 11, 18, "4"); put(G, 5, 16, "2")                                      # windage turret
put(G, 0, 19, "4"); put(G, 1, 19, "44"); put(G, 2, 20, "4")                # front flip cap (open)
put(G, 1, 9, "44"); put(G, 2, 9, "4")                                      # rear flip cap
for y in (8,): f(y, 11, 19, "5")
f(8, 12, 18, "4")
# receiver + top rail + iron sights
f(9, 4, 32, "3")
for x in range(5, 32, 2): G[9][x] = "4"
put(G, 8, 5, "44"); put(G, 8, 27, "44")
for y in range(10, 17): f(y, 2, 33, "q")
f(10, 2, 33, "p"); f(16, 4, 21, "r")
f(13, 4, 20, "r"); f(14, 4, 20, "q"); G[13][6] = "p"; G[14][6] = "p"    # receiver seam + pin
put(G, 12, 15, "pp"); put(G, 11, 16, "p")                                  # charging/selector
# side rail (forend) + folded front grip
f(11, 22, 30, "2"); f(12, 22, 30, "3")
for x in range(23, 31, 2): G[11][x] = "4"; G[12][x] = "4"
for y in range(14, 17): f(y, 22, 31, "q")
f(17, 22, 30, "r"); G[14][22] = "p"
# front end, barrel, suppressor
for y in range(10, 16): f(y, 31, 33, "q")
f(15, 31, 33, "r")
for y in range(11, 16): f(y, 34, 37, "4")
f(11, 34, 37, "3"); f(15, 34, 37, "5")
f(12, 38, 52, "3"); f(13, 38, 52, "4"); f(14, 38, 52, "4"); f(15, 38, 52, "5"); f(12, 38, 52, "2"); f(11, 39, 51, "3")
G[13][52] = "5"
# collapsed stock: butt plate hanging below the receiver
for y in range(10, 21): f(y, 0, 2, "q"); G[y][0] = "p"
f(20, 0, 2, "r"); G[10][0] = "."
# trigger guard + trigger
for y, row in {17: "q....q", 18: "q....q", 19: ".rrrr."}.items():
    for i, ch in enumerate(row):
        if ch != ".": G[y][16 + i] = ch
f(17, 4, 16, "r")
put(G, 17, 18, "3"); put(G, 18, 18, "2")
# grip with the magazine through it
for y in range(17, 26):
    a = 12 - (y - 17) // 4; b = a + 5
    f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "r"
    if 19 <= y <= 24:
        for x in range(a + 1, b):
            if (x + 2 * y) % 3 == 0: G[y][x] = "r"
for y in range(25, 32):
    a = 11 + (y - 25) // 4
    f(y, a, a + 4, "2"); G[y][a] = "1"; G[y][a + 4] = "3"                 # translucent polymer mag
    if y < 31 and y % 2: G[y][a + 2] = "1"
f(31, 11, 16, "4")
A = ["".join(r) for r in G]

# ---------------- B: 27x16 in hand
H = grid(27, 16)
g = lambda y, a, b, c: fill(H, y, a, b, c)
g(1, 4, 10, "3"); g(2, 4, 10, "4"); g(3, 5, 9, "5"); H[2][4] = "l"; H[2][10] = "k"; H[1][4] = "4"
put(H, 0, 4, "4"); put(H, 0, 10, "4")                                      # flip caps
g(4, 2, 16, "3")
for x in range(3, 16, 2): H[4][x] = "4"
H[4][6] = "4"; H[4][9] = "4"
for y in range(5, 9): g(y, 1, 16, "q")
g(5, 1, 16, "p"); g(7, 2, 10, "r")
g(5, 11, 15, "2"); H[5][12] = "4"; H[5][14] = "4"; g(6, 11, 15, "3")       # side rail
g(8, 11, 15, "r")                                                          # folded front grip
for y in range(5, 8): g(y, 16, 18, "4")
g(5, 16, 18, "3")
g(5, 19, 26, "2"); g(6, 19, 26, "4"); g(7, 19, 26, "5")                    # suppressor
for y in range(5, 11): g(y, 0, 1, "q"); H[y][0] = "p"
g(10, 0, 1, "r")
put(H, 9, 8, "q.q"); put(H, 10, 9, "r"); H[9][9] = "2"; g(9, 2, 7, ".")
for (y, a) in ((8, 6), (9, 6), (10, 6), (11, 5), (12, 5)):
    g(y, a, a + 2, "q"); H[y][a] = "p"; H[y][a + 2] = "r"
H[10][7] = "r"
for y in range(13, 16):
    g(y, 5, 7, "2"); H[y][5] = "1"; H[y][7] = "3"
g(15, 5, 7, "4")
B = ["".join(r) for r in H]
GRIP = (7.0, 10.5); SUPPORT = (13.0, 8.0); MUZZLE = (27, 6.5)
