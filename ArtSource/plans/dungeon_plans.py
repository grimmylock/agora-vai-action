# Lock-and-key redo of the small dungeons (Yan 2026-11-10: "apply the Alpha method to all dungeons"): hand-planned floors drawn from the mission graphs in dungeons/<id>.mmd
# (graph_check.py of the level-design skill), then flooded here with each key set (the text-grid check), then on the real built map by Tools/compile_check/test (PumpingChecks).
# Plan characters (LabLocks.cs): '(' a heavy blast door, '!' a steel door ('D' is a wooden one), '@' a rockfall tile, '$' an iron-gate tile and '^' a sealed-slab tile (symbols, NOT letters: the foundry plans use R and F as landmark marks); 1 badge door, 2 power door, 4 fuse crate, 5 breaker (needs the fuse), 6 pump (needs power), W deep water (until the pump runs).
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
    p.vline(6, 10, 11, 2); p.put(6, 10, '!'); p.put(7, 10, '!')   # St -> Ph (steel doors)
    p.room(4, 12, 31, 16)                                # Ph: the pump hall
    p.vline(41, 10, 12, 2)                                        # Vg -> Cr
    p.room(36, 13, 13, 10)                               # Cr: the control room, above the waterline
    p.room(4, 30, 8, 7)                                  # Pp: the pump panel room
    p.vline(7, 28, 29, 2); p.put(7, 28, '2'); p.put(8, 28, '2')   # its power door
    p.room(14, 30, 7, 6)                                 # Sump: a dry dead end
    p.vline(17, 28, 29, 2); p.put(17, 28, '!')
    p.vline(28, 28, 29, 2); p.put(28, 28, '!'); p.put(29, 28, '!')   # the culvert's door (steel)
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

def chapel_crypt():
    """Chapel Crypt (T1, under the chapel). Graph dungeons/chapel_crypt.mmd: the stair -> the burial passage (the hub: the iron gate and, beyond it, the sealed slab are in sight down the passage)
    -> the ossuary (the ledger '3': the vault code) -> the priest's cell (the strongbox '4': the gate key) ; the gate ('$' x2) across the passage -> the bone gallery -> the slab ('^' x2, needs the code)
    -> the family vault (goal). A hole from the ossuary into the gallery is the loop; the collapsed side crypt off the gallery is a dead end that pays. Both locks cross NORTH-SOUTH legs (seen from the front)."""
    p = Plan(40, 44, 9501); r = p.r
    p.room(16, 2, 8, 6, ',')                                       # St: the stair landing
    p.vline(19, 8, 25, 2, ',')                                     # Pa: the burial passage, south from the landing (to the gallery: the gate is across it)
    for y in (9, 12, 15, 18, 21):                                  # burial niches either side
        p.room(15, y, 3, 2, ','); p.room(22, y, 3, 2, ',')
    p.room(4, 10, 10, 10, ','); p.hline(14, 18, 14, 2, ',')        # Os: the ossuary, west
    p.room(4, 3, 6, 5, ','); p.vline(7, 8, 9, 1, ',')              # Pc: the priest's cell, north of the ossuary
    p.room(6, 26, 28, 4, ',')                                      # Bg: the bone gallery, a long cross passage beyond the gate
    p.vline(10, 20, 25, 1, ','); p.g[24][10] = 'r'; p.g[25][10] = 'r'   # the hole from the ossuary into the gallery (rubble)
    p.room(28, 32, 7, 6, ','); p.vline(31, 30, 31, 2, ',')         # Cs: the collapsed side crypt (a dead end)
    p.room(13, 35, 14, 7, ','); p.vline(19, 30, 34, 2, ',')        # V: the family vault (goal), at the bottom
    for x in (19, 20): p.g[24][x] = '$'                            # the iron gate across the passage
    for x in (19, 20): p.g[32][x] = '^'                            # the sealed slab across the way to the vault
    for x in (6, 8, 10, 12): p.g[11][x] = 'k' if x == 12 else 'x'  # fallen bones and stone in the ossuary
    for (x, y, ch) in ((5, 16, 'h'), (7, 16, 'h'), (11, 13, 'h'), (5, 4, 'c'), (9, 4, 'b'), (8, 27, 'h'), (14, 28, 'h'), (24, 27, 'h'), (30, 28, 'h'),
                       (29, 34, 'k'), (33, 36, 'x'), (14, 37, 'h'), (25, 37, 'h'), (14, 40, 'c')):
        if p.g[y][x] == ',': p.g[y][x] = ch
    p.g[12][6] = '3'                                               # the burial ledger in the ossuary: the vault code
    p.g[5][6] = '4'                                                # the priest's strongbox: the gate key
    p.g[34][33] = 'J'; p.g[40][24] = 'K'                           # stashes: the collapsed crypt, the vault
    p.g[4][19] = 'E'; p.g[39][19] = 'G'; p.g[15][8] = 'O'          # the way in, the vault's landmark, the ossuary's
    for (x, y) in ((17, 10), (23, 13), (16, 19), (24, 22), (11, 28), (29, 29)): p.g[y][x] = '&'    # bones
    for _ in range(14):
        x, y = r.randrange(2, 38), r.randrange(2, 42)
        if p.g[y][x] == ',': p.g[y][x] = r.choice("*~%\";'xxt")
    return p

CRYPT_GIVES = {'3': ('code', None), '4': ('gate', None)}
CRYPT_LOCKS = {'$': 'gate', '^': 'code'}
def crypt_check(p):
    st = flood_stages(p, CRYPT_GIVES, CRYPT_LOCKS); g = p.g; out = []
    G, L3, L4, GATE, SEAL = pos(g, 'G'), pos(g, '3'), pos(g, '4'), pos(g, '$'), pos(g, '^')
    s0 = st[0][1]; last = st[-1][1]
    ok(out, L3 in s0 and L4 in s0 and GATE not in s0 and SEAL not in s0, "at the start: the ledger (code) and the strongbox (gate key) are reachable; the gate and the slab are walls")
    ok(out, G in last, f"with every key found the vault is reachable ({len(st)} stages, keys {sorted(st[-1][0])})")
    ok(out, G not in s0, "the vault is NOT reachable with no key (the slab holds even when the hole lets you into the gallery)")
    first = next((sorted(k) for k, seen in st if G in seen), None)
    ok(out, first == ['code', 'gate'], f"the vault needs {first} (the code, and the gate key or the hole)")
    # the hole is a loop for the GATE only: block it and the gallery needs the gate key
    saved = {(10, y): g[y][10] for y in range(20, 26)}
    for (x, y) in saved: g[y][x] = '#'
    nohole = flood_stages(p, CRYPT_GIVES, CRYPT_LOCKS); gal = pos(g, '&') if False else (14, 28)
    first_g = next((sorted(k) for k, seen in nohole if gal in seen), None)
    ok(out, gal not in nohole[0][1] and first_g is not None and 'gate' in first_g, f"without the hole the bone gallery is closed at the start and opens once the gate key is held (keys then: {first_g}): the gate is the real gate on the direct way")
    for (x, y), ch in saved.items(): g[y][x] = ch
    ok(out, pos(g, 'J') in flood_stages(p, CRYPT_GIVES, CRYPT_LOCKS)[-1][1], "the collapsed side crypt is reachable once the gallery is")
    return out

def valley_mine():
    """Valley Mine (T2, off Black Valley's rim). Graph dungeons/valley_mine.mmd: the adit (bottom) -> the main gallery (north-south, ore rails down the middle; the collapse '@' x5 across it is in sight)
    -> the tool shed (the pry bar '4') / the flooded drift (a cache on the far ledge) ; collapse -> the deep drift (the gallery's north half) -> the miners' rest (goal, the strongbox). Loops: a crawl
    from the flooded drift into the north half, and a long ventilation crawl from the miners' rest back down to the adit. The collapse crosses a north-south gallery (seen from the front)."""
    p = Plan(44, 46, 9601); r = p.r
    p.room(18, 40, 8, 5, ',')                                      # Ad: the adit
    p.vline(20, 12, 39, 5)                                         # Mg: the main gallery (and, north of the collapse, the deep drift)
    p.room(8, 30, 8, 8); p.hline(16, 19, 33, 2)                    # Ts: the tool shed, west
    p.room(28, 28, 13, 11); p.hline(25, 27, 33, 2)                 # Fd: the flooded drift, east
    p.vline(38, 18, 27, 1); p.hline(25, 38, 17, 1)                 # the crawl from the drift into the gallery's north half (loop)
    p.room(14, 3, 17, 7); p.vline(20, 10, 11, 5)                   # Mr: the miners' rest (goal), at the head of the gallery
    p.hline(5, 13, 5, 1); p.vline(5, 5, 42, 1); p.hline(5, 17, 42, 1)   # the ventilation crawl: miners' rest -> back down to the adit (loop)
    for x in range(20, 25): p.g[22][x] = '@'                       # the collapse, across the whole gallery
    p.g[40][5] = '@'                                               # a second fall in the ventilation crawl, near the adit: the long way back is only open to someone with the pry bar (else it would walk you round the collapse from the adit)
    for y in range(13, 40): p.g[y][22] = '|' if p.g[y][22] == '.' else p.g[y][22]   # the ore rails down the middle
    for y in (15, 19, 27, 31, 35):
        for x in (20, 24):
            if p.g[y][x] == '.': p.g[y][x] = '+'                   # timber props
    for y in range(31, 39):                                        # the flooded drift: knee-deep, with a dry ledge along the north
        for x in range(28, 41):
            if p.g[y][x] == '.': p.g[y][x] = 'w'
    for (x, y, ch) in ((9, 31, 'c'), (10, 31, 'c'), (14, 31, 'b'), (9, 36, 'l'), (14, 36, 'x'), (11, 34, 'd'),
                       (21, 41, 'c'), (23, 43, 'b'), (16, 5, 'd'), (18, 5, 'd'), (28, 4, 'l'), (29, 7, 'c'), (15, 8, 'c'), (22, 14, 'x'), (21, 30, 'k'), (23, 36, 'x')):
        if p.g[y][x] == '.': p.g[y][x] = ch
    p.g[34][10] = '4'                                              # the pry bar on the tool shed's rack
    p.g[29][39] = 'J'; p.g[5][28] = 'K'                            # stashes: the drift's far ledge, the miners' strongbox
    p.g[42][21] = 'E'; p.g[6][21] = 'G'; p.g[20][21] = 'N'; p.g[34][34] = 'L'   # the way in, the miners' rest, the main gallery, the flooded drift
    for (x, y) in ((24, 18), (20, 26), (22, 33), (23, 11), (13, 33)): p.g[y][x] = '&'
    for _ in range(16):
        x, y = r.randrange(2, 42), r.randrange(2, 44)
        if p.g[y][x] == '.': p.g[y][x] = r.choice("*~%\";'xk")
    return p

