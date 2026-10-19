# Faction Armor & Weapon Loadouts

Companion to file 02 (armor) and file 04 (factions). Every human faction has its own armor line:
**3 armors per tier, Tier 1-5** (Light / Standard / Heavy). Armors marked * are the originals from
file 02 with unchanged stats; the rest are new and follow file 02's ranges. Each DR is still capped
at 75% in play. Prices follow file 07's tier bands; sell-back per file 08.

## How an NPC is equipped

1. **Armor:** take the NPC's faction line at the tier matching its rank (Rookie/Experienced T1-2,
   Professional/Veteran T2-3, Expert/Master T3-4, Legend T4-5), never more than 1 above the zone's
   tier. Roll 1d3 for Light / Standard / Heavy (Leaders always Heavy).
2. **Weapon:** roll the weapon category on the faction's d100 table below, then take a
   tier-appropriate weapon from that category (file 07 pools). A category with no pool at that tier
   rerolls. Every faction fields every weapon type - the table only sets how common each is.
3. **Combat Role** follows the weapon (file 04): Anchor / Rusher / Flanker, plus one Leader per squad.
4. **Look:** the NPC is drawn in its armor's sprite with a 1-pixel armband in its faction color.

## Weapon category weights (d100)

|Faction|Melee|Sidearm|SMG|Shotgun|Assault Rifle|Sniper Rifle|Heavy Weapon|
|-|-|-|-|-|-|-|-|
|Raiders|10|25|15|25|18|4|3|
|Outcasts|15|25|12|20|18|6|4|
|Wanderers|6|15|12|15|30|15|7|
|Prospectors|5|15|20|15|25|15|5|
|Garrison|4|10|12|10|38|12|14|
|Sentinels|4|10|10|12|34|12|18|
|Liberators|5|12|15|12|32|16|8|
|Contractors|4|12|22|12|30|14|6|
|Obelisk|6|8|10|10|34|20|12|
|Cartel|8|22|25|20|17|4|4|
|Surveyors|6|30|25|12|17|8|2|
|IZC|4|12|20|10|32|14|8|

## Armor by faction

### Raiders

|Tier|Type|Armor|Phys-DR|Anom-DR|Belt|Mods|MP|Weight|Price (Ru)|Notes|
|-|-|-|-|-|-|-|-|-|-|-|
|1|Light|Raider Tracksuit|10%|5%|2|1|4|3kg|250|-|
|1|Standard|Raider Jacket *|15%|10%|1|1|4|5kg|350|-|
|1|Heavy|Raider Patchwork Vest|20%|5%|0|1|3|9kg|350|-|
|2|Light|Raider Duster|15%|15%|3|2|4|4kg|700-750|-|
|2|Standard|Raider Stitched Coat *|25%|15%|1|2|4|4kg|850-950|-|
|2|Heavy|Raider Scrap Plates|30%|5%|1|2|3|8kg|800-850|-|
|3|Light|Raider Night Coat|25%|25%|3|2|5|6kg|1850-2050|-|
|3|Standard|Raider Veteran Coat *|35%|20%|2|2|4|6kg|2050-2250|-|
|3|Heavy|Raider Tire-Plate Armor|50%|10%|1|2|3|10kg|2200-2400|-|
|4|Light|Raider Ringleader Jacket|30%|35%|4|3|4|8kg|4500-4700|-|
|4|Standard|Raider Boss Coat *|45%|25%|3|2|4|9kg|4600-5050|-|
|4|Heavy|Raider Juggernaut|60%|20%|2|2|3|16kg|5200-5750|-|
|5|Light|Raider Warlord Greatcoat|45%|50%|4|3|4|12kg|11000-11550|-|
|5|Standard|Raider Looted Exo|65%|35%|2|3|3|30kg|11400-12600|Immune to Prone|
|5|Heavy|Raider Armored Warlord|80%|25%|1|2|2|30kg|12350-13650|-|

### Outcasts

