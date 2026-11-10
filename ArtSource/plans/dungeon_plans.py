# Lock-and-key redo of the small dungeons (Yan 2026-11-10: "apply the Alpha method to all dungeons"): hand-planned floors drawn from the mission graphs in dungeons/<id>.mmd
# (graph_check.py of the level-design skill), then flooded here with each key set (the text-grid check), then on the real built map by Tools/compile_check/test (PumpingChecks).
# Plan letters (LabLocks.cs): 1 badge door, 2 power door, 4 fuse crate, 5 breaker (needs the fuse), 6 pump (needs power), W deep water (until the pump runs).
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

def flood_stages(p, start='E'):
    """Walk the plan from E picking up keys until nothing changes: 4 -> fuse, 5 (needs the fuse) -> power, 6 (needs power) -> drain; 2 needs power, W needs drain, 1 badge."""
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
                if ch == '1' and 'badge' not in keys: continue
                if ch == '2' and 'power' not in keys: continue
                if ch == 'W' and 'drain' not in keys: continue
                seen.add(n); q.append(n)
        return seen
    while True:
        seen = walk(keys); stages.append((set(keys), seen)); new = set(keys)
        for (x, y) in seen:
            ch = g[y][x]
            if ch == '3': new.add('badge')
            if ch == '4': new.add('fuse')
            if ch == '5' and 'fuse' in keys: new.add('power')
            if ch == '6' and 'power' in keys: new.add('drain')
        if new == keys: break
        keys = new
    return stages

def pumping_check(p):
    st = flood_stages(p); g = p.g
    pos = lambda ch: next((x, y) for y, row in enumerate(g) for x, c in enumerate(row) if c == ch)
    E, G, F, B, P6 = pos('E'), pos('G'), pos('4'), pos('5'), pos('6')
    s0 = st[0][1]; last = st[-1][1]; out = []
    ok = lambda c, msg: out.append(("PASS " if c else "FAIL ") + msg)
    ok(F in s0 and B in s0 and G not in s0 and P6 not in s0, "at the start: the fuse crate and the breaker are reachable; the pump panel and the outfall are not")
    ok(G in last and P6 in last, f"with every key found: the pump and the outfall are reachable ({len(st)} stages, keys: {sorted(st[-1][0])})")
    first = {}
    for keys, seen in st:
        for name, c in (('pump panel', P6), ('outfall', G)):
            if c in seen and name not in first: first[name] = sorted(keys)
    ok(first.get('pump panel') == ['fuse', 'power'] and first.get('outfall') == ['drain', 'fuse', 'power'], f"order: pump panel needs {first.get('pump panel')}, outfall needs {first.get('outfall')}")
    # the hole is a loop, never a requirement: close it and the goal is still reachable
    saved = [g[y][x] for (x, y) in ((30, 10), (31, 10), (30, 11), (31, 11))]
    for (x, y) in ((30, 10), (31, 10), (30, 11), (31, 11)): g[y][x] = '#'
    last2 = flood_stages(p)[-1][1]; ok(G in last2, "closing the hole in the wall: the outfall is still reachable (the hole is only a loop)")
    for (x, y), ch in zip(((30, 10), (31, 10), (30, 11), (31, 11)), saved): g[y][x] = ch
    rooms_open = sum(1 for row in g for c in row if c in '.,wD')
    ok(rooms_open > 600, f"{rooms_open} open floor tiles")
    return out

PLANS = {"pumping_basement": pumping_basement}
if __name__ == "__main__":
    here = os.path.dirname(os.path.abspath(__file__)); bad = 0
    for name, fn in PLANS.items():
        p = fn(); res = pumping_check(p) if name == "pumping_basement" else []
        for line in res: print(name, line); bad += line.startswith("FAIL")
        open(os.path.join(here, name + ".txt"), "w").write("\n".join("".join(row) for row in p.g))
        print("\n".join("".join(row) for row in p.g))
    sys.exit(1 if bad else 0)
