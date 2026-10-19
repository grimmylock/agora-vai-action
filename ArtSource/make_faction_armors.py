# Faction armor catalog: 12 human factions x 5 tiers x 3 variants (Light / Standard / Heavy) = 180
# armors. The 37 armors from 02_Armor_and_Mods.md keep their exact stats and are slotted into their
# faction; the other 143 are new. New stats come from one tier x archetype table plus a small
# per-faction bias, so the whole catalog stays inside file 02's ranges.
# Outputs: faction_armor/<armor>.png (16x16), faction_armor_sheet.png, 02b_Faction_Armor_and_Weapons.md
import os, re, itertools, math
from PIL import Image, ImageDraw, ImageFont
import make_armor_avatars as av   # parts library + renderer (locked agora-vai style)

OUT = os.path.dirname(os.path.abspath(__file__))
SPR = os.path.join(OUT, "faction_armor")
os.makedirs(SPR, exist_ok=True)

# ---- the original 37 (02_Armor_and_Mods.md): phys, anom, belt, mods, MP, kg, note ----
FILE02 = {
    "Leather Jacket": (10, 5, 1, 1, 4, 3, ""), "Raider Jacket": (15, 10, 1, 1, 4, 5, ""),
    "Heavy Trenchcoat": (15, 10, 2, 1, 4, 7, ""), "Rookie Field Suit": (12, 20, 2, 1, 4, 4, ""),
    "Scrap-Plated Vest": (20, 5, 0, 1, 3, 10, ""), "Liberator Windbreaker": (15, 25, 3, 2, 4, 5, ""),
    "Marsh Recon Suit": (20, 30, 3, 2, 4, 7, ""), "Contractor Light": (20, 15, 2, 2, 4, 4, ""),
    "Daybreak Suit": (25, 25, 2, 2, 4, 6, ""), "Raider Stitched Coat": (25, 15, 1, 2, 4, 4, ""),
    "Sentinel Service Suit": (30, 10, 1, 2, 4, 6, ""), "NBC Suit": (25, 50, 1, 1, 3, 8, "Sealed gas hood (flavor)"),
    "Obelisk Patrol": (35, 40, 1, 1, 3, 9, ""), "HZ-99 Survey Suit": (10, 70, 4, 1, 4, 7, "Immune to Toxic Bleed"),
    "IZC Scout": (25, 30, 3, 2, 5, 6, "Light recon suit - 5 MP"), "Kokon Suit": (30, 80, 3, 3, 4, 10, "Thermal vision"),
    "Liberator Guard Suit": (35, 35, 3, 2, 4, 8, ""), "Contractor ACE Base": (35, 30, 3, 3, 4, 8, ""),
    "Obelisk Scientific": (35, 60, 2, 2, 4, 9, ""), "Sentinel Armored Suit": (45, 15, 1, 2, 3, 8, ""),
    "Basalt-5": (50, 15, 1, 2, 3, 9, ""), "Raider Veteran Coat": (35, 20, 2, 2, 4, 6, ""),
    "HZ-99M Survey Suit": (20, 80, 5, 2, 4, 8, ""), "Marsh Pathfinder Suit": (30, 60, 4, 3, 4, 9, ""),
    "Contractor ACE Scientific": (40, 45, 4, 3, 4, 10, ""), "Sentinel Vanguard": (45, 30, 2, 3, 4, 11, ""),
    "IZC Operative": (42, 35, 3, 3, 4, 10, ""), "Cartel Rad-Suit": (40, 65, 4, 2, 4, 10, ""),
    "Granit-9 Assault Suit": (55, 40, 2, 3, 3, 14, "Immune to critical hits"), "Proto-Exoskeleton": (65, 20, 2, 2, 3, 20, ""),
    "Obelisk Heavy": (62, 80, 1, 2, 3, 16, ""), "Raider Boss Coat": (45, 25, 3, 2, 4, 9, ""),
    "Radiant Kokon": (40, 80, 4, 3, 4, 12, "Anomaly-check advantage"), "Liberator Exoskeleton": (65, 35, 2, 3, 3, 35, ""),
    "Wanderer Exoskeleton": (70, 25, 3, 2, 3, 40, "Immune to Prone"), "Obelisk Exoskeleton": (75, 90, 1, 2, 2, 40, "Immune to Prone"),
    "Sentinel Proto-Armor": (85, 20, 1, 2, 2, 50, "Halves incoming AP-drain effects"),
}

