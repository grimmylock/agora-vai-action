// New (Batch 22): combat feedback. The rules resolve instantly (a whole enemy turn happens in one
// frame), so Weapon.Fire / Weapon.Reload / Mutant.Attack only *record* what happened here, and
// TacticalGridView plays the queue back a beat apart (muzzle flash, tracer, recoil, casings, hit
// flash, reload animation). Nothing in the rules reads this back.
//
// How each gun *feels* (WeaponFeel) is derived from its stats so every weapon differs without a
// 55-row table to keep in sync: category baseline, scaled by how hard it hits for its class
// (BaseDamage vs the class's typical damage), High Recoil x1.4, suppressed = no flash and a faint
// tracer, reload style from how it's fed. Overrides below for the odd ones out; tune there.
using System.Collections.Generic;
using GameData;
using UnityEngine;

namespace GameCore
{
    public class FxEvent
    {
        public string Kind;            // "shot" | "melee" | "strike" (mutant claw/bite/psi) | "reload"
        public ICombatant Actor, Target;
        public Weapon Weapon;
        public int Rounds = 1;
        public bool Hit, Jammed, NoLos, Psi;
        public bool CoverHit, Through;          // Batch 105: the round hit the cover at (CoverX, CoverY) / went through it
        public int CoverX, CoverY;
        public Material CoverMat;               // read before the hit can break it
        public bool Killed;            // this hit killed the target (Batch 24: the view starts the ragdoll then)
        public int Overkill;           // damage past the target's remaining HP; big overkill = gibs
        public int FromX, FromY, ToX, ToY;
        // Real-time fork: a round resolved now (Gunplay) plays at once, not queued a beat apart behind the turn's other events;
        // it ends exactly where the round stopped (tile space, y down). Pellet = a shotgun's extra pellet: tracer only.
        public bool Now, Pellet;
        public float Delay, EndX = -1f, EndY = -1f;
        public float Seconds;          // RT 6d: a reload's real length (0 = the gun's own animation length)
    }

    public static class CombatFx
    {
        public static readonly List<FxEvent> Pending = new List<FxEvent>();
        /// <summary>Every tile a unit steps onto (TacticalGridSystem.AttemptMove), so the view can walk
        /// the real route instead of sliding through walls when a whole move resolves in one frame.</summary>
        public static readonly List<(ICombatant unit, int x, int y)> Steps = new List<(ICombatant, int, int)>();

        public static void Step(ICombatant unit, int x, int y)
        {
            if (Steps.Count >= 256) Steps.RemoveAt(0);
            Steps.Add((unit, x, y));
        }
        // ponytail: nothing drains the queue without a view (tests, headless); cap it instead.
        private const int Cap = 64;

        /// <summary>Real-time fork: queue an event built by the caller (Gunplay).</summary>
        public static void Add(FxEvent e) { if (Pending.Count >= Cap) Pending.RemoveAt(0); Pending.Add(e); }

        /// <summary>RT 6d: a real-time reload starts - the animation runs as long as the reload timer. jam (RT 6f): clearing a jam.</summary>
        public static void Reload(ICombatant u, Weapon w, float seconds, bool jam = false) { var e = Push("reload", u, null, w); e.Now = true; e.Seconds = seconds; e.Jammed = jam; }

        public static FxEvent Push(string kind, ICombatant actor, ICombatant target, Weapon weapon = null)
        {
            var e = new FxEvent { Kind = kind, Actor = actor, Target = target, Weapon = weapon };
            if (actor != null) { e.FromX = actor.X; e.FromY = actor.Y; }
            if (target != null) { e.ToX = target.X; e.ToY = target.Y; }
            if (Pending.Count >= Cap) Pending.RemoveAt(0);
            Pending.Add(e);
            return e;
        }
    }

    public class WeaponFeel
    {
        public float Kick;         // character px the gun jumps back per shot
        public float Climb;        // degrees the muzzle climbs per shot
        public float Recover;      // seconds to settle back
        public int Flash;          // 0 = suppressed (smoke puff only), 1 small .. 4 huge
        public float Shake;        // camera shake in tiles when the player fires
        public float Tracer = 1f;  // tracer opacity
        public string Reload;      // "mag" | "cylinder" | "tube" | "break" | "belt" | "none"
        public float ReloadTime;   // seconds of reload animation
        public string Casing;      // per-shot ejection: "brass", "shell" or null (kept in the gun)
        public bool Rail;

