// The map overhaul's dungeons: small underground maps (about 40x40) under a tier map's places, entered by a
// hatch, a door or a culvert. Each is authored at its final size (FullSize) out of solid rock or concrete with
// rooms and passages cut into it; nothing is dressed over them. Their way out is the "exit:<region>" anchor at
// the entrance, so you come back up where you went down.
using System;
using System.Collections.Generic;
using GameData;
using static GameCore.TileType;
using static GameCore.GroundType;

namespace GameCore
{
    public static partial class TierDungeons
    {
        // ------------------------------------------------------------------ shared pieces

        /// <summary>A room cut out of the mass: open floor, its own ground.</summary>
        static void Room(TerrainCanvas c, int x, int y, int w, int h, GroundType floor)
        {
            c.Rect(x, y, w, h, Empty);
            c.Paint(x, y, w, h, floor);
        }

        /// <summary>A passage between two points, `width` wide, along an L (first along x, then y).</summary>
        static void Passage(TerrainCanvas c, int x1, int y1, int x2, int y2, int width, GroundType floor)
        {
            c.Line(x1, y1, x2, y1, width, Empty);
            c.Line(x2, y1, x2, y2, width, Empty);
            c.PaintLine(x1, y1, x2, y1, width, floor);
            c.PaintLine(x2, y1, x2, y2, width, floor);
        }

        /// <summary>A natural cave pocket: a rough blob of floor with a few rocks standing in it.</summary>
        static void Cavern(TerrainCanvas c, int x, int y, int r, GroundType floor = CaveFloor)
        {
            c.Blob(x, y, r, Empty, 0, 0.45f);
            c.PaintBlob(x, y, r + 1, floor, 0.45f);
        }

        /// <summary>Knee-deep water (difficult) over part of a room.</summary>
        static void Flood(TerrainCanvas c, int x, int y, int w, int h)
        {
            for (int yy = y; yy < y + h; yy++)
                for (int xx = x; xx < x + w; xx++)
                    if (c.Get(xx, yy) == Empty && c.LookAt(xx, yy) == null) { c.Put(xx, yy, Difficult); c.Paint(xx, yy, 1, 1, Water); }
        }

        static MapLayout Finish(TerrainCanvas c, string wallLook)
        {
            c.Border(Solid, 1, 0, wallLook);
            var m = c.ToMapLayout();
            m.FullSize = true;
            return m;
        }

        // ------------------------------------------------------------------ Tier 1

        // Rust Culvert cave (Secret Cave Area): the road's drainage pipe opens into a natural cave system that
        // runs under the fields - a pool, a squeeze, and the smugglers' old stash nook at the far end.
        /// <summary>2026-11-10: the lock-and-key redo (ArtSource/plans/dungeon_plans.py secret_cave, graph dungeons/secret_cave.mmd): 'E' the culvert pipe in, 'G' the stash nook,
        /// '4' the smugglers' crate with the pry bar, 'R' the rockfall (three tiles across the east tunnel), 'K' / 'J' stashes.</summary>
        public static MapLayout SecretCave()
        {
            var marks = new Dictionary<char, (string, string)> { ['E'] = ("Cave Mouth", "perimeter"), ['G'] = ("Stash Nook", null),
                ['N'] = ("Dark Niche West", null), ['S'] = ("Dark Niche South", null), ['T'] = ("Dark Niche East", null), ['Q'] = ("Mutant Den", null) };   // the lairs (DungeonLairs.cs)
            var stashes = new Dictionary<char, string>(LabLocks.StashesOf("secret_cave")) { ['K'] = "Stash Nook", ['J'] = "Wet Cache" };
            return FloorPlan("secret_cave", LabPlans["secret_cave"], "cliff", CaveFloor, marks, stashes);
        }

