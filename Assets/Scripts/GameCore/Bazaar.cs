// 2026-11-25 (Yan: "change the stalls to only 4, one of each color: armor, weapons, medical items, and a black market with higher tier items up to tier 4 but very expensive, like 100% more").
// The Bazaar in the Scavenger's Hub: four stalls against the top wall of the NW warehouse. Each is its own vendor: B next to a stall opens that vendor's shop (ShopUI), B anywhere else in the hub opens the general shop as before.
using System.Collections.Generic;
using System.Linq;
using GameData;

namespace GameCore
{
    public static class Bazaar
    {
        public const string Zone = "scavengers_hub";
        public const float BlackMarkup = 2f;             // the black market charges 100% more
        public const int ReachTiles = 3;
        public class Stall { public string Kind, Name, Look; public int X, Y; }
        public static readonly Stall[] Stalls =
        {
            new Stall { Kind = "armor",   Name = "Armour Stall",  Look = "stall_blue",  X = 71, Y = 146 },
            new Stall { Kind = "weapons", Name = "Arms Stall",    Look = "stall_red",   X = 75, Y = 146 },
            new Stall { Kind = "medical", Name = "Medic Stall",   Look = "stall_green", X = 81, Y = 147 },
            new Stall { Kind = "black",   Name = "Black Market",  Look = "stall_ochre", X = 87, Y = 147 },
        };
        static readonly HashSet<string> MedicalIds = new HashSet<string> { "basicMedkit", "basicStim", "splint", "bandage", "radBlockPill" };

        /// <summary>The stall within reach of this player in the hub, or null.</summary>
        public static Stall Near(Player p)
        {
            if (p == null || MacroState.Current.CurrentZoneId != Zone) return null;
            return Stalls.FirstOrDefault(s => CombatantUtil.Distance(p, s.X, s.Y) <= ReachTiles);
        }

        public static Stall Find(string kind) => Stalls.FirstOrDefault(s => s.Kind == kind);

        static bool Gear(ShopItemDef i, string type, int lo, int hi) => i.Type == type && !i.FactionIssue && (i.Tier ?? 1) >= lo && (i.Tier ?? 1) <= hi;

        /// <summary>What a vendor always has on the shelf (the black market also gets a rolled tier-4 stock, ShopUI).</summary>
        public static List<ShopItemDef> Stock(string kind, Player p)
        {
            var all = ShopDatabase.Items;
            switch (kind)
            {
                case "weapons": { var l = all.Where(i => Gear(i, "weapon", 1, 3)).ToList(); l.AddRange(ShopSystem.AmmoStock(p, 3)); return l; }
                case "armor": return all.Where(i => Gear(i, "armor", 1, 3)).ToList();
                case "medical": return all.Where(i => i.Type == "consumable" && (MedicalIds.Contains(i.Id) || i.Id.IndexOf("edkit", System.StringComparison.OrdinalIgnoreCase) >= 0 || i.Id.IndexOf("stim", System.StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
                case "black": return all.Where(i => Gear(i, "weapon", 3, 4) || Gear(i, "armor", 3, 4)).ToList();
            }
            return new List<ShopItemDef>();
        }
    }
}
