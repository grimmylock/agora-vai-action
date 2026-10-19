# Bayou Pump (590-pattern pump shotgun): hand-pixelled 56x11 over trace_BayouPump.png.
# Matte black polymer stock + ribbed pump, anodised receiver with bright bolt in the port,
# barrel over magazine tube, barrel clamp, bead sight.
from common import *
G = grid(56, 11)
f = lambda y, a, b, c: fill(G, y, a, b, c)

# --- stock: lit comb, body, dark belly, rubber buttpad, sling swivel
TOP = {x: 3 if x < 12 else 2 if x < 15 else 1 for x in range(17)}
BOT = {x: 10 if x < 4 else 9 if x < 7 else 8 if x < 11 else 7 if x < 15 else 6 for x in range(17)}
for x in range(17):
    for y in range(TOP[x], BOT[x] + 1): G[y][x] = "4"
    G[TOP[x]][x] = "3"; G[BOT[x]][x] = "5"
for y in range(3, 11): G[y][0] = "5"
G[3][0] = "4"; G[10][5] = "3"

# --- receiver + trigger group
f(0, 17, 27, "2"); f(1, 17, 27, "3"); f(2, 17, 27, "3"); f(3, 17, 27, "4")
put(G, 1, 22, "51115")                              # ejection port, bolt showing
G[0][17] = "."; G[1][17] = "4"
f(4, 17, 18, "5")                                   # stock/trigger-group joint
G[4][19] = G[4][22] = "3"; G[4][20] = "2"; f(5, 19, 22, "3"); G[5][20] = "2"   # guard + trigger

# --- barrel over magazine tube
f(0, 28, 55, "2"); f(1, 28, 55, "4")
f(2, 28, 53, "3"); f(3, 28, 53, "5"); put(G, 2, 54, "2"); put(G, 3, 54, "4")   # tube + end cap

# --- pump: ribbed polymer, lit top
for y in range(1, 5): f(y, 34, 45, "4")
f(1, 34, 45, "3")
for x in range(35, 45, 2):
    for y in (2, 3): G[y][x] = "5"
f(4, 35, 44, "5")

# --- barrel clamp, swivel, bead
for y in range(0, 4): G[y][49] = "3"
G[0][49] = "2"; G[4][49] = "3"; G[0][55] = "0"

B = ["......." + "3" + "222112" + "222" + "333333" + "2" + "3" + "22" + "1",
     "5" + "33333" + "44" + "333333" + "333" + "454545" + "3" + "4" + "3" + "2" + ".",
     "5" + "4444444" + "5" + "323" + "....." + "555555" + ".....",
     "5" + "4444" + "555" + "." * 20,
     "54555" + "." * 23,
     "55" + "." * 26]
A, B = build_ab(G, B)
