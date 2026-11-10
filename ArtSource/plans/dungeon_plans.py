# Lock-and-key redo of the small dungeons (Yan 2026-11-10: "apply the Alpha method to all dungeons"): hand-planned floors drawn from the mission graphs in dungeons/<id>.mmd
# (graph_check.py of the level-design skill), then flooded here with each key set (the text-grid check), then on the real built map by Tools/compile_check/test (PumpingChecks).
# Plan characters (LabLocks.cs): '@' a rockfall tile and '$' an iron-gate tile (symbols, NOT letters: the foundry plans use R and F as landmark marks); 1 badge door, 2 power door, 4 fuse crate, 5 breaker (needs the fuse), 6 pump (needs power), W deep water (until the pump runs).
# Rooms are plain rectangles: the first version used Plan.jag (bitten corners, alcoves), whose one-tile staircase walls the wall atlas draws as odd dark blocks (Yan: "weird textures"); the mess comes from furniture, debris and grit instead.
# Marks: E the way in, G the goal room (the outfall: daylight through a grating, NOT a second exit: a zone has one exit anchor per target), H/C/V landmarks, J/K/L stashes.
import os, sys, collections
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from foundry_plans import Plan

def pumping_basement():
    """Pumping Station Basement (T1, under the marshes' pump room). Graph: St -> Ph (the hub, the dead pumps behind windows from the control room) / St -> Vg (the fuse) -> Cr (the breaker needs it)
    -> power -> the pump panel's power door -> the pump -> the culvert's black water drains -> the outfall (goal). A hole in the wall Vg <-> Ph is the loop. Sump = a dry dead end that pays."""
    p = Plan(56, 40, 9101); r = p.r
    p.room(3, 3, 8, 7)                                   # St: the stair down
    p.hline(11, 25, 5, 2)                                         # the long gallery east to the valves
    p.room(26, 3, 21, 7)                                 # Vg: the valve gallery
    p.vline(6, 10, 11, 2); p.put(6, 10, 'D'); p.put(7, 10, 'D')   # St -> Ph
    p.room(4, 12, 31, 16)                                # Ph: the pump hall
    p.vline(41, 10, 12, 2)                                        # Vg -> Cr
    p.room(36, 13, 13, 10)                               # Cr: the control room, above the waterline
    p.room(4, 30, 8, 7)                                  # Pp: the pump panel room
    p.vline(7, 28, 29, 2); p.put(7, 28, '2'); p.put(8, 28, '2')   # its power door
    p.room(14, 30, 7, 6)                                 # Sump: a dry dead end
    p.vline(17, 28, 29, 2); p.put(17, 28, 'D')
    p.vline(28, 28, 29, 2); p.put(28, 28, 'D'); p.put(29, 28, 'D')   # the culvert's door
    p.vline(27, 29, 34, 3, 'W'); p.hline(27, 50, 33, 3, 'W')       # the culvert: black water over your head
    p.room(51, 30, 4, 8)                                 # Of: the outfall
    for x in range(35, 36):                                       # windows from the control room onto the pump hall
        for y in range(15, 19): p.g[y][x] = 'n'
    for (x, y) in ((30, 10), (31, 10), (30, 11), (31, 11)): p.g[y][x] = 'r'   # the hole knocked through, valve gallery <-> pump hall
    # --- furnishing
    for x in (8, 13, 18, 23, 28):                                  # the dead pumps
        for dx in (0, 1):
            for dy in (0, 1): p.g[16 + dy][x + dx] = 'm'
    for x in range(5, 34, 2): p.g[14][x] = 'p'
    for y in range(22, 28):
        for x in range(25, 34):
            if p.g[y][x] == '.': p.g[y][x] = 'w'                  # knee-deep along the east wall
    for x in range(28, 45, 3): p.g[4][x] = 'm'
    for x in range(27, 45, 2):
        if p.g[8][x] == '.': p.g[8][x] = 'p'
    for (x, y, ch) in ((38, 15, 'd'), (40, 15, 'd'), (44, 15, 'd'), (47, 14, 'l'), (47, 15, 'l'), (38, 21, 'c'), (39, 21, 'c'), (4, 4, 'c'), (4, 5, 'b'), (9, 8, 'c')):
        if p.g[y][x] == '.': p.g[y][x] = ch
    p.g[4][45] = '4'                                              # the fuse crate in the valve gallery
    p.g[20][47] = '5'                                             # the breaker in the control room
    p.g[33][7] = '6'                                              # the pump panel
    p.g[33][19] = 'J'; p.g[16][46] = 'K'                          # stashes: the sump's maintenance box, the control room's
    p.g[32][52] = 'L'; p.g[5][5] = 'E'; p.g[34][53] = 'G'; p.g[13][18] = 'H'; p.g[18][42] = 'C'; p.g[6][36] = 'V'
    for _ in range(26):                                           # grit: blood, grime, papers, oil, debris, lamps that don't work
        x, y = r.randrange(2, 54), r.randrange(2, 38)
        if p.g[y][x] == '.': p.g[y][x] = r.choice("*~%\";'xxbtie")
    return p

