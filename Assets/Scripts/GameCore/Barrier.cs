// The Barrier (Yan, 2026-11-18: "we want a defendable point with high cover, the barrier itself needs an HP bar ... walls with hp the enemies will break and advance ... walls with
// catwalks you can walk on and take cover/return fire"). A fortified wall on the Vanguard Barracks' north side (TierMaps.Tier4: Solid concrete along y = Y from X0 to X1, a steel
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
        public const int Y = 117, X0 = 128, X1 = 184, WallHp = 120;
        public const float BreachWear = 3f;          // wall HP a fanatic at the foot of it takes off each second
        public static string ZoneOverride;           // harness
        static string Map => MacroMapDatabase.MapOf("vanguard_barracks");
        public static bool On => (ZoneOverride ?? TerrainMap.ZoneId) == Map;
        public static readonly HashSet<Mutant> Horde = new HashSet<Mutant>();
        static readonly Dictionary<int, float> _wear = new Dictionary<int, float>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { Horde.Clear(); _wear.Clear(); }

        /// <summary>Is (x, y) a standing piece of the wall? (A broken tile is rubble and not.)</summary>
        public static bool IsWall(int x, int y) => y == Y && x >= X0 && x <= X1 && On && TerrainMap.GetTile(x, y).Type == TileType.Solid;

        public static int HpAt(int x, int y) => IsWall(x, y) ? System.Math.Max(0, WallHp - TerrainMap.GetTile(x, y).Damage) : 0;

        /// <summary>What is left of the whole wall, 0..1.</summary>
        public static float Fraction { get { if (!On) return 0f; int hp = 0; for (int x = X0; x <= X1; x++) hp += HpAt(x, Y); return hp / (float)((X1 - X0 + 1) * WallHp); } }
        public static int Gaps { get { int g = 0; if (On) for (int x = X0; x <= X1; x++) if (!IsWall(x, Y)) g++; return g; } }

        /// <summary>Every frame (Tier4Story.TickRt): the horde walks to the wall and tears at it; through a gap they go on.</summary>
        public static void Tick(float dt)
        {
            if (!On || Horde.Count == 0) return;
            Horde.RemoveWhere(m => !m.IsAlive());
            int gapX = -1;
            for (int x = X0; x <= X1; x++) if (!IsWall(x, Y)) { gapX = x; break; }
            foreach (var m in Horde)
            {
                if (!(m is HumanEnemy h) || !m.IsAggroed) continue;
                if (m.Y >= Y) continue;                                             // inside: its own AI hunts you
                int wx = Mathf.Clamp(m.X, X0, X1);
                if (gapX >= 0)                                                      // a gap: the nearest one
                {
                    int best = gapX; for (int x = X0; x <= X1; x++) if (!IsWall(x, Y) && Mathf.Abs(x - m.X) < Mathf.Abs(best - m.X)) best = x;
                    RtAi.GoTo(m, (best, Y)); continue;
                }
                RtAi.GoTo(m, (wx, Y - 1));
                if (m.Y >= Y - 1 && Mathf.Abs(m.X - wx) <= 1)                        // at the foot of it: tear at the wall
                {
                    float w = (_wear.TryGetValue(wx, out var cur) ? cur : 0f) + BreachWear * dt;
                    int whole = Mathf.FloorToInt(w); if (whole > 0) { w -= whole; TerrainMap.DamageTile(wx, Y, whole, m); }
                    _wear[wx] = w;
                }
            }
        }
    }
}
