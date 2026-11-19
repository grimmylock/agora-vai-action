#!/usr/bin/env python3
"""The Loading Annex (Tier 3 beat 4 "The Door Under the Dock", STORY_GAUNT_LAB.md; Yan, 2026-11-19: "a small corridor that leads to a loading zone ... that leads to another smaller lab, with some tier 4 lootbags
and other mutants, and some story stuff", the story told "Resident Evil style"). A smaller concrete lab of five rooms around an intake hall, laid out by hand (rectangles), written to
Assets/Scripts/GameCore/TierDungeons.AnnexPlan.cs.   python3 ArtSource/plans/loading_annex.py
Legend (TierDungeons.StampPlan): # wall, , concrete floor, D door, ! steel door, { keypad door (the vault), i ceiling light, c crate, d desk, l lockers, f file cabinet, o console, m machine, j specimen jars, a lab table,
g bench, q cables, x debris, h block; capitals are landmarks: E Freight Lift (the way back up), I Intake Desk, C Cell Block, O Handler's Office, T Control Room, V Vault."""
import os
W, H = 64, 42
g = [['#'] * W for _ in range(H)]
def room(x0, y0, x1, y1, c=','):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1): g[y][x] = c
def put(x, y, c): g[y][x] = c
# Furniture rule (Yan, 2026-11-19: "most desks and everything is just put in the middle of rooms, it makes no sense", "the labs need more lab stuff"): everything stands against a wall; lab stock (microscope, terminal,
# centrifuge, cold storage: props_lab.py) comes through `overlay`, applied by LoadingAnnex() because the plan alphabet has no letters left. Doors and landmarks stay clear.
overlay = []                                                                  # (x, y, 'H' hard / 'S' scatter cover, look)
def ov(x, y, kind, look): overlay.append((x, y, kind, look))
# 1 the lift lobby and the decontamination corridor
room(4, 30, 14, 38); put(6, 34, 'E'); put(15, 34, '!')
for (x, y) in ((4, 31), (5, 31), (4, 32), (4, 37), (4, 38), (5, 38), (13, 38), (14, 38)): put(x, y, 'c')
put(14, 31, 'b'); put(14, 37, 'b')
ov(12, 30, 'S', 'terminal'); ov(9, 30, 'H', 'cabinet')
put(9, 33, 'i'); put(9, 36, 'i')
room(16, 33, 22, 35); put(19, 33, '#'); put(19, 35, '#'); put(19, 34, '!'); put(17, 34, 'i'); put(21, 34, 'i')
# 2 the intake hall
room(23, 28, 38, 40); put(31, 29, 'I')
ov(25, 28, 'S', 'terminal'); ov(26, 28, 'S', 'desk'); ov(33, 28, 'S', 'desk'); ov(34, 28, 'S', 'terminal')
for x in (28, 29, 36, 37): ov(x, 28, 'S', 'armchair')
for y in (29, 30, 31, 37, 38, 39): ov(23, y, 'H', 'lockers')                      # (nothing stands against a SOUTH wall: its tall face hides it)
for y in (36, 37, 38, 39): ov(38, y, 'H', 'cabinet')
put(29, 38, 'x'); put(33, 36, 'x'); put(26, 30, 'i'); put(35, 30, 'i'); put(26, 38, 'i'); put(35, 38, 'i')
put(30, 27, '!'); put(39, 34, '!')
# 3 the cell block
room(28, 14, 33, 26); put(30, 20, 'C')
for ys in (15, 19, 23):
    room(24, ys, 26, ys + 2); put(27, ys + 1, '!')
    if ys != 19: room(35, ys, 37, ys + 2); put(34, ys + 1, '!')
