// The map overhaul (approved by Yan, 2026-10-10): one big map per tier instead of three small zones.
// "The whole map can be crossed in like 1 minute" - but a huge empty map is worse, so each tier map is
// about 200x200 tiles with ~25 spots: 3 big places (30-50 tiles, many rooms), 7-8 medium places that
// squads hold, and ~15 small finds that each have a reason (a note pointing to a stash, a sniper nest...).
// The old zones live on as named regions of the map (MapLayout.Regions): their names, landmarks, quests,
// traders and factions stay; walking from one region into the next is free. Dungeons are separate small
// maps (TierDungeons.cs) entered through a hatch, a door or a culvert.
//
// Authored at final scale (FullSize: no MapUpscaler pass). BuildingCarver still hollows the solid
// blocks into rooms and MapDresser still grows each region's biome over the open ground, but leaves the
// cells a recipe marks Keep (roads, yards, streets) alone.
using System;
using System.Collections.Generic;
using GameData;
using static GameCore.TileType;
using static GameCore.GroundType;

namespace GameCore
{
    public static partial class TierMaps
    {
        public const int Size = 200;

        // ------------------------------------------------------------------ shared pieces

        /// <summary>A ragged edge of woods (or cliffs, or reeds) round the whole map, 3-8 tiles deep.</summary>
        static void WildEdge(TerrainCanvas c, Func<int, int, string> lookAt, int min = 3, int extra = 5)
        {
            for (int y = 0; y < c.H; y++)
                for (int x = 0; x < c.W; x++)
                {
                    int d = Math.Min(Math.Min(x, y), Math.Min(c.W - 1 - x, c.H - 1 - y));
                    float wob = c.Noise(x, y, 9f, 501) * extra;
                    if (d < min + wob) c.Put(x, y, Solid, lookAt(x, y));
                }
        }

        /// <summary>A watchtower: a 2x2 platform `h` high on legs, reached by a ladder running south.</summary>
        static void Tower(TerrainCanvas c, int x, int y, int h = 4)
        {
            // Batch 73 (Yan: "make the sniper nest into a watchtower like this", a timber watchtower photo): one tall
            // prop (ArtSource/world/props.py watchtower) over a 3x2 footprint. Between the legs is hard cover; the stair
            // (front middle) is half way up and the deck under the cabin (back middle) is the top, h high.
            c.Clear(x - 3, y - 3, 8, 8);
            for (int dy = 0; dy < 2; dy++) for (int dx = -1; dx <= 1; dx++) c.Put(x + dx, y + dy, HardCover, "part");
            c.Rect(x, y + 1, 1, 1, Empty, (h + 1) / 2, "watchtower");   // the stair, half way up (the prop is drawn from here)
            c.Rect(x, y, 1, 1, Empty, h, "part");                        // the deck under the cabin
        }

        /// <summary>A campfire ring: fire, logs, a crate - the sort of place people stop.</summary>
        static void Fire(TerrainCanvas c, int x, int y, bool lit = true)
        {
            c.Clear(x - 3, y - 3, 7, 7, Dirt);
            if (lit) c.Deco(x, y, "campfire"); else c.Deco(x, y, "stones");
            c.Put(x - 2, y, ScatterCover, "logs").Put(x + 2, y + 1, ScatterCover, "logs").Put(x + 1, y - 2, ScatterCover, "crate");
        }

