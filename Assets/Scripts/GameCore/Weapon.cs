// Ported from script.js: class Weapon
// fire()/reload() call into grid/combat systems (line of sight, attack rolls, damage calc) that
// are implemented in later port steps — see GameServices.cs. The method bodies here are the full
// ported logic, not stubs; they'll work end-to-end once GameServices.Grid/Combat are assigned.
using System.Collections.Generic;
using System.Linq;
using GameData;

namespace GameCore
{
    public class WeaponFireResult
    {
        public bool Hit;
        public bool Jammed;
        public bool NoAmmo;
        public bool NoLineOfSight;
        public float Damage;
    }

    public class Weapon
    {
        public string Name;
        public float BaseDamage;
        public int MagCapacity;
        public int CurrentMagazine;
        public string CurrentAmmoType;
        public Dictionary<string, int> ReserveAmmo;
        public string FireMode = "Single"; // "Single" | "Burst"
        public bool IsJammed;
        public string Optic, Under;      // 2026-11-16 (Optics.cs): the sight on the rail ("pu2", "acog", "scope8", "reddot") and the laser under the barrel ("laser"); null = none. Not saved yet.

        public ICombatant Owner;         // set to whoever equips it (matches weapon.owner in JS)
        public WeaponDef Db;             // source database entry, for db.maxMpWhileHeld etc.
        public int RangeTiles;           // set externally via parseRangeToTiles(db.range) (grid step)
        /// <summary>Batch 74 (Yan): a gun shoots normally out to its range and can still fire out to twice that at -2
        /// to hit. Melee and other 1-tile weapons reach 1.</summary>
        public int MaxReach => RangeTiles <= 1 ? System.Math.Max(1, RangeTiles) : RangeTiles * 2;

        // New: mirrors InventoryItem.Condition (ItemCondition.cs) for whichever weapon is actually
        // equipped - Player.SaveLoad.cs already reads/writes this on every save/load round-trip
        // (EquippedWeapon?.Condition), and EquipWeaponFromInventory (Player.Equipment.cs) carries
        // the source inventory item's Condition over onto the equipped instance. Defaults to
        // Working (pristine) so a freshly-constructed weapon (EquipWeapon's direct db lookup,
        // GameBootstrap's starting loadout) needs no explicit initialization.
        public string Condition = ItemCondition.Working;
        public List<string> Upgrades = new List<string>();   // technician upgrades (Upgrades.cs)

        /// <summary>Re-derives what the upgrades change on the gun itself (magazine size).</summary>
        public void ApplyUpgrades()
        {
            int baseMag = Db?.MagCapacity ?? Db?.CylinderCapacity ?? MagCapacity;
            MagCapacity = Upgrades.Contains("Extended Mag") && !CombatRules.IsMelee(this) ? baseMag * 3 / 2 : baseMag;
            if (CurrentMagazine > MagCapacity) CurrentMagazine = MagCapacity;
        }

        public Weapon(string name, float baseDamage, int magCapacity, string currentAmmoType, Dictionary<string, int> reserveAmmo)
        {
            Name = name;
            BaseDamage = baseDamage;
            MagCapacity = magCapacity;
            CurrentMagazine = magCapacity;
            CurrentAmmoType = currentAmmoType;

            // Object.assign({Standard:0, HollowPoint:0, ArmorPiercing:0}, reserveAmmo)
            ReserveAmmo = new Dictionary<string, int> { ["Standard"] = 0, ["HollowPoint"] = 0, ["ArmorPiercing"] = 0 };
            if (reserveAmmo != null)
                foreach (var kvp in reserveAmmo) ReserveAmmo[kvp.Key] = kvp.Value;
        }

        public bool IsLoaded => CurrentMagazine > 0;

        public string Status
        {
            get
            {
                if (IsJammed) return "Jammed";
                return CurrentMagazine > 0 ? "Loaded" : "Empty";
            }
        }

        /// <summary>Ported from calculateDamage(target) instance method.</summary>
        public float CalculateDamage(ICombatant target)
        {
            return GameServices.RequireCombat().CalculateDamage(BaseDamage, CurrentAmmoType, target.PhysDR, target.AnomDR);
        }