# ---- stat model for new armors: (phys%, anom%, belt, mods, MP, kg) by tier x archetype ----
BASE = {
    1: {"L": (10, 10, 2, 1, 4, 3),  "S": (15, 10, 1, 1, 4, 5),  "H": (20, 5, 0, 1, 3, 9)},
    2: {"L": (15, 20, 3, 2, 4, 4),  "S": (25, 20, 2, 2, 4, 6),  "H": (32, 10, 1, 2, 3, 8)},
    3: {"L": (25, 30, 3, 2, 5, 6),  "S": (35, 30, 2, 2, 4, 8),  "H": (48, 15, 1, 2, 3, 10)},
    4: {"L": (30, 40, 4, 3, 4, 8),  "S": (45, 35, 3, 3, 4, 10), "H": (60, 25, 2, 2, 3, 16)},
    5: {"L": (45, 55, 4, 3, 4, 12), "S": (65, 40, 2, 3, 3, 18), "H": (78, 30, 1, 2, 2, 30)},
}
PRICE_BAND = {1: (200, 500), 2: (650, 1500), 3: (1800, 4000), 4: (4500, 9500), 5: (11000, 25000)}  # file 07

# faction: (armband color, lens, phys bias, anom bias, belt bias, kg bias)
FACTION = {
    "Raiders":     ("#b02a2a", None,      0, -5,  0,  0),
    "Outcasts":    ("#8a6a48", None,     -3, -3,  0,  1),
    "Wanderers":   ("#a0b04a", None,      0,  5,  0,  0),
    "Prospectors": ("#d4a62a", None,     -5, 12,  1,  0),
    "Garrison":    ("#9a8a5a", None,      5, -5,  0,  1),
    "Sentinels":   ("#3a6ab0", None,      8, -5, -1,  1),
    "Liberators":  ("#6fb03a", None,      0,  5,  0, -1),
    "Contractors": ("#c8d0d8", None,      5,  0,  0,  0),
    "Obelisk":     ("#e6e6e6", "#c46a5a", 5, 12, -1,  1),
    "Cartel":      ("#e0c030", None,      0,  5,  1,  0),
    "Surveyors":   ("#b8c43a", None,    -10, 20,  1,  0),
    "IZC":         ("#6fb0e0", None,      3,  3,  0,  0),
}

