// Ported from script.js globals: `const enemies = [...]` and `let anomalies = [];`.
// Kept as simple static lists mirroring the bare globals, since Mutant.PackTactics and the
// (future) anomaly-spawning/tick logic both need to enumerate "everyone else currently on the
// tactical grid" the same way the JS did via the `enemies` global.
using System.Collections.Generic;

namespace GameCore
{
    public static class EncounterState
    {
        public static List<Mutant> Enemies = new List<Mutant>();
        public static List<Anomaly> Anomalies = new List<Anomaly>();

        /// <summary>A fight is on (some enemy has spotted the player). Cleared by
        /// TurnLoop.CheckCombatEnd once nobody hostile is left standing.</summary>
        public static bool InCombat;
        /// <summary>Batch 69: you opened fire on enemies that hadn't spotted you - they lose their first round.</summary>
        public static bool Surprised;

        // Batch 59: how long this fight has run, how many it started with, and the radio call for help
        public static int FightRounds, FightStartCount, NextCall;
        public static HumanEnemy Caller;
        public static float CallAt = -1f;            // Time.time the caller started his radio call (the view fills its icon)
        public static bool HelpCalled;

        public static void ResetFight() { Surprised = false; FightRounds = 0; NextCall = 0; FightStartCount = 0; Caller = null; CallAt = -1f; HelpCalled = false; }

        /// <summary>Enemies still in the fight: alive, not running, not surrendered.</summary>
        public static bool IsActive(Mutant e) =>
            e.IsAlive() && !e.IsFleeing() && !(e is HumanEnemy h && h.Morale == "Surrendered");

        /// <summary>Anyone still fighting you: active and aware of you. A squad that never spotted
        /// you (or a Friendly one) doesn't keep a finished fight going.</summary>
        public static bool AnyActive() => Enemies.Exists(e => IsActive(e) && e.IsAggroed);
    }
}
