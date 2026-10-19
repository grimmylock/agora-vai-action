// Ported from database.js: WEAPON_DB
// Original JS objects were irregular per category (a shotgun has cylinderCapacity, a rifle
// has magCapacity, only some weapons have ignorePhysDRPercent, etc). Rather than forcing
// separate classes per category (which would fight the data's natural variance), all optional
// fields live on one WeaponDef with nullable types, mirroring what "irregular JS object" means
// in a statically typed language. Category keys and per-weapon keys match the JS exactly.
using System.Collections.Generic;

namespace GameData
{
    public class WeaponDef
    {
        public string Name;
        public int BaseDamage;
        public string Range;           // e.g. "Adjacent", "15cm", "80cm"
        public float? WeightKg;        // only set for melee weapons in the source data
        public int Tier;
        public string AmmoType;        // null for melee
        public int? MagCapacity;
        public int? CylinderCapacity;
        public float? IgnorePhysDRPercent;
        public bool HighRecoil;
        public bool BurstCapable;
        public int? BurstFireApCost;
        public string ActionType;      // e.g. "Break" for break-action shotguns
        public int? ReloadApCost;
        public bool MagFed;
        public bool InnateSuppressor;
        public string[] PreAttached;
        public int? MaxMpWhileHeld;
        public bool ImmuneToBurstJams;
    }

