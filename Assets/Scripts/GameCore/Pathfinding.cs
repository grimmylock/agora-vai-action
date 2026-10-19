// Ported from script.js: getStraightLinePath(start, end) (Bresenham) and findPath(start, end, unit) (A*).
using System.Collections.Generic;
using System.Linq;

namespace GameCore
{
    public static class Pathfinding
    {
        /// <summary>Ported from getStraightLinePath(start, end) — Bresenham's line algorithm.</summary>
        public static List<(int x, int y)> GetStraightLinePath(int startX, int startY, int endX, int endY)
        {
            var path = new List<(int x, int y)>();
            int x0 = startX, y0 = startY;
            int x1 = endX, y1 = endY;
            int dx = System.Math.Abs(x1 - x0);
            int dy = System.Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                path.Add((x0, y0));
                if (x0 == x1 && y0 == y1) break;
                int e2 = err * 2;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
            return path;
        }

        /// <summary>
        /// Ported from findPath(start, end, unit) — A* over the 4-directional grid, using
        /// TerrainMap.CanEnterTile for walkability and getMoveCost (via the unit) for edge cost.
        /// Returns an empty list if no path exists, matching the JS.
        /// The map overhaul: the tier maps are 200x200, so the search is a binary heap over pooled arrays (it was a list
        /// scanned twice per node - seconds for an unreachable target on a big map), and an NPC's search stops after
        /// NpcNodeBudget nodes and walks toward the closest point it found instead of standing still.
        /// </summary>
        public const int NpcNodeBudget = 6000;
        static float[] _g; static int[] _came, _stamp, _closed; static int _gen, _size;
        static int[] _heapIdx = new int[1024]; static float[] _heapF = new float[1024]; static int _heapN;

        /// <summary>Batch 126: the unit that leads its squad on the map (the first living member there, as TurnLoop has it).</summary>
        public static bool SquadLeader(Mutant m) =>
            MacroState.Current.OfflineSquads.FirstOrDefault(q => q.Members.Contains(m))?.Members.FirstOrDefault(x => x.IsAlive() && EncounterState.Enemies.Contains(x)) == m;

        public static List<(int x, int y)> FindPath(int startX, int startY, int endX, int endY, ICombatant unit)
        {
            bool explore = unit is Player && !EncounterState.InCombat;
            int W = TerrainMap.Width, H = TerrainMap.Height;
            var empty = new List<(int x, int y)>();
            if (startX < 0 || startY < 0 || startX >= W || startY >= H) return empty;
            if (_size != W * H) { _size = W * H; _g = new float[_size]; _came = new int[_size]; _stamp = new int[_size]; _closed = new int[_size]; _gen = 0; }
            if (++_gen == int.MaxValue) { System.Array.Clear(_stamp, 0, _size); System.Array.Clear(_closed, 0, _size); _gen = 1; }
            int gen = _gen;

            // who stands where (route around other units; the destination itself may be someone's tile), and the
            // anomalies the player knows about
            var units = new Dictionary<int, ICombatant>();
            var pl = Player.Current;
            if (pl != null && pl != unit && pl.IsAlive()) units[pl.Y * W + pl.X] = pl;
            foreach (var e in EncounterState.Enemies)
                if (e != unit && e.IsAlive() && e.X >= 0 && e.Y >= 0 && e.X < W && e.Y < H) units[e.Y * W + e.X] = e;
            // Batch 126: a squad doesn't block itself - its members step past each other (Mutant.MoveToward swaps them). A squad that
            // came in through a narrow gate used to stand in its own leader's way for days.
            var mates = unit is Mutant um && !EncounterState.InCombat && SquadLeader(um) ? MacroState.Current.OfflineSquads.FirstOrDefault(q => q.Members.Contains(um))?.Members : null;   // only the leader: two members trading places would never get anywhere
            HashSet<int> known = null;
            if (unit is Player) { known = new HashSet<int>(); foreach (var a in EncounterState.Anomalies) if (a.Revealed) known.Add(a.Y * W + a.X); }
            int budget = unit is Player ? int.MaxValue : NpcNodeBudget;
            // Batch 125: stepping on an exit travels, so the player's routes only end on one, never cross one (a walk past the
            // farmhouse stair took you upstairs)
            HashSet<int> exits = null;
            if (unit is Player) { exits = new HashSet<int>(); foreach (var x in TerrainMap.Exits) exits.Add(x.Y * W + x.X); }

            int s0 = startY * W + startX, goal = endY * W + endX;
            // one step's cost, or infinity if it can't be taken
            float Edge(int cx, int cy, int nx, int ny)
            {
                if (!TerrainMap.CanEnterTile(nx, ny)) return float.PositiveInfinity;
                int n = ny * W + nx;
                if (known != null && n != goal && known.Contains(n)) return float.PositiveInfinity;   // Batch 49: walk around anomalies you know about
                if (exits != null && n != goal && exits.Contains(n)) return float.PositiveInfinity;
                // Batch 67: the player only routes around people they can see (it used to dodge hidden ones too, a strange detour that gave them away)
                if (n != goal && units.TryGetValue(n, out var blocker) && !(unit is Player && !Vision.Shows(blocker)) && !(mates != null && mates.Contains(blocker))) return float.PositiveInfinity;
                // Batch 54: the climb is from the tile you'd step from; a step that costs more than a whole turn's MP (a ledge too high to climb) is a wall.
                bool diag = cx != nx && cy != ny;
                float c = unit != null ? GameServices.RequireGrid().StepCost(unit, cx, cy, nx, ny) : (diag ? TacticalGridSystem.Diagonal : 1f);
                // Batch 125: exploring tops MP up every step (PlayerController.Step), so an overloaded walker (max MP 2) still gets over a
                // ramp or a gulley crossing - only a ledge too high for anyone (more than 4) is a wall then (it had no route off the valley floor)
                float wall = explore ? System.Math.Max(unit?.MaxMp ?? 0, 4) : unit?.MaxMp ?? 0;
                if (float.IsInfinity(c) || unit != null && wall > 0 && c > wall) return float.PositiveInfinity;
                return explore ? (diag ? TacticalGridSystem.Diagonal : 1f) : c;   // Batch 67: out of a fight a step is a step (a minute), so take the shortest way, not the one that dodges bushes
            }
            _heapN = 0;
            _stamp[s0] = gen; _g[s0] = 0; _came[s0] = -1;
            Push(s0, Cheb(startX, startY, endX, endY));
            int best = s0; float bestH = Cheb(startX, startY, endX, endY); int expanded = 0;
            while (_heapN > 0)
            {
                int cur = Pop();
                if (_closed[cur] == gen) continue;
                _closed[cur] = gen;
                int cx = cur % W, cy = cur / W;
                if (cur == goal) return Smooth(Build(cur, W), W, Edge);
                float h0 = Cheb(cx, cy, endX, endY);
                if (h0 < bestH) { bestH = h0; best = cur; }
                if (++expanded > budget) break;
                for (int k = 0; k < 8; k++)   // Batch 102: 8 ways, diagonals at 1.5
                {
                    int nx = cx + DX[k], ny = cy + DY[k];
                    int n = ny * W + nx;
                    if (!TerrainMap.CanEnterTile(nx, ny) || _closed[n] == gen) continue;
                    float moveCost = Edge(cx, cy, nx, ny);
                    if (float.IsInfinity(moveCost)) continue;
                    float tg = _g[cur] + moveCost;
                    if (_stamp[n] == gen && tg >= _g[n]) continue;
                    _stamp[n] = gen; _g[n] = tg; _came[n] = cur;
                    Push(n, tg + Cheb(nx, ny, endX, endY));
                }
            }
            // no way there (or an NPC's search ran out): an NPC heads for the closest place it found
            return !(unit is Player) && best != s0 && expanded > budget ? Build(best, W) : empty;
        }