|Tier|Type|Armor|Phys-DR|Anom-DR|Belt|Mods|MP|Weight|Price (Ru)|Notes|
|-|-|-|-|-|-|-|-|-|-|-|
|1|Light|Outcast Rag Poncho|5%|5%|2|1|4|4kg|200|-|
|1|Standard|Heavy Trenchcoat *|15%|10%|2|1|4|7kg|350|-|
|1|Heavy|Scrap-Plated Vest *|20%|5%|0|1|3|10kg|350|-|
|2|Light|Outcast Patched Overalls|10%|15%|3|2|4|5kg|650-700|-|
|2|Standard|Outcast Gas-Mask Coat|20%|15%|2|2|4|7kg|800-850|-|
|2|Heavy|Outcast Car-Door Armor|30%|5%|1|2|3|9kg|800-850|-|
|3|Light|Outcast Scavenger Rig|20%|25%|3|2|5|7kg|1800-1900|-|
|3|Standard|Outcast Salvaged Hazmat|30%|35%|2|2|4|9kg|2350-2600|-|
|3|Heavy|Outcast Sheet-Metal Suit|45%|10%|1|2|3|11kg|2050-2250|-|
|4|Light|Outcast Tarp Rad-Suit|25%|45%|4|3|4|9kg|4600-5050|-|
|4|Standard|Outcast Riveted Plate|40%|30%|3|3|4|11kg|4600-5050|-|
|4|Heavy|Proto-Exoskeleton *|65%|20%|2|2|3|20kg|5500-6100|-|
|5|Light|Outcast Hermit's Kokon|40%|60%|4|3|4|13kg|11400-12600|-|
|5|Standard|Outcast Junk Exo|60%|35%|2|3|3|30kg|11000-11550|Immune to Prone|
|5|Heavy|Outcast Scrap Titan|75%|25%|1|2|2|31kg|11400-12600|-|

### Wanderers

|Tier|Type|Armor|Phys-DR|Anom-DR|Belt|Mods|MP|Weight|Price (Ru)|Notes|
|-|-|-|-|-|-|-|-|-|-|-|
|1|Light|Leather Jacket *|10%|5%|1|1|4|3kg|250|-|
|1|Standard|Wanderer Canvas Jacket|15%|15%|1|1|4|5kg|400|-|
|1|Heavy|Wanderer Padded Coat|20%|10%|0|1|3|9kg|400|-|
|2|Light|Wanderer Trail Suit|15%|25%|3|2|4|4kg|850-950|-|
|2|Standard|Daybreak Suit *|25%|25%|2|2|4|6kg|1000-1150|-|
|2|Heavy|Wanderer Plate Carrier|30%|15%|1|2|3|8kg|950-1050|-|
|3|Light|Wanderer Ghillie Coat|25%|35%|3|2|5|6kg|2200-2400|-|
|3|Standard|Kokon Suit *|30%|80%|3|3|4|10kg|3800-4000|Thermal vision|
|3|Heavy|Wanderer Field Armor|50%|20%|1|2|3|10kg|2500-2800|-|
|4|Light|Wanderer Gorka-M|30%|45%|4|3|4|8kg|4900-5400|-|
|4|Standard|Wanderer Trailblazer|45%|40%|3|3|4|10kg|5500-6100|-|
|4|Heavy|Wanderer Hardened Kokon|60%|40%|2|2|3|16kg|6450-7100|-|
|5|Light|Wanderer Veteran Kokon|45%|70%|4|3|4|12kg|14250-15750|-|
|5|Standard|Wanderer Assault Rig|65%|45%|2|3|3|18kg|13300-14700|-|
|5|Heavy|Wanderer Exoskeleton *|70%|25%|3|2|3|40kg|11000-11550|Immune to Prone|

### Prospectors