        /// <summary>One attack by an NPC with this weapon (real time: no dice). A blade or club is a real swing (Melee.NpcSwing, it lands if you are in its
        /// reach and arc when it comes); a gun fires real rounds (Gunplay.NpcShoot: spread, cover, armor). The player's own shots and swings go through Rt
        /// (Gunplay.Shoot, Melee) and never reach this.</summary>
        public WeaponFireResult Fire(ICombatant target, string fireMode = null)
        {
            if (ItemCondition.IsBroken(Condition))
            {
                GameLog.Write($"{Name} is broken and can't fire until it's repaired.");
                return new WeaponFireResult { Hit = false };
            }

            bool burst = (fireMode ?? FireMode) == "Burst" && CombatRules.CanBurst(this);
            string modeName = burst ? "Burst" : "Single";
            string cat = CombatRules.Category(Db);
            var attacker = Owner ?? (ICombatant)Player.Current;
            bool shredder = burst && cat == "heavy" && CombatantUtil.HasPerk(attacker, "Shredder");
            int roundsToUse = burst ? CombatRules.BurstRounds(this) : 1;
            bool isMelee = CombatRules.IsMelee(this);

            if (!isMelee && CurrentMagazine < roundsToUse)
            {
                GameLog.Write($"{Name} does not have enough rounds for {modeName} fire ({roundsToUse} required).");
                return new WeaponFireResult { Hit = false, NoAmmo = true };
            }
            if (isMelee)
            {
                if (Owner == null || Owner is Player) return new WeaponFireResult { Hit = false };
                var swing = CombatFx.Push("melee", Owner, target, this); swing.Now = true;
                Melee.NpcSwing(Owner, this, target);                         // Melee 2c: it winds up and lands at the strike
                return new WeaponFireResult { Hit = false };
            }
            CurrentMagazine -= roundsToUse;
            if (attacker is Player) GameLog.Write($"{Name} fires in {modeName} mode, consuming {roundsToUse} round{(roundsToUse > 1 ? "s" : "")}.");
            var fx = CombatFx.Push("shot", attacker, target, this);
            fx.Rounds = roundsToUse;
            var grid = GameServices.RequireGrid();
            Noise.Make(attacker.X, attacker.Y, Db != null && Db.InnateSuppressor ? Noise.Suppressed : Noise.Shot, attacker);   // Batch 47
            if (!grid.CheckLineOfSight(attacker.X, attacker.Y, target.X, target.Y))
            {
                GameLog.Write("Target obscured - No Line of Sight");
                fx.NoLos = true;
                return new WeaponFireResult { Hit = false, NoLineOfSight = true };
            }
            Gunplay.NpcShoot(attacker, this, target, roundsToUse);
            return new WeaponFireResult();
        }

        /// <summary>Death hooks for anything that kills a unit (a shot, a barrel blast): loot,
        /// reputation, quest progress and the Black Star's kill-AP refund. Rewards only go out
        /// when `attacker` is the player.</summary>
        public static void OnTargetKilled(ICombatant attacker, ICombatant target)
        {
            GameLog.Write($"{target.Name} has been defeated.");
            Noise.Smell(target.X, target.Y);   // blood: mutant packs nearby may come for it
            Companion.OnKilled(target);   // Batch 69
            if (attacker is HumanEnemy veteran && veteran.IsAlive()) veteran.GainXp(HumanEnemy.Worth(target));   // Batch 53: NPCs learn from their kills

            // New: death hooks - loot, reputation, quest progress and the Black Star's kill-AP
            // refund all used to have their data/plumbing in place (HumanLootSystem,
            // MutantLootSystem, Player.AdjustReputation, QuestSystem.OnKill,
            // Player.ArtifactKillRefundAp) with nothing ever actually calling them from here.
            // Loot grants always go to Player.Current (only the player can loot a corpse), but
            // reputation/quest-progress/AP-refund only make sense when the player himself
            // landed the kill - an NPC killing another NPC (if that ever happens) shouldn't pay
            // out a player reward.
            if (target is HumanEnemy deadHuman)
            {
                EnemyAI.StashGrenades(deadHuman);
                EnemyAI.OnHumanKilled(deadHuman);
                if (attacker is Player) WorldEvents.OnFactionMemberKilled(deadHuman.FactionName);
                var zone = MacroMapDatabase.Find(MacroState.Current.CurrentZoneId);
                HumanLootSystem.GrantDrop(deadHuman, Player.Current, zone?.Tier ?? 1, includeWornArmor: true);
            }
            else if (target is Mutant deadMutant && attacker is Player)   // Batch 39: parts go straight into your pack, so only for your kills
            {
                MutantLootSystem.GrantDrop(deadMutant, Player.Current);
            }

            if (attacker is Player killer) killer.NoteKill(target);
            StorySystem.OnKill(target);
            Stashes.OnKill(target);   // Batch 126: a swallowed key
            if (attacker is Player)
            {
                if (target is HumanEnemy npcKilled)
                {
                    // Small, flat rep hit for gunning down a faction's own people - keeps a
                    // single fight from being free, without the swings AdjustReputation's
                    // Hostile-lockout guard already exists to prevent (see its doc comment).
                    Player.Current.AdjustReputation(npcKilled.FactionName, -10);   // file 00 §12: a skirmish -1, several kills -2/-3
                    QuestSystem.OnKill("NPC", QuestSystem.NpcRankBucket(npcKilled.Rank), MacroState.Current.CurrentZoneId);
                }
                else if (target is Mutant mutantKilled)
                {
                    QuestSystem.OnKill("Mutant", mutantKilled.Db?.Tier ?? 1, MacroState.Current.CurrentZoneId);
                }

            }
        }

