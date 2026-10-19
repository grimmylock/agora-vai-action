// New: mutant kills previously granted nothing at all - ShopDatabase defined a flat 300 Ru
// "mutant_part" sell item but no code path ever dropped one on a kill. Ported from the Economy
// file's "Mutant Parts (Optional Tier Scaling)" table, which this project uses as the default
// (flat 300 was only ever the fallback if you didn't want tier scaling).
using System.Collections.Generic;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class MutantLootSystem
    {
        private static readonly Dictionary<int, (int min, int max)> PartValueByTier = new Dictionary<int, (int, int)>
        {
            [1] = (75, 150),
            [2] = (200, 350),
            [3] = (400, 600),
            [4] = (800, 1200),
            [5] = (1200, 1700),
        };

        /// <summary>Grants one tier-scaled Mutant Part to the player on a mutant kill. Call once,
        /// right when a mutant transitions to dead - Mutant.LootGranted guards against a second
        /// call if something else still fires damage at an already-0-HP target.
        /// ponytail: always exactly 1 part, no 0/2-part GM-discretion cases from the source doc -
        /// add if you want named/tough mutants to drop extra.</summary>
        public static void GrantDrop(Mutant mutant, Player player)
        {
            if (mutant == null || player == null || mutant is HumanEnemy || mutant.LootGranted) return;
            mutant.LootGranted = true;

            int tier = mutant.Db?.Tier ?? 1;
            var (min, max) = PartValueByTier.TryGetValue(tier, out var band) ? band : PartValueByTier[1];
            int price = Random.Range(min, max + 1);
            string id = "mutant_part_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);

            player.AddItem(new InventoryItem { Id = id, Name = $"Mutant Part (Tier {tier})", WeightKg = 2.0f, Qty = 1, Type = "mutantPart", Price = price, Category = tier.ToString() });
            GameLog.Write($"{mutant.Name} drops a Mutant Part worth {price} RU.");
            if (mutant.Named)
            {
                // file 09: a Named mutant drops 2 parts plus one Tier 5 loot roll
                player.AddItem(new InventoryItem { Id = "mutant_part_" + System.Guid.NewGuid().ToString("N").Substring(0, 8), Name = $"Mutant Part (Tier {tier})", WeightKg = 2.0f, Qty = 1, Type = "mutantPart", Price = Random.Range(min, max + 1), Category = tier.ToString() });
                LootRoll r = null;
                for (int k = 0; k < 10 && (r == null || r.Type == "junk"); k++) r = Economy.Roll(5, trader: false);
                if (r.Type == "cash") { player.Ru += r.Price; GameLog.Write($"Something glints in its den: {r.Price} RU."); }
                else { player.AddItem(r.ToItem(ItemCondition.Working)); GameLog.Write($"In its den you find: {r.Name}."); }
            }
        }
    }
}
