using System.Collections.Generic;
using UnityEngine;

// 2026-11-23 (Yan: "the main npcs are kinda static sprites that don't express much life ... pick 1 human sprite and do it only for it, as a test"): procedural life for ONE suit, no new art.
// The test subject is the RAIDER JACKET on a HumanEnemy (the commonest hostile; the player and every other suit are untouched). Everything is presentation (nothing in the rules reads it) and moves
// in whole sprite pixels (1/16 tile) so it stays crisp:
//  - (OFF since Yan's review: breathing, weight shift) breathing: a 1 px rise on the in-breath (0.8 Hz calm, 1.5 Hz once he is aware of you), each unit on its own phase
//  - weight shift: every 3-6 s he settles 1 px to one side for about a second
//  - walking: 1 px hop on the foot-down frames and a 2 degree lean into the direction of travel
//  - flinch: a HEAVY hit (a fifth of his health or more, at most every 1.5 s) kicks him back 3 px and a 4 degree tilt, settling in 0.18 s
// Everything returns to exactly zero when it ends, so the next unit to use the marker slot starts clean (TacticalGridView resets the tilt).
namespace GameCore
{
    public static class UnitLife
    {
        public const string TestSuit = "Raider";   // (kept for reference: the first test subject) 2026-11-23 (Yan: "try it on the other human factions too"): now every hostile or story human with a suit
        const float Px = 1f / 16f, HeavyHit = 0.2f, FlinchCooldown = 1.5f;     // a hit of a fifth of his health or more
        const bool Breathing = false;                                          // 2026-11-23 (Yan: "the breathing doesn't look good"): idle breathing and the weight shift are off; the walk (hop + lean) stays
        class S { public float Phase, ShiftAt, ShiftUntil, ShiftDir, FlinchAt = -9f, FlinchDx, FlinchDy; public int Hp = -1; }
        static readonly Dictionary<ICombatant, S> _s = new Dictionary<ICombatant, S>();
        public static void Reset() => _s.Clear();

        public static bool Applies(ICombatant u) => u is HumanEnemy h && h.EquippedArmor != null && !Boss.Is(u) && !Husk.Is(h);

        /// <summary>The offset (world units, y up) and tilt (degrees, + = counter-clockwise) to give this unit's sprite this frame.</summary>
        public static (Vector3 off, float tilt) Pose(ICombatant u, bool moving, bool stepFrame, float faceDx, float now, float hitDx, float hitDy)
        {
            if (!_s.TryGetValue(u, out var s)) { s = new S { Phase = Random.value * 6.28f, ShiftAt = now + Random.Range(2f, 5f) }; _s[u] = s; }
            if (s.Hp >= 0 && u.Hp < s.Hp && s.Hp - u.Hp >= ((u as HumanEnemy)?.MaxHp ?? 100) * HeavyHit && now - s.FlinchAt > FlinchCooldown) { s.FlinchAt = now; s.FlinchDx = hitDx; s.FlinchDy = hitDy; }   // 2026-11-23 (Yan: a flinch per bullet would make him vibrate): only a heavy hit, and at most once per cooldown
            s.Hp = u.Hp;
            var h = u as HumanEnemy; float rate = h != null && h.IsAggroed ? 1.5f : 0.8f;
            float off_x = 0f, off_y = 0f, tilt = 0f;
            if (!moving && Breathing)
            {
                off_y += Mathf.Sin(now * rate * 6.2832f + s.Phase) > 0.25f ? Px : 0f;                       // the in-breath
                if (now >= s.ShiftAt) { s.ShiftDir = Random.value < 0.5f ? -1f : 1f; s.ShiftUntil = now + Random.Range(0.8f, 1.4f); s.ShiftAt = s.ShiftUntil + Random.Range(3f, 6f); }
                if (now < s.ShiftUntil) off_x += s.ShiftDir * Px;                                          // settling his weight
            }
            else
            {
                if (stepFrame) off_y += Px;                                                                 // the hop on the foot-down frames
                tilt = -2f * Mathf.Sign(faceDx);
            }
            float fl = now - s.FlinchAt;
            if (fl >= 0f && fl < 0.18f)
            {
                float k = 1f - fl / 0.18f; float n = Mathf.Max(0.01f, Mathf.Sqrt(s.FlinchDx * s.FlinchDx + s.FlinchDy * s.FlinchDy));
                off_x += Mathf.Round(s.FlinchDx / n * 3f * k) * Px; off_y += Mathf.Round(s.FlinchDy / n * 3f * k) * Px; tilt += -4f * Mathf.Sign(s.FlinchDx) * k;
            }
            return (new Vector3(off_x, off_y, 0f), tilt);
        }
    }
}
