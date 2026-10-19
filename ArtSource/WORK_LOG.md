# Overnight work log

Work done while Yan slept. Each batch was compile-checked (Roslyn against Unity stubs) and run
through the runtime test harness in `Tools/compile_check/` before being copied into the project.
The original version of every changed file is kept in `Backups/<batch>/`, in case something
needs rolling back (Plastic SCM works too).

## Read this first (morning summary)

**13 batches went into the project overnight.** Each one compiled against Unity stubs and passed
the test harness before I copied it in. The test harness does unit checks, fight sims, a full
Main Story walkthrough and a 400-step random playthrough. **Unity itself hasn't compiled any of
it yet:** the editor was idle all night. So the first thing to do is focus Unity, let it
recompile, and check the Console. If something red shows up, every original file is in
`Backups/<batch>/`.

**What's new, in one breath:**

1. **Maps and terrain:**
   - Real maps for all 29 zones, composed from a tile atlas.
   - Destructible cover, doors and explosive barrels.
2. **Combat by the book (file 00/01):**
   - Bursts, jams, Condition, wounds.
   - Cover as AC, grenades.
3. **Enemy AI (file 04):** combat roles, morale, surrender, mutant behaviours.
4. **Economy (files 05/07/08):**
   - Loot tables, trader stock, sell-back and repairs.
   - Technician upgrades.
5. **The living world (file 10):**
   - Days, zone events, Emissions.
   - Faction goal tracks, rumors, named NPCs.
   - `W` opens the World panel.
6. **Artifacts:**
   - Searchable anomalies (`F` next to a revealed one).
   - Family stacking and DR penalties that actually apply.
   - The legendaries in their fixed places.
7. **Progression:**
   - Character creation (stats, loadout, perk).
   - Rank milestones, and all 34 perks working.
   - `P` opens the character sheet.
8. **The Main Story (file 09):** all 5 acts, sponsors, 5 endings, and the post-ending Named
   mutants.
9. **Quests (file 09):**
   - A d6 complication on every quest.
   - Two ways to finish each one.
   - True rumors as rewards.
   - Prisoner interrogation and release.

**New keys:**

| Key | Does |
|-|-|
| `W` | The Zone (world panel) |
| `P` | Character sheet and perk picks |
| `F` | Search the anomaly you're next to |
| `E` | Interact, including story landmarks |
| `S` | Suppressive Fire (perk) |
| `H` | Run and Gun (perk) |
| `Z` | Rest |
| `Q` | Switch ammo type |
| `X` | Single or burst fire |
| `A` / `C` / `V` | Aim, take cover, evasive |
| `G` | Ready a grenade |
| `T` | Intimidate |

**Design questions (answered by Yan in Batch 22):**

1. **Neutral factions don't attack on sight.** Only Hostile ones do. Raiders and Obelisk start at
   -3 (Hostile) so there is still someone shooting at you early on.
2. **Starting Ru: 300**, plus the loadout's Ru on top (`GameConstants.StartingRu`).
3. **Survival drains per zone crossing only**, not per tile walked. Healing (medkits, stims) also
   makes you hungrier, thirstier and drowsier; the rate is a placeholder until Yan picks one
   (`GameConstants.SurvivalCostPer10Hp`).
4. **Still open:** where the Alpha Access Keycard comes from. For now it's on a dead guard at
   Lab Alpha's Reception Airlock.

---

## Batch 1: maps, tiles and destructible cover

### New tile types
`GameCore/TerrainTile.cs` now covers the whole legend from the design docs:

| Doc | TileType | Rules |
|-|-|-|
| `#` | Solid | blocks movement and line of sight |
| `C` | ScatterCover | light cover, +1 AC, Integrity 3 |
| `=` | HardCover | hard cover, +2 AC, Integrity 6 |
| `~` | Difficult | 2 MP per tile, no cover (rubble, mud, wading water) |
| `D` | Door | closed: blocks LoS. Walking into it opens it (+1 MP). Shooting it down (Integrity 3) leaves it open for good |
| | DoorOpen | open or broken door |
| | Barrel | explosive barrel: light cover you can't stand on, blows up when hit |

### Cover works the way file 00 says
- Cover now raises **AC** (+1 light, +2 hard) instead of the old "+20% Phys-DR".
- It only counts if the firing line crosses a cover tile next to the target.
- Standing *on* cover protects from every side, except against shooters within 2 tiles.
- **Flanked:** the target is next to cover but the shot goes around it, so the shooter gets +2 to hit.
- No cover against melee weapons, or against an adjacent shot with Point-Blank Execution.

The log says which applied, e.g. "Raider is in hard cover: +2 AC" or "Raider is flanked: +2 to hit".

### Destruction (Cover Integrity, file 00)
- A shot that misses a covered target by 1-3 hits the cover instead: −1 Integrity, or −2 for a
  burst or a shotgun.
- At 0 the cover turns to rubble, which is difficult terrain and gives no cover.
- **Right-click** shoots the map: a barrel, a door, or the cover someone is hiding behind.
  Objects are AC 8, and a hit takes 1 Integrity (2 for a shotgun).
- **Explosive barrels** (`Explosions.cs`) hit everyone within 1 tile for 40 physical damage.
  Phys-DR applies; cover and Anom-DR don't. Each target makes an Agility DC 13 roll: success
  means half damage but knocked prone.
  - The blast destroys light cover, doors and other barrels (they chain), and takes 3 off hard cover.
  - These are the frag grenade rules from file 01, so grenades can reuse this code.
- NPCs know about barrels: if one sits next to you and they can shoot it without catching
  themselves in the blast, they will.
- Damage now persists within a play session: a door you kicked open or a wall of sandbags you shot
  to pieces is still that way when you come back to the zone. (It used to only persist from the
  second visit onward, due to a reference bug.)

### The map is drawn with real tile art now
- `GameCore/TerrainArt.cs` paints the whole zone into one texture: ground, drop shadows, walls,
  cover, doors, props, cracks on damaged cover, and elevation shading.
- When something changes, only that spot is redrawn.
  - Before, the map was one flat-colored GameObject per tile, which meant thousands of objects.
- The tileset has 125 sprites in the locked 16x16 style:
  - Grounds: grass, dirt, mud, asphalt, concrete, planks, lab tile, sand, gravel, water, cave floor.
  - Walls: brick, plaster, concrete, cliff, ship hull, foliage, scrap heaps.
  - Big objects: trees, red trees, dead trees, rocks, containers, wrecked cars, machinery,
    pipes, antenna masts.
  - Light cover: bushes, crates, chain-link fence, tires, debris, reeds, logs, lockers.
  - Hard cover: sandbags, jersey barriers, concrete blocks, engine blocks, trench.
  - Rubble, doors (wood/metal, open, broken), the explosive barrel, and props (campfire,
    armchair, desk, barrier pole, culvert, hatch, bones, rails, road lines).
  - Sources: `ArtSource/make_terrain_tiles.py` and `ArtSource/terrain_sheet.png` (preview).
- The atlas ships as `Resources/Sprites/terrain_atlas.bytes` + `terrain_names.txt`. It is loaded
  as raw bytes, so it never depends on texture import settings.
- `GameCore/ZoneThemes.cs` gives every zone its own floor mix and default look for walls, cover
  and hard cover. For example, the Scrapyard gets junk heaps, wrecks and tires; the Marshes get
  mud, water, reeds and dead trees.

### Every zone rebuilt (`ZoneLayoutRecipes.cs`)
- All 29 zones follow the zone map doc: the sketch's north-to-south order, the compass-tagged
  exits, and the doc's exact **room-grids** stamped in at 2-4x scale.
  - `RoomGrids.cs` is generated from `06_Zone_Map.md` by `ArtSource/make_room_grids.py`.
  - Stamped room-grids: The Maze, Central Plant, Container Stacks, Roundhouse, Sub-Station
    House, Barracks Row, Main Street, Row Houses, The Church, Town Hall Basement, The Hulks,
    Relay Station, Highrise Row, Garrison Barricade, The Trenches, Containment Hall,
    Cooling Tunnels, and all ten hidden sub-nodes.
- **The Perimeter now matches the doc.** The checkpoint is at the south (guard hut with a door,
  desk and lockers, barrier pole, jersey blocks). Watchtower 3 has a ladder, the Rust Culvert
  sits behind brush, and the Bonfire Camp has Yefim's container. A chain-link fence runs across
  with a torn section. Exits are NE and E, as the doc says.
- **The Marshes** are wading water off a planked causeway. Before, the whole map was "scatter
  cover".
- **The Siege Ward trenches** are hard cover you can walk along.
- **Scavenger's Hub** has an actual layout now: bazaar container rows, the Rusty Anchor, the
  repair yard and the Sentinel gate.
- **Landmarks sit where they belong.** Before, they were scattered at random; now the watchtower
  marker is on the watchtower.
- **Exits sit where the doc's compass tags put them.** The hidden routes (Rust Culvert, Loading
  Dock hatch, ...) open at the landmark itself instead of at the map edge.
- **Elevation is reachable everywhere.** Towers, ridges, quarry terraces and crane rigs have
  ramps, and climbing is never more than 2 levels per step. Before, several high spots
  couldn't be reached at all, and the quarry's tiers were sealed off from each other.
- **Tested:** in every zone, every exit and landmark can be walked to from the first exit, and
  no tile is a one-way drop you can't climb back out of.

### Smaller fixes
- Removed the "X moves through scatter cover for 2 MP" log line. It was 70% of the log.
- Vault now makes cover cost 1 MP instead of 0.

### Things to look at in Unity
- Walk around The Perimeter and the Scrapyard. Right-click a red barrel near a raider.
- If the map shows up as flat gray blocks instead of art, `terrain_atlas.bytes` or
  `terrain_names.txt` didn't import into `Assets/Resources/Sprites/`.

---

## Batch 2: combat math from the design docs

All of it lives in `GameCore/CombatRules.cs`, so shooting, mutant attacks, blasts and the AI use
the same numbers. `Weapon.Fire` and `Mutant.Attack` were rebuilt on top of it.

### Bugs this batch fixes (each was hurting the game)
- **Enemies only shot in their first round.** NPC AP was never refreshed, so after one volley
  every human enemy stood there with 0 AP for the rest of the fight.
  - They now refresh AP every turn. **Fights will be much harder.** That's the intended
    difficulty: the log you sent showed the raiders firing twice per fight, total.
- **Armor MP and the heavy-weapon MP cap were ignored.** Every equip reset you to 4 MP, because
  the code read two fields that are never assigned.
  - MP now follows the doc: armor MP, +1 at Expert, a cap of 2 with heavy weapons, artifacts, mods.
  - NPCs use their armor's MP too, instead of the unarmored rank number.
- **Your weapon and armor weighed nothing** (same dead fields). Now they count toward carry weight.
- **Armor did nothing against mutants.** Claws went straight to HP. Now Phys-DR reduces them.
  - Psi blasts (Mind and Void Weaver) use Anom-DR instead, and cover counts against them.
- **Anom-DR was reducing bullet damage.** The doc says it never applies to bullets or melee.
- **Every mutant had a 25% chance to make you bleed** (a fallback default). Now only the Veil and
  Alpha Stalkers do, at the doc's 16+ and 13+ on a d20.
- **No way to clear a jam.** `R` now clears it (1 AP, free with Gunsmith), then reloads.

### Rules now as written in files 00, 01 and 04
**Burst fire (`X` toggles it):**
- Assault rifles, SMGs and heavy weapons only.
- One d20 at a flat penalty: assault rifles −3 (AK-74 −2, FN FAL −4), SMG −1, heavy −3.
- Uses 3, 5 or 10 rounds and does ×2.0, ×2.4 or ×3.0 damage.
- Costs 3 AP; SMGs and the AN-94 cost 2.
- Recoil Control removes the assault-rifle penalty.
- Before, burst rolled at disadvantage, used a third of the magazine and did ×1.5.

**Hits, jams and wear:**
- **Natural 20:** automatic hit, double damage before ammo and armor.
- **Jams:** a natural 1 always jams.
  - A burst also jams on a 2, except the AK-103 and PKP Pecheneg.
  - A Dirty or Damaged weapon also jams on a 2, and the IL86 does even when Working.
  - Every jam drops the gun one Condition step: Working → Dirty → Damaged → **Broken**, and
    Broken can't fire. The AK-103 never wears.
  - A Damaged gun does −25% damage.

**Ammo and armor:**
- **Ammo against total Phys-DR:**
  - Hollow Point does ×1.25 against 0% DR, no change at 1-19%, and ×0.5 at 20% or more.
    Before, it halved damage against any armor at all.
  - Armor Piercing multiplies DR by 0.4. Dart multiplies it by 0.5.
  - After that, the weapon's "ignores X%" applies.
- **Phys-DR and Anom-DR are capped at 75%.**

**Wounds and armor wear:**
- A single hit of 20+ after DR (15+ with the AKM), or any natural-20 hit on a human, rolls a
  wound: Arm −2 to hit, Leg −2 MP, Torso bleeding, Head −1 AP next turn.
- The same trigger wears the target's armor down one step: Damaged −10% DR, Broken −25%.
- A Bandage treats one wound.

**Accuracy:**
- **AC = 10 + rank modifier** for the player and NPCs, so Expert is AC 12.
- The player adds the rank modifier to attack rolls too. Max AP follows rank: Professional and
  above get 5.
- **Weapon traits:**
  - High Recoil −1.
  - Shotguns +2 within 4 tiles.
  - TRs-301 +1 on single shots.
  - IL86 +1 at 10+ tiles.
  - OBELISK, F2000 and Zulu +1.
  - G36K: Aim gives +3.
  - Chiappa Rhino: shots cost 2, 1, 1 AP.
  - Serrated Trench Knife: bleed on a 17+.
- **Suppressed:** a burst that misses you by 3 or less gives −2 to your next attacks.

### New actions
- `X` single/burst · `A` Aim (1 AP, +2 on your next shot) · `C` Take Cover (2 AP, +2 AC on top of
  the cover) · `V` Evasive Maneuvers (2 AP, +2 AC against melee) · `R` clear jam / reload.
- **Grenades** (`G` cycles frag → smoke → flashbang, then click a tile):
  - 2 AP, range 8, needs line of sight.
  - d20 + Agility vs DC 12; a miss scatters the grenade 1d4 tiles.
  - **Frag:** 40 damage in a 3x3 blast, with the barrel rules from batch 1.
  - **Smoke:** blocks LoS for 2 turns, drawn as a grey cloud.
  - **Flashbang:** Agility DC 13 or −2 AP.
  - All three are on sale at traders at the doc's prices (250, 150, 200).
- **Stagger:** a non-berserk mutant that loses 40% of its HP in one player turn loses its next turn.
- **Bleeding works on NPCs and mutants too.** Before, only the player could bleed.
- The status panel shows AC, AP and MP maximums, and the weapon line: ammo, fire mode,
  condition, and JAMMED.

### Data synced to the docs
- **Weapons:** 18 stat fixes; every weapon now has its weight; shotgun ranges are 4-6 as the doc
  says (they were 6-8). Added the Kizlyar Machete, Serrated Trench Knife and Monolith Ritual Blade.
  The script is `ArtSource/weapons_sync.py`.
- **Mutants:**
  - Attack damage is back to the doc's numbers: Veil 20, Alpha 30, Mind Weaver 35, Brute 60,
    Dread 45, Void 50. The old lowered numbers were compensating for armor being ignored.
  - Panic thresholds follow the doc.
  - The Brute's Phys-DR is 40%.
- **Sprinter** is +2 MP, per the doc.

### Tested
- A 30-fight simulation (rookie with a PMm against 2 raiders and a Blind Maw) ran through the
  real turn loop with no errors.
- The rookie loses every time. That's expected with doc-accurate enemies that shoot back every
  turn, but worth watching in play.

---

## Batch 3: enemy AI, morale and mutant behaviors (file 04)

Enemy turns now run through `GameCore/EnemyAI.cs`. TurnLoop still handles who has spotted you.

