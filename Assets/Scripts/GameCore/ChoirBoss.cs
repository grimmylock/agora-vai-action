// The Choir (Tier 6, Abandoned City; BOSS_ROSTER.md #10; Yan, 2026-11-20: "we just build a bell tower and add the waves of husks"). The boss is the Great Bell in the Clock Tower's frame: it never moves,
// it TOLLS on the 11 second beat (a sound wave that hurts everyone within range who has a clear line to the bell: break the line behind the pillars, or dive) and every toll a wave of Husks answers
// from the four streets round the plaza. Shoot the bell down; the Husks go quiet. No new creature art (the Husks are the existing units; the bell is one hand-built sprite).
// Phases: below 66 % the waves grow and the toll reaches further, below 33 % again. Co-op: waves grow with the player count, the toll hits every player with a line to it. Not synced over the network.
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class ChoirBoss
    {
        public const string Name = "The Great Bell", Tower = "Clock Tower", Flag = "choir_silenced";
        public const float BeatSeconds = 11f, WarnSeconds = 1.2f, WakeRange = 26f;
        public const int MaxHusks = 24;
        public static readonly MutantDef Def = new MutantDef { Name = Name, Tier = 5, Hp = 9000, PhysDR = 0.3f, AnomDR = 0.5f, Ap = 0, Mp = 0, Ac = 8, AttackDamage = 0, Behavior = "boss", PanicThreshold = 0f };
        public static string ZoneOverride;                  // harness
        static string Map => MacroMapDatabase.MapOf("abandoned_city");
        public static bool On => (ZoneOverride ?? TerrainMap.ZoneId) == Map;

        public static Mutant Current;                        // the bell while the fight is on
        static Mutant _bell; static bool _fighting; static float _next = -1f, _tollAt = -1f; static int _phase, _wave;
        static readonly List<Mutant> Husks = new List<Mutant>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { Current = null; _bell = null; _fighting = false; _next = _tollAt = -1f; _phase = 0; _wave = 0; Husks.Clear(); }

        public static bool Owns(Mutant m) => m != null && m == _bell;
        public static int Phase => _phase;
        public static float Fraction => Current == null ? 0f : Mathf.Clamp01(Current.Hp / (float)Mathf.Max(1, Current.MaxHp));
        public static float TollRange => 16f + 3f * _phase;
        public static int TollDamage => 45 + 15 * _phase;
        public static string Label => Current == null ? "" : _tollAt >= 0f ? "THE BELL SWINGS - BREAK THE LINE OF SIGHT OR DIVE" : _wave > 0 ? $"THE CHOIR ANSWERS - WAVE {_wave}" : "IT HAS NOT RUNG YET";
        public static float TollWarn => Current != null && _tollAt >= 0f ? Mathf.Clamp01(1f - (_tollAt - Time.time) / WarnSeconds) : -1f;

        static LandmarkInstance Tow => (TerrainMap.Landmarks ?? new List<LandmarkInstance>()).FirstOrDefault(l => l.Name == Tower);
        /// <summary>The bell's tile: just north of the tower landmark (the belfry frame, Tier6 map).</summary>
        public static (int x, int y) BellSpot() { var l = Tow; return l == null ? (0, 0) : TerrainMap.FindNearestEnterable(l.X, l.Y - 4); }

        /// <summary>Every frame (Rt.cs): puts the bell in its frame the first time you come near, starts the fight, runs the tolls and the phases.</summary>
        public static void Tick(float dt)
        {
            if (!On || Net.Mode == Net.Role.Client || Story.Has(Flag)) return;
            var p = Player.Current; if (p == null) return;
            if (_bell != null && !EncounterState.Enemies.Contains(_bell) && _bell.IsAlive()) Reset();     // the world was reloaded under us
            var l = Tow; if (l == null) return;
            if (_bell == null)
            {
                if (Player.All.Min(q => CombatantUtil.Distance(q, l.X, l.Y)) > WakeRange) return;
                var (bx, by) = BellSpot();
                _bell = new Mutant(Name, bx, by, Def) { IsAggroed = false }; EncounterState.Enemies.Add(_bell); Current = _bell;
            }
            var e = _bell;
            if (!e.IsAlive()) { OnDeath(e); return; }
            RtAi.Pin(e, 0.4f); RtAi.Hold(e, 0.4f);
            float now = Time.time;
            if (!_fighting)
            {
                if (Player.All.Any(q => q.IsAlive() && CombatantUtil.Distance(e, q) <= WakeRange * 0.7f)) Begin(e, now);
                return;
            }
            float hpf = Fraction;
            if (_phase < 1 && hpf <= 0.66f) { _phase = 1; GameLog.Event("The bell cracks along its lip, and the note that comes out is lower. Something in the streets answers louder."); }
            else if (_phase < 2 && hpf <= 0.33f) { _phase = 2; GameLog.Event("The bell is splitting. It will not stop ringing."); }
            if (_tollAt < 0f && now >= _next - WarnSeconds)
            {
                _tollAt = now + WarnSeconds; Sfx.Play("gaunt_scream", TacticalGridView.GridToWorld(e.X, e.Y), 0.6f, 0.03f, 0.7f);
                GameLog.Event("The bell swings back on its yoke.");
            }
            if (_tollAt >= 0f && now >= _tollAt) { _tollAt = -1f; Toll(e); _next = now + BeatSeconds; }
        }

        static void Begin(Mutant e, float now)
        {
            _fighting = true; e.IsAggroed = true; _next = now + 6f; _tollAt = -1f; _phase = 0; _wave = 0;
            GameLog.Event("Somewhere above the square a bell takes its first swing. Every window in the street turns toward you.");
        }

        /// <summary>The toll: everyone within range with a clear line to the bell takes damage and is knocked down (a dive clears it); then a wave of Husks comes out of the streets.</summary>
        static void Toll(Mutant e)
        {
            CombatFx.PxAt("thunderclap", e.X, e.Y); Sfx.Play("gaunt_scream", TacticalGridView.GridToWorld(e.X, e.Y), 1f, 0.03f, 0.6f);
            foreach (var p in Player.All.Where(p => p.IsAlive()))
            {
                if (CombatantUtil.Distance(e, p) > TollRange || Rt.Untouchable(p)) continue;
                if (GameServices.Grid != null && !GameServices.Grid.CheckLineOfSight(e.X, e.Y, p.X, p.Y)) continue;     // behind a pillar or a wall: the note does not reach you
                p.TakeDamage(TollDamage); Rt.Knockdown(p, 0.5f); GameLog.Write($"The toll rings through you for {TollDamage} damage.");
            }
            SendWave(e);
        }

        public static int WaveSize => 3 + 2 * _phase + Mathf.Max(0, Player.All.Count - 1) * 2;

        static void SendWave(Mutant e)
        {
            Husks.RemoveAll(h => h == null || !h.IsAlive());
            int room = MaxHusks - Husks.Count; if (room <= 0) return;
            int n = Mathf.Min(room, WaveSize); _wave++;
            var spots = new[] { (e.X - 26, e.Y + 4), (e.X + 26, e.Y + 4), (e.X, e.Y + 26), (e.X - 4, e.Y - 20) };
            for (int i = 0; i < n; i++)
            {
                var (sx, sy) = spots[(i + _wave) % spots.Length]; var (fx, fy) = TerrainMap.FindNearestEnterable(sx + (i % 3) - 1, sy + i / 3);
                var h = Husk.Make(5, i == 0); h.X = fx; h.Y = fy; h.IsAggroed = true; EncounterState.Enemies.Add(h); Husks.Add(h);
            }
            GameLog.Event(_wave == 1 ? "They come out of the streets in a line, dragging their feet, heads tipped toward the bell." : "More of them. Every one of them has its mouth open.");
        }

        /// <summary>The bell comes down: the toll stops for good, the Husks it was calling stand still and go quiet, a cache lies under the frame.</summary>
        static void OnDeath(Mutant e)
        {
            Story.Set(Flag); Current = null; _fighting = false; _tollAt = -1f;
            int n = 0; foreach (var h in Husks.Where(h => h != null && h.IsAlive())) { RtAi.Hold(h, 600f); RtAi.Pin(h, 600f); h.IsAggroed = false; n++; }
            Husks.Clear();
            GameLog.Event(n > 0 ? "The bell hits the flagstones and rings once, flat and dead. Every Husk in the square stops where it stands, mouth still open, and does not move again."
                                : "The bell hits the flagstones and rings once, flat and dead. The square goes quiet.");
            foreach (var p in Player.All) p.Ru += 800;
            var (cx, cy) = TerrainMap.FindNearestEnterable(e.X, e.Y + 1); Stashes.Hoard(cx, cy, "Belfry Cache", 5, "What the bell-ringers kept under the frame: wages, a collection plate and a good rifle.");
            Story.Journal("The Clock Tower: a great bell was ringing the Husks out of the streets on the eleven-second beat. It is down. They stand where they are.");
            _bell = null;
        }

        /// <summary>Testing Ground: stand at the tower with the bell awake (optionally damaged).</summary>
        public static void StartTest(float hpFraction = 1f)
        {
            Reset(); Story.Clear(Flag); EncounterState.Enemies.Clear(); EncounterState.InCombat = false;
            var z = Map; MacroState.Current.CurrentZoneId = "abandoned_city"; ZoneTravel.EnterZone("abandoned_city");
            var l = Tow; if (l == null) return;
            int k = 0; foreach (var p in Player.All) { var (x, y) = TerrainMap.FindNearestEnterable(l.X - 14 + k % 3, l.Y + 8 + k / 3); p.X = x; p.Y = y; Rt.SetOffset(p, 0f, 0f); k++; }
            Tick(0.1f); if (_bell != null) { _bell.Hp = Mathf.Max(2, (int)(_bell.MaxHp * hpFraction)); Begin(_bell, Time.time); }
            GameLog.Write("[TESTING GROUND] The Great Bell (the Choir): the Clock Tower in the Abandoned City" + (hpFraction < 1f ? $", from {(int)(hpFraction * 100)}% health" : "") + ". Main menu > Testing Ground to come back.");
        }
    }
}