# ---- catalog: faction -> tier -> 3 entries. An existing armor is just its name (look + stats from
# file 02 / make_armor_avatars.LOOK). A new one: (name, archetype, head, body, suit, head color, extras)
G = "#d4a62a"   # gold trim
CATALOG = {
 "Raiders": {
  1: ["Raider Jacket", ("Raider Tracksuit", "L", "bandit", "jacket", "#2c3f8a", "#1c1c1c", dict(D="#2c3f8a", stripes=True, A="#2a2420")),
      ("Raider Patchwork Vest", "H", "hoodface", "vest", "#3a2f28", "#4a3a2c", dict(P="#6a5a48", N="#1c1c1c"))],
  2: ["Raider Stitched Coat", ("Raider Duster", "L", "bandit", "coat", "#5a4a38", "#2a2420", dict(A="#2a2420")),
      ("Raider Scrap Plates", "H", "balaclava", "vest", "#2a2726", "#4a3a2c", dict(P="#7a6a5a", N="#1c1c1c"))],
  3: ["Raider Veteran Coat", ("Raider Night Coat", "L", "hoodface", "coat", "#1e1e22", "#2a2a30", dict(N="#1c1c1c")),
      ("Raider Tire-Plate Armor", "H", "bandit", "heavy", "#3a2f28", "#8e2323", dict(P="#4a4a4a", A="#2a2420"))],
  4: ["Raider Boss Coat", ("Raider Ringleader Jacket", "L", "bare", "jacket", "#2a2420", "#2a2420", dict(arm=G, A="#1c1c1c", D="#2c3f8a", stripes=True)),
      ("Raider Juggernaut", "H", "helmet", "heavy", "#4a3a30", "#5a5a5a", dict(P="#6a6a6a"))],
  5: [("Raider Warlord Greatcoat", "L", "bare", "coat", "#3a1414", "#3a1414", dict(arm=G, A="#1c1c1c")),
      ("Raider Looted Exo", "S", "exo", "exo", "#6a2020", "#2a2420", dict(D="#3a2f28")),
      ("Raider Armored Warlord", "H", "helmet", "heavy", "#1c1c1c", "#3a3a3a", dict(P="#8e2323"))],
 },
 "Outcasts": {
  1: ["Heavy Trenchcoat", "Scrap-Plated Vest", ("Outcast Rag Poncho", "L", "hoodface", "coat", "#6e6a5a", "#5a5648", {})],
  2: [("Outcast Patched Overalls", "L", "hoodface", "suit", "#5a6a7a", "#6b6658", {}),
      ("Outcast Gas-Mask Coat", "S", "hood", "coat", "#5a5040", "#4a4436", {}),
      ("Outcast Car-Door Armor", "H", "hoodface", "vest", "#4a4a44", "#5a5648", dict(P="#8a3a2a"))],
  3: [("Outcast Scavenger Rig", "L", "balaclava", "jacket", "#6a5a3a", "#8a8070", dict(N="#1c1c1c")),
      ("Outcast Salvaged Hazmat", "S", "sealed", "suit", "#8a7a3a", "#7a6a30", {}),
      ("Outcast Sheet-Metal Suit", "H", "helmet", "heavy", "#4a4440", "#6a6258", dict(P="#7a7068"))],
  4: [("Outcast Tarp Rad-Suit", "L", "sealed", "suit", "#3a5a5a", "#2f4a4a", {}),
      ("Outcast Riveted Plate", "S", "hood", "heavy", "#5a4a3a", "#4a4436", dict(P="#9a5a30")), "Proto-Exoskeleton"],
  5: [("Outcast Hermit's Kokon", "L", "kokonmask", "suit", "#6a6a5a", "#8a6a4a", dict(quilted=True)),
      ("Outcast Junk Exo", "S", "exo", "exo", "#6a6a62", "#8a4e2c", dict(D="#4a4a44")),
      ("Outcast Scrap Titan", "H", "exo", "heavy", "#5a5a52", "#4a4a44", dict(P="#9a6a3a"))],
 },
 "Wanderers": {
  1: ["Leather Jacket", ("Wanderer Canvas Jacket", "S", "hood", "jacket", "#6a5a40", "#4f5a3a", {}),
      ("Wanderer Padded Coat", "H", "hoodface", "coat", "#5a4a36", "#7a6a50", {})],
  2: [("Wanderer Trail Suit", "L", "hood", "jacket", "#5a6a42", "#6a5a40", dict(camo=True)), "Daybreak Suit",
      ("Wanderer Plate Carrier", "H", "helmet", "vest", "#5a4a36", "#4f5a3a", dict(P="#4f5a3a"))],
  3: [("Wanderer Ghillie Coat", "L", "hood", "coat", "#4a5a32", "#3a4a28", dict(camo=True)), "Kokon Suit",
      ("Wanderer Field Armor", "H", "helmet", "heavy", "#6a5a40", "#4a4a3a", dict(P="#5a6a42"))],
  4: [("Wanderer Gorka-M", "L", "hood", "suit", "#6a6a4a", "#5a5a3a", dict(camo=True)),
      ("Wanderer Trailblazer", "S", "helmet", "vest", "#7a6040", "#4f5a3a", dict(P="#5a6e3a")),
      ("Wanderer Hardened Kokon", "H", "kokonmask", "suit", "#8a7a50", "#6a5a38", dict(quilted=True))],
  5: [("Wanderer Veteran Kokon", "L", "kokonmask", "suit", "#5a6a4a", "#8a7448", dict(quilted=True)),
      ("Wanderer Assault Rig", "S", "helmet", "heavy", "#5a4a36", "#4f5a3a", dict(P="#7a7f84")), "Wanderer Exoskeleton"],
 },
 "Prospectors": {
  1: ["Rookie Field Suit", ("Prospector Windbreaker", "L", "hood", "jacket", "#4a8aa0", "#3a6a80", {}),
      ("Prospector Rubber Coat", "H", "hoodface", "coat", "#3a6a7a", "#2f5a6a", {})],
  2: ["Marsh Recon Suit", ("Prospector Seeker Suit", "S", "hood", "suit", "#5aa0b0", "#3a7a8a", dict(arm=G)),
      ("Prospector Rad-Cape", "H", "sealed", "suit", "#2f6a7a", "#3a8090", {})],
  3: [("Marsh Scout Suit", "L", "hood", "jacket", "#3a8aa8", "#2a6a88", dict(camo=True)),
      ("Prospector Anomaly Suit", "S", "sealed", "suit", "#4a9ab0", "#2f7a8a", dict(arm=G)),
      ("Marsh Assault Suit", "H", "helmet", "vest", "#2f6a80", "#1f5068", dict(P="#3a86a0"))],
  4: [("Prospector Deep-Zone Suit", "L", "kokonmask", "suit", "#3a7a8a", "#2a5a6a", dict(quilted=True)), "Marsh Pathfinder Suit",
      ("Marsh Bulwark Suit", "H", "helmet", "heavy", "#24607a", "#1a4a60", dict(P="#4a90a8"))],
  5: [("Prospector Artifact Diver", "L", "sealed", "suit", "#6ab0c0", "#4a90a0", dict(arm=G)), "Radiant Kokon",
      ("Marsh Exo", "H", "exo", "exo", "#2a6f8a", "#3a8aa0", dict(D="#1f5068"))],
 },
 "Garrison": {
  1: [("Garrison Conscript Uniform", "L", "bare", "jacket", "#5a6040", "#5a6040", dict(D="#4a4a32", A="#3a2a1e")),
      ("Garrison Gorka Coat", "S", "hoodface", "coat", "#6a6a48", "#5a5a3a", {}),
      ("Garrison 6B2 Flak Vest", "H", "helmet", "vest", "#5a6040", "#4a5032", dict(P="#4a5032"))],
  2: [("Garrison Field Uniform", "L", "helmet", "jacket", "#6e6a48", "#4a5032", dict(camo=True)),
      ("Garrison 6B5 Armor", "H", "helmet", "vest", "#5a6040", "#4a5032", dict(P="#3e4a2a", camo=True)),
      ("Garrison OZK Chem Suit", "S", "hood", "suit", "#4a5a3a", "#3a4a2e", dict(anom_canister=True))],
  3: [("Garrison Spetsnaz Gorka", "L", "balaclava", "jacket", "#4a4d52", "#3a3d42", dict(N="#1c1c1c")), "Basalt-5",
      ("Garrison 6B23 Assault", "H", "visorhelm", "heavy", "#5a6040", "#262626", dict(P="#4a5032", camo=True))],
  4: [("Garrison Altyn Assault", "L", "visorhelm", "vest", "#6a6a48", "#4a4a3a", dict(P="#5a5a3a")), "Granit-9 Assault Suit",
      ("Garrison Sapper Suit", "H", "sphere", "heavy", "#4a5032", "#3a3f36", dict(P="#5a6040"))],
  5: [("Garrison Ratnik-Z", "L", "helmet", "vest", "#5e6a3e", "#3a4228", dict(P="#4a5432", camo=True)),
      ("Garrison Rad-Assault Suit", "S", "sealed", "suit", "#4a5a3a", "#3a4a2e", {}),
      ("Garrison Strelets Exo", "H", "exo", "exo", "#5a5e5a", "#3a3f36", dict(D="#4a5032", camo=True))],
 },
 "Sentinels": {
  1: [("Sentinel Recruit Jacket", "L", "bare", "jacket", "#2a3448", "#2a3448", dict(D="#1e2128", arm="#b02a2a")),
      ("Sentinel Watch Coat", "S", "hood", "coat", "#2d3a4c", "#20283a", dict(arm="#b02a2a")),
      ("Sentinel Training Vest", "H", "helmet", "vest", "#3a4658", "#2a3448", dict(P="#2a3448", arm="#b02a2a"))],
  2: [("Sentinel Patrol Suit", "L", "hood", "jacket", "#3a5070", "#2a3a50", dict(arm="#b02a2a")), "Sentinel Service Suit",
      ("Sentinel Riot Plates", "H", "helmet", "heavy", "#1e2128", "#2a3448", dict(P="#3a4658", arm="#b02a2a"))],
  3: [("Sentinel Guardsman Coat", "L", "hood", "coat", "#2a3a58", "#1e2840", dict(arm="#b02a2a")), "Sentinel Armored Suit",
      ("Sentinel Bastion Suit", "H", "visorhelm", "heavy", "#20283a", "#1e2128", dict(P="#46618c", arm="#b02a2a"))],
  4: [("Sentinel Armored Suit Mk2", "L", "helmet", "vest", "#3a5a8a", "#233a5c", dict(P="#2d4a70", arm="#b02a2a")),
      ("Sentinel Chem-Guard", "S", "sealed", "suit", "#2d3f5c", "#3a5070", dict(arm="#b02a2a")), "Sentinel Vanguard"],
  5: [("Sentinel Citadel Suit", "L", "sphere", "suit", "#2a3a58", "#1e2128", dict(quilted=True, arm="#b02a2a")),
      ("Sentinel Bulwark Exo", "S", "exo", "exo", "#3a5a8a", "#20283a", dict(D="#1e2128", arm="#b02a2a")), "Sentinel Proto-Armor"],
 },
 "Liberators": {
  1: [("Liberator Bandana Jacket", "L", "bandit", "jacket", "#4a6b3a", "#6fb03a", {}),
      ("Liberator Hoodie", "S", "hoodface", "jacket", "#5a7a40", "#3e5a2e", dict(N="#2a3a20")),
      ("Liberator Tire Vest", "H", "hood", "vest", "#4a6b3a", "#3e5a2e", dict(P="#3a4a30"))],
  2: ["Liberator Windbreaker", ("Liberator Breeze Suit", "S", "hood", "suit", "#7a9a42", "#5a7a34", dict(camo=True)),
      ("Liberator Armored Hoodie", "H", "hood", "heavy", "#4a6b3a", "#3e5a2e", dict(P="#5a7a50"))],
  3: [("Liberator Skirmisher", "L", "hood", "jacket", "#6a8a3a", "#8aa84a", dict(camo=True)),
      ("Liberator Field Suit", "S", "sealed", "suit", "#4f7a35", "#6f9a3a", {}), "Liberator Guard Suit"],
  4: [("Liberator Windbreaker II", "L", "hood", "suit", "#7aa04a", "#5f8a34", dict(camo=True)),
      ("Liberator Exo-Lite", "S", "helmet", "heavy", "#4f7a35", "#3e5a2e", dict(P="#6f9a3a")),
      ("Liberator Guard Suit II", "H", "visorhelm", "heavy", "#3f6a35", "#2f4f28", dict(P="#4f7a40", camo=True))],
  5: [("Liberator Sky-Kokon", "L", "kokonmask", "suit", "#6f9a3a", "#4f7a35", dict(quilted=True)),
      ("Liberator Vanguard Plate", "S", "helmet", "heavy", "#3f6a35", "#2f4f28", dict(P="#8aa84a")), "Liberator Exoskeleton"],
 },
 "Contractors": {
  1: [("Contractor Fleece", "L", "bare", "jacket", "#4a5568", "#4a5568", dict(D="#2a2a30")),
      ("Contractor Balaclava Jacket", "S", "balaclava", "jacket", "#3a4558", "#2a3448", dict(N="#1c1c1c")),
      ("Contractor Light Carrier", "H", "hood", "vest", "#4a5568", "#2a3448", dict(P="#8a7a58"))],
  2: ["Contractor Light", ("Contractor Urban Fatigues", "S", "hood", "jacket", "#6a7a98", "#3a4558", dict(camo=True)),
      ("Contractor Plate Carrier", "H", "helmet", "vest", "#3a4558", "#2a3448", dict(P="#8a7a58"))],
  3: [("Contractor Recon Suit", "L", "hood", "suit", "#7a8aa8", "#4a5568", dict(camo=True)), "Contractor ACE Base",
      ("Contractor ACE Heavy", "H", "helmet", "heavy", "#2a3a6a", "#1f2b50", dict(P="#6a6a5a"))],
  4: [("Contractor ACE Operator", "L", "visorhelm", "vest", "#4a5568", "#262626", dict(P="#8a7a58")), "Contractor ACE Scientific",
      ("Contractor ACE Assault", "H", "helmet", "heavy", "#1e2430", "#2a3448", dict(P="#5a6a80"))],
  5: [("Contractor Wraith Suit", "L", "sealed", "suit", "#2a2a3a", "#3a3a50", {}),
      ("Contractor ACE Commander", "S", "sphere", "heavy", "#2a3a6a", "#262a33", dict(P="#8a7a58")),
      ("Contractor Hardsuit Exo", "H", "exo", "exo", "#4a5568", "#2a3448", dict(D="#2a2a30"))],
 },
 "Obelisk": {
  1: [("Obelisk Novice Robe", "L", "hoodface", "coat", "#8a8478", "#6a6458", {}),
      ("Obelisk Acolyte Wrap", "S", "hood", "jacket", "#9a948a", "#7a7468", {}),
      ("Obelisk Zealot Vest", "H", "hood", "vest", "#6a6a6a", "#7a7a72", dict(P="#a8a8a0"))],
  2: [("Obelisk Pilgrim Coat", "L", "hood", "coat", "#b0aa9a", "#8a8478", {}), "Obelisk Patrol",
      ("Obelisk Guard Plates", "H", "helmet", "heavy", "#5b6066", "#8a8f94", dict(P="#c9cdd1"))],
  3: [("Obelisk Seeker Suit", "L", "hood", "suit", "#9aa0a6", "#6e7378", dict(camo=True)), "Obelisk Scientific",
      ("Obelisk Crusader", "H", "helmet", "heavy", "#4b4f54", "#d0d4d8", dict(P="#e0ddd0"))],
  4: [("Obelisk Prophet Suit", "L", "sealed", "suit", "#e8e4d8", "#c8c0a8", {}),
      ("Obelisk Templar", "S", "visorhelm", "heavy", "#6e7378", "#3c4046", dict(P="#d0d4d8")), "Obelisk Heavy"],
  5: [("Obelisk Saint Suit", "L", "kokonmask", "suit", "#d8d4c8", "#b8b0a0", dict(quilted=True)),
      ("Obelisk Monument Plate", "S", "sphere", "heavy", "#3c4046", "#8a8f94", dict(P="#ece4d0")), "Obelisk Exoskeleton"],
 },
 "Cartel": {
  1: [("Cartel Runner Tracksuit", "L", "bare", "jacket", "#4a2a5a", "#4a2a5a", dict(D="#4a2a5a", stripes=True, A="#1c1c1c")),
      ("Cartel Leather Coat", "S", "bare", "coat", "#1c1c1c", "#1c1c1c", dict(arm=G, A="#2a2420")),
      ("Cartel Smuggler Vest", "H", "hoodface", "vest", "#2e2e2c", "#3a3a36", dict(P="#5a4a2a"))],
  2: [("Cartel Courier Suit", "L", "hood", "jacket", "#5a1a2a", "#2e2e2c", {}),
      ("Cartel Enforcer Jacket", "S", "balaclava", "jacket", "#2e2e2c", "#6a5a2a", dict(N="#1c1c1c", arm=G)),
      ("Cartel Lined Coat", "H", "bandit", "coat", "#2e2e2c", "#c9a23a", dict(A="#1c1c1c"))],
  3: [("Cartel Rad-Courier", "L", "sealed", "suit", "#3a3a36", "#c9a23a", {}),
      ("Cartel Bodyguard", "S", "helmet", "vest", "#1c1c1c", "#2e2e2c", dict(P="#3a3a36", arm=G)),
      ("Cartel Bruiser Plates", "H", "bandit", "heavy", "#5a1a2a", "#1c1c1c", dict(P="#3a3a36", A="#2a2420"))],
  4: [("Cartel Lieutenant Coat", "L", "bare", "coat", "#4a2a5a", "#4a2a5a", dict(arm=G, A="#1c1c1c")), "Cartel Rad-Suit",
      ("Cartel Gold-Trim Heavy", "H", "helmet", "heavy", "#1c1c1c", "#2e2e2c", dict(P="#c9a23a"))],
  5: [("Cartel Boss Hazsuit", "L", "sealed", "suit", "#c9a23a", "#1c1c1c", {}),
      ("Cartel Kingpin Armor", "S", "sphere", "heavy", "#1c1c1c", "#2e2e2c", dict(P="#8a6a2a")),
      ("Cartel Black Exo", "H", "exo", "exo", "#232323", "#c9a23a", dict(D="#1c1c1c"))],
 },
 "Surveyors": {
  1: [("Lab Coat", "L", "bare", "coat", "#dcdcd4", "#dcdcd4", dict(A="#3b2a1e")),
      ("Field Technician Overalls", "S", "hood", "suit", "#c98a2a", "#a06a20", {}),
      ("Surveyor Guard Vest", "H", "helmet", "vest", "#5a6040", "#4a5032", dict(P="#c9a23a"))],
  2: [("HZ-90 Trainee Suit", "L", "sealed", "suit", "#e0b83a", "#c9a02a", {}), "NBC Suit",
      ("Surveyor Escort Armor", "H", "helmet", "heavy", "#4a5032", "#3a4228", dict(P="#c98a2a"))],
  3: [("HZ-95 Sampler", "L", "sealed", "suit", "#e07a2a", "#c0601a", {}), "HZ-99 Survey Suit",
      ("Surveyor Guard Plates", "H", "visorhelm", "heavy", "#5a6040", "#262626", dict(P="#b8c43a"))],
  4: ["HZ-99M Survey Suit", ("HZ-99K Coldsuit", "S", "sealed", "suit", "#e8e8e0", "#a6b42e", {}),
      ("Surveyor Containment Armor", "H", "sphere", "heavy", "#c98a2a", "#3a3f48", dict(P="#5a5a52"))],
  5: [("HZ-100 Prototype", "L", "sealed", "suit", "#f0d040", "#e0e0d8", dict(rebreather=True)),
      ("Surveyor Field Lab Exo", "S", "exo", "exo", "#c9a23a", "#e0e0d8", dict(D="#6a6a62")),
      ("Surveyor Hazard Guard", "H", "visorhelm", "heavy", "#a6b42e", "#262626", dict(P="#5a6040"))],
 },
 "IZC": {
  1: [("IZC Liaison Jacket", "L", "bare", "jacket", "#c8d0d8", "#c8d0d8", dict(D="#4a5568")),
      ("IZC Observer Coat", "S", "hood", "coat", "#8a96a2", "#4f86b8", {}),
      ("IZC Flak Vest", "H", "helmet", "vest", "#6a7b8a", "#4f86b8", dict(P="#4f86b8"))],
  2: [("IZC Field Uniform", "L", "helmet", "jacket", "#a8b4c0", "#4f86b8", {}),
      ("IZC Decon Suit", "S", "sealed", "suit", "#e0e4e8", "#4f86b8", {}),
      ("IZC Peacekeeper Armor", "H", "helmet", "heavy", "#5a6a78", "#4f86b8", dict(P="#c8d0d8"))],
  3: ["IZC Scout", ("IZC Containment Suit", "S", "sealed", "suit", "#6fa0c8", "#c8d0d8", {}),
      ("IZC Trooper", "H", "visorhelm", "heavy", "#4a5866", "#4f86b8", dict(P="#8a96a2"))],
  4: [("IZC Recon Specialist", "L", "hood", "suit", "#8aa0b8", "#c8d0d8", dict(camo=True)), "IZC Operative",
      ("IZC Breacher", "H", "sphere", "heavy", "#4a5866", "#d0d6dc", dict(P="#c8d0d8"))],
  5: [("IZC Hazard Specialist", "L", "kokonmask", "suit", "#c8d0d8", "#8aa0b8", dict(quilted=True)),
      ("IZC Commander Armor", "S", "helmet", "heavy", "#2a3a5a", "#4f86b8", dict(P="#d0d6dc")),
      ("IZC Containment Exo", "H", "exo", "exo", "#d0d6dc", "#4f86b8", dict(D="#6a7b8a"))],
 },
}

