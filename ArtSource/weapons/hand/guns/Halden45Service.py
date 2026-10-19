# Halden .45 Service (1911 pattern): blued slide with rear serrations, spur hammer, beavertail tang,
# checkered brown grip panels, round trigger guard, slide nose ahead of the dust cover.
from common import grid, fill, put
CAT = "sidearm"

# ---------------- A: 48x30 menu sprite
G = grid(48, 30)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# slide
put(G, 0, 8, "333"); G[0][45] = "3"                                        # sights
f(1, 6, 47, "2"); f(1, 6, 7, "3")
for y in (2, 3, 4, 5): f(y, 6, 47, "3")
f(6, 6, 47, "4")
G[3][47] = "5"; G[4][47] = "5"                                            # bore
for x in (9, 11, 13, 15):                                                 # rear serrations
    for y in (2, 3, 4, 5): G[y][x] = "5"
for x in (10, 12, 14): G[2][x] = "2"
f(7, 38, 47, "3"); f(8, 38, 47, "4"); G[8][47] = "."; G[7][38] = "4"      # slide nose round the recoil spring
# hammer + beavertail tang
put(G, 2, 1, "23"); put(G, 3, 2, "344"); put(G, 4, 3, "44"); f(5, 4, 5, "4")
f(7, 2, 6, "3"); f(8, 0, 6, "3"); G[8][0] = "2"; f(9, 1, 6, "4"); f(10, 3, 5, "4")
# frame + dust cover, slide stop
f(7, 6, 37, "3"); f(8, 6, 37, "4"); f(9, 15, 26, "4")
put(G, 7, 19, "2222"); G[8][20] = "2"
# trigger guard (open hole)
f(9, 15, 27, "4")
for y, (h0, h1, w0, w1) in {10: (19, 25, 26, 27), 11: (19, 25, 26, 27), 12: (19, 25, 26, 27), 13: (19, 24, 25, 26), 14: (19, 23, 24, 25)}.items():
    f(y, h0, h1, "."); f(y, w0, w1, "4")
G[10][27] = G[11][27] = "3"; f(15, 19, 23, "5"); G[14][24] = "5"; G[13][26] = "5"
put(G, 10, 20, "3"); put(G, 11, 20, "2"); put(G, 12, 20, "2"); put(G, 13, 20, "3")   # trigger
# grip
BK = {y: 4 if y < 12 else 3 if y < 15 else 2 if y < 20 else 1 if y < 25 else 0 for y in range(9, 30)}
FR = {y: 17 if y < 17 else 16 if y < 21 else 15 if y < 26 else 14 for y in range(9, 30)}
for y in range(9, 30):
    a, b = BK[y], FR[y]
    f(y, a, b, "4"); G[y][a] = "3"; G[y][b] = "5"
    if 10 <= y <= 27:
        for x in range(a + 2, b - 1):
            G[y][x] = "c" if (x + 2 * y) % 3 == 0 else "d"
        G[y][a + 2] = "c"
f(29, 1, 13, "5"); f(28, 2, 13, "4"); G[28][1] = "3"
G[12][9] = "2"; G[25][6] = "2"                                            # panel screws
G[13][17] = "2"                                                           # mag catch
A = ["".join(r) for r in G]

# ---------------- B: 19x12 in hand
B = ["...3...........3...",
     ".3" + "2" * 17,
     "34" + "5353" + "3" * 12 + "5",
     "." + "4" * 18,
     "23" + "44444" + "2" + "4444444" + "3333",
     ".3" + "cddd" + "4" + "44444" + "." * 7,
     ".3" + "dcdd" + "4" + "2.." + "44" + "." * 7,
     ".3" + "ddcd" + "4" + "..." + "4" + "." * 8,
     "34" + "cdd" + "4" + "4445" + "." * 9,
     "3" + "cddc" + "4" + "." * 13,
     "3" + "dcdd" + "4" + "." * 13,
     "4" + "4444" + "5" + "." * 13]
GRIP = (3.5, 8.5); SUPPORT = None; MUZZLE = (19, 2.5)
