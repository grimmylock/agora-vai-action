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
        public const float Spacing = 0.62f;          // tiles between neighbours at rest: 56 x 0.62 = about 35 tiles of eel
        public float SlitherAmount = 0.3f, SlitherSpeed = 6f, WaveSpacing = 0.5f;   // tiles of sideways swing, radians per second, radians between neighbours
        public float HX, HY, HRot, VX, VY, Face = -1f;   // the head: position, direction of travel, velocity (tiles/s), which way it looks (+1 right)
        public bool Under;                           // below the water (drawn as a shadow, or not at all)
        public bool AnchorOn; public float AnchorK = 7f; public int AnchorFrom = 12;     // rear-up / coil: the body is pulled toward its slots (a rest pose), only the neck swings free
        // --- mass, water and springs (Yan, 2026-11-05: "it looks like a train following a track ... no sense of mass, muscular tension or momentum")
        public float SpacingTarget = 1f;             // where the rest spacing (x Spacing) is heading: the state machine sets it (0.45 = bunched up like a spring, 1.2 = stretched)
        public float SpacingNow = 1f, SpacingVel;    // the spacing itself runs on an under-damped spring, so it overshoots a little when released
        public float SpringK = 150f, SpringC = 9f, WaveLag = 110f;      // the spacing spring; how fast the squeeze/stretch travels down the body (per second: lower = slower wave)
        public bool CompressAll; public int NeckSegs = 22;             // the squeeze is for the neck (the coil stays where it is), or for the whole body (the wrap's constriction)
        public float DragBase = 1.3f, DragTail = 3.2f, MassTail = 3f;  // water resistance per second (more toward the tail), and how much heavier the tail end is than the neck
        public int Iterations = 10;                  // distance-solver passes per sub-step
        public readonly float[] X = new float[Count], Y = new float[Count], Rot = new float[Count];     // the body: [0] is next to the head
        public readonly float[] PX = new float[Count], PY = new float[Count];                              // where each was one sub-step ago: its velocity is X - PX (Verlet), so it keeps its momentum
        public readonly float[] Sc = new float[Count], M = new float[Count];                               // each segment's squeeze/stretch now (a delayed copy of SpacingNow) and its mass
        public readonly float[] RX = new float[Count], RY = new float[Count];                              // what is drawn: the body plus the slither
        public float[] SlotX, SlotY, SlotZ;          // the rest pose; SlotZ = how high each segment lies in a coil (tiles): the head end on top, like a real coil seen from the 3/4 view
        public readonly float[] Z = new float[Count];      // each segment's height now (it eases toward its slot's): drawn as a lift up the screen
        public float HeadLift, HeadZ;                // the boss sets HeadLift (tiles): the head rears up OVER its own body (and drags the neck up behind it); HeadZ is where it is now
        public bool LookActive; public float LookX, LookY;     // the head holds still and watches this point (the player): it turns its profile that way
        float phase;
        /// <summary>Sorting order for a view (Y-sort or a manual Z offset): the head above everything, then each segment above the one behind it, so where the body loops over itself the part nearer the head is on top.</summary>
        public static int SortOrder(int i, int baseOrder = 0) => baseOrder + Count - i;
        public static int HeadOrder(int baseOrder = 0) => baseOrder + Count + 1;
        public EelRig() { for (int i = 0; i < Count; i++) { Sc[i] = 1f; M[i] = 1f + (MassTail - 1f) * i / (Count - 1f); } }

        /// <summary>Lay the body out in a straight line behind the head along (dx, dy).</summary>
        public void Place(float hx, float hy, float dx, float dy)
        {
            float l = Mathf.Sqrt(dx * dx + dy * dy) + 1e-5f; dx /= l; dy /= l; HX = hx; HY = hy;
            for (int i = 0; i < Count; i++) { X[i] = PX[i] = hx + dx * Spacing * (i + 1); Y[i] = PY[i] = hy + dy * Spacing * (i + 1); Rot[i] = Mathf.Atan2(-dy, -dx); Sc[i] = 1f; }
            SpacingNow = SpacingTarget = 1f; SpacingVel = 0f; Render();
        }
        /// <summary>Put the body exactly on a rest shape (the slots: [i] is where segment i belongs).</summary>
        public void Snap(float hx, float hy) { HX = hx; HY = hy; if (SlotX == null) return; for (int i = 0; i < Count; i++) { X[i] = PX[i] = SlotX[i]; Y[i] = PY[i] = SlotY[i]; } Render(); }

        float Rest(int i) { float w = CompressAll ? 1f : Mathf.Clamp01((NeckSegs + 8 - i) / 8f); return Spacing * (1f + (Sc[i] - 1f) * w); }

        /// <summary>Move the head to (tx, ty) over dt. The head is the one kinematic thing; everything behind it is a rope: a Verlet integration with water drag (heavier toward the tail), a
        /// weighted distance solver, and a rest spacing that compresses and stretches on a spring. Run in sub-steps no longer than the segment spacing so a fast head cannot tunnel.</summary>
        public void Step(float dt, float tx, float ty)
        {
            float acc = SpringK * (SpacingTarget - SpacingNow) - SpringC * SpacingVel;
            SpacingVel += acc * dt; SpacingNow = Mathf.Clamp(SpacingNow + SpacingVel * dt, 0.2f, 1.6f);
            float lag = 1f - Mathf.Exp(-WaveLag * dt);
            for (int i = 0; i < Count; i++) Sc[i] += ((i == 0 ? SpacingNow : Sc[i - 1]) - Sc[i]) * lag;       // the squeeze or stretch travels down the body, it does not happen everywhere at once
            float ox = HX, oy = HY, dx = tx - ox, dy = ty - oy, dist = Mathf.Sqrt(dx * dx + dy * dy);
            int n = Mathf.Clamp((int)Mathf.Ceil(dist / (Spacing * 0.7f)), 1, 24); float h = dt / n;
            float pull = AnchorOn && SlotX != null ? 1f - Mathf.Exp(-AnchorK * h) : 0f;
            for (int k = 1; k <= n; k++)
            {
                HX = ox + dx * k / n; HY = oy + dy * k / n;
                for (int i = 0; i < Count; i++)                                                                 // inertia: it keeps going the way it was going, less so in the water the further down it is
                {
                    float ret = Mathf.Exp(-(DragBase + DragTail * i / (Count - 1f)) * h), vx = (X[i] - PX[i]) * ret, vy = (Y[i] - PY[i]) * ret;
                    PX[i] = X[i]; PY[i] = Y[i]; X[i] += vx; Y[i] += vy;
                    if (pull > 0f) { float w = pull * (i < AnchorFrom ? 0.12f : Mathf.Clamp01((i - AnchorFrom) / 6f + 0.3f)); X[i] += (SlotX[i] - X[i]) * w; Y[i] += (SlotY[i] - Y[i]) * w; }
                }
                for (int it = 0; it < Iterations; it++) { for (int i = 0; i < Count; i++) Project(i); for (int i = Count - 1; i >= 0; i--) Project(i); }
            }
            for (int i = 0; i < Count; i++) { float ax = i == 0 ? HX : X[i - 1], ay = i == 0 ? HY : Y[i - 1]; Rot[i] = Mathf.Atan2(ay - Y[i], ax - X[i]); }
            if (dt > 1e-5f) { VX = (HX - ox) / dt; VY = (HY - oy) / dt; }
            float speed = Mathf.Sqrt(VX * VX + VY * VY);
            if (speed > 0.6f) { HRot = Mathf.Atan2(VY, VX); if (Mathf.Abs(VX) > 0.4f) Face = VX > 0f ? 1f : -1f; }
            if (LookActive) { HRot = Mathf.Atan2(LookY - HY, LookX - HX); Face = LookX >= HX ? 1f : -1f; }       // anchored and menacing: it looks at you, not where it is going
            HeadZ += (HeadLift - HeadZ) * (1f - Mathf.Exp(-12f * dt));
            float zk = 1f - Mathf.Exp(-9f * dt); for (int i = 0; i < Count; i++) Z[i] += ((AnchorOn && SlotZ != null ? SlotZ[i] : 0f) - Z[i]) * zk;
            phase += SlitherSpeed * dt * (0.35f + Mathf.Clamp01(speed / 5f));          // it swims faster the faster it moves
            Render();
        }

        /// <summary>One distance constraint (segment i to the one ahead of it, the head for i = 0) pulled to its rest length: the heavier of the two moves less, the head not at all.</summary>
        void Project(int i)
        {
            float ax = i == 0 ? HX : X[i - 1], ay = i == 0 ? HY : Y[i - 1], dx = X[i] - ax, dy = Y[i] - ay, d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d < 1e-5f) { dx = 1e-3f; dy = 0f; d = 1e-3f; }
            float diff = (d - Rest(i)) / d, wa = i == 0 ? 0f : 1f / M[i - 1], wb = 1f / M[i], sum = wa + wb;
            if (i > 0) { X[i - 1] += dx * diff * wa / sum; Y[i - 1] += dy * diff * wa / sum; }
            X[i] -= dx * diff * wb / sum; Y[i] -= dy * diff * wb / sum;
        }

        /// <summary>The drawn points: the body pushed sideways by a sine wave that travels down it, perpendicular to each segment's own heading (the direction it is moving along). Nothing at
        /// the head (it leads clean) and full strength from the 8th segment on; it dies down when the head stands still.</summary>
        void Render()
        {
            float speed = Mathf.Sqrt(VX * VX + VY * VY), amp = SlitherAmount * (0.3f + 0.7f * Mathf.Clamp01(speed / 4f));
            for (int i = 0; i < Count; i++)
            {
                float fx = Mathf.Cos(Rot[i]), fy = Mathf.Sin(Rot[i]), env = Mathf.Clamp01(i / 8f);
                float off = Mathf.Sin(phase - i * WaveSpacing) * amp * env;
                RX[i] = X[i] - fy * off; RY[i] = Y[i] + fx * off - Z[i] - HeadZ * Mathf.Exp(-i * 0.18f);
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
