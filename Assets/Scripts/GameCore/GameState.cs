// Ported from script.js: const gameState = { currentTurn, boltMode }, let gameMode, let macroState,
// let selectedEnemy. These were bare globals in the JS; bundled into small state classes here with
// a static Current instance each, so the rest of the port can reference GameState.Current the same
// way script.js referenced the bare `gameState` global.
using System.Collections.Generic;
using GameData;

namespace GameCore
{
    public enum Turn
    {
        Player,
        Mutant,
        CombatOver,
    }

    public enum GameMode
    {
        Tactical,
        Overworld,
        // New: shown at boot instead of auto-starting/auto-loading a game (see MainMenuUI/
        // GameBootstrap). PlayerController/TurnLoop already gate on "!= GameMode.Tactical", so
        // this value alone is enough to suppress all world input/simulation until the player
        // actually picks New Game or Load Game - no separate "menu open" flag needed.
        MainMenu,
    }

    public class GameState
    {
        public static GameState Current = new GameState();

        public Turn CurrentTurn = Turn.Player;
        public bool BoltMode = false;
        public GameMode Mode = GameMode.MainMenu;

        /// <summary>New: true while a modal UI panel (the shop, the death screen) is open -
        /// PlayerController checks this once at the top of Update() to suppress all world input
        /// (movement, attack, interact, end turn) while a menu is up, instead of every future
        /// modal needing its own bespoke input-blocking logic.</summary>
        public bool ModalOpen;

        /// <summary>Ported from `let selectedEnemy = null;`.</summary>
        public ICombatant SelectedEnemy;
    }

    /// <summary>Ported from `let macroState = { currentZoneId, discoveredHiddenNodes, artifactRolledZones };`.</summary>
    public class OfflineSquad
    {
        public string ZoneId;
        public string Faction;
        public List<Mutant> Members = new List<Mutant>();
        public int WorldX;
        public int WorldY;
        public bool IsActiveCombat;
        public bool IsHostileToPlayer = true;
        // Batch 37: a squad walking out of the zone you're in, to this zone, since this tick
        public string LeavingFor;
        public int LeavingSince;
        // Batch 43: the zone this squad was sent to (reinforcements walk there zone by zone); null = wandering
        public string Heading;
        // Batch 44: a quest's targets or its rival crew - they hold their ground (no wandering, no off-screen fights) until the job is done
        public string QuestId;
        // Batch 45: "caravan" (traders bound for a hub) or "migration:<nest>" (a mutant pack crossing the Zone); these roam
        // anywhere that isn't a Safe Zone (a caravan may also enter its hub), not just where their faction usually goes
        public string Job;
        // Batch 126: the road it holds (the zone that checkpoint's exit leads to - Checkpoints)
        public string Checkpoint;
        // Tick counter for lifecycle management (pruning/ageing)
        public int LastUpdatedTick = 0;
        // When true, squad members are persisted with full state instead of regenerated.
        public bool PersistFullState = false;
        // Batch 72: a Tier 1 Raider gang asks for its road toll once; paid, it leaves you alone until this world minute
        public bool ShakedownDone;
        public bool BossRolled;   // Batch 80: SquadManager.Arm rolled its boss chance
        public int TruceUntil;
    }

    public class MacroState
    {
        public static MacroState Current = new MacroState();

        public string CurrentZoneId = "bonfire_village";   // the map overhaul: New Game starts in the village
        public List<string> DiscoveredHiddenNodes = new List<string>();
        public int GarrisonRaidReadyDay; public bool GarrisonArmoryOpen;   // Batch 87: GarrisonRaid
        public HashSet<string> Keys = new HashSet<string>();   // the map overhaul: keys to locked dungeons (ZoneDef.RequiresKey)
        public List<string> ArtifactRolledZones = new List<string>();
        public List<OfflineSquad> OfflineSquads = new List<OfflineSquad>();
        public Dictionary<string, TerrainTile[,]> PersistedZoneTerrain = new Dictionary<string, TerrainTile[,]>();
        public Dictionary<string, List<InteractiveObjectInstance>> PersistedZoneInteractiveObjects = new Dictionary<string, List<InteractiveObjectInstance>>();
        // New (Batch 7): each zone's placed named landmarks (LandmarkDatabase), persisted the same
        // way as its terrain/interactive objects so they don't re-scatter to new tiles on revisit.
        public Dictionary<string, List<LandmarkInstance>> PersistedZoneLandmarks = new Dictionary<string, List<LandmarkInstance>>();
        public Dictionary<string, List<Anomaly>> PersistedZoneAnomalies = new Dictionary<string, List<Anomaly>>();
        // New (Batch 12): dropped items sitting on each zone's grid - see GroundItem.cs.
        public Dictionary<string, List<GroundItem>> PersistedZoneGroundItems = new Dictionary<string, List<GroundItem>>();
        // Zones whose initial hostile squads have already been rolled - so leaving and
        // re-entering a zone you cleared doesn't respawn its population from scratch (see
        // SquadManager.EnsureZonePopulated). Since Batch 41 every hostile surface zone is seeded at New Game.
        public HashSet<string> PopulatedZones = new HashSet<string>();
        // Simple global tick counter for overworld/squad aging
        public int WorldTickCounter = 0;

        // Wandering Traders (Zone Map file, section 2): rolled fresh on every zone entry, cleared
        // by the next one - "only around for this scene," per the source. Null/empty ZoneId means
        // none is present right now.
        public string WanderingTraderZoneId;
        public int WanderingTraderTier;

        // New (Batch 5): the shop's Layer 2 "exciting" stock - a handful of one-off special items
        // per zone-with-a-trader, re-rolled on every zone entry (see ZoneTravel.RollShopStock).
        // Keyed by zone id rather than by trader identity since only one trader is ever active per
        // zone at a time in this design.
        public Dictionary<string, List<ShopItemDef>> RolledShopStock = new Dictionary<string, List<ShopItemDef>>();

        // New (file 10 Days): a new Day starts on a full rest or after 3 zone crossings without one.
        public int Day = 1;
        public int CrossingsSinceRest;
        // Batch 31: time of day (0-24) and weather - see Atmosphere.cs
        public float Hour = 7f;
        public string Weather = "Overcast";

        // New (file 10 living world) - see WorldEvents.cs.
        public Dictionary<string, int> GoalBoxes = new Dictionary<string, int>();
        public List<string> CompletedGoals = new List<string>();
        public List<string> DeckCooling = new List<string>();   // Batch 52: "faction|rank|hoursLeft" - a dead unit's card, not back in the deck yet
        public List<string> NamedNpcs = new List<string>();
        public List<string> Rumors = new List<string>();
        // Batch 38: what the A-Life tick really did, newest first: "day|kind|zoneId|text" (kind F = a squad wiped out, M = a squad moved)
        public List<string> WorldNews = new List<string>();
        public Dictionary<string, int> KillsToday = new Dictionary<string, int>();
        public string EventOfDay;
        public int ExtraRumors;
        public string CaravanZoneId;
        public string MigrationZoneId;
        public int MigrationDaysLeft;
        public int SurgeRounds;
        public string SurgeZoneId;
        public int LastRumorDay;

        // New (file 09 Part 3) - see StorySystem.cs
        public List<string> StoryFlags = new List<string>();
        public string Sponsor;
        public string Ending;
    }
}
