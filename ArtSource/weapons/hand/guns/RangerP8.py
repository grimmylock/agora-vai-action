# Ranger P8 (870-pattern pump, walnut): semi-pistol-grip wood stock with black pad, blued receiver with
# bright bolt in the port, grooved wood fore-end, barrel over magazine tube.
from common import grid, fill, put
CAT = "shotgun"

# ---------------- A: 60x12 menu sprite
G = grid(60, 12)
f = lambda y, a, b, c: fill(G, y, a, b, c)
STOCK = {1: (18, 20), 2: (13, 20), 3: (2, 20), 4: (2, 19), 5: (2, 18), 6: (2, 17), 7: (2, 15), 8: (2, 10),
         9: (2, 6), 10: (2, 3)}
inn = lambda x, y: y in STOCK and STOCK[y][0] <= x <= STOCK[y][1]
for y, (a, b) in STOCK.items():
    for x in range(a, b + 1):
        c = "b"
        if not inn(x, y + 1): c = "d"
        elif not inn(x, y + 2): c = "c"
        if not inn(x, y - 1): c = "a"
        G[y][x] = c
f(6, 14, 16, "c")                                    # grip swell shadow
for y in range(3, 11): f(y, 0, 1, "4")               # recoil pad + spacer
G[3][0] = "."; G[3][1] = "3"; G[10][0] = "5"; G[10][1] = "5"
for y in range(4, 10): G[y][1] = "5"
# receiver
f(0, 22, 31, "2"); f(1, 20, 31, "3"); f(2, 20, 31, "4"); f(3, 21, 31, "4"); f(4, 21, 31, "5")
put(G, 0, 21, "3"); G[1][20] = "2"
put(G, 1, 24, "51111"); put(G, 2, 24, "55555")
G[4][31] = "."
# trigger guard (hole open)
put(G, 5, 21, "3"); put(G, 5, 26, "3"); f(6, 21, 26, "3"); f(6, 22, 25, "2"); put(G, 5, 24, "4")
# barrel, magazine tube + cap, bead
f(0, 32, 59, "2"); f(1, 32, 59, "4"); G[0][59] = "1"; G[1][59] = "5"
f(2, 32, 53, "3"); f(3, 32, 53, "5"); put(G, 2, 54, "2"); put(G, 3, 54, "4")
# grooved wood fore-end
for y in range(1, 5): f(y, 36, 47, "b")
f(1, 36, 47, "a"); f(4, 37, 46, "d"); G[4][36] = "."; G[4][47] = "."
for x in range(38, 46, 2):
    for y in (2, 3): G[y][x] = "c"
A = ["".join(r) for r in G]

# ---------------- B: 39x7 in hand
B = ["." * 13 + "32222222" + "2" * 17 + "1",
     ".........." + "aab" + "34511543" + "44" + "aaaaaaaaa" + "4444444",
     "4aaaaaaaaabbb" + "44444445" + "33" + "bcbcbcbcb" + "3332" + "...",
     "4bbbbbbbbbbcd" + "5" + "3.23" + "....." + "ccccccccd" + "." * 7,
     "4bbbbbbccc" + "cdd" + "." + "3333" + "." * 21,
     "4bbbbccd" + "." * 31,
     "5ddd" + "." * 35]
GRIP = (11.5, 3.5); SUPPORT = (27.0, 3.0); MUZZLE = (39, 1.0)
