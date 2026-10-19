// New (Batch 5): Layer 2 of the two-layer shop system described in the Gear/Perks reference file -
// Layer 1 is ShopDatabase's flat always-available catalog (already tier-capped per trader in
// ShopUI); this is the d100-rolled "exciting" stock layered on top, re-rolled per zone entry (see
// ZoneTravel.RollShopStock) at one tier above what the trader's baseline caps out at.
// ponytail: doesn't reproduce the reference file's exact named item pools/price bands - same call
// HumanLootSystem already made for NPC loot drops. Rolls a category, then pulls a tier-appropriate
// item from this project's own databases (reusing HumanLootSystem's category lookups), so there's
// one source of truth instead of retyping ~150 hand-curated entries.
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class RolledStockSystem
    {
        /// <summary>2-4 special items at (traderTier, traderTier+1) - always at least a shot at
        /// gear one tier above what the trader normally stocks.</summary>
        public static List<ShopItemDef> Roll(int traderTier)
        {
            int count = 2 + Random.Range(0, 3); // 2-4
            var list = new List<ShopItemDef>();
            for (int i = 0; i < count; i++)
            {
                var item = RollOne(traderTier);
                if (item != null) list.Add(item);
            }
            return list;
        }

        private static ShopItemDef RollOne(int traderTier)
        {
            int minTier = System.Math.Max(1, traderTier);
            int maxTier = minTier + 1;
            int roll = Random.Range(1, 101); // d100

            if (roll <= 35) // weapon
            {
                var w = EncounterGeneration.PickRandomWeaponByTier(minTier, maxTier);
                if (w == null) return null;
                var (cat, key) = HumanLootSystem.FindWeaponCategoryKey(w);
                return new ShopItemDef
                {
                    Id = "roll_w_" + System.Guid.NewGuid().ToString("N").Substring(0, 8),
                    Name = w.Name, Price = (int)(w.Tier * 150 * 1.25f), WeightKg = w.WeightKg ?? 3f,
                    Type = "weapon", Category = cat, Key = key, Tier = w.Tier,
                };
            }
            if (roll <= 55) // armor
            {
                var a = EncounterGeneration.PickRandomArmorByTier(minTier, maxTier, includeFactionIssue: false);
                if (a == null) return null;
                var (cat, key) = HumanLootSystem.FindArmorCategoryKey(a);
                int tierNum = ParseTierKey(cat);
                return new ShopItemDef
                {
                    Id = "roll_a_" + System.Guid.NewGuid().ToString("N").Substring(0, 8),
                    Name = a.Name, Price = (int)(tierNum * 200 * 1.25f), WeightKg = a.WeightKg ?? 4f,
                    Type = "armor", Category = cat, Key = key, Tier = tierNum,
                };
            }
            if (roll <= 75) // specialty ammo box
            {
                string[] types = { "HollowPoint", "ArmorPiercing" };
                string t = types[Random.Range(0, types.Length)];
                return new ShopItemDef
                {
                    Id = "roll_ammo_" + t, Name = $"Ammo Box ({t}) x20", Price = 220,
                    WeightKg = 1f, Type = "ammo", AmmoType = t, Rounds = 20,
                };
            }
            if (roll <= 90) // armor mod
            {
                var pick = ArmorModDatabase.All.Values.ElementAtOrDefault(Random.Range(0, ArmorModDatabase.All.Count));
                if (pick == null) return null;
                return new ShopItemDef
                {
                    Id = "roll_mod_" + System.Guid.NewGuid().ToString("N").Substring(0, 8),
                    Name = pick.Name, Price = 350, WeightKg = 0.3f, Type = "armorMod",
                };
            }
            // upgraded consumable
            var con = ConsumableDatabase.All.Values.ElementAtOrDefault(Random.Range(0, ConsumableDatabase.All.Count));
            if (con == null) return null;
            return new ShopItemDef
            {
                Id = "roll_con_" + System.Guid.NewGuid().ToString("N").Substring(0, 8),
                Name = con.Name, Price = 90, WeightKg = 0.5f, Type = "consumable",
            };
        }

        private static int ParseTierKey(string tierKey)
        {
            if (string.IsNullOrEmpty(tierKey)) return 1;
            var digits = new string(tierKey.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out var t) ? t : 1;
        }
    }
}
