// New (Batch 9): plain, JsonUtility-friendly DTOs for save/load. JsonUtility only serializes
// public fields (not auto-properties) and doesn't support Dictionary<> or Nullable<T>, which is
// why this is a separate mirror of Player/MacroState rather than serializing those directly -
// both are full of {get;set;} auto-properties and Dictionary<string,int> fields. Only what
// represents actual player progression is captured here; see SaveSystem.cs's header note for what
// is deliberately left out (and regenerated fresh on load) and why.
using System;
using System.Collections.Generic;

namespace GameCore
{
    /// <summary>Reusable string->int pair - stands in for a Dictionary<string,int> entry
    /// (Reputation, AmmoReserve, NpcArcStage all need this shape; JsonUtility can't serialize
    /// Dictionary directly).</summary>
    [Serializable]
    public class StringIntPair
    {
        public string Key;
        public int Value;
    }

    [Serializable]
    public class InventoryItemSave
    {
        public string Id;
        public string Name;
        public float WeightKg;
        public int Qty;
        public string Type;
        public int Price; // InventoryItem.Price is int? - 0 here means "no price", same as (item.Price ?? 0) is already treated everywhere it's read
        public string Category;
        public string Key;
        public string Upgrades;
        public string Condition;
        public int GridX = -1, GridY; public bool Rot;   // Pack 1 (an old save has none: -1, settled on load)
    }

    [Serializable]
    public class PlayerSaveData
    {
        public int X, Y, WorldX, WorldY;
        public int PermHP, TempHP, TempHPDuration;
        public bool Bleeding, ToxicBleed, Irradiated, Prone;
        public int Ap; public float Mp;   // Batch 102: float MP

        public int StatS, StatA, StatI, StatP;

        public string EquippedWeaponCategory, EquippedWeaponKey, EquippedWeaponCondition, EquippedWeaponUpgrades, EquippedArmorUpgrades;
        public string OffhandKey, OffhandCondition, OffhandUpgrades;
        public string SecondaryCategory, SecondaryKey, SecondaryCondition, SecondaryUpgrades, SecondaryAmmoType;   // Batch 80
        public bool Sneaking, FlashlightOff;   // Batch 96: toggles survive a load (off, not on, so old saves load with the light on)
        public int SecondaryMagazine;
        public int EquippedWeaponMagazine = -1;   // -1: an old save, keep the full magazine
        public string EquippedWeaponAmmoType;
        public bool EquippedWeaponJammed;
        public string EquippedArmorTier, EquippedArmorKey, EquippedArmorCondition;
        public string ArmorModSlotA, ArmorModSlotB;

        public string[] ArtifactSlots;
        public string ActiveDetectorKey;

        public List<InventoryItemSave> InventoryItems = new List<InventoryItemSave>();
        public List<StringIntPair> AmmoReserve = new List<StringIntPair>();
        public string Backpack;
        public string Companion;   // Batch 69: name|rank|weapon cat/key|armor tier/key|hp, empty if none

        public string Rank;
        // file 00 §13 milestones
        public int MaxTierReached = 1, QuestsCompleted, MaxMutantTierKilled, MaxNpcRankKilled = -1, ArcsFinished, PendingPerkPicks;
        public bool HiddenNodeCleared, MainStoryAct3Done, MainStoryFinaleDone;
        public int BonusMaxHp;
        public int Exp;
        public List<string> MilestonesDone = new List<string>();
        public List<string> DungeonsCleared = new List<string>();
        public int NamedKilled;
        public List<string> ActivePerks = new List<string>();

        public float Hunger, Thirst, Tiredness;
        public int Ru;
        public List<StringIntPair> Reputation = new List<StringIntPair>();

        public List<ActiveQuest> ActiveQuests = new List<ActiveQuest>(); // reused directly - already plain fields, JsonUtility-safe
        public List<string> CompletedQuestIds = new List<string>();
        public List<StringIntPair> NpcArcStage = new List<StringIntPair>();
    }

    [Serializable]
    public class MacroSaveData
    {
        public string CurrentZoneId;
        public List<string> DiscoveredHiddenNodes = new List<string>();
        public List<string> Keys = new List<string>();
        public int GarrisonRaidReadyDay; public bool GarrisonArmoryOpen;   // Batch 87
        public List<string> ArtifactRolledZones = new List<string>();
        public List<string> PopulatedZones = new List<string>();
        public int WorldTickCounter;
        // file 10 living world
        public int Day = 1, CrossingsSinceRest, MigrationDaysLeft;
        public string EventOfDay, CaravanZoneId, MigrationZoneId;
        public List<StringIntPair> GoalBoxes = new List<StringIntPair>();
        public List<string> CompletedGoals = new List<string>();
        public List<string> DeckCooling = new List<string>();
        public List<string> NamedNpcs = new List<string>();
        public List<string> Rumors = new List<string>();
        public List<string> WorldNews = new List<string>();      // Batch 38
        // file 09 main story
        public List<string> StoryFlags = new List<string>();
        public string Sponsor, Ending;
        public int SurgeRounds, LastRumorDay;
        public string CampZone; public int CampX = -1, CampY = -1;
        public string SurgeZoneId;
        public float Hour = 7f;
        public string Weather = "Overcast";
    }

    /// <summary>RT 7x: one character (saves/characters/id.json) - the player owns it and can take it into any world.</summary>
    [Serializable]
    public class CharacterSave
    {
        public int Version = 2;
        public string Id, Name, WorldId, SavedAtUtc;
        public PlayerSaveData Player;
    }

    /// <summary>RT 7x: one world (saves/worlds/id.json) - the host saves it.</summary>
    [Serializable]
    public class WorldSave
    {
        public int Version = 2;
        public string Id, HostCharacterId, SavedAtUtc;
        public MacroSaveData Macro;
    }

    /// <summary>The old single-file save (savegame.json, before RT 7x); read once to move it into a slot.</summary>
    [Serializable]
    public class SaveData
    {
        public int Version = 1;
        public string SavedAtUtc;
        public PlayerSaveData Player;
        public MacroSaveData Macro;
    }
}
