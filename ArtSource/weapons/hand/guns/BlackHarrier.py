# Black Harrier (Wildey-pattern magnum, stainless as in the reference photo): long ventilated-rib barrel with a slotted rib and a
# knurled barrel ring, angled slide serrations, spur hammer, checkered walnut panels in a steel frame.
from common import grid, fill, put
CAT = "sidearm"

# ---------------- A: 52x27 menu sprite
G = grid(52, 27)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# slide
put(G, 0, 6, "33"); put(G, 0, 48, "33")
f(1, 4, 27, "2")
for y in range(2, 8): f(y, 3, 28, "3" if y < 5 else "4")
f(7, 3, 28, "5")
# vent rib + barrel + knurled ring + lug
f(1, 24, 51, "2"); f(2, 28, 51, "3"); f(2, 39, 49, ".")
f(3, 28, 51, "3"); f(4, 28, 51, "4"); f(5, 28, 51, "5"); G[3][51] = G[4][51] = "5"
for y in range(2, 7):
    for x in (31, 32, 33): G[y][x] = "2" if (x + y) % 2 else "4"
G[6][32] = "4"
f(6, 29, 30, "4"); f(7, 29, 31, "4"); f(8, 29, 30, "5")
# hammer + beavertail
put(G, 5, 1, "33"); put(G, 6, 0, "344"); put(G, 7, 1, "44"); put(G, 8, 2, "4")
f(9, 0, 6, "3"); G[9][0] = "2"; f(10, 1, 5, "4")
# frame
f(8, 3, 28, "3"); f(9, 3, 28, "4"); put(G, 8, 21, "22"); G[9][16] = "3"
# trigger guard
for y, row in {10: "4......44", 11: "4.......43", 12: "4.......43", 13: "4.......4.", 14: "4......4..",
               15: ".4444444...", 16: "..5555....."}.items():
    for i, ch in enumerate(row):
        if ch != ".": G[y][16 + i] = ch
put(G, 11, 19, "2"); put(G, 12, 19, "2"); put(G, 13, 20, "2")             # trigger
# grip: steel frame edges, checkered walnut panel
GRIPWOOD = set()
BK = {y: 4 if y < 13 else 3 if y < 19 else 2 for y in range(10, 27)}
FR = {y: 15 if y < 13 else 14 if y < 19 else 13 if y < 23 else 12 for y in range(10, 27)}
for y in range(10, 26):
    a, b = BK[y], FR[y]
    f(y, a, b, "4"); G[y][a] = "3"; G[y][b] = "5"
    if 11 <= y <= 24:
        for x in range(a + 2, b - 1): G[y][x] = "c" if (x + 2 * y) % 3 == 0 else "d"; GRIPWOOD.add((x, y))
        G[y][a + 2] = "b"
f(26, 2, 12, "5"); f(25, 2, 12, "4"); G[25][2] = "3"
# stainless: lift the steel ramp one step, then cut the dark details in
M = {"2": "0", "3": "1", "4": "2", "5": "3"}
for y in range(27):
    for x in range(52):
        if (x, y) not in GRIPWOOD: G[y][x] = M.get(G[y][x], G[y][x])
f(2, 19, 26, "4"); f(3, 19, 26, "3"); G[2][19] = "3"                      # top vent window
for y in range(4, 7):                                                     # angled serrations
    for x in range(9, 17, 2): G[y][x] = "3"
A = ["".join(r) for r in G]

# ---------------- B: 21x12 in hand
B = ["..2" + "." * 16 + "2.",
     "." + "0" * 9 + "0" + "0" * 10,
     "." + "1" + "3131" + "1111" + "0" + "2" + "22" + "......" + "2",
     "." + "2" + "1313" + "2222" + "1" + "2" + "1" * 8 + "3",
     "1" + "3" * 9 + "0" + "2" + "2" * 8 + "3",
     "0" + "1" * 11 + "22" + "." * 7,
     ".1" + "bdc" + "2" + "2" + "." + "4" + "." + "1" + "." * 10,
     ".1" + "cdd" + "2" + "." + "." + "4" + "." + "1" + "." * 10,
     ".1" + "bdc" + "2" + "." + "222" + "." * 11,
     "1" + "bcdd" + "2" + "." * 15,
     "1" + "bddc" + "2" + "." * 15,
     "2" + "2222" + "3" + "." * 15]
GRIP = (3.5, 8.5); SUPPORT = None; MUZZLE = (21, 4.0)
