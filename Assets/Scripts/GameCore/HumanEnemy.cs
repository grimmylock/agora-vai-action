// Ported from script.js: class HumanEnemy extends Mutant
using System.Collections.Generic;
using GameData;
using UnityEngine;

namespace GameCore
{
    public class HumanEnemy : Mutant
    {
        public string FactionName;
        public override string Faction => FactionName;
        public string Rank;
        public RankDef RankData;
        public int D20Mod;
        public int MoraleHPPercent;

        public Weapon EquippedWeapon;
        public ArmorDef EquippedArmor;

        // New: a real per-NPC stash (living-world scavenging) instead of loot fabricated fresh
        // at death - see HumanLootSystem.RollSpawnStash (seeded at spawn), TakeSpoils (grown
        // while the squad wanders offscreen) and FinalizeCorpseLoot/CorpseLootUI (read once dead).
        public PlayerInventory Inventory = new PlayerInventory();
        public int Ru;
        // New: separate from the base Mutant.LootGranted (which HumanLootSystem.GrantDrop's flee
        // branch sets to mean "weapon already handed to the player") so a corpse whose weapon was
        // already flee-looted still gets its armor finalized into Inventory instead of the whole
        // corpse-loot step being skipped. See GrantDrop.
        public bool CorpseLootFinalized;

        // New (file 04 morale): "Steady" / "Shaken" / "Broken" / "Surrendered".
        public string Morale = "Steady";
        public int Frags;   // Professional+ carry one frag grenade (file 04)
        public bool GunDropped;   // Batch 57: shot out of their hands by an aimed arm shot; 2 AP to pick it up
        public string ExtraThrowable;   // smoke or flashbang for the disciplined factions (file 04) - drops as loot
        public bool HpShaken;   // already shaken by crossing their HP threshold
        public string ArmorCondition = ItemCondition.Working;

        /// <summary>MP for a turn (file 04): armor MP (4 unarmored), +1 at Expert or higher, heavy
        /// weapons cap at 2, a Leg wound costs 2 (minimum 1).</summary>
        public int TurnMp()
        {
            int mp = (EquippedArmor != null && EquippedArmor.Mp > 0 ? EquippedArmor.Mp : 4)
                + (CombatRules.RankIndex(Rank) >= CombatRules.RankIndex("Expert") ? 1 : 0);
            if (EquippedWeapon?.Db?.MaxMpWhileHeld != null && !CombatantUtil.HasPerk(this, "WalkingTank")) mp = System.Math.Min(mp, EquippedWeapon.Db.MaxMpWhileHeld.Value);
            if (CombatRules.IsMelee(EquippedWeapon) && CombatantUtil.HasPerk(this, "BullRush")) mp *= 2;   // Batch 86
            if (StatusEffects.Wound == "Leg") mp = System.Math.Max(1, mp - 2);
            return mp;
        }

        /// <summary>Armor drops one Condition step on a 20+ or natural-20 hit (file 00).</summary>
        public void WearArmor()
        {
            if (EquippedArmor == null || ItemCondition.IsBroken(ArmorCondition)) return;
            ArmorCondition = ItemCondition.Worse(ArmorCondition);
            PhysDR = EquippedArmor.PhysDR * ItemCondition.ArmorDrMultiplier(ArmorCondition);
            AnomDR = EquippedArmor.AnomDR * ItemCondition.ArmorDrMultiplier(ArmorCondition);
        }

