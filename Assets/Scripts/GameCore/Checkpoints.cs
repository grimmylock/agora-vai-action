// Batch 126 (Yan: "some map exits can be checkpoints that squads hold too but not every one of them, like how the cordon map in stalker
// has the military checkpoint (path to garbage) all the way north and is usually guarded by bandits, but the paths to the swamps are
// not guarded"). On each tier map one road up a tier is a checkpoint: a landmark by its gate with sandbags, held by a squad of the
// local faction (Raiders on the roads out of Tiers 1 and 2) that stays put (Job "hold", Posts keep it round its post). The other
// ways out - sideways, back down, the second road up - stay open. A checkpoint that's been wiped out gets a new garrison after two
// days, put down only while you're on another map (nothing appears in front of you).
using System.Collections.Generic;
using System.Linq;
using GameData;

namespace GameCore
{
    public static class Checkpoints
    {
        public const int RegarrisonDays = 2;
        static readonly Dictionary<string, int> EmptySince = new Dictionary<string, int>();   // checkpoint -> the day it was found empty

        /// <summary>The guarded road on a tier map: (region, the zone it leads to), or null. The first exit up a tier, Tier 7 aside.</summary>
        public static (string region, string target)? Road(string map)
        {
            var here = MacroMapDatabase.Find(map);
            if (here == null || here.IsDungeon || here.Tier >= 6) return null;
            foreach (var r in MacroMapDatabase.RegionsOf(map))
                foreach (var t in r.ConnectedZones ?? new List<string>())
                {
                    var z = MacroMapDatabase.Find(t);
                    if (z != null && !z.IsDungeon && !MacroMapDatabase.SameMap(t, map) && z.Tier > here.Tier) return (r.Id, t);
                }
            return null;
        }

        public static string Name(string target) => $"{MacroMapDatabase.Find(target)?.Name ?? target} Checkpoint";

        /// <summary>Who holds it: the region's first faction that isn't mutants or loners, Raiders if they're allowed there.</summary>
        static IEnumerable<string> Holders(string region)
        {
            var f = MacroMapDatabase.Find(region)?.AllowedFactions ?? new List<string>();
            return f.Where(x => x != "Mutant" && x != "Zombie" && x != "Wanderers").OrderBy(x => x == "Raiders" ? 0 : 1);
        }

        /// <summary>Where the checkpoint stands on the loaded map: a little way in from the gate.</summary>
        public static (int x, int y)? Spot(string target)
        {
            var map = MacroMapDatabase.Find(TerrainMap.ZoneId);
            if (map == null) return null;
            var (gx, gy) = ZoneExitPlacer.GatePos(map, target);   // where the gate is (the exits themselves are carved after the map is built)
            int cx = TerrainMap.Width / 2, cy = TerrainMap.Height / 2;
            int dx = System.Math.Sign(cx - gx), dy = System.Math.Sign(cy - gy);
            if (System.Math.Abs(cx - gx) > System.Math.Abs(cy - gy)) dy = 0; else dx = 0;   // straight in from the edge
            return TerrainMap.FindNearestEnterable(gx + dx * 7, gy + dy * 7);
        }

        /// <summary>A map was just built (TerrainMap.BuildTerrainForZone): the checkpoint's landmark and a few sandbags by the road.</summary>
        public static void Dress(string map)
        {
            var road = Road(map);
            if (road == null || TerrainMap.Landmarks.Any(l => l.Name == Name(road.Value.target))) return;
            var s = Spot(road.Value.target);
            if (s == null) return;
            TerrainMap.Landmarks.Add(new LandmarkInstance { X = s.Value.x, Y = s.Value.y, Name = Name(road.Value.target), Description = "Sandbags across the road, and somebody behind them." });
            var (_, gy) = ZoneExitPlacer.GatePos(MacroMapDatabase.Find(map), road.Value.target);
            bool across = gy <= 2 || gy >= TerrainMap.Height - 3;   // the road runs north-south: the bags run east-west
            foreach (int side in new[] { -1, 1 })
                for (int k = 1; k <= 4; k++)   // the road's edges too: the middle lane stays open (a choke point)
                {
                    int x = s.Value.x + (across ? side * k : 0), y = s.Value.y + (across ? 0 : side * k);
                    if (x < 1 || y < 1 || x >= TerrainMap.Width - 1 || y >= TerrainMap.Height - 1) continue;
                    var t = TerrainMap.GetTile(x, y);
                    bool ground = t.Type == TileType.Empty || t.Type == TileType.Difficult && t.Ground != GroundType.Water;   // mud and reeds yes, water no
                    if (!ground || t.Look != null && (t.Look.StartsWith("stash_") || t.Look == "campfire" || t.Look == "papers")) continue;   // over a puddle or litter is fine
                    t.Type = TileType.HardCover; t.Look = across ? "sandbags" : "sandbags_v";
                }
        }

        /// <summary>Every checkpoint has its garrison (SquadManager.SeedWorld at New Game, and each game hour afterwards).</summary>
        public static void Garrison(bool seeding = false)
        {
            var M = MacroState.Current;
            foreach (var map in new[] { "perimeter", "scrapyard", "scavengers_hub", "vanguard_barracks", "quarry_basin", "abandoned_city" })
            {
                var road = Road(map);
                if (road == null) continue;
                string target = road.Value.target;
                if (M.OfflineSquads.Any(q => q.Checkpoint == target && q.Members.Any(m => m.IsAlive()))) { EmptySince.Remove(target); continue; }
                if (!seeding)
                {
                    if (!EmptySince.TryGetValue(target, out var since)) { EmptySince[target] = M.Day; continue; }
                    if (M.Day - since < RegarrisonDays || MacroMapDatabase.SameMap(M.CurrentZoneId, map)) continue;   // not while you're on that map
                }
                int tier = MacroMapDatabase.Find(road.Value.region)?.Tier ?? 1;
                OfflineSquad sq = null;
                foreach (var faction in Holders(road.Value.region))   // the first that can field a squad here
                    if ((sq = SquadManager.CreateOfflineSquad(road.Value.region, faction, SquadManager.HumanSquadSize(faction, tier))) != null) break;
                if (sq == null) continue;
                sq.Job = "hold"; sq.Checkpoint = target; EmptySince.Remove(target);
            }
        }

        public static void Reset() => EmptySince.Clear();
    }
}
