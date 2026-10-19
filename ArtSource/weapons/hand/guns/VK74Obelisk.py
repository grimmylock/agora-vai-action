# VK-74 Obelisk: the pilot VK-74 (AK-74) refitted by the Obelisk faction: grey polymer stock and
# handguards, black polymer grip and mag, small red-dot sight on a side-rail mount over the receiver
# (dark body, red lens), laser module under the handguard (red emitter at its front).
# Geometry copied from the pilot (vk74.py A, b_v2.VK74 B), drawn in wood letters, then mapped to polymer.
from common import grid, fill, put
CAT = "assault"

# ---------------- A: 56x19 menu sprite (pilot art shifted down 3 rows for the sight)
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

GREY = str.maketrans("abcd", "ppqr"); BLACK = str.maketrans("abcd", "pqqr")
rows = []
for y, r in enumerate(G):
    s = "".join(r)
    rows.append("".join(ch.translate(BLACK if (6 <= y and 15 <= x <= 34) else GREY) for x, ch in enumerate(s)))
G = [list("." * 56) for _ in range(3)] + [list(r) for r in rows]
# red dot on the side-rail mount: mount legs, dark tube body, red lens
put(G, 3, 19, "5..5"); put(G, 0, 18, "3333"); put(G, 1, 17, "45554"); put(G, 2, 17, "45555"); G[1][22] = "R"; G[2][22] = "4"
put(G, 0, 22, "3"); G[0][17] = "."
# laser module under the handguard, red emitter at the front
put(G, 9, 35, "44444"); put(G, 10, 35, "55555"); G[9][40] = "R"; G[10][40] = "5"
A = ["".join(r) for r in G]

# ---------------- B: 40x13 in hand (pilot b_v2.VK74 + sight on top, laser below)
B0 = ["....................." + "23" + "." * 12 + "3" + "....",
      "........." + "aa" + "3" + "111111" + "222" + "333" + "aaaaa" + "22" + "...." + "23" + "...",
      "4" + "aaaaaaaa" + "bb" + "3333" + "5555" + "3" + "1" + "333" + "ccccc" + "33" + "2222" + "33" + "221",
      "4" + "bbbbbbbbb" + "c" + "44" + "22221" + "444444" + "bbbbb" + "44" + "4444" + "44" + "434",
      "4" + "b" + "cccccc" + "bb" + "c" + "5" * 13 + "ccccc" + "." * 11,
      "4" + "bbbbbbb" + "dd" + "." + "bccd" + "3" + "." + "2" + "abbbc" + "." * 17,
      "4" + "ddddd" + "...." + "." + "bccd" + "222" + "abbbc" + "." * 17,
      "." * 10 + "bccd" + "....." + "abbbc" + "." * 16,
      "." * 10 + "cddd" + "....." + "abbbc" + "." * 16,
      "." * 20 + "abbc" + "." * 16,
      "." * 21 + "ddd" + "." * 16]
B = ["." * 13 + "3333" + "." * 23,
     "." * 12 + "4555R" + "." * 23]
for y, s in enumerate(B0):
    B.append("".join(ch.translate(BLACK if (y >= 4 and 10 <= x <= 23) else GREY) for x, ch in enumerate(s)))
B[2] = B[2][:13] + "5..5" + B[2][17:]
B[2] = B[2][:13] + "5" + B[2][14:16] + "5" + B[2][17:]
B[7] = B[7][:25] + "4444R" + B[7][30:]
GRIP = (12.5, 8.5); SUPPORT = (26.0, 7.0); MUZZLE = (40, 4.5)
