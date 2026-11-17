// Ported from script.js: class Player
//
// Fields/self-contained methods (calculateCarryWeight, applyCarryEffects,
// applyArtifactStatBonuses, addItem/removeItem, hp get/set, takeDamage, healPermanent,
// applyTemporaryHP, statusText) are fully ported below with no external dependency.
//
// move()/attack()/reloadWeapon()/clearWeaponJam() depend on the grid/combat systems ported in
// later steps (GameServices.Grid / GameServices.Combat) — their full logic is ported here too,
// not stubbed, so they'll work as soon as those services are assigned.
//
// NOTE ON A PRE-EXISTING JS QUIRK (ported faithfully, flagging for your awareness):
// The original constructor declares `this._equippedWeapon` / `this._equippedArmor` as "internal"
// fields, and calculateCarryWeight()/applyCarryEffects() read from those. But every other place
// in script.js (equipItem(), UI code, HumanEnemy) assigns to `equippedWeapon`/`equippedArmor`
// (no underscore) instead. That means the underscored fields are never actually populated, so
// equipped weapon/armor weight was never included in carry-weight calculations, and the
// "weaponClamp" branch of applyCarryEffects() always fell back to baseMaxMP. This looks like an
// unintentional bug from a partial refactor. I've reproduced the exact same behavior here (both
// fields exist; only the non-underscored ones are ever set) so nothing changes silently — let me
// know if you'd like it fixed instead of preserved.
using System.Collections.Generic;
using System.Linq;
using GameData;

namespace GameCore
{
    public class ArmorMods
    {
        public string SlotA;
        public string SlotB;
    }

    public partial class Player : ICombatant
    {
        // Singleton-style global reference, mirroring `const player = new Player();` in script.js
        public static Player Current;

        public string Name => "Player";
        /// <summary>RT 7x: the name the player gave the character at creation (saved in its character file).</summary>
        public string CharacterName = "Scavenger";
        public string Faction => "Player";

        public int X { get; set; } = 2;
        public int Y { get; set; } = 2;

        // World-space position used for overworld squad engagement and migration.
        public int WorldX { get; set; } = 8;
        public int WorldY { get; set; } = 8;

        public int MaxHp = 1000;   // real-time fork: HP x10 (Yan: finer damage numbers)
        public int PermHP = 1000;
        public int TempHP = 0;
        public int TempHPDuration = 0;
        public StatusEffects StatusEffects { get; } = new StatusEffects();

        public int MaxMp { get; set; } = 4;

        public StatBlock Stats = new StatBlock();

        // Equipment (see class-level note above re: the underscored fields being unused-on-purpose,
        // reproducing the original's behavior).
        private Weapon _equippedWeapon;   // never actually assigned in the original either
        private ArmorDef _equippedArmor;  // never actually assigned in the original either
        public Weapon EquippedWeapon;     // the field actually used throughout
        public ArmorDef EquippedArmor;    // the field actually used throughout

        public float BasePhysDR;
        /// <summary>Armor Phys-DR, +20% while Entrenched (didn't move this turn, file 05).</summary>
        public float PhysDR
        {
            get => BasePhysDR + (MovedThisTurn || !EncounterState.InCombat || !CombatantUtil.HasPerk(this, "Entrenched") ? 0f : 0.2f);
            set => BasePhysDR = value;
        }
        public float AnomDR { get; set; } = 0f;
        // AC = 10 + rank modifier, minimum 10 (file 04). Cover / Take Cover are added per attack.
        public int Ac { get => CombatRules.RankAc(Rank); set { } }

        // New (file 00/01 combat rules)
        public string FireMode = "Single";      // toggled with X; Burst only on AR/SMG/heavy
        public int ArmorModMpBonus;             // set by ApplyArmorModsForPlayer, read by ApplyCarryEffects
        public string ArmedThrowable;           // consumable id of a grenade waiting for a target click

        public object ActiveDetector { get; set; } // DetectorDef once equipped; typed loosely to match JS's easy null-checks
        // New (Batch 8): which DetectorDatabase key ActiveDetector was equipped from - needed to
        // find the matching ShopItemDef again when swapping detectors (mirrors
        // EquippedWeaponCategory/Key's reasoning, see Player.Equipment.cs).
        public string ActiveDetectorKey;
        public string[] ArtifactSlots = new string[5];