        // Chapel Crypt: steps down from the chapel into burial passages, an ossuary, a collapsed side crypt,
        // and the family vault at the end with its slab pushed aside.
        /// <summary>2026-11-10: the lock-and-key redo (ArtSource/plans/dungeon_plans.py chapel_crypt, graph dungeons/chapel_crypt.mmd): 'E' the stair, '3' the burial ledger (the code),
        /// '4' the priest's strongbox (the gate key), '$' the iron gate, '^' the sealed slab, 'G' the vault, 'O' the ossuary, 'J' 'K' stashes.</summary>
        public static MapLayout ChapelCrypt()
        {
            var marks = new Dictionary<char, (string, string)> { ['E'] = ("Crypt Stair", "perimeter"), ['G'] = ("Sealed Vault", null), ['O'] = ("Ossuary", null) };
            var stashes = new Dictionary<char, string>(LabLocks.StashesOf("chapel_crypt")) { ['K'] = "Sealed Vault", ['J'] = "Collapsed Crypt" };
            return FloorPlan("chapel_crypt", LabPlans["chapel_crypt"], "wall", Concrete, marks, stashes);
        }

        // Pumping Station Basement: the stair down from the pump room, a flooded pump hall with the dead pumps,
        // a valve gallery, the control room above the waterline and a drainage culvert running off east.
        /// <summary>2026-11-10: the lock-and-key redo (ArtSource/plans/dungeon_plans.py pumping_basement, graph dungeons/pumping_basement.mmd): 'E' the stair in,
        /// 'G' the outfall (the goal room, daylight through a grating), '4' the fuse cabinet, '5' the breaker, '6' the culvert pump, 'W' the culvert's deep water, '2' the pump panel's power door.</summary>
        public static MapLayout PumpingBasement()
        {
            var marks = new Dictionary<char, (string, string)> { ['E'] = ("Basement Stair", "marshes"), ['G'] = ("Outfall", null),
                ['H'] = ("Pump Hall", null), ['C'] = ("Control Room", null), ['V'] = ("Valve Gallery", null) };
            var stashes = new Dictionary<char, string>(LabLocks.StashesOf("pumping_basement")) { ['J'] = "Maintenance Box", ['K'] = "Control Room", ['L'] = "Outfall Cache" };
            return FloorPlan("pumping_basement", LabPlans["pumping_basement"], "concrete", Concrete, marks, stashes);
        }

        // The Flooded Bunker: behind the locked blast door, a decontamination lock, barracks, the armory, and the
        // command level half under water. The best loot in Tier 1.
        public static MapLayout FloodedBunker()
        {
            var c = new TerrainCanvas(42, 42, TerrainCanvas.Seed("flooded_bunker"));
            for (int y = 0; y < 42; y++) for (int x = 0; x < 42; x++) c.Put(x, y, Solid, "concrete");
            // the blast door and the decontamination lock
            Room(c, 18, 2, 6, 4, Concrete);
            c.Landmark("Blast Door", 20, 3).Exit("marshes", 20, 3);
            Room(c, 18, 7, 6, 4, LabTile);
            c.Put(20, 6, Door, "metal").Put(20, 11, Door, "metal");
            c.Put(18, 8, HardCover, "lockers").Put(23, 8, HardCover, "lockers");
            // the main corridor
            Room(c, 6, 12, 30, 3, Concrete);
            // barracks west
            Room(c, 4, 16, 12, 9, Floor);
            c.Put(9, 15, Door);
            for (int y = 17; y < 24; y += 2) { c.Put(5, y, ScatterCover, "crate"); c.Put(14, y, HardCover, "lockers"); }
            c.Landmark("Barracks", 10, 20);
            // the armory east, behind a second door
            Room(c, 27, 16, 10, 7, Floor);
            c.Put(31, 15, Door, "metal");
            c.Put(28, 17, HardCover, "lockers").Put(30, 17, HardCover, "lockers").Put(35, 21, ScatterCover, "crate").Put(28, 21, ScatterCover, "crate");
            c.Landmark("Armory", 32, 19).Stash("Armory", 35, 17);
            // the stairs down to the command level
            Room(c, 18, 15, 6, 10, Concrete);
            // the command level, flooded
            Room(c, 6, 27, 30, 11, Concrete);
            Passage(c, 20, 25, 20, 27, 3, Concrete);
            c.Put(10, 30, ScatterCover, "desk").Put(14, 30, ScatterCover, "desk").Put(26, 30, HardCover, "machine").Put(30, 30, HardCover, "lockers");
            c.Put(20, 34, HardCover, "pillar").Put(12, 34, HardCover, "pillar").Put(28, 34, HardCover, "pillar");
            Flood(c, 6, 31, 30, 7);
            c.Landmark("Command Room", 20, 30).Stash("Command Safe", 33, 29);
            // a generator closet off the corridor
            Room(c, 37, 10, 3, 6, Concrete);
            c.Put(38, 11, HardCover, "machine");
            return Finish(c, "concrete");
        }

