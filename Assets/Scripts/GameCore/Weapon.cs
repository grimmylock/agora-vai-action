// Ported from script.js: class Weapon
// fire()/reload() call into grid/combat systems (line of sight, attack rolls, damage calc) that
// are implemented in later port steps — see GameServices.cs. The method bodies here are the full
// ported logic, not stubs; they'll work end-to-end once GameServices.Grid/Combat are assigned.
using System.Collections.Generic;
using System.Linq;
using GameData;

namespace GameCore
{
    /// <summary>Batch 77: a shot's odds before it's taken (Weapon.PreviewHit).</summary>
    public class HitPreview
    {
        public int TotalMod, TargetAc, Elevation, Aim, Range;
        public bool Advantage, PouchFallback;
        public string AimedPart;
        public CoverResult Cover;
        public System.Collections.Generic.List<(string what, int v)> Parts;
        public float Chance;      // 0..1
    }

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

        /// <summary>Batch 77: everything that decides a shot's roll, without firing it (no ammo, no log, aim not spent):
        /// what you need on 2d10 against what AC, and why. Fire uses it; so does the targeting panel's hit chance.</summary>
        public HitPreview PreviewHit(ICombatant attacker, ICombatant target, bool burst, string aimedPart, int modifier)
        {
            var hp = new HitPreview();
            string cat = CombatRules.Category(Db);
            bool isMelee = CombatRules.IsMelee(this);
            int distance = attacker != null ? CombatantUtil.Distance(attacker, target) : 1;
            int ax = attacker?.X ?? 0, ay = attacker?.Y ?? 0;
            int heightDiff = TerrainMap.GetHeight(ax, ay) - TerrainMap.GetHeight(target.X, target.Y);
            hp.Elevation = System.Math.Min(2, System.Math.Max(-2, heightDiff / 2));
            var cover = GameServices.RequireCombat().GetCover(attacker, target, this);
            hp.Cover = cover;
            int targetAc = target.Ac + cover.AcBonus;
            if (cover.AcBonus > 0 && target.StatusEffects.TakeCoverAc > 0 && !target.StatusEffects.EvasiveMelee) targetAc += target.StatusEffects.TakeCoverAc;
            if (isMelee && target.StatusEffects.EvasiveMelee) targetAc += target.StatusEffects.TakeCoverAc;
            hp.TargetAc = targetAc;
            int flankMod = cover.Flanked ? 2 : 0;
            int burstMod = 0;
            if (burst)
            {
                burstMod = CombatRules.BurstPenalty(this);
                if (cat == "assault" && CombatantUtil.HasPerk(attacker, "RecoilControl")) burstMod = 0;
                if (cat == "smg" && distance <= 4 && CombatantUtil.HasPerk(attacker, "BulletHose")) burstMod = 0;
            }
            int perkMod = 0;
            if (cat == "sniper" && CombatantUtil.HasPerk(attacker, "MarksmansFocus")) perkMod += 2;
            if (cat == "sidearm" && distance <= 1 && CombatantUtil.HasPerk(attacker, "PointBlankExecution")) perkMod += 2;
            if (Upgrades.Contains("Calibrated")) perkMod += 1;
            hp.Aim = attacker?.StatusEffects != null ? attacker.StatusEffects.AimBonus : 0;
            if (burst || isMelee) aimedPart = null;
            if (aimedPart == "Pouch" && !(target is HumanEnemy { Frags: > 0 })) { aimedPart = null; hp.PouchFallback = true; }
            hp.AimedPart = aimedPart;
            int partMod = aimedPart != null ? CombatRules.AimedPenalty(aimedPart) : 0;
            hp.Range = !isMelee && RangeTiles > 1 && distance > RangeTiles ? -2 : 0;
            int trait = CombatRules.TraitToHit(this, burst, distance), penalty = CombatRules.AttackerPenalty(attacker);
            hp.Parts = new System.Collections.Generic.List<(string what, int v)>
            {
                ("skill", modifier), ("height", hp.Elevation), ("burst", burstMod), ("flank", flankMod), ("aim", hp.Aim),
                ("perks", perkMod), ("aimed part", partMod), ("range", hp.Range), ("weapon", trait), ("wounds/pinned", penalty),
            };
            hp.TotalMod = hp.Parts.Sum(x => x.v);
            bool shredder = burst && cat == "heavy" && CombatantUtil.HasPerk(attacker, "Shredder");
            hp.Advantage = shredder || (attacker is Player dodger && dodger.HasLeapDodgeAdvantage && target is Mutant lm && lm.Db != null && lm.Db.Leap);
            hp.Chance = CombatRules.HitChance(hp.TotalMod, hp.TargetAc, hp.Advantage);
            return hp;
        }

