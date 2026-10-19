// Batch 125 (Yan: "simulate a new character, starting in the world, walking around, getting into fights, going about quests until
// he reaches the trading hub"): a bot plays a fresh character on the real maps and code - New Game at Bonfire Village, takes the
// jobs on offer, walks (one tile = one game minute, TurnLoop.TickAwareness, as exploring does), opens stashes it passes, hunts its
// quest targets on the map it's on, fights whatever spots it (the Raid bot from BuildsProbe: cover, medkits, reload, hunker),
// strips bodies, sleeps by a campfire when hurt, and crosses map to map (ZoneTravel, 8 h a crossing) to Scavenger's Hub.
//   build like BuildsProbe (swap this file in for test/Program.cs); args: [characters=3] [loadout...]; JOURNEY_LOG=1 prints every log line
using System; using System.Linq; using System.Collections.Generic; using GameCore; using GameData;
static class ProbeMain {
  static readonly List<string> Log = new List<string>();
  class Cap : IGameLogTarget { public void AppendLine(string s) { var m = MacroState.Current; Log.Add($"D{m.Day} {Atmosphere.Clock()}  {s}"); if (Environment.GetEnvironmentVariable("JOURNEY_LOG") == "1") Console.WriteLine("  | " + Log[^1]); } }
  static void Say(string s) => GameLog.Write("[BOT] " + s);
  static readonly HashSet<string> Said = new HashSet<string>();
  static void Once(string key, string s) { if (Said.Add(key)) Say(s); }
  static bool Out(Mutant e) => !e.IsAlive() || (e is HumanEnemy hh && EnemyAI.IsOut(hh));
  static bool Foe(Mutant e) => !Out(e) && !Companion.Is(e) && e.IsAggroed && !EnemyAI.FriendlyToPlayer(e);
  const string Hub = "scavengers_hub";

  // zones from here to the hub (breadth first over the zone graph)
  static List<string> Route(string from) {
    var prev = new Dictionary<string, string> { [from] = null }; var q = new Queue<string>(); q.Enqueue(from);
    while (q.Count > 0) { var z = q.Dequeue(); if (z == Hub) break;
      foreach (var n in MacroMapDatabase.Find(z)?.ConnectedZones ?? new List<string>()) if (!prev.ContainsKey(n) && MacroMapDatabase.Find(n) != null && !MacroMapDatabase.Find(n).IsDungeon) { prev[n] = z; q.Enqueue(n); } }
    var path = new List<string>(); for (var z = prev.ContainsKey(Hub) ? Hub : null; z != null; z = prev[z]) path.Insert(0, z);
    return path;
  }

  // Batch 125b (Yan: "What if we simulated the bots journey as if it was an a-life entity like the npc's?"): "alife" as the first
  // argument makes the bot live like a Wanderers squad leader - Posts.NextStop gives it a home post and a task for 3-6 hours (hold,
  // patrol, scavenge), it rests 20-180 min at each stop, looks over stashes close by, and leaves for the next tier with the chance an
  // hour squads migrate with (SquadManager.MigrateChancePerHour). Quest targets and clues still pull it as before.
  static bool ALife;
  static readonly OfflineSquad Me = new OfflineSquad { Faction = "Wanderers" };
  class Stats { public int Rested, Steps, Fights, Rounds, Kills, Stashes, Bodies, Sleeps, Medkits, Quests, Crossings, Tolls; public string End = "?"; public int StartRu; }

