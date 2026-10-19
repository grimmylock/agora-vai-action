// New (file 08 §5): technician upgrades at the Hub's Repair Yard. Up to 3 per weapon or armor
// piece, each option once; they stay on the item (InventoryItem.Upgrades / Weapon.Upgrades /
// Player.EquippedArmorUpgrades, comma lists) through equip, unequip and save. Paid in Mutant
// Parts plus a % of the item's band midpoint.
using System.Collections.Generic;
using System.Linq;

namespace GameCore
{
    public static class Upgrades
    {
        public static readonly Dictionary<string, string> Weapon = new Dictionary<string, string>
        {
            ["Calibrated"] = "+1 to hit",
            ["Reinforced"] = "+3 base damage",
            ["Extended Mag"] = "+50% magazine",
            ["Hardened"] = "wear never makes it jam more (stays at 1%)",
        };
        public static readonly Dictionary<string, string> Armor = new Dictionary<string, string>
        {
            ["Plating"] = "+5% Phys-DR",
            ["Lining"] = "+10% Anom-DR",
            ["Lightened"] = "-3 kg",
            ["Braced"] = "ignores the first Condition loss each fight",
        };

        public static List<string> Parse(string s) =>
            string.IsNullOrEmpty(s) ? new List<string>() : s.Split(',').Where(x => x.Length > 0).ToList();

        public static string Join(IEnumerable<string> l) => string.Join(",", l);

        /// <summary>Repair Yard only (file 08 §5) - quartermasters just repair.</summary>
        public static bool TechnicianHere() => MacroState.Current.CurrentZoneId == "scavengers_hub";

        public static int PartTier(InventoryItem i)
        {
            if (i.Type != "mutantPart") return 0;
            if (int.TryParse(i.Category, out int t)) return t;
            var digits = new string((i.Name ?? "").Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out t) ? t : 1;
        }

        /// <summary>The cheapest parts that pay for upgrade number n (1-3), or null if you can't.</summary>
        public static List<InventoryItem> PartsFor(Player p, int n)
        {
            var parts = p.Inventory.Items.Where(i => i.Type == "mutantPart").SelectMany(i => Enumerable.Repeat(i, System.Math.Max(1, i.Qty)))
                .OrderBy(PartTier).ToList();
            if (n == 1) return parts.Count >= 1 ? parts.Take(1).ToList() : null;
            if (n == 2) { var t3 = parts.Where(i => PartTier(i) >= 3).Take(2).ToList(); return t3.Count == 2 ? t3 : null; }
            var big = parts.FirstOrDefault(i => PartTier(i) >= 4);
            if (big == null) return null;
            var all = new List<InventoryItem> { big };
            var pool = new List<InventoryItem>(parts);
            pool.Remove(big);   // removes one unit - a stack of identical parts is listed once per unit
            all.AddRange(pool.Take(2));
            return all.Count == 3 ? all : null;
        }

        public static string CostText(Player p, InventoryItem item, int n) =>
            $"{(n == 1 ? "1 part" : n == 2 ? "2 parts (T3+)" : "3 parts (one T4+)")} + {RuCost(item, n)} RU";

        public static int RuCost(InventoryItem item, int n) => (int)(Economy.ValueOf(item) * 0.1f * n);

        /// <summary>Buys `upgrade` for the equipped weapon (weapon=true) or armor.</summary>
        public static bool Buy(Player p, bool weapon, string upgrade)
        {
            var item = ShopSystem.EquippedAsItem(weapon);
            if (item == null) return false;
            var have = weapon ? p.EquippedWeapon.Upgrades : p.EquippedArmorUpgrades;
            if (have.Contains(upgrade) || have.Count >= 3) return false;
            int n = have.Count + 1;
            var parts = PartsFor(p, n);
            int ru = RuCost(item, n);
            if (parts == null) { GameLog.Write($"The technician shakes his head: upgrade #{n} needs {CostText(p, item, n)}."); return false; }
            if (p.Ru < ru) { GameLog.Write($"Upgrade #{n} costs {ru} RU - not enough."); return false; }
            p.Ru -= ru;
            foreach (var part in parts) p.RemoveItem(part.Id, 1);
            have.Add(upgrade);
            if (weapon) p.EquippedWeapon.ApplyUpgrades(); else p.ApplyArmorModsForPlayer();
            GameLog.Write($"The technician fits your {item.Name} with {upgrade} ({(weapon ? Weapon : Armor)[upgrade]}). {parts.Count} part{(parts.Count > 1 ? "s" : "")} + {ru} RU.");
            return true;
        }
    }
}
