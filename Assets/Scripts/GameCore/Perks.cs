// New (file 05): the two perks that act during the enemy's turn.
using System.Linq;

namespace GameCore
{
    public static class Perks
    {
        /// <summary>Adrenaline Surge: the first time HP drops under 30 in a fight, +20 temp HP now
        /// and +2 AP on your next turn.</summary>
        public static void TryAdrenaline(Player p)
        {
            if (p.AdrenalineUsed || !p.IsAlive() || p.Hp >= 300 || !CombatantUtil.HasPerk(p, "AdrenalineSurge")) return;
            p.AdrenalineUsed = true;
            p.TempHP += 200;
            p.TempHPDuration = System.Math.Max(p.TempHPDuration, 4);
            p.AdrenalineApNextTurn = 2;
            GameLog.Write("ADRENALINE SURGE! +20 temporary HP, +2 AP next turn.");
        }
    }
}
