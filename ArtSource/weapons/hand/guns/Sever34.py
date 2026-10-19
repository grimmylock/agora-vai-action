# Sever-34 (IZh-27-pattern over/under): walnut pistol-grip stock, grey boxlock receiver with hinge pin,
# stacked blued barrels under a ventilated rib, wood fore-end around the lower barrel.
from common import grid, fill, put
CAT = "shotgun"

# ---------------- A: 62x12 menu sprite
G = grid(62, 12)
f = lambda y, a, b, c: fill(G, y, a, b, c)
TOP = {x: 3 if x < 13 else 2 if x < 17 else 1 for x in range(2, 21)}
BOT = {2: 11, 3: 11, 4: 10, 5: 10, 6: 10, 7: 9, 8: 9, 9: 9, 10: 9, 11: 8, 12: 8, 13: 9, 14: 10, 15: 10, 16: 10,
       17: 9, 18: 8, 19: 7, 20: 7}
for x in range(2, 21):
    t, b = TOP[x], BOT[x]
    for y in range(t, b + 1): G[y][x] = "b"
    G[t][x] = "a"; G[b][x] = "d"
    if b - 1 > t and x < 13: G[b - 1][x] = "c"
for (x, y) in ((14, 7), (16, 7), (15, 8), (14, 9), (16, 9), (17, 8)): G[y][x] = "c"   # grip checkering
for y in range(5, 8): G[y][18] = "c"
for y in range(3, 12): f(y, 0, 1, "4")               # recoil pad
G[3][0] = "."; G[3][1] = "3"; G[11][0] = "5"
for y in range(4, 11): G[y][1] = "5"
# receiver: grey, lit top, hinge pin, shaded belly
for y in range(1, 8): f(y, 20, 27, "2")
f(1, 21, 27, "1"); G[1][20] = "3"; f(7, 20, 27, "4"); f(6, 20, 27, "3")
put(G, 4, 25, "4"); put(G, 3, 25, "3"); G[3][22] = G[5][22] = "3"
G[7][27] = "."
# barrels: ventilated rib, top barrel, seam, bottom barrel, muzzles
f(0, 27, 61, "2")
for x in range(29, 60, 3): G[0][x] = "4"
f(1, 28, 61, "3"); f(2, 28, 61, "4"); f(3, 28, 61, "5"); f(4, 28, 61, "3"); f(5, 28, 61, "4")
f(1, 28, 61, "2"); G[0][61] = "1"
for y in range(1, 6): G[y][61] = "4"
# fore-end around the lower barrel
for y in range(3, 7): f(y, 28, 42, "b")
f(3, 28, 42, "a"); f(6, 29, 41, "d"); f(5, 28, 42, "c"); G[6][28] = "."; G[6][42] = "."
# trigger guard (hole open) + trigger
put(G, 8, 25, "3"); put(G, 9, 25, "3"); f(10, 19, 24, "3"); G[9][18] = "3"
put(G, 8, 22, "4"); put(G, 9, 22, "4")
A = ["".join(r) for r in G]

# ---------------- B: 40x7 in hand
B = ["." * 13 + "11222" + "2" * 21 + "1",
     "........" + "aaab" + "b" + "22223" + "4" * 22,
     "4" + "aaaaaaabbbbb" + "23233" + "aaaaaaaaab" + "333333333334",
     "4" + "bbbbbbbbbccd" + "3" + "3.23" + "ccccccccdd" + "5555555555.4",
     "4bbbbbbbcc" + "." + "bcd" + "3333" + "." * 22,
     "4bbbbcd" + "...." + "bcd" + "." * 26,
     "4cd" + "." * 9 + "dd" + "." * 26]
GRIP = (12.0, 4.5); SUPPORT = (23.0, 3.0); MUZZLE = (40, 1.0)
