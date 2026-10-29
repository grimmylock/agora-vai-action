// Boss 1 (Yan, 2026-10-29: parry on a "dedicated key ... default F"; "perfect parries stagger him for a second, normal parries just
// block the damage"): the parry, for every player, against any melee hit (a human's swing - a boss's or a Raider's knife).
// - Press the key with a melee weapon in hand: the guard is up for Window seconds. A hit landing in the first Perfect seconds is a
//   PERFECT parry (no damage, the attacker staggers, a hit-stop, sparks, Focus); later in the window it's BLOCKED (no damage).
// - A Cooldown after each press: spamming it doesn't work. The Serrated Trench Knife's window is half as long again (ACTION_DESIGN).
// - Some boss moves can't be parried at all (Unparryable, the red glint): dodge those.
// ponytail: mutants' claws and bites don't go through Melee.Strike yet, so they can't be parried; and the melee slot isn't built,
// so you parry with whatever melee weapon is in your hand.
using UnityEngine;

namespace GameCore
{
    public static class Parry
    {
        public const float Window = 0.35f, Perfect = 0.12f, Cooldown = 0.45f, StaggerSeconds = 1.0f, PerfectFocus = 10f;
        public enum Result { None, Blocked, Perfect }

        /// <summary>Set by a boss while it lands a move that can't be parried.</summary>
        public static bool Unparryable;

        static float Long(Player p) => p.EquippedWeapon?.Db?.Name == "Serrated Trench Knife" ? 1.5f : 1f;

        public static bool CanParry(Player p) => p != null && p.EquippedWeapon != null && CombatRules.IsMelee(p.EquippedWeapon) && !Body.Down(p);

        /// <summary>The parry key, pressed (Rt.PlayerTick).</summary>
        public static void Press(Player p)
        {
            var s = Rt.Of(p); float now = Time.time;
            if (!CanParry(p) || now < s.ParryReady || s.Diving) return;
            s.ParryAt = now; s.ParryReady = now + Cooldown;
            var anim = CombatFx.Push("melee", p, null, p.EquippedWeapon); anim.Now = true;   // the guard comes up (the swing pose)
        }

        /// <summary>A melee hit is about to land on p (Melee.Strike): parried, blocked or not.</summary>
        public static Result Try(Player p, ICombatant by)
        {
            var s = Rt.Of(p); float since = Time.time - s.ParryAt;
            if (Unparryable || !CanParry(p) || since < 0f || since > Window * Long(p)) return Result.None;
            s.ParryAt = -99f;                                                      // one parry, one hit
            bool perfect = since <= Perfect * Long(p);
            var fx = CombatFx.Push("parry", p, by, p.EquippedWeapon); fx.Now = true; fx.Hit = perfect;
            if (perfect)
            {
                Boss.Stagger(by, StaggerSeconds);
                Rt.AddFocus(p, PerfectFocus);
                Boss.HitStop(0.16f);
                GameLog.Write(Boss.Is(by) ? "PERFECT PARRY - he's open!" : "Perfect parry!");
            }
            else { Boss.HitStop(0.05f); GameLog.Write("Blocked."); }
            return perfect ? Result.Perfect : Result.Blocked;
        }
    }
}
