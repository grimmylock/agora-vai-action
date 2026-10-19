# Steppe machete (Gerber-style): black rubber handle with finger guard + lanyard hook, black blade,
# saw-back spine, bright bevel sweeping up to the clipped tip.
from common import grid, fill, put
CAT = "melee"

# ---------------- A: 62x8 menu sprite
G = grid(62, 8)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# handle
f(1, 2, 16, "p"); f(2, 1, 16, "p"); f(3, 0, 16, "q"); f(4, 0, 16, "q"); f(5, 1, 16, "r")
G[2][0] = "p"; G[1][1] = "."; G[2][1] = "p"; G[5][0] = "r"
for x in (5, 9, 13): G[2][x] = "r"; G[3][x] = "r"                        # grip panel grooves
for x in (6, 10): G[3][x] = "p"
G[3][2] = "5"                                                            # lanyard hole
f(6, 1, 3, "r"); G[7][2] = "r"; G[7][3] = "q"                             # lanyard hook
# finger guard (up and down) + ferrule
f(0, 14, 17, "p"); f(1, 17, 18, "q"); f(5, 17, 20, "r"); f(6, 15, 17, "r"); f(7, 16, 17, "r"); G[6][15] = "q"
for y in range(1, 5): f(y, 17, 20, "q")
f(1, 17, 20, "p")
# blade
BOT = {x: 5 for x in range(21, 55)}; BOT.update({55: 5, 56: 4, 57: 4, 58: 3, 59: 3, 60: 2, 61: 1})
for x in range(21, 62):
    b = BOT[x]
    for y in range(1, b + 1): G[y][x] = "5"
    G[1][x] = "3"; G[b][x] = "s"
    if b >= 3: G[b - 1][x] = "t" if x > 26 else "5"
    if b >= 4: G[2][x] = "4"
G[1][61] = "s"; G[2][60] = "s"
for x in range(27, 55, 2): G[0][x] = "3"                                  # saw teeth
A = ["".join(r) for r in G]

# ---------------- B: 23x5 in hand
B = [".....q....3.3.3.3.3.3..",
     "pppppqq333333333333333s",
     "qrqqqqq555555555555tss.",
     "qqqqrqq4ttttttttttss...",
     ".r...rrssssssssssss....",]
GRIP = (3.0, 2.5); SUPPORT = None; MUZZLE = (23, 1.5)