        /// <summary>Shoots at a map object instead of a unit - an explosive barrel, a door, or a
        /// piece of cover someone is hiding behind. Objects don't dodge: d20 + modifier vs AC 8.
        /// A hit takes 1 Integrity (2 for burst or shotgun, file 00 Cover Integrity). Caller has
        /// already paid the AP and checked range.</summary>
        public bool FireAtTile(int x, int y, string fireMode = null)
        {
            const int ObjectAc = 8;
            if (IsJammed)
            {
                GameLog.Write($"{Name} is jammed and cannot fire.");
                return false;
            }
            if (ItemCondition.IsBroken(Condition))
            {
                GameLog.Write($"{Name} is broken and can't fire until it's repaired.");
                return false;
            }
            bool burst = fireMode == "Burst" && CombatRules.CanBurst(this);
            string modeName = burst ? "Burst" : "Single";
            int roundsToUse = burst ? CombatRules.BurstRounds(this) : 1;
            bool isMelee = CombatRules.IsMelee(this);
            if (!isMelee)
            {
                if (CurrentMagazine < roundsToUse)
                {
                    GameLog.Write($"{Name} does not have enough rounds for {modeName} fire ({roundsToUse} required).");
                    return false;
                }
                CurrentMagazine -= roundsToUse;
            }
            var fx = CombatFx.Push(isMelee ? "melee" : "shot", Owner ?? (ICombatant)Player.Current, null, this);
            fx.Rounds = roundsToUse; fx.ToX = x; fx.ToY = y;

            int ownerX = Owner != null ? Owner.X : Player.Current.X;
            int ownerY = Owner != null ? Owner.Y : Player.Current.Y;
            if (!GameServices.RequireGrid().CheckLineOfSight(ownerX, ownerY, x, y))
            {
                GameLog.Write("Target obscured - No Line of Sight");
                fx.NoLos = true;
                return false;
            }

            var shooter = Owner ?? (ICombatant)Player.Current;
            fx.Hit = true;                                   // real time: a shot at a barrel or a wall in plain sight lands (no dice)
            TerrainMap.DamageTile(x, y, burst || CombatRules.IsShotgun(this) ? 2 : 1, shooter);
            return true;
        }

        /// <summary>Ported from reload(). Consumes from the player's global ammo reserve first, then local reserve.</summary>
        public bool Reload(int max = int.MaxValue)   // RT 6h: max = 1 loads a single round
        {
            if (CurrentMagazine >= MagCapacity)
            {
                GameLog.Write($"{Name} magazine is already full.");
                return false;
            }

            // Fix: this used to read Player.Current.Inventory unconditionally regardless of Owner -
            // once HumanEnemy's ammo-type key bug (above) was fixed, an enemy reload would have
            // started draining the *player's* ammo stash instead of just failing outright. Only the
            // player's own weapon (or a weapon with no Owner set, matching the old default) draws
            // from the player's global reserve; every other owner uses local ReserveAmmo only.
            int globalReserve = 0;
            bool ownerIsPlayer = Owner == null || Owner == (ICombatant)Player.Current;
            string poolKey = Ammo.Key(Db, CurrentAmmoType);   // Batch 80: the player's rounds are per class and tier
            if (ownerIsPlayer && Player.Current != null && Player.Current.Inventory.AmmoReserve.TryGetValue(poolKey, out var g))
                globalReserve = g;
            int localReserve = ReserveAmmo.TryGetValue(CurrentAmmoType, out var l) ? l : 0;
            int totalReserve = globalReserve + localReserve;

            if (totalReserve <= 0)
            {
                GameLog.Write($"No {(ownerIsPlayer ? Ammo.Label(poolKey) : CurrentAmmoType)} left to reload.");
                return false;
            }

            int needed = MagCapacity - CurrentMagazine;
            int toLoad = System.Math.Min(System.Math.Min(needed, totalReserve), max);

            int consumedFromGlobal = 0;
            if (globalReserve > 0)
            {
                consumedFromGlobal = System.Math.Min(globalReserve, toLoad);
                Player.Current.Inventory.AmmoReserve[poolKey] = globalReserve - consumedFromGlobal;
            }
            int remainingToLoad = toLoad - consumedFromGlobal;
            if (remainingToLoad > 0)
            {
                ReserveAmmo[CurrentAmmoType] = localReserve - remainingToLoad;
            }

            CurrentMagazine += toLoad;
            if (!Rt.On) CombatFx.Push("reload", Owner ?? (ICombatant)Player.Current, null, this);   // real time: the animation started with the timer (CombatFx.Reload)
            if (max == int.MaxValue) GameLog.Write($"{Name} reloaded {toLoad} rounds ({consumedFromGlobal} from stash). Magazine is now {CurrentMagazine}/{MagCapacity}.");
            return true;
        }

        /// <summary>Ported from clearJam().</summary>
        public bool ClearJam()
        {
            if (!IsJammed)
            {
                GameLog.Write($"{Name} is not jammed.");
                return false;
            }

            IsJammed = false;
            GameLog.Write($"{Name} jam is cleared.");
            return true;
        }
    }
}
