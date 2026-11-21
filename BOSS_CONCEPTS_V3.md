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
| Mother Hollow | 8 (1 bitforge test, 1 pixflux test, 4 pixflux with the Prowler as init, 1 inpaint try, 1 init with sacs painted in: the model would not draw belly sacs) | | 8 |
| The Singer | 2 | | 2 |
| The Ferryman | 2 | | 2 |
| The Orderly | 4 (the first two drew a plain big man with an IV stand; the re-roll asked for the gauze-wrapped faceless head, the stitched grin, the long arm) | | 4 |
| The Specimen, grown | 2 | | 2 |
| The Excavator | 4 (the first two: a clean toy digger with fake lettering on a white ground; the re-roll: derelict, at night, no text; its painted background cut out by flood fill) | | 4 |
