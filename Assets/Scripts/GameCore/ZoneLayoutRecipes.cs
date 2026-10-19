// Authored terrain for every zone (see 06_Zone_Map.md, the source reference doc). The map overhaul:
// Tiers 1-6 are one big map each now (TierMaps.cs) and their dungeons small full-size maps (TierDungeons.cs);
// what's left here is the Reactor Core and the original hidden sub-nodes. The notes below are from before. Grid sizes are
// roughly 2-2.2x the reference doc's suggested size per the user's request that 32x32 was "too
// small" for real exploration; hidden sub-nodes stay smaller since they're small interiors by
// design.
//
// Each method builds one zone's TerrainCanvas from stamps matching that zone's terrain
// description and named landmarks, then bakes it to a GameData.MapLayout. Where the doc gives an
// exact Room-grid for a landmark (RoomGrids.cs, generated from the doc) it is stamped in at 2-4x
// scale; map-sketch zones follow the sketch's north-to-south order and compass-tagged exits.
// Looks ("tree", "container"...) and floors (GroundType) are cosmetic - see ZoneThemes for what a
// zone uses when a stamp doesn't say. Landmark(...) pins a LandmarkDatabase entry to its spot;
// Exit(...) pins the walkable exit to a connected zone.
//
// Rules legend (TerrainTile.cs): Solid # · ScatterCover C (light, +1 AC) · HardCover = (+2 AC) ·
// Difficult ~ (2 MP) · Door D · Barrel (explodes) · Height = real elevation.
using System.Collections.Generic;
using GameData;
using static GameCore.TileType;
using static GameCore.GroundType;

namespace GameCore
{
    public static class ZoneLayoutRecipes
    {
        /// <summary>Batch 30: every zone is authored small and blown up by MapUpscaler (3x for the open
        /// zones, 2x for hidden interiors) so maps are big enough to explore. The map overhaul: tier maps and
        /// dungeons are authored at full size and skip the upscaler.</summary>
        public static Dictionary<string, MapLayout> BuildAll()
        {
            var all = BuildAuthored();
            var big = new Dictionary<string, MapLayout>();
            foreach (var kv in all)
            {
                // tier maps and dungeons are authored at their final size (TierMaps.cs): no upscale pass
                var src = kv.Value.FullSize ? kv.Value : MapUpscaler.Upscale(kv.Value, kv.Key, IsInterior(kv.Key) ? MapUpscaler.InteriorScale : MapUpscaler.Scale);
                big[kv.Key] = Grit.Apply(CoverWrap.Wrap(MapDresser.Dress(BuildingCarver.Carve(src, kv.Key), kv.Key), kv.Key), kv.Key, IsInterior(kv.Key));   // Batch 72: cover that wraps; 92: grit
            }
            return big;
        }

        /// <summary>Batch 120: a building's upper floor - indoors (no roof drawn, no weather inside) but in daylight, not underground.</summary>
        public static bool IsUpstairs(string zoneId) => zoneId != null && (MacroMapDatabase.Find(zoneId)?.Upstairs ?? false);

        public static bool IsInterior(string zoneId) =>
            zoneId != null && (zoneId.StartsWith("secret_") || zoneId.StartsWith("underground_") || zoneId == "deep_caverns"
                               || zoneId == "sub_station_depths" || zoneId == "maintenance_tunnels" || zoneId == "siege_catacombs" || zoneId == "lattice_foundry"
                               || (MacroMapDatabase.Find(zoneId)?.IsDungeon ?? false));