MINE_GIVES = {'4': ('pry', None)}
MINE_LOCKS = {'@': 'pry'}
def mine_check(p):
    st = flood_stages(p, MINE_GIVES, MINE_LOCKS); g = p.g; out = []
    G, T, C0, J, N = pos(g, 'G'), pos(g, '4'), pos(g, '@'), pos(g, 'J'), pos(g, 'N')
    s0 = st[0][1]; last = st[-1][1]
    ok(out, T in s0 and J in s0 and C0 not in s0, "at the start: the tool shed (the pry bar) and the flooded drift's ledge are reachable; the collapse is a wall")
    ok(out, G in s0, "the miners' rest is reachable with NO key (the crawl from the flooded drift is the loop round the collapse)")
    ok(out, C0 in last, "with the pry bar the collapse can be passed")
    saved = {(38, y): g[y][38] for y in range(18, 28)}
    for x in range(25, 38): saved[(x, 17)] = g[17][x]
    for (x, y) in saved: g[y][x] = '#'
    nocrawl = flood_stages(p, MINE_GIVES, MINE_LOCKS); first = next((sorted(k) for k, seen in nocrawl if G in seen), None)
    ok(out, first == ['pry'] and G not in nocrawl[0][1], f"without the drift crawl the miners' rest needs {first}: the collapse is the real gate on the main way")
    for (x, y), ch in saved.items(): g[y][x] = ch
    vs = {(5, y): g[y][5] for y in range(6, 42)}
    for (x, y) in vs: g[y][x] = '#'
    ok(out, G in flood_stages(p, MINE_GIVES, MINE_LOCKS)[-1][1], "closing the ventilation crawl changes nothing (it is a loop, not a requirement)")
    for (x, y), ch in vs.items(): g[y][x] = ch
    return out

def crusher_pit():
    """Crusher Pit (T2, under Crusher Row). Graph dungeons/crusher_pit.mmd (the goal is the PAY VAULT behind the dead blast door, not an adit: the zone has no second exit): the pit ladder -> the ram hall (the hub: the
    stopped rams, the dead power door in sight) -> the service tunnel -> the oil sump (a nest; the foreman's locker '3' = the badge) ; the tunnel's badge door ('1' x2) -> the foreman's cage (the spares crate '4' =
    the fuse) -> the control booth (the ram breaker '5', needs the fuse = power) ; the power door ('2' x2) in the ram hall's east wall -> the pay vault (goal). A hole from the sump up into the ram hall is the loop.
    The Alpha locks: badge and power."""
    p = Plan(56, 44, 9701); r = p.r
    p.room(3, 3, 7, 6, ',')                                        # Pl: the pit ladder
    p.hline(10, 11, 5, 2)
    p.room(12, 4, 29, 16)                                          # Rh: the ram hall
    p.vline(26, 20, 25, 2)                                         # Sv: the service tunnel, south from the hall
    p.hline(26, 41, 26, 2)                                         # and east, to the cage
    p.room(12, 28, 19, 13)                                         # Os: the oil sump
    p.room(42, 21, 11, 11)                                         # Fc: the foreman's cage
    p.room(42, 34, 11, 8); p.vline(47, 32, 33, 2)                  # Cb: the control booth, south of the cage
    p.hline(41, 45, 11, 2); p.room(46, 6, 8, 11)                   # Pv: the pay vault, behind the power door in the hall's east wall
    p.vline(16, 20, 27, 2); p.g[20][16] = p.g[20][17] = 'r'; p.g[27][16] = p.g[27][17] = 'r'   # the hole from the sump up into the ram hall (the loop)
    for y in (26, 27): p.g[y][34] = '1'                            # the badge door in the service tunnel
    for y in (11, 12): p.g[y][41] = '2'                            # the dead blast door: power
    for x0 in (16, 22, 30, 36):                                    # the rams: 2 x 3 machine blocks standing in the hall
        for dx in (0, 1):
            for dy in (0, 1, 2): p.g[7 + dy][x0 + dx] = 'm'; p.g[13 + dy][x0 + dx] = 'm'
    for y in range(17, 20):
        for x in range(13, 40):
            if p.g[y][x] == '.': p.g[y][x] = 'w'                  # black water along the pit's south edge
    for y in range(32, 40):
        for x in range(14, 30):
            if p.g[y][x] == '.': p.g[y][x] = 'w'                  # the sump: oil and water
    for (x, y, ch) in ((13, 29, 'b'), (14, 29, 'b'), (29, 30, 'b'), (14, 5, 'c'), (13, 18, 'x'), (38, 5, 'c'), (39, 18, 'x'), (44, 22, 'd'), (51, 22, 'l'), (51, 30, 'c'), (44, 36, 'd'), (51, 40, 'l'),
                       (48, 8, 'c'), (50, 8, 'c'), (52, 14, 'l'), (4, 4, 'c'), (8, 7, 'b')):
        if p.g[y][x] == '.': p.g[y][x] = ch
    p.g[38][13] = '3'                                              # the foreman's locker, at the sump's far west end
    p.g[24][44] = '4'                                              # the spares crate in the cage
    p.g[38][50] = '5'                                              # the ram breaker in the control booth
    p.g[30][28] = 'J'; p.g[38][44] = 'L'; p.g[8][51] = 'K'         # stashes: the sump, the booth, the pay vault
    p.g[5][6] = 'E'; p.g[11][50] = 'G'; p.g[10][26] = 'H'; p.g[34][21] = 'O'; p.g[26][47] = 'C'
    for _ in range(18):
        x, y = r.randrange(2, 54), r.randrange(2, 42)
        if p.g[y][x] == '.': p.g[y][x] = r.choice("*~%\";'xtb")
    return p

PIT_GIVES = {'3': ('badge', None), '4': ('fuse', None), '5': ('power', 'fuse')}
PIT_LOCKS = {'1': 'badge', '2': 'power'}
def pit_check(p):
    st = flood_stages(p, PIT_GIVES, PIT_LOCKS); g = p.g; out = []
    G, L3, C4, B5 = pos(g, 'G'), pos(g, '3'), pos(g, '4'), pos(g, '5')
    s0 = st[0][1]; last = st[-1][1]
    ok(out, L3 in s0 and C4 not in s0 and B5 not in s0 and G not in s0, "at the start: the foreman's locker (the badge) is reachable; the cage, the breaker and the pay vault are not")
    ok(out, G in last, f"with every key found the pay vault is reachable ({len(st)} stages, keys {sorted(st[-1][0])})")
    firsts = {}
    for keys, seen in st:
        for name, c in (('cage', C4), ('breaker', B5), ('pay vault', G)):
            if c in seen and name not in firsts: firsts[name] = sorted(keys)
    ok(out, firsts.get('cage') == ['badge'] and firsts.get('breaker') == ['badge'] and firsts.get('pay vault') == ['badge', 'fuse', 'power'], f"order: the cage and the breaker's room need {firsts.get('cage')}; the pay vault needs {firsts.get('pay vault')} (the breaker only throws with the fuse)")
    hole = [(x, y) for y in range(20, 28) for x in (16, 17)]; saved = {(x, y): g[y][x] for (x, y) in hole}
    for (x, y) in hole: g[y][x] = '#'
    ok(out, L3 in flood_stages(p, PIT_GIVES, PIT_LOCKS)[0][1] and G in flood_stages(p, PIT_GIVES, PIT_LOCKS)[-1][1], "closing the hole in the wall: the locker and the pay vault are still reachable (the hole is only a loop)")
    for (x, y), ch in saved.items(): g[y][x] = ch
    return out