def secret_cave():
    """Secret Cave (T1, under the perimeter fields). Graph dungeons/secret_cave.mmd: M (the culvert pipe, concrete) -> P (the pool chamber: the hub, the rockfall in sight) / the Squeeze -> C (the smugglers'
    camp: the pry bar in a crate) ; P -> R (rockfall: a slab dropped on purpose; '@' tiles need the pry bar) -> D (deep gallery) -> N (the stash nook, the goal). Loops: a dug tunnel C -> M, a crawl
    C -> D. W = a wet dead end that pays. Natural chambers (discs) and wandering tunnels, walls of cliff rock (not a built slab)."""
    p = Plan(48, 42, 9301); r = p.r
    p.room(3, 3, 14, 4, ',')                                       # M: the culvert pipe's concrete run
    p.disc(23, 13, 5)                                              # P: the pool chamber (hub)
    p.hline(16, 19, 5, 2); p.vline(19, 5, 10, 2)                   # the pipe broke into the cave here
    p.vline(23, 18, 25, 1); p.hline(17, 23, 25, 1); p.vline(17, 25, 28, 1)   # the squeeze: one at a time
    p.disc(13, 29, 4)                                              # C: the smugglers' camp
    p.hline(28, 34, 12, 3)                                         # east out of the pool: the tunnel the rockfall fills
    p.disc(39, 15, 4)                                              # D: the deep gallery
    p.vline(39, 19, 32, 2)
    p.disc(38, 36, 3)                                              # N: the stash nook
    p.vline(10, 7, 25, 1)                                          # the dug tunnel back to the culvert (shortcut)
    p.hline(17, 38, 31, 1)                                         # the crawl from the camp to the gallery (loop)
    p.disc(30, 22, 2)                                              # W: a wet dead end off the pool
    p.hline(26, 29, 17, 1); p.vline(29, 17, 20, 1)
    # scare corners (Yan 2026-11-10): three small niches off the long tunnels, a bigger den north of the deep gallery; a mutant waits in each (TierDungeons / DungeonLairs.cs)
    p.disc(7, 16, 1); p.hline(7, 9, 16, 1)                         # N: a niche off the dug tunnel (west side)
    p.disc(24, 35, 1); p.vline(24, 32, 34, 1)                      # S: a niche under the crawl
    p.disc(43, 24, 1); p.hline(41, 43, 24, 1)                      # T: a niche off the gallery's south tunnel (east side)
    p.disc(40, 6, 3); p.vline(40, 8, 12, 2)                        # Q: the den, north of the deep gallery
    for _ in range(2):                                             # roughen: walls with open space round them crumble (cave_plans.erode also closes cells, which cut the tunnels: only opening here)
        g = [row[:] for row in p.g]
        for y in range(2, p.h - 2):
            for x in range(2, p.w - 2):
                if p.g[y][x] == '#' and sum(p.g[y + dy][x + dx] != '#' for dy in (-1, 0, 1) for dx in (-1, 0, 1)) >= 4 and r.random() < 0.45: g[y][x] = '.'
        p.g = g
    for y in range(11, 16): p.g[y][30] = '@' if 12 <= y <= 14 else '#'                       # the rockfall, three tiles across the tunnel (in sight from the pool)
    for y in range(9, 18):
        for x in range(19, 28):
            if p.g[y][x] == '.' and (x - 23) ** 2 + (y - 13) ** 2 < 14: p.g[y][x] = 'w'   # knee-deep water in the pool
    for y in range(20, 25):
        for x in range(28, 33):
            if p.g[y][x] == '.': p.g[y][x] = 'w'
    for (x, y) in ((6, 4), (9, 4), (12, 4)): p.g[y][x] = "'"       # puddles in the pipe
    p.g[4][5] = 'E'; p.g[36][38] = 'G'
    p.g[29][12] = '4'                                              # the crate with the pry bar
    p.g[30][14] = '&'; p.g[27][11] = 'x'; p.g[28][15] = 'b'        # bones, debris, a barrel in the camp
    p.g[36][37] = 'K'; p.g[22][30] = 'J'                           # stashes: the nook, the wet dead end
    p.g[36][39] = 'c'; p.g[37][38] = 'c'
    p.g[16][7] = 'N'; p.g[35][24] = 'S'; p.g[24][43] = 'T'; p.g[6][40] = 'Q'   # landmarks of the scare corners and the den
    for (x, y) in ((39, 5), (41, 7), (40, 4), (38, 7)): p.g[y][x] = '&'   # bones in the den
    for _ in range(14):
        x, y = r.randrange(2, 46), r.randrange(2, 40)
        if p.g[y][x] == '.': p.g[y][x] = r.choice("kkv&")          # boulders, roots, bones
    return p

