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
                        case "sidearm": return it.Key == "RK12Kuvalda" || it.Key == "SteppeEagle" || it.Key == "SperlingS96" ? (3, 2) : (2, 2);
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
                case "attachment": return it.Key == "reddot" || it.Key == "laser" ? (1, 1) : (2, 1);
                case "consumable":
                    if (it.Id == null) return (1, 1);
                    if (it.Id.Contains("Medkit") || it.Id == "splint" || it.Id == "mre") return (2, 1);
                    if (it.Id == "repairKit" || it.Id.Contains("Repair")) return (2, 2);
                    return (1, 1);
            }
            return (1, 1);
        }
        public static (int w, int h) Footprint(InventoryItem it) { var (w, h) = Size(it); return it.Rot ? (h, w) : (w, h); }

        static bool Inside(int cols, int rows, int x, int y, int w, int h) => x >= 0 && y >= 0 && x + w <= cols && y + h <= rows;
        /// <summary>Is the rectangle free of every other placed item? (cols x rows and the items: your bag, or a body's grid)</summary>
        public static bool FreeIn(int cols, int rows, IEnumerable<InventoryItem> items, InventoryItem except, int x, int y, int w, int h)
        {
            if (!Inside(cols, rows, x, y, w, h)) return false;
            foreach (var o in items)
            {
                if (o == except || o.GridX < 0) continue;
                var (ow, oh) = Footprint(o);
                if (x < o.GridX + ow && o.GridX < x + w && y < o.GridY + oh && o.GridY < y + h) return false;
            }
            return true;
        }
        public static bool Free(Player p, InventoryItem except, int x, int y, int w, int h) => FreeIn(Bag(p).Cols, Bag(p).Rows, p.Inventory.Items, except, x, y, w, h);

        /// <summary>The first free spot (rows top to bottom, left to right), flat then turned. False: no room, it stays out.</summary>
        public static bool PlaceIn(int cols, int rows, IEnumerable<InventoryItem> items, InventoryItem it)
        {
            foreach (bool rot in new[] { false, true })
            {
                it.Rot = rot; var (w, h) = Footprint(it);
                for (int y = 0; y + h <= rows; y++)
                    for (int x = 0; x + w <= cols; x++)
                        if (FreeIn(cols, rows, items, it, x, y, w, h)) { it.GridX = x; it.GridY = y; return true; }
            }
            it.Rot = false; it.GridX = -1;
            return false;
        }
        public static bool Place(Player p, InventoryItem it) => PlaceIn(Bag(p).Cols, Bag(p).Rows, p.Inventory.Items, it);

        /// <summary>Drag and drop: put it at (x, y), turned or not, if that space is free.</summary>
        public static bool MoveIn(int cols, int rows, IEnumerable<InventoryItem> items, InventoryItem it, int x, int y, bool rot)
        {
            bool was = it.Rot; it.Rot = rot; var (w, h) = Footprint(it);
            if (!FreeIn(cols, rows, items, it, x, y, w, h)) { it.Rot = was; return false; }
            it.GridX = x; it.GridY = y; return true;
        }
        public static bool Move(Player p, InventoryItem it, int x, int y, bool rot) => MoveIn(Bag(p).Cols, Bag(p).Rows, p.Inventory.Items, it, x, y, rot);

        // ---- a body's grid (HudView.Pack.cs, looting): 8 wide, as tall as its things need (and a few rows spare to put something on it) ----
        public const int BodyCols = 8;
        public static int BodyRows(IEnumerable<InventoryItem> items) { int area = 0; foreach (var i in items) { var (w, h) = Size(i); area += w * h; } return System.Math.Min(12, System.Math.Max(4, (int)System.Math.Ceiling(area * 1.4 / BodyCols) + 2)); }
        /// <summary>Lays everything on a body out in its grid (biggest first). Whatever cannot fit stays at -1 (the harness checks nothing is lost).</summary>
        public static List<InventoryItem> LayOut(List<InventoryItem> items, int rows)
        {
            foreach (var it in items) { it.GridX = -1; it.Rot = false; }
            var left = new List<InventoryItem>();
            foreach (var it in items.OrderByDescending(i => Size(i).w * Size(i).h).ToList()) if (!PlaceIn(BodyCols, rows, items, it)) left.Add(it);
            return left;
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
