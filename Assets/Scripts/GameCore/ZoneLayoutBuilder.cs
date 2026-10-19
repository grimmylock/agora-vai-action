// Small procedural canvas for authoring GameData.MapLayout entries without hand-typing literal
// string grids. Each zone recipe (see ZoneLayoutRecipes.cs) builds one of these by stamping a
// handful of named shapes (rect/blob/ring/line/scatter/random-walk) matching that zone's
// described terrain and landmark list from 06_Zone_Map.md, then converts to a MapLayout.
//
// Deterministic per zone (seeded by zone id) so a given zone's authored terrain is stable across
// runs/rebuilds - only the *procedural fallback* scatter (TerrainMap.ApplyProceduralObstacles,
// used only when a zone has no recipe here at all) is truly random.
using System;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public class TerrainCanvas
    {
        public readonly int W, H;
        private readonly TileType[,] _tile;   // [y, x]
        private readonly int[,] _height;      // [y, x]
        private readonly string[,] _look;     // [y, x] art key, null = theme default
        private readonly GroundType?[,] _ground; // [y, x] null = theme default
        private readonly System.Collections.Generic.Dictionary<string, (int x, int y)> _anchors = new System.Collections.Generic.Dictionary<string, (int x, int y)>();
        private readonly System.Random _rng;
        private string[,] _region;            // tier maps: region (old zone id) per cell, null = none
        private bool[,] _keep;                // tier maps: the dresser leaves these cells as authored
        private readonly int _seed;

        public TerrainCanvas(int width, int height, int seed)
        {
            W = width;
            H = height;
            _tile = new TileType[H, W];
            _height = new int[H, W];
            _look = new string[H, W];
            _ground = new GroundType?[H, W];
            _rng = new System.Random(seed);
            _seed = seed;
        }

        private bool InBounds(int x, int y) => x >= 0 && x < W && y >= 0 && y < H;

        private void Set(int x, int y, TileType t, int height, string look)
        {
            _tile[y, x] = t;
            _height[y, x] = height;
            _look[y, x] = look;
        }

        public TileType Get(int x, int y) => InBounds(x, y) ? _tile[y, x] : TileType.Solid;

        public TerrainCanvas FillAll(TileType t, int height = 0)
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    Set(x, y, t, height, null);
                }
            return this;
        }

        public TerrainCanvas Rect(int x, int y, int w, int h, TileType t, int height = 0, string look = null)
        {
            for (int yy = y; yy < y + h; yy++)
                for (int xx = x; xx < x + w; xx++)
                    if (InBounds(xx, yy)) Set(xx, yy, t, height, look);
            return this;
        }

        /// <summary>Sets only elevation over a rect, leaving tile type (passability) untouched -
        /// for shaping walkable high ground (ridges, terraces, hills) that isn't itself cover.</summary>
        public TerrainCanvas RectHeight(int x, int y, int w, int h, int height)
        {
            for (int yy = y; yy < y + h; yy++)
                for (int xx = x; xx < x + w; xx++)
                    if (InBounds(xx, yy)) _height[yy, xx] = height;
            return this;
        }

        public TerrainCanvas Border(TileType t, int thickness = 1, int height = 0, string look = null)
        {
            Rect(0, 0, W, thickness, t, height, look);
            Rect(0, H - thickness, W, thickness, t, height, look);
            Rect(0, 0, thickness, H, t, height, look);
            Rect(W - thickness, 0, thickness, H, t, height, look);
            return this;
        }

        public TerrainCanvas Blob(int cx, int cy, int radius, TileType t, int height = 0, float roughness = 0.25f, string look = null)
        {
            for (int yy = cy - radius; yy <= cy + radius; yy++)
                for (int xx = cx - radius; xx <= cx + radius; xx++)
                {
                    if (!InBounds(xx, yy)) continue;
                    float d = Mathf.Sqrt((xx - cx) * (xx - cx) + (yy - cy) * (yy - cy));
                    float wobble = 1f + ((float)_rng.NextDouble() - 0.5f) * roughness;
                    if (d <= radius * wobble) Set(xx, yy, t, height, look);
                }
            return this;
        }

        public TerrainCanvas HeightBlob(int cx, int cy, int radius, int height, float roughness = 0.25f)
        {
            for (int yy = cy - radius; yy <= cy + radius; yy++)
                for (int xx = cx - radius; xx <= cx + radius; xx++)
                {
                    if (!InBounds(xx, yy)) continue;
                    float d = Mathf.Sqrt((xx - cx) * (xx - cx) + (yy - cy) * (yy - cy));
                    float wobble = 1f + ((float)_rng.NextDouble() - 0.5f) * roughness;
                    if (d <= radius * wobble) _height[yy, xx] = height;
                }
            return this;
        }

        public TerrainCanvas Ring(int cx, int cy, int innerR, int outerR, TileType t, int height = 0, string look = null)
        {
            for (int yy = cy - outerR; yy <= cy + outerR; yy++)
                for (int xx = cx - outerR; xx <= cx + outerR; xx++)
                {
                    if (!InBounds(xx, yy)) continue;
                    float d = Mathf.Sqrt((xx - cx) * (xx - cx) + (yy - cy) * (yy - cy));
                    if (d >= innerR && d <= outerR) Set(xx, yy, t, height, look);
                }
            return this;
        }

        /// <summary>Stamps a thick line between two points - used for causeways, rail lines,
        /// trenches, corridors. overwrite=true always sets the tile (e.g. carving a guaranteed
        /// walkable path through cover); overwrite=false only stamps onto tiles that are still the
        /// canvas' initial Empty (won't punch through walls already placed).</summary>
        public TerrainCanvas Line(int x1, int y1, int x2, int y2, int thickness, TileType t, int height = 0, bool overwrite = true, string look = null)
        {
            int steps = Mathf.Max(Mathf.Abs(x2 - x1), Mathf.Abs(y2 - y1));
            if (steps == 0) steps = 1;
            for (int i = 0; i <= steps; i++)
            {
                float f = (float)i / steps;
                int cx = Mathf.RoundToInt(Mathf.Lerp(x1, x2, f));
                int cy = Mathf.RoundToInt(Mathf.Lerp(y1, y2, f));
                int r = thickness / 2;
                for (int yy = cy - r; yy <= cy + r; yy++)
                    for (int xx = cx - r; xx <= cx + r; xx++)
                    {
                        if (!InBounds(xx, yy)) continue;
                        if (!overwrite && _tile[yy, xx] != TileType.Empty) continue;
                        Set(xx, yy, t, height, look);
                    }
            }
            return this;
        }

        /// <summary>Carves a connected, organically-shaped Empty path via a random walk from a
        /// start point - guarantees walkable connectivity, used for cave/tunnel/maze interiors
        /// where a straight Line would look too artificial.</summary>
        public TerrainCanvas RandomWalk(int startX, int startY, int steps, int brush, TileType t = TileType.Empty, int height = 0)
        {
            int x = startX, y = startY;
            for (int i = 0; i < steps; i++)
            {
                int r = brush / 2;
                for (int yy = y - r; yy <= y + r; yy++)
                    for (int xx = x - r; xx <= x + r; xx++)
                        if (InBounds(xx, yy)) Set(xx, yy, t, height, null);

                switch (_rng.Next(4))
                {
                    case 0: x++; break;
                    case 1: x--; break;
                    case 2: y++; break;
                    default: y--; break;
                }
                x = Mathf.Clamp(x, 1, W - 2);
                y = Mathf.Clamp(y, 1, H - 2);
            }
            return this;
        }

        /// <summary>Randomly re-types a fraction of tiles that are still the given base type -
        /// e.g. scattering ScatterCover across open ground without touching walls/paths already
        /// placed by earlier stamps.</summary>
        public TerrainCanvas Scatter(TileType t, float density, TileType onlyOnBase = TileType.Empty, int height = 0, string look = null)
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    if (_tile[y, x] == onlyOnBase && _look[y, x] == null && _rng.NextDouble() < density)
                        Set(x, y, t, _height[y, x] + height, look);
            return this;
        }

        /// <summary>Like Scatter, but only inside a rect - for clustering cover around a landmark.</summary>
        public TerrainCanvas ScatterIn(int x, int y, int w, int h, TileType t, float density, string look = null)
        {
            for (int yy = y; yy < y + h; yy++)
                for (int xx = x; xx < x + w; xx++)
                    if (InBounds(xx, yy) && _tile[yy, xx] == TileType.Empty && _look[yy, xx] == null && _rng.NextDouble() < density)
                        Set(xx, yy, t, _height[yy, xx], look);
            return this;
        }

        // ------------------------------------------------------------ looks, floors, buildings

        /// <summary>One tile: a prop (barrel, crate, wreck...) or a decoration on open ground.</summary>
        public TerrainCanvas Put(int x, int y, TileType t, string look = null)
        {
            if (InBounds(x, y)) Set(x, y, t, _height[y, x], look);
            return this;
        }

        /// <summary>Sets only the art key, leaving rules untouched (decorations on walkable tiles).</summary>
        public TerrainCanvas Deco(int x, int y, string look)
        {
            if (InBounds(x, y)) _look[y, x] = look;
            return this;
        }

        public TerrainCanvas Paint(int x, int y, int w, int h, GroundType g)
        {
            for (int yy = y; yy < y + h; yy++)
                for (int xx = x; xx < x + w; xx++)
                    if (InBounds(xx, yy)) _ground[yy, xx] = g;
            return this;
        }

        public TerrainCanvas PaintBlob(int cx, int cy, int radius, GroundType g, float roughness = 0.35f)
        {
            for (int yy = cy - radius; yy <= cy + radius; yy++)
                for (int xx = cx - radius; xx <= cx + radius; xx++)
                {
                    if (!InBounds(xx, yy)) continue;
                    float d = Mathf.Sqrt((xx - cx) * (xx - cx) + (yy - cy) * (yy - cy));
                    float wobble = 1f + ((float)_rng.NextDouble() - 0.5f) * roughness;
                    if (d <= radius * wobble) _ground[yy, xx] = g;
                }
            return this;
        }

        public TerrainCanvas PaintLine(int x1, int y1, int x2, int y2, int thickness, GroundType g)
        {
            int steps = Mathf.Max(1, Mathf.Max(Mathf.Abs(x2 - x1), Mathf.Abs(y2 - y1)));
            for (int i = 0; i <= steps; i++)
            {
                float f = (float)i / steps;
                int cx = Mathf.RoundToInt(Mathf.Lerp(x1, x2, f)), cy = Mathf.RoundToInt(Mathf.Lerp(y1, y2, f));
                int r = thickness / 2;
                for (int yy = cy - r; yy <= cy - r + thickness - 1; yy++)
                    for (int xx = cx - r; xx <= cx - r + thickness - 1; xx++)
                        if (InBounds(xx, yy)) _ground[yy, xx] = g;
            }
            return this;
        }

        /// <summary>A walled room at the ground's existing height: Solid perimeter in `wallLook`, walkable interior with `floor`,
        /// and a closed Door at each given absolute position (which must sit on the wall).</summary>
        public TerrainCanvas Building(int x, int y, int w, int h, string wallLook, GroundType floor, params (int x, int y)[] doors)
        {
            int ground = InBounds(x, y) ? _height[y, x] : 0;   // sits on whatever ground is there
            Rect(x, y, w, h, TileType.Solid, ground, wallLook);
            Rect(x + 1, y + 1, w - 2, h - 2, TileType.Empty, ground);
            Paint(x, y, w, h, floor);
            foreach (var d in doors) Put(d.x, d.y, TileType.Door, wallLook == "concrete" ? "metal" : null);
            return this;
        }

        /// <summary>Stamps one of the design's exact landmark room-grids (RoomGrids.cs, from
        /// 06_Zone_Map.md) with its top-left at (x0,y0), each grid cell `scale` x `scale` tiles.
        /// # wall in `wallLook`, C/= cover, ~ difficult, D door, ^ +2 elevation, H set-piece
        /// machine, T trader crates, everything walkable gets `floor` when given.</summary>
        public TerrainCanvas Stamp(string gridName, int x0, int y0, int scale = 2, string wallLook = null, GroundType? floor = null,
            int baseHeight = 0, string coverLook = null, string hardLook = null)
        {
            if (!RoomGrids.All.TryGetValue(gridName, out var rows))
            {
                Debug.LogWarning($"[MAP] No room-grid named \"{gridName}\".");
                return this;
            }
            for (int gy = 0; gy < rows.Length; gy++)
                for (int gx = 0; gx < rows[gy].Length; gx++)
                {
                    char ch = rows[gy][gx];
                    for (int sy = 0; sy < scale; sy++)
                        for (int sx = 0; sx < scale; sx++)
                        {
                            int x = x0 + gx * scale + sx, y = y0 + gy * scale + sy;
                            if (!InBounds(x, y)) continue;
                            switch (ch)
                            {
                                case '#': Set(x, y, TileType.Solid, baseHeight, wallLook); break;
                                case 'C': Set(x, y, TileType.ScatterCover, baseHeight, coverLook); break;
                                case '=': Set(x, y, TileType.HardCover, baseHeight, hardLook); break;
                                case '~': Set(x, y, TileType.Difficult, baseHeight, null); break;
                                case 'D': Set(x, y, TileType.Door, baseHeight, wallLook == "concrete" ? "metal" : null); break;
                                case '^': Set(x, y, TileType.Empty, baseHeight + 2, "platform"); break;
                                case 'H': Set(x, y, TileType.Solid, baseHeight, "machine"); break;
                                case 'T': Set(x, y, TileType.ScatterCover, baseHeight, "crate"); break;
                                case '!': Set(x, y, TileType.Empty, baseHeight, null); _ground[y, x] = GroundType.Mud; continue;
                                default: Set(x, y, TileType.Empty, baseHeight, null); break;
                            }
                            if (floor.HasValue) _ground[y, x] = floor.Value;
                        }
                }
            return this;
        }

        /// <summary>A walkable slope of `length` tiles from (x0,y0) stepping (dx,dy), climbing evenly
        /// from `fromHeight` to `toHeight` - how ridges, towers and terraces get reached (climbing
        /// costs +1 MP per height gained, so no single step should rise more than 2).</summary>
        public TerrainCanvas Ramp(int x0, int y0, int dx, int dy, int length, int fromHeight, int toHeight, int width = 1, string look = null)
        {
            for (int i = 0; i < length; i++)
            {
                int h = fromHeight + (int)System.Math.Round((toHeight - fromHeight) * (i + 1) / (double)(length + 1));
                for (int k = 0; k < width; k++)
                {
                    int x = x0 + dx * i + (dx == 0 ? k : 0), y = y0 + dy * i + (dy == 0 ? k : 0);
                    if (InBounds(x, y)) Set(x, y, TileType.Empty, h, look);
                }
            }
            return this;
        }

        /// <summary>Pins the walkable exit to a connected zone where the design's map-sketch puts it
        /// (see ZoneExitPlacer) instead of a hashed spot on the border.</summary>
        public TerrainCanvas Exit(string zoneId, int x, int y) => Landmark("exit:" + zoneId, x, y);

        /// <summary>Pins a LandmarkDatabase entry to a tile instead of letting it scatter randomly.</summary>
        public TerrainCanvas Landmark(string name, int x, int y)
        {
            _anchors[name] = (x, y);
            return this;
        }


        // ------------------------------------------------------------ tier maps (TierMaps.cs)

        public int HeightAt(int x, int y) => InBounds(x, y) ? _height[y, x] : 0;
        public string LookAt(int x, int y) => InBounds(x, y) ? _look[y, x] : null;
        public GroundType? GroundOf(int x, int y) => InBounds(x, y) ? _ground[y, x] : null;
        public int Next(int max) => _rng.Next(max);
        public float Roll() => (float)_rng.NextDouble();

        /// <summary>Smooth 0..1 value noise, stable per canvas: for organic region borders, ground patches, forests.</summary>
        public float Noise(float x, float y, float cell, int k)
        {
            float fx = x / cell, fy = y / cell;
            int ix = (int)System.Math.Floor(fx), iy = (int)System.Math.Floor(fy);
            float tx = fx - ix, ty = fy - iy; tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
            float a = ZoneThemes.Hash01(ix, iy, _seed + k), b = ZoneThemes.Hash01(ix + 1, iy, _seed + k);
            float c = ZoneThemes.Hash01(ix, iy + 1, _seed + k), d = ZoneThemes.Hash01(ix + 1, iy + 1, _seed + k);
            float top = a + (b - a) * tx, bot = c + (d - c) * tx;
            return top + (bot - top) * ty;
        }

        /// <summary>Assigns every cell to a region (an old zone id) - the HUD, quests, factions and the
        /// dresser's biome all read it.</summary>
        public TerrainCanvas Regions(Func<int, int, string> regionOf)
        {
            _region = _region ?? new string[H, W];
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) _region[y, x] = regionOf(x, y);
            return this;
        }

        public TerrainCanvas RegionRect(string id, int x, int y, int w, int h)
        {
            _region = _region ?? new string[H, W];
            for (int yy = y; yy < y + h; yy++) for (int xx = x; xx < x + w; xx++) if (InBounds(xx, yy)) _region[yy, xx] = id;
            return this;
        }

        public string RegionAt(int x, int y) => _region != null && InBounds(x, y) ? _region[y, x] : null;

        /// <summary>Marks cells the dresser must not touch (roads, yards, streets).</summary>
        /// <summary>Batch 97: kept by an earlier helper (a ruin, a yard) - later passes like hedgerows leave it alone.</summary>
        public bool Kept(int x, int y) => _keep != null && InBounds(x, y) && _keep[y, x];

        public TerrainCanvas Keep(int x, int y, int w, int h)
        {
            _keep = _keep ?? new bool[H, W];
            for (int yy = y; yy < y + h; yy++) for (int xx = x; xx < x + w; xx++) if (InBounds(xx, yy)) _keep[yy, xx] = true;
            return this;
        }

        /// <summary>Open ground: walkable, no prop, height kept; optional floor; kept from the dresser.</summary>
        public TerrainCanvas Clear(int x, int y, int w, int h, GroundType? floor = null, bool keep = true)
        {
            for (int yy = y; yy < y + h; yy++)
                for (int xx = x; xx < x + w; xx++)
                    if (InBounds(xx, yy)) { _tile[yy, xx] = TileType.Empty; _look[yy, xx] = null; if (floor.HasValue) _ground[yy, xx] = floor.Value; }
            if (keep) Keep(x, y, w, h);
            return this;
        }

        /// <summary>Paints `g` where the canvas noise is above `threshold` (patches of mud, dirt, dark grass...).</summary>
        public TerrainCanvas PaintNoise(GroundType g, float cell, float threshold, int k, Func<int, int, bool> where = null)
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    if ((where == null || where(x, y)) && Noise(x, y, cell, k) * 0.7f + Noise(x, y, cell / 3f, k + 1) * 0.3f > threshold) _ground[y, x] = g;
            return this;
        }

        public TerrainCanvas PaintWhere(GroundType g, Func<int, int, bool> where)
        {
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) if (where(x, y)) _ground[y, x] = g;
            return this;
        }

        /// <summary>A road or track along a polyline: clears the way, paints the surface, keeps it from the dresser.
        /// Asphalt roads of width 5+ get a broken centre line on their straight stretches.</summary>
        public TerrainCanvas Road(int width, GroundType surface, params (int x, int y)[] pts)
        {
            for (int i = 0; i + 1 < pts.Length; i++)
            {
                var (x1, y1) = pts[i]; var (x2, y2) = pts[i + 1];
                int steps = System.Math.Max(1, System.Math.Max(System.Math.Abs(x2 - x1), System.Math.Abs(y2 - y1)));
                int r = width / 2;
                for (int s = 0; s <= steps; s++)
                {
                    int cx = (int)System.Math.Round(x1 + (x2 - x1) * s / (double)steps), cy = (int)System.Math.Round(y1 + (y2 - y1) * s / (double)steps);
                    for (int yy = cy - r; yy <= cy - r + width - 1; yy++)
                        for (int xx = cx - r; xx <= cx - r + width - 1; xx++)
                            if (InBounds(xx, yy)) { _tile[yy, xx] = TileType.Empty; _look[yy, xx] = null; _ground[yy, xx] = surface; Keep(xx, yy, 1, 1); }
                    bool straight = x1 == x2 || y1 == y2;
                    if (surface == GroundType.Asphalt && width >= 5 && straight && s % 4 < 2 && InBounds(cx, cy)) _look[cy, cx] = "roadline";
                }
            }
            return this;
        }

        /// <summary>A fence line (light cover) between two points; `gaps` = how many tiles at the midpoint are left open.</summary>
        public TerrainCanvas Fence(int x1, int y1, int x2, int y2, string look = "fence", int gapAt = -1, int gap = 0, TileType t = TileType.ScatterCover)
        {
            int steps = System.Math.Max(System.Math.Abs(x2 - x1), System.Math.Abs(y2 - y1));
            bool vertical = x1 == x2;
            for (int i = 0; i <= steps; i++)
            {
                int x = x1 + (x2 - x1) * i / System.Math.Max(1, steps), y = y1 + (y2 - y1) * i / System.Math.Max(1, steps);
                if (!InBounds(x, y)) continue;
                if (gapAt >= 0 && i >= gapAt && i < gapAt + gap) continue;
                string lk = look == "fence" && vertical ? "fence_v" : look;
                Set(x, y, t, _height[y, x], lk);
            }
            return this;
        }

        /// <summary>Trees (or rocks...) scattered in a rect on open ground, each with all 8 neighbours open so a
        /// grove never seals a passage.</summary>
        public TerrainCanvas Grove(int x, int y, int w, int h, float density, string look = "tree", string look2 = null, Func<int, int, bool> where = null)
        {
            for (int yy = y; yy < y + h; yy++)
                for (int xx = x; xx < x + w; xx++)
                {
                    if (!InBounds(xx, yy) || _rng.NextDouble() >= density || (where != null && !where(xx, yy))) continue;
                    bool open = true;
                    for (int dy = -1; dy <= 1 && open; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = xx + dx, ny = yy + dy;
                            if (!InBounds(nx, ny) || _tile[ny, nx] != TileType.Empty || (_look[ny, nx] != null && _look[ny, nx] != "tallgrass") || (_keep != null && _keep[ny, nx])) { open = false; break; }
                        }
                    if (open) Set(xx, yy, TileType.Solid, _height[yy, xx], look2 != null && _rng.NextDouble() < 0.35 ? look2 : look);
                }
            return this;
        }

        /// <summary>A small hut: walls in `wallLook`, a plank/concrete floor, a door on the given side
        /// (0 south, 1 east, 2 west, 3 north), optionally a broken-in hole and a crate.</summary>
        public TerrainCanvas Hut(int x, int y, int w, int h, string wallLook, int doorSide = 0, bool crate = true, GroundType? floor = null)
        {
            var fl = floor ?? (wallLook == "concrete" || wallLook == "metal" ? GroundType.Concrete : GroundType.Floor);
            // Batch 122: a hut left at the default south door turns east or west about half the time, if that side is open ground
            if (doorSide == 0 && h >= 4)
            {
                float hr = ZoneThemes.Hash01(x, y, _seed + 122); int turn = hr < 0.25f ? 1 : hr < 0.5f ? 2 : 0, ox = turn == 1 ? x + w : x - 1, oy = y + h / 2;
                if (turn != 0 && InBounds(ox, oy) && _tile[oy, ox] == TileType.Empty && _ground[oy, ox] != GroundType.Water) doorSide = turn;
            }
            Building(x, y, w, h, wallLook, fl);
            int dx = doorSide == 0 || doorSide == 3 ? x + w / 2 : doorSide == 1 ? x + w - 1 : x;
            int dy = doorSide == 0 ? y + h - 1 : doorSide == 3 ? y : y + h / 2;
            Put(dx, dy, TileType.Door, wallLook == "concrete" ? "metal" : null);
            if (crate) Put(x + 1, y + 1, TileType.ScatterCover, "crate");
            Keep(x - 1, y - 1, w + 2, h + 2);
            return this;
        }

        /// <summary>A building the carver will hollow into rooms (a solid block of built wall, 7x7 or bigger),
        /// with a cleared, kept apron round it so it has somewhere to open its doors onto.</summary>
        public TerrainCanvas Block(int x, int y, int w, int h, string wallLook, int apron = 2, GroundType? apronFloor = null)
        {
            Clear(x - apron, y - apron, w + 2 * apron, h + 2 * apron, apronFloor);
            Rect(x, y, w, h, TileType.Solid, InBounds(x, y) ? _height[y, x] : 0, wallLook);
            return this;
        }

        /// <summary>Pins a guaranteed stash (Footlocker or the like) to a tile - TerrainMap.SpawnInteractiveObjects puts one there.</summary>
        public TerrainCanvas Stash(string name, int x, int y) => Landmark("stash:" + name, x, y);

        public MapLayout ToMapLayout()
        {
            var tiles = new string[H][];
            var heights = new int[H][];
            bool anyHeight = false;
            for (int y = 0; y < H; y++)
            {
                tiles[y] = new string[W];
                heights[y] = new int[W];
                for (int x = 0; x < W; x++)
                {
                    tiles[y][x] = _tile[y, x].ToString();
                    heights[y][x] = _height[y, x];
                    if (_height[y, x] != 0) anyHeight = true;
                }
            }
            string[][] looks = null, grounds = null;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (_look[y, x] != null)
                    {
                        looks = looks ?? Enumerable.Range(0, H).Select(_ => new string[W]).ToArray();
                        looks[y][x] = _look[y, x];
                    }
                    if (_ground[y, x].HasValue)
                    {
                        grounds = grounds ?? Enumerable.Range(0, H).Select(_ => new string[W]).ToArray();
                        grounds[y][x] = _ground[y, x].Value.ToString();
                    }
                }
            string[][] regions = null; bool[][] keep = null;
            if (_region != null) regions = Enumerable.Range(0, H).Select(y => Enumerable.Range(0, W).Select(x => _region[y, x]).ToArray()).ToArray();
            if (_keep != null) keep = Enumerable.Range(0, H).Select(y => Enumerable.Range(0, W).Select(x => _keep[y, x]).ToArray()).ToArray();
            return new MapLayout { Tiles = tiles, Heights = anyHeight ? heights : null, Looks = looks, Grounds = grounds, Anchors = _anchors, Regions = regions, Keep = keep };
        }

        public static int Seed(string zoneId) => zoneId.Aggregate(17, (acc, ch) => acc * 31 + ch);
    }
}
