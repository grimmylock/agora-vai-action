// Ported from script.js: equipItem(type, value), applyMpConstraintsAfterEquip(), useConsumable(itemId),
// applyArmorModsForPlayer(), handleArmorModChange(slot, modId), handleBackpackChange(backpackId).
//
// BUG FIX (diverges from the original on purpose — see note below): the original
// applyArmorModsForPlayer() does `player.equippedArmor.beltSlots += beltBonus`, mutating the
// shared ARMOR_DB entry object directly rather than a player-local value. Since JS objects are
// references, every re-application of armor mods permanently increments the *global* armor
// definition's belt slots — compounding forever, and affecting every future wearer of that armor
// (including enemies, if the same ARMOR_DB entries are ever reused for them). That's data
// corruption, not just a wrong number, so unlike the other quirks I've flagged and preserved
// elsewhere in this port, I fixed this one: the mod-derived belt-slot bonus is tracked separately
// on the Player (ArmorModBeltSlotBonus) instead of mutating the shared ArmorDef. Behavior is
// otherwise identical.
using System.Linq;
using GameData;

namespace GameCore
{
    public partial class Player
    {
        public int ArmorModBeltSlotBonus; // see BUG FIX note above

        // New: which database entry is currently worn, so EquipWeaponFromInventory/
        // EquipArmorFromInventory below can stash the outgoing piece back into the inventory
        // instead of it just vanishing when the player switches. Set inside EquipWeapon/EquipArmor
        // themselves so the starting loadout (GameBootstrap calling them directly) populates these
        // too, not just inventory-driven swaps.
        public string EquippedWeaponCategory;
        public string EquippedWeaponKey;
        public string EquippedArmorTier;
        public string EquippedArmorKey;

        // New: mirrors InventoryItem.Condition (ItemCondition.cs) for the currently worn armor -
        // ArmorDef itself is a shared database entry with no per-player instance to hang a
        // condition field off of (unlike EquippedWeapon, an actual Weapon object), so it lives here
        // instead. Player.SaveLoad.cs already reads/writes this on every save/load round-trip.
        // Defaults to Working (pristine) so a freshly-equipped-from-database armor (EquipArmor's
        // direct db lookup, GameBootstrap's starting loadout) needs no explicit initialization.
        public string EquippedArmorCondition = ItemCondition.Working;
        public System.Collections.Generic.List<string> EquippedArmorUpgrades = new System.Collections.Generic.List<string>();   // technician upgrades (Upgrades.cs)
        public bool BracedUsed;   // Braced: the first Condition loss each fight is ignored

        /// <summary>Total usable belt slots: base armor slots + any belt-slot armor mod bonus.</summary>
        public int TotalBeltSlots => (EquippedArmor?.BeltSlots ?? 0) + ArmorModBeltSlotBonus;

        /// <summary>Ported from equipItem('weapon', 'category|key').</summary>
        public void EquipWeapon(string category, string key)
        {
            var db = WeaponDatabase.Get(category, key);
            if (db == null)
            {
                GameLog.Write($"No weapon found for {category}/{key}.");
                return;
            }

            // Fix: melee weapons (HuntingKnife, SpetsnazSwitchblade...) have neither
            // MagCapacity nor CylinderCapacity, so this used to compute mag=0 - CurrentMagazine=0
            // then failed every ammo check in Weapon.Fire/Player.Attack, making melee unusable.
            // A synthetic 1-round magazine that Weapon.Fire never actually consumes (see its
            // isMelee guard) fixes it without needing a whole separate melee attack path.
            int mag = db.MagCapacity ?? db.CylinderCapacity ?? 1;
            var w = new Weapon(
                string.IsNullOrEmpty(db.Name) ? key : db.Name,
                db.BaseDamage > 0 ? db.BaseDamage : 1,
                mag,
                "Standard",
                new System.Collections.Generic.Dictionary<string, int> { ["Standard"] = 0, ["HollowPoint"] = 0, ["ArmorPiercing"] = 0 });   // was a hidden free magazine
            w.Owner = this;
            w.Db = db;
            w.RangeTiles = RangeUtil.ParseRangeToTiles(db.Range);
            EquippedWeapon = w;
            EquippedWeaponCategory = category;
            EquippedWeaponKey = key;

            GameLog.Write($"Equipped weapon: {w.Name} (range {w.RangeTiles} tiles).");
            ApplyMpConstraintsAfterEquip();
            ApplyCarryEffects();
        }

