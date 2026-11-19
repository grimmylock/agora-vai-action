# Tier 3, beat 4: "The Door Under the Dock" (design, 2026-11-19, Yan's idea; NOT built)

Yan, 2026-11-19: the keycard door in the Taken scene opens when you stand close but there is nothing behind it. Idea: the story sends the players back to the caverns (now
infested), the horde scene replays at the door but this time the players are prepared, the Elder Gaunt is a boss in the cavern at the far end (the cave gets bigger), it drops the
real keycard and turns out to be one of "those" (an early experiment in controlling mutants), the door leads down a short corridor to a loading zone and a smaller Tier 4 lab with
loot bags, mutants and story. Yan's answers to my questions: the boss fight with three phases is fine; the lab's story is told Resident Evil style (documents and files, one big
discovery); the Gaunt's notes are the reason to go into the lab.

## Why the players go back (the quest)

After the Sub-Station Reveal (flag `tier3_reveal`) Osei knows the counter-signal needs two things: the old array's power (Tier 5) and **the calibration of the leash** - how the
control signal is shaped, so that it can be cut instead of copied. The only living example is the Elder Gaunt: whatever answers its call is being driven by something. Her
spike readings under the lake ("a heartbeat") match the call's rhythm. She asks the players to bring back **the research files the Garrison sealed behind the lab door under the Loading
Dock** (the door they were dragged to in "Taken" and could not open: the card was a fake, the Garrison never had the real one).
Pavel and Hale warn them: last time they took your guns and shoved you in front of that door; this time bring everything. Osei gives a supply case (grenades, armor-piercing rounds,
stims) at the Sub-Station House - the "prepare well" the players are told about.
Step name `annex`: 1 asked (journal), 2 at the hatch / in the tunnels, 3 the door fight, 4 the Elder, 5 the Annex, 6 files handed in to Osei (done).

## The beats

