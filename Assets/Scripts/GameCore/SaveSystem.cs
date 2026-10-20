// New (Batch 9): single-slot save/load. Nothing in the project persisted to disk before this -
// closing the game lost everything.
//
// ponytail: single slot (Application.persistentDataPath/savegame.json), overwritten each save -
// add slot selection/UI if multiple saves are wanted later. Deliberately does NOT snapshot the
// live tactical state of the zone you're standing in: MacroState.PersistedZoneTerrain/
// PersistedZoneInteractiveObjects/PersistedZoneLandmarks/PersistedZoneAnomalies/OfflineSquads/
// RolledShopStock/WanderingTrader* are all runtime caches JsonUtility can't serialize anyway
// (Dictionary<>, TerrainTile[,]) and exist only to keep a zone's contents stable while you're
// bouncing between zones in one sitting - a save is meant to be "come back later," and on load
// every zone (including the one you saved in) regenerates exactly the way an unvisited zone
// already does today. What DOES carry over: your character (equipment, artifacts, inventory,
// reputation, quests, perks, survival stats) and the progression flags that matter (discovered
// hidden nodes, artifact-rolled zones, and - importantly - PopulatedZones, so a zone you already
// cleared doesn't respawn its original hostile squad from scratch; ambient spawns still occur
// there over time the same way they do in any zone).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GameCore
{
    public static class SaveSystem
    {
        private static string FilePath => Path.Combine(Application.persistentDataPath, "savegame.json");

        public static bool HasSave() => File.Exists(FilePath);

        public static bool Save()
        {
            var player = Player.Current;
            if (player == null)
            {
                GameLog.Write("Nothing to save yet.");
                return false;
            }
            if (!player.IsAlive())
            {
                GameLog.Write("Cannot save while dead.");
                return false;
            }

            var macro = MacroState.Current;
            var data = new SaveData
            {
                SavedAtUtc = DateTime.UtcNow.ToString("o"),
                Player = player.CaptureSaveData(),
                Macro = new MacroSaveData
                {
                    CurrentZoneId = macro.CurrentZoneId,
                    DiscoveredHiddenNodes = new List<string>(macro.DiscoveredHiddenNodes),
                    Keys = new List<string>(macro.Keys),
                    GarrisonRaidReadyDay = macro.GarrisonRaidReadyDay, GarrisonArmoryOpen = macro.GarrisonArmoryOpen,
                    ArtifactRolledZones = new List<string>(macro.ArtifactRolledZones),
                    PopulatedZones = new List<string>(macro.PopulatedZones),
                    WorldTickCounter = macro.WorldTickCounter,
                    Day = macro.Day, CrossingsSinceRest = macro.CrossingsSinceRest, MigrationDaysLeft = macro.MigrationDaysLeft,
                    EventOfDay = macro.EventOfDay, CaravanZoneId = macro.CaravanZoneId, MigrationZoneId = macro.MigrationZoneId,
                    GoalBoxes = macro.GoalBoxes.Select(kv => new StringIntPair { Key = kv.Key, Value = kv.Value }).ToList(),
                    CompletedGoals = new List<string>(macro.CompletedGoals), DeckCooling = new List<string>(macro.DeckCooling),
                    NamedNpcs = new List<string>(macro.NamedNpcs),
                    Rumors = new List<string>(macro.Rumors),
                    WorldNews = new List<string>(macro.WorldNews),
                    StoryFlags = new List<string>(macro.StoryFlags), Sponsor = macro.Sponsor, Ending = macro.Ending,
                    SurgeRounds = macro.SurgeRounds, SurgeZoneId = macro.SurgeZoneId, Hour = macro.Hour, Weather = macro.Weather, LastRumorDay = macro.LastRumorDay,
                    CampZone = macro.CampZone, CampX = macro.CampX, CampY = macro.CampY,
                },
            };

            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
                GameLog.Write("Game saved.");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveSystem.Save failed: {e}");
                GameLog.Write("Save failed - see the console for details.");
                return false;
            }
        }

        public static bool Load()
        {
            if (!HasSave())
            {
                GameLog.Write("No save file found.");
                return false;
            }

            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(NameMigration.Apply(File.ReadAllText(FilePath)));
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveSystem.Load failed: {e}");
                GameLog.Write("Load failed - save file could not be read, see the console for details.");
                return false;
            }
            if (data?.Player == null || data.Macro == null)
            {
                GameLog.Write("Load failed - save file is corrupt or empty.");
                return false;
            }

            // Same reset GameBootstrap.StartNewGame uses for a fresh game/death-restart - fresh
            // GameState/MacroState/Player, then populate them instead of the hardcoded starting loadout.
            GameState.Current = new GameState();
            MacroState.Current = new MacroState();
            var macro = MacroState.Current;
            macro.CurrentZoneId = string.IsNullOrEmpty(data.Macro.CurrentZoneId) ? macro.CurrentZoneId : data.Macro.CurrentZoneId;
            macro.DiscoveredHiddenNodes = new List<string>(data.Macro.DiscoveredHiddenNodes ?? new List<string>());
            macro.Keys = new HashSet<string>(data.Macro.Keys ?? new List<string>());
            macro.GarrisonRaidReadyDay = data.Macro.GarrisonRaidReadyDay; macro.GarrisonArmoryOpen = data.Macro.GarrisonArmoryOpen;
            macro.ArtifactRolledZones = new List<string>(data.Macro.ArtifactRolledZones ?? new List<string>());
            macro.PopulatedZones = new HashSet<string>(data.Macro.PopulatedZones ?? new List<string>());
            macro.WorldTickCounter = data.Macro.WorldTickCounter;
            var dm = data.Macro;
            macro.Day = System.Math.Max(1, dm.Day);
            macro.CrossingsSinceRest = dm.CrossingsSinceRest;
            macro.MigrationDaysLeft = dm.MigrationDaysLeft;
            macro.EventOfDay = dm.EventOfDay;
            macro.CaravanZoneId = dm.CaravanZoneId;
            macro.MigrationZoneId = dm.MigrationZoneId;
            foreach (var kv in dm.GoalBoxes ?? new List<StringIntPair>()) macro.GoalBoxes[kv.Key] = kv.Value;
            macro.CompletedGoals = dm.CompletedGoals ?? new List<string>();
            macro.DeckCooling = dm.DeckCooling ?? new List<string>(); FactionDeck.Reset();
            macro.NamedNpcs = dm.NamedNpcs ?? new List<string>();
            macro.Rumors = dm.Rumors ?? new List<string>();
            macro.WorldNews = dm.WorldNews ?? new List<string>();
            macro.StoryFlags = dm.StoryFlags ?? new List<string>();
            macro.Sponsor = dm.Sponsor;
            macro.SurgeRounds = dm.SurgeRounds; macro.SurgeZoneId = dm.SurgeZoneId;
            macro.Hour = dm.Hour; macro.Weather = dm.Weather ?? "Overcast"; macro.LastRumorDay = dm.LastRumorDay;
            macro.CampZone = string.IsNullOrEmpty(dm.CampZone) ? null : dm.CampZone; macro.CampX = dm.CampX; macro.CampY = dm.CampY;
            macro.Ending = string.IsNullOrEmpty(dm.Ending) ? null : dm.Ending;
            StorySystem.ResetWorld();
            StorySystem.ApplyEnding(null, apply: false);

            new Player();
            var player = Player.Current;
            player.ApplyLoadedData(data.Player);

            ZoneTravel.EnterZone(macro.CurrentZoneId);
            // EnterZone's own spawn placement (FindEntrySpawn) has no "came from" zone to work off
            // here and falls back to map-center - snap to the saved position instead, same
            // nearest-walkable-tile fallback ZoneTravel already uses for every other spawn.
            var spawn = TerrainMap.FindNearestEnterable(data.Player.X, data.Player.Y);
            player.X = spawn.x;
            player.Y = spawn.y;

            GameLog.Write("Game loaded.");
            return true;
        }
    }
}
