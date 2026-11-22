// The Elder Gaunt as a boss (Tier 3 beat 4 "The Door Under the Dock", STORY_GAUNT_LAB.md; Yan, 2026-11-19: "three phases is fine: it calls hordes at 75% and 40%, it has a stunning scream, and the arena has cover pillars" - boulders now).
// A module on the ordinary Elder Gaunt (Gaunt.ElderDef, 15,000 HP: the HP is not touched; Mutant AI walks it to you and swings, 450 a blow). While this fight runs the Elder's own recurring call (Gaunt.Tick) is switched off
// and these scripted moves replace it:
//  Phase 1 (100-75 %): the SCREAM every 14 s when it sees you within 13 tiles: a cone in front of it; whoever is in it staggers - dropped to the ground for a second (half that with the Tumbler perk, Quick Hands
//    gets the rifle up again faster) and their aim is blown (spread and bloom at the maximum).
//  At 75 %: it POINTS - the first wave (about 20, 5 more per extra player) comes out of the arena's north and south mouths, running (driven, faster); the Elder is held still behind its horde (damage cannot take it below
//    the HP it had) until the wave is down to a quarter, or 30 s.
//  Phase 2 (75-40 %): faster scream (10 s), and the scream SHATTERS the boulders in its cone (cover to rubble).
//  At 40 %: the second, bigger wave (about 30, 7 more per extra player) and the same hold; the Elder ENRAGES: a quarter faster for good, no more screams or calls; it hunts - and every 9 s, when you are within 4.5 tiles,
//    it raises both fists (a second's warning) and SLAMS: a ring of 3.8 tiles, 200 damage, knocks you down (a dive clears it).
//  Death: the call cuts off - every mutant in the cavern is held for three seconds and then loses its target (it scatters).
// Co-op: the cone and the ring hit every player; waves scale with their number. Host-side like every enemy (clients do not see the poses yet).
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameCore
{
    public static class GauntBoss
    {
        public static Mutant Current;                       // the Elder while the fight is on
        public const float ScreamRange = 13f, ScreamCone = 70f, SlamRadius = 3.8f, SlamNear = 4.5f;
        public const int SlamDamage = 200;
        static float _slamHitAt = -9f, _slamFrom; static int _phase; static bool _shield; static int _shieldHp; static float _shieldUntil, _nextScream, _nextSlam, _screamAt = -1f, _slamAt = -1f, _waveStart; static int _waveSize;
        static readonly List<Mutant> Wave = new List<Mutant>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { Current = null; _phase = 0; _shield = false; _screamAt = _slamAt = -1f; Wave.Clear(); }

        public static bool Owns(Mutant m) => m != null && m == Current;
        public static bool Shielded(ICombatant t) => _shield && t != null && t == Current;
        public static int Phase => _phase;
        public static float Fraction => Current == null ? 0f : Mathf.Clamp01(Current.Hp / (float)Mathf.Max(1, Current.MaxHp));
        public static string Label => Current == null ? "" : _shield ? "THE ELDER CALLS - KILL THE HORDE" : _slamAt >= 0f ? "SLAM!" : _screamAt >= 0f ? "SCREAM - TAKE COVER" : _phase >= 2 ? "ENRAGED" : _phase == 1 ? "PHASE 2" : "";

        /// <summary>Slam telegraph for the view: 0..1 across the one-second warning, or -1 when none (the ring on the ground grows to its full size as the fists come down).</summary>
        public static float SlamWarn => Current != null && _slamAt >= 0f ? Mathf.Clamp01(1f - (_slamAt - Time.time) / 1.0f) : -1f;
        /// <summary>The Elder's lean in degrees for the slam, drawn by code (no strip): rears back during the warning, then snaps forward and settles in a quarter second.</summary>
        public static float SlamLean(Mutant m)
        {
            if (!Owns(m) || ElderClips.Has("slam")) return 0f; float w = SlamWarn; if (w >= 0f) return -16f * w * w;
            float u = (Time.time - _slamHitAt) / 0.3f; return u >= 0f && u < 1f ? 14f * (1f - u) * (1f - u) : 0f;
        }
        public static bool Enraged(Mutant m) => Owns(m) && _phase >= 2;

        /// <summary>Which combo the Elder plays next to you: 2 hits, then 3 below 75 % health, then 4 (claw, claw, lunge, slam) below 40 %.</summary>
        public static string ComboKind(Mutant m) => !Owns(m) ? "combo2" : _phase >= 2 ? "combo4" : _phase == 1 ? "combo3" : "combo2";
        public static void SlamAt(Mutant e) => Slam(e);

        public static void Begin(Mutant elder)
        {
            if (Current == elder) return;
            Current = elder; _slamHitAt = -9f; _phase = 0; _shield = false; _screamAt = _slamAt = -1f; Wave.Clear();
            float now = Time.time; _nextScream = now + ElderClips.Seconds("rise") + 0.4f; _nextSlam = now + 9f; _nextShriek = now + 4f; _shriekAt = -1f;
            if (ElderClips.Get("rise") != null) { Gaunt.ForcePose(elder, "rise", ElderClips.Seconds("rise")); RtAi.Hold(elder, ElderClips.Seconds("rise")); }       // it stands up from its bone pile, then screams
        }

        public static void Tick(float dt)
        {
            var e = Current; if (e == null) return;
            if (!e.IsAlive()) { OnDeath(e); return; }
            if (Net.Mode == Net.Role.Client) return;
            float now = Time.time; float hpf = Fraction; var target = Rt.TargetOf(e, Player.Current);
            if (_phase < 1 && hpf <= 0.75f) { _phase = 1; StartWave(1, target); }
            else if (_phase < 2 && hpf <= 0.40f) { _phase = 2; StartWave(2, target); Gaunt.Haste(e, 99999f, 1.25f, target); if (ElderClips.Get("roar") != null) { Gaunt.ForcePose(e, "roar", ElderClips.Seconds("roar")); _nextShriek = now + ElderClips.Seconds("roar") + 1f; } GameLog.Event("The Elder throws back its head. Something in it breaks. It is faster now - and it is not going to call again: it is going to hunt."); }
            if (_shield)
            {
                int alive = Wave.Count(m => m.IsAlive());
                if (alive <= Mathf.Max(2, _waveSize / 4) || now >= _shieldUntil) { _shield = false; GameLog.Event("The call fades. The Elder turns its head toward you."); }
                else { if (e.Hp < _shieldHp) e.Hp = _shieldHp; RtAi.Pin(e, 0.4f); RtAi.Hold(e, 0.4f); ShriekTick(e, target, now); }   // it stands and breathes behind its horde - and now and then lets out a ranged shriek
            }
            if (_phase < 2 && _screamAt < 0f && !_shield && now >= _nextScream && target != null && target.IsAlive() && CombatantUtil.Distance(e, target) <= ScreamRange
                && (GameServices.Grid == null || GameServices.Grid.CheckLineOfSight(e.X, e.Y, target.X, target.Y)))
            {
                Gaunt.ForcePose(e, "scream", Gaunt.CallTotal); Sfx.Play("gaunt_scream", TacticalGridView.GridToWorld(e.X, e.Y), 1f, 0.03f, 1f);
                _screamAt = now + Gaunt.ElderScreamFor; _nextScream = now + (_phase == 0 ? 14f : 10f); GameLog.Event("The Elder plants itself and draws a breath.");
            }
            if (_screamAt >= 0f && now >= _screamAt) { _screamAt = -1f; Scream(e, target); }
            if (_phase >= 2 && _slamAt < 0f && !_shield && now >= _nextSlam && Player.All.Any(p => p.IsAlive() && CombatantUtil.Distance(e, p) <= SlamNear))
            { _slamAt = now + 1.0f; _nextSlam = now + 9f; RtAi.Hold(e, 1.0f); if (ElderClips.Get("slam") != null) Gaunt.ForcePose(e, "slam", ElderClips.Seconds("slam")); GameLog.Event("The Elder raises both fists over its head."); }
            if (_slamAt >= 0f && now >= _slamAt) { _slamAt = -1f; _slamHitAt = Time.time; Slam(e); }
        }

        // The Shriek (Yan, 2026-11-19: "during the horde summon phase it could just kinda stand there breathing, maybe it does the ranged scream every few seconds while we fight the summoned horde"):
        // while the horde lives, every ~6 s it lunges its neck and a narrow sound wave goes out up to 14 tiles - a short warning pose, then 60 damage and a blown aim to every player in the line (a dive clears it).
        static float _shriekAt = -1f, _nextShriek; public const float ShriekRange = 14f, ShriekCone = 28f; public const int ShriekDamage = 60;
        static void ShriekTick(Mutant e, Player target, float now)
        {
            if (_shriekAt < 0f && now >= _nextShriek && target != null && target.IsAlive() && CombatantUtil.Distance(e, target) <= ShriekRange
                && (GameServices.Grid == null || GameServices.Grid.CheckLineOfSight(e.X, e.Y, target.X, target.Y)))
            { Gaunt.ForcePose(e, ElderClips.Get("shriek") != null ? "shriek" : "scream", ElderClips.Get("shriek") != null ? ElderClips.Seconds("shriek") : 0.9f); _shriekAt = now + 0.55f; _nextShriek = now + 6f; _shriekDx = target.X - e.X; _shriekDy = target.Y - e.Y; }
            if (_shriekAt >= 0f && now >= _shriekAt) { _shriekAt = -1f; Shriek(e); }
        }
        static float _shriekDx, _shriekDy;
        static void Shriek(Mutant e)
        {
            CombatFx.PxAt("thunderclap", e.X, e.Y);
            float l = Mathf.Max(0.01f, Mathf.Sqrt(_shriekDx * _shriekDx + _shriekDy * _shriekDy)), dx = _shriekDx / l, dy = _shriekDy / l;
            foreach (var p in Player.All.Where(p => p.IsAlive()))
            {
                float px = p.X - e.X, py = p.Y - e.Y, d = Mathf.Sqrt(px * px + py * py);
                if (d > ShriekRange || d < 0.01f || (px * dx + py * dy) / d < Mathf.Cos(ShriekCone * 0.5f * Mathf.Deg2Rad) || Rt.Untouchable(p)) continue;
                var aim = Gunplay.AimOf(p); aim.Steady = 0f; aim.Bloom = Gunplay.PlayerMaxBloom; p.TakeDamage(ShriekDamage);
                GameLog.Write($"The shriek rips through the line for {ShriekDamage} damage and throws your aim.");
            }
        }

        static void Scream(Mutant e, Player target)
        {
            CombatFx.PxAt("thunderclap", e.X, e.Y);
            if (target == null) return;
            float dx = target.X - e.X, dy = target.Y - e.Y, l = Mathf.Max(0.01f, Mathf.Sqrt(dx * dx + dy * dy)); dx /= l; dy /= l; int hit = 0;
            foreach (var p in Player.All.Where(p => p.IsAlive()))
            {
                float px = p.X - e.X, py = p.Y - e.Y, d = Mathf.Sqrt(px * px + py * py);
                if (d > ScreamRange || d < 0.01f || (px * dx + py * dy) / d < Mathf.Cos(ScreamCone * 0.5f * Mathf.Deg2Rad) || Rt.Untouchable(p)) continue;
                var aim = Gunplay.AimOf(p); aim.Steady = 0f; aim.Bloom = Gunplay.PlayerMaxBloom;                                   // the aim is blown
                Rt.Knockdown(p, CombatantUtil.HasPerk(p, "Tumbler") ? 0.5f : 1.0f); hit++;                                      // and they go down for a second
                GameLog.Write("The scream hits you like a wall: you stagger and lose your aim.");
            }
            if (_phase >= 1) Shatter(e, dx, dy);
        }

        /// <summary>The scream shatters boulders in its cone: cover to rubble.</summary>
        static void Shatter(Mutant e, float dx, float dy)
        {
            int n = 0;
            for (int y = e.Y - 11; y <= e.Y + 11; y++) for (int x = e.X - 11; x <= e.X + 11; x++)
            {
                if (x < 1 || y < 1 || x >= TerrainMap.Width - 1 || y >= TerrainMap.Height - 1) continue;
                var t = TerrainMap.GetTile(x, y); if (t.Type != TileType.HardCover || t.Look != "rock") continue;
                float px = x - e.X, py = y - e.Y, d = Mathf.Sqrt(px * px + py * py); if (d < 0.5f || d > 11f || (px * dx + py * dy) / d < Mathf.Cos(35f * Mathf.Deg2Rad)) continue;
                TerrainMap.SetTileType(x, y, TileType.Difficult, "rubble"); n++;
            }
            if (n > 0) GameLog.Event(n == 1 ? "A boulder bursts under the scream." : $"{n} boulders burst under the scream.");
        }

        static void Slam(Mutant e)
        {
            CombatFx.PxAt("thunderclap", e.X, e.Y);
            foreach (var p in Player.All.Where(p => p.IsAlive()))
            {
                if (CombatantUtil.Distance(e, p) > SlamRadius || Rt.Untouchable(p)) continue;
                p.TakeDamage(SlamDamage); Rt.Knockdown(p, 0.6f); GameLog.Write($"The slam catches you for {SlamDamage} damage.");
            }
        }

        static void StartWave(int n, Player target)
        {
            var e = Current; int extra = Mathf.Max(0, Player.All.Count - 1);
            var mouths = new[] { "Arena North Mouth", "Arena South Mouth" }.Select(nm => (TerrainMap.Landmarks ?? new List<LandmarkInstance>()).FirstOrDefault(l => l.Name == nm)).Where(l => l != null).ToList();
            var kinds = n == 1 ? new[] { "PallidProwler", "PallidProwler", "BlindMaw", "Swarm", "tier2:VeilProwler" }.SelectMany(k => Enumerable.Repeat(k, k == "Swarm" ? 6 : k == "tier2:VeilProwler" ? 2 : 4)).ToList()
                                : new[] { "PallidProwler", "BlindMaw", "Swarm", "tier2:VeilProwler", "tier4:DreadProwler" }.SelectMany(k => Enumerable.Repeat(k, k == "Swarm" ? 8 : k == "tier4:DreadProwler" ? 3 : k == "tier2:VeilProwler" ? 4 : 8)).ToList();
            for (int i = 0; i < extra * (n == 1 ? 5 : 7); i++) kinds.Add(i % 2 == 0 ? "PallidProwler" : "BlindMaw");
            Wave.Clear(); int k2 = 0;
            foreach (var spec in kinds)
            {
                var m0 = mouths.Count > 0 ? mouths[k2 % mouths.Count] : null; int bx = m0?.X ?? e.X, by = m0?.Y ?? e.Y;
                var m = Tier3Annex.Make(spec, bx + (k2 % 5) - 2, by + (k2 / 5 % 3) - 1, Gaunt.CalledHpShare); m.IsAggroed = true; EncounterState.Enemies.Add(m); Wave.Add(m);
                if (target != null) Gaunt.Haste(m, 14f, 1.3f, target); k2++;
            }
            _waveSize = Wave.Count; _shield = true; _shieldHp = e.Hp; _shieldUntil = Time.time + 30f; _waveStart = Time.time;
            GameLog.Event(n == 1 ? "The Elder points. Out of the dark mouths of the cavern, north and south, something comes running - and the Elder goes still, breathing, listening."
                                 : "It points again. This time the whole cavern answers, and the Elder stands in the middle of it, breathing.");
            Sfx.Play("gaunt_scream", TacticalGridView.GridToWorld(e.X, e.Y), 0.8f, 0.03f, 1f);
        }

        /// <summary>The call cuts off for good: every mutant in the cavern is held for three seconds and then loses its target.</summary>
        static void OnDeath(Mutant e)
        {
            Current = null; _shield = false; _screamAt = _slamAt = -1f;
            int n = 0;
            foreach (var m in EncounterState.Enemies.OfType<Mutant>().Where(m => !(m is HumanEnemy) && m != e && m.IsAlive())) { RtAi.Hold(m, 3f); RtAi.Pin(m, 3f); m.IsAggroed = false; n++; }
            if (n > 0) GameLog.Event("The scream cuts off mid-breath. Everything it was driving drops, shudders, and scatters into the dark.");
            Wave.Clear();
        }
    }
}
