# The Choir as a repeatable mission (design draft, 2026-11-20; Yan: "the thing is a mission (secondary or story, needs some thinking), the dead city is full of husks, somebody wants it cleaned out, the bell is used to draw them in to reduce the husk population, could be a repeatable mission like the garrison raid")
DRAFT for Yan. Not built. What exists: `ChoirBoss.cs` (Great Bell as a boss unit, toll on the 11 s beat, Husk waves, plaza + pillars on the Tier 6 map, bell sprite, Testing Ground entry). `GarrisonRaid.cs` is the pattern for a repeatable raid (waves, cooldown `RaidCooldownDays = 4`, an armory reward) but it is turn-based-era code (rounds), so it is a pattern, not something to call.

## The idea in one paragraph
The Abandoned City (Tier 6) is full of Husks. A client pays to thin them. The Clock Tower's bell, rung from its rope, draws Husks from across the city to the plaza in waves; you hold the plaza (pillars, cover, the tower) and every Husk that dies is one fewer in the city. Stop whenever you like; the contract pays by the head and a bonus when its target is met. After a few in-game days the city refills a little (more Husks wander in) and the job can be taken again.

## Parts
- **Client and briefing**: needs a person. Tier 6 has no Safe Zone, so a story NPC at a landmark that exists on the map (the Garrison Barricade, the Evacuation Buses or the Police Station) or the Job Board in Tier 5/6. Candidates: Contractors (they hire cleaners), the Sentinels, a Garrison quartermaster (ties to the Siege Ward). Open question (below).
- **The Husk population**: a counter for the city (say 90 at the start, stored in the world save). City Husks are real packs placed once per world out of sight, like the Tier 2 crews (`Tier2Poi.PlaceCrews` pattern, quest squads `husk:city:N`). The population counts living city Husks; regrowth = new packs walk in over days (`Husk.Roam` already does this for roaming packs).
- **Ringing the bell**: the bell becomes a prop with an interact (E at the rope), not a boss unit. Each toll (11 s beat) sends the nearest city Husk packs toward the plaza (A-Life squads given a "go to the tower" task, so the world still lives by itself, rule 7: the only trigger is the player's rope) plus a small guaranteed wave from the streets so the first minute is never empty.
- **The toll's effect**: today the toll hurts you if you have a line to the bell. For a bell YOU ring that makes less sense. Options: (a) the toll deafens (aim blown, quiet screen) for players near the tower and puts Husks into a short frenzy (faster, more accurate); (b) no effect on players, only on Husks; (c) as built. My pick: (a).
- **Escalation**: toll count raises wave size; at toll 10 and 20 a heavier wave (a Husk with a Tier 6 kit leading, more than a normal pack). Never a nerf: the answer to being overwhelmed is the pillars, the plaza and your gear.
- **Winning and paying**: contract target e.g. 40 Husks killed in one session. Pay per Husk (small) + the completion bonus + a cache at the tower. Leaving ends the session; the kills still count against the city's population and pay.
- **Repeat**: cooldown of 4 in-game days like the raid, city refills a share of what you killed; paying less if the city is nearly empty; once the population hits 0, a one-time story beat (an empty city: the loot rooms stay quiet, an NPC line) and the job stops being offered until Husks return.
- **Story hook (optional)**: Husks are "dead inside" per the Tier 5/6 outline; the client's attitude to killing them can be the point (the Garrison sees pest control, a Sentinel sees a mercy, a robed figure might watch from the Bell Tower).

## What I would build, in order
1. Turn the bell into an interactable (reuse the sprite and the plaza), keep the toll as the beat. (small)
2. City Husk packs placed once per world and the population counter. (medium)
3. The ring loop: tolls, A-Life packs sent to the plaza, guaranteed streets wave, escalation. (medium, reuses ChoirBoss wave code)
4. The contract: briefing NPC, target, pay, cooldown, regrowth. (medium, reuse Story/QuestSystem patterns)
5. Harness checks, Testing Ground entry, pt-BR lines. (small)

## Open questions for Yan
1. Who hires you (Contractors / Sentinels / Garrison quartermaster / a story NPC)?
2. Secondary repeatable job only, or a story beat first and repeatable afterwards?
3. The toll when YOU ring it: deafens you (my pick), nothing, or hurts you as built?
4. Keep the Great Bell as a boss anywhere (e.g., a one-time finale at toll 30), or drop the boss idea entirely?
5. Does a "Ringer" Husk (the creature I proposed) still appear, e.g., as the heavy at toll 10?