        public HumanEnemy(string name, int x, int y, string faction, string rankName, WeaponDef weaponDb, ArmorDef armorDb)
            : base(name, x, y, null)
        {
            FactionName = string.IsNullOrEmpty(faction) ? "Raiders" : faction;
            Rank = string.IsNullOrEmpty(rankName) ? "Rookie" : rankName;
            RankData = FactionDatabase.Ranks.TryGetValue(Rank, out var rd)
                ? rd
                : new RankDef { Ap = 4, Mp = 4, D20Mod = 0, MoraleHPPercent = 50 };

            Ap = CombatRules.NpcAp(this);   // Batch 77: 4 for every rank; bosses follow the player's rule
            Mp = RankData.Mp;
            D20Mod = RankData.D20Mod;
            MoraleHPPercent = RankData.MoraleHPPercent > 0 ? RankData.MoraleHPPercent : 50;

            EquippedWeapon = null;
            EquippedArmor = null;
            if (weaponDb != null) EquipWeaponFromDb(weaponDb);
            if (armorDb != null) EquipArmorFromDb(armorDb);

            // Fix: this used to inflate MaxHp by (1 + PhysDR + AnomDR) on top of the armor's DR
            // already cutting incoming damage - a heavily armored NPC (0.85 PhysDR/0.9 AnomDR gear
            // exists in ArmorDatabase) ended up with 275 HP instead of the flat 100 every human
            // (including the player - see Player.cs's MaxHp = 100) is supposed to have. NPCs equip
            // real gear the same way the player does (EquipWeaponFromDb/EquipArmorFromDb just above
            // set PhysDR/AnomDR straight from the ArmorDef, same as Player.EquipArmor) - that's
            // where their defense is supposed to live, not in bonus HP.
            MaxHp = 100;
            Hp = MaxHp;
            Ac = CombatRules.RankAc(Rank);
            Frags = CombatRules.RankIndex(Rank) >= CombatRules.RankIndex("Veteran") ? 1 : 0;   // Batch 56: grenades are Veteran+ gear (quest bosses get one, QuestSystem)
            // file 04: Garrison/Sentinels/Contractors/IZC/Obelisk Veterans+ carry one more throwable
            if (Frags > 0 && System.Array.IndexOf(new[] { "Garrison", "Sentinels", "Contractors", "IZC", "Obelisk" }, FactionName) >= 0)
                ExtraThrowable = UnityEngine.Random.Range(1, 7) <= 3 ? "smokeGrenade" : "flashbang";
            State = "normal";
        }

        /// <summary>Ported from equipWeaponFromDb(db).</summary>
        public void EquipWeaponFromDb(WeaponDef db)
        {
            // Fix: this used to set CurrentAmmoType to db.AmmoType (a caliber string like
            // "9x18mm"), but ReserveAmmo/the player's global AmmoReserve are only ever keyed by
            // ammo *effect* ("Standard"/"HollowPoint"/"ArmorPiercing") - same scheme Player.EquipWeapon
            // uses. That mismatch meant Weapon.Reload() could never find any reserve under the
            // caliber key, so every human enemy went permanently silent (AP-wasting reload loop,
            // see HumanEnemy.Attack) the moment its starting magazine ran dry. db.AmmoType is kept
            // on the WeaponDef itself (Db.AmmoType) for the shotgun/Breacher check and range
            // parsing - it was never meant to double as a reload key.
            // ponytail: melee weapons (Mag/CylinderCapacity both null) default to a synthetic
            // 1-round "magazine" that Weapon.Fire never actually consumes - see the isMelee guard
            // there - matching the same fix on the player side (Player.Equipment.EquipWeapon).
            int mag = db.MagCapacity ?? db.CylinderCapacity ?? 1;
            var w = new Weapon(
                string.IsNullOrEmpty(db.Name) ? "EnemyWeapon" : db.Name,
                db.BaseDamage > 0 ? db.BaseDamage : 1,
                mag,
                "Standard",
                new Dictionary<string, int> { ["Standard"] = mag, ["HollowPoint"] = 0, ["ArmorPiercing"] = 0 });
            w.Db = db;
            w.Owner = this;
            w.RangeTiles = RangeUtil.ParseRangeToTiles(db.Range);
            EquippedWeapon = w;
            if (ApRounds) LoadApRounds();
            if (Named) BossPerks();
        }

        // Batch 86 (Yan: "boss perks = 2 perks based on their main weapon"). Every pair works for an NPC.
        static readonly Dictionary<string, string[]> PerksByClass = new Dictionary<string, string[]>
        {
            ["sniper"] = new[] { "MarksmansFocus", "HighVelocityImpact" }, ["assault"] = new[] { "RecoilControl", "ControlledTaps" },
            ["smg"] = new[] { "BulletHose", "HitAndRun" }, ["heavy"] = new[] { "Shredder", "WalkingTank" },
            ["shotgun"] = new[] { "Breacher", "StaggeringBlast" }, ["sidearm"] = new[] { "PointBlankExecution", "DoubleTap" },
            ["melee"] = new[] { "BullRush", "BladeDancer" },
        };

        /// <summary>A boss's 2 perks follow the gun (or blade) in its hands - set again if it picks up another kind.</summary>
        public void BossPerks()
        {
            foreach (var pair in PerksByClass.Values) foreach (var k in pair) ActivePerks.Remove(k);
            if (EquippedWeapon != null && PerksByClass.TryGetValue(CombatRules.Category(EquippedWeapon.Db), out var mine)) ActivePerks.AddRange(mine);
        }

        public bool TapUsed;                          // Controlled Taps, once a turn
        public int SidearmShots, Cuts, Attacks;       // Double Tap, Blade Dancer, Hit and Run - this turn
        public void NewTurn() { TapUsed = false; SidearmShots = Cuts = Attacks = 0; }