|Tier|Type|Armor|Phys-DR|Anom-DR|Belt|Mods|MP|Weight|Price (Ru)|Notes|
|-|-|-|-|-|-|-|-|-|-|-|
|1|Light|Prospector Windbreaker|5%|20%|3|1|4|3kg|350|-|
|1|Standard|Rookie Field Suit *|12%|20%|2|1|4|4kg|400-450|-|
|1|Heavy|Prospector Rubber Coat|15%|15%|1|1|3|9kg|400|-|
|2|Light|Marsh Recon Suit *|20%|30%|3|2|4|7kg|1000-1150|-|
|2|Standard|Prospector Seeker Suit|20%|30%|3|2|4|6kg|1000-1150|-|
|2|Heavy|Prospector Rad-Cape|25%|30%|2|2|3|8kg|1100-1200|-|
|3|Light|Marsh Scout Suit|20%|40%|4|2|5|6kg|2200-2400|-|
|3|Standard|Prospector Anomaly Suit|30%|50%|3|2|4|8kg|2850-3150|-|
|3|Heavy|Marsh Assault Suit|45%|25%|2|2|3|10kg|2500-2800|-|
|4|Light|Prospector Deep-Zone Suit|25%|60%|5|3|4|8kg|5500-6100|-|
|4|Standard|Marsh Pathfinder Suit *|30%|60%|4|3|4|9kg|5800-6450|-|
|4|Heavy|Marsh Bulwark Suit|55%|35%|3|2|3|16kg|5800-6450|-|
|5|Light|Prospector Artifact Diver|40%|75%|5|3|4|12kg|14250-15750|-|
|5|Standard|Radiant Kokon *|40%|80%|4|3|4|12kg|15200-16800|Anomaly-check advantage|
|5|Heavy|Marsh Exo|75%|40%|2|2|2|30kg|14250-15750|Immune to Prone|

### Garrison

|Tier|Type|Armor|Phys-DR|Anom-DR|Belt|Mods|MP|Weight|Price (Ru)|Notes|
|-|-|-|-|-|-|-|-|-|-|-|
|1|Light|Garrison Conscript Uniform|15%|5%|2|1|4|4kg|300|-|
|1|Standard|Garrison Gorka Coat|20%|5%|1|1|4|6kg|350|-|
|1|Heavy|Garrison 6B2 Flak Vest|25%|5%|0|1|3|10kg|400|-|
|2|Light|Garrison Field Uniform|20%|15%|3|2|4|5kg|800-850|-|
|2|Standard|Garrison OZK Chem Suit|30%|30%|2|2|4|7kg|1200-1300|-|
|2|Heavy|Garrison 6B5 Armor|35%|5%|1|2|3|9kg|850-950|-|
|3|Light|Garrison Spetsnaz Gorka|30%|25%|3|2|5|7kg|2050-2250|-|
|3|Standard|Basalt-5 *|50%|15%|1|2|3|9kg|2350-2600|-|
|3|Heavy|Garrison 6B23 Assault|55%|10%|1|2|3|11kg|2350-2600|-|
|4|Light|Garrison Altyn Assault|35%|35%|4|3|4|9kg|4600-5050|-|
|4|Standard|Granit-9 Assault Suit *|55%|40%|2|3|3|14kg|6150-6750|Immune to critical hits|
|4|Heavy|Garrison Sapper Suit|65%|20%|2|2|3|17kg|5500-6100|-|
|5|Light|Garrison Ratnik-Z|50%|50%|4|3|4|13kg|11400-12600|-|
|5|Standard|Garrison Rad-Assault Suit|70%|45%|2|3|3|19kg|14250-15750|-|
|5|Heavy|Garrison Strelets Exo|85%|25%|1|2|2|31kg|13300-14700|Immune to Prone|

### Sentinels

