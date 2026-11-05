// Boss 3: the eel of Sub-Station Depths (ACTION_DESIGN "Dodge bosses", Yan 2026-11-04/05). First pass of the fight code, host-side (Rt.WorldTick); the
// view that draws it is not in the game yet (the harness dump + Tools/render/eel_fight.py draw it from the strips ArtSource/bosses/eel_anim.py made).
// Two forms, one HP pool:
//  - "platform" (Levels 1-2, raised catwalks over deep water): it lurks below as a shadow and follows you; near a strike platform it winds up (red lamps
//    flicker), bursts out and WRAPS the platform in electric coils, you are held in; touching the coils SHOCKS you; it lets go and STRIKES at you.
//    Damage you do here carries over, but at most CarryCap of its HP (Yan: "maybe 20% of its hp max").
//  - "sump" (Level 3, the arena): REVEAL (it rises, swallows the body lying on the slab), then the rear-up fight with Hydra-style moves (Yan: "snapping at the
//    player, smashing around when enraged, circular sweeps"): snap, lunge, slam, sweep, and from the second wound stage (50% HP) SMASH = three slams in a row.
//    Wounds at 75/50/25% HP (Phase 1-3). At 0 HP it isn't killed: it VOMITS the body up (tier-4 armour, stash-like loot; the story key is a to-do) and
//    RETREATS down the pool (story flag eel_fled) - "we'll fight it again later in another place".
// Every move: a wind-up you can read (Zones, Label), then the hit; a dive (Rt.Untouchable) goes through everything. Bosses aren't nerfed (rule 6).
// Co-op: it fights the nearest player; every player in a zone is hit. Name is a placeholder (Yan picks it).
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class EelBoss
    {
        public const string Name = "The Drowner", Fled = "eel_fled";
        public const int MaxHp = 24000, ShockDamage = 90, StrikeDamage = 220, SnapDamage = 170, LungeDamage = 200, SlamDamage = 240, SweepDamage = 200;
        public const float CarryCap = 0.2f, F = 1f / 15f;      // the strips play at 15 fps
        public const float RingIn = 1.9f, RingOut = 3.1f, ShockGap = 0.7f, SweepR = 215f / 32f;
        public const float Burst = 0.9f, Hold = 4.0f, ReleaseFor = 0.7f, Turns = 2.5f, RingR = 2.3f;   // 2.5 turns of a ring that widens 0.28 a turn (2.3 -> 3.0, inside the 1.9-3.1 touch ring): the whole 35 tiles of body fits   // the wrap: the head circles for Burst s, the coil is held for Hold s (after the discharge), then it lets go
        // the head's landing point for each move, in tiles from the pool's centre, for an eel facing left (from the strips); x is mirrored for the right
        static readonly Dictionary<string, (float x, float y, float r, float hitAt, float len, int dmg)> Moves = new Dictionary<string, (float, float, float, float, float, int)>
        {
            ["snap"] = (-6.25f, -2.4f, 1.8f, 3 * F, 10 * F, SnapDamage), ["lunge"] = (-8.9f, -3.25f, 1.9f, 6 * F, 14 * F, LungeDamage), ["slam"] = (-4.4f, -0.95f, 2.4f, 5 * F, 10 * F, SlamDamage),
        };
        public const float RevealFor = 26 * F, HurtFor = 8 * F, SweepFor = 18 * F, VomitFor = 24 * F, EjectAt = 13 * F, RetreatFor = 14 * F;

        public static readonly MutantDef Def = new MutantDef { Name = Name, Tier = 3, Hp = MaxHp, PhysDR = 0.2f, AnomDR = 0.3f, Ap = 0, Mp = 0, Ac = 10, AttackDamage = 0, Behavior = "boss", PanicThreshold = 0f };

        public class Zone { public float X, Y, R; public string Kind; public bool Live; }      // where a move lands (Live = it can hurt now)
        public class Fight
        {
            public Mutant Unit; public string Level = "platform", Stage = "lurk", Move; public float StageAt, MoveAt = -1f, Next;
            public int Phase, Wraps, Chain, Side = -1; public bool Started, Gone, Hit, Enraged, Swallowed, Ejected;
            public float SX, SY, CX, CY, PX, PY;                 // the shadow / pool centre / the platform being wrapped (tiles)
            public string Label = "";                            // what it is doing, for the screen
            public HumanEnemy Body; public int BodyX, BodyY; public string BodyArmor;
            public readonly EelRig Rig = new EelRig();          // the procedural body: the state machine moves the head, the segments trail
            public float WindFor = Wind, FromX, FromY, PullX, PullY, S0X, S0Y, S1X, S1Y, S2X, S2Y; public bool Discharged;   // the current move: where the head started, its pull-back point, the sweep's Bezier
            public (float x, float y) Rest => (CX - Side * -3f, CY - 134f / 32f);           // the rear-up head
            public readonly List<Zone> Zones = new List<Zone>(); public readonly List<(float x, float y)> Platforms = new List<(float, float)>();
            public readonly HashSet<Player> Shocked = new HashSet<Player>();
        }
        public static Fight Current;
        static readonly Dictionary<Player, float> ShockAt = new Dictionary<Player, float>();
        static readonly List<string> Bag = new List<string>();
        public static void Reset() { Current = null; ShockAt.Clear(); Bag.Clear(); }
        public static bool Is(ICombatant u) => Current != null && !Current.Gone && Current.Unit == u;
        static int PhaseOf(int hp) => hp > MaxHp * 3 / 4 ? 0 : hp > MaxHp / 2 ? 1 : hp > MaxHp / 4 ? 2 : 3;
        public static float Age(Fight f) => Time.time - f.StageAt;

        /// <summary>Plan digits of a flooded level (TierDungeons.LabPlans): '7' strike platforms, '0' the pool's centre, '8' the body's slab.</summary>
        public static List<(int x, int y)> Marks(string zone, char digit)
        {
            var l = new List<(int, int)>(); var rows = TierDungeons.LabPlans[zone];
            for (int y = 0; y < rows.Length; y++) for (int x = 0; x < rows[y].Length; x++) if (rows[y][x] == digit) l.Add((x, y));
            return l;
        }

        public static Fight SpawnPlatform(string zone, float sx, float sy)
        {
            var m = new Mutant(Name, (int)sx, (int)sy, Def) { Hp = MaxHp }; EncounterState.Enemies.Add(m); Bag.Clear();
            var f = new Fight { Unit = m, Level = "platform", SX = sx, SY = sy, StageAt = Time.time, Next = Time.time + 1f };
            foreach (var (x, y) in Marks(zone, '7')) f.Platforms.Add((x + 0.5f, y + 0.5f));
            f.Rig.Place(sx, sy, 1f, 0f); f.Rig.Under = true;
            return Current = f;
        }

        /// <summary>The Sump: it lies in the pool until the first shot or step into its room; the body is on the slab (plan digit 8).</summary>
        public static Fight SpawnSump(string zone)
        {
            var c = Marks(zone, '0')[0]; var b = Marks(zone, '8')[0];
            var m = new Mutant(Name, c.x, c.y, Def) { Hp = MaxHp }; EncounterState.Enemies.Add(m); Bag.Clear();
            var f = new Fight { Unit = m, Level = "sump", Stage = "hidden", CX = c.x + 0.5f, CY = c.y + 0.5f, BodyX = b.x, BodyY = b.y, StageAt = Time.time };
            var suit = ArmorDatabase.Tiers["tier4"].Values.OrderBy(_ => Random.value).First(); f.BodyArmor = suit.Name;        // a random human in tier-4 armour
            f.Body = new HumanEnemy("Drowned crewman", b.x, b.y, "Contractors", "Veteran", null, suit) { Hp = 0 };
            EncounterState.Enemies.Add(f.Body);
            SetSlots(f); f.Rig.AnchorOn = true; f.Rig.Under = true; f.Rig.Snap(f.Rest.x, f.Rest.y);
            return Current = f;
        }

        static void Go(Fight f, string stage) { f.Stage = stage; f.StageAt = Time.time; f.Move = null; f.Hit = false; f.Zones.Clear(); }

        public static void OnDamaged(ICombatant u)
        {
            if (!Is(u)) return;
            var f = Current; f.Unit.IsAggroed = true; f.Started = true;
            if (f.Level == "platform") { u.Hp = System.Math.Max(u.Hp, (int)(MaxHp * (1f - CarryCap))); return; }     // Levels 1-2: at most 20% carries over
            if (f.Stage == "hidden") Go(f, "reveal");
            if (f.Stage == "vomit" || f.Stage == "retreat" || f.Stage == "reveal") { u.Hp = System.Math.Max(u.Hp, 1); return; }
            if (u.Hp <= 1) { u.Hp = 1; Go(f, "vomit"); f.Label = "it gags - the body comes back up"; GameLog.Write("The Drowner heaves."); return; }
            int ph = PhaseOf(u.Hp);
            if (ph > f.Phase) { f.Phase = ph; f.Enraged = ph >= 2; Bag.Clear(); Go(f, "hurt"); f.Label = ph >= 2 ? $"wounds open ({ph}) - ENRAGED: smashes" : $"wounds open ({ph})"; Sfx.Play("boss_sting", null, 0.7f, 0f, 1f); }
        }

        // ---------------------------------------------------------------- the clock
        public static void Tick(float dt)
        {
            var f = Current; if (f == null || f.Gone) return;
            var players = Player.All.Where(p => p.IsAlive()).ToList(); if (players.Count == 0) return;
            float now = Time.time, age = now - f.StageAt;
            var (ex, ey) = f.Level == "platform" ? (f.SX, f.SY) : (f.CX, f.CY);
            var tgt = players.OrderBy(p => { var (x, y) = Ballistics.Pos(p); return (x - ex) * (x - ex) + (y - ey) * (y - ey); }).First();
            var (px, py) = Ballistics.Pos(tgt);
            if (f.Level == "platform") Platform(f, players, tgt, px, py, now, age, dt); else Sump(f, players, tgt, px, py, now, age);
            DriveBody(f, dt);
        }

        // ---------------------------------------------------------------- Levels 1-2: the shadow, the wrap, the shock, the strike
        const float StrikeAt = Wind + 0.15f;                    // the platform strike: 0.6 s pull-back, 0.15 s dart, then it lands
        static void Platform(Fight f, List<Player> players, Player tgt, float px, float py, float now, float age, float dt)
        {
            switch (f.Stage)
            {
                case "lurk":
                    f.Label = "lurking";
                    var pl = f.Platforms.OrderBy(q => (q.x - px) * (q.x - px) + (q.y - py) * (q.y - py)).First();
                    float gx = Mathf.Abs(pl.x - px) < 9f && Mathf.Abs(pl.y - py) < 9f ? pl.x : px, gy = gx == pl.x ? pl.y : py;      // it shadows you; near a strike platform it goes for that
                    float dx = gx - f.SX, dy = gy - f.SY, d = Mathf.Sqrt(dx * dx + dy * dy) + 0.001f;
                    f.SX += dx / d * Mathf.Min(d, 3.4f * dt); f.SY += dy / d * Mathf.Min(d, 3.4f * dt); f.Unit.X = (int)f.SX; f.Unit.Y = (int)f.SY;
                    if (d < 0.6f && now >= f.Next && gx == pl.x) { f.PX = pl.x; f.PY = pl.y; Go(f, "wind"); f.Label = "WIND-UP: the lamps flicker, the water swells"; GameLog.Write("The water swells under the platform."); }
                    break;
                case "wind":
                    if (age >= 1.3f) { Go(f, "wrap"); f.Wraps++; f.Discharged = false; SetCoilSlots(f); f.Label = "WRAP: coils burst round the platform"; f.Shocked.Clear(); Sfx.Play("explosion", null, 0.5f, 0.05f, 0.6f); GameLog.Write("It bursts out and coils round the platform!"); }
                    break;
                case "wrap":
                    if (age >= Burst && !f.Discharged) { f.Discharged = true; Sfx.Play("explosion", null, 0.4f, 0.1f, 1.4f); GameLog.Write("The coils crackle with current."); }   // the segments are in place: the discharge
                    f.Label = !f.Discharged ? "WRAP: the head races round the platform, the body coils in" : "DISCHARGE held - touching the coils SHOCKS";
                    f.Zones.Clear(); f.Zones.Add(new Zone { X = f.PX, Y = f.PY, R = RingOut, Kind = "ring", Live = f.Discharged });
                    if (f.Discharged) foreach (var p in players)
                    {
                        var (qx, qy) = Ballistics.Pos(p); float rd = Mathf.Sqrt((qx - f.PX) * (qx - f.PX) + (qy - f.PY) * (qy - f.PY));
                        if (rd < RingIn || rd > RingOut || Rt.Untouchable(p)) continue;                  // inside the platform: safe; on the ring: shocked, thrown back in
                        if (ShockAt.TryGetValue(p, out var t0) && now - t0 < ShockGap) continue;
                        ShockAt[p] = now; f.Shocked.Add(p); f.Label = "SHOCK: you touched the coils"; Hurt(p, ShockDamage, "The coils shock you", "Psi");
                        float k = rd < 0.01f ? 0f : 1f / rd; Rt.MoveBy(p, (f.PX - qx) * k * 1.6f, (f.PY - qy) * k * 1.6f);
                    }
                    if (age >= Burst + Hold) { Go(f, "release"); f.Label = "it lets go"; }
                    break;
                case "release":
                    if (age >= ReleaseFor) { Go(f, "strike"); f.Label = "STRIKE: wind-up"; var (sx, sy) = Ballistics.Pos(tgt); f.Zones.Add(new Zone { X = sx, Y = sy, R = 1.7f, Kind = "strike" });
                        f.FromX = f.Rig.HX; f.FromY = f.Rig.HY; PullBack(f, sx, sy); f.Discharged = false; }
                    break;
                case "strike":
                    var z = f.Zones.FirstOrDefault();
                    if (z != null && age >= StrikeAt && !f.Hit) { f.Hit = true; z.Live = true; f.Label = "STRIKE!"; foreach (var p in players) Strike(p, z, StrikeDamage, "It strikes out of the water"); }
                    if (age >= StrikeAt + 0.4f) { Go(f, "recover"); f.Label = "it sinks back"; }
                    break;
                case "recover":
                    if (age >= 1.4f) { f.Next = now + 1.5f; Go(f, f.Wraps >= 2 ? "done" : "lurk"); }      // it has swum off by itself (DriveBody): SX, SY follow the head
                    break;
                case "done": f.Label = "swims off - the pump is waiting"; break;
            }
        }

        // ---------------------------------------------------------------- Level 3: the reveal, the rear-up fight, the vomit, the retreat
        static void Sump(Fight f, List<Player> players, Player tgt, float px, float py, float now, float age)
        {
            int sd = px >= f.CX ? 1 : -1; if (sd != f.Side && f.Stage != "move") { f.Side = sd; SetSlots(f); }          // it turns to face you between moves, not in the middle of one
            switch (f.Stage)
            {
                case "hidden": f.Label = "in the pool"; break;
                case "reveal":
                    f.Label = age < 8 * F ? "REVEAL: it rises" : age < 13 * F ? "REVEAL: it swoops at the body" : age < 15 * F ? "REVEAL: bite" : "REVEAL: it swallows the body";
                    if (age >= 13 * F && !f.Swallowed) { f.Swallowed = true; EncounterState.Enemies.Remove(f.Body); }
                    if (age >= RevealFor) { Go(f, "rear"); f.Next = now + 1.0f; f.Label = "rear-up"; GameLog.Write("The Drowner settles in over the pool."); }
                    break;
                case "hurt": if (age >= HurtFor) { Go(f, "rear"); f.Next = now + 0.8f; } break;
                case "rear":
                    if (now >= f.Next) Begin(f, tgt, px, py, now);
                    break;
                case "move":
                    Move(f, players, px, py, now, age);
                    break;
                case "vomit":
                    if (age >= EjectAt && !f.Ejected)
                    {
                        f.Ejected = true; f.Label = "VOMIT: the body is thrown up on the slab"; f.Body.X = f.BodyX; f.Body.Y = f.BodyY; EncounterState.Enemies.Add(f.Body);
                        HumanLootSystem.RollSpawnStash(f.Body, 4);        // stash-like loot; the story key is a to-do (Yan: "to-do later")
                        GameLog.Write("It heaves the body up onto the slab.");
                    }
                    if (age >= VomitFor) { Go(f, "retreat"); f.Label = "it sinks back down the pool"; }
                    break;
                case "retreat":
                    if (age >= RetreatFor) { f.Gone = true; f.Label = "gone"; EncounterState.Enemies.Remove(f.Unit); Story.Set(Fled); GameLog.Write("The Drowner slides back down the pool. This isn't over."); }
                    break;
            }
        }

        static void Begin(Fight f, Player tgt, float px, float py, float now)
        {
            float d = Mathf.Sqrt((px - f.CX) * (px - f.CX) + (py - f.CY) * (py - f.CY));
            if (Bag.Count == 0) { Bag.AddRange(new[] { "snap", "sweep", "lunge", "slam" }); if (f.Enraged) Bag.AddRange(new[] { "smash", "smash" }); Bag.Sort((a, b) => Random.value < 0.5f ? -1 : 1); }
            float Near(string m) { var (zx, zy) = Land(f, m == "smash" ? "slam" : m); return Mathf.Sqrt((px - zx) * (px - zx) + (py - zy) * (py - zy)); }   // how far you are from where the head would land
            string pick = Bag.FirstOrDefault(m => m == "sweep" ? Mathf.Abs(d - SweepR) < 4f : Near(m) <= 6.8f);       // a move that goes for where you are
            if (pick == null) { Bag.Clear(); pick = new[] { "snap", "lunge", "slam" }.OrderBy(Near).First(); }           // nothing fits: the nearest one
            Bag.Remove(pick); f.Chain = pick == "smash" ? 3 : 0; f.Move = pick == "smash" ? "slam" : pick; f.MoveAt = now; f.Stage = "move"; f.StageAt = now; f.Hit = false; f.Zones.Clear();
            Aim(f); f.WindFor = Wind; f.FromX = f.Rig.HX; f.FromY = f.Rig.HY; Prepare(f);
            f.Label = pick == "smash" ? "SMASH: wind-up (x3)" : pick.ToUpper() + ": wind-up";
        }

        static (float x, float y) Land(Fight f, string mv) { var m = Moves[mv]; return (f.CX - f.Side * m.x, f.CY + m.y); }       // x mirrored: native is left, side -1 -> as drawn
        static void Aim(Fight f)
        {
            f.Zones.Clear();
            if (f.Move == "sweep") return;
            var (zx, zy) = Land(f, f.Move); f.Zones.Add(new Zone { X = zx, Y = zy, R = Moves[f.Move].r, Kind = f.Move });
        }

        const float Wind = 0.6f, SweepRecover = 0.45f;
        static void Move(Fight f, List<Player> players, float px, float py, float now, float age)
        {
            string mv = f.Move; float t = age - f.WindFor;       // the first WindFor seconds are the telegraph: the head pulls back (DriveBody), then it strikes
            if (t < 0f) return;
            if (mv == "sweep")
            {
                bool live = t > 0.2f * SweepFor && t < 0.9f * SweepFor;
                f.Zones.Clear(); f.Zones.Add(new Zone { X = f.Rig.HX, Y = f.Rig.HY, R = 1.8f, Kind = "sweep", Live = live }); f.Label = "SWEEP!";      // the hitbox is the head itself, wherever the curve has taken it
                if (live) foreach (var p in players) Strike(p, f.Zones[0], SweepDamage, "The sweeping head catches you", true);
                if (t >= SweepFor + SweepRecover) End(f, now);
                return;
            }
            var m = Moves[mv];
            if (!f.Hit && t >= m.hitAt) { f.Hit = true; var z = f.Zones[0]; z.Live = true; f.Label = (f.Chain > 0 ? "SMASH!" : mv.ToUpper() + "!"); foreach (var p in players) Strike(p, z, m.dmg, "The Drowner's " + mv + " hits you"); }
            if (t >= m.len)
            {
                if (f.Chain > 1) { f.Chain--; f.MoveAt = now; f.StageAt = now; f.WindFor = Wind * 0.5f; f.Hit = false; Aim(f); f.FromX = f.Rig.HX; f.FromY = f.Rig.HY; Prepare(f); f.Label = "SMASH: again"; }   // the next slam comes at once (half the wind-up)
                else End(f, now);
            }
        }
        static void End(Fight f, float now) { f.Stage = "rear"; f.StageAt = now; f.Move = null; f.Zones.Clear(); f.Next = now + (f.Enraged ? 0.9f : 1.5f); f.Label = "rear-up"; f.Chain = 0; }

        // ---------------------------------------------------------------- the procedural body: where the head goes
        /// <summary>The rear-up pose as slots for the chain: the same curve ArtSource/bosses/eel_anim.py draws (neck, coil, tail), in tiles, mirrored when it faces right.</summary>
        static void SetSlots(Fight f)
        {
            var pts = new List<(float x, float y)> { (-96f, -134f) };
            pts.AddRange(Bez(new[] { (-70f, -134f), (-22f, -102f), (-6f, -58f), (-32f, -26f), (13f, -6f), (0f, 0f) }, 40));
            for (int i = 0; i <= 60; i++) { float t = i / 60f * 2f * Mathf.PI * 1.4f; pts.Add((Mathf.Cos(t) * (76f - t * 5f), Mathf.Sin(t) * (40f - t * 3f))); }
            var end = pts[pts.Count - 1]; pts.AddRange(Bez(new[] { end, (-192f, 32f), (-320f, 38f) }, 24).Skip(1));
            var d = (x: pts[pts.Count - 1].x - pts[pts.Count - 2].x, y: pts[pts.Count - 1].y - pts[pts.Count - 2].y); float dl = Mathf.Sqrt(d.x * d.x + d.y * d.y) + 1e-4f;
            for (int i = 1; i <= 40; i++) pts.Add((pts[pts.Count - 1].x + d.x / dl * 8f, pts[pts.Count - 1].y + d.y / dl * 8f));       // if the body is longer than the drawing: straight on
            var rig = f.Rig; rig.SlotX = new float[EelRig.Count]; rig.SlotY = new float[EelRig.Count];
            float step = EelRig.Spacing * 32f, acc = 0f; int k = 1, n = 0; var a = pts[0];
            while (n < EelRig.Count && k < pts.Count)
            {
                var b = pts[k]; float seg = Mathf.Sqrt((b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y));
                if (acc + seg >= step * (n + 1)) { float u = (step * (n + 1) - acc) / (seg + 1e-6f); float px = a.x + (b.x - a.x) * u, py = a.y + (b.y - a.y) * u; rig.SlotX[n] = f.CX - f.Side * px / 32f; rig.SlotY[n] = f.CY + py / 32f; n++; continue; }
                acc += seg; a = b; k++;
            }
        }
        static List<(float x, float y)> Bez((float x, float y)[] p, int n)      // a Bezier of any degree (de Casteljau)
        {
            var r = new List<(float, float)>();
            for (int i = 0; i <= n; i++) { float t = i / (float)n; var q = new (float x, float y)[p.Length]; System.Array.Copy(p, q, p.Length);
                for (int m = p.Length - 1; m > 0; m--) for (int j = 0; j < m; j++) q[j] = (q[j].x + (q[j + 1].x - q[j].x) * t, q[j].y + (q[j + 1].y - q[j].y) * t); r.Add(q[0]); }
            return r;
        }

        /// <summary>The wrap's end pose as slots: the body laid round the platform on the ring, walking back from where the head stops (2.5 turns of a ring that widens a little each turn).</summary>
        static void SetCoilSlots(Fight f)
        {
            var rig = f.Rig; rig.SlotX = new float[EelRig.Count]; rig.SlotY = new float[EelRig.Count];
            float a0 = -Mathf.PI / 2f, th = a0 + 2f * Mathf.PI * Turns; (float x, float y) prev = (f.PX + Mathf.Cos(th) * (RingR + 0.28f * Turns), f.PY + Mathf.Sin(th) * (RingR + 0.28f * Turns)); float acc = 0f; int n = 0;
            for (int guard = 0; n < EelRig.Count && guard < 4000; guard++)
            {
                th -= 0.02f; float rad = Mathf.Max(0.3f, RingR + 0.28f * (th - a0) / (2f * Mathf.PI)); var q = (x: f.PX + Mathf.Cos(th) * rad, y: f.PY + Mathf.Sin(th) * rad);
                acc += Mathf.Sqrt((q.x - prev.x) * (q.x - prev.x) + (q.y - prev.y) * (q.y - prev.y)); prev = q;
                while (n < EelRig.Count && acc >= EelRig.Spacing * (n + 1)) { rig.SlotX[n] = q.x; rig.SlotY[n] = q.y; n++; }
            }
            for (; n < EelRig.Count; n++) { rig.SlotX[n] = f.PX; rig.SlotY[n] = f.PY; }
        }

        /// <summary>The anticipation point: the head backs off from where it is about to strike (1.3 tiles).</summary>
        static void PullBack(Fight f, float tx, float ty)
        {
            float dx = f.FromX - tx, dy = f.FromY - ty, l = Mathf.Sqrt(dx * dx + dy * dy) + 1e-4f; f.PullX = f.FromX + dx / l * 1.3f; f.PullY = f.FromY + dy / l * 1.3f;
        }
        /// <summary>A move has been picked: its pull-back (strikes) or its Bezier (the sweep: from where the head is, over the top of the pool, through the control point).</summary>
        static void Prepare(Fight f)
        {
            if (f.Move == "sweep")
            {
                float sg = f.Side > 0 ? -1f : 1f, a0 = Mathf.PI + 0.5f, a1 = 2f * Mathf.PI - 0.5f;
                f.S0X = f.CX + Mathf.Cos(a0) * SweepR * sg; f.S0Y = f.CY + Mathf.Sin(a0) * SweepR; f.S2X = f.CX + Mathf.Cos(a1) * SweepR * sg; f.S2Y = f.CY + Mathf.Sin(a1) * SweepR;
                f.S1X = f.CX; f.S1Y = f.CY - SweepR * 1.45f;                                // the control point: a quadratic through both ends with its top about R above the pool
                PullBack(f, f.S0X, f.S0Y);
            }
            else { var z = f.Zones[0]; PullBack(f, z.X, z.Y); }
        }

        /// <summary>Every tick, after the state machine: where the head is now (a pure function of the stage and its age: easing curves, no animation clips), then the chain follows.</summary>
        static void DriveBody(Fight f, float dt)
        {
            var r = f.Rig; float age = Time.time - f.StageAt, t = Time.time; (float x, float y) head = (r.HX, r.HY);
            if (f.Level == "platform")
            {
                var c = (x: f.PX, y: f.PY); float ringA0 = -Mathf.PI / 2f; r.AnchorOn = f.Stage == "wrap"; r.AnchorFrom = 2; r.AnchorK = 16f * Mathf.Clamp01(age / Burst) * Mathf.Clamp01(age / Burst);   // the coil's slots pull the trailing body in as the burst goes on
                switch (f.Stage)
                {
                    case "lurk": case "done": head = (f.SX, f.SY); r.Under = true; r.SlitherAmount = 0.55f; r.SlitherSpeed = 7f; break;                     // swimming: the wave runs down the whole body
                    case "wind": head = (c.x + 0.3f * Mathf.Sin(age * 22f), c.y + 0.3f * Mathf.Cos(age * 19f)); r.Under = true; r.SlitherAmount = 0.5f; r.SlitherSpeed = 10f; break;
                    case "wrap":
                    {
                        r.Under = false; r.SlitherAmount = f.Discharged ? 0.03f : 0.2f; r.SlitherSpeed = f.Discharged ? 3f : 9f;
                        (float x, float y) RingAt(float ag)       // where the head is on the ring at stage age `ag`: out of the middle in the first half turn (the radius follows the ANGLE, so the path is long), then round and round: fast first, settling
                        {
                            float done = Turns * Ease.OutCubic(Mathf.Clamp01(ag / Burst)), ang = ringA0 + 2f * Mathf.PI * done, rad = Mathf.Lerp(0.3f, RingR + 0.28f * done, Ease.OutCubic(done / 0.6f));
                            return (c.x + Mathf.Cos(ang) * rad, c.y + Mathf.Sin(ang) * rad);
                        }
                        const int Sub = 10;                       // the head races (up to 100 tiles/s): sampled finely so it traces the circle and the rope follows the circle, not the chords
                        for (int k = 1; k <= Sub; k++) { var q = RingAt(age - dt + dt * k / Sub); r.Step(dt / Sub, q.x, q.y); }
                        f.SX = r.HX; f.SY = r.HY; return;
                    }
                    case "release":
                    {
                        float ang = ringA0 + 2f * Mathf.PI * Turns, u = Ease.OutExpo(age / ReleaseFor); r.SlitherAmount = 0.35f; r.SlitherSpeed = 8f;
                        float re = RingR + 0.28f * Turns + 4f * u; head = (c.x + Mathf.Cos(ang) * re, c.y + Mathf.Sin(ang) * re); break;                                     // the head uncoils outward, the body follows it off the platform
                    }
                    case "strike":
                    {
                        var z = f.Zones.FirstOrDefault(); float zx = z?.X ?? f.Rig.HX, zy = z?.Y ?? f.Rig.HY; r.SlitherAmount = 0.2f; r.SlitherSpeed = 6f;
                        if (age < Wind) head = Ease.Lerp((f.FromX, f.FromY), (f.PullX, f.PullY), Ease.InBack(age / Wind));                            // 0.6 s: it draws back (and dips the wrong way first)
                        else head = Ease.Lerp((f.PullX, f.PullY), (zx, zy), Ease.OutExpo((age - Wind) / 0.15f)); break;                               // then it is just there
                    }
                    case "recover":
                    {
                        float dx = f.PX + 5f - r.HX, dy = f.PY + 1f - r.HY, d = Mathf.Sqrt(dx * dx + dy * dy) + 1e-3f; r.SlitherAmount = 0.5f; r.SlitherSpeed = 7f;
                        head = age < 0.3f ? (r.HX, r.HY) : (r.HX + dx / d * Mathf.Min(d, 3.4f * dt), r.HY + dy / d * Mathf.Min(d, 3.4f * dt)); r.Under = age > 0.3f; break;   // it sinks and swims off
                    }
                }
                if (f.Stage != "lurk") { f.SX = head.x; f.SY = head.y; }
            }
            else
            {
                var rest = f.Rest; r.AnchorFrom = 12; r.AnchorK = 7f; r.AnchorOn = f.Stage != "hidden"; r.Under = f.Stage == "hidden"; r.SlitherAmount = 0.1f; r.SlitherSpeed = 2.5f;
                head = (rest.x + 0.12f * Mathf.Sin(t * 2.2f), rest.y + 0.08f * Mathf.Cos(t * 1.7f));
                if (f.Stage == "hurt") { float k = 1f - Ease.InOutSine(age / HurtFor); head = (rest.x - f.Side * 1.4f * k, rest.y - 0.6f * k); }
                else if (f.Stage == "move" && f.Move != null)
                {
                    r.SlitherAmount = 0.22f; r.SlitherSpeed = 6f; var from = (f.FromX, f.FromY); var pull = (f.PullX, f.PullY); float w = f.WindFor, s = age - w;
                    if (f.Move == "sweep")
                    {
                        var p0 = (f.S0X, f.S0Y); var p1 = (f.S1X, f.S1Y); var p2 = (f.S2X, f.S2Y);
                        if (s < 0f) head = Ease.Lerp(from, p0, Ease.InBack(age / w));                                                                        // pulls back to where the arc starts
                        else if (s < SweepFor) head = Ease.Bezier(p0, p1, p2, Ease.InOutSine(s / SweepFor));                                                 // over the top of the pool on a quadratic Bezier
                        else head = Ease.Lerp(p2, rest, Ease.InOutSine((s - SweepFor) / SweepRecover));
                    }
                    else
                    {
                        var m = Moves[f.Move]; var z = f.Zones.Count > 0 ? f.Zones[0] : null; var land = z != null ? (z.X, z.Y) : pull;
                        if (s < 0f) head = Ease.Lerp(from, pull, Ease.InBack(age / w));                                                                      // anticipation: 0.6 s (0.3 between smashes)
                        else if (s < m.hitAt) head = Ease.Lerp(pull, land, Ease.OutExpo(s / m.hitAt));                                                       // the strike: it arrives exactly when the hit lands
                        else if (s < m.hitAt + 0.12f) head = land;
                        else head = Ease.Lerp(land, rest, Ease.InOutSine((s - m.hitAt - 0.12f) / (m.len - m.hitAt - 0.12f)));
                    }
                }
            }
            r.Step(dt, head.x, head.y);
        }

        /// <summary>For a view: how many segments are above the water and worth drawing.</summary>
        public static int Surfaced(Fight f) => f.Level != "platform" ? EelRig.Count : f.Stage == "wrap" ? f.Rig.Visible(f.PX, f.PY, 5.5f) : f.Stage == "release" || f.Stage == "strike" ? f.Rig.Visible(f.PX, f.PY, 8f) : EelRig.Count;

        // ---------------------------------------------------------------- hits
        static void Strike(Player p, Zone z, int dmg, string what, bool once = false)
        {
            if (Rt.Untouchable(p)) return;
            var (qx, qy) = Ballistics.Pos(p); float dx = qx - z.X, dy = qy - z.Y;
            if (dx * dx + dy * dy > z.R * z.R) return;
            if (once) { if (Current.Shocked.Contains(p)) return; Current.Shocked.Add(p); }
            Hurt(p, dmg, what, "Slash");
        }

        static void Hurt(Player p, int dmg, string what, string kind)
        {
            int dealt = Mathf.RoundToInt(dmg * (1f - Mathf.Min(0.75f, kind == "Psi" ? p.AnomDR : p.PhysDR)));
            p.Hp = System.Math.Max(0, p.Hp - dealt);
            if (p.IsAlive()) Body.Hit(p, "Torso", 0f, dealt, kind);
            GameLog.Write($"{what} for {dealt}.");
            if (p.Hp <= 0) TurnLoop.CheckCombatEnd();
        }
    }
}
