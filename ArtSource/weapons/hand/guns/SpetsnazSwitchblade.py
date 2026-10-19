# Spetsnaz switchblade (stiletto pattern): brass bolsters + cross guard, wood scales with spring slot, needle blade.
from common import grid, fill, put
CAT = "melee"

# ---------------- A: 56x8 menu sprite
G = grid(56, 8)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# rear brass bolster
f(2, 1, 4, "y"); f(3, 0, 4, "Y"); f(4, 0, 4, "Y"); f(5, 0, 4, "Y"); f(6, 1, 4, "Y"); G[3][1] = G[3][2] = "y"
G[4][3] = "c"
# wood scale with brass liner and the spring slot
f(2, 5, 23, "b"); f(3, 5, 23, "c"); f(4, 5, 23, "c"); f(5, 5, 23, "Y"); f(6, 5, 23, "d")
for (x, y) in ((7, 3), (8, 4), (11, 4), (12, 3), (21, 4), (22, 3)): G[y][x] = "d"
for (x, y) in ((9, 3), (10, 3), (6, 4)): G[y][x] = "b"
f(3, 13, 19, "4"); f(4, 13, 19, "5"); f(3, 14, 18, "2")                   # slot: blade spine showing
G[3][9] = "1"                                                            # pin
# cross guard (quillons up/down) + front bolster + release button
for y in range(0, 8): f(y, 24, 25, "Y")
f(0, 24, 27, "y"); G[0][24] = "Y"; G[1][27] = "Y"                         # top quillon curls into a ring
for y in range(2, 7): f(y, 26, 29, "Y")
f(2, 26, 29, "y"); G[6][29] = "."; G[4][24] = "y"
G[2][20] = G[2][21] = "4"; G[1][20] = G[1][21] = "Y"                     # release lever
# blade
TOP = {x: 2 if x < 45 else 3 if x < 52 else 4 for x in range(30, 56)}
BOT = {x: 5 if x < 48 else 4 for x in range(30, 56)}
for x in range(30, 56):
    t, b = TOP[x], BOT[x]
    for y in range(t, b + 1): G[y][x] = "S"
    G[t][x] = "s"
    if b > t: G[b][x] = "t"
f(3, 30, 44, "s")                                                         # flat
f(4, 30, 31, "t")                                                         # ricasso shadow
A = ["".join(r) for r in G]

# ---------------- B: 16x5 in hand
B = ["......Y.........",
     "yybbbbyYssssss..",
     "YcdccdYYSSSSSSSs",
     "YYYYYYYYtttt....",
     "......Y.........",]
GRIP = (3.0, 2.5); SUPPORT = None; MUZZLE = (16, 2.5)
