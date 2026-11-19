#!/usr/bin/env python3
"""The Hatch Tunnels v2 (Tier 2 beat 1 "Taken", STORY.md; Yan, 2026-11-16: "not an actual maze but a hard to navigate place, underground tunnels", in the spirit of a classic Zone underground (our own design);
"the corridor is too thin, make it twice as large, and at the end of the corridor a broken wall leading down another dark tunnel, that's where the mutants come from"; "the door doesn't feel like a lab door").
Writes Assets/Scripts/GameCore/TierDungeons.TakenPlan.cs.   python3 ArtSource/plans/taken_tunnels.py     (env SEED / SEARCH=<n seeds> to pick the longest way)
What it makes: ~16 rooms (caves and old concrete halls) joined by winding tunnels 3-5 wide (a spanning tree plus a few loops, so there are dead ends AND more than one way, but no grid); a 6-wide concrete corridor along the
north with a steel lab door (the badge-reader door, '1') in the middle of its north wall; the corridor's east end is a BROKEN WALL (rubble) opening on a dark tunnel and the nest cavern, which the maze never touches.
Legend: # rock, . rough floor, , concrete floor, + a timber prop, h a block, ? a wreck, E the hatch ladder, F the lab door's spot, B the broken wall, H the nest, 1 the badge-reader door."""
import random, os, math, sys
from collections import deque
W, H = 96, 58   # (the plan is 150 wide since 2026-11-19: the arena)
def build(seed):
    rnd = random.Random(seed); g = [['#'] * W for _ in range(H)]
    def put(x, y, c):
        if 1 <= x < W - 1 and 1 <= y < H - 1: g[y][x] = c
    def disc(cx, cy, r, c='.'):
        for y in range(int(cy - r) - 1, int(cy + r) + 2):
            for x in range(int(cx - r) - 1, int(cx + r) + 2):
                if (x - cx) ** 2 + (y - cy) ** 2 <= r * r: put(x, y, c)
    def blob(cx, cy, r, c='.'):                                          # an irregular cave room: a disc with lobes
        disc(cx, cy, r, c)
        for _ in range(5):
            a = rnd.random() * 6.283; d = r * rnd.uniform(0.5, 0.9); disc(cx + math.cos(a) * d, cy + math.sin(a) * d, r * rnd.uniform(0.45, 0.7), c)
    def tunnel(a, b, width, c='.'):                                      # a winding tunnel: waypoints pushed sideways, a round brush
        (x0, y0), (x1, y1) = a, b; n = max(2, int(math.hypot(x1 - x0, y1 - y0) / 9)); pts = [a]
        for i in range(1, n):
            t = i / n; nx, ny = -(y1 - y0), (x1 - x0); l = math.hypot(nx, ny) or 1; off = rnd.uniform(-8, 8)
            pts.append((x0 + (x1 - x0) * t + nx / l * off, y0 + (y1 - y0) * t + ny / l * off))
        pts.append(b)
        for (px, py), (qx, qy) in zip(pts, pts[1:]):
            steps = int(math.hypot(qx - px, qy - py) * 2) + 1
            for s in range(steps + 1):
                t = s / steps; w = width / 2 + rnd.choice((0, 0, 0.4, -0.3)); disc(px + (qx - px) * t, py + (qy - py) * t, max(1.2, w), c)
    # --- the corridor (6 wide), the door, the broken wall, the dark tunnel and the nest
    for y in range(6, 12):
        for x in range(6, 81): g[y][x] = ','
    g[5][40] = '1'; g[7][40] = 'F'
    for lx in range(10, 79, 6): g[8][lx] = 'i'                          # ceiling lights: white in the corridor, red when the alarm goes (Tier2Story); the caves have none (flashlights only)
    for y in range(6, 12): g[y][81] = '.'                                # the broken wall: rubble where the wall was
    for (x, y) in [(81, 6), (81, 11), (80, 7), (80, 10)]: g[y][x] = 'h'
    g[8][81] = 'h'; g[9][81] = '.'
    tunnel((82, 8), (88, 14), 4); tunnel((88, 14), (90, 24), 5); blob(88, 30, 7); blob(78, 24, 0.1) if False else None
    g[9][80] = 'B'; g[30][88] = 'H'
    # --- the rooms of the underground (kept west of x = 76 so the nest side stays apart)
    nodes = []; tries = 0
    ladder = (72, 52); nodes.append(ladder)
    while len(nodes) < 17 and tries < 4000:
        tries += 1; p = (rnd.randint(8, 72), rnd.randint(18, 54))
        if all(math.hypot(p[0] - q[0], p[1] - q[1]) >= 12 for q in nodes): nodes.append(p)
    for i, (x, y) in enumerate(nodes):
        concrete = (rnd.random() < 0.35) and False; r = rnd.uniform(3.2, 5.6) if i else 3.5
        blob(x, y, r, ',' if concrete else '.')
    # --- a spanning tree (nearest-first from a random start order, so it winds), plus a few loops
    order = nodes[1:]; rnd.shuffle(order)                                # grow the tree in random order, each new room off one of its nearest neighbours: it winds and doubles back
    inn = [ladder]; edges = []
    for q in order:
        near = sorted(inn, key=lambda p: math.hypot(p[0] - q[0], p[1] - q[1]))[:2]; p = rnd.choice(near)
        edges.append((p, q)); inn.append(q)
    for _ in range(int(os.environ.get('LOOPS', '1'))):
        a, b = rnd.sample(nodes, 2)
        if math.hypot(a[0] - b[0], a[1] - b[1]) < 30: edges.append((a, b))
    for a, b in edges: tunnel(a, b, rnd.choice((3, 3, 4, 4, 5)), ',' if (rnd.random() < 0.3 and False) else '.')
    # the corridor's west end bends down into the nearest room to the north-west
    nw = min(nodes, key=lambda p: math.hypot(p[0] - 8, p[1] - 20))
    for y in range(6, 12):
        for x in range(4, 8): g[y][x] = ','
    tunnel((5, 10), nw, 4, ',')
    # --- dressing: props in the rooms (never in the tunnels' middle), a few wrecks and blocks
    for (x, y) in nodes[1:]:
        for _ in range(rnd.randint(1, 3)):
            px, py = x + rnd.randint(-3, 3), y + rnd.randint(-3, 3)
            if 0 < px < W - 1 and 0 < py < H - 1 and g[py][px] in '.,': g[py][px] = rnd.choice('+h+h')
    ex, ey = ladder; g[ey][ex] = 'E'
    for yy in range(ey - 1, ey + 2):
        for xx in range(ex - 1, ex + 2):
            if g[yy][xx] in '+h?': g[yy][xx] = '.'
    rows = [''.join(r) for r in g]
    # --- guidance without words (level-design skill: two independent cues per critical decision): the way to the lab was kept up - the old service route is concrete underfoot (',') where every
    # side cave stays bare rock ('.'), a leading line from the ladder to the corridor. Painted from the shortest way, 3 wide.
    wk = lambda c: c in '.,EFBH'
    par = {ladder: None}; qq = deque([ladder])
    while qq:
        x, y = qq.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            n = (x + dx, y + dy)
            if 0 <= n[0] < W and 0 <= n[1] < H and wk(rows[n[1]][n[0]]) and n not in par: par[n] = (x, y); qq.append(n)
    cur = (6, 9); route = []
    while cur is not None and cur in par:
        route.append(cur)
        for yy in range(cur[1] - 1, cur[1] + 2):
            for xx in range(cur[0] - 1, cur[0] + 2):
                if g[yy][xx] == '.': g[yy][xx] = ','
        cur = par[cur]
    rows = [''.join(r) for r in g]
    # --- 2026-11-19 (Tier 3 beat 4 "The Door Under the Dock", STORY_GAUNT_LAB.md): the plan grows EAST into the Elder Gaunt's arena (the nest's cavern, enlarged for the boss fight), and NORTH behind the lab
    # door into a short concrete corridor with a decontamination gate and a loading bay whose freight lift ('L') is the way down to the Annex. A separate random stream, drawn after everything else, so the
    # Taken part of the map (seed 926) is untouched: the first 96 columns and the rows below y = 5 are exactly as they were.
    W2 = 150
    for r_ in g: r_.extend(['#'] * (W2 - len(r_)))
    r2 = random.Random(seed * 31 + 7)
    def put2(x, y, c):
        if 1 <= x < W2 - 1 and 1 <= y < H - 1: g[y][x] = c
    def disc2(cx, cy, r, c='.'):
        for y in range(int(cy - r) - 1, int(cy + r) + 2):
            for x in range(int(cx - r) - 1, int(cx + r) + 2):
                if (x - cx) ** 2 + (y - cy) ** 2 <= r * r: put2(x, y, c)
    def blob2(cx, cy, r, c='.'):
        disc2(cx, cy, r, c)
        for _ in range(6):
            a = r2.random() * 6.283; d = r * r2.uniform(0.5, 0.85); disc2(cx + math.cos(a) * d, cy + math.sin(a) * d, r * r2.uniform(0.4, 0.65), c)
    def path2(a, b, width, c='.'):
        steps = int(math.hypot(b[0] - a[0], b[1] - a[1]) * 2) + 1
        for s_ in range(steps + 1):
            t = s_ / steps; disc2(a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, width / 2 + r2.choice((0, 0, 0.3)), c)
    path2((90, 30), (108, 30), 6)                                               # the nest's east side opens on the arena
    blob2(126, 30, 14); blob2(126, 30, 11)
    for (lx, ly, lr) in ((112, 18, 6), (112, 42, 6), (138, 19, 6), (138, 41, 6), (141, 30, 6)): blob2(lx, ly, lr)
    path2((126, 17), (120, 9), 4); blob2(120, 6, 4)                              # a side tunnel north: a wave comes out of it
    path2((126, 43), (120, 51), 4); blob2(120, 53, 4)                            # ... and one south
    path2((141, 30), (146, 30), 4)                                               # the dais: a pocket at the east end, where the Elder waits
    for k in range(9):                                                           # large boulders: cover (Yan: "maybe large boulders to use as cover?"). Single boulder tiles ('k') in clumps of two or three, ringed round the centre
        a = k * 6.283 / 9 + 0.35; px_, py_ = int(126 + math.cos(a) * 8.5), int(30 + math.sin(a) * 7.5)
        for (dx_, dy_) in ((0, 0), (1, 0), (0, 1))[:r2.choice((2, 3, 3))]:
            if g[py_ + dy_][px_ + dx_] == '.': g[py_ + dy_][px_ + dx_] = 'k'
    for (px_, py_) in ((118, 26), (134, 34), (130, 22), (122, 38), (116, 33), (136, 27), (128, 36), (121, 21)):
        for (dx_, dy_) in ((0, 0), (1, 0), (1, 1))[:r2.choice((1, 2, 2))]:
            if g[py_ + dy_][px_ + dx_] == '.': g[py_ + dy_][px_ + dx_] = 'k'
    g[30][126] = 'A'; g[9][120] = 'J'; g[51][120] = 'K'; g[30][145] = 'X'
    rows = [''.join(r_) for r_ in g]
    route.reverse(); trail = []                                            # a blood trail to follow (Yan: "in the cavern only flashlights and a blood splatter trail"): a smear every 3rd tile, a splat every 12th, drifting off the centre line
    for i, (x, y) in enumerate(route):
        if i % 3 == 0 and i > 6 and rows[y][x] in ',.': trail.append((x + rnd.choice((-1, 0, 0, 1)), y + rnd.choice((-1, 0, 0, 1)), 2 if i % 12 == 0 else 1))
    trail = [(x, y, k) for x, y, k in trail if rows[y][x] in ',.']
    # a way from the ladder to the corridor's west end (BFS over walkable tiles)
    walk = lambda c: c in '.,EFB+h?H' and c not in '+h?'
    start = ladder; goal = (6, 9); dist = {start: 0}; q = deque([start])
    while q:
        x, y = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            n = (x + dx, y + dy)
            if 0 <= n[0] < W and 0 <= n[1] < H and walk(rows[n[1]][n[0]]) and n not in dist: dist[n] = dist[(x, y)] + 1; q.append(n)
    blocked = {(x, y) for x in range(6, 81) for y in range(6, 12)}        # the corridor itself: the nest side must stay unreachable without it
    seen = {start}; q = deque([start])
    while q:
        x, y = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            n2 = (x + dx, y + dy)
            if 0 <= n2[0] < W and 0 <= n2[1] < H and walk(rows[n2[1]][n2[0]]) and n2 not in seen and n2 not in blocked: seen.add(n2); q.append(n2)
    reach_nest = any((x, y) in seen for x in range(81, 95) for y in range(6, 36))
    # --- behind the lab door (2026-11-19): ten rows are added at the TOP of the plan (everything moves down by SHIFT; the blood trail with it) and the annex corridor, a decontamination gate and the loading
    # bay are carved there. (Walls are drawn tall: a corridor squeezed behind the old north wall would have been hidden by it; the bay is 7 rows tall for the same reason.)
    SHIFT = 10
    g = [['#'] * W2 for _ in range(SHIFT)] + g
    def put3(x, y, c):
        if 1 <= x < W2 - 1 and 1 <= y < len(g) - 1: g[y][x] = c
    for y in range(7, 15):                                                     # the corridor north from the door (the door is at (40, 15) now): 3 wide, 8 long
        for x in range(39, 42): put3(x, y, ',')
    for y in range(2, 7):                                                      # a compact loading zone: 11 x 5, a lift platform at the far end
        for x in range(35, 46): put3(x, y, ',')
    for (x, y) in ((36, 3), (37, 3), (36, 4), (44, 3), (44, 4), (43, 6)): put3(x, y, 'c')   # crates along the walls
    for (x, y) in ((35, 6), (45, 6)): put3(x, y, 'b')                          # barrels
    for (x, y) in ((40, 10), (40, 4)): put3(x, y, 'i')
    put3(40, 2, 'L'); put3(40, 5, 'M')
    rows = [''.join(r_) for r_ in g]
    trail = [(x, y + SHIFT, k) for (x, y, k) in trail]
    build.trail = trail
    return rows, dist.get(goal), len(dist), reach_nest
