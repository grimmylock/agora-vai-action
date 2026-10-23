// Real-time fork, batch 1 (ACTION_DESIGN.md "Weapons"): how a gun handles, and one round fired for real.
// Per class: accuracy (spread once steadied), hip spread, handling (seconds to steady), recoil (degrees of kick a shot),
// recovery (degrees a second it settles back), fire rate, automatic or not, reach. Pellets for shotguns.
// Moving widens the spread (walking a little, sprinting a lot); crouching tightens it; kicks add up and recover.
// NPCs fire through the same code: their rank decides how steady they are and how often they go for the head.
// RT batch 6d (Yan: "every assault rifle is just firing at the same fire-rate, same reload speed, same accuracy"): every gun
// has its own fire rate, fire mode, reload, accuracy, handling and kick (Guns below); the class row keeps hip spread,
// recovery and reach. Numbers are a first pass from each gun's build (calibre, weight, action, magazine) - Unity tuning next.
using System.Collections.Generic;
using System.Linq;
using GameData;

namespace GameCore
{
    public class Handling
    {
        public float Steady, Hip, SteadyTime, Kick, Recover, Rpm, Reach, ReloadTime, Move = 1f, Choke;
        public bool Auto;
        public int Pellets = 1;
    }

    public static class Gunplay
    {
        // class: steadied spread, hip spread (degrees), seconds to steady, kick, recovery deg/s, rounds/min, auto, reach (tiles), reload s,
        // moving (x the spread walking or sprinting adds: RT 6d, SMGs and pistols shoot on the move, long guns don't)
        static readonly Dictionary<string, float[]> Table = new Dictionary<string, float[]>
        {
            ["sidearm"] = new[] { 1.2f, 3.5f, 0.45f, 2.6f, 14f, 320f, 0f, 22f, 1.3f, 0.6f },
            ["smg"]     = new[] { 1.9f, 3f, 0.4f, 1.5f, 16f, 720f, 1f, 24f, 1.7f, 0.5f },
            ["assault"] = new[] { 0.7f, 5f, 0.7f, 1.9f, 11f, 600f, 1f, 40f, 2.0f, 1f },
            ["sniper"]  = new[] { 0.12f, 8f, 1.2f, 7f, 5f, 50f, 0f, 70f, 2.6f, 1.6f },
            ["shotgun"] = new[] { 1.4f, 4f, 0.5f, 6f, 9f, 75f, 0f, 14f, 2.4f, 0.8f },
            ["heavy"]   = new[] { 1.4f, 7f, 0.9f, 2.2f, 8f, 560f, 1f, 40f, 3.2f, 1.5f },
        };

        // RT 6d: a shotgun's pellets leave as one shot (the aim spread moves the whole pattern) and spread round it by the choke, degrees
        public static readonly Dictionary<string, float> Choke = new Dictionary<string, float> { ["Obrez-66"] = 2.4f, ["Sever-34 O/U"] = 1.1f, ["Halden Lever Shotgun"] = 1.8f };