def flooded_bunker():
    """Flooded Bunker (T1, behind the marsh blast door; the story bunker, its way in is the zone key 'flooded_bunker' from the Headman's Strongbox: Tier1Story). Graph dungeons/flooded_bunker.mmd: the blast
    door room -> the decontamination lock -> the main corridor (the hub: the badge door east and the dead power door south are in sight) -> the barracks (the duty locker '3' = badge; a dead-end mess south of
    it) ; the badge door ('1' x2) -> the armory (the fuse crate '4') -> the generator room (the switch '5' needs the fuse = power) ; the power door ('2' x2) -> the flooded command level (the Command Safe,
    goal). The hatch ladder from the command level up to the corridor is the loop; it has its own power door, so from the hub it opens only after the power is on."""
    p = Plan(48, 48, 9801); r = p.r
    p.room(20, 2, 8, 5, ',')                                       # Bd: the blast door room
    p.vline(23, 7, 8, 2); p.room(20, 9, 8, 6)                      # Dl: the decontamination lock (a door each side)
    for x in (23, 24): p.g[7][x] = '('; p.g[15][x] = '!'   # the heavy blast door off the marsh, then a steel door out of the decon lock
    p.vline(23, 15, 17, 2); p.hline(4, 44, 18, 3)                  # the main corridor (the hub), east-west
    p.room(4, 22, 10, 10); p.vline(8, 21, 21, 2)                   # Ba: the barracks, west
    p.room(4, 35, 10, 8); p.vline(8, 32, 34, 2)                    # Ms: the mess (a dead end)
    p.room(35, 22, 11, 10); p.vline(40, 21, 21, 2)                 # Ar: the armory, east, behind the badge door
    for x in (40, 41): p.g[21][x] = '1'
    p.room(37, 35, 9, 8); p.vline(41, 32, 34, 2)                   # Gn: the generator room, south of the armory
    p.room(17, 29, 17, 16); p.vline(23, 21, 28, 2)                 # Cm: the command level, flooded, behind the power door
    for x in (23, 24): p.g[28][x] = '2'
    p.vline(18, 21, 28, 1); p.g[21][18] = '2'                      # the hatch ladder up to the corridor: its own power door at the top (the loop)
    for y in range(38, 45):
        for x in range(17, 34):
            if p.g[y][x] == '.': p.g[y][x] = 'w'                  # the command level's lower half is under water
    for (x, y, ch) in ((5, 23, 'c'), (5, 25, 'c'), (12, 23, 'l'), (12, 27, 'l'), (9, 28, 'c'), (5, 36, 'c'), (12, 36, 'b'), (7, 40, 'd'), (11, 40, 'c'),
                       (36, 23, 'l'), (38, 23, 'l'), (44, 28, 'c'), (36, 29, 'c'), (39, 37, 'm'), (44, 40, 'm'), (38, 41, 'c'), (4, 4, 'c'), (21, 11, 'l'), (26, 11, 'l'),
                       (19, 31, 'd'), (21, 31, 'd'), (31, 31, 'l'), (28, 33, 'm'), (24, 35, 'x'), (19, 41, 'x')):
        if p.g[y][x] == '.': p.g[y][x] = ch
    for (x, y) in ((18, 35), (23, 36), (30, 36)): p.g[y][x] = 'k'   # pillars of fallen concrete in the water
    p.g[24][6] = '3'                                               # the duty locker in the barracks
    p.g[24][43] = '4'                                              # the fuse crate in the armory
    p.g[39][44] = '5'                                              # the generator switch
    p.g[41][7] = 'J'; p.g[30][31] = 'L'; p.g[27][44] = 'K'         # stashes: the mess, the command safe (the goal), the armory's loot
    p.g[4][24] = 'E'; p.g[26][8] = 'B'; p.g[26][40] = 'A'; p.g[34][24] = 'C'; p.g[38][41] = 'Q'; p.g[24][18] = 'H'
    for _ in range(20):
        x, y = r.randrange(2, 46), r.randrange(2, 46)
        if p.g[y][x] == '.': p.g[y][x] = r.choice("*~%\";'xtb")
    return p

BUNKER_GIVES = {'3': ('badge', None), '4': ('fuse', None), '5': ('power', 'fuse')}
BUNKER_LOCKS = {'1': 'badge', '2': 'power'}
def bunker_check(p):
    st = flood_stages(p, BUNKER_GIVES, BUNKER_LOCKS); g = p.g; out = []
    L3, C4, S5, GOAL = pos(g, '3'), pos(g, '4'), pos(g, '5'), pos(g, 'L')
    s0 = st[0][1]; last = st[-1][1]
    ok(out, L3 in s0 and C4 not in s0 and S5 not in s0 and GOAL not in s0, "at the start: the duty locker (badge) is reachable; the armory, the generator switch and the command safe are not")
    ok(out, GOAL in last, f"with every key found the command safe is reachable ({len(st)} stages, keys {sorted(st[-1][0])})")
    firsts = {}
    for keys, seen in st:
        for name, c in (('armory', C4), ('switch', S5), ('safe', GOAL)):
            if c in seen and name not in firsts: firsts[name] = sorted(keys)
    ok(out, firsts.get('armory') == ['badge'] and firsts.get('switch') == ['badge'] and firsts.get('safe') == ['badge', 'fuse', 'power'], f"order: the armory and the generator room need {firsts.get('armory')}; the command safe needs {firsts.get('safe')}")
    hl = {(18, y): g[y][18] for y in range(21, 29)}
    for (x, y) in hl: g[y][x] = '#'
    ok(out, GOAL in flood_stages(p, BUNKER_GIVES, BUNKER_LOCKS)[-1][1], "closing the hatch ladder: the command safe is still reachable (it is only a loop)")
    for (x, y), ch in hl.items(): g[y][x] = ch
    # the hatch ladder must not be a way round the power door: with no power the command level stays shut even with the ladder open
    ok(out, pos(g, 'C') not in {c for k, seen in st if 'power' not in k for c in seen}, "before the power is on, the command level is unreachable even by the hatch ladder")
    return out

def roomy(p, x, y, floor='.,'):
    """Open floor on all eight sides: a blocker here (debris, a barrel, a boulder) can never plug a one-tile passage."""
    return all(p.g[y + dy][x + dx] in floor + "*~%\";'&:`" for dy in (-1, 0, 1) for dx in (-1, 0, 1))

def scatter(p, n, lo=2, floor='.', what="*~%\";'xtb"):
    r = p.r
    for _ in range(n):
        x, y = r.randrange(lo, p.w - lo), r.randrange(lo, p.h - lo)
        if p.g[y][x] == floor:
            ch = r.choice(what)
            if ch in "xtbk" and not roomy(p, x, y): ch = '%'
            p.g[y][x] = ch

def props(p, items):
    for (x, y, ch) in items:
        if p.g[y][x] in '.,': p.g[y][x] = ch

def stranded_ferry():
    """The Stranded Ferry (T3, the dried lake). Graph dungeons/stranded_ferry.mmd: the car deck (wrecks) -> the passenger lounge (the hub: the engine room's card reader and the dead bridge door both lead off it)
    -> the cabin row (the purser's cabin: his locker '3' = the badge) ; the badge door ('1' x2, on the stair down) -> the engine room (bilge water) -> the spares locker (the fuse '4') and the generator flat
    (the switch '5' needs the fuse = power) ; the power door ('2' x2, on the companionway up) -> the captain's bridge (goal, the Captain's Safe). The loop: a rusted-through deck plate from the car deck's west
    end down to the cabin corridor (both before any lock, so it is no way round one)."""
    p = Plan(60, 40, 9811)
    p.room(3, 3, 38, 10, ',')                                      # Cd: the car deck
    p.room(45, 3, 11, 8)                                           # Br: the captain's bridge
    p.room(24, 16, 16, 10); p.vline(30, 13, 15, 2)                 # Pl: the passenger lounge, down a stair from the car deck
    p.hline(3, 23, 20, 2)                                          # Cb: the cabin corridor, west of the lounge
    for x0 in (5, 10, 15):
        p.room(x0, 15, 4, 4); p.g[19][x0 + 1] = 'D'                # cabins north of the corridor
        p.room(x0, 23, 4, 4); p.g[22][x0 + 2] = 'D'                # and south
    p.vline(3, 13, 19, 1); p.g[13][3] = 'r'; p.g[19][3] = 'r'      # the rusted-through deck plate: car deck <-> the corridor's west end (the loop)
    p.room(14, 29, 30, 8); p.vline(30, 26, 28, 2)                  # Er: the engine room, down a stair from the lounge
    for x in (30, 31): p.g[27][x] = '1'                            # the badge door on that stair
    p.room(48, 29, 8, 7); p.hline(44, 47, 32, 2)                   # Sg: the spares locker, east of the engines
    p.room(3, 30, 8, 6); p.hline(11, 13, 32, 2)                    # Gs: the generator flat, west of the engines
    p.hline(40, 49, 20, 2); p.vline(48, 11, 19, 2)                 # the companionway from the lounge east and up to the bridge
    for x in (48, 49): p.g[14][x] = '2'                            # the dead bridge door
    for y in (35, 36):
        for x in range(14, 44):
            if p.g[y][x] == '.': p.g[y][x] = 'w'                  # the bilge: black lake water along the engine room's low side
    for x0 in (7, 13, 19, 25, 34): p.g[5][x0] = '?'; p.g[10][x0 + 2] = '?'      # the last cars, still chained down
    for x0 in (18, 26, 36):                                        # the engines: 2 x 3 blocks
        for dx in (0, 1):
            for dy in (0, 1, 2): p.g[30 + dy][x0 + dx] = '['
    props(p, ((10, 7, 'c'), (22, 8, 't'), (28, 6, 'b'), (16, 11, 'x'),
              (26, 17, '>'), (28, 17, '>'), (34, 17, '>'), (36, 17, '>'), (26, 24, '>'), (28, 24, '>'), (36, 24, '>'), (37, 19, 'd'), (37, 20, 'd'), (25, 21, 'x'),
              (8, 15, 'l'), (11, 16, 'c'), (18, 15, 'l'), (5, 26, 'l'), (13, 25, 'c'), (16, 26, 'l'), (8, 24, 'x'),
              (46, 4, 'o'), (49, 4, 'o'), (52, 4, 'o'), (54, 8, 'd'), (46, 9, 'l'),
              (15, 30, 'b'), (42, 30, 'b'), (23, 33, 'x'), (40, 33, 'c'), (49, 30, 'l'), (51, 30, 'l'), (54, 34, 'c'), (49, 34, 'c'), (4, 31, 'm'), (8, 31, 'm'), (9, 34, 'b')))
    p.g[16][6] = '3'                                               # the purser's locker, in the cabin at the corridor's west end
    p.g[31][53] = '4'                                              # the fuse, in the spares locker
    p.g[33][5] = '5'                                               # the generator switch
    p.g[25][11] = 'J'; p.g[7][54] = 'K'; p.g[34][33] = 'L'         # stashes: a cabin locker, the captain's safe (the goal), the engine room
    p.g[8][38] = 'E'; p.g[21][32] = 'P'; p.g[33][30] = 'R'; p.g[6][50] = 'G'
    scatter(p, 22)
    return p