        /// <summary>Batch 80: this one carries armor-piercing rounds (SquadManager.Arm) - kept through re-kits and looted guns.</summary>
        public bool ApRounds, ArmRolled;

        public void MakeBoss(string callsign) { Named = true; Name = $"\"{callsign}\" ({FactionName} boss)"; BossPerks(); }

        public void LoadApRounds()
        {
            var w = EquippedWeapon;
            if (w == null || CombatRules.IsMelee(w)) return;
            w.CurrentAmmoType = "ArmorPiercing";
            w.ReserveAmmo["ArmorPiercing"] = w.MagCapacity;
        }

        // Batch 53: Yan: "these are the SPAWN rules, but any faction can have higher tier npc's if they survive long
        // enough and loot other higher tier enemies". Card is the deck card it was drawn as (FactionDeck counts that, not
        // what it grew into). A kill is worth the victim's rank (1 for a Rookie .. 7 for a Legend; a mutant its tier), and
        // a rank takes 4 x (its place in the order) points to leave - so a Rookie who kills an Obelisk Master moves up at once.
        public string Card;
        public int Xp;

        public static int Worth(ICombatant victim) => victim is HumanEnemy h ? CombatRules.RankIndex(h.Rank) + 1 : victim is Mutant m && m.Db != null ? System.Math.Max(1, m.Db.Tier) : 1;

        public void GainXp(int points)
        {
            Xp += points;
            int i;
            while ((i = CombatRules.RankIndex(Rank)) < CombatRules.RankOrder.Length - 1 && Xp >= 4 * (i + 1))
            {
                Xp -= 4 * (i + 1);
                Rank = CombatRules.RankOrder[i + 1];
                RankData = FactionDatabase.Ranks[Rank];
                D20Mod = RankData.D20Mod;
                MoraleHPPercent = RankData.MoraleHPPercent > 0 ? RankData.MoraleHPPercent : 50;
                GameLog.Write($"[SQUAD] {Name} ({FactionName}) has made {Rank}.");
            }
        }

        /// <summary>Batch 53: takes a dead enemy's weapon or armor when it's better than its own.</summary>
        public void LootFrom(HumanEnemy dead)
        {
            if (dead == null || dead.IsAlive()) return;
            if (dead.EquippedWeapon?.Db != null && dead.EquippedWeapon.Db.Tier > (EquippedWeapon?.Db?.Tier ?? -1)) { EquipWeaponFromDb(dead.EquippedWeapon.Db); dead.EquippedWeapon = null; }
            if (dead.EquippedArmor != null && dead.EquippedArmor.PhysDR + dead.EquippedArmor.AnomDR > (EquippedArmor == null ? -1f : EquippedArmor.PhysDR + EquippedArmor.AnomDR)) { EquipArmorFromDb(dead.EquippedArmor); dead.EquippedArmor = null; }
        }

        /// <summary>Ported from equipArmorFromDb(db).</summary>
        public void EquipArmorFromDb(ArmorDef db)
        {
            EquippedArmor = db;
            PhysDR = db.PhysDR;
            AnomDR = db.AnomDR;
            MaxMp = db.Mp > 0 ? db.Mp : (Mp > 0 ? (int)Mp : 4);
        }

        /// <summary>Ported from isFleeing() override.</summary>
        public override bool IsFleeing() => State == "fleeing";

        /// <summary>Ported from moveAwayFrom(target) override. Returns whether a step was actually taken.</summary>
        public override bool MoveAwayFrom(ICombatant target)
        {
            if (Mp <= 0) return false;

            int dx = System.Math.Sign(X - target.X);
            if (dx == 0) dx = Random.value < 0.5f ? -1 : 1;
            int dy = System.Math.Sign(Y - target.Y);
            if (dy == 0) dy = Random.value < 0.5f ? -1 : 1;

            int newX = System.Math.Min(System.Math.Max(0, X + dx), TerrainMap.Width - 1);
            int newY = System.Math.Min(System.Math.Max(0, Y + dy), TerrainMap.Height - 1);

            if (!GameServices.RequireGrid().AttemptMove(this, newX, newY))
                return false;

            GameLog.Write($"{Name} flees to ({X}, {Y}). MP remaining: {Mp}.");
            return true;
        }

        /// <summary>Unarmed / weapon-less fallback: the base claw-and-fist attack.</summary>
        public void MeleeAttack(ICombatant target) => base.Attack(target);

        /// <summary>Ported from attack(target) override.</summary>
        public override void Attack(ICombatant target) => Attack(target, "Single");

