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

        /// <summary>Ported from getTargetInRange().</summary>
        public static Mutant GetTargetInRange()
        {
            var player = Player.Current;
            if (player == null) return null;

            var weapon = player.EquippedWeapon;
            int range = weapon != null ? weapon.MaxReach : 1;

            // Prefer the player's manually selected target, as long as it's still valid.
            if (GameState.Current.SelectedEnemy is Mutant selected
                && selected.IsAlive() && !Companion.Is(selected)
                && CombatantUtil.Distance(player, selected) <= range)
            {
                return selected;
            }

            // Otherwise fall back to the nearest living hostile enemy in range.
            var hostile = GetNearestHostile(player, EncounterState.Enemies.Where(e => !Companion.Is(e)).Cast<ICombatant>(), range);
            return hostile as Mutant;
        }
    }
}
