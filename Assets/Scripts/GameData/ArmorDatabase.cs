// Ported from database.js: ARMOR_DB
using System.Collections.Generic;

namespace GameData
{
    public class ArmorDef
    {
        public string Name;
        public float PhysDR;
        public float AnomDR;
        public int BeltSlots;
        public int ModSlots;
        public int Mp;
        public float? WeightKg;
        public string Note;
        public bool ImmuneToToxic;
        public bool AnomalyAdvantage;
        public bool ImmuneToProne;
        public bool ImmuneToCrits;
        public bool HalvesAPDamage;
    }

    public static partial class ArmorDatabase
    {
        // Keyed exactly like ARMOR_DB in the JS: Tiers["tier1"]["LeatherJacket"]
        public static readonly Dictionary<string, Dictionary<string, ArmorDef>> Tiers =
            new Dictionary<string, Dictionary<string, ArmorDef>>
        {
            ["tier1"] = new Dictionary<string, ArmorDef>
            {
                ["LeatherJacket"] = new ArmorDef { Name = "Leather Jacket", PhysDR = 0.1f, AnomDR = 0.05f, BeltSlots = 1, ModSlots = 1, Mp = 4 },
                ["RaiderJacket"] = new ArmorDef { Name = "Raider Jacket", PhysDR = 0.1f, AnomDR = 0.1f, BeltSlots = 1, ModSlots = 1, Mp = 4, WeightKg = 5f },
                ["HeavyTrenchcoat"] = new ArmorDef { Name = "Heavy Trenchcoat", PhysDR = 0.15f, AnomDR = 0.1f, BeltSlots = 2, ModSlots = 1, Mp = 4 },
                ["RookieFieldSuit"] = new ArmorDef { Name = "Rookie Field Suit", PhysDR = 0.12f, AnomDR = 0.2f, BeltSlots = 2, ModSlots = 1, Mp = 4 },
                ["ScrapPlatedVest"] = new ArmorDef { Name = "Scrap-Plated Vest", PhysDR = 0.2f, AnomDR = 0.05f, BeltSlots = 0, ModSlots = 1, Mp = 3 },
            },
            ["tier2"] = new Dictionary<string, ArmorDef>
            {
                ["LiberatorWindbreaker"] = new ArmorDef { Name = "Liberator Windbreaker", PhysDR = 0.15f, AnomDR = 0.25f, BeltSlots = 3, ModSlots = 2, Mp = 4 },
                ["MarshReconSuit"] = new ArmorDef { Name = "Marsh Recon Suit", PhysDR = 0.2f, AnomDR = 0.3f, BeltSlots = 3, ModSlots = 2, Mp = 4 },
                ["ContractorLight"] = new ArmorDef { Name = "Contractor Light", PhysDR = 0.2f, AnomDR = 0.15f, BeltSlots = 2, ModSlots = 2, Mp = 4 },
                ["DaybreakSuit"] = new ArmorDef { Name = "Daybreak Suit", PhysDR = 0.25f, AnomDR = 0.25f, BeltSlots = 2, ModSlots = 2, Mp = 4 },
                ["RaiderStitchedCoat"] = new ArmorDef { Name = "Raider Stitched Coat", PhysDR = 0.25f, AnomDR = 0.15f, BeltSlots = 1, ModSlots = 2, Mp = 4 },
                ["SentinelServiceSuit"] = new ArmorDef { Name = "Sentinel Service Suit", PhysDR = 0.3f, AnomDR = 0.1f, BeltSlots = 1, ModSlots = 2, Mp = 4 },
                ["NBCSuit"] = new ArmorDef { Name = "NBC Suit", PhysDR = 0.25f, AnomDR = 0.5f, BeltSlots = 1, ModSlots = 1, Mp = 3, Note = "Gas Hood" },
                ["ObeliskPatrol"] = new ArmorDef { Name = "Obelisk Patrol", PhysDR = 0.35f, AnomDR = 0.4f, BeltSlots = 1, ModSlots = 1, Mp = 3 },
            },
            ["tier3"] = new Dictionary<string, ArmorDef>
            {
                ["HZ99SurveySuit"] = new ArmorDef { Name = "HZ-99 Survey Suit", PhysDR = 0.1f, AnomDR = 0.7f, BeltSlots = 4, ModSlots = 1, Mp = 4, ImmuneToToxic = true },
                ["IZCScout"] = new ArmorDef { Name = "IZC Scout", PhysDR = 0.25f, AnomDR = 0.3f, BeltSlots = 3, ModSlots = 2, Mp = 4 },
                ["KokonSuit"] = new ArmorDef { Name = "Kokon Suit", PhysDR = 0.3f, AnomDR = 0.8f, BeltSlots = 3, ModSlots = 3, Mp = 4, Note = "Integrated Thermal" },
                ["LiberatorGuardSuit"] = new ArmorDef { Name = "Liberator Guard Suit", PhysDR = 0.35f, AnomDR = 0.35f, BeltSlots = 3, ModSlots = 2, Mp = 4 },
                ["ContractorACEBase"] = new ArmorDef { Name = "Contractor ACE Base", PhysDR = 0.35f, AnomDR = 0.3f, BeltSlots = 3, ModSlots = 3, Mp = 4 },
                ["ObeliskScientific"] = new ArmorDef { Name = "Obelisk Scientific", PhysDR = 0.35f, AnomDR = 0.6f, BeltSlots = 2, ModSlots = 2, Mp = 4 },
                ["SentinelArmoredSuit"] = new ArmorDef { Name = "Sentinel Armored Suit", PhysDR = 0.45f, AnomDR = 0.15f, BeltSlots = 1, ModSlots = 2, Mp = 3 },
                ["Basalt5"] = new ArmorDef { Name = "Basalt-5", PhysDR = 0.5f, AnomDR = 0.15f, BeltSlots = 1, ModSlots = 2, Mp = 3 },
                ["RaiderVeteranCoat"] = new ArmorDef { Name = "Raider Veteran Coat", PhysDR = 0.35f, AnomDR = 0.2f, BeltSlots = 2, ModSlots = 2, Mp = 4 },
            },
            ["tier4"] = new Dictionary<string, ArmorDef>
            {
                ["HZ99MSurveySuit"] = new ArmorDef { Name = "HZ-99M Survey Suit", PhysDR = 0.2f, AnomDR = 0.8f, BeltSlots = 5, ModSlots = 2, Mp = 4 },
                ["MarshPathfinderSuit"] = new ArmorDef { Name = "Marsh Pathfinder Suit", PhysDR = 0.3f, AnomDR = 0.6f, BeltSlots = 4, ModSlots = 3, Mp = 4 },
                ["ContractorACEScientific"] = new ArmorDef { Name = "Contractor ACE Scientific", PhysDR = 0.4f, AnomDR = 0.45f, BeltSlots = 4, ModSlots = 3, Mp = 4 },
                ["SentinelVanguard"] = new ArmorDef { Name = "Sentinel Vanguard", PhysDR = 0.45f, AnomDR = 0.3f, BeltSlots = 2, ModSlots = 3, Mp = 4 },
                ["IZCOperative"] = new ArmorDef { Name = "IZC Operative", PhysDR = 0.42f, AnomDR = 0.35f, BeltSlots = 3, ModSlots = 3, Mp = 4 },
                ["CartelRadSuit"] = new ArmorDef { Name = "Cartel Rad-Suit", PhysDR = 0.4f, AnomDR = 0.65f, BeltSlots = 4, ModSlots = 2, Mp = 4 },
                ["Granit9AssaultSuit"] = new ArmorDef { Name = "Granit-9 Assault Suit", PhysDR = 0.55f, AnomDR = 0.4f, BeltSlots = 2, ModSlots = 3, Mp = 3, ImmuneToCrits = true },
                ["ProtoExoskeleton"] = new ArmorDef { Name = "Proto-Exoskeleton", PhysDR = 0.6f, AnomDR = 0.2f, BeltSlots = 2, ModSlots = 2, Mp = 3, WeightKg = 20f },
                ["ObeliskHeavy"] = new ArmorDef { Name = "Obelisk Heavy", PhysDR = 0.62f, AnomDR = 0.8f, BeltSlots = 1, ModSlots = 2, Mp = 3 },
                ["RaiderBossCoat"] = new ArmorDef { Name = "Raider Boss Coat", PhysDR = 0.45f, AnomDR = 0.25f, BeltSlots = 3, ModSlots = 2, Mp = 4 },
            },
            ["tier5"] = new Dictionary<string, ArmorDef>
            {
                ["RadiantKokon"] = new ArmorDef { Name = "Radiant Kokon", PhysDR = 0.5f, AnomDR = 0.8f, BeltSlots = 4, ModSlots = 3, Mp = 4, AnomalyAdvantage = true },
                ["LiberatorExoskeleton"] = new ArmorDef { Name = "Liberator Exoskeleton", PhysDR = 0.65f, AnomDR = 0.35f, BeltSlots = 2, ModSlots = 3, Mp = 3 },
                ["WandererExoskeleton"] = new ArmorDef { Name = "Wanderer Exoskeleton", PhysDR = 0.7f, AnomDR = 0.25f, BeltSlots = 2, ModSlots = 2, Mp = 2, WeightKg = 40f, ImmuneToProne = true },
                ["ObeliskExoskeleton"] = new ArmorDef { Name = "Obelisk Exoskeleton", PhysDR = 0.75f, AnomDR = 0.9f, BeltSlots = 1, ModSlots = 2, Mp = 2, WeightKg = 40f, ImmuneToProne = true },
                ["SentinelProtoArmor"] = new ArmorDef { Name = "Sentinel Proto-Armor", PhysDR = 0.85f, AnomDR = 0.2f, BeltSlots = 1, ModSlots = 2, Mp = 2, WeightKg = 50f, HalvesAPDamage = true },
            },
        };

        /// <summary>Mirrors JS's ARMOR_DB[tier][key] lookup pattern. Returns null if not found.</summary>
        public static ArmorDef Get(string tier, string key)
        {
            if (Tiers.TryGetValue(tier, out var group) && group.TryGetValue(key, out var armor))
                return armor;
            return null;
        }
    }
}
