// Boss 3, the Sump fight (Yan, 2026-11-05: "it having the entire body out of the water doesn't work during the bossfight ... several deep pools where it can come up from to attack players in
// different parts of the arena ... the head attacks one while the tail comes out of the other pool ... IMPACT, heavy and strong like the hydra in God of War").
// The eel lives under FOUR deep pools (EelSump.Pools: the big one and the west, east and north ones, marked in the plan by 0 y z N). Only what is out of the water exists above the surface.
// Nothing is a physics rope any more: every move is AUTHORED - the eel's body is a 35-tile spine (N samples, head first, x/y in tiles, Z = height, Z < 0 = under the water) laid along hand-made
// curves, the head (or the tail tip) is driven along a curve by a timeline, and the body follows the same curve. Slow and readable wind-ups, a strike that accelerates INTO the hit, then it stays.
// RULE (Yan): it always comes up out of a pool's CENTRE and goes down its CENTRE - every curve starts and ends vertical over a pool centre (EelSumpChecks asserts every surface crossing).
// Phase 1 (above half HP): COMBO (head strike from one pool + a tail sweep round another), ARCH (out of one pool, over the arena, crash into the big one), DASH (fully out of one pool, an S across the
// arena, down into another: whoever is on the path is flung). Phase 2 (half HP and below) adds SNARE (the tail comes out of a pool, catches a player, drags him to the pool, throws him out of ANOTHER
// pool: LAUNCH = straight up and crash, or HURL = at another player) and FLOOD (the floor turns to deep water for a while, it glides through as a shadow and bursts out under someone).
// The head and the tail are separate targets (the main Unit and a tail Unit with its own HP): shoot the tail enough and it lets go and goes down. Script times are x1/Speed: Yan slowed it 20%.
// Host-side (Rt.WorldTick); co-op: moves pick targets among Player.All; every player in a zone is hit. All damage, timings and radii are first guesses.
using System;
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class EelSump
    {
        public const int N = 140; public const float PerTile = 4f, Speed = 0.8f, HideZ = -5f, Surface = 0.02f;   // 35 tiles of spine, 4 samples a tile; "out of the water" = Z above Surface
        public const int TailMax = 2600; public const string TailName = "The Drowner's tail";
        public static readonly MutantDef TailDef = new MutantDef { Name = TailName, Tier = 3, Hp = TailMax, PhysDR = 0.1f, AnomDR = 0.3f, Ap = 0, Mp = 0, Ac = 10, AttackDamage = 0, Behavior = "boss", PanicThreshold = 0f };

        public class Pool { public string Name; public float X, Y, RX, RY; public bool Big; }
        public class Ev { public string Kind, Text; public float X, Y, Power; public bool Water; public int Who, Dmg; }
        public class Fling { public float X0, Y0, X1, Y1, T0, Dur, Apex, Z0, Spin, Z, Rot; public string Land; public Player Hurled; }
        public class State
        {
            public List<Pool> Pools = new List<Pool>(); public Pool Big; public float SlabX, SlabY;
            public readonly float[] X = new float[N], Y = new float[N], Z = new float[N];          // the spine, head first
            public Mutant Tail; public float TailDownUntil = -1f; public int LastHp, LastTailHp; public bool HeadOut, TailOut, TailBroken, Dying;
            public readonly List<Ev> Events = new List<Ev>(), Out = new List<Ev>(); public float Entered; public readonly Dictionary<Player, Fling> Air = new Dictionary<Player, Fling>(); public readonly List<(Player p, Pool pool, float x, float y)> Pulled = new List<(Player, Pool, float, float)>();   // dragged under by a dive's water: the tail throws them out once the dive is over
            public string Bite; public float BiteT, StormSg, SinkT = -1f, SinkFrom, Mouth = -1f, GrabStart, GrabLast, StunUntil; public int Mash; public float Grip, Twitch, GrabLastU, BiteU, SkyK; public (float x, float y) VictimPos, BiteFrom; public (float sx, float sy) GrabShift; public float BiteSide; public (float x, float y) BiteEnd; public bool Parried; public bool Won; public Player Victim, ForcedVictim; public (float x, float y) BiteSpot; public Curve BiteCur;   // the bite that ends the storm: open, lunge, then grapple / slump / recover / sink
            public readonly HashSet<(int, int)> Wet = new HashSet<(int, int)>(); public bool Stage3, FloodSnd, RushSnd, GlintSnd; public float Rear, Charge; public float SparkAt;   // Rear: degrees the head is thrown back (charging, screaming, rearing back to strike); public float SparkAt;   // Wet: the water tiles the storm electrifies; Stage3: the 50% transition (a storm) has been queued
            public int FloodVer;  // counts changes of Flip, for the view's flood texture
            public bool Stage2;   // the 75% transition (a forced flood surge) has been queued
            public readonly List<string> Queue = new List<string>(), Bag = new List<string>();   // Queue: forced moves for the harness ("combo", "snare:hurl" ...)
            public bool NoStrike; public string Variant, RevealKey; public float FloodR; public int Flooded;
            // the move in progress
            public Strike Hs; public Curve Cur, Cur2; public Pool P1, P2, P3; public Player Tg, Tg2, Snared; public (float x, float y) GP, LP; public float TH0, EndAt = -1f, BreakTs = -1f, BreakQ;
            public bool Hit1, Hit2, Missed, Grabbed, Released, Crossed, Threw; public readonly HashSet<Player> Hurt = new HashSet<Player>(); public float PrevHZ = 9f;
            public List<(int x, int y, float d)> Shallow; public readonly HashSet<(int, int)> Flip = new HashSet<(int, int)>();
        }

        // ------------------------------------------------------------------ curves
        public class Curve
        {
            readonly float[] px, py, pz, cum; readonly int n; public readonly float L; readonly float[] d0 = new float[3], d1 = new float[3];
            public Curve(List<(float x, float y, float z)> pts)
            {
                var p = new List<(float x, float y, float z)>();
                foreach (var q in pts) if (p.Count == 0 || D3(p[p.Count - 1], q) > 1e-4f) p.Add(q);
                if (p.Count < 2) p.Add((p[0].x, p[0].y, p[0].z - 1f));
                n = p.Count; px = new float[n]; py = new float[n]; pz = new float[n]; cum = new float[n];
                for (int i = 0; i < n; i++) { px[i] = p[i].x; py[i] = p[i].y; pz[i] = p[i].z; cum[i] = i == 0 ? 0f : cum[i - 1] + D3(p[i - 1], p[i]); }
                L = cum[n - 1];
                float a = 1f / Mathf.Max(1e-4f, cum[1]); d0[0] = (px[1] - px[0]) * a; d0[1] = (py[1] - py[0]) * a; d0[2] = (pz[1] - pz[0]) * a;
                float b = 1f / Mathf.Max(1e-4f, cum[n - 1] - cum[n - 2]); d1[0] = (px[n - 1] - px[n - 2]) * b; d1[1] = (py[n - 1] - py[n - 2]) * b; d1[2] = (pz[n - 1] - pz[n - 2]) * b;
            }
            public (float x, float y, float z) At(float s)       // before the start and past the end it carries straight on (down into the pool)
            {
                if (s <= 0f) return (px[0] + d0[0] * s, py[0] + d0[1] * s, pz[0] + d0[2] * s);
                if (s >= L) { float e = s - L; return (px[n - 1] + d1[0] * e, py[n - 1] + d1[1] * e, pz[n - 1] + d1[2] * e); }
                int lo = 0, hi = n - 1; while (hi - lo > 1) { int m = (lo + hi) >> 1; if (cum[m] <= s) lo = m; else hi = m; }
                float u = (s - cum[lo]) / Mathf.Max(1e-6f, cum[hi] - cum[lo]); return (px[lo] + (px[hi] - px[lo]) * u, py[lo] + (py[hi] - py[lo]) * u, pz[lo] + (pz[hi] - pz[lo]) * u);
            }
            public List<(float x, float y, float z)> Points() { var l = new List<(float, float, float)>(); for (int i = 0; i < n; i++) l.Add((px[i], py[i], pz[i])); return l; }
        }
        static float D3((float x, float y, float z) a, (float x, float y, float z) b) { float dx = a.x - b.x, dy = a.y - b.y, dz = a.z - b.z; return Mathf.Sqrt(dx * dx + dy * dy + dz * dz); }
        public static List<(float x, float y, float z)> Bez((float x, float y, float z) a, (float x, float y, float z) b, (float x, float y, float z) c, (float x, float y, float z) d, int n = 60)
        {
            var l = new List<(float, float, float)>();
            for (int i = 0; i <= n; i++) { float t = i / (float)n, u = 1f - t, w0 = u * u * u, w1 = 3f * u * u * t, w2 = 3f * u * t * t, w3 = t * t * t; l.Add((w0 * a.x + w1 * b.x + w2 * c.x + w3 * d.x, w0 * a.y + w1 * b.y + w2 * c.y + w3 * d.y, w0 * a.z + w1 * b.z + w2 * c.z + w3 * d.z)); }
            return l;
        }
        static float Smr(float u) { u = Mathf.Clamp01(u); return u * u * u * (u * (6f * u - 15f) + 10f); }      // slow start, slow end: heavy
        static float Ein(float u, float p = 3f) => Mathf.Pow(Mathf.Clamp01(u), p);                                // accelerates the whole way: it hits at full speed
        static float Eout(float u, float p = 3f) => 1f - Mathf.Pow(1f - Mathf.Clamp01(u), p);                      // fast then settling: momentum
        static float Lerp(float a, float b, float u) => a + (b - a) * u;

        /// <summary>A head strike: it comes up the pool's centre as a column that leans toward the target (the head and neck hover there: the rear-up), the target is locked part way through,
        /// then it arcs down onto it. Timeline (script s after t0): emerge 0-1.6, creep 1.6-2.1, STRIKE 2.1-2.3 (accelerating), press 2.3-3.0, withdraw 3.0-4.1.</summary>
        public class Strike
        {
            public Pool P; public (float x, float y) T; public Curve C1, Full; public float L1, Apex, StrikeEnd = 2.3f;
            public Strike(Pool p, (float x, float y) t)
            {
                P = p; T = t; float dx = t.x - p.X, dy = t.y - p.Y, d = Mathf.Sqrt(dx * dx + dy * dy) + 1e-3f; Apex = 4.2f + 0.3f * Mathf.Min(d, 12f); float lx = 0.22f * dx, ly = 0.22f * dy;
                C1 = new Curve(Bez((p.X, p.Y, -3.2f), (p.X, p.Y, 1.8f), (p.X + 0.1f * dx, p.Y + 0.1f * dy, Apex - 1.4f), (p.X + lx, p.Y + ly, Apex))); L1 = C1.L;
            }
            public void Lock((float x, float y) t)
            {
                T = t; float dx = t.x - P.X, dy = t.y - P.Y; var pts = C1.Points(); var a = pts[pts.Count - 1];
                pts.AddRange(Bez(a, (a.x + 0.18f * dx, a.y + 0.18f * dy, a.z + 1.6f), (t.x - 0.2f * dx, t.y - 0.2f * dy, 3.0f + 0.2f * Apex), (t.x, t.y, 0.45f)).Skip(1)); Full = new Curve(pts);
            }
            public Curve Cur => Full ?? C1;
            public float Sigma(float u)
            {
                if (u < 0f) return 0f;
                if (u < 1.6f) return L1 * Smr(u / 1.6f);
                if (Full == null) return L1;
                float c = L1 + 0.05f * (Full.L - L1);
                if (u < 2.1f) return Lerp(L1, c, (u - 1.6f) / 0.5f);
                if (u < 2.3f) return Lerp(c, Full.L, Ein((u - 2.1f) / 0.2f, 2.6f));
                if (u < 3.0f) return Full.L;
                if (u < 4.1f) return Full.L * (1f - Smr((u - 3.0f) / 1.1f));
                return 0f;
            }
        }

        // ------------------------------------------------------------------ the world
        public static State Create(string zone)
        {
            var s = new State();
            foreach (var (ch, name, rx, ry, big) in new[] { ('0', "big", 8.2f, 4.0f, true), ('y', "west", 3.7f, 2.4f, false), ('z', "east", 3.7f, 2.4f, false), ('N', "north", 3.5f, 2.3f, false) })
            { var m = EelBoss.Marks(zone, ch); if (m.Count == 1) s.Pools.Add(new Pool { Name = name, X = m[0].x + 0.5f, Y = m[0].y + 0.5f, RX = rx, RY = ry, Big = big }); }
            s.Big = s.Pools.First(p => p.Big); var b = EelBoss.Marks(zone, '8'); s.SlabX = b[0].x + 0.5f; s.SlabY = b[0].y + 0.5f; Hide(s, s.Big); return s;
        }
        static void Hide(State s, Pool p, int from = 0, int to = N) { for (int i = from; i < to; i++) { s.X[i] = p.X; s.Y[i] = p.Y; s.Z[i] = HideZ - i * 0.01f; } }
        static void FillHead(State s, Curve c, float sig) { for (int i = 0; i < N; i++) { var q = c.At(sig - i / PerTile); s.X[i] = q.x; s.Y[i] = q.y; s.Z[i] = q.z; } }
        static void FillTail(State s, Curve fromTip, float sig = 0f, bool keepHead = false)
        {
            for (int i = N / 2 - 10; i < N; i++)                                                                       // 10 samples (2.5 tiles) further back than the half: the tail's root runs on down into its pool instead of ending in mid-air
            {
                var q = fromTip.At(sig + (N - 1 - i) / PerTile);
                if (i < N / 2 && s.Z[i] > Surface) continue;                                                           // those only where the head's half is hidden anyway
                if (keepHead && q.z <= Surface && s.Z[i] > Surface) continue; s.X[i] = q.x; s.Y[i] = q.y; s.Z[i] = q.z;
            }
        }
        // (a hidden tail never cuts the head's column off in mid-air: keepHead; the tail curve is ordered tip first)
        static (float x, float y) Pos(Player p) => Ballistics.Pos(p);
        static void Place(Player p, float x, float y) { int tx = (int)Mathf.Floor(x), ty = (int)Mathf.Floor(y); p.X = tx; p.Y = ty; Rt.SetOffset(p, x - tx - 0.5f, y - ty - 0.5f); }
        static Pool Nearest(State s, float x, float y, Func<Pool, bool> ok = null) => s.Pools.Where(p => ok == null || ok(p)).OrderBy(p => (p.X - x) * (p.X - x) + (p.Y - y) * (p.Y - y)).FirstOrDefault();
        static float Dist(float ax, float ay, float bx, float by) => Mathf.Sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by));
        static int Index(Player p) => Player.All.ToList().IndexOf(p);
        static System.Random Rng = new System.Random(5);
        static T Pick<T>(IList<T> l) => l[Rng.Next(l.Count)];

        // ------------------------------------------------------------------ the clock
        public static void Tick(EelBoss.Fight f, List<Player> players, float now, float dt)
        {
            var s = f.Sump; s.Events.Clear(); float ts = (now - f.StageAt) * Speed;
            switch (f.Stage)
            {
                case "hidden":
                    f.Label = "in the pools"; Hide(s, s.Big);
                    if (players.Any(p => { var q = Pos(p); return q.x >= 5f && q.x <= 55f && q.y >= 9f && q.y <= 31f; })) s.Entered += dt; else s.Entered = 0f;
                    if (s.Entered >= 1.5f) { f.Started = true; f.Unit.IsAggroed = true; EelBoss.GoStage(f, "reveal"); GameLog.Write("The water in the pools goes very still."); }       // it comes for you when you step into its room (or when it is shot)
                    break;
                case "reveal": Reveal(f, s, ts, now, false); break;
                case "vomit": Reveal(f, s, ts, now, true); break;
                case "idle":
                    f.Label = "under the water"; Hide(s, s.Big); f.Zones.Clear();
                    if (s.Dying) { EelBoss.GoStage(f, "vomit"); break; }
                    if (now >= f.Next) StartMove(f, s, players, now);
                    break;
                case "move": RunMove(f, s, players, now, ts); break;
            }
            Fly(f, s, players, now); UpdateUnits(f, s); s.LastHp = f.Unit.Hp; s.LastTailHp = s.Tail.Hp;
            s.Out.AddRange(s.Events); if (s.Out.Count > 200) s.Out.RemoveRange(0, s.Out.Count - 200);       // the view drains these (impacts, hits)
            if (s.TailDownUntil >= 0f && now >= s.TailDownUntil) { s.TailDownUntil = -1f; s.Tail.Hp = TailMax; s.TailBroken = false; }
        }

        static void UpdateUnits(EelBoss.Fight f, State s)
        {
            int hi = 0, ti = N - 1; s.HeadOut = s.Z[hi] > 0.3f; s.TailOut = s.Z[ti] > 0.3f;
            var hp = s.HeadOut ? (s.X[hi], s.Y[hi]) : (s.Big.X, s.Big.Y); var tp = s.TailOut ? (s.X[ti], s.Y[ti]) : (s.Big.X, s.Big.Y);
            f.Unit.X = (int)Mathf.Floor(hp.Item1); f.Unit.Y = (int)Mathf.Floor(hp.Item2); Rt.SetOffset(f.Unit, hp.Item1 - f.Unit.X - 0.5f, hp.Item2 - f.Unit.Y - 0.5f);
            s.Tail.X = (int)Mathf.Floor(tp.Item1); s.Tail.Y = (int)Mathf.Floor(tp.Item2); Rt.SetOffset(s.Tail, tp.Item1 - s.Tail.X - 0.5f, tp.Item2 - s.Tail.Y - 0.5f);
        }

        // ------------------------------------------------------------------ picking the next move (the snare and the flood join from 75% HP, Yan 2026-11-06)
        static void StartMove(EelBoss.Fight f, State s, List<Player> players, float now)
        {
            string kind = null; s.Variant = null;
            if (s.Queue.Count > 0) { kind = s.Queue[0]; s.Queue.RemoveAt(0); if (kind.Contains(":")) { var sp = kind.Split(':'); kind = sp[0]; s.Variant = sp[1]; } }
            else
            {
                if (s.Bag.Count == 0) { s.Bag.AddRange(new[] { "combo", "arch", "dash" }); if (f.Phase >= 1) s.Bag.AddRange(new[] { "snare", "flood" }); if (f.Phase >= 2) s.Bag.Add("storm"); s.Bag.Sort((a, b) => Rng.Next(2) == 0 ? -1 : 1); }
                kind = s.Bag[0]; s.Bag.RemoveAt(0);
            }
            if (kind == "snare" && s.TailBroken) kind = "arch";                                           // the tail is down: something else
            if (kind == "snare" && s.Variant == null) s.Variant = players.Count >= 2 && Rng.Next(2) == 0 ? "hurl" : "launch";
            if (kind == "snare" && s.Variant == "hurl" && players.Count < 2) s.Variant = "launch";
            s.Hit1 = s.Hit2 = s.Missed = s.Grabbed = s.Released = s.Crossed = s.Threw = false; s.Hurt.Clear(); s.Pulled.Clear(); s.SparkAt = 0f; s.FloodSnd = s.RushSnd = s.GlintSnd = false; s.Rear = 0f; s.Bite = null; s.BiteCur = null; s.Parried = false; s.SkyK = 0f; s.SinkT = -1f; s.Victim = null; s.Mouth = -1f; s.StormSg = 0f; s.Grip = 0f; s.Twitch = 0f; s.Snared = null; s.EndAt = -1f; s.BreakTs = -1f; s.PrevHZ = 9f; s.Cur = s.Cur2 = null; s.Hs = null;
            s.Tg = players.OrderBy(_ => Rng.Next()).First(); s.Tg2 = players.Where(q => q != s.Tg).OrderBy(_ => Rng.Next()).FirstOrDefault() ?? s.Tg; var tp = Pos(s.Tg); var t2 = Pos(s.Tg2);
            EelBoss.GoStage(f, "move"); f.Move = kind; f.Zones.Clear(); f.Label = kind.ToUpper() + ": wind-up";
            switch (kind)
            {
                case "combo":
                    s.P1 = Nearest(s, tp.x, tp.y, p => Dist(p.X, p.Y, tp.x, tp.y) >= 3.8f) ?? Nearest(s, tp.x, tp.y);
                    s.P2 = s.Pools.Where(p => p != s.P1).OrderByDescending(p => Dist(p.X, p.Y, s.P1.X, s.P1.Y) - (Dist(p.X, p.Y, t2.x, t2.y) > 11f ? 30f : 0f)).First();
                    s.Hs = new Strike(s.P1, tp); s.TH0 = Mathf.Atan2(t2.y - s.P2.Y, t2.x - s.P2.X) - 2f * Mathf.PI * 1.15f * Mathf.Pow(0.55f, 1.7f); f.Label = "COMBO: head strike + tail sweep - wind-up"; break;
                case "arch":
                    s.P1 = s.Pools.FirstOrDefault(p => p.Name == s.Variant && !p.Big) ?? Pick(s.Pools.Where(p => !p.Big).ToList()); var b = s.Big;
                    s.Cur = new Curve(Bez((s.P1.X, s.P1.Y, -3.2f), (s.P1.X, s.P1.Y, 13f), (b.X, b.Y, 13f), (b.X, b.Y, -3.5f), 90)); break;
                case "dash":
                    var ends = s.Pools.Where(p => !p.Big).ToList(); var named = s.Variant != null && s.Variant.Contains(">") ? s.Variant.Split('>') : null;
                    s.P1 = named != null ? ends.First(p => p.Name == named[0]) : Pick(ends); s.P2 = named != null ? ends.First(p => p.Name == named[1]) : ends.Where(p => p != s.P1).OrderByDescending(p => Dist(p.X, p.Y, s.P1.X, s.P1.Y)).First(); s.Cur = Dash(s.P1, s.P2); break;
                case "snare": StartSnare(f, s, players, tp, t2); break;
                case "flood": StartFlood(f, s, players, tp); break;
                case "storm": StartStorm(f, s, players); break;
            }
        }
        public static Curve Dash(Pool a, Pool b)
        {
            float dx = b.X - a.X, dy = b.Y - a.Y, d = Mathf.Sqrt(dx * dx + dy * dy), ux = dx / d, uy = dy / d, px = -uy, py = ux; var pts = new List<(float, float, float)>();
            pts.AddRange(Bez((a.X, a.Y, -4f), (a.X, a.Y, 3.8f), (a.X + 3f * ux, a.Y + 3f * uy, 3.6f), (a.X + 5.5f * ux, a.Y + 5.5f * uy, 0.45f)));      // up the centre, rears, comes down to the floor
            for (int i = 1; i <= 160; i++) { float s0 = 5.5f + (d - 11f) * i / 160f, off = 2.3f * Mathf.Sin(s0 / 14f * 2f * Mathf.PI); pts.Add((a.X + ux * s0 + px * off, a.Y + uy * s0 + py * off, 0.45f + 0.2f * Mathf.Sin(s0 / 7f * 2f * Mathf.PI))); }   // the S across the arena
            pts.Add((b.X - 1.7f * ux, b.Y - 1.7f * uy, 0.5f)); pts.Add((b.X - 0.7f * ux, b.Y - 0.7f * uy, 1.3f)); pts.Add((b.X, b.Y, 1.7f)); pts.Add((b.X, b.Y, 0.6f)); pts.Add((b.X, b.Y, -1f)); pts.Add((b.X, b.Y, -4f));      // and down the other pool's centre, straight
            return new Curve(pts);
        }

        static void End(EelBoss.Fight f, State s, float now)
        {
            f.Stage = "idle"; f.StageAt = now; f.Move = null; f.Zones.Clear(); f.Next = now + (f.Phase >= 2 ? 1.5f : 2.2f); f.Label = "under the water";
            Restore(s, true); if (s.Snared != null) { s.Snared = null; }
        }

        // ------------------------------------------------------------------ the moves
        static void RunMove(EelBoss.Fight f, State s, List<Player> players, float now, float ts)
        {
            switch (f.Move)
            {
                case "combo": Combo(f, s, players, now, ts); break;
                case "arch": Arch(f, s, players, now, ts); break;
                case "dash": DashRun(f, s, players, now, ts); break;
                case "snare": SnareRun(f, s, players, now, ts); break;
                case "flood": FloodRun(f, s, players, now, ts); break;
                case "storm": StormRun(f, s, players, now, ts); break;
            }
        }

        static void Knock(Player p, float fx, float fy, float dist, float prone, float now)
        {
            var q = Pos(p); float dx = q.x - fx, dy = q.y - fy, l = Mathf.Sqrt(dx * dx + dy * dy) + 1e-3f; Rt.MoveBy(p, dx / l * dist, dy / l * dist); Rt.Of(p).ProneUntil = Mathf.Max(Rt.Of(p).ProneUntil, now + prone);
        }
        static bool Damage(State s, Player p, int dmg, string what, float fx, float fy, float knock, float prone, float now)
        {
            if (Rt.Untouchable(p) || s.Air.ContainsKey(p)) return false;
            EelBoss.HurtPublic(p, dmg, what, "Blunt"); Knock(p, fx, fy, knock, prone, now); s.Events.Add(new Ev { Kind = "hit", Who = Index(p), Dmg = dmg, Text = what, X = Pos(p).x, Y = Pos(p).y }); return true;
        }
        static void Snd(string name, float x, float y, float vol = 1f) => Sfx.Play(name, new Vector3(x, -y, 0f), vol, 0.04f, 1f);   // host-side, like the boss sting (co-op: clients get no eel sounds yet)
        static void Impact(State s, float x, float y, float power, bool water) => s.Events.Add(new Ev { Kind = "impact", X = x, Y = y, Power = power, Water = water });
        static void Ring(EelBoss.Fight f, float x, float y, float r, bool live = false, string kind = "ring") => f.Zones.Add(new EelBoss.Zone { X = x, Y = y, R = r, Kind = kind, Live = live });

        // ---- COMBO: a head strike out of one pool, a tail sweep round another
        static void Combo(EelBoss.Fight f, State s, List<Player> players, float now, float ts)
        {
            var hs = s.Hs; f.Zones.Clear();
            if (ts >= 1.2f && hs.Full == null) { var q = Pos(s.Tg); hs.Lock(q); }
            if (hs.Full != null) Ring(f, hs.T.x, hs.T.y, 2.0f, ts >= 2.3f);
            float sg = hs.Sigma(ts); FillHead(s, hs.Cur, sg);
            float u, k, th, tz; TailPar(s, ts, out u, out k, out th, out tz); float om = (TailTh(s, ts + 0.03f) - TailTh(s, ts - 0.03f)) / 0.06f;
            if (s.TailBroken && s.BreakTs < 0f) { s.BreakTs = ts; s.BreakQ = u; }
            if (s.BreakTs >= 0f) { u = Mathf.Max(0f, s.BreakQ - (ts - s.BreakTs) / 0.7f); k = 1f; }
            var arm = Arm(s.P2, u, k, th, tz, om, 8.4f); FillTail(s, new Curve(Enumerable.Reverse(arm).ToList()), 0f, true);   // the head was filled this frame: keep its column down to the water
            if (ts > 0.6f && ts < 3.4f && s.BreakTs < 0f) Ring(f, s.P2.X, s.P2.Y, 8.4f, ts >= 2.3f, "sweep");
            if (!s.Hit1 && ts >= 2.3f && hs.Full != null) { s.Hit1 = true; f.Label = "COMBO: HEAD STRIKE"; Impact(s, hs.T.x, hs.T.y, 1.0f, false); foreach (var p in players) if (Dist(Pos(p).x, Pos(p).y, hs.T.x, hs.T.y) <= 2.0f) Damage(s, p, 260, "The head slams down", hs.T.x, hs.T.y, 3f, 1.0f, now); }
            if (ts >= 2.3f && ts < 3.4f && u > 0.5f && s.BreakTs < 0f) { f.Label = "COMBO: TAIL SWEEP"; foreach (var p in players) { if (s.Hurt.Contains(p)) continue; var q = Pos(p); if (ArmNear(arm, q.x, q.y, 1.15f)) { if (Damage(s, p, 200, "The tail sweeps you", s.P2.X, s.P2.Y, 3.5f, 1.0f, now)) s.Hurt.Add(p); } } }
            if (!s.Hit2 && ts >= 3.4f && u > 0.5f && s.BreakTs < 0f) { s.Hit2 = true; var tipP = arm[arm.Count - 1]; Impact(s, tipP.x, tipP.y, 0.75f, false); foreach (var p in players) if (Dist(Pos(p).x, Pos(p).y, tipP.x, tipP.y) <= 2.0f) Damage(s, p, 240, "The tail slams down", tipP.x, tipP.y, 3f, 1.0f, now); }
            if (ts >= 4.4f) End(f, s, now);
        }
        static float TailTh(State s, float ts) { float tau = Mathf.Clamp01((ts - 2.3f) / 1.1f); return s.TH0 + 2f * Mathf.PI * 1.15f * Mathf.Pow(tau, 1.7f); }
        static void TailPar(State s, float ts, out float u, out float k, out float th, out float tz)
        {
            th = s.TH0; tz = 0.4f; k = 0f; u = 0f;
            if (ts < 0.4f) return;
            if (ts < 1.6f) { u = Smr((ts - 0.4f) / 1.2f); return; }
            u = 1f; if (ts < 2.0f) return;
            if (ts < 2.3f) { k = Ein((ts - 2.0f) / 0.3f, 2.2f); return; }
            k = 1f;
            if (ts < 3.4f) { float tau = (ts - 2.3f) / 1.1f; th = TailTh(s, ts); tz = Lerp(0.5f, 0.12f, Mathf.Clamp01((tau - 0.8f) / 0.2f)); return; }
            th = TailTh(s, 3.4f); tz = 0.12f; if (ts < 4.3f) u = 1f - Smr((ts - 3.4f) / 0.9f); else u = 0f;
        }
        /// <summary>The tail as an arm from the pool: it comes straight up the centre (a stub), then rises as a column or topples over into a low arm that swings round (the lag makes it whip).</summary>
        static List<(float x, float y, float z)> Arm(Pool p, float u, float k, float th, float tz, float om, float R)
        {
            var l = new List<(float, float, float)>(); float h0 = 0.9f * Mathf.Clamp01(u * 5f); l.Add((p.X, p.Y, -2.5f)); l.Add((p.X, p.Y, h0));
            if (u < 0.01f) { l.Add((p.X, p.Y, h0 - 0.02f)); return l; }
            for (int j = 1; j <= 50; j++)
            {
                float r = j / 50f, thr = th - 0.1f * om * Mathf.Pow(1f - r, 1.4f), rho = R * u * r * k;
                float z = h0 * (1f - r) * (1f - r) + (1f - k) * 7.4f * u * r + k * (2.4f * u * Mathf.Sin(Mathf.PI * r) + tz * r * r);
                l.Add((p.X + rho * Mathf.Cos(thr), p.Y + rho * Mathf.Sin(thr), z));
            }
            return l;
        }
        static bool ArmNear(List<(float x, float y, float z)> arm, float x, float y, float r)
        { foreach (var a in arm) if (a.z > Surface && a.z < 2.6f && Dist(a.x, a.y, x, y) <= r) return true; return false; }

        // ---- ARCH: out of one pool, over the arena, down the big pool's centre; the whole body pours in after the head
        static void Arch(EelBoss.Fight f, State s, List<Player> players, float now, float ts)
        {
            var c = s.Cur; float L = c.L, sg;
            if (ts < 1.5f) sg = 7.5f * Smr(ts / 1.5f); else if (ts < 2.0f) sg = Lerp(7.5f, 8.5f, (ts - 1.5f) / 0.5f); else if (ts < 2.45f) sg = Lerp(8.5f, L + 1.5f, Ein((ts - 2.0f) / 0.45f, 2.4f)); else if (ts < 4.5f) sg = Lerp(L + 1.5f, L + 37f, Eout((ts - 2.45f) / 2.05f, 2.2f)); else sg = L + 37f;
            FillHead(s, c, sg); f.Zones.Clear();
            if (ts < 2.45f) { var path = new List<float>(); for (int i = 0; i <= 24; i++) { var q = c.At(L * (0.3f + 0.7f * i / 24f)); path.Add(q.x); path.Add(q.y); } f.Zones.Add(new EelBoss.Zone { X = s.Big.X, Y = s.Big.Y, R = 4.7f, Kind = "band", Live = false, Path = path.ToArray() }); }
            if (!s.RushSnd && ts >= 2.0f) { s.RushSnd = true; Snd("eel_rush", s.X[0], s.Y[0], 0.9f); }
            float hz = s.Z[0];
            if (!s.Crossed && s.PrevHZ > Surface && hz <= Surface && sg > 12f) { s.Crossed = true; f.Label = "ARCH: SLAM"; Impact(s, s.X[0], s.Y[0], 2.0f, true); Surge(f, s, players, s.Big, now); }
            s.PrevHZ = hz; if (ts >= 4.6f && !Rescue(f, s, now, ts, 4.6f)) End(f, s, now);
        }

        // ---- DASH: fully out of one pool, an S across the arena, down into another
        static void DashRun(EelBoss.Fight f, State s, List<Player> players, float now, float ts)
        {
            var c = s.Cur; float L = c.L, sg;
            if (ts < 1.5f) sg = 6f * Smr(ts / 1.5f); else if (ts < 1.95f) sg = Lerp(6f, 6.8f, (ts - 1.5f) / 0.45f); else if (ts < 4.5f) sg = Lerp(6.8f, L + 37f, Smr((ts - 1.95f) / 2.55f)); else sg = L + 37f;
            FillHead(s, c, sg); f.Zones.Clear();
            if (ts < 1.95f) { var path = new List<float>(); for (int i = 0; i <= 30; i++) { var q = c.At(5.5f + (L - 11f) * i / 30f); path.Add(q.x); path.Add(q.y); } f.Zones.Add(new EelBoss.Zone { X = s.P2.X, Y = s.P2.Y, R = 1f, Kind = "band", Live = false, Path = path.ToArray() }); }
            else if (ts < 4.5f) f.Label = "DASH!";                                                             // the body itself no longer hurts (Yan, 2026-11-06): the water it throws does the work, see Surge
            if (!s.RushSnd && ts >= 1.95f) { s.RushSnd = true; Snd("eel_rush", s.X[0], s.Y[0], 0.9f); }
            if (!s.Crossed && s.PrevHZ > Surface && s.Z[0] <= Surface && sg > c.L * 0.8f) { s.Crossed = true; Impact(s, s.X[0], s.Y[0], 0.8f, true); Surge(f, s, players, s.P2, now); }
            s.PrevHZ = s.Z[0]; if (ts >= 4.6f && !Rescue(f, s, now, ts, 4.6f)) End(f, s, now);
        }

        // ---- SURGE: the water a landing dive throws out (Yan, 2026-11-06: "the mass of water it displaces pushes the players back based on where they are and which pool it jumped into;
        // if players fall into the pool it does the second part of the grab attack, where it launches them up in the air with its tail"). Everyone within SurgeReach of the landing
        // pool's edge is shoved straight away from its centre, harder the closer they stand; no damage by itself. A player the shove carries into ANOTHER pool is caught by the tail
        // and thrown up and out, crashing where he stood (the snare's LAUNCH). Dive i-frames and people already in the air are skipped.
        const float SurgeReach = 9f, SurgeStep = 0.5f; const int FallDamage = 120;
        static bool DeepAt(float x, float y)
        {
            int tx = (int)Mathf.Floor(x), ty = (int)Mathf.Floor(y); if (tx < 0 || ty < 0 || tx >= TerrainMap.Width || ty >= TerrainMap.Height) return false;
            var t = TerrainMap.GetTile(tx, ty); return t.Type == TileType.Difficult && t.Look == "deepwater";
        }
        static Pool OwnerOf(State s, float x, float y) => s.Pools.OrderBy(o => ((x - o.X) / o.RX) * ((x - o.X) / o.RX) + ((y - o.Y) / o.RY) * ((y - o.Y) / o.RY)).First();   // which pool a deep tile belongs to (the pools are ellipses)
        public static float SurgePush(Pool dest, float x, float y)
        {
            float d = Dist(x, y, dest.X, dest.Y) - Mathf.Max(dest.RX, dest.RY);                            // from the pool's edge
            return d >= SurgeReach ? 0f : Lerp(7f, 1.5f, Mathf.Clamp01(d / SurgeReach));
        }
        static void Surge(EelBoss.Fight f, State s, List<Player> players, Pool dest, float now)
        {
            if (dest == null) return;
            foreach (var p in players)
            {
                if (Rt.Untouchable(p) || s.Air.ContainsKey(p)) continue;
                var q0 = Pos(p); float dx = q0.x - dest.X, dy = q0.y - dest.Y, d = Mathf.Sqrt(dx * dx + dy * dy) + 1e-3f, push = SurgePush(dest, q0.x, q0.y);
                if (push <= 0f) continue;
                float ux = dx / d, uy = dy / d; Pool into = null;
                for (int i = 0, n = Mathf.CeilToInt(push / SurgeStep); i < n; i++)
                {
                    var c = Pos(p); float nx = c.x + ux * SurgeStep, ny = c.y + uy * SurgeStep;
                    if (DeepAt(nx, ny)) { var own = OwnerOf(s, nx, ny); if (own == dest) break; into = own; break; }      // the landing pool's own rim just stops the shove; any other pool takes him
                    Rt.MoveBy(p, ux * SurgeStep, uy * SurgeStep); var c2 = Pos(p); if (Dist(c.x, c.y, c2.x, c2.y) < 0.1f) { if (DeepAt(c.x + ux * 0.6f, c.y + uy * 0.6f)) { var own2 = OwnerOf(s, c.x + ux * 0.6f, c.y + uy * 0.6f); if (own2 != dest) into = own2; } break; }      // a wall or a friend stops the shove
                }
                Rt.Of(p).ProneUntil = Mathf.Max(Rt.Of(p).ProneUntil, now + 0.8f);
                if (into == null) continue;
                Place(p, into.X, into.Y); Impact(s, into.X, into.Y, 1.0f, true); GameLog.Write("The wave drags you into the pool...");
                EelBoss.HurtPublic(p, FallDamage, "The wave drags you under", "Blunt"); s.Events.Add(new Ev { Kind = "hit", Who = Index(p), Dmg = FallDamage, Text = "pulled under", X = into.X, Y = into.Y });
                s.Air[p] = new Fling { X0 = into.X, Y0 = into.Y, Z0 = -3f, T0 = -1f, Dur = 0f, Land = "under" }; s.Pulled.Add((p, into, q0.x, q0.y));      // stays under until the dive is over
            }
        }

        // ---- RESCUE (Yan, 2026-11-06): a second after the dive is over, the TAIL comes out of the pool the wave dragged players into and throws them out (the snare's LAUNCH): up, spin, crash
        // where they stood. Runs inside the arch's and the dash's last seconds (they call it instead of ending); true while it is still going.
        static bool Rescue(EelBoss.Fight f, State s, float now, float ts, float diveEnd)
        {
            if (s.Pulled.Count == 0) return false;
            float tt = ts - (diveEnd + 1.0f);
            if (tt < 0f) return true;
            var first = s.Pulled[0]; var pool = first.pool; const float lx = 0.3f, ly = -0.3f;
            var wc = new Curve(Bez((pool.X, pool.Y, -3.6f), (pool.X, pool.Y, 2f), (pool.X + lx, pool.Y + ly, 5f), (pool.X + lx, pool.Y + ly, 8.2f)));
            float sg = tt < 0.45f ? wc.L * 0.97f * Ein(tt / 0.45f, 2f) : tt < 0.6f ? wc.L * 0.97f : tt < 1.6f ? wc.L * 0.97f * (1f - Smr((tt - 0.6f) / 1f)) : 0f;
            var rev = wc.Points(); rev.Reverse(); FillTail(s, new Curve(rev), -sg + wc.L);
            if (tt < 0.45f) { var tp = wc.At(sg); Place(first.p, tp.x, tp.y); s.Air[first.p] = new Fling { X0 = tp.x, Y0 = tp.y, Z0 = Mathf.Max(tp.z - 0.3f, -3f), T0 = -1f, Land = tp.z > 0f ? "carried" : "under" }; }
            if (!s.Threw && tt >= 0.45f)
            {
                s.Threw = true; var tipEnd = wc.At(wc.L * 0.97f); Impact(s, pool.X, pool.Y, 1.0f, true); f.Label = "THE TAIL THROWS YOU OUT";
                foreach (var e in s.Pulled) s.Air[e.p] = new Fling { X0 = tipEnd.x, Y0 = tipEnd.y, Z0 = tipEnd.z, X1 = e.x, Y1 = e.y, T0 = ts, Dur = 1.45f, Apex = 7.5f, Land = "launch", Spin = 720f };
            }
            if (tt >= 2.0f) { s.Pulled.Clear(); return false; }
            return true;
        }

        // ---- SNARE (phase 2): the tail out of a pool, a grab, a drag, a throw out of another pool (LAUNCH or HURL)
        const float Rim = 3.3f;
        static float ZProf(float d) => d >= Rim ? 0.45f : d > 0.15f ? 0.45f + 1.6f * Mathf.Sin(Mathf.PI * (Rim - d) / (Rim - 0.15f)) : 0.45f - (0.15f - d) / 0.15f * 4.05f;     // over the water on an arch, the dive at the centre
        static void StartSnare(EelBoss.Fight f, State s, List<Player> players, (float x, float y) tp, (float x, float y) t2)
        {
            var byReach = s.Pools.OrderBy(p => Mathf.Abs(Dist(p.X, p.Y, tp.x, tp.y) - 7f)).ToList(); s.P1 = byReach[0];
            bool two = players.Count >= 2 && s.Tg2 != s.Tg; var rest = s.Pools.Where(p => p != s.P1).ToList();
            s.P2 = two ? rest.OrderBy(p => Dist(p.X, p.Y, t2.x, t2.y)).First() : null;                    // the head's pool: it strikes at the other player meanwhile
            var left = rest.Where(p => p != s.P2).ToList();
            s.P3 = s.Variant == "hurl" ? left.OrderBy(p => Mathf.Abs(Dist(p.X, p.Y, t2.x, t2.y) - 9f)).First() : left.OrderByDescending(p => Dist(p.X, p.Y, tp.x, tp.y)).First();   // the throwing pool is a free one
            s.GP = tp; s.Tail.Hp = TailMax; s.TailBroken = false; s.TailDownUntil = -1f; f.Label = "SNARE (" + s.Variant + "): the tail surfaces";
            if (two) s.Hs = new Strike(s.P2, t2);
        }
        static float Dq(State s, float ts, float dG)
        {
            if (ts < 0.3f) return 0f; if (ts < 1.9f) return Rim * Smr((ts - 0.3f) / 1.6f);
            if (ts < 2.25f) return Lerp(Rim, dG, Ein((ts - 1.9f) / 0.35f, 2.2f)); if (ts < 2.75f) return dG;
            if (s.Missed || s.BreakTs >= 0f) { float t0 = s.BreakTs >= 0f ? s.BreakTs : 2.75f, q0 = s.BreakTs >= 0f ? s.BreakQ : dG; return Lerp(q0, 0f, Smr((ts - t0) / 0.9f)); }
            if (ts < 5.0f) return Lerp(dG, 0f, Ein((ts - 2.75f) / 2.25f, 1.5f)); return 0f;
        }
        static void SnareRun(EelBoss.Fight f, State s, List<Player> players, float now, float ts)
        {
            var pn = s.P1; float dx0 = s.GP.x - pn.X, dy0 = s.GP.y - pn.Y, dG = Mathf.Sqrt(dx0 * dx0 + dy0 * dy0) + 1e-3f, ux = dx0 / dG, uy = dy0 / dG;
            if (ts < 1.0f) { s.GP = Pos(s.Tg); }                                                                            // it keeps aiming until a second before the lunge
            if (s.TailBroken && s.BreakTs < 0f && ts < 6.2f) { s.BreakTs = ts; s.BreakQ = Dq(s, ts - 0.001f, dG); if (s.Snared != null) ReleaseSnared(s, now); s.EndAt = ts + 1.6f; f.Label = "SNARE: THE TAIL LETS GO"; }
            f.Zones.Clear(); if (ts > 0.3f && ts < 2.25f) Ring(f, s.GP.x, s.GP.y, 1.5f, false);
            // the head: it strikes at the other player from its own pool while the tail does its work
            if (s.Hs != null)
            {
                float hts = ts - 1.2f; if (ts >= 2.4f && s.Hs.Full == null) s.Hs.Lock(Pos(s.Tg2));
                FillHead(s, s.Hs.Cur, s.Hs.Sigma(hts)); if (s.Hs.Full != null && hts < 2.3f) Ring(f, s.Hs.T.x, s.Hs.T.y, 2.0f, false);
                if (!s.Hit2 && hts >= 2.3f && s.Hs.Full != null) { s.Hit2 = true; Impact(s, s.Hs.T.x, s.Hs.T.y, 1.0f, false); foreach (var p in players) if (p != s.Snared && Dist(Pos(p).x, Pos(p).y, s.Hs.T.x, s.Hs.T.y) <= 2.0f) Damage(s, p, 260, "The head slams down", s.Hs.T.x, s.Hs.T.y, 3f, 1.0f, now); }
            }
            else Hide(s, s.Big, 0, N / 2);
            // the tail
            if (ts < 6.2f)
            {
                float d = Dq(s, ts, dG), h = ts < 2.25f ? 0f : ts < 2.75f ? Smr((ts - 2.25f) / 0.5f) : (s.Missed || s.BreakTs >= 0f) ? 0f : ts < 5.0f ? 1f : 0f; if (d < 0.05f) { FillTail(s, new Curve(new List<(float, float, float)> { (pn.X, pn.Y, -3.6f), (pn.X, pn.Y, -4.6f) })); }
                else
                {
                    var pts = new List<(float x, float y, float z)>(); pts.Add((pn.X, pn.Y, -3.6f)); float px = -uy, py = ux;
                    for (int i = 1; i <= 60; i++) { float sv = i / 60f, dist = d * sv, off = 0.45f * Mathf.Sin(sv * 2f * Mathf.PI * 1.1f) * Mathf.Sin(sv * Mathf.PI); pts.Add((pn.X + ux * dist + px * off, pn.Y + uy * dist + py * off, ZProf(dist))); }
                    if (h > 0.02f) { var e = pts[pts.Count - 1]; for (int i = 1; i <= 30; i++) { float a = 2f * Mathf.PI * 1.6f * i / 30f, r = 0.62f * h * Mathf.Clamp01((d - 0.5f) / 1.2f); pts.Add((e.x + r * (Mathf.Cos(a + 1f) - Mathf.Cos(1f)), e.y + r * (Mathf.Sin(a + 1f) - Mathf.Sin(1f)), e.z + 0.15f + 0.55f * h * i / 30f)); } }
                    pts.Reverse(); FillTail(s, new Curve(pts));
                }
                var tip = (s.X[N - 1], s.Y[N - 1], s.Z[N - 1]);
                if (!s.Grabbed && !s.Missed && ts >= 2.25f)
                {
                    var q = Pos(s.Tg); if (Dist(q.x, q.y, tip.Item1, tip.Item2) <= 1.7f && !Rt.Untouchable(s.Tg)) { s.Grabbed = true; s.Snared = s.Tg; f.Label = "SNARE: CAUGHT"; Impact(s, tip.Item1, tip.Item2, 0.55f, false); Rt.Of(s.Tg).DiveUntil = -1f; }
                    else { s.Missed = true; f.Label = "SNARE: missed"; Impact(s, tip.Item1, tip.Item2, 0.55f, false); s.EndAt = ts + 1.4f; }
                }
                if (s.Snared != null)
                {
                    var p = s.Snared; Rt.Of(p).ProneUntil = now + 0.25f;
                    if (tip.Item3 > 0f) { Place(p, tip.Item1, tip.Item2); s.Air[p] = new Fling { X0 = tip.Item1, Y0 = tip.Item2, Z0 = Mathf.Max(tip.Item3, 0.3f), T0 = -1f, Dur = 0f, Land = "carried" }; f.Label = ts < 2.75f ? "SNARE: coiled round him" : "SNARE: DRAGGED - shoot the tail!"; }
                    else { s.Air[p] = new Fling { X0 = pn.X, Y0 = pn.Y, Z0 = -3f, T0 = -1f, Dur = 0f, Land = "under" }; Place(p, pn.X, pn.Y); if (!s.Crossed) { s.Crossed = true; Impact(s, pn.X, pn.Y, 0.8f, true); f.Label = "SNARE: pulled under"; } }
                }
            }
            else if (s.Missed || s.BreakTs >= 0f) Hide(s, s.Big, N / 2, N);
            else
            {
                var tp = s.P3; float lx = 0f, ly = 0f; (float x, float y) land = s.GP;
                if (s.Variant == "hurl") { land = Pos(s.Tg2 == s.Snared ? s.Tg : s.Tg2); lx = 0.4f * (land.x - tp.X) / 9f; ly = 0.4f * (land.y - tp.Y) / 9f; } else { lx = 0.3f; ly = -0.3f; }
                var wc = new Curve(Bez((tp.X, tp.Y, -3.6f), (tp.X, tp.Y, 2f), (tp.X + lx, tp.Y + ly, 5f), (tp.X + lx, tp.Y + ly, 8.2f)));
                float sg = ts < 6.5f ? 0f : ts < 6.95f ? wc.L * 0.97f * Ein((ts - 6.5f) / 0.45f, 2f) : ts < 7.1f ? wc.L * 0.97f : ts < 8.1f ? wc.L * 0.97f * (1f - Smr((ts - 7.1f) / 1f)) : 0f;
                var rev = wc.Points(); rev.Reverse(); FillTail(s, new Curve(rev), -sg + wc.L); if (s.BreakTs >= 0f) { }
                if (ts < 6.95f && s.Snared != null) { var p = s.Snared; Rt.Of(p).ProneUntil = now + 0.25f; var t = wc.At(sg); Place(p, t.x, t.y); s.Air[p] = new Fling { X0 = t.x, Y0 = t.y, Z0 = Mathf.Max(t.z - 0.3f, -3f), T0 = -1f, Land = t.z > 0f ? "carried" : "under" }; if (ts > 6.0f && ts < 6.95f) Ring(f, tp.X, tp.Y, 2.2f, false); }
                if (!s.Threw && ts >= 6.95f && s.Snared != null)
                {
                    s.Threw = true; var p = s.Snared; var tipEnd = wc.At(wc.L * 0.97f); Impact(s, tp.X, tp.Y, 1.0f, true); f.Label = s.Variant == "hurl" ? "SNARE: HURLED at the other player" : "SNARE: THROWN into the air";
                    if (s.Variant == "hurl") { s.Air[p] = new Fling { X0 = tipEnd.x, Y0 = tipEnd.y, Z0 = tipEnd.z, X1 = land.x, Y1 = land.y, T0 = ts, Dur = 0.8f, Apex = 3f, Land = "hurl", Spin = 540f, Hurled = s.Tg2 == p ? s.Tg : s.Tg2 }; }
                    else { var lp = TerrainMap.FindNearestEnterable((int)Mathf.Floor(tp.X + (s.Big.X - tp.X) * 0.5f), (int)Mathf.Floor(tp.Y + (s.Big.Y - tp.Y) * 0.5f)); s.LP = (lp.x + 0.5f, lp.y + 0.5f); s.Air[p] = new Fling { X0 = tipEnd.x, Y0 = tipEnd.y, Z0 = tipEnd.z, X1 = s.LP.x, Y1 = s.LP.y, T0 = ts, Dur = 1.45f, Apex = 7.5f, Land = "launch", Spin = 720f }; }
                    s.Snared = null;
                }
                if (s.Variant != "hurl" && ts >= 6.0f && ts < 8.4f && s.LP.x > 0f) Ring(f, s.LP.x, s.LP.y, 1.3f, false);
            }
            if (s.EndAt > 0f && ts >= s.EndAt || ts >= 9.0f) End(f, s, now);
        }
        static void ReleaseSnared(State s, float now)
        {
            var p = s.Snared; if (p == null) return; var q = Pos(p); s.Snared = null; s.Air[p] = new Fling { X0 = q.x, Y0 = q.y, Z0 = 0.6f, X1 = q.x, Y1 = q.y, T0 = -2f, Dur = 0.35f, Apex = 0.3f, Land = "drop" };
        }

        // ---- players in the air (thrown, hurled, dropped, flung by the flood)
        static void Fly(EelBoss.Fight f, State s, List<Player> players, float now)
        {
            float ts = (now - f.StageAt) * Speed;
            foreach (var kv in s.Air.ToList())
            {
                var p = kv.Key; var a = kv.Value;
                if (a.T0 == -2f) a.T0 = ts;                                                                     // a drop starts now
                if (a.Dur <= 0f) { a.Z = a.Z0; a.Rot = 25f * Mathf.Sin(now * 9f); if (a.Land == "carried" || a.Land == "under") { if (f.Move != "snare" && f.Stage != "move") s.Air.Remove(p); } continue; }
                float u = Mathf.Clamp01((ts - a.T0) / a.Dur); float x = Lerp(a.X0, a.X1, u), y = Lerp(a.Y0, a.Y1, u), z = a.Z0 * (1f - u) + a.Apex * 4f * u * (1f - u);
                Place(p, x, y); Rt.Of(p).ProneUntil = now + 0.25f; a.Z = z; a.Rot = a.Spin * u;
                if (u >= 1f)
                {
                    s.Air.Remove(p); Rt.Of(p).ProneUntil = now + (a.Land == "launch" ? 2.0f : 1.2f);
                    if (a.Land == "launch") { Impact(s, a.X1, a.Y1, 1.3f, false); EelBoss.HurtPublic(p, 420, "You crash down, half drowned", "Blunt"); s.Events.Add(new Ev { Kind = "hit", Who = Index(p), Dmg = 420, Text = "drowned", X = a.X1, Y = a.Y1 }); }
                    else if (a.Land == "hurl") { Impact(s, a.X1, a.Y1, 1.0f, false); EelBoss.HurtPublic(p, 260, "You crash into your friend", "Blunt"); s.Events.Add(new Ev { Kind = "hit", Who = Index(p), Dmg = 260, Text = "hurled", X = a.X1, Y = a.Y1 }); if (a.Hurled != null && Dist(Pos(a.Hurled).x, Pos(a.Hurled).y, a.X1, a.Y1) <= 1.9f) Damage(s, a.Hurled, 210, "A flying friend flattens you", a.X0, a.Y0, 4f, 1.2f, now); }
                    else if (a.Land == "drop") { EelBoss.HurtPublic(p, 40, "The tail drops you", "Blunt"); }
                }
            }
        }

        // ---- FLOOD (phase 2): the floor between the pools turns to deep water, the eel glides through as a shadow and bursts out under someone
        static void ScanShallow(State s)                                                                   // the knee-deep water the flood can take (and the storm electrifies): everything that is not a dry patch
        {
            s.Shallow = new List<(int, int, float)>(); s.Flip.Clear(); s.Wet.Clear();
            for (int y = 9; y <= 30; y++) for (int x = 5; x <= 54; x++)
            { var t = TerrainMap.GetTile(x, y); if (t.Type == TileType.Difficult && t.Look == null) { float d = 99f; foreach (var p in s.Pools) d = Mathf.Min(d, Dist(x + 0.5f, y + 0.5f, p.X, p.Y) - (p.RX + p.RY) / 2f); s.Shallow.Add((x, y, d)); s.Wet.Add((x, y)); } }
        }
        static void StartFlood(EelBoss.Fight f, State s, List<Player> players, (float x, float y) tp)
        {
            ScanShallow(s);
            s.P1 = s.Pools.Where(p => !p.Big).OrderByDescending(p => Dist(p.X, p.Y, tp.x, tp.y)).First(); s.GP = tp; f.Label = "FLOOD: the pools swell";
        }
        // it goes for someone standing in the water: the islands are safe from this move
        static bool InFlood(State s, Player p) { var q = Pos(p); return s.Flip.Contains(((int)Mathf.Floor(q.x), (int)Mathf.Floor(q.y))); }
        static float FloodRadius(float ts) => ts < 1.5f ? 0f : ts < 3.1f ? 17f * Smr((ts - 1.5f) / 1.6f) : ts < 6.4f ? 17f : ts < 7.7f ? 17f * (1f - Smr((ts - 6.4f) / 1.3f)) : 0f;
        // The flood is an OVERLAY (Yan, 2026-11-06: "the flood attack still freezes the game a bit, especially when the water recedes ... if caught in the water the player can't move"):
        // it used to flip hundreds of tiles to LabLocks' "deepwater" look every frame, which (a) made the view repaint the ground, standing sprites and lights for each one - the freeze - and
        // (b) made them LOCKED tiles nobody can enter, so a player caught in it could not move at all. Now the tiles stay what they were (knee-deep, slow, passable); only the Flip set changes
        // (who is "in the water" for the moves) and FloodVer counts the changes so the view can redraw one small texture (CombatFxView.Eel.cs DrawFlood).
        static void FloodTiles(State s, float r)
        {
            bool changed = false;
            foreach (var (x, y, d) in s.Shallow)
            {
                bool want = d <= r && r > 0.05f, has = s.Flip.Contains((x, y));
                if (want && !has) { s.Flip.Add((x, y)); changed = true; }
                else if (!want && has) { s.Flip.Remove((x, y)); changed = true; }
            }
            if (changed) s.FloodVer++;
            s.Flooded = s.Flip.Count; s.FloodR = r;
        }
        static void Restore(State s, bool all) { if (s.Flip.Count > 0 && all) FloodTiles(s, 0f); s.FloodR = 0f; }
        static void FloodRun(EelBoss.Fight f, State s, List<Player> players, float now, float ts)
        {
            FloodTiles(s, FloodRadius(ts)); f.Zones.Clear();
            if (ts < 1.5f) { f.Label = "FLOOD: the pools swell, the dry patches light up"; Hide(s, s.Big); return; }
            if (!s.FloodSnd && ts >= 1.5f) { s.FloodSnd = true; Snd("eel_flood", s.P1.X, s.P1.Y, 0.9f); }
            if (ts < 3.1f) f.Label = "FLOOD: THE FLOOR TURNS TO DEEP WATER";
            if (s.Cur == null && ts >= 3.0f)      // lock the swim at 3.0 s: from the far pool, through the flood, to where the nearest player stands
            {
                var q = players.OrderBy(p => InFlood(s, p) ? 0 : 1).ThenBy(p => Dist(Pos(p).x, Pos(p).y, s.P1.X, s.P1.Y)).First(); var t = Pos(q); s.GP = t; s.Tg = q; s.NoStrike = !InFlood(s, q); float dx = t.x - s.P1.X, dy = t.y - s.P1.Y, d = Mathf.Sqrt(dx * dx + dy * dy), ux = dx / d, uy = dy / d, px = -uy, py = ux;
                var pts = new List<(float x, float y, float z)> { (s.P1.X, s.P1.Y, -3.2f), (s.P1.X, s.P1.Y, 0.05f) };                                    // up the centre to the surface, then it glides
                int m = 120; for (int i = 1; i <= m; i++) { float sv = i / (float)m, dist = 1.0f + (d - 5.5f) * sv; pts.Add((s.P1.X + ux * dist + px * 0.9f * Mathf.Sin(sv * Mathf.PI * 3f), s.P1.Y + uy * dist + py * 0.9f * Mathf.Sin(sv * Mathf.PI * 3f), 0.05f)); }
                s.Cur2 = new Curve(pts); var e = pts[pts.Count - 1]; pts.AddRange(Bez(e, (e.x + 0.7f * ux, e.y + 0.7f * uy, 6.5f), (t.x - 0.9f * ux, t.y - 0.9f * uy, 7.2f), (t.x, t.y, 0.3f), 40).Skip(1)); s.Cur = new Curve(pts);
            }
            if (s.Cur == null || ts < 3.1f) { Hide(s, s.Big); return; }
            float L = s.Cur.L, Lsw = s.Cur2.L, sg;
            if (ts < 4.7f) sg = Lsw * Smr((ts - 3.1f) / 1.6f); else if (ts < 5.5f) sg = Lsw; else if (ts < 6.1f) sg = Lsw + 0.42f * (L - Lsw) * Smr((ts - 5.5f) / 0.6f); else if (ts < 6.3f) sg = Lerp(Lsw + 0.42f * (L - Lsw), L, Ein((ts - 6.1f) / 0.2f, 2.6f)); else if (ts < 7.0f) sg = L; else if (ts < 8.1f) sg = L * (1f - Smr((ts - 7.0f) / 1.1f)); else sg = 0f;
            FillHead(s, s.Cur, sg);
            if (ts < 6.3f && ts > 4.4f) Ring(f, s.GP.x, s.GP.y, 2.2f, false);
            if (ts < 4.7f) f.Label = "FLOOD: it swims through as a shadow"; else if (ts < 6.1f) f.Label = "FLOOD: it rears up under him";
            if (!s.Hit1 && ts >= 6.3f && s.NoStrike) { s.Hit1 = true; f.Label = "FLOOD: nobody in the water - it rears up and sinks back"; }
            if (!s.Hit1 && ts >= 6.3f) { s.Hit1 = true; f.Label = "FLOOD: BURSTS OUT"; Impact(s, s.GP.x, s.GP.y, 1.3f, true); foreach (var p in players) if (Dist(Pos(p).x, Pos(p).y, s.GP.x, s.GP.y) <= 2.4f && Damage(s, p, 380, "It bursts out under you", s.GP.x, s.GP.y, 0f, 1.5f, now)) { var q = Pos(p); s.Air[p] = new Fling { X0 = q.x, Y0 = q.y, Z0 = 0.4f, X1 = q.x - 2.6f, Y1 = q.y + 2.8f, T0 = ts, Dur = 0.9f, Apex = 3.0f, Land = "fling", Spin = 400f }; } }
            if (ts >= 8.2f) End(f, s, now);
        }

        // ---- STORM (phase 3, Yan 2026-11-06; the 50% transition, then one of the moves): the arena floods, the eel comes up out of a pool and charges for ~4 s with sparks all round it, then it
        // "explodes", electrifying all the water of the map: anyone not on a dry patch (an island, the slab, a platform) is shocked. THEN the BITE (chained): it turns on a player and opens its mouth
        // slowly (2.8 s), then springs. A player who presses the parry key just before it lands PARRIES it bare-handed (anyone can) and holds the jaws open: mash the parry key (presses that outpace its push-back, within
        // MashSeconds, slow motion for everyone, the others can keep shooting) to shut its mouth and slam its head down - it is stunned for StunSeconds (slumped on the ground), then wakes, shakes its head and
        // sinks. Fail the mash, or never parry, and the jaws close: BiteDamage, then it throws him like the tail's LAUNCH. A dive through the lunge (i-frames), or stepping away, makes it miss.
        // Wind-ups all the way: the pools swell, the floor floods, it rears up, 4 s of sparks, the mouth slowly opening.
        const float StormCharge = 4f, StormDamage = 450f, BiteOpen = 2.8f, BiteCock = 0.8f, StopFront = 3.4f, StopHold = 3.9f, BiteLunge = 0.4f, BiteWindow = 1.2f, MashSeconds = 6f, StunSeconds = 4f;
        const int SlamDamage = 1800, BiteDamage = 200;
        const float GripPerPress = 1f / 30f, GripDecay = 0.06f;   // 30 presses fill it (Yan), the eel pushes back 6% a second: ~3 presses a second to make headway, ~8 to win inside MashSeconds;   // each press shuts the jaws a little (twitch), and the eel pushes them back open between presses: it takes ~6 presses a second to win in MashSeconds
        static void StartStorm(EelBoss.Fight f, State s, List<Player> players)
        {
            ScanShallow(s); s.P1 = (s.Variant != null ? s.Pools.FirstOrDefault(q => q.Name == s.Variant) : null) ?? Pick(s.Pools); var p = s.P1;      // "storm:big" etc. names the pool (the harness and the clips)
            s.Cur = new Curve(Bez((p.X, p.Y, -3.6f), (p.X, p.Y, 2.5f), (p.X + 0.5f, p.Y + 0.3f, 4.6f), (p.X + 0.9f, p.Y + 0.4f, 6.2f)));
            f.Label = "STORM: the pools swell";
        }
        /// <summary>The parry key went down while someone is holding the eel's jaws: it is a mash press (Parry.Press).</summary>
        public static bool GrappleInput(Player p)
        {
            var f = EelBoss.Current; if (f == null || f.Sump == null || f.Gone) return false; var s = f.Sump;
            if (s.Bite != "grapple" || s.Victim != p) return false; s.Mash++; s.Grip = Mathf.Min(1f, s.Grip + GripPerPress); s.Twitch = 1f; if (s.Grip >= 1f) s.Won = true; Snd("eel_strain", s.BiteSpot.x, s.BiteSpot.y, 0.8f); return true;
        }
        /// <summary>For the view: how open the mouth is (-1 = not controlled: the head as he drew it; 0 shut .. 1 as drawn .. 1.4 forced wide), and the twitch of the last press (1 .. 0).</summary>
        public static float MouthOpen(EelBoss.Fight f) => f != null && f.Sump != null ? f.Sump.Mouth : -1f;
        public static bool Gripping(Player p) { var f = EelBoss.Current; return p != null && f != null && f.Sump != null && !f.Gone && (f.Sump.Bite == "grapple" || f.Sump.Bite == "lift" || f.Sump.Bite == "smash") && f.Sump.Victim == p; }
        /// <summary>While someone holds the eel's jaws: where (tile space), for the camera to close in on or highlight.</summary>
        public static bool GrappleSpot(out float x, out float y) { var f = EelBoss.Current; x = y = 0f; if (f == null || f.Sump == null || f.Gone || (f.Sump.Bite != "grapple" && f.Sump.Bite != "lift" && f.Sump.Bite != "smash")) return false; x = f.Sump.BiteSpot.x; y = f.Sump.BiteSpot.y; return true; }
        public static float GripOf(EelBoss.Fight f) => f != null && f.Sump != null ? f.Sump.Grip : 0f;
        /// <summary>True from the glint until the bite lands: the moment to parry (the HUD shouts it).</summary>
        public static bool ParryCue(EelBoss.Fight f) => f != null && f.Sump != null && f.Sump.GlintSnd && (f.Sump.Bite == "cock" || f.Sump.Bite == "lunge");
        /// <summary>Slumped on the ground after the slam (the view dims it and circles its head with stars).</summary>
        /// <summary>While it is held: which way the head must face - +1 = to the right (he is on its right), -1 = to the left, 0 = free (the view picks it from the neck).</summary>
        public static int GrabFace(EelBoss.Fight f) { if (f == null || f.Sump == null || (f.Sump.Bite != "grapple" && f.Sump.Bite != "lift" && f.Sump.Bite != "smash")) return 0; return f.Sump.VictimPos.x >= f.Sump.X[0] ? 1 : -1; }
        public static bool Stunned(EelBoss.Fight f) => f != null && f.Sump != null && f.Sump.Bite == "slump";
        public static float ChargeOf(EelBoss.Fight f) => f != null && f.Sump != null ? f.Sump.Charge : 0f;
        public static float RearOf(EelBoss.Fight f) => f != null && f.Sump != null ? f.Sump.Rear : 0f;
        public static float TwitchOf(EelBoss.Fight f) => f != null && f.Sump != null ? f.Sump.Twitch : 0f;
        static void ThrowBitten(EelBoss.Fight f, State s, Player v, float now, float ts)
        {
            s.Air.Remove(v); var sp = s.BiteSpot; Damage(s, v, BiteDamage, "The jaws close on you", sp.x, sp.y, 0f, 0.5f, now);
            float ux = 30.5f - sp.x, uy = 20f - sp.y, d = Mathf.Sqrt(ux * ux + uy * uy) + 1e-3f; var lp = TerrainMap.FindNearestEnterable((int)Mathf.Floor(sp.x + 5f * ux / d), (int)Mathf.Floor(sp.y + 5f * uy / d));
            s.Air[v] = new Fling { X0 = sp.x, Y0 = sp.y, Z0 = 0.6f, X1 = lp.x + 0.5f, Y1 = lp.y + 0.5f, T0 = ts, Dur = 1.45f, Apex = 7.5f, Land = "launch", Spin = 720f };
            f.Label = "BITE: it grabs you and throws you!"; s.Bite = "recover"; s.BiteT = ts; s.Mouth = 0.4f;
        }
        static void BiteContact(EelBoss.Fight f, State s, List<Player> players, float now, float ts)
        {
            var sp = s.BiteSpot; Impact(s, s.X[0], s.Y[0], 1.0f, false); Snd("eel_bite", s.X[0], s.Y[0]);
            var v = players.Where(p => !Rt.Untouchable(p) && !s.Air.ContainsKey(p) && Dist(Pos(p).x, Pos(p).y, sp.x, sp.y) <= 1.8f).OrderBy(p => Dist(Pos(p).x, Pos(p).y, sp.x, sp.y)).FirstOrDefault();
            if (v == null) { f.Label = "BITE: it misses and slams the ground"; s.Bite = "recover"; s.BiteT = ts; s.Mouth = 0.3f; return; }
            float since = now - Rt.Of(v).GrabAt;
            if (since >= 0f && since <= BiteWindow)                                                                    // parried: he holds the jaws
            {
                s.Bite = "grapple"; s.Victim = v; s.Mash = 0; s.Grip = 0f; s.Twitch = 0f; s.Won = false; s.GrabStart = Time.unscaledTime; s.GrabLastU = Time.unscaledTime; s.GrabLast = now; s.Mouth = 1.4f;
                var vp = Pos(v); s.VictimPos = vp; s.Parried = true;
                s.GrabShift = (vp.x + s.BiteSide * StopHold - s.BiteEnd.x, vp.y - s.BiteEnd.y);                                              // stopped dead in front of him, level with him: the jaw tips (3.2 tiles from the head's anchor, to the side) are 0.7 tiles from his hands; he stays where he stands
                s.Air[v] = new Fling { X0 = vp.x, Y0 = vp.y, Z0 = 0f, T0 = -1f, Dur = 0f, Land = "grip" }; f.Label = "BITE: PARRIED! Hold its jaws - mash the parry key!";
                s.Events.Add(new Ev { Kind = "parry", X = sp.x, Y = sp.y, Power = 1f, Who = Index(v) }); Boss.Hold(MashSeconds + 0.5f);
            }
            else { s.Victim = v; ThrowBitten(f, s, v, now, ts); }
        }
        static void StormRun(EelBoss.Fight f, State s, List<Player> players, float now, float ts)
        {
            float rise = 3.1f, hold = rise + 1.2f, burst = hold + StormCharge;                                              // 3.1 rises, 4.3 charging, 8.3 the burst, then the bite
            float fr = ts < 1.5f ? 0f : ts < 3.1f ? 17f * Smr((ts - 1.5f) / 1.6f) : 17f; if (s.SinkT >= 0f) fr = 17f * (1f - Smr((ts - s.SinkT) / 1.3f));
            FloodTiles(s, fr); f.Zones.Clear(); var c = s.Cur; float L0 = c.L * 0.97f; s.Rear = 0f; s.Charge = 0f;
            if (!s.FloodSnd && ts >= 1.5f) { s.FloodSnd = true; Snd("eel_flood", s.P1.X, s.P1.Y, 0.9f); }
            if (s.Bite == null)
            {
                s.StormSg = ts < rise ? 0f : ts < hold ? L0 * Smr((ts - rise) / 1.2f) : L0;
                if (ts < 1.5f) f.Label = "STORM: the pools swell, the dry patches light up"; else if (ts < rise) f.Label = "STORM: the floor turns to deep water"; else if (ts < hold) f.Label = "STORM: it rears up out of the water";
                else
                {
                    f.Label = "STORM: it is charging - get onto dry ground!";
                    float cu = Mathf.Clamp01((ts - hold) / StormCharge); s.Charge = cu; s.SkyK = 0.85f * cu; s.Rear = 10f + 55f * cu;                                  // it throws its head up and twitches harder as it fills
                    s.Twitch = Rng.NextDouble() < 0.2 + 0.5 * cu ? 0.35f + 0.65f * cu * (float)Rng.NextDouble() : s.Twitch * 0.7f;
                    if (ts >= s.SparkAt && ts < burst) { if (s.SparkAt <= 0f) Snd("eel_charge", s.X[0], s.Y[0]); s.SparkAt = ts + 0.1f; s.Events.Add(new Ev { Kind = "spark", X = s.X[0], Y = s.Y[0], Power = cu }); }
                }
                if (ts >= burst)
                {
                    f.Label = "STORM: THE WATER IS ELECTRIC"; Snd("eel_burst", s.X[0], s.Y[0], 1.2f); s.Events.Add(new Ev { Kind = "storm", X = s.X[0], Y = s.Y[0], Power = 1.5f, Water = true });
                    foreach (var p in players) { var q = Pos(p); if (s.Wet.Contains(((int)Mathf.Floor(q.x), (int)Mathf.Floor(q.y)))) Damage(s, p, (int)StormDamage, "The water electrocutes you", s.X[0], s.Y[0], 0f, 1.2f, now); }
                    s.Bite = "scream"; s.BiteT = ts; s.Victim = s.ForcedVictim ?? Pick(players); Snd("eel_scream", s.X[0], s.Y[0], 1.3f);       // it lets the charge go with a scream
                }
            }
            else switch (s.Bite)
            {
                case "scream":                                                                                            // head thrown back, jaws wide, shaking, while the water goes electric
                    s.Charge = 1f; s.SkyK = 1f; s.Mouth = 1.4f; s.Rear = 84f + 6f * Mathf.Sin(ts * 40f); s.Twitch = 0.55f + 0.3f * Mathf.Sin(ts * 55f); f.Label = "STORM: IT SCREAMS - the water is electric!";
                    if (ts - s.BiteT >= 0.9f) { s.Bite = "open"; s.BiteT = ts; s.Mouth = 0f; Snd("eel_mouth", s.X[0], s.Y[0]); }
                    break;
                case "open":                                                                                              // it rears back like a snake, the mouth opening slowly
                    s.Charge = Mathf.Clamp01(1f - (ts - s.BiteT) / 0.8f); s.SkyK = Mathf.Clamp01(1f - (ts - s.BiteT) / 0.5f); s.Mouth = Mathf.Clamp01((ts - s.BiteT) / BiteOpen); s.Rear = 14f + 36f * Smr(Mathf.Clamp01((ts - s.BiteT) / BiteOpen)); f.Label = "BITE: it turns on " + (s.Victim == Player.Current ? "you" : "a player") + " and opens its mouth";
                    if (s.BiteCur == null && ts - s.BiteT >= BiteOpen - 0.8f)                                              // the aim locks 0.8 s before it springs
                    {
                        var q = Pos(s.Victim); s.BiteSpot = q; var pts = new List<(float x, float y, float z)>(c.Points()); var a = pts[pts.Count - 1];
                        s.BiteSide = a.x >= q.x ? 1f : -1f; float ex = q.x + s.BiteSide * StopFront, ey = q.y; s.BiteEnd = (ex, ey);                    // the head is a side profile: it ends level with him, on its pool's side, with its snout (3.2 tiles from the anchor) at him
                        float dx = ex - a.x, dy = ey - a.y;
                        pts.AddRange(Bez(a, (a.x + 0.18f * dx, a.y + 0.18f * dy, a.z + 1.6f), (ex - 0.2f * dx, ey - 0.2f * dy, 3.0f), (ex, ey, 0.45f)).Skip(1)); s.BiteCur = new Curve(pts); s.BiteFrom = (a.x, a.y);
                    }
                    if (ts - s.BiteT >= BiteOpen) { s.Bite = "cock"; s.BiteT = ts; }
                    break;
                case "cock":                                                                                              // fully reared back, trembling: the tell. A white glint: parry NOW
                    s.Mouth = 1.2f; s.Rear = 58f; s.Twitch = 0.45f + 0.25f * Mathf.Sin(ts * 50f); f.Label = "BITE: it is about to strike - PARRY on the glint!";
                    if (!s.GlintSnd && ts - s.BiteT >= 0.5f) { s.GlintSnd = true; s.Events.Add(new Ev { Kind = "glint", X = s.X[0], Y = s.Y[0], Power = 1f }); Snd("glint", s.X[0], s.Y[0], 0.9f); }
                    if (ts - s.BiteT >= BiteCock) { s.Bite = "lunge"; s.BiteT = ts; f.Label = "BITE: it springs!"; }
                    break;
                case "lunge":
                    { float u = Mathf.Clamp01((ts - s.BiteT) / BiteLunge); s.StormSg = Lerp(L0, s.BiteCur.L, Ein(u, 2.2f)); if (u >= 1f) BiteContact(f, s, players, now, ts); }
                    break;
                case "grapple":                                                                                           // the scene: the clock stands still, the victim holds the jaws and fights them shut
                    {
                        f.StageAt += now - s.GrabLast; s.GrabLast = now; var v = s.Victim; Place(v, s.VictimPos.x, s.VictimPos.y);
                        float du = Time.unscaledTime - s.GrabLastU; s.GrabLastU = Time.unscaledTime;
                        s.Grip = Mathf.Max(0f, s.Grip - GripDecay * du); s.Twitch = Mathf.Max(0f, s.Twitch - 7f * du); s.Twitch = Mathf.Max(s.Twitch, 0.4f + 0.2f * Mathf.Sin(Time.unscaledTime * 27f)); s.Mouth = Lerp(1.4f, 0f, s.Grip);      // the mouth IS the progress: every press closes it a little, it creeps back open
                        if (s.Won) { s.Bite = "lift"; s.BiteU = Time.unscaledTime; s.Mouth = 0f; Boss.Hold(1.3f); Snd("eel_rush", s.BiteSpot.x, s.BiteSpot.y, 0.7f); f.Label = "THE JAWS SHUT - it heaves its head up..."; }                        else if (Time.unscaledTime - s.GrabStart > MashSeconds) { Boss.Release(); s.BiteSpot = s.VictimPos; ThrowBitten(f, s, v, now, ts); }
                    }
                    break;
                case "lift":                                                                                              // the jaws are shut: the head is heaved up and back (real time, under the slow motion)
                    { var v = s.Victim; Place(v, s.VictimPos.x, s.VictimPos.y); s.Mouth = 0f; s.Twitch = 0.5f; s.Rear = 20f + 25f * Smr(Mathf.Clamp01((Time.unscaledTime - s.BiteU) / 0.7f));
                      if (Time.unscaledTime - s.BiteU >= 0.7f) { s.Bite = "smash"; s.BiteU = Time.unscaledTime; f.Label = "AND SLAMS IT DOWN!"; } }
                    break;
                case "smash":                                                                                             // ... and driven into the ground: the moment it lands is the slam
                    {
                        var v = s.Victim; Place(v, s.VictimPos.x, s.VictimPos.y); s.Mouth = 0f;
                        if (Time.unscaledTime - s.BiteU >= 0.16f)
                        {
                            Boss.Release(); s.Air.Remove(v); Impact(s, s.X[0], s.Y[0], 2.4f, false); s.Events.Add(new Ev { Kind = "slam", X = s.X[0], Y = s.Y[0], Power = 2.4f, Who = Index(v) });
                            f.Unit.Hp = System.Math.Max(2, f.Unit.Hp - SlamDamage); Boss.OnDamaged(f.Unit);
                            s.Bite = "slump"; s.BiteT = ts; s.StunUntil = now + StunSeconds; Snd("eel_slam", s.X[0], s.Y[0], 1.3f); f.Label = "IT IS STUNNED - slumped on the ground!";
                        }
                    }
                    break;
                case "slump": if (now >= s.StunUntil) { s.Bite = "wake"; s.BiteT = ts; f.Label = "It wakes and shakes its head"; Snd("eel_wake", s.BiteSpot.x, s.BiteSpot.y); } break;
                case "wake": if (ts - s.BiteT >= 0.9f) { s.Bite = "sink"; s.BiteT = ts; s.SinkT = ts; s.SinkFrom = s.StormSg; } break;
                case "recover": if (ts - s.BiteT >= 1.0f) { s.Bite = "sink"; s.BiteT = ts; s.SinkT = ts; s.SinkFrom = s.StormSg; } break;
                case "sink": s.StormSg = s.SinkFrom * (1f - Smr((ts - s.BiteT) / 1.4f)); if (ts - s.BiteT >= 1.6f) End(f, s, now); break;
            }
            if (f.Move == "storm")
            {
                FillHead(s, s.BiteCur != null && s.Bite != null ? s.BiteCur : c, s.StormSg);
                if (s.SkyK > 0.01f)                                                                                // thrown up and back, screaming at the sky
                {
                    float sx0 = s.X[0] - 30.5f, sy0 = s.Y[0] - 20f, sl = Mathf.Sqrt(sx0 * sx0 + sy0 * sy0) + 1e-3f;
                    for (int i = 0; i < 40; i++) { float w = Mathf.Pow(1f - i / 40f, 3f) * s.SkyK; s.X[i] += sx0 / sl * 1.8f * w; s.Y[i] += sy0 / sl * 1.8f * w; s.Z[i] += 1.6f * w; }
                }
                if (s.Parried && (s.Bite == "grapple" || s.Bite == "lift" || s.Bite == "smash" || s.Bite == "slump" || s.Bite == "wake" || s.Bite == "sink"))      // stopped short in front of him (the parry): the head holds where its jaw tips are at his hands, through the slam and the stun, and eases back as it sinks
                {
                    float gk = s.Bite == "sink" ? 1f - Smr(Mathf.Clamp01((ts - s.BiteT) / 1.4f)) : Smr(Mathf.Clamp01((Time.unscaledTime - s.GrabStart) / 0.18f));
                    for (int i = 0; i < 48; i++) { float w = (i < 20 ? 1f : Mathf.Pow(1f - (i - 20) / 28f, 2f)) * gk; s.X[i] += s.GrabShift.sx * w; s.Y[i] += s.GrabShift.sy * w; }
                }
                if ((s.Bite == "open" || s.Bite == "cock" || s.Bite == "lunge" || s.Bite == "lift" || s.Bite == "smash") && s.Victim != null)                      // the head and neck are drawn back, away from the target, and up: an S-curve like a snake about to strike
                {
                    var q = s.Bite == "lift" || s.Bite == "smash" ? (x: 30.5f, y: 20f) : Pos(s.Victim); float dx = s.X[0] - q.x, dy = s.Y[0] - q.y, l = Mathf.Sqrt(dx * dx + dy * dy) + 1e-3f;
                    float k = s.Bite == "cock" ? 1f : s.Bite == "open" ? Smr(Mathf.Clamp01((ts - s.BiteT) / BiteOpen)) : s.Bite == "lift" ? Smr(Mathf.Clamp01((Time.unscaledTime - s.BiteU) / 0.7f)) : s.Bite == "smash" ? 1f - Ein(Mathf.Clamp01((Time.unscaledTime - s.BiteU) / 0.16f), 2.6f) : Mathf.Clamp01(1f - (ts - s.BiteT) / 0.12f);      // springs forward out of it; the slam: heaved up, then driven down
                    float backAmt = s.Bite == "lift" || s.Bite == "smash" ? 0.9f : 4.2f, upAmt = s.Bite == "lift" || s.Bite == "smash" ? 1.7f : 2.6f;      // the rear-back is big: 4 tiles back and 2.6 up, an S in the neck
                    for (int i = 0; i < 40; i++) { float w = Mathf.Pow(1f - i / 40f, 3f) * k; s.X[i] += dx / l * backAmt * w; s.Y[i] += dy / l * backAmt * w; s.Z[i] += upAmt * w; }
                }
            }
        }

        // ---- the reveal (it swallows the body on the slab) and the death (it brings the body back up, then goes down the big pool's centre)
        static void Reveal(EelBoss.Fight f, State s, float ts, float now, bool vomit)
        {
            var b = s.Big; if (s.RevealKey != f.Stage) { s.RevealKey = f.Stage; s.Hs = new Strike(b, (s.SlabX, s.SlabY)); s.Hs.Lock((s.SlabX, s.SlabY)); } var hs = s.Hs;
            float u = ts; FillHead(s, hs.Cur, hs.Sigma(u)); f.Zones.Clear();
            if (!vomit)
            {
                f.Label = u < 1.6f ? "REVEAL: it rises out of the big pool" : u < 2.3f ? "REVEAL: it swoops at the body" : u < 3.0f ? "REVEAL: it swallows the body" : "REVEAL: it sinks back to start the fight";
                if (u >= 2.3f && !f.Swallowed) { f.Swallowed = true; EncounterState.Enemies.Remove(f.Body); }
                if (u >= 4.2f) { f.Stage = "idle"; f.StageAt = now; f.Next = now + 1.0f; s.Hs = null; GameLog.Write("The Drowner settles in under the pools."); }
            }
            else
            {
                f.Label = u < 2.3f ? "VOMIT: it rises with the body in its throat" : u < 3.0f ? "VOMIT: the body is thrown up on the slab" : "RETREAT: it sinks down the big pool's centre";
                if (u >= 2.3f && !f.Ejected) { f.Ejected = true; f.Body.X = f.BodyX; f.Body.Y = f.BodyY; EncounterState.Enemies.Add(f.Body); HumanLootSystem.RollSpawnStash(f.Body, 4); GameLog.Write("It heaves the body up onto the slab."); s.Events.Add(new Ev { Kind = "impact", X = s.SlabX, Y = s.SlabY, Power = 0.6f, Water = false }); }
                if (u >= 4.2f) { f.Gone = true; f.Label = "gone"; EncounterState.Enemies.Remove(f.Unit); EncounterState.Enemies.Remove(s.Tail); Story.Set(EelBoss.Fled); GameLog.Write("The Drowner slides back down the pool. This isn't over."); }
            }
        }

        // ------------------------------------------------------------------ for the views
        /// <summary>How high a player is drawn (tiles): carried by the tail, thrown, hurled, flung.</summary>
        public static float LiftOf(Player p) { var f = EelBoss.Current; if (f == null || f.Sump == null || f.Gone) return 0f; return f.Sump.Air.TryGetValue(p, out var a) ? Mathf.Max(a.Z, 0f) : 0f; }
        /// <summary>Pulled under the water in the tail's grip: not drawn until it throws him.</summary>
        /// <summary>How far up the screen a boss part is drawn above its ground spot (tiles), plus half its sprite: the crosshair sits there when a player aims at the eel,
        /// so Rt.UnitUnder and Ballistics read it (0 for anything else, and for a part under the water).</summary>
        public static float LiftOfUnit(ICombatant u)
        {
            var f = EelBoss.Current; if (u == null || f == null || f.Sump == null || f.Gone) return 0f; var s = f.Sump;
            if (ReferenceEquals(u, f.Unit)) return s.Z[0] > 0.3f ? s.Z[0] + 1.3f : 0f;
            if (ReferenceEquals(u, s.Tail)) return s.Z[N - 1] > 0.3f ? s.Z[N - 1] + 0.6f : 0f;
            return 0f;
        }
        public static bool UnderWater(Player p) { var f = EelBoss.Current; return f != null && f.Sump != null && !f.Gone && f.Sump.Air.TryGetValue(p, out var a) && a.Z < -0.5f; }
        public static List<Ev> Drain() { var f = EelBoss.Current; if (f == null || f.Sump == null) return null; var l = new List<Ev>(f.Sump.Out); f.Sump.Out.Clear(); return l; }

        // ------------------------------------------------------------------ damage routing (called from EelBoss.OnDamaged)
        public static void OnHeadHit(EelBoss.Fight f, ICombatant u)
        {
            var s = f.Sump; if (!s.HeadOut || f.Stage == "reveal" || f.Stage == "vomit") { u.Hp = System.Math.Max(s.LastHp, 1); return; }       // under the water, or busy: nothing lands
            if (u.Hp <= 1) { u.Hp = 1; s.Dying = true; f.Label = "it gags - the body comes back up"; return; }
        }
        public static void OnTailHit(EelBoss.Fight f, ICombatant u)
        {
            var s = f.Sump; if (!s.TailOut) { u.Hp = System.Math.Max(s.LastTailHp, 1); return; }
            if (u.Hp <= 1) { u.Hp = 1; s.TailBroken = true; s.TailDownUntil = Time.time + 9f; GameLog.Write("The Drowner's tail is torn up - it goes down."); }
        }
    }
}
