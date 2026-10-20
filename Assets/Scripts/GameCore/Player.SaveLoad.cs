// New (Batch 9): save/load state capture, split into its own partial-class file matching the
// existing Player.Equipment.cs/Player.Artifacts.cs/Player.Quests.cs convention. See SaveSystem.cs
// for the file I/O and MacroState side, and SaveData.cs for the DTO shapes.
using System.Collections.Generic;
using GameData;

namespace GameCore
{
    public partial class Player
    {
        /// <summary>Snapshots everything that represents actual player progression. Derived stats
        /// (MaxHp, MaxMp, PhysDR, AnomDR, all the Artifact* bonus fields) are deliberately NOT
        /// captured - ApplyLoadedData re-derives them from the equipment/artifacts that ARE
        /// captured, the same way equipping something already does.</summary>
        public PlayerSaveData CaptureSaveData()
        {
            var d = new PlayerSaveData
            {
                X = X, Y = Y, WorldX = WorldX, WorldY = WorldY,
                PermHP = PermHP, TempHP = TempHP, TempHPDuration = TempHPDuration,
                Bleeding = StatusEffects.Bleeding, ToxicBleed = StatusEffects.ToxicBleed,
                Irradiated = StatusEffects.Irradiated, Prone = StatusEffects.Prone,
                Ap = Ap, Mp = Mp,
                StatS = Stats.S, StatA = Stats.A, StatI = Stats.I, StatP = Stats.P,
                EquippedWeaponCategory = EquippedWeaponCategory, EquippedWeaponKey = EquippedWeaponKey,
                EquippedWeaponCondition = EquippedWeapon?.Condition,
                EquippedWeaponUpgrades = EquippedWeapon != null ? Upgrades.Join(EquippedWeapon.Upgrades) : null,
                EquippedArmorUpgrades = Upgrades.Join(EquippedArmorUpgrades),
                EquippedWeaponMagazine = EquippedWeapon?.CurrentMagazine ?? -1, EquippedWeaponAmmoType = EquippedWeapon?.CurrentAmmoType,
                EquippedWeaponJammed = EquippedWeapon?.IsJammed ?? false,
                OffhandKey = OffhandKey, OffhandCondition = OffhandWeapon?.Condition, OffhandUpgrades = OffhandWeapon != null ? Upgrades.Join(OffhandWeapon.Upgrades) : null,
                SecondaryCategory = SecondaryCategory, SecondaryKey = SecondaryKey, SecondaryCondition = SecondaryWeapon?.Condition,
                SecondaryUpgrades = SecondaryWeapon != null ? Upgrades.Join(SecondaryWeapon.Upgrades) : null,
                SecondaryMagazine = SecondaryWeapon?.CurrentMagazine ?? 0, SecondaryAmmoType = SecondaryWeapon?.CurrentAmmoType,
                EquippedArmorTier = EquippedArmorTier, EquippedArmorKey = EquippedArmorKey,
                EquippedArmorCondition = EquippedArmorCondition,
                ArmorModSlotA = ArmorMods.SlotA, ArmorModSlotB = ArmorMods.SlotB,
                ArtifactSlots = (string[])ArtifactSlots.Clone(),
                ActiveDetectorKey = ActiveDetectorKey,
                Backpack = Inventory.Backpack,
                Companion = GameCore.Companion.Capture(),
                Rank = Rank, Exp = Exp, MilestonesDone = new System.Collections.Generic.List<string>(MilestonesDone), Sneaking = Sneaking, FlashlightOff = !FlashlightOn,
                MaxTierReached = MaxTierReached, QuestsCompleted = QuestsCompleted, MaxMutantTierKilled = MaxMutantTierKilled,
                MaxNpcRankKilled = MaxNpcRankKilled, ArcsFinished = ArcsFinished, PendingPerkPicks = PendingPerkPicks,
                HiddenNodeCleared = HiddenNodeCleared, MainStoryAct3Done = MainStoryAct3Done, MainStoryFinaleDone = MainStoryFinaleDone,
                BonusMaxHp = BonusMaxHp,
                ActivePerks = new List<string>(ActivePerks),
                Hunger = Hunger, Thirst = Thirst, Tiredness = Tiredness,
                Ru = Ru,
                ActiveQuests = new List<ActiveQuest>(ActiveQuests),
                CompletedQuestIds = new List<string>(CompletedQuestIds),
            };

            foreach (var item in Inventory.Items)
                d.InventoryItems.Add(new InventoryItemSave
                {
                    Id = item.Id, Name = item.Name, WeightKg = item.WeightKg, Qty = item.Qty, Type = item.Type,
                    Price = item.Price ?? 0, Category = item.Category, Key = item.Key, Condition = item.Condition, Upgrades = item.Upgrades,
                });

            foreach (var kvp in Inventory.AmmoReserve)
                d.AmmoReserve.Add(new StringIntPair { Key = kvp.Key, Value = kvp.Value });

            foreach (var kvp in Reputation)
                d.Reputation.Add(new StringIntPair { Key = kvp.Key, Value = kvp.Value });

            foreach (var kvp in NpcArcStage)
                d.NpcArcStage.Add(new StringIntPair { Key = kvp.Key, Value = kvp.Value });

            return d;
        }

