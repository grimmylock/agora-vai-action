// Batch 91 (Yan: "this is a map problem, not a gun problem ... design dungeons and these other 'raid' style and basically all
// buildings in a way that rewards positioning, if you have a sniper, by all means hold the longest range as possible, if you
// have a shotgun or something else, play the rooms and corridors smartly. Labs and building insides need to have more
// deviations and paths, rooms, basically be modeled like real buildings and insipired by the stalker labs"; "yes start with
// the lattice foundry prototype"). Floor plans drawn as text at full size, one character a tile, and a reader that turns them
// into a map. The plans are made by ArtSource/plans/foundry_plans.py (rooms, corridors, rings, doors), which also checks
// that every floor tile can be reached.
// Real-building patterns used (Cold War bunkers and research institutes): a blast-door entrance with a decontamination lock,
// rooms off a long spine corridor, a parallel service corridor so there's always a second way round, a control room opening
// onto several rooms, an escape duct, plant rooms, several levels joined by a stair.
using System.Collections.Generic;
using GameData;
using static GameCore.TileType;
using static GameCore.GroundType;

namespace GameCore
{
    public static partial class TierDungeons
    {
        static readonly Dictionary<char, string> Decals = new Dictionary<char, string>
        {
            ['*'] = "blood", ['\''] = "splat", ['~'] = "bloodtrail", ['%'] = "grime", ['`'] = "scorch", ['"'] = "papers", [':'] = "glass", [';'] = "oil", ['&'] = "bones",
        };

        /// <summary>One character a tile: '#' wall, '.' floor, ',' concrete floor, 'D' door, 'h' concrete block (hard cover),
        /// 'm' machine (hard), 'l' lockers (hard), 'd' desk (light), 'c' crate (light), 'p' pipes on the floor; an uppercase
        /// letter is floor with the landmark `marks` names for it (and its exit, if it has one).
        /// Batch 92 (the mess): 'x' debris (hard), 'b' rust barrel and 't' trash (light), 'r' rubble and 'w' flooded floor (slow),
        /// 'i' a ceiling light and 'e' a red emergency light (LightMap), and floor decals: '*' blood, '\'' splat, '~' blood trail, '%' grime, '`' scorch,
        /// '"' papers, ':' glass, ';' oil, '&' bones.</summary>
        static MapLayout FloorPlan(string id, string[] rows, string wallLook, GroundType floor,
            Dictionary<char, (string name, string exit)> marks, Dictionary<char, string> stashes = null, GroundType? under = null)
        {
            var c = new TerrainCanvas(rows[0].Length, rows.Length, TerrainCanvas.Seed(id));
            if (under != null) c.Paint(0, 0, rows[0].Length, rows.Length, under.Value);   // Batch 120: what shows round an upper floor's walls
            StampPlan(c, 0, 0, rows, wallLook, floor, marks, stashes);
            return Finish(c, wallLook);
        }

