// New (Batch 7): ported from the Zone Map file's "Location Details" sections - every one of the
// 29 zones' 2-5 named landmarks, verbatim flavor text included. Previously these only existed as
// markdown a human GM would read; nothing in Unity ever surfaced a zone's actual place-names, so
// every zone looked like an anonymous field of random loot containers (TerrainMap.
// SpawnInteractiveObjects) with no identity of its own. See LandmarkPlacement.cs for how these get
// placed on the tactical grid and surfaced to the player.
using System.Collections.Generic;

namespace GameData
{
    public class LandmarkDef
    {
        public string Name;
        public string Description;
        // Non-null on the handful of landmarks the Zone Map file explicitly calls out as a hidden
        // node's physical entrance (Rust Culvert, Loading Dock, ...) - already gated for real by
        // MacroMapDatabase's AccessCondition/ZoneTravel.SearchArea; this just lets the landmark's
        // own flavor text say so once that node is actually discovered.
        public string HiddenNodeId;
    }

    public static class LandmarkDatabase
    {
        private static LandmarkDef L(string name, string desc, string hiddenNode = null) =>
            new LandmarkDef { Name = name, Description = desc, HiddenNodeId = hiddenNode };

        public static readonly Dictionary<string, List<LandmarkDef>> ByZone = new Dictionary<string, List<LandmarkDef>>
        {
            ["perimeter"] = new List<LandmarkDef>
            {
                L("Checkpoint Gate", "The rusted vehicle barrier and guard hut where new arrivals used to be processed - now abandoned but structurally intact."),
                L("Watchtower 3", "A leaning steel tower with a sniper's-eye view over the northern field; the ladder is missing several rungs."),
                L("Rust Culvert", "A drainage tunnel running under the perimeter road; a hidden path is said to lie behind the brush near its mouth.", hiddenNode: "secret_cave"),
                // the map overhaul: the rest of the Perimeter's places on the Tier 1 map
                L("Old Farmstead", "A farmhouse, a barn and a long cowshed round a fenced yard. Whoever holds it can see anyone coming up the track."),
                L("Old Cemetery", "Rows of leaning headstones behind a low fence, and the roofless-looking chapel at the top. Nobody buries anyone here any more."),
                L("Chapel Crypt", "A trapdoor behind the chapel altar. Cold air comes up the steps, and something down there has been chewing on the old coffins."),
                L("Roadside Station", "A fuel stop at the fork, its pumps long dry. The shop's windows are boarded and somebody keeps coming back for the stock room."),
                L("Army Field Camp", "The cordon troops' camp: sandbags, tents and a trench line, abandoned in a hurry. Watchtower 3 still leans over the corner."),
                L("Crashed Car", "A car nosed into the ditch with the driver still in it. A note in the glovebox: \"stash behind the bus stop, south side.\""),
                L("Bus Stop", "A concrete shelter on the main road. The note from the crashed car was right - there's a loose slab behind it."),
                L("Transformer Yard", "A fenced substation where the pole line ends. The transformers still hum when the weather turns."),
                L("Lone Grave", "A single cross in the open field. The note pinned to it reads: \"Left you your share, brother. Under the stones.\""),
                L("Collapsed Footbridge", "The old plank bridge over the drainage stream, broken in the middle. You can still wade across."),
                L("Sniper Nest", "A hunting stand rebuilt as a firing post over the fork. Somebody left a bag up there."),
                L("Burnt-Out Carrier", "An armoured carrier burnt out on the depot road, from the first months after the cordon went up."),
            },
            ["bonfire_village"] = new List<LandmarkDef>
            {
                L("The Bonfire Camp", "A semi-permanent ring of stones and salvaged furniture where Wanderers and freelancers trade rumors - the zone's closest thing to a social hub, now the middle of a walled village."),
                L("Yefim's Footlocker", "A tarp-roofed lean-to around a modified shipping crate. Old Man Yefim trades out of it (B to trade)."),
                L("Bunkhouse", "Rows of bunks for anyone who pays their way. Rest here (Z) and you wake up patched up."),
                L("Medic's House", "The village medic works out of the old plaster house - bandages, stitches, and no questions."),
                L("Repair Bench", "A tin shed with a vice, a grinder and a bench full of parts. Bring your gear when it's falling apart."),
                L("Job Board", "A pole by the fire stuck full of notes: jobs, warnings, and who owes whom (J for jobs)."),
            },
            ["marshes"] = new List<LandmarkDef>
            {
                L("The Causeway", "A single raised wooden walkway, the only dry through-route across the marsh; falling off means wading in contaminated water."),
                L("Sunken Tanker", "A rusted chemical tanker truck half-submerged in the marsh, looted long ago but still used as a landmark and occasional sniper's roost."),
                L("Reed Blind", "Thick reed cover along the eastern bank, a favored ambush spot for Raiders."),
                L("Fishing Village", "Stilt huts and jetties on the one dry island in the marsh. The fishermen left, but their boat shed and drying racks are still standing."),
                L("Headman's House", "The biggest house in the fishing village. The headman kept the keys to everything round here in his strongbox."),
                L("Pumping Station", "The station that once kept the fields dry. Its pumps are dead and the basement under the pump room is flooded."),
                L("Pump Room", "A concrete room with a hatch in the floor - the stairs go down into the basement.", hiddenNode: null),
                L("Flooded Bunker", "A concrete bunker entrance on a hummock in the marsh, its steel door locked. The key was never handed back."),
                L("Fisherman's Shack", "A shack on stilts at the edge of the reeds, with a jetty and a padlocked locker."),
                L("Duck Blind", "A hide in the reeds with a raised platform. Good for shooting ducks, or people on the causeway."),
            },
            ["smugglers_run"] = new List<LandmarkDef>
            {
                L("The Cutting", "The main rail trench itself, littered with abandoned handcarts and crates. One handcart near the midpoint has been tarped over and fitted with a padlocked crate lid - \"The Runner\" works out of it."),
                L("Blind Curve", "A sharp bend in the trench with poor sightlines both ways; a common ambush point."),
                L("Overwatch Ridge", "The embankment lip above the cutting, reachable by a scramble path - good vantage for ranged characters."),
                L("Rail Depot", "The freight depot at the cutting's mouth: a warehouse, offices, a loco shed and wagons left on the sidings. Smuggling crews use it as a waystation."),
                L("Loading Office", "A concrete office on the depot yard. Under the floor is a hatch into the smugglers' tunnel."),
                L("Water Tower", "A tank on stilts over the depot yard. From the top you can see the whole yard and the road."),
                L("Signal Box", "The signalman's hut at the mouth of the cutting, windows shot out."),
                L("Derailed Wagon", "A freight wagon off its rails near the north end of the cutting, crates still spilling out of it."),
                L("Dead Scavenger's Camp", "A cold campfire in a forest clearing and what's left of the scavenger who camped there. His pack is still under the log."),
                L("Old Well", "A stone well west of the depot. Something heavy is tied to the rope down there."),
                L("Logging Camp", "Log piles, a sawmill shed and a few cabins in the middle of the woods. Squads like it for the cover."),
                L("Hunter's Hide", "A plank hide deep in the woods, with a locker the hunter never came back for."),
                L("Radio Mast", "A relay mast on a hill above the woods, with an equipment shed. You can see half the tier from up here."),
            },
            ["scrapyard"] = new List<LandmarkDef>
            {
                L("Crusher Row", "A line of dead hydraulic car-crushers, some still hissing residual pressure; good cover, bad place to stand still."),
                L("The Maze", "The densest stack of scrap - a genuine navigational hazard with blind corners everywhere."),
                L("Scrap King's Nest", "An elevated shack built from car doors atop the tallest pile, favored by whoever currently controls the yard."),
                L("Crusher Pit", "A hatch in the concrete between the crushers, down into the hydraulic pit. Things live in the oil down there."),
                L("Truck Stop", "A motel and a diner on the old highway, where convoys used to stop. Raiders use the rooms now."),
                L("Tire Fort", "A ring of stacked tires somebody built to hold off dogs. It didn't work."),
                L("Dog Kennels", "The yard's old guard-dog pens. The gnawed bones aren't all from dogs."),
                L("Rusted Bus", "A bus rusted onto its rims among the wrecks, good cover from three sides."),
                L("Scrapper's Shack", "A tin shack where a scrapper sorted copper. His locker is still bolted to the wall."),
            },
            ["industrial_basin"] = new List<LandmarkDef>
            {
                L("Silo Row", "Four grain/chemical silos, two collapsed, two still climbable for overwatch."),
                L("Central Plant", "The main processing building - a multi-level interior, good for a vertical firefight."),
                L("Loading Dock", "A flat concrete apron where a sealed service hatch is hidden.", hiddenNode: "secret_lab_alpha"),
                L("Workers' Flats", "Plaster blocks where the plant's workers lived. Every flat was left with the table still laid."),
                L("Gatehouse", "The plant's gatehouse on the south road, the barrier still down."),
                L("Smokestack", "A brick stack with iron rungs up the side. Somebody keeps a rifle bag at the top."),
                L("Crane Cab", "A gantry crane over the yard. From the cab you can see half the basin."),
                L("Chemical Dump", "Rusted drums leaking into the ground. Detectors go mad here."),
                L("Substation", "The plant's fenced substation, the transformers long dead."),
            },
            ["black_valley"] = new List<LandmarkDef>
            {
                L("Old Mine Works", "The pithead, the headframe and the spoil heaps of the valley's coal mine. The adit into the rim is still open."),
                L("Mine Adit", "The mine's entrance, cut into the north rim, ore rails running into the dark."),
                L("The Ford", "The one place the gulley can be waded, in full view of both banks."),
                L("Rail Bridge Camp", "A camp under the piers of the broken rail bridge. Someone left in a hurry and didn't take everything."),
                L("Radio Repeater", "A repeater mast on the rim, still blinking on a car battery."),
                L("Burnt Tank", "A tank burnt out on the valley floor, its turret thrown ten metres off."),
                L("Hanged Man's Tree", "A dead tree with a rope on it. Carved in the bark: \"what's mine is under the roots\"."),
                L("Overturned Lorry", "A supply lorry on its side by the valley road. Its cargo was taken, but not the driver's box."),
                L("Hunter's Shack", "A plank shack in the pines on the valley's east end."),
                L("The Wreck Line", "Burned-out trucks strung along the valley floor - this zone's main cover."),
                L("Sniper's Lip", "The north ridge overlooking the valley - exposed, but commands the whole route."),
                L("Chokepoint Bridge", "A half-collapsed footbridge, the only crossing over the valley's central gulley."),
            },
            ["scavengers_hub"] = new List<LandmarkDef>
            {
                L("The Bazaar", "Rows of container-stalls selling weapons, armor, consumables, and artifacts - the primary shop location."),
                L("Sentinel Checkpoint", "The guarded gate controlling entry; Sentinels keep the peace inside."),
                L("The Rusty Anchor", "The Hub's bar/inn - the best place for rumor hooks, faction contacts, and job boards."),
                L("Repair Yard", "A fenced lot where a mechanic-type NPC could be stationed for gear repair/upgrade work."),
                L("Hub Bunkhouse", "Paid bunks behind the bazaar. Sleep here (Z) and wake up patched up."),
                L("Sentinel Barracks", "The Sentinels' quarters by the north wall. They keep the peace in here, and they mean it."),
                L("Sentinel Outpost", "A sandbagged post on the road north of the hub, the Sentinels' first line."),
            },
            ["rail_yards"] = new List<LandmarkDef>
            {
                L("Roundhouse", "The old locomotive maintenance building, now a Contractor staging point."),
                L("Container Stacks", "Rows of stacked freight containers forming a maze of firing lanes."),
                L("Signal Tower", "A tall trackside control tower with sightlines across the whole yard."),
                L("Rail Culvert", "A drainage tunnel beneath the tracks.", hiddenNode: "maintenance_tunnels"),
                L("Freight Platform", "The long concrete loading platform beside the tracks."),
                L("Burned Locomotive", "A diesel locomotive burnt out on the middle track, two flatcars still coupled behind it."),
                L("Loco Depot", "The engine shed in the south-east of the yards. Its pits are deep enough to hide in."),
                L("Yard Water Tower", "A water tower for the engines - a long view over the depot."),
                L("Track Workers' Hut", "A gangers' hut by the far rails, their locker still padlocked."),
                L("Billboard Perch", "A rickety platform behind a billboard frame. A sniper's nest, by the shell casings."),
            },
            ["dried_lake"] = new List<LandmarkDef>
            {
                L("Stranded Ferry", "The lake ferry, beached on the lakebed when the water went. Its car-deck door hangs open."),
                L("Lakeside Camp", "Holiday cabins and a lodge on the old west shore, the lake a kilometre off now."),
                L("Lifeguard Tower", "The lifeguard's chair on stilts, staring at a beach with no water."),
                L("Old Pier", "A pier running out over cracked mud."),
                L("Water Works", "The settling pools and pump house that once fed the hub. The pools still hold water."),
                L("Downed Helicopter", "A helicopter that came down on the lakebed, its rotor bent into the mud."),
                L("Weather Station", "A fenced weather station, the instruments still turning."),
                L("Dead Surveyor", "A Surveyor in a hazmat suit, dead by his crate. His notes mention a cache."),
                L("Rowboat", "An upturned rowboat on the old shore. Something's under it."),
                L("Roadside Shrine", "A little shrine of stones and plastic flowers by the road south."),
                L("The Flats", "The open dry lakebed itself - almost no cover, easy to be spotted from a distance."),
                L("Sub-Station House", "The main transformer building, partially caved in; Surveyors sometimes camp here to study residual anomalous activity."),
                L("Tower Line", "The row of transmission towers crossing the lakebed - climbable for overwatch."),
                L("Drainage Sump", "A sunken concrete basin at the lake's lowest point.", hiddenNode: "sub_station_depths"),
            },
            ["vanguard_barracks"] = new List<LandmarkDef>
            {
                L("Parade Ground", "The open central yard - now a kill-zone with no cover."),
                L("Barracks Row", "Three long dormitory buildings, good for room-to-room fighting."),
                L("Motor Pool", "A garage of stripped military vehicles - useful cover and a common ambush point."),
                L("Command Post", "A sandbagged bunker at the compound's north end, usually held by whichever faction currently controls the barracks."),
            },
            ["ghost_town"] = new List<LandmarkDef>
            {
                L("Town Hall", "The town hall on the square. Its basement stair is round the side, in a concrete hut."),
                L("School", "The town school, two storeys of classrooms with the desks still in rows."),
                L("Flats", "A block of flats on the south edge of town. Every door kicked in."),
                L("Petrol Station", "The town's petrol station, pumps dry, the shop full of broken glass."),
                L("Laundromat", "A laundromat with its machines open. One of them holds more than laundry."),
                L("Pharmacy", "The town pharmacy, picked over - but not by anyone who knew where to look."),
                L("Balcony Perch", "A rooftop perch over the south streets, sandbags on the parapet."),
                L("Playground", "Swings and a climbing frame, rusting in the long grass."),
                L("Bus Stop", "A bus shelter by the school, its timetable still on the wall."),
                L("Old Checkpoint", "The army's checkpoint on the west road out of town, abandoned in the evacuation."),
                L("Main Street", "The town's central row of shopfronts, most looted, some booby-trapped. One has been cleared and re-secured - a squatter runs their trade stall out of the old storeroom in back."),
                L("The Church", "A stone chapel at the town's edge, structurally sound and a common shelter point."),
                L("Row Houses", "A residential block, dense interior cover, easy to get flanked in."),
                L("Town Hall Basement", "Beneath the town hall - a sealed hatch is said to lie here.", hiddenNode: "secret_lab_beta"),
            },
            ["crimson_woods"] = new List<LandmarkDef>
            {
                L("Sentinel Firebase", "The Sentinels' sandbagged firebase on the road through the woods, their hold against the mutants."),
                L("Old Summer Camp", "A children's summer camp in the red forest: cabins, a mess hall, a pond. Its bomb shelter is still sealed - nearly."),
                L("Boathouse", "The camp boathouse on the pond, canoes rotting on the racks."),
                L("Shelter Hatch", "A concrete hut at the edge of the camp with a hatch down into the bomb shelter."),
                L("Ranger's Watchtower", "A fire lookout over the east woods."),
                L("Ranger Station", "The forest rangers' station, its radio long dead."),
                L("Hunter's Blind", "A hide of cut brush on a game trail."),
                L("Dead Zealot", "An Obelisk zealot, dead under a red tree. His last note points to a cache."),
                L("Forester's Hut", "A forester's hut in the north-west woods, its locker untouched."),
                L("Old Trench", "A trench line from some older war, dug through the forest floor."),
                L("Fallen Pylon", "A power pylon brought down across the clearing."),
                L("The Red Canopy", "The deep forest interior - thick tree cover, short sightlines."),
                L("Obelisk Shrine", "A crude stone marker erected by Obelisk zealots deep in the woods; treated as sacred ground by that faction."),
                L("Deadfall Clearing", "A wide clearing choked with fallen, petrified-looking trees - a common mutant nesting ground."),
                L("Sinkhole Grove", "A cluster of exposed roots and a collapsed sinkhole.", hiddenNode: "deep_caverns"),
            },
            ["quarry_basin"] = new List<LandmarkDef>
            {
                L("Mining Camp", "The Contractors' mining camp on the west rim: offices, workshops, fuel and floodlights. Their base in the north."),
                L("Equipment Shed", "Containers of mining gear, the Contractors' stores."),
                L("Overturned Excavator", "A giant excavator on its side at the rim, tracks in the air."),
                L("Sandbag Nest", "A sandbagged nest covering the road down to the quarry."),
                L("Generator Shed", "A tin shed with a dead generator and a padlocked toolbox."),
                L("Sinkhole", "A fresh collapse in the rim gravel. Don't stand too close."),
                L("The Terraces", "The stepped quarry walls, offering tiered elevated positions all the way down. A repurposed shipping container bolted to one upper terrace serves as a Contractor equipment shed."),
                L("Pit Floor", "The flooded bottom - anomaly activity concentrated here; approach with a Detector."),
                L("Crane Rig", "A massive derelict excavator crane spanning part of the pit - climbable and unstable."),
                L("Blast Shack", "An old explosives storage shed at the quarry's rim.", hiddenNode: "underground_labyrinth"),
            },
            ["ship_graveyard"] = new List<LandmarkDef>
            {
                L("Flooded Slipway", "A rusted hatch at the foot of the old slipway, welded shut from below - or so it looks."),   // Batch 88
                L("Fish Cannery", "The cannery on the old waterfront, its lines rusted solid."),
                L("Dry Dock", "A ship still sits in the drained dry dock, propped on blocks. Ramps lead down to the dock floor."),
                L("Dock Pump House", "A steel door in the dock wall, into the pump tunnels that drained it."),
                L("Lighthouse", "The lighthouse over a sea that isn't there any more. A long view, and a long climb."),
                L("Beached Lifeboat", "A ship's lifeboat on the sand, its emergency locker still shut."),
                L("Dead Diver", "A diver in an old dive suit, dead on dry land."),
                L("Buoy Pile", "Rusted buoys heaped where the tide line was."),
                L("Hermit's Shack", "A shack of driftwood and sheet steel on the north edge, a hermit's stash inside."),
                L("Anchor Chains", "Anchor chains as thick as a man's leg, running off into the sand."),
                L("The Hulks", "The largest beached freighters - climbable, with interior compartments to clear."),
                L("Rust Flat", "The open ground between wrecks - exposed but fast to cross."),
                L("Captain's Row", "A cluster of smaller wrecked vessels favored by Wanderer squatters. One hull's intact cargo hold has been converted into shared storage."),
            },
            ["antenna_grid"] = new List<LandmarkDef>
            {
                L("Control Bunker", "The array's control bunker, half buried, its consoles still warm."),
                L("Transformer Field", "Rows of transformers humming in a fenced field. Hair stands up in here."),
                L("Fuel Depot", "Fuel tanks in two rows on the south-east edge. Don't shoot near them."),
                L("Mast Perch", "A climbing platform halfway up a mast, with a rifleman's bag tied to it."),
                L("Bone Circle", "A ring of stones round a pile of bones. The Obelisk were here."),
                L("Radio Van", "A broadcast van with its mast still raised, driver gone."),
                L("Generator Hut", "A concrete hut by the fuel depot with a locker of spares."),
                L("The Array", "The central cluster of tall antenna masts - climbable, dangerous in a storm."),
                L("Relay Station", "A squat concrete building at the array's center - Obelisk's forward outpost."),
                L("Dead Broadcast Van", "An abandoned mobile broadcast unit, sunk into the mud at the field's edge.", hiddenNode: "secret_lab_gamma"),
            },
            ["abandoned_city"] = new List<LandmarkDef>
            {
                L("Department Store", "Four floors of a department store, gutted, every floor a firefight waiting to happen."),
                L("Police Station", "The city police station. The armoury's been emptied - the cells haven't."),
                L("Parking Garage", "A concrete parking garage, dark and echoing, cars still in their bays."),
                L("Cinema", "The cinema on the plaza, the last film's poster still up."),
                L("Tram Depot", "The tram depot, two trams left on the rails."),
                L("Burned Tram", "A tram burnt out in the street, a good wall to hide behind."),
                L("Clock Tower", "A clock tower over the north streets with a sniper's bag at the top."),
                L("Dead Scout", "A Garrison scout, dead in a doorway. His notes mark a cache."),
                L("Cart Barricade", "Shopping carts and rubbish chained across a street."),
                L("Evacuation Buses", "Two buses that never left, their doors still open."),
                L("Kiosk", "A newspaper kiosk with a strongbox under the counter."),
                L("Highrise Row", "A line of partially collapsed towers - multi-story interiors, extreme verticality."),
                L("Central Plaza", "A wide open square dominated by a dead fountain - brutal in a firefight, no cover in the middle."),
                L("Subway Entrance", "A caved-in metro stairwell at the plaza's edge - the marked route down.", hiddenNode: "underground_bunker"),
                L("Garrison Barricade", "A fortified street barricade marking the edge of Garrison-held territory. A sandbagged storeroom built into the anchoring building holds the line's supplies."),
            },
            ["outskirts"] = new List<LandmarkDef>
            {
                L("Relic Cache", "Someone's hoard of relics from the crater, hidden under a wreck."),
                L("Petrified Tree", "A tree turned to grey stone, every leaf intact."),
                L("Obelisk Wall", "A wall daubed with the Obelisk's sign, facing the crater."),
                L("Lost Expedition", "The tents of an expedition that walked to the crater and didn't walk back."),
                L("Standing Stones", "Grey stones in a ring nobody remembers putting up. Compasses spin here."),
                L("The Crater Rim", "The outer edge - already anomaly-dense; approach with a Detector or don't approach at all."),
                L("The Wound", "The crater's center, where reality visibly bends - a narrative centerpiece, not a place to linger."),
                L("Scattered Relics", "Debris and equipment from expeditions that didn't make it back, littering the approach."),
            },
            ["hospital"] = new List<LandmarkDef>
            {
                L("Siege Ward Hospital", "The hospital the ward was named for, its wards turned into strongpoints."),
                L("Garrison Armory", "A steel-doored ward room where Garrison keeps its guns and medicine. Barred while they hold the base."),   // Batch 87
                L("Morgue Stair", "A concrete hut beside the hospital with stairs down to the morgue."),
                L("Mortar Pit", "A Garrison mortar pit behind the trenches, the tube still aimed at the hospital."),
                L("Ambulances", "Three ambulances burnt out in a row."),
                L("The Trenches", "Dug-in fighting positions crisscrossing the ward - the primary firefight terrain."),
                L("Bell Tower", "A shattered church tower still standing - prime sniper vantage over the whole ward."),
                L("Field Hospital Ruins", "A collapsed aid station - grim atmosphere, occasional salvage."),
                L("Catacomb Stair", "A stone stairwell beneath the bell tower's crypt.", hiddenNode: "siege_catacombs"),
            },
            ["reactor_core"] = new List<LandmarkDef>
            {
                L("Containment Hall", "The vast central chamber housing the ruptured reactor core itself - visibly wrong, heavily irradiated."),
                L("Control Room", "An elevated control gallery overlooking the containment hall - Obelisk's command point."),
                L("Cooling Tunnels", "A maze of maintenance tunnels beneath the facility - tight corridors, easy to get surrounded in."),
                L("The Last Gate", "The blast door sealing the facility's innermost chamber - the narrative endgame location."),
            },

            // ---- Hidden sub-nodes ----
            ["secret_cave"] = new List<LandmarkDef>
            {
                L("Stash Nook", "A dry alcove with old crates - the usual loot payoff for finding this node."),
                L("Cave Mouth", "The entrance chamber, still lit by daylight from outside."),
            },
            // the map overhaul: Tier 1 dungeons (TierDungeons.cs)
            ["testing_ground"] = new List<LandmarkDef>   // the menu's dev range (TestingGround.cs; the names are its constants)
            {
                L("Armory Crate", "Every weapon, suit and supply in the Zone, free for the taking. E to open."),
                L("Spawner Terminal", "Puts any faction at any rank, or any mutant, on the range. E to use."),
                L("Spawn Pad", "Where the terminal sends what you ask for."),
            },
            ["chapel_crypt"] = new List<LandmarkDef>
            {
                L("Crypt Stair", "Stone steps down from the chapel. The air smells of wet earth and something worse."),
                L("Ossuary", "Niches stacked with old bones. Some of them were moved recently."),
                L("Sealed Vault", "The family vault at the far end of the crypt, its slab pushed aside from the inside."),
            },
            ["pumping_basement"] = new List<LandmarkDef>
            {
                L("Basement Stair", "Concrete stairs down from the pump room, the bottom steps under black water."),
                L("Pump Hall", "The great pumps stand in a flooded hall. Pipes groan when the marsh water rises."),
                L("Control Room", "The station's control room, desks and lockers above the waterline."),
            },
            ["flooded_bunker"] = new List<LandmarkDef>
            {
                L("Blast Door", "The bunker's entrance from the marsh, behind the door the key opened."),
                L("Barracks", "Rows of rusted bunks and lockers. Whoever lived here left in a hurry."),
                L("Armory", "The bunker's armory. Most racks are empty - most."),
                L("Command Room", "Maps on the walls, a radio set, water to the knees."),
            },
            ["smugglers_tunnel"] = new List<LandmarkDef>
            {
                L("Hatch Ladder", "The ladder down from the depot's loading office."),
                L("Smugglers' Den", "A dug-out room shored with planks, where the crews sit out the patrols."),
                L("Cache Room", "The smugglers' store at the end of the tunnel. They don't like visitors in here."),
            },
            ["crusher_pit"] = new List<LandmarkDef>
            {
                L("Pit Ladder", "The rungs down from Crusher Row, slick with oil."),
                L("Ram Hall", "The long pit where the crushers' hydraulic rams stand, half in black water."),
                L("Oil Sump", "The pit's sump: oil, water and whatever fell in."),
                L("Foreman's Cage", "A mesh cage where the foreman kept the logbooks and his locker."),
            },
            ["valley_mine"] = new List<LandmarkDef>
            {
                L("Adit", "The mine's mouth. Daylight reaches in about ten metres."),
                L("Main Gallery", "The timbered main gallery, the ore rails running its length."),
                L("Flooded Drift", "A side drift the water took. Something swims in it."),
                L("Miners' Rest", "The miners' break room at the head of the gallery, lockers and a strongbox."),
            },
            ["stranded_ferry"] = new List<LandmarkDef>
            {
                L("Car Deck", "The ferry's car deck, the last cars still chained down."),
                L("Passenger Lounge", "Rows of seats and a bar, dust on everything."),
                L("Engine Room", "The ferry's engines in a bilge of black lake water."),
                L("Captain's Bridge", "The bridge, its windows looking out over a lake that isn't there."),
            },
            ["camp_shelter"] = new List<LandmarkDef>
            {
                L("Shelter Stair", "The stairs down from the camp, to a blast door that didn't quite close."),
                L("Dormitory", "Bunks for a hundred children, stacked three high."),
                L("Generator Room", "The shelter's diesel generators and air filters."),
                L("Shelter Command", "The command post at the back of the shelter, maps of the camp still pinned up."),
            },
            ["drydock_tunnels"] = new List<LandmarkDef>
            {
                L("Pump House Stair", "Steel stairs down from the dock wall, slick with old seawater."),
                L("Pump Gallery", "The dock's pumps in a long gallery, pipes running into the dark."),
                L("Main Culvert", "The culvert that drained the dock, still half full."),
                L("Sluice Chamber", "The sluice gates' chamber, the gears rusted solid."),
                L("Fitters' Store", "The ship-fitters' store, lockers of tools and parts."),
            },
            ["hospital_morgue"] = new List<LandmarkDef>
            {
                L("Morgue Landing", "The stairs end at a tiled landing. It's cold down here, colder than it should be."),
                L("Cold Room", "Steel drawers along both walls, some of them open."),
                L("Autopsy Theatre", "Two steel tables under dead lamps."),
                L("Boiler Room", "The hospital's boilers and the service corridor round them."),
            },
            ["secret_lab_alpha"] = new List<LandmarkDef>
            {
                L("Reception Airlock", "The sealed entry chamber - control panel long dead, hatch forced open."),
                L("Sample Storage", "Shelves of ruined specimen containers - unsettling but low threat."),
            },
            ["sub_station_depths"] = new List<LandmarkDef>
            {
                L("Flooded Corridor", "Ankle-to-knee-deep water throughout - difficult terrain."),
                L("Transformer Vault", "The lowest chamber - heart of the old power system."),
            },
            ["maintenance_tunnels"] = new List<LandmarkDef>
            {
                L("Tunnel Junction", "A four-way branch point - easy to get turned around."),
                L("Collapsed Section", "A partial cave-in forcing a crawl-through chokepoint."),
            },
            ["secret_lab_beta"] = new List<LandmarkDef>
            {
                L("Archive Room", "Intact filing and old terminals - good Intelligence-check flavor."),
                L("Isolation Ward", "A row of sealed cells - deeply unsettling, worth investigating carefully."),
            },
            ["deep_caverns"] = new List<LandmarkDef>
            {
                L("Root Hall", "A wide chamber choked with hanging roots - poor sightlines."),
                L("Crystal Pool", "A still underground pool with a faint anomalous glow."),
            },
            ["underground_labyrinth"] = new List<LandmarkDef>
            {
                L("The Fork", "The labyrinth's main branching point - three routes diverging."),
                L("Cave-In Chamber", "A partially collapsed hall - unstable footing."),
            },
            ["lattice_foundry"] = new List<LandmarkDef>   // Batch 88; Batch 91: the test level
            {
                L("Foundry Lift", "A freight lift down from the slipway, its cage bent open."),
                L("Decon Lock", "Two steel chambers with shower heads in the ceiling. Both doors still seal."),
                L("Records Office", "Filing cabinets and a dead terminal. Somebody burned papers in a bin."),
                L("Observation Room", "Thick glass over the firing range, scorched from the far side."),
                L("Firing Range", "A long test lane, its concrete blocks pitted by something that wasn't a bullet."),
                L("Service Stair", "A steel stair down to the synthesis level."),
            },
            ["lattice_foundry_lower"] = new List<LandmarkDef>   // Batch 91: the synthesis level
            {
                L("Service Stair", "A steel stair up to the test level."),
                L("Cell Synthesizer", "A humming machine of coils and glass that eats artifacts and presses rail cells."),
                L("Coil Hall", "A round hall of copper coils, warm as a living thing."),
                L("Crew Quarters", "Bunks and lockers; someone still lives down here."),
                L("Foreman's Office", "A desk, a cot, and a wall of schematics for a rifle that fires light."),
                L("Plant Room", "Pumps and pipes that keep the hall cold."),
            },
            ["secret_lab_gamma"] = new List<LandmarkDef>
            {
                L("Signal Room", "Banks of dead equipment still faintly humming."),
                L("Sealed Vault", "A reinforced door - the node's main loot payoff."),
            },
            ["underground_bunker"] = new List<LandmarkDef>
            {
                L("Blast Door Foyer", "The entry chamber - heavy sealed door already forced."),
                L("Ration Stores", "Old emergency supplies, mostly spoiled, some salvageable."),
            },
            ["siege_catacombs"] = new List<LandmarkDef>
            {
                L("Ossuary", "Shelves of old bones lining the walls - deeply atmospheric."),
                L("Sealed Crypt", "The deepest chamber, locked - worth a stat check to open."),
            },
        };

        public static List<LandmarkDef> ForZone(string zoneId) =>
            ByZone.TryGetValue(zoneId, out var list) ? list : new List<LandmarkDef>();
    }
}
