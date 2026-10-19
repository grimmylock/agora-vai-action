// Batch 30: bigger maps. Every authored zone (ZoneLayoutRecipes) is drawn on a small canvas and
// then blown up here by an integer factor, feature-aware, so the world gets S x S times the area
// without walls turning into fat blocks:
//   - floors, heights, water/rubble, platforms and natural masses (cliffs, hedges, scrap piles)
//     fill the whole S x S block;
//   - built walls, doors, fences, sandbag/jersey lines, rails and road lines keep 1 tile of
//     thickness: a "skeleton" through the block's centre joins the neighbours of the same kind;
//   - props (trees, wrecks, crates, rocks...) sit once per block at a jittered spot, and forests
//     get an extra tree now and then so a tree line stays a tree line;
//   - landmark / exit anchors move to the centre of their block.
// Deterministic per zone (seeded hash), so a zone looks the same every visit.
using System.Collections.Generic;
using GameData;

namespace GameCore
{
    public static class MapUpscaler
    {
        public const int Scale = 3;             // overworld zones
        public const int InteriorScale = 2;     // hidden caves / labs / tunnels stay tighter

        static readonly HashSet<string> BuiltWalls = new HashSet<string> { "wall", "plaster", "concrete", "window", "hull", "metal", "metaldoor", "plank" };
        static readonly HashSet<string> NaturalMass = new HashSet<string> { "cliff", "foliage", "redfoliage", "junk" };
        static readonly HashSet<string> LineLooks = new HashSet<string> { "fence", "fence_v", "fence_torn", "sandbags", "jersey", "rail", "rail_v", "roadline", "trench", "barrierpole", "pipes" };
        static readonly HashSet<string> Trees = new HashSet<string> { "tree", "deadtree", "redtree" };

        enum Kind { Fill, Struct, Skeleton, Prop }

