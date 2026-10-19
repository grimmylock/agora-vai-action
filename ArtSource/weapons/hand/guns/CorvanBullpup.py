# Corvan bullpup (F2000 pattern): one-piece black polymer shell, integral compact optic on top, big
# full-hand trigger guard far forward with the pistol grip behind it, magazine behind the grip in the
# butt, sloped nose, short barrel stub with flash hider.
from common import grid, fill, put
CAT = "assault"

def shell(G, tops, bots):
    for x in tops:
        t, b = tops[x], bots[x]
        for y in range(t, b + 1): G[y][x] = "q"
        G[t][x] = "p"; G[b][x] = "r"

# ---------------- A: 54x23 menu sprite
G = grid(54, 25)
f = lambda y, a, b, c: fill(G, y, a, b, c)
tops = {0: 10, 1: 9, **{x: 8 for x in range(2, 15)}, **{x: 7 for x in range(15, 44)}, 44: 8, 45: 8, 46: 9}
bots = {0: 17, 1: 19, **{x: 19 for x in range(2, 16)}, **{x: 20 for x in range(16, 26)}, 26: 19, 27: 18, 28: 17,
        **{x: 16 for x in range(29, 44)}, 44: 15, 45: 14, 46: 13}
shell(G, tops, bots)
for x in tops:
    for y in range(13, bots[x]): G[y][x] = "4"            # lower shell in shadow
for y in range(10, 18): G[y][0] = "r"
for y in range(9, 20): G[y][1] = "r" if y > 9 else "q"
for y in range(9, 19): G[y][15] = "r"                    # butt-module seam
f(9, 3, 13, "p")                                          # cheek rest lip
for x in range(2, 15): G[18][x] = "r"
for x in range(29, 44): G[15][x] = "r"
f(11, 22, 42, "r"); f(12, 23, 42, "p")                    # ejection-tube groove over lit lip
for y in range(15, 19): f(y, 20, 25, ".")                 # full-hand trigger guard opening
put(G, 15, 21, "2"); put(G, 16, 21, "3")                  # trigger
for y in range(14, 20): G[y][16] = "p"                    # lit back of the grip
for (x, y) in ((18, 16), (17, 18), (19, 18), (18, 19)): G[y][x] = "r"   # stippling
f(19, 21, 25, "q"); f(20, 18, 25, "r")
# magazine behind the grip
MAG = {20: (10, 14), 21: (10, 14), 22: (11, 15), 23: (11, 15)}
for y, (a, b) in MAG.items(): f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "r"
f(24, 11, 15, "r")
# top rail + integral optic
for x in range(16, 43, 2): G[7][x] = "3"
f(2, 23, 35, "2"); f(3, 21, 36, "3"); f(3, 25, 32, "1"); f(4, 21, 36, "4"); f(5, 21, 36, "4"); f(6, 22, 36, "5")
for y in (3, 4, 5): G[y][20] = "5"
put(G, 3, 37, "k"); put(G, 4, 37, "l"); put(G, 5, 37, "l")
f(6, 26, 31, "."); G[6][22] = G[6][36] = "."
# nose facet + barrel stub with flash hider
G[8][44] = "q"; G[9][45] = "q"
put(G, 10, 47, "2221212"); put(G, 11, 47, "4435354")
A = ["".join(r) for r in G if "".join(r).strip(".")]   # drop empty rows

# ---------------- B: 31x15 in hand
H = grid(31, 15)
h = lambda y, a, b, c: fill(H, y, a, b, c)
tops = {0: 6, **{x: 5 for x in range(1, 8)}, **{x: 4 for x in range(8, 25)}, 25: 5, 26: 6}
bots = {0: 10, **{x: 11 for x in range(1, 9)}, **{x: 12 for x in range(9, 15)}, 15: 12, 16: 11,
        **{x: 9 for x in range(17, 25)}, 25: 9, 26: 8}
shell(H, tops, bots)
for x in tops:
    for y in range(8, bots[x]): H[y][x] = "4"
for y in range(6, 11): H[y][0] = "r"
for y in range(5, 11): H[y][8] = "r" if y > 4 else H[y][8]
h(7, 13, 23, "r")                                         # ejection groove
for y in (9, 10): h(y, 12, 14, ".")                       # guard opening
H[9][12] = "3"                                            # trigger
for y in range(8, 12): H[y][9] = "p"
H[10][10] = "r"
h(11, 12, 15, "q"); h(12, 9, 15, "r")
for y, (a, b) in {12: (5, 8), 13: (5, 8)}.items(): h(y, a, b, "q"); H[y][a] = "p"; H[y][b] = "r"
h(14, 6, 8, "r")
h(0, 13, 19, "2"); h(1, 12, 20, "3"); h(1, 14, 17, "1"); h(2, 12, 20, "4"); H[1][11] = H[2][11] = "5"
put(H, 1, 21, "k"); put(H, 2, 21, "l"); put(H, 3, 13, "5"); put(H, 3, 19, "5")
put(H, 6, 27, "2212"); put(H, 7, 27, "4354")
B = ["".join(r) for r in H]
GRIP = (10.0, 10.5); SUPPORT = (21.0, 10.0); MUZZLE = (31, 7.0)
