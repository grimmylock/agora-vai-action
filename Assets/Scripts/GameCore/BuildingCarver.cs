// Batch 30: the authored zones mark many buildings as plain solid blocks (a 10x12 "plaster" rect),
// which the 3x upscale turns into 30x36-tile featureless roofs. This hollows every big rectangular
// block of built wall into an enterable building, Zero Sievert style: 1-tile outer walls with
// windows, a door onto open ground, rooms split off by inner walls (each with a door), and some
// furniture against the walls. Runs after MapUpscaler, before MapDresser. Deterministic per zone.
using System.Collections.Generic;
using System.Linq;
using GameData;

namespace GameCore
{
    public static class BuildingCarver
    {
        static readonly HashSet<string> Built = new HashSet<string> { "wall", "plaster", "concrete", "hull", "metal", "plank" };
        const int MinSize = 7, MaxRoom = 12, MinRoom = 4;

        public static MapLayout Carve(MapLayout m, string zoneId)
        {
            if (m?.Tiles == null) return m;
            int H = m.Tiles.Length, W = m.Tiles[0].Length;
            var theme = ZoneThemes.For(zoneId);
            int seed = TerrainCanvas.Seed(zoneId ?? "x") ^ 0xb11d;
            if (m.Looks == null) { m.Looks = new string[H][]; for (int y = 0; y < H; y++) m.Looks[y] = new string[W]; }
            if (m.Grounds == null) { m.Grounds = new string[H][]; for (int y = 0; y < H; y++) m.Grounds[y] = new string[W]; }
            string Mat(int x, int y)
            {
                if (m.Tiles[y][x] != "Solid") return null;
                string l = m.Looks[y][x] ?? theme.Mass;
                return l != null && Built.Contains(l) ? l : null;
            }
            var seen = new bool[H, W];
            int n = 0;
            for (int y0 = 3; y0 < H - 3; y0++)
                for (int x0 = 3; x0 < W - 3; x0++)
                {
                    if (seen[y0, x0] || Mat(x0, y0) == null) continue;
                    // flood the block, then keep it only if it fills its bounding box
                    var cells = new List<(int x, int y)>(); var q = new Queue<(int, int)>();
                    q.Enqueue((x0, y0)); seen[y0, x0] = true;
                    int minX = x0, maxX = x0, minY = y0, maxY = y0;
                    while (q.Count > 0)
                    {
                        var (x, y) = q.Dequeue(); cells.Add((x, y));
                        minX = System.Math.Min(minX, x); maxX = System.Math.Max(maxX, x); minY = System.Math.Min(minY, y); maxY = System.Math.Max(maxY, y);
                        foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= W || ny >= H || seen[ny, nx] || Mat(nx, ny) == null) continue;
                            seen[ny, nx] = true; q.Enqueue((nx, ny));
                        }
                    }
                    int bw = maxX - minX + 1, bh = maxY - minY + 1;
                    if (bw < MinSize || bh < MinSize || cells.Count != bw * bh) continue;
                    if (minX < 3 || minY < 3 || maxX > W - 4 || maxY > H - 4) continue;     // the zone's own border wall
                    if (Hollow(m, minX, minY, maxX, maxY, Mat(minX, minY), seed + n * 101)) n++;
                }
            return m;
        }

        static bool Hollow(MapLayout m, int x0, int y0, int x1, int y1, string mat, int seed)
        {
            int H = m.Tiles.Length, W = m.Tiles[0].Length;
            float R(int a, int b, int k) => ZoneThemes.Hash01(a, b, seed + k * 7919);
            bool Outside(int x, int y) => x >= 0 && y >= 0 && x < W && y < H && (m.Tiles[y][x] == "Empty" || m.Tiles[y][x] == "Difficult") && m.Grounds[y][x] != "Water";

            // a way in: the middle-ish of a side that opens onto walkable ground, south first (the facade)
            var doors = new List<(int x, int y)>();
            (int x, int y)? Entry(int side)
            {
                var c = new List<(int, int)>();
                if (side == 0) for (int x = x0 + 2; x <= x1 - 2; x++) { if (Outside(x, y1 + 1)) c.Add((x, y1)); }
                if (side == 1) for (int y = y0 + 2; y <= y1 - 2; y++) { if (Outside(x1 + 1, y)) c.Add((x1, y)); }
                if (side == 2) for (int y = y0 + 2; y <= y1 - 2; y++) { if (Outside(x0 - 1, y)) c.Add((x0, y)); }
                if (side == 3) for (int x = x0 + 2; x <= x1 - 2; x++) { if (Outside(x, y0 - 1)) c.Add((x, y0)); }
                if (c.Count == 0) return null;
                return c[(int)(R(side, 0, 1) * c.Count) % c.Count];
            }
            // Batch 122 (Yan: "every building faces south, you can make some of them be facing east or west as well"): the front
            // (the main door, the trodden path to it, and the way RoofView turns the roof) is south for half the buildings, east or
            // west for the rest; the other sides may get a back door
            float f = R(x0, y0, 50);
            int front = f < 0.5f ? 0 : f < 0.75f ? 1 : 2;
            foreach (int side in new[] { front, 0, 1, 2, 3 })
            {
                var e = Entry(side);
                if (e.HasValue && !doors.Contains(e.Value) && (doors.Count == 0 || side != front && R(side, 1, 2) < 0.35f)) doors.Add(e.Value);
            }
            if (doors.Count == 0) return false;                      // nowhere to walk in from: leave it a solid block
            var main = doors[0];

            string floor = mat == "concrete" || mat == "hull" || mat == "metal" ? "Concrete" : "Floor";
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    bool edge = x == x0 || x == x1 || y == y0 || y == y1;
                    m.Tiles[y][x] = edge ? "Solid" : "Empty";
                    m.Looks[y][x] = edge ? mat : null;
                    m.Grounds[y][x] = floor;
                }
            foreach (var (x, y) in doors) { m.Tiles[y][x] = "Door"; m.Looks[y][x] = null; }
            m.Looks[main.y][main.x] = "front";                       // RoofView turns the roof by it
            {   // a trodden path out of the front door, over grass only
                int sx = main.y == y1 ? 0 : main.y == y0 ? 0 : main.x == x1 ? 1 : -1, sy = main.y == y1 ? 1 : main.y == y0 ? -1 : 0;
                int len = 3 + (int)(R(main.x, main.y, 51) * 4);
                for (int k = 1; k <= len; k++)
                {
                    int px = main.x + sx * k, py = main.y + sy * k;
                    if (px < 0 || py < 0 || px >= W || py >= H || m.Tiles[py][px] != "Empty") break;
                    if (m.Grounds[py][px] == "Grass" || m.Grounds[py][px] == "DarkGrass") m.Grounds[py][px] = "Dirt";
                }
            }

            // rooms: split anything bigger than MaxRoom with an inner wall + door, never across a door
            var rooms = new List<(int x0, int y0, int x1, int y1)>();
            void Split(int ax, int ay, int bx, int by, int depth)          // interior rect, inclusive
            {
                int w = bx - ax + 1, h = by - ay + 1;
                bool vertical = w >= h;
                int len = vertical ? w : h;
                if (len <= MaxRoom || depth > 6) { rooms.Add((ax, ay, bx, by)); return; }
                var options = new List<int>();
                for (int k = MinRoom; k <= len - MinRoom - 1; k++)
                {
                    int at = (vertical ? ax : ay) + k;
                    bool blocked = false;
                    foreach (var (dx, dy) in doors) if (vertical ? dx == at : dy == at) { blocked = true; break; }   // a wall would sit in front of a door
                    if (!blocked) options.Add(at);
                }
                if (options.Count == 0) { rooms.Add((ax, ay, bx, by)); return; }
                int s = options[(int)(R(ax, ay, 3 + depth) * options.Count) % options.Count];
                int from = vertical ? ay : ax, to = vertical ? by : bx;
                int door = from + 1 + (int)(R(ax, by, 4 + depth) * System.Math.Max(1, to - from - 1));
                for (int t = from; t <= to; t++)
                {
                    int x = vertical ? s : t, y = vertical ? t : s;
                    m.Tiles[y][x] = t == door ? "Door" : "Solid"; m.Looks[y][x] = t == door ? null : mat;
                }
                doors.Add(vertical ? (s, door) : (door, s));
                if (vertical) { Split(ax, ay, s - 1, by, depth + 1); Split(s + 1, ay, bx, by, depth + 1); }
                else { Split(ax, ay, bx, s - 1, depth + 1); Split(ax, s + 1, bx, by, depth + 1); }
            }
            Split(x0 + 1, y0 + 1, x1 - 1, y1 - 1, 0);

            // Batch 118 (Yan: "ALL of them are either a square or a rectangle, we need more varied building shapes"): about two
            // thirds of the buildings with 3+ rooms lose one room on the outside - a corner one makes an L, one in the middle of a
            // side a U. The walls it shared stay as outer walls and their doors open outside now, so nothing gets cut off.
            if (rooms.Count >= 3 && R(x0, y1, 40) < 0.65f)
            {
                int Edges((int x0, int y0, int x1, int y1) q) => (q.x0 == x0 + 1 ? 1 : 0) + (q.x1 == x1 - 1 ? 1 : 0) + (q.y0 == y0 + 1 ? 1 : 0) + (q.y1 == y1 - 1 ? 1 : 0);
                int area = (x1 - x0 - 1) * (y1 - y0 - 1);
                bool corner = R(x1, y1, 41) < 0.6f;
                var cands = rooms.Where(q => (corner ? Edges(q) >= 2 : Edges(q) == 1) && (q.x1 - q.x0 + 1) * (q.y1 - q.y0 + 1) * 3 < area).ToList();
                if (cands.Count == 0) cands = rooms.Where(q => Edges(q) >= 1 && (q.x1 - q.x0 + 1) * (q.y1 - q.y0 + 1) * 3 < area).ToList();
                if (cands.Count > 0)
                {
                    var cut = cands[(int)(R(x0, y0, 42) * cands.Count) % cands.Count];
                    bool Removed(int x, int y) => x >= cut.x0 && x <= cut.x1 && y >= cut.y0 && y <= cut.y1;
                    bool Inside(int x, int y) => x > x0 && x < x1 && y > y0 && y < y1 && m.Tiles[y][x] == "Empty" && !Removed(x, y);
                    var gone = new List<(int x, int y)>();
                    for (int y = y0; y <= y1; y++)
                        for (int x = x0; x <= x1; x++)
                        {
                            bool keep = !Removed(x, y) && (m.Tiles[y][x] == "Empty" || Enumerable.Range(-1, 3).Any(dy => Enumerable.Range(-1, 3).Any(dx => Inside(x + dx, y + dy))));
                            if (!keep) gone.Add((x, y));
                        }
                    var ring = new List<string>();                                   // the ground round the building fills the gap
                    for (int x = x0 - 1; x <= x1 + 1; x++) { ring.Add(m.Grounds[y0 - 1][x]); ring.Add(m.Grounds[y1 + 1][x]); }
                    for (int y = y0; y <= y1; y++) { ring.Add(m.Grounds[y][x0 - 1]); ring.Add(m.Grounds[y][x1 + 1]); }
                    string outside = ring.Where(g => g != null && g != "Floor" && g != "Water").GroupBy(g => g).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault() ?? "Dirt";
                    foreach (var (x, y) in gone) { m.Tiles[y][x] = "Empty"; m.Looks[y][x] = null; m.Grounds[y][x] = outside; }
                    rooms.Remove(cut);
                    doors.RemoveAll(d => m.Tiles[d.y][d.x] != "Door");
                    // still a way in from outside? (a door with open ground on one side)
                    bool Open(int x, int y) => x < x0 || x > x1 || y < y0 || y > y1 || gone.Contains((x, y));
                    if (!doors.Any(d => Open(d.x + 1, d.y) || Open(d.x - 1, d.y) || Open(d.x, d.y + 1) || Open(d.x, d.y - 1)))
                        for (int y = y0; y <= y1 && true; y++)
                            for (int x = x0; x <= x1; x++)
                                if (m.Tiles[y][x] == "Solid" && ((Open(x - 1, y) && Inside(x + 1, y)) || (Open(x + 1, y) && Inside(x - 1, y)) || (Open(x, y - 1) && Inside(x, y + 1)) || (Open(x, y + 1) && Inside(x, y - 1))))
                                { m.Tiles[y][x] = "Door"; m.Looks[y][x] = null; doors.Add((x, y)); y = y1 + 1; break; }
                }
            }

            bool NearDoor(int x, int y)
            {
                foreach (var (dx, dy) in doors) if (System.Math.Abs(dx - x) <= 1 && System.Math.Abs(dy - y) <= 1) return true;
                return false;
            }
            // windows along the outer walls, every few tiles, away from corners and doors
            for (int x = x0 + 2; x <= x1 - 2; x++)
                foreach (int y in new[] { y0, y1 })
                    if ((x - x0) % 4 == 2 && m.Tiles[y][x] == "Solid" && !NearDoor(x, y)) m.Looks[y][x] = "window";
            for (int y = y0 + 2; y <= y1 - 2; y++)
                foreach (int x in new[] { x0, x1 })
                    if ((y - y0) % 4 == 2 && m.Tiles[y][x] == "Solid" && !NearDoor(x, y)) m.Looks[y][x] = "window";

            // furniture against the walls, a little rubble on the floor
            string[] furniture = mat == "concrete" || mat == "hull" || mat == "metal"
                ? new[] { "lockers", "crate", "machine", "rustbarrel", "desk" }
                : new[] { "lockers", "desk", "armchair", "crate", "crate" };
            foreach (var (ax, ay, bx, by) in rooms)
            {
                int want = 1 + (int)(R(ax, ay, 9) * 3), placed = 0;
                for (int tries = 0; tries < 24 && placed < want; tries++)
                {
                    int x = ax + (int)(R(tries, ax, 10) * (bx - ax + 1)), y = ay + (int)(R(tries, ay, 11) * (by - ay + 1));
                    bool againstWall = m.Tiles[y][x - 1] == "Solid" || m.Tiles[y][x + 1] == "Solid" || m.Tiles[y - 1][x] == "Solid";
                    if (!againstWall || NearDoor(x, y) || m.Tiles[y][x] != "Empty") continue;
                    string look = furniture[(int)(R(x, y, 12) * furniture.Length) % furniture.Length];
                    m.Tiles[y][x] = look == "lockers" || look == "machine" ? "HardCover" : "ScatterCover";
                    m.Looks[y][x] = look;
                    placed++;
                }
                if (R(ax, by, 13) < 0.5f)
                {
                    int x = (ax + bx) / 2, y = (ay + by) / 2;
                    if (m.Tiles[y][x] == "Empty" && !NearDoor(x, y)) { m.Tiles[y][x] = "Difficult"; m.Looks[y][x] = "rubble"; }
                }
            }

            // Batch 112: less tidy (Yan on the labs: "less neat ... dark, gritty, dirty"; the plain buildings were still neat boxes).
            // Only ever opens walls, so every room stays reachable. Half the buildings have a hole knocked through an inner wall,
            // a quarter a caved-in corner with the rubble spilled inside; desks lie tipped over; litter, glass and papers on the floors.
            var inner = new List<(int x, int y)>();
            for (int y = y0 + 1; y < y1; y++)
                for (int x = x0 + 1; x < x1; x++)
                    if (m.Tiles[y][x] == "Solid" && m.Looks[y][x] == mat && !NearDoor(x, y)) inner.Add((x, y));
            if (inner.Count > 0 && R(x0, y0, 21) < 0.5f)
            {
                var (hx, hy) = inner[(int)(R(x0, y1, 22) * inner.Count) % inner.Count];
                m.Tiles[hy][hx] = "Difficult"; m.Looks[hy][hx] = "rubble";
                foreach (var (nx, ny) in new[] { (hx + 1, hy), (hx - 1, hy), (hx, hy + 1), (hx, hy - 1) })   // sometimes a wider hole
                    if (inner.Contains((nx, ny)) && R(nx, ny, 23) < 0.35f) { m.Tiles[ny][nx] = "Difficult"; m.Looks[ny][nx] = "rubble"; break; }
            }
            if (R(x1, y0, 24) < 0.25f && x1 - x0 >= 8 && y1 - y0 >= 8)
            {
                int cx = R(x1, y1, 25) < 0.5f ? x0 : x1, cy = R(x0, y1, 26) < 0.5f ? y0 : y1, sx = cx == x0 ? 1 : -1, sy = cy == y0 ? 1 : -1;
                for (int k = 0; k <= 2; k++)
                    foreach (var (wx, wy) in new[] { (cx + sx * k, cy), (cx, cy + sy * k) })
                        if (m.Tiles[wy][wx] == "Solid" && !NearDoor(wx, wy)) { m.Tiles[wy][wx] = "Difficult"; m.Looks[wy][wx] = "rubble"; }
                for (int k = 0; k < 5; k++)                                                 // what came down lies inside
                {
                    int x = cx + sx * (1 + (int)(R(k, cx, 27) * 3)), y = cy + sy * (1 + (int)(R(k, cy, 28) * 3));
                    if (m.Tiles[y][x] == "Empty" && !NearDoor(x, y)) { m.Tiles[y][x] = R(x, y, 29) < 0.4f ? "HardCover" : "Difficult"; m.Looks[y][x] = m.Tiles[y][x] == "HardCover" ? "debris" : "rubble"; }
                }
            }
            string[] litter = { "papers", "glass", "grime", "papers", "litter" };
            for (int y = y0 + 1; y < y1; y++)
                for (int x = x0 + 1; x < x1; x++)
                {
                    if (m.Tiles[y][x] == "ScatterCover" && m.Looks[y][x] == "desk" && R(x, y, 30) < 0.35f)
                    { m.Looks[y][x] = "desk_" + "nsew"[(int)(R(x, y, 31) * 4) % 4]; continue; }   // knocked over long ago
                    if (m.Tiles[y][x] == "Empty" && m.Looks[y][x] == null && !NearDoor(x, y) && R(x, y, 32) < 0.18f)
                        m.Looks[y][x] = litter[(int)(R(x, y, 33) * litter.Length) % litter.Length];
                }
            return true;
        }
    }
}
