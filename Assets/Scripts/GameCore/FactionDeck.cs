// Batch 52: real rank decks. Yan (decision card): "Real decks". Each faction has a fixed roster (FactionDef.Roster)
// split into rank cards by its DeckWeights - Obelisk's 24 people include exactly one Legend. A new squad member is a
// card drawn from what's free: the roster, minus everyone of that faction and rank alive in the world, minus cards
// of the recently dead (they come back after CooldownHours: a day for the faction to train or hire a replacement). A
// faction whose deck is empty sends nobody. Mutants aren't a deck: they breed, and zones refill them by quota.
// Quest targets aren't drawn either (a job's target is a specific person), but while alive they hold their card.
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class FactionDeck
    {
        public const int CooldownHours = 24;
        static readonly string[] Order = { "Rookie", "Experienced", "Professional", "Veteran", "Expert", "Master", "Legend" };
        static Dictionary<string, int> _last;   // faction|rank -> alive at the last tick; not saved (the first tick after a load is a baseline)

        // Batch 83 (Yan: "north uses it's own deck, when a squad from the (north) deck spawns they immediatly go up north to tier
        // 4 to 6 areas"): two decks per faction. The south deck holds Rookie..Veteran (Tiers 1-3), the north deck
        // Professional..Legend; a north card is written "N:Master". Both come from the same weights, so inside each deck every
        // rank keeps the faction's share. Squads made for a Tier 4+ zone draw from the north deck (SquadManager.CreateOfflineSquad).
        public const string NorthPrefix = "N:";
        public static float NorthScale = 4f;
        static int RankIdx(string rank) => System.Array.IndexOf(Order, rank);
        public static bool InDeck(string rank, bool north) => north ? RankIdx(rank) >= 2 : RankIdx(rank) >= 0 && RankIdx(rank) <= 3;
        public static string Card(string rank, bool north) => north ? NorthPrefix + rank : rank;

        /// <summary>How many cards the faction's deck holds of `card` (a rank, or "N:rank" for the north deck): its weight's share of
        /// the roster times the deck's scale, at least 1 if the weight isn't 0; 0 for a rank outside that deck.</summary>
        public static int Cards(string faction, string card)
        {
            bool north = card.StartsWith(NorthPrefix);
            string rank = north ? card.Substring(NorthPrefix.Length) : card;
            return InDeck(rank, north) ? Cards(faction, rank, north ? NorthScale : DeckScale) : 0;
        }

        /// <summary>Batch 80 (Yan: "increase the faction card deck in a way that fills the world but keeps the same % for each
        /// tier of units"): every deck is its roster times this, so each rank keeps its share. At 1 the decks ran dry at the
        /// first seeding (Garrison, Obelisk, Contractors) and the Reactor Core held 1 squad of 6.</summary>
        public static float DeckScale = 3f;

        static int Cards(string faction, string rank, float scale)
        {
            if (!FactionDatabase.Factions.TryGetValue(faction, out var f) || !f.DeckWeights.TryGetValue(rank, out var w) || w <= 0) return 0;
            int total = f.DeckWeights.Values.Sum();
            return System.Math.Max(1, (int)System.Math.Round(f.Roster * scale * w / (double)total));
        }

        /// <summary>How many Veteran-or-better people the faction can field at once (decides how far north its outpost is).
        /// Batch 80: from the roster itself, so the bigger decks don't move anyone's outpost (Yan's faction tiers).</summary>
        public static int Strength(string faction) => Order.Skip(3).Sum(r => Cards(faction, r, 1f));   // the roster's own split, not the decks

        public static Dictionary<string, int> Alive(IEnumerable<Mutant> extra = null)
        {
            var seen = new HashSet<Mutant>(); var n = new Dictionary<string, int>();
            foreach (var u in MacroState.Current.OfflineSquads.SelectMany(s => s.Members).Concat(EncounterState.Enemies).Concat(extra ?? Enumerable.Empty<Mutant>()))
                if (u is HumanEnemy h && h.IsAlive() && seen.Add(h)) { var k = h.FactionName + "|" + (h.Card ?? h.Rank); /* Batch 53: the card it was drawn as, not the rank it grew into */ n[k] = (n.TryGetValue(k, out var c) ? c : 0) + 1; }
            return n;
        }

        static int Cooling(string key) => MacroState.Current.DeckCooling.Count(c => c.StartsWith(key + "|"));

        public static int Free(string faction, string card, Dictionary<string, int> alive = null)
        {
            alive ??= Alive(); var k = faction + "|" + card;
            return System.Math.Max(0, Cards(faction, card) - (alive.TryGetValue(k, out var a) ? a : 0) - Cooling(k));
        }

        /// <summary>Can the faction still field a leader from that deck (a Tier 4+ leader for the north)?</summary>
        public static bool Any(string faction, bool north = false) { var alive = Alive(); return Order.Any(r => LeaderOk(r, north) && Free(faction, Card(r, north), alive) > 0); }

        /// <summary>North squads are led by an Expert or better (a Tier 4+ rank); any rank may lead in the south.</summary>
        public static bool LeaderOk(string rank, bool north) => InDeck(rank, north) && (!north || SquadManager.RankTier(rank) >= 4);

        /// <summary>Batch 83 (Yan: "leaders need to be the higher rank in a squad, members of his squad can be up to 2 tiers below
        /// him"): who may join a squad led by `lead`.</summary>
        public static bool MemberOk(string rank, string lead) =>
            RankIdx(rank) <= RankIdx(lead) && SquadManager.RankTier(rank) >= SquadManager.RankTier(lead) - 2;

        /// <summary>Draws a free card of the faction's deck at random among the ranks `ok` allows (so the odds follow what's
        /// left); returns the rank, or null when there's none.</summary>
        public static string Draw(string faction, IEnumerable<Mutant> squadSoFar, bool north, System.Func<string, bool> ok)
        {
            var alive = Alive(squadSoFar);
            var pool = Order.Where(r => InDeck(r, north) && ok(r)).SelectMany(r => Enumerable.Repeat(r, Free(faction, Card(r, north), alive))).ToList();
            if (pool.Count == 0) return null;
            var rank = pool[Random.Range(0, pool.Count)];
            if (_last != null) { var k = faction + "|" + Card(rank, north); _last[k] = (_last.TryGetValue(k, out var c) ? c : 0) + 1; }
            return rank;
        }

        /// <summary>A card drawn but put straight back (SquadManager's redraw): it isn't a death, so no cooling card.</summary>
        public static void Undraw(string faction, string card)
        {
            var k = faction + "|" + card;
            if (_last != null && _last.TryGetValue(k, out var c)) _last[k] = System.Math.Max(0, c - 1);
        }

        /// <summary>Hourly (SquadManager.TickHour): whoever is gone since the last tick puts a cooling card down, and cooling cards count down.</summary>
        public static void Tick()
        {
            var m = MacroState.Current;
            m.DeckCooling = m.DeckCooling.Select(c => { var p = c.Split('|'); return $"{p[0]}|{p[1]}|{int.Parse(p[2]) - 1}"; }).Where(c => !c.EndsWith("|0")).ToList();
            var alive = Alive();
            if (_last != null)
                foreach (var kv in _last)
                    for (int i = alive.TryGetValue(kv.Key, out var a) ? a : 0; i < kv.Value; i++) m.DeckCooling.Add($"{kv.Key}|{CooldownHours}");
            _last = alive;
        }

        public static void Reset() => _last = null;
    }
}