        /// <summary>Ported from fire(target, fireMode, modifier), rebuilt on the design's combat
        /// math (CombatRules, file 00 §6 / file 01): flat burst penalties and multipliers, natural
        /// 20 crits, jams that wear the gun, cover as AC, ammo vs capped Phys-DR, wounds.
        /// `modifier` is the shooter's own attack bonus (Agility + rank for the player, rank for
        /// NPCs); everything situational is added here.</summary>
        public WeaponFireResult Fire(ICombatant target, string fireMode = null, int modifier = 0, string aimedPart = null, bool ambush = false)
        {
            if (IsJammed)
            {
                GameLog.Write($"{Name} is jammed and cannot fire.");
                return new WeaponFireResult { Hit = false, Jammed = true };
            }
            if (ItemCondition.IsBroken(Condition))
            {
                GameLog.Write($"{Name} is broken and can't fire until it's repaired.");
                return new WeaponFireResult { Hit = false };
            }

            bool burst = (fireMode ?? FireMode) == "Burst" && CombatRules.CanBurst(this);
            string modeName = burst ? "Burst" : "Single";
            string cat = CombatRules.Category(Db);
            var shooterUnit = Owner ?? (ICombatant)Player.Current;
            bool shredder = burst && cat == "heavy" && CombatantUtil.HasPerk(shooterUnit, "Shredder");
            int roundsToUse = burst ? CombatRules.BurstRounds(this) * (shredder ? 2 : 1) : 1;
            bool isMelee = CombatRules.IsMelee(this);

            if (!isMelee && CurrentMagazine < roundsToUse)
            {
                GameLog.Write($"{Name} does not have enough rounds for {modeName} fire ({roundsToUse} required).");
                return new WeaponFireResult { Hit = false, NoAmmo = true };
            }
            if (!isMelee)
            {
                CurrentMagazine -= roundsToUse;
                GameLog.Write($"{Name} fires in {modeName} mode, consuming {roundsToUse} round{(roundsToUse > 1 ? "s" : "")}.");
            }
            var fx = CombatFx.Push(isMelee ? "melee" : "shot", Owner ?? (ICombatant)Player.Current, target, this);
            fx.Rounds = roundsToUse;

            var grid = GameServices.RequireGrid();
            var attacker = Owner ?? (ICombatant)Player.Current;
            int ownerX = attacker != null ? attacker.X : 0;
            int ownerY = attacker != null ? attacker.Y : 0;
            if (!isMelee) Noise.Make(ownerX, ownerY, Db != null && Db.InnateSuppressor ? Noise.Suppressed : Noise.Shot, attacker);   // Batch 47
            if (!isMelee && !grid.CheckLineOfSight(ownerX, ownerY, target.X, target.Y))
            {
                GameLog.Write("Target obscured - No Line of Sight");
                fx.NoLos = true;
                return new WeaponFireResult { Hit = false, NoLineOfSight = true };
            }

            // Real-time fork: an NPC's gunfire is real rounds now (Gunplay), not a roll (the player fires through Rt directly)
            if (Rt.On && !isMelee && !(attacker is Player)) { Gunplay.NpcShoot(attacker, this, target, roundsToUse); return new WeaponFireResult(); }

            var combat = GameServices.RequireCombat();
            int distance = attacker != null ? CombatantUtil.Distance(attacker, target) : 1;
            // Batch 77: the numbers come from PreviewHit, the same the targeting panel shows; Fire only adds the side effects
            var hp = PreviewHit(attacker, target, burst, aimedPart, modifier);
            var cover = hp.Cover;
            int targetAc = hp.TargetAc, totalMod = hp.TotalMod;
            aimedPart = hp.AimedPart;
            if (hp.Elevation != 0) GameLog.Write(hp.Elevation > 0 ? $"High ground advantage: +{hp.Elevation} to hit." : $"Firing from below: {hp.Elevation} to hit.");
            if (cover.Hunkered) GameLog.Write($"{target.Name} is hunkered down: +{targetAc - target.Ac} AC.");
            else if (cover.AcBonus > 0) GameLog.Write($"{target.Name} is in {(cover.AcBonus >= 2 ? "hard" : "light")} cover: +{targetAc - target.Ac} AC.");
            else if (cover.Flanked) GameLog.Write($"{target.Name} is flanked: +2 to hit.");
            if (hp.Aim > 0) { attacker.StatusEffects.AimBonus = 0; GameLog.Write($"Aimed shot: +{hp.Aim}."); }
            if (hp.PouchFallback) GameLog.Write($"{target.Name} has no grenade on them - a plain shot.");
            if (aimedPart != null) GameLog.Write($"Aiming for the {aimedPart.ToLowerInvariant()}: {CombatRules.AimedPenalty(aimedPart)} to hit.");
            if (hp.Range != 0) GameLog.Write($"Beyond {Name}'s range ({RangeTiles} tiles): -2 to hit.");
            string adv = hp.Advantage ? "advantage" : "standard";
            if (attacker is Player dodger && dodger.HasLeapDodgeAdvantage && target is Mutant lm && lm.Db != null && lm.Db.Leap) dodger.HasLeapDodgeAdvantage = false;   // spent
            var parts = hp.Parts.Where(x => x.v != 0).Select(x => $"{x.v:+0;-0} {x.what}").ToList();
            var result = combat.RollAttack(adv, totalMod, targetAc, parts.Count == 0 ? null : "(" + string.Join(", ", parts) + ")");
            int die = result.ChosenRoll;

            if (CombatRules.Jams(CombatRules.RollJam(), this))
            {
                IsJammed = true;
                fx.Jammed = true;
                GameLog.Write($"{Name} jams!");
                CombatRules.DegradeOnJam(this);
                return new WeaponFireResult { Hit = false, Jammed = true };
            }

            // Controlled Taps: a kept 2d10 of 14+ refunds 1 AP, once per turn.
            if (attacker is Player tapper && CombatantUtil.HasPerk(tapper, "ControlledTaps") && die >= 14 && !tapper.ControlledTapsUsed)
            {
                tapper.ControlledTapsUsed = true;
                tapper.Ap = System.Math.Min(tapper.MaxAp, tapper.Ap + 1);
                GameLog.Write("Controlled Taps triggers: regained 1 AP.");
            }
            else if (attacker is HumanEnemy boss && CombatantUtil.HasPerk(boss, "ControlledTaps") && die >= 14 && !boss.TapUsed)
            {
                boss.TapUsed = true; boss.Ap += 1;   // Batch 86: bosses use their perks too
                GameLog.Write($"{boss.Name}'s Controlled Taps: +1 AP.");
            }

            bool crit = die == CombatRules.Headshot;
            bool isShotgun = CombatRules.IsShotgun(this);
            float through = 1f;
            int missBy = targetAc - (die + totalMod);
            // Batch 105: a round the cover stopped (missed by no more than the cover's AC) goes on through wood, tin, brush
            // or cloth at half damage; concrete, stone, earth and steel stop it.
            if (!result.Hit && !crit && !isMelee && cover.HasTile && missBy >= 1 && missBy <= cover.AcBonus
                && Materials.Pierced(Materials.At(cover.TileX, cover.TileY)))
            {
                through = Materials.ThroughDamage;
                fx.CoverX = cover.TileX; fx.CoverY = cover.TileY; fx.Through = true; fx.CoverMat = Materials.At(cover.TileX, cover.TileY);
                GameLog.Write($"The round punches through the {Materials.At(cover.TileX, cover.TileY).ToString().ToLowerInvariant()} cover!");
                TerrainMap.DamageTile(cover.TileX, cover.TileY, burst || isShotgun ? 2 : 1, attacker);
            }
            else if (!result.Hit && !crit)
            {
                GameLog.Write($"{Name} misses {target.Name}.");
                // Cover Integrity: a miss by 1-3 on a covered target hits the cover instead.
                if (cover.HasTile && missBy >= 1 && missBy <= 3)
                {
                    fx.CoverX = cover.TileX; fx.CoverY = cover.TileY; fx.CoverHit = true; fx.CoverMat = Materials.At(cover.TileX, cover.TileY);   // Batch 105: the view shows the hit on the cover
                    TerrainMap.DamageTile(cover.TileX, cover.TileY, burst || isShotgun ? 2 : 1, attacker);
                }
                // Suppressed: a burst that misses you by 3 or less pins you down.
                if (burst && missBy <= 3 && CombatRules.IsHuman(target) && !target.StatusEffects.Suppressed)
                {
                    target.StatusEffects.Suppressed = true;
                    GameLog.Write($"{(target is Player ? "Player" : target.Name)} is Suppressed: -2 to attack next turn.");
                }
                return new WeaponFireResult { Hit = false };
            }
            float tierMult = CombatRules.HitMultiplier(die, die + totalMod, targetAc);
            if (aimedPart == "Head") tierMult = CombatRules.TierUp(tierMult);
            if (ambush) { tierMult = CombatRules.TierUp(tierMult); GameLog.Write($"{target.Name} never saw it coming: one hit tier higher."); }
            if (through < 1f) tierMult = CombatRules.HitMultiplier(0, -1, 0);   // through cover: always a graze, never aimed or ambush-boosted
            GameLog.Write(crit ? "Double 10 - headshot! Damage x1.5." : $"{CombatRules.HitName(tierMult)}: damage x{tierMult:0.0#}.");

            float raw = (BaseDamage + (Upgrades.Contains("Reinforced") ? 30 : 0)) * tierMult * (burst ? CombatRules.BurstMultiplier(this) : 1f) * through;
            if (ItemCondition.IsDamaged(Condition)) raw *= 0.75f;
            if (attacker is Player) raw *= CombatRules.PlayerDamage;   // Batch 127: probe knob
            raw *= CombatRules.Lethality;   // Batch 74: one knob for how deadly every gun is (player and NPCs alike); 1 = as before
            float ignore = Db?.IgnorePhysDRPercent ?? 0f;
            if (cat == "sniper" && distance >= 12 && CombatantUtil.HasPerk(attacker, "HighVelocityImpact")) ignore = 1f - (1f - ignore) * 0.8f;
            if (attacker is Player shooter && shooter.ArtifactBypassPhysDR > 0) ignore = 1f - (1f - ignore) * (1f - shooter.ArtifactBypassPhysDR);
            int damage = CombatRules.PhysicalDamage(raw, isMelee ? "Standard" : CurrentAmmoType, target.PhysDR * CombatRules.ArmorScale(target), ignore);   // Batch 75: armor-tier knob (1 = unchanged)

            // Breacher: +4 raw damage to shotgun hits within 4 tiles, applied last.
            if (isShotgun && distance <= 4 && CombatantUtil.HasPerk(attacker, "Breacher"))
            {
                damage += 4;
                GameLog.Write("Breacher: +4 shotgun damage at close range.");
            }

            // Desperate Parry (file 05): a held AP and an Agility DC 14 negate a melee hit.
            if (isMelee && target is Player parrier && parrier.ParryHeld)
            {
                parrier.ParryHeld = false;
                int pr = UnityEngine.Random.Range(1, 21) + (int)parrier.AgilityMod;
                GameLog.Write($"Desperate Parry: {pr} vs DC 14 - {(pr >= 14 ? "you knock the blow aside!" : "too slow.")}");
                if (pr >= 14) return new WeaponFireResult { Hit = false };
            }
            var shakenBefore = aimedPart == "Head" && target is HumanEnemy hv ? EnemyAI.Squad(hv).Where(o => o != hv && o.Morale == "Shaken").ToList() : null;   // Batch 57
            int hpBefore = target.Hp;
            target.Hp = System.Math.Max(0, target.Hp - damage);
            fx.Hit = true; fx.Killed = !target.IsAlive(); fx.Overkill = damage - hpBefore;
            if (target is Mutant hitMutant && !(target is HumanEnemy)) hitMutant.DamageTakenThisRound += damage;
            if (isShotgun && distance <= 4 && target.IsAlive() && CombatantUtil.HasPerk(attacker, "StaggeringBlast"))
            {
                target.StatusEffects.PendingMpDrain = 2;
                GameLog.Write($"Staggering Blast: {target.Name} reels (-2 MP next turn).");
            }
            if (isShotgun && attacker is Player && CombatantUtil.HasPerk(attacker, "BuckshotSpread"))
                BuckshotSpread(attacker, target, die + totalMod, raw, ignore);

            GameLog.Write($"{Name} hits {target.Name} for {damage} damage using {(isMelee ? "a blade" : CurrentAmmoType + " ammo")}.");
            GameLog.Write($"{target.Name} HP is now {target.Hp}.");

            CombatRules.CheckWound(target, 0, CombatRules.HitWounds(tierMult, this));   // Batch 60: vital hits and headshots wound, not raw damage
            CombatRules.ApplyAimed(target, aimedPart, tierMult);
            // Serrated Trench Knife: an extra d20, 17+ also inflicts Bleeding.
            if (Db?.Name == "Serrated Trench Knife" && target.IsAlive() && UnityEngine.Random.Range(1, 21) >= 17)
            {
                target.StatusEffects.Bleeding = true;
                GameLog.Write($"{target.Name} is bleeding from the serrated blade.");
            }

            if (!target.IsAlive()) OnTargetKilled(attacker, target);
            if (shakenBefore != null && !target.IsAlive()) EnemyAI.Execution((HumanEnemy)target, shakenBefore, attacker as Player);
            if (aimedPart == "Pouch" && tierMult >= 1f) CombatRules.CookOff(target, attacker);
            return new WeaponFireResult { Hit = true, Damage = damage };
        }

