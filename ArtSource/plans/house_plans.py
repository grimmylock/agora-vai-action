# Batch 120 (Yan: "try making some 2 or 3 story buildings, these could be separate maps similar to caves"): buildings with
# upper floors. The ground floor is stamped onto the tier map (TierMaps.cs, StampPlan) in place of a plain carved block; its
# stair 'S' is an exit to the floor above, a small map of its own (TierDungeons.Upstairs). Footprints are not boxes: the
# farmhouse is an L, the school a U round its yard, the flats a T. Hand-drawn, messy on purpose; checked here: every row the
# same width, every floor tile reachable from the doors / stairs.
# Legend (TierDungeons.StampPlan): '#' wall  'n' window  'D' door  '.' floor  '_' outside (left as ground)
#   d desk  l lockers  c crate  b barrel  t trash  r rubble  x debris  ' " % ` : decals  S stair down/out  U stair up  T stash
import os

HOUSES = {
    # Tier 1, the Old Farmstead's farmhouse (12x9, an L: the north-east corner is the yard)
    "farmhouse_ground": [
        "########____",
        "#......#____",
        "#.l..c.#____",
        "#...'..D____",
        "#####D######",
        "n...'#..d..n",
        "#.%S.D....t#",
        "n....#.c...#",
        "####D###n###",
    ],
    "farmhouse_upper": [
        "#n##n#######",
        "#c...#######",
        "#..\".#######",
        "#r..T#######",
        "##D#########",
        "n....#..d.l#",
        "#.%S.D....:n",
        "n....#c..rb#",
        "#n####n##n##",
    ],
    # Tier 4, Ghost Town's school (30x16, a U round its yard to the north)
    "school_ground": [
        "##########__________##########",
        "#...d...dn__________n..l..c..#",
        "#........#__________#........#",
        "n..d..d..#__________#..d..d..n",
        "#........#__________#........#",
        "#..d..d..#__________#.'d..d..#",
        "#####D####__________####D#####",
        "#........############........#",
        "#........#..........#........#",
        "n..d..d..D....S.....D..d..d..n",
        "#........#...'......#........#",
        "#...x....#.r..c.....#..t.....#",
        "#####D####....b.....####D#####",
        "#........D..........D........#",
        "n..l..c..#...%......#..\"....n#",
        "##########nn##DD##nn##########",
    ],
    "school_upper": [
        "##########################n###",
        "#..d..d..n################...#",
        "#........##################..#",
        "n..d..d..##################..n",
        "#.....r..#################...#",
        "#..d..d..#################.d.#",
        "#####D####################D###",
        "#........############........#",
        "#...\"....#..........#........#",
        "n..d..d..D....S.....D..d.xd..n",
        "#........#.....T....#........#",
        "#..c.....#.r........#..t..r..#",
        "#####D####..........####D#####",
        "#....r...D....x.....D........#",
        "n..l..c..#...%......#..\"....n#",
        "##########nn########nn########",
    ],
    # Tier 4, the Flats (16x14, a T: the stairwell sticks out to the south)
    "flats_ground": [
        "################",
        "#.....##.....c.#",
        "n.d...##..'....n",
        "#.....D#...d...#",
        "#..t..#D.......#",
        "n.....#.......rn",
        "####D##.##D#####",
        "#......S.......#",
        "#.\"............#",
        "#####..%..######",
        "____#......#____",
        "____#.c....#____",
        "____##D##n##____",
        "____________ ___",
    ],
    "flats_2": [
        "################",
        "#..c..##.....d.#",
        "n.d...##.%.....n",
        "#.....D#...l...#",
        "#..r..#D...'...#",
        "n.....#....T..rn",
        "####D##.##D#####",
        "#......S.U.....#",
        "#..'...........#",
        "#####.....######",
        "#####..x...#####",
        "#####......#####",
        "#####n##n#######",
        "################",
    ],
    "flats_3": [
        "################",
        "#..x..##..r..d.#",
        "n.d...##....r..n",
        "#.....D#...l...#",
        "#..r..#D...\"...#",
        "n...T.#.......rn",
        "####D##.##D#####",
        "#........S.....#",
        "#..%..r........#",
        "#####.....######",
        "#####..r...#####",
        "#####...b..#####",
        "#####n##n#######",
        "################",
    ],
}
HOUSES["flats_ground"][13] = "________________"

WALK = set(".DSUT'\"%`:~;&*" + "dct")     # desks, crates and trash are climbed past; lockers, debris and barrels are not

def check(name, rows):
    w = len(rows[0])
    for i, r in enumerate(rows): assert len(r) == w, f"{name} row {i}: {len(r)} != {w}  {r!r}"
    starts = [(x, y) for y, r in enumerate(rows) for x, ch in enumerate(r) if ch in "DSU"]
    seen, q = set(starts), list(starts)
    while q:
        x, y = q.pop()
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if 0 <= ny < len(rows) and 0 <= nx < w and (nx, ny) not in seen and rows[ny][nx] in WALK: seen.add((nx, ny)); q.append((nx, ny))
    floor = [(x, y) for y, r in enumerate(rows) for x, ch in enumerate(r) if ch in WALK]
    cut = [p for p in floor if p not in seen]
    assert not cut, f"{name}: unreachable {cut[:8]}"
    return len(floor)

if __name__ == "__main__":
    here = os.path.dirname(os.path.abspath(__file__))
    for name, rows in HOUSES.items():
        n = check(name, rows)
        print(name, len(rows[0]), "x", len(rows), "floor", n)
        open(os.path.join(here, name + ".txt"), "w").write("\n".join(rows))