def r5(v): return int(5 * round(v / 5))

def new_stats(fac, tier, arch, head, body, look=None):
    phys, anom, belt, mods, mp, kg = BASE[tier][arch]
    _, _, bp, ba, bb, bk = FACTION[fac]
    phys, anom, belt, kg = phys + bp, anom + ba, belt + bb, kg + bk
    if head in ("sealed", "kokonmask"): anom += 10          # sealed hazmat hoods
    if look and look.get("anom_canister"): anom += 15     # chem suit + filter mask
    note = ""
    if body == "exo":
        mp = min(mp, 3); kg = max(kg, 20 if tier == 4 else 30)
        if tier == 5: note = "Immune to Prone"
    return (max(5, min(85, r5(phys))), max(5, min(90, r5(anom))), max(0, min(5, belt)), mods, mp, max(2, kg), note)

SCORE = {}   # tier -> (min, max) of phys+anom across the catalog, filled after the catalog is built

def price(tier, phys, anom):
    lo, hi = PRICE_BAND[tier]
    mn, mx = SCORE[tier]
    p = lo + (phys + anom - mn) / max(1, mx - mn) * (hi - lo)
    return max(lo, int(round(p * 0.95 / 50) * 50)), min(hi, int(round(p * 1.05 / 50) * 50))

