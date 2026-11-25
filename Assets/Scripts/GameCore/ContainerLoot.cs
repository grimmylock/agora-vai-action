// 2026-11-24 (Yan: "carefull to match the stash spot to the actual loot pool, i don't want weapons and armor spawning in normal civilian homes ... Has to make sense"): the small containers of furnished buildings (a wardrobe, a kitchen counter, a footlocker).
// A building has an ARCHETYPE (house, shop, barracks ...), a furniture piece in it may hold something, and what it can hold comes from the pool of that archetype and that piece: a wardrobe in a house holds pocket money and a bandage, a kitchen
// counter holds food and drink, a barracks footlocker holds ammo, dressings, a stim; only military archetypes can hold weapons, armor or ammo (`Civilian` ones may never: the harness checks it). They are small finds - the game's rule stays "ordinary
// loot comes from bodies and quests, about eight real stashes a map" (Stashes.cs) - so only some pieces of a building hold anything (`Chance`), decided by a hash of the tile, the same every time the map is built.
using System.Collections.Generic;
using System.Linq;
using GameData;

namespace GameCore
{
    public static class ContainerLoot
    {
        public struct Entry { public int W; public string Kind, Id; public int Min, Max; }
        static Entry E(int w, string kind, string id = null, int min = 1, int max = 1) => new Entry { W = w, Kind = kind, Id = id, Min = min, Max = max };

        public class Rule { public string Name; public float Chance; public Entry[] Pool; }
        static Rule R(string name, float chance, params Entry[] pool) => new Rule { Name = name, Chance = chance, Pool = pool };

        public static readonly HashSet<string> Civilian = new HashSet<string> { "house", "shop", "office", "clinic", "school", "church", "workshop", "bunkhouse", "bar", "warehouse" };
        public static readonly HashSet<string> CivilianKinds = new HashSet<string> { "ru", "item" };                       // no weapon, armor or ammo
        public static readonly HashSet<string> CivilianItems = new HashSet<string> { "mre", "energyDrink", "bandage", "splint", "fieldRepairKit", "basicMedkit", "radBlockPill" };

