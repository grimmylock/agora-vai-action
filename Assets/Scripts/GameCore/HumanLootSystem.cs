// New: human NPC kills previously granted nothing at all - same gap Batch 1 found on the mutant
// side. Ported from the Economy file's "NPC Loot Drops" section: guaranteed weapon w/ a 1d20
// condition roll and partial magazine, plus a 1d10+rank-bonus roll for additional items.
// ponytail: the additional-loot roll uses this project's own WeaponDatabase/ArmorDatabase/
// ConsumableDatabase/AmmoDatabase/ArmorModDatabase at the zone's tier instead of retyping the
// reference file's ~150 hand-curated named items with their own price bands - same shape (roll a
// category, pull a tier-appropriate item), far less duplicate data to keep in sync. Artifacts and
// Detectors are left out of the pool: Artifacts already have their own acquisition path
// (searching a resolved anomaly with F - AnomalyEngine.TrySearchAnomaly) and Detectors are now buyable from
// ShopDatabase (Batch 8) - neither needs a second, redundant human-loot source.
//
// New (Batch 10 - living-world NPCs): loot is no longer fabricated fresh at the moment an NPC dies.
// RollSpawnStash seeds a real per-NPC inventory when the NPC is created; TakeSpoils (Batch 48) lets it grow
// while a squad wanders offscreen (SquadManager.TickSquadMigration). GrantDrop now only finalizes
// what's already sitting in npc.Inventory - it rolls condition on the worn weapon/armor and drops
// them into that same stash for CorpseLootUI to read, rather than handing items straight to the
// player. The one exception is the "drop and run" flee case (includeWornArmor=false): the NPC is
// still alive and running, there's no corpse to loot later, so that path keeps the old
// grant-straight-to-player behavior.
using System;
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class HumanLootSystem
    {
        /// <summary>includeWornArmor=false for the "drop and run" flee case - a fleeing NPC is
        /// still alive and wearing their armor, only their weapon and any loose extras spill out
        /// of their pack in the panic, straight into the player's hands. includeWornArmor=true
        /// (the death-hook call) means there's a real corpse now: instead of granting to the
        /// player, finalize the worn weapon/armor into npc.Inventory alongside whatever
        /// RollSpawnStash/TakeSpoils already put there, so CorpseLootUI has one place to read
        /// the whole stash from.</summary>
        public static void GrantDrop(HumanEnemy npc, Player player, int zoneTier, bool includeWornArmor = true)
        {
            if (npc == null || player == null) return;

            if (!includeWornArmor)
            {
                if (npc.LootGranted) return;
                npc.LootGranted = true;
                GrantWeaponDrop(npc, player);
                for (int i = 0; i < RollExtraCount(npc.Rank); i++)
                    GrantRolledItem(player, zoneTier);
                return;
            }

            // Corpse finalize: tracked separately from LootGranted (flee's "weapon already handed
            // out" flag) so a corpse whose weapon was already flee-looted still gets its armor
            // finalized here instead of the whole step being skipped.
            if (npc.CorpseLootFinalized) return;
            npc.CorpseLootFinalized = true;
            if (!npc.LootGranted) AddWeaponToStash(npc);
            AddArmorToStash(npc);
        }

        /// <summary>New (Batch 10): called once when an NPC is created (EncounterGeneration,
        /// SquadManager.CreateOfflineSquad) - the same extraCount roll GrantDrop's flee branch
        /// uses, just diverted into the NPC's own stash instead of the player's.</summary>
        public static void RollSpawnStash(HumanEnemy npc, int zoneTier)
        {
            if (npc == null) return;
            for (int i = 0; i < RollExtraCount(npc.Rank); i++)
                AddRolledItemToStash(npc, zoneTier);
        }

        /// <summary>New (Batch 10): lightweight A-Life-style scavenging - each offscreen migration
        /// tick, a living squad member has a flat chance to add one rolled item to their stash.
        /// ponytail: flat 15% chance, no destination/travel simulation or pathfinding to actual
        /// InteractiveObjectInstances on the tactical grid - upgrade path is real pathfinding if
        /// the game ever needs NPCs to visibly loot specific world objects.</summary>
        // Batch 48: ScavengeTick (a rolled item out of nowhere, 2% an hour) is gone; squads strip real War Spoils instead
        // (SquadManager.Scavenge). This puts what was in the spoils into one member's pack.
        public static void TakeSpoils(HumanEnemy npc, int zoneTier, int ru)
        {
            if (npc == null || !npc.IsAlive()) return;
            AddRolledItemToStash(npc, zoneTier + 1);           // the spoils' "abandoned gear" is a high-tier roll
            npc.Ru += ru;
        }

        /// <summary>Batch 48: a fleeing NPC lets go of what it's carrying onto the ground where it stood, instead of it
        /// landing in your pack. Their worn armor stays on.</summary>
        public static void DropOnGround(HumanEnemy npc)
        {
            if (npc == null || npc.LootGranted) return;
            npc.LootGranted = true;
            AddWeaponToStash(npc);
            foreach (var item in npc.Inventory.Items.ToList()) { TerrainMap.DropItem(npc.X, npc.Y, item); npc.Inventory.Items.Remove(item); }
            npc.EquippedWeapon = null;
        }

        private static int RollExtraCount(string rank)
        {
            int bonus = RankBonus(rank);
            int d10 = UnityEngine.Random.Range(1, 11);
            if (GameCore.CombatantUtil.HasPerk(Player.Current, "Scavenger")) d10 = Math.Max(d10, UnityEngine.Random.Range(1, 11));
            int roll = Math.Min(10, d10 + bonus);
            if (roll <= 3) return 0;
            if (roll <= 6) return 1;
            if (roll <= 8) return 2;
            if (roll == 9) return 3;
            return 4;
        }

        /// <summary>Public (Batch 5): reused by Weapon.Fire's reputation-loss hook to scale rep
        /// loss by the same rank tiers this loot roll already uses, and by RolledStockSystem for
        /// nothing yet but kept alongside its sibling methods below.</summary>
        public static int RankBonus(string rank)
        {
            switch (rank)
            {
                case "Professional":
                case "Veteran": return 1;
                case "Expert":
                case "Master": return 2;
                case "Legend": return 3;
                default: return 0;
            }
        }

        private static string RollCondition() // 1d20: 1-7 Damaged, 8-14 Dirty, 15-20 Working
        {
            if (Survival.On) return "Working";                                   // Survival: everything they carry is pristine
            int r = UnityEngine.Random.Range(1, 21);
            if (r <= 7) return "Damaged";
            if (r <= 14) return "Dirty";
            return "Working";
        }

        private static void GrantWeaponDrop(HumanEnemy npc, Player player)
        {
            var wdb = npc.EquippedWeapon?.Db;
            if (wdb == null) return;

            string condition = RollCondition();
            int mag = UnityEngine.Random.Range(1, 5) + 1; // 1d4+1

            var (cat, key) = FindWeaponCategoryKey(wdb);
            player.AddItem(new InventoryItem
            {
                Id = "loot_weapon_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                Name = wdb.Name,
                WeightKg = wdb.WeightKg ?? 3f,
                Qty = 1,
                Type = "weapon",
                Price = Economy.Mid(Economy.WeaponBand(wdb)),
                Category = cat,
                Key = key,
                Condition = condition, // New (Batch 5): a real field now, not just flavor text - see ItemCondition.cs
            });
            if (mag > 0) Ammo.Add(player.Inventory, Ammo.Key(wdb, "Standard"), mag);
            GameLog.Write($"{npc.Name} drops their {wdb.Name} ({condition}, {mag} rounds still loaded).");
        }

        /// <summary>New (Batch 10): the death-path twin of GrantWeaponDrop - rolls condition on
        /// the worn weapon and drops it into the corpse's own stash instead of the player's.</summary>
        private static void AddWeaponToStash(HumanEnemy npc)
        {
            var wdb = npc.EquippedWeapon?.Db;
            if (wdb == null) return;

            var (cat, key) = FindWeaponCategoryKey(wdb);
            AddToStash(npc.Inventory, new InventoryItem
            {
                Id = "loot_weapon_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                Name = wdb.Name,
                WeightKg = wdb.WeightKg ?? 3f,
                Qty = 1,
                Type = "weapon",
                Price = Economy.Mid(Economy.WeaponBand(wdb)),
                Category = cat,
                Key = key,
                Condition = RollCondition(),
            });
            // file 08: it holds 1d4+1 rounds of its standard ammo
            if (!string.IsNullOrEmpty(wdb.AmmoType))
                AddToStash(npc.Inventory, new InventoryItem { Id = "ammo_loose_" + Ammo.Key(wdb, "Standard"), Name = Ammo.Label(Ammo.Key(wdb, "Standard")), Type = "ammo", Key = Ammo.Key(wdb, "Standard"), Qty = Survival.On ? System.Math.Max(10, (wdb.MagCapacity ?? wdb.CylinderCapacity ?? 10) * 3) : UnityEngine.Random.Range(1, 5) + 1, WeightKg = 0f });   // Survival: three magazines
        }

        /// <summary>New (Batch 10): the death-path twin of the old GrantArmorDrop - rolls
        /// condition on the worn armor and drops it into the corpse's own stash.</summary>
        private static void AddArmorToStash(HumanEnemy npc)
        {
            var adb = npc.EquippedArmor;
            if (adb == null) return;

            var (cat, key) = FindArmorCategoryKey(adb);
            AddToStash(npc.Inventory, new InventoryItem
            {
                Id = "loot_armor_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                Name = adb.Name,
                WeightKg = adb.WeightKg ?? 4f,
                Qty = 1,
                Type = "armor",
                Price = Economy.Mid(Economy.ArmorBand(adb, ParseTier(cat))),
                Category = cat,
                Key = key,
                Condition = RollCondition(),
            });
        }

        /// <summary>New (Batch 10): same id-based stacking Player.AddItem uses, minus the
        /// carry-weight recompute (NPCs don't have a carry cap) and minus the .Clone() (the item
        /// was just built fresh, nothing else holds a reference to it yet).</summary>
        private static void AddToStash(PlayerInventory inv, InventoryItem item)
        {
            var existing = inv.Items.FirstOrDefault(i => i.Id == item.Id);
            if (existing != null) existing.Qty += item.Qty > 0 ? item.Qty : 1;
            else inv.Items.Add(item);
        }

        private enum LootKind { Consumable, Ammo, Weapon, Armor, ArmorMod, Cash, Scrap }

        private class RolledLoot
        {
            public LootKind Kind;
            public InventoryItem Item;
            public string AmmoType;
            public int Amount;
            public string LogLine;
        }

        /// <summary>New (Batch 10): the d100 roll table GrantRolledItem used to build items
        /// straight onto the player - extracted so both the player-facing grant and the silent
        /// NPC-stash version (AddRolledItemToStash) share one table instead of two copies drifting
        /// apart.</summary>
        /// <summary>One extra-loot item: a roll on file 07's table at the zone's tier (with Tier
        /// Drift and the Tier 5 weapon rule - see Economy.Roll). Junk is 10-50 loose Ru.</summary>
        private static RolledLoot RollLoot(int zoneTier)
        {
            var r = Economy.Roll(zoneTier, trader: false);
            switch (r.Type)
            {
                case "cash":
                    return new RolledLoot { Kind = LootKind.Cash, Amount = r.Price, LogLine = $"Found {r.Price} RU." };
                case "junk":
                    return new RolledLoot { Kind = LootKind.Scrap, Amount = r.Price, LogLine = $"Just junk - and {r.Price} loose RU." };
                case "weapon":
                case "armor":
                    return new RolledLoot { Kind = r.Type == "weapon" ? LootKind.Weapon : LootKind.Armor, Item = r.ToItem(RollCondition()), LogLine = $"Found: {r.Name}." };
                default:
                    return new RolledLoot { Kind = LootKind.Consumable, Item = r.ToItem(), LogLine = $"Found: {r.Name}." };
            }
        }

        private static void GrantRolledItem(Player player, int zoneTier)
        {
            var loot = RollLoot(zoneTier);
            if (loot == null) return;

            switch (loot.Kind)
            {
                case LootKind.Ammo:
                    Ammo.Add(player.Inventory, Ammo.Key(Ammo.PoolAt(player.EquippedWeapon?.Db, zoneTier) ?? "pistol" + zoneTier, loot.AmmoType), loot.Amount);   // Batch 80: rounds of this map's tier
                    break;
                case LootKind.Cash:
                case LootKind.Scrap:
                    player.Ru += loot.Amount;
                    break;
                default:
                    player.AddItem(loot.Item);
                    break;
            }
            GameLog.Write(loot.LogLine);
        }

        /// <summary>New (Batch 10): same roll table as GrantRolledItem, applied silently to an
        /// NPC's own stash (spawn seed / scavenging) instead of the player - no GameLog line,
        /// nothing has happened the player would witness.</summary>
        private static void AddRolledItemToStash(HumanEnemy npc, int zoneTier)
        {
            var loot = RollLoot(zoneTier);
            if (loot == null) return;

            switch (loot.Kind)
            {
                case LootKind.Ammo:
                    Ammo.Add(npc.Inventory, Ammo.Key(Ammo.Pool(npc.EquippedWeapon?.Db) ?? "pistol" + zoneTier, loot.AmmoType), loot.Amount);   // Batch 80: rounds for their own gun
                    break;
                case LootKind.Cash:
                case LootKind.Scrap:
                    npc.Ru += loot.Amount;
                    break;
                default:
                    AddToStash(npc.Inventory, loot.Item);
                    break;
            }
        }

        /// <summary>Public (Batch 10): reused by SquadManager's offscreen "war spoils" drop so a
        /// wiped squad's pooled RU reward scales the same way this loot roll's cash band already does.</summary>
        public static int CashBandForTier(int tier)
        {
            switch (tier)
            {
                case 1: return UnityEngine.Random.Range(50, 201);
                case 2: return UnityEngine.Random.Range(150, 501);
                case 3: return UnityEngine.Random.Range(400, 1201);
                case 4: return UnityEngine.Random.Range(1000, 3001);
                case 5: return UnityEngine.Random.Range(2500, 7001);
                default: return UnityEngine.Random.Range(6000, 15001);
            }
        }

        private static int ParseTier(string tierKey)
        {
            if (string.IsNullOrEmpty(tierKey)) return 1;
            var digits = new string(tierKey.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out var t) ? t : 1;
        }

        /// <summary>Public (Batch 5): reused by RolledStockSystem so the shop's rolled stock and
        /// this loot roll share one lookup instead of two copies drifting apart.</summary>
        public static (string category, string key) FindWeaponCategoryKey(WeaponDef def)
        {
            foreach (var cat in WeaponDatabase.Categories)
                foreach (var kvp in cat.Value)
                    if (kvp.Value == def) return (cat.Key, kvp.Key);
            return (null, null);
        }

        public static (string category, string key) FindArmorCategoryKey(ArmorDef def)
        {
            foreach (var tier in ArmorDatabase.Tiers)
                foreach (var kvp in tier.Value)
                    if (kvp.Value == def) return (tier.Key, kvp.Key);
            return (null, null);
        }
    }
}