BP_GIVES = {'3': ('badge', None), '4': ('fuse', None), '5': ('power', 'fuse')}
BP_LOCKS = {'1': 'badge', '2': 'power'}
def badge_power_check(p, loop=None, loop_name="the loop", goal='G', badge_rooms=('4', '5'), gives=BP_GIVES, locks=BP_LOCKS, first_need=('badge',)):
    """The Alpha chain on a plan: the locker ('3') is reachable at the start, the fuse crate and the switch only with the badge (`first_need`), the goal only with the power; closing `loop` (tiles) keeps the goal reachable."""
    st = flood_stages(p, gives, locks); g = p.g; out = []
    L3, GOAL = pos(g, '3'), pos(g, goal); s0 = st[0][1]; last = st[-1][1]
    ok(out, L3 in s0 and GOAL not in s0 and all(pos(g, c) not in s0 for c in badge_rooms), "at the start: the badge locker is reachable; the fuse, the switch and the goal are not")
    ok(out, GOAL in last, f"with every key found the goal is reachable ({len(st)} stages, keys {sorted(st[-1][0])})")
    firsts = {}
    for keys, seen in st:
        for c in list(badge_rooms) + [goal]:
            if pos(g, c) in seen and c not in firsts: firsts[c] = sorted(keys)
    ok(out, all(firsts.get(c) == list(first_need) for c in badge_rooms) and 'power' in (firsts.get(goal) or []), f"order: fuse and switch need {[firsts.get(c) for c in badge_rooms]}; the goal needs {firsts.get(goal)}")
    if loop:
        saved = {(x, y): g[y][x] for (x, y) in loop}
        for (x, y) in loop: g[y][x] = '#'
        st2 = flood_stages(p, gives, locks)
        ok(out, L3 in st2[0][1] and GOAL in st2[-1][1], f"closing {loop_name}: the locker and the goal are still reachable (it is only a loop)")
        for (x, y), ch in saved.items(): g[y][x] = ch
    ok(out, all(GOAL not in seen for keys, seen in st if 'power' not in keys), "without the power the goal is unreachable by any route")
    return out

def ferry_check(p): return badge_power_check(p, loop=[(3, y) for y in range(13, 20)], loop_name="the rusted deck plate")

def camp_shelter():
    """Camp Shelter (T4, under the summer camp). Graph dungeons/camp_shelter.mmd: the stair and the blast door -> the decontamination lock -> the central hall (the hub: the dead command door is straight ahead)
    -> the dormitory west (the warden's locker '3' = the badge), the stores east (a dead end), the mess down the west passage ; the mess's badge door ('1' x2) -> the filter room (the fuse '4') -> the generator
    room (the switch '5' needs the fuse = power) ; the power door ('2' x2) south of the hall -> the shelter command (goal, the Shelter Safe). The loop: a vent crawl between the dormitory and the mess."""
    p = Plan(48, 54, 9821)
    p.room(21, 2, 6, 4, ','); p.vline(23, 6, 7, 2)                 # St: the stair
    p.room(18, 8, 12, 6); p.vline(23, 14, 16, 2)                   # Dc: the decontamination lock
    for x in (23, 24): p.g[6][x] = '('; p.g[15][x] = '!'
    p.hline(4, 43, 17, 3)                                          # the central hall
    p.vline(8, 20, 22, 2); p.room(4, 23, 10, 8)                    # Dm: the dormitory
    p.vline(15, 20, 38, 2); p.hline(14, 14, 37, 2); p.room(4, 34, 10, 8)   # Ms: the mess, down the west passage
    p.vline(6, 31, 33, 1, 'r')                                     # the vent crawl dormitory <-> mess (the loop)
    p.vline(8, 42, 45, 2); p.room(4, 46, 12, 6)                    # Fr: the filter room, behind the badge door
    for x in (8, 9): p.g[43][x] = '1'
    p.room(20, 45, 13, 7); p.hline(16, 19, 48, 2)                  # Gr: the generator room
    p.vline(23, 20, 27, 2); p.room(19, 28, 14, 12)                 # Cp: the shelter command, behind the power door
    for x in (23, 24): p.g[23][x] = '2'
    p.vline(39, 20, 22, 2); p.room(35, 23, 10, 9)                  # the stores (a dead end)
    props(p, ((19, 9, 'l'), (28, 9, 'l'), (19, 12, 'c'),
              (4, 23, 'l'), (6, 23, 'l'), (11, 23, 'l'), (13, 23, 'l'), (4, 26, 'c'), (13, 26, 'c'), (11, 29, 'c'), (13, 29, 'l'),
              (6, 36, 'd'), (9, 36, 'd'), (12, 36, 'd'), (6, 39, 'd'), (9, 39, 'd'), (12, 40, 'b'), (4, 35, 't'),
              (5, 47, ']'), (8, 47, ']'), (11, 47, ']'), (5, 50, 'p'), (6, 50, 'p'), (7, 50, 'p'), (14, 47, 'c'),
              (22, 46, 'm'), (26, 46, 'm'), (22, 50, 'm'), (31, 50, 'b'), (28, 50, 'x'),
              (20, 29, 'o'), (23, 29, 'o'), (28, 29, 'f'), (31, 29, 'f'), (22, 34, 'd'), (25, 34, 'd'), (28, 34, 'd'), (20, 38, 'l'), (25, 37, '>'),
              (36, 24, 'c'), (38, 24, 'c'), (43, 24, 'l'), (36, 30, 'b'), (40, 27, 'c'), (41, 27, 'c'), (36, 27, 'x')))
    p.g[29][5] = '3'                                               # the warden's locker in the dormitory
    p.g[50][13] = '4'                                              # the fuse, with the filter spares
    p.g[47][30] = '5'                                              # the generator switch
    p.g[30][43] = 'J'; p.g[38][31] = 'K'; p.g[40][5] = 'L'         # stashes: the stores, the shelter safe (the goal), the mess
    p.g[3][23] = 'E'; p.g[26][9] = 'B'; p.g[38][9] = 'M'; p.g[49][9] = 'F'; p.g[49][26] = 'Q'; p.g[33][26] = 'G'
    scatter(p, 22)
    return p
def shelter_check(p): return badge_power_check(p, loop=[(6, y) for y in range(31, 34)], loop_name="the vent crawl")

def hospital_morgue():
    """Hospital Morgue (T6, under the Siege Ward hospital). Graph dungeons/hospital_morgue.mmd: the landing -> the long corridor (the dead records door is in sight beside the stair) -> the cold room and the
    autopsy theatre (the drawer '3' = the badge) ; the theatre's badge door ('1' x2) -> the service corridor -> the boiler room (the boilerman's locker '4' = the fuse; the boiler switch '5' needs it = power) ;
    the power door ('2' x2) -> the records room (goal, the Records Safe). The loop: a service hole between the cold room and the autopsy theatre."""
    p = Plan(48, 46, 9831)
    p.room(21, 2, 6, 5); p.vline(23, 7, 9, 2)                      # Ml: the morgue landing
    p.hline(4, 43, 10, 3)                                          # the long corridor
    p.room(34, 2, 10, 5); p.vline(38, 7, 9, 2)                     # Rs: the records room, behind the dead door
    for x in (38, 39): p.g[8][x] = '2'
    p.room(4, 16, 14, 12); p.vline(10, 13, 15, 2)                  # Cr: the cold room
    p.room(22, 16, 14, 10); p.vline(28, 13, 15, 2)                 # At: the autopsy theatre
    for x in (10, 11): p.g[14][x] = '!'
    for x in (28, 29): p.g[14][x] = 'D'
    p.hline(18, 21, 20, 1, 'r')                                    # the service hole cold room <-> theatre (the loop)
    p.vline(28, 26, 29, 2); p.hline(6, 41, 30, 2)                  # the service corridor, behind the theatre's badge door
    for x in (28, 29): p.g[27][x] = '1'
    p.room(10, 35, 28, 8); p.vline(22, 32, 34, 2)                  # Bo: the boiler room
    p.room(40, 16, 5, 8); p.vline(42, 13, 15, 1)                   # a linen store (a dead end)
    for y in range(17, 27, 2): p.g[y][4] = 'l'; p.g[y][17] = 'l'   # the drawers along the cold room's walls
    p.g[19][17] = '.'
    props(p, ((8, 20, 'a'), (12, 23, 'a'), (9, 25, 'x'), (26, 20, 'a'), (31, 20, 'a'), (23, 17, 'f'), (25, 17, 'f'), (35, 24, 'c'), (23, 24, 'j'), (33, 17, 'j'),
              (35, 3, 'f'), (37, 3, 'f'), (39, 3, 'f'), (36, 5, 'd'), (41, 5, 'd'), (22, 3, 'c'),
              (14, 36, 'm'), (15, 36, 'm'), (20, 36, 'm'), (21, 36, 'm'), (28, 36, 'm'), (29, 36, 'm'), (26, 41, 'b'), (18, 41, 'x'), (32, 40, 'c'),
              (11, 40, 'p'), (12, 40, 'p'), (13, 40, 'p'), (41, 17, 'c'), (44, 20, 'c'), (41, 22, 't'), (8, 30, 'c'), (36, 31, 't'), (16, 11, '>'), (33, 11, 't')))
    p.g[17][34] = '3'                                              # the autopsy drawer with the pathologist's badge
    p.g[41][36] = '4'                                              # the boilerman's locker: the fuse
    p.g[36][11] = '5'                                              # the boiler switch
    p.g[5][42] = 'K'; p.g[22][43] = 'L'                            # stashes: the records safe (the goal), the linen store
    p.g[3][23] = 'E'; p.g[22][11] = 'C'; p.g[22][29] = 'A'; p.g[39][24] = 'B'; p.g[4][39] = 'G'
    scatter(p, 20)
    return p
def morgue_check(p): return badge_power_check(p, loop=[(x, 20) for x in range(18, 22)], loop_name="the service hole")

