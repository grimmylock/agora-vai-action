# Nightwalker .45-70 (Marlin 1895 guide-gun pattern). The reference photo is tilted, so this is a clean
# level profile: pistol-grip walnut stock with black recoil pad and grip cap, blued solid-top receiver,
# big finger-loop lever, chunky forend with steel cap and sling swivel, heavy short barrel over the tube
# magazine, ghost-ring rear and hooded front sight.
from common import grid, fill, put
CAT = "sniper"

def wood(G, x, t, b):
    for y in range(t, b + 1): G[y][x] = "c"
    G[t][x] = "b"; G[b][x] = "d"

# ---------------- A: 56x12 menu sprite
G = grid(56, 12)
f = lambda y, a, b, c: fill(G, y, a, b, c)
BOT = {1: 10, 2: 10, 3: 10, 4: 9, 5: 9, 6: 9, 7: 8, 8: 8, 9: 8, 10: 7, 11: 7, 12: 7, 13: 8, 14: 9, 15: 9, 16: 8, 17: 7}
for x, b in BOT.items(): wood(G, x, 3, b)
for y in range(3, 11): G[y][0] = "r"
G[3][0] = "q"
f(9, 14, 15, "4"); G[8][16] = "d"                          # grip cap
f(5, 2, 9, "d"); f(6, 2, 8, "b")                            # grain line
# receiver + hammer
f(2, 19, 28, "2"); f(2, 21, 25, "1"); G[3][18] = "3"
for y in range(3, 7): f(y, 18, 28, "4")
f(3, 19, 28, "3"); f(7, 18, 28, "5"); f(6, 18, 28, "5")
put(G, 5, 27, "2"); put(G, 4, 20, "2")                    # lever pivot + hammer screw
put(G, 1, 17, "4"); put(G, 0, 16, "3"); put(G, 2, 17, "4")
# big finger-loop lever + trigger
f(8, 17, 27, "3"); f(8, 18, 22, "4")
for y in (9, 10): G[y][17] = "3"; G[y][23] = "3"
f(11, 18, 22, "3"); put(G, 9, 21, "2")
# forend, cap, sling swivel
for x in range(29, 40): wood(G, x, 4, 7)
for y in range(2, 8): G[y][40] = "3"
G[2][40] = "2"; G[7][40] = "4"; put(G, 8, 40, "4"); put(G, 9, 40, "3")
# heavy barrel over the tube magazine, sights
f(2, 29, 55, "2"); f(3, 29, 55, "4"); f(2, 29, 34, "1")
f(5, 41, 53, "3"); f(6, 41, 53, "4"); put(G, 5, 54, "2"); put(G, 6, 54, "3")
for y in range(2, 7): G[y][50] = "3"
G[6][50] = "4"; G[4][50] = "3"
put(G, 1, 30, "4"); put(G, 0, 30, "3"); put(G, 1, 53, "33"); put(G, 0, 54, "3")
A = ["".join(r) for r in G]

# ---------------- B: 38x8 in hand
H = grid(38, 8)
h = lambda y, a, b, c: fill(H, y, a, b, c)
BB = {1: 6, 2: 6, 3: 5, 4: 5, 5: 5, 6: 5, 7: 4, 8: 4, 9: 5, 10: 6, 11: 5}
for x, b in BB.items(): wood(H, x, 2, b)
for y in range(2, 7): H[y][0] = "r"
H[6][10] = "4"
h(1, 13, 18, "2"); h(1, 14, 16, "1"); h(2, 12, 18, "3"); h(3, 12, 18, "4"); h(4, 12, 18, "5")
put(H, 1, 12, "4"); put(H, 0, 11, "3")
h(5, 12, 18, "3"); put(H, 6, 12, "3"); put(H, 6, 14, "2"); put(H, 6, 16, "3"); h(7, 13, 15, "3")  # lever loop
for x in range(19, 27): wood(H, x, 2, 4)
h(1, 19, 37, "2"); h(2, 27, 37, "4")
for y in range(1, 5): H[y][27] = "3"
H[5][27] = "4"
h(4, 28, 35, "3"); put(H, 4, 36, "2"); H[3][33] = "3"
H[0][20] = "4"; H[0][37] = "3"
B = ["".join(r) for r in H]
GRIP = (10.0, 4.5); SUPPORT = (23.0, 5.5); MUZZLE = (38, 2.0)
