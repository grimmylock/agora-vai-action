# Batch 92: messy floor plans for the hidden labs and bunkers (they were a 4x-stamped room grid: one big hall with crates in rows).
# Each is an institute floor in the spirit of the Foundry: a spine corridor that jogs, rooms of every size off both sides, a back
# corridor for a second way round, one long hall for the long guns, holes knocked through walls, a cave-in, blood, and lamps that
# mostly don't work. Writes <id>.txt next to itself; TierDungeons.Plans.cs holds the pasted plans.
# Marks: 'E' = entry (first landmark + the exit), 'G' = the goal room's landmark. Legend: see foundry_plans.py.
import os, sys
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

if __name__ == "__main__":
    here = os.path.dirname(os.path.abspath(__file__))
    for name, kw in LABS.items():
        p = institute(**kw)
        rows = p.rows(); start = next((x, y) for y, row in enumerate(rows) for x, ch in enumerate(row) if ch == 'E')
        n, cut = p.check(start)
        assert any('G' in row for row in rows), name
        print(name, p.w, "x", p.h, "walkable", n, "unreachable", len(cut), cut[:6])
        assert not cut, (name, cut)
        open(os.path.join(here, f"{name}.txt"), "w").write("\n".join(rows))