def drydock_tunnels():
    """Dry Dock Tunnels (T5, under the dock's pump house). Graph dungeons/drydock_tunnels.mmd: the pump house stair -> the workmen's niche (the fuse '4') and the pump gallery (the hub: the pump panel '6',
    which needs the fuse and gives the drain, and the flooded main culvert in sight) ; the culvert ('W', over your head until the pump runs) -> the sluice chamber (the wheel handle '3' = the sluice key) ;
    the sluice gate ('$' x2) off the culvert's east side -> the fitters' store (goal). The shortcut: the service ladder from the sluice chamber back to the pump gallery, its hatch ('$') on the same handle."""
    p = Plan(50, 46, 9841)
    p.room(3, 3, 7, 5, ',')                                        # Ps: the pump house stair
    p.room(14, 3, 6, 4); p.hline(10, 13, 5, 2)                     # Wk: the workmen's niche
    p.vline(5, 8, 10, 2); p.room(3, 11, 20, 8)                     # Pg: the pump gallery
    p.hline(23, 26, 14, 2)                                         # the way to the culvert's edge
    p.room(27, 6, 5, 34, 'W')                                      # Mc: the main culvert, flooded to the roof
    p.room(36, 3, 11, 10); p.hline(32, 35, 8, 2)                   # Sc: the sluice chamber
    p.hline(32, 41, 22, 2); p.vline(40, 24, 27, 2); p.room(36, 28, 11, 10)   # Fs: the fitters' store, behind the sluice gate
    for x in (40, 41): p.g[25][x] = '$'
    p.hline(24, 35, 4, 1); p.vline(24, 4, 13, 1); p.g[9][24] = '$' # the service ladder: sluice chamber -> pump gallery (the shortcut; its hatch takes the same handle)
    p.vline(7, 19, 23, 2); p.room(3, 24, 12, 8)                    # a side drain (a dead end)
    for y in range(9, 13):
        for x in range(37, 46):
            if p.g[y][x] == '.': p.g[y][x] = 'w'                  # the sluice chamber's floor is awash
    props(p, ((5, 13, 'm'), (9, 13, 'm'), (13, 13, 'm'), (17, 13, 'm'), (4, 17, 'p'), (5, 17, 'p'), (6, 17, 'p'), (10, 17, 'p'), (11, 17, 'p'), (21, 17, 'b'),
              (15, 4, 'l'), (16, 6, 'c'), (4, 4, 'c'), (38, 4, 'm'), (44, 4, 'm'), (41, 10, 'k'),
              (37, 29, 'l'), (39, 29, 'l'), (41, 29, 'l'), (45, 36, 'c'), (43, 36, 'c'), (37, 35, 'd'), (38, 33, 'x'), (44, 32, 'c'),
              (5, 30, 'b'), (10, 26, 'c'), (12, 30, 'x'), (8, 28, '&')))
    p.g[4][18] = '4'                                               # the fuse in the workmen's niche
    p.g[12][20] = '6'                                              # the pump panel in the gallery
    p.g[5][45] = '3'                                               # the wheel handle in the sluice chamber
    p.g[29][45] = 'K'; p.g[30][4] = 'J'                            # stashes: the fitters' store (the goal), the side drain
    p.g[4][6] = 'E'; p.g[15][12] = 'P'; p.g[14][26] = 'C'; p.g[7][41] = 'U'; p.g[32][41] = 'G'
    scatter(p, 18)
    return p
DOCK_GIVES = {'4': ('fuse', None), '6': ('drain', 'fuse'), '3': ('gate', None)}
DOCK_LOCKS = {'W': 'drain', '$': 'gate'}
def drydock_check(p):
    st = flood_stages(p, DOCK_GIVES, DOCK_LOCKS); g = p.g; out = []
    F, P6, H, G = pos(g, '4'), pos(g, '6'), pos(g, '3'), pos(g, 'G'); s0 = st[0][1]
    ok(out, F in s0 and P6 in s0 and H not in s0 and G not in s0, "at the start: the fuse and the pump panel are reachable; the wheel handle and the fitters' store are not")
    ok(out, G in st[-1][1], f"with every key found the fitters' store is reachable ({len(st)} stages, keys {sorted(st[-1][0])})")
    firsts = {}
    for keys, seen in st:
        for name, c in (('handle', H), ('store', G)):
            if c in seen and name not in firsts: firsts[name] = sorted(keys)
    ok(out, firsts.get('handle') == ['drain', 'fuse'] and firsts.get('store') == ['drain', 'fuse', 'gate'], f"order: the handle needs {firsts.get('handle')}, the store {firsts.get('store')}")
    lad = [(24, y) for y in range(4, 14)]; saved = {(x, y): g[y][x] for (x, y) in lad}
    for (x, y) in lad: g[y][x] = '#'
    ok(out, G in flood_stages(p, DOCK_GIVES, DOCK_LOCKS)[-1][1], "closing the service ladder: the store is still reachable (it is only a shortcut)")
    for (x, y), ch in saved.items(): g[y][x] = ch
    ok(out, all(H not in seen for keys, seen in st if 'drain' not in keys), "before the pump runs the sluice chamber is unreachable (the ladder's hatch needs the handle from inside it)")
    return out

def maintenance_tunnels():
    """Maintenance Tunnels (T3, under the rail yards). Graph dungeons/maintenance_tunnels.mmd: the access hatch -> the tunnel junction (the hub: a card door south, a dead door north, the flooded gallery east)
    -> the foreman's locker room north-west ('3' = badge) ; the badge door ('1' x2) -> the tool crib ('4' = fuse) -> the substation (the breaker '5' needs the fuse = power) ; the power door ('2' x2) -> the
    pump house (the pump '6' needs power = drain) ; the flooded gallery ('W') -> the collapsed section -> the surface depot (goal). The shortcut: a dead door ('2' x2) between the substation and the junction."""
    p = Plan(72, 44, 9851)
    p.room(3, 19, 6, 5, ','); p.hline(9, 13, 21, 2)               # St: the access hatch
    p.room(14, 16, 14, 11)                                         # Jn: the tunnel junction
    p.room(14, 6, 7, 6); p.vline(17, 12, 15, 2)                    # Lk: the foreman's locker room
    p.vline(20, 27, 30, 2); p.room(14, 31, 14, 9)                  # Tc: the tool crib, behind the badge door
    for x in (20, 21): p.g[28][x] = '1'
    p.room(32, 32, 12, 8); p.hline(28, 31, 35, 2)                  # Ss: the substation
    p.vline(25, 10, 15, 2); p.room(24, 4, 14, 6)                   # Ph: the pump house, behind the power door
    for x in (25, 26): p.g[13][x] = '2'
    p.vline(36, 22, 31, 2); p.hline(28, 37, 22, 2)                 # the shortcut: substation -> junction, a dead door on it
    for x in (36, 37): p.g[27][x] = '2'
    p.hline(28, 33, 18, 2); p.room(34, 12, 26, 8, 'W')             # Fl: the flooded gallery
    p.hline(60, 61, 15, 2); p.room(62, 8, 7, 14)                   # the collapsed section
    p.vline(65, 22, 27, 2); p.room(56, 28, 13, 10)                 # Sd: the surface depot
    p.collapse(65, 13, 2)
    props(p, ((4, 20, 'c'), (15, 17, 'p'), (16, 17, 'p'), (17, 17, 'p'), (26, 25, 'b'), (15, 25, 'x'), (23, 17, 'c'),
              (15, 7, 'l'), (17, 7, 'l'), (19, 10, 'c'), (15, 32, 'l'), (17, 32, 'l'), (19, 32, 'l'), (26, 38, 'c'), (24, 38, 'c'), (15, 38, 'd'), (22, 35, 'x'),
              (33, 33, 'm'), (36, 33, 'm'), (39, 33, 'm'), (42, 38, 'b'), (33, 38, 'q'), (34, 38, 'q'),
              (26, 5, 'm'), (30, 5, 'm'), (34, 5, 'm'), (36, 8, 'b'), (27, 8, 'p'), (28, 8, 'p'), (29, 8, 'p'),
              (57, 29, 'c'), (59, 29, 'c'), (61, 29, 'c'), (67, 32, 'l'), (57, 36, 'b'), (62, 34, 'x'), (60, 36, 'c')))
    p.g[8][19] = '3'; p.g[33][26] = '4'; p.g[37][39] = '5'; p.g[7][36] = '6'
    p.g[36][67] = 'K'; p.g[9][63] = 'J'
    p.g[21][5] = 'E'; p.g[21][20] = 'T'; p.g[6][29] = 'P'; p.g[36][37] = 'B'; p.g[17][65] = 'C'; p.g[32][62] = 'G'
    scatter(p, 24)
    return p
MT_GIVES = {'3': ('badge', None), '4': ('fuse', None), '5': ('power', 'fuse'), '6': ('drain', 'power')}
MT_LOCKS = {'1': 'badge', '2': 'power', 'W': 'drain'}
def tunnels_check(p):
    st = flood_stages(p, MT_GIVES, MT_LOCKS); g = p.g; out = []
    c = {k: pos(g, k) for k in '3456G'}; s0 = st[0][1]
    ok(out, c['3'] in s0 and all(c[k] not in s0 for k in '456G'), "at the start: the foreman's locker; not the crib, the breaker, the pump or the depot")
    ok(out, c['G'] in st[-1][1], f"with every key found the surface depot is reachable ({len(st)} stages, keys {sorted(st[-1][0])})")
    firsts = {}
    for keys, seen in st:
        for k in '456G':
            if c[k] in seen and k not in firsts: firsts[k] = sorted(keys)
    ok(out, firsts['4'] == ['badge'] and firsts['5'] == ['badge'] and firsts['6'] == ['badge', 'fuse', 'power'] and firsts['G'] == ['badge', 'drain', 'fuse', 'power'], f"order: crib {firsts['4']}, pump {firsts['6']}, depot {firsts['G']}")
    sc = [(x, y) for x in (36, 37) for y in range(22, 32)]; saved = {(x, y): g[y][x] for (x, y) in sc}
    for (x, y) in sc: g[y][x] = '#'
    ok(out, c['G'] in flood_stages(p, MT_GIVES, MT_LOCKS)[-1][1], "closing the substation shortcut: the depot is still reachable")
    for (x, y), ch in saved.items(): g[y][x] = ch
    return out

