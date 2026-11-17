// Ported from script.js: getTargetInRange().
using System.Collections.Generic;
using System.Linq;
using GameData;

namespace GameCore
{
    public static class TargetSelection
    {
        public static ICombatant GetNearestHostile(ICombatant source, IEnumerable<ICombatant> candidates, int maxRange = int.MaxValue)
        {
            if (source == null || candidates == null) return null;

            return candidates
                .Where(candidate => candidate != null && candidate.IsAlive()
                    && FactionDatabase.AreHostile(source.Faction, candidate.Faction)
                    && CombatantUtil.Distance(source, candidate) <= maxRange)
                .OrderBy(candidate => CombatantUtil.Distance(source, candidate))
                .FirstOrDefault();
        }

    }
}
