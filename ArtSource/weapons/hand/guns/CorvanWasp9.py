# Corvan Wasp 9 (MP5A3 pattern, stock collapsed): black steel receiver with the cocking tube on top, rear
# sight drum, hooded front sight, slim polymer handguard, 3-lug barrel, raked polymer grip + trigger
# housing, curved steel magazine, retracted butt pad tucked against the receiver.
from common import grid, fill, put
CAT = "smg"

def mag(G, y0, y1, x0, x1, w, ch="4", lit="3", dark="5"):
    for y in range(y0, y1 + 1):
        a = x0 + (y - y0) * (x1 - x0) // max(1, y1 - y0)
        fill(G, y, a, a + w, ch); G[y][a] = lit; G[y][a + w] = dark

# ---------------- A: 50x32 menu sprite
G = grid(50, 32)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# retracted butt pad + stock arms
for y in range(3, 16): f(y, 0, 2, "q"); G[y][0] = "p"
G[3][0] = G[15][0] = "."; G[15][2] = "r"; f(15, 1, 2, "r")
f(4, 3, 4, "4"); f(10, 3, 4, "4")
# receiver + cocking tube
f(3, 4, 43, "2")
for y in range(4, 8): f(y, 3, 43, "3" if y < 5 else "4")
f(7, 3, 43, "5")
for y in range(8, 10): f(y, 3, 22, "4")
f(9, 3, 22, "5")
put(G, 2, 31, "33"); put(G, 3, 31, "12")                                   # cocking handle
for y in range(0, 3): f(y, 7, 11, "3")                                     # rear sight drum
f(0, 8, 10, "2"); G[1][9] = "5"
put(G, 5, 14, "5"); put(G, 6, 18, "1")                                     # pins
# handguard (slim polymer)
f(8, 23, 41, "2")
for y in range(9, 12): f(y, 22, 42, "p")
f(11, 23, 41, "q"); G[8][22] = "p"; G[11][42] = "."; G[8][42] = "."
for x in range(26, 40, 4): G[10][x] = "q"
# front sight hood + barrel with 3-lug + muzzle
f(0, 42, 44, "3"); put(G, 1, 41, "3...4"); put(G, 2, 41, "3...4"); f(3, 41, 45, "4")
for y in range(4, 10): f(y, 43, 44, "4"); G[y][45] = "5"
f(7, 46, 49, "3"); f(8, 46, 49, "3"); f(9, 46, 49, "4"); f(7, 46, 49, "2")
for y in (6, 10): f(y, 46, 47, "4")
G[7][49] = G[8][49] = "3"
# lower: trigger housing (polymer), guard, grip, selector
for y in range(10, 14): f(y, 6, 21, "q")
f(10, 6, 21, "p")
put(G, 11, 9, "22"); put(G, 12, 10, "2")                                  # selector
for y, row in {14: "q......q", 15: "q......q", 16: "q......q", 17: ".rrrrrrr"}.items():
    for i, ch in enumerate(row):
        if ch != ".": G[y][12 + i] = ch
put(G, 14, 15, "4"); put(G, 15, 15, "3"); put(G, 16, 16, "3")              # trigger
BK = {14: 7, 15: 7, 16: 6, 17: 6, 18: 6, 19: 5, 20: 5, 21: 5, 22: 6, 23: 7}
FR = {14: 12, 15: 12, 16: 12, 17: 11, 18: 11, 19: 11, 20: 10, 21: 10, 22: 10, 23: 9}
for y in range(14, 24):
    a, b = BK[y], FR[y]
    f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "r"
f(23, 7, 9, "r")
# magazine well + curved magazine
f(10, 19, 24, "4"); f(11, 19, 24, "4"); f(12, 20, 24, "5")
mag(G, 12, 31, 20, 24, 4)
for y in range(14, 30, 1): G[y][20 + (y - 12) * 4 // 19 + 2] = "3" if y % 3 else "4"
A = ["".join(r) for r in G]

# ---------------- B: 30x19 in hand
H = grid(30, 19)
g = lambda y, a, b, c: fill(H, y, a, b, c)
for y in range(2, 10): g(y, 0, 1, "q"); H[y][0] = "p"
H[2][0] = H[9][0] = "."; H[9][1] = "r"
g(3, 2, 2, "4"); g(7, 2, 2, "4")
g(2, 3, 25, "2"); g(3, 2, 25, "3"); g(4, 2, 25, "4"); g(5, 2, 12, "4"); g(6, 2, 12, "5")
put(H, 0, 5, "33"); put(H, 1, 4, "3443"); put(H, 1, 19, "32")              # rear drum, cocking handle
g(5, 13, 24, "2"); g(6, 13, 24, "p"); g(7, 13, 24, "q"); H[7][24] = "."
put(H, 0, 24, "333"); put(H, 1, 24, "3.3")                                 # front sight hood
for y in range(2, 6): g(y, 25, 26, "4")
g(4, 27, 29, "3"); g(5, 27, 29, "2"); g(6, 27, 29, "4"); H[3][27] = "4"; H[7][27] = "4"
g(6, 25, 26, "5")
for y in range(5, 9): g(y, 3, 12, "q")
g(5, 3, 12, "p"); H[6][5] = "2"
for y, row in {9: "q...q", 10: "q.3.q", 11: ".rrrr"}.items():
    for i, ch in enumerate(row):
        if ch != ".": H[y][7 + i] = ch
H[9][9] = "4"
BKb = {9: 4, 10: 4, 11: 3, 12: 3, 13: 3, 14: 3, 15: 4}
FRb = {9: 7, 10: 7, 11: 7, 12: 6, 13: 6, 14: 6, 15: 6}
for y in range(9, 16):
    a, b = BKb[y], FRb[y]
    g(y, a, b, "q"); H[y][a] = "p"; H[y][b] = "r"
g(15, 4, 6, "r")
g(5, 12, 15, "4"); g(6, 12, 15, "4")
mag(H, 7, 18, 13, 16, 3)
B = ["".join(r) for r in H]
GRIP = (5.0, 12.0); SUPPORT = (19.5, 7.0); MUZZLE = (30, 5.5)
