// Ported from script.js: buyItem(itemId), modifyAmmo(type, delta).
using System.Linq;
using GameData;

namespace GameCore
{
    public static class ShopSystem
    {
        /// <summary>New: resolves the same trader context ShopUI's Rebuild() derives for display
        /// (named Quartermaster > Wandering Trader > the current zone's own tier) so GetBuyPrice/
        /// GetSellValuePerUnit price against the actual trader standing in front of the player
        /// rather than a zone-agnostic guess. Faction is null (no Friendly discount/bonus) for the
        /// zone-tier and Wandering Trader cases - only a named Quartermaster has one.</summary>
        /// <summary>The pool key of an ammo box: its own caliber and load, or (an old generic rolled box) the caliber of the gun in your hands. Null with no gun out and no caliber on the box.</summary>
        public static string AmmoKey(ShopItemDef it)
        {
            string cal = !string.IsNullOrEmpty(it.Caliber) ? it.Caliber : Ammo.Pool(Player.Current?.EquippedWeapon?.Db);
            if (cal == null) return null;
            var c = AmmoDatabase.Calibers.TryGetValue(cal, out var cc) ? cc : null;
            string kind = c != null && c.Find(it.AmmoType) == null ? "Standard" : it.AmmoType;
            return Ammo.Key(cal, kind);
        }

        /// <summary>The calibers of the guns you carry (hands, second slot, off hand, pack).</summary>
        public static System.Collections.Generic.List<Caliber> OwnedCalibers(Player p)
        {
            var found = new System.Collections.Generic.List<Caliber>();
            void Add(WeaponDef d) { var c = AmmoDatabase.Of(d); if (c != null && !found.Contains(c)) found.Add(c); }
            if (p == null) return found;
            Add(p.EquippedWeapon?.Db); Add(p.SecondaryWeapon?.Db); Add(p.OffhandWeapon?.Db);
            foreach (var it in p.Inventory.Items) if (it.Type == "weapon" && !string.IsNullOrEmpty(it.Category) && WeaponDatabase.Categories.TryGetValue(it.Category, out var cat) && cat.TryGetValue(it.Key ?? "", out var def)) Add(def);
            return found;
        }

        /// <summary>One box of one load of one caliber, as a shop item.</summary>
        public static ShopItemDef AmmoItem(Caliber c, AmmoLoad l) => new ShopItemDef
        {
            Id = "ammo|" + c.Id + "|" + l.Kind, Name = $"{c.Id} {l.Name} x{c.Box}", Type = "ammo", AmmoType = l.Kind, Caliber = c.Id, Rounds = c.Box, WeightKg = 0.02f * c.Box, Baseline = true,
        };

        /// <summary>What every trader of this tier sells in ammo: for each caliber you carry a gun for, the loads whose tier the map has reached.</summary>
        public static System.Collections.Generic.List<ShopItemDef> AmmoStock(Player p, int tier)
        {
            var list = new System.Collections.Generic.List<ShopItemDef>();
            foreach (var c in OwnedCalibers(p)) foreach (var l in c.Loads) if (l.Tier <= tier) list.Add(AmmoItem(c, l));
            return list;
        }

        private static (int tier, string faction) CurrentTraderContext()
        {
            var zone = MacroMapDatabase.Find(MacroState.Current.CurrentZoneId);
            var qm = zone != null ? QuartermasterDatabase.Find(zone.Id) : null;
            if (qm != null) return (qm.MaxTier, qm.Faction);
            if (zone != null && MacroState.Current.WanderingTraderZoneId == zone.Id)
                return (MacroState.Current.WanderingTraderTier, null);
            return (zone?.Tier ?? 1, null);
        }

        /// <summary>New: buy price used to be the raw ShopDatabase.Price - the source's ±10-30%
        /// remoteness/reputation swing (ReputationPricing.cs, see its header note) never actually
        /// reached a transaction. Remoteness always applies (goods cost more the further out you
        /// are); the Friendly discount only applies at a named Quartermaster (a faction with an
        /// opinion of you) - the zone-tier/Wandering Trader fallback has no faction to be Friendly
        /// with.</summary>
        public static int GetBuyPrice(ShopItemDef item)
        {
            if (item == null) return 0;
            var (tier, faction) = CurrentTraderContext();
            float repMult = !string.IsNullOrEmpty(faction) && Player.Current != null
                ? ReputationPricing.FriendlyMultiplier(Player.Current.GetRepLevel(faction), isSelling: false)
                : 1f;
            float haggle = CombatantUtil.HasPerk(Player.Current, "Haggler") ? 0.85f : 1f;
            if (StorySystem.ContainPrices && Player.Current != null) repMult = ReputationPricing.FriendlyMultiplier("Friendly", isSelling: false);
            haggle *= StorySystem.PriceMultiplier;
            float basePrice = item.Type == "ammo" ? AmmoBoxPrice(item) : item.Price;
            return (int)(basePrice * ReputationPricing.RemotenessMarkup(tier) * repMult * haggle);
        }