def underground_bunker():
    """Underground Bunker (T6, under the Abandoned City). Graph dungeons/underground_bunker.mmd: the blast door foyer -> the long corridor (the hub: a card door north, a dead door south-east) -> the barracks
    ('3' = badge) and the ration stores (open; a duct joins the two) ; the badge door ('1' x2) -> air handling ('4' = fuse) -> the generator hall (the switch '5' needs the fuse = power) ; the power door
    ('2' x2) -> the armory -> the command centre (goal). The duct was air handling <-> armory in the first graph, which walked round the power door; it is barracks <-> ration stores now."""
    p = Plan(60, 48, 9861)
    p.room(4, 3, 8, 6, ','); p.vline(7, 9, 12, 2)                  # E: the blast door foyer
    for x in (7, 8): p.g[10][x] = '('
    p.hline(4, 51, 13, 3)                                          # Hl: the long corridor
    p.vline(9, 16, 19, 2); p.room(4, 20, 12, 10)                   # Bk: the barracks
    p.vline(24, 16, 19, 2); p.room(20, 20, 10, 8)                  # the ration stores
    p.hline(16, 19, 24, 1, 'r')                                    # the duct barracks <-> stores (the loop)
    p.vline(24, 9, 12, 2); p.room(18, 2, 14, 7)                    # Ah: air handling, behind the badge door
    for x in (24, 25): p.g[10][x] = '1'
    p.room(36, 2, 14, 8); p.hline(32, 35, 5, 2)                    # Gn: the generator hall
    p.vline(42, 16, 21, 2); p.room(36, 22, 14, 9)                  # Ar: the armory, behind the power door
    for x in (42, 43): p.g[18][x] = '2'
    p.vline(42, 31, 34, 2); p.room(32, 35, 22, 10)                 # Cc: the command centre
    for x in (42, 43): p.g[32][x] = '!'
    props(p, ((5, 4, 'c'), (10, 7, 'x'), (5, 21, 'l'), (7, 21, 'l'), (12, 21, 'l'), (14, 21, 'l'), (5, 25, 'c'), (14, 28, 'c'), (5, 28, 'l'), (11, 27, 'x'),
              (21, 21, 'c'), (23, 21, 'c'), (27, 21, 'c'), (21, 26, 'b'), (26, 23, 'c'), (27, 23, 'c'),
              (19, 3, ']'), (22, 3, ']'), (28, 3, ']'), (19, 7, 'p'), (20, 7, 'p'), (21, 7, 'p'), (30, 7, 'c'),
              (38, 3, 'm'), (39, 3, 'm'), (43, 3, 'm'), (44, 3, 'm'), (38, 8, 'b'), (47, 8, 'x'),
              (37, 23, 'l'), (39, 23, 'l'), (45, 23, 'l'), (37, 29, 'c'), (39, 29, 'c'), (48, 29, 'c'), (44, 27, 'x'),
              (34, 36, 'o'), (37, 36, 'o'), (40, 36, 'o'), (48, 36, 'f'), (51, 36, 'f'), (36, 40, 'd'), (39, 40, 'd'), (47, 40, 'd'), (50, 40, 'd'), (33, 43, 'l'), (44, 43, '>')))
    p.g[28][8] = '3'; p.g[4][30] = '4'; p.g[6][48] = '5'
    p.g[43][52] = 'K'; p.g[26][28] = 'J'; p.g[23][48] = 'L'
    p.g[5][7] = 'E'; p.g[25][10] = 'B'; p.g[24][25] = 'R'; p.g[5][25] = 'A'; p.g[6][43] = 'Q'; p.g[26][43] = 'M'; p.g[40][43] = 'G'
    scatter(p, 22)
    return p
def ubunker_check(p): return badge_power_check(p, loop=[(x, 24) for x in range(16, 20)], loop_name="the duct")

def roughen(p, passes=2, chance=0.45):
    """Cave walls crumble where open space surrounds them (only ever OPENS cells; keep 3+ wall tiles between spaces that must stay apart, and set lock tiles AFTER this)."""
    r = p.r
    for _ in range(passes):
        g = [row[:] for row in p.g]
        for y in range(2, p.h - 2):
            for x in range(2, p.w - 2):
                if p.g[y][x] == '#' and sum(p.g[y + dy][x + dx] != '#' for dy in (-1, 0, 1) for dx in (-1, 0, 1)) >= 4 and r.random() < chance: g[y][x] = '.'
        p.g = g

def lock_row(p, y, xs, ch, span=4):
    """A lock across a north-south leg: the tiles `xs` of row y become `ch`, everything else within `span` of them on that row is wall (so roughened walls cannot leave a way round)."""
    for x in range(min(xs) - span, max(xs) + span + 1):
        if 0 < x < p.w - 1: p.g[y][x] = ch if x in xs else '#'

def cave_litter(p, n): scatter(p, n, what="kkv&")

def deep_caverns():
    """Deep Caverns (T4, under the Crimson Woods' sinkhole). Graph dungeons/deep_caverns.mmd: the surface shaft -> the Root Hall (the hub: the rockfall east, the black lake south) -> the climbers' camp
    (the pry bar '4') ; the rockfall ('@' x3) -> the old dam (the sluice lever '6' = drain) -> a grotto (a dead end that pays) ; the lower lake ('W', over your head until the dam is opened) -> the Crystal
    Pool on its island (goal). The loop: a chimney from the dam back up to the surface shaft, choked by a second fall ('@') the same bar lifts."""
    p = Plan(64, 46, 9871)
    p.disc(8, 9, 3)                                                # E: the surface shaft
    p.hline(11, 18, 9, 2); p.disc(25, 14, 7)                       # Cv: the Root Hall
    p.vline(20, 19, 28, 1); p.hline(13, 20, 28, 1); p.disc(10, 30, 4)   # Cp: the climbers' camp
    p.hline(32, 41, 13, 3); p.disc(47, 13, 5)                      # the tunnel the rockfall fills, then Dm: the old dam
    p.vline(8, 2, 6, 1); p.hline(8, 47, 2, 1); p.vline(47, 2, 8, 1)     # the chimney: dam -> shaft
    p.vline(47, 18, 31, 1); p.hline(47, 50, 31, 1); p.disc(53, 32, 3)   # a grotto south of the dam
    p.vline(25, 21, 26, 2); p.disc(30, 34, 8)                      # the lower lake
    roughen(p)
    for y in range(25, 44):
        for x in range(20, 41):
            if p.g[y][x] == '.' and (x - 30) ** 2 + (y - 34) ** 2 <= 76 and y >= 27: p.g[y][x] = 'W'
    for y in range(32, 37):
        for x in range(28, 33):
            if (x - 30) ** 2 + (y - 34) ** 2 <= 5: p.g[y][x] = '.'     # the island
    for y in range(11, 18): p.g[y][37] = '@' if 13 <= y <= 15 else '#'   # the rockfall across the east tunnel
    lock_row(p, 5, [47], '@', span=3)                              # the fall in the chimney
    for (x, y) in ((22, 10), (27, 9), (29, 13), (21, 16), (28, 18), (24, 12), (19, 13), (30, 16)):
        if p.g[y][x] == '.': p.g[y][x] = 'v'                       # the hanging roots
    props(p, ((9, 28, 'c'), (12, 31, 'b'), (8, 31, '&'), (11, 28, 'x'), (45, 11, 'h'), (46, 11, 'h'), (47, 11, 'h'), (48, 11, 'h'), (49, 11, 'h'), (44, 15, 'k'), (52, 33, 'k'), (54, 30, '&')))
    p.g[30][11] = '4'; p.g[14][50] = '6'
    p.g[33][54] = 'J'; p.g[35][30] = 'K'
    p.g[9][8] = 'E'; p.g[14][25] = 'R'; p.g[31][9] = 'B'; p.g[13][45] = 'M'; p.g[34][30] = 'G'
    cave_litter(p, 16)
    return p
DC_GIVES = {'4': ('pry', None), '6': ('drain', None)}
DC_LOCKS = {'@': 'pry', 'W': 'drain'}
def caverns_check(p):
    st = flood_stages(p, DC_GIVES, DC_LOCKS); g = p.g; out = []
    P4, L6, G = pos(g, '4'), pos(g, '6'), pos(g, 'G'); s0 = st[0][1]
    ok(out, P4 in s0 and L6 not in s0 and G not in s0, "at the start: the climbers' camp (the pry bar); not the dam or the crystal pool")
    firsts = {}
    for keys, seen in st:
        for name, c in (('dam', L6), ('pool', G)):
            if c in seen and name not in firsts: firsts[name] = sorted(keys)
    ok(out, firsts.get('dam') == ['pry'] and firsts.get('pool') == ['drain', 'pry'], f"order: the dam needs {firsts.get('dam')}, the crystal pool {firsts.get('pool')}")
    ch = [(x, 2) for x in range(9, 47)]; saved = {(x, y): g[y][x] for (x, y) in ch}
    for (x, y) in ch: g[y][x] = '#'
    ok(out, G in flood_stages(p, DC_GIVES, DC_LOCKS)[-1][1], "closing the chimney: the pool is still reachable (it is only a loop)")
    for (x, y), c in saved.items(): g[y][x] = c
    return out

def underground_labyrinth():
    """Underground Labyrinth (T5, under the quarry's blast shack). Graph dungeons/underground_labyrinth.mmd: a maze of cells and three locks, each key in a cell beyond the one before: The Fork (start) ->
    cell A west (the gate key '4') and cell B east ; the iron gate ('$' x2) south of B's passage -> cell C (the pry bar '3') ; A and C both lead to the Cave-In Chamber (D) ; the rockfall ('@' x2) south
    of D -> cell F (the wall of tally marks '6' = the code) ; the sealed slab ('^' x2) south of F -> the Minotaur's Hall (goal). Dead-end cells with something in them hang off C and F."""
    p = Plan(72, 58, 9881)
    cells = {'A': (6, 5), 'E': (22, 5), 'B': (38, 5), 'D': (22, 19), 'C': (54, 19), 'F': (22, 33), 'X': (54, 33), 'Y': (38, 33)}
    for (x, y) in cells.values(): p.room(x, y, 8, 6)
    p.hline(14, 21, 7, 2); p.hline(30, 37, 7, 2)                   # E - A, E - B
    p.hline(46, 58, 7, 2); p.vline(57, 8, 18, 2)                   # B - C (the iron gate on the way down)
    p.vline(9, 11, 22, 2); p.hline(9, 21, 21, 2)                   # A - D
    p.hline(30, 40, 21, 2); p.hline(47, 53, 21, 2)                 # blind passages from D and from C that once met (a fall between them): C is ONLY reached through the gate
    p.vline(25, 25, 32, 2)                                         # D - F (the rockfall)
    p.vline(25, 39, 45, 2); p.room(14, 46, 26, 9)                  # F - the Minotaur's Hall (the slab)
    p.vline(57, 25, 32, 2)                                         # C - X (a dead end)
    p.hline(30, 37, 35, 2)                                         # F - Y (a dead end)
    p.vline(15, 23, 29, 1); p.hline(46, 50, 36, 1)                 # more blind passages
    roughen(p, passes=1, chance=0.3)
    lock_row(p, 14, [57, 58], '$'); lock_row(p, 28, [25, 26], '@'); lock_row(p, 42, [25, 26], '^')
    p.collapse(26, 22, 2)
    props(p, ((7, 6, 'c'), (12, 9, 'k'), (39, 6, 'x'), (44, 9, 'k'), (55, 20, 'c'), (60, 23, 'b'), (23, 34, 'k'), (28, 37, '&'), (55, 34, 'c'), (60, 37, '&'), (39, 34, 'x'), (44, 37, 'b'),
              (16, 47, 'h'), (22, 47, 'h'), (30, 47, 'h'), (37, 47, 'h'), (16, 53, 'h'), (22, 53, 'h'), (30, 53, 'h'), (37, 53, 'h'), (26, 51, '&'), (28, 50, '&'), (25, 49, '&')))
    p.g[8][8] = '4'; p.g[22][59] = '3'; p.g[36][27] = '6'
    p.g[36][58] = 'J'; p.g[36][42] = 'L'; p.g[52][38] = 'K'
    p.g[7][25] = 'E'; p.g[21][24] = 'C'; p.g[50][27] = 'G'
    cave_litter(p, 20)
    return p
