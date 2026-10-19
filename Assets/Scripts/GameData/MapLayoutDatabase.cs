// Ported from database.js: MAP_LAYOUTS
//
// Authored terrain for the tactical grid, keyed by MacroMapDatabase zone id. If a zone has no
// entry here, the game falls back to random obstacle scatter on the default GameConstants
// GridWidth/GridHeight - nothing needs an authored layout to keep working.
//
// Each entry defines its own size (Tiles is [rows][cols], rows = height, cols = width) - grid
// size is per-zone now, not a fixed constant (see TerrainMap.Width/Height). Heights is an
// optional parallel [rows][cols] array of per-tile elevation (real physical elevation used for
// climb cost / line-of-sight / high-ground accuracy - see TacticalGridSystem); a zone with no
// Heights array is flat ground level (0) everywhere.
//
// Valid Tiles strings: any GameCore.TileType name ("Empty", "Solid", "ScatterCover", "HardCover",
// "Difficult", "Door", "DoorOpen", "Barrel").
//
// All 28 non-safe zones below are generated from compact per-zone recipes (ZoneLayoutRecipes.cs)
// via the small procedural canvas in ZoneLayoutBuilder.cs, rather than hand-typed as literal
// string grids - these maps run into the thousands of cells each, so a recipe of named
// rectangle/blob/line/scatter stamps (mirroring each zone's landmark list in 06_Zone_Map.md) is
// the only reasonable way to author 28 of them. "scavengers_hub" is a Safe Zone and never gets a
// hostile population (see ZoneTravel.EnterZone / SquadManager.EnsureZonePopulated), so it has no
// layout here - it falls back to a small empty procedural map, which is fine for a hub with no
// combat.
using System.Collections.Generic;

namespace GameData
{
    public class MapLayout
    {
        public string[][] Tiles;
        public int[][] Heights; // optional; null = flat (height 0 everywhere)
        public string[][] Looks;   // optional art keys (TerrainTile.Look); null = renderer default
        public string[][] Grounds; // optional GroundType names (null entries = zone theme floor)
        public Dictionary<string, (int x, int y)> Anchors; // landmark name -> tile (see TerrainMap.SpawnLandmarks)
        // Tier maps (one big map per tier, see TierMaps.cs): which region (an old zone id) each cell belongs to,
        // and cells the dresser must leave as authored (roads, yards, village streets). Both null on ordinary maps.
        public string[][] Regions;
        public bool[][] Keep;
        public bool FullSize;      // authored at final scale: no MapUpscaler pass
    }

    public static class MapLayoutDatabase
    {
        public static readonly Dictionary<string, MapLayout> All = GameCore.ZoneLayoutRecipes.BuildAll();
    }
}