        /// <summary>Ported from equipItem('armor', 'tier|key'). condition defaults to Working (a
        /// fresh database equip has no wear info) - EquipArmorFromInventory passes the source
        /// inventory item's own Condition through instead.</summary>
        public void EquipArmor(string tier, string key, string condition = null)
        {
            var db = ArmorDatabase.Get(tier, key);
            if (db == null)
            {
                GameLog.Write($"No armor found for {tier}/{key}.");
                return;
            }

            EquippedArmor = db;
            EquippedArmorTier = tier;
            EquippedArmorKey = key;
            EquippedArmorCondition = string.IsNullOrEmpty(condition) ? ItemCondition.Working : condition;
            PhysDR = db.PhysDR;
            AnomDR = db.AnomDR;
            MaxMp = db.Mp > 0 ? db.Mp : 4;

            if (EquippedWeapon != null && EquippedWeapon.Db != null && EquippedWeapon.Db.MaxMpWhileHeld.HasValue && !CombatantUtil.HasPerk(this, "WalkingTank"))
                MaxMp = System.Math.Min(MaxMp, EquippedWeapon.Db.MaxMpWhileHeld.Value);

            if (Mp > MaxMp + RushBonus) Mp = MaxMp + RushBonus;   // Batch 76: ApplyCarryEffects settles the Bull Rush MP

            GameLog.Write($"Equipped armor: {db.Name}. Phys-DR {PhysDR}, Anom-DR {AnomDR}, Max MP {MaxMp}.");
            ApplyArmorModsForPlayer();
            ApplyCarryEffects();
        }

        /// <summary>New: equip a weapon the player actually owns (an inventory item with
        /// Type "weapon"), unlike EquipWeapon(category,key) above which equips anything that
        /// exists in WeaponDatabase regardless of ownership - that direct form is left as-is since
        /// GameBootstrap's starting loadout and any future dev/cheat code still wants it. The
        /// outgoing weapon (if any) goes back into the inventory, reconstructed from
        /// ShopDatabase's auto-generated entries so there's one source of truth for its
        /// name/weight/price rather than a second hand-typed copy here.</summary>
        public void EquipWeaponFromInventory(string itemId)
        {
            var item = Inventory.Items.FirstOrDefault(i => i.Id == itemId && i.Type == "weapon");
            if (item == null) { GameLog.Write("You don't own that weapon."); return; }

            string category = item.Category, key = item.Key;
            var upgrades = Upgrades.Parse(item.Upgrades);
            var previousUpgrades = EquippedWeapon?.Upgrades;
            // New: carried through onto the equipped Weapon below (see Weapon.Condition) - without
            // this, swapping in a looted Dirty/Damaged weapon silently reset it to pristine.
            string condition = item.Condition;
            string previousCategory = EquippedWeaponCategory, previousKey = EquippedWeaponKey;
            string previousCondition = EquippedWeapon?.Condition;
            if (!RemoveItem(itemId, 1)) return;
            if (category != "sidearm") StowOffhand();
            // unload the old gun into the stash - swapping used to delete its loaded rounds
            if (EquippedWeapon != null && EquippedWeapon.CurrentMagazine > 0 && !CombatRules.IsMelee(EquippedWeapon))
            {
                Ammo.Add(Inventory, Ammo.Key(EquippedWeapon.Db, EquippedWeapon.CurrentAmmoType), EquippedWeapon.CurrentMagazine);
            }   // two guns only work with a sidearm in the main hand

            if (!string.IsNullOrEmpty(previousCategory))
            {
                var previousDef = ShopDatabase.Items.FirstOrDefault(
                    i => i.Type == "weapon" && i.Category == previousCategory && i.Key == previousKey);
                if (previousDef != null)
                    AddItem(new InventoryItem
                    {
                        Id = previousDef.Id, Name = previousDef.Name, WeightKg = previousDef.WeightKg, Qty = 1,
                        Type = "weapon", Price = previousDef.Price, Category = previousCategory, Key = previousKey,
                        Condition = previousCondition, Upgrades = previousUpgrades != null ? GameCore.Upgrades.Join(previousUpgrades) : null,
                    });
            }

            EquipWeapon(category, key);
            if (EquippedWeapon != null)
            {
                EquippedWeapon.Condition = string.IsNullOrEmpty(condition) ? ItemCondition.Working : condition;
                EquippedWeapon.Upgrades = upgrades;
                EquippedWeapon.ApplyUpgrades();
                if (!CombatRules.IsMelee(EquippedWeapon)) EquippedWeapon.CurrentMagazine = 0;   // a gun out of the pack isn't loaded - and swapping back and forth isn't a free reload
            }
            if (!PairOk(EquippedWeaponCategory, SecondaryCategory))
            {
                GameLog.Write($"Your {SecondaryWeapon.Name} goes back in the pack: a sniper rifle only pairs with a pistol or a blade.");
                StowSecondary();
            }
        }