CAVE_GIVES = {'4': ('pry', None)}
CAVE_LOCKS = {'@': 'pry'}
def cave_check(p):
    st = flood_stages(p, CAVE_GIVES, CAVE_LOCKS); g = p.g; out = []
    G, C4, R0 = pos(g, 'G'), pos(g, '4'), pos(g, '@')
    s0 = st[0][1]; last = st[-1][1]
    ok(out, C4 in s0 and R0 not in s0, "at the start: the camp (the pry bar) is reachable; the rockfall tile is not")
    ok(out, G in s0, "the stash nook is reachable with NO key (the crawl from the camp is the loop round the rockfall)")
    ok(out, R0 in last, "with the pry bar: the rockfall can be passed")
    # the direct way (P -> R -> D): block the crawl and the nook needs the pry bar
    saved = {}
    for x in range(17, 37): saved[(x, 31)] = g[31][x]; g[31][x] = '#'
    nocrawl = flood_stages(p, CAVE_GIVES, CAVE_LOCKS); first = next((sorted(k) for k, seen in nocrawl if G in seen), None)
    ok(out, first == ['pry'], f"without the crawl the nook needs {first}: the rockfall is the real gate on the direct way")
    for (x, y), ch in saved.items(): g[y][x] = ch
    # the dug tunnel to the culvert is a loop, not a requirement
    saved = {(9, y): g[y][9] for y in range(8, 25)}
    for (x, y) in saved: g[y][x] = '#'
    for (x, y) in [(10, y) for y in range(8, 25)]: saved[(x, y)] = g[y][x]; g[y][x] = '#'
    ok(out, G in flood_stages(p, CAVE_GIVES, CAVE_LOCKS)[-1][1], "closing the dug tunnel: the nook is still reachable")
    for (x, y), ch in saved.items(): g[y][x] = ch
    s0 = flood_stages(p, CAVE_GIVES, CAVE_LOCKS)[0][1]
    ok(out, all(pos(g, c) in s0 for c in 'NST'), "the three scare niches (N, S, T) are reachable with no key")
    ok(out, pos(g, 'Q') in s0, "the den (Q) is reachable with no key (it is optional: the nook never needs it)")
    return out