        /// <summary>Set when the player successfully dodges a Dread Prowler's Leap Attack
        /// (Circle & Leap mechanic) - consumed by the next shot Weapon.Fire resolves.
        /// ponytail: doesn't actually expire "this round" if the player never fires again this
        /// combat - it just carries over. Small enough to live with rather than adding a
        /// round-counter to clear it.</summary>
        public bool HasLeapDodgeAdvantage;

        /// <summary>Per-faction standing, keyed by FactionDatabase's normalized faction names.
        /// Missing entries default to 0 (Neutral) via GetRepLevel - nothing needs to pre-populate
        /// this dictionary. Independent from FactionDatabase.HostilityMatrix, which still governs
        /// whether that faction's squads attack on sight in the field; this is the separate social/
        /// trade-access axis the Zone Map's Faction Outposts and Quest Bank content needs. Nothing
        /// currently raises reputation above 0 - that's quest-reward content (Batch 6+), so
        /// Friendly-gated traders are correctly, if unavoidably, locked until that exists.</summary>
        public Dictionary<string, int> Reputation = new Dictionary<string, int>();

        /// <summary>"Hostile" (-50 or less), "Neutral", or "Friendly" (50+). File 00 §12 counts
        /// reputation in points where ≤ -3 is Hostile and ≥ +3 Friendly; here one doc point is
        /// RepPoint (17) so the same thresholds land on ±50.</summary>
        public string GetRepLevel(string faction)
        {
            faction = FactionDatabase.NormalizeFactionName(faction);
            int score = Reputation.TryGetValue(faction, out var s) ? s : 0;
            return RepLevelForScore(score);
        }

        public const int RepPoint = 17;
        private static string RepLevelForScore(int score) => score <= -50 ? "Hostile" : score >= 50 ? "Friendly" : "Neutral";

        /// <summary>Standing in the doc's points (e.g. +2), for display.</summary>
        public int RepPoints(string faction)
        {
            faction = FactionDatabase.NormalizeFactionName(faction);
            int score = Reputation.TryGetValue(faction, out var s) ? s : 0;
            return (int)System.Math.Round(score / (double)RepPoint);
        }

        /// <summary>New (Batch 5): the only writer Reputation ever had. Clamped to [-100,100] so a
        /// grudge or a friendship can't run away forever. Three things move it right now: combat
        /// against a faction's NPCs (Weapon.Fire's death hook, negative), trading with a faction's
        /// Quartermaster (ShopSystem, +1/deal), and a slow 1-point/travel drift back toward Neutral
        /// (ZoneTravel.TravelToSector) so one fight doesn't lock a faction Hostile forever with no
        /// recourse before the Quest Bank (Batch 6) exists to hand out reputation as a reward.
        /// Logs only on a level change (Hostile/Neutral/Friendly), not every single point.</summary>
        public void AdjustReputation(string faction, int delta)
        {
            if (string.IsNullOrEmpty(faction) || delta == 0) return;
            faction = FactionDatabase.NormalizeFactionName(faction);
            int before = Reputation.TryGetValue(faction, out var s) ? s : 0;
            int after = System.Math.Max(-100, System.Math.Min(100, before + delta));
            if (after == before) return;
            Reputation[faction] = after;
            if (RepLevelForScore(before) != RepLevelForScore(after))
                GameLog.Write($"Reputation with the {faction} is now {RepLevelForScore(after)} ({after}).");
        }

        public int Ru = 1000;
        public PlayerInventory Inventory = new PlayerInventory();
        public float CarryCapacityBonus = 0;
        public ArmorMods ArmorMods = new ArmorMods();
        public float CurrentCarryWeight = 0;

        public string Rank = "Rookie";
        public int Exp = 0;
        public List<string> ActivePerks { get; } = new List<string>();
        public bool CombatReloadFree = false;

        public float Hunger = 10;
        public float Thirst = 10;
        public float Tiredness = 10;

