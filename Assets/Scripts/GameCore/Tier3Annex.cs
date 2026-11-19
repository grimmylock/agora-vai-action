// Tier 3, beat 4: "The Door Under the Dock" (STORY_GAUNT_LAB.md; Yan, 2026-11-19). Batch 2: the quest, the infested tunnels, the door scene (the "Taken" horde scene again, this time with guns), the Elder in its cavern, the card.
//  Osei (E at the Sub-Station House, after the Reveal and the "Taken" mission): the pulses under the lake have the rhythm of the Gaunt's scream; a core alone is a key without a lock, she needs the calibration of the leash, and
//  the only place it can be is behind the lab door under the Loading Dock. She gives a supply case (grenades, stims, medkits, armor-piercing rounds). Step "annex":
//   1 asked: go down the hatch under the Loading Dock (Industrial Basin; the hatch is a hidden node, opened here)   2 in the tunnels: packs of mutants between the ladder and the door (placed on the old blood trail)
//   3 the door: E at the Lab Door - the reader fails, the lights go red, the Elder walks out, screams and points; the horde (about 45 + 12 per extra player) comes out of the dark running. It withdraws into its cavern.
//   4 the horde is down: go into the cavern (the Gaunt Arena); the Elder is at its dais when you come near and fights (the boss module is batch 3: until then it is the ordinary Elder, 15,000 HP)
//   5 the Elder is dead: it drops a white card and a plate stamped "G-1" (flag `gaunt_was_those`); with the card in a pack the lab door opens (`annex_staff`)   6 you are in the Annex (the files, batch 4)
// Co-op: everything reads Player.All; the supply case goes to each player; the horde scales with their number.
// ponytail: the Elder is a plain fight until batch 3; nothing in the Annex yet; the infestation is placed again on each entry while step 2 (packs you killed come back if you leave and return).
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class Tier3Annex
    {
        public const string Mission = "annex", Zone = "taken_tunnels", Door = "Lab Door", Arena = "Gaunt Arena", Dais = "Gaunt Dais", Broken = "Broken Wall", Nest = "Nest", Card = "story_annex_card", Plate = "story_gaunt_plate";
        public static int Step => Story.Step(Mission);
        static bool InTunnels => TerrainMap.ZoneId == Zone;
        static LandmarkInstance Mark(string n) => (TerrainMap.Landmarks ?? new List<LandmarkInstance>()).FirstOrDefault(l => l.Name == n);
        static float DistTo(Player p, string n) { var l = Mark(n); return l == null ? 9999f : Mathf.Sqrt((p.X - l.X) * (p.X - l.X) + (p.Y - l.Y) * (p.Y - l.Y)); }

        static readonly List<Mutant> Packs = new List<Mutant>();
        static readonly List<(float at, List<Mutant> who)> Waves = new List<(float, List<Mutant>)>();
        static readonly HashSet<int> _out = new HashSet<int>();
        static Mutant _elder; static float _sceneAt = -1f, _screamAt = -1f, _gauntStep, _retreatAt = -1f; static bool _spawnedPacks, _walking;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { Packs.Clear(); Waves.Clear(); _out.Clear(); _elder = null; _sceneAt = _screamAt = _retreatAt = -1f; _spawnedPacks = _walking = false; }

        /// <summary>The story holds the Elder while the door scene runs (no spotting you, no wandering).</summary>
        public static bool Holds(ICombatant u) => u != null && u == _elder && _sceneAt >= 0f && Step == 3;

        // ------------------------------------------------------------------ Osei asks

        /// <summary>E at the Sub-Station House after the Reveal: Osei's ask (step 0) or a reminder / the hand-in (later steps). True if it spoke.</summary>
        public static bool TalkOsei()
        {
            if (!Story.Has("tier3_reveal") || !Story.IsDone("taken")) return false;
            int s = Step;
            if (s == 0)
            {
                Story.Talk("Dr. Priya Osei", new[] { "Wait. Before you go north. Sit down - you will want to hear this.",
                    "Under the Loading Dock, the Garrison took you to a door and an alarm went off, and you said the mutants answered it in hundreds. Describe what screamed.",
                    "...That is the pulse. I have been logging spikes under the lake for a month, and they come with the rhythm of that scream. One signal. I thought it was interference. It is a call.",
                    "A core alone is a key without a lock. To build the counter-signal I need to see how that call is SHAPED - the calibration of the leash. The only place it can be written down is behind that door. The Garrison sealed a lab there and never had the real card: whatever screamed has it.",
                    "Pavel: Last time they took your guns and shoved you in front of that door. This time you carry everything you own. Everything.",
                    "Konrad Hale: (clears his throat) I found what I could. Grenades, stims, armor-piercing rounds. Do not make me regret the paperwork." },
                    new[] { "We'll go.", "What are we walking into?" }, i =>
                {
                    if (i == 1) Story.Talk("Dr. Priya Osei", new[] { "A tunnel full of what answered, a corridor you know, and at the end of it a horde again. This time it is a fight and not a funeral: dig in, use the corridor, make them come to you.",
                        "And whatever screams. It stood in the dark and pointed. I do not think it is only an animal." });
                    Begin();
                });
                return true;
            }
            if (s >= 1 && s <= 5) { Story.Talk("Dr. Priya Osei", new[] { s <= 2 ? "The hatch under the Loading Dock, in the Industrial Basin. Take everything." : s == 3 || s == 4 ? "Hold the corridor, then the cavern past the broken wall. Whatever screams is at the far end." : "The card and the door. Then the files - the calibration is what I need. Bring me anything with numbers on it." }); return true; }
            return false;
        }

        static void Begin()
        {
            Story.SetStep(Mission, 1);
            Story.SetObjective(Mission, "Go down the hatch under the Loading Dock (Industrial Basin)", "Loading Dock");
            Story.Journal("The Door Under the Dock: Osei needs the calibration of the leash. It is behind the Garrison's lab door under the Loading Dock, and whatever screamed there has the real card.");
            var hid = MacroState.Current.DiscoveredHiddenNodes; if (!hid.Contains(Zone)) hid.Add(Zone);
            foreach (var p in Player.All) Supply(p);
        }

        static void Supply(Player p)
        {
            foreach (var (id, n) in new[] { ("fragGrenade", 4), ("basicStim", 3), ("militaryMedkit", 2), ("bandage", 2) })
            {
                var d = ShopDatabase.Find(id); if (d == null) continue;
                p.AddItem(new InventoryItem { Id = d.Id, Name = d.Name, WeightKg = d.WeightKg, Qty = n, Type = d.Type, Price = d.Price });
            }
            var w = p.EquippedWeapon; if (w?.Db != null && !CombatRules.IsMelee(w) && Ammo.Kinds(w.Db).Contains("ArmorPiercing")) Ammo.Add(p.Inventory, Ammo.Key(w.Db, "ArmorPiercing"), 120);
            GameLog.Write("Osei's supply case: grenades, stims, medkits and armor-piercing rounds.");
        }

        // ------------------------------------------------------------------ the tunnels, the door, the Elder

        static Mutant Make(string spec, int x, int y)
        {
            var tk = spec.Split(':'); string tier = tk.Length == 2 ? tk[0] : "tier1", key = tk.Length == 2 ? tk[1] : spec;
            var def = MutantDatabase.Tiers[tier][key]; var (fx, fy) = TerrainMap.FindNearestEnterable(x, y);
            var m = EncounterGeneration.CreateMutantFromDb(def, fx, fy); m.IsAggroed = false; return m;
        }

        static void PlacePacks()
        {
            _spawnedPacks = true;
            var tr = TierDungeons.TakenTrail; int n = tr.Length / 3; if (n < 8) return;
            var specs = new (float at, string[] kinds)[]
            {
                (0.20f, new[] { "PallidProwler", "PallidProwler", "BlindMaw" }),
                (0.38f, new[] { "Swarm", "Swarm", "Swarm", "Swarm", "Swarm", "BlindMaw" }),
                (0.55f, new[] { "PallidProwler", "PallidProwler", "PallidProwler", "tier2:VeilProwler" }),
                (0.72f, new[] { "BlindMaw", "BlindMaw", "tier2:VeilProwler", "tier2:VeilProwler" }),
                (0.88f, new[] { "PallidProwler", "BlindMaw", "tier4:DreadProwler" }),
            };
            int extra = Mathf.Max(0, Player.All.Count - 1);
            foreach (var (at, kinds) in specs)
            {
                int ti = Mathf.Clamp((int)(at * n), 0, n - 1); int bx = tr[ti * 3], by = tr[ti * 3 + 1]; int k = 0;
                foreach (var spec in kinds.Concat(Enumerable.Repeat(kinds[0], extra * 2)))
                { var m = Make(spec, bx + (k % 3) * 2 - 2, by + (k / 3) * 2 - 1); EncounterState.Enemies.Add(m); Packs.Add(m); k++; }
            }
        }

        static void BuildWaves()
        {
            Waves.Clear(); _out.Clear();
            var nest = Mark(Nest); if (nest == null) return;
            int extra = Mathf.Max(0, Player.All.Count - 1);
            void Wave(float at, int x, int y, params (string key, int n)[] kinds)
            {
                var who = new List<Mutant>(); var taken = new HashSet<(int, int)>(Waves.SelectMany(w => w.who).Select(m => (m.X, m.Y)));
                int placed = 0, total = kinds.Sum(kv => kv.n) + extra * 4;
                foreach (var kv in kinds)
                    for (int k = 0; k < kv.n + extra * 4 / kinds.Length && placed < total; k++, placed++)
                    {
                        var (sx, sy) = TerrainMap.FindNearestEnterable(x + (placed % 6) - 3, y + placed / 6, 10);
                        taken.Add((sx, sy)); who.Add(Make(kv.key, sx, sy));
                    }
                Waves.Add((at, who));
            }
            Wave(Gaunt.ScreamFor, nest.X, nest.Y - 12, ("PallidProwler", 6), ("BlindMaw", 5), ("tier2:VeilProwler", 2));
            Wave(Gaunt.ScreamFor + 0.5f, nest.X, nest.Y - 8, ("BlindMaw", 5), ("PallidProwler", 5));
            Wave(Gaunt.ScreamFor + 1.0f, nest.X, nest.Y - 4, ("Swarm", 6), ("BlindMaw", 3), ("tier4:DreadProwler", 2));
            Wave(Gaunt.ScreamFor + 1.5f, nest.X, nest.Y + 2, ("Swarm", 6), ("PallidProwler", 4), ("tier2:VeilProwler", 2));
        }

        public static bool TryInteract(Player p)
        {
            if (!InTunnels || Step < 2 || Step > 3 || _sceneAt >= 0f || !Story.Near(p, Door, 3)) return false;
            StartScene(); return true;
        }

        static void StartScene()
        {
            SaveSystem.Save();
            Story.SetStep(Mission, 3); _sceneAt = Time.time; _screamAt = -1f; _walking = false; _retreatAt = -1f;
            BuildWaves();
            GameLog.Event("The reader buzzes red. The lights go red down the whole corridor and the siren starts.");
            Sfx.Play("explosion", TacticalGridView.GridToWorld(Player.Current.X, Player.Current.Y), 0.8f, 0.05f, 1f);
            Tier2Story.RedLights();
            var nest = Mark(Nest);
            if (nest != null) { var (gx, gy) = TerrainMap.FindNearestEnterable(nest.X, nest.Y - 14); _elder = new Mutant(Gaunt.ElderName, gx, gy, Gaunt.ElderDef) { IsAggroed = false }; EncounterState.Enemies.Add(_elder); }
            Story.SetObjective(Mission, "Hold the corridor", Door);
        }

        /// <summary>Every frame (Rt.WorldTick).</summary>
        public static void TickRt(float dt)
        {
            if (Net.Mode == Net.Role.Client || Player.Current == null || Step < 1 || Story.IsDone(Mission)) return;
            float now = Time.time; int s = Step;
            if (InTunnels)
            {
                if (s == 1) { Story.SetStep(Mission, 2); s = 2; Story.SetObjective(Mission, "Fight through the tunnels to the lab door", Door); }
                if (s == 2 && !_spawnedPacks) PlacePacks();
                if (s == 3) TickScene(now);
                if (s == 4 || s == 3 && _screamAt >= 0f && _elder != null && !EncounterState.Enemies.Contains(_elder)) TickCavern();
                if (s == 5) TickCard();
            }
            else
            {
                if (s == 3) { Story.SetStep(Mission, 2); _sceneAt = -1f; _spawnedPacks = false; Packs.Clear(); Waves.Clear(); _elder = null; Vision.Spotlight = null; Story.CineEnd(); }   // out of the tunnels mid-scene: it starts again from the door
                if (s == 2 && TerrainMap.ZoneId != Zone) _spawnedPacks = false;
                if (TerrainMap.ZoneId == "loading_annex" && s == 5) { Story.SetStep(Mission, 6); Story.SetObjective(Mission, "Search the Annex for the Gaunt's files", "Control Room"); Story.Journal("The Annex: a lab cut into the rock under the Dock. Somewhere in it is the calibration Osei needs."); }
            }
        }

        static void TickScene(float now)
        {
            float t = now - _sceneAt; var bw = Mark(Broken); int sx = bw?.X ?? 80, sy = bw?.Y ?? 19;
            Sfx.Ambient("amb_alarm");
            const float WalkAt = 2.5f;
            // the camera: the broken wall, then the Gaunt as it walks out and screams; you stand still for it (as in "Taken")
            if (_elder != null && _elder.IsAlive() && t >= WalkAt && (_screamAt < 0f || now - _screamAt < Gaunt.CallTotal + 0.4f))
            { Vision.SpotAt = (sx, sy); Vision.Spotlight = _elder; Story.CamLock = (_elder.X + 0.5f, _elder.Y + 0.5f); Story.CamHalf = 7f; Story.CamRate = 3f; Story.Letterbox = true; Story.CutsceneUntil = Time.time + 0.4f; Story.LookAt = (_elder.X, _elder.Y); }
            else if (t < WalkAt) { Story.CamLock = (sx + 0.5f, sy + 0.5f); Story.CamHalf = 6f; Story.CamRate = 3f; Story.Letterbox = true; Story.CutsceneUntil = Time.time + 0.4f; Story.LookAt = (sx, sy); }
            else if (Vision.Spotlight != null && Vision.Spotlight == _elder) { Vision.Spotlight = null; Story.CineEnd(); }
            if (_elder != null && _elder.IsAlive() && _screamAt < 0f && t >= WalkAt)
            {
                if (!_walking) { _walking = true; GameLog.Event("Something tall is walking out of the dark."); }
                if (now >= _gauntStep)
                {
                    _gauntStep = now + 0.25f;
                    if (System.Math.Abs(_elder.X - sx) > 1 || System.Math.Abs(_elder.Y - sy) > 1) { _elder.FaceToward(sx, sy); _elder.PatrolTarget = (sx, sy); _elder.MoveToward(sx, sy); }
                }
                bool arrived = System.Math.Abs(_elder.X - sx) <= 1 && System.Math.Abs(_elder.Y - sy) <= 2;
                if (arrived || t >= 20f)
                {
                    _screamAt = now; Gaunt.ForcePose(_elder, "scream", Gaunt.CallTotal); Sfx.Play("gaunt_scream", TacticalGridView.GridToWorld(_elder.X, _elder.Y), 1f, 0.03f, 1f);
                    GameLog.Event("It opens its jaw wider than a jaw should open and screams. Then it lifts one long arm and points down the corridor - at you.");
                }
            }
            if (_screamAt < 0f) return;
            float ts = now - _screamAt;
            for (int wi = 0; wi < Waves.Count; wi++)
            {
                var w = Waves[wi]; if (ts < w.at || !_out.Add(wi)) continue;
                foreach (var m in w.who) { if (!EncounterState.Enemies.Contains(m)) EncounterState.Enemies.Add(m); m.IsAggroed = true; }
                if (wi == 0) GameLog.Event("Everything that was feeding turns its head toward the corridor and comes."); else if (wi == 1) GameLog.Event("More. Dozens more, up out of the dark tunnel.");
            }
            if (ts >= Gaunt.CallTotal + 0.6f && _retreatAt < 0f) { _retreatAt = now; if (_elder != null) { var d = Mark(Dais); if (d != null) { _elder.PatrolTarget = (d.X, d.Y); } } GameLog.Event("The Elder turns and goes back into the dark. The horde does not."); }
            if (_retreatAt >= 0f && _elder != null && EncounterState.Enemies.Contains(_elder))
            {
                var d = Mark(Dais);
                if (now >= _gauntStep) { _gauntStep = now + 0.25f; if (d != null) { _elder.FaceToward(d.X, d.Y); _elder.MoveToward(d.X, d.Y); } }
                if (now - _retreatAt > 12f) EncounterState.Enemies.Remove(_elder);            // far in the dark: it waits for you at the dais
            }
            if (_out.Count == Waves.Count && Waves.All(w => w.who.All(m => !m.IsAlive())))
            {
                if (_elder != null) EncounterState.Enemies.Remove(_elder);                       // it waits in the dark at its dais
                Story.SetStep(Mission, 4); Story.Radio("Pavel", "...Is that all of them? Listen. Something is still breathing in the dark, past the broken wall.");
                Story.SetObjective(Mission, "Go into the cavern past the broken wall - whatever screams waits at the far end", Arena);
                Vision.Spotlight = null; Story.CineEnd();
            }
        }

        static void TickCavern()
        {
            var a = Mark(Arena); if (a == null) return;
            bool near = Player.All.Any(p => p.IsAlive() && DistTo(p, Arena) <= 22f);
            if (near && Step == 4 && _elder != null && _elder.IsAlive())
            {
                if (!EncounterState.Enemies.Contains(_elder))
                {
                    var d = Mark(Dais) ?? a; var (ex, ey) = TerrainMap.FindNearestEnterable(d.X, d.Y); _elder.X = ex; _elder.Y = ey; Rt.SetOffset(_elder, 0f, 0f);
                    EncounterState.Enemies.Add(_elder); GameLog.Event("At the far end of the cavern, on a heap of arranged bones, the Elder stands up.");
                }
                _elder.IsAggroed = true;
            }
            if (_elder != null && !_elder.IsAlive() && Step == 4) ElderDown();
        }

        static void ElderDown()
        {
            Story.SetStep(Mission, 5); var (x, y) = (_elder.X, _elder.Y);
            TerrainMap.DropItem(x, y, new InventoryItem { Id = Card, Name = "Annex Keycard", Type = "key", Qty = 1, WeightKg = 0.05f, Price = 0 });
            TerrainMap.DropItem(x, y, new InventoryItem { Id = Plate, Name = "Plate: G-1", Type = "key", Qty = 1, WeightKg = 0.1f, Price = 0 });
            Story.Set("gaunt_was_those");
            GameLog.Event("It kneels, and the scream cuts off mid-breath. Every mutant in the cavern drops, then scatters. Round its neck, on a chain: a white card, and a steel plate stamped G-1.");
            Story.Journal("The Elder wore a plate stamped G-1 and carried the lab's real card. Whatever it was, someone made it - one of \"those\", an early experiment.");
            Story.SetObjective(Mission, "Take the card and open the lab door", Door);
        }

        static void TickCard()
        {
            if (Tier1Story.Holder(Card) == null) return;
            var key = LabLocks.Key("staff"); if (!MacroState.Current.Keys.Contains(key)) { MacroState.Current.Keys.Add(key); LabLocks.Granted(key); }
        }

        // ------------------------------------------------------------------ Testing Ground

        /// <summary>The Testing Ground's entry: stage 0 the tunnels (the hatch), 1 the corridor at the lab door, 2 the cavern after the horde. Everything the story needs is set.</summary>
        public static void StartForTest(int stage)
        {
            Reset(); var f = MacroState.Current.StoryFlags;
            Story.Set("tier3_reveal"); Story.Set("taken_done"); Story.SetStep("taken", Story.Done); Story.Clear("gaunt_was_those"); MacroState.Current.Keys.Remove("annex_staff");
            Story.SetStep(Mission, stage == 0 ? 1 : stage == 1 ? 2 : 4);
            MacroState.Current.CurrentZoneId = Zone; ZoneTravel.EnterZone(Zone);
            EncounterState.Enemies.Clear(); EncounterState.InCombat = false;
            var at = Mark(stage == 0 ? "Hatch Ladder" : stage == 1 ? Door : Arena); int k = 0;
            foreach (var p in Player.All) { if (at != null) { var (x, y) = TerrainMap.FindNearestEnterable(at.X + k % 3 - 1, at.Y + (stage == 1 ? 2 : 1) + k / 3); p.X = x; p.Y = y; Rt.SetOffset(p, 0f, 0f); } Supply(p); k++; }
            if (stage == 2)
            {
                var nest = Mark(Nest); var (gx, gy) = nest == null ? (0, 0) : TerrainMap.FindNearestEnterable(nest.X, nest.Y - 14);
                _elder = new Mutant(Gaunt.ElderName, gx, gy, Gaunt.ElderDef) { IsAggroed = false }; _screamAt = Time.time - 99f; _retreatAt = Time.time - 99f;   // (the Elder is added to the world when you come near the arena)
                Story.SetObjective(Mission, "Go into the cavern past the broken wall - whatever screams waits at the far end", Arena);
            }
            GameLog.Write("[TESTING GROUND] The Door Under the Dock: " + (stage == 0 ? "the tunnels, from the hatch" : stage == 1 ? "the corridor at the lab door (E at the door starts the scene)" : "the cavern, the horde is done") + ". Main menu > Testing Ground to come back.");
        }
    }
}
