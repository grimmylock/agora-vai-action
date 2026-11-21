# Boss concepts, third pass (2026-11-21)
Yan, 2026-11-21: "when you're done with [the Verger] redo the other bosses concepts", then "i got a pixel-lab signature, we have 2k credits now, use them to create the new bosses after you finish the concepts, just dont waste all, 50 credits for each boss max".
Yan's earlier notes these build on: the hand-built concepts "didn't have much care put into it, they look bad"; the Gaunt is the bar ("way more detailed and a good reference"); direction for creature bosses is Silent Hill / Resident Evil / Bloodborne (`BOSS_ROSTER.md`, `BOSS_DESIGNS.md`).

**How these were made.** Pixel Lab `create-image-pixflux` (`ArtSource/pixellab_pixflux.py`), not hand-built pxart and not bitforge (bitforge at 160 px drew a two-headed blob; pixflux draws at game quality). Mother Hollow was anchored on the real Pallid Prowler sprite (init image, strength 140) so she reads as one of them. Candidates in `ArtSource/mutants_anim/boss_concepts/v3/`, sheet `boss_concepts_v3_sheet.png`. These are **concepts**: the stand pose each boss's animations will start from, pending Yan's pick.

**Picks (mine, pending Yan):** Mother Hollow `mother_hollow_23`, the Singer `singer_1` (painted cave included: it is a wall), the Ferryman `ferryman_1`, the Specimen `specimen_1`, the Excavator `excavator_74_cut`, the Orderly `orderly_54` (alt `orderly_53`, the faceless one). What the generator would not do: Mother Hollow's belly sacs, the Singer's vertical mouth and no eyes (it drew a skull face with sockets), the Ferryman's drowned face inside the glass, the Orderly's saw-toothed bed rail (it drew a bloody cleaver).

**Budget** (Pixel Lab generations; Yan: max 50 a boss). Kept in the table at the end; every call is counted.

Every fight below is built on what exists: `Boss.cs` (glints: white = parry, red = dive/get out; phases; break + execution), the Eel's authored-move dodge frame (`EelSump.cs`: telegraphs on the floor, hazard zones, a weak point), the Elder Gaunt's waves (`Tier2Story`), the Preacher's pulse rings, and the Pixel Lab clip pipeline the Gaunt uses (stand -> `animate-pixminimax` clips, timing table, `last_frame = first_frame` for loops).

---