        // per gun: rounds/min, automatic, reload s, steadied spread (deg), seconds to steady, kick (deg a shot)
        public static readonly Dictionary<string, (float rpm, bool auto, float reload, float steady, float steadyTime, float kick)> Guns =
            new Dictionary<string, (float, bool, float, float, float, float)>
        {
            // sidearms: quick to raise; revolvers slow to reload, magnums kick
            ["PSh-18 Pchela"]           = (300, false, 1.4f, 1.4f, 0.40f, 2.2f),
            ["Walden P9"]               = (360, false, 1.2f, 1.1f, 0.40f, 2.2f),
            ["Halden .45 Service"]      = (300, false, 1.3f, 1.0f, 0.45f, 3.0f),
            ["Halden Service Revolver"] = (220, false, 2.6f, 1.0f, 0.50f, 3.2f),
            ["SR-21 Gadyuka"]           = (340, false, 1.3f, 1.1f, 0.45f, 2.6f),
            ["Black Harrier"]           = (260, false, 1.5f, 0.9f, 0.50f, 4.4f),
            ["Corvan 5.7"]              = (380, false, 1.2f, 1.0f, 0.35f, 1.8f),
            ["Rinaldi R6"]              = (240, false, 2.4f, 0.8f, 0.50f, 3.4f),
            ["Steppe Eagle"]            = (200, false, 1.6f, 0.9f, 0.60f, 6.0f),
            ["RK-12 Kuvalda"]           = (150, false, 3.0f, 0.8f, 0.65f, 7.5f),
            // SMGs: fast, light kick, wide
            ["Vosa vz. 62"]             = (850, true, 1.5f, 2.2f, 0.30f, 1.1f),
            ["PPZ Zubr"]                = (680, true, 2.4f, 1.8f, 0.45f, 1.2f),
            ["Corvan Wasp-9"]           = (800, true, 1.8f, 1.4f, 0.40f, 1.2f),
            ["Corvan Hornet .45"]       = (600, true, 1.9f, 1.6f, 0.45f, 2.0f),
            ["Corvan Gnat PDW"]         = (950, true, 1.7f, 1.5f, 0.30f, 1.0f),
            // rifles: short ones handle fast and spread; long and heavy ones steady slower and shoot tighter
            ["VKS-74U"]                 = (700, true, 1.9f, 1.1f, 0.50f, 2.2f),
            ["Smerch-3"]                = (800, true, 1.8f, 0.9f, 0.50f, 2.3f),
            ["VK-74"]                   = (620, true, 2.1f, 0.7f, 0.70f, 1.7f),
            ["VKM"]                     = (600, true, 2.1f, 0.8f, 0.70f, 2.4f),
            ["TS-9 Tishina"]            = (850, true, 2.0f, 0.75f, 0.60f, 2.0f),
            ["TR-30"]                   = (700, true, 2.0f, 0.6f, 0.70f, 1.7f),
            ["Halden HR36K"]            = (720, true, 2.0f, 0.65f, 0.60f, 1.6f),
            ["VK-103"]                  = (600, true, 2.1f, 0.7f, 0.75f, 2.3f),
            ["BR-86 Bulldog"]           = (650, true, 2.6f, 0.55f, 0.85f, 1.5f),
            ["VK-74 Obelisk"]           = (650, true, 2.0f, 0.5f, 0.55f, 1.6f),
            ["NA-94 Yenisei"]           = (600, true, 2.2f, 0.55f, 0.80f, 1.4f),
            ["Corvan Bullpup"]          = (750, true, 2.3f, 0.55f, 0.65f, 1.6f),
            ["Corvan Battle Rifle"]     = (600, true, 2.4f, 0.5f, 0.85f, 3.0f),
            // snipers: semis fire faster and wander more; bolts are the tightest
            ["SKN Karabin"]             = (120, false, 2.4f, 0.30f, 0.9f, 4.5f),
            ["Frontier Lever Rifle"]    = (70, false, 3.2f, 0.25f, 0.9f, 5.0f),
            ["VSN Shepot"]              = (150, false, 2.2f, 0.30f, 0.8f, 3.5f),
            ["M91 Trekhlineyka"]        = (40, false, 3.4f, 0.10f, 1.2f, 7.0f),
            ["Nightwalker .45-70"]      = (55, false, 3.0f, 0.20f, 1.1f, 8.0f),
            ["Corvan Marksman"]         = (110, false, 2.5f, 0.15f, 1.1f, 5.0f),
            ["SV-2 Strela"]             = (100, false, 2.6f, 0.15f, 1.2f, 6.0f),
            ["Sever-98"]                = (45, false, 2.8f, 0.08f, 1.3f, 7.0f),
            ["VK-127 Grom"]             = (40, false, 3.0f, 0.10f, 1.4f, 9.0f),
            ["Halden AM-50 Sledge"]     = (70, false, 3.4f, 0.10f, 1.8f, 11f),
            ["G-7 Rail Prototype"]      = (40, false, 3.0f, 0.05f, 1.4f, 4.0f),
            // shotguns (spread = where the pattern goes; Choke = how wide it is): pumps and levers slow, semis and doubles quick
            ["Obrez-66"]                = (200, false, 2.0f, 1.8f, 0.35f, 8.0f),
            ["Halden Lever Shotgun"]    = (70, false, 3.0f, 1.4f, 0.50f, 6.0f),
            ["Hound 13"]                = (75, false, 2.6f, 1.3f, 0.50f, 6.0f),
            ["Ranger P8"]               = (80, false, 2.8f, 1.2f, 0.50f, 6.0f),
            ["Bayou Pump"]              = (75, false, 3.0f, 1.2f, 0.55f, 6.0f),
            ["Sever-34 O/U"]            = (150, false, 2.2f, 1.0f, 0.50f, 7.0f),
            ["Tambour-12"]              = (180, false, 4.0f, 1.5f, 0.60f, 6.5f),
            ["SP-14 Warden"]            = (200, false, 3.2f, 1.3f, 0.55f, 5.5f),
            ["Sayan-12"]                = (180, false, 2.2f, 1.5f, 0.55f, 6.0f),
            ["Kaban-12"]                = (200, false, 2.4f, 1.3f, 0.60f, 5.5f),
            // machine guns: slow to steady, long belt changes
            ["PL-62 Kobza"]             = (650, true, 4.0f, 1.4f, 0.90f, 2.2f),
            ["Corvan LMG"]              = (750, true, 4.5f, 1.3f, 0.90f, 1.9f),
            ["RL-44"]                   = (600, true, 4.2f, 1.2f, 1.00f, 2.4f),
            ["PL-74"]                   = (650, true, 4.5f, 1.1f, 1.00f, 2.6f),
            ["PL-74P Polovets"]         = (650, true, 4.5f, 1.0f, 1.00f, 2.5f),
            ["PL-74P Polovets Zulu"]    = (650, true, 4.5f, 0.8f, 1.10f, 2.4f),
        };

