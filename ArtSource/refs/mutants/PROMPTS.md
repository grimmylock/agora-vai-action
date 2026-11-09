# Gemini prompts for the four new mutants (reference images; Yan generates them in the Gemini app, 2026-11-09)

Why references: the pipeline that turned your earlier references into the game's mutant sprites (`mutant_from_ref.py`, `make_mutants_hd.py`) reads one clean picture per sprite. These four need a 3/4 view WITH DEPTH (the first attempts were flat side-ons), so each gets one neutral reference first, then a few key poses made from that first image.

## Attach these reference images (in `style_pack/`)
- Every NEUTRAL message: attach `style_1_finished_mutants.png` (the five finished mutants: this is the look, density and finish to match), `style_2_scale_with_a_man.png` (how big mutants are next to a man) and `style_5_world_palette.png` (the world the creature stands in: palette and light).
- Do NOT attach the eel or the brain (`style_4`, `style_3`): they are finer than the rest of the game and pull the result toward "HD" (Yan, 2026-11-09: they look weirdly HD next to the world and the human NPCs). They stay in the pack only to show what NOT to match.
- Add this line before the style block: "The attached images show the art style, finish and size to match. Do NOT copy any of the creatures in them; draw a NEW creature as described, in the same style: the pixel clusters, selective dark outline, muted palette and shading of the first sheet, and the same simple finish: a few tones per material, no fine texture."
- POSE messages: attach only that creature's `neutral.png` (the style images are not needed again).