  static void Main(string[] args) {
    ALife = args.Length > 0 && args[0] == "alife"; if (ALife) args = args.Skip(1).ToArray();
    int n = args.Length > 0 && int.TryParse(args[0], out var k) ? k : 3;
    var loadouts = args.Skip(1).ToArray(); if (loadouts.Length == 0) loadouts = new[] { "Scavenger", "Ex-Military", "Freelance Trader" };
    GameServices.Grid = new TacticalGridSystem(); GameServices.Combat = new CombatResolver(); GameServices.AnomalyTrigger = new AnomalyTriggerService();
    GameLog.Target = new Cap();
    for (int c = 0; c < n; c++) {
      Log.Clear(); Said.Clear(); var st = new Stats();
      var spec = new CharacterSpec { Loadout = loadouts[c % loadouts.Length] };
      GameBootstrap.StartNewGame(spec);
      GameState.Current.Mode = GameMode.Tactical; GameState.Current.CurrentTurn = Turn.Player;
      Shakedown.TestPick = Shakedown.Pay;   // the toll dialog: pay if it can (Resolve falls back to refusing)
      var p = Player.Current; st.StartRu = p.Ru;
      Say($"A new {spec.Loadout} wakes up at {MacroMapDatabase.Find(MacroState.Current.CurrentZoneId)?.Name}: {p.Hp} HP, {p.Ru} Ru, {p.EquippedWeapon?.Name}, {p.EquippedArmor?.Name}.");
      foreach (var q in QuestSystem.AvailableInZone(MacroState.Current.CurrentZoneId).Take(2)) QuestSystem.AcceptQuest(q.Id);
      Play(p, st);
      st.Tolls = Log.Count(l => l.Contains("A shakedown"));
      Console.WriteLine($"=== {c + 1}. {spec.Loadout}: {st.End}");
      var m = MacroState.Current;
      Console.WriteLine($"    {m.Day - 1} days {Atmosphere.Clock()} of game time, ~{((st.Steps + st.Rested) / UnitMotionView.ExploreSpeed + st.Rounds * 20) / 60:F0} real minutes (the exploring clock: a game minute every 0.24 s, walking or standing; + ~20 s a combat round)");
      Console.WriteLine($"    walked {st.Steps} tiles, crossed {st.Crossings} maps; fights {st.Fights} ({st.Rounds} rounds), kills {st.Kills}, tolls {st.Tolls}; stashes {st.Stashes}, bodies {st.Bodies}; medkits {st.Medkits}, nights {st.Sleeps}");
      Console.WriteLine($"    quests done {p.CompletedQuestIds.Count}, active {string.Join(" / ", p.ActiveQuests.Select(a => QuestDatabase.Find(a.QuestId)?.Hook))}");
      Console.WriteLine($"    HP {p.Hp}/{p.MaxHp}, Ru {st.StartRu} -> {p.Ru}, weapon {p.EquippedWeapon?.Name}, armor {p.EquippedArmor?.Name}, carrying {p.Inventory.Items.Sum(i => i.Qty)} items");
      var file = $"journey_{c + 1}_{spec.Loadout.ToLowerInvariant().Replace(' ', '_').Replace('-', '_')}.txt";
      System.IO.File.WriteAllLines(file, Log); Console.WriteLine($"    full log: {file} ({Log.Count} lines)");
    }
  }

