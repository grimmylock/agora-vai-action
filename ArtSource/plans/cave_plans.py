# Batch 100: the three caves as plans (they were the 4x-stamped room grid like the old labs). Organic chambers joined by
# winding tunnels, eroded edges, pools, cave-ins and boulders to fight round; the catacombs are brick tunnels with bone niches.
# No lights: caves are dark (the Crystal Pool's glow is the one exception). Writes <id>.txt; to_cs.py pastes them.
# Marks: 'E' entry (first landmark + exit), 'G' goal (second landmark). 'k' = a boulder (hard cover), 'v' = hanging roots (slow).
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from foundry_plans import Plan

def tunnel(p, a, b, width=1):
    """a wandering tunnel from a to b"""
    (x, y), (tx, ty) = a, b
    for _ in range(400):
        for dx in range(width):
            for dy in range(width): p.put(x + dx, y + dy, '.')
        if (x, y) == (tx, ty): break
        if p.r.random() < 0.7:
            x += (tx > x) - (tx < x) if abs(tx - x) >= abs(ty - y) else 0
            y += (ty > y) - (ty < y) if abs(ty - y) > abs(tx - x) else 0
        else:
            x += p.r.choice((-1, 0, 1)); y += p.r.choice((-1, 0, 1))
        x = max(2, min(p.w - 3, x)); y = max(2, min(p.h - 3, y))

def erode(p, n=2):
    """roughen chamber edges: walls with open space round them crumble, a few open cells near walls close up"""
    for _ in range(n):
        g = [row[:] for row in p.g]
        for y in range(2, p.h - 2):
            for x in range(2, p.w - 2):
                opn = sum(p.g[y + dy][x + dx] != '#' for dy in (-1, 0, 1) for dx in (-1, 0, 1))
                if p.g[y][x] == '#' and opn >= 5 and p.r.random() < 0.5: g[y][x] = '.'
                elif p.g[y][x] == '.' and opn <= 4 and p.r.random() < 0.25: g[y][x] = '#'
        p.g = g