        // ---- Batch 80 (Yan: "add 2 weapon slots to the system, swapping from any gun to a pistol or melee costs 0 ap but
        // swapping to any other costs 1 ... make it so snipers can only use 'small' weapons as a secondary") ----
        public Weapon SecondaryWeapon;
        public string SecondaryCategory, SecondaryKey;

        public static bool SmallWeapon(string category) => category == "sidearm" || category == "melee";
        /// <summary>A sniper rifle only pairs with a pistol or a blade.</summary>
        public static bool PairOk(string a, string b) => a == null || b == null || (a != "sniper" && b != "sniper") || SmallWeapon(a) || SmallWeapon(b);

        /// <summary>AP to swap now: drawing a pistol or a blade is free, anything else 1 (only counts in a fight).</summary>
        public int SwapAp => SecondaryWeapon == null || SmallWeapon(SecondaryCategory) || !EncounterState.InCombat ? 0 : 1;

        public void EquipSecondaryFromInventory(string itemId)
        {
            var item = Inventory.Items.FirstOrDefault(i => i.Id == itemId && i.Type == "weapon");
            if (item == null) return;
            if (!PairOk(EquippedWeaponCategory, item.Category)) { GameLog.Write("With a sniper rifle, only a pistol or a blade fits as the second weapon."); return; }
            var w = MakeWeapon(item.Category, item.Key);
            if (w == null || !RemoveItem(itemId, 1)) return;
            StowSecondary();
            w.Condition = string.IsNullOrEmpty(item.Condition) ? ItemCondition.Working : item.Condition;
            w.Upgrades = Upgrades.Parse(item.Upgrades);
            w.ApplyUpgrades();
            if (!CombatRules.IsMelee(w)) w.CurrentMagazine = 0;   // out of the pack it isn't loaded, like the main gun
            SecondaryWeapon = w; SecondaryCategory = item.Category; SecondaryKey = item.Key;
            GameLog.Write($"Second weapon: {w.Name}. Swap with Y.");
            ApplyCarryEffects();
        }

        /// <summary>The second weapon back into the pack (its loaded rounds back into the reserve).</summary>
        public void StowSecondary()
        {
            var w = SecondaryWeapon;
            if (w == null) return;
            if (w.CurrentMagazine > 0 && !CombatRules.IsMelee(w))
            {
                Ammo.Add(Inventory, Ammo.Key(w.Db, w.CurrentAmmoType), w.CurrentMagazine);
            }
            var d = ShopDatabase.Items.FirstOrDefault(i => i.Type == "weapon" && i.Category == SecondaryCategory && i.Key == SecondaryKey);
            if (d != null) AddItem(new InventoryItem { Id = d.Id, Name = d.Name, WeightKg = d.WeightKg, Qty = 1, Type = "weapon", Price = d.Price,
                Category = SecondaryCategory, Key = SecondaryKey, Condition = w.Condition, Upgrades = GameCore.Upgrades.Join(w.Upgrades) });
            SecondaryWeapon = null; SecondaryCategory = SecondaryKey = null;
            ApplyCarryEffects();
        }

