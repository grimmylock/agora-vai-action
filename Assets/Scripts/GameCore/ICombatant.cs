// Ported from script.js: the shared shape that distance(), isAdjacent(), hasPerk(),
// getMoveCost(), attemptMove() etc. all expect from "a unit" — whether that's the Player,
// a Mutant, or a HumanEnemy. JS didn't need an explicit interface for this (duck typing), but
// C# does if grid/combat code is going to work generically across all combatant types.
using System.Collections.Generic;
using System.Linq;

namespace GameCore
{
    public interface ICombatant
    {
        string Name { get; }
        int X { get; set; }
        int Y { get; set; }
        int Ap { get; set; }
        float Mp { get; set; }   // Batch 102: float - a diagonal step costs 1.5
        int MaxMp { get; set; }
        float PhysDR { get; set; }
        float AnomDR { get; set; }
        int Ac { get; set; }
        int Hp { get; set; }
        bool IsAlive();

        /// <summary>Empty list (not null) for units with no perks — mirrors hasPerk()'s null-safety in the JS.</summary>
        List<string> ActivePerks { get; }

        /// <summary>Faction identity used for hostility checks and target selection.</summary>
        string Faction { get; }

        // --- Added for the anomaly system (script.js's performAnomalyCheck/processTurnStart
        // operate on *any* unit generically, not just Player — see AnomalyEngine.cs). In the JS,
        // Mutant simply didn't have statusEffects/pendingApDrain/etc., so assigning unit.statusEffects.prone
        // = true on a mutant silently created an ad-hoc property nothing else read. Giving every
        // ICombatant these members for real is the structurally-honest version of that same behavior. ---

        StatusEffects StatusEffects { get; }
        float AgilityMod { get; }
        int PendingApDrain { get; set; }

        /// <summary>0 for units with no artifact system (Mutant/HumanEnemy).</summary>
        float ArtifactAvoidanceBonus { get; }

        /// <summary>false for units with no artifact system (Mutant/HumanEnemy).</summary>
        bool ArtifactAutoPassAnomalies { get; }

        /// <summary>DetectorDef or null — only Player ever sets this in the source data.</summary>
        object ActiveDetector { get; }
    }

    public static class CombatantUtil
    {
        /// <summary>Ported from hasPerk(unit, perk) in script.js.</summary>
        public static bool HasPerk(ICombatant unit, string perk)
        {
            if (unit == null || unit.ActivePerks == null) return false;
            return unit.ActivePerks.Contains(perk);
        }

        /// <summary>Ported from distance(a, b) in script.js — Chebyshev (chessboard) distance.</summary>
        public static int Distance(ICombatant a, ICombatant b)
        {
            return System.Math.Max(System.Math.Abs(a.X - b.X), System.Math.Abs(a.Y - b.Y));
        }

        /// <summary>Ported from isAdjacent(a, b) in script.js.</summary>
        public static bool IsAdjacent(ICombatant a, ICombatant b)
        {
            return Distance(a, b) <= 1;
        }

        /// <summary>Distance overload against a raw coordinate (patrol/flank targets aren't full units).</summary>
        public static int Distance(ICombatant a, int x, int y)
        {
            return System.Math.Max(System.Math.Abs(a.X - x), System.Math.Abs(a.Y - y));
        }

        /// <summary>IsAdjacent overload against a raw coordinate.</summary>
        public static bool IsAdjacent(ICombatant a, int x, int y) => Distance(a, x, y) <= 1;

        /// <summary>Ported from getStatModifier(unit, stat) in script.js.</summary>
        public static int GetStatModifier(int statValue)
        {
            return (int)System.Math.Floor((statValue - 10) / 2.0);
        }
    }
}