        // 2026-11-17: a box costs its load's price per round times the rounds in it (AmmoDatabase), before the remoteness markup, Haggler and reputation. An old generic rolled box is priced as the load of the
        // gun in your hands (a box of 20 when it does not say).
        public static float AmmoBoxPrice(ShopItemDef it)
        {
            string key = AmmoKey(it);
            if (key == null || !AmmoDatabase.Calibers.TryGetValue(Ammo.Caliber(key), out var c)) return 150f;
            var load = c.Find(Ammo.Kind(key)) ?? c.Standard;
            return load.RoundPrice * (it.Rounds ?? c.Box);
        }

        /// <summary>New: sell value used to be flat item.Price with no remoteness/reputation swing
        /// (same gap as GetBuyPrice) and no condition penalty despite ItemCondition.SellMultiplier
        /// existing specifically for this (see its header note - "the sell-back price multiplier
        /// is the one place all three [condition] tiers differ"). Condition only matters for
        /// weapons/armor, same guard CorpseLootUI/InventoryUI already use for the condition
        /// display suffix.</summary>
        public static int GetSellValuePerUnit(InventoryItem item)
        {
            if (item == null) return 0;
            var (tier, faction) = CurrentTraderContext();
            float repMult = !string.IsNullOrEmpty(faction) && Player.Current != null
                ? ReputationPricing.FriendlyMultiplier(Player.Current.GetRepLevel(faction), isSelling: true)
                : 1f;
            float conditionMult = (item.Type == "weapon" || item.Type == "armor") ? ItemCondition.SellMultiplier(item.Condition) : 1f;
            // File 08: sell price = the item's buy price x the category's sell-back % x condition.
            float haggle = CombatantUtil.HasPerk(Player.Current, "Haggler") ? 1.15f : 1f;
            return (int)(Economy.ValueOf(item) * Economy.SellBack(item.Type) * ReputationPricing.RemotenessMarkup(tier) * repMult * conditionMult * haggle);
        }

        /// <summary>Ported from buyItem(itemId).</summary>
        public static void BuyItem(string itemId)
        {
            var it = ShopDatabase.Find(itemId);
            if (it == null && itemId != null && itemId.StartsWith("ammo|"))                       // a box generated for a caliber you carry (AmmoStock)
            {
                var parts = itemId.Split('|');
                if (parts.Length == 3 && AmmoDatabase.Calibers.TryGetValue(parts[1], out var cal) && cal.Find(parts[2]) is AmmoLoad load) it = AmmoItem(cal, load);
            }
            if (it == null) return;
            ApplyPurchase(it);
        }

        /// <summary>New: RolledStockSystem's Layer 2 "special stock" items are freshly rolled
        /// per zone entry with generated ids (see RolledStockSystem.RollOne) - they never live in
        /// ShopDatabase, so BuyItem's ShopDatabase.Find(itemId) lookup can't reach them. Same
        /// purchase logic via ApplyPurchase, plus removing the one-off item from
        /// MacroState.Current.RolledShopStock so it can't be bought twice off the same roll.</summary>
        public static void BuyRolledItem(string zoneId, ShopItemDef item)
        {
            if (item == null || string.IsNullOrEmpty(zoneId)) return;
            if (!MacroState.Current.RolledShopStock.TryGetValue(zoneId, out var list) || !list.Contains(item)) return;
            if (!ApplyPurchase(item)) return;
            list.Remove(item);
        }

        /// <summary>Shared by BuyItem and BuyRolledItem: charges GetBuyPrice(item) and applies it
        /// to the player's inventory/ammo/backpack. Returns false (no charge, no log) if the
        /// player can't afford it.</summary>
        private static bool ApplyPurchase(ShopItemDef it)
        {
            var player = Player.Current;
            int price = GetBuyPrice(it);
            if (it.Type == "ammo" && AmmoKey(it) == null) { GameLog.Write("Draw the gun you want rounds for first."); return false; }
            if (player.Ru < price)
            {
                GameLog.Write($"Not enough RU to buy {it.Name}");
                return false;
            }

            player.Ru -= price;

            if (it.Type == "ammo")
            {
                string key = AmmoKey(it);
                int rounds = it.Rounds ?? RoundsPerBox(player.EquippedWeapon);
                Ammo.Add(player.Inventory, key, rounds);
                GameLog.Write($"Bought {rounds} {Ammo.Label(key)}.");
            }
            else if (it.Type == "backpack")
            {
                player.HandleBackpackChange(it.GrantId ?? it.Id);   // keeps the old pack and the armor-mod carry bonus
                GameLog.Write($"Equipped backpack: {it.Name}. Capacity +{it.CapacityBonus ?? 0}kg.");
            }
            else
            {
                player.AddItem(new InventoryItem { Id = it.GrantId ?? it.Id, Name = it.Name, WeightKg = it.WeightKg, Qty = 1, Type = it.Type, Price = it.Price, Category = it.Category, Key = it.Key });
                GameLog.Write($"Bought {it.Name} and added to inventory.");
            }

            player.ApplyCarryEffects();
            return true;
        }

