// New (file 05): the two perks that act during the enemy's turn.
using System.Linq;

namespace GameCore
{
    public static class Perks
    {
        /// <summary>Adrenaline Surge: the first time HP drops under 30 in a fight, +20 temp HP now
        /// (the +2 AP of the turn-based game is gone with action points).</summary>
        public static void TryAdrenaline(Player p)
        {
            if (p.AdrenalineUsed || !p.IsAlive() || p.Hp >= 300 || !CombatantUtil.HasPerk(p, "AdrenalineSurge")) return;
            p.AdrenalineUsed = true;
            p.TempHP += 200;
            p.TempHPDuration = System.Math.Max(p.TempHPDuration, 4);
            GameLog.Write("ADRENALINE SURGE! +20 temporary HP.");
        }
    }
}
