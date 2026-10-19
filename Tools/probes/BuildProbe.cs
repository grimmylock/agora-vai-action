// Batch 75: a gunslinger pistol build at Tiers 1-5 against the real squads of each tier (SquadManager.CreateOfflineSquad),
// under four rule sets: as now, armor tiers 1-3 at half Phys-DR, the player at 8 AP, and both.
using System; using System.Linq; using System.Collections.Generic; using GameCore; using GameData;
static class ProbeMain {
  class Quiet : IGameLogTarget { public void AppendLine(string s) { } }
  static bool Out(Mutant e) => !e.IsAlive() || (e is HumanEnemy hh && EnemyAI.IsOut(hh));
  static readonly string[] Rank = { "", "Rookie", "Experienced", "Professional", "Veteran", "Expert" };
  // (main, offhand or null, perks)
  static readonly (string main, string off, string[] perks, string medkit)[] Build = {
    default,
    ("HaldenServiceRevolver", null, new string[0], "basicMedkit"),
    ("WaldenP9", null, new string[0], "basicMedkit"),
    ("BlackHarrier", "BlackHarrier", new[] { "Gunslinger" }, "militaryMedkit"),
    ("SteppeEagle", "SteppeEagle", new[] { "Gunslinger" }, "militaryMedkit"),
    ("RK12Kuvalda", "RK12Kuvalda", new[] { "Gunslinger", "QuickHands" }, "scientificMedkit"),
  };
  static readonly (string zone, string faction)[][] Foes = {
    null,
    new[] { ("perimeter", "Raiders") },
    new[] { ("scrapyard", "Raiders"), ("industrial_basin", "Garrison") },
    new[] { ("rail_yards", "Contractors"), ("rail_yards", "Sentinels") },
    new[] { ("ghost_town", "Contractors"), ("ghost_town", "Raiders") },
    new[] { ("quarry_basin", "Contractors"), ("quarry_basin", "Obelisk") },
  };
  static string MidArmor(int t) {
    var list = ArmorDatabase.Tiers["tier" + t].Where(kv => !FactionArmorDatabase.IsFactionIssue(kv.Key)).OrderBy(kv => kv.Value.PhysDR).ToList();
    return list[list.Count / 2].Key;
  }
  static int Main(string[] args) {
    GameServices.Grid = new TacticalGridSystem(); GameServices.Combat = new CombatResolver(); GameServices.AnomalyTrigger = new AnomalyTriggerService();
    int n = args.Length > 0 ? int.Parse(args[0]) : 150;
    var rules = new[] { ("as now", false, 0), ("armor T1-3 half", true, 0), ("player 8 AP", false, 4), ("both", true, 4) };
    for (int tier = 1; tier <= 5; tier++) {
      var b = Build[tier];
      { GameLog.Target = new Quiet(); CombatRules.PlayerApBonus = 0; CombatRules.ArmorScaleByTier = new[] { 1f, 1f, 1f, 1f, 1f, 1f }; GameBootstrap.StartNewGame(); var p = Player.Current; p.Rank = Rank[tier]; p.EquipArmor("tier" + tier, MidArmor(tier));
        Console.WriteLine($"=== Tier {tier}: {Rank[tier]} (AP {p.MaxAp}, mod {CombatRules.RankMod(p.Rank):+0;-0}), {WeaponDatabase.Categories["sidearm"][b.main].Name}{(b.off != null ? " x2" : "")}, {p.EquippedArmor.Name} DR {p.PhysDR:P0}, perks {string.Join("+", b.perks)}, 3 {b.medkit}"); }
      foreach (var (rname, armorHalf, apBonus) in rules) {
        Console.WriteLine($"  -- {rname}");
        foreach (var (zone, faction) in Foes[tier])
        foreach (bool ambush in new[] { false, true })
        foreach (int size in new[] { 1, 2, 3 }) {
          int wins = 0, deaths = 0, rounds = 0, hp = 0; var foeDesc = new Dictionary<string, int>();
          for (int f = 0; f < n; f++) {
            GameLog.Target = new Quiet();
            CombatRules.ArmorScaleByTier = new[] { 1f, 1f, 1f, 1f, 1f, 1f }; CombatRules.PlayerApBonus = 0;
            GameBootstrap.StartNewGame();
            if (armorHalf) CombatRules.ArmorScaleByTier = new[] { 1f, 0.5f, 0.5f, 0.5f, 1f, 1f };
            CombatRules.PlayerApBonus = apBonus;
            GameState.Current.Mode = GameMode.Tactical; GameState.Current.CurrentTurn = Turn.Player;
            var p = Player.Current; p.Rank = Rank[tier];
            p.EquipArmor("tier" + tier, MidArmor(tier));
            p.EquipWeapon("sidearm", b.main);
            foreach (var perk in b.perks) p.ActivePerks.Add(perk);
            if (b.off != null) { p.OffhandWeapon = p.MakeWeapon("sidearm", b.off); p.OffhandKey = b.off; p.OffhandWeapon.CurrentMagazine = p.OffhandWeapon.MagCapacity; }
            p.EquippedWeapon.CurrentMagazine = p.EquippedWeapon.MagCapacity;
            foreach (var t in new[] { p.EquippedWeapon.CurrentAmmoType ?? "Standard", "Standard" }) p.Inventory.AmmoReserve[t] = 200;
            p.Inventory.Items.Add(new InventoryItem { Id = b.medkit, Name = b.medkit, Type = "consumable", Qty = 3, WeightKg = 0.3f });
            EncounterState.Enemies.Clear(); MacroState.Current.OfflineSquads.Clear(); EncounterState.InCombat = !ambush; EncounterState.ResetFight();
            for (int y = p.Y - 6; y <= p.Y + 6; y++) for (int x = p.X - 3; x <= p.X + 14; x++) if (x > 0 && y > 0 && x < TerrainMap.Width - 1 && y < TerrainMap.Height - 1) TerrainMap.SetTileType(x, y, TileType.Empty);
            foreach (var (dx, dy) in new[] { (1, 0), (1, -1), (1, 1), (0, -1), (-1, -1) }) TerrainMap.SetTileType(p.X + dx, p.Y + dy, TileType.HardCover);
            var sq = SquadManager.CreateOfflineSquad(zone, faction, size);
            var g = sq.Members.OfType<HumanEnemy>().Take(size).ToList();
            for (int i = 0; i < g.Count; i++) {
              var h = g[i]; h.X = p.X + 6 + i % 2; h.Y = p.Y - 2 + i * 2; h.IsAggroed = !ambush;
              TerrainMap.SetTileType(h.X - 1, h.Y, TileType.HardCover);
              EncounterState.Enemies.Add(h);
              var key = h.Rank; foeDesc[key] = foeDesc.TryGetValue(key, out var c) ? c + 1 : 1;
            }
            p.StartTurn(); p.HealPermanent(200);
            int r = 0;
            for (; r < 40 && p.IsAlive() && g.Any(e => !Out(e)); r++) {
              int shots = 0; bool ended = false;
              for (int k = 0; k < 10 && GameState.Current.CurrentTurn == Turn.Player && p.Ap > 0; k++) {
                var w = p.EquippedWeapon;
                if (p.Hp < 45 && p.Ap >= 2 && p.Inventory.Items.Any(i => i.Id == b.medkit && i.Qty > 0)) { p.UseConsumable(b.medkit); continue; }
                if (w.IsJammed) { p.ClearWeaponJam(); continue; }
                if (!w.IsLoaded || (p.OffhandWeapon != null && !p.OffhandWeapon.IsLoaded)) { int ap0 = p.Ap; p.ReloadWeapon(); if (p.Ap == ap0 && !w.IsLoaded) break; continue; }
                var t = g.Where(e => !Out(e) && CombatantUtil.Distance(p, e) <= w.MaxReach).OrderBy(e => e.Hp).FirstOrDefault();
                int alive = g.Count(e => !Out(e));
                if (shots >= 1 && alive >= 2 && p.Hp < 50 && p.CanHunker(out _)) { p.HunkerDown(); ended = true; break; }
                if (t == null || p.Ap < 2) break;
                int before = p.Ap; p.Attack(t, null); shots++;
                if (p.Ap == before) break;
              }
              TurnLoop.CheckCombatEnd();
              if (!ended && GameState.Current.CurrentTurn == Turn.Player) TurnLoop.PlayerEndTurn();
            }
            rounds += r;
            if (!p.IsAlive()) deaths++; else if (!g.Any(e => !Out(e))) { wins++; hp += p.Hp; }
          }
          string foes = string.Join(" ", foeDesc.OrderBy(kv => CombatRules.RankIndex(kv.Key)).Select(kv => $"{kv.Key} {kv.Value * 100 / (n * size)}%"));
          Console.WriteLine($"    {faction,-11} {(ambush ? "ambush" : "open  ")} vs {size}: win {wins * 100 / n,3}%  die {deaths * 100 / n,3}%  hp left {(wins > 0 ? hp / wins : 0),3}  rounds {rounds / (double)n,4:F1}   [{foes}]");
        }
      }
    }
    return 0;
  }
}
