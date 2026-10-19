// Real-time fork, batch 1 (ACTION_DESIGN.md "Weapons"): how a gun handles, and one round fired for real.
// Per class: accuracy (spread once steadied), hip spread, handling (seconds to steady), recoil (degrees of kick a shot),
// recovery (degrees a second it settles back), fire rate, automatic or not, reach. Pellets for shotguns.
// Moving widens the spread (walking a little, sprinting a lot); crouching tightens it; kicks add up and recover.
// NPCs fire through the same code: their rank decides how steady they are and how often they go for the head.
// ponytail: per class, with High Recoil and suppressors read off the data; a per-gun table when 3-4 guns get tuned by feel.
using System.Collections.Generic;
using System.Linq;
using GameData;

namespace GameCore
{
    public class Handling
    {
        public float Steady, Hip, SteadyTime, Kick, Recover, Rpm, Reach, ReloadTime;
        public bool Auto;
        public int Pellets = 1;
    }

    public static class Gunplay
    {
        // class: steadied spread, hip spread (degrees), seconds to steady, kick, recovery deg/s, rounds/min, auto, reach (tiles), reload s
        static readonly Dictionary<string, float[]> Table = new Dictionary<string, float[]>
        {
            ["sidearm"] = new[] { 1.2f, 4f, 0.45f, 2.6f, 14f, 320f, 0f, 22f, 1.3f },
            ["smg"]     = new[] { 1.9f, 5f, 0.4f, 1.5f, 16f, 720f, 1f, 24f, 1.7f },
            ["assault"] = new[] { 0.7f, 4.5f, 0.7f, 1.9f, 11f, 600f, 1f, 40f, 2.0f },
            ["sniper"]  = new[] { 0.12f, 7f, 1.2f, 7f, 5f, 50f, 0f, 70f, 2.6f },
            ["shotgun"] = new[] { 3.5f, 6f, 0.5f, 6f, 9f, 75f, 0f, 14f, 2.4f },
            ["heavy"]   = new[] { 1.4f, 6f, 0.9f, 2.2f, 8f, 560f, 1f, 40f, 3.2f },
        };

