// Ported from factions.js: RANKS, FACTIONS, pickRankFromFaction()
using System.Collections.Generic;
using UnityEngine;

namespace GameData
{
    public class RankDef
    {
        public int Ap;
        public int Mp;
        public int D20Mod;
        public int MoraleHPPercent;
    }

    public class FactionDef
    {
        public string Name;
        public Dictionary<string, int> DeckWeights;
        public int Roster;   // Batch 52: how many people the faction has in the Zone; DeckWeights split them into rank cards
    }

    public static class FactionDatabase
    {
        // Ranks: Rookie, Experienced, Professional, Veteran, Expert, Master, Legend
        // Each rank: AP, MP, d20 modifier, morale threshold (HP percent when they flee)
        // Fix: D20Mod used to climb +1 per rank straight through Legend (-1..+5), which is steeper
        // than the Mutants & Factions reference's curve (it compresses at the top: Veteran/Expert
        // and Expert/Master share a modifier). That made a Legend NPC's AC noticeably tougher than
        // the source ruleset intends. MoraleHPPercent for Legend also corrected 5 -> 1 to match
        // "Flees at <=1% HP" (Legends essentially never break).
        public static readonly Dictionary<string, RankDef> Ranks = new Dictionary<string, RankDef>
        {
            ["Rookie"] = new RankDef { Ap = 4, Mp = 4, D20Mod = -1, MoraleHPPercent = 50 },
            ["Experienced"] = new RankDef { Ap = 4, Mp = 4, D20Mod = 0, MoraleHPPercent = 30 },
            ["Professional"] = new RankDef { Ap = 4, Mp = 4, D20Mod = 1, MoraleHPPercent = 25 },
            ["Veteran"] = new RankDef { Ap = 4, Mp = 4, D20Mod = 1, MoraleHPPercent = 20 },
            ["Expert"] = new RankDef { Ap = 4, Mp = 5, D20Mod = 2, MoraleHPPercent = 15 },
            ["Master"] = new RankDef { Ap = 4, Mp = 5, D20Mod = 2, MoraleHPPercent = 10 },
            ["Legend"] = new RankDef { Ap = 4, Mp = 5, D20Mod = 3, MoraleHPPercent = 1 },
        };

