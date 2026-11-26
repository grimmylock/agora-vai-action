// 2026-11-24 (Yan: buildings should feel lived in, furnished for what they are; ArtSource/BUILDINGS_PLAN.md): interior plans that carry a FLOOR LAYER next to the structure layer, so every room has its own floor (tile in a kitchen and a bathroom, wood in
// a bedroom, concrete in a shop and a barracks), and furniture glyphs for the new Pixel Lab props. Three TEST buildings (a house, a shop, a barracks) and a dev-only map to look at them (`BUILDING_LAB=1` in ZoneLayoutRecipes, or the render tool).
// Structure layer: '#' wall, 'n' window, 'D' door, '.' floor; furniture: b bed, e cot, w wardrobe, k sink counter, s stove, f fridge, T dining table + chairs, o sofa, a armchair, z toilet, u bathtub, v washbasin, l bookshelf, g goods shelf,
// C shop counter, K bunk bed, F footlocker, R weapon rack, M map table, d desk, c crate, h lockers, i nightstand, p plant, y floor lamp, q kitchen counter, N notice board, X box stack, B bench. Floor layer: 'w' wood (Floor), 't' lab tile (LabTile), 'k' white kitchen / bathroom tile (KitchenTile), 'c' concrete, 'd' dirt, 'g' gravel, 's' sand, '.' leave the ground as it is.
using System.Collections.Generic;
using GameData;
using static GameCore.TileType;

namespace GameCore
{
    public static class Interiors
    {
        // glyph -> (look, tall/hard cover?)
        static readonly Dictionary<char, (string look, bool hard)> Pieces = new Dictionary<char, (string, bool)>
        {
            ['b'] = ("bed", false), ['e'] = ("cot", false), ['w'] = ("wardrobe", true), ['k'] = ("kitchensink", false), ['s'] = ("stove", true), ['f'] = ("fridge", true), ['T'] = ("diningset", false),
            ['o'] = ("sofa", false), ['a'] = ("armchair", false), ['z'] = ("toilet", false), ['u'] = ("bathtub", false), ['v'] = ("washbasin", false), ['l'] = ("bookshelf", true), ['g'] = ("goodsshelf", true),
            ['C'] = ("shopcounter", false), ['K'] = ("bunkbed", true), ['F'] = ("footlocker", false), ['R'] = ("weaponrack", true), ['M'] = ("maptable", false), ['d'] = ("desk", false), ['c'] = ("crate", false), ['h'] = ("lockers", true),
            ['i'] = ("nightstand", false), ['p'] = ("plant", false), ['y'] = ("floorlamp", false), ['q'] = ("kitchencounter", false), ['N'] = ("noticeboard", false), ['X'] = ("boxstack", true), ['B'] = ("bench", false),
            ['S'] = ("stool", false), ['H'] = ("medcabinet", true), ['P'] = ("bottleshelf", true), ['V'] = ("barcounter", false), ['m'] = ("machine", true), ['r'] = ("rustbarrel", false), ['Q'] = ("pooltable", false), ['J'] = ("jukebox", true), ['U'] = ("cardtable", false), ['G'] = ("kegs", true), ['L'] = ("barcounter_e", false), ['Z'] = ("barcounter_w", false), ['Y'] = ("barcounter_n", false), ['A'] = ("sofa_e", false), ['I'] = ("sofa_w", false), ['t'] = ("bench_e", false), ['j'] = ("bench_w", false), ['O'] = ("jukebox_w", true), ['E'] = ("bottleshelf_e", true), ['W'] = ("bottleshelf_w", true), ['1'] = ("bunkbed_e", true), ['2'] = ("bunkbed_w", true), ['3'] = ("cot_e", false), ['4'] = ("cot_w", false), ['5'] = ("goodsshelf_e", true), ['6'] = ("goodsshelf_w", true), ['7'] = ("workbench", false), ['8'] = ("haybale", true), ['9'] = ("trough", false), ['0'] = ("radiotable", false),
        };
        static GroundType FloorOf(char f) => f == 'k' ? GroundType.KitchenTile : f == 't' ? GroundType.LabTile : f == 'c' ? GroundType.Concrete : f == 'd' ? GroundType.Dirt : f == 'g' ? GroundType.Gravel : f == 's' ? GroundType.Sand : GroundType.Floor;

        /// <summary>Stamps a plan with its floor layer at (x0, y0). `wallLook` is the outer wall; inner walls use it too.</summary>
        public static void Stamp(TerrainCanvas c, int x0, int y0, string[] rows, string[] floors, string wallLook, string archetype = null)
        {
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
            "###n##n####n##",
            "#.gg.g.f#c.vz#",
            "#.......#c...#",
            "nc...g..D....n",
            "#cCC....#..7.#",
            "#......y#...X#",
            "#.......#....#",
            "##n#D#n####D##",
        };

        public static readonly string[] StationFloor = {
            "..............",
            ".ccccccc.cccc.",
            ".ccccccc.cccc.",
            ".ccccccc.cccc.",
            ".ccccccc.cccc.",
            ".ccccccc.cccc.",
            ".ccccccc.cccc.",
            "..............",
        };

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
