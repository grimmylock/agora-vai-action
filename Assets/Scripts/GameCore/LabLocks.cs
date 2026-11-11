// Boss 2 (Yan, 2026-11-03: "right now it looks very even in space, it doesn't have a linear progression ... most rooms are locked,
// the few that are open have either a key, a hole in the wall going into the other room ... i'd like them to be engaging to explore
// and figure out"): the dungeons' locks (ArtSource/plans/lab_plans.py alpha_plan / depths_plan, each a mission graph checked on the
// plan grid). One lock type, one look, everywhere:
//   staff badge - blue-lit doors (plan '1'); the badge is in a locker ('3')
//   power       - dead hazard-striped blast doors (plan '2'); a switch ('5') that needs a fuse from a crate ('4')
//   drain       - black deep water (plan 'W', Sub-Station Depths): over your head until the pump switch ('6', needs power) runs
// Keys live in MacroState.Keys (saved with the world), one set per dungeon ("alpha_staff", "depths_power"...): a lock is a wall to
// everyone until the player holds that dungeon's key, then it's a door like any other (or knee-deep water). Nothing is spent, so
// nothing can strand you.
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class LabLocks
    {
        // Secret Lab Alpha's keys by name (the harness and older code use these)
        public const string Staff = "alpha_staff", Fuse = "alpha_fuse", Power = "alpha_power";
        public const string Locker = "Security Locker", Crate = "Fuse Crate", Switch = "Generator Switch";
        public const string Depths = "sub_station_depths", Depths1 = "sub_station_depths_1", Depths2 = "sub_station_depths_2", Depths3 = "sub_station_depths_3";

        /// <summary>A key-giving object in a plan: its name, look, the key kind it gives, the kind it needs, what it says.</summary>
        class Thing { public string Name, Look, Kind, Needs, Desc, Says; public int Ru; }

        static readonly Dictionary<string, string> Prefix = new Dictionary<string, string> { ["secret_lab_alpha"] = "alpha", [Depths] = "depths", [Depths1] = "depths", [Depths2] = "depths", [Depths3] = "depths", ["pumping_basement"] = "pump", ["secret_cave"] = "cave", ["smugglers_tunnel"] = "smug", ["chapel_crypt"] = "crypt", ["valley_mine"] = "mine", ["crusher_pit"] = "crusher", ["flooded_bunker"] = "bunker", ["stranded_ferry"] = "ferry", ["camp_shelter"] = "shelter", ["drydock_tunnels"] = "dock", ["hospital_morgue"] = "morgue", ["maintenance_tunnels"] = "mtun", ["underground_bunker"] = "ubunk", ["deep_caverns"] = "cavern", ["underground_labyrinth"] = "maze", ["siege_catacombs"] = "siege", ["lattice_foundry_lower"] = "foundry", ["secret_lab_gamma"] = "gamma", ["secret_lab_beta"] = "beta" };

        /// <summary>Which drain key a map's deep water answers to. The Depths' flooded levels each drain with their own pump; the station's
        /// basin only empties when the last level is drained (the eel boss, ACTION_DESIGN "Dodge bosses"). Any other map: "drain".</summary>
        static readonly Dictionary<string, string> Water = new Dictionary<string, string>
            { [Depths] = "drain3", [Depths1] = "drain1", [Depths2] = "drain2", [Depths3] = "drain3" };
        static string WaterKind(string zone) => Water.TryGetValue(zone ?? "", out var k) ? k : "drain";

        static readonly Dictionary<string, Dictionary<char, Thing>> Things = new Dictionary<string, Dictionary<char, Thing>>
        {
            ["secret_lab_alpha"] = new Dictionary<char, Thing>
            {
                ['3'] = new Thing { Name = Locker, Look = "stash_footlocker", Kind = "staff", Ru = 60, Desc = "A dented steel locker, SECURITY stencilled on the door.",
                                    Says = "[LAB] A staff badge on a blue lanyard, still warm from nobody's neck. The card readers will know it." },
                ['4'] = new Thing { Name = Crate, Look = "stash_toolbox", Kind = "fuse", Desc = "A crate of electrical spares.",
                                    Says = "[LAB] A heavy fuse in a greased sleeve, stamped GEN-2. It would fit a generator." },
                ['5'] = new Thing { Name = Switch, Look = "labswitch", Kind = "power", Needs = "fuse",
                                    Desc = "The generator's main switch. The fuse slot is empty: you'd need a fuse from somewhere in the labs.",
                                    Says = "[LAB] The generator coughs, catches. Somewhere deep in the walls a lot of locks let go at once." },
            },
            // Sub-Station Depths (Tier 3, under the Dried Lake): a flooded switching station. Badge in the dispatcher's locker -> the cable
            // gallery -> the battery room's breaker (needs the fuse from the workers' quarters) -> the pump room -> the basin drains -> the vault.
            [Depths] = new Dictionary<char, Thing>
            {
                ['3'] = new Thing { Name = "Dispatcher's Locker", Look = "stash_footlocker", Kind = "staff", Ru = 80, Desc = "A grey locker, the shift roster still taped to it.",
                                    Says = "[DEPTHS] A staff badge clipped inside the door, the photo bleached to nothing. The card readers down here still take it." },
                ['4'] = new Thing { Name = "Spares Crate", Look = "stash_toolbox", Kind = "fuse", Desc = "A crate of electrical spares under a bunk.",
                                    Says = "[DEPTHS] A cartridge fuse as long as your forearm, BATTERY BANK B on the tag." },
                ['5'] = new Thing { Name = "Battery Breaker", Look = "labswitch", Kind = "power", Needs = "fuse",
                                    Desc = "The battery bank's breaker. One fuse holder is empty and scorched.",
                                    Says = "[DEPTHS] The breaker slams home. The batteries hum, and down the halls the blast doors' lights wake up." },
            },
            // Pumping Station Basement (Tier 1, under the marshes; the lock-and-key redo of 2026-11-10, dungeons/pumping_basement.mmd): spares crate in the valve
            // gallery -> the breaker in the control room -> power opens the pump panel's door -> the pump -> the culvert's black water drains -> the outfall.
            ["pumping_basement"] = new Dictionary<char, Thing>
            {
                ['4'] = new Thing { Name = "Valve Spares Cabinet", Look = "stash_toolbox", Kind = "fuse", Desc = "A steel cabinet of spare valves and fittings, one drawer hanging open.",
                                    Says = "[PUMPS] A cartridge fuse in a rag, PUMP STN 2 on the tag. It would fit the control room's breaker." },
                ['5'] = new Thing { Name = "Pump Station Breaker", Look = "labswitch", Kind = "power", Needs = "fuse",
                                    Desc = "The station's breaker panel. One fuse holder is empty and scorched.",
                                    Says = "[PUMPS] The breaker slams home. Somewhere below the floor a relay clicks and the blast door's light turns amber." },
                ['6'] = new Thing { Name = "Culvert Pump Panel", Look = "labswitch", Kind = "drain", Needs = "power",
                                    Desc = "The pump panel for the drainage culvert, its lamps dark: no power reaches it.",
                                    Says = "[PUMPS] The pump shudders, then thumps. Down the culvert the black water starts to slide away." },
            },
            // Secret Cave (Tier 1, under the perimeter fields; dungeons/secret_cave.mmd): the smugglers dropped a slab across the tunnel behind them; a pry bar from their camp lifts it.
            ["secret_cave"] = new Dictionary<char, Thing>
            {
                ['4'] = new Thing { Name = "Smugglers' Crate", Look = "stash_toolbox", Kind = "pry", Desc = "A split wooden crate in the corner of the old camp, a bent tool sticking out of the straw.",
                                    Says = "[CAVE] A steel pry bar, bent and rust-red, still sharp at one end. It would lever a fallen slab." },
            },
            // Smugglers' Tunnel (Tier 1, from the depot's loading office; dungeons/smugglers_tunnel.mmd): an iron gate across the tunnel, padlocked; the key is in the guard post's footlocker.
            ["smugglers_tunnel"] = new Dictionary<char, Thing>
            {
                ['4'] = new Thing { Name = "Guard Post Footlocker", Look = "stash_footlocker", Kind = "gate", Desc = "A dented footlocker under the guard's bunk, the lid wired shut with a loop that has long since rusted through.",
                                    Says = "[TUNNEL] A heavy iron key on a loop of wire, a smeared GATE scratched in the bow. It would fit the padlock down the tunnel." },
            },
            // Chapel Crypt (Tier 1, under the chapel; dungeons/chapel_crypt.mmd): an iron gate across the burial passage (the key is in the priest's cell) and, beyond it, a sealed slab over the family
            // vault whose combination the ossuary's ledger gives.
            ["chapel_crypt"] = new Dictionary<char, Thing>
            {
                ['3'] = new Thing { Name = "Burial Ledger", Look = "stash_pda", Kind = "code", Desc = "A water-stained ledger chained to the ossuary wall, open to its last page.",
                                    Says = "[CRYPT] The last page is a column of dates, three of them ringed in the priest's hand. Dial them on the vault's slab and it would turn." },
                ['4'] = new Thing { Name = "Priest's Strongbox", Look = "stash_footlocker", Kind = "gate", Desc = "A small iron strongbox under the priest's cot, its hasp snapped.",
                                    Says = "[CRYPT] An iron key as long as a hand, the bow worn smooth by thumbs. It would fit the gate across the passage." },
            },
            // Valley Mine (Tier 2, off Black Valley's rim; dungeons/valley_mine.mmd): timbers fell across the main gallery; a pry bar from the tool shed levers them aside.
            ["valley_mine"] = new Dictionary<char, Thing>
            {
                ['4'] = new Thing { Name = "Tool Shed Rack", Look = "stash_toolbox", Kind = "pry", Desc = "A rack of mining tools on the shed wall, most of them rusted solid.",
                                    Says = "[MINE] A long steel pry bar, the end worn to a chisel edge. It would shift fallen timbers." },
            },
            // Crusher Pit (Tier 2, under Crusher Row; dungeons/crusher_pit.mmd): the Alpha locks. The foreman's locker in the oil sump holds the staff badge (the service tunnel's blue door), the spares crate in the cage the
            // fuse, the ram breaker in the control booth needs it; power wakes the dead blast door onto the pay vault.
            ["crusher_pit"] = new Dictionary<char, Thing>
            {
                ['3'] = new Thing { Name = "Foreman's Locker", Look = "stash_footlocker", Kind = "staff", Ru = 70, Desc = "A dented steel locker at the sump's far end, FOREMAN stencilled on the door in oil.",
                                    Says = "[PIT] A staff badge on a greasy lanyard, the photo gone to black. The card readers down here would still know it." },
                ['4'] = new Thing { Name = "Ram Spares Crate", Look = "stash_toolbox", Kind = "fuse", Desc = "A crate of hydraulic spares under the foreman's desk.",
                                    Says = "[PIT] A heavy cartridge fuse in a greased sleeve, RAM FEED 3 on the tag." },
                ['5'] = new Thing { Name = "Ram Breaker", Look = "labswitch", Kind = "power", Needs = "fuse",
                                    Desc = "The rams' breaker panel. One fuse holder is empty and scorched.",
                                    Says = "[PIT] The breaker slams home. Far off, the rams shudder once, and the blast door's lamp turns amber." },
            },
            // Flooded Bunker (Tier 1, behind the marsh blast door; dungeons/flooded_bunker.mmd): the Alpha locks. The duty locker in the barracks holds the staff badge (the armory's blue door), the armory the
            // fuse, the generator room's switch needs it; power wakes the dead door onto the flooded command level (and the hatch ladder's). The way INTO the bunker is the zone key "flooded_bunker" (Tier1Story).
            ["flooded_bunker"] = new Dictionary<char, Thing>
            {
                ['3'] = new Thing { Name = "Duty Locker", Look = "stash_footlocker", Kind = "staff", Ru = 50, Desc = "A dented steel locker at the head of a bunk, DUTY OFFICER stencilled on it.",
                                    Says = "[BUNKER] A staff badge on a faded lanyard, a face scratched out of the photo. The card readers still know it." },
                ['4'] = new Thing { Name = "Armory Spares Crate", Look = "stash_toolbox", Kind = "fuse", Desc = "A crate of electrical spares in the armory's back corner.",
                                    Says = "[BUNKER] A heavy fuse in a greased sleeve, GEN 1 on the tag. It would fit the generator's switch." },
                ['5'] = new Thing { Name = "Generator Switch", Look = "labswitch", Kind = "power", Needs = "fuse",
                                    Desc = "The generator's main switch. The fuse slot is empty and scorched.",
                                    Says = "[BUNKER] The generator coughs, catches. Somewhere above the flooded level a lot of locks let go at once." },
            },
            // The Stranded Ferry (Tier 3, the dried lake; dungeons/stranded_ferry.mmd): the Alpha locks. The purser's locker in the cabin row holds the crew badge (the engine room's card reader), the spares
            // locker the fuse, the generator switch needs it; power wakes the dead door onto the captain's bridge.
            ["stranded_ferry"] = new Dictionary<char, Thing>
            {
                ['3'] = new Thing { Name = "Purser's Locker", Look = "stash_footlocker", Kind = "staff", Ru = 110, Desc = "A grey steel locker in the purser's cabin, a ticket punch still hanging off the handle.",
                                    Says = "[FERRY] A crew badge in a cracked plastic sleeve. The card reader on the engine room stair should still take it." },
                ['4'] = new Thing { Name = "Spares Locker Crate", Look = "stash_toolbox", Kind = "fuse", Desc = "A crate of engine spares, packed in grease that has gone hard.",
                                    Says = "[FERRY] A marine fuse the size of a fist, AUX GEN on the tag." },
                ['5'] = new Thing { Name = "Auxiliary Generator Switch", Look = "labswitch", Kind = "power", Needs = "fuse",
                                    Desc = "The auxiliary generator's switchboard. One fuse clip hangs open.",
                                    Says = "[FERRY] The generator turns over, the deck plates hum. Up top, the bridge door's lamp comes on." },
            },
            // Camp Shelter (Tier 4; dungeons/camp_shelter.mmd): the Alpha locks. Warden's locker (dormitory) = badge -> the filter room's door; the fuse there; the generator switch; power wakes the command door.
            ["camp_shelter"] = new Dictionary<char, Thing>
            {
                ['3'] = new Thing { Name = "Warden's Locker", Look = "stash_footlocker", Kind = "staff", Ru = 140, Desc = "A tall locker at the end of the bunks, CAMP WARDEN on a brass plate.",
                                    Says = "[SHELTER] A warden's pass card on a whistle cord. The reader by the filter room would know it." },
                ['4'] = new Thing { Name = "Filter Spares Crate", Look = "stash_toolbox", Kind = "fuse", Desc = "A crate of filter spares: gaskets, a hand crank, something heavy wrapped in wax paper.",
                                    Says = "[SHELTER] A big cartridge fuse, DIESEL SET A stamped on the cap." },
                ['5'] = new Thing { Name = "Shelter Generator Switch", Look = "labswitch", Kind = "power", Needs = "fuse",
                                    Desc = "The generator's switch panel. A fuse is missing from the middle of the row.",
                                    Says = "[SHELTER] The diesels catch one after the other. The lamps come up yellow, and the command door's lock drops." },
            },
            // Dry Dock Tunnels (Tier 5; dungeons/drydock_tunnels.mmd): fuse -> the pump panel (drains the main culvert) -> the sluice wheel's handle (the "gate" key: the sluice gate and the ladder hatch).
            ["drydock_tunnels"] = new Dictionary<char, Thing>
            {
                ['4'] = new Thing { Name = "Workmen's Toolbox", Look = "stash_toolbox", Kind = "fuse", Ru = 90, Desc = "A dockers' toolbox left in the niche, a thermos still on top of it.",
                                    Says = "[DOCK] A pump fuse in an oilcloth, CULVERT 1 chalked on it." },
                ['6'] = new Thing { Name = "Culvert Pump Controls", Look = "labswitch", Kind = "drain", Needs = "fuse",
                                    Desc = "The main culvert's pump controls. The fuse carrier is pulled out and empty.",
                                    Says = "[DOCK] The pump groans and takes hold. In the culvert the black water starts to drop, a hand's width at a time." },
                ['3'] = new Thing { Name = "Sluice Wheel Handle", Look = "stash_toolbox", Kind = "gate", Desc = "A cast-iron crank handle hung on a hook beside the sluice gears.",
                                    Says = "[DOCK] The sluice wheel's handle, heavy as a sledge. It fits the gate by the fitters' store, and the ladder hatch." },
            },
            // Hospital Morgue (Tier 6; dungeons/hospital_morgue.mmd): the Alpha locks. The autopsy drawer = badge -> the theatre's back door; the boilerman's locker = fuse; the boiler switch; power wakes the records door.
            ["hospital_morgue"] = new Dictionary<char, Thing>
            {
                ['3'] = new Thing { Name = "Autopsy Drawer", Look = "stash_footlocker", Kind = "staff", Ru = 180, Desc = "An instrument drawer under the theatre's wall cabinet, jammed half open.",
                                    Says = "[MORGUE] A pathologist's badge among the scalpels. The service door's reader would still read it." },
                ['4'] = new Thing { Name = "Boilerman's Locker", Look = "stash_footlocker", Kind = "fuse", Ru = 60, Desc = "A locker by the boilers, a calendar from a year nobody finished taped inside the door.",
                                    Says = "[MORGUE] A ceramic fuse wrapped in a rag, BOILER FEED written on the rag." },
                ['5'] = new Thing { Name = "Boiler Switch", Look = "labswitch", Kind = "power", Needs = "fuse",
                                    Desc = "The boiler house switchboard. One fuse carrier is empty.",
                                    Says = "[MORGUE] The board hums. Overhead the corridor lamps flicker on, and by the stair a dead door wakes." },
            },
            // Maintenance Tunnels (hidden, Tier 3; dungeons/maintenance_tunnels.mmd): badge -> fuse -> power -> the pump (drain).
            ["maintenance_tunnels"] = new Dictionary<char, Thing>
            {
                ['3'] = new Thing { Name = "Track Foreman's Locker", Look = "stash_footlocker", Kind = "staff", Ru = 100, Desc = "A locker with a rail company crest, the padlock long since cut.",
                                    Says = "[TUNNELS] A maintenance pass on a chain. The tool crib's reader will take it." },
                ['4'] = new Thing { Name = "Tool Crib Bin", Look = "stash_toolbox", Kind = "fuse", Desc = "A parts bin in the crib, every drawer labelled in a neat hand.",
                                    Says = "[TUNNELS] A traction fuse, long as a forearm, SUB 2 on the label." },
                ['5'] = new Thing { Name = "Substation Breaker", Look = "labswitch", Kind = "power", Needs = "fuse",
                                    Desc = "The substation's main breaker. One fuse clip gapes empty.",
                                    Says = "[TUNNELS] The breaker bangs shut and the transformers start to sing. Two dead doors come alive." },
                ['6'] = new Thing { Name = "Gallery Pump Starter", Look = "labswitch", Kind = "drain", Needs = "power",
                                    Desc = "The starter for the gallery's drainage pump, dark without power.",
                                    Says = "[TUNNELS] The pump takes hold. Along the gallery the black water starts to fall." },
            },
            // Underground Bunker (hidden, Tier 6; dungeons/underground_bunker.mmd): the Alpha locks.
            ["underground_bunker"] = new Dictionary<char, Thing>
            {
                ['3'] = new Thing { Name = "Watch Officer's Locker", Look = "stash_footlocker", Kind = "staff", Ru = 200, Desc = "A locker at the end of the bunks, WATCH OFFICER on a card in the slot.",
                                    Says = "[BUNKER] A watch officer's pass. The reader on the air handling door will know it." },
                ['4'] = new Thing { Name = "Air Handling Spares", Look = "stash_toolbox", Kind = "fuse", Desc = "A spares box bolted under the fan housings.",
                                    Says = "[BUNKER] A sealed fuse, GENERATOR HALL - BUS B printed on the tube." },
                ['5'] = new Thing { Name = "Generator Hall Switch", Look = "labswitch", Kind = "power", Needs = "fuse",
                                    Desc = "The generator hall's bus switch, one fuse short.",
                                    Says = "[BUNKER] The generators run up. Down the corridor the armory's dead door unlocks with a bang." },
            },
            // Deep Caverns (hidden, Tier 4; dungeons/deep_caverns.mmd): the pry bar (rockfalls), the dam's lever (drains the lower lake).
            ["deep_caverns"] = new Dictionary<char, Thing>
            {
                ['4'] = new Thing { Name = "Climbers' Crate", Look = "stash_toolbox", Kind = "pry", Ru = 120, Desc = "A crate of climbing gear at a camp nobody came back to: rope, pitons, a long steel bar.",
                                    Says = "[CAVERNS] A pry bar as long as your arm. It would shift a fallen slab." },
                ['6'] = new Thing { Name = "Dam Sluice Lever", Look = "labswitch", Kind = "drain", Desc = "A rusted lever on the old dam's sluice, as tall as a man.",
                                    Says = "[CAVERNS] The lever gives all at once. Below, the lake starts to pour away through the sluice." },
            },
            // Underground Labyrinth (hidden, Tier 5; dungeons/underground_labyrinth.mmd): gate key -> pry bar -> the slab's code.
["underground_labyrinth"] = new Dictionary<char, Thing>
            {
                ['4'] = new Thing { Name = "Red Lever", Look = "lever_a", Kind = "lever1", Ru = 150, Desc = "A heavy floor lever with a red knob, chained in the up position. The chain has rusted through.",
                                    Says = "[LABYRINTH] The red lever goes over. Far off, iron grinds on stone: a gate with a red plate has lifted." },
                ['3'] = new Thing { Name = "Amber Lever", Look = "lever_b", Kind = "lever2", Desc = "A floor lever with an amber knob, set in a steel box bolted to the rock.",
                                    Says = "[LABYRINTH] The amber lever drops. Somewhere behind you a second portcullis rattles up." },
                ['6'] = new Thing { Name = "Blue Lever", Look = "lever_c", Kind = "lever3", Desc = "A floor lever with a blue knob. Bones lie round its base.",
                                    Says = "[LABYRINTH] The blue lever. The last gate rises, and a breath of cold air comes up the passage from the hall." },
            },
            // Siege Catacombs (hidden, Tier 6; dungeons/siege_catacombs.mmd): the sexton's key (iron gate), the sappers' pry bar (rockfalls), the burial roll (the Sealed Crypt's slab).
            ["siege_catacombs"] = new Dictionary<char, Thing>
            {
                ['3'] = new Thing { Name = "Sexton's Box", Look = "stash_chest", Kind = "gate", Ru = 220, Desc = "A small iron-bound box on a ledge among the bones.",
                                    Says = "[CATACOMBS] A long iron key, cold as the stone. The gate to the north chamber has a lock that size." },
                ['4'] = new Thing { Name = "Sappers' Crate", Look = "stash_toolbox", Kind = "pry", Desc = "A crate stencilled with an engineers' badge: fuse wire, a lamp, digging bars.",
                                    Says = "[CATACOMBS] A sapper's digging bar. Enough to lever a fallen block aside." },
                ['6'] = new Thing { Name = "Burial Roll", Look = "stash_pda", Kind = "code", Desc = "A ledger of the dead, chained to a lectern. The last page lists the crypt and four numbers.",
                                    Says = "[CATACOMBS] The crypt's entry: a name scratched out, and the four numbers of its seal." },
            },
            // The Lattice Foundry's synthesis level (Tier 5; dungeons/lattice_foundry_lower.mmd, built without the coolant lock): the Foreman's locker = badge -> the plant room (fuse) -> the bus switch
            // in the cable tunnel -> power wakes the Coil Hall's doors (the Cell Synthesizer).
            ["lattice_foundry_lower"] = new Dictionary<char, Thing>
            {
                ['3'] = new Thing { Name = "Foreman's Locker", Look = "stash_footlocker", Kind = "staff", Ru = 160, Desc = "A locker beside the Foreman's cot, schematics taped inside the door.",
                                    Says = "[FOUNDRY] A plant pass, the photo burned away at one corner. The plant room's readers will take it." },
                ['4'] = new Thing { Name = "Crew Quarters Spares", Look = "stash_toolbox", Kind = "fuse", Desc = "A spares case under a bunk in the crew quarters.",
                                    Says = "[FOUNDRY] A bus fuse in a ceramic sleeve, COIL HALL FEED on the band." },
                ['6'] = new Thing { Name = "Coolant Valve Wheel", Look = "labswitch", Kind = "coolant", Desc = "The plant room's coolant main: a wheel valve as wide as your arms, wired shut with a tag.",
                                    Says = "[FOUNDRY] The wheel turns, the pipes knock, and cold runs through the level. The steam at the crew quarters' doors thins and stops." },
                ['5'] = new Thing { Name = "Coil Hall Bus Switch", Look = "labswitch", Kind = "power", Needs = "fuse",
                                    Desc = "The bus switch for the Coil Hall, at the end of the cable tunnel. A fuse is missing.",
                                    Says = "[FOUNDRY] The cables in the tunnel start to hum, and round the ring three dead doors come alive." },
            },
            // Secret Lab Gamma (hidden, Tier 5; dungeons/secret_lab_gamma.mmd): the Alpha locks; the Sealed Vault itself opens with the story key (StorySystem, EelBoss.VaultKeyId).
            ["secret_lab_gamma"] = new Dictionary<char, Thing>
            {
                ['3'] = new Thing { Name = "Gamma Security Locker", Look = "stash_footlocker", Kind = "staff", Ru = 170, Desc = "A grey locker in the security office, a duty roster curling on the door.",
                                    Says = "[GAMMA] A security badge, the stripe still bright. The labs' reader will take it." },
                ['4'] = new Thing { Name = "Gamma Lab Spares", Look = "stash_toolbox", Kind = "fuse", Desc = "A spares case under a lab bench, marked with the station's antenna sign.",
                                    Says = "[GAMMA] A long glass fuse, GEN HALL on the cap." },
                ['5'] = new Thing { Name = "Gamma Generator Switch", Look = "labswitch", Kind = "power", Needs = "fuse",
                                    Desc = "The generator hall's switch. A fuse holder stands empty.",
                                    Says = "[GAMMA] The generators wind up. In the atrium a dead door wakes, and something in the Signal Room starts to hum." },
            },
            // Secret Lab Beta (hidden, Tier 4; dungeons/secret_lab_beta.mmd): the Alpha locks; Boss 1's wing stays behind its cracked wall (Boss.TryBreakWall).
            ["secret_lab_beta"] = new Dictionary<char, Thing>
            {
                ['3'] = new Thing { Name = "Guard Room Locker", Look = "stash_footlocker", Kind = "staff", Ru = 130, Desc = "A guard's locker, a uniform jacket still on its hanger.",
                                    Says = "[BETA] A guard's badge clipped to the jacket. The lab wing's reader will take it." },
                ['4'] = new Thing { Name = "Archive Strongbox", Look = "stash_toolbox", Kind = "fuse", Desc = "A strongbox on the archive's bottom shelf, among boxes of punched tape.",
                                    Says = "[BETA] Somebody locked a generator fuse away with the files. WARD CIRCUIT, the tag says." },
                ['6'] = new Thing { Name = "Lab Notebook", Look = "stash_pda", Kind = "code", Desc = "A researcher's notebook left open on a bench, a door code written inside the cover and underlined twice.",
                                    Says = "[BETA] Four digits, underlined: the generator room's keypad." },
                ['5'] = new Thing { Name = "Beta Generator Switch", Look = "labswitch", Kind = "power", Needs = "fuse",
                                    Desc = "The generator's switch, tagged DO NOT ENERGISE - WARD. The fuse is gone.",
                                    Says = "[BETA] The generator catches. Across the reception, the Isolation Ward's door unlocks itself." },
            },
            // the flooded levels under it (Yan, 2026-11-04): each level's pump drops its water and opens the stairs further down.
            // the last pump is gated by the eel (Stashes.Blocked: Story flag eel_fled)
            [Depths1] = new Dictionary<char, Thing> { ['6'] = new Thing { Name = "Cistern Pump", Look = "labswitch", Kind = "drain1", Needs = "power",
                Desc = "A sump pump's panel, its lamps dark: no power reaches this level.",
                Says = "[DEPTHS] The pump bangs into life. The black water slides off the stairs going down." } },
            [Depths2] = new Dictionary<char, Thing> { ['6'] = new Thing { Name = "Lower Pump", Look = "labswitch", Kind = "drain2", Needs = "drain1",
                Desc = "The lower pump's panel.",
                Says = "[DEPTHS] Somewhere below, a second pump answers. The water drops again." } },
            [Depths3] = new Dictionary<char, Thing> { ['6'] = new Thing { Name = "Main Sump Pump", Look = "labswitch", Kind = "drain3", Needs = "drain2",
                Desc = "The last pump, at the very bottom of the station.",
                Says = "[DEPTHS] The last pump screams, then settles. The whole station drains, all the way up to the basin." } },
        };

        static string Zone => TerrainMap.ZoneId;
        static string KeyIn(string zone, string kind) => Prefix.TryGetValue(zone ?? "", out var p) ? p + "_" + kind : null;
        /// <summary>This map's key of a kind ("staff", "fuse", "power", "drain"), or null if the map has no locks.</summary>
        public static string Key(string kind) => KeyIn(Zone, kind);
        /// <summary>Has this flooded level's pump run (its water is gone)?</summary>
        public static bool Drained(string zone) => Has(KeyIn(zone, WaterKind(zone)));
        static bool Has(string key) => key != null && MacroState.Current != null && MacroState.Current.Keys.Contains(key);

        public static bool Is(string key) => key != null && Prefix.Values.Any(p => key.StartsWith(p + "_"));

/// <summary>Every lock look and the key kind it asks for. Doors (TileType.Door) and barriers (TileType.Difficult); an unlocked one's look gets "_on".
        /// 2026-11-11: + the keypad door (the code), the steam door (the coolant valve), three lever gates (portcullis a/b/c: lever1/2/3).</summary>
        static readonly Dictionary<string, string> LockKinds = new Dictionary<string, string>
        {
            ["rockfall"] = "pry", ["irongate"] = "gate", ["sealslab"] = "code", ["door_badge"] = "staff", ["door_power"] = "power",
            ["door_keypad"] = "code", ["door_steam"] = "coolant", ["portcullis_a"] = "lever1", ["portcullis_b"] = "lever2", ["portcullis_c"] = "lever3",
        };
        static bool IsBarrier(string look) => look != null && !look.StartsWith("door_");

        /// <summary>The key a lock's look asks for, or null for an ordinary door (and for one already unlocked, "_on").</summary>
        public static string KeyOf(string look) => look == null ? null : look == "deepwater" ? Key(WaterKind(Zone)) : LockKinds.TryGetValue(look, out var kind) ? Key(kind) : null;

        /// <summary>A lock nobody can pass yet: it has a lock and the player doesn't hold the key.</summary>
        public static bool Locked(TerrainTile t)
        {
            if (t == null || t.Look == null || !(t.Type == TileType.Door || t.Type == TileType.Difficult)) return false;
            var k = KeyOf(t.Look);
            return k != null && !Has(k);
        }

/// <summary>A dungeon's own line for a lock's look ("zone:look"), where the default line (an iron gate's guard post...) does not fit the place.</summary>
        static readonly Dictionary<string, string> Nags = new Dictionary<string, string>
        {
            ["siege_catacombs:irongate"] = "An iron gate, older than the war. The sexton will have kept its key close to the bones he tended.",
            ["siege_catacombs:sealslab"] = "The crypt's slab, a dial of four rings sunk in it. The burial roll would have the numbers.",
            ["drydock_tunnels:irongate"] = "A sluice gate, wound shut. The wheel's handle is gone from its square; whoever worked the sluices will have hung it up in their chamber.",
        };

        static float _nagAt = -99f;
        /// <summary>Bumping into a lock says what it wants (not more than every 2 seconds). True if (x, y) was one.</summary>
        public static bool Nag(int x, int y)
        {
            var t = x >= 0 && y >= 0 && x < TerrainMap.Width && y < TerrainMap.Height ? TerrainMap.GetTile(x, y) : null;
            if (!Locked(t)) return false;
            if (Time.time - _nagAt > 2f)
            {
                _nagAt = Time.time;
                GameLog.Write(Nags.TryGetValue(Zone + ":" + t.Look, out var own) ? own
                            : t.Look == "door_keypad" ? "The door is locked. A keypad beside it blinks red: ENTER CODE."
                            : t.Look == "door_steam" ? "Steam hisses from the door's seams, too hot to go near. A coolant valve somewhere would have to be opened."
                            : t.Look.StartsWith("portcullis_") ? "A portcullis is down across the passage, a coloured plate on its beam. Somewhere a lever with the same colour lifts it."
                            : t.Look == "sealslab" ? "A great stone slab seals the way, a carved dial in its face. Somewhere in the crypt the numbers must be written down."
                            : t.Look == "irongate" ? "An iron gate across the tunnel, padlocked. Whoever kept the guard post will have had the key."
                            : t.Look == "rockfall" ? "A slab of fallen rock fills the tunnel, too heavy to shift by hand. Something to lever it with, maybe."
                            : t.Look == "door_badge" ? "The door is locked. A card reader blinks blue: STAFF BADGE REQUIRED."
                            : t.Look == "door_power" ? "The blast door is dead. A hazard light: NO POWER ON THIS LEVEL."
                            : "The water is black and over your head. Somewhere a pump would have to run to clear it.");
            }
            return true;
        }

        /// <summary>What picking a key up says and does: the badge and the power relight their doors green, the drain empties the deep water.</summary>
        public static void Granted(string key)
        {
            var thing = Things.SelectMany(z => z.Value.Values.Select(v => (z.Key, v))).FirstOrDefault(p => KeyIn(p.Key, p.v.Kind) == key);
            if (thing.v != null) GameLog.Write(thing.v.Says);
bool any = false;
            foreach (var kv in LockKinds) if (key == Key(kv.Value)) { Relight(kv.Key); any = true; }
            if (any && key != Key("staff")) Sfx.Play("explosion", null, 0.4f, 0.05f, 0.25f);
            Thrown(key);
            if (key == Key(WaterKind(Zone))) { Sfx.Play("explosion", null, 0.35f, 0.05f, 0.2f); Whirlpools(key); Drain(); }
        }

        /// <summary>The doors of one kind stop looking locked (their look gets "_on": a green light) - they're ordinary doors now.</summary>
static void Relight(string look)
        {
            bool barrier = IsBarrier(look);
            for (int y = 0; y < TerrainMap.Height; y++)
                for (int x = 0; x < TerrainMap.Width; x++)
                {
                    var t = TerrainMap.GetTile(x, y);
                    if (t.Look == look && (t.Type == TileType.Door || (barrier && t.Type == TileType.Difficult))) { t.Look = look + "_on"; TerrainMap.DirtyTiles.Add((x, y)); }
                }
        }

        /// <summary>A lever that gave this key shows as thrown (its look gets "_on").</summary>
        static void Thrown(string key)
        {
            foreach (var o in TerrainMap.InteractiveObjects)
            {
                if (o.GrantsKey != key) continue;
                var t = TerrainMap.GetTile(o.X, o.Y);
                if (t.Look != null && t.Look.StartsWith("lever_") && !t.Look.EndsWith("_on")) { t.Look += "_on"; TerrainMap.DirtyTiles.Add((o.X, o.Y)); }
            }
        }

        /// <summary>RT fx 4: as a pump starts, up to three whirlpools turn in the deep water round it (nearest first, 3+ tiles apart).</summary>
        static void Whirlpools(string key)
        {
            var pump = TerrainMap.InteractiveObjects.FirstOrDefault(o => o.GrantsKey == key);
            if (pump == null) return;
            var spots = new List<(int x, int y)>();
            for (int r = 1; r <= 7 && spots.Count < 3; r++)
                for (int dy = -r; dy <= r && spots.Count < 3; dy++)
                    for (int dx = -r; dx <= r && spots.Count < 3; dx++)
                    {
                        int x = pump.X + dx, y = pump.Y + dy;
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r || x < 0 || y < 0 || x >= TerrainMap.Width || y >= TerrainMap.Height) continue;
                        var t = TerrainMap.GetTile(x, y);
                        if (t.Type != TileType.Difficult || t.Look != "deepwater") continue;
                        if (spots.Any(s => Mathf.Max(Mathf.Abs(s.x - x), Mathf.Abs(s.y - y)) < 3)) continue;
                        spots.Add((x, y));
                    }
            foreach (var s in spots) CombatFx.PxAt("whirlpool", s.x, s.y);
        }

        /// <summary>The deep water drops to knee height: plain flooded floor (slow, like any 'w').</summary>
        static void Drain()
        {
            for (int y = 0; y < TerrainMap.Height; y++)
                for (int x = 0; x < TerrainMap.Width; x++)
                {
                    var t = TerrainMap.GetTile(x, y);
                    if (t.Type == TileType.Difficult && t.Look == "deepwater") { t.Look = null; TerrainMap.DirtyTiles.Add((x, y)); }
                }
        }

        /// <summary>Called when a map is built: locks whose key the player already holds start open (a save, a second visit).</summary>
        public static void Refresh()
        {
foreach (var kv in LockKinds) if (Has(Key(kv.Value))) { Relight(kv.Key); Thrown(Key(kv.Value)); }
            if (Has(Key(WaterKind(Zone)))) Drain();
        }

        /// <summary>Every key a dungeon's locks ask for (tests: walk the whole map as someone who found them all).</summary>
        public static IEnumerable<string> KeysOf(string zone) =>
            !Prefix.ContainsKey(zone ?? "") ? Enumerable.Empty<string>()
            : (Things.TryGetValue(zone, out var d) ? d.Values.Select(v => KeyIn(zone, v.Kind)) : Enumerable.Empty<string>())
              .Concat(new[] { KeyIn(zone, "power"), KeyIn(zone, WaterKind(zone)) }).Distinct();   // keys found on another level count too

        /// <summary>The plan letters of a dungeon's key objects, as stash names for FloorPlan (null if it has none).</summary>
        public static Dictionary<char, string> StashesOf(string zone) =>
            Things.TryGetValue(zone, out var d) ? d.ToDictionary(kv => kv.Key, kv => kv.Value.Name) : null;

        /// <summary>The map's interactive object for a named key object of this dungeon; false for any other name.</summary>
        public static bool Spawn(string name, int x, int y)
        {
            if (Zone == null || !Things.TryGetValue(Zone, out var d)) return false;
            var th = d.Values.FirstOrDefault(v => v.Name == name);
            if (th == null) return false;
            TerrainMap.InteractiveObjects.Add(new InteractiveObjectInstance
            {
                X = x, Y = y, Name = th.Name, Stat = new[] { "Strength" }, Dc = 0, Reward = th.Ru > 0 ? new InteractiveReward { Ru = th.Ru } : null,
                Description = th.Desc, GrantsKey = Key(th.Kind), Requires = th.Needs == null ? null : Key(th.Needs),
            });
            var t = TerrainMap.Tiles[y, x];
            if (t.Look == null) { t.Look = th.Look; if (th.Look != "labswitch" && !th.Look.StartsWith("lever_")) t.Type = TileType.Difficult; }   // boxes are climbed over; a switch is a panel on the floor
            return true;
        }
    }
}
