# Story status: what exists in code (2026-11-18)

Everything below passes the harness (`Tools/compile_check`, `Tier1StoryChecks`, `Tier3StoryChecks`) and is **unplayed in Unity**. The story text is **not translated** (pt-BR) yet. Companions are never required except the three Pavel escorts.

## Tier 1 (Tier1Story.cs, .Pavel.cs, .Defence.cs, Husk.cs, GreyRobes.cs)
| Beat | State |
|-|-|
| First morning, courier's pack, Raider camp (Stas), cut fence, distress loop, Headman's key | built earlier (Story 4) |
| The Engineer: Pavel in the Flooded Bunker, walks home with you | built; if he dies the step resets (no soft lock) |
| The Culvert: canister, the Blight Brute (10,000 HP, slower than you), hoard (Tier 3 bag) | built; chase untuned |
| Reveal at the Medic's House (flag `husks_known`) | built |
| Night defence of Bonfire Village (4 Husk waves, door HP, auto-save reload on loss) | built; win rate NOT measured |
| Husks (unit, AI, sprites) | built; roaming packs in the A-Life after the canister (`Husk.Roam`, up to 4 packs of 4-8, one tier above yours at most) |
| "Those" (robed figures: turn away, smoke when hit, watch Pavel's walk) | built; **placeholder sprite** |

## Tier 2 (Tier2Story.cs, Tier2Jobs.cs, Tier2Escort.cs)
| Beat | State |
|-|-|
| "Taken" (grab, interrogation, maze, door, horde, run) | built earlier |
| Scrap King's call and the Loading Dock crew | built (replaces SCRAPKING-1) |
| Bogdan / Yefim's last request (fight or the dog tag) | built (replaces YEFIM-3) |
| Runner's patron (free him or side with the King) | built (replaces RUNNER-3) |
| Liberators at the Chokepoint Bridge (search, pay, other crossing, convoy) | built; a talk point, not a gate |
| Trail north: Radio Repeater, Husks with Tier 2 gear, robed figures | built |
| Escort: snipers at Sniper's Lip, Raider toll at the road cut, Hub arrival | built; new landmark "Road Cut Checkpoint" |
| Brute's return (Yefim's rematch request, +1500 Ru) | built; not checked in the harness |
| Crusher Row (the Garrison is the backer) and Rooke at the Maze (SCRAPKING-2/3) | built (6d); the rest of Black Valley is still generic |

## Tier 3 (Tier3Story.cs)
| Beat | State |
|-|-|
| Lev Marchuk, the pills (Stranded Ferry crate), the Confession | built |
| Pavel from the Hub to the Sub-Station, the lockdown (breaker room), the Reveal | built; new landmark "Breaker Room" (position not looked at) |
| Osei's spikes ("a pulse"), Hale (supply racket), Rask (coffee) | built |
| Voss and the Contractors' leak hunt (Quill at the Signal Tower, the ledger, the snipers were paid by "those") | built (polish, 7b) |
| Osei as an alternative pills source | built (E at the House while Lev's job is open) |

## Tier 4 (Tier4Story.cs, Preacher.cs) - built 2026-11-18
| Beat | State |
|-|-|
| Liberators' call (Miro), Tolik, wait for dawn | built (talk points + units; new landmark "The Barrier") |
| The Barrier itself (Barrier.cs): a concrete wall of high cover with 120 HP a tile (57 tiles), a steel catwalk behind it, an HP bar on the HUD; the horde tears at it and goes through gaps | built (8b); tripod machine guns not yet |
| The Siege: a horde of 90 fanatics fed in at 20-30 alive at once, beats at 30 / 55 / 75 (grenades, a machine-gun nest, the Preacher), the Preacher (pulse every 7 s, dive through it), eight mutants wait behind the line and leave when he falls | built; win rate NOT measured |
| The core: intact or cracked (an explosion on the Preacher cracks it); keep / destroy / sell to the Garrison (flags `core_kept`, `core_destroyed`, `core_sold_garrison`) | built |
| The Apex (Old Crimson, 6x health) and the Hunter | built |
| The Summit (Dovbush and Miro, a truce) | built (flag `truce_sentinels_liberators`) |
| Tolik fights beside you; a vision-swim effect and art for the Preacher's wave | not built |

## Tiers 5-7
Outline only (`STORY_OUTLINE_T4_T7.md`): the Array and the Array Commander, the Siege Ward and the portal, the Descent and the Mind. The ending ("those" lose the leash, the mutants are awake, the world hardens) is in `STORY.md`; the Tier 7 "how you leave" choice is still open.

## Cross-cutting gaps
1. **Nothing was played.** Unity first: Console red, then the Brute chase, a Husk, one night defence, the robed sprite, the escorts.
2. **Pavel's three escorts** (bunker, north, lake) now share one rule (`Tier1Story.PavelLost`): the save made when he joined is loaded, falling back to resetting the mission.
3. **People are units now** (`StoryNpcs.cs`): Yefim, Stas, Headman, Pavel (follows the story's places), the King, the Runner, Voss, Quill, Lev, Osei, Rask, Hale stand at their landmarks, unkillable, unarmed, hostile to nobody; they say small lines when you pass (`StoryBarks.cs`). They don't sit, sleep or walk.
4. **Husk/"those" art:** Husks look good (Yan); the robed sprite is a placeholder.
5. **No translation** of the story, quest and NPC text.
6. **A-Life:** Husk packs roam (no deck, no home); hostile gangs that see you first fight you before you can talk above Tier 1.
7. **Flags for the ending exist but are not read yet:** `runner_free`/`runner_king`, `bridge_passed`/`bridge_fought`, `lev_confessed`, `hale_cleared`, `spikes_known`, `pavel_lost`, `petro_robbed`, `tier1_done`, `tier2_done`, `tier3_reveal`.
8. **Leftover:** `StorySystem.cs` (the old "Heart of the Zone" story and its Sponsor/Ending code) still runs; it is to be replaced when the new ending is built. `ArtSource/double_armor.py` junk files (a cleanup task is queued).
