# PL-62 Kobza (Batch 113; RPK pattern - real guns are drawing references only): the VKM family's light machine gun.
# Built on the VKM: dark laminate "club foot" stock with the deep drop at the butt, the same receiver and plum grip, a longer
# heavy barrel with a plain muzzle, a folded bipod under it, and the 75-round drum where the VKM's curved mag would be.
from common import grid, fill, put
CAT = "heavy"

def disc(G, cx, cy, r, ch, edge=None, lit=None, dark=None):
    for y in range(len(G)):
        for x in range(len(G[0])):
            d2 = (x - cx) ** 2 + (y - cy) ** 2
            if d2 <= r * r:
                G[y][x] = ch
                if edge and d2 > (r - 1) ** 2: G[y][x] = dark if dark and (x > cx or y > cy) else lit if lit and (x < cx or y < cy) else edge

# ---------------- A: 62x19 menu sprite
G = grid(62, 19)
f = lambda y, a, b, c: fill(G, y, a, b, c)
# club-foot stock: comb straight, belly dropping deep at the butt
BOT = {0: 12, 1: 12, 2: 12, 3: 12, 4: 11, 5: 10, 6: 9, 7: 9, 8: 8, 9: 8, 10: 7, 11: 7, 12: 7, 13: 6, 14: 6, 15: 5}
for x, b in BOT.items():
    top = 2 if x >= 13 else 3
    for y in range(top, b + 1): G[y][x] = "c"
    G[top][x] = "b"; G[b][x] = "d"
for x in range(2, 11): G[5][x] = "d"
f(4, 1, 12, "c")
for y in range(3, 13): G[y][0] = "4"
G[3][0] = "3"; G[12][0] = "5"
# receiver (as the VKM): lit cover, rear sight, port, selector, bolt handle
f(2, 15, 16, "4"); G[1][16] = "3"
f(1, 17, 27, "2"); f(1, 18, 23, "1")
f(2, 17, 27, "3"); f(2, 28, 33, "3"); f(1, 28, 31, "2"); put(G, 0, 29, "23")
for y in (3, 4): f(y, 15, 33, "4")
f(3, 22, 27, "5"); put(G, 4, 19, "2222221"); put(G, 3, 28, "11")
f(5, 15, 33, "5"); G[5][15] = "4"; G[3][17] = G[3][19] = "2"
# plum pistol grip, trigger + guard
GRIP_ = {6: (16, 20), 7: (16, 19), 8: (15, 19), 9: (15, 19), 10: (15, 18), 11: (15, 18)}
for y, (a, b) in GRIP_.items():
    f(y, a, b, "c"); G[y][a] = "b"; G[y][b] = "d"
f(11, 15, 18, "d"); G[11][15] = "c"
put(G, 6, 21, "3"); put(G, 6, 24, "2"); f(7, 20, 24, "2"); G[7][24] = "3"
# the 75-round drum: a round steel drum, lit top-left, a winding key in the middle
disc(G, 29.5, 11.5, 6.5, "4", edge="3", lit="2", dark="5")
disc(G, 29.5, 11.5, 2.2, "3"); G[11][29] = "1"; G[12][30] = "5"
f(6, 27, 32, "5")                                          # the feed lip into the receiver
# handguard with the finger swell
f(1, 34, 40, "b"); f(2, 34, 40, "d")
f(3, 33, 41, "b"); f(4, 33, 41, "c"); f(5, 33, 41, "d")
# gas block, heavy barrel, folded bipod, front sight, plain muzzle
for y in range(1, 5): f(y, 41, 42, "3")
f(1, 41, 42, "2"); f(4, 41, 42, "4")
f(2, 43, 59, "2"); f(3, 43, 59, "4"); f(4, 43, 52, "3")
f(5, 44, 55, "4"); f(6, 44, 55, "5"); G[5][56] = "3"; G[5][43] = G[6][43] = "3"   # the bipod legs folded along the barrel
put(G, 0, 55, "3.3"); f(1, 55, 57, "3"); G[1][56] = "2"
put(G, 2, 60, "22"); put(G, 3, 60, "45")
A = ["".join(r) for r in G]

# ---------------- B: 44x14 in hand (hand-drawn at half size)
TOP = [
    ".......................23............3......",
    "..........bb3111111222333bbbbb22.....33.....",
    "4bbbbbbbbbcc333355553133dddddd332222222222222",
    "4cccccccccccd44222214444bbbbbb444444444444445",
    "4cdddddddccd555555555555ccccc4444444444445...",
]
H = [list(r[:44].ljust(44, ".")) for r in TOP] + [["."] * 44 for _ in range(9)]
for y, row in ((5, "4ccccccccccd"), (6, "4cccccccdd"), (7, "4cccccdd"), (8, "4ddddd")): put(H, y, 0, row)   # the club foot
for y, (a, b) in {5: (13, 15), 6: (13, 15), 7: (13, 15), 8: (13, 14)}.items():                       # plum grip
    fill(H, y, a, b, "c"); H[y][a] = "b"; H[y][b] = "d"
fill(H, 9, 13, 14, "d"); put(H, 5, 16, "3"); put(H, 6, 16, "2")                                          # trigger + guard
disc(H, 21.0, 8.5, 3.6, "4", edge="3", lit="2", dark="5"); H[8][21] = "1"; H[9][22] = "5"              # the drum
fill(H, 5, 19, 23, "5")                                                                                  # its feed lip
fill(H, 5, 32, 39, "5"); H[5][31] = "3"                                                                  # folded bipod
B = ["".join(r) for r in H]
GRIP = (14.0, 6.5)
SUPPORT = (30.0, 4.5)
MUZZLE = (44, 2.5)
