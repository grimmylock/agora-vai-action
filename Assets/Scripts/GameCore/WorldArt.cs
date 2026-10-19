// Batch 30: the 3/4 world renderer's art side. Replaces TerrainArt's "one 16x16 sprite per tile,
// all baked flat" with two layers:
//   - GROUND: one composed texture per zone - 64x64 tileable floors blended with a domain-warped
//     border (no straight tile edges between grass/dirt/asphalt), water banks, line flats (rails,
//     road paint, trenches), scattered decals, and the baked shadows of everything standing;
//   - STANDING: walls, masses, trees, cars, crates... as separate sprites (Standing) that the view
//     y-sorts with the units, so you walk behind a tree or a wall and in front of it.
// Walls are per-cell autotiles in 3/4 (cap + front face, ArtSource/world/walls.py); a wall's front
// face sits on its own cell and its cap rises H px above, over the cell behind it.
// Art: Resources/Sprites/world_atlas.bytes (png) + world_atlas_meta.txt (ArtSource/world/pack.py).
// No atlas = TacticalGridView keeps the old TerrainArt path.
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public static class WorldArt
    {
        public const int Px = 16;
        public const int SortBase = 100;      // ground stuff below this; standing things = SortBase + foot pixel row
        public const int SortTop = 32000;     // smoke, tracers, muzzle flashes: over everything

        public sealed class Spr { public string Name; public int X, Y, W, H, PvX, PvY, ShW, ShH, ShDx, ShDy; public bool Tall; }

        /// <summary>One standing sprite: its pivot (ground contact) in map pixels, y down.</summary>
        public struct Standing
        {
            public Spr Spr; public int X, Y; public int CellX, CellY; public bool Fades;
            public int Sort => SortBase + Y;
            public int Left => X - Spr.PvX;
            public int Top => Y - Spr.PvY;
        }

        private static bool _loaded;
        private static Color32[] _atlas;
        private static int _aw, _ah;
        private static readonly Dictionary<string, Spr> _spr = new Dictionary<string, Spr>();
        private static readonly Dictionary<string, int> _variants = new Dictionary<string, int>();   // "p_tree" -> 4
        public static Texture2D AtlasTexture { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCaches() { _loaded = false; _atlas = null; AtlasTexture = null; _spr.Clear(); _variants.Clear(); _sprites.Clear(); }

        public static bool Ready { get { Load(); return _atlas != null; } }

        private static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            var png = Resources.Load<TextAsset>("Sprites/world_atlas");
            var meta = Resources.Load<TextAsset>("Sprites/world_atlas_meta");
            if (png == null || meta == null) return;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(png.bytes)) return;
            tex.filterMode = FilterMode.Point; tex.wrapMode = TextureWrapMode.Clamp;
            _atlas = tex.GetPixels32(); _aw = tex.width; _ah = tex.height;
            AtlasTexture = tex;
            foreach (var raw in meta.text.Split('\n'))
            {
                var f = raw.Trim().Split(' ');
                if (f.Length < 12) continue;
                int I(int k) => int.Parse(f[k]);
                var s = new Spr { Name = f[0], X = I(1), Y = I(2), W = I(3), H = I(4), PvX = I(5), PvY = I(6), ShW = I(7), ShH = I(8), ShDx = I(9), ShDy = I(10), Tall = f[11] == "1" };
                _spr[s.Name] = s;
                int us = s.Name.LastIndexOf('_');
                if (us > 0 && int.TryParse(s.Name.Substring(us + 1), out int v))
                {
                    string b = s.Name.Substring(0, us);
                    _variants[b] = System.Math.Max(_variants.TryGetValue(b, out int n) ? n : 0, v + 1);
                }
            }
        }

        public static Spr Get(string name) { Load(); return name != null && _spr.TryGetValue(name, out var s) ? s : null; }

        /// <summary>variant k of "p_tree" style families (k wraps), or the exact name.</summary>
        private static Spr Pick(string family, float roll)
        {
            if (family == null) return null;
            if (_variants.TryGetValue(family, out int n)) return Get(family + "_" + System.Math.Min(n - 1, (int)(roll * n)));
            return Get(family);
        }

        // sprite px, y counted from the sprite's top
        private static Color32 At(Spr s, int x, int y) => _atlas[(_ah - 1 - (s.Y + y)) * _aw + s.X + x];

        private static readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
        /// <summary>Unity sprite for an atlas entry, pivot at its ground contact point (cached).</summary>
        public static Sprite SpriteOf(Spr s)
        {
            if (s == null || AtlasTexture == null) return null;
            if (_sprites.TryGetValue(s.Name, out var sp) && sp != null) return sp;
            var rect = new Rect(s.X, _ah - s.Y - s.H, s.W, s.H);
            sp = Sprite.Create(AtlasTexture, rect, new Vector2((s.PvX + 0.5f) / s.W, (s.H - s.PvY - 1f) / s.H), Px, 0, SpriteMeshType.FullRect);
            _sprites[s.Name] = sp;
            return sp;
        }

        // ------------------------------------------------------------------ classification
        private static readonly HashSet<string> BuiltWall = new HashSet<string> { "wall", "plaster", "concrete", "hull", "metal", "plank", "container" };   // Batch 101: containers fill their rectangle like a wall block
        private static readonly HashSet<string> Mass = new HashSet<string> { "cliff", "foliage", "redfoliage", "junk" };
        private static readonly HashSet<string> Lines = new HashSet<string> { "rail", "rail_v", "roadline", "trench", "boardwalk" };
        public static bool IsDecal(string l) => l != null && Decal.Contains(l);   // Batch 103: props roll over decals
        private static readonly HashSet<string> Decal = new HashSet<string> { "puddle", "bones", "tallgrass", "flowers", "blood", "splat", "bloodtrail", "grime", "scorch", "papers", "glass", "oil", "litter" };
        private static readonly HashSet<string> Natural = new HashSet<string> { "tree", "pine", "deadtree", "redtree", "bush", "redbush", "rock", "reeds", "brush", "stones", "debris", "rubble", "scrap", "logs", "tires" };

        private static string _zone;
        private static bool _interior;          // caves, labs, tunnels: solid rock beyond the walls is darkness
        private static ZoneTheme _theme;
        private static int _seed, _w, _h;
        private static string[] _look;          // per cell: ObjectLook
        private static GroundType[] _ground;
        private static bool[] _uniform, _buried, _core;

        private static string LookAt(int x, int y) => x < 0 || y < 0 || x >= _w || y >= _h ? null : _look[y * _w + x];
        private static TerrainTile TileAt(int x, int y) => TerrainMap.Tiles[y, x] ?? new TerrainTile();

        /// <summary>A wall/mass cell, or a closed door (they join walls). Open doors break the wall line.</summary>
        private static bool Wallish(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _w || y >= _h) return true;
            var t = TerrainMap.Tiles[y, x];
            if (t == null) return false;
            if (t.Type == TileType.Door) return true;
            string l = _look[y * _w + x];
            return t.Type == TileType.Solid && l != null && (BuiltWall.Contains(l) || Mass.Contains(l) || l == "window" || l == "metaldoor");
        }

        private static int WallMask(int x, int y) =>
            (Wallish(x, y - 1) ? 1 : 0) | (Wallish(x + 1, y) ? 2 : 0) | (Wallish(x, y + 1) ? 4 : 0) | (Wallish(x - 1, y) ? 8 : 0);

        /// <summary>Batch 127 (Yan: "we need some natural shapes"): a mass cell's mask, open towards a core cell too (drawn as forest
        /// floor with trees), so the canopy's edge round a wood is wavy and rounded there, not a straight line along the cells.</summary>
        private static int MassMask(int x, int y)
        {
            bool Core(int a, int b) => a >= 0 && b >= 0 && a < _w && b < _h && _core[b * _w + a];
            int m = WallMask(x, y);
            if (_look[y * _w + x] != "foliage" && _look[y * _w + x] != "redfoliage") return m;   // a cliff's or scrap heap's top is the same surface as its core
            if (Core(x, y - 1)) m &= ~1;
            if (Core(x + 1, y)) m &= ~2;
            if (Core(x, y + 1)) m &= ~4;
            if (Core(x - 1, y)) m &= ~8;
            return m;
        }

        /// <summary>Built walls: the NESW mask plus, for solid blocks, which corners are filled
        /// ("15c15" = a cell inside a block, drawn as roof) - names match ArtSource/world/walls.py.</summary>
        private static string WallKey(int x, int y)
        {
            int m = WallMask(x, y), c = 0;
            if ((m & 3) == 3 && Wallish(x + 1, y - 1)) c |= 1;
            if ((m & 6) == 6 && Wallish(x + 1, y + 1)) c |= 2;
            if ((m & 12) == 12 && Wallish(x - 1, y + 1)) c |= 4;
            if ((m & 9) == 9 && Wallish(x - 1, y - 1)) c |= 8;
            return c == 0 ? m.ToString() : m + "c" + c;
        }

        /// <summary>Interior zones: a wall cell with wall all around - deep rock, drawn as darkness.</summary>
        /// <summary>Batch 98: underground, the inside of a thick wall between rooms - a plain concrete top, no wall piece.</summary>
        private static bool[] _slab;
        private static readonly HashSet<string> SlabWall = new HashSet<string> { "concrete", "metal", "hull", "plaster" };   // built labs; brick-lined tunnels and rock stay dark
        private static bool Slab(int x, int y)
        {
            if (!_interior || Buried(x, y) || TerrainMap.Tiles[y, x]?.Type != TileType.Solid || !SlabWall.Contains(_look[y * _w + x] ?? "")) return false;
            return WallMask(x, y) == 15;   // walled on all four sides: no face shows, only the top (not a tar roof underground)
        }

        private static bool Buried(int x, int y)
        {
            if (!_interior) return false;
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) if (!Wallish(x + dx, y + dy)) return false;
            // Batch 98: a thick wall between two rooms (open within 6 tiles on both sides) is a partition, drawn as wall top;
            // only rock beyond the building stays dark (the Foundry's walls between rooms were black voids)
            bool Open(int dx, int dy) { for (int k = 2; k <= 6; k++) if (!Wallish(x + dx * k, y + dy * k)) return true; return false; }
            if (SlabWall.Contains(_look[y * _w + x] ?? "") && (Open(1, 0) && Open(-1, 0) || Open(0, 1) && Open(0, -1))) return false;   // built walls only: rock stays rock
            var t = TerrainMap.Tiles[y, x];
            return t != null && t.Type == TileType.Solid;
        }

        /// <summary>Overworld: a cliff / hedge / scrap cell deep inside its mass. Drawn as ground (rock top,
        /// forest floor, scrap) with trees, boulders or scrap standing on it, instead of one flat cap.</summary>
        private static bool MassCore(int x, int y)
        {
            if (_interior) return false;
            string l = LookAt(x, y);
            if (l == null || !Mass.Contains(l) || TerrainMap.Tiles[y, x]?.Type != TileType.Solid) return false;
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) if (!Wallish(x + dx, y + dy)) return false;
            return true;
        }

        private static readonly Style RockTop = new Style { Ramp = Ramp("3d3e3f", "4a4b4b", "525353", "5c5d5c"), Cuts = new[] { 0.36f, 0.5f, 0.64f }, Big = 20, Mid = 6, SpeckA = Hex("6a6a68"), SpeckB = Hex("2c2d2e"), DensA = .04f, DensB = .04f };
        private static readonly Style ForestFloor = new Style { Ramp = Ramp("1E2A1C", "232f20", "283524", "2E3D26"), Cuts = new[] { 0.36f, 0.5f, 0.64f }, Big = 18, Mid = 6, SpeckA = Hex("3d3229"), SpeckB = Hex("18231d"), DensA = .05f, DensB = .04f };
        private static readonly Style RedFloor = new Style { Ramp = Ramp("2a1416", "321a19", "3a1f1c", "4a1e1e"), Cuts = new[] { 0.36f, 0.5f, 0.64f }, Big = 18, Mid = 6, SpeckA = Hex("524436"), SpeckB = Hex("1e1012"), DensA = .05f, DensB = .04f };
        private static readonly Style ScrapTop = new Style { Ramp = Ramp("3A2219", "4a2e20", "5E3524", "525C5E"), Cuts = new[] { 0.36f, 0.5f, 0.64f }, Big = 7, Mid = 3, SpeckA = Hex("86502E"), SpeckB = Hex("26292b"), DensA = .08f, DensB = .08f };

        /// <summary>The built material a window/door sits in: a neighbouring wall's, else plain brick.</summary>
        private static string HostMaterial(int x, int y)
        {
            foreach (var (dx, dy) in new[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
            {
                string l = LookAt(x + dx, y + dy);
                if (l != null && BuiltWall.Contains(l)) return l;
            }
            return "wall";
        }

        private static float H01(int x, int y, int k) => ZoneThemes.Hash01(x, y, _seed + k);

        // ------------------------------------------------------------------ zone setup
        private static readonly object Gate = new object();   // a background sun re-bake vs. Begin / repaints

        public static void Begin(string zoneId) { lock (Gate) BeginLocked(zoneId); }
        private static void BeginLocked(string zoneId)
        {
            Load();
            _zone = zoneId; _theme = ZoneThemes.For(zoneId); _interior = ZoneLayoutRecipes.IsInterior(zoneId); _seed = TerrainCanvas.Seed(zoneId ?? "x");
            _w = TerrainMap.Width; _h = TerrainMap.Height; _lat.Clear(); _wx1 = null; _gen++; _shade = null; SetSun();
            _look = new string[_w * _h]; _ground = new GroundType[_w * _h]; _uniform = new bool[_w * _h]; _buried = null; _core = null;
            for (int y = 0; y < _h; y++) for (int x = 0; x < _w; x++) Refresh(x, y);
            for (int y = 0; y < _h; y++)
                for (int x = 0; x < _w; x++)
                {
                    bool u = true; var g = _ground[y * _w + x];
                    for (int dy = -1; dy <= 1 && u; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = Mathf.Clamp(x + dx, 0, _w - 1), ny = Mathf.Clamp(y + dy, 0, _h - 1);
                            if (_ground[ny * _w + nx] != g) { u = false; break; }
                        }
                    _uniform[y * _w + x] = u;   // warp can't reach a different floor from here: skip it
                }
            _buried = new bool[_w * _h]; _core = new bool[_w * _h]; _slab = new bool[_w * _h];
            for (int y = 0; y < _h; y++) for (int x = 0; x < _w; x++) { _buried[y * _w + x] = Buried(x, y); _core[y * _w + x] = MassCore(x, y); _slab[y * _w + x] = Slab(x, y); }
            FindOutside();
        }

        // Batch 97 (Yan: "walls look too neat for a decaying building"): the open air, flooded from outdoor ground through
        // gaps (not through walls or doors, open or shut). A house wall with no closed room on either side stands in a
        // roofless ruin and draws broken (w_<mat>_<key>_ruin). Underground there is no open air, so nothing is a ruin.
        private static bool[] _outside;
        private static readonly HashSet<string> Ruinable = new HashSet<string> { "wall", "plaster", "plank" };
        private static void FindOutside()
        {
            _outside = new bool[_w * _h];
            if (_interior) return;
            bool Open(int i) { var ty = TerrainMap.Tiles[i / _w, i % _w]?.Type ?? TileType.Empty; return ty != TileType.Solid && ty != TileType.Door && ty != TileType.DoorOpen; }
            var q = new Queue<int>();
            for (int i = 0; i < _w * _h; i++)
                if (Open(i) && _ground[i] != GroundType.Floor && _ground[i] != GroundType.LabTile) { _outside[i] = true; q.Enqueue(i); }
            while (q.Count > 0)
            {
                int i = q.Dequeue(), x = i % _w, y = i / _w;
                foreach (int n in new[] { x > 0 ? i - 1 : -1, x < _w - 1 ? i + 1 : -1, y > 0 ? i - _w : -1, y < _h - 1 ? i + _w : -1 })
                    if (n >= 0 && !_outside[n] && Open(n)) { _outside[n] = true; q.Enqueue(n); }
            }
        }
        private static bool Ruined(int x, int y, string l)
        {
            if (_outside == null || _interior || !Ruinable.Contains(l)) return false;
            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= _w || ny >= _h) continue;
                var ty = TileAt(nx, ny).Type;
                if (ty != TileType.Solid && ty != TileType.Door && ty != TileType.DoorOpen && !_outside[ny * _w + nx]) return false;   // a closed room beside it
            }
            return true;
        }

        /// <summary>Re-reads one cell after it changed (door opened, cover destroyed).</summary>
        public static void Refresh(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _w || y >= _h) return;
            _look[y * _w + x] = TerrainArt.ObjectLook(x, y, _theme);
            _ground[y * _w + x] = TileAt(x, y).Ground;
            if (_buried != null)
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                        if (x + dx >= 0 && y + dy >= 0 && x + dx < _w && y + dy < _h)
                        {
                            _buried[(y + dy) * _w + x + dx] = Buried(x + dx, y + dy);
                            _core[(y + dy) * _w + x + dx] = MassCore(x + dx, y + dy);
                        }
        }

        // Batch 95 (Yan: "cover like this needs to be drawn sideways too"): a wall-like prop in a north-south run (same kind above
        // or below, none beside it) uses its _v sprite: the top running up the screen, a face only at the south end.
        private static readonly HashSet<string> Runs = new HashSet<string> { "sandbags", "jersey", "block", "logs", "fence", "fence_torn" };
        private static string RunFamily(string l) => l == "fence_torn" || l == "fence_v" ? "fence" : l;
        private static string RunLook(int x, int y, string l)
        {
            if (!Runs.Contains(l)) return l;
            string f = RunFamily(l);
            bool Same(int ax, int ay) => ax >= 0 && ay >= 0 && ax < _w && ay < _h && RunFamily(_look[ay * _w + ax]) == f;
            bool north = Same(x, y - 1), vert = north || Same(x, y + 1), horiz = Same(x - 1, y) || Same(x + 1, y);
            if (vert && !horiz) return f == "fence" ? "fence_v" : l + "_v";
            return north && horiz && f != "fence" ? l + "_c" : l;   // Batch 96: a run from the north joins an east-west one (a south arm draws over it anyway)
        }

        private static bool InRun(int x, int y, string l)
        {
            if (!Runs.Contains(l)) return false;
            string f = RunFamily(l);
            bool Same(int ax, int ay) => ax >= 0 && ay >= 0 && ax < _w && ay < _h && RunFamily(_look[ay * _w + ax]) == f;
            return Same(x, y - 1) || Same(x, y + 1) || Same(x - 1, y) || Same(x + 1, y);
        }

        /// <summary>What stands on a cell (0 or 1 sprite).</summary>
        public static bool StandingAt(int x, int y, out Standing st)
        {
            st = default;
            if (_atlas == null || x < 0 || y < 0 || x >= _w || y >= _h) return false;
            var t = TileAt(x, y);
            string l = _look[y * _w + x];
            if (l == null && t.Type == TileType.Barrel) l = "barrel";   // Batch 96: explosive barrels were placed with no look and drawn as nothing
            Spr s = null; int ox = 0, oy = 0; bool wall = false;
            if (Buried(x, y) || _slab != null && _slab[y * _w + x]) return false;
            if (_core[y * _w + x])
            {
                float roll = H01(x, y, 15);
                if (l == "foliage") s = roll < 0.6f ? Pick(H01(x, y, 16) < 0.35f ? "p_pine" : "p_tree", H01(x, y, 3)) : null;
                else if (l == "redfoliage") s = roll < 0.6f ? Pick(H01(x, y, 16) < 0.2f ? "p_deadtree" : "p_redtree", H01(x, y, 3)) : null;
                else if (l == "cliff") s = roll < 0.06f ? Pick("p_rock", H01(x, y, 3)) : roll < 0.09f ? Pick("p_bush", H01(x, y, 3)) : roll < 0.12f ? Pick("p_stones", H01(x, y, 3)) : null;   // Batch 127: scrub and pebbles too (a bare top read as a road)
                else if (l == "junk") s = roll < 0.35f ? Pick(roll < 0.12f ? "p_wreck" : "p_scrap", H01(x, y, 3)) : null;
                if (s == null) return false;
                st = new Standing { Spr = s, CellX = x, CellY = y, Fades = true, X = x * Px + 8 + (int)((H01(x, y, 4) - 0.5f) * 10), Y = y * Px + 13 - (int)(H01(x, y, 5) * 6) };
                return true;
            }
            if (t.Type == TileType.Door)
            {
                s = Get($"w_{HostMaterial(x, y)}_{WallKey(x, y)}_{(l == "metaldoor" ? "metaldoor" : "door")}"); wall = true;
            }
            else if (t.Type == TileType.Solid && l != null && (BuiltWall.Contains(l) || Mass.Contains(l) || l == "window" || l == "metaldoor"))
            {
                if (l == "window" || l == "metaldoor") s = Get($"w_{HostMaterial(x, y)}_{WallKey(x, y)}_{l}");   // metaldoor on a Solid: a sealed blast door
                else if (Mass.Contains(l)) s = Get($"w_{l}_{MassMask(x, y)}_{(int)(H01(x, y, 9) * 3) % 3}");
                else s = (Ruined(x, y, l) ? Get($"w_{l}_{WallKey(x, y)}_ruin") : null)
                         ?? (t.Damage == 0 && x % 2 == 0 && H01(x, y, 44) < 0.75f ? Get($"w_{l}_{WallKey(x, y)}_uwin") : null)   // Batch 119: upper windows in a rhythm
                         ?? (t.Damage == 0 && H01(x, y, 43) < 0.4f ? Get($"w_{l}_{WallKey(x, y)}_worn") : null)   // Batch 98: weathered
                         ?? Get($"w_{l}_{WallKey(x, y)}_{(t.Damage > 0 ? "crack" : "0")}") ?? Get($"w_{l}_{WallKey(x, y)}_0");
                wall = true;
            }
            else if (l != null && !Lines.Contains(l) && !Decal.Contains(l) && l != "platform" && l != "door_open" && l != "door_broken")
            {
                string rl = RunLook(x, y, l);
                s = Pick("p_" + rl, H01(x, y, 3)) ?? Pick("p_" + l, H01(x, y, 3));
                if (s != null && Natural.Contains(l) && !InRun(x, y, l))         // pieces of a run stay in line
                {
                    bool patch = t.Type == TileType.Difficult;          // reeds/brush/rubble fields: break the rows up
                    if (patch && H01(x, y, 14) < 0.3f) return false;
                    ox = (int)((H01(x, y, 4) - 0.5f) * (patch ? 12 : 8)); oy = -(int)(H01(x, y, 5) * (patch ? 5 : 3));
                }
            }
            if (s == null) return false;
            st = new Standing
            {
                Spr = s, CellX = x, CellY = y, Fades = wall || s.Tall,
                X = x * Px + 8 + ox,
                Y = wall ? y * Px + Px - 1 : y * Px + 13 + oy,
            };
            return true;
        }

        // ------------------------------------------------------------------ ground layer
        private static bool Crisp(GroundType g) => g == GroundType.Floor || g == GroundType.LabTile;
        private static bool Paved(GroundType g) => g == GroundType.Concrete || g == GroundType.Asphalt;
        private const float Warp = 11f;

        // Value noise on a hashed lattice. The lattices are cached per zone (Begin clears them): composing a
        // 200x200-tile ground samples them tens of millions of times, so no hashing in the inner loop,
        // and the hot loop holds the Lat objects directly (no dictionary lookups per pixel).
        private sealed class Lat
        {
            public float[] G; public int W, H, Cell;
            public float At(float x, float y)
            {
                float fx = x / Cell, fy = y / Cell;
                int ix = (int)fx, iy = (int)fy;             // x, y >= 0 here
                if (ix < 0 || iy < 0 || ix + 1 >= W || iy + 1 >= H) return 0.5f;
                float tx = fx - ix, ty = fy - iy;
                tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
                int o = iy * W + ix;
                float a = G[o], b = G[o + 1], c = G[o + W], d = G[o + W + 1];
                float top = a + (b - a) * tx, bot = c + (d - c) * tx;
                return top + (bot - top) * ty;
            }
        }
        private static readonly Dictionary<long, Lat> _lat = new Dictionary<long, Lat>();
        private static Lat GetLat(int cell, int k)
        {
            long key = ((long)cell << 32) | (uint)k;
            if (_lat.TryGetValue(key, out var l)) return l;
            l = new Lat { Cell = cell, W = _w * Px / cell + 3, H = _h * Px / cell + 3 };
            l.G = new float[l.W * l.H];
            for (int j = 0; j < l.H; j++) for (int i = 0; i < l.W; i++) l.G[j * l.W + i] = H01(i, j, k);
            return _lat[key] = l;
        }
        private static float VNoise(float x, float y, int cell, int k) => GetLat(cell, k).At(x, y);
        private static Lat _wx1, _wx2, _wy1, _wy2;          // warp lattices for the zone being painted
        private static int _gen;                             // bumps on every Begin

        private static GroundType GroundPx(int gx, int gy)
        {
            int cx = Mathf.Clamp(gx / Px, 0, _w - 1), cy = Mathf.Clamp(gy / Px, 0, _h - 1);
            var own = _ground[cy * _w + cx];
            if (Crisp(own) || _uniform[cy * _w + cx]) return own;
            if (_wx1 == null) { _wx1 = GetLat(23, 21); _wx2 = GetLat(7, 23); _wy1 = GetLat(23, 22); _wy2 = GetLat(7, 24); }
            float ox = (_wx1.At(gx, gy) * 0.65f + _wx2.At(gx, gy) * 0.35f - 0.5f) * 2f * Warp;
            float oy = (_wy1.At(gx, gy) * 0.65f + _wy2.At(gx, gy) * 0.35f - 0.5f) * 2f * Warp;
            int sx = Mathf.Clamp(Mathf.FloorToInt((gx + ox) / Px), 0, _w - 1), sy = Mathf.Clamp(Mathf.FloorToInt((gy + oy) / Px), 0, _h - 1);
            var g = _ground[sy * _w + sx];
            return Crisp(g) || Paved(g) && Paved(own) ? own : g;     // pavement meets road in a straight kerb
        }

        // Natural floors are generated per pixel in world space (no repeating texture tile): a tone
        // field from two noise octaves picks a colour off the ramp, plus hashed speckles and grass blades.
        // Colours = ArtSource/world/worldlib.py palettes. Built floors (planks, tiles, slabs) use the atlas.
        private sealed class Style
        {
            public Color32[] Ramp; public float[] Cuts; public int Big, Mid; public Color32 SpeckA, SpeckB; public float DensA, DensB; public Color32? Blade, Blade2;
            public Color32[] Dry;                                   // Batch 31: straw mixed in pixel by pixel
            public Lat BigLat, MidLat, DryLat, DryLat2; public int Gen = -1;        // lattices of the zone (Begin generation) they were made for
        }
        private static Color32 Hex(string h) => new Color32(System.Convert.ToByte(h.Substring(0, 2), 16), System.Convert.ToByte(h.Substring(2, 2), 16), System.Convert.ToByte(h.Substring(4, 2), 16), 255);
        private static Color32[] Ramp(params string[] h) { var r = new Color32[h.Length]; for (int i = 0; i < h.Length; i++) r[i] = Hex(h[i]); return r; }
        private static readonly float[] Cuts3 = { 0.36f, 0.5f, 0.64f };
        private static readonly Style[] Styles =
        {
            /* Grass     */ new Style { Ramp = Ramp("2f3524", "3a4130", "454d37", "535a40", "656a4a"), Dry = Ramp("5c5638", "6f6745", "857b52", "9a8f61"), Cuts = new[] { 0.24f, 0.4f, 0.56f, 0.72f }, Big = 28, Mid = 9, SpeckA = Hex("656a4a"), SpeckB = Hex("262b20"), DensA = .015f, DensB = .015f, Blade = Hex("6f744d"), Blade2 = Hex("535a40") },
            /* DarkGrass */ new Style { Ramp = Ramp("262b20", "2e3427", "373e2e", "424a36"), Dry = Ramp("4a4632", "5c5638", "6f6745", "857b52"), Cuts = Cuts3, Big = 28, Mid = 9, SpeckA = Hex("535a40"), SpeckB = Hex("1f231a"), DensA = .015f, DensB = .02f, Blade = Hex("535a40"), Blade2 = Hex("454d37") },
            /* Dirt      */ new Style { Ramp = Ramp("332b22", "40372b", "4e4434", "5d5240"), Cuts = Cuts3, Big = 24, Mid = 7, SpeckA = Hex("6b5e48"), SpeckB = Hex("2a241c"), DensA = .025f, DensB = .02f },
            /* Mud       */ new Style { Ramp = Ramp("34291f", "3a2e23", "3f3226", "45372b"), Cuts = Cuts3, Big = 22, Mid = 8, SpeckA = Hex("5A4838"), SpeckB = Hex("25332F"), DensA = .015f, DensB = .01f },
            /* Asphalt   */ new Style { Ramp = Ramp("313336", "36393c", "3a3d40", "3e4144"), Cuts = Cuts3, Big = 30, Mid = 6, SpeckA = Hex("4c4f53"), SpeckB = Hex("25272A"), DensA = .04f, DensB = .02f },
            /* Concrete  */ new Style { Ramp = Ramp("5a5c5d", "616362", "666867", "6b6c6b"), Cuts = Cuts3, Big = 30, Mid = 8, SpeckA = Hex("7f7f7c"), SpeckB = Hex("4d4f51"), DensA = .03f, DensB = .03f },
            /* Floor     */ null,
            /* LabTile   */ null,
            /* Sand      */ new Style { Ramp = Ramp("6b5f45", "7d7052", "8e8160", "958866"), Cuts = Cuts3, Big = 30, Mid = 10, SpeckA = Hex("a0936f"), SpeckB = Hex("605c55"), DensA = .02f, DensB = .015f },
            /* Gravel    */ new Style { Ramp = Ramp("3f3d3a", "4f4c47", "57534d", "605c55"), Cuts = Cuts3, Big = 18, Mid = 4, SpeckA = Hex("757067"), SpeckB = Hex("2c2d2e"), DensA = .07f, DensB = .05f },
            /* Water     */ new Style { Ramp = Ramp("1d2624", "212b28", "24302c", "283430"), Cuts = Cuts3, Big = 34, Mid = 12, SpeckA = Hex("4F6458"), SpeckB = Hex("1B2422"), DensA = .004f, DensB = .01f },
            /* CaveFloor */ new Style { Ramp = Ramp("232220", "2c2a27", "322f2b", "36332f"), Cuts = Cuts3, Big = 22, Mid = 7, SpeckA = Hex("423e39"), SpeckB = Hex("1a1918"), DensA = .03f, DensB = .02f },
        };

        private static void Prime(Style st, int gi)
        {
            if (st.Gen == _gen) return;
            st.BigLat = GetLat(st.Big, 40 + gi); st.MidLat = GetLat(st.Mid, 50 + gi);
            if (st.Dry != null) { st.DryLat = GetLat(40, 60); st.DryLat2 = GetLat(7, 61); }
            st.Gen = _gen;
        }

        private static Color32 Procedural(Style st, int gi, int gx, int gy)
        {
            Prime(st, gi);
            float n = st.BigLat.At(gx, gy) * 0.65f + st.MidLat.At(gx, gy) * 0.35f;
            var ramp = st.Ramp;
            if (st.Dry != null)
            {
                n += (ZoneThemes.Hash01(gx, gy, _seed + 77) - 0.5f) * 0.18f;            // fine grain: blades, not blotches
                float dry = st.DryLat.At(gx, gy) * 0.7f + st.DryLat2.At(gx, gy) * 0.3f;
                float pDry = Mathf.Clamp01((dry - 0.45f) / 0.2f) * 0.85f;
                if (ZoneThemes.Hash01(gx, gy, _seed + 78) < pDry) ramp = st.Dry;
            }
            int i = 0; while (i < st.Cuts.Length && n > st.Cuts[i]) i++;
            var c = ramp[Mathf.Min(i, ramp.Length - 1)];
            float h = ZoneThemes.Hash01(gx, gy, _seed + 70 + gi);
            if (h < st.DensA) c = st.SpeckA; else if (h < st.DensA + st.DensB) c = st.SpeckB;
            if (st.Blade.HasValue)
            {
                // 2-3 px blades leaning a little: a blade root at (gx, gy+1) or (gx, gy+2) reaches up here
                float b1 = ZoneThemes.Hash01(gx, gy + 1, _seed + 81), b2 = ZoneThemes.Hash01(gx, gy, _seed + 81);
                if (b2 < 0.035f || b1 < 0.035f) c = (b2 < 0.012f || b1 < 0.012f) ? st.Blade.Value : st.Blade2.Value;
            }
            if (gi == (int)GroundType.Water && ZoneThemes.Hash01(gx / 3, gy, _seed + 90) < 0.004f) c = st.SpeckA;   // a glint
            return c;
        }

        private static readonly string[] GroundName = { "grass", "darkgrass", "dirt", "mud", "asphalt", "concrete", "floor", "labtile", "sand", "gravel", "water", "cavefloor" };
        private static readonly Color32 KerbLight = new Color32(0x8f, 0x8c, 0x83, 255), KerbDark = new Color32(0x4a, 0x48, 0x44, 255);
        private static readonly Color32 ShadowCol = new Color32(0x32, 0x21, 0x25, 255);
        private static readonly Color32 Black = new Color32(0, 0, 0, 255), White = new Color32(255, 255, 255, 255);

        /// <summary>The whole ground layer (Unity row order: row 0 = bottom). Call Begin first.</summary>
        static bool Walled(int x, int y) => x < 0 || y < 0 || x >= _w || y >= _h || TileAt(x, y).Type == TileType.Solid;

        public static Color32[] ComposeGround()
        {
            lock (Gate)
            {
                SetSun();                     // the hour may have moved since Begin: re-bakes pick up the new sun
                var px = new Color32[_w * Px * _h * Px];
                PaintGround(px, 0, 0, _w - 1, _h - 1);
                return px;
            }
        }

        /// <summary>Clear daylight shear for unit shadows (see CastShadows); false when there is no sun.</summary>
        public static bool Sun(out float kx, out float ky) { kx = _sunKx; ky = _sunKy; return _sunA > 0f; }

        /// <summary>Repaints cells x0..x1, y0..y1 (inclusive) of the ground layer.</summary>
        public static void PaintGround(Color32[] px, int x0, int y0, int x1, int y1) { lock (Gate) PaintGroundLocked(px, x0, y0, x1, y1); }
        private static void PaintGroundLocked(Color32[] px, int x0, int y0, int x1, int y1)
        {
            x0 = System.Math.Max(0, x0); y0 = System.Math.Max(0, y0); x1 = System.Math.Min(_w - 1, x1); y1 = System.Math.Min(_h - 1, y1);
            int W = _w * Px, H = _h * Px;
            var tex = new Spr[GroundName.Length];
            for (int i = 0; i < tex.Length; i++) tex[i] = Get("g_" + GroundName[i]);
            int gx0 = x0 * Px, gy0 = y0 * Px, gx1 = (x1 + 1) * Px, gy1 = (y1 + 1) * Px;

            // 1. floors, warped borders, water banks, elevation tint - cell by cell, so per-cell facts are read once
            var dark = new Color32(18, 18, 19, 255);
            var macroLat = GetLat(80, 31);
            var stainLat = GetLat(40, 92); var stainLat2 = GetLat(11, 93); var Grime = new Color32(34, 30, 24, 255);   // Batch 92
            // every lattice the loop can touch is made up front, so the rows can be painted in parallel
            if (_wx1 == null) { _wx1 = GetLat(23, 21); _wx2 = GetLat(7, 23); _wy1 = GetLat(23, 22); _wy2 = GetLat(7, 24); }
            for (int i = 0; i < Styles.Length; i++) if (Styles[i] != null) Prime(Styles[i], i);
            foreach (var cs in new[] { RockTop, ForestFloor, RedFloor, ScrapTop }) Prime(cs, 20);
            System.Action<int> paintRow = cy =>
            {
                for (int cx = x0; cx <= x1; cx++)
                {
                    int ci = cy * _w + cx;
                    bool buried = _buried[ci], core = _core[ci], uni = _uniform[ci];
                    var own = _ground[ci];
                    string ml = _look[ci];
                    var coreStyle = !core ? null : ml == "cliff" ? RockTop : ml == "foliage" ? ForestFloor : ml == "redfoliage" ? RedFloor : ScrapTop;
                    int hgt = TileAt(cx, cy).Height;
                    // a kerb where pavement meets the road (N, E, S, W)
                    bool kerb = own == GroundType.Concrete && !core && !buried;
                    bool kN = kerb && cy > 0 && _ground[ci - _w] == GroundType.Asphalt, kS = kerb && cy < _h - 1 && _ground[ci + _w] == GroundType.Asphalt;
                    bool kW = kerb && cx > 0 && _ground[ci - 1] == GroundType.Asphalt, kE = kerb && cx < _w - 1 && _ground[ci + 1] == GroundType.Asphalt;
                    kerb = kN || kS || kW || kE;
                    float hShade = Mathf.Min(0.15f, Mathf.Abs(hgt) * 0.025f);
                    // Batch 92 (Yan: "dark, gritty, dirty"): indoor floors carry water stains and dirt banked against the walls
                    bool indoor = !core && !buried && (own == GroundType.Floor || own == GroundType.LabTile || _interior && own == GroundType.Concrete);
                    bool wN = indoor && Walled(cx, cy - 1), wS = indoor && Walled(cx, cy + 1), wW = indoor && Walled(cx - 1, cy), wE = indoor && Walled(cx + 1, cy);
                    var hCol = hgt > 0 ? White : Black;
                    for (int py = 0; py < Px; py++)
                    {
                        int gy = cy * Px + py, row = (H - 1 - gy) * W;
                        for (int pxl = 0; pxl < Px; pxl++)
                        {
                            int gx = cx * Px + pxl;
                            if (buried) { px[row + gx] = dark; continue; }
                            if (_slab != null && _slab[ci]) { float n = H01(gx >> 1, gy >> 1, 41); px[row + gx] = Lerp(SlabA, SlabB, n * 0.6f); continue; }
                            Color32 c;
                            var g = own;
                            if (core) c = Procedural(coreStyle, 20, gx, gy);
                            else
                            {
                                if (!uni) g = GroundPx(gx, gy);
                                var style = Styles[(int)g];
                                var s = tex[(int)g];
                                c = style != null ? Procedural(style, (int)g, gx, gy)
                                  : s != null ? At(s, gx % s.W, gy % s.H) : new Color32(60, 60, 56, 255);
                                if (g == GroundType.Water && !uni)
                                {
                                    if (gy > 0 && GroundPx(gx, gy - 1) != GroundType.Water) c = Lerp(c, new Color32(0x5A, 0x48, 0x38, 255), 0.85f);       // muddy lip
                                    else if (gy > 2 && (GroundPx(gx, gy - 2) != GroundType.Water || GroundPx(gx, gy - 3) != GroundType.Water)) c = Mul(c, 0.72f);   // bank shadow
                                }
                            }
                            if (g != GroundType.Floor && g != GroundType.LabTile)
                            {
                                float macro = macroLat.At(gx, gy) - 0.5f;                     // big soft light/dark patches
                                c = Lerp(c, macro > 0 ? White : Black, Mathf.Abs(macro) * 0.07f);
                            }
                            if (g == GroundType.Concrete && !core && ((gx & 31) == 0 || (gy & 31) == 0)) c = Mul(c, 0.84f);   // slab joints
                            if (kerb)
                            {
                                int e = System.Math.Min(System.Math.Min(kN ? py : 99, kS ? Px - 1 - py : 99), System.Math.Min(kW ? pxl : 99, kE ? Px - 1 - pxl : 99));
                                if (e == 0) c = KerbDark; else if (e == 1) c = KerbLight;
                            }
                            if (g == GroundType.Floor && _outside != null && _outside[ci])   // Batch 97: a roofless floor is overgrown
                            {
                                var gs = Styles[(int)GroundType.Grass];
                                if (gs != null)
                                {
                                    float k = Mathf.Clamp01((stainLat2.At(gx * 1.5f, gy * 1.5f) - 0.3f) * 2.6f);
                                    c = Lerp(c, Procedural(gs, (int)GroundType.Grass, gx, gy), k);
                                }
                            }
                            if (indoor)
                            {
                                float st = stainLat.At(gx, gy) * 0.65f + stainLat2.At(gx, gy) * 0.35f;
                                float k = Mathf.Clamp01((st - 0.45f) * 2.2f) * (_interior ? 0.6f : 0.4f);
                                int e = System.Math.Min(System.Math.Min(wN ? py : 99, wS ? Px - 1 - py : 99), System.Math.Min(wW ? pxl : 99, wE ? Px - 1 - pxl : 99));
                                if (e < 5) k = Mathf.Max(k, (5 - e) * 0.09f * (0.6f + 0.8f * stainLat2.At(gx * 2, gy * 2)));
                                c = Lerp(Mul(c, _interior ? 0.78f : 0.88f), Grime, k);
                            }
                            if (hgt != 0) c = Lerp(c, hCol, hShade);
                            px[row + gx] = c;
                        }
                    }
                }
            };
            if (y1 - y0 > 16) System.Threading.Tasks.Parallel.For(y0, y1 + 1, paintRow);
            else for (int cy = y0; cy <= y1; cy++) paintRow(cy);

            // 2. flats and decals per cell (a cell's decal may spill a few px; repaint keeps it inside the region)
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    PaintFlats(px, x, y, gx0, gy0, gx1, gy1);

            // 3. shadows from walls and standing things near the region
            bool sun = _sunA > 0f;
            if (sun) CastShadows(px, x0, y0, x1, y1, gx0, gy0, gx1, gy1);
            for (int y = y0 - 3; y <= y1 + 1; y++)
                for (int x = x0 - 3; x <= x1 + 3; x++)
                {
                    if (x < 0 || y < 0 || x >= _w || y >= _h) continue;
                    if (!StandingAt(x, y, out var st)) continue;
                    if (st.Spr.Name.StartsWith("w_")) { if (!sun) WallShadow(px, x, y, gx0, gy0, gx1, gy1); }   // Batch 94: in sun the contact blobs stay, under the cast shadow
                    else if (st.Spr.ShW > 0) Ellipse(px, st.X + st.Spr.ShDx, st.Y + st.Spr.ShDy, st.Spr.ShW, st.Spr.ShH, gx0, gy0, gx1, gy1);
                }
            DrawWires(px, gx0, gy0, gx1, gy1);   // Batch 96: power lines, over the ground and its shadows
        }

        // ------------------------------------------------------------------ power lines (Batch 96, Yan said yes: the concept's wires)
        // Each pole is joined to its nearest pole within 15 tiles by two wires from the crossbar ends, sagging between them.
        // ponytail: painted with the ground, so a tree north of the line can draw over a wire; an overlay layer if it shows.
        private static List<(int ax, int ay, int bx, int by)> _wires;
        private static string[] _wiresFor;
        private static void DrawWires(Color32[] px, int gx0, int gy0, int gx1, int gy1)
        {
            if (!ReferenceEquals(_wiresFor, _look))
            {
                _wiresFor = _look; _wires = new List<(int, int, int, int)>();
                var poles = new List<(int x, int y)>();
                for (int y = 0; y < _h; y++) for (int x = 0; x < _w; x++) if (_look[y * _w + x] == "pole") poles.Add((x, y));
                foreach (var a in poles)
                {
                    (int x, int y) best = (-1, -1); int bd = 15 * 15 + 1;
                    foreach (var b in poles)
                    {
                        int d = (a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y);
                        if (d > 0 && d < bd && (b.y > a.y || b.y == a.y && b.x > a.x)) { bd = d; best = b; }   // each pair once
                    }
                    if (best.x >= 0) _wires.Add((a.x, a.y, best.x, best.y));
                }
            }
            int W = _w * Px, H = _h * Px;
            var col = new Color32(28, 28, 30, 255);
            foreach (var (ax, ay, bx, by) in _wires)
                foreach (int off in new[] { -7, 6 })                        // the crossbar's two ends
                {
                    float x0 = ax * Px + 8 + off, y0 = ay * Px + 13 - 54, x1 = bx * Px + 8 + off, y1 = by * Px + 13 - 54;
                    float len = Mathf.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0)), sag = 4f + len * 0.05f;
                    int n = (int)len + 1, lastX = int.MinValue, lastY = int.MinValue;
                    for (int i = 0; i <= n; i++)
                    {
                        float t = i / (float)n;
                        int x = Mathf.RoundToInt(x0 + (x1 - x0) * t), y = Mathf.RoundToInt(y0 + (y1 - y0) * t + sag * 4f * t * (1f - t));
                        if (x == lastX && y == lastY) continue; lastX = x; lastY = y;
                        if (x < gx0 || x >= gx1 || y < gy0 || y >= gy1 || x < 0 || y < 0 || x >= W || y >= H) continue;
                        int k = (H - 1 - y) * W + x; px[k] = Lerp(px[k], col, 0.85f);
                    }
                }
        }

        // ------------------------------------------------------------------ sun
        // Clear daytime: every standing sprite's silhouette is sheared onto the ground - a pixel `up` px
        // above its foot lands up*Kx right and up*Ky down. Morning shadows run west, noon short, evening east.
        // Batch 94 (Yan: "real lighting, shadows affected by the sun, gradual passage of time"): soft-edged, darkest at the
        // foot and lighter higher up, moving every 10 game minutes; overcast skies give faint diffuse ones. Fog, rain,
        // storm, night and indoors keep only the contact blobs.
        private static float _sunKx, _sunKy, _sunA;
        private static int _sunKey = -1;
        private static readonly Color32 SunShadowCol = new Color32(26, 24, 40, 255);   // cool, as in the concept
        public static float SunStrength = 0.85f, OvercastStrength = 0.3f;             // tuning knobs
        private static float[] _maskBuf;
        private static int _soft = 1;
        private static readonly Color32 SlabA = new Color32(120, 117, 108, 255), SlabB = new Color32(98, 96, 89, 255);   // concrete wall top           // blur passes: overcast shadows are softer
        private static byte[] _shade;           // 1 = in cast shadow, map px (y down); read by units

        /// <summary>Changes when the cast shadows need a re-bake: 10-minute steps of daylight, +1000 when overcast; -1 = none.
        /// A re-bake costs ~0.45 s on a 200x200 map, off the main thread; a step comes every ~2.4 s of exploring.</summary>
        public static int SunKey()
        {
            var w = Atmosphere.Current;
            if (_interior || w != Weather.Clear && w != Weather.Overcast) return -1;
            float h = MacroState.Current.Hour;
            return Mathf.Abs(h - 12f) > 7f ? -1 : (int)(h * 6f) + (w == Weather.Overcast ? 1000 : 0);
        }

        private static void SetSun()
        {
            _sunKey = SunKey(); _sunA = 0f;
            if (_sunKey < 0) return;
            bool overcast = _sunKey >= 1000;
            float d = (_sunKey % 1000 + 0.5f) / 6f - 12f, t = Mathf.Clamp(d / 6f, -1f, 1f);
            _sunKx = 1.7f * t; _sunKy = 0.22f + 0.12f * Mathf.Abs(t);   // the golden concept: (1.6, 0.32)
            _sunA = (overcast ? OvercastStrength : SunStrength) * Mathf.Clamp01(7f - Mathf.Abs(d));
            _soft = overcast ? 2 : 1;
        }

        /// <summary>Is map pixel (gx, gy) (y down) inside a baked cast shadow?</summary>
        public static bool InShade(int gx, int gy)
        {
            var s = _shade; int W = _w * Px;
            return s != null && gx >= 0 && gy >= 0 && gx < W && gy < _h * Px && s[gy * W + gx] != 0;
        }

        private static void CastShadows(Color32[] px, int x0, int y0, int x1, int y1, int gx0, int gy0, int gx1, int gy1)
        {
            int W = _w * Px, H = _h * Px, rw = gx1 - gx0, rh = gy1 - gy0;
            if (_maskBuf == null || _maskBuf.Length < rw * rh) _maskBuf = new float[rw * rh]; else System.Array.Clear(_maskBuf, 0, rw * rh);
            var mask = _maskBuf;
            if (_shade == null || _shade.Length != W * H) _shade = new byte[W * H];
            for (int gy = gy0; gy < gy1; gy++) System.Array.Clear(_shade, gy * W + gx0, rw);
            void Mark(int wx, int groundY, int up)
            {
                int ty = groundY + Mathf.RoundToInt(up * _sunKy) - gy0, tx = wx + Mathf.RoundToInt(up * _sunKx) - gx0;
                if (ty < 0 || ty >= rh || tx < 0 || tx >= rw) return;
                float v = 1f - 0.35f * Mathf.Min(1f, up / 64f);        // darkest at the foot, lighter where the top lands
                if (mask[ty * rw + tx] < v) mask[ty * rw + tx] = v;
                if (tx + 1 < rw && mask[ty * rw + tx + 1] < v) mask[ty * rw + tx + 1] = v;     // the shear spreads rows apart: fill the gap
            }
            // reach: the tallest sprites are ~48 px, so a shadow travels at most ~70 px sideways and ~22 down
            int rx = 6, ryUp = 2;
            for (int y = y0 - ryUp; y <= y1 + 1; y++)
                for (int x = x0 - rx; x <= x1 + rx; x++)
                {
                    if (x < 0 || y < 0 || x >= _w || y >= _h || !StandingAt(x, y, out var st)) continue;
                    var s = st.Spr; int left = st.Left, top = st.Top;
                    // a 3/4 wall sprite is a face (H - 16 px tall) plus its top surface: the top's rows are depth, not height
                    int hw = s.Name.StartsWith("w_") ? s.H - Px : int.MaxValue;
                    for (int sy = 0; sy < s.H; sy++)
                    {
                        int up = st.Y - (top + sy);
                        if (up < 0) continue;
                        for (int sx = 0; sx < s.W; sx++)
                        {
                            if (At(s, sx, sy).a < 128) continue;
                            if (up <= hw) Mark(left + sx, st.Y, up);
                            else for (int u = 0; u <= hw; u++) Mark(left + sx, st.Y - (up - hw), u);   // the wall's whole height stands on this spot
                        }
                    }
                }
            for (int pass = 0; pass < _soft; pass++) mask = Blur3(mask, rw, rh);   // soft edges
            for (int j = 0; j < rh; j++)
                for (int i = 0; i < rw; i++)
                {
                    float m = mask[j * rw + i];
                    if (m < 0.02f) continue;
                    int gx = gx0 + i, gy = gy0 + j;
                    var fl = _ground[(gy / Px) * _w + gx / Px];
                    if (fl == GroundType.Floor || fl == GroundType.LabTile) continue;   // indoors: under a roof, no sun
                    if (m > 0.5f) _shade[gy * W + gx] = 1;
                    int k = (H - 1 - gy) * W + gx;
                    px[k] = Lerp(px[k], SunShadowCol, _sunA * m);
                }
        }

        /// <summary>3x3 box blur of a shadow mask, in place: a horizontal then a vertical pass, rows in parallel.</summary>
        private static float[] _blurTmp;
        private static float[] Blur3(float[] a, int w, int h)
        {
            if (_blurTmp == null || _blurTmp.Length < a.Length) _blurTmp = new float[a.Length];
            var t = _blurTmp;
            System.Threading.Tasks.Parallel.For(0, h, y =>
            {
                int r = y * w;
                for (int x = 0; x < w; x++)
                {
                    float l = x > 0 ? a[r + x - 1] : a[r + x], rr = x < w - 1 ? a[r + x + 1] : a[r + x];
                    t[r + x] = (l + a[r + x] + rr) / 3f;
                }
            });
            System.Threading.Tasks.Parallel.For(0, h, y =>
            {
                int up = y > 0 ? y - 1 : y, dn = y < h - 1 ? y + 1 : y;
                for (int x = 0; x < w; x++) a[y * w + x] = (t[up * w + x] + t[y * w + x] + t[dn * w + x]) / 3f;
            });
            return a;
        }

        private static void PaintFlats(Color32[] px, int x, int y, int gx0, int gy0, int gx1, int gy1)
        {
            var t = TileAt(x, y);
            string l = _look[y * _w + x];
            Spr s = null; int dx = 0, dy = 0;
            if (l != null && Lines.Contains(l))
            {
                string b = l == "rail_v" ? "rail" : l;
                int m = 0;
                string L(int ax, int ay) { var k = LookAt(ax, ay); return k == "rail_v" ? "rail" : k; }
                if (L(x, y - 1) == b) m |= 1; if (L(x + 1, y) == b) m |= 2; if (L(x, y + 1) == b) m |= 4; if (L(x - 1, y) == b) m |= 8;
                if (m == 0 && l == "rail_v") m = 5;
                s = Get($"f_{b}_{m}");
            }
            else if (l == "platform") s = Pick("f_platform", H01(x, y, 6));
            else if (t.Type == TileType.DoorOpen) s = Get(l == "door_broken" ? "f_door_broken_0" : "f_door_open_0");
            else if (l != null && Decal.Contains(l))
            {
                s = Pick("d_" + l, H01(x, y, 7));
                dx = (int)((H01(x, y, 8) - 0.5f) * 6); dy = (int)((H01(x, y, 10) - 0.5f) * 6);
            }
            else if (l == null && (t.Type == TileType.Empty || t.Type == TileType.Difficult && (_ground[y * _w + x] == GroundType.Water || _ground[y * _w + x] == GroundType.Mud)) && H01(x, y, 11) < (_ground[y * _w + x] == GroundType.Grass || _ground[y * _w + x] == GroundType.DarkGrass || _ground[y * _w + x] == GroundType.Mud ? 0.24f : 0.1f))
            {
                s = Pick(ScatterDecal(_ground[y * _w + x], H01(x, y, 12)), H01(x, y, 13));
                dx = (int)((H01(x, y, 8) - 0.5f) * 8); dy = (int)((H01(x, y, 10) - 0.5f) * 8);
            }
            if (s == null) return;
            // flats are 16x16 and cover their cell; decals centre on it
            int left = s.W == Px && s.H == Px ? x * Px : x * Px + 8 - s.W / 2 + dx;
            int top = s.W == Px && s.H == Px ? y * Px : y * Px + 8 - s.H / 2 + dy;
            Blit(px, s, left, top, gx0, gy0, gx1, gy1);
        }

        private static string ScatterDecal(GroundType g, float r)
        {
            switch (g)
            {
                case GroundType.Grass: case GroundType.DarkGrass:
                    return r < 0.45f ? "d_tallgrass" : r < 0.72f ? "d_tallgrass_dry" : r < 0.8f ? "d_tuft" : r < 0.86f ? "d_flowers" : r < 0.93f ? "d_pebbles" : "d_leaves";
                case GroundType.Dirt: return r < 0.4f ? "d_pebbles" : r < 0.65f ? "d_tallgrass_dry" : r < 0.85f ? "d_leaves" : "d_litter";
                case GroundType.Mud: return r < 0.3f ? "d_puddle" : r < 0.65f ? "d_tallgrass_dry" : r < 0.9f ? "d_tallgrass" : "d_pebbles";   // sedge in the mud
                case GroundType.Asphalt: case GroundType.Concrete: return r < 0.45f ? "d_crack" : r < 0.7f ? "d_oil" : "d_litter";
                case GroundType.Floor: case GroundType.LabTile: return r < 0.6f ? "d_litter" : "d_crack";
                case GroundType.Sand: case GroundType.Gravel: return r < 0.6f ? "d_pebbles" : "d_tallgrass_dry";
                case GroundType.CaveFloor: return r < 0.85f ? "d_pebbles" : "d_bones";
                case GroundType.Water: return r < 0.45f ? "d_lily" : null;          // pads on still water
                default: return null;
            }
        }

        private static void Blit(Color32[] px, Spr s, int left, int top, int gx0, int gy0, int gx1, int gy1)
        {
            int W = _w * Px, H = _h * Px;
            for (int y = 0; y < s.H; y++)
            {
                int gy = top + y; if (gy < gy0 || gy >= gy1) continue;
                for (int x = 0; x < s.W; x++)
                {
                    int gx = left + x; if (gx < gx0 || gx >= gx1) continue;
                    var c = At(s, x, y); if (c.a == 0) continue;
                    int i = (H - 1 - gy) * W + gx;
                    px[i] = Over(px[i], c);
                }
            }
        }

        private static void Darken(Color32[] px, int gx, int gy, float a, int gx0, int gy0, int gx1, int gy1)
        {
            if (gx < gx0 || gy < gy0 || gx >= gx1 || gy >= gy1) return;
            int i = (_h * Px - 1 - gy) * _w * Px + gx;
            px[i] = Lerp(px[i], ShadowCol, a);
        }

        private static void Ellipse(Color32[] px, int cx, int cy, int w, int h, int gx0, int gy0, int gx1, int gy1)
        {
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float ex = (x + 0.5f - w / 2f) / (w / 2f), ey = (y + 0.5f - h / 2f) / (h / 2f);
                    if (ex * ex + ey * ey <= 1f) Darken(px, cx - w / 2 + x, cy - h / 2 + y, 0.5f, gx0, gy0, gx1, gy1);
                }
        }

        /// <summary>Light from the top-left: a wall darkens the strip of ground just south and east of its face.</summary>
        private static void WallShadow(Color32[] px, int x, int y, int gx0, int gy0, int gx1, int gy1)
        {
            int m = WallMask(x, y);
            bool thin = !Mass.Contains(LookAt(x, y) ?? "");
            int xa = thin && (m & 10) == 0 ? 3 : 0, xb = thin && (m & 10) == 0 ? 13 : 16;
            if ((m & 4) == 0)
                for (int k = 0; k < 4; k++)
                    for (int i = xa + k; i < xb + (k < 2 ? 1 : 0); i++) Darken(px, x * Px + i, (y + 1) * Px + k, 0.45f - k * 0.08f, gx0, gy0, gx1, gy1);
            if ((m & 2) == 0)
                for (int k = 0; k < 2; k++)
                    for (int j = 4; j < Px + 2; j++) Darken(px, (x + 1) * Px - (thin ? 3 : 0) + k, y * Px + j, 0.35f, gx0, gy0, gx1, gy1);
        }

        /// <summary>Tests: looks in the current zone (after Begin) with no art to draw them.</summary>
        public static HashSet<string> MissingArt()
        {
            var miss = new HashSet<string>();
            for (int y = 0; y < _h; y++)
                for (int x = 0; x < _w; x++)
                {
                    string l = _look[y * _w + x];
                    if (l == null || l == "part" || Lines.Contains(l) || Decal.Contains(l) || l == "platform" || l == "door_open" || l == "door_broken") continue;
                    var t = TileAt(x, y);
                    bool wall = t.Type == TileType.Door || t.Type == TileType.Solid && (BuiltWall.Contains(l) || Mass.Contains(l) || l == "window" || l == "metaldoor");
                    if (wall) { if (!StandingAt(x, y, out _) && !_buried[y * _w + x] && !_core[y * _w + x]) miss.Add($"wall {l} {WallKey(x, y)}"); }
                    else if (Pick("p_" + l, 0f) == null) miss.Add(l);
                }
            return miss;
        }

        /// <summary>Harness / debugging: ground + every standing sprite painted in sort order.</summary>
        public static bool NoRoofs;   // Batch 92: render harness (NOROOF=1) shows every building's insides

        public static Color32[] ComposePreview(string zoneId)
        {
            Begin(zoneId);
            if (_atlas == null) return null;
            var px = ComposeGround();
            var all = new List<Standing>();
            for (int y = 0; y < _h; y++) for (int x = 0; x < _w; x++) if (StandingAt(x, y, out var st)) all.Add(st);
            all.Sort((a, b) => a.Sort != b.Sort ? a.Sort.CompareTo(b.Sort) : a.X.CompareTo(b.X));
            // roofs (Batch 34) go in at their sort position: over what's inside, under what's in front
            RoofView.Find();
            var roofs = new List<RoofView.Building>();
            int inside = Vision.PlayerX >= 0 && Vision.PlayerX < _w && Vision.PlayerY >= 0 && Vision.PlayerY < _h ? RoofView.IdMap[Vision.PlayerY * _w + Vision.PlayerX] : -1;
            foreach (var b in RoofView.Buildings)
            {
                if (b.Id == inside || NoRoofs) continue;              // the player's own roof is off (NoRoofs: a harness floor-plan view)
                if (Vision.PlayerX != int.MinValue) RoofView.Cut(b);   // a harness view from a player: cutaways as in game
                roofs.Add(b);
            }
            roofs.Sort((a, b) => RoofView.SortOf(a).CompareTo(RoofView.SortOf(b)));
            int r = 0;
            foreach (var st in all)
            {
                while (r < roofs.Count && RoofView.SortOf(roofs[r]) < st.Sort) RoofView.PaintInto(px, roofs[r++]);
                Blit(px, st.Spr, st.Left, st.Top, 0, 0, _w * Px, _h * Px);
            }
            while (r < roofs.Count) RoofView.PaintInto(px, roofs[r++]);
            return px;
        }

        private static Color32 Over(Color32 dst, Color32 src)
        {
            if (src.a == 255) return src;
            float a = src.a / 255f;
            return new Color32((byte)(src.r * a + dst.r * (1 - a)), (byte)(src.g * a + dst.g * (1 - a)), (byte)(src.b * a + dst.b * (1 - a)), 255);
        }
        private static Color32 Mul(Color32 c, float f) => new Color32((byte)(c.r * f), (byte)(c.g * f), (byte)(c.b * f), 255);
        private static Color32 Lerp(Color32 a, Color32 b, float t) =>
            new Color32((byte)(a.r + (b.r - a.r) * t), (byte)(a.g + (b.g - a.g) * t), (byte)(a.b + (b.b - a.b) * t), 255);
    }
}
