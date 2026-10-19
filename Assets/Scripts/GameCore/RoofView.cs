// Batch 34: roofs. Every walled-in space gets a roof at wall height (lifted 18 px, flush with the wall
// caps), drawn over everything inside it. The roof comes off:
//   - completely while the player is inside that building;
//   - room by room from outside, for rooms the player can see into (through a door, a window, a
//     broken wall). Inner walls keep their strip of roof, which reads as the wall top.
// Buildings are found by a flood fill from the map edge that stops at walls and doors (like
// MapDresser.Indoors), split into connected rooms-groups. Pixels are generated here (tar roofs with a
// parapet shadow, moss and vents; corrugated tin for metal/plank/hull sheds).
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public class RoofView
    {
        public const int Lift = 28;                       // wall face height in px of one storey (walls.py STOREY; Batch 106, was 18)
        public const int LiftTall = 40;                   // Batch 119: brick, plaster and concrete buildings are two storeys (walls.py STOREY2)
        public static readonly System.Collections.Generic.HashSet<string> TwoStorey = new System.Collections.Generic.HashSet<string> { "wall", "plaster", "concrete" };
        static readonly HashSet<string> Built = new HashSet<string> { "wall", "plaster", "concrete", "plank", "metal", "hull", "window", "metaldoor" };
        static readonly HashSet<string> Tin = new HashSet<string> { "metal", "plank", "hull" };

        public enum Roof { Tar, Tin, Slab, Gable, Hip, Saw }
        public enum Cover { Tile, Slate, TinGreen, TinRed, TinBare, Felt }

        public sealed class Building
        {
            public int Id, X0, Y0, X1, Y1;               // roof cell bounds (inclusive)
            public bool[] Cell;                          // roof over (x - X0, y - Y0)
            public bool TinRoof;
            // Batch 122 (Yan: "different roofs that fit each kind of building"): the roof's shape and covering, from the walls'
            // material and the building's size; Front is the side its main door is on (0 S, 1 E, 2 W, 3 N)
            public Roof Style; public Cover Mat; public string WallMat; public int Front; public bool RidgeNS;
            public int Lift = RoofView.Lift;             // Batch 119: the wall height its roof sits on
            public Color32[] Px, Base;                   // roof pixels (texture covers the bounds + 1 cell margin), Unity row order
            public int TW => (W + 2) * TerrainArt.Px;
            public int TH => (H + 2) * TerrainArt.Px;
            public bool[] Open;                          // cells currently cut away
            public int[] Room;                           // room index per cell (-1: a wall under the roof)
            public int Rooms;
            public SpriteRenderer Sr; public Texture2D Tex;
            public float Alpha = 1f;
            public int W => X1 - X0 + 1;
            public int H => Y1 - Y0 + 1;
        }

        public static int[] IdMap { get; private set; }   // building id per cell (-1 = none); roof cells and the building's interior
        public static List<Building> Buildings { get; private set; } = new List<Building>();
        static int _w, _h;
        static RoofView _live;

        /// <summary>Is (x, y) under a roof that is currently drawn (not cut away / faded)?</summary>
        public static bool Covered(int x, int y)
        {
            if (_live == null || IdMap == null || x < 0 || y < 0 || x >= _w || y >= _h) return false;
            int id = IdMap[y * _w + x]; if (id < 0) return false;
            var b = Buildings[id];
            return b.Alpha > 0.5f && b.Cell[(y - b.Y0) * b.W + x - b.X0] && !b.Open[(y - b.Y0) * b.W + x - b.X0];
        }

        static bool Floored(int x, int y) { var g = TerrainMap.GetTile(x, y).Ground; return g == GroundType.Floor || g == GroundType.LabTile; }

        static bool WallAt(int x, int y)
        {
            var t = TerrainMap.GetTile(x, y);
            if (t.Type == TileType.Door || t.Type == TileType.DoorOpen) return true;
            return t.Type == TileType.Solid && t.Look != null && Built.Contains(t.Look);
        }

        /// <summary>Finds the buildings of the current map and paints their roofs (no Unity objects: testable).</summary>
        public static void Find()
        {
            _w = TerrainMap.Width; _h = TerrainMap.Height;
            int W = _w, H = _h;
            if (ZoneLayoutRecipes.IsInterior(TerrainMap.ZoneId))
            {
                // underground there is no sky to roof over: a crypt or bunker is all "indoors" and must stay visible
                IdMap = new int[W * H]; for (int i = 0; i < IdMap.Length; i++) IdMap[i] = -1;
                Buildings = new List<Building>();
                return;
            }
            var outside = new bool[W * H]; var q = new Queue<int>();
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
                    if ((x < 5 || y < 5 || x >= W - 5 || y >= H - 5) && !WallAt(x, y) && !Floored(x, y)) { outside[y * W + x] = true; q.Enqueue(y * W + x); }
            while (q.Count > 0)
            {
                int i = q.Dequeue(), x = i % W, y = i / W;
                foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    int nx = x + dx, ny = y + dy, n = ny * W + nx;
                    if (nx < 0 || ny < 0 || nx >= W || ny >= H || outside[n] || WallAt(nx, ny)) continue;
                    outside[n] = true; q.Enqueue(n);
                }
            }
            // a roof cell: indoors, or a wall with indoors/walls all round (inner partitions); outer walls keep their caps
            bool Inside(int x, int y) => x >= 0 && y >= 0 && x < W && y < H && !outside[y * W + x] && !WallAt(x, y);
            bool Roof(int x, int y)
            {
                if (x < 1 || y < 1 || x >= W - 1 || y >= H - 1 || outside[y * W + x]) return false;
                if (!WallAt(x, y)) return true;
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) if (outside[(y + dy) * W + x + dx]) return false;
                return true;
            }
            IdMap = new int[W * H]; for (int i = 0; i < IdMap.Length; i++) IdMap[i] = -1;
            Buildings = new List<Building>();
            var cells = new List<int>();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (IdMap[y * W + x] >= 0 || !Roof(x, y)) continue;
                    int id = Buildings.Count; cells.Clear();
                    IdMap[y * W + x] = id; q.Enqueue(y * W + x);
                    int x0 = x, x1 = x, y0 = y, y1 = y;
                    while (q.Count > 0)
                    {
                        int i = q.Dequeue(), cx = i % W, cy = i / W; cells.Add(i);
                        x0 = System.Math.Min(x0, cx); x1 = System.Math.Max(x1, cx); y0 = System.Math.Min(y0, cy); y1 = System.Math.Max(y1, cy);
                        foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                        {
                            int nx = cx + dx, ny = cy + dy;
                            if (nx < 0 || ny < 0 || nx >= W || ny >= H || IdMap[ny * W + nx] >= 0 || !Roof(nx, ny)) continue;
                            IdMap[ny * W + nx] = id; q.Enqueue(ny * W + nx);
                        }
                    }
                    var b = new Building { Id = id, X0 = x0, Y0 = y0, X1 = x1, Y1 = y1 };
                    b.Cell = new bool[b.W * b.H]; b.Open = new bool[b.W * b.H];
                    foreach (int i in cells) b.Cell[(i / W - y0) * b.W + i % W - x0] = true;
                    // rooms: the indoor cells split by walls and doors
                    b.Room = new int[b.W * b.H]; for (int k = 0; k < b.Room.Length; k++) b.Room[k] = -1;
                    var rq = new Queue<int>();
                    for (int k = 0; k < b.Room.Length; k++)
                    {
                        int kx = x0 + k % b.W, ky = y0 + k / b.W;
                        if (!b.Cell[k] || b.Room[k] >= 0 || WallAt(kx, ky)) continue;
                        b.Room[k] = b.Rooms; rq.Enqueue(k);
                        while (rq.Count > 0)
                        {
                            int c = rq.Dequeue(), cx = c % b.W, cy = c / b.W;
                            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                            {
                                int nx = cx + dx, ny = cy + dy, n = ny * b.W + nx;
                                if (nx < 0 || ny < 0 || nx >= b.W || ny >= b.H || !b.Cell[n] || b.Room[n] >= 0 || WallAt(x0 + nx, y0 + ny)) continue;
                                b.Room[n] = b.Rooms; rq.Enqueue(n);
                            }
                        }
                        b.Rooms++;
                    }
                    // roof material: the outer walls' (majority)
                    int tin = 0, other = 0, tall = 0;
                    var mats = new Dictionary<string, int>();
                    int[] doorsOn = new int[4]; int front = -1;
                    for (int yy = y0 - 1; yy <= y1 + 1; yy++)
                        for (int xx = x0 - 1; xx <= x1 + 1; xx++)
                        {
                            if (xx < 0 || yy < 0 || xx >= W || yy >= H || !WallAt(xx, yy)) continue;
                            var t = TerrainMap.GetTile(xx, yy);
                            if (t.Type == TileType.Door || t.Type == TileType.DoorOpen)
                            {
                                bool Out(int ax, int ay) => ax >= 0 && ay >= 0 && ax < W && ay < H && outside[ay * W + ax];
                                int side = Out(xx, yy + 1) ? 0 : Out(xx + 1, yy) ? 1 : Out(xx - 1, yy) ? 2 : Out(xx, yy - 1) ? 3 : -1;
                                if (side >= 0) { doorsOn[side]++; if (t.Look == "front") front = side; }
                                continue;
                            }
                            var l = t.Look; if (l == null) continue;
                            if (Tin.Contains(l)) tin++; else other++;
                            if (TwoStorey.Contains(l)) tall++;
                            if (l != "window") mats[l] = (mats.TryGetValue(l, out var n0) ? n0 : 0) + 1;
                        }
                    b.TinRoof = tin > other;
                    if (tall * 2 > tin + other) b.Lift = LiftTall;
                    if (front < 0) foreach (int sd in new[] { 0, 1, 2, 3 }) if (doorsOn[sd] > 0) { front = sd; break; }
                    b.Front = System.Math.Max(0, front);
                    string wm = null; int best = 0;
                    foreach (var kv in mats) if (kv.Value > best) { best = kv.Value; wm = kv.Key; }
                    b.WallMat = wm ?? "wall";
                    Choose(b, cells.Count == b.W * b.H);
                    Paint(b);
                    Buildings.Add(b);
                }
        }

        static Color32 C(string h) => new Color32(System.Convert.ToByte(h.Substring(0, 2), 16), System.Convert.ToByte(h.Substring(2, 2), 16), System.Convert.ToByte(h.Substring(4, 2), 16), 255);
        static readonly Color32[] Tar = { C("2f2c2a"), C("3a3634"), C("45403c"), C("57524b") };
        static readonly Color32[] TinC = { C("3b4245"), C("525c5e"), C("6b7675"), C("88928d") };
        static readonly Color32[] Rust = { C("3a2219"), C("5e3524"), C("86502e") };
        static readonly Color32 Moss = C("2c3322"), Moss2 = C("363f27"), Vent = C("6b6a64"), VentTop = C("8c8a82"), VentDark = C("2a2a28");

        static float Hash(int x, int y, int k) => ZoneThemes.Hash01(x, y, 7919 * k + 17);
        static float Smooth(int x, int y, int cell, int k)
        {
            float fx = (float)x / cell, fy = (float)y / cell; int ix = Mathf.FloorToInt(fx), iy = Mathf.FloorToInt(fy);
            float tx = fx - ix, ty = fy - iy; tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
            float a = Hash(ix, iy, k), b = Hash(ix + 1, iy, k), c = Hash(ix, iy + 1, k), d = Hash(ix + 1, iy + 1, k);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        /// <summary>Batch 122: which roof a building gets. Huts and village houses are pitched (clay tile, wavy cement sheets,
        /// painted or rusty tin, roofing felt), as in the references (ArtSource/refs/bld_*); blocks keep concept A's flat tar
        /// roof with a parapet; factories and big brick halls a saw-tooth; small concrete boxes a bare slab. A pitched roof's
        /// ridge runs along the front (the main door's side), so a house facing east or west shows its gable end to the south.</summary>
        static void Choose(Building b, bool rect)
        {
            float h = Hash(b.X0, b.Y0, 60), h2 = Hash(b.Y0, b.X0, 61);
            int span = System.Math.Min(b.W, b.H);
            bool pitched = false;
            b.Style = b.TinRoof ? Roof.Tin : Roof.Tar;
            switch (b.WallMat)
            {
                case "plank":
                    if (span <= 10) { pitched = true; b.Mat = h < 0.45f ? Cover.Felt : h < 0.8f ? Cover.TinBare : Cover.TinRed; }
                    break;
                case "metal": case "hull":
                    if (span <= 8 && h < 0.5f) { pitched = true; b.Mat = Cover.TinBare; }
                    else if (span > 12 && h < 0.6f) b.Style = Roof.Saw;              // a workshop hall
                    break;
                case "wall":
                    if (span <= 10) { pitched = true; b.Mat = h < 0.55f ? Cover.Tile : Cover.Slate; }
                    else if (span <= 16) { pitched = true; b.Mat = h < 0.4f ? Cover.Slate : h < 0.7f ? Cover.TinRed : Cover.TinBare; }
                    else b.Style = h < 0.55f ? Roof.Saw : Roof.Tar;
                    break;
                case "plaster":
                    if (span <= 10) { pitched = true; b.Mat = h < 0.35f ? Cover.TinGreen : h < 0.6f ? Cover.TinRed : Cover.Slate; }
                    break;
                case "concrete":
                    if (span <= 8 && b.W * b.H <= 72) b.Style = Roof.Slab;
                    else if (span >= 10 && b.W * b.H >= 150 && h < 0.4f) b.Style = Roof.Saw;   // a factory hall
                    break;
            }
            if (!pitched) return;
            b.Style = rect ? Roof.Gable : Roof.Hip;
            b.RidgeNS = b.Front == 1 || b.Front == 2;
            if (b.RidgeNS ? b.W > 14 : b.H > 14) b.RidgeNS = !b.RidgeNS;   // never a span past ~14 tiles: turn it along the long side
            if (h2 < 0.08f) b.Style = Roof.Hip;                            // the odd hip roof on a plain box
        }

        static readonly Dictionary<Cover, Color32[]> CoverPal = new Dictionary<Cover, Color32[]>
        {
            [Cover.Tile] = new[] { C("4a281f"), C("67372a"), C("7e4433"), C("94553f") },
            [Cover.Slate] = new[] { C("4f514c"), C("666862"), C("7b7d76"), C("8f9088") },
            [Cover.TinGreen] = new[] { C("2f3a2c"), C("3e4c3a"), C("4e5f48"), C("617359") },
            [Cover.TinRed] = new[] { C("3e2520"), C("55322a"), C("6a4033"), C("7d4e3e") },
            [Cover.TinBare] = new[] { C("3b4245"), C("525c5e"), C("6b7675"), C("88928d") },
            [Cover.Felt] = new[] { C("312d2a"), C("403b37"), C("4e4842"), C("625b53") },
        };
        static readonly Color32 Lichen = C("7d7a52"), Batten = C("2a1d17"), Board = C("5e4a38"), Glass = C("2b3539"), Glass2 = C("3f4c51");
        static Color32 Mul(Color32 c, float k) => new Color32((byte)Mathf.Clamp(c.r * k, 0, 255), (byte)Mathf.Clamp(c.g * k, 0, 255), (byte)Mathf.Clamp(c.b * k, 0, 255), 255);

        /// <summary>The gable end's wall (a house turned east or west shows it to the south): the walls' material, in px.</summary>
        static Color32 GableWall(string mat, int wx, int wy)
        {
            switch (mat)
            {
                case "plank": return wx % 4 == 0 ? C("3f3126") : Hash(wx, 0, 70) < 0.5f ? C("5f4a37") : C("6a523d");
                case "plaster": return Hash(wx, wy, 71) < 0.15f ? C("837c6c") : C("9a9282");
                case "concrete": case "metal": case "hull": return Hash(wx, wy, 72) < 0.2f ? C("6a6c66") : C("7d7f78");
                default:
                    bool mortar = wy % 3 == 0 || (wx + (wy / 3 % 2) * 3) % 6 == 0;
                    return mortar ? C("6e5f52") : Hash(wx / 6, wy / 3, 73) < 0.5f ? C("7e4433") : C("8a4c38");
            }
        }

        static void Paint(Building b)
        {
            if (b.Style == Roof.Gable || b.Style == Roof.Hip || b.Style == Roof.Saw || b.Style == Roof.Slab) { PaintShaped(b); return; }
            int P = TerrainArt.Px, pw = b.TW, ph = b.TH;
            b.Px = new Color32[pw * ph];
            bool RoofCell(int cx, int cy) { cx -= 1; cy -= 1; return cx >= 0 && cy >= 0 && cx < b.W && cy < b.H && b.Cell[cy * b.W + cx]; }
            // texture px (gx, gy) -> on the roof? Roof cells, plus up to 4 px into a wall cell next to one (under the thin cap)
            bool In(int gx, int gy)
            {
                if (gx < 0 || gy < 0 || gx >= pw || gy >= ph) return false;
                int cx = gx / P, cy = gy / P;
                if (RoofCell(cx, cy)) return true;
                if (!WallAt(b.X0 - 1 + cx, b.Y0 - 1 + cy)) return false;
                int lx = gx % P, ly = gy % P;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if ((dx == 0 && dy == 0) || !RoofCell(cx + dx, cy + dy)) continue;
                        int ex = dx < 0 ? lx : dx > 0 ? P - 1 - lx : 0, ey = dy < 0 ? ly : dy > 0 ? P - 1 - ly : 0;
                        if (ex < 4 && ey < 4) return true;
                    }
                return false;
            }
            for (int py = 0; py < ph; py++)
                for (int pxl = 0; pxl < pw; pxl++)
                {
                    if (!In(pxl, py)) continue;
                    int wx = (b.X0 - 1) * P + pxl, wy = (b.Y0 - 1) * P + py;          // world px: stable patterns across rebuilds
                    Color32 c;
                    if (b.TinRoof)
                    {
                        c = TinC[1 + (wx % 4 == 0 ? 1 : 0) + (wx % 4 == 1 ? 1 : 0)];
                        if (Smooth(wx, wy, 9, 3) > 0.68f && Hash(wx, wy, 4) < 0.7f) c = Rust[(int)(Hash(wx, wy, 5) * 3)];
                    }
                    else
                    {
                        float v = Hash(wx, wy, 1);
                        c = v < 0.05f ? Tar[3] : v < 0.3f ? Tar[1] : Tar[2];
                        float tone = Smooth(wx, wy, 40, 8) - 0.5f;                                     // patched, re-tarred areas
                        c = new Color32((byte)Mathf.Clamp(c.r * (1f + tone * 0.25f), 0, 255), (byte)Mathf.Clamp(c.g * (1f + tone * 0.25f), 0, 255), (byte)Mathf.Clamp(c.b * (1f + tone * 0.25f), 0, 255), 255);
                        float m = Smooth(wx, wy, 14, 2);
                        if (m > 0.72f && Hash(wx, wy, 6) < (m - 0.72f) * 5f) c = Hash(wx, wy, 7) < 0.5f ? Moss : Moss2;   // moss where water stands
                    }
                    // parapet shadow on the inner top/left edges, lip on the outer edges
                    bool edgeN = !In(pxl, py - 1), edgeW = !In(pxl - 1, py), edgeS = !In(pxl, py + 1), edgeE = !In(pxl + 1, py);
                    if (!b.TinRoof && (!In(pxl, py - 2) || !In(pxl - 2, py))) c = Tar[0];
                    if (edgeN || edgeW || edgeS || edgeE) c = new Color32((byte)(c.r * 0.55f), (byte)(c.g * 0.55f), (byte)(c.b * 0.55f), 255);
                    b.Px[(ph - 1 - py) * pw + pxl] = c;
                }
            // a few vents / boxes on bigger flat roofs
            if (!b.TinRoof)
                for (int k = 0; k < b.W * b.H / 40; k++)
                {
                    int vx = (int)(Hash(b.X0, b.Y0, 20 + k) * (pw - 10)) + 2, vy = (int)(Hash(b.Y0, b.X0, 40 + k) * (ph - 10)) + 2;
                    bool ok = true;
                    for (int y = vy - 1; y < vy + 8 && ok; y++) for (int x = vx - 1; x < vx + 8; x++) if (!In(x, y)) { ok = false; break; }
                    if (!ok) continue;
                    for (int y = 0; y < 7; y++)
                        for (int x = 0; x < 7; x++)
                        {
                            var c = y < 2 ? VentTop : x == 0 || x == 6 || y == 6 ? VentDark : Vent;
                            if (y >= 2 && y <= 4 && x >= 2 && x <= 4) c = VentDark;
                            b.Px[(ph - 1 - (vy + y)) * pw + vx + x] = c;
                        }
                    for (int x = 1; x < 8; x++) { int sy = vy + 7; if (In(vx + x, sy)) { int i = (ph - 1 - sy) * pw + vx + x; b.Px[i] = Tar[0]; } }   // its shadow
                }
            b.Base = (Color32[])b.Px.Clone();
        }

        /// <summary>Batch 122: pitched, saw-tooth and slab roofs. Each roof pixel finds how far the roof's edge is up, down, left
        /// and right of it; that picks its slope (which way it faces, so its light) and how far up the slope it is (the courses
        /// of tiles or sheets run parallel to the eaves).</summary>
        static void PaintShaped(Building b)
        {
            int P = TerrainArt.Px, pw = b.TW, ph = b.TH;
            b.Px = new Color32[pw * ph];
            bool RoofCell(int cx, int cy) { cx -= 1; cy -= 1; return cx >= 0 && cy >= 0 && cx < b.W && cy < b.H && b.Cell[cy * b.W + cx]; }
            var inn = new bool[pw * ph];
            for (int gy = 0; gy < ph; gy++)
                for (int gx = 0; gx < pw; gx++)
                {
                    int cx = gx / P, cy = gy / P;
                    bool on = RoofCell(cx, cy);
                    if (!on && WallAt(b.X0 - 1 + cx, b.Y0 - 1 + cy))
                    {
                        int lx = gx % P, ly = gy % P;
                        for (int dy = -1; dy <= 1 && !on; dy++)
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                if ((dx == 0 && dy == 0) || !RoofCell(cx + dx, cy + dy)) continue;
                                int ex = dx < 0 ? lx : dx > 0 ? P - 1 - lx : 0, ey = dy < 0 ? ly : dy > 0 ? P - 1 - ly : 0;
                                if (ex < 4 && ey < 4) { on = true; break; }
                            }
                    }
                    inn[gy * pw + gx] = on;
                }
            bool In(int x, int y) => x >= 0 && y >= 0 && x < pw && y < ph && inn[y * pw + x];
            int[] dN = new int[pw * ph], dS = new int[pw * ph], dW = new int[pw * ph], dE = new int[pw * ph];
            for (int x = 0; x < pw; x++)
            {
                for (int y = 0; y < ph; y++) dN[y * pw + x] = In(x, y) ? (y > 0 ? dN[(y - 1) * pw + x] : 0) + 1 : 0;
                for (int y = ph - 1; y >= 0; y--) dS[y * pw + x] = In(x, y) ? (y < ph - 1 ? dS[(y + 1) * pw + x] : 0) + 1 : 0;
            }
            for (int y = 0; y < ph; y++)
            {
                for (int x = 0; x < pw; x++) dW[y * pw + x] = In(x, y) ? (x > 0 ? dW[y * pw + x - 1] : 0) + 1 : 0;
                for (int x = pw - 1; x >= 0; x--) dE[y * pw + x] = In(x, y) ? (x < pw - 1 ? dE[y * pw + x + 1] : 0) + 1 : 0;
            }
            // which slope (0 N, 1 S, 2 W, 3 E) and the distance up it from its eave; the south slope is the bigger one (seen from the south)
            int Face(int i)
            {
                switch (b.Style)
                {
                    case Roof.Gable: return b.RidgeNS ? (dW[i] <= dE[i] ? 2 : 3) : (dN[i] * 0.58f < dS[i] * 0.42f ? 0 : 1);
                    case Roof.Hip:
                        float n = dN[i] / 0.42f, so = dS[i] / 0.58f, w = dW[i] / 0.5f, e = dE[i] / 0.5f, m = Mathf.Min(Mathf.Min(n, so), Mathf.Min(w, e));
                        return m == so ? 1 : m == n ? 0 : m == w ? 2 : 3;
                    default: return 1;
                }
            }
            int Up(int i, int f) => f == 0 ? dN[i] : f == 1 ? dS[i] : f == 2 ? dW[i] : dE[i];
            float[] Lit = { 1.3f, 0.9f, 1.12f, 0.78f };
            var pal = CoverPal.TryGetValue(b.Mat, out var cp) ? cp : CoverPal[Cover.Slate];
            Color32[] Concrete = { C("4f514a"), C("62645c"), C("74766d"), C("878980") };
            var face = new int[pw * ph];
            for (int i = 0; i < face.Length; i++) face[i] = inn[i] ? Face(i) : -1;

            for (int py = 0; py < ph; py++)
                for (int pxl = 0; pxl < pw; pxl++)
                {
                    int i = py * pw + pxl;
                    if (!inn[i]) continue;
                    int wx = (b.X0 - 1) * P + pxl, wy = (b.Y0 - 1) * P + py;
                    Color32 c;
                    bool edge = !In(pxl, py - 1) || !In(pxl - 1, py) || !In(pxl, py + 1) || !In(pxl + 1, py);
                    if (b.Style == Roof.Slab)
                    {
                        float v = Hash(wx, wy, 80);
                        c = v < 0.08f ? Concrete[0] : v < 0.5f ? Concrete[1] : Concrete[2];
                        float st = Smooth(wx, wy, 18, 81);
                        if (st > 0.66f) c = Mul(c, 0.86f);                                                      // rain stains
                        if (Smooth(wx, wy, 11, 82) > 0.74f && Hash(wx, wy, 83) < 0.55f) c = Hash(wx, wy, 84) < 0.5f ? Moss : Moss2;
                        int rim = Mathf.Min(Mathf.Min(dN[i], dS[i]), Mathf.Min(dW[i], dE[i]));
                        if (rim == 2) c = Concrete[3];                                                          // the slab's lip
                        if (edge) c = Mul(c, 0.55f);
                        b.Px[(ph - 1 - py) * pw + pxl] = c;
                        continue;
                    }
                    if (b.Style == Roof.Saw)
                    {
                        const int T = 40;
                        int t = (dN[i] - 1) % T;
                        if (t < 7)
                        {   // the north light: a strip of glazing, steel glazing bars, the odd pane gone
                            c = t == 0 || wx % 6 == 0 ? Vent : (Hash(wx / 6, wy / 7, 85) < 0.15f ? VentDark : t < 3 ? Glass2 : Glass);
                        }
                        else
                        {
                            int a = wx % 3;
                            c = a == 0 ? TinC[3] : a == 1 ? TinC[2] : TinC[1];
                            if (Smooth(wx, wy, 9, 86) > 0.68f && Hash(wx, wy, 87) < 0.7f) c = Rust[(int)(Hash(wx, wy, 88) * 3)];
                            c = Mul(c, 1.1f - 0.25f * (t - 7) / (T - 8f));
                            if (t >= T - 2) c = Tar[0];                                                           // the valley gutter
                        }
                        if (edge) c = Mul(c, 0.55f);
                        b.Px[(ph - 1 - py) * pw + pxl] = c;
                        continue;
                    }
                    int f = face[i], d = Up(i, f) - 1, along = f < 2 ? wx : wy;
                    switch (b.Mat)
                    {
                        case Cover.Tile:
                        {
                            int course = d / 3, r = d % 3, tile = (along + (course % 2) * 2) / 4;
                            c = r == 0 ? pal[1] : r == 2 && Hash(tile, course, 90) < 0.5f ? pal[3] : pal[2];
                            if ((along + (course % 2) * 2) % 4 == 0) c = pal[1];
                            if (Hash(tile, course + wy / 97, 91) < 0.15f) c = Mul(c, 0.88f);
                            if (Smooth(wx, wy, 11, 92) > 0.8f && Hash(tile, course, 93) < 0.7f) c = r == 0 ? Mul(Batten, 1.3f) : Batten;   // tiles gone: the battens
                            break;
                        }
                        case Cover.Slate:
                        {
                            int a = along % 4;
                            c = a == 0 ? pal[3] : a == 2 ? pal[1] : pal[2];
                            if (d % 12 == 0) c = pal[0];                                                        // the sheets overlap
                            if (Smooth(wx, wy, 7, 94) > 0.6f && Hash(wx, wy, 95) < 0.18f) c = Lichen;
                            if (Smooth(wx, wy, 13, 96) > 0.82f) c = Batten;                                      // a broken sheet
                            break;
                        }
                        case Cover.Felt:
                        {
                            int r = d % 6;
                            c = r == 0 ? pal[3] : r == 1 ? pal[0] : Hash(wx, wy, 97) < 0.3f ? pal[1] : pal[2];
                            if (Smooth(wx, wy, 10, 98) > 0.76f) c = along % 5 == 0 ? Batten : Board;             // torn off: the boards
                            break;
                        }
                        default:
                        {   // corrugated tin: ribs running down the slope, sheet joints, rust
                            int a = along % 3;
                            c = a == 0 ? pal[3] : a == 1 ? pal[2] : pal[1];
                            if (d % 24 == 0) c = pal[0];
                            if (Smooth(wx, wy, 9, 99) > (b.Mat == Cover.TinBare ? 0.62f : 0.72f) && Hash(wx, wy, 100) < 0.7f) c = Rust[(int)(Hash(wx, wy, 101) * 3)];
                            break;
                        }
                    }
                    if (Smooth(wx, wy, 14, 102) > 0.75f && d < 10 && Hash(wx, wy, 103) < 0.5f) c = Hash(wx, wy, 104) < 0.5f ? Moss : Moss2;   // moss along the eaves
                    c = Mul(c, Lit[f]);
                    // the ridge and hips: where two slopes meet, a lighter cap
                    bool ridge = false;
                    foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1), (2, 0), (-2, 0), (0, 2), (0, -2) })
                        if (In(pxl + dx, py + dy) && face[(py + dy) * pw + pxl + dx] != f) ridge = true;
                    if (ridge) c = Mul(pal[3], f == 0 || f == 2 ? 1.2f : 1.0f);   // a 2-3 px cap, its lit side brighter
                    if (d == 1) c = Mul(c, 0.8f);                                                               // the drip edge
                    // a roof turned north-south shows its gable end to the south: the wall rises above the eaves into a triangle
                    if (b.Style == Roof.Gable && b.RidgeNS)
                    {
                        int span = dW[i] + dE[i];
                        float g = 1f - Mathf.Abs(dW[i] - dE[i]) / (float)span, G = Mathf.Min(18f, span * 0.45f);
                        if (dS[i] <= G * g)
                        {
                            c = Mul(GableWall(b.WallMat, wx, wy), 0.95f);
                            if (G >= 10 && Mathf.Abs(dW[i] - dE[i]) <= 2 && dS[i] >= 3 && dS[i] <= 6) c = C("2a2522");   // the loft vent
                            if (dS[i] > G * g - 1.5f) c = C("2f2722");                                            // the barge board
                        }
                    }
                    if (edge) c = Mul(c, 0.55f);
                    b.Px[(ph - 1 - py) * pw + pxl] = c;
                }
            b.Base = (Color32[])b.Px.Clone();
        }

        // ------------------------------------------------------------------ view
        readonly Transform _root;
        int _seenVersion = -1, _lastInside = -2;

        public RoofView(Transform root)
        {
            _root = root; _live = this;
            Find();
            int P = TerrainArt.Px;
            foreach (var b in Buildings)
            {
                b.Tex = new Texture2D(b.TW, b.TH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                b.Tex.SetPixels32(b.Px); b.Tex.Apply();
                var go = new GameObject($"Roof{b.Id}");
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(b.X0 - 1, -(b.Y0 - 1) + b.Lift / (float)P, 0f);       // top-left (margin cell), lifted to wall height
                b.Sr = go.AddComponent<SpriteRenderer>();
                b.Sr.sprite = Sprite.Create(b.Tex, new Rect(0, 0, b.TW, b.TH), new Vector2(0f, 1f), P);
                // just under the building's south wall: over everything inside, under everything in front of it
                b.Sr.sortingOrder = SortOf(b);
            }
        }

        /// <summary>Per frame: fade the roof the player is under; cut away what the player can see into.</summary>
        public void Tick(Player p)
        {
            if (p == null || IdMap == null) return;
            int inside = p.X >= 0 && p.Y >= 0 && p.X < _w && p.Y < _h ? IdMap[p.Y * _w + p.X] : -1;
            foreach (var b in Buildings)
            {
                float target = b.Id == inside ? 0f : 1f;
                if (b.Alpha != target) { b.Alpha = Mathf.MoveTowards(b.Alpha, target, Time.deltaTime * 4f); b.Sr.color = new Color(1f, 1f, 1f, b.Alpha); }
            }
            if (Vision.Version == _seenVersion && inside == _lastInside) return;
            _seenVersion = Vision.Version; _lastInside = inside;
            foreach (var b in Buildings)
                if (b.Id != inside && Cut(b)) { b.Tex.SetPixels32(b.Px); b.Tex.Apply(); }
        }

        /// <summary>Opens the roof cells over floor the player can see (and closes the rest); true if anything changed.</summary>
        public static bool Cut(Building b)
        {
            int P = TerrainArt.Px; bool changed = false;
            // a room seen into (any of its floor in line of sight) comes off whole: a clean cutaway, not a staircase
            var seen = new bool[b.Rooms];
            for (int i = 0; i < b.Room.Length; i++)
                if (b.Room[i] >= 0 && !seen[b.Room[i]] && Vision.InSight(b.X0 + i % b.W, b.Y0 + i / b.W)) seen[b.Room[i]] = true;
            for (int y = b.Y0; y <= b.Y1; y++)
                for (int x = b.X0; x <= b.X1; x++)
                {
                    int i = (y - b.Y0) * b.W + x - b.X0;
                    if (!b.Cell[i]) continue;
                    bool open = b.Room[i] >= 0 && seen[b.Room[i]];
                    if (open == b.Open[i]) continue;
                    b.Open[i] = open; changed = true;
                    int pw = b.TW, ph = b.TH;
                    for (int py = 0; py < P; py++)
                        for (int pxl = 0; pxl < P; pxl++)
                        {
                            int k = (ph - 1 - ((y - b.Y0 + 1) * P + py)) * pw + (x - b.X0 + 1) * P + pxl;
                            var c = b.Base[k]; if (open) c.a = 0; b.Px[k] = c;
                        }
                }
            return changed;
        }

        /// <summary>Harness: paints one roof (closed) into a full-map ground-order image (row 0 = bottom).</summary>
        public static void PaintInto(Color32[] px, Building b)
        {
            int P = TerrainArt.Px, W = _w * P, H = _h * P, pw = b.TW, ph = b.TH;
            for (int y = 0; y < ph; y++)
                for (int x = 0; x < pw; x++)
                {
                    var c = b.Px[(ph - 1 - y) * pw + x]; if (c.a == 0) continue;
                    int gx = (b.X0 - 1) * P + x, gy = (b.Y0 - 1) * P + y - b.Lift;
                    if (gx < 0 || gy < 0 || gx >= W || gy >= H) continue;
                    px[(H - 1 - gy) * W + gx] = c;
                }
        }
        public static int SortOf(Building b) => WorldArt.SortBase + (b.Y1 + 1) * TerrainArt.Px + TerrainArt.Px - 2;
    }
}
