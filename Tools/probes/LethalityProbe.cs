using System; using System.Linq; using System.Collections.Generic; using GameCore; using GameData;
static class ProbeMain {
  class Quiet : IGameLogTarget { public void AppendLine(string s) { } }
  static bool Out(Mutant e) => !e.IsAlive() || (e is HumanEnemy hh && EnemyAI.IsOut(hh));
  static int Main(string[] args) {
    GameServices.Grid = new TacticalGridSystem(); GameServices.Combat = new CombatResolver(); GameServices.AnomalyTrigger = new AnomalyTriggerService();
    var rules = new[] { ("now", 1f, false), ("lethal x2 + morale", 2f, true), ("lethal x3 + morale", 3f, true) };
    int n = args.Length > 0 ? int.Parse(args[0]) : 300;
    foreach (var (rname, leth, morale) in rules) {
      Console.WriteLine($"--- {rname}");
      foreach (int dist in new[] { 8, 5 })
      foreach (bool ambush in new[] { false, true })
      foreach (int gang in new[] { 1, 2, 3, 4 }) {
        int wins = 0, deaths = 0, rounds = 0, hpLeft = 0;
        for (int f = 0; f < n; f++) {
          GameLog.Target = new Quiet(); CombatRules.Lethality = 1f;
          GameBootstrap.StartNewGame();
          CombatRules.Lethality = leth;
          GameState.Current.Mode = GameMode.Tactical; GameState.Current.CurrentTurn = Turn.Player;
          var p = Player.Current; EncounterState.Enemies.Clear(); EncounterState.InCombat = !ambush; EncounterState.ResetFight();
          for (int y = p.Y - 6; y <= p.Y + 6; y++) for (int x = p.X - 3; x <= p.X + 14; x++) if (x > 0 && y > 0 && x < TerrainMap.Width - 1 && y < TerrainMap.Height - 1) TerrainMap.SetTileType(x, y, TileType.Empty);
          foreach (var (dx, dy) in new[] { (1, 0), (1, -1), (1, 1), (0, -1), (-1, -1) }) TerrainMap.SetTileType(p.X + dx, p.Y + dy, TileType.HardCover);
          // the gang has cover too: a sandbag in front of each
          string[] ranks = { "Rookie", "Experienced", "Rookie", "Rookie" };
          var g = new List<HumanEnemy>();
          for (int i = 0; i < gang; i++) {
            var (w, a) = EncounterGeneration.PickFactionLoadout("Raiders", ranks[i], 1);
            var h = new HumanEnemy("Raider" + i, p.X + dist + i % 2, p.Y - 2 + i * 2, "Raiders", ranks[i], w, a) { IsAggroed = !ambush };
            TerrainMap.SetTileType(h.X - 1, h.Y, TileType.HardCover);
            g.Add(h); EncounterState.Enemies.Add(h);
          }
          var boss = g.OrderByDescending(h => CombatRules.RankIndex(h.Rank)).First();
          p.StartTurn(); p.HealPermanent(100);
          int deadSeen = 0; bool bossSeen = false;
          void Morale() {
            if (!morale) return;
            int dead = g.Count(h => !h.IsAlive());
            bool bossDown = !boss.IsAlive() || EnemyAI.IsOut(boss);
            if (dead > deadSeen || (bossDown && !bossSeen)) {
              foreach (var h in g.Where(h => !Out(h)).ToList())
                if ((bossDown && !bossSeen) || UnityEngine.Random.value < 0.5f) EnemyAI.Break(h, p);
              deadSeen = dead; bossSeen = bossDown;
            }
          }
          int r = 0;
          for (; r < 40 && p.IsAlive() && g.Any(e => !Out(e)); r++) {
            int shots = 0; bool ended = false;
            for (int k = 0; k < 5 && GameState.Current.CurrentTurn == Turn.Player && p.Ap > 0; k++) {
              var w = p.EquippedWeapon;
              if (w.IsJammed) { p.ClearWeaponJam(); continue; }
              if (!w.IsLoaded) { p.ReloadWeapon(); continue; }
              var t = g.Where(e => !Out(e) && CombatantUtil.Distance(p, e) <= w.RangeTiles).OrderBy(e => e.Hp).FirstOrDefault();
              int alive = g.Count(e => !Out(e));
              if (shots >= 1 && alive >= 2 && p.Hp < 50 && p.CanHunker(out _)) { p.HunkerDown(); ended = true; break; }
              if (t == null || p.Ap < 2) break;
              if (p.Ap >= 3 && p.Ap < 4 && p.StatusEffects.AimBonus == 0) p.Aim();
              p.Attack(t, null); shots++; Morale();
            }
            TurnLoop.CheckCombatEnd();
            if (!ended && GameState.Current.CurrentTurn == Turn.Player) TurnLoop.PlayerEndTurn();
            Morale();
          }
          rounds += r;
          if (!p.IsAlive()) deaths++; else if (!g.Any(e => !Out(e))) { wins++; hpLeft += p.Hp; }
        }
        Console.WriteLine($"  {dist} tiles {(ambush ? "ambush" : "open  ")} vs {gang}: win {wins * 100 / n,3}%  die {deaths * 100 / n,3}%  hp left {(wins > 0 ? hpLeft / wins : 0),3}  rounds {rounds / (double)n:F1}");
      }
    }
    return 0;
  }
}