        /// <summary>archetype -> furniture look -> what it can hold.</summary>
        public static readonly Dictionary<string, Dictionary<string, Rule>> Rules = new Dictionary<string, Dictionary<string, Rule>>
        {
            ["house"] = new Dictionary<string, Rule>
            {
                ["kitchensink"] = R("Cupboard", 0.5f, E(4, "item", "mre"), E(3, "item", "energyDrink"), E(3, "ru", null, 3, 12)),
                ["kitchencounter"] = R("Cupboard", 0.5f, E(4, "item", "mre"), E(3, "item", "energyDrink"), E(3, "ru", null, 3, 12)),
                ["fridge"] = R("Fridge", 0.6f, E(4, "item", "mre"), E(4, "item", "energyDrink"), E(2, "ru", null, 2, 8)),
                ["wardrobe"] = R("Wardrobe", 0.45f, E(6, "ru", null, 5, 25), E(2, "item", "bandage")),
                ["nightstand"] = R("Nightstand", 0.5f, E(5, "ru", null, 5, 30), E(3, "item", "bandage"), E(1, "item", "basicMedkit")),
                ["bookshelf"] = R("Bookshelf", 0.25f, E(1, "ru", null, 3, 10)),
                ["desk"] = R("Desk", 0.5f, E(5, "ru", null, 5, 30), E(1, "item", "bandage")),
            },
            ["shop"] = new Dictionary<string, Rule>
            {
                ["goodsshelf"] = R("Shelf", 0.5f, E(4, "item", "mre"), E(4, "item", "energyDrink"), E(2, "item", "bandage"), E(1, "item", "splint"), E(1, "item", "fieldRepairKit")),
                ["shopcounter"] = R("Till", 0.7f, E(1, "ru", null, 25, 90)),
                ["boxstack"] = R("Boxes", 0.5f, E(3, "item", "mre"), E(3, "item", "energyDrink"), E(2, "ru", null, 5, 20), E(1, "item", "fieldRepairKit")),
                ["fridge"] = R("Freezer", 0.7f, E(4, "item", "energyDrink"), E(3, "item", "mre")),
                ["desk"] = R("Desk", 0.6f, E(1, "ru", null, 10, 45)),
                ["crate"] = R("Crate", 0.4f, E(3, "item", "mre"), E(2, "item", "energyDrink"), E(2, "ru", null, 3, 15)),
            },
            ["warehouse"] = new Dictionary<string, Rule>
            {
                ["goodsshelf"] = R("Shelf", 0.4f, E(3, "item", "mre"), E(2, "item", "fieldRepairKit"), E(3, "ru", null, 5, 25)),
                ["boxstack"] = R("Boxes", 0.5f, E(3, "item", "mre"), E(2, "item", "energyDrink"), E(3, "ru", null, 5, 25)),
                ["crate"] = R("Crate", 0.5f, E(2, "item", "fieldRepairKit"), E(3, "ru", null, 3, 15)),
                ["desk"] = R("Desk", 0.5f, E(1, "ru", null, 5, 30)),
                ["lockers"] = R("Locker", 0.5f, E(3, "item", "bandage"), E(2, "ru", null, 5, 20)),
                ["bottleshelf"] = R("Bottle Shelf", 0.7f, E(4, "item", "energyDrink"), E(2, "ru", null, 3, 12)),
                ["barcounter"] = R("Till", 0.8f, E(1, "ru", null, 10, 45)),
                ["rustbarrel"] = R("Barrel", 0.3f, E(3, "item", "energyDrink"), E(1, "ru", null, 2, 8)),
                ["kegs"] = R("Kegs", 0.4f, E(3, "item", "energyDrink"), E(1, "ru", null, 2, 10)),
            },
            ["bunkhouse"] = new Dictionary<string, Rule>
            {
                ["footlocker"] = R("Footlocker", 0.5f, E(4, "ru", null, 5, 25), E(3, "item", "bandage"), E(3, "item", "mre")),
                ["stove"] = R("Stove", 0.25f, E(1, "item", "mre")),
                ["bunkbed"] = R("Bunk", 0.2f, E(3, "ru", null, 3, 15), E(1, "item", "bandage")),
            },
            ["clinic"] = new Dictionary<string, Rule>
            {
                ["medcabinet"] = R("Medicine Cabinet", 0.7f, E(5, "item", "bandage"), E(3, "item", "splint"), E(2, "item", "basicMedkit"), E(1, "item", "radBlockPill")),
                ["fridge"] = R("Medicine Fridge", 0.6f, E(3, "item", "bandage"), E(2, "item", "basicMedkit")),
                ["desk"] = R("Desk", 0.5f, E(1, "ru", null, 5, 30)),
                ["kitchensink"] = R("Cupboard", 0.4f, E(3, "item", "bandage"), E(1, "item", "splint")),
            },
            ["bar"] = new Dictionary<string, Rule>
            {
                ["bottleshelf"] = R("Bottle Shelf", 0.7f, E(4, "item", "energyDrink"), E(2, "ru", null, 3, 12)),
                ["barcounter"] = R("Till", 0.8f, E(1, "ru", null, 10, 45)),
                ["rustbarrel"] = R("Barrel", 0.3f, E(3, "item", "energyDrink"), E(1, "ru", null, 2, 8)),
            },
            ["workshop"] = new Dictionary<string, Rule>
            {
                ["lockers"] = R("Locker", 0.6f, E(4, "item", "fieldRepairKit"), E(2, "ru", null, 5, 20)),
                ["boxstack"] = R("Boxes", 0.6f, E(3, "item", "fieldRepairKit"), E(3, "ru", null, 5, 20)),
                ["desk"] = R("Desk", 0.5f, E(1, "ru", null, 5, 25)),
                ["crate"] = R("Crate", 0.5f, E(2, "item", "fieldRepairKit"), E(2, "ru", null, 3, 15)),
            },
            ["barracks"] = new Dictionary<string, Rule>
            {
                ["footlocker"] = R("Footlocker", 0.55f, E(4, "ammo", null, 6, 14), E(3, "item", "bandage"), E(2, "ru", null, 10, 40), E(1, "item", "basicStim")),
                ["lockers"] = R("Locker", 0.5f, E(3, "item", "bandage"), E(2, "ammo", null, 6, 12), E(2, "ru", null, 10, 35), E(1, "armor")),
                ["weaponrack"] = R("Weapon Rack", 0.7f, E(3, "weapon"), E(3, "ammo", null, 8, 16)),
                ["boxstack"] = R("Supply Boxes", 0.6f, E(3, "ammo", null, 10, 20), E(2, "item", "basicMedkit"), E(2, "item", "mre")),
                ["maptable"] = R("Map Table", 0.4f, E(1, "ru", null, 10, 50)),
                ["desk"] = R("Officer's Desk", 0.6f, E(3, "ru", null, 15, 60), E(1, "item", "basicStim")),
                ["crate"] = R("Crate", 0.5f, E(3, "ammo", null, 6, 12), E(2, "item", "mre")),
            },
        };

