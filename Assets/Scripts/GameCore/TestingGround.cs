// Testing Ground (Yan, 2026-10-20: "an option at the menu for a 'testing ground' with enemy spawners and chests with all the
// loot for testing and stuff"). A small walled range of its own, started from the main menu: an armory crate with every
// weapon, armor and supply in the game (free, unlimited), a spawner terminal that puts any faction at any rank, or any
// mutant, on the spawn pad (or near you), and a lane of every kind of cover. It is a dev tool, outside the living world:
// no squads walk in, nothing seeds it, no anomalies (rule 7 is about the real maps).
using System.Collections.Generic;
using System.Linq;
using GameData;
using static GameCore.TileType;

namespace GameCore
{
    public static class TestingGround
    {
        public const string Zone = "testing_ground";
        public const string Armory = "Armory Crate", Spawner = "Spawner Terminal", Pad = "Spawn Pad";
        public const int W = 64, H = 40;

        public static bool Here => MacroState.Current != null && MacroState.Current.CurrentZoneId == Zone;

        /// <summary>The range: you arrive at the west end by the armory and the terminal; the spawn pad is 40 tiles east.
        /// Between them, lanes of cover: sandbags, concrete, wood (rounds go through), desks and barrels you can shove,
        /// an explosive barrel, a fence, brush, and a hut with a door and windows for room fights.</summary>
        public static MapLayout Map()
        {
            var c = new TerrainCanvas(W, H, TerrainCanvas.Seed(Zone));
            c.Paint(0, 0, W, H, GroundType.Grass);
            c.PaintNoise(GroundType.Dirt, 6f, 0.62f, 1);
            c.Keep(0, 0, W, H);                                                       // the dresser leaves it as drawn
            // the west yard: arrival, armory, terminal, on concrete
            c.Paint(2, 14, 10, 12, GroundType.Concrete);
            c.Landmark("arrive:" + Zone, 6, 20);
            c.Put(4, 16, HardCover, "lockers").Landmark(Armory, 4, 16);
            c.Put(4, 23, HardCover, "machine").Landmark(Spawner, 4, 23);
            // the spawn pad, east
            c.Paint(52, 15, 9, 10, GroundType.Concrete);
            c.Landmark(Pad, 56, 20);
            // lane 1 (north): hard cover - sandbags and concrete blocks at staggered ranges
            for (int x = 16; x <= 46; x += 6) { c.Put(x, 6, HardCover, "sandbags").Put(x + 1, 6, HardCover, "sandbags"); c.Put(x + 3, 10, HardCover, "block"); }
            // lane 2 (middle): light cover - wooden crates, desks to tip, rust barrels to kick, one explosive barrel
            for (int x = 18; x <= 44; x += 6) { c.Put(x, 17, ScatterCover, "crate"); c.Put(x + 2, 21, ScatterCover, "desk"); c.Put(x + 4, 24, ScatterCover, "rustbarrel"); }
            c.Put(34, 19, Barrel);
            // lane 3 (south): a fence line with a gap, brush, and a hut (door west, windows) to clear
            c.Fence(16, 30, 30, 30, gapAt: 23, gap: 2);
            c.ScatterIn(16, 32, 14, 5, ScatterCover, 0.18f, "brush");
            c.Building(36, 28, 9, 7, "wall", GroundType.Concrete, (36, 31));
            c.Put(40, 28, Solid, "window").Put(44, 31, Solid, "window");
            c.Put(39, 32, ScatterCover, "desk").Put(42, 30, HardCover, "lockers");
            c.Border(Solid, 1, 0, "wall");
            var m = c.ToMapLayout();
            m.FullSize = true;
            return m;
        }

        /// <summary>From the main menu: a fresh Scavenger dropped on the range with a fat purse, a big carry limit, and every
        /// faction hostile so whatever you spawn fights you.</summary>
        public static void Start()
        {
            GameBootstrap.StartNewGame(new CharacterSpec(), Zone);
            var p = Player.Current;
            foreach (var f in FactionDatabase.Factions.Keys) p.Reputation[f] = -3 * Player.RepPoint;
            p.Ru = 999999; p.CarryCapacityBonus = 500; p.ApplyCarryEffects();
            GameLog.Write("[TESTING GROUND] E at the armory crate: any gear, free. E at the spawner terminal: any enemy, on the pad or near you.");
        }

        // ---------------- what the panel does ----------------

        public static readonly string[] Kinds = { "Standard", "HollowPoint", "ArmorPiercing", "Dart" };

        public static InventoryItem ItemOf(ShopItemDef d, int qty = 1) => new InventoryItem
        { Id = d.Id, Name = d.Name, WeightKg = d.WeightKg, Qty = qty, Type = d.Type, Price = d.Price, Category = d.Category, Key = d.Key };

        public static void Take(Player p, ShopItemDef d, bool equip)
        {
            p.AddItem(ItemOf(d, d.Type == "weapon" || d.Type == "armor" ? 1 : 5));
            if (equip && d.Type == "weapon") { p.EquipWeaponFromInventory(d.Id); RefillAmmo(p); }
            else if (equip && d.Type == "armor") p.EquipArmorFromInventory(d.Id);
            p.ApplyCarryEffects();
            GameLog.Write($"[TESTING GROUND] {(equip ? "Equipped" : "Took")} {d.Name}.");
        }

