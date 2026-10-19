// Ported from script.js: travelToSector, triggerZoneEncounter, initializeTacticalEncounterForZone,
// searchArea - then reworked so the game world is the map itself instead of an abstract
// "Overworld" menu screen. There is no more safe-arrival-vs-instant-combat dice roll that
// teleports you into a separate tactical instance: every zone is populated once (see
// SquadManager.EnsureZonePopulated) and stays populated across visits, and whether you end up in
// a fight is now organic - TurnLoop's existing vision/aggro check decides that as you walk
// around, the same way it always drove enemy AI. TravelToSector now fires when the player
// physically walks onto a ZoneExit tile (see PlayerController.WalkToward) rather than a button
// click, and EnterZone tries to drop you in near the exit that leads back the way you came so
// crossing a border feels continuous instead of a teleport to map-center.
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class ZoneTravel
    {
        /// <summary>Ported from travelToSector(targetZoneId), minus the old mode/encounter
        /// branching - see EnterZone for what replaced it.</summary>
        public static bool TravelToSector(string targetZoneId)
        {
            var currentZone = MacroMapDatabase.Find(MacroState.Current.CurrentZoneId);
            var targetZone = MacroMapDatabase.Find(targetZoneId);

            if (targetZone == null)
            {
                GameLog.Write($"Zone {targetZoneId} does not exist.");
                return false;
            }
            if (currentZone != null && MacroMapDatabase.SameMap(currentZone.Id, targetZoneId) && TerrainMap.ZoneId == MacroMapDatabase.MapOf(targetZoneId))
            {
                GameLog.Write($"{targetZone.Name} is on this map - walk there.");   // the map overhaul: regions are walked to, never jumped to
                return false;
            }

            // the map overhaul: any region of the map you're on may hold the way there
            var regions = currentZone == null ? new System.Collections.Generic.List<ZoneDef>() : MacroMapDatabase.RegionsOf(currentZone.Id).ToList();
            bool isConnected = regions.Any(r => r.ConnectedZones != null && r.ConnectedZones.Contains(targetZoneId));
            bool isDiscoveredHidden = regions.Any(r => r.HiddenConnections != null && r.HiddenConnections.Contains(targetZoneId))
                && MacroState.Current.DiscoveredHiddenNodes.Contains(targetZoneId);

            if (!isConnected && !isDiscoveredHidden)
            {
                GameLog.Write($"Cannot travel from {currentZone?.Name} to {targetZone.Name}: Not connected or undiscovered.");
                return false;
            }
            if (targetZone.RequiresKey != null && !MacroState.Current.Keys.Contains(targetZone.RequiresKey))
            {
                if (_lockedNag != targetZoneId) GameLog.Write($"The way into {targetZone.Name} is locked fast. Somebody round here must have the key.");
                _lockedNag = targetZoneId;
                return false;
            }
            _lockedNag = null;
            // a dungeon is a climb down a ladder, not a day on the road
            bool shortHop = targetZone.IsDungeon || (currentZone?.IsDungeon ?? false);

            // Resource consumption / travel cost: crossing to another sector costs roughly the
            // same as crossing an entire map on foot.
            var player = Player.Current;
            if (!shortHop) {
            player.Hunger = System.Math.Max(0, player.Hunger - GameConstants.HungerPerTravel);
            player.Thirst = System.Math.Max(0, player.Thirst - GameConstants.ThirstPerTravel);
            player.Tiredness = System.Math.Max(0, player.Tiredness - GameConstants.TirednessPerTravel);

            var validRations = ConsumableDatabase.ValidTravelRations;
            var ration = player.Inventory.Items.FirstOrDefault(item => item != null && item.Qty > 0 && validRations.Contains(item.Id));
            if (ration != null)
            {
                player.RemoveItem(ration.Id, 1);
                player.Hunger = System.Math.Min(10, player.Hunger + 2);
                player.Thirst = System.Math.Min(10, player.Thirst + 2);
                GameLog.Write("Used a travel ration to ease the journey.");
            }

            // file 00 §7: warn before the next trip would push a stat to 0
            if (player.Hunger < GameConstants.HungerPerTravel || player.Thirst < GameConstants.ThirstPerTravel || player.Tiredness < GameConstants.TirednessPerTravel)
                GameLog.Write("[SURVIVAL] One more crossing like that and you'll be running on empty - eat, drink or rest (Z) first.");
            }

            string previousZoneId = MacroState.Current.CurrentZoneId;
            MacroState.Current.CurrentZoneId = targetZoneId;
            GameLog.Write(shortHop ? $"You climb into {targetZone.Name}." : $"Crossing into {targetZone.Name} (Tier {targetZone.Tier}).");

            EnterZone(targetZoneId, previousZoneId);
            if (!shortHop)
            {
                WorldEvents.OnCrossing(previousZoneId, targetZoneId);
                WorldDays.OnZoneCrossed();
                Atmosphere.RollFor(targetZoneId);      // Batch 31: new place, new weather
            }
            return true;
        }

        static string _lockedNag;

        /// <summary>The map overhaul: every step, the region you're standing in becomes the current zone - its name on
        /// the HUD, its quests and story beats, its Safe Zone rules. No crossing, no time passes.</summary>
        public static void UpdateRegion()
        {
            var p = Player.Current;
            if (p == null || TerrainMap.Tiles == null) return;
            string here = TerrainMap.RegionAt(p.X, p.Y), was = MacroState.Current.CurrentZoneId;
            if (here == null || here == was || !MacroMapDatabase.SameMap(here, was)) return;
            var zone = MacroMapDatabase.Find(here);
            if (zone == null) return;
            MacroState.Current.CurrentZoneId = here;
            GameLog.Write(zone.IsSafeZone ? $"[SAFE ZONE] You walk into {zone.Name}. Weapons hold." : $"You're in {zone.Name} now.");
            StorySystem.OnEnterZone(zone);
            G7Lab.OnEnterZone(zone);   // Batch 88
            QuestSystem.OnEnterZone(here);
            if (zone.IsSafeZone) WorldEvents.TellRumors();
        }

        /// <summary>New: loads a zone as the live, walkable map - builds/restores its terrain,
        /// makes sure it has a hostile population (first visit only) or restores the survivors
        /// from last time, restores or rolls its anomaly field, and drops the player in near
        /// wherever they crossed from. Called both by TravelToSector (walking through an exit)
        /// and once at boot for the starting zone.</summary>
        public static void EnterZone(string zoneId, string cameFromZoneId = null)
        {
            var zone = MacroMapDatabase.Find(zoneId);
            if (zone == null) return;

            GameState.Current.SelectedEnemy = null;
            EncounterState.InCombat = false; EncounterState.ResetFight();
            TerrainMap.BuildTerrainForZone(zoneId); // also recomputes/carves TerrainMap.Exits
            string mapId = TerrainMap.ZoneId;        // the map overhaul: a region loads its tier's whole map
            var regions = MacroMapDatabase.RegionsOf(mapId).ToList();

            var player = Player.Current;
            var spawn = FindEntrySpawn(cameFromZoneId, zoneId);
            player.X = spawn.x;
            player.Y = spawn.y;
            // you're in whichever region of the map you came out in
            string here = TerrainMap.RegionAt(player.X, player.Y);
            if (here != null && MacroMapDatabase.Find(here) != null) { MacroState.Current.CurrentZoneId = here; zone = MacroMapDatabase.Find(here); zoneId = here; }

            EncounterState.Enemies.Clear();
            foreach (var region in regions)
            {
                if (!region.IsSafeZone)
                {
                    SquadManager.EnsureZonePopulated(region);
                    foreach (var squad in SquadManager.GetSquadsInZone(region.Id))
                        EncounterState.Enemies.AddRange(squad.Members.Where(m => m.IsAlive()));
                    SquadManager.PositionSquadsInZone(region.Id);
                }
                else foreach (var squad in SquadManager.GetSquadsInZone(region.Id).Where(s => s.QuestId != null))   // Batch 44: a quest's targets can be in a Safe Zone
                {
                    EncounterState.Enemies.AddRange(squad.Members.Where(m => m.IsAlive()));
                    SquadManager.PositionSquadsInZone(region.Id);
                }
            }
            if (regions.Any(r => !r.IsSafeZone))
            {
                foreach (var e in EncounterState.Enemies.Where(EnemyAI.FriendlyToPlayer)) e.IsAggroed = false;
                // file 04: every NPC starts an encounter Steady; one who fled last time has left for good
                EncounterState.Enemies.RemoveAll(e => e.IsAlive() && e.IsFleeing());
                foreach (var h in EncounterState.Enemies.OfType<HumanEnemy>().Where(h => h.IsAlive() && h.Morale != "Surrendered"))
                { h.Morale = "Steady"; h.HpShaken = false; }
                EncounterState.InCombat = EncounterState.AnyActive();
            }

            if (MacroState.Current.PersistedZoneAnomalies.TryGetValue(mapId, out var savedAnomalies))
            {
                EncounterState.Anomalies = savedAnomalies;
            }
            else
            {
                EncounterState.Anomalies = new System.Collections.Generic.List<Anomaly>();
                // each wild region brings its own share of fields (a tier map has about what its three old zones had)
                int hazardCount = regions.Where(r => !r.IsSafeZone).Sum(r => System.Math.Max(2, WorldEvents.Hazard(r) * 2));
                if (hazardCount > 0) EncounterGeneration.SpawnSectorAnomalies(hazardCount, zone.Tier);
                EncounterState.Anomalies.RemoveAll(a => MacroMapDatabase.Find(TerrainMap.RegionAt(a.X, a.Y))?.IsSafeZone ?? false);
                MacroState.Current.PersistedZoneAnomalies[mapId] = EncounterState.Anomalies;
            }

            player.NoteZoneTier(zone.Tier);
            Companion.OnEnterZone(player);   // Batch 69
            StorySystem.OnEnterZone(zone);
            G7Lab.OnEnterZone(zone);   // Batch 88
            QuestSystem.OnEnterZone(zoneId);

            GameState.Current.Mode = GameMode.Tactical;
            GameState.Current.CurrentTurn = Turn.Player;

            GameLog.Write(zone.IsSafeZone
                ? $"[SAFE ZONE] Entered {zone.Name}. Weapons hold."
                : $"Entered {zone.Name} (Tier {zone.Tier} | Hazard {WorldEvents.Hazard(zone)}).");
            if (zone.IsSafeZone) WorldEvents.TellRumors();
        }

        /// <summary>Drops the player near the exit that leads back to whichever zone they just
        /// came from, so crossing a border feels continuous rather than a jump to map-center -
        /// falls back to the center when there's no reciprocal exit (boot, or a one-way hidden
        /// node) or no cameFromZoneId was given.</summary>
        private static (int x, int y) FindEntrySpawn(string cameFromZoneId, string zoneId = null)
        {
            if (cameFromZoneId != null)
            {
                var reciprocal = TerrainMap.Exits.FirstOrDefault(e => e.TargetZoneId == cameFromZoneId)
                    ?? TerrainMap.Exits.FirstOrDefault(e => MacroMapDatabase.SameMap(e.TargetZoneId, cameFromZoneId));   // Batch 120: you came from another region of that map
                if (reciprocal != null)
                    return TerrainMap.FindNearestEnterable(reciprocal.X, reciprocal.Y);
            }
            // the map overhaul: New Game and loads put you at the region's arrival spot (Bonfire Village's fire), or its middle
            var pinned = TerrainMap.ArrivalAnchor(zoneId);
            if (pinned.HasValue) return TerrainMap.FindNearestEnterable(pinned.Value.x, pinned.Value.y);
            if (zoneId != null && MacroMapDatabase.SameMap(zoneId, TerrainMap.ZoneId) && MacroMapDatabase.RegionsOf(zoneId).Count() > 1) return TerrainMap.RegionCenter(zoneId);
            return TerrainMap.FindNearestEnterable(TerrainMap.Width / 2, TerrainMap.Height / 2);
        }

        // Batch 50: a hidden way is a place on the map (where its gate will be), not a skill roll from anywhere in the
        // zone. Walking within your sensing range of it finds it: 3 tiles + your Perception modifier, minus 1 per point
        // the old DC was above 14, at least 1, and +3 if you've heard a rumor about a way in from here. F searches
        // carefully (twice that range) and otherwise tells you which way the ground looks disturbed, if it's within 30.
        public static int HiddenSenseRange(ZoneDef hidden, ZoneDef from)
        {
            var p = Player.Current;
            int r = 3 + (p?.PerceptionMod ?? 0) - System.Math.Max(0, (hidden.AccessCondition?.Dc ?? 14) - 14);
            bool heard = MacroState.Current.Rumors.Any(x => x.Contains(hidden.Name) && x.Contains(from.Name));
            return System.Math.Max(1, r) + (heard ? 3 : 0);
        }

        static System.Collections.Generic.IEnumerable<(ZoneDef hidden, int dist, int dx, int dy)> HiddenNear(ZoneDef zone)
        {
            var p = Player.Current;
            if (zone == null || p == null) yield break;
            var hiddenIds = MacroMapDatabase.RegionsOf(zone.Id).Where(r => r.HiddenConnections != null).SelectMany(r => r.HiddenConnections).Distinct().ToList();
            foreach (var id in hiddenIds.Where(id => !MacroState.Current.DiscoveredHiddenNodes.Contains(id)))
            {
                var hz = MacroMapDatabase.Find(id);
                if (hz?.AccessCondition == null) continue;
                var (x, y) = ZoneExitPlacer.GatePos(zone, id);
                yield return (hz, System.Math.Max(System.Math.Abs(x - p.X), System.Math.Abs(y - p.Y)), x - p.X, y - p.Y);
            }
        }

        /// <summary>Every step: you notice a hidden way you walk close to.</summary>
        public static void SenseHiddenWays()
        {
            var zone = MacroMapDatabase.Find(MacroState.Current.CurrentZoneId);
            var found = HiddenNear(zone).Where(h => h.dist <= HiddenSenseRange(h.hidden, zone)).ToList();
            foreach (var h in found) Discover(h.hidden);
            if (found.Count > 0) TerrainMap.RecomputeExits();
        }

        public static void SearchArea()
        {
            var zone = MacroMapDatabase.Find(MacroState.Current.CurrentZoneId);
            var hiddenIds = zone == null ? new System.Collections.Generic.List<string>() : MacroMapDatabase.RegionsOf(zone.Id).Where(r => r.HiddenConnections != null).SelectMany(r => r.HiddenConnections).ToList();
            if (hiddenIds.Count == 0 || hiddenIds.All(id => MacroState.Current.DiscoveredHiddenNodes.Contains(id)))
            {
                GameLog.Write(hiddenIds.Count == 0 ? "There is nothing hidden to find here." : "You have already found everything hidden in this area.");
                return;
            }
            var near = HiddenNear(zone).OrderBy(h => h.dist).ToList();
            var found = near.Where(h => h.dist <= 2 * HiddenSenseRange(h.hidden, zone)).ToList();
            foreach (var h in found) Discover(h.hidden);
            if (found.Count > 0) { TerrainMap.RecomputeExits(); return; }
            var hint = near.FirstOrDefault(h => h.dist <= 30);
            if (hint.hidden != null) GameLog.Write($"The ground to the {Compass(hint.dx, hint.dy)} looks disturbed - something's over there.");
            else GameLog.Write("Nothing here. If there's a hidden way, it's somewhere else in this zone.");
        }

        static void Discover(ZoneDef hidden)
        {
            MacroState.Current.DiscoveredHiddenNodes.Add(hidden.Id);
            GameLog.Write($"You discover a hidden path to {hidden.Name}!");
            QuestSystem.OnHiddenNodeDiscovered(hidden.Id);
        }

        /// <summary>"north", "south-east"... for an offset on the map (y grows southward).</summary>
        public static string Compass(int dx, int dy)
        {
            string ns = System.Math.Abs(dy) * 2 >= System.Math.Abs(dx) ? (dy < 0 ? "north" : "south") : "";
            string ew = System.Math.Abs(dx) * 2 >= System.Math.Abs(dy) ? (dx < 0 ? "west" : "east") : "";
            return ns.Length > 0 && ew.Length > 0 ? ns + "-" + ew : ns + ew;
        }
    }
}
