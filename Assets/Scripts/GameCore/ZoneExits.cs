// New: physical, walkable zone transitions - replaces the old OverworldMapView button menu.
// A zone's exits are computed from its MacroMapDatabase connections (+ any discovered hidden
// ones) and spread evenly around the authored map's border; stepping onto one crosses into that
// zone (see PlayerController.WalkToward). Since MacroMapDatabase doesn't encode which physical
// edge a connection should sit on, placement here is invented/deterministic (evenly spaced by
// list order) rather than hand-authored per zone - ponytail: good enough for "walk to the edge
// to leave", not a claim that zone B's entrance geographically lines up with zone A's exit.
using System.Collections.Generic;
using System.Linq;
using GameData;

namespace GameCore
{
    public class ZoneExit
    {
        public string TargetZoneId;
        public int X, Y;
    }

    public static class ZoneExitPlacer
    {
        public static List<ZoneExit> ComputeAndCarveExits(ZoneDef zone)
        {
            // the map overhaul: a tier map's exits are every one of its regions' ways to another map (a neighbouring
            // tier, a dungeon); a region next door on the same map is just more ground to walk on
            string map = MacroMapDatabase.MapOf(zone.Id);
            var targets = new List<string>();
            foreach (var region in MacroMapDatabase.RegionsOf(map))
            {
                foreach (var t in region.ConnectedZones ?? new List<string>())
                    if (MacroMapDatabase.MapOf(t) != map && !targets.Contains(t)) targets.Add(t);
                if (region.HiddenConnections != null)
                    foreach (var hidden in region.HiddenConnections)
                        if (MacroState.Current.DiscoveredHiddenNodes.Contains(hidden) && MacroMapDatabase.MapOf(hidden) != map && !targets.Contains(hidden))
                            targets.Add(hidden);
            }

            var exits = new List<ZoneExit>();
            if (targets.Count == 0) return exits;

            // Fix: this used to place exits by list index/count (perimeter * i / targets.Count).
            // SearchArea() discovering a hidden connection appends to the target list and calls
            // TerrainMap.RecomputeExits(), which rebuilds this list from scratch with a new
            // Count - shifting every *already-carved* exit's computed position without ever
            // un-carving the old (now-orphaned, still-walkable) gate tile. The live exit silently
            // teleports to a new border spot while the old opening becomes dead, misleading terrain.
            // Hashing the (zone, target) pair instead means each exit's position only ever depends
            // on the two zone ids involved, not on how many other exits exist or in what order -
            // adding a new one never moves anyone else's.
            foreach (var targetId in targets)
            {
                // New: recipes can pin an exit where the design's map-sketch puts it ("exit:<zone>"
                // anchor - a compass edge, or a landmark inside the map for hidden routes like the
                // Rust Culvert). Otherwise the old hashed spot on the border.
                var (x, y) = GatePos(zone, targetId);
                CarveGate(x, y);
                exits.Add(new ZoneExit { TargetZoneId = targetId, X = x, Y = y });
            }
            return exits;
        }

        /// <summary>Where the gate to targetId sits on this zone's loaded map (Batch 50: also where an undiscovered hidden
        /// way physically is, before it's carved).</summary>
        public static (int x, int y) GatePos(ZoneDef zone, string targetId)
        {
            MapLayoutDatabase.All.TryGetValue(MacroMapDatabase.MapOf(zone.Id), out var layout);
            if (layout?.Anchors != null && layout.Anchors.TryGetValue("exit:" + targetId, out var pinned)) return pinned;
            int perimeter = 2 * (TerrainMap.Width + TerrainMap.Height);
            return PerimeterPointAt((int)((uint)StableHash($"{MacroMapDatabase.MapOf(zone.Id)}->{targetId}") % (uint)perimeter));
        }

