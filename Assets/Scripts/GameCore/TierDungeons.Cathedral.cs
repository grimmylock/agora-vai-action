// The Abandoned Cathedral (2026-11-20, MISSION_CHOIR.md): the plan is TierDungeons.CathedralPlan (ArtSource/plans/cathedral_plan.py). Marks: E the front door (exit to the Abandoned City), B the bell rope, A the altar, P / Q the
// transept doors, R the sacristy, U the crypt niche and V the roost (blood-lured mutants, DungeonLairs), F G H J K L M O the eight windows Husks climb through.
using System.Collections.Generic;
using GameData;
using static GameCore.GroundType;

namespace GameCore
{
    public static partial class TierDungeons
    {
        public static MapLayout Cathedral()
        {
            var marks = new Dictionary<char, (string, string)>
            {
                ['E'] = ("Cathedral Doors", "abandoned_city"), ['B'] = ("Bell Rope", null), ['A'] = ("Altar", null), ['P'] = ("West Door", null), ['Q'] = ("East Door", null), ['R'] = ("Sacristy", null),
                ['U'] = ("Crypt Niche", null), ['V'] = ("Roost", null),
                ['F'] = ("Window 1", null), ['G'] = ("Window 2", null), ['H'] = ("Window 3", null), ['J'] = ("Window 4", null), ['K'] = ("Window 5", null), ['L'] = ("Window 6", null), ['M'] = ("Window 7", null), ['O'] = ("Window 8", null),
            };
            return FloorPlan("abandoned_cathedral", CathedralPlan, "concrete", Concrete, marks, null);
        }
    }
}
