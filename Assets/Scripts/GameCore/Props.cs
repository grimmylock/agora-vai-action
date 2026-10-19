// Batch 103 (Yan: "ENTER THE GUNGEON has some physics on the objects, i'd like stuff like that, rolling barrels, overturning
// tables"; "barrels do not need to explode when rolled, only when shot at"; "tables are not cover unless flipped"):
// props you can shove. They stay tiles (the rules' map); shoving moves or turns the tile, and the view slides the sprite.
//  - A standing desk is no cover, just something to climb over (2 MP). Shoved, it flips onto its side away from you:
//    light cover, but only against shots from the side it faces ("desk_n/s/e/w" = the side the top now faces).
//  - A barrel (rusty or explosive) rolls away from you until something stops it; a unit in its way is knocked down.
//  - A crate slides one tile.
//  - Blasts shove the props just outside their radius (the ones inside are blown apart, as before).
//  - Enemies flip desks for cover too (EnemyAI.HumanTurn).
using System.Collections.Generic;

namespace GameCore
{
    public static class Props
    {
        public const int RollMax = 6;          // tiles a kicked barrel rolls at most
        public const int ShoveAp = 1;

        /// <summary>Slides for the view to animate: the tile a prop ended on and the tile it came from.</summary>
        public static readonly List<(int x, int y, int fromX, int fromY)> Slides = new List<(int, int, int, int)>();

        static bool Desk(TerrainTile t) => t.Look == "desk";
        static bool Barrel(TerrainTile t) => t.Type == TileType.Barrel || t.Look == "rustbarrel";
        static bool Crate(TerrainTile t) => t.Look == "crate";
        static bool Flipped(TerrainTile t) => t.Look != null && t.Look.Length == 6 && t.Look.StartsWith("desk_");   // Batch 107: a tipped desk slides like a crate

        public static bool Movable(int x, int y)
        {
            if (x < 0 || y < 0 || x >= TerrainMap.Width || y >= TerrainMap.Height) return false;
            var t = TerrainMap.GetTile(x, y);
            return Desk(t) || Barrel(t) || Crate(t) || Flipped(t);
        }

        /// <summary>A fresh map: standing desks are something to climb over, not cover.</summary>
        public static void Settle()
        {
            for (int y = 0; y < TerrainMap.Height; y++)
                for (int x = 0; x < TerrainMap.Width; x++)
                {
                    var t = TerrainMap.GetTile(x, y);
                    if (Desk(t)) t.Type = TileType.Difficult;
                }
        }

        /// <summary>Does the cover on (x,y) count against a shot from (ax,ay)? A flipped desk only shields from the side it faces.</summary>
        public static bool Shields(int x, int y, int ax, int ay)
        {
            var l = TerrainMap.GetTile(x, y).Look;
            if (l == null || l.Length != 6 || !l.StartsWith("desk_")) return true;
            var (dx, dy) = Facing(l[5]);
            return (ax - x) * dx + (ay - y) * dy > 0;
        }

        static (int dx, int dy) Facing(char c) => c == 'n' ? (0, -1) : c == 's' ? (0, 1) : c == 'e' ? (1, 0) : (-1, 0);
        static char FacingOf(int dx, int dy) => dy < 0 ? 'n' : dy > 0 ? 's' : dx > 0 ? 'e' : 'w';   // a diagonal shove tips it north or south

        static bool Free(int x, int y) =>
            x >= 0 && y >= 0 && x < TerrainMap.Width && y < TerrainMap.Height
            && TerrainMap.GetTile(x, y).Type == TileType.Empty && (TerrainMap.GetTile(x, y).Look == null || WorldArt.IsDecal(TerrainMap.GetTile(x, y).Look))
            && TerrainMap.UnitAt(x, y, null) == null;

        /// <summary>Why `who` can't shove (x,y) now, or null.</summary>
        public static string Blocked(ICombatant who, int x, int y)
        {
            if (!Movable(x, y)) return "Nothing to shove there.";
            if (CombatantUtil.Distance(who, x, y) != 1) return "Get next to it first.";
            if (EncounterState.InCombat && who.Ap < ShoveAp) return $"Needs {ShoveAp} AP.";
            return null;
        }

        /// <summary>Shove the prop on (x,y) away from `who`: flip a desk, roll a barrel, slide a crate.</summary>
        public static bool Shove(ICombatant who, int x, int y)
        {
            var why = Blocked(who, x, y);
            if (why != null) { if (who is Player) GameLog.Write(why); return false; }
            int dx = System.Math.Sign(x - who.X), dy = System.Math.Sign(y - who.Y);
            string name = who is Player ? "You" : who.Name;
            var t = TerrainMap.GetTile(x, y);
            bool moved;
            if (Desk(t))
            {
                TerrainMap.SetTileType(x, y, TileType.ScatterCover, "desk_" + FacingOf(dx, dy));
                GameLog.Write($"{name} {(who is Player ? "flip" : "flips")} the desk onto its side.");
                Sfx.Play("door_break", TacticalGridView.GridToWorld(x, y), 0.6f);
                moved = true;
            }
            else moved = Push(x, y, dx, dy, Barrel(t) ? RollMax : 1, who);
            if (!moved) { if (who is Player) GameLog.Write("It won't budge: something's in the way."); return false; }
            if (EncounterState.InCombat) who.Ap -= ShoveAp;
            Noise.Make(x, y, Noise.Blast / 3, who);
            return true;
        }

