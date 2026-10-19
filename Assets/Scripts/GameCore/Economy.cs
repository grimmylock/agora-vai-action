// The economy from files 05, 07 and 08 in one place:
//   * Price bands for every rolled item (file 07 tier pools). An item's "buy price" is the middle
//     of its band; a trader's rolled stock gets a price rolled inside the band.
//   * Sell price = buy price x sell-back % (weapons 40, armor 35, artifacts 45, detectors 30,
//     mods 25, consumables 20; mutant parts at full value) x condition (file 08).
//   * The d100 loot/trader table (file 07): category map, Tier Drift for loot, Tier 5 weapons
//     loot-only, cash/junk/lucky finds. Used by trader stock, NPC extra loot and containers.
//   * Repairs: 10% of the band midpoint per Condition step (file 08 §5).
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public class LootRoll
    {
        public string Type;       // "weapon" | "armor" | "armorMod" | "artifact" | "detector" | "backpack" | "consumable" | "cash" | "junk"
        public string Id, Name, Category, Key;
        public int Price;         // rolled inside the band (cash: the amount of Ru)
        public float WeightKg;

        public InventoryItem ToItem(string condition = null) => new InventoryItem
        {
            Id = Id, Name = Name, WeightKg = WeightKg, Qty = 1, Type = Type, Price = Price, Category = Category, Key = Key, Condition = condition,
        };

        public ShopItemDef ToShopItem() => new ShopItemDef
        {
            Id = Id, Name = Name, Price = Price, WeightKg = WeightKg, Type = Type, Category = Category, Key = Key,
        };
    }

    public static class Economy
    {
        // ------------------------------------------------------------------ file 07 pools
        private class Pool { public string[] Names; public int Min, Max; }
        private static Pool P(int min, int max, params string[] names) => new Pool { Names = names, Min = min, Max = max };

        // [tier][category] -> pool. Categories: melee sidearm smg shotgun assault heavy sniper mod artifact detector backpack consumable
        private static readonly Dictionary<int, Dictionary<string, Pool>> Pools = new Dictionary<int, Dictionary<string, Pool>>
        {
            [1] = new Dictionary<string, Pool>
            {
                ["melee"] = P(60, 100, "Hunting Knife"),
                ["sidearm"] = P(150, 300, "PSh-18 Pchela", "Halden Service Revolver"),
                ["smg"] = P(300, 400, "Vosa vz. 62"),
                ["shotgun"] = P(250, 400, "Obrez-66"),
                ["assault"] = P(450, 500, "VKS-74U"),
                ["mod"] = P(150, 300, "Aramid Weave", "Lead-Lined Fabric", "Asbestos Coating", "Faraday Mesh"),
                ["artifact"] = P(300, 700, "Drift Bubble", "Pulse Pebble", "Clot", "Static Wisp"),
                ["detector"] = P(800, 1200, "Clicker"),
                ["consumable"] = P(180, 300, "Military Medkit", "Military Stim"),
            },
            [2] = new Dictionary<string, Pool>
            {
                ["melee"] = P(100, 180, "Spetsnaz Switchblade"),
                ["sidearm"] = P(400, 700, "Walden P9", "Halden .45 Service"),
                ["smg"] = P(500, 800, "PPZ Zubr"),
                ["shotgun"] = P(500, 900, "Halden Lever Shotgun", "Hound 13", "Ranger P8"),
                ["assault"] = P(900, 1400, "Smerch-3", "VK-74", "VKM"),
                ["sniper"] = P(1000, 1500, "SKN Karabin"),
                ["mod"] = P(400, 800, "Ceramic Plates", "Titanium Grid", "Artifact Pouch"),
                ["artifact"] = P(800, 1800, "Twistknot", "Fiery Rouge", "Scab Stone"),
                ["detector"] = P(2500, 3500, "Sniffer"),
                ["backpack"] = P(350, 450, "Tourist Frame"),
                ["consumable"] = P(400, 700, "Scientific Medkit", "Military Stim"),
            },
            [3] = new Dictionary<string, Pool>
            {
                ["melee"] = P(220, 350, "Steppe Machete"),
                ["sidearm"] = P(1800, 2800, "SR-21 Gadyuka", "Black Harrier"),
                ["smg"] = P(2000, 3200, "Corvan Wasp-9", "Corvan Hornet .45"),
                ["shotgun"] = P(2200, 3500, "Bayou Pump", "Sever-34 O/U"),
                ["assault"] = P(2800, 4000, "TS-9 Tishina", "TR-30", "Halden HR36K", "VK-103"),
                ["heavy"] = P(3500, 4000, "Corvan LMG", "RL-44"),
                ["sniper"] = P(3000, 4000, "Frontier Lever Rifle", "VSN Shepot", "M91 Trekhlineyka"),
                ["mod"] = P(900, 1800, "Treated Mutant Hide", "Psi-Stimulator Earpiece", "Servomotors", "Closed-Breathing"),
                ["artifact"] = P(2000, 4000, "Ballast", "Heartspark", "Quickglass"),
                ["detector"] = P(9000, 11000, "Dowser"),
                ["consumable"] = P(900, 1500, "Scientific Stim"),
            },
            [4] = new Dictionary<string, Pool>
            {
                ["melee"] = P(400, 650, "Serrated Trench Knife"),
                ["sidearm"] = P(4500, 7000, "Corvan 5.7", "Rinaldi R6", "Steppe Eagle"),
                ["smg"] = P(5000, 7500, "Corvan Gnat PDW"),
                ["shotgun"] = P(5000, 8000, "Tambour-12", "SP-14 Warden", "Sayan-12", "Kaban-12"),
                ["assault"] = P(6000, 9500, "BR-86 Bulldog", "VK-74 Obelisk", "NA-94 Yenisei", "Corvan Battle Rifle"),
                ["heavy"] = P(7000, 9500, "PL-74", "PL-74P Polovets"),
                ["sniper"] = P(6500, 9500, "Nightwalker .45-70", "Corvan Marksman", "SV-2 Strela", "Sever-98"),
                ["mod"] = P(2000, 4000, "Auto-Injector", "Advanced Hydraulics"),
                ["artifact"] = P(4000, 7000, "Rosary"),
                ["detector"] = P(14000, 16000, "Seer"),
            },
            [5] = new Dictionary<string, Pool>
            {
                ["melee"] = P(700, 1200, "Obelisk Ritual Blade"),
                ["sidearm"] = P(16000, 22000, "RK-12 Kuvalda"),
                ["assault"] = P(20000, 28000, "Corvan Bullpup"),
                ["sniper"] = P(15000, 24000, "VK-127 Grom", "Halden AM-50 Sledge", "G-7 Rail Prototype"),
                ["heavy"] = P(18000, 24000, "PL-74P Polovets Zulu"),
                ["artifact"] = P(12000, 25000, "Weightless Pearl", "Lantern Seed", "Frost Lattice"),
            },
        };

        // per-armor bands (file 07); faction-issue armor falls back to its tier band
        private static readonly Dictionary<string, (int min, int max)> ArmorBands = new Dictionary<string, (int, int)>
        {
            ["Leather Jacket"] = (200, 280), ["Raider Jacket"] = (280, 350), ["Scrap-Plated Vest"] = (220, 320), ["Rookie Field Suit"] = (320, 420), ["Heavy Trenchcoat"] = (400, 500),
            ["Contractor Light"] = (650, 800), ["Liberator Windbreaker"] = (700, 850), ["Raider Stitched Coat"] = (700, 850), ["Sentinel Service Suit"] = (850, 1000), ["Marsh Recon Suit"] = (900, 1100),
            ["Daybreak Suit"] = (950, 1150), ["NBC Suit"] = (1100, 1300), ["Obelisk Patrol"] = (1300, 1500),
            ["Raider Veteran Coat"] = (1800, 2200), ["Sentinel Armored Suit"] = (1800, 2200), ["IZC Scout"] = (2000, 2400), ["Basalt-5"] = (2200, 2600), ["Contractor ACE Base"] = (2400, 2900),
            ["Liberator Guard Suit"] = (2500, 3000), ["HZ-99 Survey Suit"] = (2800, 3300), ["Obelisk Scientific"] = (2900, 3400), ["Kokon Suit"] = (3400, 4000),
            ["HZ-99M Survey Suit"] = (4500, 5500), ["Raider Boss Coat"] = (4800, 5800), ["Marsh Pathfinder Suit"] = (5200, 6200), ["IZC Operative"] = (5800, 6800), ["Cartel Rad-Suit"] = (6000, 7000),
            ["Contractor ACE Scientific"] = (6200, 7200), ["Sentinel Vanguard"] = (6500, 7500), ["Granit-9 Assault Suit"] = (7500, 8500), ["Proto-Exoskeleton"] = (8000, 9000), ["Obelisk Heavy"] = (8500, 9500),
            ["Radiant Kokon"] = (11000, 14000), ["Liberator Exoskeleton"] = (14000, 17000), ["Wanderer Exoskeleton"] = (17000, 20000), ["Obelisk Exoskeleton"] = (20000, 23000), ["Sentinel Proto-Armor"] = (22000, 25000),
        };
        private static readonly Dictionary<int, (int min, int max)> ArmorTierBand = new Dictionary<int, (int, int)>
        {
            [1] = (200, 500), [2] = (650, 1500), [3] = (1800, 4000), [4] = (4500, 9500), [5] = (11000, 25000),
        };

        private static readonly Dictionary<int, (int min, int max)> CashBand = new Dictionary<int, (int, int)>
        {
            [1] = (50, 200), [2] = (150, 500), [3] = (400, 1200), [4] = (1000, 3000), [5] = (2500, 7000), [6] = (6000, 15000), [7] = (6000, 15000),
        };

        /// <summary>Layer 1 baseline prices (file 05) for consumables, by consumable id.</summary>
        public static readonly Dictionary<string, int> BaselinePrice = new Dictionary<string, int>
        {
            ["basicMedkit"] = 100, ["basicStim"] = 120, ["bandage"] = 40, ["splint"] = 60, ["radBlockPill"] = 60, ["mre"] = 50, ["energyDrink"] = 40,
            ["fragGrenade"] = 250, ["smokeGrenade"] = 150, ["flashbang"] = 200, ["fieldRepairKit"] = 300,
        };

        // ------------------------------------------------------------------ prices

        public static int Mid((int min, int max) b) => (b.min + b.max) / 2;

        /// <summary>Price band of a weapon (file 07 tier pool its name is in; tier band if not).</summary>
        public static (int min, int max) WeaponBand(WeaponDef w)
        {
            if (w == null) return (100, 100);
            string cat = CombatRules.Category(w);
            foreach (var tier in Pools)
                if (tier.Value.TryGetValue(cat, out var p) && p.Names.Contains(w.Name)) return (p.Min, p.Max);
            return Pools.TryGetValue(w.Tier, out var t) && t.TryGetValue(cat, out var tp) ? (tp.Min, tp.Max) : (w.Tier * 300, w.Tier * 500);
        }

        public static (int min, int max) ArmorBand(ArmorDef a, int tier)
        {
            if (a != null && ArmorBands.TryGetValue(a.Name, out var b)) return b;
            return ArmorTierBand.TryGetValue(Mathf.Clamp(tier, 1, 5), out var tb) ? tb : (200, 500);
        }

        private static (int min, int max)? PoolBandByName(string category, string name)
        {
            foreach (var tier in Pools)
                if (tier.Value.TryGetValue(category, out var p) && p.Names.Contains(name)) return (p.Min, p.Max);
            return null;
        }

        /// <summary>The middle-of-band value of an inventory item - what a trader asks for it new,
        /// and the base its sell price is cut from.</summary>
        public static int ValueOf(InventoryItem item)
        {
            if (item == null) return 0;
            switch (item.Type)
            {
                case "weapon":
                    var w = item.Category != null && item.Key != null ? WeaponDatabase.Get(item.Category, item.Key) : null;
                    return w != null ? Mid(WeaponBand(w)) : item.Price ?? 0;
                case "armor":
                    var a = item.Category != null && item.Key != null ? ArmorDatabase.Get(item.Category, item.Key) : null;
                    int tier = int.TryParse(item.Category?.Replace("tier", ""), out var t) ? t : 1;
                    return a != null ? Mid(ArmorBand(a, tier)) : item.Price ?? 0;
                case "artifact":
                    return Mid(PoolBandByName("artifact", item.Name) ?? (item.Price ?? 500, item.Price ?? 500));
                case "detector":
                    return Mid(PoolBandByName("detector", item.Name) ?? (item.Price ?? 1000, item.Price ?? 1000));
                case "armorMod":
                    return Mid(PoolBandByName("mod", item.Name) ?? (item.Price ?? 300, item.Price ?? 300));
                case "consumable":
                    if (BaselinePrice.TryGetValue(item.Id, out var bp)) return bp;
                    return Mid(PoolBandByName("consumable", item.Name) ?? (item.Price ?? 50, item.Price ?? 50));
                default:
                    return item.Price ?? 0;
            }
        }

        /// <summary>Sell-back share by category (file 08 §1).</summary>
        public static float SellBack(string type)
        {
            switch (type)
            {
                case "weapon": return 0.40f;
                case "armor": return 0.35f;
                case "artifact": return 0.45f;
                case "detector": return 0.30f;
                case "armorMod": return 0.25f;
                case "consumable": return 0.20f;
                case "mutantPart": return 1f;     // traders pay the part's rolled value
                default: return 0.5f;
            }
        }

        /// <summary>Repair to Working: 10% of the band midpoint per Condition step (file 08 §5).</summary>
        public static int RepairCost(InventoryItem item)
        {
            int steps = ConditionSteps(item?.Condition);
            return steps == 0 ? 0 : System.Math.Max(10, ValueOf(item) / 10 * steps);
        }

        public static int ConditionSteps(string condition)
        {
            switch (condition)
            {
                case ItemCondition.Dirty: return 1;
                case ItemCondition.Damaged: return 2;
                case ItemCondition.Broken: return 3;
                default: return 0;
            }
        }

        // ------------------------------------------------------------------ the d100 table

        /// <summary>One roll on file 07's table. trader: Tier 5 weapons become Junk and there's no
        /// Tier Drift; loot: Tier Drift (d20 1-5 = a tier lower) and Tier 5 weapons only on 18-20.</summary>
        public static LootRoll Roll(int tier, bool trader)
        {
            tier = Mathf.Clamp(tier, 1, 5);
            for (int attempt = 0; attempt < 12; attempt++)
            {
                int t = tier;
                if (!trader && Random.Range(1, 21) <= 5) t = System.Math.Max(1, t - 1);   // Tier Drift
                int d = Random.Range(1, 101);
                if (d >= 98) { t = System.Math.Min(5, t + 1); d = Random.Range(1, 98); }   // Lucky find: next tier up
                string cat = d <= 16 ? "consumable" : d <= 21 ? "melee" : d <= 28 ? "sidearm" : d <= 33 ? "smg" : d <= 38 ? "shotgun"
                    : d <= 45 ? "assault" : d <= 49 ? "heavy" : d <= 54 ? "sniper" : d <= 67 ? "armor" : d <= 72 ? "mod" : d <= 78 ? "artifact"
                    : d <= 83 ? (Random.value < 0.5f ? "detector" : "backpack") : d <= 90 ? "cash" : "junk";

                if (cat == "cash")
                {
                    var b = CashBand[t];
                    return new LootRoll { Type = "cash", Name = "Ru", Price = Random.Range(b.min, b.max + 1) };
                }
                if (cat == "junk") return new LootRoll { Type = "junk", Name = "Junk", Price = Random.Range(10, 51) };
                if (cat == "armor") return RollArmor(t);

                if (!Pools[t].TryGetValue(cat, out var pool)) continue;                     // no pool at this tier: reroll
                string name = pool.Names[Random.Range(0, pool.Names.Length)];
                int price = Random.Range(pool.Min, pool.Max + 1);
                bool weapon = cat == "melee" || cat == "sidearm" || cat == "smg" || cat == "shotgun" || cat == "assault" || cat == "heavy" || cat == "sniper";
                if (weapon && t == 5)
                {
                    if (trader) return new LootRoll { Type = "junk", Name = "Junk", Price = Random.Range(10, 51) };
                    if (Random.Range(1, 21) <= 17 && Pools[4].TryGetValue(cat, out var p4))
                    {
                        name = p4.Names[Random.Range(0, p4.Names.Length)];
                        price = Random.Range(p4.Min, p4.Max + 1);
                    }
                }
                if (EncounterGeneration.Unique.Contains(name)) continue;                   // Batch 96: one of a kind, placed by hand
                var r = Make(cat, name, price);
                if (r != null) return r;
            }
            return new LootRoll { Type = "junk", Name = "Junk", Price = Random.Range(10, 51) };
        }

        /// <summary>An artifact pulled from an anomaly (file 03): evenly from the zone tier's
        /// Artifact pool, Tier Drift 25% one tier lower.</summary>
        public static LootRoll RollArtifact(int tier)
        {
            int t = Mathf.Clamp(tier, 1, 5);
            if (t > 1 && Random.Range(1, 5) == 1) t--;
            for (; t >= 1; t--)
            {
                if (!Pools[t].TryGetValue("artifact", out var pool)) continue;
                var r = Make("artifact", pool.Names[Random.Range(0, pool.Names.Length)], Random.Range(pool.Min, pool.Max + 1));
                if (r != null) return r;
            }
            return null;
        }

        private static LootRoll RollArmor(int tier)
        {
            // general pool only - faction-issue armor comes off that faction's NPCs, not traders
            var names = ArmorBands.Keys.Where(n => FindArmor(n).tier == tier).ToList();
            if (names.Count == 0) return null;
            string name = names[Random.Range(0, names.Count)];
            var (tierKey, key, def) = (FindArmor(name).tierKey, FindArmor(name).key, FindArmor(name).def);
            var band = ArmorBands[name];
            return new LootRoll
            {
                Type = "armor", Id = $"armor_{tierKey}_{key}", Name = name, Category = tierKey, Key = key,
                Price = Random.Range(band.min, band.max + 1), WeightKg = def.WeightKg ?? 4f,
            };
        }

        private static (string tierKey, string key, ArmorDef def, int tier) FindArmor(string name)
        {
            foreach (var t in ArmorDatabase.Tiers)
                foreach (var kv in t.Value)
                    if (kv.Value.Name == name) return (t.Key, kv.Key, kv.Value, int.Parse(t.Key.Replace("tier", "")));
            return (null, null, null, 0);
        }

        private static LootRoll Make(string cat, string name, int price)
        {
            switch (cat)
            {
                case "mod":
                    var mod = ArmorModDatabase.All.FirstOrDefault(kv => kv.Value.Name == name);
                    if (mod.Value == null) return null;
                    return new LootRoll { Type = "armorMod", Id = mod.Key, Key = mod.Key, Name = name, Price = price, WeightKg = 0.3f };
                case "artifact":
                    var art = ArtifactDatabase.All.FirstOrDefault(kv => kv.Value.Name == name);
                    if (art.Value == null) return null;
                    return new LootRoll { Type = "artifact", Id = art.Key, Name = name, Price = price, WeightKg = art.Value.WeightKg };
                case "detector":
                    var det = DetectorDatabase.All.FirstOrDefault(kv => kv.Value.Name == name);
                    if (det.Value == null) return null;
                    var shop = ShopDatabase.Items.FirstOrDefault(i => i.Type == "detector" && i.Key == det.Key);
                    return new LootRoll { Type = "detector", Id = shop?.Id ?? "detector_" + det.Key, Key = det.Key, Name = name, Price = price, WeightKg = 0.5f };
                case "backpack":
                    return new LootRoll { Type = "backpack", Id = "backpack_tourist", Name = "Tourist Frame (+50kg)", Price = price, WeightKg = 5f };
                case "consumable":
                    var con = ConsumableDatabase.All.FirstOrDefault(kv => kv.Value.Name == name);
                    if (con.Value == null) return null;
                    return new LootRoll { Type = "consumable", Id = con.Key, Name = name, Price = price, WeightKg = 0.5f };
                default:
                    if (!WeaponDatabase.Categories.TryGetValue(cat, out var dict)) return null;
                    var w = dict.FirstOrDefault(kv => kv.Value.Name == name);
                    if (w.Value == null) return null;
                    return new LootRoll { Type = "weapon", Id = $"weapon_{cat}_{w.Key}", Name = name, Category = cat, Key = w.Key, Price = price, WeightKg = w.Value.WeightKg ?? 3f };
            }
        }

        /// <summary>A trader's rolled stock (file 07): established traders 8-12 rolls, wandering 4-6.
        /// Junk and cash rolls just use up a slot.</summary>
        public static List<ShopItemDef> RollTraderStock(int tier, bool wandering)
        {
            int count = wandering ? Random.Range(4, 7) : Random.Range(8, 13);
            var list = new List<ShopItemDef>();
            for (int i = 0; i < count; i++)
            {
                var r = Roll(tier, trader: true);
                if (r.Type == "junk" || r.Type == "cash") continue;
                if (r.Type == "backpack" && list.Any(x => x.Id == r.Id)) continue;
                var item = r.ToShopItem();
                item.Id = "roll_" + System.Guid.NewGuid().ToString("N").Substring(0, 8) + "_" + r.Id;
                item.GrantId = r.Id;
                item.Tier = tier;
                list.Add(item);
            }
            return list;
        }
    }
}
