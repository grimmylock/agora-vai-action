// Ported from script.js: the `backpacks` array inside initEquipment() and the duplicate
// `backpackBonuses` map inside handleBackpackChange()/applyArmorModsForPlayer(). This table lived
// inline in script.js's UI code rather than in database.js, so it was missed in the original
// data-layer pass — added here now for completeness.
using System.Collections.Generic;

namespace GameData
{
    public class BackpackDef
    {
        public string Id;
        public string Name;
        public int CapacityBonus;
        public int Cols = 5, Rows = 4;          // Pack 1: the grid inside it (Pack.cs)
        public string Colour = "#3d3226";       // the bag's fabric in the pack screen (PackUI)
    }

    public static class BackpackDatabase
    {
        public static readonly Dictionary<string, BackpackDef> All = new Dictionary<string, BackpackDef>
        {
            // Pack 1: no backpack = your pockets (5x4); the sack and the frame open bigger grids in their own cloth
            ["none"] = new BackpackDef { Id = "none", Name = "None", CapacityBonus = 0, Cols = 5, Rows = 4, Colour = "#3d3226" },
            ["backpack_small"] = new BackpackDef { Id = "backpack_small", Name = "Small Canvas Sack (+10kg)", CapacityBonus = 10, Cols = 8, Rows = 5, Colour = "#7d6e4e" },
            ["backpack_tourist"] = new BackpackDef { Id = "backpack_tourist", Name = "Tourist Frame (+50kg)", CapacityBonus = 50, Cols = 11, Rows = 7, Colour = "#465a6e" },
        };
    }
}
