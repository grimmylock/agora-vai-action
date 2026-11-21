# Unity checklist: the new bosses (2026-11-21)
Everything below was built and checked in the offline harness only; **none of it has been run in Unity.** Main menu > Testing Ground > Humans tab: each boss has a "BOSS ..." entry that drops it a few tiles in front of you.
Watch the Console for red first. Then, per boss:

## Everyone
- [ ] It is drawn from its clips (not a plain human body with no weapon): idle when still, walk when it moves, the move's clip on each attack, the hurt clip when you hit it between moves, the death clip at the break (held).
- [ ] Size: Mother Hollow about 4 tiles long, the Singer an 8 x 6 tile wall, the Ferryman about 3.5 tiles tall, the Specimen about 4 wide, the Excavator about 8 wide, the Orderly about 3.5 tall. Too big, too small?
- [ ] It turns toward you (Mother Hollow, the Excavator face left in their strips and flip; the Singer, the Ferryman, the Specimen, the Orderly face the camera and never flip).
- [ ] The glint: white = parry, red = dive or step out. Does each attack's hit land on the frame where it visibly hits? (Hit frames were picked by eye from the strips: `HIT` in `ArtSource/export_boss_clips.py`.)
- [ ] The boss bar, PHASE 2 at half health, the break (E to execute), the trophy in its body.
- [ ] Its own sounds (procedural, `ArtSource/audio/sfx_bosses.py`): too loud, too quiet, wrong?
- [ ] Shooting it: rounds now hit its drawn silhouette (`HitProfiles` "boss:<key>", F6 shows the shape), but only its lower part (rounds fly at most one body-tile above the feet, as for every unit).
- [ ] Walking: it still blocks only its own tile, so you can walk into the Excavator's tracks or the Singer's wall picture. Needs multi-tile blocking if it looks wrong.

## Mother Hollow (Secret Cave, Mutant Den, after the culvert story)
- [ ] Bite (white, parry), Pounce (red, dive under the leap), Spit (red: it lands where you stood when she wound up: walk out), Howl (the brood: on the range they crawl in from 10 tiles round her; in the cave out of the three Dark Niches).
- [ ] Phase 2: the Drag (a 9-tile lunge), the Double Spit.

## The Singer (Valley Mine, Miners' Rest)
- [ ] It never moves. The Note: get behind cover (a wall, a crate) and it passes you; in the open it knocks you down and deafens you. The Grasp hauls you toward it (dive). The Crush at the wall (parry).
- [ ] The painted wall rectangle: do its edges look wrong against the mine's own walls? Does it cover the floor badly?

## The Ferryman (Stranded Ferry, Car Deck)
- [ ] The Flare: keep your crosshair OFF him when it glints and nothing happens; look at him and you are dazzled. Is that readable?
- [ ] The Hands (dive), the Hook (dive), the Thrust (parry; its clip is weak, a re-roll is queued), phase 2 the Row.

## The Specimen, grown (Deep Caverns, Crystal Pool; only if it escaped the lab)
- [ ] Lash, Double Lash (parry), Skitter (dive), the Mind-Lash (break its line of sight), phase 2 the Scream and the Frenzy.

## The Excavator (Quarry Basin, Pit Floor)
- [ ] The Boom Sweep and the Double Sweep (red, wide), the Dig-Slam on where you stood, the Scoop's three rock spots, the Track Charge. Is anything telegraphed on the floor? (No floor markers are drawn yet: only the glint and the clip.)

## The Orderly (Hospital Morgue, Autopsy Theatre)
- [ ] The Drip: hurt him while he squeezes the bag (3% of his health) and it bursts: no heal, he bleeds. Readable?
- [ ] Drag-Saw (parry), Overhead (dive), Grab (dive), phase 2 the Double Saw and the Saw Rush.

## The Verger (cathedral job, or Testing Ground)
- [ ] The scythe poses (reap, sweep, rising, hook, danse, ankle reap, backhand, impale) read as different swings? Phase 2: the hat comes off.

## Co-op (a friend joined)
- [ ] The friend sees the boss as itself (its clips) and its bar; the host runs the fight.