def key(n): return re.sub(r"[^a-z0-9]+", "_", n.lower()).strip("_")

rows = []   # (faction, tier, arch, name, stats, look, existing)
for fac, tiers in CATALOG.items():
    band, lens = FACTION[fac][0], FACTION[fac][1]
    for tier, entries in tiers.items():
        assert len(entries) >= 3, (fac, tier)
        for e in entries:
            if isinstance(e, str):
                st = FILE02[e]; look = dict(av.LOOK[e]); arch = None; existing = True; name = e
            else:
                name, arch, head, body, S, H, extra = e
                look = dict(head=head, body=body, S=S, H=H, **extra)
                if lens and "E" not in look: look["E"] = lens
                st = new_stats(fac, tier, arch, head, body, look); existing = False
            rows.append((fac, tier, arch, name, st, look, existing))

# archetype for the originals = whichever of Light/Standard/Heavy the new armors left open in that
# tier (two originals in one tier: higher Phys-DR takes the heavier slot)
for fac in CATALOG:
    for t in range(1, 6):
        idx = [i for i, r in enumerate(rows) if r[0] == fac and r[1] == t]
        free = [a for a in "LSH" if a not in {rows[i][2] for i in idx}]
        orig = sorted([i for i in idx if rows[i][2] is None], key=lambda i: rows[i][4][0])
        assert len(free) == len(orig), (fac, t, free)
        for i, a in zip(orig, free):
            r = rows[i]; rows[i] = (r[0], r[1], a) + r[3:]

