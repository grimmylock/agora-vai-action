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
        public bool CureBleeding;
        public bool CureFracture;   // real-time fork, RT batch 2: a splint
        public bool CureRadiation;
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
            ["basicMedkit"] = new ConsumableDef { Id = "basicMedkit", Name = "Basic Medkit", HealingPermanent = 200, Description = "Restores 200 permanent HP." },
            ["militaryMedkit"] = new ConsumableDef { Id = "militaryMedkit", Name = "Military Medkit", HealingPermanent = 400, Description = "Restores 400 permanent HP." },
            ["scientificMedkit"] = new ConsumableDef { Id = "scientificMedkit", Name = "Scientific Medkit", HealingPermanent = 600, Description = "Restores 600 permanent HP." },
            ["basicStim"] = new ConsumableDef { Id = "basicStim", Name = "Basic Stim", HealingTemp = 150, Description = "Grants 150 temporary HP for about 24 seconds." },
            ["militaryStim"] = new ConsumableDef { Id = "militaryStim", Name = "Military Stim", HealingTemp = 250, Description = "Grants 250 temporary HP for about 24 seconds." },
            ["scientificStim"] = new ConsumableDef { Id = "scientificStim", Name = "Scientific Stim", HealingTemp = 400, Description = "Grants 400 temporary HP for about 24 seconds." },
            ["splint"] = new ConsumableDef { Id = "splint", Name = "Splint", CureFracture = true, Description = "Sets a broken or fractured arm or leg (legs first)." },
            ["bandage"] = new ConsumableDef { Id = "bandage", Name = "Bandage", CureBleeding = true, Description = "Stops bleeding." },
            ["radBlockPill"] = new ConsumableDef { Id = "radBlockPill", Name = "Rad-Block Pill", CureRadiation = true, Description = "Purges radiation status." },
            ["mre"] = new ConsumableDef { Id = "mre", Name = "MRE", IsTravelRation = true, HealingPermanent = 100, HungerRestore = 4, ThirstRestore = 1, Description = "Restores 100 permanent HP, 4 hunger, and 1 thirst." },
            ["fragGrenade"] = new ConsumableDef { Id = "fragGrenade", Name = "Frag Grenade", Throwable = "frag", Price = 250, Description = "400 damage at the centre of a 3x3 blast, half at the edge; everyone caught is knocked down. Wrecks light cover. It flies, then burns a short fuse: run or dive." },
            ["smokeGrenade"] = new ConsumableDef { Id = "smokeGrenade", Name = "Smoke Grenade", Throwable = "smoke", Price = 150, Description = "Blocks line of sight through a 3x3 area for about 12 seconds." },
            ["flashbang"] = new ConsumableDef { Id = "flashbang", Name = "Flashbang", Throwable = "flash", Price = 200, Description = "Everyone within 4 tiles who can see it is blinded for 3 seconds (no damage)." },
            ["fieldRepairKit"] = new ConsumableDef { Id = "fieldRepairKit", Name = "Field Repair Kit", RepairKit = true, Price = 300, Description = "Out of combat: +1 Condition step to your weapon (or armor), never above Dirty." },
            ["energyDrink"] = new ConsumableDef { Id = "energyDrink", Name = "Energy Drink", HealingTemp = 100, Description = "Restores 100 temporary HP." },
        };

        public static readonly HashSet<string> ValidTravelRations = new HashSet<string>(All.Values.Where(def => def.IsTravelRation).Select(def => def.Id));
    }
}
