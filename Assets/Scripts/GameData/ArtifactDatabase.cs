// Ported from database.js: ARTIFACT_DB
using System.Collections.Generic;

namespace GameData
{
    public class ArtifactDef
    {
        public string Name;
        public string Type;            // Gravitational, Chemical, Thermal, Electrical, Legendary
        public int Tier;
        public float AnomPenalty;
        public float WeightKg;
        public string Description;

        // Gravitational
        public int? CarryWeightBonus;

        // Chemical
        public int? HpRegen;

        // Thermal
        public bool? BleedNegate;

        // Electrical
        public int? MpBonus;
        public int? AnomalyAvoidanceBonus;

        // Legendaries
        public bool ImmuneBleed;
        public bool PassAllAnomalies;
        public int? ApBonus;
        public float? PhysDRBonus;
        public float? AnomDRBonus;
        public int? MaxHpPenalty;
        public float? BypassPhysDR;
        public bool KillRefundAp;
    }

    public static class ArtifactDatabase
    {
        public static readonly Dictionary<string, ArtifactDef> All = new Dictionary<string, ArtifactDef>
        {
            // Gravitational (Carry Weight)
            ["driftBubble"] = new ArtifactDef { Name = "Drift Bubble", Type = "Gravitational", Tier = 1, AnomPenalty = -0.05f, CarryWeightBonus = 5, WeightKg = 0.5f, Description = "+5kg Carry Weight" },
            ["twistknot"] = new ArtifactDef { Name = "Twistknot", Type = "Gravitational", Tier = 2, AnomPenalty = -0.10f, CarryWeightBonus = 10, WeightKg = 0.5f, Description = "+10kg Carry Weight" },
            ["ballast"] = new ArtifactDef { Name = "Ballast", Type = "Gravitational", Tier = 3, AnomPenalty = -0.15f, CarryWeightBonus = 15, WeightKg = 0.5f, Description = "+15kg Carry Weight" },
            ["weightlessPearl"] = new ArtifactDef { Name = "Weightless Pearl", Type = "Gravitational", Tier = 5, AnomPenalty = -0.25f, CarryWeightBonus = 30, WeightKg = 0.5f, Description = "+30kg Carry Weight" },

            // Chemical (Auto-Regeneration)
            ["pulsePebble"] = new ArtifactDef { Name = "Pulse Pebble", Type = "Chemical", Tier = 1, AnomPenalty = -0.05f, HpRegen = 10, WeightKg = 0.5f, Description = "+10 HP Auto-Regeneration per turn" },
            ["fieryRouge"] = new ArtifactDef { Name = "Fiery Rouge", Type = "Chemical", Tier = 2, AnomPenalty = -0.10f, HpRegen = 20, WeightKg = 0.5f, Description = "+20 HP Auto-Regeneration per turn" },
            ["heartspark"] = new ArtifactDef { Name = "Heartspark", Type = "Chemical", Tier = 3, AnomPenalty = -0.15f, HpRegen = 30, WeightKg = 0.5f, Description = "+30 HP Auto-Regeneration per turn" },
            ["lanternSeed"] = new ArtifactDef { Name = "Lantern Seed", Type = "Chemical", Tier = 5, AnomPenalty = -0.25f, HpRegen = 50, WeightKg = 0.5f, Description = "+50 HP Auto-Regeneration per turn" },

            // Thermal (Bleeding Control)
            ["clot"] = new ArtifactDef { Name = "Clot", Type = "Thermal", Tier = 1, AnomPenalty = -0.05f, BleedNegate = false, WeightKg = 0.5f, Description = "Mitigates minor bleed effects" },
            ["scabStone"] = new ArtifactDef { Name = "Scab Stone", Type = "Thermal", Tier = 2, AnomPenalty = -0.10f, BleedNegate = false, WeightKg = 0.5f, Description = "Reduces bleed damage" },
            ["rosary"] = new ArtifactDef { Name = "Rosary", Type = "Thermal", Tier = 4, AnomPenalty = -0.20f, BleedNegate = true, WeightKg = 0.5f, Description = "Negates bleeding damage entirely" },

            // Electrical (MP & Avoidance)
            ["staticWisp"] = new ArtifactDef { Name = "Static Wisp", Type = "Electrical", Tier = 1, AnomPenalty = -0.05f, MpBonus = 1, WeightKg = 0.5f, Description = "+1 MP" },
            ["quickglass"] = new ArtifactDef { Name = "Quickglass", Type = "Electrical", Tier = 3, AnomPenalty = -0.15f, MpBonus = 1, AnomalyAvoidanceBonus = 1, WeightKg = 0.5f, Description = "+1 MP and +1 Anomaly Avoidance" },
            ["frostLattice"] = new ArtifactDef { Name = "Frost Lattice", Type = "Electrical", Tier = 5, AnomPenalty = -0.25f, MpBonus = 2, AnomalyAvoidanceBonus = 2, WeightKg = 0.5f, Description = "+2 MP and +2 Anomaly Avoidance" },

            // Tier 6 Legendaries
            ["heartOfTheWound"] = new ArtifactDef { Name = "Heart of the Wound", Type = "Legendary", Tier = 6, AnomPenalty = -0.30f, HpRegen = 100, ImmuneBleed = true, WeightKg = 0.8f, Description = "Heals 100 HP/turn, immune to bleed" },
            ["theLodestone"] = new ArtifactDef { Name = "The Lodestone", Type = "Legendary", Tier = 6, AnomPenalty = -0.30f, PassAllAnomalies = true, WeightKg = 0.8f, Description = "Pass all Anomaly Avoidance checks automatically" },
            ["lucifer"] = new ArtifactDef { Name = "Lucifer", Type = "Legendary", Tier = 6, AnomPenalty = -0.40f, ApBonus = 1, PhysDRBonus = 0.10f, AnomDRBonus = 0.10f, MaxHpPenalty = 250, WeightKg = 0.8f, Description = "+1 AP, +10% Phys/Anom-DR, Max HP -25" },
            ["theBlackStar"] = new ArtifactDef { Name = "The Black Star (Awakened)", Type = "Legendary", Tier = 6, AnomPenalty = -0.35f, BypassPhysDR = 0.30f, KillRefundAp = true, CarryWeightBonus = -10, WeightKg = 0.8f, Description = "Bypasses 30% Phys-DR, kills refund 1 AP, Carry -10kg" },
        };
    }
}
