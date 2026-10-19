# BR-86 Bulldog (L85A2-pattern bullpup): black steel receiver running the full length, tan cheek piece,
# handguard and pistol grip, SUSAT-style optic on a mount, mag behind the grip, long trigger-guard loop,
# barrel with flash hider.
from common import grid, fill, put
CAT = "assault"

# ---------------- A: 58x22 menu sprite
G = grid(58, 22)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# receiver (black steel): full length, ejection port, charging slot, lower butt + buttplate
for y in range(6, 12): f(y, 0, 31, "4")
f(6, 0, 31, "3"); f(11, 1, 31, "5"); f(10, 1, 31, "4")
for y in (12, 13): f(y, 1, 10, "4")
f(13, 1, 10, "5"); G[12][1] = "3"
for y in range(6, 14): G[y][0] = "5"
f(9, 13, 18, "5"); put(G, 9, 19, "1"); f(8, 21, 29, "5"); f(7, 21, 29, "3")
# tan cheek piece
for y in (6, 7, 8): f(y, 1, 12, "b")
f(6, 1, 12, "a"); f(8, 1, 11, "c"); G[6][1] = "b"; G[7][8] = "a"
# SUSAT-style optic: eyepiece, body, objective bell, lens, mount
put(G, 0, 19, "2222233"); f(1, 16, 26, "3"); f(2, 16, 27, "4"); f(3, 16, 27, "4"); f(4, 17, 26, "5")
f(1, 17, 24, "2"); G[1][16] = "4"; f(0, 24, 27, "3"); G[0][24] = "2"; f(1, 25, 27, "3")
put(G, 2, 27, "l"); put(G, 3, 27, "L"); put(G, 1, 27, "k")
for x in range(17, 19): G[2][x] = "5"; G[3][x] = "5"
put(G, 5, 19, "55"); put(G, 5, 24, "55")
# mag behind the grip, slight forward cant, ribs
for y in range(12, 22):
    a = 12 + (y - 12) // 5; b = a + 5
    f(y, a, b, "4"); G[y][a] = "3"; G[y][b] = "5"
    if y in (14, 16, 18): f(y, a + 2, b - 1, "5")
f(21, 13, 18, "5")
# tan pistol grip, raked
GR = {12: (23, 26), 13: (22, 26), 14: (22, 25), 15: (21, 25), 16: (21, 25), 17: (21, 24), 18: (21, 24)}
for y, (a, b) in GR.items(): f(y, a, b, "b"); G[y][a] = "a"; G[y][b] = "c"
f(18, 21, 24, "c"); G[14][24] = "c"
# long trigger-guard loop (hole open) + trigger
put(G, 12, 29, "4"); G[12][31] = "3"; G[12][32] = "3"; f(13, 27, 31, "3"); G[11][32] = "4"
f(12, 27, 28, "."); f(12, 30, 30, ".")
# tan handguard: lit top, vent slots, rounded nose, front sight post
for y in range(6, 12): f(y, 32, 47, "b")
f(6, 33, 46, "a"); f(11, 33, 46, "c"); f(10, 32, 47, "b"); G[11][32] = "."; G[11][47] = "."; G[6][47] = "."
for x in (34, 38, 42): f(8, x, x + 2, "c")
f(5, 42, 43, "4"); G[4][43] = "4"
# barrel + flash hider
f(8, 48, 52, "2"); f(9, 48, 52, "4")
for y in range(7, 11): f(y, 53, 57, "3")
f(7, 53, 57, "2"); f(10, 53, 57, "4"); G[8][55] = "5"; G[9][55] = "5"; G[8][57] = "5"; G[9][57] = "5"
A = ["".join(r) for r in G]

# ---------------- B: 35x14 in hand
B = ["." * 11 + "33333" + "." * 19,
     "." * 10 + "444444" + "l" + "." * 18,
     "." * 10 + "455554" + "L" + "." * 18,
     "." * 11 + "5.5" + "." * 12 + "3" + "." * 8,
     "4" + "aaaaaaa" + "33333333333" + "aaaaaaaaaa" + "......",
     "4" + "bbbbbbb" + "4" + "4555514444" + "bbcbbcbbcb" + "22" + "3333",
     "4" + "4444444" + "4" + "4444444444" + "bbbbbbbbbb" + "44" + "3553",
     "4" + "5555555" + "5" + "5555555555" + "cccccccccc" + "......",
     "45" + "....." + "3444" + ".." + "abb" + ".4.3" + "." * 15,
     "." * 7 + "3444" + ".." + "abc" + "3333" + "." * 15,
     "." * 7 + "3454" + ".." + "abc" + "." * 19,
     "." * 7 + "3444" + "." + "abc" + "." * 20,
     "." * 8 + "3454" + "ccc" + "." * 20,
     "." * 8 + "5555" + "." * 23]
GRIP = (14.0, 10.0); SUPPORT = (24.0, 7.0); MUZZLE = (35, 5.5)
