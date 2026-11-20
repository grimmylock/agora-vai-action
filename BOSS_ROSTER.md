# Boss roster proposal (2026-11-20, from Yan: "about 1 to 2 more for tier 1, 1 for tier 2, 1 for tier 3, the specimen rematch in tier 4, tier 5 has the mind weaver then 2 more, tier 6 and 7 about 4 or 5")
DRAFT for Yan's pick. All names original. Arenas are places that already exist (landmark names from the databases). "Frame" = what Boss.cs already gives (parry glints, dodge tells, phases, break + finisher); "new" = needs new code. Art: **H** = a human (armour looks only, like Spool: cheap), **C** = a creature that needs real sprites (SpriteCook is out of credits, Pixel Lab has about 16 generations; hand-built pxart works but looks like concept art).
Already built: Tier 1 Blight Brute (chase), Tier 2 Spool + the Specimen (Lab Alpha), Tier 3 the Drowner (eel) + Elder Gaunt, Tier 4 Black Visor (Lab Beta) + the Preacher + Old Crimson.

| # | Tier | Boss | Where | The fight | Why it is there (story) | Reward | Frame / art |
|-|-|-|-|-|-|-|-|
| 1 | 1 | **Mother Hollow** | Secret Cave, the Mutant Den lair | A huge pallid pack-mother: calls her brood in waves of Pallid Prowlers, you break the brood or hunt her between waves; she pounces from the dark | the dogs are the Zone's first sign of the "pulse"; her pups sit facing north | a pack-leader trophy (perk: Prowlers hesitate), Tier 1 hoard | new (brood waves reuse the Elder's wave code); C (Pallid Prowler sprite scaled + scarring, cheap) |
| 2 | 1 | **The Hook** | Smugglers' Tunnel / Smuggler's Run | A human smuggler lord who fights from traps: tripwire grenades, a hook-and-chain pull, ambush crew; a parry-duel at the end | he has been running people (the "taken") for the Garrison | his ledger (a clue paper), a chain hook melee weapon | frame (Visor/Spool style duel) + traps new; H |
| 3 | 2 | **The Pitman** | Crusher Pit, Ram Hall | The last crew member, a Husk, runs the rams on their 11-second beat: the arena floor is the hazard (dodge the slams, hit him between), then the pay vault opens | the foreman's logbook; the beat is the pulse | Strongroom Wages + a unique heavy tool/club | new (ram hazards reuse the Eel's dodge lanes); H (Husk look) |
| 4 | 3 | **The Ferryman** | Stranded Ferry (Car Deck / Captain's Bridge), the "lake that is not there" | An anomaly captain who rows the deck through a phantom lake: the deck tilts between phases, drowned crew pull at you from the rails | the ferry carried the first taken north; the lake is a leash echo | a ferry key + water-walking charm (perk) | new (tilt = arena states); C (drowned figure, hand-built or Pixel Lab) |
| 5 | 4 | **The Specimen, grown** (rematch) | Deep Caverns, Crystal Pool island | It escaped Lab Alpha (`brain_escaped`) and fed on the pool: mobile, three tendril arms, mind-lashes; smarter than round 1 (it remembers how you beat it) | its escape flag is already set | Specimen tissue (crafting/perk), Deep Caverns cache | reuse BrainBoss moves + new mobility; C (new sprites, large) |
| 6 | 5 | **The Mind Weaver** (the big SpriteCook one, 163x294) | Lattice Foundry, Synthesis Level, the round Coil Hall | A dodge boss in a ring arena: thread lattices as traps, lashes you must cut or dodge, takes your aim for a moment, calls small Mind Weavers (the hand-built 68x105 ones, 3 colourways) as adds | the amplifier the Annex log wanted moved north | psi perk, Coil Hall cache | new (lattice traps); C (art exists) |
| 7 | 5 | **The Array Commander** | Antenna Grid, Control Bunker | A Garrison commander with a riot shield and drones; a 3-minute broadcast timer: kill him before it ends or every mutant on the map hears it | story outline Tier 5 | the Commander's shield (item), broadcast key | frame + timer new; H |
| 8 | 5 | **The Excavator** | Quarry Basin, Pit Floor (Overturned Excavator, Crane Rig) | A husk-driven mining rig: a vehicle boss that sweeps the pit; you board its arm or shoot its hydraulics | the Basin is where the Obelisk dug; the Husk driver is "dead inside" | rig parts (a vehicle-grade weapon mod) | new (vehicle); C (large machine sprite, hand-built) |
| 9 | 6 | **The Marshal** | Siege Ward, Garrison Armory / Hospital | A Garrison marshal in the hospital: three phases (rifle line, grenadier support, melee duel), the portal computer behind him | outline Tier 6 (the portal, the leash reveal) | the portal key choice (shoot the computer, or push through) | frame; H |
| 10 | 6 | **The Choir** | Abandoned City, Bell Tower / Clock Tower | A crowd of Husks ringing the bell: you climb the tower while waves answer each toll; the boss is the bell | the Husks ring on the 11-second beat | the bell (sets a story flag), a cache | new (tower climb); H/C (Husks exist) |
| 11 | 6 | **The Orderly** | Hospital Morgue | A silent giant in a surgical apron wearing a hospital bed rail as a weapon, steam and bone saws, a parry duel | the morgue stairs lead to the Mind's first voice | morgue key | frame; C (big human-mutant) |
| 12 | 7 | **The Calm Voice** | Reactor Core, the lift to the bottom | The leader of "those": a robed duellist who speaks through the fight; the player can end it early by listening (an ending branch) | "those" in the outline | ending flag | frame; H (robe sprite already a placeholder) |
| 13 | 7 | **The Mind** | Reactor Core, the bottom | The last boss: a living arena (the walls, the husks, the pulse on the 11 s beat all fight you), three ending branches (cut / take / listen) | the whole thread | the ending | new; big art |

13 new fights + the Specimen rematch = 14 slots counting #5 (your budget: T1 1-2 (2), T2 1 (1), T3 1 (1), T4 rematch (1), T5 Mind Weaver + 2 (3), T6-7 4-5 (5) = 13). The numbers add up to 13.

## Order I would build in (cheapest, most reuse first)
1. Pitman (T2) and the Hook (T1): reuse the Eel/Visor frames, human or Husk art only.
2. Mother Hollow (T1): scaled Prowler art + the Elder's wave code.
3. Array Commander (T5) and the Marshal (T6): human duels with timers, no new creature art.
4. The Mind Weaver (T5): the arena is built, the art exists.
5. The Ferryman, the Specimen rematch, the Excavator, the Choir, the Orderly: new art each, so spread them out as art credits allow.
6. The Calm Voice and the Mind last, with the ending.

## Open questions
- Does a boss have to be unique per dungeon? (Yan, 2026-11-19: no; so T1/T2/T3 slots could be story squads instead.)
- Mind Weaver boss: the small hand-built version is the regular Tier 3 mutant; is the big one a boss only here, or also reachable earlier as a rare spawn?
- Art: the creature bosses (Ferryman, Specimen grown, Excavator, Orderly, the Mind) need real sprites; with no SpriteCook credits, are hand-built sprites acceptable for now?
