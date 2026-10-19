// Batch 87 (Yan: "Garrison base should be a 'raid', where enemys come in waves ... reward should include plenty of ap and hp
// ammo, high tier healing items, 2 tier 4 and 1 tier 5 weapon (random), one tier 5 armor. all of this in their 'armory'";
// "repeatable after a few in-game days"; "since the garisson is technically the government they'll send more troops in
// eventually so they can continue spawning"; "trigger is either the player starting combat in the area or the eventual faction
// wars"). Start a fight in the Siege Ward (the hospital region, Garrison's base) and the base answers: the guards there are
// wave 1, then two more squads come out of the base a few rounds apart - real Garrison cards from their deck, so a drained
// deck sends fewer - the last one led by a boss. When the last wave is down the armory opens. It can be raided again
// RaidCooldownDays after; Garrison's deck refills as usual (cards come back a day after their people die).
using System.Collections.Generic;
using System.Linq;
using GameData;

namespace GameCore
{
    public static class GarrisonRaid
    {
        public const string Region = "hospital", Armory = "Garrison Armory";
        public const int WaveGapRounds = 3, Waves = 3, RaidCooldownDays = 4, BaseRadius = 35;

        static int _wave, _roundsToNext;    // ponytail: a raid in progress isn't saved; a reload mid-raid just ends it
        static readonly List<Mutant> _raiders = new List<Mutant>();   // the guards in the ward when it started, and the waves
        public static bool Active => _wave > 0;
        public static int Left => _raiders.Count(m => m.IsAlive() && EncounterState.Enemies.Contains(m) && !(m is HumanEnemy h && EnemyAI.IsOut(h)));   // Batch 90: one that ran off the map has left the fight
        public static void Reset() { _wave = 0; _roundsToNext = 0; _raiders.Clear(); }

        public static bool InBase(Player p) => MacroState.Current.CurrentZoneId == Region;
        static IEnumerable<HumanEnemy> GarrisonHere() => EncounterState.Enemies.OfType<HumanEnemy>().Where(h => h.FactionName == "Garrison" && h.IsAlive() && !EnemyAI.IsOut(h));

        /// <summary>Every enemy round (TurnLoop.PlayerEndTurn): start the raid, send the next wave, or open the armory.</summary>
        public static void Round(Player p)
        {
            if (p == null) return;
            if (!Active)
            {
                if (EncounterState.InCombat && InBase(p) && MacroState.Current.Day >= MacroState.Current.GarrisonRaidReadyDay && GarrisonHere().Any(h => h.IsAggroed)) Start();
                return;
            }
            if (!InBase(p) && !EncounterState.InCombat) { GameLog.Write("[RAID] You pulled out of the Siege Ward. Garrison locks the base down again."); End(false); return; }
            foreach (var m in _raiders) if (m.IsAlive()) m.IsAggroed = true;   // Batch 89: the sirens are on - they know you're here (no breaking contact mid-raid)
            if (_wave < Waves && --_roundsToNext <= 0) SendWave(p);
            else if (_wave >= Waves && Left == 0) End(true);   // Batch 89: only the raid's own soldiers (not Garrison elsewhere on the map)
        }

        /// <summary>Also the hook for the faction wars to come.</summary>
        public static void Start()
        {
            _wave = 1; _roundsToNext = WaveGapRounds;
            _raiders.Clear();
            // Batch 89: the guards round the base itself (not every Garrison squad in the whole region)
            var hq = TerrainMap.Landmarks.FirstOrDefault(l => l.Name == "Siege Ward Hospital");
            _raiders.AddRange(GarrisonHere().Where(h => TerrainMap.RegionAt(h.X, h.Y) == Region
                && (hq == null || System.Math.Max(System.Math.Abs(h.X - hq.X), System.Math.Abs(h.Y - hq.Y)) <= BaseRadius)));
            GameLog.Write("[RAID] Sirens wail across the Siege Ward. Garrison is calling every man in the base to arms.");
            Sfx.Play("alert");
        }

