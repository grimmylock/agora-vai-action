// The design's combat math in one place (file 00 §6 "Combat Resolution", file 01 burst table and
// weapon traits, file 04 ranks), so Weapon.Fire, mutant attacks, explosions and NPC AI all agree:
//   * Burst: one d20 at a flat penalty (AR -3, SMG -1, heavy -3; VK-74 -2, Corvan Battle Rifle -4), uses 3/5/10
//     rounds, damage x2.0 / x2.4 / x3.0. AP 3 (SMG 2, NA-94 2).
//   * Batch 56: attacks roll 2d10. A hit by 0-3 over AC grazes (x0.85), 4-6 is a clean hit (x1.0), 7+ a
//     vital hit (x1.2); a natural 20 (both dice 10, 1%) is a headshot: auto-hit, x1.5. All before ammo and armor.
//   * Batch 63: a separate d100 per attack jams 1% of the time, +2% per wear step (Dirty 3%, Damaged 5%).
//   * Jams drop the weapon one Condition step (Working > Dirty > Damaged > Broken).
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

        /// <summary>Batch 77: the chance (0..1) that a 2d10 attack hits: kept total + mod >= AC, and a double 10 always hits.
        /// With advantage the better of two pairs is kept.</summary>
        public static float HitChance(int mod, int ac, bool advantage = false)
        {
            float single = 0f, cdfMiss = 0f;
            var pSum = new float[21];
            for (int a = 1; a <= 10; a++) for (int b = 1; b <= 10; b++) pSum[a + b] += 0.01f;
            for (int sum = 2; sum <= 20; sum++) if (sum + mod >= ac || sum == Headshot) single += pSum[sum];
            if (!advantage) return single;
            // keep the higher sum of two pairs: hit unless both pairs miss... except a miss pair can't make the other one miss,
            // and "hit" is monotone in the sum (a double 10 is the top sum), so P(hit) = 1 - P(miss)^2
            cdfMiss = 1f - single;
            return 1f - cdfMiss * cdfMiss;
        }

        /// <summary>Batch 77 (Yan): the player starts with 5 AP and gets +1 at Veteran (about 30% into the game). Bosses
        /// (Named units) follow the same rule; every other NPC has 4 AP whatever its rank (`NpcAp`).</summary>
        public static int PlayerRankAp(string rank) => RankIndex(rank ?? "Rookie") >= RankIndex("Veteran") ? 6 : 5;
        public const int NpcBaseAp = 4;
        /// <summary>Batch 78 (Yan): each gun of a Gunslinger pair at -1 to hit (was -2).</summary>
        public const int GunslingerPenalty = 1;
        public static int NpcAp(Mutant m) => m is HumanEnemy h && h.Named ? PlayerRankAp(h.Rank) : NpcBaseAp;

        /// <summary>Batch 75 (the build probe): extra AP for the player only. 0 = unchanged.</summary>
        public static int PlayerApBonus = 0;

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

        public static float BurstMultiplier(Weapon w)
        {
            switch (Category(w?.Db)) { case "smg": return 2.4f; case "heavy": return 3.0f; default: return 2.0f; }
        }

        public static int BurstPenalty(Weapon w)
        {
            string name = w?.Db?.Name;
            if (name == "VK-74") return -2;
            if (name == "Corvan Battle Rifle") return -4;
            return Category(w?.Db) == "smg" ? -1 : -3;
        }

        public static int BurstApCost(Weapon w) =>
            w?.Db?.BurstFireApCost ?? (Category(w?.Db) == "smg" ? 2 : 3);

        /// <summary>Weapon-specific to-hit modifiers (file 01 traits) for one attack.</summary>
        /// <summary>Batch 76 (Yan): "nerf snipers to consume 3 ap per shot". A single shot costs 2 AP, 3 with a sniper rifle.</summary>
        public static int SingleShotAp(Weapon w) => (Category(w?.Db) == "sniper" ? 3 : 2) + Kick(w);

        /// <summary>Batch 80 (Yan: "make it cost +1 ap per shot"): the RK-12's extra AP. Added after Double Tap and Gunslinger,
        /// so those perks still work with it (a Double Tap follow-up is 2, a Gunslinger pair 3).</summary>
        public static int Kick(Weapon w) => w?.Db?.Name == "RK-12 Kuvalda" ? 1 : 0;

        public static int TraitToHit(Weapon w, bool burst, int distance)
        {
            if (w?.Db == null) return 0;
            int mod = 0;
            if (Category(w.Db) == "sniper")   // Batch 80 (Yan): the scope helps far out, hurts up close
            {
                if (distance > w.RangeTiles / 2) mod += 1;
                if (distance <= 5) mod -= 2;
            }
            if (Category(w.Db) == "sidearm" && distance <= w.RangeTiles) mod += 1;   // Batch 78 (Yan): pistols handle well inside their range   // Batch 76 (Yan): "+1 to hit in the weapon class because they're scoped"
            if (w.Db.HighRecoil) mod -= 1;
            if (IsShotgun(w) && distance <= 4) mod += 2;
            switch (w.Db.Name)
            {
                case "TR-30": if (!burst) mod += 1; break;
                case "BR-86 Bulldog": if (distance >= 10) mod += 1; break;
                case "VK-74 Obelisk":
                case "Corvan Bullpup":
                case "PL-74P Polovets Zulu": mod += 1; break;
            }
            return mod;
        }

        /// <summary>Does this kept die jam the weapon?</summary>
        public const int Headshot = 20;

        /// <summary>Batch 56 hit tiers: damage multiplier for a hit, from the kept 2d10 and the total vs AC.</summary>
        public static float HitMultiplier(int die, int total, int ac) =>
            die == Headshot ? 1.5f : total - ac <= 3 ? 0.85f : total - ac <= 6 ? 1f : 1.2f;

        // Batch 57: aimed shots (L to pick, +1 AP, single shots only). Penalty to hit, then on a clean hit or better:
        // Legs = Leg wound (-2 MP; a vital hit also knocks them down), Arms = Arm wound (-2 to hit; a vital hit
        // knocks the gun out of their hands, 2 AP to pick up), Torso = Bleeding, Head = one hit tier higher and
        // -1 AP next turn (a head-shot kill shakes the squad, and breaks anyone already shaken), Armor = one
        // Condition step off their armor (a mutant's hide loses a quarter of its Phys-DR), Pouch = the grenade
        // on their belt goes off (only on someone carrying one).
        public static readonly string[] AimedParts = { "Legs", "Arms", "Torso", "Head", "Armor", "Pouch" };
        public static int AimedPenalty(string part) => part == "Legs" || part == "Arms" ? -2 : part == "Torso" || part == "Armor" ? -1 : part == "Pouch" ? -4 : part == "Head" ? -5 : 0;
        public static float TierUp(float mult) => mult < 1f ? 1f : mult == 1f ? 1.2f : 1.5f;

        public static void ApplyAimed(ICombatant target, string part, float mult)
        {
            if (part == null || mult < 1f || !target.IsAlive()) return;
            string who = target is Player ? "Player" : target.Name;
            switch (part)
            {
                case "Legs":
                    target.StatusEffects.Wound = "Leg";
                    if (mult > 1f) target.StatusEffects.Prone = true;
                    GameLog.Write($"Leg shot: {who} is hobbled (-2 MP){(mult > 1f ? " and goes down" : "")}.");
                    break;
                case "Arms":
                    target.StatusEffects.Wound = "Arm";
                    bool disarm = mult > 1f && target is HumanEnemy armed && armed.EquippedWeapon != null && !IsMelee(armed.EquippedWeapon);
                    if (disarm) ((HumanEnemy)target).GunDropped = true;
                    GameLog.Write($"Arm shot: {who} is maimed (-2 to hit){(disarm ? " and drops their gun" : "")}.");
                    break;
                case "Armor":
                    if (target is HumanEnemy worn && worn.EquippedArmor != null) { worn.WearArmor(); GameLog.Write($"Armor shot: {who}'s armor cracks ({worn.ArmorCondition})."); }
                    else if (!(target is HumanEnemy) && target.PhysDR > 0) { target.PhysDR *= 0.75f; GameLog.Write($"Armor shot: {who}'s hide cracks (Phys-DR {target.PhysDR:P0})."); }
                    break;
                case "Torso":
                    target.StatusEffects.Bleeding = true;
                    GameLog.Write($"Body shot: {who} is bleeding.");
                    break;
                case "Head":
                    target.PendingApDrain += 1;
                    GameLog.Write($"Head shot: {who} is dazed (-1 AP next turn).");
                    break;
            }
        }

        /// <summary>Pouch shot: the grenade on their belt goes off where they stand (alive or not).</summary>
        public static void CookOff(ICombatant target, ICombatant shooter)
        {
            if (!(target is HumanEnemy h) || h.Frags <= 0) return;
            h.Frags--;
            GameLog.Write($"Pouch shot: the grenade on {h.Name}'s belt goes off!");
            Explosions.Blast(h.X, h.Y, Explosions.ThrowRadius, 400, shooter, "cooked-off grenade");
        }

        public static string HitName(float mult) => mult >= 1.5f ? "Headshot!" : mult > 1f ? "Vital hit" : mult < 1f ? "Graze" : "Clean hit";

        /// <summary>Batch 63 (Yan): jam chance in percent - 1%, +2% per wear step. The BR-86 Bulldog is finicky (one
        /// step worse); a Hardened gun doesn't jam more as it wears.</summary>
        public static int JamChance(Weapon w)
        {
            if (w != null && IsMelee(w)) return 0;   // Batch 92: a blade doesn't jam (the harness caught a knife "jamming")
            int steps = w?.Condition == ItemCondition.Dirty ? 1 : w?.Condition == ItemCondition.Damaged ? 2 : 0;
            if (w != null && w.Upgrades.Contains("Hardened")) steps = 0;
            if (w?.Db?.Name == "BR-86 Bulldog") steps++;
            return 1 + 2 * steps;
        }

        public static bool Jams(int d100, Weapon w) => d100 <= JamChance(w);

        public static int RollJam() => UnityEngine.Random.Range(1, 101);

        /// <summary>A jam costs the weapon one Condition step (VK-103 is Reliable: never).</summary>
        public static void DegradeOnJam(Weapon w)
        {
            if (w == null || w.Db?.Name == "VK-103") return;
            string before = w.Condition;
            w.Condition = ItemCondition.Worse(w.Condition);
            if (w.Condition != before) GameLog.Write($"{w.Name} is now {w.Condition}.");
        }

        public static int WoundThreshold(Weapon w) => w?.Db?.Name == "VKM" ? 15 : 20;

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
                case "ArmorPiercing": dr *= 0.4f; break;
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
                case "Leg": GameLog.Write($"{who} takes a LEG wound: -2 MP until bandaged."); break;
                case "Torso":
                    target.StatusEffects.Bleeding = true;
                    GameLog.Write($"{who} takes a TORSO wound and is bleeding.");
                    break;
                default:
                    target.PendingApDrain += 1;
                    GameLog.Write($"{who} takes a HEAD wound: -1 AP next turn.");
                    break;
            }
            if (target is Player p) { p.ApplyWoundEffects(); p.WearArmor(); }
            else if (target is HumanEnemy h) h.WearArmor();
        }

        /// <summary>To-hit penalty from the attacker's own state (Arm wound, Suppressed, Shaken).</summary>
        public static int AttackerPenalty(ICombatant attacker)
        {
            if (attacker?.StatusEffects == null) return 0;
            int mod = 0;
            if (attacker.StatusEffects.Wound == "Arm") mod -= 2;
            if (attacker.StatusEffects.Suppressed) mod -= 2;
            if (attacker is HumanEnemy h && h.Morale == "Shaken") mod -= 2;
            return mod;
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
