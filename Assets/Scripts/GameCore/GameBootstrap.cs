using System.Linq;
using GameData;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace GameCore
{
    public class GameBootstrap : MonoBehaviour
    {
        // Batch 73 (Yan: "the main menu is broken" - a blank blue screen on Play): the project has Enter Play Mode
        // Options on with domain reload off, so the game's statics survived from the last Play. Player.Current was still
        // set, so the menu was skipped, and the old run's state (dead, or mid-dialog) had nothing to draw. Every Play now
        // starts from a clean slate and always opens on the menu.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetGameStatics()
        {
            Player.Current = null;
            GameState.Current = new GameState();
            MacroState.Current = new MacroState();
            EncounterState.Enemies.Clear();
            EncounterState.Anomalies.Clear();
            EncounterState.InCombat = false;
            EncounterState.ResetFight();
            Shakedown.Reset(); GarrisonRaid.Reset(); MenuBackdrop.Reset(); Checkpoints.Reset(); Banter.Reset();
        }

        private void Awake()
        {
            // Validate macro map data once at startup.
            if (!MacroMapDatabase.Validate())
            {
                Debug.LogError("GameBootstrap: MacroMapDatabase validation failed.");
            }

            // Assign core game services so Player/Mutant/Weapon logic works.
            GameServices.Grid = new TacticalGridSystem();
            GameServices.Combat = new CombatResolver();
            // AnomalyTriggerService (AnomalyEngine.cs) was already fully ported and sitting unused —
            // TurnLoop already calls AnomalyEngine.ProcessTurnStart() every turn, but nothing wired
            // up the step-on-a-hidden-anomaly trigger until now. One-line fix: turn it on.
            GameServices.AnomalyTrigger = new AnomalyTriggerService();

            // The shop panel and death screen both use uGUI Buttons, which need an EventSystem +
            // input module to receive clicks at all - do this once, up front, rather than each UI
            // component defensively checking for it.
            EnsureEventSystem();

            // Route GameLog through an on-screen panel (built at runtime — no scene editing
            // required) in addition to the Console, so combat feedback is visible.
            if (GetComponent<RuntimeGameLog>() == null)
                gameObject.AddComponent<RuntimeGameLog>();

            // Grid view and click controls are self-contained (build themselves lazily off
            // GameState.Current/MacroState) — no scene wiring needed. There's no separate
            // Overworld menu screen anymore (see ZoneTravel.EnterZone) - the tactical grid IS the
            // explorable zone, all the time.
            if (GetComponent<TacticalGridView>() == null)
                gameObject.AddComponent<TacticalGridView>();
            if (GetComponent<PlayerController>() == null)
                gameObject.AddComponent<PlayerController>();
            // New: a "YOU DIED" overlay + restart button (TurnLoop.CheckCombatEnd's death branch
            // used to just leave input frozen forever with no feedback), and a buy/sell panel for
            // Safe Zones (ShopSystem existed but nothing ever opened it - see ShopUI).
            if (GetComponent<DeathScreen>() == null)
                gameObject.AddComponent<DeathScreen>();
            if (GetComponent<ShopUI>() == null)
                gameObject.AddComponent<ShopUI>();
            // New (Batch 10): the loot screen for a dead HumanEnemy's own stash - see
            // CorpseLootUI/HumanLootSystem. Same self-building convention.
            if (GetComponent<CorpseLootUI>() == null)
                gameObject.AddComponent<CorpseLootUI>();
            // New (Batch 6): the quest journal - QuestSystem existed but nothing ever showed
            // available/active quests or let the player accept one. Same self-building convention.
            if (GetComponent<TestingGroundUI>() == null)
                gameObject.AddComponent<TestingGroundUI>();
            if (GetComponent<QuestUI>() == null)
                gameObject.AddComponent<QuestUI>();
            if (GetComponent<WorldUI>() == null)
                gameObject.AddComponent<WorldUI>();
            if (GetComponent<CharacterUI>() == null)
                gameObject.AddComponent<CharacterUI>();
            if (GetComponent<ChoiceUI>() == null)
                gameObject.AddComponent<ChoiceUI>();
            // New: a static keybindings panel (bottom-right) so the controls aren't only a
            // one-time line in the scrolling combat log.
            if (GetComponent<ControlsHint>() == null)
                gameObject.AddComponent<ControlsHint>();
            // Batch 30: the HUD from the approved mockup (hides the debug panels above)
            if (GetComponent<HudView>() == null)
                gameObject.AddComponent<HudView>();
            // New (Batch 11): New Game / Load Game / Options (controls) / Quit - see MainMenuUI.
            if (GetComponent<MainMenuUI>() == null)
                gameObject.AddComponent<MainMenuUI>();
            if (GetComponent<CoopUI>() == null)   // RT 7y: host / join (the networking test)
                gameObject.AddComponent<CoopUI>();

            // New (Batch 11): show the main menu instead of auto-starting/auto-loading a game.
            // GameState.Mode defaults to GameMode.MainMenu (see GameState.cs), which already keeps
            // PlayerController/TurnLoop idle until the player actually picks New Game or Load Game
            // from the panel - StartNewGame()/SaveSystem.Load() are only ever called from there (or
            // from DeathScreen's Restart/Load buttons) now, never automatically. Guarded by
            // Player.Current so a second Awake() (shouldn't happen on a cold boot) doesn't re-show
            // the menu over an already-running game.
            MainMenuUI.Show();   // Batch 73: always (the statics are reset above, so there's never a game running at boot)

            GameLog.Write("GameBootstrap: Core services initialized.");
            GameLog.Write("Explore freely: click a tile to move, click an enemy to fire. Green markers are exits to connected zones - walk onto one to cross over.");
            GameLog.Write("E interacts with a nearby object, F searches the area for hidden routes, R reloads, B opens the shop in a Safe Zone, Space ends your turn.");
            GameLog.Write("F5 saves your game, F9 loads your last save.");
        }

        /// <summary>New: (re)creates the player and world state from scratch and drops them into
        /// the starting zone - the same loadout logic Awake() used to run inline, extracted so a
        /// death restart can call the exact same path instead of duplicating it. Doesn't touch
        /// GameServices or the MonoBehaviour components (TacticalGridView etc.) - those don't need
        /// recreating, they already read everything fresh from the statics reset here.</summary>
        public static void StartNewGame(CharacterSpec spec = null, string startZone = null, bool save = true)
        {
            spec = spec ?? new CharacterSpec();
            SaveSystem.CharacterId = SaveSystem.WorldId = null;   // RT 7x: a new slot is made at the end (none for the Testing Ground)
            GameState.Current = new GameState();
            MacroState.Current = new MacroState();
            FactionDeck.Reset(); GarrisonRaid.Reset();
            Companion.Current = null;
            StorySystem.ResetWorld();

            new Player();
            var player = Player.Current;
            player.Stats.S = spec.S; player.Stats.P = spec.P; player.Stats.A = spec.A; player.Stats.I = spec.I;
            // file 00 §4 loadouts; their Ru comes on top of the starting purse
            player.Ru = GameConstants.StartingRu;
            foreach (var f in GameConstants.StartHostile) player.Reputation[f] = -3 * Player.RepPoint;
            switch (spec.Loadout)
            {
                case "Ex-Military":
                    player.EquipWeapon("assault", "VKS74U");
                    Ammo.Add(player.Inventory, Ammo.Key(player.EquippedWeapon.Db, "Standard"), 60);
                    GiveWeapon(player, "melee", "SpetsnazSwitchblade");
                    player.EquipArmor("tier1", "RaiderJacket");
                    Give(player, "basicMedkit", 1); Give(player, "basicStim", 1);
                    player.Ru += 100;
                    break;
                case "Freelance Trader":
                    player.EquipWeapon("sidearm", "PSh18Pchela");
                    Ammo.Add(player.Inventory, Ammo.Key(player.EquippedWeapon.Db, "Standard"), 30);
                    player.EquipArmor("tier1", "ScrapPlatedVest");
                    player.Inventory.Backpack = "backpack_small";
                    player.CarryCapacityBonus = 10;
                    Give(player, "basicMedkit", 1);
                    player.Ru += 500;
                    break;
                default:   // Scavenger
                    player.EquipWeapon("sidearm", "PSh18Pchela");
                    Ammo.Add(player.Inventory, Ammo.Key(player.EquippedWeapon.Db, "Standard"), 30);
                    GiveWeapon(player, "melee", "HuntingKnife");
                    player.EquipArmor("tier1", "LeatherJacket");
                    Give(player, "basicMedkit", 2); Give(player, "bandage", 1);
                    player.Ru += 200;
                    break;
            }
            Give(player, "bandage", 1); Give(player, "splint", 1); Give(player, "basicStim", 1);   // real-time fork: a way back up when you go down
            if (!string.IsNullOrEmpty(spec.Perk)) player.TakePerk(spec.Perk);
            player.ApplyCarryEffects();
            Pack.Refit(player);                     // Pack 1: the starting kit packed into whatever bag you started with

            if (startZone != null)   // the Testing Ground: its own map, never seeded with squads or anomalies
            {
                MacroState.Current.CurrentZoneId = startZone;
                MacroState.Current.PopulatedZones.Add(startZone);
                MacroState.Current.PersistedZoneAnomalies[startZone] = new System.Collections.Generic.List<Anomaly>();
            }
            ZoneTravel.EnterZone(MacroState.Current.CurrentZoneId);
            SquadManager.SeedWorld();               // Batch 41: the rest of the Zone is populated from the start (after a map is loaded)
            player.CharacterName = SaveSystem.CleanName(spec.Name);
            if (startZone == null && save) SaveSystem.Create(spec.Name);   // RT 7x: the save at creation (Yan) - a new character in a new world
            GameLog.Write($"You step into the Perimeter: a Rookie {spec.Loadout.ToLowerInvariant()} with {player.Ru} Ru. P shows your character, W the state of the Zone.");
        }

        private static void Give(Player p, string shopId, int qty)
        {
            var d = ShopDatabase.Find(shopId);
            if (d != null) p.AddItem(new InventoryItem { Id = d.Id, Name = d.Name, WeightKg = d.WeightKg, Qty = qty, Type = d.Type, Price = d.Price });
        }

        private static void GiveWeapon(Player p, string cat, string key)
        {
            var d = ShopDatabase.Items.FirstOrDefault(i => i.Type == "weapon" && i.Category == cat && i.Key == key);
            if (d == null) return;
            p.AddItem(new InventoryItem { Id = d.Id, Name = d.Name, WeightKg = d.WeightKg, Qty = 1, Type = "weapon", Price = d.Price, Category = cat, Key = key });
            if (p.SecondaryWeapon == null) p.EquipSecondaryFromInventory(d.Id);   // Batch 80: the starting blade sits in the second slot (Y)
        }

        /// <summary>uGUI Buttons (the shop panel, the death screen's Restart button) silently do
        /// nothing without an EventSystem + a UI input module in the scene. This project uses the
        /// new Input System (see PlayerController), so it needs InputSystemUIInputModule
        /// specifically, not the old StandaloneInputModule.</summary>
        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }
    }
}
