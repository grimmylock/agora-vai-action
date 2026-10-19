# Lattice Foundry floor plans (Batch 91, made messy in Batch 92: Yan "make stuff less 'neat', everything is too organized and
# simetrical, also make it dark, gritty, dirty, bloody ... make some places dark requiring a flashlight").
# Writes upper.txt / lower.txt next to itself; paste them into Assets/Scripts/GameCore/TierDungeons.Plans.cs.
# Legend (TierDungeons.FloorPlan): # wall  . floor  , concrete  D door  h block  m machine  l lockers (hard)  d desk  c crate
#   b rust barrel  t trash (light)  x debris (hard)  r rubble (slow)  w flooded floor (slow)  p pipes  i ceiling light
#   e red emergency light
#   decals: * blood  ' splat  ~ blood trail  % grime  ` scorch  " papers  : glass  ; oil  & bones   uppercase: landmark
import math, collections, random, os

WALK = set('.,DpABCEFGHIJKLMNOPQRSTUVWXYZdcbhlmtxrwieskv*\'~%`":;&')

class Plan:
    def __init__(s, w, h, seed): s.w, s.h = w, h; s.g = [['#'] * w for _ in range(h)]; s.r = random.Random(seed)
    def at(s, x, y): return s.g[y][x] if 0 <= x < s.w and 0 <= y < s.h else '#'
    def put(s, x, y, ch):
        if 0 < x < s.w - 1 and 0 < y < s.h - 1: s.g[y][x] = ch
    def room(s, x, y, w, h, ch='.'):
        for yy in range(y, y + h):
            for xx in range(x, x + w): s.put(xx, yy, ch)
    def hline(s, x1, x2, y, width=1, ch='.'):
        for yy in range(y, y + width):
            for xx in range(min(x1, x2), max(x1, x2) + 1): s.put(xx, yy, ch)
    def vline(s, x, y1, y2, width=1, ch='.'):
        for xx in range(x, x + width):
            for yy in range(min(y1, y2), max(y1, y2) + 1): s.put(xx, yy, ch)
    def disc(s, cx, cy, r, ch='.', r0=-1):
        for yy in range(cy - r - 1, cy + r + 2):
            for xx in range(cx - r - 1, cx + r + 2):
                d = math.hypot(xx - cx, yy - cy)
                if r0 < d <= r + 0.4: s.put(xx, yy, ch)

    # ---- the mess
    def jag(s, x, y, w, h, ch='.', bite=0.5):
        """A room whose corners are bitten off or pushed out and whose walls have the odd alcove: no clean boxes."""
        s.room(x, y, w, h, ch)
        for cx, cy, dx, dy in ((x, y, 1, 1), (x + w - 1, y, -1, 1), (x, y + h - 1, 1, -1), (x + w - 1, y + h - 1, -1, -1)):
            if s.r.random() < bite:                                   # a pillar or collapsed corner
                for k in range(s.r.randint(1, 2)): s.put(cx + dx * k, cy, '#'); s.put(cx, cy + dy * k, '#')
        for _ in range(s.r.randint(1, 3)):                            # an alcove pushed into the wall
            side = s.r.randrange(4)
            if side < 2: ax, ay, ox, oy = s.r.randrange(x + 1, x + w - 1), (y - 1 if side == 0 else y + h), 0, (-1 if side == 0 else 1)
            else: ax, ay, ox, oy = (x - 1 if side == 2 else x + w), s.r.randrange(y + 1, y + h - 1), (-1 if side == 2 else 1), 0
            if s.at(ax - ox, ay - oy) == ch and all(s.at(ax + ox * k + px, ay + oy * k + py) == '#' for k in range(2) for px, py in ((oy, ox), (0, 0), (-oy, -ox))):
                s.put(ax, ay, ch)
    def breach(s, x, y, n=3):
        """A hole knocked through a wall, rubble spilled round it: a second way in."""
        s.put(x, y, 'r')
        for _ in range(n):
            xx, yy = x + s.r.randint(-1, 1), y + s.r.randint(-1, 1)
            if s.at(xx, yy) in '.,': s.put(xx, yy, s.r.choice('rr%x'))
    def collapse(s, cx, cy, rad, keep_path=True):
        """A caved-in ceiling: rubble and debris heaped over the floor (rubble stays walkable, debris is hard cover)."""
        for yy in range(cy - rad, cy + rad + 1):
            for xx in range(cx - rad, cx + rad + 1):
                if s.at(xx, yy) in '.,' and math.hypot(xx - cx, yy - cy) <= rad + s.r.uniform(-0.6, 0.4):
                    s.put(xx, yy, 'x' if s.r.random() < 0.3 and math.hypot(xx - cx, yy - cy) < rad - 0.5 else 'r')
    def flood(s, cx, cy, rad):
        for yy in range(cy - rad, cy + rad + 1):
            for xx in range(cx - rad - 2, cx + rad + 3):
                if s.at(xx, yy) in '.,%' and math.hypot((xx - cx) * 0.7, yy - cy) <= rad + s.r.uniform(-0.8, 0.5): s.put(xx, yy, 'w')
    def trail(s, pts, pool=True):
        """Someone dragged, or crawled: a blood trail along the points, a pool where it ends."""
        for (x1, y1), (x2, y2) in zip(pts, pts[1:]):
            n = max(abs(x2 - x1), abs(y2 - y1))
            for k in range(n + 1):
                x, y = round(x1 + (x2 - x1) * k / max(1, n)), round(y1 + (y2 - y1) * k / max(1, n))
                if s.at(x, y) in '.,%' and s.r.random() < 0.75: s.put(x, y, '~')
        if pool: s.pool(*pts[-1])
    def pool(s, x, y, body=True):
        if s.at(x, y) in '.,%~': s.put(x, y, '*')
        for _ in range(4):
            xx, yy = x + s.r.randint(-2, 2), y + s.r.randint(-1, 1)
            if s.at(xx, yy) in '.,%': s.put(xx, yy, "'")
        if body:
            xx, yy = x + s.r.choice((-1, 1)), y
            if s.at(xx, yy) in ".,%'": s.put(xx, yy, '&')
    def scatter(s, x, y, w, h, chars, n):
        for _ in range(n):
            xx, yy = s.r.randrange(x, x + w), s.r.randrange(y, y + h)
            if s.at(xx, yy) in '.,': s.put(xx, yy, s.r.choice(chars))
    def dirty(s):
        """Grime along the walls, oil, papers and glass about: nothing is clean down here."""
        for y in range(1, s.h - 1):
            for x in range(1, s.w - 1):
                if s.g[y][x] not in '.,': continue
                wall = any(s.g[y + dy][x + dx] == '#' for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
                q = s.r.random()
                if wall and q < 0.10: s.g[y][x] = '%'
                elif wall and q < 0.12: s.g[y][x] = ';'
                elif q < 0.03: s.g[y][x] = '%'
                elif q < 0.04: s.g[y][x] = '"'
                elif q < 0.047: s.g[y][x] = ':'

    def mend(s, start, door='D'):
        """Any pocket cut off by overlapping rooms gets the shortest way through the walls to the rest, with a door."""
        while True:
            n, cut = s.check(start)
            if not cut: return
            src = set(cut[:1]); q = collections.deque(src)
            # flood the pocket, then walk through walls to the first reachable tile
            while q:
                x, y = q.popleft()
                for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)):
                    t = (x+dx, y+dy)
                    if t not in src and s.at(*t) in WALK: src.add(t); q.append(t)
            reached = set(f for f in [(x, y) for y in range(s.h) for x in range(s.w) if s.g[y][x] in WALK] if f not in set(cut))
            prev = {c: None for c in src}; q = collections.deque(src); end = None
            while q and end is None:
                x, y = q.popleft()
                for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)):
                    t = (x+dx, y+dy)
                    if t in prev or not (0 < t[0] < s.w - 1 and 0 < t[1] < s.h - 1): continue
                    prev[t] = (x, y)
                    if t in reached: end = t; break
                    if s.at(*t) == '#': q.append(t)
            if end is None: return
            path = []; c = prev[end]
            while c not in src: path.append(c); c = prev[c]
            for i, (x, y) in enumerate(path): s.put(x, y, door if i == len(path) - 1 else '.')

    def rows(s): return [''.join(r) for r in s.g]
    def check(s, start):
        seen = {start}; q = collections.deque([start])
        while q:
            x, y = q.popleft()
            for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)):
                n = (x+dx, y+dy)
                if 0 <= n[0] < s.w and 0 <= n[1] < s.h and n not in seen and s.g[n[1]][n[0]] in WALK: seen.add(n); q.append(n)
        floor = [(x, y) for y in range(s.h) for x in range(s.w) if s.g[y][x] in WALK]
        return len(floor), [f for f in floor if f not in seen]

