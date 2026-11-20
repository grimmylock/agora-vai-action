// The Abandoned Cathedral (2026-11-20, MISSION_CHOIR.md): the plan is TierDungeons.CathedralPlan (ArtSource/plans/cathedral_plan.py). Marks: E the front door (exit to the Abandoned City), B the bell rope, A the altar, P / Q the
// transept doors, R the sacristy, U the crypt niche and V the roost (blood-lured mutants, DungeonLairs), F G H J K L M O the eight windows Husks climb through.
using System.Collections.Generic;
using GameData;
using static GameCore.GroundType;
using static GameCore.TileType;

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
            // the cathedral's own legend (3 4 5 = the left end, middle and right end of a pew, 6 a column, 7 the altar, 8 a candelabra, 9 a statue, e a tomb, y a font, z a confessional) is stamped on top of the common plan
            const string special = "3456789eyz";
            var plain = new string[CathedralPlan.Length];
            for (int i = 0; i < plain.Length; i++) { var cs = CathedralPlan[i].ToCharArray(); for (int j = 0; j < cs.Length; j++) if (special.IndexOf(cs[j]) >= 0) cs[j] = '.'; plain[i] = new string(cs); }
            var c = new TerrainCanvas(CathedralPlan[0].Length, CathedralPlan.Length, TerrainCanvas.Seed("abandoned_cathedral"));
            StampPlan(c, 0, 0, plain, "gothic", LabTile, marks, null);
            for (int y = 0; y < CathedralPlan.Length; y++)
                for (int x = 0; x < CathedralPlan[y].Length; x++)
                    switch (CathedralPlan[y][x])
                    {
                        case '3': c.Put(x, y, ScatterCover, "pew_l"); break;
                        case '4': c.Put(x, y, ScatterCover, "pew_m"); break;
                        case '5': c.Put(x, y, ScatterCover, "pew_r"); break;
                        case '6': c.Put(x, y, HardCover, "column"); break;
                        case '7': c.Put(x, y, HardCover, "altar"); break;
                        case '8': c.Put(x, y, ScatterCover, "candelabra"); break;
                        case '9': c.Put(x, y, HardCover, "statue"); break;
                        case 'e': c.Put(x, y, HardCover, "tomb"); break;
                        case 'y': c.Put(x, y, ScatterCover, "font"); break;
                        case 'z': c.Put(x, y, HardCover, "confessional"); break;
                    }
            return Finish(c, "gothic");
        }
    }
}