        public static Handling For(Weapon w)
        {
            string cat = CombatRules.Category(w?.Db);
            if (!Table.TryGetValue(cat, out var t)) t = Table["sidearm"];
            var h = new Handling { Steady = t[0], Hip = t[1], SteadyTime = t[2], Kick = t[3], Recover = t[4], Rpm = t[5], Auto = t[6] > 0f, Reach = t[7], ReloadTime = t[8] };
            if (w?.Db != null)
            {
                if (w.Db.HighRecoil) { h.Kick *= 1.4f; h.Recover *= 0.85f; }
                if (cat == "assault" || cat == "smg") h.Auto = w.Db.BurstCapable;      // a semi-only rifle stays semi
                if (cat == "assault" && !h.Auto) h.Rpm = 240f;
                if (CombatRules.IsShotgun(w)) h.Pellets = 6;
                if (w.Db.ReloadApCost >= 2) h.ReloadTime *= 1.4f;
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
        }
        static readonly Dictionary<ICombatant, Aim> Aims = new Dictionary<ICombatant, Aim>();
        public static Aim AimOf(ICombatant u) { if (!Aims.TryGetValue(u, out var a)) Aims[u] = a = new Aim(); return a; }
        public static void Reset() => Aims.Clear();

        /// <summary>Spread (degrees, one standard deviation) right now. move: 0 still, 1 walking, 2 sprinting.</summary>
        public static float Spread(Handling h, Aim a, int move, bool crouched)
        {
            float s = h.Hip + (h.Steady - h.Hip) * a.Steady;
            s += move == 1 ? 1.2f : move >= 2 ? 6f : 0f;
            if (crouched) s *= 0.8f;
            return s + a.Bloom;
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
        public static IEnumerable<ICombatant> Bodies()
        {
            if (Player.Current != null) yield return Player.Current;
            foreach (var e in EncounterState.Enemies) yield return e;
        }

        /// <summary>One trigger pull: rounds leave the muzzle, scattered by `spread` degrees sideways and up/down, at aim
        /// height h. Ammo, jams and the fire rate are the caller's (PlayerShoot / NpcShoot). Returns the rounds' hits.</summary>
        public static List<Ballistics.Hit> Shoot(ICombatant shooter, Weapon w, float dx, float dy, float h, float spread, bool ambush, float delay = 0f)
        {
            var hits = new List<Ballistics.Hit>();
            var hd = For(w);
            var (x0, y0) = Ballistics.Pos(shooter);
            for (int p = 0; p < hd.Pellets; p++)
            {
                float side = Gauss() * spread * 0.0174533f, up = Gauss() * spread * 0.0174533f;
                float c = (float)System.Math.Cos(side), s = (float)System.Math.Sin(side);
                float rx = dx * c - dy * s, ry = dx * s + dy * c;
                // up/down scatter lands as a height change at the range the round meets a body: approximated per trace
                var probe = Ballistics.Trace(shooter, x0, y0, rx, ry, hd.Reach, h, Bodies());
                float hh = h + (float)System.Math.Tan(up) * probe.Dist / Ballistics.BodyTiles;
                var hit = System.Math.Abs(hh - h) < 0.02f ? probe : Ballistics.Trace(shooter, x0, y0, rx, ry, hd.Reach, hh, Bodies());
                Resolve(shooter, w, hit, ambush, p == 0, delay, hd.Pellets);
                hits.Add(hit);
                if (p == 0 && !(shooter is Player) && hit.Unit != Player.Current && Player.Current != null && NearMiss(x0, y0, hit.EndX, hit.EndY, Player.Current))
                    Rt.AddFocus(CombatantUtil.HasPerk(Player.Current, "ColdBlood") ? 12f : 6f);   // RT batch 3: a round that just missed you
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
                float raw = (w.BaseDamage + (w.Upgrades.Contains("Reinforced") ? 3 : 0)) * Ballistics.PartMult(hit.Part) * hit.Through / pellets * (pellets > 1 ? 1.5f : 1f);
                if (ambush) raw *= 1.3f;                                       // they never saw it coming (Batch 64, as a bonus now)
                if (ItemCondition.IsDamaged(w.Condition)) raw *= 0.75f;
                if (shooter is Player) raw *= CombatRules.PlayerDamage;
                raw *= CombatRules.Lethality;
                float ignore = w.Db?.IgnorePhysDRPercent ?? 0f;
                if (shooter is Player sp && sp.ArtifactBypassPhysDR > 0) ignore = 1f - (1f - ignore) * (1f - sp.ArtifactBypassPhysDR);
                int dmg = CombatRules.PhysicalDamage(raw, w.CurrentAmmoType, target.PhysDR * CombatRules.ArmorScale(target), ignore);
                int before = target.Hp;
                target.Hp = System.Math.Max(0, target.Hp - dmg);
                fx.Killed = !target.IsAlive(); fx.Overkill = dmg - before;
                if (first || pellets == 1) GameLog.Write($"{Who(shooter)} hits {Who(target)} in the {hit.Part.ToLowerInvariant()} for {dmg}.");
                if (target is Mutant tm) { tm.IsAggroed = true; if (!(target is HumanEnemy)) tm.DamageTakenThisRound += dmg; }
                if (shooter is Player && target is HumanEnemy he) foreach (var o in EnemyAI.Squad(he)) if (o.IsAlive()) o.IsAggroed = true;
                if (target.IsAlive()) Body.Hit(target, hit.Part, hit.Side, dmg, w.CurrentAmmoType, shooter is Player && Rt.SlowMo ? 1.5f : 1f);   // RT batch 3: bullet time wounds more
                if (shooter is Player) Rt.AddFocus(target.IsAlive() ? 4f : 25f);   // RT batch 2: limb health, wounds, down
                if (!target.IsAlive()) Weapon.OnTargetKilled(shooter, target);
                if (target is Player p && p.IsAlive()) Perks.TryAdrenaline(p);
            }
            CombatFx.Add(fx);
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
            var (sx, sy) = Ballistics.Pos(shooter); var (tx, ty) = Ballistics.Pos(target);
            float dx = tx - sx, dy = ty - sy, len = (float)System.Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.01f) return;
            dx /= len; dy /= len;
            var (h, spread) = NpcAim(shooter, w, target);
            var hd = For(w);
            for (int i = 0; i < rounds; i++)
                Shoot(shooter, w, dx, dy, h, spread + i * hd.Kick * 0.6f, false, i * 60f / System.Math.Max(60f, hd.Rpm));
        }
    }
}
