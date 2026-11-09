# New concepts for the four mutants without a good double-resolution sprite (first pass, 2026-11-09)
Yan: "check the other mutants that do not have 'good' updated sprites and do them, these too will need a new enemy concept, humanoid kinda thing".
Drawn in Python/PIL by `make_mutants_humanoid.py`; sprites in `ArtSource/mutants_anim/humanoid/` (NOT in the game yet: the old 16 px atlas cells are still used). Names are the existing database names; the concepts replace what the old cells showed (the atlas cell for Alpha Prowler already was a humanoid; the others were a rat, a jellyfish and a hooded blob).

| Mutant (tier) | Concept | Why it reads | Tells to animate later |
|-|-|-|-|
| **Alpha Prowler** (T3, ambush, cloak, bleeds you) | A lean, hunched, long-armed hunter: narrow body, ribs and spine showing, knuckles that reach the ground, one red eye on a narrow muzzle of teeth. The cloaked version is the same body pale grey. | Predator silhouette, clearly bigger and leaner than a man; the arms say "reach", the red eye is the only warm colour | the lunge (arms swing forward, claws spread), the cloak shimmer |
| **Rot Crawler** (the Rot Swarm, T1, swarm) | One of many: a bloated, fungus-eaten man crawling on all fours, a human skull with pale eyes, moss and caps along the spine. Always drawn in packs. | Small, low, readable as "something that used to be a person"; the green caps are the Zone's rot | scuttle cycle, bite, a pop of spores when killed |
| **Mind Weaver** (T3, ranged psi) | A gaunt floating seer: an oversized cranium with exposed folds, three eyes, a shroud for a body that tapers into tatters, long pale fingers weaving a thread of light between its hands. | Floats (no feet, no running), the thread is the tell for the psi blast | hands pull the thread apart (blast), the cranium folds pulse |
| **Void Weaver** (T5, ranged, cloak, neural overload) | A tall hooded figure whose face is a hole with one pale point; the robe is cracked with violet light and comes apart into motes at the hem; hands with long fingers. The cloaked version is lighter, ghost-like. | The darkest, tallest shape in the game; the cracks and motes are its anomaly | the face-hole widens (overload), the motes stream toward the target |

Open questions for Yan: keep "Rot Swarm" as a name for the crawlers? (the swarm is the pack, the unit is the crawler). Is the Alpha Prowler's new body too far from a "prowler" (it was a dog in the name)? Each needs run/attack frames after the concepts are approved; the canine rig (`make_canine_anims.py`) does not apply.


**Update 2026-11-09 (second pass):** sprites redrawn with volume shading (`pxart.py`) and animated from their joints (`humanoid_anims.py`): run/crawl/float, bite or cast, pounce for the melee ones (Alpha Prowler, Rot Crawler), psi blast (Mind Weaver) and neural overload (Void Weaver). GIFs in `ArtSource/mutants_anim/humanoid/`. The Husks (STORY.md) still have no sprite or unit.
