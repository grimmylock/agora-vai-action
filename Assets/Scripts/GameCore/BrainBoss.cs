// Boss 2 (Yan, 2026-11-03): the brain in the vat, the first dodge boss (ACTION_DESIGN "Dodge bosses"). It sits in Secret Lab
// Alpha's big room. Few, slow, readable psychic orbs in rings, spirals and aimed bursts, each with a short wind-up; telekinesis
// on the room's props (thrown at you, dragged into lanes); from the second crack the void pull drags you toward the vat while
// orbs fly. One HP pool: the glass cracks at 75/50/25% (the liquid drains, each phase angrier), the last quarter is the brain
// half out of the liquid. At 0 it doesn't die: it goes still, its eyes open, the void swallows it - it comes back in a later lab
// (Yan: "then we fight it again with enhanced mechanics in another place"). Story flag "brain_escaped".
// Tools the player has: the dive's i-frames (RT 9), bullet time (V), cover (orbs stop on walls and hard cover).
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class BrainBoss
    {
        public const string Lab = "secret_lab_alpha", Name = "The Specimen", Escaped = "brain_escaped";
        public const int VatX = 37, VatY = 9, MaxHp = 6000, Wake = 9;
        public const float StillFor = 3.5f, AwakeFor = 1.6f, WindUp = 0.6f;
        public const float OrbSpeed = 3.2f, OrbRadius = 0.3f, OrbLife = 9f, PullFor = 3f, PullSpeed = 1.3f, PullReach = 14f;
        public const int OrbDamage = 110, CrushDamage = 220;

        public static readonly MutantDef Def = new MutantDef { Name = Name, Tier = 3, Hp = MaxHp, PhysDR = 0.2f, AnomDR = 0.5f, Ap = 0, Mp = 0,
            Ac = 10, AttackDamage = 0, Behavior = "boss", PsiBlast = true, PanicThreshold = 0f };

        public class Fight
        {
            public Mutant Unit; public bool Started, Drained, Awake, Gone;
            public int Phase;                                   // 0 full, 1-3 cracks (3 = the brain half bare)
            public string Doing; public float DoneAt, Next, StillUntil = -1f, AwakeUntil = -1f, PullUntil = -1f, NextOrb;
            public float SpiralAngle; public int Count;
            public readonly List<string> Bag = new List<string>();   // the moves left this round (each once, then reshuffled)
        }
        public class Orb { public float X, Y, Dx, Dy, Speed, Age; public string Map; }

        public static Fight Current;
        public static readonly List<Orb> Orbs = new List<Orb>();
        public static void Reset() { Current = null; Orbs.Clear(); }
        public static bool Is(ICombatant u) => Current != null && !Current.Gone && Current.Unit == u;

        /// <summary>Which of the vat's six frames to show (ArtSource/bosses/make_brain_vat.py: full, crack1-3, drained, awake).</summary>
        public static int ArtFrame => Current == null ? 0 : Current.Awake ? 5 : Current.Drained ? 4 : Current.Phase;

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
            Orbs.Clear();
            return Current = new Fight { Unit = m };
        }

        static int PhaseOf(int hp) => hp > MaxHp * 3 / 4 ? 0 : hp > MaxHp / 2 ? 1 : hp > MaxHp / 4 ? 2 : 3;

        /// <summary>From Boss.OnDamaged: a crack opens at each quarter; at 0 the brain goes still instead of dying.</summary>
        public static void OnDamaged(ICombatant u)
        {
            if (!Is(u)) return;
            var f = Current; f.Unit.IsAggroed = true;
            if (!f.Started) Start(f);
            if (f.Drained) { u.Hp = System.Math.Max(1, u.Hp); return; }
            if (u.Hp <= 1)
            {
                u.Hp = 1; f.Drained = true; f.Doing = null; f.PullUntil = -1f; Orbs.Clear();
                f.StillUntil = Time.time + StillFor;
                Boss.HitStop(0.4f);
                CombatFx.Push("stagger", u, null, null).Now = true;
                GameLog.Write($"The last of the liquid runs out. {Name} slumps against the glass, still.");
                return;
            }
            int ph = PhaseOf(u.Hp);
            if (ph <= f.Phase) return;
            f.Phase = ph; f.Doing = null; f.PullUntil = -1f; f.Next = Time.time + 1.5f; f.Bag.Clear();   // the new moves join at once
            Boss.HitStop(0.25f);
            CombatFx.Push("stagger", u, null, null).Now = true;
            GameLog.Write(ph == 3 ? "The glass gives. The brain sags out of the liquid, every eye on you." : "The glass cracks. The liquid starts to drain.");
        }

        static void Start(Fight f)
        {
            f.Started = true; f.Next = Time.time + 1.2f; f.Unit.IsAggroed = true;
            EncounterState.InCombat = true;
            Boss.SplashTitle = Name.ToUpperInvariant(); Boss.SplashSub = "Break the vat"; Boss.SplashAt = Time.unscaledTime;
            GameLog.Write("Something in the vat opens its eyes.");
        }

        /// <summary>Every frame on the host (Rt.WorldTick). dt is game time.</summary>
        public static void Tick(float dt)
        {
            var f = Current;
            if (f == null || f.Gone) { Orbs.Clear(); return; }
            if (!EncounterState.Enemies.Contains(f.Unit)) { Orbs.Clear(); return; }   // left the map (the enemy list is per map)
            float now = Time.time;
            var players = Player.All.Where(p => p.IsAlive() && !Body.Down(p)).ToList();
            if (!f.Started)
            {
                if (players.Any(p => CombatantUtil.Distance(p, f.Unit) <= 4 || CombatantUtil.Distance(p, f.Unit) <= Wake && Vision.Sees(f.Unit.X, f.Unit.Y))) Start(f);
                else return;
            }
            FlyOrbs(dt, players);
            if (f.Drained) { TickEscape(f, now); return; }
            if (players.Count == 0) return;
            var target = players.OrderBy(p => CombatantUtil.Distance(p, f.Unit)).First();
            if (now < f.PullUntil) { Pull(f, players, dt, now); return; }
            if (f.Doing == null)
            {
                if (now < f.Next) return;
                f.Doing = Pick(f); f.DoneAt = now + WindUp; f.Count = 0;
                var g = CombatFx.Push("glint", f.Unit, target, null); g.Psi = true; g.Now = true;   // the tell: a purple glint over the vat
                return;
            }
            if (now < f.DoneAt) return;
            Act(f, target, now);
        }

        /// <summary>The next move: a shuffled bag of patterns that grows with each crack - every move comes up once before any repeats,
        /// so the fight shows all it has and stays readable.</summary>
        static string Pick(Fight f)
        {
            if (f.Bag.Count == 0)
            {
                f.Bag.AddRange(new[] { "ring", "aimed", "props" });
                if (f.Phase >= 1) f.Bag.Add("spiral");
                if (f.Phase >= 2) f.Bag.Add("pull");
                if (f.Phase >= 3) { f.Bag.Add("ring"); f.Bag.Add("spiral"); }
                for (int i = f.Bag.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (f.Bag[i], f.Bag[j]) = (f.Bag[j], f.Bag[i]); }
            }
            var m = f.Bag[f.Bag.Count - 1]; f.Bag.RemoveAt(f.Bag.Count - 1);
            return m;
        }

        static float Gap(Fight f) => 2.4f - 0.35f * f.Phase;

        static void Act(Fight f, Player target, float now)
        {
            var (cx, cy) = Center(f);
            switch (f.Doing)
            {
                case "ring":
                    Ring(cx, cy, 10 + 2 * f.Phase, Random.value * 360f, OrbSpeed + 0.3f * f.Phase);
                    Done(f, now);
                    break;
                case "aimed":
                    var (tx, ty) = Ballistics.Pos(target);
                    float a = Mathf.Atan2(ty - cy, tx - cx) * Mathf.Rad2Deg;
                    for (int k = -1; k <= 1; k++) Shoot(cx, cy, a + k * 12f, OrbSpeed * 1.4f);
                    if (++f.Count < 2 + f.Phase / 2) { f.DoneAt = now + 0.45f; return; }   // a second (third) volley
                    Done(f, now);
                    break;
                case "spiral":
                    for (int k = 0; k < 2; k++) Shoot(cx, cy, f.SpiralAngle + k * 180f, OrbSpeed);
                    f.SpiralAngle += 23f;
                    if (++f.Count < 16 + 2 * f.Phase) { f.DoneAt = now + 0.12f; return; }
                    Done(f, now);
                    break;
                case "props":
                    Telekinesis(f, target);
                    Done(f, now);
                    break;
                case "pull":
                    f.PullUntil = now + PullFor; f.NextOrb = now + 0.4f; f.Doing = null;
                    CombatFx.Push("void", f.Unit, null, null).Now = true;
                    GameLog.Write("The air bends toward the vat.");
                    break;
                default: Done(f, now); break;
            }
        }

        static void Done(Fight f, float now) { f.Doing = null; f.Next = now + Gap(f); }

        /// <summary>The void pull: everyone near drags toward the vat (a dive still moves you, its i-frames don't stop the pull);
        /// rings keep coming; whoever reaches the glass is crushed against it and thrown back.</summary>
        static void Pull(Fight f, List<Player> players, float dt, float now)
        {
            var (cx, cy) = Center(f);
            foreach (var p in players)
            {
                var (px, py) = Ballistics.Pos(p);
                float dx = cx - px, dy = cy - py, d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > PullReach || d < 0.01f) continue;
                if (d < 1.6f && !Rt.Untouchable(p))
                {
                    Hurt(p, CrushDamage);
                    Rt.MoveBy(p, -dx / d * 2.5f, -dy / d * 2.5f);
                    continue;
                }
                Rt.MoveBy(p, dx / d * PullSpeed * dt, dy / d * PullSpeed * dt);
            }
            if (now >= f.NextOrb) { Ring(cx, cy, 8, f.SpiralAngle += 17f, OrbSpeed * 0.8f); f.NextOrb = now + 0.8f; }
            if (now + dt >= f.PullUntil) Done(f, now);
        }

        /// <summary>Props in the room fly: the ones between the vat and you are hurled at you, the rest dragged across the floor
        /// into lanes (Props.Push: barrels roll and bowl you over, crates and desks slide).</summary>
        static void Telekinesis(Fight f, Player target)
        {
            int moved = 0, want = 2 + f.Phase;
            var spots = new List<(int x, int y)>();
            for (int y = f.Unit.Y - 9; y <= f.Unit.Y + 9; y++)
                for (int x = f.Unit.X - 11; x <= f.Unit.X + 11; x++)
                    if (Props.Movable(x, y)) spots.Add((x, y));
            foreach (var (x, y) in spots.OrderBy(_ => Random.value))
            {
                if (moved >= want) break;
                int dx = System.Math.Sign(target.X - x), dy = System.Math.Sign(target.Y - y);
                bool hurl = moved % 2 == 0;
                if (!hurl) { dx = System.Math.Sign(f.Unit.X - x); dy = 0; if (dx == 0) dx = 1; }   // dragged sideways into a wall
                if ((dx != 0 || dy != 0) && Props.Push(x, y, dx, dy, hurl ? 7 : 4, f.Unit)) moved++;
            }
            CombatFx.Push("strike", f.Unit, target, null).Psi = true;
            if (moved > 0) GameLog.Write(moved == 1 ? "Something in the room lifts and flies." : "The furniture moves on its own.");
        }

        static void TickEscape(Fight f, float now)
        {
            if (!f.Awake && now >= f.StillUntil)
            {
                f.Awake = true; f.AwakeUntil = now + AwakeFor;
                var g = CombatFx.Push("glint", f.Unit, null, null); g.Psi = true; g.Now = true;
                GameLog.Write("Its eyes open again.");
            }
            else if (f.Awake && now >= f.AwakeUntil)
            {
                f.Gone = true;
                CombatFx.Push("escape", f.Unit, null, null).Now = true;
                EncounterState.Enemies.Remove(f.Unit);
                Story.Set(Escaped);
                GameLog.Write("The void opens under the vat and swallows it whole. It isn't dead. It's somewhere else, thinking.");
                Rt.AddFocus(Player.Current, Rt.FocusMax);
                TurnLoop.CheckCombatEnd();
            }
        }

        // ---------------- the orbs ----------------

        static (float x, float y) Center(Fight f) { var (x, y) = Ballistics.Pos(f.Unit); return (x, y - 1f); }   // the brain, not the base

        static void Ring(float x, float y, int n, float start, float speed) { for (int k = 0; k < n; k++) Shoot(x, y, start + k * 360f / n, speed); }

        public static void Shoot(float x, float y, float deg, float speed)
        {
            float r = deg * Mathf.Deg2Rad;
            Orbs.Add(new Orb { X = x, Y = y, Dx = Mathf.Cos(r), Dy = Mathf.Sin(r), Speed = speed, Map = TerrainMap.ZoneId });
        }

        /// <summary>Orbs drift straight on; a wall or hard cover stops them, a player in the way takes the hit (not mid-dive).</summary>
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
                    gone = t == null || t.Type == TileType.Solid || t.Type == TileType.HardCover || t.Type == TileType.Door;
                }
                if (!gone)
                    foreach (var p in players)
                    {
                        var (px, py) = Ballistics.Pos(p);
                        if ((px - o.X) * (px - o.X) + (py - o.Y) * (py - o.Y) > (OrbRadius + 0.3f) * (OrbRadius + 0.3f) || Rt.Untouchable(p)) continue;
                        Hurt(p, OrbDamage); gone = true; break;
                    }
                if (gone) Orbs.RemoveAt(i--);
            }
        }

        static void Hurt(Player p, int dmg)
        {
            int dealt = Mathf.RoundToInt(dmg * (1f - Mathf.Min(0.75f, p.AnomDR)));
            p.Hp = System.Math.Max(0, p.Hp - dealt);
            if (p.IsAlive()) Body.Hit(p, "Torso", 0f, dealt, "Psi");
            var e = CombatFx.Push("strike", Current?.Unit, p, null); e.Psi = true; e.Hit = true; e.Now = true;
            GameLog.Write($"A psychic orb hits you for {dealt}.");
            if (p.Hp <= 0) TurnLoop.CheckCombatEnd();
        }
    }
}