        /// <summary>Batch 92: draws a plan onto a canvas at (x0, y0) - a whole dungeon, or a building on a tier map
        /// (the Siege Ward Hospital). 'n' is a window in a wall, 's' sandbags (hard cover).</summary>
        public static void StampPlan(TerrainCanvas c, int x0, int y0, string[] rows, string wallLook, GroundType floor,
            Dictionary<char, (string name, string exit)> marks = null, Dictionary<char, string> stashes = null)
        {
            for (int py = 0; py < rows.Length; py++)
                for (int px = 0; px < rows[py].Length; px++)
                {
                    char ch = rows[py][px]; int x = x0 + px, y = y0 + py;
                    switch (ch)
                    {
                        case '#': c.Put(x, y, Solid, wallLook); continue;
                        case '_': c.Put(x, y, Empty); c.Paint(x, y, 1, 1, Dirt); continue;    // Batch 120: outside a non-rectangular footprint
                        case 'n': c.Put(x, y, Solid, "window"); continue;
                        case 's': c.Put(x, y, HardCover, "sandbags"); break;
                        case ',': c.Put(x, y, Empty); c.Paint(x, y, 1, 1, Concrete); continue;
                        case 'D': c.Put(x, y, Door); c.Paint(x, y, 1, 1, floor); continue;
                        case 'h': c.Put(x, y, HardCover, "block"); break;
                        case 'm': c.Put(x, y, HardCover, "machine"); break;
                        case 'l': c.Put(x, y, HardCover, "lockers"); break;
                        case 'd': c.Put(x, y, ScatterCover, "desk"); break;
                        case 'c': c.Put(x, y, ScatterCover, "crate"); break;
                        case 'p': c.Put(x, y, Empty); c.Deco(x, y, "floorpipe"); break;   // Batch 99: on the floor, not the overhead line
                        case 'x': c.Put(x, y, HardCover, "debris"); break;
                        case 'b': c.Put(x, y, ScatterCover, "rustbarrel"); break;
                        case 't': c.Put(x, y, ScatterCover, "trash"); break;
                        case 'r': c.Put(x, y, Difficult, "rubble"); break;
                        case 'w': c.Put(x, y, Difficult); c.Paint(x, y, 1, 1, Water); continue;
                        case 'i': c.Put(x, y, Empty); c.Deco(x, y, "light"); break;
                        case 'e': c.Put(x, y, Empty); c.Deco(x, y, "redlight"); break;
                        case 'k': c.Put(x, y, HardCover, "rock"); break;          // Batch 100: a boulder
                        case 'v': c.Put(x, y, Difficult, "roots"); break;         // hanging roots: slow going
                        case '*': case '\'': case '~': case '%': case '`': case '"': case ':': case ';': case '&':
                            c.Put(x, y, Empty); c.Deco(x, y, Decals[ch]); break;
                        default: c.Put(x, y, Empty); break;
                    }
                    c.Paint(x, y, 1, 1, floor);
                    if (marks != null && marks.TryGetValue(ch, out var mk))
                    {
                        c.Landmark(mk.name, x, y);
                        if (mk.exit != null) c.Exit(mk.exit, x, y);
                    }
                    if (stashes != null && stashes.TryGetValue(ch, out var stash)) c.Stash(stash, x, y);
                }
        }

        /// <summary>Batch 92: the hidden labs and bunkers as messy floor plans (TierDungeons.LabPlans.cs, from
        /// ArtSource/plans/lab_plans.py): 'E' is the way in (first landmark and the exit up), 'G' the goal room.</summary>
        /// <summary>Batch 120: a building's floor above the ground floor (house_plans.py), its own map like a cave. 'S' is the
        /// stair down (to `down`), 'U' the stair up (to `up`, if any), 'T' a stash.</summary>
        public static MapLayout Upstairs(string id, string wallLook, string down, string up = null) =>
            FloorPlan(id, Pad(LabPlans[id], 3), wallLook, Floor,
                up == null ? new Dictionary<char, (string, string)> { ['S'] = ("Stairs Down", down) }
                           : new Dictionary<char, (string, string)> { ['S'] = ("Stairs Down", down), ['U'] = ("Stairs Up", up) },
                new Dictionary<char, string> { ['T'] = "Forgotten Things" }, Concrete);

        // a margin of solid wall round a plan, drawn dark like a lab's rock (walls on the map's edge drew no face)
        static string[] Pad(string[] rows, int k)
        {
            string edge = new string('#', rows[0].Length + 2 * k), side = new string('#', k);
            var o = new List<string>();
            for (int i = 0; i < k; i++) o.Add(edge);
            foreach (var r in rows) o.Add(side + r + side);
            for (int i = 0; i < k; i++) o.Add(edge);
            return o.ToArray();
        }

        /// <summary>Batch 120: stamps a building's ground floor onto a tier map: its stair 'S' goes up to `up`.</summary>
        public static void StampHouse(TerrainCanvas c, int x, int y, string plan, string wallLook, string upName, string up)
        {
            var rows = LabPlans[plan];
            StampPlan(c, x, y, rows, wallLook, Floor, new Dictionary<char, (string, string)> { ['S'] = (upName, up) });
            c.Keep(x, y, rows[0].Length, rows.Length);
        }