        static void SendWave(Player p)
        {
            _wave++; _roundsToNext = WaveGapRounds;
            var sq = SquadManager.CreateOfflineSquad(Region, "Garrison", 4 + UnityEngine.Random.Range(0, 3));
            if (sq == null) { GameLog.Write("[RAID] The base has nobody left to send."); return; }
            var lead = sq.Members.OfType<HumanEnemy>().OrderByDescending(h => CombatRules.RankIndex(h.Rank)).FirstOrDefault();
            if (_wave == Waves && lead != null && !lead.Named) { SquadManager.MakeBoss(lead); SquadManager.Arm(sq); }
            var gate = Gate(p);
            var taken = new HashSet<(int, int)>(EncounterState.Enemies.Where(e => e.IsAlive()).Select(e => (e.X, e.Y))) { (p.X, p.Y) };
            foreach (var m in sq.Members.Where(m => m.IsAlive()))
            {
                var spot = TerrainMap.FindNearestEnterable(gate.x, gate.y);
                for (int i = 0; i < 20 && taken.Contains(spot); i++)
                    spot = TerrainMap.FindNearestEnterable(gate.x + UnityEngine.Random.Range(-3, 4), gate.y + UnityEngine.Random.Range(-3, 4));
                taken.Add(spot);
                m.X = spot.x; m.Y = spot.y; m.IsAggroed = true;
                EncounterState.Enemies.Add(m); _raiders.Add(m);
            }
            GameLog.Write(_wave == Waves ? $"[RAID] The last of the garrison pours out of the hospital, led by {lead?.Name}." : "[RAID] More Garrison troops come running out of the hospital.");
        }

        /// <summary>Where a wave comes out: the base's doors farthest from you (at least out of arm's reach).</summary>
        static (int x, int y) Gate(Player p)
        {
            var doors = TerrainMap.Landmarks.Where(l => l.Name == "Siege Ward Hospital" || l.Name == "Morgue Stair")   // Batch 90: not the armory (waves came out inside it and stayed)
                .Select(l => (l.X, l.Y)).ToList();
            if (doors.Count == 0) return (p.X, p.Y);
            return doors.OrderByDescending(d => System.Math.Max(System.Math.Abs(d.X - p.X), System.Math.Abs(d.Y - p.Y))).First();
        }

        static void End(bool won)
        {
            Reset();
            MacroState.Current.GarrisonRaidReadyDay = MacroState.Current.Day + RaidCooldownDays;
            if (!won) return;
            MacroState.Current.GarrisonArmoryOpen = true;
            GameLog.Write("[RAID] The Siege Ward falls quiet. The armory door hangs open.");
        }

        /// <summary>E at the armory (InteractionEngine, before stashes).</summary>
        public static bool TryInteract(Player p)
        {
            if (!InBase(p) || !TerrainMap.Landmarks.Any(l => l.Name == Armory && System.Math.Max(System.Math.Abs(l.X - p.X), System.Math.Abs(l.Y - p.Y)) <= 2)) return false;
            if (!MacroState.Current.GarrisonArmoryOpen) { GameLog.Write("The armory is barred from the inside. Garrison still holds the Siege Ward."); return true; }
            MacroState.Current.GarrisonArmoryOpen = false;
            GameLog.Write("[RAID] You strip the Garrison armory:");
            Loot(p);
            return true;
        }

        public static void Loot(Player p)
        {
            foreach (var w in new[] { p.EquippedWeapon, p.SecondaryWeapon }.Where(w => w != null && !CombatRules.IsMelee(w)))
                foreach (var kind in new[] { "ArmorPiercing", "HollowPoint" })
                {
                    var key = Ammo.Key(w.Db, kind); int n = ShopSystem.RoundsPerBox(w) * 4;
                    Ammo.Add(p.Inventory, key, n); GameLog.Write($"  - {n} {Ammo.Label(key)}");
                }
            foreach (var (id, qty) in new[] { ("scientificMedkit", 3), ("scientificStim", 2), ("bandage", 3) })
            {
                var d = ConsumableDatabase.All[id];
                p.AddItem(new InventoryItem { Id = id, Name = d.Name, Type = "consumable", Qty = qty, WeightKg = 0.3f });
                GameLog.Write($"  - {d.Name} x{qty}");
            }
            foreach (int tier in new[] { 4, 4, 5 }) Give(p, "weapon", tier);
            Give(p, "armor", 5);
        }

        static void Give(Player p, string type, int tier)
        {
            var pool = ShopDatabase.Items.Where(i => i.Type == type && i.Tier == tier && i.Key != "G7RailPrototype").ToList();
            if (pool.Count == 0) return;
            var d = pool[UnityEngine.Random.Range(0, pool.Count)];
            p.AddItem(new InventoryItem { Id = d.Id, Name = d.Name, WeightKg = d.WeightKg, Qty = 1, Type = type, Price = d.Price, Category = d.Category, Key = d.Key, Condition = ItemCondition.Working });
            GameLog.Write($"  - {d.Name}");
        }
    }
}
