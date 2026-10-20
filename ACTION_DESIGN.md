# agora vai - real-time fork: design (agreed with Yan, 2026-10-16)

The turn-based game stays as it is (frozen snapshot: branch `turn-based-fallback`, Batch 131f; work continues on
`tier-maps`). This document is the plan for a separate repo, `agora-vai-action`, copied from that snapshot, where
the combat becomes real-time. Everything not listed here (maps, A-Life, factions, items, trading, quests, art,
floating hands, audio, banter, the "!" detection) carries over unchanged. All the hard rules in CLAUDE.md still apply,
including "never nerf enemies: give the player tools".

## Feel

Between S.T.A.L.K.E.R./Zero Sievert (slow, tense, deadly) and Max Payne (rare stylish moments) - not Hotline Miami.
Patience usually wins; aggression is possible but risky. Fights swing: hold an angle, get pinned, move, push, dive.

## Core rules

- **Pause:** ESC pauses. Inventory, items, healing, looting all happen in real time.
- **Time:** the world clock always runs (about 6 real minutes per game day, as exploring today). No rounds, no AP/MP.
- **Controls:** WASD move, mouse aim, RMB aim down sights, LMB fire, Shift sprint, Ctrl crouch, Space dive, R reload,
  Q/E lean, F interact (hold behind an unaware enemy = takedown), G throw (no grenade selected = bolt), Tab inventory,
  Y swap weapon, ESC pause. Gamepad later.
- **Camera:** drifts toward the mouse; aiming looks further; scopes further still (Zero Sievert style).
  Directional sound for fights out of sight.

## Hit model (no dice)

- A body is a footprint circle on the ground. A bullet travels over the ground; it hits when its path crosses
  the footprint, from any direction (so standing higher or lower on screen gives no advantage).
- **Height comes from the crosshair:** over the head pixels = head height, chest = torso, legs = legs.
- Weapon spread scatters the shot sideways (miss / the arm on that side) and in height (lower or higher part, or over the head).
- Holding aim steadies spread over ~1 s. Moving widens it (walk a little, sprint a lot). Recoil kicks the crosshair after each shot.
- Bullets have travel time: lead moving targets at range.
- Cover blocks by height: low cover hides legs and torso (the head shows when leaning out); walls hide everything.
  Crouching lowers every part. Materials as today: rounds go through wood/tin/brush/cloth, not concrete.
- Enemies aim the same way; their rank decides how well they hold head height.
- Suppression: rounds landing near a unit shake its aim (both ways).

## Weapons

New stats per gun: **accuracy** (steadied spread), **handling** (steady-up speed, sway), **recoil**, **recovery**,
**fire rate / modes** (semi, burst, auto), **muzzle velocity** (bullet speed, drop-off), **penetration** (cover and
armor; AP/HP ammo plug in here). Weight, wear, jams (d100 as now, 1% +2% per wear step) and magazines stay.

## Health, wounds, death

- **Limb health (Tarkov-lite):** head, torso, left/right arm, left/right leg, each with its own HP.
  Head or torso at 0 = down. A limb at 0 is broken; more damage to it spills into the torso.