1. **The tunnels, infested.** The same map (`taken_tunnels`), no soldiers, no clock. Mutants of the tier (pack dogs, a few Blind Maws, rat swarms, one Dread Prowler) nest in the rooms
   along the way to the corridor; it is a fight through, not a stealth walk. Ammunition and medicine caches in two rooms (the Garrison's old dead-drop).
2. **The door.** E at the lab door (the same place as in "Taken"): the reader buzzes, the alarm goes, the corridor lights go red. The Elder Gaunt walks into the red light at the broken
   wall and screams (the same scene, the same camera, shorter): the horde pours out - **but the players have guns** and a prepared position (sandbag line, two fixed lamps, a medical cache
   in the corridor's west end). Waves as in Taken (about 55 mutants, four waves close together) but sized for a squad with Tier 3 gear. The Elder does not fight here: after the call it
   withdraws into the dark.
3. **The cavern.** The tunnel beyond the broken wall opens on the nest: enlarged into an arena about 40x30 tiles (the plan generator gets a bigger nest room; the corridor, the door,
   the broken wall and the hatch keep their landmarks so the Taken scene is untouched). Cover: broken stone pillars and a collapsed ceiling slab (cover that the Gaunt's scream can shatter).
4. **The boss: the Elder Gaunt** (15,000 HP, the existing `Gaunt.ElderDef`; HP is not touched). Three phases:
   - 100-75 %: a long reach (it lunges and swipes at range 2-3, slower than you, you can parry-dodge with the dive), and the **scream**: a cone that staggers and drops your aim
     (Quick-Hands and Tumbler shorten it; the ear-ring artifact idea is left out for now).
   - at 75 %: it points - a horde wave (a third of the Taken horde) comes out of the side tunnels; it stays untouchable for a few seconds behind the wave (rock fall seals it).
   - 75-40 %: faster, the scream shatters cover pillars.
   - at 40 %: the second, bigger wave; it enrages (shorter scream cooldown, a ground-slam ring).
   - 40-0 %: it stops calling and hunts. At 0 it kneels, the call cuts off for good - **every mutant on the map drops to the ground for a moment** (the horde is driven by it), then
     scatters. It drops the **Annex keycard** and a **dog-tag-like plate** stamped "G-1" (one of "those": an early control subject).
5. **The Annex.** The door opens with the card (`LabLocks`: a per-dungeon key, `annex_card`). A short corridor (a decontamination gate, a lit sign), a loading zone (a freight lift,
   crates, a forklift: you ride it up/down = the zone change), and **the Annex**: a smaller lab (about 60x40) of five rooms - intake, cell block, the Handler's office, the control
   room (the old transmitter, still warm), the vault (the research files; a code from the documents opens it). Tier 4 loot bags (`Stashes`: 5-6 of them), Tier 4 mutants that escaped
   containment (a few Veil/Dread Prowlers, two small Gaunts: "the others of its pack"), and no boss.
6. **Resident Evil style story** (documents, `Examine`-style readable items, journal entries; one main discovery):
   - Intake log: "G-1, adult male, volunteer" - the Gaunt was a person; photo-less ID card; his name is on the cell-block door.
   - The Handler's diary (four entries, the voice of a Garrison liaison working for "those"): the subject's vocal cords were reworked; the call is a *carrier* that the others can hear
     and answer; "the first one to answer was the dog we named Bell".
   - Calibration table (the quest item) - the shape of the signal, with a margin note "it cannot be cut, only drowned" (this is what Osei needs: the counter-signal drowns it).
   - Transmitter log: the first test of the **Preacher's sphere** ("a better amplifier than a throat") - sets up Tier 4 (the Barrier's Preacher).
   - The vault code: two documents give half each (a locker note and the diary), the vault holds the files and a Tier 4 reward.
   - The main discovery: the Gaunt was the *prototype* of the Preacher; "those" learned to build the leash with a person, then replaced the person with a device.
7. **Hand-in**: Osei at the Sub-Station House: the calibration is what the counter-signal needed (flag `annex_files`, `gaunt_was_those`); +1500 Ru; the Garrison thread: the sealed pay
   note from Crusher Row ("`king_garrison_backer`") connects to the Handler's name. She tells the players the pulses under the lake change when the Gaunt falls (a hook for Tier 4).

## What already exists to build on

- `Tier2Story.cs`: the door, the alarm, `RedLights`, the Elder's walk, `GauntScream`, `SpawnHorde`, the waves, `Out()`; `Gaunt.cs`: `ElderDef` (15,000 HP), the call and the pose.
- `TierDungeons.TakenPlan.cs` / `ArtSource/plans/taken_tunnels.py` (96x58; the generator picks rooms and tunnels by seed 926): the corridor, 'F' the door's spot, 'B' the broken wall,
  'H' the nest. The door ('1') sits in the corridor's north wall; there is only rock behind it.
- `LabLocks.cs`: per-dungeon keys, locked doors; `Stashes.cs` for loot bags; boss infrastructure (`Boss.cs`, `BrainBoss.cs`, `EelBoss.cs`: phases, bars in `HudView.Boss.cs`).
- The Testing Ground entry pattern (`TakenMission`, `Tier4Story.StartForTest`): a button for the new beat.

## Work plan (four batches, each logged, each with a Testing Ground entry and harness checks)

1. **Map**: the plan generator grows the nest into the arena and adds the door's inside (corridor, loading zone exit); a new dungeon `loading_annex` (floor plan script
   `ArtSource/plans/loading_annex.py`, registered in `ZoneLayoutRecipes`, `LabLocks` key `annex_card`); renders to `maps/renders/` for Yan to look at before anything else is built.
2. **The quest**: Osei's ask, the supply case, Pavel's and Hale's lines, the infested tunnels (mutants placed by the story), the door fight (`Tier3Annex.cs`, steps `annex` 1-3).
3. **The boss**: `GauntBoss.cs` on the Elder (phases, scream, waves, the arena's breakable cover), a boss bar (`HudView.Boss.cs`), the drop; harness probes for the phases like the eel's.
4. **The Annex**: loot, mutants, the documents and the code puzzle, the hand-in, journal lines, `STORY_STATUS.md`, pt-BR table lines for the new text.

## Decisions I made so far (Yan, say if you want them different)

- The new lab is its own dungeon `loading_annex` (not Gamma or Beta: Gamma's vault is what the eel's key opens).
- Osei is the one who asks, because the Reveal already gave her the counter-signal problem.
- Tier of the Annex: loot Tier 4, mutants Tier 4, no boss; the Gaunt is the boss of the beat.
- The scream is a stagger cone, not damage; the horde waves are the damage.

## Risks

- A 15,000 HP boss with 1.15 damage to humans and 1000 HP players needs tuning in Unity: the fight's length is a guess (aim: about 4-6 minutes for a squad with Tier 3 gear).
- Enlarging the nest changes the Taken map: the harness `taken` checks and the blood trail coordinates (`TakenTrail`) must be re-run; the Taken scene itself must not change.
- Co-op: the boss targets the nearest player; the horde waves scale with the number of players.
