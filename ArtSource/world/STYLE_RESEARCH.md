# World Art Style Guide: 3/4 Top-Down (Zero Sievert-like)

Base: 16 px grid, 16 px tall characters, light from the top-left, 1 world unit = 16 px (PPU 16).
Notes marked **[est]** are my own measurements or conventions; no published source gives them.

## 1. Perspective rules
- Use the 3/4 top-down view: camera about 45-60° above the ground, orthographic (nothing shrinks with distance), and only one projection per scene. Show the top face and the front face. Never show side faces, and keep walls vertical with no lean. ([Slynyrd PB3](https://www.slynyrd.com/blog/2018/3/14/pixelblog-3-graphical-projections-1), [LPC guide](https://lpc.opengameart.org/static/LPC-Style-Guide/build/styleguide.html))
- **Top vs front ratio [est]:** a vertical height H is drawn at about 0.75·H px. A depth D is drawn at about 0.5–0.6·D px. Reference values:
  - crate, 1 m: top 8 px + front 10 px
  - car: 32×16 roof/hood strip + 8–10 px side band (front of the car faces down) or a 14 px band (car seen side-on)
  - rock: 60% top, 40% darker front
  - barrel: 6 px ellipse top + 10 px body
- **Wall height:** Stardew uses 16×32 characters and 16×48 wallpaper, so a wall is 1.5× character height ([farmer](https://stardewvalleywiki.com/Modding:Farmer_sprite), [wallpaper](https://stardewvalleywiki.com/Modding:Wallpaper_and_flooring)). For our 16 px characters, use these front-face heights:
  - interior or low wall: 24 px
  - one-storey building: 32 px (2 tiles)
  - two storeys: 48–56 px
  - fences and concrete blocks: 8–12 px
- **Wall tops / roofs:** draw a wall top as a 4–6 px cap strip that is much brighter than the wall face ([Slynyrd PB45](https://www.slynyrd.com/blog/2023/7/21/pixelblog-45-bricks-walls-doors-and-more)). Draw the roof straight on its plan footprint, shifted up by the wall height. A pitched roof gets a ridge line at about 40% from the top: the upper slope is lighter and the lower slope darker. Flat industrial roofs get a 2 px parapet rim plus vents and AC boxes. Use a column piece at each corner instead of special corner tiles (PB45).
- **Bricks:** each brick is 15, 7, 3 or 1 px in either dimension, with 1 px grout, so the pattern tiles inside 16 px (PB45).
- **Doors and windows [est]:** a door is 10–12 px wide × 18–20 px tall, so the top of the frame clears the character's head by 2–4 px. Draw a broken door as a dark 10×18 hole. Windows are 8–10 px wide × 8 px tall, placed with the sill about 10 px above the ground line. Windows are dark blue-grey with a 1 px highlight, or a boarded variant.
- **Roof cutaway:** when the player enters a building, fade the roof sprite to alpha 0 over about 0.2 s and show the interior. Walls on the south side drop to cap height only.

## 2. Trees and foliage
- **Size vs a 16 px character [est]:** bush 16–24 px wide. Birch or pine canopy 32–48 px wide. Large oak 48–64 px wide. Canopy bottom about 20–28 px above the ground. Trunk 4–6 px wide with a 6–8×4 px root flare. Collision is only the 6×4 px trunk base.
- **Shading:** build each canopy from round foliage "bundles", each with 3 tones (base, darker, lighter) plus 4–5 colors in total. Put a dark crescent at the bottom-left and a small light blob at the upper-right, or mirror both for top-left light. Use 2×2 px leaf squares for small-leaf texture. On conifers, upper branches overlap the lower ones and cast small shadows on them. ([Slynyrd PB44](https://slynyrd.com/blog/2023/5/22/pixelblog-44-top-down-trees))
- **Forests:** use ball-shaped canopies in staggered, overlapping rows, with occasional color swaps and rare asymmetric "landmark" trees ([PB21](https://www.slynyrd.com/blog/2019/9/18/pixelblog-21-top-down-objects)).
- **Occlusion:** draw the canopy as a separate sprite sorted above characters. When the player's feet are inside the canopy rectangle and above the trunk base, lerp the canopy alpha to 0.35–0.5. A cheaper alternative is a radial "hole" shader around the player ([80.lv](https://80.lv/articles/transparent-trees-effect-set-up-in-unity/), [Godot forum](https://forum.godotengine.org/t/make-a-wall-tree-a-little-bit-translucent-trasnparent-when-there-is-a-player-behind-it-in-2d-top-down-especially/111322)).
- **Avoiding the grid look:**
  - Place props at random sub-tile offsets (±0–8 px), never snapped to tiles.
  - Flip horizontally at random.
  - Pick from 3+ variants and apply a ±5% palette jitter.
  - Scatter grass tufts at 0.3–0.6 per tile in forest and 0.1 per tile on asphalt.
  - Add decals: leaves, puddles, cracks, trash, bones, tire tracks.
  - Let clusters overlap tile edges, and avoid edge-to-edge touching clusters ([PB20](https://slynyrd.com/blog/2019/8/27/pixelblog-20-top-down-tiles)).
  - Make at least one variant of every heavily used texture (PB45).

## 3. Ground
- **Blending:** use a dual-grid corner autotile: 16 tiles per pair of terrains, sampled at tile corners ([Red Blob](https://www.redblobgames.com/articles/autotile/gemini/autotiling.html)). Draw organic, noisy edges inside each tile and give every tile 2–4 variants. To do it procedurally, render terrain from a noise field per pixel (or per 2 px), then quantize to the palette. For a softer edge, apply 1–2 px of ordered dithering only on the boundary band. Keep dithering sparse ([LPC](https://lpc.opengameart.org/static/LPC-Style-Guide/build/styleguide.html)).
- **Base textures:** keep ground textures low-contrast and quiet, with flat patches and negative space. Never put two busy textures next to each other (PB20).
- **Paths and roads [est]:**
  - Dirt tracks: 2 ruts, each 3 px wide, spaced 10 px apart, with grass between them.
  - Asphalt road: 3–4 tiles wide, with a faded dashed centre line (6 px on / 6 px off), cracks, potholes and grass in the cracks.
  - Gravel paths wind along a smoothed random walk.
  - Roads link POIs and give the player navigation lines.

## 4. Shadows
- Use separate shadow sprites on their own layer below objects so props work on any ground. Bake a shadow into a sprite only if it fits inside the sprite's frame ([PB21](https://www.slynyrd.com/blog/2019/9/18/pixelblog-21-top-down-objects)).
- **Color:** dark purple-brown `#322125` at 60% alpha ([LPC](https://lpc.opengameart.org/static/LPC-Style-Guide/build/styleguide.html)). Keep shadows short (≤16 px), cast to the side and slightly downward (PB45).
- **Characters:** 10×4 px ellipse centred at the feet.
- **Trees:** ellipse about 70% of the canopy width, centred under the trunk with a slight offset away from the light (PB44).
- **Buildings:** a parallelogram 6–8 px wide down the side away from the light, plus a 3–4 px band below the front wall.
- **Rendering:** draw all shadows to one layer at full opacity, then composite that layer at 0.6 alpha. This stops overlapping shadows from double-darkening.

## 5. Depth sorting and pivots
- In Unity, set Graphics → Transparency Sort Mode = Custom Axis (0,1,0) ([Unity docs](https://docs.unity3d.com/2020.3/Documentation/Manual/sprites-sort.html)). On each SpriteRenderer set Sprite Sort Point = Pivot. Use a Sorting Group on multi-sprite objects (character + hands + gun).
- **Pivot:** bottom-centre at the ground-contact line. For characters that is the feet. For buildings and walls it is the bottom of the front face. Large buildings are split into a front wall (sorted) and a roof (on an overhead layer, faded).
- **Collision:** use the footprint only, never the visual. Character: 8×4 px at the feet. Crate: full top-face rectangle. Building: plan rectangle = front-face width × top depth. Bullets use the same footprint plus a "tall" flag so they are blocked by walls but pass over low props.
- **Layers:** Ground → Decals → Shadows → Y-sorted (props, actors, wall faces) → Canopies/Roofs → FX → Fog/LOS → Light/grade → UI.

## 6. Atmosphere and camera
- **Zero Sievert vision:** a vision cone (an option; with it off you get 360°) hides enemies behind you. A separate toggle adds a white outline at the cone edge, with adjustable alpha. Players asked for a 160–180° cone, so the default is narrower ([Steam 1](https://steamcommunity.com/app/1782120/discussions/0/594010171065271649/), [Steam 2](https://steamcommunity.com/app/1782120/discussions/0/3773490849746193865/)). Enemies need line of sight, so you can break contact behind cars or buildings ([PC Gamer](https://www.pcgamer.com/this-top-down-shooter-is-like-stalker-as-a-roguelike/)).
- **Implementation:**
  - Raycast a visibility polygon from the player, about 360 rays, blocked by "tall" footprints.
  - Clip it with a 120–140° cone, plus a radius of about 1.5 tiles all round so the player always sees nearby surroundings.
  - Outside the polygon, darken the world to about 35% brightness, desaturate it, and hide actors.
- **Night:** visibility shrinks to the headlamp radius. Night is "properly dark", and dark interiors need NVG or a light ([PC Gamer](https://www.pcgamer.com/this-top-down-shooter-is-like-stalker-as-a-roguelike/), [Namu](https://en.namu.wiki/w/ZERO%20Sievert)).
- **Day/night tint [est]:** use a multiply overlay:
  - day `#FFFFFF`
  - dusk `#C89A7A`
  - night `#2A3350` at about 85%
  - lamp radius about 5 tiles with a soft, banded (pixel-stepped) falloff
- **Vignette:** 15–25%.
- **Weather and options:** rain, fog and thunder, a radiation cloud overlay, swaying grass, and adjustable fog/rain particle density ([SteamDeckHQ](https://steamdeckhq.com/game-reviews/zero-sievert/)). For rain, use 1×3 px streaks angled 15°, plus splash decals. Fog is two parallax noise layers at 10–25% alpha.
- **Grading:** muted, low-saturation palette ([foro3d](https://foro3d.com/en/2026/mayo/zero-sievert-tecnicas-de-pixel-art-y-niebla-de-guerra-en-gamemaker.html)). Use no pure colors, shift shadows toward purple and highlights toward yellow (LPC).
- **Camera [est]:** render at 480×270 internally and scale by an integer factor (×4 = 1080p). That fits about 30×17 tiles, roughly 17 character heights vertically. Look-ahead toward the aim direction is 25–35% of the half-screen.
- **Map size [est, no source gives numbers]:** a Zero Sievert map feels like about 6×6 to 10×10 screens. Target 192–256 tiles square (3–4 km of feel). The Swamp is the largest and most open map ([Namu](https://en.namu.wiki/w/ZERO%20Sievert)).

## 7. Zero Sievert feel worth copying
- **Loop:** Bunker hub → train to the map → loot → reach a green-circle extraction (some need a key). Death loses everything found during the raid; gear taken in is kept ([PC Gamer](https://www.pcgamer.com/this-top-down-shooter-is-like-stalker-as-a-roguelike/), [Namu](https://en.namu.wiki/w/ZERO%20Sievert)). Extraction points are random, so the map has to be checked constantly ([SteamDeckHQ](https://steamdeckhq.com/game-reviews/zero-sievert/)).
- **Map generation:** fixed landmark POIs (the Village always sits in the forest centre, plus the Sawmill), with random fill around them. Industrial and Mall maps stay about 80–90% the same between runs ([Steam](https://steamcommunity.com/app/1782120/discussions/0/3544924290411507088/)). Forest POIs: bandit camps, crystal field, hunter cabin, Igor's house, police station (boss + key), sawmill, town, scattered small houses ([TechRaptor](https://techraptor.net/gaming/guides/zero-sievert-forest-map-and-locations-guide)).
- **Our POI density [est]:** one major POI per about 64×64 tiles, one minor POI (camp, wreck, shack) per about 24×24 tiles, all connected by roads and tracks.
- **HUD:** red health bar at the top-left with a green stamina bar under it. Status icons cover bleeding, wound, hunger, thirst and radiation. A 6-slot quickbar holds meds and grenades. A device screen (J) has tabs for map, quests, perks and so on (Namu).
- **Looting:** player grid on the left, container or corpse on the right. Items are revealed one at a time with a delay, and that wait is the tension (PC Gamer, Namu).
- **Aliveness:** wildlife (wolf packs of 4+, boars), roaming factions, distant gunshots, Geiger crackle, and birds landing on corpses ([gamepressure](https://www.gamepressure.com/S013-amp.asp?ID=19635)).

## 8. Palettes (authored to fit the muted brief; dark → light)
| Set | Hex |
|---|---|
| Forest ground | `#2B2A1E` `#3D3B28` `#4F4C32` `#62603F` |
| Dry grass | `#5E5838` `#7A7245` `#958A55` `#B0A46B` |
| Mud | `#2E241B` `#43352A` `#5A4838` `#6E5B47` |
| Marsh water | `#1B2422` `#25332F` `#33463F` `#4F6458` |
| Asphalt | `#25272A` `#33363A` `#45484C` `#5A5D60` |
| Concrete | `#55575A` `#6E6F6E` `#8A8A85` `#A6A59D` |
| Rusted metal | `#3A2219` `#5E3524` `#86502E` `#A8703E` |
| Brick | `#4A2A24` `#683A30` `#844C3D` `#9E6450` |
| Plaster | `#6E6A5E` `#8C8676` `#A8A190` `#C2BBA8` |
| Roof tiles | `#3A2A2A` `#5A3A34` `#74493E` `#8E5D4C` |
| Roof tin | `#3B4245` `#525C5E` `#6B7675` `#88928D` |
| Foliage | `#1E2A1C` `#2E3D26` `#425431` `#5B6E3E` `#7A8A50` |
| Shadow | `#322125` @ 60% |