for t in range(1, 6):
    sc = [r[4][0] + r[4][1] for r in rows if r[1] == t]; SCORE[t] = (min(sc), max(sc))

names = [r[3] for r in rows]
assert len(names) == len(set(names)), [n for n in names if names.count(n) > 1]
print("armors:", len(rows), "new:", sum(not r[6] for r in rows))

# ---- render sprites ----
imgs = {}
for fac, tier, arch, name, st, look, ex in rows:
    img = av.build(name, st[1] / 100, st[2], L=look)
    img.save(os.path.join(SPR, f"armor_{key(name)}.png"))
    imgs[name] = img

# ---- sheet: one row per faction, 5 tier groups of 3 ----
S8, CELL, GAP, TG = 5, 80, 6, 22
LBL = 150
W = LBL + 5 * (3 * (CELL + GAP) + TG) + 10
H = 70 + len(CATALOG) * (CELL + 22) + 10
sheet = Image.new("RGBA", (W, H), av.hexc("#1b1d1f"))
d = ImageDraw.Draw(sheet)
fb = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 15)
fs = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 11)
d.text((16, 14), "Faction armor catalog - 12 factions x 5 tiers x 3 (Light / Standard / Heavy)", font=fb, fill=av.hexc("#e6e6e6"))
for t in range(5):
    d.text((LBL + t * (3 * (CELL + GAP) + TG), 44), f"Tier {t+1}", font=fb, fill=av.hexc("#b8bcc0"))
