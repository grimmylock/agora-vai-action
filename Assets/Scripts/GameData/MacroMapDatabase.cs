// Ported from database.js: MACRO_MAP_DB + validateMapDatabase()
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameData
{
    public class AccessCondition
    {
        public string RequiresCheck;
        public int Dc;
    }

    public class ZoneDef
    {
        public string Id;
        public string Name;
        public int Tier;
        public int BaseHazardModifier;
        public List<string> ConnectedZones = new List<string>();
        public List<string> AllowedFactions = new List<string>();
        public List<string> HiddenConnections;      // null if the zone has none
        public bool IsSafeZone;
        public AccessCondition AccessCondition;      // null for standard surface-mesh nodes
        // The map overhaul (TierMaps.cs): a region shares its tier's one big map; MapId names the zone that owns
        // that map (null = the zone has a map of its own). Walking between regions of one map is free.
        public string MapId;
        public bool IsDungeon;                       // a small underground map (TierDungeons.cs), entered by a hatch or door
        public bool Upstairs;                        // Batch 120: a building's upper floor (indoor daylight, not underground)
        public string RequiresKey;                   // the way in is locked until you carry this key (MacroState.Keys)
    }

    public static class MacroMapDatabase
    {
        public static readonly List<ZoneDef> Zones = new List<ZoneDef>
        {
            // Lower Tier (T1)
            // New: IsSafeZone=true, marking these as traders - previously scavengers_hub was the
            // ONLY safe zone in the entire 20-zone map, meaning the whole tiered weapon/armor
            // catalog (see ShopDatabase) was reachable from a single Tier 3 outpost with no earlier
            // or later trading post at all. Spread across the tier ladder instead so ShopUI's new
            // per-trader tier cap actually means something: an early trader for starting gear, a
            // mid trader, the existing hub, and a late-game trader - Tier 5 weapons/armor are
            // intentionally left with no trader stocking them, so end-game gear has to be found
            // (loot/ElectronicLock), not just bought once you have enough RU.
            // The map overhaul: Tier 1 is one map (owned by "perimeter"); the marsh, the run and Bonfire Village are its
            // regions. The Safe Zone is now the village, not the whole Perimeter.
            new ZoneDef { Id = "perimeter", Name = "The Perimeter", Tier = 1, BaseHazardModifier = 0, ConnectedZones = { "marshes", "smugglers_run", "bonfire_village", "chapel_crypt", "farmhouse_upper" }, AllowedFactions = { "Wanderers", "Raiders" }, HiddenConnections = new List<string> { "secret_cave" } },
            new ZoneDef { Id = "marshes", Name = "Toxic Marshes", Tier = 1, BaseHazardModifier = 0, ConnectedZones = { "perimeter", "smugglers_run", "scrapyard", "pumping_basement", "flooded_bunker" }, AllowedFactions = { "Wanderers", "Raiders" }, MapId = "perimeter" },
            new ZoneDef { Id = "smugglers_run", Name = "Smuggler's Run", Tier = 1, BaseHazardModifier = 0, ConnectedZones = { "perimeter", "marshes", "industrial_basin", "smugglers_tunnel" }, AllowedFactions = { "Wanderers", "Raiders" }, MapId = "perimeter" },
            new ZoneDef { Id = "bonfire_village", Name = "Bonfire Village", Tier = 1, BaseHazardModifier = 0, ConnectedZones = { "perimeter" }, AllowedFactions = { "Wanderers" }, IsSafeZone = true, MapId = "perimeter" },

            // Low-Mid Tier (T2)
            // Tier 2 is one map (owned by "scrapyard"): the Industrial Basin and Black Valley are its regions
            new ZoneDef { Id = "scrapyard", Name = "The Scrapyard", Tier = 2, BaseHazardModifier = 1, ConnectedZones = { "marshes", "industrial_basin", "black_valley", "crusher_pit" }, AllowedFactions = { "Raiders", "Wanderers" } },
            // ponytail: no IsSafeZone here on purpose - the TTRPG source's Zone Map calls Industrial
            // Basin out explicitly as a transit zone with no static trader ("that absence is
            // intentional texture, not an oversight"). Wandering Traders (Batch 4) cover it instead.
            new ZoneDef { Id = "industrial_basin", Name = "Industrial Basin", Tier = 2, BaseHazardModifier = 1, ConnectedZones = { "smugglers_run", "scrapyard", "black_valley" }, AllowedFactions = { "Wanderers", "Garrison" }, HiddenConnections = new List<string> { "secret_lab_alpha" }, MapId = "scrapyard" },
            new ZoneDef { Id = "black_valley", Name = "Black Valley", Tier = 2, BaseHazardModifier = 1, ConnectedZones = { "scrapyard", "industrial_basin", "scavengers_hub", "valley_mine" }, AllowedFactions = { "Raiders", "Liberators" }, MapId = "scrapyard" },

            // Middle Tier (T3)
            // Tier 3 is one map (owned by the hub, which stays a Safe Zone): the Rail-Yards and Dried Lake are its regions
            new ZoneDef { Id = "scavengers_hub", Name = "Scavenger's Hub", Tier = 3, BaseHazardModifier = 0, ConnectedZones = { "black_valley", "rail_yards", "dried_lake" }, AllowedFactions = { "Sentinels", "Wanderers" }, IsSafeZone = true },
            new ZoneDef { Id = "rail_yards", Name = "The Rail-Yards", Tier = 3, BaseHazardModifier = 2, ConnectedZones = { "scavengers_hub", "dried_lake", "vanguard_barracks" }, AllowedFactions = { "Contractors", "Sentinels" }, HiddenConnections = new List<string> { "maintenance_tunnels" }, MapId = "scavengers_hub" },
            new ZoneDef { Id = "dried_lake", Name = "Dried Lake Sub-Station", Tier = 3, BaseHazardModifier = 2, ConnectedZones = { "scavengers_hub", "rail_yards", "ghost_town", "stranded_ferry" }, AllowedFactions = { "Surveyors", "Mutant" }, HiddenConnections = new List<string> { "sub_station_depths" }, MapId = "scavengers_hub" },

            // High-Mid Tier (T4)
            // Tier 4 is one map (owned by the barracks, which stay a Safe Zone): Ghost Town and Crimson Woods are its regions
            new ZoneDef { Id = "vanguard_barracks", Name = "Vanguard Barracks", Tier = 4, BaseHazardModifier = 3, ConnectedZones = { "rail_yards", "crimson_woods", "ghost_town" }, AllowedFactions = { "Liberators", "Sentinels" }, IsSafeZone = true },
            new ZoneDef { Id = "ghost_town", Name = "Ghost Town", Tier = 4, BaseHazardModifier = 3, ConnectedZones = { "dried_lake", "crimson_woods", "vanguard_barracks", "school_upper", "flats_2" }, AllowedFactions = { "Contractors", "Raiders" }, HiddenConnections = new List<string> { "secret_lab_beta" }, MapId = "vanguard_barracks" },
            new ZoneDef { Id = "crimson_woods", Name = "Crimson Woods", Tier = 4, BaseHazardModifier = 3, ConnectedZones = { "vanguard_barracks", "ghost_town", "quarry_basin", "camp_shelter" }, AllowedFactions = { "Obelisk", "Mutant", "Sentinels" }, HiddenConnections = new List<string> { "deep_caverns" }, MapId = "vanguard_barracks" },

            // High Tier (T5)
            // Tier 5 is one map (owned by the quarry): the Ship Graveyard and the Antenna Grid are its regions
            new ZoneDef { Id = "quarry_basin", Name = "Quarry Basin", Tier = 5, BaseHazardModifier = 4, ConnectedZones = { "crimson_woods", "ship_graveyard", "antenna_grid" }, AllowedFactions = { "Contractors", "Obelisk" }, HiddenConnections = new List<string> { "underground_labyrinth" } },
            new ZoneDef { Id = "ship_graveyard", Name = "Ship Graveyard", Tier = 5, BaseHazardModifier = 4, ConnectedZones = { "quarry_basin", "antenna_grid", "drydock_tunnels" }, AllowedFactions = { "Wanderers", "Raiders" }, HiddenConnections = new List<string> { "lattice_foundry" }, MapId = "quarry_basin" },
            new ZoneDef { Id = "antenna_grid", Name = "The Antenna Grid", Tier = 5, BaseHazardModifier = 4, ConnectedZones = { "quarry_basin", "ship_graveyard", "abandoned_city" }, AllowedFactions = { "Obelisk", "Mutant" }, HiddenConnections = new List<string> { "secret_lab_gamma" }, MapId = "quarry_basin" },

            // Extreme Tier (T6)
            // Tier 6 is one map (owned by the city): the Epicenter and the Siege Ward are its regions
            new ZoneDef { Id = "abandoned_city", Name = "Abandoned City", Tier = 6, BaseHazardModifier = 5, ConnectedZones = { "antenna_grid", "outskirts", "hospital" }, AllowedFactions = { "Obelisk", "Garrison" }, HiddenConnections = new List<string> { "underground_bunker" } },
            new ZoneDef { Id = "outskirts", Name = "Anomalous Epicenter", Tier = 6, BaseHazardModifier = 5, ConnectedZones = { "abandoned_city" }, AllowedFactions = { "Obelisk" }, MapId = "abandoned_city" },
            new ZoneDef { Id = "hospital", Name = "The Siege Ward", Tier = 6, BaseHazardModifier = 5, ConnectedZones = { "abandoned_city", "reactor_core", "hospital_morgue" }, AllowedFactions = { "Obelisk", "Garrison" }, HiddenConnections = new List<string> { "siege_catacombs" }, MapId = "abandoned_city" },

            // Certain Death (T7)
            new ZoneDef { Id = "reactor_core", Name = "The Reactor Core", Tier = 7, BaseHazardModifier = 6, ConnectedZones = { "hospital" }, AllowedFactions = { "Obelisk" } },

            // Hidden Sub-Nodes
            new ZoneDef { Id = "secret_cave", Name = "Secret Cave Area", Tier = 1, BaseHazardModifier = 1, ConnectedZones = { "perimeter" }, AllowedFactions = { "Mutant" }, IsDungeon = true, AccessCondition = new AccessCondition { RequiresCheck = "Perception", Dc = 14 } },

            // Tier 1 dungeons (TierDungeons.cs): found by walking up to them, not by a roll
            new ZoneDef { Id = "chapel_crypt", Name = "Chapel Crypt", Tier = 1, BaseHazardModifier = 1, ConnectedZones = { "perimeter" }, AllowedFactions = { "Mutant" }, IsDungeon = true },
            // Batch 120: upper floors of buildings, their own small maps (TierDungeons.Upstairs, ArtSource/plans/house_plans.py)
            new ZoneDef { Id = "testing_ground", Name = "Testing Ground", Tier = 1, BaseHazardModifier = 0 },   // the menu's dev range (TestingGround.cs): no exits, no factions
            new ZoneDef { Id = "farmhouse_upper", Name = "Farmhouse - Upstairs", Tier = 1, BaseHazardModifier = 0, ConnectedZones = { "perimeter" }, IsDungeon = true, Upstairs = true },
            new ZoneDef { Id = "school_upper", Name = "School - First Floor", Tier = 4, BaseHazardModifier = 2, ConnectedZones = { "ghost_town" }, IsDungeon = true, Upstairs = true },
            new ZoneDef { Id = "flats_2", Name = "The Flats - Second Floor", Tier = 4, BaseHazardModifier = 2, ConnectedZones = { "ghost_town", "flats_3" }, IsDungeon = true, Upstairs = true },
            new ZoneDef { Id = "flats_3", Name = "The Flats - Top Floor", Tier = 4, BaseHazardModifier = 3, ConnectedZones = { "flats_2" }, IsDungeon = true, Upstairs = true },
            new ZoneDef { Id = "pumping_basement", Name = "Pumping Station Basement", Tier = 1, BaseHazardModifier = 1, ConnectedZones = { "marshes" }, AllowedFactions = { "Mutant" }, IsDungeon = true },
            new ZoneDef { Id = "flooded_bunker", Name = "Flooded Bunker", Tier = 1, BaseHazardModifier = 2, ConnectedZones = { "marshes" }, AllowedFactions = { "Mutant" }, IsDungeon = true, RequiresKey = "flooded_bunker" },
            new ZoneDef { Id = "smugglers_tunnel", Name = "Smugglers' Tunnel", Tier = 1, BaseHazardModifier = 1, ConnectedZones = { "smugglers_run" }, AllowedFactions = { "Raiders", "Mutant" }, IsDungeon = true },
            new ZoneDef { Id = "crusher_pit", Name = "The Crusher Pit", Tier = 2, BaseHazardModifier = 2, ConnectedZones = { "scrapyard" }, AllowedFactions = { "Mutant" }, IsDungeon = true },
            new ZoneDef { Id = "valley_mine", Name = "The Valley Mine", Tier = 2, BaseHazardModifier = 2, ConnectedZones = { "black_valley" }, AllowedFactions = { "Mutant" }, IsDungeon = true },
            new ZoneDef { Id = "stranded_ferry", Name = "The Stranded Ferry", Tier = 3, BaseHazardModifier = 3, ConnectedZones = { "dried_lake" }, AllowedFactions = { "Mutant" }, IsDungeon = true },
            new ZoneDef { Id = "camp_shelter", Name = "The Camp Shelter", Tier = 4, BaseHazardModifier = 4, ConnectedZones = { "crimson_woods" }, AllowedFactions = { "Mutant" }, IsDungeon = true },
            new ZoneDef { Id = "drydock_tunnels", Name = "The Dry Dock Tunnels", Tier = 5, BaseHazardModifier = 5, ConnectedZones = { "ship_graveyard" }, AllowedFactions = { "Mutant" }, IsDungeon = true },
            new ZoneDef { Id = "hospital_morgue", Name = "The Hospital Morgue", Tier = 6, BaseHazardModifier = 6, ConnectedZones = { "hospital" }, AllowedFactions = { "Mutant" }, IsDungeon = true },
            new ZoneDef { Id = "secret_lab_alpha", Name = "Secret Lab Alpha", Tier = 2, BaseHazardModifier = 2, ConnectedZones = { "industrial_basin" }, AccessCondition = new AccessCondition { RequiresCheck = "Perception", Dc = 15 } },
            new ZoneDef { Id = "sub_station_depths", Name = "Sub-Station Depths", Tier = 3, BaseHazardModifier = 3, ConnectedZones = { "dried_lake" }, AccessCondition = new AccessCondition { RequiresCheck = "Perception", Dc = 16 } },
            new ZoneDef { Id = "maintenance_tunnels", Name = "Maintenance Tunnels", Tier = 3, BaseHazardModifier = 3, ConnectedZones = { "rail_yards" }, AccessCondition = new AccessCondition { RequiresCheck = "Perception", Dc = 15 } },
            new ZoneDef { Id = "secret_lab_beta", Name = "Secret Lab Beta", Tier = 4, BaseHazardModifier = 4, ConnectedZones = { "ghost_town" }, AccessCondition = new AccessCondition { RequiresCheck = "Perception", Dc = 17 } },
            new ZoneDef { Id = "deep_caverns", Name = "Deep Caverns", Tier = 4, BaseHazardModifier = 4, ConnectedZones = { "crimson_woods" }, AccessCondition = new AccessCondition { RequiresCheck = "Perception", Dc = 16 } },
            new ZoneDef { Id = "underground_labyrinth", Name = "Underground Labyrinth", Tier = 5, BaseHazardModifier = 5, ConnectedZones = { "quarry_basin" }, AccessCondition = new AccessCondition { RequiresCheck = "Perception", Dc = 18 } },
            new ZoneDef { Id = "secret_lab_gamma", Name = "Secret Lab Gamma", Tier = 5, BaseHazardModifier = 5, ConnectedZones = { "antenna_grid" }, AccessCondition = new AccessCondition { RequiresCheck = "Perception", Dc = 18 } },
            new ZoneDef { Id = "lattice_foundry", Name = "The Lattice Foundry", Tier = 5, BaseHazardModifier = 5, ConnectedZones = { "ship_graveyard", "lattice_foundry_lower" }, AccessCondition = new AccessCondition { RequiresCheck = "Perception", Dc = 18 } },   // Batch 88: the G-7's lab (G7Lab)
            new ZoneDef { Id = "lattice_foundry_lower", Name = "The Lattice Foundry - Synthesis Level", Tier = 5, BaseHazardModifier = 5, ConnectedZones = { "lattice_foundry" }, IsDungeon = true },   // Batch 91: its second floor
            new ZoneDef { Id = "underground_bunker", Name = "Underground Bunker", Tier = 6, BaseHazardModifier = 6, ConnectedZones = { "abandoned_city" }, AccessCondition = new AccessCondition { RequiresCheck = "Perception", Dc = 19 } },
            new ZoneDef { Id = "siege_catacombs", Name = "Siege Catacombs", Tier = 6, BaseHazardModifier = 6, ConnectedZones = { "hospital" }, AccessCondition = new AccessCondition { RequiresCheck = "Perception", Dc = 19 } },
        };

        public static ZoneDef Find(string id) => id == null ? null : Zones.FirstOrDefault(z => z.Id == id);   // the list can change (an ending removes zones): no cache

        /// <summary>The zone whose map this zone is drawn on: itself, or the tier map's owner for a region.</summary>
        public static string MapOf(string id) => id == null ? null : Find(id)?.MapId ?? id;

        /// <summary>Every zone drawn on this map (the owner first, then its regions).</summary>
        public static IEnumerable<ZoneDef> RegionsOf(string mapId)
        {
            mapId = MapOf(mapId);
            foreach (var z in Zones) if (z.Id == mapId) yield return z;
            foreach (var z in Zones) if (z.MapId == mapId && z.Id != mapId) yield return z;
        }

        public static bool SameMap(string a, string b) => a != null && b != null && MapOf(a) == MapOf(b);

        /// <summary>
        /// Ported 1:1 from validateMapDatabase() in database.js. Checks: no duplicate ids, valid
        /// tier range, all connectedZones/hiddenConnections point to real zones, hidden nodes
        /// point back to their parent, and standard (non-hidden) links are symmetric.
        /// Call this once at startup (e.g. from a bootstrap MonoBehaviour's Awake()).
        /// </summary>
        public static bool Validate()
        {
            bool isValid = true;
            var ids = new HashSet<string>(Zones.Select(z => z.Id));
            var zoneMap = Zones.ToDictionary(z => z.Id, z => z);

            if (ids.Count != Zones.Count)
            {
                Debug.LogError("FATAL: Duplicate Zone IDs detected in MacroMapDatabase.");
                isValid = false;
            }

            foreach (var zone in Zones)
            {
                if (zone.Tier < 1 || zone.Tier > 7)
                {
                    Debug.LogError($"FATAL: Zone {zone.Id} has invalid tier {zone.Tier}.");
                    isValid = false;
                }

                // 1. Check existence of standard connectedZones
                if (zone.ConnectedZones != null)
                {
                    foreach (var targetId in zone.ConnectedZones)
                    {
                        if (!ids.Contains(targetId))
                        {
                            Debug.LogError($"FATAL: Broken link from {zone.Id} to non-existent zone {targetId}.");
                            isValid = false;
                        }
                    }
                }

                // 2. Check hidden-node consistency (parent references hidden node, hidden node points back)
                if (zone.HiddenConnections != null)
                {
                    foreach (var hiddenId in zone.HiddenConnections)
                    {
                        if (!ids.Contains(hiddenId))
                        {
                            Debug.LogError($"FATAL: Broken hidden link from {zone.Id} to non-existent sub-node {hiddenId}.");
                            isValid = false;
                        }
                        else
                        {
                            var hiddenNode = zoneMap[hiddenId];
                            if (hiddenNode.ConnectedZones == null || !hiddenNode.ConnectedZones.Contains(zone.Id))
                            {
                                Debug.LogError($"FATAL: Hidden node {hiddenId} does not point back to parent {zone.Id} in connectedZones.");
                                isValid = false;
                            }
                        }
                    }
                }

                if (zone.AllowedFactions != null)
                {
                    foreach (var faction in zone.AllowedFactions)
                    {
                        if (!FactionDatabase.IsValidFaction(faction))
                        {
                            Debug.LogError($"FATAL: Zone {zone.Id} specifies unknown allowed faction '{faction}' in AllowedFactions.");
                            isValid = false;
                        }
                    }
                }
            }

            // 3. Check reverse-link symmetry for standard surface mesh nodes
            foreach (var zone in Zones)
            {
                if (zone.AccessCondition != null) continue; // Skip hidden sub-nodes for standard symmetry checks

                if (zone.ConnectedZones != null)
                {
                    foreach (var targetId in zone.ConnectedZones)
                    {
                        if (zoneMap.TryGetValue(targetId, out var targetZone) && targetZone.AccessCondition == null)
                        {
                            if (targetZone.ConnectedZones == null || !targetZone.ConnectedZones.Contains(zone.Id))
                            {
                                Debug.LogError($"FATAL: Asymmetric link error: {zone.Id} connects to {targetId}, but {targetId} does not link back.");
                                isValid = false;
                            }
                        }
                    }
                }
            }

            if (isValid)
                Debug.Log("Macro Map Database integrity check passed.");
            else
                Debug.LogError("Macro Map Database integrity check FAILED.");

            return isValid;
        }
    }
}
