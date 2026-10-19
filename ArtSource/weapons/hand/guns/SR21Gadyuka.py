# SR-21 Gadyuka (SR-1 Vektor / Gyurza pattern): long black slide with raked serrations front and rear,
# stepped nose, bright steel frame insert, black polymer frame with a big rounded trigger guard,
# ribbed front/back straps.
from common import grid, fill, put
CAT = "sidearm"

# ---------------- A: 40x30 menu sprite
G = grid(40, 30)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# slide
put(G, 0, 6, "33"); G[0][36] = "3"
f(1, 3, 38, "2"); G[1][2] = "3"
for y in (2, 3, 4, 5): f(y, 2, 39, "4")
f(2, 2, 39, "3")
for y in (2, 3, 4, 5):                                                    # raked serrations
    for x in range(3, 16):
        if (x + (y > 3)) % 2 == 0: G[y][x] = "5"
    for x in range(27, 35):
        if (x + (y > 3)) % 2 == 0 and y < 4: G[y][x] = "5"
for y in range(1, 7): G[y][25] = "5" if y > 1 else "2"                     # step between slide sections
f(5, 28, 34, "5"); f(4, 28, 34, "2"); G[4][26] = G[4][27] = "4"           # slot
G[1][39] = "."; G[6][39] = "."; f(6, 26, 39, "4"); f(6, 2, 24, "5")
# frame: steel insert band + polymer
f(7, 2, 24, "2"); f(8, 2, 24, "3"); G[7][2] = "1"
f(7, 25, 37, "q"); f(8, 25, 33, "q"); f(7, 25, 37, "p"); G[8][34] = "r"
f(9, 2, 30, "q"); G[9][30] = "r"
# trigger guard
for y, row in {10: "q.......qqr", 11: "q.........qr", 12: "q.........qr", 13: "q.........qr",
               14: "q........qr", 15: "q.......qr.", 16: "rrrrrrrrr.."}.items():
    for i, ch in enumerate(row): G[y][16 + i] = ch
put(G, 10, 19, "4"); put(G, 11, 20, "3"); put(G, 12, 20, "3"); put(G, 13, 19, "34")     # trigger
# grip
BK = {y: 2 if y < 15 else 1 if y < 23 else 0 for y in range(9, 30)}
FR = {y: 15 if y < 15 else 13 if y < 17 else 12 if y < 23 else 11 for y in range(9, 30)}
for y in range(10, 29):
    a, b = BK[y], FR[y]
    f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "r"
    if y % 2 == 0 and 12 <= y <= 27:
        G[y][a] = "r"; G[y][a + 1] = "r"; G[y][b - 1] = "r"; G[y][b - 2] = "r"   # ribbed straps
    if y % 2 == 1 and 12 <= y <= 27: G[y][a + 1] = "p"
f(29, 1, 11, "r"); f(28, 1, 11, "q"); G[28][1] = "p"
G[14][12] = "p"; G[14][13] = "p"                                          # safety lever
A = ["".join(r) for r in G]

# ---------------- B: 18x14 in hand
B = ["..3" + "." * 13 + "3.",
     "." + "2" * 17,
     "." + "5353535" + "4444" + "5" + "3535" + "4",
     "." + "5353535" + "4444" + "5" + "4444" + "4",
     "." + "2" * 11 + "p" + "ppppp",
     "." + "q" * 15 + "..",
     "." + "rpqqq" + "q" + "." + "3" + ".." + "qr" + ".....",
     "." + "pqqqq" + "q" + "." + "3" + ".." + "r" + "......",
     "." + "rqqq" + "r" + "q" + "..." + "r" + ".......",
     "." + "pqqqr" + "rrrr" + "." * 8,
     "r" + "pqqq" + "r" + "." * 12,
     "p" + "qqqq" + "r" + "." * 12,
     "r" + "pqqq" + "r" + "." * 12,
     "q" + "rrrr" + "." * 13]
GRIP = (3.0, 9.5); SUPPORT = None; MUZZLE = (18, 2.5)