        // The Smugglers' Tunnel: a dug tunnel from the depot's loading office, shored with planks, with side caches,
        // the crews' den and the cache room at the far end.
        /// <summary>2026-11-10: the lock-and-key redo (ArtSource/plans/dungeon_plans.py smugglers_tunnel, graph dungeons/smugglers_tunnel.mmd): 'E' the hatch, '4' the guard post's
        /// footlocker with the gate key, 'F' the iron gate (two tiles across the tunnel), 'M' the den, 'G' the cache room, 'K' 'J' 'L' stashes.</summary>
        public static MapLayout SmugglersTunnel()
        {
            var marks = new Dictionary<char, (string, string)> { ['E'] = ("Hatch Ladder", "smugglers_run"), ['G'] = ("Cache Room", null), ['M'] = ("Smugglers' Den", null) };
            var stashes = new Dictionary<char, string>(LabLocks.StashesOf("smugglers_tunnel")) { ['K'] = "Smugglers' Cache", ['J'] = "Side Cache", ['L'] = "Dug-out Cache" };
            return FloorPlan("smugglers_tunnel", LabPlans["smugglers_tunnel"], "cliff", CaveFloor, marks, stashes);
        }

        // ------------------------------------------------------------------ Tier 2

        // The Crusher Pit: under Crusher Row, the hydraulic rams' pit and the service tunnels round it - oil, scrap
        // and black water. Mutants nest in the sump.
        public static MapLayout CrusherPit()
        {
            var c = new TerrainCanvas(40, 40, TerrainCanvas.Seed("crusher_pit"));
            for (int y = 0; y < 40; y++) for (int x = 0; x < 40; x++) c.Put(x, y, Solid, "concrete");
            Room(c, 17, 2, 6, 4, Concrete);
            c.Landmark("Pit Ladder", 19, 3).Exit("scrapyard", 19, 3);
            // the ram hall: a long pit with the rams standing in it
            Room(c, 4, 8, 32, 10, Concrete);
            Passage(c, 19, 6, 19, 8, 3, Concrete);
            for (int x = 7; x < 34; x += 6) { c.Rect(x, 11, 2, 3, Solid, 0, "part"); c.Put(x, 13, Solid, "machine"); }
            Flood(c, 4, 15, 32, 3);
            c.Landmark("Ram Hall", 19, 10);
            // service tunnels round the sides, junk choking parts of them
            Room(c, 2, 19, 3, 18, Concrete); Room(c, 35, 19, 3, 18, Concrete);
            Passage(c, 3, 17, 3, 19, 3, Concrete); Passage(c, 36, 17, 36, 19, 3, Concrete);
            c.Blob(3, 27, 1, Solid, 0, 0.3f, "junk"); c.Put(2, 27, Empty).Paint(2, 27, 1, 1, Concrete);
            c.Put(36, 24, ScatterCover, "scrap").Put(37, 30, ScatterCover, "tires");
            // the oil sump at the bottom
            Room(c, 6, 30, 28, 8, Concrete);
            Passage(c, 5, 34, 6, 34, 2, Concrete); Passage(c, 34, 34, 35, 34, 2, Concrete);
            Flood(c, 12, 32, 16, 5);
            c.Put(8, 31, ScatterCover, "rustbarrel").Put(9, 31, ScatterCover, "rustbarrel").Put(31, 36, ScatterCover, "rustbarrel");
            c.Landmark("Oil Sump", 20, 34);
            // the foreman's cage
            Room(c, 14, 21, 12, 6, Floor);
            Passage(c, 19, 18, 19, 21, 2, Concrete); Passage(c, 19, 27, 19, 30, 2, Concrete);
            c.Put(15, 22, ScatterCover, "desk").Put(24, 22, HardCover, "lockers").Put(15, 25, ScatterCover, "crate");
            c.Stash("Foreman's Locker", 24, 25);
            c.Landmark("Foreman's Cage", 20, 24);
            return Finish(c, "concrete");
        }

