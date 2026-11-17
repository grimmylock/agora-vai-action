// 2026-11-17 (Yan: "separate them by caliber now, have the actual ammo in the game"): the player's spare rounds are kept per CALIBER and load, not per weapon class
// and tier any more (Batch 80's "rifle3|HollowPoint" is gone: old saves are converted in Migrate). Pool key: "<caliber>|<kind>", e.g. "5.45x39mm|ArmorPiercing";
// the loads of every caliber, what they do and what they cost are in GameData/AmmoDatabase.cs. NPC guns keep their own per-weapon reserve (unchanged).
using System.Collections.Generic;
using System.Linq;
using GameData;

namespace GameCore
{
    public static class Ammo
    {
        /// <summary>The caliber of a gun, as the pool name ("5.45x39mm"); null for a blade.</summary>
        public static string Pool(WeaponDef d) => d == null || string.IsNullOrEmpty(d.AmmoType) ? null : d.AmmoType;

        public static string Key(WeaponDef d, string kind) => Pool(d) + "|" + (kind ?? "Standard");
        public static string Key(string pool, string kind) => pool + "|" + (kind ?? "Standard");
        public static string Kind(string key) { int i = key.IndexOf('|'); return i < 0 ? key : key.Substring(i + 1); }
        public static string Caliber(string key) { int i = key.IndexOf('|'); return i < 0 ? key : key.Substring(0, i); }

        /// <summary>"5.45x39mm Armor-Piercing".</summary>
        public static string Label(string key)
        {
            int i = key.IndexOf('|');
            if (i < 0) return key;
            string cal = key.Substring(0, i), kind = Kind(key);
            string load = AmmoDatabase.Calibers.TryGetValue(cal, out var c) ? c.Find(kind)?.Name ?? kind : kind;
            return cal == "2mm Battery" ? "Rail cells" : $"{cal} {load}";
        }

        /// <summary>The kinds a gun can be loaded with (the loads of its caliber), in order.</summary>
        public static List<string> Kinds(WeaponDef d) => AmmoDatabase.Of(d)?.Loads.Select(l => l.Kind).ToList() ?? new List<string> { "Standard" };

        /// <summary>The player-facing name of the load a weapon is firing ("Armor-Piercing").</summary>
        public static string LoadName(Weapon w) => AmmoDatabase.Load(w?.Db, w?.CurrentAmmoType)?.Name ?? (w?.CurrentAmmoType ?? "Standard");

        public static int Get(PlayerInventory inv, string key) => inv.AmmoReserve.TryGetValue(key, out var n) ? n : 0;
        public static void Add(PlayerInventory inv, string key, int n) => inv.AmmoReserve[key] = System.Math.Max(0, Get(inv, key) + n);

        /// <summary>A bare kind ("Standard", from an old save or old loot) becomes rounds for the gun in your hands.</summary>
        public static string Normalize(Player p, string key) => key.IndexOf('|') >= 0 ? key : Key(Pool(p.EquippedWeapon?.Db) ?? "9x19mm", key);

        static readonly Dictionary<string, string> OldClasses = new Dictionary<string, string> { ["pistol"] = "pistol", ["smg"] = "pistol", ["rifle"] = "rifle", ["sniper"] = "sniper", ["shotgun"] = "shotgun", ["belt"] = "sniper", ["cell"] = "cell" };

        /// <summary>Old saves: one pile per kind (before Batch 80) or per class and tier ("rifle3|HollowPoint", before the caliber split): those rounds go to the caliber of the gun in your hands
        /// when it is of that class, else to the commonest caliber of the class (9x19mm, 5.45x39mm, 7.62x54mmR, 12g, rail cells).</summary>
        public static void Migrate(Player p)
        {
            var inHand = p.EquippedWeapon?.Db; string handClass = AmmoDatabase.Of(inHand)?.Class;
            foreach (var k in new List<string>(p.Inventory.AmmoReserve.Keys))
            {
                string cal = Caliber(k), kind = Kind(k);
                if (k.IndexOf('|') >= 0 && AmmoDatabase.Calibers.ContainsKey(cal)) continue;                  // already a caliber pool
                string cls = k.IndexOf('|') < 0 ? handClass ?? "pistol" : cal.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
                if (cls == "belt" && handClass == "sniper") cls = "sniper";
                string target = (handClass != null && (cls == handClass || (cls == "belt" && handClass == "rifle") || (cls == "smg" && handClass == "pistol"))) ? AmmoDatabase.Of(inHand).Id
                    : AmmoDatabase.DefaultOf(OldClasses.TryGetValue(cls, out var oc) ? oc : "pistol")?.Id ?? "9x19mm";
                int n = p.Inventory.AmmoReserve[k]; p.Inventory.AmmoReserve.Remove(k); Add(p.Inventory, Key(target, kind), n);
            }
        }
    }
}
