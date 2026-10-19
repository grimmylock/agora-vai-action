// Per-zone look: which floor a zone is made of and what its walls / cover / hard cover look like
// when a zone recipe doesn't name a specific look (TerrainTile.Look). Purely cosmetic - the rules
// only ever read TileType. Look names are cells in Resources/Sprites/terrain_atlas (see
// ArtSource/make_terrain_tiles.py); "tree" picks any "tree#n" variant.
using System.Collections.Generic;

namespace GameCore
{
    public class ZoneTheme
    {
        public GroundType Base, Alt;
        public float AltAmount;       // 0..1 share of the map covered by Alt patches
        public string Mass;       // big solid regions (an edge-outlined texture: wall, cliff, junk...)
        public string[] Props;    // small, isolated solids (trees, wrecks, containers...)
        public string Border;     // solids along the map edge
        public string[] Cover, Hard;
    }

    public static class ZoneThemes
    {
        private static ZoneTheme T(GroundType b, GroundType a, float amt, string mass, string[] props, string border, string[] cover, string[] hard) =>
            new ZoneTheme { Base = b, Alt = a, AltAmount = amt, Mass = mass, Props = props, Border = border, Cover = cover, Hard = hard };

        private static readonly ZoneTheme Field = T(GroundType.Grass, GroundType.Dirt, .2f, "wall", new[] { "tree", "tree", "rock", "wreck" }, "foliage", new[] { "bush", "bush", "debris", "tires" }, new[] { "jersey" });
        private static readonly ZoneTheme Marsh = T(GroundType.Mud, GroundType.Water, .35f, "foliage", new[] { "deadtree", "deadtree", "wreck" }, "foliage", new[] { "reeds", "reeds", "bush" }, new[] { "sandbags" });
        private static readonly ZoneTheme Cutting = T(GroundType.Gravel, GroundType.Dirt, .3f, "cliff", new[] { "rock", "container", "wreck" }, "cliff", new[] { "crate", "debris", "tires" }, new[] { "block" });
        private static readonly ZoneTheme Scrap = T(GroundType.Dirt, GroundType.Gravel, .35f, "junk", new[] { "wreck", "container", "scrap" }, "junk", new[] { "tires", "debris", "rustbarrel" }, new[] { "engine" });
        private static readonly ZoneTheme Industrial = T(GroundType.Concrete, GroundType.Gravel, .25f, "concrete", new[] { "container", "machine", "pipes" }, "concrete", new[] { "crate", "debris", "rustbarrel" }, new[] { "block" });
        private static readonly ZoneTheme Valley = T(GroundType.DarkGrass, GroundType.Dirt, .25f, "cliff", new[] { "rock", "tree", "tree" }, "foliage", new[] { "bush", "debris" }, new[] { "sandbags" });
        private static readonly ZoneTheme Hub = T(GroundType.Dirt, GroundType.Gravel, .3f, "plaster", new[] { "container", "wreck" }, "concrete", new[] { "crate", "tires" }, new[] { "sandbags" });
        private static readonly ZoneTheme Lake = T(GroundType.Sand, GroundType.Gravel, .25f, "cliff", new[] { "rock", "rock", "wreck" }, "cliff", new[] { "debris", "brush" }, new[] { "block" });
        private static readonly ZoneTheme Military = T(GroundType.Dirt, GroundType.Gravel, .3f, "concrete", new[] { "container", "wreck" }, "concrete", new[] { "crate", "tires" }, new[] { "sandbags" });
        private static readonly ZoneTheme Urban = T(GroundType.Asphalt, GroundType.Dirt, .22f, "plaster", new[] { "wreck", "wreck", "pillar" }, "plaster", new[] { "debris", "crate" }, new[] { "block" });
        private static readonly ZoneTheme Crimson = T(GroundType.DarkGrass, GroundType.Dirt, .25f, "redfoliage", new[] { "redtree", "redtree", "deadtree" }, "redfoliage", new[] { "bush", "logs" }, new[] { "sandbags" });
        private static readonly ZoneTheme Quarry = T(GroundType.Gravel, GroundType.Sand, .3f, "cliff", new[] { "rock", "machine" }, "cliff", new[] { "debris" }, new[] { "block" });
        private static readonly ZoneTheme Ships = T(GroundType.Sand, GroundType.Water, .25f, "hull", new[] { "scrap", "container" }, "junk", new[] { "debris", "rustbarrel" }, new[] { "engine" });
        private static readonly ZoneTheme Antenna = T(GroundType.Gravel, GroundType.Dirt, .3f, "concrete", new[] { "machine", "pipes", "container" }, "concrete", new[] { "crate", "debris" }, new[] { "block" });
        private static readonly ZoneTheme Epicenter = T(GroundType.Dirt, GroundType.Mud, .4f, "cliff", new[] { "deadtree", "rock" }, "foliage", new[] { "brush", "debris" }, new[] { "block" });
        private static readonly ZoneTheme Hospital = T(GroundType.Asphalt, GroundType.Dirt, .25f, "plaster", new[] { "pillar", "wreck" }, "plaster", new[] { "debris", "crate" }, new[] { "block" });
        private static readonly ZoneTheme Reactor = T(GroundType.Concrete, GroundType.Gravel, .25f, "concrete", new[] { "machine", "pipes" }, "concrete", new[] { "debris" }, new[] { "block" });
        private static readonly ZoneTheme Cave = T(GroundType.CaveFloor, GroundType.Mud, .25f, "cliff", new[] { "rock", "rock" }, "cliff", new[] { "debris", "bones" }, new[] { "rock" });
        private static readonly ZoneTheme Lab = T(GroundType.LabTile, GroundType.Concrete, .2f, "concrete", new[] { "machine", "pillar" }, "concrete", new[] { "crate", "lockers" }, new[] { "block" });
        private static readonly ZoneTheme Tunnel = T(GroundType.Concrete, GroundType.CaveFloor, .3f, "concrete", new[] { "pipes", "pillar" }, "concrete", new[] { "crate", "debris" }, new[] { "block" });

