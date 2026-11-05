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
- Bullets have travel time: lead moving targets at range. **Built (RT 8):** every round flies at its gun's speed
  (`Gunplay.Round`/`Fly`, `Handling.Speed`); bullet time slows them; enemies lead you (`Gunplay.Lead`), so changing direction
  after they fire (a dive) is a real dodge. Near misses pin, but not mid-dive (Yan, 2026-10-23).
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

- Space: dive in any direction on stamina; nothing hits you while airborne (i-frames), aim and fire freely; the landing is
  prone and open. V (rebindable): bullet time on/off, drains Focus (Yan, 2026-11-03: "unbinding bullet time from the dodge,
  having it be a separate key ... and use the dodge for mechanics like those, having it be invulnerable").
- **On landing you are prone ~0.6 s** (can't move or dodge, getting up included): diving into cover is safe, into the open is risky.
- The dive costs **stamina** (30 of 100). **Focus** (filled by kills and near misses) feeds bullet time only.
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

**Melee slot (Yan, 2026-10-20, for the inventory/equipment UI):** a dedicated melee slot beside the two weapon slots, so everyone
always has at least a knife (for parrying). A throwing build can still put more melee weapons in the weapon slots.

**Built (Boss 1, 2026-10-29):** the Tactical Hatchet's corpse gets up when you take the hatchet and fights you in melee - you
parry him with it. Secret Lab Beta's south-west rooms have a cracked wall (E breaks it), a long dark corridor, and the room where
"The Visor" lies; also on the Testing Ground (Humans tab, BOSS row).

## Parry and bosses (Boss 1, Yan 2026-10-29: the baseline for every boss fight; "HYPE MOMENTS AND AURA")

- **Parry: F** (rebindable in Options; search moved to G), with a melee weapon in hand, against any human's melee hit. The first
  0.12 s of the guard is a **perfect parry** (no damage, the attacker staggers 1 s, a hit-stop, sparks, Focus); later in the 0.35 s
  window it's **blocked** (no damage). 0.45 s cooldown. The Serrated Trench Knife's windows are x1.5.
- **Bosses** (`Boss.cs`, a reusable framework): telegraphed moves - a **white glint** = parry it, a **red glint** = it can't be
  parried, dive or step out. Combos. Bullets glance off unless he's staggered (the parry is the way in, your gun the punish).
  Phase 2 at half health (aura, a sting, faster, new moves). No posture bar (Yan: posture only makes sense in boss duels).
  At 0 HP he breaks: **E next to him = the finisher** (slow motion, the camera, the blood). Once he's looted, a smoke grenade and
  the body is gone (Yan).
- A name card when the fight starts, his health bar with a draining chunk, STAGGERED / EXECUTE, procedural sounds (parry,
  perfect parry, glints, the sting, the finisher, the smoke).
- Not yet: mutants' claws can't be parried (they don't go through `Melee.Strike`); finishers on normal enemies; the melee slot.

## Dodge bosses (Yan, 2026-11-03: "some of them a bullet-hell style bossfight ... more akin to Binding of Isaac where it's kinda
bullet hell but not really? you have to dodge stuff")

- For mutant and anomaly bosses; human bosses stay duels (the Visor). Few, slow, big projectiles you can read; simple patterns
  (rings, spirals, aimed bursts, slams that leave something on the floor); a short wind-up before each; a closed room (a lair).
- The dodge is the tool: Space dives with i-frames on stamina; bullet time is its own key (V, drains Focus) - RT 9.
- **First one - the brain in the vat** (Yan: "a mutant brain and eyes inside a vat of liquid inside a lab? his attacks hit world
  objects like desks and stuff throwing, pulling, rearranging them into stuff you have to dodge while dodging his attacks"):
  a psychic mutant in a Tier 2-3 lab. Void orbs in rings and spirals (the `zone/void-pull` look); the big move opens a full-size
  void pull that drags you toward him while orbs fly. Telekinesis on the room's props (`Props.cs`): desks, crates and barrels
  thrown at you, pulled across the floor, set up as walls and lanes you have to weave through.
- Where and how (Yan, 2026-11-03: "sure" / "sounds good"): Secret Lab Alpha (Tier 2, hidden off the Industrial Basin). You crack
  the vat glass in a few places; the liquid drains in phases, each angrier; in the last phase the brain lies bare and takes damage.
- **It comes back** (Yan: "when it drains completely it sits still for a few moments, then its eyes open again and it uses a
  portal/black hole thingy to teleport away, then we fight it again with enhanced mechanics in another place"): beaten, the
  brain goes still, the eyes open, a void pull swallows it and it's gone. A later lab holds the rematch with new tricks.
  The first fight ends with the escape, not a kill.
- Art first: the vat, the brain and the eyes drawn in our style and shown to Yan as a real render before any fight code.
- Not built yet.

**Second one - the eel of Sub-Station Depths (Yan, 2026-11-04; not built, name to pick):** a very big electric eel mutant lurking in
the flooded levels under the Depths' basin.
- **Levels are separate maps** (Yan: "you go down the stairs and it loads a diferent map that is the lower level, and again after the
  water lowers again"), linked by stairs like the Flats' floors. The drains are keys, so they stay drained (dying never refills them).
- **Level 1:** raised platforms over deep water; the eel swims below (its shadow) until you near the pump, then it **attacks**: coils burst
  up round your platform (Yan: "kind of jump-scare style stuff, where it's actually attacking the player"). The burst hits; a quick
  dive gets through it. Caught inside, you shoot the coils until it lets go; **touching it electrocutes you**; the ring tightens (a clock).
- **Level 2:** the same, but it strikes at several set points of the search for the pump, varied (wrap, tail sweep, surge wave, spit).
- **Fair scare rule:** never a real strike without its tell - nearby lamps flicker, buzz and dim as it draws power, then silence. On
  Level 2 some flickers are **false alarms**.
- **Level 3:** the water is so low it has to rise half out of it, like a snake about to strike; it blocks the last pump. **The boss fight**,
  in the open. Put it down, run the pump, the lab drains completely.
- **It's big** (Yan: "yes, its pretty big"): with more players it can wrap two platforms at once, or go for whoever is nearest the pump.
- **Damage carries over** from Levels 1-2, **capped at 20% of its max HP** (Yan: "it can, but not much"); shown as scorched, torn coils.
- **The reward** (Yan: "every boss needs a reward"): special eel parts that sell for a good profit (above a normal mutant part, which is
  400-600 RU at Tier 3 in `MutantLootSystem`), a chest of random loot of the dungeon's tier (3), and **one** armor or weapon a tier above (4).
- **Sump fight redesign (Yan, 2026-11-05) - proposed details marked (P), not built (art, map and data only):**
  - **Damage marks** like the brain's phases: the sprite gets wounds at **75 / 50 / 25 % HP** (stepped; stage 1 gashes and a torn fin, 2 flesh and
    blood on the flank, 3 open wounds, bone, drips; head: scratches, cracked then bloodshot eye). Art: overlay strips `boss_eel_<anim>_d1..d3`
    drawn over the base strip (same frames; the one of the stage reached holds every earlier wound); listed under `damage` in `boss_eel_anims.json`.
  - **Attacks, like the Hydra in God of War 1 (Yan):** *snap* (a short fast bite at the player), *smash* when enraged (chained slams; **P**: enrage from
    50 % HP, three slams in a row with a short gap), *circular sweep* (the head swings round the pool in an arc, the neck trailing: a flail you
    dive or vault over). Plus the lunge and slam already drawn. Strips: `snap`, `slam`, `lunge`, `sweep`.
  - **Reveal:** a body lies on a small slab on the pool's north rim (plan digit `8`). The eel rises out of the pool, swoops at the body, bites
    it, gulps it down its neck, settles into the rear-up pose and the fight starts (`reveal`; the json gives the mouth's position in every frame and the swallow frame).
  - **Death:** it isn't killed. It **vomits up the same body** (`vomit`, eject frame in the json) and **sinks back down the pool** (`retreat`) - "we'll
    fight it again later in another place" (**P**: the next fight is the Level-3 deep dungeon / a later map; it keeps its wounds).
  - **The body** = the loot: a dead random human NPC (dead sprite) in **tier-4 armor**, with random loot like a stash (`HumanLootSystem` corpse
    stash, tier 3-4) and **a key tied to the story** (**P**: the Sealed Vault key of Secret Lab Gamma, main-story act 3 - `StorySystem` has no lock there yet;
    or an OSEI quest item). **No other stashes in the Sump** (removed). Levels 1-2 dead-end stashes stay unless Yan says otherwise.
  - **Open:** do eel parts still drop (design above) or is the body the whole reward?
- Art first: the eel lurking (shadow), wrapped round a platform, and half out of the water, plus the three water levels, as real renders.
- **The eel's animation frames (2026-11-05, `ArtSource/bosses/eel_anim.py`, strips `Assets/Resources/Sprites/Fx/boss_eel_*.png` + `boss_eel_anims.json`)**,
  all bent from his own pixels, facing left as drawn (the game flips for right), 15 fps, 32 px a tile: `rear_idle` (loop, half out of the water, swaying),
  `lunge` (wind-up, the head darts left, hold, recover), `slam` (rears and crashes down in front), `hurt` (a white flash and a recoil), `die` (the head
  drops, the coil slackens, it goes dark), `wrap_back` / `wrap_front` (burst, a hold that loops with electricity along the coils, release: round a 3x3
  platform, split at the platform's row so the game draws the platform between them), `swim` and `swim_shadow` (a travelling wave, the lurking one).
  The json holds each strip's frames, fps, loop, frame size and anchor (the pivot: the pool's centre, the platform's centre, the head). A lunge toward
  up or down isn't baked: the fight code lines the eel up, or uses the slam. No fight code yet.
- **The Sump, the arena (2026-11-05, built; `sub_station_depths_3.mmd`, `lab_plans.py depths_level3`)**: one wide room of knee-deep water (slow), 60x44;
  the eel lies in a deep pool (102 tiles, centre marked with plan digit 0) at its south end; the only way to the last pump is the 5-wide channel just
  south of the pool along a 2-tile shore its neck reaches (digit 9 = the channel mouth, 3 tiles from the pool): its coil blocks the way, you put it down
  first. Tools for the player: 7 pillars as cover from its lunges (4-12 tiles from the pool), two dry patches inside its reach (10.5 tiles) to stand on,
  two far ones outside it, safe, each with a stash; red lamps for the tell. The pump is 14 tiles from the pool; the bottom (the reward), a stash and the
  service lift up lie past a deep band the last pump drains. The eel itself isn't a lock in the graph (`{item:eel_down}` stands for the fight).
- **The eel's art is Yan's own drawing** (2026-11-05, `ArtSource/refs/eel/yan_eel_reference.jpg`: "a higher quality image of the eel boss, just
  like the ones we used for the mutants"): moray head with a blind white eye and spines, a long dorsal fin, mottled green with gold flecks.
  Recovered to the pixel grid it was drawn on (480x268 art pixels, one per game pixel, 27 colours: `eel_native.py`) and bent along any path
  from his own pixels (`eel_poses.py`: coiled, rearing, lurking); about 34 tiles of body from his one S, as long as a pose needs by repeating
  the trunk. At 32 px a tile its body is about 1.7 tiles thick (his drawing is 15 tiles across). Not in the game yet.
- **Maps first, then the fight (Yan: "We need the Maps before the fight, otherwise well have to rework it later").** Built 2026-11-04:
  `sub_station_depths_1` Cistern, `_2` Lower Cistern, `_3` Sump (plans in `lab_plans.py`, graph `sub_station_depths.mmd`). Each level's
  deep water answers to its own pump (`LabLocks`: drain1 needs the power, drain2 needs drain1, drain3 needs drain2); the station's pump
  room became the stairwell (behind its own power door) and its basin drains with the last pump; a service lift links the bottom and
  the station's basin. Not yet: the eel gate on the last pump (it just works until the fight exists), the reward chest and tier-4 item,
  stair art on the exits. Render: `maps/renders/dungeon/sub_station_depths_levels.jpg`.

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
- **Yan's co-op rules (2026-10-20):**
  - **Quests are shared:** when anyone accepts a job, everyone has it. Rewards are given to each player separately - money,
    reputation, and items (a quest that rewards a weapon gives everyone one).
  - **Saves (Terraria/Borderlands):** the host saves the world; each player owns their character and can join other worlds or
    play their own. The host's saves also save the other players' characters automatically. **Several save slots**, so a
    player can keep a solo character and a co-op one.
  - **Talking and trading don't pause anything:** the world keeps going, for you too.
  - **Downed players are revived** by a friend holding E over them for a few seconds. **Game over only when everyone is dead**;
    then the host's last save loads.
  - **Loot on bodies: first come, first served.**
  - **Three death states (co-op):** downed (crawl, revive with E); dead (rank XP loss, one equipped item wears a step,
    revivable with E out of combat); DEAD DEAD when the squad wipes - everyone back at the last camp (E at a bonfire; Bonfire
    Village by default), armor one step worse, half the pack dropped where they fell (hostile passers-by loot it - kill them to
    get it back), rank XP loss. Dying never costs a rank; XP stops at 0. (RT 7q-7r, `Death.cs`; rank XP in `Player.Progression`.)
- **Ranks (Yan):** rank XP from milestones, jobs and kills. Master and Legend don't need the main story but are a long road
  (2500 / 6000 XP; dungeons cleared, storylines finished, Named kills, 40 quests) - the game should hold a lot of playtime.
- The first prototype is single-player, but the combat code is written host-authoritative from day one.
- **Groundwork done (RT 7n, 2026-10-20; it slipped through RT 1-7m, Yan caught it):** each player's real-time state is its own
  (`Rt.State`, `Rt.Of(p)`); controls arrive as `Rt.Input` packets (the local player's from `Rt.Read`, joined players' from
  `Rt.Remote`); `Player.All` / `Player.Join`; rounds and melee can hit any player; each enemy fights the nearest player
  (`Rt.TargetOf`); every player gets the round's upkeep; fair shots use each player's own sight. **Rule from now on: new code
  works for several players, or the batch says why not.**
- **Networking test (RT 7y, 2026-10-21):** Netcode for GameObjects 2.13.3 + Multiplayer Services 2.3.3 (a session with a join
  code over the relay) + Multiplayer Play Mode 2.0.2. Main menu > Co-op: Host (your last character) or Join (type the code). The
  host runs everything; a client sends its Input every frame and gets a snapshot 20 times a second. **Players only so far**:
  enemies, shots, items, quests, characters travelling with their owner, the exit vote and saving the friends' characters come next.
- **Saves (RT 7x):** a character file and a world file per slot, New Game names the character and saves at once.
- Still one-player (to do with the network layer): bullet time is one time scale for everyone (step 4: per fight); spotting
  memory (`Contact`) is shared; morale/radio and reputation read the host; the HUD, vision and camera are the local player's;
  a joined player's exit is ignored until the "everyone at the exit" vote; game over is the host's death; friendly fire is on
  was on until RT 7o; now off for players (Yan).

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

- **Real lighting (Yan, 2026-11-04):** "Eventually i'd like real lighting in the game with shadows reacting to light sources,
  similar to how octopath traveler looks, instead of flat lighting we have today." Today: the time-of-day wash and the
  darkness/flashlight texture in `AtmosphereView`, `LightMap` for lamps (they change how bright a tile is, nothing casts a shadow
  from them), and the sun's baked shadows in `WorldArt`. To look into: URP 2D `Light2D` point/spot lights on the scene's Global Light 2D
  with shadow casters on walls and props, normal maps for the sprites (we draw in Python: derive them from a height pass), plus the
  HD-2D finish (bloom, a soft depth-of-field band, warm/cool light colours). Needs a test render on one map before committing to it.
- **Dungeons you figure out, not corridors with rooms (Yan, 2026-11-03):** "right now it looks very even in space, it doesn't have
  a linear progression ... Most rooms are locked, the few that are open have either a key, a hole in the wall going into the other
  room, maybe something else? recently i played scavland and its 'bunkers' are basically just a loooot of corridors with rooms
  branching off of them ... 20 rooms that look the same and corridors, i'd like to avoid this here and have them be engaging to
  explore and figure out". Ideas to bring him: a locked spine where each opened room gives the way into the next (key cards,
  a code on a note, a breakable wall, a vent you crawl, power you restore to a door, a flooded room you drain), rooms with one job
  each (a lab, a cell block, a generator, an office with the code) so they look and play different, shortcuts back to the
  entrance once opened, and the boss room last. Start with Secret Lab Alpha (the brain's lab).
  **Secret Lab Alpha done (2026-11-04)**: see WORK_LOG "Secret Lab Alpha redesigned" - the lock types (staff badge, power), the hole, the shortcut loops,
  the boss room last, checked with the level-design skill's graph_check. The other dungeons are still corridors-with-rooms.
  **To do (Yan, 2026-11-04):** redo the rest of the dungeons the same way (lock-and-key spine, rooms with one job each), after Alpha.
  **Lock types (one look each, everywhere):** staff badge (blue card-reader doors), power (dead hazard-striped blast doors; a switch
  that may need a fuse), and **deep water (Yan, 2026-11-04, a new lock type): a flooded room you drain** - black water over your head
  blocks everyone until the dungeon's pump panel runs (it needs the power), then it's knee-deep water you wade through (`LabLocks.cs`,
  plan letter `W`). Keys are per dungeon: one lab's badge never opens another's doors.
  **Sub-Station Depths done (2026-11-04, waiting on Yan's review)**: mission graph first (`ArtSource/plans/sub_station_depths.mmd`,
  checked with `graph_check.py`), then the plan (`lab_plans.py depths_plan`), which the generator proves matches the graph room by
  room. Render: `maps/renders/dungeon/sub_station_depths_before_after.jpg`. The other dungeons wait for Yan's go.
- **To do - Unity lighting check (Yan, 2026-11-04):** play the new in-game lighting and report: night too bright or too flat? does F7 (lights on/off) show an obvious difference? Cost per frame not measured yet.
- **Poses (Yan, 2026-10-29):** extra body frames besides front/back (crouch, kneel, sit, leans for cover, lunge, stagger,
  lying, with in-betweens), the floating hands doing the expressive part. Prototype sheet: `maps/renders/poses/poses_preview.png`
  (`ArtSource/make_poses.py`, any suit). Waiting on Yan's picks before anything goes in the game.
- **Redraw every gun the melee way (Yan, 2026-10-20):** "i was looking at some of them in the files and they look weird,
  especially the ones without very popular designs". The guns came from `ArtSource/weapons/batch_all.py`, which shrinks a
  reference photo into each size separately; the well-known silhouettes survive, the rest turn to mush. Redo each from a clean
  pixel-art reference, drawn at its in-hand size with details placed by hand, the menu version made from the same drawing
  (method: **`ArtSource/weapons/hand/PIXEL_GUIDE.md`**, as the melee set). Keep the parts the animations need: grip/support points (`WeaponArt.Grips`), magazine cuts
  (`mags.py`), break-action barrels and revolver cylinders. After the melee weapons.
- **Character creator (Yan, 2026-10-20):** new games starting in different factions with their own loadouts, and a storyline per
  faction (like Anomaly's faction starts).
- **Starting area rework (Yan, 2026-10-20; waiting on his map and quest spots):** Bonfire Village as a real Safe Zone in the
  Cordon rookie-village spirit - the starting faction's boundary and perimeter that hostile squads (mostly Raiders) can't or
  very rarely cross (and then fight the guards), guards, random friendly squads, a trader, a medic, a small bar and barman.
  Tutorial: the trader's job (recover something from a dead courier, a fixed pack of 2-3 Blind Maws) and the barman's job (clear
  a nearby spot held by a fixed 3-man Raider squad, laid out so stealth works - teaches sneaking, takedowns). Plus: name your
  character, and a save made at character creation. Renders: `maps/renders/start_area/`.
- A real run cycle for the hounds (Batch 131f): the knee-rig walk looks wrong at running speed.
- Shotguns: 3 full patterns to drop a mid-armor target at 5 tiles (2-4 s with a pump) vs ~1 s for rifles - look at it after playing.
- SMGs still need 14-15 chest hits on the heaviest tier 3 armor (Wasp-9, Hornet): their answer is aiming for the head or AP.
