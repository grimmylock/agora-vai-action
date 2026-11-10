// 2026-11-10 (Yan, on the Secret Cave render: "add a corner where we'll spawn a mutant to give the player a scare ... on the top big corner a higher tier mutant, 1 tier above
// the area"): authored lairs. A lair is a landmark in a dungeon's plan (a niche off a tunnel, a den); the first time you enter the map one mutant is placed there, unaware,
// holding its ground (the usual detection fills before it comes for you). A niche mutant is the zone's tier, a den's is TierPlus above. Once per world save (a story flag), so
// a lair you cleared stays cleared. These are authored placements, not rolls near the player (CLAUDE.md rule 7): the mutant is an ordinary offline squad of one.
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class DungeonLairs
    {
        class Lair { public string Zone, Landmark; public int TierPlus; }
        static readonly Lair[] All =
        {
            new Lair { Zone = "secret_cave", Landmark = "Dark Niche West", TierPlus = 0 },
            new Lair { Zone = "secret_cave", Landmark = "Dark Niche South", TierPlus = 0 },
            new Lair { Zone = "secret_cave", Landmark = "Dark Niche East", TierPlus = 0 },
            new Lair { Zone = "secret_cave", Landmark = "Mutant Den", TierPlus = 1 },
        };

        static string Flag(Lair l) => "lair:" + l.Zone + ":" + l.Landmark;

        /// <summary>The landmark names of a zone's lairs (tests and the examine text).</summary>
        public static IEnumerable<string> LandmarksOf(string zone) => All.Where(l => l.Zone == zone).Select(l => l.Landmark);
        public static int TierPlusOf(string zone, string landmark) => All.FirstOrDefault(l => l.Zone == zone && l.Landmark == landmark)?.TierPlus ?? 0;

        /// <summary>Called when a map is entered (ZoneTravel): places each lair's mutant the first time.</summary>
        public static void OnEnterZone(ZoneDef zone)
        {
            if (zone == null || MacroState.Current == null) return;
            bool any = false;
            foreach (var l in All.Where(x => x.Zone == zone.Id))
            {
                var flags = MacroState.Current.StoryFlags;
                if (flags.Contains(Flag(l))) continue;
                var lm = TerrainMap.Landmarks?.FirstOrDefault(q => q.Name == l.Landmark);
                if (lm == null) continue;
                flags.Add(Flag(l));
                int t = Mathf.Clamp(zone.Tier + l.TierPlus, 1, MutantDatabase.MaxTier);
                var db = EncounterGeneration.PickRandomMutantByTier(t, t) ?? EncounterGeneration.PickRandomMutantByTier(1, t);
                var sq = SquadManager.CreateOfflineSquad(zone.Id, "Mutant", 1);
                if (sq == null) continue;
                var m = EncounterGeneration.CreateMutantFromDb(db, lm.X, lm.Y);
                m.IsAggroed = false;
                sq.Members.Clear(); sq.Members.Add(m); sq.Job = "hold";
                if (!EncounterState.Enemies.Contains(m)) EncounterState.Enemies.Add(m);
                any = true;
            }
            if (any) SquadManager.PositionSquadsInZone(zone.Id);
        }
    }
}