        private static readonly Dictionary<string, ZoneTheme> ByZone = new Dictionary<string, ZoneTheme>
        {
            ["perimeter"] = Field, ["marshes"] = Marsh, ["smugglers_run"] = Cutting,
            ["scrapyard"] = Scrap, ["industrial_basin"] = Industrial, ["black_valley"] = Valley,
            ["scavengers_hub"] = Hub, ["rail_yards"] = Cutting, ["dried_lake"] = Lake,
            ["vanguard_barracks"] = Military, ["ghost_town"] = Urban, ["crimson_woods"] = Crimson,
            ["quarry_basin"] = Quarry, ["ship_graveyard"] = Ships, ["antenna_grid"] = Antenna,
            ["abandoned_city"] = Urban, ["outskirts"] = Epicenter, ["hospital"] = Hospital,
            ["reactor_core"] = Reactor,
            ["secret_cave"] = Cave, ["deep_caverns"] = Cave, ["siege_catacombs"] = Cave,
            ["bonfire_village"] = Field, ["chapel_crypt"] = Cave, ["smugglers_tunnel"] = Cave, ["pumping_basement"] = Tunnel, ["flooded_bunker"] = Tunnel,
            ["crusher_pit"] = Tunnel, ["valley_mine"] = Cave, ["stranded_ferry"] = Tunnel, ["camp_shelter"] = Tunnel, ["drydock_tunnels"] = Tunnel, ["hospital_morgue"] = Lab,
            ["secret_lab_alpha"] = Lab, ["secret_lab_beta"] = Lab, ["secret_lab_gamma"] = Lab, ["lattice_foundry"] = Lab, ["lattice_foundry_lower"] = Lab,
            ["sub_station_depths"] = Tunnel, ["maintenance_tunnels"] = Tunnel,
            ["underground_labyrinth"] = Tunnel, ["underground_bunker"] = Tunnel,
        };

        public static ZoneTheme For(string zoneId) =>
            zoneId != null && ByZone.TryGetValue(zoneId, out var t) ? t : Field;

        /// <summary>Default floor for a tile: the theme's base ground with smooth patches of its
        /// alt ground (value noise, ~6-tile blobs), stable per zone.</summary>
        public static GroundType GroundAt(string zoneId, int x, int y)
        {
            var t = For(zoneId);
            int seed = zoneId == null ? 0 : TerrainCanvas.Seed(zoneId);
            const float cell = 15f;   // Batch 30: maps are 3x bigger, patches grew with them
            float fx = x / cell, fy = y / cell;
            int ix = (int)System.Math.Floor(fx), iy = (int)System.Math.Floor(fy);
            float tx = Smooth(fx - ix), ty = Smooth(fy - iy);
            float a = Hash01(ix, iy, seed), b = Hash01(ix + 1, iy, seed), c = Hash01(ix, iy + 1, seed), d = Hash01(ix + 1, iy + 1, seed);
            float n = Lerp(Lerp(a, b, tx), Lerp(c, d, tx), ty);
            return n < t.AltAmount ? t.Alt : t.Base;
        }

        /// <summary>Deterministic 0..1 hash - also used by the renderer to pick sprite variants.</summary>
        public static float Hash01(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2246822519);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }

        private static float Smooth(float t) => t * t * (3 - 2 * t);
        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
