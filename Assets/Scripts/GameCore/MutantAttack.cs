// 2026-11-12 (Yan, ACTION_DESIGN.md "Canine mutants' attacks"): the canine mutants play an attack animation and the damage lands at its CONTACT frame, not at once.
//  - Bite (adjacent): bite A about 60 %, bite B about 40 %, the cooldown is the old one (RtAi: 1.0 s, 1.6 s for the big ones) counted from the START of the animation, so the attack rate is unchanged.
//  - Pounce (2..N tiles, clear line, not hobbled): coil, launch (the tile jumps to the landing tile next to the target at the launch frame and Rt's Slide carries the body across by the strip's travel
//    curve), the bite lands at the landing frame. Dread: up to 6 tiles, KNOCKS DOWN, 10 s. Blind Maw: up to 4, applies BLEEDING, 5 s. Veil: up to 4, 6 s.
//  - Spit (Pallid, 3..6 tiles, clear line): the head animation, the glob leaves the muzzle on the spawn frame and flies at SpitSpeed tiles a second; the damage (60 % of a bite, toxic) lands on arrival.
// Only mutants that have a strip here use this (MutantAttackData); everyone else keeps RtAi's instant strike. Host-side only: nothing is networked yet (co-op mirrors do not see these animations).
using System.Collections.Generic;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class MutantAttack
    {
        public const float BiteBShare = 0.4f, SpitSpeed = 10f, KnockdownSeconds = 0.9f, SpitDamageShare = 0.6f;

        public class Rules { public int PounceMin = 2, PounceMax; public float PounceCd; public string PounceEffect; public int SpitMin = 3, SpitMax; public float SpitCd, SpitShare = SpitDamageShare; public bool PounceFromCloak; }
        public static readonly Dictionary<string, Rules> RuleSet = new Dictionary<string, Rules>
        {
            ["Veil Prowler"] = new Rules { PounceMax = 4, PounceCd = 6f, PounceFromCloak = true },
            ["Pallid Prowler"] = new Rules { SpitMax = 6, SpitCd = 3.5f },
            ["Dread Prowler"] = new Rules { PounceMax = 6, PounceCd = 10f, PounceEffect = "knockdown" },
            ["Blind Maw"] = new Rules { PounceMax = 4, PounceCd = 5f, PounceEffect = "bleed" },
        };

        class State
        {
            public string Kind; public float Start; public bool Hit, Launched;
            public int FromX, FromY, ToX, ToY; public float NextPounce, NextSpit;
            public float SpitArrive = -1f; public int SpitTx, SpitTy; public bool SpitDone;
        }
        static readonly Dictionary<Mutant, State> States = new Dictionary<Mutant, State>();
        static State Of(Mutant m) { if (!States.TryGetValue(m, out var s)) States[m] = s = new State(); return s; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { States.Clear(); }

        public static bool Has(Mutant m) => m != null && !(m is HumanEnemy) && m.Db != null && MutantAttackData.Table.ContainsKey(m.Db.Name);
        public static AttackAnim Anim(string mutant, string kind) => mutant != null && MutantAttackData.Table.TryGetValue(mutant, out var t) && t.TryGetValue(kind, out var a) ? a : null;
        public static bool Playing(Mutant m) => m != null && States.TryGetValue(m, out var s) && s.Kind != null;
        public static string KindOf(Mutant m) => m != null && States.TryGetValue(m, out var s) ? s.Kind : null;

        // ---- pure helpers (tested in the harness) ----
        /// <summary>Which frame of an attack is up `tMs` milliseconds after it started (the last frame holds).</summary>
        public static int FrameIndexAt(int[] ms, float tMs)
        {
            float acc = 0f;
            for (int i = 0; i < ms.Length; i++) { acc += ms[i]; if (tMs < acc) return i; }
            return ms.Length - 1;
        }
        /// <summary>How far across a leap the body is (0..1): the strip's per-frame travel, blended linearly inside each frame.</summary>
        public static float TravelAt(int[] ms, float[] travel, float tMs)
        {
            float acc = 0f;
            for (int i = 0; i < ms.Length; i++)
            {
                if (tMs < acc + ms[i])
                {
                    float f = ms[i] > 0 ? (tMs - acc) / ms[i] : 0f; float a = travel[i], b = i + 1 < travel.Length ? travel[i + 1] : travel[i];
                    return a + (b - a) * f;
                }
                acc += ms[i];
            }
            return travel[travel.Length - 1];
        }
        /// <summary>The ms at which the first frame with some travel starts (the launch).</summary>
        public static float LaunchMs(int[] ms, float[] travel)
        {
            float acc = 0f;
            for (int i = 0; i < ms.Length; i++) { if (travel[i] > 0f) return acc; acc += ms[i]; }
            return acc;
        }
        /// <summary>Walk-time cooldown of a bite, as RtAi had it: 1.0 s, 1.6 s for the big ones, a little random.</summary>
        public static float BiteCooldown(Mutant m, float rand01) => (Ballistics.MutantSize(m) >= 2f ? 1.6f : 1.0f) * (0.85f + 0.35f * rand01);

        // ---- the decision and the timeline: called every frame from RtAi.TickMutant; true = an attack is playing (no walking, no instant strike) ----
        public static bool Update(Mutant m, Player p, float now, ref float nextShot)
        {
            if (!Rt.On || !Has(m)) return false;
            var st = Of(m);
            if (!m.IsAlive() || m.StatusEffects.Prone) { if (st.Kind != null) Cancel(m, st); return false; }
            if (st.Kind != null) { Advance(m, p, st, now); return st.Kind != null; }
            if (p == null || !p.IsAlive() || Body.Down(p)) return false;
            var rules = RuleSet[m.Db.Name]; int d = CombatantUtil.Distance(m, p);
            if (d <= 1)
            {
                if (now < nextShot) return false;
                nextShot = now + BiteCooldown(m, Random.value);
                Begin(m, st, Random.value < BiteBShare ? "bite2" : "bite", now); return true;
            }
            var grid = GameServices.RequireGrid(); bool los = grid != null && grid.CheckLineOfSight(m.X, m.Y, p.X, p.Y);
            if (rules.PounceMax > 0 && m.StatusEffects.Wound != "Leg" && d >= rules.PounceMin && d <= rules.PounceMax && los && now >= st.NextPounce && FindLanding(m, p, out int lx, out int ly))
            {
                st.NextPounce = now + rules.PounceCd; st.FromX = m.X; st.FromY = m.Y; st.ToX = lx; st.ToY = ly; nextShot = System.Math.Max(nextShot, now + 1f);
                Begin(m, st, "pounce", now); return true;
            }
            if (rules.SpitMax > 0 && d >= rules.SpitMin && d <= rules.SpitMax && los && now >= st.NextSpit && Random.value < 0.6f)
            {
                st.NextSpit = now + rules.SpitCd; st.SpitTx = p.X; st.SpitTy = p.Y; st.SpitArrive = -1f; st.SpitDone = false;
                Begin(m, st, "spit", now); return true;
            }
            return false;
        }

        static void Begin(Mutant m, State st, string kind, float now) { st.Kind = kind; st.Start = now; st.Hit = false; st.Launched = false; }

        static void Cancel(Mutant m, State st) { if (st.Kind == "pounce" && st.Launched) Rt.ReleaseSlide(m); st.Kind = null; }

        static void Advance(Mutant m, Player p, State st, float now)
        {
            var a = Anim(m.Db.Name, st.Kind); if (a == null) { st.Kind = null; return; }
            float tMs = (now - st.Start) * 1000f;
            if (st.Kind == "pounce")
            {
                if (!st.Launched && tMs >= LaunchMs(a.Ms, a.Travel))
                {
                    st.Launched = true; Rt.HoldSlide(m); m.X = st.ToX; m.Y = st.ToY;
                }
                if (st.Launched) { float k = 1f - TravelAt(a.Ms, a.Travel, tMs); Rt.SetSlide(m, (st.FromX - st.ToX) * k, (st.FromY - st.ToY) * k); }
            }
            if (!st.Hit && tMs >= a.ContactMs) { st.Hit = true; Contact(m, p, st, now); }
            if (st.Kind == "spit" && st.SpitArrive > 0f && !st.SpitDone && now >= st.SpitArrive) { st.SpitDone = true; SpitLands(m, p, st); }
            if (tMs >= a.TotalMs && (st.Kind != "spit" || st.SpitDone || st.SpitArrive < 0f))
            {
                if (st.Kind == "pounce" && st.Launched) Rt.ReleaseSlide(m);
                st.Kind = null;
            }
        }

        static void Contact(Mutant m, Player p, State st, float now)
        {
            if (p == null || !p.IsAlive()) return;
            if (st.Kind == "spit")
            {
                float dist = Mathf.Sqrt((st.SpitTx - m.X) * (st.SpitTx - m.X) + (st.SpitTy - m.Y) * (st.SpitTy - m.Y));
                st.SpitArrive = now + dist / SpitSpeed; return;                      // the glob is drawn by the view from GlobAt()
            }
            if (!CombatantUtil.IsAdjacent(m, p) || Body.Down(p)) return;              // the target stepped away: the bite finds nothing
            bool hit = Strike(m, p, 1f, false);
            if (hit && st.Kind == "pounce") Effect(m, p, RuleSet[m.Db.Name].PounceEffect);
        }

        static void SpitLands(Mutant m, Player p, State st)
        {
            if (p == null || !p.IsAlive() || Body.Down(p)) return;
            if (CombatantUtil.Distance(m, p) < 1) return;
            bool hit = Strike(m, p, RuleSet[m.Db.Name].SpitShare, true);
            if (hit && p.IsAlive()) { p.StatusEffects.ToxicBleed = true; GameLog.Write("The acid burns: you suffer Toxic Bleed."); }
        }

        /// <summary>One resolved strike through Mutant.Attack (its fx event, the hurt/blood effects, the wound system); returns whether it hit.</summary>
        static bool Strike(Mutant m, Player p, float share, bool anyRange)
        {
            m.DamageMult = share; m.IgnoreRange = anyRange; m.LastHit = false;
            m.Attack(p);
            m.DamageMult = 1f; m.IgnoreRange = false;
            return m.LastHit;
        }

        static void Effect(Mutant m, Player p, string effect)
        {
            if (!p.IsAlive()) return;
            if (effect == "knockdown") { Rt.Knockdown(p, KnockdownSeconds); GameLog.Write($"{m.Name} knocks you down!"); }
            else if (effect == "bleed" && !p.StatusEffects.Bleeding) { p.StatusEffects.Bleeding = true; GameLog.Write($"{m.Name}'s jaws tear you open: you are bleeding."); }
        }

        /// <summary>A free tile next to the target, the nearest to the mutant.</summary>
        public static bool FindLanding(Mutant m, ICombatant target, out int lx, out int ly)
        {
            lx = ly = 0; float best = float.MaxValue; bool found = false;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int x = target.X + dx, y = target.Y + dy;
                    if (!TerrainMap.CanEnterTile(x, y) || TerrainMap.UnitAt(x, y, m) != null) continue;
                    float d = (x - m.X) * (x - m.X) + (y - m.Y) * (y - m.Y);
                    if (d < best) { best = d; lx = x; ly = y; found = true; }
                }
            return found;
        }

        /// <summary>The attack frame to draw for this mutant now, or null (walk or stand-still sprite).</summary>
        public static Sprite Frame(Mutant m, string name, float now)
        {
            if (!Rt.On || m == null || !m.IsAlive() || !States.TryGetValue(m, out var st) || st.Kind == null) return null;
            var a = Anim(name, st.Kind); var frames = UnitSprites.HDAttack(name, st.Kind);
            if (a == null || frames == null) return null;
            return frames[Mathf.Clamp(FrameIndexAt(a.Ms, (now - st.Start) * 1000f), 0, frames.Length - 1)];
        }

        /// <summary>Where the spit glob is right now (tile space), or false when none is in the air; the view flies the glob with it.</summary>
        public static bool GlobAt(Mutant m, float now, out float x, out float y, out float angle)
        {
            x = y = angle = 0f;
            if (m == null || !States.TryGetValue(m, out var st) || st.Kind != "spit" || st.SpitArrive < 0f || st.SpitDone) return false;
            var a = Anim(m.Db.Name, "spit"); if (a == null) return false;
            float tStart = st.Start + a.ContactMs / 1000f, k = Mathf.Clamp01((now - tStart) / Mathf.Max(0.01f, st.SpitArrive - tStart));
            float sx = m.X + 0.5f, sy = m.Y + 0.5f, tx = st.SpitTx + 0.5f, ty = st.SpitTy + 0.5f;
            x = sx + (tx - sx) * k; y = sy + (ty - sy) * k; angle = Mathf.Atan2(ty - sy, tx - sx) * Mathf.Rad2Deg; return true;
        }
    }
}