        /// <summary>Y: swap the two weapons. Both keep their loaded rounds.</summary>
        public bool SwapWeapons()
        {
            if (SecondaryWeapon == null) { GameLog.Write("No second weapon (Second in the pack)."); return false; }
            int ap = SwapAp;
            if (Ap < ap) { GameLog.Write($"Swapping to {SecondaryWeapon.Name} needs {ap} AP."); return false; }
            Ap -= ap;
            (EquippedWeapon, SecondaryWeapon) = (SecondaryWeapon, EquippedWeapon);
            (EquippedWeaponCategory, SecondaryCategory) = (SecondaryCategory, EquippedWeaponCategory);
            (EquippedWeaponKey, SecondaryKey) = (SecondaryKey, EquippedWeaponKey);
            if (SecondaryWeapon == null) SecondaryCategory = SecondaryKey = null;
            GameLog.Write($"You draw your {EquippedWeapon.Name}{(ap > 0 ? $" ({ap} AP)" : "")}.");
            ApplyMpConstraintsAfterEquip();
            ApplyCarryEffects();
            return true;
        }

        /// <summary>New: armor counterpart to EquipWeaponFromInventory above - same reasoning.</summary>
        public void EquipArmorFromInventory(string itemId)
        {
            var item = Inventory.Items.FirstOrDefault(i => i.Id == itemId && i.Type == "armor");
            if (item == null) { GameLog.Write("You don't own that armor."); return; }

            string tier = item.Category, key = item.Key;
            var armorUpgrades = Upgrades.Parse(item.Upgrades);
            var previousArmorUpgrades = EquippedArmorUpgrades;
            // New: carried through to EquipArmor's condition param below - without this, swapping
            // in a looted Dirty/Damaged armor silently reset it to pristine (see EquippedArmorCondition).
            string condition = item.Condition;
            string previousTier = EquippedArmorTier, previousKey = EquippedArmorKey;
            string previousCondition = EquippedArmorCondition;
            if (!RemoveItem(itemId, 1)) return;

            if (!string.IsNullOrEmpty(previousTier))
            {
                var previousDef = ShopDatabase.Items.FirstOrDefault(
                    i => i.Type == "armor" && i.Category == previousTier && i.Key == previousKey);
                if (previousDef != null)
                    AddItem(new InventoryItem
                    {
                        Id = previousDef.Id, Name = previousDef.Name, WeightKg = previousDef.WeightKg, Qty = 1,
                        Type = "armor", Price = previousDef.Price, Category = previousTier, Key = previousKey,
                        Condition = previousCondition, Upgrades = GameCore.Upgrades.Join(previousArmorUpgrades),
                    });
            }

            EquippedArmorUpgrades = armorUpgrades;
            var newDef = ArmorDatabase.Get(tier, key);
            int slots = newDef?.ModSlots ?? 0;
            if (slots < 2) ReturnMod(ref ArmorMods.SlotB);
            if (slots < 1) ReturnMod(ref ArmorMods.SlotA);
            EquipArmor(tier, key, condition);
        }

        /// <summary>File 02: one mod per Mod Slot - a mod the new armor has no slot for goes back in the pack.</summary>
        private void ReturnMod(ref string slot)
        {
            if (string.IsNullOrEmpty(slot)) return;
            if (ArmorModDatabase.All.TryGetValue(slot, out var def))
            {
                AddItem(new InventoryItem { Id = slot, Key = slot, Name = def.Name, Type = "armorMod", Qty = 1, WeightKg = 0.3f });
                GameLog.Write($"{def.Name} doesn't fit the new armor - back in your pack.");
            }
            slot = null;
        }

