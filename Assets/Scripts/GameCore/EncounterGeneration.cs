// Ported from script.js: createMutantFromDb, pickRandomMutantByTier, rankToTierRange,
// pickRandomWeaponByTier, pickRandomArmorByTier, generateEncounter, spawnMutantEncounterForTier,
// spawnHumanEncounterForTier, spawnSectorAnomalies, spawnArtifactsInSector, tickAmbientSpawns.
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class EncounterGeneration
    {
        /// <summary>Ported from createMutantFromDb(dbEntry, x, y).</summary>
        public static Mutant CreateMutantFromDb(MutantDef dbEntry, int x, int y) =>
            dbEntry == null ? new Mutant("Mutant", x, y, null) : new Mutant(dbEntry.Name, x, y, dbEntry);

        /// <summary>Ported from pickRandomMutantByTier(minTier, maxTier).</summary>
        public static MutantDef PickRandomMutantByTier(int minTier, int maxTier)
        {
            var pool = new List<MutantDef>();
            foreach (var tierKey in MutantDatabase.Tiers.Keys)
            {
                int tier = ParseTierKey(tierKey);
                if (tier < minTier || tier > maxTier) continue;
                pool.AddRange(MutantDatabase.Tiers[tierKey].Values);
            }
            return pool.Count == 0 ? null : pool[Random.Range(0, pool.Count)];
        }

        /// <summary>Ported from rankToTierRange(rankName).</summary>
        public static (int min, int max) RankToTierRange(string rankName)
        {
            switch (rankName)
            {
                case "Rookie": return (1, 2);
                case "Experienced": return (1, 2);
                case "Professional": return (2, 3);
                case "Veteran": return (3, 4);
                case "Expert": return (4, 4);
                case "Master": return (4, 5);
                case "Legend": return (5, 5);
                default: return (1, 2);
            }
        }

        /// <summary>Batch 96: one-of-a-kind weapons are placed by hand (the G-7 is the Foreman's, G7Lab) - never in random kit,
        /// trader stock or loot. The harness caught a Lattice Foundry guard rolling the G-7.</summary>
        public static readonly HashSet<string> Unique = new HashSet<string> { "G-7 Rail Prototype", "Tactical Hatchet" };

        /// <summary>Ported from pickRandomWeaponByTier(minTier, maxTier).</summary>
        public static WeaponDef PickRandomWeaponByTier(int minTier, int maxTier)
        {
            var pool = new List<WeaponDef>();
            foreach (var category in WeaponDatabase.Categories.Values)
                foreach (var w in category.Values)
                    if (w.Tier >= minTier && w.Tier <= maxTier && !Unique.Contains(w.Name))
                        pool.Add(w);
            return pool.Count == 0 ? null : pool[Random.Range(0, pool.Count)];
        }

        /// <summary>Ported from pickRandomArmorByTier(minTier, maxTier). includeFactionIssue=false
        /// leaves out FactionArmorDatabase gear (trader stock - see RolledStockSystem).</summary>
        public static ArmorDef PickRandomArmorByTier(int minTier, int maxTier, bool includeFactionIssue = true)
        {
            var pool = new List<ArmorDef>();
            foreach (var tierKey in ArmorDatabase.Tiers.Keys)
            {
                int tier = ParseTierKey(tierKey);
                if (tier < minTier || tier > maxTier) continue;
                foreach (var kvp in ArmorDatabase.Tiers[tierKey])
                    if (includeFactionIssue || !FactionArmorDatabase.IsFactionIssue(kvp.Key))
                        pool.Add(kvp.Value);
            }
            return pool.Count == 0 ? null : pool[Random.Range(0, pool.Count)];
        }

        /// <summary>New: a weapon from the faction's preferred categories (FactionArmorDatabase.
        /// WeaponWeights - every faction fields every type, the weights only set how common each is),
        /// at the rank's tier band. A category with nothing in the band falls back to its best weapon
        /// below the band (melee tops out at Tier 2); null only for factions with no weights.</summary>
        public static WeaponDef PickFactionWeapon(string faction, int minTier, int maxTier)
        {
            string category = FactionArmorDatabase.PickWeaponCategory(faction);
            if (category == null || !WeaponDatabase.Categories.TryGetValue(category, out var weapons)) return null;
            var pool = weapons.Values.Where(w => w.Tier >= minTier && w.Tier <= maxTier && !Unique.Contains(w.Name)).ToList();
            if (pool.Count == 0)
            {
                int best = weapons.Values.Where(w => w.Tier <= maxTier && !Unique.Contains(w.Name)).Select(w => w.Tier).DefaultIfEmpty(0).Max();
                pool = weapons.Values.Where(w => w.Tier == best && !Unique.Contains(w.Name)).ToList();
            }
            return pool.Count == 0 ? null : pool[Random.Range(0, pool.Count)];
        }

        /// <summary>New: NPC gear from its faction (ArtSource/02b_Faction_Armor_and_Weapons.md) - the
        /// faction's own armor line at the rank's tier (Leaders/bosses always Heavy) plus a weapon from
        /// its preferred categories. Factions without a line fall back to the old any-faction pools.</summary>
        public static (WeaponDef weapon, ArmorDef armor) PickFactionLoadout(string faction, string rank, int zoneTier, bool isLeader = false)
        {
            var (min, max) = RankToTierRange(rank);
            var weapon = PickFactionWeapon(faction, min, max) ?? PickRandomWeaponByTier(min, max);
            var armor = FactionArmorDatabase.PickArmor(faction, rank, zoneTier, isLeader) ?? PickRandomArmorByTier(min, max);
            return (weapon, armor);
        }

        private static int ParseTierKey(string tierKey)
        {
            var digits = new string(tierKey.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out var t) ? t : 1;
        }

        /// <summary>
        /// Ported from generateEncounter(factionName, centerX, centerY, overrideCount, spawnBossOverride, tierOverride).
        ///
        /// NOTE (pre-existing bug, preserved): the boss-candidate lookup below reads
        /// MutantDatabase.Get("tier3", "ChimeraAlpha") / "Controller" — mirroring
        /// MUTANT_DB.tier3.ChimeraAlpha / .Controller in the original. Neither key exists in the
        /// mutant database (see database.js / MutantDatabase.cs); the source comment claims these
        /// are "the actual highest-tier mutants available" but they were never added to MUTANT_DB.
        /// So a boss-spawn roll for a Mutant faction always resolves both boss candidates to null,
        /// and CreateMutantFromDb's null-fallback silently spawns a generic default-stat "Mutant"
        /// as the boss instead of anything actually boss-tier. Flag if you'd like this fixed
        /// (e.g. point it at MUTANT_DB.tier5.VoidWeaver / tier4 entries instead).
        /// </summary>
        public static void GenerateEncounter(string factionName, int centerX = 8, int centerY = 8, int? overrideCount = null, bool spawnBossOverride = false, int? tierOverride = null)
        {
            var roll = DiceRoller.RollD20();
            int count = overrideCount ?? 3;

            if (overrideCount == null)
            {
                if (roll.Chosen >= 1 && roll.Chosen <= 6) count = 2;
                else if (roll.Chosen >= 7 && roll.Chosen <= 13) count = 3;
                else if (roll.Chosen >= 14 && roll.Chosen <= 18) count = 4;
                else if (roll.Chosen == 19) count = 5;
                else if (roll.Chosen == 20) count = 0; // will spawn boss + squad
            }

            bool spawnBoss = spawnBossOverride || (overrideCount == null && roll.Chosen == 20);
            int tier = tierOverride ?? 1;

            GameLog.Write($"Encounter roll: {roll.Chosen} -> spawning {(spawnBoss ? "Boss+Squad" : count.ToString())} for faction {factionName}.");

            if (factionName == "Mutants" || factionName == "Mutant")
            {
                if (spawnBoss)
                {
                    var bossCandidates = new[]
                    {
                        MutantDatabase.Get("tier5", "VoidWeaver"),
                        MutantDatabase.Get("tier4", "DreadProwler"),
                    }.Where(m => m != null).ToArray();

                    var bossDb = bossCandidates.Length > 0
                        ? bossCandidates[Random.Range(0, bossCandidates.Length)]
                        : PickRandomMutantByTier(4, 5);

                    var boss = CreateMutantFromDb(bossDb, centerX, centerY);
                    boss.IsAggroed = false;
                    EncounterState.Enemies.Add(boss);

                    for (int i = 0; i < 3; i++)
                    {
                        var db = PickRandomMutantByTier(1, 2);
                        int ex = System.Math.Min(System.Math.Max(0, centerX + (i + 1)), TerrainMap.Width - 1);
                        int ey = System.Math.Min(System.Math.Max(0, centerY + (i % 2 == 0 ? 1 : -1)), TerrainMap.Height - 1);
                        if (db != null)
                        {
                            var m = CreateMutantFromDb(db, ex, ey);
                            m.IsAggroed = false;
                            EncounterState.Enemies.Add(m);
                        }
                    }
                    return;
                }

                for (int i = 0; i < count; i++)
                {
                    // Fix: this used to hardcode 3 as the cap regardless of MutantDatabase's actual
                    // range - so mutant difficulty flatlined at tier 3 for every Tier 4-7 zone,
                    // trivializing the back half of the game's difficulty curve outside boss rolls.
                    int mTier = tierOverride.HasValue ? System.Math.Min(MutantDatabase.MaxTier, tier) : (roll.Chosen <= 6 ? 1 : roll.Chosen <= 13 ? 1 : 2);
                    var db = PickRandomMutantByTier(mTier, System.Math.Min(MutantDatabase.MaxTier, mTier + 1));
                    int ex = System.Math.Min(System.Math.Max(0, centerX + (Random.Range(0, 3) - 1)), TerrainMap.Width - 1);
                    int ey = System.Math.Min(System.Math.Max(0, centerY + (Random.Range(0, 3) - 1)), TerrainMap.Height - 1);
                    if (db != null)
                    {
                        var m = CreateMutantFromDb(db, ex, ey);
                        m.IsAggroed = false;
                        EncounterState.Enemies.Add(m);
                    }
                }
                return;
            }

            if (spawnBoss)
            {
                const string bossRank = "Legend";
                var (bMin, bMax) = RankToTierRange(bossRank);
                var (bossWeapon, bossArmor) = PickFactionLoadout(factionName, bossRank, tier + 1, isLeader: true);
                var boss = new HumanEnemy($"{factionName} Boss", centerX, centerY, factionName, bossRank, bossWeapon, bossArmor);
                boss.IsAggroed = false;
                HumanLootSystem.RollSpawnStash(boss, bMax);
                EncounterState.Enemies.Add(boss);

                for (int i = 0; i < 3; i++)
                {
                    var rank = FactionDatabase.PickRankFromFaction(factionName);
                    var (rMin, rMax) = RankToTierRange(rank);
                    var (w, a) = PickFactionLoadout(factionName, rank, tier);
                    var e = new HumanEnemy($"{factionName} Grunt", centerX + (i + 1), centerY + (i % 2 == 0 ? 1 : -1), factionName, rank, w, a);
                    e.IsAggroed = false;
                    HumanLootSystem.RollSpawnStash(e, rMax);
                    EncounterState.Enemies.Add(e);
                }
                return;
            }

            for (int i = 0; i < count; i++)
            {
                var rank = FactionDatabase.PickRankFromFaction(factionName);
                var (rMin, rMax) = RankToTierRange(rank);
                var (w, a) = PickFactionLoadout(factionName, rank, tier);
                int ex = System.Math.Min(System.Math.Max(0, centerX + (Random.Range(0, 3) - 1)), TerrainMap.Width - 1);
                int ey = System.Math.Min(System.Math.Max(0, centerY + (Random.Range(0, 3) - 1)), TerrainMap.Height - 1);
                var e = new HumanEnemy($"{factionName} Thug", ex, ey, factionName, rank, w, a);
                e.IsAggroed = false;
                HumanLootSystem.RollSpawnStash(e, rMax);
                EncounterState.Enemies.Add(e);
            }
        }

        /// <summary>Ported from spawnMutantEncounterForTier(tier).</summary>
        public static void SpawnMutantEncounterForTier(int tier)
        {
            int count = 2 + Random.Range(0, tier);
            var player = Player.Current;
            for (int i = 0; i < count; i++)
            {
                int minT = System.Math.Max(1, tier - 1);
                int maxT = System.Math.Min(MutantDatabase.MaxTier, tier);
                var mutantDb = PickRandomMutantByTier(minT, maxT);
                int ex = System.Math.Min(System.Math.Max(0, player.X + (i % 2 == 0 ? 2 : -2)), TerrainMap.Width - 1);
                int ey = System.Math.Min(System.Math.Max(0, player.Y + (i > 1 ? 2 : -2)), TerrainMap.Height - 1);
                if (mutantDb != null) EncounterState.Enemies.Add(CreateMutantFromDb(mutantDb, ex, ey));
            }
        }

        /// <summary>Ported from spawnHumanEncounterForTier(factionName, tier).</summary>
        public static void SpawnHumanEncounterForTier(string factionName, int tier)
        {
            string[] ranksByTier = { "Rookie", "Experienced", "Professional", "Veteran", "Master" };
            string rankName = ranksByTier[System.Math.Min(tier - 1, ranksByTier.Length - 1)];
            int count = 2 + Random.Range(0, 2);
            var player = Player.Current;

            for (int i = 0; i < count; i++)
            {
                var (min, max) = RankToTierRange(rankName);
                var (w, a) = PickFactionLoadout(factionName, rankName, tier);
                int ex = System.Math.Min(System.Math.Max(0, player.X + (i % 2 == 0 ? 3 : -3)), TerrainMap.Width - 1);
                int ey = System.Math.Min(System.Math.Max(0, player.Y + (i > 1 ? 3 : -3)), TerrainMap.Height - 1);
                var enemy = new HumanEnemy($"{factionName} Operative", ex, ey, factionName, rankName, w, a);
                HumanLootSystem.RollSpawnStash(enemy, max);
                EncounterState.Enemies.Add(enemy);
            }
        }

        /// <summary>Ported from spawnSectorAnomalies(count, tier).</summary>
        public static void SpawnSectorAnomalies(int count, int tier)
        {
            int attempts = 0;
            var player = Player.Current;
            while (EncounterState.Anomalies.Count < count && attempts < 100)
            {
                int x = Random.Range(0, TerrainMap.Width);
                int y = Random.Range(0, TerrainMap.Height);
                if ((player != null && x == player.X && y == player.Y) || EncounterState.Anomalies.Any(a => a.X == x && a.Y == y))
                {
                    attempts++;
                    continue;
                }
                var db = AnomalyEngine.ChooseRandomAnomalyDefinition();
                var anomaly = Anomaly.FromDb(db, x, y);
                if (anomaly != null)
                {
                    anomaly.Dc = (anomaly.Dc ?? 12) + tier / 2;
                    anomaly.Damage += tier * 5;
                    AnomalyEngine.SeedArtifact(anomaly, tier, MacroState.Current.CurrentZoneId);   // Batch 49
                    EncounterState.Anomalies.Add(anomaly);
                }
            }
        }

        // Batch 42: TickAmbientSpawns (a group every few steps at a random map edge) and CrossingHazardCheck (a group on
        // re-entering a zone) were removed. They were tabletop rolls; Yan: "encounters should be natural from the squads
        // and mutants wandering around following their tasks exactly like a-life". Enemies now only come from A-Life squads.
    }
}
