// 2026-11-17 (Yan: "separate them by caliber now, have the actual ammo in the game ... then we can balance using ammo too"; grades per caliber, tiers dropped).
// Every caliber is its own ammunition and has its own list of loads. A weapon's WeaponDef.AmmoType is its caliber; the loads (Kind ids "Standard", "HollowPoint",
// "ArmorPiercing", "Dart" are kept for saves and NPC data) change what a round does: Damage multiplies the damage, DrMult multiplies the target's Phys-DR (below 1 =
// pierces armor), Hollow is the hollow-point rule (more damage on the unarmored, less on armor of 20%+). Tier = the first map tier whose traders and loot carry the load.
// Standard is the baseline every gun was tuned against (damage 1.0, DR x1.0 for every caliber: a gun that pierces by itself, the Corvan 5.7, SR-21, RK-12, Grom, Sledge, G-7..., has its own
// WeaponDef.IgnorePhysDRPercent, which Gunplay.Resolve applies); the other loads are the balance lever.
using System.Collections.Generic;
using System.Linq;

namespace GameData
{
    public class AmmoLoad
    {
        public string Kind, Name, Note;
        public float Damage = 1f, DrMult = 1f;
        public bool Hollow;
        public int Tier = 1;
        public float RoundPrice;          // RU a round
    }

    public class Caliber
    {
        public string Id, Class;          // Class: pistol, smg, rifle, sniper, shotgun, belt, cell (what the old pools were called)
        public int Box;                   // rounds in one box
        public List<AmmoLoad> Loads = new List<AmmoLoad>();
        public AmmoLoad Standard => Loads[0];
        public AmmoLoad Find(string kind) => Loads.FirstOrDefault(l => l.Kind == kind);
    }

    public static class AmmoDatabase
    {
        static AmmoLoad Ball(string name, float price, float dr = 1f, float dmg = 1f) => new AmmoLoad { Kind = "Standard", Name = name, RoundPrice = price, DrMult = dr, Damage = dmg };
        static AmmoLoad HP(float price, string name = "Hollow Point", float dmg = 1f, int tier = 1) => new AmmoLoad { Kind = "HollowPoint", Name = name, RoundPrice = price * 1.5f, Hollow = true, Damage = dmg, Tier = tier };
        static AmmoLoad AP(float price, int tier = 3, string name = "Armor-Piercing", float dmg = 0.95f, float dr = 0.5f) => new AmmoLoad { Kind = "ArmorPiercing", Name = name, RoundPrice = price * 2.5f, DrMult = dr, Damage = dmg, Tier = tier };
        static AmmoLoad Dart(float price, int tier = 4) => new AmmoLoad { Kind = "Dart", Name = "Sabot Dart", RoundPrice = price * 2f, DrMult = 0.4f, Damage = 0.85f, Tier = tier };

        /// <summary>A shotgun slug (2026-11-17): one heavy projectile instead of the pellets (Gunplay: no spread cone, tight aim, passes through 2 bodies, loses damage from 15 tiles on),
        /// +35% damage and it pierces some armor (x0.7). Where buckshot is the close-range load, the slug is the answer to armor and to range.</summary>
        static AmmoLoad Slug(float price, int tier = 2) => new AmmoLoad { Kind = "Slug", Name = "Slug", RoundPrice = price * 1.8f, Damage = 1.35f, DrMult = 0.7f, Tier = tier };

        static Caliber C(string id, string cls, int box, params AmmoLoad[] loads) => new Caliber { Id = id, Class = cls, Box = box, Loads = loads.ToList() };

