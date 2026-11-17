// The design's combat math in one place (file 00 §6 "Combat Resolution", file 01 burst table and
// weapon traits, file 04 ranks), so Weapon.Fire, mutant attacks, explosions and NPC AI all agree:
//   * Burst: one d20 at a flat penalty (AR -3, SMG -1, heavy -3; VK-74 -2, Corvan Battle Rifle -4), uses 3/5/10
//     rounds, damage x2.0 / x2.4 / x3.0. AP 3 (SMG 2, NA-94 2).
//   * Batch 56: attacks roll 2d10. A hit by 0-3 over AC grazes (x0.85), 4-6 is a clean hit (x1.0), 7+ a
//     vital hit (x1.2); a natural 20 (both dice 10, 1%) is a headshot: auto-hit, x1.5. All before ammo and armor.
//   * (Jams were removed 2026-11-16: no weapon jams any more.)
//   * Ammo vs total Phys-DR: Hollow Point x1.25 at 0%, x0.5 at 20%+; AP = DR x0.4; Dart = DR x0.5;
//     then the weapon's "ignores X%". Phys-DR and Anom-DR are capped at 75%. Anom-DR never applies
//     to bullets or melee.
//   * Wounds (Batch 60): a vital hit or headshot (VKM: a clean hit too); blasts: 20+ damage after DR. On a human it
//     rolls d6: 1-2 Arm (-2 to hit), 3-4 Leg (-2 MP), 5 Torso (Bleeding), 6 Head (-1 AP next turn).
//     The same trigger drops worn armor one Condition step.
using System.Collections.Generic;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class CombatRules
    {
        /// <summary>Batch 74: a multiplier on every gun's damage, for everyone (the lethality probe). 1 = unchanged.</summary>
        public static float Lethality = 1f;
        /// <summary>Batch 127 (probe only): a multiplier on the player's own damage. 1 = unchanged.</summary>
        public static float PlayerDamage = 1f;

        /// <summary>Batch 75 (the build probe): how much of its Phys-DR each armor tier keeps, by tier (index 1-5). All 1 = unchanged.</summary>
        public static float[] ArmorScaleByTier = { 1f, 1f, 1f, 1f, 1f, 1f };
        static System.Collections.Generic.Dictionary<GameData.ArmorDef, int> _armorTier;
        public static int ArmorTier(GameData.ArmorDef a)
        {
            if (a == null) return 0;
            if (_armorTier == null)
            {
                _armorTier = new System.Collections.Generic.Dictionary<GameData.ArmorDef, int>();
                foreach (var kv in GameData.ArmorDatabase.Tiers)
                    if (int.TryParse(kv.Key.Replace("tier", ""), out int t)) foreach (var d in kv.Value.Values) _armorTier[d] = t;
            }
            return _armorTier.TryGetValue(a, out int tier) ? tier : 0;
        }
        public static float ArmorScale(ICombatant target)
        {
            var a = target is Player p ? p.EquippedArmor : target is HumanEnemy h ? h.EquippedArmor : null;
            int t = ArmorTier(a);
            return t >= 1 && t < ArmorScaleByTier.Length ? ArmorScaleByTier[t] : 1f;
        }



        public const float DrCap = 0.75f;

        private static Dictionary<WeaponDef, string> _category;

        /// <summary>WeaponDatabase category key ("assault", "smg", ...) of a weapon, or "".</summary>
        public static string Category(WeaponDef db)
        {
            if (db == null) return "";
            if (_category == null)
            {
                _category = new Dictionary<WeaponDef, string>();
                foreach (var cat in WeaponDatabase.Categories)
                    foreach (var w in cat.Value.Values) _category[w] = cat.Key;
            }
            return _category.TryGetValue(db, out var c) ? c : "";
        }

        public static bool IsMelee(Weapon w) => w?.Db != null && string.IsNullOrEmpty(w.Db.AmmoType);
        public static bool IsShotgun(Weapon w) => w?.Db?.AmmoType == "12g";

        public static bool CanBurst(Weapon w)
        {
            string c = Category(w?.Db);
            return c == "assault" || c == "smg" || c == "heavy";
        }

        public static int BurstRounds(Weapon w)
        {
            switch (Category(w?.Db)) { case "smg": return 5; case "heavy": return 10; default: return 3; }
        }




        /// <summary>Does this kept die jam the weapon?</summary>
        public const int Headshot = 20;

        /// <summary>Batch 56 hit tiers: damage multiplier for a hit, from the kept 2d10 and the total vs AC.</summary>
        public static float HitMultiplier(int die, int total, int ac) =>
            die == Headshot ? 1.5f : total - ac <= 3 ? 0.85f : total - ac <= 6 ? 1f : 1.2f;

        // 2026-11-16 (Yan: "remove jam chance from the game completely, it ruins automatic weapons"): there is no jam roll any more - guns wear (Condition) but never jam.


        /// <summary>Batch 60 (Yan: "Vital hits only"): a gun or claw wounds - and wears armor down - on a vital hit or a
        /// headshot. The VKM's heavy round (it used to wound from 15 damage) also wounds on a clean hit. Blasts still go by damage.</summary>
        public static bool HitWounds(float tierMult, Weapon w) => tierMult > 1f || tierMult >= 1f && w?.Db?.Name == "VKM";

        /// <summary>Physical damage after ammo and armor. `rawDamage` already includes crit/burst
        /// multipliers. Returns the final whole number to subtract (minimum 1).</summary>
        public static int PhysicalDamage(float rawDamage, string ammo, float targetPhysDR, float ignorePercent = 0f)
        {
            float dr = Mathf.Min(DrCap, Mathf.Max(0f, targetPhysDR));
            float dmg = rawDamage;
            switch (ammo)
            {
                case "HollowPoint":
                    if (dr <= 0f) dmg *= 1.25f;
                    else if (dr >= 0.20f) dmg *= 0.5f;
                    break;
                case "ArmorPiercing": dr *= 0.5f; break;   // RT batch 6d (Yan): AP halves armor
                case "Dart": dr *= 0.5f; break;
            }
            dr *= 1f - ignorePercent;
            return System.Math.Max(1, (int)System.Math.Ceiling(dmg * (1f - dr)));
        }

        /// <summary>Anomaly / psi damage: only Anom-DR applies.</summary>
        public static int AnomalousDamage(float rawDamage, float targetAnomDR) =>
            System.Math.Max(1, (int)System.Math.Ceiling(rawDamage * (1f - Mathf.Min(DrCap, Mathf.Max(0f, targetAnomDR)))));

        public static bool IsHuman(ICombatant u) => u is Player || u is HumanEnemy;

        /// <summary>Wound + armor wear check after a physical hit on a human (file 00).</summary>
        public static void CheckWound(ICombatant target, int finalDamage, bool natural20, int threshold = 200)
        {
            if (!IsHuman(target) || !target.IsAlive()) return;
            if (finalDamage < threshold && !natural20) return;

            int d6 = Random.Range(1, 7);
            string wound = d6 <= 2 ? "Arm" : d6 <= 4 ? "Leg" : d6 == 5 ? "Torso" : "Head";
            target.StatusEffects.Wound = wound;
            string who = target is Player ? "Player" : target.Name;
            switch (wound)
            {
                case "Arm": GameLog.Write($"{who} takes an ARM wound: -2 to attack rolls until bandaged."); break;
                case "Leg": GameLog.Write($"{who} takes a LEG wound: a limp until bandaged."); break;
                case "Torso":
                    target.StatusEffects.Bleeding = true;
                    GameLog.Write($"{who} takes a TORSO wound and is bleeding.");
                    break;
                default:
                    GameLog.Write($"{who} takes a HEAD wound.");
                    break;
            }
            if (target is Player p) { p.ApplyWoundEffects(); p.WearArmor(); }
            else if (target is HumanEnemy h) h.WearArmor();
        }


        /// <summary>Rank modifier from file 04 (Rookie -1 ... Legend +3).</summary>
        public static int RankMod(string rank) =>
            rank != null && FactionDatabase.Ranks.TryGetValue(rank, out var r) ? r.D20Mod : 0;

        /// <summary>AC = 10 + rank modifier, minimum 10 (player and NPCs alike).</summary>
        public static int RankAc(string rank) => System.Math.Max(10, 10 + RankMod(rank));

        public static readonly string[] RankOrder = { "Rookie", "Experienced", "Professional", "Veteran", "Expert", "Master", "Legend" };
        public static int RankIndex(string rank) => System.Math.Max(0, System.Array.IndexOf(RankOrder, rank));
    }
}