        public static MapLayout Upscale(MapLayout src, string zoneId, int s)
        {
            if (src?.Tiles == null || s <= 1) return src;
            int h = src.Tiles.Length, w = src.Tiles[0].Length;
            int W = w * s, H = h * s;
            var theme = ZoneThemes.For(zoneId);
            int seed = TerrainCanvas.Seed(zoneId ?? "x");

            string T(int x, int y) => x < 0 || y < 0 || x >= w || y >= h ? "Solid" : src.Tiles[y][x];
            string L(int x, int y) => x < 0 || y < 0 || x >= w || y >= h ? null : src.Looks?[y][x];
            bool IsSolid(int x, int y) => T(x, y) == "Solid";

            // what a source cell is: built structure (walls, doors, natural masses), a line of cover, floor, or a prop
            string WallLookOf(int x, int y)
            {
                string t = T(x, y), l = L(x, y);
                if (t == "Door" || t == "DoorOpen") return "door";
                if (t != "Solid") return null;
                if (l != null) return BuiltWalls.Contains(l) || NaturalMass.Contains(l) ? l : null;
                if (x <= 0 || y <= 0 || x >= w - 1 || y >= h - 1) return theme.Border ?? "wall";
                int n = (IsSolid(x - 1, y) ? 1 : 0) + (IsSolid(x + 1, y) ? 1 : 0) + (IsSolid(x, y - 1) ? 1 : 0) + (IsSolid(x, y + 1) ? 1 : 0);
                return n >= 2 ? theme.Mass ?? "wall" : null;
            }
            bool IsStruct(int x, int y) => x < 0 || y < 0 || x >= w || y >= h || WallLookOf(x, y) != null;
            Kind KindOf(int x, int y)
            {
                string t = T(x, y), l = L(x, y);
                if (WallLookOf(x, y) != null) return Kind.Struct;
                if (l != null && LineLooks.Contains(l)) return Kind.Skeleton;
                return t == "Empty" || t == "Difficult" ? Kind.Fill : Kind.Prop;    // floors, platforms, water, rubble / cover, barrels, trees
            }
            bool Joins(int x, int y, int nx, int ny)
            {
                string la = L(x, y), lb = L(nx, ny);
                if (la == null || lb == null) return false;
                string Base(string k) => k == "fence_v" || k == "fence_torn" ? "fence" : k == "rail_v" ? "rail" : k;
                return LineLooks.Contains(la) && Base(la) == Base(lb) && T(x, y) == T(nx, ny);
            }

            var tiles = new string[H][]; var heights = src.Heights != null ? new int[H][] : null;
            var looks = new string[H][]; var grounds = src.Grounds != null ? new string[H][] : null;
            for (int y = 0; y < H; y++)
            {
                tiles[y] = new string[W]; looks[y] = new string[W];
                if (heights != null) heights[y] = new int[W];
                if (grounds != null) grounds[y] = new string[W];
            }
            int c = s / 2;
            for (int sy = 0; sy < h; sy++)
                for (int sx = 0; sx < w; sx++)
                {
                    string t = T(sx, sy), l = L(sx, sy);
                    var kind = KindOf(sx, sy);
                    int hv = src.Heights != null && sy < src.Heights.Length && sx < src.Heights[sy].Length ? src.Heights[sy][sx] : 0;
                    string g = src.Grounds?[sy][sx];
                    // the floor under a block: what's around it, so a lone prop doesn't sit on a 3x3 "wall floor"
                    for (int by = 0; by < s; by++)
                        for (int bx = 0; bx < s; bx++)
                        {
                            int X = sx * s + bx, Y = sy * s + by;
                            tiles[Y][X] = kind == Kind.Fill ? t : "Empty";
                            looks[Y][X] = kind == Kind.Fill ? l : null;
                            if (heights != null) heights[Y][X] = hv;
                            if (grounds != null) grounds[Y][X] = g;
                        }
                    if (kind == Kind.Struct)
                    {
                        // keep built walls thin and masses solid: a sub-cell is wall when every source
                        // neighbour in its direction is structure too (centre always; edges need that
                        // side's neighbour; corners need both sides and the diagonal)
                        bool door = t == "Door" || t == "DoorOpen";
                        for (int by = 0; by < s; by++)
                            for (int bx = 0; bx < s; bx++)
                            {
                                int dx = System.Math.Sign(bx - c), dy = System.Math.Sign(by - c);
                                bool on = dx == 0 && dy == 0
                                    || (dx == 0 || dy == 0) && IsStruct(sx + dx, sy + dy)
                                    || dx != 0 && dy != 0 && IsStruct(sx + dx, sy) && IsStruct(sx, sy + dy) && IsStruct(sx + dx, sy + dy);
                                if (!on) continue;
                                int X = sx * s + bx, Y = sy * s + by;
                                bool doorSide = (dx == 0 || T(sx + dx, sy) == "Door" || T(sx + dx, sy) == "DoorOpen")
                                             && (dy == 0 || T(sx, sy + dy) == "Door" || T(sx, sy + dy) == "DoorOpen")
                                             && (dx == 0 || dy == 0 || T(sx + dx, sy + dy) == "Door" || T(sx + dx, sy + dy) == "DoorOpen");
                                if (door && (dx != 0 || dy != 0) && !doorSide)
                                {
                                    // the door itself is one tile; the rest of its block is the wall it sits in
                                    string nl = WallLookOf(sx + dx, sy + dy);
                                    tiles[Y][X] = "Solid"; looks[Y][X] = nl == "door" || nl == null ? L(sx + dx, sy + dy) ?? theme.Mass : nl;
                                    if (looks[Y][X] != null && !BuiltWalls.Contains(looks[Y][X]) && !NaturalMass.Contains(looks[Y][X])) looks[Y][X] = null;
                                }
                                else { tiles[Y][X] = t; looks[Y][X] = l; }
                            }
                    }
                    else if (kind == Kind.Skeleton)
                    {
                        void Arm(int bx, int by) { int X = sx * s + bx, Y = sy * s + by; tiles[Y][X] = t; looks[Y][X] = l; }
                        Arm(c, c);
                        if (Joins(sx, sy, sx, sy - 1)) for (int by = 0; by < c; by++) Arm(c, by);
                        if (Joins(sx, sy, sx, sy + 1)) for (int by = c + 1; by < s; by++) Arm(c, by);
                        if (Joins(sx, sy, sx - 1, sy)) for (int bx = 0; bx < c; bx++) Arm(bx, c);
                        if (Joins(sx, sy, sx + 1, sy)) for (int bx = c + 1; bx < s; bx++) Arm(bx, c);
                    }
                    else if (kind == Kind.Prop)
                    {
                        // one prop per block, nudged off-centre so rows of props don't line up on a lattice
                        bool big = l == "container" || l == "wreck";          // block-sized: rows of them stay rows
                        int jx = big ? c : c + (int)System.Math.Round((ZoneThemes.Hash01(sx, sy, seed) - 0.5f) * (s - 1));
                        int jy = big ? c : c + (int)System.Math.Round((ZoneThemes.Hash01(sx, sy, seed + 1) - 0.5f) * (s - 1));
                        tiles[sy * s + jy][sx * s + jx] = t; looks[sy * s + jy][sx * s + jx] = l;
                        bool tree = l != null && Trees.Contains(l) || (l == null && t == "Solid" && System.Array.IndexOf(theme.Props ?? new string[0], "tree") >= 0 && ZoneThemes.Hash01(sx, sy, seed + 5) < 0.5f);
                        if (tree && ZoneThemes.Hash01(sx, sy, seed + 2) < 0.55f)
                        {
                            int ex = (jx + 1 + (int)(ZoneThemes.Hash01(sx, sy, seed + 3) * (s - 1))) % s;
                            int ey = (jy + 1 + (int)(ZoneThemes.Hash01(sx, sy, seed + 4) * (s - 1))) % s;
                            if (System.Math.Abs(ex - jx) + System.Math.Abs(ey - jy) >= 2) { tiles[sy * s + ey][sx * s + ex] = t; looks[sy * s + ey][sx * s + ex] = l; }
                        }
                    }
                }
            var anchors = new Dictionary<string, (int x, int y)>();
            if (src.Anchors != null)
                foreach (var kv in src.Anchors) anchors[kv.Key] = (kv.Value.x * s + c, kv.Value.y * s + c);
            // looks array only when something is set (matches TerrainCanvas.ToMapLayout)
            bool anyLook = false;
            for (int y = 0; y < H && !anyLook; y++) for (int x = 0; x < W; x++) if (looks[y][x] != null) { anyLook = true; break; }
            return new MapLayout { Tiles = tiles, Heights = heights, Looks = anyLook ? looks : null, Grounds = grounds, Anchors = anchors };
        }
    }
}
