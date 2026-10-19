// Real-time fork, RT batch 6 (Yan approved, 2026-10-19): head protection, read off what each armor's sprite wears on its head, so no
// new art. The head type of every armor comes from the art scripts (make_armor_avatars.py LOOK + make_faction_armors.py; Heads below
// is generated from them - the harness checks every armor has one). By head type and the armor's tier:
//   bare / bandana / balaclava / hood / hood + scarf: nothing.  gas-mask hood / sealed hazmat hood: 10%, no deflect.
//   steel helmet: 25% (tiers 1-2), 30% / 10% (3), 35% / 12% (4), 40% / 15% (5).  visor helmet: 45% / 15%.
//   full-face helmet: 55% / 20%.  exoskeleton helmet: 55% / 20% (tier 4), 60% / 25% (5).
// A head hit uses this instead of the body's Phys-DR; a helmet can also turn the round away entirely (no damage, a 2 s
// concussion). AP rounds cut head protection like body armor (to 40%) and halve the deflect chance - tiers 4-5 need AP (Yan).
// Yan: "we can have armor that have more deflect chance or weapons that can't be deflected": DeflectBonus per armor, NoDeflect per
// weapon (anti-materiel rounds go straight through).
using System.Collections.Generic;
using GameData;

namespace GameCore
{
    public static class HeadGear
    {
        /// <summary>Armors whose helmets turn rounds more (or less) often than their type: name -> extra deflect chance.</summary>
        static readonly Dictionary<string, float> DeflectBonus = new Dictionary<string, float> { ["Obelisk Heavy"] = 0.03f, ["Sentinel Proto-Armor"] = 0.05f };

        /// <summary>Rounds no helmet turns (anti-materiel). By weapon name.</summary>
        static readonly HashSet<string> NoDeflect = new HashSet<string> { "VK-127 Grom", "Halden AM-50 Sledge", "G-7 Rail Prototype",
            "RK-12 Kuvalda", "Black Harrier", "Steppe Eagle" };   // + the heavy handguns (Yan)

        public static ArmorDef ArmorOf(ICombatant u) => u is Player p ? p.EquippedArmor : u is HumanEnemy h ? h.EquippedArmor : null;

        public static string HeadOf(ArmorDef a) => a != null && Heads.TryGetValue(a.Name, out var h) ? h : null;

        /// <summary>(head protection, deflect chance) for an armor.</summary>
        public static (float dr, float deflect) Of(ArmorDef a)
        {
            int t = CombatRules.ArmorTier(a);
            (float, float) v = HeadOf(a) switch
            {
                "kokonmask" or "sealed" => (0.10f, 0f),
                "helmet" => t <= 2 ? (0.25f, 0.08f) : t == 3 ? (0.30f, 0.10f) : t == 4 ? (0.35f, 0.12f) : (0.40f, 0.15f),
                "visorhelm" => (0.45f, 0.15f),
                "sphere" => (0.55f, 0.20f),
                "exo" => t >= 5 ? (0.60f, 0.25f) : (0.55f, 0.20f),
                _ => (0f, 0f),
            };
            if (a != null && DeflectBonus.TryGetValue(a.Name, out var b)) v.Item2 += b;
            return v;
        }

        public static bool CantDeflect(Weapon w) => w?.Db != null && NoDeflect.Contains(w.Db.Name);

        /// <summary>The chance this round glances off the target's helmet.</summary>
        public static float DeflectChance(ICombatant target, Weapon w)
        {
            if (CantDeflect(w)) return 0f;
            float d = Of(ArmorOf(target)).deflect;
            return w != null && w.CurrentAmmoType == "ArmorPiercing" ? d * 0.5f : d;
        }