  static void Play(Player p, Stats st) {
    var opened = new HashSet<(int, int)>(); var tried = new HashSet<(int, int)>();
    int stuck = 0; string lastMap = null; int mapSteps = 0;
    (int x, int y)? stop = null; int restUntil = 0, lastHour = -1; bool leaving = !ALife; Mutant lead = null;
    int Minute() => (int)((MacroState.Current.Day * 24f + MacroState.Current.Hour) * 60f);
    while (p.IsAlive() && MacroState.Current.Day < (ALife ? 60 : 15)) {
      string region = MacroState.Current.CurrentZoneId;
      if (region == Hub) { st.End = $"reached {MacroMapDatabase.Find(Hub).Name}"; Say("Reaches the Hub's gate."); return; }
      if (TerrainMap.ZoneId != lastMap) { lastMap = TerrainMap.ZoneId; mapSteps = 0; opened.Clear(); tried.Clear(); stop = null; restUntil = 0; leaving = !ALife; lead = null; }
      if (ALife && !leaving && Minute() / 60 != lastHour) {
        lastHour = Minute() / 60;
        if (UnityEngine.Random.value < SquadManager.MigrateChancePerHour) { leaving = true; Say($"Has seen enough of {MacroMapDatabase.Find(region)?.Name} - heads on toward the Hub."); }
      }
      if (!PlayerController.Exploring()) { Fight(p, st); continue; }

      // hurt and nothing around: patch up, or sleep by a fire if there's one close
      if (p.Hp < p.MaxHp * 0.45f) {
        var kit = p.Inventory.Items.FirstOrDefault(i => i.Type == "consumable" && i.Id.ToLowerInvariant().Contains("medkit") && i.Qty > 0);
        if (kit != null) { p.UseConsumable(kit.Id); st.Medkits++; Say($"Patches up with a {kit.Name} ({p.Hp} HP)."); continue; }
        var fire = Nearest(p, 60, (x, y) => TerrainMap.GetTile(x, y).Look == "campfire");
        if (fire != null) {
          if (Math.Max(Math.Abs(fire.Value.x - p.X), Math.Abs(fire.Value.y - p.Y)) <= 1 || TerrainMap.GetTile(p.X, p.Y).Look == "campfire") { WorldDays.Rest(); st.Sleeps++; continue; }
          if (WalkTo(p, fire.Value.x, fire.Value.y, st)) continue;
        }
      }
      // what to walk to: a stash close by, a quest target on this map, else the exit toward the hub
      (int x, int y)? goal = null; string why = null;
      int look = ALife ? 6 : 14;   // A-Life: only what's round the post it's at
      var box = TerrainMap.InteractiveObjects.Where(o => !tried.Contains((o.X, o.Y)) && Math.Max(Math.Abs(o.X - p.X), Math.Abs(o.Y - p.Y)) <= look)
        .OrderBy(o => Math.Max(Math.Abs(o.X - p.X), Math.Abs(o.Y - p.Y))).FirstOrDefault();
      if (box != null) {
        if (Math.Max(Math.Abs(box.X - p.X), Math.Abs(box.Y - p.Y)) <= 1) {
          tried.Add((box.X, box.Y)); p.Ap = p.MaxAp; int ru = p.Ru, items = p.Inventory.Items.Sum(i => i.Qty);
          InteractionEngine.AttemptInteract(p);
          if (!TerrainMap.InteractiveObjects.Contains(box)) { st.Stashes++; Say($"Opens the {box.Name}: +{p.Ru - ru} Ru, +{p.Inventory.Items.Sum(i => i.Qty) - items} items."); }
          else Say($"Can't get the {box.Name} open.");
          continue;
        }
        goal = (box.X, box.Y); why = "stash";
      }
      var targets = MacroState.Current.OfflineSquads.Where(s => s.QuestId != null && p.ActiveQuests.Any(a => a.QuestId == s.QuestId) && MacroMapDatabase.SameMap(s.ZoneId, TerrainMap.ZoneId))
        .SelectMany(s => s.Members).Where(m => m.IsAlive() && EncounterState.Enemies.Contains(m)).ToList();
      if (goal == null && targets.Count > 0) {
        var t = targets.OrderBy(m => CombatantUtil.Distance(p, m)).First(); goal = (t.X, t.Y); why = "quest"; Once("hunt " + t.Name, $"Goes after its quest target, {t.Name}.");
        var w = p.EquippedWeapon;   // close enough and in sight: open fire (they haven't noticed: an ambush)
        if (w != null && CombatantUtil.Distance(p, t) <= Math.Min(w.MaxReach, 8) && GameServices.RequireGrid().CheckLineOfSight(p.X, p.Y, t.X, t.Y)) {
          Say($"Opens fire on {t.Name}."); p.Ap = p.MaxAp; t.IsAggroed = true; p.Attack(t, null); EncounterState.InCombat = true; continue;
        }
      }
      var clue = p.ActiveQuests.Where(a => MacroMapDatabase.SameMap(a.ZoneId, TerrainMap.ZoneId)).SelectMany(a => a.Objectives)
        .FirstOrDefault(o => !o.Done && o.Kind == "SkillCheck" && o.HiddenNodeId == null && o.ClueZone != null);   // F points the way; the bot just walks there
      if (goal == null && clue != null) { goal = (clue.ClueX, clue.ClueY); why = "clue"; Once("clue " + clue.ClueX, "Follows the signs for its quest."); }
      if (goal == null && !leaving) {   // A-Life: the squads' own task plan (Posts), resting at each stop
        if (Minute() < restUntil) { TurnLoop.TickAwareness(); st.Rested++; continue; }
        if (stop != null && Math.Max(Math.Abs(stop.Value.x - p.X), Math.Abs(stop.Value.y - p.Y)) <= 1) stop = null;
        if (stop == null) {
          if (lead == null) lead = new Mutant("you", p.X, p.Y);
          lead.X = p.X; lead.Y = p.Y;
          stop = Posts.NextStop(Me, lead, Minute(), out int rest);
          if (mapSteps > 0) restUntil = Minute() + rest;
          if (stop == null) { leaving = true; continue; }
          Once($"task {Posts.TaskOf(Me)} {Posts.HomeOf(Me)} {MacroState.Current.Day}", $"Takes up a task: {Posts.TaskOf(Me)} round {Posts.HomeOf(Me)}.");
          continue;
        }
        goal = stop; why = "post";
      }
      if (goal == null) {
        var route = Route(region);
        var next = route.Skip(1).FirstOrDefault(z => !MacroMapDatabase.SameMap(z, TerrainMap.ZoneId));
        var exit = next == null ? null : TerrainMap.Exits.FirstOrDefault(e => e.TargetZoneId == next) ?? TerrainMap.Exits.FirstOrDefault(e => route.Contains(e.TargetZoneId));
        if (exit == null) { st.End = $"stuck: no way on from {region} (route {string.Join(">", route)})"; return; }
        goal = (exit.X, exit.Y); why = "exit to " + exit.TargetZoneId;
      }
      if (!WalkTo(p, goal.Value.x, goal.Value.y, st)) {
        if (why == "stash") { tried.Add(goal.Value); continue; }
        if (why == "post") { stop = null; continue; }
        if (++stuck > 40) {
          var g2 = GameServices.RequireGrid();
          st.End = $"stuck walking to {why} at {goal} from {p.X},{p.Y} on {TerrainMap.ZoneId} (h{TerrainMap.GetHeight(p.X, p.Y)} {TerrainMap.GetTile(p.X, p.Y).Type}; free neighbours "
            + string.Join(" ", TerrainMap.GetAdjacentCoords(p.X, p.Y).Where(c => g2.CanEnterTile(c.x, c.y)).Select(c => $"{c.x},{c.y}h{TerrainMap.GetHeight(c.x, c.y)}{(TerrainMap.UnitAt(c.x, c.y, p) != null ? "U" : "")}")) + ")";
          return;
        }
      } else stuck = 0;
      if (TerrainMap.ZoneId != lastMap) st.Crossings++;
      if (++mapSteps > (ALife ? 60000 : 6000)) { st.End = $"wandered {mapSteps} steps on {TerrainMap.ZoneId} without getting out"; return; }
    }
    st.End = p.IsAlive() ? "ran out of days" : $"died in {MacroMapDatabase.Find(MacroState.Current.CurrentZoneId)?.Name}, day {MacroState.Current.Day}";
  }