        // Artifact-derived bonuses, populated by ApplyArtifactStatBonuses()
        public float ArtifactAnomPenalty;
        public float ArtifactCarryBonus;
        public float ArtifactRegen;
        /// <summary>Batch 90: a worn artifact that stops bleeding (Rosary, Heart of the Wound) - it only cleared bleeding once, when put on.</summary>
        public bool BleedImmune => ArtifactSlots.Any(id => id != null && GameData.ArtifactDatabase.All.TryGetValue(id, out var a) && (a.ImmuneBleed || a.BleedNegate == true));
        public float ArtifactMpBonus;
        public float ArtifactAvoidanceBonus { get; set; }
        public bool ArtifactAutoPassAnomalies { get; set; }
        // New (Batch 8): Lucifer's flat Phys/Anom-DR bonus and The Black Star's Phys-DR bypass/
        // kill-AP-refund - these ArtifactDef fields existed but nothing ever read them.
        public float ArtifactPhysDRBonus;
        public float ArtifactAnomDRBonus;
        public float ArtifactBypassPhysDR;

        public Player() : this(true) { }

        /// <summary>Co-op groundwork: every player in the game - the local one (Current) first. A new local player (New Game,
        /// a load) starts the list over; Join adds another player beside you (the host's view of a friend, or the harness).</summary>
        public static readonly System.Collections.Generic.List<Player> All = new System.Collections.Generic.List<Player>();
        Player(bool local)
        {
            if (local) { Current = this; All.Clear(); }
            All.Add(this);
        }
        public static Player Join() => new Player(false);

        /// <summary>Ported from calculateCarryWeight().</summary>
        public float CalculateCarryWeight()
        {
            float weight = 0;
            foreach (var it in Inventory.Items)
                weight += it.WeightKg * (it.Qty > 0 ? it.Qty : 1);

            // Fix: these read _equippedWeapon/_equippedArmor, which nothing ever assigns - the gear
            // you were actually carrying never counted toward carry weight.
            if (EquippedWeapon?.Db?.WeightKg != null)
                weight += EquippedWeapon.Db.WeightKg.Value;
            if (OffhandWeapon?.Db?.WeightKg != null)
                weight += OffhandWeapon.Db.WeightKg.Value;
            if (SecondaryWeapon?.Db?.WeightKg != null)
                weight += SecondaryWeapon.Db.WeightKg.Value;
            if (EquippedArmor?.WeightKg != null)
                weight += System.Math.Max(0f, EquippedArmor.WeightKg.Value - (EquippedArmorUpgrades.Contains("Lightened") ? 3f : 0f));

            foreach (var artifactId in ArtifactSlots)
            {
                if (string.IsNullOrEmpty(artifactId)) continue;
                if (ArtifactDatabase.All.TryGetValue(artifactId, out var art))
                    weight += art.WeightKg;
            }

            CurrentCarryWeight = weight;
            return weight;
        }

        /// <summary>Ported from isDepleted().</summary>
        public bool IsDepleted() => Hunger <= 0 || Thirst <= 0 || Tiredness <= 0;

        /// <summary>Ported from applyCarryEffects().</summary>
        public void ApplyCarryEffects()
        {
            float baseCap = CombatantUtil.HasPerk(this, "PackMule") ? 50f : Inventory.CarryCapacityBase;
            float totalCap = baseCap + CarryCapacityBonus + ArtifactCarryBonus;
            CalculateCarryWeight();

            // Fix: same dead fields as above - armor MP and the heavy-weapon MP cap were ignored and
            // every equip ended with MaxMp reset to 4. File 00: armor MP (4 unarmored) + 1 at Expert
            // or higher + artifacts + mods; heavy weapons cap at 2; a Leg wound costs 2 (min 1).
            float baseMaxMP = (EquippedArmor != null && EquippedArmor.Mp > 0 ? EquippedArmor.Mp : 4) + ArmorModMpBonus + ArtifactMpBonus
                + (CombatRules.RankIndex(Rank) >= CombatRules.RankIndex("Expert") ? 1 : 0);
            float weaponClamp = CombatantUtil.HasPerk(this, "WalkingTank") ? baseMaxMP : EquippedWeapon?.Db?.MaxMpWhileHeld ?? baseMaxMP;

            bool isStarving = IsDepleted();
            if (CurrentCarryWeight > totalCap)
            {
                int overweightDivisor = isStarving ? 4 : 2;
                MaxMp = System.Math.Max(2, (int)System.Math.Floor(System.Math.Min(baseMaxMP, weaponClamp) / overweightDivisor));
            }
            else
            {
                MaxMp = (int)System.Math.Min(baseMaxMP, weaponClamp);
            }

            if (StatusEffects.Wound == "Leg") MaxMp = System.Math.Max(1, MaxMp - 2);
            // Batch 76 (Yan): Bull Rush - a melee weapon in hand doubles your MP. Swapping to a gun mid-turn takes the extra
            // MP back off what you have left (so you can't run on the knife's MP and then shoot); swapping to the blade
            // mid-turn gives nothing until your next turn.
            // Batch 78 (Yan): any blade in hand is +2 MP; Bull Rush then doubles that
            bool blade = EquippedWeapon != null && CombatRules.IsMelee(EquippedWeapon);
            int rush = blade ? 2 + (CombatantUtil.HasPerk(this, "BullRush") ? MaxMp + 2 : 0) : 0;
            RushBonus = rush;
            MaxMp += rush;
        }