        // RT 6h (Yan: "individual bullet reloading for revolvers and lever actions/ shotguns"): tube shotguns, levers, revolvers
        // and the bolt rifles without a box magazine load a round at a time; shooting stops a tube/lever/bolt reload with what's in,
        // a revolver finishes the round in hand and closes first. The per-gun reload time is a full load from empty.
        public static bool ByRound(Weapon w) { var r = WeaponFeel.For(w?.Db).Reload; return r == "tube" || r == "cylinder"; }
        public static bool Revolver(Weapon w) => WeaponFeel.For(w?.Db).Reload == "cylinder";
        public const float CylinderOpen = 0.4f;               // swing it out, dump the brass
        public const float BreakOpenClose = 0.6f;             // RT 6k: a break-action opens, dumps and closes; the rest is per shell fired

        /// <summary>A whole-magazine reload's length (RT 6k: a break-action's depends on how many shells it fired).</summary>
        public static float ReloadSeconds(Weapon w)
        {
            float full = For(w).ReloadTime;
            if (WeaponFeel.For(w?.Db).Reload != "break") return full;
            int cap = System.Math.Max(1, w.MagCapacity);
            return BreakOpenClose + (cap - w.CurrentMagazine) * (full - BreakOpenClose) / cap;
        }
        public static float PerRound(Weapon w) =>
            System.Math.Max(0.25f, System.Math.Min(0.6f, (For(w).ReloadTime - (Revolver(w) ? CylinderOpen : 0f)) / System.Math.Max(1, w.MagCapacity)));

        public const float MaxBloom = 10f;                   // degrees of kick at most, however long the trigger is held

        public static Handling For(Weapon w)
        {
            string cat = CombatRules.Category(w?.Db);
            if (!Table.TryGetValue(cat, out var t)) t = Table["sidearm"];
            var h = new Handling { Steady = t[0], Hip = t[1], SteadyTime = t[2], Kick = t[3], Recover = t[4], Rpm = t[5], Auto = t[6] > 0f, Reach = t[7], ReloadTime = t[8], Move = t[9] };
            if (w?.Db != null)
            {
                if (Guns.TryGetValue(w.Db.Name, out var g)) { h.Rpm = g.rpm; h.Auto = g.auto; h.ReloadTime = g.reload; h.Steady = g.steady; h.SteadyTime = g.steadyTime; h.Kick = g.kick; }
                if (CombatRules.IsShotgun(w)) { h.Pellets = 6; h.Choke = Choke.TryGetValue(w.Db.Name, out var c) ? c : 1.5f; }
            }
            if (ItemCondition.IsDamaged(w?.Condition ?? ItemCondition.Working)) h.Steady *= 1.4f;
            return h;
        }

