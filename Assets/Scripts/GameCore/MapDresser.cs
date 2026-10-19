// Batch 30: dresses an upscaled zone so the extra space is a place, not a lawn. After MapUpscaler
// blows a recipe up 3x, this fills the open ground with what the zone's biome would have:
// forest patches (value noise) with bushes on their edges, lone trees, rocks, reeds, scrap,
// crates, tall grass and litter decals, plus small points of interest - shacks, car wrecks
// along roads, camps. Rules:
//   - deterministic per zone (seeded hashes), so revisits look the same;
//   - never near landmarks/exits (radius 5), never on roads, building floors or decorated tiles;
//   - a blocking prop only goes where all 8 neighbours are open ground, so it can never close a
//     passage; a stamped POI needs a 2-tile open margin all round for the same reason.
using System.Collections.Generic;
using GameData;

namespace GameCore
{
    public static class MapDresser
    {
        class Biome
        {
            public string Tree, Tree2; public float Forest, LoneTree, Bush, Rock, Grass;
            public string[] Clutter; public float ClutterRate; public string ClutterType = "ScatterCover";
            public int Shacks, Camps; public bool Wrecks; public string ShackWall;
        }

        static string FirstRegion(MapLayout m)
        {
            foreach (var row in m.Regions) foreach (var r in row) if (r != null) return r;
            return null;
        }

        static Biome For(string zoneId)
        {
            switch (zoneId)
            {
                case "bonfire_village": return new Biome { Tree = "tree", Tree2 = "pine", Forest = 0.10f, LoneTree = 0.004f, Bush = 0.010f, Rock = 0.001f, Grass = 0.08f, Clutter = new[] { "crate", "tires" }, ClutterRate = 0.002f, Wrecks = false };
                case "perimeter": return new Biome { Tree = "tree", Tree2 = "pine", Forest = 0.30f, LoneTree = 0.010f, Bush = 0.020f, Rock = 0.004f, Grass = 0.12f, Clutter = new[] { "debris", "tires" }, ClutterRate = 0.002f, Shacks = 5, Camps = 2, Wrecks = true };
                case "black_valley": return new Biome { Tree = "pine", Tree2 = "tree", Forest = 0.32f, LoneTree = 0.012f, Bush = 0.018f, Rock = 0.010f, Grass = 0.10f, Clutter = new[] { "debris" }, ClutterRate = 0.002f, Shacks = 3, Camps = 2, Wrecks = true };
                case "crimson_woods": return new Biome { Tree = "redtree", Tree2 = "deadtree", Forest = 0.36f, LoneTree = 0.015f, Bush = 0.02f, Rock = 0.004f, Grass = 0.10f, Clutter = new[] { "logs" }, ClutterRate = 0.002f, Shacks = 2, Camps = 2 };
                case "marshes": return new Biome { Tree = "tree", Tree2 = "deadtree", Forest = 0.22f, LoneTree = 0.03f, Bush = 0.03f, Rock = 0.002f, Grass = 0.20f, Clutter = new[] { "reeds" }, ClutterRate = 0.012f, Shacks = 4, Camps = 1, Wrecks = true, ShackWall = "plank" };
                case "outskirts": return new Biome { Tree = "deadtree", Forest = 0.06f, LoneTree = 0.008f, Bush = 0.008f, Rock = 0.008f, Grass = 0.06f, Clutter = new[] { "brush", "debris" }, ClutterRate = 0.006f, Shacks = 2, Camps = 0, Wrecks = true };
                case "smugglers_run": case "rail_yards": return new Biome { Tree = "pine", Forest = 0.12f, LoneTree = 0.005f, Bush = 0.010f, Rock = 0.010f, Grass = 0.06f, Clutter = new[] { "crate", "debris", "tires" }, ClutterRate = 0.004f, Shacks = 3, Camps = 1, Wrecks = true };
                case "quarry_basin": case "dried_lake": return new Biome { Tree = "deadtree", Forest = 0.02f, LoneTree = 0.003f, Bush = 0.004f, Rock = 0.016f, Grass = 0.04f, Clutter = new[] { "debris", "brush" }, ClutterRate = 0.006f, Shacks = 2, Camps = 1, Wrecks = true };
                case "scrapyard": case "ship_graveyard": return new Biome { Tree = "deadtree", Forest = 0.02f, LoneTree = 0.003f, Bush = 0.006f, Rock = 0.004f, Grass = 0.05f, Clutter = new[] { "tires", "rustbarrel", "debris", "scrap", "engine" }, ClutterRate = 0.010f, Shacks = 2, Camps = 1, Wrecks = true };
                case "industrial_basin": case "antenna_grid": case "reactor_core": return new Biome { Tree = "tree", Forest = 0.05f, LoneTree = 0.003f, Bush = 0.006f, Rock = 0.002f, Grass = 0.05f, Clutter = new[] { "crate", "rustbarrel", "debris", "pipes", "engine" }, ClutterRate = 0.008f, Shacks = 2, Camps = 0, Wrecks = true };
                case "ghost_town": case "abandoned_city": case "hospital": return new Biome { Tree = "tree", Forest = 0.06f, LoneTree = 0.006f, Bush = 0.010f, Rock = 0.001f, Grass = 0.06f, Clutter = new[] { "debris", "crate", "tires" }, ClutterRate = 0.006f, Shacks = 4, Camps = 1, Wrecks = true };
                case "vanguard_barracks": return new Biome { Tree = "pine", Forest = 0.10f, LoneTree = 0.006f, Bush = 0.010f, Rock = 0.004f, Grass = 0.08f, Clutter = new[] { "crate", "sandbags", "tires" }, ClutterRate = 0.004f, Shacks = 3, Camps = 1, Wrecks = true };
                case "scavengers_hub": return new Biome { Tree = "tree", Forest = 0.10f, LoneTree = 0.006f, Bush = 0.010f, Rock = 0.002f, Grass = 0.10f, Clutter = new[] { "crate", "tires" }, ClutterRate = 0.002f, Shacks = 2, Camps = 0, Wrecks = true };
                default: return null;    // hidden interiors stay as authored
            }
        }