        /// <summary>Buckshot Spread (file 05): the same roll checked against one enemy next to the
        /// target. ponytail: the second target's cover isn't traced, just its AC.</summary>
        private void BuckshotSpread(ICombatant attacker, ICombatant first, int total, float raw, float ignore)
        {
            var second = EncounterState.Enemies.FirstOrDefault(e => e != first && e.IsAlive() && CombatantUtil.Distance(e, first) <= 1);
            if (second == null) return;
            if (total < second.Ac) { GameLog.Write($"The spread misses {second.Name}."); return; }
            int dmg = CombatRules.PhysicalDamage(raw, CurrentAmmoType, second.PhysDR, ignore);
            second.Hp = System.Math.Max(0, second.Hp - dmg);
            GameLog.Write($"Buckshot Spread: the spread also hits {second.Name} for {dmg}. HP {second.Hp}.");
            if (!second.IsAlive()) OnTargetKilled(attacker, second);
        }

        /// <summary>Death hooks for anything that kills a unit (a shot, a barrel blast): loot,
        /// reputation, quest progress and the Black Star's kill-AP refund. Rewards only go out
        /// when `attacker` is the player.</summary>
        public static void OnTargetKilled(ICombatant attacker, ICombatant target)
        {
            GameLog.Write($"{target.Name} has been defeated.");
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

                if (Player.Current.ArtifactKillRefundAp)
                {
                    Player.Current.Ap = System.Math.Min(Player.Current.MaxAp, Player.Current.Ap + 1);
                    GameLog.Write("The Black Star pulses: 1 AP refunded on the kill.");
                }
            }
        }

        /// <summary>Shoots at a map object instead of a unit - an explosive barrel, a door, or a
        /// piece of cover someone is hiding behind. Objects don't dodge: d20 + modifier vs AC 8.
        /// A hit takes 1 Integrity (2 for burst or shotgun, file 00 Cover Integrity). Caller has
        /// already paid the AP and checked range.</summary>
        public bool FireAtTile(int x, int y, string fireMode = null, int modifier = 0)
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
            int mod = modifier + (burst ? CombatRules.BurstPenalty(this) : 0) + CombatRules.AttackerPenalty(shooter);
            var result = GameServices.RequireCombat().RollAttack("standard", mod, ObjectAc);
            if (CombatRules.Jams(CombatRules.RollJam(), this))
            {
                IsJammed = true;
                fx.Jammed = true;
                GameLog.Write($"{Name} jams!");
                CombatRules.DegradeOnJam(this);
                return false;
            }
            if (!result.Hit)
            {
                GameLog.Write($"{Name} misses.");
                return false;
            }
            fx.Hit = true;
            TerrainMap.DamageTile(x, y, burst || CombatRules.IsShotgun(this) ? 2 : 1, shooter);
            return true;
        }

        /// <summary>Ported from reload(). Consumes from the player's global ammo reserve first, then local reserve.</summary>
        public bool Reload()
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
            int toLoad = System.Math.Min(needed, totalReserve);

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
            CombatFx.Push("reload", Owner ?? (ICombatant)Player.Current, null, this);
            GameLog.Write($"{Name} reloaded {toLoad} rounds ({consumedFromGlobal} from stash). Magazine is now {CurrentMagazine}/{MagCapacity}.");
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