        // category: kick, climb, recover, flash, shake, typical damage
        private static readonly Dictionary<string, float[]> Base = new Dictionary<string, float[]>
        {
            ["sidearm"] = new[] { 1f, 10f, 0.14f, 1f, 0f, 150f },
            ["smg"]     = new[] { 0.5f, 3f, 0.08f, 1f, 0f, 130f },
            ["shotgun"] = new[] { 2f, 12f, 0.25f, 3f, 0.06f, 310f },
            ["assault"] = new[] { 1f, 5f, 0.12f, 2f, 0.02f, 240f },
            ["sniper"]  = new[] { 2f, 14f, 0.35f, 2f, 0.05f, 380f },
            ["heavy"]   = new[] { 1f, 4f, 0.10f, 2f, 0.03f, 250f },
        };

        // Odd ones out, by WeaponDatabase key. Runs after the derived values.
        private static readonly Dictionary<string, System.Action<WeaponFeel>> Overrides = new Dictionary<string, System.Action<WeaponFeel>>
        {
            ["G7RailPrototype"] = f => { f.Rail = true; f.Flash = 0; f.Casing = null; f.Kick *= 0.5f; f.Climb *= 0.5f; f.Shake = 0.1f; },
            ["FrontierLeverRifle"] = f => { f.Reload = "tube"; f.ReloadTime = 1.6f; },   // loads through the side gate, round by round
            ["HaldenAM50Sledge"] = f => { f.Flash = 4; f.Shake = 0.15f; },
            ["RK12Kuvalda"] = f => { f.Shake = 0.08f; },
        };

        private static readonly Dictionary<WeaponDef, WeaponFeel> Cache = new Dictionary<WeaponDef, WeaponFeel>();

        public static WeaponFeel For(WeaponDef db)
        {
            if (db == null) return new WeaponFeel { Reload = "none" };
            if (Cache.TryGetValue(db, out var hit)) return hit;
            var (cat, key) = WeaponArt.Identify(db);
            var f = new WeaponFeel();
            if (!Base.TryGetValue(cat, out var b)) { f.Reload = "none"; return Cache[db] = f; }   // melee

            float p = Mathf.Clamp(db.BaseDamage / b[5], 0.6f, 2.5f) * (db.HighRecoil ? 1.4f : 1f);
            f.Kick = b[0] * p;
            f.Climb = b[1] * p;
            f.Recover = b[2] * Mathf.Sqrt(p);
            f.Flash = Mathf.Clamp(Mathf.RoundToInt(b[3] * Mathf.Min(p, 1.5f)), 1, 4);
            f.Shake = b[4] * p;
            if (db.InnateSuppressor) { f.Flash = 0; f.Tracer = 0.35f; f.Shake *= 0.5f; }

            int cap = db.MagCapacity ?? db.CylinderCapacity ?? 1;
            float extra = 0.4f * (db.ReloadApCost ?? 0);
            if (db.CylinderCapacity.HasValue) { f.Reload = "cylinder"; f.ReloadTime = 0.5f + 0.12f * cap + extra; f.Casing = null; }
            else if (db.ActionType == "Break") { f.Reload = "break"; f.ReloadTime = 0.8f + extra; f.Casing = null; }
            else if (cat == "shotgun" && !db.MagFed) { f.Reload = "tube"; f.ReloadTime = 0.4f + 0.15f * cap + extra; f.Casing = "shell"; }
            else if (cat == "heavy") { f.Reload = "belt"; f.ReloadTime = 2f + extra; f.Casing = "brass"; }
            else { f.Reload = "mag"; f.ReloadTime = (cat == "sidearm" ? 0.7f : cat == "sniper" ? 1.2f : 1f) + extra; f.Casing = cat == "shotgun" ? "shell" : "brass"; }

            if (key != null && Overrides.TryGetValue(key, out var o)) o(f);
            return Cache[db] = f;
        }
    }
}
