# agora vai - the story (draft, written with Yan step by step)

Started 2026-10-28. The base is Yan's two campaign files from a Gemini session (an escort of a defector engineer through
faction lands to a science bunker, then a descent into an underground complex; and the "flooded tunnels / split assault / the
cult were the scientists / final choice" beats). We are rewriting it into this world one tier at a time. It replaces the old
"Heart of the Zone" main story (`StorySystem.cs`) once built. **Nothing here is coded yet.** Names in *italics* are
placeholders until Yan picks.

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
   very badly** - measure it with a probe before shipping. Winning opens the road south (the checkpoint or the Runner's tunnel).

Side content alongside: the Yefim arc, the Runner arc (opens the tunnel route), the board jobs (T1-02 to T1-05).

## Names (suggestions, Yan to pick)

- Grey Robes: *the Ashen*, *the Hush*, *the Quiet Choir*, *the Greycloaks*
- Husks: *Husks*, *the Hollowed*, *the Dimmed*, *Emptied*
- The Engineer: *Ilya Vashkevich*, *Pavel Druzhin*, *Anton Lisovsky*

## Needed to build Tier 1 (later)

- Sprites: Grey Robes (16x16, robe over armour), husks (gun, melee, bare hands; pale, bloodied).
- Cutscenes: camera to a target, radio lines on screen, a short loss of control.
- Quests with real steps: talk to the giver, steps in order, hand-in, hidden bonus (the fence), escort.
- Grey Robe rules: never hostile, never killed, smoke and leave when threatened, hits stagger.
- Husk faction: tier-scaled kit, worse aim, slower, no radio; loot always Broken.
- The siege: waves for the village defence, plus a win-rate probe.
- The Blight Brute chase in the culvert (too big for the pipe).

## Tier 2+

Not written yet.