        // The Valley Mine: the adit off Black Valley's north rim, timbered galleries with the ore rails, a flooded
        // drift, a collapse, and the miners' rest room at the far end.
        /// <summary>2026-11-10: the lock-and-key redo (ArtSource/plans/dungeon_plans.py valley_mine, graph dungeons/valley_mine.mmd): 'E' the adit, '4' the tool shed's rack (the pry bar),
        /// '@' the collapse across the gallery (and one fall in the ventilation crawl), '|' the rails, '+' timber props, 'G' the miners' rest, 'N' the main gallery, 'L' the flooded drift.</summary>
        public static MapLayout ValleyMine()
        {
            var marks = new Dictionary<char, (string, string)> { ['E'] = ("Adit", "black_valley"), ['G'] = ("Miners' Rest", null), ['N'] = ("Main Gallery", null), ['L'] = ("Flooded Drift", null) };
            var stashes = new Dictionary<char, string>(LabLocks.StashesOf("valley_mine")) { ['K'] = "Miners' Strongbox", ['J'] = "Drowned Cache" };
            return FloorPlan("valley_mine", LabPlans["valley_mine"], "cliff", CaveFloor, marks, stashes);
        }

        // ------------------------------------------------------------------ Tier 3

        // The Stranded Ferry: the lake ferry beached when the water went - the car deck with its wrecks, the passenger
        // lounge, a row of cabins, the engine room with water in the bilge, and the captain's bridge.
        public static MapLayout StrandedFerry()
        {
            var c = new TerrainCanvas(46, 30, TerrainCanvas.Seed("stranded_ferry"));
            for (int y = 0; y < 30; y++) for (int x = 0; x < 46; x++) c.Put(x, y, Solid, "hull");
            // the car deck: the door from the lakebed opens onto it
            Room(c, 4, 4, 36, 10, Concrete);
            c.Landmark("Car Deck", 36, 8).Exit("dried_lake", 38, 8);
            for (int x = 7; x < 34; x += 6) { c.Put(x, 6, HardCover, "wreck"); c.Put(x + 2, 11, HardCover, "wreck"); }
            c.Put(20, 8, ScatterCover, "crate").Put(28, 9, ScatterCover, "tires");
            // the cabins: a corridor with rooms off it
            Room(c, 4, 16, 36, 2, Floor);
            Passage(c, 6, 14, 6, 16, 2, Floor);
            for (int x = 4; x < 38; x += 6)
            {
                Room(c, x, 19, 5, 4, Floor);
                c.Put(x + 2, 18, Door);
                if (x % 12 == 4) c.Put(x + 1, 21, ScatterCover, "crate"); else c.Put(x + 3, 20, ScatterCover, "lockers");
            }
            c.Stash("Cabin Locker", 35, 21);
            // the passenger lounge aft
            Room(c, 41, 4, 3, 22, Floor);
            Passage(c, 39, 9, 41, 9, 2, Concrete); Passage(c, 39, 16, 41, 16, 2, Floor);
            c.Put(42, 12, ScatterCover, "armchair").Put(42, 20, ScatterCover, "armchair").Put(42, 24, ScatterCover, "desk");
            c.Landmark("Passenger Lounge", 42, 15);
            // the engine room forward, flooded bilge
            Room(c, 1, 24, 22, 4, Concrete);
            Passage(c, 2, 18, 2, 24, 2, Floor);
            c.Put(5, 25, HardCover, "machine").Put(9, 25, HardCover, "machine").Put(13, 25, HardCover, "machine");
            Flood(c, 1, 27, 22, 1);
            c.Landmark("Engine Room", 10, 26);
            // the captain's bridge up front
            Room(c, 25, 24, 15, 4, Floor);
            Passage(c, 23, 25, 25, 25, 2, Floor);
            c.Put(27, 25, ScatterCover, "desk").Put(31, 25, ScatterCover, "desk").Put(37, 26, HardCover, "lockers");
            c.Stash("Captain's Safe", 38, 26);
            c.Landmark("Captain's Bridge", 32, 26);
            return Finish(c, "hull");
        }
    
