// Batch 111: how long does it take to walk round a tier map? (CLAUDE.md: "Target: 1 to 3 hours to fully explore Tier 1 ...
// Not measured yet".) For each tier map: start at the first landmark, then always walk to the nearest unvisited landmark (real A*
// routes, as a click would walk them, diagonals included) until all are seen. Real time = route length / the exploring walk
// speed (UnitMotionView.ExploreSpeed tiles a second; a diagonal step is 1.41 tiles of walking). Walking only: no fights,
// looting, reading or dungeons - those come on top.
//   build like BuildsProbe (swap this file in for test/Program.cs), run with no arguments.
using System; using System.Linq; using System.Collections.Generic; using GameCore; using GameData;
static class ProbeMain {
  class Quiet : IGameLogTarget { public void AppendLine(string s) { } }
  static void Main() {
    GameLog.Target = new Quiet();
    GameServices.Grid = new TacticalGridSystem(); GameServices.Combat = new CombatResolver(); GameServices.AnomalyTrigger = new AnomalyTriggerService();
    GameBootstrap.StartNewGame();
    var p = Player.Current;
    foreach (var map in new[] { "perimeter", "scrapyard", "scavengers_hub", "vanguard_barracks", "quarry_basin", "abandoned_city" }) {
      MacroState.Current.CurrentZoneId = map; TerrainMap.BuildTerrainForZone(map);
      EncounterState.Enemies.Clear(); EncounterState.Anomalies.Clear();
      var spots = TerrainMap.Landmarks.Where(l => TerrainMap.CanEnterTile(l.X, l.Y) || true).ToList();
      if (spots.Count == 0) continue;
      var at = TerrainMap.FindNearestEnterable(spots[0].X, spots[0].Y); p.X = at.x; p.Y = at.y;
      var left = spots.Skip(1).ToList(); double walk = 0; int unreachable = 0;
      while (left.Count > 0) {
        LandmarkInstance best = null; List<(int x, int y)> bestPath = null; double bestLen = double.MaxValue;
        foreach (var l in left.OrderBy(l => Math.Max(Math.Abs(l.X - p.X), Math.Abs(l.Y - p.Y))).Take(4)) {   // the 4 nearest as the crow flies
          var t = TerrainMap.FindNearestEnterable(l.X, l.Y);
          var path = Pathfinding.FindPath(p.X, p.Y, t.x, t.y, p);
          if (path.Count == 0) continue;
          double len = 0; for (int i = 1; i < path.Count; i++) len += path[i].x != path[i - 1].x && path[i].y != path[i - 1].y ? 1.4142 : 1;
          if (len < bestLen) { bestLen = len; best = l; bestPath = path; }
        }
        if (best == null) { unreachable += left.Count(l => true) > 0 ? 1 : 0; left.RemoveAt(0); continue; }
        walk += bestLen; left.Remove(best); var end = bestPath[bestPath.Count - 1]; p.X = end.x; p.Y = end.y;
      }
      double secs = walk / UnitMotionView.ExploreSpeed;
      Console.WriteLine($"{map,-18} {spots.Count,3} spots  route {walk,6:0} tiles  walking {secs / 60,5:0.0} min real time  (unreachable {unreachable})");
    }
  }
}