        /// <summary>The live state of one shooter's aim: how steady it is and how much kick it still carries.</summary>
        public class Aim
        {
            public float Steady;          // 0 = hip, 1 = fully steadied
            public float Bloom;           // degrees of kick not yet recovered
            public float NextShot;        // time the gun can fire again
            public float ReloadUntil = -1f;
            public int Loading;           // RT 6h: rounds still to go in, one at a time (0 = a whole-magazine reload, or none)
        }
        static readonly Dictionary<ICombatant, Aim> Aims = new Dictionary<ICombatant, Aim>();
        public static Aim AimOf(ICombatant u) { if (!Aims.TryGetValue(u, out var a)) Aims[u] = a = new Aim(); return a; }
        public static void Reset() { Aims.Clear(); OffAims.Clear(); }
        static readonly Dictionary<ICombatant, Aim> OffAims = new Dictionary<ICombatant, Aim>();
        /// <summary>RT 7z-c: a Gunslinger's left-hand gun keeps its own steadiness, kick, fire rate and reload.</summary>
        public static Aim OffAimOf(ICombatant u) { if (!OffAims.TryGetValue(u, out var a)) OffAims[u] = a = new Aim(); return a; }

        /// <summary>RT 7z (Yan: "weapon dispersion seems very high even for the starter pistol ... past a few 'me's' bullets
        /// already fly high or low"): the player's spread is halved. The degrees were set when one shot was a turn; in real time a
        /// hip-fired pistol hit 24% at 8 tiles (Program.cs AccuracyProbe). Enemies keep theirs (NpcAim) - Yan's rule 6.</summary>
        public const float PlayerSpread = 0.5f;

        /// <summary>The player's spread (degrees, one standard deviation) right now. move: 0 still, 1 walking, 2 sprinting.</summary>
        public static float Spread(Handling h, Aim a, int move, bool crouched)
        {
            float s = h.Hip + (h.Steady - h.Hip) * a.Steady;
            s += (move == 1 ? 1.2f : move >= 2 ? 6f : 0f) * h.Move;
            if (crouched) s *= 0.8f;
            return (s + a.Bloom) * PlayerSpread;
        }

        /// <summary>Per frame: steady up while aiming (and not sprinting), the kick settles back.</summary>
        public static void Tick(Handling h, Aim a, bool aiming, int move, float dt)
        {
            float target = aiming && move < 2 ? 1f : 0.25f;
            float rate = 1f / System.Math.Max(0.1f, h.SteadyTime);
            a.Steady = a.Steady < target ? System.Math.Min(target, a.Steady + rate * dt) : System.Math.Max(target, a.Steady - 2f * rate * dt);
            a.Bloom = System.Math.Max(0f, a.Bloom - h.Recover * dt);
        }

        static float Gauss() { float u1 = System.Math.Max(1e-6f, UnityEngine.Random.value), u2 = UnityEngine.Random.value; return (float)(System.Math.Sqrt(-2.0 * System.Math.Log(u1)) * System.Math.Cos(6.2831853 * u2)); }

        /// <summary>The bodies a round can hit: you and everyone on the map.</summary>
        /// <summary>What a shot or a blade from `shooter` can hit. Yan: "turn friendly fire off for now for players, make it so bullets
        /// go through allied players" - a player's rounds, swings and throws pass through the other players.</summary>
        public static IEnumerable<ICombatant> Targets(ICombatant shooter) => shooter is Player ? Bodies().Where(u => !(u is Player)) : Bodies();

        /// <summary>On the same side: players together; people of one faction or of factions at peace.</summary>
        public static bool Allies(ICombatant a, ICombatant b) =>
            a is Player && b is Player
            || a is HumanEnemy x && b is HumanEnemy y && (x.FactionName == y.FactionName || !FactionDatabase.AreHostile(x.FactionName, y.FactionName));

