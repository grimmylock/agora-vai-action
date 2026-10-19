# Hound-13 (Mossberg 500-pattern pump): black synthetic stock, blued receiver with tang safety and
# bright bolt in the port, corrugated polymer pump, barrel over magazine tube with barrel clamp.
from common import grid, fill, put
CAT = "shotgun"

# ---------------- A: 60x12 menu sprite
G = grid(60, 12)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# stock: row -> x range; lit comb, dark belly, rubber pad
STOCK = {1: (17, 19), 2: (14, 19), 3: (1, 19), 4: (1, 18), 5: (1, 17), 6: (1, 17), 7: (1, 13), 8: (1, 10),
         9: (1, 7), 10: (1, 5)}
inn = lambda x, y: y in STOCK and STOCK[y][0] <= x <= STOCK[y][1]
for y, (a, b) in STOCK.items():
    for x in range(a, b + 1):
        c = "q"
        if not inn(x, y + 1): c = "r"
        if not inn(x, y - 1): c = "p"
        G[y][x] = c
for y in range(3, 11): G[y][0] = "r"
put(G, 11, 1, "rr"); f(4, 2, 13, "q")
# receiver: lit top, tang safety, ejection port with bright bolt, loading-port shadow
f(0, 21, 31, "2"); f(1, 20, 31, "3"); f(2, 20, 31, "4"); f(3, 20, 31, "4"); f(4, 20, 31, "5")
put(G, 0, 19, "3"); G[0][21] = "1"; G[1][20] = "2"
put(G, 1, 25, "5115"); put(G, 2, 25, "5555")
G[3][20] = "5"; G[4][20] = "."; G[4][31] = "."
# trigger guard (hole open) + trigger
put(G, 5, 21, "3"); put(G, 5, 26, "3"); f(6, 21, 26, "3"); f(6, 22, 25, "2"); put(G, 5, 24, "4"); put(G, 6, 21, "4")
# barrel, magazine tube + cap, barrel clamp, bead
f(0, 32, 59, "2"); f(1, 32, 59, "4"); G[0][59] = "1"; G[1][59] = "5"
f(2, 32, 52, "3"); f(3, 32, 52, "5"); put(G, 2, 53, "2"); put(G, 3, 53, "4")
for y in range(1, 4): f(y, 49, 50, "4")
f(1, 49, 50, "3")
# corrugated pump
for y in range(1, 5): f(y, 36, 47, "q")
f(1, 36, 47, "p"); f(4, 37, 46, "r")
for x in range(38, 46, 2):
    for y in (2, 3): G[y][x] = "r"
G[4][36] = "."; G[4][47] = "."
A = ["".join(r) for r in G]

# ---------------- B: 38x8 in hand
B = ["." * 13 + "3222112" + "2" * 17 + "1",
     ".........." + "pqq" + "3351153" + "444" + "pppppppp" + "4444444",
     "r" + "pppppppppqqq" + "4444445" + "333" + "qrqrqrqr" + "3332" + "...",
     "r" + "qqqqqqqqqqqr" + "5" + "3.23" + "....." + "rrrrrrrr" + "." * 7,
     "r" + "qqqqqqqqqqr" + ".." + "3333" + "." * 20,
     "r" + "qqqqqqqqr" + "." * 28,
     "r" + "qqqqqr" + "." * 31,
     "rrrr" + "." * 34]
GRIP = (11.0, 4.0); SUPPORT = (26.5, 3.0); MUZZLE = (38, 1.0)
