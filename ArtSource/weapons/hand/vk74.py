# VK-74 (AK-74 pattern): hand-pixelled 56x16 over trace_VK74.png. Orange laminate furniture,
# plum grip, orange bakelite mag, dark blued steel.
from common import *
G = grid(56, 16)
f = lambda y, a, b, c: fill(G, y, a, b, c)

# --- stock: buttplate, lit comb, lightening groove, sloping belly up to the wrist
BOT = {0: 9, 1: 9, 2: 9, 3: 9, 4: 9, 5: 8, 6: 8, 7: 8, 8: 8, 9: 7, 10: 7, 11: 7, 12: 7, 13: 6, 14: 6, 15: 5}
for x, b in BOT.items():
    top = 2 if x >= 13 else 3
    for y in range(top, b + 1): G[y][x] = "b"
    G[top][x] = "a"; G[b][x] = "d"; G[b - 1][x] = "c" if b - 1 > top else G[b - 1][x]
for x in range(2, 11): G[5][x] = "c"; G[6][x] = "a" if x < 10 else "b"     # groove: shadow over lit lip
for y in range(3, 10): G[y][0] = "4"
G[3][0] = "3"; G[9][0] = "5"

# --- receiver: lit top cover, rear sight block, ejection port, selector lever, bottom rail
f(2, 15, 16, "4"); G[1][16] = "3"
f(1, 17, 27, "2"); f(1, 18, 23, "1")               # top cover, rounded highlight
f(2, 17, 27, "3"); f(2, 28, 33, "3"); f(1, 28, 31, "2"); put(G, 0, 29, "23")   # rear sight leaf
for y in (3, 4): f(y, 15, 33, "4")
f(3, 22, 27, "5")                                   # ejection port
put(G, 4, 19, "2222221")                            # selector lever
put(G, 3, 28, "11")                                 # bolt handle knob
f(5, 15, 33, "5"); G[5][15] = "4"
G[3][17] = G[3][19] = "2"; G[5][24] = "3"           # rivets, mag catch

# --- pistol grip (plum), lit back edge, finger texture
GRIP = {6: (16, 20), 7: (16, 19), 8: (15, 19), 9: (15, 19), 10: (15, 18), 11: (15, 18)}
for y, (a, b) in GRIP.items():
    f(y, a, b, "c"); G[y][a] = "b"; G[y][b] = "d"
    if y in (8, 10): G[y][a + 2] = "d"
f(11, 15, 18, "d"); G[11][15] = "c"

# --- trigger + guard
put(G, 6, 21, "3"); put(G, 6, 24, "2"); f(7, 20, 24, "2"); G[7][24] = "3"

# --- magazine: curved bakelite, lit spine, darker front, floorplate; back edge curves more than front
MAG = {6: (25, 31), 7: (25, 31), 8: (26, 31), 9: (26, 32), 10: (27, 32), 11: (27, 32), 12: (28, 33), 13: (28, 33), 14: (29, 33)}
for y, (a, b) in MAG.items():
    f(y, a, b, "b"); G[y][a] = "a"; G[y][b] = "c"
f(6, 26, 30, "c")                                   # shadow where it enters the well
G[8][27] = "a"; G[9][27] = "a"                      # rear reinforcing rib
f(15, 29, 33, "d")

# --- handguards: upper gas-tube cover + lower guard, retaining rings
f(1, 34, 40, "a"); f(2, 34, 40, "c")               # upper guard: lit top, shaded underside
f(3, 33, 41, "a"); f(4, 33, 41, "b"); f(5, 33, 41, "c")
for y in range(1, 6): G[y][33] = "4" if y > 2 else G[y][33]
G[2][33] = "3"

# --- gas block, barrel, cleaning rod, front sight, muzzle brake
for y in range(1, 5): f(y, 41, 42, "3")
f(1, 41, 42, "2"); f(4, 41, 42, "4")
f(2, 43, 48, "2"); f(3, 43, 48, "4"); f(4, 43, 50, "3")   # barrel over cleaning rod
for y in range(1, 5): f(y, 49, 51, "3")
f(1, 49, 51, "2"); put(G, 0, 49, "3 3"); G[1][50] = "1"
put(G, 2, 52, "2221"); put(G, 3, 52, "3533"); put(G, 4, 52, "4444")

# B 28x8 drawn by hand (each px = 2x2 of A)
B = ["........11111122.aaaa2..23..",
     "4aaaaaab33355544" + "4bbbb3" + "223321",
     "4bbbbbbc42222444" + "4cccc4" + "444443",
     "4ccddd..bc32abc" + "." * 13,
     "5dd....bcd...abc" + "." * 12,
     ".......bd....abc" + "." * 12,
     "." * 14 + "abc" + "." * 11,
     "." * 14 + "cdd" + "." * 11]
A, B = build_ab(G, B)
