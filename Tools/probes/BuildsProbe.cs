// Batch 75b: which builds are strongest? Each build = a weapon category, its best gun at the tier, its two category perks
// (one from Tier 3, both from Tier 5), the tier's mid armor and medkits. "screen" ranks them all; "full" runs the picked ones.
using System; using System.Linq; using System.Collections.Generic; using GameCore; using GameData;
static class ProbeMain {
  class Quiet : IGameLogTarget { public void AppendLine(string s) { if (Environment.GetEnvironmentVariable("PROBE_LOG") == "1") Console.WriteLine("  | " + s); } }
  static bool Out(Mutant e) => !e.IsAlive() || (e is HumanEnemy hh && EnemyAI.IsOut(hh));
  static readonly string[] Rank = { "", "Rookie", "Experienced", "Professional", "Veteran", "Expert" };
  class B { public string Name, Cat; public string[] Perks; public bool Burst, Dual; }
  static readonly B[] Builds = {
    new B { Name = "Gunslinger pistols", Cat = "sidearm", Perks = new[] { "PointBlankExecution", "Gunslinger", "QuickHands" }, Dual = true },   // Batch 80: Gunslinger needs Point-Blank first
    new B { Name = "Double Tap pistol", Cat = "sidearm", Perks = new[] { "DoubleTap", "PointBlankExecution", "AdrenalineSurge" } },
    new B { Name = "SMG bursts", Cat = "smg", Perks = new[] { "BulletHose", "HitAndRun", "AdrenalineSurge" }, Burst = true },
    new B { Name = "Shotgun", Cat = "shotgun", Perks = new[] { "Breacher", "BuckshotSpread", "AdrenalineSurge" } },
    new B { Name = "Rifle single", Cat = "assault", Perks = new[] { "ControlledTaps", "RecoilControl", "AdrenalineSurge" } },
    new B { Name = "Rifle bursts", Cat = "assault", Perks = new[] { "RecoilControl", "ControlledTaps", "AdrenalineSurge" }, Burst = true },
    new B { Name = "Sniper", Cat = "sniper", Perks = new[] { "MarksmansFocus", "HighVelocityImpact", "AdrenalineSurge" } },
    new B { Name = "Machine gun", Cat = "heavy", Perks = new[] { "Shredder", "WalkingTank", "AdrenalineSurge" }, Burst = true },
    new B { Name = "Melee", Cat = "melee", Perks = new[] { "BullRush", "BladeDancer", "AdrenalineSurge" } },
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
  static (string cat, string key) Gun(B b, int t) {
    var best = WeaponDatabase.Categories[b.Cat].Where(kv => kv.Value.Tier <= t && kv.Key != "G7RailPrototype")   // Batch 79: the G-7 runs on rare crafted ammo, outside the targets
                .OrderByDescending(kv => kv.Value.Tier).ThenByDescending(kv => kv.Value.BaseDamage).FirstOrDefault();
    if (best.Key != null) return (b.Cat, best.Key);
    // no gun of this kind yet at this tier (heavy before Tier 3): the best rifle
    var r = WeaponDatabase.Categories["assault"].Where(kv => kv.Value.Tier <= t).OrderByDescending(kv => kv.Value.Tier).ThenByDescending(kv => kv.Value.BaseDamage).First();
    return ("assault", r.Key);
  }
  // the build's kit at a tier (Batch 89: shared by the raid probe)
  static string StartLoadout;   // Batch 127: "start <loadout>" fights with exactly what New Game hands you (CharacterUI loadouts)
  static Player Kit(B b, int tier, string medkit, int apBonus) {
    if (StartLoadout != null) {
      GameLog.Target = new Quiet(); CombatRules.PlayerApBonus = 0;
      GameBootstrap.StartNewGame(new CharacterSpec { Loadout = StartLoadout });
      GameState.Current.Mode = GameMode.Tactical; GameState.Current.CurrentTurn = Turn.Player;
      var sp = Player.Current;   // what-ifs (env): PHP=<max HP>, PAP=<extra AP>, PDMG=<damage x>, PARMOR=<tier>:<key>
      if (Environment.GetEnvironmentVariable("PHP") is string hp) { sp.MaxHp = int.Parse(hp); sp.Hp = sp.MaxHp; }
      if (Environment.GetEnvironmentVariable("PAP") is string ap) CombatRules.PlayerApBonus = int.Parse(ap);
      if (Environment.GetEnvironmentVariable("PDMG") is string dm) CombatRules.PlayerDamage = float.Parse(dm, System.Globalization.CultureInfo.InvariantCulture);
      if (Environment.GetEnvironmentVariable("PARMOR") is string ar) { var parts = ar.Split(':'); sp.EquipArmor(parts[0], parts[1]); sp.ApplyCarryEffects(); }
      return sp;
    }
    var perks = b.Perks.Take(tier >= 5 ? 3 : tier >= 3 ? 2 : 1).ToArray();
    var (cat, key) = Gun(b, tier);
      GameLog.Target = new Quiet(); CombatRules.PlayerApBonus = 0;
      GameBootstrap.StartNewGame(); CombatRules.PlayerApBonus = apBonus;
      GameState.Current.Mode = GameMode.Tactical; GameState.Current.CurrentTurn = Turn.Player;
      var p = Player.Current; p.Rank = Rank[tier];
      p.EquipArmor("tier" + tier, MidArmor(tier));
      p.EquipWeapon(cat, key);
      foreach (var perk in perks) if (Environment.GetEnvironmentVariable("KIT_NOPERK") != "1") p.ActivePerks.Add(perk);
      if (Environment.GetEnvironmentVariable("KIT_ARMOR") is string ka) { var kp = ka.Split(':'); p.EquipArmor(kp[0], kp[1]); }   // Batch 127: what-ifs between the build kit and the New Game kit
      if (b.Dual && perks.Contains("Gunslinger") && cat == "sidearm") { p.OffhandWeapon = p.MakeWeapon("sidearm", key); p.OffhandKey = key; p.OffhandWeapon.CurrentMagazine = p.OffhandWeapon.MagCapacity; }
      p.SecondaryWeapon = null; p.SecondaryCategory = p.SecondaryKey = null;   // Batch 80: no starting knife in the second slot
      if (cat == "sniper") { var (sc, sk) = Gun(new B { Cat = "sidearm" }, tier); p.SecondaryWeapon = p.MakeWeapon(sc, sk); p.SecondaryCategory = sc; p.SecondaryKey = sk; }   // Batch 80: a pistol on the side
      p.FireMode = b.Burst && CombatRules.CanBurst(p.EquippedWeapon) ? "Burst" : "Single";
      p.EquippedWeapon.CurrentMagazine = p.EquippedWeapon.MagCapacity;
      Ammo.Add(p.Inventory, Ammo.Key(p.EquippedWeapon.Db, "Standard"), int.Parse(Environment.GetEnvironmentVariable("KIT_AMMO") ?? "400"));   // Batch 80: rounds per class and tier
      if (p.SecondaryWeapon != null) Ammo.Add(p.Inventory, Ammo.Key(p.SecondaryWeapon.Db, "Standard"), 400);
      p.Inventory.Items.Add(new InventoryItem { Id = medkit, Name = medkit, Type = "consumable", Qty = int.Parse(Environment.GetEnvironmentVariable("KIT_MEDS") ?? "3"), WeightKg = 0.3f });
    return p;
  }
  // Batch 89: the Garrison raid on the real Tier 6 map. The bot starts in the trenches with its Tier 5 kit, walks at the nearest
  // Garrison soldier and opens fire; GarrisonRaid (called from TurnLoop.PlayerEndTurn) sends the waves. A win = the armory opens.
  static void Raid(B b, int n) {
    int wins = 0, deaths = 0, stalls = 0, empty = 0; double rounds = 0, kills = 0, wave1 = 0, sent = 0, hpLeft = 0;
    for (int f = 0; f < n; f++) {
      var p = Kit(b, 5, "scientificMedkit", 0);
      if (Gear) {   // Batch 89b: a late-game loadout - 2 armor mods and 3 artifacts
        p.ArmorMods.SlotA = "CeramicPlates"; p.ArmorMods.SlotB = "ArtifactPouch"; p.ApplyArmorModsForPlayer();
        p.ArtifactSlots[0] = "lanternSeed"; p.ArtifactSlots[1] = "frostLattice"; p.ArtifactSlots[2] = "rosary"; p.ApplyArtifactStatBonuses(); p.ApplyCarryEffects();
        if (f0Note++ == 0) Console.WriteLine($"  (kit: Phys-DR {p.PhysDR:P0}, regen {p.ArtifactRegen}/turn, MP {p.MaxMp}, belt {p.TotalBeltSlots})");
      }
      var m = MacroState.Current; m.CurrentZoneId = "hospital"; ZoneTravel.EnterZone("hospital");
      GameState.Current.Mode = GameMode.Tactical; GameState.Current.CurrentTurn = Turn.Player;
      var start = TerrainMap.Landmarks.First(l => l.Name == "The Trenches"); var sp = TerrainMap.FindNearestEnterable(start.X, start.Y); p.X = sp.x; p.Y = sp.y;
      bool Garrison(Mutant e) => e is HumanEnemy h && h.FactionName == "Garrison";
      bool Foe(Mutant e) => !Out(e) && !Companion.Is(e) && (Garrison(e) && TerrainMap.RegionAt(e.X, e.Y) == "hospital" || e.IsAggroed && !EnemyAI.FriendlyToPlayer(e));
      wave1 += EncounterState.Enemies.Count(e => Garrison(e) && !Out(e) && m.CurrentZoneId == TerrainMap.RegionAt(e.X, e.Y));
      int before = EncounterState.Enemies.Count; var seen = new HashSet<Mutant>(EncounterState.Enemies);
      p.StartTurn(); p.HealPermanent(200);
      int r = 0; bool everStarted = false;
      for (; r < 120 && p.IsAlive() && !m.GarrisonArmoryOpen; r++) {
        bool ended = false; int shots = 0;
        for (int k = 0; k < 12 && GameState.Current.CurrentTurn == Turn.Player && (p.Ap > 0 || p.Mp > 0); k++) {
          var w = p.EquippedWeapon; bool melee = CombatRules.IsMelee(w);
          if (p.Hp < 45 && p.Ap >= 2 && p.Inventory.Items.Any(i => i.Id == "scientificMedkit" && i.Qty > 0)) { p.UseConsumable("scientificMedkit"); continue; }
          if (w.IsJammed) { if (p.Ap < 1) break; p.ClearWeaponJam(); continue; }
          if (!melee && (!w.IsLoaded || (p.FireMode == "Burst" && w.CurrentMagazine < CombatRules.BurstRounds(w) * (CombatRules.Category(w.Db) == "heavy" && p.ActivePerks.Contains("Shredder") ? 2 : 1)))) { int ap0 = p.Ap; p.ReloadWeapon(); if (p.Ap == ap0) break; continue; }
          var foes = EncounterState.Enemies.Where(Foe).ToList();
          var near = foes.OrderBy(e => CombatantUtil.Distance(p, e)).FirstOrDefault();
          if (near == null) break;
          var t = foes.Where(e => CombatantUtil.Distance(p, e) <= w.MaxReach && GameServices.RequireGrid().CheckLineOfSight(p.X, p.Y, e.X, e.Y)).OrderBy(e => e.Hp).FirstOrDefault();
          if (t == null) {   // walk there the way a player clicks a route (A*), round walls and through doors
            if (p.Mp <= 0) break;
            var grid = GameServices.RequireGrid();
            var goal = TerrainMap.GetAdjacentCoords(near.X, near.Y).Where(c => grid.CanEnterTile(c.x, c.y) && TerrainMap.UnitAt(c.x, c.y, p) == null)
              .OrderBy(c => Math.Max(Math.Abs(c.x - p.X), Math.Abs(c.y - p.Y))).Select(c => ((int x, int y)?)c).FirstOrDefault() ?? (near.X, near.Y);   // a free tile next to it (its own tile is taken)
            var path = grid.FindPath(p.X, p.Y, goal.x, goal.y, p);
            if (path.Count < 2 || !grid.AttemptMove(p, path[1].x, path[1].y)) break;
            continue;
          }
          if (!melee && shots >= 1 && foes.Count(e => e.IsAggroed) >= 2 && p.Hp < 50 && p.CanHunker(out _)) { p.HunkerDown(); ended = true; break; }
          if (p.Ap < 1) break;
          int ap1 = p.Ap; t.IsAggroed = true; p.Attack(t, null); EncounterState.InCombat = true; shots++;
          if (p.Ap == ap1) break;
        }
        TurnLoop.CheckCombatEnd();
        if (!ended && GameState.Current.CurrentTurn == Turn.Player) TurnLoop.PlayerEndTurn();
        foreach (var e in EncounterState.Enemies) if (seen.Add(e) && Garrison(e)) sent++;
        everStarted |= GarrisonRaid.Active;
      }
      rounds += r; hpLeft += m.GarrisonArmoryOpen ? p.Hp : 0;
      kills += EncounterState.Enemies.Count(e => Garrison(e) && !e.IsAlive());
      if (!p.IsAlive()) deaths++; else if (m.GarrisonArmoryOpen) wins++; else if (!everStarted) empty++; else stalls++;
      if (Environment.GetEnvironmentVariable("RAID_DEBUG") == "1" && p.IsAlive() && !m.GarrisonArmoryOpen)
        Console.WriteLine($"    stalled: raid active {GarrisonRaid.Active}, raid soldiers left {GarrisonRaid.Left}, you at {p.X},{p.Y} HP {p.Hp}; foes: " + string.Join(" ", EncounterState.Enemies.Where(Foe).Take(8).Select(e => $"{e.X},{e.Y}{(e.IsAggroed ? "!" : "")}{(e is HumanEnemy hh ? hh.Morale[0].ToString() : "")} d{CombatantUtil.Distance(p, e)}")));
      GarrisonRaid.Reset();
    }
    Console.WriteLine($"  {b.Name,-20} raid: armory {wins * 100 / n,3}%  died {deaths * 100 / n,3}%  unfinished {stalls * 100 / n,3}%  no Garrison at the base {empty * 100 / n,3}%  | {rounds / n:F0} rounds, guards at the start {wave1 / n:F1}, sent in waves {sent / n:F1}, Garrison killed {kills / n:F1}, HP left when won {hpLeft / Math.Max(1, wins):F0}");
  }
  static (int win, int die) Run(B b, int tier, string zone, string faction, int size, bool ambush, int apBonus, int n) {
    int wins = 0, deaths = 0;
    string medkit = tier <= 2 ? "basicMedkit" : tier <= 4 ? "militaryMedkit" : "scientificMedkit";
    var perks = b.Perks.Take(tier >= 5 ? 3 : tier >= 3 ? 2 : 1).ToArray();   // Batch 78: the starting perk, then Professional (T3) and Expert (T5)
    var (cat, key) = Gun(b, tier);
    for (int f = 0; f < n; f++) {
      var p = Kit(b, tier, medkit, apBonus);
      EncounterState.Enemies.Clear(); MacroState.Current.OfflineSquads.Clear(); EncounterState.InCombat = !ambush; EncounterState.ResetFight();
      for (int y = p.Y - 7; y <= p.Y + 7; y++) for (int x = p.X - 3; x <= p.X + 14; x++) if (x > 0 && y > 0 && x < TerrainMap.Width - 1 && y < TerrainMap.Height - 1) TerrainMap.SetTileType(x, y, TileType.Empty);
      if (!NoCover) foreach (var (dx, dy) in new[] { (1, 0), (1, -1), (1, 1), (0, -1), (-1, -1) }) TerrainMap.SetTileType(p.X + dx, p.Y + dy, TileType.HardCover);
      // Batch 76: only squads whose leader belongs in this tier stay here (the rest move to their own tier)
      OfflineSquad sq = null;
      for (int tries = 0; tries < 30; tries++) {
        MacroState.Current.OfflineSquads.Clear(); FactionDeck.Reset();
        int want = size > 0 ? size : SquadManager.HumanSquadSize(faction, tier);
        sq = SquadManager.CreateOfflineSquad(zone, faction, want);
        if (sq != null && sq.Members.Count >= Math.Min(want, faction == "Raiders" && tier <= 1 ? 3 : want) && !SquadManager.Settle(sq, true)) break;
      }
      if (Force != null) {   // Batch 85: a fixed squad (e.g. a Legend and 4 Masters), kitted by rank like the game does
        foreach (var old in sq.Members.ToList()) sq.Members.Remove(old);
        foreach (var r in Force) { var (fw, fa) = EncounterGeneration.PickFactionLoadout(faction, r, tier); sq.Members.Add(new HumanEnemy($"{faction} Operative", p.X, p.Y, faction, r, fw, fa) { Card = r }); }
        sq.BossRolled = false; SquadManager.Arm(sq);
      }
      var g = sq.Members.OfType<HumanEnemy>().Take(size > 0 ? size : 99).ToList(); SizeSum += g.Count; SizeN++;
      for (int i = 0; i < g.Count; i++) {
        var h = g[i]; h.X = p.X + 6 + i % 2; h.Y = p.Y - 5 + i * 2; h.IsAggroed = !ambush;
        TerrainMap.SetTileType(h.X - 1, h.Y, TileType.HardCover);
        EncounterState.Enemies.Add(h);
      }
      p.StartTurn(); p.HealPermanent(200);
      for (int r = 0; r < 40 && p.IsAlive() && g.Any(e => !Out(e)); r++) {
        int shots = 0; bool ended = false;
        for (int k = 0; k < 12 && GameState.Current.CurrentTurn == Turn.Player && (p.Ap > 0 || p.Mp > 0); k++) {
          var w = p.EquippedWeapon; bool melee = CombatRules.IsMelee(w);
          if (p.Hp < 45 && p.Ap >= 2 && p.Inventory.Items.Any(i => i.Id == medkit && i.Qty > 0)) { p.UseConsumable(medkit); continue; }
          if (w.IsJammed) { if (p.Ap < 1) break; p.ClearWeaponJam(); continue; }
          if (!melee && (!w.IsLoaded || (p.OffhandWeapon != null && !p.OffhandWeapon.IsLoaded) || (p.FireMode == "Burst" && w.CurrentMagazine < CombatRules.BurstRounds(w) * (CombatRules.Category(w.Db) == "heavy" && p.ActivePerks.Contains("Shredder") ? 2 : 1)))) { int ap0 = p.Ap; p.ReloadWeapon(); if (p.Ap == ap0) break; continue; }
          var near = g.Where(e => !Out(e)).OrderBy(e => CombatantUtil.Distance(p, e)).FirstOrDefault();
          if (near == null) break;
          var t = g.Where(e => !Out(e) && CombatantUtil.Distance(p, e) <= w.MaxReach).OrderBy(e => e.Hp).FirstOrDefault();
          if (t == null) { if (p.Mp <= 0) break; int x0 = p.X, y0 = p.Y; p.Move(Math.Sign(near.X - p.X), Math.Sign(near.Y - p.Y)); if (p.X == x0 && p.Y == y0) p.Move(Math.Sign(near.X - p.X), 0); if (p.X == x0 && p.Y == y0) break; continue; }
          if (p.SecondaryWeapon != null && (CombatRules.Category(w.Db) == "sniper" || CombatRules.Category(p.SecondaryWeapon.Db) == "sniper")) {   // Batch 127: only the sniper pairing (the New Game blade made the bot juggle rifle and knife)   // Batch 80: the sniper draws the pistol when someone's within 5, and back when clear
            bool close = g.Any(e => !Out(e) && CombatantUtil.Distance(p, e) <= 5);
            if (close == (CombatRules.Category(w.Db) == "sniper") && p.Ap >= p.SwapAp) { p.SwapWeapons(); continue; }
          }
          int alive = g.Count(e => !Out(e));
          if (!melee && shots >= 1 && alive >= 2 && p.Hp < 50 && p.CanHunker(out _)) { p.HunkerDown(); ended = true; break; }
          if (p.Ap < 1) break;
          if (shots == 0 && alive >= 2 && p.Ap >= 4 && g.All(e => Out(e) || e.IsAggroed) && CombatRules.CanBurst(w) && w.CurrentMagazine >= CombatRules.BurstRounds(w) && Environment.GetEnvironmentVariable("NOSUP") != "1") {   // Batch 127: pin the bunch first
            int rad = Player.SuppressRadius(p);
            var spot = g.Where(e => !Out(e) && CombatantUtil.Distance(p, e) <= w.MaxReach && !e.StatusEffects.Suppressed).OrderByDescending(e => g.Count(o => !Out(o) && Math.Max(Math.Abs(o.X - e.X), Math.Abs(o.Y - e.Y)) <= rad)).FirstOrDefault();
            if (spot != null && g.Count(o => !Out(o) && Math.Max(Math.Abs(o.X - spot.X), Math.Abs(o.Y - spot.Y)) <= rad) >= 2) { int a0 = p.Ap; p.SuppressAt(spot.X, spot.Y); if (p.Ap != a0) { shots++; continue; } }
          }
          int before = p.Ap; p.Attack(t, null); shots++;
          if (p.Ap == before) break;
        }
        TurnLoop.CheckCombatEnd();
        if (!ended && GameState.Current.CurrentTurn == Turn.Player) TurnLoop.PlayerEndTurn();
      }
      if (!p.IsAlive()) deaths++; else if (!g.Any(e => !Out(e))) wins++;
    }
    return (wins * 100 / n, deaths * 100 / n);
  }
  static int SizeSum, SizeN; static bool NoCover;
  static string[] Force; static bool Gear; static int f0Note;
  static int Main(string[] args) {
    GameServices.Grid = new TacticalGridSystem(); GameServices.Combat = new CombatResolver(); GameServices.AnomalyTrigger = new AnomalyTriggerService();
    string mode = args.Length > 0 ? args[0] : "screen";
    int n = args.Length > 1 ? int.Parse(args[1]) : 40;
    if (mode == "screen") {
      // score: average win % over Tiers 1, 3 and 5, vs 1-3, open and ambush, at today's AP and at 6 AP
      var scores = new List<(string name, double now, double six, string guns)>();
      foreach (var b in Builds) {
        double sNow = 0, sSix = 0; int cnt = 0; var guns = new List<string>();
        foreach (int tier in new[] { 1, 3, 5 }) {
          guns.Add($"T{tier} {WeaponDatabase.Categories[Gun(b, tier).cat][Gun(b, tier).key].Name}");
          var (zone, faction) = Foes[tier][tier == 5 ? 1 : 0];
          foreach (int size in new[] { 1, 2, 3 }) foreach (bool amb in new[] { false, true }) {
            sNow += Run(b, tier, zone, faction, size, amb, 0, n).win; sSix += Run(b, tier, zone, faction, size, amb, 2, n).win; cnt++;
          }
        }
        scores.Add((b.Name, sNow / cnt, sSix / cnt, string.Join(", ", guns)));
        Console.WriteLine($"{b.Name,-20} score now {sNow / cnt,5:F1}  at 6 AP {sSix / cnt,5:F1}   ({string.Join(", ", guns)})");
      }
      Console.WriteLine("ranked (6 AP): " + string.Join(" > ", scores.OrderByDescending(s => s.six).Select(s => s.name)));
      return 0;
    }
    if (mode == "start") {   // Batch 127: start <n> <loadout>: the New Game kit vs Tier 1 Raiders, 1-3 of them, open and ambush, in cover and in the open
      StartLoadout = args[2]; var b = Builds[4];
      foreach (bool cover in new[] { true, false }) {
        NoCover = !cover;
        foreach (bool amb in new[] { false, true }) {
          var cells = new List<string>();
          foreach (int size in new[] { 1, 2, 3 }) { var r = Run(b, 1, "perimeter", "Raiders", size, amb, 0, n); cells.Add($"vs{size} win {r.win,3} die {r.die,3}"); }
          Console.WriteLine($"  {StartLoadout} {(cover ? "in cover " : "in the open")} {(amb ? "ambush" : "open  ")}  {string.Join("   ", cells)}");
        }
      }
      var p0 = Kit(b, 1, "basicMedkit", 0);
      Console.WriteLine($"  kit: {p0.EquippedWeapon?.Name} ({p0.EquippedWeapon?.Db?.BaseDamage} dmg, mag {p0.EquippedWeapon?.MagCapacity}, range {p0.EquippedWeapon?.RangeTiles}), {p0.EquippedArmor?.Name} (DR {p0.PhysDR:P0}), HP {p0.MaxHp}, AP {p0.MaxAp}, MP {p0.MaxMp}, rank {p0.Rank}, ammo {string.Join(" ", p0.Inventory.Items.Where(i => i.Type == "ammo").Select(i => i.Name + " x" + i.Qty))}");
      return 0;
    }
    if (mode == "raidgear") { Gear = true; mode = "raid"; }
    if (mode == "raid") { foreach (var name in args.Length > 2 ? args.Skip(2).ToArray() : Builds.Select(x => x.Name).ToArray()) Raid(Builds.First(x => x.Name == name), n); return 0; }   // Batch 89
    if (mode == "elite") {   // Batch 85: elite <n> <tier> <foe index> <rank,rank,...> [builds...]
      int tier = int.Parse(args[2]); var (z, f) = Foes[tier][int.Parse(args[3])]; Force = args[4].Split(',');
      foreach (var name in args.Length > 5 ? args.Skip(5).ToArray() : Builds.Select(x => x.Name).ToArray()) {
        var b = Builds.First(x => x.Name == name);
        var a = Run(b, tier, z, f, -1, true, 0, n); var o = Run(b, tier, z, f, -1, false, 0, n);
        Console.WriteLine($"  {b.Name,-20} T{tier} vs {f} {args[4]}: ambush {a.win,3} (died {a.die,3})  open {o.win,3} (died {o.die,3})");
      }
      return 0;
    }
    if (mode == "one") { var b = Builds.First(x => x.Name == args[2]); int tier = int.Parse(args[3]); var (z, f) = Foes[tier][int.Parse(args[4])]; Console.WriteLine(Run(b, tier, z, f, 1, false, 0, n)); return 0; }
    if (mode == "t1") {   // Batch 127: one build at Tier 1 only (targets' first row), for the kit what-ifs
      var b = Builds.First(x => x.Name == args[2]);
      var o1 = Run(b, 1, "perimeter", "Raiders", 1, false, 0, n); var a3 = Run(b, 1, "perimeter", "Raiders", -1, true, 0, n); var o3 = Run(b, 1, "perimeter", "Raiders", -1, false, 0, n);
      var k = Kit(b, 1, "basicMedkit", 0);
      Console.WriteLine($"  {b.Name} T1 open1 {o1.win,3}  ambushSquad {a3.win,3}  openSquad {o3.win,3}  | {k.EquippedWeapon?.Name}, {k.EquippedArmor?.Name} (DR {k.PhysDR:P0}), perks {string.Join("+", k.ActivePerks)}");
      return 0;
    }
    if (mode == "targets") {
      // Batch 78: every build at Tiers 1-5 against the targets - open vs 1 (80%+), ambush vs 3 (~60%), open vs 3 (low)
      var names = args.Length > 2 ? args.Skip(2).ToArray() : Builds.Select(x => x.Name).ToArray();
      foreach (var name in names) {
        var b = Builds.First(x => x.Name == name);
        Console.WriteLine($"=== {b.Name}");
        for (int tier = 1; tier <= 5; tier++) {
          var (c, k) = Gun(b, tier);
          foreach (var (zone, faction) in Foes[tier]) {
            var o1 = Run(b, tier, zone, faction, 1, false, 0, n); SizeSum = SizeN = 0; var a3 = Run(b, tier, zone, faction, -1, true, 0, n); var o3 = Run(b, tier, zone, faction, -1, false, 0, n);
            Console.WriteLine($"  T{tier} {WeaponDatabase.Categories[c][k].Name,-24} {faction,-11} open1 {o1.win,3}  ambushSquad {a3.win,3}  openSquad {o3.win,3}  (squads of {SizeSum / (double)Math.Max(1, SizeN):F1})");
          }
        }
      }
      return 0;
    }
    // full: the named builds through Tiers 1-5, today's AP vs 6 AP
    foreach (var name in args.Skip(2)) {
      var b = Builds.First(x => x.Name == name);
      Console.WriteLine($"=== {b.Name}");
      for (int tier = 1; tier <= 5; tier++) {
        var (c, k) = Gun(b, tier);
        Console.WriteLine($"  Tier {tier} ({Rank[tier]}, {WeaponDatabase.Categories[c][k].Name}{(b.Dual && tier >= 3 ? " x2" : "")}{(tier >= 3 ? ", " + string.Join("+", tier >= 5 ? b.Perks : b.Perks.Take(1)) : "")})");
        foreach (var (zone, faction) in Foes[tier])
          foreach (bool amb in new[] { false, true }) {
            var cells = new List<string>();
            foreach (int size in new[] { 1, 2, 3 }) { var a = Run(b, tier, zone, faction, size, amb, 0, n); var s = Run(b, tier, zone, faction, size, amb, 2, n); cells.Add($"vs{size} {a.win,3}->{s.win,3}"); }
            Console.WriteLine($"    {faction,-11} {(amb ? "ambush" : "open  ")}  {string.Join("   ", cells)}");
          }
      }
    }
    return 0;
  }
}