### Human squads
- **Morale.** Everyone starts Steady.
  - **What shakes them:** dropping to their rank's Shaken HP %, seeing an ally die, losing their
    Leader, or your Intimidate.
  - **Shaken:** −2 to hit. They stop advancing and fall back to cover.
  - **Broken** (at their rank's Broken HP %, or Intimidated while Shaken): d20 1-10 they flee
    (your free parting shot and drop-and-run still apply), 11-20 they **surrender**.
  - Legends never shake and never surrender.
- **Surrendered NPCs** drop their gear into a lootable pack and stand there with their hands up,
  tinted blue. **Left-click** takes their gear. Right-click shoots them.
- **Combat Roles from their weapon:**
  - **Anchor** (assault rifle, heavy, sniper): holds cover at range, fires bursts, aims with
    spare AP, and moves when its cover is nearly shot through.
  - **Rusher** (shotgun, SMG): closes in cover to cover and looks for flanks.
  - **Flanker** (sidearm, melee): moves wide to get around your cover for the +2.
  - **Leader** (the highest rank in the squad): stays in good cover, **Commands** one ally each
    round (+2 on their next shot) and **Rallies** a shaken ally.
- Each turn an NPC scores every tile it can reach (cover against you, line of sight, range,
  flanking) and walks to the best one before shooting.
- **NPC grenades:** Professional+ carry a frag. They throw it if you sit on the same cover tile
  for two enemy rounds, and it drops as loot if unused.
- NPCs shoot explosive barrels next to you (batch 1), clear their own jams, and get up when
  knocked prone.

### Mutants
- **Pack and swarm** close in on tiles next to a packmate, for Pack Tactics advantage.
- **Ambushers** (Veil and Alpha Stalker, Void Weaver) stay cloaked 4+ tiles out until they can
  reach you, strike, then slip away and **re-cloak**. Before, they stayed visible forever after
  their first attack.
- **Ranged** (Mind and Void Weaver) keep 3-5 tiles away with line of sight and psi-blast from there.
- **Dread Stalker** circles at 3-5 tiles, then **Leaps**: 2 AP plus all its MP, landing next to you
  with advantage.
  - If you have an AP left, you automatically try to dive aside: Agility vs 16.
  - Success means the leap misses and your next attack on it has advantage.
- **Blight Brute** does **Ground Slam** once per fight: 30 damage to everyone adjacent, Agility
  DC 13 or prone.
- A mutant attacks at most once per turn (2 AP). Staggered mutants lose their turn.

### Smaller changes
- **Units can't stand on the same tile any more.** Pathfinding routes around other units.
- **New action:** `T` Intimidate the nearest human enemy (2 AP).
- **Corpses stay on the map after you win**, so you can still loot them. Before, the "area
  secured" step wiped the enemy list, and with it every corpse.
- **When a fight ends:** it ends once nobody is left fighting (dead, fled or surrendered).
  Anyone who ran is gone.

---

## Batch 4: economy, traders, loot, repairs and Days (files 05, 07, 08, 10 §1)

`GameCore/Economy.cs` has every price band and pool from file 07, the d100 loot/trader table,
sell-back rates and repair costs.

### Traders
- **Rolled stock never existed in-game.** `RolledStockSystem` was written but never called. Before
  this, every trader just sold every weapon and armor up to its tier.
- **Layer 1, the baseline (file 05),** is what every trader always has at fixed prices:
  - Basic Medkit 100, Basic Stimpak 120, Bandage 40, Antirad 60, MRE 50, Energy Drink 40.
  - Ammo boxes: Standard 150, Hollow Point 250, Dart 400, AP 600. Rounds per box depend on the
    weapon you have out: sidearm 18, SMG 30, shotgun 8, rifle 15, sniper 5, heavy 25.
  - Frag 250, smoke 150, flashbang 200.
  - **Field Repair Kit** 300 and Small Canvas Sack 80.
- **Layer 2, today's stock (file 07):**
  - Established traders get 8-12 rolls on their tier's table; wandering traders get 4-6.
  - The table follows the doc: category map, cash and junk rolls, lucky finds on the next tier up.
  - Traders never sell Tier 5 weapons.
  - Prices are rolled inside each item's band.
  - Stock restocks each new **Day**.
- **Selling** now uses the doc's sell-back rates: weapons 40%, armor 35%, artifacts 45%, detectors
  30%, mods 25%, consumables 20%, then the condition modifier (Broken ×0.2).
  - Before, you sold everything at 100% of its buy price, which was a money printer.
- **Technician** (the Hub's Repair Yard, or any faction quartermaster): repair rows appear in the
  shop. Cost is 10% of the item's band midpoint per Condition step.

### Loot
- **NPC extra loot and containers** roll on the real file 07 table, with Tier Drift and the
  "Tier 5 weapons are rare" rule.
- **Dropped weapons** carry their 1d4+1 rounds.
- **New container: Footlocker.** Force or pick it (Strength or Agility, DC 10) for 3-6 rolls.
- **Electronic Lock:** 180 Ru plus one roll at Tier 3 or higher, rerolling junk and cash, as in
  file 05.

### Days (file 10 §1)
- A new Day starts after **3 zone crossings** or a **rest**.
- **`Z` rests** in a Safe Zone or next to a campfire: Tiredness goes back to 10, and it can't be
  done with enemies hunting you.
- Traders restock each Day. Zone Events and Faction Goals hook into the same spot (next batch).

### Inventory
- **`Q` switches ammo type** (Standard → HP → Dart → AP). Before, special ammo could be bought but
  never loaded.
- **New inventory buttons** for things you previously couldn't use from the inventory: **Belt**
  (artifacts), **Fit** (armor mods into your armor's mod slots), **Wear** (backpacks).
- **Field Repair Kit:** out of combat, +1 Condition step on your weapon (or armor), never above Dirty.
- **Base carry capacity is 25 kg** as in the doc (it was 40). Pack Mule makes it 50.

### Bug fixes
- **Skill checks with full stat names were at −5.** Data writes stats both as "A" and as
  "Agility"; full names read as a stat of 0. That made hidden routes (Perception DC 14+) and
  objects like the Encrypted PDA nearly impossible. Two-stat checks now use your better stat.
- **The shop's "Medkit"** (an item id nothing recognised) is now the real Basic Medkit.

## Batch 5: the living world (file 10) and artifact hunting (file 03)

### Artifacts can finally be found
- **Before:** the game "detected" artifacts in anomalies and logged it, but nothing ever gave you
  one. Artifacts could only be bought.
- **Now:** once an anomaly is revealed, stand on it or next to it and press **`F`** to search it.
  This follows file 03:
  - The search costs 1 action (2 AP).
  - The roll is d20 + Perception + detector bonus vs DC 15.
  - On a success you get an artifact from the zone tier's pool, with a 25% chance it's one tier
    lower.
  - Each anomaly can only be searched once.
- **Passing the avoidance roll now reveals the anomaly too.** Before, only a failed roll did.
  Per file 03, you resolve the anomaly either way.
- **The Svarog detector** reveals anomalies within 6 tiles on its own, and tells you when one is
  worth searching. Its "passive reveal" flag existed in the data but was never used.
- Away from anomalies, `F` still searches the area for hidden routes, as before.

### Zone events, Emissions and faction goals
- **Each new Day** rolls a Zone Event (d20, file 10 §2). Possible events:
  - A quiet day (extra rumors).
  - A caravan, which gives that zone's trader 2 extra items.
  - A mutant migration (+1 hazard and mutants in that zone for a few days).
  - An anomaly shift (that zone's anomalies are fresh and richer).
  - Someone you know wants to talk.
  - An **Emission**.
- **Emission:** the HUD shows *EMISSION IN 3*. You have 3 rounds to get indoors (a building
  floor), into a hidden sub-zone or into a Safe Zone. Otherwise you take 60 anomalous damage
  (reduced by Anom-DR) and become Irradiated.
  - Afterwards that zone's anomalies can be searched again, with +3 to the search.
  - Out of combat, one move's worth of tiles counts as a round.
- **Faction goals** (file 10 §3): each faction has a 6-box track.
  - Each Day one faction advances a box.
  - Finishing a quest for a faction adds a box.
  - Killing 3 of a faction's members in one day removes a box.
- **A completed goal changes the map:**
  - **Obelisk** takes Quarry Basin and the Ship Graveyard from the Contractors.
  - **Contractors** fortify Ghost Town: hazard −1 and the Raiders leave.
  - **Raiders** hold the bridge: crossing into or out of Black Valley costs 200 RU or a fight,
    and its hazard is +1.
  - **Sentinels or Liberators** take Vanguard Barracks. Whichever wins first locks out the other.
  - **Garrison:** Siege Ward hazard −1.
  - **Surveyors:** +2 to artifact searches at the Epicenter.
  - Zone hazard and faction lists now go through `WorldEvents.Hazard()/Factions()` everywhere,
    so these changes reach spawning, squads and the zone header.
- **Rumors:** you hear 1-3 when you arrive in a Safe Zone, once per day. They come from the real
  world state (factions close to winning, hidden routes, caravans, migrations, fresh anomalies).
  About 1 in 3 is wrong, per file 10. Rumors you've already heard aren't repeated.
- **People you know:** NPCs who surrender to you, run from you, or give you a finished quest are
  remembered. The list keeps 10.

### World panel: `W`
- Shows the Day, today's event, the emission warning, every faction track (`[###...]`), recent
  rumors and the people you know.
- The HUD also shows the Day now.

### Saves
- Day, crossings since rest, faction tracks, finished goals, rumors, NPCs, today's event, the
  caravan and the migration are now saved. Day wasn't saved before at all.

### Tests
- New world sim: 120 Days of events and rumors, with no exceptions. Tracks stay within 0-6,
  Sentinels and Liberators never both win, and every zone resolves its hazard and factions.
- Artifact rolls for tiers 1-5 always give a real artifact.
- **Not checked in Unity yet**, same as batches 1-4 (the editor hadn't recompiled when I pushed).

## Batch 6: character creation, ranks and every perk (files 00 §4, §13 and 05)

### Character creation
- **New Game now opens a creation screen** (file 00 §4).
- **Stats:** everything starts at 10 and you have 6 points to spend. Each stat goes from 6 to 16,
  and lowering one gives you points back. The screen shows each stat's modifier and what it
  governs.
- **Loadout, pick one:**
  - **Scavenger:** knife, PMm, Leather Jacket, 2 medkits, a bandage, +200 Ru.
  - **Ex-Military:** AKS-74U and 60 rounds, switchblade, Raider Jacket, medkit, stimpak, +100 Ru.
  - **Freelance Trader:** PMm, Scrap-Plated Vest, Small Canvas Sack, medkit, +500 Ru.
  - The loadout's Ru is added to the 1000 everyone starts with. If you meant the loadout amount
    to be the whole purse, change it in `GameBootstrap.StartNewGame`.
- **Starting perk:** pick from the full list.
- The death screen's Restart also goes through this screen.

### Ranks (file 00 §13)
- **You rank up as soon as every milestone for the next rank is met.** Nothing to grind.
  - **Experienced:** Tier 2 zone + 1 quest.
  - **Professional:** 3 quests + a Tier 2 mutant or a Professional+ NPC kill. Gives a perk.
  - **Veteran:** Tier 3 zone + clearing a hidden sub-zone or finishing an NPC storyline.
  - **Expert:** Tier 4 zone + a Tier 4 mutant + a finished storyline. Gives a perk.
  - **Master and Legend** need the Main Story (file 09), which isn't in the game yet, so for now
    Expert is the cap.
- A rank up raises AP, the attack/AC modifier and, at Expert, MP (already wired in batch 2).
- **`P` opens the character sheet:** stats, perks, and the next rank's milestones ticked off.
  Unspent perk picks can be taken from there ("Later" is allowed).
- Progress is saved.

### Perks: all of file 05 now does something
- **Before:** 10 perks existed in the data and nothing ever gave you one.
- **Now:** there are 33. Only **Gunslinger** was left out, because it needs a second sidearm
  slot. What each one does in game:
  - **Marksman's Focus:** +2 with snipers.
  - **High-Velocity Impact:** extra 20% DR ignore at 12+ tiles.
  - **Overwatch:** end your turn with 2+ AP and a loaded sniper. The first enemy that acts in your
    sights takes a free shot. It fires as that enemy starts its turn, not mid-walk.
  - **Recoil Control** and **Controlled Taps:** already worked.
  - **Suppressive Fire:** `S`, then click a tile; 3 AP with an assault rifle. Enemies within 4
    tiles are Suppressed, and anyone who fails Agility DC 13 loses 2 AP.
  - **Bullet Hose:** no SMG burst penalty within 4 tiles.
  - **Run and Gun:** `H`, 1 AP for +2 MP, once per turn.
  - **Hit and Run:** +2 MP after your first attack each turn.
  - **Entrenched:** +20% Phys-DR on turns you didn't move.
  - **Shredder:** heavy bursts get advantage and use double ammo.
  - **Walking Tank:** no 2-MP cap from heavy weapons.
  - **Breacher:** already worked.
  - **Buckshot Spread:** the same shotgun roll is checked against one enemy next to your target.
  - **Staggering Blast:** a shotgun hit within 4 tiles takes 2 MP off the target's next turn.
  - **Double Tap:** your second sidearm shot in a turn costs 1 AP.
  - **Point-Blank Execution:** +2 to hit and ignores cover, now limited to sidearms as written.
  - **Blade Dancer:** melee attacks cost 2, then 1, then 1 AP, with a maximum of 3 per turn.
  - **Desperate Parry:** end your turn with 1+ AP. The first melee hit on you gets an Agility
    DC 14 roll to negate it.
  - **Combat Medic:** +10 HP from medkits and stimpaks.
  - **Quick Hands:** now every reload in combat is free, as written. Before, only the first was.
  - **Gunsmith** and **Sprinter:** already worked.
  - **Tactical Reposition:** +1 MP per turn, and +1 AC if you end your turn in or next to cover.
    The "MP for cover only" restriction isn't enforced.
  - **Adrenaline Surge:** the first time you drop under 30 HP in a fight, +20 temp HP and +2 AP
    on your next turn.
  - **Vault**, **Pack Mule** and **Artifact Handler:** already worked.
  - **Iron Stomach:** food and water restore double.
  - **Anomaly Whisperer:** +2 to avoidance.
  - **Scavenger:** NPC extra-loot and footlocker counts are rolled twice and the higher kept.
  - **Iron Will:** temp HP lasts 8 turns.
  - **Haggler:** buy 15% cheaper, sell 15% higher.
- The controls hint lists the new keys: `W` the Zone, `P` character, `S` and `H`.

### Tests
- **Every loadout:** the right gear, Ru and perk.
- **The rank ladder:** checked step by step. Master stays locked.
- **Specific perk checks:** Iron Will, Haggler and Walking Tank.
- **Every perk in real fights:** each one was taken into 3 fights against 2 raiders, with no
  exceptions.
- **Balance:** a fresh rookie with a PMm against one rookie raider in the open wins about 64% of
  the time. Lone Tier 1 mutants are easy (they're pack animals, per file 04).

## Batch 7: the Main Story (file 09 Part 3), safe passage, and a Max HP bug

### "The Heart of the Zone" is playable from start to finish
- Every step is **`E` next to a canon landmark**.
- The World panel (`W`) shows the act, the fragments (`Alpha [x] Beta [ ] Gamma [ ]`) and the
  next objective in plain words.

**The acts:**

1. **Origin**, in Secret Lab Alpha (find it from the Industrial Basin's Loading Dock).
   - Take the access keycard from a dead guard at the **Reception Airlock**.
   - Then open the sealed terminal at **Sample Storage** for **Fragment Alpha**.
   - The reward is a supply crate: 2 Tier 4 items + 2000 Ru.
   - File 09 doesn't say where the keycard comes from, so I put it at the airlock.
2. **Guardians**, in Secret Lab Beta.
   - Get the encrypted **Research Drive** from the **Archive Room**.
   - Decode it with **Dr. Osei at the Sub-Station House** (Dried Lake, +rep with the Surveyors),
     or yourself at any lab landmark: Intelligence DC 18, 2 AP per try.
3. **The Signal**, in Secret Lab Gamma.
   - Clear the room, then open the **Sealed Vault**.
   - You get **Fragment Gamma** and **The Compass** (Tier 6). This counts as "Act 3 done" for the
     Master rank.
4. **Allegiance**, at the **Garrison Barricade** (Abandoned City).
   - Pick a sponsor from the factions that are Friendly with you, or go alone.
   - With a sponsor, the Siege Ward's hazard drops by 1 while they escort you.
5. **The Last Gate**, in the Reactor Core.
   - Clear the Control Room first (a Friendly Obelisk doesn't block you if they're your sponsor).
   - Then open **The Last Gate** with all three Fragments.
   - You get **The Black Star**, and the finale unlocks **Legend**.

**Endings follow the sponsor, and their world changes are real:**

| Sponsor | Ending | What changes |
|-|-|-|
| Surveyors | Contain it | Emissions only on a natural 20; Friendly prices at every trader |
| Contractors | Sell it | +20,000 Ru; prices +20% |
| Sentinels, Garrison or Liberators | Destroy it | Tier 6-7 zones become Tier 5; artifact searches −2 |
| Obelisk | Wake it | Every other faction turns Hostile; Obelisk maxed |
| Alone | Take it | −rep with everyone; +10 Max HP for good |

- After the ending the game keeps going, as file 09 says.

**Rules around it:**
- Fragments and the keycard are "key" items, and traders refuse them.
- The story and the ending are saved.
- Zone tiers are restored on a new game or a load, so a "Destroy it" run doesn't leak into the
  next one.

### Safe passage (file 00 §12)
- **Before:** every NPC attacked on sight, whatever your reputation.
- **Now:** **Friendly factions don't.** If you shoot them, they fight back.
- **Neutral factions still attack on sight, as before.** File 00 literally says only Hostile
  factions do. Making Neutral peaceful would turn the early game quiet, so I left it for you to
  decide.

### Bug fix
- **Max HP never came back after taking Lucifer off.** Unequipping it left you at 75 Max HP
  forever. Max HP is now recomputed from 100 each time artifacts change.

### Tests
- **Full story run:** played twice, once with the Garrison as sponsor and once alone.
- **Each step checked:**
  - The terminal refuses you without the keycard.
  - Key items can't be sold.
  - The escort lowers the hazard.
  - The Gate stays shut until the Control Room is clear.
  - The Destroy ending shrinks the Zone, and a new game restores it.
  - Take it gives +10 Max HP.

## Batch 8: technician upgrades (file 08 §5)

### Upgrades
- **The Hub's Repair Yard (`B` in Scavenger's Hub) now sells upgrades** for your equipped weapon
  and armor. Faction quartermasters still only repair, per the doc.
- **Limits:** up to 3 per item, each option once. Upgrades stay on the item through swaps,
  stowing it in your pack, and saves.
- **Weapon upgrades:**
  - **Calibrated:** +1 to hit.
  - **Reinforced:** +3 base damage.
  - **Extended Mag:** +50% magazine.
  - **Hardened:** only a natural-1 jam costs Condition.
- **Armor upgrades:**
  - **Plating:** +5% Phys-DR.
  - **Lining:** +10% Anom-DR.
  - **Lightened:** −3 kg.
  - **Braced:** the first Condition loss each fight is ignored.
- **Cost, as written:**
  - 1st upgrade: 1 Mutant Part + 10% of the item's band midpoint.
  - 2nd: 2 Tier 3+ parts + 20%.
  - 3rd: 3 parts (one Tier 4+) + 30%.
  - The cheapest qualifying parts are used.
- **Mutant Parts now remember their tier.** Older saves' parts are read from the name.

### Fixes found on the way
- **Damaged armor could bake the Entrenched bonus in for good.** If your armor lost Condition
  while Entrenched, the +20% got stuck permanently. This was my bug from batch 6.
- **Worn or upgraded copies of the same gun used to merge into one stack**, and the stack took
  the first copy's condition. They now stay separate.
- **The test runner** used to report the previous run's result when the test file failed to
  compile. It now deletes the old build first.

### Tests
- **The full upgrade path:**
  - No parts means no upgrade.
  - Each tier's part requirement is checked.
  - The 3-upgrade cap holds.
  - Plating adds exactly 5%.
  - Upgrades survive stowing and re-equipping a gun, and a save/load.
- **Hardened:** ignores a non-1 jam.

## Batch 9: legendary artifacts, Named mutants, prisoners (files 09 and 04)

### Legendary artifacts in their fixed places (file 09)
- **Heart of the Oasis:** press `E` beside **The Wound**, the crater's centre in the Anomalous
  Epicenter.
- **Lucifer:** in the **Sealed Crypt** of the Siege Catacombs.
  - A **Named mutant** curls around the crypt and has to die first.
  - Then it's a Strength DC 18 roll to shift the lid, 2 AP per try.
- **The Compass** and **The Black Star** come from the Main Story (batch 7).

### Named mutants (file 09, "After the ending")
- **What makes them Named:**
  - ×1.5 HP and +2 to hit.
  - They never panic.
  - One borrowed trait: Cloak, Pack Tactics or bleeding claws.
  - A name: "Old Scar (Dread Stalker)", "the Widowmaker", and so on.
- **Drops:** 2 Mutant Parts plus a Tier 5 loot roll.
- **Where they appear:**
  - The crypt guardian, until Lucifer is taken.
  - After the ending, one in each Tier 5+ zone until you kill it. Each stays dead.
  - They go into the "People you know" list.

### Prisoners (file 04, Surrender)
- **Left-clicking a surrendered NPC** now asks what to do:
  - **Take their gear:** what used to happen.
  - **Interrogate:** once per prisoner, Intelligence or Strength DC 12. Success gives either a
    *true* lead on this zone's hidden route (added to your rumors) or their squad's stash (one
    loot roll).
  - **Let them go:** they run, and you get +rep with their faction (not Raiders or Obelisk, as
    written). They're remembered in "People you know".
- **Right-click** is still an execution.

### Tests
- **Named mutant stats.**
- **The crypt:**
  - The guardian spawns and blocks the crypt.
  - Its death is remembered and drops 2 parts.
  - Lucifer can then be taken, and the guardian doesn't respawn.
- **The Heart of the Oasis** pickup.
- **Prisoners:**
  - Letting one go makes them flee and gives +rep.
  - Interrogation pays off.

## Batch 10: quest rules (file 09: complications and non-Ru rewards)

### Every quest rolls a complication when you accept it (d6, file 09 rule 2)
- **The roll shows** in the log and in the quest journal (`J`).
- **What each result does:**
  - **1, clean:** nothing.
  - **2, Rival:** the first time you enter the quest zone, a 2-person crew (a Professional and an
    Experienced) from the Raiders, Wanderers or Outcasts shows up, after the same thing.
  - **3, Not as described:** kill objectives need one more target.
  - **4, Double-cross:** at turn-in the giver tries to short you. Intelligence or Strength DC 13
    to call it out, otherwise half pay.
  - **5, Clock:** finish within 2 Days or the pay is halved.
  - **6, Better offer:** at turn-in another faction offers +50% for what you bring back. Taking
    it gives +rep with them and −rep with the quest's faction.
- **Older saves:** quests accepted before this read as "clean".

### One non-Ru reward per quest (file 09 rule 4)
- **Storyline quests** already gave +rep with the faction.
- **Standalone jobs** now also give a **true rumor** about an undiscovered hidden route,
  preferring the quest's own zone.

### Not done
- File 09's **"two ways to finish"** (rule 3) needs per-quest alternative endings written into
  the quest data. "Better offer" covers some of it for now.

### Tests
- **Each complication 1-6 played through:**
  - The rival crew appears once.
  - The clock halving applies.
  - Every quest still pays and gives its rumor.

## Batch 11: artifact rules, crossing encounters, survival warning (files 03 and 00 §7-11)

### Artifacts actually cost something now
- **Bug:** every artifact's **Anom-DR penalty** was computed and then never applied. So were
  Lucifer's +10% Phys/Anom-DR. Artifacts were all upside. Both now feed your real DR, and the
  Artifact Handler perk still halves the penalty.
- **Same family doesn't stack** (file 03): only the strongest Gravitational, Chemical, Thermal or
  Electrical artifact gives its bonus, and the extras still cost their penalty.
- **Each legendary is its own family,** as file 03 says. The old "only one legendary works" limit
  wasn't in the docs, so it's gone. Lucifer and The Compass both work together.
- With no armor, artifact DR bonuses and penalties still apply.

### Hazard check on every crossing (file 00 §11)
- Re-entering a zone you've already been to rolls the doc's check: Hazard × 15% + Tier × 5% on a
  d100, automatic at Hazard 4+.
- A hit drops a group in at the map edge: 2 people, or 3 plus a leader on a 20.
- First visits still use the zone's own population.

### Survival
- **Warning:** after a crossing, if another trip would take Hunger, Thirst or Tiredness to 0, the
  log tells you to eat, drink or rest first.

### Tests
- **Artifacts:**
  - Two Gravitationals give only the stronger bonus, and both penalties apply.
  - Two legendaries both work.
  - Lucifer sets Max HP to 75, and everything is restored when you take them off.
- **Hazard checks:** Hazard 4+ guarantees an encounter, and Safe Zones never get one.

## Batch 12: reputation on the doc's scale, and "two ways to finish" (file 00 §12, file 09 rule 3)

### Reputation now matches file 00
- **The problem:** internally reputation runs −100…100, but the thresholds didn't match the doc.
  You became **Hostile at −1**: a single kill did it. Quests gave +5, so Friendly (+50) took
  about 10 storyline jobs.
- **Now:** 1 doc point = 17 internal points.
  - **Hostile:** −3 (−50 internal).
  - **Friendly:** +3 (+50 internal).
- **What moves it:**
  - Killing one of their people: −10. Several kills add up to the doc's −2 or −3.
  - A storyline job: **+2**.
  - Letting a prisoner go, Dr. Osei decoding the drive, and similar favours: **+1**.
- **The character sheet (`P`)** now lists your standing with every faction in doc points. `*`
  means Friendly and `!` means Hostile.
- **Stale comment:** an old note in `Player.cs` claims trading and travel move reputation. Nothing
  in the code does that. I left it alone.

### Every quest has two ways to finish (file 09 rule 3)
- **Both sides are named when you accept:** the faction it's for (the storyline NPC's faction,
  the zone's quartermaster, or the zone's main faction) and a rival buyer. The quest journal
  shows them too.
- **At turn-in you choose:**
  - Give it to the giver: full pay and +1 rep (+2 for storyline jobs).
  - Take it to the rival: the same pay, +1 rep with them, −1 with the giver.
- **Complication 6 ("Better offer")** is now simply the rival bidding +50%.
- **Turn-ins also push faction goals:** whoever you hand the result to fills a box on their
  Faction Goal track (file 10).

### Tests
- **Both sides of a quest** are different factions.
- **A storyline job** gives exactly +2 to its giver.
- **The prisoner release** test now expects +1 doc point.

## Batch 13: Gunslinger, a better equipment panel, and a soak test

### Gunslinger (the last missing perk, file 05)
- **Setup:** with the perk and a sidearm in your main hand, sidearms in your pack get an
  **Off-hand** button.
- **Firing:** a single shot fires **both guns**. That's exactly 2 AP, each at −2, each resolved
  fully. Chiappa Rhino cadence and Double Tap don't apply to dual shots, as written.
- **Reloading** covers both guns for +1 AP.
- **Housekeeping:**
  - The off-hand gun counts toward carry weight and is saved.
  - Equipping a non-sidearm in your main hand puts the off-hand gun back in your pack.
- **All 34 perks** in file 05 now work.

### Equipment panel (the inventory corner)
- Shows the off-hand gun.
- Shows **every** belt slot your armor has, not just 2.
- Shows artifact **names** instead of internal ids ("Lucifer", not "lucifer").

### Soak test
- **What it does:** a 400-step random playthrough in the test harness. It travels through
  connected and hidden zones, fights whatever is there, searches anomalies, interacts with
  objects and landmarks, rests, shops, and saves/loads every 60 steps.
- **Purpose:** catching exceptions in code paths the unit checks don't reach. It runs clean:
  about 280 crossings and 800 combat rounds without a single exception.

### Tests
- **Gunslinger:**
  - Two PMms fire for 2 AP.
  - The off-hand survives a save.
  - Stowing puts the gun back in your pack.

## Batch 14-15: switches, and fixes from a full code review

A second pass read through everything written tonight, looking for bugs that only show up in
real play. These were confirmed and fixed.

### Could crash, lock up or lose progress
- **Two quests finishing on the same kill lost one quest's reward.**
  - **Before:** the second dialog replaced the first, and the first quest's reward was never
    paid.
  - **Now:** dialogs queue up. A dark backdrop also stops clicks from reaching panels underneath.
- **Closing one panel could unlock the game under another.** A perk pick and a quest dialog
  both open was the case that triggered it. Each now leaves input blocked if the other is still
  up.
- **You could bleed out at the start of your turn and still act.** You could even heal back with
  a medkit. Death is now checked at turn start and every frame.
- **In combat, running out of MP ended your turn and wasted your AP.** That auto-end is now
  exploration-only, as its comment always said.
- **The game could think you were still in a fight** after crossing into a new zone, loading, or
  restarting. That blocked repair kits and threw off the Emission timing. Fixed.

### Rules that were broken
- **Quests tied to finding a hidden route could never finish.** Nothing told the quest system
  when a route was found. This blocked the Dr. Osei and Brother Anatoly storylines, among others.
- **Kill quests in Safe Zones had nothing to kill.** Safe Zones never spawn anything, which
  stalled Yefim's and Sgt. Kade's storylines.
  - **Now:** the first time you enter a quest's zone, its targets are there.
  - **Clear-the-zone** objectives now ignore prisoners, runners and Friendly NPCs, and are also
    rechecked when a fight ends by surrender.
- **Free ammo from swapping guns or save/load:**
  - **Before:** every equip came with a full magazine plus a hidden spare one.
  - **Now:** swapping out unloads the old gun into your stash, and a gun from the pack comes out
    empty.
  - **Saves** now keep the magazine count, the ammo type and whether the gun is jammed.
- **The Emission counted down twice as fast while exploring.** Fixed.
- **Lucifer's −25 Max HP never cost real HP.** It does now.
- **Armor mods followed you onto armor with fewer slots.** Mods that don't fit go back to your
  pack (file 02).
- **The "one legendary at a time" check** was still in the belt code. Removed, as file 03
  says.
- **The double-cross complication also halved the rival's payment.** It now only hits a turn-in
  to the giver.
- **Two copies of the same gun with different condition shared an id.** "Sell PMm (Damaged)"
  could sell your good one. Each copy now gets its own id.

### Smaller fixes
- **Artifacts:** they keep their sell value after you take them off the belt. Legendaries are
  about 20,000.
- **Anomaly damage** now respects the 75% Anom-DR cap like everything else.
- **The Emission's +3 search bonus** only lasts for one search.
- **A new backpack** puts the old one in your pack instead of deleting it.
- **No line of sight:** the shot is refused before any AP is spent.
- **The Emission countdown and "rumors already heard today"** are saved.
- **Quest rivals** don't spawn in Safe Zones.

### The switch from batch 14
- `GameConstants.NeutralAttackOnSight`; see the questions at the top of this log.

### Tests
- **Regression checks for these fixes:**
  - Unloading on a swap, and no free reload.
  - The magazine is saved.
  - The extra mod is returned.
  - Lucifer clamps HP.
  - Legendaries keep their value.
  - Distinct ids for different copies.
  - Hidden-route quests complete.
- **The rival test** was updated so Safe Zones expect no rivals.
- **The soak test** still runs clean.

## Batch 16: fixes from a second review (combat, AI, movement)

A second review pass covered the older combat, AI, terrain and economy code. It found no hangs or
out-of-range crashes. It did confirm the problems below, all now fixed.

- **A fight couldn't end while an unaware or Friendly squad was alive somewhere in the zone.**
  "Area secured" never fired. Only enemies who are *aware of you* keep a fight going now.
- **Enemies already hunting you didn't count as a fight.** These were squads that spawn aggroed
  at Hazard 4+, or ones still angry from your last visit. The turn then auto-ended when your MP
  ran out, wasting your AP. They now put you in combat.
- **One error during the enemy turn froze the game for good.** The turn never came back to
  you. Errors are now caught, logged to the Console, and the turn returns to you.
- **NPCs who ran out of ammo kept "reloading" forever and never attacked again.** They now throw
  the empty gun away and fight with what's left.
- **Idle enemies could get stuck wandering toward a point inside a wall.** Searching a path to
  it also cost a full map-wide pathfind every tile you walked. Wander targets are now always
  walkable, and a blocked wanderer picks a new one.
- **Two separate squads of the same faction shared one leader.** That leader's death shook the
  other squad too. A squad is now the faction's people near each other who are equally alert.
- **Morale carried over between visits** (file 04 says everyone starts Steady), and NPCs who
  fled came back as permanent runners. They now reset on zone entry, and runners are gone for
  good.
- **Pack Tactics counted any neighbour.** It now needs an actual packmate.
- **Prone mutants paid MP to stand up instead of AP** (file 00).
- **Dread Stalkers could leap through walls.** Leaps now need line of sight.
- **Garrison, Sentinels, Contractors, IZC and Obelisk** Professionals+ carry their extra smoke
  grenade or flashbang (file 04).
  - **Smoke:** thrown to cover their own retreat once they're Shaken.
  - **Flashbang:** thrown at you when you close to within 4 tiles.
  - **Unused ones** drop as loot.
- **Spawned units no longer stack on one tile.** Before, only the top one could be clicked.
- **A fleeing NPC's dropped gun** is no longer still in their hands.

### Tests
- **New checks:**
  - An unaware squad doesn't hold a fight open.
  - No stacked spawns.
  - Pack Tactics needs a packmate.
- **Everything else still passes**, including the soak run.

## Batch 17: seeing what matters on the map

- **Hover info.** Point at a tile and a line at the top of the screen says what's there:
  - **Units:** name, faction, rank, morale, HP, and whether they're unaware or Friendly. Named
    mutants are flagged.
  - **Anomalies:** whether they've been searched yet.
  - **Landmarks:** a story objective is marked `[OBJECTIVE] E next to it`.
  - **Objects and exits:** objects say "E next to it"; exits show where they lead.
- **Story objectives pulse gold on the map.** This covers the landmark the Main Story wants next,
  plus The Wound and the Sealed Crypt until their legendaries are taken.
- **Searched anomalies are dimmed.**
- **Named mutants glow orange** and are drawn slightly bigger.
- **A hidden route you discover mid-visit** now shows its exit marker right away. Before, it only
  appeared after leaving and coming back.

### Tests
- **Objective markers:** the right landmark is marked for each act and for the legendaries.
- **Hover text:** units and every landmark produce it.

## Batch 18: fixes from a UI review

- **Load (F9) or Restart in the same zone used to keep showing the old map.** Old doors were
  still open, old rubble was still there and landmarks sat in their old spots, while clicks used
  the new layout. The map now redraws whenever the terrain is regenerated.
- **The HUD boxes swallowed map clicks.** These are the status box, the equipment box, the
  inventory panel background and the controls hint. Together they cover about a fifth of the
  screen, so enemies on the right-hand side often couldn't be clicked. Clicks now pass through
  them; only the actual buttons catch clicks.
- **Closing a dialog could unlock the world under another panel.** For example, a quest that
  completes while the journal is open. Dialogs now restore exactly the state they found.
- **Nothing in the world reacts while you're dead.** Before, F5 could save a dead character.
- **The main menu and death screen** always come to the front when shown again.
- **Inventory buttons are inactive while the shop or another panel is open.**
- **The inventory now refreshes after a repair**, and when you gain Gunslinger. Before, the
  "Off-hand" button only appeared after something else changed.
- **The EventSystem check now finds an existing one during Awake**, so no duplicate is created.
- **A small memory leak on every zone change** (the old map sprite) is fixed.

## Batch 19: copyright rename pass

Every name that came from S.T.A.L.K.E.R., another game, or a real gun or gear brand was replaced
with an original one, in the design docs, the game code and the art scripts. The full old -> new
list is in `ArtSource/NAME_CHANGES.md`.

- **What changed:**
  - 55 weapons (the Kalashnikov family is now "VK", the western makers are Corvan and Halden).
  - All STALKER armor names, including the faction-armor line (SEVA -> Kokon, SSP -> HZ, CS -> Marsh, Merc -> Contractor).
  - 16 artifacts, 4 anomalies, all 4 detectors.
  - The "... Stalker" mutants are now "... Prowler" (Flesh Stalker -> Pallid Prowler, since Flesh is a STALKER mutant too).
  - Emission -> Surge, Stimpak -> Stim, Kevlar -> Aramid, Antirad Pill -> Rad-Block Pill.
  - Sentinel Nosorog / Sentinel Juggernaut -> Sentinel Proto-Armor; PS5-M Universal -> Sentinel Service Suit.
  - Ecologist -> Surveyors everywhere in the docs (the code already used Surveyors).
  - "NEW STALKER" on the character screen -> "NEW SCAVENGER".
- **Old saves still load.** New `NameMigration.cs` swaps old names for new ones in the save file
  (and the offline-squads file) before it is read. Only exact values are swapped, never text inside
  sentences.
- **Left alone on purpose:** internal code keys (e.g. `ColtM1911`, `SEVASuit`, `gravi`), method and
  field names, and sprite file names. Players never see them, and keeping them avoids breaking
  saves and asset references. NPC names, zones, factions and the other mutants were already original.

### Tests
- **Old save names migrate:** an old-style save snippet comes out with the new names, and free text is untouched.
- All existing checks pass with the new names.

## Batch 20: internal names cleaned too

Batch 19 changed what players see. This batch renames what's under the hood, so nothing points
at S.T.A.L.K.E.R. or a real brand even if someone decompiles the game.

- **Internal keys renamed to match the new names**, e.g. `ColtM1911` -> `Halden45Service`,
  `SEVASuit` -> `KokonSuit`, `gravi` -> `ballast`, `basicStimpak` -> `basicStim`,
  `FleshStalker` -> `PallidProwler`. That's 130 keys across weapons, armor, faction armor, artifacts,
  consumables, mods, mutants and anomalies.
- **Zone ids:** `red_forest` -> `crimson_woods`, `dead_city` -> `ghost_town` (both were STALKER place names).
- **Code names:** `EmissionRounds`/`StartEmission`/... -> `SurgeRounds`/`StartSurge`/...; comments
  that mentioned STALKER were reworded.
- **Art scripts and sprite files:** `unit_stalker` -> `unit_wanderer`, `unit_ecologist` -> `unit_surveyor`,
  `mutant_veil_stalker` -> `mutant_veil_prowler`, `artifact_jellyfish/blood_stone/sparkler` ->
  `artifact_drift_bubble/pulse_pebble/static_wisp`, `anomaly_burner(_strip)` -> `anomaly_pyre(_strip)`.
  The export script re-generates `FactionArmorDatabase.cs` and `SpriteAtlasIndex.cs` byte-for-byte
  identical to the renamed ones, and its now-pointless name-mapping helpers were removed.
- **Old dev saves still load:** `NameMigration.cs` now also converts old keys, zone ids and save-field
  names. That file (plus two alias lines in `FactionDatabase`) is the only place old names remain,
  and it's marked to delete before release.

### Tests
- **Old save keys migrate:** an old weapon key, item key, zone id and save-field name all convert.
- All existing checks pass with the new keys.

## Batch 21: floating hands, held weapons and the new armless characters

- **Characters are armless now** (the locked art style): rounded shoulders, a detail per armor
  style (zip, harness, plate edge), heavy armor and exoskeletons one leg-row taller, and the new
  full-knit balaclava. `armor_atlas.png` is regenerated for all 180 armors; the faction armband
  sits on the left shoulder.
- **Every human holds their weapon with two floating gloves** (new `GameCore/HeldWeapon.cs`):
  - The gun turns toward the mouse for the player and toward the player for aware enemies.
    Corpses and surrendered NPCs show no gun.
  - The grip hand orbits the chest at a fixed reach (pistol and knife 4, SMG 3.5, long guns 3
    character pixels), snaps to whole pixels and never drops below the waist.
  - Pistols cup the second hand under the first; long guns put it on the handguard; knives are
    one-handed. Aiming left mirrors everything, so the gun is never upside down.
  - Gloves are built at runtime per armor: cuff in the suit colour, charcoal glove by default,
    suit-coloured on hazmat suits, metal gauntlets on exoskeletons, skin when bare-handed. The
    colours come from `SpriteAtlasIndex.ArmorCuff/ArmorGlove`, generated by the export script.
- **Weapon art:** each weapon uses `Resources/Sprites/Weapons/weapon_<Key>.png` once it's drawn.
  Until then it uses a stand-in for its category (`weapon_cat_melee/sidearm/smg/shotgun/assault/
  sniper/heavy.png`). Grip and support points live in `WeaponArt.Grips`.
- Smoke now draws over guns and hands too.

### Tests
- **Hand placement:** pistol right/left mirrored, aiming down stays at the waist, aiming up raises
  both hands, rifle support on the handguard, knife one-handed, reach per weapon type, a hand
  colour for every armor.

## Batch 22: combat feedback, reloads, and Yan's design answers

### Design answers
- **Neutral factions hold fire** until you shoot them (`GameConstants.NeutralAttackOnSight = false`).
  Raiders and Obelisk start the game Hostile (-3), so bandits and the cult still attack on sight.
- **Starting money is 300 Ru**, plus each loadout's bonus (Scavenger +200, Ex-Military +100, Trader +500).
- **No more drain per tile walked.** Hunger, thirst and tiredness only drop when you cross zones.
- **Healing has a cost:** each medkit or stim takes 0.25 hunger, thirst and tiredness per 10 HP it
  restores (a 20 HP medkit costs 0.5 of each). Food and drink don't. Placeholder rate, one constant to tune.
- File 00 updated to match (§4 vitals, §7 survival, §12 reputation).

### Combat feedback (new `CombatFx.cs`, `CombatFxView.cs`, `make_fx.py`)
- The rules now record every shot, melee swing, mutant attack and reload. The view plays them back
  a beat apart, so an enemy turn reads as a sequence instead of everything at once.
- **Shots:** muzzle flash at the barrel tip, a tracer to the target (or past it on a miss, into the
  cover when line of sight is blocked), recoil (the gun jumps back and the muzzle climbs, then
  settles), ejected casings (red shells for pump shotguns; revolvers and break-actions keep theirs
  until reload), a red hit flash with a 1-pixel knockback and a blood puff on the target, and
  camera shake for big guns when you fire. Bursts fire each round separately. A jam is just a twitch.
- **Every weapon feels different**, derived from its stats so there's no 55-row table to maintain:
  - The category sets the baseline. How hard the gun hits for its class scales recoil, climb,
    flash size and shake, and High Recoil adds 40%.
  - Results: the RK-12 Kuvalda kicks about 3x a PSh-18, and the AM-50 has a muzzle-brake flash and
    the biggest shake.
  - Suppressed guns (TS-9, VSN Shepot, VK-127) show a small smoke puff instead of a flash, with a faint tracer.
  - The G-7 rail fires a cyan beam.
  - Overrides for odd cases live in `WeaponFeel.Overrides`.
- **Reloads are animated:** the gun tips and the support hand goes to the belt pouch and back.
  - Box mags drop the old magazine.
  - Revolvers tip up and dump their empties; break-actions pop their shells.
  - Tube-fed shotguns and the lever rifle make several trips to the pouch.
  - Belt-fed guns take longest.
  - Length depends on the weapon (0.7 s for a pistol to about 2.7 s for the Tambour-12).
- **Melee:** the blade winds up and cuts through, with a lunge. **Mutants** lunge at their target;
  psi attacks flash violet.
- New art: `Sprites/Fx/fx_muzzle_1..4`, `fx_muzzle_rail`, `fx_muzzle_puff`, `fx_hit_blood/spark/psi`
  (generated by `ArtSource/make_fx.py`). Missing art falls back to coloured squares.
- ponytail: the Gunslinger off-hand gun flashes from the main gun until the off-hand is drawn.
  There's no sound yet.

### Tests
- **Feel:** hand cannon vs pistol recoil, High Recoil climbs more, suppressed has no flash and a
  faint tracer, the AM-50 and rail overrides, and the reload style per feed type. Every gun has a
  sane reload time, flash and kick.
- **Curves:** recoil eases back, the swing ends at rest, a mag reload has the hand at the pouch
  mid-way, and a tube reload makes one trip per batch of shells.
- **Recording:** a shot is recorded with the right shooter, target and hit; reloads are recorded;
  the queue is capped when there's no view.
- **Design answers:** loadout Ru = 300 + bonus, Raiders start Hostile, others Neutral, and a medkit
  costs hunger and tiredness.


## Batch 23: animated anomalies

- **Every anomaly type now has its own 6-frame animation** instead of the shared `icon_anomaly`
  marker, drawn at full tile size:
  - **Pyre:** the approved flame, now with a scorch ring.
  - **Livewire:** blue-white arcs crackling from a white-hot core, with yellow sparks.
  - **Acid Bloom:** a green pool with bubbles that swell and pop, and fumes rising.
  - **Spindle:** a two-armed dust vortex turning around a dark eye.
  - **Radiation Pocket:** a faint green shimmer with motes rising.
- Neighbouring anomalies animate out of step. Searched anomalies still show dimmed.
- Art: `Sprites/anomaly_<type>_strip.png` from `ArtSource/make_anomalies.py`. A missing strip falls
  back to the old icon.
- The fx strip loader is shared (`CombatFxView.Strip`).

### Tests
- Every anomaly type in `AnomalyDatabase` maps to a drawn strip.

## Batch 24: walking, ragdolls and gore

- **Units walk instead of teleporting.**
  - Every tile step the rules take is recorded (`CombatFx.Steps`, from `TacticalGridSystem.AttemptMove`).
  - The view walks each unit along that exact route at 5 tiles a second, so they go around walls
    instead of cutting through them. Long routes speed up a little so a turn never drags.
  - Humans alternate their feet: left up, stand, right up, stand, once per tile. That's
    `armor_walk_a/b.png`, generated from the armor atlas by `ArtSource/make_walk_atlas.py`;
    `export_to_unity.py` now rebuilds them automatically. Only the legs change, never the body height.
  - Mutants slide for now.
  - The camera follows the walking player smoothly.
  - Moves that skip normal walking (a Spindle throw, squads placed on arrival) slide if short and snap if far.
- **Shots and attacks wait for the shooter to arrive**, so an enemy walks up first and then fires.
- **Ragdoll deaths.**
  - The body is cut into head, torso and legs (sliced from its own sprite, no new art). It falls
    over away from the killing blow, floppy, with the head and legs lagging behind the torso.
  - It settles into a crisp lying pose (right angles, whole pixels). The gun drops and tumbles next to it.
  - The victim keeps standing until the killing shot visually lands.
  - Bodies stay on their tile, so looting by clicking the corpse works as before.
  - Bodies already lying there when you enter a zone are drawn fallen.
- **Gore** (`GameConstants.Gore`, on by default; off = bodies still fall, no blood):
  - Every hit sprays blood on the ground behind the victim, in the direction of the shot.
  - A pool spreads under every body.
  - A big overkill (15+ damage past the HP left: AM-50, Kuvalda, crits) or dying in a frag blast
    takes the head off and throws gibs, each leaving a smear.
  - The oldest of up to 120 blood decals are removed first.
  - New art: `fx_blood_pool` and `fx_blood_splat` (in `make_fx.py`).

### Tests
- **Walk cycle:** the foot order is left, stand, right, stand.
- **Route:** a unit walks the recorded route (not diagonally), arrives and stops, and a long teleport snaps.
- **Killing shots:** they are flagged with their overkill.

## Batch 25: ragdoll fixes (Yan's feedback)

- **Corpses stay exactly where the ragdoll comes to rest.** The final "settle" used to snap
  everything to right angles, which popped. Pre-existing corpses play the fall out instantly instead.
- **The legs are two pieces now**, and the floating gloves stay with the body on soft joints.
- **Dismemberment by how hard the killing blow was:**
  - Overkill 15+: the head or one leg comes off.
  - Overkill 45+, or a frag blast: head plus a leg, both legs, head and both legs, or blown
    completely apart (gloves too).
  - More pieces throw more gibs.
- **Walking slowed** from 5 to 3 tiles a second so the steps are visible, with gentler catch-up on long routes.
- The blood pool spreads where the body lands, not where it stood.

## Batch 26: random ragdolls and blood on bodies

- **Every death rolls its own fall:**
  - Direction jitter of about ±25°, push strength from 0.6x to 1.35x, and a different stiffness
    (floppy to stiff) each time.
  - Each limb comes to rest at its own angle (±25°).
  - About 15% of the time the body twists the "wrong" way.
  - Three styles:
    - **Topple** (about 55%): falls over 60-140°.
    - **Spin** (about 20%): spins round as it goes, ending 200-290°.
    - **Crumple** (about 25%, only without dismemberment): slumps where it stood, head sunk, legs splayed.
- **Blood on bodies:**
  - Every physical hit on a human leaves a small stain (1-3 pixels) inside the silhouette: head 15%,
    torso 55%, legs 30%. Up to 10 per body, for the rest of the visit.
  - When the body falls, the stains go with the piece they were on.
  - Torn-off pieces and the torso get a bloody edge where they parted (neck, hips).
  - Stain sprites are built in code (no art). Off when `GameConstants.Gore` is off.
- Weapon photo list for Yan: `refs/weapon_photo_list.md` (59 weapons, real-world reference for each,
  file names to use).

### Tests
- **Stains:** hits stain the body, capped at 10.

## Batch 27: all 59 weapons drawn from Yan's reference photos

- **Every weapon now has its own sprite:** `Sprites/Weapons/weapon_<Key>.png` (in hand, about 32x16)
  and `weapon_<Key>_hd.png` (menus/inventory, 64x32). The game picks them up automatically; the
  category stand-ins are only a fallback now.
- **How they were made** (`ArtSource/weapons/`):
  - Cut-out: AI background removal (rembg, isnet model). GrabCut or colour keys for the photos on
    wood, red or blue backgrounds. Slings, sheaths and placards were dropped by colour or box.
  - Flipped and straightened so the muzzle points right.
  - Edge-preserving smoothing (removes photo noise and checkering), then scaled to each gun's in-game
    length (pistols 14-17 px in hand, SMGs 16-22, long guns 24-30, RK-12 unchanged).
  - Every pixel snapped to the weapon palette by material: gunmetal ramp, wood, olive polymer, brass.
    The colour is taken from well inside the cut-out so background halos don't bleed in.
  - Majority cleanup, lit top edge, shaded bottom edge, outline. Trigger guards and stock holes stay open.
  - The in-hand sprite is a 2x2 reduction of the menu sprite.
  - Hand-drawn additions: the 4x scope and laser on the PL-74P Zulu, the red dot and laser on the
    VK-74 Obelisk.
  - The RK-12 keeps its hand-pixelled sprite.
- **Grip, support and muzzle points for all 59** are in `WeaponArt.Grips`: an automatic guess from
  each silhouette, checked on an overlay sheet with a few corrected by hand. Pistols, knives and the
  Vosa vz. 62 have no support point; the second hand cups.
- Photo credits (Wikimedia Commons; drawing reference only): `ArtSource/weapons/CREDITS.txt`.
- **Weaker references:**
  - VK-127 Grom: exhibit photo, cut out by hand.
  - Steppe Eagle: angled shot.
  - PL-74P Polovets: on grass, in perspective.
  - Kaban-12: angled display.
  - Better side-view photos of these would improve them.

### Tests
- **Hand points:** every weapon in `WeaponDatabase` has grip/support/muzzle points.

## Batch 28 - hand-pixelled pilot weapons, two glove sizes

- **Pilot weapons redrawn by hand** (`ArtSource/weapons/hand/`): VK-74, PSh-18 Pchela, Bayou Pump and RK-12 Kuvalda.
  - The in-hand sprites are drawn directly at their in-game size (`b_v2.py`), no longer shrunk from the menu art.
    Sizes: Makarov 18 px, RK-12 22 px, VK-74 and Bayou Pump 40 px.
  - New menu sprites: VK-74 and Bayou Pump (hand-drawn at their old size), and the PSh-18 drawn at
    44x30 so it fills the canvas like the RK-12. The RK-12 menu sprite is unchanged.
  - New grip points: the fist closes on the centre of the handle, and the second hand goes under
    the handguard or pump. Pistols still cup.
- **Two glove sizes** (`HeldWeapon.cs`):
  - Nothing equipped: the big 5x4 gloves float beside the torso.
  - Holding something: small 4x3 gloves, so the weapon stays readable.
  - Ragdolls keep whichever size the unit had when it died.
- **Not redone yet:** the other 55 weapons keep their pipeline sprites (now held with the small gloves).
  They get redone in batches once each batch is approved.
- **Tests:** two glove sizes, empty hands sit clear of the torso, pistol cup still works with the new grip points.

## Batch 29 - every weapon hand-pixelled

- **All 59 weapons redrawn by hand** (`ArtSource/weapons/hand/`), same quality bar as the RK-12.
  - One file per gun in `guns/<Key>.py`: menu sprite, in-hand sprite, grip, support and muzzle points.
  - `kit.py guide <Key> <w>` prints a tracing guide; `kit.py sheet <Keys>` draws review sheets.
  - `export_hand.py` writes `Sprites/Weapons/weapon_<Key>.png` / `_hd.png` and the grip table.
  - Sizes in hand: pistols about 18 px (RK-12 22), long guns about 40 px.
- `HeldWeapon.Grips` replaced with the exported table.
- **Drawn from memory, worth a second look:** Steppe Eagle, Kaban-12, VK-127 Grom, PL-74P Polovets, G7.
  The Black Harrier is stainless. The Corvan Wasp 9 sits tall in the hand.
- `Editor/IncomingSync.cs`: zips dropped into `<project>/Incoming/` are unpacked into the project
  and the zip deleted (used to deliver batches while Unity is open).

## Batch 30 - 3/4 world, maps 3x bigger, free walking

- **Bigger maps.** Every authored zone is blown up 3x (hidden interiors 2x) by `MapUpscaler`:
  - Floors and natural masses fill their block.
  - Walls, doors, fences and other lines stay one tile thick.
  - Props sit once per block, off-centre.
  - Then `BuildingCarver` and `MapDresser` fill the extra space.
- **Buildings you can enter** (`BuildingCarver`): every big rectangular block of built wall becomes
  a building:
  - 1-tile outer walls with windows.
  - A door onto open ground (south first).
  - Rooms split off by inner walls, each with a door.
  - Furniture against the walls and some rubble.
  - A block with no open side stays solid.
- **Dressing** (`MapDresser`): per-zone biomes add trees, bushes, rocks, grass, clutter, shacks, camps
  and road wrecks, keeping clear of landmarks and exits.
- **New renderer** (`WorldArt.cs`, art from `ArtSource/world/`). The old `TerrainArt` stays as the
  fallback when the atlas is missing.
  - **Ground:** one texture per zone.
    - Natural floors are generated per pixel in world space, so no tile ever repeats: grass, dirt,
      mud, sand, gravel, water, asphalt, cave.
    - Planks, lab tiles and concrete slabs come from 64 px textures.
    - Borders between floors are warped so they read as patches, not squares.
    - Water gets a muddy bank.
    - Rails, road paint, trenches and platforms connect to their neighbours.
    - Grass tufts, pebbles, leaves, litter, cracks and oil stains are scattered over the ground.
    - Shadows are baked in.
    - Speed: noise lattices are cached per zone and rows are painted in parallel. The 204x204 city
      takes about 0.7 s on 2 cores (harness).
  - **Standing things** are separate sprites, y-sorted with the units so you walk behind a tree or
    wall and in front of it:
    - walls, cliffs, hedges, scrap heaps;
    - trees, cars, crates, furniture.
    Anything tall that hides a unit goes see-through.
  - **Walls are 3/4 autotiles** (`walls.py`).
    - Each has a cap 18 px above a front face, with brick, plaster, concrete panels or corrugated metal.
    - Faces carry windows, doors, metal doors and cracks.
    - Solid blocks get a roof with a parapet.
    - Cliffs, hedges and junk piles have ragged edges.
  - **Atlas:** `Resources/Sprites/world_atlas.bytes` (png) + `world_atlas_meta.txt`, built by `ArtSource/world/pack.py`.
    It holds 1539 sprites: grounds, flats, decals, 75 props and the wall tiles.
- **Movement range** is one soft outlined area instead of yellow squares.
- **Units cast a small shadow** under their feet, in the same colour as the props' shadows.
- **Sorting:**
  - Ground is 0.
  - Ground marks (blood, casings, ragdolls) are below 100.
  - Everything standing is 100 + its foot row.
  - Smoke, tracers and impacts are 32000+.
  - Units use a SortingGroup, so the gun and gloves stay with the body.
- **Free walking out of combat:** WASD or the arrows walk step after step at 4.2 tiles/s. Turns still
  tick underneath. Once anything hostile is aware of you, it's turn-based click-to-move again and
  WASD go back to being combat keys.
  - **The Zone map moved from W to M.**
- **Line of sight** (`Vision.cs`): you see 13 tiles around you, blocked by walls and closed doors.
  - Everything else is drawn darker, with a soft edge.
  - Living enemies out of sight are not drawn, can't be hovered for info and can't be clicked to
    attack. Bodies stay visible.
- **New HUD** (`HudView.cs`), built from the approved mockup:
  - **Zone card:** name, day, tier, exits.
  - **Turn banner:** your turn / enemy turn / exploring, plus the surge countdown.
  - **Hovered enemy card:** rank, hostile / unaware / friendly, HP.
  - **Player card:** portrait, segmented HP, AC, status chips, AP/MP pips, food / water / rest.
  - **Weapon card:** menu sprite, condition and fire-mode chips, magazine, ammo type and spare,
    belt and artifact slots.
  - **End-turn button** (combat only) and a key strip.
  - **Keys:** 1-4 use the belt items, I opens the old inventory panel, F1 the controls list.
  - **Combat log:** moved above the player card, without a background.
  - **Fonts:** Silkscreen and Pixelify Sans (SIL OFL, `Resources/Fonts`, licences in
    `ArtSource/fonts`), turned into TMP fonts at runtime.
  - The canvas scales from 1920x1080.
- **Sound** (`Sfx.cs`; 61 clips in `Resources/Audio`, all synthesized by `ArtSource/audio/sfx.py`,
  so there are no licences to track):
  - Gunshots per weapon class (pistol, SMG, rifle, shotgun, sniper, heavy, rail). Suppressed guns
    are quieter and thinner.
  - Dry fire, flesh and ricochet hits, the player getting hurt.
  - Melee swing, mutant claw and psi.
  - Reloads: magazine, shell, cylinder.
  - Explosions, doors opening and being shot open, pickups, UI clicks.
  - Footsteps by ground type: grass, dirt, hard, wood, water.
  - **Ambient loops:** wind with birds, marsh, cave drips, lab hum.
  - Sounds pan from the camera and fade with distance.
- **Loot and squads for the bigger maps:**
  - About twice the old number of loot spots per zone (not nine times).
  - 75% of them sit indoors or next to crates, lockers, desks, wrecks and machines.
  - One extra squad per ~12k tiles of map.
- Leftover "New Text" objects in the scene are hidden at runtime.
- Urban zones: asphalt with dirt patches, instead of asphalt/concrete camo.
- `ArtSource/world/mock_frame.py`: in-game mock from a harness render.

### Tests
- **World art:** every look in every zone has world art.
- **City buildings:** the city's blocks were carved into buildings with doors and windows.
- **Interrogation:** the test counts ammo and stacked items too.
- **Walking:** the walk test uses the exploration pace.
- **Line of sight:** your own tile is seen; a tile behind a wall and a tile out of range are not.

## Batch 31 - time of day and weather

- **Art direction:** three concepts (`ArtSource/concept/concept.py`: A Zone realism, B Grim wood, C Golden decay).
  Yan: all three have their place, so they became moods of the time of day and weather, on top of A as the base look.
- **Clock** (`Atmosphere.cs`, `MacroState.Hour`, saved):
  - Walking a tile or a combat round takes a minute.
  - Crossing to another zone takes 8 hours, so 3 crossings are about a day, as file 10 has it.
  - Sleeping wakes you at 6:30.
  - Passing midnight starts a new Day. Crossings no longer force a new Day on their own.
- **Weather** (`MacroState.Weather`, saved): clear, overcast, fog, rain or storm.
  - Rolled on every zone entry. Marshes and Black Valley are foggy; the dried lake and the quarry are dry.
  - Fog is likelier around dawn and burns off after 9:00.
  - Drifts a step every 2 game hours.
  - Underground zones have no weather: always dark beyond your light.
- **Look by time of day:**

  | Time | Look | Vision |
  |-|-|-|
  | Night | blue-black, flashlight | 5 tiles + an 11-tile cone toward the mouse |
  | Dawn | violet | |
  | Morning | pale warm | |
  | Day | overcast grey | 13 tiles |
  | 18:00 | golden | |
  | Dusk | violet | |

- **Weather effects on the look and vision:**
  - **Overcast:** greyer.
  - **Fog:** drifting fog layers, vision 7.
  - **Rain:** blue-grey, falling streaks and splashes, vision -2.
  - **Storm:** darker, heavy rain, lightning flashes followed by thunder, vision -4.
- **Enemies** see you less far in the dark, fog and rain (`Atmosphere.SightFactor` scales their range).
- **Drawing** (`AtmosphereView.cs`, every frame around the camera, all textures generated in code):
  - a colour wash;
  - two fog layers;
  - two rain layers, splashes and lightning;
  - a light texture (2 texels per tile, bilinear) that darkens what is outside your light and outside your line of sight.

  It replaces the map-sized vision texture of Batch 30. `Vision.cs` now takes a radius, the cone reach and the aim angle.
- **HUD:** the zone card shows the clock and the weather.
- **Sound:** rain and storm loops replace the zone ambience while it rains; thunder (`sfx.py`).
- **Mock:** `ArtSource/concept/weather_mock.py` renders six conditions with the same numbers.

### Tests
- **Looks:** day, night, fog and storm.
- **Clock:** passing midnight starts a new Day.
- **Flashlight:** at night, a tile 9 ahead is seen and a tile 9 behind is not.
- **World remade in the concept A look** (Yan liked the previews):
  - **Trees and plants** (`ArtSource/world/zone_style.py` wraps the concept builders):
    - Birches with broken, see-through canopies; Crimson Woods gets rust-red autumn birches.
    - Ragged spruces, dead snags, scrub instead of round bushes.
  - **Wrecks:** rusted cars in four paint jobs (rust, faded blue, faded olive, old white), facing either way.
  - **Concrete power poles** (`pole` prop).
  - **Outlines:** every world prop and wall is outlined with a darker shade of its own colour, not black.
    Characters keep the black outline. `WORLD_BLACK_OUTLINE=1` brings the old outline back.
  - **Walls:**
    - Damp dark base, moss, rain streaks.
    - Brick walls get a concrete coping.
    - Hedges are scrub texture, not puffs.
  - **Palettes** moved to olive and straw grass, grey-brown concrete, muted brick and rust; shipping
    containers are faded.
  - **Ground:**
    - Grass mixes straw in pixel by pixel, with a fine grain.
    - A lot more grass clumps and dry weeds; sedge in the mud.
    - Calmer water; softer large-scale light and dark patches.
- **Fog:** what you can't see fades into grey mist, not darkness (`AtmosphereView`).
- `ArtSource/concept/world_shots.py`: in-game style shots of real zone renders, with the atmosphere overlays applied.

## Batch 32 - biome concepts, see-through windows

- **Concept frames per area type** (`ArtSource/concept/biomes.py`), at the forest concept's quality, for Yan to approve before the map generators are rebuilt to match:
  - Swamp: day, and dawn fog.
  - City: day, and rain. A collapsed roof shows the flat below.
  - Industrial: day, and golden dusk with long shadows.
  - Underground lab: flashlight, plus a green tank glow and a red emergency light.
- Characters go through the same light and grade as the world, so darkness, shadow and sun tint them too. Their colours stay, because the colours identify the factions (Yan).
- **Windows are see-through** (Yan): they no longer block line of sight or shots, but still block movement. A window frame next to the target gives hard cover (+2 AC).

### Tests
- **Windows:** see-through, hard cover, not walkable.

## Batch 33 - sun shadows

- **Cast shadows follow the sun** (`WorldArt.CastShadows`). On clear days, every wall, tree, car and crate throws its silhouette onto the ground:
  - long to the west in the morning, short at noon, long to the east in the evening;
  - the colour and length match the golden concept.
  - 3/4 wall sprites cast from their real height (a face of H-16 px plus a top surface), so a building throws one solid band rather than a comb.
  - Overcast, fog, rain, night and indoors keep the soft contact blobs. Indoor floors get no sun.
- **Re-bake off the main thread:** the ground is re-baked on a background task every half hour of clear daylight, or when the weather turns. `WorldArt.SunKey` decides when, and `TacticalGridView` swaps the texture once the task is done. Door and cover repaints wait for the bake to finish. A lock (`WorldArt.Gate`) guards `Begin` and `PaintGround`. The largest map (204x204) bakes in about 1.2 s on 2 cores.
- **Units react to the sun:**
  - A unit standing in a cast shadow is dimmed (×0.64/0.62/0.68, slightly cool). Faction colours stay readable.
  - The unit's blob shadow leans and stretches away from the sun.
- Harness: `RENDER_HOUR` and `RENDER_WEATHER` env vars set the clock and weather before a render.

## Batch 33b - biome pass: swamp, city, industrial (matching the Batch 32 concepts)

`MapDresser` now runs a per-biome composer before the generic dressing. Each composer only rewrites plain cells; landmarks and exits keep their surroundings.

- **Marshes** (`MapDresser.Marsh`): the authored map was one sheet of wading mud. It is now laid out like the concept:
  - sedge meadow (dry and walkable, where the generic pass grows birch groves, scrub and huts);
  - mud flats, reed beds lining the pools, and open water with drowned trees, lily pads and the odd sunk lorry.
  - The upscaled 9-wide causeway is a 3-wide plank boardwalk (`f_boardwalk_<mask>`); the rest of its width goes back to the marsh.
  - Huts are built from the new `plank` wall material.
- **Dead City / Ghost Town** (`MapDresser.City`): streets are rebuilt outward from the building blocks, using Chebyshev distance:
  - 2 tiles of pavement (concrete slabs with a straight kerb), roadway out to 11 tiles, and overgrown lots beyond;
  - a centre line on straight stretches;
  - lamps along the kerb (walkable), bus stops, dumpsters and rubbish bags against walls, burnt-out buses (5-tile footprint) in the road.
- **Industrial Basin** (`MapDresser.Industrial`):
  - The Central Plant is now a corrugated `metal` hall with 4 doors (recipe change).
  - Outside: weeds and gravel breaking up the slab yard, pairs of storage tanks (2x2), overhead pipelines on posts (walk under them), and a rail spur with a flatcar.
  - `MapDresser.Indoors` (a flood fill from the edge) keeps all of this out of the buildings.
- **New world art** (in the atlas, now 1024x2048, 1824 sprites):
  - wall material: `plank`;
  - props: reeds (the concept cattails), `sunktruck`, `lamp`, `busstop`, `bus`, `dumpster`, `trash`, `tank`, `flatcar`, `pipe`/`pipepost`;
  - flats and decals: `f_boardwalk_*`, `d_lily_*`.
- **Ground:** concrete has slab joints every 2 tiles; pavement meets asphalt without the warp; lily pads float on wading water.
- **Footprints:** multi-tile props use a hidden `part` look for the rest of their footprint (no sprite; it blocks like the anchor).
- **Clipping fixes:** birch canopies and scrub no longer clip at the canvas edge (the clusters are clamped inside it).
- **Harness:** `LOOK_STATS=1` prints look counts per zone; `LOOK_WHERE=<look>` prints where a look sits.

## Batch 34 - roofs with a line-of-sight reveal (Yan's idea)

- **Roofs** (`RoofView`): every walled-in space has a roof at wall height, lifted 18 px so it sits flush with the wall caps.
  - Draw order: over everything inside the building, under everything in front of its south wall.
  - Materials: tar roofs with a parapet shadow, re-tarred patches, damp moss and vents. Corrugated tin (with rust) where the walls are metal, plank or hull.
  - The roof reaches 4 px under the thin wall caps, so no floor shows at the seams.
- **Reveal:**
  - The roof fades out while the player is inside that building.
  - From outside, a room loses its whole roof as soon as line of sight reaches any of its floor (through open doors, see-through windows, broken walls). The cutaway is clean, not a staircase, and inner walls keep a strip of roof that reads as the wall top.
  - Each roof keeps a pristine copy of its pixels, so cut cells close again cleanly.
- **Finding buildings:** a flood fill from the map edge (stopping at walls and doors) finds the buildings, and each connected group of indoor cells is one building. Inner partition walls are roofed; outer walls keep their caps.
- **Darkness overlay:** it treats a drawn roof as seen, so roofs don't go dark just because the room under them is out of sight.
- **City:** a stamped plank floor left in the open air becomes pavement in the city composer.
- **Harness:**
  - `PREVIEW_PLAYER=x,y` renders the roofs cut away as that player would see them.
  - Tests: roofs found in the city, none over the street, and a front door between a roofed room and the street.

## Batch 34b - first test in Unity (home PC)

- b30_world unpacked through `Incoming/` and compiled with **no errors** (warnings only). A New Game runs at about 358 FPS in the editor.
- **Checked in play mode:**
  - the world art and HUD;
  - WASD exploring;
  - sun shadows, re-baked in the background when the hour or weather changes (16:00 clear, 18:00 golden);
  - storm and rain;
  - night with the flashlight cone;
  - a shack's roof: it comes off the room seen through the opened door and fades while inside.
- **Dev keys** (editor and development builds only): F10 = +1 hour, F11 = next weather.
- **Fixes from the test:**
  - **Rain** was far too dense in Unity: streaks 70 -> 40 per tile, alpha .55/.35 -> .34/.2.
  - **Doorways:** standing in one left the room's corners dark (single-cell line of sight hugs the wall). What's *drawn* as seen (`Vision.InSight`: overlay and roof cutaways) now also counts line of sight from the four cells next to the player that don't block sight. Gameplay `Sees` (targeting, enemy visibility) stays strict.

## Batch 35 - colour grading by hour and weather

Yan: "one at a time, start with the colour grading, then a-life ticking with the clock, then have squads actually move in world and come in through whatever transition zone they took. The off screen stuff is cool too. Eventually we'll need ambient sounds too." (Also: it's an open-world game like S.T.A.L.K.E.R., not an extraction game; CLAUDE.md now says so.)

- **Grade** (`ColorGrade.cs`): one global URP Volume, built in code at the first frame, with Color Adjustments (saturation, colour filter), Lift Gamma Gain (lift only) and a Vignette. It takes the same moves as the concept frames' `post()`:
  - day: slightly desaturated with a little haze (concept A);
  - 18:00: a warm filter instead of the strong orange wash (concept C);
  - dawn and dusk violet, night cool and less saturated;
  - rain and storm colder and darker, fog kept grey.
- **Numbers** live next to the wash keyframes in `Atmosphere.Day` (`Sat`, `F`, `Haze`, `Vig`), with weather modifiers in `Atmosphere.Look`. Saturation never goes below 0.65, so faction colours stay readable.
- **Dusk toned down:** the 18:00 wash went from 255,168,72 at .17 to 226,172,112 at .11, and 19:30 from .32 to .28.
- **Fallback:** the wash quad stays. It runs at half strength while the grade is on and at full strength without URP. **F8** (editor) turns the grade off and on, to compare.
- The camera gets `renderPostProcessing = true`. If nothing changes in Unity, check that Post-processing is enabled on the 2D Renderer asset.
- `weather_mock.py` and `world_shots.py` apply the same grade (an approximation of URP's maths); `GRADE=0` gives the old look.

### Tests
- **Grade:** warm at 18:00, cold in the rain, less saturated at night than at noon.
- **Readable:** saturation stays at 0.65 or above for every hour and weather, and underground.
- **No URP:** in the harness the grade stays off and the wash works alone.
- Not yet checked in Unity: the real URP result, and whether the haze lift needs tuning.

## Batch 36 - A-Life on the clock

Yan: "move on to the a-life stuff" (step 2 of his order in Batch 35).

- **Hourly tick** (`SquadManager.TickHour`): the off-screen world (squads moving between zones, scavenging, fights between hostile squads, pruning) now runs once per game hour from `Atmosphere.Advance`, instead of once per zone crossing. Walking, combat rounds, the F10 dev key and the 8 hours on the road all count.
  - The odds are spread over the hours so a crossing feels the same as before: 35% to move on per crossing is now 5.2% an hour, and 15% to scavenge is 2% an hour (`MigrateChancePerHour`, `ScavengeChancePerHour`).
  - Squads are pruned after 2400 ticks (100 days) instead of 300, since a tick is now an hour.
- **The player's zone is live**, so the hourly tick leaves it alone: nothing moves into it, out of it, fights inside it or gets pruned from it while you're there. Step 3 (squads walking in through the exit they took) will open it up.
- `ZoneTravel` no longer calls the tick itself; the road's 8 hours do it 8 times.
- **Quiet log:** the hourly news (`[MOVEMENT]`, `[OFFSCREEN WAR]`) goes to the Console only (`GameLog.Quiet`), so it doesn't flood the on-screen log. Step 4 will bring it to the player as radio chatter and rumors.

### Tests
- **Road:** 8 hours on the road are 8 A-Life ticks.
- **Off-screen:** a squad in another zone moves on (or dies) as the hours pass.
- **Live zone:** a squad in the player's zone stays put, alive, in the list.

## Batch 37 - squads walk in and out of your zone

Yan: "have squads actually move in world and come in through whatever transition zone they took" (step 3).

- **Coming in** (`SquadManager.ArriveLive`): when the hourly tick moves a squad into the zone you're in, its members appear at the exit that leads back to the zone they came from, then walk about 10 tiles inward before their usual wander. They arrive unaware of you, like anyone else.
  - Nobody arrives through an exit within 15 tiles of you (the one you just crossed through, or one you're standing by).
  - Not in Safe Zones, and only when that zone's map is the one loaded (`TerrainMap.ZoneId`, new).
- **Going out** (`SquadManager.DepartLive`): squads in your zone move on at the same 5.2% an hour, but on foot. The squad picks an exit its faction may go through (`OfflineSquad.LeavingFor`). Its members walk there as part of their wander (`TurnLoop.CheckVisionAndWander` asks `LeavingExit`) and step off the map at the exit (`StepOut`). The last one out moves the squad.
  - Nobody leaves during a fight, and a squad that spots you fights instead of leaving.
  - A squad that hasn't made it out within 2 game hours just goes (a `ponytail:` note: it can vanish in view if its exit is unreachable).
  - If you leave the zone first, they finish leaving off-screen.
- The 8 hours on the road now play out in the zone you arrive in too: squads may come in or set off while you travel.
- The soak test dies a bit more often (41 restarts against 36 in Batch 36): zones now fill up while you're in them.

### Tests
- **Arriving:** a squad arrives through an exit; the newcomers stand by the exit they came by and have somewhere inward to go.
- **Leaving:** a leaving squad walks to a different exit, steps out there, and is moved to that zone.
- **Off-screen tick:** with the zone's map not loaded, squads there don't move.

## Batch 38 - off-screen news from real events

Yan: the off-screen news "need to be connected to actual world events".

- **News log** (`MacroState.WorldNews`, saved, about 30 kept): only `SquadManager` writes it (through `WorldEvents.News`), and only for things the A-Life tick really did:
  - **F:** an off-screen fight that wiped a squad out, with the zone, the two sides, who was wiped out, and whether their gear was left as War Spoils (that loot object already existed since Batch 10). A standoff isn't news, because it's fought again the next hour.
  - **M:** a squad moving between zones.
- **Radio:** a fight that ends a squad in a zone connected to yours comes over the radio straight away, at most once a game hour ("[RADIO] Gunfire next door. Raiders squad and a pack of mutants fought in ...").
- **Rumors:** at a Safe Zone, the rumor pool now also holds the fights from the last 3 days and the latest squad seen moving, word for word from the news log, next to the existing goal, caravan, migration, Surge and hidden-way rumors.
- Not done yet: bodies left after off-screen fights (the members are just removed).
- Found while checking: enemy AI only ever targets the player (`TurnLoop.cs`, `GetNearestHostile(enemy, new[] { player })`), so hostile squads on your map ignore each other. That's next (Yan: "the world needs to be alive by itself without the player").

### Tests
- **News:** a squad wiped out off-screen next door is news.
- **Radio:** it comes over the radio.
- **Rumor:** it turns up word for word as a rumor at the next telling.

## Batch 39 - factions fight each other on your map

Yan: "yes, the world needs to be alive by itself without the player, mutants should also be hostile to everyone (minus one faction i think, can't remember which)".

- **NPC against NPC** (`NpcFights.cs`): an NPC that isn't fighting you looks for the nearest hostile NPC it can see (vision x the weather's `SightFactor`, line of sight; either side's hostility counts, since the matrix isn't symmetric). It closes in, 2 tiles a beat, and attacks with the usual `HumanEnemy.Attack` or mutant `Attack` (ammo, jams, reloads, cover and damage all apply).
  - A beat is each tile you walk (`TurnLoop.TickAwareness`) and each enemy phase (`TurnLoop.PlayerEndTurn`); NPCs friendly to you take part in the enemy phase too.
  - Enemies already fighting you stay on you.
- **Who takes part:** only A-Life squad members. Quest targets, story bosses and named mutants are never killed by someone else, so quests and the story can't be broken by it.
- **Kills:** the normal death hooks run (morale shakes squadmates, the human's gear stays on the body as corpse loot). Reputation, quest and kill credit stay player-only, as `Weapon.OnTargetKilled` already intended.
  - Fixed: a mutant killed by anyone used to drop its parts straight into your pack; now only your kills do.
- The NPC attack lines now name the real target instead of always saying "Player".
- **Mutant hostility:** the matrix already makes Mutant hostile to every faction, Obelisk included. Yan thinks one faction should be exempt; asked which (not changed yet).

### Tests
- **Live fight:** a Raiders squad and a mutant pack placed near each other, far from you, fight until one side is wiped out, with no input from you.
- **Bystander:** their fight doesn't hurt you or put anything in your pack.

## Batch 40 - mutants spare Obelisk; first A-Life simulations

Yan (decision card): mutants are hostile to everyone except **Obelisk**.

- **Hostility** (`FactionDatabase.HostilityMatrix`): Mutant no longer lists Obelisk and Obelisk no longer lists Mutant, so they ignore each other on the live map and off-screen. Crimson Woods and the Antenna Grid hold both.
- **Off-screen fights now count either side's hostility**, like the live map (`NpcFights`). Before, it depended on which squad was first in the list: a Raiders squad listed before a Wanderers squad never fought it, because only the Wanderers list the Raiders. One-sided pairs: Wanderers, Prospectors, Cartel and IZC list Raiders; Outcasts list Sentinels and Liberators.
- News grammar: "an Obelisk squad", "an Outcasts squad".

### Simulations (scratch program, not shipped; player idle in the Hub, game clock running)
- **All surface zones populated, 30 days:** 68 squads (209 people) fall to 52 after day 1, 38 by day 10 (111 people), then barely change. Nobody new ever appears off-screen: zones are populated only on the first visit (`EnsureZonePopulated`), and no squad spawns later. After the early fights, the squads left in each zone aren't hostile to each other, and the matrix plus `CanEnter` (a squad can only enter zones that list its faction) keep enemies apart, so the world goes quiet.
- **Only the two starting zones visited:** 4 squads in all of the map; nothing anywhere else until you walk in. The news is all "seen heading" lines between the Perimeter and the Marshes.
- **Off-screen 3v3:** mutants (tier 2-4 in a tier-3 zone) beat Raiders 91% and Rookies 92%; Sentinels beat Raiders 83%; Garrison beat Outcasts 78%; Wanderers beat Raiders 63%. A fight is usually over in 1-2 hours.
- **Faction goals:** 2-3 of the 7 tracks complete within 30 days.
- **Weather drift while staying put:** Overcast 34%, Rain 31%, Clear 19%, Storm 14%, no fog (fog only comes from zone entry and sleep).
- **Harness numbers:** fight sim and duels are unchanged from before Batch 35. The soak restart count is a crude random walker, not a difficulty measure.
- Squads may migrate into Safe Zones (The Perimeter lists Rookie and Raiders); they aren't loaded there, so they idle out of sight until they move on.

### Tests
- Mutants are hostile to every faction but Obelisk, and Obelisk to none of them.
- Raiders listed before Wanderers still fight them off-screen; the news says "an", not "a Obelisk".

## Batch 41 - the Zone fills itself

Yan (decision card): "Seed and refill". The Batch 40 simulations showed an empty world: zones only got squads on your first visit, nothing ever spawned later, and after the first fights everything went quiet.

- **Seeded at New Game** (`SquadManager.SeedWorld`, called from `GameBootstrap.StartNewGame` after the first zone loads): every hostile surface zone gets its usual squads right away, not on your first visit. Safe Zones and underground zones are unchanged (still first visit).
- **Refill** (`SquadManager.Refill`, in the hourly `TickHour`): a zone below its quota (`Quota`: the old hazard + map size formula) has a 1-in-24 chance an hour to get a new squad of a faction it allows, so about one a day. It's news ("... was seen heading from X into Y"), so it reaches rumors. Your own zone is never refilled directly; it only gets squads that migrate in through an exit (Batch 37).
- `EnsureZonePopulated` now uses the shared `Quota` and `Spawn` helpers (same numbers and hazard 4+ aggro as before).
- **Revisit hazard check**: changed here, then removed in Batch 42.
- News and arrival lines use the same wording helpers ("A pack of mutants comes in from...", "An Obelisk squad...").

### Simulation (60 days, player idle in the Hub)
68 squads (about 200 people) dip to about 53 in the first days, then hold at 55-61 squads (150-175 people) for the rest of the 60 days. Every zone keeps 1-5 squads, and off-screen fights keep happening (33 radio lines from fights next to the Hub).

### Tests
- Every hostile surface zone has squads at New Game; Safe Zones aren't seeded.
- An emptied zone fills back up, and it's news.
- No zone goes above 6 squads over 20 days.
- The Batch 38 rumor test now clears the rest of the (now populated) world first, so its fight is the only news.

## Batch 42 - no more tabletop spawn rolls

Yan: "the hazard check might need to be dropped completely, it makes no sense because the world has 'organic' movement, encounters should be natural from the squads and mutants wandering around following their tasks exactly like a-life ... That mechanic is a left-over from when this was a tabletop game".

- **Removed `EncounterGeneration.CrossingHazardCheck`**: a d100 on re-entering a zone (automatic at hazard 4+) that put 2-3 enemies (or a boss) at a random map edge.
- **Removed `EncounterGeneration.TickAmbientSpawns`**: every 10th call (it was called twice per step, so about every 5 steps) a 10% + 5%-per-hazard roll put 2-3 enemies at a random map edge. These weren't squads: not simulated off-screen, not saved, and gone when you left.
- Enemies on your map now only come from A-Life squads: the zone's own, and those walking in through exits (Batch 37), with zones seeded and refilled by Batch 41.
- `GenerateEncounter` stays (quests and the story use it).

### Tests
- The old "hazard 4+ = automatic encounter" test is replaced: walking 200 steps in a hazard-4 zone, every enemy on the map belongs to an A-Life squad.

## Batch 43 - reinforcements walk in from home; no pre-aggroed squads

Yan, on the tabletop-leftovers audit: "you can start, do all of them in the best order you can". This is the first group, the squad foundations.

- **Reinforcements come from somewhere** (`SquadManager.Refill`, `Homes`, `Route`): a zone below its quota no longer gets a squad created inside it.
  - The faction musters one at a home zone. Bases are hand-picked from where each faction is allowed; mutants come from their nests, the zones over the underground labs and caves.
  - The squad gets `OfflineSquad.Heading` and walks there one connected zone at a time, through zones its faction may enter (BFS `Route`), about one zone per 8 hours (`TravelChancePerHour`).
  - On the way it fights, shows up in the news ("set out from X, heading for Y", then each real hop), and comes in through an exit if its route crosses your zone (and leaves through the exit toward its next hop).
  - `Heading` is saved with the squads. Squads in transit don't count toward the per-zone prune cap.
  - Known gap: Ghost Town's Raiders are a cut-off pocket (no zone around it lets Raiders in), so Ghost Town is only reinforced by Contractors.
- **Quota uses the zone's fixed base hazard**, not `WorldEvents.Hazard()`, so events and goals no longer conjure squads by changing a number.
- **No squad spawns already aggroed.** "Hazard 4+ = guaranteed encounter" was a tabletop rule; squads find you by vision now.

### Simulation (60 days, idle in the Hub)
The world holds 53-60 squads (157-185 people) throughout; every zone keeps 1-6 squads.

### Tests
- No squad starts aggroed.
- Every allowed faction can reinforce every zone from a home, except the known Ghost Town Raiders pocket.
- A refill for the Abandoned City musters at its faction's home, not in the city, and walks there zone by zone along a valid route.
- Zone crowding is checked without squads in transit.

## Batch 44 - quest targets and rivals are real squads

From the tabletop-leftovers audit (Yan: "do all of them in the best order you can").

- **Quest targets** (`QuestSystem.SpawnTargets` → `SquadManager.PlaceQuestSquad`): when you take a Kill or Clear job, its targets become a real squad in the quest zone right away (`OfflineSquad.QuestId`). Before, they appeared 10+ tiles from you the moment you entered.
  - If you're standing in that zone when you take the job, they walk in through the exit farthest from you.
  - They can be in a Safe Zone (some storyline jobs need that); `EnterZone` puts quest squads on the map there.
  - A save from before this batch still gets its targets on entering the zone.
- **Rival complication** (`QuestSystem.SendRival` → `SquadManager.SendQuestSquad`): instead of two rivals dropped at random spots on entry, a Raiders or Wanderers crew sets out from its home toward the quest zone.
  - It travels like any reinforcement (news, fights, in through an exit) and holds there once it arrives.
  - If neither can reach the zone, a local faction's crew goes, possibly from its base in that very zone; if nobody can, the log says so. No rivals for Safe Zone jobs. Their squad's `QuestId` is `rival:<quest>`.
- **Quest squads hold their ground**: they don't wander, aren't pruned, and sit out off-screen and on-map NPC fights, since a quest only counts your kills (`ponytail`: a "someone got there first" outcome would be the upgrade). When the job is done they become ordinary squads.
- `QuestId` is saved with the squads, and quest squads keep their full state (names, gear).
- Fixes found on the way:
  - Quota is capped at the prune's 6 squads per zone (big maps had a quota of 7).
  - A Safe Zone you're standing in now counts as off-screen for the hourly tick, so squads can pass through The Perimeter while you're there instead of waiting forever.
  - Off-screen fights never happen in Safe Zones.

### Tests
- The old rival test (two units named "Rival") is replaced: a rival crew sets out from its faction's home (or is already at its base in the quest zone), only once.
- Quest targets are a squad in the quest zone.

## Batch 45 - caravans and mutant migrations travel for real

From the tabletop-leftovers audit.

- **Caravan** (zone event 9-10, `SquadManager.SendCaravan`): a Wanderers trade caravan now sets out from The Perimeter (the Zone's edge) for the nearest hub. Before, the hub simply got extra stock.
  - It travels like any squad: news, fights, and in through an exit if it crosses your zone.
  - When it arrives, that hub gets its extra stock for the day (`CaravanZoneId`, now cleared at each new Day; before, it was never cleared).
  - If it's wiped out on the road, no stock comes.
- **Mutant migration** (zone event 11-12, `SquadManager.SendMigration`): 1-2 real packs come up out of the nearest underground zone (labs, caves, tunnels), cross the chosen zone next to you, and go down into another underground zone.
  - Before, it only added +1 hazard and "Mutant" to that zone's list for two days; both are removed.
  - The underground serves as the nests because the Scavenger's Hub splits the surface in two and nothing roams through a Safe Zone, so the lower half had no surface nest to come from.
- **Roaming squads** (`OfflineSquad.Job`, saved): caravans and migrating packs may cross any zone that isn't a Safe Zone (a caravan may enter its hub), not only where their faction is usually allowed (`Route(..., roam: true)`).
- Test fix: the crypt-lid test guaranteed its DC 18 roll with Strength 40, which still failed on a natural 1-2; it's now 100.

### Tests
- A caravan sets out from the Perimeter with no stock yet, then either arrives (and the hub gets its stock) or dies on the road.
- Migrating packs come up from underground heading for the zone; the zone's hazard and faction list don't change; a pack reaches it (or dies trying).

## Batch 46 - faction goals move on who holds the ground

From the tabletop-leftovers audit. Replaces file 10's daily d6 "fill a box".

- **Each goal is a zone to hold against named rivals** (`WorldEvents.GoalGround`):

  | Faction | Zone | Rivals |
  |-|-|-|
  | Obelisk | Quarry Basin | Contractors |
  | Contractors | Ghost Town | Raiders |
  | Raiders | Black Valley | Liberators |
  | Sentinels | Vanguard Barracks | Liberators |
  | Liberators | Vanguard Barracks | Sentinels |
  | Garrison | Siege Ward | Obelisk, Mutant |
  | Surveyors | Anomalous Epicenter | Obelisk, Mutant |

- **Every new Day** (`TallyGoals`): a faction with more squads there than its rivals fills a box; one with fewer loses a box. One that isn't winning sends a squad from home to take it (`SquadManager.SendGoalSquad`): it roams there like a caravan, then digs in (`Job = "hold"`: it doesn't wander, and the 6-per-zone prune pushes other squads out before it).
- **Zone event "faction push"** (13-14): one faction sends an extra squad for its goal, instead of an extra d6 roll.
- Quest boxes (+1 for the faction you work for) and kill losses (-1 after 3 of their members killed by you in a day) are unchanged.

### Simulation (60 days, idle in the Hub)
Four goals completed (Obelisk, Contractors, Raiders, Sentinels). Garrison and the Surveyors were held back by Obelisk's numbers. The Liberators' track is shut once the Sentinels take the Barracks. The world kept 56-62 squads.

### Tests
- Obelisk out-numbering Contractors at Quarry Basin fill a box. Pushed out, they lose one and send a squad to take it back.

## Batch 47 - gunfire and explosions are heard

From the tabletop-leftovers audit: nothing but sight ever alerted anyone.

- **`Noise.Make`** (new `Noise.cs`): every shot (`Weapon.Fire`, anyone's) and every explosion (`Explosions.Blast`) makes a noise. Every NPC on the map within earshot who isn't already fighting and isn't on its way out walks over to the spot to look. Whether it then joins in is down to the usual vision check (you) and `NpcFights` (other factions).
- **Earshot:** 20 tiles for a shot, 6 with a built-in suppressor (TS-9, VSN, VK-127), 30 for an explosion. Rain muffles it to 80%, a storm to 60%.

### Tests
- A shot draws the NPC 15 tiles away to it, not the one 26 away.
- A suppressed shot isn't heard 15 tiles away.
- Firing your gun makes that noise.

## Batch 48 - prisoners, fleeing, morale, scavenging and the double-cross come from the situation

From the tabletop-leftovers audit.

- **Morale break** (`EnemyAI.Break`): there's no d20 any more. An NPC surrenders when it can't get away, meaning you're within 3 tiles or it's down to a quarter of its HP or less. Otherwise it runs. Legends always run.
- **Fleeing** (`TurnLoop.ResolveFleeReactions`): there's no d20 and nothing goes into your pack. A fleeing NPC throws down a heavy weapon, an empty one, or everything if it's badly hurt. The weapon and its pack land on the ground where it stood (`HumanLootSystem.DropOnGround`, `TerrainMap.DropItem`); walk over them to pick them up.
- **Interrogation** (`EnemyAI.Interrogate`): a success no longer hands you a stash item rolled out of nowhere. The prisoner gives up the nearest other squad of their faction: where it is, how many, and the best thing it carries. That goes in your rumors marked true (`SquadManager.NearestSquad`). The hidden-way tip is unchanged, and so is the Int/Str roll (it's a social check, which is fine).
- **Scavenging** (`SquadManager.Scavenge`): squads no longer gain rolled items out of nowhere (`ScavengeTick`, 2% an hour, is gone). A human squad in a zone where War Spoils from an off-screen fight are lying has a 25% chance an hour to strip them. The spoils disappear and their contents go into a member's pack (you'll find them on the body), and it's news.
- **Double-cross** (quest complication 4): no d20 vs 13. Being Friendly with the giver gets you full pay; otherwise you get half.

### Tests
- Cornered, it surrenders; with room to run, it runs; too hurt to run, it surrenders.
- A fleeing NPC with an empty gun drops it and its pack on the ground, and your pack is unchanged. A healthy one with a loaded light gun keeps it.
- A prisoner gives up where their people are.
- A squad strips the War Spoils in its zone.

## Batch 49 - anomalies, artifacts and cloak are spatial

From the tabletop-leftovers audit.

- **Anomalies: seeing them is the protection, not a dodge roll** (`AnomalyEngine`).
  - Walking into a field now always hurts. The Agility + detector dodge roll is gone; the artifact auto-pass still works.
  - Every step, your detector reveals fields within its range, the file's "cm" read as tiles (`DetectRange`): Clicker 3, Sniffer 6, Dowser 9, Seer 15. Without a detector you only catch the tile next to you ("the air ahead shimmers wrong"). The Anomaly Whisperer perk adds 2, and artifact avoidance adds its value.
  - A newly revealed field stops a click-walk (`PlayerController.Step`), and click-paths route around fields you know about (`Pathfinding`).
  - NPCs no longer trigger fields. The Zone's own know where they are, and the dodge roll was their only protection.
  - Pyre's old "natural 1 burns a medkit" is kept as a 5% chance.
- **Artifacts are really in the fields** (`Anomaly.Artifact`, `SeedArtifact`).
  - A field gets one when it spawns: 35%, plus 10% per point of the zone's search bonus (the Surveyors' Epicenter goal, the story's bonus).
  - With a detector, revealing a field tells you whether something's in it.
  - Searching (F, 2 AP, next to a revealed field) takes it, with no d20 + Perception vs DC 15. An empty field gives nothing.
  - A Surge refills its zone's fields (60% each). An anomaly shift may add one, and the fields now drift up to 3 tiles instead of teleporting anywhere.
- **Cloak** (`Mutant.CloakSpotRange`, `Vision.Shows`): a cloaked mutant beyond 3 + your Perception modifier tiles is no longer drawn or targetable, and there's no Perception roll. That range is shortened by weather and dark like everyone's sight, with a minimum of 1. Before, it was drawn but needed a roll to click.

### Tests
- Bare senses catch only the next tile; a Clicker reveals within 3.
- Searching takes the artifact lying in the field, with no roll; an empty field gives nothing.
- Walking into a field hurts.
- NPCs aren't hurt by fields.
- A click-walk routes around a known field.
- A cloaked mutant is hidden beyond your spotting range and seen up close.

## Batch 50 - hidden ways and quest clues are places

From the tabletop-leftovers audit.

- **Hidden ways** (`ZoneTravel.SenseHiddenWays`/`SearchArea`). A hidden way is now a spot on the map, where its gate will open (`ZoneExitPlacer.GatePos`), instead of a Perception roll from anywhere in the zone.
  - Walking within your sensing range of it finds it: 3 tiles + Perception modifier, minus 1 per point the old DC was above 14, at least 1, and +3 if you've heard a rumor about it.
  - F looks twice as far. If nothing's in reach but the spot is within 30 tiles, it says which way ("the ground to the north-east looks disturbed").
- **Investigate jobs** (`QuestSystem.SenseClues`/`TryInvestigate`). The job's clue is a spot in the quest zone, placed the first time you're there (at least 15 tiles from you) and saved on the objective.
  - Walking within 2 + Perception tiles finds it and completes the objective. F looks twice as far, otherwise points the way within 30 tiles.
  - The Perception roll on F is gone.

### Tests
- Far from a hidden gate nothing happens, F from 20 tiles gives the direction, and walking to it finds it.
- An investigate job puts a clue on the map, and walking to it completes the objective.

## Batch 51 - the last small rolls, and the audit is done

From the tabletop-leftovers audit (items 17 and 20). Yan: "you can start, do all of them in he best order you can".

- **Cloaked mutants** (`Mutant`): the 20% "melts into the shadows" line is gone (it only printed text), and the 15% coin flip on slipping back after a strike is now always: a cloaked hunter hits and pulls back every time.
- **Failed interactions** (`InteractionEngine`): no more 25% chance of 1-5 damage from anything. A failed lock bypass sounds an alarm (heard 20 tiles, `Noise`); failing to force something with Strength clangs (10 tiles). A failed PDA costs nothing but the 2 AP.
- Test fix: the b48 flee test now makes sure the runner has a gun (a melee loadout made it fail once the random sequence shifted).

Kept on purpose (audit items 22-25): the named NPC "wants to talk" line, rumors that can be wrong, the Surge timing, and one-off DC checks like terminals and lids.

### 60-day simulation after Batches 43-51 (player idle in the Hub)
- 53 to 68 squads, 164 to 213 people alive, 17 to 19 zones held. Every hostile surface zone ends with 1 to 6 squads.
- 4 faction goals done (Contractors, Raiders, Obelisk, Sentinels); Liberators, Garrison and Surveyors make no progress.
- Obelisk is a third of the world (23 to 28 squads), since mutants leave it alone. Raiders shrink to 2 to 5 squads.
- Still unreachable: Raiders can't walk to Ghost Town.

### Tests
- A failed lock bypass draws an NPC 15 tiles away and hurts nobody.

## Batch 52 - real rank decks, two-way hostility, faction outposts

Yan (decision card): "Real decks". Then: "factions that are hostile should be hostile both ways / Obelisks are enemies to everyone but themselves and mutants / ... each should have it's own outpost where the new squads spawn from before wandering the zone / this can be decided based on the factions ''strenght'' (basically how many high tier units they can have at one time) / the farther north the stronger the enemies and factions".

- **Rank decks** (`FactionDeck`, `FactionDef.Roster`). Each faction has a fixed roster split into rank cards by its old weights: Raiders 36, Wanderers 30, Garrison 24, Obelisk 24 (exactly 1 Legend), Rookie 24, Contractors 20, Sentinels 18, Liberators 14, Surveyors 10 (and 20 each for the four factions no zone uses yet).
  - A new squad member is a card drawn from what's free: the roster minus everyone of that rank alive minus the recently dead. A faction with no cards left sends nobody (a squad can come out smaller, or not at all).
  - A dead unit's card comes back after 24 h (`CooldownHours`); the hourly tick notices who's gone.
  - Mutants aren't a deck (they breed; zones refill them by quota). Quest targets aren't drawn but hold their card while alive.
  - New Game seeds a squad per zone per round, so a faction's deck is spread over all its ground.
- **Hostility goes both ways** (`FactionDatabase.AreHostile`): either side listing the other is enough. Obelisk is now hostile to every faction but Mutants.
- **One outpost per faction** (`SquadManager.Homes`), placed by strength = its Veteran-or-better cards (`FactionDeck.Strength`): 0-4 -> tier 1-2, 5-9 -> tier 3-4, 10-14 -> tier 5-6, 15+ -> tier 7. Rookie Perimeter (0), Raiders Scrapyard (4), Surveyors Dried Lake (6), Liberators Vanguard Barracks (7), Wanderers Scavengers Hub (8), Sentinels Rail Yards (9), Contractors Quarry Basin (11), Garrison Hospital (11), Obelisk Reactor Core (18). Mutant nests unchanged.
  - Squads travel from their outpost through any zone, Safe Zones included (no fights there), so every zone can be reinforced. Raiders reach Ghost Town now.

### 60-day simulation (player idle in the Hub)
- 38 to 57 squads, 102 to 169 people alive (was 53-68 and 164-213): with everyone at war both ways, humans die faster than a zone refills (once a day). 0 to 4 zones are empty at a time.
- Mutants, not capped by a deck, become the biggest group (up to 47). Obelisk stays at 6 to 23 people, Garrison often near 0.
- A faster refill (every 8 h) and a 72 h cooldown were tried; neither changed the picture much. Kept the agreed once-a-day refill and a 24 h cooldown.

### Tests
- Hostility is two-way; Obelisk is hostile to every faction but Mutants.
- Obelisk never has more of a rank than its deck holds (one Legend); an empty deck sends nobody.
- A dead unit's card is back after the cooldown, not before.
- Each faction has one outpost in the tier band its strength gives.
- Every zone can be reinforced from its faction's outpost.

## Batch 53 - Yan's faction tiers, Rookie is a rank, NPCs grow past their card

Yan: "rookie is supossed to be a rank, not a faction ... based on faction strenght they should be in this order: S tier: Obelisk (like 2 tiers above the others) / A tier: liberators=sentinels, contractors, garrison / B tier: wanderers (wildcard, can have high and low tiers), surveyors / C tier: Raiders". And: "these are the SPAWN rules, but any faction can have higher tier npc's if they survive long enough and loot other higher tier enemies, if a raiders ends up in the hospital fighting obelisk soldiers and loots them he's a legend technically".

The factions mirror a well-known Zone shooter's (Yan): Wanderers = loners, Raiders = bandits, Surveyors = ecologists, Liberators = freedom, Sentinels = duty, Contractors = mercenaries, Garrison = military, Obelisk = the monolith cult.

- **No Rookie faction.** The three south zones that had Rookie squads now have Wanderers (rookie-heavy, it's the wildcard deck). Old saves map "Rookie" to Wanderers.
- **Decks retuned** (`FactionDatabase`). Strength = Veteran-or-better cards:
  - C: Raiders 4 (36 people, no Masters or Legends).
  - B: Surveyors 7 (12), Wanderers 9 (36, every rank from 11 Rookies to 1 Legend).
  - A: Liberators 14 and Sentinels 14 (26 each, same deck), Contractors 13 (22), Garrison 12 (26).
  - S: Obelisk 25 (32 people, 2 Legends, 5 Masters), about two A-tier steps above.
- **Outposts by tier** (`SquadManager.Homes`, `OutpostTiers`): C tier 1-2, B tier 3, A tier 4-6, S tier 7. Raiders Scrapyard, Surveyors Dried Lake, Wanderers Scavengers Hub, Liberators Vanguard Barracks, Sentinels Crimson Woods (Sentinels now also allowed there: an anti-mutant order in the mutant woods), Contractors Quarry Basin, Garrison Hospital, Obelisk Reactor Core.
- **Growing past the card** (`HumanEnemy.GainXp`/`LootFrom`). A kill is worth the victim's rank (Rookie 1 .. Legend 7; a mutant its tier); a rank takes 4 x its place to leave (Rookie 4, Experienced 8 ... Master 24). In off-screen fights the killer also takes the dead man's weapon or armor if it's better. On your map NPCs learn from kills too, but the bodies stay for you to loot. The deck counts the card a unit was drawn as, so a promoted Raider doesn't free or use up a Legend card.

### 60-day simulation
- 28 to 56 squads, 77 to 158 people alive. Off-screen fights almost always end in a wipe within an hour or two, and with Obelisk at war with everyone, people die faster than a once-a-day refill brings them back. Refill every 6 h only lifts it to about 100-124.
- Only 2-3 NPCs are promoted at the end: few live long enough.

### Tests
- Faction strength follows Yan's tiers; Rookie is only a rank.
- A Rookie raider who kills an Obelisk Master moves up and takes his gear; enough kills make a Legend.
- A promoted NPC still holds the card it was drawn as.
- Each faction's outpost is in its tier's band.

## Batch 54 - beaten squads retreat

Yan (decision card): "Retreat" - losing squads break off instead of fighting to the last man.

- **Off-screen fights** (`SquadManager.ResolveSquadSkirmish`): a side down to half the people it started with, and outnumbered, breaks off. It heads for its outpost (mutants: the nearest nest), or a neighbouring zone if it's already home (`Retreat`, `Job = "retreat"`).
  - A retreating squad isn't fought again and moves every hour until it gets there (`TickSquadMigration`); then it's an ordinary squad again (`OnArrived`).
  - The news says so: "A Sentinels squad and a pack of mutants fought in Crimson Woods. The Sentinels fell back, leaving it to the mutant pack."
- Test fixes: three older tests made their own squads from decks that the seeded world had already used up; they now clear those factions' squads first.

### 60-day simulation
- 40 to 61 squads and 101 to 148 people alive, rising over time (Batch 53 without retreats: 77 to 116 and falling). 4 faction goals done.
- Still thin: Garrison is wiped out and can't rebuild. Its outpost is the Hospital, deep in Obelisk ground, and reinforcements for Industrial Basin have about ten zones to cross. Industrial Basin, Black Valley and Ghost Town end empty.
- Few promotions yet (3 of 110 alive).

### Tests
- A beaten squad falls back to its outpost alive and isn't fought while it goes.

### Batch 54, part 2 - fixes found by a simulated playthrough

Yan: "try simulating a playthrough where the player goes from the starting point to the trading hub, completing missions along the way. What happens in the world during that, notable events, encounters..." A bot (scratch `tools/Play.cs`, not shipped) walks Perimeter -> Toxic Marshes -> Scrapyard -> Black Valley -> Scavenger's Hub tile by tile, takes local jobs and fights what spots it. It found three bugs:

- **Walks stalled on ledges** (`Pathfinding.FindPath`): the climb cost was measured from where the unit stood, not from the tile it would step from, so paths went up ledges taller than a whole turn's MP and the walk stopped at the foot for good (Black Valley's way to the Hub). The climb is now from the previous tile, and a step that costs more than the unit's max MP is a wall.
- **Walks stalled on rubble** (`PlayerController.Step`/`WalkToward`): exploring with 1 MP left in front of a 2-MP tile stopped the click-walk until you pressed End Turn. Out of combat the turn now rolls over by itself, as it already did at 0 MP.
- **Quest targets from the wrong faction** (`QuestSystem.SpawnTargets`): it took the zone's first listed faction, so after Batch 53 the Marshes' "Caravans keep getting hit" sent you after Wanderers. It now picks the local faction you're on worst terms with (Raiders there).

### Tests
- No path up a ledge taller than a turn's MP.

### Batch 55 - Raider gangs

Yan: "About Raiders, should we up their spawn rate maybe? they don't need to be a strong faction but they bunch up in higher numbers to compensate?"

- **Bigger roster, same low ranks** (`FactionDatabase`): Raiders go from 36 to 60 people, weighted toward the bottom: 27 Rookies, 20 Experienced, 10 Professionals, 2 Veterans, 1 Expert. Strength (Veteran-or-better cards) is 3, so they stay C tier.
- **Gangs** (`SquadManager.Spawn`): Raider squads are 4 to 6 strong; everyone else stays 2 to 4.
- 60-day sim: Raiders are now the most numerous faction most of the time (14 squads, 35 to 52 alive), where before they were often wiped out by day 3. Two Raiders earned promotions by day 60.

### Tests
- The exit-walk test now clears other squads from the map first, so a nearby fight can't kill a walker.

### Batch 56 - 2d10 hit tiers; grenades for Veterans and bosses

Yan: "what about anything below the 10ac base is a miss, but above it there are tiers to the damage dealt? like 10 to 13 could be extremities with a -30% damage multiplier, 14 to 16 does 100% normal damage, 17 to 19 does 20% more damage and a natural 20 (not boosted by anything) is a critical with 150% damage? try that against your 2d10 suggestion" ... "2d10 sounds good, lets do it" ... "only veteran or higher tier enemies should use the grenade method, lower tier do not have access to those kind off equipment (minus maybe for bossfights, bosses get 1 grenade to surprise the player early...)"

A scratch dice model (lone player vs 2 equal enemies) compared the options: the player's win rate going from +0 to +6 to hit rose 24 points with the old d20, 29 with the tiers on a d20, 33 with plain 2d10 and 53 with the tiers on 2d10.

- **Attack rolls are 2d10** (`DiceRoller.Roll2D10`, `CombatResolver.RollAttack`): every attack (player, NPCs, mutants, off-screen fights, shooting objects). Skill checks and other rolls stay d20.
- **Hit tiers** (`CombatRules.HitMultiplier`): 0-3 over AC is a graze (x0.7), 4-6 a clean hit (x1.0), 7+ a vital hit (x1.2). A natural 20 (both dice 10, 1%) is a headshot: always hits, x1.5 (was x2 at 5%). Wounds still trigger on it.
- **Jams** (`CombatRules.Jams`): a natural 2-4 always jams (6%, was 5%); bursts and Dirty/Damaged guns also jam on a 5 (10% total, as before).
- **Controlled Taps** refunds on 14+ (about the same 28% as 15+ on a d20).
- **Grenades** (`HumanEnemy`, `QuestSystem.SpawnTargets`): frags and the smoke/flashbang extra are Veteran-and-up gear (was Professional). A quest's boss always carries one frag.
- 60-day sim: world stays at 104-142 people. The bold playthrough bot still dies in the Marshes to the Raider boss and its gang (the tier-1 boss gun question is still open).

### Tests
- Graze, clean, vital and headshot multipliers; 2d10 range and bell curve; jam rules on 2d10; only Veterans and up carry grenades.

### Batch 57 - aimed shots; grazes x0.85

Yan: "change the x0.7 to 0.85, maybe the 30% debuff is too much" ... "what if we had a targetting system like fallout in VATS or xcom? where you can focus on limbs to debuff enemies" ... "Started with the targeted shots, give me ideas for debuffs applied" ... "leave the bosses as they are, we need to give the player ways to deal with them not nerf the enemies. The game needs to have a fight for survival feeling to it, similar to getting overwhelmed in Space Marine 2 or Helldivers 2 but still coming out on top"

- **Grazes** (`CombatRules.HitMultiplier`) are x0.85 (was x0.7).
- **Aimed shots** (`Player.CycleAimedPart`, key L; `Weapon.Fire`; `CombatRules.ApplyAimed`): pick Legs, Arms, Torso or Head. Single shots only, +1 AP. A graze does damage but no effect; a clean hit or better lands it, using the existing wounds:
  - Legs (-2 to hit): Leg wound, -2 MP. A vital hit also knocks them down (they spend AP getting up).
  - Arms (-2): Arm wound, -2 to hit. A vital hit also jams their gun (1 AP to clear).
  - Torso (-1): Bleeding, 5 HP a turn.
  - Head (-5): the hit counts one tier higher (graze to clean, clean to vital, vital to headshot x1.5) and dazes them, -1 AP next turn.
- Mutants feel aimed wounds too: a leg wound costs them 2 MP and stops a Dread Prowler's leap; an arm wound gives their claws -2 to hit.
- The HUD's fire-mode chip shows the aimed part in yellow; the controls hint lists L.
- Bosses unchanged, on Yan's word.

### Tests
- L cycles parts and back to off; an aimed shot costs 1 AP more; a graze on a limb does no wound; each part's effect; head shots land a tier higher.

### Batch 58 - more aimed shots: disarm, execution, armor, grenade pouch

Yan: "all of them sound good, apply them" (pouch shot, execution shake, armor shot, disarm).

- **Disarm** (`CombatRules.ApplyAimed`, `HumanEnemy.GunDropped`): a vital arm hit now knocks the gun out of their hands instead of jamming it. Picking it back up costs them 2 AP (half their turn).
- **Execution** (`EnemyAI.Execution`): a head-shot kill shakes every squadmate within 8 tiles, seen or not, and anyone already shaken before the shot breaks (runs, or surrenders if cornered). Legends don't shake.
- **Armor shot** (new part, -1 to hit): breaks their armor one Condition step. On a mutant, its hide loses a quarter of its Phys-DR (Blight Brute).
- **Pouch shot** (new part, -4 to hit, `CombatRules.CookOff`): on someone carrying a grenade (Veterans and up, every quest boss), a clean hit or better sets it off on them: the normal 40-damage 3x3 blast, Agility save for half. Works even if the shot killed them. On anyone without one it's a plain shot.
- L now cycles Legs, Arms, Torso, Head, Armor, Pouch, off.

### Tests
- Disarm and the 2-AP pickup; armor shot; execution shakes and breaks; pouch shot blows the grenade.

### Batch 59 - radio calls for backup; no turns while exploring

Yan: "if a fight extends for X amount of turns they call reinforcements? maybe based on the number of enemies? Like if im fighting 3 enemies and the fight extends to 9 turns they call in reinforcements and if there's a squad from the same faction in the zone they respond and get there in a few turns, or if they're in a different zone they take way longer or refuse" ... "yes do that." ... "when not in a combat the player still takes turns when wandering the world, it should be free roam without turns or ap used outside of combat"

- **Radio calls** (`TurnLoop.RadioForHelp`, `SquadManager.CallReinforcements`, `EncounterState.FightRounds`): after 3 rounds per enemy the fight started with, the leader of the biggest human squad still fighting yells into the radio ([ALERT] in the log). If they're still up and steady at the end of the next round the call goes out; kill, break or shake them first and it's cut off. Once a fight.
  - Same faction, same zone: their squads on the map walk to the caller.
  - Same faction, next zone (not Raiders - no discipline): one squad sets out (Job "backup"), moving every hour instead of by chance, and comes in through the exit. A squad walking into a zone mid-fight heads for the player.
  - Squads retreating, on a quest, on another job or already leaving don't answer.
- **Free roam** (`PlayerController`, `TurnLoop.TickAwareness`): out of combat AP and MP are never spent and no turns pass - walking no longer ends turns. Your own upkeep (bleeding, healing over time, smoke, the Surge) runs every full move's worth of tiles, quietly. Space still waits a turn.

### Tests
- The leader radios after 3 rounds per enemy and a same-faction squad next door heads over; killing the caller in time cuts the call off.

### Batch 60 - wounds on vital hits only; radio retries

Yan chose "Vital hits only" on: "Change when hits cause wounds and wear down armor?" (a mid-tier bot run - a Veteran rifleman in tier-3 armor doing tier 3-4 jobs - took 6 to 11 wounds a run and its armor went Working to Broken in every run, because nearly every tier-3 hit is 20+ damage after armor).

- **Wounds** (`CombatRules.HitWounds`, `Weapon.Fire`, `Mutant.Attack`): a gun or claw wounds - and wears armor down a step - only on a vital hit or a headshot. The VKM, which used to wound from 15 damage, also wounds on a clean hit. Blasts (grenades, barrels, the Blight Brute's slam) still go by damage (20+ after armor).
- **Radio** (`TurnLoop.RadioForHelp`): only a steady fighter makes the call (a shaken leader hands it to someone steady), and a call that's cut off is tried again 3 rounds later instead of every round.
- Same bot afterwards: 0 to 4 wounds a run, armor down 1-2 steps; it still died in every run (the Contractors job boss in Rail-Yards, a Dread Prowler in Dried Lake).

### Tests
- Only vital hits and headshots wound (the VKM on a clean hit too).

### Batch 61 - stalking mutants close in

Yan: "why does the prowler just hover instead of attacking?"

- **Cause** (`EnemyAI.MutantTurn`): a cloaked ambusher (Veil/Alpha Prowler) only struck when it could reach a tile next to you this turn; otherwise it circled 4-6 tiles out to keep its cloak. When the only way to you was longer than its MP (you behind a wall, up a ledge, in a nook), it circled forever and the fight never ended. The Dread Prowler did the same waiting for a leap line.
- **Fix** (`Mutant.StalkRounds`): after 2 rounds of circling with no way to strike, it walks the long way in (losing its cloak as it closes) and attacks when it gets there. Striking resets the count.
- A mid-tier bot (Veteran rifleman, tier-3 gear) that now fights from cover at range, focuses the weakest foe, grenades groups, falls back out of sight to heal and hunkers down reaches Crimson Woods in most runs (dying to the Obelisk Expert job boss there).

### Tests
- A cloaked Alpha Prowler behind a wall walks the long way round and strikes (fails on the old code).

### Batch 62 - breaking contact

Yan: "Makes sense, build it like that and run some simulations again, try another build" (on: contact only breaks when nobody has seen you for 3 rounds and you're 15+ tiles from every hunter; they search where you were last seen; Raiders give up fast, Garrison, Sentinels and Obelisk keep looking; packs follow your trail; bleeding gives you away; the squad stays on watch for an hour).

- **Before**: every enemy in a fight knew where you were every round, through walls and at any range. A fight only ended when one side was dead or ran.
- **Sight** (`Contact.Sees`): a hunter sees you at twice its idle vision (16 tiles in clear daylight), cut by night and weather, with line of sight. Anyone who sees you tells the rest. Your gunfire and blasts tell every hunter in earshot (`Contact.Heard` from `Noise.Make`; a suppressed gun carries 6 tiles).
- **Searching** (`EnemyAI.Search`): a hunter that doesn't know where you are walks to where you were last seen, then sweeps tiles around it. If it spots you on the way it fights with what it has left.
- **Trails**: pack and swarm mutants follow your real position for 3 rounds after losing sight. While you bleed they all know exactly where you are.
- **Giving up** (`Contact.SearchRounds`): Raiders after 3 rounds unseen, Garrison, Sentinels and Obelisk after 8, mutants after 4, everyone else after 5.
- **Breaking contact** (`Contact.EndRound`): 3 rounds unseen and 15+ tiles from every hunter ends the fight at once ("You've broken contact"). Whoever gave up walks to your last-seen spot and watches with 1.5x vision for an hour (`Mutant.AlertUntil`).
- Small fixes: the graze multiplier shows as x0.85, not x0.9; the headshot line says "Double 10" instead of "Natural 20".

### Tests
- A hunter who lost you walks to where you were seen.
- Raiders give up after 3 rounds, Garrison keeps hunting and gives up after 8; the ones who gave up see farther.
- A bleeding player is tracked; your gunfire gives your position away.
- 3 rounds unseen 15+ tiles from every hunter breaks contact.

### Batch 63 - jams 1%, +2% per wear step

Yan: "Change it to match a 1% rate of jamming, but increases by 2% each lower durability tier" (bots on b62: about 6% of shots jammed and 3 jams leave a gun Broken, so 4 of 10 runs ended up with a broken gun mid-fight).

- **Jams** (`CombatRules.JamChance`, `Jams`, `RollJam`): a separate d100 per attack, no longer the low attack rolls. Working 1%, Dirty 3%, Damaged 5%. Bursts no longer jam more (so `ImmuneToBurstJams` does nothing now). The BR-86 Bulldog is still finicky: one step worse. Each jam still costs a Condition step; the VK-103 still never wears.
- **Hardened** upgrade: the gun stays at 1% however worn (it used to stop jams on a 5 from wearing the gun).
- The jam log line is "<gun> jams!" instead of "jams on a natural N".
- Bots afterwards (5 seeds each): no gun broke. The marksman build cleared all 4 zones twice; the rifle build once.

### Tests
- Jam chance by condition, Hardened, VK-103, and exactly 5 of 100 d100 results jam a Damaged gun.
- The exit-walk test makes the walkers unkillable (a passing fight killed two of them once the dice order shifted).

### Batch 64 - ambush shots

Yan: "Sure, then run simulations with 5 different builds and tell me what happens in each" (on: a shot at someone who hasn't spotted you lands one hit tier higher, so quiet builds get a real edge from hiding).

- **Ambush** (`Player.Attack`, `Weapon.Fire(..., ambush)`): attacking an enemy that hasn't spotted you moves the hit up a tier (graze to clean, clean to vital, vital to headshot), logged as "<name> never saw it coming". It stacks with a head aim. The shot still makes noise as usual, so a loud gun tells everyone within 20 tiles where you are; a suppressed one only 6.
- **Shot origin** (`Contact.ShotFrom`): opening a fight from hiding tells the enemy where the shot came from, not where you go next. Before, hunters in a fight you started unseen searched from a stale spot.
- Bots: 5 builds (rifle, suppressed marksman, shotgun breacher, SMG runner, LMG gunner) x 5 seeds, all opening from hiding when they can. The marksman cleared all 4 zones in 4 of 5 runs; the others in 1 or 0. Details in tools/playthrough_midtier.txt.

### Tests
- A shot at an unaware target lands a tier higher and sets the last-seen spot to the shooter; an alert target takes normal hits.

### Batch 65 - zoom, longer sight, short-gun ranges, range falloff

Yan: "i like the ideas but im thinking about gun ranges now, maybe they're too short? since we tripled map sizes maybe we should take a look at that" and then "sure, lets do that, just check the guns data first because some weapons in those categories already had higher ranges, i think the rk-12 (pistol/revolver) was 20 or something".

- **Data check**: ranges are tabletop centimetres, 2.5 cm a tile. Before: shotguns 4-6 tiles, SMGs and pistols 6-10 (the RK-12 Kuvalda was 25 cm = 10 tiles, tied for the longest sidearm), rifles 10-20, machine guns 16-20, snipers 18-32. The close camera showed about 12 tiles each side and 7 up and down, and daylight sight was 13, so rifles and snipers already reached past the screen.
- **Short guns +2 tiles** (`WeaponDatabase`, +5 cm each): shotguns 6-8, SMGs and pistols 8-12 (RK-12 now 12). Rifles, machine guns and snipers unchanged.
- **Zoom** (`TacticalGridView.FollowCamera`): the mouse wheel zooms between the old view and twice as far (half-height 7 to 14 tiles).
- **Sight**: your daylight sight 13 -> 20 tiles (night 5 -> 8, fog cap 7 -> 11, never under 6; `Vision.MaxRadius` 20). Enemies see 12 tiles idle instead of 8 (`Mutant.VisionRange`); `SightFactor` now divides by 20, so night and weather cut both the same way as before.
- **Range falloff** (`Weapon.Fire`): past half a gun's range, -2 to hit ("Long range"). Applies to everyone; enemy AI now prefers tiles within half its range when it has a shot.
- Harness stubs gained `Mouse.scroll` and `Mathf.Sign`.

### Tests
- Short guns reach 2 tiles further and long guns are unchanged; a shot past half range gets -2 and a close one doesn't.

### Batch 66 - sneaking and enemy facing

Yan: "sure, do that next" (on: a sneak toggle that halves your speed and how far enemies see you, and enemies that see 8 tiles ahead but about 3 behind and to the sides, so short-range builds can open from hiding).

- **Sneak** (`Player.Sneaking`, `ToggleSneak`, key N): enemies see you at half range, whether idle or hunting (`Contact.IdleSight`, `Contact.Sees`). In a fight every tile costs double MP (`TacticalGridSystem.GetMoveCost`); out of one each step takes 2 game minutes instead of 1 and you walk at half pace on screen. Shows as "Sneaking" in your status.
- **Facing** (`Mutant.Facing`, set on every step in `AttemptMove`): an enemy that hasn't spotted you sees its full range only inside a 120-degree cone ahead of it, and 3 tiles anywhere else (`Contact.InFront`, `Contact.RearSight`). Hunters in a fight look all around as before.
- Bots: they sneak in once within 24 tiles of a job target and try to come at it from behind. 5 builds x 5 seeds cleared all 4 zones 9 times (b65: 7). Breacher 2 (was 1), runner 1 (was 0), gunner 1 (was 0), rifle 3, marksman 2.

### Tests
- Sight in front, behind and while sneaking; sneaking in a fight costs double MP.

### Batch 67 - first playtest fixes

Yan, after testing b35-b66 in Unity: "the caracter runs faster based on the distance of the click and it feels weird / The lighting is bugged in certain times of day / Time of day change needs to be gradual, same with weathers / and i also didn't encounter any enemies at all".

- **Walk pace** (`UnitMotionView.Place`): the sprite sped up by half a tile a second for every queued step past 4, so a far click made you run. The player now walks at one pace however far the click; enemies still catch up on long routes so the enemy phase doesn't drag.
- **Gradual time and weather** (`Atmosphere.Ease`, `TacticalGridView`): what's on screen eases toward the real look (2.5 s time constant), so weather changes, sleeping and F10 fade instead of popping. Entering a zone starts as it is. The flashlight beam now grows in between night 0.15 and 0.35 instead of switching on at 0.25.
- **No enemies**: a fresh game starts in the Perimeter, a Safe Zone (empty by design). A probe of 5 new games found 2-3 squads (5-10 people, Raiders and Wanderers) in each of its neighbours, Marshes and Smugglers Run, the nearest 11-18 tiles from the centre. Asked Yan where he went.
- Lighting at certain hours: not reproduced without knowing the hour; asked Yan for the time and a screenshot.

Then Yan: "npcs only walk when i walk, is that because of how time works? / the character takes weird deviations whenever i click for it to walk, i clicked one place and he walked all the way to the other side of the map, pathing seems broken".

- **Click paths** (`Pathfinding.FindPath`): out of a fight every step costs the same (it's a minute either way), so the route is the shortest one, not one that dodges bushes and doors. The player only routes around people they can see; hidden ones used to bend the path. Ledges too high to climb are still walls.
- **Long way round** (`PlayerController.WalkToward`): if the only route is more than twice the straight distance plus 8 tiles (a wall, a ledge, a known anomaly), the log says "No direct way there" with the length, and a second click on the same spot takes it. An unreachable spot says "You can't get there from here." A probe of 1200 clicks within 15 tiles in 4 zones found 9 such detours, all around buildings.
- NPCs only move when you do: yes, time only passes with your steps. Asked Yan whether the clock should run on its own out of combat.


Yan chose "Clock runs alone" (card: make the clock run on its own while exploring, so NPCs move when you stand still).

- **Exploring clock** (`PlayerController.TickClock`): out of a fight a game minute passes every 1/4.2 s (the time it takes to walk a tile), and each minute runs `TurnLoop.TickAwareness` (enemies look and wander, detector, clues, upkeep, A-Life hours). Your steps no longer move time themselves, so sneaking (half pace) still costs twice the time per tile. The clock pauses while a menu is open; in a fight it's turns as before.
- **Click walking**: a clicked route is walked one tile at a time as your sprite catches up, like held keys, instead of all at once. Getting spotted or pressing a move key stops it.

Yan, with 3 screenshots: "Npc's just move erratica around the map and do not seem to have a purpose, also waaaay too fast, some stuff keeps dissapearing and reapearing / That pack of wood is a watchtower? / The street leads nowhere / The flashlight seems to be inverted, whenever i look up it moves down".

- **Flashlight and vanishing** (`AtmosphereView.DrawLight`): the light layer was written top row first into a texture whose rows count from the bottom, so the whole darkness overlay was mirrored top to bottom: the beam went down when you aimed up, and the lit area didn't match what you could actually see, so enemies popped in and out of the dark. Fixed by writing rows bottom up.
- **Idle NPCs** (`TurnLoop.CheckVisionAndWander`): each unit picked its own random spot within 5 tiles and walked 2 tiles a minute (twice your pace). Now a squad moves as one: its leader walks to a spot 8-20 tiles off at half your pace, rests 20-90 minutes, then moves on; the rest keep a loose ring 2 tiles round the leader. Squads leaving the zone walk a tile a minute and get 4 hours to reach the exit (was 2).
- Map looks (watchtower is only a raised plank platform, the Perimeter road runs off both map edges with no exit): proposed to Yan, not changed.

## Batch 68 - animated bonfire (2026-10-10)

Yan: "make an animated sprite for the bonfires like how pyre anomalies have it, not exactly the same but using the same fire kidna thingy".

- **Art** (`ArtSource/make_bonfire.py` -> `Resources/Sprites/bonfire_strip.png`, 6 frames of 16x24, 110 ms): the Pyre's flame (same 5-colour palette, heat from the axis and height, re-rolled each frame) as a tamer camp fire: stone ring, three crossed logs, flickering ember bed, a tongue that sways side to side, three sparks drifting up, warm light on the ground.
- **In game** (`TacticalGridView`): every standing cell with the "campfire" look swaps its static sprite for the strip each frame, offset per tile like anomalies. The static p_campfire stays as the fallback when the strip is missing and in the map renders.

## Batch 69 - player tools for early fights (2026-10-10)

Yan: "What do you suggest about the balancing ? it seems that fighting early just kills you all the time and that's kinda boring". Measured with the real combat code: a fresh rookie against a Tier 1 Raider gang of 4 died in about 2 rounds even in hard cover with a VKS-74U (each Raider fires 2 shots a phase: 2 shots against 8 every round). Yan chose "Player tools" on a card (no enemy nerfs, no hero stats).

- **Surprise round** (`Player.Attack`, `TurnLoop.PlayerEndTurn`, `EncounterState.Surprised`): open fire on enemies when none of them has spotted you and they lose their first enemy phase ("SURPRISE!"), on top of the one-tier-higher ambush hit from Batch 64.
- **Takedown** (`Player.TryTakedown`): sneaking (N), attack an enemy human next to you who hasn't spotted you: a silent kill for 2 AP, whatever you're holding. Masters and Legends can't be grabbed. Squadmates who see the body are shaken as usual.
- **Rest heals** (`WorldDays.Rest`): sleeping in a Safe Zone or by a campfire (Z) restores full HP and stops bleeding. Before, only medkits healed.
- **Hire a Wanderer** (`Companion.cs`, `ShopUI` hire row at Wanderers traders like Old Man Yefim, 600 RU): a partner (Rookie, Experienced once you are) who follows you, holds fire while you sneak, and in a fight shoots whoever is fighting you; enemies shoot it when it's the closer target they can see. Crosses zones with you, saves with you, drawn green. Dies for good; loot the body.
- Grenades, smoke and flashbangs were already baseline stock at every trader (250/150/200 RU).

## Batch 70 - one map per tier, and the dungeons (2026-10-10, cloud session, branch `tier-maps`)

Yan: "you can start doing the tier 1 maps now based on the context i gave you, after that work on doing all the other maps, base yourself in the instructions for the tier 1, no wasted space, a few dungeons, not too small but not too empty". The plan he'd approved: one ~200x200 map per tier, ~25 spots each (3 big places, 7-8 medium ones squads hold, ~15 small finds that each have a reason), dungeons for the hours, and a rookie village round Yefim. The approved Tier 1 overview image was lost with the old cloud session, so Tier 1 was redrawn from that plan. **Nothing here has been checked in Unity yet**: it's on the `tier-maps` branch; renders are in `maps/renders/`.

**The maps** (`GameCore/TierMaps.cs`, authored at full size, no upscaler; `BuildingCarver` and `MapDresser` still run):
- **Tier 1** (map owner `perimeter`): The Perimeter (south: Bonfire Village, the Checkpoint Gate, the farmstead, the cemetery and chapel, the army field camp with Watchtower 3, the roadside station), Toxic Marshes (east: the causeway north to The Scrapyard, the fishing village, the pumping station, the flooded bunker, the Sunken Tanker, Reed Blind) and Smuggler's Run (north-west: the rail depot, The Cutting north to the Industrial Basin, Overwatch Ridge, Blind Curve), with the logging camp and radio mast hill between. 
- **Tier 2** (`scrapyard`): Black Valley across the north (a ravine with a gulley only the Chokepoint Bridge, a ford and a broken rail bridge cross; the old mine works; Sniper's Lip on the rim; the road north to the Hub), the Industrial Basin (south-west: Central Plant, Silo Row, the Loading Dock over Secret Lab Alpha, the workers' flats) and The Scrapyard (south-east: The Maze, the Scrap King's Nest, Crusher Row, the truck stop).
- **Tier 3** (`scavengers_hub`): the Hub (a walled market town in the south middle, Safe Zone), Dried Lake (west: the lakebed, Tower Line, the Sub-Station House, the holiday camp, the stranded ferry, the water works) and the Rail-Yards (east: container stacks, the Roundhouse, the signal tower, the loco depot).
- **Tier 4** (`vanguard_barracks`): Ghost Town (south-west, the city pass makes the streets), Vanguard Barracks (south-east, the Liberators' compound, Safe Zone) and Crimson Woods (north and east: the Obelisk Shrine, the Sentinel firebase, Sinkhole Grove, the old summer camp).
- **Tier 5** (`quarry_basin`): Quarry Basin (the south: a pit terraced four levels down to a flooded floor, ramps between; the Contractors' mining camp), the Ship Graveyard (north-west: The Hulks, the cannery, Captain's Row, the dry dock) and the Antenna Grid (north-east: the mast array, the Relay Station, the transformer field, the fuel depot).
- **Tier 6** (`abandoned_city`): the Abandoned City (south and middle: Highrise Row, the Central Plaza and subway, the Garrison Barricade, the department store, cinema, police station, parking garage, tram depot), the Siege Ward (north: the walled ward, the trenches, the Bell Tower, the hospital) and the Anomalous Epicenter (west: the crater, The Wound).
- Tier 7 (the Reactor Core) is unchanged. Exits line up between tiers (Tier 1's cutting and causeway at x 46 and x 166 come out at the same x on Tier 2, and so on). Between the places: ruins, hedgerows, old fields, pole lines, so the walk between spots has cover and texture; the dresser grows each region's own biome.
- Every old landmark keeps its name and is pinned where its region is; 175 new landmarks have their own text (`LandmarkDatabase`; The Bonfire Camp moved into the village). Small finds with a note pin a real stash (`TerrainCanvas.Stash` -> a guaranteed Footlocker at that spot).

**The dungeons** (`GameCore/TierDungeons.cs`, ~40x40, underground, entered by a hatch, door or culvert; climbing in or out is not a day on the road): Tier 1 has five - the Rust Culvert cave (the old Secret Cave Area, redrawn), the chapel crypt, the pumping station basement, the flooded bunker (locked: the key is in the Headman's Strongbox in the fishing village) and the smugglers' tunnel under the depot. Tiers 2-6 get one new dungeon each beside their existing hidden nodes: the crusher pit and the valley mine (T2), the stranded ferry (T3), the camp shelter (T4), the dry dock tunnels (T5), the hospital morgue (T6). Each has 3-4 rooms worth a name, a stash, and mutants (Raiders too in the smugglers' tunnel), populated the first time you go down.

**How a tier map works in the game** (the old zones are regions):
- `ZoneDef.MapId` names the zone that owns the map a region is drawn on (`MacroMapDatabase.MapOf`, `RegionsOf`, `SameMap`). `MapLayout.Regions` says which region each tile is. `TerrainMap.ZoneId` is the map; `MacroState.CurrentZoneId` is the region you're standing in.
- Walking over a region border (`ZoneTravel.UpdateRegion`, every step) makes that region the current zone: the HUD name, its quests, story beats, rumors and Safe Zone rules. No crossing, no time. Travelling to a region of your own map is refused ("walk there").
- A map's exits are every region's ways to another map or a dungeon (`ZoneExitPlacer`); landmarks, loot, anomalies, war spoils are per map (keyed by `MapOf`).
- Squads: everything on the loaded map is live (`SquadManager.OnMap`). A squad belongs to the region its leader stands in; moving to the next region it walks there (`LeavingExit` -> `RegionCenter`), off the map it walks to the exit as before. Nobody wanders into a Safe Zone that isn't theirs (`MayWalkInto`: Wanderers may visit Bonfire Village, Raiders don't). A full region (6 squads) takes no one more. A radio call reaches 60 tiles, not the whole map. Quotas count each region's area.
- Quests: every job on the map shows on the board; kills anywhere on the quest's map count; "clear the zone" means the quest's own region; clues land in the quest region.
- **Bonfire Village is the Tier 1 Safe Zone now, not the whole Perimeter.** Yefim trades and gives his arc there (`bonfire_village`); his jobs' targets are still out in the Perimeter. New Game starts at the village fire. Old saves: Yefim's arc carries over.
- No roofs underground (`RoofView` skips interior maps: a crypt was one big roof).
- Pathfinding (`Pathfinding.FindPath`) is a binary heap now (it scanned a list per node - seconds for an unreachable target on a big map); an NPC's search stops after 6000 nodes and walks toward the closest point it found. All 27 maps build in 0.4 s; a long NPC path is ~2 ms, a full player path across a map ~8 ms.

### Tests
- New `TierMapChecks`: six 200x200 maps, each region has ground, every way off a map is pinned in a region that leads there and the far side leads back, arrival spots and every landmark pinned, every dungeon small/underground with its way up pinned, the bunker key in a stash on its map; New Game in the village, walking into the marsh changes zone with no time passing, travelling to your own region is refused, the bunker is locked until you have the key, climbing out puts you at its door, Raiders stay out of the village.
- The existing per-zone checks (every exit and landmark reachable, no one-way traps) run over every tier map and dungeon; they caught five ladders a tile short of their platforms (Overwatch Ridge, the Signal Tower, The Hulks, the dry dock, the Bell Tower).
- Squad tests adapted: "next door" is a neighbouring map now; a few checks that were really dice rolls (a rifle-less dummy for the arm shot, a refill or caravan that retreats on the way) were made robust.
- ALL CHECKS PASSED.

### For Yan to check in Unity
- Start a New Game: you should be by the fire in Bonfire Village; B trades with Yefim, Z rests.
- Walk out of the village into the Perimeter and up to the marsh: the HUD name should change at each border, the view shouldn't rebuild.
- The Rust Culvert (walk near it), the chapel crypt hatch, the depot's loading office hatch, the pump room: in and out.
- The flooded bunker door should refuse you until you've opened the Headman's Strongbox in the fishing village.
- Frame rate with squads walking about a 200x200 map; the ground texture is 3200x3200 px per map (the city was already 204x204).

## Batch 71 - lock loot follows the map's tier (2026-10-10)

Yan found a Steppe Machete (a Tier 3 melee weapon) in the first Electronic Lock he opened in Tier 1. Yan: "the distribution should be based on map tier".
- `InteractionEngine.GrantHighTierLoot` rolled at least Tier 3 everywhere (an old reading of file 05). It now rolls the tier of the map you're on (a region's tier; dungeons have their own). The table's usual drift (25% one tier down) and lucky finds (3% one up) still apply, and cash and junk are still rerolled. War Spoils use the same roll.
- Not changed yet, offered to Yan: what a lock can roll (melee and consumables are still 21% of results) and where locks spawn (still random next to crates, desks and wrecks).

## Batch 72 - action bar, Hunker Down, Overwatch, the shakedown, cover that wraps (2026-10-10)

Yan: "we need to update the UI and gameplay systems, buttons for stuff in the bottom of the screen where the player can choose which action to take similar to xcom, with icons. Also i tried playing but died in every encoutner, either i got terrible luck or the game is unbalanced as hell". Asked what to give the player (no enemy nerfs, his rule), he chose "Hunker + Overwatch" and "Cover that wraps"; for Raiders, "Shake you down first". He did not pick fairer starting guns or more starting healing.

**The action bar** (`GameCore/CombatActions.cs` = what each action is; `GameCore/HudView.ActionBar.cs` = the buttons):
- Bottom centre, between the player card and the weapon card. One 46 px button per action, a 16x16 pixel icon at 2x, the hotkey in the corner, the AP cost under it. Greyed when it can't be used, a yellow edge while a stance or toggle is on (aiming, burst, sneaking, hunkered, overwatch, grenade ready). Hover: name, key, AP, what it does, and in red why it's greyed.
- In a fight: Fire (shoots the nearest seen enemy in range, the ones already fighting you first), Aim, Aimed shot, Fire mode, Reload, Take cover, Hunker down, Overwatch, Evasive, Grenade, Intimidate, Suppress and Run and gun (only with those perks), Sneak, Ammo type. Exploring: Aimed shot, Fire mode, Reload, Sneak, Ammo, Use, Search, Rest. The hotkeys all still work.
- Icons: `ArtSource/make_action_icons.py` (Python/PIL, the locked style: flat 2-3 tones, #141617 outline) -> `Assets/Resources/Sprites/icon_act_<name>.png` (18 icons; `--install` copies them). Preview: `ArtSource/_out/icons/preview_action_icons.png`.

**Hunker Down (K)** - `Player.HunkerDown`, `CoverUtil.Hunker`: spends the rest of your AP (at least 1, next to cover, in a fight) and ends your turn. Until your next turn the best cover on or next to you counts from every side (nobody flanks you) and its AC doubles: light +2, hard +4. The HUD shows HUNKERED.

**Overwatch (O)** - `Player.Overwatch`, `Perks.OnStep`: needs a loaded gun and 2 AP; spends the rest and ends your turn. The first hostile that takes a step where you can see it, in range, eats one shot. It fires from `TacticalGridSystem.AttemptMove`, so it really is on the step (the old Sniper perk fired when an enemy started its turn; the perk now uses the same hook and its text says "moves").

**The Raider shakedown** - `GameCore/Shakedown.cs`: a Raider gang on a Tier 1 map that notices you (not in a fight already) calls out instead of opening fire: a road toll of 50 Ru a head, or the gun in your hands. Pay or hand it over: they let you walk and leave you alone for a day (`OfflineSquad.TruceUntil`, saved; `EnemyAI.FriendlyToPlayer`). Refuse: they shoot first (your turn ends). Shoot first: the Batch 69 surprise round, they lose their first turn. A gang asks once (`ShakedownDone`, saved). Your gun goes to their boss: kill them later and it's yours again. The gang still walked into you on its own; nothing spawns.

**Cover that wraps** - `GameCore/CoverWrap.cs`, after the dresser on the six tier maps: every lone sandbag, jersey barrier, crate, log pile, tyre stack, stones or debris grows into an L (a run of 3 with a 2-tile arm), and open wild ground gets an occasional L of its own (sandbags or logs/stones on soft ground, jersey barriers, concrete blocks or debris on hard; 60% hard cover). Cover stays walkable; nothing goes next to walls, doors, water, roads, kept spots or into a Safe Zone. Tier 1: 13 grown, 22 new positions; Tier 2-6: 19-90 positions each.

**What the probe says** (a fresh Scavenger, 300 fights each, Raiders already shooting, behind an L of hard cover; `shoot+hunker` = one shot, then hunker while 2+ are up):

| Gang | Plain shooting | + Hunker | + shot first | + shot first, assault rifle |
|-|-|-|-|-|
| 2 | win 2%, die 81% | win 3%, die 71% | win 5%, die 69% | win 13%, die 44% |
| 3 | win 0%, die 98% | 0%, 92% | 0%, 88% | 0%, 84% |
| 4 | 0%, 99% | 0%, 98% | 0%, 96% | 0%, 92% |

Hunkering roughly doubles to triples how long you last (gang of 3: 4 rounds -> 10), but you still can't win: the starting pistol does about 10 a hit, a Rookie Raider has 100 HP and also takes cover (+2 AC), so killing a gang of 3 takes some 75 shots. The shakedown (pay, or walk away) is the real way out of a bad Tier 1 fight for now. Asked Yan what he wants to do about the damage gap.

### Tests
- New `Batch72Checks`: hunkered cover is +4 from every side (+2 and flanked without), hunkering ends your turn and wears off; overwatch fires once on a hostile step; the shakedown (pay = Ru gone and a day's truce; gun = their boss carries it; refuse = a fight; shoot first = surprise round on your turn), only once per gang, only Raiders; every action entry answers in both modes, the fight bar shows Hunker and not Rest, Fire shoots the enemy in sight; every tier map got wrapped cover and at most 1 in 4 sandbag/jersey tiles stay alone.
- `runtest.sh ... only72` runs just these. ALL CHECKS PASSED.

### For Yan to check in Unity
- The bar: does it fit between the two cards at your resolution, are the icons readable at 2x, does hovering show the tooltip, does a click on a button not also walk you there?
- K and O in a fight; the HUNKERED / OVERWATCH chips.
- Walk up to a Raider gang in the Perimeter: the ROAD TOLL dialog and each of its four answers.
- The new sandbag and log Ls in the Perimeter wild.

## Batch 73 - the main menu on every Play (2026-10-10)

Yan: "the main menu is broken" - Play showed a blank blue screen with "New Text" (the scene's leftover placeholder texts) and the log, no menu.
- Cause: `ProjectSettings/EditorSettings.asset` has Enter Play Mode Options on with domain reload off, so static game state survives from one Play to the next. `GameBootstrap` only opened the menu `if (Player.Current == null)`; after an earlier Play the old player was still there, so no menu, and the old run's state had nothing to draw.
- Fix: `GameBootstrap.ResetGameStatics` (`RuntimeInitializeOnLoadMethod(SubsystemRegistration)`) clears the player, game/macro state, the encounter and the pending shakedown at the start of every Play, and the menu now always opens at boot.
- ALL CHECKS PASSED.

## Batch 73b - dice in the log, real lookout towers (2026-10-10)

Yan: "Is the burst mode broken? Also i think attack rolls overall are broken, why is it 6-3 = 3, are there not 2 d10's being rolled?" and "snipers nest is just a pile of wood".
- Attack rolls were fine: 2d10 are rolled (`DiceRoller.Roll2D10`), the log just printed the sum. The "-3" was the burst penalty (VKS-74U: `CombatRules.BurstPenalty` -3 for non-VK-74 rifles), and a burst is one roll for 3 rounds at x2 damage, as designed. The log now shows both dice and what the modifier is made of: `Rolled 2d10 9+5=14 (-1 skill, -2 range) = 11 vs AC 14` (`DiceRoller.FormatRollLog`, the breakdown built in `Weapon.Fire`).
- Towers (`TierMaps.Tower`, used by the Sniper Nest and every lookout on the tier maps): a 4x3 plank deck with a sandbag parapet along the front and sides (hard cover), two posts underneath and the ladder at the back, instead of a bare 2x2 deck. Render: `maps/renders/sniper_nest_b73.png`.
- ALL CHECKS PASSED.

## Batch 73c - a real watchtower, Tier 1 gangs of 3, the gang walks over before asking (2026-10-10)

Yan sent a photo of a timber watchtower: "make the sniper nest into a watchtower like this. Also change raider gangs in the starting area to be a max of 3. Also the enemies aggro from outside of the screen, and they stayed basically outside of my view while asking me to pay up, need to change something about that".
- **Watchtower** (`ArtSource/world/props.py` `watchtower`, packed into the world atlas as `p_watchtower_0`): four tall legs with X-bracing, a zig-zag stair up the front, a railed deck and a plank cabin with a window under a hipped roof, ~3 tiles wide and 6 tall, fades when you stand behind it. `TierMaps.Tower` (every lookout: the Sniper Nest, Watchtower 3, the Water Tower and the towers on Tiers 2-6) places it over a 3x2 footprint: hard cover between the legs, the stair (front middle) half way up, the deck under the cabin (back middle) at full height for the high-ground bonus. Render: `maps/renders/sniper_nest_b73.png`. The atlas only gained the one sprite (every other sprite's size and pivot unchanged).
- **Tier 1 Raider gangs are at most 3** (`SquadManager.MaxStarterGang`): capped when a gang is made for a Tier 1 region, and a bigger gang from further in won't roam into Tier 1 (`FitsZone`). Squads already in an old save keep their size.
- **The gang walks over first** (`Shakedown.Tick`): when a Tier 1 gang spots you it shouts ("Hey! Stay right there!") and walks over, two tiles a minute; the toll is asked once their boss is within 4 tiles and in your sight. Shooting first while they walk up is a fight with their first turn lost; get 22+ tiles away or wait 25 minutes and they give up (and fight if they see you again).
- **The view eases out in a fight** (`TacticalGridView.FollowCamera`): enemies see 12 tiles but the close view shows ~7 up and down, so while anything hostile you can see is fighting you (or walking over for the toll) the camera zooms out until they all fit, up to the far zoom, then eases back to your own zoom.
- Tests: the gang walks over and asks face to face; Tier 1 gangs never exceed 3. ALL CHECKS PASSED.

## Batch 74 - seeing is mutual; the lethality / morale simulation (2026-10-13)

Yan: "i noticed that there's no way to not nerf enemies if i designed the encounters with them so overpowered to begin with ... technically a fresh player would only win against a single also fresh npc 50% of the time" - then: "ok simulate those, but when i played the game i literally died to everything because of some broken game systems, like raiders spotting me from outside of the screen".

**Fix - seeing is mutual** (`TurnLoop.CheckVisionAndWander`): an enemy only spots you from somewhere you can see it too (`Vision.Shows`: in your line of sight and your light), or from within 3 tiles. With Batch 73c's camera ease-out, every fight now starts with the spotter on screen. Nobody opens a fight from the dark or off screen.

**Knob for the probe**: `CombatRules.Lethality` (1 = unchanged) multiplies every gun's damage for everyone, applied in `Weapon.Fire`. Nothing changes in the game at 1.

**The simulation** (`Tools/probes/LethalityProbe.cs`, results in `Tools/probes/lethality_results.txt`; 200 fights per row; fresh Scavenger with the pistol behind an L of hard cover, Tier 1 Raiders each behind a sandbag; "morale" = when one dies, each of the rest has a 50% chance to break, and all break if the boss goes down). Win %, 5 tiles apart:

| Rules | open vs 1 | open vs 2 | open vs 3 | ambush vs 1 | ambush vs 2 | ambush vs 3 | ambush vs 4 |
|-|-|-|-|-|-|-|-|
| now | 56 | 4 | 0 | 60 | 2 | 0 | 0 |
| lethal x2 + morale | 59 | 8 | 1 | 71 | 15 | 3 | 0 |
| lethal x3 + morale | 67 | 7 | 2 | 78 | 25 | 6 | 5 |
| now, gun reaches | 72 | 7 | 0 | 83 | 13 | 0 | 0 |
| lethal x2 + morale, gun reaches | 78 | 14 | 3 | 85 | 31 | 15 | 9 |
| lethal x3 + morale, gun reaches | 75 | 17 | 2 | 86 | 40 | 34 | 25 |

What it showed:
- **The player can barely hit.** Measured hit rate ~20-25% at 5 tiles. The starting pistol's range is 8, so past 4 tiles it takes the long-range -2 (`Weapon.Fire`, Batch 65); the Raider's sandbag is +2 AC; the Scavenger's Agility is -1. On 2d10 that's needing 15+: about 21%. The Raiders, with longer guns, hit the player in hard cover ~30%.
- Lethality and morale alone barely move it while the player can't hit. Once the gun reaches ("gun reaches" = no long-range penalty at 5 tiles), lethality x3 + morale makes an ambush the thing that decides the fight (ambush vs 3: 34% win, open vs 3: 2%) - the S.T.A.L.K.E.R. shape.
- Asked Yan which levers to build.

## Batch 74b - a gun's range is where it shoots straight; it can reach twice as far at -2 (2026-10-13)

Yan (after learning about Batch 65's half-range penalty): "remove the long range penalty for when inside the guns range, add it for when outside, so a pistol with 8 tiles range has normal rolls up until 8 tiles, and it CAN shoot up to 16 tiles but with a -2 on rolls, makes sense? If yes then apply to all weapons".
- `Weapon.MaxReach` = twice `RangeTiles` (melee and 1-tile weapons stay 1). `Weapon.Fire`: no penalty out to the range, -2 past it ("Beyond <gun>'s range (N tiles): -2 to hit"). Every "can I shoot that" check uses `MaxReach` now: your shots and tile shots, the off-hand gun, Overwatch, Suppressive Fire, the Fire button's target, auto-targeting, the parting shot at a fleeing NPC, Raiders' shots and barrel shots, and the enemy AI's "in range" (it still prefers tiles inside its range, where it shoots straight). NPC-vs-NPC fights and your companion still close to their gun's range before firing.
- Same for everyone - Raiders' rifles reach twice as far too.
- Probe again (`Tools/probes/lethality_results_b74b.txt`, 200 fights a row). Win % with today's damage: 1 vs 1 at 8 tiles 50% -> 77%, at 5 tiles 56% -> 68%; 1 vs 2 and more still ~0-6%. With lethality x3 + morale: ambush at 5 tiles vs 2/3/4 = 35/23/21%, in the open 13/1/0%.
- Test rewritten: no penalty at the gun's range, -2 one tile past it, refused past twice it. ALL CHECKS PASSED.

## Batch 75 - a pistol build through Tiers 1-5 (probe only, nothing changed in play) (2026-10-13)

Yan: "is this problem more present in the early game or does it stay all the way to the end? Create a gunslinger pistol build for tier 1 to 5 and run simulations against npc's squads just to see. i was thinking about maybe lowering armor effectivness for the tier 1 to 3 and keeping it the same in tier 4 and 5. also i thought about maybe giving the player more AP (8 instead of 4)".
- Knobs (default off, the game plays as before): `CombatRules.ArmorScaleByTier` (how much Phys-DR each armor tier keeps, applied in `Weapon.Fire`) and `CombatRules.PlayerApBonus` (added to `Player.MaxAp`).
- `Tools/probes/BuildProbe.cs`, results `Tools/probes/build_results_b75.txt` (100 fights a row). The build: T1 Rookie, Halden Service Revolver; T2 Experienced, Walden P9; T3 Professional, two Black Harriers (Gunslinger); T4 Veteran, two Steppe Eagles; T5 Expert, two RK-12 Kuvaldas (Gunslinger + Quick Hands); a mid armor of the tier, 3 medkits of the tier. Foes are real squads from `SquadManager.CreateOfflineSquad` for a zone of the tier, 6 tiles off, each behind a sandbag; you behind an L of hard cover.
- Found: an NPC's rank doesn't depend on the tier (`FactionDeck.Draw` uses the faction's whole deck; the tier only picks their gear), so Tier 2 Garrison squads include Masters and Legends - the hardest fights of the run.

Win % (as now -> armor T1-3 at half -> player 8 AP -> both):

| Tier, foe | open vs 2 | open vs 3 | ambush vs 2 | ambush vs 3 |
|-|-|-|-|-|
| T1 Raiders | 17 / 23 / 49 / 48 | 6 / 7 / 14 / 13 | 52 / 58 / 76 / 91 | 36 / 37 / 64 / 71 |
| T2 Raiders | 33 / 38 / 64 / 67 | 9 / 8 / 22 / 16 | 62 / 73 / 87 / 90 | 39 / 49 / 80 / 79 |
| T2 Garrison | 3 / 3 / 11 / 13 | 0 / 0 / 0 / 0 | 6 / 7 / 29 / 48 | 0 / 0 / 8 / 23 |
| T3 Contractors | 8 / 9 / 30 / 40 | 1 / 0 / 8 / 6 | 31 / 41 / 56 / 67 | 7 / 13 / 32 / 45 |
| T4 Contractors | 15 / 23 / 43 / 55 | 1 / 1 / 7 / 15 | 43 / 46 / 77 / 79 | 19 / 30 / 48 / 60 |
| T5 Contractors | 93 / 94 / 99 / 100 | 75 / 71 / 90 / 94 | 95 / 93 / 98 / 99 | 92 / 94 / 98 / 99 |
| T5 Obelisk | 89 / 88 / 96 / 97 | 52 / 54 / 80 / 88 | 96 / 98 / 95 / 99 | 87 / 94 / 97 / 99 |

Reading: the problem is worst in Tiers 1-3 (and Tier 2 Garrison), eases in Tier 4 and is gone by Tier 5, where this kit (70% DR exoskeleton, two 40-damage revolvers) outclasses the squads. Halving armor in T1-3 helps only a little. 8 AP is the big lever early (T1 ambush vs 3: 36% -> 64%) but makes Tier 5 trivial. The probe player only shoots, heals and hunkers (no flanking, grenades, retreat); everyone starts in cover.

## Batch 75b - strongest, middle and weakest builds, today vs 6 AP (probe only) (2026-10-13)

Yan: "try it with 6 ap. Simulate the strongest vs mid tier vs low tier builds (identify them first)".
- "6 AP" = `CombatRules.PlayerApBonus = 2` (Rookie/Experienced 6, Professional+ 7). Still a knob only; play unchanged.
- `Tools/probes/BuildsProbe.cs`. A build = a weapon kind, its best gun at the tier (a rifle until the kind exists: snipers before Tier 2, machine guns before Tier 3), its two kind perks (one from Tier 3, both from Tier 5), the tier's mid armor and 3 medkits. Melee walks in; everyone else fights from an L of hard cover against squads behind sandbags 6 tiles off.
- Ranking (`builds_screen_b75.txt`; average win % over Tiers 1/3/5, 1-3 foes, open and ambush, 40 fights a case), today -> 6 AP: Sniper 73 -> 81, Rifle single 61 -> 74, Shotgun 58 -> 71, Rifle bursts 56 -> 71, Machine gun 55 -> 68, SMG bursts 59 -> 68, Double Tap pistol 60 -> 65, Gunslinger pistols 53 -> 65, Melee 51 -> 58. Picked: Sniper (strongest), Machine gun (middle), Melee (weakest).
- Through Tiers 1-5 (`builds_full_b75.txt`, 100 fights a case), win % today -> 6 AP, selected rows:

| | Sniper | Machine gun | Melee |
|-|-|-|-|
| T1 Raiders, ambush vs 3 | 43 -> 55 | 4 -> 32 | 0 -> 5 |
| T1 Raiders, open vs 2 | 27 -> 42 | 9 -> 22 | 9 -> 20 |
| T2 Garrison, ambush vs 2 | 30 -> 52 | 14 -> 20 | 13 -> 26 |
| T3 Contractors, ambush vs 3 | 75 -> 90 | 41 -> 66 | 38 -> 49 |
| T3 Contractors, open vs 3 | 14 -> 32 | 8 -> 20 | 3 -> 2 |
| T4 Contractors, open vs 3 | 23 -> 49 | 11 -> 16 | 4 -> 7 |
| T5 Obelisk, open vs 3 | 96 -> 95 | 50 -> 77 | 56 -> 62 |

## Batch 76 - snipers 3 AP +1 scoped, squads live in their leader's tier, Bull Rush (2026-10-13)

Yan: "nerf snipers to consume 3 ap per shot, but add a +1 to hit in the weapon class because they're 'scoped'. make it so enemy ranks move to tiered areas based on the leaders tier, and the leader is always the higher tier. So if a garrison squad spawns with a master tier leader they immediatly move to a tier 5 area, and vice-versa. Melee gets a perk that doubles movement (or movement points) when equipped with a melee weapon (maybe abusable by swapping weapons in the middle of combat, test needed)".
- **Snipers** (`CombatRules.SingleShotAp`, `TraitToHit`): a single shot with a sniper rifle costs 3 AP (everyone: you, NPCs, the Fire button, Overwatch, shooting tiles), and sniper rifles get +1 to hit. The Overwatch perk now needs 3+ AP held.
- **Squads live in their leader's tier** (`SquadManager.RankTier/HomeTier/Settle`): a squad's tier is its best rank's - Rookie and Experienced 1, Professional 2, Veteran 3, Expert 4, Master 5, Legend 6 (the best rank is already the leader, `EnemyAI.Leader`). When the world is seeded or a squad is sent to refill a zone, a squad led from another tier goes to a region of its leader's tier that its faction may hold (the nearest tier with one if none - e.g. Garrison has no Tier 5 ground, so a Master-led Garrison squad goes to Tier 6), re-kitted for that tier. Off your map it is there at once; onto or off your map it walks. A zone whose new squad left tries twice more for one of its own. The Reactor Core keeps its garrison. Quest squads, caravans, migrations and backup aren't moved.
- **Bull Rush** (new Melee perk): a melee weapon in hand doubles your MP. The abuse Yan predicted is closed: swapping to a gun mid-turn takes the extra MP back off what you have left (walk 3 of 8 with the knife, swap to the pistol: 1 left, not 5); swapping back to the knife mid-turn gives nothing until your next turn (`Player.ApplyCarryEffects`, `RushBonus`). Perk count 35.
- Tests: sniper shot costs 3 and gets +1; Bull Rush doubles, the swap loses the extra, swapping back gives nothing, next turn doubles again; a Master-led Garrison squad leaves Tier 2 for Tier 5+, a Rookie-led one leaves Tier 6 downward. ALL CHECKS PASSED.
- The build probe now uses Bull Rush for melee and only fights squads whose leader belongs in the tier (results to follow).
- Probe after Batch 76 (`Tools/probes/builds_full_b76.txt`, 100 fights a case, today -> 6 AP): Tier 1 got easier (only Rookie/Experienced-led gangs stay: Sniper-kit ambush vs 3 58 -> 87, Machine gun 17 -> 37, Melee 1 -> 7). Tier 2 is now the hardest tier for every build (its squads are all Professional-led: Raiders ambush vs 3 Sniper 33 -> 82, Machine gun 14 -> 47, Melee 0 -> 4). Bull Rush lifts melee from Tier 3 on (Contractors ambush vs 3: 21 -> 65 with 6 AP). Snipers at 3 AP still top every tier from Tier 3 (one shot a turn at 4-5 AP; two at 6-7).

## Batch 77 - AP rules, XCOM targeting by body part, examine panel (2026-10-13)

Yan: "make it so all npc's only have 4 ap total regardless of rank, but boss npc's (from quests or randomly spawned) share the players rules of having more ap and maybe a perk? that way they feel special to fight (have to elaborate a bit more on this but it's a good idea). Also, when an enemy is in range or visible add an 'examine' prompt similar to BG3, where you can see his equipped weapon and armor, resistances and if he is a boss, perks. Also i want the targetting system to be similar to xcom, where you click the enemy bodypart to select the target during combat". (Before that: player AP "start with 5 and get +1 when reaching Veteran rank".)
- **AP** (`CombatRules.PlayerRankAp`, `NpcAp`): the player has 5 AP, 6 from Veteran (artifacts still add). Every NPC has 4 whatever its rank (`FactionDatabase.Ranks` Ap all 4). Bosses - a human with `Named` set - use the player's rule (5, 6 from Veteran). Boss perks and where bosses come from are waiting on Yan's design; nothing makes human bosses yet (Named mutants are unchanged: they keep their own AP).
- **Hit chance, one source of truth** (`Weapon.PreviewHit`, `CombatRules.HitChance`): everything that decides a roll (skill, height, cover/flank, burst, aim, perks, aimed part, range, weapon traits, wounds/pinned) is computed in one place; `Weapon.Fire` uses it and only adds the side effects (aim spent, logs). `HitChance` is the exact 2d10 odds (a double 10 always hits; advantage keeps the better pair). `Player.PreviewShot` gives chance, AP and why not; `Player.AttackPart` fires at a part.
- **Targeting panel** (`HudView.Targeting.cs`): left-clicking an enemy (or the Fire button) opens it above the action bar: a body figure - head, arms, body, legs - each part coloured red to green and labelled with its real hit chance; click a part to aim for it (+1 AP, the Batch 57 effects), SHOOT for a plain centre-mass shot, ARMOR and POUCH buttons, hover for what a part does and the roll's modifiers vs AC, AMBUSH when they haven't seen you. Bursts and blades: SHOOT/BURST/STRIKE only. It stays open while they're up and you can still afford a shot. Esc, right-click or a click on the map closes it. A sneak takedown and right-click still act at once. L still cycles a default aimed part.
- **Examine panel** (`Examine.cs`, Tab or EXAMINE): HP, AC, AP, MP, morale; weapon (damage, range/reach, magazine, bursts, condition); armor (Phys-DR / Anom-DR, condition); grenade carried; wounds and stance; your cover angle on them; for a boss, its perks. Mutants: attack, resistances, traits. The target card says "CLICK TO TARGET / TAB EXAMINE".
- Settling (Batch 76) adjusted: a Master-led squad may hold Tier 5 or 6, a Legend's 6 or 7 (Tier 6 was left empty otherwise); seeding tries once more (not twice) for a squad of the zone's own, which was draining the faction decks.
- Tests: AP by rank and for a boss; 2d10 odds; the previewed chance matches 3000 real shots within 4 points; examine shows weapon, armor and resistances; out-of-reach is refused. ALL CHECKS PASSED.
- Not seen in Unity yet - the panels are built in code like the rest of the HUD; positions are in 1920x1080 reference pixels.

## Batch 77b - all 9 builds against the targets (probe only) (2026-10-13)

Yan agreed the targets: open vs 1 80%+, ambush vs a squad of 3 about 60%, open vs 3 low (15-25%) - "since the simulations are not an actual player using tactics". `BuildsProbe.cs targets`, results `Tools/probes/builds_targets_b77.txt` (100 fights a case, Batch 77 AP: player 5/6, NPCs 4). Summary:
- On target or close: Tier 1 for most builds (ambush vs 3 50-77%), SMG and shotgun through Tier 3, rifles single from Tier 2.
- Tier 2 Garrison: ambush vs 3 1-22% for every build (their armor; the player is still Experienced with Tier 2 gear).
- Too weak: pistols at Tiers 2-4 (ambush vs 3 32-45%, Double Tap a bit better), rifle and MG bursts at Tiers 1-2 (19-26%, the -3 burst penalty on the VKS-74U/Smerch-3), melee at Tiers 1-3 (1-37%; Bull Rush only from Tier 3).
- Too strong: snipers from Tier 3 (ambush vs 3 90-100%, open vs 3 up to 100% at Tier 5), and every build at Tier 5 (open vs 3 53-100%).

## Batch 78 - Garrison duos, bigger late squads, pistol/Gunslinger/melee changes (2026-10-13)

Yan: "make it so garisson at tier 2 only have duos patrolling an area around their base, squads that spawn with a tier 3 or higher move on to maps up north ... this turns them into the 'don't come near me' faction and players can come back to woop their ass after getting stronger (we'll need to add something to their base as a reward for this later). ... Raiders at tier 3 or higher get bigger squads remember, 4 to 6. Make it so pistols get the +1 to hit only inside their effective range, and add the suggested gunslinger change. Melee add your suggestion too. make the later tier squads bigger as in 4 to 6. then run the tests again". He also pointed out the starting perk: Professional's pick is the second perk, not the first.
- `SquadManager.HumanSquadSize`: Garrison in Tier 2 = duos; Raiders 4-6 (Tier 1 capped at 3, Batch 73); every faction 4-6 from Tier 4; otherwise 2-4. Squads settle by their leader's tier as before (Batch 76), so Garrison squads led from Tier 3 up move to Garrison ground up north (only Tier 6 has it). `FitSize`: a squad settling into a bigger-squad tier recruits up to 4 (never above its leader's rank); one going down to Garrison's Tier 2 keeps a duo. Seeding tries up to 4 times per slot for a squad that stays (bigger squads drain decks faster; the marshes started empty at 2).
- Pistols +1 to hit inside their range (`CombatRules.TraitToHit`); Gunslinger -1 per gun (`CombatRules.GunslingerPenalty`, was -2).
- Melee: any blade in hand +2 MP, Bull Rush doubles the total (still swap-proof); a blade kill on someone who hadn't seen you starts no fight if nobody else is onto you ("Nobody heard a thing.").
- Probe fixed: the starting perk from Tier 1, the second at Tier 3, a third (Adrenaline Surge) at Tier 5; squad fights use each tier's real sizes. Results: `Tools/probes/builds_targets_b78.txt`. Tier 1 on target or easy (pistol builds 87-95 ambushing 3); Tier 2 Garrison duos on target for 7 of 9 builds; Tier 2 Raider gangs of ~5 too hard (ambush 23-47); Tier 3 too easy for all (ambush 79-94, open 30-57: squads of 2-4 against 2 perks and 5 AP); Tier 4 on target for most; Tier 5 too easy for the RK-12 Kuvalda pair, the G-7 Rail Prototype and the rifles, on target for SMG and shotgun. The machine-gun build is weak before Tier 3 because its starting perk (Shredder) needs a machine gun and there are none before Tier 3. Melee is still weak against squads at Tiers 1-2 (the probe can't sneak up, so silent kills don't show).
- ALL CHECKS PASSED (twice).

## Batch 79 - RK-12 34, Tier 2 Raiders 3-4, Tier 3 squads 3-5, the PL-62 Kobza (2026-10-13)

Yan: "make tier 2 raiders 3 to 4, make tier 3 squads 3 to 5" (Raiders keep 4-6 from Tier 3); the G-7 Rail Prototype "is gonna be an extremely rare weapon with rare ammo so it is a power fantasy weapon" (left out of the balance targets; its ammo mechanic is still to be designed); RK-12 "drop it to 34 damage"; "add a tier 2 light machine gun"; melee "is best when accompanied with another weapon type so the early weakness is good" (left as is).
- `WeaponDatabase`: RK-12 Kuvalda 40 -> 34 damage. New heavy `PL62Kobza` "PL-62 Kobza": 20 damage, 75-round drum, range 40 cm (16 tiles), Tier 2, 2 MP while held, usual 10-round heavy bursts. No sprite yet: the generic heavy art and the RL-44's hand points (`WeaponArt.Grips`) until it gets its own.
- `SquadManager.HumanSquadSize`: Raiders 3-4 in Tier 2; Tier 3 squads 3-5 (Raiders 4-6).
- Seeding: a squad led from another tier only moves into a region still under its quota while the world is seeded; if none has room it never forms (its cards stay in the deck). With bigger squads the decks ran dry and the Reactor Core started empty (the story gate test caught it). Rosters unchanged (they set Yan's faction strengths).
- The probe's machine-gun player didn't reload for Shredder's double bursts (it stalled with 15 rounds in the drum); fixed. Results: `Tools/probes/builds_targets_b79.txt`.
- ALL CHECKS PASSED.

## Batch 80 - two weapon slots, tiered ammo, sniper scope, RK-12 kick, Gunslinger after Point-Blank, Tier 3 squads 4-6 (2026-10-13)

Yan: "same tier ammo no damage change, since weapons already have different damage numbers"; "hollow point and armor piercing will need the tiered + per weapon class system as well"; "try the rk-12 changes but i don't think they'll feel good for a player if he grinds all the way to tier 5 only to see his builds perks do not work with the top tier weapon ... might be worth looking onto other changes"; "add 2 weapon slots to the system, swapping from any gun to a pistol or melee costs 0 ap but swapping to any other costs 1, including swapping back from a pistol or melee to another class, make it so snipers can only use 'small' weapons as a secondary"; "tier 3 squads 4 to 6, ok"; snipers "+1 only beyond half it's effective range, -2 inside 5 tiles"; "gunslinger only appears for selection after taking the first pistol perk" (Point-Blank Execution). G-7 ammo: a synthesizer in a northern lab that turns artifacts into cells - agreed, not built yet.
- **Two weapon slots** (`Player.Equipment.cs`: `SecondaryWeapon`, `SwapWeapons`, `EquipSecondaryFromInventory`, `StowSecondary`, `PairOk`): "Second" button on a weapon in the pack; Y or the new SWAP button (`icon_act_swap`) swaps. Drawing a pistol or a blade is free, anything else 1 AP (only in a fight). Both keep their loaded rounds. A sniper rifle only pairs with a pistol or a blade: equipping one sends a bigger second weapon back to the pack. Saved. The Gunslinger off-hand stays its own slot. New games start with the knife (or switchblade) in the second slot.
- **Tiered ammo** (`Ammo.cs`): the player's spare rounds are kept per weapon class and tier - pistol, smg, shotgun, rifle, sniper, belt - "T3 rifle rounds (Hollow Point)"; a gun only loads its own class and tier. Kinds (Standard, Hollow Point, Dart, Armor Piercing) work as before; the tier never changes damage. Traders sell boxes for the gun in your hands at their own tier only (`ShopSystem.AmmoKeyHere`; the row warns when your gun takes another tier); prices already rise north (`RemotenessMarkup`). Loot: an NPC's rounds are for its own gun; a found stash has rounds of the map's tier for your gun's class. Old saves: loose piles go to the gun in your hands (`Ammo.Migrate`). The G-7 has its own "rail cells" pool (no source yet - the synthesizer). Faction-reputation gating for HP/AP: not done.
- **Sniper scope** (`CombatRules.TraitToHit`): +1 only beyond half the rifle's range, -2 within 5 tiles. Enemy snipers too.
- **RK-12 Kuvalda +1 AP a shot** (`CombatRules.Kick`), added after the perks so they still work with it: a plain shot 3 AP, a Double Tap follow-up 2, a Gunslinger pair 3.
- **Gunslinger needs Point-Blank Execution** (`PerkDef.Requires`, checked in `TakePerk`; the picker greys it out with "needs Point-Blank Execution"). It can no longer be the starting perk.
- **Tier 3 squads 4-6** for every faction (`HumanSquadSize`, `FitSize`).
- Seeding now goes north first each round, so the Reactor Core gets its Obelisk squads before the Tier 5 zones drain the deck (the story-gate test caught the empty reactor again with the bigger squads). The crowding test no longer counts migrating mutant packs (they're passing through).
- Fixed a stale log line: Gunslinger off-hand said "-2 each" (it's -1 since Batch 78).
- Tests: Gunslinger refused without Point-Blank; sniper +1 far / 0 mid / -2 close; RK-12 3 AP; sniper + rifle refused, + pistol ok; swap to the pistol 0 AP, back to the sniper 1 AP; equipping a sniper sends the rifle back; starting rounds are "pistol1|Standard"; a T1 pistol won't load T2 pistol or T1 rifle rounds; a trader sells its own tier. ALL CHECKS PASSED.
- Probe (`Tools/probes/builds_targets_b80.txt`, 100 fights a case; Gunslinger build = Point-Blank, then Gunslinger at Tier 3, Quick Hands at Tier 5; the sniper carries the tier's best pistol and draws it when someone is within 5):
  - Tier 3 with squads of ~4.8: ambush 47-81 (was 71-95) - on target for most builds; shotgun (73-81) and SMG vs Contractors (72) a bit high.
  - Snipers: Tier 3 ambush 95/87 -> 54/47, Tier 4 96/93 -> 68/72, Tier 5 100/99 -> 97/93 (open vs squad 86/90 -> 63/72). Still the strongest at Tier 5.
  - RK-12 at 3 AP: Double Tap open vs squad 56/49 -> 17/13; Gunslinger 77/84 -> 45/52. Ambush still 85-97 - but every Tier 5 build ambushes at 79-97 except SMG/shotgun, so Tier 5 as a whole is easy, not just the RK-12.
  - Gunslinger at Tiers 1-2 (now a plain pistol): ambush 74 / 44-47 (was 93 / 65-70).

## Batch 81 - bigger faction decks, human bosses, armor-piercing rounds for Tier 4+ enemies (2026-10-13)

Yan: "increase the faction card deck in a way that fills the world but keeps the same % for each tier of units in that faction"; "One suggestion to fix tier 4-5 easyness, bosses more likely to spawn and they have a 50% chance of using ap ammo, tier 4 to 5 normal npc's have a 5 to 15% (based on rank) to use ap ammo as well, limited to 2 enemy units per squad".
- **Decks x3** (`FactionDeck.DeckScale = 3`): every rank's card count is roster x 3 x its weight, so each rank keeps its share (Raiders Rookies still 45%). Outposts still use the plain roster (`Strength` at scale 1), so no faction moved. Before: Garrison, Obelisk and Contractors ran out at the first seeding and the Reactor Core held 1 squad of 6 (41 of 48 squad slots filled). After: 46 of 48 filled, 4 squads in the Reactor Core. Obelisk still uses its whole deck (they're allowed in every zone from Tier 4 north).
- Seeding also counts squads that settled into a zone from elsewhere, so no region starts over its quota (Abandoned City had 6 for a quota of 4).
- **Human bosses** (`SquadManager.Arm`, `HumanEnemy.MakeBoss`): when a squad forms, its leader may be a boss, by the tier the leader belongs to: Tier 1 0%, Tier 2 5%, Tier 3 8%, Tier 4 15%, Tier 5+ 25%. A boss gets a callsign ("Ironjaw" (Raiders boss)), the player's AP rule (5, 6 from Veteran) and BOSS in the examine panel. Boss perks still to be designed. (Before this, nothing made human bosses at all.)
- **Armor-piercing rounds**: a boss 50%; other members of squads whose leader belongs in Tier 4+ 5% (Rookie) to 15% (Legend); at most 2 per squad; blades never. Kept through re-kits and looted guns (`HumanEnemy.ApRounds`, re-loaded in `EquipWeaponFromDb`). Bosses and AP rounds are saved with the squad.
- Tests: deck shares and strength unchanged; 300 Tier 5 squads give ~13% bosses, about half of them with AP rounds, ~9% of other members with AP rounds, never more than 2 a squad; AP rounds survive a re-kit. ALL CHECKS PASSED.
- Probe (`Tools/probes/builds_targets_b81.txt`, vs `b80`): the change is small, mostly inside the noise of 100 fights. Tier 5 ambush on a squad is still 71-97 for most builds (RK-12 Double Tap vs Obelisk 87 -> 71, the rest within a few points); Tier 4 mostly 56-87 as before. Open fights at Tier 5 dropped a little for some (MG vs Obelisk 63 -> 37, RK-12 Gunslinger 45-52 -> 31). Armor piercing only matters for the 1-2 shooters that have it, and bosses are 1 squad in 4-8.

## Batch 82 - no cap on AP rounds per squad; one rival crew per job (2026-10-13)

Yan: "remove the cap of 2 per squad, each unit in a tier 4 to 5 squad has a chance to roll ap ammo". He also asked why Tier 4-5 squads have Rookies in them; his understanding is that they should be made of the ranks that belong to those maps ("a tier 5 map might have a squad of a legend leader and 3 master companions"). Explained, waiting on his call (see below).
- `SquadManager.Arm`: every member of a Tier 4+ squad rolls (bosses 50%, others 5-15% by rank), with no cap per squad.
- `QuestSystem.SendRival` no longer sends a second rival crew for a job that already has one (the harness caught two crews once the dice fell differently).
- Measured at a new game (squads by map tier): Tier 4, 21 people: 3 Rookies, 3 Experienced, 5 Professionals, 5 Veterans, 5 Experts. Tier 5, 54 people: 2 Rookies, 8 Experienced, 11 Professionals, 15 Veterans, 9 Experts, 8 Masters, 1 Legend. E.g. a Tier 5 Wanderers squad: Legend, Expert, Professional, Rookie. Why: only the leader decides where a squad goes (`HomeTier`, `Settle`); members are drawn at random from the whole deck (`FactionDeck.Draw`), with the only limit that they don't outrank the leader (`FitSize`).
- ALL CHECKS PASSED. No probe run (waiting on the squad-makeup decision).

## Batch 83 - north and south decks, leader-first squads, squads keep to their tier band (2026-10-13)

Yan: "leaders need to be the higher rank in a squad, members of his squad can be up to 2 tiers below him, (so a legend could have masters and experts in his squad)"; "north uses it's own deck, when a squad from the (north) deck spawns they immediatly go up north to tier 4 to 6 areas, this way the player might be caught unaware by one of them transversing the zone but they do not daddle around lower tier areas, their objectives in a-life need to be all in high tier areas, similar to this, lower tiered squads need objectives in the lower tiered areas".
- **Two decks** (`FactionDeck`): south = Rookie..Veteran at x3 the roster, north = Professional..Legend at x4 (`NorthScale`); north cards are written "N:Master". Same weights, so each rank keeps its share inside each deck; outposts still from the plain roster.
- **Squads** (`SquadManager.CreateOfflineSquad`, `FitSize`): a squad made for a Tier 4+ zone draws from the north deck. The leader is drawn first (north: Expert or better); every member after is his rank or below and at most 2 tiers under him (`FactionDeck.MemberOk`).
- **Bands** (`SquadManager.InBand`, used by `FitsZone`): a squad led from Tier 4 up only roams to, settles in, answers radio calls in and retreats to Tier 4+ regions; the rest only Tiers 1-3. Routes may still pass through other regions on the way. A northern squad whose outpost is in the south falls back to a northern neighbour instead.
- New game: Tier 5 maps have nobody below Professional (were 2 Rookies and 8 Experienced of 54), Tier 6 Veteran..Legend, the Reactor Core 6 of 6 squads (was 4), 47 of 48 squad slots filled.
- Tests rewritten for the two decks (north deck counts, Expert+ leaders and the 2-tier rule at the Reactor Core, cooling cards by card, northern retreats). ALL CHECKS PASSED.
- Probe (`Tools/probes/builds_targets_b83.txt` vs `b81`, ambush on a squad): Tier 4 got clearly harder where the squads used to be weak - vs Raiders 46-80 -> 9-60 (Double Tap 56 -> 22, rifle single 70 -> 33, MG 82 -> 55); vs Contractors about the same (35-82). Tier 5 barely moved (ambush 49-98; rifle bursts 93-96, sniper 87-98, RK-12 71-94): the probe already only fought Master-led squads there, so their makeup changed less. Open fights vs a squad at Tier 5 dropped a little for most builds (rifle single 35 -> 16 vs Contractors).

## Batch 84 - south squads draw whole, best card leads; why Tier 5 is easy (2026-10-13)

Yan said "yes to both": (1) give the south its old squad strength back while keeping his 2-tier rule, (2) find out what makes Tier 5 easy.
- `SquadManager.CreateOfflineSquad`: in Tiers 1-3 the whole squad is drawn from the south deck and the best card leads (leader-first had made the south mostly Rookies: Tier 2 Raiders 61% Rookies vs 42% before); anyone more than 2 tiers under the leader is put back (`FactionDeck.Undraw`, no cooling card) and redrawn. The north still draws its Expert-or-better leader first. Squads in the probe now: Tier 2 Raiders Rookie 40 / Experienced 29 / Professional 24 / Veteran 4%, like before Batch 83; Tier 2 Garrison duos have no Experts any more (they used to slip in).
- `Tools/probes/TierDiag.cs` -> `tier_diag_b84.txt`: the player (best rifle or pistol of the tier, the tier's mid armor, no perks) against a typical squad, both directions, from the game's own hit chances and damage:
  - Tier 4 -> Tier 5, the player: armor 45% -> 70% Phys-DR (every Tier 5 armor is an exoskeleton, 50-85%), AC 11 -> 12, hit chance 61% -> 74% (Expert's +2).
  - Tier 4 -> Tier 5, the enemies: armor 37% -> 42%, hit chance on you 68% -> 63%, damage a hit on you 14 -> 12, a squad's damage a turn ~100 -> ~75. Their most common gun at Tier 5 is still the Corvan Gnat PDW, a Tier 4 SMG. Your damage a hit stays ~19-24.
  - So Tier 5 is easy because the player's gear jumps a whole step (exoskeletons) while the enemies' kit barely moves: they're kitted by rank (`EncounterGeneration.PickFactionLoadout`, `FactionArmorDatabase.PickArmor`), not by the tier they live in.
- ALL CHECKS PASSED (new check: south squads within 2 tiers of the best card, south deck only).
- Probe after the south fix (`Tools/probes/builds_targets_b84.txt`, ambush on a squad): Tiers 1-3 are back near where they were before Batch 83 and mostly on Yan's ~60% target (Tier 2 44-83, Tier 3 35-80; Batch 83 had 45-90 / 43-88). Tier 1 vs Raiders 62-82 for gun builds; melee and the MG kit (a rifle until Tier 2) still weak early, as before.

## Batch 85 - probe: a Tier 5 elite squad (Legend + 4 Masters) (2026-10-13)

Yan agreed enemies stay kitted by rank ("them being equipped by the rank makes sense") and asked whether a squad of 1 Legend and 4 Masters is still easy. New probe mode `BuildsProbe elite <n> <tier> <foe> <ranks>` (a fixed squad, kitted by rank, bosses and AP rounds rolled as in the game). Results `Tools/probes/elite_t5_b85.txt`, Tier 5 Contractors, 100 fights a case:
- Open fight: 0-18% wins, the player dies 82-100% - not easy at all.
- Ambush: SMG 58, shotgun 58, melee 60, MG 64, rifle single 79, Double Tap 86, rifle bursts 91, sniper 95, Gunslinger 96.
- So an elite squad is hard; Tier 5 reads easy in the targets probe because most Tier 5 squads are Veteran/Expert-heavy (North deck share), and because an ambush (a free first turn, one hit tier higher) lets the burst-heavy builds kill 2-3 before anyone shoots back.
- Yan (2026-10-14): "leave it like that" - Tier 5 stays as it is (enemies kitted by rank; no extra elite share).
- CLAUDE.md brought up to Batch 85.

## Batch 86 - boss perks (2026-10-14)

Yan: "boss perks = 2 perks based on their main weapon". Also: "hollow point and armor piercing should be always available, don't know about the prices, need to balance that"; the G-7 "is either used by an enemy npc boss for a hidden sidequest/lab (not any of the already made ones)"; the Garrison base "should be a 'raid', where enemys come in waves ? need to elaborate that (maybe similar to mara's shop final quest), reward should include plenty of ap and hp ammo, high tier healing items, 2 tier 4 and 1 tier 5 weapon (random), one tier 4 armor. all of this in their 'armory'". Those three are proposals for now (see the reply); only the perks are built.
- `HumanEnemy.BossPerks`: a boss has the 2 perks of its weapon class - sniper Marksman's Focus + High-Velocity Impact, assault Recoil Control + Controlled Taps, SMG Bullet Hose + Hit and Run, heavy Shredder + Walking Tank, shotgun Breacher + Staggering Blast, pistol Point-Blank Execution + Double Tap, melee Bull Rush + Blade Dancer. Set when it becomes a boss and again whenever it picks up another kind of gun; restored on load. The examine panel already lists a boss's perks.
- Made these work for NPCs (they were player-only): Controlled Taps (`Weapon.Fire`), Double Tap, Blade Dancer and Hit and Run (`HumanEnemy.Attack`, per-turn counters reset in `EnemyAI.HumanTurn` via `NewTurn`), Walking Tank and Bull Rush (`HumanEnemy.TurnMp`). The other six already worked for anyone.
- Checked: HP and AP boxes are already in every trader's fixed stock (`ShopDatabase` Baseline items, `ShopUI` "Layer 1"); Mara's final quest (MARA-3 "Defend Main Street during a full raid") is quest text only - no wave/raid mechanic exists in code.
- Tests: a sniper boss gets its two perks; they change with a heavy gun; Walking Tank lifts the heavy MP cap; a pistol boss's second shot costs 1 AP. ALL CHECKS PASSED.

## Batch 87 - ammo prices by tier, the Garrison raid (2026-10-14)

Yan: "yes for the ammo" (the price table); for the Garrison base: "repeatable after a few in-game days, change the tier 4 armor to tier 5 / since the garisson is technically the government they'll send more troops in eventually so they can continue spawning / trigger is either the player starting combat in the area or the eventual faction wars we'll add later". "yes for the g-7 proposal" (built next, Batch 88).
- **Ammo prices** (`ShopSystem.AmmoBoxPrice`): Standard boxes 150 / 210 / 285 / 375 / 480 by the trader's tier (600, 750 beyond), Hollow Point x1.5, Dart x2, Armor Piercing x2.5; then the usual remoteness markup, reputation and Haggler. Rolled specialty boxes use the same price.
- **Garrison raid** (`GarrisonRaid.cs`): a fight in the Siege Ward (the hospital region, Garrison's base) with Garrison aggroed starts it. The guards there are wave 1; every 3 rounds another squad comes out of the base doors farthest from you (`CreateOfflineSquad` - real Garrison cards from their north deck), wave 3 led by a boss. When the last wave is down the Garrison Armory (new landmark in the hospital, (76,14)) opens: 4 boxes each of AP and HP rounds for each gun you carry, 3 scientific medkits, 2 scientific stims, 3 bandages, 2 random Tier 4 weapons, 1 random Tier 5 weapon (never the G-7), 1 random Tier 5 armor. It can be raided again 4 days later (`MacroState.GarrisonRaidReadyDay`, saved). Garrison keeps spawning as before: dead people's cards come back after a day. `GarrisonRaid.Start()` is the hook for the faction wars. Walking out mid-raid ends it (and starts the cooldown). A raid in progress isn't saved (a reload ends it).
- Tests: the armory is barred until the raid is won; a fight there starts the raid; two more waves, the last with a boss; the armory opens, the cooldown is set, the loot is all there; ammo prices. ALL CHECKS PASSED.

## Batch 88 - the Lattice Foundry: the G-7's boss and the cell synthesizer (2026-10-14)

Yan: "yes for the g-7 proposal" (a new hidden Tier 5 lab, found through the clue system; its boss carries the G-7; the synthesizer turns artifacts into cells).
- New hidden zone `lattice_foundry` "The Lattice Foundry" (Tier 5, Perception 18), a hidden way off the Ship Graveyard: the Flooded Slipway hatch at (14,63) on the Tier 5 map, by the Dry Dock. Rumours of hidden ways pick it up like the others. Layout `RoomGrids` "Lattice Foundry": the Foundry Lift room, a test range, the synthesizer hall. Render: `maps/renders/lattice_foundry.jpg`.
- `G7Lab.cs`: the first time you go down, an Obelisk crew (from their north deck) holds it, led by "Foreman" (an Obelisk boss) with the G-7 Rail Prototype (so Marksman's Focus + High-Velocity Impact) and 12 spare rail cells on his body. The crew holds the lab (Job "hold"). Kill him and the G-7 drops like any gun.
- The Cell Synthesizer (E next to it, out of combat): eats one artifact from your pack (the lowest tier first) and gives rail cells - 2 per artifact tier, 15 for a legendary. Rail cells are their own ammo pool ("Rail cells", `Ammo.Pool`), so the G-7 reloads from them.
- Fixed while rendering: the lab wasn't on the interior list (`ZoneLayoutRecipes.IsInterior`), so it was built like an open field.
- Tests: hidden way listed; the Foreman has the G-7, 2 perks and the cells; the crew is only made once; the G-7 drops on his body; no artifact, no cells; a Tier 3 artifact makes 6; the G-7 loads them. ALL CHECKS PASSED.
- Harness fix: "a healthy NPC with a loaded light gun keeps it" read the gun's class after the flight (always empty once a heavy gun is dropped), so a Raider rolling the PL-62 Kobza (Batch 79) failed it at random. It reads the class first now. ALL CHECKS PASSED twice.

## Batch 89 - the Garrison raid, bot-tested (2026-10-14)

Yan: "can you run a bot test on the raid?" New probe mode `BuildsProbe raid <n> [builds]`: each build with its Tier 5 kit (best Tier 5 gun, mid Tier 5 armor, 3 perks, 3 scientific medkits) on the real Tier 6 map, starting in the Trenches; the bot walks at the nearest Garrison soldier in the Siege Ward (A* routes, like a clicked path), shoots, heals, hunkers; the raid itself runs on the game's code (`TurnLoop.PlayerEndTurn` -> `GarrisonRaid.Round`).
The test found three problems in the raid, fixed:
- It only ended when every Garrison soldier on the whole Tier 6 map was dead (squads in other regions never join), so it could almost never be won. It now tracks its own soldiers: the guards within 35 tiles of the hospital when it starts, and the waves (`GarrisonRaid.Left`).
- Raid soldiers broke contact and went back to wandering; while the sirens are on they stay alerted to you.
- (Probe only) the bot's straight-line walking got stuck on walls; it walks A* routes now.
Results (`Tools/probes/raid_b89.txt`, 50 raids a build): about 9 guards at the start and 8-9 more in the two waves (~18 soldiers, Tier 6 Garrison). Armory won: Sniper 16%, rifle bursts 6%, Gunslinger 2%, SMG 2%, the rest 0%. Died: 46-92%. 4-14% of runs had no Garrison at their base at all (the world seeded Obelisk there). The bot fights from wherever it stands, with no flanking, grenades, companions or retreat, so a real player will do better - but as built the raid is far beyond a lone Tier 5 character. Waiting on Yan's call.
- New harness check: Garrison elsewhere on the map doesn't block the win. ALL CHECKS PASSED.
- Yan: "did the bot have artifacts equipped? we never talked about that" / "and armor modifications too". No - no simulation so far had artifacts or armor mods (`Kit` gives gun, armor, perks, ammo, medkits). New mode `BuildsProbe raidgear`: the same raid with Ceramic Plates + Artifact Pouch on the Wanderer Exoskeleton and Lantern Seed (+5 HP/turn), Frost Lattice (+2 MP), Rosary (no bleeding) in its 3 belt slots. Results `Tools/probes/raid_gear_b89.txt` (50 raids a build), without -> with: deaths fall a lot (Sniper 46 -> 18%, rifle single 88 -> 30%, rifle bursts 52 -> 34%, Gunslinger 92 -> 56%), armory wins rise a little (Sniper 16 -> 26%, rifle bursts 6 -> 16%, rifle single 0 -> 8%), and most of the rest now end unfinished after 120 rounds - the bot survives but stays in a standoff (it never flanks or uses grenades). Melee still loses (88% deaths).

## Batch 90 - why raids dragged on: Anchor AI, Rosary, raid fixes (2026-10-14)

Yan: "the enemy doesnt throw any grenades and rush the bot using the different archetypes? also how does it last forever, are the dice rolls that bad, are enemies healing? or they are just not all dead in 120 turns and it stops there?" Read from the raid logs and the code:
- Grenades: Professional+ NPCs carry a frag, thrown only at a player who stayed on the same cover 2+ enemy rounds, within 8 tiles and in sight (`EnemyAI.HumanTurn`); flashbangs within 4, smoke when Shaken. Most raid fighting was at 10-25 tiles, so ~2 grenades in 120 rounds.
- Archetypes (`EnemyAI.Role`): Leader; Anchor = rifle, machine gun, sniper; Rusher = shotgun, SMG; Flanker = the rest. Garrison is mostly Anchors. **Bug fixed**: an Anchor/Leader only scored tiles it could reach this turn for cover and a shot; with no shot from any of them nothing pulled it closer, so it sat in cover out of reach forever. Now a tile with no shot is better the closer it is (`PickTile`).
- Enemies don't heal (no NPC healing code). The dice were fine. The bot heals (medkits, +5 HP/turn Lantern Seed).
- **Bug fixed**: Rosary (and Heart of the Wound) only cleared bleeding once, when put on; new wounds still bled 5 HP a turn. `Player.BleedImmune`, checked each turn.
- Raid fixes: waves came out of the "farthest base door", which could be the armory itself - they appeared inside it and stayed; the armory is no longer a wave door. A raid soldier who ran off the map no longer has to be killed (`GarrisonRaid.Left` counts only those still on the map). Wave 3 sometimes never comes: Garrison's north deck has no leader left to send ("The base has nobody left to send").
- Probe: the bot walks to a free tile next to its target (it was trying to path onto the target's own tile and stopping).
- Results `Tools/probes/raid_gear_b90.txt` (late-game kit, 50 raids a build): raids now resolve (unfinished 0-12%, was 28-58%). Armory won: Sniper 38%, rifle bursts 22%, rifle single 18%, SMG 4%, the rest 0-2%. Died: 48-90%. ~18 Tier 6 soldiers against one bot that doesn't flank, use grenades or retreat.
- Harness: the raid test allows a boss in wave 2 too (any squad can roll one). ALL CHECKS PASSED.

## Batch 91 - floor plans: the Lattice Foundry prototype, two floors (2026-10-14)

Yan: "this is a map problem, not a gun problem, we need to design dungeons and these other 'raid' style and basically all buildings in a way that rewards positioning, if you have a sniper, by all means hold the longest range as possible, if you have a shotgun or something else, play the rooms and corridors smartly. Labs and building insides need to have more deviations and paths, rooms, basically be modeled like real buildings and insipired by the stalker labs" (with four reference floor maps); "yes start with the lattice foundry prototype"; "search for references of buildings online if needed".
- References looked up: Cold War bunkers (Harnekop: ~150 rooms on 3 levels; Horsham: corridor -> control room opening onto four rooms; Amersham: rooms off a long corridor, escape tunnels; blast doors and decontamination at the entrance) and Soviet research-institute sketches (CIA reading room: labs and workshops off corridors, several floors), plus ring corridors round lab blocks. Used: decon lock at the entrance, a spine with rooms off it, a parallel service corridor, an escape/maintenance duct, a control/observation room between two spaces, plant rooms, a stair between levels.
- `TierDungeons.Plans.cs`: `FloorPlan` reads a text plan, one character a tile ('#' wall, '.' floor, ',' concrete, 'D' door, h/m/l hard cover, d/c light cover, 'p' pipes, capitals = landmarks and exits). Plans are generated by `ArtSource/plans/foundry_plans.py` (rooms, corridors, rings, doors), which checks every floor tile is reachable and prints the plan to paste in.
- The Lattice Foundry is now two floors:
  - Test level (`lattice_foundry`, 74x36): lift lobby -> two-chamber Decon Lock -> the west checkpoint hall; four offices between a 3-wide spine and a 1-wide service corridor (doors both ways, and from office to office); the Service Stair room; the Observation Room between the spine and the Firing Range (a 43x10 lane with concrete blocks - long-gun ground); Foundry Storage (a stash); the lift's maintenance duct, a 1-wide pipe run from the lobby past the storage to the range's back door - the close-quarters way round.
  - Synthesis level (`lattice_foundry_lower`, new dungeon zone, 74x48): the stair landing; a ring corridor round the round Coil Hall (four doors in, the Cell Synthesizer at the centre ringed by machines); the long cable tunnel along the north wall; the crew corridor with three bunk rooms; the Foreman's Office (door to the ring, door to the crew corridor); the Plant Room under the ring, joined to the crew corridor and the stair.
- `G7Lab`: an Obelisk guard squad holds the test level (made once); the Foreman, his crew and the synthesizer are on the synthesis level. The old 7x3 "Lattice Foundry" room grid is gone.
- Renders: `maps/renders/lattice_foundry.jpg`, `lattice_foundry_lower.jpg`. Known look issue: wall masses 2+ tiles thick inside a building draw as black voids (like rock), e.g. between the spine and the range.
- Harness: the Foundry test now walks both floors (guards upstairs with no G-7, the stair both ways, the Foreman downstairs). The War Spoils test accepts any squad on that map taking them (a fresh squad mustering at the Scrapyard outpost could get there first). ALL CHECKS PASSED twice.

## Batch 92 - grit, blood and a light system (2026-10-14)

Yan: "make stuff less 'neat', everything is too organized and simetrical, also make it dark, gritty, dirty, bloody, add a light system, make some places dark requiring a flashlight / use the stalker labs as a reference / keep working until you hit the usage cap".
- New floor decals (`ArtSource/world/ground.py`, PIL): blood pools, spatter, blood trails (blotchy, read in any direction), grime/mould stains, scorch marks, papers, broken glass; registered as decals in `WorldArt.Decal` (plus oil and litter).
- Indoor floors are dirty: water stains (two noise layers) and dirt banked against the walls, darker underground (`WorldArt.PaintGroundLocked`, `indoor`).
- **Light system** (`LightMap.cs`): street lamps (`lamp`), campfires, ceiling lights (`light`) and red emergency lamps (`redlight`) bake a per-cell light map when a map loads (walls and closed doors block it). `Vision.Light` counts it, so lamp-lit rooms show from the dark and enemies standing in them can be seen. The darkness overlay tints lamp pools (warm work light, red emergency light) (`AtmosphereView.DrawLight`).
- Underground is darker: your own light radius is 2.5 tiles (was 5), night 0.85 (was 0.7); the flashlight cone (10) and lamps do the seeing.
- **Flashlight toggle (U, action bar "Flashlight")**: off, you see only what lamps light, but in the dark enemies spot you at 60% range; standing in a lamp's pool you're spotted as by day (`LightMap.Exposure`, used by `Contact.Sees`/`IdleSight`). Not saved yet (starts on).
- Floor-plan legend grows: x debris (hard), b rust barrel / t trash (light), r rubble / w flooded floor (slow), i ceiling light, e emergency light, and decals * ' ~ % ` " : ; & (`TierDungeons.FloorPlan`).
- The Lattice Foundry plans redone messy (`ArtSource/plans/foundry_plans.py`): rooms with bitten corners and alcoves (`jag`), offices of different sizes, a pinched spine, holes knocked through walls (`breach`, extra paths), a caved-in roof mid-range and in the south ring (`collapse`), burst-pipe flooding, an off-centre coil ring with one door blown open, a kinked duct and cable tunnel; a story in blood (a stand at the decon lock, the wounded dragged to storage, one who crawled down the duct; a last stand round the synthesizer). Most of it is dark: only a few rooms keep working lights, red emergency lamps mark the lock, duct and tunnels.
- Bug fixed: blades could "jam" (the jam roll applied to melee). `CombatRules.JamChance` is 0 for blades. The harness caught it when map changes shifted the dice.
- Render harness: `LIT=1 Tools/render/render.sh <zone>` draws the darkness and lamp pools as the game does (no player light).
- Renders: `maps/renders/lattice_foundry.jpg`, `_lower.jpg` (plain) and `_lit.jpg` (dark with lamps).
- ALL CHECKS PASSED.
- **The other labs as floor plans.** Secret Labs Alpha, Beta and Gamma, the Underground Bunker, Sub-Station Depths and the Maintenance Tunnels were a 4x-stamped room grid (one big hall, crates in rows). Now each is a generated institute floor (`ArtSource/plans/lab_plans.py`, pasted by `to_cs.py` into `TierDungeons.LabPlans.cs`, built by `TierDungeons.Lab`): a spine corridor that jogs, rooms of every size off both sides (furnished as offices, stores, labs, wards, plant rooms or archives), back corridors along the top and bottom edges with doors into the rooms (a second way round), one long hall (the long-gun ground), holes knocked through walls, cave-ins, flooding (the Sub-Station most), blood trails and few working lamps. Landmarks and exits keep their names (the story steps find them: 'E' = way in, 'G' = goal room, the farthest from the entry). `Plan.mend` carves a door from any pocket that overlapping rooms cut off, and every plan is checked reachable.
- **Grit pass for every other map** (`Grit.cs`, last step of `ZoneLayoutRecipes.BuildAll`): grime at the walls, oil, papers, glass, bones and 1-6 blood scenes on indoor floors (all maps); underground maps without hand-placed lamps also get ceiling lights 11+ tiles apart, ~30% working, ~15% red emergency, the rest dead (caves get none).
- **The Siege Ward Hospital** (Garrison's raid base, Tier 6 map at 68,8): was a solid block the carver cut into a tidy grid. Now a plan (`ArtSource/plans/hospital_plan.py`, stamped by the new `TierDungeons.StampPlan`, kept from the dresser): a shelled hospital held as a fort - sandbagged lobby with the main doors south, a side door in each wing, a kinked ward corridor with wards of different sizes both sides, the operating theatre (two doors, a blood trail to it), the quartermaster's office, the armory in the north-west corner (same spot, (76,14), two ways in), the east wing caved in, windows on the outer walls for long guns. Its lamps work (the base has power). New plan chars: 'n' window, 's' sandbags. Render: `maps/renders/siege_ward_hospital.jpg` (`NOROOF=1 Tools/render/render.sh <map>` shows buildings without roofs). Raid tests pass.
- Not done yet: the caves (Deep Caverns, Labyrinth, Catacombs) stay as they were. The six generated labs share one shape (entry west, spine, rooms both sides); per-lab hand edits would make them more distinct.
- Renders: `maps/renders/<lab>.jpg` and `<lab>_lit.jpg` for the six.
- ALL CHECKS PASSED.

## Batch 93 - art audit, reference photos, first redraws (2026-10-14)

Yan: "objects seem very generic, and plenty of stuff doesn't even make sense ... there's some 'AI hallucinations' around"; "start with the audit sheet, but remember how we talked about real lighting and shadows"; (on references) "you can use my personal e-mail for that"; "you can start the audit now".
- Audit (`maps/renders/AUDIT.md`): every prop rendered through the game renderer on its own patch at 09:00 and 17:30 in clear weather (`audit_props.png`), concept A over the Tier 1 farmstead at the same zoom (`audit_concept_vs_game.jpg`), look counts per map. Render tool options added: `PROP_SHEET=<looks>` (a test map, "prop_sheet"), `LOOK_AUDIT=1`, `RENDER_WEATHER=<Clear|Overcast|...>`. Findings: sun shadows only in clear weather and faint, the game starts overcast; objects about half the concept's scale; buildings show roofs not fronts; no power lines; ~12 props that read as nothing; sandbags and armchairs placed where they make no sense.
- Reference photos: `ArtSource/refs/fetch.py` pulls openly licensed photos from Openverse (Wikimedia/Flickr direct are rate-limited from the container) into `ArtSource/refs/<prop>/` with `credits.txt` and a contact sheet. Photos are references only; sprites are still drawn by hand in PIL.
- Redrawn from the photos at real size (16 px person = 1.8 m, ~11 cm a pixel): road barrier (the old one computed its sloped foot and never drew it), tyres (a stack plus one on edge, was a black box), foundation blocks with lifting loops (was a grey cube), bunker hatch (a steel hatch box on a concrete collar with a vent, was a dark box), broken column with rebar (was a grey slab), dry fountain (rim, empty basin, puddle, nozzle hub; was an "eye"), culvert (pipe in a headwall under a grass bank; was a ring on flat ground). Before/after render: `maps/renders/redraw_batch1.png`.
- ALL CHECKS PASSED.

### Planned (not built): physical props (Yan, 2026-10-14)
Yan: "since the characters are going to be very animated i think the physical world needs to be very well estabilished ... ENTER THE GUNGEON has some physics on the objects, i'd like stuff like that, rolling barrels, overturning tables" (mechanics as reference only, original names and art).
Proposed: overturning tables/desks into cover, kicking barrels/tyres so they roll, breakable crates/lockers/glass/sandbags, explosions shoving props, a material per prop (impact effect, sound, toughness). Comes after lighting and the prop redraws.
- Decided: "barrels do not need to explode when rolled, only when shot at" - a rolled barrel just rolls and stops.
- Yan, "yes to everything else": enemies use them too (flip tables, kick barrels); flipping/kicking costs 1 AP; a flipped table is cover from its front only; bullets go through thin materials (wood, tin) but not concrete. Materials (Yan: "YES ADD IT TO THE PLAN"): every prop and wall gets a material (metal, concrete, brick, wood, sand, earth, glass) that picks its impact effect, sound and toughness, done with the prop redraws. Yan: "tables are not cover unless flipped" (desks are light cover today; changed together with the flip action). Order agreed: lighting, reference library, prop redraws (with materials), building fronts and scale, then physical props.

## Batch 94 - sun lighting (2026-10-14)

Yan: "remember how we talked about real lighting and shadows ... shadows affected by the sun, gradual passage of time"; on breakable walls: "all cover COULD be breakable but that would required reworking the systems and animating all walls ... leave this for later" (parked).
- Sun shadows (`WorldArt.CastShadows`): soft edges (a 3x3 blur, two passes when overcast), darkest at an object's foot and lighter where its top lands, stronger (0.85, was a flat 0.58), cooler colour. Contact blobs stay under objects in the sun too.
- Overcast skies now give faint diffuse shadows (0.3) in the sun's direction; fog, rain, storm, night and indoors keep only the contact blobs.
- The sun moves every 10 game minutes (~2.4 s of exploring), was every 30. A re-bake of a 200x200 map takes ~0.56 s on a background thread (the blur is two passes over parallel rows, buffers reused).
- Clear skies more often: 34% on entering an area (was 22%); overcast clears up 30% of the time every 2 h (was 20%).
- Render tool: `GRADE=1` applies the in-game colour of the hour (half wash + saturation, filter, haze; no vignette), `TIME_BAKE=1` times a re-bake. The harness stub `Color.Lerp` returned its first argument (so every hour rendered as its keyframe); it lerps now.
- Renders: `maps/renders/sun_day.jpg` (Tier 1 farmstead 07:00-18:00 clear, noon overcast, with the grade), `sun_1800_zoom.jpg`.
- Knobs: `WorldArt.SunStrength`, `WorldArt.OvercastStrength`. ALL CHECKS PASSED.

## Batch 95 - cover drawn sideways (2026-10-14)

Yan (with a screenshot of a sandbag L): "cover like this needs to be drawn sideways too, otherwise it looks weird".
- New north-south sprites in the 3/4 view: `sandbags_v` (bags laid lengthwise in staggered pairs), `jersey_v` (the lit top down the middle, sloped sides, the barrier's end profile), `block_v` (block tops with joints and lifting loops). Each shows its top running up the screen and a face only at the south end; the next tile south covers the face of the one above, so a run reads as one wall.
- `WorldArt.RunLook`: sandbags, barriers, blocks and fences (incl. torn fence) in a north-south run (same kind above or below, none beside) use the `_v` sprite. Before, only hand-placed `fence_v` turned. Render: `maps/renders/cover_runs.png`. ALL CHECKS PASSED.

## Batch 96 - small fixes: saved toggles, fighting positions, corners, barrels, power lines (2026-10-14)

Yan said yes to all five small items.
- Sneaking and the flashlight survive save/load (`PlayerSaveData.Sneaking`, `.FlashlightOff` - stored as "off" so old saves load with the light on).
- Fighting positions (`CoverWrap.Worth`): the random sandbag/log Ls only go within 10 tiles of a named place or 4 of a road - no more fortifications in empty meadows. About half as many now; the harness minimum is 5 a map (was 10).
- The audit's armchair flag was wrong: 31 of 35 are indoors, the other 4 by campfires. Left as they are.
- Corners: where a north-south run meets an east-west one, the corner tile draws the east-west piece with the north-south top rising out of it (`props.corner`, `sandbags_c`, `jersey_c`, `block_c`, `logs_c`), so the runs join. Logs got a north-south sprite too (`logs_v`), and pieces of a run no longer get the random nudge natural props have (`WorldArt.InRun`). Render tool: `PROP_SHEET=L:<look>` builds an L. Render: `maps/renders/cover_corners.png`.
- Bug: explosive barrels were placed with no look, so they were invisible; they draw the red barrel now (it was the unused `barrel` sprite). The unused engine block joins the scrapyard and industrial junk.
- Bug: one-of-a-kind weapons (the G-7) could turn up in a guard's random kit, trader stock or loot; `EncounterGeneration.Unique` keeps them out (the harness caught a Foundry guard with the G-7 when the dice shifted).
- Power lines: each pole is joined to its nearest pole within 15 tiles by two sagging wires from the crossbar ends (`WorldArt.DrawWires`, painted with the ground; a tree north of a line can draw over a wire). Render: `maps/renders/power_lines.png`.
- ALL CHECKS PASSED.

## Batch 97 - ruins that look ruined (2026-10-14)

Yan (screenshot of a Tier 1 ruin): "walls look too neat for a decaying building, also the grass has a line that goes over it?"
- The line: a hedgerow (`TierMaps.Hedge`, y=146) ran straight across the ruin at (126,142) built just before it. Hedges now skip
  kept spots (`TerrainCanvas.Kept`), so they stop at ruins and yards.
- Broken walls: `walls.py ruin_wall` builds a `_ruin` version of every brick, plaster and plank wall piece: a rubble top
  (exposed brick / splintered boards instead of the clean concrete cap), each column sunk by a ragged amount (north-south
  runs only a little, so they stay joined), knocked-out bricks, moss at the foot.
- `WorldArt.FindOutside` floods the open air from outdoor ground through gaps (not walls or doors); a house wall with no closed
  room beside it stands in a roofless ruin and draws its `_ruin` piece. Intact houses, concrete/metal walls and everything
  underground are untouched. A roofless floor is overgrown with grass in patches.
- `TierMaps.Ruin`: earth round a ruin, floorboards only inside (it painted a strip of planks outside the walls).
- Render: `maps/renders/ruin_walls.png`. ALL CHECKS PASSED.

## Batch 98 - worn walls, no black voids inside buildings (2026-10-14)

Yan said yes to "weathering on intact buildings" and "the black voids inside buildings".
- Worn walls (`walls.py wear`): a `_worn` version of every brick, plaster, plank and concrete wall piece - chips out of the top
  edge, rain stains down the face, plaster fallen off in patches showing brick, a crack. About 40% of intact walls use it,
  picked per tile (`WorldArt.StandingAt`). Render: `maps/renders/worn_walls.png`.
- Black voids: underground, a cell walled on all 8 sides drew as dark rock, so any wall 3+ tiles thick between rooms showed a
  black stripe (the Foundry's band between the spine and the range). Now a wall mass with rooms on both sides within 6 tiles
  is a partition (`WorldArt.Buried`), and every fully walled cell that isn't rock draws a plain concrete wall top (`_slab`)
  instead of the overground tar-roof piece. Rock beyond the rooms stays dark. Render: `maps/renders/lattice_foundry.jpg`.
- ALL CHECKS PASSED.

## Batch 99 - the last unreadable props (2026-10-14)

Yan: "use whatever usage i have left to do whatever you can from the list".
- Playtest checklist for Batches 72-98: `maps/PLAYTEST.md`.
- Redrawn from reference photos (`ArtSource/refs/debris|trash|scrap|pipe`): debris (tilted concrete slabs, bent rebar, a snapped
  plank, grit; 1408 tiles use it), scrap (bent rusty sheets, a pipe, a wheel rim), rubbish (black bin bags, one split, litter
  spilled round them; was a smudge), brush (dark woody stems with pale dead tips; it drew as nothing on dirt).
- Overhead pipeline (`pipe`/`pipepost`): two foil-wrapped district-heating pipes with faint bands and the odd tear showing rusty
  steel, on a concrete T support. Floor plans' 'p' now draws a new `floorpipe` lying on the floor - the labs drew the overhead
  line floating in mid-air.
- Render: `maps/renders/redraw_batch2.png`. ALL CHECKS PASSED.

## Batch 100 - the caves as plans (2026-10-14)

- Deep Caverns, the Underground Labyrinth and the Siege Catacombs were the last 4x-stamped room grids (`HiddenNode`, now
  deleted). `ArtSource/plans/cave_plans.py` generates them (pasted by `to_cs.py` into `TierDungeons.LabPlans.cs`, built by
  `TierDungeons.Lab`): caverns are round chambers with lobes joined by wandering tunnels (some get a second way), edges eroded,
  boulders to fight round (new plan char 'k', hard cover), a cave-in, blood and bones; Root Hall is choked with hanging roots
  ('v', slow; a new `roots` sprite), the Crystal Pool is still water with one faint light, the Cave-In Chamber half collapsed.
  The catacombs are brick-lined tunnels in broken, jogging stretches, bone niches along the walls, the Ossuary's shelves and the
  Sealed Crypt behind one door at the far end. Landmark names are unchanged (the story finds the Sealed Crypt).
- `Plan.mend` can join pockets with an open passage instead of a door (caves).
- Batch 98's concrete wall top is now only for built lab walls (concrete, metal, hull, plaster); rock and brick-lined earth
  stay dark (the caves drew grey plateaus).
- Render: `maps/renders/caves.jpg`. ALL CHECKS PASSED.

## Batch 101 (2026-10-14): shipping containers as a wall material
- Containers were rectangles of Solid cells each drawing a full 46 px `p_container`, so the sprites piled on top of each other.
  Now "container" is a wall material (`walls.py` `container_face`: ribbed rusty steel, rails, rust runs) and in
  `WorldArt.BuiltWall`, so a container rectangle draws as one 3/4 corrugated block like a wall block. Atlas repacked (2420 sprites).
- Yan asked (same day) about dropping the tile grid for BG3-style free distances. Options given: full free positions, a hybrid
  (hidden finer grid, smooth movement, AP by metres, off-grid props), or half tiles. Recommended the hybrid, before physical props.
  Waiting on his call.

## Batch 102 (2026-10-14): free movement, step 1 - diagonals, MP by distance, straight routes
Yan: "Go with 2, id like if things could be moved on a diagonal too, free movement and physics" (option 2 = the grid stays
underneath as the rules' map, but movement and props stop looking and feeling like a grid). Plan, in order: (1) this batch;
(2) sub-tile positions: units stand anywhere, ranges and moves in real distances; (3) physical props (tables, barrels...).
- `ICombatant.Mp` is a float. A diagonal step costs 1.5x (`TacticalGridSystem.Diagonal`, about its real length);
  `IGridService.StepCost(unit, from, to)` is the one cost rule (climb measured from the tile you step from), and a diagonal
  can't squeeze between two blocked corners (infinite cost). A* (`Pathfinding.FindPath`) searches 8 ways with an octile
  heuristic; the player's reach outline and the AI's `EnemyAI.Reachable` use the same costs and diagonals.
- `Pathfinding.Smooth` pulls a route taut: a straight (Bresenham) line replaces part of the route when every step on it can
  be taken and it costs no more, so clicks walk straight lines instead of Ls. NPCs walk the same straightened routes.
- `UnitMotionView.CornerCut`: walkers turn for the next waypoint 0.45 tiles early, so routes read as lines and curves.
- HUD MP pips round down. Saves keep MP as a float (old int saves load fine).
- Harness: a diagonal costs 1.5, no diagonal past a wall corner, an open-ground route hugs the straight line.
- Probe (BuildsProbe targets, base vs Batch 102, same code otherwise): averages unchanged - open fight vs 1 94.0 -> 94.4%,
  ambush on a squad 61.8 -> 65.0%, open fight vs a squad 13.0 -> 11.2%. Single rows swing up to 45 points (40 fights a row,
  so partly noise); blades lost the most (Obelisk Ritual Blade vs Obelisk ambush 72 -> 27%). Watch melee in playtests.

## Batch 103 (2026-10-14): physical props, step 1 (Gungeon-style)
Yan (earlier): rolling barrels and overturning tables like Enter the Gungeon; "barrels do not need to explode when rolled,
only when shot at"; "tables are not cover unless flipped". New `Props.cs`; props stay tiles, shoving moves or turns the tile.
- Standing desks are no cover, just something to climb over (2 MP; `Props.Settle` on a fresh map).
- Shove (left-click a desk, barrel or crate next to you, or the Shove button that shows when one is next to you; 1 AP in a
  fight, free exploring): a desk tips onto its side away from you (`desk_n/s/e/w`, light cover that only counts against shots
  from the side it faces: `Props.Shields` in `CoverUtil.GetCoverAt`); a barrel (rusty or explosive) rolls up to 6 tiles until
  something stops it, and knocks down whoever it hits (1-3 damage per speed, prone); a crate slides one tile.
- Blasts shove the props just outside their radius one tile outward, and tip standing desks (`Props.BlastShove`).
- Enemies with no cover flip a desk next to them if it would shield them (`Props.FlipForCover` in `EnemyAI.HumanTurn`).
- The view slides moved props to their new tile (`TacticalGridView.SlideProps`). Props roll over decals (wiping them).
- Art: `desk_flip(face)` in props.py (4 sprites), shove icon. Render: `maps/renders/flip_props.png`.
- Not yet: enemies kicking barrels at you, pushing flipped desks, materials (bullets through wood), new sounds.

## Batch 103b (2026-10-14): side-flipped desks redrawn
Yan: "desk flipped north and south look good but east and west looks pretty bad, in some sprites it has legs in others its a
solid object... search for references". Openverse photos of tipped tables were useless; Enter the Gungeon's own flipped-table
frames (wiki, `ArtSource/refs/desk_flipped_game/`, reference only) show the rule: tipped sideways, the top is a thin upright
strip seen edge-on, and the legs stick out of its back as stubs; every view shows the same parts. `desk_e/w` now: the slab
edge-on on the shielded side, with the same leg and drawer pedestal the north view shows, as stubs. Render `maps/renders/flip_props.png`.

## Batch 104 (2026-10-14): the reference library
Yan: "ok, you can start the reference library now" (step 1 of the agreed art plan). `ArtSource/refs/` now covers every world
prop, the building fronts (brick, plaster, panel blocks, plank, concrete, corrugated metal, windows, doors) and power lines
(poles and pylons): 61 folders, `INDEX.md` lists them with what to take from each. Openverse's matching is loose, so every
contact sheet was checked by eye and off-topic photos dropped (pine trees that were people, "rock" that was satellite shots...),
then thin folders topped up with plainer searches. `fetch.py` now adds to a folder instead of replacing it, `--drop` removes
photos by sheet number (and remembers them), and it waits out Openverse's rate limit. About 28 MB (thumbnails plus contact sheets).
Thin still (2-4 photos): barrel (red drums), bush, crate, flatcar, roots, rubble, tank, barrierpole, busstop.

## Batch 105 (2026-10-15): materials
The agreed plan: "prop redraws with a material each (impact effect, sound, toughness; bullets go through wood/tin, not concrete)".
`Materials.cs`: every prop look has a material (wood, tin, steel, concrete, stone, earth, plant, cloth).
- Toughness (`TileRules.MaxIntegrity(tile)`): brush 1, tin and cloth 2, wood 3, sandbags and concrete 6 (4 as light cover),
  steel 8, stone 10 (boulders, rubble). Doors and explosive barrels unchanged.
- Through-shots (`Weapon.Fire`): a round the cover stopped (missed by no more than the cover's AC) goes on through wood, tin,
  brush or cloth: always a graze, at half damage, never boosted by aiming or ambush; the cover takes the hit too. Concrete,
  stone, earth and steel stop it. Same rule for everyone.
- The view: a round that hits cover now ends at the cover (it used to fly past the target) and throws chips in the material's
  colour (splinters, dust, earth, leaves; sparks off metal), out the far side when it goes through; a sound per material
  (`hit_wood/tin/concrete/dirt`, new in `sfx.py`; the 65 old sounds are byte-identical).
- Harness: materials checks; the "refill musters at home" test now keeps its target zone empty while it waits (squads had walked
  in on their own, so no refill was ever needed - a seed-dependent test, not a bug).

## Batch 106 (2026-10-15): walls one storey tall
The audit: "the concept shows two storeys of wall ... the game shows mostly flat roofs with a thin strip of wall". Built walls
(brick, plaster, concrete, metal, plank, hull) were 18 px (2 m: a 16 px person is 1.8 m); now one real storey, 28 px (3 m)
(`walls.py STOREY`, `RoofView.Lift`). Doors grow to person height, shadows lengthen with them. Render `maps/renders/storey_walls.png`.
Not done: two storeys, bigger windows; true scale for trees (a real birch is 15-20 m = 130-180 px, the game's are 40-52) is Yan's call.
- Probe after Batch 105 (BuildsProbe targets vs the Batch 102 run): open fight vs 1 94.4 -> 93.7%, ambush on a squad 65.0 -> 64.4%,
  open fight vs a squad 11.2 -> 11.8%. No real change (the probe arenas use mostly hard cover).

## Batch 107 (2026-10-15): enemies kick barrels, tipped desks slide
- Enemies use the props too (agreed plan): a soldier next to an explosive barrel kicks it (1 AP) when it would roll to a stop right
  beside its foe and well clear of itself (`Props.KickAtFoe`, `EnemyAI.HumanTurn`), then the existing barrel shot
  (`HumanEnemy.FindBarrelNear`) sets it off. Otherwise it may tip a desk for cover, as before.
- A tipped desk can be shoved along a tile, keeping the side it faces (Gungeon pushes its tables).

## Batch 108 (2026-10-15): audit placement leftovers
- Armchairs: checked with `LOOK_AUDIT` - all but 4 are on indoor floors now (the Batch 97 ruins work fixed the fields); the 4 on
  dirt are the dumped chairs round the Bonfire Village camps, on purpose.
- Sunken trucks: one stood on a building floor (stamped over the marsh after the dresser). `Grit.Apply` now turns any sunken
  truck not in water into an ordinary wreck. The look audit also reports the ground under sunken trucks.
- Harness: the armory-loot test counted inventory entries; two identical random weapons stack into one, so it now sums quantities.

## Batch 109 (2026-10-15): five props redrawn from the reference library
At real size (16 px person = 1.8 m), from `ArtSource/refs/<name>/sheet.jpg`: `busstop` (a Soviet concrete shelter: slab roof,
a faded tile mosaic of a sun over waves on the back wall, a plank bench, moss), `engine` (a dumped tractor engine: valve cover,
manifold stubs, pulley, rust over green paint), `armchair` (0.9 m, was twice too big; three faded fabrics, stuffing out of a
tear), `dumpster` (a ribbed steel container, three paints, one lid ajar, rust from below and from the rim), `machine` (a lathe:
headstock and chuck, bed, carriage, tailstock, cabinet stand). Render `maps/renders/redraw_batch3.png`.

## Batch 110 (2026-10-15): A-Life posts, step 1 - and a Batch 102 walking bug
- `Posts.cs` (the "A-Life for big maps" plan in CLAUDE.md, Yan: it "makes sense"): the loaded map's landmarks are posts. Each
  squad on your map gets a home post (the nearest landmark it may walk into) and a task it keeps 3-6 hours: **hold** (round
  home, resting 1-3 h at a time), **patrol** (a loop of 2-3 posts within 60 tiles, then home), **scavenge** (look over a post
  nobody holds, then home) or **attack** (march on a nearby post held only by a weaker hostile squad; at most one attack per
  faction per day). Faction leanings: Raiders patrol most, Wanderers hold and scavenge, Obelisk hold, the rest mix; mutants
  keep to their nest by day and roam at night; quest crews and caravans keep their own business. A post's holders are simply
  the squads camped within 10 tiles. No log lines (no on-screen enemy intent). The leader still walks at half your pace and the
  squad keeps its ring round it (`TurnLoop.CheckVisionAndWander` asks `Posts.NextStop`; a map without landmarks strolls as before).
- Harness: a squad on the Tier 1 map over 30 game hours: tasks hold, scavenge, patrol; within 10 tiles of a post 76% of the time.
- **Bug found by that test (from Batch 102):** walkers out of a fight get a small MP top-up per step (2, "enough for a bush or a
  door"); a diagonal through a bush now costs 3, so squads froze in brush and marsh. Now `TurnLoop.WalkMp = 3` for the wander,
  NPC-vs-NPC fights, the companion and the Raider toll walk.
- The "refill" test now only checks that a refill comes from outside the zone (an existing squad on the road may be sent too).
- Not yet: faction bases, territory along roads, nests that breed, far squads hopping between posts off-screen, saving plans
  (they're rebuilt after a load).

## Batch 111 (2026-10-15): measured - how long to walk round a tier map
CLAUDE.md's open question ("Target: 1 to 3 hours to fully explore Tier 1 ... Not measured yet"). New probe
`Tools/probes/ExploreTime.cs` (results `explore_time_b111.txt`): from the first landmark, always walk the real A* route to the
nearest unvisited one, at the exploring speed. Walking only (no fights, looting, reading, dungeons):
Tier 1 (perimeter) 44 spots, 1357 tiles, **5.4 min of real time**; Tiers 2-6: 4.2-5.8 min each.
The cause: exploring walks 4.2 tiles a second (`UnitMotionView.ExploreSpeed`, Batch 67) = about 7.5 m/s, a sprint (a 16 px tile
is about 1.8 m); a 200-tile map is crossed in under a minute. Real walking pace would be ~0.8 tiles/s (about 30 min of walking
for Tier 1). Not changed: Yan set the pace after playtesting; his call.

## Batch 112 (2026-10-15): ordinary buildings less tidy
Open item from CLAUDE.md ("a less tidy generator for ordinary buildings"; Yan on the labs: "less neat ... dark, gritty, dirty").
`BuildingCarver` now dirties every carved building, only ever opening walls (every room stays reachable): half get a hole
knocked through an inner wall (sometimes two tiles wide), a quarter of the big ones a caved-in corner with the rubble and
debris spilled inside, a third of the desks lie tipped over (the Batch 103 sprites), litter, glass, papers and grime on about
a fifth of the floor. Deterministic per map.
- Harness: the posts test advanced the clock twice a step (`TickAwareness` already does), so the leader's walk-every-other-minute
  check always fell on the same parity and it never moved; fixed in the test (the game advances one minute a step).

## Batch 113 (2026-10-15): the PL-62 Kobza sprite
Open since Batch 79 (it borrowed the generic heavy art and the RL-44's hand points). Hand-pixelled in the weapon kit's format
(`ArtSource/weapons/hand/guns/PL62Kobza.py`, RPK pattern as a drawing reference only): the VKM family's light machine gun -
dark laminate club-foot stock, the VKM's receiver and plum grip, a longer heavy barrel with a plain muzzle, the bipod folded
under it, and the round 75-round drum. Menu sprite 64x32, in-hand 46x16 (the RL-44 is 44x14); `HeldWeapon.cs` grips
(15, 7.5) / support (31, 5.5) / muzzle (45, 3.5), replacing the RL-44 placeholder row (a duplicate key would have broken the
table). Preview next to the VKM and RL-44: `maps/renders/kobza_preview.png`.
Note: `kit.py`'s held-pose preview needs `ingame_sim.py`, lost with an old cloud session; the export here used `b_v2.render`
and `common.place` directly (`/home/claude/art/weapons` must point at `ArtSource/weapons` for the kit's imports).

## Batch 114 (2026-10-15): free movement, look - nobody stands on a tile's centre
Part of free movement step 2, the visual half (no rule changes): the player stops where you clicked inside the tile (up to
0.3 tile off its middle; walking by keys keeps to the middle), and every other unit has a fixed spot of its own up to about
a quarter tile off (`TacticalGridView.StandOffset`, `PlayerController.StandAt`). Corpses fall from the same spot. Rules (range,
cover, line of sight) still use the tile. To watch for: clicking the very edge of a unit standing off-centre can land on the
next tile.

## Batch 115 (2026-10-15): street lamp and stone pile redrawn
From the reference library: `lamp` is a Soviet street lamp now (a tapered precast concrete pole with cast-through square
holes, a steel arm bent out, a dead lamp head; a slight lean on some), `stones` a pile of six or seven field stones with
dark rims so they read apart (was two blobs).

## Batch 116 (2026-10-15): the path preview line
Yan (on ranges): "i'd like something like a line indicating the path the character is gonna take if you click there, it could be
green when in range and turn to red when out of range (not enough MP)". Also: walking speed "on hold" until he's tested more;
trees "bigger but have different sprites and sizes"; buildings "go with the concept", more varied shapes than squares and
rectangles, references per kind, some 2-3 storey buildings as separate maps like caves (next batches).
- `TacticalGridView.UpdatePathPreview`: hovering a walkable tile draws a dotted line (2 px dots every 4 px, on the pixel grid)
  from your feet along the route a click there would walk (the same `FindPath`, smoothing and diagonals), ending where you'd
  stop inside the tile. In a fight each step is green while the running MP cost fits your MP and red past it; exploring the
  line is pale. Hidden over a visible enemy (that's a shot), over UI, with a grenade or suppression armed, or with the targeting
  panel open. Recomputed only when the hovered tile, your tile or your MP change.

## Batch 117 (2026-10-15): bigger trees, many shapes
Yan: "make trees bigger but have different sprites and sizes for them, some variety". New in `zone_style.py`, in concept A's
palette and see-through canopies: `big_birch` (4 shapes: tall single, forked twin, slender young, broad old with hanging
strands; 70-112 px = 8-12 m), `big_spruce` (3: dense, scraggly with thin tiers, snapped top; 76-126 px, branches in tiers so
the edge is jagged), `big_snag` (3: forked, snapped bare pole, leaning; 48-92 px). Each kind keeps its last two variants small
(the old 40-52 px trees) as saplings, so woods mix sizes: birch 10 variants, autumn birch 6, spruce 8, dead 6. Shadows grow
with them. Sheet `maps/renders/trees_variety.png` (a 16 px person for scale), in a map `maps/renders/trees_in_map.png`.

## Batch 118 (2026-10-15): buildings in L and U shapes
Yan: "ALL of them are either a square or a rectangle, we need more varied building shapes". `BuildingCarver` now takes a room
off about two thirds of the buildings with 3+ rooms: a corner room makes an L, a room in the middle of a side a U (never more
than a third of the floor). The walls it shared stay as outer walls and their doors now open outside, so nothing is cut off
(if no door to the outside is left, one is punched into the new outer wall). The gap gets the ground round the building.
Render `maps/renders/building_shapes.png` (the abandoned city). Next: the concept's two-storey fronts, the references per
building kind, multi-storey buildings as their own maps.

## Batch 119 (2026-10-15): two-storey fronts, as in the concept
Yan: buildings "go with the concept". Brick, plaster and concrete buildings are two storeys now: a 40 px face (20 px a floor,
`walls.py STOREY2`), a floor band between the floors, windows on both floors in the "window" sprite, a new `_uwin` sprite (an
upper-floor window on a plain wall) that `WorldArt` puts on every other wall tile along a facade (some boarded), doors one floor
tall at the bottom. Plank and metal (huts, sheds, barns) stay one storey. Roofs sit on the right height per building
(`RoofView.Building.Lift`: 40 if most of its outer walls are brick/plaster/concrete, else 28). The world atlas is 1024x4096 now
(2581 sprites). Renders `maps/renders/two_storey_fronts.png`, `two_storey_farm.png`.

## Batch 120 (2026-10-15): buildings with upper floors, as maps of their own
Yan: "try making some 2 or 3 story buildings, these could be separate maps similar to caves". Three hand-drawn buildings
(`ArtSource/plans/house_plans.py`, checked for reachability, then `to_cs.py`), none of them a box:
- Tier 1, the Old Farmstead's farmhouse: an L, two floors (`farmhouse_upper`).
- Tier 4, Ghost Town's school: a U round its yard, two floors (`school_upper`).
- Tier 4, the Flats: a T with the stairwell sticking out, three floors (`flats_2`, `flats_3`).
The ground floor is stamped onto the tier map (`TierDungeons.StampHouse`, kept from the carver and dresser); its stair is an exit
to the floor above, a small map like a dungeon (`ZoneDef.Upstairs`, `TierDungeons.Upstairs`): a short hop with no travel time,
and you arrive by the stair you took. Upstairs is indoors in daylight (the hour's overcast light, sight 14), the HUD says
INDOORS, mutants don't nest there. Plan legend: `_` is outside a footprint (left as ground), `U` a stair up, `T` a stash.
Fixes on the way: exits on open ground inside a map no longer get a 3x3 gate punched round them (it broke the stairwell
walls; `ZoneExits.CarveGate`); arriving from another region of the same map lands at its
stair (`ZoneTravel.FindEntrySpawn`). Render `maps/renders/multi_storey.png`.

## Batch 122 (2026-10-15): roofs to fit each kind of building; fronts turned east and west
Yan: "different roofs that fit each kind of building" and "every building faces south, you can make some of them be facing east
or west as well, for variety".
- Roofs (`RoofView.Choose`/`PaintShaped`), from the walls' material and the building's size, after the `refs/bld_*` photos:
  village houses in brick get clay tile or wavy cement sheets; plaster houses painted green or red tin, or cement sheets; plank
  huts and barns roofing felt, bare or red tin; mid-size brick halls (schools, barns, warehouses) tin or cement sheets;
  big concrete or brick halls and metal workshops a saw-tooth (glazed north lights, tin slopes, gutters); small concrete
  boxes a bare slab with moss; blocks keep concept A's flat tar with a parapet. Pitched roofs are gables on plain boxes and
  hips on L, U and T shapes (each pixel's slope comes from the nearest roof edge, so any shape works); the north slope is lit,
  the east one in shade, ridges and hips get a lighter cap, moss along the eaves, tiles or sheets missing in places.
- Fronts: the carver puts a building's main door south for half the buildings and east or west for the rest (back doors
  as before), with a trodden dirt path out of it; huts left at the default south door turn east or west half the time
  when that side is open ground. A pitched roof's ridge runs along the front, so a house facing east or west shows its gable end
  (the wall rising into a triangle, a barge board, a loft vent) to the south. Tier maps now: 88 fronts south, 68 east or west,
  19 north.
- Fixed a Batch 120 mistake: the carver skipped every block inside a kept area (whole villages), so most of them stayed plain.
- Harness: every roof kind is on some tier map; a good share of fronts face east or west.
Render `maps/renders/roofs_by_kind.png`.

## Batch 123 (2026-10-15): the toll fix, a dialogue panel, interaction prompts, a living main menu
Yan: "yesterday during a playtest i still got the raider extorsion dialogue from outside of the screen, maybe we need to take a
look at the UI next, for dialogue and interactions, also a main menu screen of someplace in the map showcasing the passage of
time mechanic, so if you stay in the main menu long enough you see it turn to night or raining".
- The toll from off screen: `Vision.Sees` answers "yes" for every cell until line of sight has been worked out, which happens
  right after the view is rebuilt, so `Shakedown.Tick`'s "boss in sight" check could pass for a boss you couldn't see.
  Now the boss must be within 4 tiles, truly seen (`Vision.Ready`) and inside the camera's view (`TacticalGridView.OnScreen`).
- Dialogue (`ChoiceUI`, same API): a panel at the bottom in the HUD's look; who's talking (their sprite as a portrait, the
  faction's armband colour, name and rank), the text typed out, numbered answers (keys 1-5 or click, rows light up on hover),
  a light dim so the map stays visible. With a speaker on the map (the toll, a surrender) the camera eases to frame you both
  above the panel and a yellow chevron bobs over them.
- Interaction prompts (`InteractionPrompts`, drawn by `HudView.Prompts`): chips over what you can use near you, nearest
  first, at most 3: E for containers/locks/stashes, the Garrison armory and the G-7 synthesizer; F for an anomaly to search;
  CLICK for bodies with loot, items on the ground, and the desk/barrel/crate you point at next to you; WALK for exits and stairs.
- Main menu (`MenuBackdrop`, `TacticalGridView.TickBackdrop`, `MainMenuUI`): the menu is a column on the left over Bonfire
  Village by its fire, drawn by the real map view under the real sky. Its own clock runs a day in 3 minutes from 16:30, the
  weather drifts every 2 game hours as in play (night after ~40 s, rain after ~2.5 min on average); no squads, no A-Life.
  Place, day, clock and weather tick in the corner. The game log is hidden while the menu is up.
- Harness: no toll before sight is worked out; prompts over the farmhouse stair and a container; 30 menu minutes = 10 days
  with night and 3+ kinds of weather. Render `maps/renders/menu_backdrop.png` (the world behind the menu at four hours;
  the UI itself can only be seen in Unity).

## Batch 124 (2026-10-15): stashes that make sense and look like what they are
Yan: "i noticed that ''stashes'' are kind of randomly placed all over the world, and they are a tiny wooden dot instead of a
chest, box, bag (you get the idea)".
- Why: a tier map got ~35 objects from four kinds (Footlocker, Electronic Lock, Encrypted PDA, Rusted Blast Door), a quarter of
  them dropped on any open tile of the map, all drawn with the same small `icon_loot` marker at half a tile.
- Now (`TerrainMap.SpawnInteractiveObjects`): footlockers, old chests and toolboxes stand against an indoor wall; duffel bags,
  chests and tarp caches by a camp's fire; toolboxes and bags by a wreck or container; a keypad safe (the Electronic Lock) only
  against the wall of a concrete or metal building; an Encrypted PDA by a desk or a wreck. Never out in the open, never two
  within 5 tiles, none in a Safe Zone (the villagers' things), and the Rusted Blast Door (a door, not loot) is never scattered.
  The notes' named stashes stay where the recipe put them, on the nearest bare tile, as a cache, chest, bag or footlocker.
  Each is named for what it is ("Duffel Bag", "Keypad Safe"...), so the E prompt says so.
- Art (`props.py`, Batch 124 section): footlocker, old chest, duffel bag (2 colours), toolbox, tarp cache, keypad safe, PDA, each
  with an opened version - the tile switches to it once you've been through it (`TerrainMap.MarkOpened`), so a looted stash
  stays where it was, open and empty. Drawn by WorldArt like any prop (y-sorted, shadowed, lit), boxes are climbed over.
  Atlas 2597 sprites. Sheets: `maps/renders/stash_sprites.png`, `maps/renders/stashes_in_place.png`.
- Also: a world seeding gap - four draws in a row can all settle in a harder tier and leave a region empty at New Game
  (the marshes, once the random rolls shifted); `SquadManager.FillIfEmpty` keeps drawing until one stays.
- Harness: every stash drawn as a container, kept apart, opens; the whole suite passes on seeds 1-7.

## Batch 125 (2026-10-15): a simulated new character walks to the Hub - and what it ran into
Yan: "simulate a new character, starting in the world, walking around, getting into fights, going about quests until he reaches
the trading hub". `Tools/probes/Journey.cs`: a bot on the real maps and code (New Game, jobs from the list, a step = a game
minute through TurnLoop.TickAwareness, stashes opened with E, quest targets hunted and clues followed, fights with the BuildsProbe
bot, the toll paid, sleeping by a fire, ZoneTravel between maps). Results in `Tools/probes/journey_b125.txt`: 4 of 6 reach
Scavenger's Hub in about a game day and a half (~6-7 real minutes of walking and fighting), one Scavenger dies on day 1, one run stuck.
Bugs it found, fixed:
- A Blind Maw stood at a bank retrying every minute ("not enough MP"): the out-of-combat top-up (3 MP) couldn't pay a diagonal climb
  through brush (6). `TurnLoop.WalkMp` = 6, enough for any single step (callers still take at most 1-2 steps a top-up).
- Overloaded (max MP 2), the path search called any step over 2 MP a wall even while exploring - no route off the Tier 2 valley
  floor. Exploring tops MP up every step, so the wall is now what an unloaded walker can't climb (4) (`Pathfinding`).
- The Tier 2 road cut to the Hub was left 5 high on the cliff (only reachable by a long detour over the rim): lowered to the valley.
- Routes crossed exit tiles (a walk past the farmhouse stair took you upstairs): the player's routes only end on an exit.
- Jobs were handed out upstairs and in caves, their targets dropped into the room with you: none are offered in dungeons.
Open: run 5 got stuck going for a quest target near the Fishing Village (not reproduced); nobody strips bodies yet (the bot only
loots stashes); the loadout name in the probe must match GameBootstrap ("Ex-Military", "Freelance Trader", default Scavenger).
Harness: every map exit reachable from the others, an overloaded walker finds the road to the Hub, routes go round the stair,
no jobs upstairs.
- Batch 125b: `Journey.cs alife` (Yan: "What if we simulated the bots journey as if it was an a-life entity like the npc's?"): the
  bot takes tasks from Posts.NextStop like a Wanderers squad leader, rests at each stop, and leaves a map with the squads' migrate
  chance an hour. One run reached the Hub in ~8 game days (~43 real minutes) with 5 fights and 13 stashes; one died on day 4 with
  2 fights. Even living like a squad, it meets few enemies - the maps are big and the squads keep to their posts.

## Batch 126 (2026-10-15): fewer, richer, locked stashes that move weekly; dungeon end stashes; road checkpoints
Yan: "Fewer, richer stashes: about 8 per map, mostly locked, either requiring solving a puzzle/killing a difficult enemy (like a
mutant swalled the key or something) or inside places squads hold ... randomyze them every 7 days in game ... dungeons should
always have a stash in one of its ends ... some map exits can be checkpoints that squads hold too but not every one of them, like
how the cordon map in stalker has the military checkpoint (path to garbage) all the way north and is usually guarded by bandits,
but the paths to the swamps are not guarded". (An in-game day is ~5.7 real minutes of exploring: 1440 game minutes at 4.2 a second.)
- `Stashes.cs`: a tier map keeps its hand-placed note stashes (one-time finds with a story, some hold dungeon keys) and is topped
  up to about 8 with rolled ones, mostly locked: a Locked Chest whose key is in the toughest mutant on the map (or the hardest gang
  boss if there are none) - kill it and the key comes out ("Something glints in its gut"); a Keypad Safe whose code is on a
  Scrawled Note by another landmark (the lock says which: "the code's at Bus Stop"); and rich footlockers in buildings by the
  map's posts, where squads camp. Rolled stashes are rich (a higher-tier piece, 3-6 rolls and Ru); every 7 game days they're put
  down somewhere else. A dungeon has one rich stash at its far end (the walkable spot farthest from the way in), also renewed weekly.
  Ordinary loot is now bodies and quests. Per map now: 8-10 stashes (was ~35).
- `Checkpoints.cs`: on each tier map the first road up a tier is a checkpoint - a landmark a few tiles in from the gate, sandbags on
  the road's edges (the middle lane open), and a squad holding it (Job "hold"): Raiders on Tier 1's road north and Tier 2's road to
  the Hub, Contractors on Tier 3's road to Tier 4, Obelisk on Tier 4's. The other exits stay open. Wiped out, a checkpoint is held
  again two days later, put down only while you're on another map.
- Also: a squad's members no longer jam their own leader in a narrow gate - the leader steps past them (they trade places).
  Found when a squad stood at a map edge for days.
- Harness: stash counts and locks, the code note opens its safe, the toughest mutant carries the key and drops it, stashes move after
  7 days, a dungeon's stash at its far end; checkpoints held from the start, their holders at the post, sandbags, re-garrisoned.
  Render `maps/renders/checkpoint_and_locked_stash.png`.

## Batch 127 - natural hedge and wood edges (2026-10-15)

Yan: "also take a look at these grass or hedge generation, does everything need to be squared like that? we need some natural shapes"
(screenshot: the wood edge north of the Causeway, a band of square hedge cells with straight steps).

- `walls.py footprint`: an organic mass's open side is now a wavy edge (three random harmonics, pinned to the same depth at
  the cell's ends so neighbours still join), up to 6 px deep (was 0-2 px of noise), and a corner between two open sides is
  rounded (radius 6). The 3/4 face follows: its bottom corners round off at an open east/west side (no box-like sides).
- `walls.py leaf_face`: the canopy front uses the same leaf clusters as the top, a lit rim and shade down into it, trunks
  under it (was flat speckle, which read as a clipped box).
- `WorldArt.MassMask`: a foliage cell next to a core cell (drawn as forest floor with trees) is open on that side, so the
  canopy's edge round a wood is wavy too. The straight lines in Yan's screenshot were exactly that boundary. Cliffs and
  junk keep the plain mask (their top is the same surface as the core; opening it put teeth along the rock).
- Gameplay unchanged: tiles, cover and pathing are the same; only the sprites changed. Atlas repacked.
- Render: `maps/renders/hedges_natural_b127.jpg` (before/after, Tier 1).

## Batch 127b - why fights are lost, squad-breaking tools, varied rocks (2026-10-15)

Yan: "i playetested the game a bit usually starting with ex-military for the rifle and i have yet to win a player vs human
enemies fight". Asked "are the previous simulations all wrong?". Picked "Tactics that break squads" from the options.

**The simulations.** The old ones (Batches 75-85, `BuildsProbe targets`) were right: re-run on today's code, the Tier 1 rifle build
still wins open vs 1 100%, ambush on a squad 88%, open vs a squad 17%. The new start-kit run (first `start_kit_b127.txt`) was the
wrong one: the New Game kit has a knife in the second slot and the probe's sniper rule ("draw the pistol within 5") swapped rifle and
knife back and forth every turn. Fixed (`BuildsProbe.Run`: only for the sniper pairing). Corrected, the Ex-Military start wins open vs 1
85-92%, ambush on 3 Raiders 42-64%, open vs 3 2-9% (`Tools/probes/start_kit_b127.txt`). On target.

**Why Yan still loses.** A click route only stopped once a fight had already started (`PlayerController.TickClock`), so you walked
on into a squad's sight (about 12 tiles in front of it; you see 20), and every fight began as an open fight against the whole squad,
the case Yan's targets set "low". The surprise round existed (Batch 69) but nothing said it was on offer.
- `TurnLoop.CheckVisionAndWander`: the first time you see a squad that hasn't seen you, one log line ("Raiders ahead. They haven't
  seen you - shoot first for a surprise round, or sneak (N) round them.") and the route stops (`PlayerController.StopWalk`).
- Squad-breaking tools (Yan's pick): Suppress (S) for every automatic gun, radius 2 (4 with the Suppressive Fire perk, which used to
  gate it); a kill shakes squadmates within 4 tiles as well as those who saw it; Shaken costs 1 AP (on top of -2 to hit, falls back).
  Measured: these barely move the open fight vs 3 (3-9% either way). The probe bot also learnt to suppress, but only once the squad
  is alert (suppressing first threw away the surprise round: ambush vs 3 fell to 3%). No enemy stats changed.
- Harness: an unaware squad is called out once and stays unaware.

**Rocks** (Yan: "the road is full of these rocks? we need at least a few sprites for each stuff like those"): that grey band is the
top of The Cutting's embankments (cliff, height 3), not a road. `props.py rock`: seven kinds (boulder, slab, split, cluster, shard,
mossy, sunk in grass) in three tints, 14 variants (was 4 near-identical boulders). Cliff tops also get scrub and pebbles
(`WorldArt.StandingAt`), so they read less like asphalt.

## Batch 127c - detection meter and the "!" (2026-10-15)

Yan: "since the game is 2d the enemies are almost always offscreen, im not sure if that's because when im playing on unity the
screen is smaller or if somethings broken". Ideas: "a detection system for enemies based on line of sight, depending on which way
they're moving" and "an awareness meter for the player (perception stat boosts it)... a small ! appears on his head, similar to the
metal gear !... enemy npcs can have this as well, yellow when they detect something and red when they confirm".

Not broken: the close view shows 7 tiles up and down (`TacticalGridView.CameraViewHalfHeightTiles`), ~12 to the sides at 16:9 (fewer
in a narrow Game tab), while you see 20 and enemies about 12; the view only eased out for enemies already hunting you.
Facing-based sight was already in (`Contact.IdleSight`: 120-degree cone ahead, 3 tiles behind, halved sneaking, night/weather/light).
- `Contact.Notice`: spotting takes a moment. Each look (a game minute exploring, a round in a fight) an enemy that can see you adds
  0.1-0.5 to `Mutant.Alert` by how close you are inside its sight (at once within 3 tiles); while it fills it stops and turns to look,
  out of sight it drains 0.05 a look. At 1 it spots you as before (`TurnLoop.CheckVisionAndWander`, `Mutant.SpottedAt`).
- `Vision.SenseRange/Senses`: you sense a hostile you can see within 14 tiles at Perception 10, +1 a point, up to 20.
- `TacticalGridView`: a pixel "!" - white over you while you sense a hostile (out of a fight, even off screen), yellow over an enemy
  whose alert is filling, red for 2.5 s when it spots you. The camera also eases out (up to the far zoom) to fit enemies you sense.
- CLAUDE.md rule 7 notes the exception (Yan's own proposal).
- Harness: first look only raises alert, spotted after 3-8 looks at 8 tiles facing you, sneaking at 8 never, next to it at once.
- For Yan to check in Unity: the "!" size and colours, the camera easing out when a squad comes into sense range, how long
  spotting takes at walking pace (about 1-2 s real at 8-12 tiles).

## Batch 128 (step 1) - armor back views, for review (2026-10-15)

Yan: "we might need to add the back as well so the player can know which way the npc's are looking (at least up or down)" -
"go ahead with step 1, front and back is enough".
- `make_armor_avatars.py`: `HEADS_BACK` (every head seen from behind: no face, eyes or lenses; mask straps, hair, bandana knot,
  exo helmet vents) and `build(..., back=True)` -> `_back()`: the same suit, pouches, camo, quilting, stripes; no zip, collar,
  chest canister or glints; a small pack between the shoulders (a tank on Survey suits, a spine plate on exos); the faction
  patch on the mirrored shoulder. Fronts are unchanged.
- `preview_armor_backs.py <Faction> <out.png>`: the review sheet (armband baked in like `export_to_unity.py`, on the mirrored
  shoulder for backs). `maps/renders/armor_backs_raiders.png`, `armor_backs_sentinels.png`.
- Not in the game yet (step 2, after Yan approves the look): all 180 into an `armor_back_atlas` plus walk frames, the
  facing switch (back when the last step went up the screen), the player's facing, the gun pointing where an unaware NPC
  looks (Yan's idea), and dialogue turning both sides to face each other (Yan: "it needs to take into account where both
  parties are talking from"). Known weak spot: the round full-face helmet (Citadel, Granit-9) has no face from the front
  either, so its back only differs by the pack.

## Batch 128 (step 2) - back views in the game, facing, guns, dialogue (2026-10-15)

Yan: "Yes" (the visor on the round helmet), "And start step 2"; "also make sure that when they're with their back turned, the
weapon is covered by the body and the hands change orientation"; earlier: "make their gun in hand point to the direction they're
looking/walking" and "dialogue ... needs to take into account where both parties are talking from".
- Art: the round full-face helmet ("sphere") gets a dark visor with a shine on the front (8 armors change: Granit-9, Citadel...);
  its back stays plain. `make_back_atlas.py` writes `armor_back_atlas.png` and re-bakes `armor_atlas.png` from the generator with
  the faction armband exactly as `export_to_unity.py` (only the 8 visor cells changed - checked). `make_walk_atlas.py` also makes
  `armor_back_walk_a/b.png`.
- `UnitSprites.Back/Away/Walk(away)`: a human whose aim or gaze goes more than ~17 degrees up the screen shows its back.
- `TacticalGridView.AimPoint`: one point drives both the sprite and the gun - you: the mouse (the speaker while one talks to
  you); a human enemy: you once it fights you, else the way it faces (`Mutant.Facing`: its last step, where it turned to look
  while spotting you, or the squad it shoots at in `NpcFights`). So an unaware NPC's gun points where it looks.
- `HeldWeaponView.Show(back)`: from behind, the gun and both hands sort under the body and the gloves flip vertically.
- `ChoiceUI.Show`: the speaker and squadmates within 8 turn to you (`Mutant.FaceToward`); you face the speaker. The world is
  already paused while a panel is up (`PlayerController` returns on `ModalOpen`).
- Harness: the up/up-diagonal rule, a dialogue turning the speaker and its squadmate (not another faction).
- For Yan to check in Unity: walking up shows backs (walk frames too), gun hidden behind the body when aiming up, glove flip
  reads right, unaware NPCs' guns point along their walk, the toll gang turning to you. Mutants keep one view (no back art).

## Batch 129 - banter and shouts (2026-10-15)

Yan: "Games like bg3, Stalker, Death road to canada, have npc's having little talks that add to the world ... you're scouting a
squad or a friendly squad passes by and you can see little words above their heads as if they're talking, or they acknowledge
the player with a 'hey'"; "when an enemy detects you they can shout something like 'SOMEONE'S HERE'".
- `Banter.cs`: lines over heads, in four voices (gangs: Raiders/Cartel/Outcasts; soldiers: Garrison/Sentinels/Contractors/IZC;
  the Obelisk; drifters: the rest), all original writing. Every game minute (`TurnLoop.TickAwareness`), for squads you can see
  within 14 tiles and not fighting: a friendly squad passing within 5 greets you once ("Hey." / "Watch yourself, rookie." by
  your standing); otherwise two members turn to each other and trade a two-line exchange every 14-30 s (night and rain lines
  join the pool). An enemy whose detection starts filling mutters ("Huh?", `Contact.Notice`); the first of a squad to spot you
  shouts ("SOMEONE'S HERE!", "CONTACT!", `TurnLoop.CheckVisionAndWander`).
- `HudView.Barks.cs`: typed lines in a dark box over the speaker (`TacticalGridView.UnitPos`), cream / yellow / red.
- Talk is visual only (it doesn't make a squad easier to find) - Yan didn't pick; easy to change.
- CLAUDE.md rule 7 notes banter next to the "!" exception. Harness: greeting once, a squad's exchange, one shout per squad.
- Mutant rework: waiting on Yan's answers (size for the big ones, side vs front/back views).

## Batch 130 (step 1) - mutant redraw for review, and the Blight Brute's boulder (2026-10-15)

Yan: "we'll need to rework the mutants to be in line with the rest of the game's quality now, they are still using the initial
'placeholder' designs"; sizes and views: "yes to both, make the blight brute even bigger, like a tank from left4dead"; "do not copy
the left4dead tank entirely though, make sure its different enough, also add an attack for him ... if he can't reach the player in
3 turns he throws a boulder he rips of the ground, if the player is hit it loses half his mp next turn".
- `make_mutants_v2.py`: all 9 (+3 cloaked) redrawn - shapes per material, the armor polish (lit top/left, shaded bottom/right,
  #141617 outline), signature pixels. Blind Maw: an eyeless hound that's all mouth. Pallid Prowler: pale hairless crawler, toxic
  drool, boils. Rot Swarm: four separate rats. Veil Prowler: lean long-legged stalker, one red eye. Alpha Prowler: spined, bone
  brow, two eyes, fangs (was an upright figure; now the prowlers' pack leader on four legs). Mind Weaver: gaunt, tall skull, long
  arms, jaw tendrils, violet eyes. Blight Brute 32x32: lopsided - a crusted club arm of blight and rock with glowing crystals, a
  long clawed arm, fungal stalks on the hump, a low head with a split jaw and one eye (not a copy of any game's tank). Dread
  Prowler 24x24: a big mangy leaper, ribs and spine showing, burning eyes. Void Weaver: a floating tattered shroud, a void
  under the hood. Review sheet: `maps/renders/mutants_v2_review.png`. Not in the game yet (step 2: walk frames, backs for
  the upright ones, mirroring, sprite loading for 24/32 px sizes).
- Boulder (`EnemyAI.ThrowBoulder`, in the game now): a Blight Brute that starts 3 turns without reaching you throws on the 4th
  (`Mutant.ChaseTurns`; up to 10 tiles, line of sight, its whole turn): 2d10+3 vs your AC (it arcs over cover), 25 damage, and you
  start the next turn on half MP (`StatusEffects.HalfMpNextTurn`, `Player.StartTurn`). Harness: thrown on the 4th turn, hits
  sometimes, half MP after a hit. No boulder sprite or arc yet (log line and a deep crunch).

## Batch 130b - mutants redrawn from Yan's references, review round (2026-10-15)

Yan: "they do not look good, i'll send some references so you can redo them"; sent references for the Pallid Prowler ("the white one
is the pallid prowler"), Blind Maw, Veil Prowler, Dread Prowler, Blight Brute and the rat swarm (the swarm image came inline only).
- References filed in `ArtSource/refs/mutants/` (drawing guides only). `mutant_from_ref.py`: cuts a creature off its flat
  background and prints a value map at sprite size; a straight mode-reduction was tried and was mush, as the weapon workflow
  warns, so every sprite is hand-placed over the map. `make_mutants_v3.py`: Pallid Prowler 24x16 (pale, starved, hunched, acid
  drool), Blind Maw 24x16 (hunched red hound, jaws in profile with tooth rows and saliva), Rot Swarm 24x16 (four rats, one
  sick and mangy), Veil Prowler 24x16 (dark lean hound, red eye, smoke off its back), Dread Prowler 32x24 (mid-leap, bone
  spines, ribs, burning eyes), Blight Brute 32x32 (rock-crusted arm with glowing crystals, fungal stalks, toothy mouth).
- Sheet: `maps/renders/mutants_v3_review.png` (reference beside each sprite, true size next to a human). Not in the game.
- Open: the references carry 4-6x the detail a 24-32 px sprite can hold; asked Yan whether that resolution is right. Veil
  Prowler needs another pass (its thin legs read spidery). Alpha Prowler, Mind Weaver, Void Weaver: no references yet.

## Batch 130c - mutants at double resolution, review round (2026-10-15)

Yan: "can't we draw them bigger and reduce their size similar to what we did with the guns?"
- Yes: like the in-hand weapons (32x16 at 32 px per tile), mutants are drawn at 2x and shown at 32 px per tile, so they cover the
  same ground as before (hounds 24x16 character px, Dread Prowler 32x24, Blight Brute 32x32) with four times the pixels:
  48x32, 64x48, 64x64.
- `mutant_from_ref.py`: `auto()` - cut the creature off its background (`crop` for the Veil reference's grey panel), a palette of
  6 body tones plus 1-2 bright accents (eyes, acid, crystals) that win their blocks, mode per block, despeckle, outline.
  `make_mutants_hd.py`: the five referenced mutants, all facing right; sheet `maps/renders/mutants_hd_review.png`.
- Flagged to Yan: these come from reducing his references, not from hand pixelling - if a reference is AI-generated that clashes
  with his rule 2; his call. The finer pixels sit next to 16 px humans the way the guns already do.
- Not in the game yet: needs Yan's OK, then hand touch-ups, walk frames, loading at 32 px per tile with a foot pivot, mirroring.
  Still no file for the rat swarm, no references for Alpha Prowler, Mind Weaver, Void Weaver.

## Batch 131 - mutant animations, review round (2026-10-15)

Yan: "the 5 you did look great, lets try animating them - for the dogs, a run animation cycle and bite/claw swipe; For the brute,
he drags his arm around with him and the rock throw and a melee swipe".
- `make_mutant_anims.py`: the double-resolution sprites are cut into parts and posed per frame, then outlined. Hounds: hind and
  fore legs (below the belly line, split front/back) swing from their hips in opposite phase, the body bobs, the head
  counter-bobs - `run` (6 frames); `attack` (6): gathers, lunges with the forelegs reaching, bite and claw with a white swipe,
  recovers (the neck is filled as the head moves out). Blind Maw's reference ground shadow is dropped so it doesn't swing.
  Blight Brute: `walk` (6) - short legs step, body bobs, the club arm lags and drags with dust at its foot; `throw` (8) -
  crouches, reaches down, rips a boulder out of the ground (dirt), heaves it overhead, hurls it (the boulder follows the hand
  until it flies; a drawn rock sprite); `attack` (6) - winds the claw arm back, sweeps it forward with a swipe arc.
- `mutant_from_ref.auto(do_outline=False)` and `make_mutants_hd.sprites(outlined=False)` give the un-outlined bases.
- Preview: `maps/renders/mutant_anims_preview.gif` (all 13 loops). Strips (`mutant_<key>_<anim>_strip.png`) are written by the
  script; not in the game yet (needs Yan's OK, then loading at 32 px per tile with a foot pivot, mirroring, hooking the
  attacks to EnemyAI's bite/leap/slam/boulder moments).

## Batch 131b - frame-by-frame pilot: Pallid Prowler (2026-10-15)

Yan: "does not look good, we'll need to draw frame by frame, the dogs bite without moving their mouth, the brute doesnt use his big
arm for stuff, their legs bug a lot".
- `make_mutant_frames.py`: the approved body and head stay; every leg is redrawn per frame from hand-keyed joints (hip, knee,
  hock, paw / shoulder, elbow, wrist, paw), thick at the thigh, lit on top, feet planted on the ground row; far legs half a cycle
  behind and a shade darker. Run: extended, front paw down, gathered, hind paws plant, push-off, swing. Bite: the lower jaw is
  cut free and hinged - it drops open (dark mouth behind the snout), the head lunges with the forelegs braced, the jaw snaps
  shut with a flash, then recovers.
- Preview: `maps/renders/pallid_frames_pilot.gif`. The other hounds and the Brute (club arm used for drag, slam and throw)
  follow once Yan OKs the pilot.

## Batch 131c - Pallid Prowler walk, second pass (2026-10-15)

Yan: "the walk animation is still not that great, it bugs a lot, also his legs look like stubs, he has no paws like the initial sprite".
- The jitter came from hand-keyed joints whose leg lengths and foot spots wobbled frame to frame. Now: fixed bone lengths
  solved with 2-bone IK (`ik`), feet on paths (`foot_at`: planted and sliding back at a steady speed for 60% of the cycle, then
  an arc forward), a 4-beat walk in 8 frames (legs a quarter cycle apart), no body bob; long strides so the legs reach out on
  diagonals like the reference; legs taper thigh 3 -> shin 2 and end in long pale toes along the ground (`toes`). The bite
  uses the same legs, braced forward for the lunge. Preview `maps/renders/pallid_frames_pilot.gif` (walk | bite).
- Fix (Yan: "the bite animation has both legs fused and a weird stub leftover, the walk animation also has the stub leftover"):
  the body cut left one of the old legs under the head (it only cleared x < 37); now every pixel below the belly line goes except
  the green drool. In the bite the near and far legs stand on clearly different spots instead of overlapping.
- Yan: "make his body bob around a little bit when his walking, otherwise it looks weird, and a few white parts inside his mouth to
  simulate teeth": the walk dips the body 1 px twice a stride (`WALK_BOB`), hips and shoulders with it, feet still planted; the open
  mouth has white fangs along its top and bottom edge.

## Batch 131e - Pallid Prowler with its original legs (2026-10-16)

Yan: "i don't like how much the legs changed for the animations ... the initial sprites ... had a lot more detail in the paws and legs,
the new ones make them kind of squish together and lose a lot of detail".
- `make_mutant_frames.py pallid_rig/pose_leg`: the four approved legs are cut out of the sprite one by one (`PALLID_LEGS`: pixel
  range, hip, knee, behind the body or not) and split at the knee/elbow. Each frame turns the upper part about the hip and hangs
  the lower part from the moved knee, flexing it while the leg swings forward (lifted a pixel); 4-beat order, 8 frames, the body
  bob kept. The bite braces the same legs (forelegs reach, hind legs push) with the hinged, toothy jaw. All leg pixels are the
  original ones (paws, toes, shading). The drawn-limb version (131c/d) stays in the file, unused.

## Batch 131f - Blight Brute frames, club arm rig (2026-10-16)

Yan on the knee-rig hound walk: "it looks fine for a walking animation, but if we wanted them to be seen running it would look weird.
add it to the list for we to come back to later / For now try the brute".
- **Later list:** a real RUN cycle for the hounds (gallop: legs gathered and stretched, body arching). The knee-rig walk stays as walk.
- `make_mutant_frames.py brute_rig/brute_pose/brute_anims`: the same idea as the approved hound legs. The original pixels, cut into
  the rock-crusted club arm (pivot at the shoulder; the shoulder itself stays on the body so a raised arm leaves no hole), the claw
  arm and the two legs. While crossing the body the club gets its own outline and is drawn a shade lighter so it reads.
  - Walk (8 frames, slow): alternate heavy steps, body bob, the club trails behind and is dragged back in, scraping dust.
  - Attack (9): club back, overhead, the body twists the shoulder forward and chops it down in front, swipe arc and dust.
  - Throw (12): crouch, the club punches into the ground at its side, rips a boulder out, hauls it back and over, hurls it forward.
  Strips `mutant_blight_brute_{walk,attack,throw}_strip.png` (100x94 frames); preview `maps/renders/brute_frames_pilot.gif`.

---------------------------------------------------------------------------------------------------------------------------
# REAL-TIME FORK (repo `grimmylock/agora-vai-action`, from Batch 131f; plan: `ACTION_DESIGN.md`)

## RT batch 1 - move, aim, shoot, enemies fire back (2026-10-19)

Yan: "i'd like to keep some of the tactical feel and stalker like slow crawl though, not hotline miami level of crazy,
somewhere in between it and max payne, stalker and zero sievert". "a headshot is a headshot if the bullet connects to the
sprites HEAD. No dice rolls only aim and weapon accuracy affect it". The fallback is frozen in agora-vai123 (`turn-based-fallback`).
- **Turns are gone in the game** (`Rt.On`, set by `PlayerController.Awake`; the harness keeps it off so its turn-based
  probes still run). The world clock always runs (a game minute every 1/4.2 s, fighting or not).
- **Moving (`Rt.cs`):** WASD moves you freely inside and across tiles (2.6 tiles/s), walls collide (a 0.22-tile body);
  Shift sprints (4.4, stamina 100, -18/s, back at 14/s after 0.8 s); Ctrl crouches (1.3, the old sneak); aiming slows you
  to 65%; a leg wound to 60%. Crossing into a tile is a real step for the rules (doors, anomalies, exits, regions).
- **Shooting (`Ballistics.cs`, `Gunplay.cs`):** left mouse fires (held for automatics), right mouse aims: the spread closes
  over the gun's handling time and the camera looks further (more with a sniper rifle). A round is a line over the ground;
  the crosshair's height on a body picks head / torso / arm / legs, from any side (Yan's "enemy above you" worry: tested from
  the side, above, below, diagonal). Spread scatters sideways and up/down; walking widens it, sprinting a lot, crouching
  narrows it, each shot kicks and the kick settles. Low cover stops rounds below its height (a head above it is still a
  target; cover right next to the shooter is fired over), wood/tin/brush/cloth let them through at half damage, walls stop
  them. Aiming at a barrel or cover with no body under the crosshair shoots the object. Part damage until limb health:
  head x1.6, torso x1, arm x0.6, legs x0.7; armor as before; an unaware target takes x1.3. Shotguns fire 6 pellets.
  Per class (ponytail: per-class table, per-gun tuning later): steadied/hip spread, seconds to steady, kick, recovery, rate,
  automatic (rifles/SMGs that could burst), reach, reload time; High Recoil kicks harder, a damaged gun spreads more.
- **R reloads over the gun's reload time** (Quick Hands 60%); R on a jam clears it in 0.8 s. Jams roll per round as before.
- **Enemies (bridge until RT batch 4):** each one in a fight takes an old-style turn every ~3 s (first after a 0.5-1.3 s
  reaction); its gunfire becomes real rounds (`Weapon.Fire` -> `Gunplay.NpcShoot`): rank decides steadiness and how often it
  goes for your head. Melee and mutant strikes still roll. Every 6 s of fighting is a round for upkeep (bleeding, radio
  calls, fight end).
- **Look:** the player sprite is drawn where you are (walk frames by distance); a 4-tick crosshair shows the spread at the
  mouse (red when reloading/empty/jammed, centre dot red over a body); no move range or path dots; the action bar and End
  Turn are hidden; the HUD shows FIGHT/EXPLORING and stamina. ESC pauses. E also loots a body next to you or picks up what's
  at your feet. Controls panel (F1) rewritten.
- Harness: `RtChecks` (parts by height from four sides both ways, low cover by height, firing over your own cover, wood
  through, walls, crouching, a Rookie at 6 tiles hits 30-60%). ALL CHECKS PASSED.
- **For Yan to check in Unity:** walking/sprinting/crouching feel and speeds; the crosshair; hitting heads vs bodies; enemy
  fire (too accurate? too slow at one turn per ~3 s?); reload timing; ESC pause; looting with E; crossing a map exit.

## RT batch 2 - limb health, wounds, downed, timed healing (2026-10-19)

Yan: "maybe modular limb health system like tarkov and stalker anomaly?", "the wounds could work in the limb system as IF bodypart
is below 60% it has a chance of getting a wound, goes higher the lower HP that part has", "Death downs the player, he can either
stay still and bleed out slowly or crawl behind cover at the cost of expending his bleed out faster".
- **`Body.cs`:** you and human enemies have six parts (head 35% of max HP, torso 80%, arms 45% each, legs 50% each). A round's
  damage comes off the part it hit and the HP pool (which everything else still reads). Arm/leg hits pick the side the round
  passed (`Ballistics.Hit.Side`). Head or torso at 0 = down (you) / dead (enemies). A limb at 0 breaks; more damage to it
  spills into the torso (x0.7).
- **Wounds (Yan's rule):** nothing above 60% part health; below, `(60 - part%) / 60 * 0.75` per hit (50% ~13%, 40% 25%, 20% 50%),
  x1.4 with hollow points, x0.6 with AP rounds. Head: concussion (20 s: +2 deg spread). Torso: bleeding. Arm/leg: fracture or
  bleeding (half each). Effects only from wounds/broken parts: a fractured/broken leg limps (60% speed, no sprint); each bad arm
  +2.5 deg spread and 1.5x reload time. Enemies: their fractures set the old `StatusEffects.Wound` (MP and aim penalties).
- **Down (you):** lethal damage in real time drops you instead (`Player.TakeDamage`), HP 1. You bleed out in 40 s lying still,
  2.5x faster while crawling (0.6 tiles/s). You can't shoot; you're a low target (30% height). Enemies stop shooting someone down.
  Only a stim (belt keys) gets you up (head and torso back to 25%). Any hit while down kills.
- **Items take time:** bandage 3 s (stops bleeding), splint 4 s (new item: sets the worst fracture, legs first; 60 RU, every
  trader), medkit 4 s, stim 1.5 s, others 2.5 s; you move at half pace meanwhile and firing cancels. Grenades and repair kits
  stay instant. Healing (medkits, rest) restores the parts too. Every start kit gets a bandage, a splint and a basic stim.
- **HUD:** the AC slot shows each part's health (H T A/A L/L, red X = fractured); chips for DOWN with the seconds left,
  broken/fractured parts, CONCUSSED, USING with time left. Down, you're drawn lying on your side with no gun in hand.
- ponytail: part health isn't saved (a load starts every part full; the HP pool is saved as before); NPCs die instead of going
  down; melee and blasts hit the HP pool only.
- Harness: wound odds, breaking a leg and the spill, an enemy's torso, going down, nobody shooting you down, crawl drain,
  bandage refused / stim accepted while down, bleeding out, splint. ALL CHECKS PASSED.
- **For Yan to check:** how fast fights end now that heads and torsos can drop someone; going down and crawling; stim/splint/bandage
  timing (belt keys 1-4); the part readout on the HUD.

## RT batch 3 - the dive, bullet time, Focus (2026-10-19)

Yan: "the max payne part could be a double edged thing, where you jump and get the slowmo but when it ends you're prone for a tiny
moment, immobilizing you and making it risky". Also: "concussion seems too long, 20 seconds is a lot during gameplay, set it to 10" (done).
- **Space dives** the way you're moving (standing still: toward the mouse), 4.5 tiles/s for 0.55 s of game time (~2.5 tiles),
  sliding along walls and landing early if one stops you. Time runs at 30% while you're airborne; you can aim and fire, and your
  aim steadies at real-time speed (the rest of the world is slowed: enemy timers, everyone's fire rate, your own gun's rate too).
- **Landing:** prone 0.6 s (perk Tumbler: 0.35) - no moving, you can still shoot. Airborne or sprawled you're a low target
  (40% height): head-height rounds go over you. Time goes back to full speed on landing.
- **Focus** (0-100, HUD): costs 40 a dive, so no chaining. Fills from your kills (+25), hits (+4) and enemy rounds that pass within
  0.6 tiles of you without hitting (+6; perk Cold Blood: +12).
- **In bullet time** the part under the crosshair lights up (head / torso / legs band of the sprite) and your hits wound 1.5x as often.
- New perks: Tumbler, Cold Blood (Survival group). ESC during bullet time pauses and resumes it. Controls panel: Space.
- ponytail: team bullet time waits for co-op; the dive has no art of its own (the body tilts into it and lies flat after).
- Harness: no Focus no dive, bullet time on and 40 spent, ~2.5 tiles and prone on landing, head-height rounds over you while
  sprawled, no moving while prone, up and walking after, near-miss geometry. ALL CHECKS PASSED.
- **For Yan to check:** the dive's distance and slow-mo length; whether 0.6 s prone feels risky but fair; how fast Focus fills.

## RT batch 4 - human enemies fight in real time (2026-10-19)

- **Thinking stays, acting is continuous.** The old turn AI already had the tactics (roles Leader/Anchor/Flanker, morale and
  rallying, cover and flanking tiles from `PickTile`, frags at a player who camps, flashbangs, smoke, props, searching where you
  were last seen). With `Rt.On`, `EnemyAI.HumanTurn` still runs every ~3 s but only decides: a goal tile (`RtAi.SetGoal`) or to
  flee (`RtAi.Flee`); it no longer walks and shoots inside that one frame.
- **`RtAi.Tick` every frame:** walks one tile at a time toward the goal at 2.2 tiles/s (leg wound: slower); fires while it sees
  you and you're within the gun's reach - bursts of 3-5 from automatics, single shots otherwise, then a 0.7-1.6 s pause (shorter
  with rank); walking or being pinned widens its aim; reloads take the gun's reload time; a jam takes a clear; out of rounds it
  closes in to swing ("I'm out!"). Someone down isn't shot at.
- **Suppression both ways:** your rounds that pass within 0.6 tiles of an enemy pin it for 2.5 s (it holds still, shoots half as
  often, wider); theirs pin you for 1.5 s (+2 deg spread).
- **Call-outs** over heads (Banter): "Reloading!", "Jammed!", "Grenade!" (before a frag), "Flanking!", "Pinned!", "I'm out!".
- Real-time fights burn more rounds: human enemies now carry 3 magazines of reserve (was 1) when the game runs in real time.
- Mutants keep the bridge (their whole move and strike every ~3 s). ponytail: real-time mutants (circling hounds, the Brute's
  charge) next if Yan wants them before testing.
- Harness: a 20 s fight with one gunman - rounds spread over the fight in bursts, a reload, one tile per step, pinned by a
  round over his head. ALL CHECKS PASSED.
- **For Yan to check:** whether squads feel alive (moving between cover, flanking, pausing between bursts) or too static/too
  aggressive; how often you get pinned; the call-outs.

## RT batch 5 - mutants in real time; how long the Tier 1 map takes to cross (2026-10-19)

Yan asked how fast a character crosses Tier 1 (can't open Unity right now). Computed from `Rt.cs` (walk 2.6 tiles/s, sprint 4.4,
stamina 100 at -18/s, back at 14/s after a 0.8 s pause): 200 tiles edge to edge - walking 77 s, sprinting each full bar then
walking until it's full again 58 s (tapping sprint whenever a bit came back: 73 s, the pause eats it); corner to corner 283
tiles - 109 s / 84 s. Real routes round buildings and walls: +20-40%. About 4-5 game hours a crossing. (Turn-based explored
at 4.2 tiles/s: 48 s edge to edge.)
- **Mutants** (the last bridge): `EnemyAI.MutantTurn` still decides every ~2 s (circle, chase, leap, ground slam, boulder after
  three chases, re-cloak, flee, staggers), but with `Rt.On` its moves only set a destination (`WalkTo` -> `RtAi.GoTo`,
  `Approach` -> `RtAi.Chase`, fleeing -> `RtAi.Flee`) and it no longer strikes in that frame. `RtAi.TickMutant` walks it at its
  own speed - 0.6 x its MP in tiles/s, 1.8 to 5: Pallid Prowler 2.4, Blind Maw 3.0, Rot Swarm / Dread Prowler 3.6, Brute 1.8 -
  re-aiming at you every step while chasing, and strikes whenever you're in its reach (bite/claw/psi as before: the old roll)
  every ~1 s, the big ones every ~1.6 s. The leap is still an instant jump next to you.
- Harness: a Blind Maw 10 tiles off reaches you in ~4 s (reaction + first decision included) and bites on its own clock; the
  Brute lags behind. ALL CHECKS PASSED.
- **For Yan to check:** hound pace vs yours (they're a bit faster than your walk, slower than your sprint), bite rate, the Brute.

## RT batch 6 - weapon and armor balance for real time; head protection (2026-10-19)

Yan's targets (weapon vs armor of its own tier): "assault rifles, 2 to 4 headshots; Snipers, 1 headshot, 2 chest; pistols ... 2 to 4
as well; Smgs, 4 to 6 (faster fire-rate); Shotguns, another wildcard, 1 to X, depending on range, AP ammo makes them lethal at close
range with 1 headshot kills; Machine guns, same as assault rifles but have higher recoil". "tier 4 to 5 ap ammo is basically
required". "we can have armor that have more deflect chance or weapons that can't be deflected".
- **Head 50 HP (was 35), head hits x1.3 (was x1.6)** (`Body.cs`, `Ballistics.PartMult`): a head hit is strong, not automatic.
- **Damage per class for real time** (`Gunplay.ClassDamage`): sniper x1.8, SMG x0.8, the rest x1 - the old per-round damage was
  tuned for one shot per turn. **Shotguns fade with range**: full to 4 tiles, down to 30% at 12 (`Gunplay.RoundDamage`, shared by
  the game and the probe).
- **Head protection** (`HeadGear.cs`), from the head each armor's sprite already wears (no new art): hoods/bare/balaclava 0;
  gas-mask and hazmat hoods 10%; steel helmets 25-40% by tier with 8-15% deflect; visor helmets 45%/15%; full-face 55%/20%;
  exoskeleton helmets 55-60%/20-25%. A head hit uses it instead of the body armor; a deflect does no damage and concusses 2 s.
  AP rounds cut it to 40% and halve the deflect. Hooks for Yan's idea: `DeflectBonus` per armor (Obelisk Heavy +3%, Sentinel
  Proto-Armor +5%) and `NoDeflect` per weapon (VK-127 Grom, AM-50 Sledge, G-7 Rail). The head type of all 180 armors is generated
  from the art scripts (make_armor_avatars.py + make_faction_armors.py); the harness checks none is missing.
- **Probe** (`TtkProbe`, `test ttk` prints it; table in `Tools/probes/ttk_rt6.txt`): every gun vs the middle armor of its tier
  (AP from tier 4), head and chest hits to drop, seconds to drop by chest if all hit. Medians: rifles 2 head / 4-5 chest, machine
  guns 2 / 4-6, pistols 3 / 4-8, SMGs 4-5 / 9-10, snipers 1 / 2, shotguns 1-2 head close (more far). All of Yan's targets pass
  as checks.
- ponytail: handling (spread, rate, recoil, reload) stays per class with the data-driven tweaks; a per-gun table when Yan has
  felt the classes in game. Ammo prices not rescaled yet (rounds per kill went up for automatics).

## RT batch 6b - armor variants per tier, heavy handguns can't be deflected (2026-10-19)

Yan: "calculate how each weapon performs against a few variants of armor of the same tier and see if there's any outliers",
"add the can't be deflected to the rk-12 and the high recoil handguns" (done: RK-12 Kuvalda, Black Harrier, Steppe Eagle join
`HeadGear.NoDeflect`). The probe now also prints every gun vs the lightest / middle / heaviest body armor of its tier and that
tier's best helmet (expected hits, deflects counted as misses) - `test ttk`, saved in `Tools/probes/ttk_rt6.txt`. Findings in the
reply to Yan (SR-21 Gadyuka weak, Halden Service Revolver strong, SMGs collapse vs heavy tier 2-3 armor, AP flattens tier 4-5 armor).

## RT batch 6c - HP and damage x10 (2026-10-19)

Yan: "would it be easier to balance if we made base HP 1000 instead of 100? that way we can fiddle a bit more with weapon damage
without being restricted to a low amount of number variations ... we do that, increase every weapon damage x10 as well and continue
from there" ("base human enemy hp i meant, but mutants as well" - the player is human too and takes the same rounds, so x10 too).
- x10: player and human enemies 1000 HP (Lucifer floor 500, the "Take it" ending +100); every mutant's HP and attack; every weapon's
  damage (Reinforced +30); medkits, stims, rations, energy drink; artifact regeneration and Lucifer's penalty; anomalies; frags,
  cooked-off grenades and barrels (400); the surge (600); the Brute's boulder (250) and ground slam (300); bleeding and toxic bleed
  (50 a round); starvation; rolling barrels; off-screen squad fights; Adrenaline Surge (under 300 HP, +200 temp HP); the old wound
  threshold (200); gib overkill (150); the gun-feel "typical damage" per class; descriptions that quote numbers.
- Unchanged on purpose: everything that's a share (armor, part shares, wound chances, morale by % HP). The hunger/sleep cost of
  healing stays per 10 *old* HP (now per 100).
- The finer scale removed a hidden bonus: armor rounding went up to the next whole point, which mattered at 10-30 damage (a
  shotgun pellet of 8.04 counted as 9). Some guns need a hit more now (SMGs ~+1 chest hit, a couple of shotguns 2 heads at 6 tiles).
- Outlier fixes on the new scale: SR-21 Gadyuka 150 -> 180, Corvan LMG 220 -> 240. All of Yan's class targets still pass.
- Harness tests updated to the new numbers (and the real-time gunman test now always gets a gun - its random loadout could be a knife).