        /// <summary>Rounds in one ammo box, by the weapon category it's bought for (file 01).</summary>
        public static int RoundsPerBox(Weapon w)
        {
            switch (CombatRules.Category(w?.Db))
            {
                case "smg": return 30;
                case "shotgun": return 8;
                case "assault": return 15;
                case "sniper": return 5;
                case "heavy": return 25;
                default: return 18;   // sidearm (and anything without a gun out)
            }
        }

        /// <summary>A technician (the Hub's Repair Yard, or any faction quartermaster) is here.</summary>
        public static bool TechnicianHere()
        {
            string zone = MacroState.Current.CurrentZoneId;
            return zone == "scavengers_hub" || QuartermasterDatabase.Find(zone) != null;
        }

        /// <summary>Repair to Working at a technician: 10% of the band midpoint per step (file 08 §5).</summary>
        public static bool Repair(InventoryItem item)
        {
            var p = Player.Current;
            int cost = Economy.RepairCost(item);
            if (cost <= 0) return false;
            if (p.Ru < cost) { GameLog.Write($"Repairing {item.Name} costs {cost} RU - not enough."); return false; }
            p.Ru -= cost;
            item.Condition = ItemCondition.Working;
            GameLog.Write($"The technician fixes up your {item.Name} for {cost} RU. Good as new.");
            return true;
        }

        /// <summary>The equipped weapon or armor as an InventoryItem (for pricing repairs/upgrades).</summary>
        public static InventoryItem EquippedAsItem(bool weapon)
        {
            var p = Player.Current;
            if (weapon && p?.EquippedWeapon != null)
                return new InventoryItem { Type = "weapon", Name = p.EquippedWeapon.Name, Condition = p.EquippedWeapon.Condition, Category = p.EquippedWeaponCategory, Key = p.EquippedWeaponKey };
            if (!weapon && p?.EquippedArmor != null)
                return new InventoryItem { Type = "armor", Name = p.EquippedArmor.Name, Condition = p.EquippedArmorCondition, Category = p.EquippedArmorTier, Key = p.EquippedArmorKey };
            return null;
        }

        /// <summary>Repairs what you're carrying in your hands / on your back.</summary>
        public static void RepairEquipped(bool weapon)
        {
            var p = Player.Current;
            var stub = EquippedAsItem(weapon);
            if (stub == null || !Repair(stub)) return;
            if (weapon) p.EquippedWeapon.Condition = ItemCondition.Working;
            else { p.EquippedArmorCondition = ItemCondition.Working; p.ApplyArmorModsForPlayer(); }
        }

        /// <summary>Ported from modifyAmmo(type, delta).</summary>
        public static void ModifyAmmo(string type, int delta)
        {
            var player = Player.Current;
            int current = player.Inventory.AmmoReserve.TryGetValue(type, out var c) ? c : 0;
            player.Inventory.AmmoReserve[type] = System.Math.Max(0, current + delta);
        }

        /// <summary>New: BuyItem existed but nothing let the player sell loot back - a shop that
        /// only sells isn't a usable economy loop for a loot-based survival game. Sells through
        /// GetSellValuePerUnit (remoteness/reputation/condition-aware) rather than flat Price.
        /// Items with no Price set (most InteractionEngine loot rewards) sell for 0.</summary>
        public static bool SellItem(string itemId, int qty = 1)
        {
            var player = Player.Current;
            var item = player.Inventory.Items.FirstOrDefault(i => i.Id == itemId);
            if (item == null) return false;
            if (item.Type == "key") { GameLog.Write($"Nobody will buy the {item.Name} - and you shouldn't sell it."); return false; }

            int sellQty = System.Math.Min(qty, item.Qty);
            int total = GetSellValuePerUnit(item) * sellQty;
            if (!player.RemoveItem(itemId, sellQty)) return false;

            player.Ru += total;
            GameLog.Write($"Sold {sellQty}x {item.Name} for {total} RU.");
            return true;
        }
    }
}