def upper():
    p = Plan(74, 36, 92)
    p.jag(4, 4, 7, 8); p.room(5, 3, 3, 1); p.put(7, 7, 'L')                  # Foundry Lift lobby, a bent cage
    p.put(11, 7, 'D'); p.room(12, 6, 4, 4, ','); p.put(16, 8, 'D'); p.room(17, 5, 4, 5, ','); p.put(18, 7, 'K')   # decon lock, chambers out of line
    p.put(21, 6, 'D')
    p.vline(22, 2, 30, 3); p.vline(25, 13, 18); p.room(19, 21, 3, 2)        # west hall (checkpoint), a guard niche bulging out
    p.hline(22, 68, 2)                                                        # north service corridor, 1 wide
    p.hline(22, 41, 12, 3); p.hline(41, 50, 13, 2); p.hline(50, 68, 12, 3)   # the spine: pinched where the ceiling sagged
    offices = ((26, 4, 7, 6), (34, 4, 6, 6), (41, 5, 9, 5), (51, 4, 6, 6))   # offices of all sizes, not a row of boxes
    for i, (x, y, w, h) in enumerate(offices):
        p.jag(x, y, w, h, bite=0.6)
        dx = x + p.r.randint(1, w - 2)
        if i == 2: p.vline(dx, 10, 12); p.put(dx, 10, 'D')                  # the pinched spine: a short passage down to it
        else: p.put(dx, 11, 'D')
        if i in (0, 3): p.put(x + p.r.randint(1, w - 2), 3, 'D')
        p.scatter(x, y, w, h, 'ddl"', 4)
    p.vline(36, 10, 11)                                                       # office 2's door hangs off its frame
    p.put(33, 6, 'D'); p.breach(40, 7); p.breach(50, 8, 4)                    # enfilade: a door, then holes knocked through
    p.put(29, 6, 'O')                                                         # Records Office
    p.jag(59, 4, 10, 7); p.put(64, 7, 'S'); p.put(62, 11, 'D'); p.put(66, 3, 'D')   # Service Stair
    p.room(40, 16, 11, 3, ','); p.room(38, 17, 2, 1, ','); p.put(45, 15, 'D'); p.put(43, 19, 'D')  # Observation Room, glass blown out
    p.put(42, 17, 'd'); p.put(48, 17, 'd'); p.put(45, 17, 'B'); p.scatter(40, 16, 11, 3, '::', 5)
    p.room(26, 20, 43, 10)                                                    # the Firing Range: the long lane, battered
    p.room(26, 30, 9, 1); p.room(58, 19, 6, 1)
    for (x, y) in ((30, 23), (33, 27), (39, 22), (44, 26), (51, 23), (56, 28), (61, 22), (64, 26)): p.put(x, y, 'h')   # blocks where they fell, not in rows
    p.collapse(48, 21, 2)                                                     # the roof came down mid-lane: a heap to fight round
    p.flood(63, 28, 2)                                                        # pipe burst in the back corner
    p.put(27, 15, 'D'); p.vline(27, 16, 19); p.put(67, 15, 'D'); p.vline(67, 16, 19)   # range doors at both ends
    p.breach(39, 19)                                                          # and a hole from the range into the observation glass
    p.put(47, 24, 'R')
    p.jag(10, 18, 9, 8); p.put(14, 21, 'T'); p.put(21, 21, 'D')               # Storage
    for (x, y) in ((11, 19), (12, 19), (17, 20), (11, 24), (16, 24), (17, 23)): p.put(x, y, p.r.choice('cccb'))
    p.put(14, 26, 'D')
    p.vline(7, 12, 32); p.put(7, 12, 'D')                                     # the lift's maintenance duct, kinked
    p.hline(7, 20, 32); p.vline(20, 31, 33); p.hline(20, 33, 31); p.vline(33, 31, 32); p.hline(33, 47, 32); p.vline(14, 27, 32)
    for x in range(9, 46, 3):
        if p.at(x, 32) == '.': p.put(x, 32, 'p')
    p.vline(47, 30, 32); p.put(47, 30, 'D')
    p.dirty()
    # what happened here: a stand at the decon lock, the wounded dragged to storage, one crawled down the duct and didn't make it
    p.pool(13, 7); p.pool(19, 8); p.scatter(12, 6, 9, 4, "'``", 5)
    p.trail([(23, 9), (23, 18), (21, 21), (16, 22)]); p.trail([(7, 14), (7, 25), (8, 32), (19, 32)])
    p.pool(35, 27); p.pool(56, 24); p.scatter(26, 20, 43, 10, "`'&b", 12); p.scatter(22, 2, 47, 1, '%t', 6)
    # lights: the lobby's emergency lamp, the stair, the observation room and two of the range's lamps still work;
    # the offices, storage, the spine's far end and the duct are black
    for (x, y) in ((6, 6), (63, 5), (45, 16), (31, 21), (58, 21), (24, 26), (30, 13)): p.put(x, y, 'i')
    for (x, y) in ((18, 6), (7, 22), (40, 32), (66, 13)): p.put(x, y, 'e')    # emergency lamps: the lock, the duct, the spine's end
    return p