- **Wounds (Yan's rule):** above 60% part HP no wound. Below 60% each hit has a chance that rises as the part weakens,
  `chance = (60 - hp%) / 60 * 0.75` (50% ~13%, 40% 25%, 20% 50%; 0% = broken). HP rounds raise it, AP rounds lower it.
  Types: bleeding (any part, bandage), fracture (limb, splint), concussion (head: blurred view and sway for a while).
- **Debuffs only from wounds/broken parts:** broken leg = limp, no sprint; broken arm = heavy sway, slow reload.
- **Armor:** helmet covers the head, vest the torso. Heavier armor = slower and louder.
- **Healing:** bandage, splint, medkit; each takes a few seconds of real time.
- **Downed (Yan):** at 0 you go down and bleed out. Lying still drains slowly; crawling (e.g. to cover) drains faster.
  A partner revives by holding a key for a few seconds. Solo: use a stim to get up, otherwise death.
- Enemies use the same system (shoot a Raider's legs and he limps).
- Saves only at safe places (bonfires, bases). Permadeath maybe later as a mode.

## Dive and bullet time (Yan)

- Space: dive in any direction; time slows while airborne, aim and fire freely.
- **On landing you are prone ~0.6 s** (can't move or dodge, getting up included): diving into cover is safe, into the open is risky.
- Costs **Focus** (meter filled by kills and near misses), so no chaining.
- In bullet time the body part under the crosshair is highlighted and hits on it get a better wound chance.
- **Co-op:** bullet time slows everyone within ~40 tiles / in the same fight, and they can all use it (team effort).

## Stealth, throwables, anomalies

- Detection "!" meter, banter and shouts as today, in real time.
- Takedown: hold F behind an unaware enemy. Suppressed guns don't alert the whole squad. Found bodies alert squadmates.
- Ambush: a first shot on an unaware enemy does bonus damage.
- Throwables: frag, smoke, flashbang; enemies throw them too and shout a warning first.
- Anomalies as today; throw bolts (G) to find edges; a detector item beeps faster when close.

## Melee (Yan, 2026-10-20)

Melee's place: ambushes round corners, unseen camp massacres, parrying mutants - not open charges into gunfire.
- **Controls (Yan, RT 7e):** with a melee weapon in hand, LMB swings; hold LMB to charge the swing (held as long as you like,
  up to x1.75 damage a second past the wind-up). RMB lifts it over your head and charges the throw; let go to throw it (full
  speed and range at 0.6 s held; let go before 0.2 s - or a two-handed weapon's 1 s wind-up - and you just lower it).
- **Every melee weapon can be thrown.** Smaller = faster swings and faster, longer throws. Thrown weapons fly (travel time,
  spin, max range) and hit through the same body-part rules as bullets.
- **Bladed weapons stick** where they hit: in wood, tin or cloth cover (pull it out with E), or in a body. A living enemy or
  mutant pulls it out after a few seconds, spending 2-3 s doing it; then it lies on the floor. Stone, concrete and steel make it
  bounce off onto the floor. **Blunt weapons never stick.**
- **Every bladed weapon makes the target bleed** on a hit and on a throw that lands. **Blunt weapons concuss** (`Body.Concuss`).
- **Two-handed weapons** are heavier: slower swings, more damage, shorter throws and a 1 s wind-up before a throw.
- New perk **Flying Steel**: throws travel faster and further, and pulling a stuck blade out is quicker.

| Weapon | Tier | Kind | Notes |
|-|-|-|-|
| Hunting Knife | 1 | knife | as is |
| Spetsnaz Switchblade | 2 | knife | quicker draw than the others, flies faster when thrown |
| Steppe Machete | 3 | blade | as is |
| Serrated Trench Knife | 4 | knife | easier parry: a longer window |
| Obelisk Ritual Blade | 5 | blade | as is, but a boss reward: one Master-rank Obelisk boss at an Obelisk stronghold on Tier 5 or 6 |
| Hatchet | 2 | axe | wooden handle, plain axe head |
| Fire Axe | 3 | axe, two-handed | red head, black handle |
| Tactical Hatchet | 5 | axe | look after Leon's axe in RE9 (Yan sends a reference); found in a secret room of a Tier 4-5 lab (not the G-7 lab): break a wall, walk a long corridor; it's stuck in a corpse in black armour with a red visor (a nod only, no names from that game) |
| Wooden Baseball Bat | 2 | blunt, two-handed | concussive |
| Hammer | 3 | blunt, one-handed | faster swings, smaller reach, concussive |
| Sledgehammer | 4 | blunt, two-handed | like the bat with more damage; breaks cover (not walls) |
| Barbed Wire Bat | 5 (secret) | blunt, two-handed | bleeds and concusses; carried by a boss in a Tier 4 corner or dungeon, lore to settle (not regular Raider kit) |

To-do (Yan): the Tactical Hatchet's corpse gets up when you take the hatchet and fights you in melee - you parry him with it.

## Weight and stamina

Carried weight slows you and drains stamina faster (sprint, dive, melee). Looting becomes a choice.

## Enemy AI (squads stay the same, now every frame)

Patrol and posts as today; spot, shout, take cover, hold angles, suppress, flank, throw grenades, call backup on
the radio, retreat when losing. Mutants: hounds circle and lunge in packs; the Brute charges and throws its boulder
if you keep distance (hit = slowed); the Veil Prowler goes invisible. The new mutant animations carry over.

## Companion

Hireable Wanderer kept as an AI partner for solo play: follow, hold here, attack my target. Fills an empty co-op slot later.

## Co-op (decided: build for it from the start)

- Host runs the world (clock, A-Life, saves); other players send inputs. Unity Netcode for GameObjects + Relay.
- 2-4 players on the same map; explore separately; **map exits only together** (everyone at an exit and confirmed, like Borderlands).
- Separate inventories; drop items for each other; trade as now.
- The first prototype is single-player, but the combat code is written host-authoritative from day one.

## Perks (rework; same groups, names kept where they fit)

| Group | Perk | Real-time effect |
|-|-|-|
| Sniper | Marksman's Focus | Less sway when aiming |
| | High-Velocity Impact | Long shots pierce armor more |
| | Overwatch | Hold aim on an area: the first enemy to step in slows time briefly |
| Assault | Recoil Control | Recoil climbs half as fast |
| | Controlled Taps | Single shots reset spread faster |
| | Suppressive Fire | Wider suppression |
| SMG | Bullet Hose | Little spread at close range |
| | Run and Gun | Fire while sprinting |
| | Hit and Run | Speed boost 2 s after a hit |
| Heavy | Entrenched | Damage reduction while standing still |
| | Walking Tank | Normal speed with heavy weapons |
| Shotgun | Breacher / Staggering Blast | Close hits knock down |
| | Buckshot Spread | Wider lethal pellet cone |
| Sidearm | Gunslinger | Dual pistols, one per mouse button |
| | Point-Blank Execution | Instantly finish downed or stunned enemies |
| | Double Tap | Faster pistol fire rate |
| Melee | Bull Rush | Sprint into a shoulder charge that knocks down |
| | Desperate Parry | Timed block parries a melee hit |
| | Blade Dancer | Melee hits chain faster |
| Survival | Quick Hands / Gunsmith | Faster reload / jam clear |
| | Sprinter | More stamina |
| | Tactical Reposition | Faster leaning and moving between cover |
| | Vault | Vault low cover |
| | Adrenaline Surge | At low HP: a burst of slow-mo and Focus |
| | Haggler, Pack Mule, etc. | Unchanged |
| New | Tumbler | Shorter prone time after a dive (never zero) |
| New | Disarm | Grab the gun from a stunned enemy |
| New | Cold Blood | Focus fills faster from near misses |

Turn-only systems are removed: action bar, AP/MP, turn overwatch, surprise round, the body-part targeting panel.

## First prototype (done when all of this works)

One part of Tier 1 with: movement, aim, fire, reload with the new stats on 3-4 guns; the hit model with height,
limb health and wounds; cover; the dive and bullet time (solo); one Raider squad that patrols, spots, shouts,
flanks and suppresses; downed and bleed-out. Old turn-based saves are not compatible.

## Setup

- Yan creates an empty private repo `grimmylock/agora-vai-action` (no README). The project is copied in from
  `turn-based-fallback` (Batch 131f) plus this document. Game name stays "agora vai".
- Yan's side: also label the Plastic changeset ("turn-based-131") and keep a zip of the project folder.

## Real combat tests (Yan, 2026-10-19: everything below waits for a play session in Unity, RT batches 1-5)

- Moving: walk/sprint/crouch speeds and stamina; crossing a map exit; E looting.
- Shooting: the crosshair and spread; hitting heads vs bodies; reload and jam timing; firing over your own cover.
- Body: how fast fights end now that heads/torsos count; wounds; going down, crawling, stims/splints/bandages (belt 1-4).
- Dive: distance, slow-mo length, 0.6 s prone (risky but fair?), how fast Focus fills.
- Human enemies: alive or static/too aggressive? bursts and pauses, flanking, pins, call-outs, accuracy.
- Mutants: hound pace vs yours, bite rate, the Brute's pace and boulder, the leap.
- Weapon balance (see below) - per-gun damage, rate, recoil vs armor and HP.
- Reloads (RT 6g-6i): magazines dropping and seating (hidden pistol magazines sliding out of the grip), round-by-round loading
  and firing mid-reload (tube shotguns, levers), revolvers finishing the round, break-actions opening on the hinge.
- Per-gun handling (RT 6d, `Gunplay.Guns`): does each gun feel different? fire rates, reloads, how fast it steadies, kick;
  SMGs/pistols from the hip on the move vs rifles aimed; shotgun patterns (choke); the 10-degree kick cap when holding the trigger.
- Any red in the Console.

## Later list

- **Starting area rework (Yan, 2026-10-20; waiting on his map and quest spots):** Bonfire Village as a real Safe Zone in the
  Cordon rookie-village spirit - the starting faction's boundary and perimeter that hostile squads (mostly Raiders) can't or
  very rarely cross (and then fight the guards), guards, random friendly squads, a trader, a medic, a small bar and barman.
  Tutorial: the trader's job (recover something from a dead courier, a fixed pack of 2-3 Blind Maws) and the barman's job (clear
  a nearby spot held by a fixed 3-man Raider squad, laid out so stealth works - teaches sneaking, takedowns). Plus: name your
  character, and a save made at character creation. Renders: `maps/renders/start_area/`.
- A real run cycle for the hounds (Batch 131f): the knee-rig walk looks wrong at running speed.
- Shotguns: 3 full patterns to drop a mid-armor target at 5 tiles (2-4 s with a pump) vs ~1 s for rifles - look at it after playing.
- SMGs still need 14-15 chest hits on the heaviest tier 3 armor (Wasp-9, Hornet): their answer is aiming for the head or AP.
