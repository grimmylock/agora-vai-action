// Real-time fork, batch 1 (ACTION_DESIGN.md "Hit model"): a shot is a line over the ground, not a dice roll.
//  - Every body is a small footprint on the ground; a round hits it when its path passes close enough, from any
//    direction, so standing higher or lower on the screen gives nobody free headshots (Yan's worry, 2026-10-16).
//  - The HEIGHT of the round comes from the crosshair: over the head pixels = head height, chest = torso, legs = legs.
//    Spread scatters it sideways (a miss, or the arm on that side) and up/down (a lower or higher part, or over the head).
//  - Walls stop it. Low cover stops it below its own height (a head over a low wall can still be hit); wood, tin, brush
//    and cloth let it through at half damage (Materials, Batch 105). Cover right in front of the shooter is fired over.
// Positions are in tile space: x right, y DOWN (the same axes as X/Y), a unit at tile (X, Y) stands at X + 0.5 + ox.
// Hitscan: at these ranges (screen ~20 tiles, bullets 100+ tiles/s) travel time is a few frames.
// ponytail: hitscan, real travel time (leading targets at range) when long guns need it.
using System.Collections.Generic;

namespace GameCore
{
    public static class Ballistics
    {
        // Body bands as a share of a standing body's height, measured off the 16x16 human sprite (UnitMotionView.BodyPx):
        // feet at row 15, head rows 2-6, torso 7-11, legs 12-14.
        public const float HeadFrom = 0.61f, TorsoFrom = 0.23f;
        public const float CrouchScale = 0.7f;              // crouching: every part lower
        public const float BodyTiles = 0.82f;               // a standing human, in tiles (feet to crown)
        public const float LowCover = 0.55f;                // a low wall / sandbags / a tipped desk: waist high
        public const float LightCover = 0.42f;              // brush, a fence, debris
        public const float HeadHalf = 0.13f, TorsoHalf = 0.21f, ArmFrom = 0.12f, LegsHalf = 0.17f;   // half-widths, tiles

        public class Hit
        {
            public ICombatant Unit;                         // null: hit the world (or nothing)
            public string Part;                             // "Head" | "Torso" | "Arm" | "Legs" | "Body" (mutants)
            public float EndX, EndY;                        // where the round stopped (tile space)
            public float Through = 1f;                      // 0.5 per pierced cover on the way
            public bool Wall, Cover;                        // stopped by a wall / by low cover
            public int CoverX = -1, CoverY = -1;            // the cover it hit or went through
            public float Dist;
            public float Side;                              // + = passed on the round's right (which arm or leg)
        }

        /// <summary>Where a unit stands, tile space.</summary>
        public static (float x, float y) Pos(ICombatant u)
        {
            var (ox, oy) = Rt.Offset(u);
            return (u.X + 0.5f + ox, u.Y + 0.5f + oy);
        }

        /// <summary>Half-width of a unit's footprint for a round at height h (share of a standing human). Mutants: one band.</summary>
        static float HalfWidth(ICombatant u, float h, out string part)
        {
            if (!(u is Player) && !(u is HumanEnemy) && u is Mutant m)
            {
                float size = MutantSize(m);
                part = h >= 0.7f * size / BodyTiles ? "Head" : "Body";
                return h <= size / BodyTiles ? 0.25f * size : -1f;
            }
            float s = Body.Down(u) ? 0.3f : Rt.Low(u) ? 0.4f : Crouched(u) ? CrouchScale : 1f;   // RT batch 3: diving or sprawled, low   // RT batch 2: someone down lies flat
            if (h > s) { part = null; return -1f; }        // over the head
            if (h >= HeadFrom * s) { part = "Head"; return HeadHalf; }
            if (h >= TorsoFrom * s) { part = "Torso"; return TorsoHalf; }
            part = "Legs"; return LegsHalf;
        }

        /// <summary>Mutant height in tiles from its sprite size (hounds 1, Dread Prowler 1.5, Brute 2).</summary>
        public static float MutantSize(Mutant m) => m?.Db == null ? 1f : m.Db.Name.Contains("Brute") ? 2f : m.Db.Name.Contains("Dread") ? 1.5f : 1f;

