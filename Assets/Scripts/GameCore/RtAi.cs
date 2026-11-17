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
// RT batch 5: mutants too. EnemyAI.MutantTurn still decides every ~2 s (circle, chase, leap, slam, boulder, re-cloak, flee) but
// with Rt on it only sets where to go (WalkTo -> GoTo, Approach -> Chase); TickMutant walks them at their own speed (MP-based:
// hounds ~3.6-4.8 tiles/s, the Brute ~2.4) and strikes whenever you're in reach (every ~1 s, the big ones ~1.6 s).
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
            public bool Flee, Chasing;
            public float NextStep, NextShot, ReloadUntil = -1f, PinnedUntil = -1f, MovedAt = -9f;
            public int BurstLeft;
            public float ClearAt = -9f, MoveForShotAt = -9f; public bool Clear = true;   // the round's path to the target is open (re-traced every 0.25 s); when it is not he stops firing and looks for a tile with a clear line
            public float FarShotAt = -9f;                             // the last probing shot taken from outside his effective range while still closing in
            public bool Dry;                                          // out of rounds: closes in to swing
            public float ReadyAt = -1f, CanShootAt = -9f;             // 2026-11-17: when he is done reacting to a target he can finally shoot at (ReactionSeconds), and when he last could
        }
        static readonly Dictionary<Mutant, Mind> Minds = new Dictionary<Mutant, Mind>();
        static Mind Of(Mutant m) { if (!Minds.TryGetValue(m, out var s)) Minds[m] = s = new Mind(); return s; }
        public static void Reset() => Minds.Clear();

        /// <summary>How long a human enemy takes to react to a target he can shoot at before the first round: 0.95 s for a Rookie, 0.1 s less for every rank above (0.35 s for a Legend), +/-20%.</summary>
        public static float ReactionSeconds(HumanEnemy h) => Random.Range(0.8f, 1.2f) * System.Math.Max(0.25f, 0.95f - 0.1f * CombatRules.RankIndex(h.Rank));

        public static void SetGoal(HumanEnemy h, (int x, int y) goal, string role)
        {
            var s = Of(h);
            if (role == "Flanker" && s.Goal != goal && (h.X, h.Y) != goal && Random.value < 0.4f) Banter.Say(h, "Flanking!", 3);
            s.Goal = goal; s.Flee = false; s.Chasing = false;
        }

        public static void Flee(Mutant m) { var s = Of(m); s.Flee = true; s.Goal = null; s.Chasing = false; }
        public static void GoTo(Mutant m, (int x, int y) goal) { var s = Of(m); s.Goal = goal; s.Flee = false; s.Chasing = false; }
        public static void Chase(Mutant m) { var s = Of(m); s.Chasing = true; s.Goal = null; s.Flee = false; }

        /// <summary>RT batch 5: tiles a second. Mutants by their MP (hounds fast, the Brute slow); humans walk at 2.2.</summary>
        public static float Speed(Mutant m)
        {
            float v = (m is HumanEnemy ? 2.2f : System.Math.Max(1.8f, System.Math.Min(5f, 0.6f * (m.Db?.Mp ?? 4)))) * Gaunt.Hurry(m);   // a mutant the Gaunt drives runs 35% faster
            if (m.Db?.Name == "Swarm") v = System.Math.Min(v, Rt.Sprint * 0.7f);        // Yan, 2026-11-17: "the rot swarm is waay too fast and can't be outrun": the rats are slower than a sprinting player (70%: 88% still caught Yan), driven or not
            return v;
        }

        /// <summary>RT batch 5: one frame of a mutant in a fight - walks/chases/flees at its own speed, strikes when in reach.</summary>
        public static void TickMutant(Mutant m, Player p)
        {
            var s = Of(m);
            float now = Time.time;
            if (m.StatusEffects.Prone) return;
            if (Gaunt.Busy(m)) return;                                                                     // a Gaunt mid-call stands and screams
            if (!s.Flee && MutantAttack.Has(m) && MutantAttack.Update(m, p, now, ref s.NextShot)) return;   // 2026-11-12: a canine plays its attack animation (no walking, the strike lands on its contact frame)
            if (now >= s.NextStep)
            {
                s.NextStep = now + 1f / Speed(m) * (m.StatusEffects.Wound == "Leg" ? 1.7f : 1f);
                int stance = s.Chasing && !s.Flee ? MutantAttack.Stance(m, p, now) : 2;      // a canine in a pack keeps its own distance (MutantAttack.Stance)
                if (s.Flee) m.MoveAwayFrom(p);
                else if (stance == 1) m.MoveToward(p.X, p.Y);
                else if (stance == -1) m.MoveAwayFrom(p);
                else if (stance == 0) { }
                else if (stance == 3) { if (MutantAttack.FlankGoal(m, p, out int fgx, out int fgy)) m.MoveToward(fgx, fgy); }      // level with the target before a lunge (profile sprites)
                else if (s.Chasing && !CombatantUtil.IsAdjacent(m, p) && !m.IsInEngageRange(p)) m.MoveToward(p.X, p.Y);
                else if (s.Goal.HasValue && (m.X, m.Y) != s.Goal.Value && !m.MoveToward(s.Goal.Value.x, s.Goal.Value.y)) s.Goal = null;
            }
            if (s.Flee || now < s.NextShot || Body.Down(p) || !p.IsAlive() || !m.IsInEngageRange(p)) return;
            s.NextShot = now + (Ballistics.MutantSize(m) >= 2f ? 1.6f : 1.0f) * Random.Range(0.85f, 1.2f);
            m.Attack(p);
        }

        /// <summary>A tile two steps to one side of the line to the target (whichever side is open), for a clear shot.</summary>
        static (int x, int y)? SideStep(HumanEnemy h, ICombatant p)
        {
            int dx = System.Math.Sign(p.X - h.X), dy = System.Math.Sign(p.Y - h.Y);
            foreach (int side in Random.value < 0.5f ? new[] { 1, -1 } : new[] { -1, 1 })
            {
                int x = h.X - dy * side * 2, y = h.Y + dx * side * 2;           // perpendicular to (dx, dy)
                if (TerrainMap.CanEnterTile(x, y) && TerrainMap.UnitAt(x, y) == null) return (x, y);
            }
            return null;
        }

        /// <summary>Holds its fire for `seconds` (caught off guard: the shakedown's "shoot first").</summary>
        /// <summary>A mutant busy with something else (the story's soldiers): it neither walks nor bites for `seconds`.</summary>
        public static void Pin(Mutant m, float seconds) { var s = Of(m); float t = Time.time + seconds; s.NextStep = System.Math.Max(s.NextStep, t); s.NextShot = System.Math.Max(s.NextShot, t); }

        /// <summary>Seconds until this mutant may walk and strike again (a parry or a story pin pushes it out); 0 when free.</summary>
        public static float BusyFor(Mutant m) => Minds.TryGetValue(m, out var s) ? System.Math.Max(0f, System.Math.Min(s.NextStep, s.NextShot) - Time.time) : 0f;

        public static void Hold(Mutant m, float seconds) { var s = Of(m); s.NextShot = System.Math.Max(s.NextShot, Time.time + seconds); s.BurstLeft = 0; }

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
            if (Melee.Busy(h)) return;                                             // RT 7a: wrenching a blade out of himself

            // ---- walking ----
            if (now >= s.NextStep && !Pinned(h))
            {
                s.NextStep = now + StepSeconds * (h.StatusEffects.Wound == "Leg" ? 1.7f : 1f);
                bool moved = false;
                int bx = h.X, by = h.Y;
                if (s.Flee) h.MoveAwayFrom(p);
                else if (s.Goal.HasValue && (h.X, h.Y) != s.Goal.Value && !h.MoveToward(s.Goal.Value.x, s.Goal.Value.y)) s.Goal = null;
                moved = h.X != bx || h.Y != by;
                if (moved) s.MovedAt = now;
                if (moved && Rt.Ledge(h.X, h.Y)) s.NextStep += StepSeconds;   // climbing over low cover, as you do (Rt.TryVault)
            }

            Shoot(h, s, p, p, null);
        }

        /// <summary>One frame of a story soldier fighting a mutant: the same trigger discipline as `Tick` (no walking: the story walks him).</summary>
        public static void TickFoe(HumanEnemy h, Mutant foe)
        {
            var s = Of(h);
            if (s.PinnedUntil >= 0f && Time.time >= s.PinnedUntil) { s.PinnedUntil = -1f; h.StatusEffects.Suppressed = false; }
            if (h.StatusEffects.Prone || Melee.Busy(h)) return;
            Shoot(h, s, foe, null, null);
        }

        /// <summary>The shooting half of a frame: his own gun's rules (bursts, rpm, magazine and reload time, jams, the fair-shot reach, a friend in the line). `pl` = the target as a Player (the fair-shot vision rules), null for a mutant.</summary>
        public static bool ClearCheck = true;                          // the harness switches the path check off to compare (RTBAL=7, CLEAR=0)
        static void Shoot(HumanEnemy h, Mind s, ICombatant t, Player pl, object _)
        {
            float now = Time.time;
            var w = h.EquippedWeapon;
            if (s.ReloadUntil >= 0f)
            {
                if (now < s.ReloadUntil) return;
                s.ReloadUntil = -1f;
                if (w != null) { if (w.IsJammed) w.ClearJam(); else if (!w.Reload()) { s.Dry = true; Shout(h, "I'm out!"); } }
            }
            if (s.Flee || now < s.NextShot || (pl != null && Body.Down(pl)) || !t.IsAlive()) return;
            var grid = GameServices.Grid;
            if (grid == null) return;
            if (s.Dry && pl != null) s.Goal = (t.X, t.Y);
            if (w == null || CombatRules.IsMelee(w) || s.Dry)
            {
                if (CombatantUtil.IsAdjacent(h, t))
                {
                    s.NextShot = now + 1.2f;
                    if (w != null && CombatRules.IsMelee(w)) w.Fire(t);                  // Melee 2c: a real swing with his blade (it winds up: parry it)
                    else h.MeleeAttack(t);                                             // out of rounds: fists and the rifle butt
                }
                return;
            }
            if (w.IsJammed || w.CurrentMagazine <= 0)
            {
                int missing = w.MagCapacity - w.CurrentMagazine;
                float rt = w.IsJammed ? 0.8f : Gunplay.ByRound(w) ? (Gunplay.Revolver(w) ? Gunplay.CylinderOpen : 0f) + missing * Gunplay.PerRound(w) : Gunplay.ReloadSeconds(w);   // RT 6h, 6k
                s.ReloadUntil = now + rt * 1.2f;
                CombatFx.Reload(h, w, s.ReloadUntil - now, w.IsJammed, missing);
                Shout(h, w.IsJammed ? "Jammed!" : "Reloading!");
                return;
            }
            var hd = Gunplay.For(w);
            // a fair shot (Yan: "i can't see past a certain range but enemies keep shooting me ... Enemies also see me through walls"):
            // only from as far as you could see him if you looked (Vision.LookReach), and the line of sight has to hold both ways -
            // a tile line traced from him to you can slip past a corner that the line from you to him doesn't
            if (pl != null ? (CombatantUtil.Distance(h, t) > System.Math.Min(hd.Reach, Rt.Of(pl).LookReach) || !Contact.Mutual(h, pl) || !Contact.Knows(h, pl))
                : (CombatantUtil.Distance(h, t) > hd.Reach || !grid.CheckLineOfSight(h.X, h.Y, t.X, t.Y))) { s.BurstLeft = 0; return; }   // the story's soldiers shoot mutants: plain line of sight

            // 2026-11-17 (Yan: "enemies try to move in the effective range before shooting too much, that way fights happen more inside the normal vision range (besides snipers)"): while he is still walking
            // closer (he has a goal he has not reached) and the target is past half his gun's reach, he only takes a probing shot every 3 s; snipers shoot from where they are.
            if (pl != null && CombatRules.IsShotgun(w) && w.CurrentAmmoType != "Slug" && CombatantUtil.Distance(h, t) > Gunplay.EffectiveRange(w)) { s.BurstLeft = 0; return; }   // buckshot is for inside half its reach
            if (pl != null && CombatRules.Category(w.Db) != "sniper" && CombatantUtil.Distance(h, t) > Gunplay.EffectiveRange(w) && s.Goal.HasValue && (h.X, h.Y) != s.Goal.Value && !s.Flee)
            {
                if (now < s.FarShotAt + 3f) { s.BurstLeft = 0; return; }
                s.FarShotAt = now; s.BurstLeft = 1;
            }

            // 2026-11-17 (Yan, after RTBAL=7: ~65% of the rounds the raiders fired ended in walls and cover): the round's own path has to be open, not just the tile line of sight. Blocked: hold fire,
            // and after a second try a tile within 4 of him from which the path is open (else a step toward the target), at most one new goal every 1.2 s.
            if (pl != null && ClearCheck)
            {
                var (hx, hy) = Ballistics.Pos(h);
                if (now >= s.ClearAt) { s.ClearAt = now + 0.25f; s.Clear = Gunplay.ClearShotFrom(h, hx, hy, t); }
                if (!s.Clear)
                {
                    s.BurstLeft = 0;
                    if (now >= s.MoveForShotAt && !Pinned(h))
                    {
                        s.MoveForShotAt = now + 1.2f; (int x, int y)? pick = null; float bestD = 1e9f;
                        for (int ox = -4; ox <= 4 && pick == null; ox += 2) for (int oy = -4; oy <= 4; oy += 2)
                        {
                            int x = h.X + ox, y = h.Y + oy; if ((ox == 0 && oy == 0) || !TerrainMap.CanEnterTile(x, y) || TerrainMap.UnitAt(x, y) != null) continue;
                            float d = CombatantUtil.Distance(h, t) - (float)System.Math.Sqrt((x - t.X) * (x - t.X) + (y - t.Y) * (y - t.Y));
                            if (Gunplay.ClearShotFrom(h, x + 0.5f, y + 0.5f, t) && -d < bestD) { bestD = -d; pick = (x, y); }
                        }
                        if (pick == null) { int x = h.X + System.Math.Sign(t.X - h.X) * 3, y = h.Y + System.Math.Sign(t.Y - h.Y) * 3; if (TerrainMap.CanEnterTile(x, y)) pick = (x, y); }
                        if (pick != null) s.Goal = pick;
                    }
                    return;
                }
            }

            if (Gunplay.AllyInLine(h, t))                                  // a friend in the line of fire: hold, and step aside for a clear shot
            {
                s.BurstLeft = 0; s.NextShot = now + 0.3f;
                if (s.Goal == null || (h.X, h.Y) == s.Goal.Value) s.Goal = SideStep(h, t);
                return;
            }
            // Yan (2026-11-17): a gang of raiders "gunned me down in like 0.2 seconds" the moment the talk turned: nobody opens fire in the same frame. He reacts first (ReactionSeconds),
            // then his aim settles over 0.8 s, like the player's does. Losing the target for more than 2.5 s makes him react again.
            if (s.ReadyAt < 0f || now - s.CanShootAt > 2.5f) s.ReadyAt = now + ReactionSeconds(h);
            s.CanShootAt = now;
            if (now < s.ReadyAt) { s.BurstLeft = 0; return; }
            if (s.BurstLeft <= 0) s.BurstLeft = hd.Auto ? Random.Range(3, 6) : 1;
            w.CurrentMagazine--;
            float extra = (now - s.MovedAt < 0.4f ? 1.5f : 0f) + (Pinned(h) ? 2f : 0f) + 2.5f * Mathf.Clamp01(1f - (now - s.ReadyAt) / 0.8f);
            Gunplay.NpcShootOnce(h, w, t, extra);
            s.BurstLeft--;
            int rank = CombatRules.RankIndex(h.Rank);
            // the rest between bursts (an automatic) or between two pulls (anything else): the old 0.7-1.6 s pause came from the turn-based days and made a rifleman's real rate a third of
            // the gun's, and a pistol one shot a second (2026-11-17: a Rookie with every shot landing took 25 s to kill a player who stood still). Now a short, rank-scaled breath.
            float pause = (hd.Auto ? Random.Range(0.3f, 0.8f) : Random.Range(0.2f, 0.45f)) * (1.25f - 0.08f * rank) * (Pinned(h) ? 2f : 1f);
            s.NextShot = now + (s.BurstLeft > 0 ? 60f / hd.Rpm : System.Math.Max(60f / hd.Rpm, pause));
        }
    }
}