        /// <summary>Batch 76: the part of MaxMp that Bull Rush adds right now.</summary>
        public int RushBonus;

        public void ApplyWoundEffects() => ApplyCarryEffects();

        /// <summary>Armor drops one Condition step on a 20+ hit or a natural-20 hit (file 00).</summary>
        public void WearArmor()
        {
            if (EquippedArmor == null || ItemCondition.IsBroken(EquippedArmorCondition)) return;
            if (EquippedArmorUpgrades.Contains("Braced") && !BracedUsed) { BracedUsed = true; GameLog.Write($"The bracing on your {EquippedArmor.Name} takes the hit."); return; }
            EquippedArmorCondition = ItemCondition.Worse(EquippedArmorCondition);
            GameLog.Write($"Your {EquippedArmor.Name} is now {EquippedArmorCondition}.");
            ApplyArmorModsForPlayer();
        }
        public void ApplyArtifactStatBonuses()
        {
            float totalAnomPenalty = 0;
            float carryBonus = 0;
            float regenBonus = 0;
            float mpBonusAdd = 0;
            float anomalyAvoidanceAdd = 0;
            float physDRBonusAdd = 0;
            float anomDRBonusAdd = 0;
            float bypassPhysDRAdd = 0;
            ArtifactAutoPassAnomalies = false;
            bool hasArtifactHandlerPerk = CombatantUtil.HasPerk(this, "ArtifactHandler");
            MaxHp = 1000 + BonusMaxHp;   // was never reset: taking Lucifer off left you at -25 Max HP for good

            // File 03: in each family only the strongest worn artifact gives its bonus; extras
            // still cost their Anom-DR penalty. Each legendary is its own family. (The old
            // one-legendary limit isn't in the docs and is gone.)
            var worn = ArtifactSlots.Where(id => !string.IsNullOrEmpty(id) && ArtifactDatabase.All.ContainsKey(id))
                .Select(id => (id, art: ArtifactDatabase.All[id])).ToList();
            var best = worn.GroupBy(w => w.art.Type == "Legendary" ? w.id : w.art.Type)
                .Select(g => g.OrderByDescending(w => w.art.Tier).First().id).ToList();
            foreach (var (artifactId, art) in worn)
            {
                float penalty = art.AnomPenalty;
                if (hasArtifactHandlerPerk) penalty /= 2;
                totalAnomPenalty += penalty;
                if (!best.Remove(artifactId)) continue;   // a weaker one of a family already counted

                if (art.CarryWeightBonus.HasValue) carryBonus += art.CarryWeightBonus.Value;
                if (art.HpRegen.HasValue) regenBonus += art.HpRegen.Value;
                if (art.MpBonus.HasValue) mpBonusAdd += art.MpBonus.Value;
                if (art.AnomalyAvoidanceBonus.HasValue) anomalyAvoidanceAdd += art.AnomalyAvoidanceBonus.Value;
                if (art.PassAllAnomalies) ArtifactAutoPassAnomalies = true;
                // New (Batch 8): BleedNegate (Clot/Scab Stone/Rosary) was set on data but
                // never read - only true (Rosary) actually negates bleed, same one-shot-clear
                // effect ImmuneBleed already has below; false (the other two) has no numeric
                // reduction in the source data to wire, so it stays flavor-only.
                if (art.ImmuneBleed || art.BleedNegate == true) StatusEffects.Bleeding = false;
                if (art.MaxHpPenalty.HasValue) MaxHp = System.Math.Max(500, MaxHp - art.MaxHpPenalty.Value);
                if (art.PhysDRBonus.HasValue) physDRBonusAdd += art.PhysDRBonus.Value;
                if (art.AnomDRBonus.HasValue) anomDRBonusAdd += art.AnomDRBonus.Value;
                if (art.BypassPhysDR.HasValue) bypassPhysDRAdd += art.BypassPhysDR.Value;
            }

            if (PermHP > MaxHp) PermHP = MaxHp;   // Lucifer's -25 Max HP has to actually cost HP
            ArtifactAnomPenalty = totalAnomPenalty;
            ArtifactCarryBonus = carryBonus;
            ArtifactRegen = regenBonus;
            ArtifactMpBonus = mpBonusAdd;
            ArtifactAvoidanceBonus = anomalyAvoidanceAdd;
            ArtifactPhysDRBonus = physDRBonusAdd;
            ArtifactAnomDRBonus = anomDRBonusAdd;
            ArtifactBypassPhysDR = bypassPhysDRAdd;

            // PhysDR/AnomDR are otherwise owned by ApplyArmorModsForPlayer (recomputed from scratch
            // on every armor change) - route through it here too so the artifact bonus isn't wiped
            // out the next time armor/mods change, instead of adding it on top just this once.
            ApplyArmorModsForPlayer();
        }

