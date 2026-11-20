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
        /// <summary>Is this body still sliding to catch up with a tile jump (Glide)? That is what 'walking' looks like on screen, for NPCs and for co-op mirrors alike.</summary>
        public static bool Sliding(ICombatant u) => u != null && Slide.TryGetValue(u, out var sl) && (sl.x != 0f || sl.y != 0f);

        // 2026-11-12: a pounce carries the body across by the animation's own travel curve (MutantAttack), so Glide leaves a held body alone and only follows where its tile is
        static readonly HashSet<ICombatant> Held = new HashSet<ICombatant>();
        public static void HoldSlide(ICombatant u) { if (u != null) Held.Add(u); }
        public static void SetSlide(ICombatant u, float x, float y) { if (u != null) { Held.Add(u); Slide[u] = (x, y); } }
        public static void ReleaseSlide(ICombatant u) { if (u != null) { Held.Remove(u); Slide[u] = (0f, 0f); Was.Remove(u); } }
        /// <summary>Knocked down: sprawled for `seconds` like after a dive (can't move, a low target).</summary>
        public static void Knockdown(Player p, float seconds) { if (p == null) return; var s = Of(p); s.DiveUntil = -1f; s.ProneUntil = Mathf.Max(s.ProneUntil, Time.time + seconds); }

        /// <summary>A knock-back: slides the body (dx, dy) tiles over `seconds`, fast at first and easing out, stopped by walls and friends like walking (MoveBy). Yan, 2026-11-16: the eel "teleports the player around instead of knocking him back".</summary>
        public static void Shove(Player p, float dx, float dy, float seconds = 0.35f)
        {
            if (p == null || (dx == 0f && dy == 0f)) return;
            var s = Of(p); s.ShoveX = dx; s.ShoveY = dy; s.ShoveFrom = Time.time; s.ShoveLen = Mathf.Max(0.05f, seconds);
        }
        static void ShoveTick(Player p, State s)
        {
            if (s.ShoveFrom < 0f) return;
            float u0 = Mathf.Clamp01((Time.time - Time.deltaTime - s.ShoveFrom) / s.ShoveLen), u1 = Mathf.Clamp01((Time.time - s.ShoveFrom) / s.ShoveLen);
            float e0 = 1f - (1f - u0) * (1f - u0), e1 = 1f - (1f - u1) * (1f - u1), d = e1 - e0;      // ease-out: the share of the slide done this frame
            if (d > 0f) MoveBy(p, s.ShoveX * d, s.ShoveY * d);
            if (u1 >= 1f) s.ShoveFrom = -1f;
        }

        public static void Glide(float dt)
        {
            foreach (var e in EncounterState.Enemies)
            {
                if (Held.Contains(e)) { var (hx, hy) = Slide.TryGetValue(e, out var hs) ? hs : (0f, 0f); var (hox, hoy) = Offset(e); Was[e] = (e.X + 0.5f + hox - hx, e.Y + 0.5f + hoy - hy); continue; }
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
            public bool Slow;                                         // RT 9: this player has bullet time on (its own key, drains Focus)
            public float Stamina = StaminaMax, StaminaRest, Focus, DiveUntil = -1f, ProneUntil = -1f, PinnedUntil = -1f, DiveX, DiveY;
            public float VaultFrom = -1f, VaultLen, VaultX, VaultY, VaultDist, UseDone, UseFrom;
            public float ScreenReach = Vision.MaxRadius;              // 2026-11-19 (Yan: "enemies are still shooting a lot at me from veeeeery far away, I had to zoom all the way out"): how far this player's SCREEN shows (half the view's width plus how far the camera looks ahead); nobody fair-shoots from past it
            public float LookReach = Vision.MaxRadius;                // how far this player sees where they look (Vision, for fair shots)
            public int FireMode, BurstLeft;                           // 0 auto, 1 3-round burst, 2 semi (Gunplay.HasModes guns only); the rounds of the burst still to fire
            public bool Aiming;
            public float ShoveX, ShoveY, ShoveFrom = -1f, ShoveLen;   // a knock-back in progress: the total offset, when it began, how long it takes (Rt.Shove)
            public int Moving;                                        // 0 still, 1 walking, 2 sprinting
            public Vector3 Look;                                      // camera look-ahead (world), eased by TacticalGridView
            public float PullEdge = 30f;                              // how far from the player the view's edge may be pulled toward the cursor (tiles): the gun's reach, scopes stretch it (PullEdgeOf); TacticalGridView clamps the pull to it
            public string UsingId;
            public float Reviving;                                    // seconds E has been held over a downed friend
            public (float x, float y)? AimAt;                         // RT 7z: where this player's crosshair is (tile space) - friends' guns point there
            public bool ReloadOffhand;                                // RT 7z-c: R was pressed while the right-hand gun reloaded: the left one is next
            public bool Dead;                                         // co-op: the dead penalty has been taken (Death.Tick)
            public float ThrowReadyAt;                                // a grenade can leave the hand again at this time
            public float ParryAt = -99f, ParryReady;                  // Boss 1: when the parry key went down, when it can again
            public float GrabAt = -99f;                               // Boss 3: when the parry key last went down, weapon or not (the eel's bite is parried bare-handed)
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
            public bool Mode;                                         // the fire-mode key pressed (X): auto / burst / semi on the modern guns
            public bool Sneak, Sprint, Aim, Fire, FireDown, Jump, Reload, Use;   // held / held / held / held / pressed / pressed / pressed / held (E)
            public bool Fire2Down;                                    // RT 7z-c: right mouse pressed - a Gunslinger's left-hand gun
            public bool Parry;                                        // Boss 1: the parry key pressed (F by default, Keybinds)
            public bool Throw;                                        // 2026-11-18: the throw key pressed (T by default, Keybinds): take out a grenade / the next kind / put it away
            public bool BulletTime;                                   // RT 9: the bullet-time key pressed (V by default, Keybinds)
            public (float x, float y)? Mouse;                         // the crosshair, tile space
        }
        public static Input Read(Keyboard k, Mouse m) => new Input
        {
            X = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0),
            Y = (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0) - (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0),
            Sneak = k.leftCtrlKey.isPressed, Sprint = k.leftShiftKey.isPressed, Jump = k.spaceKey.wasPressedThisFrame, Reload = k.rKey.wasPressedThisFrame, Use = k.eKey.isPressed,
            Aim = m != null && m.rightButton.isPressed, Fire = m != null && m.leftButton.isPressed, FireDown = m != null && m.leftButton.wasPressedThisFrame,
            Fire2Down = m != null && m.rightButton.wasPressedThisFrame,
            Parry = Keybinds.Pressed(k, Keybinds.Parry), Throw = Keybinds.Pressed(k, Keybinds.Throw),
            BulletTime = Keybinds.Pressed(k, Keybinds.Slow),
            Mode = k.xKey.wasPressedThisFrame,
            Mouse = MouseTile(m),
        };
        static float _round;

        /// <summary>The last Input each joined player sent (the network layer fills it; the harness too). Ticked with the local one.</summary>
        public static readonly Dictionary<Player, Input> Remote = new Dictionary<Player, Input>();

        /// <summary>Co-op: who an enemy fights - the nearest living player it has a line of sight to, else the nearest living one.
        /// `fallback` (the local player) when nobody else qualifies.</summary>
        public static Player TargetOf(Mutant e, Player fallback)
        {
            if (Gaunt.Marked(e) is Player marked) return marked;                  // a mutant the Gaunt drove at someone hunts that player
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
        /// <summary>Co-op (Yan, 2026-11-17: "enemy squads divide attention between coop players, 50% to the closest player and the rest divides between the rest of players"): who an enemy in a fight is on right now.
        /// Each enemy re-draws every 4-8 s (and when its target dies or is out of sight range): half the draws go to the player `TargetOf` names (the closest, one it can see first), the other half is shared evenly
        /// by the other living players within sight range of the map (Vision.MaxRadius); alone, or with nobody else near, it is always the closest. A mutant the Gaunt drove keeps its mark.</summary>
        public static Player AttentionOf(Mutant e, Player fallback)
        {
            var closest = TargetOf(e, fallback);
            if (Player.All.Count < 2 || Gaunt.Marked(e) != null) return closest;
            float now = Time.time;
            if (Attention.TryGetValue(e, out var a) && now < a.until && a.who != null && a.who.IsAlive() && CombatantUtil.Distance(e, a.who) <= Vision.MaxRadius + 10) return a.who;
            var others = new List<Player>();
            foreach (var p in Player.All) if (p != closest && p.IsAlive() && CombatantUtil.Distance(e, p) <= Vision.MaxRadius) others.Add(p);
            var pick = others.Count == 0 || Random.value < 0.5f ? closest : others[Random.Range(0, others.Count)];
            Attention[e] = (pick, now + Random.Range(4f, 8f));
            return pick;
        }
        static readonly Dictionary<Mutant, (Player who, float until)> Attention = new Dictionary<Mutant, (Player, float)>();

        static readonly Dictionary<ICombatant, float> NextTurn = new Dictionary<ICombatant, float>();

        public static void Reset() { RtAi.Reset(); Paused = false; States.Clear(); Remote.Clear(); SlowMo = false; Body.Reset(); Off.Clear(); Was.Clear(); Slide.Clear(); NextTurn.Clear(); Attention.Clear(); Explosions.Reset(); NpcFights.Reset(); Gunplay.Reset(); Melee.Reset(); Boss.Reset(); BrainBoss.Reset(); _round = 0f; }

        /// <summary>Move a unit by (dx, dy) tiles, sliding along walls. Crossing into the next tile is a real step for the
        /// rules (Player.Move: doors, anomalies, exits), so the tile underneath always matches where the body is.</summary>
        public static void MoveBy(Player p, float dx, float dy)
        {
            var (ox, oy) = Offset(p);
            float x = p.X + 0.5f + ox, y = p.Y + 0.5f + oy;
            if (dx != 0f && Clear(p, x + dx, y) && !Crowds(p, x, y, x + dx, y)) x += dx; else if (dx != 0f) LabLocks.Nag(Mathf.FloorToInt(x + dx + (dx > 0f ? Radius : -Radius)), Mathf.FloorToInt(y));   // Boss 2: bumping a locked door says why
            if (dy != 0f && Clear(p, x, y + dy) && !Crowds(p, x, y, x, y + dy)) y += dy; else if (dy != 0f) LabLocks.Nag(Mathf.FloorToInt(x), Mathf.FloorToInt(y + dy + (dy > 0f ? Radius : -Radius)));
            int tx = Mathf.FloorToInt(x), ty = Mathf.FloorToInt(y);
            if (tx != p.X || ty != p.Y)
            {
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
            var s = Of(p); ShoveTick(p, s);
            if (i.Parry) Parry.Press(p);                                                       // Boss 1
            TickThrow(p, s, ref i);                                                            // grenades, flashbangs and smokes: T takes one out, left click throws it

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
            if (Emplacement.IsMounted(p)) { i.X = i.Y = 0f; i.Jump = false; i.Sprint = false; }   // on the Barrier's machine gun: you stay put
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
            speed *= p.MoveScale;                                       // heavy armor -10% (Yan); artifacts and armor mods nudge it
            if (Body.LegHurt(p)) speed *= 0.6f;                       // RT batch 2: a broken or fractured leg limps
            if (s.UsingId != null) speed *= 0.5f;                     // patching yourself up
            if (down) speed = Crawl;
            if (p.StatusEffects.Prone) speed = 0f;
            if (Time.time < s.ProneUntil) speed = 0f;                 // RT batch 3: sprawled after a dive
            float l = Mathf.Sqrt(ix * ix + iy * iy);
            s.Moving = l > 0f && speed > 0f ? (sprint ? 2 : 1) : 0;
            if (i.BulletTime && !down) ToggleSlow(p);
            if (s.Slow && (s.Focus -= SlowDrain * Time.unscaledDeltaTime) <= 0f) { s.Focus = 0f; s.Slow = false; UpdateSlow(); }
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
            Gunplay.Tick(hd, aim, s.Aiming || s.Diving, s.Moving, (SlowMo ? Time.unscaledDeltaTime : dt) * Optics.SteadyMul(w));   // in bullet time your aim keeps real-time speed
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
            if (i.Mode) CycleFireMode(p, w, s);
            int fmode = Gunplay.HasModes(w) ? s.FireMode : 0; bool trigger;
            if (fmode == 1) { if (w.CurrentMagazine <= 0 || aim.ReloadUntil >= 0f) s.BurstLeft = 0; else if (i.FireDown && s.BurstLeft <= 0) s.BurstLeft = Gunplay.BurstRounds; trigger = s.BurstLeft > 0; }   // a burst runs to its end once pulled
            else { s.BurstLeft = 0; trigger = fmode == 2 ? i.FireDown : hd.Auto ? i.Fire : i.FireDown; }
            if (trigger && aim.Loading > 0) StopLoading(p, w, aim);   // RT 6h: a tube or lever fires what's in; a revolver closes first
            if (trigger && s.UsingId != null) { s.UsingId = null; GameLog.Write("You stop to shoot."); }
            if (trigger && w != null && mw.HasValue && aim.ReloadUntil < 0f && Time.time >= aim.NextShot)
            {
                PlayerFire(p, w, hd, aim, mw.Value);
                if (fmode == 1 && s.BurstLeft > 0 && --s.BurstLeft == 0) aim.NextShot += Gunplay.BurstGap * (SlowMo ? SlowScale : 1f);   // the pause between bursts
            }
            if (p.Gunslinging) TickOffhand(p, i, s, mw, dt, aim);

            camera:
            // ---- camera looks toward the mouse; further when aiming, further still with a long gun ----
            if (mw.HasValue)
            {
                var (px, py) = Ballistics.Pos(p);
                float reach = s.Aiming ? Optics.Reach(w, CombatRules.Category(w?.Db) == "sniper" ? 0.75f : 0.5f) : 0.25f;   // a scope leans the camera further (Optics.cs)
                s.Look = new Vector3((mw.Value.x - px) * reach, -(mw.Value.y - py) * reach, 0f);
                s.PullEdge = PullEdgeOf(w);
            }
        }

        // ---- thrown grenades (2026-11-18, Yan: "let the player throw grenades, flashbangs and smokes too") ----
        public const float ThrowRange = 14f, ThrowCooldown = 0.8f;
        static readonly string[] Throwables = { "fragGrenade", "flashbang", "smokeGrenade" };
        static int Have(Player p, string id) => p.Inventory.Items.Where(it => it.Id == id && it.Type == "consumable").Sum(it => it.Qty);

        /// <summary>Where a grenade thrown at the cursor lands: along the line from the player, at most ThrowRange tiles, stopping short of a wall (the preview circle in TacticalGridView uses the same).</summary>
        public static (int x, int y) ThrowLanding(Player p, (float x, float y) mouse)
        {
            var (px, py) = Ballistics.Pos(p); float dx = mouse.x - px, dy = mouse.y - py, len = Mathf.Sqrt(dx * dx + dy * dy);
            if (len < 0.5f) { dx = 1f; dy = 0f; len = 0.5f; }
            float reach = Mathf.Min(len, ThrowRange), ux = dx / len, uy = dy / len, at = 0f;
            for (float t = 0.25f; t <= reach; t += 0.25f)                                       // it stops short of a wall
            {
                int tx = Mathf.FloorToInt(px + ux * t), ty = Mathf.FloorToInt(py + uy * t);
                if (TerrainMap.BlocksSight(tx, ty)) break;
                at = t;
            }
            return (Mathf.Clamp(Mathf.FloorToInt(px + ux * at), 0, TerrainMap.Width - 1), Mathf.Clamp(Mathf.FloorToInt(py + uy * at), 0, TerrainMap.Height - 1));
        }

        /// <summary>T: take out the first grenade kind you carry (frag, flashbang, smoke), the next kind on a second press, nothing after the last. While one is out the left button throws it at the cursor (at most ThrowRange tiles,
        /// stopping short of a wall) instead of firing; the right button or a dive puts it away. The throw itself is Explosions.Throw: it flies, then the fuse. Co-op: per player, from the Input packet.</summary>
        static void TickThrow(Player p, State s, ref Input i)
        {
            if (i.Throw)
            {
                int at = p.ArmedThrowable == null ? -1 : System.Array.IndexOf(Throwables, p.ArmedThrowable), next = -1;
                for (int k = at + 1; k < Throwables.Length; k++) if (Have(p, Throwables[k]) > 0) { next = k; break; }
                if (next < 0) { if (p.ArmedThrowable != null) GameLog.Write("You put the grenade away."); else GameLog.Write("You have nothing to throw."); p.ArmedThrowable = null; }
                else { p.ArmedThrowable = Throwables[next]; GameLog.Write($"{GameData.ConsumableDatabase.All[p.ArmedThrowable].Name} ready - left click to throw, right click to put it away, T for the next kind."); }
            }
            if (p.ArmedThrowable == null) return;
            if (Body.Down(p) || s.Diving || Have(p, p.ArmedThrowable) <= 0) { p.ArmedThrowable = null; return; }
            if (i.Fire2Down) { p.ArmedThrowable = null; GameLog.Write("You put the grenade away."); i.Fire2Down = false; i.Aim = false; return; }
            bool go = i.FireDown && i.Mouse.HasValue && Time.time >= s.ThrowReadyAt && s.UsingId == null;
            i.FireDown = false; i.Fire = false;                                                // no shooting with a grenade in the hand
            if (!go) return;
            var def = GameData.ConsumableDatabase.All[p.ArmedThrowable];
            var (lx, ly) = ThrowLanding(p, i.Mouse.Value);
            if (!p.RemoveItem(def.Id, 1)) { p.ArmedThrowable = null; return; }
            s.ThrowReadyAt = Time.time + ThrowCooldown;
            Explosions.Throw(p, def, lx, ly);
            if (Have(p, def.Id) <= 0) p.ArmedThrowable = null;
        }

        /// <summary>2026-11-17 (Yan: pull the camera toward the cursor like Zero Sievert, the pull based on the gun's max range so nobody tries to shoot what is out of range): how far from the player the edge of the view may reach
        /// toward the cursor, in tiles = the gun's reach (a scope stretches it, Optics.PullMul), never past what the player can see (Vision.MaxRadius). Melee and fists: a short pull.</summary>
        public static float PullEdgeOf(Weapon w)
        {
            if (w?.Db == null || CombatRules.IsMelee(w)) return Vision.MaxRadius;   // 2026-11-18: was 12, which with a view 12 tiles wide to each side cancelled the pull sideways with a melee weapon (Yan: it snapped back to the centre); a blade has no range to limit the view by
            return System.Math.Min(Gunplay.For(w).Reach * Optics.PullMul(w), Vision.MaxRadius);
        }

        /// <summary>X: the next fire mode (auto, burst, semi) on a gun with a selector; the old designs say they only have the one.</summary>
        static void CycleFireMode(Player p, Weapon w, State s)
        {
            if (w == null || CombatRules.IsMelee(w)) return;
            if (!Gunplay.HasModes(w)) { GameLog.Write($"{w.Name} only fires {(Gunplay.For(w).Auto ? "full auto" : "single shots")}."); return; }
            s.FireMode = (s.FireMode + 1) % 3; s.BurstLeft = 0; Sfx.Play("ui_click");
            GameLog.Write($"{w.Name}: {(s.FireMode == 0 ? "full auto" : s.FireMode == 1 ? $"{Gunplay.BurstRounds}-round burst" : "single shots")}.");
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
            aim.NextShot = Time.time + 60f / (hd.Rpm * Gunplay.HipRate(w, Of(p).Aiming)) * (SlowMo ? SlowScale : 1f);   // RT 7z-g: bullet time never slows a player's trigger (enemies stay slowed)
            if (CombatRules.IsMelee(w)) { Melee.PlayerAttack(p, w, at, Of(p).Aiming); return; }   // RT 7a: swing, or aim and throw
            if (w.CurrentMagazine <= 0) { Sfx.Play("dryfire", TacticalGridView.GridToWorld(p.X, p.Y)); return; }
            w.CurrentMagazine--;
            var (px, py) = Ballistics.Pos(p);
            float dx = at.x - px, dy = at.y - py, len = Mathf.Sqrt(dx * dx + dy * dy);
            if (len < 0.05f) return;
            var over = UnitUnder(at.x, at.y); bool assisted = false;
            if (over == null) { over = UnitUnder(at.x, at.y, AssistFor(w)); assisted = over != null; }   // not on a sprite, but in his direction: that is him
            bool atEel = EelSump.BodyUnder(at.x, at.y, out var eelU, out float eelX, out float eelY);   // Boss 3: the crosshair is over some part of the eel's body: aim at that part's spot on the ground
            if (atEel) over = eelU;
            float h = atEel ? 0.45f : assisted ? 0.55f : Ballistics.AimHeight(over, at.y);   // (an assisted aim takes the chest)
            int mx = Mathf.FloorToInt(at.x), my = Mathf.FloorToInt(at.y);
            if (over == null && TerrainMap.CoverAcAt(mx, my) > 0 && !TerrainMap.BlocksSight(mx, my)) h = 0.3f;   // a barrel, sandbags, a desk: shoot the thing itself
            // the crosshair is over a body: aim at that body's footprint, at the height of the pixel under it
            if (over != null) { var (ux, uy) = atEel ? (eelX, eelY) : Ballistics.Pos(over); dx = ux - px; dy = uy - py; len = Mathf.Sqrt(dx * dx + dy * dy); }
            if (len < 0.05f) return;
            // Yan, 2026-11-13 (eel fight): "the aim keeps snapping toward its base in the water". The round is aimed at the part's footing on the ground (that is what the hit test reads), but the part is drawn Z tiles
            // up, so the gun and the round went to the water. The round now RISES to the part's height over that distance (Up), and the gun stays on the crosshair (FxEvent.AtEel).
            Gunplay.AimUp = atEel ? (EelSump.UnderZ - 0.45f * Ballistics.BodyTiles) / len : 0f;
            bool ambush = over is Mutant u && !u.IsAggroed;
            if (!EncounterState.InCombat) Contact.ShotFrom(p);
            float spread = Gunplay.Spread(hd, aim, Of(p).Moving, p.Sneaking);
            spread *= Optics.SpreadMul(w, aim.Steady);                                       // 2026-11-16: a sight or a laser on this gun (Optics.cs), per player
            // RT batch 2: broken arms sway, a concussion blurs; RT batch 4: their near misses pin you. RT 7z-d: halved like the rest of
            // your spread (Gunplay.PlayerSpread) - left whole in RT 7z, a pin alone tripled a steady pistol's spread
            spread += (2.5f * Body.ArmsHurt(p) + (Body.Concussed(p) ? 2f : 0f)) * Gunplay.PlayerSpread;
            if (!Of(p).Diving && (p.StatusEffects.Suppressed || Time.time < Of(p).PinnedUntil)) spread += 2f * Gunplay.PlayerSpread;   // RT 8 (Yan): no pinning mid-dive
            if (p.Gunslinging) spread *= GunslingerSpread;                                  // RT 7z-c: a gun in each hand
            bool tower = Emplacement.Elevated(p); if (tower) spread *= Emplacement.SteadyMul;   // 2026-11-19 (Yan: "the height system makes the shots miss too much"): a gun on a tower's mount does not sway like one in your hands
            Gunplay.AimAtEel = atEel; try { Gunplay.Shoot(p, w, dx / len, dy / len, h, spread, ambush); } finally { Gunplay.AimAtEel = false; Gunplay.AimUp = 0f; }
            if (!tower) aim.Bloom = System.Math.Min(Gunplay.PlayerMaxBloom, aim.Bloom + hd.Kick);   // (a mounted gun does not bloom)   // RT 6d: kick tops out (task #36: lower for the player)
            if (EncounterState.Enemies.Any(e => e.IsAlive() && e.IsAggroed)) EncounterState.InCombat = true;
            TurnLoop.CheckCombatEnd();
        }

        // ---- RT batch 3: the dive and bullet time (Yan: "you jump and get the slowmo but when it ends you're prone for a tiny
        //      moment, immobilizing you and making it risky"). Space dives the way you're moving (or toward the mouse): airborne
        //      ~0.55 s of game time, time runs at 30% meanwhile, you can aim and fire (the aim steadies at real-time speed).
        //      Landing: prone 0.6 s (Tumbler: 0.35) - no moving, but you can still shoot. Costs 40 Focus, so no chaining.
        //      Focus fills from kills (+25), hits (+4) and enemy rounds that just miss you (+6, Cold Blood x2). ----
        //      RT 9 (Yan, 2026-11-03: "unbinding bullet time from the dodge, having it be a separate key ... and use the dodge for
        //      mechanics like those, having it be invulnerable"): the dive costs stamina (DiveStamina) and nothing hits you while
        //      it lasts (Untouchable: rounds pass, blades miss, blasts skip you); the landing is still prone and open. Bullet time is
        //      its own key (V, Keybinds.Slow): on/off, drains Focus (SlowDrain a real second), stops when it runs dry.
        public const int SlowReach = 40;
        public const float DiveStamina = 30f, SlowDrain = 25f, SlowMin = 10f;
        public const float DiveSpeed = 4.5f, DiveTime = 0.55f, ProneTime = 0.6f, SlowScale = 0.3f, FocusMax = 100f;
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
            if (s.Stamina < DiveStamina) { GameLog.Write("Too winded to dive."); return false; }
            float dx = ix, dy = iy;
            if (dx == 0f && dy == 0f && mouse.HasValue) { var (px, py) = Ballistics.Pos(p); dx = mouse.Value.x - px; dy = mouse.Value.y - py; }
            float l = Mathf.Sqrt(dx * dx + dy * dy);
            if (l < 0.01f) return false;
            s.DiveX = dx / l; s.DiveY = dy / l;
            s.Stamina -= DiveStamina; s.StaminaRest = 0.8f;
            s.DiveUntil = Time.time + DiveTime;
            Sfx.Play("swing", TacticalGridView.GridToWorld(p.X, p.Y), 1f, 0.05f, 0.6f);
            return true;
        }

        static void Land(Player p)
        {
            var s = Of(p);
            s.DiveUntil = -1f;
            Sfx.Play("hit_dirt", TacticalGridView.GridToWorld(p.X, p.Y), 0.8f, 0.1f, 0.7f);   // RT 7z: the body hits the ground
            s.ProneUntil = Time.time + (CombatantUtil.HasPerk(p, "Tumbler") ? 0.35f : ProneTime);
            Sfx.Play("hit_dirt", TacticalGridView.GridToWorld(p.X, p.Y), 0.8f);
        }

        /// <summary>RT 9: nothing touches a diving player (rounds, blades, blasts); the prone landing is fair game.</summary>
        public static bool Untouchable(ICombatant u) => On && u is Player p && Of(p).Diving;

        static void ToggleSlow(Player p)
        {
            var s = Of(p);
            if (!s.Slow && s.Focus < SlowMin) { GameLog.Write($"Not enough Focus for bullet time ({Mathf.FloorToInt(s.Focus)}/{SlowMin:0})."); return; }
            s.Slow = !s.Slow;
            if (s.Slow) Sfx.Play("swing", TacticalGridView.GridToWorld(p.X, p.Y), 0.8f, 0.05f, 0.5f);
            UpdateSlow();
        }

        // co-op step 4 (ACTION_DESIGN: "bullet time slows everyone within ~40 tiles / in the same fight"): Unity has one clock, so a
        // player's bullet time counts only when every living player is within 40 tiles of them; a friend far off keeps full speed
        static void UpdateSlow() =>
            SetSlow(Player.All.Any(p => p.IsAlive() && Of(p).Slow && Player.All.All(o => o == p || !o.IsAlive() || CombatantUtil.Distance(o, p) <= SlowReach)));

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
        /// <summary>2026-11-19 (Yan: "the crosshair needs to be in the general direction of the enemy for it to count as aiming at him, putting the mouse directly over the enemy sprite is very hard, should matter when aiming through scopes"):
        /// how far (tiles) from a body's middle the crosshair still counts as aiming at it when it is not over the sprite itself: wider through a scope, where the picture is magnified and the cursor travels far.</summary>
        public const float AssistRadius = 1.0f, ScopeAssistRadius = 1.8f;
        public static float AssistFor(Weapon w) => Optics.Scoped(w) ? ScopeAssistRadius : AssistRadius;

        /// <summary>The body the crosshair is on - or, with `assist` > 0 and nothing under it, the nearest living, visible body whose middle is within `assist` tiles (a big body: that much plus a third of its height).</summary>
        public static ICombatant UnitUnder(float x, float y, float assist)
        {
            var hit = UnitUnder(x, y); if (hit != null || assist <= 0f) return hit;
            ICombatant best = null; float bd = float.MaxValue;
            foreach (var u in Gunplay.Bodies())
            {
                if (u is Player || !u.IsAlive() || EelBoss.Is(u) || !Vision.Shows(u)) continue;
                var (ux, uy) = Ballistics.Pos(u);
                float tall = u is Mutant mm && !(u is HumanEnemy) ? Ballistics.MutantHeight(mm) : Ballistics.BodyTiles;
                float cy = uy + Ballistics.FeetBelow - 0.5f * tall;                                   // the middle of the body, not its feet
                float d = Mathf.Sqrt((x - ux) * (x - ux) + (y - cy) * (y - cy));
                if (d <= assist + 0.33f * (tall - 1f) && d < bd) { bd = d; best = u; }
            }
            return best;
        }

        public static ICombatant UnitUnder(float x, float y)
        {
            ICombatant best = null; float bd = 9f;
            foreach (var u in Gunplay.Bodies())
            {
                if (u is Player || !u.IsAlive() || EelBoss.Is(u) || !Vision.Shows(u)) continue;     // (the eel: below, by its whole body)
                var (ux, uy) = Ballistics.Pos(u);
                bool mut = u is Mutant mm && !(u is HumanEnemy);
                // the crosshair is over the SPRITE's own pixels' reach (HitProfiles: the width of the row of the sprite at that height); a body without a silhouette (the bosses) keeps the old box
                float a = GameData.HitProfiles.Half(mut ? ((Mutant)u).Db?.Name : "human", uy + Ballistics.FeetBelow - y);
                if (a < 0f)
                {
                    if (!mut || GameData.HitProfiles.All.ContainsKey(((Mutant)u).Db?.Name ?? "")) continue;
                    float tall = Ballistics.MutantSize((Mutant)u);
                    if (System.Math.Abs(x - ux) > 0.45f * tall || y > uy + Ballistics.FeetBelow || y < uy + Ballistics.FeetBelow - tall) continue;
                }
                else if (System.Math.Abs(x - ux) > a) continue;
                float d = System.Math.Abs(x - ux) + System.Math.Abs(y - uy);
                if (d < bd) { bd = d; best = u; }
            }
            if (best == null && EelSump.BodyUnder(x, y, out var eel, out _, out _)) best = eel;       // Boss 3: any part of the eel's body (the sprite is lifted by its height)
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
            BrainBoss.Tick(Time.deltaTime);                                        // Boss 2: the vat's orbs, props and pull
            EelBoss.Tick(Time.deltaTime);                                          // Boss 3: the eel
            Tier2Story.Tick(Time.deltaTime);                                       // Tier 2 beat 1 "Taken"
            SpoolBoss.Tick(Time.deltaTime);                                        // Boss 4: Spool in the Dog Kennels
            ChoirBoss.Tick(Time.deltaTime);                                       // Tier 6: the Choir's bell in the Abandoned City
            Tier2Poi.Tick();                                                      // Tier 2 points of interest: papers + crews
            CrownJob.Tick(Time.deltaTime);                                         // Tier 2 "The Crown": the waves at the Nest, then the King
            Tier3Annex.TickRt(Time.deltaTime);                                     // Tier 3 beat 4 "The Door Under the Dock"
            Emplacement.Tick(Time.deltaTime);                                      // Tier 4: the Barrier's machine guns
            Tier4Story.TickRt(Time.deltaTime);                                     // Tier 4: the siege of the Barrier
            Preacher.Tick(Time.deltaTime);                                         // Tier 4: the Preacher's pulse
            StoryBarks.Tick();                                                     // the story's people and Pavel's small talk
            GreyRobes.Tick(Time.deltaTime);                                        // Tier 1: "those" on the marsh roads
            Tier1Story.TickRt(Time.deltaTime);                                     // Tier 1: the night defence of Bonfire Village
            Gaunt.Tick();                                                          // the Gaunt: its call, and the ones it drove when it falls
            Gunplay.Fly(Time.deltaTime);                                           // RT 8: bullets fly
            Explosions.Tick();                                                     // thrown grenades: in the air, then the fuse
            foreach (var e in EncounterState.Enemies.ToList())
            {
                if (!e.IsAlive()) { NextTurn.Remove(e); continue; }
                if (Boss.Of(e) is Boss.Fight bf) { Boss.Tick(bf, TargetOf(e, local)); continue; }   // Boss 1: a boss runs his own moves
                if (BrainBoss.Is(e) || EelBoss.Is(e)) continue;                  // Boss 2/3: their own Tick runs them
                bool fighting = e.IsAggroed && EncounterState.IsActive(e) || Companion.Is(e) && EncounterState.InCombat;
                if (!fighting) { NextTurn.Remove(e); if (!e.IsAggroed && !Companion.Is(e) && !TestingGround.IsDummy(e) && NpcFights.Real(e, grid)) { } continue; }   // squads fighting each other (NpcFights.Real) run here every frame
                p = Companion.Is(e) ? local : AttentionOf(e, local);             // co-op: half the enemies fight the player nearest them, the rest are shared among the others (AttentionOf)
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
                pl.Upkeep();
                AnomalyEngine.ProcessTurnStart(pl);
                if (pl.IsAlive()) Contact.EndRound(pl);
            }
            TurnLoop.CheckCombatEnd();
            if (EncounterState.InCombat) TurnLoop.RadioForHelp();
        }
    }
}
