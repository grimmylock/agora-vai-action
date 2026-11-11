// 2026-11-11 (Yan: "the walking animation for those 2 are fine, put them in"): which frame of a mutant's walk strip to draw. A mutant walks on screen while Rt.Glide is sliding its body toward
// where its tile says it is, so the same rule serves NPCs and co-op mirrors (a client sees tile jumps from the snapshot and slides them the same way); no state is networked.
// Pallid and Veil Prowler have a strip (UnitSprites.HDWalk); everyone else keeps the stand-still sprite. Standing still, dead, or not in real time: the stand-still sprite.
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public static class MutantWalk
    {
        /// <summary>Seconds a picture stays up (the animator's own speed: 8 pictures in 1.0 s). Tuning: the mutants cross the ground faster than these feet were drawn for (about 2.4 tiles a second), so the feet skate a little at any speed.</summary>
        public const float FrameSeconds = 0.125f;
        /// <summary>A mutant that stopped sliding less than this long ago still counts as walking: Glide ends a hair before the next tile step, which would otherwise flicker to the stand-still sprite.</summary>
        public const float HoldSeconds = 0.25f;

        static readonly Dictionary<ICombatant, (float start, float lastSlide)> State = new Dictionary<ICombatant, (float, float)>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => State.Clear();

        /// <summary>The picture for this moment: 0..WalkFrames-1 counted from when the walk began; pure, for the tests.</summary>
        public static int FrameAt(float now, float start) => Mathf.Max(0, Mathf.FloorToInt((now - start) / FrameSeconds)) % UnitSprites.WalkFrames;

        /// <summary>The walk-strip frame to draw for this mutant now, or null (stand-still sprite).</summary>
        public static Sprite Frame(Mutant m, string name, float now)
        {
            if (!Rt.On || m == null || !m.IsAlive()) return null;
            var frames = UnitSprites.HDWalk(name);
            if (frames == null) return null;
            State.TryGetValue(m, out var st);
            bool walking = st.lastSlide > 0f && now - st.lastSlide < HoldSeconds;
            if (Rt.Sliding(m))
            {
                if (!walking) st.start = now;                         // a new walk begins on its first picture
                st.lastSlide = now; walking = true;
            }
            State[m] = st;
            return walking ? frames[FrameAt(now, st.start)] : null;
        }
    }
}
