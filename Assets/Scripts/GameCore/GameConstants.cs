// Ported from script.js top-level constants (lines 3-18).
// TILE_SIZE/GRID_WIDTH/GRID_HEIGHT drove both the canvas pixel size and the logical grid in the
// JS version. Unity's tactical grid may use different tile dimensions or a Tilemap instead of a
// fixed pixel canvas — re-check GridWidth/GridHeight against whatever grid the "grid/movement"
// port step actually builds; the depletion-rate constants (per-tile hunger/thirst/fatigue) are
// derived from GridWidth, so they'd need recalculating too if the grid size changes.
namespace GameCore
{
    public static class GameConstants
    {
        public const int TileSize = 25; // pixels, from the original canvas (800x600)

        // Fallback grid size used only when a zone has no authored MapLayout (see
        // MapLayoutDatabase) and TerrainMap falls back to procedural scatter. Every zone now
        // carries its own size via TerrainMap.Width/TerrainMap.Height set from its authored
        // layout - these two are no longer "the" grid size, just the default one.
        public const int GridWidth = 32;
        public const int GridHeight = 24;

        // Yan (b22): survival drains per zone crossing only, not per tile walked.
        public const float HungerPerTravel = 1.5f;
        public const float ThirstPerTravel = 1.5f;
        public const float TirednessPerTravel = 1.0f;

        // Yan (b22): only Hostile factions attack on sight (file 00 §12). Neutral ones hold fire
        // until you shoot first; Friendly ones never do.
        public const bool NeutralAttackOnSight = false;
        // ...so bandits and the cult start the game Hostile (-3), or no human would ever open fire.
        public static readonly string[] StartHostile = { "Raiders", "Obelisk" };

        // Yan (b22): every new character starts with 300 Ru; the loadout's Ru comes on top.
        public const int StartingRu = 300;

        // Yan (b22): healing makes you hungry, thirsty and sleepy. Each of the three drops by this
        // much per 10 HP healed (permanent or temporary). Food/drink items are exempt.
        // ponytail: placeholder rate, Yan hasn't picked the amounts yet; tune here.
        public const float SurvivalCostPer10Hp = 0.25f;

        // Batch 24: blood splatter, pools and gibs. Off = bodies still fall over, just no blood.
        public static bool Gore = true;
    }
}