def cavern(seed, w, h, n, roots=False, pool=False, fork=False, cavein=False):
    p = Plan(w, h, seed); r = p.r
    centers = []
    for i in range(n):                                            # chambers spread over the map
        for _ in range(60):
            cx, cy = r.randrange(6, w - 6), r.randrange(5, h - 5)
            if all(math.hypot(cx - a, cy - b) > 10 for a, b in centers): break
        rad = r.randint(3, 6)
        p.disc(cx, cy, rad); centers.append((cx, cy))
        for _ in range(r.randint(1, 3)):                          # lobes: no round rooms
            p.disc(cx + r.randint(-rad, rad), cy + r.randint(-rad // 2, rad // 2), r.randint(2, 3))
    centers.sort()
    for i in range(1, n):                                         # each chamber to its nearest earlier one, some get a second way
        j = min(range(i), key=lambda k: math.hypot(centers[k][0] - centers[i][0], centers[k][1] - centers[i][1]))
        tunnel(p, centers[i], centers[j], r.choice((1, 2)))
        if r.random() < 0.35 and i > 1: tunnel(p, centers[i], centers[r.randrange(i)], 1)
    erode(p)
    e = centers[0]; g = max(centers, key=lambda c: math.hypot(c[0] - e[0], c[1] - e[1]))
    # features
    for (cx, cy) in centers:
        for _ in range(r.randint(1, 3)):                          # boulders: cover
            x, y = cx + r.randint(-3, 3), cy + r.randint(-2, 2)
            if p.at(x, y) == '.': p.put(x, y, 'k')
    if roots:                                                     # Root Hall: hanging roots choke the sightlines
        cx, cy = e
        for _ in range(16):
            x, y = cx + r.randint(-5, 5), cy + r.randint(-3, 3)
            if p.at(x, y) == '.': p.put(x, y, 'v')
    if pool:                                                      # Crystal Pool: still water and its faint glow
        p.flood(g[0], g[1] + 1, 2); p.put(g[0] + 3, g[1] - 2, 'i')
    if cavein:                                                    # Cave-In Chamber: half of it came down
        p.collapse(g[0] - 1, g[1], 3)
    else:
        mid = centers[len(centers) // 2]; p.collapse(mid[0], mid[1], 2)
    p.dirty()
    for _ in range(3):                                            # what lives down here eats here
        c = r.choice(centers); p.pool(c[0] + r.randint(-2, 2), c[1] + r.randint(-1, 1))
    p.put(e[0], e[1], 'E'); p.put(g[0], g[1], 'G')
    p.mend(e, '.')
    return p

def catacombs(seed, w=60, h=36):
    """brick tunnels in a rough grid, every wall lined with bone niches; the Ossuary a hall of shelves; the Sealed Crypt
    at the far end behind a door; a stretch caved in"""
    p = Plan(w, h, seed); r = p.r
    xs = sorted(r.sample(range(6, w - 6, 3), 4)); ys = sorted(r.sample(range(5, h - 5, 3), 3))
    for y in ys:                                                  # tunnels, each in a few stretches that jog up or down
        x = 3
        while x < w - 4:
            seg = r.randint(6, 14); jog = r.choice((-1, 0, 0, 1))
            if r.random() > 0.2: p.hline(x, min(w - 4, x + seg), y + jog, 2)   # a stretch fallen in
            x += seg
    for x in xs:
        y = 3
        while y < h - 4:
            seg = r.randint(5, 10); jog = r.choice((-1, 0, 0, 1))
            if r.random() > 0.25: p.vline(x + jog, y, min(h - 4, y + seg), 2)
            y += seg
    erode(p, 1)
    for y in ys:                                                  # bone niches along the tunnels
        for x in range(4, w - 4, 3):
            for side in (-1, 2):
                if p.at(x, y + side) == '#' and r.random() < 0.6: p.put(x, y + side, '&')
    p.room(xs[1] + 3, ys[0] + 3, 8, 5); ox, oy = xs[1] + 7, ys[0] + 5    # the Ossuary
    for x in range(xs[1] + 3, xs[1] + 11, 2): p.put(x, ys[0] + 3, 'l')   # shelves
    cx, cy = w - 6, h // 2                                        # the Sealed Crypt, at the far end behind one door
    p.room(cx - 3, cy - 2, 5, 5, ','); p.hline(xs[-1] + 2, cx - 4, cy); p.put(cx - 4, cy, 'D')
    p.collapse(xs[2], ys[1], 2)
    p.dirty()
    p.put(3, ys[1], 'E'); p.put(ox, oy, 'H'); p.put(cx - 1, cy, 'G')
    p.mend((3, ys[1]), '.')
    return p

CAVES = {
    "deep_caverns":          dict(fn=cavern, seed=301, w=64, h=40, n=7, names=("Root Hall", "Crystal Pool"), roots=True, pool=True),
    "underground_labyrinth": dict(fn=cavern, seed=302, w=72, h=46, n=10, names=("The Fork", "Cave-In Chamber"), fork=True, cavein=True),
    "siege_catacombs":       dict(fn=lambda seed, **k: catacombs(seed), seed=303),
}

if __name__ == "__main__":
    here = os.path.dirname(os.path.abspath(__file__))
    for name, kw in CAVES.items():
        kw = dict(kw); fn = kw.pop("fn"); kw.pop("names", None)
        p = fn(**kw); rows = p.rows()
        start = next((x, y) for y, row in enumerate(rows) for x, ch in enumerate(row) if ch == 'E')
        n, cut = p.check(start)
        print(name, p.w, "x", p.h, "walkable", n, "unreachable", len(cut))
        assert not cut and any('G' in row for row in rows), name
        open(os.path.join(here, f"{name}.txt"), "w").write("\n".join(rows))
