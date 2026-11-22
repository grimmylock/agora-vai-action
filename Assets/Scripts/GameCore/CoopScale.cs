// Co-op enemy health (Yan, 2026-11-22: "Extra players buff all mutant and boss enemies by 25% hp"). Every player beyond the first adds PerExtraPlayer (25 %) to the health of every mutant and boss.
// Done as damage taken (x 1 / HpMult in Gunplay.Resolve and Melee.Strike through Boss.DamageScale), not as bigger health pools: the same effect on what players do, and every phase threshold,
// health bar, drip-heal share and saved wound that reads a fraction of MaxHp keeps working. Not scaled: damage that is not a gun round or a melee blow (grenades, anomalies, props, allies' fire).
using System;
namespace GameCore
{
    public static class CoopScale
    {
        public const float PerExtraPlayer = 0.25f;
        /// <summary>1 for one player, 1.25 for two, 1.5 for three, 1.75 for four.</summary>
        public static float HpMult => 1f + PerExtraPlayer * Math.Max(0, Player.All.Count - 1);
        /// <summary>The share of a blow a mutant or boss takes with the players on the map; 1 for everything else (humans, the range's dummies).</summary>
        public static float DamageTakenMult(ICombatant u) => (u is Mutant m && !(m is HumanEnemy) && !TestingGround.IsDummy(m)) || (u != null && Boss.Of(u) != null) ? 1f / HpMult : 1f;
    }
}
