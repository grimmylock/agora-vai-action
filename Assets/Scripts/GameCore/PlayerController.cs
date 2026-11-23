// The player's controls. Real time only (2026-11-17: the turn-based click-to-move, click-to-target and
// end-turn branch that used to follow RealTime() was unreachable and is gone): RealTime() below runs the
// keys that are not Rt's own (movement, aim, fire and reload are in Rt.cs), TickClock ticks the zone's
// awareness 4.2 times a second, Step crosses one tile (doors, anomalies, exits, regions).
// Added automatically by GameBootstrap - no scene wiring required.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameCore
{
    public class PlayerController : MonoBehaviour
    {
        private void Awake() { Radiant.Init(); Rt.On = true; Rt.Reset(); Time.timeScale = 1f; }   // real-time fork: the game runs on the clock (Rt.cs); the harness stays turn-based

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

        private static void TickClock(Player player)
        {
            _clock += Time.deltaTime;
            while (_clock >= SecondsPerMinute) { _clock -= SecondsPerMinute; TurnLoop.TickAwareness(); }
        }

        public static bool Exploring() =>
            GameState.Current.CurrentTurn == Turn.Player && !EncounterState.InCombat
            && !EncounterState.Enemies.Any(e => e.IsAlive() && e.IsAggroed);

        /// <summary>Real-time fork: one tile crossed by free movement (Rt.MoveBy) - doors, anomalies, exits, regions.</summary>
        internal static void StepFree(Player player, int dx, int dy) => Step(player, dx, dy, true);

        /// <summary>Real-time fork (ACTION_DESIGN.md): the controls without turns. Movement, aim, fire and reload are Rt's;
        /// the rest are the old keys that still make sense. E also loots a body or picks up what's at your feet.</summary>
        static readonly HashSet<Player> _remoteUse = new HashSet<Player>();       // joined players whose E (Use) was already down: the examine fires on the press, not the hold
        private static void RealTime(Player player, Keyboard k, Mouse m)
        {
            bool panel = GameState.Current.ModalOpen || Story.InCutscene;   // a panel (or a cutscene) is up: you stand still, everything else runs
            if (k == null && !panel) return;
            if (!panel && k.escapeKey.wasPressedThisFrame && Time.frameCount != HudView.PackClosedFrame && (Rt.Paused || !HudView.CloseOldPanels())) { Rt.Paused = !Rt.Paused; Time.timeScale = Rt.Paused ? 0f : Rt.SlowMo ? Rt.SlowScale : 1f; }   // ESC pauses; nothing else does
            if (Story.InCutscene && !Net.Client && k != null && k.escapeKey.wasPressedThisFrame) { if (!VergerPhase2.Skip()) EelBoss.SkipIntro(); }     // ESC skips the eel's introduction, or the Verger's hat-and-scythe rite
            if (Rt.Paused && !Net.Client) return;                   // ponytail: ESC still pauses a host's whole world (Yan's rule: no pausing in co-op - with the menu work)
            var mine = panel || k == null ? default : Rt.Read(k, m);
            Net.Tick(Time.unscaledTime, mine);                       // RT 7y: a client sends this; a host sends snapshots
            if (Net.Client) return;                                  // the host's world is the only one: no tick here, the snapshot places you
            if (player.IsAlive()) Rt.PlayerTick(player, mine);       // co-op: the local player's devices become one Input packet
            foreach (var j in Player.All) if (j != player && j.IsAlive())                         // joined players, on their last packet (presses spent once)
            {
                var ji = Net.TakeInput(j); bool edge = ji.Use && !_remoteUse.Contains(j); if (ji.Use) _remoteUse.Add(j); else _remoteUse.Remove(j);
                if (edge && (EelBoss.TryExamine(j) || Tier2Story.TryUseDoor(j))) continue;                                      // a friend presses E at the crewman on the slab: the eel's cutscene
                Rt.PlayerTick(j, Story.InCutscene ? default : ji);                                // and in a cutscene they stand still, like the host
            }
            TickClock(player);
            Rt.WorldTick(player);
            if (!player.IsAlive()) { TurnLoop.CheckCombatEnd(); return; }   // dead: the world still runs above while a friend lives
            if (panel || Body.Down(player)) return;   // RT batch 2: down - crawl, or a stim from the belt (1-4)
            if (k.eKey.wasPressedThisFrame && Rt.DownedNear(player) == null)    // E next to a downed friend revives them (held, Rt.PlayerTick)
            {
                var corpse = EncounterState.Enemies.OfType<HumanEnemy>().FirstOrDefault(e => !e.IsAlive() && CombatantUtil.Distance(player, e) <= 1);
                var ground = TerrainMap.GetGroundItemsAt(player.X, player.Y);
                if (Emplacement.TryInteract(player)) { }                                                                                                  // the Barrier's tower gun: E climbs up or down before anything else can take the press (a corpse or an item beside the nook used to eat it)
                else if (EelBoss.TryExamine(player) || Tier2Story.TryUseDoor(player) || Radiant.TryTalk(player)) { }                                             // Boss 3: the crewman on the slab: examine him (the introduction) instead of looting
                else if (corpse != null) { if (!Boss.TryWake(player, corpse)) CorpseLootUI.Open(corpse); }   // Boss 1: the Visor's body
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
            if (Keybinds.Pressed(k, Keybinds.Search)) { if (!AnomalyEngine.TrySearchAnomaly(player)) { ZoneTravel.SearchArea(); QuestSystem.TryInvestigate(); } return; }
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
            if (k.f7Key.wasPressedThisFrame) { Lighting.Toggle(); return; }   // lighting v2 on/off, to compare with the old night overlay
            if (k.f6Key.wasPressedThisFrame) { Ballistics.ShowBoxes = !Ballistics.ShowBoxes; GameLog.Write($"[DEV] hit boxes {(Ballistics.ShowBoxes ? "on" : "off")}"); return; }
            if (k.f8Key.wasPressedThisFrame) { ColorGrade.Off = !ColorGrade.Off; return; }
            if (k.f10Key.wasPressedThisFrame) { Atmosphere.Advance(60f); GameLog.Write($"[DEV] {Atmosphere.Clock()} {Atmosphere.Current}"); return; }
            if (k.f11Key.wasPressedThisFrame) { Atmosphere.Current = (Weather)(((int)Atmosphere.Current + 1) % 5); GameLog.Write($"[DEV] {Atmosphere.Clock()} {Atmosphere.Current}"); }
#endif
        }

        /// <summary>One step of a walk. False = stop walking (blocked, left the zone, or just got spotted).</summary>
        private static bool Step(Player player, int dx, int dy, bool alreadyInCombat)
        {
            int beforeX = player.X, beforeY = player.Y, known = EncounterState.Anomalies.Count(a => a.Revealed);
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
    }
}
