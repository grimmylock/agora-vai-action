// Batch 72 (Yan chose "Cover that wraps"): single cover tiles were easy to flank - a gang of four walks round one sandbag
// and you count as Flanked (CoverUtil). On the tier maps, after the dresser, every lone low-cover piece (sandbags, a
// jersey barrier, crates, logs, tyres, stones, debris) grows into an L - a run of 3 with a 2-tile arm - so standing in
// its corner covers two sides. Near named places and roads the wild also gets an occasional L of its own (a fighting position:
// sandbags or concrete blocks on hard ground, logs or stones on soft). Cover tiles stay walkable, so no path is cut;
// cells next to walls, doors, water, roads and kept spots (village streets, yards) are left alone.
using System.Collections.Generic;
using GameData;

namespace GameCore
{
    public static class CoverWrap
    {
        static readonly HashSet<string> Grows = new HashSet<string> { "sandbags", "jersey", "crate", "logs", "tires", "stones", "debris", "block" };
        static readonly HashSet<string> Decos = new HashSet<string> { "bones", "crack", "flowers", "leaves", "lily", "litter", "oil", "pebbles", "puddle", "tallgrass", "tallgrass_dry", "tuft" };
        const int PositionStep = 13;          // one candidate fighting position per 13x13 cell of open wild
        const float PositionChance = 0.45f;

        public static readonly Dictionary<string, (int grown, int added)> Stats = new Dictionary<string, (int grown, int added)>();   // for the harness
        public static readonly List<(string zone, int x, int y)> Positions = new List<(string zone, int x, int y)>();   // and for render checks

        public static MapLayout Wrap(MapLayout m, string zoneId)
        {
            if (m?.Tiles == null || m.Regions == null || ZoneLayoutRecipes.IsInterior(zoneId)) return m;
            int h = m.Tiles.Length, w = m.Tiles[0].Length;
            var rng = new System.Random(TerrainCanvas.Seed(zoneId) ^ 0x5eed72);
            int Grown = 0, Added = 0;

            // 1. lone cover pieces grow into an L
            var lone = new List<(int x, int y)>();
            for (int y = 1; y < h - 1; y++)
                for (int x = 1; x < w - 1; x++)
                    if (IsCover(m, x, y) && m.Looks?[y][x] != null && Grows.Contains(m.Looks[y][x]) && CoverNeighbours(m, x, y) == 0) lone.Add((x, y));
            foreach (var (x, y) in lone)
                if (TryL(m, rng, x, y, m.Tiles[y][x], m.Looks[y][x], 2, 2, false)) Grown++;

            // 2. fighting positions in the open wild
            for (int cy = 4; cy < h - 4; cy += PositionStep)
                for (int cx = 4; cx < w - 4; cx += PositionStep)
                {
                    if (rng.NextDouble() > PositionChance) continue;
                    for (int tries = 0; tries < 8; tries++)
                    {
                        int x = cx + rng.Next(PositionStep - 4), y = cy + rng.Next(PositionStep - 4);
                        if (x >= w - 4 || y >= h - 4 || !Plain(m, x, y) || !Worth(m, x, y)) continue;
                        var region = MacroMapDatabase.Find(m.Regions[y][x]);
                        if (region == null || region.IsSafeZone) break;
                        string g = m.Grounds?[y][x];
                        bool hard = g == "Concrete" || g == "Gravel" || g == "Asphalt";
                        bool sturdy = rng.NextDouble() < 0.6;   // most are hard cover
                        string look = hard ? (sturdy ? (rng.Next(2) == 0 ? "jersey" : "block") : "debris") : (sturdy ? "sandbags" : (rng.Next(2) == 0 ? "logs" : "stones"));
                        if (TryL(m, rng, x, y, sturdy ? "HardCover" : "ScatterCover", look, 3, 2, true)) { Added++; Positions.Add((zoneId, x, y)); break; }
                    }
                }
            Stats[zoneId] = (Grown, Added);
            return m;
        }

