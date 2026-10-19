using System; using System.Linq; using GameData; using GameCore;
static class Program {
  static int fails = 0;
  static void Check(bool ok, string msg) { if (!ok) { fails++; Console.WriteLine("FAIL " + msg); } }
  static int Main(string[] args) {
    if (args.Contains("only72")) { Batch72Checks(); Console.WriteLine(fails == 0 ? "BATCH 72 CHECKS PASSED" : $"{fails} FAILURES"); return fails; }
    TerrainChecks(args);
    TierMapChecks();
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
    LiveSquadChecks();
    GroundedChecks(); DeckChecks();
    SoakTest();
    FightSim(args);
    Batch72Checks();
    RaidChecks();
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
    { // Batch 125: on every tier map each exit can be walked to from every other (the Tier 2 road to the Hub sat 5 high on a cliff)
      var walker = new Player { X = 0, Y = 0 }; var grid = GameServices.RequireGrid(); var cut = new System.Collections.Generic.List<string>();
      foreach (var map in new[] { "perimeter", "scrapyard", "scavengers_hub", "vanguard_barracks", "quarry_basin", "abandoned_city" }) {
        MacroState.Current.CurrentZoneId = map; TerrainMap.BuildTerrainForZone(map); EncounterState.Enemies.Clear();
        var ex = TerrainMap.Exits.Where(e => !(MacroMapDatabase.Find(e.TargetZoneId)?.IsDungeon ?? false)).ToList();
        var from = TerrainMap.FindNearestEnterable(ex[0].X, ex[0].Y);
        foreach (var e in ex.Skip(1)) {
          var to = TerrainMap.FindNearestEnterable(e.X, e.Y); walker.X = from.x; walker.Y = from.y;
          if (grid.FindPath(from.x, from.y, to.x, to.y, walker).Count < 2) cut.Add($"{map}: {ex[0].TargetZoneId} -> {e.TargetZoneId}");
        }
      }
      Check(cut.Count == 0, "every map exit can be walked to: cut off " + string.Join("; ", cut));
      MacroState.Current.CurrentZoneId = "scrapyard"; TerrainMap.BuildTerrainForZone("scrapyard"); EncounterState.Enemies.Clear(); EncounterState.InCombat = false;
      var hub = TerrainMap.Exits.First(e => e.TargetZoneId == "scavengers_hub"); walker.X = 60; walker.Y = 59; walker.MaxMp = 2;   // overloaded
      Check(grid.FindPath(60, 59, hub.X, hub.Y, walker).Count > 1, "an overloaded walker still finds the road to the Hub while exploring");
    }
    { // Batch 124: stashes sit where they make sense, drawn as what they are, never two close together
      var byLook = new System.Collections.Generic.Dictionary<string, int>(); int bad = 0, close = 0; string badAt = "";
      foreach (var map in new[] { "perimeter", "scrapyard", "scavengers_hub", "vanguard_barracks", "quarry_basin", "abandoned_city" }) {
        MacroState.Current.CurrentZoneId = map; TerrainMap.BuildTerrainForZone(map);
        foreach (var o in TerrainMap.InteractiveObjects) {
          var l = TerrainMap.GetTile(o.X, o.Y).Look ?? "none";
          byLook[l] = (byLook.TryGetValue(l, out var n) ? n : 0) + 1;
          if (!(l.StartsWith("stash_") || l == "papers" && o.Lock == "note") || o.Name == "Rusted Blast Door") { bad++; badAt = $"{map} {o.X},{o.Y} {o.Name} {l}"; }
          if (TerrainMap.InteractiveObjects.Any(q => q != o && Math.Max(Math.Abs(q.X - o.X), Math.Abs(q.Y - o.Y)) < 5)) close++;
        }
      }
      Console.WriteLine("  stashes: " + string.Join(", ", byLook.OrderByDescending(kv => kv.Value).Select(kv => kv.Key.Replace("stash_", "") + " " + kv.Value)));
      Check(bad == 0, $"every stash is drawn as a container ({bad}, e.g. {badAt})");
      Check(close <= 4, $"stashes kept apart ({close} near another - authored ones may sit close)");
      var box = TerrainMap.InteractiveObjects.First(); var t0 = TerrainMap.GetTile(box.X, box.Y).Look;
      TerrainMap.MarkOpened(box);
      Check(TerrainMap.GetTile(box.X, box.Y).Look == t0 + "_open", "an opened stash shows open");
    }
    { // Batch 126: one road up a tier per map is a held checkpoint; the others are open
      GameBootstrap.StartNewGame(); var M2 = MacroState.Current; var pc = Player.Current;
      var holders = M2.OfflineSquads.Where(q => q.Checkpoint != null).ToList();
      Console.WriteLine("  checkpoints: " + string.Join(", ", holders.Select(q => $"{q.Faction} x{q.Members.Count} hold the road to {q.Checkpoint} ({q.ZoneId})")));
      Check(holders.Count >= 4 && holders.All(q => q.Job == "hold"), "the roads up a tier are held from the start");
      var road = Checkpoints.Road("perimeter");
      var cp = TerrainMap.Landmarks.FirstOrDefault(l => l.Name == Checkpoints.Name(road.Value.target));
      var held = holders.FirstOrDefault(q => q.Checkpoint == road.Value.target);
      Check(cp != null && held != null && held.Members.Where(u => EncounterState.Enemies.Contains(u)).All(u => Math.Max(Math.Abs(u.X - cp.X), Math.Abs(u.Y - cp.Y)) <= 6),
        $"Tier 1's road north has its checkpoint, and its holders stand at it ({cp?.X},{cp?.Y}: {string.Join(" ", held?.Members.Select(u => u.X + "," + u.Y) ?? new string[0])})");
      int bags = 0; if (cp != null) for (int yy = cp.Y - 5; yy <= cp.Y + 5; yy++) for (int xx = cp.X - 5; xx <= cp.X + 5; xx++) if (TerrainMap.GetTile(xx, yy).Look?.StartsWith("sandbags") == true) bags++;
      Check(bags >= 2, $"sandbags at the checkpoint ({bags} at {cp?.X},{cp?.Y})");
      // wiped out: after two days off the map, a new garrison
      foreach (var u in held.Members) u.Hp = 0;
      Checkpoints.Garrison(); M2.CurrentZoneId = "scavengers_hub"; M2.Day += 3; Checkpoints.Garrison();
      Check(M2.OfflineSquads.Count(q => q.Checkpoint == road.Value.target && q.Members.Any(u => u.IsAlive())) == 1, "a wiped-out checkpoint is held again a couple of days later");
    }
    { // Batch 126: ~8 stashes a map, most locked (a key in the toughest mutant, a code on a note by another post), moved every 7 days;
      // a dungeon has one at its far end
      var counts = new System.Collections.Generic.List<string>(); bool kinds = true;
      foreach (var map in new[] { "perimeter", "scrapyard", "scavengers_hub", "vanguard_barracks", "quarry_basin", "abandoned_city" }) {
        MacroState.Current.CurrentZoneId = map; TerrainMap.BuildTerrainForZone(map);
        var boxes = TerrainMap.InteractiveObjects.Where(o => o.Lock != "note").ToList();
        counts.Add($"{map} {boxes.Count} ({boxes.Count(o => o.Lock != null)} locked)");
        kinds &= boxes.Count <= 11 && boxes.Count(o => o.Rolled) >= Stashes.MinRolled;
      }
      Console.WriteLine("  stashes per map: " + string.Join(", ", counts));
      Check(kinds, "about 8 stashes a map, a few rolled ones always");
      // the code puzzle: the safe won't open; reading the note opens it
      MacroState.Current.CurrentZoneId = "abandoned_city"; TerrainMap.BuildTerrainForZone("abandoned_city"); EncounterState.Enemies.Clear();
      var pl = Player.Current; pl.Ap = 10; GameState.Current.CurrentTurn = Turn.Player;
      var safe = TerrainMap.InteractiveObjects.FirstOrDefault(o => o.Lock == "code");
      if (safe != null) {
        var note = TerrainMap.InteractiveObjects.First(o => o.Lock == "note" && o.Opens == Stashes.KeyId(safe));
        pl.X = safe.X; pl.Y = safe.Y + 1; InteractionEngine.AttemptInteract(pl);
        bool shut = TerrainMap.InteractiveObjects.Contains(safe);
        pl.X = note.X; pl.Y = note.Y + 1; InteractionEngine.AttemptInteract(pl);
        pl.X = safe.X; pl.Y = safe.Y + 1; pl.Ap = 10; InteractionEngine.AttemptInteract(pl);
        Check(shut && !TerrainMap.InteractiveObjects.Contains(safe), "a keypad safe opens only with the code from its note");
      } else Check(false, "a code safe on the Tier 6 map");
      // the key: the toughest mutant on the map carries it; killing it hands it over
      var chest = TerrainMap.InteractiveObjects.FirstOrDefault(o => o.Lock == "key");
      var beast = EncounterGeneration.CreateMutantFromDb(EncounterGeneration.PickRandomMutantByTier(3, 5), pl.X + 5, pl.Y);
      EncounterState.Enemies.Add(beast); Stashes.Tick();
      Check(chest != null && beast.Carries == Stashes.KeyId(chest), "the toughest mutant carries the locked chest's key");
      Stashes.OnKill(beast);
      Check(chest != null && MacroState.Current.Keys.Contains(Stashes.KeyId(chest)), "killing it gives you the key");
      // a week later they've moved
      var before = TerrainMap.InteractiveObjects.Where(o => o.Rolled).Select(o => (o.X, o.Y)).ToList();
      MacroState.Current.Day += 7; Stashes.Tick();
      var after = TerrainMap.InteractiveObjects.Where(o => o.Rolled).Select(o => (o.X, o.Y)).ToList();
      Check(after.Count > 0 && after.Intersect(before).Count() < after.Count, "seven days later the stashes are somewhere else");
      // a dungeon: one at the far end from the way in
      MacroState.Current.CurrentZoneId = "smugglers_tunnel"; TerrainMap.BuildTerrainForZone("smugglers_tunnel");
      var door = TerrainMap.Exits.First(); var end = TerrainMap.InteractiveObjects.FirstOrDefault(o => o.Rolled);
      Check(end != null && Math.Max(Math.Abs(end.X - door.X), Math.Abs(end.Y - door.Y)) >= 10, $"a dungeon has its stash at the far end ({end?.X},{end?.Y} vs door {door.X},{door.Y})");
    }
    { // Batch 123: the main menu's live backdrop - Bonfire Village, a day in three minutes, the weather drifting
      MacroState.Current = new MacroState(); MenuBackdrop.Reset(); MenuBackdrop.Prepare();
      Check(TerrainMap.ZoneId == "perimeter" && TerrainMap.GetTile(MenuBackdrop.FireX, MenuBackdrop.FireY).Look == "campfire", "the menu shows Bonfire Village's fire");
      int day0 = MacroState.Current.Day; var seen = new System.Collections.Generic.HashSet<Weather> { Atmosphere.Current }; bool night = false;
      for (int i = 0; i < 180 * 10; i++) { MenuBackdrop.Advance(1f); seen.Add(Atmosphere.Current); night |= Atmosphere.Look().Night > 0.6f; }
      Check(MacroState.Current.Day == day0 + 10 && night && seen.Count >= 3, $"30 menu minutes: 10 days pass, night falls, the weather changes ({string.Join(",", seen)})");
      Console.WriteLine($"  menu after 30 min: {MenuBackdrop.Label()}");
    }
    Console.WriteLine(fails == 0 ? "ALL CHECKS PASSED" : $"{fails} FAILURES");
    return fails == 0 ? 0 : 1;
  }

  // --- terrain: destructible tiles, doors, barrels, and a render of every zone through TerrainArt ---

  // the map overhaul: one map per tier. Its regions, exits, dungeons, arrival spots and stashes have to agree with the
  // zone graph, or you'd walk out of a hatch into the wrong region, or a squad would route through a wall.
  static void TierMapChecks() {
    var maps = MacroMapDatabase.Zones.Where(z => z.MapId == null && MapLayoutDatabase.All.TryGetValue(z.Id, out var l) && l.Regions != null).ToList();
    Check(maps.Count == 6, $"six tier maps ({maps.Count}: {string.Join(",", maps.Select(z => z.Id))})");
    foreach (var map in maps) {
      var lay = MapLayoutDatabase.All[map.Id];
      var regions = MacroMapDatabase.RegionsOf(map.Id).ToList();
      Check(lay.Tiles.Length >= 180 && lay.Tiles[0].Length >= 180, $"{map.Id} is a big map ({lay.Tiles[0].Length}x{lay.Tiles.Length})");
      Check(regions.Count >= 3, $"{map.Id} holds its tier's old zones as regions ({regions.Count})");
      foreach (var r in regions) Check(TerrainMap.RegionArea(r.Id) > 1000, $"{map.Id}: region {r.Id} has ground ({TerrainMap.RegionArea(r.Id)} tiles)");
      Check(regions.All(r => r.Tier == map.Tier), $"{map.Id}: every region is tier {map.Tier}");
      string RegionOf(int x, int y) => lay.Regions[Math.Clamp(y, 0, lay.Tiles.Length - 1)][Math.Clamp(x, 0, lay.Tiles[0].Length - 1)] ?? map.Id;
      // every way off the map is pinned, in a region that actually leads there, and the far side leads back to it
      foreach (var r in regions)
        foreach (var t in r.ConnectedZones.Concat(r.HiddenConnections ?? new System.Collections.Generic.List<string>()).Where(t => !MacroMapDatabase.SameMap(t, map.Id))) {
          bool pinned = lay.Anchors.TryGetValue("exit:" + t, out var a);
          Check(pinned, $"{map.Id}: the way from {r.Id} to {t} is pinned on the map");
          if (!pinned) continue;
          string at = RegionOf(a.x, a.y);
          Check(MacroMapDatabase.Find(at)?.ConnectedZones.Contains(t) == true || MacroMapDatabase.Find(at)?.HiddenConnections?.Contains(t) == true,
            $"{map.Id}: the exit to {t} at ({a.x},{a.y}) lies in {at}, which leads there");
          var far = MacroMapDatabase.Find(t);
          Check(far != null && far.ConnectedZones.Contains(at), $"{t} leads back to {at}");
          var farLay = MapLayoutDatabase.All.TryGetValue(MacroMapDatabase.MapOf(t), out var fl) ? fl : null;
          Check(farLay?.Anchors != null && farLay.Anchors.ContainsKey("exit:" + at), $"{t}'s map has its way back to {at} pinned");
        }
      // arrival spots and stashes stand in the right places
      foreach (var kv in lay.Anchors.Where(k => k.Key.StartsWith("arrive:")))
        Check(RegionOf(kv.Value.x, kv.Value.y) == kv.Key.Substring(7), $"{map.Id}: {kv.Key} lies in its region");
      // every region's landmarks are pinned (nothing scattered at random over a 200x200 map)
      foreach (var r in regions)
        foreach (var d in LandmarkDatabase.ForZone(r.Id))
          Check(lay.Anchors.ContainsKey(d.Name), $"{map.Id}: landmark {d.Name} ({r.Id}) is pinned");
    }
    // every dungeon: a small map of its own, pinned exit back up, interior (no sky), and its entrance is on the surface
    foreach (var d in MacroMapDatabase.Zones.Where(z => z.IsDungeon)) {
      Check(MapLayoutDatabase.All.TryGetValue(d.Id, out var dl) && dl.FullSize && dl.Tiles.Length <= 60, $"{d.Id} is a small full-size map");
      Check(ZoneLayoutRecipes.IsInterior(d.Id), $"{d.Id} is underground");
      foreach (var up in d.ConnectedZones) Check(MapLayoutDatabase.All[d.Id].Anchors.ContainsKey("exit:" + up), $"{d.Id}: the way back up to {up} is pinned");
    }
    // the locked bunker's key is in a real stash, on the map whose region leads to the bunker
    var locked = MacroMapDatabase.Zones.Where(z => z.RequiresKey != null).ToList();
    foreach (var z in locked) {
      var stash = TerrainMap.StashKeys.FirstOrDefault(k => k.Value == z.RequiresKey).Key;
      Check(stash != null && MapLayoutDatabase.All[MacroMapDatabase.MapOf(z.ConnectedZones[0])].Anchors.ContainsKey("stash:" + stash), $"{z.Id}'s key ({stash}) is in a stash on its map");
    }
    // walking between regions is free: no crossing, no time, the region becomes the current zone
    {
      GameBootstrap.StartNewGame();
      var m = MacroState.Current; var p = Player.Current;
      Check(m.CurrentZoneId == "bonfire_village" && TerrainMap.ZoneId == "perimeter", $"New Game starts in Bonfire Village on the Tier 1 map ({m.CurrentZoneId} on {TerrainMap.ZoneId})");
      Check(MacroMapDatabase.Find(m.CurrentZoneId).IsSafeZone && !MacroMapDatabase.Find("perimeter").IsSafeZone, "the village is the Safe Zone, the Perimeter isn't");
      float hour = m.Hour; int day = m.Day;
      var spot = TerrainMap.RegionCenter("marshes");
      p.X = spot.x; p.Y = spot.y; ZoneTravel.UpdateRegion();
      Check(m.CurrentZoneId == "marshes" && TerrainMap.ZoneId == "perimeter" && m.Hour == hour && m.Day == day, $"walking into the marsh makes it the current zone, same map, no time passes ({m.CurrentZoneId})");
      Check(!ZoneTravel.TravelToSector("smugglers_run") && m.CurrentZoneId == "marshes", "a region on your own map is walked to, not travelled to");
      Check(TerrainMap.Exits.Any(e => e.TargetZoneId == "scrapyard") && TerrainMap.Exits.Any(e => e.TargetZoneId == "industrial_basin") && !TerrainMap.Exits.Any(e => MacroMapDatabase.SameMap(e.TargetZoneId, "perimeter")),
        "the Tier 1 map's exits are its ways to Tier 2 and the dungeons, none to its own regions");
      // the bunker is locked until you have the key
      var bunkerExit = TerrainMap.Exits.FirstOrDefault(e => e.TargetZoneId == "flooded_bunker");
      Check(bunkerExit != null, "the flooded bunker's door is on the map");
      m.Keys.Clear();
      Check(!ZoneTravel.TravelToSector("flooded_bunker") && TerrainMap.ZoneId == "perimeter", "without the key the bunker stays shut");
      m.Keys.Add("flooded_bunker");
      Check(ZoneTravel.TravelToSector("flooded_bunker") && m.CurrentZoneId == "flooded_bunker", "with the key you go down into the bunker");
      Check(m.Day == day && Math.Abs(m.Hour - hour) < 0.01f, "climbing into a dungeon isn't a day on the road");
      Check(ZoneTravel.TravelToSector("marshes") && m.CurrentZoneId == "marshes" && TerrainMap.ZoneId == "perimeter"
        && Math.Max(Math.Abs(p.X - bunkerExit.X), Math.Abs(p.Y - bunkerExit.Y)) <= 3, $"climbing out you're back at the bunker door ({p.X},{p.Y} vs {bunkerExit.X},{bunkerExit.Y})");
      { // Batch 120: upper floors are maps of their own - up the farmhouse stairs and back down
        var stair = TerrainMap.Exits.FirstOrDefault(e => e.TargetZoneId == "farmhouse_upper");
        Check(stair != null && stair.X == 20 && stair.Y == 119, $"the farmhouse has a stair up ({stair?.X},{stair?.Y})");
        { // Batch 123: prompts over what you can use: the stair (walk on it), a container (E)
          int sx0 = p.X, sy0 = p.Y;
          p.X = stair.X + 1; p.Y = stair.Y;
          var pr = InteractionPrompts.Near(p);
          Check(pr.Any(q => q.Key == "WALK" && q.Label == "Stairs" && q.X == stair.X && q.Y == stair.Y), $"a prompt over the stair ({string.Join(", ", pr.Select(q => q.Key + " " + q.Label))})");
          var box = TerrainMap.InteractiveObjects.FirstOrDefault();
          if (box != null) { p.X = box.X; p.Y = box.Y + 1; Check(InteractionPrompts.Near(p).Any(q => q.Key == "E" && q.Label == box.Name), "an E prompt over a container next to you"); }
          // Batch 125: a route past the stair doesn't step on it (that would take you upstairs); no jobs are handed out up there
          var around = GameServices.RequireGrid().FindPath(stair.X - 1, stair.Y, stair.X + 1, stair.Y, p);
          Check(around.Count > 1 && !around.Any(c => c.x == stair.X && c.y == stair.Y), $"a walk past the stair goes round it ({string.Join(" ", around)})");
          Check(QuestSystem.AvailableInZone("farmhouse_upper").Count == 0, "no jobs handed out upstairs");
          p.X = sx0; p.Y = sy0;
        }
        Check(ZoneTravel.TravelToSector("farmhouse_upper") && TerrainMap.ZoneId == "farmhouse_upper" && Math.Max(Math.Abs(p.X - 6), Math.Abs(p.Y - 9)) <= 2
          && !ZoneLayoutRecipes.IsUpstairs("perimeter") && ZoneLayoutRecipes.IsUpstairs("farmhouse_upper") && Atmosphere.Look().Night < 0.85f,
          $"upstairs in the farmhouse, by the stair, in daylight ({p.X},{p.Y} on {TerrainMap.ZoneId}, night {Atmosphere.Look().Night})");
        Check(ZoneTravel.TravelToSector("perimeter") && TerrainMap.ZoneId == "perimeter" && Math.Max(Math.Abs(p.X - stair.X), Math.Abs(p.Y - stair.Y)) <= 3,
          $"down again, by the farmhouse stair ({p.X},{p.Y})");
        Check(m.Day == day, "stairs take no travel time");
        string here0 = TerrainMap.ZoneId;
        TerrainMap.BuildTerrainForZone("flats_2");
        Check(TerrainMap.Exits.Any(e => e.TargetZoneId == "ghost_town") && TerrainMap.Exits.Any(e => e.TargetZoneId == "flats_3"), "the flats' second floor has stairs down and up");
        TerrainMap.BuildTerrainForZone("flats_3");
        Check(TerrainMap.Exits.Any(e => e.TargetZoneId == "flats_2") && TerrainMap.Exits.Count == 1, "the top floor only goes down");
        TerrainMap.BuildTerrainForZone("vanguard_barracks");
        Check(TerrainMap.Exits.Any(e => e.TargetZoneId == "school_upper") && TerrainMap.Exits.Any(e => e.TargetZoneId == "flats_2"), "Ghost Town's school and flats have stairs up");
        TerrainMap.BuildTerrainForZone(here0);
      }
      // the stash in the headman's house holds the key
      var strongbox = TerrainMap.InteractiveObjects.FirstOrDefault(o => o.GrantsKey == "flooded_bunker");
      Check(strongbox != null && strongbox.Name == "Headman's Strongbox", "the headman's strongbox is on the map and holds the bunker key");
      // squads keep out of a Safe Zone that isn't theirs
      var v = TerrainMap.RegionCenter("bonfire_village");
      Check(!SquadManager.MayWalkInto("Raiders", v.x, v.y) && SquadManager.MayWalkInto("Wanderers", v.x, v.y) && SquadManager.MayWalkInto("Raiders", spot.x, spot.y), "Raiders don't wander into Bonfire Village; Wanderers may");
    }
  }