        /// <summary>Moves the prop on (x,y) up to `max` tiles along (dx,dy) while the way is free (no squeezing past corners).
        /// A rolling barrel that runs into someone knocks them down. True if it moved at all.</summary>
        public static bool Push(int x, int y, int dx, int dy, int max, ICombatant by)
        {
            var (cx, cy, hit) = Roll(x, y, dx, dy, max);
            if (hit != null && max > 1 && hit.IsAlive()) Bowl(hit, x == cx && y == cy ? 1 : 2);
            if (cx == x && cy == y) return false;
            var from = TerrainMap.GetTile(x, y);
            var (type, look, dmg) = (from.Type, from.Look, from.Damage);
            TerrainMap.SetTileType(x, y, TileType.Empty);
            TerrainMap.SetTileType(cx, cy, type, look);
            TerrainMap.GetTile(cx, cy).Damage = dmg;
            Slides.Add((cx, cy, x, y));
            Sfx.Play("hit_hard_0", TacticalGridView.GridToWorld(cx, cy), 0.5f);
            return true;
        }

        /// <summary>Where a prop pushed from (x,y) along (dx,dy) would stop, and who it would run into. Changes nothing.</summary>
        static (int x, int y, ICombatant hit) Roll(int x, int y, int dx, int dy, int max)
        {
            int cx = x, cy = y;
            for (int i = 0; i < max; i++)
            {
                int nx = cx + dx, ny = cy + dy;
                if (dx != 0 && dy != 0 && (!Free(nx, cy) || !Free(cx, ny))) break;
                if (!Free(nx, ny)) return (cx, cy, TerrainMap.UnitAt(nx, ny, null));
                cx = nx; cy = ny;
            }
            return (cx, cy, null);
        }

        /// <summary>An enemy next to an explosive barrel kicks it if it would stop right by its foe and well clear of itself,
        /// so it can shoot it next (HumanEnemy.FindBarrelNear). True if it did.</summary>
        public static bool KickAtFoe(HumanEnemy h, ICombatant foe)
        {
            if (h.Ap < ShoveAp + 2 || string.IsNullOrEmpty(h.EquippedWeapon?.Db?.AmmoType)) return false;
            foreach (var (x, y) in TerrainMap.GetAdjacentCoords(h.X, h.Y))
            {
                if (x < 0 || y < 0 || x >= TerrainMap.Width || y >= TerrainMap.Height || TerrainMap.GetTile(x, y).Type != TileType.Barrel) continue;
                var (ex, ey, _) = Roll(x, y, System.Math.Sign(x - h.X), System.Math.Sign(y - h.Y), RollMax);
                if (CombatantUtil.Distance(foe, ex, ey) <= 1 && CombatantUtil.Distance(h, ex, ey) > Explosions.BarrelRadius + 1) return Shove(h, x, y);
            }
            return false;
        }

        static void Bowl(ICombatant v, int speed)
        {
            int dmg = UnityEngine.Random.Range(1, 4) * speed;
            v.Hp = System.Math.Max(0, v.Hp - dmg);
            v.StatusEffects.Prone = true;
            GameLog.Write($"{(v is Player ? "You are" : v.Name + " is")} bowled over by a rolling barrel: {dmg} damage, knocked down.");
        }

        /// <summary>A blast at (cx,cy): props in the ring just outside its radius are thrown one tile outward (a standing desk tips over).</summary>
        public static void BlastShove(int cx, int cy, int radius)
        {
            int r = radius + 1;
            var ring = new List<(int x, int y)>();
            for (int y = cy - r; y <= cy + r; y++)
                for (int x = cx - r; x <= cx + r; x++)
                    if (System.Math.Max(System.Math.Abs(x - cx), System.Math.Abs(y - cy)) == r && Movable(x, y)) ring.Add((x, y));
            foreach (var (x, y) in ring)
            {
                int dx = System.Math.Sign(x - cx), dy = System.Math.Sign(y - cy);
                if (Desk(TerrainMap.GetTile(x, y))) TerrainMap.SetTileType(x, y, TileType.ScatterCover, "desk_" + FacingOf(-dx, -dy));   // tipped over, top toward the blast
                else Push(x, y, dx, dy, 1, null);
            }
        }

        /// <summary>An enemy with no cover from the player flips a desk next to it if that would shield it. True if it did.</summary>
        public static bool FlipForCover(ICombatant h, ICombatant foe)
        {
            if (h.Ap < ShoveAp + 2 || CoverUtil.GetCoverAt(foe.X, foe.Y, h.X, h.Y).AcBonus > 0) return false;
            foreach (var (x, y) in TerrainMap.GetAdjacentCoords(h.X, h.Y))
            {
                if (!Movable(x, y) || !Desk(TerrainMap.GetTile(x, y))) continue;
                var t = TerrainMap.GetTile(x, y);
                t.Type = TileType.ScatterCover; t.Look = "desk_" + FacingOf(System.Math.Sign(x - h.X), System.Math.Sign(y - h.Y));
                bool helps = CoverUtil.GetCoverAt(foe.X, foe.Y, h.X, h.Y).AcBonus > 0;
                t.Type = TileType.Difficult; t.Look = "desk";
                if (helps) return Shove(h, x, y);
            }
            return false;
        }
    }
}
