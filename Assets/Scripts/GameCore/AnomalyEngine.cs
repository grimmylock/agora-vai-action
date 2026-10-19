// Ported from script.js: chooseRandomAnomalyDefinition, spawnRandomAnomalies, getAnomalyAt,
// revealAnomaly, throwPlayerAway, performAnomalyCheck, processTurnStart, and
// triggerAnomalyIfPresent (wired here as the concrete IAnomalyTrigger from the grid step).
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// Assign `GameServices.AnomalyTrigger = new AnomalyTriggerService();` (alongside Grid/Combat)
    /// to make TacticalGridSystem.AttemptMove actually trigger anomaly checks on step-in, exactly
    /// like the original triggerAnomalyIfPresent(unit) hook.
    /// </summary>
    public class AnomalyTriggerService : IAnomalyTrigger
    {
        public void TriggerAnomalyIfPresent(ICombatant unit) => AnomalyEngine.TriggerAnomalyIfPresent(unit);
    }

    public static class AnomalyEngine
    {
        /// <summary>Ported from chooseRandomAnomalyDefinition().</summary>
        public static AnomalyDef ChooseRandomAnomalyDefinition()
        {
            var keys = AnomalyDatabase.All.Keys.ToList();
            if (keys.Count == 0) return null;
            return AnomalyDatabase.All[keys[Random.Range(0, keys.Count)]];
        }

        /// <summary>Ported from spawnRandomAnomalies(count = 6).</summary>
        public static void SpawnRandomAnomalies(int count = 6)
        {
            EncounterState.Anomalies.Clear();
            int attempts = 0;
            var player = Player.Current;

            while (EncounterState.Anomalies.Count < count && attempts < 200)
            {
                int x = Random.Range(0, TerrainMap.Width);
                int y = Random.Range(0, TerrainMap.Height);
                bool onPlayer = player != null && x == player.X && y == player.Y;
                if (onPlayer || EncounterState.Anomalies.Any(an => an.X == x && an.Y == y))
                {
                    attempts++;
                    continue;
                }
                var db = ChooseRandomAnomalyDefinition();
                var anomaly = Anomaly.FromDb(db, x, y);
                if (anomaly != null) EncounterState.Anomalies.Add(anomaly);
            }

            GameLog.Write($"Generated {EncounterState.Anomalies.Count} hidden anomalies on the grid.");
        }

        /// <summary>Ported from getAnomalyAt(x, y).</summary>
        public static Anomaly GetAnomalyAt(int x, int y) =>
            EncounterState.Anomalies.FirstOrDefault(a => a.X == x && a.Y == y);

        /// <summary>Ported from revealAnomaly(anomaly).</summary>
        public static void RevealAnomaly(Anomaly anomaly)
        {
            if (anomaly == null || anomaly.Revealed) return;
            anomaly.Revealed = true;
            GameLog.Write($"{anomaly.DisplayName} anomaly has been revealed.");
        }

        /// <summary>Ported from throwPlayerAway(unit, anomaly).</summary>
        public static void ThrowPlayerAway(ICombatant unit, Anomaly anomaly)
        {
            int dx = System.Math.Sign(unit.X - anomaly.X);
            if (dx == 0) dx = 1;
            int dy = System.Math.Sign(unit.Y - anomaly.Y);
            if (dy == 0) dy = 1;

            unit.X = System.Math.Min(System.Math.Max(0, unit.X + dx * 2), TerrainMap.Width - 1);
            unit.Y = System.Math.Min(System.Math.Max(0, unit.Y + dy * 2), TerrainMap.Height - 1);

            bool isPlayer = unit is Player;
            GameLog.Write($"{(isPlayer ? "Player" : unit.Name)} is thrown to ({unit.X}, {unit.Y}).");
        }

        // ------------------------------------------------------------------ Batch 49: anomalies are spatial, not dice
        // No dodge roll: walking into an anomaly hurts. What protects you is seeing it first - your detector reveals
        // fields within its range as you walk (Clicker 3 tiles, Sniffer 6, Dowser 9, Seer 15; bare senses 1, i.e. the
        // tile next to you), a revealed field stops a click-walk and is routed around. Fields hold real artifacts:
        // a detector tells you which ones; searching (F, 2 AP, next to a revealed field) takes it - no roll.

        public const float ArtifactChance = 0.35f;

        /// <summary>Puts an artifact in the field with this chance (plus 10% per point of the zone's search bonus).</summary>
        public static void SeedArtifact(Anomaly a, int tier, string zoneId, float chance = ArtifactChance)
        {
            if (a == null || a.Artifact != null) return;
            if (Random.value >= chance + 0.1f * WorldEvents.ArtifactSearchBonus(zoneId)) return;
            a.Artifact = Economy.RollArtifact(tier)?.ToItem();
        }

        public static int DetectRange(ICombatant u)
        {
            int r = (u?.ActiveDetector as DetectorDef)?.RangeCm ?? 1;
            r += CombatantUtil.HasPerk(u, "AnomalyWhisperer") ? 2 : 0;
            r += (int)(u?.ArtifactAvoidanceBonus ?? 0);
            return System.Math.Max(1, r);
        }

        /// <summary>Ported from performAnomalyCheck(unit, anomaly). Batch 49: no avoidance roll - you're in it.</summary>
        public static void PerformAnomalyCheck(ICombatant unit, Anomaly anomaly)
        {
            bool isPlayer = unit is Player;
            string label = isPlayer ? "Player" : unit.Name;

            if (anomaly.Dc.HasValue && unit.ArtifactAutoPassAnomalies)
            {
                GameLog.Write($"{label}'s artifact automatically dodges the anomaly hazard.");
                return;
            }
            RevealAnomaly(anomaly);
            float anomDR = unit.AnomDR;
            int effectiveDamage = CombatRules.AnomalousDamage(anomaly.Damage, anomDR);   // same 75% cap as everything else

            if (anomaly.AnomalyType == "Radiation")
            {
                if (isPlayer && PlayerGearUtil.HasArmorMod("ClosedBreathing"))
                {
                    GameLog.Write("Closed-Breathing protects against the radiation pocket. No damage taken.");
                    return;
                }
                unit.Hp = System.Math.Max(0, unit.Hp - effectiveDamage);
                GameLog.Write($"{label} takes {effectiveDamage} radiation damage from the pocket.");
                if (isPlayer) unit.StatusEffects.Irradiated = true;
                return;
            }

            unit.Hp = System.Math.Max(0, unit.Hp - effectiveDamage);
            GameLog.Write($"{label} takes {effectiveDamage} anomaly damage.");

            if (anomaly.AnomalyType == "Pyre" && Random.value < 0.05f)   // ponytail: the old "natural 1" burn, kept as a 5% chance
            {
                PlayerGearUtil.RemoveRandomMedicalConsumable();
            }

            if (anomaly.AnomalyType == "Livewire")
            {
                unit.PendingApDrain += 2;
                GameLog.Write($"{label} will lose 2 AP on next turn due to Livewire failure.");
            }

            if (anomaly.AnomalyType == "Acid Bloom")
            {
                unit.StatusEffects.ToxicBleed = true;
                GameLog.Write("Toxic Bleed has been inflicted and will deal 5 HP per turn until treated.");
            }

            if (anomaly.AnomalyType == "Spindle")
            {
                unit.StatusEffects.Prone = true;
                ThrowPlayerAway(unit, anomaly);
                GameLog.Write($"{label} is now prone and must spend 1 AP to stand up before moving.");
            }

            if (anomaly.Effects != null && anomaly.Effects.ApDrain.HasValue && anomaly.AnomalyType != "Livewire")
            {
                unit.PendingApDrain += anomaly.Effects.ApDrain.Value;
                GameLog.Write($"{label} will lose {anomaly.Effects.ApDrain.Value} AP on next turn.");
            }
        }

        /// <summary>The nearest revealed, unsearched anomaly on or next to the player.</summary>
        public static Anomaly SearchableNear(Player p) =>
            EncounterState.Anomalies.FirstOrDefault(a => a.Revealed && !a.Searched
                && System.Math.Max(System.Math.Abs(a.X - p.X), System.Math.Abs(a.Y - p.Y)) <= 1);

        /// <summary>Searches a revealed anomaly (file 03): 1 action (2 AP). Batch 49: no roll - if an artifact is lying in
        /// the field you take it. Fields refill after a Surge or an anomaly shift.</summary>
        public static bool TrySearchAnomaly(Player p)
        {
            var a = SearchableNear(p);
            if (a == null) return false;
            if (GameState.Current.CurrentTurn != Turn.Player) return true;
            if (p.Ap < 2) { GameLog.Write("Not enough AP to search the anomaly."); return true; }
            p.Ap -= 2;
            a.Searched = true;
            GameLog.Write($"You comb the {a.DisplayName} field.");
            if (a.Artifact == null) { GameLog.Write("Nothing in this one. Fields fill up again after a Surge."); return true; }
            var art = a.Artifact; a.Artifact = null;
            p.AddItem(art);
            GameLog.Write($"[ARTIFACT] You pull a {art.Name} out of the anomaly! (worth ~{art.Price} RU)");
            p.ApplyCarryEffects();
            return true;
        }

        /// <summary>Batch 49: every step, anomalies within your detection range are revealed (any detector, not only the
        /// Seer). With a detector you also learn whether the field holds an artifact. Returns how many were found.</summary>
        public static int DetectorSweep(Player p)
        {
            int range = DetectRange(p), found = 0;
            bool detector = p.ActiveDetector is DetectorDef;
            foreach (var a in EncounterState.Anomalies)
                if (!a.Revealed && System.Math.Max(System.Math.Abs(a.X - p.X), System.Math.Abs(a.Y - p.Y)) <= range)
                {
                    RevealAnomaly(a);
                    found++;
                    if (!detector) GameLog.Write("  The air ahead shimmers wrong. You stop.");
                    else if (a.Artifact != null) GameLog.Write("  The detector chirps - there's something worth searching in there.");
                }
            return found;
        }

        /// <summary>Ported from triggerAnomalyIfPresent(unit).</summary>
        public static void TriggerAnomalyIfPresent(ICombatant unit)
        {
            var anomaly = GetAnomalyAt(unit.X, unit.Y);
            if (anomaly == null) return;
            if (!(unit is Player)) return;   // Batch 49: the Zone's own know where the fields are (the old dodge roll was their only protection)

            bool isPlayer = unit is Player;
            GameLog.Write($"{(isPlayer ? "Player" : unit.Name)} steps onto a hidden anomaly.");
            PerformAnomalyCheck(unit, anomaly);
        }

        /// <summary>Ported from processTurnStart(unit).</summary>
        public static void ProcessTurnStart(ICombatant unit)
        {
            bool isPlayer = unit is Player;
            string label = isPlayer ? "Player" : unit.Name;

            if (isPlayer && CombatantUtil.HasPerk(unit, "Sprinter"))
                unit.Mp += 2;   // file 05: +2 MP per turn

            if (unit.PendingApDrain > 0)
            {
                int drain = System.Math.Min(unit.Ap, unit.PendingApDrain);
                unit.Ap = System.Math.Max(0, unit.Ap - drain);
                GameLog.Write($"{label} loses {drain} AP (drained).");
                unit.PendingApDrain = 0;
            }

            // New: NPCs and mutants bleed too (wounds, serrated blades, Prowler claws) - this
            // used to only ever tick for the player.
            if (!isPlayer && unit.IsAlive() && (unit.StatusEffects.Bleeding || unit.StatusEffects.ToxicBleed))
            {
                int bleed = (unit.StatusEffects.Bleeding ? 5 : 0) + (unit.StatusEffects.ToxicBleed ? 5 : 0);
                unit.Hp = System.Math.Max(0, unit.Hp - bleed);
                GameLog.Write($"{label} bleeds for {bleed} HP.");
                if (!unit.IsAlive()) Weapon.OnTargetKilled(Player.Current, unit);
            }

            if (isPlayer)
            {
                var player = (Player)unit;
                DetectorSweep(player);

                if (player.StatusEffects.Bleeding && player.BleedImmune) { player.StatusEffects.Bleeding = false; GameLog.Write("The bleeding stops on its own."); }   // Batch 90: Rosary / Heart of the Wound
                if (player.StatusEffects.Bleeding)
                {
                    player.TakeDamage(5);
                    GameLog.Write("Bleeding deals 5 HP damage.");
                }
                if (player.StatusEffects.ToxicBleed)
                {
                    player.TakeDamage(5);
                    GameLog.Write("Toxic Bleed deals 5 HP damage.");
                }
                if (player.TempHPDuration > 0)
                {
                    player.TempHPDuration -= 1;
                    if (player.TempHPDuration <= 0)
                    {
                        player.TempHPDuration = 0;
                        player.TempHP = 0;
                        GameLog.Write("Temporary HP has expired.");
                    }
                }

                if (player.ArtifactRegen > 0)
                {
                    player.HealPermanent((int)player.ArtifactRegen);
                    GameLog.Write($"Artifact passive regenerates {player.ArtifactRegen} HP.");
                }

                if (player.IsDepleted())
                {
                    var depletedStats = new List<string>();
                    if (player.Hunger <= 0) depletedStats.Add("hunger");
                    if (player.Thirst <= 0) depletedStats.Add("thirst");
                    if (player.Tiredness <= 0) depletedStats.Add("tiredness");
                    GameLog.Write($"[SURVIVAL WARNING] You are critically low on {string.Join(" and ", depletedStats)}! Weapons cost +1 AP and carry penalties double.");

                    // New: depletion previously had no cost beyond the AP/carry penalties above -
                    // a player could sit at 0 hunger/thirst/tiredness indefinitely with nothing
                    // actually punishing it. 1 HP per depleted stat per turn is a mild, escalating
                    // tax (worse the more stats are empty at once) rather than a death sentence -
                    // ponytail: flat rate, not scaled by how deep into the negative a stat is
                    // (they're clamped at 0 anyway, see Player.Move/TravelToSector).
                    player.TakeDamage(depletedStats.Count);
                    GameLog.Write($"Survival depletion deals {depletedStats.Count} HP damage.");
                }
            }
        }
    }
}
