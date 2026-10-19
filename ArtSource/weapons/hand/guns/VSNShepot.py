# VSN Shepot (VSS Vintorez pattern): blonde birch skeleton stock (two open windows, the front post is the
# pistol grip), dark receiver, straight 10-rd box mag, black polymer handguard, fat integral suppressor
# with a front sight, PSO-1-style scope with rubber eyecup, turret block and side mount.
from common import grid, fill, put
CAT = "sniper"

# ---------------- A: 56x15 menu sprite
G = grid(56, 15)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# scope
put(G, 0, 13, "pp"); f(1, 13, 14, "q"); f(2, 13, 14, "q"); f(3, 13, 14, "r")   # eyecup
f(1, 15, 34, "2"); f(2, 15, 34, "4"); f(1, 18, 22, "1"); f(1, 15, 17, "3"); f(2, 15, 17, "5")
f(0, 24, 27, "3"); G[0][25] = "2"; f(1, 23, 28, "3"); f(2, 23, 28, "4"); f(3, 22, 29, "4")  # turret block
put(G, 1, 35, "k"); put(G, 2, 35, "l")
f(3, 19, 21, "5"); f(4, 19, 30, "5"); f(3, 30, 30, "5")    # side mount
# receiver
f(5, 18, 30, "2"); f(5, 19, 24, "1"); f(6, 18, 30, "3"); f(7, 18, 30, "4"); f(8, 18, 30, "5")
f(6, 23, 27, "5")                                         # ejection port
# handguard (black polymer) + integral suppressor
f(5, 31, 37, "p"); f(6, 31, 37, "q"); f(7, 31, 37, "q"); f(8, 31, 37, "r")
f(5, 38, 55, "2"); f(6, 38, 55, "3"); f(7, 38, 55, "4"); f(8, 38, 55, "5"); f(5, 40, 47, "1")
put(G, 4, 52, "3"); put(G, 4, 38, "3")
for y in range(5, 9): G[y][55] = "4"
G[5][55] = "3"
# skeleton stock (birch): top bar, butt, middle strut, grip post, bottom bar
f(7, 1, 17, "a"); f(8, 1, 15, "b")
for y in range(7, 15): G[y][0] = "r"
for y in range(9, 13): put(G, y, 1, "ab"); put(G, y, 9, "ab")
GP = {8: (16, 19), 9: (16, 19), 10: (16, 19), 11: (15, 18), 12: (14, 17), 13: (14, 17)}   # raked grip post
for y, (a, b) in GP.items(): f(y, a, b, "b"); G[y][a] = "a"; G[y][b] = "c"
f(9, 11, 15, "."); f(10, 11, 15, "."); f(11, 11, 14, "."); f(12, 11, 13, ".")   # front window
f(13, 1, 13, "b"); f(14, 1, 18, "c"); G[13][1] = "a"; G[14][18] = "."
# trigger + guard
put(G, 9, 21, "2"); put(G, 10, 21, "3"); G[9][24] = "4"; G[10][24] = "4"; f(11, 20, 24, "4")
# magazine
for y in range(9, 15):
    a = 25 if y < 12 else 26
    f(y, a, a + 5, "4"); G[y][a] = "3"; G[y][a + 5] = "5"
f(14, 26, 31, "5")
A = ["".join(r) for r in G]

# ---------------- B: 38x10 in hand
H = grid(38, 10)
h = lambda y, a, b, c: fill(H, y, a, b, c)
put(H, 0, 8, "p"); h(1, 8, 9, "q"); put(H, 2, 9, "r")
h(1, 10, 22, "2"); h(1, 12, 13, "1"); h(2, 10, 22, "4"); h(0, 15, 17, "3"); h(2, 14, 18, "3")
put(H, 1, 23, "k"); put(H, 2, 23, "l"); h(3, 12, 20, "5")
h(4, 12, 20, "2"); h(4, 13, 15, "1"); h(5, 12, 20, "4"); h(6, 12, 20, "5"); h(5, 15, 18, "5")
h(4, 21, 24, "p"); h(5, 21, 24, "q"); h(6, 21, 24, "r")
h(4, 25, 37, "2"); h(5, 25, 37, "3"); h(6, 25, 37, "4"); h(4, 27, 31, "1"); put(H, 3, 35, "3")
h(5, 1, 11, "a")
for y in range(5, 10): H[y][0] = "r"
for y in range(6, 9): H[y][1] = "b"; H[y][7] = "b"
for y, (a, b) in {6: (10, 12), 7: (10, 12), 8: (9, 11), 9: (9, 11)}.items(): h(y, a, b, "b"); H[y][a] = "a"; H[y][b] = "c"
h(9, 1, 9, "c")
put(H, 7, 14, "2"); h(8, 13, 16, "4")
for y in range(7, 10): h(y, 17, 20, "4"); H[y][17] = "3"; H[y][20] = "5"
h(9, 17, 20, "5")
B = ["".join(r) for r in H]
GRIP = (11.0, 7.5); SUPPORT = (22.5, 7.0); MUZZLE = (38, 5.0)
