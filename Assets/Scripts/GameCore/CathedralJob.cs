// The cathedral job (Yan, 2026-11-20; MISSION_CHOIR.md): a repeatable Contractors contract inside the Abandoned City's cathedral (zone abandoned_cathedral). Ring the bell (E at the Bell Rope): the toll deafens you for a few seconds
// (aim blown, the screen muffled) and sends the Husks into a short frenzy; each toll a wave climbs through the windows and comes in at the doors. Every Husk that falls is paid (15 Ru) and is one fewer in the city's population (90, it
// refills 12 a day). Their blood draws the unaware mutants placed in the aisles and the crypt on its own (Noise.Smell, rule 7): nothing is spawned beside you. At 40 kills the bell stops, the last Husks fall, and the Verger walks in from
// the altar (VergerBoss.cs, Boss.Verger): no more Husks while he stands. His end pays the bonus and a cache in the sacristy; the contract can be taken again after 4 days.
// Co-op: the waves grow with the players, every player is paid, every player near the rope is deafened. Host-side only (not synced yet). Not run in Unity.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameCore
{
    public static class CathedralJob
    {
        public const string Zone = "abandoned_cathedral", Rope = "Bell Rope", AltarMark = "Altar", Sacristy = "Sacristy";
        public const int PopStart = 90, PayPerHusk = 15, Bonus = 1500, CooldownDays = 4, RegrowPerDay = 12, MaxAlive = 18;
        public static int Target = 40;                       // (the Testing Ground may lower it)
        public const float RopeReach = 2.2f, RingGap = 5f, FrenzyFor = 6f, DeafenFor = 3f, DeafenRange = 18f, VergerAfter = 20f, MinSpawnDistance = 9f;
        public static string ZoneOverride;                   // harness
        public static bool On => (ZoneOverride ?? TerrainMap.ZoneId) == Zone;

        public enum Stage { Idle, Ringing, Closing, Verger, Done }
        public static Stage St; public static int Kills, Tolls;
        static float _lastRing = -99f, _frenzyUntil = -1f, _closedAt = -1f; static Boss.Fight _verger;
        static readonly List<HumanEnemy> Husks = new List<HumanEnemy>(); static readonly HashSet<HumanEnemy> Counted = new HashSet<HumanEnemy>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { St = Stage.Idle; Kills = Tolls = 0; _lastRing = -99f; _frenzyUntil = _closedAt = -1f; _verger = null; Husks.Clear(); Counted.Clear(); Target = 40; }

        // ---- the city's Husk population (saved in the story steps: cath_pop = pop + 1, 0 = never touched; cath_pop_day; cath_ready = the first day the contract is open again) ----
        public static int Pop
        {
            get
            {
                int raw = Story.Step("cath_pop"); if (raw == 0) return PopStart;
                int p = raw - 1, d = MacroState.Current.Day - Story.Step("cath_pop_day");
                return Mathf.Clamp(p + Mathf.Max(0, d) * RegrowPerDay, 0, PopStart);
            }
        }
        static void SetPop(int p) { Story.SetStep("cath_pop", Mathf.Clamp(p, 0, PopStart) + 1); Story.SetStep("cath_pop_day", MacroState.Current.Day); }
        public static int ReadyDay => Story.Step("cath_ready");
        public static bool Open => MacroState.Current.Day >= ReadyDay;

        static IEnumerable<LandmarkInstance> Marks(System.Func<string, bool> f) => (TerrainMap.Landmarks ?? new List<LandmarkInstance>()).Where(l => f(l.Name));
        static LandmarkInstance Mark(string name) => Marks(n => n == name).FirstOrDefault();

        /// <summary>E at the Bell Rope: the first pull takes the contract and rings; every pull after rings again (not sooner than RingGap s).</summary>
        public static bool TryInteract(Player p)
        {
            if (!On || Net.Mode == Net.Role.Client) return false;
            var l = Mark(Rope); if (l == null || CombatantUtil.Distance(p, l.X, l.Y) > RopeReach) return false;
            if (St == Stage.Closing || St == Stage.Verger) { GameLog.Write("The rope hangs slack. The bell has stopped."); return true; }
            if (St == Stage.Idle || St == Stage.Done)
            {
                if (!Open) { GameLog.Write($"The rope is cold and still. Nothing has come back to the city yet (the contract opens again on day {ReadyDay})."); return true; }
                if (Pop <= 0) { GameLog.Write("You pull the rope. The bell rings out over an empty city, and nothing answers."); return true; }
                Begin();
            }
            if (Time.time - _lastRing < RingGap) { GameLog.Write("The bell is still swinging."); return true; }
            Toll(l);
            return true;
        }

        static void Begin()
        {
            St = Stage.Ringing; Kills = 0; Tolls = 0; Husks.Clear(); Counted.Clear(); _verger = null; _closedAt = -1f;
            Story.Talk("CONTRACTORS - HUSK CLEARANCE", new[] { $"A notice nailed to the rope post, in a Contractor's block capitals: \"HUSKS IN THE CITY: {Pop} COUNTED. {PayPerHusk} RU A HEAD. {Target} IN ONE SITTING AND THE COMPANY PAYS {Bonus} MORE. RING THE BELL. THEY COME TO IT. DO NOT STAND IN THE DOORWAYS.\"",
                "Below it, in pencil: \"The verger does not like the bell. He will come when it has rung enough. Do not be there when he does.\"" });
            GameLog.Event("The contract is taken. Ring the bell.");
        }

        /// <summary>The toll: deafens everyone near the rope, puts the Husks in a frenzy, and a wave climbs in.</summary>
        static void Toll(LandmarkInstance l)
        {
            Tolls++; _lastRing = Time.time; _frenzyUntil = Time.time + FrenzyFor;
            CombatFx.PxAt("thunderclap", l.X, l.Y); Sfx.Play("gaunt_scream", TacticalGridView.GridToWorld(l.X, l.Y), 1f, 0.03f, 0.6f);
            foreach (var q in Player.All.Where(q => q.IsAlive() && CombatantUtil.Distance(q, l.X, l.Y) <= DeafenRange)) Body.Concuss(q, DeafenFor);
            GameLog.Event(Tolls == 1 ? "The bell goes off over your head like a collapsing roof. The whole nave rings with it, and your ears give out. Somewhere in the walls, something starts to climb."
                                     : $"Toll {Tolls}. The stone hums. Your ears are shut, and the Husks are quick.");
            SendWave();
        }

        public static int WaveSize => Mathf.Min(14, 4 + Tolls + 2 * Mathf.Max(0, Player.All.Count - 1));
        static IEnumerable<LandmarkInstance> Entries => Marks(n => n.StartsWith("Window ") || n == "West Door" || n == "East Door");

        static void SendWave()
        {
            Husks.RemoveAll(h => h == null || !h.IsAlive());
            int n = Mathf.Min(Mathf.Min(WaveSize, MaxAlive - Husks.Count), Pop - Kills - Husks.Count); if (n <= 0) return;
            var far = Entries.Where(e => Player.All.All(q => !q.IsAlive() || CombatantUtil.Distance(q, e.X, e.Y) >= MinSpawnDistance)).ToList();     // never beside you: they come from the far doors and windows
            if (far.Count == 0) far = Entries.OrderByDescending(e => Player.All.Min(q => CombatantUtil.Distance(q, e.X, e.Y))).Take(3).ToList();
            if (far.Count == 0) return;
            for (int i = 0; i < n; i++)
            {
                var e = far[(i + Tolls) % far.Count]; var (x, y) = TerrainMap.FindNearestEnterable(e.X + (i % 3) - 1, e.Y + i / 3 % 3 - 1);
                bool heavy = i == 0 && Tolls % 5 == 0;                                           // every fifth toll a Husk with a better kit leads
                var h = Husk.Make(6, heavy); h.X = x; h.Y = y; h.IsAggroed = true; Rt.SetOffset(h, 0f, 0f);
                RtAi.Hold(h, e.Name.StartsWith("Window") ? 1.1f : 0.3f);                         // a beat on the sill: the climb in
                EncounterState.Enemies.Add(h); Husks.Add(h);
                if (e.Name.StartsWith("Window") && (i % 3 == 0)) { var gb = CombatFx.Push("glassburst", null, null); gb.Now = true; gb.FromX = e.X; gb.FromY = e.Y; }      // the glass bursts inward (the view draws the shards and plays the sound)
            }
            GameLog.Event(Tolls == 1 ? "Glass bursts down both walls. They come in over the sills and through the doors, heads tipped to the bell." : "More of them, through the windows and the doors.");
        }

        /// <summary>RtAi.Speed: for a few seconds after a toll the Husks are quick.</summary>
        public static float Hurry(Mutant m) => On && Time.time < _frenzyUntil && Husk.Is(m) ? 1.35f : 1f;

        /// <summary>Every frame (Rt.cs).</summary>
        public static void Tick(float dt)
        {
            if (Net.Mode == Net.Role.Client) return;
            if (!On) { if (St != Stage.Idle) { Reset(); } return; }                              // you left: the session ends (the kills were paid and counted as they fell)
            if (St == Stage.Idle || St == Stage.Done) return;
            float now = Time.time;
            foreach (var h in Husks.Where(h => h != null && !h.IsAlive() && Counted.Add(h)).ToList())          // each Husk that falls: paid, and one fewer in the city
            {
                Kills++; SetPop(Pop - 1); foreach (var q in Player.All) q.Ru += PayPerHusk;
            }
            if (St == Stage.Ringing && Kills >= Target) Close(now);
            if (St == Stage.Closing && (Husks.All(h => h == null || !h.IsAlive()) || now - _closedAt >= VergerAfter)) SendVerger();
            if (St == Stage.Verger && _verger != null && _verger.Finished) Finish();
        }

        static void Close(float now)
        {
            St = Stage.Closing; _closedAt = now; _frenzyUntil = -1f;
            GameLog.Event($"{Kills} Husks. The rope jerks out of your hand on its own, and the bell stops mid-swing. The silence is worse than the noise.");
        }

        static void SendVerger()
        {
            foreach (var h in Husks.Where(h => h != null && h.IsAlive()).ToList()) { EncounterState.Enemies.Remove(h); }      // no Husks while he stands: they drop where they are
            Husks.Clear();
            var a = Mark(AltarMark) ?? Mark(Rope); if (a == null) { St = Stage.Done; return; }
            var (x, y) = TerrainMap.FindNearestEnterable(a.X, a.Y + 1);
            St = Stage.Verger; _verger = VergerBoss.Spawn(x, y, true);
        }

        static void Finish()
        {
            St = Stage.Done; Story.SetStep("cath_ready", MacroState.Current.Day + CooldownDays);
            foreach (var q in Player.All) q.Ru += Bonus;
            var s = Mark(Sacristy);
            if (s != null) { var (cx, cy) = TerrainMap.FindNearestEnterable(s.X, s.Y); Stashes.Hoard(cx, cy, "Sacristy Cache", 6, "What the verger kept under the vestments: the contract's wages, a collection plate and a beast hunter's tools."); }
            GameLog.Event($"The company pays {Bonus} Ru for the clearance. The sacristy door is open.");
            Story.Journal("The cathedral: the Contractors pay for every Husk the bell draws in, and the verger comes at the end of every run. He is down. The contract opens again in four days.");
        }

        /// <summary>Testing Ground: into the cathedral with a short contract (`target` kills) at the rope.</summary>
        public static void StartTest(int target = 8)
        {
            Reset(); Target = target; EncounterState.Enemies.Clear(); EncounterState.InCombat = false; Boss.Reset();
            Story.SetStep("cath_ready", 0); SetPop(PopStart);
            MacroState.Current.CurrentZoneId = Zone; ZoneTravel.EnterZone(Zone);
            var l = Mark(Rope); if (l == null) return;
            int k = 0; foreach (var p in Player.All) { var (x, y) = TerrainMap.FindNearestEnterable(l.X + k % 3, l.Y + 2 + k / 3); p.X = x; p.Y = y; Rt.SetOffset(p, 0f, 0f); k++; }
            GameLog.Write($"[TESTING GROUND] The cathedral job, short contract ({target} Husks, then the Verger). E at the Bell Rope. Main menu > Testing Ground to come back.");
        }
    }
}
