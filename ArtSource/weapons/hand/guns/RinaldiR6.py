# Rinaldi R6 (Chiappa Rhino pattern): bright frame, flat-sided black cylinder, low bore (barrel fires from
# the bottom chamber), tall barrel shroud with three oval cut-outs, cocking lever on top, raked black grip.
# Reference photo is tilted ~14 deg; drawn as a straight side profile.
from common import grid, fill, put
CAT = "sidearm"

# ---------------- A: 50x24 menu sprite
G = grid(50, 24)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# frame top strap + shroud (bright)
f(2, 9, 24, "0"); f(3, 8, 49, "0")
for y in range(4, 13): f(y, 8, 49, "1")
f(11, 24, 49, "2"); f(12, 24, 49, "3"); G[10][49] = "4"; G[11][49] = "5"     # low bore at the muzzle
f(4, 24, 49, "1"); G[3][49] = "1"
for (a, b, y0, y1) in ((27, 31, 5, 7), (35, 40, 5, 7), (43, 47, 5, 7)):        # oval cut-outs
    for y in range(y0, y1 + 1): f(y, a, b, ".")
    G[y0][a] = G[y0][b] = G[y1][a] = G[y1][b] = "1"
for (a, b) in ((27, 31), (35, 40), (43, 47)): f(8, a, b, "2")                 # shadow under each cut-out
f(9, 26, 48, "1"); f(10, 26, 48, "2"); f(10, 26, 48, "1")
# cylinder (black, flat-sided) + release
for y in range(2, 11): f(y, 13, 23, "4")
f(2, 13, 23, "3"); f(3, 13, 23, "3"); f(10, 13, 23, "5"); f(6, 14, 22, "5")
G[2][13] = G[2][23] = "0"
for y in range(4, 9): f(y, 24, 26, "5")
G[4][24] = "4"
# cocking lever
put(G, 0, 10, "44"); put(G, 1, 10, "4444"); put(G, 2, 11, "44")
# frame under cylinder + trigger guard
f(11, 9, 25, "1"); f(12, 9, 25, "2")
for y, row in {13: "1......1", 14: "1......12", 15: "1......1.", 16: ".1....12.", 17: "..2222..."}.items():
    for i, ch in enumerate(row):
        if ch != ".": G[y][15 + i] = ch
put(G, 13, 19, "4"); put(G, 14, 19, "4"); put(G, 15, 20, "4")                # trigger
# grip (raked, black, checkered)
BK = {3: 6, 4: 6, 5: 5, 6: 5, 7: 5, 8: 4, 9: 4, 10: 4, 11: 3, 12: 3, 13: 3, 14: 2, 15: 2, 16: 2, 17: 1, 18: 1, 19: 1, 20: 1, 21: 1, 22: 2, 23: 3}
FR = {3: 8, 4: 9, 5: 10, 6: 11, 7: 12, 8: 12, 9: 13, 10: 13, 11: 13, 12: 12, 13: 11, 14: 10, 15: 10, 16: 9, 17: 9, 18: 10, 19: 10, 20: 9, 21: 9, 22: 8, 23: 7}
for y in range(3, 24):
    a, b = BK.get(y, 0), FR[y]
    f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "r"
    if 7 <= y <= 21:
        for x in range(a + 2, b - 1):
            if (x + y) % 3 == 0: G[y][x] = "r"
f(23, 3, 7, "r"); G[3][7] = "p"
put(G, 2, 46, "RR")                                                       # fibre front sight
A = ["".join(r) for r in G]

# ---------------- B: 20x10 in hand
B = ["....q4" + "." * 14,
     "..." + "p" + "0" + "33333" + "0" * 10,
     ".." + "pq" + "1" + "44444" + "11..11..12",
     ".." + "pq" + "1" + "45554" + "1" * 9 + "2",
     "." + "pqq" + "1" + "55555" + "1" * 8 + "2" + "5",
     "." + "pqqr" + "1" * 7 + "2" * 7 + ".",
     "." + "pqrq" + "1" + "." + "4" + "." + "." + "1" + "." * 9,
     "pqrq" + "." + "2222" + "." * 11,
     "pqqr" + "." * 16,
     "rrr" + "." * 17]
GRIP = (2.5, 6.0); SUPPORT = None; MUZZLE = (20, 4.5)
