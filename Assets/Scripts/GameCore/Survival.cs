// Survival (Yan, 2026-10-30: "add a survival mode, starts at a randomly chosen poi, enemy waves come starting with 5 then 3 more
// each wave, every 5 waves one boss, or high tier mutant spawns, every boss or high tier mutant ups the 'tier' of the waves, so
// harder mutants and human enemies, all human enemies are lootable and have ammo and pristine weapons and armor on death, 15 second
// delay between waves, maximum time of 1 minute between waves besides the boss one, i'll use this to stress test the combat,
// player starts with basic pistol and some meds").
// A dev mode like the Testing Ground, outside the living world (rule 7 is about the real game): a random spot on a random tier map,
// no squads (none seeded, none walking in: SquadManager.TickHour), no anomalies, every faction hostile, not saved.
// - Wave n: 5 + 3(n-1) enemies coming for you from 16-24 tiles out, human waves (one faction, ranks of the current tier) and mutant
//   waves (that tier's mutants) taking turns.
// - Every 5th wave: one boss instead - a human boss (SquadManager.MakeBoss) and a named apex mutant (two tiers up) taking turns.
//   Killing it raises the tier (to 6).
// - The next wave: 15 s after a wave is cleared, or 60 s after it began if it isn't; a boss wave waits for the boss (+15 s).
// - Human dead drop pristine gear and ammo (HumanLootSystem asks Survival.On).
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class Survival
    {
        public static bool On;
        public static int Wave, Tier = 1, Kills;
        public static float NextAt = -1f, WaveStarted = -1f;
        public static bool BossWave;
        public static readonly List<Mutant> Live = new List<Mutant>();
        public const float Breather = 15f, MaxWave = 60f;
        public const int FirstWave = 5, PerWave = 3, BossEvery = 5, MaxTier = 6;
        static readonly string[] Maps = { "perimeter", "scrapyard", "scavengers_hub", "vanguard_barracks", "quarry_basin", "abandoned_city" };
        static readonly string[] HumanFactions = { "Raiders", "Garrison", "Outcasts", "Contractors", "Cartel", "Liberators", "Obelisk" };
        public static float Now = -1f;                                            // the harness sets the clock
        static float T => Now >= 0f ? Now : Time.time;

        /// <summary>From the main menu: a Scavenger (pistol, knife, meds) at a random place on a random tier map.</summary>
        public static void Start()
        {
            string map = Maps[Random.Range(0, Maps.Length)];
            GameBootstrap.StartNewGame(new CharacterSpec(), map, save: false);
            Begin(Player.Current);
        }

        /// <summary>Set the run up on the loaded map (the harness calls it directly).</summary>
        public static void Begin(Player p)
        {
            On = true; Wave = 0; Tier = 1; Kills = 0; BossWave = false; Live.Clear(); WaveStarted = -1f;
            MacroState.Current.OfflineSquads.Clear();                                // nobody else in the Zone
            EncounterState.Enemies.RemoveAll(e => e != Companion.Current); EncounterState.Anomalies.Clear();
            foreach (var f in FactionDatabase.Factions.Keys) p.Reputation[f] = -3 * Player.RepPoint;
            var spots = TerrainMap.Landmarks.Where(l => l.Name != null && !l.Name.Contains(":")).ToList();
            if (spots.Count > 0)
            {
                var l = spots[Random.Range(0, spots.Count)];
                var (x, y) = TerrainMap.FindNearestEnterable(l.X, l.Y);
                p.X = x; p.Y = y; Rt.SetOffset(p, 0f, 0f);
                GameLog.Write($"[SURVIVAL] {l.Name}. The first wave comes in {Breather:0} s.");
            }
            NextAt = T + Breather;
        }

        public static void Stop() { On = false; Live.Clear(); NextAt = -1f; }

        /// <summary>Every frame (Rt.WorldTick).</summary>
        public static void Tick(Player p)
        {
            if (!On || p == null) return;
            float now = T;
            int before = Live.Count;
            bool bossDown = BossWave && Live.Count > 0 && Live.All(e => !e.IsAlive());
            Live.RemoveAll(e => !e.IsAlive() || !EncounterState.Enemies.Contains(e));
            Kills += before - Live.Count;
            if (BossWave && (bossDown || Live.Count == 0) && WaveStarted >= 0f)
            {
                if (Tier < MaxTier) Tier++;
                GameLog.Write($"[SURVIVAL] The boss is down. Tier {Tier}: the waves get harder.");
                BossWave = false; WaveStarted = -1f; NextAt = now + Breather;
            }
            else if (!BossWave && Live.Count == 0 && WaveStarted >= 0f && NextAt < 0f) NextAt = now + Breather;   // cleared: a breather
            if (!BossWave && WaveStarted >= 0f && now - WaveStarted >= MaxWave && (NextAt < 0f || NextAt > WaveStarted + MaxWave)) NextAt = WaveStarted + MaxWave;   // a minute at most
            if (NextAt >= 0f && now >= NextAt) StartWave(p);
        }

        public static int WaveSize(int wave) => FirstWave + PerWave * (wave - 1);
        public static bool IsBossWave(int wave) => wave % BossEvery == 0;

        static void StartWave(Player p)
        {
            Wave++; NextAt = -1f; WaveStarted = T; BossWave = IsBossWave(Wave);
            EncounterState.InCombat = true;
            if (BossWave)
            {
                if ((Wave / BossEvery) % 2 == 1)
                {
                    var h = Human(p, HumanFactions[Random.Range(0, HumanFactions.Length)], RankFor(Tier, true), true);
                    if (h != null) { SquadManager.MakeBoss(h); h.LoadApRounds(); GameLog.Write($"[SURVIVAL] Wave {Wave}: {h.Name}."); }
                }
                else
                {
                    var defs = MutantsOf(Mathf.Min(Tier + 2, MaxTier));
                    var m = defs.Count > 0 ? Mutant(p, defs.OrderByDescending(d => d.Hp).First()) : null;
                    if (m != null) { m.MakeNamed("The Apex"); GameLog.Write($"[SURVIVAL] Wave {Wave}: {m.Name}."); }
                }
                return;
            }
            int n = WaveSize(Wave);
            if (Wave % 2 == 1)
            {
                string faction = HumanFactions[Random.Range(0, HumanFactions.Length)];
                for (int i = 0; i < n; i++) Human(p, faction, RankFor(Tier, i == 0), i == 0);
                GameLog.Write($"[SURVIVAL] Wave {Wave}: {n} {faction}.");
            }
            else
            {
                var defs = MutantsOf(Tier);
                for (int i = 0; i < n && defs.Count > 0; i++) Mutant(p, defs[Random.Range(0, defs.Count)]);
                GameLog.Write($"[SURVIVAL] Wave {Wave}: {n} mutants.");
            }
        }

        /// <summary>A rank of the tier: tier t draws from the two ranks around it (Rookie/Experienced at 1 ... Master/Legend at 6); a leader the higher one.</summary>
        public static string RankFor(int tier, bool leader)
        {
            int lo = Mathf.Clamp(tier - 1, 0, CombatRules.RankOrder.Length - 2);
            return CombatRules.RankOrder[leader || Random.value < 0.35f ? lo + 1 : lo];
        }

        static List<MutantDef> MutantsOf(int tier)
        {
            for (int t = tier; t >= 1; t--)
                if (MutantDatabase.Tiers.TryGetValue("tier" + t, out var d) && d.Count > 0) return d.Values.ToList();
            return new List<MutantDef>();
        }

        static HumanEnemy Human(Player p, string faction, string rank, bool leader)
        {
            var spot = Spot(p); if (!spot.HasValue) return null;
            var (w, a) = EncounterGeneration.PickFactionLoadout(faction, rank, TestingGround.TierOf(rank), isLeader: leader);
            var h = new HumanEnemy($"{faction} {rank}", spot.Value.x, spot.Value.y, faction, rank, w, a) { IsAggroed = true };
            HumanLootSystem.RollSpawnStash(h, TestingGround.TierOf(rank));
            EncounterState.Enemies.Add(h); Live.Add(h);
            return h;
        }

        static Mutant Mutant(Player p, MutantDef def)
        {
            var spot = Spot(p); if (!spot.HasValue) return null;
            var m = EncounterGeneration.CreateMutantFromDb(def, spot.Value.x, spot.Value.y); m.IsAggroed = true;
            EncounterState.Enemies.Add(m); Live.Add(m);
            return m;
        }

        /// <summary>A free, walkable tile 16-24 tiles from you.</summary>
        static (int x, int y)? Spot(Player p)
        {
            for (int tries = 0; tries < 60; tries++)
            {
                float a = Random.value * Mathf.PI * 2f, d = Random.Range(16f, 24f);
                int x = p.X + Mathf.RoundToInt(Mathf.Cos(a) * d), y = p.Y + Mathf.RoundToInt(Mathf.Sin(a) * d);
                if (!TerrainMap.CanEnterTile(x, y) || EncounterState.Enemies.Any(e => e.X == x && e.Y == y)) continue;
                return (x, y);
            }
            return null;
        }

        /// <summary>The HUD line.</summary>
        public static string Status()
        {
            if (!On) return null;
            float next = NextAt >= 0f ? Mathf.Max(0f, NextAt - T) : -1f;
            string when = BossWave ? "BOSS" : next >= 0f ? $"NEXT IN {next:0}s" : WaveStarted >= 0f ? $"NEXT IN {Mathf.Max(0f, WaveStarted + MaxWave - T):0}s" : "";
            return $"WAVE {Wave}   TIER {Tier}   LEFT {Live.Count(e => e.IsAlive())}   KILLS {Kills}   {when}";
        }
    }
}
