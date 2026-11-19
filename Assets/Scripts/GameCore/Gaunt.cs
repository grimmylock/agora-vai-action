// The Gaunt (2026-11-16, Yan: "a kind of wendigo that controls other mutants, that'll come up in their fights as well"; the horror redesign of the Alpha Prowler, art: ArtSource/make_gaunt.py).
// It does not just fight: its CALL is a recurring attack, ONE animation (Yan, 2026-11-16: the scream includes the point): it plants itself and screams for 1.5 s (killing it then cancels the call), then its arm snaps out
// at a player and the call lands - every other mutant in range turns aggressive, hunts that player and runs faster for a while (the Elder also summons two wild Gaunts) - and it calls again every 10-16 s while it sees its target. Kill it and the driven ones falter: the drive ends and they stop for a moment (RtAi.Hold). The Tier 2 "Taken" horde
// scene scripts the first Call (Tier2Story). Host-side like every enemy; clients do not see the poses yet.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameCore
{
    public static class Gaunt
    {
        public const string Name = "Gaunt", ElderName = "Elder Gaunt";
        /// <summary>The Elder Gaunt (Yan, 2026-11-16: the big mean one is a mini-boss, the smaller crazier one is the wild kind of the forests): not in any random pool, placed by hand (the Tier 2 horde, later a forest lair).</summary>
        public static readonly GameData.MutantDef ElderDef = new GameData.MutantDef { Name = ElderName, Tier = 4, Hp = 15000, PhysDR = 0.1f, AnomDR = 0.35f, Ap = 5, Mp = 5, Ac = 14, AttackDamage = 450, AgilityMod = 2, Behavior = "berserk", ThroatLacerationChance = 0.4f, ArmorPierce = 0.25f, PanicThreshold = 0f };
        /// <summary>Forest zones: the wild Gaunt lives there (SquadManager gives a mutant pack a Gaunt now and then); nowhere else it is not rolled.</summary>
        public static readonly string[] Woods = { "crimson_woods", "black_valley", "camp_shelter" };
        public static bool IsElder(Mutant m) => m?.Db?.Name == ElderName;
        public const float WalkFrameSeconds = 0.085f, RunFrameSeconds = 0.07f, ElderWalkFrameSeconds = 0.11f, ElderRunFrameSeconds = 0.085f;           // its strips are tuned to its ground speed (about 1.6 and 2.5 tiles a cycle against 3 tiles a second: close, not exact)
        public const float ArtScale = 32f / 43f;                    // Yan, 2026-11-17: "reduce the gaunt size by 25% ... they barely fit the screen": both Gaunts are drawn at this fraction of their art (HitProfiles are cut at 43 px a tile to match)
        public const float ScreamFor = 1.2f, CallTotal = 2.6f;      // the arm snaps out and the call lands at ScreamFor; the whole animation lasts CallTotal
        // the wild Gaunt calls a smaller, shorter drive; the Elder drives a whole horde (range tiles, drive seconds, speed factor, seconds between calls)
        static (float range, float drive, float haste, float every) Kit(Mutant m) => IsElder(m) ? (22f, 12f, 1.45f, 10f) : (12f, 7f, 1.3f, 16f);

        class State { public float NextCall = -1f, PoseUntil = -1f, PoseAt = -1f, DiedAt = -1f, EffectAt = -1f; public Player Target; public string Pose; public bool WasAlive = true; }
        static readonly Dictionary<Mutant, State> States = new Dictionary<Mutant, State>();
        static readonly Dictionary<Mutant, (float until, Player target, float haste)> Driven = new Dictionary<Mutant, (float, Player, float)>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { States.Clear(); Driven.Clear(); Summoned.Clear(); }

        public static bool Is(ICombatant u) => u is Mutant m && !(u is HumanEnemy) && (m.Db?.Name == Name || m.Db?.Name == ElderName);

        /// <summary>The pose it is held in right now ("scream" while it calls, then "point"), or null (the standing sprite).</summary>
        public static string Pose(Mutant m) => States.TryGetValue(m, out var s) && Time.time < s.PoseUntil ? s.Pose : null;
        // the call animation (SpriteCook, one strip, scream + point; 8 pictures, 2.6 s): frame durations in ms - breath, head back, the scream held, the arm snaps out (the call lands on frame 5), held, relax
        static readonly int[] ScreamMs = { 200, 260, 300, 380, 380, 420, 380, 280 };
        /// <summary>True while it is mid-call: it stands and screams, it does not walk or strike (RtAi.TickMutant).</summary>
        public static bool Busy(Mutant m) => Is(m) && m.IsAlive() && (IsElder(m) ? Pose(m) != null : Pose(m) == "scream");   // the Elder stands for every pose it plays (scream, roar, rise, slam, shriek)
        /// <summary>The picture for the pose it is in right now, or null (the standing sprite / the walk / the attack take over).</summary>
        public static Sprite Frame(Mutant m)
        {
            if (!(Pose(m) is string p) || !States.TryGetValue(m, out var s)) return null;
            if (IsElder(m)) return ElderClips.Sprite(p, (Time.time - s.PoseAt) * 1000f);                     // the Elder's own clips (ElderClips): scream, roar, rise, slam, shriek
            var frames = UnitSprites.GauntAnim(m.Db.Name, "scream", ScreamMs.Length); if (frames == null) return null;
            float t = (Time.time - s.PoseAt) * 1000f; int i = 0; float acc = 0f;
            for (; i < ScreamMs.Length - 1; i++) { if (t < acc + ScreamMs[i]) break; acc += ScreamMs[i]; }
            return frames[i];
        }

        static readonly int[] DeathMs = { 90, 120, 130, 160, 120, 110, 110, 900 };
        /// <summary>Standing still: a breathing loop (8 pictures, 160 ms each); null when it is not alive.</summary>
        public static Sprite Idle(Mutant m)
        {
            if (!Is(m) || !m.IsAlive()) return null;
            if (IsElder(m)) return ElderClips.Sprite("idle", Time.time * 1000f + (m.X * 7 + m.Y * 3) * 170f);   // out of step with its neighbours
            var frames = UnitSprites.GauntAnim(m.Db.Name, "idle", 8); if (frames == null) return null;
            return frames[(int)(Time.time / 0.16f + (m.X * 7 + m.Y * 3)) % frames.Length];     // out of step with its neighbours
        }
        /// <summary>Its fall without a strip (Yan, 2026-11-16: death is done in code to save credits): the standing picture tips over backwards about its feet, 0 to 84 degrees in 0.8 s (TacticalGridView turns the marker).</summary>
        public static float DeathTilt(Mutant m)
        {
            if (!Is(m)) return 0f; if (m.IsAlive()) return 0f;
            if (IsElder(m) && ElderClips.Has("death")) return 0f;                                           // it has its own fall (kneel, slump)
            var s = Of(m); if (s.DiedAt < 0f) s.DiedAt = Time.time;
            float u = Mathf.Clamp01((Time.time - s.DiedAt) / 0.8f); return 84f * u * u * (3f - 2f * u);
        }
        /// <summary>Its fall (8 pictures), then the last one: it stays where it dropped.</summary>
        public static Sprite DeathFrame(Mutant m)
        {
            var s = Of(m); if (s.DiedAt < 0f) s.DiedAt = Time.time;
            if (IsElder(m) && ElderClips.Has("death")) return ElderClips.Sprite("death", (Time.time - s.DiedAt) * 1000f);
            var frames = UnitSprites.GauntAnim(m.Db.Name, "death", 8); if (frames == null) return null;
            float t = (Time.time - s.DiedAt) * 1000f; int i = 0; float acc = 0f;
            for (; i < frames.Length - 1; i++) { if (t < acc + DeathMs[i]) break; acc += DeathMs[i]; }
            return frames[i];
        }

        /// <summary>A mutant it is driving runs faster; 1 otherwise (RtAi.Speed).</summary>
        public static float Hurry(Mutant m) => Driven.TryGetValue(m, out var d) && Time.time < d.until && m.IsAlive() ? d.haste : 1f;
        /// <summary>The player a driven mutant hunts: the one the Gaunt pointed at (Rt.TargetOf), or null.</summary>
        public static Player Marked(Mutant m) => Driven.TryGetValue(m, out var d) && Time.time < d.until && d.target != null && d.target.IsAlive() ? d.target : null;
        public static bool IsDriven(Mutant m) => Driven.TryGetValue(m, out var d) && Time.time < d.until;

        /// <summary>Hold a pose for `seconds` (the cutscene's scripted scream and point).</summary>
        /// <summary>A mutant is driven (hurried, aimed at `target`) for `seconds` - the boss module hurries its waves and itself.</summary>
        public static void Haste(Mutant m, float seconds, float factor, Player target) => Driven[m] = (Time.time + seconds, target, factor);

        public static void ForcePose(Mutant m, string pose, float seconds) { var s = Of(m); if (s.Pose != pose || Time.time >= s.PoseUntil) s.PoseAt = Time.time; s.Pose = pose; s.PoseUntil = Time.time + seconds; }
        static State Of(Mutant m) { if (!States.TryGetValue(m, out var s)) States[m] = s = new State(); return s; }

        // ---- co-op: the host sends the pose and its age (in 1/20 s); a client's mirror plays it the same way (NetWorld) ----
        public static (byte code, byte t20) NetPose(Mutant e)
        {
            if (!Is(e) || !(Pose(e) is string p) || !States.TryGetValue(e, out var s)) return (0, 0);
            return ((byte)1, (byte)Mathf.Clamp(Mathf.RoundToInt((Time.time - s.PoseAt) * 20f), 0, 255));
        }
        public static void Mirror(Mutant m, int code, float age)
        {
            if (!Is(m)) return;
            if (code == 0) { if (States.TryGetValue(m, out var s0)) s0.PoseUntil = -1f; return; }
            var s = Of(m); s.Pose = "scream"; s.PoseAt = Time.time - age; s.PoseUntil = Time.time + 0.3f;       // renewed every snapshot while the host still shows it
        }

        /// <summary>Begins the call: it plants itself and screams; the call lands CallEffectAt later (Tick), unless it dies first.</summary>
        public static void Call(Mutant g, Player target)
        {
            var s = Of(g); float now = Time.time;
            s.NextCall = now + System.Math.Max(Kit(g).every, CallTotal + 2f);
            ForcePose(g, "scream", CallTotal);
            s.EffectAt = now + ScreamFor; s.Target = target;
            Sfx.Play("gaunt_scream", TacticalGridView.GridToWorld(g.X, g.Y), 1f, 0.03f, 1f);
        }

        /// <summary>The arm snaps out: every other mutant in range is driven at the target (and the Elder summons two wild Gaunts).</summary>
        static void Land(Mutant g, Player target)
        {
            if (target == null || !target.IsAlive()) target = Rt.TargetOf(g, Player.Current);
            float now = Time.time;
            CombatFx.PxAt("thunderclap", g.X, g.Y);                                                // the call lands as a ring of force
            if (IsElder(g)) Summon(g, target);
            int n = 0;
            foreach (var m in EncounterState.Enemies.OfType<Mutant>().Where(m => !(m is HumanEnemy) && m != g && m.IsAlive() && !Is(m) && CombatantUtil.Distance(g, m) <= Kit(g).range && (IsElder(g) || Ballistics.MutantSize(m) < 2f)))   // Yan: the wild one only calls the smaller mutants; the Elder drives everything
            { m.IsAggroed = true; Driven[m] = (now + Kit(g).drive, target, Kit(g).haste); n++; }
            if (n > 0) GameLog.Write($"The Gaunt screams and points. {n} {(n == 1 ? "creature turns" : "creatures turn")} on {(target == Player.Current ? "you" : "your friend")}.");
            else GameLog.Write("The Gaunt screams.");
        }
        static readonly List<Mutant> Summoned = new List<Mutant>();
        public const int SummonPer = 2, SummonCap = 4;
        /// <summary>The Elder's call summons two wild Gaunts out of the dark round it (never more than four of its own alive at once); they arrive aggressive and driven at the player it pointed at.</summary>
        static void Summon(Mutant elder, Player target)
        {
            Summoned.RemoveAll(m => m == null || !m.IsAlive());
            int want = System.Math.Min(SummonPer, SummonCap - Summoned.Count); if (want <= 0) return;
            var def = GameData.MutantDatabase.Get("tier3", Name); int made = 0;
            for (int r = 3; r <= 9 && made < want; r++)
                for (int k = 0; k < 24 && made < want; k++)
                {
                    float a = (k * 2.399f + r) % 6.283f; int x = elder.X + Mathf.RoundToInt(Mathf.Cos(a) * r), y = elder.Y + Mathf.RoundToInt(Mathf.Sin(a) * r);
                    if (x < 1 || y < 1 || x >= TerrainMap.Width - 1 || y >= TerrainMap.Height - 1 || !TerrainMap.CanEnterTile(x, y) || TerrainMap.UnitAt(x, y, null) != null || CombatantUtil.Distance(target, new Mutant("probe", x, y, def)) < 3) continue;
                    var w = new Mutant(Name, x, y, def) { IsAggroed = true }; EncounterState.Enemies.Add(w); Summoned.Add(w); Driven[w] = (Time.time + Kit(elder).drive, target, Kit(elder).haste); made++;
                }
            if (made > 0) GameLog.Write(made == 1 ? "A wild Gaunt crawls out of the dark at its call." : $"{made} wild Gaunts crawl out of the dark at its call.");
        }

        /// <summary>Every frame (Rt.WorldTick): a Gaunt in a fight calls when it can; one that just died frees the ones it drove.</summary>
        public static void Tick()
        {
            float now = Time.time;
            foreach (var g in EncounterState.Enemies.OfType<Mutant>().Where(Is).ToList())
            {
                var s = Of(g);
                if (s.EffectAt >= 0f && g.IsAlive() && now >= s.EffectAt) { s.EffectAt = -1f; Land(g, s.Target); }          // the call lands
                if (!g.IsAlive()) s.EffectAt = -1f;                                                                      // killed mid-scream: nothing happens
                if (g.IsAlive() == false)
                {
                    if (s.WasAlive) { s.WasAlive = false; s.DiedAt = now; Falter(); }
                    continue;
                }
                if (!g.IsAggroed || now < s.NextCall || Net.Mode == Net.Role.Client || GauntBoss.Owns(g)) continue;   // (the boss fight scripts the Elder's moves)
                var p = Rt.TargetOf(g, Player.Current);
                if (p == null || !p.IsAlive() || CombatantUtil.Distance(g, p) > Kit(g).range) continue;
                var grid = GameServices.Grid;
                if (grid != null && !grid.CheckLineOfSight(g.X, g.Y, p.X, p.Y)) continue;
                Call(g, p);
            }
        }

        static void Falter()
        {
            int n = 0;
            foreach (var kv in Driven.ToList()) if (kv.Key.IsAlive() && Time.time < kv.Value.until) { RtAi.Hold(kv.Key, 1.5f); n++; }
            Driven.Clear();
            if (n > 0) GameLog.Write("The Gaunt falls. Everything it was driving stops, and shudders.");
        }
    }
}
