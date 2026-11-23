using UnityEngine;

// 2026-11-23 (Yan: "yes, try the spring on the scythe"): the one place a weapon is not pure choreography. The hand is still placed by the pose keyframes, but the BLADE's angle follows the pose through a damped spring: it trails
// the hand when a move starts, whips past the end of the swing and rocks back, so a heavy scythe feels like it has weight. Presentation only (the hit shapes read the pose, not the spring). Only the weapons listed in For().
namespace GameCore
{
    public static class WeaponSpring
    {
        public struct State { public float Pos, Vel; public bool Init; }
        /// <summary>omega (rad/s: stiffness), zeta (damping ratio: < 1 overshoots), max lag in degrees (so a fast spin drags the blade behind by at most this much).</summary>
        public static (float w, float z, float max)? For(string key)
        {
            if (key == "VergerScythe") return (18f, 0.32f, 75f);          // the scythe and the sledgehammer are hand-tuned (Yan liked them)
            if (key == "Sledgehammer") return (14f, 0.36f, 65f);          // the heavier head: a softer, slower spring
            if (key == null || !GameData.WeaponDatabase.Categories["melee"].TryGetValue(key, out var d)) return null;
            // 2026-11-23 (Yan: "add it to the rest by weight"): 0.5 kg or less = a crisp small flick, 4 kg or more = nearly the hammer's slow swing
            float t = Mathf.Clamp01(((d.WeightKg ?? 1f) - 0.5f) / 3.5f); return (Mathf.Lerp(26f, 15f, t), Mathf.Lerp(0.46f, 0.37f, t), Mathf.Lerp(32f, 68f, t));
        }

        /// <summary>One step toward `target` (degrees); returns where the blade is now.</summary>
        public static float Step(ref State s, float target, float dt, float w, float z, float max)
        {
            if (!s.Init || Mathf.Abs(target - s.Pos) > 300f) { s.Pos = target; s.Vel = 0f; s.Init = true; return target; }     // first frame, or the facing flipped: snap
            dt = Mathf.Min(dt, 0.1f); int n = Mathf.Max(1, Mathf.CeilToInt(dt / (1f / 120f))); float h = dt / n;
            for (int i = 0; i < n; i++) { float acc = w * w * (target - s.Pos) - 2f * z * w * s.Vel; s.Vel += acc * h; s.Pos += s.Vel * h; }
            s.Pos = Mathf.Clamp(s.Pos, target - max, target + max); return s.Pos;
        }
    }
}
