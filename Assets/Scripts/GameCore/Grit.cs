// Batch 92 (Yan: "make stuff less 'neat' ... dark, gritty, dirty, bloody ... make some places dark requiring a flashlight"):
// a last pass over every built map. Indoor floors get grime banked at the walls, oil, papers, glass and the odd bones; a few
// blood scenes (a pool, spatter, a trail to where someone ended up). Underground maps also get their lights: ceiling lamps
// spread out, many of them dead, some red emergency lamps - most of the place stays dark. Floor-plan maps that already have
// lights (the Lattice Foundry) are drawn by hand and left alone.
using System.Collections.Generic;
using GameData;

namespace GameCore
{
    public static class Grit
    {
        public static MapLayout Apply(MapLayout m, string zoneId, bool interior)
        {
            if (m?.Tiles == null) return m;
            int h = m.Tiles.Length, w = m.Tiles[0].Length;
            if (m.Looks == null) { m.Looks = new string[h][]; for (int y = 0; y < h; y++) m.Looks[y] = new string[w]; }
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)   // Batch 108: a sunken truck a later pass left on dry land is just a wreck
                    if (m.Looks[y][x] == "sunktruck" && m.Grounds?[y]?[x] != "Water") m.Looks[y][x] = "wreck";
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                    if (m.Looks[y][x] == "light" || m.Looks[y][x] == "redlight") return m;   // hand-made plan
            var r = new System.Random(TerrainCanvas.Seed(zoneId) ^ 0x6ad);
            string G(int x, int y) => m.Grounds?[y]?[x];
            bool Cave(int x, int y) => G(x, y) == "CaveFloor" || G(x, y) == "Mud" || G(x, y) == "Dirt";
            // indoor floor: a map underground, or a building's floor on the surface
            bool Floor(int x, int y) => x > 0 && y > 0 && x < w - 1 && y < h - 1 && m.Tiles[y][x] == "Empty" && m.Looks[y][x] == null
                                        && G(x, y) != "Water" && (interior || G(x, y) == "Floor" || G(x, y) == "LabTile");
            bool Wall(int x, int y) => m.Tiles[y][x] == "Solid";
            var near = new HashSet<(int, int)>();
            if (m.Anchors != null) foreach (var a in m.Anchors.Values) for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) near.Add((a.x + dx, a.y + dy));
            var floors = new List<(int x, int y)>();
            for (int y = 1; y < h - 1; y++) for (int x = 1; x < w - 1; x++) if (Floor(x, y) && !near.Contains((x, y))) floors.Add((x, y));
            if (floors.Count == 0) return m;

            // dirt
            foreach (var (x, y) in floors)
            {
                bool wall = Wall(x + 1, y) || Wall(x - 1, y) || Wall(x, y + 1) || Wall(x, y - 1);
                double q = r.NextDouble();
                string d = wall && q < 0.08 ? "grime" : wall && q < 0.095 ? "oil"
                         : q < 0.02 ? "grime" : !Cave(x, y) && q < 0.026 ? "papers" : !Cave(x, y) && q < 0.03 ? "glass" : q < 0.033 ? "bones" : null;
                if (d != null) m.Looks[y][x] = d;
            }
            // blood: a scene for every ~600 floor tiles, 1 to 6
            int scenes = System.Math.Max(1, System.Math.Min(6, floors.Count / 600));
            for (int s = 0; s < scenes; s++)
            {
                var (x, y) = floors[r.Next(floors.Count)];
                for (int k = 0, n = r.Next(4, 11); k < n; k++)
                {
                    if (Floor(x, y) || m.Looks[y][x] == "grime") m.Looks[y][x] = "bloodtrail";
                    int dir = r.Next(4); int nx = x + (dir == 0 ? 1 : dir == 1 ? -1 : 0), ny = y + (dir == 2 ? 1 : dir == 3 ? -1 : 0);
                    if (m.Tiles[ny][nx] == "Empty") { x = nx; y = ny; }
                }
                m.Looks[y][x] = "blood";
                for (int k = 0; k < 3; k++) { int sx = x + r.Next(-2, 3), sy = y + r.Next(-1, 2); if (sx > 0 && sy > 0 && sx < w - 1 && sy < h - 1 && Floor(sx, sy)) m.Looks[sy][sx] = k == 0 ? "bones" : "splat"; }
            }
            if (!interior) return m;
            // lights: spread out (8+ tiles apart), most dead; caves have none
            var lights = new List<(int x, int y)>();
            for (int i = 0; i < floors.Count; i++)
            {
                var (x, y) = floors[r.Next(floors.Count)];
                if (Cave(x, y) || m.Looks[y][x] != null) continue;
                bool crowded = false;
                foreach (var l in lights) if ((l.x - x) * (l.x - x) + (l.y - y) * (l.y - y) < 121) { crowded = true; break; }
                if (crowded) continue;
                lights.Add((x, y));
                double q = r.NextDouble();
                if (q < 0.3) m.Looks[y][x] = "light"; else if (q < 0.45) m.Looks[y][x] = "redlight";   // the rest are dead
            }
            return m;
        }
    }
}
