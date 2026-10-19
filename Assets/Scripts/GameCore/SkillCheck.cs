// Ported from script.js: rollSkillCheck(unit, stat, targetDC, options). Used by attemptInteract()
// (interactive objects) and other stat-gated checks — those calling flows are ported in the
// interactions/anomalies step, but the dice logic itself belongs here with the rest of the roll
// math. `stat` may be a single stat key or several candidates (e.g. Electronic Lock accepts
// Agility OR Intelligence); only Player currently has a StatBlock, so non-Player units always
// roll with a flat +0 modifier, matching the JS's `unit.stats &&` guard.
namespace GameCore
{
    public struct SkillCheckResult
    {
        public D20Roll Roll;
        public int Modifier;
        public int Total;
        public bool Success;
        public string Stat;
    }

    public static class SkillCheck
    {
        /// <summary>Ported from rollSkillCheck(unit, stat, targetDC, options).</summary>
        public static SkillCheckResult RollSkillCheck(ICombatant unit, string[] stat, int targetDC, string advantageState = "standard")
        {
            string selectedStat = null;
            int modifier = 0;

            if (unit is Player player && stat != null)
            {
                // Fix: data writes stats both as "A" and as "Agility" - a full name read as a 0
                // stat (a -5 modifier). Normalized to the letter, and when two stats are listed
                // the player uses the better one (file 00 §3).
                modifier = int.MinValue;
                foreach (var raw in stat)
                {
                    if (string.IsNullOrEmpty(raw)) continue;
                    string s = raw.Substring(0, 1).ToUpperInvariant();
                    int m = CombatantUtil.GetStatModifier(player.Stats.Get(s));
                    if (m > modifier) { modifier = m; selectedStat = s; }
                }
                if (modifier == int.MinValue) modifier = 0;
            }
            else if (stat != null && stat.Length > 0)
            {
                selectedStat = stat[0];
            }

            var roll = DiceRoller.RollD20(advantageState);
            int total = roll.Chosen + modifier;
            bool success = total >= targetDC;

            bool isPlayer = unit is Player;
            string label = isPlayer ? "Player" : (unit.Name ?? "Unit");
            GameLog.Write($"{label} attempts {selectedStat} check: {DiceRoller.FormatRollLog(roll, modifier, targetDC)} -> {(success ? "Success" : "Failure")}");

            return new SkillCheckResult { Roll = roll, Modifier = modifier, Total = total, Success = success, Stat = selectedStat };
        }
    }
}