  static (int x, int y)? Nearest(Player p, int r, Func<int, int, bool> ok) {
    (int, int)? best = null; int bd = int.MaxValue;
    for (int y = Math.Max(0, p.Y - r); y < Math.Min(TerrainMap.Height, p.Y + r); y++)
      for (int x = Math.Max(0, p.X - r); x < Math.Min(TerrainMap.Width, p.X + r); x++)
        if (ok(x, y)) { int d = Math.Max(Math.Abs(x - p.X), Math.Abs(y - p.Y)); if (d < bd) { bd = d; best = (x, y); } }
    return best;
  }

  /// <summary>One exploring step toward (x, y): as PlayerController.Step does - top up MP, move, a minute passes, cross an exit,
  /// update the region. False when there's no route or the step was refused.</summary>
  static bool WalkTo(Player p, int x, int y, Stats st) {
    var grid = GameServices.RequireGrid();
    var goal = grid.CanEnterTile(x, y) ? (x, y) : TerrainMap.GetAdjacentCoords(x, y).Where(c => grid.CanEnterTile(c.x, c.y)).OrderBy(c => Math.Max(Math.Abs(c.x - p.X), Math.Abs(c.y - p.Y))).Select(c => ((int, int)?)c).FirstOrDefault() ?? (x, y);
    var path = grid.FindPath(p.X, p.Y, goal.Item1, goal.Item2, p);
    if (path.Count < 2) return false;
    p.Mp = Math.Max(p.Mp, Math.Max(p.MaxMp, TurnLoop.WalkMp)); p.Ap = Math.Max(p.Ap, p.MaxAp);
    int bx = p.X, by = p.Y;
    p.Move(path[1].x - p.X, path[1].y - p.Y);
    if (p.X == bx && p.Y == by) {
      var u = TerrainMap.UnitAt(path[1].x, path[1].y, p);
      Once($"block {path[1].x},{path[1].y}", $"Can't step to {path[1].x},{path[1].y}: {(u != null ? u.Name + (u is HumanEnemy h ? $" ({h.FactionName}, {(h.IsAggroed ? "hostile" : "not fighting")})" : "") + " stands there" : $"tile {TerrainMap.GetTile(path[1].x, path[1].y).Type} h{TerrainMap.GetHeight(path[1].x, path[1].y)} from h{TerrainMap.GetHeight(p.X, p.Y)}, MP {p.Mp}")}.");
      return false;
    }
    st.Steps++;
    TurnLoop.TickAwareness();                       // the exploring clock: a minute a step
    var exit = TerrainMap.Exits.FirstOrDefault(e => e.X == p.X && e.Y == p.Y);
    if (exit != null && ZoneTravel.TravelToSector(exit.TargetZoneId)) {
      foreach (var q in QuestSystem.AvailableInZone(MacroState.Current.CurrentZoneId).Where(q => p.ActiveQuests.Count < 3).Take(1)) QuestSystem.AcceptQuest(q.Id);
      return true;
    }
    ZoneTravel.UpdateRegion();
    return true;
  }

