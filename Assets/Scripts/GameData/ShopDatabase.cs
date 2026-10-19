// Ported from script.js: const SHOP_ITEMS = [...]. Also lived inline in script.js rather than
// database.js — missed in the original data-layer pass, added here for completeness.
//
// Layer 1 baseline stock and prices follow file 05; weapon/armor/detector/consumable entries
// beyond that exist for id lookups and carry file 07 band-midpoint prices (GameCore.Economy).
using System.Collections.Generic;
using System.Linq;

namespace GameData
{
    public class ShopItemDef
    {
        public string Id;
        public string Name;
        public int Price;
        public float WeightKg;
        public string Type; // "consumable" | "ammo" | "backpack" | "mutantPart" | ...
        public string AmmoType;
        public int? Rounds;
        public int? CapacityBonus;
        // New: only set on the auto-generated "weapon"/"armor" entries below - see InventoryItem's
        // matching fields for what these mean.
        public string Category;
        public string Key;
        // New: only set on "weapon"/"armor" entries (from WeaponDef.Tier / the ArmorDatabase tier
        // key) - lets ShopUI cap what a given trader's zone actually stocks, instead of every Safe
        // Zone selling the entire tier-1-through-5 arsenal on day one. Consumables/ammo/backpacks
        // stay untiered (null) - basic survival supplies are assumed to be sold everywhere.
        public int? Tier;
        // New: faction-issue armor (FactionArmorDatabase) - kept in Items so lookups by key keep
        // working (e.g. Player.Equipment putting a swapped-out armor back into the inventory), but
        // ShopUI doesn't put it on general traders' shelves; it comes from that faction's NPCs.
        public bool FactionIssue;
        // New (file 05 "Layer 1"): always in stock at every trader, fixed price. Everything else
        // (weapons, armor, artifacts, mods, detectors, better consumables) is Layer 2 rolled
        // stock (Economy.RollTraderStock) and only kept here so lookups by id keep working.
        public bool Baseline;
        // Rolled stock: the inventory id the buyer actually receives (an artifact's key, a
        // consumable's id...) - the listing's own Id is unique per roll.
        public string GrantId;
    }

    public static class ShopDatabase
    {
        public static readonly List<ShopItemDef> Items = BuildItems();

