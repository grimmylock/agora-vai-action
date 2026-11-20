# Playtest checklist: the Verger and the cathedral job (2026-11-20)
Nothing below has been run in Unity yet. Console first (any red = stop and send it), then play. Main menu > Testing Ground, then the entries:

## A. The Verger's duel (Testing Ground > BOSS The Verger)
1. He appears 6 tiles in front of you with a hat, a long dark coat, a red scarf and a scythe. Is the held scythe readable (blade curving back over the snath, the cutting point mid-blade)? Is the grip where his hands are?
2. His four scythe moves: **Reap** (overhead, white glint: parry it with F), **Sweep** (two wide hits), **Hook** (red glint: dive or step away), **Harvest** (three hits). Is the glint on the blade, is the wind-up long enough to read?
3. **The Hook**: he stops about 2 tiles away; if it cuts you the chain draws from his hands to you (a gaff hook on interlocking links), you are hauled in and lie prone half a second. Check: the chain size (half size, 32 px per unit), the hook's curl stays below the line when he hooks you from the left, the sounds (`hook_throw`, `hook_catch`, `chain_drag`: were they synthesized and never listened to, do they sound right?), whether the drag feels like a pull and not a teleport, whether 3.2 tiles is too long.
4. Hit him down to half health (7000 HP: use the range's armory). **The change**: a name card "THE CHANGE", the camera on him for about 3 s, two lines of dialogue, he stands still. Shoot or cut him: damage should be a tenth. After 4 s he fights on with **claws** (Rake, Pounce, Maul, Frenzy). NOTE: there is no beast sprite yet, he stays in the human body.
5. At the break (0 HP) E next to him: the finisher. His body should hold the Verger's Scythe and the Verger Coat (and no claws). Neither is in any shop (check a trader).
6. Parry him three times in eight seconds: he should stagger and take full damage. Do bullets glance off him when he is not open (they should), and land at a tenth during the change?

## B. The cathedral job (Testing Ground > JOB The cathedral job; a short contract of 8 Husks)
1. You land in the cathedral by the Bell Rope. E at the rope: a notice (read the numbers: 90 counted, 15 Ru a head, 8 in one sitting, 1500 bonus) and the first toll.
2. The toll: your ears give out for 3 s (the concussion effect: is the aim blown, the screen muffled enough?). Do Husks come through the windows and the far doors, never beside you? Are they fast for about 6 s? Does the wave grow each toll (E again, 5 s apart)?
3. Do the pillars and pews work as cover? Do blood and bodies draw the mutants in the aisles and the crypt (the lairs)?
4. Each Husk that falls: +15 Ru. At 8 kills the bell stops by itself; the last Husks fall; the Verger walks in from the altar; the rope is slack; no more Husks.
5. After he falls: 1500 Ru, a Sacristy Cache, and E at the rope says the contract is closed for 4 days. After 4 in-game days it reopens (the city refills 12 Husks a day).
6. Leave the cathedral mid-run and come back: the session restarts, the kills were still paid.
7. Not built / expect problems: the wave's entrance is a sound only (no glass art), nothing is synced for co-op clients, the real contract (40 Husks) is long: try the short one first.

## C. Portuguese
Options > Language: the Verger's name card, his lines, the notice and the job's log lines should be Portuguese (Husks stays Husks).

## What to send back
The Console's red lines first, then anything that felt wrong in A3 (the Hook), A4 (the change), B2 (the toll) and the beast-less transformation.
