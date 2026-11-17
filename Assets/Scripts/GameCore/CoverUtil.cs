// Ported from script.js: isMeleeAttack(attacker, target, weapon) and getCoverBonus(attacker,
// target, weapon), reworked to the design rules (file 00 §6): cover raises the target's AC
// (+1 light, +2 hard) instead of adding Phys-DR, and only counts if the traced firing line
// crosses a cover tile adjacent to the target. Standing ON a cover tile covers you from every
// direction except vs attackers within 2 tiles. A target next to cover that the firing line
// bypasses is Flanked: no cover and +2 to hit. Never vs melee weapons, or vs an adjacent shot
// with Point-Blank Execution.
namespace GameCore
{
    public struct CoverResult
    {
        public int AcBonus;
        public bool HasTile;
        public int TileX, TileY;   // the cover tile that took the bonus - near misses chip it
        public bool Flanked;
    }

    public static class CoverUtil
    {
        /// <summary>Ported from isMeleeAttack(attacker, target, weapon). Melee means a melee
        /// weapon (file 00 §6) - a gun fired at an adjacent target is still a ranged attack.</summary>
        public static bool IsMeleeAttack(ICombatant attacker, ICombatant target, Weapon weapon)
        {
            if (weapon == null) return CombatantUtil.Distance(attacker, target) <= 1;
            if (weapon.Db != null) return string.IsNullOrEmpty(weapon.Db.AmmoType);
            return weapon.RangeTiles <= 1;
        }

        public static CoverResult GetCover(ICombatant attacker, ICombatant target, Weapon weapon)
        {
            if (IsMeleeAttack(attacker, target, weapon)) return new CoverResult();
            if (CombatantUtil.Distance(attacker, target) <= 1 && CombatantUtil.HasPerk(attacker, "PointBlankExecution") && CombatRules.Category(weapon?.Db) == "sidearm") return new CoverResult();
            return GetCoverAt(attacker.X, attacker.Y, target.X, target.Y);
        }

        /// <summary>Cover a ranged shot from (ax,ay) at someone standing on (tx,ty) would face -
        /// also used by the AI to score tiles before moving.</summary>
        public static CoverResult GetCoverAt(int ax, int ay, int tx, int ty)
        {
            var best = new CoverResult();
            int dist = System.Math.Max(System.Math.Abs(ax - tx), System.Math.Abs(ay - ty));
            var line = Pathfinding.GetStraightLinePath(ax, ay, tx, ty);
            for (int i = 1; i < line.Count - 1; i++)
            {
                var p = line[i];
                if (System.Math.Max(System.Math.Abs(p.x - tx), System.Math.Abs(p.y - ty)) != 1) continue;
                int ac = Props.Shields(p.x, p.y, ax, ay) ? TerrainMap.CoverAcAt(p.x, p.y) : 0;   // Batch 103: a flipped desk shields one side
                if (ac > best.AcBonus) best = new CoverResult { AcBonus = ac, HasTile = true, TileX = p.x, TileY = p.y };
            }

            int onAc = TerrainMap.CoverAcAt(tx, ty);
            if (dist > 2 && onAc > best.AcBonus)
                best = new CoverResult { AcBonus = onAc, HasTile = true, TileX = tx, TileY = ty };

            if (best.AcBonus > 0) return best;

            foreach (var adj in TerrainMap.GetAdjacentCoords(tx, ty))
                if (TerrainMap.CoverAcAt(adj.x, adj.y) > 0)
                    return new CoverResult { Flanked = true };
            return best;
        }
    }
}
