// Real-time fork, RT batch 4: human enemies fight in real time.
// The tactics are the old turn AI's (EnemyAI.HumanTurn: roles - Leader, Anchor, Flanker -, morale and rallying, cover and
// flanking tiles from PickTile, frags at a player who camps, flashbangs, smoke, props, searching where you were last seen).
// It still "thinks" every ~3 s (Rt.WorldTick), but with Rt on it no longer walks and shoots inside that one frame: it only
// picks a goal tile (SetGoal). Every frame, here:
//  - walking: one tile at a time toward the goal at 2.2 tiles/s (moving widens its aim), a fleeing one runs off;
//  - shooting: while it can see you and you're in reach - bursts of 3-5 from automatics, single shots otherwise, then a pause
//    (shorter with rank); reloads take the gun's reload time ("Reloading!"); someone close and unarmed swings;
//  - suppression both ways: your rounds that just miss it pin it for 2.5 s (it stays put, shoots half as often, wider);
//    its near misses pin you for 1.5 s (Rt: +2 deg spread);
//  - call-outs over heads (Banter): "Reloading!", "Grenade!", "Flanking!", "Pinned!".
// Mutants keep the bridge (their whole move and strike every ~3 s, EnemyAI.MutantTurn). ponytail: real-time mutants later.
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public static class RtAi
    {
        public const float StepSeconds = 1f / 2.2f, SuppressSeconds = 2.5f;

        class Mind
        {
            public (int x, int y)? Goal;
            public bool Flee;
            public float NextStep, NextShot, ReloadUntil = -1f, PinnedUntil = -1f, MovedAt = -9f;
            public int BurstLeft;
            public bool Dry;                                          // out of rounds: closes in to swing
        }
        static readonly Dictionary<Mutant, Mind> Minds = new Dictionary<Mutant, Mind>();
        static Mind Of(Mutant m) { if (!Minds.TryGetValue(m, out var s)) Minds[m] = s = new Mind(); return s; }
        public static void Reset() => Minds.Clear();

        public static void SetGoal(HumanEnemy h, (int x, int y) goal, string role)
        {
            var s = Of(h);
            if (role == "Flanker" && s.Goal != goal && (h.X, h.Y) != goal && Random.value < 0.4f) Banter.Say(h, "Flanking!", 3);
            s.Goal = goal; s.Flee = false;
        }

        public static void Flee(HumanEnemy h) { var s = Of(h); s.Flee = true; s.Goal = null; }

        public static bool Pinned(Mutant m) => Minds.TryGetValue(m, out var s) && Time.time < s.PinnedUntil;

        /// <summary>A round just missed it (Gunplay): pinned for a moment.</summary>
        public static void Suppress(Mutant m)
        {
            var s = Of(m);
            if (Time.time >= s.PinnedUntil && m is HumanEnemy h && Random.value < 0.3f) Banter.Say(h, "Pinned!", 3);
            s.PinnedUntil = Time.time + SuppressSeconds;
            m.StatusEffects.Suppressed = true;
        }

        public static void Shout(Mutant m, string line) { if (m is HumanEnemy) Banter.Say(m, line, 3); }

        /// <summary>One frame of one human enemy in a fight.</summary>
        public static void Tick(HumanEnemy h, Player p)
        {
            var s = Of(h);
            float now = Time.time;
            if (s.PinnedUntil >= 0f && now >= s.PinnedUntil) { s.PinnedUntil = -1f; h.StatusEffects.Suppressed = false; }
            if (h.Morale == "Surrendered" || h.StatusEffects.Prone) return;

            // ---- walking ----
            if (now >= s.NextStep && !Pinned(h))
            {
                s.NextStep = now + StepSeconds * (h.StatusEffects.Wound == "Leg" ? 1.7f : 1f);
                h.Mp = System.Math.Max(h.Mp, TurnLoop.WalkMp);
                bool moved = false;
                int bx = h.X, by = h.Y;
                if (s.Flee) h.MoveAwayFrom(p);
                else if (s.Goal.HasValue && (h.X, h.Y) != s.Goal.Value && !h.MoveToward(s.Goal.Value.x, s.Goal.Value.y)) s.Goal = null;
                moved = h.X != bx || h.Y != by;
                if (moved) s.MovedAt = now;
            }

            // ---- shooting ----
            var w = h.EquippedWeapon;
            if (s.ReloadUntil >= 0f)
            {
                if (now < s.ReloadUntil) return;
                s.ReloadUntil = -1f;
                if (w != null) { if (w.IsJammed) w.ClearJam(); else if (!w.Reload()) { s.Dry = true; Shout(h, "I'm out!"); } }
            }
            if (s.Flee || now < s.NextShot || Body.Down(p) || !p.IsAlive()) return;
            var grid = GameServices.Grid;
            if (grid == null) return;
            if (s.Dry) s.Goal = (p.X, p.Y);
            if (w == null || CombatRules.IsMelee(w) || s.Dry)
            {
                if (CombatantUtil.IsAdjacent(h, p)) { s.NextShot = now + 1.2f; h.MeleeAttack(p); }
                return;
            }
            if (w.IsJammed || w.CurrentMagazine <= 0)
            {
                s.ReloadUntil = now + Gunplay.For(w).ReloadTime * 1.2f;
                Shout(h, w.IsJammed ? "Jammed!" : "Reloading!");
                return;
            }
            var hd = Gunplay.For(w);
            if (CombatantUtil.Distance(h, p) > hd.Reach || !grid.CheckLineOfSight(h.X, h.Y, p.X, p.Y) || !Contact.Knows(h, p)) { s.BurstLeft = 0; return; }

            if (s.BurstLeft <= 0) s.BurstLeft = hd.Auto ? Random.Range(3, 6) : 1;
            w.CurrentMagazine--;
            if (CombatRules.Jams(CombatRules.RollJam(), w)) { w.IsJammed = true; CombatRules.DegradeOnJam(w); s.BurstLeft = 0; return; }
            float extra = (now - s.MovedAt < 0.4f ? 1.5f : 0f) + (Pinned(h) ? 2f : 0f);
            Gunplay.NpcShootOnce(h, w, p, extra);
            s.BurstLeft--;
            int rank = CombatRules.RankIndex(h.Rank);
            float pause = Random.Range(0.7f, 1.6f) * (1.25f - 0.08f * rank) * (Pinned(h) ? 2f : 1f);
            s.NextShot = now + (s.BurstLeft > 0 ? 60f / hd.Rpm : System.Math.Max(60f / hd.Rpm, pause));
        }
    }
}