|Tier|Type|Armor|Phys-DR|Anom-DR|Belt|Mods|MP|Weight|Price (Ru)|Notes|
|-|-|-|-|-|-|-|-|-|-|-|
|1|Light|Sentinel Recruit Jacket|20%|5%|1|1|4|4kg|350|-|
|1|Standard|Sentinel Watch Coat|25%|5%|0|1|4|6kg|400|-|
|1|Heavy|Sentinel Training Vest|30%|5%|0|1|3|10kg|450|-|
|2|Light|Sentinel Patrol Suit|25%|15%|2|2|4|5kg|850-950|-|
|2|Standard|Sentinel Service Suit *|30%|10%|1|2|4|6kg|850-950|-|
|2|Heavy|Sentinel Riot Plates|40%|5%|0|2|3|9kg|950-1050|-|
|3|Light|Sentinel Guardsman Coat|35%|25%|2|2|5|7kg|2200-2400|-|
|3|Standard|Sentinel Armored Suit *|45%|15%|1|2|3|8kg|2200-2400|-|
|3|Heavy|Sentinel Bastion Suit|55%|10%|0|2|3|11kg|2350-2600|-|
|4|Light|Sentinel Armored Suit Mk2|40%|35%|3|3|4|9kg|4900-5400|-|
|4|Standard|Sentinel Chem-Guard|55%|40%|2|3|4|11kg|6150-6750|-|
|4|Heavy|Sentinel Vanguard *|45%|30%|2|3|4|11kg|4900-5400|-|
|5|Light|Sentinel Citadel Suit|55%|50%|3|3|4|13kg|12350-13650|-|
|5|Standard|Sentinel Bulwark Exo|75%|35%|1|3|3|30kg|13300-14700|Immune to Prone|
|5|Heavy|Sentinel Proto-Armor *|85%|20%|1|2|2|50kg|12350-13650|Halves incoming AP-drain effects|

### Liberators

|Tier|Type|Armor|Phys-DR|Anom-DR|Belt|Mods|MP|Weight|Price (Ru)|Notes|
|-|-|-|-|-|-|-|-|-|-|-|
|1|Light|Liberator Bandana Jacket|10%|15%|2|1|4|2kg|350|-|
|1|Standard|Liberator Hoodie|15%|15%|1|1|4|4kg|400|-|
|1|Heavy|Liberator Tire Vest|20%|10%|0|1|3|8kg|400|-|
|2|Light|Liberator Windbreaker *|15%|25%|3|2|4|5kg|850-950|-|
|2|Standard|Liberator Breeze Suit|25%|25%|2|2|4|5kg|1000-1150|-|
|2|Heavy|Liberator Armored Hoodie|30%|15%|1|2|3|7kg|950-1050|-|
|3|Light|Liberator Skirmisher|25%|35%|3|2|5|5kg|2200-2400|-|
|3|Standard|Liberator Field Suit|35%|45%|2|2|4|7kg|2850-3150|-|
|3|Heavy|Liberator Guard Suit *|35%|35%|3|2|4|8kg|2500-2800|-|
|4|Light|Liberator Windbreaker II|30%|45%|4|3|4|7kg|4900-5400|-|
|4|Standard|Liberator Exo-Lite|45%|40%|3|3|4|9kg|5500-6100|-|
|4|Heavy|Liberator Guard Suit II|60%|30%|2|2|3|15kg|5800-6450|-|
|5|Light|Liberator Sky-Kokon|45%|70%|4|3|4|11kg|14250-15750|-|
|5|Standard|Liberator Vanguard Plate|65%|45%|2|3|3|17kg|13300-14700|-|
|5|Heavy|Liberator Exoskeleton *|65%|35%|2|3|3|35kg|11400-12600|-|

### Contractors

|Tier|Type|Armor|Phys-DR|Anom-DR|Belt|Mods|MP|Weight|Price (Ru)|Notes|
|-|-|-|-|-|-|-|-|-|-|-|
|1|Light|Contractor Fleece|15%|10%|2|1|4|3kg|350|-|
|1|Standard|Contractor Balaclava Jacket|20%|10%|1|1|4|5kg|400|-|
|1|Heavy|Contractor Light Carrier|25%|5%|0|1|3|9kg|400|-|
|2|Light|Contractor Light *|20%|15%|2|2|4|4kg|800-850|-|
|2|Standard|Contractor Urban Fatigues|30%|20%|2|2|4|6kg|1000-1150|-|
|2|Heavy|Contractor Plate Carrier|35%|10%|1|2|3|8kg|950-1050|-|
|3|Light|Contractor Recon Suit|30%|30%|3|2|5|6kg|2200-2400|-|
|3|Standard|Contractor ACE Base *|35%|30%|3|3|4|8kg|2350-2600|-|
|3|Heavy|Contractor ACE Heavy|55%|15%|1|2|3|10kg|2500-2800|-|
|4|Light|Contractor ACE Operator|35%|40%|4|3|4|8kg|4900-5400|-|
|4|Standard|Contractor ACE Scientific *|40%|45%|4|3|4|10kg|5500-6100|-|
|4|Heavy|Contractor ACE Assault|65%|25%|2|2|3|16kg|5800-6450|-|
|5|Light|Contractor Wraith Suit|50%|65%|4|3|4|12kg|14250-15750|-|
|5|Standard|Contractor ACE Commander|70%|40%|2|3|3|18kg|13300-14700|-|
|5|Heavy|Contractor Hardsuit Exo|85%|30%|1|2|2|30kg|14250-15750|Immune to Prone|