for r, fac in enumerate(CATALOG):
    y0 = 70 + r * (CELL + 22)
    band = FACTION[fac][0]
    d.rectangle([16, y0 + 22, 21, y0 + 56], fill=av.hexc(band))
    d.text((28, y0 + 30), fac, font=fb, fill=av.hexc("#e6e6e6"))
    for t in range(1, 6):
        tri = sorted([x for x in rows if x[0] == fac and x[1] == t], key=lambda x: "LSH".index(x[2]))
        for c, x in enumerate(tri):
            x0 = LBL + (t - 1) * (3 * (CELL + GAP) + TG) + c * (CELL + GAP)
            d.rectangle([x0, y0, x0 + CELL - 1, y0 + CELL - 1], fill=av.hexc("#222527"))
            sheet.alpha_composite(imgs[x[3]].resize((CELL, CELL), Image.NEAREST), (x0, y0))
            d.text((x0 + 2, y0 + CELL + 2), x[2] + ("*" if x[6] else ""), font=fs, fill=av.hexc("#8a8e92"))
sheet.convert("RGB").save(os.path.join(OUT, "faction_armor_sheet.png"))

# ---- distinctiveness inside each faction+tier triple ----
def feat(img):
    def mean(rr):
        px = [img.getpixel((x, y))[:3] for x in range(16) for y in rr if img.getpixel((x, y))[3] and sum(img.getpixel((x, y))[:3]) > 90]
        return tuple(sum(p[i] for p in px) / len(px) for i in range(3)) if px else (0, 0, 0)
    return mean(range(0, 7)), mean(range(7, 16))