        private static List<ShopItemDef> BuildItems()
        {
            // Layer 1 - baseline stock (file 05): every trader, unlimited, fixed price. Ammo boxes
            // hold a number of rounds that depends on the weapon you have out (ShopSystem).
            var list = new List<ShopItemDef>
            {
                new ShopItemDef { Id = "basicMedkit", Name = "Basic Medkit", Price = 100, WeightKg = 1.0f, Type = "consumable", Baseline = true },
                new ShopItemDef { Id = "basicStim", Name = "Basic Stim", Price = 120, WeightKg = 0.5f, Type = "consumable", Baseline = true },
                new ShopItemDef { Id = "bandage", Name = "Bandage", Price = 40, WeightKg = 0.2f, Type = "consumable", Baseline = true },
                new ShopItemDef { Id = "radBlockPill", Name = "Rad-Block Pill", Price = 60, WeightKg = 0.2f, Type = "consumable", Baseline = true },
                new ShopItemDef { Id = "mre", Name = "MRE", Price = 50, WeightKg = 0.5f, Type = "consumable", Baseline = true },
                new ShopItemDef { Id = "energyDrink", Name = "Energy Drink", Price = 40, WeightKg = 0.3f, Type = "consumable", Baseline = true },
                new ShopItemDef { Id = "ammo_standard", Name = "Ammo box: Standard", Price = 150, WeightKg = 1.0f, Type = "ammo", AmmoType = "Standard", Baseline = true },
                new ShopItemDef { Id = "ammo_hollowpoint", Name = "Ammo box: Hollow Point", Price = 250, WeightKg = 1.0f, Type = "ammo", AmmoType = "HollowPoint", Baseline = true },
                new ShopItemDef { Id = "ammo_dart", Name = "Ammo box: Dart", Price = 400, WeightKg = 1.0f, Type = "ammo", AmmoType = "Dart", Baseline = true },
                new ShopItemDef { Id = "ammo_ap", Name = "Ammo box: Armor Piercing", Price = 600, WeightKg = 1.0f, Type = "ammo", AmmoType = "ArmorPiercing", Baseline = true },
                new ShopItemDef { Id = "fragGrenade", Name = "Frag Grenade", Price = 250, WeightKg = 0.5f, Type = "consumable", Baseline = true },
                new ShopItemDef { Id = "smokeGrenade", Name = "Smoke Grenade", Price = 150, WeightKg = 0.5f, Type = "consumable", Baseline = true },
                new ShopItemDef { Id = "flashbang", Name = "Flashbang", Price = 200, WeightKg = 0.5f, Type = "consumable", Baseline = true },
                new ShopItemDef { Id = "fieldRepairKit", Name = "Field Repair Kit", Price = 300, WeightKg = 1.0f, Type = "consumable", Baseline = true },
                new ShopItemDef { Id = "backpack_small", Name = "Small Canvas Sack (+10kg)", Price = 80, WeightKg = 0.5f, Type = "backpack", CapacityBonus = 10, Baseline = true },
                new ShopItemDef { Id = "backpack_tourist", Name = "Tourist Frame (+50kg)", Price = 400, WeightKg = 5.0f, Type = "backpack", CapacityBonus = 50 },
            };

            // Layer 2 consumables (Military/Scientific medkits and stims): rolled stock only,
            // listed here for lookups, priced at their file 07 band.
            foreach (var kvp in ConsumableDatabase.All)
            {
                if (list.Any(i => i.Id == kvp.Key)) continue;
                var def = kvp.Value;
                int price = def.Price ?? GameCore.Economy.ValueOf(new GameCore.InventoryItem { Id = def.Id, Name = def.Name, Type = "consumable" });
                list.Add(new ShopItemDef { Id = def.Id, Name = def.Name, Price = price, WeightKg = 0.5f, Type = "consumable" });
            }

            // New: every weapon/armor piece in WeaponDatabase/ArmorDatabase becomes purchasable,
            // generated here instead of hand-typing ~80 entries. Neither database had a canonical
            // price before this (nothing was ever buyable) - ponytail: flat per-tier price
            // estimate, tune per-item later if you want real economy balance.
            foreach (var categoryEntry in GameData.WeaponDatabase.Categories)
            {
                foreach (var weaponEntry in categoryEntry.Value)
                {
                    var def = weaponEntry.Value;
                    list.Add(new ShopItemDef
                    {
                        Id = $"weapon_{categoryEntry.Key}_{weaponEntry.Key}",
                        Name = def.Name,
                        Price = GameCore.Economy.Mid(GameCore.Economy.WeaponBand(def)),
                        WeightKg = def.WeightKg ?? 3f,
                        Type = "weapon",
                        Category = categoryEntry.Key,
                        Key = weaponEntry.Key,
                        Tier = def.Tier,
                    });
                }
            }

            foreach (var tierEntry in GameData.ArmorDatabase.Tiers)
            {
                int tierNum = int.Parse(tierEntry.Key.Replace("tier", ""));
                foreach (var armorEntry in tierEntry.Value)
                {
                    var def = armorEntry.Value;
                    list.Add(new ShopItemDef
                    {
                        Id = $"armor_{tierEntry.Key}_{armorEntry.Key}",
                        Name = def.Name,
                        Price = GameCore.Economy.Mid(GameCore.Economy.ArmorBand(def, tierNum)),
                        WeightKg = def.WeightKg ?? 4f,
                        Type = "armor",
                        Category = tierEntry.Key,
                        Key = armorEntry.Key,
                        Tier = tierNum,
                        FactionIssue = GameData.FactionArmorDatabase.IsFactionIssue(armorEntry.Key),
                    });
                }
            }

            // New (Batch 8): DetectorDatabase had no shop entry at all - Detectors couldn't be
            // bought, so ActiveDetector/PlayerGearUtil.GetDetectorBonus (already read by
            // AnomalyEngine's avoidance roll) had no way to ever be non-null. Tier is a rough
            // estimate from Ru (matching the "tune later" pricing tone already set on weapon/armor
            // gen above), since DetectorDef itself carries no tier.
            foreach (var kvp in GameData.DetectorDatabase.All)
            {
                var def = kvp.Value;
                int tier = def.Ru <= 1000 ? 1 : def.Ru <= 3000 ? 2 : def.Ru <= 10000 ? 4 : 5;
                list.Add(new ShopItemDef
                {
                    Id = $"detector_{kvp.Key}",
                    Name = def.Name,
                    Price = def.Ru,
                    WeightKg = 1.0f,
                    Type = "detector",
                    Key = kvp.Key,
                    Tier = tier,
                });
            }

            return list;
        }

        public static ShopItemDef Find(string id) => Items.FirstOrDefault(i => i.Id == id);
    }
}