LAB_GIVES = {'4': ('gate', None), '3': ('pry', None), '6': ('code', None)}
LAB_LOCKS = {'$': 'gate', '@': 'pry', '^': 'code'}
def labyrinth_check(p):
    st = flood_stages(p, LAB_GIVES, LAB_LOCKS); g = p.g; out = []
    K1, K2, K3, G = pos(g, '4'), pos(g, '3'), pos(g, '6'), pos(g, 'G'); s0 = st[0][1]
    ok(out, K1 in s0 and K2 not in s0 and K3 not in s0 and G not in s0, "at the start: only the gate key's cell")
    firsts = {}
    for keys, seen in st:
        for name, c in (('pry', K2), ('code', K3), ('hall', G)):
            if c in seen and name not in firsts: firsts[name] = sorted(keys)
    ok(out, firsts.get('pry') == ['gate'] and firsts.get('code') == ['gate', 'pry'] and firsts.get('hall') == ['code', 'gate', 'pry'], f"order: pry {firsts.get('pry')}, code {firsts.get('code')}, hall {firsts.get('hall')}")
    return out

def siege_catacombs():
    """Siege Catacombs (T6, under the Siege Ward's bell tower; the story's Sealed Crypt). Graph dungeons/siege_catacombs.mmd: the stair -> the Ossuary (the hub: the iron gate north, the rockfall south)
    -> the west chamber (the sexton's box '3' = the gate key) and the sappers' camp east (the pry bar '4') ; the iron gate ('$' x2) -> the north chamber (the burial roll '6' = the code) ; the rockfall
    ('@' x2) -> the bone gallery -> the sealed slab ('^' x2) -> the Sealed Crypt (goal). The loop: the sappers' tunnel from their camp to the gallery, choked by a fall the same bar lifts."""
    p = Plan(60, 50, 9891)
    p.room(26, 2, 8, 5, ','); p.vline(29, 7, 10, 2, ',')           # St: the stair
    p.room(18, 11, 24, 10, ',')                                    # Hl: the Ossuary
    p.hline(42, 46, 15, 2, ','); p.room(47, 10, 9, 10, ',')        # Sc: the sappers' camp
    p.hline(13, 17, 15, 2, ','); p.room(4, 10, 9, 10, ',')         # Ch1: the west chamber
    p.vline(21, 6, 10, 2, ','); p.room(6, 2, 18, 4, ',')           # Ch2: the north chamber, behind the iron gate
    for x in (21, 22): p.g[8][x] = '$'
    p.vline(29, 21, 27, 2, ','); p.room(20, 28, 20, 5, ',')        # the bone gallery, behind the rockfall
    for x in (29, 30): p.g[24][x] = '@'
    p.vline(29, 33, 38, 2, ','); p.room(21, 39, 18, 8, ',')        # T: the Sealed Crypt, behind the slab
    for x in (29, 30): p.g[36][x] = '^'
    p.vline(51, 20, 30, 1, ','); p.hline(40, 51, 30, 1, ','); p.g[25][51] = '@'   # the sappers' tunnel (the loop)
    for x in range(19, 41, 3):                                     # bone niches along the Ossuary's walls
        if x not in (21, 22, 29, 30): p.g[11][x] = '&'; p.g[20][x] = '&'
    props(p, ((20, 13, 'h'), (26, 13, 'h'), (33, 13, 'h'), (39, 13, 'h'), (20, 18, 'h'), (26, 18, 'h'), (33, 18, 'h'), (39, 18, 'h'),
              (48, 11, 'c'), (50, 11, 'c'), (54, 14, 's'), (54, 15, 's'), (48, 18, 'b'), (53, 18, 'x'), (5, 11, 'h'), (11, 11, 'h'), (5, 18, 'h'), (11, 18, 'h'), (8, 14, '&'),
              (7, 3, 'h'), (12, 3, 'h'), (17, 3, 'h'), (9, 5, '&'), (22, 29, '&'), (26, 31, 'h'), (34, 29, 'h'), (38, 31, '&'), (22, 40, 'h'), (37, 40, 'h'), (22, 45, 'h'), (37, 45, 'h'), (27, 42, 'k'), (33, 44, 'x')))
    p.g[16][6] = '3'; p.g[17][54] = '4'; p.g[4][8] = '6'
    p.g[45][36] = 'K'; p.g[3][21] = 'J'
    p.g[4][29] = 'E'; p.g[16][30] = 'O'; p.g[43][30] = 'G'
    scatter(p, 16, floor=',', what="*~%&&'xt")
    return p
SC_GIVES = {'3': ('gate', None), '4': ('pry', None), '6': ('code', None)}
SC_LOCKS = {'$': 'gate', '@': 'pry', '^': 'code'}
def catacombs_check(p):
    st = flood_stages(p, SC_GIVES, SC_LOCKS); g = p.g; out = []
    K1, K2, K3, G = pos(g, '3'), pos(g, '4'), pos(g, '6'), pos(g, 'G'); s0 = st[0][1]
    ok(out, K1 in s0 and K2 in s0 and K3 not in s0 and G not in s0, "at the start: the gate key and the pry bar; not the burial roll or the crypt")
    firsts = {}
    for keys, seen in st:
        for name, c in (('roll', K3), ('crypt', G)):
            if c in seen and name not in firsts: firsts[name] = sorted(keys)
    ok(out, firsts.get('roll') == ['gate', 'pry'] and firsts.get('crypt') == ['code', 'gate', 'pry'], f"order: the roll {firsts.get('roll')}, the crypt {firsts.get('crypt')}")
    ok(out, all(G not in seen for keys, seen in st if 'code' not in keys), "without the code the crypt is unreachable by any route (the sappers' tunnel ends in the gallery, before the slab)")
    tun = [(51, y) for y in range(20, 31)]; saved = {(x, y): g[y][x] for (x, y) in tun}
    for (x, y) in tun: g[y][x] = '#'
    ok(out, G in flood_stages(p, SC_GIVES, SC_LOCKS)[-1][1], "closing the sappers' tunnel: the crypt is still reachable")
    for (x, y), c in saved.items(): g[y][x] = c
    return out

def lattice_foundry_lower():
    """The Lattice Foundry's synthesis level (T5). NOT re-planned: the Batch 91 floor (foundry_plans.lower: the ring corridor round the Coil Hall, the cable tunnel, the crew corridor, the Foreman's office, the
    plant room) is kept as Yan approved it, and the locks are set INTO it: the Foreman's locker ('3', in his office) = the badge ; the plant room's three ways in become badge doors ('1') and the fuse ('4') is
    in there ; the bus switch ('5', needs the fuse) is at the cable tunnel's west end ; the Coil Hall's north, west and south doors become dead power doors ('2') and the east breach is walled up, so the Cell
    Synthesizer (goal, 'Y') needs the power. The graph's coolant lock (steam doors) is not built: there is no art for it yet."""
    import foundry_plans
    p = foundry_plans.lower(); g = p.g
    for (x, y) in ((36, 15), (27, 24), (36, 33)): assert g[y][x] == 'D', (x, y, g[y][x]); g[y][x] = '2'
    for (x, y) in ((45, 25), (44, 26)): assert g[y][x] == 'r', (x, y, g[y][x]); g[y][x] = '#'
    for (x, y) in ((19, 41), (52, 42)): assert g[y][x] == 'D', (x, y, g[y][x]); g[y][x] = '1'
    assert g[38][36] == '.'; g[38][36] = '1'
    g[27][19] = '3'; g[40][22] = '4'; g[3][6] = '5'
    return p
LF_GIVES = {'3': ('badge', None), '4': ('fuse', None), '5': ('power', 'fuse')}
def foundry_check(p):
    st = flood_stages(p, LF_GIVES, BP_LOCKS, start='S'); g = p.g; out = []
    L3, C4, S5, Y = pos(g, '3'), pos(g, '4'), pos(g, '5'), pos(g, 'Y'); s0 = st[0][1]
    ok(out, L3 in s0 and S5 in s0 and C4 not in s0 and Y not in s0, "at the start: the Foreman's locker and the bus switch; not the plant room's fuse or the synthesizer")
    firsts = {}
    for keys, seen in st:
        for name, c in (('fuse', C4), ('synth', Y)):
            if c in seen and name not in firsts: firsts[name] = sorted(keys)
    ok(out, firsts.get('fuse') == ['badge'] and firsts.get('synth') == ['badge', 'fuse', 'power'], f"order: the fuse needs {firsts.get('fuse')}, the synthesizer {firsts.get('synth')}")
    return out

