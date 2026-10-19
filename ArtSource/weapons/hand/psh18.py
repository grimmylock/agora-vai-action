# PSh-18 "Pchela" (Makarov pattern): hand-pixelled 28x19 over trace_PSh18Pchela.png.
# Blued steel slide/frame, brown checkered grip panel, round trigger guard.
from common import *
G = grid(28, 19)
f = lambda y, a, b, c: fill(G, y, a, b, c)

# --- slide: lit top streak, rear serrations, rounded ends, dark lower edge, safety lever
f(1, 3, 26, "2"); f(1, 7, 23, "1")
f(2, 2, 27, "3"); f(3, 2, 27, "4"); f(4, 2, 27, "4"); f(4, 8, 27, "5")
G[0][4] = "3"; G[0][25] = "3"                      # rear + front sight
for x in (3, 5, 7): G[2][x] = G[3][x] = "5"         # serrations
put(G, 4, 3, "2222")                                # safety lever
G[1][27] = "."; G[4][2] = "3"

# --- hammer spur behind the slide
put(G, 1, 0, "23"); put(G, 2, 0, "34"); put(G, 3, 1, "4")

# --- frame + dust cover
f(5, 3, 27, "4"); f(5, 13, 27, "3"); G[5][27] = "4"; f(6, 20, 26, "5")

# --- grip: steel back/front strap, brown checkered panel, screw, magazine base
GRIP = {6: (3, 12), 7: (3, 12), 8: (3, 12), 9: (2, 12), 10: (2, 11), 11: (2, 11), 12: (1, 11),
        13: (1, 10), 14: (1, 10), 15: (0, 10), 16: (0, 10), 17: (0, 9)}
for y, (a, b) in GRIP.items():
    f(y, a, b, "4"); G[y][a] = "3"; G[y][b] = "5"
    if 7 <= y <= 16:
        for x in range(a + 2, b - 1): G[y][x] = "d" if (x + 2 * y) % 4 == 0 else "c"   # checkering
        if y == 7: f(y, a + 2, b - 2, "b")          # panel top catches light
G[11][6] = "1"                                      # grip screw
f(18, 1, 9, "5"); put(G, 18, 1, "4")                # magazine base

# --- trigger guard loop + trigger
for y in (6, 7, 8): G[y][21] = "3"
G[9][20] = "3"; f(10, 14, 19, "3"); f(10, 15, 18, "2"); G[9][13] = "3"
G[6][15] = G[7][15] = "3"; G[8][16] = "3"          # trigger

# B 14x10 by hand (each px = 2x2 of A)
B = ["3221111111112.",
     "4545333333333" + "3",
     ".222" + "5" * 10,
     ".34bb453..3555",
     ".34cc45.3.3...",
     ".34cc5.222....",
     "34cc45" + "." * 8,
     "34cd45" + "." * 8,
     "34d45." + "." * 8,
     "45555" + "." * 9]
A, B = build_ab(G, B)