        /// <summary>Ported from addItem(item).</summary>
        public void AddItem(InventoryItem item)
        {
            // loose rounds go straight into the ammo stash (Key = ammo type)
            if (item.Type == "ammo")
            {
                string t = Ammo.Normalize(this, string.IsNullOrEmpty(item.Key) ? "Standard" : item.Key);
                Ammo.Add(Inventory, t, System.Math.Max(1, item.Qty));
                GameLog.Write($"+{item.Qty} {Ammo.Label(t)}.");
                return;
            }
            // a worn or upgraded copy is its own item, not one more of the same stack - and gets its
            // own id, since Sell/Equip/Drop buttons find items by id
            var existing = Inventory.Items.FirstOrDefault(i => i.Id == item.Id && i.Condition == item.Condition && i.Upgrades == item.Upgrades);
            if (existing != null) existing.Qty += item.Qty > 0 ? item.Qty : 1;
            else
            {
                var copy = item.Clone();
                if (Inventory.Items.Any(i => i.Id == copy.Id))
                {
                    int n = 2;
                    while (Inventory.Items.Any(i => i.Id == $"{item.Id}~{n}")) n++;
                    copy.Id = $"{item.Id}~{n}";
                }
                Inventory.Items.Add(copy);
                if (!Pack.Place(this, copy)) GameLog.Write($"No room in your pack for the {copy.Name} - make room or drop something (I).");   // Pack 1
            }
            ApplyCarryEffects();
        }

        /// <summary>Ported from removeItem(itemId, qty=1).</summary>
        public bool RemoveItem(string itemId, int qty = 1)
        {
            int idx = Inventory.Items.FindIndex(i => i.Id == itemId);
            if (idx < 0) return false;
            var it = Inventory.Items[idx];
            if (it.Qty <= qty) Inventory.Items.RemoveAt(idx);
            else it.Qty -= qty;
            ApplyCarryEffects();
            return true;
        }

