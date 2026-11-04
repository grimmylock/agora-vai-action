# Batch 92: messy floor plans for the hidden labs and bunkers (they were a 4x-stamped room grid: one big hall with crates in rows).
# Each is an institute floor in the spirit of the Foundry: a spine corridor that jogs, rooms of every size off both sides, a back
# corridor for a second way round, one long hall for the long guns, holes knocked through walls, a cave-in, blood, and lamps that
# mostly don't work. Writes <id>.txt next to itself; TierDungeons.Plans.cs holds the pasted plans.
# Marks: 'E' = entry (first landmark + the exit), 'G' = the goal room's landmark. Legend: see foundry_plans.py.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from foundry_plans import Plan

FURNISH = {
    "office": "dddll\"\"c",
    "store": "cccbbt",
    "lab": "mmdl::",
    "ward": "lldd\"",
    "plant": "mmmpb",
    "archive": "lllll\"\"\"",
}

def institute(seed, w=72, h=44, kinds=("office", "store", "lab", "ward"), hall="lab", flood=0, caves=1, scenes=4, lit=0.4):
    p = Plan(w, h, seed); r = p.r
    # the spine: 3 wide, jogs up or down once or twice on its way east
    y = h // 2 - 1; x = 9; spine = []
    jogs = sorted(r.sample(range(20, w - 20), r.randint(1, 2)))
    for jx in jogs + [w - 3]:
        p.hline(x, jx, y, 3); spine.append((x, jx, y))
        ny = max(8, min(h - 11, y + r.choice((-3, -2, 2, 3))))
        p.vline(jx, min(y, ny), max(y, ny) + 2, 3); x = jx; y = ny
    # entry: a lift room at the west end, off the spine
    ey = spine[0][2] + 1
    p.jag(2, ey - 3, 6, 7); p.put(8, ey, 'D'); p.put(4, ey, 'E')
    rooms = []
    # rooms above and below each spine run
    for (x0, x1, sy) in spine:
        for side in (-1, 1):
            x = x0 + r.randint(0, 2)
            while x < x1 - 5:
                rw = r.randint(5, 11)
                if x + rw > x1: break
                if side < 0: top = r.randint(3, 5); bot = sy - 2; rh = bot - top + 1
                else: top = sy + 4; rh = r.randint(5, min(11, h - 4 - top)); bot = top + rh - 1
                if rh >= 4:
                    p.jag(x, top, rw, rh, bite=0.6)
                    dx = x + r.randint(1, rw - 2)
                    if side < 0: p.vline(dx, bot + 1, sy - 1); p.put(dx, bot + 1, 'D')
                    else: p.vline(dx, sy + 3, top - 1); p.put(dx, top - 1, 'D')
                    rooms.append((x, top, rw, rh, side))
                x += rw + r.randint(1, 2)
    # a long hall: one room on the far side blown out to the full run
    if rooms:
        big = max(rooms, key=lambda q: q[2] * q[3])
        x, top, rw, rh, side = big
        ext = min(w - 3 - (x + rw), r.randint(8, 16))
        p.room(x, top, rw + ext, rh); rooms.remove(big); rooms.append((x, top, rw + ext, rh, side))
        for k in range(r.randint(3, 6)): p.put(r.randrange(x + 1, x + rw + ext - 1), r.randrange(top + 1, top + rh - 1), 'h')
        big = rooms[-1]
    # back corridors: along the top and bottom edge, with doors into the rooms that reach them (the second way round)
    for edge, side in ((2, -1), (h - 3, 1)):
        reach = [q for q in rooms if q[4] == side and (q[1] <= 4 if side < 0 else q[1] + q[3] >= h - 5)]
        if len(reach) < 2: continue
        a, b = min(q[0] for q in reach), max(q[0] + q[2] for q in reach)
        p.hline(a, b, edge)
        for q in reach:
            if r.random() < 0.7:
                dx = q[0] + r.randint(1, q[2] - 2)
                if side < 0: p.vline(dx, edge, q[1] - 1); p.put(dx, q[1] - 1, 'D')
                else: p.vline(dx, q[1] + q[3], edge); p.put(dx, q[1] + q[3], 'D')
        for x in range(a, b + 1, 4):
            if p.at(x, edge) == '.' and r.random() < 0.5: p.put(x, edge, 'p')
    # furnish, knock holes between neighbours
    for i, (x, top, rw, rh, side) in enumerate(rooms):
        kind = hall if (x, top, rw, rh, side) == big else r.choice(kinds)
        p.scatter(x, top, rw, rh, FURNISH[kind], max(2, rw * rh // 9))
        if kind == "plant": p.flood(x + rw // 2, top + rh // 2, 1)
    for _ in range(r.randint(2, 4)):
        q = r.choice(rooms); x, top, rw, rh, side = q
        bx = x - 1 if r.random() < 0.5 else x + rw
        by = r.randrange(top + 1, top + rh - 1)
        if p.at(bx - 1, by) != '#' and p.at(bx + 1, by) != '#': p.breach(bx, by)
    for _ in range(caves):
        x0, x1, sy = r.choice(spine); p.collapse(r.randint(x0 + 2, x1 - 2), sy + 1, 2)
    for _ in range(flood):
        q = r.choice(rooms); p.flood(q[0] + q[2] // 2, q[1] + q[3] // 2, 2)
    # the goal: the room farthest from the entry
    gx, gy, best = 0, 0, -1
    for (x, top, rw, rh, side) in rooms:
        d = abs(x + rw // 2 - 4) + abs(top + rh // 2 - ey)
        if d > best: best, gx, gy = d, x + rw // 2, top + rh // 2
    p.put(gx, gy, 'G')
    p.dirty()
    for _ in range(scenes):                                     # someone didn't make it out
        q = r.choice(rooms); x, top, rw, rh, side = q
        cx, cy = x + rw // 2, top + rh // 2
        sx, sy = spine[0][0] + r.randint(5, 30), spine[0][2] + 1
        p.trail([(cx, cy), (cx, (cy + sy) // 2), (min(sx, w - 4), (cy + sy) // 2)])
        p.scatter(x, top, rw, rh, "'`", 3)
    # lamps: a share of rooms keep a working light; red emergency lamps along the spine, most of them dead
    for (x, top, rw, rh, side) in rooms:
        if r.random() < lit:
            lx, ly = x + r.randint(1, rw - 2), top + r.randint(1, rh - 2)
            if p.at(lx, ly) in '.,%': p.put(lx, ly, 'i')
    for (x0, x1, sy) in spine:
        for x in range(x0 + 4, x1, 9):
            if r.random() < 0.5 and p.at(x, sy) in '.,%': p.put(x, sy, 'e')
    p.put(5, ey - 1, 'e')
    p.mend((4, ey))
    return p

LABS = {
    "secret_lab_alpha":     dict(seed=201, kinds=("office", "lab", "store", "lab"), hall="lab", scenes=4, lit=0.35),
    "secret_lab_beta":      dict(seed=202, kinds=("archive", "office", "ward", "ward"), hall="archive", scenes=5, lit=0.3),
    "secret_lab_gamma":     dict(seed=203, w=76, kinds=("lab", "plant", "office", "store"), hall="lab", scenes=4, lit=0.3),
    "underground_bunker":   dict(seed=204, kinds=("store", "ward", "office", "store"), hall="store", scenes=3, lit=0.45),
    "sub_station_depths":   dict(seed=205, kinds=("plant", "plant", "store", "office"), hall="plant", flood=4, scenes=2, lit=0.25),
    "maintenance_tunnels":  dict(seed=206, h=40, kinds=("plant", "store", "store"), hall="plant", caves=3, scenes=3, lit=0.2),
}

def visor_wing(rows):
    """Boss 1 (ACTION_DESIGN "Melee", the Tactical Hatchet): Lab Beta's secret wing. A cracked stretch of wall (Z) on the east
    side of the south-west rooms, a long dark corridor behind it through the rock, and a small room at the end where a body in
    black armour lies with the hatchet in it (V). Added after the reachability check: it's sealed until the wall is broken."""
    g = [list(r) for r in rows]
    y = 31
    wall = next(x for x in range(30, 60) if g[y][x] == '#' and g[y][x - 1] != '#')        # the first wall east of the room
    assert all(g[yy][xx] == '#' for yy in range(29, 35) for xx in range(wall + 1, 69)), "the rock isn't solid there"
    g[y][wall] = 'Z'
    for x in range(wall + 1, 58): g[y][x] = '.'                                            # the corridor, 1 wide, unlit
    g[y][wall + 9] = 'e'; g[y][wall + 15] = '*'                                             # one red light half way, old blood
    for yy in range(30, 34):
        for x in range(58, 67): g[yy][x] = '.'                                             # the room at the end
    g[30][58] = '%'; g[33][66] = '%'; g[30][59] = 'x'; g[33][58] = 'x'; g[30][66] = 'e'
    g[31][63] = 'V'; g[31][62] = '*'; g[32][63] = '*'
    return ["".join(r) for r in g]

# Boss 2 (Yan, 2026-11-03): Secret Lab Alpha, redesigned: "right now it looks very even in space, it doesn't have a linear progression ...
# most rooms are locked, the few that are open have either a key, a hole in the wall going into the other room ... avoid 20 rooms that
# look the same and corridors". A mission graph first (checked with the level-design skill's graph_check.py - see WORK_LOG), then rooms:
#   E  Reception Airlock (the start; a dead guard's keycard)     A  Atrium (the hub: three locked doors in sight, windows onto the vat room)
#   S  Security office (the staff badge, in a locker, 3)          C  Canteen   M  Maintenance corridor (dark)   P  Generator hall (the switch, 5)
#   L  Labs wing (badge door 1; the fuse crate, 4)                R  Archive (badge door 1; a hole in the wall from Security)
#   G  Sample Storage (the story terminal)                        V  the vat room (power doors 2, from the Atrium and from the Labs): the boss
# Locks: staff badge = blue doors (1), power = dead hazard doors (2); the generator switch needs the fuse. Loops: the hole S-R, the door
# E-P (south from the airlock to the generator hall: the lock you can see before you can open it), the Labs-vat door. Nothing is spent.
# Legend additions: 1 badge door, 2 power door, 3 badge locker, 4 fuse crate, 5 generator switch (see TierDungeons.Plans.cs).
VAT_ROOM = [            # x 27..47, y 3..19
    "jj.u..o..q.q..o..u.jj",
    "g...................g",
    "g..c....q...q....c..g",
    "u...a...q...q...a...u",
    ".......q.....q.......",
    "f..c..qq.....qq..c..f",
    "....a..q.....q..a....",
    "..c.....:...:.....c..",
    "......f.........f....",
    "i..a.............a..i",
    "...........*.........",
    "..c...f.......f...c..",
    ".....................",
    "u..a......e......a..u",
    "....c...........c....",
    "g......%...%.......g.",
    ".....................",
]

def alpha_plan():
    p = Plan(72, 44, 201); R = p.room
    R(3, 22, 7, 9); R(21, 21, 32, 10)                                   # E: the Reception Airlock; A: the Atrium
    p.hline(11, 19, 25, 3); p.put(10, 26, 'D'); p.put(20, 26, 'D')      # the corridor between, a door at each end
    R(54, 22, 15, 9); p.put(53, 26, 'D')                                # S: Security
    R(3, 3, 22, 17); R(27, 3, 21, 17); R(51, 3, 18, 17)                 # L: Labs, V: the vat room, R: the Archive
    R(27, 32, 19, 11); R(48, 32, 21, 11)                                # C: the Canteen, G: Sample Storage
    p.put(36, 31, 'D'); p.put(50, 31, 'D')                              # the atrium's south doors
    R(15, 36, 11, 3); p.put(26, 37, 'D'); R(3, 33, 12, 10)              # M: the maintenance corridor (canteen west door), P: the generator hall
    p.put(5, 31, 'D'); p.put(5, 32, 'D')                                # the E-P shortcut: south from the airlock
    for x in (22, 51): p.put(x, 20, '1')                                # badge doors: Atrium -> Labs, Atrium -> Archive
    p.put(37, 20, '2'); p.put(25, 12, '2'); p.put(26, 12, '2')          # power doors: Atrium -> vat room, Labs -> vat room
    for x in (30, 31, 32, 33, 34, 40, 41, 42, 43, 44): p.put(x, 20, 'n')   # windows from the atrium onto the vat room
    for x in (66, 67):
        for y in (20, 21): p.put(x, y, 'r')                             # the hole in the wall: Security <-> Archive
    # the vat room (the boss fight's arena)
    for dy, line in enumerate(VAT_ROOM):
        for dx, ch in enumerate(line): p.put(27 + dx, 3 + dy, ch)
    # E: the airlock. The way in, a dead guard's desk, a light.
    p.put(5, 26, 'E'); p.put(6, 22, 'i'); p.put(8, 29, 'c'); p.put(3, 23, 'd'); p.put(3, 24, 'd'); p.put(9, 23, 'b'); p.put(7, 26, '*'); p.put(6, 27, '&')
    p.hline(12, 18, 24, 1, 'p') if False else None
    p.put(15, 26, 'i')
    # A: the atrium: a reception desk, pillars you can fight round, the locked doors' lights, a blood trail from the power door
    for x in (23, 24, 25): p.put(x, 22, 'd')
    for (x, y) in ((31, 25), (43, 25), (31, 28), (43, 28)): p.put(x, y, 'h')
    for (x, y) in ((26, 23), (37, 24), (48, 23), (26, 28), (48, 28)): p.put(x, y, 'i')
    for y in range(22, 27): p.put(37, y, '~')
    for (x, y) in ((22, 29), (23, 29), (49, 29), (50, 29)): p.put(x, y, 'l')
    p.put(52, 22, 'e'); p.put(21, 22, 'e')
    # S: Security: lockers along the wall, the duty desk, the locker with the badge in the far corner
    for x in (55, 56, 57, 58, 59): p.put(x, 22, 'l')
    for (x, y) in ((58, 26), (59, 26), (58, 27)): p.put(x, y, 'd')
    p.put(67, 23, '3'); p.put(61, 25, 'i'); p.put(65, 29, 'c'); p.put(55, 28, 'b'); p.put(64, 27, ':')
    # R: the Archive: rows of shelving with aisles, papers everywhere
    for y in (5, 8, 11, 14, 17):
        for x in range(53, 67):
            if x not in (59, 60): p.put(x, y, 'l')                      # a gap in each row to cross
    for (x, y) in ((54, 3), (58, 12), (62, 15), (65, 6)): p.put(x, y, '"')
    p.put(60, 3, 'i'); p.put(60, 19, 'i')
    # L: the Labs wing: benches and tables in work bays, a divider with one doorway, jars along the north wall, the fuse crate far away
    p.vline(13, 3, 14, 1, '#'); p.put(13, 8, 'D'); p.put(13, 9, 'D')        # a divider: west bays / east cold room
    p.hline(14, 24, 14, 1, '#'); p.put(19, 14, 'D'); p.put(20, 14, 'D')
    for (x, y) in ((4, 3), (5, 3), (6, 3), (8, 3), (9, 3)): p.put(x, y, 'j')
    for (x, y) in ((4, 6), (6, 6), (8, 6), (10, 6), (4, 11), (6, 11), (8, 11), (10, 11)): p.put(x, y, 'g')
    for (x, y) in ((5, 8), (9, 8), (5, 13), (9, 13)): p.put(x, y, 'a')
    for (x, y) in ((11, 16), (4, 16), (4, 18)): p.put(x, y, 'u')
    for (x, y) in ((16, 4), (18, 4), (20, 4), (22, 4), (16, 8), (22, 8), (18, 11), (20, 11)): p.put(x, y, 'm')
    p.put(23, 4, 'j'); p.put(4, 4, '4'); p.put(21, 17, 'f'); p.put(22, 17, 'f')
    for (x, y) in ((8, 14), (7, 16), (12, 18), (16, 16), (6, 9)): p.put(x, y, '~')
    p.put(7, 5, 'i'); p.put(18, 7, 'i'); p.put(8, 17, 'i'); p.put(20, 17, 'i')
    # C: the Canteen: long tables, a kitchen counter
    for y in (34, 37, 40):
        for x in (30, 31, 32, 33, 37, 38, 39, 40): p.put(x, y, 'd')
    for x in range(42, 45): p.put(x, 33, 'm')
    p.put(29, 41, 'b'); p.put(43, 40, 't'); p.put(36, 33, 'i'); p.put(36, 41, 'i'); p.put(34, 38, '*'); p.put(35, 38, '&')
    # M: the maintenance corridor: dark, pipes, a leak
    for (x, y) in ((17, 35), (19, 39), (22, 35)): p.put(x, y, 'p')
    for (x, y) in ((18, 37), (19, 37), (23, 37)): p.put(x, y, 'w')
    # P: the generator hall: two rows of generators, the switch in the far corner, standing water, a red emergency light
    for x in (5, 7, 9, 11): p.put(x, 36, 'm'); p.put(x, 39, 'm')
    p.put(4, 41, '5'); p.put(12, 41, 'p'); p.put(13, 35, 'p'); p.put(3, 36, 'p')
    for (x, y) in ((6, 38), (7, 38), (8, 38), (10, 41), (11, 41)): p.put(x, y, 'w')
    p.put(6, 34, 'e'); p.put(12, 40, 'e')
    # G: Sample Storage: shelves of samples, the terminal at the back, crates
    for y in (34, 37, 40):
        for x in range(51, 62):
            if x % 5 != 0: p.put(x, y, 'l' if y != 37 else 'j')
    p.put(65, 37, 'G'); p.put(66, 36, 'o'); p.put(66, 38, 'o'); p.put(62, 41, 'c'); p.put(67, 41, 'c'); p.put(58, 33, 'i'); p.put(64, 40, 'i')
    p.dirty()
    return p

def alpha_check(p):
    """The plan itself, not the graph: walk it from E picking up keys (3 -> badge, 4 -> fuse, 5 needs the fuse -> power) until nothing changes.
    Returns {'noKeys': set of landmarks reachable with nothing, 'badge': .., 'full': ..} - the same story as the mission graph."""
    g = p.rows(); H, W = len(g), len(g[0])
    spots = {'E': 'E', 'G': 'G', '3': 'S', '4': 'L(fuse)', '5': 'P(switch)'}
    start = next((x, y) for y in range(H) for x in range(W) if g[y][x] == 'E')
    keys = set(); changed = True
    def walk(keys):
        seen = {start}; q = [start]
        while q:
            x, y = q.pop()
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                n = (x + dx, y + dy)
                if n in seen or not (0 <= n[0] < W and 0 <= n[1] < H): continue
                ch = g[n[1]][n[0]]
                if ch in '#n': continue
                if ch == '1' and 'badge' not in keys: continue
                if ch == '2' and 'power' not in keys: continue
                seen.add(n); q.append(n)
        return seen
    stages = []
    while changed:
        changed = False; seen = walk(keys); stages.append((set(keys), seen))
        for (x, y) in seen:
            ch = g[y][x]
            if ch == '3' and 'badge' not in keys: keys.add('badge'); changed = True
            if ch == '4' and 'fuse' not in keys: keys.add('fuse'); changed = True
            if ch == '5' and 'fuse' in keys and 'power' not in keys: keys.add('power'); changed = True
    seen = walk(keys); stages.append((set(keys), seen))
    return stages, g

# Sub-Station Depths (Tier 3, under the Dried Lake), redesigned the same way (Yan, 2026-11-04: "redo the rest of the dungeons the same way").
# A flooded switching station; every room has one job:
#   E  Flooded Corridor (the ladder in; knee-deep water)        H  Switchyard Hall (the hub: breaker rows, the badge door, the dead power
#   Q  Workers' Quarters (bunks; the fuse in a crate, 4)            door to the pumps, windows onto the black basin and the vault door beyond)
#   K  Control Room (the dispatcher's locker: the staff badge, 3; windows onto the basin)
#   Y  Cable Gallery (badge doors 1 from the hall and from the corridor: long, dark)   B  Battery Room (the breaker, 5, needs the fuse)
#   U  Pump Room (power door 2; a hole from the quarters lets you see the dead pump panel, 6, early)
#   W  the Basin (deep water: over your head until the pumps run)   G  Transformer Vault (the goal, across the basin)
# Keys: badge -> fuse -> power -> drain. Loops: the corridor's own badge door into the gallery, the hole quarters <-> pump room.
def depths_plan():
    p = Plan(72, 44, 205); R = p.room
    R(3, 16, 12, 8); p.hline(16, 19, 18, 3); p.put(15, 19, 'D'); p.put(20, 19, 'D')         # E -> the hall
    R(21, 14, 25, 13)                                                                       # H: the Switchyard Hall
    R(47, 18, 13, 9); p.put(46, 22, 'D')                                                    # K: the Control Room
    p.hline(5, 51, 5, 3); R(53, 3, 15, 12); p.put(52, 6, 'D')                               # Y: the Cable Gallery, B: the Battery Room
    p.vline(33, 8, 12); p.put(33, 13, '1'); p.vline(6, 8, 14); p.put(6, 15, '1')            # badge doors: hall -> gallery, corridor -> gallery
    R(3, 28, 14, 14); p.vline(8, 25, 26); p.put(8, 24, 'D'); p.put(8, 27, 'D')              # Q: the Quarters, off the corridor
    R(19, 30, 14, 11); p.put(26, 27, '2'); p.put(26, 28, '.'); p.put(26, 29, 'D')           # U: the Pump Room, behind a dead blast door
    for (x, y) in ((17, 35), (18, 35), (17, 36), (18, 36)): p.put(x, y, 'r')               # the hole: quarters <-> pump room
    R(35, 28, 22, 14, 'W'); p.put(40, 27, 'D')                                              # the Basin, all deep water ...
    for y in (28, 29, 30):
        for x in range(35, 51): p.put(x, y, ',')                                            # ... but a concrete ledge under the hall
    for y in (31, 32, 33): p.put(44, y, 'w')                                                # a drowned stair: it just goes down
    R(58, 29, 11, 13); p.put(57, 36, 'D')                                                   # G: the Transformer Vault, across the water
    for x in (36, 37, 38, 42, 43, 44): p.put(x, 27, 'n')                                    # windows: hall -> basin
    for x in (50, 51, 52): p.put(x, 27, 'n')                                                # control room -> basin
    # E: the corridor. The ladder, knee-deep water, a body that tried the deep end
    p.put(4, 17, 'E'); p.put(5, 16, 'i')
    for (x, y) in ((7, 19), (8, 19), (9, 20), (10, 20), (11, 19), (12, 21), (9, 22), (10, 22), (11, 22), (12, 18), (13, 18)): p.put(x, y, 'w')
    p.put(13, 22, '*'); p.put(14, 22, '&'); p.put(3, 23, 'b'); p.put(14, 16, 'c')
    # H: the hall: three rows of breaker cabinets with gaps, pillars, cables everywhere, one working light
    for y in (16, 20, 24):
        for x in range(23, 44):
            if x not in (28, 29, 38, 39): p.put(x, y, 'm' if y != 20 else 'f')
    for (x, y) in ((22, 18), (44, 18), (22, 22), (44, 22)): p.put(x, y, 'h')
    for (x, y) in ((25, 18), (30, 22), (36, 18), (41, 22), (33, 26), (27, 26)): p.put(x, y, 'q')
    p.put(33, 14, 'i'); p.put(21, 26, 'e'); p.put(45, 14, 'e'); p.put(39, 26, ';'); p.put(40, 26, ';')
    # K: the control room: the dispatcher's desk, consoles along the windows, the locker in the corner
    for x in (49, 50, 51, 52, 53): p.put(x, 25, 'o')
    for (x, y) in ((50, 21), (51, 21), (55, 21)): p.put(x, y, 'd')
    p.put(58, 19, '3'); p.put(56, 19, 'l'); p.put(57, 19, 'l'); p.put(48, 18, 'f'); p.put(53, 19, 'i'); p.put(52, 22, '"'); p.put(54, 23, '"')
    # Y: the gallery: cable trays, pipes, dead red lights, a trail of blood toward the battery room
    for x in range(8, 50, 3): p.put(x, 5, 'q')
    for x in range(10, 50, 7): p.put(x, 7, 'p')
    for x in (14, 30, 46): p.put(x, 6, 'e')
    for x in range(36, 51): p.put(x, 6, '~') if x % 2 else None
    # B: the battery room: rows of cells, acid, the breaker on the far wall
    for y in (6, 9, 12):
        for x in range(55, 66):
            if x != 60: p.put(x, y, 'm')
    p.put(66, 4, '5'); p.put(54, 3, 'i'); p.put(62, 10, ';'); p.put(63, 11, ';'); p.put(57, 14, 'b'); p.put(67, 14, 'x')
    # Q: the quarters: bunks (lockers), a table, the crate with the fuse under the far bunk
    for y in (29, 33, 37):
        p.put(4, y, 'l'); p.put(5, y, 'l'); p.put(14, y, 'l'); p.put(15, y, 'l')
    p.put(9, 34, 'd'); p.put(10, 34, 'd'); p.put(4, 40, '4'); p.put(9, 31, 'i'); p.put(12, 39, 't'); p.put(16, 41, 'c')
    p.put(10, 36, '*'); p.put(11, 37, '\''); p.put(6, 41, '"')
    # U: the pump room: three pumps, pipes to the basin, the dead panel
    for x in (22, 26, 30): p.put(x, 33, 'm'); p.put(x, 34, 'm')
    for x in range(21, 33, 2): p.put(x, 37, 'p')
    p.put(19, 40, 'b'); p.put(32, 40, 'w'); p.put(31, 40, 'w'); p.put(24, 31, 'e')
    # G: the vault: transformers round the walls, a console in the middle, the researcher's last stand
    for (x, y) in ((59, 30), (60, 30), (66, 30), (67, 30), (59, 40), (60, 40), (66, 40), (67, 40)): p.put(x, y, 'm')
    # the stairwell down to the flooded levels: a walled corner of the pump room behind its own power door (the hole only shows it)
    for y in range(37, 41): p.put(29, y, '#')
    for x in range(29, 33): p.put(x, 36, '#')
    p.room(30, 37, 3, 4); p.put(29, 38, '2'); p.put(31, 39, 'S'); p.put(32, 37, 'e')
    p.put(55, 40, 'Y')                                                                      # the service lift up from the bottom, in the drained basin
    p.put(64, 35, 'G'); p.put(63, 34, 'o'); p.put(65, 37, '*'); p.put(66, 37, '&'); p.put(61, 33, '"'); p.put(68, 35, 'e'); p.put(62, 38, 'i')
    p.dirty()
    return p

# The rooms of depths_plan (x, y, w, h), named as in sub_station_depths.mmd; anything walkable outside them is a passage.
DEPTHS_ROOMS = {'E': (3, 16, 12, 8), 'H': (21, 14, 25, 13), 'K': (47, 18, 13, 9), 'Y': (5, 5, 47, 3), 'B': (53, 3, 15, 12),
                'Q': (3, 28, 14, 14), 'U': (19, 30, 14, 11), 'L': (35, 28, 22, 14), 'V': (58, 29, 11, 13)}
DEPTHS_SWITCHES = {'BS': ('B', '5'), 'Q': ('Q', '4'), 'K': ('K', '3')}   # graph node -> (room, plan letter)

def graph_edges(path):
    """The passages of a Mermaid mission graph as {(frozenset(a, b), req or None)} (the subset graph_check.py reads)."""
    import re
    out = set()
    for line in open(path):
        m = re.match(r'\s*(\w+)(?:\[".*?"\])?\s*-->(?:\|([^|]*)\|)?\s*(\w+)', line)
        if m:
            req = next((t[4:] for t in (m.group(2) or '').split(',') if t.startswith('req:')), None)
            out.add((frozenset((m.group(1), m.group(3))), req))
    return out

def plan_edges(g, rooms, locks=None):
    """The same thing read off the grid: which rooms reach which through passages, and the locks on the way ('W' = drain)."""
    locks = locks or {'1': 'badge', '2': 'power', 'W': 'drain'}
    def room_of(x, y):
        if g[y][x] == 'W': return None                      # deep water is a lock, never a room
        return next((r for r, (rx, ry, rw, rh) in rooms.items() if rx <= x < rx + rw and ry <= y < ry + rh), None)
    H, W = len(g), len(g[0]); out = set()
    for r in rooms:
        cells = [(x, y) for y in range(H) for x in range(W) if g[y][x] not in '#n' and room_of(x, y) == r]
        seen = {(c, None) for c in cells}; q = list(seen)
        while q:
            (x, y), lock = q.pop()
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if not (0 <= nx < W and 0 <= ny < H) or g[ny][nx] in '#n': continue
                other = room_of(nx, ny)
                if other == r: continue
                if other is not None: out.add((frozenset((r, other)), lock)); continue
                st = ((nx, ny), locks.get(g[ny][nx], lock))
                if st not in seen: seen.add(st); q.append(st)
    return out

def key_walk(p, start_ch, gives, blocks, held=()):
    """Walks a plan from start_ch picking up keys until nothing changes. gives: plan letter -> (key, needs or None); blocks: plan
    letter -> key; held: keys from other maps. Returns [(keys, tiles reached)] per stage."""
    g = p.rows(); H, W = len(g), len(g[0])
    start = next((x, y) for y in range(H) for x in range(W) if g[y][x] == start_ch)
    def walk(keys):
        seen = {start}; q = [start]
        while q:
            x, y = q.pop()
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                n = (x + dx, y + dy)
                if n in seen or not (0 <= n[0] < W and 0 <= n[1] < H): continue
                ch = g[n[1]][n[0]]
                if ch in '#n' or (ch in blocks and blocks[ch] not in keys): continue
                seen.add(n); q.append(n)
        return seen
    keys = set(held); stages = []
    while True:
        seen = walk(keys); stages.append((keys - set(held), seen)); new = False
        for (x, y) in seen:
            k = gives.get(g[y][x])
            if k and k[0] not in keys and (k[1] is None or k[1] in keys): keys.add(k[0]); new = True
        if not new: return stages, g


# ------------------------------------------------------------------ the eel's flooded levels (Yan, 2026-11-04; sub_station_depths.mmd F-1..F-3)
# Each is a cistern under the station: deep water ('W', over your head) with steel catwalks ('=': grated decks on legs, rusted,
# mossy, no guard rails - Yan) over it.
# The level's pump ('6') drops the water, which opens its stairs down ('S', on a landing under the water). 'U' = stairs up.
# Islands of 3x3 or more stand clear in open water so the eel has room to wrap round them; catwalks are 1-2 wide (tense to cross).
def cistern(w, h, seed):
    p = Plan(w, h, seed); p.room(2, 2, w - 4, h - 4, 'W'); return p

def isle(p, x, y, w, h):                                       # a steel catwalk deck on legs, a corner rusted away now and then
    p.room(x, y, w, h, '=')
    for (cx, cy) in ((x, y), (x + w - 1, y), (x, y + h - 1), (x + w - 1, y + h - 1)):
        if p.r.random() < 0.5 and w > 3 and h > 3: p.put(cx, cy, 'W')

def walk(p, x0, y0, x1, y1, wide=1, ch='='):                 # a catwalk: along x, then along y
    p.hline(x0, x1, y0, wide, ch); p.vline(x1, y0, y1, wide, ch)

def pump_spur(p, x, y0, y1):
    """The pump at the end of a long 1-wide catwalk (Yan: "the pumps should be at the end of a thin catwalk not in a whole platform,
    otherwise the eel can't block it by wraping around"): the catwalk from y0 up to y1, the pump on its last tile, a lamp before it."""
    p.vline(x, y1, y0, 1, '='); p.put(x, y1, '6'); p.put(x, y1 + 1, 'i')

def depths_level1():
    """The Cistern: the eel's first strike. Small islands (3x3, room for it to wrap) on single catwalks from the stairs to the last
    island, then a long thin spur out to the pump. The stairs down sit on a landing in plain sight, under the water until the pump runs."""
    p = cistern(64, 40, 211)
    isle(p, 3, 3, 5, 4); p.put(5, 4, 'U'); p.put(4, 3, 'e')                   # the landing at the foot of the stairs
    walk(p, 8, 5, 15, 5); isle(p, 16, 4, 3, 3)                                # A
    walk(p, 17, 7, 17, 14); isle(p, 16, 15, 3, 3)                             # B
    walk(p, 8, 16, 15, 16); isle(p, 5, 15, 3, 3); p.put(5, 15, 'c')           # C: a dead end with a crate (it pays)
    walk(p, 19, 16, 30, 16); isle(p, 31, 15, 3, 3)                            # D: the middle
    walk(p, 32, 18, 32, 26)                                                   # south: the long one
    isle(p, 31, 27, 3, 3); p.put(33, 29, '*'); p.put(32, 29, '&')            # E: the strike island (clear water all round), someone who didn't make it
    walk(p, 34, 28, 47, 28); isle(p, 48, 27, 3, 3)                            # F: the last island
    pump_spur(p, 49, 26, 18)                                                  # the pump, out on its spur
    isle(p, 40, 8, 4, 4); p.put(41, 9, 'S'); p.put(43, 8, 'e')               # the stairs down: in sight, cut off by the water
    p.put(17, 5, '*')
    return p

def depths_level2():
    """The Lower Cistern: a search over longer, thinner catwalks (the eel's strikes come on them), three dead ends, the pump on a spur
    off the farthest island. The stairs down are on a landing in the middle, under the water."""
    p = cistern(64, 40, 212)
    isle(p, 55, 3, 4, 4); p.put(57, 4, 'U')                                   # the landing (stairs up, north-east)
    walk(p, 43, 5, 54, 5); isle(p, 40, 4, 3, 3)                               # A
    walk(p, 22, 5, 39, 5); isle(p, 19, 4, 3, 3); p.put(19, 4, 'c')            # C: a dead end over the longest catwalk, a crate
    walk(p, 41, 7, 41, 15); isle(p, 40, 16, 3, 3)                             # B
    walk(p, 24, 17, 39, 17); isle(p, 21, 16, 3, 3)                            # D: west
    walk(p, 22, 19, 22, 28); isle(p, 21, 29, 3, 3); p.put(23, 31, 't'); p.put(21, 29, '*')   # E: a dead end (south-west)
    walk(p, 41, 19, 41, 29); isle(p, 40, 30, 3, 3)                            # F: south
    walk(p, 43, 31, 50, 31); isle(p, 51, 30, 3, 3)                            # G: the farthest island
    pump_spur(p, 52, 29, 21)                                                  # the pump, out on its spur
    isle(p, 28, 9, 4, 4); p.put(29, 10, 'S')                                  # the stairs down, under the water
    p.put(41, 25, 'e'); p.put(30, 17, 'e')
    return p

def depths_level3():
    """The Sump: the arena. The water is low (knee-deep, slow), dry patches and pillars for cover, a deep pool in the middle where the
    eel lies and rises; the last pump past it on a plinth to the south; the bottom (the goal) and the service lift behind a band of
    deep water that only the last pump empties."""
    p = Plan(60, 42, 213)
    p.room(26, 3, 8, 5, ','); p.put(29, 4, 'U')                              # the landing, steps down into the sump
    p.room(6, 9, 48, 24, 'w'); p.hline(28, 31, 8, 1, ',')
    for (x, y, w, h) in ((9, 11, 6, 4), (44, 11, 7, 4), (8, 26, 7, 4), (45, 26, 7, 4), (20, 12, 4, 3), (36, 12, 4, 3), (19, 27, 5, 3), (36, 27, 5, 3)):
        p.room(x, y, w, h, ',')                                               # dry patches
    for (x, y) in ((12, 18), (47, 18), (17, 22), (42, 22), (24, 30), (35, 30)): p.put(x, y, 'h')   # pillars to break its line
    for y in range(13, 25):                                                   # the eel's pool, deep, in the middle: a ragged round sump
        for x in range(21, 39):
            if ((x + 0.5 - 30) / 7.2) ** 2 + ((y + 0.5 - 18.5) / 5.0) ** 2 <= 1 + 0.18 * math.sin(x * 1.7 + y * 2.3): p.put(x, y, 'W')
    p.room(26, 33, 8, 3, ','); p.put(29, 34, '6'); p.put(27, 33, 'i')        # the last pump, on a plinth past the pool
    p.room(42, 35, 13, 5, ','); p.put(50, 37, 'G'); p.put(53, 38, 'Y'); p.put(45, 36, 'c'); p.put(46, 36, 'c')   # the bottom, the lift
    p.room(36, 34, 6, 2, 'W'); p.room(38, 33, 2, 1, 'W')                     # the deep band between the sump and the bottom
    for (x, y) in ((8, 10), (51, 10), (8, 31), (51, 31), (30, 12)): p.put(x, y, 'e')
    p.put(29, 24, '~'); p.put(30, 25, '*')
    return p

DEPTHS_LEVELS = {"sub_station_depths_1": (depths_level1, 'drain1', 'power', 'S'),
                 "sub_station_depths_2": (depths_level2, 'drain2', 'drain1', 'S'),
                 "sub_station_depths_3": (depths_level3, 'drain3', 'drain2', 'G')}

if __name__ == "__main__":
    here = os.path.dirname(os.path.abspath(__file__))
    for name, kw in LABS.items():
        if name == "secret_lab_alpha":      # Boss 2: hand-planned, not generated (alpha_plan); its checks run on the real grid
            p = alpha_plan(); stages, g = alpha_check(p); rows = p.rows()
            def where(seen, ch): return any(g[y][x] == ch for (x, y) in seen)
            s0, s1, sN = stages[0][1], stages[1][1], stages[-1][1]
            vat = lambda seen: any(27 <= x <= 47 and 3 <= y <= 19 for (x, y) in seen)   # anywhere inside the vat room
            print(name, p.w, "x", p.h, "keys by stage:", [sorted(st[0]) for st in stages])
            assert where(s0, 'G') and where(s0, '3') and not where(s0, '4'), "start: Sample Storage and the badge locker open, the fuse crate not"
            assert not vat(s0) and not vat(stages[1][1]), "the vat room must stay shut until the power is on"
            assert vat(sN) and where(sN, '5') and where(sN, '4'), "with every key the whole lab opens"
            open(os.path.join(here, f"{name}.txt"), "w").write("\n".join(rows)); continue
        if name == "sub_station_depths":    # the station (2026-11-04, then the eel's levels below it): checked key by key and against the graph
            p = depths_plan(); rows = p.rows()
            stages, g = key_walk(p, 'E', {'3': ('badge', None), '4': ('fuse', None), '5': ('power', 'fuse')}, {'1': 'badge', '2': 'power', 'W': 'drain3'})
            def has(seen, ch): return any(g[y][x] == ch for (x, y) in seen)
            print(name, p.w, "x", p.h, "keys by stage:", [sorted(st[0]) for st in stages])
            assert [st[0] for st in stages] == [set(), {'badge', 'fuse'}, {'badge', 'fuse', 'power'}], "badge and fuse first, then power"
            assert has(stages[0][1], '3') and has(stages[0][1], '4') and not has(stages[0][1], '5'), "start: the locker and the crate, not the breaker"
            assert not has(stages[1][1], 'S') and has(stages[2][1], 'S'), "the stairs down need the power (the hole only shows them)"
            assert not has(stages[-1][1], 'G') and not has(stages[-1][1], 'Y'), "the vault and the lift wait for the last level's drain"
            full, _ = key_walk(p, 'E', {}, {'1': 'badge', '2': 'power', 'W': 'drain3'}, held=('badge', 'fuse', 'power', 'drain3'))
            floor = {(x, y) for y in range(p.h) for x in range(p.w) if g[y][x] not in '#n'}
            assert floor <= full[-1][1] and has(full[-1][1], 'G') and has(full[-1][1], 'Y'), "drained, the whole station opens"
            # the plan is the graph: this map's passages and locks equal the graph's edges between its rooms
            want = {(e, None if r == 'hole' else r) for e, r in graph_edges(os.path.join(here, "sub_station_depths.mmd")) if e <= set(DEPTHS_ROOMS)}
            got = plan_edges(g, DEPTHS_ROOMS, {'1': 'badge', '2': 'power', 'W': 'drain3'})
            assert got == want, ("plan != graph", sorted(map(lambda e: (sorted(e[0]), str(e[1])), got ^ want)))
            for node, (room, ch) in DEPTHS_SWITCHES.items():
                rx, ry, rw, rh = DEPTHS_ROOMS[room]
                assert sum(row[rx:rx + rw].count(ch) for row in g[ry:ry + rh]) == 1, (node, "not in", room)
            print(name, "matches sub_station_depths.mmd:", len(got), "passages on this map")
            open(os.path.join(here, f"{name}.txt"), "w").write("\n".join(rows)); continue
        p = institute(**kw)
        rows = p.rows(); start = next((x, y) for y, row in enumerate(rows) for x, ch in enumerate(row) if ch == 'E')
        n, cut = p.check(start)
        assert any('G' in row for row in rows), name
        print(name, p.w, "x", p.h, "walkable", n, "unreachable", len(cut), cut[:6])
        assert not cut, (name, cut)
        if name == "secret_lab_beta": rows = visor_wing(rows)
        if name == "secret_lab_alpha": rows = vat_room(rows)
        open(os.path.join(here, f"{name}.txt"), "w").write("\n".join(rows))
    for name, (fn, drain, needs, goal) in DEPTHS_LEVELS.items():   # the eel's levels: the pump in reach, the way on only after it
        p = fn(); rows = p.rows()
        stages, g = key_walk(p, 'U', {'6': (drain, needs)}, {'W': drain}, held=(needs,))
        def has(seen, ch): return any(g[y][x] == ch for (x, y) in seen)
        assert [st[0] for st in stages] == [set(), {drain}], (name, [st[0] for st in stages])
        assert has(stages[0][1], '6') and not has(stages[0][1], goal), (name, "the pump in reach, the way on not")
        floor = {(x, y) for y in range(p.h) for x in range(p.w) if g[y][x] not in '#n'}
        assert floor <= stages[-1][1], (name, "drained, every tile", sorted(floor - stages[-1][1])[:6])
        # before the drain: the only dry tiles out of reach are the ones round the way on (no stray bits standing in the water)
        dry = {t for t in floor if g[t[1]][t[0]] != 'W'}
        way = key_walk(p, goal, {}, {'W': 'never'})[0][-1][1]
        assert dry - stages[0][1] <= way, (name, "dry tiles cut off in the water", sorted(dry - stages[0][1] - way)[:6])
        print(name, p.w, "x", p.h, "pump in reach at the start,", goal, "only after", drain)
        open(os.path.join(here, f"{name}.txt"), "w").write("\n".join(rows))
