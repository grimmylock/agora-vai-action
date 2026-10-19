# Playtest checklist (Batches 72-102)

Pull `tier-maps` in GitHub Desktop, open Unity, wait for the recompile, check the Console for red. Then Play, New Game, pick a
perk, Start. F10 = +1 hour, F11 = next weather, F8 = colour grade on/off (editor only).

Write down anything odd with a screenshot; "looks wrong" is enough.

## 1. First minutes (Tier 1, Bonfire Village)
- [ ] The game starts with no red errors in the Console.
- [ ] Walk around the village (WASD, then click to walk). Houses show worn walls (stains, chips, plaster patches). (98)
- [ ] The red barrels: explosive barrels used to be invisible; they should now be red drums. Shoot one from a distance. (96)
- [ ] Poles along roads have two sagging wires between them. (96)
- [ ] Sandbag/log/barrier runs going north-south look like a wall seen end-on, and corners join. (95, 96)
- [ ] No sandbag positions in empty meadows far from roads and places. (96)

## 2. Sun, weather, time (press F10 a few times, F11 to change weather)
- [ ] In clear weather objects cast soft shadows; morning they fall west, evening east, longer near dusk. (94)
- [ ] The shadows creep every few seconds while exploring instead of jumping. Any stutter when they move? (94)
- [ ] Overcast gives faint shadows; fog/rain give only the small patch under objects. (94)
- [ ] The colour of the hour: cool morning, golden around 18:00, grey overcast. (35/94)

## 3. Ruins (south-east of the village, e.g. the ruin near the bridge)
- [ ] Roofless ruins have broken, uneven brick tops; intact houses don't. The floor is overgrown; no floorboard strip outside. (97)
- [ ] No hedge of bushes running through a ruin. (97)

## 4. Combat basics
- [ ] Start a fight. The action bar along the bottom shows icons, keys, AP; greyed actions say why on hover. (72)
- [ ] K hunker down, O overwatch, L aimed shot (click an enemy: the body-part panel), Tab examine. (72, 77)
- [ ] Y swaps weapons (pistol/blade free, others 1 AP). A sniper rifle only pairs with a pistol or blade. (80)
- [ ] Q cycles ammo kinds; rounds are per gun class and tier. (80)
- [ ] A knife never jams. (92)

## 5. Light and dark (any dungeon, e.g. the flooded bunker, or night: F10 to 22:00)
- [ ] Underground it is dark; your own light reaches about 2-3 tiles, the flashlight cone reaches far. (92)
- [ ] Lamps light pools of warm light; red emergency lamps light red. Enemies standing in light can be seen from the dark. (92)
- [ ] U toggles the flashlight (also on the action bar). Off: you see only lit areas; enemies spot you later in the dark. (92)
- [ ] Save, load: sneaking and the flashlight state are kept. (96)

## 6. Labs and the Foundry (Tier 5, under the Ship Graveyard; or any secret lab)
- [ ] Labs are messy: crooked rooms, holes in walls, rubble, blood trails, mostly dark. (92)
- [ ] Thick walls between rooms show a grey concrete top, not black stripes. (98)
- [ ] The Foundry: guards upstairs, the stair down, the Foreman with the G-7 downstairs; nobody else carries a G-7. (88, 96)

## 7. Movement (102)
- [ ] Click a spot at an angle: you walk a straight line to it, not an L. Corners are rounded, not tile-centre zigzags.
- [ ] In a fight the reach outline includes diagonals; a diagonal step costs 1.5 MP (the MP pips round down).
- [ ] Nobody (you or enemies) squeezes diagonally between two wall corners.
- [ ] WASD diagonals still slide along walls.
- [ ] You stop where you click inside a tile; groups of NPCs don't stand on a perfect grid. Clicking enemies still picks the right one. (114)

- [ ] Hovering a spot draws a dotted path; in a fight it's green while you have the MP and turns red past it. (116)