        /// <summary>Deterministic string hash (FNV-1a) - string.GetHashCode() is randomized per
        /// process in modern .NET/Mono, which would silently reshuffle every exit's position on
        /// every game restart if used here instead.</summary>
        private static int StableHash(string s)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in s) { hash ^= c; hash *= 16777619; }
                return (int)hash;
            }
        }

        private static (int x, int y) PerimeterPointAt(int offset)
        {
            int w = TerrainMap.Width, h = TerrainMap.Height;
            if (offset < w) return (offset, 0);                          // top edge
            offset -= w;
            if (offset < h) return (w - 1, offset);                      // right edge
            offset -= h;
            if (offset < w) return (w - 1 - offset, h - 1);              // bottom edge
            offset -= w;
            return (0, h - 1 - offset);                                  // left edge
        }

        /// <summary>Punches a walkable gate through the border wall an authored recipe may have
        /// placed there - exits are computed after terrain generation, not part of the recipe, so
        /// this always wins regardless of what the zone's border looked like.</summary>
        private static void CarveGate(int x, int y)
        {
            // Batch 120: a way out pinned on open ground inside the map (a stair, a hatch) needs no gate - a 3x3 one broke
            // the walls round the stairwells in the floor plans
            if (x > 2 && y > 2 && x < TerrainMap.Width - 3 && y < TerrainMap.Height - 3 && TerrainMap.CanEnterTile(x, y)) return;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    Open(x + dx, y + dy);

            // A thick border (tree line, embankment) can be deeper than the 3x3 gate - keep
            // digging toward the map's centre until the gate meets ground you can walk on.
            int cx = TerrainMap.Width / 2, cy = TerrainMap.Height / 2;
            int px = x, py = y;
            var dug = new List<(int x, int y)> { (x, y) };
            for (int step = 0; step < 40; step++)   // Batch 30: borders are 3x thicker on the bigger maps
            {
                px += System.Math.Sign(cx - px) * (System.Math.Abs(cx - px) > System.Math.Abs(cy - py) ? 1 : 0);
                py += System.Math.Sign(cy - py) * (System.Math.Abs(cy - py) >= System.Math.Abs(cx - px) ? 1 : 0);
                bool wasOpen = TerrainMap.CanEnterTile(px, py);
                Open(px, py);
                // 3 tiles wide, so a gate reads as a gap in the tree line / wall, not a crack
                bool alongX = System.Math.Abs(cx - px) > System.Math.Abs(cy - py);
                Open(px + (alongX ? 0 : 1), py + (alongX ? 1 : 0)); Open(px - (alongX ? 0 : 1), py - (alongX ? 1 : 0));
                dug.Add((px, py));
                if (wasOpen && step > 0) break;
            }
            // a gate dug through a high embankment must also step down to the ground it meets,
            // or you could drop in and never climb back out (each step at most 2 higher)
            for (int i = dug.Count - 2; i >= 0; i--)
            {
                var t = TerrainMap.GetTile(dug[i].x, dug[i].y);
                t.Height = System.Math.Min(t.Height, TerrainMap.GetHeight(dug[i + 1].x, dug[i + 1].y) + 2);
            }
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int gx = x + dx, gy = y + dy;
                    if (gx < 0 || gx >= TerrainMap.Width || gy < 0 || gy >= TerrainMap.Height) continue;
                    var t = TerrainMap.GetTile(gx, gy);
                    t.Height = System.Math.Min(t.Height, TerrainMap.GetHeight(x, y) + 2);
                }
        }

        private static void Open(int x, int y)
        {
            if (x < 0 || x >= TerrainMap.Width || y < 0 || y >= TerrainMap.Height) return;
            var t = TerrainMap.GetTile(x, y);
            if (TerrainMap.CanEnterTile(x, y) && t.Type != TileType.Door) return;
            t.Type = TileType.Empty;
            t.Look = null;
            t.Damage = 0;
            TerrainMap.DirtyTiles.Add((x, y)); // a gate found mid-visit (Search Area) redraws
        }
    }
}