        /// <summary>New: detector counterpart to EquipWeaponFromInventory/EquipArmorFromInventory
        /// above - only one detector active at a time (ActiveDetector/ActiveDetectorKey, see
        /// Player.cs), so equipping a new one swaps the old one back into the inventory the same
        /// way. Detector shop/loot items carry the DetectorDatabase key in InventoryItem.Key (see
        /// ShopDatabase's detector section), no Category needed since there's only one detector
        /// "tier".</summary>
        public void EquipDetectorFromInventory(string itemId)
        {
            var item = Inventory.Items.FirstOrDefault(i => i.Id == itemId && i.Type == "detector");
            if (item == null) { GameLog.Write("You don't own that detector."); return; }
            if (!DetectorDatabase.All.TryGetValue(item.Key, out var def))
            {
                GameLog.Write("Unknown detector.");
                return;
            }

            string previousKey = ActiveDetectorKey;
            if (!RemoveItem(itemId, 1)) return;

            if (!string.IsNullOrEmpty(previousKey))
            {
                var previousShopDef = ShopDatabase.Items.FirstOrDefault(i => i.Type == "detector" && i.Key == previousKey);
                if (previousShopDef != null)
                    AddItem(new InventoryItem
                    {
                        Id = previousShopDef.Id, Name = previousShopDef.Name, WeightKg = previousShopDef.WeightKg,
                        Qty = 1, Type = "detector", Price = previousShopDef.Price, Key = previousKey,
                    });
            }

            ActiveDetector = def;
            ActiveDetectorKey = item.Key;
            GameLog.Write($"Equipped detector: {def.Name}.");
        }

        /// <summary>Ported from applyMpConstraintsAfterEquip().</summary>
        /// <summary>The backpack you take off goes into the pack instead of vanishing.</summary>
        public void StowBackpack(string replacement)
        {
            string old = Inventory.Backpack;
            if (string.IsNullOrEmpty(old) || old == "none" || old == replacement) return;
            if (!BackpackDatabase.All.TryGetValue(old, out var bp)) return;
            var shop = ShopDatabase.Find(old);
            AddItem(new InventoryItem { Id = old, Name = bp.Name, Type = "backpack", Qty = 1, WeightKg = shop?.WeightKg ?? 1f, Price = shop?.Price });
        }

        public void ApplyMpConstraintsAfterEquip()
        {
            int armorMp = EquippedArmor != null && EquippedArmor.Mp > 0 ? EquippedArmor.Mp : 4;
            int maxMp = armorMp;
            if (EquippedWeapon != null && EquippedWeapon.Db != null && EquippedWeapon.Db.MaxMpWhileHeld.HasValue && !CombatantUtil.HasPerk(this, "WalkingTank"))
                maxMp = System.Math.Min(maxMp, EquippedWeapon.Db.MaxMpWhileHeld.Value);

            MaxMp = maxMp;
            if (Mp > MaxMp + RushBonus) Mp = MaxMp + RushBonus;   // Batch 76: ApplyCarryEffects settles the Bull Rush MP
        }

