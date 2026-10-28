# agora vai - the story (draft, written with Yan step by step)

Started 2026-10-28. The base is Yan's two campaign files from a Gemini session (an escort of a defector engineer through
faction lands to a science bunker, then a descent into an underground complex; and the "flooded tunnels / split assault / the
cult were the scientists / final choice" beats). We are rewriting it into this world one tier at a time. It replaces the old
"Heart of the Zone" main story (`StorySystem.cs`) once built. **Nothing here is coded yet.** Names in *italics* are
placeholders until Yan picks.

## Our factions (cheat sheet, for writing)

| Ours | Role in the world |
|-|-|
| Wanderers | free stalkers, the everyman faction; Bonfire Village, the Scavenger's Hub |
| Raiders | bandit gangs, many and weak; the Scrapyard |
| Garrison | the military; hostile to the player and most factions, friendly with the Sentinels, neutral with the Surveyors |
| Sentinels | the strict armed order (anti-anomaly, disciplined) |
| Liberators | the free-spirited, anti-authority militia |
| Surveyors | the scientists' escorts and researchers; Dr. Osei, the Dried Lake |
| Contractors | mercenaries, hired guns |
| Obelisk | the fanatics guarding the centre |
| Outcasts, Prospectors, Cartel, IZC | smaller factions |

## Rules the story keeps

- The world lives by itself (rule 7): story enemies are real squads with a task, never spawned next to you. Dungeon bosses
  placed in a dungeon are part of the map.
- Never weaken enemies for difficulty (rule 6): when a fight is too big, the story says run, hide or go round.
- Co-op (rule 9): every mission must work with 2-4 players; story flags belong to the host's world.

## Tier 1 - Extraction

0. **Tutorial** (agreed earlier, `ACTION_DESIGN.md`): the trader's job (a dead courier's pack, 2-3 Blind Maws) and the
   barman's job (a 3-man Raider camp laid out for stealth).
1. **The Cut Fence** (Yefim, was T1-01). A mutant prowls by the gap at Watchtower 3 as a scapegoat: kill it and Yefim believes
   it. Inspecting the fence closely (optional) shows bolt-cutter cuts and a scrap of dark grey cloth; reporting that raises the
   reward and is the first hint of the Grey Robes.
2. **The Grey Robes** appear: a small team in dark grey robes over their armour (faction unknown), seen at a distance on the
   marsh roads with a hand-held device. Not hostile; they avoid other squads; they turn away when you come close.
   **Shot at (Yan: yes):** they don't fight back - a smoke charge, they walk into it and leave the map; hits only stagger them.
   They can't die in Tier 1.