        public static MapLayout Dress(MapLayout m, string zoneId)
        {
            var b = For(zoneId);
            if (m?.Regions != null) b = b ?? For(FirstRegion(m));
            if (b == null || m?.Tiles == null) return m;
            int H = m.Tiles.Length, W = m.Tiles[0].Length;
            // Tier maps: each cell dresses as its own region's biome, and the authored spots replace the random huts and camps
            bool tier = m.Regions != null;
            var biomes = new Dictionary<string, Biome>();
            Biome At(int x, int y)
            {
                if (!tier) return b;
                string r = m.Regions[y][x];
                if (r == null) return b;
                if (!biomes.TryGetValue(r, out var bb)) biomes[r] = bb = For(r) ?? b;
                return bb;
            }
            bool[,] RegionMask(params string[] ids)
            {
                if (!tier) return null;
                var mask = new bool[H, W];
                for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) mask[y, x] = System.Array.IndexOf(ids, m.Regions[y][x]) >= 0;
                return mask;
            }
            int seed = TerrainCanvas.Seed(zoneId) ^ 0x5eed;
            var theme = ZoneThemes.For(zoneId);
            var looks = m.Looks ?? NewGrid(W, H);
            var grounds = m.Grounds ?? NewGrid(W, H);

            // keep-clear mask: anchors (landmarks, exits) and everything already authored
            var keep = new bool[H, W];
            if (m.Keep != null)
                for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) keep[y, x] = m.Keep[y][x];
            if (m.Anchors != null)
                foreach (var a in m.Anchors.Values)
                    for (int y = a.y - 5; y <= a.y + 5; y++)
                        for (int x = a.x - 5; x <= a.x + 5; x++)
                            if (x >= 0 && y >= 0 && x < W && y < H) keep[y, x] = true;
            int Hgt(int x, int y) => m.Heights == null ? 0 : m.Heights[y][x];
            GroundType Gr(int x, int y) => grounds[y][x] != null && System.Enum.TryParse<GroundType>(grounds[y][x], out var g) ? g : ZoneThemes.GroundAt(zoneId, x, y);
            bool OpenGround(int x, int y)
            {
                if (x < 1 || y < 1 || x >= W - 1 || y >= H - 1) return false;
                if (m.Tiles[y][x] != "Empty" || looks[y][x] != null || keep[y, x]) return false;
                var g = Gr(x, y);
                return g != GroundType.Asphalt && g != GroundType.Floor && g != GroundType.LabTile && g != GroundType.Concrete && g != GroundType.Water;
            }
            bool Clear8(int x, int y, int r = 1)
            {
                int h0 = Hgt(x, y);
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                        if (!OpenGround(x + dx, y + dy) || Hgt(x + dx, y + dy) != h0) return false;
                return true;
            }
            float Rnd(int x, int y, int k) => ZoneThemes.Hash01(x, y, seed + k * 7919);
            float Noise(int x, int y, float cell, int k)
            {
                float fx = x / cell, fy = y / cell; int ix = (int)System.Math.Floor(fx), iy = (int)System.Math.Floor(fy);
                float tx = fx - ix, ty = fy - iy; tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
                float a = Rnd(ix, iy, k), bb = Rnd(ix + 1, iy, k), c = Rnd(ix, iy + 1, k), d = Rnd(ix + 1, iy + 1, k);
                return (a + (bb - a) * tx) + ((c + (d - c) * tx) - (a + (bb - a) * tx)) * ty;
            }
            void Put(int x, int y, string type, string look) { m.Tiles[y][x] = type; looks[y][x] = look; }
            if (tier)
            {
                // the composers only touch their own region's cells (everything else is "kept" for the pass)
                void Masked(string[] ids, System.Action<bool[,]> pass)
                {
                    var mask = RegionMask(ids); bool any = false;
                    for (int y = 0; y < H && !any; y++) for (int x = 0; x < W; x++) if (mask[y, x]) { any = true; break; }
                    if (!any) return;
                    var k2 = new bool[H, W];
                    for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) k2[y, x] = keep[y, x] || !mask[y, x];
                    pass(k2);
                }
                Masked(new[] { "marshes" }, k2 => Marsh(m, looks, grounds, k2, W, H, Noise, Rnd));
                Masked(new[] { "abandoned_city", "ghost_town" }, k2 => City(m, looks, grounds, k2, W, H, Noise, Rnd));
                Masked(new[] { "industrial_basin" }, k2 => Industrial(m, looks, grounds, k2, W, H, Noise, Rnd));
            }
            else
            {
                if (zoneId == "marshes") Marsh(m, looks, grounds, keep, W, H, Noise, Rnd);
                if (zoneId == "abandoned_city" || zoneId == "ghost_town") City(m, looks, grounds, keep, W, H, Noise, Rnd);
                if (zoneId == "industrial_basin") Industrial(m, looks, grounds, keep, W, H, Noise, Rnd);
            }

            // 1) points of interest first (they need the most room) - authored on tier maps
            int placedShacks = 0;
            for (int i = 0; i < 600 && placedShacks < (tier ? 0 : b.Shacks); i++)
            {
                int w = 6 + (int)(Rnd(i, 1, 11) * 5), h = 5 + (int)(Rnd(i, 2, 11) * 4);
                int x0 = 3 + (int)(Rnd(i, 3, 11) * (W - w - 6)), y0 = 3 + (int)(Rnd(i, 4, 11) * (H - h - 6));
                if (!RectClear(x0 - 2, y0 - 2, w + 4, h + 4)) continue;
                string wall = b.ShackWall ?? (Rnd(i, 5, 11) < 0.5f ? "plaster" : "wall");
                for (int y = y0; y < y0 + h; y++)
                    for (int x = x0; x < x0 + w; x++)
                    {
                        bool edge = x == x0 || y == y0 || x == x0 + w - 1 || y == y0 + h - 1;
                        grounds[y][x] = GroundType.Floor.ToString();
                        if (edge) Put(x, y, "Solid", wall); else keep[y, x] = true;
                    }
                // a door on the south side, a hole on another side (ruins have holes), a crate inside
                int dx0 = x0 + 1 + (int)(Rnd(i, 6, 11) * (w - 2));
                Put(dx0, y0 + h - 1, "Door", null);
                if (Rnd(i, 7, 11) < 0.6f) { int hy = y0 + 1 + (int)(Rnd(i, 8, 11) * (h - 2)); Put(Rnd(i, 9, 11) < 0.5f ? x0 : x0 + w - 1, hy, "DoorOpen", "broken"); }
                Put(x0 + 1, y0 + 1, "ScatterCover", "crate");
                if (w > 7) Put(x0 + w - 2, y0 + 1, "ScatterCover", Rnd(i, 10, 11) < 0.5f ? "lockers" : "crate");
                for (int y = y0 - 2; y < y0 + h + 2; y++) for (int x = x0 - 2; x < x0 + w + 2; x++) if (x >= 0 && y >= 0 && x < W && y < H) keep[y, x] = true;
                placedShacks++;
            }
            bool RectClear(int x0, int y0, int w, int h)
            {
                if (x0 < 1 || y0 < 1 || x0 + w >= W - 1 || y0 + h >= H - 1) return false;
                int h0 = Hgt(x0, y0);
                for (int y = y0; y < y0 + h; y++)
                    for (int x = x0; x < x0 + w; x++)
                        if (!OpenGround(x, y) || Hgt(x, y) != h0) return false;
                return true;
            }
            for (int i = 0, camps = 0; i < 300 && camps < (tier ? 0 : b.Camps); i++)
            {
                int x = 4 + (int)(Rnd(i, 21, 13) * (W - 8)), y = 4 + (int)(Rnd(i, 22, 13) * (H - 8));
                if (!RectClear(x - 3, y - 3, 7, 7)) continue;
                Put(x, y, "Empty", "campfire");
                Put(x - 2, y, "ScatterCover", "logs"); Put(x + 2, y + 1, "ScatterCover", "logs");
                Put(x + 1, y - 2, "ScatterCover", "crate");
                for (int yy = y - 3; yy <= y + 3; yy++) for (int xx = x - 3; xx <= x + 3; xx++) keep[yy, xx] = true;
                camps++;
            }
            // wrecks parked along roads
            for (int y = 2; y < H - 2; y++)
                    for (int x = 2; x < W - 2; x++)
                        if (At(x, y).Wrecks && Gr(x, y) == GroundType.Asphalt && m.Tiles[y][x] == "Empty" && looks[y][x] == null && !keep[y, x]
                            && Rnd(x, y, 31) < 0.006f && Gr(x - 1, y) == GroundType.Asphalt && Gr(x + 1, y) == GroundType.Asphalt)
                        {
                            Put(x, y, "HardCover", "wreck");
                            for (int yy = y - 4; yy <= y + 4; yy++) for (int xx = x - 4; xx <= x + 4; xx++) if (xx >= 0 && yy >= 0 && xx < W && yy < H) keep[yy, xx] = true;
                        }

            // 2) vegetation and clutter
            for (int y = 1; y < H - 1; y++)
                for (int x = 1; x < W - 1; x++)
                {
                    if (!OpenGround(x, y)) continue;
                    var b0 = b; b = At(x, y);
                    DressCell(x, y);
                    b = b0;
                }
            void DressCell(int x, int y)
                {
                    float forest = Noise(x, y, 11f, 41) * 0.75f + Noise(x, y, 4f, 43) * 0.25f;
                    bool inForest = forest > 0.62f;
                    bool forestEdge = !inForest && forest > 0.55f;
                    float r = Rnd(x, y, 47);
                    string tree = Rnd(x, y, 49) < 0.7f || b.Tree2 == null ? b.Tree : b.Tree2;
                    if (inForest && r < b.Forest && Clear8(x, y)) { Put(x, y, "Solid", tree); return; }
                    if (!inForest && r < b.LoneTree && Clear8(x, y)) { Put(x, y, "Solid", tree); return; }
                    r = Rnd(x, y, 53);
                    if ((forestEdge ? 4f : inForest ? 2f : 1f) * b.Bush > r) { Put(x, y, "ScatterCover", "bush"); return; }
                    r = Rnd(x, y, 59);
                    if (r < b.Rock && Clear8(x, y)) { Put(x, y, "Solid", "rock"); return; }
                    r = Rnd(x, y, 61);
                    if (b.Clutter != null && r < b.ClutterRate) { string c = b.Clutter[(int)(Rnd(x, y, 67) * b.Clutter.Length) % b.Clutter.Length]; Put(x, y, c == "rustbarrel" ? "ScatterCover" : "ScatterCover", c); return; }
                    r = Rnd(x, y, 71);
                    if (r < b.Grass * (inForest ? 1.6f : 1f)) looks[y][x] = Rnd(x, y, 73) < 0.75f ? "tallgrass" : "flowers";   // walkable decoration
                }
            m.Looks = looks; m.Grounds = grounds;
            return m;
        }

        /// <summary>Batch 33, swamp concept: the authored marsh is one sheet of wading mud. Recompose it as
        /// sedge meadow (dry, walkable - the generic pass then grows birch groves, scrub and huts there),
        /// mud flats, reed beds lining the pools, and open water with drowned trees and a sunk lorry.
        /// The causeway's planked floor becomes a boardwalk. Only plain marsh cells are touched; landmarks keep their surroundings.</summary>
        static void Marsh(MapLayout m, string[][] looks, string[][] grounds, bool[,] keep, int W, int H,
                          System.Func<int, int, float, int, float> noise, System.Func<int, int, int, float> rnd)
        {
            bool Solid(int x, int y) => x >= 0 && y >= 0 && x < W && y < H && m.Tiles[y][x] == "Solid";
            bool Alone(int x, int y) { for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) if ((dx != 0 || dy != 0) && Solid(x + dx, y + dy)) return false; return true; }
            var deck = new bool[H, W];                                   // the causeway as authored (decided before anything changes)
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) deck[y, x] = grounds[y][x] == "Floor" && m.Tiles[y][x] == "Empty" && looks[y][x] == null;
            for (int y = 1; y < H - 1; y++)
                for (int x = 1; x < W - 1; x++)
                {
                    string t = m.Tiles[y][x], l = looks[y][x];
                    if (deck[y, x] && !keep[y, x])
                    {
                        // the upscaled causeway is 9 tiles of plank: keep a 3-wide boardwalk down its middle, the rest goes back to the marsh
                        bool Deck(int ax, int ay) => ay >= 0 && ay < H && ax >= 0 && ax < W && deck[ay, ax];
                        bool midNS = Deck(x, y - 3) && Deck(x, y + 3), midEW = Deck(x - 3, y) && Deck(x + 3, y) && x > 3 && x < W - 4;
                        bool runEW = Deck(x - 1, y) && Deck(x + 1, y);
                        if (runEW ? midNS : midEW) { looks[y][x] = "boardwalk"; continue; }
                        if (m.Heights != null) m.Heights[y][x] = 0;
                        t = m.Tiles[y][x] = "Difficult"; grounds[y][x] = null;
                    }
                    if (keep[y, x] || (m.Heights != null && m.Heights[y][x] != 0)) continue;
                    bool plain = t == "Difficult" && (l == null || l == "reeds" || l == "rubble") || t == "ScatterCover" && l == "reeds"
                              || t == "Solid" && l == "deadtree" || t == "Empty" && l == null;
                    if (!plain) continue;
                    float n = noise(x, y, 13f, 101) * 0.7f + noise(x, y, 4f, 103) * 0.3f, r = rnd(x, y, 107);
                    string tile, look = null, ground;
                    if (n > 0.57f)                       // open water
                    {
                        tile = "Difficult"; ground = "Water";
                        if (r < 0.012f && Alone(x, y)) { tile = "Solid"; look = "deadtree"; }
                        else if (r > 0.9985f && Alone(x, y)) { tile = "HardCover"; look = "sunktruck"; }
                    }
                    else if (n > 0.50f) { tile = r < 0.7f ? "ScatterCover" : "Difficult"; look = tile == "ScatterCover" ? "reeds" : null; ground = "Mud"; }   // reed beds on the banks
                    else if (n > 0.46f) { tile = "Difficult"; ground = "Mud"; }
                    else { tile = "Empty"; ground = noise(x, y, 9f, 109) < 0.55f ? "DarkGrass" : "Grass"; }   // sedge meadow
                    m.Tiles[y][x] = tile; looks[y][x] = look; grounds[y][x] = ground;
                }
        }

        static readonly HashSet<string> Built = new HashSet<string> { "wall", "plaster", "concrete", "plank", "metal", "window", "metaldoor" };

        /// <summary>Batch 33, city concept: the authored city is one asphalt sheet with blocks on it. Rebuild the
        /// streets from the blocks outward - a 2-tile pavement round every building, roadway out to 11 tiles,
        /// and anything further (big lots, the gaps) overgrown grass. Lamps along the kerb, a bus stop here
        /// and there, dumpsters and rubbish bags against the walls, dead buses in the road.</summary>
        static void City(MapLayout m, string[][] looks, string[][] grounds, bool[,] keep, int W, int H,
                         System.Func<int, int, float, int, float> noise, System.Func<int, int, int, float> rnd)
        {
            // Chebyshev distance to the nearest building wall/door (city blocks are rectangles: square rings = streets)
            const int Far = 99; var d = new int[H, W]; var q = new Queue<(int x, int y)>();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    bool wall = m.Tiles[y][x] == "Door" || m.Tiles[y][x] == "DoorOpen" || m.Tiles[y][x] == "Solid" && looks[y][x] != null && Built.Contains(looks[y][x]);
                    bool border = x == 0 || y == 0 || x == W - 1 || y == H - 1;
                    d[y, x] = wall && !border ? 0 : Far; if (d[y, x] == 0) q.Enqueue((x, y));
                }
            while (q.Count > 0)
            {
                var (x, y) = q.Dequeue();
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= W || ny >= H || d[ny, nx] <= d[y, x] + 1) continue;
                        d[ny, nx] = d[y, x] + 1; if (d[ny, nx] < 12) q.Enqueue((nx, ny));
                    }
            }
            var inside = Indoors(m, looks, W, H);
            // plank floor left out in the open (a stamped yard with no roof possible) is paving here too
            bool Plain(int x, int y) => m.Tiles[y][x] == "Empty" && looks[y][x] == null && !keep[y, x]
                && (grounds[y][x] == null || grounds[y][x] == "Asphalt" || grounds[y][x] == "Dirt" || grounds[y][x] == "Floor" && !inside[y, x]) && (m.Heights == null || m.Heights[y][x] == 0);
            var mine = new bool[H, W];
            for (int y = 1; y < H - 1; y++)
                for (int x = 1; x < W - 1; x++)
                {
                    if (!Plain(x, y)) continue;
                    mine[y, x] = true;
                    int k = d[y, x];
                    grounds[y][x] = k <= 2 ? "Concrete" : k <= 11 ? "Asphalt" : noise(x, y, 9f, 121) < 0.3f ? "Dirt" : noise(x, y, 6f, 123) < 0.5f ? "DarkGrass" : "Grass";
                    if (k > 11 && rnd(x, y, 125) < 0.25f) looks[y][x] = "tallgrass";
                }
            // the centre line: the middle row (column) of a straight stretch of road
            for (int y = 2; y < H - 2; y++)
                for (int x = 2; x < W - 2; x++)
                {
                    int k = d[y, x];
                    if (!mine[y, x] || grounds[y][x] != "Asphalt" || k < 5) continue;
                    bool ew = d[y, x - 1] == k && d[y, x + 1] == k && d[y - 1, x] < k && d[y + 1, x] <= k;
                    bool ns = d[y - 1, x] == k && d[y + 1, x] == k && d[y, x - 1] < k && d[y, x + 1] <= k;
                    if (ew || ns) looks[y][x] = "roadline";
                }
            // street furniture (after the ground, so neighbours are known)
            bool Kerb(int x, int y) => d[y, x] == 2 && grounds[y][x] == "Concrete";
            bool Near(int x, int y, string look, int r)
            {
                for (int yy = y - r; yy <= y + r; yy++) for (int xx = x - r; xx <= x + r; xx++)
                    if (xx >= 0 && yy >= 0 && xx < W && yy < H && looks[yy][xx] == look) return true;
                return false;
            }
            for (int y = 2; y < H - 2; y++)
                for (int x = 2; x < W - 2; x++)
                {
                    if (!mine[y, x] || looks[y][x] != null || m.Tiles[y][x] != "Empty") continue;
                    float r = rnd(x, y, 127);
                    if (Kerb(x, y) && r < 0.2f && !Near(x, y, "lamp", 5)) { looks[y][x] = "lamp"; continue; }              // walkable: a pole, not a wall
                    if (Kerb(x, y) && r > 0.985f && Kerb(x - 1, y) && Kerb(x + 1, y) && !Near(x, y, "busstop", 30))
                    { m.Tiles[y][x] = "HardCover"; looks[y][x] = "busstop"; continue; }
                    if (d[y, x] == 1 && grounds[y][x] == "Concrete" && r > 0.975f && !Near(x, y, "dumpster", 8)) { m.Tiles[y][x] = "HardCover"; looks[y][x] = "dumpster"; continue; }
                    if (d[y, x] == 1 && grounds[y][x] == "Concrete" && r < 0.04f) { m.Tiles[y][x] = "ScatterCover"; looks[y][x] = "trash"; continue; }
                    if (d[y, x] >= 5 && d[y, x] <= 8 && grounds[y][x] == "Asphalt" && r > 0.9975f && !Near(x, y, "bus", 20))
                    {
                        bool run = true;                                                              // a bus is 5 tiles long
                        for (int i = -2; i <= 2; i++) run &= mine[y, x + i] && m.Tiles[y][x + i] == "Empty" && grounds[y][x + i] == "Asphalt" && looks[y][x + i] == null;
                        if (!run) continue;
                        for (int i = -2; i <= 2; i++) { m.Tiles[y][x + i] = "HardCover"; looks[y][x + i] = i == 0 ? "bus" : "part"; }
                        for (int yy = y - 3; yy <= y + 3; yy++) for (int xx = x - 5; xx <= x + 5; xx++) if (xx >= 0 && xx < W && yy >= 0 && yy < H) keep[yy, xx] = true;   // no wreck parked into it
                    }
                }
        }

        /// <summary>Batch 33, industrial concept: weeds breaking up the slab yard, rusty storage tanks in pairs,
        /// overhead pipelines on concrete posts (walk under them), and a rail spur with a flatcar.</summary>
        static void Industrial(MapLayout m, string[][] looks, string[][] grounds, bool[,] keep, int W, int H,
                               System.Func<int, int, float, int, float> noise, System.Func<int, int, int, float> rnd)
        {
            var inside = Indoors(m, looks, W, H);
            bool Plain(int x, int y) => x >= 1 && y >= 1 && x < W - 1 && y < H - 1 && m.Tiles[y][x] == "Empty" && looks[y][x] == null && !keep[y, x] && !inside[y, x]
                && (grounds[y][x] == null || grounds[y][x] == "Concrete" || grounds[y][x] == "Gravel") && (m.Heights == null || m.Heights[y][x] == 0);
            bool Clear(int x0, int y0, int w, int h) { for (int y = y0; y < y0 + h; y++) for (int x = x0; x < x0 + w; x++) if (!Plain(x, y)) return false; return true; }
            void Keep(int x0, int y0, int w, int h) { for (int y = y0; y < y0 + h; y++) for (int x = x0; x < x0 + w; x++) if (x >= 0 && y >= 0 && x < W && y < H) keep[y, x] = true; }
            void Block(int x0, int y0, int w, int h, string tile, string look, int ax, int ay)
            {
                for (int y = y0; y < y0 + h; y++) for (int x = x0; x < x0 + w; x++) { m.Tiles[y][x] = tile; looks[y][x] = x == ax && y == ay ? look : "part"; }
            }
            // tank pairs (each tank 2x2, drawn from its bottom-left cell)
            for (int i = 0, placed = 0; i < 200 && placed < 4; i++)
            {
                int x = 3 + (int)(rnd(i, 1, 141) * (W - 12)), y = 3 + (int)(rnd(i, 2, 141) * (H - 8));
                if (!Clear(x - 2, y - 2, 9, 6)) continue;
                Block(x, y, 2, 2, "Solid", "tank", x, y + 1); Block(x + 3, y, 2, 2, "Solid", "tank", x + 3, y + 1);
                Keep(x - 3, y - 3, 11, 8); placed++;
            }
            // rail spur: the longest clear row in the middle half, if it's long enough
            int bestY = -1, bestX = 0, bestLen = 0;
            for (int y = H / 4; y < H * 3 / 4; y++)
                for (int x = 1, run = 0; x < W - 1; x++)
                {
                    run = Clear(x, y - 1, 1, 3) ? run + 1 : 0;
                    if (run > bestLen) { bestLen = run; bestY = y; bestX = x - run + 1; }
                }
            if (bestLen >= W / 3)
            {
                for (int x = bestX; x < bestX + bestLen; x++) looks[bestY][x] = "rail";
                int fx = bestX + bestLen / 3;
                Block(fx - 1, bestY, 4, 1, "HardCover", "flatcar", fx, bestY);
                Keep(bestX, bestY - 2, bestLen, 5);
            }
            // overhead pipelines
            for (int i = 0, runs = 0; i < 60 && runs < 3; i++)
            {
                int x0 = 8 + (int)(rnd(i, 3, 143) * (W - 26)), y = 8 + (int)(rnd(i, 4, 143) * (H - 16)), len = 0;
                while (x0 + len < W - 2 && len < 34 && Clear(x0 + len, y, 1, 1)) len++;
                if (len < 12) continue;
                for (int k = 0; k < len; k++) looks[y][x0 + k] = k % 4 == 1 ? "pipepost" : "pipe";
                Keep(x0, y - 1, len, 3); runs++;
            }
            // weeds and gravel breaking up the slabs
            for (int y = 1; y < H - 1; y++)
                for (int x = 1; x < W - 1; x++)
                {
                    if (!Plain(x, y)) continue;
                    float n = noise(x, y, 16f, 131) * 0.75f + noise(x, y, 5f, 133) * 0.25f;
                    if (n > 0.63f) { grounds[y][x] = n > 0.68f ? "DarkGrass" : "Grass"; if (rnd(x, y, 135) < 0.3f) looks[y][x] = "tallgrass"; }
                    else if (n > 0.58f) grounds[y][x] = "Gravel";
                    else if (rnd(x, y, 137) < 0.015f) looks[y][x] = "tallgrass";             // a tuft in a slab joint
                }
        }

        /// <summary>Cells walled in by buildings: whatever a flood fill from the map edge can't reach without
        /// passing a wall or a door.</summary>
        public static bool[,] Indoors(MapLayout m, string[][] looks, int W, int H)
        {
            var seen = new bool[H, W]; var q = new Queue<(int x, int y)>();
            bool Wall(int x, int y) => m.Tiles[y][x] == "Door" || m.Tiles[y][x] == "DoorOpen" || m.Tiles[y][x] == "Solid" && looks[y][x] != null && Built.Contains(looks[y][x]);
            // seeds: open cells just inside the border wall (the border itself is walled too)
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) if ((x < 5 || y < 5 || x >= W - 5 || y >= H - 5) && !Wall(x, y)) q.Enqueue((x, y));
            while (q.Count > 0)
            {
                var (x, y) = q.Dequeue();
                if (x < 0 || y < 0 || x >= W || y >= H || seen[y, x] || Wall(x, y)) continue;
                seen[y, x] = true;
                q.Enqueue((x + 1, y)); q.Enqueue((x - 1, y)); q.Enqueue((x, y + 1)); q.Enqueue((x, y - 1));
            }
            var inside = new bool[H, W];
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) inside[y, x] = !seen[y, x] && !Wall(x, y);
            return inside;
        }

        static string[][] NewGrid(int w, int h)
        {
            var g = new string[h][];
            for (int y = 0; y < h; y++) g[y] = new string[w];
            return g;
        }
    }
}
