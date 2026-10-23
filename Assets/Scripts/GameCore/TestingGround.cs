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
            Dummies.Clear(); Numbers.Clear(); LastHp.Clear(); Noted.Clear();
            SpawnDummy(13, 19, null, null); SpawnDummy(26, 19, "Raiders", "Rookie"); SpawnDummy(40, 19, "Garrison", "Veteran");   // 7, 20 and 34 tiles out
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

        /// <summary>RT 7z-c: try two pistols - Point-Blank Execution and Gunslinger, a PSh-18 in each hand, full magazines.</summary>
        public static void GunslingerKit(Player p)
        {
            p.TakePerk("PointBlankExecution"); p.TakePerk("Gunslinger");
            var d = ShopDatabase.Items.First(i => i.Type == "weapon" && i.Category == "sidearm" && i.Key == "PSh18Pchela");
            if (CombatRules.Category(p.EquippedWeapon?.Db) != "sidearm") Take(p, d, true);
            p.AddItem(ItemOf(d));
            p.EquipOffhandFromInventory(d.Id);
            RefillAmmo(p);
            GameLog.Write("[TESTING GROUND] Two pistols: left button fires the right-hand gun, right button the left. R reloads both.");
        }

        /// <summary>300 rounds of every ammo kind for every gun you carry, and full magazines.</summary>
        public static void RefillAmmo(Player p)
        {
            var guns = p.Inventory.Items.Where(i => i.Type == "weapon" && i.Category != "melee")
                .Select(i => WeaponDatabase.Categories.TryGetValue(i.Category ?? "", out var cat) && cat.TryGetValue(i.Key ?? "", out var d) ? d : null)
                .Concat(new[] { p.EquippedWeapon?.Db, p.SecondaryWeapon?.Db }).Where(d => d != null && d.Range != "Adjacent").Distinct();
            foreach (var d in guns) foreach (var k in Kinds) Ammo.Add(p.Inventory, Ammo.Key(d, k), 300 - Ammo.Get(p.Inventory, Ammo.Key(d, k)));
            foreach (var w in new[] { p.EquippedWeapon, p.SecondaryWeapon, p.OffhandWeapon }) if (w != null && !CombatRules.IsMelee(w)) { w.CurrentMagazine = w.MagCapacity; w.IsJammed = false; }
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
            EncounterState.Enemies.RemoveAll(e => e != Companion.Current && !IsDummy(e));   // the dummies stay (the terminal adds more)
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
            if (aware && EncounterState.Enemies.Any(e => e.IsAlive() && e.IsAggroed && !IsDummy(e) && !EnemyAI.FriendlyToPlayer(e))) EncounterState.InCombat = true;
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

        // ---------------- target dummies and damage numbers ----------------
        // Yan: "add a target dummy that shows damage numbers". A dummy is a person kitted as a faction at a rank (or bare) who
        // never moves, shoots, wanders or notices you. Every hit on the range shows its damage over the target (head hits
        // yellow, kills red); a dummy heals fully 3 s after its last hit, and one you drop stands up again 3 s later, so
        // you can count the hits to kill. ponytail: it uses the faction's own person sprite, not a straw dummy of its own.

        public const string DummyFaction = "Target Dummy";
        public const float ResetAfter = 3f;
        public class Dummy { public HumanEnemy H; public int X, Y; public string Faction, Rank; public float LastHit = -99f; public int Hits, Total; }
        public static readonly List<Dummy> Dummies = new List<Dummy>();
        public class Number { public ICombatant Who; public string Text; public int Kind; public float At; }   // Kind: 0 body, 1 head, 2 kill
        public static readonly List<Number> Numbers = new List<Number>();
        public const float NumberSeconds = 1.1f;
        public static float Now = -1f;                                            // the harness sets the clock; the game uses Time.time
        static float T => Now >= 0f ? Now : UnityEngine.Time.time;
        static readonly Dictionary<ICombatant, int> LastHp = new Dictionary<ICombatant, int>(), Noted = new Dictionary<ICombatant, int>();

        public static bool IsDummy(ICombatant u) => u is HumanEnemy h && h.FactionName == DummyFaction;

        /// <summary>A dummy at (x, y) wearing what `faction` gives a `rank` (null faction: no armor at all). Unarmed.</summary>
        public static HumanEnemy SpawnDummy(int x, int y, string faction, string rank)
        {
            var armor = faction == null ? null : EncounterGeneration.PickFactionLoadout(faction, rank, TierOf(rank)).armor;
            var h = new HumanEnemy(faction == null ? "Target Dummy" : $"Dummy ({faction} {rank})", x, y, DummyFaction, rank ?? "Rookie", null, armor) { IsAggroed = false };
            EncounterState.Enemies.Add(h);
            Dummies.Add(new Dummy { H = h, X = x, Y = y, Faction = faction, Rank = rank });
            return h;
        }

        public static HumanEnemy SpawnDummy(Player p, string faction, string rank, bool near)
        {
            var (x, y) = Spots(p, 1, near)[0];
            GameLog.Write($"[TESTING GROUND] A target dummy{(faction == null ? " with no armor" : $" kitted as a {faction} {rank}")}.");
            return SpawnDummy(x, y, faction, rank);
        }

        /// <summary>Gunplay.Resolve and Melee.Strike tell us the part a hit landed on; any other damage (a blast, bleeding)
        /// shows up as plain numbers from the HP change in Tick.</summary>
        public static void Note(ICombatant u, int dmg, string part)
        {
            if (!Here || u == null || dmg <= 0) return;
            Noted[u] = (Noted.TryGetValue(u, out var n) ? n : 0) + dmg;
            Show(u, dmg, part);
        }

        static void Show(ICombatant u, int dmg, string part)
        {
            bool dead = !u.IsAlive();
            Numbers.Add(new Number { Who = u, Text = $"{dmg}{(part != null ? " " + part.ToLowerInvariant() : "")}{(dead ? " KILL" : "")}", Kind = dead ? 2 : part == "Head" ? 1 : 0, At = T });
            var d = Dummies.FirstOrDefault(q => q.H == u);
            if (d == null) return;
            d.Hits++; d.Total += dmg; d.LastHit = T;
            if (dead) GameLog.Write($"[TESTING GROUND] {d.H.Name} down: {d.Hits} hit{(d.Hits == 1 ? "" : "s")}, {d.Total} damage.");
        }

        /// <summary>Every frame on the range (Rt.WorldTick): dummies stay put and unaware, heal or stand up again, and HP lost
        /// without a Note becomes a number too.</summary>
        public static void Tick()
        {
            float now = T;
            Numbers.RemoveAll(n => now - n.At > NumberSeconds);
            foreach (var e in EncounterState.Enemies.ToList())
            {
                int hp = e.Hp, last = LastHp.TryGetValue(e, out var l) ? l : hp, noted = Noted.TryGetValue(e, out var n) ? n : 0;
                if (last - hp > noted) Show(e, last - hp - noted, null);
                LastHp[e] = hp; Noted.Remove(e);
            }
            foreach (var d in Dummies.ToList())
            {
                d.H.IsAggroed = false;
                if (now - d.LastHit < ResetAfter) continue;
                if (!d.H.IsAlive())
                {
                    EncounterState.Enemies.Remove(d.H); Dummies.Remove(d); LastHp.Remove(d.H);
                    LastHp[SpawnDummy(d.X, d.Y, d.Faction, d.Rank)] = int.MaxValue;   // (the fresh one's full HP isn't a hit)
                }
                else if (d.Hits > 0)
                {
                    Body.Forget(d.H); d.H.Hp = d.H.MaxHp; d.H.StatusEffects.Bleeding = false; d.Hits = 0; d.Total = 0;
                    LastHp[d.H] = d.H.Hp;
                }
            }
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
