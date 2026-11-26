// Door variety by place (2026-11-20; Yan: "everything has the same wooden door ... we need variety", "assign by place, wire them in"). After a map is built (ZoneLayoutRecipes), every plain door in a horizontal wall
// gets a look by WHERE it is: the nearest landmark's kind decides (a church gets arched oak and a pair of gothic double doors, a police station or barracks barred cell doors and iron gates, shops and offices glass,
// garages and depots roller shutters and barn doors, labs and bunkers lab doors and sliding steel pairs, farms plank doors); with no landmark close, the zone's own kind decides (labs, crushers, crypts). Doors that
// already have a look (blast, badge, power, metal, the cathedral's own) are left alone. Looks: dx_<single> or dx_<double>_l / _r; the art is walls.py (ArtSource/world/doors_new.py), built for the two-storey walls
// (concrete, plaster, brick, gothic) on horizontal wall pieces; anywhere else WorldArt falls back to the plain door.
using System.Collections.Generic;
using System.Linq;
using GameData;

namespace GameCore
{
    public static class DoorStyles
    {
        // keywords (lower case, found in a landmark's name), single style, double style; the first group with a match wins, so the specific kinds come first
        static readonly (string[] keys, string single, string dbl)[] Places =
        {
            (new[] { "cathedral", "church", "chapel", "crypt", "bell tower", "shrine", "altar", "sacristy" }, "arched", "gothic"),
            (new[] { "police", "prison", "cell block", "checkpoint", "gatehouse", "barracks", "armory", "command post", "garrison barricade", "firebase", "outpost" }, "cell", "gate"),
            (new[] { "hospital", "ward", "morgue", "bunker", "control", "relay", "generator", "transmitter", "sub-station", "substation", "breaker", "foundry", "array", "laborator", "pump", "water works", "central plant", "vault" }, "lab", "steel"),
            (new[] { "garage", "depot", "motor pool", "tram", "loading", "workshop", "repair", "cannery", "silo", "crusher", "hangar", "warehouse", "slipway", "container", "dock", "mine works", "smokestack", "truck stop", "roundhouse", "freight" }, "roller", "barn"),
            (new[] { "pharmacy", "department", "store", "cinema", "laundromat", "kiosk", "bazaar", "anchor", "office", "town hall", "hotel", "school", "petrol", "flats", "highrise", "bunkhouse" }, "glass", "glassd"),
            (new[] { "barn", "farm", "shed", "stable", "cabin", "hut", "shack", "camp", "hide", "blind", "fishing", "logging", "headman", "house" }, "plank", "barn"),
        };
        // the zone's own kind when no landmark is near
        static readonly Dictionary<string, (string single, string dbl)> Zones = new Dictionary<string, (string, string)>
        {
            ["secret_lab_alpha"] = ("lab", "steel"), ["secret_lab_beta"] = ("lab", "steel"), ["secret_lab_gamma"] = ("lab", "steel"), ["lattice_foundry"] = ("lab", "steel"), ["lattice_foundry_lower"] = ("lab", "steel"),
            ["loading_annex"] = ("lab", "steel"), ["underground_bunker"] = ("lab", "steel"), ["hospital_morgue"] = ("lab", "steel"), ["flooded_bunker"] = ("lab", "steel"), ["underground_labyrinth"] = ("lab", "steel"),
            ["crusher_pit"] = ("barred", "gate"), ["taken_tunnels"] = ("barred", "gate"), ["sub_station_depths"] = ("barred", "gate"), ["sub_station_depths_1"] = ("barred", "gate"), ["sub_station_depths_2"] = ("barred", "gate"),
            ["sub_station_depths_3"] = ("barred", "gate"), ["maintenance_tunnels"] = ("barred", "steel"), ["smugglers_tunnel"] = ("barred", "gate"), ["pumping_basement"] = ("lab", "steel"),
            ["chapel_crypt"] = ("arched", "gothic"), ["siege_catacombs"] = ("arched", "gothic"), ["abandoned_cathedral"] = ("arched", "gothic"), ["camp_shelter"] = ("cell", "gate"),
        };
        public const int Reach = 14;

        static readonly HashSet<string> Built = new HashSet<string> { "wall", "plaster", "stucco", "concrete", "gothic", "hull", "metal", "plank", "driftwood", "window", "cathedral_mass" };
        /// <summary>A piece of a built wall: a Solid tile in a building material (not a fence or a rock) or another door.</summary>
        static bool Wall(MapLayout m, int x, int y)
        {
            if (x < 0 || y < 0 || y >= m.Tiles.Length || x >= m.Tiles[y].Length) return false; var t = m.Tiles[y][x];
            if (t == "Door" || t == "DoorOpen") return true;
            return t == "Solid" && m.Looks != null && m.Looks[y][x] != null && Built.Contains(m.Looks[y][x]);
        }
        static bool Solid(MapLayout m, int x, int y) => x >= 0 && y >= 0 && y < m.Tiles.Length && x < m.Tiles[y].Length && m.Tiles[y][x] == "Solid";
        static bool Plain(MapLayout m, int x, int y) => y >= 0 && y < m.Tiles.Length && x >= 0 && x < m.Tiles[y].Length && m.Tiles[y][x] == "Door" && (m.Looks == null || m.Looks[y][x] == null);

        /// <summary>The (single, double) style for a place: the nearest landmark with a known kind within Reach, else the zone's default, else none.</summary>
        public static (string single, string dbl)? StyleAt(MapLayout m, string zone, int x, int y)
        {
            if (m.Anchors != null)
            {
                int best = int.MaxValue; (string, string)? pick = null;
                foreach (var kv in m.Anchors)
                {
                    string n = kv.Key.ToLowerInvariant(); if (n.StartsWith("exit:") || n.StartsWith("arrive:") || n.StartsWith("stash:")) continue;
                    int d = System.Math.Max(System.Math.Abs(kv.Value.x - x), System.Math.Abs(kv.Value.y - y)); if (d > Reach || d >= best) continue;
                    foreach (var p in Places) if (p.keys.Any(k => n.Contains(k))) { best = d; pick = (p.single, p.dbl); break; }
                }
                if (pick != null) return pick;
            }
            return Zones.TryGetValue(zone ?? "", out var z) ? ((string, string)?)(z.single, z.dbl) : null;
        }

        /// <summary>Assign the looks (idempotent: only plain doors in a horizontal wall are touched). Returns the map for chaining.</summary>
        public static MapLayout Apply(MapLayout m, string zone)
        {
            if (m?.Tiles == null) return m; if (m.Looks == null) return m;
            for (int y = 0; y < m.Tiles.Length; y++)
                for (int x = 0; x < m.Tiles[y].Length; x++)
                {
                    if (!Plain(m, x, y)) continue;
                    if (!(Wall(m, x - 1, y) && Wall(m, x + 1, y)) || (Solid(m, x, y - 1) && Solid(m, x, y + 1))) continue;          // a door in an east-west wall only (one side open: an uncarved block like the cathedral has solid behind it)
                    var st = StyleAt(m, zone, x, y); if (st == null) continue;
                    bool pairR = Plain(m, x + 1, y) && !Plain(m, x - 1, y) && !Plain(m, x + 2, y) && Wall(m, x + 2, y);          // a pair: this tile is the left leaf
                    if (pairR) { m.Looks[y][x] = "dx_" + st.Value.dbl + "_l"; m.Looks[y][x + 1] = "dx_" + st.Value.dbl + "_r"; }
                    else if (!Plain(m, x - 1, y) && !Plain(m, x + 1, y)) m.Looks[y][x] = "dx_" + st.Value.single;
                }
            return m;
        }
    }
}