        // Faction decks define relative weightings for ranks when spawning enemies.
        // Batch 53, Yan's strength tiers: S Obelisk (about two tiers above the rest); A Liberators = Sentinels,
        // Contractors, Garrison; B Wanderers (the wildcard, every rank), Surveyors; C Raiders. Rookie is a rank only.
        public static readonly Dictionary<string, FactionDef> Factions = new Dictionary<string, FactionDef>
        {
                        ["Raiders"] = new FactionDef { Name = "Raiders", Roster = 60, DeckWeights = new Dictionary<string, int> { ["Rookie"] = 45, ["Experienced"] = 33, ["Professional"] = 17, ["Veteran"] = 4, ["Expert"] = 1, ["Master"] = 0, ["Legend"] = 0 } },   // Batch 55: Yan: "they don't need to be a strong faction but they bunch up in higher numbers to compensate"
            ["Outcasts"] = new FactionDef { Name = "Outcasts", Roster = 20, DeckWeights = new Dictionary<string, int> { ["Rookie"] = 30, ["Experienced"] = 30, ["Professional"] = 20, ["Veteran"] = 10, ["Expert"] = 7, ["Master"] = 2, ["Legend"] = 1 } },
            ["Wanderers"] = new FactionDef { Name = "Wanderers", Roster = 36, DeckWeights = new Dictionary<string, int> { ["Rookie"] = 30, ["Experienced"] = 25, ["Professional"] = 20, ["Veteran"] = 12, ["Expert"] = 8, ["Master"] = 4, ["Legend"] = 1 } },
            ["Prospectors"] = new FactionDef { Name = "Prospectors", Roster = 20, DeckWeights = new Dictionary<string, int> { ["Rookie"] = 10, ["Experienced"] = 25, ["Professional"] = 30, ["Veteran"] = 20, ["Expert"] = 10, ["Master"] = 4, ["Legend"] = 1 } },
            ["Garrison"] = new FactionDef { Name = "Garrison", Roster = 26, DeckWeights = new Dictionary<string, int> { ["Rookie"] = 5, ["Experienced"] = 20, ["Professional"] = 30, ["Veteran"] = 25, ["Expert"] = 12, ["Master"] = 6, ["Legend"] = 2 } },
            ["Sentinels"] = new FactionDef { Name = "Sentinels", Roster = 26, DeckWeights = new Dictionary<string, int> { ["Rookie"] = 5, ["Experienced"] = 15, ["Professional"] = 30, ["Veteran"] = 30, ["Expert"] = 12, ["Master"] = 6, ["Legend"] = 2 } },
            ["Liberators"] = new FactionDef { Name = "Liberators", Roster = 26, DeckWeights = new Dictionary<string, int> { ["Rookie"] = 5, ["Experienced"] = 15, ["Professional"] = 30, ["Veteran"] = 30, ["Expert"] = 12, ["Master"] = 6, ["Legend"] = 2 } },
            ["Contractors"] = new FactionDef { Name = "Contractors", Roster = 22, DeckWeights = new Dictionary<string, int> { ["Rookie"] = 5, ["Experienced"] = 10, ["Professional"] = 30, ["Veteran"] = 30, ["Expert"] = 15, ["Master"] = 7, ["Legend"] = 3 } },
            ["Obelisk"] = new FactionDef { Name = "Obelisk", Roster = 32, DeckWeights = new Dictionary<string, int> { ["Rookie"] = 0, ["Experienced"] = 5, ["Professional"] = 20, ["Veteran"] = 30, ["Expert"] = 25, ["Master"] = 15, ["Legend"] = 5 } },
            ["Cartel"] = new FactionDef { Name = "Cartel", Roster = 20, DeckWeights = new Dictionary<string, int> { ["Rookie"] = 10, ["Experienced"] = 25, ["Professional"] = 30, ["Veteran"] = 20, ["Expert"] = 10, ["Master"] = 4, ["Legend"] = 1 } },
            ["IZC"] = new FactionDef { Name = "IZC", Roster = 20, DeckWeights = new Dictionary<string, int> { ["Rookie"] = 5, ["Experienced"] = 15, ["Professional"] = 30, ["Veteran"] = 30, ["Expert"] = 12, ["Master"] = 6, ["Legend"] = 2 } },
            ["Mutant"] = new FactionDef { Name = "Mutant", Roster = 0, DeckWeights = new Dictionary<string, int> { ["Rookie"] = 40, ["Experienced"] = 35, ["Professional"] = 20, ["Veteran"] = 5, ["Expert"] = 0, ["Master"] = 0, ["Legend"] = 0 } },
            ["Surveyors"] = new FactionDef { Name = "Surveyors", Roster = 12, DeckWeights = new Dictionary<string, int> { ["Rookie"] = 5, ["Experienced"] = 20, ["Professional"] = 30, ["Veteran"] = 30, ["Expert"] = 10, ["Master"] = 4, ["Legend"] = 1 } },
        };

        // Aliases keep old identifiers (e.g. from existing save data, or code that hasn't been
        // updated yet) resolving to their current, non-lore-referencing names.
        private static readonly Dictionary<string, string> FactionAliases = new Dictionary<string, string>
        {
            ["Mutants"] = "Mutant",
            ["Zombie"] = "Mutant",
            ["Rookie"] = "Wanderers",   // Batch 53: Yan: "rookie is supossed to be a rank, not a faction" (old saves)
            // ponytail: old faction names, only so pre-rename dev saves load. Delete these two lines together
            // with GameCore/NameMigration.cs before release - nothing shipped ever wrote them.
            ["Stalker"] = "Wanderers",
            ["Ecologist"] = "Surveyors",
        };

