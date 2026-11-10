# Lock-and-key redo of the small dungeons (Yan 2026-11-10: "apply the Alpha method to all dungeons"): hand-planned floors drawn from the mission graphs in dungeons/<id>.mmd
# (graph_check.py of the level-design skill), then flooded here with each key set (the text-grid check), then on the real built map by Tools/compile_check/test (PumpingChecks).
# Plan characters (LabLocks.cs): '!' a steel blast door ('D' is a wooden one), '@' a rockfall tile, '$' an iron-gate tile and '^' a sealed-slab tile (symbols, NOT letters: the foundry plans use R and F as landmark marks); 1 badge door, 2 power door, 4 fuse crate, 5 breaker (needs the fuse), 6 pump (needs power), W deep water (until the pump runs).
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
    for x in (23, 24): p.g[7][x] = '!'; p.g[15][x] = '!'   # blast doors (steel), not wooden ones
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
    p.g[4][24] = 'E'; p.g[26][8] = 'B'; p.g[26][40] = 'A'; p.g[34][24] = 'C'; p.g[38][41] = 'N'; p.g[24][18] = 'H'
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

PLANS = {"pumping_basement": pumping_basement, "secret_cave": secret_cave, "smugglers_tunnel": smugglers_tunnel, "chapel_crypt": chapel_crypt, "valley_mine": valley_mine, "crusher_pit": crusher_pit, "flooded_bunker": flooded_bunker}
CHECKS = {"pumping_basement": pumping_check, "secret_cave": cave_check, "smugglers_tunnel": smugglers_check, "chapel_crypt": crypt_check, "valley_mine": mine_check, "crusher_pit": pit_check, "flooded_bunker": bunker_check}
if __name__ == "__main__":
    here = os.path.dirname(os.path.abspath(__file__)); bad = 0
    for name, fn in PLANS.items():
        p = fn(); res = CHECKS[name](p) if name in CHECKS else []
        for line in res: print(name, line); bad += line.startswith("FAIL")
        open(os.path.join(here, name + ".txt"), "w").write("\n".join("".join(row) for row in p.g))
        print("\n".join("".join(row) for row in p.g))
    sys.exit(1 if bad else 0)
