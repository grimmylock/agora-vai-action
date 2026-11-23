using UnityEngine;

// 2026-11-23 (Yan: "the chain doesn't launch if you dodge and also can't be parried, make it if you parry the chain you get a button mash of 15 clicks to pull him to you and he's stunned for 3 seconds"):
// the Verger's Hook is now a WHITE blow (parry it). Parry the chain and the rope goes taut between you in slow motion: MASH the parry key 15 times inside Window seconds to haul HIM in (he slides to you and is
// stunned Stun seconds, the rest of the combo cancelled); fail, and the chain wins: it cuts you and drags you in as an unparried hook does. Host side, the victim's presses only (co-op mirroring later).
namespace GameCore
{
    public static class VergerHook
    {
        public const int Need = 15; public const float Window = 4f, Stun = 3f;
        static Boss.Fight _f; static Player _v; static int _n; static float _until, _nextFx;
        public static bool Active => _f != null;
        public static void Reset() { if (_f != null) Boss.Release(); _f = null; _v = null; _n = 0; }

        public static void Begin(Boss.Fight f, Player p, float now)
        {
            _f = f; _v = p; _n = 0; _until = now + Window; _nextFx = 0f; f.Doing = null; f.Clip = null; f.ChangeUntil = _until + 5f; f.StrikeAt = now + 99f;
            Boss.Hold(Window + 0.3f); Boss.NameCard("MASH PARRY", "0 / " + Need); GameLog.Write("You caught the chain! MASH the parry key to haul him in!");
        }
        public static bool MashInput(Player p)
        {
            if (_f == null || p != _v) return false;
            _n++; Boss.NameCard("MASH PARRY", _n + " / " + Need); Sfx.Play("chain_drag", null, 0.6f, 0.04f, 1f + _n * 0.03f); return true;
        }
        /// <summary>Every frame from Boss.Tick: true while the tug-of-war runs (he does nothing else).</summary>
        public static bool Tick(Boss.Fight f, float now)
        {
            if (_f == null || f != _f) return false;
            var h = f.Unit; var p = _v;
            if (f.Broken || f.Finished || p == null || !p.IsAlive()) { Reset(); f.ChangeUntil = -1f; return false; }
            f.Doing = null; f.StrikeAt = now + 99f;
            if (now >= _nextFx) { _nextFx = now + 0.2f; var cf = CombatFx.Push("hookchain", h, p, null); cf.Now = true; cf.Hit = true; }
            bool won = _n >= Need;
            if (won || now >= _until)
            {
                Boss.Release(); _f = null; _v = null; f.ChangeUntil = -1f;
                if (won)
                {
                    Boss.StepIn(h, p, 1.0f); f.Clip = null; f.StaggerUntil = now + Stun; f.NextMoveAt = f.StaggerUntil + 0.3f; f.OpenUntil = -1f;
                    CombatFx.Push("stagger", h, null, null).Now = true; Boss.ParryMoment(); GameLog.Write("You haul him in - he's stunned!");
                }
                else
                {
                    GameLog.Write("The chain wins.");
                    Parry.Unparryable = true; try { Melee.HitUnit(h, h.EquippedWeapon, p, Boss.MultOf(f.Def.Moves[0], 0)); } finally { Parry.Unparryable = false; }
                    Boss.DragIn(h, p, 3.2f); f.NextMoveAt = now + 1.0f;
                }
                _n = 0; return won;
            }
            return true;
        }
    }
}