def secret_lab_gamma():
    """Secret Lab Gamma (T5, under the Antenna Grid; the story's Sealed Vault). Graph dungeons/secret_lab_gamma.mmd: the lift -> the atrium (the hub: a card door and a dead door in sight) -> the security
    office ('3' = badge) and a storage room (a dead end) ; the badge door ('1' x2) -> the labs ('4' = fuse) -> the generator hall (the switch '5' needs the fuse = power) ; the power door ('2' x2) -> the
    Signal Room -> the Sealed Vault's antechamber (goal: the vault itself opens with the STORY key the Depths' eel carries, StorySystem). The loop: a cable duct labs <-> generator hall."""
    p = Plan(64, 48, 9901)
    p.room(28, 2, 8, 5); p.vline(31, 7, 9, 2)                      # E: the lift
    p.room(22, 10, 20, 10)                                         # the atrium
    p.hline(17, 21, 14, 2); p.room(6, 9, 11, 10)                   # the security office
    p.hline(42, 46, 14, 2); p.room(47, 9, 10, 10)                  # storage (a dead end)
    p.vline(26, 20, 24, 2); p.room(8, 25, 24, 9)                   # the labs, behind the badge door
    for x in (26, 27): p.g[22][x] = '1'
    p.vline(14, 34, 37, 2); p.room(6, 38, 18, 7)                   # the generator hall
    p.vline(22, 34, 37, 1, 'r')                                    # the cable duct (the loop)
    p.vline(37, 20, 24, 2); p.room(36, 25, 20, 9)                  # the Signal Room, behind the power door
    for x in (37, 38): p.g[22][x] = '2'
    p.vline(45, 34, 37, 2); p.room(38, 38, 16, 7)                  # the Sealed Vault
    for x in (45, 46): p.g[36][x] = '!'
    props(p, ((23, 11, 'd'), (25, 11, 'd'), (40, 11, '>'), (40, 18, '>'), (23, 18, 'x'), (33, 15, 'h'), (29, 15, 'h'),
              (7, 10, 'l'), (9, 10, 'l'), (15, 10, 'f'), (8, 14, 'd'), (11, 14, 'd'), (15, 17, 'o'),
              (48, 10, 'c'), (50, 10, 'c'), (55, 10, 'l'), (48, 17, 'b'), (53, 14, 'c'), (54, 14, 'c'),
              (10, 27, 'a'), (14, 27, 'a'), (18, 27, 'g'), (22, 27, 'g'), (10, 31, 'j'), (13, 31, 'j'), (18, 31, 'u'), (29, 31, 'f'), (30, 26, 'f'),
              (8, 39, 'm'), (9, 39, 'm'), (14, 39, 'm'), (15, 39, 'm'), (20, 43, 'b'), (8, 43, 'q'), (9, 43, 'q'),
              (38, 26, 'o'), (41, 26, 'o'), (44, 26, 'o'), (50, 26, 'o'), (53, 26, 'o'), (40, 31, 'd'), (48, 31, 'd'), (54, 32, 'f'),
              (40, 39, 'h'), (51, 39, 'h'), (40, 43, 'h'), (51, 43, 'h')))
    p.g[12][12] = '3'; p.g[26][9] = '4'; p.g[41][21] = '5'
    p.g[17][55] = 'J'; p.g[43][46] = 'K'
    p.g[4][31] = 'E'; p.g[15][31] = 'A'; p.g[29][46] = 'T'; p.g[41][46] = 'G'
    scatter(p, 22)
    return p
def gamma_check(p): return badge_power_check(p, loop=[(22, y) for y in range(34, 38)], loop_name="the cable duct")

def secret_lab_beta():
    """Secret Lab Beta (T4, under the Ghost Town's town hall; the story's Archive Room, and Boss 1's hidden wing). The lift -> the reception (the hub) -> the guard room ('3' = badge) ; the badge door
    ('1' x2) -> the lab wing -> the Archive Room ('4' = fuse; the story's drive) ; the generator room south of the labs (the switch '5' needs the fuse = power; a vent hole joins it to the archive) ;
    the power door ('2' x2) -> the Isolation Ward (goal). In the ward's east wall a cracked stretch ('Z', Boss.TryBreakWall) hides the long corridor to the room where the body in black armour lies ('V').
    The graph's gate and keypad locks are built as the badge/power pair: there is no keypad-door art."""
    p = Plan(74, 48, 9911)
    p.room(32, 2, 8, 5); p.vline(35, 7, 9, 2)                      # E: the lift
    p.room(26, 10, 20, 9)                                          # the reception
    p.hline(21, 25, 13, 2); p.room(10, 9, 11, 9)                   # the guard room
    p.vline(29, 19, 23, 2); p.room(14, 24, 20, 8)                  # the lab wing, behind the badge door
    for x in (29, 30): p.g[21][x] = '1'
    p.hline(10, 13, 27, 2); p.room(3, 22, 7, 20)                   # the Archive Room
    p.vline(23, 32, 35, 2); p.room(14, 36, 20, 8)                  # the generator room
    p.hline(10, 13, 40, 1, 'r')                                    # the vent hole archive <-> generator (the loop)
    p.vline(41, 19, 23, 2); p.room(38, 24, 16, 10)                 # the Isolation Ward, behind the power door
    for x in (41, 42): p.g[21][x] = '2'
    p.g[29][54] = 'Z'                                              # the cracked wall
    p.hline(55, 62, 29, 1); p.room(63, 27, 7, 5)                   # the corridor nobody was meant to find, and the room at its end
    p.g[29][59] = 'e'; p.g[29][61] = '*'
    props(p, ((27, 11, 'd'), (29, 11, 'd'), (44, 11, '>'), (44, 17, '>'), (36, 14, 'h'), (27, 17, 'x'),
              (11, 10, 'l'), (13, 10, 'l'), (19, 10, 'f'), (12, 14, 'd'), (15, 14, 'o'), (19, 16, 'c'),
              (16, 26, 'a'), (20, 26, 'a'), (24, 26, 'g'), (16, 30, 'j'), (19, 30, 'j'), (26, 30, 'u'), (32, 25, 'f'),
              (4, 23, 'f'), (6, 23, 'f'), (8, 23, 'f'), (4, 30, 'f'), (8, 30, 'f'), (4, 34, 'd'), (8, 37, 'f'), (4, 39, 'x'),
              (16, 37, 'm'), (17, 37, 'm'), (22, 37, 'm'), (23, 37, 'm'), (31, 42, 'b'), (16, 42, 'q'),
              (39, 25, 'l'), (42, 25, 'l'), (45, 25, 'l'), (48, 25, 'l'), (51, 25, 'l'), (40, 32, 'a'), (46, 32, 'a'), (52, 32, 'j'), (39, 29, 'x'),
              (63, 27, 'x'), (69, 31, 'x'), (69, 27, 'e')))
    p.g[29][67] = 'V'; p.g[29][66] = '*'; p.g[30][67] = '*'
    p.g[11][17] = '3'; p.g[26][5] = '4'; p.g[42][20] = '5'
    p.g[17][44] = 'J'; p.g[30][50] = 'K'
    p.g[4][35] = 'E'; p.g[14][31] = 'R'; p.g[32][6] = 'A'; p.g[28][46] = 'G'
    scatter(p, 20)
    return p
BETA_LOCKS = {'1': 'badge', '2': 'power', 'Z': 'a wall'}
def beta_check(p):
    out = badge_power_check(p, loop=[(x, 40) for x in range(10, 14)], loop_name="the vent hole", locks=BETA_LOCKS)
    st = flood_stages(p, BP_GIVES, BETA_LOCKS); g = p.g
    ok(out, pos(g, 'V') not in st[-1][1] and pos(g, 'A') in st[1][1], "the body's room is sealed behind the cracked wall with every key; the Archive Room opens with the badge")
    g2 = pos(g, 'Z'); g[g2[1]][g2[0]] = '.'
    ok(out, pos(g, 'V') in flood_stages(p, BP_GIVES, BETA_LOCKS)[-1][1], "with the wall broken the body's room is reachable from the ward")
    g[g2[1]][g2[0]] = 'Z'
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

PLANS = {"pumping_basement": pumping_basement, "secret_cave": secret_cave, "smugglers_tunnel": smugglers_tunnel, "chapel_crypt": chapel_crypt, "valley_mine": valley_mine, "crusher_pit": crusher_pit, "flooded_bunker": flooded_bunker, "stranded_ferry": stranded_ferry, "camp_shelter": camp_shelter, "drydock_tunnels": drydock_tunnels, "hospital_morgue": hospital_morgue, "maintenance_tunnels": maintenance_tunnels, "underground_bunker": underground_bunker, "deep_caverns": deep_caverns, "underground_labyrinth": underground_labyrinth, "siege_catacombs": siege_catacombs, "lattice_foundry_lower": lattice_foundry_lower, "secret_lab_gamma": secret_lab_gamma, "secret_lab_beta": secret_lab_beta}
CHECKS = {"pumping_basement": pumping_check, "secret_cave": cave_check, "smugglers_tunnel": smugglers_check, "chapel_crypt": crypt_check, "valley_mine": mine_check, "crusher_pit": pit_check, "flooded_bunker": bunker_check, "stranded_ferry": ferry_check, "camp_shelter": shelter_check, "drydock_tunnels": drydock_check, "hospital_morgue": morgue_check, "maintenance_tunnels": tunnels_check, "underground_bunker": ubunker_check, "deep_caverns": caverns_check, "underground_labyrinth": labyrinth_check, "siege_catacombs": catacombs_check, "lattice_foundry_lower": foundry_check, "secret_lab_gamma": gamma_check, "secret_lab_beta": beta_check}
if __name__ == "__main__":
    here = os.path.dirname(os.path.abspath(__file__)); bad = 0
    for name, fn in PLANS.items():
        p = fn(); res = CHECKS[name](p) if name in CHECKS else []
        for line in res: print(name, line); bad += line.startswith("FAIL")
        open(os.path.join(here, name + ".txt"), "w").write("\n".join("".join(row) for row in p.g))
        print("\n".join("".join(row) for row in p.g))
    sys.exit(1 if bad else 0)
