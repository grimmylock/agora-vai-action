// Ported from script.js: the {id,name,weightKg,qty,type,price} item shape used throughout
// (player.inventory.items, obj.reward.loot, shop listings, etc.), plus Player's this.inventory.
using System.Collections.Generic;
using System.Linq;

namespace GameCore
{
    public class InventoryItem
    {
        public string Id;
        public string Name;
        public float WeightKg;
        public int Qty = 1;
        public string Type;     // "consumable", "loot", "patch", "mutantPart", "weapon", "armor", ...
        public int? Price;
        // New: only set for Type "weapon"/"armor" - Category is the WeaponDatabase category /
        // ArmorDatabase tier key ("sidearm", "tier2", ...), Key is the entry key within it
        // ("PSh18Pchela", "LiberatorWindbreaker", ...). Lets EquipWeaponFromInventory/EquipArmorFromInventory
        // (Player.Equipment.cs) look the item back up in the database without a separate id
        // parsing scheme.
        public string Category;
        public string Key;
        // New: weapon/armor wear tier ("Working"/"Dirty"/"Damaged" - see ItemCondition.cs), rolled
        // by HumanLootSystem on drop and carried through equip/sell by Player.Equipment.cs/
        // ShopSystem.cs/Player.SaveLoad.cs - null/empty means Working (pristine), same as those
        // callers already assume.
        public string Condition;
        public string Upgrades;   // technician upgrades, comma list (Upgrades.cs)
        public int GridX = -1, GridY;   // Pack 1: where it sits in the bag (Pack.cs); -1 = not in it (didn't fit)
        public bool Rot;                // turned a quarter

        public InventoryItem Clone() => new InventoryItem
        {
            Id = Id, Name = Name, WeightKg = WeightKg, Qty = Qty, Type = Type, Price = Price,
            Category = Category, Key = Key, Condition = Condition, Upgrades = Upgrades,
        };
    }

    public class PlayerInventory
    {
        public List<InventoryItem> Items = new List<InventoryItem>();
        public Dictionary<string, int> AmmoReserve = new Dictionary<string, int>();
        public string Backpack;              // equipped backpack id, null if none
        public float CarryCapacityBase = 25.0f;   // file 00 §8 (50 with Pack Mule)
    }
}
