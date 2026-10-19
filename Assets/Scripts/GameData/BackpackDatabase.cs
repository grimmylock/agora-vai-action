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
    }

    public static class BackpackDatabase
    {
        public static readonly Dictionary<string, BackpackDef> All = new Dictionary<string, BackpackDef>
        {
            ["none"] = new BackpackDef { Id = "none", Name = "None", CapacityBonus = 0 },
            ["backpack_small"] = new BackpackDef { Id = "backpack_small", Name = "Small Canvas Sack (+10kg)", CapacityBonus = 10 },
            ["backpack_tourist"] = new BackpackDef { Id = "backpack_tourist", Name = "Tourist Frame (+50kg)", CapacityBonus = 50 },
        };
    }
}
