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
        public const string ChaseTrack = "mus_no_escape";   // Yan (2026-11-17): "add this song to the escape sequence, after the gaunt scene" (NO ESCAPE, Daniel Herrera; CREDITS_AUDIO.md)
        public const float ShotAfter = 240f;            // seconds in the maze before they lose patience (Yan, 2026-11-16: "7 minutes might be too much, we need them feeling nervous"; the route is ~200 tiles, a minute if you knew it)

        static float _mapSince = -1f, _stageAt = -1f, _descentAt = -1f, _hordeAt = -1f, _gameOverAt = -1f;
        static int _stage;                              // the grab's stage while step 1; the horde's soldiers lost so far while step 4
        static bool _warned0, _warned1, _warned2, _released1, _released2, _chaseMusic, _doorHint;
        static float _doorAt = -1f; static int _doorStage; static readonly List<(int x, int y)> Posts = new List<(int, int)>();
        static readonly List<HumanEnemy> Escorts = new List<HumanEnemy>();
        static readonly List<(float at, List<Mutant> who)> Waves = new List<(float, List<Mutant>)>();   // the horde, released in waves (seconds after the alarm)
        static readonly float[] DeathTimes = { 4.5f, 5.8f, 7.1f, 8.4f, 9.8f };   // seconds after the Gaunt's scream
        static readonly Dictionary<HumanEnemy, float> NextStep = new Dictionary<HumanEnemy, float>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { _mapSince = _stageAt = _descentAt = _hordeAt = _gameOverAt = -1f; _stage = 0; _warned0 = _warned1 = _warned2 = _released1 = _released2 = false; Escorts.Clear(); Waves.Clear(); NextStep.Clear(); _gaunt = null; _screamAt = -1f; _gauntWalking = false; _chaseMusic = false; Sfx.StopMusic(0f); _doorAt = -1f; _doorStage = 0; _doorHint = false; Posts.Clear(); }

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
            if (step == 2) { AtBase(now); return; }                                           // the interrogation is a ChoiceUI; its answer goes on from there (BeginDescent)
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
            BeginGrab(now);
        }

        /// <summary>The grab itself: the flashbang, the beating, black, the base (Grab goes on from here every frame).</summary>
        static void BeginGrab(float now)
        {
            Story.SetStep(Mission, 1); _stage = 0; _stageAt = now;
            foreach (var pl in Everyone) Rt.Knockdown(pl, 9f);
            Story.Fade(Color.white, 1f, 0.12f);
            Hear("explosion", 1f);
            Say("A flashbang bounces off the road. White. Nothing but white and a ringing that swallows everything.");
        }

        /// <summary>True from the grab until the mission is finished (the Testing Ground's respawn restarts it instead of going back to the range).</summary>
        public static bool InProgress => Step >= 1 && !Story.IsDone(Mission);

        /// <summary>The Testing Ground's "Tier 2: Taken" button (Yan, 2026-11-16: "add the mission to the testing ground, a button that takes us directly to the cutscene where we're kidnapped and the mission
        /// starts"): forget the mission (not started, not done, no guns in the box), go to the Scrapyard, stand everyone by the Gatehouse and begin the grab right now. From there it is the real mission: the beating,
        /// the interrogation, the hatch, the maze and its clock, the alarm and the horde, then out of the hatch with the guns back. Press the button again to start over.</summary>
        public static void StartForTest()
        {
            foreach (var pl in Everyone) Return(pl, Player.All.IndexOf(pl));                  // pressed mid-mission: the guns in the box come back first
            var flags = MacroState.Current.StoryFlags;
            flags.RemoveAll(f => f.StartsWith("box:") || f == "f:taken_done");
            Story.SetStep(Mission, 0); Story.SetObjective(Mission, null); Reset();
            MacroState.Current.CurrentZoneId = "scrapyard";
            ZoneTravel.EnterZone("scrapyard");
            EncounterState.Enemies.Clear(); EncounterState.InCombat = false;
            var gate = Mark("Gatehouse"); int k = 0;
            foreach (var pl in Everyone) { if (gate != null) Put(pl, gate.X - 1 + k % 3, gate.Y + 1 + k / 3); k++; Rt.Knockdown(pl, 0f); }
            BeginGrab(Time.time);
        }

        static void Grab(float now)
        {
            float t = now - _stageAt;
            foreach (var pl in Everyone) if (pl.IsAlive()) Rt.Knockdown(pl, 3f);              // they stay down for the whole beating
            if (_stage == 0 && t >= 1.4f) { _stage = 1; Say("Boots. Fists. A rifle butt across the back of the head."); Hear("hit_flesh"); }
            if (_stage == 1 && t >= 2.6f) { _stage = 2; Hear("hit_flesh"); Say("Somebody is laughing. Somebody says: \"Enough. Bag them.\""); }
            if (_stage == 2 && t >= 4.0f) { _stage = 3; Story.Fade(Color.black, 1f, 1.2f); }
            if (_stage == 3 && t >= 6.0f) { _stage = 4; WakeAtBase(); Story.Fade(Color.black, 0f, 1.8f); }   // WakeAtBase moves the story to step 2: from here AtBase runs, not Grab
        }

        /// <summary>Step 2, every frame: the soldiers stay calm, you are still coming round (nobody shoots a soldier before the questions), and 3 s after the wake the sergeant starts the interrogation.
        /// (2026-11-17, found by Yan in Unity: this used to sit at the end of Grab, which stops running the moment WakeAtBase sets step 2 - so the questions never came, the soldiers stood about and you kept your gun.
        /// A load into step 2 has _stage 0: it asks at once.)</summary>
        static void AtBase(float now)
        {
            KeepDocile();
            if (_stage >= 5) return;                                                       // the ChoiceUI has the floor
            foreach (var pl in Everyone) if (pl.IsAlive()) Rt.Knockdown(pl, 0.4f);
            if (_stage != 4 || now - _stageAt >= 9.0f) { _stage = 5; Interrogate(); }
        }

        /// <summary>The Garrison squad that holds you: while the story runs them (the base, the maze, the horde) they take no part in the game's own awareness - no "!", no "Intruder! Engage!", no FIGHT,
        /// no sitting down - because TurnLoop.CheckVisionAndWander would alert them to you every game minute and KeepDocile can only undo the aggro, not the shouting.</summary>
        public static bool Holds(ICombatant u) => Step >= 2 && !Story.IsDone(Mission)
            && (u is HumanEnemy h ? Escorts.Contains(h) : u is Mutant m && !m.IsAggroed && (m == _gaunt || Waves.Any(w => w.who.Contains(m))));   // (the horde and the Gaunt wait in the dark, unaggroed, until Alarm lets them go)

        /// <summary>Steps 1-3 (the beating, the base, the walk to the door): nobody walks out of the hatch, down to the lab or up the ladder - the guns are in the box and only the story gives them back. (Step 4, the
        /// run, ends by walking out: Alarm notices it.)</summary>
        /// <summary>The siren is wailing (Sfx.AmbientFor asks: the view re-sets the zone's ambience every frame, which used to drown it).</summary>
        public static bool AlarmOn => Step == 4 && TerrainMap.ZoneId == Maze;
        public static bool BlocksTravel => Step >= 1 && Step <= 3;

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
            var sergeant = Escorts.FirstOrDefault(e => e.IsAlive());                          // the squad turns to you while he talks
            Story.Talk("Garrison Sergeant", new[] {
                "Sit up. Hands where I can see them.",
                "You came up this road with something that points at a place that is not on our maps. We have been looking for a way into it for two years.",
                "So. What is down there?" },
                new[] { "I don't know.", "A lab. Old research, I think.", "Nothing you'd want." }, i =>
                {
                    Story.Talk("Garrison Sergeant", new[] {
                        i == 0 ? "Then you'll show us." : i == 1 ? "A lab. Good. Then you know the way." : "That's for us to decide. Show us.",
                        "Everything that shoots goes in the box. You can keep the knife and the armour - we are not animals. And a bandage, in case you need one.",
                        "Move. You go first, and you go where I tell you to stop." }, null, _ => BeginDescent(), sergeant);
                }, sergeant);
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
            SpawnEscorts(); Sfx.Preload(ChaseTrack); Shout("Show us the way. And don't take too long - my patience is running thin.", 2.2f);   // Yan: the sergeant's first words down here, as the black clears
                                                  // the song is read from disk now, not in the middle of the alarm
            _descentAt = Time.time; _warned0 = _warned1 = _warned2 = false; _doorAt = -1f; _doorHint = false;
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
                SpawnEscorts(); _descentAt = now; _warned0 = _warned1 = _warned2 = false;
                Story.SetObjective(Mission, "Find the hidden lab's door - the soldiers are watching the clock", Door);
            }
            KeepDocile(); FollowPlayers(now, Mark(Door), 3.5f);
            if (GameOver(now)) return;
            if (_doorAt >= 0f) { DoorScene(now); return; }                                    // the scene stops the clock
            float t = now - _descentAt;
            if (!_warned0 && t > ShotAfter * 0.25f) { _warned0 = true; Shout("Don't stop. Don't look back at me. Walk."); }
            if (!_warned1 && t > ShotAfter * 0.55f) { _warned1 = true; Shout("Faster. Or I decide you don't know the way."); Shout($"Draught from the {Bearing()}. And the floor - old concrete. Someone kept this route up. Follow it."); }
            if (!_warned2 && t > ShotAfter * 0.85f) { _warned2 = true; Say("The sergeant's rifle clicks off safe."); }
            if (t > ShotAfter) { Shout("Enough."); Say("A flat crack."); Hear("shot_rifle"); LoseAndReload(); return; }
            if (!_doorHint && Everyone.Any(p => p.IsAlive() && DistTo(p, Door) <= 7f)) { _doorHint = true; Shout("There. That's the door. Go on - the card."); Say("E at the door: run the passcard through the lock."); }
        }

        // ------------------------------------------------------------------ 3 -> 4: the door, the alarm, the horde

        public static (int x, int y)? DoorTile => Mark(Door) is LandmarkInstance l ? (l.X, l.Y) : ((int, int)?)null;
        public static bool CanUseDoor(Player p) => Step == 3 && TerrainMap.ZoneId == Maze && _doorAt < 0f && _descentAt >= 0f && p != null && p.IsAlive() && DistTo(p, Door) <= 3.5f;
        /// <summary>E at the lab door (Yan, 2026-11-17: "a cutscene when we get to the door and interact with it before the gaunt scream"): the scene starts, with the second auto-save (dying later starts here, not at the ladder).</summary>
        public static bool TryUseDoor(Player p) { if (!CanUseDoor(p)) return false; _doorAt = Time.time; _doorStage = 0; SaveSystem.Save(); return true; }

        /// <summary>The door scene: the camera on the lock, everyone stands still; the sergeant orders the card, you run it, the reader buzzes - then the alarm (StartAlarm) and the Gaunt's scene.</summary>
        static void DoorScene(float now)
        {
            float t = now - _doorAt; var d = Mark(Door); if (d == null) { _doorAt = -1f; return; }
            Story.CamLock = (d.X + 0.5f, d.Y + 1.5f); Story.CamHalf = 5f; Story.CamRate = 3f; Story.Letterbox = true; Story.CutsceneUntil = Time.time + 0.4f; Story.LookAt = (d.X, d.Y);
            if (_doorStage == 0 && t >= 0.4f) { _doorStage = 1; Shout("That's it. The lock. Card - now."); }
            if (_doorStage == 1 && t >= 2.4f) { _doorStage = 2; Say("You run the passcard through the reader."); Hear("ui_click"); }
            if (_doorStage == 2 && t >= 3.8f) { _doorStage = 3; Say("The reader blinks once. A flat buzz."); Hear("dryfire"); }
            if (_doorStage == 3 && t >= 4.9f) { _doorStage = 4; Shout("Why isn't it opening? Again!"); }
            if (_doorStage == 4 && t >= 6.2f) { _doorAt = -1f; Story.CineEnd(); StartAlarm(now); }
        }

        /// <summary>What a soldier says: over his head (Banter), not in the log in the corner (Yan, 2026-11-17).</summary>
        static void Shout(string text, float delay = 0f)
        {
            var s = Escorts.FirstOrDefault(e => e.IsAlive()); if (s != null) Banter.Say(s, text, 2, delay); else Say("\"" + text + "\"");
        }

        static void StartAlarm(float now)
        {
            Story.SetStep(Mission, 4); _hordeAt = now; _stage = 0;
            Say("The sergeant shoves you at the lock. You run the card through it. A red light. A buzz. A scream of sirens down the whole corridor.");
            Hear("explosion", 0.8f);
            RedLights();
            Posts.Clear(); var dm = Mark(Door); int dx = dm?.X ?? 40;
            foreach (var (ox, oy) in new[] { (4, 6), (7, 11), (10, 6), (13, 11), (16, 6) }) Posts.Add(TerrainMap.FindNearestEnterable(dx + ox, oy));   // east of you, on the corridor's edges: the middle stays open for you to run west
            Say("At the far end of the corridor the wall bursts outward. Something answers from the dark behind it. Many somethings.");
            SpawnHorde();
            Story.SetObjective(Mission, "RUN - back to the hatch ladder", Ladder);
        }

        /// <summary>Yan (2026-11-16): the corridor is white-lit; when the card fails the ceiling lights turn to red hazard lights (their tiles change look, the light map and the lighting bake follow) and the siren starts.</summary>
        static void RedLights()
        {
            for (int y = 6; y <= 11; y++) for (int x = 6; x <= 80; x++) if (TerrainMap.GetTile(x, y)?.Look == "light") TerrainMap.SetTileType(x, y, TileType.Empty, "redlight");
            LightMap.Reset(); Lighting.Reset();                                              // (the picture's lamp field keeps its own bake of the lights: it must collect them again)
            Sfx.Ambient("amb_alarm");
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
            Wave(2.6f, bw.X + 4, bw.Y, ("PallidProwler", 4), ("BlindMaw", 4));                // seconds after the Gaunt's scream
            Wave(4.1f, nest.X, nest.Y - 10, ("Swarm", 12));
            Wave(6.6f, nest.X, nest.Y - 4, ("Swarm", 12), ("BlindMaw", 3));
            Wave(9.6f, nest.X, nest.Y + 2, ("Swarm", 10), ("PallidProwler", 4));
            { var (gx, gy) = TerrainMap.FindNearestEnterable(nest.X, nest.Y - 14); _gaunt = new Mutant(Gaunt.ElderName, gx, gy, Gaunt.ElderDef) { IsAggroed = false }; EncounterState.Enemies.Add(_gaunt); } _screamAt = -1f; _gauntWalking = false;     // the Gaunt waits in the dark tunnel; it walks out first
        }

        static Mutant _gaunt; static float _gauntStep, _screamAt = -1f; static bool _gauntWalking;

        /// <summary>Yan (2026-11-16): the Gaunt is the first thing out of the dark: it walks into the red light at the broken wall, SCREAMS and POINTS at the soldiers - and only then does the horde come.
        /// Timeline from the alarm: 2.5 s the Gaunt starts out of the tunnel (camera on it), it screams on arrival (or at 9 s), the waves follow 2.6 / 4.1 / 6.6 / 9.6 s after the scream, the soldiers fall from
        /// 4.5 s on, the Gaunt itself joins at 4 s.</summary>
        static void Alarm(float now)
        {
            float t = now - _hordeAt;
            if (TerrainMap.ZoneId != Maze) { Out(); return; }
            Sfx.Ambient("amb_alarm");                                                        // the siren keeps wailing (nothing else sets the ambient meanwhile)
            var dmk = Mark(Door); var bwk = Mark("Broken Wall"); int sx = bwk?.X ?? 80, sy = bwk?.Y ?? 9;      // Yan (2026-11-17): the Gaunt comes out of the dark tunnel and INTO the red-lit corridor, 20 tiles from the door, to scream at you
            // Yan: "the camera did not zoom at the gaunt and i didn't see it": from its first step to the end of its scream and point it stands in the dark, so it is lit for the scene (Vision.Spotlight) and the
            // cinematic camera (letterbox, zoom, as the eel's introduction) is locked on it; then the camera eases back to you.
            if (_gaunt != null && _gaunt.IsAlive() && t >= 2.5f && (_screamAt < 0f || now - _screamAt < Gaunt.CallTotal + 0.4f))
            { Vision.Spotlight = _gaunt; Story.CamLock = (_gaunt.X + 0.5f, _gaunt.Y + 0.5f); Story.CamHalf = 7f; Story.CamRate = 3f; Story.Letterbox = true; Story.CutsceneUntil = Time.time + 0.4f; Story.LookAt = (_gaunt.X, _gaunt.Y); }
            else if (Vision.Spotlight != null && Vision.Spotlight == _gaunt) { Vision.Spotlight = null; Story.CineEnd(); }
            foreach (var e in Escorts) if (e.IsAlive()) e.IsAggroed = false;
            { int i = 0; foreach (var h in Escorts) { if (i < Posts.Count && h.IsAlive() && (h.X, h.Y) != Posts[i] && now >= (NextStep.TryGetValue(h, out var at0) ? at0 : 0f)) { NextStep[h] = now + 0.25f; h.Mp = System.Math.Max(h.Mp, TurnLoop.WalkMp); h.MoveToward(Posts[i].x, Posts[i].y); } i++; } }
            if (_gaunt != null && _gaunt.IsAlive() && _screamAt < 0f && t >= 2.5f)
            {
                if (!_gauntWalking) { _gauntWalking = true; Say("Something tall is walking out of the dark."); }
                if (now >= _gauntStep)
                {
                    _gauntStep = now + 0.25f; _gaunt.Mp = System.Math.Max(_gaunt.Mp, TurnLoop.WalkMp);
                    if (System.Math.Abs(_gaunt.X - sx) > 1 || System.Math.Abs(_gaunt.Y - sy) > 1) { _gaunt.FaceToward(sx, sy); _gaunt.PatrolTarget = (sx, sy); _gaunt.MoveToward(sx, sy); }   // (it moonwalked: an unaggroed mutant is drawn turned toward its PatrolTarget, not its Facing)
                }
                bool arrived = System.Math.Abs(_gaunt.X - sx) <= 1 && System.Math.Abs(_gaunt.Y - sy) <= 2;
                if (arrived || t >= 20f)
                {
                    _screamAt = now; Gaunt.ForcePose(_gaunt, "scream", Gaunt.CallTotal); GauntScream(_gaunt);
                    Say("It opens its jaw wider than a jaw should open and screams. Then it lifts one long arm and points at the soldiers.");
                }
            }
            if (_gaunt == null || !_gaunt.IsAlive()) { if (_screamAt < 0f) _screamAt = now; }
            float ts = _screamAt < 0f ? -1f : now - _screamAt;
            if (ts < 0f) { GameOver(now); return; }                                         // the soldiers stand and stare at it until the scream is over
            if (!_chaseMusic && ts >= Gaunt.CallTotal) { _chaseMusic = true; Sfx.Music(ChaseTrack, 1.5f); }   // the scream and the point are over: the horde is let go and the chase song starts (it ends at the ladder, or when you go down)
            // Yan (2026-11-17): "the soldiers didn't shoot the mutants or hold their ground, they just kinda died without being touched": they hold the corridor and fight for real (real bullets at the nearest
            // mutant they can see; whatever reaches them is pinned on them and mauls them), so the horde has to chew through five rifles before it gets to you.
            SoldiersFight(now, ts);
            foreach (var w in Waves) if (ts >= w.at && w.who.Any(m => m.IsAlive() && !m.IsAggroed)) { foreach (var m in w.who.Where(m => m.IsAlive())) m.IsAggroed = true; if (w.at <= 2.7f) Say("Everything that was feeding turns its head toward the corridor and comes."); else if (w.at <= 4.2f) Say("More. Dozens more, coming up out of the dark tunnel."); }
            if (_gaunt != null && _gaunt.IsAlive() && ts >= 4f) _gaunt.IsAggroed = true;      // it joins the hunt: from here the game's Gaunt (its call, its drive) takes over
            if (GameOver(now)) return;
            if (t > 6f && Everyone.Any(p => p.IsAlive() && DistTo(p, Ladder) <= 3f)) Out();
        }

        static readonly Dictionary<HumanEnemy, float> NextFire = new Dictionary<HumanEnemy, float>(), NextMaul = new Dictionary<HumanEnemy, float>();
        static void SoldiersFight(float now, float ts)
        {
            var grid = GameServices.Grid; if (grid == null) return;
            var foes = EncounterState.Enemies.OfType<Mutant>().Where(m => !(m is HumanEnemy) && m.IsAlive() && m.IsAggroed).ToList();
            var targets = foes.Where(m => !Gaunt.IsElder(m)).ToList();                                 // the Elder is the scene's: the soldiers' fire goes to the rest
            foreach (var h in Escorts.Where(e => e.IsAlive()))
            {
                var foe = targets.Where(m => CombatantUtil.Distance(h, m) <= 18 && grid.CheckLineOfSight(h.X, h.Y, m.X, m.Y)).OrderBy(m => CombatantUtil.Distance(h, m)).FirstOrDefault();
                if (foe == null) continue;
                h.FaceToward(foe.X, foe.Y);
                var w = h.EquippedWeapon;
                if (w != null && !CombatRules.IsMelee(w) && !PlayerInLine(h, foe)) Fire(h, w, foe, now);
                foreach (var m in foes.Where(m => CombatantUtil.IsAdjacent(m, h)))                       // what reaches a soldier is stuck on him
                {
                    RtAi.Pin(m, 0.5f);
                    if (now >= (NextMaul.TryGetValue(h, out var nm) ? nm : 0f)) { NextMaul[h] = now + 0.45f; Hear(Random.value < 0.5f ? "claw" : "hit_flesh", 0.8f); h.Hp -= System.Math.Max(40, m.Db?.AttackDamage ?? 60); if (h.Hp <= 0) { h.Hp = 0; Hear("shot_rifle", 0.5f); Say("A soldier goes down under them, still firing."); } }
                }
            }
        }

        static readonly Dictionary<HumanEnemy, int> BurstLeft = new Dictionary<HumanEnemy, int>(); static readonly Dictionary<HumanEnemy, float> ReloadUntil = new Dictionary<HumanEnemy, float>();
        /// <summary>One soldier's trigger, by his own gun (Yan: "uncapped fire-rate", "never reload", "even the pistol soldier was firing as a machinegun", "perfect aim"): a self-loader fires one round per
        /// 0.4-0.9 s, an automatic fires 3-5 round bursts at its own rate and rests 1.2-2.4 s; every round leaves the magazine and an empty one costs the gun's reload time; the aim is rough (+2.5 spread).</summary>
        static void Fire(HumanEnemy h, Weapon w, Mutant foe, float now)
        {
            if (ReloadUntil.TryGetValue(h, out var ru) && now < ru) return;
            var hd = Gunplay.For(w);
            if (w.CurrentMagazine <= 0) { ReloadUntil[h] = now + System.Math.Max(1.5f, hd.ReloadTime); w.CurrentMagazine = w.MagCapacity; BurstLeft[h] = 0; Hear("reload_mag", 0.6f); return; }
            if (now < (NextFire.TryGetValue(h, out var nf) ? nf : 0f)) return;
            int left = BurstLeft.TryGetValue(h, out var b) && b > 0 ? b : (hd.Auto ? Random.Range(3, 6) : 1);
            w.CurrentMagazine--; left--; BurstLeft[h] = left;
            NextFire[h] = left > 0 ? now + 60f / System.Math.Max(60f, hd.Rpm) : now + (hd.Auto ? Random.Range(1.2f, 2.4f) : Random.Range(0.4f, 0.9f));
            Gunplay.NpcShootOnce(h, w, foe, 2.5f);
        }

        static bool PlayerInLine(HumanEnemy h, Mutant foe)
        {
            var (sx, sy) = Ballistics.Pos(h); var (tx, ty) = Ballistics.Pos(foe); float ex = tx - sx, ey = ty - sy, l2 = ex * ex + ey * ey; if (l2 < 0.01f) return false;
            foreach (var p in Everyone) { if (!p.IsAlive()) continue; var (px, py) = Ballistics.Pos(p); float u = ((px - sx) * ex + (py - sy) * ey) / l2; if (u <= 0f || u >= 1.05f) continue; float cx = sx + u * ex - px, cy = sy + u * ey - py; if (cx * cx + cy * cy < 1.0f) return true; }
            return false;
        }

        static void GauntScream(Mutant g) { Sfx.Play("gaunt_scream", TacticalGridView.GridToWorld(g.X, g.Y), 1f, 0.03f, 1f); }

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
            Sfx.StopMusic(0.8f); _chaseMusic = false; Vision.Spotlight = null; Story.CineEnd();
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
            Sfx.StopMusic(2.5f); _chaseMusic = false; Vision.Spotlight = null; Story.CineEnd();
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
