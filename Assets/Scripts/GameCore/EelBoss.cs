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
        public const float RingIn = 1.9f, RingOut = 3.4f, ShockGap = 0.7f, SweepR = 215f / 32f;
        public const float Burst = 0.9f, Hold = 4.0f, ConstrictFor = 1.5f, ConstrictSpacing = 0.5f, ReleaseFor = 0.7f, Turns = 2.5f, RingR = 3.2f, Pitch = 0.4f, CoilRise = 0.7f;   // 2.5 turns of a ring that widens 0.28 a turn (2.3 -> 3.0, inside the 1.9-3.1 touch ring): the whole 35 tiles of body fits   // the wrap: the head circles for Burst s, the coil is held for Hold s (after the discharge), then it lets go
        public static readonly MutantDef Def = new MutantDef { Name = Name, Tier = 3, Hp = MaxHp, PhysDR = 0.2f, AnomDR = 0.3f, Ap = 0, Mp = 0, Ac = 10, AttackDamage = 0, Behavior = "boss", PanicThreshold = 0f };

        public class Zone { public float X, Y, R; public string Kind; public bool Live; public float[] Path; }      // where a move lands (Live = it can hurt now)
        public class Fight
        {
            public Mutant Unit; public string Level = "platform", Stage = "lurk", Move; public float StageAt, MoveAt = -1f, Next;
            public int Phase, Wraps, Chain, Side = -1; public bool Started, Gone, Hit, Enraged, Swallowed, Ejected;
            public float SX, SY, CX, CY, PX, PY;                 // the shadow / pool centre / the platform being wrapped (tiles)
            public string Label = "";                            // what it is doing, for the screen
            public HumanEnemy Body; public int BodyX, BodyY; public string BodyArmor;
            public EelSump.State Sump;                          // the Sump fight (four pools, authored moves, a targetable tail): EelSump.cs
            public EelSump.State View;                          // Levels 1-2: a spine laid along the rig (FillView) so the platform form is drawn and shot like the Sump's
            public readonly EelRig Rig = new EelRig();          // the procedural body: the state machine moves the head, the segments trail
            public float WindFor = Wind, FromX, FromY, PullX, PullY, S0X, S0Y, S1X, S1Y, S2X, S2Y; public bool Discharged;   // the current move: where the head started, its pull-back point, the sweep's Bezier
            public float TgtX, TgtY, EscX, EscY;                   // the player it is watching; the way it dives after a strike
            public (float x, float y) Rest => (CX - Side * -3f, CY - 134f / 32f);           // the rear-up head
            public readonly List<Zone> Zones = new List<Zone>(); public readonly List<(float x, float y)> Platforms = new List<(float, float)>();
            public readonly HashSet<Player> Shocked = new HashSet<Player>();
        }
        public static Fight Current;
        static readonly Dictionary<Player, float> ShockAt = new Dictionary<Player, float>();
        static readonly List<string> Bag = new List<string>();
        /// <summary>On entering a zone (StorySystem.OnEnterZone): the Sump's eel, until it has fled (Story flag eel_fled) - hidden in its pools until you step into the room.</summary>
        public static void OnEnterZone(string zoneId)
        {
            Reset();
            if (zoneId == LabLocks.Depths3 && !Story.Has(Fled)) { SpawnSump(zoneId); return; }
            // Levels 1-2 (Yan, 2026-11-04: "kind of jump-scare style stuff"): it lurks below as a shadow, follows you, and wraps the platform near the pump; gone once that level's pump has run
            string drain = zoneId == LabLocks.Depths1 ? "depths_drain1" : zoneId == LabLocks.Depths2 ? "depths_drain2" : null;
            if (drain == null || MacroState.Current != null && MacroState.Current.Keys.Contains(drain) || !TierDungeons.LabPlans.ContainsKey(zoneId)) return;
            var marks = Marks(zoneId, '7'); if (marks.Count == 0) return;
            var me = Player.Current; var far = marks.OrderByDescending(m => me == null ? 0f : (m.x - me.X) * (m.x - me.X) + (m.y - me.Y) * (m.y - me.Y)).First();      // it starts at the strike platform farthest from where you came in
            SpawnPlatform(zoneId, far.x + 0.5f, far.y + 0.5f);
        }
        public static void Reset() { Current = null; ShockAt.Clear(); Bag.Clear(); }
        public static bool Is(ICombatant u) => Current != null && !Current.Gone && (Current.Unit == u || Current.Sump != null && Current.Sump.Tail == u);
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
            f.Rig.Place(sx, sy, 1f, 0f); f.Rig.Under = true; f.View = new EelSump.State(); FillView(f);
            return Current = f;
        }

        /// <summary>Lays the EelSump spine (140 samples, 4 a tile, head first) along the rig: the head, then its 56 segments; whatever is under the water (the whole body while it lurks, the part away from
        /// the platform while it wraps) gets Z -1, which the view draws as the swimming shadow and the hit tests ignore. Called every tick of the platform fight.</summary>
        public static void FillView(Fight f)
        {
            var v = f.View; if (v == null) return; var r = f.Rig; int n = EelRig.Count, up = r.Under ? 0 : Surfaced(f);
            var px = new float[n + 1]; var py = new float[n + 1]; var pz = new float[n + 1];
            px[0] = r.HX; py[0] = r.HY; pz[0] = up > 0 ? Mathf.Max(0.12f, r.HeadZ) : -1f;
            for (int i = 0; i < n; i++) { px[i + 1] = r.RX[i]; py[i + 1] = r.RY[i]; pz[i + 1] = i < up ? Mathf.Max(0.1f, r.Z[i]) : -1f; }
            bool any = false;
            for (int j = 0; j < EelSump.N; j++)
            {
                float u = j / (float)(EelSump.N - 1) * n; int k = Mathf.Min(n - 1, (int)u); float t = u - k;
                v.X[j] = Mathf.Lerp(px[k], px[k + 1], t); v.Y[j] = Mathf.Lerp(py[k], py[k + 1], t);
                v.Z[j] = pz[k] < 0f || pz[k + 1] < 0f ? -1f : Mathf.Lerp(pz[k], pz[k + 1], t);
                if (v.Z[j] > EelSump.Surface) any = true;
            }
            v.BodyOut = any; v.HeadOut = v.Z[0] > EelSump.Surface;
        }

        /// <summary>The Sump: it lies in the pool until the first shot or step into its room; the body is on the slab (plan digit 8).</summary>
        public static Fight SpawnSump(string zone)
        {
            var c = Marks(zone, '0')[0]; var b = Marks(zone, '8')[0];
            var m = new Mutant(Name, c.x, c.y, Def) { Hp = MaxHp }; EncounterState.Enemies.Add(m);
            var f = new Fight { Unit = m, Level = "sump", Stage = "hidden", CX = c.x + 0.5f, CY = c.y + 0.5f, BodyX = b.x, BodyY = b.y, StageAt = Time.time };
            var suit = ArmorDatabase.Tiers["tier4"].Values.OrderBy(_ => Random.value).First(); f.BodyArmor = suit.Name;        // a random human in tier-4 armour
            f.Body = new HumanEnemy("Drowned crewman", b.x, b.y, "Contractors", "Veteran", null, suit) { Hp = 0 };
            EncounterState.Enemies.Add(f.Body);
            f.Sump = EelSump.Create(zone); f.Sump.Tail = new Mutant(EelSump.TailName, c.x, c.y, EelSump.TailDef) { Hp = EelSump.TailMax }; EncounterState.Enemies.Add(f.Sump.Tail);   // the tail is its own target
            f.Sump.LastHp = m.Hp; f.Sump.LastTailHp = f.Sump.Tail.Hp;
            return Current = f;
        }

        /// <summary>The reward's eel parts (Yan: "eel parts still drop"): what it coughs up with the body - two parts that sell for well above a Tier 3 mutant part (400-600 RU, MutantLootSystem):
        /// a gland (1400-1900 RU) and a spine plate (900-1300 RU). They are in the body's pack with its gear and the story key (still undecided), taken from the corpse like any loot.</summary>
        public static void AddParts(HumanEnemy body)
        {
            if (body == null || body.Inventory == null) return;
            string Id() => "eel_part_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
            body.Inventory.Items.Add(new InventoryItem { Id = Id(), Name = "Drowner Gland", WeightKg = 3f, Qty = 1, Type = "mutantPart", Price = Random.Range(1400, 1901), Category = "3" });
            body.Inventory.Items.Add(new InventoryItem { Id = Id(), Name = "Drowner Spine Plate", WeightKg = 4f, Qty = 1, Type = "mutantPart", Price = Random.Range(900, 1301), Category = "3" });
        }

        /// <summary>The last pump (Main Sump Pump, Depths 3) cannot be run while the eel lies in its pool (ACTION_DESIGN: "it blocks the last pump ... put it down, run the pump"): true = blocked, and says so.
        /// It is "put down" when it has fled (Story flag eel_fled), which also holds for a save that beat it before this gate existed.</summary>
        public static bool BlocksPump(string grantsKey)
        {
            if (grantsKey == null || grantsKey != LabLocks.Key("drain3") || TerrainMap.ZoneId != LabLocks.Depths3 || Story.Has(Fled)) return false;
            GameLog.Write("The pump's panel sits at the edge of the pool, and something huge is circling right beside it. Nobody is touching that panel until it is put down.");
            return true;
        }

        public static void GoStage(Fight f, string stage) => Go(f, stage);
        public static void HurtPublic(Player p, int dmg, string what, string kind) => Hurt(p, dmg, what, kind);
        static void Go(Fight f, string stage) { f.Stage = stage; f.StageAt = Time.time; f.Move = null; f.Hit = false; f.Zones.Clear(); }

        public static void OnDamaged(ICombatant u)
        {
            if (!Is(u)) return;
            var f = Current;
            if (f.Level == "sump" && u == f.Sump.Tail) { EelSump.OnTailHit(f, u); return; }                          // the tail: its own HP; at 0 it lets go and goes down
            f.Unit.IsAggroed = true; f.Started = true;
            if (f.Level == "platform") { u.Hp = System.Math.Max(u.Hp, (int)(MaxHp * (1f - CarryCap))); return; }     // Levels 1-2: at most 20% carries over
            if (f.Stage == "hidden") Go(f, "reveal");
            EelSump.OnHeadHit(f, u);                                                                                 // under the water or busy: nothing lands; at 0 it is taken to the vomit when its move ends
            int ph = PhaseOf(u.Hp);
            if (ph >= 1 && f.Sump != null && !f.Sump.Stage2) { f.Sump.Stage2 = true; f.Sump.Queue.Insert(0, "flood"); }      // 75% HP: the flood surge is the phase transition, and the snare and the flood join the moves
            if (ph >= 2 && f.Sump != null && !f.Sump.Stage3) { f.Sump.Stage3 = true; f.Sump.Queue.Add("storm"); }      // 50% HP: the storm is the third phase's transition, then one of the moves
            if (ph > f.Phase) { f.Phase = ph; f.Enraged = ph >= 2; f.Sump.Bag.Clear(); GameLog.Write(ph >= 2 ? $"The Drowner's wounds open ({ph}): it gets worse." : $"The Drowner's wounds open ({ph})."); Sfx.Play("boss_sting", null, 0.7f, 0f, 1f); }
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
            if (f.Level == "platform") { Platform(f, players, tgt, px, py, now, age, dt); f.TgtX = px; f.TgtY = py; DriveBody(f, dt); FillView(f); }
            else EelSump.Tick(f, players, now, dt);
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
                    if (age >= Burst + ConstrictFor && !f.Discharged) { f.Discharged = true; Sfx.Play("explosion", null, 0.4f, 0.1f, 1.4f); GameLog.Write("The coils crackle with current."); }   // the segments are in place: the discharge
                    f.Label = f.Discharged ? "DISCHARGE held - touching the coils SHOCKS" : age < Burst ? "WRAP: the head races round the platform, the body coils in" : "CONSTRICT: the coil tightens round the platform";
                    f.Zones.Clear(); f.Zones.Add(new Zone { X = f.PX, Y = f.PY, R = RingOut, Kind = "ring", Live = f.Discharged });
                    if (age >= Burst) foreach (var p in players)      // from the end of the burst the coil is a wall; once it is charged, a wall that shocks
                    {
                        var (qx, qy) = Ballistics.Pos(p); float rd = Mathf.Sqrt((qx - f.PX) * (qx - f.PX) + (qy - f.PY) * (qy - f.PY));
                        if (rd < RingIn || rd > RingOut || Rt.Untouchable(p)) continue;                  // inside the platform: safe; on the ring: shocked, thrown back in
                        if (!f.Discharged) { float k0 = 1f / (rd + 1e-3f), over = rd - (RingIn - 0.1f); Rt.MoveBy(p, -(qx - f.PX) * k0 * over, -(qy - f.PY) * k0 * over); f.Label = "CONSTRICT: the coil won't let you through"; continue; }   // before the charge the coil is a wall: it holds you at its edge, no damage
                        if (ShockAt.TryGetValue(p, out var t0) && now - t0 < ShockGap) continue;
                        ShockAt[p] = now;
                        f.Shocked.Add(p); f.Label = "SHOCK: you touched the coils"; Hurt(p, ShockDamage, "The coils shock you", "Psi");
                        float k = rd < 0.01f ? 0f : 1f / rd; Rt.MoveBy(p, (f.PX - qx) * k * 1.6f, (f.PY - qy) * k * 1.6f);
                    }
                    if (age >= Burst + Hold) { Go(f, "release"); f.Discharged = false; f.Label = "it lets go"; }
                    break;
                case "release":
                    if (age >= ReleaseFor) { Go(f, "strike"); f.Label = "STRIKE: wind-up"; var (sx, sy) = Ballistics.Pos(tgt); f.Zones.Add(new Zone { X = sx, Y = sy, R = 1.7f, Kind = "strike" });
                        f.FromX = f.Rig.HX; f.FromY = f.Rig.HY; PullBack(f, sx, sy); f.Discharged = false; }
                    break;
                case "strike":
                    var z = f.Zones.FirstOrDefault();
                    if (z != null && age >= StrikeAt && !f.Hit) { f.Hit = true; z.Live = true; f.Label = "STRIKE!"; foreach (var p in players) Strike(p, z, StrikeDamage, "It strikes out of the water"); }
                    if (age >= StrikeAt + 0.4f) { Go(f, "recover"); f.Label = "it sinks back"; PickEscape(f); }
                    break;
                case "recover":
                    if (age >= 1.4f) { f.Next = now + 1.5f; Go(f, f.Wraps >= 2 ? "done" : "lurk"); }      // it has swum off by itself (DriveBody): SX, SY follow the head
                    break;
                case "done": f.Label = "swims off - the pump is waiting"; break;
            }
        }

        const float Wind = 0.6f;                                  // the platform strike's pull-back (the Sump fight is EelSump)

        static float GuideR(float turnsDone) => RingR - Pitch * turnsDone;          // the coil is a slight INWARD spiral: the outer start is on the outside, the head's end is the innermost loop

        static void SetCoilSlots(Fight f, float sp = 1f)
        {
            var rig = f.Rig; rig.SlotX = new float[EelRig.Count]; rig.SlotY = new float[EelRig.Count]; rig.SlotZ = new float[EelRig.Count];
            float a0 = -Mathf.PI / 2f, th = a0 + 2f * Mathf.PI * Turns, step = EelRig.Spacing * sp; (float x, float y) prev = (f.PX + Mathf.Cos(th) * GuideR(Turns), f.PY + Mathf.Sin(th) * GuideR(Turns)); float acc = 0f; int n = 0;
            for (int guard = 0; n < EelRig.Count && guard < 4000; guard++)
            {
                th -= 0.02f; float rad = Mathf.Max(0.3f, GuideR((th - a0) / (2f * Mathf.PI))); var q = (x: f.PX + Mathf.Cos(th) * rad, y: f.PY + Mathf.Sin(th) * rad);
                acc += Mathf.Sqrt((q.x - prev.x) * (q.x - prev.x) + (q.y - prev.y) * (q.y - prev.y)); prev = q;
                while (n < EelRig.Count && acc >= step * (n + 1)) { rig.SlotX[n] = q.x; rig.SlotY[n] = q.y; n++; }
            }
            for (; n < EelRig.Count; n++) { rig.SlotX[n] = f.PX; rig.SlotY[n] = f.PY; }
            for (int i = 0; i < EelRig.Count; i++) rig.SlotZ[i] = CoilRise * (1f - i / (EelRig.Count - 1f));        // the loops stack: the head end lies on top of the tail end
        }

        /// <summary>After a strike: the dive's direction, pointing OUT: away from the platform, turned (up to 80 degrees) to whichever way keeps furthest from its own body, so it never cuts back through its coil.</summary>
        public static float EscScore;
        static void PickEscape(Fight f)
        {
            var r = f.Rig; float dx = r.HX - f.PX, dy = r.HY - f.PY, l = Mathf.Sqrt(dx * dx + dy * dy); if (l < 0.05f) { dx = 1f; dy = 0f; l = 1f; }
            float baseA = Mathf.Atan2(dy, dx), best = -1f; f.EscX = dx / l; f.EscY = dy / l;
            foreach (var deg in new[] { 0f, 20f, -20f, 40f, -40f, 60f, -60f, 80f, -80f })
            {
                float a = baseA + deg * Mathf.Deg2Rad, ux = Mathf.Cos(a), uy = Mathf.Sin(a), score = 99f;
                for (float d = 0.8f; d <= 6f; d += 0.4f) for (int i = 3; i < EelRig.Count; i++) { float qx = r.HX + ux * d - r.X[i], qy = r.HY + uy * d - r.Y[i]; score = Mathf.Min(score, Mathf.Sqrt(qx * qx + qy * qy)); }
                score -= Mathf.Abs(deg) * 0.004f;                      // all else equal, straight out
                if (score > best) { best = score; f.EscX = ux; f.EscY = uy; }
            }
            EscScore = best;
        }

        /// <summary>The anticipation point: the head backs off from where it is about to strike (1.3 tiles).</summary>
        static void PullBack(Fight f, float tx, float ty)
        {
            float dx = f.FromX - tx, dy = f.FromY - ty, l = Mathf.Sqrt(dx * dx + dy * dy) + 1e-4f; f.PullX = f.FromX + dx / l * 1.3f; f.PullY = f.FromY + dy / l * 1.3f;
        }
        /// <summary>Every tick, after the state machine: where the head is now (a pure function of the stage and its age: easing curves, no animation clips), then the chain follows.</summary>
        static void DriveBody(Fight f, float dt)
        {
            var r = f.Rig; float age = Time.time - f.StageAt, t = Time.time; (float x, float y) head = (r.HX, r.HY); r.SpacingTarget = 1f; r.CompressAll = false; r.LookActive = false; r.HeadLift = 0f;
            if (f.Level == "platform")
            {
                var c = (x: f.PX, y: f.PY); float ringA0 = -Mathf.PI / 2f; r.AnchorOn = f.Stage == "wrap"; r.AnchorFrom = 2; r.AnchorK = 16f * Mathf.Clamp01(age / Burst) * Mathf.Clamp01(age / Burst);   // the coil's slots pull the trailing body in as the burst goes on
                switch (f.Stage)
                {
                    case "lurk": case "done": head = (f.SX, f.SY); r.Under = true; r.SlitherAmount = 0.55f; r.SlitherSpeed = 7f; break;                     // swimming: the wave runs down the whole body
                    case "wind": head = (c.x + 0.3f * Mathf.Sin(age * 22f), c.y + 0.3f * Mathf.Cos(age * 19f)); r.Under = true; r.SlitherAmount = 0.5f; r.SlitherSpeed = 10f; break;
                    case "wrap":
                    {
                        r.Under = false; r.SlitherAmount = f.Discharged ? 0.03f : 0.2f; r.SlitherSpeed = f.Discharged ? 3f : 9f; r.CompressAll = true;
                        float cf = Ease.InOutSine((age - Burst) / ConstrictFor); r.HeadLift = age > Burst + Hold - 0.35f ? 0.8f : 0f;       // just before it lets go the head rears up: it will have to cross its own coil
                        if (age >= Burst)      // the constriction is the spring system alone: the head is anchored and watches you, the rest spacing falls, and the body is pulled in along the spiral
                        {
                            r.SpacingTarget = Mathf.Lerp(1f, ConstrictSpacing, cf); SetCoilSlots(f, r.SpacingNow); r.AnchorK = 16f; r.LookActive = true; r.LookX = f.TgtX; r.LookY = f.TgtY;
                        }
                        (float x, float y) RingAt(float ag)       // the head's path: out of the middle in the first half turn (the radius follows the ANGLE, so the path is long), then round an inward spiral; after the burst it stays where it ended
                        {
                            float done = Turns * Ease.OutCubic(Mathf.Clamp01(ag / Burst)), ang = ringA0 + 2f * Mathf.PI * done, rad = Mathf.Lerp(0.3f, GuideR(done), Ease.OutCubic(done / 0.6f));
                            return (c.x + Mathf.Cos(ang) * rad, c.y + Mathf.Sin(ang) * rad);
                        }
                        const int Sub = 10;                       // the head races (up to 100 tiles/s): sampled finely so it traces the spiral and the body follows the spiral, not the chords
                        for (int k = 1; k <= Sub; k++) { var q = RingAt(age - dt + dt * k / Sub); r.Step(dt / Sub, q.x, q.y); }
                        f.SX = r.HX; f.SY = r.HY; return;
                    }
                    case "release":
                    {
                        float ang = ringA0 + 2f * Mathf.PI * Turns, u = Ease.OutExpo(age / ReleaseFor); r.SlitherAmount = 0.35f; r.SlitherSpeed = 8f; r.HeadLift = 0.8f;
                        float re = GuideR(Turns) + 4f * u; head = (c.x + Mathf.Cos(ang) * re, c.y + Mathf.Sin(ang) * re); break;                                     // the head uncoils outward, the body follows it off the platform
                    }
                    case "strike":
                    {
                        var z = f.Zones.FirstOrDefault(); float zx = z?.X ?? f.Rig.HX, zy = z?.Y ?? f.Rig.HY; r.SlitherAmount = 0.2f; r.SlitherSpeed = 6f;
                        r.HeadLift = age >= 0.25f ? 0.8f : 0f;                                                                                       // the dart goes OVER its own coil, not through it
                        r.SpacingTarget = age < Wind ? 0.45f : age < StrikeAt + 0.1f ? 1.22f : 1f;                                                          // bunched up like a spring while it draws back, then stretched as it strikes
                        if (age < Wind) head = Ease.Lerp((f.FromX, f.FromY), (f.PullX, f.PullY), Ease.InBack(age / Wind));                            // 0.6 s: it draws back (and dips the wrong way first)
                        else head = Ease.Lerp((f.PullX, f.PullY), (zx, zy), Ease.OutExpo((age - Wind) / 0.15f)); break;                               // then it is just there
                    }
                    case "recover":
                    {
                        r.SlitherAmount = 0.5f; r.SlitherSpeed = 7f;
                        r.HeadLift = age < 0.7f ? 0.8f : 0f;                                                   // and the dive too, until it is clear of the loops and under the water
                        r.SpacingTarget = age < 0.5f ? 1.15f : 1f;                                             // let go of the tension at once: no bunching up on the way down
                        float spd = 14f * (1f - Ease.InOutSine(age / 1f)) + 3.4f;                              // a fast dive that eases into a swim; the mass and the drag pull the loosened body straight after it
                        head = (r.HX + f.EscX * spd * dt, r.HY + f.EscY * spd * dt); r.Under = age > 0.35f; break;
                    }
                }
                if (f.Stage != "lurk") { f.SX = head.x; f.SY = head.y; }
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