        /// <summary>Ported from useConsumable(itemId).</summary>
        public void UseConsumable(string itemId)
        {
            if (!ConsumableDatabase.All.TryGetValue(itemId, out var def))
            {
                GameLog.Write("Consumable not recognized.");
                return;
            }

            int inventoryIndex = Inventory.Items.FindIndex(it => it.Id == itemId && it.Type == "consumable");
            if (inventoryIndex < 0)
            {
                GameLog.Write($"No {def.Name} available to use.");
                return;
            }

            var item = Inventory.Items[inventoryIndex];
            if (def.RepairKit)
            {
                if (UseRepairKit()) RemoveItem(itemId, 1);
                return;
            }
            if (def.Throwable != null)
            {
                ArmedThrowable = itemId;
                GameLog.Write($"{def.Name} ready - click a tile within 8 to throw it (right-click to put it away).");
                return;
            }
            int apCost = def.ApCost > 0 ? def.ApCost : 2;
            bool inPlayerTacticalTurn = GameState.Current.Mode == GameMode.Tactical && GameState.Current.CurrentTurn == Turn.Player;

            if (inPlayerTacticalTurn && Ap < apCost)
            {
                GameLog.Write($"Not enough AP to use {def.Name}.");
                return;
            }
            if (inPlayerTacticalTurn)
            {
                Ap = System.Math.Max(0, Ap - apCost);
                GameLog.Write($"{def.Name} used for {apCost} AP.");
            }

            int medic = CombatantUtil.HasPerk(this, "CombatMedic") ? 10 : 0;
            if (def.HealingPermanent.HasValue && def.HealingPermanent.Value > 0)
            {
                HealPermanent(def.HealingPermanent.Value + medic);
                GameLog.Write($"Restored {def.HealingPermanent.Value + medic} permanent HP.");
            }
            if (def.HealingTemp.HasValue && def.HealingTemp.Value > 0)
            {
                ApplyTemporaryHP(def.HealingTemp.Value + medic);
                GameLog.Write($"Gained {def.HealingTemp.Value + medic} temporary HP for {TempHPDuration} turns.");
            }
            if (def.MpRestore.HasValue && def.MpRestore.Value > 0)
            {
                Mp = System.Math.Min(MaxMp, Mp + def.MpRestore.Value);
                GameLog.Write($"Restored {def.MpRestore.Value} MP.");
            }
            if (def.CureBleeding)
            {
                // File 00: a Bandage treats one Wound (a Torso wound is the bleeding one).
                if (StatusEffects.Wound != null)
                {
                    GameLog.Write($"You bandage your {StatusEffects.Wound.ToLowerInvariant()} wound.");
                    if (StatusEffects.Wound == "Torso") StatusEffects.Bleeding = false;
                    StatusEffects.Wound = null;
                    ApplyCarryEffects();
                }
                else if (StatusEffects.Bleeding) { StatusEffects.Bleeding = false; GameLog.Write("Bleeding has been stopped."); }
                else GameLog.Write("No bleeding to treat.");
            }
            if (def.CureRadiation)
            {
                if (StatusEffects.Irradiated) { StatusEffects.Irradiated = false; GameLog.Write("Irradiation has been removed."); }
                else GameLog.Write("No radiation to treat.");
            }
            // New: Hunger/Thirst previously only ever recovered automatically via a travel ration
            // burned on crossing zones (ZoneTravel.TravelToSector) - there was no way to relieve
            // either stat while exploring inside a single zone, so a long visit could run you to 0
            // with nothing to do about it beyond leaving. Food/drink items can now restore both
            // directly through the same Use action as medkits/stims.
            int stomach = CombatantUtil.HasPerk(this, "IronStomach") ? 2 : 1;
            if (def.HungerRestore.HasValue && def.HungerRestore.Value > 0)
            {
                Hunger = System.Math.Min(10, Hunger + def.HungerRestore.Value * stomach);
                GameLog.Write($"Restored {def.HungerRestore.Value} hunger.");
            }
            if (def.ThirstRestore.HasValue && def.ThirstRestore.Value > 0)
            {
                Thirst = System.Math.Min(10, Thirst + def.ThirstRestore.Value * stomach);
                GameLog.Write($"Restored {def.ThirstRestore.Value} thirst.");
            }

            int healed = (def.HealingPermanent ?? 0) + (def.HealingTemp ?? 0);
            if (healed > 0 && def.HungerRestore == null && def.ThirstRestore == null)
            {
                float cost = healed / 10f * GameConstants.SurvivalCostPer10Hp;
                Hunger = System.Math.Max(0, Hunger - cost);
                Thirst = System.Math.Max(0, Thirst - cost);
                Tiredness = System.Math.Max(0, Tiredness - cost);
                GameLog.Write($"The {def.Name} leaves you hungrier, thirstier and drowsier (-{cost:0.##} each).");
            }

            item.Qty -= 1;
            if (item.Qty <= 0) Inventory.Items.RemoveAt(inventoryIndex);
        }

