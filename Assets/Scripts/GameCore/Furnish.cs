// 2026-11-19 (Yan: "furniture placement in the whole game: most desks and everything is just put in the middle of rooms, it makes no sense", and "the labs need more lab stuff, microscopes, computers, etc."):
// the last pass over every map (ZoneLayoutRecipes.BuildAll, after Grit), on authored plans and generated buildings alike.
//  1. Lab stock: on lab floor (LabTile) some benches get a microscope, some desks a computer terminal, some machines a centrifuge, some lockers become cold-storage cabinets (props_lab.py).
//  2. Wall placement: desks, lockers, cabinets, armchairs, terminals, cold storage and consoles stand against a wall. One that stands out in a room is moved to the nearest free spot beside a wall (within 9 tiles),
//     never into a doorway, a corridor, a landmark or a stash, and never so that it narrows a passage. Benches, lab tables and crates may stand free (islands, stacks).
// Deterministic (a hash of the tile and the map), so a map looks the same every time it is built.
using System.Collections.Generic;
using GameData;

namespace GameCore
{
    public static class Furnish
    {
        static readonly HashSet<string> WallBound = new HashSet<string> { "desk", "desk_n", "desk_s", "desk_e", "desk_w", "lockers", "cabinet", "armchair", "terminal", "fridge_lab", "console" };
        static readonly HashSet<string> WallLooks = new HashSet<string> { "wall", "plaster", "concrete", "plank", "metal", "hull", "window", "metaldoor", "cliff", "pillar" };

        static float H(int x, int y, int zone, int k) { unchecked { int h = x * 73856093 ^ y * 19349663 ^ zone * 83492791 ^ k * 2654435; h ^= h >> 13; h *= 1274126177; h ^= h >> 16; return (h & 0xffff) / 65536f; } }

        public static MapLayout Apply(MapLayout m, string zoneId)
        {
            if (m?.Tiles == null || m.Looks == null) return m;
            int h = m.Tiles.Length, w = m.Tiles[0].Length, zh = zoneId == null ? 0 : zoneId.GetHashCode() & 0xffff;
            bool Cover(int x, int y) => m.Tiles[y][x] == "HardCover" || m.Tiles[y][x] == "ScatterCover";
            // 1. lab stock
            if (m.Grounds != null)
                for (int y = 1; y < h - 1; y++) for (int x = 1; x < w - 1; x++)
                {
                    if (m.Grounds[y][x] != "LabTile" || !Cover(x, y)) continue;
                    string l = m.Looks[y][x], to = null;
                    if (l == "labbench" && H(x, y, zh, 1) < 0.5f) to = "microscope";
                    else if (l == "desk" && H(x, y, zh, 2) < 0.6f) to = "terminal";
                    else if (l == "machine" && H(x, y, zh, 3) < 0.25f) to = "centrifuge";
                    else if (l == "lockers" && H(x, y, zh, 4) < 0.3f) to = "fridge_lab";
                    if (to != null) m.Looks[y][x] = to;
                }
            // 2. against the walls
            var keep = new HashSet<(int, int)>(); if (m.Anchors != null) foreach (var a in m.Anchors.Values) for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) keep.Add((a.x + dx, a.y + dy));
            bool Wall(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && m.Tiles[y][x] == "Solid" && (m.Looks[y][x] == null || WallLooks.Contains(m.Looks[y][x]));
            bool Door(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && (m.Tiles[y][x] == "Door" || m.Tiles[y][x] == "DoorOpen");
            bool Touches(int x, int y) => Wall(x - 1, y) || Wall(x + 1, y) || Wall(x, y - 1) || Wall(x, y + 1);
            bool TouchesSide(int x, int y) => Wall(x - 1, y) || Wall(x + 1, y) || Wall(x, y - 1);   // a spot against a wall that is not only a south wall (its tall face would hide the piece)
            bool NearDoor(int x, int y) { for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) if (Door(x + dx, y + dy)) return true; return false; }
            bool Free(int x, int y) => x > 0 && y > 0 && x < w - 1 && y < h - 1 && m.Tiles[y][x] == "Empty" && m.Looks[y][x] == null;
            bool Corridor(int x, int y) => Wall(x - 1, y) && Wall(x + 1, y) || Wall(x, y - 1) && Wall(x, y + 1);
            int Open(int x, int y) { int n = 0; for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) if ((dx != 0 || dy != 0) && x + dx >= 0 && y + dy >= 0 && x + dx < w && y + dy < h && m.Tiles[y + dy][x + dx] != "Solid" && !Cover(x + dx, y + dy)) n++; return n; }
            var order = new List<(int x, int y)>();
            for (int y = 1; y < h - 1; y++) for (int x = 1; x < w - 1; x++) if (Cover(x, y) && m.Looks[y][x] != null && WallBound.Contains(m.Looks[y][x])) order.Add((x, y));
            foreach (var (x, y) in order)
            {
                if (Touches(x, y) || keep.Contains((x, y))) continue;
                (int x, int y) best = (-1, -1); int bd = int.MaxValue;
                for (int dy = -9; dy <= 9; dy++) for (int dx = -9; dx <= 9; dx++)
                {
                    int nx = x + dx, ny = y + dy, d = dx * dx + dy * dy; if (d >= bd || !Free(nx, ny) || !TouchesSide(nx, ny) || NearDoor(nx, ny) || keep.Contains((nx, ny)) || Corridor(nx, ny) || Open(nx, ny) < 5) continue;
                    if (m.Grounds != null && m.Grounds[ny][nx] != m.Grounds[y][x]) continue;                      // the same kind of floor (a desk does not go from the lab into the mud)
                    bool crowd = false; for (int ey = -1; ey <= 1 && !crowd; ey++) for (int ex = -1; ex <= 1; ex++) if ((ex != 0 || ey != 0) && Cover(nx + ex, ny + ey) && m.Looks[ny + ey][nx + ex] != m.Looks[y][x]) { crowd = true; break; }   // (a bank of lockers or cabinets side by side is fine)
                    if (crowd) continue;
                    best = (nx, ny); bd = d;
                }
                if (best.x < 0) continue;
                m.Tiles[best.y][best.x] = m.Tiles[y][x]; m.Looks[best.y][best.x] = m.Looks[y][x];
                m.Tiles[y][x] = "Empty"; m.Looks[y][x] = null;
            }
            return m;
        }
    }
}
