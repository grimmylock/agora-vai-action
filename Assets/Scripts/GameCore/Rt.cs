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
            var (sx, sy) = u != null && Slide.TryGetValue(u, out var sl) ? sl : (0f, 0f);
            if (u != null && Off.TryGetValue(u, out var o)) return (o.x + sx, o.y + sy);
            if (u == null || u is Player) return (0f, 0f);
            int h = CombatantUtil.StableId(u) * 73856093;             // NPCs: the fixed spot of Batch 114 (RT 7z-d: stable, not GetHashCode)
            return (((h & 255) / 255f - 0.5f) * 0.55f + sx, -(((h >> 8) & 255) / 255f - 0.5f) * 0.4f + sy);
        }
        public static void SetOffset(ICombatant u, float x, float y) => Off[u] = (x, y);
        // the first real-time playtest ("some of my shots seem to pass straight through them"): an NPC's tile changes in one jump
        // and its sprite used to walk over after it, so the body you shot at was up to a tile behind its hitbox. Now the jump is
        // undone as a slide that runs out at its walking speed (Glide), and everyone is drawn where the rounds see them.
        static readonly Dictionary<ICombatant, (float x, float y)> Was = new Dictionary<ICombatant, (float, float)>();
        static readonly Dictionary<ICombatant, (float x, float y)> Slide = new Dictionary<ICombatant, (float, float)>();
        public static bool Free(ICombatant u) => On && u != null;     // drawn where it really is, no tile-to-tile walk

        public static void Glide(float dt)
        {
            foreach (var e in EncounterState.Enemies)
            {
                // Melee 2e (Yan: "characters teleporting around"): the spot inside the tile too (a step into reach before a swing),
                // not only the tile - every jump of where he stands slides
                var (sx, sy) = Slide.TryGetValue(e, out var sl) ? sl : (0f, 0f);
                var (ox, oy) = Offset(e); float bx = e.X + 0.5f + ox - sx, by = e.Y + 0.5f + oy - sy;
                if (!Was.TryGetValue(e, out var w)) { Was[e] = (bx, by); continue; }
                if (Mathf.Abs(w.x - bx) + Mathf.Abs(w.y - by) > 0.001f)
                {
                    float jx = w.x - bx, jy = w.y - by;
                    if (Mathf.Abs(jx) <= 2.5f && Mathf.Abs(jy) <= 2.5f) { sx += jx; sy += jy; } else { sx = 0f; sy = 0f; }   // a jump far off is a teleport
                    Was[e] = (bx, by);
                }
                float len = Mathf.Sqrt(sx * sx + sy * sy), step = Mathf.Max(RtAi.Speed(e) * 1.15f, len > 1.2f ? len / RushTime : 0f) * dt;   // a lunge rushes (game time: bullet time slows it too)
                if (len <= step || !e.IsAlive()) { sx = 0f; sy = 0f; } else { sx *= (len - step) / len; sy *= (len - step) / len; }
                Slide[e] = (sx, sy);
            }
        }

        // ---- tuning (tiles a second; stamina points) ----
        public const float Walk = 2.6f, Sprint = 4.4f, Crouch = 1.3f, AimSlow = 0.65f;
        public const float StaminaMax = 100f, SprintDrain = 18f, StaminaRegen = 14f;
        public const float Radius = 0.22f;
        public const float NpcTurnSeconds = 3f, RoundSeconds = 6f;
        public const float RushTime = 0.12f;                          // Glide: a jump of more than 1.2 tiles (a lunge) closes in about this long   // a human enemy re-thinks its plan every ~3 s (RtAi acts in between)

        // Co-op groundwork (Yan: "if we decided to build for it from the start why are we building NOT for it?" - ACTION_DESIGN
        // "Co-op"): everything that belongs to one player's body and hands lives on that player, not in globals. The local player
        // is Me (Player.Current); a joined player (Player.Join) gets its own State and is driven by its own Input packets.
        public class State
        {
            public float Stamina = StaminaMax, StaminaRest, Focus, DiveUntil = -1f, ProneUntil = -1f, PinnedUntil = -1f, DiveX, DiveY;
            public float VaultFrom = -1f, VaultLen, VaultX, VaultY, VaultDist, UseDone, UseFrom;
            public float LookReach = Vision.MaxRadius;                // how far this player sees where they look (Vision, for fair shots)
            public bool Aiming;
            public int Moving;                                        // 0 still, 1 walking, 2 sprinting
            public Vector3 Look;                                      // camera look-ahead (world), eased by TacticalGridView
            public string UsingId;
            public float Reviving;                                    // seconds E has been held over a downed friend
            public (float x, float y)? AimAt;                         // RT 7z: where this player's crosshair is (tile space) - friends' guns point there
            public bool ReloadOffhand;                                // RT 7z-c: R was pressed while the right-hand gun reloaded: the left one is next
            public bool Dead;                                         // co-op: the dead penalty has been taken (Death.Tick)
            public float ParryAt = -99f, ParryReady;                  // Boss 1: when the parry key went down, when it can again
            public bool Diving => DiveUntil >= 0f;
            public bool DiveRight => DiveX >= 0f;                     // the view tilts the body into the dive
            public bool Vaulting => VaultFrom >= 0f;
            /// <summary>The hop's height now (tiles), for the view - 0 when on their feet.</summary>
            public float VaultLift => Vaulting ? VaultHeight * Mathf.Sin(Mathf.PI * Mathf.Clamp01((Time.time - VaultFrom) / VaultLen)) : 0f;
        }
        static readonly Dictionary<Player, State> States = new Dictionary<Player, State>();
        public static State Of(Player p) { if (p == null) return new State(); if (!States.TryGetValue(p, out var s)) States[p] = s = new State(); return s; }
        public static State Me => Of(Player.Current);

        /// <summary>One frame of one player's controls - read from the keyboard and mouse for the local player (Read), or sent
        /// by a joined player. Everything Rt does with a player goes through this, never the devices directly.</summary>
        public struct Input
        {
            public float X, Y;                                        // move, -1..1 each (y down)
            public bool Sneak, Sprint, Aim, Fire, FireDown, Jump, Reload, Use;   // held / held / held / held / pressed / pressed / pressed / held (E)
            public bool Fire2Down;                                    // RT 7z-c: right mouse pressed - a Gunslinger's left-hand gun
            public bool Parry;                                        // Boss 1: the parry key pressed (F by default, Keybinds)
            public (float x, float y)? Mouse;                         // the crosshair, tile space
        }
        public static Input Read(Keyboard k, Mouse m) => new Input
        {
            X = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0),
            Y = (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0) - (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0),
            Sneak = k.leftCtrlKey.isPressed, Sprint = k.leftShiftKey.isPressed, Jump = k.spaceKey.wasPressedThisFrame, Reload = k.rKey.wasPressedThisFrame, Use = k.eKey.isPressed,
            Aim = m != null && m.rightButton.isPressed, Fire = m != null && m.leftButton.isPressed, FireDown = m != null && m.leftButton.wasPressedThisFrame,
            Fire2Down = m != null && m.rightButton.wasPressedThisFrame,
            Parry = Keybinds.Pressed(k, Keybinds.Parry),
            Mouse = MouseTile(m),
        };
        static float _round;

        /// <summary>The last Input each joined player sent (the network layer fills it; the harness too). Ticked with the local one.</summary>
        public static readonly Dictionary<Player, Input> Remote = new Dictionary<Player, Input>();

        /// <summary>Co-op: who an enemy fights - the nearest living player it has a line of sight to, else the nearest living one.
        /// `fallback` (the local player) when nobody else qualifies.</summary>
        public static Player TargetOf(Mutant e, Player fallback)
        {
            Player best = null; int bd = int.MaxValue; bool bestSees = false;
            var grid = GameServices.Grid;
            foreach (var p in Player.All)
            {
                if (!p.IsAlive()) continue;
                int d = CombatantUtil.Distance(e, p);
                bool sees = grid != null && grid.CheckLineOfSight(e.X, e.Y, p.X, p.Y);
                if (best == null || sees && !bestSees || sees == bestSees && d < bd) { best = p; bd = d; bestSees = sees; }
            }
            return best ?? fallback;
        }
        static readonly Dictionary<ICombatant, float> NextTurn = new Dictionary<ICombatant, float>();

        public static void Reset() { RtAi.Reset(); Paused = false; States.Clear(); Remote.Clear(); SlowMo = false; Body.Reset(); Off.Clear(); Was.Clear(); Slide.Clear(); NextTurn.Clear(); Gunplay.Reset(); Melee.Reset(); Boss.Reset(); _round = 0f; }

        /// <summary>Move a unit by (dx, dy) tiles, sliding along walls. Crossing into the next tile is a real step for the
        /// rules (Player.Move: doors, anomalies, exits), so the tile underneath always matches where the body is.</summary>
        public static void MoveBy(Player p, float dx, float dy)
        {
            var (ox, oy) = Offset(p);
            float x = p.X + 0.5f + ox, y = p.Y + 0.5f + oy;
            if (dx != 0f && Clear(p, x + dx, y) && !Crowds(p, x, y, x + dx, y)) x += dx;
            if (dy != 0f && Clear(p, x, y + dy) && !Crowds(p, x, y, x, y + dy)) y += dy;
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

        // Yan (2026-10-29): "add the minimum distance between bodies so that they don't overlap, but dead bodies can be walked
        // over". The living (and not downed) keep BodyGap tiles between their middles: you can't walk closer, and one that steps in
        // on you (an NPC walks tile to tile) pushes you back out. ponytail: only players move to keep it - NPCs stand on tile middles,
        // one to a tile, so two of them never overlap.
        public const float BodyGap = 0.8f, PushSpeed = 4f;
        static IEnumerable<ICombatant> Bodies(Player p) =>
            EncounterState.Enemies.Cast<ICombatant>().Concat(Player.All).Where(u => u != p && u.IsAlive() && !Body.Down(u));
        /// <summary>Would moving from (x0, y0) to (x1, y1) take p closer to a body already inside the gap?</summary>
        static bool Crowds(Player p, float x0, float y0, float x1, float y1)
        {
            foreach (var u in Bodies(p))
            {
                var (ux, uy) = Ballistics.Pos(u);
                float d1 = (x1 - ux) * (x1 - ux) + (y1 - uy) * (y1 - uy);
                if (d1 < BodyGap * BodyGap && d1 < (x0 - ux) * (x0 - ux) + (y0 - uy) * (y0 - uy)) return true;
            }
            return false;
        }
        /// <summary>Pushed back out of anyone standing too close.</summary>
        static void Separate(Player p, float dt)
        {
            var (px, py) = Ballistics.Pos(p);
            foreach (var u in Bodies(p))
            {
                var (ux, uy) = Ballistics.Pos(u);
                float dx = px - ux, dy = py - uy, d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d >= BodyGap) continue;
                if (d < 0.01f) { dx = 1f; dy = 0f; d = 1f; }
                float push = Mathf.Min(BodyGap - d, PushSpeed * dt);
                MoveBy(p, dx / d * push, dy / d * push);
                return;
            }
        }

        /// <summary>The body's circle fits: every tile it overlaps is walkable - and, unless you're vaulting, not low cover
        /// (Yan: "i can walk right through cover like the sandbags"). The tile you're in never blocks (you can always step off).</summary>
        static bool Clear(Player p, float x, float y)
        {
            bool vaulting = Of(p).Vaulting;
            for (int i = 0; i < 4; i++)
            {
                int tx = Mathf.FloorToInt(x + (i % 2 == 0 ? -Radius : Radius)), ty = Mathf.FloorToInt(y + (i < 2 ? -Radius : Radius));
                if (!TerrainMap.CanEnterTile(tx, ty)) return false;
                if (!vaulting && Ledge(tx, ty) && (tx != p.X || ty != p.Y)) return false;
            }
            return true;
        }

        // ---- vaulting: Space at low cover hops you over it (no Focus, no landing on your face like a dive) ----
        public const float VaultTime = 0.45f, VaultHeight = 0.35f;     // seconds; tiles the body rises at the top of the hop
        public static bool Ledge(int x, int y) { var t = TerrainMap.GetTile(x, y).Type; return t == TileType.HardCover || t == TileType.ScatterCover; }

        /// <summary>Low cover within reach the way you're going (the keys, else the mouse) with floor beyond it: hop over.</summary>
        internal static void TickVault(Player p, float dt)
        {
            var s = Of(p);
            MoveBy(p, s.VaultX * s.VaultDist / s.VaultLen * dt, s.VaultY * s.VaultDist / s.VaultLen * dt);
            if (Time.time - s.VaultFrom >= s.VaultLen) s.VaultFrom = -1f;
        }

        internal static bool TryVault(Player p, float ix, float iy, (float x, float y)? mouse)
        {
            var (px, py) = Ballistics.Pos(p);
            float dx = ix, dy = iy;
            if (dx == 0f && dy == 0f && mouse.HasValue) { dx = mouse.Value.x - px; dy = mouse.Value.y - py; }
            float l = Mathf.Sqrt(dx * dx + dy * dy);
            if (l < 0.01f) return false;
            dx /= l; dy /= l;
            for (float d = 0.5f; d <= 1.2f; d += 0.1f)                   // cover just ahead...
            {
                int cx = Mathf.FloorToInt(px + dx * d), cy = Mathf.FloorToInt(py + dy * d);
                if ((cx, cy) == (p.X, p.Y) || !Ledge(cx, cy)) continue;
                for (float e = d + 0.6f; e <= d + 1.6f; e += 0.1f)       // ...and open floor past it
                {
                    float ex = px + dx * e, ey = py + dy * e;
                    int fx = Mathf.FloorToInt(ex), fy = Mathf.FloorToInt(ey);
                    if (!TerrainMap.CanEnterTile(fx, fy)) return false;
                    if (Ledge(fx, fy) || TerrainMap.UnitAt(fx, fy, p) != null) continue;
                    var s = Of(p);
                    s.VaultX = dx; s.VaultY = dy; s.VaultDist = e;
                    s.VaultLen = CombatantUtil.HasPerk(p, "Vault") ? VaultTime * 0.7f : VaultTime;
                    s.VaultFrom = Time.time;
                    Sfx.Play("swing", TacticalGridView.GridToWorld(p.X, p.Y), 0.6f, 0.05f, 0.8f);
                    return true;
                }
                return false;
            }
            return false;
        }

        public const float ReviveSeconds = 3f;

        /// <summary>A friend within arm's reach you can get up: downed, or (out of combat) dead. Null if none.</summary>
        public static Player DownedNear(Player p)
        {
            var (px, py) = Ballistics.Pos(p);
            foreach (var o in Player.All)
            {
                if (o == p || (o.IsAlive() ? !Body.Down(o) : EncounterState.InCombat || TurnLoop.AllDead)) continue;
                var (ox, oy) = Ballistics.Pos(o);
                if ((ox - px) * (ox - px) + (oy - py) * (oy - py) <= 1.5f * 1.5f) return o;
            }
            return null;
        }

        /// <summary>One player's controls, once a frame.</summary>
        public static void PlayerTick(Player p, Input i)
        {
            float dt = Time.deltaTime;
            var s = Of(p);
            p.Ap = System.Math.Max(p.Ap, p.MaxAp); p.Mp = System.Math.Max(p.Mp, p.MaxMp);   // no turns: AP/MP only gate old actions
            if (i.Parry) Parry.Press(p);                                                       // Boss 1

            // ---- reviving: hold E over a downed friend (Yan: "downed players can be revived by holding E over them for a few seconds") ----
            bool down = Body.Down(p);
            var friend = down ? null : DownedNear(p);
            if (friend != null && i.Use)
            {
                if ((s.Reviving += dt) >= ReviveSeconds)
                {
                    s.Reviving = 0f;
                    if (friend.IsAlive()) { Body.GetUp(friend); GameLog.Write("You get your friend back on their feet."); }
                    else Death.Revive(friend);                        // dead, out of combat (Death)
                }
                i.X = i.Y = 0f;                                       // you kneel there while you do it
            }
            else s.Reviving = 0f;

            // ---- moving ----
            float ix = i.X, iy = i.Y;
            if (i.Mouse.HasValue) s.AimAt = i.Mouse;
            p.Sneaking = down || i.Sneak;
            s.Aiming = !down && i.Aim && !p.Gunslinging;              // RT 7z-c: with two pistols the right button is the left gun
            bool wantSprint = i.Sprint && !p.Sneaking && !s.Aiming && !Body.LegHurt(p) && s.UsingId == null && (ix != 0 || iy != 0);
            bool sprint = wantSprint && s.Stamina > 1f;
            if (sprint) { s.Stamina = Mathf.Max(0f, s.Stamina - SprintDrain * dt); s.StaminaRest = 0.8f; }
            else if ((s.StaminaRest -= dt) <= 0f) s.Stamina = Mathf.Min(StaminaMax, s.Stamina + StaminaRegen * dt);
            float speed = sprint ? Sprint : p.Sneaking ? Crouch : Walk;
            if (s.Aiming) speed *= AimSlow;
            if (Body.LegHurt(p)) speed *= 0.6f;                       // RT batch 2: a broken or fractured leg limps
            if (s.UsingId != null) speed *= 0.5f;                     // patching yourself up
            if (down) speed = Crawl;
            if (p.StatusEffects.Prone) speed = 0f;
            if (Time.time < s.ProneUntil) speed = 0f;                 // RT batch 3: sprawled after a dive
            float l = Mathf.Sqrt(ix * ix + iy * iy);
            s.Moving = l > 0f && speed > 0f ? (sprint ? 2 : 1) : 0;
            if (!down && i.Jump && !s.Diving && !s.Vaulting && Time.time >= s.ProneUntil && s.UsingId == null && !TryVault(p, ix, iy, i.Mouse))
                TryDive(p, ix, iy, i.Mouse);                          // Space at low cover vaults it; anywhere else it dives
            if (s.Vaulting) { s.Moving = 0; TickVault(p, dt); }
            else if (s.Diving)
            {
                s.Moving = 0;
                var (bx, by) = Ballistics.Pos(p);
                float k = DiveSpeedAt(DiveTime - (s.DiveUntil - Time.time));   // RT 7z: slow in the crouch, fast in the air, a skid on landing
                MoveBy(p, s.DiveX * DiveSpeed * k * dt, s.DiveY * DiveSpeed * k * dt);
                var (ax, ay) = Ballistics.Pos(p);
                bool blocked = dt > 0f && Mathf.Abs(ax - bx) + Mathf.Abs(ay - by) < DiveSpeedFloor * dt;
                if (Time.time >= s.DiveUntil || blocked) Land(p);
            }
            else if (s.Moving > 0) MoveBy(p, ix / l * speed * dt, iy / l * speed * dt);
            if (!down) Separate(p, dt);

            // ---- down: bleeding out (faster while crawling); only a stim (belt keys) gets you up ----
            if (down)
            {
                if (s.Diving) Land(p);
                if (Body.TickDown(p, s.Moving > 0, dt)) { GameLog.Write("You bled out."); p.Hp = 0; TurnLoop.CheckCombatEnd(); return; }
                TickUse(p);
                return;
            }
            TickUse(p);

            // ---- aiming and firing ----
            var w = p.EquippedWeapon;
            var aim = Gunplay.AimOf(p);
            var hd = Gunplay.For(w);
            Gunplay.Tick(hd, aim, s.Aiming || s.Diving, s.Moving, SlowMo ? Time.unscaledDeltaTime : dt);   // in bullet time your aim keeps real-time speed
            var mw = i.Mouse;
            TickReload(p, w, aim, Time.time);
            if (i.Reload && w != null && aim.ReloadUntil < 0f && s.UsingId == null && !CombatRules.IsMelee(w)
                && (w.IsJammed || w.CurrentMagazine < w.MagCapacity))
                StartReload(p, w, aim, Time.time);
            if (w != null && CombatRules.IsMelee(w))                                         // RT 7e: hold to charge a swing; right mouse to throw
            {
                if (s.UsingId == null) Melee.PlayerInput(p, w, i.Fire, i.Aim, mw);
                goto camera;
            }
            bool trigger = hd.Auto ? i.Fire : i.FireDown;
            if (trigger && aim.Loading > 0) StopLoading(p, w, aim);   // RT 6h: a tube or lever fires what's in; a revolver closes first
            if (trigger && s.UsingId != null) { s.UsingId = null; GameLog.Write("You stop to shoot."); }
            if (trigger && w != null && mw.HasValue && aim.ReloadUntil < 0f && Time.time >= aim.NextShot) PlayerFire(p, w, hd, aim, mw.Value);
            if (p.Gunslinging) TickOffhand(p, i, s, mw, dt, aim);

            camera:
            // ---- camera looks toward the mouse; further when aiming, further still with a long gun ----
            if (mw.HasValue)
            {
                var (px, py) = Ballistics.Pos(p);
                float reach = s.Aiming ? (CombatRules.Category(w?.Db) == "sniper" ? 0.75f : 0.5f) : 0.25f;
                s.Look = new Vector3((mw.Value.x - px) * reach, -(mw.Value.y - py) * reach, 0f);
            }
        }

        // ---- RT 7z-c: Gunslinger (ACTION_DESIGN "Perks": "Dual pistols, one per mouse button"; Yan: "do 1, build gunslinger for real").
        //      With the perk and a pistol in each hand: left button = the right-hand gun, right button = the left-hand gun, each on
        //      its own rate of fire, kick, magazine and jams. No steadied aim (the right button is a trigger now), but a dive steadies
        //      both; each gun spreads a quarter wider than alone. R reloads the right-hand gun, then the left. ----
        public const float GunslingerSpread = 1.25f;

        static void TickOffhand(Player p, Input i, State s, (float x, float y)? mw, float dt, Gunplay.Aim mainAim)
        {
            var off = p.OffhandWeapon;
            var aim2 = Gunplay.OffAimOf(p);
            var hd2 = Gunplay.For(off);
            Gunplay.Tick(hd2, aim2, s.Diving, s.Moving, SlowMo ? Time.unscaledDeltaTime : dt);
            if (aim2.ReloadUntil >= 0f && Time.time >= aim2.ReloadUntil)
            {
                aim2.ReloadUntil = -1f;
                if (off.IsJammed) off.ClearJam(); else off.Reload();
                CombatFx.Reload(p, off, -1f);
            }
            bool wants = i.Reload || s.ReloadOffhand;
            if (wants && mainAim.ReloadUntil < 0f && aim2.ReloadUntil < 0f && (off.IsJammed || off.CurrentMagazine < off.MagCapacity))
            {
                float t = off.IsJammed ? 0.8f : Gunplay.ReloadSeconds(off) * (CombatantUtil.HasPerk(p, "QuickHands") ? 0.6f : 1f) * (Body.ArmsHurt(p) > 0 ? 1.5f : 1f);
                aim2.ReloadUntil = Time.time + t; s.ReloadOffhand = false;
                CombatFx.Reload(p, off, t, off.IsJammed, off.MagCapacity - off.CurrentMagazine);
                GameLog.Write(off.IsJammed ? "Clearing the left gun..." : "Reloading the left gun...");
            }
            else if (i.Reload && mainAim.ReloadUntil >= 0f) s.ReloadOffhand = true;   // after the right-hand gun
            if (i.Fire2Down && mw.HasValue && aim2.ReloadUntil < 0f && Time.time >= aim2.NextShot) PlayerFire(p, off, hd2, aim2, mw.Value);
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
            float t = w.IsJammed ? 0.8f : Gunplay.ReloadSeconds(w) * speed;
            aim.ReloadUntil = now + t;
            CombatFx.Reload(p, w, t, w.IsJammed, w.MagCapacity - w.CurrentMagazine);   // rounds: the spent shells a break-action dumps
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
            aim.NextShot = Time.time + 60f / hd.Rpm * (SlowMo ? SlowScale : 1f);   // RT 7z-g: bullet time never slows a player's trigger (enemies stay slowed)
            if (CombatRules.IsMelee(w)) { Melee.PlayerAttack(p, w, at, Of(p).Aiming); return; }   // RT 7a: swing, or aim and throw
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
            float spread = Gunplay.Spread(hd, aim, Of(p).Moving, p.Sneaking);
            // RT batch 2: broken arms sway, a concussion blurs; RT batch 4: their near misses pin you. RT 7z-d: halved like the rest of
            // your spread (Gunplay.PlayerSpread) - left whole in RT 7z, a pin alone tripled a steady pistol's spread
            spread += (2.5f * Body.ArmsHurt(p) + (Body.Concussed(p) ? 2f : 0f)) * Gunplay.PlayerSpread;
            if (!Of(p).Diving && (p.StatusEffects.Suppressed || Time.time < Of(p).PinnedUntil)) spread += 2f * Gunplay.PlayerSpread;   // RT 8 (Yan): no pinning mid-dive
            if (p.Gunslinging) spread *= GunslingerSpread;                                  // RT 7z-c: a gun in each hand
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
        public const int SlowReach = 40;
        public const float DiveCost = 40f, DiveSpeed = 4.5f, DiveTime = 0.55f, ProneTime = 0.6f, SlowScale = 0.3f, FocusMax = 100f;
        const float DiveSpeedFloor = 1f;                              // moved slower than this (tiles/s): hit a wall, land now
        public static bool SlowMo;                                    // ponytail: one time scale for everyone - step 4 makes it per fight

        // ---- RT 7z, the dive's look (Yan: "maybe the character lowers himself to build momentum before jumping, today he kinda just
        //      turns sideways and there's no sense of momentum or height"). The same 0.55 s, in four beats: a crouch that loads the
        //      legs (squashed, leaning in, barely moving), the launch and the arc (stretched, rising to 0.45 tiles and down, turning
        //      flat; the shadow stays on the ground and shrinks), the belly landing (squashed flat, skidding), then the get-up at the
        //      end of the prone time. The distance covered is unchanged (DiveSpeedAt averages to 1). ----
        public const float DiveCrouch = 0.1f, DiveLanding = 0.08f, DiveHeight = 0.45f, GetUpTime = 0.2f;
        const float CrouchPace = 0.3f, SkidPace = 0.6f;
        static float AirPace => (DiveTime - CrouchPace * DiveCrouch - SkidPace * DiveLanding) / (DiveTime - DiveCrouch - DiveLanding);

        /// <summary>The dive's speed (x DiveSpeed) `t` seconds into it.</summary>
        public static float DiveSpeedAt(float t) => t < DiveCrouch ? CrouchPace : t < DiveTime - DiveLanding ? AirPace : SkidPace;

        public struct Pose { public bool Active; public float Lift, Tilt, SquashX, SquashY, Shadow; }

        /// <summary>How a player's body is drawn right now: height off the ground (tiles), turn (degrees; + = to the left), squash
        /// along and across the body, and the ground shadow's size. RT 7z-d (Yan: "we need an animation for diving upward and
        /// downward as well, right now the character dives and always turns right or left"): the body leans into the dive's own
        /// direction - its sideways share turns it (head first), its up/down share lays it out along the screen, foreshortened
        /// (a body flat on its belly, seen from above and head-on, is short); diagonals mix the two.</summary>
        public static Pose PoseOf(Player p)
        {
            var s = Of(p); float now = Time.time;
            var o = new Pose { SquashX = 1f, SquashY = 1f, Shadow = 1f };
            float lean = 0f;                                                 // 0 upright .. 90 flat out
            if (s.Diving)
            {
                o.Active = true;
                float t = Mathf.Clamp(DiveTime - (s.DiveUntil - now), 0f, DiveTime), air = DiveTime - DiveCrouch - DiveLanding;
                if (t < DiveCrouch)                                          // loading the legs
                {
                    float k = t / DiveCrouch;
                    lean = 20f * k; o.SquashY = 1f - 0.28f * k; o.SquashX = 1f + 0.14f * k;
                }
                else if (t < DiveTime - DiveLanding)                         // launch and arc
                {
                    float u = (t - DiveCrouch) / air, pop = Mathf.Max(0f, 1f - u / 0.35f);
                    o.Lift = DiveHeight * Mathf.Sin(Mathf.PI * u);
                    lean = 20f + 70f * Mathf.Min(1f, u * 1.8f);
                    o.SquashY = 1f + 0.14f * pop; o.SquashX = 1f - 0.1f * pop;
                }
                else                                                         // belly landing
                {
                    float k = (t - (DiveTime - DiveLanding)) / DiveLanding;
                    lean = 90f; o.SquashY = 0.8f + 0.2f * k; o.SquashX = 1.12f - 0.12f * k;
                }
                o.Shadow = 1f - 0.45f * o.Lift / DiveHeight;
            }
            else if (now < s.ProneUntil)                                     // sprawled, then pushing back up
            {
                o.Active = true;
                lean = 90f * Mathf.Clamp01((s.ProneUntil - now) / GetUpTime);
            }
            float l = Mathf.Sqrt(s.DiveX * s.DiveX + s.DiveY * s.DiveY), v = l > 0.01f ? Mathf.Abs(s.DiveY) / l : 0f;
            // lying flat, the head points where the dive goes: right -90, up 0, down 180 (degrees, + = counter-clockwise)
            float full = l > 0.01f ? -Mathf.Atan2(s.DiveX, -s.DiveY) * Mathf.Rad2Deg : (s.DiveRight ? -90f : 90f);
            float ramp = Mathf.Abs(full) <= 90f ? lean / 90f : Mathf.Clamp01((lean - 20f) / 70f);   // a dive toward you turns in the air, not in the crouch
            o.Tilt = full * ramp;
            // up or down the screen (Yan, RT 7z-e: "dodging up doesn't [look great] ... the character squishes himself and un-squishes
            // after, what if you try playing a trick on the camera by making the character a little bigger during the dodge so it
            // seems he's closer to the camera"): no foreshortening - he grows with his height instead; side dives are unchanged
            float grow = 1f + DiveGrow * (o.Lift / DiveHeight) * v;
            o.SquashX *= grow; o.SquashY *= grow;
            return o;
        }
        public const float DiveGrow = 0.3f;
        public static bool Low(ICombatant u) => On && u is Player p && (Of(p).Diving || Time.time < Of(p).ProneUntil);   // airborne or sprawled: a low target

        public static void AddFocus(Player p, float f)
        {
            if (!On || p == null) return;
            var s = Of(p); s.Focus = Mathf.Min(FocusMax, s.Focus + f);
        }

        static bool TryDive(Player p, float ix, float iy, (float x, float y)? mouse)
        {
            var s = Of(p);
            if (s.Focus < DiveCost) { GameLog.Write($"Not enough Focus to dive ({Mathf.FloorToInt(s.Focus)}/{DiveCost:0})."); return false; }
            float dx = ix, dy = iy;
            if (dx == 0f && dy == 0f && mouse.HasValue) { var (px, py) = Ballistics.Pos(p); dx = mouse.Value.x - px; dy = mouse.Value.y - py; }
            float l = Mathf.Sqrt(dx * dx + dy * dy);
            if (l < 0.01f) return false;
            s.DiveX = dx / l; s.DiveY = dy / l;
            s.Focus -= DiveCost;
            s.DiveUntil = Time.time + DiveTime;
            // co-op step 4 (ACTION_DESIGN: "bullet time slows everyone within ~40 tiles / in the same fight"): Unity has one clock, so
            // it slows only when every living player is within 40 tiles of the diver; a friend far off in their own fight keeps full speed
            if (Player.All.All(o => o == p || !o.IsAlive() || CombatantUtil.Distance(o, p) <= SlowReach)) SetSlow(true);
            Sfx.Play("swing", TacticalGridView.GridToWorld(p.X, p.Y), 1f, 0.05f, 0.6f);
            return true;
        }

        static void Land(Player p)
        {
            var s = Of(p);
            s.DiveUntil = -1f;
            Sfx.Play("hit_dirt", TacticalGridView.GridToWorld(p.X, p.Y), 0.8f, 0.1f, 0.7f);   // RT 7z: the body hits the ground
            s.ProneUntil = Time.time + (CombatantUtil.HasPerk(p, "Tumbler") ? 0.35f : ProneTime);
            if (!Player.All.Any(o => Of(o).Diving)) SetSlow(false);   // still slow while a friend is mid-dive
            Sfx.Play("hit_dirt", TacticalGridView.GridToWorld(p.X, p.Y), 0.8f);
        }

        public static void SetSlow(bool on) { SlowMo = on; if (!Paused) Time.timeScale = on ? SlowScale : 1f; }

        // ---- using an item takes time (RT batch 2): you move at half pace, firing cancels it ----
        public const float Crawl = 0.6f;
        public static bool Applying;                                  // true while the finished use runs Player.UseConsumable for real

        public static float UseSeconds(GameData.ConsumableDef d) =>
            d.CureFracture ? 4f : d.CureBleeding ? 3f : d.HealingPermanent > 0 && d.HungerRestore == null ? 4f : d.HealingTemp > 0 ? 1.5f : 2.5f;

        public static void BeginUse(Player p, GameData.ConsumableDef d)
        {
            var s = Of(p);
            if (s.UsingId != null) { GameLog.Write("Already busy."); return; }
            if (Body.Down(p) && !(d.HealingTemp > 0)) { GameLog.Write("You're down - only a stim gets you up."); return; }
            if (d.Throwable != null || d.RepairKit) { Applying = true; try { p.UseConsumable(d.Id); } finally { Applying = false; } return; }
            s.UsingId = d.Id; s.UseFrom = Time.time; s.UseDone = Time.time + UseSeconds(d);
            GameLog.Write($"Using {d.Name}...");
        }

        static void TickUse(Player p)
        {
            var s = Of(p);
            if (s.UsingId == null || Time.time < s.UseDone) return;
            string id = s.UsingId; s.UsingId = null;
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
        public static void WorldTick(Player local)
        {
            var p = local;
            float now = Time.time;
            var grid = GameServices.Grid;
            if (grid == null) return;
            Death.Tick();                                                          // co-op: the dead penalty, once
            if (TestingGround.Here) TestingGround.Tick();                         // dummies and damage numbers
            Survival.Tick(local);                                                  // Survival: the waves
            Melee.Tick(Time.deltaTime);                                            // RT 7a: thrown weapons fly, stuck blades come out
            Boss.TickAll();                                                        // Boss 1: the hit-stop, a looted boss's smoke
            Gunplay.Fly(Time.deltaTime);                                           // RT 8: bullets fly
            foreach (var e in EncounterState.Enemies.ToList())
            {
                if (!e.IsAlive()) { NextTurn.Remove(e); continue; }
                if (Boss.Of(e) is Boss.Fight bf) { Boss.Tick(bf, TargetOf(e, local)); continue; }   // Boss 1: a boss runs his own moves
                bool fighting = e.IsAggroed && EncounterState.IsActive(e) || Companion.Is(e) && EncounterState.InCombat;
                if (!fighting) { NextTurn.Remove(e); continue; }
                p = Companion.Is(e) ? local : TargetOf(e, local);                // co-op: each enemy fights the player nearest it
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
                if (!p.IsAlive()) TurnLoop.CheckCombatEnd();
                else Perks.TryAdrenaline(p);
                if (TurnLoop.AllDead) return;
            }
            if (!EncounterState.InCombat) { _round = 0f; return; }
            if ((_round += Time.deltaTime) < RoundSeconds) return;
            _round = 0f;
            EnemyAI.BeginEnemyRound(local);                                 // ponytail: morale and radio still read the local player
            foreach (var pl in Player.All.ToList())                         // every player's upkeep: bleeding, effects, anomalies
            {
                pl.EndTurn(); pl.StartTurn();
                AnomalyEngine.ProcessTurnStart(pl);
                if (pl.IsAlive()) Contact.EndRound(pl);
            }
            TurnLoop.CheckCombatEnd();
            if (EncounterState.InCombat) TurnLoop.RadioForHelp();
        }
    }
}
