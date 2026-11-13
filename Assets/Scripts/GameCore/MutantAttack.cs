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
        /// <summary>Profile sprites look wrong running or leaping straight up or down the screen (Yan, 2026-11-12), so a charge only starts within ~31 degrees of horizontal (|dy| <= 0.6 |dx|) and a pounce within 45 degrees; otherwise the mutant first moves to a flank tile on its side of the target (Stance 3).</summary>
        public const float ChargeSideRatio = 0.6f, PounceSideRatio = 1.0f;
        public static bool SideOk(Mutant m, ICombatant t, float ratio) => System.Math.Abs(t.Y - m.Y) <= ratio * System.Math.Abs(t.X - m.X);
        public const float BoulderSpeed = 9f, BoulderShare = 0.4f, BoulderOutSeconds = 5f; public const int BoulderHitRadius = 1;
        public const float BiteBShare = 0.4f, SpitSpeed = 10f, KnockdownSeconds = 0.9f, SpitDamageShare = 0.6f, RecloakSeconds = 3f, RecloakDistance = 3, CloakFadeSeconds = 0.35f;

        public class Rules { public int PounceMin = 2, PounceMax; public float PounceCd; public string PounceEffect; public int SpitMin = 3, SpitMax; public float SpitCd, SpitShare = SpitDamageShare; public bool PounceFromCloak; public int ChargeMin, ChargeMax; public float ChargeLen = 8f, ChargeSpeed = 7f, ChargeCd; public string MeleeA = "bite", MeleeB = "bite2"; public float SlamCd; public int BoulderMin, BoulderMax; }
        public static readonly Dictionary<string, Rules> RuleSet = new Dictionary<string, Rules>
        {
            ["Veil Prowler"] = new Rules { PounceMax = 4, PounceCd = 6f, PounceFromCloak = true },
            ["Pallid Prowler"] = new Rules { SpitMax = 6, SpitCd = 3.5f },
            ["Dread Prowler"] = new Rules { PounceMax = 6, PounceCd = 10f, PounceEffect = "knockdown" },
            ["Blight Brute"] = new Rules { MeleeA = "swipe", MeleeB = null, SlamCd = 10f, BoulderMin = 2, BoulderMax = 10 },       // Yan, 2026-11-12: no jumps (too heavy): a lateral SWIPE, a GROUND SLAM (10 s, knocks down), a BOULDER after 5 s out of reach
            ["Blind Maw"] = new Rules { ChargeMin = 4, ChargeMax = 6, ChargeLen = 8f, ChargeSpeed = 7f, ChargeCd = 8f, PounceEffect = "bleed" },     // the Maw does not leap: it CHARGES in a straight line (Yan, 2026-11-12)
        };

        class State
        {
            public string Kind; public float Start; public bool Hit, Launched;
            public int FromX, FromY, ToX, ToY; public float NextPounce, NextSpit, NextSlam, OutSince = -1f, DustStart = -99f; public bool ThrowFaceLeft;
            public float SpitArrive = -1f; public int SpitTx, SpitTy; public bool SpitDone; public float SplatStart = -99f; public bool Mirror; public float DisengageUntil; public float NextCharge, ChargeRunStart = -1f, ChargeDx, ChargeDy, ChargeX0, ChargeY0, ChargeDist, ChargeLast, ChargeEnd = -1f; public bool Recloak; public float RecloakFrom; public int RecloakHp;
        }
        static readonly Dictionary<Mutant, State> States = new Dictionary<Mutant, State>();
        static State Of(Mutant m) { if (!States.TryGetValue(m, out var s)) States[m] = s = new State(); return s; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { States.Clear(); Fade.Clear(); }

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
            if (st.Recloak)
            {
                if (m.Hp < st.RecloakHp) { st.RecloakHp = m.Hp; st.RecloakFrom = now; }                  // hit while slipping away: the timer starts again
                else if (now - st.RecloakFrom >= RecloakSeconds && CombatantUtil.Distance(m, p) >= RecloakDistance) { m.IsCloaked = true; st.Recloak = false; }
            }
            var rules = RuleSet[m.Db.Name]; int d = CombatantUtil.Distance(m, p);
            if (d <= 1)
            {
                st.OutSince = -1f;
                if (now < nextShot) return false;
                nextShot = now + BiteCooldown(m, Random.value);
                if (rules.SlamCd > 0f && now >= st.NextSlam) { st.NextSlam = now + rules.SlamCd; st.DustStart = -99f; Begin(m, st, "slam", now); return true; }
                Begin(m, st, rules.MeleeB != null && Random.value < BiteBShare ? rules.MeleeB : rules.MeleeA, now); return true;
            }
            var grid = GameServices.RequireGrid(); bool los = grid != null && grid.CheckLineOfSight(m.X, m.Y, p.X, p.Y);
            if (rules.ChargeMax > 0 && m.StatusEffects.Wound != "Leg" && d >= rules.ChargeMin && d <= rules.ChargeMax && los && now >= st.NextCharge && SideOk(m, p, ChargeSideRatio))
            {
                st.NextCharge = now + rules.ChargeCd; nextShot = System.Math.Max(nextShot, now + 1f); Begin(m, st, "charge", now);
                float cx = m.X + 0.5f, cy = m.Y + 0.5f, tx = p.X + 0.5f - cx, ty = p.Y + 0.5f - cy, len = Mathf.Sqrt(tx * tx + ty * ty);
                st.ChargeDx = tx / len; st.ChargeDy = ty / len; st.ChargeX0 = cx; st.ChargeY0 = cy; st.ChargeDist = 0f; st.ChargeRunStart = -1f; st.ChargeLast = now; st.ChargeEnd = -1f; return true;     // the direction is fixed NOW: it does not turn while it runs, so the player can step aside
            }
            if (rules.PounceMax > 0 && m.StatusEffects.Wound != "Leg" && d >= rules.PounceMin && d <= rules.PounceMax && los && now >= st.NextPounce && SideOk(m, p, PounceSideRatio) && FindLanding(m, p, out int lx, out int ly))
            {
                st.NextPounce = now + rules.PounceCd; st.FromX = m.X; st.FromY = m.Y; st.ToX = lx; st.ToY = ly; nextShot = System.Math.Max(nextShot, now + 1f);
                Begin(m, st, "pounce", now); return true;
            }
            if (rules.BoulderMax > 0)                                                  // the Brute: out of reach for 5 s in a row and in sight, it rips up a rock and throws it
            {
                if (st.OutSince < 0f) st.OutSince = now;
                if (d >= rules.BoulderMin && d <= rules.BoulderMax && los && now - st.OutSince >= BoulderOutSeconds)
                {
                    st.OutSince = -1f; st.SpitTx = p.X; st.SpitTy = p.Y; st.SpitArrive = -1f; st.SpitDone = false; st.ThrowFaceLeft = p.X < m.X; nextShot = System.Math.Max(nextShot, now + 1f);
                    Begin(m, st, "throw", now); return true;
                }
            }
            if (rules.SpitMax > 0 && d >= rules.SpitMin && d <= rules.SpitMax && los && now >= st.NextSpit && Random.value < 0.6f)
            {
                st.NextSpit = now + rules.SpitCd; st.SpitTx = p.X; st.SpitTy = p.Y; st.SpitArrive = -1f; st.SpitDone = false;
                Begin(m, st, "spit", now); return true;
            }
            return false;
        }

        static void Begin(Mutant m, State st, string kind, float now)
        {
            st.Kind = kind; st.Start = now; st.Hit = false; st.Launched = false; st.Recloak = false;
            if (m.ActiveCamouflage && m.IsCloaked) m.IsCloaked = false;                      // the Veil shows itself as it strikes (the dissolve plays out in the view)
        }

        static void Cancel(Mutant m, State st) { if (st.Kind == "pounce" && st.Launched) Rt.ReleaseSlide(m); st.Kind = null; }

        static void Advance(Mutant m, Player p, State st, float now)
        {
            if (st.Kind == "charge") { AdvanceCharge(m, p, st, now); return; }
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
            bool ranged = st.Kind == "spit" || st.Kind == "throw";
            if (ranged && st.SpitArrive > 0f && !st.SpitDone && now >= st.SpitArrive) { st.SpitDone = true; st.SplatStart = now; if (st.Kind == "throw") BoulderLands(m, st); else SpitLands(m, p, st); }
            if (tMs >= a.TotalMs && (!ranged || st.SpitDone || st.SpitArrive < 0f))
            {
                if (st.Kind == "pounce" && st.Launched) Rt.ReleaseSlide(m);
                st.Kind = null; AfterAttack(m, p, st, now);
            }
        }

        /// <summary>Yan: a cloaking hunter retreats 3 tiles after its strike and re-cloaks about 3 s later out of sight and without being hit.</summary>
        public const float DisengageSeconds = 1.0f;
        static void AfterAttack(Mutant m, Player p, State st, float now)
        {
            if (!m.ActiveCamouflage) { st.DisengageUntil = now + DisengageSeconds; return; }       // nobody glues itself to the player (Yan): after a strike the others back off for a moment too
            if (p == null) return;
            st.Recloak = true; st.RecloakFrom = now; st.RecloakHp = m.Hp;
            int dx = System.Math.Sign(m.X - p.X), dy = System.Math.Sign(m.Y - p.Y); if (dx == 0 && dy == 0) dx = 1;
            for (int k = 3; k >= 1; k--)
            {
                int x = m.X + dx * k, y = m.Y + dy * k;
                if (TerrainMap.CanEnterTile(x, y) && TerrainMap.UnitAt(x, y, m) == null) { RtAi.GoTo(m, (x, y)); return; }
            }
        }

        // ---- the cloak's fade (drawn by the view): 0 = seen, 1 = cloaked, moving over CloakFadeSeconds; a mutant first seen cloaked starts cloaked ----
        static readonly Dictionary<Mutant, (float fade, float t)> Fade = new Dictionary<Mutant, (float, float)>();
        public static float CloakFade(Mutant m, float now)
        {
            if (m == null || !m.ActiveCamouflage) return 0f;
            float target = m.IsCloaked ? 1f : 0f;
            if (!Fade.TryGetValue(m, out var f)) f = (target, now);
            f.fade = Mathf.MoveTowards(f.fade, target, Mathf.Clamp(now - f.t, 0f, 0.1f) / CloakFadeSeconds); f.t = now; Fade[m] = f; return f.fade;
        }
        /// <summary>Which pixel-dissolve frame (0..5) to draw while the cloak is fading, or -1.</summary>
        public static int DissolveIndex(float fade) => fade <= 0.03f || fade >= 0.97f ? -1 : Mathf.Clamp((int)(fade * 6f), 0, 5);

        public const float ChargeWindupSeconds = 0.3f, ChargeStumbleSeconds = 1.2f, ChargeHitRecoverSeconds = 0.7f, ChargeReach = 0.95f;

        /// <summary>The Maw's charge: a short rear-back, then a straight run along the line fixed at the start for ChargeLen tiles at ChargeSpeed. The bite (and bleeding) lands if the player is in its path;
        /// if the player stepped aside it runs the whole length, stops where it ends and stumbles for a moment (a window to hurt it). It stops short at a wall or another body.</summary>
        static void AdvanceCharge(Mutant m, Player p, State st, float now)
        {
            var r = RuleSet[m.Db.Name];
            if (st.ChargeEnd >= 0f) { if (now >= st.ChargeEnd) { Rt.ReleaseSlide(m); st.Kind = null; } return; }       // stumbling or recovering
            if (now - st.Start < ChargeWindupSeconds) { st.ChargeLast = now; return; }
            if (st.ChargeRunStart < 0f) { st.ChargeRunStart = now; st.ChargeLast = now; Rt.HoldSlide(m); }
            float dt = Mathf.Clamp(now - st.ChargeLast, 0f, 0.1f); st.ChargeLast = now;
            st.ChargeDist = Mathf.Min(r.ChargeLen, st.ChargeDist + r.ChargeSpeed * dt);
            float fx = st.ChargeX0 + st.ChargeDx * st.ChargeDist, fy = st.ChargeY0 + st.ChargeDy * st.ChargeDist;
            int nx = Mathf.FloorToInt(fx), ny = Mathf.FloorToInt(fy);
            if ((nx != m.X || ny != m.Y))
            {
                if (TerrainMap.CanEnterTile(nx, ny) && TerrainMap.UnitAt(nx, ny, m) == null) { m.X = nx; m.Y = ny; }
                else { Rt.SetSlide(m, 0f, 0f); st.ChargeEnd = now + ChargeStumbleSeconds; return; }                  // hit a wall or a body: stops dead
            }
            Rt.SetSlide(m, fx - (m.X + 0.5f), fy - (m.Y + 0.5f));
            if (p != null && p.IsAlive() && !Body.Down(p))
            {
                var (mox, moy) = Rt.Offset(m); var (pox, poy) = Rt.Offset(p);
                float mx = m.X + 0.5f + mox, my = m.Y + 0.5f + moy, px = p.X + 0.5f + pox, py = p.Y + 0.5f + poy;
                if ((mx - px) * (mx - px) + (my - py) * (my - py) <= ChargeReach * ChargeReach)
                {
                    bool hit = Strike(m, p, 1f, true); if (hit) Effect(m, p, r.PounceEffect);
                    Rt.SetSlide(m, 0f, 0f); st.ChargeEnd = now + ChargeHitRecoverSeconds; return;
                }
            }
            if (st.ChargeDist >= r.ChargeLen) { Rt.SetSlide(m, 0f, 0f); st.ChargeEnd = now + ChargeStumbleSeconds; }       // ran the whole length without finding anything
        }

        /// <summary>Where a pack member wants to be (Yan, 2026-11-12): 1 = close in, -1 = back off, 0 = hold here, 2 = no opinion (the old chase). Pallid keeps at spit range, Dread at 3..6 tiles while its
        /// pounce is ready, Maw at 4..6 while its charge is ready; the Veil goes in (its strike then slips away, AfterAttack).</summary>
        public static int Stance(Mutant m, Player p, float now)
        {
            if (!Rt.On || !Has(m) || p == null || !p.IsAlive()) return 2;
            var st = Of(m); var r = RuleSet[m.Db.Name]; int d = CombatantUtil.Distance(m, p);
            if (now < st.DisengageUntil && d < 4) return -1;
            var grid = GameServices.RequireGrid(); bool los = grid != null && grid.CheckLineOfSight(m.X, m.Y, p.X, p.Y);
            switch (m.Db.Name)
            {
                case "Pallid Prowler": return d < r.SpitMin ? -1 : (d > r.SpitMax || !los) ? 1 : 0;
                case "Dread Prowler": if (now < st.NextPounce) return 2; if (d > r.PounceMax || !los) return 1; if (d < 3) return -1; return SideOk(m, p, PounceSideRatio) ? 0 : 3;
                case "Blind Maw": if (now < st.NextCharge) return 2; if (d > r.ChargeMax) return 1; if (d < r.ChargeMin) return -1; if (!los) return 1; return SideOk(m, p, ChargeSideRatio) ? 0 : 3;
                default: return 2;
            }
        }

        /// <summary>The tile a canine steps to when its lunge would be vertical: level with the target (or one row off), on the side it is already on, at charge or pounce distance.</summary>
        public static bool FlankGoal(Mutant m, ICombatant t, out int gx, out int gy)
        {
            var r = RuleSet[m.Db.Name]; int dist = r.ChargeMax > 0 ? r.ChargeMin + 1 : System.Math.Min(r.PounceMax, 4); int side = m.X >= t.X ? 1 : -1; gx = gy = 0;
            foreach (int k in new[] { dist, dist - 1, dist + 1 })
                foreach (int dy in new[] { 0, 1, -1 })
                    foreach (int sd in new[] { side, -side })
                    {
                        int x = t.X + sd * k, y = t.Y + dy;
                        if (TerrainMap.CanEnterTile(x, y) && TerrainMap.UnitAt(x, y, m) == null) { gx = x; gy = y; return true; }
                    }
            return false;
        }

        static void Contact(Mutant m, Player p, State st, float now)
        {
            if (st.Kind == "slam")                                                    // the big fist lands: everyone next to it is hit and knocked down (co-op: every player, not only the target)
            {
                st.DustStart = now;
                foreach (var pl in Player.All.ToArray())
                    if (pl != null && pl.IsAlive() && !Body.Down(pl) && CombatantUtil.IsAdjacent(m, pl) && Strike(m, pl, 1f, false)) Effect(m, pl, "knockdown");
                return;
            }
            if (p == null || !p.IsAlive()) return;
            if (st.Kind == "spit" || st.Kind == "throw")
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

        /// <summary>The rock comes down: every player within BoulderHitRadius tiles of where it lands is hit (a player who stepped away from the tile it was thrown at is missed).</summary>
        static void BoulderLands(Mutant m, State st)
        {
            foreach (var pl in Player.All.ToArray())
            {
                if (pl == null || !pl.IsAlive() || Body.Down(pl) || CombatantUtil.Distance(pl, st.SpitTx, st.SpitTy) > BoulderHitRadius) continue;
                if (Strike(m, pl, BoulderShare, true)) GameLog.Write("The boulder crashes into you!");
            }
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

        // ---- co-op (2026-11-12): the host sends each mutant's attack as a code, the seconds since it began (20 ms steps) and two small tile deltas; a joined player's mirror replays it ----
        const string Codes = "|bite|bite2|pounce|spit|charge|slam|swipe|throw";
        /// <summary>For a snapshot: (attack code 0..5, elapsed in 20 ms steps, d1, d2). d = (from tile - landing tile) for a pounce, (target tile - this tile) for a spit.</summary>
        public static (byte code, byte t20, sbyte d1, sbyte d2) NetState(Mutant m, float now)
        {
            if (m == null || m is HumanEnemy || !States.TryGetValue(m, out var st) || st.Kind == null) return (0, 0, 0, 0);
            int code = System.Array.IndexOf(Codes.Split('|'), st.Kind); if (code <= 0) return (0, 0, 0, 0);
            sbyte D(int v) => (sbyte)Mathf.Clamp(v, -127, 127);
            sbyte d1 = 0, d2 = 0;
            if (st.Kind == "pounce") { d1 = D(st.FromX - m.X); d2 = D(st.FromY - m.Y); }
            else if (st.Kind == "spit" || st.Kind == "throw") { d1 = D(st.SpitTx - m.X); d2 = D(st.SpitTy - m.Y); }
            return ((byte)code, (byte)Mathf.Clamp(Mathf.RoundToInt((now - st.Start) * 50f), 0, 255), d1, d2);
        }

        /// <summary>On a client: set a mirror mutant's attack from the host's snapshot (the damage and the hit effects come as the host's strike events; this is the picture).</summary>
        public static void Mirror(Mutant m, int code, float elapsed, int d1, int d2, float now)
        {
            if (m == null || !Has(m)) return;
            var st = Of(m); st.Mirror = true;
            string kind = code > 0 && code < Codes.Split('|').Length ? Codes.Split('|')[code] : null;
            if (kind == null) { if (st.Kind == "pounce" || st.Kind == "charge") Rt.ReleaseSlide(m); st.Kind = null; return; }
            bool fresh = st.Kind != kind || System.Math.Abs((now - st.Start) - elapsed) > 0.15f;
            if (st.Kind != kind) st.Hit = false;
            st.Kind = kind; if (fresh) st.Start = now - elapsed;
            var a = Anim(m.Db.Name, kind);
            if (kind == "pounce") { st.FromX = m.X + d1; st.FromY = m.Y + d2; st.ToX = m.X; st.ToY = m.Y; Rt.HoldSlide(m); }
            else if (kind == "charge") Rt.HoldSlide(m);                                      // the host's tile and in-tile offset already carry the run, no smoothing on top
            else if ((kind == "spit" || kind == "throw") && a != null)
            {
                st.SpitTx = m.X + d1; st.SpitTy = m.Y + d2; st.ThrowFaceLeft = st.SpitTx < m.X;
                float dist = Mathf.Sqrt(d1 * d1 + d2 * d2); st.SpitArrive = st.Start + a.ContactMs / 1000f + dist / SpitSpeed;
            }
        }

        /// <summary>The attack frame to draw for this mutant now, or null (walk or stand-still sprite).</summary>
        public static Sprite Frame(Mutant m, string name, float now)
        {
            if (!Rt.On || m == null || !m.IsAlive() || !States.TryGetValue(m, out var st) || st.Kind == null) return null;
            if (st.Mirror && st.Kind == "pounce")
            {
                var pa = Anim(name, "pounce"); float tm = (now - st.Start) * 1000f;
                if (pa != null) { float k = 1f - TravelAt(pa.Ms, pa.Travel, tm); Rt.SetSlide(m, tm < LaunchMs(pa.Ms, pa.Travel) ? 0f : (st.FromX - st.ToX) * k, tm < LaunchMs(pa.Ms, pa.Travel) ? 0f : (st.FromY - st.ToY) * k); }
            }
            if (st.Kind == "charge")
            {
                if (st.Mirror) { var wfm = UnitSprites.HDWalk(name); return (now - st.Start) < ChargeWindupSeconds ? UnitSprites.HDAttack(name, "bite")?[2] : wfm == null ? null : wfm[Mathf.Max(0, Mathf.FloorToInt((now - st.Start - ChargeWindupSeconds) / (MutantWalk.FrameSeconds * 0.5f))) % wfm.Length]; }
                if (st.ChargeRunStart < 0f || st.ChargeEnd >= 0f) { var rb = UnitSprites.HDAttack(name, "bite"); return st.ChargeEnd >= 0f ? null : rb != null ? rb[2] : null; }      // the rear-back before the run; stumbling shows the stand pose
                var wf = UnitSprites.HDWalk(name); return wf == null ? null : wf[Mathf.Max(0, Mathf.FloorToInt((now - st.ChargeRunStart) / (MutantWalk.FrameSeconds * 0.5f))) % wf.Length];     // running: the walk, twice as fast
            }
            var a = Anim(name, st.Kind); var frames = UnitSprites.HDAttack(name, st.Kind);
            if (a == null || frames == null) return null;
            return frames[Mathf.Clamp(FrameIndexAt(a.Ms, (now - st.Start) * 1000f), 0, frames.Length - 1)];
        }

        public static readonly int[] SplatMs = { 60, 60, 70, 80, 120, 140 };
        /// <summary>The acid splash where a glob landed (tile space, ground level) and which of its frames is up, for 0.53 s after the landing.</summary>
        public static bool SplatAt(Mutant m, float now, out float x, out float y, out int frame)
        {
            x = y = 0f; frame = 0;
            if (m == null || !States.TryGetValue(m, out var st)) return false;
            float ss = st.SplatStart >= 0f ? st.SplatStart : st.SpitArrive > 0f ? st.SpitArrive : -99f; if (ss < 0f) return false;
            float tMs = (now - ss) * 1000f; int total = 0; foreach (var d in SplatMs) total += d;
            if (tMs < 0f || tMs >= total) return false;
            frame = FrameIndexAt(SplatMs, tMs); x = st.SpitTx + 0.5f; y = st.SpitTy + 0.5f; return true;
        }

        /// <summary>The Brute's boulder in flight: tile-space position, height above the ground (an arc) and which of its 6 tumbling frames to draw.</summary>
        public static bool BoulderAt(Mutant m, float now, out float x, out float y, out float h, out int frame)
        {
            x = y = h = 0f; frame = 0;
            if (m == null || !States.TryGetValue(m, out var st) || st.Kind != "throw" || st.SpitArrive < 0f || st.SpitDone || now >= st.SpitArrive) return false;
            var a = Anim(m.Db.Name, "throw"); if (a == null) return false;
            float tStart = st.Start + a.ContactMs / 1000f, k = Mathf.Clamp01((now - tStart) / Mathf.Max(0.01f, st.SpitArrive - tStart));
            float ox = 0f, oh = 0.9f;                                                // where the rock leaves: the release point of the contact frame (px in the strip, 32 px a tile, pivot 16 px above the ground), mirrored with the sprite
            if (a.MuzzleX != null && a.Contact < a.MuzzleX.Length) { ox = (st.ThrowFaceLeft ? -1f : 1f) * (a.MuzzleX[a.Contact] - a.W * 0.5f) / 32f; oh = (a.H * (1f - a.PivotY) - a.MuzzleY[a.Contact]) / 32f; }
            float sx = m.X + 0.5f + ox, sy = m.Y + 0.5f, tx = st.SpitTx + 0.5f, ty = st.SpitTy + 0.5f;
            x = sx + (tx - sx) * k; y = sy + (ty - sy) * k; h = oh * (1f - k) + 0.55f * Mathf.Sin(Mathf.PI * k) + 0.1f; frame = (int)(now * 14f) % 6; return true;
        }

        /// <summary>While the Brute throws, which way its sprite faces: toward the tile it throws at, fixed when the throw began (the view's usual flip follows the local player, which would turn the held rock the wrong way on a client).</summary>
        public static bool FaceOverride(Mutant m, out bool left)
        {
            left = false;
            if (m == null || !States.TryGetValue(m, out var st) || st.Kind != "throw") return false;
            left = st.ThrowFaceLeft; return true;
        }

        public static readonly int[] DustMs = { 60, 60, 70, 80, 120, 140 };
        /// <summary>The slam's dust ring round the Brute (tile space) and which of its frames is up, for 0.53 s after the fist lands.</summary>
        public static bool DustAt(Mutant m, float now, out int frame)
        {
            frame = 0;
            if (m == null || !States.TryGetValue(m, out var st)) return false;
            float ds = st.Kind == "slam" && st.Mirror ? st.Start + (Anim(m.Db.Name, "slam")?.ContactMs ?? 0) / 1000f : st.DustStart;
            if (ds < 0f) return false;
            float tMs = (now - ds) * 1000f; int total = 0; foreach (var d in DustMs) total += d;
            if (tMs < 0f || tMs >= total) return false;
            frame = FrameIndexAt(DustMs, tMs); return true;
        }

        /// <summary>Where the spit glob is right now (tile space), or false when none is in the air; the view flies the glob with it.</summary>
        public static bool GlobAt(Mutant m, float now, out float x, out float y, out float angle)
        {
            x = y = angle = 0f;
            if (m == null || !States.TryGetValue(m, out var st) || st.Kind != "spit" || st.SpitArrive < 0f || st.SpitDone || now >= st.SpitArrive) return false;
            var a = Anim(m.Db.Name, "spit"); if (a == null) return false;
            float tStart = st.Start + a.ContactMs / 1000f, k = Mathf.Clamp01((now - tStart) / Mathf.Max(0.01f, st.SpitArrive - tStart));
            float sx = m.X + 0.5f, sy = m.Y + 0.5f, tx = st.SpitTx + 0.5f, ty = st.SpitTy + 0.5f;
            x = sx + (tx - sx) * k; y = sy + (ty - sy) * k; angle = Mathf.Atan2(ty - sy, tx - sx) * Mathf.Rad2Deg; return true;
        }
    }
}