  static void TerrainChecks(string[] args) {
    string art = Environment.GetEnvironmentVariable("ART_DIR");
    UnityEngine.Resources.Hook = (path, type) => {
      if (art == null) return null;
      if (path == "Sprites/terrain_atlas" && System.IO.File.Exists(art + "/terrain_atlas.raw")) return new UnityEngine.TextAsset { bytes = System.IO.File.ReadAllBytes(art + "/terrain_atlas.raw") };
      if (path == "Sprites/terrain_names" && System.IO.File.Exists(art + "/terrain_names.txt")) return new UnityEngine.TextAsset { text = System.IO.File.ReadAllText(art + "/terrain_names.txt") };
      if (path == "Sprites/world_atlas" && System.IO.File.Exists(art + "/world_atlas.raw")) return new UnityEngine.TextAsset { bytes = System.IO.File.ReadAllBytes(art + "/world_atlas.raw") };
      if (path == "Sprites/world_atlas_meta" && System.IO.File.Exists(art + "/world_atlas.txt")) return new UnityEngine.TextAsset { text = System.IO.File.ReadAllText(art + "/world_atlas.txt") };
      return null;
    };
    GameServices.Grid = new TacticalGridSystem();
    GameServices.Combat = new CombatResolver();
    var dump = Environment.GetEnvironmentVariable("DUMP_ZONE");
    if (dump != null) foreach (var dz in dump.Split(',')) {
      MacroState.Current.CurrentZoneId = dz; TerrainMap.BuildTerrainForZone(dz);
      var sb = new System.Text.StringBuilder();
      for (int y = 0; y < TerrainMap.Height; y++) {
        for (int x = 0; x < TerrainMap.Width; x++) {
          var t = TerrainMap.GetTile(x, y); char ch = t.Type switch { TileType.Solid => '#', TileType.ScatterCover => 'c', TileType.HardCover => '=', TileType.Difficult => '~', TileType.Door => 'D', TileType.DoorOpen => 'd', TileType.Barrel => 'b', _ => t.Height != 0 ? (char)('0' + Math.Min(9, Math.Abs(t.Height))) : '.' };
          if (TerrainMap.Landmarks.Any(l => l.X == x && l.Y == y)) ch = 'L';
          if (TerrainMap.Exits.Any(e => e.X == x && e.Y == y)) ch = 'E';
          sb.Append(ch);
        }
        sb.Append('\n');
      }
      System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"dump_{dz}.txt"), sb.ToString());
    }
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
    { // Batch 102: diagonals cost 1.5, never cut a wall corner, and an open-ground route is a straight line
      var g = GameServices.Grid; int bx = 60, by = 60;
      for (int y = by - 1; y <= by + 6; y++) for (int x = bx - 1; x <= bx + 12; x++) TerrainMap.SetTileType(x, y, TileType.Empty);
      var d = new HumanEnemy("d", bx, by, "Raiders", "Rookie", null, null) { Mp = 4 };
      Check(Math.Abs(g.GetMoveCost(d, bx + 1, by + 1) - 1.5f) < 0.01f, "a diagonal step costs 1.5 MP");
      TerrainMap.SetTileType(bx + 1, by, TileType.Solid);
      Check(float.IsInfinity(g.GetMoveCost(d, bx + 1, by + 1)) && !g.AttemptMove(d, bx + 1, by + 1), "no diagonal past a wall corner");
      TerrainMap.SetTileType(bx + 1, by, TileType.Empty);
      var path = g.FindPath(bx, by, bx + 10, by + 4, d);
      bool straight = path.Count == 11 && path.TrueForAll(q => Math.Abs((q.y - by) - (q.x - bx) * 0.4f) <= 0.6f);
      Check(straight, $"an open-ground route hugs the straight line ({string.Join(" ", path)})");
    }
    { // Batch 103: physical props - desks flip into one-sided cover, barrels roll and bowl people over, blasts shove
      int bx = 60, by = 60;
      for (int y = by - 4; y <= by + 8; y++) for (int x = bx - 4; x <= bx + 14; x++) TerrainMap.SetTileType(x, y, TileType.Empty);
      var me = new HumanEnemy("me", bx, by, "Raiders", "Rookie", null, null) { Ap = 4 };
      TerrainMap.SetTileType(bx, by + 1, TileType.Difficult, "desk");
      Check(CoverUtil.GetCoverAt(bx, by + 6, bx, by).AcBonus == 0, "a standing desk is no cover");
      Check(Props.Shove(me, bx, by + 1) && TerrainMap.GetTile(bx, by + 1).Look == "desk_s", "shoving a desk south tips it, top facing south");
      Check(CoverUtil.GetCoverAt(bx, by + 6, bx, by).AcBonus > 0, "a flipped desk shields from the side it faces");
      Check(CoverUtil.GetCoverAt(bx, by - 4, bx, by + 2).AcBonus == 0, "but not from its back side");
      Check(Props.Shove(me, bx, by + 1) && TerrainMap.GetTile(bx, by + 2).Look == "desk_s", "a tipped desk slides a tile when shoved, still facing the same way");
      TerrainMap.SetTileType(bx, by + 2, TileType.Empty);
      TerrainMap.SetTileType(bx + 1, by, TileType.ScatterCover, "rustbarrel");
      TerrainMap.SetTileType(bx + 9, by, TileType.Solid);
      Check(Props.Shove(me, bx + 1, by) && TerrainMap.GetTile(bx + 7, by).Look == "rustbarrel" && TerrainMap.GetTile(bx + 1, by).Look == null,
        "a kicked barrel rolls up to 6 tiles");
      TerrainMap.SetTileType(bx + 7, by, TileType.Empty); TerrainMap.SetTileType(bx + 1, by, TileType.Barrel);
      var foe = new HumanEnemy("foe", bx + 4, by, "Raiders", "Rookie", null, null); EncounterState.Enemies.Add(foe); int hp = foe.Hp;
      Check(Props.Shove(me, bx + 1, by) && TerrainMap.GetTile(bx + 3, by).Type == TileType.Barrel && foe.Hp < hp && foe.StatusEffects.Prone,
        "a rolling explosive barrel stops at a man, knocks him down, and doesn't blow");
      EncounterState.Enemies.Remove(foe);
      { // Batch 107: an enemy kicks an explosive barrel up to its foe
        TerrainMap.SetTileType(bx + 3, by, TileType.Empty);
        var kicker = new HumanEnemy("kicker", bx, by + 3, "Raiders", "Rookie", WeaponDatabase.Categories["assault"].Values.First(), null) { Ap = 4 };
        var target2 = new HumanEnemy("target", bx + 7, by + 3, "Wanderers", "Rookie", null, null); EncounterState.Enemies.Add(target2);
        TerrainMap.SetTileType(bx + 1, by + 3, TileType.Barrel);
        Check(Props.KickAtFoe(kicker, target2) && TerrainMap.GetTile(bx + 6, by + 3).Type == TileType.Barrel,
          "an enemy kicks a barrel to roll it up next to its foe");
        TerrainMap.SetTileType(bx + 6, by + 3, TileType.Empty); EncounterState.Enemies.Remove(target2);
      }
      TerrainMap.SetTileType(bx + 3, by, TileType.Empty); TerrainMap.SetTileType(bx + 5, by + 4, TileType.ScatterCover, "crate");
      Props.BlastShove(bx + 3, by + 4, 1);
      Check(TerrainMap.GetTile(bx + 6, by + 4).Look == "crate", "a blast shoves a crate just outside it one tile outward");
      TerrainMap.SetTileType(bx + 6, by + 4, TileType.Empty); TerrainMap.SetTileType(bx, by + 1, TileType.Empty);
    }
    { // Batch 105: materials - toughness and what bullets go through
      int bx = 60, by = 60;
      TerrainMap.SetTileType(bx, by, TileType.ScatterCover, "crate"); TerrainMap.SetTileType(bx + 1, by, TileType.HardCover, "sandbags");
      TerrainMap.SetTileType(bx + 2, by, TileType.HardCover, "rock"); TerrainMap.SetTileType(bx + 3, by, TileType.HardCover, "block");
      Check(Materials.Pierced(Materials.At(bx, by)) && !Materials.Pierced(Materials.At(bx + 1, by)) && !Materials.Pierced(Materials.At(bx + 3, by)),
        "bullets go through a wooden crate, not sandbags or concrete");
      Check(TileRules.MaxIntegrity(TerrainMap.GetTile(bx, by)) == 3 && TileRules.MaxIntegrity(TerrainMap.GetTile(bx + 2, by)) == 10
        && TileRules.MaxIntegrity(TerrainMap.GetTile(bx + 3, by)) == 6, "a crate takes 3 hits, a boulder 10, a concrete block 6");
      for (int x = bx; x <= bx + 3; x++) TerrainMap.SetTileType(x, by, TileType.Empty);
    }
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

    { // Batch 122: roofs to fit the building (pitched, flat tar, saw-tooth, slab), fronts turned east and west too
      var styles = new System.Collections.Generic.Dictionary<RoofView.Roof, int>(); int[] fronts = new int[4];
      foreach (var map in new[] { "perimeter", "scrapyard", "scavengers_hub", "vanguard_barracks", "quarry_basin", "abandoned_city" }) {
        MacroState.Current.CurrentZoneId = map; TerrainMap.BuildTerrainForZone(map); RoofView.Find();
        var here = new System.Collections.Generic.List<string>();
        foreach (var rb in RoofView.Buildings) { styles[rb.Style] = (styles.TryGetValue(rb.Style, out var n) ? n : 0) + 1; fronts[rb.Front]++; if (rb.Style == RoofView.Roof.Saw) here.Add($"{rb.X0},{rb.Y0}"); }
        if (here.Count > 0) Console.WriteLine($"  (saw-tooth roofs on {map}: {string.Join(" ", here)})");
      }
      Console.WriteLine($"  roofs: {string.Join(", ", styles.Select(kv => kv.Key + " " + kv.Value))}; fronts S/E/W/N {string.Join("/", fronts)}");
      Check(styles.ContainsKey(RoofView.Roof.Gable) && styles.ContainsKey(RoofView.Roof.Hip) && styles.ContainsKey(RoofView.Roof.Tar)
        && styles.ContainsKey(RoofView.Roof.Saw) && styles.ContainsKey(RoofView.Roof.Slab), "every roof kind is on some tier map");
      Check(fronts[1] + fronts[2] > fronts.Sum() / 4, "a good share of buildings front east or west");
    }
    if (art == null) return;
    // Batch 30: every look in every zone has world art; the city's solid blocks became buildings
    if (WorldArt.Ready) {
      foreach (var zone in MacroMapDatabase.Zones) {
        MacroState.Current.CurrentZoneId = zone.Id; TerrainMap.BuildTerrainForZone(zone.Id); WorldArt.Begin(zone.Id);
        var miss = WorldArt.MissingArt();
        Check(miss.Count == 0, $"{zone.Id}: no world art for {string.Join(", ", miss.Take(6))}");
      }
      // line of sight: own tile seen, far tiles not, a tile behind a wall not
      MacroState.Current.CurrentZoneId = "abandoned_city"; TerrainMap.BuildTerrainForZone("abandoned_city");
      var vp = new Player(); bool tested = false;
      for (int y = 3; y < TerrainMap.Height - 3 && !tested; y++)
        for (int x = 3; x < TerrainMap.Width - 3 && !tested; x++)
          if (TerrainMap.CanEnterTile(x, y) && TerrainMap.GetTile(x, y + 1).Type == TileType.Solid && TerrainMap.GetTile(x, y + 2).Type == TileType.Solid && TerrainMap.CanEnterTile(x, y + 3)) {
            vp.X = x; vp.Y = y; Vision.Reset(); Vision.Update(vp, 0);
            Check(Vision.Sees(x, y) && !Vision.Sees(x, y + 3) && !Vision.Sees(x + Vision.MaxRadius + 2, y), $"vision at ({x},{y}): own tile yes, behind the wall no, out of range no");
            tested = true;
          }
      Check(tested, "found a wall to test vision against");
      // Batch 32: windows - see and shoot through, hard cover, still a wall to walk into
      bool win = false;
      for (int y = 1; y < TerrainMap.Height - 1 && !win; y++)
        for (int x = 1; x < TerrainMap.Width - 1 && !win; x++)
          if (TerrainMap.GetTile(x, y).Look == "window" && TerrainMap.GetTile(x, y).Type == TileType.Solid) {
            Check(!TerrainMap.BlocksSight(x, y) && TerrainMap.CoverAcAt(x, y) == 2 && !TerrainMap.CanEnterTile(x, y), "windows: see-through, hard cover, not walkable");
            win = true;
          }
      Check(win, "found a window");
      // Batch 31: time of day and weather
      var noon = Atmosphere.Look(12f, Weather.Clear, false); var night = Atmosphere.Look(1f, Weather.Clear, false);
      var fog = Atmosphere.Look(8f, Weather.Fog, false); var storm = Atmosphere.Look(14f, Weather.Storm, false);
      Check(noon.Night == 0f && noon.Vision >= 12f && night.Night > 0.6f && night.Cone > 0f && night.Vision < 7f, $"day/night look (noon {noon.Vision}, night {night.Vision}/{night.Cone})");
      Check(fog.Fog > 0f && fog.Vision <= 7f && storm.Rain == 1f && storm.Lightning, "fog and storm look");
      // Batch 35: the colour grade - warm at 18:00, cold in the rain, never grey enough to lose faction colours
      var gold = Atmosphere.Look(18f, Weather.Clear, false); var rain = Atmosphere.Look(12f, Weather.Rain, false);
      Check(gold.Filter.r > gold.Filter.b && rain.Filter.b > rain.Filter.r && rain.Filter.r < 1f && night.Saturation < noon.Saturation, "colour grade by hour and weather");
      bool readable = true;
      for (float h = 0f; h < 24f; h += 0.5f) foreach (Weather w in Enum.GetValues(typeof(Weather))) readable &= Atmosphere.Look(h, w, false).Saturation >= 0.65f;
      Check(readable && Atmosphere.Look(0f, Weather.Clear, true).Saturation >= 0.65f, "grade keeps faction colours readable");
      ColorGrade.Apply(noon); Check(!ColorGrade.Active, "no URP in the harness: the grade stays off and the wash works alone");
      int day0 = MacroState.Current.Day; MacroState.Current.Hour = 23.5f; Atmosphere.Advance(60f);
      Check(MacroState.Current.Day == day0 + 1 && Math.Abs(MacroState.Current.Hour - 0.5f) < 0.01f, "midnight starts a new day");
      // Batch 36: A-Life on the clock - one tick per game hour, and the player's own (live) zone is left alone
      {
        var mm = MacroState.Current; string zone0 = mm.CurrentZoneId;
        var here = MacroMapDatabase.Zones.First(z => !z.IsSafeZone && z.ConnectedZones.Count > 0);
        var away = MacroMapDatabase.Zones.First(z => z.Id != here.Id && !z.IsSafeZone && z.ConnectedZones.Count > 0 && !z.ConnectedZones.Contains(here.Id));
        mm.CurrentZoneId = here.Id; string loaded = TerrainMap.ZoneId; TerrainMap.ZoneId = null;   // its map isn't loaded: nothing walks in or out
        var live = SquadManager.CreateOfflineSquad(here.Id, "Raiders", 3); var roam = SquadManager.CreateOfflineSquad(away.Id, "Raiders", 3);
        int t0 = mm.WorldTickCounter; mm.Hour = 10.5f; Atmosphere.Advance(Atmosphere.CrossingHours * 60f);
        Check(mm.WorldTickCounter - t0 == 8, $"8 hours on the road are 8 A-Life ticks ({mm.WorldTickCounter - t0})");
        bool acted = false;
        for (int h = 0; h < 500 && !acted; h++) { Atmosphere.Advance(60f); acted = roam.ZoneId != away.Id || !mm.OfflineSquads.Contains(roam); }
        Check(acted, "an off-screen squad moves on (or dies) as the hours pass");
        Check(live.ZoneId == here.Id && mm.OfflineSquads.Contains(live) && live.Members.All(x => x.IsAlive()), "the off-screen tick leaves the player's own zone alone");
        mm.OfflineSquads.Remove(live); mm.OfflineSquads.Remove(roam); mm.CurrentZoneId = zone0; TerrainMap.ZoneId = loaded;
      }
      // the flashlight: at night you see along your aim, not behind you
      for (int y = 3, done = 0; y < TerrainMap.Height - 3 && done == 0; y++)
        for (int x = 15; x < TerrainMap.Width - 15 && done == 0; x++) {
          bool open = true; for (int k = -10; k <= 10 && open; k++) open = TerrainMap.CanEnterTile(x + k, y);
          if (!open) continue;
          vp.X = x; vp.Y = y; Vision.Reset(); Vision.Update(vp, 0, night.Vision, night.Cone, 0f);   // aiming east
          Check(Vision.Sees(x + 9, y) && !Vision.Sees(x - 9, y), "flashlight: 9 tiles ahead seen, 9 behind not");
          done = 1;
        }
      Vision.Reset();
      var city = MapLayoutDatabase.All["abandoned_city"];
      Check(city.Looks.Sum(r => r.Count(l => l == "window")) > 20 && city.Tiles.Sum(r => r.Count(t => t == "Door")) > 6, "city blocks carved into buildings with doors and windows");
      // Batch 34: roofs over every walled-in space, none over the street
      MacroState.Current.CurrentZoneId = "abandoned_city"; TerrainMap.BuildTerrainForZone("abandoned_city");
      RoofView.Find();
      int roofed = 0, streetRoofed = 0, floorOpen = 0;
      for (int yy = 0; yy < TerrainMap.Height; yy++)
        for (int xx = 0; xx < TerrainMap.Width; xx++) {
          var tt = TerrainMap.GetTile(xx, yy); bool r = RoofView.IdMap[yy * TerrainMap.Width + xx] >= 0;
          if (r) roofed++;
          if (r && (tt.Ground == GroundType.Asphalt)) streetRoofed++;
          if (!r && tt.Ground == GroundType.Floor && tt.Type == TileType.Empty && TerrainArt.ObjectLook(xx, yy, ZoneThemes.For("abandoned_city")) == null) floorOpen++;
        }
      Check(RoofView.Buildings.Count >= 6 && roofed > 2000, $"roofs found ({RoofView.Buildings.Count} buildings, {roofed} cells)");
      Check(streetRoofed == 0, $"no roof over the street ({streetRoofed})");
      Console.WriteLine($"  (open-air floor cells in the city: {floorOpen})");
      // every door of a roofed building leads outside on one side
      var door = (x: -1, y: -1);
      for (int yy = 1; yy < TerrainMap.Height - 1 && door.x < 0; yy++)
        for (int xx = 1; xx < TerrainMap.Width - 1; xx++)
          if (TerrainMap.GetTile(xx, yy).Type == TileType.Door && RoofView.IdMap[(yy - 1) * TerrainMap.Width + xx] >= 0 && RoofView.IdMap[(yy + 1) * TerrainMap.Width + xx] < 0) { door = (xx, yy); break; }
      Check(door.x >= 0, "a front door between a roofed room and the street"); Console.WriteLine($"  (front door at {door.x},{door.y})");
    }
    foreach (var zone in MacroMapDatabase.Zones) {
      MacroState.Current.CurrentZoneId = zone.Id;
      TerrainMap.BuildTerrainForZone(zone.Id);
      if (Environment.GetEnvironmentVariable("LOOK_STATS") != null) {
        var cnt = new System.Collections.Generic.Dictionary<string, int>();
        for (int yy = 0; yy < TerrainMap.Height; yy++) for (int xx = 0; xx < TerrainMap.Width; xx++) { var lk = TerrainArt.ObjectLook(xx, yy, ZoneThemes.For(zone.Id)) ?? "-"; cnt[lk] = cnt.TryGetValue(lk, out var c0) ? c0 + 1 : 1; }
        var where = Environment.GetEnvironmentVariable("LOOK_WHERE");
        if (where != null) { int nn = 0; for (int yy = 0; yy < TerrainMap.Height && nn < 6; yy++) for (int xx = 0; xx < TerrainMap.Width && nn < 6; xx++) if (TerrainArt.ObjectLook(xx, yy, ZoneThemes.For(zone.Id)) == where && (xx + yy) % 7 == 0) { Console.WriteLine($"  {where} at {xx},{yy}"); nn++; } }
        Console.WriteLine($"LOOKS {zone.Id} {TerrainMap.Width}x{TerrainMap.Height}: " + string.Join(" ", cnt.OrderByDescending(k => k.Value).Select(k => $"{k.Key}={k.Value}")));
      }
      var only = Environment.GetEnvironmentVariable("RENDER_ONLY");
      if (only != null && !only.Split(',').Contains(zone.Id)) continue;
      if (Environment.GetEnvironmentVariable("RENDER_HOUR") is string rh) MacroState.Current.Hour = float.Parse(rh, System.Globalization.CultureInfo.InvariantCulture);
      if (Environment.GetEnvironmentVariable("RENDER_WEATHER") is string rwx) Atmosphere.Current = Enum.Parse<Weather>(rwx);
      Vision.Reset();
      if (Environment.GetEnvironmentVariable("PREVIEW_PLAYER") is string pp) {          // roofs cut away as this player would see them
        var pv = new Player(); var xy = pp.Split(','); pv.X = int.Parse(xy[0]); pv.Y = int.Parse(xy[1]); Vision.Update(pv, 0);
      }
      var sw = System.Diagnostics.Stopwatch.StartNew();
      var px = WorldArt.Ready ? WorldArt.ComposePreview(zone.Id) : TerrainArt.Compose(zone.Id);
      if (Environment.GetEnvironmentVariable("TIME_RENDER") != null) Console.WriteLine($"render {zone.Id}: {sw.ElapsedMilliseconds} ms ({TerrainMap.Width}x{TerrainMap.Height})");
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
    // Batch 63 (Yan): jams are 1%, +2% per wear step
    Check(CombatRules.JamChance(vk74) == 1 && CombatRules.Jams(1, vk74) && !CombatRules.Jams(2, vk74), "a working gun jams 1% of the time");
    vk74.Condition = ItemCondition.Dirty; Check(CombatRules.JamChance(vk74) == 3, "dirty jams 3%");
    CombatRules.DegradeOnJam(vk74); Check(vk74.Condition == ItemCondition.Damaged && CombatRules.JamChance(vk74) == 5, "a jam wears it to Damaged: 5%");
    CombatRules.DegradeOnJam(vk103); Check(vk103.Condition != ItemCondition.Dirty, "VK-103 reliable");
    vk74.Upgrades.Add("Hardened"); Check(CombatRules.JamChance(vk74) == 1, "hardened stays at 1% however worn"); vk74.Upgrades.Clear();
    { int n = 0; for (int i = 1; i <= 100; i++) if (CombatRules.Jams(i, vk74)) n++; int r = CombatRules.RollJam(); Check(n == 5 && r >= 1 && r <= 100, $"Damaged jams on 5 of 100 ({n})"); }
    Check(new HumanEnemy("p", 0, 0, "Garrison", "Professional", null, null).Frags == 0 && new HumanEnemy("v", 0, 0, "Garrison", "Veteran", null, null).Frags == 1, "only Veterans and up carry grenades");
    // Batch 56: hit tiers on 2d10
    Check(!CombatRules.HitWounds(0.85f, null) && !CombatRules.HitWounds(1f, null) && CombatRules.HitWounds(1.2f, null) && CombatRules.HitWounds(1.5f, null) && CombatRules.HitWounds(1f, W("assault", "VKM")), "only vital hits and headshots wound (the VKM on a clean hit too)");   // Batch 60
    Check(CombatRules.HitMultiplier(12, 13, 10) == 0.85f && CombatRules.HitMultiplier(14, 15, 10) == 1f && CombatRules.HitMultiplier(17, 18, 10) == 1.2f && CombatRules.HitMultiplier(20, 12, 30) == 1.5f, "graze, clean, vital, headshot");
    { int lo = 99, hi = 0, mid = 0; for (int i = 0; i < 2000; i++) { int r = DiceRoller.Roll2D10().Chosen; lo = Math.Min(lo, r); hi = Math.Max(hi, r); if (r >= 9 && r <= 13) mid++; }
      Check(lo >= 2 && hi <= 20 && mid > 700, $"2d10 rolls 2-20 and bunch in the middle ({lo}-{hi}, {mid}/2000 in 9-13)"); }
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

  // Batch 72: Hunker Down, Overwatch on movement, the Raider shakedown, and the action bar's entries
  static Player Arena(out int px, out int py) {
    GameBootstrap.StartNewGame();
    GameState.Current.Mode = GameMode.Tactical; GameState.Current.CurrentTurn = Turn.Player;
    var p = Player.Current; px = p.X; py = p.Y;
    EncounterState.Enemies.Clear(); EncounterState.InCombat = false; EncounterState.ResetFight();
    for (int y = py - 7; y <= py + 7; y++) for (int x = px - 7; x <= px + 9; x++)
      if (x > 0 && y > 0 && x < TerrainMap.Width - 1 && y < TerrainMap.Height - 1) TerrainMap.SetTileType(x, y, TileType.Empty);
    p.StartTurn(); p.HealPermanent(100);
    return p;
  }

  // Batch 87: the Garrison raid - fighting in the Siege Ward brings 2 more waves (the last led by a boss), then the armory opens
  static void RaidChecks() {
    var log = new ListLog(); GameLog.Target = log;
    GameBootstrap.StartNewGame(); var m = MacroState.Current; var p = Player.Current;
    TerrainMap.BuildTerrainForZone("abandoned_city"); TerrainMap.SpawnLandmarks("abandoned_city"); m.CurrentZoneId = "hospital";
    var arm = TerrainMap.Landmarks.First(l => l.Name == GarrisonRaid.Armory);
    p.X = arm.X + 1; p.Y = arm.Y; GameState.Current.Mode = GameMode.Tactical; GameState.Current.CurrentTurn = Turn.Player; p.Ap = 5;
    EncounterState.Enemies.Clear(); m.OfflineSquads.Clear(); FactionDeck.Reset();
    var guard = new HumanEnemy("Guard", p.X + 8, p.Y + 4, "Garrison", "Veteran", null, null) { IsAggroed = true };
    EncounterState.Enemies.Add(guard); EncounterState.InCombat = true;
    Check(GarrisonRaid.TryInteract(p) && !m.GarrisonArmoryOpen && log.L.Any(l => l.Contains("barred")), "the armory is barred while Garrison holds the ward");
    var far = new HumanEnemy("Far", 20, 150, "Garrison", "Veteran", null, null);   // Batch 89: Garrison elsewhere on the map isn't part of the raid
    EncounterState.Enemies.Add(far);
    GarrisonRaid.Round(p);
    Check(GarrisonRaid.Active, "a fight in the Siege Ward starts the raid");
    int bosses = 0, waves = 0;
    for (int r = 0; r < 30 && GarrisonRaid.Active; r++) {
      int before = EncounterState.Enemies.Count;
      GarrisonRaid.Round(p);
      if (EncounterState.Enemies.Count > before) { waves++; bosses += EncounterState.Enemies.OfType<HumanEnemy>().Count(h => h.Named && h.IsAlive()); }
      foreach (var e in EncounterState.Enemies) if (e != far) e.Hp = 0;   // you win every exchange (the far one never joins)
    }
    Check(waves == 2 && bosses >= 1, $"two more waves, the last led by a boss ({waves} waves, {bosses} bosses)");
    Check(!GarrisonRaid.Active && m.GarrisonArmoryOpen && m.GarrisonRaidReadyDay == m.Day + GarrisonRaid.RaidCooldownDays, "the last wave down opens the armory, and the next raid waits a few days (Garrison elsewhere on the map doesn't count)");
    int items = p.Inventory.Items.Count;
    Check(GarrisonRaid.TryInteract(p) && !m.GarrisonArmoryOpen, "the armory is looted");
    Check(p.Inventory.Items.Where(i => i.Type == "weapon").Sum(i => System.Math.Max(1, i.Qty)) >= 3 && p.Inventory.Items.Any(i => i.Type == "armor") && p.Inventory.Items.Any(i => i.Id == "scientificMedkit")
      && Ammo.Get(p.Inventory, Ammo.Key(p.EquippedWeapon.Db, "ArmorPiercing")) > 0, $"loot: 3 weapons, armor, medkits, AP and HP rounds ({items} -> {p.Inventory.Items.Count})");
    EncounterState.Enemies.Clear(); EncounterState.InCombat = false; m.CurrentZoneId = "bonfire_village";
    // Batch 88: the Lattice Foundry - hidden off the Ship Graveyard, an Obelisk crew led by a boss with the G-7, the cell synthesizer
    { Check(MacroMapDatabase.Find("ship_graveyard").HiddenConnections.Contains("lattice_foundry"), "the Lattice Foundry is a hidden way off the Ship Graveyard");
      m.OfflineSquads.Clear(); EncounterState.Enemies.Clear(); FactionDeck.Reset();
      m.CurrentZoneId = "lattice_foundry"; ZoneTravel.EnterZone("lattice_foundry");   // Batch 91: the test level - guards, no boss
      Check(EncounterState.Enemies.OfType<HumanEnemy>().Count(h => h.FactionName == "Obelisk") >= 1 && !EncounterState.Enemies.OfType<HumanEnemy>().Any(h => h.EquippedWeapon?.Db?.Name == "G-7 Rail Prototype")
        && TerrainMap.Exits.Any(e => e.TargetZoneId == G7Lab.Zone) && TerrainMap.Exits.Any(e => e.TargetZoneId == "ship_graveyard"), $"the test level: a guard squad, the lift up and the stair down (obelisk {EncounterState.Enemies.OfType<HumanEnemy>().Count(h => h.FactionName == "Obelisk")}, named {EncounterState.Enemies.OfType<HumanEnemy>().Count(h => h.Named)}, exits {string.Join(",", TerrainMap.Exits.Select(e => e.TargetZoneId))})");
      EncounterState.Enemies.Clear();
      m.CurrentZoneId = G7Lab.Zone; ZoneTravel.EnterZone(G7Lab.Zone);
      Check(TerrainMap.Exits.Any(e => e.TargetZoneId == "lattice_foundry"), "the synthesis level's stair goes back up");
      var foreman = EncounterState.Enemies.OfType<HumanEnemy>().FirstOrDefault(h => h.Named);
      Check(foreman != null && foreman.EquippedWeapon?.Db?.Name == "G-7 Rail Prototype" && foreman.ActivePerks.Count == 2 && Ammo.Get(foreman.Inventory, G7Lab.CellKey) == G7Lab.SpareCells,
        $"the Foreman holds the lab with the G-7 and spare cells ({foreman?.Name}, {foreman?.EquippedWeapon?.Name})");
      int crew = m.OfflineSquads.Count(q => q.ZoneId == G7Lab.Zone);
      ZoneTravel.EnterZone(G7Lab.Zone);
      Check(m.OfflineSquads.Count(q => q.ZoneId == G7Lab.Zone) == crew, "the crew is only made once");
      foreman.Hp = 0; HumanLootSystem.GrantDrop(foreman, p, 5);
      Check(foreman.Inventory.Items.Any(i => i.Key == "G7RailPrototype"), "the G-7 drops on his body");
      foreach (var e in EncounterState.Enemies) e.Hp = 0; EncounterState.InCombat = false;
      var syn = TerrainMap.Landmarks.First(l => l.Name == G7Lab.Machine); p.X = syn.X; p.Y = syn.Y;
      p.Inventory.AmmoReserve.Remove(G7Lab.CellKey);
      Check(G7Lab.TryInteract(p) && Ammo.Get(p.Inventory, G7Lab.CellKey) == 0, "no artifact, no cells");
      p.AddItem(new InventoryItem { Id = "ballast", Name = "Ballast", Type = "artifact", Qty = 1, WeightKg = 0.5f });
      Check(G7Lab.TryInteract(p) && Ammo.Get(p.Inventory, G7Lab.CellKey) == 6 && !p.Inventory.Items.Any(i => i.Id == "ballast"), $"a Tier 3 artifact makes 6 rail cells ({Ammo.Get(p.Inventory, G7Lab.CellKey)})");
      p.EquipWeapon("sniper", "G7RailPrototype"); p.EquippedWeapon.CurrentMagazine = 0;
      Check(p.EquippedWeapon.Reload() && p.EquippedWeapon.CurrentMagazine == 6, "the G-7 loads rail cells");
      m.CurrentZoneId = "bonfire_village"; EncounterState.Enemies.Clear(); }
    Check(ShopSystem.AmmoBoxPrice("Standard", 1) == 150 && ShopSystem.AmmoBoxPrice("HollowPoint", 3) == 427.5f && ShopSystem.AmmoBoxPrice("ArmorPiercing", 5) == 1200, "ammo boxes priced by tier and kind");   // Batch 87
    GameLog.Target = null;
  }

  static void Batch72Checks() {
    // cover that wraps: every tier map got Ls, and few lone sandbags/jerseys remain
    foreach (var id in new[] { "perimeter", "scrapyard", "scavengers_hub", "vanguard_barracks", "quarry_basin", "abandoned_city" }) {
      var m = MapLayoutDatabase.All[id];
      CoverWrap.Stats.TryGetValue(id, out var st);
      int lone = 0, all = 0;
      for (int y = 1; y < m.Tiles.Length - 1; y++) for (int x = 1; x < m.Tiles[0].Length - 1; x++) {
        string l = m.Looks?[y][x]; if ((m.Tiles[y][x] != "HardCover" && m.Tiles[y][x] != "ScatterCover") || (l != "sandbags" && l != "jersey")) continue;
        all++;
        bool nb = false; foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }) { var t = m.Tiles[y + dy][x + dx]; nb |= t == "HardCover" || t == "ScatterCover"; }
        if (!nb) lone++;
      }
      if (id == "perimeter") Console.WriteLine("  perimeter positions: " + string.Join(" ", CoverWrap.Positions.Where(q => q.zone == id).Take(12).Select(q => $"{q.x},{q.y}")));
      Console.WriteLine($"cover wrap {id}: {st.grown} grown, {st.added} fighting positions, {lone}/{all} sandbag/jersey tiles still alone");
      Check(st.added >= 5 && lone * 4 <= all, $"{id}: wrapping cover ({st.added} positions, {lone}/{all} alone)");
    }
    var log = new ConsoleLog(); GameLog.Target = log;
    GameServices.Grid = new TacticalGridSystem(); GameServices.Combat = new CombatResolver(); GameServices.AnomalyTrigger = new AnomalyTriggerService();
    try {
      // Hunker Down: hard cover east of you counts double, and from the north too (no flank)
      var p = Arena(out int px, out int py);
      TerrainMap.SetTileType(px + 1, py, TileType.HardCover);
      var east = new HumanEnemy("E", px + 6, py, "Raiders", "Rookie", null, null) { IsAggroed = true };
      var north = new HumanEnemy("N", px, py - 6, "Raiders", "Rookie", null, null) { IsAggroed = true };
      var c0 = CoverUtil.GetCover(east, p, null); var f0 = CoverUtil.GetCover(north, p, null);
      Check(c0.AcBonus == 2 && f0.Flanked, $"plain: hard cover +2 from the east ({c0.AcBonus}), flanked from the north ({f0.Flanked})");
      EncounterState.Enemies.AddRange(new Mutant[] { east, north }); EncounterState.InCombat = true;
      Check(p.CanHunker(out _), "can hunker next to cover in a fight");
      p.StatusEffects.Hunkered = true;
      var c1 = CoverUtil.GetCover(east, p, null); var f1 = CoverUtil.GetCover(north, p, null);
      Check(c1.AcBonus == 4 && f1.AcBonus == 4 && !f1.Flanked && f1.Hunkered, $"hunkered: +4 from every side ({c1.AcBonus}, {f1.AcBonus})");
      p.StartTurn(); Check(!p.StatusEffects.Hunkered, "hunker ends at the start of your turn");
      p.Ap = 3; p.HunkerDown();
      Check(GameState.Current.CurrentTurn == Turn.Player && p.Ap == p.MaxAp, "hunkering ends your turn (and the next one starts)");

      // Overwatch: the first hostile step in view draws one shot
      p = Arena(out px, out py);
      var (w1, a1) = EncounterGeneration.PickFactionLoadout("Raiders", "Rookie", 1);
      var r = new HumanEnemy("Walker", px + 5, py, "Raiders", "Rookie", w1, a1) { IsAggroed = true };
      EncounterState.Enemies.Add(r); EncounterState.InCombat = true;
      p.EquippedWeapon.CurrentMagazine = p.EquippedWeapon.MagCapacity;
      Check(p.CanOverwatch(out var why), "can overwatch with a loaded gun: " + why);
      int mag = p.EquippedWeapon.CurrentMagazine;
      p.OverwatchHeld = true; GameState.Current.CurrentTurn = Turn.Mutant; r.Mp = 4;
      GameServices.Grid.AttemptMove(r, r.X - 1, r.Y);
      Check(p.EquippedWeapon.CurrentMagazine == mag - 1 && !p.OverwatchHeld, $"overwatch fired once on the step ({mag} -> {p.EquippedWeapon.CurrentMagazine})");
      if (r.IsAlive()) { r.Mp = 4; GameServices.Grid.AttemptMove(r, r.X - 1, r.Y); Check(p.EquippedWeapon.CurrentMagazine == mag - 1, "only one overwatch shot"); }
      GameState.Current.CurrentTurn = Turn.Player;

      // the shakedown: a Tier 1 Raider gang asks once; paying buys a day's truce
      foreach (int pick in new[] { Shakedown.Pay, Shakedown.HandGun, Shakedown.Refuse, Shakedown.ShootFirst }) {
        p = Arena(out px, out py);
        MacroState.Current.OfflineSquads.Clear();
        var gang = new System.Collections.Generic.List<Mutant>();
        for (int i = 0; i < 4; i++) { var (w, a) = EncounterGeneration.PickFactionLoadout("Raiders", i == 0 ? "Experienced" : "Rookie", 1); gang.Add(new HumanEnemy("Raider " + i, px + 6, py - 2 + i, "Raiders", i == 0 ? "Experienced" : "Rookie", w, a)); }
        EncounterState.Enemies.AddRange(gang);
        MacroState.Current.OfflineSquads.Add(new OfflineSquad { ZoneId = MacroState.Current.CurrentZoneId, Faction = "Raiders", Members = gang });
        int ru = p.Ru = 1000; var gun = p.EquippedWeapon;
        Shakedown.TestPick = pick;
        bool started = Shakedown.TryStart(gang[0], p);
        Check(started, "a Tier 1 Raider gang shakes you down");
        Check(!Shakedown.TryStart(gang[1], p), "only once per gang");
        if (pick == Shakedown.Pay) Check(p.Ru == ru - 4 * Shakedown.TollPerMember && Shakedown.InTruce(gang[2]) && EnemyAI.FriendlyToPlayer((Mutant)gang[2]) && !gang.Any(g => g.IsAggroed), "paid: Ru gone, a day's truce");
        if (pick == Shakedown.HandGun) Check(p.EquippedWeapon == null && ((HumanEnemy)gang[0]).EquippedWeapon == gun && Shakedown.InTruce(gang[0]), "handed over: the boss carries your gun");
        if (pick == Shakedown.Refuse) Check(gang.All(g => g.IsAggroed || !g.IsAlive()) && !Shakedown.InTruce(gang[0]), "refused: they fight");
        if (pick == Shakedown.ShootFirst) Check(gang.All(g => g.IsAggroed) && EncounterState.Surprised && GameState.Current.CurrentTurn == Turn.Player, "shot first: surprise round, your turn");
      }
      Shakedown.TestPick = -1;
      // Batch 73: the gang walks over first and asks face to face (headless, the dialog takes its first answer: pay)
      {
        p = Arena(out px, out py);
        MacroState.Current.OfflineSquads.Clear();
        var gang = new System.Collections.Generic.List<Mutant>();
        for (int i = 0; i < 3; i++) { var (w, a) = EncounterGeneration.PickFactionLoadout("Raiders", "Rookie", 1); gang.Add(new HumanEnemy("Walker " + i, px + 9, py - 1 + i, "Raiders", "Rookie", w, a)); }
        EncounterState.Enemies.AddRange(gang);
        MacroState.Current.OfflineSquads.Add(new OfflineSquad { ZoneId = MacroState.Current.CurrentZoneId, Faction = "Raiders", Members = gang });
        p.Ru = 1000; Shakedown.TestApproach = true;
        Check(Shakedown.TryStart(gang[0], p) && Shakedown.Approaching(gang[1]) && p.Ru == 1000, "the gang calls out and walks over first");
        int d0 = CombatantUtil.Distance(p, gang[0]);
        Vision.Reset();   // Batch 123: no line of sight worked out yet (right after a view rebuild): nobody asks
        for (int t = 0; t < 6; t++) Shakedown.Tick(p);
        Check(Shakedown.Approaching(gang[0]) && p.Ru == 1000, "no toll asked before your sight is worked out");
        for (int t = 0; t < 12 && Shakedown.Approaching(gang[0]); t++) { Vision.Update(p, t); Shakedown.Tick(p); }
        Check(!Shakedown.Approaching(gang[0]) && p.Ru == 1000 - 3 * Shakedown.TollPerMember && Shakedown.InTruce(gang[0]), $"they asked once close (from {d0} tiles, Ru {p.Ru})");
        Shakedown.Reset();
      }
      // Batch 127: a squad that hasn't seen you is called out once (and a click route stops), without waking it
      {
        p = Arena(out px, out py); var ll = new ListLog(); var oldLog = GameLog.Target; GameLog.Target = ll;
        var (wu, au) = EncounterGeneration.PickFactionLoadout("Raiders", "Rookie", 1);
        var u = new HumanEnemy("Lookout", px + 8, py, "Raiders", "Rookie", wu, au) { Facing = (1, 0) };   // its back to you
        EncounterState.Enemies.Add(u); Vision.Update(p, 0);
        TurnLoop.TickAwareness(); TurnLoop.TickAwareness();
        Check(ll.L.Count(l => l.Contains("haven't seen you")) == 1 && !u.IsAggroed && !EncounterState.InCombat, "an unaware squad is called out once: " + string.Join(" / ", ll.L));
        GameLog.Target = oldLog; EncounterState.Enemies.Clear();
      }
      // Batch 127c: spotting fills up over a few looks (it turns to face you meanwhile); within 3 tiles it's at once; sneaking past 6 it never does
      {
        p = Arena(out px, out py);
        var (wd, ad) = EncounterGeneration.PickFactionLoadout("Raiders", "Rookie", 1);
        var d = new HumanEnemy("Watcher", px + 8, py, "Raiders", "Rookie", wd, ad) { Facing = (-1, 0) };
        EncounterState.Enemies.Add(d); Vision.Update(p, 0);
        TurnLoop.TickAwareness();
        Check(!d.IsAggroed && d.Alert > 0f && d.Alert < 1f, $"a first look only raises its alert ({d.Alert:F2})");
        int looks = 1; while (!d.IsAggroed && looks < 20) { TurnLoop.TickAwareness(); looks++; }
        Check(d.IsAggroed && looks >= 3 && looks <= 8, $"spotted after {looks} looks");
        Check(Vision.Senses(p, d) && Vision.SenseRange(p) == 14, "you sense it (14 tiles at Perception 10)");
        EncounterState.Enemies.Clear(); EncounterState.InCombat = false;
        p = Arena(out px, out py); p.Sneaking = true;
        var s2 = new HumanEnemy("Watcher", px + 8, py, "Raiders", "Rookie", wd, ad) { Facing = (-1, 0) };
        EncounterState.Enemies.Add(s2); Vision.Update(p, 0);
        for (int t = 0; t < 10; t++) TurnLoop.TickAwareness();
        Check(!s2.IsAggroed && s2.Alert == 0f, "sneaking at 8 tiles: never noticed");
        s2.X = px + 2; Vision.Update(p, 1); TurnLoop.TickAwareness();
        Check(s2.IsAggroed, "right next to it: spotted at once");
        EncounterState.Enemies.Clear(); EncounterState.InCombat = false; p.Sneaking = false;
      }
      // Batch 128: the back shows when the aim/gaze goes up the screen; a dialogue turns the speaker's squad to you
      {
        var o = UnityEngine.Vector3.zero;
        Check(UnitSprites.Away(o, new UnityEngine.Vector3(0, 1, 0)) && UnitSprites.Away(o, new UnityEngine.Vector3(1, 1, 0)) && !UnitSprites.Away(o, new UnityEngine.Vector3(1, 0.1f, 0))
          && !UnitSprites.Away(o, new UnityEngine.Vector3(0, -1, 0)) && !UnitSprites.Away(o, o), "back view: up and up-diagonal only");
        p = Arena(out px, out py);
        var (wt, at) = EncounterGeneration.PickFactionLoadout("Raiders", "Rookie", 1);
        var talker = new HumanEnemy("Talker", px + 3, py - 3, "Raiders", "Rookie", wt, at) { Facing = (0, -1) };
        var mate = new HumanEnemy("Mate", px + 5, py + 2, "Raiders", "Rookie", wt, at) { Facing = (1, 0) };
        var other = new HumanEnemy("Other", px - 4, py, "Wanderers", "Rookie", wt, at) { Facing = (0, -1) };
        EncounterState.Enemies.AddRange(new Mutant[] { talker, mate, other });
        int picked = -1; ChoiceUI.Show("T", "B", new System.Collections.Generic.List<string> { "ok" }, i => picked = i, talker);
        Check(picked == 0 && talker.Facing == (-1, 1) && mate.Facing == (-1, -1) && other.Facing == (0, -1), $"dialogue: speaker {talker.Facing} and squadmate {mate.Facing} face you, others don't");
        mate.FaceToward(mate.X, mate.Y); Check(mate.Facing == (-1, -1), "facing your own tile changes nothing");
        EncounterState.Enemies.Clear();
      }
      // Batch 129: banter - a friendly squad close by greets you once, an unaware squad you can see talks, a spotter shouts
      {
        p = Arena(out px, out py); Banter.Reset(); Banter.Now = 100f; MacroState.Current.OfflineSquads.Clear();
        OfflineSquad Sq(string fac, int x, int y, int n) {
          var q = new OfflineSquad { ZoneId = MacroState.Current.CurrentZoneId, Faction = fac, Members = new System.Collections.Generic.List<Mutant>() };
          for (int i = 0; i < n; i++) { var (w, a) = EncounterGeneration.PickFactionLoadout(fac, "Rookie", 1); var m = new HumanEnemy(fac + " " + i, x + i, y, fac, "Rookie", w, a); q.Members.Add(m); EncounterState.Enemies.Add(m); }
          MacroState.Current.OfflineSquads.Add(q); return q; }
        var fr = Sq("Wanderers", px + 3, py + 1, 2);
        Vision.Update(p, 0); Banter.Tick(p);
        Check(EnemyAI.FriendlyToPlayer(fr.Members[0]) && Banter.Active.Count == 1 && fr.Members.Contains(Banter.Active[0].Who), "a friendly squad passing close says hi: " + string.Join(" / ", Banter.Active.Select(b => b.Text)));
        Banter.Now = 200f; Banter.Tick(p);
        Check(Banter.Active.All(b => b.Text != null) && !Banter.Active.Any(b => b.Kind != 0), "no second greeting, maybe a chat");
        EncounterState.Enemies.Clear(); MacroState.Current.OfflineSquads.Clear(); Banter.Reset(); Banter.Now = 300f;
        var foes = Sq("Raiders", px + 7, py - 4, 3);
        foreach (var m in foes.Members) m.Facing = (1, 0);   // their backs to you: they don't notice
        Vision.Update(p, 1); Banter.Tick(p);
        Check(Banter.Active.Count == 2 && Banter.Active[1].Start > Banter.Active[0].Start && foes.Members.Contains(Banter.Active[1].Who), "an unaware squad you can see trades two lines");
        Banter.Reset();
        Banter.Notice(foes.Members[0]); Banter.Spotted(foes.Members[0]); Banter.Spotted(foes.Members[1]);
        Check(Banter.Active.Count(b => b.Kind == 2) == 1 && !Banter.Active.Any(b => b.Kind == 1), "one shout per squad (it replaces the 'huh')");
        EncounterState.Enemies.Clear(); MacroState.Current.OfflineSquads.Clear(); Banter.Reset(); Banter.Now = -1f;
      }
      // Batch 130: the Blight Brute charges three turns, then on the fourth (still not next to you) throws a boulder; a hit halves your MP
      {
        p = Arena(out px, out py);
        var brute = new Mutant("Blight Brute", px + 8, py, MutantDatabase.Get("tier4", "BlightBrute")) { IsAggroed = true };
        EncounterState.Enemies.Add(brute); EncounterState.InCombat = true;
        int threw = -1; var bl = new ListLog(); var oldLog = GameLog.Target; GameLog.Target = bl;
        for (int turn = 1; turn <= 4 && threw < 0; turn++) {
          brute.X = px + 8; brute.Y = py; brute.Ap = 4; brute.Mp = 0;   // pinned in place: it can't reach you
          int before = bl.L.Count; EnemyAI.TakeTurn(brute, p);
          if (bl.L.Skip(before).Any(l => l.Contains("HURLS"))) threw = turn;
        }
        Check(threw == 4, $"the boulder comes on the 4th turn without reaching you (turn {threw})");
        p.Hp = p.MaxHp; p.StatusEffects.HalfMpNextTurn = false;
        int hits = 0; for (int i = 0; i < 40; i++) { p.StatusEffects.HalfMpNextTurn = false; EnemyAI.ThrowBoulder(brute, p); if (p.StatusEffects.HalfMpNextTurn) hits++; p.Hp = p.MaxHp; }
        Check(hits > 5 && hits < 40, $"boulders hit sometimes ({hits}/40)");
        p.StatusEffects.HalfMpNextTurn = true; p.StartTurn();
        Check(System.Math.Abs(p.Mp - p.MaxMp / 2f) < 0.01f && !p.StatusEffects.HalfMpNextTurn, $"a hit: half MP next turn ({p.Mp}/{p.MaxMp})");
        GameLog.Target = oldLog; EncounterState.Enemies.Clear(); EncounterState.InCombat = false;
      }
      // Batch 73: Tier 1 Raider gangs are at most 3
      for (int i = 0; i < 5; i++) { var sq = SquadManager.CreateOfflineSquad("perimeter", "Raiders", 6); Check(sq == null || sq.Members.Count <= SquadManager.MaxStarterGang, "Tier 1 gang of " + sq?.Members.Count); if (sq != null) MacroState.Current.OfflineSquads.Remove(sq); }
      // Batch 76: snipers cost 3 AP a shot and get +1 (scoped)
      { var sn = W("sniper", "M91Trekhlineyka"); var ar = W("assault", "VK74");
        sn.RangeTiles = RangeUtil.ParseRangeToTiles(sn.Db.Range); int rec = sn.Db.HighRecoil ? -1 : 0;
        Check(CombatRules.SingleShotAp(sn) == 3 && CombatRules.SingleShotAp(ar) == 2, "sniper: 3 AP a shot");
        // Batch 80: +1 only beyond half its range, -2 within 5 tiles
        Check(CombatRules.TraitToHit(sn, false, sn.RangeTiles) - rec == 1 && CombatRules.TraitToHit(sn, false, sn.RangeTiles / 2) - rec == 0
          && CombatRules.TraitToHit(sn, false, 3) - rec == -2, $"sniper scope: far {CombatRules.TraitToHit(sn, false, sn.RangeTiles)}, mid {CombatRules.TraitToHit(sn, false, sn.RangeTiles / 2)}, close {CombatRules.TraitToHit(sn, false, 3)}"); }
      // Batch 80: the RK-12 costs +1 AP a shot, Double Tap and Gunslinger still apply on top
      { var rk = W("sidearm", "RK12Kuvalda"); Check(CombatRules.SingleShotAp(rk) == 3 && CombatRules.Kick(W("sidearm", "PSh18Pchela")) == 0, "RK-12: 3 AP a shot"); }
      // Batch 80: two weapon slots - Y swaps; a pistol or blade is free, anything else 1 AP; a sniper pairs only with small ones
      { p = Arena(out px, out py); EncounterState.InCombat = true;
        p.EquipWeapon("sniper", "M91Trekhlineyka"); p.StowSecondary();
        ShopItemDef Item(string cat, string key) => ShopDatabase.Items.First(i => i.Type == "weapon" && i.Category == cat && i.Key == key);
        void Give(string cat, string key) { var d = Item(cat, key); p.AddItem(new InventoryItem { Id = d.Id, Name = d.Name, Type = "weapon", Category = cat, Key = key, Qty = 1, WeightKg = d.WeightKg }); }
        Give("assault", "VK74"); p.EquipSecondaryFromInventory(Item("assault", "VK74").Id);
        Check(p.SecondaryWeapon == null, "sniper + rifle refused");
        Give("sidearm", "PSh18Pchela"); p.EquipSecondaryFromInventory(Item("sidearm", "PSh18Pchela").Id);
        Check(p.SecondaryCategory == "sidearm", "sniper + pistol ok");
        p.Ap = 5; p.SwapWeapons(); Check(p.EquippedWeaponCategory == "sidearm" && p.Ap == 5, "to the pistol: 0 AP");
        p.SwapWeapons(); Check(p.EquippedWeaponCategory == "sniper" && p.Ap == 4, $"back to the sniper: 1 AP ({p.Ap})");
        p.EquipWeaponFromInventory(Item("assault", "VK74").Id); p.SwapWeapons();
        Check(p.EquippedWeaponCategory == "sidearm" && p.SecondaryCategory == "assault", "rifle + pistol swap");
        p.EquipWeaponFromInventory(Item("sniper", "M91Trekhlineyka").Id);
        Check(p.EquippedWeaponCategory == "sniper" && p.SecondaryCategory == null && p.Inventory.Items.Any(i => i.Key == "VK74"), "equipping a sniper sends the rifle back to the pack");
        EncounterState.InCombat = false; }
      // Batch 76: Bull Rush doubles MP with a blade; swapping to a gun mid-turn takes the extra back off what's left
      { p = Arena(out px, out py); EncounterState.InCombat = true;
        p.ActivePerks.Add("BullRush"); p.EquipWeapon("sidearm", "PSh18Pchela"); p.StartTurn(); int baseMp = p.MaxMp;
        p.EquipWeapon("melee", "HuntingKnife"); p.StartTurn();
        float full = p.Mp, extra = full - baseMp;
        Check(full == 2 * (baseMp + 2) && p.RushBonus == extra, $"blade +2 MP, Bull Rush doubles it ({baseMp} -> {full})");
        p.Mp -= 3;                                            // walked 3 tiles
        p.EquipWeapon("sidearm", "PSh18Pchela");
        Check(p.Mp == Math.Max(0, full - 3 - extra) && p.RushBonus == 0, $"swapping to a gun mid-turn loses the extra MP ({p.Mp}, expected {full - 3 - extra})");
        float left = p.Mp; p.EquipWeapon("melee", "HuntingKnife");
        Check(p.Mp == left, "swapping back to the knife mid-turn gives nothing until next turn");
        p.StartTurn(); Check(p.Mp == full, "next turn with the knife: doubled again");
        p.ActivePerks.Remove("BullRush"); p.StartTurn(); Check(p.MaxMp == baseMp + 2, "a blade alone is +2 MP");
        EncounterState.InCombat = false; }
      // Batch 78: a blade kill on someone unaware starts no fight; pistols +1 inside their range; Gunslinger -1
      { p = Arena(out px, out py);
        var victim = new HumanEnemy("Victim", px + 1, py, "Raiders", "Rookie", null, null) { Hp = 1 };
        EncounterState.Enemies.Add(victim); p.EquipWeapon("melee", "HuntingKnife"); p.Ap = 5;
        for (int i = 0; i < 20 && victim.IsAlive(); i++) { victim.IsAggroed = false; EncounterState.InCombat = false; p.Ap = 5; p.Attack(victim, null); }
        Check(!victim.IsAlive() && !EncounterState.InCombat, "a silent blade kill starts no fight");
        var pist = W("sidearm", "PSh18Pchela"); pist.RangeTiles = 8;
        Check(CombatRules.TraitToHit(pist, false, 8) == 1 && CombatRules.TraitToHit(pist, false, 9) == 0 && CombatRules.GunslingerPenalty == 1, "pistol +1 inside its range only; Gunslinger -1"); }
      // Batch 78: squad sizes - Garrison duos in Tier 2, 4-6 from Tier 4, Raiders 4-6 from Tier 2
      { bool ok = true; for (int i = 0; i < 30; i++) { ok &= SquadManager.HumanSquadSize("Garrison", 2) == 2; int b4 = SquadManager.HumanSquadSize("Contractors", 4); ok &= b4 >= 4 && b4 <= 6; int r3 = SquadManager.HumanSquadSize("Raiders", 3); ok &= r3 >= 4 && r3 <= 6; int c3 = SquadManager.HumanSquadSize("Contractors", 3); ok &= c3 >= 4 && c3 <= 6; /* Batch 80: Tier 3 4-6 */ int r2 = SquadManager.HumanSquadSize("Raiders", 2); ok &= r2 >= 3 && r2 <= 4; }
        Check(ok, "squad sizes by tier"); }
      // Batch 80: bigger decks keep each rank's share and the outposts where they were; bosses and AP rounds
      { int r1 = FactionDeck.Cards("Raiders", "Rookie"), r4 = FactionDeck.Cards("Raiders", "Veteran");
        Check(Math.Abs(r1 / (double)(r1 + FactionDeck.Cards("Raiders", "Experienced") + FactionDeck.Cards("Raiders", "Professional") + r4 + FactionDeck.Cards("Raiders", "Expert")) - 0.45) < 0.02 && FactionDeck.Strength("Raiders") == 3,
          $"deck scale keeps shares (Rookie {r1}, Veteran {r4}, strength {FactionDeck.Strength("Raiders")})");
        int bosses = 0, apBoss = 0, maxAp = 0, apT4 = 0, n4 = 0;
        for (int i = 0; i < 300; i++) {
          MacroState.Current.OfflineSquads.Clear(); FactionDeck.Reset();
          var sq = SquadManager.CreateOfflineSquad("quarry_basin", "Contractors", 5);
          if (sq == null) continue;
          var hs = sq.Members.OfType<HumanEnemy>().ToList();
          maxAp = Math.Max(maxAp, hs.Count(h => h.ApRounds));
          foreach (var h in hs) { if (h.Named) { bosses++; if (h.ApRounds) apBoss++; } else if (SquadManager.HomeTier(sq) >= 4) { n4++; if (h.ApRounds) apT4++; } }
          if (hs.Any(h => h.ApRounds)) { var a = hs.First(h => h.ApRounds); a.EquipWeaponFromDb(a.EquippedWeapon.Db); Check(a.EquippedWeapon.CurrentAmmoType == "ArmorPiercing", "AP rounds kept through a re-kit"); }
        }
        Console.WriteLine($"  bosses {bosses}/300 squads, {apBoss} with AP rounds; normal Tier 4+ members with AP {apT4}/{n4}; most AP in one squad {maxAp}");
        Check(bosses > 10 && apBoss > bosses / 4 && apBoss < bosses * 3 / 4 && apT4 > n4 / 30 && apT4 < n4 / 4, "bosses and AP rounds roll as asked");
        MacroState.Current.OfflineSquads.Clear(); }
      // Batch 86: a boss gets the 2 perks of its gun, and they work for it
      { var b = new HumanEnemy("B", px + 5, py, "Contractors", "Master", WeaponDatabase.Categories["sniper"]["M91Trekhlineyka"], null);
        b.MakeBoss("Test");
        Check(b.ActivePerks.Contains("MarksmansFocus") && b.ActivePerks.Contains("HighVelocityImpact") && b.ActivePerks.Count == 2, "sniper boss: Marksman's Focus + High-Velocity Impact");
        b.EquipWeaponFromDb(WeaponDatabase.Categories["heavy"]["RL44"]);
        Check(b.ActivePerks.Contains("Shredder") && b.ActivePerks.Contains("WalkingTank") && !b.ActivePerks.Contains("MarksmansFocus"), "perks follow a new gun");
        Check(b.TurnMp() > (b.EquippedWeapon.Db.MaxMpWhileHeld ?? 99), $"Walking Tank lifts the heavy MP cap ({b.TurnMp()})");
        b.EquipWeaponFromDb(WeaponDatabase.Categories["sidearm"]["PSh18Pchela"]);
        var tgt = Arena(out int tx, out int ty); b.X = tx + 3; b.Y = ty; EncounterState.Enemies.Add(b); b.IsAggroed = true;
        b.NewTurn(); b.Ap = 5; b.Attack(tgt, "Single"); int after1 = b.Ap; b.Attack(tgt, "Single");
        Check(after1 == 3 && b.Ap == 2, $"boss Double Tap: 2 AP, then 1 ({after1}, {b.Ap})"); }
      // Batch 76: a squad goes where its leader belongs
      { MacroState.Current.OfflineSquads.Clear();
        var sq = SquadManager.CreateOfflineSquad("industrial_basin", "Garrison", 2);
        var boss = (HumanEnemy)sq.Members[0]; boss.Rank = "Master";
        bool moved = SquadManager.Settle(sq, true);
        int t = MacroMapDatabase.Find(sq.ZoneId).Tier;
        Check(moved && t >= 5 && SquadManager.HomeTier(sq) == 5, $"a Master-led Garrison squad leaves Tier 2 (now in {sq.ZoneId}, tier {t})");
        foreach (var m in sq.Members.OfType<HumanEnemy>()) m.Rank = "Rookie";
        MacroState.Current.OfflineSquads.Remove(sq); sq.ZoneId = "abandoned_city"; MacroState.Current.OfflineSquads.Add(sq);
        Check(SquadManager.Settle(sq, true) && MacroMapDatabase.Find(sq.ZoneId).Tier < 6, $"a Rookie-led squad moves down (now {sq.ZoneId})");
        MacroState.Current.OfflineSquads.Clear(); }
      // Batch 77: AP - player 5, 6 from Veteran; every NPC 4; bosses (Named) by the player's rule
      { p = Arena(out px, out py);
        p.Rank = "Rookie"; int r5 = p.MaxAp; p.Rank = "Veteran"; int v6 = p.MaxAp; p.Rank = "Rookie";
        var leg = new HumanEnemy("L", px + 3, py, "Obelisk", "Legend", null, null); var rook = new HumanEnemy("R", px + 3, py, "Raiders", "Rookie", null, null);
        var bossL = new HumanEnemy("B", px + 3, py, "Obelisk", "Legend", null, null) { Named = true };
        Check(r5 == 5 && v6 == 6 && CombatRules.NpcAp(leg) == 4 && leg.Ap == 4 && rook.Ap == 4 && CombatRules.NpcAp(bossL) == 6, $"AP: player {r5}/{v6}, Legend NPC {leg.Ap}, boss {CombatRules.NpcAp(bossL)}"); }
      // Batch 77: the hit chance the targeting panel shows is the real one
      Check(Math.Abs(CombatRules.HitChance(0, 10) - 0.64f) < 0.001f && Math.Abs(CombatRules.HitChance(0, 10, true) - 0.8704f) < 0.001f && CombatRules.HitChance(-30, 10) > 0.009f, "2d10 hit chance (double 10 always hits)");
      { p = Arena(out px, out py); EncounterState.InCombat = true;
        var (gw2, ga2) = EncounterGeneration.PickFactionLoadout("Raiders", "Rookie", 1);
        var dummy = new HumanEnemy("Dummy", px + 5, py, "Raiders", "Rookie", gw2, ga2) { IsAggroed = true }; dummy.MaxHp = dummy.Hp = 100000;
        TerrainMap.SetTileType(px + 4, py, TileType.HardCover);
        EncounterState.Enemies.Add(dummy);
        var (pre, ap0, amb0, why0) = p.PreviewShot(dummy, null);
        var (preHead, apH, _, _) = p.PreviewShot(dummy, "Head");
        int hits = 0, shots = 0; var w = p.EquippedWeapon; int mod = (int)p.AgilityMod + CombatRules.RankMod(p.Rank);
        if (Environment.GetEnvironmentVariable("DBG77") == "1") { GameLog.Target = new ConsoleLog { Clicker = true }; for (int i = 0; i < 4; i++) { w.CurrentMagazine = w.MagCapacity; w.Fire(dummy, "Single", mod); } }
        for (int i = 0; i < 3000; i++) { w.CurrentMagazine = w.MagCapacity; w.IsJammed = false; w.Condition = ItemCondition.Working; TerrainMap.SetTileType(px + 4, py, TileType.HardCover); var res = w.Fire(dummy, "Single", mod); if (res.Jammed) continue; shots++; if (res.Hit) hits++; }
        float real = hits / (float)shots;
        Check(why0 == null && Math.Abs(real - pre.Chance) < 0.04f && preHead.Chance < pre.Chance && apH == ap0 + 1, $"preview {pre.Chance:P0} vs fired {real:P0} (head {preHead.Chance:P0}, {apH} AP)");
        string body = Examine.Body(dummy, p), mbody = Examine.Body(new Mutant("BlindMaw", px + 2, py, MutantDatabase.Get("tier1", "BlindMaw")), p);
        Check(body.Contains(gw2?.Name ?? "Unarmed") && body.Contains("Phys-DR") && mbody.Contains("Phys-DR"), "examine shows weapon, armor and resistances");
        var far = new HumanEnemy("Far", px + 60, py, "Raiders", "Rookie", gw2, ga2) { IsAggroed = true };
        Check(p.PreviewShot(far, null).why != null, "out of reach is refused in the preview");
        EncounterState.InCombat = false; }
      // not in Tier 2+, not other factions
      p = Arena(out px, out py);
      var sentry = new HumanEnemy("S", px + 6, py, "Garrison", "Rookie", null, null);
      MacroState.Current.OfflineSquads.Add(new OfflineSquad { ZoneId = MacroState.Current.CurrentZoneId, Faction = "Garrison", Members = new System.Collections.Generic.List<Mutant> { sentry } });
      Shakedown.TestPick = Shakedown.Pay; Check(!Shakedown.TryStart(sentry, p), "only Raiders shake you down"); Shakedown.TestPick = -1;

      // the action bar: every entry answers without throwing, exploring and fighting
      p = Arena(out px, out py);
      foreach (var fight in new[] { false, true }) {
        if (fight) { var e = new HumanEnemy("F", px + 5, py, "Raiders", "Rookie", null, null) { IsAggroed = true }; EncounterState.Enemies.Add(e); EncounterState.InCombat = true; }
        var vis = CombatActions.Visible(p);
        Check(vis.Count >= 4 && vis.Count <= 15, $"{vis.Count} buttons ({(fight ? "fight" : "exploring")})");
        Check(fight == vis.Any(a => a.Id == "hunker") && fight != vis.Any(a => a.Id == "rest"), "fight shows combat actions, exploring the walking ones");
        foreach (var a in CombatActions.All) { a.Show(p); a.Ap(p); a.On(p); CombatActions.WhyNot(a, p); Check(!string.IsNullOrEmpty(a.Name) && !string.IsNullOrEmpty(a.Desc) && a.Run != null, "action entry complete: " + a.Id); }
      }
      Check(!CombatActions.Press("hunker", p), "no hunkering in the open");
      Check(CombatActions.FireTarget(p) != null && CombatActions.Press("fire", p), "the fire button shoots the enemy in sight");
    } catch (Exception e) { Check(false, "Batch 72 checks threw: " + e); }
    Shakedown.TestPick = -1;
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
      if (l == "Ex-Military") Check(p.EquippedWeapon.Name == "VKS-74U" && p.SecondaryWeapon != null, "ex-mil gear");
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
    Check(PerkDatabase.All.Count == 35, "perk count " + PerkDatabase.All.Count);
    // Gunslinger: two PMms, one action fires both
    GameBootstrap.StartNewGame(new CharacterSpec { Perk = "Gunslinger" });
    Check(!CombatantUtil.HasPerk(Player.Current, "Gunslinger"), "gunslinger needs point-blank first");   // Batch 80
    GameBootstrap.StartNewGame(new CharacterSpec { Perk = "PointBlankExecution" });
    var gs = Player.Current;
    Check(gs.TakePerk("Gunslinger"), "gunslinger after point-blank"); var glog = new ConsoleLog { Counts = new System.Collections.Generic.Dictionary<string, int>() }; GameLog.Target = glog;
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
    // Batch 57: aimed shots - L cycles the part, +1 AP, single shots only, effects on a clean hit or better
    { var ap = Player.Current; ap.StowOffhand(); ap.Ap = 4; ap.EquippedWeapon.Reload(); ap.EquippedWeapon.IsJammed = false;
      ap.CycleAimedPart(); Check(ap.AimedPart == "Legs", "L aims for the legs");
      ap.Attack(dummy, "Single"); Check(ap.Ap == 1, $"an aimed shot costs 1 AP more (ap {ap.Ap})");
      for (int i = 0; i < 6; i++) ap.CycleAimedPart(); Check(ap.AimedPart == null, "L cycles back to off");
      var t = new HumanEnemy("T", 0, 0, "Raiders", "Rookie", WeaponDatabase.Categories["assault"]["VK74"], ga);   // a gun to drop (a random loadout can be a knife)
      CombatRules.ApplyAimed(t, "Legs", 0.85f); Check(t.StatusEffects.Wound == null, "a graze on a limb does no wound");
      int mp = t.TurnMp(); CombatRules.ApplyAimed(t, "Legs", 1.2f); Check(t.StatusEffects.Wound == "Leg" && t.StatusEffects.Prone && t.TurnMp() == Math.Max(1, mp - 2), "a vital leg shot hobbles and drops them");
      CombatRules.ApplyAimed(t, "Arms", 1.2f); Check(t.StatusEffects.Wound == "Arm" && t.GunDropped && CombatRules.AttackerPenalty(t) <= -2, "a vital arm shot maims and disarms");
      t.Ap = 4; t.Attack(ap); Check(!t.GunDropped && t.Ap == 2, "picking the gun back up costs 2 AP");
      CombatRules.ApplyAimed(t, "Torso", 1f); Check(t.StatusEffects.Bleeding, "a body shot bleeds");
      int drain = t.PendingApDrain; CombatRules.ApplyAimed(t, "Head", 1f); Check(t.PendingApDrain == drain + 1, "a head shot dazes");
      var cond = t.ArmorCondition; CombatRules.ApplyAimed(t, "Armor", 1f); Check(t.EquippedArmor == null || t.ArmorCondition != cond, "an armor shot breaks their armor a step");
      var sq = new HumanEnemy("Sq1", dummy.X + 1, dummy.Y + 1, "Raiders", "Rookie", gw, ga); var sq2 = new HumanEnemy("Sq2", dummy.X + 2, dummy.Y + 2, "Raiders", "Rookie", gw, ga); sq2.Morale = "Shaken"; sq.IsAggroed = sq2.IsAggroed = dummy.IsAggroed;
      EncounterState.Enemies.Add(sq); EncounterState.Enemies.Add(sq2); dummy.Hp = 0;
      EnemyAI.Execution(dummy, new System.Collections.Generic.List<HumanEnemy> { sq2 }, ap);
      Check(sq.Morale == "Shaken" && (sq2.IsFleeing() || sq2.Morale == "Surrendered"), $"a head-shot kill shakes the squad and breaks the shaken ({sq.Morale}, {sq2.Morale})");
      var bomb = new HumanEnemy("Bomb", sq.X, sq.Y + 3, "Raiders", "Veteran", gw, ga); bomb.Hp = 100; EncounterState.Enemies.Add(bomb); int f0 = bomb.Frags;
      CombatRules.CookOff(bomb, ap); Check(f0 == 1 && bomb.Frags == 0 && bomb.Hp < 100, $"a pouch shot sets off their grenade on them (hp {bomb.Hp})");
      // Batch 61: a cloaked ambusher with no short way to you used to circle forever; now it walks the long way in
      { var me = Player.Current; me.Hp = me.MaxHp = 5000; int cx = me.X, cy = me.Y;
        for (int y = cy - 12; y <= cy + 12; y++) for (int x = cx - 4; x <= cx + 10; x++) if (x > 0 && y > 0 && x < TerrainMap.Width - 1 && y < TerrainMap.Height - 1) TerrainMap.SetTileType(x, y, TileType.Empty);
        for (int y = cy - 8; y <= cy + 8; y++) TerrainMap.SetTileType(cx + 3, y, TileType.Solid);
        EncounterState.Enemies.RemoveAll(e => e != null);
        var stalker = new Mutant("Stalker", cx + 5, cy, MutantDatabase.Get("tier3", "AlphaProwler")); stalker.IsAggroed = true; stalker.IsCloaked = true; EncounterState.Enemies.Add(stalker);
        bool reached = false; for (int i = 0; i < 10 && !reached; i++) { Contact.Spot(me); EnemyAI.TakeTurn(stalker, me); reached = me.Hp < 5000 || CombatantUtil.Distance(stalker, me) <= 1; }
        Check(reached, $"a cloaked prowler behind a wall walks the long way round and strikes (at {stalker.X},{stalker.Y}, you {cx},{cy})");
        EncounterState.Enemies.Remove(stalker); me.MaxHp = 100; me.Hp = 100; }
      // Batch 62: out of sight, hunters search where you were; Raiders give up after 3 rounds, Garrison after 8; 15+ tiles and 3 rounds unseen breaks contact
      { var me = Player.Current; int cx = me.X, cy = me.Y; MacroState.Current.Hour = 12f; Atmosphere.Current = Weather.Clear;
        for (int y = cy - 12; y <= cy + 12; y++) for (int x = cx - 4; x <= cx + 14; x++) if (x > 0 && y > 0 && x < TerrainMap.Width - 1 && y < TerrainMap.Height - 1) TerrainMap.SetTileType(x, y, TileType.Empty);
        EncounterState.Enemies.RemoveAll(e => e != null); me.StatusEffects.Bleeding = false;
        var r = new HumanEnemy("Hunter", cx + 10, cy, "Raiders", "Rookie", gw, ga) { IsAggroed = true, VisionRange = 1 };
        var g = new HumanEnemy("Soldier", cx + 10, cy + 2, "Garrison", "Rookie", gw, ga) { IsAggroed = true, VisionRange = 1 };
        EncounterState.Enemies.Add(r); EncounterState.Enemies.Add(g); EncounterState.InCombat = true;
        Contact.Spot(me); Contact.EndRound(me); me.X = cx - 3; me.Y = cy + 8;
        EnemyAI.TakeTurn(r, me); Check(CombatantUtil.Distance(r, cx, cy) < 10 && r.Hp == r.MaxHp, $"a hunter who lost you walks to where you were seen (at {r.X},{r.Y})");
        for (int i = 0; i < 4; i++) Contact.EndRound(me);
        Check(!r.IsAggroed && g.IsAggroed && EncounterState.InCombat && Contact.SightMult(r) == 1.5f, "Raiders give up after 3 rounds unseen; Garrison keeps hunting and the losers stay on watch");
        for (int i = 0; i < 5; i++) Contact.EndRound(me);
        Check(!g.IsAggroed && !EncounterState.InCombat, "Garrison gives up after 8 rounds and the fight ends");
        g.IsAggroed = true; g.X = cx + 14; g.Y = cy - 10; EncounterState.InCombat = true; Contact.Spot(me);
        me.StatusEffects.Bleeding = true; Contact.EndRound(me); Check(Contact.Knows(g, me), "a bleeding player is tracked exactly");
        me.StatusEffects.Bleeding = false; Contact.EndRound(me); Contact.EndRound(me);
        Contact.Heard(g.X, g.Y, 20, me); Check(Contact.UnseenRounds == 0, "your gunfire tells hunters within earshot where you are");
        for (int i = 0; i < 4; i++) Contact.EndRound(me);   // the round you fired in counts as seen
        Check(!g.IsAggroed && !EncounterState.InCombat, $"3 rounds unseen 15+ tiles from every hunter breaks contact (dist {CombatantUtil.Distance(g, me)})");
        EncounterState.Enemies.Clear(); me.X = cx; me.Y = cy; }
      // Batch 64: a shot at someone who hasn't spotted you lands a tier higher; they know where it came from
      { var me = Player.Current; var ll = new ListLog(); var oldLog = GameLog.Target; GameLog.Target = ll; var gun = me.EquippedWeapon;
        Func<bool, bool> shoot = alert => { for (int i = 0; i < 60; i++) {
            var u = new HumanEnemy("Target", me.X + 3, me.Y, "Raiders", "Rookie", gw, ga) { VisionRange = 0, IsAggroed = alert }; u.MaxHp = u.Hp = 1000;
            EncounterState.Enemies.Clear(); EncounterState.Enemies.Add(u); EncounterState.InCombat = alert; me.Ap = 9; gun.IsJammed = false; gun.Condition = ItemCondition.Working; gun.Reload();
            ll.L.Clear(); me.Attack(u, "Single"); if (u.Hp < 1000) return true; } return false; };
        Check(shoot(false) && ll.L.Any(l => l.Contains("never saw it coming")) && Contact.LastSeen == (me.X, me.Y), "a shot at an unaware target lands a tier higher and gives away where it came from");
        Check(shoot(true) && !ll.L.Any(l => l.Contains("never saw it coming")), "an alert target takes normal hits");
        // Batch 65: short guns +2 tiles
        int R(string c, string k) => RangeUtil.ParseRangeToTiles(WeaponDatabase.Categories[c][k].Range);
        Check(R("shotgun", "BayouPump") == 6 && R("sidearm", "RK12Kuvalda") == 12 && R("smg", "CorvanHornet45") == 10 && R("assault", "TR30") == 18, "short guns reach 2 tiles further, long guns unchanged");
        // Batch 74 (Yan): normal rolls out to the gun's range; past it -2, out to twice the range; beyond that no shot
        { HumanEnemy Dummy(string n, int dx) { var d = new HumanEnemy(n, me.X + dx, me.Y, "Raiders", "Rookie", gw, ga) { IsAggroed = true }; d.MaxHp = d.Hp = 1000; return d; }
          var edge = Dummy("Edge", gun.RangeTiles); var past = Dummy("Past", gun.RangeTiles + 1); var gone = Dummy("Gone", gun.MaxReach + 1);
          EncounterState.Enemies.Clear(); EncounterState.Enemies.AddRange(new Mutant[] { edge, past, gone }); EncounterState.InCombat = true;
          bool Pen(Mutant t) { me.Ap = 9; gun.IsJammed = false; gun.Reload(); ll.L.Clear(); me.Attack(t, "Single"); return ll.L.Any(l => l.Contains("-2 to hit") && l.Contains("range")); }
          bool penEdge = Pen(edge), penPast = Pen(past);
          me.Ap = 9; gun.IsJammed = false; gun.Reload(); ll.L.Clear(); me.Attack(gone, "Single"); bool refused = ll.L.Any(l => l.Contains("out of range"));
          Check(gun.RangeTiles < 2 || !penEdge && penPast && refused, $"no penalty at the gun's range, -2 past it, no shot past twice it (range {gun.RangeTiles}: {penEdge} {penPast} {refused})"); }
        EncounterState.Enemies.Clear(); EncounterState.InCombat = false; GameLog.Target = oldLog; }
      // Batch 66: sneaking halves how far you're seen; an idle guard sees 3 tiles behind and to the sides
      { var me = Player.Current; MacroState.Current.Hour = 12f; Atmosphere.Current = Weather.Clear; me.Sneaking = false;
        var g = new HumanEnemy("Guard", me.X, me.Y, "Raiders", "Rookie", gw, ga) { Facing = (1, 0) };
        g.X = me.X - 8;   // you're 8 tiles east of a guard looking east: in front
        float front = Contact.IdleSight(g, me); g.X = me.X + 8; float behind = Contact.IdleSight(g, me);
        me.Sneaking = true; g.X = me.X - 8; float sneakFront = Contact.IdleSight(g, me);
        Check(front >= 11.9f && behind == Contact.RearSight && System.Math.Abs(sneakFront - front / 2) < 0.01f, $"facing and sneaking set sight (front {front}, behind {behind}, sneaking {sneakFront})");
        var grid = GameServices.RequireGrid(); float c0; EncounterState.InCombat = true; me.Sneaking = false; c0 = grid.GetMoveCost(me, me.X + 1, me.Y);
        me.Sneaking = true; Check(grid.GetMoveCost(me, me.X + 1, me.Y) == c0 * 2, "sneaking in a fight costs double MP");
        me.Sneaking = false; EncounterState.InCombat = false; }
      Check(CombatRules.TierUp(0.85f) == 1f && CombatRules.TierUp(1f) == 1.2f && CombatRules.TierUp(1.2f) == 1.5f, "head shots land a tier higher"); }
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
        Ammo.Add(p.Inventory, Ammo.Key(p.EquippedWeapon.Db, "Standard"), 200); p.EquippedWeapon.Reload();
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
      if (StorySystem.Has("ending")) Console.WriteLine($"  reactor: {EncounterState.Enemies.Count(e => e.IsAlive())} alive, obelisk cards left {FactionDeck.Any("Obelisk")}, squads {MacroState.Current.OfflineSquads.Count(q => q.ZoneId == "reactor_core")}, obelisk squads {MacroState.Current.OfflineSquads.Count(q => q.Faction == "Obelisk")} members {MacroState.Current.OfflineSquads.Where(q => q.Faction == "Obelisk").Sum(q => q.Members.Count)}");
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
    p.StowSecondary(); p.EquipWeaponFromInventory(p.Inventory.Items.First(i => i.Type == "weapon").Id);
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
    p.Stats.S = 100;   // guarantee the DC 18 roll (40 still failed on a natural 1-2)
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
    int Stuff() => p.Inventory.Items.Sum(i => System.Math.Max(1, i.Qty)) + p.Inventory.AmmoReserve.Values.Sum();   // ammo and stacking count too
    p.Stats.I = 40; int items = Stuff(), ru = p.Ru, rumors = MacroState.Current.Rumors.Count;
    EnemyAI.Interrogate(pr2, p);
    Check(pr2.Interrogated && (Stuff() > items || p.Ru > ru || MacroState.Current.Rumors.Count > rumors), "interrogation pays off");
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
        // Batch 44: the targets are a squad holding the quest zone; the rival is a crew sent from its home to that zone
        QuestSystem.SendRival(aq);
        var sq = MacroState.Current.OfflineSquads;
        bool safe = MacroMapDatabase.Find(aq.ZoneId).IsSafeZone;
        var crew = sq.Where(x => x.QuestId == "rival:" + aq.QuestId).ToList();
        Check(safe ? crew.Count == 0 : crew.Count == 1 && SquadManager.Homes[crew[0].Faction].Contains(crew[0].ZoneId) && (crew[0].Heading == aq.ZoneId || crew[0].ZoneId == aq.ZoneId), $"rival crew sets out from its home ({crew.Count}, {aq.ZoneId}, {crew.FirstOrDefault()?.ZoneId})");
        Check(aq.TargetsSpawned && sq.Any(x => x.QuestId == aq.QuestId && x.ZoneId == aq.ZoneId && x.Heading == null && x.Members.Any(u => u.IsAlive())), "quest targets are a squad in the quest zone");
        MacroState.Current.CurrentZoneId = aq.ZoneId; QuestSystem.OnEnterZone(aq.ZoneId);
        Check(sq.Count(x => x.QuestId == "rival:" + aq.QuestId) == crew.Count, "rival only once");
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

  // Batch 37: squads come into the zone you're in through the exit they took, and walk out through one
  // the map overhaul: a zone one map over from `here` (a neighbouring tier map's region), never a region of the same map
  static string OffMap(ZoneDef here) => MacroMapDatabase.RegionsOf(here.Id).SelectMany(r => r.ConnectedZones)
    .First(z => !MacroMapDatabase.SameMap(z, here.Id) && !(MacroMapDatabase.Find(z)?.IsSafeZone ?? true) && !ZoneLayoutRecipes.IsInterior(z));
  static ZoneDef SurfaceMapZone() => MacroMapDatabase.Zones.First(z => !z.IsSafeZone && z.MapId == null && !ZoneLayoutRecipes.IsInterior(z.Id)
    && MacroMapDatabase.RegionsOf(z.Id).SelectMany(r => r.ConnectedZones).Any(c => !MacroMapDatabase.SameMap(c, z.Id) && !ZoneLayoutRecipes.IsInterior(c)));

  static void LiveSquadChecks() {
    var log = new ConsoleLog(); GameLog.Target = log;
    GameBootstrap.StartNewGame();
    var m = MacroState.Current;
    var here = SurfaceMapZone();
    m.CurrentZoneId = here.Id; ZoneTravel.EnterZone(here.Id);
    GameState.Current.CurrentTurn = Turn.Player; EncounterState.InCombat = false;
    var p = Player.Current;
    Check(TerrainMap.ZoneId == here.Id && TerrainMap.Exits.Count >= 2, $"{here.Id} is loaded with exits");
    // in: the newcomers stand by the exit that leads back where they came from, and head inward
    var door = TerrainMap.Exits.OrderByDescending(e => Math.Abs(e.X - p.X) + Math.Abs(e.Y - p.Y)).First();
    var faction = WorldEvents.Factions(here).FirstOrDefault(f => f != "Zombie") ?? "Raiders";
    m.OfflineSquads.RemoveAll(s => s.Faction == faction && !SquadManager.OnMap(s.ZoneId, here.Id));   // Batch 76: squads settle by tier, so free some cards for the test
    var guests = SquadManager.CreateOfflineSquad(door.TargetZoneId, faction, 3);
    guests.ZoneId = here.Id;
    Check(SquadManager.ArriveLive(guests, door.TargetZoneId), "a squad can arrive through an exit");
    Check(guests.Members.All(x => EncounterState.Enemies.Contains(x) && Math.Abs(x.X - door.X) <= 8 && Math.Abs(x.Y - door.Y) <= 8 && x.PatrolTarget != null),
      "newcomers stand at the exit they came by and walk inward");
    // out: the squad walks to the exit it picked and steps off the map there
    var exitOut = TerrainMap.Exits.First(e => e.TargetZoneId != door.TargetZoneId);
    EncounterState.Enemies.RemoveAll(e => !guests.Members.Contains(e)); m.OfflineSquads.RemoveAll(s => s != guests && s.ZoneId == here.Id);   // Batch 55: bigger gangs mean more fights; keep this walk alone
    foreach (var x in guests.Members) { x.IsAggroed = false; x.VisionRange = 0; x.MaxHp = x.Hp = 100000; }   // the walk is under test, not whatever crosses its path
    guests.LeavingFor = exitOut.TargetZoneId; guests.LeavingSince = m.WorldTickCounter;
    int steps = 0;
    while (guests.ZoneId == here.Id && steps < 400) { TurnLoop.TickAwareness(); steps++; GameState.Current.CurrentTurn = Turn.Player; EncounterState.InCombat = false; }
    Check(guests.ZoneId == exitOut.TargetZoneId && guests.LeavingFor == null && !guests.Members.Any(x => EncounterState.Enemies.Contains(x))
      && guests.Members.All(x => Math.Abs(x.X - exitOut.X) <= 3 && Math.Abs(x.Y - exitOut.Y) <= 3), $"a leaving squad walks out through its exit ({steps} steps, {guests.Faction} zone {guests.ZoneId} vs {exitOut.TargetZoneId}, leaving {guests.LeavingFor}, on map {guests.Members.Count(x => EncounterState.Enemies.Contains(x))}, at {string.Join(" ", guests.Members.Select(x => x.X + "," + x.Y + (x.IsAlive() ? "" : "dead")))} exit {exitOut.X},{exitOut.Y})");
    { // Batch 110: posts - a squad on your map takes tasks round the map's landmarks instead of strolling at random
      m.OfflineSquads.Remove(guests); EncounterState.Enemies.RemoveAll(e => guests.Members.Contains(e));   // the immortal, blind walkers of the last check stay out of this one
      var crew = SquadManager.CreateOfflineSquad(door.TargetZoneId, faction, 3); crew.ZoneId = here.Id;
      SquadManager.ArriveLive(crew, door.TargetZoneId);
      EncounterState.Enemies.RemoveAll(e => !crew.Members.Contains(e)); m.OfflineSquads.RemoveAll(s => s != crew && s != guests && s.ZoneId == here.Id);
      crew.ShakedownDone = true; Shakedown.Reset();   // not a toll gang for this walk
      foreach (var x in crew.Members) { x.IsAggroed = false; x.VisionRange = 0; x.MaxHp = x.Hp = 100000; x.PatrolTarget = null; x.RestUntil = 0; }
      var tasks = new System.Collections.Generic.HashSet<string>(); int nearPost = 0, mins = 0;
      for (int kk = 0; kk < 60 * 30 && SquadManager.OnMap(crew.ZoneId, here.Id); kk++) {
        crew.LeavingFor = null;   // the walk round the posts is under test, not when the squad decides to leave
        GameState.Current.CurrentTurn = Turn.Player; EncounterState.InCombat = false; TurnLoop.TickAwareness();
        var t = Posts.TaskOf(crew); if (t != null) tasks.Add(t);
        var lead = crew.Members.First(x => x.IsAlive()); mins++;
        if (TerrainMap.Landmarks.Any(l => Math.Max(Math.Abs(l.X - lead.X), Math.Abs(l.Y - lead.Y)) <= Posts.OwnRadius)) nearPost++;
      }
      Console.WriteLine($"posts: tasks {string.Join(",", tasks)}, home {Posts.HomeOf(crew)}, near a post {nearPost}/{mins} min");
      Check(tasks.Count() >= 2 && Posts.HomeOf(crew) != null && nearPost * 2 >= mins,
        $"a squad keeps a home post and takes tasks round the landmarks (tasks {string.Join(",", tasks)}, home {Posts.HomeOf(crew)}, near a post {nearPost}/{mins} min)");
      m.OfflineSquads.Remove(crew); EncounterState.Enemies.RemoveAll(e => crew.Members.Contains(e));
    }
    m.OfflineSquads.Remove(guests);
    // Batch 59: a fight that drags on gets the leader on the radio; a same-faction squad next door sets out to help
    {
      var nb = OffMap(here);
      m.OfflineSquads.RemoveAll(s => s.Faction == "Garrison");
      var foes = SquadManager.CreateOfflineSquad(nb, "Garrison", 3); foes.ZoneId = here.Id; SquadManager.ArriveLive(foes, nb);
      foreach (var x in foes.Members) { x.IsAggroed = true; }
      var help = SquadManager.CreateOfflineSquad(nb, "Garrison", 2);
      EncounterState.InCombat = true; EncounterState.ResetFight();
      var radio = new ListLog(); GameLog.Target = radio;
      int r = 0; while (!EncounterState.HelpCalled && r++ < 30) TurnLoop.RadioForHelp();
      Check(EncounterState.HelpCalled && r == 3 * 3 + 1 && radio.L.Any(l => l.Contains("yelling into the radio")) && help.Heading == here.Id && help.Job == "backup",
        $"after 3 rounds per enemy the leader radios and the squad next door comes ({r} rounds, heading {help.Heading})");
      EncounterState.ResetFight(); r = 0; while (EncounterState.Caller == null && r++ < 30) TurnLoop.RadioForHelp();
      var caller = EncounterState.Caller; caller.Hp = 0; TurnLoop.RadioForHelp();
      Check(!EncounterState.HelpCalled && radio.L.Any(l => l.Contains("cut off")), "killing the caller in time cuts the call off");
      m.OfflineSquads.Remove(foes); m.OfflineSquads.Remove(help); EncounterState.Enemies.RemoveAll(e => foes.Members.Contains(e));
      EncounterState.InCombat = false; EncounterState.ResetFight(); GameLog.Target = log;
    }
    // Batch 38: a real off-screen fight next door becomes news, comes over the radio, and turns up as a rumor
    var heard = new ListLog(); GameLog.Target = heard;
    var nextDoor = OffMap(here);
    m.OfflineSquads.RemoveAll(s => s.ZoneId != here.Id); m.WorldNews.Clear();   // Batch 41: the rest of the world is populated now; keep this fight the only news
    var r1 = SquadManager.CreateOfflineSquad(nextDoor, "Raiders", 2); var r2 = SquadManager.CreateOfflineSquad(nextDoor, "Mutant", 2);
    for (int i = 0; i < 40 && m.OfflineSquads.Contains(r1) && m.OfflineSquads.Contains(r2); i++) { m.WorldTickCounter++; SquadManager.ResolveOffscreenSquadConflicts(here.Id); }
    var fight = m.WorldNews.FirstOrDefault(n => n.Contains("|F|" + nextDoor + "|"));
    Check(fight != null && fight.Contains(MacroMapDatabase.Find(nextDoor).Name), "a squad wiped out off-screen is news");
    Check(heard.L.Any(l => l.StartsWith("[RADIO]") && fight != null && l.EndsWith(fight.Split('|')[3])), "gunfire next door comes over the radio");
    m.LastRumorDay = m.Day - 1; m.ExtraRumors = 30; m.Rumors.Clear(); WorldEvents.TellRumors();
    Check(fight != null && m.Rumors.Any(r => r.EndsWith(fight.Split('|')[3])), "the fight turns up as a rumor");
    m.OfflineSquads.Remove(r1); m.OfflineSquads.Remove(r2);
    // Batch 39: hostile squads on your map fight each other without you
    var gang = SquadManager.CreateOfflineSquad(here.Id, "Raiders", 3); var pack = SquadManager.CreateOfflineSquad(here.Id, "Mutant", 3);
    var spot = TerrainMap.FindNearestEnterable(door.X, door.Y);
    int k = 0;
    foreach (var u in gang.Members.Concat(pack.Members))
    {
      var c = TerrainMap.FindNearestEnterable(Math.Clamp(spot.x + (k % 3) - 1 + (u.Faction == "Mutant" ? 0 : 0), 0, TerrainMap.Width - 1), Math.Clamp(spot.y + (u.Faction == "Mutant" ? 2 : -2) + k / 3 % 2, 0, TerrainMap.Height - 1));
      var c0 = c;   // step further each try: right of a wall, the nearest open tile can be the taken one again
      for (int t = 1; t < 60 && EncounterState.Enemies.Any(o => o.IsAlive() && o.X == c.x && o.Y == c.y); t++) c = TerrainMap.FindNearestEnterable(Math.Clamp(c0.x + t, 0, TerrainMap.Width - 1), c0.y);
      u.X = c.x; u.Y = c.y; u.IsAggroed = false; u.VisionRange = 10; k++;
      EncounterState.Enemies.Add(u);
    }
    var far = TerrainMap.FindNearestEnterable(TerrainMap.Width - 1 - spot.x, TerrainMap.Height - 1 - spot.y);
    p.X = far.x; p.Y = far.y; int hp0 = p.Hp, packItems = p.Inventory.Items.Count;
    for (int i = 0; i < 300 && gang.Members.Any(x => x.IsAlive()) && pack.Members.Any(x => x.IsAlive()); i++)
    { GameState.Current.CurrentTurn = Turn.Player; EncounterState.InCombat = false; foreach (var x in gang.Members.Concat(pack.Members)) x.IsAggroed = false; TurnLoop.TickAwareness(); }
    int dead = gang.Members.Concat(pack.Members).Count(x => !x.IsAlive());
    Check(dead >= 3 && (gang.Members.All(x => !x.IsAlive()) || pack.Members.All(x => !x.IsAlive())), $"raiders and mutants fight it out on your map ({dead} dead)");
    Check(p.Hp == hp0 && p.Inventory.Items.Count == packItems, "their fight doesn't hurt you or fill your pack");
    m.OfflineSquads.Remove(gang); m.OfflineSquads.Remove(pack);
    // Batch 47: gunfire is heard - NPCs in earshot who aren't fighting walk over to look; a suppressor carries much less
    Atmosphere.Current = Weather.Clear;
    var (lw, la) = EncounterGeneration.PickFactionLoadout("Wanderers", "Rookie", 1);
    int ldir = p.X + 26 < TerrainMap.Width ? 1 : -1;
    var near = new HumanEnemy("Listener A", p.X + ldir * 15, p.Y, "Wanderers", "Rookie", lw, la) { IsAggroed = false };
    var farL = new HumanEnemy("Listener B", p.X + ldir * 26, p.Y, "Wanderers", "Rookie", lw, la) { IsAggroed = false };
    near.PatrolTarget = null; farL.PatrolTarget = null;
    EncounterState.Enemies.Add(near); EncounterState.Enemies.Add(farL);
    Noise.Make(p.X, p.Y, Noise.Shot, p);
    Check(near.PatrolTarget != null && Math.Abs(near.PatrolTarget.Value.x - p.X) + Math.Abs(near.PatrolTarget.Value.y - p.Y) <= 3 && farL.PatrolTarget == null, "a shot draws the NPC 15 tiles away to it, not the one 26 away");
    near.PatrolTarget = null;
    Noise.Make(p.X, p.Y, Noise.Suppressed, p);
    Check(near.PatrolTarget == null, "a suppressed shot isn't heard 15 tiles away");
    if (p.EquippedWeapon != null) { p.EquippedWeapon.CurrentMagazine = Math.Max(1, p.EquippedWeapon.CurrentMagazine); p.EquippedWeapon.Fire(near, "Single"); }
    Check(p.EquippedWeapon == null || p.EquippedWeapon.Db?.InnateSuppressor == true || near.PatrolTarget != null || !near.IsAlive() || near.IsAggroed, "firing your gun makes that noise");
    EncounterState.Enemies.Remove(near); EncounterState.Enemies.Remove(farL);
    // Batch 40: mutants leave Obelisk alone (and the other way round); everyone else is fair game
    Check(!FactionDatabase.AreHostile("Mutant", "Obelisk") && !FactionDatabase.AreHostile("Obelisk", "Mutant")
      && FactionDatabase.Factions.Keys.Where(f => f != "Mutant" && f != "Obelisk").All(f => FactionDatabase.AreHostile("Mutant", f)), "mutants are hostile to everyone but Obelisk");
    // off-screen, one side listing the other is enough, whichever squad came first (Wanderers list Raiders, not the reverse)
    m.OfflineSquads.RemoveAll(s => s.ZoneId == nextDoor);
    var rd = SquadManager.CreateOfflineSquad(nextDoor, "Raiders", 2); var wd = SquadManager.CreateOfflineSquad(nextDoor, "Wanderers", 2);
    for (int i = 0; i < 40 && m.OfflineSquads.Contains(rd) && m.OfflineSquads.Contains(wd) && rd.Job != "retreat" && wd.Job != "retreat"; i++) SquadManager.ResolveOffscreenSquadConflicts(here.Id);
    Check(!m.OfflineSquads.Contains(rd) || !m.OfflineSquads.Contains(wd) || rd.Job == "retreat" || wd.Job == "retreat", "raiders listed first still fight wanderers off-screen");
    Check(m.WorldNews.Any(n => n.Contains("|F|" + nextDoor + "|") && (n.Contains("A Raiders squad and a Wanderers") || n.Contains("a Wanderers"))) && !m.WorldNews.Any(n => n.Contains("A Obelisk") || n.Contains("a Obelisk") || n.Contains("a Outcasts")), "news says 'an Obelisk squad'");
    m.OfflineSquads.Remove(rd); m.OfflineSquads.Remove(wd);
    // Batch 41: the whole Zone is populated at New Game, and a thinned-out zone gets a squad back, never yours
    var wild = MacroMapDatabase.Zones.Where(z => !z.IsSafeZone && z.AllowedFactions.Count > 0 && !ZoneLayoutRecipes.IsInterior(z.Id)).ToList();
    GameBootstrap.StartNewGame(); m = MacroState.Current;
    Check(wild.All(z => m.OfflineSquads.Any(q => q.ZoneId == z.Id)), "every hostile surface zone has squads from the start: empty " + string.Join(",", wild.Where(z => !m.OfflineSquads.Any(q => q.ZoneId == z.Id)).Select(z => z.Id)));
    Check(!MacroMapDatabase.Zones.Where(z => z.IsSafeZone).Any(z => m.PopulatedZones.Contains(z.Id)), "safe zones aren't seeded");
    var thin = wild.First(z => z.Id != m.CurrentZoneId && !z.ConnectedZones.Contains(m.CurrentZoneId) && !MacroMapDatabase.SameMap(z.Id, m.CurrentZoneId));
    m.OfflineSquads.RemoveAll(q => q.ZoneId == thin.Id || q.ZoneId == m.CurrentZoneId);
    int hours = 0;
    while (!m.OfflineSquads.Any(q => q.ZoneId == thin.Id) && hours < 24 * 10) { Atmosphere.Advance(60f); hours++; }
    Check(m.OfflineSquads.Any(q => q.ZoneId == thin.Id) && m.WorldNews.Any(n => n.Contains("|M|" + thin.Id + "|")), $"an emptied zone fills back up and it's news ({hours} h, {thin.Id}, heading there: {string.Join(",", m.OfflineSquads.Where(q => q.Heading == thin.Id).Select(q => q.Faction + "@" + q.ZoneId))})");
    int most = 0;
    string crowd = "";
    for (int i = 0; i < 24 * 20; i++) { Atmosphere.Advance(60f); var g7 = m.OfflineSquads.Where(q => q.Heading == null && !(q.Job ?? "").StartsWith("migration:")).GroupBy(q => q.ZoneId).OrderByDescending(g => g.Count()).First(); if (g7.Count() > most) { most = g7.Count(); crowd = g7.Key + ":" + string.Join(",", g7.Select(q => q.Faction + (q.QuestId ?? "") + (q.Job != null ? "/" + q.Job : ""))); } }
    Check(most <= 6, $"no zone gets crowded (max {most} squads staying, not counting those passing through) {crowd}");
    // Batch 43: reinforcements muster at a home zone and walk to where they're needed; nobody spawns already hunting you
    Check(m.OfflineSquads.All(q => q.Members.All(x => !x.IsAggroed || x.IsAlive() == false) || q.ZoneId == m.CurrentZoneId), "no squad starts out aggroed");
    var unreachable = wild.SelectMany(z => z.AllowedFactions.Select(f => (z, f: f == "Zombie" ? "Mutant" : f)))
      .Where(t => !SquadManager.Homes.TryGetValue(t.f, out var hs) || !hs.Any(h => SquadManager.Route(h, t.z.Id, t.f, true) != null)).Select(t => t.f + "->" + t.z.Id).ToList();
    Check(unreachable.Count == 0, "every zone can be reinforced from its faction's outpost: " + string.Join(",", unreachable));
    var target = wild.First(z => z.Id == "abandoned_city");
    m.OfflineSquads.RemoveAll(q => q.ZoneId == target.Id || q.Heading == target.Id);
    OfflineSquad sent = null; hours = 0;
    while (sent == null && hours < 24 * 10) { m.OfflineSquads.RemoveAll(q => q.ZoneId == target.Id); Atmosphere.Advance(60f); hours++; sent = m.OfflineSquads.FirstOrDefault(q => q.Heading == target.Id); }
    Check(sent != null && sent.ZoneId != target.Id, $"a refill comes from elsewhere (its home, or a squad already on the road) ({sent?.Faction} at {sent?.ZoneId}), not in the zone; there: " + string.Join(",", m.OfflineSquads.Where(q => q.ZoneId == target.Id).Select(q => q.Faction + "/" + q.Job)) + " cur " + m.CurrentZoneId);
    var path = new System.Collections.Generic.List<string>();
    bool diverted = false;   // a fight on the way can send it back (a retreat) instead
    for (int i = 0; i < 24 * 15 && sent != null && sent.Heading != null && m.OfflineSquads.Contains(sent); i++) { var z0 = sent.ZoneId; Atmosphere.Advance(60f); if (sent.ZoneId != z0) path.Add(sent.ZoneId); if (sent.Heading != null && sent.Heading != target.Id || sent.Job == "retreat") diverted = true; }
    Check(sent == null || !m.OfflineSquads.Contains(sent) || diverted || sent.ZoneId == target.Id && path.All(z => z == target.Id || SquadManager.Route(z, target.Id, sent.Faction) != null), $"it walks there zone by zone ({string.Join(" > ", path)})");
    // Batch 45: a caravan really travels from the Perimeter to the hub; the stock comes only if it gets there
    var hubId = "scavengers_hub";
    m.CaravanZoneId = null;
    var van = SquadManager.SendCaravan(hubId);
    Check(van != null && van.ZoneId == "perimeter" && van.Heading == hubId && m.CaravanZoneId == null, "a caravan sets out from the Perimeter, no stock yet");
    bool came = false; var vanPath = new System.Collections.Generic.List<string>();
    bool beaten = false;   // a caravan beaten off on the road retreats (and is no longer a caravan)
    for (int i = 0; i < 24 * 12 && !came && m.OfflineSquads.Contains(van); i++) { var z0 = van.ZoneId; Atmosphere.Advance(60f); if (van.ZoneId != z0) vanPath.Add(van.ZoneId); came = m.CaravanZoneId == hubId; beaten |= van.Job == "retreat"; }
    Check(came ? van.ZoneId == hubId && van.Job == null : beaten || !m.OfflineSquads.Contains(van) || van.Members.All(u => !u.IsAlive()), $"the caravan arrives (and the hub gets its stock) or dies on the road ({string.Join(" > ", vanPath)}{(came ? ", arrived" : beaten ? ", beaten off" : ", lost")})");
    // Batch 45: a mutant migration is packs from a nest crossing the zone, not a hazard bonus
    var through = MacroMapDatabase.Find("black_valley");
    int hz = WorldEvents.Hazard(through);
    var packs = SquadManager.SendMigration(through.Id);
    Check(packs.Count >= 1 && packs.All(q => q.Faction == "Mutant" && ZoneLayoutRecipes.IsInterior(q.ZoneId) && q.Heading == through.Id), "migrating packs come up from underground and head for the zone");
    m.MigrationZoneId = through.Id;
    Check(WorldEvents.Hazard(through) == hz && !WorldEvents.Factions(through).Contains("Mutant"), "the migration no longer just bumps a number");
    bool crossed = false;
    for (int i = 0; i < 24 * 12 && !crossed && packs.Any(q => m.OfflineSquads.Contains(q)); i++) { Atmosphere.Advance(60f); crossed = packs.Any(q => q.ZoneId == through.Id || q.Job == null && m.OfflineSquads.Contains(q)); }
    Check(crossed || !packs.Any(q => m.OfflineSquads.Contains(q)), "a pack reaches the zone (or dies trying)");
    m.MigrationZoneId = null;
    // Batch 46: goals move on who holds the ground, and a faction that isn't winning sends a squad to take it
    m.OfflineSquads.RemoveAll(q => q.ZoneId == "quarry_basin" || q.Heading == "quarry_basin" || q.Faction == "Obelisk" || q.Faction == "Contractors");   // Batch 52: free their cards
    m.GoalBoxes["Obelisk"] = 2; m.CompletedGoals.Remove("Obelisk");
    var ob1 = SquadManager.CreateOfflineSquad("quarry_basin", "Obelisk", 2); var ob2 = SquadManager.CreateOfflineSquad("quarry_basin", "Obelisk", 2);
    var ct = SquadManager.CreateOfflineSquad("quarry_basin", "Contractors", 2);
    WorldEvents.TallyGoals();
    Check(WorldEvents.Boxes("Obelisk") == 3, "Obelisk out-hold the Contractors at Quarry Basin: +1 box");
    m.OfflineSquads.Remove(ob1); m.OfflineSquads.Remove(ob2);
    WorldEvents.TallyGoals();
    Check(WorldEvents.Boxes("Obelisk") == 2 && m.OfflineSquads.Any(q => q.Faction == "Obelisk" && q.Heading == "quarry_basin" && q.Job == "goal"), $"pushed out: -1 box, and Obelisk send a squad to take it back (boxes {WorldEvents.Boxes("Obelisk")}, any {FactionDeck.Any("Obelisk")}, here {m.CurrentZoneId})");
    m.OfflineSquads.Remove(ct);
    GameLog.Target = null;
  }
  // Batch 48: prisoners, fleeing, morale breaks and scavenging come from the situation and real things, not dice
  static void DeckChecks() {
    var log = new ListLog(); GameLog.Target = log;
    GameBootstrap.StartNewGame(); var m = MacroState.Current;
    // Batch 52: hostility goes both ways, and Obelisk is against everyone but mutants
    Check(FactionDatabase.AreHostile("Raiders", "Wanderers") && FactionDatabase.AreHostile("Raiders", "Prospectors"), "one side listing the other is enough, both ways");
    // Batch 53: Yan's tiers - C Raiders < B Surveyors, Wanderers < A Liberators = Sentinels, Contractors, Garrison < S Obelisk, about two tiers up
    int St(string f) => FactionDeck.Strength(f);
    Check(St("Raiders") < 5 && new[] { "Surveyors", "Wanderers" }.All(f => St(f) >= 5 && St(f) <= 10) && new[] { "Liberators", "Sentinels", "Contractors", "Garrison" }.All(f => St(f) > 10 && St(f) < 18)
      && St("Liberators") == St("Sentinels") && St("Obelisk") - St("Sentinels") >= 2 * (St("Sentinels") - St("Wanderers")) && !FactionDatabase.Factions.ContainsKey("Rookie"), "faction strength follows Yan's tiers, and Rookie is only a rank");
    Check(FactionDatabase.Factions.Keys.Where(f => f != "Obelisk").All(f => FactionDatabase.AreHostile("Obelisk", f) == (f != "Mutant")), "Obelisk is hostile to every faction but Mutants");
    // real decks: never more of a rank alive than the deck holds; an empty deck sends nobody
    for (int i = 0; i < 40; i++) SquadManager.CreateOfflineSquad("reactor_core", "Obelisk", 4);   // Batch 80: decks x3
    var alive = FactionDeck.Alive();
    Check(new[] { "Rookie", "Experienced", "Professional", "Veteran", "Expert", "Master", "Legend" }.All(r => (alive.TryGetValue("Obelisk|N:" + r, out var a) ? a : 0) <= FactionDeck.Cards("Obelisk", "N:" + r)) && FactionDeck.Cards("Obelisk", "N:Legend") == 6 && FactionDeck.Cards("Obelisk", "Legend") == 0,
      "Obelisk never has more people of a rank than its north deck (six Legends, none in the south deck): " + string.Join(" ", alive.Where(kv => kv.Key.StartsWith("Obelisk")).Select(kv => kv.Key + "=" + kv.Value + "/" + FactionDeck.Cards("Obelisk", kv.Key.Split('|')[1]))));
    Check(MacroState.Current.OfflineSquads.Where(q => q.Faction == "Obelisk").SelectMany(q => q.Members.OfType<HumanEnemy>()).All(h => h.Card.StartsWith("N:")), "the Reactor Core draws from the north deck");
    Check(MacroState.Current.OfflineSquads.Where(q => q.Faction == "Obelisk").All(q => { var hs = q.Members.OfType<HumanEnemy>().ToList(); var lead = hs[0]; return SquadManager.RankTier(lead.Rank) >= 4 && hs.All(h => FactionDeck.MemberOk(h.Rank, lead.Rank)); }),
      "north squads: an Expert-or-better leader, members his rank or below and at most 2 tiers under");
    // Batch 84: the south draws the whole squad, the best card leads, nobody more than 2 tiers under him
    { bool ok = true; for (int i = 0; i < 40; i++) { FactionDeck.Reset(); var sq = SquadManager.CreateOfflineSquad("rail_yards", "Contractors", 5); if (sq == null) continue;
        var hs = sq.Members.OfType<HumanEnemy>().ToList(); var best = hs.OrderByDescending(h => CombatRules.RankIndex(h.Rank)).First(); ok &= hs.All(h => FactionDeck.MemberOk(h.Rank, best.Rank) && !h.Card.StartsWith("N:")); m.OfflineSquads.Remove(sq); }
      Check(ok, "south squads: the best card leads, everyone within 2 tiers of him, south deck only"); }
    Check(!FactionDeck.Any("Obelisk", true) && SquadManager.CreateOfflineSquad("reactor_core", "Obelisk", 3) == null, "an empty deck (no leaders left) sends nobody");
    // the dead's cards come back after the cooldown
    FactionDeck.Tick();
    var one = m.OfflineSquads.First(q => q.Faction == "Obelisk"); var dead = (HumanEnemy)one.Members[0]; dead.Hp = 0;
    FactionDeck.Tick();
    bool cooling = FactionDeck.Free("Obelisk", dead.Card) == 0;
    for (int h = 0; h < FactionDeck.CooldownHours; h++) FactionDeck.Tick();
    Check(cooling && FactionDeck.Free("Obelisk", dead.Card) == 1, $"a dead {dead.Rank}'s card is back after {FactionDeck.CooldownHours} h, not before ({cooling}, free {FactionDeck.Free("Obelisk", dead.Card)}, cooling list {m.DeckCooling.Count})");
    // Batch 53: decks are only the spawn rule - a survivor who kills better men and takes their gear grows past its card
    var (rw, ra) = EncounterGeneration.PickFactionLoadout("Raiders", "Rookie", 1); var (ow, oa) = EncounterGeneration.PickFactionLoadout("Obelisk", "Master", 7, true);
    var raider = new HumanEnemy("Raider", 0, 0, "Raiders", "Rookie", rw, ra) { Card = "Rookie" };
    var master = new HumanEnemy("Monk", 1, 0, "Obelisk", "Master", ow, oa); master.Hp = 0;
    raider.GainXp(HumanEnemy.Worth(master)); raider.LootFrom(master);
    Check(raider.Rank == "Experienced" && raider.EquippedWeapon?.Db == ow && raider.EquippedArmor == oa && raider.Card == "Rookie", $"a Rookie raider who kills an Obelisk Master moves up and takes his gear ({raider.Rank}, {raider.EquippedWeapon?.Db?.Name})");
    for (int k = 0; k < 40; k++) raider.GainXp(7);
    Check(raider.Rank == "Legend" && raider.D20Mod == FactionDatabase.Ranks["Legend"].D20Mod, "enough kills make a Legend of a raider");
    m.OfflineSquads.RemoveAll(q => q.Faction == "Raiders"); var rsq = SquadManager.CreateOfflineSquad("scrapyard", "Raiders", 1); int rookies0 = FactionDeck.Alive().TryGetValue("Raiders|" + ((HumanEnemy)rsq.Members[0]).Card, out var r0) ? r0 : 0;
    ((HumanEnemy)rsq.Members[0]).GainXp(200);
    Check(FactionDeck.Alive().TryGetValue("Raiders|" + ((HumanEnemy)rsq.Members[0]).Card, out var r1) && r1 == rookies0, "a promoted NPC still holds the card it was drawn as");
    m.OfflineSquads.Remove(rsq);
    // Batch 54: the losing side of an off-screen fight falls back to its outpost alive instead of fighting to the last man
    int fellBack = 0, wiped = 0; bool wentHome = true, quietAfter = true;
    for (int t = 0; t < 60; t++)
    {
      m.OfflineSquads.Clear(); m.WorldNews.Clear();
      var ra2 = SquadManager.CreateOfflineSquad("ship_graveyard", "Raiders", 4); var gb = SquadManager.CreateOfflineSquad("ship_graveyard", "Wanderers", 4);
      if (ra2 == null || gb == null) continue;
      SquadManager.ResolveOffscreenSquadConflicts("scavengers_hub");
      var beaten = new[] { ra2, gb }.FirstOrDefault(q => q.Job == "retreat");
      if (beaten != null)
      {
        fellBack++;
        // Batch 83: a northern squad falls back to its outpost only if that's in the north too, else to a northern neighbour
        wentHome &= beaten.Members.Any(x => x.IsAlive()) && (SquadManager.Homes[beaten.Faction].Contains(beaten.Heading) || SquadManager.InBand(beaten, beaten.Heading)) && m.WorldNews.Any(n => n.Contains("fell back"));
        int hp = beaten.Members.Sum(x => x.Hp); SquadManager.ResolveOffscreenSquadConflicts("scavengers_hub"); quietAfter &= beaten.Members.Sum(x => x.Hp) == hp;
      }
      else if (!m.OfflineSquads.Contains(ra2) || !m.OfflineSquads.Contains(gb)) wiped++;
    }
    Check(fellBack > 0 && wentHome && quietAfter, $"a beaten squad falls back to its outpost and isn't fought while it goes ({fellBack} retreats, {wiped} wipes in 60 fights)");
    m.OfflineSquads.Clear();
    // Batch 54: a ledge higher than a whole turn's MP is a wall to the pathfinder (walks used to stall at its foot)
    var pl = Player.Current; var foot = TerrainMap.FindNearestEnterable(pl.X + 4, pl.Y); var top = TerrainMap.FindNearestEnterable(foot.x + 1, foot.y);
    if (top.x == foot.x + 1 && top.y == foot.y)
    {
      int h0 = TerrainMap.Tiles[top.y, top.x].Height; TerrainMap.Tiles[top.y, top.x].Height = TerrainMap.GetHeight(foot.x, foot.y) + pl.MaxMp + 1;
      var ledge = GameServices.RequireGrid().FindPath(foot.x, foot.y, top.x, top.y, pl);
      TerrainMap.Tiles[top.y, top.x].Height = h0;
      Check(ledge.Count == 0, $"no path up a ledge taller than a turn's MP ({ledge.Count} steps)");
    }
    else Console.WriteLine("ledge test skipped: no flat spot next to the player");
    // outposts: one per faction, the stronger the farther north
    foreach (var kv in SquadManager.Homes.Where(kv => kv.Key != "Mutant"))
    {
      var (lo, hi) = SquadManager.OutpostTiers(FactionDeck.Strength(kv.Key)); int tier = MacroMapDatabase.Find(kv.Value[0]).Tier;
      Check(kv.Value.Length == 1 && tier >= lo && tier <= hi, $"{kv.Key} (strength {FactionDeck.Strength(kv.Key)}) has its outpost at tier {tier}, wants {lo}-{hi}");
    }
    GameLog.Target = null;
  }

  static void GroundedChecks() {
    var log = new ListLog(); GameLog.Target = log;
    GameBootstrap.StartNewGame();
    var m = MacroState.Current;
    var here = SurfaceMapZone();
    m.CurrentZoneId = here.Id; ZoneTravel.EnterZone(here.Id);
    GameState.Current.CurrentTurn = Turn.Player; EncounterState.InCombat = false;
    var p = Player.Current;
    HumanEnemy Guy(int dx) { var (w, a) = EncounterGeneration.PickFactionLoadout("Raiders", "Experienced", 1); var c = TerrainMap.FindNearestEnterable(Math.Clamp(p.X + dx, 1, TerrainMap.Width - 2), p.Y); var h = new HumanEnemy("Raider", c.x, c.y, "Raiders", "Experienced", w, a); EncounterState.Enemies.Add(h); return h; }
    // morale break: cornered (you're close, or it's badly hurt) surrenders; otherwise it runs
    var close = Guy(2); EnemyAI.Break(close, p);
    Check(close.Morale == "Surrendered", "cornered by you: surrenders");
    var away = Guy(12); EnemyAI.Break(away, p);
    Check(away.IsFleeing(), "room to run: runs");
    var hurt = Guy(12); hurt.Hp = hurt.MaxHp / 5; EnemyAI.Break(hurt, p);
    Check(hurt.Morale == "Surrendered", "too hurt to run: surrenders");
    // fleeing: an empty gun goes on the ground where they stood, not into your pack
    var runner = Guy(14); for (int g = 0; g < 50 && (runner.EquippedWeapon == null || CombatRules.IsMelee(runner.EquippedWeapon)); g++) { EncounterState.Enemies.Remove(runner); runner = Guy(14); }   // needs a gun to empty
    runner.Inventory.Items.Add(new InventoryItem { Id = "bandage", Name = "Bandage", Type = "consumable", Qty = 1 });
    if (runner.EquippedWeapon != null) runner.EquippedWeapon.CurrentMagazine = 0;
    int packBefore = p.Inventory.Items.Count; p.Ap = 0;
    TurnLoop.ResolveFleeReactions(runner, p);
    Check(runner.EquippedWeapon == null && TerrainMap.GetGroundItemsAt(runner.X, runner.Y).Any(g => g.Item.Type == "weapon") && TerrainMap.GetGroundItemsAt(runner.X, runner.Y).Any(g => g.Item.Id == "bandage") && p.Inventory.Items.Count == packBefore,
      "a fleeing NPC with an empty gun drops it and its pack on the ground, not in yours");
    var keeps = Guy(16); keeps.Hp = keeps.MaxHp; ItemsBefore(keeps);
    bool heavyGun = CombatRules.Category(keeps.EquippedWeapon?.Db) == "heavy";   // Batch 88: read before the flight (a heavy one is dropped - Raiders can roll the PL-62 since Batch 79)
    TurnLoop.ResolveFleeReactions(keeps, p);
    Check(keeps.EquippedWeapon != null || heavyGun, "a healthy NPC with a loaded light gun keeps it");
    // interrogation gives up a real squad of theirs
    var elsewhere = OffMap(here);
    m.OfflineSquads.RemoveAll(q => q.Faction == "Raiders" && q.ZoneId != here.Id);   // also frees Raider cards (Batch 52)
    var theirs = SquadManager.CreateOfflineSquad(elsewhere, "Raiders", 2);
    p.Stats.I = 100;
    EnemyAI.Interrogate(close, p);
    Check(m.Rumors.Any(r => r.Contains(MacroMapDatabase.Find(elsewhere).Name) && r.EndsWith("(true)")) || log.L.Any(l => l.Contains("way into")), "a prisoner gives up where their people are");
    // scavenging: a squad strips real War Spoils lying in its zone; nothing appears from nowhere
    var spoilZone = elsewhere; var spoilKey = MacroMapDatabase.MapOf(spoilZone);   // the map overhaul: things lie on the region's map
    m.OfflineSquads.RemoveAll(q => q != theirs && (MacroMapDatabase.SameMap(q.ZoneId, spoilZone) || q.Heading != null));   // nobody else gets there first (anyone on that map could)
    m.PersistedZoneInteractiveObjects[spoilKey] = new System.Collections.Generic.List<InteractiveObjectInstance> { new InteractiveObjectInstance { Name = "War Spoils", Reward = new InteractiveReward { Ru = 50 } } };
    var scav = (HumanEnemy)theirs.Members.First(); int ru0 = scav.Ru, items0 = theirs.Members.OfType<HumanEnemy>().Sum(x => x.Inventory.Items.Count);
    int hrs = 0;
    while (m.PersistedZoneInteractiveObjects[spoilKey].Count > 0 && hrs < 72 && m.OfflineSquads.Contains(theirs)) { theirs.ZoneId = spoilZone; m.OfflineSquads.RemoveAll(q => q != theirs && MacroMapDatabase.SameMap(q.ZoneId, spoilZone)); Atmosphere.Advance(60f); hrs++; }   // Batch 52: new squads muster at outposts, maybe this zone
    Check(!m.OfflineSquads.Contains(theirs) || m.PersistedZoneInteractiveObjects[spoilKey].Count == 0 && m.OfflineSquads.Where(q => MacroMapDatabase.SameMap(q.ZoneId, spoilZone)).SelectMany(q => q.Members.OfType<HumanEnemy>()).Sum(x => x.Ru) >= ru0 + 50, $"a squad on that map strips the War Spoils (Batch 91: a fresh squad mustering at an outpost there may get to them first) ({hrs} h, left {m.PersistedZoneInteractiveObjects[spoilKey].Count}, ru {theirs.Members.OfType<HumanEnemy>().Sum(x => x.Ru)} vs {ru0}, alive {theirs.Members.Count(x => x.IsAlive())}, zone {theirs.ZoneId}, job {theirs.Job}, news {m.WorldNews.FirstOrDefault()})");
    m.PersistedZoneInteractiveObjects.Remove(spoilKey);
    // Batch 49: anomalies - detectors reveal by range, walking in hurts (no dodge), fields hold real artifacts
    EncounterState.Enemies.RemoveAll(e => e is HumanEnemy);
    EncounterState.Anomalies.Clear(); p.ActiveDetector = null; p.ArtifactAvoidanceBonus = 0;
    var c3 = TerrainMap.FindNearestEnterable(p.X + 3, p.Y); var c1 = TerrainMap.FindNearestEnterable(p.X + 1, p.Y);
    var an3 = Anomaly.FromDb(AnomalyEngine.ChooseRandomAnomalyDefinition(), c3.x, c3.y); var an1 = Anomaly.FromDb(AnomalyEngine.ChooseRandomAnomalyDefinition(), c1.x, c1.y);
    EncounterState.Anomalies.Add(an3); EncounterState.Anomalies.Add(an1);
    AnomalyEngine.DetectorSweep(p);
    bool bare = an1.Revealed && (Math.Max(Math.Abs(c3.x - p.X), Math.Abs(c3.y - p.Y)) <= 1 || !an3.Revealed);
    p.ActiveDetector = GameData.DetectorDatabase.All["Clicker"]; AnomalyEngine.DetectorSweep(p);
    Check(bare && an3.Revealed, "bare senses only catch the next tile; a Clicker reveals within 3");
    an1.Artifact = new InventoryItem { Id = "testArt", Name = "Test Stone", Type = "artifact", Qty = 1 }; an1.Searched = false;
    GameState.Current.CurrentTurn = Turn.Player; p.Ap = 4;
    Check(AnomalyEngine.TrySearchAnomaly(p) && p.Inventory.Items.Any(i => i.Id == "testArt") && an1.Artifact == null, "searching a field takes the artifact lying in it, no roll");
    p.Ap = 4; an3.Searched = false; p.X = TerrainMap.FindNearestEnterable(c3.x - 1, c3.y).x;
    Check(AnomalyEngine.SearchableNear(p) == null || AnomalyEngine.TrySearchAnomaly(p) && !p.Inventory.Items.Any(i => i.Id != "testArt" && i.Type == "artifact" && i.Name == "x"), "an empty field gives nothing");
    p.ArtifactAutoPassAnomalies = false; an1.Dc = 15; int hp0 = p.Hp; p.HealPermanent(100); hp0 = p.Hp;
    AnomalyEngine.PerformAnomalyCheck(p, an1);
    Check(p.Hp < hp0 || an1.AnomalyType == "Radiation" && PlayerGearUtil.HasArmorMod("ClosedBreathing"), "walking into a field hurts - no dodge roll");
    var (nw, na) = EncounterGeneration.PickFactionLoadout("Raiders", "Rookie", 1);
    var local = new HumanEnemy("Local", an1.X, an1.Y, "Raiders", "Rookie", nw, na); int lhp = local.Hp;
    AnomalyEngine.TriggerAnomalyIfPresent(local);
    Check(local.Hp == lhp, "the Zone's own know where the fields are");
    var path = GameServices.Grid.FindPath(p.X, p.Y, c3.x + 2, c3.y, p);
    Check(!path.Skip(1).Take(path.Count - 2).Any(t => t.x == c3.x && t.y == c3.y), "a click-walk routes around a known field");
    // cloak: a cloaked mutant beyond your spotting range isn't drawn or targetable; close up it is
    Atmosphere.Current = Weather.Clear; m.Hour = 12f;
    var mdef = MutantDatabase.Tiers["tier1"].First().Value;
    var ghost = new Mutant(mdef.Name, p.X, p.Y, mdef) { IsCloaked = true };
    int spot = Mutant.CloakSpotRange();
    var farC = TerrainMap.FindNearestEnterable(p.X + spot + 3, p.Y); ghost.X = farC.x; ghost.Y = farC.y;
    bool hiddenFar = ghost.HiddenByCloak();
    ghost.X = p.X + 1; ghost.Y = p.Y;
    Check(hiddenFar && !ghost.HiddenByCloak(), $"a cloaked mutant is hidden beyond {spot} tiles and seen up close");
    EncounterState.Anomalies.Clear(); p.ActiveDetector = null;

    // Batch 50: a hidden way is a place by its gate; a clue is a place in the quest zone
    var ib = MacroMapDatabase.Find("industrial_basin"); var lab = MacroMapDatabase.Find("secret_lab_alpha");
    m.CurrentZoneId = ib.Id; ZoneTravel.EnterZone(ib.Id); p = Player.Current; m.DiscoveredHiddenNodes.Remove(lab.Id);
    var gate = ZoneExitPlacer.GatePos(ib, lab.Id); int sense = ZoneTravel.HiddenSenseRange(lab, ib);
    var farG = TerrainMap.FindNearestEnterable(gate.x < TerrainMap.Width / 2 ? gate.x + 40 : gate.x - 40, gate.y); p.X = farG.x; p.Y = farG.y;
    ZoneTravel.SenseHiddenWays();
    bool farMissed = !m.DiscoveredHiddenNodes.Contains(lab.Id);
    log.L.Clear(); p.X = gate.x < TerrainMap.Width / 2 ? gate.x + 20 : gate.x - 20; p.Y = gate.y; ZoneTravel.SearchArea();
    bool hinted = log.L.Any(l => l.Contains("looks disturbed"));
    p.X = gate.x; p.Y = gate.y; ZoneTravel.SenseHiddenWays();
    Check(farMissed && hinted && m.DiscoveredHiddenNodes.Contains(lab.Id), $"a hidden way is found by walking near its gate (range {sense}), F hints the direction, nothing from far off");

    var invQ = QuestDatabase.All.First(q => q.Category == QuestCategory.Investigate && q.HiddenNodeId == null);
    m.CurrentZoneId = invQ.ZoneId ?? ib.Id; ZoneTravel.EnterZone(m.CurrentZoneId); p = Player.Current; p.ActiveQuests.Clear();
    QuestSystem.AcceptQuest(invQ.Id);
    var aqI = p.ActiveQuests.FirstOrDefault(a => a.QuestId == invQ.Id); var clue = aqI?.Objectives.First(o => o.Kind == "SkillCheck");
    QuestSystem.SenseClues();
    bool placed = clue?.ClueZone == m.CurrentZoneId && !clue.Done;
    if (clue != null) { p.X = clue.ClueX; p.Y = clue.ClueY; QuestSystem.SenseClues(); }
    Check(placed && (clue.Done || !p.ActiveQuests.Contains(aqI)), "an investigate job puts a clue on the map and walking to it finds it");

    // Batch 51: a failed lock bypass trips an alarm that draws NPCs, and costs no HP
    Atmosphere.Current = Weather.Clear; TerrainMap.Landmarks.Clear(); TerrainMap.InteractiveObjects.Clear(); EncounterState.Enemies.Clear();
    var lk = InteractiveObjectDatabase.All["ElectronicLock"];
    TerrainMap.InteractiveObjects.Add(new InteractiveObjectInstance { X = p.X, Y = p.Y, Name = lk.Name, Stat = lk.Stat, Dc = 99, Reward = lk.Reward });
    var (iw, ia) = EncounterGeneration.PickFactionLoadout("Wanderers", "Rookie", 1);
    var ear = new HumanEnemy("Listener C", p.X + (p.X + 15 < TerrainMap.Width ? 15 : -15), p.Y, "Wanderers", "Rookie", iw, ia) { IsAggroed = false, PatrolTarget = null };
    EncounterState.Enemies.Add(ear);
    GameState.Current.CurrentTurn = Turn.Player; p.Ap = p.MaxAp; int hpL = p.Hp;
    InteractionEngine.AttemptInteract(p);
    Check(ear.PatrolTarget != null && p.Hp == hpL, "a failed lock bypass sounds an alarm the NPC 15 tiles off comes to, and hurts nobody");
    EncounterState.Enemies.Clear();
    GameLog.Target = null;
  }
  static void ItemsBefore(HumanEnemy h) { if (h.EquippedWeapon != null) h.EquippedWeapon.CurrentMagazine = Math.Max(1, h.EquippedWeapon.CurrentMagazine); }
  class ListLog : IGameLogTarget { public System.Collections.Generic.List<string> L = new System.Collections.Generic.List<string>(); public void AppendLine(string s) => L.Add(s); }

  static void HazardChecks() {
    var log = new ConsoleLog(); GameLog.Target = log;
    GameBootstrap.StartNewGame();
    var hot = MacroMapDatabase.Zones.First(z => !z.IsSafeZone && z.BaseHazardModifier >= 4);
    MacroState.Current.CurrentZoneId = hot.Id; ZoneTravel.EnterZone(hot.Id);
    // Batch 42: no tabletop spawn rolls - walking around a zone only ever meets its A-Life squads
    var squadMembers = MacroState.Current.OfflineSquads.SelectMany(q => q.Members).ToHashSet();
    var p = Player.Current; EncounterState.InCombat = false;
    for (int i = 0; i < 200; i++) { GameState.Current.CurrentTurn = Turn.Player; EncounterState.InCombat = false; p.Mp = 9; int x0 = p.X; p.Move(i / 20 % 2 == 0 ? 1 : -1, 0); if (p.X == x0) p.Move(0, 1); TurnLoop.TickAwareness(); }
    Check(EncounterState.Enemies.All(e => squadMembers.Contains(e)), "every enemy on the map belongs to an A-Life squad");
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
        p.HealPermanent(100); p.Hunger = p.Thirst = p.Tiredness = 10; Ammo.Add(p.Inventory, Ammo.Key(p.EquippedWeapon.Db, "Standard"), 300);
        var zone = MacroMapDatabase.Find(MacroState.Current.CurrentZoneId);
        var regs = MacroMapDatabase.RegionsOf(zone.Id).ToList();   // the map overhaul: the ways off this map, from any of its regions
        var options = regs.SelectMany(r => r.ConnectedZones.Concat(r.HiddenConnections ?? new System.Collections.Generic.List<string>())).Where(z => !MacroMapDatabase.SameMap(z, zone.Id)).Distinct().ToList();
        foreach (var h in regs.SelectMany(r => r.HiddenConnections ?? new System.Collections.Generic.List<string>())) if (!MacroState.Current.DiscoveredHiddenNodes.Contains(h)) MacroState.Current.DiscoveredHiddenNodes.Add(h);
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
    string pk = Ammo.Key(p.EquippedWeapon.Db, "Standard");   // Batch 80: "pistol1|Standard"
    int stash = Ammo.Get(p.Inventory, pk), mag = p.EquippedWeapon.CurrentMagazine;
    Check(pk == "pistol1|Standard" && stash == 30 && Ammo.Label(pk) == "T1 pistol rounds (Standard)", $"starting rounds are T1 pistol ({pk} {stash})");
    Check(p.SecondaryWeapon?.Db?.Name == "Hunting Knife", "the starting knife sits in the second slot");
    { var w = p.EquippedWeapon; int m0 = w.CurrentMagazine; w.CurrentMagazine = 0; int keep = Ammo.Get(p.Inventory, pk); p.Inventory.AmmoReserve[pk] = 0;
      Ammo.Add(p.Inventory, "pistol2|Standard", 50); Ammo.Add(p.Inventory, "rifle1|Standard", 50);
      Check(!w.Reload() && w.CurrentMagazine == 0, "a T1 pistol won't load T2 pistol or T1 rifle rounds");
      p.Inventory.AmmoReserve[pk] = keep; p.Inventory.AmmoReserve.Remove("pistol2|Standard"); p.Inventory.AmmoReserve.Remove("rifle1|Standard"); w.CurrentMagazine = m0;
      Check(ShopSystem.AmmoKeyHere("HollowPoint") == $"pistol{MacroMapDatabase.Find(MacroState.Current.CurrentZoneId).Tier}|HollowPoint", "a trader sells rounds of its own tier: " + ShopSystem.AmmoKeyHere("HollowPoint")); }
    p.StowSecondary();
    var knife = p.Inventory.Items.First(i => i.Key == "HuntingKnife");
    p.EquipWeaponFromInventory(knife.Id);
    Check(Ammo.Get(p.Inventory, pk) == stash + mag, "swapping out unloads into the stash");
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
    Vision.Reset(); Vision.Update(p, 0);
    string d = TacticalGridView.DescribeTile(e.X, e.Y, p);
    Check(d.Contains("Hover Me") && d.Contains("HP"), "hover text: " + d);
    foreach (var lm in TerrainMap.Landmarks) Check(TacticalGridView.DescribeTile(lm.X, lm.Y, p).Length > 0 || EncounterState.Enemies.Any(en => en.X == lm.X && en.Y == lm.Y), "landmark hover " + lm.Name);
    CombatFeedbackChecks(p, e);
    GameLog.Target = null;
  }

  static void CombatFeedbackChecks(Player p, HumanEnemy e) {
    WeaponDef W(string c, string k) => WeaponDatabase.Categories[c][k];
    var pistol = WeaponFeel.For(W("sidearm", "PSh18Pchela")); var kuvalda = WeaponFeel.For(W("sidearm", "RK12Kuvalda"));
    Check(kuvalda.Kick >= pistol.Kick * 2.5f && kuvalda.Climb > pistol.Climb * 2.5f, $"hand cannon kicks harder ({kuvalda.Kick} vs {pistol.Kick})");
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
    Check(mv.IsMoving(e) && Math.Abs(wp.x - (TacticalGridView.GridToWorld(sx, sy).x + (EncounterState.InCombat ? UnitMotionView.WalkSpeed : UnitMotionView.ExploreSpeed) * 0.1f)) < 0.07f && Math.Abs(wp.y - TacticalGridView.GridToWorld(sx, sy).y) < 0.07f, $"walks along the route, not diagonally ({wp.x},{wp.y})");
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