3. **The Burst -> the Key -> the Engineer.** A looping distress call on Yefim's radio; the Flooded Bunker's key from the
   fishing village; a wounded defector engineer with a crate deep in the bunker. Getting him back past the searching Grey Robes
   unseen (they don't attack; they stop at the village edge and watch). **He stays in Bonfire Village until later (Yan).**
4. **The Culvert.** He hid a sealed canister in the Rust Culvert cave while fleeing; he gives you a radio and sends you for it.
   Deep inside, a **Blight Brute** wakes (Tier 4: 1200 HP, 40% physical resistance). Cutscene: the camera on the Brute rising,
   the Engineer on the radio - *"That's not... get out. GET OUT, RUN!"* - then a chase to the pipe you came in by; the Brute is
   too big to follow and pounds the opening. Hint: something is pushing deep-Zone mutants south.
5. **The Husks** (new enemy, from the moment you leave the cave). Zombified stalkers: pale, bloodied; some fire guns, some
   swing melee weapons, some claw bare-handed; **their loot is always Broken**. The canister is a field-test log: the Grey
   Robes are testing a portable mind-control device in the marsh - the missing courier and fishermen are husks now.
   **Husks in the whole game (Yan):** they appear in every tier, as strong as that tier through their armour and weapons like
   any NPC, but with **worse accuracy, slower, and they can't call reinforcements**. They roam as packs like any squad.
6. **Defend Bonfire Village** (last Tier 1 story mission). A husk horde comes out of the marsh at night in waves from several
   sides; the guards and Yefim fight with you; the Engineer must survive in the bunkhouse. **Lost only if you die or the
   Engineer dies** -> retry from a save before it. **Balance (Yan): the player should win most of the time unless he plays
   very badly** - measure it with a probe before shipping. Winning opens the road north to Tier 2 (the Causeway to the Scrapyard, or the Cutting to the Industrial Basin; a
   checkpoint on the road, or the Runner's tunnel round it).

Side content alongside: the Yefim arc, the Runner arc (opens the tunnel route), the board jobs (T1-02 to T1-05).

## Tier 1 walkthrough for a completionist (draft, 2026-10-28)

Places are the real Tier 1 landmarks (`TierMaps.Tier1`). Marked (fill-in) = my guess where Yan hasn't said yet.

**Phase 1 - Bonfire Village (Rookie).** Wake by the fire. Name the character (saved at creation). Walk the village: Yefim
(trader), the Medic's House, the Repair Bench, the Bunkhouse (rest heals), the bar, the Job Board. Barks and guards make it
feel safe: hostile squads almost never cross its edge.

**Phase 2 - The tutorial.** Trader's job: a dead courier's pack at a random spot (see Decided), 2-3 Blind Maws round
it - first gunfight, first loot. Barman's job: a 3-man Raider camp nearby, laid out for stealth - sneaking, a takedown, the
"!" detection marker.

**Phase 3 - The Perimeter opens up (south).** The Cut Fence at Watchtower 3 (scapegoat mutant; the close look = bonus pay and
the grey thread). Yefim 1: debris at the Checkpoint Gate. Yefim 2: his keepsake near Watchtower 3, Raiders guarding it - he
used to ride with them. Free exploring: the Old Farmstead (its upstairs floor), the Old Cemetery and the Chapel Crypt dungeon,
the Army Field Camp, the Roadside Station, the Bus Stop, the Crashed Car, the Lone Grave, the Transformer Yard. First stashes
(about 8 on the map: a key carried by a mutant, a code to find, or a post a squad holds; they move every 7 game days).
Board jobs: T1-05 escort a greenhorn to a random spot.

**Phase 4 - First sightings of "those".** On the marsh roads and the Causeway: robed stalkers at a distance, looking at the
ground, holding up a device. They turn away when you come close; shoot and they vanish in smoke.

**Phase 5 - The Marshes (east), main story.** The Burst: Yefim's radio catches the loop, traced to a random spot.
The Key: the Flooded Bunker key at the Headman's House in the Fishing Village (buy, favour or take). The Flooded Bunker
dungeon: Pavel, wounded, with his crate. Back to the village past "those" searching the reeds - unseen. Side: T1-02 the drum
at the Sunken Tanker (mutants), T1-03 the Raider ambusher at the Reed Blind, the Pumping Station and its basement dungeon, the
Fisherman's Shack, the Duck Blind.

**Phase 6 - Smuggler's Run (north-west).** The Runner arc: carry the sealed package to a random spot without
opening it; the ambush at Blind Curve; T1-04 the Rot Swarms there; the Rail Depot, Loading Office, Derailed Wagon, Water
Tower, Overwatch Ridge and its Sniper Nest, the Logging Camp. The Smugglers' Tunnel under the depot: the Raider-and-mutant
dungeon that later becomes the way round the checkpoint. (Runner 3 and Yefim 3 / Bogdan are written as Tier 2 jobs: they
can wait or be done at the end.)

**Phase 7 - The Culvert.** Pavel sends you into the Rust Culvert for the canister. The Blight Brute, the radio "RUN", the
chase to the pipe. From here on **Husks** roam Tier 1 - pale, bloodied, slow, poor shots, no radio, everything they drop
Broken. Back at the village Pavel opens the canister: "those" are testing a mind device in the marsh; the courier and the
missing fishermen are Husks now.

**Phase 8 - Mopping up.** Remaining stashes and landmarks, the dungeons' far-end stashes, Husk packs, rank-up (Rookie ->
Experienced is likely by here: milestones, jobs, kills). Gear up for the night.

**Phase 9 - Defend Bonfire Village (final).** At night a Husk horde comes out of the marsh in waves from several sides; guards
and Yefim fight with you; Pavel must live in the Bunkhouse. Win (most players should) -> the road north opens: the Causeway to
the Scrapyard or the Cutting to the Industrial Basin, through a checkpoint or under it by the Runner's tunnel.

**Length (estimate, not measured):** the agreed target for Tier 1 is 1-3 hours; a completionist with all of the above is
likely 2.5-4 hours. Walking every landmark alone takes 4-6 real minutes (Batch 111).

**Decided (Yan, 2026-10-28):**
- The tutorial courier, the distress loop, the Runner's contact and the greenhorn's destination are each **a different spot,
  not used by any other mission, picked when the world is created** - for replayability.
- **The Blight Brute rematch:** yes. When the player first reaches the trading hub (the Scavenger's Hub, Tier 3), Yefim
  radios to ask him to come back and deal with the Brute in the Rust Culvert. (It keeps its Tier 4 strength - rule 6.)
- **Yefim 3 (Bogdan) and Runner 3 (the patron) stay Tier 2 jobs:** "we need reason for the player to move in between zones,
  like in stalker" - quests that send you back and forth between tiers are wanted, not avoided.

## Names (Yan, 2026-10-28)

- The Grey Robes have **no name**: robed stalkers, still mysterious. People just call them "those" ("those were at the fence
  again"). "Grey Robes" stays only as our working description.
- The zombified stalkers: **Husks**.
- The Engineer: **Pavel**.

## Needed to build Tier 1 (later)

- Sprites: Grey Robes (16x16, robe over armour), husks (gun, melee, bare hands; pale, bloodied).
- Cutscenes: camera to a target, radio lines on screen, a short loss of control.
- Quests with real steps: talk to the giver, steps in order, hand-in, hidden bonus (the fence), escort.
- Grey Robe rules: never hostile, never killed, smoke and leave when threatened, hits stagger.
- Husk faction: tier-scaled kit, worse aim, slower, no radio; loot always Broken.
- The siege: waves for the village defence, plus a win-rate probe.
- The Blight Brute chase in the culvert (too big for the pipe).

## Tier 2 - The Road North (agreed as good enough for now, 2026-10-28)

Draft proposal given 2026-10-28 (Pavel's trail north, Secret Lab Alpha as the device's birthplace, the Liberators at the
Chokepoint Bridge, the Garrison at the Gatehouse, the escort to the Scavenger's Hub with a Contractor sniper team at
Sniper's Lip). Yan's story for Tier 2 still to come. Decided so far:
- **The Veteran comes later, in Tier 3.**
- **The escort can be failed** (Pavel dies -> retry); the lasting consequences wait until the ending is known.
- **The Runner's patron could be the Scrap King** (maybe).
- **Fighting the Liberators at the Bridge makes them hostile, but a contract for them makes up for it later** (details tbd).

### Tier 2, beat 1: "Taken" (Yan, 2026-10-28)

1. **The grab.** Soon after the player(s) first enter Tier 2, a Garrison squad ambushes them: a flashbang out of nowhere -
   white screen, sounds of a beating, black screen, a short load.
2. **Their base.** The player(s) wake at a Garrison base of operations at one POI (tbd; suggestion: the Loading Dock, which
   already has the hatch down to Secret Lab Alpha). The soldiers interrogate them about what is "down there". Whatever the
   answers, they strip **all weapons** and put them in a box inside the POI.
3. **Down the hatch.** Led at gunpoint down a tunnel into a big underground maze (in the spirit of a classic Zone
   underground; our own design). "Take us THERE." Auto-save here. The player must find the way; **taking too long gets them
   shot** -> game over -> reload the auto-save. Finding it should take **about 3-4 minutes** of wandering.
4. **The door.** In the middle of a long corridor: the hidden lab's door. The player tries a passcard they carry on the
   electronic lock - it fails, an alarm goes off, and **a horde of mutants** pours in from the far end of the corridor
   (10-15 on screen, but it must FEEL overwhelming).
5. **The run.** The soldiers turn on the mutants - the player(s) run all the way back to the entrance while hearing the
   mutants tear into the soldiers (shots, screams); after ~10 s some come after the players.
6. **Out.** At the top the weapons box gives everything back, and a hint (a **player journal** entry) that the place is
   worth coming back to with better gear. The hidden lab's content is to be designed later.

Decided (Yan, 2026-10-28):
- **The strip-search leaves the knife and armour**, and maybe a medkit and a bandage.
- **The grab always happens, on the first entry by either road** (the Cutting or the Causeway), at a spot the player is
  basically guaranteed to pass at the start of Tier 2. Suggestion: the Gatehouse on the Cutting road (a Garrison post already),
  and a Garrison roadblock where the Causeway comes up by the Scrapyard.
- **The Garrison is always hostile to the player and to most factions; friendly with the Sentinels, neutral with the
  Surveyors** (Yan). Note: today's table
  (`FactionDatabase.cs:84`) has the Garrison hostile only to the player, Raiders, Outcasts, Wanderers and mutants.

- **The POI is the Loading Dock** (the hatch down to Secret Lab Alpha). **The lab needs a rework** for this.

Still open (suggestions given, not confirmed): the passcard (from Pavel's
canister - why the Garrison think you know the way); which mutants; co-op details.

## Tier 2 walkthrough for a completionist (draft, 2026-10-28)

Yan's beat is "Taken"; the rest is my proposal (marked P) until Yan says otherwise. Places are the real Tier 2 landmarks.

**Phase 1 - Taken.** First step onto Tier 2, by the Cutting (the Gatehouse) or the Causeway (a Garrison roadblock by the
Scrapyard): flashbang, beating, black. The Loading Dock, the interrogation, the strip-search (knife, armour, a medkit and a
bandage stay), down the hatch, the maze, the door, the alarm, the horde, the run back. Weapons back from the box; journal
entry: come back here with better gear. The whole Garrison squad (5) died down there, so the camp is empty when you climb out; rival Raiders move in after.

**Phase 2 - Getting a foothold (P).** No safe zone on this map. The Scrap King's Nest is the only trader (Raiders, neutral
access): sell, buy ammo, take work. Side: T2-02 the stolen ledger in The Maze -> the Scrap King arc 1-2 (the Loading Dock
rivals who took it after the Garrison, by radio a day later - and the rival crew in Crusher Row).

**Phase 3 - Old business from Tier 1 (P).** Yefim 3: Bogdan, the Raider warlord, holds the Tire Fort - fight him or talk him
down. Runner 3: the Runner's patron is (maybe) the Scrap King - free the Runner and lose the King, or side with the King.

**Phase 4 - The trail north (P).** Pavel, on the radio from Bonfire Village, decodes more of the canister: "those" came down
through Black Valley, and only Dr. Osei at the Dried Lake (Tier 3) can be trusted with the data. You scout a route he can
survive. The strange broadcast (T2-05, moved here) comes from the Radio Repeater on the north rim - the device's signal.
Robed figures seen again, more of them, on the valley road. Husks here carry better gear than in Tier 1.

**Phase 5 - The crossings (P).** The gulley has three: the Chokepoint Bridge (Liberators, who search everyone and seize
anything high-tech - Pavel's crate included), the Ford (slow, in the open) and the Rail Bridge Camp (scramble between the
piers). Earn the Liberators' trust with T2-03 (guard their convoy over the bridge), pay them, scout another crossing, or fight
them (hostile; a contract for them later makes up for it).

**Phase 6 - Exploring and dungeons.** The Central Plant (big place), the Smokestack, Silo Row (T2-01 Pallid Prowlers), the
Workers' Flats, the Crane Cab, the Chemical Dump, the Substation; The Maze, Crusher Row and the Crusher Pit dungeon, the Truck
Stop, the Dog Kennels, the Rusted Bus, the Scrapper's Shack; the Old Mine Works and the Valley Mine dungeon, the Wreck Line,
the Burnt Tank, the Hanged Man's Tree, the Overturned Lorry, the Hunter's Shack. Six stash spots (locked, moving every 7 days).
Rank: likely Experienced -> Professional around here.

**Phase 7 - The escort (finale, P).** Back to Bonfire Village for Pavel (a trip down a tier). Up through the Runner's tunnel
or the Causeway (the Gatehouse is Garrison now), across the basins and a ridge pass, over the crossing you prepared, then the
north rim: a Contractor sniper team at Sniper's Lip (the T2-04 bounty gets its target) and the Raider checkpoint on the road
cut (how it goes depends on you and the Scrap King). Arrive at the Scavenger's Hub (Tier 3) - Yefim radios about the Blight
Brute. Pavel can die -> retry from a save; lasting consequences tbd.

**Crossing between tiers in Tier 2:** Yefim 3 and Runner 3 (Tier 1 arcs ending here), fetching Pavel from Tier 1, the
Liberators' make-up contract (later), the lab under the Loading Dock (later, with better gear), the Brute rematch (from the Hub).

**Length (estimate, not measured):** 3-5 hours for everything.

**Decided (Yan, 2026-10-28):**
- **The lab under the Loading Dock is open to the player whenever they want.** Going further into the tunnels, the way the
  mutants came from, they find the body that has the real passcard, and can then explore the lab: maybe Tier 4 loot and a
  boss fight (tbd, part of the lab rework).
- **The Garrison are at the Loading Dock only for that mission.** The whole squad (5 soldiers) dies in the tunnel; the camp is empty after, and rival Raiders take the place.
- **The Scrap King's first job comes after the Garrison mission:** about a day later he radios the player - clear the rival
  Raiders who moved into the Loading Dock.

## Tier 3+

Not written yet.
