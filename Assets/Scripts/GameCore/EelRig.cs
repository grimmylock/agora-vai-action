// The eel's procedural body (Yan, 2026-11-05: "the animations look stiff because we are translating and swapping a single static sprite"): a Head and a chain of
// BodySegments, each one following the one ahead at a fixed distance (kinematic follow: pure maths, no Rigidbody2D, no joints), with a slither wave across the
// movement vector. The state machine (EelBoss) only says where the HEAD goes; the body trails.
// It is plain float maths on purpose, not Transforms/Coroutines: the host-side fight (EelBoss, like BrainBoss) must run in the offline harness (whose UnityEngine stub
// has no working Transform or coroutines) and be the same on every machine in co-op. A view copies X/Y/Rot (the DRAWN points RX/RY) onto Transforms one to one.
// Positions are in tiles (the units of the map). Rot is the direction (radians) from a segment to the one ahead of it.
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public static class Ease
    {
        const float C1 = 1.70158f, C3 = C1 + 1f;
        public static float InBack(float t) { t = Mathf.Clamp01(t); return C3 * t * t * t - C1 * t * t; }          // dips backwards first, then goes: the wind-up
        public static float OutExpo(float t) { t = Mathf.Clamp01(t); return t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t); }   // all of it at once, then settles: the strike
        public static float InOutSine(float t) { t = Mathf.Clamp01(t); return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f; }
        public static float OutCubic(float t) { t = Mathf.Clamp01(t); float u = 1f - t; return 1f - u * u * u; }
        public static (float x, float y) Bezier((float x, float y) a, (float x, float y) b, (float x, float y) c, float t)   // quadratic
        { float u = 1f - t; return (u * u * a.x + 2f * u * t * b.x + t * t * c.x, u * u * a.y + 2f * u * t * b.y + t * t * c.y); }
        public static (float x, float y) Lerp((float x, float y) a, (float x, float y) b, float t) => (a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);
    }

    public class EelRig
    {
        public const int Count = 56;                 // segments behind the head
        public const float Spacing = 0.62f;          // tiles between neighbours: 56 x 0.62 = about 35 tiles of eel
        public float SlitherAmount = 0.3f, SlitherSpeed = 6f, WaveSpacing = 0.5f;   // tiles of sideways swing, radians per second, radians between neighbours
        public float HX, HY, HRot, VX, VY, Face = -1f;   // the head: position, direction of travel, velocity (tiles/s), which way it looks (+1 right)
        public bool Under;                           // below the water (drawn as a shadow, or not at all)
        public bool AnchorOn; public float AnchorK = 7f; public int AnchorFrom = 12;     // rear-up: the coil is pulled back to its slots, only the neck swings free
        public readonly float[] X = new float[Count], Y = new float[Count], Rot = new float[Count];     // the chain: [0] follows the head
        public readonly float[] RX = new float[Count], RY = new float[Count];                              // what is drawn: the chain plus the slither
        public float[] SlotX, SlotY;
        float phase;

        /// <summary>Lay the body out in a straight line behind the head along (dx, dy).</summary>
        public void Place(float hx, float hy, float dx, float dy)
        {
            float l = Mathf.Sqrt(dx * dx + dy * dy) + 1e-5f; dx /= l; dy /= l; HX = hx; HY = hy;
            for (int i = 0; i < Count; i++) { X[i] = hx + dx * Spacing * (i + 1); Y[i] = hy + dy * Spacing * (i + 1); Rot[i] = Mathf.Atan2(-dy, -dx); }
            Render();
        }
        /// <summary>Put the body exactly on a rest shape (the slots: [i] is where segment i belongs).</summary>
        public void Snap(float hx, float hy) { HX = hx; HY = hy; if (SlotX == null) return; for (int i = 0; i < Count; i++) { X[i] = SlotX[i]; Y[i] = SlotY[i]; } Follow(0f); Render(); }

        /// <summary>Move the head to (tx, ty) over dt: in sub-steps no longer than the segment spacing, so a fast head drags a clean trail (a coil) behind it, not a straight chord.</summary>
        public void Step(float dt, float tx, float ty)
        {
            float ox = HX, oy = HY, dx = tx - ox, dy = ty - oy, dist = Mathf.Sqrt(dx * dx + dy * dy);
            int n = Mathf.Clamp((int)Mathf.Ceil(dist / (Spacing * 0.7f)), 1, 24);
            float pull = AnchorOn && SlotX != null ? 1f - Mathf.Exp(-AnchorK * dt / n) : 0f;
            for (int k = 1; k <= n; k++) { HX = ox + dx * k / n; HY = oy + dy * k / n; Follow(pull); }
            if (dt > 1e-5f) { VX = (HX - ox) / dt; VY = (HY - oy) / dt; }
            float speed = Mathf.Sqrt(VX * VX + VY * VY);
            if (speed > 0.6f) { HRot = Mathf.Atan2(VY, VX); if (Mathf.Abs(VX) > 0.4f) Face = VX > 0f ? 1f : -1f; }
            phase += SlitherSpeed * dt * (0.35f + Mathf.Clamp01(speed / 5f));          // it swims faster the faster it moves
            Render();
        }

        /// <summary>Kinematic follow, head to tail: each segment goes to the fixed distance from the one ahead, along the line to where it was. The coil's segments are first
        /// pulled toward their slots (rear-up only) so the body keeps its shape round the pool while the neck follows the head.</summary>
        void Follow(float pull)
        {
            float px = HX, py = HY;
            for (int i = 0; i < Count; i++)
            {
                if (pull > 0f) { float w = pull * (i < AnchorFrom ? 0.12f : Mathf.Clamp01((i - AnchorFrom) / 6f + 0.3f)); X[i] += (SlotX[i] - X[i]) * w; Y[i] += (SlotY[i] - Y[i]) * w; }
                float dx = X[i] - px, dy = Y[i] - py, d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d < 1e-4f) { dx = 1f; dy = 0f; d = 1f; }
                X[i] = px + dx / d * Spacing; Y[i] = py + dy / d * Spacing;
                Rot[i] = Mathf.Atan2(py - Y[i], px - X[i]); px = X[i]; py = Y[i];
            }
        }

        /// <summary>The drawn points: the chain pushed sideways by a sine wave that travels down the body, perpendicular to each segment's own heading (the direction it
        /// is moving along). Nothing at the head (it leads clean) and full strength from the 8th segment on; it dies down when the head stands still.</summary>
        void Render()
        {
            float speed = Mathf.Sqrt(VX * VX + VY * VY), amp = SlitherAmount * (0.3f + 0.7f * Mathf.Clamp01(speed / 4f));
            for (int i = 0; i < Count; i++)
            {
                float fx = Mathf.Cos(Rot[i]), fy = Mathf.Sin(Rot[i]), env = Mathf.Clamp01(i / 8f);
                float off = Mathf.Sin(phase - i * WaveSpacing) * amp * env;
                RX[i] = X[i] - fy * off; RY[i] = Y[i] + fx * off;
            }
        }

        /// <summary>How many segments from the head are above the water within `radius` tiles of (cx, cy): a body that is still dragging in from far off is not drawn there.</summary>
        public int Visible(float cx, float cy, float radius)
        {
            int n = 0; float r2 = radius * radius;
            for (int i = 0; i < Count; i++) { float dx = X[i] - cx, dy = Y[i] - cy; if (dx * dx + dy * dy > r2) break; n = i + 1; }
            return n;
        }
    }
}