        /// <summary>New (Batch 12): same as RemoveItem, but what's removed actually lands
        /// somewhere - a GroundItem at the player's own tile in the current zone - instead of just
        /// disappearing. What InventoryUI's Drop button calls now (e.g. to shed weight when
        /// overweight without losing the item for good).</summary>
        public bool DropItem(string itemId, int qty = 1)
        {
            var it = Inventory.Items.FirstOrDefault(i => i.Id == itemId);
            if (it == null) return false;

            int dropped = System.Math.Min(qty > 0 ? qty : 1, it.Qty);
            var groundCopy = it.Clone();
            groundCopy.Qty = dropped;

            if (!RemoveItem(itemId, dropped)) return false;

            TerrainMap.DropItem(X, Y, groundCopy);
            GameLog.Write($"Dropped {groundCopy.Name} x{dropped} on the ground.");
            return true;
        }

        /// <summary>Ported from the `get hp()` / `set hp(value)` accessors.</summary>
        public int Hp
        {
            get => System.Math.Min(MaxHp, PermHP + TempHP);
            set
            {
                int total = System.Math.Max(0, System.Math.Min(MaxHp, value));
                int current = Hp;
                if (total == current) return;
                if (total < current)
                {
                    TakeDamage(current - total);
                    return;
                }
                int heal = total - current;
                PermHP = System.Math.Min(MaxHp, PermHP + heal);
            }
        }

        /// <summary>Ported from takeDamage(amount).</summary>
        public void TakeDamage(int amount)
        {
            amount = System.Math.Max(0, amount);
            if (TempHP >= amount)
            {
                TempHP -= amount;
                return;
            }
            amount -= TempHP;
            TempHP = 0;
            if (Rt.On && !Body.Down(this) && PermHP - amount <= 0) { PermHP = 1; Body.GoDown(this); return; }   // RT batch 2: down, not dead
            PermHP = System.Math.Max(0, PermHP - amount);
        }

        public int StrengthMod => CombatantUtil.GetStatModifier(Stats.S);
        public float AgilityMod => CombatantUtil.GetStatModifier(Stats.A);
        public int IntelligenceMod => CombatantUtil.GetStatModifier(Stats.I);
        public int PerceptionMod => CombatantUtil.GetStatModifier(Stats.P);

        /// <summary>Ported from healPermanent(amount).</summary>
        public void HealPermanent(int amount)
        {
            if (amount <= 0) return;
            PermHP = System.Math.Min(MaxHp, PermHP + amount);
            Body.Heal(this, amount);   // RT batch 2: the parts heal too
        }

        /// <summary>Ported from applyTemporaryHP(amount).</summary>
        public void ApplyTemporaryHP(int amount)
        {
            if (amount <= 0) return;
            int maxTemp = System.Math.Max(0, MaxHp - PermHP);
            TempHP = System.Math.Min(maxTemp, System.Math.Max(TempHP, amount));
            TempHPDuration = CombatantUtil.HasPerk(this, "IronWill") ? 8 : 4;
        }

        /// <summary>Ported from the `get statusText()` accessor.</summary>
        public string StatusText
        {
            get
            {
                var statuses = new List<string>();
                if (StatusEffects.Bleeding) statuses.Add("Bleeding");
                if (StatusEffects.ToxicBleed) statuses.Add("Toxic Bleed");
                if (StatusEffects.Irradiated) statuses.Add("Irradiated");
                if (StatusEffects.Prone) statuses.Add("Prone");
                if (Sneaking) statuses.Add("Sneaking");
                if (StatusEffects.Wound != null) statuses.Add($"{StatusEffects.Wound} wound");
                if (StatusEffects.Suppressed) statuses.Add("Suppressed");
                if (StatusEffects.AimBonus > 0) statuses.Add($"Aiming +{StatusEffects.AimBonus}");
                if (ActivePerks.Count > 0) statuses.Add($"Perks: {string.Join(", ", ActivePerks)}");
                return statuses.Count > 0 ? string.Join(", ", statuses) : "Healthy";
            }
        }

        public bool IsAlive() => Hp > 0;

        /// <summary>
        /// Ported from move(dx, dy). Depends on GameState (turn check), GameServices.Grid
        /// (attemptMove), and ambient-spawn/UI hooks from later steps.
        /// </summary>
        public void Move(int dx, int dy)
        {
            if (GameState.Current.CurrentTurn != Turn.Player)
            {
                GameLog.Write("Wait until your turn to move.");
                return;
            }

            if (StatusEffects.Prone)
            {
                StatusEffects.Prone = false;
                GameLog.Write("You stand up from prone.");
            }

            int newX = X + dx;
            int newY = Y + dy;

            // NOTE: bounds checked against GRID_WIDTH/GRID_HEIGHT in the original — those constants
            // live in the grid step; GameServices.Grid.AttemptMove is expected to cover this too.
            if (!GameServices.RequireGrid().AttemptMove(this, newX, newY))
                return;

        }