if __name__ == '__main__':
    best = None
    for sd in range(int(os.environ.get('SEARCH', '1')) if 'SEED' not in os.environ else 1):
        seed = int(os.environ.get('SEED', sd + 1)); rows, d, n, rn = build(seed)
        if d and not rn and (best is None or d > best[0]): best = (d, seed, rows, n, build.trail)
    d, seed, rows, n, trail = best
    out = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Assets', 'Scripts', 'GameCore', 'TierDungeons.TakenPlan.cs')
    with open(out, 'w') as f:
        f.write('// GENERATED by ArtSource/plans/taken_tunnels.py (seed %d) - the Hatch Tunnels (Tier 2 beat 1 "Taken"): underground tunnels and rooms, a 6-wide corridor, the lab door, the broken wall and the nest.\nusing System.Collections.Generic;\nusing GameData;\nusing static GameCore.GroundType;\n\nnamespace GameCore\n{\n    public static partial class TierDungeons\n    {\n        public static readonly string[] TakenPlan =\n        {\n' % seed)
        for r in rows: f.write('            "%s",\n' % r)
        f.write('        };\n\n        /// <summary>The blood trail along the way to the lab: x, y, kind (1 smear, 2 splat), flattened.</summary>\n        public static readonly int[] TakenTrail = { ' + ', '.join(str(v) for t in trail for v in t) + ' };\n')
        f.write('''
        /// <summary>The underground under the Loading Dock: 'E' the hatch ladder (the way back up), 'F' the lab door's spot, 'B' the broken wall, 'H' the nest. Entered by the story (Tier2Story), not by an exit.</summary>
        public static MapLayout TakenTunnels()
        {
            var marks = new Dictionary<char, (string, string)> { ['E'] = ("Hatch Ladder", "industrial_basin"), ['F'] = ("Lab Door", null), ['B'] = ("Broken Wall", null), ['H'] = ("Nest", null), ['A'] = ("Gaunt Arena", null), ['J'] = ("Arena North Mouth", null), ['K'] = ("Arena South Mouth", null), ['X'] = ("Gaunt Dais", null), ['M'] = ("Loading Bay", null), ['L'] = ("Freight Lift", "loading_annex") };
            var m = FloorPlan("taken_tunnels", TakenPlan, "cliff", CaveFloor, marks, null);
            for (int i = 0; i + 2 < TakenTrail.Length; i += 3) if (m.Looks != null && TakenTrail[i + 1] < m.Looks.Length && m.Looks[TakenTrail[i + 1]][TakenTrail[i]] == null) m.Looks[TakenTrail[i + 1]][TakenTrail[i]] = TakenTrail[i + 2] == 2 ? "blood" : "bloodtrail";
            if (m.Looks != null && m.Tiles != null)                                         // behind the lab door: concrete walls, not cave rock (only the walls that touch the corridor and the loading zone; the rock behind stays rock)
                for (int yy = 1; yy <= 15; yy++) for (int xx = 33; xx <= 47; xx++) if (m.Tiles[yy][xx] == "Solid" && (m.Tiles[yy - 1][xx] != "Solid" || m.Tiles[yy + 1][xx] != "Solid" || m.Tiles[yy][xx - 1] != "Solid" || m.Tiles[yy][xx + 1] != "Solid")) m.Looks[yy][xx] = "concrete";
            return m;
        }
    }
}
''')
    print('seed', seed, 'size', W, 'x', H, '; the way from the ladder to the corridor:', d, 'tiles; reachable floor', n)
