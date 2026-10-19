// Batch 80 (Yan: "something like 'tier 1 rifle ammo / tier 2 rifle ammo' ... tier 5 ammo only in tier 5 maps"; "hollow point
// and armor piercing will need the tiered + per weapon class system as well"; "same tier ammo no damage change"): the
// player's spare rounds are kept per weapon class and tier - a Tier 3 rifle only loads Tier 3 rifle rounds. The kind
// (Standard, Hollow Point, Dart, Armor Piercing) still decides what a round does; the tier never changes damage.
// Pool key: "<class><tier>|<kind>", e.g. "rifle3|HollowPoint". NPC guns keep their own per-weapon reserve (unchanged).
using System.Collections.Generic;
using GameData;

namespace GameCore
{
    public static class Ammo
    {
        static readonly Dictionary<string, string> ClassOf = new Dictionary<string, string>
        {
            ["sidearm"] = "pistol", ["smg"] = "smg", ["shotgun"] = "shotgun", ["assault"] = "rifle", ["sniper"] = "sniper", ["heavy"] = "belt",
        };

        /// <summary>"rifle3" for a Tier 3 assault rifle; null for a blade. The G-7 runs on its own cells.</summary>
        public static string Pool(WeaponDef d)
        {
            if (d == null || string.IsNullOrEmpty(d.AmmoType)) return null;
            if (d.Name == "G-7 Rail Prototype") return "cell";
            return PoolAt(d, d.Tier);
        }

        /// <summary>The same class at another tier (what a trader or a stash of that tier has for this gun).</summary>
        public static string PoolAt(WeaponDef d, int tier) =>
            d == null || string.IsNullOrEmpty(d.AmmoType) ? null : (ClassOf.TryGetValue(CombatRules.Category(d), out var c) ? c : "misc") + System.Math.Max(1, tier);

        public static string Key(WeaponDef d, string kind) => Pool(d) + "|" + (kind ?? "Standard");
        public static string Key(string pool, string kind) => pool + "|" + (kind ?? "Standard");
        public static string Kind(string key) { int i = key.IndexOf('|'); return i < 0 ? key : key.Substring(i + 1); }

        static readonly Dictionary<string, string> KindName = new Dictionary<string, string>
        {
            ["Standard"] = "Standard", ["HollowPoint"] = "Hollow Point", ["Dart"] = "Dart", ["ArmorPiercing"] = "Armor Piercing",
        };

        /// <summary>"T3 rifle rounds (Hollow Point)".</summary>
        public static string Label(string key)
        {
            int i = key.IndexOf('|');
            if (i < 0) return key;
            string pool = key.Substring(0, i), kind = KindName.TryGetValue(Kind(key), out var k) ? k : Kind(key);
            if (pool == "cell") return $"Rail cells ({kind})";
            int t = pool.Length - 1;
            while (t > 0 && char.IsDigit(pool[t - 1])) t--;
            return $"T{pool.Substring(t)} {pool.Substring(0, t)} rounds ({kind})";
        }

        public static int Get(PlayerInventory inv, string key) => inv.AmmoReserve.TryGetValue(key, out var n) ? n : 0;
        public static void Add(PlayerInventory inv, string key, int n) => inv.AmmoReserve[key] = System.Math.Max(0, Get(inv, key) + n);

        /// <summary>A bare kind ("Standard", from an old save or old loot) becomes rounds for the gun in your hands.</summary>
        public static string Normalize(Player p, string key) => key.IndexOf('|') >= 0 ? key : Key(Pool(p.EquippedWeapon?.Db) ?? "pistol1", key);

        /// <summary>Old saves kept one pile per kind: those rounds go to the gun in your hands.</summary>
        public static void Migrate(Player p)
        {
            var pool = Pool(p.EquippedWeapon?.Db) ?? "pistol1";
            foreach (var k in new List<string>(p.Inventory.AmmoReserve.Keys))
                if (k.IndexOf('|') < 0) { int n = p.Inventory.AmmoReserve[k]; p.Inventory.AmmoReserve.Remove(k); Add(p.Inventory, Key(pool, k), n); }
        }
    }
}