## 8. Physical props (103) - labs and dungeons have desks, crates and barrels
- [ ] Click a desk next to you: it tips over away from you. Enemies on the far side can't hit you as easily; from behind it's no help.
- [ ] Kick a barrel (click it): it rolls until something stops it; standing in its way knocks you down. Explosive ones don't blow.
- [ ] A grenade shoves crates and barrels just outside the blast.
- [ ] Enemies in the open tip desks over for cover.
- [ ] Enemies next to a red barrel kick it toward you, then shoot it. (107)
- [ ] A tipped desk slides a tile when you shove it again. (107)
- [ ] Shots that hit cover throw splinters / dust / sparks by material; rounds go through wooden crates and tin at half damage, never through sandbags or concrete. (105)
- [ ] Buildings' walls are one storey tall now (doors at person height). (106)

## 9. Things to report on feel
- Is it too dark underground? Is 2.5 tiles of own light too little?
- Do the shadows look right in size and darkness? (`WorldArt.SunStrength`, now 0.85)
- Is the 10-minute sun step smooth enough, or does it stutter on your PC?
- Any building, prop or ruin that looks wrong or makes no sense.

## 10. Menu, dialogue and prompts (123)
- Play: the menu sits on the left over Bonfire Village. Leave it running: dusk comes in ~30 s, night ~40 s, the fire glows
  in the dark; rain usually within 2-3 minutes. The bottom corner shows the day, the clock and the weather.
  Does the shadow re-bake (every ~1 s at this speed) make the menu stutter or the fans spin?
- New Game and Load from the menu still work, and the log shows again once you're in.
- Tier 1 Raider gang: the toll is only asked once their boss stands within 4 tiles, in your sight and on screen.
- The toll and surrender dialogues: a panel at the bottom with their portrait, name, faction and rank, the words typed out
  (click/Space to finish), answers on 1-5 or a click. The camera frames you and the speaker; a yellow chevron bobs over them.
- Quest turn-ins and story cards: same panel without a portrait.
- Chips over things you can use: E (containers, the armory, the synthesizer), F (an anomaly to search), CLICK (a body, an item,
  a desk/barrel/crate you point at next to you), WALK (exits, stairs). Do they get in the way?

## 11. Stashes (124)
- Stashes are footlockers, chests, bags, toolboxes, tarp caches, keypad safes and PDAs, against walls indoors, by camps and wrecks,
  never in the open or in Bonfire Village. Press E next to one: it opens (the sprite stays, open and empty).
- Is the PDA (a small dark handheld with a cyan screen) too small to spot?

## 12. Stashes and checkpoints (126)
- 8-10 stashes a map. A Locked Chest says who has its key (kill that mutant); a Keypad Safe says where its code note is (go read
  it with E). They move every 7 days. Each dungeon has a stash at its far end.
- Tier 1's boardwalk north has a Raider checkpoint (expect the toll); the other ways out of the Perimeter are open.

## 13. Batch 127b/c - fights start on your terms, detection, the "!"
- Click-walk towards a squad: the walk stops when you first see it, with "<Faction> ahead. They haven't seen you..." in the log.
- A white "!" over you while a hostile you can see is within ~14 tiles; the camera eases out to show it.
- Walk into an enemy's view: a yellow "!" over it while it notices you (it stops and turns your way), red when it spots you.
- Back out of its view before it fills: the yellow "!" fades and it goes back to its business. Sneak (N): it notices you much later.
- In a fight: S (Suppress) with any automatic gun; a kill shakes squadmates within 4 tiles (log: SHAKEN, -1 AP).

## 14. Batch 128 - backs, facing, guns, dialogue
- Aim the mouse above you: your character shows its back, the gun and hands go behind the body, the gloves flip.
- NPCs walking up the screen show their backs (walking frames too); unaware ones point their gun where they walk.
- The road toll: the boss and the gang turn to face you when the dialogue opens; you face the boss.
- Round full-face helmets (Granit-9, Citadel...) now have a visor on the front.

## 15. Batch 129 - banter
- Watch a squad from cover (in sight, within ~14 tiles): every 15-30 s two of them trade lines over their heads.
- Walk past a friendly squad (Wanderers at the village): one greets you once.
- Walk into an enemy's view: a yellow "Huh?" while it notices you, a red shout when it spots you.
- Lines readable at your screen size? Too frequent or too rare?