        /// <summary>Start-of-turn refresh shared by the turn loop and the end of a fight: MP back
        /// to full, per-turn trackers reset, Take Cover / Evasive bonuses expire.</summary>
        public void StartTurn()
        {
            ApplyCarryEffects();
            MovedThisTurn = HitAndRunUsed = RunAndGunUsed = ParryHeld = false;
            MeleeAttacksThisTurn = SidearmShotsThisTurn = 0;
            if (!EncounterState.InCombat) AdrenalineUsed = BracedUsed = false;
        }

        /// <summary>End of the player's turn: an unused Aim is lost, Suppression wears off.</summary>
        public void EndTurn()
        {
            StatusEffects.AimBonus = 0;
            StatusEffects.Suppressed = false;
            if (EncounterState.InCombat)
            {
                if (CombatantUtil.HasPerk(this, "DesperateParry"))
                { ParryHeld = true; GameLog.Write("Desperate Parry: ready to parry."); }
            }
            TerrainMap.TickSmoke();
            if (EncounterState.InCombat) WorldEvents.TickSurge();   // exploring, TurnLoop.TickAwareness counts tiles instead
        }

        /// <summary>Q: switch the loaded ammo to the next type you have rounds for (Standard >
        /// Hollow Point > Dart > Armor Piercing). The rounds in the magazine go back to the stash
        /// and you reload with the new type (normal reload AP).</summary>
        public void CycleAmmoType()
        {
            var w = EquippedWeapon;
            if (w == null || CombatRules.IsMelee(w)) { GameLog.Write("Nothing to load."); return; }
            string[] order = { "Standard", "HollowPoint", "Dart", "ArmorPiercing" };
            int start = System.Array.IndexOf(order, w.CurrentAmmoType);
            for (int i = 1; i <= order.Length; i++)
            {
                string next = order[(start + i + order.Length) % order.Length];
                if (next == w.CurrentAmmoType) break;
                if (Ammo.Get(Inventory, Ammo.Key(w.Db, next)) <= 0) continue;
                Ammo.Add(Inventory, Ammo.Key(w.Db, w.CurrentAmmoType), w.CurrentMagazine);
                w.CurrentMagazine = 0;
                w.CurrentAmmoType = next;
                GameLog.Write($"Loading {next} rounds.");
                ReloadWeapon();
                return;
            }
            GameLog.Write("You have no other ammo type to load.");
        }

        /// <summary>Puts an artifact from the inventory on the belt: first free slot, or swaps slot 1.</summary>
        public void EquipArtifactFromInventory(string itemId)
        {
            int index = Inventory.Items.FindIndex(i => i.Id == itemId && i.Type == "artifact");
            if (index < 0) return;
            int slots = System.Math.Max(1, EquippedArmor != null ? TotalBeltSlots : 1);
            int free = System.Array.FindIndex(ArtifactSlots, sl => string.IsNullOrEmpty(sl));
            EquipArtifact(index, free >= 0 && free < slots ? free : 0);
        }

        /// <summary>Fits an armor mod from the inventory into a free mod slot of the worn armor
        /// (swapping out the first mod if both are full).</summary>
        public void FitArmorMod(string itemId)
        {
            var item = Inventory.Items.FirstOrDefault(i => i.Id == itemId && i.Type == "armorMod");
            if (item == null) return;
            if (EquippedArmor == null || EquippedArmor.ModSlots <= 0) { GameLog.Write("Your armor has no mod slots."); return; }
            string key = item.Key ?? ArmorModDatabase.All.FirstOrDefault(kv => kv.Value.Name == item.Name).Key;
            if (key == null) { GameLog.Write("Unknown mod."); return; }
            string slot = string.IsNullOrEmpty(ArmorMods.SlotA) ? "slotA" : EquippedArmor.ModSlots >= 2 && string.IsNullOrEmpty(ArmorMods.SlotB) ? "slotB" : "slotA";
            string old = slot == "slotA" ? ArmorMods.SlotA : ArmorMods.SlotB;
            RemoveItem(itemId, 1);
            if (!string.IsNullOrEmpty(old) && ArmorModDatabase.All.TryGetValue(old, out var oldDef))
                AddItem(new InventoryItem { Id = old, Key = old, Name = oldDef.Name, Type = "armorMod", Qty = 1, WeightKg = 0.3f });
            HandleArmorModChange(slot, key);
            GameLog.Write($"Fitted {item.Name} to your {EquippedArmor.Name}.");
        }

