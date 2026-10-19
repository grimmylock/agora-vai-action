# PL-74 (PKM pattern GPMG): red-brown laminate skeleton stock with the long slot, laminate pistol grip,
# black stamped receiver with feed cover and rear sight, 100-rd OD ammo box under the feed, wooden barrel
# carry handle, gas cylinder under the barrel, folded bipod, front sight and slotted flash hider.
from common import grid, fill, put
CAT = "heavy"

def wood(G, x, t, b):
    for y in range(t, b + 1): G[y][x] = "c"
    G[t][x] = "b"; G[b][x] = "d"

def draw_a(G):
    f = lambda y, a, b, c: fill(G, y, a, b, c)
    BOT = {1: 11, 2: 11, 3: 11, 4: 11, 5: 11, 6: 11, 7: 11, 8: 11, 9: 10, 10: 9, 11: 8, 12: 7, 13: 7}
    for x, b in BOT.items(): wood(G, x, 5, b)
    for y in range(5, 12): G[y][0] = "4"
    G[5][0] = "3"
    f(7, 4, 9, "."); f(8, 3, 10, "."); f(9, 4, 9, "."); f(6, 3, 10, "d")     # stock slot
    # receiver
    f(3, 14, 31, "2"); f(3, 16, 22, "1"); f(4, 14, 31, "4"); f(5, 14, 31, "4"); f(6, 14, 31, "4"); f(7, 14, 31, "5")
    f(4, 16, 28, "3"); f(5, 16, 28, "5"); put(G, 2, 22, "33"); put(G, 6, 29, "1"); G[3][14] = "3"
    # pistol grip, trigger guard
    GR = {8: (15, 18), 9: (15, 18), 10: (14, 17), 11: (14, 17), 12: (13, 16)}
    for y, (a, b) in GR.items(): f(y, a, b, "c"); G[y][a] = "b"; G[y][b] = "d"
    f(13, 13, 15, "d"); G[10][16] = "d"
    put(G, 8, 20, "2"); G[8][22] = "4"; f(9, 19, 22, "4")
    # ammo box
    for y in range(8, 14): f(y, 23, 29, "O"); G[y][23] = "o"; G[y][29] = "e"
    f(8, 23, 29, "o"); f(13, 23, 29, "e"); f(10, 24, 28, "e"); G[8][29] = "O"
    # barrel, gas cylinder, gas block, carry handle, front sight, hider, bipod
    f(4, 32, 55, "2"); f(5, 32, 55, "4")
    f(7, 32, 45, "3"); f(8, 32, 45, "5"); f(6, 32, 33, "4")       # gas cylinder, gap under the barrel
    for y in range(4, 9): f(y, 45, 47, "3")
    f(4, 45, 47, "2"); f(8, 45, 47, "5")
    put(G, 3, 32, "4"); put(G, 2, 31, "44"); put(G, 1, 29, "ab"); put(G, 0, 27, "abb"); G[1][31] = "4"
    put(G, 3, 54, "33"); put(G, 2, 54, "3"); put(G, 1, 54, "33"); put(G, 0, 54, "3")
    put(G, 4, 56, "2121"); put(G, 5, 56, "4545")
    f(9, 35, 46, "4"); put(G, 9, 47, "3"); G[9][34] = "5"

def draw_b(H):
    h = lambda y, a, b, c: fill(H, y, a, b, c)
    BB = {1: 8, 2: 8, 3: 8, 4: 8, 5: 8, 6: 7, 7: 6, 8: 5, 9: 5}
    for x, b in BB.items(): wood(H, x, 3, b)
    for y in range(3, 9): H[y][0] = "4"
    h(5, 2, 5, "."); h(6, 3, 5, "."); h(4, 2, 6, "d")
    h(2, 10, 21, "2"); h(2, 11, 14, "1"); h(3, 10, 21, "4"); h(4, 10, 21, "4"); h(5, 10, 21, "5")
    h(3, 12, 18, "5"); put(H, 1, 15, "3")
    for y, (a, b) in {6: (11, 13), 7: (11, 13), 8: (10, 12), 9: (10, 12)}.items(): h(y, a, b, "c"); H[y][a] = "b"; H[y][b] = "d"
    h(9, 10, 12, "d")
    put(H, 6, 14, "2"); h(7, 13, 15, "4")
    for y in range(6, 10): h(y, 16, 20, "O"); H[y][16] = "o"; H[y][20] = "e"
    h(6, 16, 20, "o"); h(9, 16, 20, "e")
    h(3, 22, 39, "2"); h(4, 22, 39, "4"); h(6, 22, 31, "3"); H[5][22] = "4"
    for y in range(3, 7): h(y, 31, 32, "3")
    put(H, 2, 22, "4"); put(H, 1, 21, "4"); put(H, 0, 19, "ab")
    put(H, 2, 38, "3"); put(H, 1, 38, "3"); put(H, 3, 40, "212"); put(H, 4, 40, "454")
    h(7, 23, 31, "4")

G = grid(60, 14); draw_a(G); A = ["".join(r) for r in G]
H = grid(43, 10); draw_b(H); B = ["".join(r) for r in H]
GRIP = (11.5, 7.5); SUPPORT = (26.0, 7.5); MUZZLE = (43, 4.0)
