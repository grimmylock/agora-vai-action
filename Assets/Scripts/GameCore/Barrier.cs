// The Barrier (Yan, 2026-11-18: "we want a defendable point with high cover, the barrier itself needs an HP bar ... walls with hp the enemies will break and advance ... walls with
// catwalks you can walk on and take cover/return fire"). A fortified wall on the Vanguard Barracks' north side (TierMaps.Tier4: Solid tiles drawn as low concrete jersey barriers along an arc, a steel
// catwalk two tiles deep behind it, the Liberators' firing step). Rules, in code:
// - Each wall tile is "high cover" (TerrainMap.CoverAcAt = 3, Ballistics.HighCover): a round below a standing head's height stops in it; you can see through it and fire over it from the tile
//   right behind it (the shooter-next-to-cover rule, Ballistics.Trace) - so the catwalk's front row is the firing step. It cannot be walked through or vaulted (Solid).
// - It has hit points (WallHp each, TerrainMap.DamageTile): every round that hits it wears it down, and the horde (Horde: the siege's fanatics) walk up to it and tear at it (BreachWear a
//   second each). A tile at 0 becomes rubble: the way is open and they pour through.
// - `Fraction` = what is left of the whole wall (the HUD bar, HudView.Siege.cs).
// ponytail: the horde walks to the wall tile in front of it (not round the ends); once a gap exists they go through the nearest one, then their normal AI hunts you. Tile Height/Catwalk are
// flat (no high-ground accuracy: the engine's rounds are 2D lines with heights). The wall is part of the map (always there), its damage is saved with the tiles.
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class Barrier
    {
        // 2026-11-18 (Yan: "make it a more rounded barrier ... in the shape of a ("): an arc bowing out toward the enemy (north), 57 columns wide, ends on the compound's north fence line (y 120),
        // apex at (156, 106). Circle of radius R through the two ends and the apex. The catwalk is the two tiles inside it. (Rotated, a "(" is this: convex toward the enemy.)
        public const int X0 = 128, X1 = 184, CX = 156, ApexY = 106, EndY = 120, R = 35;
        public const int WallHp = 3000;               // in damage points, not hits: a rifle round (40) takes off ~16 of it (RoundWear), a frag half its damage, a fanatic at the foot 60 a second
        public const float BreachWear = 60f, RoundWear = 0.4f, BlastWear = 0.5f;
        public static string ZoneOverride;           // harness
        static string Map => MacroMapDatabase.MapOf("vanguard_barracks");
        public static bool On => (ZoneOverride ?? TerrainMap.ZoneId) == Map;
        public static readonly HashSet<Mutant> Horde = new HashSet<Mutant>();
        static readonly Dictionary<int, float> _wear = new Dictionary<int, float>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { Horde.Clear(); _wear.Clear(); }

        static int ArcY(int x) => Mathf.RoundToInt(ApexY + R - Mathf.Sqrt(R * R - (x - CX) * (float)(x - CX)));
        static HashSet<(int x, int y)> _cells; static Dictionary<int, (int top, int bot)> _cols;
        /// <summary>The wall's tiles: for each column from the arc's height to the next column's (so the line has no diagonal gaps).</summary>
        public static HashSet<(int x, int y)> Cells
        {
            get
            {
                if (_cells != null) return _cells;
                _cells = new HashSet<(int, int)>(); _cols = new Dictionary<int, (int, int)>();
                for (int x = X0; x <= X1; x++)
                {
                    int y0 = ArcY(x), y1 = x < X1 ? ArcY(x + 1) : y0, lo = System.Math.Min(y0, y1), hi = System.Math.Max(y0, y1);
                    for (int y = lo; y <= hi; y++) _cells.Add((x, y));
                    _cols[x] = (lo, hi);
                }
                return _cells;
            }
        }
        public static int TopY(int x) { var _ = Cells; return _cols[Mathf.Clamp(x, X0, X1)].top; }
        public static int BottomY(int x) { var _ = Cells; return _cols[Mathf.Clamp(x, X0, X1)].bot; }
        /// <summary>The catwalk's front row (right behind the wall: the firing step) at a column.</summary>
        public static int StepY(int x) => BottomY(x) + 1;

        /// <summary>The columns of the two watchtowers (their middle columns): flat stretches of the arc near x 145 and 166, clear of the steps (TierMaps).</summary>
        public static int[] TowerCols() { var r = new List<int>(); foreach (int target in new[] { 145, 166 }) for (int d = 0; d <= 4; d++) foreach (int tx in new[] { target + d, target - d }) if (r.Count < (target == 145 ? 1 : 2) && BottomY(tx - 1) == BottomY(tx) && BottomY(tx) == BottomY(tx + 1) && !StairCols.Contains(tx - 1) && !StairCols.Contains(tx) && !StairCols.Contains(tx + 1)) r.Add(tx); return r.ToArray(); }
        public static readonly HashSet<int> StairCols = new HashSet<int> { 140, 141, 150, 151, 161, 162, 171, 172 };

        /// <summary>Is (x, y) a standing piece of the wall? (A broken tile is rubble and not.)</summary>
        /// <summary>The Barrier's wall or a watchtower's: the tough structures (rounds and blasts are scaled down against them).</summary>
        public static bool Sturdy(int x, int y) => IsWall(x, y) || Emplacement.TowerAt(x, y) != null;

        public static bool IsWall(int x, int y) => On && x >= X0 && x <= X1 && Cells.Contains((x, y)) && TerrainMap.GetTile(x, y).Type == TileType.Solid;

        public static int HpAt(int x, int y) => IsWall(x, y) ? System.Math.Max(0, WallHp - TerrainMap.GetTile(x, y).Damage) : 0;

        /// <summary>What is left of the whole wall, 0..1.</summary>
        public static float Fraction { get { if (!On) return 0f; int hp = 0; foreach (var (x, y) in Cells) hp += HpAt(x, y); return hp / (float)(Cells.Count * WallHp); } }
        public static int Gaps { get { int g = 0; if (On) foreach (var (x, y) in Cells) if (!IsWall(x, y)) g++; return g; } }

        /// <summary>Is this unit on the enemy's side of the wall (north of it at its column, or out past an end)?</summary>
        public static bool Outside(Mutant m) => m.X < X0 || m.X > X1 ? m.Y < EndY : m.Y < TopY(m.X);

        /// <summary>A horde unit the wall's logic drives (outside it, marked): its own AI does not set its goal.</summary>
        public static bool Controls(Mutant m) => On && Horde.Contains(m) && Outside(m);

        /// <summary>Every frame (Tier4Story.TickRt): the horde walks to the wall and tears at it; through a gap they go on.</summary>
        public static void Tick(float dt)
        {
            if (!On || Horde.Count == 0) return;
            Horde.RemoveWhere(m => !m.IsAlive());
            var gaps = Cells.Where(c => !IsWall(c.x, c.y)).ToList();
            var walls = gaps.Count == Cells.Count ? new List<(int x, int y)>() : Cells.Where(c => IsWall(c.x, c.y)).ToList();
            foreach (var m in Horde)
            {
                if (m.IsAggroed && !Outside(m))                                    // inside: its own AI hunts you - and whoever stands at a watchtower's wall tears at it too
                {
                    for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                        if (Emplacement.TowerAt(m.X + dx, m.Y + dy) != null && TerrainMap.GetTile(m.X + dx, m.Y + dy).Type == TileType.Solid)
                        {
                            int tk = (m.X + dx) * 1000 + m.Y + dy; float ta = (_wear.TryGetValue(tk, out var tc0) ? tc0 : 0f) + BreachWear * dt; int tw = Mathf.FloorToInt(ta);
                            if (tw > 0) { ta -= tw; TerrainMap.DamageTile(m.X + dx, m.Y + dy, tw, m); }
                            _wear[tk] = ta; dx = dy = 2;
                        }
                    continue;
                }
                if (!m.IsAggroed) continue;
                if (gaps.Count > 0)                                                  // a gap: the nearest one
                {
                    var g = gaps.OrderBy(c => (c.x - m.X) * (c.x - m.X) + (c.y - m.Y) * (c.y - m.Y)).First();
                    RtAi.GoTo(m, g); continue;
                }
                if (walls.Count == 0) continue;
                var w = walls.OrderBy(c => (c.x - m.X) * (c.x - m.X) + (c.y - m.Y) * (c.y - m.Y)).First();
                if (System.Math.Max(System.Math.Abs(m.X - w.x), System.Math.Abs(m.Y - w.y)) <= 1)      // at the foot of it: tear at the wall
                {
                    int key = w.x * 1000 + w.y;
                    float acc = (_wear.TryGetValue(key, out var cur) ? cur : 0f) + BreachWear * dt;
                    int whole = Mathf.FloorToInt(acc); if (whole > 0) { acc -= whole; TerrainMap.DamageTile(w.x, w.y, whole, m); }
                    _wear[key] = acc; continue;
                }
                // walk to the free tile beside that piece of wall nearest to us
                (int x, int y) best = (m.X, m.Y); int bd = int.MaxValue;
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                {
                    int tx = w.x + dx, ty = w.y + dy; if ((dx == 0 && dy == 0) || Cells.Contains((tx, ty)) || !TerrainMap.CanEnterTile(tx, ty)) continue;
                    int d = (tx - m.X) * (tx - m.X) + (ty - m.Y) * (ty - m.Y); if (d < bd) { bd = d; best = (tx, ty); }
                }
                RtAi.GoTo(m, best);
            }
        }
    }
}