        private static readonly Dictionary<string, HashSet<string>> HostilityMatrix = new Dictionary<string, HashSet<string>>
        {
            ["Player"] = new HashSet<string> { "Raiders", "Outcasts", "Wanderers", "Prospectors", "Garrison", "Sentinels", "Liberators", "Contractors", "Obelisk", "Cartel", "IZC", "Mutant", "Surveyors" },
            // Batch 40: Yan: "mutants should also be hostile to everyone (minus one faction)" - Obelisk, both ways
            ["Mutant"] = new HashSet<string> { "Player", "Raiders", "Outcasts", "Wanderers", "Prospectors", "Garrison", "Sentinels", "Liberators", "Contractors", "Cartel", "IZC", "Surveyors" },
            ["Raiders"] = new HashSet<string> { "Player", "Mutant", "Garrison", "Sentinels", "Liberators", "Contractors", "Obelisk" },
            ["Outcasts"] = new HashSet<string> { "Player", "Mutant", "Garrison", "Sentinels", "Liberators" },
            ["Wanderers"] = new HashSet<string> { "Player", "Mutant", "Raiders", "Garrison" },
            ["Prospectors"] = new HashSet<string> { "Player", "Mutant", "Raiders" },
            ["Garrison"] = new HashSet<string> { "Player", "Raiders", "Outcasts", "Wanderers", "Mutant" },
            ["Sentinels"] = new HashSet<string> { "Player", "Raiders", "Mutant" },
            ["Liberators"] = new HashSet<string> { "Player", "Raiders", "Mutant" },
            ["Contractors"] = new HashSet<string> { "Player", "Raiders", "Mutant" },
            ["Obelisk"] = new HashSet<string> { "Player", "Raiders", "Outcasts", "Wanderers", "Prospectors", "Garrison", "Sentinels", "Liberators", "Contractors", "Cartel", "IZC", "Surveyors" },   // Batch 52: Yan: "enemies to everyone but themselves and mutants"
            ["Cartel"] = new HashSet<string> { "Player", "Raiders", "Mutant" },
            ["IZC"] = new HashSet<string> { "Player", "Raiders", "Mutant" },
            ["Surveyors"] = new HashSet<string> { "Player", "Mutant" },
        };

        public static string NormalizeFactionName(string factionName)
        {
            if (string.IsNullOrEmpty(factionName)) return "Raiders";
            if (FactionAliases.TryGetValue(factionName, out var normalized))
                return normalized;
            return factionName;
        }

        public static bool IsValidFaction(string factionName)
        {
            var normalized = NormalizeFactionName(factionName);
            return Factions.ContainsKey(normalized);
        }

        public static bool AreHostile(string left, string right)
        {
            var a = NormalizeFactionName(left);
            var b = NormalizeFactionName(right);
            if (a == b) return false;
            // Batch 52: Yan: "factions that are hostile should be hostile both ways" - either side listing the other is enough
            return HostilityMatrix.TryGetValue(a, out var ha) && ha.Contains(b) || HostilityMatrix.TryGetValue(b, out var hb) && hb.Contains(a);
        }

        /// <summary>
        /// Batch 52: only for one-off units outside the world's squads; squads draw real cards (GameCore.FactionDeck).
        /// Picks a rank from a faction's deck using its weights, ported 1:1 from
        /// pickRankFromFaction() in factions.js (builds a weighted pool, then picks uniformly
        /// from it). Falls back to Raiders if factionName isn't found, and to Rookie if the
        /// resulting pool is empty (all weights 0), matching the JS behavior.
        /// </summary>
        public static string PickRankFromFaction(string factionName)
        {
            var normalized = NormalizeFactionName(factionName);
            var faction = Factions.TryGetValue(normalized, out var f) ? f : Factions["Raiders"];
            var pool = new List<string>();
            foreach (var kvp in faction.DeckWeights)
            {
                for (int i = 0; i < kvp.Value; i++) pool.Add(kvp.Key);
            }

            if (pool.Count == 0) return "Rookie";
            return pool[Random.Range(0, pool.Count)];
        }
    }
}