        // ------------------------------------------------------------------ Tier 4

        // The Camp Shelter: the summer camp's Cold War bomb shelter - the stair and blast door, decontamination, the
        // dormitory, the mess, the generator and filter rooms, and the shelter command post at the back.
        public static MapLayout CampShelter()
        {
            var c = new TerrainCanvas(42, 40, TerrainCanvas.Seed("camp_shelter"));
            for (int y = 0; y < 40; y++) for (int x = 0; x < 42; x++) c.Put(x, y, Solid, "concrete");
            Room(c, 18, 2, 6, 4, Concrete);
            c.Landmark("Shelter Stair", 20, 3).Exit("crimson_woods", 20, 3);
            Room(c, 18, 7, 6, 4, LabTile);
            c.Put(20, 6, Door, "metal").Put(21, 11, Door, "metal");
            c.Put(18, 8, HardCover, "lockers").Put(23, 8, HardCover, "lockers");
            // the central hall
            Room(c, 6, 12, 30, 4, Concrete);
            // the dormitory west
            Room(c, 3, 17, 14, 12, Floor);
            c.Put(9, 16, Door);
            for (int y = 18; y < 28; y += 3) { c.Put(4, y, ScatterCover, "crate"); c.Put(15, y, HardCover, "lockers"); }
            c.Landmark("Dormitory", 9, 22);
            // the mess east
            Room(c, 24, 17, 14, 8, Floor);
            c.Put(30, 16, Door);
            for (int x = 26; x < 36; x += 3) c.Put(x, 20, ScatterCover, "desk");
            c.Put(36, 23, ScatterCover, "crate");
            // the generator and the air filters
            Room(c, 18, 17, 5, 12, Concrete);
            Passage(c, 20, 16, 20, 17, 2, Concrete);
            c.Put(19, 19, HardCover, "machine").Put(19, 23, HardCover, "machine").Put(21, 27, HardCover, "machine");
            c.Landmark("Generator Room", 20, 21);
            Room(c, 24, 26, 14, 4, Concrete);
            Passage(c, 30, 25, 30, 26, 2, Concrete);
            for (int x = 25; x < 37; x += 3) c.Deco(x, 27, "pipe");
            // the shelter command post
            Room(c, 8, 31, 26, 6, Floor);
            Passage(c, 10, 29, 10, 31, 2, Floor); Passage(c, 31, 30, 31, 31, 2, Concrete);
            c.Put(12, 32, ScatterCover, "desk").Put(16, 32, ScatterCover, "desk").Put(22, 32, HardCover, "machine").Put(31, 35, HardCover, "lockers");
            c.Stash("Shelter Safe", 32, 35);
            c.Landmark("Shelter Command", 20, 34);
            return Finish(c, "concrete");
        }
    
        // ------------------------------------------------------------------ Tier 5

        // The Dry Dock Tunnels: under the dock's pump house, the culverts that drained the dock - a pump gallery, the
        // flooded main culvert, the sluice chamber, and the old ship-fitters' store.
        public static MapLayout DrydockTunnels()
        {
            var c = new TerrainCanvas(44, 40, TerrainCanvas.Seed("drydock_tunnels"));
            for (int y = 0; y < 40; y++) for (int x = 0; x < 44; x++) c.Put(x, y, Solid, "concrete");
            Room(c, 3, 3, 6, 5, Concrete);
            c.Landmark("Pump House Stair", 5, 4).Exit("ship_graveyard", 5, 4);
            // the pump gallery
            Room(c, 3, 10, 18, 7, Concrete);
            Passage(c, 5, 8, 5, 10, 3, Concrete);
            for (int x = 5; x < 20; x += 4) c.Put(x, 12, HardCover, "machine");
            for (int x = 4; x < 20; x++) if (x % 4 != 0) c.Deco(x, 15, x % 4 == 1 ? "pipepost" : "pipe");
            c.Landmark("Pump Gallery", 12, 13);
            // the main culvert east, flooded
            Room(c, 22, 6, 4, 30, Concrete);
            Passage(c, 20, 13, 22, 13, 2, Concrete);
            Flood(c, 22, 10, 4, 24);
            c.Landmark("Main Culvert", 24, 22);
            // the sluice chamber
            Room(c, 28, 3, 13, 10, Concrete);
            Passage(c, 26, 7, 28, 7, 2, Concrete);
            c.Put(30, 5, HardCover, "machine").Put(38, 5, HardCover, "machine").Put(34, 10, Solid, "pillar");
            Flood(c, 29, 8, 11, 4);
            c.Landmark("Sluice Chamber", 34, 6);
            // the fitters' store at the far end
            Room(c, 28, 26, 13, 10, Floor);
            Passage(c, 26, 30, 28, 30, 2, Concrete);
            c.Put(29, 27, HardCover, "lockers").Put(31, 27, HardCover, "lockers").Put(39, 34, ScatterCover, "crate").Put(36, 34, ScatterCover, "crate").Put(30, 33, ScatterCover, "desk");
            c.Stash("Fitters' Store", 39, 27);
            c.Landmark("Fitters' Store", 34, 31);
            // a side drain back west
            Room(c, 3, 20, 16, 4, Concrete);
            Passage(c, 18, 22, 22, 22, 1, Concrete);
            Room(c, 3, 26, 10, 10, Concrete);
            Passage(c, 6, 24, 6, 26, 2, Concrete);
            c.Put(5, 33, ScatterCover, "rustbarrel").Put(10, 28, ScatterCover, "crate").Deco(8, 31, "bones");
            return Finish(c, "concrete");
        }
    