        public static bool Crouched(ICombatant u) => u is Player p ? p.Sneaking : u.StatusEffects != null && (u.StatusEffects.Hunkered || u.StatusEffects.Prone);

        /// <summary>Trace one round from (x0, y0) along the unit direction (dx, dy) for up to `range` tiles at height h.
        /// The first body, wall or blocking cover on the way ends it.</summary>
        public static Hit Trace(ICombatant shooter, float x0, float y0, float dx, float dy, float range, float h, IEnumerable<ICombatant> bodies)
        {
            var hit = new Hit();
            // the nearest body the line passes close enough to, at this height
            ICombatant best = null; float bestT = float.MaxValue, bestSide = 0f; string bestPart = null;
            foreach (var u in bodies)
            {
                if (u == null || u == shooter || !u.IsAlive()) continue;
                var (ux, uy) = Pos(u);
                float rx = ux - x0, ry = uy - y0;
                float t = rx * dx + ry * dy;                 // along the line
                if (t <= 0.05f || t > range || t >= bestT) continue;
                float side = rx * dy - ry * dx;              // signed distance off the line (+ = the round passes on its right)
                float half = HalfWidth(u, h, out string part);
                if (half <= 0f || System.Math.Abs(side) > half) continue;
                if (part == "Torso" && System.Math.Abs(side) > ArmFrom) part = "Arm";
                best = u; bestT = t; bestPart = part; bestSide = side;
            }
            float stop = best != null ? bestT : range;
            // walk the tiles the line crosses up to the body (or the end of its reach)
            int lastX = (int)System.Math.Floor(x0), lastY = (int)System.Math.Floor(y0);
            const float step = 0.1f;
            for (float t = step; t < stop; t += step)
            {
                float px = x0 + dx * t, py = y0 + dy * t;
                int tx = (int)System.Math.Floor(px), ty = (int)System.Math.Floor(py);
                if (tx == lastX && ty == lastY) continue;
                lastX = tx; lastY = ty;
                if (tx < 0 || ty < 0 || tx >= TerrainMap.Width || ty >= TerrainMap.Height) { stop = t; hit.Wall = true; break; }
                if (TerrainMap.BlocksSight(tx, ty))
                {
                    if (TerrainMap.CoverAcAt(tx, ty) > 0 && h > LowCover) continue;   // a window frame: over the sill
                    stop = t; hit.Wall = true; hit.CoverX = tx; hit.CoverY = ty; break;
                }
                int ac = TerrainMap.CoverAcAt(tx, ty);
                if (ac <= 0 || shooter != null && System.Math.Abs(tx - shooter.X) <= 1 && System.Math.Abs(ty - shooter.Y) <= 1) continue;   // open ground, or cover right next to the shooter: fired over
                float top = ac >= 2 ? LowCover : LightCover;
                if (h > top) continue;                       // over it
                hit.CoverX = tx; hit.CoverY = ty;
                if (Materials.Pierced(Materials.At(tx, ty)) && hit.Through > 0.3f) { hit.Through *= Materials.ThroughDamage; continue; }
                stop = t; hit.Cover = true; break;
            }
            hit.Dist = stop;
            hit.EndX = x0 + dx * stop; hit.EndY = y0 + dy * stop;
            if (best != null && stop >= bestT) { hit.Unit = best; hit.Part = bestPart; hit.Side = bestSide; }
            return hit;
        }

        /// <summary>Damage share per part (ponytail: a multiplier on one HP pool until limb health lands, RT batch 2).</summary>
        public static float PartMult(string part) => part switch { "Head" => 1.3f, "Arm" => 0.6f, "Legs" => 0.7f, _ => 1f };

        /// <summary>Height (share of a standing human) the crosshair puts the round at, given the world point under the
        /// mouse and the unit it is over. tileY = the mouse in tile space (y down).</summary>
        public static float AimHeight(ICombatant over, float mouseTileY)
        {
            if (over == null) return 0.45f;                  // nothing under the crosshair: chest height
            var (_, uy) = Pos(over);
            float feet = uy + 0.44f;                         // the sprite's feet sit 7 px under its middle
            return System.Math.Max(0.02f, System.Math.Min(1.1f, (feet - mouseTileY) / BodyTiles));
        }
    }
}