        /// <summary>Yan: "enemies make no effort to not shoot their friends ... they need to at least try to not shoot each other in the
        /// back": a living ally stands within ~half a tile of the line from the shooter to the target, before the target.</summary>
        public static bool AllyInLine(ICombatant shooter, ICombatant target)
        {
            var (sx, sy) = Ballistics.Pos(shooter); var (tx, ty) = Ballistics.Pos(target);
            float dx = tx - sx, dy = ty - sy, len = (float)System.Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.01f) return false;
            dx /= len; dy /= len;
            foreach (var u in Bodies())
            {
                if (u == shooter || u == target || !u.IsAlive() || !Allies(shooter, u)) continue;
                var (ux, uy) = Ballistics.Pos(u);
                float t = (ux - sx) * dx + (uy - sy) * dy, side = (ux - sx) * dy - (uy - sy) * dx;
                if (t > 0.3f && t < len && System.Math.Abs(side) < 0.6f) return true;
            }
            return false;
        }

        public static IEnumerable<ICombatant> Bodies()
        {
            foreach (var p in Player.All) yield return p;                  // co-op: every player has a body rounds can hit
            foreach (var e in EncounterState.Enemies) yield return e;
        }

        /// <summary>One trigger pull: rounds leave the muzzle, scattered by `spread` degrees sideways and up/down, at aim
        /// height h. Ammo, jams and the fire rate are the caller's (PlayerShoot / NpcShoot). Returns the rounds' hits.</summary>
        public static List<Ballistics.Hit> Shoot(ICombatant shooter, Weapon w, float dx, float dy, float h, float spread, bool ambush, float delay = 0f)
        {
            var hits = new List<Ballistics.Hit>();
            var hd = For(w);
            var (x0, y0) = Ballistics.Pos(shooter);
            float aimSide = Gauss() * spread, aimUp = Gauss() * spread;
            for (int p = 0; p < hd.Pellets; p++)
            {
                float side = (aimSide + Gauss() * hd.Choke) * 0.0174533f, up = (aimUp + Gauss() * hd.Choke) * 0.0174533f;
                float c = (float)System.Math.Cos(side), s = (float)System.Math.Sin(side);
                float rx = dx * c - dy * s, ry = dx * s + dy * c;
                // up/down scatter lands as a height change at the range the round meets a body: approximated per trace
                var probe = Ballistics.Trace(shooter, x0, y0, rx, ry, hd.Reach, h, Targets(shooter));
                float hh = h + (float)System.Math.Tan(up) * probe.Dist / Ballistics.BodyTiles;
                var hit = System.Math.Abs(hh - h) < 0.02f ? probe : Ballistics.Trace(shooter, x0, y0, rx, ry, hd.Reach, hh, Targets(shooter));
                Resolve(shooter, w, hit, ambush, p == 0, delay, hd.Pellets);
                hits.Add(hit);
                if (p == 0 && !(shooter is Player))
                    foreach (var pl in Player.All)                                                  // co-op: whoever it just missed
                        if (hit.Unit != pl && NearMiss(x0, y0, hit.EndX, hit.EndY, pl))
                        {
                            Rt.AddFocus(pl, CombatantUtil.HasPerk(pl, "ColdBlood") ? 12f : 6f);    // RT batch 3: a round that just missed you
                            Rt.Of(pl).PinnedUntil = UnityEngine.Time.time + 1.5f;                  // RT batch 4: and it pins you
                        }
                if (p == 0 && shooter is Player && Rt.On)                                          // RT batch 4: your near misses pin them
                    foreach (var e in EncounterState.Enemies)
                        if (e != hit.Unit && e.IsAlive() && NearMiss(x0, y0, hit.EndX, hit.EndY, e)) RtAi.Suppress(e);
            }
            if (!CombatRules.IsMelee(w)) Noise.Make(shooter.X, shooter.Y, w.Db != null && w.Db.InnateSuppressor ? Noise.Suppressed : Noise.Shot, shooter);
            return hits;
        }