worst = []
for fac in CATALOG:
    for t in range(1, 6):
        tri = [x for x in rows if x[0] == fac and x[1] == t]
        for a, b in itertools.combinations(tri, 2):
            fa, fb_ = feat(imgs[a[3]]), feat(imgs[b[3]])
            dd = 0.5 * math.dist(fa[0], fb_[0]) + math.dist(fa[1], fb_[1])
            same_shape = a[5]["head"] == b[5]["head"] and a[5]["body"] == b[5]["body"]
            worst.append((round(dd, 1), same_shape, a[3], b[3]))
worst.sort()
print("closest pairs inside a faction tier (rgb dist, same silhouette):")
for w in worst[:6]: print("  ", w)

# ---- design doc ----
WEAPON_CATS = ["Melee", "Sidearm", "SMG", "Shotgun", "Assault Rifle", "Sniper Rifle", "Heavy Weapon"]
WEAPONS = {   # weights, every faction uses every category
    "Raiders":     [10, 25, 15, 25, 18, 4, 3],
    "Outcasts":    [15, 25, 12, 20, 18, 6, 4],
    "Wanderers":   [6, 15, 12, 15, 30, 15, 7],
    "Prospectors": [5, 15, 20, 15, 25, 15, 5],
    "Garrison":    [4, 10, 12, 10, 38, 12, 14],
    "Sentinels":   [4, 10, 10, 12, 34, 12, 18],
    "Liberators":  [5, 12, 15, 12, 32, 16, 8],
    "Contractors": [4, 12, 22, 12, 30, 14, 6],
    "Obelisk":     [6, 8, 10, 10, 34, 20, 12],
    "Cartel":      [8, 22, 25, 20, 17, 4, 4],
    "Surveyors":   [6, 30, 25, 12, 17, 8, 2],
    "IZC":         [4, 12, 20, 10, 32, 14, 8],
}
for k, v in WEAPONS.items(): assert sum(v) == 100 and min(v) > 0, k

ARCH = {"L": "Light", "S": "Standard", "H": "Heavy"}
md = ["# Faction Armor & Weapon Loadouts", "",
      "Companion to file 02 (armor) and file 04 (factions). Every human faction has its own armor line:",
      "**3 armors per tier, Tier 1-5** (Light / Standard / Heavy). Armors marked * are the originals from",
      "file 02 with unchanged stats; the rest are new and follow file 02's ranges. Each DR is still capped",
      "at 75% in play. Prices follow file 07's tier bands; sell-back per file 08.", "",
      "## How an NPC is equipped", "",
      "1. **Armor:** take the NPC's faction line at the tier matching its rank (Rookie/Experienced T1-2,",
      "   Professional/Veteran T2-3, Expert/Master T3-4, Legend T4-5), never more than 1 above the zone's",
      "   tier. Roll 1d3 for Light / Standard / Heavy (Leaders always Heavy).",
      "2. **Weapon:** roll the weapon category on the faction's d100 table below, then take a",
      "   tier-appropriate weapon from that category (file 07 pools). A category with no pool at that tier",
      "   rerolls. Every faction fields every weapon type - the table only sets how common each is.",
      "3. **Combat Role** follows the weapon (file 04): Anchor / Rusher / Flanker, plus one Leader per squad.",
      "4. **Look:** the NPC is drawn in its armor's sprite with a 1-pixel armband in its faction color.", "",
      "## Weapon category weights (d100)", "",
      "|Faction|" + "|".join(WEAPON_CATS) + "|", "|-|" + "|".join("-" for _ in WEAPON_CATS) + "|"]
for fac, w in WEAPONS.items():
    md.append(f"|{fac}|" + "|".join(str(x) for x in w) + "|")
md += ["", "## Armor by faction", ""]
for fac in CATALOG:
    md += [f"### {fac}", "", "|Tier|Type|Armor|Phys-DR|Anom-DR|Belt|Mods|MP|Weight|Price (Ru)|Notes|", "|-|-|-|-|-|-|-|-|-|-|-|"]
    for t in range(1, 6):
        for x in sorted([x for x in rows if x[0] == fac and x[1] == t], key=lambda x: "LSH".index(x[2])):
            p, a, b, m, mp, kg, note = x[4]
            lo, hi = price(t, p, a)
            md.append(f"|{t}|{ARCH[x[2]]}|{x[3]}{' *' if x[6] else ''}|{p}%|{a}%|{b}|{m}|{mp}|{kg}kg|{lo if lo == hi else f'{lo}-{hi}'}|{note or '-'}|")
    md.append("")
open(os.path.join(OUT, "02b_Faction_Armor_and_Weapons.md"), "w").write("\n".join(md))
print("doc ok")
