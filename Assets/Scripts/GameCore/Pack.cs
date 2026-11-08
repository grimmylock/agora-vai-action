// Pack 1 (Yan, 2026-10-29: "the inventory is just text for now, we can add a real one like the case from re4 using our detailed
// sprites for items"; his answers: items take grid shapes ("could be"), they rotate ("yeah"), pack space and weight both limit
// ("both"), and it's "inside of a bag, also reflected by the color and size of the backpack"). The grid logic, no Unity:
// - the bag: columns x rows and its colour from the equipped backpack (BackpackDatabase; no backpack = your pockets);
// - each item stack takes a footprint (Size), turned when Rot; GridX < 0 = not in the bag (it didn't fit: PackUI shows it aside
//   until you make room or drop it - nothing is ever lost);
// - Place puts an item in the first free spot (as it is, then turned); Move puts it where you drag it; Refit repacks everything
//   (biggest first) when the bag changes.
// Ammo stays in the ammo stash (Ammo.cs), not in the grid.
using System.Collections.Generic;
using System.Linq;
using GameData;

namespace GameCore
{
    public static class Pack
    {
        public static BackpackDef Bag(Player p) =>
            BackpackDatabase.All.TryGetValue(p?.Inventory.Backpack ?? "none", out var b) ? b : BackpackDatabase.All["none"];

        /// <summary>An item's footprint in cells, laid flat (w >= h); Rot turns it.</summary>
        public static (int w, int h) Size(InventoryItem it)
        {
            switch (it.Type)
            {
                case "weapon":
                    switch (it.Category)
                    {
                        case "sidearm": return it.Key == "RK12Kuvalda" || it.Key == "SteppeEagle" ? (3, 2) : (2, 2);
                        case "smg": return (3, 2);
                        case "shotgun": return it.Key == "Obrez66" ? (3, 2) : (5, 2);
                        case "assault": return (5, 2);
                        case "sniper": return (6, 2);
                        case "heavy": return (6, 3);
                        case "melee":
                            var d = it.Key != null && WeaponDatabase.Categories["melee"].TryGetValue(it.Key, out var wd) ? wd : null;
                            return d == null ? (2, 1) : d.WeightKg >= 2f ? (4, 2) : d.WeightKg >= Melee.TwoHandedKg ? (4, 1) : d.WeightKg >= 0.8f ? (3, 1) : (2, 1);
                    }
                    return (3, 2);
                case "armor": return (3, 3);
                case "backpack": return (2, 2);
                case "detector": case "armorMod": return (2, 1);
                case "consumable":
                    if (it.Id == null) return (1, 1);
                    if (it.Id.Contains("Medkit") || it.Id == "splint" || it.Id == "mre") return (2, 1);
                    if (it.Id == "repairKit" || it.Id.Contains("Repair")) return (2, 2);
                    return (1, 1);
            }
            return (1, 1);
        }
        public static (int w, int h) Footprint(InventoryItem it) { var (w, h) = Size(it); return it.Rot ? (h, w) : (w, h); }

        static bool Inside(BackpackDef b, int x, int y, int w, int h) => x >= 0 && y >= 0 && x + w <= b.Cols && y + h <= b.Rows;
        /// <summary>Is the rectangle free of every other placed item?</summary>
        public static bool Free(Player p, InventoryItem except, int x, int y, int w, int h)
        {
            if (!Inside(Bag(p), x, y, w, h)) return false;
            foreach (var o in p.Inventory.Items)
            {
                if (o == except || o.GridX < 0) continue;
                var (ow, oh) = Footprint(o);
                if (x < o.GridX + ow && o.GridX < x + w && y < o.GridY + oh && o.GridY < y + h) return false;
            }
            return true;
        }

        /// <summary>The first free spot (rows top to bottom, left to right), flat then turned. False: no room, it stays out.</summary>
        public static bool Place(Player p, InventoryItem it)
        {
            var b = Bag(p);
            foreach (bool rot in new[] { false, true })
            {
                it.Rot = rot; var (w, h) = Footprint(it);
                for (int y = 0; y + h <= b.Rows; y++)
                    for (int x = 0; x + w <= b.Cols; x++)
                        if (Free(p, it, x, y, w, h)) { it.GridX = x; it.GridY = y; return true; }
            }
            it.Rot = false; it.GridX = -1;
            return false;
        }

        /// <summary>Drag and drop: put it at (x, y), turned or not, if that space is free.</summary>
        public static bool Move(Player p, InventoryItem it, int x, int y, bool rot)
        {
            bool was = it.Rot; it.Rot = rot; var (w, h) = Footprint(it);
            if (!Free(p, it, x, y, w, h)) { it.Rot = was; return false; }
            it.GridX = x; it.GridY = y; return true;
        }

        /// <summary>Everything out and back in, biggest first (a new bag). Returns what didn't fit.</summary>
        public static List<InventoryItem> Refit(Player p)
        {
            foreach (var it in p.Inventory.Items) { it.GridX = -1; it.Rot = false; }
            return Settle(p);
        }

        /// <summary>Places whatever isn't in the bag yet (an old save, a pickup), biggest first. Returns what still doesn't fit.</summary>
        public static List<InventoryItem> Settle(Player p)
        {
            var left = new List<InventoryItem>();
            foreach (var it in p.Inventory.Items.Where(i => i.GridX < 0).OrderByDescending(i => Size(i).w * Size(i).h).ToList())
                if (!Place(p, it)) left.Add(it);
            return left;
        }

        public static IEnumerable<InventoryItem> Outside(Player p) => p.Inventory.Items.Where(i => i.GridX < 0);
    }
}