        public static readonly Dictionary<string, Caliber> Calibers = new[]
        {
            // pistol and sub-gun calibers
            C(".32 ACP",     "pistol", 20, Ball("Ball", 3), HP(3)),
            C("9x18mm",      "pistol", 20, Ball("Ball", 4), HP(4), AP(4, 3, "Armor-Piercing", 0.9f, 0.7f)),
            C("9x19mm",      "pistol", 20, Ball("Ball", 5), HP(5), AP(5, 3, "Armor-Piercing", 0.95f, 0.65f)),
            C("9x21mm",      "pistol", 20, Ball("Ball", 6), HP(6), AP(6, 3, "Armor-Piercing", 0.95f, 0.65f)),
            C(".45 ACP",     "pistol", 20, Ball("Ball", 6), HP(6), AP(6, 3, "Armor-Piercing", 0.95f, 0.65f)),
            C("5.7x28mm",    "pistol", 30, Ball("Ball", 9), HP(9), AP(9, 4, "Armor-Piercing", 0.9f, 0.65f)),
            C("4.6x30mm",    "pistol", 30, Ball("Ball", 9), HP(9), AP(9, 4, "Armor-Piercing", 0.9f, 0.6f)),
            C(".357 Mag",    "pistol", 12, Ball("Soft Point", 8), HP(8), AP(8, 3, "Hard-Cast", 1.0f, 0.7f)),
            C(".44 Mag",     "pistol", 12, Ball("Soft Point", 12), HP(12), AP(12, 3, "Hard-Cast", 1.0f, 0.7f)),
            C("12.7x55mm",   "pistol", 10, Ball("Ball", 25), AP(25, 4, "Armor-Piercing", 1.0f, 0.65f)),
            // rifle calibers
            C("5.45x39mm",   "rifle",  30, Ball("Ball", 6), HP(6), AP(6), Dart(6)),
            C("5.56x45mm",   "rifle",  30, Ball("Ball", 7), HP(7), AP(7), Dart(7)),
            C("7.62x39mm",   "rifle",  30, Ball("Ball", 7), HP(7), AP(7), Dart(7)),
            C("7.62x51mm",   "rifle",  20, Ball("Ball", 9), HP(9), AP(9)),
            C("9x39mm",      "rifle",  20, Ball("Subsonic", 10), HP(10, "Soft Point"), AP(10, 3)),
            // sniper and heavy calibers
            C("7.62x54mmR",  "sniper", 20, Ball("Ball", 12), HP(12), AP(12)),
            C(".45-70 Govt", "sniper", 10, Ball("Ball", 14), HP(14, "Soft Point")),
            C(".50 BMG",     "sniper", 10, Ball("Ball", 35), AP(35, 4, "Armor-Piercing", 1.0f, 0.45f)),
            // shotgun
            C("12g",         "shotgun", 10, Ball("Buckshot", 8), HP(8, "Birdshot"), Slug(8), AP(8, 3, "Flechette", 0.9f, 0.5f)),
            // the G-7
            C("2mm Battery", "cell",    5, Ball("Cell", 60)),
        }.ToDictionary(c => c.Id);

        /// <summary>The caliber of a gun (its WeaponDef.AmmoType); null for blades and unknown strings.</summary>
        public static Caliber Of(WeaponDef d) => d == null || string.IsNullOrEmpty(d.AmmoType) ? null : (Calibers.TryGetValue(d.AmmoType, out var c) ? c : null);

        /// <summary>The load a gun fires when it is told `kind`; Standard when the caliber has no such load.</summary>
        public static AmmoLoad Load(WeaponDef d, string kind)
        {
            var c = Of(d); if (c == null) return null;
            return c.Find(kind ?? "Standard") ?? c.Standard;
        }

        /// <summary>The caliber most often fired by a pool class ("pistol", "rifle", ...): where old save-game rounds of that class go when no gun in hand takes them.</summary>
        public static Caliber DefaultOf(string cls) => Calibers.Values.FirstOrDefault(c => c.Class == cls && (c.Id == "9x19mm" || c.Id == "5.45x39mm" || c.Id == "7.62x54mmR" || c.Id == "12g" || c.Id == "2mm Battery"))
            ?? Calibers.Values.FirstOrDefault(c => c.Class == cls);
    }
}
