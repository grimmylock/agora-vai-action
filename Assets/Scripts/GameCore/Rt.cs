// Real-time fork, batch 1 (ACTION_DESIGN.md): the game runs on the clock instead of turns.
//  - You: WASD moves you freely inside and across tiles (walls collide), Shift sprints on stamina, Ctrl crouches (the old
//    sneak), right mouse aims (slower, steadier, the camera looks further), left mouse fires (held for automatics), R reloads
//    over the gun's reload time (again: clears a jam). Your tile still drives the rules underneath (X/Y, exits, anomalies).
//  - Enemies (bridge until RT batch 4): each one in a fight takes an old-style turn every ~3 s (EnemyAI.TakeTurn): it moves
//    and its gunfire becomes real rounds (Weapon.Fire -> Gunplay.NpcShoot). Melee and mutant strikes still use the old rolls.
//  - Every 6 s of fighting counts as a round for upkeep: bleeding, status effects, radio calls, the fight ending.
//  - The world clock always runs (exploring and fighting alike): a game minute every 1/4.2 s, as Batch 67's exploring clock.
// Rt.On is false in the offline harness (its fight probes stay turn-based) and true in the game (PlayerController.Awake).
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameCore
{
    public static class Rt
    {
        public static bool On, Paused;

        // ---- where each unit stands inside its tile (tile space, y down; within +-0.5) ----
        static readonly Dictionary<ICombatant, (float x, float y)> Off = new Dictionary<ICombatant, (float, float)>();
        public static (float x, float y) Offset(ICombatant u)
        {
            if (u != null && Off.TryGetValue(u, out var o)) return o;
            if (u == null || u is Player) return (0f, 0f);
            int h = u.GetHashCode() * 73856093;                      // NPCs: the fixed spot of Batch 114
            return (((h & 255) / 255f - 0.5f) * 0.55f, -(((h >> 8) & 255) / 255f - 0.5f) * 0.4f);
        }
        public static void SetOffset(ICombatant u, float x, float y) => Off[u] = (x, y);
        public static bool Free(ICombatant u) => On && u is Player;   // drawn where it really is, no tile-to-tile walk

        // ---- tuning (tiles a second; stamina points) ----
        public const float Walk = 2.6f, Sprint = 4.4f, Crouch = 1.3f, AimSlow = 0.65f;
        public const float StaminaMax = 100f, SprintDrain = 18f, StaminaRegen = 14f;
        public const float Radius = 0.22f;
        public const float NpcTurnSeconds = 3f, RoundSeconds = 6f;   // a human enemy re-thinks its plan every ~3 s (RtAi acts in between)

        public static float Stamina = StaminaMax;
        static float _staminaRest;
        public static bool Aiming;
        public static int Moving;                                     // 0 still, 1 walking, 2 sprinting
        public static Vector3 Look;                                   // camera look-ahead (world), eased by TacticalGridView
        static float _round;
        static readonly Dictionary<ICombatant, float> NextTurn = new Dictionary<ICombatant, float>();

        public static void Reset() { RtAi.Reset(); PinnedUntil = -1f; Paused = false; Focus = 0f; DiveUntil = ProneUntil = -1f; SlowMo = false; UsingId = null; Body.Reset(); Off.Clear(); NextTurn.Clear(); Gunplay.Reset(); Stamina = StaminaMax; _round = 0f; }

        /// <summary>Move a unit by (dx, dy) tiles, sliding along walls. Crossing into the next tile is a real step for the
        /// rules (Player.Move: doors, anomalies, exits), so the tile underneath always matches where the body is.</summary>
        public static void MoveBy(Player p, float dx, float dy)
        {
            var (ox, oy) = Offset(p);
            float x = p.X + 0.5f + ox, y = p.Y + 0.5f + oy;
            if (dx != 0f && Clear(x + dx, y)) x += dx;
            if (dy != 0f && Clear(x, y + dy)) y += dy;
            int tx = Mathf.FloorToInt(x), ty = Mathf.FloorToInt(y);
            if (tx != p.X || ty != p.Y)
            {
                p.Mp = System.Math.Max(p.Mp, TurnLoop.WalkMp); p.Ap = System.Math.Max(p.Ap, p.MaxAp);
                int bx = p.X, by = p.Y;
                PlayerController.StepFree(p, tx - p.X, ty - p.Y);
                if (p.X == bx && p.Y == by) return;                       // refused: someone stands there
                if (p.X != tx || p.Y != ty) { SetOffset(p, 0f, 0f); return; }   // thrown elsewhere (an anomaly) or a new map (an exit)
            }
            SetOffset(p, x - (p.X + 0.5f), y - (p.Y + 0.5f));
        }

        /// <summary>The body's circle fits: every tile it overlaps is walkable.</summary>
        static bool Clear(float x, float y)
        {
            for (int i = 0; i < 4; i++)
            {
                float cx = x + (i % 2 == 0 ? -Radius : Radius), cy = y + (i < 2 ? -Radius : Radius);
                if (!TerrainMap.CanEnterTile(Mathf.FloorToInt(cx), Mathf.FloorToInt(cy))) return false;
            }
            return true;
        }

        /// <summary>Your controls, once a frame. False = nothing to do (a panel is open).</summary>
        public static void PlayerTick(Player p, Keyboard k, Mouse m)
        {
            float dt = Time.deltaTime;
            p.Ap = System.Math.Max(p.Ap, p.MaxAp); p.Mp = System.Math.Max(p.Mp, p.MaxMp);   // no turns: AP/MP only gate old actions

            // ---- moving ----
            float ix = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0);
            float iy = (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0) - (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0);
            bool down = Body.Down(p);
            p.Sneaking = down || k.leftCtrlKey.isPressed;
            Aiming = !down && m != null && m.rightButton.isPressed;
            bool wantSprint = k.leftShiftKey.isPressed && !p.Sneaking && !Aiming && !Body.LegHurt(p) && UsingId == null && (ix != 0 || iy != 0);
            bool sprint = wantSprint && Stamina > 1f;
            if (sprint) { Stamina = Mathf.Max(0f, Stamina - SprintDrain * dt); _staminaRest = 0.8f; }
            else if ((_staminaRest -= dt) <= 0f) Stamina = Mathf.Min(StaminaMax, Stamina + StaminaRegen * dt);
            float speed = sprint ? Sprint : p.Sneaking ? Crouch : Walk;
            if (Aiming) speed *= AimSlow;
            if (Body.LegHurt(p)) speed *= 0.6f;                       // RT batch 2: a broken or fractured leg limps
            if (UsingId != null) speed *= 0.5f;                       // patching yourself up
            if (down) speed = Crawl;
            if (p.StatusEffects.Prone) speed = 0f;
            if (Time.time < ProneUntil) speed = 0f;                   // RT batch 3: sprawled after a dive
            float l = Mathf.Sqrt(ix * ix + iy * iy);
            Moving = l > 0f && speed > 0f ? (sprint ? 2 : 1) : 0;
            if (!down && k.spaceKey.wasPressedThisFrame && !Diving && Time.time >= ProneUntil && UsingId == null) TryDive(p, ix, iy, MouseTile(m));
            if (Diving)
            {
                Moving = 0;
                var (bx, by) = Ballistics.Pos(p);
                MoveBy(p, _diveX * DiveSpeed * dt, _diveY * DiveSpeed * dt);
                var (ax, ay) = Ballistics.Pos(p);
                bool blocked = dt > 0f && Mathf.Abs(ax - bx) + Mathf.Abs(ay - by) < DiveSpeedFloor * dt;
                if (Time.time >= DiveUntil || blocked) Land(p);
            }
            else if (Moving > 0) MoveBy(p, ix / l * speed * dt, iy / l * speed * dt);

            // ---- down: bleeding out (faster while crawling); only a stim (belt keys) gets you up ----
            if (down)
            {
                if (Diving) Land(p);
                if (Body.TickDown(p, Moving > 0, dt)) { GameLog.Write("You bled out."); p.Hp = 0; TurnLoop.CheckCombatEnd(); return; }
                TickUse(p);
                return;
            }
            TickUse(p);

            // ---- aiming and firing ----
            var w = p.EquippedWeapon;
            var aim = Gunplay.AimOf(p);
            var hd = Gunplay.For(w);
            Gunplay.Tick(hd, aim, Aiming || Diving, Moving, SlowMo ? Time.unscaledDeltaTime : dt);   // in bullet time your aim keeps real-time speed
            var mw = MouseTile(m);
            TickReload(p, w, aim, Time.time);
            if (k.rKey.wasPressedThisFrame && w != null && aim.ReloadUntil < 0f && UsingId == null && !CombatRules.IsMelee(w)
                && (w.IsJammed || w.CurrentMagazine < w.MagCapacity))
                StartReload(p, w, aim, Time.time);
            bool trigger = m != null && (hd.Auto ? m.leftButton.isPressed : m.leftButton.wasPressedThisFrame);
            if (trigger && aim.Loading > 0) StopLoading(p, w, aim);   // RT 6h: a tube or lever fires what's in; a revolver closes first
            if (trigger && UsingId != null) { UsingId = null; GameLog.Write("You stop to shoot."); }
            if (trigger && w != null && mw.HasValue && aim.ReloadUntil < 0f && Time.time >= aim.NextShot) PlayerFire(p, w, hd, aim, mw.Value);

            // ---- camera looks toward the mouse; further when aiming, further still with a long gun ----
            if (mw.HasValue)
            {
                var (px, py) = Ballistics.Pos(p);
                float reach = Aiming ? (CombatRules.Category(w?.Db) == "sniper" ? 0.75f : 0.5f) : 0.25f;
                Look = new Vector3((mw.Value.x - px) * reach, -(mw.Value.y - py) * reach, 0f);
            }
        }

        /// <summary>R: clear the jam, swap the magazine, or (RT 6h) start loading round by round.</summary>
        public static void StartReload(Player p, Weapon w, Gunplay.Aim aim, float now)
        {
            float speed = (CombatantUtil.HasPerk(p, "QuickHands") ? 0.6f : 1f) * (Body.ArmsHurt(p) > 0 ? 1.5f : 1f);
            if (!w.IsJammed && Gunplay.ByRound(w))
            {
                int n = w.MagCapacity - w.CurrentMagazine;
                float open = Gunplay.Revolver(w) ? Gunplay.CylinderOpen * speed : 0f, per = Gunplay.PerRound(w) * speed;
                aim.Loading = n; aim.ReloadUntil = now + open + per;
                CombatFx.Reload(p, w, open + n * per, false, n);
                GameLog.Write("Loading...");
                return;
            }
            float t = w.IsJammed ? 0.8f : Gunplay.For(w).ReloadTime * speed;
            aim.ReloadUntil = now + t;
            CombatFx.Reload(p, w, t, w.IsJammed);
            GameLog.Write(w.IsJammed ? "Clearing the jam..." : "Reloading...");
        }

        /// <summary>Per frame: a reload's timer ran out - the jam clears, the magazine is in, or one more round goes in.</summary>
        public static void TickReload(Player p, Weapon w, Gunplay.Aim aim, float now)
        {
            if (aim.ReloadUntil < 0f || now < aim.ReloadUntil) return;
            aim.ReloadUntil = -1f;
            if (w == null) { aim.Loading = 0; return; }
            if (aim.Loading <= 0) { if (w.IsJammed) w.ClearJam(); else p.ReloadWeapon(); return; }
            bool more = w.Reload(1) && --aim.Loading > 0 && w.CurrentMagazine < w.MagCapacity;
            Sfx.Play("reload_shell", TacticalGridView.GridToWorld(p.X, p.Y), 0.6f);
            if (more) aim.ReloadUntil = now + Gunplay.PerRound(w) * (CombatantUtil.HasPerk(p, "QuickHands") ? 0.6f : 1f) * (Body.ArmsHurt(p) > 0 ? 1.5f : 1f);
            else { aim.Loading = 0; CombatFx.Reload(p, w, -1f); }
        }

        /// <summary>The trigger during a round-by-round reload: a tube, lever or bolt stops and fires what's in; a revolver
        /// finishes the round in hand and snaps shut (no firing until then).</summary>
        public static void StopLoading(Player p, Weapon w, Gunplay.Aim aim)
        {
            if (Gunplay.Revolver(w)) { aim.Loading = 1; return; }
            aim.Loading = 0; aim.ReloadUntil = -1f;
            CombatFx.Reload(p, w, -1f);
        }

        static void PlayerFire(Player p, Weapon w, Handling hd, Gunplay.Aim aim, (float x, float y) at)
        {
            aim.NextShot = Time.time + 60f / hd.Rpm;
            if (CombatRules.IsMelee(w)) return;                      // ponytail: melee is RT batch 3 (shoulder charge, parry)
            if (w.IsJammed) { CombatFx.Push("shot", p, null, w).Jammed = true; GameLog.Write($"{w.Name} is jammed - R to clear it."); return; }
            if (w.CurrentMagazine <= 0) { Sfx.Play("dryfire", TacticalGridView.GridToWorld(p.X, p.Y)); return; }
            w.CurrentMagazine--;
            if (CombatRules.Jams(CombatRules.RollJam(), w)) { w.IsJammed = true; CombatRules.DegradeOnJam(w); CombatFx.Push("shot", p, null, w).Jammed = true; GameLog.Write($"{w.Name} jams!"); return; }
            var (px, py) = Ballistics.Pos(p);
            float dx = at.x - px, dy = at.y - py, len = Mathf.Sqrt(dx * dx + dy * dy);
            if (len < 0.05f) return;
            var over = UnitUnder(at.x, at.y);
            float h = Ballistics.AimHeight(over, at.y);
            int mx = Mathf.FloorToInt(at.x), my = Mathf.FloorToInt(at.y);
            if (over == null && TerrainMap.CoverAcAt(mx, my) > 0 && !TerrainMap.BlocksSight(mx, my)) h = 0.3f;   // a barrel, sandbags, a desk: shoot the thing itself
            // the crosshair is over a body: aim at that body's footprint, at the height of the pixel under it
            if (over != null) { var (ux, uy) = Ballistics.Pos(over); dx = ux - px; dy = uy - py; len = Mathf.Sqrt(dx * dx + dy * dy); }
            bool ambush = over is Mutant u && !u.IsAggroed;
            if (!EncounterState.InCombat) Contact.ShotFrom(p);
            float spread = Gunplay.Spread(hd, aim, Moving, p.Sneaking);
            spread += 2.5f * Body.ArmsHurt(p) + (Body.Concussed(p) ? 2f : 0f);   // RT batch 2: broken arms sway, a concussion blurs
            if (p.StatusEffects.Suppressed || Time.time < PinnedUntil) spread += 2f;   // RT batch 4: their near misses pin you
            Gunplay.Shoot(p, w, dx / len, dy / len, h, spread, ambush);
            aim.Bloom = System.Math.Min(Gunplay.MaxBloom, aim.Bloom + hd.Kick);   // RT 6d: kick tops out
            if (EncounterState.Enemies.Any(e => e.IsAlive() && e.IsAggroed)) EncounterState.InCombat = true;
            TurnLoop.CheckCombatEnd();
        }

        // ---- RT batch 3: the dive and bullet time (Yan: "you jump and get the slowmo but when it ends you're prone for a tiny
        //      moment, immobilizing you and making it risky"). Space dives the way you're moving (or toward the mouse): airborne
        //      ~0.55 s of game time, time runs at 30% meanwhile, you can aim and fire (the aim steadies at real-time speed).
        //      Landing: prone 0.6 s (Tumbler: 0.35) - no moving, but you can still shoot. Costs 40 Focus, so no chaining.
        //      Focus fills from kills (+25), hits (+4) and enemy rounds that just miss you (+6, Cold Blood x2). ----
        public const float DiveCost = 40f, DiveSpeed = 4.5f, DiveTime = 0.55f, ProneTime = 0.6f, SlowScale = 0.3f, FocusMax = 100f;
        public static float Focus, DiveUntil = -1f, ProneUntil = -1f, PinnedUntil = -1f;
        const float DiveSpeedFloor = 1f;                              // moved slower than this (tiles/s): hit a wall, land now
        static float _diveX, _diveY;
        public static bool SlowMo;
        public static bool Diving => DiveUntil >= 0f;
        public static bool DiveRight => _diveX >= 0f;              // the view tilts the body into the dive
        public static bool Low(ICombatant u) => On && u is Player && (Diving || Time.time < ProneUntil);   // airborne or sprawled: a low target

        public static void AddFocus(float f)
        {
            if (!On) return;
            Focus = Mathf.Min(FocusMax, Focus + f);
        }

        static bool TryDive(Player p, float ix, float iy, (float x, float y)? mouse)
        {
            if (Focus < DiveCost) { GameLog.Write($"Not enough Focus to dive ({Mathf.FloorToInt(Focus)}/{DiveCost:0})."); return false; }
            float dx = ix, dy = iy;
            if (dx == 0f && dy == 0f && mouse.HasValue) { var (px, py) = Ballistics.Pos(p); dx = mouse.Value.x - px; dy = mouse.Value.y - py; }
            float l = Mathf.Sqrt(dx * dx + dy * dy);
            if (l < 0.01f) return false;
            _diveX = dx / l; _diveY = dy / l;
            Focus -= DiveCost;
            DiveUntil = Time.time + DiveTime;
            SetSlow(true);
            Sfx.Play("swing", TacticalGridView.GridToWorld(p.X, p.Y), 1f, 0.05f, 0.6f);
            return true;
        }

        static void Land(Player p)
        {
            DiveUntil = -1f;
            ProneUntil = Time.time + (CombatantUtil.HasPerk(p, "Tumbler") ? 0.35f : ProneTime);
            SetSlow(false);
            Sfx.Play("hit_dirt", TacticalGridView.GridToWorld(p.X, p.Y), 0.8f);
        }

        public static void SetSlow(bool on) { SlowMo = on; if (!Paused) Time.timeScale = on ? SlowScale : 1f; }

        // ---- using an item takes time (RT batch 2): you move at half pace, firing cancels it ----
        public const float Crawl = 0.6f;
        public static string UsingId;
        public static float UseDone;
        public static bool Applying;                                  // true while the finished use runs Player.UseConsumable for real

        public static float UseSeconds(GameData.ConsumableDef d) =>
            d.CureFracture ? 4f : d.CureBleeding ? 3f : d.HealingPermanent > 0 && d.HungerRestore == null ? 4f : d.HealingTemp > 0 ? 1.5f : 2.5f;

        public static void BeginUse(Player p, GameData.ConsumableDef d)
        {
            if (UsingId != null) { GameLog.Write("Already busy."); return; }
            if (Body.Down(p) && !(d.HealingTemp > 0)) { GameLog.Write("You're down - only a stim gets you up."); return; }
            if (d.Throwable != null || d.RepairKit) { Applying = true; try { p.UseConsumable(d.Id); } finally { Applying = false; } return; }
            UsingId = d.Id; UseDone = Time.time + UseSeconds(d);
            GameLog.Write($"Using {d.Name}...");
        }

        static void TickUse(Player p)
        {
            if (UsingId == null || Time.time < UseDone) return;
            string id = UsingId; UsingId = null;
            Applying = true;
            try { p.UseConsumable(id); } finally { Applying = false; }
        }

        /// <summary>The living unit whose sprite the point (tile space) is over, if any - the nearest one.</summary>
        public static ICombatant UnitUnder(float x, float y)
        {
            ICombatant best = null; float bd = 9f;
            foreach (var u in Gunplay.Bodies())
            {
                if (u is Player || !u.IsAlive() || !Vision.Shows(u)) continue;
                var (ux, uy) = Ballistics.Pos(u);
                float tall = u is Mutant mm && !(u is HumanEnemy) ? Ballistics.MutantSize(mm) : 1f;
                // the sprite covers half a tile each side and from its feet up to its crown
                if (System.Math.Abs(x - ux) > 0.45f * tall || y > uy + 0.5f || y < uy + 0.44f - Ballistics.BodyTiles * tall - 0.1f) continue;
                float d = System.Math.Abs(x - ux) + System.Math.Abs(y - uy);
                if (d < bd) { bd = d; best = u; }
            }
            return best;
        }

        public static (float x, float y)? MouseTile(Mouse m)
        {
            if (m == null || Camera.main == null || GameState.Current.ModalOpen) return null;
            var w = Camera.main.ScreenToWorldPoint(m.position.ReadValue());
            return (w.x, -w.y);
        }

        /// <summary>The world, once a frame: enemies in a fight act on their own timers, rounds tick over.</summary>
        public static void WorldTick(Player p)
        {
            float now = Time.time;
            var grid = GameServices.Grid;
            if (grid == null) return;
            foreach (var e in EncounterState.Enemies.ToList())
            {
                if (!e.IsAlive()) { NextTurn.Remove(e); continue; }
                bool fighting = e.IsAggroed && EncounterState.IsActive(e) || Companion.Is(e) && EncounterState.InCombat;
                if (!fighting) { NextTurn.Remove(e); continue; }
                if (e is HumanEnemy rh && !Companion.Is(e)) RtAi.Tick(rh, p);   // RT batch 4: walks and shoots every frame; thinks below
                else if (!(e is HumanEnemy)) RtAi.TickMutant(e, p);              // RT batch 5: mutants too
                if (!NextTurn.TryGetValue(e, out var at)) { NextTurn[e] = now + Random.Range(0.5f, 1.3f); continue; }   // reaction time
                if (now < at) continue;
                NextTurn[e] = now + (e is HumanEnemy ? NpcTurnSeconds : 2f) * Random.Range(0.85f, 1.15f);   // mutants re-decide faster
                try
                {
                    if (Companion.Is(e)) { Companion.Act(p, grid, true); continue; }
                    if (Companion.TakesFire(e, p, grid)) continue;
                    EnemyAI.TakeTurn(e, p);
                }
                catch (System.Exception ex) { Debug.LogException(ex); }
                if (!p.IsAlive()) { TurnLoop.CheckCombatEnd(); return; }
                Perks.TryAdrenaline(p);
            }
            if (!EncounterState.InCombat) { _round = 0f; return; }
            if ((_round += Time.deltaTime) < RoundSeconds) return;
            _round = 0f;
            EnemyAI.BeginEnemyRound(p);
            p.EndTurn(); p.StartTurn();
            AnomalyEngine.ProcessTurnStart(p);
            if (p.IsAlive()) Contact.EndRound(p);
            TurnLoop.CheckCombatEnd();
            if (EncounterState.InCombat) TurnLoop.RadioForHelp();
        }
    }
}
