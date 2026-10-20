// XCOM-style point-and-click control, used for both free exploration and combat on the same
// map/grid (there's no separate mode switch — see ZoneTravel.EnterZone): click a reachable tile
// to walk there (along the ported A* path, spending MP tile by tile — same as Mutant.MoveToward
// already does for the AI), click an enemy to fire on it. Walking onto a green ZoneExit marker
// crosses into that connected zone mid-step. Replaces free WASD movement, which doesn't fit a
// turn-based tactics game.  Added automatically by GameBootstrap — no scene wiring required.
//
// Batch 30: out of combat, WASD / arrows walk freely (ExploreKeys); click-to-move still works.
// Controls: Left click = move to tile / attack enemy under cursor. Right click = shoot a barrel,
// door or piece of cover (or attack the enemy under the cursor). E = interact with a nearby
// object. F = search the area for hidden routes. R = reload. B = open the shop in a Safe Zone.
// Space = end turn.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameCore
{
    public class PlayerController : MonoBehaviour
    {
        private void Awake() { Rt.On = true; Rt.Reset(); Time.timeScale = 1f; }   // real-time fork: the game runs on the clock (Rt.cs); the harness stays turn-based

        private void Update()
        {
            var player = Player.Current;
            if (player == null) return;
            if (GameState.Current.Mode != GameMode.Tactical) return;
            // Anything can drop HP to 0 (bleeding at turn start, an anomaly underfoot, a failed
            // interaction) - one check here catches every path instead of one per caller.
            if (player.Hp <= 0 && GameState.Current.CurrentTurn != Turn.CombatOver) { TurnLoop.CheckCombatEnd(); if (!Rt.On || TurnLoop.AllDead) return; }   // co-op: the world goes on while a friend lives
            // co-op (Yan: "Talking to npc, world keeps going as normal"): a panel (talk, trade, loot, the sheet) stops your hands, not the world
            if (Rt.On && GameState.Current.ModalOpen && GameState.Current.CurrentTurn != Turn.CombatOver) { RealTime(player, null, null); return; }
            // A modal panel (shop, death screen) is up - don't let any keybind or click fall
            // through to the world underneath it.
            if (GameState.Current.ModalOpen || GameState.Current.CurrentTurn == Turn.CombatOver) return;

            var keyboard = Keyboard.current;
            if (Rt.On) { RealTime(player, keyboard, Mouse.current); return; }
            // Batch 30: out of combat you just walk - WASD / arrows, held, step after step at walking
            // pace (the turns still tick underneath, unseen). Once anything hostile is aware of you,
            // WASD go back to being combat keys and movement is click-to-move on your turn.
            bool exploring = Exploring();
            if (exploring) { player.Ap = System.Math.Max(player.Ap, player.MaxAp); player.Mp = System.Math.Max(player.Mp, player.MaxMp); }   // Batch 59: out of combat AP and MP are never spent
            if (exploring) TickClock(player);
            if (exploring && keyboard != null && ExploreKeys(keyboard, player)) { _walk.Clear(); return; }
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                TurnLoop.PlayerEndTurn();
                return;
            }
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                // R clears a jam first if there is one (1 AP), otherwise reloads.
                if (player.EquippedWeapon != null && player.EquippedWeapon.IsJammed) player.ClearWeaponJam();
                else player.ReloadWeapon();
                return;
            }
            // New combat actions (file 00 §5-6): fire mode, Aim, Take Cover, Evasive Maneuvers.
            if (keyboard != null && keyboard.xKey.wasPressedThisFrame) { player.ToggleFireMode(); return; }
            if (keyboard != null && keyboard.lKey.wasPressedThisFrame) { player.CycleAimedPart(); return; }   // Batch 57
            if (keyboard != null && keyboard.nKey.wasPressedThisFrame) { player.ToggleSneak(); return; }      // Batch 66
            if (keyboard != null && keyboard.uKey.wasPressedThisFrame) { player.ToggleFlashlight(); return; } // Batch 92
            if (keyboard != null && keyboard.qKey.wasPressedThisFrame) { player.CycleAmmoType(); return; }
            if (keyboard != null && keyboard.yKey.wasPressedThisFrame && (exploring || GameState.Current.CurrentTurn == Turn.Player)) { player.SwapWeapons(); return; }   // Batch 80
            if (keyboard != null && keyboard.zKey.wasPressedThisFrame) { WorldDays.Rest(); return; }
            if (GameState.Current.CurrentTurn == Turn.Player && keyboard != null && !exploring)
            {
                if (keyboard.aKey.wasPressedThisFrame) { player.Aim(); return; }
                if (keyboard.cKey.wasPressedThisFrame) { player.TakeCover(); return; }
                if (keyboard.vKey.wasPressedThisFrame) { player.EvasiveManeuvers(); return; }
                if (keyboard.gKey.wasPressedThisFrame) { player.CycleThrowable(); return; }
                if (keyboard.tKey.wasPressedThisFrame) { player.Intimidate(); return; }
                if (keyboard.hKey.wasPressedThisFrame) { player.RunAndGun(); return; }
                if (keyboard.sKey.wasPressedThisFrame) { player.ArmSuppressiveFire(); return; }
                if (keyboard.kKey.wasPressedThisFrame) { player.HunkerDown(); return; }      // Batch 72
                if (keyboard.oKey.wasPressedThisFrame) { player.Overwatch(); return; }       // Batch 72
            }
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                InteractionEngine.AttemptInteract(player);
                return;
            }
            if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
            {
                // file 03: next to a resolved anomaly, F searches it for an artifact instead.
                if (AnomalyEngine.TrySearchAnomaly(player)) return;
                ZoneTravel.SearchArea();
                // New (Batch 6): also resolves any active quest's in-zone Investigate/Retrieve
                // skill check - "search the area" already covers this game's one investigate verb.
                QuestSystem.TryInvestigate();
                return;
            }
            if (keyboard != null && keyboard.bKey.wasPressedThisFrame)
            {
                ShopUI.Toggle();
                return;
            }
            if (keyboard != null && keyboard.pKey.wasPressedThisFrame)
            {
                CharacterUI.ShowSheet();
                return;
            }
            if (keyboard != null && keyboard.mKey.wasPressedThisFrame)
            {
                WorldUI.Toggle();
                return;
            }
            if (keyboard != null && keyboard.jKey.wasPressedThisFrame)
            {
                QuestUI.Toggle();
                return;
            }
            // New (Batch 9): quick save/load - F5/F9 is the standard convention this game had none
            // of before (SaveSystem.cs).
            if (keyboard != null && keyboard.f5Key.wasPressedThisFrame)
            {
                SaveSystem.Save();
                return;
            }
            if (keyboard != null && keyboard.f9Key.wasPressedThisFrame)
            {
                SaveSystem.Load();
                return;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // dev keys for testing light and weather: F8 = colour grade on/off, F10 = +1 hour, F11 = next weather
            if (keyboard != null && keyboard.f8Key.wasPressedThisFrame) { ColorGrade.Off = !ColorGrade.Off; GameLog.Write($"[DEV] colour grade {(ColorGrade.Off ? "off" : "on")}"); return; }
            if (keyboard != null && keyboard.f10Key.wasPressedThisFrame) { Atmosphere.Advance(60f); GameLog.Write($"[DEV] {Atmosphere.Clock()} {Atmosphere.Current}"); return; }
            if (keyboard != null && keyboard.f11Key.wasPressedThisFrame)
            {
                Atmosphere.Current = (Weather)(((int)Atmosphere.Current + 1) % 5);
                GameLog.Write($"[DEV] {Atmosphere.Clock()} {Atmosphere.Current}"); return;
            }
#endif

            if (GameState.Current.CurrentTurn != Turn.Player) return;

            var mouse = Mouse.current;
            if (mouse == null) return;
            bool rightClick = mouse.rightButton.wasPressedThisFrame;
            if (!mouse.leftButton.wasPressedThisFrame && !rightClick) return;
            // Defense in depth for ModalOpen above: don't let a click on a UI element (a shop
            // button, etc.) also register as a world click underneath it.
            if (UnityEngine.EventSystems.EventSystem.current != null
                && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            if (Camera.main == null) return;
            // Batch 77: a click on the map while the targeting panel is up just closes it
            if (HudView.TargetingOpen) { HudView.CloseTargeting(); return; }

            var world = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());
            if (!TacticalGridView.WorldToGrid(world, out int x, out int y)) return;

            if (player.ArmedAction == "suppress")
            {
                if (rightClick) { player.ArmedAction = null; GameLog.Write("Suppressive Fire cancelled."); }
                else { player.SuppressAt(x, y); TurnLoop.CheckCombatEnd(); }
                return;
            }
            // A grenade is armed (picked from the inventory): left-click throws, right-click cancels.
            if (player.ArmedThrowable != null)
            {
                if (rightClick) { player.ArmedThrowable = null; GameLog.Write("You put the grenade away."); }
                else { player.ThrowAt(x, y); TurnLoop.CheckCombatEnd(); }
                return;
            }

            var targetEnemy = EncounterState.Enemies.Find(e => e.IsAlive() && e.X == x && e.Y == y && Vision.Shows(e));   // can't target what you can't see (Batch 49: or a cloaked mutant too far to make out)

            // Batch 103: a left click on a desk, barrel or crate right next to you shoves it
            if (!rightClick && Props.Movable(x, y) && CombatantUtil.Distance(player, x, y) == 1 && TerrainMap.UnitAt(x, y, null) == null)
            {
                if (Props.Shove(player, x, y)) TurnLoop.CheckCombatEnd();
                return;
            }

            // New: right-click shoots the map itself - an explosive barrel, a door, or the cover
            // an enemy is hiding behind (see Weapon.FireAtTile). On a living enemy it's a normal attack.
            // A surrendered NPC: left-click takes their gear, right-click is an execution.
            if (targetEnemy is HumanEnemy prisoner && prisoner.Morale == "Surrendered" && !rightClick)
            {
                EnemyAI.HandlePrisoner(prisoner, player);
                return;
            }

            if (rightClick && targetEnemy == null)
            {
                player.ShootTile(x, y);
                TurnLoop.CheckCombatEnd();
                return;
            }

            // Batch 77 (Yan: "targetting similar to xcom, where you click the enemy bodypart"): a left click on a hostile
            // opens the targeting panel (HudView.Targeting); a sneak takedown and right-click still act at once.
            bool takedown = player.Sneaking && targetEnemy is HumanEnemy th && !th.IsAggroed && CombatantUtil.Distance(player, th) <= 1;
            if (targetEnemy != null && !rightClick && !takedown && !Companion.Is(targetEnemy) && HudView.OpenTargeting(targetEnemy)) return;
            if (targetEnemy != null)
            {
                player.Attack(targetEnemy, null); // Attack() already reports range/LoS/ammo failures via GameLog
                TurnLoop.CheckCombatEnd(); // in case that shot just killed the last enemy
                return;
            }

            // New (Batch 10): clicking a corpse loots it instead of walking onto it - the "click
            // an enemy" gesture already covers "click a living one to fire", this is its natural
            // dead-enemy counterpart. Dead HumanEnemies stay in EncounterState.Enemies (see
            // Weapon.Fire) rather than being removed, so they're still found by X/Y here.
            var corpse = EncounterState.Enemies.OfType<HumanEnemy>().FirstOrDefault(e => !e.IsAlive() && e.X == x && e.Y == y);
            if (corpse != null)
            {
                CorpseLootUI.Open(corpse);
                return;
            }

            // New (Batch 12): clicking a tile with dropped items on it picks all of them up - no
            // separate loot screen needed, it's just your own stuff (or anyone's loose gear) lying
            // in the open, not a corpse/container worth a dedicated UI.
            var ground = TerrainMap.GetGroundItemsAt(x, y);
            if (ground.Count > 0)
            {
                foreach (var g in ground)
                {
                    player.AddItem(g.Item);
                    TerrainMap.GroundItems.Remove(g);
                }
                GameLog.Write(ground.Count == 1 ? $"Picked up {ground[0].Item.Name}." : $"Picked up {ground.Count} items.");
                Sfx.Play("pickup");
                return;
            }

            var c0 = TacticalGridView.GridToWorld(x, y);   // Batch 114: stop where you clicked inside the tile, not on its centre
            StandAt = new Vector3(Mathf.Clamp(world.x - c0.x, -0.3f, 0.3f), Mathf.Clamp(world.y - c0.y, -0.3f, 0.3f), 0f);
            WalkToward(player, x, y);
        }

        /// <summary>Batch 114 (free movement, look): where inside its tile the player stands - the last click's spot.
        /// Purely visual; the rules still use the tile.</summary>
        public static Vector3 StandAt;

        // Batch 67 (Yan chose "Clock runs alone"): out of a fight the clock runs by itself at walking pace - a game minute
        // for every tile you could walk - so the Zone moves while you stand still. Your steps no longer move time; the
        // clock does, so sneaking (half pace) still costs twice the time per tile. A clicked route is walked one step
        // at a time as the sprite catches up, like held keys, instead of all at once.
        public const float SecondsPerMinute = 1f / UnitMotionView.ExploreSpeed;
        private static float _clock;
        private static readonly Queue<(int x, int y)> _walk = new Queue<(int x, int y)>();
        /// <summary>Batch 127: stop a click route (a squad that hasn't seen you came into view - don't walk into its sight).</summary>
        public static void StopWalk() => _walk.Clear();

        private static void TickClock(Player player)
        {
            _clock += Time.deltaTime;
            while (_clock >= SecondsPerMinute && (Rt.On || Exploring())) { _clock -= SecondsPerMinute; TurnLoop.TickAwareness(); }
            if (Rt.On) return;
            if (!Exploring()) { _clock = 0f; _walk.Clear(); return; }
            if (_walk.Count > 0 && UnitMotionView.PlayerQueue <= 1)
            {
                var (x, y) = _walk.Dequeue();
                if (!Step(player, x - player.X, y - player.Y, false)) _walk.Clear();
            }
        }

        public static bool Exploring() =>
            GameState.Current.CurrentTurn == Turn.Player && !EncounterState.InCombat
            && !EncounterState.Enemies.Any(e => e.IsAlive() && e.IsAggroed);

        /// <summary>Held movement keys: one step whenever the walk animation has caught up. True = a key was held.</summary>
        private static bool ExploreKeys(Keyboard k, Player player)
        {
            int dx = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0);
            int dy = (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0) - (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0);
            if (dx == 0 && dy == 0) return false;
            StandAt = Vector3.zero;                                        // Batch 114: walking by keys keeps to the middle
            if (UnitMotionView.PlayerQueue > 1) return true;              // still walking the last step
            var grid = GameServices.RequireGrid();
            // diagonal into a wall: slide along whichever axis is open
            if (dx != 0 && dy != 0 && (!grid.CanEnterTile(player.X + dx, player.Y + dy)
                || float.IsInfinity(grid.StepCost(player, player.X, player.Y, player.X + dx, player.Y + dy))))   // Batch 102: no squeezing past a wall corner
            {
                if (grid.CanEnterTile(player.X + dx, player.Y)) dy = 0;
                else if (grid.CanEnterTile(player.X, player.Y + dy)) dx = 0;
            }
            if (!grid.CanEnterTile(player.X + dx, player.Y + dy)) return true;
            Step(player, dx, dy, false);
            return true;
        }

        /// <summary>Real-time fork: one tile crossed by free movement (Rt.MoveBy) - doors, anomalies, exits, regions.</summary>
        internal static void StepFree(Player player, int dx, int dy) => Step(player, dx, dy, true);

        /// <summary>Real-time fork (ACTION_DESIGN.md): the controls without turns. Movement, aim, fire and reload are Rt's;
        /// the rest are the old keys that still make sense. E also loots a body or picks up what's at your feet.</summary>
        private static void RealTime(Player player, Keyboard k, Mouse m)
        {
            bool panel = GameState.Current.ModalOpen;                // a panel is up: you stand still, everything else runs
            if (k == null && !panel) return;
            if (HudView.TargetingOpen) HudView.CloseTargeting();
            if (!panel && k.escapeKey.wasPressedThisFrame) { Rt.Paused = !Rt.Paused; Time.timeScale = Rt.Paused ? 0f : Rt.SlowMo ? Rt.SlowScale : 1f; }   // ESC pauses; nothing else does
            if (Rt.Paused) return;
            if (player.IsAlive()) Rt.PlayerTick(player, panel ? default : Rt.Read(k, m));   // co-op: the local player's devices become one Input packet
            foreach (var j in Player.All) if (j != player && j.IsAlive()) Rt.PlayerTick(j, Rt.Remote.TryGetValue(j, out var ji) ? ji : default);   // joined players, on their last packet
            TickClock(player);
            Rt.WorldTick(player);
            if (!player.IsAlive()) { TurnLoop.CheckCombatEnd(); return; }   // dead: the world still runs above while a friend lives
            if (panel || Body.Down(player)) return;   // RT batch 2: down - crawl, or a stim from the belt (1-4)
            if (k.eKey.wasPressedThisFrame && Rt.DownedNear(player) == null)    // E next to a downed friend revives them (held, Rt.PlayerTick)
            {
                var corpse = EncounterState.Enemies.OfType<HumanEnemy>().FirstOrDefault(e => !e.IsAlive() && CombatantUtil.Distance(player, e) <= 1);
                var ground = TerrainMap.GetGroundItemsAt(player.X, player.Y);
                if (corpse != null) CorpseLootUI.Open(corpse);
                else if (ground.Count > 0)
                {
                    foreach (var g in ground) { player.AddItem(g.Item); TerrainMap.GroundItems.Remove(g); }
                    GameLog.Write(ground.Count == 1 ? $"Picked up {ground[0].Item.Name}." : $"Picked up {ground.Count} items.");
                    Sfx.Play("pickup");
                    foreach (var g in ground) Melee.EquipIfEmptyHanded(player, g.Item);   // RT 7a: your thrown weapon back in hand
                }
                else if (Melee.PullLodged(player)) { }                                  // RT 7a: a blade stuck in cover next to you
                else InteractionEngine.AttemptInteract(player);
                return;
            }
            if (k.fKey.wasPressedThisFrame) { if (!AnomalyEngine.TrySearchAnomaly(player)) { ZoneTravel.SearchArea(); QuestSystem.TryInvestigate(); } return; }
            if (k.uKey.wasPressedThisFrame) { player.ToggleFlashlight(); return; }
            if (k.qKey.wasPressedThisFrame) { player.CycleAmmoType(); return; }
            if (k.yKey.wasPressedThisFrame) { player.SwapWeapons(); return; }
            if (k.zKey.wasPressedThisFrame && !EncounterState.InCombat) { WorldDays.Rest(); return; }
            if (k.bKey.wasPressedThisFrame) { ShopUI.Toggle(); return; }
            if (k.pKey.wasPressedThisFrame) { CharacterUI.ShowSheet(); return; }
            if (k.mKey.wasPressedThisFrame) { WorldUI.Toggle(); return; }
            if (k.jKey.wasPressedThisFrame) { QuestUI.Toggle(); return; }
            if (k.f5Key.wasPressedThisFrame) { SaveSystem.Save(); return; }
            if (k.f9Key.wasPressedThisFrame) { SaveSystem.Load(); return; }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (k.f8Key.wasPressedThisFrame) { ColorGrade.Off = !ColorGrade.Off; return; }
            if (k.f10Key.wasPressedThisFrame) { Atmosphere.Advance(60f); GameLog.Write($"[DEV] {Atmosphere.Clock()} {Atmosphere.Current}"); return; }
            if (k.f11Key.wasPressedThisFrame) { Atmosphere.Current = (Weather)(((int)Atmosphere.Current + 1) % 5); GameLog.Write($"[DEV] {Atmosphere.Clock()} {Atmosphere.Current}"); }
#endif
        }

        /// <summary>One step of a walk. False = stop walking (blocked, left the zone, or just got spotted).</summary>
        private static bool Step(Player player, int dx, int dy, bool alreadyInCombat)
        {
            int beforeX = player.X, beforeY = player.Y, known = EncounterState.Anomalies.Count(a => a.Revealed);
            // Batch 59: exploring has no turns - walking out of combat never runs out of MP or AP
            if (!EncounterState.InCombat && GameState.Current.CurrentTurn == Turn.Player) { player.Mp = System.Math.Max(player.Mp, System.Math.Max(player.MaxMp, TurnLoop.WalkMp)); player.Ap = System.Math.Max(player.Ap, player.MaxAp); }   // Batch 110: a diagonal through a bush costs 3, even overloaded
            bool exploring = Exploring();
            player.Move(dx, dy);
            if (player.X == beforeX && player.Y == beforeY) return false; // step was rejected (blocked / out of MP)

            // Let the zone react to each step taken: enemies already here can notice you (or
            // wander past) near-instantly instead of only in a batch once MP runs out.
            // Batch 67: exploring, the clock ticks the zone instead (TickClock), a quarter second at most behind your step.
            // RT: never per step - the clock (TickClock) already ticks the zone 4.2 times a second, and a tick per tile crossed
            // made the world (squads walking, the clock, the hourly A-Life) run faster the faster you moved: sprinting, enemies zoomed
            if (!exploring && !Rt.On) TurnLoop.TickAwareness();

            // co-op: map exits are taken together (ACTION_DESIGN) - a joined player standing on one doesn't move the game; ponytail:
            // the "everyone at the exit and confirmed" vote comes with the network layer
            var exit = player != Player.Current ? null : TerrainMap.Exits.FirstOrDefault(e => e.X == player.X && e.Y == player.Y);
            if (exit != null)
            {
                if (ZoneTravel.TravelToSector(exit.TargetZoneId)) return false; // the grid under our feet just changed - stop following the old path
            }
            ZoneTravel.UpdateRegion();   // the map overhaul: walking into the next region of a tier map

            // Freshly getting spotted hands control straight back instead of auto-walking the
            // rest of this click's path - the player should get to react to a break in
            // concealment, not blunder further into it. Doesn't apply if a fight was already
            // underway (see alreadyInCombat).
            if (!alreadyInCombat && EncounterState.Enemies.Any(e => e.IsAlive() && e.IsAggroed)) return false;
            if (EncounterState.Anomalies.Count(a => a.Revealed) > known) return false;   // Batch 49: your detector just found a field - stop and look

            return true;
        }

        private static (int x, int y) _detourAt = (-1, -1);

        private static void WalkToward(Player player, int targetX, int targetY)
        {
            // Only interrupt the walk on a NEW spot - if a fight's already underway, repositioning
            // clicks should still cover the full clicked path in one go, not stop after one tile.
            bool alreadyInCombat = EncounterState.Enemies.Any(e => e.IsAlive() && e.IsAggroed);

            var path = GameServices.RequireGrid().FindPath(player.X, player.Y, targetX, targetY, player);
            // Batch 67 (Yan: "i clicked one place and he walked all the way to the other side of the map"): when the only
            // way there is a long way round (a wall, a ledge, a known anomaly), say so and wait for a second click on the spot.
            int straight = System.Math.Abs(targetX - player.X) + System.Math.Abs(targetY - player.Y);
            if (path.Count - 1 > straight * 2 + 8 && _detourAt != (targetX, targetY))
            {
                _detourAt = (targetX, targetY);
                GameLog.Write($"No direct way there: the route goes round ({path.Count - 1} tiles). Click again to take it.");
                return;
            }
            _detourAt = (-1, -1);
            _walk.Clear();
            if (Exploring()) { for (int i = 1; i < path.Count; i++) _walk.Enqueue(path[i]); return; }   // Batch 67: walked by TickClock
            if (path.Count == 0 && straight > 0) GameLog.Write("You can't get there from here.");
            for (int i = 1; i < path.Count && (player.Mp > 0 || !EncounterState.InCombat); i++)
            {
                if (GameState.Current.CurrentTurn != Turn.Player) break;
                if (!Step(player, path[i].x - player.X, path[i].y - player.Y, alreadyInCombat)) return;
            }
        }
    }
}