        /// <summary>One attack action. Burst only for assault rifles / SMGs / heavy weapons (file 01),
        /// at that weapon's own AP cost; single shots cost 2.</summary>
        public void Attack(ICombatant target, string fireMode)
        {
            if (GunDropped)
            {
                if (Ap < 2) return;
                Ap -= 2;
                GunDropped = false;
                GameLog.Write($"{Name} scrambles to pick up their {EquippedWeapon?.Name ?? "gun"}.");
                return;
            }
            if (EquippedWeapon == null)
            {
                base.Attack(target); // Fallback to melee/unarmed
                return;
            }

            int dist = CombatantUtil.Distance(this, target);
            if (dist > EquippedWeapon.MaxReach)
            {
                GameLog.Write($"{Name} is too far to shoot.");
                return;
            }

            if (!GameServices.RequireGrid().CheckLineOfSight(X, Y, target.X, target.Y))
            {
                GameLog.Write($"{Name} has no line of sight to {(target is Player ? "Player" : target.Name)}.");
                return;
            }

            if (EquippedWeapon.IsJammed && Ap >= 1)
            {
                Ap -= 1;
                EquippedWeapon.ClearJam();
                GameLog.Write($"{Name} clears a jam.");
            }
            else if (Ap >= 2 && EquippedWeapon.IsLoaded && FindBarrelNear(target, out int bx, out int by))
            {
                Ap -= 2;
                GameLog.Write($"{Name} shoots at the barrel next to {target.Name}!");
                EquippedWeapon.FireAtTile(bx, by, "Single", D20Mod);
            }
            else if (Ap >= CombatRules.SingleShotAp(EquippedWeapon) && EquippedWeapon.IsLoaded)
            {
                bool burst = fireMode == "Burst" && CombatRules.CanBurst(EquippedWeapon)
                    && Ap >= CombatRules.BurstApCost(EquippedWeapon) && EquippedWeapon.CurrentMagazine >= CombatRules.BurstRounds(EquippedWeapon);
                string cat = CombatRules.Category(EquippedWeapon.Db);
                bool melee = CombatRules.IsMelee(EquippedWeapon);
                if (melee && CombatantUtil.HasPerk(this, "BladeDancer") && Cuts >= 3) return;
                int cost = burst ? CombatRules.BurstApCost(EquippedWeapon) : CombatRules.SingleShotAp(EquippedWeapon);
                // Batch 86: the perks a boss can have (the player's rules for each)
                if (!burst && cat == "sidearm" && SidearmShots == 1 && CombatantUtil.HasPerk(this, "DoubleTap")) cost = 1 + CombatRules.Kick(EquippedWeapon);
                if (melee && CombatantUtil.HasPerk(this, "BladeDancer")) cost = Cuts == 0 ? 2 : 1;
                Ap -= cost;
                if (!burst && cat == "sidearm") SidearmShots++;
                if (melee) Cuts++;
                if (++Attacks == 1 && CombatantUtil.HasPerk(this, "HitAndRun")) Mp += 2;
                GameLog.Write($"{Name} {(burst ? "opens up with a burst from" : "fires")} {EquippedWeapon.Name} at {(target is Player ? "Player" : target.Name)}!");
                EquippedWeapon.Fire(target, burst ? "Burst" : "Single", D20Mod);
            }
            else if (!EquippedWeapon.IsLoaded && Ap >= 1)
            {
                Ap -= 1;
                if (EquippedWeapon.Reload()) GameLog.Write($"{Name} reloads their weapon.");
                else
                {
                    // dry: drop the useless gun and fight with whatever's left (melee / Flanker)
                    GameLog.Write($"{Name}'s {EquippedWeapon.Name} clicks empty. They throw it aside.");
                    EquippedWeapon = null;
                }
            }
        }

        /// <summary>An explosive barrel within 1 tile of the target that this NPC can hit without
        /// catching itself in the blast - a smart shooter takes that over a straight shot.</summary>
        private bool FindBarrelNear(ICombatant target, out int bx, out int by)
        {
            bx = by = 0;
            if (string.IsNullOrEmpty(EquippedWeapon?.Db?.AmmoType)) return false; // melee can't
            foreach (var p in TerrainMap.GetAdjacentCoords(target.X, target.Y))
            {
                if (TerrainMap.GetTile(p.x, p.y).Type != TileType.Barrel) continue;
                int d = CombatantUtil.Distance(this, p.x, p.y);
                if (d <= Explosions.BarrelRadius || d > EquippedWeapon.MaxReach) continue;
                if (!GameServices.RequireGrid().CheckLineOfSight(X, Y, p.x, p.y)) continue;
                bx = p.x; by = p.y;
                return true;
            }
            return false;
        }
    }
}
