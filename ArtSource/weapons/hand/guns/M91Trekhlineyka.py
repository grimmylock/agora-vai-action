# M91 Trekhlineyka (Mosin-Nagant 91/30 sniper pattern). The reference photo is tilted, so this is a clean
# level profile: long walnut stock with straight wrist, upper handguard and two steel bands, receiver with
# the bent-down bolt handle, PU short scope on top, trigger guard + magazine box, hooded front sight.
from common import grid, fill, put
CAT = "sniper"

def wood(G, x, t, b):
    for y in range(t, b + 1): G[y][x] = "c"
    G[t][x] = "b"; G[b][x] = "d"

# ---------------- A: 62x12 menu sprite
G = grid(62, 12)
f = lambda y, a, b, c: fill(G, y, a, b, c)
BOT = {1: 11, 2: 11, 3: 10, 4: 10, 5: 10, 6: 10, 7: 9, 8: 9, 9: 9, 10: 9, 11: 8, 12: 8, 13: 8, 14: 8}
for x, b in BOT.items(): wood(G, x, 6 if x < 7 else 5, b)
for x in range(15, 32): wood(G, x, 5, 7)                  # stock under the receiver
for x in range(32, 57): wood(G, x, 5, 6)                  # forend
for y in range(6, 12): G[y][0] = "4"
G[6][0] = "3"
f(7, 2, 9, "d"); f(8, 2, 8, "b")                          # grain line
# receiver, bolt, scope
f(3, 17, 32, "2"); f(3, 19, 24, "1"); f(4, 17, 32, "4"); f(4, 25, 31, "3")
G[5][21] = "3"; put(G, 6, 20, "21")                       # bent bolt handle + knob
put(G, 0, 18, "3"); put(G, 1, 18, "5"); f(0, 19, 30, "2"); f(0, 21, 24, "1"); f(1, 19, 30, "4")
put(G, 0, 31, "k"); put(G, 1, 31, "l"); put(G, 2, 21, "55"); put(G, 2, 27, "55")   # raised on its mount
# barrel, rear sight, upper handguard, bands, front sight
f(2, 33, 35, "3"); f(3, 33, 36, "3"); f(4, 33, 36, "4")
f(3, 37, 55, "b"); f(4, 37, 55, "c")
f(3, 56, 61, "2"); f(4, 56, 61, "4"); put(G, 2, 60, "3"); put(G, 1, 60, "3")
for x in (45, 55):
    for y in range(3, 7): G[y][x] = "3"
    G[3][x] = "2"
# trigger guard + magazine box
put(G, 8, 20, "2"); G[8][23] = "3"; f(9, 19, 23, "3"); G[9][19] = "."
f(8, 24, 28, "3"); f(9, 24, 28, "4"); G[8][24] = "2"
A = ["".join(r) for r in G]

# ---------------- B: 44x8 in hand
H = grid(44, 8)
h = lambda y, a, b, c: fill(H, y, a, b, c)
BB = {1: 7, 2: 7, 3: 6, 4: 6, 5: 6, 6: 6, 7: 5, 8: 5, 9: 5, 10: 5}
for x, b in BB.items(): wood(H, x, 4, b)
for y in range(4, 8): H[y][0] = "4"
for x in range(11, 23): wood(H, x, 4, 5); H[4][x] = "c"
h(4, 23, 38, "d")
h(2, 12, 22, "2"); h(2, 14, 16, "1"); h(3, 12, 22, "4"); h(3, 18, 21, "3")
put(H, 4, 14, "3"); put(H, 5, 14, "1")                     # bent bolt
put(H, 0, 12, "3"); put(H, 1, 12, "5"); h(0, 13, 21, "2"); h(0, 15, 17, "1"); h(1, 13, 21, "4")
put(H, 0, 22, "k"); put(H, 1, 22, "l")
h(2, 23, 25, "3"); h(3, 23, 25, "4"); put(H, 1, 24, "3")
h(2, 26, 38, "b"); h(3, 26, 38, "c")
h(2, 39, 43, "2"); h(3, 39, 43, "4"); put(H, 1, 43, "3")
for x in (31, 38): H[2][x] = "2"; H[3][x] = "3"; H[4][x] = "3"
put(H, 6, 14, "2"); h(7, 13, 16, "3"); H[6][16] = "3"
h(6, 17, 19, "3"); h(7, 17, 19, "5")
H.insert(0, ["."] * 44)                                   # lift the scope onto a visible mount
for y in (0, 1): H[y][12:23] = H[y + 1][12:23]
H[2][12:23] = list("..55....5..")
B = ["".join(r) for r in H]
GRIP = (11.5, 6.0); SUPPORT = (30.0, 6.5); MUZZLE = (44, 4.0)