## 1. Mother Hollow (Tier 1, Secret Cave: the Mutant Den lair)
**Look.** The Pallid Prowler grown to horse size: the same low hunched bone-grey body, the bony hump, thin bent spider legs, the eyeless skull with the acid-green maw dripping. Ribbed neck, wrinkled hairless skin. One accent: the Prowler's green. (Concept `mother_hollow_23.png`. The brood sacs from the old brief would not generate cleanly: four tries; the brood now comes from the den walls instead, which also reads better in the dark.)
**The fight: the den mother.** A dark cave. She does not fight alone and does not stay in the light.
- *Prowl*: circles at the edge of your light, only her green maw visible (Vision.Light already hides what's outside the light).
- *Pounce* (red): a long leap at you from the dark; a dive goes under. *Bite-and-shake* (white, parry it): if it lands she holds and shakes you (a short grab, like the Drowner's jaw-hold, mash to break).
- *Call the brood*: she howls and 3-5 Pallid Prowlers crawl out of side tunnels (the Elder Gaunt wave code, the den's tunnels as the spawn points, so nothing appears next to the player). Kill the brood or hunt her while they come.
- *Phase 2 (half)*: she starts *dragging* (a sweeping lunge across the cave, red) and spits acid puddles that stay (the Rot Crawler's spit reused, larger).
- Weak point: none, a straight brawl; reward stays the pack-leader trophy.
**Animations (Pixel Lab, from the stand):** idle breathing (loop), prowl walk (loop), pounce, bite-and-shake, howl (calls the brood), drag lunge, spit, hurt, death. ~9 clips.

## 2. The Singer (Tier 2, Valley Mine: the warm wall in the Main Gallery)
**Look.** A wall of mine rock and pale swollen flesh that bulges into one huge eyeless face; a vertical mouth from forehead to chin with teeth down both lips; miners' arms pushing out of the stone; two helmet lamps still burning amber in the flesh; timber props either side. Static: 8 x 6 tiles of wall.
**The fight: a wall you hear before you see.** It never moves; the gallery is the arena.
- *The note* (on the 11 s beat): the mouth opens and a sound wave rolls down the gallery as a visible ring (the Preacher's pulse code). Step into a timber alcove (cover) to break line, or be deafened and knocked down.
- *Grasp*: arms shoot out of the side walls where cracks glow amber (floor telegraph, red); caught = held and pulled toward the mouth until you shoot or cut the arm.
- *The lamps are its eyes*: shoot a lamp and that side goes blind for 10 s (its grasp misses on that side). Both lamps out = phase 2.
- *Phase 2*: the face tears open to the chest; it screams (a cone straight down the gallery, red), and spits miners' bones (a spray, white = parry the bigger ones back). The open chest is the weak point.
**Animations:** idle breathe (loop: the flesh heaves, the arms curl in time), the note (mouth opens), grasp (an arm out of the rock, a separate small clip), lamp blinded, tear-open (phase change), scream, spit, death (the face slackens, the rock cracks). ~8 clips (the wall is mostly static, so cheap).

## 3. The Ferryman (Tier 3, Stranded Ferry: the Car Deck)
**Look.** Tall drowned figure in a torn black oilskin coat bound with wet rope; a brass ship's lantern for a head with a pale drowned face pressed against the green glass from inside; a lifebuoy on his hip; a long boat hook. Water always running off him. One accent: sickly lantern green.
**The fight: the phantom lake.** The car deck floods with water that is not there (a visual layer + slow zones).
- *Boat hook* (white, parry): long thrusts; *hook and pull* (red, the Verger's drag code): catches you and pulls you into the water.
- *Lantern flare*: the face in the glass opens its eyes and the lantern flashes; anyone looking at it (aim within the cone of him) is blinded 2 s; look away (aim off him) to avoid it. A new tell in the game's language: "don't look".
- *Drowned crew*: hands come up out of the deck water and hold your legs (slow, then root); shoot them or keep moving.
- *Phase 2 (half)*: the deck tilts (the arena slides everything a few tiles toward the low rail; cars roll as hazards, the Props tip code); he rows the deck: sweeps of the hook across the whole width (red, dive).
- Weak point: the lantern; break the glass in phase 2 and the face comes out (an execution like the Visor's).
**Animations:** idle (water dripping, lantern sway; loop), walk (loop), thrust, hook-and-pull, lantern flare, the row sweep, hurt, death (he folds over and the lantern goes out). ~8 clips.

## 4. The Orderly (Tier 6, Hospital Morgue)
**Look.** A hunched giant in a blood-stained surgical apron and rubber boots; one arm too long, dragging a bed rail with saw teeth; head wrapped in gauze with a grin stitched on, no face; an IV stand fused into his spine with a bag of dark fluid. Accent: arterial red on dirty white.
**The fight: a heavy parry duel in the dark morgue** (the Boss.cs duel frame, the Spool/King weight class, but huge).
- *Drag-saw* (white): the rail dragged across the floor at you, sparks; *overhead rail* (red): slammed down; *grab-and-slab*: grabs you and slams you onto a morgue slab (red, dive).
- *The drip*: when hurt he squeezes the IV bag and heals a sliver while it drains; shoot the bag (it's a visible target on his back) and the healing stops for good and he bleeds instead. Gives the player a tool, not a nerf (rule 6).
- *Lights out*: he pulls the morgue lights (the dark lighting from the cave); his saw sparks show where he is.
- *Phase 2*: the long arm unhooks the bed rail into two halves: faster double sweeps.
**Animations:** idle (breathing, gauze moving; loop), drag walk (loop), drag-saw, overhead, grab, squeeze the drip, pull the lights (reuse overhead), phase 2 double sweep, hurt, death (kneels, the IV bag swings). ~9 clips.

## 5. The Specimen, grown (Tier 4, Deep Caverns: the Crystal Pool island) - the rematch
**Look.** The brain from the Lab Alpha vat, now the size of a bed, walking on a spray of long spinal-cord legs; nerve tendrils with small human mouths; one eye in the wrong place; the torn lab collar and tag still round one stalk. Wet pink and grey.
**The fight: it remembers you.** The Brain boss's moves (BrainBoss) plus legs.
- *It remembers*: it reads how you beat it in round 1 (the lab fight's stats: which move you used most) and opens with the counter (e.g. if you shot it from range, it starts by closing in; if you meleed, it keeps away and lashes).
- *Stalk lashes* (white/red mixed combos), *mind-lash* (your aim drifts toward the pool for 2 s), *skitter* (fast repositioning across the island, its legs clatter), *mouths*: the tendril mouths scream together (the Howl code: a cone, deafened).
- *The pool*: it drinks from the crystal pool to heal; break the crystals round the pool (4 targets) to stop it.
- *Phase 2*: legs torn, it drags itself and calls the Husks on the shore (puppets).
**Animations:** idle (pulsing, legs shifting; loop), skitter walk (loop), lash, double lash, mind-lash (the eye glows), scream, drink, hurt, death (legs fold, it sinks). ~9 clips.

## 6. The Excavator (Tier 5, Quarry Basin: the Pit Floor)
**Look.** A wrecked rust-orange mining excavator on tracks; a dented cab with fogged wet glass and a Husk driver with his hands on it; the boom raised with a toothed bucket; hanging hoses and cables, a hard hat hung on the boom. No flesh (Yan: a husk-driven rig).
**The fight: a vehicle boss on the Eel's dodge frame.** Authored moves with floor telegraphs.
- *Boom sweep* (red, a long arc across the pit floor: dive or get outside it), *dig-slam* (the bucket comes down on a marked circle), *scoop-and-throw* (scoops rubble and throws it at you: rocks land on marked spots), *track charge* (it drives straight down a lane: get out of the lane).
- *Weak points*: the hydraulic hoses on the boom (3, shootable: each cut slows the boom), then the cab glass (phase 2: the driver is exposed).
- *Phase 2*: the cab glass breaks and the Husk driver can be shot; the rig goes berserk (faster sweeps, the boom drags along the ground).
**Animations:** idle (engine shudder, exhaust; loop), drive (loop: tracks turn), boom sweep, dig-slam, scoop, throw, hydraulic burst (a hose cut), glass break, death (the boom sags, steam). ~9 clips; a machine animates more reliably than a creature.

---

## Generations used (Pixel Lab, of 2000; max 50 a boss)
| Boss | Concept stills | Animations | Total |
|-|-|-|-|
| Mother Hollow | 8 (1 bitforge test, 1 pixflux test, 4 pixflux with the Prowler as init, 1 inpaint try, 1 init with sacs painted in: the model would not draw belly sacs) | 8 (idle, walk, pounce, bite, howl, spit, hurt, death; all first try) | 16 |
| The Singer | 2 | 7 (idle, note, scream, grasp, hurt, death; the scream re-rolled: the first barely moved) | 9 |
| The Ferryman | 2 | 9 (idle, walk, thrust, hook, flare, sweep, hurt, death; the thrust re-rolled: the first only tilted the pole) | 11 |
| The Orderly | 4 (the first two drew a plain big man with an IV stand; the re-roll asked for the gauze-wrapped faceless head, the stitched grin, the long arm) | 8 (idle, walk, drag (sparks along the floor), overhead, grab, drip, hurt, death) | 12 |
| The Specimen, grown | 2 | 9 (idle, walk, lash, lash2, scream, mind, hurt, death; the scream re-rolled) | 11 |
| The Excavator | 4 (the first two: a clean toy digger with fake lettering on a white ground; the re-roll: derelict, at night, no text; its painted background cut out by flood fill) | 7 (idle, drive, sweep (the turntable spins), slam, scoop (16 frames), hurt (oil spray), death (smoke)) | 11 |

## Mother Hollow: BUILT (2026-11-21)
`MotherHollow.cs` (the fight, the brood, the acid, her spoils), `BossArt.cs` (the creature-boss drawing, generic for all six), `export_boss_clips.py` (strips + the clip table). Testing Ground > Humans tab > "BOSS Mother Hollow". Harness `MotherHollowChecks`. Preview: `maps/renders/bosses_v3/mother_hollow_in_den.gif` (her clips at game scale in the Secret Cave render, next to a player).
- Moves: Bite (white, parry), Pounce (red, 6-tile leap), Howl (the brood: 3 Pallid Prowlers out of the cave's three Dark Niches, 5 in phase 2, never more than 6 alive; on the range a ring 10 tiles round her), Spit (red: lands where you stood when she wound up: walk out or dive; Toxic Bleed). Phase 2: Drag (red, 9-tile lunge), Double Spit.
- When: in the Secret Cave once the Brute's part of the culvert story is over (culvert step 4+, no Brute rematch pending), when a player comes within 10 tiles of the Mutant Den; once (`mother_hollow_done`). Spoils: Pack-Mother's Skull (1800 Ru). Her bite: Mother's Jaws (220, Tier 1), never loot.
- Drawn 0.75x (about 4 tiles long, 2 tall), turns toward the nearest player; the hurt clip plays when a hit lands between moves; the death clip plays at the break and holds.
- Hit frames: picked from the silhouette's furthest reach, then bite/spit/pounce corrected by eye (`HIT` in the exporter). Timings are mine (wind-up slows into the hit).
- NOT done: no sound of her own (the Gaunt's scream for the howl, a splash for the acid); the brood sacs; co-op sync; never run in Unity.

## The Singer: BUILT (2026-11-21)
`SingerBoss.cs`; Testing Ground > "BOSS The Singer"; harness `SingerChecks`; preview `maps/renders/bosses_v3/singer_in_mine.gif` (its clips at game scale over the Valley Mine render, at the Miners' Rest).
- Rooted (`Boss.Def.Rooted`: never walks, never steps into a blow). Moves: Note (red: everyone within 14 tiles who can see it is hit, deafened 1.5 s and knocked down; cover breaks the line; a dive goes under), Grasp (red: a hand hauls the nearest player within 8 tiles 4 tiles toward the wall), Crush (white, at the wall: parry it); phase 2 the Scream (the Howl's cone) and the Chorus (two notes).
- When: the Valley Mine, a player within 12 tiles of the Miners' Rest; once (`singer_done`). Spoils: the Singing Stone (2400 Ru). Its hands: The Singer's Hands (240, Tier 2), never loot.
- Honest state: the art is the painted concept (the cave round the face included), so it is a rectangle 8 x 6 tiles stood at the head of the gallery: its edges show and it covers the top of the Miners' Rest floor. The idle clip has the lamps flare in its middle frames (reads as a pulse). The lamps as weak points and the face tearing open (the brief's phase 2) are NOT built. Never run in Unity.

## The Ferryman, the Specimen grown, the Excavator, the Orderly: BUILT in code (2026-11-21)
Each has its file, a Testing Ground entry (Humans tab, "BOSS ...") and harness checks (`FerrymanChecks`, `LateBossChecks`). Their clips come from the same Pixel Lab plan (`bosses_v3/plan.json`) and are exported by `export_boss_clips.py` as they finish; until a boss's clips are in, `BossArt.Drawn` is false and it shows as a plain body.
- **The Ferryman** (`FerrymanBoss.cs`, Stranded Ferry, Car Deck, 6000 HP): Thrust (white), Hook (red, catch and drag 3.5), Flare (dazzles whoever's crosshair is on him: "don't look"; a dive does not help), Hands (red, knocked down within 6 tiles); phase 2 Row (red sweep), Double Thrust. Spoils: Ferry Lantern. NOT built: the phantom lake, the tilting deck and rolling cars, the lantern as a weak point.
- **The Specimen, grown** (`SpecimenGrown.cs`, Deep Caverns, Crystal Pool, only if `brain_escaped`, 8000 HP): Lash, Double Lash (white), Skitter (red rush), Mind-Lash (aim torn away and dazed in its sight; break its line of sight); phase 2 Scream (cone), Frenzy (3 lashes, last red). Spoils: Specimen Tissue. NOT built: "it remembers", drinking from the pool and the crystals, the Husk puppets.
- **The Excavator** (`ExcavatorBoss.cs`, Quarry Basin, Pit Floor, 11000 HP): Boom Sweep (red, wide), Dig-Slam (red, a 2-tile circle where you stood), Scoop (red, rubble on three spots), Track Charge (red, 8 tiles); phase 2 Double Sweep. Spoils: the control lever. NOT built: the hoses and cab glass as weak points.
- **The Orderly** (`OrderlyBoss.cs`, Hospital Morgue, Autopsy Theatre, 12000 HP): Drag-Saw (white), Overhead (red), Grab (red: slammed down and pulled in), the Drip (mends 6% unless you hurt him by 3% while he squeezes: then the bag bursts and he bleeds; 3 a fight, phase 1 only); phase 2 Double Saw, Saw Rush. Spoils: the Ward Name Tag. NOT built: the lights going out, the bag as its own target.
- All health, damage, ranges and timings are guesses; none of it has been run in Unity.

## A rough fight probe (2026-11-21): `dotnet /tmp/test.dll bossprobe [all|name] [runs]`
A bot with an assault rifle: parries every white blow on the perfect beat (misses one in four), dives every red one, shoots all the time (rounds glance off unless the boss is open or staggered), drinks a medkit under 35 %, gets up when downed. 2 runs each, the real Boss/Rt/Melee code at 60 frames a second:

| Boss | Broken | Time | Hits taken | Medkits | Downed |
|-|-|-|-|-|-|
| The Visor (baseline Yan has played) | 2/2 | 53 s | 44 | 18.5 | 9 |
| Spool | 2/2 | 39 s | 12 | 3 | 1.5 |
| The Scrap King | 2/2 | 48 s | 14 | 9.5 | 6 |
| The Verger (scythe, this batch) | 2/2 | 71 s | 43 | 25 | 13.5 |
| Mother Hollow | 2/2 | 25 s | 9 | 2 | 1 |
| The Singer | 2/2 | 35 s | 0 | 0 | 0 |
| The Ferryman | 2/2 | 35 s | 5 | 1 | 1 |
| The Specimen, grown | 2/2 | 45 s | 24 | 13 | 6.5 |
| The Excavator | 2/2 | 54 s | 6 | 3 | 1.5 |
| The Orderly | 2/2 | 67 s | 20 | 7 | 3 |

(Second run of the probe: the bot now dives to the side, alternating, instead of away; the first version drifted out of the Singer's reach and stalled.)
How to read it: the bot dives every red move perfectly, which no person does, so the bosses built mostly of red moves (Mother Hollow, the Singer, the Ferryman) look far easier here than they will be. The probe also shoots Mother Hollow's brood off-screen. The Verger is the hardest of all to this bot (54 hits taken): his whites come fast and in long strings. Numbers were NOT changed from this (rule 6: harder is allowed, easier is not; Yan's first run decides).

**Total so far: about 70 of the 2000 generations (Pixel Lab balance read 1940 mid-run); every boss is under 20 of its 50.** The old run of each re-rolled clip is kept as `<clip>_v1` (not exported).

## Yan's review (2026-11-21) and what changed
Yan: "Mother Hollow fits and it's well animated." The rest did not land:
- **The Singer**: "the artstyle is completely different and it being drawn as a painted rectangle will not fit the game, also it's drawn in a front-facing perspective and our walls are in 3/4". Option A chosen: a face built into our 3/4 mine wall. Work in progress (the first pixflux/bitforge tries from our wall tiles did not work: one gave a stone dome with lamps, no face).
- **The Specimen**: "doesn't read as the Brain escaped ... doesn't have any of the aspects the brain had like the big eye tentacle or the laser attacks" -> Yan: "reanimate the brain in the vat boss now that we have credits, so reanimate him first THEN continue animating it outside the vat". DONE: the vat sheet re-animated (11 generations, `ArtSource/bosses/vat_reanimate.py`, only inside the glass); the Specimen v2 drawn from the vat brain's own art (the brain cut from the sheet, grown by Pixel Lab at a low init weight, the giant eye behind it; 4 stills + 7 clips) and refought with the vat's attacks: the Laser from the giant eye (dive, walls stop it; phase 2 the Sweep), the vat's orbs and bolt, the Mind-Lash, the Drift; it keeps its distance (`Boss.Def.KeepAt`). The generator's baked magenta beam is keyed out (the game draws the beam with the vat's beam strips, `CombatFxView.SpecimenBeams`) and its magenta flashes are mapped to the vat's violet. The first Specimen's clips are in `bosses_v3/_scrapped/`.
- **The Excavator**: "kinda decent but it has ground drawn below it that doesn't match the artstyle, also the zombie inside is not one of our sprites for husks". DONE without generations: `ArtSource/fix_excavator_clips.py` (the painted ground cleared, the real Husk sprite in the cab, found frame by frame).
- **The Orderly / the Ferryman**: "doesn't really read as a boss" -> Yan: "scrap the ferryman and orderly or make them clearly mutant, no human shaped BIG bosses like the mutants we have today". Proposed: the Ferryman as a drowned anglerfish mutant with a lure (the flare kept), the Orderly as a mantis-like mutant with bone-saw forelimbs and a blood sac (the drip kept). Concepts only until Yan picks.

| Pixel Lab, since the review | |
|-|-|
| The vat re-animated | 11 |
| The Specimen v2 | 4 stills + 7 clips = 11 (the Specimen in all: 33 of 50) |
| The Singer, wall tries | 2 pixflux + 2 bitforge + 1 pixflux (1 lost to a dropped connection) = 5 (the Singer in all: 14) |

## Second review (2026-11-21) and status
Yan: "Ferryman as an anglerfish in the water that lures people and comes out to attack, has legs. OK / Orderly as a insect, ok but im not sure on the designs, feels too natural and less freaky mutant / face in the wall, scrap it / the excavator, i think the artstyle itself holds it back / For the monsters, i want more silent hill style/dead space style stuff, looks too tame for now, search for references / the brain outside the vat should be at another lab ... new moves, moves around ... the eye behind him connected through a cord moves around and fires", then "try and make the designs match the games current art-style too, some of them came out TOO detailed".
- References and rules: `HORROR_DESIGN_REFS.md`.
- The Singer: PARKED.
- The Specimen v3: BUILT (Lab Gamma's Signal Room, the eye on a cord as its own target, blink/pulse/pull; see WORK_LOG 9aj).
- The Ferryman / the Orderly: concepts at game size from our sprites (`boss_concepts/v5/`: the style now matches the Pallid Prowler and the Gaunt, but the "made of people" horror does not survive at that size: the model draws a pale beast with a lantern, insects with a bag). At double size and reduced, it went cartoonish (a grinning fish with a skull lantern). Not animated; waiting for a direction (see the reply to Yan).
- The Excavator: a flat-style test from our APC sprite came out as an APC (the init dominates; without it the result is too detailed). Proposal: draw it in code like our vehicles (`ArtSource/world/props_x2.py`), as a rig (body, boom, stick, bucket) animated by turning its joints.

| Pixel Lab since the second review | |
|-|-|
| The Specimen's eye | 1 still + 3 clips |
| The Ferryman v5 | 2 at game size + 2 at double size |
| The Orderly v5 | 2 |
| The Excavator flat test | 1 |