        private static Dictionary<string, MapLayout> BuildAuthored()
        {
            return new Dictionary<string, MapLayout>
            {
                // the map overhaul: Tier 1 is one map; Toxic Marshes, Smuggler's Run and Bonfire Village are regions of it
                ["perimeter"] = TierMaps.Tier1(),
                ["chapel_crypt"] = TierDungeons.ChapelCrypt(),
                ["pumping_basement"] = TierDungeons.PumpingBasement(),
                ["flooded_bunker"] = TierDungeons.FloodedBunker(),
                ["smugglers_tunnel"] = TierDungeons.SmugglersTunnel(),

                // Tier 2 is one map too (owned by "scrapyard")
                ["scrapyard"] = TierMaps.Tier2(),
                ["crusher_pit"] = TierDungeons.CrusherPit(),
                ["valley_mine"] = TierDungeons.ValleyMine(),

                // Tier 3 is one map (owned by the hub)
                ["scavengers_hub"] = TierMaps.Tier3(),
                ["stranded_ferry"] = TierDungeons.StrandedFerry(),

                // Tier 4 is one map (owned by the barracks)
                ["vanguard_barracks"] = TierMaps.Tier4(),
                ["camp_shelter"] = TierDungeons.CampShelter(),

                // Tier 5 is one map (owned by the quarry)
                ["quarry_basin"] = TierMaps.Tier5(),
                ["drydock_tunnels"] = TierDungeons.DrydockTunnels(),

                // Tier 6 is one map (owned by the city)
                ["abandoned_city"] = TierMaps.Tier6(),
                ["hospital_morgue"] = TierDungeons.HospitalMorgue(),

                ["reactor_core"] = ReactorCore(),

                ["secret_cave"] = TierDungeons.SecretCave(),
                ["secret_lab_alpha"] = TierDungeons.Lab("secret_lab_alpha", "concrete", LabTile, "Reception Airlock", "Sample Storage", "industrial_basin"),   // Batch 92: a floor plan
                ["sub_station_depths"] = TierDungeons.Lab("sub_station_depths", "concrete", Concrete, "Flooded Corridor", "Transformer Vault", "dried_lake"),   // Batch 92: a floor plan
                ["maintenance_tunnels"] = TierDungeons.Lab("maintenance_tunnels", "concrete", Concrete, "Tunnel Junction", "Collapsed Section", "rail_yards"),   // Batch 92: a floor plan
                ["secret_lab_beta"] = TierDungeons.Lab("secret_lab_beta", "plaster", LabTile, "Archive Room", "Isolation Ward", "ghost_town"),   // Batch 92: a floor plan
                ["deep_caverns"] = TierDungeons.Lab("deep_caverns", "cliff", CaveFloor, "Root Hall", "Crystal Pool", "crimson_woods"),   // Batch 100: a plan
                ["underground_labyrinth"] = TierDungeons.Lab("underground_labyrinth", "cliff", CaveFloor, "The Fork", "Cave-In Chamber", "quarry_basin"),   // Batch 100: a plan
                ["secret_lab_gamma"] = TierDungeons.Lab("secret_lab_gamma", "concrete", LabTile, "Signal Room", "Sealed Vault", "antenna_grid"),   // Batch 92: a floor plan
                ["lattice_foundry"] = TierDungeons.LatticeUpper(),         // Batch 91: floor plans (TierDungeons.Plans.cs)
                [TestingGround.Zone] = TestingGround.Map(),   // the menu's testing ground
                ["farmhouse_upper"] = TierDungeons.Upstairs("farmhouse_upper", "wall", "perimeter"),   // Batch 120: upper floors
                ["school_upper"] = TierDungeons.Upstairs("school_upper", "wall", "ghost_town"),
                ["flats_2"] = TierDungeons.Upstairs("flats_2", "plaster", "ghost_town", "flats_3"),
                ["flats_3"] = TierDungeons.Upstairs("flats_3", "plaster", "flats_2"),
                ["lattice_foundry_lower"] = TierDungeons.LatticeLower(),
                ["underground_bunker"] = TierDungeons.Lab("underground_bunker", "concrete", Concrete, "Blast Door Foyer", "Ration Stores", "abandoned_city"),   // Batch 92: a floor plan
                ["siege_catacombs"] = TierDungeons.Lab("siege_catacombs", "wall", CaveFloor, "Ossuary", "Sealed Crypt", "hospital"),   // Batch 100: a plan
            };
        }

        private static int Seed(string id) => TerrainCanvas.Seed(id);

        // ---------------------------------------------------------------- Tier 7

        // 68x68 finale. Containment Hall + Control Room (room-grid) · Cooling Tunnels (room-grid
        // + carved maze) · The Last Gate. W -> Siege Ward.
        private static MapLayout ReactorCore()
        {
            var c = new TerrainCanvas(68, 68, Seed("reactor_core"));
            c.FillAll(Solid, 2);                                       // rock/wall mass
            c.Stamp("Containment Hall + Control Room", 11, 20, 2, "concrete", Concrete, 0, "pipes");
            c.Landmark("Control Room", 33, 22).Landmark("Containment Hall", 33, 32);
            c.Stamp("Cooling Tunnels", 4, 44, 3, "concrete", Concrete);
            c.RandomWalk(12, 40, 60, 3, Empty, 0);
            c.RandomWalk(34, 40, 60, 3, Empty, 0);
            c.Line(6, 30, 11, 30, 3, Empty);                           // west approach into the hall (E1)
            c.Line(4, 44, 6, 30, 3, Empty);
            c.Landmark("Cooling Tunnels", 10, 50);
            c.Line(57, 30, 64, 30, 3, Empty);                          // east to The Last Gate (E2)
            c.Rect(62, 27, 3, 7, Empty);
            c.Rect(65, 28, 2, 5, Solid, 0, "metaldoor");
            c.Landmark("The Last Gate", 63, 30);
            c.Border(Solid, 1, 2, "concrete");
            c.Scatter(ScatterCover, 0.02f, Empty, 0, "debris");
            c.Exit("hospital", 0, 30);
            return c.ToMapLayout();
        }

        // ---------------------------------------------------------------- Hidden Sub-Nodes
        // Rock/concrete mass with the design's room-grid stamped in the middle at 4x, a couple of
        // organic side passages, and the way back out pinned to the grid's E cell.
    }
}