        public void EquipBackpackFromInventory(string itemId)
        {
            var item = Inventory.Items.FirstOrDefault(i => i.Id == itemId && i.Type == "backpack");
            if (item == null) return;
            RemoveItem(itemId, 1);
            HandleBackpackChange(itemId);
            GameLog.Write($"You shoulder the {item.Name}.");
        }

        /// <summary>Field Repair Kit (file 05): out of combat, +1 Condition step to your weapon -
        /// or your armor if the weapon is fine - never above Dirty.</summary>
        public bool UseRepairKit()
        {
            if (EncounterState.InCombat) { GameLog.Write("No time for repairs mid-fight."); return false; }
            var w = EquippedWeapon;
            if (w != null && Economy.ConditionSteps(w.Condition) >= 2)
            {
                w.Condition = w.Condition == ItemCondition.Broken ? ItemCondition.Damaged : ItemCondition.Dirty;
                GameLog.Write($"You patch up your {w.Name}: now {w.Condition}.");
                return true;
            }
            if (EquippedArmor != null && Economy.ConditionSteps(EquippedArmorCondition) >= 2)
            {
                EquippedArmorCondition = EquippedArmorCondition == ItemCondition.Broken ? ItemCondition.Damaged : ItemCondition.Dirty;
                ApplyArmorModsForPlayer();
                GameLog.Write($"You patch up your {EquippedArmor.Name}: now {EquippedArmorCondition}.");
                return true;
            }
            GameLog.Write("Nothing a field kit can fix - a kit never gets gear above Dirty. See a technician.");
            return false;
        }


        /// <summary>Batch 66: N - sneak. Moving costs double (MP in a fight, minutes out of one); enemies see you at half range.</summary>
        public bool Sneaking;
        public bool FlashlightOn = true;   // Batch 92: U; off in the dark, enemies see you at 60% (LightMap.Exposure)
        public void ToggleFlashlight()
        {
            FlashlightOn = !FlashlightOn;
            GameLog.Write(FlashlightOn ? "Flashlight on." : "Flashlight off: harder to spot in the dark, and you see only what's lit.");
        }
        public void ToggleSneak()
        {
            Sneaking = !Sneaking;
            GameLog.Write(Sneaking ? "You move quietly: half speed, enemies spot you at half range." : "You stop sneaking.");
        }


        public bool NearCover()
        {
            if (TerrainMap.CoverAcAt(X, Y) > 0) return true;
            foreach (var adj in TerrainMap.GetAdjacentCoords(X, Y)) if (TerrainMap.CoverAcAt(adj.x, adj.y) > 0) return true;
            return false;
        }

        /// <summary>Ported from reloadWeapon().</summary>
        public void ReloadWeapon()
        {
            if (GameState.Current.CurrentTurn != Turn.Player)
            {
                GameLog.Write("Cannot reload outside your turn.");
                return;
            }

            var weapon = EquippedWeapon;
            bool reloaded = weapon.Reload();
            if (Gunslinging && OffhandWeapon.CurrentMagazine < OffhandWeapon.MagCapacity) reloaded |= OffhandWeapon.Reload();
        }

        /// <summary>Ported from clearWeaponJam().</summary>
        public void ClearWeaponJam()
        {
            if (GameState.Current.CurrentTurn != Turn.Player)
            {
                GameLog.Write("Cannot clear jam outside your turn.");
                return;
            }
            var weapon = EquippedWeapon;
            bool cleared = weapon.ClearJam();
        }
    }
}
