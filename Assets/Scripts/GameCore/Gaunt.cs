// The Gaunt (2026-11-16, Yan: "a kind of wendigo that controls other mutants, that'll come up in their fights as well"; the horror redesign of the Alpha Prowler, art: ArtSource/make_gaunt.py).
// It does not just fight: its CALL (a scream, then one arm flung out at a player) drives every other mutant within 16 tiles for 9 seconds - they turn aggressive, hunt the player it pointed at
// and run 35% faster - and it calls again every 14 s while it can see its target. Kill it and the driven ones falter: the drive ends and they stop for a moment (RtAi.Hold). The Tier 2 "Taken" horde
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
        public static readonly GameData.MutantDef ElderDef = new GameData.MutantDef { Name = ElderName, Tier = 4, Hp = 3200, PhysDR = 0.1f, AnomDR = 0.35f, Ap = 5, Mp = 5, Ac = 14, AttackDamage = 450, AgilityMod = 2, Behavior = "berserk", ThroatLacerationChance = 0.4f, ArmorPierce = 0.25f, PanicThreshold = 0f };
        /// <summary>Forest zones: the wild Gaunt lives there (SquadManager gives a mutant pack a Gaunt now and then); nowhere else it is not rolled.</summary>
        public static readonly string[] Woods = { "crimson_woods", "black_valley", "camp_shelter" };
        public static bool IsElder(Mutant m) => m?.Db?.Name == ElderName;
        public const float WalkFrameSeconds = 0.085f, RunFrameSeconds = 0.07f;           // its strips are tuned to its ground speed (about 1.6 and 2.5 tiles a cycle against 3 tiles a second: close, not exact)
        public const float ScreamFor = 1.5f, PointFor = 1.1f;
        // the wild Gaunt calls a smaller, shorter drive; the Elder drives a whole horde (range tiles, drive seconds, speed factor, seconds between calls)
        static (float range, float drive, float haste, float every) Kit(Mutant m) => IsElder(m) ? (22f, 12f, 1.45f, 10f) : (12f, 7f, 1.3f, 16f);

        class State { public float NextCall = -1f, PoseUntil = -1f, PoseAt = -1f, DiedAt = -1f; public string Pose; public bool WasAlive = true; }
        static readonly Dictionary<Mutant, State> States = new Dictionary<Mutant, State>();
        static readonly Dictionary<Mutant, (float until, Player target, float haste)> Driven = new Dictionary<Mutant, (float, Player, float)>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { States.Clear(); Driven.Clear(); }

        public static bool Is(ICombatant u) => u is Mutant m && !(u is HumanEnemy) && (m.Db?.Name == Name || m.Db?.Name == ElderName);

        /// <summary>The pose it is held in right now ("scream" while it calls, then "point"), or null (the standing sprite).</summary>
        public static string Pose(Mutant m) => States.TryGetValue(m, out var s) && Time.time < s.PoseUntil ? s.Pose : null;
        // the animations (make_gaunt_anims.py): frame durations in ms; the scream holds (frames 3-5 shake) until the pose ends, the point holds its last frame
        static readonly int[] ScreamMs = { 100, 140, 120, 100, 100, 100, 140, 120 }, PointMs = { 90, 90, 110, 220, 220, 220 };
        /// <summary>The picture for the pose it is in right now, or null (the standing sprite / the walk / the attack take over).</summary>
        public static Sprite Frame(Mutant m)
        {
            if (!(Pose(m) is string p) || !States.TryGetValue(m, out var s)) return null;
            var ms = p == "scream" ? ScreamMs : PointMs; var frames = UnitSprites.GauntAnim(m.Db.Name, p, ms.Length); if (frames == null) return null;
            float t = (Time.time - s.PoseAt) * 1000f; int i = 0; float acc = 0f;
            for (; i < ms.Length - 1; i++) { if (t < acc + ms[i]) break; acc += ms[i]; }
            if (p == "scream" && t >= ScreamMs.Sum()) i = 3 + (int)((t - ScreamMs.Sum()) / 90f) % 3;             // the held scream shakes: frames 3, 4, 5
            return frames[i];
        }

        static readonly int[] DeathMs = { 90, 120, 130, 160, 120, 110, 110, 900 };
        /// <summary>Standing still: a breathing loop (8 pictures, 160 ms each); null when it is not alive.</summary>
        public static Sprite Idle(Mutant m)
        {
            if (!Is(m) || !m.IsAlive()) return null;
            var frames = UnitSprites.GauntAnim(m.Db.Name, "idle", 8); if (frames == null) return null;
            return frames[(int)(Time.time / 0.16f + (m.X * 7 + m.Y * 3)) % frames.Length];     // out of step with its neighbours
        }
        /// <summary>Its fall (8 pictures), then the last one: it stays where it dropped.</summary>
        public static Sprite DeathFrame(Mutant m)
        {
            var s = Of(m); if (s.DiedAt < 0f) s.DiedAt = Time.time;
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
        public static void ForcePose(Mutant m, string pose, float seconds) { var s = Of(m); if (s.Pose != pose || Time.time >= s.PoseUntil) s.PoseAt = Time.time; s.Pose = pose; s.PoseUntil = Time.time + seconds; }
        static State Of(Mutant m) { if (!States.TryGetValue(m, out var s)) States[m] = s = new State(); return s; }

        // ---- co-op: the host sends the pose and its age (in 1/20 s); a client's mirror plays it the same way (NetWorld) ----
        public static (byte code, byte t20) NetPose(Mutant e)
        {
            if (!Is(e) || !(Pose(e) is string p) || !States.TryGetValue(e, out var s)) return (0, 0);
            return ((byte)(p == "scream" ? 1 : 2), (byte)Mathf.Clamp(Mathf.RoundToInt((Time.time - s.PoseAt) * 20f), 0, 255));
        }
        public static void Mirror(Mutant m, int code, float age)
        {
            if (!Is(m)) return;
            if (code == 0) { if (States.TryGetValue(m, out var s0)) s0.PoseUntil = -1f; return; }
            var s = Of(m); s.Pose = code == 1 ? "scream" : "point"; s.PoseAt = Time.time - age; s.PoseUntil = Time.time + 0.3f;       // renewed every snapshot while the host still shows it
        }

        /// <summary>The call: a scream, then the arm flung at `target`; every other mutant within range is driven at it.</summary>
        public static void Call(Mutant g, Player target)
        {
            var s = Of(g); float now = Time.time;
            s.NextCall = now + Kit(g).every;
            ForcePose(g, "scream", ScreamFor);
            s.PoseUntil = now + ScreamFor + PointFor;                                      // the scream first, then the point: Tick() switches them
            _pointAt[g] = now + ScreamFor;
            Sfx.Play("gaunt_scream", TacticalGridView.GridToWorld(g.X, g.Y), 1f, 0.03f, 1f);
            int n = 0;
            foreach (var m in EncounterState.Enemies.OfType<Mutant>().Where(m => !(m is HumanEnemy) && m != g && m.IsAlive() && !Is(m) && CombatantUtil.Distance(g, m) <= Kit(g).range))
            { m.IsAggroed = true; Driven[m] = (now + Kit(g).drive, target, Kit(g).haste); n++; }
            if (n > 0) GameLog.Write($"The Gaunt screams and points. {n} {(n == 1 ? "creature turns" : "creatures turn")} on {(target == Player.Current ? "you" : "your friend")}.");
            else GameLog.Write("The Gaunt screams.");
        }
        static readonly Dictionary<Mutant, float> _pointAt = new Dictionary<Mutant, float>();

        /// <summary>Every frame (Rt.WorldTick): a Gaunt in a fight calls when it can; one that just died frees the ones it drove.</summary>
        public static void Tick()
        {
            float now = Time.time;
            foreach (var g in EncounterState.Enemies.OfType<Mutant>().Where(Is).ToList())
            {
                var s = Of(g);
                if (s.Pose == "scream" && _pointAt.TryGetValue(g, out var pa) && now >= pa && now < s.PoseUntil) s.Pose = "point";   // the scream ends, the arm comes up
                if (g.IsAlive() == false)
                {
                    if (s.WasAlive) { s.WasAlive = false; s.DiedAt = now; Falter(); }
                    continue;
                }
                if (!g.IsAggroed || now < s.NextCall || Net.Mode == Net.Role.Client) continue;
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
