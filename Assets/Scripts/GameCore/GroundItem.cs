// New (Batch 12): an item lying on the tactical grid - previously InventoryUI's Drop button
// (and overweight players who wanted to lighten their load) just deleted the item via
// Player.RemoveItem, it never went anywhere. Mirrors InteractiveObjectInstance's shape (a plain
// positioned runtime instance) but with no skill check/reward wrapper - picking one up is just
// "walk up, click, take it back" (see PlayerController/TerrainMap.DropItem). Not saved to disk,
// same convention as InteractiveObjects/Landmarks/Anomalies (see SaveSystem.cs's "zone tactical
// state regenerates on load" note) - a dropped item persists for the rest of this play session
// (MacroState.PersistedZoneGroundItems) but a zone regenerates fresh (empty) on a real save/load
// like every other piece of per-zone tactical state.
namespace GameCore
{
    public class GroundItem
    {
        public int X;
        public int Y;
        public InventoryItem Item;
    }
}
