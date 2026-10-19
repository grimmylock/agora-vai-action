# TS-9 Tishina (AS Val pattern): skeleton folding stock (extended), grey parkerized receiver with
# rear-sight block and AK selector, black polymer grip, steel 20-round mag, long fat integral
# suppressor with front sight and end cap.
from common import grid, fill, put
CAT = "assault"

# ---------------- A: 60x15 menu sprite
G = grid(60, 15)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# skeleton stock: buttplate, top strut, diagonal lower strut
for y in range(3, 11): f(y, 0, 1, "4")
for y in range(3, 11): G[y][0] = "3"
G[10][0] = "5"; G[10][1] = "5"
f(3, 2, 16, "3"); f(4, 2, 16, "4"); f(3, 2, 15, "2")
for x in range(2, 17):
    y = 9 - round((x - 2) * 4 / 14)
    G[y][x] = "3"; G[y + 1][x] = "4"
# receiver: rear-sight block, lit top, port, selector, rivets
put(G, 0, 22, "22222"); G[0][26] = "3"
f(1, 19, 36, "1"); G[1][19] = "2"
for y in range(2, 6): f(y, 16, 36, "2")
f(2, 16, 18, "3"); f(4, 16, 36, "3"); f(5, 17, 36, "4"); G[5][16] = "."
f(2, 27, 31, "5"); f(3, 27, 31, "5"); put(G, 3, 32, "1")
put(G, 3, 20, "1111110")
G[4][18] = G[4][34] = "4"
# black polymer pistol grip
GR = {6: (19, 22), 7: (19, 22), 8: (18, 22), 9: (18, 21), 10: (18, 21), 11: (17, 21), 12: (17, 20)}
for y, (a, b) in GR.items(): f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "r"
f(12, 17, 20, "r"); G[8][21] = "r"; G[10][20] = "r"
# trigger guard (hole open) + trigger
for y in (6, 7): G[y][26] = "3"
f(8, 23, 26, "3"); put(G, 6, 24, "4")
# steel 20-round mag with witness slots
for y in range(6, 15):
    a = 27 + (y - 6) // 4; b = a + 5
    f(y, a, b, "3"); G[y][a] = "2"; G[y][b] = "4"
    if y in (8, 10, 12): G[y][a + 3] = "5"
f(14, 29, 34, "4"); f(6, 28, 31, "4")
# integral suppressor: rear collar, fat can, front sight, end cap
for y in range(1, 6): f(y, 37, 57, "4")
f(1, 37, 57, "3"); f(2, 37, 57, "3"); f(1, 39, 55, "2"); f(5, 37, 57, "5")
for y in range(1, 6): G[y][38] = "3" if y < 3 else "4"
G[5][37] = "5"
put(G, 0, 41, "33")
for y in range(0, 6): f(y, 58, 59, "3")
f(0, 58, 59, "2"); f(5, 58, 59, "4"); G[0][59] = "."; G[3][59] = "4"; G[4][59] = "5"
A = ["".join(r) for r in G]

# ---------------- B: 36x9 in hand
B = ["." * 12 + "2222" + "." * 6 + "3" + "." * 11 + "33",
     "........." + "3" + "11111111111" + "3333333333333" + "33",
     "3" + "33333333" + "2" + "22255552222" + "4444444444444" + "44",
     "4" + "......" + "33" + "3" + "31111133333" + "5555555555555" + "45",
     "4" + "...." + "33" + "..." + "pqr" + ".43" + "2334" + "." * 16,
     "4" + ".." + "33" + "....." + "pqr" + "333" + "2334" + "." * 16,
     "433" + "......" + "pqr" + "...." + "2534" + "." * 16,
     "." * 9 + "rrr" + "....." + "2334" + "." * 15,
     "." * 17 + "4444" + "." * 15]
GRIP = (10.5, 5.5); SUPPORT = (24.0, 3.0); MUZZLE = (36, 2.5)
