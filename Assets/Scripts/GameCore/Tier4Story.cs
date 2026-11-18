// Story 8 (2026-11-18): Tier 4 - The Barrier (STORY_DIALOGUE.md Tier 4; STORY_OUTLINE_T4_T7.md Tier 4). The Vanguard Barracks (Liberators' Commander Miro at the Command Post, Tolik Brodsky at the
// new "The Barrier" landmark, Commander Dovbush of the Sentinels at the Sentinel Firebase, a Hunter at the Motor Pool).
// 1. The Liberators' Call ("barrier" 1): Miro (after the Sub-Station reveal): the Obelisk are massing in the Crimson Woods and they're ORGANISED. Tolik's talk. E at the Barrier: "wait for dawn".
// 2. The Siege ("barrier" 2): an auto-save, dawn (05:00), four Liberator defenders hold the line; four waves of Obelisk come at the barrier - a rifle line from the north (6), grenade men on both flanks
//    (5), a machine-gun nest on the ridge (3), then the Preacher (Preacher.cs) with four guards - and, as the outline asks, eight mutants gather behind the fanatics and DON'T attack them (listening). When
//    the Preacher falls and everyone is dead the mutants scatter; Miro: "take it far away from my men" (the core, intact or cracked). If every defender dies the dawn save loads. +1500 Ru.
// 3. The Apex ("apex"): the Hunter: something takes whole patrols in the Crimson Woods (boots found). "Old Crimson", a named giant (six times a Dread Prowler's health), lairs at Deadfall Clearing; claw marks
//    "higher than a man can reach"; kill it, bring the trophy: +1200 Ru.
// 4. The Summit ("summit", after the siege): Miro asks you to bring Dovbush (E at the Firebase); at the Command Post the two argue; your line decides the truce ([1] "driven, by the people who make Husks", [2]
//    "you did it once before"); flag `truce_sentinels_liberators`; +800 Ru. (If you fought the Liberators at the Bridge Miro's line says so.)
// ponytail: the defenders and the fanatics are real placed squads (they fight each other through NpcFights/RtAi); Tolik is an unkillable unit who doesn't fight yet; the waves' win rate was NOT measured; no art for the wave or the
// core's glow. Rewards go to every player.
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class Tier4Story
    {
        public const string Command = "Command Post", BarrierName = "The Barrier", Firebase = "Sentinel Firebase", Pool = "Motor Pool", Deadfall = "Deadfall Clearing", Trophy = "story_trophy";
        public static string ZoneOverride;
        static string Map4 => MacroMapDatabase.MapOf("vanguard_barracks");
        static bool OnTier4 => (ZoneOverride ?? TerrainMap.ZoneId) == Map4;

        static float _siegeAt = -1f;
        static readonly List<Mutant> Defenders = new List<Mutant>(), Fanatics = new List<Mutant>(), Listeners = new List<Mutant>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { _siegeAt = -1f; _spawned = 0; _beat = 0; _gunners = false; Defenders.Clear(); Fanatics.Clear(); Listeners.Clear(); Barrier.Reset(); }

        public static bool TryInteract(Player p)
        {
            if (!OnTier4) return false;
            if (Story.Near(p, Command, 3) && TalkCommand()) return true;
            if (Story.Step("barrier") == 1 && Story.Near(p, BarrierName, 4)) { TalkTolik(); return true; }
            if (Story.Near(p, Pool, 3) && TalkHunter()) return true;
            if (Story.Step("summit") == 1 && Story.Near(p, Firebase, 3)) { TalkDovbush(); return true; }
            if (Story.Step("summit") == 3 && Story.Near(p, Command, 4)) { Summit(); return true; }
            return false;
        }

        // ---------------- the Call ----------------

        static bool TalkCommand()
        {
            int b = Story.Step("barrier"), s = Story.Step("summit");
            if (b == 0 && Story.Has("tier3_reveal"))
            {
                bool fought = Story.Has("bridge_fought");
                Story.Talk("Commander Miro", new[] { "The Obelisk are massing in the Crimson Woods. More than I've ever seen. And they're organised - since when are fanatics organised?",
                    "They'll hit our barrier at dawn. I need every gun. Even yours." + (fought ? " Even after the bridge. Help us hold, and we'll call that forgotten." : " Help us hold." ) });
                Story.SetStep("barrier", 1); Story.SetObjective("barrier", "Join Tolik at the Barrier and hold until dawn", BarrierName);
                Story.Journal("The Barrier: the Obelisk are massing, organised, and will hit the Liberators' barrier at dawn.");
                return true;
            }
            if (Story.IsDone("barrier") && s == 0)
            {
                Story.Talk("Commander Miro", new[] { "The Sentinels hold the north road. If their line breaks, so does mine. Dovbush won't sit with me - he thinks I'm an anarchist. Bring him to the Command Post. He salutes his own boots, but he listens to people who bleed." });
                Story.SetStep("summit", 1); Story.SetObjective("summit", "Bring Commander Dovbush from the Sentinel Firebase", Firebase);
                Story.Journal("The Summit: Miro wants the Sentinels and Liberators at one table.");
                return true;
            }
            return false;
        }

        static void TalkTolik()
        {
            Story.Talk("Tolik Brodsky", new[] { "Hey! New faces. I'm Tolik. This is Gertrude. (slaps his exoskeleton) Say hello, Gertrude.", "...She's shy.", "You hold the left, I'll stand in the middle and be very large. That's my whole plan. It usually works.",
                "Dawn's a few hours off. Ready, or want to wait for it?" }, new[] { "Wait for dawn.", "Not yet." }, i => { if (i == 0) BeginSiege(); });
        }

        // ---------------- the Siege ----------------
        // A horde (Total fanatics) fed in at a pressure: LiveTarget alive at once (growing), a batch of four whenever the count is under it, from the Crimson Woods 40 tiles north of the wall. They walk to
        // the Barrier (Barrier.Tick) and tear at it; the defenders (eight Liberators) hold the catwalk. Story beats by how many have come: the line (0), grenade men (30), a machine-gun nest of
        // Professionals (55), the Preacher with guards (75). The siege ends when all of them are down, the Preacher too. It is lost if the wall is gone and every defender is dead.

        public const int Total = 90, DefenderCount = 8, Batch = 4;
        public const float BatchGap = 5f, TargetStart = 20f, TargetEnd = 30f, RampSeconds = 360f;
        static int _spawned, _beat; static float _nextBatch;
        /// <summary>The siege is on (the HUD's bar), and how many of the horde are down.</summary>
        public static bool SiegeOn => Story.Step("barrier") == 2 && _siegeAt >= 0f;
        public static int HordeDown => Mathf.Min(Total, Fanatics.Count(f => !f.IsAlive()));

        static void PlaceNear(string mission, int x, int y, string faction, List<Mutant> members)
        {
            SquadManager.PlaceQuestSquad(TerrainMap.RegionAt(x, y) ?? "vanguard_barracks", faction, members, "story:" + mission);
            int k = 0;
            foreach (var m in members) { var (fx, fy) = TerrainMap.FindNearestEnterable(x + (k % 4) * 2 - 3, y + (k / 4) * 2, 8); m.X = fx; m.Y = fy; k++; }
        }

        static List<Mutant> Obelisk(int n, string rank, int frags = 0)
        {
            var list = new List<Mutant>();
            for (int i = 0; i < n; i++) { var (w, a) = EncounterGeneration.PickFactionLoadout("Obelisk", rank, 4, i == 0); var h = new HumanEnemy("Fanatic", 0, 0, "Obelisk", rank, w, a) { Frags = frags }; list.Add(h); }
            return list;
        }

        static void BeginSiege()
        {
            var bl = TerrainMap.Landmarks.FirstOrDefault(l => l.Name == BarrierName); if (bl == null) return;
            SaveSystem.Save();
            MacroState.Current.Hour = 5f;
            Story.Fade(Color.black, 0f, 0.1f);
            Story.SetStep("barrier", 2); Story.SetObjective("barrier", "Hold the Barrier", BarrierName);
            Defenders.Clear(); Fanatics.Clear(); Listeners.Clear(); GameData_Reset();
            _siegeAt = Time.time; _spawned = 0; _beat = 0; _gunners = false; _nextBatch = Time.time + 12f; Preacher.Reset(); GameLog.Event("Dawn. The Obelisk are coming out of the Crimson Woods. Hold the wall.");
            var def = new List<Mutant>();
            for (int i = 0; i < DefenderCount; i++) { var (w, a) = EncounterGeneration.PickFactionLoadout("Liberators", "Experienced", 4, i == 0); def.Add(new HumanEnemy("Liberator", 0, 0, "Liberators", "Experienced", w, a)); }
            PlaceNear("siege_def", bl.X, bl.Y, "Liberators", def);
            int dx = 0;
            foreach (var m in def) { int cx = Barrier.X0 + 10 + dx * 6; var (fx, fy) = TerrainMap.FindNearestEnterable(cx, Barrier.StepY(cx), 3); m.X = fx; m.Y = fy; m.IsAggroed = false; dx++; }   // along the catwalk's front row
            Defenders.AddRange(def);
            // eight mutants gather behind the fanatics and wait (the outline: they are listening)
            var lis = new List<Mutant>();
            for (int i = 0; i < 8; i++) { var d = EncounterGeneration.PickRandomMutantByTier(4, 4) ?? EncounterGeneration.PickRandomMutantByTier(1, 5); lis.Add(EncounterGeneration.CreateMutantFromDb(d, 0, 0)); }
            PlaceNear("siege_listeners", Barrier.CX, Barrier.ApexY - 46, "Mutant", lis); Listeners.AddRange(lis);
            Story.Radio("Miro", "Here they come! Everyone to the wall!");
        }

        static void GameData_Reset() { Barrier.Horde.Clear(); }

        static bool _gunners;
        static void Feed()
        {
            int x = Random.Range(Barrier.X0 + 4, Barrier.X1 - 3), y = Barrier.ApexY - 40;
            string tag = "siege_h" + _spawned;
            if (_beat == 0 && _spawned >= 30) { _beat = 1; GameLog.Event("They're on the right - grenades!"); }
            if (_beat == 1 && _spawned >= 55) { _beat = 2; }
            if (_beat == 2 && !_gunners)                                                    // a machine-gunner squad moves up (Professionals with belt-fed guns)
            {
                _gunners = true; GameLog.Event("A machine-gun squad is moving up through the trees!");
                var gun = WeaponDatabase.Categories.ContainsKey("heavy") ? WeaponDatabase.Categories["heavy"].Values.FirstOrDefault() : null;
                var gs = new List<Mutant>();
                for (int i = 0; i < 3; i++) { var (w, a) = EncounterGeneration.PickFactionLoadout("Obelisk", "Professional", 4, i == 0); gs.Add(new HumanEnemy("Machine Gunner", 0, 0, "Obelisk", "Professional", gun ?? w, a)); }
                PlaceNear("siege_gunners", x, y, "Obelisk", gs);
                foreach (var m in gs) { m.IsAggroed = true; Fanatics.Add(m); Barrier.Horde.Add(m); }
            }
            var pack = Obelisk(Batch, "Experienced", _beat >= 1 && _spawned % 3 == 0 ? 2 : 0);
            if (_beat == 2 && _spawned >= 75) { _beat = 3; SpawnPreacher(x, y); pack = Obelisk(Batch, "Professional"); }
            PlaceNear(tag, x, y, "Obelisk", pack);
            foreach (var m in pack) { m.IsAggroed = true; Fanatics.Add(m); Barrier.Horde.Add(m); }
            _spawned += Batch;
        }

        static void SpawnPreacher(int x, int y)
        {
            var pr = Preacher.Spawn(x, y - 4); var (px, py) = TerrainMap.FindNearestEnterable(x, y - 4, 8); pr.X = px; pr.Y = py;
            Barrier.Horde.Add(pr);
            var husks = Husk.Pack(4, 10);                                                    // his escort: plenty of Husks (the leash is in his hands)
            PlaceNear("siege_husks", x, y - 2, Husk.Faction, husks);
            foreach (var m in husks) { m.IsAggroed = true; Fanatics.Add(m); Barrier.Horde.Add(m); }
            GameLog.Event("Through the smoke: a tall figure in Obelisk white, a glowing violet sphere strapped to his back, arms raised."); Banter.Say(pr, "Be still. Be quiet. Be one.", 2);
        }

        public static void TickRt(float dt)
        {
            if (Net.Mode == Net.Role.Client || Story.Step("barrier") != 2 || _siegeAt < 0f) return;
            float now = Time.time, t = now - _siegeAt;
            Barrier.Tick(dt);
            int alive = Fanatics.Count(f => f.IsAlive());
            float target = Mathf.Lerp(TargetStart, TargetEnd, Mathf.Clamp01(t / RampSeconds));
            if (_spawned < Total && now >= _nextBatch && alive < target) { Feed(); _nextBatch = now + BatchGap; }
            foreach (var m in Fanatics) if (m.IsAlive()) m.IsAggroed = true;
            foreach (var m in Listeners) if (m.IsAlive()) m.IsAggroed = false;      // they wait, listening
            foreach (var m in Defenders.OfType<HumanEnemy>()) if (m.IsAlive() && Fanatics.Any(f => f.IsAlive() && CombatantUtil.Distance(m, f) < 34)) m.IsAggroed = true;
            if (Defenders.Count > 0 && Defenders.All(d => !d.IsAlive()) && Barrier.Fraction <= 0f) { Lose(); return; }
            if (_spawned >= Total && Preacher.Dead && Fanatics.All(f => !f.IsAlive())) Win();
        }

        static void Lose()
        {
            Story.SetStep("barrier", 1); _siegeAt = -1f;
            GameLog.Event("[ALERT] The wall is down and the line is overrun.");
            Story.Fade(Color.black, 1f, 0.6f);
            if (!SaveSystem.Load()) { Story.Fade(Color.black, 0f, 0.6f); foreach (var m in Fanatics) EncounterState.Enemies.Remove(m); Fanatics.Clear(); Barrier.Horde.Clear(); }
        }

        static void Win()
        {
            _siegeAt = -1f; Barrier.Horde.Clear();
            foreach (var m in Listeners.Where(m => m.IsAlive()).ToList()) EncounterState.Enemies.Remove(m);
            GameLog.Event("Behind the fallen line the waiting mutants turn together and go - not scattering in panic: leaving, the way a crowd leaves when a song stops. They will regroup farther in.");
            if (Story.Has("tier3_reveal")) Story.Radio("Osei", "Every mutant on that ridge facing the same way. For hours. Listening. I want to know to what.");
            Story.Set(Preacher.Exploded ? "core_cracked" : "core_intact"); Story.Set("barrier_held");
            Story.Talk("Commander Miro", new[] { "We held. We actually held.", "...Whatever you just pulled off his back - take it far away from my men. Keep it, break it, or sell it. I don't want to know which." },
                new[] { "I'll keep it safe.", "Destroy it.", "A Garrison buyer pays well for things like this. (+1200 Ru)" }, i =>
                {
                    string id = Preacher.Exploded ? "story_core_cracked" : "story_core";
                    if (i == 1) { Player.All.ForEach(pl => pl.RemoveItem(id, 1)); Story.Set("core_destroyed"); Story.Journal("The core is destroyed. Whatever it could do, nobody will do it now."); }
                    else if (i == 2) { Player.All.ForEach(pl => pl.RemoveItem(id, 1)); Tier1Story.Pay(1200, "the core, sold to the Garrison"); Story.Set("core_sold_garrison"); Story.Journal("The core went to a Garrison buyer. The Garrison work for \"those\" without knowing it."); }
                    else Story.Set("core_kept");
                });
            Tier1Story.Pay(1500, "the Barrier");
            Story.Journal("The Barrier: the Preacher's core is the countermeasure's missing piece." + (Preacher.Exploded ? " (It is cracked.)" : ""));
            Story.Finish("barrier"); Story.Journal("The mutants gathered behind the Obelisk and did not attack them. They left when the Preacher fell.");
        }

        // ---------------- the Apex ----------------

        static bool TalkHunter()
        {
            int a = Story.Step("apex");
            if (a == 3 && Tier1Story.Holder(Trophy) is Player h)
            {
                h.RemoveItem(Trophy, 1);
                Story.Talk("Hunter", new[] { "That's... that's it. That's really it. Take the trophy to the scientists - they pay for anything with that many teeth. Here's my share.", "Whatever it was, it's quiet now." });
                Tier1Story.Pay(1200, "Old Crimson"); Story.Journal("The Apex: Old Crimson is dead."); Story.Finish("apex"); return true;
            }
            if (a == 0)
            {
                Story.Talk("Hunter", new[] { "Something's taking whole patrols in the red woods. Not killing - taking. We find boots.", "My grandfather called it Old Crimson. I thought it was a story to keep kids out of the trees." });
                Story.SetStep("apex", 1); Story.SetObjective("apex", "Find what takes patrols at the Deadfall Clearing", Deadfall);
                Story.Journal("The Apex: something takes whole patrols in the Crimson Woods.");
                var d = MutantDatabase.Tiers["tier4"].Values.OrderByDescending(x => x.Hp).First();
                var m = EncounterGeneration.CreateMutantFromDb(d, 0, 0); m.MaxHp = m.Hp = d.Hp * 6;
                PlaceNear("apex", Deadfall, "Mutant", new List<Mutant> { m });
                return true;
            }
            if (a >= 1 && a <= 2) { Story.Talk("Hunter", new[] { "The Deadfall Clearing. Look up when you get there." }); return true; }
            return false;
        }

        static void PlaceNear(string mission, string landmark, string faction, List<Mutant> members)
        {
            var l = TerrainMap.Landmarks.FirstOrDefault(x => x.Name == landmark); if (l == null) return;
            PlaceNear(mission, l.X, l.Y, faction, members);
        }

        // ---------------- the Summit ----------------

        static void TalkDovbush()
        {
            Story.Talk("Commander Dovbush", new[] { "Miro wants a table? I didn't come here to drink with anarchists.", "...But the road north is mine to hold, and the Obelisk come in numbers I don't like. Fine. I will stand in the same room as him. Once." });
            Story.SetStep("summit", 3); Story.SetObjective("summit", "Meet Miro and Dovbush at the Command Post", Command);
        }

        static void Summit()
        {
            Story.Talk("Summit", new[] { "Dovbush: I didn't come here to drink with anarchists.", "Miro: And I didn't come to be lectured by a man who salutes his own boots." },
                new[] { "The Obelisk are being driven. By the same people who make Husks. You both bleed for this or neither of you does.", "You did it once before, I hear. A truce, the last time someone tried to put a leash on the Zone." }, i =>
                {
                    Story.Talk("Summit", new[] { i == 1 ? "Dovbush: ...We did. It cost us both. Fine." : "Miro: Driven... by the ones who make Husks? Then we have the same enemy.", "Dovbush: We hold the north road. Your people hold the woods.", "Miro: Until it's done. Not a day longer.", "Dovbush: Not a day longer." });
                    Story.Set("truce_sentinels_liberators"); Tier1Story.Pay(800, "the truce");
                    foreach (var pl in Player.All) { pl.AdjustReputation("Liberators", Player.RepPoint); pl.AdjustReputation("Sentinels", Player.RepPoint); }
                    Story.Journal("The Summit: the Sentinels and Liberators agree a truce. Not a day longer than the Obelisk threat.");
                    Story.Finish("summit"); Story.SetStep("summit", Story.Done);
                });
        }

        // ---------------- every game minute ----------------

        public static void Tick()
        {
            var p = Player.Current; if (p == null || Net.Mode == Net.Role.Client || !OnTier4) return;
            if (Story.Step("apex") == 1)
            {
                var l = TerrainMap.Landmarks.FirstOrDefault(x => x.Name == Deadfall);
                if (l != null && System.Math.Max(System.Math.Abs(p.X - l.X), System.Math.Abs(p.Y - l.Y)) < 22 && !Story.Has("apex_marks")) { Story.Set("apex_marks"); GameLog.Event("Claw marks on a tree, higher than a man can reach."); }
                if (Tier1Story.Cleared("apex")) { Story.SetStep("apex", 3); Story.SetObjective("apex", "Take the trophy to the Hunter at the Motor Pool", Pool); Tier1Story.Give(Trophy, "Old Crimson's Trophy"); }
            }
        }
    }
}
