using System; using System.Linq; using GameData; using GameCore;
static class Program {
  static int fails = 0;
  static void Check(bool ok, string msg) { if (!ok) { fails++; Console.WriteLine("FAIL " + msg); } }
  static int Main(string[] args) {
    TerrainChecks(args);
    CombatChecks();
    EconomyChecks();
    WorldChecks();
    ProgressionChecks();
    StoryChecks();
    UpgradeChecks();
    LegendChecks();
    QuestRuleChecks();
    ArtifactChecks();
    HazardChecks();
    args_soak = args.Contains("soak");
    ReviewFixChecks();
    AiFixChecks();
    ViewChecks();
    SoakTest();
    FightSim(args);
    // every faction line resolves to real armor, and every armor has a sprite
    var atlas = SpriteAtlasIndex.Armor.ToHashSet();
    foreach (var kv in FactionArmorDatabase.Lines)
      for (int t = 1; t <= 5; t++) foreach (var key in kv.Value[t-1]) {
        var a = ArmorDatabase.Get($"tier{t}", key);
        Check(a != null, $"{kv.Key} T{t} {key} missing");
        if (a != null) Check(atlas.Contains(a.Name), $"no sprite for {a.Name}");
      }
    var allNames = ArmorDatabase.Tiers.Values.SelectMany(d => d.Values).Select(a => a.Name).ToHashSet();
    foreach (var n in SpriteAtlasIndex.Armor) Check(allNames.Contains(n), $"atlas name not an armor: {n}");
    Check(ArmorDatabase.Tiers.Values.Sum(d => d.Count) == 180, "armor count " + ArmorDatabase.Tiers.Values.Sum(d => d.Count));
    foreach (var t in MutantDatabase.Tiers.Values) foreach (var m in t.Values) Check(SpriteAtlasIndex.Mutants.Contains(m.Name), "no mutant sprite " + m.Name);
    Check(ShopDatabase.Items.Count(i => i.FactionIssue) == 143, "faction issue in shop list " + ShopDatabase.Items.Count(i => i.FactionIssue));
    // loadouts: every human faction, rank and zone tier gets weapon + armor; categories vary
    var ranks = FactionDatabase.Ranks.Keys.ToArray();
    foreach (var f in FactionDatabase.Factions.Keys.Where(f => f != "Mutant")) {
      Check(FactionArmorDatabase.Lines.ContainsKey(f == "Rookie" ? "Wanderers" : f), "no line for " + f);
      var cats = new System.Collections.Generic.HashSet<string>();
      foreach (var r in ranks) for (int z = 1; z <= 7; z++) for (int i = 0; i < 40; i++) {
        var (w, a) = EncounterGeneration.PickFactionLoadout(f, r, z, i == 0);
        Check(w != null && a != null, $"null loadout {f} {r} z{z}");
        var (min, max) = FactionArmorDatabase.ArmorTierRange(r, z);
        Check(min >= 1 && min <= max && max <= 5 && max <= Math.Max(1, z + 1), $"tier range {r} z{z} {min}-{max}");
        if (w != null) cats.Add(WeaponDatabase.Categories.First(c => c.Value.ContainsValue(w)).Key);
      }
      Check(cats.Count == 7, $"{f} only uses {cats.Count} weapon categories");
    }
    // raiders in a T1 zone wear raider tier-1/2 gear
    for (int i = 0; i < 200; i++) {
      var a = FactionArmorDatabase.PickArmor("Raiders", "Rookie", 1);
      Check(FactionArmorDatabase.Lines["Raiders"].Take(2).SelectMany(x => x).Any(k => ArmorDatabase.Get("tier1", k) == a || ArmorDatabase.Get("tier2", k) == a), "raider rookie armor off-line: " + a?.Name);
    }
    Check(FactionArmorDatabase.PickArmor("Ecologist", "Expert", 4) != null, "Ecologist alias");
    Check(EncounterGeneration.PickRandomArmorByTier(1, 5, includeFactionIssue: false) is var x && !FactionArmorDatabase.Lines.Values.Any(l => false) , "");
    for (int i = 0; i < 300; i++) { var a = EncounterGeneration.PickRandomArmorByTier(1, 5, false); var key = ArmorDatabase.Tiers.Values.SelectMany(d => d).First(kv => kv.Value == a).Key; Check(!FactionArmorDatabase.IsFactionIssue(key), "trader got faction armor " + key); }
    Console.WriteLine(fails == 0 ? "ALL CHECKS PASSED" : $"{fails} FAILURES");
    return fails == 0 ? 0 : 1;
  }