        /// <summary>A pole line across the map (power lines that once fed the area), one pole every `step` tiles.</summary>
        static void Poles(TerrainCanvas c, int x1, int y1, int x2, int y2, int step = 11)
        {
            int n = Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1)) / step;
            for (int i = 0; i <= n; i++)
            {
                int x = x1 + (x2 - x1) * i / Math.Max(1, n), y = y1 + (y2 - y1) * i / Math.Max(1, n);
                if (c.Get(x, y) == Empty && c.LookAt(x, y) == null && c.HeightAt(x, y) == 0) c.Put(x, y, Solid, "pole");
            }
        }

        /// <summary>A sandbag ring (hard cover) with openings on the given sides (0 S, 1 E, 2 W, 3 N).</summary>
        static void Sandbags(TerrainCanvas c, int x, int y, int w, int h, params int[] open)
        {
            for (int i = 0; i < w; i++)
            {
                bool gapS = Array.IndexOf(open, 0) >= 0 && Math.Abs(i - w / 2) <= 1, gapN = Array.IndexOf(open, 3) >= 0 && Math.Abs(i - w / 2) <= 1;
                if (!gapN) c.Put(x + i, y, HardCover, "sandbags");
                if (!gapS) c.Put(x + i, y + h - 1, HardCover, "sandbags");
            }
            for (int j = 1; j < h - 1; j++)
            {
                bool gapW = Array.IndexOf(open, 2) >= 0 && Math.Abs(j - h / 2) <= 1, gapE = Array.IndexOf(open, 1) >= 0 && Math.Abs(j - h / 2) <= 1;
                if (!gapW) c.Put(x, y + j, HardCover, "sandbags");
                if (!gapE) c.Put(x + w - 1, y + j, HardCover, "sandbags");
            }
        }

        /// <summary>A stream or ditch: wading water (difficult), `width` wide, along a polyline.</summary>
        static void Stream(TerrainCanvas c, int width, params (int x, int y)[] pts)
        {
            for (int i = 0; i + 1 < pts.Length; i++)
            {
                c.Line(pts[i].x, pts[i].y, pts[i + 1].x, pts[i + 1].y, width, Difficult);
                c.PaintLine(pts[i].x, pts[i].y, pts[i + 1].x, pts[i + 1].y, width, Water);
                c.PaintLine(pts[i].x, pts[i].y, pts[i + 1].x, pts[i + 1].y, width + 2, Mud);
                c.PaintLine(pts[i].x, pts[i].y, pts[i + 1].x, pts[i + 1].y, width, Water);
            }
        }

        /// <summary>A plank footbridge (or a ford) over water: walkable boards across `len` tiles.</summary>
        static void Bridge(TerrainCanvas c, int x, int y, int w, int h)
        {
            for (int yy = y; yy < y + h; yy++)
                for (int xx = x; xx < x + w; xx++) { c.Put(xx, yy, Empty, "boardwalk"); c.Paint(xx, yy, 1, 1, Floor); }
            c.Keep(x, y, w, h);
        }

        /// <summary>A boardwalk along an axis-aligned polyline: 3 planks wide, walkable over the marsh.</summary>
        static void Boardwalk(TerrainCanvas c, params (int x, int y)[] pts)
        {
            for (int i = 0; i + 1 < pts.Length; i++)
            {
                var (x1, y1) = pts[i]; var (x2, y2) = pts[i + 1];
                int x0 = Math.Min(x1, x2) - 1, y0 = Math.Min(y1, y2) - 1;
                Bridge(c, x0, y0, Math.Abs(x2 - x1) + 3, Math.Abs(y2 - y1) + 3);
            }
        }

        static void Graves(TerrainCanvas c, int x, int y, int cols, int rows)
        {
            for (int j = 0; j < rows; j++)
                for (int i = 0; i < cols; i++)
                {
                    float r = c.Roll();
                    if (r < 0.45f) c.Put(x + i * 3, y + j * 3, ScatterCover, "stones");
                    else if (r < 0.85f) c.Put(x + i * 3, y + j * 3, HardCover, "block");
                }
        }


        /// <summary>A roofless ruin: the stumps of a house's walls with gaps blown in them and rubble inside -
        /// cover on the way between places, never a closed room.</summary>
        static void Ruin(TerrainCanvas c, int x, int y, int w, int h, string wall = "wall")
        {
            c.Clear(x - 1, y - 1, w + 2, h + 2, Dirt, false);   // Batch 97: earth round it, floorboards only inside
            c.Paint(x, y, w, h, Floor);
            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                {
                    bool edge = i == 0 || j == 0 || i == w - 1 || j == h - 1;
                    if (edge && c.Noise(x + i, y + j, 2.3f, 777) < 0.58f) c.Put(x + i, y + j, Solid, wall);
                    else if (!edge && c.Roll() < 0.12f) c.Put(x + i, y + j, Difficult, "rubble");
                }
            c.Put(x + w / 2, y + h - 1, Empty, null);              // there's always a way in
            c.Keep(x - 1, y - 1, w + 2, h + 2);
        }

        /// <summary>A hedgerow: a line of bushes (light cover) along a field edge, with gaps to walk through.</summary>
        static void Hedge(TerrainCanvas c, int x1, int y1, int x2, int y2)
        {
            int n = Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1));
            for (int i = 0; i <= n; i++)
            {
                int x = x1 + (x2 - x1) * i / Math.Max(1, n), y = y1 + (y2 - y1) * i / Math.Max(1, n);
                if (c.Get(x, y) != Empty || c.LookAt(x, y) != null || c.Kept(x, y)) continue;   // Batch 97: not through ruins and yards
                float r = c.Noise(x, y, 3f, 919);
                if (r < 0.3f) continue;                                    // a gap
                c.Put(x, y, r > 0.82f ? Solid : ScatterCover, r > 0.82f ? "tree" : "bush");
            }
        }

        /// <summary>A ploughed field gone to seed: dark earth with rows of brush, a hay pile or two.</summary>
        static void Field(TerrainCanvas c, int x, int y, int w, int h)
        {
            c.Paint(x, y, w, h, Dirt);
            for (int j = y + 1; j < y + h - 1; j += 3)
                for (int i = x + 1; i < x + w - 1; i++)
                    if (c.Get(i, j) == Empty && c.LookAt(i, j) == null && c.Roll() < 0.35f) c.Deco(i, j, "tallgrass");
            c.Put(x + w / 3, y + h / 2, ScatterCover, "logs");
            c.Keep(x, y, w, h);
        }

        // ------------------------------------------------------------------ Tier 1

        // 200x200. The Perimeter (south: Bonfire Village, the checkpoint, the farmstead, the cemetery, the
        // army camp), Toxic Marshes (east: the causeway north to The Scrapyard, the fishing village, the
        // pumping station, the flooded bunker), Smuggler's Run (north-west: the rail depot and the cutting
        // north to the Industrial Basin). Dungeons: Rust Culvert cave, chapel crypt, pumping station
        // basement, flooded bunker (key in the fishing village), smugglers' tunnel under the depot.
        public static MapLayout Tier1()
        {
            var c = new TerrainCanvas(Size, Size, TerrainCanvas.Seed("tier1"));
            c.FillAll(Empty, 0);

            // ---- regions: the marsh fills the east, the run the north-west, the perimeter everything south
            c.Regions((x, y) =>
            {
                if (x >= 58 && x <= 100 && y >= 146 && y <= 186) return "bonfire_village";
                float wob = (c.Noise(x, y, 22f, 11) - 0.5f) * 18f;
                if (x > 134 + wob && y < 140 + wob * 0.5f) return "marshes";
                if (x < 126 + wob && y < 110 + wob * 0.6f) return "smugglers_run";
                return "perimeter";
            });
            bool In(int x, int y, string r) => c.RegionAt(x, y) == r;

            // ---- ground: meadow with dirt in the south, darker woodland north, gravel and dirt along the run
            c.PaintWhere(Grass, (x, y) => true);
            c.PaintNoise(DarkGrass, 26f, 0.52f, 21);
            c.PaintNoise(Dirt, 18f, 0.66f, 23, (x, y) => In(x, y, "perimeter") || In(x, y, "bonfire_village"));
            c.PaintNoise(DarkGrass, 14f, 0.45f, 25, (x, y) => In(x, y, "smugglers_run") && x > 64);
            c.PaintNoise(Gravel, 16f, 0.62f, 27, (x, y) => In(x, y, "smugglers_run") && x < 64);
            c.PaintWhere(Mud, (x, y) => In(x, y, "marshes"));

            // ---- the edge of the world: woods west and north, the old perimeter fence south, reeds east
            WildEdge(c, (x, y) => In(x, y, "marshes") ? "foliage" : "foliage");

            // ---- big woods: the west forest, the central forest between the run and the marsh
            foreach (var (bx, by, r) in new[] { (12, 30, 13), (22, 8, 10), (8, 58, 9), (76, 14, 10), (88, 50, 9), (122, 22, 11), (126, 62, 8), (70, 60, 6), (186, 160, 7), (8, 92, 6) })
                c.Blob(bx, by, r, Solid, 0, 0.5f, "foliage");
            c.Grove(4, 4, 60, 70, 0.05f, "pine", "tree");
            c.Grove(64, 4, 66, 110, 0.06f, "pine", "tree");
            c.Grove(4, 100, 50, 96, 0.012f, "tree", "pine");
            c.Grove(110, 140, 30, 50, 0.010f, "tree");

            // ---- the stream that drains the fields into the marsh, under the road at the Rust Culvert
            Stream(c, 3, (4, 154), (30, 155), (50, 154), (56, 151), (62, 144), (72, 140), (96, 140), (120, 137), (140, 133));

            // ================================================================ THE PERIMETER
            // the main road, south gate to the fork; the north-west branch to the depot; a track north-east to the causeway
            c.Road(5, Asphalt, (100, 199), (100, 121));
            c.Road(5, Asphalt, (100, 121), (88, 112), (74, 104), (68, 101));
            c.Road(3, Dirt, (101, 119), (118, 116), (134, 113));
            Bridge(c, 97, 139, 7, 3);                                   // the road over the culvert (asphalt edges stay)
            c.Paint(98, 139, 5, 3, Asphalt); for (int x = 98; x <= 102; x++) c.Deco(x, 140, null);

            // Checkpoint Gate: the old perimeter fence with the barrier across the road and the guard hut
            c.Fence(2, 193, 197, 193, "fence", 95, 11);
            for (int x = 30; x < 34; x++) c.Deco(x, 193, "fence_torn");
            for (int x = 98; x <= 102; x++) c.Deco(x, 193, "barrierpole");
            c.Hut(105, 185, 8, 6, "wall", 2);
            c.Put(108, 190, Solid, "window").Put(106, 186, ScatterCover, "lockers").Deco(110, 187, "desk");
            c.Hut(90, 186, 5, 4, "concrete", 1, false);
            c.Put(96, 191, HardCover, "jersey").Put(104, 194, HardCover, "jersey").Put(96, 195, HardCover, "jersey").Put(104, 191, HardCover, "jersey");
            Sandbags(c, 86, 194, 6, 4, 3);
            c.Put(114, 192, Barrel).Put(88, 191, ScatterCover, "rustbarrel");
            c.Landmark("Checkpoint Gate", 100, 190);

            // Bonfire Village (place A, a Safe Zone): Yefim's old camp grown into a walled village
            c.Clear(60, 148, 39, 37, Dirt);
            c.PaintNoise(Grass, 7f, 0.55f, 31, (x, y) => x >= 60 && x <= 98 && y >= 148 && y <= 184);
            for (int x = 59; x <= 99; x++)
            {
                if (Math.Abs(x - 79) > 2) c.Put(x, 147, Solid, "plank");
                c.Put(x, 185, Solid, "plank");
            }
            for (int y = 147; y <= 185; y++)
            {
                if (Math.Abs(y - 172) > 2) c.Put(59, y, Solid, "plank");
                if (Math.Abs(y - 166) > 2) c.Put(99, y, Solid, "plank");
            }
            c.Road(3, Dirt, (79, 146), (79, 166), (98, 166));               // village lanes: north gate - fire - east gate
            c.Road(3, Dirt, (60, 172), (79, 172), (79, 166));
            Fire(c, 79, 166);
            c.Put(77, 168, ScatterCover, "armchair").Put(82, 165, ScatterCover, "stones");
            c.Landmark("The Bonfire Camp", 79, 167).Landmark("arrive:bonfire_village", 79, 170);   // New Game starts by the fire
            // Yefim's Footlocker: the trader's container lean-to by the fire
            c.Rect(84, 160, 3, 2, Solid, 0, "container");
            c.Put(84, 162, ScatterCover, "crate").Put(86, 162, ScatterCover, "crate").Put(87, 160, Barrel);
            c.Landmark("Yefim's Footlocker", 85, 163);
            // the bunkhouse (rest), the medic, the repair shed, the job board, and the villagers' huts
            c.Block(62, 150, 14, 9, "plank", 1, Dirt);
            c.Landmark("Bunkhouse", 69, 160);
            c.Block(88, 150, 9, 8, "plaster", 1, Dirt);
            c.Landmark("Medic's House", 92, 159);
            c.Hut(62, 176, 8, 6, "metal", 3, false);
            c.Put(63, 177, HardCover, "machine").Put(68, 177, ScatterCover, "desk").Put(63, 180, ScatterCover, "crate");
            c.Landmark("Repair Bench", 66, 175);
            c.Put(75, 162, ScatterCover, "pole");
            c.Landmark("Job Board", 75, 163);
            c.Hut(72, 177, 6, 5, "plank", 3).Hut(86, 177, 7, 5, "plank", 3).Hut(90, 168, 6, 5, "plank", 2);
            c.Hut(62, 161, 6, 4, "metal", 1, false).Hut(62, 166, 6, 4, "metal", 1, false);      // tents for the passing Wanderers
            for (int x = 79; x <= 85; x += 2) c.Put(x, 151, ScatterCover, x == 83 ? "desk" : "crate");   // the market stalls
            c.Put(80, 154, ScatterCover, "armchair").Put(86, 153, ScatterCover, "crate");
            Field(c, 79, 175, 6, 8);                                                       // a vegetable patch
            c.Put(94, 163, Solid, "stones").Deco(94, 164, "puddle");                       // the well
            Tower(c, 95, 179, 3);                                                          // a lookout over the road
            for (int y = 150; y < 184; y += 8) c.Deco(78, y, "lamp");
            c.Put(70, 172, ScatterCover, "logs").Put(61, 182, ScatterCover, "tires").Put(97, 150, ScatterCover, "crate");
            c.Put(101, 162, HardCover, "sandbags").Put(101, 170, HardCover, "sandbags");      // the gate post on the road
            c.Keep(59, 146, 42, 41);

            // Old Cemetery and the chapel (the crypt below is a dungeon)
            c.Clear(14, 158, 34, 30, Grass, false);
            c.Fence(14, 158, 47, 158, "fence", 16, 3).Fence(14, 187, 47, 187).Fence(14, 158, 14, 187).Fence(47, 158, 47, 187, "fence", 12, 3);
            c.Hut(18, 161, 9, 12, "plaster", 0, false);
            c.Put(19, 162, ScatterCover, "desk").Put(25, 162, ScatterCover, "armchair");
            c.Deco(22, 163, "hatch");
            c.Landmark("Chapel Crypt", 22, 164).Exit("chapel_crypt", 22, 163);
            Graves(c, 30, 162, 5, 8);
            Graves(c, 18, 177, 4, 3);
            c.Grove(14, 158, 34, 30, 0.02f, "deadtree");
            c.Landmark("Old Cemetery", 36, 172);
            c.Road(2, Dirt, (36, 159), (36, 186));
            c.Keep(14, 158, 34, 30);
            c.Road(2, Dirt, (58, 172), (48, 172), (40, 172));

            // Old Farmstead: farmhouse, barn, cowshed and silos round a fenced yard
            c.Clear(14, 110, 44, 39, Dirt, false);
            c.PaintNoise(Grass, 8f, 0.5f, 33, (x, y) => x >= 14 && x < 58 && y >= 110 && y < 150);
            c.Block(17, 113, 12, 9, "wall", 1);
            TierDungeons.StampHouse(c, 17, 113, "farmhouse_ground", "wall", "Farmhouse Stairs", "farmhouse_upper");   // Batch 120: an L, with an upstairs
            c.Block(34, 113, 16, 10, "plank", 1);
            c.Block(17, 138, 24, 7, "plaster", 1);
            c.Rect(52, 114, 2, 2, Solid, 0, "part").Put(52, 115, Solid, "tank");
            c.Rect(52, 118, 2, 2, Solid, 0, "part").Put(52, 119, Solid, "tank");
            c.Fence(14, 110, 57, 110).Fence(14, 149, 57, 149, "fence", 30, 4).Fence(57, 110, 57, 149, "fence", 18, 4).Fence(14, 110, 14, 149);
            c.Put(44, 130, HardCover, "wreck").Put(30, 128, ScatterCover, "logs").Put(31, 128, ScatterCover, "logs").Put(48, 140, ScatterCover, "brush");
            c.Landmark("Old Farmstead", 36, 130);
            c.Road(3, Dirt, (78, 146), (66, 140), (57, 130));
            Bridge(c, 64, 139, 4, 5);

            // Rust Culvert: the road's drainage tunnel, its mouth hidden in brush (Secret Cave Area behind it)
            c.Deco(95, 140, "culvert").Deco(96, 140, "culvert");
            foreach (var (bx, by) in new[] { (93, 138), (94, 142), (91, 141), (92, 137), (95, 143) }) c.Put(bx, by, Difficult, "brush");
            c.Landmark("Rust Culvert", 94, 140).Exit("secret_cave", 95, 140);

            // Roadside Station: pumps, a shop, wrecks, just short of the fork
            c.Clear(104, 122, 18, 14, Concrete);
            c.Block(110, 124, 9, 7, "plaster", 1, Concrete);
            c.Put(105, 128, HardCover, "pillar").Put(105, 132, HardCover, "pillar");
            c.Put(107, 133, HardCover, "wreck").Put(119, 133, HardCover, "dumpster").Put(103, 124, ScatterCover, "tires");
            c.Landmark("Roadside Station", 108, 130);

            // Army Field Camp with Watchtower 3: sandbags, tents, an abandoned carrier, the leaning tower
            c.Clear(150, 150, 38, 36, Dirt, false);
            Sandbags(c, 152, 152, 32, 30, 2, 3);
            c.Hut(156, 156, 7, 5, "metal", 0).Hut(166, 156, 7, 5, "metal", 0).Hut(156, 170, 7, 5, "metal", 3);
            c.Rect(168, 171, 4, 2, Solid, 0, "container").Rect(174, 171, 4, 2, Solid, 0, "container");
            Tower(c, 178, 156, 4);
            c.Landmark("Watchtower 3", 178, 156);
            c.Put(162, 178, HardCover, "wreck").Put(176, 178, HardCover, "jersey").Put(153, 166, Barrel).Put(180, 166, ScatterCover, "crate");
            for (int x = 154; x < 182; x++) if (x < 165 || x > 171) c.Put(x, 166, HardCover, "trench");
            c.Put(168, 175, ScatterCover, "crate").Put(169, 175, ScatterCover, "crate").Put(176, 175, HardCover, "sandbags").Put(177, 175, HardCover, "sandbags");
            c.Put(160, 163, Barrel).Put(174, 161, ScatterCover, "crate").Put(167, 154, ScatterCover, "pole");
            c.Landmark("Army Field Camp", 168, 167);
            c.Keep(152, 152, 32, 30);
            c.Road(3, Dirt, (103, 178), (130, 176), (151, 167));

            // ---- small finds in the Perimeter
            // the crashed car: a body, and a note in the glovebox about the stash behind the bus stop
            c.Clear(72, 128, 7, 5, null, false);
            c.Put(74, 130, HardCover, "wreck").Deco(76, 130, "bones").Put(75, 129, ScatterCover, "debris");
            c.Landmark("Crashed Car", 76, 131);
            c.Put(104, 150, HardCover, "busstop").Put(103, 150, HardCover, "part");
            c.Stash("Bus Stop", 105, 149);
            c.Landmark("Bus Stop", 104, 152);
            // the transformer yard (a fenced substation the pole line ends at)
            c.Clear(128, 172, 11, 8, Gravel);
            c.Fence(128, 172, 138, 172).Fence(128, 179, 138, 179, "fence", 4, 2).Fence(128, 172, 128, 179).Fence(138, 172, 138, 179);
            c.Put(131, 174, HardCover, "machine").Put(135, 174, HardCover, "machine").Put(133, 177, ScatterCover, "rustbarrel");
            c.Landmark("Transformer Yard", 133, 176);
            Poles(c, 133, 171, 72, 104, 10);
            // the lone grave in the field, with a note pinned to the cross
            c.Put(124, 152, ScatterCover, "stones").Deco(125, 153, "bones").Deco(123, 153, "flowers");
            c.Stash("Lone Grave", 125, 151);
            c.Landmark("Lone Grave", 124, 154);
            // the collapsed footbridge over the stream
            c.Put(80, 139, Difficult, "rubble").Put(81, 140, Difficult, "rubble");
            c.Landmark("Collapsed Footbridge", 80, 141);
            // the sniper nest on a stand over the fork
            Tower(c, 86, 104, 3);
            c.Stash("Sniper Nest", 87, 105);
            c.Landmark("Sniper Nest", 86, 108);
            // the burnt-out carrier on the depot road
            c.Put(80, 108, HardCover, "wreck").Put(81, 108, HardCover, "part").Deco(82, 109, "bones");
            c.Landmark("Burnt-Out Carrier", 81, 110);

            // ================================================================ SMUGGLER'S RUN
            // The Rail Depot (place B): fenced yard, warehouse, offices, loco shed, wagons on the sidings
            c.Clear(14, 66, 64, 38, Gravel, false);
            c.PaintNoise(Dirt, 9f, 0.55f, 41, (x, y) => x >= 14 && x < 78 && y >= 66 && y < 104);
            c.Fence(14, 66, 40, 66).Fence(52, 66, 77, 66).Fence(14, 103, 77, 103, "fence", 50, 6).Fence(14, 66, 14, 103).Fence(77, 66, 77, 103, "fence", 30, 6);
            c.Block(17, 69, 22, 12, "concrete", 1, Concrete);                  // warehouse
            c.Block(17, 87, 12, 9, "plaster", 1, Concrete);                    // offices
            c.Block(56, 69, 18, 11, "wall", 1, Concrete);                      // loco shed
            for (int y = 66; y <= 103; y++) { c.Deco(46, y, "rail_v"); if (y >= 80) { c.Deco(41, y, "rail_v"); c.Deco(51, y, "rail_v"); } }
            c.Rect(41, 84, 1, 3, Solid, 0, "container").Rect(41, 92, 1, 3, Solid, 0, "container").Rect(51, 88, 1, 4, Solid, 0, "container");
            c.Paint(33, 84, 6, 18, Concrete);                                   // loading platform
            c.Put(34, 88, ScatterCover, "crate").Put(36, 91, ScatterCover, "crate").Put(35, 96, ScatterCover, "crate").Put(60, 88, Barrel).Put(66, 95, ScatterCover, "tires");
            // the loading office over the smugglers' hatch
            c.Hut(58, 86, 7, 6, "concrete", 0, true);
            c.Deco(61, 88, "hatch");
            c.Landmark("Loading Office", 61, 90).Exit("smugglers_tunnel", 61, 88);
            c.Landmark("Rail Depot", 46, 90);
            // the water tower in the yard
            Tower(c, 70, 90, 4);
            c.Landmark("Water Tower", 70, 94);
            c.Keep(15, 67, 62, 36);

            // The Cutting: the line north between high embankments to the Industrial Basin
            c.Rect(30, 2, 11, 62, Solid, 3, "cliff");                            // west embankment
            c.Rect(52, 2, 11, 62, Solid, 3, "cliff");                            // east embankment
            c.Clear(41, 0, 11, 66, Gravel);
            for (int y = 0; y < 66; y++) c.Deco(46, y, "rail_v");
            c.Rect(54, 6, 3, 52, Empty, 5); c.Paint(54, 6, 3, 52, DarkGrass);    // Overwatch Ridge: the walkway along the top
            c.Ramp(63, 30, -1, 0, 7, 0, 5, 2);                                  // the scramble path up from the woods
            c.Paint(57, 30, 8, 2, Dirt);
            c.Landmark("Overwatch Ridge", 55, 24);
            c.Rect(41, 28, 4, 6, Solid, 3, "cliff");                             // Blind Curve: the slumped banks
            c.Rect(48, 36, 4, 6, Solid, 3, "cliff");
            c.Landmark("Blind Curve", 46, 34);
            c.Put(49, 50, Solid, "container").Put(50, 51, ScatterCover, "crate").Put(42, 46, ScatterCover, "crate").Put(43, 56, ScatterCover, "debris");
            c.Landmark("The Cutting", 47, 50);
            // the derailed wagon near the north end
            c.Rect(42, 14, 2, 4, Solid, 0, "container"); c.Put(44, 13, ScatterCover, "crate").Put(42, 19, ScatterCover, "crate");
            c.Landmark("Derailed Wagon", 45, 16);
            c.Exit("industrial_basin", 46, 0);
            // the signal box at the cutting's mouth
            c.Hut(64, 58, 5, 5, "wall", 2);
            c.Landmark("Signal Box", 63, 61);

            // the dead scavenger's camp in a west-forest clearing
            c.Clear(10, 44, 9, 8, Dirt, false);
            Fire(c, 14, 48, false);
            c.Deco(16, 46, "bones").Deco(12, 50, "bones");
            c.Stash("Scavenger's Pack", 16, 49);
            c.Landmark("Dead Scavenger's Camp", 14, 51);
            // the old well west of the depot
            c.Clear(4, 104, 8, 7, Dirt, false);
            c.Put(7, 107, Solid, "stones").Deco(8, 108, "puddle");
            c.Stash("Old Well", 8, 106);
            c.Landmark("Old Well", 7, 110);

            // Logging Camp (central woods): log piles, a sawmill, cabins
            c.Clear(98, 70, 22, 20, Dirt, false);
            c.Block(101, 72, 10, 7, "plank", 1, Dirt);
            c.Hut(113, 72, 5, 5, "plank", 0).Hut(113, 81, 5, 5, "plank", 2);
            for (int x = 100; x < 110; x += 2) { c.Put(x, 83, ScatterCover, "logs"); c.Put(x, 86, ScatterCover, "logs"); }
            c.Put(98, 88, HardCover, "wreck");
            c.Landmark("Logging Camp", 106, 81);
            c.Road(3, Dirt, (100, 119), (104, 100), (106, 88));
            // the hunter's hide in the woods east of the camp
            c.Hut(122, 90, 5, 4, "plank", 2, false);
            c.Stash("Hunter's Hide", 124, 91);
            c.Landmark("Hunter's Hide", 121, 92);

            // Radio Mast Hill: the hill north of the woods, the mast, the equipment shed
            c.HeightBlob(102, 32, 10, 2, 0.3f);
            c.Clear(94, 24, 17, 16, Gravel, false);
            c.Put(102, 30, Solid, "antenna").Put(103, 30, Solid, "part");
            c.Hut(96, 34, 6, 5, "concrete", 1);
            c.Fence(94, 24, 110, 24).Fence(94, 24, 94, 39).Fence(110, 24, 110, 39, "fence", 6, 3);
            c.Landmark("Radio Mast", 102, 33);
            c.Road(3, Dirt, (106, 70), (104, 50), (103, 41));

            // ================================================================ TOXIC MARSHES
            // The Causeway: the boardwalk north through the marsh to The Scrapyard
            Boardwalk(c, (134, 113), (150, 113), (150, 94), (147, 94), (147, 58), (160, 58), (160, 30), (166, 30), (166, 0));
            c.Landmark("The Causeway", 150, 104);
            c.Exit("scrapyard", 166, 0);

            // The Fishing Village (place C): stilt huts on a dry island, jetties, the boat shed, the headman's house
            c.Clear(150, 60, 36, 30, DarkGrass);
            c.PaintNoise(Dirt, 6f, 0.55f, 51, (x, y) => x >= 150 && x < 186 && y >= 60 && y < 90);
            c.Block(154, 63, 10, 8, "plank", 1, Dirt);                          // boat shed
            c.Hut(167, 63, 7, 6, "plank", 0).Hut(176, 63, 7, 6, "plank", 0).Hut(154, 77, 7, 6, "plank", 3).Hut(166, 79, 6, 5, "plank", 3);
            c.Hut(175, 75, 9, 8, "plaster", 2, true);                           // the headman's house
            c.Stash("Headman's Strongbox", 182, 80);
            c.Landmark("Headman's House", 179, 79);
            Bridge(c, 186, 70, 8, 3); Bridge(c, 160, 90, 3, 7);                  // jetties
            c.Put(192, 70, HardCover, "sunktruck");
            for (int x = 170; x < 176; x += 2) c.Put(x, 72, ScatterCover, "logs");
            c.Hut(150, 84, 6, 5, "plank", 1).Hut(173, 84, 6, 5, "plank", 3);
            Bridge(c, 150, 89, 3, 6); Bridge(c, 182, 88, 3, 6); Bridge(c, 143, 66, 7, 3);
            for (int y = 64; y < 70; y += 2) c.Put(186, y, ScatterCover, "logs");             // drying racks
            Fire(c, 168, 76, false);
            c.Keep(150, 60, 36, 30);
            c.Landmark("Fishing Village", 168, 74);
            // the Sunken Tanker: the hull half under water, a dry deck to shoot from
            c.Rect(138, 36, 8, 3, Solid, 0, "hull");
            c.Rect(139, 37, 6, 1, Empty, 2, "platform");
            c.Ramp(146, 37, 1, 0, 1, 2, 0, 1, "platform");
            c.Clear(147, 35, 1, 5, Mud);
            c.Landmark("Sunken Tanker", 141, 36);
            // the Reed Blind on the east bank
            c.Blob(178, 104, 5, ScatterCover, 0, 0.35f, "reeds");
            c.Landmark("Reed Blind", 178, 104);
            // the Pumping Station on the marsh's south shore (its basement is a dungeon)
            c.Clear(152, 116, 26, 20, Concrete);
            c.Block(156, 119, 12, 9, "concrete", 1, Concrete);
            c.Hut(170, 121, 6, 6, "concrete", 0, false);
            c.Deco(172, 123, "hatch");
            c.Landmark("Pump Room", 172, 125).Exit("pumping_basement", 172, 123);
            for (int x = 152; x < 178; x++) if (x % 5 != 0) c.Deco(x, 132, x % 4 == 1 ? "pipepost" : "pipe");
            c.Rect(152, 116, 2, 2, Solid, 0, "part").Put(152, 117, Solid, "tank");
            c.Fence(151, 115, 178, 115, "fence", 8, 3).Fence(151, 135, 178, 135, "fence", 14, 4);
            c.Landmark("Pumping Station", 162, 130);
            c.Road(3, Dirt, (134, 113), (140, 124), (152, 126));
            // the Flooded Bunker: a concrete entrance on a hummock, sealed - the key is in the fishing village
            c.Clear(180, 18, 14, 12, DarkGrass);
            c.Building(183, 20, 8, 6, "concrete", Concrete);
            c.Put(186, 25, Door, "metal");
            c.Landmark("Flooded Bunker", 187, 28).Exit("flooded_bunker", 186, 25);
            Bridge(c, 167, 26, 13, 3);                                            // planks out from the causeway
            // marsh small finds: the fisherman's shack, the duck blind
            c.Clear(184, 112, 10, 8, Dirt);
            c.Hut(186, 113, 6, 5, "plank", 2);
            Bridge(c, 180, 115, 5, 2);
            c.Stash("Fisherman's Shack", 190, 115);
            c.Landmark("Fisherman's Shack", 185, 118);
            c.Ring(150, 20, 2, 3, ScatterCover, 0, "reeds");
            c.Rect(150, 20, 2, 2, Empty, 2, "platform");
            c.Clear(150, 22, 1, 1, null, false);
            c.Ramp(150, 22, 0, 1, 1, 2, 0, 1, "platform");
            c.Landmark("Duck Blind", 152, 23);

            // ---- between the places: ruins, hedgerows, old fields (cover on the way, nothing to loot)
            Ruin(c, 126, 142, 8, 6); Ruin(c, 112, 160, 7, 6); Ruin(c, 136, 184, 8, 6); Ruin(c, 62, 116, 9, 7);
            Ruin(c, 84, 40, 7, 6); Ruin(c, 124, 46, 8, 6); Ruin(c, 184, 140, 8, 7, "plank"); Ruin(c, 20, 104, 6, 5);
            Hedge(c, 106, 146, 148, 146); Hedge(c, 142, 148, 142, 190); Hedge(c, 104, 156, 104, 190); Hedge(c, 60, 104, 60, 113);
            Hedge(c, 4, 151, 48, 151); Hedge(c, 112, 112, 112, 120);
            Field(c, 108, 158, 14, 9); Field(c, 116, 180, 11, 8); Field(c, 64, 104, 10, 9);

            c.Scatter(ScatterCover, 0.01f);
            return Finish(c);
        }


        // ------------------------------------------------------------------ Tier 2

        /// <summary>A silo: a round concrete tower `h` high; climbable ones have a ladder up the south side to a top platform.</summary>
        static void Silo(TerrainCanvas c, int x, int y, bool climbable, int h = 4)
        {
            if (!climbable) { c.Blob(x, y, 3, Difficult, 0, 0.45f, "rubble"); c.PaintBlob(x, y, 3, Concrete); return; }
            c.Blob(x, y, 2, Solid, h, 0.1f, "concrete");
            c.Put(x, y, Empty, "platform");
            c.Ramp(x, y + 3, 0, -1, 3, 0, h, 1, "platform");
        }

        /// <summary>A heap of scrap (junk mass), with an optional walkable top `h` high reached from the south.</summary>
        static void ScrapHeap(TerrainCanvas c, int x, int y, int r, int h = 0)
        {
            c.Blob(x, y, r, Solid, Math.Max(0, h - 2), 0.4f, "junk");
            if (h > 0) { c.Rect(x - 1, y - 1, 3, 3, Empty, h, "platform"); c.Ramp(x, y + r + 1, 0, -1, r, 0, h, 1); }
        }

        // 200x200. Black Valley (north: a ravine with a gulley across it - the Chokepoint Bridge, a ford and a broken
        // rail bridge are the only crossings - the old mine works, Sniper's Lip along the north rim, the way north to
        // the Scavenger's Hub), the Industrial Basin (south-west: the Central Plant, Silo Row, the Loading Dock over
        // Secret Lab Alpha, the workers' flats) and The Scrapyard (south-east: The Maze, the Scrap King's Nest, Crusher
        // Row, the truck stop). Exits south line up with Tier 1's: the cutting (x 46) and the causeway (x 166).
        // Dungeons: Secret Lab Alpha (hidden), the crusher pit, the valley mine.
        public static MapLayout Tier2()
        {
            var c = new TerrainCanvas(Size, Size, TerrainCanvas.Seed("tier2"));
            c.FillAll(Empty, 0);
            c.Regions((x, y) =>
            {
                float wob = (c.Noise(x, y, 20f, 61) - 0.5f) * 14f;
                if (y < 82 + wob * 0.3f) return "black_valley";
                return x < 100 + wob ? "industrial_basin" : "scrapyard";
            });
            bool In(int x, int y, string r) => c.RegionAt(x, y) == r;

            // ---- ground
            c.PaintWhere(DarkGrass, (x, y) => In(x, y, "black_valley"));
            c.PaintNoise(Dirt, 16f, 0.6f, 63, (x, y) => In(x, y, "black_valley"));
            c.PaintWhere(Concrete, (x, y) => In(x, y, "industrial_basin"));
            c.PaintNoise(Gravel, 14f, 0.55f, 65, (x, y) => In(x, y, "industrial_basin"));
            c.PaintNoise(Grass, 18f, 0.66f, 67, (x, y) => In(x, y, "industrial_basin"));
            c.PaintWhere(Dirt, (x, y) => In(x, y, "scrapyard"));
            c.PaintNoise(Gravel, 12f, 0.55f, 69, (x, y) => In(x, y, "scrapyard"));

            WildEdge(c, (x, y) => In(x, y, "black_valley") ? (y < 20 ? "cliff" : "foliage") : In(x, y, "industrial_basin") ? "foliage" : "junk");

            // ================================================================ BLACK VALLEY
            // the north rim: cliffs with Sniper's Lip along the top, a road cut through to the Hub
            c.Rect(0, 0, 200, 13, Solid, 5, "cliff");
            c.Rect(8, 5, 184, 2, Empty, 6); c.Paint(8, 5, 184, 2, Gravel);
            c.Ramp(150, 12, 0, -1, 6, 0, 6, 2); c.Paint(150, 7, 2, 6, Dirt);
            c.Ramp(58, 12, 0, -1, 6, 0, 6, 2); c.Paint(58, 7, 2, 6, Dirt);
            c.Landmark("Sniper's Lip", 120, 5);
            Sandbags(c, 116, 4, 8, 4, 0); c.Put(118, 5, ScatterCover, "crate");
            c.Clear(95, 0, 11, 14, Gravel); c.RectHeight(95, 0, 11, 14, 0);   // Batch 125: the cut is down at the valley's level (it was left 5 high on the rock - the road to the Hub couldn't be walked)
            c.Exit("scavengers_hub", 100, 0);
            // the radio repeater on the rim
            c.Put(176, 5, Solid, "antenna").Put(177, 6, ScatterCover, "crate");
            c.Landmark("Radio Repeater", 176, 7);
            // the gulley across the valley floor, and its three crossings
            c.Rect(0, 40, 200, 6, Difficult, -2, "rubble");
            c.Paint(0, 39, 200, 8, Mud);
            for (int x = 0; x < 200; x += 1) if (c.Noise(x, 42, 4f, 71) > 0.55f) c.Put(x, 42, Difficult, null);
            // Chokepoint Bridge: half-collapsed, on the valley road
            c.Clear(97, 39, 7, 8, Floor);
            for (int y = 39; y <= 46; y++) { c.Deco(97, y, "fence_v"); c.Deco(103, y, "fence_v"); }
            c.Put(101, 43, Difficult, "rubble").Put(102, 43, Difficult, "rubble");
            Sandbags(c, 94, 48, 13, 4, 3);
            c.Landmark("Chokepoint Bridge", 100, 42);
            // the ford in the west: wading, slow, in the open
            c.Rect(24, 40, 9, 6, Difficult, -1); c.Paint(24, 40, 9, 6, Water);
            c.Landmark("The Ford", 28, 47);
            // the broken rail bridge in the east: piers and a gap you can scramble down into
            for (int y = 39; y <= 46; y++) { c.Put(162, y, Empty, null); c.Put(163, y, Empty, null); c.Paint(162, y, 2, 1, Floor); }
            c.Put(162, 42, Difficult, "rubble").Put(163, 43, Difficult, "rubble");
            c.Rect(160, 37, 1, 2, Solid, 0, "concrete").Rect(165, 37, 1, 2, Solid, 0, "concrete").Rect(160, 47, 1, 2, Solid, 0, "concrete").Rect(165, 47, 1, 2, Solid, 0, "concrete");
            Fire(c, 158, 52, false); c.Put(156, 53, ScatterCover, "crate");
            c.Stash("Bridge Camp", 159, 50);
            c.Landmark("Rail Bridge Camp", 160, 53);
            for (int y = 14; y < 39; y++) c.Deco(162, y, "rail_v");
            for (int y = 47; y < 78; y++) c.Deco(162, y, "rail_v");
            // the valley road and The Wreck Line along it
            c.Road(5, Asphalt, (100, 84), (100, 47));
            c.Road(5, Asphalt, (100, 39), (100, 13));
            c.Road(3, Dirt, (100, 62), (60, 62), (28, 54), (28, 46));
            c.Road(3, Dirt, (100, 62), (140, 62), (160, 56));
            for (int i = 0; i < 10; i++)
            {
                int wx = 20 + i * 16 + (i % 2) * 3;
                if (Math.Abs(wx - 100) < 8) continue;
                c.Put(wx, 64 + (i % 3), Solid, "wreck").Put(wx + 1, 64 + (i % 3), Solid, "wreck").Put(wx + 2, 65 + (i % 3), Difficult, "rubble");
            }
            c.Landmark("The Wreck Line", 60, 64);
            // Old Mine Works (place C): pithead, headframe, spoil heaps, the adit into the rim
            c.Clear(14, 14, 58, 24, Dirt, false);
            c.Block(20, 18, 14, 9, "plaster", 1, Gravel);
            c.Block(48, 24, 12, 8, "wall", 1, Gravel);
            Tower(c, 38, 18, 5);
            for (int x = 36; x < 66; x++) if (x % 4 != 0) c.Deco(x, 34, x % 4 == 1 ? "pipepost" : "pipe");
            c.Blob(64, 18, 4, Solid, 0, 0.5f, "cliff"); c.Blob(18, 33, 3, Solid, 0, 0.5f, "cliff");
            c.Rect(26, 30, 1, 3, Solid, 0, "container").Rect(29, 30, 1, 3, Solid, 0, "container");
            for (int y = 13; y < 34; y++) c.Deco(44, y, "rail_v");
            c.Rect(38, 9, 5, 5, Empty, 0); c.Paint(38, 9, 5, 5, CaveFloor);
            c.Landmark("Mine Adit", 40, 12).Exit("valley_mine", 40, 10);
            c.Landmark("Old Mine Works", 40, 28);
            // valley small finds: the burnt tank, the hanged man's tree, the overturned lorry, the hunter's shack
            c.Put(130, 22, HardCover, "wreck").Put(131, 22, HardCover, "part").Put(132, 23, Difficult, "rubble").Deco(129, 23, "bones");
            c.Landmark("Burnt Tank", 131, 24);
            c.Put(76, 52, Solid, "deadtree").Deco(77, 53, "bones");
            c.Landmark("Hanged Man's Tree", 77, 54);
            c.Stash("Under the Roots", 74, 50);
            c.Put(118, 72, HardCover, "wreck").Put(119, 72, HardCover, "part").Put(120, 72, HardCover, "part").Put(121, 73, ScatterCover, "crate");
            c.Stash("Overturned Lorry", 122, 71);
            c.Landmark("Overturned Lorry", 120, 74);
            c.Hut(178, 24, 6, 5, "plank", 0);
            c.Stash("Hunter's Shack", 182, 25);
            c.Landmark("Hunter's Shack", 180, 30);
            c.Grove(4, 14, 192, 24, 0.035f, "pine", "tree");
            c.Grove(4, 48, 192, 30, 0.03f, "pine", "deadtree");
            // the south ridge between the valley and the basins, with three passes
            for (int x = 0; x < 200; x++)
            {
                if (Math.Abs(x - 40) <= 6 || Math.Abs(x - 100) <= 6 || Math.Abs(x - 160) <= 6) continue;
                int top = 78 + (int)(c.Noise(x, 0, 7f, 73) * 3);
                c.Rect(x, top, 1, 6, Solid, 3, "cliff");
            }

            // ================================================================ INDUSTRIAL BASIN
            c.Road(5, Asphalt, (46, 199), (46, 170));
            c.Road(5, Asphalt, (10, 96), (96, 96));
            c.Road(5, Asphalt, (40, 96), (40, 84));
            c.Road(4, Asphalt, (70, 134), (70, 199));
            c.Road(4, Asphalt, (8, 134), (98, 134));
            c.Road(5, Asphalt, (98, 176), (46, 176));
            // the gatehouse on the south road
            c.Hut(52, 186, 6, 5, "concrete", 2);
            for (int x = 44; x <= 48; x++) c.Deco(x, 189, "barrierpole");
            c.Landmark("Gatehouse", 50, 190);
            // Central Plant (place A): the hall, its machine bays, offices alongside
            c.Clear(12, 100, 76, 32, Concrete, false);
            c.Building(14, 102, 54, 26, "metal", Concrete, (40, 127), (40, 102), (14, 114), (67, 114));
            c.Stamp("Central Plant", 15, 103, 3, "concrete", Concrete, 0, "machine");
            c.Landmark("Central Plant", 40, 114);
            c.Block(72, 103, 12, 9, "plaster", 1, Concrete);
            c.Block(72, 116, 12, 10, "plaster", 1, Concrete);
            // the smokestack (a sniper's perch)
            Tower(c, 8, 112, 6);
            c.Landmark("Smokestack", 8, 116);
            c.Stash("Smokestack", 9, 113);
            // Silo Row: two climbable, two collapsed
            c.Clear(12, 138, 54, 12, Concrete, false);
            Silo(c, 18, 142, true); Silo(c, 30, 142, true); Silo(c, 42, 142, false); Silo(c, 54, 142, false);
            c.Landmark("Silo Row", 36, 147);
            // the Loading Dock over the hidden hatch
            c.Clear(14, 152, 52, 20, Concrete);
            c.Rect(16, 168, 4, 2, Solid, 0, "container").Rect(56, 154, 4, 2, Solid, 0, "container").Rect(22, 154, 4, 2, Solid, 0, "container");
            for (int i = 0; i < 14; i++) c.Put(18 + (i * 7) % 44, 156 + (i * 5) % 12, ScatterCover, "crate");
            c.Deco(40, 162, "hatch");
            c.Put(26, 160, Barrel).Put(52, 166, Barrel);
            c.Landmark("Loading Dock", 40, 162).Exit("secret_lab_alpha", 40, 162);
            // the workers' flats
            c.Block(76, 140, 14, 14, "plaster", 2, Asphalt);
            c.Block(76, 158, 14, 14, "plaster", 2, Asphalt);
            c.Landmark("Workers' Flats", 82, 156);
            // industrial small finds: the crane, the chemical dump, the substation
            Tower(c, 30, 182, 5);
            c.Landmark("Crane Cab", 30, 186);
            c.Clear(10, 182, 12, 10, Gravel, false);
            for (int i = 0; i < 9; i++) c.Put(11 + (i * 4) % 10, 183 + (i * 3) % 8, ScatterCover, "rustbarrel");
            c.Deco(15, 187, "puddle").Deco(17, 185, "puddle");
            c.Landmark("Chemical Dump", 16, 190);
            c.Clear(84, 184, 12, 9, Gravel);
            c.Fence(84, 184, 95, 184).Fence(84, 192, 95, 192, "fence", 5, 2).Fence(84, 184, 84, 192).Fence(95, 184, 95, 192);
            c.Put(87, 187, HardCover, "machine").Put(91, 187, HardCover, "machine");
            c.Landmark("Substation", 90, 190);

            // ================================================================ THE SCRAPYARD
            c.Road(4, Dirt, (166, 199), (166, 170), (150, 160), (140, 152));
            c.Road(3, Dirt, (166, 170), (190, 150), (186, 124));
            c.Road(4, Dirt, (100, 84), (104, 96), (118, 100));
            c.Road(3, Dirt, (160, 84), (160, 98));
            // The Maze (place B): canyons of stacked wrecks
            c.Stamp("The Maze", 112, 100, 6, "junk", Dirt, 0, "tires");
            c.Landmark("The Maze", 140, 124);
            // the Scrap King's Nest: the tallest heap, a car-door shack on top - the Raiders' yard
            c.Blob(186, 108, 8, Solid, 3, 0.35f, "junk");
            c.Rect(184, 105, 5, 5, Empty, 5, "platform"); c.Ramp(186, 117, 0, -1, 8, 0, 5, 2);
            c.Put(184, 105, ScatterCover, "crate").Put(188, 105, ScatterCover, "armchair");
            c.Hut(190, 120, 6, 5, "metal", 2, true);
            c.Landmark("Scrap King's Nest", 186, 108);
            Sandbags(c, 178, 120, 16, 5, 3);
            // Crusher Row: dead crushers in a line, wrecks queued between; the pit underneath is a dungeon
            c.Clear(108, 158, 86, 10, Concrete);
            for (int x = 112; x < 190; x += 8) { c.Rect(x, 160, 2, 2, Solid, 1, "machine"); c.Put(x + 3, 161, Solid, "wreck"); }
            c.Deco(150, 166, "hatch");
            c.Landmark("Crusher Row", 130, 163).Landmark("Crusher Pit", 150, 165).Exit("crusher_pit", 150, 166);
            // the truck stop on the old highway
            c.Clear(104, 176, 30, 20, Asphalt, false);
            c.Block(106, 178, 22, 7, "plaster", 1, Asphalt);
            c.Block(110, 188, 9, 7, "wall", 1, Asphalt);
            c.Put(124, 190, HardCover, "pillar").Put(124, 193, HardCover, "pillar").Put(129, 191, HardCover, "wreck").Put(131, 186, HardCover, "wreck");
            c.Landmark("Truck Stop", 120, 187);
            // scrap small finds: the tire fort, the kennels, the rusted bus, the scrapper's shack
            for (int i = 0; i < 16; i++) { double a = i * 0.39; c.Put(126 + (int)(Math.Cos(a) * 5), 90 + (int)(Math.Sin(a) * 4), ScatterCover, "tires"); }
            c.Landmark("Tire Fort", 126, 92);
            c.Clear(176, 136, 14, 10, Dirt);
            c.Fence(176, 136, 189, 136).Fence(176, 145, 189, 145).Fence(176, 136, 176, 145, "fence", 4, 2).Fence(189, 136, 189, 145);
            for (int x = 178; x < 188; x += 3) c.Put(x, 137, ScatterCover, "crate");
            c.Deco(182, 141, "bones").Deco(185, 142, "bones");
            c.Landmark("Dog Kennels", 182, 140);
            for (int i = -2; i <= 2; i++) c.Put(148 + i, 178, HardCover, i == 0 ? "bus" : "part");
            c.Landmark("Rusted Bus", 148, 180);
            c.Hut(176, 182, 6, 5, "metal", 2);
            c.Stash("Scrapper's Shack", 180, 183);
            c.Landmark("Scrapper's Shack", 175, 186);
            foreach (var (hx, hy, hr) in new[] { (108, 150, 3), (196, 96, 4), (138, 192, 3), (192, 176, 3), (106, 128, 3) })
                ScrapHeap(c, hx, hy, hr);
            c.Exit("marshes", 166, 199).Exit("smugglers_run", 46, 199);

            // ---- between the places
            Ruin(c, 60, 66, 8, 6); Ruin(c, 140, 28, 7, 6); Ruin(c, 86, 22, 8, 6, "plaster");
            Ruin(c, 96, 112, 6, 6, "concrete");
            Poles(c, 10, 90, 196, 90, 10);
            c.Scatter(ScatterCover, 0.008f);
            return Finish(c);
        }


        // ------------------------------------------------------------------ Tier 3

        // 200x200. The Scavenger's Hub (south middle, a walled market town and Safe Zone - the Black Valley road comes
        // in at its south gate), Dried Lake Sub-Station (west: the cracked lakebed, Tower Line, the Sub-Station House,
        // the lakeside holiday camp, the stranded ferry, the water works) and The Rail-Yards (east: container stacks,
        // the Roundhouse, the signal tower, the loco depot). Exits north to Ghost Town (x 40) and Vanguard Barracks (x 160).
        // Dungeons: Sub-Station Depths and the Maintenance Tunnels (hidden), the stranded ferry.
        public static MapLayout Tier3()
        {
            var c = new TerrainCanvas(Size, Size, TerrainCanvas.Seed("tier3"));
            c.FillAll(Empty, 0);
            c.Regions((x, y) =>
            {
                if (x >= 66 && x <= 134 && y >= 142) return "scavengers_hub";   // to the map edge: the Black Valley road comes in here
                float wob = (c.Noise(x, y, 20f, 81) - 0.5f) * 12f;
                return x < 102 + wob ? "dried_lake" : "rail_yards";
            });
            bool In(int x, int y, string r) => c.RegionAt(x, y) == r;
            bool Lakebed(int x, int y) => In(x, y, "dried_lake") && y < 138 + c.Noise(x, y, 14f, 83) * 10 && x > 4;

            // ---- ground: the lakebed is cracked sand and gravel, its old shore grass; the yards gravel; the hub dirt
            c.PaintWhere(Grass, (x, y) => true);
            c.PaintNoise(DarkGrass, 22f, 0.55f, 85);
            c.PaintWhere(Sand, Lakebed);
            c.PaintNoise(Gravel, 12f, 0.6f, 87, Lakebed);
            c.PaintNoise(Mud, 9f, 0.7f, 89, Lakebed);
            c.PaintWhere(Gravel, (x, y) => In(x, y, "rail_yards") && y < 130);
            c.PaintNoise(Dirt, 14f, 0.55f, 91, (x, y) => In(x, y, "rail_yards"));
            c.PaintWhere(Dirt, (x, y) => In(x, y, "scavengers_hub"));

            WildEdge(c, (x, y) => In(x, y, "dried_lake") ? (y < 100 ? "cliff" : "foliage") : In(x, y, "rail_yards") ? "concrete" : "foliage");

            // ================================================================ THE SCAVENGER'S HUB (Safe Zone)
            c.Road(5, Asphalt, (100, 199), (100, 140));
            c.Clear(67, 143, 67, 51, Dirt);
            for (int x = 66; x <= 134; x++)
            {
                if (Math.Abs(x - 100) > 3) { c.Put(x, 142, Solid, "concrete"); c.Put(x, 194, Solid, "concrete"); }
            }
            for (int y = 142; y <= 194; y++)
            {
                if (Math.Abs(y - 168) > 2) { c.Put(66, y, Solid, "concrete"); c.Put(134, y, Solid, "concrete"); }
            }
            c.Paint(97, 143, 7, 51, Concrete);
            // Sentinel Checkpoint at the south gate
            c.Hut(90, 186, 6, 5, "concrete", 1, false).Hut(105, 186, 6, 5, "concrete", 2, false);
            Sandbags(c, 94, 180, 13, 4, 0, 3);
            for (int x = 97; x <= 103; x++) c.Deco(x, 193, "barrierpole");
            c.Landmark("Sentinel Checkpoint", 100, 189);
            // The Bazaar: container stalls down the main lane
            for (int y = 150; y < 178; y += 6)
            {
                c.Rect(88, y, 4, 2, Solid, 0, "container").Put(90, y + 2, ScatterCover, "crate");
                c.Rect(109, y, 4, 2, Solid, 0, "container").Put(110, y + 2, ScatterCover, "crate");
            }
            c.Landmark("The Bazaar", 100, 163);
            Fire(c, 100, 168); c.Put(98, 170, ScatterCover, "armchair");
            // The Rusty Anchor, the bunkhouse, the Sentinel barracks
            c.Block(70, 146, 16, 11, "plaster", 1, Dirt);
            c.Landmark("The Rusty Anchor", 78, 158);
            c.Block(70, 172, 14, 10, "plank", 1, Dirt);
            c.Landmark("Hub Bunkhouse", 77, 183);
            c.Block(116, 146, 15, 10, "concrete", 1, Dirt);
            c.Landmark("Sentinel Barracks", 123, 157);
            // the Repair Yard
            c.Clear(115, 170, 17, 21, Concrete);
            c.Fence(114, 169, 132, 169, "fence", 2, 3).Fence(114, 169, 114, 191, "fence", 8, 3).Fence(114, 191, 132, 191);
            c.Put(118, 174, Solid, "wreck").Put(124, 180, Solid, "wreck").Put(128, 174, HardCover, "machine").Put(120, 186, ScatterCover, "crate").Put(127, 186, ScatterCover, "desk");
            c.Landmark("Repair Yard", 122, 182);
            c.Keep(66, 142, 69, 53);
            c.Landmark("arrive:scavengers_hub", 100, 172);

            // the Sentinel outpost on the road north of the hub
            c.Road(5, Asphalt, (100, 140), (100, 110));
            Sandbags(c, 92, 118, 17, 12, 0, 3);
            c.Hut(94, 120, 6, 5, "concrete", 1, true);
            Tower(c, 105, 120, 4);
            c.Landmark("Sentinel Outpost", 100, 126);
            c.Road(4, Asphalt, (100, 110), (92, 96), (88, 40), (70, 22), (40, 8), (40, 0));
            c.Road(4, Asphalt, (100, 110), (106, 100), (106, 34), (160, 26), (160, 0));
            c.Road(3, Dirt, (66, 168), (52, 168));
            c.Road(3, Dirt, (134, 168), (150, 160));

            // ================================================================ DRIED LAKE SUB-STATION
            // Tower Line: dead masts marching across the lakebed
            for (int i = 0; i < 9; i++) if (i != 3) c.Put(14 + i * 9, 30 + (i % 2), Solid, "antenna");
            c.Landmark("Tower Line", 50, 32);
            c.Landmark("The Flats", 30, 70);
            // the Sub-Station House (the Surveyors' field cache)
            c.Clear(38, 68, 36, 24, Concrete, false);
            c.Stamp("Sub-Station House", 40, 70, 4, "concrete", Concrete, 0, "debris");
            c.Landmark("Sub-Station House", 56, 82);
            for (int x = 38; x < 74; x++) if (x % 5 != 0) c.Deco(x, 93, x % 4 == 1 ? "pipepost" : "pipe");
            // the Drainage Sump: a concrete pit with a hatch (Sub-Station Depths)
            c.RectHeight(58, 98, 8, 8, -2); c.Paint(58, 98, 8, 8, Concrete);
            c.Deco(62, 102, "hatch");
            c.Landmark("Drainage Sump", 62, 102).Exit("sub_station_depths", 62, 102);
            // the stranded ferry, beached on the lakebed when the water went
            c.Rect(50, 40, 24, 8, Solid, 0, "hull");
            c.Rect(52, 42, 20, 4, Empty, 2, "platform");
            c.Ramp(62, 48, 0, -1, 2, 0, 2, 2, "platform");
            c.Put(73, 44, Door, "metal");
            c.Landmark("Stranded Ferry", 75, 44).Exit("stranded_ferry", 73, 44);
            // the lakeside holiday camp on the old west shore (place C)
            c.Clear(6, 100, 34, 40, Grass, false);
            for (int i = 0; i < 8; i++) c.Hut(8 + (i % 4) * 8, 104 + (i / 4) * 12, 6, 5, "plank", i < 4 ? 0 : 3);
            c.Block(10, 128, 18, 9, "plaster", 1, Grass);
            c.Landmark("Lakeside Camp", 22, 116);
            Tower(c, 42, 104, 4);
            c.Landmark("Lifeguard Tower", 42, 108);
            Bridge(c, 40, 118, 14, 3);
            c.Landmark("Old Pier", 52, 120);
            // the water works between the lake and the hub
            c.Clear(16, 148, 38, 38, Concrete, false);
            c.Block(20, 152, 14, 9, "concrete", 1, Concrete);
            foreach (var (px, py) in new[] { (22, 168), (34, 168), (22, 178), (34, 178) })
            { c.Rect(px, py, 9, 6, Difficult); c.Paint(px, py, 9, 6, Water); }
            c.Rect(44, 152, 2, 2, Solid, 0, "part").Put(44, 153, Solid, "tank");
            c.Rect(48, 152, 2, 2, Solid, 0, "part").Put(48, 153, Solid, "tank");
            c.Landmark("Water Works", 30, 164);
            // lake small finds: the downed helicopter, the weather station, the dead surveyor, the rowboat, the shrine
            c.Put(78, 74, HardCover, "wreck").Put(79, 74, HardCover, "part").Put(80, 75, Difficult, "rubble").Put(76, 73, ScatterCover, "debris");
            c.Landmark("Downed Helicopter", 78, 76);
            c.Clear(76, 10, 12, 10, Gravel);
            c.Fence(76, 10, 87, 10).Fence(76, 19, 87, 19, "fence", 4, 2).Fence(76, 10, 76, 19).Fence(87, 10, 87, 19);
            c.Put(81, 13, Solid, "antenna").Put(84, 16, HardCover, "machine");
            c.Landmark("Weather Station", 81, 17);
            c.Deco(80, 58, "bones").Put(81, 58, ScatterCover, "crate");
            c.Stash("Dead Surveyor", 82, 57);
            c.Landmark("Dead Surveyor", 80, 60);
            c.Rect(70, 128, 3, 1, Solid, 0, "hull");
            c.Stash("Rowboat", 71, 129);
            c.Landmark("Rowboat", 71, 131);
            c.Put(8, 192, ScatterCover, "stones").Deco(9, 192, "flowers").Deco(7, 193, "flowers");
            c.Landmark("Roadside Shrine", 9, 194);

            // ================================================================ THE RAIL-YARDS
            for (int x = 104; x < 197; x++) { c.Deco(x, 50, "rail"); c.Deco(x, 56, "rail"); c.Deco(x, 62, "rail"); }
            c.Clear(130, 44, 40, 4, Concrete); c.Landmark("Freight Platform", 150, 45);
            // Container Stacks (place B): firing lanes between the stacks
            c.Stamp("Container Stacks", 112, 66, 4, "container", Gravel);
            c.Landmark("Container Stacks", 148, 80);
            // the Roundhouse: the Contractors' staging point
            c.Clear(118, 108, 44, 20, Concrete, false);
            c.Stamp("Roundhouse", 120, 110, 4, "wall", Concrete, 0, "crate");
            c.Landmark("Roundhouse", 140, 118);
            // the Signal Tower over the tracks
            c.Rect(184, 38, 5, 5, Solid, 0, "concrete");
            c.Rect(185, 39, 3, 3, Empty, 6, "platform");
            c.Ramp(186, 46, 0, -1, 5, 0, 6, 2, "platform");
            c.Landmark("Signal Tower", 186, 40);
            // the Rail Culvert under the track bed (Maintenance Tunnels)
            c.PaintBlob(120, 44, 3, Mud); c.Deco(120, 44, "culvert"); c.Deco(121, 44, "culvert");
            c.Landmark("Rail Culvert", 120, 43).Exit("maintenance_tunnels", 120, 44);
            // the burned locomotive and its wagons, stopped on the middle track
            c.Put(150, 56, HardCover, "flatcar").Put(149, 56, HardCover, "part").Put(151, 56, HardCover, "part").Put(152, 56, HardCover, "part");
            c.Put(156, 56, HardCover, "flatcar").Put(155, 56, HardCover, "part").Put(157, 56, HardCover, "part").Put(158, 56, HardCover, "part");
            c.Put(162, 56, Solid, "wreck").Put(163, 56, Solid, "wreck");
            c.Landmark("Burned Locomotive", 162, 58);
            // the loco depot and its water tower (south-east)
            c.Clear(146, 140, 46, 34, Gravel, false);
            c.Block(150, 146, 24, 14, "wall", 2, Concrete);
            for (int y = 162; y < 196; y++) c.Deco(176, y, "rail_v");
            Tower(c, 182, 144, 4);
            c.Landmark("Loco Depot", 162, 162).Landmark("Yard Water Tower", 182, 148);
            c.Hut(186, 184, 6, 5, "wall", 2);
            c.Stash("Track Workers' Hut", 190, 186);
            c.Landmark("Track Workers' Hut", 185, 190);
            Tower(c, 140, 132, 3);
            c.Landmark("Billboard Perch", 140, 136);
            c.Stash("Billboard Perch", 141, 133);

            c.Exit("black_valley", 100, 199).Exit("ghost_town", 40, 0).Exit("vanguard_barracks", 160, 0);
            // ---- between the places
            Ruin(c, 140, 186, 8, 6); Ruin(c, 30, 196 - 8, 7, 5); Ruin(c, 112, 20, 7, 6, "concrete");
            Ruin(c, 76, 120, 6, 5); Ruin(c, 180, 96, 8, 6, "concrete");
            Hedge(c, 138, 132, 196, 132); Hedge(c, 4, 145, 60, 145);
            c.Grove(104, 4, 92, 36, 0.03f, "pine", "tree");
            c.Grove(134, 128, 62, 70, 0.012f, "tree");
            Poles(c, 104, 36, 196, 36, 10);
            c.Scatter(ScatterCover, 0.006f);
            return Finish(c);
        }


        // ------------------------------------------------------------------ Tier 4

        // 200x200. Ghost Town (south-west: Main Street, the Row Houses, the Church, the Town Hall over Secret Lab Beta,
        // the school and the flats - the city pass turns the gaps between them into streets), Vanguard Barracks (south-
        // east: the Liberators' fortified compound, a Safe Zone, with its road south to the Rail-Yards) and Crimson Woods
        // (north and east: the red forest, the Obelisk Shrine, the Sentinel firebase on the road north, Deadfall Clearing,
        // Sinkhole Grove over the Deep Caverns, and the old summer camp with its bomb shelter). Exits: Dried Lake (x 40)
        // and the Rail-Yards (x 160) south, the Quarry Basin north (x 100).
        public static MapLayout Tier4()
        {
            var c = new TerrainCanvas(Size, Size, TerrainCanvas.Seed("tier4"));
            c.FillAll(Empty, 0);
            c.Regions((x, y) =>
            {
                if (x >= 120 && x <= 190 && y >= 116) return "vanguard_barracks";
                float wob = (c.Noise(x, y, 20f, 101) - 0.5f) * 14f;
                return x < 106 + wob && y > 92 + wob * 0.5f ? "ghost_town" : "crimson_woods";
            });
            bool In(int x, int y, string r) => c.RegionAt(x, y) == r;

            // ---- ground: the town is left bare for the street pass; the woods are dark and red-leaved
            c.PaintWhere(DarkGrass, (x, y) => In(x, y, "crimson_woods"));
            c.PaintNoise(Dirt, 12f, 0.6f, 103, (x, y) => In(x, y, "crimson_woods"));
            c.PaintWhere(Dirt, (x, y) => In(x, y, "ghost_town"));
            c.PaintWhere(Gravel, (x, y) => In(x, y, "vanguard_barracks"));
            c.PaintNoise(Dirt, 10f, 0.5f, 105, (x, y) => In(x, y, "vanguard_barracks"));

            WildEdge(c, (x, y) => In(x, y, "crimson_woods") ? "redfoliage" : In(x, y, "ghost_town") ? "plaster" : "concrete");

            // ================================================================ CRIMSON WOODS
            foreach (var (bx, by, r) in new[] { (30, 40, 12), (60, 70, 8), (16, 14, 9), (84, 26, 9), (196, 30, 8), (112, 84, 7), (196, 100, 6), (40, 88, 5) })
                c.Blob(bx, by, r, Solid, 0, 0.5f, "redfoliage");
            c.Landmark("The Red Canopy", 30, 54);
            // the road north through the woods to the Quarry Basin
            c.Road(4, Dirt, (100, 0), (100, 60), (96, 96));
            c.Road(4, Dirt, (100, 60), (130, 96), (155, 116));
            c.Exit("quarry_basin", 100, 0);
            // the Sentinel firebase on the road (their base in these woods)
            c.Clear(88, 50, 26, 22, Dirt, false);
            Sandbags(c, 89, 51, 24, 20, 0, 3);
            c.Hut(92, 54, 7, 5, "concrete", 1, true).Hut(104, 63, 7, 5, "metal", 3, true);
            Tower(c, 92, 62, 4);
            c.Landmark("Sentinel Firebase", 100, 60);
            // the Obelisk Shrine in its clearing
            c.Blob(56, 24, 7, Empty, 0, 0.2f); c.PaintBlob(56, 24, 7, Gravel);
            c.Put(56, 24, Solid, "pillar");
            c.Ring(56, 24, 3, 3, ScatterCover, 0, "stones");
            c.Put(51, 20, Solid, "pillar").Put(61, 28, Solid, "pillar");
            c.Landmark("Obelisk Shrine", 56, 27);
            c.Keep(49, 17, 15, 15);
            // Deadfall Clearing: petrified trunks lying across it
            c.Blob(34, 72, 8, Empty, 0, 0.3f); c.PaintBlob(34, 72, 8, Dirt);
            for (int i = 0; i < 6; i++) c.Line(27 + i * 3, 67 + (i % 2) * 3, 29 + i * 3, 74 + (i % 2) * 3, 1, ScatterCover, 0, true, "logs");
            c.Landmark("Deadfall Clearing", 34, 72);
            // Sinkhole Grove: roots round a collapsed sinkhole (the Deep Caverns)
            c.Blob(14, 80, 6, Empty, 0, 0.3f);
            c.HeightBlob(14, 80, 4, -2); c.PaintBlob(14, 80, 4, Mud);
            c.Blob(14, 80, 3, Difficult, -2, 0.3f, "brush");
            c.Put(14, 80, Empty);
            c.Landmark("Sinkhole Grove", 14, 82).Exit("deep_caverns", 14, 80);
            // the old summer camp (place C): cabins, the mess hall, the sports field, the pond, the bomb shelter
            c.Clear(118, 16, 66, 58, Grass, false);
            for (int i = 0; i < 10; i++) c.Hut(122 + (i % 5) * 9, 20 + (i / 5) * 10, 7, 6, "plank", i < 5 ? 0 : 3);
            c.Block(166, 20, 14, 10, "plaster", 1, Grass);
            c.Block(166, 36, 12, 8, "wall", 1, Grass);
            c.Clear(122, 44, 30, 18, Dirt);
            c.Put(137, 44, ScatterCover, "pole").Put(122, 52, ScatterCover, "pole").Put(151, 52, ScatterCover, "pole");
            c.Rect(158, 52, 16, 12, Difficult); c.Paint(158, 52, 16, 12, Water); c.PaintLine(157, 51, 174, 51, 1, Mud);
            Bridge(c, 164, 48, 3, 6);
            c.Hut(176, 56, 6, 5, "plank", 2);
            c.Landmark("Boathouse", 178, 61);
            c.Hut(126, 64, 6, 5, "concrete", 3, false);
            c.Deco(129, 66, "hatch");
            c.Landmark("Shelter Hatch", 129, 70).Exit("camp_shelter", 129, 66);
            c.Landmark("Old Summer Camp", 150, 40);
            // woods small finds
            Tower(c, 186, 80, 4);
            c.Landmark("Ranger's Watchtower", 186, 84);
            c.Hut(160, 90, 8, 6, "plank", 0, true);
            c.Landmark("Ranger Station", 164, 97);
            c.Ring(72, 44, 1, 2, ScatterCover, 0, "brush");
            c.Landmark("Hunter's Blind", 72, 47);
            c.Deco(80, 82, "bones").Put(81, 82, ScatterCover, "crate");
            c.Stash("Dead Zealot", 82, 81);
            c.Landmark("Dead Zealot", 80, 84);
            c.Hut(10, 30, 6, 5, "plank", 1);
            c.Stash("Forester's Hut", 12, 31);
            c.Landmark("Forester's Hut", 12, 36);
            for (int x = 140; x < 156; x++) if (x % 3 != 0) c.Put(x, 104, HardCover, "trench");
            c.Landmark("Old Trench", 148, 106);
            c.Put(54, 100 - 10, Solid, "pole").Put(56, 91, Difficult, "rubble").Put(58, 92, Difficult, "rubble");
            c.Landmark("Fallen Pylon", 56, 94);
            c.Grove(4, 4, 196, 112, 0.05f, "redtree", "deadtree", (x, y) => In(x, y, "crimson_woods"));
            c.Grove(4, 4, 196, 112, 0.02f, "redbush", null, (x, y) => In(x, y, "crimson_woods"));

            // ================================================================ GHOST TOWN
            c.Road(5, Asphalt, (40, 199), (40, 150));
            c.Road(5, Asphalt, (6, 120), (104, 120));
            c.Road(4, Asphalt, (96, 96), (96, 120));
            c.Road(5, Asphalt, (6, 150), (122, 150));
            // Main Street (Mara's stall) and the Row Houses behind it
            c.Stamp("Main Street", 14, 102, 3, "plaster", Floor, 0, "debris");
            c.Landmark("Main Street", 48, 110);
            c.Stamp("Row Houses", 14, 126, 3, "wall", Floor, 0, "debris");
            c.Landmark("Row Houses", 42, 136);
            // the Church on the square
            c.Stamp("The Church", 78, 126, 4, "wall", Floor, 0, "logs");
            c.Landmark("The Church", 88, 138);
            // the Town Hall and its basement stair (Secret Lab Beta below)
            c.Block(12, 156, 18, 12, "plaster", 2);
            c.Hut(31, 158, 7, 6, "concrete", 1, false);
            c.Deco(34, 160, "hatch");
            c.Landmark("Town Hall Basement", 34, 162).Exit("secret_lab_beta", 34, 160);
            c.Landmark("Town Hall", 21, 170);
            // the school, the flats, the petrol station
            c.Block(48, 154, 30, 16, "wall", 2);
            TierDungeons.StampHouse(c, 48, 154, "school_ground", "wall", "School Stairs", "school_upper");   // Batch 120: a U round its yard, two floors
            c.Landmark("School", 62, 172);
            c.Block(84, 154, 14, 20, "plaster", 2);
            c.Block(10, 178, 16, 14, "plaster", 2);
            TierDungeons.StampHouse(c, 10, 178, "flats_ground", "plaster", "Stairwell", "flats_2");   // Batch 120: a T, three floors
            c.Landmark("Flats", 18, 194);
            c.Clear(62, 178, 26, 16, Asphalt, false);
            c.Block(64, 180, 10, 8, "plaster", 1, Asphalt);
            c.Put(78, 182, HardCover, "pillar").Put(78, 186, HardCover, "pillar").Put(83, 190, HardCover, "wreck");
            c.Landmark("Petrol Station", 76, 190);
            // town small finds
            c.Hut(86, 100, 8, 6, "plaster", 0);
            c.Stash("Laundromat", 88, 101);
            c.Landmark("Laundromat", 90, 108);
            c.Hut(4 + 46, 180, 7, 5, "plaster", 0);
            c.Landmark("Pharmacy", 53, 186);
            Tower(c, 30, 186, 4);
            c.Landmark("Balcony Perch", 30, 190);
            c.Put(90, 182, ScatterCover, "pole").Put(94, 182, ScatterCover, "pole").Put(92, 186, ScatterCover, "tires");
            c.Landmark("Playground", 92, 188);
            c.Put(60, 147, HardCover, "busstop").Put(61, 147, HardCover, "part");
            c.Landmark("Bus Stop", 60, 153);
            Sandbags(c, 4, 116, 6, 10, 1);
            c.Landmark("Old Checkpoint", 7, 126);

            // ================================================================ VANGUARD BARRACKS (Safe Zone)
            c.Road(5, Asphalt, (160, 199), (160, 178));
            c.Clear(124, 120, 63, 61, Gravel);
            c.Fence(124, 120, 186, 120, "fence", 29, 5).Fence(124, 180, 186, 180, "fence", 34, 5).Fence(124, 120, 124, 180, "fence", 28, 5).Fence(186, 120, 186, 180);
            // the Command Post
            c.Building(146, 123, 18, 8, "concrete", Concrete, (154, 130), (155, 130));
            for (int x = 144; x < 166; x += 2) c.Put(x, 132, HardCover, "sandbags");
            c.Landmark("Command Post", 155, 126);
            // Barracks Row
            c.Stamp("Barracks Row", 127, 136, 3, "wall", Floor, 0, "lockers");
            c.Landmark("Barracks Row", 155, 146);
            // the Parade Ground and the Motor Pool
            c.Paint(130, 158, 52, 8, Concrete);
            c.Landmark("Parade Ground", 155, 162);
            c.Rect(130, 168, 50, 10, Solid, 0, "concrete"); c.Rect(131, 169, 48, 8, Empty); c.Paint(130, 168, 50, 10, Concrete);
            for (int x = 134; x < 178; x += 8) c.Rect(x, 168, 3, 1, Empty);
            for (int x = 133; x < 178; x += 7) c.Put(x, 172, Solid, "wreck").Put(x + 1, 172, Solid, "wreck").Put(x + 3, 175, ScatterCover, "crate");
            c.Landmark("Motor Pool", 155, 174);
            Tower(c, 126, 122, 4); Tower(c, 183, 122, 4);
            c.Keep(124, 120, 63, 61);
            c.Landmark("arrive:vanguard_barracks", 155, 156);

            c.Exit("dried_lake", 40, 199).Exit("rail_yards", 160, 199);
            Ruin(c, 110, 108, 7, 6); Ruin(c, 192, 140, 6, 6, "concrete"); Ruin(c, 104, 190, 8, 6, "plaster");
            c.Scatter(ScatterCover, 0.006f);
            return Finish(c);
        }


        // ------------------------------------------------------------------ Tier 5

        /// <summary>A ramp straight across a terrace wall: from `fromH` outside to `toH` inside, 3 wide, `len` tiles,
        /// stepping (dx, dy) from (x, y).</summary>
        static void TerraceRamp(TerrainCanvas c, int x, int y, int dx, int dy, int len, int fromH, int toH)
        {
            for (int i = 0; i < len; i++)
            {
                int h = fromH + (int)Math.Round((toH - fromH) * (i + 1) / (double)(len + 1));
                for (int k = -1; k <= 1; k++)
                {
                    int px = x + dx * i + (dx == 0 ? k : 0), py = y + dy * i + (dy == 0 ? k : 0);
                    c.Put(px, py, Empty, null); c.Rect(px, py, 1, 1, Empty, h); c.Paint(px, py, 1, 1, Gravel);
                }
            }
        }

        // 200x200. Quarry Basin (the south: a terraced pit stepping down four levels to a flooded floor, ramps cut
        // between them; the Contractors' mining camp on the west rim, the Crane Rig, the Blast Shack over the
        // Labyrinth), the Ship Graveyard (north-west: The Hulks, the cannery, Captain's Row, the dry dock and its
        // tunnels, the lighthouse) and The Antenna Grid (north-east: the mast array, the Relay Station, the control
        // bunker, the transformer field, the fuel depot, the dead broadcast van over Secret Lab Gamma).
        // Exits: Crimson Woods south (x 100), the Abandoned City north (x 150).
        public static MapLayout Tier5()
        {
            var c = new TerrainCanvas(Size, Size, TerrainCanvas.Seed("tier5"));
            c.FillAll(Empty, 0);
            c.Regions((x, y) =>
            {
                float wob = (c.Noise(x, y, 20f, 121) - 0.5f) * 12f;
                if (y > 104 + wob * 0.4f && x < 172 + wob) return "quarry_basin";
                return x < 96 + wob ? "ship_graveyard" : "antenna_grid";
            });
            bool In(int x, int y, string r) => c.RegionAt(x, y) == r;

            // ---- ground
            c.PaintWhere(Gravel, (x, y) => In(x, y, "quarry_basin"));
            c.PaintNoise(Sand, 14f, 0.58f, 123, (x, y) => In(x, y, "quarry_basin"));
            c.PaintWhere(Sand, (x, y) => In(x, y, "ship_graveyard"));
            c.PaintNoise(Mud, 12f, 0.6f, 125, (x, y) => In(x, y, "ship_graveyard"));
            c.PaintWhere(Gravel, (x, y) => In(x, y, "antenna_grid"));
            c.PaintNoise(Dirt, 14f, 0.52f, 127, (x, y) => In(x, y, "antenna_grid"));
            c.PaintNoise(DarkGrass, 18f, 0.66f, 129, (x, y) => In(x, y, "antenna_grid"));

            WildEdge(c, (x, y) => In(x, y, "ship_graveyard") ? "junk" : In(x, y, "antenna_grid") ? "concrete" : "cliff");

            // ================================================================ QUARRY BASIN
            const int qx = 100, qy = 148;
            c.Road(5, Gravel, (100, 199), (100, 189));
            c.Ring(qx, qy, 34, 40, Empty, 0);
            c.Ring(qx, qy, 25, 33, Empty, -2);
            c.Ring(qx, qy, 16, 24, Empty, -4);
            c.Ring(qx, qy, 8, 15, Empty, -6);
            c.Ring(qx, qy, 0, 7, Difficult, -8);
            c.PaintBlob(qx, qy, 7, Water, 0.1f);
            c.Ring(qx, qy, 33, 34, Solid, 0, "cliff");
            c.Ring(qx, qy, 24, 25, Solid, -2, "cliff");
            c.Ring(qx, qy, 15, 16, Solid, -4, "cliff");
            c.Ring(qx, qy, 7, 8, Solid, -6, "cliff");
            // ramps through every wall, alternating sides so the way down winds round the pit
            TerraceRamp(c, qx, qy - 36, 0, 1, 4, 0, -2);  TerraceRamp(c, qx, qy + 36, 0, -1, 4, 0, -2);
            TerraceRamp(c, qx + 27, qy, -1, 0, 4, -2, -4); TerraceRamp(c, qx - 27, qy, 1, 0, 4, -2, -4);
            TerraceRamp(c, qx, qy + 18, 0, -1, 4, -4, -6); TerraceRamp(c, qx, qy - 18, 0, 1, 4, -4, -6);
            TerraceRamp(c, qx - 10, qy, 1, 0, 4, -6, -8);
            c.Landmark("The Terraces", qx, qy - 30).Landmark("Pit Floor", qx + 4, qy);
            // the Crane Rig: a dead excavator arm reaching out over the second terrace
            c.Line(qx + 30, qy + 16, qx + 22, qy + 10, 1, Solid, -2, true, "machine");
            c.Landmark("Crane Rig", qx + 30, qy + 18);
            // the Blast Shack on the east rim (the Underground Labyrinth)
            c.Building(146, 128, 8, 6, "concrete", Concrete, (146, 131));
            c.Deco(150, 130, "hatch").Put(152, 129, Barrel);
            c.Landmark("Blast Shack", 150, 131).Exit("underground_labyrinth", 150, 130);
            // the Contractors' mining camp on the west rim (place A)
            c.Clear(10, 116, 48, 60, Gravel, false);
            c.Fence(10, 116, 57, 116, "fence", 22, 4).Fence(10, 175, 57, 175, "fence", 20, 4).Fence(10, 116, 10, 175).Fence(57, 116, 57, 175, "fence", 28, 5);
            c.Block(14, 120, 16, 10, "concrete", 1, Concrete);
            c.Block(34, 120, 12, 8, "plaster", 1, Concrete);
            c.Block(14, 140, 12, 10, "wall", 1, Concrete);
            for (int i = 0; i < 6; i++) c.Rect(32 + (i % 3) * 7, 140 + (i / 3) * 6, 4, 2, Solid, 0, "container");
            c.Rect(16, 160, 2, 2, Solid, 0, "part").Put(16, 161, Solid, "tank").Rect(20, 160, 2, 2, Solid, 0, "part").Put(20, 161, Solid, "tank");
            Tower(c, 50, 120, 4); Tower(c, 50, 164, 4);
            c.Put(30, 165, HardCover, "wreck").Put(40, 168, HardCover, "machine");
            c.Landmark("Mining Camp", 34, 150).Landmark("Equipment Shed", 37, 143);
            // quarry small finds: the overturned excavator, the sandbag nest, the generator shed, the sinkhole
            c.Put(162, 180, HardCover, "machine").Put(163, 180, HardCover, "part").Put(164, 181, Difficult, "rubble");
            c.Landmark("Overturned Excavator", 163, 183);
            Sandbags(c, 126, 186, 8, 5, 0);
            c.Landmark("Sandbag Nest", 130, 192);
            c.Hut(64, 186, 6, 5, "metal", 1);
            c.Stash("Generator Shed", 65, 187);
            c.Landmark("Generator Shed", 67, 193);
            c.HeightBlob(150, 170, 3, -2); c.PaintBlob(150, 170, 3, Mud); c.Blob(150, 170, 2, Difficult, -2, 0.3f, "rubble");
            c.Landmark("Sinkhole", 150, 174);

            // ================================================================ SHIP GRAVEYARD
            // The Hulks (place B): one great freighter you clear room by room, on its own deck height
            c.Rect(10, 10, 40, 34, Solid, 2, "hull");
            c.Stamp("The Hulks", 12, 12, 6, "hull", Floor, 2);
            c.Rect(10, 42, 40, 2, Solid, 2, "hull");
            c.Ramp(29, 46, 0, -1, 5, 0, 2, 3);
            c.Landmark("The Hulks", 30, 24);
            // the cannery and the second wreck
            c.Block(58, 10, 26, 14, "wall", 1, Concrete);
            c.Landmark("Fish Cannery", 70, 26);
            c.Rect(58, 30, 30, 8, Solid, 2, "hull");
            c.Landmark("Rust Flat", 64, 50);
            // Captain's Row: the squatters' wrecks, one hold turned into their stash
            c.Clear(54, 64, 38, 34, Sand, false);
            c.Building(60, 68, 12, 7, "hull", Floor, (60, 71));
            c.Put(62, 69, ScatterCover, "crate").Put(66, 70, ScatterCover, "crate").Put(70, 72, HardCover, "lockers");
            c.Landmark("Captain's Row", 66, 76);
            for (int i = 0; i < 6; i++) c.Hut(56 + (i % 3) * 12, 80 + (i / 3) * 9, 8, 6, "metal", i % 2 == 0 ? 0 : 3);
            Fire(c, 76, 76, true);
            // the dry dock: a pit with a hull still in it, the pump tunnels below
            c.RectHeight(10, 56, 38, 38, -3); c.Paint(10, 56, 38, 38, Concrete);
            c.Rect(10, 56, 38, 1, Solid, -3, "concrete"); c.Rect(10, 93, 38, 1, Solid, -3, "concrete");
            c.Rect(10, 56, 1, 38, Solid, -3, "concrete"); c.Rect(47, 56, 1, 38, Solid, -3, "concrete");
            c.Rect(20, 64, 18, 20, Solid, -1, "hull");
            TerraceRamp(c, 29, 98, 0, -1, 6, 0, -3);
            c.Put(44, 90, Door, "metal");
            c.Landmark("Dry Dock", 29, 88).Landmark("Dock Pump House", 44, 88).Exit("drydock_tunnels", 44, 90);
            // ship small finds
            Tower(c, 6, 98, 6);
            c.Landmark("Lighthouse", 6, 102);
            c.Rect(50, 48, 3, 1, Solid, 0, "hull");
            c.Stash("Beached Lifeboat", 51, 49);
            c.Landmark("Beached Lifeboat", 51, 51);
            c.Deco(74, 54, "bones").Put(75, 54, ScatterCover, "crate");
            c.Landmark("Dead Diver", 74, 56);
            for (int i = 0; i < 7; i++) c.Put(86 + i % 3, 44 + i / 3, ScatterCover, "rustbarrel");
            c.Landmark("Buoy Pile", 87, 48);
            c.Hut(86, 6, 6, 5, "plank", 0);
            c.Stash("Hermit's Shack", 88, 7);
            c.Landmark("Hermit's Shack", 89, 13);
            for (int x = 30; x < 48; x += 2) c.Deco(x, 50, "rail");
            c.Landmark("Anchor Chains", 38, 52);

            // ================================================================ THE ANTENNA GRID
            c.Road(4, Asphalt, (150, 0), (150, 40));
            c.Road(4, Asphalt, (150, 80), (150, 104), (120, 112));
            // The Array: masts in a grid across the field
            for (int gx = 106; gx < 196; gx += 12)
                for (int gy = 10; gy < 102; gy += 12)
                    if (!(gx > 124 && gx < 176 && gy > 34 && gy < 86)) c.Put(gx, gy, Solid, "antenna");
            c.Landmark("The Array", 118, 22);
            // the Relay Station (place C, the Obelisk outpost)
            c.Clear(128, 38, 46, 46, Concrete, false);
            c.Stamp("Relay Station", 138, 48, 5, "concrete", Concrete, 0, "crate");
            c.Fence(128, 38, 173, 38, "fence", 20, 5).Fence(128, 83, 173, 83, "fence", 20, 5).Fence(128, 38, 128, 83).Fence(173, 38, 173, 83);
            Tower(c, 130, 40, 5); Tower(c, 169, 76, 5);
            c.Landmark("Relay Station", 150, 60);
            // the control bunker, the transformer field, the fuel depot
            c.Block(104, 84, 14, 10, "concrete", 1, Concrete);
            c.Landmark("Control Bunker", 111, 96);
            c.Clear(176, 18, 20, 26, Gravel);
            c.Fence(176, 18, 195, 18).Fence(176, 43, 195, 43, "fence", 8, 3).Fence(176, 18, 176, 43).Fence(195, 18, 195, 43);
            for (int tx = 179; tx < 194; tx += 4) for (int ty = 21; ty < 41; ty += 5) c.Put(tx, ty, HardCover, "machine");
            c.Landmark("Transformer Field", 185, 46);
            c.Clear(176, 116, 20, 60, Concrete, false);
            for (int ty = 120; ty < 170; ty += 10) { c.Rect(178, ty, 2, 2, Solid, 0, "part"); c.Put(178, ty + 1, Solid, "tank"); c.Rect(186, ty, 2, 2, Solid, 0, "part"); c.Put(186, ty + 1, Solid, "tank"); }
            c.Landmark("Fuel Depot", 184, 172);
            // the dead broadcast van (Secret Lab Gamma)
            c.PaintBlob(186, 96, 4, Mud);
            c.Put(185, 96, Solid, "wreck").Put(186, 96, Solid, "wreck");
            c.Landmark("Dead Broadcast Van", 187, 94).Exit("secret_lab_gamma", 187, 98);
            // Batch 88: a rusted hatch on the old slipway, down to the Lattice Foundry (hidden: G7Lab)
            c.Deco(14, 63, "hatch");
            c.Landmark("Flooded Slipway", 14, 62).Exit("lattice_foundry", 14, 63);
            // antenna small finds: the mast perch, the bone circle, the radio van, the generator hut
            Tower(c, 112, 50, 6);
            c.Stash("Mast Perch", 113, 51);
            c.Landmark("Mast Perch", 112, 54);
            c.Ring(120, 70, 3, 3, ScatterCover, 0, "stones"); c.Deco(120, 70, "bones");
            c.Landmark("Bone Circle", 120, 74);
            c.Put(160, 96, HardCover, "wreck").Put(161, 96, Solid, "antenna");
            c.Landmark("Radio Van", 160, 98);
            c.Hut(186, 186 - 6, 6, 5, "concrete", 2);
            c.Stash("Generator Hut", 189, 181);
            c.Landmark("Generator Hut", 186, 188);

            c.Exit("crimson_woods", 100, 199).Exit("abandoned_city", 150, 0);
            Ruin(c, 100, 60, 7, 6, "concrete"); Ruin(c, 60, 104, 8, 6); Ruin(c, 160, 192, 6, 5, "concrete");
            Poles(c, 104, 108, 170, 108, 10);
            c.Scatter(ScatterCover, 0.006f);
            return Finish(c);
        }


        // ------------------------------------------------------------------ Tier 6

        // 200x200. The Abandoned City (south and middle: Highrise Row, the Central Plaza and its subway entrance over the
        // Underground Bunker, the Garrison Barricade, the department store, the cinema, the police station, the parking
        // garage, the tram depot - the city pass makes streets of the gaps), The Siege Ward (north: a walled ward with the
        // trench lines across it, the Bell Tower over the Siege Catacombs, the field hospital ruins, the hospital and its
        // morgue; the way north to the Reactor Core) and the Anomalous Epicenter (west: the crater, The Wound, the
        // scattered relics - a dead end). Exits: the Antenna Grid south (x 150), the Reactor Core north (x 130).
        public static MapLayout Tier6()
        {
            var c = new TerrainCanvas(Size, Size, TerrainCanvas.Seed("tier6"));
            c.FillAll(Empty, 0);
            c.Regions((x, y) =>
            {
                float wob = (c.Noise(x, y, 20f, 141) - 0.5f) * 12f;
                if (x < 58 + wob) return "outskirts";
                return y < 80 + wob * 0.4f ? "hospital" : "abandoned_city";
            });
            bool In(int x, int y, string r) => c.RegionAt(x, y) == r;

            // ---- ground: the city is left bare for the street pass; the ward is churned earth; the epicenter dead mud
            c.PaintWhere(Dirt, (x, y) => In(x, y, "abandoned_city"));
            c.PaintWhere(Dirt, (x, y) => In(x, y, "hospital"));
            c.PaintNoise(Mud, 10f, 0.55f, 143, (x, y) => In(x, y, "hospital"));
            c.PaintWhere(Dirt, (x, y) => In(x, y, "outskirts"));
            c.PaintNoise(Mud, 12f, 0.5f, 145, (x, y) => In(x, y, "outskirts"));

            WildEdge(c, (x, y) => In(x, y, "outskirts") ? "cliff" : "plaster");

            // ================================================================ THE ABANDONED CITY
            c.Road(5, Asphalt, (150, 199), (150, 182));
            // Highrise Row
            c.Stamp("Highrise Row", 80, 86, 5, "plaster", Floor, 0, "debris");
            c.Landmark("Highrise Row", 107, 98);
            // the Central Plaza, its dead fountain, the subway entrance (Underground Bunker)
            c.Clear(110, 120, 40, 30, Concrete);
            c.Rect(128, 133, 4, 4, Solid, 1, "fountain");
            c.Put(114, 124, HardCover, "busstop").Put(146, 124, HardCover, "dumpster").Put(118, 144, ScatterCover, "trash");
            c.Landmark("Central Plaza", 130, 128);
            c.Rect(144, 145, 4, 3, Difficult, -1, "rubble");
            c.Deco(146, 147, "hatch");
            c.Landmark("Subway Entrance", 146, 145).Exit("underground_bunker", 146, 147);
            // the Garrison Barricade (their field quartermaster, the story's Act 4)
            c.Clear(148, 158, 40, 24, Concrete, false);
            c.Stamp("Garrison Barricade", 150, 160, 4, "concrete", Concrete);
            Tower(c, 186, 160, 5);
            for (int x = 150; x < 186; x += 2) c.Put(x, 182, HardCover, "sandbags");
            c.Landmark("Garrison Barricade", 168, 170);
            // the big buildings round the plaza
            c.Block(64, 120, 30, 22, "plaster", 2);
            c.Landmark("Department Store", 79, 144);
            c.Block(64, 150, 20, 14, "wall", 2);
            c.Landmark("Police Station", 74, 166);
            c.Block(92, 160, 22, 16, "concrete", 2);
            c.Landmark("Parking Garage", 103, 178);
            c.Block(160, 120, 20, 14, "plaster", 2);
            c.Landmark("Cinema", 170, 136);
            c.Block(180, 88, 16, 22, "plaster", 2);
            c.Block(140, 88, 16, 20, "plaster", 2);
            c.Block(118, 182, 14, 12, "plaster", 2);
            // the tram depot
            c.Clear(62, 176, 30, 20, Asphalt, false);
            for (int x = 62; x < 92; x++) { c.Deco(x, 182, "rail"); c.Deco(x, 190, "rail"); }
            for (int i = -2; i <= 2; i++) { c.Put(72 + i, 182, HardCover, i == 0 ? "bus" : "part"); c.Put(82 + i, 190, HardCover, i == 0 ? "bus" : "part"); }
            c.Landmark("Tram Depot", 76, 186);
            // city small finds: the burned tram, the clock tower perch, the dead scout, the cart barricade, the evac buses
            for (int i = -2; i <= 2; i++) c.Put(124 + i, 114, HardCover, i == 0 ? "bus" : "part");
            c.Landmark("Burned Tram", 124, 116);
            Tower(c, 156, 112, 6);
            c.Stash("Clock Tower", 157, 113);
            c.Landmark("Clock Tower", 156, 116);
            c.Deco(100, 150, "bones").Put(101, 150, ScatterCover, "crate");
            c.Stash("Dead Scout", 102, 149);
            c.Landmark("Dead Scout", 100, 152);
            for (int x = 134; x < 146; x++) if (x % 2 == 0) c.Put(x, 116, ScatterCover, "trash");
            c.Landmark("Cart Barricade", 140, 118);
            for (int i = -2; i <= 2; i++) { c.Put(190 + i, 140, HardCover, i == 0 ? "bus" : "part"); c.Put(190 + i, 146, HardCover, i == 0 ? "bus" : "part"); }
            c.Landmark("Evacuation Buses", 190, 150);
            c.Rect(196 - 8, 186, 6, 5, Solid, 0, "plaster");
            c.Put(186, 192, HardCover, "dumpster");
            c.Stash("Kiosk", 187, 192);
            c.Landmark("Kiosk", 187, 195);

            // ================================================================ THE SIEGE WARD
            for (int x = 62; x <= 198; x++)
            {
                if (Math.Abs(x - 130) > 3) c.Put(x, 4, Solid, "plaster");
                if (Math.Abs(x - 100) > 3 && Math.Abs(x - 160) > 3) c.Put(x, 76, Solid, "plaster");
            }
            for (int y = 4; y <= 76; y++) { c.Put(62, y, Solid, "plaster"); c.Put(198, y, Solid, "plaster"); }
            c.Road(5, Asphalt, (130, 0), (130, 6));
            // the trenches across the ward (difficult to cross, hard cover from outside)
            c.Stamp("The Trenches", 66, 30, 4, "cliff", Dirt);
            for (int y = 30; y < 58; y++)
                for (int x = 66; x < 150; x++)
                    if (c.Get(x, y) == Difficult) c.Put(x, y, HardCover, "trench");
            c.Landmark("The Trenches", 108, 36);
            Sandbags(c, 126, 62, 10, 6, 0);
            c.Put(130, 64, HardCover, "machine");
            c.Landmark("Mortar Pit", 131, 66);
            // the hospital (place B) and its morgue stair
            c.Block(68, 8, 46, 18, "plaster", 1, Asphalt);
            TierDungeons.StampPlan(c, 68, 8, TierDungeons.LabPlans["hospital"], "plaster", Floor);   // Batch 92: a shelled hospital held as a fort
            c.Keep(68, 8, 46, 18);
            c.Landmark("Siege Ward Hospital", 91, 27);
            c.Landmark("Garrison Armory", 76, 14);   // Batch 87: GarrisonRaid
            c.Hut(118, 10, 7, 6, "concrete", 0, false);
            c.Deco(121, 12, "hatch");
            c.Landmark("Morgue Stair", 121, 17).Exit("hospital_morgue", 121, 12);
            // the Bell Tower: shattered church tower, stair to the top, the catacomb stair beneath
            c.Building(160, 8, 12, 12, "plaster", Floor, (165, 19));
            c.Rect(163, 10, 4, 3, Empty, 6, "platform");
            c.Ramp(164, 17, 0, -1, 5, 0, 6, 2, "platform");
            c.Deco(169, 16, "hatch");
            c.Landmark("Bell Tower", 165, 11).Landmark("Catacomb Stair", 169, 16).Exit("siege_catacombs", 169, 16);
            // the Field Hospital Ruins
            c.Building(164, 34, 24, 16, "plaster", LabTile, (175, 34), (164, 42));
            c.Blob(181, 44, 3, Difficult, 0, 0.4f, "rubble");
            c.Put(168, 37, ScatterCover, "lockers").Put(172, 46, ScatterCover, "crate").Deco(170, 46, "bones").Put(184, 37, HardCover, "lockers");
            c.Landmark("Field Hospital Ruins", 172, 42);
            for (int i = 0; i < 3; i++) c.Put(152 + i * 5, 64, HardCover, "wreck");
            c.Landmark("Ambulances", 157, 66);
            c.Exit("reactor_core", 130, 0);

            // ================================================================ THE ANOMALOUS EPICENTER
            c.Ring(28, 110, 22, 26, Solid, 2, "cliff");
            foreach (var (gx, gy) in new[] { (54, 108), (28, 86), (28, 134), (4, 110) }) c.Blob(gx, gy, 3, Empty, 0, 0.1f);
            c.Landmark("The Crater Rim", 50, 100);
            c.HeightBlob(28, 110, 18, -2);
            c.HeightBlob(28, 110, 10, -4);
            c.PaintBlob(28, 110, 16, Mud);
            c.Blob(28, 110, 5, Difficult, -4, 0.5f, "rubble");
            c.Landmark("The Wound", 28, 110);
            foreach (var (rx, ry) in new[] { (12, 30), (40, 20), (20, 60), (44, 160), (14, 180), (36, 190), (16, 96), (40, 124) })
            {
                c.Put(rx, ry, Solid, "wreck");
                c.Deco(rx + 1, ry + 1, "bones"); c.Put(rx - 1, ry, ScatterCover, "crate");
            }
            c.Landmark("Scattered Relics", 13, 31);
            c.Stash("Relic Cache", 41, 21).Landmark("Relic Cache", 41, 23);
            c.Put(44, 60, Solid, "deadtree").Put(45, 61, Difficult, "brush");
            c.Landmark("Petrified Tree", 44, 63);
            c.Rect(16, 150, 8, 1, Solid, 0, "plaster");
            c.Landmark("Obelisk Wall", 20, 152);
            // the dead land round the crater: little craters, rock fields, a wrecked expedition camp, standing stones
            foreach (var (kx, ky, kr) in new[] { (14, 50, 4), (40, 44, 3), (20, 166, 5), (46, 182, 3), (10, 140, 3), (48, 72, 3), (30, 14, 3) })
            { c.HeightBlob(kx, ky, kr, -1); c.PaintBlob(kx, ky, kr, Mud); c.Blob(kx, ky, kr / 2, Difficult, -1, 0.5f, "rubble"); }
            c.Grove(4, 4, 54, 192, 0.012f, "rock", null, (x, y) => In(x, y, "outskirts"));
            c.Clear(30, 60, 16, 12, Dirt, false);
            c.Hut(32, 62, 5, 4, "metal", 0, true).Hut(39, 62, 5, 4, "metal", 0, false);
            c.Put(34, 69, ScatterCover, "crate").Put(41, 69, Difficult, "rubble").Deco(37, 68, "bones").Deco(43, 66, "bones");
            c.Landmark("Lost Expedition", 38, 72);
            c.Stash("Expedition Locker", 33, 63);
            for (int i = 0; i < 7; i++) { double a = i * 0.9; c.Put(24 + (int)(Math.Cos(a) * 6), 178 + (int)(Math.Sin(a) * 5), Solid, "pillar"); }
            c.Landmark("Standing Stones", 24, 186);
            c.Grove(4, 4, 54, 192, 0.05f, "deadtree", null, (x, y) => In(x, y, "outskirts"));
            c.Scatter(Difficult, 0.01f, Empty, 0, "brush");

            c.Exit("antenna_grid", 150, 199);
            Ruin(c, 70, 100, 7, 6, "plaster"); Ruin(c, 190, 60, 6, 6, "plaster"); Ruin(c, 76, 66, 8, 6, "plaster");
            c.Scatter(ScatterCover, 0.006f);
            return Finish(c);
        }

        static MapLayout Finish(TerrainCanvas c)
        {
            var m = c.ToMapLayout();
            m.FullSize = true;
            return m;
        }
    }
}