        /// <summary>Every armor's drawn head (generated from the art scripts).</summary>
        public static readonly Dictionary<string, string> Heads = new Dictionary<string, string>
        {
            ["Basalt-5"] = "visorhelm",
            ["Cartel Black Exo"] = "exo",
            ["Cartel Bodyguard"] = "helmet",
            ["Cartel Boss Hazsuit"] = "sealed",
            ["Cartel Bruiser Plates"] = "bandit",
            ["Cartel Courier Suit"] = "hood",
            ["Cartel Enforcer Jacket"] = "balaclava",
            ["Cartel Gold-Trim Heavy"] = "helmet",
            ["Cartel Kingpin Armor"] = "sphere",
            ["Cartel Leather Coat"] = "bare",
            ["Cartel Lieutenant Coat"] = "bare",
            ["Cartel Lined Coat"] = "bandit",
            ["Cartel Rad-Courier"] = "sealed",
            ["Cartel Rad-Suit"] = "sealed",
            ["Cartel Runner Tracksuit"] = "bare",
            ["Cartel Smuggler Vest"] = "hoodface",
            ["Contractor ACE Assault"] = "helmet",
            ["Contractor ACE Base"] = "hood",
            ["Contractor ACE Commander"] = "sphere",
            ["Contractor ACE Heavy"] = "helmet",
            ["Contractor ACE Operator"] = "visorhelm",
            ["Contractor ACE Scientific"] = "sealed",
            ["Contractor Balaclava Jacket"] = "balaclava",
            ["Contractor Fleece"] = "bare",
            ["Contractor Hardsuit Exo"] = "exo",
            ["Contractor Light"] = "hood",
            ["Contractor Light Carrier"] = "hood",
            ["Contractor Plate Carrier"] = "helmet",
            ["Contractor Recon Suit"] = "hood",
            ["Contractor Urban Fatigues"] = "hood",
            ["Contractor Wraith Suit"] = "sealed",
            ["Daybreak Suit"] = "hood",
            ["Field Technician Overalls"] = "hood",
            ["Garrison 6B2 Flak Vest"] = "helmet",
            ["Garrison 6B23 Assault"] = "visorhelm",
            ["Garrison 6B5 Armor"] = "helmet",
            ["Garrison Altyn Assault"] = "visorhelm",
            ["Garrison Conscript Uniform"] = "bare",
            ["Garrison Field Uniform"] = "helmet",
            ["Garrison Gorka Coat"] = "hoodface",
            ["Garrison OZK Chem Suit"] = "hood",
            ["Garrison Rad-Assault Suit"] = "sealed",
            ["Garrison Ratnik-Z"] = "helmet",
            ["Garrison Sapper Suit"] = "sphere",
            ["Garrison Spetsnaz Gorka"] = "balaclava",
            ["Garrison Strelets Exo"] = "exo",
            ["Granit-9 Assault Suit"] = "sphere",
            ["HZ-100 Prototype"] = "sealed",
            ["HZ-90 Trainee Suit"] = "sealed",
            ["HZ-95 Sampler"] = "sealed",
            ["HZ-99 Survey Suit"] = "sealed",
            ["HZ-99K Coldsuit"] = "sealed",
            ["HZ-99M Survey Suit"] = "sealed",
            ["Heavy Trenchcoat"] = "hoodface",
            ["IZC Breacher"] = "sphere",
            ["IZC Commander Armor"] = "helmet",
            ["IZC Containment Exo"] = "exo",
            ["IZC Containment Suit"] = "sealed",
            ["IZC Decon Suit"] = "sealed",
            ["IZC Field Uniform"] = "helmet",
            ["IZC Flak Vest"] = "helmet",
            ["IZC Hazard Specialist"] = "kokonmask",
            ["IZC Liaison Jacket"] = "bare",
            ["IZC Observer Coat"] = "hood",
            ["IZC Operative"] = "helmet",
            ["IZC Peacekeeper Armor"] = "helmet",
            ["IZC Recon Specialist"] = "hood",
            ["IZC Scout"] = "helmet",
            ["IZC Trooper"] = "visorhelm",
            ["Kokon Suit"] = "kokonmask",
            ["Lab Coat"] = "bare",
            ["Leather Jacket"] = "balaclava",
            ["Liberator Armored Hoodie"] = "hood",
            ["Liberator Bandana Jacket"] = "bandit",
            ["Liberator Breeze Suit"] = "hood",
            ["Liberator Exo-Lite"] = "helmet",
            ["Liberator Exoskeleton"] = "exo",
            ["Liberator Field Suit"] = "sealed",
            ["Liberator Guard Suit"] = "helmet",
            ["Liberator Guard Suit II"] = "visorhelm",
            ["Liberator Hoodie"] = "hoodface",
            ["Liberator Skirmisher"] = "hood",
            ["Liberator Sky-Kokon"] = "kokonmask",
            ["Liberator Tire Vest"] = "hood",
            ["Liberator Vanguard Plate"] = "helmet",
            ["Liberator Windbreaker"] = "hood",
            ["Liberator Windbreaker II"] = "hood",
            ["Marsh Assault Suit"] = "helmet",
            ["Marsh Bulwark Suit"] = "helmet",
            ["Marsh Exo"] = "exo",
            ["Marsh Pathfinder Suit"] = "helmet",
            ["Marsh Recon Suit"] = "hood",
            ["Marsh Scout Suit"] = "hood",
            ["NBC Suit"] = "hood",
            ["Obelisk Acolyte Wrap"] = "hood",
            ["Obelisk Crusader"] = "helmet",
            ["Obelisk Exoskeleton"] = "exo",
            ["Obelisk Guard Plates"] = "helmet",
            ["Obelisk Heavy"] = "helmet",
            ["Obelisk Monument Plate"] = "sphere",
            ["Obelisk Novice Robe"] = "hoodface",
            ["Obelisk Patrol"] = "hood",
            ["Obelisk Pilgrim Coat"] = "hood",
            ["Obelisk Prophet Suit"] = "sealed",
            ["Obelisk Saint Suit"] = "kokonmask",
            ["Obelisk Scientific"] = "sealed",
            ["Obelisk Seeker Suit"] = "hood",
            ["Obelisk Templar"] = "visorhelm",
            ["Obelisk Zealot Vest"] = "hood",
            ["Outcast Car-Door Armor"] = "hoodface",
            ["Outcast Gas-Mask Coat"] = "hood",
            ["Outcast Hermit's Kokon"] = "kokonmask",
            ["Outcast Junk Exo"] = "exo",
            ["Outcast Patched Overalls"] = "hoodface",
            ["Outcast Rag Poncho"] = "hoodface",
            ["Outcast Riveted Plate"] = "hood",
            ["Outcast Salvaged Hazmat"] = "sealed",
            ["Outcast Scavenger Rig"] = "balaclava",
            ["Outcast Scrap Titan"] = "exo",
            ["Outcast Sheet-Metal Suit"] = "helmet",
            ["Outcast Tarp Rad-Suit"] = "sealed",
            ["Prospector Anomaly Suit"] = "sealed",
            ["Prospector Artifact Diver"] = "sealed",
            ["Prospector Deep-Zone Suit"] = "kokonmask",
            ["Prospector Rad-Cape"] = "sealed",
            ["Prospector Rubber Coat"] = "hoodface",
            ["Prospector Seeker Suit"] = "hood",
            ["Prospector Windbreaker"] = "hood",
            ["Proto-Exoskeleton"] = "exo",
            ["Radiant Kokon"] = "kokonmask",
            ["Raider Armored Warlord"] = "helmet",
            ["Raider Boss Coat"] = "bare",
            ["Raider Duster"] = "bandit",
            ["Raider Jacket"] = "hoodface",
            ["Raider Juggernaut"] = "helmet",
            ["Raider Looted Exo"] = "exo",
            ["Raider Night Coat"] = "hoodface",
            ["Raider Patchwork Vest"] = "hoodface",
            ["Raider Ringleader Jacket"] = "bare",
            ["Raider Scrap Plates"] = "balaclava",
            ["Raider Stitched Coat"] = "bandit",
            ["Raider Tire-Plate Armor"] = "bandit",
            ["Raider Tracksuit"] = "bandit",
            ["Raider Veteran Coat"] = "hoodface",
            ["Raider Warlord Greatcoat"] = "bare",
            ["Rookie Field Suit"] = "hood",
            ["Scrap-Plated Vest"] = "hoodface",
            ["Sentinel Armored Suit"] = "helmet",
            ["Sentinel Armored Suit Mk2"] = "helmet",
            ["Sentinel Bastion Suit"] = "visorhelm",
            ["Sentinel Bulwark Exo"] = "exo",
            ["Sentinel Chem-Guard"] = "sealed",
            ["Sentinel Citadel Suit"] = "sphere",
            ["Sentinel Guardsman Coat"] = "hood",
            ["Sentinel Patrol Suit"] = "hood",
            ["Sentinel Proto-Armor"] = "exo",
            ["Sentinel Recruit Jacket"] = "bare",
            ["Sentinel Riot Plates"] = "helmet",
            ["Sentinel Service Suit"] = "hood",
            ["Sentinel Training Vest"] = "helmet",
            ["Sentinel Vanguard"] = "helmet",
            ["Sentinel Watch Coat"] = "hood",
            ["Surveyor Containment Armor"] = "sphere",
            ["Surveyor Escort Armor"] = "helmet",
            ["Surveyor Field Lab Exo"] = "exo",
            ["Surveyor Guard Plates"] = "visorhelm",
            ["Surveyor Guard Vest"] = "helmet",
            ["Surveyor Hazard Guard"] = "visorhelm",
            ["Wanderer Assault Rig"] = "helmet",
            ["Wanderer Canvas Jacket"] = "hood",
            ["Wanderer Exoskeleton"] = "exo",
            ["Wanderer Field Armor"] = "helmet",
            ["Wanderer Ghillie Coat"] = "hood",
            ["Wanderer Gorka-M"] = "hood",
            ["Wanderer Hardened Kokon"] = "kokonmask",
            ["Wanderer Padded Coat"] = "hoodface",
            ["Wanderer Plate Carrier"] = "helmet",
            ["Wanderer Trail Suit"] = "hood",
            ["Wanderer Trailblazer"] = "helmet",
            ["Wanderer Veteran Kokon"] = "kokonmask"
        };
    }
}