        /// <summary>Batch 96 (Yan, on sandbags in empty meadows: "AI hallucinations"): a fighting position only goes where
        /// there is something to hold - within 10 tiles of a named place or 4 of a road.</summary>
        static bool Worth(MapLayout m, int x, int y)
        {
            if (m.Anchors != null)
                foreach (var a in m.Anchors.Values)
                    if (System.Math.Abs(a.x - x) <= 10 && System.Math.Abs(a.y - y) <= 10) return true;
            int h = m.Tiles.Length, w = m.Tiles[0].Length;
            for (int dy = -4; dy <= 4; dy++)
                for (int dx = -4; dx <= 4; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx >= 0 && ny >= 0 && nx < w && ny < h && m.Grounds?[ny][nx] == "Asphalt") return true;
                }
            return false;
        }

        static bool IsCover(MapLayout m, int x, int y) => m.Tiles[y][x] == "HardCover" || m.Tiles[y][x] == "ScatterCover";

        static int CoverNeighbours(MapLayout m, int x, int y)
        {
            int n = 0;
            if (IsCover(m, x + 1, y)) n++;
            if (IsCover(m, x - 1, y)) n++;
            if (IsCover(m, x, y + 1)) n++;
            if (IsCover(m, x, y - 1)) n++;
            return n;
        }

        static readonly (int dx, int dy)[] Dirs = { (1, 0), (-1, 0), (0, 1), (0, -1) };

        /// <summary>An L with its corner at (x,y): `run` cells one way (counting the corner) and `arm` more the other.</summary>
        static bool TryL(MapLayout m, System.Random rng, int x, int y, string tile, string look, int run, int arm, bool placeCorner)
        {
            int start = rng.Next(4);
            for (int k = 0; k < 4; k++)
            {
                var a = Dirs[(start + k) % 4];
                var b = (rng.Next(2) == 0) ? (-a.dy, a.dx) : (a.dy, -a.dx);
                var cells = new List<(int x, int y)>();
                for (int i = 1; i < run; i++) cells.Add((x + a.dx * i, y + a.dy * i));
                for (int i = 1; i <= arm; i++) cells.Add((x + b.Item1 * i, y + b.Item2 * i));
                bool ok = !placeCorner || Free(m, x, y, -1, -1, cells);
                foreach (var c in cells) if (!Free(m, c.x, c.y, x, y, cells)) { ok = false; break; }
                if (!ok) continue;
                if (placeCorner) Set(m, x, y, tile, look);
                foreach (var c in cells) Set(m, c.x, c.y, tile, look);
                return true;
            }
            return false;
        }

        /// <summary>Plain outdoor ground with nothing awkward round it (only the L's own cells may touch it).</summary>
        static bool Free(MapLayout m, int x, int y, int cx, int cy, List<(int x, int y)> mine)
        {
            int h = m.Tiles.Length, w = m.Tiles[0].Length;
            if (x < 1 || y < 1 || x >= w - 1 || y >= h - 1 || !Plain(m, x, y)) return false;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx == cx && ny == cy || mine.Contains((nx, ny))) continue;
                    if (!Plain(m, nx, ny)) return false;
                }
            return true;
        }

        static bool Plain(MapLayout m, int x, int y)
        {
            if (m.Tiles[y][x] != "Empty") return false;
            string look = m.Looks?[y][x];
            if (look != null && !Decos.Contains(look)) return false;   // ground decals (grass tufts, litter) are fine to build over
            if (m.Keep != null && m.Keep[y][x]) return false;
            string g = m.Grounds?[y][x];
            return g != "Water" && g != "Asphalt" && g != "Floor" && g != "Mud";
        }

        static void Set(MapLayout m, int x, int y, string tile, string look)
        {
            m.Tiles[y][x] = tile;
            if (m.Looks != null) m.Looks[y][x] = look;
        }
    }
}
