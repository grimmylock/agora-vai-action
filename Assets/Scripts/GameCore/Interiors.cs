// 2026-11-24 (Yan: buildings should feel lived in, furnished for what they are; ArtSource/BUILDINGS_PLAN.md): interior plans that carry a FLOOR LAYER next to the structure layer, so every room has its own floor (tile in a kitchen and a bathroom, wood in
// a bedroom, concrete in a shop and a barracks), and furniture glyphs for the new Pixel Lab props. Three TEST buildings (a house, a shop, a barracks) and a dev-only map to look at them (`BUILDING_LAB=1` in ZoneLayoutRecipes, or the render tool).
// Structure layer: '#' wall, 'n' window, 'D' door, '.' floor; furniture: b bed, e cot, w wardrobe, k sink counter, s stove, f fridge, T dining table + chairs, o sofa, a armchair, z toilet, u bathtub, v washbasin, l bookshelf, g goods shelf,
// C shop counter, K bunk bed, F footlocker, R weapon rack, M map table, d desk, c crate, h lockers, i nightstand, p plant, y floor lamp, q kitchen counter, N notice board, X box stack, B bench. Floor layer: 'w' wood (Floor), 't' lab tile (LabTile), 'k' white kitchen / bathroom tile (KitchenTile), 'c' concrete, 'd' dirt, 'g' gravel, 's' sand, '.' leave the ground as it is.
using System.Collections.Generic;
using GameData;
using static GameCore.TileType;