  // the Raid bot from BuildsProbe, for whatever fight finds us
  static void Fight(Player p, Stats st) {
    st.Fights++; int kills0 = EncounterState.Enemies.Count(e => !e.IsAlive());
    var foes0 = EncounterState.Enemies.Where(Foe).ToList();
    Say($"Fight! {string.Join(", ", foes0.GroupBy(e => e is HumanEnemy h ? h.FactionName + " " + h.Rank : e.Name).Select(g => g.Count() + "x " + g.Key))} ({p.Hp} HP).");
    int r = 0;
    for (; r < 60 && p.IsAlive() && !PlayerController.Exploring(); r++) {
      bool ended = false;
      for (int k = 0; k < 12 && GameState.Current.CurrentTurn == Turn.Player && (p.Ap > 0 || p.Mp > 0); k++) {
        var w = p.EquippedWeapon; bool melee = w == null || CombatRules.IsMelee(w);
        var kit = p.Inventory.Items.FirstOrDefault(i => i.Type == "consumable" && i.Id.ToLowerInvariant().Contains("medkit") && i.Qty > 0);
        if (p.Hp < p.MaxHp * 0.4f && p.Ap >= 2 && kit != null) { p.UseConsumable(kit.Id); st.Medkits++; continue; }
        if (w != null && w.IsJammed) { if (p.Ap < 1) break; p.ClearWeaponJam(); continue; }
        if (!melee && !w.IsLoaded) { int ap0 = p.Ap; p.ReloadWeapon(); if (p.Ap == ap0) { if (p.SecondaryWeapon != null) p.SwapWeapons(); else break; } continue; }
        var foes = EncounterState.Enemies.Where(Foe).ToList();
        var near = foes.OrderBy(e => CombatantUtil.Distance(p, e)).FirstOrDefault();
        if (near == null) break;
        var t = foes.Where(e => CombatantUtil.Distance(p, e) <= (w?.MaxReach ?? 1) && GameServices.RequireGrid().CheckLineOfSight(p.X, p.Y, e.X, e.Y)).OrderBy(e => e.Hp).FirstOrDefault();
        if (t == null) {
          if (p.Mp <= 0) break;
          var grid = GameServices.RequireGrid();
          var goal = TerrainMap.GetAdjacentCoords(near.X, near.Y).Where(c => grid.CanEnterTile(c.x, c.y) && TerrainMap.UnitAt(c.x, c.y, p) == null)
            .OrderBy(c => Math.Max(Math.Abs(c.x - p.X), Math.Abs(c.y - p.Y))).Select(c => ((int x, int y)?)c).FirstOrDefault() ?? (near.X, near.Y);
          var path = grid.FindPath(p.X, p.Y, goal.x, goal.y, p);
          if (path.Count < 2 || !grid.AttemptMove(p, path[1].x, path[1].y)) break;
          continue;
        }
        if (!melee && foes.Count >= 2 && p.Hp < p.MaxHp / 2 && p.CanHunker(out _)) { p.HunkerDown(); ended = true; break; }
        if (p.Ap < 1) break;
        int ap1 = p.Ap; p.Attack(t, null); EncounterState.InCombat = true;
        if (p.Ap == ap1) break;
      }
      TurnLoop.CheckCombatEnd();
      if (!ended && GameState.Current.CurrentTurn == Turn.Player && !PlayerController.Exploring()) TurnLoop.PlayerEndTurn();
    }
    st.Rounds += r;
    int kills = EncounterState.Enemies.Count(e => !e.IsAlive()) - kills0; st.Kills += Math.Max(0, kills);
    if (!p.IsAlive()) { Say($"Dies after {r} rounds."); return; }
    if (r >= 60) { Say("The fight drags on for an hour; the bot breaks it off."); foreach (var e in EncounterState.Enemies) e.IsAggroed = false; EncounterState.InCombat = false; }
    Say($"Fight over after {r} rounds: {kills} down, {p.Hp}/{p.MaxHp} HP left.");
    // strip the bodies close by: ammo, meds, food, cash (the bot leaves the heavy guns and armor)
    int tier = MacroMapDatabase.Find(MacroState.Current.CurrentZoneId)?.Tier ?? 1;
    foreach (var body in EncounterState.Enemies.OfType<HumanEnemy>().Where(h => !h.IsAlive() && CombatantUtil.Distance(p, h) <= 12 && !h.CorpseLootFinalized).ToList()) {
      HumanLootSystem.GrantDrop(body, p, tier);
      int took = 0;
      foreach (var it in body.Inventory.Items.Where(i => i.WeightKg <= 1.5f && i.Type != "weapon" && i.Type != "armor").ToList()) {
        p.CalculateCarryWeight(); if (p.CurrentCarryWeight + it.WeightKg * it.Qty > p.Inventory.CarryCapacityBase) continue;   // a player wouldn't pack past the limit
        p.AddItem(it); body.Inventory.Items.Remove(it); took += it.Qty; }
      p.ApplyCarryEffects();
      st.Bodies++; if (took > 0) Say($"Strips {body.Name}: {took} things.");
    }
  }
}
