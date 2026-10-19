using System; using System.Linq; using GameCore; using GameData;
static class BenchMain {
  static int Main(string[] args) {
    GameServices.Grid = new TacticalGridSystem(); GameServices.Combat = new CombatResolver();
    var sw = System.Diagnostics.Stopwatch.StartNew();
    int n = MapLayoutDatabase.All.Count;
    Console.WriteLine($"MapLayoutDatabase: {n} maps built in {sw.ElapsedMilliseconds} ms");
    foreach (var id in new[] { "perimeter", "scrapyard", "scavengers_hub", "vanguard_barracks", "quarry_basin", "abandoned_city" }) {
      sw.Restart(); MacroState.Current.CurrentZoneId = id; TerrainMap.BuildTerrainForZone(id); long build = sw.ElapsedMilliseconds;
      // long NPC paths: corner to corner, many times
      var probe = new HumanEnemy("p", 0, 0, "Raiders", "Rookie", null, null) { Mp = 4 };
      var a = TerrainMap.FindNearestEnterable(20, 20); var b = TerrainMap.FindNearestEnterable(180, 180);
      probe.X = a.x; probe.Y = a.y;
      sw.Restart(); int len = 0; for (int i = 0; i < 20; i++) len = Pathfinding.FindPath(a.x, a.y, b.x, b.y, probe).Count; long npc = sw.ElapsedMilliseconds;
      var pl = new Player(); pl.X = a.x; pl.Y = a.y;
      sw.Restart(); int plen = Pathfinding.FindPath(a.x, a.y, b.x, b.y, pl).Count; long ply = sw.ElapsedMilliseconds;
      // a short wander step (what most NPC ticks are)
      var c2 = TerrainMap.FindNearestEnterable(a.x + 15, a.y + 10);
      sw.Restart(); for (int i = 0; i < 200; i++) Pathfinding.FindPath(a.x, a.y, c2.x, c2.y, probe); long shortp = sw.ElapsedMilliseconds;
      Console.WriteLine($"{id}: build {build} ms | NPC long path x20 {npc} ms (len {len}) | player long path {ply} ms (len {plen}) | short paths x200 {shortp} ms");
    }
    return 0;
  }
}
