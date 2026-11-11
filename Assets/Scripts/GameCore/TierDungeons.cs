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
        /// <summary>2026-11-10: the lock-and-key redo (ArtSource/plans/dungeon_plans.py flooded_bunker, graph dungeons/flooded_bunker.mmd): 'E' the blast door, '3' the duty locker (badge), '1' the
        /// armory's badge door, '4' the fuse crate, '5' the generator switch (power), '2' the dead doors (the command level and the hatch ladder), 'L' the Command Safe (goal), 'K' the armory's loot, 'J' the mess.</summary>
        public static MapLayout FloodedBunker()
        {
            var marks = new Dictionary<char, (string, string)> { ['E'] = ("Blast Door", "marshes"), ['B'] = ("Barracks", null), ['A'] = ("Armory", null), ['C'] = ("Command Room", null),
                ['Q'] = ("Generator Room", null), ['H'] = ("Hatch Ladder", null) };   // not 'N': StampPlan reads N as deep water
            var stashes = new Dictionary<char, string>(LabLocks.StashesOf("flooded_bunker")) { ['K'] = "Armory", ['L'] = "Command Safe", ['J'] = "Mess Cache" };
            return FloorPlan("flooded_bunker", LabPlans["flooded_bunker"], "concrete", Concrete, marks, stashes);
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
        /// <summary>2026-11-10: the lock-and-key redo (ArtSource/plans/dungeon_plans.py crusher_pit, graph dungeons/crusher_pit.mmd): 'E' the ladder, '3' the foreman's locker (badge),
        /// '1' the service tunnel's badge door, '4' the spares crate (fuse), '5' the ram breaker (power), '2' the dead blast door, 'G' the pay vault, 'H' 'O' 'C' landmarks, 'J' 'L' 'K' stashes.</summary>
        public static MapLayout CrusherPit()
        {
            var marks = new Dictionary<char, (string, string)> { ['E'] = ("Pit Ladder", "scrapyard"), ['G'] = ("Pay Vault", null), ['H'] = ("Ram Hall", null), ['O'] = ("Oil Sump", null), ['C'] = ("Foreman's Cage", null) };
            var stashes = new Dictionary<char, string>(LabLocks.StashesOf("crusher_pit")) { ['J'] = "Sump Cache", ['L'] = "Booth Strongbox", ['K'] = "Pay Vault" };
            return FloorPlan("crusher_pit", LabPlans["crusher_pit"], "concrete", Concrete, marks, stashes);
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
/// <summary>2026-11-11: the lock-and-key redo (ArtSource/plans/dungeon_plans.py stranded_ferry, graph dungeons/stranded_ferry.mmd): 'E' the car deck's ramp, '3' the purser's locker (badge),
        /// '1' the engine room's badge door, '4' the spares locker's fuse, '5' the generator switch (power), '2' the dead bridge door, 'G' the bridge (goal), 'P' the lounge, 'R' the engine room, 'J' 'K' 'L' stashes.</summary>
        public static MapLayout StrandedFerry()
        {
            var marks = new Dictionary<char, (string, string)> { ['E'] = ("Car Deck", "dried_lake"), ['P'] = ("Passenger Lounge", null), ['R'] = ("Engine Room", null), ['G'] = ("Captain's Bridge", null) };
            var stashes = new Dictionary<char, string>(LabLocks.StashesOf("stranded_ferry")) { ['J'] = "Cabin Locker", ['K'] = "Captain's Safe", ['L'] = "Engine Room Cache" };
            return FloorPlan("stranded_ferry", LabPlans["stranded_ferry"], "hull", Concrete, marks, stashes);
        }

        // ------------------------------------------------------------------ Tier 4

        // The Camp Shelter: the summer camp's Cold War bomb shelter - the stair and blast door, decontamination, the
        // dormitory, the mess, the generator and filter rooms, and the shelter command post at the back.
/// <summary>2026-11-11: the lock-and-key redo (dungeon_plans.py camp_shelter, graph dungeons/camp_shelter.mmd): 'E' the stair, '(' the blast door, '3' the warden's locker (badge), '1' the filter room's
        /// badge door, '4' the fuse, '5' the generator switch (power), '2' the dead command door, 'G' the shelter command (goal), 'B' dormitory, 'M' mess, 'F' filter room, 'Q' generator room, 'J' 'K' 'L' stashes.</summary>
        public static MapLayout CampShelter()
        {
            var marks = new Dictionary<char, (string, string)> { ['E'] = ("Shelter Stair", "crimson_woods"), ['B'] = ("Dormitory", null), ['M'] = ("Mess", null), ['F'] = ("Filter Room", null),
                ['Q'] = ("Generator Room", null), ['G'] = ("Shelter Command", null) };
            var stashes = new Dictionary<char, string>(LabLocks.StashesOf("camp_shelter")) { ['J'] = "Shelter Stores", ['K'] = "Shelter Safe", ['L'] = "Mess Cache" };
            return FloorPlan("camp_shelter", LabPlans["camp_shelter"], "concrete", Concrete, marks, stashes);
        }
    
        // ------------------------------------------------------------------ Tier 5

        // The Dry Dock Tunnels: under the dock's pump house, the culverts that drained the dock - a pump gallery, the
        // flooded main culvert, the sluice chamber, and the old ship-fitters' store.
/// <summary>2026-11-11: the lock-and-key redo (dungeon_plans.py drydock_tunnels, graph dungeons/drydock_tunnels.mmd): 'E' the stair, '4' the fuse (the workmen's niche), '6' the pump panel (needs the
        /// fuse, drains the culvert 'W'), '3' the sluice wheel's handle, '$' the sluice gate and the service ladder's hatch, 'G' the fitters' store (goal), 'P' 'C' 'U' landmarks, 'J' 'K' stashes.</summary>
        public static MapLayout DrydockTunnels()
        {
            var marks = new Dictionary<char, (string, string)> { ['E'] = ("Pump House Stair", "ship_graveyard"), ['P'] = ("Pump Gallery", null), ['C'] = ("Main Culvert", null), ['U'] = ("Sluice Chamber", null),
                ['G'] = ("Fitters' Store", null) };
            var stashes = new Dictionary<char, string>(LabLocks.StashesOf("drydock_tunnels")) { ['J'] = "Side Drain Cache", ['K'] = "Fitters' Store" };
            return FloorPlan("drydock_tunnels", LabPlans["drydock_tunnels"], "concrete", Concrete, marks, stashes);
        }
    
        // ------------------------------------------------------------------ Tier 6

        // The Hospital Morgue: under the Siege Ward hospital - the cold room, the autopsy theatre, the records, and the
        // service corridor to the boiler room. The worst of Tier 6 waits in the cold.
/// <summary>2026-11-11: the lock-and-key redo (dungeon_plans.py hospital_morgue, graph dungeons/hospital_morgue.mmd): 'E' the landing, '3' the autopsy drawer (badge), '1' the theatre's badge door,
        /// '4' the boilerman's locker (fuse), '5' the boiler switch (power), '2' the dead records door, 'G' the records room (goal), 'C' cold room, 'A' theatre, 'B' boiler room, 'K' 'L' stashes.</summary>
        public static MapLayout HospitalMorgue()
        {
            var marks = new Dictionary<char, (string, string)> { ['E'] = ("Morgue Landing", "hospital"), ['C'] = ("Cold Room", null), ['A'] = ("Autopsy Theatre", null), ['B'] = ("Boiler Room", null),
                ['G'] = ("Records Room", null) };
            var stashes = new Dictionary<char, string>(LabLocks.StashesOf("hospital_morgue")) { ['K'] = "Records Safe", ['L'] = "Linen Store" };
            return FloorPlan("hospital_morgue", LabPlans["hospital_morgue"], "plaster", LabTile, marks, stashes);
        }

        // ------------------------------------------------------------------ the hidden dungeons, redone by hand (2026-11-11; they were generated lab plans)

        /// <summary>Maintenance Tunnels (dungeon_plans.py maintenance_tunnels, graph dungeons/maintenance_tunnels.mmd): 'E' the hatch, 'T' the junction (hub), '3' foreman's locker (badge), '1' the crib's door,
        /// '4' fuse, '5' the substation breaker (power), '2' the pump house door and the shortcut's, '6' the pump (drain), 'W' the flooded gallery, 'C' the collapsed section, 'G' the surface depot (goal).</summary>
        public static MapLayout MaintenanceTunnels()
        {
            var marks = new Dictionary<char, (string, string)> { ['E'] = ("Access Hatch", "rail_yards"), ['T'] = ("Tunnel Junction", null), ['P'] = ("Pump House", null), ['B'] = ("Substation", null),
                ['C'] = ("Collapsed Section", null), ['G'] = ("Surface Depot", null) };
            var stashes = new Dictionary<char, string>(LabLocks.StashesOf("maintenance_tunnels")) { ['K'] = "Depot Strongbox", ['J'] = "Buried Kit" };
            return FloorPlan("maintenance_tunnels", LabPlans["maintenance_tunnels"], "concrete", Concrete, marks, stashes);
        }

        /// <summary>Underground Bunker (dungeon_plans.py underground_bunker, graph dungeons/underground_bunker.mmd): 'E' the foyer, '(' the blast door, '3' the barracks locker (badge), '1' air handling's door,
        /// '4' fuse, '5' the generator switch (power), '2' the armory's dead door, 'G' the command centre (goal), 'R' the ration stores, 'B' 'A' 'Q' 'M' landmarks, 'J' 'K' 'L' stashes.</summary>
        public static MapLayout UndergroundBunker()
        {
            var marks = new Dictionary<char, (string, string)> { ['E'] = ("Blast Door Foyer", "abandoned_city"), ['B'] = ("Bunker Barracks", null), ['R'] = ("Ration Stores", null), ['A'] = ("Air Handling", null),
                ['Q'] = ("Generator Hall", null), ['M'] = ("Bunker Armory", null), ['G'] = ("Command Centre", null) };
            var stashes = new Dictionary<char, string>(LabLocks.StashesOf("underground_bunker")) { ['K'] = "Command Safe", ['J'] = "Ration Crate", ['L'] = "Armory Rack" };
            return FloorPlan("underground_bunker", LabPlans["underground_bunker"], "concrete", Concrete, marks, stashes);
        }

        /// <summary>Deep Caverns (dungeon_plans.py deep_caverns, graph dungeons/deep_caverns.mmd): 'E' the surface shaft, 'R' the Root Hall (hub), '4' the climbers' crate (pry bar), '@' the rockfall and the
        /// chimney's fall, '6' the dam's lever (drain), 'W' the lower lake, 'G' the Crystal Pool on its island (goal), 'B' the camp, 'M' the dam, 'J' 'K' stashes.</summary>
        public static MapLayout DeepCaverns()
        {
            var marks = new Dictionary<char, (string, string)> { ['E'] = ("Surface Shaft", "crimson_woods"), ['R'] = ("Root Hall", null), ['B'] = ("Climbers' Camp", null), ['M'] = ("Old Dam", null), ['G'] = ("Crystal Pool", null) };
            var stashes = new Dictionary<char, string>(LabLocks.StashesOf("deep_caverns")) { ['K'] = "Crystal Cache", ['J'] = "Grotto Cache" };
            return FloorPlan("deep_caverns", LabPlans["deep_caverns"], "cliff", CaveFloor, marks, stashes);
        }

        /// <summary>Underground Labyrinth (dungeon_plans.py underground_labyrinth, graph dungeons/underground_labyrinth.mmd): 'E' The Fork, '4' the gate key, '$' the iron gate, '3' the pry bar, '@' the rockfall,
        /// '6' the tally wall (the code), '^' the sealed slab, 'C' the Cave-In Chamber, 'G' the Minotaur's Hall (goal), 'J' 'K' 'L' stashes.</summary>
        public static MapLayout UndergroundLabyrinth()
        {
            var marks = new Dictionary<char, (string, string)> { ['E'] = ("The Fork", "quarry_basin"), ['C'] = ("Cave-In Chamber", null), ['G'] = ("Minotaur's Hall", null) };
            var stashes = new Dictionary<char, string>(LabLocks.StashesOf("underground_labyrinth")) { ['K'] = "Hall Hoard", ['J'] = "Dead-End Cache", ['L'] = "Lost Pack" };
            return FloorPlan("underground_labyrinth", LabPlans["underground_labyrinth"], "cliff", CaveFloor, marks, stashes);
        }

        /// <summary>Siege Catacombs (dungeon_plans.py siege_catacombs, graph dungeons/siege_catacombs.mmd): 'E' the stair, 'O' the Ossuary (hub), '3' the sexton's box (gate key), '$' the iron gate,
        /// '6' the burial roll (the code), '4' the sappers' crate (pry bar), '@' the rockfall and the tunnel's fall, '^' the sealed slab, 'G' the Sealed Crypt (goal; the story's guardian curls round it).</summary>
        public static MapLayout SiegeCatacombs()
        {
            var marks = new Dictionary<char, (string, string)> { ['E'] = ("Catacomb Stair", "hospital"), ['O'] = ("Ossuary", null), ['G'] = ("Sealed Crypt", null) };
            var stashes = new Dictionary<char, string>(LabLocks.StashesOf("siege_catacombs")) { ['K'] = "Crypt Reliquary", ['J'] = "North Chamber Urn" };
            return FloorPlan("siege_catacombs", LabPlans["siege_catacombs"], "wall", Concrete, marks, stashes);
        }

        /// <summary>Secret Lab Gamma (dungeon_plans.py secret_lab_gamma, graph dungeons/secret_lab_gamma.mmd): 'E' the lift, 'A' the atrium, '3' the security locker (badge), '1' the labs' door, '4' fuse,
        /// '5' the generator switch (power), '2' the Signal Room's dead door, 'T' the Signal Room, 'G' the Sealed Vault (the story opens it with the eel's key, StorySystem), 'J' 'K' stashes.</summary>
        public static MapLayout SecretLabGamma()
        {
            var marks = new Dictionary<char, (string, string)> { ['E'] = ("Entry Lift", "antenna_grid"), ['A'] = ("Atrium", null), ['T'] = ("Signal Room", null), ['G'] = ("Sealed Vault", null) };
            var stashes = new Dictionary<char, string>(LabLocks.StashesOf("secret_lab_gamma")) { ['K'] = "Vault Shelf", ['J'] = "Storage Locker" };
            return FloorPlan("secret_lab_gamma", LabPlans["secret_lab_gamma"], "concrete", LabTile, marks, stashes);
        }

        /// <summary>Secret Lab Beta (dungeon_plans.py secret_lab_beta, graph dungeons/secret_lab_beta.mmd): 'E' the lift, 'R' reception, '3' the guard's locker (badge), '1' the lab wing's door, 'A' the
        /// Archive Room (the story's drive; '4' the fuse), '5' the generator switch (power), '2' the ward's dead door, 'G' the Isolation Ward (goal), 'Z' the cracked wall and 'V' the body in black armour (Boss 1).</summary>
        public static MapLayout SecretLabBeta()
        {
            var marks = new Dictionary<char, (string, string)> { ['E'] = ("Entry Lift", "ghost_town"), ['R'] = ("Reception", null), ['A'] = ("Archive Room", null), ['G'] = ("Isolation Ward", null),
                ['Z'] = (Boss.CrackedWall, null), ['V'] = (Boss.VisorMark, null) };
            var stashes = new Dictionary<char, string>(LabLocks.StashesOf("secret_lab_beta")) { ['K'] = "Sample Cooler", ['J'] = "Reception Desk" };
            return FloorPlan("secret_lab_beta", LabPlans["secret_lab_beta"], "plaster", LabTile, marks, stashes);
        }
    }
}