## How to use
1. Paste the STYLE BLOCK, then the creature's NEUTRAL prompt. Generate until the design is right (4-6 tries are normal). Save it as `ArtSource/refs/mutants/<key>/neutral.png`.
2. For each POSE, start a new message in the SAME chat, attach `neutral.png` and write: "Same creature, exactly the same design, colours and proportions. Change only the pose: ..." then the pose line. Save as `<key>/<pose>.png`.
3. Always: one subject, plain flat background (solid #808080 mid grey), nothing else in the picture, no text, no shadow on the ground, full body inside the frame with a margin, 1:1 square.
4. If a result is smooth/painterly or too detailed, add "crisp hand-placed pixels, limited palette, no anti-aliasing, no gradients, no blur, fewer details". Pixel counts do not matter (the pipeline shrinks it); clean shapes and a strong silhouette do.

## STYLE BLOCK (paste first, every time)
Pixel art game sprite for a gritty 2D top-down survival shooter set in an anomalous post-industrial wasteland (Roadside Picnic mood, Zero Sievert look). Muted, desaturated palette with one accent colour. Hand-placed pixels, limited palette (about 16 colours), dark selective outline (never pure black), light from the top-left, volumetric shading with a lit top-left edge and a darker bottom-right edge. Three-quarter view from the front-right, camera slightly above, so the body shows real depth: chest, shoulders and head have thickness, the far limbs sit partly behind the near ones. The creature faces right. Same pixel density and finish as the first reference sheet: clustered shading with a handful of tones per material, no fine texture, no gradients, no glow or bloom, no painterly rendering. Plain flat mid-grey (#808080) background, full body visible, one creature, no text, no ground shadow.

## ALPHA PROWLER (key `alpha_prowler`; tier 3 ambush hunter, bleeds you, can cloak)
NEUTRAL: A lean, hunched, long-armed predator, about twice a man's height when it stands: a narrow ribcage with the ribs and spine showing through grey-violet skin, a long hunched shoulder hump with a row of small bone spurs down the back, very long arms that reach nearly to the ground and end in three long bone claws, digitigrade legs (backward knees) with clawed feet, a long skull with a narrow muzzle full of small sharp teeth and ONE glowing red eye in a dark socket. Colours: charcoal and grey-violet skin, pale bone, one red accent (the eye). Standing hunched, weight forward, knuckles near the ground.
POSES:
- `run_a`: mid-lope, arms stretched far forward and reaching, body leaning forward, back legs pushing off behind.
- `run_b`: mid-lope, all four limbs gathered under the body, back arched high.
- `bite_windup`: reared back, head pulled back and up, mouth slightly open, one arm raised.
- `bite_snap`: lunging forward, head thrust ahead, jaws open wide then snapping, claws spread toward the viewer's right.
- `pounce_crouch`: crouched very low, chest on its forearms, rear raised, about to spring.
- `pounce_air`: fully airborne, stretched out horizontally, arms forward with claws spread, legs trailing behind, mouth open.
- `pounce_land`: landing on the target, forelegs planted forward, head low, jaws closing.
CLOAKED variant (optional): same image, but pale grey-blue and semi-transparent like heat shimmer, outline still visible: `cloaked.png`.

## ROT CRAWLER (key `rot_crawler`; tier 1 swarm unit, comes in packs)
NEUTRAL: A bloated, fungus-eaten human crawling on all fours: a swollen rotting trunk with a sagging belly and visible ribs, thin wasted limbs with long fingers and bare toes, a human skull-like head hanging low with sunken sockets holding a pale point of light and a slack jaw of broken teeth, a few yellow weeping sores, and a ridge of pale yellow-green mushrooms with thick stalks growing along the spine and shoulder, moss patches on the belly. Colours: sickly olive and brown flesh, bright yellow-green fungus as the accent.
POSES: `crawl_a` (right arm and left leg forward, body low), `crawl_b` (opposite limbs forward), `bite_windup` (head pulled back, jaw hanging open, front arms braced), `bite_snap` (head lunging ahead, jaws wide, arms stretched), `pounce_air` (springing off all four limbs, stretched out, mouth wide), `pounce_land` (landing on its front hands, head low, biting).

## MIND WEAVER (key `mind_weaver`; tier 3 ranged psi caster, floats)
NEUTRAL: A gaunt floating seer, never touching the ground. A huge hairless skull whose top is open: the exposed pink-violet folds of the brain are visible over the crown. Three pale glowing eyes (two large, one on the brow) with violet pupils, a thin dark slit of a mouth. A thin neck, a narrow chest, and instead of legs a tattered dark-violet shroud that tapers down to long torn strips hanging in the air. Long thin arms with four very long pale fingers on each hand, held up and apart, a bright thread of violet light stretched taut between the hands and a few light motes drifting. Colours: lavender-grey skin, dark violet shroud, pink-violet brain, glowing violet-white light as the accent.
POSES: `float_a` / `float_b` (idle drift: slightly up, slightly down, shroud swaying opposite ways), `cast_charge` (hands drawn far apart, thread taut and bright white, eyes blazing, brain glowing), `cast_release` (both hands thrust toward the right, a ring-shaped pulse and a beam of violet light leaving the hands, body leaning back).

## VOID WEAVER (key `void_weaver`; tier 5 ranged, can cloak, neural overload)
NEUTRAL: A tall hooded figure of torn near-black violet robes, the hood a deep cowl whose face is a hole: pure black with a single pale point of light and a few distant specks, like looking into night sky. The robe is cracked all down the front with glowing violet light (like fractures in glass), the sleeves are long and wide, ending in thin, many-jointed pale hands with four long fingers. The hem of the robe dissolves into drifting motes and shards of violet light instead of reaching the floor (it floats just above it). Colours: near-black indigo robes with subtle fold shading, bright violet cracks and motes as the accent.
POSES: `drift_a` / `drift_b` (idle sway, hem motes rising), `overload_charge` (arms rising and opening wide, the face-hole widening, the cracks blazing nearly white), `overload_release` (arms thrust forward to the right, a pulse ring from the face and a stream of violet motes flying right).
CLOAKED variant (optional): `cloaked.png`: same figure, lighter and ghost-like, semi-transparent.

## In-context check (every new creature, before anything goes in the game)
`bash Tools/render/render.sh bonfire_village` once, then `python3 Tools/render/incontext.py <sprite.png> --out ctx.png` puts the sprite on a real map render beside human NPCs and the finished mutants at their true relative pixel density (the world and men at 16 px a tile, creatures at double density). Judge: is it the same finish, not brighter or more saturated than the world, not finer than the Pallid/Dread? `--demo` shows the finished mutants and the first-pass humanoids as an example. It is a size/finish comparison, not a lit Unity screenshot.

## Afterwards
Tell me when a creature's files are in the repo (or say which folder). I will check the neutral against the concept, convert it with the existing pipeline into a game-sized sprite, then build the animation frames from the pose images (cutting and re-posing from them, never stretching).
