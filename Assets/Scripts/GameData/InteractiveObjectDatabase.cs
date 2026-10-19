// Ported from database.js: INTERACTIVE_OBJECT_DB
using System.Collections.Generic;

namespace GameData
{
    public class LootItem
    {
        public string Id;
        public string Name;
        public float WeightKg;
        public int Qty;
        public string Type;
    }

    public class InteractiveReward
    {
        public int? Ru;
        public LootItem Loot;
        public bool OpenPath;
    }

    public class InteractiveObjectDef
    {
        public string Name;
        // JS had either a single string ("Intelligence") or an array (["Agility","Intelligence"])
        // for `stat`. Represented uniformly as an array here; single-stat entries just have length 1.
        public string[] Stat;
        public int Dc;
        public InteractiveReward Reward;
        public string Description;
    }

    public static class InteractiveObjectDatabase
    {
        public static readonly Dictionary<string, InteractiveObjectDef> All = new Dictionary<string, InteractiveObjectDef>
        {
            ["EncryptedPDA"] = new InteractiveObjectDef
            {
                Name = "Encrypted PDA",
                Stat = new[] { "Intelligence" },
                Dc = 14,
                Reward = new InteractiveReward { Ru = 120 },
                Description = "Requires an Intelligence check to extract intel and credits.",
            },
            ["Footlocker"] = new InteractiveObjectDef
            {
                Name = "Footlocker",
                Stat = new[] { "Strength", "Agility" },
                Dc = 10,
                Reward = new InteractiveReward { Loot = new LootItem { Id = "containerRolls", Name = "Container", WeightKg = 0f, Qty = 1, Type = "loot" } },
                Description = "A locked footlocker or stash box. Force it (Strength) or pick it (Agility).",
            },
            ["ElectronicLock"] = new InteractiveObjectDef
            {
                Name = "Electronic Lock",
                Stat = new[] { "Agility", "Intelligence" },
                Dc = 15,
                Reward = new InteractiveReward
                {
                    Ru = 180,
                    Loot = new LootItem { Id = "highTierLoot", Name = "High-Tier Loot", WeightKg = 2.0f, Qty = 1, Type = "loot" },
                },
                Description = "Requires Agility or Intelligence to bypass and reveal high-tier loot.",
            },
            ["RustedBlastDoor"] = new InteractiveObjectDef
            {
                Name = "Rusted Blast Door",
                Stat = new[] { "Strength" },
                Dc = 16,
                Reward = new InteractiveReward { OpenPath = true },
                Description = "Requires Strength to force the door open.",
            },
        };
    }
}
