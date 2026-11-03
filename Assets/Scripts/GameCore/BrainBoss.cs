// Boss 2 (Yan, 2026-11-03): the brain in the vat, the first dodge boss (ACTION_DESIGN "Dodge bosses"), in Secret Lab Alpha's vat
// room (lab_plans.py vat_room: a real lab, its cover boxes, metal tables and file cabinets).
// v2 (Yan: "Standing behind something completely nullifies the boss ... there's no buildup or animation for each attack, the orbs
// just spawn in and look exactly the same for every attack"):
//  - every move winds up: the eyes shut, it twitches, then they snap open on the release (ArtRow: the shut rows of the vat sheet)
//  - cover has HP (CoverHp: boxes 3, metal tables/benches 6, file cabinets 9); at each crack it emits a pulse that does 3 to all
//    cover in reach (Yan: "at 75% ... destroys the boxes, at 50% the tables and at 25% the file cabinets so there's no more cover")
//    and hurts anyone it passes who isn't diving through it
//  - rip: "telekinesis rips the cover off the nearest player and throws it at them" - lifted, hovering, then hurled
//  - laser: a beam that sweeps the whole room (cover doesn't stop it, the dive does); two beams in the last phase
//  - orbs by kind: pale ring orbs, fat aimed bolts, dark void orbs during the pull. Walls, doors and cover stop them.
// One HP pool, cracks at 75/50/25%; at 0 it holds at 1 HP, goes still, floats up, the giant eye looks at you, and the void
// takes it (Yan: "he floats into the air and a giant eye ... comes from 'behind' him and looks at the player, THEN he teleports
// away"). Story flag "brain_escaped": it comes back in a later lab. Host-side (Rt.WorldTick); clients don't see it yet.
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class BrainBoss
    {
        public const string Lab = "secret_lab_alpha", Name = "The Specimen", Escaped = "brain_escaped";
        public const int VatX = 37, VatY = 9, MaxHp = 18000, Wake = 9;   // Yan: "not tanky enough" -> "triple his hp"
        public const float StillFor = 3f, RiseFor = 1.4f, EyeFor = 2.4f;
        public const float OrbSpeed = 3.2f, BoltSpeed = 4.6f, OrbRadius = 0.3f, OrbLife = 9f, PullFor = 3f, PullSpeed = 1.3f, PullReach = 14f;
        public const float PulseSpeed = 8f, PulseReach = 16f, PulseWind = 1.1f, LaserTurn = 105f, LaserFor = 3.4f, LaserHitGap = 0.6f;
        public const float LiftFor = 0.7f, ThrowSpeed = 9f, ThrowReach = 12f;
        public const int OrbDamage = 100, BoltDamage = 130, PulseDamage = 150, LaserDamage = 120, ThrowDamage = 180, CrushDamage = 220, PulseCover = 3;

        public static readonly MutantDef Def = new MutantDef { Name = Name, Tier = 3, Hp = MaxHp, PhysDR = 0.2f, AnomDR = 0.5f, Ap = 0, Mp = 0,
            Ac = 10, AttackDamage = 0, Behavior = "boss", PsiBlast = true, PanicThreshold = 0f };

        public class Fight
        {
            public Mutant Unit; public bool Started, Drained, Gone;
            public int Phase;                                   // 0 full, 1-3 cracks (3 = the brain half bare)
            public string Stage;                                // the end: "still" -> "rise" -> "eye" -> gone
            public float Next, StageAt, PullUntil = -1f, NextOrb, PulseAt = -1f;
            public int HomeX, HomeY;                            // where the vat stands - it never leaves it
            public readonly List<Move> Moves = new List<Move>();   // what it's doing now: one move, two at once from the second crack
            public string Doing => Moves.Count > 0 ? Moves[0].Kind : null;
            public readonly List<string> Bag = new List<string>();   // the moves left this round (each once, then reshuffled)
        }
        public class Move { public string Kind; public float WindUntil, DoneAt; public int Count; public bool Finished; }
        public class Orb { public float X, Y, Dx, Dy, Speed, Age; public string Map, Kind; }   // Kind: ring | bolt | void
        public class Thrown { public float X, Y, Dx, Dy, Born, Gone; public string Look; public int Damage; public Player Target; public bool Flying; }
        public class Beam { public float Angle, Until, Length; }
        public class Wave { public float R; public readonly HashSet<int> Done = new HashSet<int>(); public readonly HashSet<Player> Hit = new HashSet<Player>(); }

        public static Fight Current;
        public static readonly List<Orb> Orbs = new List<Orb>();
        public static readonly List<Thrown> Throws = new List<Thrown>();
        public static readonly List<Beam> Beams = new List<Beam>();
        public static Wave Pulse;
        static readonly Dictionary<Player, float> LaserHitAt = new Dictionary<Player, float>();
        public static void Reset() { Current = null; Orbs.Clear(); Throws.Clear(); Beams.Clear(); Pulse = null; LaserHitAt.Clear(); }
        public static bool Is(ICombatant u) => Current != null && !Current.Gone && Current.Unit == u;

        /// <summary>Cover the room is built of, and how much the pulses must wear off it (each pulse does PulseCover).</summary>
        public static int CoverHp(string look) => look == "crate" ? 3 : look == "labtable" || look == "labbench" || look == "desk" ? 6 : look == "cabinet" ? 9 : 0;

        // ---------------- the vat's art (ArtSource/bosses/make_brain_vat.py: 8 columns, 11 rows) ----------------

        /// <summary>Row of the vat sheet: 0-3 eyes open by phase, 4-7 eyes shut (winding up), 8 drained, 9 floating up, 10 the eye.</summary>
        public static int ArtRow
        {
            get
            {
                var f = Current;
                if (f == null) return 0;
                if (f.Stage == "still") return 8;
                if (f.Stage == "rise") return 9;
                if (f.Stage == "eye") return 10;
                return Winding(f) ? 4 + f.Phase : f.Phase;
            }
        }
        /// <summary>Column: the loops run at 8 fps; the end stages play once over their length.</summary>
        public static int ArtCol
        {
            get
            {
                var f = Current;
                if (f != null && (f.Stage == "rise" || f.Stage == "eye"))
                    return Mathf.Clamp((int)((Time.time - f.StageAt) / (f.Stage == "rise" ? RiseFor : EyeFor) * 8f), 0, 7);
                return (int)(Time.time * 8f) % 8;
            }
        }
        public static bool Winding(Fight f) => f != null && !f.Drained && (f.Moves.Any(m => Time.time < m.WindUntil) || f.PulseAt >= 0f);

        // ---------------- spawning and damage ----------------

        public static void OnEnterZone(string zoneId)
        {
            Reset();
            if (zoneId != Lab || Story.Has(Escaped)) return;
            Spawn(VatX, VatY);
        }

        public static Fight Spawn(int x, int y)
        {
            var m = new Mutant(Name, x, y, Def) { Hp = MaxHp };
            EncounterState.Enemies.Add(m);
            Orbs.Clear(); Throws.Clear(); Beams.Clear(); Pulse = null;
            return Current = new Fight { Unit = m, HomeX = m.X, HomeY = m.Y };
        }

        static int PhaseOf(int hp) => hp > MaxHp * 3 / 4 ? 0 : hp > MaxHp / 2 ? 1 : hp > MaxHp / 4 ? 2 : 3;

        /// <summary>From Boss.OnDamaged: a crack at each quarter (and its pulse); at 0 it goes still instead of dying.</summary>
        public static void OnDamaged(ICombatant u)
        {
            if (!Is(u)) return;
            var f = Current; f.Unit.IsAggroed = true;
            if (!f.Started) Start(f);
            if (f.Drained) { u.Hp = System.Math.Max(1, u.Hp); return; }
            if (u.Hp <= 1)
            {
                u.Hp = 1; f.Drained = true; f.Moves.Clear(); f.PullUntil = -1f; f.PulseAt = -1f;
                Orbs.Clear(); Beams.Clear(); Pulse = null;
                f.Stage = "still"; f.StageAt = Time.time;
                Boss.HitStop(0.4f);
                CombatFx.Push("stagger", u, null, null).Now = true;
                GameLog.Write($"The last of the liquid runs out. {Name} slumps in the empty vat, still.");
                return;
            }
            int ph = PhaseOf(u.Hp);
            if (ph <= f.Phase) return;
            f.Phase = ph; f.Moves.Clear(); f.PullUntil = -1f; Beams.Clear(); f.Bag.Clear();
            f.PulseAt = Time.time + PulseWind;                   // eyes shut, gathering - then the pulse
            Boss.HitStop(0.25f);
            CombatFx.Push("stagger", u, null, null).Now = true;
            GameLog.Write(ph == 3 ? "The glass gives. The brain sags out of the liquid, every eye shut tight." : "The glass cracks. Its eyes squeeze shut.");
        }

        static void Start(Fight f)
        {
            f.Started = true; f.Next = Time.time + 1.2f; f.Unit.IsAggroed = true;
            EncounterState.InCombat = true;
            Boss.SplashTitle = Name.ToUpperInvariant(); Boss.SplashSub = "Break the vat"; Boss.SplashAt = Time.unscaledTime;
            GameLog.Write("Something in the vat opens its eyes.");
        }

        // ---------------- the loop ----------------

        /// <summary>Every frame on the host (Rt.WorldTick). dt is game time.</summary>
        public static void Tick(float dt)
        {
            var f = Current;
            if (f == null || f.Gone || !EncounterState.Enemies.Contains(f.Unit)) { Orbs.Clear(); Throws.Clear(); Beams.Clear(); Pulse = null; return; }
            float now = Time.time;
            var players = Player.All.Where(p => p.IsAlive() && !Body.Down(p)).ToList();
            if (!f.Started)
            {
                if (players.Any(p => CombatantUtil.Distance(p, f.Unit) <= 4 || CombatantUtil.Distance(p, f.Unit) <= Wake && Vision.Sees(f.Unit.X, f.Unit.Y))) Start(f);
                else return;
            }
            if (f.Unit.X != f.HomeX || f.Unit.Y != f.HomeY) { f.Unit.X = f.HomeX; f.Unit.Y = f.HomeY; }   // it's a vat: nothing walks it off its spot
            FlyOrbs(dt, players);
            FlyThrows(dt, players, now);
            SweepBeams(dt, players, now);
            SpreadPulse(dt, players);
            if (f.Drained) { TickEscape(f, now); return; }
            if (f.PulseAt >= 0f)
            {
                if (now < f.PulseAt) return;
                f.PulseAt = -1f; f.Next = now + 1.6f;
                Pulse = new Wave();
                CombatFx.Push("pulse", f.Unit, null, null).Now = true;
                GameLog.Write("Its eyes snap open. A wave of force rolls out across the room.");
                return;
            }
            if (players.Count == 0) return;
            var target = players.OrderBy(p => CombatantUtil.Distance(p, f.Unit)).First();
            if (now < f.PullUntil) Pull(f, players, dt, now);
            foreach (var m in f.Moves.ToList()) if (now >= m.DoneAt) Act(f, m, target, players, now);
            f.Moves.RemoveAll(m => m.Finished);
            if (f.Moves.Count > 0 || now < f.PullUntil || Beams.Count > 0 || now < f.Next) return;   // a volley runs out before the next
            // the next volley: one move, two at once from the second crack (Yan: "make it so he can use 2 attacks at the same time after 50%")
            for (int i = 0; i < (f.Phase >= 2 ? 2 : 1); i++)
            {
                var k = Pick(f);
                if (k == null) break;
                float w = now + (k == "laser" || k == "pull" ? 1.0f : k == "rip" ? 0.8f : 0.65f);
                f.Moves.Add(new Move { Kind = k, WindUntil = w, DoneAt = w });
            }
            CombatFx.Push("charge", f.Unit, target, null).Now = true;
        }

        /// <summary>The next move: a shuffled bag that grows with each crack - every move once before any repeats.</summary>
        static string Pick(Fight f)
        {
            for (int i = f.Bag.Count - 1; i >= 0; i--)          // the first in the bag that can go alongside what's already chosen
                if (Fits(f, f.Bag[i])) { var k = f.Bag[i]; f.Bag.RemoveAt(i); return k; }
            if (f.Bag.Count > 0) return null;                    // only clashing moves left: this volley is a single
            Refill(f);
            for (int i = f.Bag.Count - 1; i >= 0; i--)
                if (Fits(f, f.Bag[i])) { var k = f.Bag[i]; f.Bag.RemoveAt(i); return k; }
            return null;
        }

        /// <summary>Two moves at once must differ, and the laser never runs with the pull (no escape from both).</summary>
        static bool Fits(Fight f, string k) => f.Moves.All(m => m.Kind != k && !(m.Kind == "laser" && k == "pull" || m.Kind == "pull" && k == "laser"));

        static void Refill(Fight f)
        {
            {
                f.Bag.AddRange(new[] { "ring", "aimed", "rip" });
                if (f.Phase >= 1) f.Bag.Add("laser");
                if (f.Phase >= 2) f.Bag.Add("pull");
                if (f.Phase >= 3) { f.Bag.Add("ring"); f.Bag.Add("rip"); }
                for (int i = f.Bag.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (f.Bag[i], f.Bag[j]) = (f.Bag[j], f.Bag[i]); }
            }
        }

        static float Gap(Fight f) => 2.2f - 0.3f * f.Phase;

        static void Act(Fight f, Move mv, Player target, List<Player> players, float now)
        {
            var (cx, cy) = Center(f);
            if (mv.Count == 0) CombatFx.Push("release", f.Unit, target, null).Now = true;   // the eyes snap open
            switch (mv.Kind)
            {
                case "ring":
                    Ring(cx, cy, 10 + 2 * f.Phase, Random.value * 360f, OrbSpeed + 0.3f * f.Phase, "ring");
                    if (f.Phase >= 2 && ++mv.Count < 2) { mv.DoneAt = now + 0.5f; return; }   // a second ring, offset, when angry
                    Done(f, mv, now);
                    break;
                case "aimed":
                    var (tx, ty) = Ballistics.Pos(target);
                    float a = Mathf.Atan2(ty - cy, tx - cx) * Mathf.Rad2Deg;
                    for (int k = -1; k <= 1; k++) Shoot(cx, cy, a + k * 11f, BoltSpeed, "bolt");
                    if (++mv.Count < 2 + f.Phase / 2) { mv.DoneAt = now + 0.45f; return; }
                    Done(f, mv, now);
                    break;
                case "rip":
                    Rip(f, players, now);
                    Done(f, mv, now);
                    break;
                case "laser":
                    float start = Mathf.Atan2(target.Y - f.Unit.Y, target.X - f.Unit.X) * Mathf.Rad2Deg + 90f;   // starts off to your side, comes round
                    Beams.Add(new Beam { Angle = start, Until = now + LaserFor });
                    if (f.Phase >= 3) Beams.Add(new Beam { Angle = start + 180f, Until = now + LaserFor });
                    GameLog.Write("A beam lances out of the vat and starts to sweep the room.");
                    Done(f, mv, now);
                    break;
                case "pull":
                    f.PullUntil = now + PullFor; f.NextOrb = now + 0.4f; Done(f, mv, now);
                    CombatFx.Push("void", f.Unit, null, null).Now = true;
                    GameLog.Write("The air bends toward the vat.");
                    break;
                default: Done(f, mv, now); break;
            }
        }

        static void Done(Fight f, Move mv, float now) { mv.Finished = true; mv.Count = System.Math.Max(1, mv.Count); f.Next = System.Math.Max(f.Next, now + Gap(f)); }

        // ---------------- the moves ----------------

        /// <summary>The void pull: everyone near drags toward the vat; dark orbs keep coming; touching the glass hurts and throws you back.</summary>
        static void Pull(Fight f, List<Player> players, float dt, float now)
        {
            var (cx, cy) = Center(f);
            foreach (var p in players)
            {
                var (px, py) = Ballistics.Pos(p);
                float dx = cx - px, dy = cy - py, d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > PullReach || d < 0.01f) continue;
                if (d < 1.6f && !Rt.Untouchable(p)) { Hurt(p, CrushDamage, "The vat's glass slams into you"); Rt.MoveBy(p, -dx / d * 2.5f, -dy / d * 2.5f); continue; }
                Rt.MoveBy(p, dx / d * PullSpeed * dt, dy / d * PullSpeed * dt);
            }
            if (now >= f.NextOrb) { Ring(cx, cy, 8, Random.value * 360f, OrbSpeed * 0.8f, "void"); f.NextOrb = now + 0.8f; }
            if (now + dt >= f.PullUntil) f.Next = System.Math.Max(f.Next, now + Gap(f));
        }

        /// <summary>Rip: for each player (nearest first, one more piece when it's angry) the cover beside them - the piece between them and
        /// the vat if there is one - tears off the floor, hangs in the air a moment, then flies at them.</summary>
        static void Rip(Fight f, List<Player> players, float now)
        {
            int left = 1 + f.Phase / 2, ripped = 0;
            foreach (var p in players.OrderBy(p => CombatantUtil.Distance(p, f.Unit)))
            {
                if (left-- <= 0) break;
                var spot = CoverFor(p, f);
                if (spot == null) continue;
                var (x, y) = spot.Value;
                var t = TerrainMap.GetTile(x, y);
                Throws.Add(new Thrown { X = x + 0.5f, Y = y + 0.5f, Born = now, Look = t.Look, Damage = t.Damage, Target = p });
                TerrainMap.SetTileType(x, y, TileType.Empty, "");
                ripped++;
            }
            GameLog.Write(ripped > 0 ? "Your cover tears loose from the floor and hangs in the air." : "Nothing near you to tear loose - it glares.");
        }

        /// <summary>The cover piece to rip for a player: a neighbour on the vat's side first, then any next to them, then the nearest within 7 tiles.</summary>
        static (int x, int y)? CoverFor(Player p, Fight f)
        {
            (int, int)? best = null; float bestScore = float.MaxValue;
            float vx = f.Unit.X - p.X, vy = f.Unit.Y - p.Y, vl = Mathf.Max(0.01f, Mathf.Sqrt(vx * vx + vy * vy));
            for (int y = p.Y - 7; y <= p.Y + 7; y++)
                for (int x = p.X - 7; x <= p.X + 7; x++)
                {
                    var t = TerrainMap.GetTile(x, y);
                    if (t == null || CoverHp(t.Look) <= 0) continue;
                    float dx = x - p.X, dy = y - p.Y, d = Mathf.Sqrt(dx * dx + dy * dy);
                    float score = d - (d < 1.6f ? 10f : 0f) - (d < 1.6f ? (dx * vx + dy * vy) / (d * vl) : 0f);   // adjacent wins; the vat side first
                    if (score < bestScore) { bestScore = score; best = (x, y); }
                }
            return best;
        }

        static void FlyThrows(float dt, List<Player> players, float now)
        {
            for (int i = 0; i < Throws.Count; i++)
            {
                var t = Throws[i];
                if (!t.Flying)
                {
                    if (now - t.Born < LiftFor) continue;                         // hanging in the air, turning
                    var (tx, ty) = Ballistics.Pos(t.Target);
                    float dx = tx - t.X, dy = ty - t.Y, d = Mathf.Max(0.01f, Mathf.Sqrt(dx * dx + dy * dy));
                    t.Dx = dx / d; t.Dy = dy / d; t.Flying = true;
                    Sfx.Play("swing", TacticalGridView.GridToWorld((int)t.X, (int)t.Y), 1f, 0.05f, 0.5f);
                }
                float step = ThrowSpeed * dt;
                t.X += t.Dx * step; t.Y += t.Dy * step; t.Gone += step;
                bool stop = t.Gone >= ThrowReach;
                foreach (var p in players)
                {
                    var (px, py) = Ballistics.Pos(p);
                    if ((px - t.X) * (px - t.X) + (py - t.Y) * (py - t.Y) > 0.45f || Rt.Untouchable(p)) continue;
                    Hurt(p, ThrowDamage, "A flying " + Thing(t.Look) + " smashes into you");
                    Rt.MoveBy(p, t.Dx * 0.8f, t.Dy * 0.8f);
                    stop = true; break;
                }
                var tile = TerrainMap.GetTile(Mathf.FloorToInt(t.X), Mathf.FloorToInt(t.Y));
                if (tile == null || tile.Type != TileType.Empty) stop = true;
                if (!stop) continue;
                Land(t);
                Throws.RemoveAt(i--);
            }
        }

        /// <summary>A thrown piece comes down a step back from where it stopped, as cover again (with its wear), or in pieces if worn through.</summary>
        static void Land(Thrown t)
        {
            int x = Mathf.FloorToInt(t.X - t.Dx * 0.6f), y = Mathf.FloorToInt(t.Y - t.Dy * 0.6f);
            var at = TerrainMap.GetTile(x, y);
            Sfx.Play(Materials.Sound(Materials.Of(new TerrainTile { Look = t.Look })), TacticalGridView.GridToWorld(x, y), 1f, 0.1f);
            if (at == null || at.Type != TileType.Empty || TerrainMap.UnitAt(x, y, null) != null || t.Damage + 1 >= CoverHp(t.Look)) { Smash(x, y); return; }
            TerrainMap.SetTileType(x, y, t.Look == "cabinet" ? TileType.HardCover : TileType.ScatterCover, t.Look);
            TerrainMap.GetTile(x, y).Damage = t.Damage + 1;                      // the landing dents it
        }

        static void Smash(int x, int y)
        {
            var at = TerrainMap.GetTile(x, y);
            if (at != null && at.Type == TileType.Empty) TerrainMap.SetTileType(x, y, TileType.Empty, "papers");
        }

        static string Thing(string look) => look == "cabinet" ? "filing cabinet" : look == "crate" ? "crate" : "table";

        /// <summary>The laser: each beam turns LaserTurn degrees a second from the vat to the first wall; cover doesn't stop it.</summary>
        static void SweepBeams(float dt, List<Player> players, float now)
        {
            if (Beams.Count == 0) return;
            var (cx, cy) = Center(Current);
            for (int i = 0; i < Beams.Count; i++)
            {
                var b = Beams[i];
                if (now >= b.Until) { Beams.RemoveAt(i--); continue; }
                b.Angle += LaserTurn * dt;
                float r = b.Angle * Mathf.Deg2Rad, dx = Mathf.Cos(r), dy = Mathf.Sin(r);
                float len = 0f;
                for (; len < 20f; len += 0.25f)
                {
                    var t = TerrainMap.GetTile(Mathf.FloorToInt(cx + dx * len), Mathf.FloorToInt(cy + dy * len));
                    if (t == null || t.Type == TileType.Solid || t.Type == TileType.Door) break;
                }
                b.Length = len;
                foreach (var p in players)
                {
                    if (Rt.Untouchable(p) || LaserHitAt.TryGetValue(p, out var at) && now - at < LaserHitGap) continue;
                    var (px, py) = Ballistics.Pos(p);
                    float rx = px - cx, ry = py - cy, along = rx * dx + ry * dy, off = Mathf.Abs(rx * dy - ry * dx);
                    if (along < 0.5f || along > len || off > 0.4f) continue;
                    LaserHitAt[p] = now;
                    Hurt(p, LaserDamage, "The beam sears across you");
                }
            }
        }

        /// <summary>The pulse: a ring rolling out from the vat. Cover it passes loses PulseCover (and breaks when worn through); a player
        /// it passes is hurt and shoved, unless they're diving through it.</summary>
        static void SpreadPulse(float dt, List<Player> players)
        {
            if (Pulse == null) return;
            var (cx, cy) = Center(Current);
            float r0 = Pulse.R; Pulse.R += PulseSpeed * dt;
            int R = Mathf.CeilToInt(Pulse.R);
            for (int y = Mathf.FloorToInt(cy) - R; y <= Mathf.FloorToInt(cy) + R; y++)
                for (int x = Mathf.FloorToInt(cx) - R; x <= Mathf.FloorToInt(cx) + R; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                    if (d > Pulse.R || !Pulse.Done.Add(y * TerrainMap.Width + x)) continue;
                    var t = TerrainMap.GetTile(x, y);
                    if (t == null || CoverHp(t.Look) <= 0) continue;
                    t.Damage += PulseCover;
                    if (t.Damage >= CoverHp(t.Look)) { Smash(x, y); if (TerrainMap.GetTile(x, y).Type != TileType.Empty) TerrainMap.SetTileType(x, y, TileType.Empty, "papers"); }
                }
            foreach (var p in players)
            {
                var (px, py) = Ballistics.Pos(p);
                float d = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
                if (d < r0 || d > Pulse.R || !Pulse.Hit.Add(p) || Rt.Untouchable(p)) continue;
                Hurt(p, PulseDamage, "The wave of force throws you back");
                Rt.MoveBy(p, (px - cx) / Mathf.Max(0.1f, d) * 1.2f, (py - cy) / Mathf.Max(0.1f, d) * 1.2f);
            }
            if (Pulse.R >= PulseReach) Pulse = null;
        }

        static void TickEscape(Fight f, float now)
        {
            if (f.Stage == "still" && now - f.StageAt >= StillFor)
            {
                f.Stage = "rise"; f.StageAt = now;
                var g = CombatFx.Push("release", f.Unit, null, null); g.Now = true;
                GameLog.Write("Its eyes open again. It lifts off the floor of the vat.");
            }
            else if (f.Stage == "rise" && now - f.StageAt >= RiseFor)
            {
                f.Stage = "eye"; f.StageAt = now;
                GameLog.Write("Something huge slides out from behind it - an eye, on a cord of flesh. It looks straight at you.");
            }
            else if (f.Stage == "eye" && now - f.StageAt >= EyeFor)
            {
                f.Gone = true;
                CombatFx.Push("escape", f.Unit, null, null).Now = true;
                EncounterState.Enemies.Remove(f.Unit);
                Story.Set(Escaped);
                GameLog.Write("The void opens in the vat and swallows it whole. It isn't dead. It's somewhere else, thinking.");
                Rt.AddFocus(Player.Current, Rt.FocusMax);
                TurnLoop.CheckCombatEnd();
            }
        }

        // ---------------- the orbs ----------------

        /// <summary>The brain, not the base (tile space, y down).</summary>
        public static (float x, float y) Center(Fight f) { var (x, y) = Ballistics.Pos(f.Unit); return (x, y - 1f); }

        static void Ring(float x, float y, int n, float start, float speed, string kind) { for (int k = 0; k < n; k++) Shoot(x, y, start + k * 360f / n, speed, kind); }

        public static void Shoot(float x, float y, float deg, float speed, string kind = "ring")
        {
            float r = deg * Mathf.Deg2Rad;
            Orbs.Add(new Orb { X = x, Y = y, Dx = Mathf.Cos(r), Dy = Mathf.Sin(r), Speed = speed, Map = TerrainMap.ZoneId, Kind = kind });
        }

        /// <summary>Orbs drift straight on; a wall, a door or cover stops them; a player in the way takes the hit (not mid-dive).</summary>
        static void FlyOrbs(float dt, List<Player> players)
        {
            for (int i = 0; i < Orbs.Count; i++)
            {
                var o = Orbs[i];
                o.X += o.Dx * o.Speed * dt; o.Y += o.Dy * o.Speed * dt; o.Age += dt;
                bool gone = o.Map != TerrainMap.ZoneId || o.Age > OrbLife;
                if (!gone && o.Age > 0.25f)
                {
                    var t = TerrainMap.GetTile(Mathf.FloorToInt(o.X), Mathf.FloorToInt(o.Y));
                    gone = t == null || t.Type == TileType.Solid || t.Type == TileType.HardCover || t.Type == TileType.Door || CoverHp(t.Look) > 0;
                }
                if (!gone)
                    foreach (var p in players)
                    {
                        var (px, py) = Ballistics.Pos(p);
                        if ((px - o.X) * (px - o.X) + (py - o.Y) * (py - o.Y) > (OrbRadius + 0.3f) * (OrbRadius + 0.3f) || Rt.Untouchable(p)) continue;
                        Hurt(p, o.Kind == "bolt" ? BoltDamage : OrbDamage, "A psychic orb hits you"); gone = true; break;
                    }
                if (gone) Orbs.RemoveAt(i--);
            }
        }

        static void Hurt(Player p, int dmg, string what)
        {
            int dealt = Mathf.RoundToInt(dmg * (1f - Mathf.Min(0.75f, p.AnomDR)));
            p.Hp = System.Math.Max(0, p.Hp - dealt);
            if (p.IsAlive()) Body.Hit(p, "Torso", 0f, dealt, "Psi");
            var e = CombatFx.Push("psihit", null, p, null); e.Psi = true; e.Hit = true; e.Now = true;
            GameLog.Write($"{what} for {dealt}.");
            if (p.Hp <= 0) TurnLoop.CheckCombatEnd();
        }
    }
}
