// Ported from script.js: attemptInteract(unit).
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class InteractionEngine
    {
        /// <summary>Ported from attemptInteract(unit).</summary>
        public static void AttemptInteract(ICombatant unit)
        {
            if (GameState.Current.CurrentTurn != Turn.Player)
            {
                GameLog.Write("Cannot interact outside your turn.");
                return;
            }
            if (unit.Ap < 2)
            {
                GameLog.Write("Not enough AP to interact.");
                return;
            }

            if (unit is Player storyPlayer && StorySystem.TryInteract(storyPlayer)) return;
            if (unit is Player raider && GarrisonRaid.TryInteract(raider)) return;   // Batch 87
            if (unit is Player synth && G7Lab.TryInteract(synth)) return;            // Batch 88

            var obj = TerrainMap.GetNearbyInteractiveObject(unit);
            if (obj == null)
            {
                GameLog.Write("No interactive object nearby to interact with.");
                return;
            }

            if (Stashes.Blocked(obj)) return;   // Batch 126: locked (key / code), or a code note read
            unit.Ap -= 2;
            var check = SkillCheck.RollSkillCheck(unit, obj.Stat, obj.Dc);

            if (check.Success)
            {
                GameLog.Write($"{obj.Name} successfully interacted with.");
                if (obj.Reward != null)
                {
                    if (obj.Reward.Ru.HasValue && unit is Player player)
                    {
                        player.Ru += obj.Reward.Ru.Value;
                        GameLog.Write($"Gained {obj.Reward.Ru.Value} RU.");
                    }
                    if (obj.Reward.Loot != null && unit is Player p2)
                    {
                        // Fix: "highTierLoot" (ElectronicLock's reward) had no matching entry in
                        // any real database - AddItem was handing the player a generic Type="loot"
                        // stack with no Price, unusable and unsellable for 0 RU. The only "cracked
                        // container has good loot" flavor object in the game gave nothing actually
                        // good. Roll a real weapon or armor piece instead, scaled to the current
                        // zone's tier.
                        if (obj.Reward.Loot.Id == "richStash") { GrantHighTierLoot(p2); GrantContainerLoot(p2); }   // Batch 126
                        else if (obj.Reward.Loot.Id == "highTierLoot")
                            GrantHighTierLoot(p2);
                        else if (obj.Reward.Loot.Id == "containerRolls")
                            GrantContainerLoot(p2);
                        else
                        {
                            p2.AddItem(new InventoryItem
                            {
                                Id = obj.Reward.Loot.Id,
                                Name = obj.Reward.Loot.Name,
                                WeightKg = obj.Reward.Loot.WeightKg,
                                Qty = obj.Reward.Loot.Qty,
                                Type = obj.Reward.Loot.Type,
                            });
                            GameLog.Write($"Recovered loot: {obj.Reward.Loot.Name}.");
                        }
                    }
                    if (obj.Reward.OpenPath)
                    {
                        obj.Opened = true;
                        GameLog.Write("A new path has been opened.");
                    }
                }
                if (obj.GrantsKey != null && unit is Player && MacroState.Current.Keys.Add(obj.GrantsKey))   // the map overhaul: a key for a locked dungeon
                    GameLog.Write($"Under the junk there's a heavy key on a wire tag - it opens the {MacroMapDatabase.Find(obj.GrantsKey)?.Name ?? obj.GrantsKey}.");
                TerrainMap.MarkOpened(obj);   // Batch 124: it stays, open and empty
                TerrainMap.InteractiveObjects.Remove(obj);
            }
            else
            {
                GameLog.Write($"{obj.Name} interaction failed.");
                // Batch 51: a failure costs what that object would really cost - a lock's alarm or the clang of forcing
                // metal carries (Noise) - instead of a 25% chance of 1-5 damage from anything.
                if (obj.Name.Contains("Lock") || obj.Name.Contains("Safe")) { GameLog.Write("The lock shrieks an alarm."); Noise.Make(obj.X, obj.Y, 20, unit); }
                else if (System.Array.IndexOf(obj.Stat ?? new string[0], "Strength") >= 0) { GameLog.Write("Metal clangs as it holds."); Noise.Make(obj.X, obj.Y, 10, unit); }
            }
        }

        /// <summary>New: rolls a real weapon or armor item for "highTierLoot" rewards, reusing
        /// ShopDatabase's auto-generated weapon/armor entries so the granted item carries a real
        /// id/price/category/key - the same shape EquipWeaponFromInventory/EquipArmorFromInventory
        /// (Player.Equipment.cs) expect. Prefers items near the current zone's tier, falling back
        /// to any tier of that type if none match (e.g. a Tier 1 zone still has *some* weapon to
        /// give, even if nothing is exactly tier 0-1).</summary>
        /// <summary>Footlocker / stash / container: 3-6 rolls on the zone's table (file 07).</summary>
        private static void GrantContainerLoot(Player p)
        {
            int tier = MacroMapDatabase.Find(MacroState.Current.CurrentZoneId)?.Tier ?? 1;
            int rolls = Random.Range(3, 7);
            if (CombatantUtil.HasPerk(p, "Scavenger")) rolls = System.Math.Max(rolls, Random.Range(3, 7));
            for (int i = 0; i < rolls; i++) GrantRoll(p, Economy.Roll(tier, trader: false));
        }

        private static void GrantRoll(Player p, LootRoll r)
        {
            if (r.Type == "cash" || r.Type == "junk")
            {
                p.Ru += r.Price;
                GameLog.Write(r.Type == "cash" ? $"Found {r.Price} RU." : $"Junk, mostly. {r.Price} loose RU.");
                return;
            }
            p.AddItem(r.ToItem(r.Type == "weapon" || r.Type == "armor" ? ItemCondition.Dirty : null));
            GameLog.Write($"Recovered loot: {r.Name}.");
        }

        /// <summary>Electronic Lock (file 05): one roll on the zone's table, junk and cash rerolled.
        /// Batch 71 (Yan: "the distribution should be based on map tier"): the roll is the tier of the map you're on
        /// (it was at least Tier 3, so a Tier 1 lock paid out Tier 3 gear). The usual drift and lucky finds still apply.</summary>
        private static void GrantHighTierLoot(Player p2)
        {
            int t = MacroMapDatabase.Find(MacroState.Current.CurrentZoneId)?.Tier ?? 1;
            LootRoll r = null;
            for (int i = 0; i < 8 && (r == null || r.Type == "cash" || r.Type == "junk"); i++) r = Economy.Roll(t, trader: false);
            GrantRoll(p2, r);
        }
    }
}