### Obelisk

|Tier|Type|Armor|Phys-DR|Anom-DR|Belt|Mods|MP|Weight|Price (Ru)|Notes|
|-|-|-|-|-|-|-|-|-|-|-|
|1|Light|Obelisk Novice Robe|15%|20%|1|1|4|4kg|450|-|
|1|Standard|Obelisk Acolyte Wrap|20%|20%|0|1|4|6kg|500|-|
|1|Heavy|Obelisk Zealot Vest|25%|15%|0|1|3|10kg|500|-|
|2|Light|Obelisk Pilgrim Coat|20%|30%|2|2|4|5kg|1000-1150|-|
|2|Standard|Obelisk Patrol *|35%|40%|1|1|3|9kg|1400-1500|-|
|2|Heavy|Obelisk Guard Plates|35%|20%|0|2|3|9kg|1100-1200|-|
|3|Light|Obelisk Seeker Suit|30%|40%|2|2|5|7kg|2500-2800|-|
|3|Standard|Obelisk Scientific *|35%|60%|2|2|4|9kg|3300-3650|-|
|3|Heavy|Obelisk Crusader|55%|25%|0|2|3|11kg|2850-3150|-|
|4|Light|Obelisk Prophet Suit|35%|60%|3|3|4|9kg|6150-6750|-|
|4|Standard|Obelisk Templar|50%|45%|2|3|4|11kg|6150-6750|-|
|4|Heavy|Obelisk Heavy *|62%|80%|1|2|3|16kg|9000-9500|-|
|5|Light|Obelisk Saint Suit|50%|75%|3|3|4|13kg|16150-17850|-|
|5|Standard|Obelisk Monument Plate|70%|50%|1|3|3|19kg|15200-16800|-|
|5|Heavy|Obelisk Exoskeleton *|75%|90%|1|2|2|40kg|23750-25000|Immune to Prone|

### Cartel

|Tier|Type|Armor|Phys-DR|Anom-DR|Belt|Mods|MP|Weight|Price (Ru)|Notes|
|-|-|-|-|-|-|-|-|-|-|-|
|1|Light|Cartel Runner Tracksuit|10%|15%|3|1|4|3kg|350|-|
|1|Standard|Cartel Leather Coat|15%|15%|2|1|4|5kg|400|-|
|1|Heavy|Cartel Smuggler Vest|20%|10%|1|1|3|9kg|400|-|
|2|Light|Cartel Courier Suit|15%|25%|4|2|4|4kg|850-950|-|
|2|Standard|Cartel Enforcer Jacket|25%|25%|3|2|4|6kg|1000-1150|-|
|2|Heavy|Cartel Lined Coat|30%|15%|2|2|3|8kg|950-1050|-|
|3|Light|Cartel Rad-Courier|25%|45%|4|2|5|6kg|2500-2800|-|
|3|Standard|Cartel Bodyguard|35%|35%|3|2|4|8kg|2500-2800|-|
|3|Heavy|Cartel Bruiser Plates|50%|20%|2|2|3|10kg|2500-2800|-|
|4|Light|Cartel Lieutenant Coat|30%|45%|5|3|4|8kg|4900-5400|-|
|4|Standard|Cartel Rad-Suit *|40%|65%|4|2|4|10kg|6750-7450|-|
|4|Heavy|Cartel Gold-Trim Heavy|60%|30%|3|2|3|16kg|5800-6450|-|
|5|Light|Cartel Boss Hazsuit|45%|70%|5|3|4|12kg|14250-15750|-|
|5|Standard|Cartel Kingpin Armor|65%|45%|3|3|3|18kg|13300-14700|-|
|5|Heavy|Cartel Black Exo|80%|35%|2|2|2|30kg|14250-15750|Immune to Prone|