def smugglers_tunnel():
    """Smugglers' Tunnel (T1, from the depot's loading office). Graph dungeons/smugglers_tunnel.mmd. Yan (2026-11-10): dungeons run TOP TO BOTTOM, not left to right: the hatch room is at the top, the main
    tunnel runs south, the iron gate ('$' x2) crosses the 2-wide NORTH-SOUTH leg (so it is seen from the front, as the 3/4 view draws it), the den and then the cache room (goal) are at the bottom. The
    guard post (west, off the tunnel) holds the gate key ('4'); a side cache east; a bypass the crews dug from the guard post round the gate to the cross tunnel is the loop; a dug-out nook off the den."""
    p = Plan(40, 44, 9401); r = p.r
    p.room(16, 2, 9, 6, ',')                                       # the hatch room (concrete), top centre
    p.vline(20, 8, 18, 2)                                          # the main tunnel runs south
    p.room(6, 10, 9, 7); p.hline(14, 19, 13, 2)                    # Gd: the guard post, west of the tunnel
    p.room(27, 10, 7, 6); p.hline(22, 26, 12, 1)                   # Sc: a side cache, east of the tunnel
    p.vline(20, 19, 24, 2)                                         # the leg the gate crosses
    p.hline(8, 34, 25, 2)                                          # beyond the gate: the cross tunnel
    p.vline(10, 17, 24, 1)                                         # the crews' bypass: guard post -> the cross tunnel (the loop)
    p.room(14, 28, 13, 6); p.vline(20, 27, 27, 2)                  # Dn: the smugglers' den
    p.vline(20, 34, 36, 2); p.room(14, 37, 13, 5)                  # Cr: the cache room (goal), at the bottom
    p.room(30, 29, 5, 4); p.hline(27, 29, 30, 1)                   # Dd: a dug-out nook off the den
    for x in (20, 21): p.g[21][x] = '$'                            # the iron gate, across the 2-wide leg
    for (x, y, ch) in ((7, 11, 'c'), (8, 11, 'c'), (12, 11, 'b'), (7, 15, 'd'), (12, 15, 'c'), (28, 11, 'c'), (31, 14, 'c'), (31, 11, 'b'),
                       (15, 29, 'c'), (16, 29, 'c'), (25, 32, 'l'), (22, 31, 'd'), (15, 38, 'c'), (17, 38, 'c'), (25, 40, 'l'), (18, 3, 'c'), (23, 6, 'b')):
        if p.g[y][x] == '.': p.g[y][x] = ch
    p.g[12][10] = '4'                                              # the guard post's footlocker: the gate key
    p.g[12][30] = 'J'; p.g[40][24] = 'K'; p.g[31][32] = 'L'        # stashes: the side cache, the cache room, the dug-out nook
    p.g[4][20] = 'E'; p.g[40][20] = 'G'; p.g[31][20] = 'M'         # the way in, the cache room's landmark, the den's
    for _ in range(18):
        x, y = r.randrange(2, 38), r.randrange(2, 42)
        if p.g[y][x] == '.': p.g[y][x] = r.choice("*~%\";'xtb")
    return p

SMUG_GIVES = {'4': ('gate', None)}
SMUG_LOCKS = {'$': 'gate'}
def smugglers_check(p):
    st = flood_stages(p, SMUG_GIVES, SMUG_LOCKS); g = p.g; out = []
    G, K, M, F0 = pos(g, 'G'), pos(g, '4'), pos(g, 'M'), pos(g, '$')
    s0 = st[0][1]
    ok(out, K in s0 and F0 not in s0, "at the start: the guard post (the gate key) is reachable; the gate itself is a wall")
    ok(out, G in s0 and M in s0, "the den and the cache room are reachable with NO key (through the crews' bypass, which is the loop round the gate)")
    saved = {}
    for y in range(17, 25): saved[(10, y)] = g[y][10]; g[y][10] = '#'
    nob = flood_stages(p, SMUG_GIVES, SMUG_LOCKS); first = next((sorted(k) for k, seen in nob if G in seen), None)
    ok(out, first == ['gate'] and G not in nob[0][1], f"with the bypass blocked the cache room needs {first}: the gate is the real gate on the direct way")
    for (x, y), ch in saved.items(): g[y][x] = ch
    ok(out, all(pos(g, c) in s0 for c in 'JL'), "the side cache and the dug-out nook are reachable with no key")
    return out