        public static MapLayout Lab(string id, string wallLook, GroundType floor, string first, string second, string parent) =>
            FloorPlan(id, LabPlans[id], wallLook, floor,
                new Dictionary<char, (string, string)> { ['E'] = (first, parent), ['G'] = (second, null) });

        // ------------------------------------------------------------------ the Lattice Foundry (Tier 5, under the Ship Graveyard)

        /// <summary>Upper floor, the test level: the lift lobby, a two-chamber decon lock, the west checkpoint hall, a row of
        /// offices between the spine and a service corridor (doors both ways, and into each other), the observation room over
        /// the Firing Range (a 43-tile lane with concrete blocks: the long-gun ground), the storage room, and the lift's
        /// maintenance duct that comes out at the range's back door (the close-quarters way round). The Service Stair goes down.</summary>
        public static MapLayout LatticeUpper() => FloorPlan("lattice_foundry", FoundryUpperPlan, "concrete", LabTile,
            new Dictionary<char, (string, string)>
            {
                ['L'] = ("Foundry Lift", "ship_graveyard"), ['K'] = ("Decon Lock", null), ['O'] = ("Records Office", null),
                ['B'] = ("Observation Room", null), ['R'] = ("Firing Range", null), ['S'] = ("Service Stair", "lattice_foundry_lower"),
            },
            new Dictionary<char, string> { ['T'] = "Foundry Storage" });

        /// <summary>Lower floor, the synthesis level: the stair landing, a ring corridor round the Coil Hall (four doors in;
        /// the Cell Synthesizer at its heart, ringed by machines), the long cable tunnel along the north wall, the crew
        /// corridor with its bunk rooms, the Foreman's office (a door to the ring and one to the crew corridor), and the plant
        /// room under the ring, joined to both the crew corridor and the stair.</summary>
        public static MapLayout LatticeLower() => FloorPlan("lattice_foundry_lower", FoundryLowerPlan, "concrete", LabTile,
            new Dictionary<char, (string, string)>
            {
                ['S'] = ("Service Stair", "lattice_foundry"), ['Y'] = ("Cell Synthesizer", null), ['H'] = ("Coil Hall", null),
                ['Q'] = ("Crew Quarters", null), ['F'] = ("Foreman's Office", null), ['P'] = ("Plant Room", null),
            });

        static readonly string[] FoundryUpperPlan =
        {
            "##########################################################################",
            "##########################################################################",
            "######################...%......%....%...t........%.%..............t.#####",
            "#####...##############...###D########################D############D#######",
            "######..%..###########;..#.%...d.#.....############......###.%%.....######",
            "#####%...%.######%,,,#...#.......#%......d........#.....\"##...%i..%.%#####",
            "####..i.....,''`#,e,,D...#d..O...D.....l#x..l.\"...#....d.##..........#####",
            "####...L...D&*,,#,K',#...#......d#..d...rr........#..\"...##.....S....#####",
            "####.......#',,,D'&*,#..%##......#.%...d#........#r.....\".#%.........#####",
            "####.......#,,,,#',,,#.~.###.%%.##%.d...##%l..%.###......##...........####",
            "####.......###########;~%######.####.#######.###D##########.....%..%.#####",
            "####%....;.###########...#####D#####.#D#########.####D########D###########",
            "#######D##############.~........%.......%.######%#.......;%...:.%....#####",
            "#######.##############%~......i..............%.........%..........e..#####",
            "#######~##############%.........%...%..................%.........%...#####",
            "#######~##############.~..#D#################D#####################D######",
            "#######~##############%...#.############,,,,,i;,%,,################.######",
            "#######%##############.~.%#.##########,,,,d,:B:,d,,################.######",
            "#######~####......%###.~..#.############:%,,,,:,,,,################.######",
            "#######~###cc.....%###~.%##.###########r###D##############.%....###.######",
            "#######~##.......b.###~..#........%%..x%r.....%rrr.......%.......;..b#####",
            "#######~##%...T'...~~D...#&....i..............rxrr.\"......i..........#####",
            "#######e##.....&*~~......#......%......h.......rxx...........h....%..#####",
            "#######~##..%..%.c.###...#;...h......%.............h...'..'&........%#####",
            "#######~##.b....b..###%..#....b............%..%R........*&...........#####",
            "#######.##.%....%.####...#..............\"\"............'..............#####",
            "#######~######D.######..i#..........'.......h...................h....#####",
            "#######~######.#######...#.......h.*&...............\"..:.....wwwww&.;#####",
            "#######.######.#######...#.'.....'.'.......\"............h\"...wwwww...#####",
            "#######.######.#######...#......\"......;....%....&..b........wwww....#####",
            "#######.######%#######...#.......%.############D##########################",
            "#######.######.#####..............#############.##########################",
            "#######.~p.~p.~p~.p*&############p..p..pe.p..p..##########################",
            "####################.#####################################################",
            "##########################################################################",
            "##########################################################################",
        };

