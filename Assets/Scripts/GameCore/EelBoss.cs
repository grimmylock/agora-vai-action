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
        public const float RingIn = 1.9f, RingOut = 3.1f, WrapHold = 4.2f, ShockGap = 0.7f, SweepR = 215f / 32f;
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
        }

        // ---------------------------------------------------------------- Levels 1-2: the shadow, the wrap, the shock, the strike
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
                    if (age >= 1.3f) { Go(f, "wrap"); f.Wraps++; f.Label = "WRAP: coils burst round the platform"; f.Shocked.Clear(); Sfx.Play("explosion", null, 0.5f, 0.05f, 0.6f); GameLog.Write("It bursts out and coils round the platform!"); }
                    break;
                case "wrap":
                    f.Label = age < 8 * F ? "WRAP: coils burst round the platform" : "WRAP held - touching the coils SHOCKS";
                    f.Zones.Clear(); f.Zones.Add(new Zone { X = f.PX, Y = f.PY, R = RingOut, Kind = "ring", Live = true });
                    foreach (var p in players)
                    {
                        var (qx, qy) = Ballistics.Pos(p); float rd = Mathf.Sqrt((qx - f.PX) * (qx - f.PX) + (qy - f.PY) * (qy - f.PY));
                        if (rd < RingIn || rd > RingOut || Rt.Untouchable(p)) continue;                  // inside the platform: safe; on the ring: shocked, thrown back in
                        if (ShockAt.TryGetValue(p, out var t0) && now - t0 < ShockGap) continue;
                        ShockAt[p] = now; f.Shocked.Add(p); f.Label = "SHOCK: you touched the coils"; Hurt(p, ShockDamage, "The coils shock you", "Psi");
                        float k = rd < 0.01f ? 0f : 1f / rd; Rt.MoveBy(p, (f.PX - qx) * k * 1.6f, (f.PY - qy) * k * 1.6f);
                    }
                    if (age >= WrapHold) { Go(f, "release"); f.Label = "it lets go"; }
                    break;
                case "release":
                    if (age >= 8 * F) { Go(f, "strike"); f.Label = "STRIKE: wind-up"; var (sx, sy) = Ballistics.Pos(tgt); f.Zones.Add(new Zone { X = sx, Y = sy, R = 1.7f, Kind = "strike" }); }
                    break;
                case "strike":
                    var z = f.Zones.FirstOrDefault();
                    if (z != null && age >= 0.55f && !f.Hit) { f.Hit = true; z.Live = true; f.Label = "STRIKE!"; foreach (var p in players) Strike(p, z, StrikeDamage, "It strikes out of the water"); }
                    if (age >= 0.55f + 0.4f) { Go(f, "recover"); f.Label = "it sinks back"; }
                    break;
                case "recover":
                    if (age >= 1.2f) { f.SX = f.PX + 5f; f.SY = f.PY + 1f; f.Next = now + 1.5f; Go(f, f.Wraps >= 2 ? "done" : "lurk"); }
                    break;
                case "done": f.Label = "swims off - the pump is waiting"; break;
            }
        }

        // ---------------------------------------------------------------- Level 3: the reveal, the rear-up fight, the vomit, the retreat
        static void Sump(Fight f, List<Player> players, Player tgt, float px, float py, float now, float age)
        {
            f.Side = px >= f.CX ? 1 : -1;
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
            Aim(f);
            f.Label = pick == "smash" ? "SMASH: wind-up (x3)" : pick.ToUpper() + ": wind-up";
        }

        static (float x, float y) Land(Fight f, string mv) { var m = Moves[mv]; return (f.CX - f.Side * m.x, f.CY + m.y); }       // x mirrored: native is left, side -1 -> as drawn
        static void Aim(Fight f)
        {
            f.Zones.Clear();
            if (f.Move == "sweep") return;
            var (zx, zy) = Land(f, f.Move); f.Zones.Add(new Zone { X = zx, Y = zy, R = Moves[f.Move].r, Kind = f.Move });
        }

        const float Wind = 0.6f;
        static void Move(Fight f, List<Player> players, float px, float py, float now, float age)
        {
            string mv = f.Move; float t = age - Wind;           // the first Wind seconds are the telegraph (the rear-up pose holds), then the strip plays
            if (t < 0f) return;
            if (mv == "sweep")
            {
                float e = Mathf.Clamp01(t / SweepFor); e = e * e * (3 - 2 * e); float th = Mathf.Lerp(Mathf.PI + 0.5f, 2 * Mathf.PI - 0.5f, e);
                float hx = f.CX + Mathf.Cos(th) * SweepR * (f.Side > 0 ? -1f : 1f), hy = f.CY + Mathf.Sin(th) * SweepR;
                f.Zones.Clear(); f.Zones.Add(new Zone { X = hx, Y = hy, R = 1.8f, Kind = "sweep", Live = t > 3 * F && t < 16 * F }); f.Label = "SWEEP!";
                if (t > 3 * F && t < 16 * F) foreach (var p in players) { var z = f.Zones[0]; Strike(p, z, SweepDamage, "The sweeping head catches you", true); }
                if (t >= SweepFor) End(f, now);
                return;
            }
            var m = Moves[mv];
            if (!f.Hit && t >= m.hitAt) { f.Hit = true; var z = f.Zones[0]; z.Live = true; f.Label = (f.Chain > 0 ? "SMASH!" : mv.ToUpper() + "!"); foreach (var p in players) Strike(p, z, m.dmg, "The Drowner's " + mv + " hits you"); }
            if (t >= m.len)
            {
                if (f.Chain > 1) { f.Chain--; f.MoveAt = now; f.StageAt = now - Wind * 0.5f; f.Hit = false; Aim(f); f.Label = "SMASH: again"; }   // the next slam comes at once (half the wind-up)
                else End(f, now);
            }
        }
        static void End(Fight f, float now) { f.Stage = "rear"; f.StageAt = now; f.Move = null; f.Zones.Clear(); f.Next = now + (f.Enraged ? 0.9f : 1.5f); f.Label = "rear-up"; f.Chain = 0; }

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
