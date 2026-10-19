// Ported from database.js: CONSUMABLES_DB
using System.Collections.Generic;
using System.Linq;

namespace GameData
{
    public class ConsumableDef
    {
        public string Id;
        public string Name;
        public string Type = "consumable";
        public bool IsTravelRation;
        public int? HealingPermanent;
        public int? HealingTemp;
        public int? MpRestore;
        public bool CureBleeding;
        public bool CureFracture;   // real-time fork, RT batch 2: a splint
        public bool CureRadiation;
        public int ApCost;
        public string Description;
        // New: nothing previously let a food/drink item restore Hunger/Thirst through the "Use"
        // action - only the automatic travel-ration burn on crossing zones (ZoneTravel) touched
        // either stat, leaving no way to relieve them while exploring inside a single zone.
        public int? HungerRestore;
        public int? ThirstRestore;
        // New: throwables (file 01) - "frag" / "smoke" / "flash". Using one arms it; the next
        // click on a tile throws it (Player.ThrowAt). Price/weight from the doc.
        public string Throwable;
        public int? Price;
        public bool RepairKit;   // Field Repair Kit: +1 Condition step out of combat, never above Dirty
    }

    public static class ConsumableDatabase
    {
        public static readonly Dictionary<string, ConsumableDef> All = new Dictionary<string, ConsumableDef>
        {
            ["basicMedkit"] = new ConsumableDef { Id = "basicMedkit", Name = "Basic Medkit", HealingPermanent = 20, ApCost = 2, Description = "Restores 20 permanent HP." },
            ["militaryMedkit"] = new ConsumableDef { Id = "militaryMedkit", Name = "Military Medkit", HealingPermanent = 40, ApCost = 2, Description = "Restores 40 permanent HP." },
            ["scientificMedkit"] = new ConsumableDef { Id = "scientificMedkit", Name = "Scientific Medkit", HealingPermanent = 60, ApCost = 2, Description = "Restores 60 permanent HP." },
            ["basicStim"] = new ConsumableDef { Id = "basicStim", Name = "Basic Stim", HealingTemp = 15, ApCost = 2, Description = "Grants 15 temporary HP for 4 turns." },
            ["militaryStim"] = new ConsumableDef { Id = "militaryStim", Name = "Military Stim", HealingTemp = 25, ApCost = 2, Description = "Grants 25 temporary HP for 4 turns." },
            ["scientificStim"] = new ConsumableDef { Id = "scientificStim", Name = "Scientific Stim", HealingTemp = 40, ApCost = 2, Description = "Grants 40 temporary HP for 4 turns." },
            ["splint"] = new ConsumableDef { Id = "splint", Name = "Splint", CureFracture = true, ApCost = 2, Description = "Sets a broken or fractured arm or leg (legs first)." },
            ["bandage"] = new ConsumableDef { Id = "bandage", Name = "Bandage", CureBleeding = true, ApCost = 2, Description = "Stops bleeding." },
            ["radBlockPill"] = new ConsumableDef { Id = "radBlockPill", Name = "Rad-Block Pill", CureRadiation = true, ApCost = 2, Description = "Purges radiation status." },
            ["mre"] = new ConsumableDef { Id = "mre", Name = "MRE", IsTravelRation = true, HealingPermanent = 10, HungerRestore = 4, ThirstRestore = 1, ApCost = 2, Description = "Restores 10 permanent HP, 4 hunger, and 1 thirst." },
            ["fragGrenade"] = new ConsumableDef { Id = "fragGrenade", Name = "Frag Grenade", Throwable = "frag", ApCost = 2, Price = 250, Description = "40 damage to everyone in a 3x3 blast (Agility DC 13 halves it but knocks you prone). Wrecks light cover." },
            ["smokeGrenade"] = new ConsumableDef { Id = "smokeGrenade", Name = "Smoke Grenade", Throwable = "smoke", ApCost = 2, Price = 150, Description = "Blocks line of sight through a 3x3 area for two turns." },
            ["flashbang"] = new ConsumableDef { Id = "flashbang", Name = "Flashbang", Throwable = "flash", ApCost = 2, Price = 200, Description = "Everyone in the 3x3 blast: Agility DC 13 or lose 2 AP next turn." },
            ["fieldRepairKit"] = new ConsumableDef { Id = "fieldRepairKit", Name = "Field Repair Kit", RepairKit = true, ApCost = 0, Price = 300, Description = "Out of combat: +1 Condition step to your weapon (or armor), never above Dirty." },
            ["energyDrink"] = new ConsumableDef { Id = "energyDrink", Name = "Energy Drink", HealingTemp = 10, MpRestore = 1, ApCost = 2, Description = "Restores 10 temporary HP and +1 MP." },
        };

        public static readonly HashSet<string> ValidTravelRations = new HashSet<string>(All.Values.Where(def => def.IsTravelRation).Select(def => def.Id));
    }
}