def flood_stages(p, gives, locks, start='E', blocked_extra=''):
    """Walk the plan from E picking up keys until nothing changes. gives: {plan char: (key, needs or None)}; locks: {plan char: key needed to pass that tile}.
    '#' and 'n' are walls. Returns [(keys held, tiles reachable), ...]."""
    g = p.g; H, W = len(g), len(g[0]); sx, sy = next((x, y) for y in range(H) for x in range(W) if g[y][x] == start)
    keys = set(); stages = []
    def walk(keys):
        seen = {(sx, sy)}; q = [(sx, sy)]
        while q:
            x, y = q.pop()
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                n = (x + dx, y + dy)
                if n in seen or not (0 <= n[0] < W and 0 <= n[1] < H): continue
                ch = g[n[1]][n[0]]
                if ch in '#n': continue
                if ch in locks and locks[ch] not in keys: continue
                seen.add(n); q.append(n)
        return seen
    while True:
        seen = walk(keys); stages.append((set(keys), seen)); new = set(keys)
        for (x, y) in seen:
            ch = g[y][x]
            if ch in gives and (gives[ch][1] is None or gives[ch][1] in keys): new.add(gives[ch][0])
        if new == keys: break
        keys = new
    return stages

def pos(g, ch): return next((x, y) for y, row in enumerate(g) for x, c in enumerate(row) if c == ch)
def ok(out, c, msg): out.append(("PASS " if c else "FAIL ") + msg)

PUMP_GIVES = {'4': ('fuse', None), '5': ('power', 'fuse'), '6': ('drain', 'power')}
PUMP_LOCKS = {'2': 'power', 'W': 'drain'}
def pumping_check(p):
    st = flood_stages(p, PUMP_GIVES, PUMP_LOCKS); g = p.g; out = []
    E, G, F, B, P6 = pos(g, 'E'), pos(g, 'G'), pos(g, '4'), pos(g, '5'), pos(g, '6')
    s0 = st[0][1]; last = st[-1][1]
    ok(out, F in s0 and B in s0 and G not in s0 and P6 not in s0, "at the start: the fuse crate and the breaker are reachable; the pump panel and the outfall are not")
    ok(out, G in last and P6 in last, f"with every key found: the pump and the outfall are reachable ({len(st)} stages, keys: {sorted(st[-1][0])})")
    first = {}
    for keys, seen in st:
        for name, c in (('pump panel', P6), ('outfall', G)):
            if c in seen and name not in first: first[name] = sorted(keys)
    ok(out, first.get('pump panel') == ['fuse', 'power'] and first.get('outfall') == ['drain', 'fuse', 'power'], f"order: pump panel needs {first.get('pump panel')}, outfall needs {first.get('outfall')}")
    hole = ((30, 10), (31, 10), (30, 11), (31, 11)); saved = [g[y][x] for (x, y) in hole]
    for (x, y) in hole: g[y][x] = '#'
    ok(out, G in flood_stages(p, PUMP_GIVES, PUMP_LOCKS)[-1][1], "closing the hole in the wall: the outfall is still reachable (the hole is only a loop)")
    for (x, y), ch in zip(hole, saved): g[y][x] = ch
    return out

PLANS = {"pumping_basement": pumping_basement, "secret_cave": secret_cave, "smugglers_tunnel": smugglers_tunnel}
CHECKS = {"pumping_basement": pumping_check, "secret_cave": cave_check, "smugglers_tunnel": smugglers_check}
if __name__ == "__main__":
    here = os.path.dirname(os.path.abspath(__file__)); bad = 0
    for name, fn in PLANS.items():
        p = fn(); res = CHECKS[name](p) if name in CHECKS else []
        for line in res: print(name, line); bad += line.startswith("FAIL")
        open(os.path.join(here, name + ".txt"), "w").write("\n".join("".join(row) for row in p.g))
        print("\n".join("".join(row) for row in p.g))
    sys.exit(1 if bad else 0)