  // --- terrain: destructible tiles, doors, barrels, and a render of every zone through TerrainArt ---
  static void TerrainChecks(string[] args) {
    string art = Environment.GetEnvironmentVariable("ART_DIR");
    UnityEngine.Resources.Hook = (path, type) => {
      if (art == null) return null;
      if (path == "Sprites/terrain_atlas") return new UnityEngine.TextAsset { bytes = System.IO.File.ReadAllBytes(art + "/terrain_atlas.raw") };
      if (path == "Sprites/terrain_names") return new UnityEngine.TextAsset { text = System.IO.File.ReadAllText(art + "/terrain_names.txt") };
      return null;
    };
    GameServices.Grid = new TacticalGridSystem();
    GameServices.Combat = new CombatResolver();
    MacroState.Current.CurrentZoneId = "perimeter";
    TerrainMap.BuildTerrainForZone("perimeter");
    // find an open tile with open tiles around it, stage a little scene
    int cx = 15, cy = 15;
    for (int y = cy - 2; y <= cy + 2; y++) for (int x = cx - 2; x <= cx + 2; x++) TerrainMap.SetTileType(x, y, TileType.Empty);
    TerrainMap.SetTileType(cx, cy, TileType.ScatterCover);
    Check(TerrainMap.CoverAcAt(cx, cy) == 1, "light cover AC");
    Check(!TerrainMap.DamageTile(cx, cy, 2), "cover broke early");
    Check(TerrainMap.DamageTile(cx, cy, 1), "cover didn't break at 3");
    Check(TerrainMap.GetTile(cx, cy).Type == TileType.Difficult && TerrainMap.CoverAcAt(cx, cy) == 0, "cover -> rubble");
    Check(GameServices.Grid.GetMoveCost(new HumanEnemy("t", cx - 1, cy, "Raiders", "Rookie", null, null), cx, cy) == 2, "rubble costs 2 MP");
    // door: blocks LoS while closed, opens on entry
    TerrainMap.SetTileType(cx + 1, cy, TileType.Door);
    Check(!GameServices.Grid.CheckLineOfSight(cx, cy, cx + 2, cy), "closed door should block LoS");
    var walker = new HumanEnemy("w", cx, cy, "Raiders", "Rookie", null, null) { Mp = 4 };
    Check(GameServices.Grid.AttemptMove(walker, cx + 1, cy) && TerrainMap.GetTile(cx + 1, cy).Type == TileType.DoorOpen, "door opens on entry");
    Check(GameServices.Grid.CheckLineOfSight(cx, cy, cx + 2, cy), "open door shouldn't block LoS");
    // barrel blast: hurts both men in range, wrecks light cover, chips hard cover, chains
    TerrainMap.SetTileType(cx, cy + 2, TileType.Barrel);
    TerrainMap.SetTileType(cx + 1, cy + 2, TileType.ScatterCover);
    TerrainMap.SetTileType(cx - 1, cy + 2, TileType.HardCover);
    TerrainMap.SetTileType(cx - 1, cy + 1, TileType.Barrel);
    var a = new HumanEnemy("a", cx + 1, cy + 1, "Raiders", "Rookie", null, null);
    var b = new HumanEnemy("b", cx + 2, cy - 2, "Raiders", "Rookie", null, null);
    EncounterState.Enemies.Clear(); EncounterState.Enemies.Add(a); EncounterState.Enemies.Add(b);
    Check(TerrainMap.DamageTile(cx, cy + 2, 1), "barrel didn't blow");
    Check(a.Hp < 100, "man next to barrel unhurt");
    Check(b.Hp == 100, "man far away hurt");
    Check(TerrainMap.GetTile(cx + 1, cy + 2).Type == TileType.Difficult, "blast didn't wreck light cover");
    Check(TerrainMap.GetTile(cx - 1, cy + 1).Type == TileType.Difficult, "chain barrel didn't go off");
    Check(TerrainMap.GetTile(cx - 1, cy + 2).Type == TileType.HardCover && TerrainMap.GetTile(cx - 1, cy + 2).Damage == 6 - 0 || TerrainMap.GetTile(cx - 1, cy + 2).Type == TileType.Difficult,
          "hard cover: two blasts of 3 should break it");
    EncounterState.Enemies.Clear();
    // cover as AC: shooter west, target behind cover east of it
    for (int y = 30; y <= 34; y++) for (int x = 5; x <= 15; x++) TerrainMap.SetTileType(x, y, TileType.Empty);
    var shooter = new HumanEnemy("s", 5, 32, "Raiders", "Rookie", null, null);
    var target = new HumanEnemy("t", 12, 32, "Raiders", "Rookie", null, null);
    TerrainMap.SetTileType(11, 32, TileType.HardCover);
    var cov = CoverUtil.GetCover(shooter, target, null);
    Check(cov.AcBonus == 2 && cov.HasTile && cov.TileX == 11, "hard cover between should give +2");
    TerrainMap.SetTileType(11, 32, TileType.Empty); TerrainMap.SetTileType(12, 31, TileType.ScatterCover);
    cov = CoverUtil.GetCover(shooter, target, null);
    Check(cov.AcBonus == 0 && cov.Flanked, "cover off the firing line = flanked");
    TerrainMap.SetTileType(12, 31, TileType.Empty); TerrainMap.SetTileType(12, 32, TileType.ScatterCover);
    Check(CoverUtil.GetCover(shooter, target, null).AcBonus == 1, "standing on cover far away");
    shooter.X = 10; Check(CoverUtil.GetCover(shooter, target, null).AcBonus == 0, "standing on cover vs attacker within 2");

    // every zone: all exits and landmarks reachable from the first exit on foot (4-dir, max 4 MP a step)
    var probe = new HumanEnemy("probe", 0, 0, "Raiders", "Rookie", null, null);
    foreach (var zone in MacroMapDatabase.Zones) {
      MacroState.Current.CurrentZoneId = zone.Id;
      MacroState.Current.PersistedZoneTerrain.Remove(zone.Id);
      foreach (var h in zone.HiddenConnections ?? new System.Collections.Generic.List<string>()) if (!MacroState.Current.DiscoveredHiddenNodes.Contains(h)) MacroState.Current.DiscoveredHiddenNodes.Add(h);
      TerrainMap.BuildTerrainForZone(zone.Id);
      if (TerrainMap.Exits.Count == 0) { Check(false, zone.Id + " has no exits"); continue; }
      var start = (TerrainMap.Exits[0].X, TerrainMap.Exits[0].Y);
      var seen = new System.Collections.Generic.HashSet<(int, int)> { start };
      var q = new System.Collections.Generic.Queue<(int x, int y)>(); q.Enqueue(start);
      while (q.Count > 0) {
        var cur = q.Dequeue();
        foreach (var d in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }) {
          int nx = cur.x + d.Item1, ny = cur.y + d.Item2;
          if (seen.Contains((nx, ny)) || !TerrainMap.CanEnterTile(nx, ny)) continue;
          probe.X = cur.x; probe.Y = cur.y;
          if (GameServices.Grid.GetMoveCost(probe, nx, ny) > 4) continue;
          seen.Add((nx, ny)); q.Enqueue((nx, ny));
        }
      }
      // reverse search: tiles you can walk into but never back out of (a one-way drop) are traps
      var back = new System.Collections.Generic.HashSet<(int, int)> { start };
      q.Enqueue(start);
      while (q.Count > 0) {
        var cur = q.Dequeue();
        foreach (var d in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }) {
          int nx = cur.x + d.Item1, ny = cur.y + d.Item2;
          if (back.Contains((nx, ny)) || !TerrainMap.CanEnterTile(nx, ny)) continue;
          probe.X = nx; probe.Y = ny;
          if (GameServices.Grid.GetMoveCost(probe, cur.x, cur.y) > 4) continue;
          back.Add((nx, ny)); q.Enqueue((nx, ny));
        }
      }
      int traps = seen.Count(t => !back.Contains(t));
      if (traps > 0) Check(false, $"{zone.Id}: {traps} tiles you can reach but never leave, e.g. {seen.First(t => !back.Contains(t))}");
      foreach (var e in TerrainMap.Exits) Check(seen.Contains((e.X, e.Y)), $"{zone.Id}: exit to {e.TargetZoneId} at ({e.X},{e.Y}) unreachable");
      foreach (var l in TerrainMap.Landmarks) Check(seen.Contains((l.X, l.Y)), $"{zone.Id}: landmark {l.Name} at ({l.X},{l.Y}) unreachable");
      int walk = 0; for (int y = 0; y < TerrainMap.Height; y++) for (int x = 0; x < TerrainMap.Width; x++) if (TerrainMap.CanEnterTile(x, y)) walk++;
      if (seen.Count < walk * 0.85) Console.WriteLine($"note {zone.Id}: only {seen.Count}/{walk} walkable tiles reachable");
    }

    if (art == null) return;
    foreach (var zone in MacroMapDatabase.Zones) {
      MacroState.Current.CurrentZoneId = zone.Id;
      TerrainMap.BuildTerrainForZone(zone.Id);
      var px = TerrainArt.Compose(zone.Id);
      int w = TerrainMap.Width * 16, h = TerrainMap.Height * 16;
      var bytes = new byte[12 + px.Length * 4];
      System.Text.Encoding.ASCII.GetBytes("RAW1").CopyTo(bytes, 0);
      BitConverter.GetBytes(w).CopyTo(bytes, 4); BitConverter.GetBytes(h).CopyTo(bytes, 8);
      for (int i = 0; i < px.Length; i++) { bytes[12 + i * 4] = px[i].r; bytes[13 + i * 4] = px[i].g; bytes[14 + i * 4] = px[i].b; bytes[15 + i * 4] = px[i].a; }
      System.IO.File.WriteAllBytes($"{art}/render_{zone.Id}.raw", bytes);
    }
  }

  static Weapon W(string cat, string key) { var d = WeaponDatabase.Categories[cat][key]; var w = new Weapon(d.Name, d.BaseDamage, d.MagCapacity ?? d.CylinderCapacity ?? 1, "Standard", null) { Db = d }; return w; }
  static void CombatChecks() {
    Check(CombatRules.PhysicalDamage(30, "Standard", 0.3f) == 21, "std dmg");
    Check(CombatRules.PhysicalDamage(30, "HollowPoint", 0f) == 38, "HP vs 0% x1.25");
    Check(CombatRules.PhysicalDamage(30, "HollowPoint", 0.1f) == 27, "HP vs 10% no change");
    Check(CombatRules.PhysicalDamage(30, "HollowPoint", 0.3f) == 11, "HP vs 30% x0.5");
    Check(CombatRules.PhysicalDamage(30, "ArmorPiercing", 0.5f) == 24, "AP x0.4 DR");
    Check(CombatRules.PhysicalDamage(30, "Standard", 0.9f) == 8, "DR cap 75%");
    Check(CombatRules.PhysicalDamage(30, "Standard", 0.5f, 0.2f) == 18, "ignore 20%");
    var vk74 = W("assault", "VK74"); var vk103 = W("assault", "VK103"); var gnat = W("smg", "CorvanGnatPDW"); var pl74 = W("heavy", "PL74");
    Check(CombatRules.CanBurst(vk74) && CombatRules.BurstPenalty(vk74) == -2 && CombatRules.BurstRounds(vk74) == 3 && CombatRules.BurstMultiplier(vk74) == 2f, "VK-74 burst");
    Check(CombatRules.BurstPenalty(gnat) == -1 && CombatRules.BurstRounds(gnat) == 5 && CombatRules.BurstApCost(gnat) == 2, "SMG burst");
    Check(CombatRules.BurstRounds(pl74) == 10 && CombatRules.BurstMultiplier(pl74) == 3f, "heavy burst");
    Check(!CombatRules.CanBurst(W("sidearm", "PSh18Pchela")), "pistol can't burst");
    Check(CombatRules.Jams(1, vk74, false) && CombatRules.Jams(2, vk74, true) && !CombatRules.Jams(2, vk74, false), "jam rules");
    Check(!CombatRules.Jams(2, vk103, true), "VK-103 reliable");
    vk74.Condition = ItemCondition.Dirty; Check(CombatRules.Jams(2, vk74, false), "dirty jams on 2");
    CombatRules.DegradeOnJam(vk74, 2); Check(vk74.Condition == ItemCondition.Damaged, "jam degrades");
    vk74.Upgrades.Add("Hardened"); CombatRules.DegradeOnJam(vk74, 2); Check(vk74.Condition == ItemCondition.Damaged, "hardened ignores a non-1 jam"); vk74.Upgrades.Clear();
    Check(CombatRules.RankAc("Rookie") == 10 && CombatRules.RankAc("Expert") == 12, "rank AC");
    Check(CombatRules.TraitToHit(W("shotgun", "RangerP8"), false, 3) == 2, "shotgun close +2");
    // mutant claws vs armor: 20 dmg Veil Prowler vs 40% armor = 12
    var veil = new Mutant("v", 1, 1, MutantDatabase.Get("tier2", "VeilProwler"));
    var man = new HumanEnemy("m", 2, 1, "Raiders", "Rookie", null, ArmorDatabase.Tiers["tier1"].Values.First());
    Check(CombatRules.PhysicalDamage(veil.AttackDamage, "Standard", 0.4f) == 12, "mutant vs armor");
    Check(man.Ac == 10, "rookie NPC AC 10");
    // wounds: a 25-dmg hit on a human always wounds
    CombatRules.CheckWound(man, 25, false); Check(man.StatusEffects.Wound != null, "wound applied");
  }

  class ConsoleLog : IGameLogTarget { public bool Clicker; public int Lines; public System.Collections.Generic.Dictionary<string, int> Counts;
    public void AppendLine(string m) { Lines++; if (Clicker) Console.WriteLine("  | " + m);
      if (Counts != null) { var k = new string(m.TakeWhile(c => !char.IsDigit(c)).Take(40).ToArray()); Counts[k] = (Counts.TryGetValue(k, out var n) ? n : 0) + 1; } } }
  static void FightSim(string[] args) {
    var log = new ConsoleLog { Clicker = args.Contains("fightlog") };
    GameLog.Target = log;
    GameServices.Grid = new TacticalGridSystem(); GameServices.Combat = new CombatResolver(); GameServices.AnomalyTrigger = new AnomalyTriggerService();
    int playerWins = 0, deaths = 0;
    for (int fight = 0; fight < 30; fight++) {
      try {
        GameBootstrap.StartNewGame();
        GameState.Current.Mode = GameMode.Tactical; GameState.Current.CurrentTurn = Turn.Player;
        var p = Player.Current;
        p.Inventory.Items.Add(new InventoryItem { Id = "fragGrenade", Name = "Frag Grenade", Type = "consumable", Qty = 2, WeightKg = 0.5f });
        EncounterState.Enemies.Clear();
        // clear a 12x5 arena next to the player, two raiders + a blind maw
        for (int y = p.Y - 2; y <= p.Y + 2; y++) for (int x = p.X; x <= p.X + 12; x++) if (x < TerrainMap.Width - 1 && y > 0 && y < TerrainMap.Height - 1) TerrainMap.SetTileType(x, y, TileType.Empty);
        TerrainMap.SetTileType(p.X + 7, p.Y, TileType.HardCover);
        var (w1, a1) = EncounterGeneration.PickFactionLoadout("Raiders", "Experienced", 1);
        var r1 = new HumanEnemy("Raider A", p.X + 8, p.Y, "Raiders", "Experienced", w1, a1) { IsAggroed = true };
        var (w2, a2) = EncounterGeneration.PickFactionLoadout("Raiders", "Rookie", 1);
        var r2 = new HumanEnemy("Raider B", p.X + 9, p.Y + 2, "Raiders", "Rookie", w2, a2) { IsAggroed = true };
        string[] mk = { "tier1/BlindMaw", "tier2/VeilProwler", "tier4/DreadProwler", "tier4/BlightBrute", "tier3/MindWeaver", "tier5/VoidWeaver" };
        var mkey = mk[fight % mk.Length].Split('/');
        var maw = new Mutant(mkey[1], p.X + 5, p.Y - 2, MutantDatabase.Get(mkey[0], mkey[1])) { IsAggroed = true };
        p.StartTurn(); p.HealPermanent(100);
        EncounterState.Enemies.AddRange(new Mutant[] { r1, r2, maw });
        for (int round = 0; round < 25 && p.IsAlive() && EncounterState.Enemies.Any(e => e.IsAlive()); round++) {
          if (round == 1 && fight % 3 == 0) { p.ArmedThrowable = "fragGrenade"; p.ThrowAt(r1.X, r1.Y); }
          if (fight % 2 == 0 && p.EquippedWeapon?.IsJammed == true) p.ClearWeaponJam();
          for (int k = 0; k < 3; k++) {
            var t = EncounterState.Enemies.Where(e => e.IsAlive()).OrderBy(e => CombatantUtil.Distance(p, e)).FirstOrDefault();
            if (t == null) break;
            if (!p.EquippedWeapon.IsLoaded) p.ReloadWeapon();
            else if (CombatantUtil.Distance(p, t) > p.EquippedWeapon.RangeTiles) { p.Move(Math.Sign(t.X - p.X), 0); }
            else p.Attack(t, null);
          }
          TurnLoop.CheckCombatEnd();
          if (GameState.Current.CurrentTurn == Turn.Player) TurnLoop.PlayerEndTurn();
        }
        if (!p.IsAlive()) deaths++; else if (!EncounterState.Enemies.Any(e => e.IsAlive())) playerWins++;
      } catch (Exception e) { Check(false, "fight sim threw: " + e); break; }
    }
    Console.WriteLine($"fight sim: 30 fights, player won {playerWins}, died {deaths}, {log.Lines} log lines");
    // fair duels: fresh rookie vs one rookie raider (and vs one Tier 1 mutant), plain shoot-out
    foreach (var foe in new[] { "raider", "mutant" }) {
      int w = 0, d = 0, hpLeft = 0;
      for (int fight = 0; fight < 100; fight++) {
        GameBootstrap.StartNewGame();
        GameState.Current.Mode = GameMode.Tactical; GameState.Current.CurrentTurn = Turn.Player;
        var p = Player.Current;
        EncounterState.Enemies.Clear();
        for (int y = p.Y - 2; y <= p.Y + 2; y++) for (int x = p.X; x <= p.X + 10; x++) if (x < TerrainMap.Width - 1 && y > 0 && y < TerrainMap.Height - 1) TerrainMap.SetTileType(x, y, TileType.Empty);
        Mutant e;
        if (foe == "raider") { var (w1, a1) = EncounterGeneration.PickFactionLoadout("Raiders", "Rookie", 1); e = new HumanEnemy("Raider", p.X + 6, p.Y, "Raiders", "Rookie", w1, a1) { IsAggroed = true }; }
        else { var keys = MutantDatabase.Tiers["tier1"].Keys.ToArray(); var k = keys[fight % keys.Length]; e = new Mutant(k, p.X + 6, p.Y, MutantDatabase.Get("tier1", k)) { IsAggroed = true }; }
        p.StartTurn(); p.HealPermanent(100);
        EncounterState.Enemies.Add(e);
        for (int round = 0; round < 25 && p.IsAlive() && e.IsAlive() && !(e is HumanEnemy h0 && EnemyAI.IsOut(h0)); round++) {
          if (p.EquippedWeapon.IsJammed) p.ClearWeaponJam();
          for (int k = 0; k < 3 && e.IsAlive(); k++) {
            if (!p.EquippedWeapon.IsLoaded) p.ReloadWeapon();
            else if (CombatantUtil.Distance(p, e) > p.EquippedWeapon.RangeTiles) p.Move(Math.Sign(e.X - p.X), 0);
            else p.Attack(e, null);
          }
          TurnLoop.CheckCombatEnd();
          if (GameState.Current.CurrentTurn == Turn.Player) TurnLoop.PlayerEndTurn();
        }
        if (!p.IsAlive()) d++; else { w++; hpLeft += p.Hp; }
      }
      Console.WriteLine($"duel vs {foe}: 100 fights, player won/outlasted {w}, died {d}, avg hp left {(w > 0 ? hpLeft / w : 0)}/{Player.Current.MaxHp}");
    }
    GameLog.Target = null;
  }

  static void EconomyChecks() {
    for (int t = 1; t <= 5; t++) for (int i = 0; i < 400; i++) {
      var r = Economy.Roll(t, i % 2 == 0);
      Check(r != null && r.Type != null, $"roll null t{t}");
      if (r.Type == "weapon") { Check(WeaponDatabase.Get(r.Category, r.Key) != null, "rolled weapon unknown " + r.Name); if (i % 2 == 0) Check(WeaponDatabase.Get(r.Category, r.Key).Tier < 5, "trader rolled T5 weapon"); }
      if (r.Type == "armor") Check(ArmorDatabase.Get(r.Category, r.Key) != null, "rolled armor unknown " + r.Name);
      if (r.Type == "artifact") Check(ArtifactDatabase.All.ContainsKey(r.Id), "artifact id " + r.Id);
      if (r.Type == "consumable") Check(ConsumableDatabase.All.ContainsKey(r.Id), "consumable id " + r.Id);
      if (r.Type == "armorMod") Check(ArmorModDatabase.All.ContainsKey(r.Key), "mod key " + r.Key);
    }
    var stock = Economy.RollTraderStock(2, false);
    Check(stock.Count <= 12 && stock.All(i => i.GrantId != null), "trader stock");
    var ak = new InventoryItem { Type = "weapon", Category = "assault", Key = "VK74", Name = "VK-74" };
    Check(Economy.ValueOf(ak) == 1150, "VK-74 value " + Economy.ValueOf(ak));
    Check(Economy.RepairCost(new InventoryItem { Type = "weapon", Category = "assault", Key = "VK74", Condition = "Damaged" }) == 230, "repair cost");
    Check(ShopDatabase.Items.Count(i => i.Baseline) == 15, "baseline count " + ShopDatabase.Items.Count(i => i.Baseline));
    foreach (var b in ShopDatabase.Items.Where(i => i.Baseline && i.Type == "consumable")) Check(ConsumableDatabase.All.ContainsKey(b.Id), "baseline consumable missing def " + b.Id);
  }

  static void WorldChecks() {
    MacroState.Current = new MacroState();
    if (Player.Current == null) new Player();
    for (int t = 1; t <= 5; t++) for (int i = 0; i < 200; i++) {
      var r = Economy.RollArtifact(t);
      Check(r != null && r.Type == "artifact" && ArtifactDatabase.All.ContainsKey(r.Id), "artifact roll t" + t);
    }
    try {
      for (int d = 0; d < 120; d++) { WorldDays.NewDay(); WorldEvents.TellRumors(); }
    } catch (Exception e) { Check(false, "120 days threw: " + e); }
    var m = MacroState.Current;
    foreach (var kv in m.GoalBoxes) Check(kv.Value >= 0 && kv.Value <= 6, "goal boxes " + kv.Key + "=" + kv.Value);
    Check(!(m.CompletedGoals.Contains("Sentinels") && m.CompletedGoals.Contains("Liberators")), "both barracks factions won");
    Check(m.Rumors.Count <= 10 && m.NamedNpcs.Count <= 10, "rumor/npc caps");
    foreach (var z in MacroMapDatabase.Zones) { Check(WorldEvents.Hazard(z) >= 0, "hazard " + z.Id); var f = WorldEvents.Factions(z); Check(f != null, "factions " + z.Id); }
    Console.WriteLine($"world after 120 days: day {m.Day}, goals done [{string.Join(",", m.CompletedGoals)}], rumors {m.Rumors.Count}");
    Console.WriteLine(GameCore.WorldUI.Describe());
    MacroState.Current = new MacroState();
  }

  static void ProgressionChecks() {
    GameServices.Grid = new TacticalGridSystem(); GameServices.Combat = new CombatResolver(); GameServices.AnomalyTrigger = new AnomalyTriggerService();
    foreach (var l in CharacterUI.Loadouts) {
      GameBootstrap.StartNewGame(new CharacterSpec { Loadout = l, S = 8, P = 12, A = 14, I = 12, Perk = "Haggler" });
      var p = Player.Current;
      Check(p.EquippedWeapon != null && p.EquippedArmor != null, "loadout gear " + l);
      Check(p.Ru == GameConstants.StartingRu + (l == "Scavenger" ? 200 : l == "Ex-Military" ? 100 : 500), $"loadout ru {l} {p.Ru}");
      Check(p.ActivePerks.Contains("Haggler") && p.Stats.A == 14, "spec applied " + l);
      Check(p.Inventory.Items.Any(i => i.Id == "basicMedkit"), "medkit " + l);
      if (l == "Ex-Military") Check(p.EquippedWeapon.Name == "VKS-74U" && p.Inventory.Items.Any(i => i.Type == "weapon"), "ex-mil gear");
      if (l == "Freelance Trader") Check(p.CarryCapacityBonus == 10, "trader sack");
      Check(p.GetRepLevel("Raiders") == "Hostile" && p.GetRepLevel("Wanderers") == "Neutral", "raiders start hostile, others neutral");
      float h0 = p.Hunger; p.UseConsumable("basicMedkit");
      Check(Math.Abs(h0 - p.Hunger - 20 / 10f * GameConstants.SurvivalCostPer10Hp) < 0.001f && p.Tiredness < 10, "medkit makes you hungry and sleepy");
    }
    // rank-ups follow the milestones, one perk pick at Professional
    GameBootstrap.StartNewGame();
    var pl = Player.Current;
    Check(pl.Rank == "Rookie", "starts rookie");
    pl.NoteZoneTier(2);
    Check(pl.Rank == "Rookie", "tier alone isn't enough");
    var q = new QuestDef { Id = "t", Stage = 0 };
    pl.NoteQuestDone(q);
    Check(pl.Rank == "Experienced", "experienced after tier 2 + quest, got " + pl.Rank);
    pl.NoteQuestDone(q); pl.NoteQuestDone(q);
    Check(pl.Rank == "Experienced", "professional needs a kill");
    var m2 = MutantDatabase.Tiers["tier2"].First();
    pl.NoteKill(new Mutant(m2.Key, 0, 0, m2.Value));
    Check(pl.Rank == "Professional" && pl.PendingPerkPicks == 1 && pl.MaxAp == 5, $"professional: {pl.Rank} picks {pl.PendingPerkPicks} ap {pl.MaxAp}");
    pl.HiddenNodeCleared = true; pl.NoteZoneTier(5);
    Check(pl.Rank == "Veteran", "veteran after tier 3 + hidden node, got " + pl.Rank);
    pl.ArcsFinished = 1; pl.NoteKill(new Mutant("x", 0, 0, MutantDatabase.Tiers["tier4"].First().Value));
    Check(pl.Rank == "Expert" && pl.PendingPerkPicks == 2, "expert, got " + pl.Rank);
    pl.NoteKill(new Mutant("x", 0, 0, MutantDatabase.Tiers["tier5"].First().Value));
    Check(pl.Rank == "Expert", "master is gated on the main story");
    // a few perk effects
    pl.ApplyTemporaryHP(10); Check(pl.TempHPDuration == 4, "temp hp 4 turns");
    pl.TakePerk("IronWill"); pl.ApplyTemporaryHP(10); Check(pl.TempHPDuration == 8, "iron will");
    var item = ShopDatabase.Find("basicMedkit");
    int before = ShopSystem.GetBuyPrice(item); pl.TakePerk("Haggler"); int after = ShopSystem.GetBuyPrice(item);
    Check(after < before, $"haggler {before}->{after}");
    pl.EquipWeapon("heavy", WeaponDatabase.Categories["heavy"].Keys.First());
    pl.ApplyCarryEffects(); int capped = pl.MaxMp; pl.TakePerk("WalkingTank"); pl.ApplyCarryEffects();
    Check(pl.MaxMp >= capped, $"walking tank {capped}->{pl.MaxMp}");
    Check(PerkDatabase.All.Count == 34, "perk count " + PerkDatabase.All.Count);
    // Gunslinger: two PMms, one action fires both
    GameBootstrap.StartNewGame(new CharacterSpec { Perk = "Gunslinger" });
    var gs = Player.Current; var glog = new ConsoleLog { Counts = new System.Collections.Generic.Dictionary<string, int>() }; GameLog.Target = glog;
    var pmm = ShopDatabase.Items.First(i => i.Type == "weapon" && i.Key == "PSh18Pchela");
    gs.AddItem(new InventoryItem { Id = pmm.Id, Name = pmm.Name, Type = "weapon", Category = "sidearm", Key = "PSh18Pchela", Qty = 1, WeightKg = pmm.WeightKg });
    gs.EquipOffhandFromInventory(pmm.Id);
    Check(gs.Gunslinging, "gunslinging");
    gs.OffhandWeapon.Reload();
    GameState.Current.Mode = GameMode.Tactical; GameState.Current.CurrentTurn = Turn.Player; gs.Ap = 4;
    var (gw, ga) = EncounterGeneration.PickFactionLoadout("Raiders", "Rookie", 1);
    for (int y = gs.Y - 1; y <= gs.Y + 1; y++) for (int x = gs.X; x <= gs.X + 4; x++) if (x < TerrainMap.Width - 1 && y > 0 && y < TerrainMap.Height - 1) TerrainMap.SetTileType(x, y, TileType.Empty);
    var dummy = new HumanEnemy("Dummy", gs.X + 2, gs.Y, "Raiders", "Rookie", gw, ga); dummy.Hp = 1000; EncounterState.Enemies.Add(dummy);
    gs.Attack(dummy, "Single");
    Check(gs.Ap == 2 && glog.Counts.ContainsKey("Off hand: PSh-"), $"dual shot for 2 AP (ap {gs.Ap})");
    var gsave = gs.CaptureSaveData(); new Player(); Player.Current.ApplyLoadedData(gsave);
    Check(Player.Current.OffhandWeapon != null, "off-hand saved");
    Player.Current.StowOffhand(); Check(Player.Current.OffhandWeapon == null && Player.Current.Inventory.Items.Any(i => i.Key == "PSh18Pchela"), "stow off-hand");
    GameLog.Target = null;
    // every perk survives a few real fights (exercises the hooks for exceptions)
    var log = new ConsoleLog(); GameLog.Target = log;
    foreach (var perk in PerkDatabase.All.Keys) for (int f = 0; f < 3; f++) {
      try {
        GameBootstrap.StartNewGame(new CharacterSpec { Loadout = CharacterUI.Loadouts[f], Perk = perk });
        GameState.Current.Mode = GameMode.Tactical; GameState.Current.CurrentTurn = Turn.Player;
        var p = Player.Current;
        if (perk == "Shredder" || perk == "Entrenched") p.EquipWeapon("heavy", WeaponDatabase.Categories["heavy"].Keys.First());
        if (perk == "BuckshotSpread" || perk == "StaggeringBlast") p.EquipWeapon("shotgun", WeaponDatabase.Categories["shotgun"].Keys.First());
        if (perk == "Overwatch" || perk == "MarksmansFocus") p.EquipWeapon("sniper", WeaponDatabase.Categories["sniper"].Keys.First());
        if (perk == "BladeDancer" || perk == "DesperateParry") p.EquipWeapon("melee", "HuntingKnife");
        p.Inventory.AmmoReserve["Standard"] = 200; p.EquippedWeapon.Reload();
        EncounterState.Enemies.Clear();
        for (int y = p.Y - 2; y <= p.Y + 2; y++) for (int x = p.X; x <= p.X + 10; x++) if (x < TerrainMap.Width - 1 && y > 0 && y < TerrainMap.Height - 1) TerrainMap.SetTileType(x, y, TileType.Empty);
        var (w1, a1) = EncounterGeneration.PickFactionLoadout("Raiders", "Rookie", 1);
        var e1 = new HumanEnemy("R1", p.X + 5, p.Y, "Raiders", "Rookie", w1, a1) { IsAggroed = true };
        var e2 = new HumanEnemy("R2", p.X + 5, p.Y + 1, "Raiders", "Rookie", w1, a1) { IsAggroed = true };
        EncounterState.Enemies.AddRange(new Mutant[] { e1, e2 });
        p.StartTurn();
        for (int round = 0; round < 15 && p.IsAlive() && EncounterState.Enemies.Any(e => e.IsAlive()); round++) {
          if (p.EquippedWeapon.IsJammed) p.ClearWeaponJam();
          if (perk == "RunAndGun") p.RunAndGun();
          if (perk == "SuppressiveFire") { p.EquipWeapon("assault", "VKS74U"); p.EquippedWeapon.Reload(); p.ArmSuppressiveFire(); p.SuppressAt(e1.X, e1.Y); }
          for (int k = 0; k < 4; k++) {
            var t = EncounterState.Enemies.Where(e => e.IsAlive()).OrderBy(e => CombatantUtil.Distance(p, e)).FirstOrDefault();
            if (t == null) break;
            if (!p.EquippedWeapon.IsLoaded && !CombatRules.IsMelee(p.EquippedWeapon)) p.ReloadWeapon();
            else if (CombatantUtil.Distance(p, t) > p.EquippedWeapon.RangeTiles) p.Move(Math.Sign(t.X - p.X), 0);
            else p.Attack(t, null);
          }
          TurnLoop.CheckCombatEnd();
          if (GameState.Current.CurrentTurn == Turn.Player) TurnLoop.PlayerEndTurn();
        }
      } catch (Exception ex) { Check(false, $"perk {perk} fight threw: {ex}"); }
    }
    GameLog.Target = null;
  }

  static bool StoryAt(string zone, string landmark) {
    MacroState.Current.CurrentZoneId = zone; ZoneTravel.EnterZone(zone);
    GameState.Current.CurrentTurn = Turn.Player;
    var p = Player.Current; p.Ap = 5;
    var l = TerrainMap.Landmarks.FirstOrDefault(x => x.Name == landmark);
    Check(l != null, $"landmark {landmark} missing in {zone}");
    if (l == null) return false;
    p.X = l.X; p.Y = l.Y;
    return StorySystem.TryInteract(p);
  }
  static void StoryChecks() {
    var log = new ConsoleLog(); GameLog.Target = log;
    foreach (var sponsor in new[] { "Garrison", "Alone" }) {
      GameBootstrap.StartNewGame();
      var p = Player.Current;
      Check(StorySystem.Act == 1, "act 1");
      Check(StoryAt("secret_lab_alpha", "Sample Storage") && !StorySystem.Has("alpha"), "terminal needs keycard");
      Check(StoryAt("secret_lab_alpha", "Reception Airlock") && StorySystem.Has("keycard"), "keycard");
      Check(StoryAt("secret_lab_alpha", "Sample Storage") && StorySystem.Act == 2, "fragment alpha");
      Check(StoryAt("secret_lab_beta", "Archive Room") && StorySystem.Has("beta_drive"), "beta drive");
      Check(!ShopSystem.SellItem("story_drive_beta"), "key items can't be sold");
      Check(StoryAt("dried_lake", "Sub-Station House") && StorySystem.Act == 3, "osei decodes");
      Check(StoryAt("secret_lab_gamma", "Sealed Vault"), "vault reachable");
      if (!StorySystem.Has("gamma")) { foreach (var e in EncounterState.Enemies) e.Hp = 0; StoryAt("secret_lab_gamma", "Sealed Vault"); }
      Check(StorySystem.Act == 4 && p.Inventory.Items.Any(i => i.Id == "theLodestone") && p.MainStoryAct3Done, "gamma + compass");
      if (sponsor != "Alone") p.AdjustReputation(sponsor, 60);
      Check(StoryAt("abandoned_city", "Garrison Barricade") && StorySystem.Act == 5, "sponsor chosen");
      Check(MacroState.Current.Sponsor == sponsor, $"sponsor {MacroState.Current.Sponsor} vs {sponsor}");
      if (sponsor != "Alone") Check(WorldEvents.Hazard(MacroMapDatabase.Find("hospital")) == MacroMapDatabase.Find("hospital").BaseHazardModifier - 1, "escort lowers siege ward hazard");
      StoryAt("reactor_core", "The Last Gate");
      Check(!StorySystem.Has("ending"), "gate needs the control room");
      foreach (var e in EncounterState.Enemies) e.Hp = 0;
      StorySystem.OnKill();
      Check(StorySystem.Has("control_room"), "control room");
      StoryAt("reactor_core", "The Last Gate");
      Check(StorySystem.Act == 6 && p.MainStoryFinaleDone && p.Inventory.Items.Any(i => i.Id == "theBlackStar"), "ending reached");
      if (sponsor == "Garrison") Check(MacroMapDatabase.Zones.Max(z => z.Tier) == 5 && WorldEvents.ArtifactSearchBonus("perimeter") == -2, "destroy ending shrinks the zone");
      if (sponsor == "Alone") { p.ApplyArtifactStatBonuses(); Check(p.MaxHp == 110, "take it: +10 max hp, got " + p.MaxHp); }
      Console.WriteLine($"story ({sponsor}): ending '{MacroState.Current.Ending}', rank {p.Rank}");
    }
    GameBootstrap.StartNewGame();
    Check(MacroMapDatabase.Zones.Max(z => z.Tier) > 5, "new game restores zone tiers");
    GameLog.Target = null;
  }

  static InventoryItem Part(int tier) => new InventoryItem { Id = "mp_" + Guid.NewGuid().ToString("N").Substring(0, 6), Name = $"Mutant Part (Tier {tier})", Type = "mutantPart", Qty = 1, WeightKg = 2, Category = tier.ToString() };
  static void UpgradeChecks() {
    var log = new ConsoleLog(); GameLog.Target = log;
    GameBootstrap.StartNewGame(new CharacterSpec { Loadout = "Ex-Military" });
    var p = Player.Current;
    MacroState.Current.CurrentZoneId = "scavengers_hub";
    Check(Upgrades.TechnicianHere(), "repair yard");
    Check(!Upgrades.Buy(p, true, "Calibrated"), "no parts, no upgrade");
    p.AddItem(Part(1)); p.AddItem(Part(3)); p.AddItem(Part(3)); p.AddItem(Part(4)); p.AddItem(Part(1)); p.AddItem(Part(2));
    int ru = p.Ru; int mag = p.EquippedWeapon.MagCapacity;
    Check(Upgrades.Buy(p, true, "Extended Mag"), "upgrade 1");
    Check(p.EquippedWeapon.MagCapacity == mag * 3 / 2, $"extended mag {mag}->{p.EquippedWeapon.MagCapacity}");
    Check(p.Ru < ru && p.Inventory.Items.Count(i => i.Type == "mutantPart") == 5, "upgrade 1 paid with the cheapest part");
    Check(Upgrades.Buy(p, true, "Reinforced"), "upgrade 2 (two T3 parts)");
    Check(p.Inventory.Items.Count(i => i.Type == "mutantPart" && Upgrades.PartTier(i) == 3) == 0, "used T3 parts");
    Check(Upgrades.Buy(p, true, "Calibrated"), "upgrade 3 (T4 + two)");
    Check(!Upgrades.Buy(p, true, "Hardened"), "max 3 upgrades");
    // armor
    p.AddItem(Part(1));
    float dr = p.PhysDR;
    Check(Upgrades.Buy(p, false, "Plating") && Math.Abs(p.PhysDR - dr - 0.05f) < 0.001f, $"plating {dr}->{p.PhysDR}");
    // upgrades ride along through a swap and a save
    var save = p.CaptureSaveData();
    p.EquipWeaponFromInventory(p.Inventory.Items.First(i => i.Type == "weapon").Id);
    var aks = p.Inventory.Items.First(i => i.Key == "VKS74U");
    Check(Upgrades.Parse(aks.Upgrades).Count == 3, "upgrades kept on the stowed gun: " + aks.Upgrades);
    p.EquipWeaponFromInventory(aks.Id);
    Check(p.EquippedWeapon.Upgrades.Count == 3 && p.EquippedWeapon.MagCapacity == mag * 3 / 2, "upgrades back on re-equip");
    new Player(); Player.Current.ApplyLoadedData(save);
    Check(Player.Current.EquippedWeapon.Upgrades.Contains("Calibrated") && Player.Current.EquippedArmorUpgrades.Contains("Plating"), "upgrades saved");
    GameLog.Target = null;
  }

  static void LegendChecks() {
    var log = new ConsoleLog(); GameLog.Target = log;
    GameBootstrap.StartNewGame();
    var p = Player.Current;
    var def = MutantDatabase.Tiers["tier4"].First().Value;
    var m = new Mutant(def.Name, 5, 5, def); int hp = m.MaxHp; m.MakeNamed("Old Scar");
    Check(m.Named && m.MaxHp == (int)(hp * 1.5f) && m.AttackBonus == 2 && m.Name.StartsWith("Old Scar"), "named mutant stats");
    // the crypt guardian blocks Lucifer until it dies
    StoryAt("siege_catacombs", "Ossuary");
    var guard = EncounterState.Enemies.FirstOrDefault(e => e.Named);
    Check(guard != null, "crypt guardian spawned");
    p.Stats.S = 40;   // guarantee the DC 18 roll
    StoryAt("siege_catacombs", "Sealed Crypt");
    Check(!StorySystem.Has("lucifer"), "guardian blocks the crypt");
    var g2 = EncounterState.Enemies.First(e => e.Named);
    g2.Hp = 0; Weapon.OnTargetKilled(p, g2);
    Check(StorySystem.Has("named:siege_catacombs"), "guardian death remembered");
    int parts = p.Inventory.Items.Count(i => i.Type == "mutantPart");
    Check(parts == 2, "named drops 2 parts, got " + parts);
    var lp = TerrainMap.Landmarks.First(l => l.Name == "Sealed Crypt"); p.X = lp.X; p.Y = lp.Y; p.Ap = 5;
    StorySystem.TryInteract(p);
    Check(StorySystem.Has("lucifer") && p.Inventory.Items.Any(i => i.Id == "lucifer"), "lucifer taken");
    StoryAt("siege_catacombs", "Ossuary");
    Check(!EncounterState.Enemies.Any(e => e.Named), "guardian doesn't respawn");
    StoryAt("outskirts", "The Wound");
    Check(p.Inventory.Items.Any(i => i.Id == "heartOfTheWound"), "heart of the wound");
    // prisoners
    var (w1, a1) = EncounterGeneration.PickFactionLoadout("Wanderers", "Rookie", 1);
    var pr = new HumanEnemy("Prisoner", p.X + 1, p.Y, "Wanderers", "Rookie", w1, a1) { Morale = "Surrendered" };
    EnemyAI.LetGo(pr, p);
    Check(pr.IsFleeing() && p.Reputation.TryGetValue("Wanderers", out var rep) && rep == Player.RepPoint, "let go: flees, +rep");
    var pr2 = new HumanEnemy("Prisoner 2", p.X + 1, p.Y, "Raiders", "Rookie", w1, a1) { Morale = "Surrendered" };
    p.Stats.I = 40; int items = p.Inventory.Items.Count, ru = p.Ru, rumors = MacroState.Current.Rumors.Count;
    EnemyAI.Interrogate(pr2, p);
    Check(pr2.Interrogated && (p.Inventory.Items.Count > items || p.Ru > ru || MacroState.Current.Rumors.Count > rumors), "interrogation pays off");
    GameLog.Target = null;
  }

  static void QuestRuleChecks() {
    var log = new ConsoleLog(); GameLog.Target = log;
    var standalone = QuestDatabase.All.Where(q => q.Stage == 0 && q.Category == QuestCategory.Elimination).ToList();
    foreach (int comp in new[] { 1, 2, 3, 4, 5, 6 }) {
      GameBootstrap.StartNewGame();
      var p = Player.Current;
      var q = standalone[comp % standalone.Count];
      Check(QuestSystem.AcceptQuest(q.Id), "accept " + q.Id);
      var aq = p.ActiveQuests.Last();
      Check(aq.Complication >= 1 && aq.Complication <= 6, "rolled a complication");
      aq.Complication = comp;
      if (comp == 2) {
        MacroState.Current.CurrentZoneId = aq.ZoneId; QuestSystem.OnEnterZone(aq.ZoneId);
        int want = MacroMapDatabase.Find(aq.ZoneId).IsSafeZone ? 0 : 2;
        Check(EncounterState.Enemies.Count(e => e.Name.StartsWith("Rival")) == want, $"rival crew ({want})");
        Check(aq.TargetsSpawned && EncounterState.Enemies.Any(e => e.IsAlive()), "quest targets spawned");
        QuestSystem.OnEnterZone(aq.ZoneId); Check(EncounterState.Enemies.Count(e => e.Name.StartsWith("Rival")) == want, "rival only once");
      }
      if (comp == 5) MacroState.Current.Day += 5;
      int ru = p.Ru, rumors = MacroState.Current.Rumors.Count;
      foreach (var o in aq.Objectives) o.Done = true;
      QuestSystem.OnKill("Mutant", 1, aq.ZoneId);
      Check(!p.ActiveQuests.Contains(aq) && p.Ru > ru, $"complication {comp}: quest pays");
      Check(MacroState.Current.Rumors.Count > rumors || MacroState.Current.DiscoveredHiddenNodes.Count > 0, "standalone quest gives a true rumor");
    }
    Check(QuestSystem.ComplicationText(0).StartsWith("none"), "old saves read as no complication");
    // rule 3: both sides named, different factions; a stage quest pays +2 rep to its giver
    var arc = QuestDatabase.All.First(q => q.Stage == 1 && !string.IsNullOrEmpty(q.Faction));
    var (gv, alt) = QuestSystem.Sides(arc, arc.ZoneId ?? "perimeter");
    Check(gv != alt && gv == FactionDatabase.NormalizeFactionName(arc.Faction), $"sides {gv}/{alt}");
    GameBootstrap.StartNewGame();
    Check(QuestSystem.AcceptQuest(arc.Id), "accept arc");
    var aqa = Player.Current.ActiveQuests.Last(); aqa.Complication = 1;
    foreach (var o in aqa.Objectives) o.Done = true;
    QuestSystem.OnKill("Mutant", 1, aqa.ZoneId);
    Check(Player.Current.RepPoints(gv) == 2, "arc giver +2 rep, got " + Player.Current.RepPoints(gv));
    GameLog.Target = null;
  }

  static void ArtifactChecks() {
    var log = new ConsoleLog(); GameLog.Target = log;
    GameBootstrap.StartNewGame(new CharacterSpec { Loadout = "Ex-Military" });
    var p = Player.Current;
    p.EquipArmor("tier3", ArmorDatabase.Tiers["tier3"].First(kv => (kv.Value.BeltSlots) >= 3).Key);
    float anom0 = p.AnomDR;
    p.ArtifactSlots[0] = "driftBubble"; p.ArtifactSlots[1] = "ballast"; p.ApplyArtifactStatBonuses();
    Check(p.ArtifactCarryBonus == 15, "only the strongest gravitational counts: " + p.ArtifactCarryBonus);
    Check(Math.Abs(p.AnomDR - Math.Max(0, anom0 - 0.20f)) < 0.001f, $"both penalties apply: {anom0} -> {p.AnomDR}");
    p.ArtifactSlots[0] = "lucifer"; p.ArtifactSlots[1] = "theLodestone"; p.ApplyArtifactStatBonuses();
    Check(p.ArtifactAutoPassAnomalies && p.ArtifactApBonus == 1, "two legendaries both work");
    Check(p.MaxHp == 75, "lucifer max hp " + p.MaxHp);
    p.ArtifactSlots[0] = null; p.ArtifactSlots[1] = null; p.ApplyArtifactStatBonuses();
    Check(p.MaxHp == 100 && Math.Abs(p.AnomDR - anom0) < 0.001f, "everything restored after taking them off");
    GameLog.Target = null;
  }

  static void HazardChecks() {
    var log = new ConsoleLog(); GameLog.Target = log;
    GameBootstrap.StartNewGame();
    var hot = MacroMapDatabase.Zones.First(z => !z.IsSafeZone && z.BaseHazardModifier >= 4);
    MacroState.Current.CurrentZoneId = hot.Id; ZoneTravel.EnterZone(hot.Id);
    int before = EncounterState.Enemies.Count;
    Check(EncounterGeneration.CrossingHazardCheck(hot) && EncounterState.Enemies.Count > before, "hazard 4+ = automatic encounter");
    var safe = MacroMapDatabase.Zones.First(z => z.IsSafeZone);
    Check(!EncounterGeneration.CrossingHazardCheck(safe), "no encounters in safe zones");
    GameLog.Target = null;
  }

  // A long random playthrough: travel, fight, search, rest, shop - anything that throws is a bug.
  static bool args_soak;
  static void SoakTest() {
    var log = new ConsoleLog { Counts = new System.Collections.Generic.Dictionary<string, int>() }; GameLog.Target = log;
    var rng = new Random(7);
    int crossings = 0, fights = 0, kills = 0, restarts = 0;
    var why = new System.Collections.Generic.Dictionary<string, int>();
    var cap = new ConsoleLog(); 
    GameBootstrap.StartNewGame(new CharacterSpec { Loadout = "Ex-Military", Perk = "Sprinter" });
    string step = "";
    try {
      for (int i = 0; i < 400; i++) {
        var p = Player.Current;
        GameState.Current.Mode = GameMode.Tactical;
        if (GameState.Current.CurrentTurn == Turn.CombatOver) { restarts++; GameBootstrap.StartNewGame(); continue; }
        GameState.Current.CurrentTurn = Turn.Player;
        p.HealPermanent(100); p.Hunger = p.Thirst = p.Tiredness = 10; p.Inventory.AmmoReserve["Standard"] = 300;
        var zone = MacroMapDatabase.Find(MacroState.Current.CurrentZoneId);
        var options = zone.ConnectedZones.Concat(zone.HiddenConnections ?? new System.Collections.Generic.List<string>()).ToList();
        foreach (var h in zone.HiddenConnections ?? new System.Collections.Generic.List<string>()) if (!MacroState.Current.DiscoveredHiddenNodes.Contains(h)) MacroState.Current.DiscoveredHiddenNodes.Add(h);
        string target = options[rng.Next(options.Count)];
        step = $"travel {zone.Id} -> {target}";
        if (ZoneTravel.TravelToSector(target)) crossings++;
        // fight whatever is awake for a few rounds
        step = "fight in " + MacroState.Current.CurrentZoneId;
        for (int round = 0; round < 6; round++) {
          var t = EncounterState.Enemies.Where(e => e.IsAlive() && !(e is HumanEnemy hh && EnemyAI.IsOut(hh))).OrderBy(e => CombatantUtil.Distance(p, e)).FirstOrDefault();
          if (t == null) break;
          fights++;
          t.IsAggroed = true; EncounterState.InCombat = true;
          for (int k = 0; k < 3 && t.IsAlive(); k++) {
            if (p.EquippedWeapon.IsJammed) p.ClearWeaponJam();
            else if (!p.EquippedWeapon.IsLoaded) p.ReloadWeapon();
            else if (CombatantUtil.Distance(p, t) > p.EquippedWeapon.RangeTiles) { p.X = t.X - 2 < 0 ? t.X + 2 : t.X - 2; p.Y = t.Y; }
            else p.Attack(t, null);
          }
          if (!t.IsAlive()) kills++;
          TurnLoop.CheckCombatEnd();
          if (GameState.Current.CurrentTurn == Turn.Player) { p.HealPermanent(100); TurnLoop.PlayerEndTurn(); }
          if (GameState.Current.CurrentTurn == Turn.CombatOver) break;
        }
        if (GameState.Current.CurrentTurn == Turn.CombatOver) continue;
        GameState.Current.CurrentTurn = Turn.Player; p.Ap = p.MaxAp;
        step = "search/interact";
        var a = EncounterState.Anomalies.FirstOrDefault(); if (a != null) { a.Revealed = true; p.X = a.X; p.Y = a.Y; AnomalyEngine.TrySearchAnomaly(p); }
        p.Ap = p.MaxAp; InteractionEngine.AttemptInteract(p);
        var lm = TerrainMap.Landmarks.FirstOrDefault(); if (lm != null) { p.X = lm.X; p.Y = lm.Y; p.Ap = p.MaxAp; StorySystem.TryInteract(p); }
        if (i % 25 == 0) { step = "rest"; MacroState.Current.CurrentZoneId = "scavengers_hub"; ZoneTravel.EnterZone("scavengers_hub"); GameState.Current.CurrentTurn = Turn.Player; EncounterState.InCombat = false; WorldDays.Rest(); }
        if (i % 40 == 0) { step = "shop"; var it = ShopDatabase.Items.FirstOrDefault(x => x.Baseline); ShopSystem.BuyItem(it.Id); var sell = p.Inventory.Items.FirstOrDefault(x => x.Type != "key"); if (sell != null) ShopSystem.SellItem(sell.Id); }
        if (i % 60 == 0) { step = "save"; var d = p.CaptureSaveData(); new Player(); Player.Current.ApplyLoadedData(d); }
      }
    } catch (Exception e) { Check(false, $"soak test threw at '{step}': {e}"); }
    Console.WriteLine($"soak: {restarts} restarts");
    if (args_soak) foreach (var kv in log.Counts.OrderByDescending(kv => kv.Value).Take(40)) Console.WriteLine($"   {kv.Value,6} {kv.Key}");
    Console.WriteLine($"soak: {crossings} crossings, {fights} fight rounds, {kills} kills, day {MacroState.Current.Day}, rank {Player.Current.Rank}, {log.Lines} log lines");
    GameLog.Target = null;
  }

  static void ReviewFixChecks() {
    var log = new ConsoleLog(); GameLog.Target = log;
    GameBootstrap.StartNewGame();   // Scavenger: PSh-18 Pchela loaded, knife in the pack, 30 in the stash
    var p = Player.Current;
    int stash = p.Inventory.AmmoReserve["Standard"], mag = p.EquippedWeapon.CurrentMagazine;
    var knife = p.Inventory.Items.First(i => i.Key == "HuntingKnife");
    p.EquipWeaponFromInventory(knife.Id);
    Check(p.Inventory.AmmoReserve["Standard"] == stash + mag, "swapping out unloads into the stash");
    p.EquipWeaponFromInventory(p.Inventory.Items.First(i => i.Key == "PSh18Pchela").Id);
    Check(p.EquippedWeapon.CurrentMagazine == 0, "a gun from the pack comes out empty (no free reload)");
    GameState.Current.Mode = GameMode.Tactical; GameState.Current.CurrentTurn = Turn.Player; p.Ap = 4;
    p.ReloadWeapon(); p.EquippedWeapon.CurrentMagazine -= 3;
    var d = p.CaptureSaveData(); new Player(); Player.Current.ApplyLoadedData(d);
    Check(Player.Current.EquippedWeapon.CurrentMagazine == p.EquippedWeapon.CurrentMagazine, "magazine saved");
    p = Player.Current;
    // armor mods that don't fit go back to the pack
    var mods = ArmorModDatabase.All.Keys.Take(2).ToArray();
    p.EquipArmor("tier3", ArmorDatabase.Tiers["tier3"].First(kv => kv.Value.ModSlots >= 2).Key);
    p.ArmorMods.SlotA = mods[0]; p.ArmorMods.SlotB = mods[1];
    var one = ShopDatabase.Items.First(i => i.Type == "armor" && ArmorDatabase.Get(i.Category, i.Key)?.ModSlots == 1);
    p.AddItem(new InventoryItem { Id = one.Id, Name = one.Name, Type = "armor", Category = one.Category, Key = one.Key, Qty = 1 });
    p.EquipArmorFromInventory(one.Id);
    Check(p.ArmorMods.SlotB == null && p.Inventory.Items.Any(i => i.Id == mods[1]), "extra mod returned");
    // Lucifer costs real HP; legendaries keep their value off the belt
    p.HealPermanent(100); p.ArtifactSlots[0] = "lucifer"; p.ApplyArtifactStatBonuses();
    Check(p.Hp == 75 && p.PermHP == 75, "lucifer clamps hp " + p.Hp);
    p.UnequipArtifact(0);
    Check(p.Inventory.Items.First(i => i.Id == "lucifer").Price >= 20000, "legendary keeps its value");
    // two copies with different condition get their own ids
    var pmm = ShopDatabase.Items.First(i => i.Type == "weapon" && i.Key == "PSh18Pchela");
    p.AddItem(new InventoryItem { Id = pmm.Id, Name = pmm.Name, Type = "weapon", Category = "sidearm", Key = "PSh18Pchela", Qty = 1, Condition = "Damaged" });
    p.AddItem(new InventoryItem { Id = pmm.Id, Name = pmm.Name, Type = "weapon", Category = "sidearm", Key = "PSh18Pchela", Qty = 1 });
    var ids = p.Inventory.Items.Where(i => i.Key == "PSh18Pchela").Select(i => i.Id).ToList();
    Check(ids.Count == 2 && ids.Distinct().Count() == 2, "distinct copies, distinct ids: " + string.Join(",", ids));
    // finding a hidden node completes the quest tied to it
    var hq = QuestDatabase.All.FirstOrDefault(q => q.HiddenNodeId != null && q.Stage == 0);
    if (hq != null) {
      QuestSystem.AcceptQuest(hq.Id);
      var aqh = p.ActiveQuests.First(a => a.QuestId == hq.Id);
      foreach (var o in aqh.Objectives.Where(o => o.Kind != "SkillCheck")) o.Done = true;
      QuestSystem.OnHiddenNodeDiscovered(hq.HiddenNodeId);
      Check(!p.ActiveQuests.Contains(aqh), "hidden-node quest completes on discovery");
    }
    GameLog.Target = null;
  }

  static void AiFixChecks() {
    var log = new ConsoleLog(); GameLog.Target = log;
    GameBootstrap.StartNewGame();
    var p = Player.Current;
    EncounterState.Enemies.Clear();
    var (w, a) = EncounterGeneration.PickFactionLoadout("Raiders", "Rookie", 1);
    var asleep = new HumanEnemy("Asleep", p.X + 8, p.Y, "Raiders", "Rookie", w, a);
    EncounterState.Enemies.Add(asleep);
    Check(!EncounterState.AnyActive(), "an unaware squad doesn't keep a fight going");
    asleep.IsAggroed = true; Check(EncounterState.AnyActive(), "an aware one does");
    var stack = new HumanEnemy("Stacked", asleep.X, asleep.Y, "Raiders", "Rookie", w, a);
    Check(stack.X != asleep.X || stack.Y != asleep.Y, "no spawning on top of each other");
    var mdef = MutantDatabase.Tiers["tier1"]["BlindMaw"];
    var m1 = new Mutant("Maw", p.X + 3, p.Y, mdef); EncounterState.Enemies.Add(m1);
    var h2 = new HumanEnemy("Next to it", m1.X, m1.Y + 1, "Raiders", "Rookie", w, a); h2.X = m1.X; h2.Y = m1.Y + 1; EncounterState.Enemies.Add(h2);
    Check(!m1.PackTactics, "a human next to a mutant isn't its pack");
    var m2 = new Mutant("Maw 2", m1.X + 1, m1.Y, mdef); m2.X = m1.X + 1; m2.Y = m1.Y; EncounterState.Enemies.Add(m2);
    Check(m1.PackTactics, "a packmate is");
    GameLog.Target = null;
  }

  static void ViewChecks() {
    { // floating hands (HeldWeapon.cs): reach, waist clamp, pistol cup, mirroring when aiming left
      var nan = float.NaN;
      var r = HeldPoseMath.Compute("sidearm", 0f, 10.5f, 11.5f, nan, nan);
      Check(r.GripX == 12 && r.GripY == 10 && r.HasSupport && r.SupportX == 13 && r.SupportY == 11 && !r.AimLeft, $"pistol right ({r.GripX},{r.GripY}) cup ({r.SupportX},{r.SupportY})");
      var l = HeldPoseMath.Compute("sidearm", 180f, 10.5f, 11.5f, nan, nan);
      Check(l.AimLeft && l.GripX == 1 && l.GripY == 10 && l.SupportX == 0 && l.SupportY == 11, $"pistol left mirrors ({l.GripX},{l.GripY}) cup ({l.SupportX},{l.SupportY})");
      var dn = HeldPoseMath.Compute("sidearm", -90f, 10.5f, 11.5f, nan, nan);
      Check(dn.GripY == 10 && dn.SupportY <= 11, $"aiming down stays at the waist ({dn.GripY},{dn.SupportY})");
      var up = HeldPoseMath.Compute("assault", 60f, 12.5f, 10f, 22f, 8.5f);
      Check(up.GripY < 10 && up.SupportY < up.GripY, $"aiming up raises both hands ({up.GripY},{up.SupportY})");
      var ar = HeldPoseMath.Compute("assault", 0f, 12.5f, 10f, 22f, 8.5f);
      Check(ar.GripX == 11 && ar.SupportX == 16 && ar.SupportY == 9, $"rifle shouldered, support on the handguard ({ar.GripX}; {ar.SupportX},{ar.SupportY})");
      Check(!HeldPoseMath.Compute("melee", 0f, 7f, 7f, nan, nan).HasSupport, "knife is one-handed");
      Check(HeldPoseMath.Reach("smg") == 3.5f && HeldPoseMath.Reach("heavy") == 3f, "reach per weapon type");
      Check(SpriteAtlasIndex.ArmorCuff.Length == SpriteAtlasIndex.Armor.Length && SpriteAtlasIndex.ArmorGlove.Length == SpriteAtlasIndex.Armor.Length, "hand colours for every armor");
      // Batch 28: big 5x4 gloves when empty-handed, small 4x3 while holding something; rest spots clear the torso
      Check(HeldWeaponView.GloveBig.Length == 4 && HeldWeaponView.GloveBig[0].Length == 5 && HeldWeaponView.GloveSmall.Length == 3 && HeldWeaponView.GloveSmall[0].Length == 4, "two glove sizes");
      Check(HeldWeaponView.RestRightX - 2.5f >= 11f && HeldWeaponView.RestLeftX + 2.5f <= 5f, "empty hands float beside the torso");
      float[] mk = { 5.5f, 9f, float.NaN, float.NaN };   // WeaponArt.Grips["PSh18Pchela"]
      var pk = HeldPoseMath.Compute("sidearm", 0f, mk[0], mk[1], mk[2], mk[3]);
      Check(pk.HasSupport && pk.SupportX == pk.GripX + 1 && pk.SupportY == pk.GripY + 1, "pistol still cupped with the v2 grip");
    }
    Check(NameMigration.Apply("{\"Name\":\"PMm\",\"Key\":\"SEVA Suit\",\"Note\":\"PMm ammo\"}") == "{\"Name\":\"PSh-18 Pchela\",\"Key\":\"Kokon Suit\",\"Note\":\"PMm ammo\"}", "old save names migrate");
    Check(NameMigration.Apply("{\"EquippedWeaponKey\":\"PMm\",\"Name\":\"PMm\",\"Key\":\"ColtM1911\",\"SurgeZoneId\":\"red_forest\",\"EmissionRounds\":2}") == "{\"EquippedWeaponKey\":\"PSh18Pchela\",\"Name\":\"PSh-18 Pchela\",\"Key\":\"Halden45Service\",\"SurgeZoneId\":\"crimson_woods\",\"SurgeRounds\":2}", "old save keys migrate");
    var log = new ConsoleLog(); GameLog.Target = log;
    GameBootstrap.StartNewGame();
    Check(StorySystem.IsObjective("secret_lab_alpha", "Reception Airlock") && !StorySystem.IsObjective("secret_lab_alpha", "Sample Storage"), "act 1 objective marker");
    Check(StorySystem.IsObjective("outskirts", "The Wound"), "legend objective marker");
    var p = Player.Current;
    var (w, a) = EncounterGeneration.PickFactionLoadout("Raiders", "Rookie", 1);
    var e = new HumanEnemy("Hover Me", p.X + 3, p.Y, "Raiders", "Rookie", w, a); EncounterState.Enemies.Add(e);
    string d = TacticalGridView.DescribeTile(e.X, e.Y, p);
    Check(d.Contains("Hover Me") && d.Contains("HP"), "hover text: " + d);
    foreach (var lm in TerrainMap.Landmarks) Check(TacticalGridView.DescribeTile(lm.X, lm.Y, p).Length > 0 || EncounterState.Enemies.Any(en => en.X == lm.X && en.Y == lm.Y), "landmark hover " + lm.Name);
    CombatFeedbackChecks(p, e);
    GameLog.Target = null;
  }

  static void CombatFeedbackChecks(Player p, HumanEnemy e) {
    WeaponDef W(string c, string k) => WeaponDatabase.Categories[c][k];
    var pistol = WeaponFeel.For(W("sidearm", "PSh18Pchela")); var kuvalda = WeaponFeel.For(W("sidearm", "RK12Kuvalda"));
    Check(kuvalda.Kick >= pistol.Kick * 3 && kuvalda.Climb > pistol.Climb * 3, $"hand cannon kicks harder ({kuvalda.Kick} vs {pistol.Kick})");
    Check(WeaponFeel.For(W("sidearm", "SteppeEagle")).Climb > WeaponFeel.For(W("sidearm", "WaldenP9")).Climb * 1.5f, "high recoil climbs more");
    Check(WeaponFeel.For(W("sniper", "VSNShepot")).Flash == 0 && WeaponFeel.For(W("assault", "TS9Tishina")).Flash == 0 && WeaponFeel.For(W("sniper", "VSNShepot")).Tracer < 1f, "suppressed: no flash, faint tracer");
    Check(WeaponFeel.For(W("sniper", "HaldenAM50Sledge")).Flash == 4 && WeaponFeel.For(W("sniper", "G7RailPrototype")).Rail, "anti-materiel and rail overrides");
    Check(WeaponFeel.For(W("sniper", "HaldenAM50Sledge")).Kick > WeaponFeel.For(W("sniper", "SKNKarabin")).Kick, "AM-50 out-kicks the carbine");
    Check(WeaponFeel.For(W("shotgun", "Obrez66")).Reload == "break" && WeaponFeel.For(W("shotgun", "Hound13")).Reload == "tube" && WeaponFeel.For(W("shotgun", "Sayan12")).Reload == "mag"
      && WeaponFeel.For(W("shotgun", "Tambour12")).Reload == "cylinder" && WeaponFeel.For(W("heavy", "CorvanLMG")).Reload == "belt" && WeaponFeel.For(W("sidearm", "HaldenServiceRevolver")).Reload == "cylinder", "reload styles");
    foreach (var cat in WeaponDatabase.Categories) foreach (var kv in cat.Value) {
      var f = WeaponFeel.For(kv.Value);
      if (cat.Key == "melee") Check(f.Reload == "none", "melee doesn't reload " + kv.Key);
      else Check(f.ReloadTime > 0.3f && f.ReloadTime < 3.5f && f.Flash >= 0 && f.Flash <= 4 && f.Kick > 0f, $"feel {kv.Key}: reload {f.ReloadTime}, flash {f.Flash}, kick {f.Kick}");
    }
    // curves
    Check(HeldPoseMath.Recoil(0f, 0.2f) == 1f && HeldPoseMath.Recoil(0.2f, 0.2f) == 0f && Math.Abs(HeldPoseMath.Recoil(0.1f, 0.2f) - 0.25f) < 0.001f, "recoil eases back");
    Check(HeldPoseMath.Swing(0f).deg == 70f && HeldPoseMath.Swing(HeldPoseMath.SwingTime).deg == 0f, "swing winds up then ends at rest");
    var mid = HeldPoseMath.Reload(0.5f, "mag", 1); var end = HeldPoseMath.Reload(1f, "mag", 1);
    Check(mid.tip == 1f && mid.pouch == 1f && end.tip == 0f && end.pouch == 0f, $"mag reload: hand at the pouch mid-way ({mid.tip},{mid.pouch})");
    int trips = 0; float prev = 0f;
    for (float r = 0f; r < 1f; r += 0.005f) { var s = HeldPoseMath.Reload(r, "tube", 3); if (s.pouch >= 0.99f && prev < 0.99f) trips++; prev = s.pouch; }
    Check(trips == 3, "tube reload makes one trip per shell batch: " + trips);
    // the rules record what happened for the view to play back
    CombatFx.Pending.Clear();
    e.Hp = e.MaxHp; p.EquippedWeapon.IsJammed = false; p.EquippedWeapon.CurrentMagazine = p.EquippedWeapon.MagCapacity;
    int hp = e.Hp; p.EquippedWeapon.Fire(e, "Single");
    var shot = CombatFx.Pending.LastOrDefault(x => x.Kind == "shot");
    Check(shot != null && shot.Actor == p && shot.Target == e && shot.Rounds == 1 && shot.Hit == (e.Hp < hp), $"shot recorded (hit {shot?.Hit}, hp {hp}->{e.Hp})");
    p.EquippedWeapon.Reload();
    Check(CombatFx.Pending.Any(x => x.Kind == "reload" && x.Actor == p), "reload recorded");
    for (int i = 0; i < 100; i++) CombatFx.Push("shot", p, e);
    Check(CombatFx.Pending.Count <= 64, "fx queue is capped without a view");
    CombatFx.Pending.Clear();
    // Batch 24: killing blows are flagged with their overkill
    FxEvent killShot = null;
    for (int i = 0; i < 80 && killShot == null; i++) {
      e.Hp = 1; p.EquippedWeapon.IsJammed = false; p.EquippedWeapon.CurrentMagazine = p.EquippedWeapon.MagCapacity;
      CombatFx.Pending.Clear(); p.EquippedWeapon.Fire(e, "Single");
      killShot = CombatFx.Pending.FirstOrDefault(x => x.Kind == "shot" && x.Hit);
    }
    Check(killShot != null && killShot.Killed && killShot.Overkill > 0, $"killing shot flagged (overkill {killShot?.Overkill})");
    e.Hp = e.MaxHp; CombatFx.Pending.Clear();
    // walking follows the recorded route at walking pace
    Check(HeldPoseMath.WalkFrame(0f) == 0 && HeldPoseMath.WalkFrame(0.3f) == -1 && HeldPoseMath.WalkFrame(0.55f) == 1 && HeldPoseMath.WalkFrame(0.8f) == -1, "walk cycle: left, stand, right, stand");
    var mv = new UnitMotionView(null, null);
    int sx = e.X, sy = e.Y;
    var at0 = mv.Place(e, TacticalGridView.GridToWorld(sx, sy));
    Check(at0.x == TacticalGridView.GridToWorld(sx, sy).x && !mv.IsMoving(e), "first sight: drawn on its tile");
    e.X = sx + 1; CombatFx.Step(e, sx + 1, sy); e.Y = sy + 1; CombatFx.Step(e, sx + 1, sy + 1);
    mv.DrainSteps();
    UnityEngine.Time.deltaTime = 0.1f;
    var wp = mv.Place(e, TacticalGridView.GridToWorld(e.X, e.Y));
    Check(mv.IsMoving(e) && Math.Abs(wp.x - (TacticalGridView.GridToWorld(sx, sy).x + UnitMotionView.WalkSpeed * 0.1f)) < 0.07f && Math.Abs(wp.y - TacticalGridView.GridToWorld(sx, sy).y) < 0.07f, $"walks along the route, not diagonally ({wp.x},{wp.y})");
    for (int i = 0; i < 20; i++) wp = mv.Place(e, TacticalGridView.GridToWorld(e.X, e.Y));
    var goal = TacticalGridView.GridToWorld(e.X, e.Y);
    Check(!mv.IsMoving(e) && Math.Abs(wp.x - goal.x) < 0.01f && Math.Abs(wp.y - goal.y) < 0.01f, "arrives and stops");
    e.X = sx + 9; wp = mv.Place(e, TacticalGridView.GridToWorld(e.X, e.Y));
    Check(Math.Abs(wp.x - TacticalGridView.GridToWorld(e.X, e.Y).x) < 0.01f, "a long teleport snaps instead of sliding");
    e.X = sx; e.Y = sy; UnityEngine.Time.deltaTime = 0f;
    for (int i = 0; i < 25; i++) mv.Bleed(e);
    Check(mv.StainCount(e) > 0 && mv.StainCount(e) <= 10, "hits stain the body, capped: " + mv.StainCount(e));
    foreach (var cat in WeaponDatabase.Categories) foreach (var kv in cat.Value) Check(WeaponArt.HasGrip(kv.Key), "hand points for " + kv.Key);
    var drawn = new System.Collections.Generic.HashSet<string> { "pyre", "livewire", "acid_bloom", "spindle", "radiation" };   // ArtSource/make_anomalies.py
    foreach (var an in AnomalyDatabase.All.Values) Check(drawn.Contains(TacticalGridView.AnomalyArtKey(an.AnomalyType)), "anomaly art for " + an.AnomalyType);
  }
}
