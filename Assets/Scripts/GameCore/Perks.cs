// New (file 05): the two perks that act during the enemy's turn.
using System.Linq;

namespace GameCore
{
    public static class Perks
    {
        /// <summary>Overwatch (Batch 72: an action anyone can take, O; the Sniper perk holds it for free at the end of your
        /// turn with 2+ AP): one shot at the first hostile that takes a step where you can see it. Called by
        /// TacticalGridSystem.AttemptMove after every step on the enemy's turn.</summary>
        public static void OnStep(ICombatant unit)
        {
            if (!(unit is Mutant enemy) || GameState.Current.CurrentTurn == Turn.Player) return;
            var p = Player.Current;
            if (p == null || !p.OverwatchHeld || !p.IsAlive() || !enemy.IsAlive() || !enemy.IsAggroed) return;
            if (Companion.Is(enemy) || EnemyAI.FriendlyToPlayer(enemy) || !EncounterState.IsActive(enemy)) return;
            TryOverwatch(p, enemy);
            if (!enemy.IsAlive()) enemy.Mp = 0;   // shot down mid-stride
        }

        public static void TryOverwatch(Player p, Mutant enemy)
        {
            if (!p.OverwatchHeld || !enemy.IsAlive()) return;
            var w = p.EquippedWeapon;
            if (w == null || !w.IsLoaded || w.IsJammed || CombatRules.IsMelee(w) || CombatantUtil.Distance(p, enemy) > w.MaxReach) return;
            if (!GameServices.RequireGrid().CheckLineOfSight(p.X, p.Y, enemy.X, enemy.Y) || !Vision.Shows(enemy)) return;
            p.OverwatchHeld = false;
            GameLog.Write($"OVERWATCH! You fire on {enemy.Name} as they move.");
            w.Fire(enemy, "Single", (int)p.AgilityMod + CombatRules.RankMod(p.Rank));
        }

        /// <summary>Adrenaline Surge: the first time HP drops under 30 in a fight, +20 temp HP now
        /// and +2 AP on your next turn.</summary>
        public static void TryAdrenaline(Player p)
        {
            if (p.AdrenalineUsed || !p.IsAlive() || p.Hp >= 30 || !CombatantUtil.HasPerk(p, "AdrenalineSurge")) return;
            p.AdrenalineUsed = true;
            p.TempHP += 20;
            p.TempHPDuration = System.Math.Max(p.TempHPDuration, 4);
            p.AdrenalineApNextTurn = 2;
            GameLog.Write("ADRENALINE SURGE! +20 temporary HP, +2 AP next turn.");
        }
    }
}
