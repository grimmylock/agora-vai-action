from bench import G, show
PAL = {"0": "#2e3036", "1": "#4a4e56", "2": "#6a6e76", "3": "#9aa0a8", "4": "#d8dce0",
       "b": "#4a3618", "c": "#7a5a20", "d": "#a8842c", "e": "#d4b048", "f": "#f0d878",
       "g": "#2a2a2e", "h": "#4a4a50", "i": "#6e6e74", "j": "#9a9aa0", "K": "#141617"}
m = G(58, 17)
m.col(0, 6, "ij"); m.col(1, 6, "jih"); m.col(2, 6, "ih"); m.set(1, 7, "K")            # the skull nub
for x in (3, 4): m.col(x, 5, "fedddc" if x == 3 else "edddcb")                       # brass end ring
for x in range(5, 20):
    for y in range(5, 10):
        k = (x + y) % 3
        k = (x + y) % 4; m.set(x, y, ("jjih" if y < 7 else "ihgg" if y < 9 else "hggg")[k])
for x in (20, 21): m.col(x, 4, "fedddddb" if x == 20 else "edddcccb")               # guard ring
m.col(22, 1, "eddc"); m.set(23, 0, "f"); m.set(22, 0, ".")                         # the quillon curling up
# knuckle duster: brass bar under the grip, four round rings with see-through holes, bumps under each ring
RING = ["eeeee", "db_bd", "c___c", "c___c", "db_bd", "cdbdc", ".cbc."]
for x in range(3, 23):
    for y, r in enumerate(RING): m.set(x, 10 + y, r[(x - 3) % 5])
for x in range(23, 58):
    top = 4 if x <= 44 else 5 if x <= 48 else 6 if x <= 52 else 7 if x <= 56 else 8
    bot = 10 if x <= 44 else 9 if x <= 52 else 8
    for y in range(top, bot + 1):
        c = ("3" if top == 4 else "4") if y == top else "4" if y == bot else "0" if (y == 7 and 25 <= x <= 46) else "1" if y < 7 else "3"
        m.set(x, y, c)
    if x <= 43 and x % 2 == 0: m.set(x, 3, "2")
for x, y in [(30, 5), (34, 6), (38, 5), (41, 6), (27, 6)]: m.set(x, y, "2")             # mottled steel
for x, y in [(40, 8), (41, 8), (42, 9), (47, 8), (48, 8)]: m.set(x, y, "4")
h = G(17, 6)
h.col(0, 1, "ji")
for x in range(1, 6): h.col(x, 1, "jh" if x % 2 else "ig")
h.col(6, 0, "fddc")
for x in range(7, 17):
    if x <= 13: h.col(x, 1, "24")
    elif x <= 15: h.col(x, 1, "44")
    else: h.set(x, 2, "3")
for x in (8, 10, 12): h.set(x, 0, "2")
h.row(3, 1, "ddddd"); h.row(4, 1, "c_c_c"); h.row(5, 1, "bcbcb")
show("SerratedTrenchKnife", [m, h], PAL)
