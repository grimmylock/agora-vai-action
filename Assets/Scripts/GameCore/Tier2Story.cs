// Tier 2, beat 1: "Taken" (Yan, 2026-10-28, STORY.md; built 2026-11-16). A Garrison squad grabs the player(s) soon after they first reach Tier 2, takes their guns, marches them down the Loading Dock's hatch
// into a big maze and tells them to find the hidden lab. Too slow and they are shot (-> the auto-save). At the lab door the passcard fails, an alarm goes, a mutant horde pours down the corridor, the
// soldiers turn on it and the players run back to the hatch. At the top the guns are in the box again.
//
// How it runs (all host-side, every frame from Rt.WorldTick -> Tick; the state is Story steps, saved with the world, plus a few transient fields):
//   Story.Step("taken"): 0 not yet, 1 the grab (flash, beating, black: transient - a load in the middle starts over), 2 at the Garrison's base (the interrogation), 3 the descent (the maze; the
//   auto-save is made as it starts, so "shot" = reload it), 4 the alarm and the run, done (Story.Done) = back up with the guns.
// - The grab happens once, near the Gatehouse (the Cutting road) or the Causeway's end, or after four minutes on the map (the grab is never skipped).
// - Co-op: everyone is taken; each player's guns go in the box under their own index ("box:<i>:..." world flags, so the auto-save carries them).
// - The strip-search leaves melee weapons, armour, ammo and the pack (Yan: "leaves the knife and armour, maybe a medkit and a bandage").
// - The soldiers are real Garrison units held non-aggressive by this class (it clears their IsAggroed every frame) and walked behind the players; the maze is `taken_tunnels` (TierDungeons.TakenPlan,
//   ArtSource/plans/taken_tunnels.py): 72x52, the way from the ladder to the corridor about 310 tiles with dead ends all round, the corridor along the north with the lab door in its middle, the nest at its east end.
// - ponytail: the soldiers dying to the horde is scripted (shots, screams, then they drop) rather than a real fight; the horde proper (Swarm rats, Pallid, Blind Maw) is released in two waves.
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class Tier2Story
    {
        public const string Mission = "taken", Maze = "taken_tunnels", Dock = "Loading Dock", Ladder = "Hatch Ladder", Door = "Lab Door";
        public const float ShotAfter = 420f;            // seconds in the maze before they lose patience (the way takes 3-4 minutes of wandering)

        static float _mapSince = -1f, _stageAt = -1f, _descentAt = -1f, _hordeAt = -1f, _gameOverAt = -1f;
        static int _stage;                              // the grab's stage while step 1; the horde's soldiers lost so far while step 4
        static bool _warned1, _warned2, _released1, _released2;
        static readonly List<HumanEnemy> Escorts = new List<HumanEnemy>();
        static readonly List<(float at, List<Mutant> who)> Waves = new List<(float, List<Mutant>)>();   // the horde, released in waves (seconds after the alarm)
        static readonly float[] DeathTimes = { 3.0f, 4.4f, 5.8f, 7.2f, 8.8f };
        static readonly Dictionary<HumanEnemy, float> NextStep = new Dictionary<HumanEnemy, float>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { _mapSince = _stageAt = _descentAt = _hordeAt = _gameOverAt = -1f; _stage = 0; _warned1 = _warned2 = _released1 = _released2 = false; Escorts.Clear(); Waves.Clear(); NextStep.Clear(); }

        public static int Step => Story.Step(Mission);
        static bool Active => Step >= 1 && !Story.IsDone(Mission);
        static List<Player> Everyone => Player.All.ToList();
        static LandmarkInstance Mark(string name) => (TerrainMap.Landmarks ?? new List<LandmarkInstance>()).FirstOrDefault(l => l.Name == name);
        static float DistTo(Player p, string landmark) { var l = Mark(landmark); return l == null ? 9999f : Mathf.Sqrt((p.X - l.X) * (p.X - l.X) + (p.Y - l.Y) * (p.Y - l.Y)); }

        static void Put(ICombatant u, int x, int y) { var (fx, fy) = TerrainMap.FindNearestEnterable(x, y); u.X = fx; u.Y = fy; Rt.SetOffset(u, 0f, 0f); }
        static void Say(string text) => GameLog.Write(text);
        static void Hear(string sfx, float vol = 1f) { var p = Player.Current; if (p != null) Sfx.Play(sfx, TacticalGridView.GridToWorld(p.X, p.Y), vol, 0.05f, 1f); }

        // ------------------------------------------------------------------ the per-frame tick

        public static void Tick(float dt)
        {
            if (Net.Mode == Net.Role.Client || Player.Current == null || TestingGround.Here || Survival.On) return;
            float now = Time.time;
            if (Story.IsDone(Mission)) return;
            int step = Step;
            if (step == 1 && _stageAt < 0f) { Story.SetStep(Mission, 0); step = 0; }          // a load in the middle of the grab: start over
            if (step == 0) { TryStart(now); return; }
            if (step == 1) { Grab(now); return; }
            if (step == 2) { KeepDocile(); return; }                                          // the interrogation is a ChoiceUI; its answer goes on from there
            if (step == 3) { Descent(now); return; }
            if (step == 4) { Alarm(now); }
        }

        // ------------------------------------------------------------------ 0 -> 1: the grab

        static void TryStart(float now)
        {
            if (TerrainMap.ZoneId != "scrapyard" || EncounterState.InCombat) { _mapSince = -1f; return; }   // Tier 2's one map (the Cutting and the Causeway both end on it)
            var p = Player.Current;
            if (!p.IsAlive() || Body.Down(p)) return;
            if (_mapSince < 0f) _mapSince = now;
            bool near = DistTo(p, "Gatehouse") <= 10f || DistTo(p, "The Causeway") <= 10f;
            if (!near && now - _mapSince < 240f) return;
            Story.SetStep(Mission, 1); _stage = 0; _stageAt = now;
            foreach (var pl in Everyone) Rt.Knockdown(pl, 9f);
            Story.Fade(Color.white, 1f, 0.12f);
            Hear("explosion", 1f);
            Say("A flashbang bounces off the road. White. Nothing but white and a ringing that swallows everything.");
        }

        static void Grab(float now)
        {
            float t = now - _stageAt;
            foreach (var pl in Everyone) if (pl.IsAlive()) Rt.Knockdown(pl, 3f);              // they stay down for the whole beating
            if (_stage == 0 && t >= 1.4f) { _stage = 1; Say("Boots. Fists. A rifle butt across the back of the head."); Hear("hit_flesh"); }
            if (_stage == 1 && t >= 2.6f) { _stage = 2; Hear("hit_flesh"); Say("Somebody is laughing. Somebody says: \"Enough. Bag them.\""); }
            if (_stage == 2 && t >= 4.0f) { _stage = 3; Story.Fade(Color.black, 1f, 1.2f); }
            if (_stage == 3 && t >= 6.0f) { _stage = 4; WakeAtBase(); Story.Fade(Color.black, 0f, 1.8f); }
            if (_stage == 4 && t >= 9.0f) { _stage = 5; Interrogate(); }
        }

        // ------------------------------------------------------------------ 2: the Garrison's base

        static void WakeAtBase()
        {
            var dock = Mark(Dock);
            int k = 0;
            foreach (var pl in Everyone)
            {
                if (dock != null) Put(pl, dock.X - 1 + k++ % 3, dock.Y + 2 + k / 3);
                pl.Hp = Mathf.Max(1, pl.Hp - pl.MaxHp / 10);                                // the beating
                Rt.Knockdown(pl, 0.5f);
            }
            Story.SetStep(Mission, 2);
            Escorts.Clear(); NextStep.Clear();
            if (dock != null) for (int i = 0; i < 5; i++) Escorts.Add(NewSoldier(dock.X + (i % 3) * 2 - 2, dock.Y - 2 - i / 3, i == 0));
            foreach (var e in Escorts) EncounterState.Enemies.Add(e);
            Say("You come to on concrete. A hatch stands open in the floor, and a Garrison squad stands round it.");
        }

        static HumanEnemy NewSoldier(int x, int y, bool leader)
        {
            var (w, a) = EncounterGeneration.PickFactionLoadout("Garrison", leader ? "Professional" : "Experienced", 2, isLeader: leader);
            var h = new HumanEnemy(leader ? "Garrison Sergeant" : "Garrison Soldier", x, y, "Garrison", leader ? "Professional" : "Experienced", w, a) { IsAggroed = false };
            var (fx, fy) = TerrainMap.FindNearestEnterable(x, y); h.X = fx; h.Y = fy;
            return h;
        }

        /// <summary>The soldiers never open fire until the story says so: awareness would otherwise turn them on you.</summary>
        static void KeepDocile() { foreach (var e in Escorts) if (e.IsAlive()) e.IsAggroed = false; }

        static void Interrogate()
        {
            Story.Talk("Garrison Sergeant", new[] {
                "Sit up. Hands where I can see them.",
                "You came up this road with something that points at a place that is not on our maps. We have been looking for a way into it for two years.",
                "So. What is down there?" },
                new[] { "I don't know.", "A lab. Old research, I think.", "Nothing you'd want." }, i =>
                {
                    Story.Talk("Garrison Sergeant", new[] {
                        i == 0 ? "Then you'll show us." : i == 1 ? "A lab. Good. Then you know the way." : "That's for us to decide. Show us.",
                        "Everything that shoots goes in the box. You can keep the knife and the armour - we are not animals. And a bandage, in case you need one.",
                        "Move. You go first, and you go where I tell you to stop." }, null, _ => BeginDescent());
                });
        }

        // ------------------------------------------------------------------ 2 -> 3: down the hatch

        public static void BeginDescent()
        {
            foreach (var pl in Everyone) Strip(pl, Player.All.IndexOf(pl));
            Story.Fade(Color.black, 1f, 0.8f);
            _descentAt = -2f;                                                              // the black is coming up; the real descent starts when it is solid (Descent)
            Story.SetStep(Mission, 3);
            Say("They take everything that shoots and drop it in a steel box. Then the rifle barrels point you down.");
        }

        static void EnterMaze()
        {
            Escorts.Clear(); NextStep.Clear(); EncounterState.Enemies.Clear();
            ZoneTravel.EnterZone(Maze, "industrial_basin");
            var l = Mark(Ladder); int k = 0;
            foreach (var pl in Everyone) { if (l != null) Put(pl, l.X - 1 + k++ % 3, l.Y - 1); Rt.Knockdown(pl, 0f); }
            SpawnEscorts();
            _descentAt = Time.time; _warned1 = _warned2 = false;
            Story.SetObjective(Mission, "Find the hidden lab's door - the soldiers are watching the clock", Door);
            Story.Journal("The Garrison dragged us down a hatch under the Loading Dock into a maze, to find a door nobody has ever opened. They are not patient.");
            SaveSystem.Save();                                                             // "Auto-save here": being shot means starting this walk again
            Story.Fade(Color.black, 0f, 1.5f);
        }

        static void SpawnEscorts()
        {
            Escorts.Clear(); NextStep.Clear();
            var p = Player.Current; if (p == null) return;
            var spots = Around(p.X, p.Y + 2, 5);                                           // round the player: at the ladder on the way down, at the door after the second auto-save
            for (int i = 0; i < spots.Count; i++) { var h = NewSoldier(spots[i].x, spots[i].y, i == 0); Escorts.Add(h); EncounterState.Enemies.Add(h); }
        }

        /// <summary>Which way the lab door lies from the nearest player, as a word.</summary>
        static string Bearing()
        {
            var d = Mark(Door); var p = Everyone.Where(x => x.IsAlive()).OrderBy(x => DistTo(x, Door)).FirstOrDefault(); if (d == null || p == null) return "ahead";
            float dx = d.X - p.X, dy = d.Y - p.Y; string ns = dy < -6 ? "north" : dy > 6 ? "south" : "", ew = dx > 6 ? "east" : dx < -6 ? "west" : "";
            return ns + (ns != "" && ew != "" ? "-" : "") + ew is string r && r != "" ? r : "close by";
        }

        static void Descent(float now)
        {
            if (_descentAt == -2f) { if (Story.FadeAlpha > 0.98f) EnterMaze(); return; }
            if (TerrainMap.ZoneId != Maze) { _descentAt = -1f; return; }
            if (_descentAt < 0f)                                                            // a load into the maze (the auto-save): the soldiers and the clock start again
            {
                SpawnEscorts(); _descentAt = now; _warned1 = _warned2 = false;
                Story.SetObjective(Mission, "Find the hidden lab's door - the soldiers are watching the clock", Door);
            }
            KeepDocile(); FollowPlayers(now, Mark(Door), 3.5f);
            if (GameOver(now)) return;
            float t = now - _descentAt;
            if (!_warned1 && t > ShotAfter * 0.55f) { _warned1 = true; Say("\"Faster. Or I decide you don't know the way.\""); Say($"The sergeant wets a finger. \"Draught from the {Bearing()}. And the floor - the old concrete. Someone kept this route up. Follow it.\""); }
            if (!_warned2 && t > ShotAfter * 0.85f) { _warned2 = true; Say("The sergeant's rifle clicks off safe."); }
            if (t > ShotAfter) { Say("\"Enough.\" A flat crack."); Hear("shot_rifle"); LoseAndReload(); return; }
            if (Everyone.Any(p => p.IsAlive() && DistTo(p, Door) <= 4.5f)) { SaveSystem.Save(); StartAlarm(now); }   // a second auto-save: dying in the run starts at the door, not at the ladder (replay cost)
        }

        // ------------------------------------------------------------------ 3 -> 4: the door, the alarm, the horde

        static void StartAlarm(float now)
        {
            Story.SetStep(Mission, 4); _hordeAt = now; _stage = 0;
            Say("The sergeant shoves you at the lock. You run the card through it. A red light. A buzz. A scream of sirens down the whole corridor.");
            Hear("explosion", 0.8f);
            Say("At the far end of the corridor the wall bursts outward. Something answers from the dark behind it. Many somethings.");
            SpawnHorde();
            Story.SetObjective(Mission, "RUN - back to the hatch ladder", Ladder);
        }

        static Mutant Spawn(string tier, string key, int x, int y)
        {
            var def = MutantDatabase.Tiers[tier][key]; var (fx, fy) = TerrainMap.FindNearestEnterable(x, y);
            var m = EncounterGeneration.CreateMutantFromDb(def, fx, fy); m.IsAggroed = false; EncounterState.Enemies.Add(m); return m;
        }

        /// <summary>`n` free tiles in a spiral round (x, y): where a wave stands before it is let go.</summary>
        static List<(int x, int y)> Around(int x, int y, int n)
        {
            var spots = new List<(int, int)>(); var taken = new HashSet<(int, int)>(EncounterState.Enemies.Select(e => (e.X, e.Y)));
            for (int r = 0; r <= 12 && spots.Count < n; r++)
                for (int dy = -r; dy <= r && spots.Count < n; dy++)
                    for (int dx = -r; dx <= r && spots.Count < n; dx++)
                    {
                        if (System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy)) != r) continue;
                        int tx = x + dx, ty = y + dy;
                        if (tx < 1 || ty < 1 || tx >= TerrainMap.Width - 1 || ty >= TerrainMap.Height - 1 || !TerrainMap.CanEnterTile(tx, ty) || taken.Contains((tx, ty))) continue;
                        spots.Add((tx, ty)); taken.Add((tx, ty));
                    }
            return spots;
        }

        static void Wave(float at, int x, int y, params (string key, int n)[] kinds)
        {
            var who = new List<Mutant>(); var spots = Around(x, y, kinds.Sum(k => k.n)); int i = 0;
            foreach (var (key, n) in kinds) for (int k = 0; k < n && i < spots.Count; k++, i++) who.Add(Spawn("tier1", key, spots[i].x, spots[i].y));
            Waves.Add((at, who));
        }

        /// <summary>Yan (2026-11-16): "the horde feels kinda small" - now about 45: the soldiers' killers at the broken wall, then three waves out of the dark tunnel (rats in dozens, Maws and Prowlers among them).</summary>
        static void SpawnHorde()
        {
            Waves.Clear();
            var bw = Mark("Broken Wall"); var nest = Mark("Nest"); if (bw == null || nest == null) return;
            Wave(10f, bw.X + 4, bw.Y, ("PallidProwler", 4), ("BlindMaw", 4));
            Wave(12f, nest.X, nest.Y - 10, ("Swarm", 12));
            Wave(15f, nest.X, nest.Y - 4, ("Swarm", 12), ("BlindMaw", 3));
            Wave(18f, nest.X, nest.Y + 2, ("Swarm", 10), ("PallidProwler", 4));
        }

        static void Alarm(float now)
        {
            float t = now - _hordeAt;
            if (TerrainMap.ZoneId != Maze) { Out(); return; }
            // the soldiers charge down the corridor toward the noise and die one after another
            var bwl = Mark("Broken Wall"); var east = new LandmarkInstance { Name = "charge", X = (bwl?.X ?? 70) - 12, Y = bwl?.Y ?? 9 };
            foreach (var e in Escorts) if (e.IsAlive()) e.IsAggroed = false;
            FollowPlayers(now, east, 0f, charge: true);
            while (_stage < DeathTimes.Length && t >= DeathTimes[_stage])
            {
                var h = Escorts.FirstOrDefault(e => e.IsAlive());
                if (h != null) { Hear("shot_rifle", 0.9f); Hear(_stage % 2 == 0 ? "claw" : "hit_flesh"); Say(_stage % 2 == 0 ? "A soldier screams. Then a lot of them do." : "Automatic fire, a short burst, and a wet sound over it."); h.Hp = 0; }
                _stage++;
            }
            foreach (var w in Waves) if (t >= w.at && w.who.Any(m => m.IsAlive() && !m.IsAggroed)) { foreach (var m in w.who.Where(m => m.IsAlive())) m.IsAggroed = true; if (w.at == 10f) Say("Some of them have stopped feeding. They are looking down the corridor at you."); else if (w.at == 15f) Say("More. Dozens more, coming up out of the dark tunnel."); }
            if (GameOver(now)) return;
            if (t > 6f && Everyone.Any(p => p.IsAlive() && DistTo(p, Ladder) <= 3f)) Out();
        }

        // ------------------------------------------------------------------ the soldiers' walking

        static void FollowPlayers(float now, LandmarkInstance goal, float stayBack, bool charge = false)
        {
            var live = Everyone.Where(p => p.IsAlive()).ToList(); if (live.Count == 0) return;
            foreach (var h in Escorts.Where(e => e.IsAlive()))
            {
                if (NextStep.TryGetValue(h, out var at) && now < at) continue;
                NextStep[h] = now + (charge ? 0.22f : 0.3f);
                int tx, ty;
                if (charge) { tx = goal.X; ty = goal.Y; }
                else
                {
                    var near = live.OrderBy(p => CombatantUtil.Distance(p, h)).First();
                    if (CombatantUtil.Distance(near, h) <= 3f) continue;                 // close enough: the rifle stays on them
                    tx = near.X; ty = near.Y;
                }
                h.Mp = System.Math.Max(h.Mp, TurnLoop.WalkMp);
                h.MoveToward(tx, ty);
            }
        }

        // ------------------------------------------------------------------ being shot, or killed on the run: the auto-save

        static bool GameOver(float now)
        {
            if (_gameOverAt >= 0f) { if (now >= _gameOverAt) { _gameOverAt = -1f; Reload(); } return true; }
            if (Everyone.Any(p => p.IsAlive() && !Body.Down(p))) return false;
            LoseAndReload(); return true;
        }

        static void LoseAndReload()
        {
            foreach (var p in Everyone) { Rt.Knockdown(p, 5f); }
            Story.Fade(Color.black, 1f, 1.0f);
            _gameOverAt = Time.time + 1.6f;
            Say("Everything goes dark.");
        }

        static void Reload()
        {
            if (!SaveSystem.Load()) { foreach (var p in Everyone) { p.Hp = p.MaxHp; Body.Reset(); } Story.SetStep(Mission, 3); _descentAt = -2f; Story.Fade(Color.black, 1f, 0.1f); return; }   // no auto-save to go back to: the walk simply starts again
            Story.Fade(Color.black, 0f, 1.5f);
        }

        // ------------------------------------------------------------------ 4 -> done: out, with the box

        static void Out()
        {
            Story.Fade(Color.black, 1f, 0.6f);
            Escorts.Clear(); Waves.Clear(); NextStep.Clear();
            EncounterState.Enemies.Clear();
            ZoneTravel.EnterZone("industrial_basin", Maze);
            foreach (var p in Everyone) { Rt.Knockdown(p, 0f); }
            Story.Finish(Mission);
            Story.Set("taken_done");
            foreach (var p in Everyone) Return(p, Player.All.IndexOf(p));
            Story.Journal("The Garrison took us under the Loading Dock to open a door they could not. Whatever it hides, the mutants in that corridor answered the alarm in hundreds. Worth coming back with better gear.");
            Say("You haul yourself up the ladder. The Garrison camp is empty. Behind you, the hatch breathes warm air. In the steel box beside it: everything you own that shoots.");
            Story.Fade(Color.black, 0f, 1.6f);
        }

        // ------------------------------------------------------------------ the weapons box (world flags: "box:<player>:<role>:<cat>|<key>|<condition>|<upgrades>|<optic>|<under>")

        static string Pack(string role, int pi, string cat, string key, Weapon w) =>
            $"box:{pi}:{role}:{cat}|{key}|{w?.Condition}|{Upgrades.Join(w?.Upgrades)}|{w?.Optic}|{w?.Under}";

        static void Strip(Player p, int pi)
        {
            var flags = MacroState.Current.StoryFlags;
            if (p.EquippedWeapon != null && !CombatRules.IsMelee(p.EquippedWeapon)) flags.Add(Pack("main", pi, p.EquippedWeaponCategory, p.EquippedWeaponKey, p.EquippedWeapon));
            if (p.SecondaryWeapon != null && !CombatRules.IsMelee(p.SecondaryWeapon)) flags.Add(Pack("second", pi, p.SecondaryCategory, p.SecondaryKey, p.SecondaryWeapon));
            if (p.OffhandWeapon != null) flags.Add(Pack("bag", pi, "sidearm", p.OffhandKey, p.OffhandWeapon));
            foreach (var it in p.Inventory.Items.Where(i => i.Type == "weapon" && i.Category != "melee")) flags.Add($"box:{pi}:bag:{it.Category}|{it.Key}|{it.Condition}|{it.Upgrades}|{it.Optic}|{it.Under}");
            for (int guard = 0; guard < 4 && p.EquippedWeapon != null && !CombatRules.IsMelee(p.EquippedWeapon); guard++) p.UnequipWeapon();
            if (p.SecondaryWeapon != null && !CombatRules.IsMelee(p.SecondaryWeapon)) p.StowSecondary();
            p.StowOffhand();
            foreach (var it in p.Inventory.Items.Where(i => i.Type == "weapon" && i.Category != "melee").ToList()) p.RemoveItem(it.Id, System.Math.Max(1, it.Qty));
            var knife = p.Inventory.Items.FirstOrDefault(i => i.Type == "weapon" && i.Category == "melee");
            if (p.EquippedWeapon == null && knife != null) p.EquipWeaponFromInventory(knife.Id);   // never bare-handed when there is a blade left
        }

        static void Return(Player p, int pi)
        {
            var flags = MacroState.Current.StoryFlags; string prefix = $"box:{pi}:";
            var mine = flags.Where(f => f.StartsWith(prefix)).ToList(); string mainId = null, secondId = null;
            foreach (var f in mine)
            {
                var role = f.Substring(prefix.Length, f.IndexOf(':', prefix.Length) - prefix.Length); var parts = f.Substring(f.IndexOf(':', prefix.Length) + 1).Split('|');
                var def = ShopDatabase.Items.FirstOrDefault(d => d.Type == "weapon" && d.Category == parts[0] && d.Key == parts[1]); if (def == null) continue;
                var item = new InventoryItem { Id = def.Id, Name = def.Name, WeightKg = def.WeightKg, Qty = 1, Type = "weapon", Price = def.Price, Category = parts[0], Key = parts[1],
                    Condition = string.IsNullOrEmpty(parts[2]) ? null : parts[2], Upgrades = string.IsNullOrEmpty(parts[3]) ? null : parts[3], Optic = string.IsNullOrEmpty(parts[4]) ? null : parts[4], Under = string.IsNullOrEmpty(parts[5]) ? null : parts[5] };
                p.AddItem(item);
                if (role == "main") mainId = item.Id; else if (role == "second") secondId = item.Id;
            }
            flags.RemoveAll(f => f.StartsWith(prefix));
            if (mainId != null) p.EquipWeaponFromInventory(mainId);
            if (secondId != null) p.EquipSecondaryFromInventory(secondId);
        }
    }
}