        /// <summary>Damage (armor as before: CombatRules.PhysicalDamage), aggro, death hooks and the shot's effects.</summary>
        static void Resolve(ICombatant shooter, Weapon w, Ballistics.Hit hit, bool ambush, bool first, float delay, int pellets)
        {
            var fx = new FxEvent { Kind = "shot", Actor = shooter, Weapon = w, Now = true, Delay = delay, Pellet = !first,
                EndX = hit.EndX, EndY = hit.EndY, FromX = shooter.X, FromY = shooter.Y };
            if (hit.CoverX >= 0)
            {
                fx.CoverX = hit.CoverX; fx.CoverY = hit.CoverY; fx.CoverMat = Materials.At(hit.CoverX, hit.CoverY);
                fx.CoverHit = hit.Cover || hit.Wall; fx.Through = hit.Through < 1f;
                if (hit.Cover || hit.Through < 1f) TerrainMap.DamageTile(hit.CoverX, hit.CoverY, pellets > 1 ? 1 : 1, shooter);
            }
            var target = hit.Unit;
            if (target != null)
            {
                fx.Target = target; fx.ToX = target.X; fx.ToY = target.Y; fx.Hit = true;
                // RT batch 6: a helmet can turn the round away
                if (hit.Part == "Head" && UnityEngine.Random.value < HeadGear.DeflectChance(target, w))
                {
                    fx.Hit = false; fx.CoverHit = true; fx.CoverX = target.X; fx.CoverY = target.Y; fx.CoverMat = Material.Steel;
                    Body.Concuss(target, 2f);
                    if (first) GameLog.Write($"The round pings off {(target is Player ? "your" : target.Name + "'s")} helmet!");
                    CombatFx.Add(fx);
                    return;
                }
                float raw = RoundDamage(w, hit.Part, hit.Dist, pellets) * hit.Through;
                if (ambush) raw *= 1.3f;                                       // they never saw it coming (Batch 64, as a bonus now)
                if (ItemCondition.IsDamaged(w.Condition)) raw *= 0.75f;
                if (shooter is Player) raw *= CombatRules.PlayerDamage;
                raw *= CombatRules.Lethality;
                float ignore = w.Db?.IgnorePhysDRPercent ?? 0f;
                if (shooter is Player sp && sp.ArtifactBypassPhysDR > 0) ignore = 1f - (1f - ignore) * (1f - sp.ArtifactBypassPhysDR);
                float dr = hit.Part == "Head" ? HeadGear.Of(HeadGear.ArmorOf(target)).dr : target.PhysDR * CombatRules.ArmorScale(target);   // RT batch 6: helmets
                int dmg = CombatRules.PhysicalDamage(raw, w.CurrentAmmoType, dr, ignore);
                int before = target.Hp;
                target.Hp = System.Math.Max(0, target.Hp - dmg);
                fx.Killed = !target.IsAlive(); fx.Overkill = dmg - before;
                if (first || pellets == 1) GameLog.Write($"{Who(shooter)} hits {Who(target)} in the {hit.Part.ToLowerInvariant()} for {dmg}.");
                TestingGround.Note(target, dmg, hit.Part);                    // the testing ground's damage numbers
                if (target is Mutant tm && !TestingGround.IsDummy(tm)) { tm.IsAggroed = true; if (!(target is HumanEnemy)) tm.DamageTakenThisRound += dmg; }
                if (shooter is Player && target is HumanEnemy he && !TestingGround.IsDummy(he)) foreach (var o in EnemyAI.Squad(he)) if (o.IsAlive()) o.IsAggroed = true;
                if (target.IsAlive()) Body.Hit(target, hit.Part, hit.Side, dmg, w.CurrentAmmoType, shooter is Player && Rt.SlowMo ? 1.5f : 1f);   // RT batch 3: bullet time wounds more
                if (shooter is Player sf) Rt.AddFocus(sf, target.IsAlive() ? 4f : 25f);   // RT batch 2: limb health, wounds, down
                if (!target.IsAlive()) Weapon.OnTargetKilled(shooter, target);
                if (target is Player p && p.IsAlive()) Perks.TryAdrenaline(p);
            }
            CombatFx.Add(fx);
        }

        // RT batch 6 (Yan's targets, weapon vs armor of its own tier: rifles 2-4 head hits, snipers 1 head / 2 chest, pistols 2-4,
        // SMGs 4-6, shotguns 1 to many by range, machine guns like rifles): the per-round damage was tuned for one shot per turn,
        // so each class gets a real-time multiplier. Shotguns fade with range: full to 4 tiles, down to 30% at 12.
        public static readonly Dictionary<string, float> ClassDamage = new Dictionary<string, float>
            { ["sidearm"] = 1f, ["smg"] = 0.8f, ["assault"] = 1f, ["sniper"] = 1.8f, ["shotgun"] = 1f, ["heavy"] = 1f };

        /// <summary>One round's (or one pellet's) damage before armor: base x class x part x shotgun range fall-off.</summary>
        public static float RoundDamage(Weapon w, string part, float dist, int pellets)
        {
            string cat = CombatRules.Category(w?.Db);
            float raw = (w.BaseDamage + (w.Upgrades.Contains("Reinforced") ? 30 : 0)) * (ClassDamage.TryGetValue(cat, out var m) ? m : 1f) * Ballistics.PartMult(part);
            if (pellets > 1) raw = raw / pellets * 1.5f * (dist <= 4f ? 1f : System.Math.Max(0.3f, 1f - (dist - 4f) / 8f * 0.7f));
            return raw;
        }

        /// <summary>The round's path (x0,y0)-(x1,y1) passed within 0.6 tiles of the unit.</summary>
        public static bool NearMiss(float x0, float y0, float x1, float y1, ICombatant u)
        {
            var (ux, uy) = Ballistics.Pos(u);
            float vx = x1 - x0, vy = y1 - y0, l2 = vx * vx + vy * vy;
            if (l2 < 1e-4f) return false;
            float t = System.Math.Max(0f, System.Math.Min(1f, ((ux - x0) * vx + (uy - y0) * vy) / l2));
            float dx = x0 + vx * t - ux, dy = y0 + vy * t - uy;
            return dx * dx + dy * dy < 0.36f;
        }

        static string Who(ICombatant u) => u is Player ? "You" : u.Name;

        /// <summary>Where an NPC aims: at the body, or (sometimes, more with rank) at the head; how steady it holds.</summary>
        public static (float h, float spread) NpcAim(ICombatant shooter, Weapon w, ICombatant target)
        {
            int r = shooter is HumanEnemy he ? CombatRules.RankIndex(he.Rank) : 0;      // 0 Rookie .. 6 Legend
            float h = UnityEngine.Random.value < 0.08f + 0.07f * r ? 0.8f : 0.45f;
            var hd = For(w);
            float spread = hd.Steady * (2.6f - 0.25f * r) + 0.8f + (shooter.StatusEffects?.Suppressed == true ? 2.5f : 0f);
            if (shooter.StatusEffects?.Wound == "Arm") spread += 1.5f;
            if (Body.Concussed(shooter)) spread += 2f;
            return (h, spread);
        }

        /// <summary>The bridge while enemies still think in turns (EnemyAI.TakeTurn every few seconds, RT batch 4 replaces
        /// it): Weapon.Fire hands NPC gunfire here, and each round is a real one at the target.</summary>
        public static void NpcShoot(ICombatant shooter, Weapon w, ICombatant target, int rounds)
        {
            if (Body.Down(target)) return;                     // RT batch 2: they leave someone who's down to bleed
            if (AllyInLine(shooter, target)) return;           // a friend in the way: hold fire
            var (sx, sy) = Ballistics.Pos(shooter); var (tx, ty) = Ballistics.Pos(target);
            float dx = tx - sx, dy = ty - sy, len = (float)System.Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.01f) return;
            dx /= len; dy /= len;
            var (h, spread) = NpcAim(shooter, w, target);
            var hd = For(w);
            for (int i = 0; i < rounds; i++)
                Shoot(shooter, w, dx, dy, h, spread + i * hd.Kick * 0.6f, false, i * 60f / System.Math.Max(60f, hd.Rpm));
        }

        /// <summary>RT batch 4: one trigger pull by an NPC fighting in real time (RtAi), extra = spread from moving or being pinned.</summary>
        public static void NpcShootOnce(ICombatant shooter, Weapon w, ICombatant target, float extra)
        {
            var (sx, sy) = Ballistics.Pos(shooter); var (tx, ty) = Ballistics.Pos(target);
            float dx = tx - sx, dy = ty - sy, len = (float)System.Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.01f) return;
            var (h, spread) = NpcAim(shooter, w, target);
            Shoot(shooter, w, dx / len, dy / len, h, spread + extra, false);
        }
    }
}
