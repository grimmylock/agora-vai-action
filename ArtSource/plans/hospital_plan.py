# Batch 92: the Siege Ward Hospital, Garrison's base on the Tier 6 map (the Batch 87 raid), as a plan stamped at (68,8), 46x18.
# Was a solid block the carver cut into a tidy room grid. Now: a shelled hospital held as a fort - the main doors south (the
# sandbagged lobby), a side door in each wing, a long ward corridor with wards both sides, the operating theatre, the armory
# in the north-west corner behind two doors (the raid's prize, landmark at map (76,14) = plan (8,6)), the east wing caved in
# (rubble you can climb through: the flanking way in), windows on the outer walls for the long guns on both sides.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from foundry_plans import Plan

def hospital():
    p = Plan(46, 18, 87); r = p.r
    p.hline(1, 44, 9, 2)                                                      # the ward corridor, kinked at the theatre
    p.hline(20, 27, 11)
    p.jag(2, 2, 11, 6, bite=0.3); p.put(8, 6, 'A'); p.put(10, 8, 'D')         # the armory, north-west
    p.put(6, 8, '#'); p.room(3, 3, 1, 1, 'l'); p.scatter(2, 2, 11, 6, 'lllcc', 7)
    p.jag(14, 2, 7, 6); p.put(17, 8, 'D'); p.scatter(14, 2, 7, 6, 'dl"', 4)  # the quartermaster's office, door to the armory
    p.put(13, 4, 'D')
    p.jag(22, 1, 10, 7); p.put(25, 8, 'D'); p.put(31, 8, 'D')                 # the operating theatre: two doors
    for (x, y) in ((25, 4), (28, 4)): p.put(x, y, 'm')
    p.scatter(22, 1, 10, 7, "d::*'", 6)
    p.jag(33, 2, 11, 6); p.put(36, 8, 'D'); p.scatter(33, 2, 11, 6, "ll\"'", 5)   # the north ward
    for (x0, w) in ((2, 8), (11, 8), (29, 7), (37, 7)):                       # south wards of all sizes
        p.jag(x0, 12, w, 4); p.put(x0 + r.randint(1, w - 2), 11, 'D'); p.scatter(x0, 12, w, 4, "llc\"&", 3)
    p.room(20, 12, 8, 4); p.room(22, 16, 3, 1)                                # the lobby, sandbagged, main doors south
    for x in (20, 21, 26, 27): p.put(x, 13, 's')
    p.collapse(41, 11, 2); p.breach(44, 11, 2)                                # the east wing caved in: a way through the rubble
    p.breach(19, 13); p.breach(28, 14)
    p.dirty()
    p.trail([(23, 14), (23, 10), (30, 10), (27, 5)])                          # the wounded carried to the theatre
    p.pool(5, 14); p.pool(40, 4); p.scatter(1, 9, 44, 2, "~'%", 8)
    # outer doors and windows (on the outer wall itself, which Plan.put leaves alone)
    for (x, y) in ((23, 17), (0, 9), (45, 10)): p.g[y][x] = 'D'
    for x in range(3, 44, 4):
        for y in (0, 17):
            if p.g[y][x] == '#' and p.g[1 if y == 0 else 16][x] != '#': p.g[y][x] = 'n'
    for y in (3, 6, 13):
        for x in (0, 45):
            if p.g[y][1 if x == 0 else 44] != '#': p.g[y][x] = 'n'
    for (x, y) in ((6, 4), (25, 3), (12, 9), (32, 10), (24, 13)): p.put(x, y, 'i')   # the base has power: lamps work
    p.put(38, 10, 'e')
    return p

if __name__ == "__main__":
    here = os.path.dirname(os.path.abspath(__file__))
    p = hospital(); rows = p.rows()
    p.mend((23, 14))
    rows = p.rows()
    walk = [(x, y) for y, row in enumerate(rows) for x, ch in enumerate(row) if ch == 'D' and (x in (0, 45) or y in (0, 17))]
    n, cut = p.check((23, 14)); print("hospital walkable", n, "unreachable", len(cut), "outer doors", walk)
    assert not cut and rows[6][8] == 'A'
    open(os.path.join(here, "hospital.txt"), "w").write("\n".join(rows))
    print("\n".join(rows))
