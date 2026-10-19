# Serrated trench knife (Mk1 pattern): cast knuckle-duster grip with four finger loops and spiked knuckles,
# down-swept quillon, double-edged dagger blade with a saw-cut spine.
from common import grid, fill, put
CAT = "melee"

# ---------------- A: 52x17 menu sprite
G = grid(52, 17)
f = lambda y, a, b, c: fill(G, y, a, b, c)
G[0][7] = G[0][13] = G[0][20] = "3"                                     # knuckle spikes
f(1, 6, 9, "3"); f(1, 11, 14, "3"); f(1, 19, 20, "3")                      # raised middle knuckles
for y in range(2, 13): f(y, 0 if y > 2 else 1, 21, "3")
G[12][0] = "."
for y in range(13, 16): f(y, 20, 21, "3")                                 # quillon
G[16][21] = "3"; G[16][22] = "3"
for x0, top in ((1, 3), (6, 2), (11, 2), (16, 3)):                        # finger loops, rounded
    for y in range(top, 9): f(y, x0, x0 + 3, ".")
    for (dx, dy) in ((0, top), (3, top), (0, 8), (3, 8)): G[dy][x0 + dx] = "3"
# auto light: top-facing pixels lit, bottom-facing shaded
S = [r[:] for r in G]
for y in range(17):
    for x in range(52):
        if S[y][x] != "3": continue
        up = y == 0 or S[y - 1][x] == "."; dn = y == 16 or S[y + 1][x] == "."
        G[y][x] = "2" if up else "4" if dn else "3"
for x in range(1, 20): G[12][x] = "4"
G[9][21] = "4"; G[10][21] = "4"
# blade
TOP = {x: 8 if x < 48 else 9 for x in range(22, 52)}
BOT = {x: 11 if x < 45 else 10 if x < 51 else 9 for x in range(22, 52)}
for x in range(22, 52):
    t, b = TOP[x], BOT[x]
    for y in range(t, b + 1): G[y][x] = "S"
    G[t][x] = "s"
    if b > t: G[b][x] = "t"
f(9, 22, 46, "s")                                                         # centre ridge catches light
for x in range(23, 36, 2): G[7][x] = "S"                                  # saw-cut spine
A = ["".join(r) for r in G]

# ---------------- B: 17x8 in hand
B = ["222222222........",
     "3.3.3.3.3........",
     "3.3.3.3.3.S.S....",
     "222222223sssssss.",
     "333433434SSSSSSSs",
     "44444444.tttttt..",
     "........4........",
     "........4........",]
GRIP = (4.5, 4.0); SUPPORT = None; MUZZLE = (17, 4.5)