namespace GameCore
{
    public static partial class Interiors
    {
        /// <summary>2026-11-27: every Stamp call (canvas seed, plan, where): the harness checks after the map is fully built that every wall, window and door of a plan is still there (a road or clear after the stamp can erase them).</summary>
        public static readonly List<(int seed, string[] rows, int x0, int y0)> Stamped = new List<(int, string[], int, int)>();
        /// <summary>Every public plan array X that has a floor array XFloor (generated and hand-written ones), for the harness.</summary>
        public static IEnumerable<(string name, string[] rows, string[] floors)> AllPlans()
        {
            foreach (var f in typeof(Interiors).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                if (f.FieldType == typeof(string[]) && !f.Name.EndsWith("Floor") && typeof(Interiors).GetField(f.Name + "Floor") != null)
                    yield return (f.Name, (string[])f.GetValue(null), (string[])typeof(Interiors).GetField(f.Name + "Floor").GetValue(null));
        }
        // glyph -> (look, tall/hard cover?)
        static readonly Dictionary<char, (string look, bool hard)> Pieces = new Dictionary<char, (string, bool)>
        {
            ['b'] = ("bed", false), ['e'] = ("cot", false), ['w'] = ("wardrobe", true), ['k'] = ("kitchensink", false), ['s'] = ("stove", true), ['f'] = ("fridge", true), ['T'] = ("diningset", false),
            ['o'] = ("sofa", false), ['a'] = ("armchair", false), ['z'] = ("toilet", false), ['u'] = ("bathtub", false), ['v'] = ("washbasin", false), ['l'] = ("bookshelf", true), ['g'] = ("goodsshelf", true),
            ['C'] = ("shopcounter", false), ['K'] = ("bunkbed", true), ['F'] = ("footlocker", false), ['R'] = ("weaponrack", true), ['M'] = ("maptable", false), ['d'] = ("desk", false), ['c'] = ("crate", false), ['h'] = ("lockers", true),
            ['i'] = ("nightstand", false), ['p'] = ("plant", false), ['y'] = ("floorlamp", false), ['q'] = ("kitchencounter", false), ['N'] = ("noticeboard", false), ['X'] = ("boxstack", true), ['B'] = ("bench", false),
            ['S'] = ("stool", false), ['H'] = ("medcabinet", true), ['P'] = ("bottleshelf", true), ['V'] = ("barcounter", false), ['m'] = ("machine", true), ['r'] = ("rustbarrel", false), ['Q'] = ("pooltable", false), ['J'] = ("jukebox", true), ['U'] = ("cardtable", false), ['G'] = ("kegs", true), ['L'] = ("barcounter_e", false), ['Z'] = ("barcounter_w", false), ['Y'] = ("barcounter_n", false), ['A'] = ("sofa_e", false), ['I'] = ("sofa_w", false), ['t'] = ("bench_e", false), ['j'] = ("bench_w", false), ['O'] = ("jukebox_w", true), ['E'] = ("bottleshelf_e", true), ['W'] = ("bottleshelf_w", true), ['1'] = ("bunkbed_e", true), ['2'] = ("bunkbed_w", true), ['3'] = ("cot_e", false), ['4'] = ("cot_w", false), ['5'] = ("goodsshelf_e", true), ['6'] = ("goodsshelf_w", true), ['7'] = ("workbench", false), ['8'] = ("haybale", true), ['9'] = ("trough", false), ['0'] = ("radiotable", false), ['$'] = ("cooler", true), ['%'] = ("snackrack", false), ['x'] = ("debris", false), ['*'] = ("trash", false), ['+'] = ("rubble", false), ['&'] = ("netrack", false), ['!'] = ("fishcrates", false), ['='] = ("dryingrack", true), ['^'] = ("crabpots", false), [';'] = ("cleantable", false), ['['] = ("truckstopsign", true), [']'] = ("carlift", true), ['{'] = ("forge", true), ['}'] = ("scrapthrone", true), ['<'] = ("doghouse", false), ['('] = ("tires", false), ['>'] = ("pew_l", false), ['?'] = ("pew_m", false), ['~'] = ("pew_r", false), [':'] = ("altar", true), [','] = ("font", false), ['-'] = ("candelabra", false), ['_'] = ("confessional", true), ['|'] = ("logs", false),
            ['@'] = ("jerrycan", false),
            [')'] = ("escalator", false),
            ['/'] = ("liftdoors", true),
            ['§'] = ("displaycounter", false),
            ['£'] = ("officedesk", true),
            ['¥'] = ("stafftable", false),
            ['µ'] = ("escalatorseg", false),
            ['Æ'] = ("liftshaft", true),
            ['Ø'] = ("escalatorlong", true),
            ['¶'] = ("part", true),
            ['Þ'] = ("checkout", false),
            ['ß'] = ("mannequin", true),
            ['Ð'] = ("storesign", false),
            ['Å'] = ("fittingbooth", true),
            ['Ä'] = ("standmirror", false),
            ['Ö'] = ("clothesrack", true),
            ['Ü'] = ("storepost", true),
            ['É'] = ("stairs", true),
        };
        static GroundType FloorOf(char f) => f == 'k' ? GroundType.KitchenTile : f == 't' ? GroundType.LabTile : f == 'c' ? GroundType.Concrete : f == 'd' ? GroundType.Dirt : f == 'g' ? GroundType.Gravel : f == 's' ? GroundType.Sand : GroundType.Floor;

        /// <summary>Stamps a plan with its floor layer at (x0, y0). `wallLook` is the outer wall; inner walls use it too.</summary>
        public static void Stamp(TerrainCanvas c, int x0, int y0, string[] rows, string[] floors, string wallLook, string archetype = null)
        {
            Stamped.Add((c.SeedValue, rows, x0, y0));
            for (int py = 0; py < rows.Length; py++)
                for (int px = 0; px < rows[py].Length; px++)
                {
                    char ch = rows[py][px], fl = floors != null ? floors[py][px] : 'w'; int x = x0 + px, y = y0 + py;
                    if (ch == '#') { c.Put(x, y, Solid, wallLook); continue; }
                    if (ch == 'n') { c.Put(x, y, Solid, "window"); continue; }
                    if (fl != '.') c.Paint(x, y, 1, 1, FloorOf(fl));
                    if (ch == 'D') { c.Put(x, y, Door); continue; }
                    if (ch != '.' && Pieces.TryGetValue(ch, out var pc)) { c.Put(x, y, pc.hard ? HardCover : ScatterCover, pc.look); var an = ContainerLoot.AnchorFor(archetype, pc.look, x, y); if (an != null) c.Landmark(an, x, y); }
                    else c.Put(x, y, Empty);
                }
            c.Keep(x0 - 1, y0 - 1, rows[0].Length + 2, rows.Length + 2);
        }

        public static readonly string[] House = {
            "###n######n####n###",
            "#qkqsqf#z.v.#l...w#",
            "#......#....#.....#",
            "n......#....#i.b.in",
            "#q.T..p#..up#y...c#",
            "#......#....#.....#",
            "###D#####D#####D###",
            "#.................D",
            "####D########D#####",
            "#y.o...l..#.....w.#",
            "#.........#.eie...#",
            "nc.d..a..p#d...c.yn",
            "#.........#.......#",
            "###n##n#####n###n##",
        };
        public static readonly string[] HouseFloor = {
            "...................",
            ".kkkkkk.kkkk.wwwww.",
            ".kkkkkk.kkkk.wwwww.",
            ".kkkkkk.kkkk.wwwww.",
            ".kkkkkk.kkkk.wwwww.",
            ".kkkkkk.kkkk.wwwww.",
            "...................",
            ".wwwwwwwwwwwwwwwww.",
            "...................",
            ".wwwwwwwww.wwwwwww.",
            ".wwwwwwwww.wwwwwww.",
            ".wwwwwwwww.wwwwwww.",
            ".wwwwwwwww.wwwwwww.",
            "...................",
        };
        public static readonly string[] Shop = {
            "#######n##############",
            "#.g.g.g.g.g.g.f#.g.g.#",
            "#..............#.....#",
            "#c.............#.....#",
            "nc.............#.X.X.n",
            "#..g.g.g..g.g..#.....#",
            "#..............#....c#",
            "#..............D.....#",
            "#..............#.c...#",
            "n...C.C...N....#.....n",
            "#.X............#h..d.#",
            "#c..........Xc.#.X..cD",
            "#..............#.....#",
            "###n###D###n##########",
        };
        public static readonly string[] ShopFloor = {
            "......................",
            ".cccccccccccccc.ccccc.",
            ".cccccccccccccc.ccccc.",
            ".cccccccccccccc.ccccc.",
            ".cccccccccccccc.ccccc.",
            ".cccccccccccccc.ccccc.",
            ".cccccccccccccc.ccccc.",
            ".cccccccccccccc.ccccc.",
            ".cccccccccccccc.ccccc.",
            ".cccccccccccccc.ccccc.",
            ".cccccccccccccc.ccccc.",
            ".cccccccccccccc.ccccc.",
            ".cccccccccccccc.ccccc.",
            "......................",
        };
        public static readonly string[] Barracks = {
            "####n####n#######n####n###",
            "#h..........h#.R.N.......#",
            "#.KF..KF..KF.#........d..#",
            "n............#.....M.....n",
            "#.......y....#...........#",
            "#hKF..KF..KFh#.l.....y.FF#",
            "#............#...........#",
            "#######D#########D########",
            "#..................#.....#",
            "#N.............sqfq#R.R.R#",
            "#...T...T...T......#....h#",
            "n..................D.....n",
            "#.............B.kq.#.X..X#",
            "#.B...B...B........#F.F..D",
            "#..................#.....#",
            "######n###n##D##n#####n###",
        };
        public static readonly string[] BarracksFloor = {
            "..........................",
            ".wwwwwwwwwwww.ccccccccccc.",
            ".wwwwwwwwwwww.ccccccccccc.",
            ".wwwwwwwwwwww.ccccccccccc.",
            ".wwwwwwwwwwww.ccccccccccc.",
            ".wwwwwwwwwwww.ccccccccccc.",
            ".wwwwwwwwwwww.ccccccccccc.",
            ".......c.........c........",
            ".cccccccccccccccccc.ccccc.",
            ".cccccccccccccccccc.ccccc.",
            ".cccccccccccccccccc.ccccc.",
            ".cccccccccccccccccc.ccccc.",
            ".cccccccccccccccccc.ccccc.",
            ".cccccccccccccccccc.ccccc.",
            ".cccccccccccccccccc.ccccc.",
            "..........................",
        };


        public static readonly string[] Anchor = {
            "###n####n###n###",
            "#.P.P.P.P..rrs.#",
            "#..............n",
            "#.VVVVVV.....o.#",
            "n..S.S.S.......#",
            "#..............D",
            "#.STS..STS.STS.#",
            "n..............#",
            "#y.S....S...S..n",
            "#..............#",
            "#####n####n#####",
        };
        public static readonly string[] AnchorFloor = {
            "................",
            ".wwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwww.",
            "................",
        };
        public static readonly string[] BarracksHall = {
            "###n#######n###",
            "#.KF.KF.KF..RR#",
            "#.............#",
            "#h...........h#",
            "n.KF.KF.KF....n",
            "#...........M.#",
            "#.............#",
            "#.N..y.....T.X#",
            "#.............#",
            "###n###D###n###",
        };
        public static readonly string[] BarracksHallFloor = {
            "...............",
            ".wwwwwwwwwwwww.",
            ".wwwwwwwwwwwww.",
            ".wwwwwwwwwwwww.",
            ".wwwwwwwwwwwww.",
            ".wwwwwwwwwwwww.",
            ".wwwwwwwwwwwww.",
            ".wwwwwwwwwwwww.",
            ".wwwwwwwwwwwww.",
            "...............",
        };



        public static readonly string[] Cabin1 = {
            "###D##",
            "#ib.s#",
            "n...yn",
            "#....#",
            "######",
        };
        public static readonly string[] Cabin1Floor = {
            "......",
            ".wwww.",
            ".wwww.",
            ".wwww.",
            "......",
        };
        public static readonly string[] Cabin2 = {
            "###D###",
            "#bi.sw#",
            "nyT...n",
            "#.....#",
            "#######",
        };
        public static readonly string[] Cabin2Floor = {
            ".......",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".......",
        };
        public static readonly string[] Workshop = {
            "####D###",
            "#mc...h#",
            "#....m.#",
            "nXd....n",
            "#......#",
            "########",
        };
        public static readonly string[] WorkshopFloor = {
            "........",
            ".cccccc.",
            ".cccccc.",
            ".cccccc.",
            ".cccccc.",
            "........",
        };
        public static readonly string[] Tent = {
            "##n###",
            "#eF.e#",
            "#....D",
            "######",
        };
        public static readonly string[] TentFloor = {
            "......",
            ".dddd.",
            ".dddd.",
            "......",
        };
        public static readonly string[] Bar = {
            "####n###",
            "#PP...r#",
            "#..VV.rn",
            "D..SS..#",
            "#......#",
            "########",
        };
        public static readonly string[] BarFloor = {
            "........",
            ".wwwwww.",
            ".wwwwww.",
            ".wwwwww.",
            ".wwwwww.",
            "........",
        };
        public static readonly string[] BarracksLong = {
            "###n#####n####n###",
            "#.KF..K...KF..RRh#",
            "#................#",
            "#.....N.......M..n",
            "D........T.......#",
            "#...T......y.c..X#",
            "#................#",
            "#####n######n#####",
        };
        public static readonly string[] BarracksLongFloor = {
            "..................",
            ".cccccccccccccccc.",
            ".cccccccccccccccc.",
            ".cccccccccccccccc.",
            ".cccccccccccccccc.",
            ".cccccccccccccccc.",
            ".cccccccccccccccc.",
            "..................",
        };
        public static readonly string[] GuardE = {
            "##n###",
            "#RR.F#",
            "#d...D",
            "#....#",
            "###n##",
        };
        public static readonly string[] GuardEFloor = {
            "......",
            ".cccc.",
            ".cccc.",
            ".cccc.",
            "......",
        };
        public static readonly string[] GuardW = {
            "##n###",
            "#F.RR#",
            "D...d#",
            "#....#",
            "###n##",
        };
        public static readonly string[] GuardWFloor = {
            "......",
            ".cccc.",
            ".cccc.",
            ".cccc.",
            "......",
        };








        public static readonly string[] Bunkhouse = {
            "####n####n####",
            "#.KF..K..KF.c#",
            "#............#",
            "#............#",
            "n1F.....K...Fn",
            "#.........c..#",
            "#y...T...B..s#",
            "#............#",
            "###n###D###n##",
        };
        public static readonly string[] BunkhouseFloor = {
            "..............",
            ".wwwwwwwwwwww.",
            ".wwwwwwwwwwww.",
            ".wwwwwwwwwwww.",
            ".wwwwwwwwwwww.",
            ".wwwwwwwwwwww.",
            ".wwwwwwwwwwww.",
            ".wwwwwwwwwwww.",
            "..............",
        };
        public static readonly string[] Clinic = {
            "##n###n##",
            "#HH..k.f#",
            "#.......#",
            "n.e....4n",
            "#....e..#",
            "#p.....d#",
            "#.......#",
            "####D####",
        };
        public static readonly string[] ClinicFloor = {
            ".........",
            ".kkkkkkk.",
            ".kkkkkkk.",
            ".kkkkkkk.",
            ".kkkkkkk.",
            ".kkkkkkk.",
            ".kkkkkkk.",
            ".........",
        };
        public static readonly string[] ClinicSmall = {
            "###D####",
            "#HH...f#",
            "#...p..#",
            "n3.e..dn",
            "#......#",
            "########",
        };
        public static readonly string[] ClinicSmallFloor = {
            "........",
            ".kkkkkk.",
            ".kkkkkk.",
            ".kkkkkk.",
            ".kkkkkk.",
            "........",
        };
        public static readonly string[] BunkTall = {
            "###n###",
            "#1F.F2#",
            "#.....#",
            "ny....#",
            "#...F2n",
            "#1....#",
            "#.....#",
            "#....s#",
            "D.....#",
            "#..T..#",
            "#.....#",
            "#1F...#",
            "#...F2n",
            "n.....#",
            "#.c.F2#",
            "#.....#",
            "#.....#",
            "###n###",
        };
        public static readonly string[] BunkTallFloor = {
            ".......",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".......",
        };
        public static readonly string[] WarehouseS = {
            "####n#####D####n####",
            "#.gg.g.......gg..g.#",
            "#..................#",
            "#..................#",
            "#c................6#",
            "#Xc...c...........6#",
            "n..................#",
            "#............c.....#",
            "#X.................n",
            "#.c................#",
            "#......c...........#",
            "#.......c......Xc..#",
            "n...............X..#",
            "#.XX...............n",
            "#cc.........rr...c.#",
            "#..c.....m.......XX#",
            "#..................#",
            "####################",
        };
        public static readonly string[] WarehouseSFloor = {
            "....................",
            ".cccccccccccccccccc.",
            ".cccccccccccccccccc.",
            ".cccccccccccccccccc.",
            ".cccccccccccccccccc.",
            ".cccccccccccccccccc.",
            ".cccccccccccccccccc.",
            ".cccccccccccccccccc.",
            ".cccccccccccccccccc.",
            ".cccccccccccccccccc.",
            ".cccccccccccccccccc.",
            ".cccccccccccccccccc.",
            ".cccccccccccccccccc.",
            ".cccccccccccccccccc.",
            ".cccccccccccccccccc.",
            ".cccccccccccccccccc.",
            ".cccccccccccccccccc.",
            "....................",
        };
        public static readonly string[] WarehouseN = {
            "######################",
            "#.........N..........#",
            "#X.............p....X#",
            "#........c.c....Sr...#",
            "#.c......X..........X#",
            "#....r...............#",
            "#.......y.....c...cr.#",
            "#....................#",
            "#............y.......#",
            "#....................D",
            "#....................#",
            "#.GPP.P.r........N..O#",
            "#.......y......Q....p#",
            "#..VVVVVV....y......I#",
            "#..S.S.S.U........U..#",
            "#X.......c..........I#",
            "#....................#",
            "#....................#",
            "###########D##########",
        };
        public static readonly string[] WarehouseNFloor = {
            "......................",
            ".cccccccccccccccccccc.",
            ".cccccccccccccccccccc.",
            ".cccccccccccccccccccc.",
            ".cccccccccccccccccccc.",
            ".cccccccccccccccccccc.",
            ".cccccccccccccccccccc.",
            ".cccccccccccccccccccc.",
            ".cccccccccccccccccccc.",
            ".cccccccccccccccccccc.",
            ".cccccccccccccccccccc.",
            ".wwwwwwwwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwwwwwwwww.",
            "......................",
        };

        public static readonly string[] Barn = {
            "###n####n####n##",
            "#....#....#F.hR#",
            "n.8..#.8..#..7.n",
            "#8...#..8.#....#",
            "#.........##D###",
            "D..............D",
            "#...........cc.#",
            "#c.......r....Xn",
            "n..............#",
            "####n##D####n###",
        };
        public static readonly string[] BarnFloor = {
            "................",
            ".dddd.dddd.wwww.",
            ".dddd.dddd.wwww.",
            ".dddd.dddd.wwww.",
            ".ddddddddd..d...",
            ".dddddddddddddd.",
            ".dddddddddddddd.",
            ".dddddddddddddd.",
            ".dddddddddddddd.",
            "................",
        };
        public static readonly string[] Cowshed = {
            "###n######n#########n###",
            "#.9..9...9..9...9..9...#",
            "#....................8.#",
            "D..........cc.7.....r..#",
            "#8.....................#",
            "#......................#",
            "###n##D###n######D##n###",
        };
        public static readonly string[] CowshedFloor = {
            "........................",
            ".dddddddddddddddddddddd.",
            ".dddddddddddddddddddddd.",
            ".dddddddddddddddddddddd.",
            ".dddddddddddddddddddddd.",
            ".dddddddddddddddddddddd.",
            "........................",
        };
        public static readonly string[] CommandHut = {
            "####n####",
            "#.0..M.h#",
            "D.......#",
            "#y...Sd.n",
            "#.......#",
            "#####n###",
        };
        public static readonly string[] CommandHutFloor = {
            ".........",
            ".ccccccc.",
            ".ccccccc.",
            ".ccccccc.",
            ".ccccccc.",
            ".........",
        };
        public static readonly string[] FieldBarracks = {
            "###n##n##",
            "#.e.e.e.#",
            "n.......n",
            "#3.F.F.4#",
            "#.......#",
            "####D####",
        };
        public static readonly string[] FieldBarracksFloor = {
            ".........",
            ".ccccccc.",
            ".ccccccc.",
            ".ccccccc.",
            ".ccccccc.",
            ".........",
        };
        public static readonly string[] MessHut = {
            "###n##n#",
            "#ksq..f#",
            "#......#",
            "D..T.TXn",
            "#......#",
            "########",
        };
        public static readonly string[] MessHutFloor = {
            "........",
            ".cccccc.",
            ".cccccc.",
            ".cccccc.",
            ".cccccc.",
            "........",
        };
        public static readonly string[] Booth = {
            "####n###",
            "#.eN.dh#",
            "#......n",
            "D...S.s#",
            "#......#",
            "###n####",
        };
        public static readonly string[] BoothFloor = {
            "........",
            ".wwwwww.",
            ".wwwwww.",
            ".wwwwww.",
            ".wwwwww.",
            "........",
        };
        public static readonly string[] Sentry = {
            "##n##",
            "#R.F#",
            "#...D",
            "#####",
        };
        public static readonly string[] SentryFloor = {
            ".....",
            ".www.",
            ".www.",
            ".....",
        };

















        public static readonly string[] Station = {
            "###n###n#####n##",
            "#.$$$$$$.N.#v.z#",
            "#..........#...#",
            "#.gggg..CC%#D###",
            "#c.+.......D.7X#",
            "#...x..*.x.#.cX#",
            "#..........#...#",
            "##nn##D##nn##D##",
        };

        public static readonly string[] StationFloor = {
            "................",
            ".kkkkkkkkkk.ccc.",
            ".kkkkkkkkkk.ccc.",
            ".kkkkkkkkkk.c...",
            ".kkkkkkkkkk.ccc.",
            ".kkkkkkkkkk.ccc.",
            ".kkkkkkkkkk.ccc.",
            "................",
        };

        public static readonly string[] BoatShed = {
            "###n####n###",
            "#7X.......&#",
            "#..........#",
            "n^........!n",
            "#..........#",
            "#c........r#",
            "#..........#",
            "#####DD#####",
        };
        public static readonly string[] BoatShedFloor = {
            "............",
            ".dddddddddd.",
            ".dddddddddd.",
            ".dddddddddd.",
            ".dddddddddd.",
            ".dddddddddd.",
            ".dddddddddd.",
            "............",
        };
        public static readonly string[] FisherHomeA = {
            "##n#n##",
            "#bi..s#",
            "n.....n",
            "#..T.&#",
            "#.....#",
            "###D###",
        };
        public static readonly string[] FisherHomeAFloor = {
            ".......",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".......",
        };
        public static readonly string[] FisherHomeB = {
            "##n#n##",
            "#s..ib#",
            "n.....n",
            "#^.T..#",
            "#.....#",
            "###D###",
        };
        public static readonly string[] FisherHomeBFloor = {
            ".......",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".......",
        };
        public static readonly string[] Smokehouse = {
            "###n####",
            "#s.=..r#",
            "#......n",
            "#!....c#",
            "#......#",
            "###D####",
        };
        public static readonly string[] SmokehouseFloor = {
            "........",
            ".dddddd.",
            ".dddddd.",
            ".dddddd.",
            ".dddddd.",
            "........",
        };
        public static readonly string[] Headman = {
            "###n##n##",
            "#bi.M..s#",
            "#.......n",
            "#w.T.T.&#",
            "D.......#",
            "#.......#",
            "#.......#",
            "###n##n##",
        };
        public static readonly string[] HeadmanFloor = {
            ".........",
            ".wwwwwww.",
            ".wwwwwww.",
            ".wwwwwww.",
            ".wwwwwww.",
            ".wwwwwww.",
            ".wwwwwww.",
            ".........",
        };
        public static readonly string[] Shack = {
            "######",
            "#b..s#",
            "n...^n",
            "#....#",
            "###D##",
        };
        public static readonly string[] ShackFloor = {
            "......",
            ".wwww.",
            ".wwww.",
            ".wwww.",
            "......",
        };

        public static readonly string[] Diner = {
            "###n####n#####n###n###",
            "#ksqqf......N....J.p.#",
            "#.............*......#",
            "n.VVVVVVVV.........+.n",
            "#..S.S..S....T...T...#",
            "#......x.............#",
            "#....................#",
            "##nn#nn#nn#Dnn#nn#nn##",
        };
        public static readonly string[] DinerFloor = {
            "......................",
            ".kkkkkkkkkkkkkkkkkkkk.",
            ".kkkkkkkkkkkkkkkkkkkk.",
            ".kkkkkkkkkkkkkkkkkkkk.",
            ".kkkkkkkkkkkkkkkkkkkk.",
            ".kkkkkkkkkkkkkkkkkkkk.",
            ".kkkkkkkkkkkkkkkkkkkk.",
            "......................",
        };
        public static readonly string[] Garage = {
            "###n#####n###",
            "#77.h.....{.#",
            "#...........#",
            "#...]......r#",
            "n(.........rn",
            "#(......x...#",
            "#.*.......c.#",
            "#...........#",
            "#####DDD#####",
        };
        public static readonly string[] GarageFloor = {
            ".............",
            ".ccccccccccc.",
            ".ccccccccccc.",
            ".ccccccccccc.",
            ".ccccccccccc.",
            ".ccccccccccc.",
            ".ccccccccccc.",
            ".ccccccccccc.",
            ".............",
        };
        public static readonly string[] KingHall = {
            "##n###n##",
            "#RR.}..F#",
            "#.......#",
            "nc...*.Xn",
            "#.y...y.#",
            "#.......#",
            "####D####",
        };
        public static readonly string[] KingHallFloor = {
            ".........",
            ".ccccccc.",
            ".ccccccc.",
            ".ccccccc.",
            ".ccccccc.",
            ".ccccccc.",
            ".........",
        };
        public static readonly string[] ScrapShack = {
            "###n###n#",
            "#{....7.#",
            "n.......#",
            "#cX..d.h#",
            "#.......#",
            "####D####",
        };
        public static readonly string[] ScrapShackFloor = {
            ".........",
            ".ccccccc.",
            ".ccccccc.",
            ".ccccccc.",
            ".ccccccc.",
            ".........",
        };

        public static readonly string[] CommandPost = {
            "###n##n####n##n###",
            "#hh.0..N..M..0.RR#",
            "#................#",
            "n....d...T..d....n",
            "#.y...........S.y#",
            "#................#",
            "#................#",
            "###n####DD###n####",
        };
        public static readonly string[] CommandPostFloor = {
            "..................",
            ".cccccccccccccccc.",
            ".cccccccccccccccc.",
            ".cccccccccccccccc.",
            ".cccccccccccccccc.",
            ".cccccccccccccccc.",
            ".cccccccccccccccc.",
            "..................",
        };
        public static readonly string[] Armoury = {
            "#####n######n#####",
            "#.RRRRR..XX.hhh.F#",
            "#................#",
            "#...CC........XX.n",
            "D................#",
            "#.........F.....c#",
            "#................#",
            "########n#####n###",
        };
        public static readonly string[] ArmouryFloor = {
            "..................",
            ".cccccccccccccccc.",
            ".cccccccccccccccc.",
            ".cccccccccccccccc.",
            ".cccccccccccccccc.",
            ".cccccccccccccccc.",
            ".cccccccccccccccc.",
            "..................",
        };
        public static readonly string[] MessHall = {
            "#####n######n#####",
            "#ksqqf...N......p#",
            "#................#",
            "#.VVVV...........n",
            "D.........T..T..T#",
            "#......x.........#",
            "#................#",
            "########n#####n###",
        };
        public static readonly string[] MessHallFloor = {
            "..................",
            ".kkkkkkkkkkkkkkkk.",
            ".kkkkkkkkkkkkkkkk.",
            ".kkkkkkkkkkkkkkkk.",
            ".kkkkkkkkkkkkkkkk.",
            ".kkkkkkkkkkkkkkkk.",
            ".kkkkkkkkkkkkkkkk.",
            "..................",
        };

        public static readonly string[] Church = {
            "####n###n####n###n####",
            "#.-.....-.::.-.....-.#",
            "#....................#",
            "#........p...........#",
            "n.>???~........>???~.n",
            "#....................#",
            "#.>???~........>???~.#",
            "#....................#",
            "n.>???~........>???~.n",
            "#....................#",
            "#.,................__#",
            "#....................#",
            "####n#####DD#####n####",
        };
        public static readonly string[] ChurchFloor = {
            "......................",
            ".wwwwwwwwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwwwwwwwww.",
            "......................",
        };
        public static readonly string[] CouncilHall = {
            "###n###n##n###n###",
            "#ll.N..d.M..N..ll#",
            "#y..............y#",
            "#.......T........#",
            "n................n",
            "#.BB.BB....BB.BB.#",
            "#................#",
            "#.BB.BB....BB.BB.#",
            "n................n",
            "#................#",
            "#................#",
            "###n####DD####n###",
        };
        public static readonly string[] CouncilHallFloor = {
            "..................",
            ".wwwwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwwwww.",
            "..................",
        };
        public static readonly string[] PharmacyShop = {
            "###n##n##",
            "#HHg.g.f#",
            "n.......#",
            "#.CC..x*#",
            "#.......#",
            "##nnDnn##",
        };
        public static readonly string[] PharmacyShopFloor = {
            ".........",
            ".kkkkkkk.",
            ".kkkkkkk.",
            ".kkkkkkk.",
            ".kkkkkkk.",
            ".........",
        };


        public static readonly string[] RowHouses = {
            "##nDn###nDn###nDn###nDn###nDn###nDn##",
            "#ks...#...sk#ks...#...sk#ks...#...sk#",
            "#.....#.....#.....#.....#.....#.....#",
            "#...T.#.T...#...T.#.T...#...T.#.T...#",
            "#.....#.....#.....#.....#.....#.....#",
            "###D#####D#####D#####D#####D#####D###",
            "#....+#+....#....+#+....#....+#+....#",
            "#A.*.+#+.*.I#A.*.+#+.*.I#A.*.+#+.*.I#",
            "#.....#.....#.....#.....#.....#.....#",
            "#hx..z#z..xh#hx..z#z..xh#hx..z#z..xh#",
            "#.....#.....#.....#.....#.....#.....#",
            "##nDn###nDn###nDn###nDn###nDn###nDn##",
        };

        public static readonly string[] RowHousesFloor = {
            ".....................................",
            ".kkkkk.kkkkk.kkkkk.kkkkk.kkkkk.kkkkk.",
            ".kkkkk.kkkkk.kkkkk.kkkkk.kkkkk.kkkkk.",
            ".kkkkk.kkkkk.kkkkk.kkkkk.kkkkk.kkkkk.",
            ".kkkkk.kkkkk.kkkkk.kkkkk.kkkkk.kkkkk.",
            ".....................................",
            ".wwwww.wwwww.wwwww.wwwww.wwwww.wwwww.",
            ".wwwww.wwwww.wwwww.wwwww.wwwww.wwwww.",
            ".wwwww.wwwww.wwwww.wwwww.wwwww.wwwww.",
            ".wwwww.wwwww.wwwww.wwwww.wwwww.wwwww.",
            ".wwwww.wwwww.wwwww.wwwww.wwwww.wwwww.",
            ".....................................",
        };

        public static readonly string[] CabinA = {
            "###n###",
            "#.K..s#",
            "#.....#",
            "n..T..#",
            "#....6#",
            "#c...|#",
            "#.....#",
            "###D###",
        };
        public static readonly string[] CabinAFloor = {
            ".......",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".......",
        };
        public static readonly string[] CabinB = {
            "###n###",
            "#s..K.#",
            "#.....#",
            "#..T.pn",
            "#6....#",
            "#|...c#",
            "#.....#",
            "###D###",
        };
        public static readonly string[] CabinBFloor = {
            ".......",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".......",
        };
        public static readonly string[] CabinHunter = {
            "###n###",
            "#KF..s#",
            "#.....#",
            "#...T.n",
            "#R...c#",
            "#*...|#",
            "#.....#",
            "###D###",
        };
        public static readonly string[] CabinHunterFloor = {
            ".......",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".......",
        };
        public static readonly string[] CabinFisher = {
            "##n####",
            "#bi..s#",
            "#.....#",
            "n..T..#",
            "#^...&#",
            "#....!#",
            "#.....#",
            "###D###",
        };
        public static readonly string[] CabinFisherFloor = {
            ".......",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".......",
        };
        public static readonly string[] CabinLogger = {
            "####n##",
            "#K.K.s#",
            "#.....#",
            "n..B..#",
            "#....|#",
            "#7...|#",
            "#.....#",
            "###D###",
        };
        public static readonly string[] CabinLoggerFloor = {
            ".......",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".wwwww.",
            ".......",
        };

        public static readonly string[] HutA = {
            "######D#",
            "#gggg..#",
            "n7.cc.r#",
            "#......#",
            "#####D##",
            "#N....h#",
            "#......#",
            "ndS...s#",
            "#......#",
            "##nD#n##",
        };
        public static readonly string[] HutAFloor = {
            "........",
            ".cccccc.",
            ".cccccc.",
            ".cccccc.",
            "........",
            ".wwwwww.",
            ".wwwwww.",
            ".wwwwww.",
            ".wwwwww.",
            "........",
        };
        public static readonly string[] HutB = {
            "#D######",
            "#..gggg#",
            "#r.cc.7n",
            "#......#",
            "##D#####",
            "#h....N#",
            "#......#",
            "#s...Sdn",
            "#......#",
            "##n#Dn##",
        };
        public static readonly string[] HutBFloor = {
            "........",
            ".cccccc.",
            ".cccccc.",
            ".cccccc.",
            "........",
            ".wwwwww.",
            ".wwwwww.",
            ".wwwwww.",
            ".wwwwww.",
            "........",
        };
        public static readonly string[] HutSignal = {
            "######D#",
            "#hhhh..#",
            "n7.c..r#",
            "#......#",
            "#####D##",
            "#00...h#",
            "#...N..#",
            "ndS...s#",
            "#......#",
            "##nD#n##",
        };
        public static readonly string[] HutSignalFloor = {
            "........",
            ".cccccc.",
            ".cccccc.",
            ".cccccc.",
            "........",
            ".wwwwww.",
            ".wwwwww.",
            ".wwwwww.",
            ".wwwwww.",
            "........",
        };
        public static readonly string[] HutLoading = {
            "######D#",
            "#ggXX..#",
            "nc..X.c#",
            "#......#",
            "#####D##",
            "#N....h#",
            "#..CC..#",
            "nd....s#",
            "#......#",
            "##nD#n##",
        };
        public static readonly string[] HutLoadingFloor = {
            "........",
            ".cccccc.",
            ".cccccc.",
            ".cccccc.",
            "........",
            ".wwwwww.",
            ".wwwwww.",
            ".wwwwww.",
            ".wwwwww.",
            "........",
        };

        /// <summary>2026-11-27 (Yan's Gemini cabin plan): the one-room cabin variants, 7x8 outer; Cabin(i) cycles them.</summary>
        public static void Cabin(TerrainCanvas c, int x, int y, int v, string[] pick = null)
        {
            string[][] rows = { CabinA, CabinB, CabinHunter, CabinFisher, CabinLogger }, floors = { CabinAFloor, CabinBFloor, CabinHunterFloor, CabinFisherFloor, CabinLoggerFloor };
            int k = pick != null ? System.Array.IndexOf(pick, "x") : 0;
            Stamp(c, x, y, rows[v % 5], floors[v % 5], "plank", "house");
        }

        /// <summary>The dev map: the three test buildings on grass, side by side.</summary>
        public static MapLayout LabMap()
        {
            const int W = 84, H = 26;
            var c = new TerrainCanvas(W, H, TerrainCanvas.Seed("building_lab"));
            c.Paint(0, 0, W, H, GroundType.Grass); c.PaintNoise(GroundType.Dirt, 6f, 0.62f, 1);
            c.Keep(0, 0, W, H);
            Stamp(c, 3, 5, House, HouseFloor, "plank", "house");
            Stamp(c, 27, 5, Shop, ShopFloor, "plaster", "shop");
            Stamp(c, 53, 4, Barracks, BarracksFloor, "concrete", "barracks");
            c.Landmark("arrive:building_lab", 41, 22);
            c.Border(Solid, 1, 0, "wall");
            var m = c.ToMapLayout(); m.FullSize = true; return m;
        }
    }
}