### Surveyors

|Tier|Type|Armor|Phys-DR|Anom-DR|Belt|Mods|MP|Weight|Price (Ru)|Notes|
|-|-|-|-|-|-|-|-|-|-|-|
|1|Light|Lab Coat|5%|30%|3|1|4|3kg|450|-|
|1|Standard|Field Technician Overalls|5%|30%|2|1|4|5kg|450|-|
|1|Heavy|Surveyor Guard Vest|10%|25%|1|1|3|9kg|450|-|
|2|Light|HZ-90 Trainee Suit|5%|50%|4|2|4|4kg|1100-1200|-|
|2|Standard|NBC Suit *|25%|50%|1|1|3|8kg|1400-1500|Sealed gas hood (flavor)|
|2|Heavy|Surveyor Escort Armor|20%|30%|2|2|3|8kg|1000-1150|-|
|3|Light|HZ-95 Sampler|15%|60%|4|2|5|6kg|2650-2950|-|
|3|Standard|HZ-99 Survey Suit *|10%|70%|4|1|4|7kg|2850-3150|Immune to Toxic Bleed|
|3|Heavy|Surveyor Guard Plates|40%|35%|2|2|3|10kg|2650-2950|-|
|4|Light|HZ-99M Survey Suit *|20%|80%|5|2|4|8kg|6450-7100|-|
|4|Standard|HZ-99K Coldsuit|35%|65%|4|3|4|10kg|6450-7100|-|
|4|Heavy|Surveyor Containment Armor|50%|45%|3|2|3|16kg|6150-6750|-|
|5|Light|HZ-100 Prototype|35%|85%|5|3|4|12kg|15200-16800|-|
|5|Standard|Surveyor Field Lab Exo|55%|60%|3|3|3|30kg|14250-15750|Immune to Prone|
|5|Heavy|Surveyor Hazard Guard|70%|50%|2|2|2|30kg|15200-16800|-|

### IZC

|Tier|Type|Armor|Phys-DR|Anom-DR|Belt|Mods|MP|Weight|Price (Ru)|Notes|
|-|-|-|-|-|-|-|-|-|-|-|
|1|Light|IZC Liaison Jacket|15%|15%|2|1|4|3kg|400|-|
|1|Standard|IZC Observer Coat|20%|15%|1|1|4|5kg|450|-|
|1|Heavy|IZC Flak Vest|25%|10%|0|1|3|9kg|450|-|
|2|Light|IZC Field Uniform|20%|25%|3|2|4|4kg|950-1050|-|
|2|Standard|IZC Decon Suit|30%|35%|2|2|4|6kg|1250-1400|-|
|2|Heavy|IZC Peacekeeper Armor|35%|15%|1|2|3|8kg|1000-1150|-|
|3|Light|IZC Scout *|25%|30%|3|2|5|6kg|2050-2250|Light recon suit - 5 MP|
|3|Standard|IZC Containment Suit|40%|45%|2|2|4|8kg|3000-3300|-|
|3|Heavy|IZC Trooper|50%|20%|1|2|3|10kg|2500-2800|-|
|4|Light|IZC Recon Specialist|35%|45%|4|3|4|8kg|5200-5750|-|
|4|Standard|IZC Operative *|42%|35%|3|3|4|10kg|5000-5550|-|
|4|Heavy|IZC Breacher|65%|30%|2|2|3|16kg|6150-6750|-|
|5|Light|IZC Hazard Specialist|50%|70%|4|3|4|12kg|15200-16800|-|
|5|Standard|IZC Commander Armor|70%|45%|2|3|3|18kg|14250-15750|-|
|5|Heavy|IZC Containment Exo|80%|35%|1|2|2|30kg|14250-15750|Immune to Prone|