    public static class WeaponDatabase
    {
        // Keyed exactly like WEAPON_DB in the JS: Categories["sidearm"]["PSh18Pchela"]
        public static readonly Dictionary<string, Dictionary<string, WeaponDef>> Categories =
            new Dictionary<string, Dictionary<string, WeaponDef>>
        {
            ["melee"] = new Dictionary<string, WeaponDef>
            {
                ["HuntingKnife"] = new WeaponDef { Name = "Hunting Knife", BaseDamage = 20, Range = "Adjacent", WeightKg = 0.5f, Tier = 1 },
                ["SpetsnazSwitchblade"] = new WeaponDef { Name = "Spetsnaz Switchblade", BaseDamage = 25, Range = "Adjacent", WeightKg = 0.5f, Tier = 2 },
                ["SteppeMachete"] = new WeaponDef { Name = "Steppe Machete", BaseDamage = 30, Range = "Adjacent", WeightKg = 0.8f, Tier = 3, IgnorePhysDRPercent = 0.1f },
                ["SerratedTrenchKnife"] = new WeaponDef { Name = "Serrated Trench Knife", BaseDamage = 36, Range = "Adjacent", WeightKg = 0.6f, Tier = 4 },
                ["ObeliskRitualBlade"] = new WeaponDef { Name = "Obelisk Ritual Blade", BaseDamage = 45, Range = "Adjacent", WeightKg = 0.7f, Tier = 5, IgnorePhysDRPercent = 0.2f },
            },
            ["sidearm"] = new Dictionary<string, WeaponDef>
            {
                ["PSh18Pchela"] = new WeaponDef { Name = "PSh-18 Pchela", BaseDamage = 12, AmmoType = "9x18mm", MagCapacity = 8, Range = "20cm", WeightKg = 0.7f, Tier = 1 },
                ["WaldenP9"] = new WeaponDef { Name = "Walden P9", BaseDamage = 15, AmmoType = "9x19mm", MagCapacity = 16, Range = "25cm", WeightKg = 0.8f, Tier = 2 },
                ["Halden45Service"] = new WeaponDef { Name = "Halden .45 Service", BaseDamage = 16, AmmoType = ".45 ACP", MagCapacity = 7, Range = "20cm", WeightKg = 1.1f, Tier = 2 },
                ["HaldenServiceRevolver"] = new WeaponDef { Name = "Halden Service Revolver", BaseDamage = 18, AmmoType = ".45 ACP", CylinderCapacity = 6, Range = "25cm", WeightKg = 1.1f, Tier = 1 },
                ["SR21Gadyuka"] = new WeaponDef { Name = "SR-21 Gadyuka", BaseDamage = 15, AmmoType = "9x21mm", MagCapacity = 18, Range = "25cm", WeightKg = 1f, Tier = 3, IgnorePhysDRPercent = 0.1f },
                ["BlackHarrier"] = new WeaponDef { Name = "Black Harrier", BaseDamage = 22, AmmoType = ".357 Mag", MagCapacity = 9, Range = "25cm", WeightKg = 1.2f, Tier = 3, HighRecoil = true },
                ["Corvan57"] = new WeaponDef { Name = "Corvan 5.7", BaseDamage = 17, AmmoType = "5.7x28mm", MagCapacity = 20, Range = "30cm", WeightKg = 0.7f, Tier = 4, IgnorePhysDRPercent = 0.1f },
                ["RinaldiR6"] = new WeaponDef { Name = "Rinaldi R6", BaseDamage = 20, AmmoType = ".357 Mag", CylinderCapacity = 6, Range = "30cm", WeightKg = 0.9f, Tier = 4, BurstCapable = true, BurstFireApCost = 3 },
                ["SteppeEagle"] = new WeaponDef { Name = "Steppe Eagle", BaseDamage = 25, AmmoType = ".44 Mag", MagCapacity = 8, Range = "25cm", WeightKg = 2f, Tier = 4, HighRecoil = true },
                ["RK12Kuvalda"] = new WeaponDef { Name = "RK-12 Kuvalda", BaseDamage = 34, AmmoType = "12.7x55mm", CylinderCapacity = 5, Range = "30cm", WeightKg = 2.2f, Tier = 5, IgnorePhysDRPercent = 0.2f },
            },
            ["smg"] = new Dictionary<string, WeaponDef>
            {
                ["VosaVz62"] = new WeaponDef { Name = "Vosa vz. 62", BaseDamage = 11, AmmoType = ".32 ACP", MagCapacity = 20, Range = "20cm", WeightKg = 1.3f, Tier = 1 },
                ["PPZZubr"] = new WeaponDef { Name = "PPZ Zubr", BaseDamage = 12, AmmoType = "9x18mm", MagCapacity = 64, Range = "25cm", WeightKg = 2.5f, Tier = 2 },
                ["CorvanWasp9"] = new WeaponDef { Name = "Corvan Wasp-9", BaseDamage = 14, AmmoType = "9x19mm", MagCapacity = 30, Range = "30cm", WeightKg = 2.5f, Tier = 3 },
                ["CorvanHornet45"] = new WeaponDef { Name = "Corvan Hornet .45", BaseDamage = 16, AmmoType = ".45 ACP", MagCapacity = 25, Range = "25cm", WeightKg = 2.3f, Tier = 3 },
                ["CorvanGnatPDW"] = new WeaponDef { Name = "Corvan Gnat PDW", BaseDamage = 13, AmmoType = "4.6x30mm", MagCapacity = 40, Range = "30cm", WeightKg = 1.9f, Tier = 4, IgnorePhysDRPercent = 0.1f },
            },
            ["shotgun"] = new Dictionary<string, WeaponDef>
            {
                ["Obrez66"] = new WeaponDef { Name = "Obrez-66", BaseDamage = 30, AmmoType = "12g", MagCapacity = 2, Range = "15cm", WeightKg = 2f, Tier = 1, ActionType = "Break" },
                ["HaldenLeverShotgun"] = new WeaponDef { Name = "Halden Lever Shotgun", BaseDamage = 31, AmmoType = "12g", MagCapacity = 5, Range = "15cm", WeightKg = 3.6f, Tier = 2 },
                ["Hound13"] = new WeaponDef { Name = "Hound 13", BaseDamage = 30, AmmoType = "12g", MagCapacity = 6, Range = "15cm", WeightKg = 3.2f, Tier = 2 },
                ["RangerP8"] = new WeaponDef { Name = "Ranger P8", BaseDamage = 30, AmmoType = "12g", MagCapacity = 7, Range = "15cm", WeightKg = 3.6f, Tier = 2 },
                ["BayouPump"] = new WeaponDef { Name = "Bayou Pump", BaseDamage = 32, AmmoType = "12g", MagCapacity = 8, Range = "15cm", WeightKg = 3.4f, Tier = 3 },
                ["Sever34"] = new WeaponDef { Name = "Sever-34 O/U", BaseDamage = 34, AmmoType = "12g", MagCapacity = 2, Range = "20cm", WeightKg = 3.2f, Tier = 3, ActionType = "Break" },
                ["Tambour12"] = new WeaponDef { Name = "Tambour-12", BaseDamage = 32, AmmoType = "12g", CylinderCapacity = 12, Range = "15cm", WeightKg = 4.2f, Tier = 4, ReloadApCost = 2 },
                ["SP14Warden"] = new WeaponDef { Name = "SP-14 Warden", BaseDamage = 33, AmmoType = "12g", MagCapacity = 8, Range = "15cm", WeightKg = 4.4f, Tier = 4, BurstCapable = true },
                ["Sayan12"] = new WeaponDef { Name = "Sayan-12", BaseDamage = 32, AmmoType = "12g", MagCapacity = 10, Range = "15cm", WeightKg = 3.6f, Tier = 4, MagFed = true },
                ["Kaban12"] = new WeaponDef { Name = "Kaban-12", BaseDamage = 33, AmmoType = "12g", MagCapacity = 10, Range = "20cm", WeightKg = 4.5f, Tier = 4, MagFed = true },
            },
            ["assault"] = new Dictionary<string, WeaponDef>
            {
                ["VKS74U"] = new WeaponDef { Name = "VKS-74U", BaseDamage = 18, AmmoType = "5.45x39mm", MagCapacity = 30, Range = "30cm", WeightKg = 2.7f, Tier = 1 },
                ["Smerch3"] = new WeaponDef { Name = "Smerch-3", BaseDamage = 26, AmmoType = "9x39mm", MagCapacity = 30, Range = "25cm", WeightKg = 2f, Tier = 2, IgnorePhysDRPercent = 0.1f },
                ["VK74"] = new WeaponDef { Name = "VK-74", BaseDamage = 22, AmmoType = "5.45x39mm", MagCapacity = 30, Range = "40cm", WeightKg = 3.3f, Tier = 2 },
                ["VKM"] = new WeaponDef { Name = "VKM", BaseDamage = 25, AmmoType = "7.62x39mm", MagCapacity = 30, Range = "40cm", WeightKg = 3.5f, Tier = 2 },
                ["TS9Tishina"] = new WeaponDef { Name = "TS-9 Tishina", BaseDamage = 28, AmmoType = "9x39mm", MagCapacity = 20, Range = "35cm", WeightKg = 2.6f, Tier = 3, IgnorePhysDRPercent = 0.1f, InnateSuppressor = true },
                ["TR30"] = new WeaponDef { Name = "TR-30", BaseDamage = 24, AmmoType = "5.56x45mm", MagCapacity = 30, Range = "45cm", WeightKg = 3.4f, Tier = 3 },
                ["HaldenHR36K"] = new WeaponDef { Name = "Halden HR36K", BaseDamage = 24, AmmoType = "5.56x45mm", MagCapacity = 30, Range = "45cm", WeightKg = 3f, Tier = 3 },
                ["VK103"] = new WeaponDef { Name = "VK-103", BaseDamage = 26, AmmoType = "7.62x39mm", MagCapacity = 30, Range = "45cm", WeightKg = 3.6f, Tier = 3 },
                ["BR86Bulldog"] = new WeaponDef { Name = "BR-86 Bulldog", BaseDamage = 27, AmmoType = "5.56x45mm", MagCapacity = 30, Range = "45cm", WeightKg = 4.4f, Tier = 4 },
                ["VK74Obelisk"] = new WeaponDef { Name = "VK-74 Obelisk", BaseDamage = 25, AmmoType = "5.45x39mm", MagCapacity = 30, Range = "45cm", WeightKg = 3.6f, Tier = 4, PreAttached = new[] { "Laser", "Red Dot" } },
                ["NA94Yenisei"] = new WeaponDef { Name = "NA-94 Yenisei", BaseDamage = 22, AmmoType = "5.45x39mm", MagCapacity = 30, Range = "45cm", WeightKg = 3.9f, Tier = 4, BurstFireApCost = 2 },
                ["CorvanBullpup"] = new WeaponDef { Name = "Corvan Bullpup", BaseDamage = 33, AmmoType = "5.56x45mm", MagCapacity = 30, Range = "50cm", WeightKg = 3.6f, Tier = 5 },
                ["CorvanBattleRifle"] = new WeaponDef { Name = "Corvan Battle Rifle", BaseDamage = 30, AmmoType = "7.62x51mm", MagCapacity = 20, Range = "50cm", WeightKg = 4.4f, Tier = 4 },
            },
            ["sniper"] = new Dictionary<string, WeaponDef>
            {
                ["SKNKarabin"] = new WeaponDef { Name = "SKN Karabin", BaseDamage = 30, AmmoType = "7.62x39mm", MagCapacity = 10, Range = "55cm", WeightKg = 3.9f, Tier = 2 },
                ["FrontierLeverRifle"] = new WeaponDef { Name = "Frontier Lever Rifle", BaseDamage = 32, AmmoType = ".44 Mag", MagCapacity = 8, Range = "45cm", WeightKg = 3.2f, Tier = 3 },
                ["VSNShepot"] = new WeaponDef { Name = "VSN Shepot", BaseDamage = 35, AmmoType = "9x39mm", MagCapacity = 10, Range = "45cm", WeightKg = 2.6f, Tier = 3, InnateSuppressor = true },
                ["M91Trekhlineyka"] = new WeaponDef { Name = "M91 Trekhlineyka", BaseDamage = 40, AmmoType = "7.62x54mmR", MagCapacity = 5, Range = "65cm", WeightKg = 4f, Tier = 3 },
                ["Nightwalker4570"] = new WeaponDef { Name = "Nightwalker .45-70", BaseDamage = 38, AmmoType = ".45-70 Govt", MagCapacity = 6, Range = "45cm", WeightKg = 3.4f, Tier = 4, IgnorePhysDRPercent = 0.1f },
                ["CorvanMarksman"] = new WeaponDef { Name = "Corvan Marksman", BaseDamage = 38, AmmoType = "7.62x51mm", MagCapacity = 20, Range = "60cm", WeightKg = 4.9f, Tier = 4 },
                ["SV2Strela"] = new WeaponDef { Name = "SV-2 Strela", BaseDamage = 45, AmmoType = "7.62x54mmR", MagCapacity = 10, Range = "70cm", WeightKg = 4.5f, Tier = 4 },
                ["Sever98"] = new WeaponDef { Name = "Sever-98", BaseDamage = 45, AmmoType = "7.62x54mmR", MagCapacity = 10, Range = "70cm", WeightKg = 5.8f, Tier = 4 },
                ["VK127Grom"] = new WeaponDef { Name = "VK-127 Grom", BaseDamage = 55, AmmoType = "12.7x55mm", MagCapacity = 5, Range = "70cm", WeightKg = 6.5f, Tier = 5, IgnorePhysDRPercent = 0.3f, InnateSuppressor = true },
                ["HaldenAM50Sledge"] = new WeaponDef { Name = "Halden AM-50 Sledge", BaseDamage = 60, AmmoType = ".50 BMG", MagCapacity = 10, Range = "80cm", WeightKg = 13f, Tier = 5, IgnorePhysDRPercent = 0.5f },
                ["G7RailPrototype"] = new WeaponDef { Name = "G-7 Rail Prototype", BaseDamage = 80, AmmoType = "2mm Battery", MagCapacity = 10, Range = "80cm", WeightKg = 9f, Tier = 5, IgnorePhysDRPercent = 0.8f },
            },
            ["heavy"] = new Dictionary<string, WeaponDef>
            {
                ["PL62Kobza"] = new WeaponDef { Name = "PL-62 Kobza", BaseDamage = 20, AmmoType = "7.62x39mm", MagCapacity = 75, Range = "40cm", WeightKg = 6.5f, Tier = 2, MaxMpWhileHeld = 2 },   // Batch 79: the Tier 2 light machine gun
                ["CorvanLMG"] = new WeaponDef { Name = "Corvan LMG", BaseDamage = 22, AmmoType = "5.56x45mm", MagCapacity = 200, Range = "40cm", WeightKg = 7.5f, Tier = 3, MaxMpWhileHeld = 2 },
                ["RL44"] = new WeaponDef { Name = "RL-44", BaseDamage = 24, AmmoType = "7.62x39mm", MagCapacity = 100, Range = "40cm", WeightKg = 7.4f, Tier = 3, MaxMpWhileHeld = 2 },
                ["PL74"] = new WeaponDef { Name = "PL-74", BaseDamage = 28, AmmoType = "7.62x54mmR", MagCapacity = 100, Range = "45cm", WeightKg = 8f, Tier = 4, MaxMpWhileHeld = 2 },
                ["PL74PPolovets"] = new WeaponDef { Name = "PL-74P Polovets", BaseDamage = 28, AmmoType = "7.62x54mmR", MagCapacity = 100, Range = "50cm", WeightKg = 8.5f, Tier = 4, MaxMpWhileHeld = 2, ImmuneToBurstJams = true },
                ["PL74PPolovetsZulu"] = new WeaponDef { Name = "PL-74P Polovets Zulu", BaseDamage = 28, AmmoType = "7.62x54mmR", MagCapacity = 100, Range = "50cm", WeightKg = 9f, Tier = 5, PreAttached = new[] { "4x Scope", "Laser" }, MaxMpWhileHeld = 2 },
            },
        };

        /// <summary>Mirrors JS's WEAPON_DB[category][key] lookup pattern. Returns null if not found.</summary>
        public static WeaponDef Get(string category, string key)
        {
            if (Categories.TryGetValue(category, out var group) && group.TryGetValue(key, out var weapon))
                return weapon;
            return null;
        }
    }
}