        /// <summary>Batch 102 (Yan: free movement): pull the A* route taut. Between two of its points, a straight line (Bresenham,
        /// diagonal steps included) replaces the route when every step on it can be taken and it costs no more - on open ground
        /// it never does - so a click walks straight there instead of in an L. Looks at most LookAhead points ahead.</summary>
        const int LookAhead = 24;
        static List<(int x, int y)> Smooth(List<(int x, int y)> path, int W, System.Func<int, int, int, int, float> edge)
        {
            if (path.Count < 3) return path;
            var outp = new List<(int x, int y)> { path[0] };
            int a = 0;
            while (a < path.Count - 1)
            {
                int far = a + 1; List<(int x, int y)> farLine = null;
                for (int j = a + 2; j < path.Count && j <= a + LookAhead; j++)
                {
                    var line = GetStraightLinePath(path[a].x, path[a].y, path[j].x, path[j].y);
                    float cost = 0f;
                    for (int i = 1; i < line.Count && !float.IsInfinity(cost); i++) cost += edge(line[i - 1].x, line[i - 1].y, line[i].x, line[i].y);
                    if (cost > _g[path[j].y * W + path[j].x] - _g[path[a].y * W + path[a].x] + 0.01f) continue;
                    far = j; farLine = line;
                }
                if (farLine == null) outp.Add(path[far]);
                else for (int i = 1; i < farLine.Count; i++) outp.Add(farLine[i]);
                a = far;
            }
            return outp;
        }

        static List<(int x, int y)> Build(int idx, int W)
        {
            var path = new List<(int x, int y)>();
            for (int c = idx; c >= 0; c = _came[c]) path.Add((c % W, c / W));
            path.Reverse();
            return path;
        }

        static readonly int[] DX = { 1, -1, 0, 0, 1, 1, -1, -1 }, DY = { 0, 0, 1, -1, 1, -1, 1, -1 };
        // octile distance: the cheapest a route could be with 1-per-straight and 1.5-per-diagonal steps
        static float Cheb(int ax, int ay, int bx, int by)
        {
            int dx = System.Math.Abs(ax - bx), dy = System.Math.Abs(ay - by);
            return System.Math.Max(dx, dy) + (TacticalGridSystem.Diagonal - 1f) * System.Math.Min(dx, dy);
        }

        static void Push(int idx, float f)
        {
            if (_heapN == _heapIdx.Length) { System.Array.Resize(ref _heapIdx, _heapN * 2); System.Array.Resize(ref _heapF, _heapN * 2); }
            int i = _heapN++;
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (_heapF[p] <= f) break;
                _heapIdx[i] = _heapIdx[p]; _heapF[i] = _heapF[p]; i = p;
            }
            _heapIdx[i] = idx; _heapF[i] = f;
        }

        static int Pop()
        {
            int top = _heapIdx[0];
            int lastI = _heapIdx[--_heapN]; float lastF = _heapF[_heapN];
            int i = 0;
            while (true)
            {
                int l = 2 * i + 1;
                if (l >= _heapN) break;
                int c = l + 1 < _heapN && _heapF[l + 1] < _heapF[l] ? l + 1 : l;
                if (_heapF[c] >= lastF) break;
                _heapIdx[i] = _heapIdx[c]; _heapF[i] = _heapF[c]; i = c;
            }
            if (_heapN > 0) { _heapIdx[i] = lastI; _heapF[i] = lastF; }
            return top;
        }

        private static float Chebyshev((int x, int y) a, (int x, int y) b) =>
            System.Math.Max(System.Math.Abs(a.x - b.x), System.Math.Abs(a.y - b.y));
    }
}
