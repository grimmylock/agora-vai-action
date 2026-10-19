# Tambour-12 (Striker/Protecta-pattern drum shotgun): strut stock with tall buttpad, steel receiver,
# big rotary drum, polymer pistol grip and vertical foregrip, barrel shroud.
from common import grid, fill, put
CAT = "shotgun"

# ---------------- A: 60x14 menu sprite
G = grid(60, 14)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# buttpad + strut + hinge
for y in range(1, 13): f(y, 0, 2, "q")
for y in range(1, 13): G[y][0] = "p"
G[1][2] = "."; f(12, 0, 2, "r"); G[1][1] = "p"
f(4, 3, 21, "2"); f(5, 3, 21, "4")
for y in range(3, 7): f(y, 18, 21, "3")
f(3, 18, 21, "2"); f(6, 18, 21, "4"); G[4][19] = "5"; G[5][19] = "5"
# receiver + top bump
for y in range(1, 7): f(y, 22, 30, "3")
f(1, 22, 30, "2"); f(5, 22, 30, "4"); f(6, 22, 30, "5"); put(G, 0, 24, "3333"); G[0][24] = "2"
put(G, 3, 23, "5555"); put(G, 2, 23, "2")
# pistol grip with finger grooves
GR = {7: (23, 27), 8: (23, 27), 9: (22, 26), 10: (22, 26), 11: (21, 26), 12: (21, 25), 13: (21, 25)}
for y, (a, b) in GR.items():
    f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "r"
f(13, 21, 25, "r"); G[9][26] = "."; G[11][26] = "."
# trigger guard (hole open) + trigger
for y in (7, 8, 9): G[y][30] = "3"
f(10, 27, 30, "3"); put(G, 7, 28, "4"); put(G, 8, 28, "4")
# drum: rounded cylinder, lit top, dark belly, rims
DR = {0: (33, 41), 1: (32, 42), 12: (32, 42), 13: (33, 41)}
for y in range(14):
    a, b = DR.get(y, (31, 43))
    c = "2" if y < 2 else "3" if y < 8 else "4" if y < 11 else "5"
    f(y, a, b, c); G[y][a] = "4" if y < 11 else "5"; G[y][b] = "4" if y < 11 else "5"
f(0, 34, 40, "1")
for y in range(2, 11): G[y][32] = "2" if y < 8 else "3"
put(G, 6, 36, "545"); put(G, 7, 37, "5")          # winding-key hub
# barrel shroud, rail stub, vertical foregrip
f(1, 44, 59, "2"); f(2, 44, 59, "3"); f(3, 44, 59, "4"); G[2][59] = "5"; G[1][59] = "3"
f(4, 46, 54, "4")
FG = {5: (48, 52), 6: (48, 52), 7: (48, 52), 8: (48, 52), 9: (49, 52), 10: (49, 52)}
for y, (a, b) in FG.items():
    f(y, a, b, "q"); G[y][a] = "p"; G[y][b] = "r"
f(10, 49, 52, "r")
A = ["".join(r) for r in G]

# ---------------- B: 36x8 in hand
B = ["." * 14 + "2" * 11 + "." * 11,
     "pq" + "." * 11 + "3" + "3443" + "5" + "222222" + "5" + "2" * 10,
     "pq" + "2" * 11 + "4" + "4554" + "5" + "333333" + "5" + "4" * 9 + "5",
     "pq" + "4" * 11 + "5" + "5445" + "5" + "333333" + "5" + ".." + "pqq" + ".....",
     "pq" + "." * 12 + "pqq" + "3" + "5" + "444444" + "5" + ".." + "pqq" + ".....",
     "pq" + "." * 12 + "pqr" + "3" + "5" + "444444" + "5" + ".." + "pqq" + ".....",
     "pq" + "." * 11 + "pqr" + ".." + "5" + "444444" + "5" + ".." + "rrr" + ".....",
     "qq" + "." * 11 + "rrr" + "..." + "555555" + "." * 11]
GRIP = (15.0, 5.5); SUPPORT = (29.0, 4.5); MUZZLE = (36, 1.5)
