# Vosa vz.62 (Skorpion pattern machine pistol), wire stock folded over the top with the U-shaped butt loop
# hanging round the muzzle; blued receiver, round cocking knobs, wooden grip with grooves, curved magazine
# ahead of the trigger guard.
from common import grid, fill, put
CAT = "smg"

# ---------------- A: 50x31 menu sprite
G = grid(50, 31)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# receiver
f(2, 3, 39, "2")
for y in range(3, 10): f(y, 2, 39, "3" if y < 6 else "4")
f(9, 2, 39, "5"); f(3, 2, 39, "3")
f(4, 13, 32, "4"); f(5, 13, 32, "4"); f(4, 14, 31, "5")                   # bolt slot
f(1, 34, 37, "3"); f(1, 7, 9, "3")                                        # front + rear sight ears
put(G, 3, 33, "121"); put(G, 4, 33, "202"); put(G, 5, 33, "121")          # cocking knob
put(G, 7, 20, "11"); put(G, 8, 20, "22")                                  # takedown pin
# barrel through the butt loop
f(4, 40, 49, "2"); f(5, 40, 49, "3"); f(6, 40, 49, "4"); f(7, 39, 41, "4"); G[5][49] = "5"
# lower frame + selector
f(10, 2, 36, "4"); f(11, 3, 35, "5"); put(G, 7, 6, "222"); put(G, 8, 7, "2")
# folded wire stock: top wire, rear hinge, U-shaped butt loop at the front (bright wire so it reads apart)
f(0, 2, 44, "2"); f(1, 1, 2, "2"); f(1, 44, 45, "2")
for y in range(2, 6): G[y][0] = "2"; G[y][1] = "4"
G[6][1] = "3"
for y in (2, 3, 7, 8, 9, 10, 11, 12, 13): G[y][45] = "2"; G[y][44] = "3" if y > 6 else G[y][44]
for y in range(2, 14): G[y][49] = "2"
f(1, 46, 48, "2"); G[2][48] = "3"
f(14, 45, 48, "3"); G[13][49] = "3"
# trigger guard + trigger
for y, row in {12: "4.......4", 13: "4.......4", 14: "4.......4", 15: "4......4.", 16: "4.....4..", 17: ".44444..."}.items():
    for i, ch in enumerate(row):
        if ch != ".": G[y][12 + i] = ch
put(G, 12, 15, "3"); put(G, 13, 15, "2"); put(G, 14, 15, "2"); put(G, 15, 16, "2")
# wooden grip with finger grooves
for y in range(12, 31):
    a = 1 if y < 26 else 0; b = 11 if y < 19 else 10 if y < 22 else 9
    f(y, a, b, "b"); G[y][a] = "a"; G[y][b] = "c"
    if y > 19:
        for x in (3, 6): G[y][x] = "c"
        G[y][4] = "a"
    if y == 18: f(y, a + 1, b, "c")
f(12, 1, 11, "a"); f(30, 0, 9, "d")
# curved magazine
for y in range(11, 31):
    a = 22 + (y - 11) * 5 // 19; w = 6
    f(y, a, a + w, "4"); G[y][a] = "3"; G[y][a + w] = "5"
    if y == 30: f(y, a, a + w, "5")
A = ["".join(r) for r in G]

# ---------------- B: 22x14 in hand
B = ["." + "2" * 17 + "....",
     "2" + "3" * 16 + "2" + "..." + "2",
     "2" + "3" * 14 + "1" + "4" + "2" + "2222",
     "." + "4" * 16 + "3" + "2" + ".." + "2",
     "." + "4" * 15 + ".." + "2" + ".." + "2",
     "aaaaa" + "5" * 11 + ".." + "2" + ".." + "2",
     "abcbc" + "4" + "2" + "." + "4" + "34444" + "...." + "." + "22" + ".",
     "abcbc" + "4444" + "34444" + "." * 8,
     "abcbc" + "...." + "." + "34444" + "." * 7,
     "abcbc" + "....." + "34444" + "." * 7,
     "abcbc" + "......" + "34444" + "." * 6,
     "abcbc" + "......" + "34444" + "." * 6,
     "abcbc" + "......." + "34444" + "." * 5,
     "ddddd" + "......." + "45555" + "." * 5]
GRIP = (2.5, 9.0); SUPPORT = None; MUZZLE = (22, 2.5)
