# Halden Service Revolver (Colt New Service / M1917 pattern): worn bright frame, dark fluted cylinder,
# long tapered barrel with ejector rod + knob, spur hammer, bird's-head wood grip with lanyard ring.
from common import grid, fill, put
CAT = "sidearm"

# ---------------- A: 50x29 menu sprite
G = grid(50, 29)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# barrel + front sight + ejector rod
put(G, 0, 46, "33")
f(1, 28, 49, "2"); f(2, 28, 49, "3"); f(3, 28, 49, "3"); f(4, 28, 49, "4"); G[2][49] = G[3][49] = "5"
f(5, 28, 35, "3"); put(G, 5, 33, "222"); put(G, 6, 33, "444")
# frame: top strap, recoil shield, under-cylinder bridge, crane
f(0, 16, 27, "1"); f(1, 13, 28, "2")
for y in range(2, 13): f(y, 13, 17, "2"); G[y][13] = "1"
for y in range(2, 12): f(y, 27, 29, "2"); G[y][29] = "3"
f(10, 13, 29, "2"); f(11, 13, 29, "2"); f(12, 14, 29, "3"); G[11][22] = "3"; G[11][24] = "3"
G[9][15] = "3"; G[5][15] = "3"                                            # side-plate screws
# cylinder
for y in range(2, 10): f(y, 18, 26, "4")
f(2, 19, 25, "3"); f(3, 18, 26, "3"); f(5, 18, 26, "5"); f(8, 18, 26, "5"); f(9, 19, 25, "4")
G[2][18] = G[2][26] = "2"; f(4, 18, 26, "4"); f(6, 18, 26, "3"); f(7, 18, 26, "4")
# hammer (spur back and up)
put(G, 0, 10, "3"); put(G, 1, 10, "234"); put(G, 2, 11, "3442"); put(G, 3, 12, "42")
# backstrap curving into the grip
for y, (a, b) in {3: (11, 13), 4: (9, 13), 5: (8, 13), 6: (7, 13), 7: (6, 13), 8: (6, 13), 9: (5, 13)}.items():
    f(y, a, b, "2"); G[y][a] = "1"
# wood grip
BK = {9: 6, 10: 5, 11: 5, 12: 4, 13: 4, 14: 3, 15: 3, 16: 2, 17: 2, 18: 1, 19: 1, 20: 1}
FR = {9: 13, 10: 13, 11: 13, 12: 13, 13: 12, 14: 11, 15: 11, 16: 10, 17: 10, 18: 10, 19: 10, 20: 10, 21: 10, 22: 10, 23: 10, 24: 10, 25: 10}
for y in range(9, 26):
    a, b = BK.get(y, 0), FR[y]
    f(y, a, b, "c"); G[y][a] = "b"; G[y][a + 1] = "b"; G[y][b] = "d"
    if y > 12: G[y][b - 1] = "d"
for (x, y) in ((4, 14), (6, 19), (3, 22), (7, 16), (5, 24), (8, 21)): G[y][x] = "d"
G[19][4] = "2"                                                            # grip screw
f(26, 0, 10, "3"); f(26, 1, 9, "2"); G[26][10] = "4"                      # butt cap
put(G, 27, 2, "3..3"); put(G, 28, 3, "33")                                # lanyard ring
# trigger guard + trigger
for y, row in {13: ".2.....2....23", 14: ".2.....2....23", 15: ".2....2.....2.", 16: "..2........2..",
               17: "...22222222...", 18: "....333333...."}.items():
    for i, ch in enumerate(row):
        if ch != ".": G[y][14 + i] = ch
A = ["".join(r) for r in G]

# ---------------- B: 21x11 in hand
B = ["....3.111111.......3.",
     "...34223333322222222",
     "....212" + "33333" + "3" * 8 + "4",
     "...2212" + "44444" + "2" + "3333" + "....",
     "..2bb22" + "55555" + "22" + ".......",
     ".2bcc" + "222222222" + ".......",
     ".bccd3.2.3" + "." * 11,
     "bcccd3.2.3" + "." * 11,
     "bcccd.333" + "." * 12,
     "bcccd" + "." * 16,
     "23334" + "." * 16]
B[1] = B[1] + "4"
GRIP = (2.5, 7.5); SUPPORT = None; MUZZLE = (21, 2.0)