        /// <summary>300 rounds of every ammo kind for every gun you carry, and full magazines.</summary>
        public static void RefillAmmo(Player p)
        {
            var guns = p.Inventory.Items.Where(i => i.Type == "weapon" && i.Category != "melee")
                .Select(i => WeaponDatabase.Categories.TryGetValue(i.Category ?? "", out var cat) && cat.TryGetValue(i.Key ?? "", out var d) ? d : null)
                .Concat(new[] { p.EquippedWeapon?.Db, p.SecondaryWeapon?.Db }).Where(d => d != null && d.Range != "Adjacent").Distinct();
            foreach (var d in guns) foreach (var k in Kinds) Ammo.Add(p.Inventory, Ammo.Key(d, k), 300 - Ammo.Get(p.Inventory, Ammo.Key(d, k)));
            foreach (var w in new[] { p.EquippedWeapon, p.SecondaryWeapon }) if (w != null && !CombatRules.IsMelee(w)) { w.CurrentMagazine = w.MagCapacity; w.IsJammed = false; }
            GameLog.Write("[TESTING GROUND] Ammo topped up.");
        }

        public static void Heal(Player p)
        {
            if (Body.Down(p)) Body.GetUp(p);
            p.HealPermanent(p.MaxHp * 2);
            p.StatusEffects.Bleeding = false;
            GameLog.Write("[TESTING GROUND] Patched up.");
        }

        public static void Clear()
        {
            EncounterState.Enemies.RemoveAll(e => e != Companion.Current);
            EncounterState.InCombat = false;
            GameLog.Write("[TESTING GROUND] Range cleared.");
        }

        /// <summary>Rank -> the tier its gear is drawn for (Rookie/Experienced 1 ... Legend 6).</summary>
        public static int TierOf(string rank) => System.Math.Max(1, CombatRules.RankIndex(rank));

        /// <summary>`n` of a faction at a rank: on the pad, or 8 tiles in front of you (east). Aware = they know where you are.</summary>
        public static List<HumanEnemy> SpawnHumans(Player p, string faction, string rank, int n, bool near, bool aware)
        {
            var list = new List<HumanEnemy>();
            foreach (var (x, y) in Spots(p, n, near))
            {
                var (w, a) = EncounterGeneration.PickFactionLoadout(faction, rank, TierOf(rank), isLeader: list.Count == 0);
                var h = new HumanEnemy($"{faction} {rank}", x, y, faction, rank, w, a) { IsAggroed = aware };
                HumanLootSystem.RollSpawnStash(h, TierOf(rank));
                EncounterState.Enemies.Add(h); list.Add(h);
            }
            Spawned(n, $"{faction} {rank}", aware);
            return list;
        }

        public static List<Mutant> SpawnMutants(Player p, MutantDef def, int n, bool near, bool aware)
        {
            var list = new List<Mutant>();
            foreach (var (x, y) in Spots(p, n, near))
            {
                var m = EncounterGeneration.CreateMutantFromDb(def, x, y); m.IsAggroed = aware;
                EncounterState.Enemies.Add(m); list.Add(m);
            }
            Spawned(n, def.Name, aware);
            return list;
        }

        public static IEnumerable<MutantDef> AllMutants() => MutantDatabase.Tiers.OrderBy(t => t.Key).SelectMany(t => t.Value.Values);

        static void Spawned(int n, string what, bool aware)
        {
            if (aware && EncounterState.Enemies.Any(e => e.IsAlive() && e.IsAggroed && !EnemyAI.FriendlyToPlayer(e))) EncounterState.InCombat = true;
            GameLog.Write($"[TESTING GROUND] {n} x {what} {(aware ? "- they know you're here" : "- unaware")}.");
        }

        static List<(int x, int y)> Spots(Player p, int n, bool near)
        {
            var pad = TerrainMap.Landmarks.FirstOrDefault(l => l.Name == Pad);
            var (cx, cy) = near || pad == null ? (System.Math.Min(TerrainMap.Width - 2, p.X + 8), p.Y) : (pad.X, pad.Y);
            var taken = new HashSet<(int, int)>(EncounterState.Enemies.Where(e => e.IsAlive()).Select(e => (e.X, e.Y))) { (p.X, p.Y) };
            var spots = new List<(int, int)>();
            for (int i = 0; i < n; i++)
            {
                var s = TerrainMap.FindNearestEnterable(cx, cy);
                for (int k = 0; k < 30 && taken.Contains(s); k++) s = TerrainMap.FindNearestEnterable(cx + UnityEngine.Random.Range(-3, 4), cy + UnityEngine.Random.Range(-3, 4));
                taken.Add(s); spots.Add(s);
            }
            return spots;
        }

        /// <summary>E next to the crate or the terminal opens the panel on that page.</summary>
        public static bool TryInteract(Player p)
        {
            if (!Here) return false;
            foreach (var l in TerrainMap.Landmarks)
            {
                if (System.Math.Max(System.Math.Abs(l.X - p.X), System.Math.Abs(l.Y - p.Y)) > 2) continue;
                if (l.Name == Armory) { TestingGroundUI.Open(false); return true; }
                if (l.Name == Spawner) { TestingGroundUI.Open(true); return true; }
            }
            return false;
        }
    }
}