        public static bool Is(InteractiveObjectInstance o) => o?.Reward?.Loot?.Id != null && o.Reward.Loot.Id.StartsWith("cont:");

        static int Hash(int x, int y, string salt) { unchecked { uint h = 2166136261; foreach (char c in salt) { h ^= c; h *= 16777619; } h ^= (uint)(x * 73856093) ^ (uint)(y * 19349663); h *= 16777619; return (int)(h & 0x7fffffff); } }

        /// <summary>Does this piece of this archetype hold something here (decided by the tile)? Returns the anchor name for the layout, or null.</summary>
        public static string AnchorFor(string archetype, string look, int x, int y)
        {
            if (archetype == null || !Rules.TryGetValue(archetype, out var set) || !set.TryGetValue(look, out var rule)) return null;
            return Hash(x, y, archetype + look) % 1000 < rule.Chance * 1000f ? $"cont:{archetype}:{look}:{x},{y}" : null;
        }

        /// <summary>TerrainMap.SpawnInteractiveObjects: an anchor "cont:archetype:look:x,y" becomes a small container on the furniture's own tile.</summary>
        public static InteractiveObjectInstance Spawn(string anchorName, int x, int y)
        {
            var parts = anchorName.Split(':'); if (parts.Length < 4 || !Rules.TryGetValue(parts[1], out var set) || !set.TryGetValue(parts[2], out var rule)) return null;
            return new InteractiveObjectInstance
            {
                X = x, Y = y, Name = rule.Name, Stat = new[] { "Agility" }, Dc = 2, Description = "You could go through it.",
                Reward = new InteractiveReward { Loot = new LootItem { Id = "cont:" + parts[1] + ":" + parts[2], Name = rule.Name, Qty = 1, Type = "loot" } },
            };
        }

        public static Entry Pick(Rule rule)
        {
            int total = rule.Pool.Sum(e => e.W), r = UnityEngine.Random.Range(0, total);
            foreach (var e in rule.Pool) { r -= e.W; if (r < 0) return e; }
            return rule.Pool[0];
        }

        /// <summary>E on one: what is in it.</summary>
        public static void Grant(Player p, string id)
        {
            var parts = id.Split(':'); if (parts.Length < 3 || !Rules.TryGetValue(parts[1], out var set) || !set.TryGetValue(parts[2], out var rule)) return;
            var e = Pick(rule); string what = rule.Name.ToLowerInvariant(); int tier = MacroMapDatabase.Find(MacroState.Current.CurrentZoneId)?.Tier ?? 1;
            switch (e.Kind)
            {
                case "ru": { int n = UnityEngine.Random.Range(e.Min, e.Max + 1); p.Ru += n; GameLog.Write($"In the {what}: {n} RU."); break; }
                case "item": { var d = ShopDatabase.Find(e.Id); if (d != null) { p.AddItem(new InventoryItem { Id = d.Id, Name = d.Name, WeightKg = d.WeightKg, Qty = 1, Type = d.Type, Price = d.Price }); GameLog.Write($"In the {what}: {d.Name}."); } break; }
                case "ammo": { var db = p.EquippedWeapon?.Db; int n = UnityEngine.Random.Range(e.Min, e.Max + 1); Ammo.Add(p.Inventory, Ammo.Key(Ammo.Pool(db) ?? "9x19mm", "Standard"), n); GameLog.Write($"In the {what}: {n} rounds."); break; }
                case "weapon": case "armor": { LootRoll r = null; for (int i = 0; i < 12 && (r == null || r.Type != e.Kind); i++) r = Economy.Roll(tier, trader: false); if (r != null && r.Type == e.Kind) InteractionEngine.GrantRoll(p, r); break; }
            }
        }
    }
}