        /// <summary>Ported from applyArmorModsForPlayer(). See the BUG FIX note at the top of this file.</summary>
        public void ApplyArmorModsForPlayer()
        {
            if (EquippedArmor == null)
            {
                BasePhysDR = ArtifactPhysDRBonus;
                AnomDR = System.Math.Max(0f, ArtifactAnomDRBonus + ArtifactAnomPenalty);
                return;
            }

            PhysDR = EquippedArmor.PhysDR;
            AnomDR = EquippedArmor.AnomDR;

            // New: ItemCondition.cs centralizes condition effects (see its header note) but the
            // armor DR side of that was never actually wired up - a Damaged breastplate protected
            // exactly as well as a pristine one. Applied before mods so a mod's own PhysDR/AnomDR
            // percentage bonus below is a percentage of the already-degraded base, not the other
            // way around.
            // File 00 Condition table: Damaged -10% Phys-DR and Anom-DR, Broken -25%.
            BasePhysDR *= ItemCondition.ArmorDrMultiplier(EquippedArmorCondition);
            AnomDR *= ItemCondition.ArmorDrMultiplier(EquippedArmorCondition);

            int mpModifier = 0;
            int beltBonus = 0;
            float modCarryBonus = 0;

            foreach (var modId in new[] { ArmorMods.SlotA, ArmorMods.SlotB })
            {
                if (string.IsNullOrEmpty(modId)) continue;
                if (!ArmorModDatabase.All.TryGetValue(modId, out var mod)) continue;

                if (mod.PhysDRPercentAdd.HasValue) BasePhysDR += EquippedArmor.PhysDR * mod.PhysDRPercentAdd.Value;
                if (mod.AnomDRPercentAdd.HasValue) AnomDR += EquippedArmor.AnomDR * mod.AnomDRPercentAdd.Value;
                if (mod.MpModifier.HasValue) mpModifier += mod.MpModifier.Value;
                if (mod.BeltSlotBonus.HasValue) beltBonus += mod.BeltSlotBonus.Value;
                if (mod.CarryCapacityBonus.HasValue) modCarryBonus += mod.CarryCapacityBonus.Value;
            }

            if (EquippedArmorUpgrades.Contains("Plating")) BasePhysDR += 0.05f;
            if (EquippedArmorUpgrades.Contains("Lining")) AnomDR += 0.10f;
            // Artifacts (file 03): DR bonuses (Lucifer) and the Anom-DR penalty every worn artifact
            // costs - both were computed but never actually applied before.
            BasePhysDR += ArtifactPhysDRBonus;
            AnomDR = System.Math.Max(0f, AnomDR + ArtifactAnomDRBonus + ArtifactAnomPenalty);

            ArmorModMpBonus = mpModifier;
            MaxMp = System.Math.Max(2, (EquippedArmor.Mp > 0 ? EquippedArmor.Mp : 4) + mpModifier);
            ArmorModBeltSlotBonus = beltBonus; // player-local, not mutating the shared ArmorDef

            var backpackDef = BackpackDatabase.All.TryGetValue(Inventory.Backpack ?? "none", out var bp) ? bp : null;
            CarryCapacityBonus = (backpackDef?.CapacityBonus ?? 0) + modCarryBonus;

            ApplyCarryEffects();
        }

        /// <summary>Ported from handleArmorModChange(slot, modId). slot is "slotA" or "slotB".</summary>
        public void HandleArmorModChange(string slot, string modId)
        {
            string resolved = modId == "none" ? null : modId;
            if (slot == "slotA") ArmorMods.SlotA = resolved;
            else if (slot == "slotB") ArmorMods.SlotB = resolved;
            ApplyArmorModsForPlayer();
        }

        /// <summary>Ported from handleBackpackChange(backpackId).</summary>
        public void HandleBackpackChange(string backpackId)
        {
            StowBackpack(backpackId);
            Inventory.Backpack = backpackId;
            var backpackDef = BackpackDatabase.All.TryGetValue(backpackId ?? "none", out var bp) ? bp : null;
            CarryCapacityBonus = backpackDef?.CapacityBonus ?? 0;
            ApplyArmorModsForPlayer();
        }
    }
}