        static readonly string[] FoundryLowerPlan =
        {
            "##########################################################################",
            "##########################################################################",
            "##########################################################################",
            "#####%......%....%...;..e...%..#############.......rx......%....%...%#####",
            "#####.p.%.....p...p..;p.%.p%..p...p...p...p...p...p.r.p%.%e.%;p%%.p..#####",
            "#########..###################.....%.........#####################;#######",
            "#########..#########################..############################.#######",
            "#########..###..l..#################..############################.#######",
            "#########;.##......#################..############################.#######",
            "#########..D...Q....################..############################.#######",
            "#########~;#..l.\".##################..############################.#######",
            "#########%.#..\"..################.......##########################.#######",
            "#########.%##.#r###############...........########################.#######",
            "#########..##.r.....#########.....%.........######################.#######",
            "#########..#......l.########.....###.###%...%#####################.#######",
            "#########~.Dl.......#######....#####D#####....####################.#######",
            "#########&.#.t\".....######....####.....####..ww###################%#######",
            "#######....#........#####....###.'.'.....###%.ww##################;#######",
            "#######...e#.%t.....#####...###...&*......###www##################.#######",
            "#######...%#############..%###%..`'.'..H..%###...#################D#######",
            "#######....##.......####%..##.......m.......##...############..%...#######",
            "#########..#..d\".....##...###....m...%.m....###..%###########.......######",
            "#########..#..\"\"..cd..#..%##....'.i.....*&%..##...###########..i.....#####",
            "#########..#;.\".xi....#...##%.`.%.....'.%.\"..##;:%###########...S....#####",
            "#########..D...:F:....#....D....m...Y....m...##.............D........#####",
            "#########..#......~~~~D...##..\"...:..%.......r.............%%........#####",
            "#########..#....*~'%.;#...##....&*..........r##%..###.....####.......#####",
            "#########..#..l..'...##..%###%...m''..i..'..###..%#############..%..%#####",
            "#########;.##.......####...##%...`.....m'.*&##.~.###############D#########",
            "#########..#############...###......m...'.'###.%~###############.#########",
            "#########..##############%..###........`..###...################.#########",
            "#########..##############...;###.%.'.....###...~################.#########",
            "#########..###############....####.....####....#################.#########",
            "#########..###.t###########....#####D#####..~.##################.#########",
            "#########..##..\".###########.%...###.###...~.###################.#########",
            "#########..Dc.....###########....xrr:....&*'####################.#########",
            "#########~;#.....##############..rxre....%######################%#########",
            "#########..#....#################rrx...%########################.#########",
            "#########%.#########################.###########################.#########",
            "#########..#########,,,,,,,,,,,,,,,%,,,,,,,,%%,,%,##############.#########",
            "#########&.########,,,,,,,,,,,,,\",,,,,,,,,,wwwwww\"%#############.#########",
            "#########..........D,,,m,ww,m,,m,,,,,,m,,,,,mwwwm,,,############.#########",
            "#########;.#########,,,,,www,,,,,,P,%,,%,,,,ewww,,,,D..%%.......%#########",
            "####################,,p,p,p,p,p,p,p,p,p,p,p,pwp,p,,%######################",
            "####################,,,,,%,,,,%,,,,,,,,,,,,,,,,,,,,%######################",
            "##########################################################################",
            "##########################################################################",
            "##########################################################################",
        };
    }
}
