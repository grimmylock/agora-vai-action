// New (Batch 9): save/load. RT 7x (Yan's co-op rules: "the host saves the world, the players own their characters and can
// join other worlds or play their own ... multiple save-slots"): a character file per character (saves/characters/<id>.json:
// name, the PlayerSaveData, the world it was last in) and a world file per world (saves/worlds/<id>.json: the MacroSaveData,
// who hosts it). A slot = a character; loading it opens the world it was last in. New Game names the character and saves both
// at once (the save at creation). The old single savegame.json becomes a slot the first time the slots are listed.
// Co-op: Save() writes the world and the local character; a joined player's character lives on their own machine (the network
// layer asks them to save when the host does - not wired yet); the harness's local joiners aren't saved.
//
// Deliberately does NOT snapshot the
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
        /// <summary>Where the saves live; the harness points it at a temp folder. Null = no saving (the harness by default).</summary>
        public static string Root;
        static string Dir => Root ?? (string.IsNullOrEmpty(Application.persistentDataPath) ? null : Path.Combine(Application.persistentDataPath, "saves"));
        static string CharFile(string id) => Path.Combine(Dir, "characters", id + ".json");
        static string WorldFile(string id) => Path.Combine(Dir, "worlds", id + ".json");
        static string Legacy => Path.Combine(Path.GetDirectoryName(Dir), "savegame.json");

        /// <summary>The slot in play: the local character and the world it's in. Null in the Testing Ground (never saved).</summary>
        public static string CharacterId, WorldId;

        public class Slot { public string CharacterId, Name, Rank, WorldId, Place, SavedAtUtc; public int Day; }

        /// <summary>Every character on this machine, the last saved first.</summary>
        public static List<Slot> Slots()
        {
            var list = new List<Slot>();
            if (Dir == null) return list;
            MigrateLegacy();
            var folder = Path.Combine(Dir, "characters");
            if (!Directory.Exists(folder)) return list;
            foreach (var f in Directory.GetFiles(folder, "*.json"))
            {
                try
                {
                    var c = JsonUtility.FromJson<CharacterSave>(NameMigration.Apply(File.ReadAllText(f)));
                    if (c?.Player == null) continue;
                    var w = ReadWorld(c.WorldId);
                    list.Add(new Slot { CharacterId = c.Id, Name = c.Name, Rank = c.Player.Rank ?? "Rookie", WorldId = c.WorldId, SavedAtUtc = c.SavedAtUtc,
                        Day = w?.Macro?.Day ?? 0, Place = GameData.MacroMapDatabase.Find(w?.Macro?.CurrentZoneId)?.Name ?? "" });
                }
                catch (Exception e) { Debug.LogWarning($"SaveSystem: skipping {f}: {e.Message}"); }
            }
            return list.OrderByDescending(x => x.SavedAtUtc).ToList();
        }

        public static bool HasSave() => Slots().Count > 0;

        static WorldSave ReadWorld(string id)
        {
            if (string.IsNullOrEmpty(id) || !File.Exists(WorldFile(id))) return null;
            return JsonUtility.FromJson<WorldSave>(NameMigration.Apply(File.ReadAllText(WorldFile(id))));
        }

        /// <summary>New Game: a new character in a new world, saved straight away. False when there's nowhere to save (harness).</summary>
        public static bool Create(string name)
        {
            CharacterId = WorldId = null;
            if (Dir == null || Player.Current == null) return false;
            Player.Current.CharacterName = CleanName(name);
            CharacterId = Guid.NewGuid().ToString("N");
            WorldId = Guid.NewGuid().ToString("N");
            return Save();
        }

        public static string CleanName(string name)
        {
            name = new string((name ?? "").Where(ch => !char.IsControl(ch)).ToArray()).Trim();
            return name.Length == 0 ? "Scavenger" : name.Length > 20 ? name.Substring(0, 20) : name;
        }

        /// <summary>Deletes a character; its world goes too when no other character on this machine is in it.</summary>
        public static bool Delete(string characterId)
        {
            if (Dir == null || string.IsNullOrEmpty(characterId) || !File.Exists(CharFile(characterId))) return false;
            var slot = Slots().FirstOrDefault(x => x.CharacterId == characterId);
            File.Delete(CharFile(characterId));
            if (slot?.WorldId != null && !Slots().Any(x => x.WorldId == slot.WorldId) && File.Exists(WorldFile(slot.WorldId))) File.Delete(WorldFile(slot.WorldId));
            if (CharacterId == characterId) CharacterId = WorldId = null;
            return true;
        }

        /// <summary>The old one-slot savegame.json becomes a slot ("Scavenger"); the old file is kept as savegame.json.migrated.</summary>
        static void MigrateLegacy()
        {
            try
            {
                if (!File.Exists(Legacy)) return;
                var old = JsonUtility.FromJson<SaveData>(NameMigration.Apply(File.ReadAllText(Legacy)));
                if (old?.Player != null && old.Macro != null)
                {
                    string cid = Guid.NewGuid().ToString("N"), wid = Guid.NewGuid().ToString("N");
                    Write(new CharacterSave { Id = cid, Name = "Scavenger", WorldId = wid, SavedAtUtc = old.SavedAtUtc, Player = old.Player },
                          new WorldSave { Id = wid, HostCharacterId = cid, SavedAtUtc = old.SavedAtUtc, Macro = old.Macro });
                }
                File.Move(Legacy, Legacy + ".migrated");
            }
            catch (Exception e) { Debug.LogWarning($"SaveSystem: the old save couldn't be moved over: {e.Message}"); }
        }

        static void Write(CharacterSave c, WorldSave w)
        {
            Directory.CreateDirectory(Path.Combine(Dir, "characters")); Directory.CreateDirectory(Path.Combine(Dir, "worlds"));
            if (w != null) WriteSafe(WorldFile(w.Id), JsonUtility.ToJson(w, true));
            WriteSafe(CharFile(c.Id), JsonUtility.ToJson(c, true));
        }
        static void WriteSafe(string path, string json) { File.WriteAllText(path + ".tmp", json); if (File.Exists(path)) File.Delete(path); File.Move(path + ".tmp", path); }   // a crash mid-write never eats the last save

        public static bool Save()
        {
            var player = Player.Current;
            if (player == null)
            {
                GameLog.Write("Nothing to save yet.");
                return false;
            }
            if (Survival.On) { GameLog.Write("Survival runs aren't saved."); return false; }   // (a run must never overwrite a real slot)
            if (CharacterId == null || Dir == null)
            {
                GameLog.Write(TestingGround.Here ? "The Testing Ground isn't saved." : "Nothing to save to.");
                return false;
            }
            if (!player.IsAlive())
            {
                GameLog.Write("Cannot save while dead.");
                return false;
            }

            var macro = MacroState.Current;
            string now = DateTime.UtcNow.ToString("o");
            var world = new WorldSave
            {
                Id = WorldId, HostCharacterId = CharacterId, SavedAtUtc = now,
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
            var character = new CharacterSave { Id = CharacterId, Name = player.CharacterName, WorldId = WorldId, SavedAtUtc = now, Player = player.CaptureSaveData() };

            try
            {
                Write(character, world);
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

        /// <summary>F9 / the death screen: the slot in play again, else the last saved one.</summary>
        public static bool Load() => Load(CharacterId ?? Slots().FirstOrDefault()?.CharacterId);

        public static bool Load(string characterId)
        {
            Survival.Stop();                                                       // loading a character ends a survival run
            if (Dir == null || string.IsNullOrEmpty(characterId) || !File.Exists(CharFile(characterId)))
            {
                GameLog.Write("No save file found.");
                return false;
            }

            CharacterSave character; WorldSave world;
            try
            {
                character = JsonUtility.FromJson<CharacterSave>(NameMigration.Apply(File.ReadAllText(CharFile(characterId))));
                world = ReadWorld(character?.WorldId);
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveSystem.Load failed: {e}");
                GameLog.Write("Load failed - save file could not be read, see the console for details.");
                return false;
            }
            if (character?.Player == null || world?.Macro == null)
            {
                GameLog.Write("Load failed - save file is corrupt or empty.");
                return false;
            }
            var data = new SaveData { Player = character.Player, Macro = world.Macro };

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
            player.CharacterName = CleanName(character.Name);
            CharacterId = character.Id; WorldId = world.Id;

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