        // ------------------------------------------------------------------ Tier 6

        // The Hospital Morgue: under the Siege Ward hospital - the cold room, the autopsy theatre, the records, and the
        // service corridor to the boiler room. The worst of Tier 6 waits in the cold.
        public static MapLayout HospitalMorgue()
        {
            var c = new TerrainCanvas(44, 40, TerrainCanvas.Seed("hospital_morgue"));
            for (int y = 0; y < 40; y++) for (int x = 0; x < 44; x++) c.Put(x, y, Solid, "plaster");
            Room(c, 19, 2, 6, 4, LabTile);
            c.Landmark("Morgue Landing", 21, 3).Exit("hospital", 21, 3);
            // the long corridor
            Room(c, 4, 7, 36, 3, LabTile);
            Passage(c, 21, 6, 21, 7, 3, LabTile);
            // the cold room: drawers along the walls
            Room(c, 4, 11, 14, 12, LabTile);
            c.Put(10, 10, Door);
            for (int y = 12; y < 22; y += 2) { c.Put(4, y, HardCover, "lockers"); c.Put(17, y, HardCover, "lockers"); }
            c.Deco(10, 16, "bones");
            c.Landmark("Cold Room", 10, 17);
            // the autopsy theatre
            Room(c, 22, 11, 14, 10, LabTile);
            c.Put(28, 10, Door);
            c.Put(26, 15, HardCover, "block").Put(31, 15, HardCover, "block").Put(23, 12, ScatterCover, "desk").Put(34, 19, ScatterCover, "crate");
            c.Landmark("Autopsy Theatre", 29, 17);
            // the records room
            Room(c, 37, 11, 4, 14, Floor);
            Passage(c, 38, 9, 38, 11, 2, LabTile);
            c.Put(39, 13, ScatterCover, "desk").Put(39, 17, HardCover, "lockers").Put(39, 21, ScatterCover, "crate");
            c.Stash("Records Safe", 39, 23);
            // the service corridor and the boiler room
            Room(c, 4, 25, 36, 3, Concrete);
            Passage(c, 6, 22, 6, 25, 2, LabTile); Passage(c, 30, 21, 30, 25, 2, LabTile);
            Room(c, 8, 29, 28, 8, Concrete);
            Passage(c, 20, 28, 20, 29, 3, Concrete);
            c.Put(12, 31, HardCover, "machine").Put(18, 31, HardCover, "machine").Put(30, 31, HardCover, "machine").Put(24, 35, ScatterCover, "rustbarrel");
            for (int x = 9; x < 35; x++) if (x % 4 != 0) c.Deco(x, 30, x % 4 == 1 ? "pipepost" : "pipe");
            c.Landmark("Boiler Room", 22, 33);
            c.Stash("Boilerman's Locker", 34, 35);
            return Finish(c, "plaster");
        }
    }
}