def lower():
    p = Plan(74, 48, 93)
    cx, cy = 36, 24
    p.disc(cx, cy, 13, '.', 10.6)                                             # ring corridor
    p.disc(cx, cy, 8, '.')                                                    # Coil Hall
    for a in (0, 50, 95, 130, 180, 220, 275, 315):                            # coils, not evenly spaced
        r = 4.5 + p.r.uniform(-0.6, 0.8)
        p.put(cx + round(r * math.cos(math.radians(a))), cy + round(r * math.sin(math.radians(a))), 'm')
    p.put(cx, cy, 'Y')                                                        # Cell Synthesizer
    for (dx, dy) in ((0, -9), (0, 9), (-9, 0)):                               # 3 doors from the ring into the hall...
        p.put(cx + dx, cy + dy, 'D'); p.put(cx + dx * 10 // 9, cy + dy * 10 // 9, '.')
    p.breach(cx + 9, cy + 1, 4); p.put(cx + 10, cy + 1, '.')                  # ...and the east one blown open
    p.put(cx + 3, cy - 5, 'H')                                                # Coil Hall anchor
    p.collapse(cx - 2, cy + 12, 2)                                            # the ring caved in at the south: squeeze over the rubble
    p.flood(cx + 11, cy - 7, 2)
    p.jag(61, 20, 8, 8); p.put(64, 23, 'S')                                   # Service Stair landing
    p.hline(50, 60, 24, 2); p.hline(53, 57, 26); p.put(60, 24, 'D')           # stair -> ring
    p.hline(5, 30, 3, 2); p.hline(30, 44, 4, 2); p.hline(44, 68, 3, 2)       # north cable tunnel, kinked where it was rerouted
    for x in range(6, 68, 4):
        if p.at(x, 4) == '.': p.put(x, 4, 'p')
    p.collapse(52, 3, 1)
    p.vline(cx, 5, 10, 2); p.vline(66, 5, 19); p.put(66, 19, 'D')             # tunnel -> ring, tunnel -> stair
    p.vline(9, 3, 42, 2); p.room(7, 17, 2, 4)                                 # crew corridor (west), a widened bit
    for i, (y, w, h) in enumerate(((7, 7, 5), (13, 8, 6), (33, 6, 5))):       # bunk rooms of different sizes
        p.jag(12, y, w, h); p.put(11, y + 2, 'D'); p.scatter(12, y, w, h, 'llct"', 5)
    p.breach(15, 12)                                                          # two bunk rooms knocked together
    p.put(15, 9, 'Q')                                                         # Crew Quarters
    p.jag(12, 20, 10, 9, bite=0.8); p.put(11, 24, 'D'); p.put(16, 24, 'F')    # Foreman's Office
    p.put(14, 21, 'd'); p.put(19, 22, 'd'); p.put(14, 27, 'l'); p.scatter(12, 20, 10, 9, '""":', 6)
    p.hline(22, 25, 25); p.put(22, 25, 'D')                                   # office -> ring (west)
    p.jag(20, 39, 32, 6, ','); p.put(36, 38, 'D'); p.vline(36, 37, 38)        # Plant Room, under the ring
    for x in (23, 28, 31, 38, 44, 48): p.put(x, 41 + p.r.choice((-1, 0, 0, 1)), 'm')
    for x in range(22, 50, 2):
        if p.at(x, 43) in '.,': p.put(x, 43, 'p')
    p.flood(26, 42, 1); p.flood(46, 41, 2)
    p.put(34, 42, 'P'); p.hline(11, 19, 41); p.put(19, 41, 'D')               # plant -> crew corridor
    p.hline(52, 64, 42); p.put(52, 42, 'D'); p.vline(64, 28, 42); p.put(64, 28, 'D')   # plant -> stair (south)
    p.dirty()
    # the crew made a last stand round the synthesizer; the Foreman's office was barricaded and broken into
    for (x, y) in ((cx - 3, cy + 2), (cx + 4, cy - 2), (cx - 1, cy - 6), (cx + 6, cy + 4)): p.pool(x, y)
    p.trail([(cx + 10, cy + 1), (cx + 12, cy + 6), (cx + 6, cy + 11)]); p.trail([(22, 25), (18, 25), (16, 26)])
    p.scatter(cx - 8, cy - 8, 17, 17, "`'``", 10); p.scatter(12, 20, 10, 9, 'cx', 3)
    p.scatter(9, 3, 2, 39, '~%;&', 8)
    # lights: only the synthesizer's work lights and a lamp at the stair; the tunnel, crew rooms and plant room are black
    for (x, y) in ((cx - 2, cy - 2), (cx + 2, cy + 3), (63, 22), (17, 23)): p.put(x, y, 'i')
    for (x, y) in ((10, 18), (24, 3), (58, 4), (cx, cy + 12), (44, 42)): p.put(x, y, 'e')   # red lamps down the tunnels and the plant room
    return p

if __name__ == "__main__":
    here = os.path.dirname(os.path.abspath(__file__))
    for name, f, start in (("upper", upper, (7, 7)), ("lower", lower, (64, 23))):
        p = f(); n, cut = p.check(start)
        print(name, p.w, "x", p.h, "walkable", n, "unreachable", len(cut), cut[:8])
        assert not cut, cut
        open(os.path.join(here, f"{name}.txt"), "w").write("\n".join(p.rows()))
