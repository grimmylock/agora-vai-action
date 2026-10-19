// Batch 84: why is Tier 5 easy? Player vs a typical squad, both directions, from the game's own numbers (PreviewHit, PhysicalDamage).
using System; using System.Linq; using System.Collections.Generic; using GameCore; using GameData;
static class DiagMain {
  static readonly string[] Rank = { "", "Rookie", "Experienced", "Professional", "Veteran", "Expert" };
  static string MidArmor(int t) { var l = ArmorDatabase.Tiers["tier" + t].Where(kv => !FactionArmorDatabase.IsFactionIssue(kv.Key)).OrderBy(kv => kv.Value.PhysDR).ToList(); return l[l.Count / 2].Key; }
  static string Gun(string cat, int t) => WeaponDatabase.Categories[cat].Where(kv => kv.Value.Tier <= t && kv.Key != "G7RailPrototype").OrderByDescending(kv => kv.Value.Tier).ThenByDescending(kv => kv.Value.BaseDamage).First().Key;
  static int Dmg(Weapon w, ICombatant target) {
    float ignore = w.Db?.IgnorePhysDRPercent ?? 0f;
    return CombatRules.PhysicalDamage(w.BaseDamage, CombatRules.IsMelee(w) ? "Standard" : w.CurrentAmmoType, target.PhysDR * CombatRules.ArmorScale(target), ignore);
  }
  static int Main() {
    GameLog.Target = null;
    GameServices.Grid = new TacticalGridSystem(); GameServices.Combat = new CombatResolver(); GameServices.AnomalyTrigger = new AnomalyTriggerService();
    var start = Environment.GetEnvironmentVariable("START");   // Batch 127: START=<loadout> uses exactly the New Game kit (Tier 1 only)
    var cases = start != null ? new[] { (1, "perimeter", "Raiders") } : new[] { (3, "rail_yards", "Contractors"), (4, "ghost_town", "Contractors"), (4, "ghost_town", "Raiders"), (5, "quarry_basin", "Contractors"), (5, "quarry_basin", "Obelisk") };
    foreach (var cat in start != null ? new[] { "assault" } : new[] { "assault", "sidearm" }) {
      Console.WriteLine($"=== player with the best {cat} of the tier, mid armor of the tier, no perks; enemies 6 tiles away in the open");
      foreach (var (tier, zone, faction) in cases) {
        double pHit = 0, pDmg = 0, eHit = 0, eDmg = 0, eHp = 0, eAc = 0, eDr = 0; int n = 0, squads = 0, apUsers = 0; var guns = new Dictionary<string, int>();
        int pAc = 0; float pDr = 0; string pGun = ""; int pShotAp = 0;
        for (int f = 0; f < 60; f++) {
          GameBootstrap.StartNewGame(start != null ? new CharacterSpec { Loadout = start } : null); GameState.Current.Mode = GameMode.Tactical;
          var p = Player.Current;
          if (start == null) { p.Rank = Rank[tier]; p.EquipArmor("tier" + tier, MidArmor(tier)); p.EquipWeapon(cat, Gun(cat, tier)); p.EquippedWeapon.CurrentMagazine = p.EquippedWeapon.MagCapacity; p.ApplyCarryEffects(); }
          pAc = p.Ac; pDr = p.PhysDR; pGun = p.EquippedWeapon.Name; pShotAp = CombatRules.SingleShotAp(p.EquippedWeapon);
          for (int y = p.Y - 8; y <= p.Y + 8; y++) for (int x = p.X - 3; x <= p.X + 10; x++) if (x > 0 && y > 0 && x < TerrainMap.Width - 1 && y < TerrainMap.Height - 1) TerrainMap.SetTileType(x, y, TileType.Empty);
          OfflineSquad sq = null;
          for (int tries = 0; tries < 30; tries++) { MacroState.Current.OfflineSquads.Clear(); FactionDeck.Reset(); sq = SquadManager.CreateOfflineSquad(zone, faction, SquadManager.HumanSquadSize(faction, tier)); if (sq != null && !SquadManager.Settle(sq, true)) break; }
          squads++;
          int i = 0;
          foreach (var h in sq.Members.OfType<HumanEnemy>()) {
            h.X = p.X + 6; h.Y = p.Y - 4 + 2 * i++; h.IsAggroed = true; EncounterState.Enemies.Add(h);
            var ph = p.PreviewShot(h, null).hit; pHit += ph.Chance; pDmg += Dmg(p.EquippedWeapon, h);
            var w = h.EquippedWeapon; if (w == null) continue;
            var eh = w.PreviewHit(h, p, false, null, h.D20Mod); eHit += eh.Chance; eDmg += Dmg(w, p);
            eHp += h.MaxHp; eAc += h.Ac; eDr += h.PhysDR * CombatRules.ArmorScale(h); n++; if (h.ApRounds) apUsers++;
            guns[w.Name] = (guns.TryGetValue(w.Name, out var c) ? c : 0) + 1;
          }
          EncounterState.Enemies.Clear();
        }
        double ph1 = pHit / n, pd = pDmg / n, eh1 = eHit / n, ed = eDmg / n, hp = eHp / n;
        double perEnemyShots = Math.Ceiling(hp / Math.Max(1, pd)) / Math.Max(0.01, ph1);
        int pAp = CombatRules.PlayerRankAp(Rank[tier]);
        double squadSize = n / (double)squads;
        double playerTurnsToWin = perEnemyShots * squadSize / Math.Max(1, pAp / pShotAp);
        double squadDmgPerTurn = squadSize * 2 * eh1 * ed;   // 4 AP = 2 single shots each
        Console.WriteLine($"T{tier} {faction,-11} you: {pGun}, AC {pAc}, DR {pDr:P0} | them: {squadSize:F1} a squad, HP {hp:F0}, AC {eAc / n:F1}, DR {eDr / n:P0}, AP rounds {apUsers * 100 / n}%, guns {string.Join(", ", guns.OrderByDescending(g => g.Value).Take(3).Select(g => g.Key))}");
        Console.WriteLine($"      you hit {ph1:P0} for {pd:F0} -> {perEnemyShots:F1} shots a kill, ~{playerTurnsToWin:F1} turns for the squad | they hit {eh1:P0} for {ed:F0} -> the squad does ~{squadDmgPerTurn:F0} a turn to your 100 HP ({100 / Math.Max(1, squadDmgPerTurn):F1} turns)");
      }
    }
    return 0;
  }
}