        /// <summary>Restores a captured snapshot onto a freshly-constructed Player (call this right
        /// after `new Player()`, mirroring GameBootstrap.StartNewGame's equip-then-enter-zone
        /// order - SaveSystem.Load does exactly that instead of the hardcoded starting loadout).</summary>
        public void ApplyLoadedData(PlayerSaveData d)
        {
            X = d.X; Y = d.Y; WorldX = d.WorldX; WorldY = d.WorldY;
            Stats.S = d.StatS; Stats.A = d.StatA; Stats.I = d.StatI; Stats.P = d.StatP;

            if (!string.IsNullOrEmpty(d.EquippedWeaponCategory))
            {
                EquipWeapon(d.EquippedWeaponCategory, d.EquippedWeaponKey);
                if (EquippedWeapon != null)
                {
                    EquippedWeapon.Condition = string.IsNullOrEmpty(d.EquippedWeaponCondition) ? ItemCondition.Working : d.EquippedWeaponCondition;
                    EquippedWeapon.Upgrades = Upgrades.Parse(d.EquippedWeaponUpgrades);
                    EquippedWeapon.ApplyUpgrades();
                    if (d.EquippedWeaponMagazine >= 0) EquippedWeapon.CurrentMagazine = System.Math.Min(d.EquippedWeaponMagazine, EquippedWeapon.MagCapacity);
                    if (!string.IsNullOrEmpty(d.EquippedWeaponAmmoType)) EquippedWeapon.CurrentAmmoType = d.EquippedWeaponAmmoType;
                    EquippedWeapon.IsJammed = d.EquippedWeaponJammed;
                }
            }
            if (!string.IsNullOrEmpty(d.EquippedArmorTier))
            {
                EquippedArmorUpgrades = Upgrades.Parse(d.EquippedArmorUpgrades);
                EquipArmor(d.EquippedArmorTier, d.EquippedArmorKey);
                EquippedArmorCondition = string.IsNullOrEmpty(d.EquippedArmorCondition) ? ItemCondition.Working : d.EquippedArmorCondition;
            }
            if (!string.IsNullOrEmpty(d.OffhandKey))
            {
                OffhandWeapon = MakeWeapon("sidearm", d.OffhandKey);
                OffhandKey = d.OffhandKey;
                if (OffhandWeapon != null)
                {
                    OffhandWeapon.Condition = string.IsNullOrEmpty(d.OffhandCondition) ? ItemCondition.Working : d.OffhandCondition;
                    OffhandWeapon.Upgrades = Upgrades.Parse(d.OffhandUpgrades);
                    OffhandWeapon.ApplyUpgrades();
                }
            }
            SecondaryWeapon = null; SecondaryCategory = SecondaryKey = null;
            if (!string.IsNullOrEmpty(d.SecondaryKey) && (SecondaryWeapon = MakeWeapon(d.SecondaryCategory, d.SecondaryKey)) != null)
            {
                SecondaryCategory = d.SecondaryCategory; SecondaryKey = d.SecondaryKey;
                SecondaryWeapon.Condition = string.IsNullOrEmpty(d.SecondaryCondition) ? ItemCondition.Working : d.SecondaryCondition;
                SecondaryWeapon.Upgrades = Upgrades.Parse(d.SecondaryUpgrades);
                SecondaryWeapon.ApplyUpgrades();
                SecondaryWeapon.CurrentMagazine = System.Math.Min(d.SecondaryMagazine, SecondaryWeapon.MagCapacity);
                if (!string.IsNullOrEmpty(d.SecondaryAmmoType)) SecondaryWeapon.CurrentAmmoType = d.SecondaryAmmoType;
            }
            ArmorMods.SlotA = d.ArmorModSlotA;
            ArmorMods.SlotB = d.ArmorModSlotB;

            if (d.ArtifactSlots != null)
                for (int i = 0; i < ArtifactSlots.Length && i < d.ArtifactSlots.Length; i++)
                    ArtifactSlots[i] = d.ArtifactSlots[i];

            if (!string.IsNullOrEmpty(d.ActiveDetectorKey) && DetectorDatabase.All.TryGetValue(d.ActiveDetectorKey, out var det))
            {
                ActiveDetector = det;
                ActiveDetectorKey = d.ActiveDetectorKey;
            }

            Inventory.Items.Clear();
            foreach (var item in d.InventoryItems)
                Inventory.Items.Add(new InventoryItem
                {
                    Id = item.Id, Name = item.Name, WeightKg = item.WeightKg, Qty = item.Qty, Type = item.Type,
                    Price = item.Price, Category = item.Category, Key = item.Key, Condition = item.Condition, Upgrades = item.Upgrades,
                });

            Inventory.AmmoReserve.Clear();
            foreach (var kv in d.AmmoReserve) Inventory.AmmoReserve[kv.Key] = kv.Value;
            Ammo.Migrate(this);   // Batch 80
            Inventory.Backpack = d.Backpack;
            GameCore.Companion.Restore(d.Companion, this);   // EnterZone places them beside you

            Rank = string.IsNullOrEmpty(d.Rank) ? "Rookie" : d.Rank;
            Sneaking = d.Sneaking; FlashlightOn = !d.FlashlightOff;
            MaxTierReached = System.Math.Max(1, d.MaxTierReached); QuestsCompleted = d.QuestsCompleted;
            MaxMutantTierKilled = d.MaxMutantTierKilled; MaxNpcRankKilled = d.MaxNpcRankKilled;
            ArcsFinished = d.ArcsFinished; PendingPerkPicks = d.PendingPerkPicks; HiddenNodeCleared = d.HiddenNodeCleared;
            MainStoryAct3Done = d.MainStoryAct3Done; MainStoryFinaleDone = d.MainStoryFinaleDone; BonusMaxHp = d.BonusMaxHp;
            Exp = d.Exp;
            MilestonesDone = d.MilestonesDone ?? new System.Collections.Generic.List<string>();
            ActivePerks.Clear();
            if (d.ActivePerks != null) ActivePerks.AddRange(d.ActivePerks);

            Hunger = d.Hunger; Thirst = d.Thirst; Tiredness = d.Tiredness;
            Ru = d.Ru;

            Reputation.Clear();
            if (d.Reputation != null) foreach (var kv in d.Reputation) Reputation[kv.Key] = kv.Value;

            ActiveQuests.Clear();
            if (d.ActiveQuests != null) ActiveQuests.AddRange(d.ActiveQuests);
            CompletedQuestIds.Clear();
            if (d.CompletedQuestIds != null) foreach (var id in d.CompletedQuestIds) CompletedQuestIds.Add(id);
            NpcArcStage.Clear();
            if (d.NpcArcStage != null) foreach (var kv in d.NpcArcStage) NpcArcStage[kv.Key] = kv.Value;
            // the map overhaul: Yefim moved from the Perimeter into Bonfire Village (a save from before keeps his arc)
            if (NpcArcStage.TryGetValue("perimeter", out var yefim) && !NpcArcStage.ContainsKey("bonfire_village")) { NpcArcStage["bonfire_village"] = yefim; NpcArcStage.Remove("perimeter"); }

            // Re-derive MaxHp/PhysDR/AnomDR/Artifact* bonuses from what's now equipped/slotted.
            ApplyArtifactStatBonuses();
            ApplyCarryEffects();

            // Resource pools aren't re-derived above - restore them last, clamped to whatever
            // Max* the recompute just settled on.
            PermHP = System.Math.Min(MaxHp, d.PermHP);
            TempHP = System.Math.Max(0, System.Math.Min(MaxHp - PermHP, d.TempHP));
            TempHPDuration = d.TempHPDuration;
            Ap = d.Ap;
            Mp = System.Math.Min(MaxMp, d.Mp);
            StatusEffects.Bleeding = d.Bleeding;
            StatusEffects.ToxicBleed = d.ToxicBleed;
            StatusEffects.Irradiated = d.Irradiated;
            StatusEffects.Prone = d.Prone;
        }
    }
}