for y in (17, 23): put(30, y, 'i')
put(25, 15, 'x'); put(24, 23, 't'); put(36, 15, 't'); put(37, 25, 'x')
ov(24, 19, 'H', 'lockers'); ov(37, 23, 'H', 'lockers')
put(30, 26, ',')
put(34, 20, '!')
room(35, 20, 41, 20); put(42, 20, '!')
# 4 the control room and lab bay
room(43, 14, 57, 26); put(50, 20, 'T')
for x in (44, 45): ov(x, 14, 'H', 'fridge_lab')
for x in (47, 48, 49): ov(x, 14, 'H', 'console')
for x in (51, 52, 53): ov(x, 14, 'H', 'machine')
for x in (55, 56): ov(x, 14, 'S', 'terminal')
for y in (16, 19, 22): ov(43, y, 'S', 'microscope')
ov(43, 25, 'H', 'centrifuge')
for y in (16, 17, 18, 22, 23): ov(57, y, 'H', 'cabinet')
for (x, y) in ((47, 24), (53, 22)): put(x, y, 'q')
put(47, 20, 'i'); put(53, 20, 'i'); put(50, 17, 'i'); put(46, 21, '+'); put(54, 21, '+')
# 5 the Handler's office and the vault
room(42, 30, 53, 38); put(41, 34, '!'); room(40, 34, 40, 34); put(48, 34, 'O')
ov(47, 30, 'S', 'desk'); ov(48, 30, 'S', 'terminal'); ov(49, 30, 'S', 'desk')
for y in (31, 32, 33): ov(53, y, 'H', 'cabinet')
for y in (31, 32, 36, 37): ov(42, y, 'H', 'lockers')
put(52, 38, 'c'); put(51, 38, 'c'); put(47, 36, 'i'); put(47, 32, 'i')
put(54, 34, '{')
room(55, 30, 61, 38); put(58, 34, 'V')
for x in (56, 57, 58): ov(x, 30, 'H', 'lockers')
for x in (60, 61): ov(x, 30, 'H', 'fridge_lab')
ov(61, 32, 'H', 'cabinet'); ov(61, 33, 'H', 'cabinet'); ov(61, 36, 'H', 'cabinet'); ov(61, 37, 'H', 'cabinet')
for y in (31, 32, 36, 37): ov(55, y, 'H', 'cabinet')
put(58, 36, 'i')
put(40, 34, ',')
# CARVED, not built (Yan, 2026-11-19: "it's a lab inside a cave, not a whole building, it was carved into the cave walls"): the walls are rock (wallLook "cliff" in LoadingAnnex), the rooms are hand-cut: rough bites out of the edges and rock left standing in the
# corners; the floor is a poured concrete slab. Nothing is touched that carries furniture, a door, a landmark or a light, and no floor tile is cut off.
import random
rr = random.Random(2611)
protect = {(x, y) for (x, y, k, l) in overlay}
for y in range(H):
    for x in range(W):
        if g[y][x] != ',': protect.add((x, y)) if False else None
def keep(x, y):
    for dy in range(-2, 3):
        for dx in range(-2, 3):
            xx, yy = x + dx, y + dy
            if (xx, yy) in protect: return True
            if 0 <= xx < W and 0 <= yy < H and g[yy][xx] not in ',#': return True        # doors, marks, props, lights, debris
    return False
cand = [(x, y) for y in range(1, H - 1) for x in range(1, W - 1) if g[y][x] == ',' and sum(g[y + dy][x + dx] == '#' for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))) == 1 and not keep(x, y)]
rr.shuffle(cand)
for (x, y) in cand[:70]:
    nb = sum(g[y + dy][x + dx] == ',' for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
    if nb == 3: g[y][x] = '#'
rows = [''.join(r) for r in g]
out = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Assets', 'Scripts', 'GameCore', 'TierDungeons.AnnexPlan.cs')
with open(out, 'w') as f:
    f.write('// GENERATED by ArtSource/plans/loading_annex.py - the Loading Annex (Tier 3 beat 4 "The Door Under the Dock"): a small concrete lab behind the freight lift.\n')
    f.write('using System.Collections.Generic;\nusing GameData;\nusing static GameCore.GroundType;\n\nnamespace GameCore\n{\n    public static partial class TierDungeons\n    {\n        public static readonly string[] AnnexPlan =\n        {\n')
    for r in rows: f.write('            "%s",\n' % r)
    f.write('        };\n\n        /// <summary>Furniture and lab stock along the walls (the plan alphabet has no letters left): x, y, hard cover?, look.</summary>\n        static readonly (int, int, bool, string)[] AnnexStock =\n        {\n')
    for (x, y, k, l) in overlay: f.write('            (%d, %d, %s, "%s"),\n' % (x, y, 'true' if k == 'H' else 'false', l))
    f.write('''        };

        /// <summary>The lab under the Loading Dock: 'E' the freight lift (the way back up to the tunnels), 'I' the intake desk, 'C' the cell block, 'O' the Handler's office, 'T' the control room, 'V' the vault (a keypad door).</summary>
        public static MapLayout LoadingAnnex()
        {
            var marks = new Dictionary<char, (string, string)> { ['E'] = ("Freight Lift", "taken_tunnels"), ['I'] = ("Intake Desk", null), ['C'] = ("Cell Block", null), ['O'] = ("Handler's Office", null), ['T'] = ("Control Room", null), ['V'] = ("Vault", null) };
            var m = FloorPlan("loading_annex", AnnexPlan, "cliff", LabTile, marks, null);
            if (m.Looks == null) { m.Looks = new string[m.Tiles.Length][]; for (int y = 0; y < m.Tiles.Length; y++) m.Looks[y] = new string[m.Tiles[0].Length]; }
            foreach (var (x, y, hard, look) in AnnexStock) { if (m.Tiles[y][x] != "Empty" || m.Looks[y][x] != null) continue; m.Tiles[y][x] = hard ? "HardCover" : "ScatterCover"; m.Looks[y][x] = look; }
            return m;
        }
    }
}
''')
print('wrote', out, W, 'x', H)
