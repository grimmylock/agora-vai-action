using System.Collections.Generic;
using System.Linq;
using System.IO;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class SquadManager
    {
        private static bool _initialized = false;

        public static IEnumerable<OfflineSquad> GetSquadsInZone(string zoneId)
        {
            EnsureInitialized();
            return MacroState.Current.OfflineSquads.Where(s => s.ZoneId == zoneId);
        }

        public static IEnumerable<OfflineSquad> GetHostileSquadsToPlayer(string zoneId)
        {
            return GetSquadsInZone(zoneId)
                .Where(s => FactionDatabase.AreHostile(s.Faction, "Player"));
        }

        public static OfflineSquad CreateOfflineSquad(string zoneId, string faction, int count = 3)
        {
            EnsureInitialized();
            faction = FactionDatabase.NormalizeFactionName(faction);
            var zone = MacroMapDatabase.Find(zoneId) ?? new ZoneDef { Id = zoneId };
            if (faction == "Raiders" && zone.Tier <= 1) count = System.Math.Min(count, MaxStarterGang);   // Batch 73
            var squad = new OfflineSquad
            {
                ZoneId = zoneId,
                Faction = faction,
                WorldX = TerrainMap.Width / 2,
                WorldY = TerrainMap.Height / 2,
                IsActiveCombat = false,
                IsHostileToPlayer = FactionDatabase.AreHostile(faction, "Player"),
                PersistFullState = false,
                LastUpdatedTick = MacroState.Current.WorldTickCounter,
            };

            for (int i = 0; i < count; i++)
            {
                int x = System.Math.Min(System.Math.Max(0, squad.WorldX + (i % 3) - 1), TerrainMap.Width - 1);
                int y = System.Math.Min(System.Math.Max(0, squad.WorldY + (i / 3) - 1), TerrainMap.Height - 1);

                if (faction == "Mutant")
                {
                    // Fix: hardcoded "5" as the upper tier bound - for zone.Tier==7 that made
                    // min(6) > max(5), an empty pool, and a silent default-stat 30HP "Mutant"
                    // fallback (PickRandomMutantByTier returns null on an empty pool, and
                    // CreateMutantFromDb treats null as "spawn a generic Mutant"). Currently
                    // unreachable (no Tier 6/7 zone lists "Mutant" in AllowedFactions yet) but would
                    // silently produce garbage the moment one does - clamp against the database's
                    // real max tier instead of a stale hardcoded number.
                    int minTier = System.Math.Min(MutantDatabase.MaxTier, System.Math.Max(1, zone.Tier - 1));
                    int maxTier = System.Math.Min(MutantDatabase.MaxTier, zone.Tier + 1);
                    var db = EncounterGeneration.PickRandomMutantByTier(minTier, maxTier);
                    var member = EncounterGeneration.CreateMutantFromDb(db, x, y);
                    member.IsAggroed = false;
                    squad.Members.Add(member);
                }
                else
                {
                    // Batch 52: a real card, or nobody left. Batch 83: a Tier 4+ zone draws from the north deck, leader first (an
                    // Expert or better), then his rank or below and at most 2 tiers under him. Batch 84: the south draws the whole
                    // squad and the best card leads (leader-first made the south mostly Rookies); see the fix-up below.
                    bool north = zone.Tier >= 4;
                    var lead = squad.Members.OfType<HumanEnemy>().FirstOrDefault();
                    var rank = !north ? FactionDeck.Draw(faction, squad.Members, false, r => true)
                             : lead == null ? FactionDeck.Draw(faction, squad.Members, true, r => FactionDeck.LeaderOk(r, true))
                             : FactionDeck.Draw(faction, squad.Members, true, r => FactionDeck.MemberOk(r, lead.Rank));
                    if (rank == null) break;
                    squad.Members.Add(NewMember(faction, rank, north, x, y, zone));
                }
            }
            // Batch 84 (Yan: "members of his squad can be up to 2 tiers below him"): in the south, anyone more than 2 tiers under
            // the best card is redrawn (or dropped if there's nobody fitting left)
            var best = squad.Members.OfType<HumanEnemy>().OrderByDescending(h => CombatRules.RankIndex(h.Rank)).FirstOrDefault();
            if (best != null && zone.Tier < 4)
                foreach (var h in squad.Members.OfType<HumanEnemy>().Where(h => !FactionDeck.MemberOk(h.Rank, best.Rank)).ToList())
                {
                    squad.Members.Remove(h); FactionDeck.Undraw(faction, h.Card);
                    var rank = FactionDeck.Draw(faction, squad.Members, false, r => FactionDeck.MemberOk(r, best.Rank));
                    if (rank != null) squad.Members.Add(NewMember(faction, rank, false, h.X, h.Y, zone));
                }

            if (squad.Members.Count == 0) return null;
            Arm(squad);   // Batch 80
            MacroState.Current.OfflineSquads.Add(squad);
            GameLog.Write($"[SQUAD] A {faction} force takes position in {zone?.Name ?? zoneId}.");
            SaveOfflineSquadsToDisk();
            return squad;
        }

        static HumanEnemy NewMember(string faction, string rank, bool north, int x, int y, ZoneDef zone)
        {
            var (min, max) = EncounterGeneration.RankToTierRange(rank);
            var (w, a) = EncounterGeneration.PickFactionLoadout(faction, rank, zone?.Tier ?? 1);
            var member = new HumanEnemy($"{faction} Operative", x, y, faction, rank, w, a) { Card = FactionDeck.Card(rank, north) };
            member.IsAggroed = false;
            HumanLootSystem.RollSpawnStash(member, max);
            return member;
        }

        /// <summary>New: gives a zone its initial hostile population the first time it's ever
        /// entered - the offline-squad system already existed (migration, offscreen skirmishes,
        /// disk persistence) but nothing ever called CreateOfflineSquad to seed a zone with one.
        /// Safe zones stay empty. Tier-5+ zones (BaseHazardModifier >= 4) spawn already-aggroed,
        /// matching 06_Zone_Map.md's "hostile encounter guaranteed on arrival" rule for those
        /// zones - everywhere else, squads start unaggroed and are found (or avoided) by walking
        /// into their vision range, same as TurnLoop's existing aggro/wander logic already does.</summary>
        public static void EnsureZonePopulated(ZoneDef zone)
        {
            EnsureInitialized();
            if (zone == null || zone.IsSafeZone) return;
            if (MacroState.Current.PopulatedZones.Contains(zone.Id)) return;
            MacroState.Current.PopulatedZones.Add(zone.Id);

            for (int i = 0, n = Quota(zone); i < n; i++)
                for (int tries = 0; tries < 4; tries++) { var s = Spawn(zone); if (s == null || !Settle(s, true)) break; }   // Batch 76
            FillIfEmpty(zone);
            PositionSquadsInZone(zone.Id);
        }

        /// <summary>Batch 124: four draws in a row can all be led by cards that settle in a harder tier and leave a zone
        /// empty; keep drawing until one stays (Batch 41: every wild zone starts populated).</summary>
        static void FillIfEmpty(ZoneDef zone)
        {
            for (int tries = 0; tries < 12 && !MacroState.Current.OfflineSquads.Any(q => q.ZoneId == zone.Id); tries++)
            {
                var s = Spawn(zone);
                if (s == null) break;
                Settle(s, true);
            }
        }

        /// <summary>How many squads a zone holds when full: base hazard/2 + 1, plus one per ~12k tiles (Batch 30: the maps
        /// are 3x bigger). Batch 43: the zone's fixed base hazard, so events and goals don't conjure squads by changing a number.</summary>
        public const int MaxSquadsPerZone = 6;
        static int Quota(ZoneDef zone)
        {
            int n = Mathf.Max(1, zone.BaseHazardModifier / 2 + 1);
            n += TerrainMap.RegionArea(zone.Id) / 12000;   // the map overhaul: a region's share of its tier map
            return System.Math.Min(n, MaxSquadsPerZone);   // Batch 44: never more than the prune keeps
        }

        /// <summary>One new squad of a faction the zone allows (or the one given), equipped for that zone's tier.
        /// Batch 43: no squad starts already aggroed any more (it was "hazard 4+ = guaranteed encounter", a tabletop
        /// rule); they find you by vision like everyone else.</summary>
        /// <summary>Batch 73 (Yan: "change raider gangs in the starting area to be a max of 3"): a Tier 1 Raider gang is
        /// at most 3 strong, and a bigger gang from further in doesn't roam down into Tier 1.</summary>
        public const int MaxStarterGang = 3;
        static bool FitsZone(OfflineSquad squad, string zoneId) =>
            (squad.Faction != "Raiders" || (MacroMapDatabase.Find(zoneId)?.Tier ?? 1) > 1 || squad.Members.Count(m => m.IsAlive()) <= MaxStarterGang)
            && InBand(squad, zoneId);

        /// <summary>Batch 83 (Yan: "their objectives in a-life need to be all in high tier areas ... lower tiered squads need
        /// objectives in the lower tiered areas"): a squad led from Tier 4 up only goes to Tier 4+ regions, the rest only to
        /// Tiers 1-3. Mutants go anywhere. (Walking through on the way to an objective is fine - routes aren't limited.)</summary>
        public static bool InBand(OfflineSquad squad, string zoneId)
        {
            int home = HomeTier(squad);
            return home == 0 || (home >= 4) == ((MacroMapDatabase.Find(zoneId)?.Tier ?? 1) >= 4);
        }

        // ------------------------------------------------------------------ Batch 76: squads live where their leader belongs
        // Yan: "make it so enemy ranks move to tiered areas based on the leaders tier, and the leader is always the higher
        // tier. So if a garrison squad spawns with a master tier leader they immediatly move to a tier 5 area, and vice-versa".
        // A faction's deck draws ranks for the whole Zone, so a squad seeded in Tier 2 could be led by a Master. Now the
        // squad's tier is its best rank's (EnemyAI.Leader is already the best rank): seeding and refills send it to a
        // region of that tier its faction may hold, re-kitted for that tier. Off your map it moves at once; on your map it
        // walks there.

        public static int RankTier(string rank) => rank switch
        {
            "Rookie" => 1, "Experienced" => 1, "Professional" => 2, "Veteran" => 3, "Expert" => 4, "Master" => 5, "Legend" => 6, _ => 1,
        };

        /// <summary>The highest tier a leader of this rank may also hold: Masters keep Tier 5 or 6, Legends 6 or 7 (else Tier 6 would
        /// only ever hold the odd Legend's squad).</summary>
        public static int RankTierMax(string rank) => rank == "Master" ? 6 : rank == "Legend" ? 7 : RankTier(rank);

        /// <summary>The tier a human squad belongs in: its leader's (the best rank in it). 0 for mutants.</summary>
        public static int HomeTier(OfflineSquad s)
        {
            var hs = s.Members.OfType<HumanEnemy>().Where(m => m.IsAlive()).ToList();
            return hs.Count == 0 ? 0 : hs.Max(m => RankTier(m.Rank));
        }

        /// <summary>A region of `tier` (or the nearest tier that has one) the squad's faction may hold, with room.</summary>
        static ZoneDef TierZone(OfflineSquad s, int tier, bool seeding = false)
        {
            for (int d = 0; d <= 6; d++)
                foreach (int t in new[] { tier - d, tier + d }.Distinct())
                {
                    // Batch 79: while seeding, only regions still under their quota take a newcomer (else the decks run dry)
                    var options = Wild.Where(z => z.Tier == t && CanEnter(z.Id, s.Faction) && FitsZone(s, z.Id) && Staying(z.Id) < (seeding ? Quota(z) : MaxSquadsPerZone)).ToList();
                    if (options.Count > 0) return options[Random.Range(0, options.Count)];
                }
            return null;
        }

        /// <summary>Sends a freshly made squad to where its leader belongs. False = it already belongs here.</summary>
        public static bool Settle(OfflineSquad s, bool seeding = false)
        {
            int tier = HomeTier(s);
            var here = MacroMapDatabase.Find(s.Heading ?? s.ZoneId);
            var lead = s.Members.OfType<HumanEnemy>().Where(m => m.IsAlive()).OrderByDescending(m => CombatRules.RankIndex(m.Rank)).FirstOrDefault();
            int top = lead == null ? tier : RankTierMax(lead.Rank);
            if (tier == 0 || here == null || (here.Tier >= tier && here.Tier <= top) || here.Tier >= 7) return false;   // the Reactor Core keeps its garrison (the story needs it held)
            var dest = TierZone(s, tier, seeding);
            if (seeding && dest == null) { MacroState.Current.OfflineSquads.Remove(s); return true; }   // Batch 79: nowhere of its tier has room: it never forms (its cards stay in the deck)
            if (dest == null || dest.Tier == here.Tier || (here.Tier >= tier && here.Tier <= top)) return false;
            foreach (var h in s.Members.OfType<HumanEnemy>())          // kitted out for where it's going
            {
                var (w, a) = EncounterGeneration.PickFactionLoadout(s.Faction, h.Rank, dest.Tier);
                if (w != null) h.EquipWeaponFromDb(w);
                if (a != null) h.EquipArmorFromDb(a);
            }
            FitSize(s, dest);   // Batch 78
            // on (or into) your map it walks; elsewhere, or while the world is first seeded, it's simply there
            bool yours = OnMap(s.ZoneId, TerrainMap.ZoneId) || OnMap(dest.Id, TerrainMap.ZoneId);
            if (yours && !seeding) s.Heading = dest.Id;
            else { s.ZoneId = dest.Id; s.Heading = null; }
            return true;
        }

        /// <summary>How many people a new human squad has. Batch 55: Raiders come in gangs of 4-6 (Batch 73: at most 3 in
        /// Tier 1). Batch 78 (Yan): from Tier 4 every faction moves in 4-6; Garrison in Tier 2 only sends duos out around
        /// their base (Yan: "the don't come near me faction" - their real squads settle up north, Batch 76). Else 2-4.</summary>
        public static int HumanSquadSize(string faction, int tier) =>
            faction == "Garrison" && tier <= 2 ? 2
            : faction == "Raiders" && tier == 2 ? 3 + Random.Range(0, 2)   // Batch 79 (Yan): Tier 2 Raiders 3-4
            : faction == "Raiders" || tier >= 3 ? 4 + Random.Range(0, 3)   // Batch 80 (Yan): Tier 3 squads 4-6 too
            : 2 + Random.Range(0, 3);

        /// <summary>Batch 78: a squad that settled in a tier where squads run bigger fills up to that size with more of its
        /// faction (never someone who outranks its leader, so it stays where it belongs); one going down to Garrison's
        /// Tier 2 patrol keeps only a duo. Cards come from the deck as usual.</summary>
        static void FitSize(OfflineSquad s, ZoneDef dest)
        {
            var lead = s.Members.OfType<HumanEnemy>().Where(m => m.IsAlive()).OrderByDescending(m => CombatRules.RankIndex(m.Rank)).FirstOrDefault();
            if (lead == null) return;
            if (s.Faction == "Garrison" && dest.Tier <= 2)
            {
                foreach (var extra in s.Members.Where(m => m != lead).Skip(1).ToList()) s.Members.Remove(extra);
                return;
            }
            int want = s.Faction == "Raiders" && dest.Tier <= 1 ? 0             // Batch 73 (Yan): a Tier 1 gang is at most 3 - never padded back to 4
                : (s.Faction == "Raiders" || dest.Tier >= 3) ? 4 : 0;
            for (int tries = 0; s.Members.Count(m => m.IsAlive()) < want && tries < 12; tries++)
            {
                bool north = dest.Tier >= 4;   // Batch 83: the deck of where it's going, members within 2 tiers under the leader
                var rank = FactionDeck.Draw(s.Faction, s.Members, north, r => FactionDeck.MemberOk(r, lead.Rank));
                if (rank == null) break;
                var (w, a) = EncounterGeneration.PickFactionLoadout(s.Faction, rank, dest.Tier);
                var member = new HumanEnemy($"{s.Faction} Operative", lead.X, lead.Y, s.Faction, rank, w, a) { Card = FactionDeck.Card(rank, north) };
                HumanLootSystem.RollSpawnStash(member, EncounterGeneration.RankToTierRange(rank).Item2);
                s.Members.Add(member);
            }
            Arm(s);
        }

        // Batch 80 (Yan: "bosses more likely to spawn and they have a 50% chance of using ap ammo, tier 4 to 5 normal npc's have
        // a 5 to 15% (based on rank) to use ap ammo as well"; Batch 82: "remove the cap of 2 per squad"). Rolled once per squad (boss)
        // and once per member (rounds), by the tier the squad's leader belongs to - where it settles (Settle).
        public static readonly float[] BossChanceByTier = { 0f, 0f, 0.05f, 0.08f, 0.15f, 0.25f, 0.25f, 0.25f };
        static readonly string[] Callsigns = { "Ironjaw", "Ghostlight", "Rasp", "Cinder", "Hollowman", "Saltfinger", "Quietus", "Brickback", "Nightjar", "Splinter", "Vesper", "Tallow", "Gravel", "Old Static", "Moth", "Sixpence" };

        public static void Arm(OfflineSquad s)
        {
            int tier = HomeTier(s);
            var humans = s.Members.OfType<HumanEnemy>().Where(h => h.IsAlive()).ToList();
            if (tier == 0 || humans.Count == 0) return;
            if (!s.BossRolled)
            {
                s.BossRolled = true;
                var lead = humans.OrderByDescending(h => CombatRules.RankIndex(h.Rank)).First();
                if (Random.value < BossChanceByTier[System.Math.Min(tier, BossChanceByTier.Length - 1)]) MakeBoss(lead);
            }
            foreach (var h in humans.OrderByDescending(h => h.Named))
            {
                if (h.ArmRolled) continue;
                h.ArmRolled = true;
                if (h.EquippedWeapon == null || CombatRules.IsMelee(h.EquippedWeapon)) continue;   // a blade has no rounds
                float chance = h.Named ? 0.5f : tier >= 4 ? (5f + 10f * CombatRules.RankIndex(h.Rank) / 6f) / 100f : 0f;   // Rookie 5% .. Legend 15%
                if (Random.value < chance) { h.ApRounds = true; h.LoadApRounds(); }   // Batch 82 (Yan): no cap per squad
            }
        }

        /// <summary>A boss: a callsign, and the player's AP rules (CombatRules.NpcAp). Its perks are still to be designed.</summary>
        public static void MakeBoss(HumanEnemy h)
        {
            h.MakeBoss(Callsigns[Random.Range(0, Callsigns.Length)]);
        }

        static OfflineSquad Spawn(ZoneDef zone, string faction = null)
        {
            if (faction == null)
            {
                var factions = WorldEvents.Factions(zone).Where(f => f == "Mutant" || f == "Zombie" || FactionDeck.Any(f, zone.Tier >= 4)).ToList();
                if (factions.Count == 0) return null;
                faction = factions[Random.Range(0, factions.Count)];
            }
            if (faction == "Zombie") faction = "Mutant";
            int memberCount = faction == "Mutant" ? 2 + Random.Range(0, Mathf.Max(1, zone.Tier)) : HumanSquadSize(faction, zone.Tier);
            return CreateOfflineSquad(zone.Id, faction, memberCount);
        }

        // ------------------------------------------------------------------ Batch 41: the world fills itself
        // Yan (decision card): "Seed and refill". Every hostile surface zone gets its squads at New Game, not on
        // your first visit, and a zone below its quota gets a new squad about once a day (1/24 an hour), walking
        // in from a neighbour. Your own zone fills only through squads migrating in (Batch 37), so nothing pops in
        // out of thin air on your map.
        public const float RefillChancePerHour = 1f / 24f;

        static IEnumerable<ZoneDef> Wild => MacroMapDatabase.Zones.Where(z => !z.IsSafeZone && z.AllowedFactions.Count > 0 && !ZoneLayoutRecipes.IsInterior(z.Id));

        public static void SeedWorld()
        {
            bool quiet = GameLog.Quiet; GameLog.Quiet = true;
            try
            {
                // Batch 52: a squad per zone per round, so a faction's deck is spread over its ground instead of
                // running out on the first zones in the list
                var zones = Wild.Where(z => MacroState.Current.PopulatedZones.Add(z.Id)).OrderByDescending(z => z.Tier).ToList();   // Batch 80: north first, so the Reactor Core never finds the Obelisk deck drained
                for (int round = 0; round < MaxSquadsPerZone; round++)
                    foreach (var z in zones.Where(z => round < Quota(z) && MacroState.Current.OfflineSquads.Count(q => q.ZoneId == z.Id) < Quota(z)))   // Batch 80: squads that settled here from elsewhere count too
                        for (int tries = 0; tries < 4; tries++) { var s = Spawn(z); if (s == null || !Settle(s, true)) break; }   // Batch 76: led by a Master? off to Tier 5 - and the zone tries again for one of its own
                foreach (var z in zones) FillIfEmpty(z);   // Batch 124
                Checkpoints.Garrison(seeding: true);   // Batch 126: the roads up a tier are held
                foreach (var z in zones) PositionSquadsInZone(z.Id);
            }
            finally { GameLog.Quiet = quiet; }
        }

        // Batch 43: a refill no longer appears inside the zone. The faction musters it at one of its home zones (a base,
        // or a nest for mutants: the zones over the underground labs and caves they crawl out of) and it walks to the zone
        // that needs it, one connected zone at a time, through zones its faction may enter. On the way it fights, gets
        // seen ("news"), and walks in through an exit if its route crosses your zone.
        // Batch 52: one outpost per faction, where its new squads muster before they walk out. Yan: "decided based on the
        // factions' strength (how many high tier units they can have at one time) ... the farther north the stronger".
        // Strength = Veteran-or-better cards in its deck (FactionDeck.Strength); tier is how far north a zone is.
        // Batch 53, Yan's tiers: C (under 5) tier 1-2, B (5-10) tier 3, A (11-17) tier 4-6, S (18+) tier 7.
        // Mutants have nests, not outposts.
        public static readonly Dictionary<string, string[]> Homes = new Dictionary<string, string[]>
        {
            ["Raiders"] = new[] { "scrapyard" },             // C, strength 4, tier 2
            ["Surveyors"] = new[] { "dried_lake" },          // B, 7, tier 3
            ["Wanderers"] = new[] { "scavengers_hub" },      // B, 9, tier 3
            ["Garrison"] = new[] { "hospital" },             // A, 12, tier 6
            ["Contractors"] = new[] { "quarry_basin" },      // A, 13, tier 5
            ["Liberators"] = new[] { "vanguard_barracks" },  // A, 14, tier 4
            ["Sentinels"] = new[] { "crimson_woods" },       // A, 14, tier 4
            ["Obelisk"] = new[] { "reactor_core" },          // S, 25, tier 7
            ["Mutant"] = new[] { "dried_lake", "crimson_woods", "antenna_grid" },
        };
        public static (int min, int max) OutpostTiers(int strength) => strength < 5 ? (1, 2) : strength <= 10 ? (3, 3) : strength <= 17 ? (4, 6) : (7, 7);
        public const float TravelChancePerHour = 1f / 8f;          // a squad with orders crosses a zone about as fast as you do (8 h)

        /// <summary>Zones from `from` to `to` (excluding `from`) through zones the faction may enter, or null if there's no way.</summary>
        public static List<string> Route(string from, string to, string faction, bool roam = false)
        {
            if (from == to) return new List<string>();
            var prev = new Dictionary<string, string> { [from] = null };
            var q = new Queue<string>(); q.Enqueue(from);
            while (q.Count > 0)
            {
                var z = MacroMapDatabase.Find(q.Dequeue());
                if (z == null) continue;
                foreach (var n in z.ConnectedZones)
                {
                    if (prev.ContainsKey(n) || !(roam ? MacroMapDatabase.Find(n) != null : CanEnter(n, faction))) continue;   // Batch 52: travellers may pass through a Safe Zone (no fights there)
                    prev[n] = z.Id;
                    if (n == to)
                    {
                        var path = new List<string>();
                        for (var c = to; c != from; c = prev[c]) path.Insert(0, c);
                        return path;
                    }
                    q.Enqueue(n);
                }
            }
            return null;
        }

        /// <summary>Batch 44: a quest's targets as a real squad in the quest zone. If you're standing in that zone they
        /// walk in through the exit farthest from you instead of appearing out of thin air.</summary>
        public static OfflineSquad PlaceQuestSquad(string zoneId, string faction, List<Mutant> members, string questId)
        {
            EnsureInitialized();
            var squad = new OfflineSquad { ZoneId = zoneId, Faction = faction, Members = members, QuestId = questId, PersistFullState = true,
                IsHostileToPlayer = FactionDatabase.AreHostile(faction, "Player"), LastUpdatedTick = MacroState.Current.WorldTickCounter };
            foreach (var m in members) m.IsAggroed = false;
            MacroState.Current.OfflineSquads.Add(squad);
            if (OnMap(zoneId, TerrainMap.ZoneId) && MacroMapDatabase.SameMap(MacroState.Current.CurrentZoneId, zoneId) && HasRegions())
            {
                // the map overhaul: on a tier map they're somewhere in that region, out of your sight, not at a map edge
                EncounterState.Enemies.AddRange(members); PositionSquadsInZone(zoneId);
            }
            else if (zoneId == MacroState.Current.CurrentZoneId && TerrainMap.ZoneId == zoneId)
            {
                var p = Player.Current;
                var exit = TerrainMap.Exits.Where(e => !NearPlayer(e)).OrderByDescending(e => p == null ? 0 : System.Math.Abs(e.X - p.X) + System.Math.Abs(e.Y - p.Y)).FirstOrDefault();
                if (exit != null) ArriveLive(squad, exit.TargetZoneId);
                else { EncounterState.Enemies.AddRange(members); PositionSquadsInZone(zoneId); }
            }
            SaveOfflineSquadsToDisk();
            return squad;
        }

        /// <summary>Batch 44: send a squad of one of these factions from its home to a zone, for a quest (the Rival crew).
        /// Null if none of them can get there.</summary>
        public static OfflineSquad SendQuestSquad(IEnumerable<string> factions, string zoneId, string questId)
        {
            var zone = MacroMapDatabase.Find(zoneId);
            if (zone == null) return null;
            foreach (var faction in factions.OrderBy(_ => Random.value))
            {
                var home = (Homes.TryGetValue(faction, out var h) ? h : new string[0])
                    .Where(id => !OnMap(id, TerrainMap.ZoneId) && Route(id, zoneId, faction, true) != null).OrderBy(_ => Random.value).FirstOrDefault();
                if (home == null) continue;
                var squad = Spawn(zone, faction);
                if (squad == null) continue;
                squad.ZoneId = home; squad.Heading = home == zoneId ? null : zoneId; squad.QuestId = questId; squad.PersistFullState = true;
                if (squad.Heading != null) WorldEvents.News('M', home, $"{Cap(Who(faction))} set out from {MacroMapDatabase.Find(home)?.Name ?? home}, heading for {zone.Name}.");
                return squad;
            }
            return null;
        }

        /// <summary>The job is over: its squads go back to being ordinary squads.</summary>
        public static void ReleaseQuestSquads(string questId)
        {
            foreach (var s in MacroState.Current.OfflineSquads.Where(s => s.QuestId == questId || s.QuestId == "rival:" + questId)) { s.QuestId = null; s.PersistFullState = false; }
        }

        static string NextHop(OfflineSquad s) => s.Heading == null ? null : Route(s.ZoneId, s.Heading, s.Faction, true)?.FirstOrDefault();   // Batch 52: from its one outpost, through anywhere but a Safe Zone

        // ------------------------------------------------------------------ Batch 45: caravans and mutant migrations travel for real
        /// <summary>A caravan of traders sets out from the Zone's edge (The Perimeter) for a hub. Its extra stock only
        /// arrives if it does; it can be ambushed on the way.</summary>
        public static OfflineSquad SendCaravan(string hubId)
        {
            var hub = MacroMapDatabase.Find(hubId);
            if (hub == null || Route("perimeter", hubId, "Wanderers", true) == null) return null;
            var squad = Spawn(hub, "Wanderers");
            if (squad == null) return null;
            squad.ZoneId = "perimeter"; squad.Heading = hubId == "perimeter" ? null : hubId; squad.Job = "caravan";
            if (squad.Heading == null) OnArrived(squad);
            else WorldEvents.News('M', "perimeter", $"A trade caravan set out from The Perimeter for {hub.Name}.");
            return squad;
        }

        /// <summary>One or two mutant packs come up out of the nearest underground zone (labs, caves, tunnels), cross the given
        /// zone and head down into another one. The underground is where both halves of the map have nests; the Hub splits
        /// the surface in two and nothing roams through a Safe Zone.</summary>
        public static List<OfflineSquad> SendMigration(string throughId)
        {
            var sent = new List<OfflineSquad>();
            var zone = MacroMapDatabase.Find(throughId);
            if (zone == null) return sent;
            var nests = MacroMapDatabase.Zones.Where(z => ZoneLayoutRecipes.IsInterior(z.Id) && !z.Upstairs && !OnMap(z.Id, TerrainMap.ZoneId) && Route(z.Id, throughId, "Mutant", true) != null)
                .Select(z => z.Id).OrderBy(n => Route(n, throughId, "Mutant", true).Count).ThenBy(_ => Random.value).ToList();
            if (nests.Count == 0) return sent;
            string from = nests[0], to = nests.Count > 1 ? nests[1 + Random.Range(0, nests.Count - 1)] : nests[0];
            for (int i = 0, n = 1 + Random.Range(0, 2); i < n; i++)
            {
                var squad = Spawn(zone, "Mutant");
                squad.ZoneId = from; squad.Heading = throughId == from ? to : throughId; squad.Job = "migration:" + to;
                sent.Add(squad);
            }
            WorldEvents.News('M', from, $"Mutants are pouring up out of {MacroMapDatabase.Find(from)?.Name}, heading for {zone.Name}.");
            return sent;
        }

        /// <summary>Batch 46: a faction sends a squad from home to take and hold its goal zone. It roams there (any zone
        /// but a Safe Zone, plus the goal zone itself) and then holds that ground. Null if one is already on the way or
        /// nobody can get there.</summary>
        public static OfflineSquad SendGoalSquad(string faction, string zoneId)
        {
            var zone = MacroMapDatabase.Find(zoneId);
            if (zone == null || MacroState.Current.OfflineSquads.Any(s => s.Faction == faction && s.Heading == zoneId && s.Job == "goal")) return null;
            var home = (Homes.TryGetValue(faction, out var h) ? h : new string[0])
                .Where(id => !OnMap(id, TerrainMap.ZoneId) && id != zoneId && Route(id, zoneId, faction, true) != null).OrderBy(_ => Random.value).FirstOrDefault();
            if (home == null) return null;
            var squad = Spawn(zone, faction);
            if (squad == null) return null;
            squad.ZoneId = home; squad.Heading = zoneId; squad.Job = "goal";
            WorldEvents.News('M', home, $"{Cap(Who(faction))} set out from {MacroMapDatabase.Find(home)?.Name ?? home} to take {zone.Name}.");
            return squad;
        }

        /// <summary>Batch 48: a human squad in a zone with War Spoils lying around (from an off-screen fight) may find and
        /// strip them: the spoils are gone and their contents are in a member's pack (you'll find them on the body).</summary>
        static void Scavenge(OfflineSquad squad, ZoneDef zone)
        {
            var who = squad.Members.OfType<HumanEnemy>().FirstOrDefault(h => h.IsAlive());
            if (who == null || !MacroState.Current.PersistedZoneInteractiveObjects.TryGetValue(MacroMapDatabase.MapOf(zone.Id), out var objects) || objects == null) return;
            var spoils = objects.FirstOrDefault(o => o.Name == "War Spoils");
            if (spoils == null || Random.value > ScavengeChancePerHour) return;
            objects.Remove(spoils);
            HumanLootSystem.TakeSpoils(who, zone.Tier, spoils.Reward?.Ru ?? 0);
            WorldEvents.News('M', zone.Id, $"{Cap(Who(squad.Faction))} picked over the remains of a fight in {zone.Name}.");
        }

        /// <summary>Batch 48: the closest (fewest zones away) other squad of a faction, for an interrogated prisoner to give up.</summary>
        public static OfflineSquad NearestSquad(string faction, string fromZone, Mutant except)
        {
            var dist = new Dictionary<string, int> { [fromZone] = 0 };
            var q = new Queue<string>(); q.Enqueue(fromZone);
            while (q.Count > 0) { var z = q.Dequeue(); foreach (var n in MacroMapDatabase.Find(z)?.ConnectedZones ?? new List<string>()) if (!dist.ContainsKey(n)) { dist[n] = dist[z] + 1; q.Enqueue(n); } }
            return MacroState.Current.OfflineSquads.Where(s => s.Faction == faction && !s.Members.Contains(except) && s.Members.Any(x => x.IsAlive()) && dist.ContainsKey(s.ZoneId))
                .OrderBy(s => dist[s.ZoneId]).FirstOrDefault();
        }

        /// <summary>A squad reached where it was heading.</summary>
        /// <summary>Batch 59: a squad leader's radio call. Squads of the faction already in this zone walk to the fight
        /// (they're on the map, so they arrive in a few turns); one squad in a neighbouring zone sets out to help and
        /// comes in through the exits on the hourly tick, so it takes far longer. Raiders have no discipline: only
        /// those in the same zone answer. A squad that's retreating, on a quest or on another job doesn't come.
        /// Returns how many squads answered.</summary>
        public const int RadioRange = 60;
        public static int CallReinforcements(string faction, string zoneId, int x, int y)
        {
            int answered = 0;
            var spot = TerrainMap.FindNearestEnterable(x, y);
            int D(Mutant m) => System.Math.Max(System.Math.Abs(m.X - x), System.Math.Abs(m.Y - y));
            // Yan (first real-time playtest, swarmed by ~8 Raiders): "radio the specific closest squad for help instead of it being a
            // full AOE call" - the nearest free squad on this map within radio range (~60 tiles) comes, not every one of them
            var nearest = MacroState.Current.OfflineSquads
                .Where(s => s.Faction == faction && OnMap(s.ZoneId, TerrainMap.ZoneId) && s.Job == null && s.QuestId == null && s.LeavingFor == null)
                .Select(s => s.Members.Where(m => m.IsAlive() && !m.IsAggroed && EncounterState.Enemies.Contains(m) && D(m) <= RadioRange).ToList())
                .Where(c => c.Count > 0).OrderBy(c => c.Min(D)).FirstOrDefault();
            if (nearest != null) { foreach (var m in nearest) m.PatrolTarget = spot; return 1; }
            if (faction == "Raiders") return answered;
            var zone = MacroMapDatabase.Find(zoneId);
            var neighbours = zone == null ? new List<string>() : MacroMapDatabase.RegionsOf(zone.Id).SelectMany(r => r.ConnectedZones).Where(z => !OnMap(z, TerrainMap.ZoneId)).ToList();
            var helper = MacroMapDatabase.Zones.Count == 0 || zone == null ? null : MacroState.Current.OfflineSquads.FirstOrDefault(s => s.Faction == faction
                && neighbours.Contains(s.ZoneId) && s.Heading == null && s.Job == null && s.QuestId == null && s.Members.Any(m => m.IsAlive()) && InBand(s, zoneId));
            if (helper != null)
            {
                helper.Heading = zoneId; helper.Job = "backup";
                GameLog.Write($"[RADIO] A {faction} squad in {MacroMapDatabase.Find(helper.ZoneId)?.Name ?? helper.ZoneId} answers: they're on their way.");
                answered++;
            }
            return answered;
        }

        static void OnArrived(OfflineSquad squad)
        {
            if (squad.Job == "retreat" || squad.Job == "backup") squad.Job = null;   // Batch 54: home, licking its wounds; Batch 59: help has arrived
            if (squad.Job == "caravan")
            {
                squad.Job = null;
                var m = MacroState.Current;
                m.CaravanZoneId = squad.ZoneId; m.RolledShopStock.Remove(squad.ZoneId);
                var name = MacroMapDatabase.Find(squad.ZoneId)?.Name ?? squad.ZoneId;
                WorldEvents.News('M', squad.ZoneId, $"A trade caravan rolled into {name}.");
                bool quiet = GameLog.Quiet; GameLog.Quiet = false;
                GameLog.Write($"[ZONE EVENT] A caravan has pulled into {name} - extra stock today.");
                GameLog.Quiet = quiet;
            }
            else if (squad.Job == "goal") squad.Job = "hold";              // Batch 46: dig in
            else if (squad.Job != null && squad.Job.StartsWith("migration:"))
            {
                var nest = squad.Job.Substring("migration:".Length);
                if (squad.ZoneId == nest) squad.Job = null;            // home: an ordinary pack again
                else squad.Heading = nest;                             // crossed the zone; on to the next nest
            }
        }

        static void Refill(string skipZone)
        {
            var all = MacroState.Current.OfflineSquads;
            foreach (var z in Wild.Where(z => !OnMap(z.Id, skipZone) && MacroState.Current.PopulatedZones.Contains(z.Id)).ToList())
            {
                if (Random.value > RefillChancePerHour || all.Count(s => s.ZoneId == z.Id && s.Heading == null || s.Heading == z.Id) >= Quota(z)) continue;
                var factions = WorldEvents.Factions(z).Select(f => f == "Zombie" ? "Mutant" : f).Distinct().ToList();
                if (factions.Count == 0) continue;
                factions = factions.Where(f => f == "Mutant" || FactionDeck.Any(f, z.Tier >= 4)).ToList();   // Batch 52: nobody left to send
                if (factions.Count == 0) continue;
                string faction = factions[Random.Range(0, factions.Count)];
                var home = (Homes.TryGetValue(faction, out var h) ? h : new string[0])
                    .Where(id => !OnMap(id, skipZone) && Route(id, z.Id, faction, true) != null).OrderBy(_ => Random.value).FirstOrDefault();
                if (home == null) continue;                                 // nobody can get there (or the only way out of their base is past you)
                var squad = Spawn(z, faction);                                 // kitted out for where it's going
                if (squad == null) continue;
                squad.ZoneId = home; squad.Heading = home == z.Id ? null : z.Id;
                if (Settle(squad)) continue;                                // Batch 76: its leader belongs in another tier
                var homeName = MacroMapDatabase.Find(home)?.Name ?? home;
                WorldEvents.News('M', home, home == z.Id ? $"{Cap(Who(faction))} has been seen around {homeName}." : $"{Cap(Who(faction))} set out from {homeName}, heading for {z.Name}.");
            }
        }

        /// <summary>New: spreads a zone's living squad members across whatever terrain is
        /// currently loaded for that zone, instead of leaving them stacked wherever
        /// CreateOfflineSquad/TickSquadMigration last placed them (both default to map-center).
        /// Call this whenever a zone becomes the active one - cheap and idempotent, so it also
        /// self-corrects a squad that migrated in off-screen while nobody was there to see it.
        /// Must run while TerrainMap.Tiles is already that zone's terrain (i.e. after
        /// TerrainMap.BuildTerrainForZone).</summary>
        public static void PositionSquadsInZone(string zoneId)
        {
            var occupied = new HashSet<(int, int)>();
            var player = Player.Current;
            if (player != null) occupied.Add((player.X, player.Y));

            bool regions = HasRegions() && OnMap(zoneId, TerrainMap.ZoneId);
            foreach (var squad in GetSquadsInZone(zoneId))
            {
                (int x, int y)? camp = null;     // the map overhaul: a squad on a tier map stands together somewhere in its own region
                foreach (var m in squad.Members)
                {
                    if (!m.IsAlive()) continue;
                    if (TerrainMap.CanEnterTile(m.X, m.Y) && (!regions || TerrainMap.RegionAt(m.X, m.Y) == zoneId) && occupied.Add((m.X, m.Y))) continue;

                    for (int attempts = 0; attempts < 40; attempts++)
                    {
                        (int x, int y) candidate;
                        if (regions)
                        {
                            if (camp == null && squad.Checkpoint != null) camp = Checkpoints.Spot(squad.Checkpoint);   // Batch 126: at its post
                            if (camp == null) camp = TerrainMap.RandomTileIn(zoneId, player?.X ?? -1, player?.Y ?? -1, 24) ?? TerrainMap.RegionCenter(zoneId);
                            candidate = attempts == 0 && occupied.Count == 0 ? camp.Value : TerrainMap.FindNearestEnterable(
                                Mathf.Clamp(camp.Value.x + Random.Range(-2, 3), 0, TerrainMap.Width - 1), Mathf.Clamp(camp.Value.y + Random.Range(-2, 3), 0, TerrainMap.Height - 1));
                        }
                        else candidate = TerrainMap.FindNearestEnterable(Random.Range(0, TerrainMap.Width), Random.Range(0, TerrainMap.Height));
                        if (!occupied.Add(candidate)) continue;
                        m.X = candidate.x;
                        m.Y = candidate.y;
                        break;
                    }
                }
            }
        }

        // Batch 36: A-Life runs on the clock, one tick per game hour (Atmosphere.Advance), instead of once per
        // zone crossing. A crossing is 8 hours on the road, so the old per-crossing odds are spread over 8 ticks:
        // 35% to move on per crossing = 5.2% an hour, 15% to scavenge = 2% an hour.
        public const float MigrateChancePerHour = 0.052f, ScavengeChancePerHour = 0.25f;   // Batch 48: chance an hour to find and strip War Spoils lying in the zone

        /// <summary>One game hour of the off-screen world. The player's own zone is live (its squads are
        /// the enemies on the map), so nothing moves into, out of or fights inside it here.</summary>
        public static void TickHour()
        {
            // the map overhaul: everything on the loaded map is live (all of a tier map's regions); `here` is that map's id
            var here = MacroMapDatabase.SameMap(MacroState.Current.CurrentZoneId, TerrainMap.ZoneId) && !(MacroMapDatabase.RegionsOf(TerrainMap.ZoneId).All(r => r.IsSafeZone)) ? TerrainMap.ZoneId : null;
            GameLog.Quiet = true;     // ponytail: hourly news goes to the Console until it reaches the player as radio chatter and rumors
            try
            {
                TickSquadMigration(here); Refill(here); ResolveOffscreenSquadConflicts(here); FactionDeck.Tick();
                Checkpoints.Garrison();   // Batch 126: a wiped-out checkpoint is taken up again after a while
                if (here != null) { DepartLive(here); UpdateLiveRegions(here); }
                foreach (var s in MacroState.Current.OfflineSquads.Where(s => s.LeavingFor != null && !OnMap(s.ZoneId, here)).ToList()) Leave(s);   // you left before they did
                PruneOldSquads(here);   // the map overhaul: squads that walked off your map this hour don't overfill where they went
            }
            finally { GameLog.Quiet = false; }
        }

        /// <summary>The map overhaul: may a squad of this faction walk onto this tile of a tier map? (Not into a Safe Zone it doesn't belong to.)</summary>
        public static bool MayWalkInto(string faction, int x, int y)
        {
            var z = MacroMapDatabase.Find(TerrainMap.RegionAt(x, y));
            return z == null || !z.IsSafeZone || faction != null && z.AllowedFactions.Contains(faction);
        }

        static bool CanEnter(string zoneId, string faction)
        {
            var z = MacroMapDatabase.Find(zoneId);
            return z != null && (WorldEvents.Factions(z).Count == 0 || WorldEvents.Factions(z).Contains(faction));
        }

        // ------------------------------------------------------------------ Batch 37: walking in and out of the zone you're in
        // Yan: "have squads actually move in world and come in through whatever transition zone they took".

        /// <summary>The current zone's map is loaded and squads may come and go there (not a Safe Zone).</summary>
        /// <summary>The map overhaul: is this zone (a region) drawn on the given map? (Null map: nothing is.)</summary>
        /// <summary>Squads holding a zone (not passing through).</summary>
        static int Staying(string zoneId) => MacroState.Current.OfflineSquads.Count(s => s.ZoneId == zoneId && s.Heading == null);

        public static bool OnMap(string zoneId, string mapId) => mapId != null && zoneId != null && MacroMapDatabase.MapOf(zoneId) == MacroMapDatabase.MapOf(mapId);

        static bool HasRegions() => TerrainMap.ZoneId != null && MapLayoutDatabase.All.TryGetValue(TerrainMap.ZoneId, out var l) && l?.Regions != null;

        /// <summary>A squad walking about a tier map belongs to whichever region its leader is standing in (not a Safe Zone,
        /// nor ground its faction won't go).</summary>
        static void UpdateLiveRegions(string map)
        {
            if (!HasRegions()) return;
            foreach (var squad in MacroState.Current.OfflineSquads.Where(s => OnMap(s.ZoneId, map) && s.LeavingFor == null))
            {
                var lead = squad.Members.FirstOrDefault(m => m.IsAlive() && EncounterState.Enemies.Contains(m));
                if (lead == null) continue;
                string r = TerrainMap.RegionAt(lead.X, lead.Y);
                if (r == squad.ZoneId || r == null || !OnMap(r, map) || (MacroMapDatabase.Find(r)?.IsSafeZone ?? true) || !CanEnter(r, squad.Faction)) continue;
                if (squad.Heading != r && Staying(r) >= MaxSquadsPerZone) continue;   // a full region: they're just passing
                squad.ZoneId = r;
                if (squad.Heading == r) { squad.Heading = null; OnArrived(squad); }
            }
        }

        /// <summary>A squad that walked across the map into the next region now belongs to it.</summary>
        static void ArriveRegion(OfflineSquad squad)
        {
            if (squad.Heading != squad.LeavingFor && Staying(squad.LeavingFor) >= MaxSquadsPerZone) { squad.LeavingFor = null; return; }   // full up there: they stay put
            GameLog.Write($"[MOVEMENT] {squad.Faction} squad moves on into {MacroMapDatabase.Find(squad.LeavingFor)?.Name ?? squad.LeavingFor}.");
            squad.ZoneId = squad.LeavingFor; squad.LeavingFor = null;
            squad.LastUpdatedTick = MacroState.Current.WorldTickCounter;
            if (squad.Heading == squad.ZoneId) { squad.Heading = null; OnArrived(squad); }
        }

        // nobody walks in right on top of you (you crossed in through that exit, or you're standing by it)
        static bool NearPlayer(ZoneExit e) => Player.Current != null && System.Math.Max(System.Math.Abs(e.X - Player.Current.X), System.Math.Abs(e.Y - Player.Current.Y)) < 15;

        /// <summary>A squad that moved into the zone you're in comes in through the exit that leads back to where it
        /// came from, then walks about 10 tiles inward before it starts its usual wander.</summary>
        public static bool ArriveLive(OfflineSquad squad, string fromZoneId)
        {
            var exit = TerrainMap.Exits.FirstOrDefault(e => e.TargetZoneId == fromZoneId);
            if (exit == null) return false;
            var taken = new HashSet<(int, int)>(EncounterState.Enemies.Where(e => e.IsAlive()).Select(e => (e.X, e.Y)));
            if (Player.Current != null) taken.Add((Player.Current.X, Player.Current.Y));
            int dx = TerrainMap.Width / 2 - exit.X, dy = TerrainMap.Height / 2 - exit.Y;
            var inward = TerrainMap.FindNearestEnterable(exit.X + System.Math.Sign(dx) * System.Math.Min(10, System.Math.Abs(dx)), exit.Y + System.Math.Sign(dy) * System.Math.Min(10, System.Math.Abs(dy)));
            if (EncounterState.InCombat && Player.Current != null) inward = TerrainMap.FindNearestEnterable(Player.Current.X, Player.Current.Y);   // Batch 59: walking into a gunfight, they head for it
            foreach (var m in squad.Members.Where(m => m.IsAlive()))
            {
                var spot = TerrainMap.FindNearestEnterable(exit.X, exit.Y);
                for (int tries = 0; tries < 16 && taken.Contains(spot); tries++)
                    spot = TerrainMap.FindNearestEnterable(Mathf.Clamp(exit.X + Random.Range(-2, 3), 0, TerrainMap.Width - 1), Mathf.Clamp(exit.Y + Random.Range(-2, 3), 0, TerrainMap.Height - 1));
                taken.Add(spot);
                m.X = spot.x; m.Y = spot.y; m.PatrolTarget = inward; m.IsAggroed = false;
                if (m is HumanEnemy h && h.Morale != "Surrendered") { h.Morale = "Steady"; h.HpShaken = false; }
                EncounterState.Enemies.Add(m);
            }
            GameLog.Write($"[MOVEMENT] {Cap(Who(squad.Faction))} comes in from {MacroMapDatabase.Find(fromZoneId)?.Name ?? fromZoneId}.");
            return true;
        }

        /// <summary>The zone you're in has its squads move on too, on foot: the squad picks an exit, its members walk
        /// there (TurnLoop's wander asks LeavingExit) and step out one by one (StepOut). Not while anyone is fighting.</summary>
        static void DepartLive(string here)
        {
            if (EncounterState.InCombat) return;
            foreach (var squad in MacroState.Current.OfflineSquads.Where(s => OnMap(s.ZoneId, here)).ToList())
            {
                if (squad.LeavingFor != null)
                {
                    // ponytail: a squad that can't reach its exit within 4 hours just goes (it may vanish in view); upgrade is a reachability check
                    bool walking = OnMap(squad.LeavingFor, here);   // the map overhaul: on to the next region of this map, on foot
                    int patience = System.Math.Max(4, (TerrainMap.Width + TerrainMap.Height) / 50) * (walking ? 2 : 1);   // the map overhaul: a long walk on a big map
                    if (MacroState.Current.WorldTickCounter - squad.LeavingSince >= patience) { if (walking) ArriveRegion(squad); else Leave(squad); }   // Batch 67: 4 hours, they walk a tile a minute now (was 2 a minute)
                    continue;
                }
                var alive = squad.Members.Where(m => m.IsAlive()).ToList();
                bool crowded = squad.Heading == null && squad.QuestId == null && squad.Job == null && Staying(squad.ZoneId) > MaxSquadsPerZone;   // the map overhaul: too many here, someone moves on
                if (alive.Count == 0 || alive.Any(m => m.IsAggroed) || (squad.QuestId != null || squad.Job == "hold") && squad.Heading == null || !crowded && Random.value > (squad.Heading != null ? TravelChancePerHour : MigrateChancePerHour)) continue;
                var hop = NextHop(squad);                                       // Batch 43: passing through on its way somewhere
                if (squad.Heading != null && hop == null) squad.Heading = null;
                // the map overhaul: the squad's own region's ways on - another region of this map (walked to), or an exit off it
                var from = MacroMapDatabase.Find(squad.ZoneId);
                var ways = (from?.ConnectedZones ?? new List<string>())
                    .Where(t => (hop != null ? t == hop : CanEnter(t, squad.Faction) && FitsZone(squad, t) && !(MacroMapDatabase.Find(t)?.IsDungeon ?? false) && !(MacroMapDatabase.Find(t)?.IsSafeZone ?? false) && Staying(t) < MaxSquadsPerZone)
                                && (OnMap(t, here) || TerrainMap.Exits.Any(e => e.TargetZoneId == t))).ToList();
                if (ways.Count == 0) continue;
                squad.LeavingFor = ways[Random.Range(0, ways.Count)];
                squad.LeavingSince = MacroState.Current.WorldTickCounter;
            }
        }

        /// <summary>Where this unit is walking to leave the zone, or null if its squad is staying.</summary>
        public static (int x, int y)? LeavingExit(Mutant m)
        {
            var squad = MacroState.Current.OfflineSquads.FirstOrDefault(s => s.LeavingFor != null && s.Members.Contains(m));
            if (squad == null || !OnMap(squad.ZoneId, TerrainMap.ZoneId)) return null;
            if (OnMap(squad.LeavingFor, TerrainMap.ZoneId)) return TerrainMap.RegionCenter(squad.LeavingFor);   // the map overhaul: walking to the next region
            var exit = TerrainMap.Exits.FirstOrDefault(e => e.TargetZoneId == squad.LeavingFor);
            if (exit == null) return null;
            return TerrainMap.FindNearestEnterable(exit.X, exit.Y);
        }

        /// <summary>A unit reached its squad's exit: off the map it goes. The last one out moves the squad.</summary>
        public static void StepOut(Mutant m)
        {
            var walker = MacroState.Current.OfflineSquads.FirstOrDefault(s => s.Members.Contains(m) && s.LeavingFor != null && OnMap(s.LeavingFor, TerrainMap.ZoneId));
            if (walker != null) { ArriveRegion(walker); return; }   // the map overhaul: reached the next region, still on the map
            EncounterState.Enemies.Remove(m);
            var squad = MacroState.Current.OfflineSquads.FirstOrDefault(s => s.Members.Contains(m));
            if (squad != null && !squad.Members.Any(x => x.IsAlive() && EncounterState.Enemies.Contains(x))) Leave(squad);
        }

        static void Leave(OfflineSquad squad)
        {
            EncounterState.Enemies.RemoveAll(e => e.IsAlive() && squad.Members.Contains(e));
            GameLog.Write($"[MOVEMENT] {squad.Faction} squad leaves {MacroMapDatabase.Find(squad.ZoneId)?.Name ?? squad.ZoneId} for {MacroMapDatabase.Find(squad.LeavingFor)?.Name ?? squad.LeavingFor}.");
            squad.ZoneId = squad.LeavingFor; squad.LeavingFor = null;
            if (squad.Heading == squad.ZoneId) { squad.Heading = null; OnArrived(squad); }
            squad.WorldX = TerrainMap.Width / 2; squad.WorldY = TerrainMap.Height / 2;
            squad.LastUpdatedTick = MacroState.Current.WorldTickCounter;
        }

        public static void TickSquadMigration(string skipZone = null)
        {
            // Advance the global overworld tick used for squad ageing/pruning
            MacroState.Current.WorldTickCounter++;

            var squads = MacroState.Current.OfflineSquads.Where(s => !s.IsActiveCombat && !OnMap(s.ZoneId, skipZone)).ToList();
            foreach (var squad in squads)
            {
                var currentZone = MacroMapDatabase.Find(squad.ZoneId);
                if (currentZone == null) continue;

                Scavenge(squad, currentZone);                                 // Batch 48: real spoils, not items out of nowhere

                bool canArrive = skipZone != null && TerrainMap.ZoneId == MacroMapDatabase.MapOf(skipZone) && TerrainMap.Exits.Any(e => e.TargetZoneId == currentZone.Id && !NearPlayer(e));   // Batch 37: in through the exit
                string destId;
                if (squad.Heading != null)                                     // Batch 43: on its way somewhere
                {
                    if (squad.Job != "retreat" && squad.Job != "backup" && Random.value > TravelChancePerHour) continue;   // Batch 54: a beaten squad doesn't linger; Batch 59: nor does help
                    destId = NextHop(squad);
                    if (destId == null) { squad.Heading = null; squad.Job = null; continue; }    // the way is closed now: it stays and wanders
                    if (OnMap(destId, skipZone) && (!canArrive || Staying(destId) >= MaxSquadsPerZone)) continue;   // waits for a clear way into your zone (and room in it)
                }
                else
                {
                    if (squad.QuestId != null || squad.Job == "hold" || Random.value > MigrateChancePerHour) continue;   // Batch 44/46: quest and goal squads hold their ground
                    var destinationIds = currentZone.ConnectedZones
                        .Where(targetId => CanEnter(targetId, squad.Faction) && FitsZone(squad, targetId) && (!OnMap(targetId, skipZone) || canArrive) && Staying(targetId) < MaxSquadsPerZone)   // the map overhaul: nobody roams into a full zone
                        .ToList();
                    if (destinationIds.Count == 0) continue;
                    destId = destinationIds[Random.Range(0, destinationIds.Count)];
                }
                bool arrived = squad.Heading == destId;
                squad.ZoneId = destId;
                squad.WorldX = TerrainMap.Width / 2;
                squad.WorldY = TerrainMap.Height / 2;
                squad.LastUpdatedTick = MacroState.Current.WorldTickCounter;
                var destZone = MacroMapDatabase.Find(destId);
                GameLog.Write($"[MOVEMENT] {squad.Faction} squad migrates from {currentZone.Name} to {destZone?.Name ?? destId}.");
                WorldEvents.News('M', destId, $"{(squad.Job == "caravan" ? "A trade caravan" : Cap(Who(squad.Faction)))} was seen heading from {currentZone.Name} into {destZone?.Name ?? destId}.");
                if (OnMap(destId, skipZone)) ArriveLive(squad, currentZone.Id);
                if (arrived) { squad.Heading = null; OnArrived(squad); }
            }

            // Prune old or empty squads to avoid unbounded growth
            PruneOldSquads(skipZone);
        }

        public static void ResolveOffscreenSquadConflicts(string skipZone = null)
        {
            var groupedByZone = MacroState.Current.OfflineSquads.Where(s => !OnMap(s.ZoneId, skipZone) && !(MacroMapDatabase.Find(s.ZoneId)?.IsSafeZone ?? false)).GroupBy(s => s.ZoneId).ToList();   // weapons hold in a Safe Zone
            foreach (var group in groupedByZone)
            {
                var squads = group.ToList();
                for (int i = 0; i < squads.Count; i++)
                {
                    for (int j = i + 1; j < squads.Count; j++)
                    {
                        var a = squads[i];
                        var b = squads[j];
                        if (!FactionDatabase.AreHostile(a.Faction, b.Faction) && !FactionDatabase.AreHostile(b.Faction, a.Faction)) continue;
                        if (a.QuestId != null || b.QuestId != null) continue;
                        if (a.Job == "retreat" || b.Job == "retreat" || !a.Members.Any(x => x.IsAlive()) || !b.Members.Any(x => x.IsAlive())) continue;   // Batch 53: they're already pulling out   // ponytail: quest squads sit out off-screen fights (a quest only counts your kills); upgrade = "someone got there first" outcomes   // Batch 40: either side, as on the live map (NpcFights)

                        GameLog.Write($"[OFFSCREEN WAR] {a.Faction} and {b.Faction} clash in {group.Key}.");
                        ResolveSquadSkirmish(a, b);
                    }
                }
            }

            MacroState.Current.OfflineSquads.RemoveAll(s => !OnMap(s.ZoneId, skipZone) && s.Members.All(m => !m.IsAlive()));
            SaveOfflineSquadsToDisk();
        }

        // TryEngageNearbySquad (WorldX/WorldY proximity check -> force a separate tactical
        // instance) was removed: it belonged to the old abstract-Overworld model. Engagement is
        // now organic - EnterZone puts a zone's squads directly on the live map, and TurnLoop's
        // existing vision/aggro check (see PlayerEndTurn) decides when that turns into a fight.

        private static void PruneOldSquads(string skipZone = null)
        {
            const int maxAgeTicks = 2400; // after this many overworld ticks (game hours since Batch 36, 100 days), inactive squads are removed

            // Remove squads with no living members first
            MacroState.Current.OfflineSquads.RemoveAll(s => !OnMap(s.ZoneId, skipZone) && (s.Members == null || s.Members.All(m => !m.IsAlive())));

            // Buckets by zone: trim oldest beyond max per zone
            var groups = MacroState.Current.OfflineSquads.Where(s => !OnMap(s.ZoneId, skipZone) && s.Heading == null && s.QuestId == null && (s.Job == null || s.Job == "hold")).GroupBy(s => s.ZoneId);   // the live zone is left alone, and so are squads passing through (Batch 43)
            foreach (var g in groups)
            {
                var list = g.OrderByDescending(s => s.Job == "hold").ThenByDescending(s => s.LastUpdatedTick).ToList();   // Batch 46: squads holding a goal zone push the others out
                if (list.Count <= MaxSquadsPerZone) continue;
                var toRemove = list.Skip(MaxSquadsPerZone).Where(s => s.Job != "hold").ToList();
                foreach (var rem in toRemove) MacroState.Current.OfflineSquads.Remove(rem);
            }

            // Finally, remove very old squads
MacroState.Current.OfflineSquads.RemoveAll(s => !OnMap(s.ZoneId, skipZone) && s.QuestId == null && s.Job != "hold" && (MacroState.Current.WorldTickCounter - s.LastUpdatedTick) > maxAgeTicks);
            SaveOfflineSquadsToDisk();
        }

        private static void ResolveSquadSkirmish(OfflineSquad a, OfflineSquad b)
        {
            var combatantsA = a.Members.Where(m => m.IsAlive()).Cast<ICombatant>().ToList();
            var combatantsB = b.Members.Where(m => m.IsAlive()).Cast<ICombatant>().ToList();
            int round = 0, startA = combatantsA.Count, startB = combatantsB.Count;
            OfflineSquad fled = null;

            while (combatantsA.Any(x => x.IsAlive()) && combatantsB.Any(x => x.IsAlive()) && round < 12)
            {
                // Batch 54: Yan (decision card): "Retreat". A side down to half its people and outnumbered breaks off
                // and heads home instead of fighting to the last man.
                if (combatantsA.Count * 2 <= startA && combatantsA.Count < combatantsB.Count) { fled = a; break; }
                if (combatantsB.Count * 2 <= startB && combatantsB.Count < combatantsA.Count) { fled = b; break; }
                round++;
                var attackerA = combatantsA.Where(x => x.IsAlive()).OrderBy(_ => Random.value).FirstOrDefault();
                var targetB = combatantsB.Where(x => x.IsAlive()).OrderBy(_ => Random.value).FirstOrDefault();
                if (attackerA != null && targetB != null) PerformOffscreenAttack(attackerA, targetB);

                var attackerB = combatantsB.Where(x => x.IsAlive()).OrderBy(_ => Random.value).FirstOrDefault();
                var targetA = combatantsA.Where(x => x.IsAlive()).OrderBy(_ => Random.value).FirstOrDefault();
                if (attackerB != null && targetA != null) PerformOffscreenAttack(attackerB, targetA);

                combatantsA = combatantsA.Where(x => x.IsAlive()).ToList();
                combatantsB = combatantsB.Where(x => x.IsAlive()).ToList();
            }

            if (fled != null)
            {
                Retreat(fled);
                var stood = fled == a ? b : a;
                WorldEvents.News('F', a.ZoneId, $"{Cap(Who(a.Faction))} and {Who(b.Faction)} fought in {MacroMapDatabase.Find(a.ZoneId)?.Name ?? a.ZoneId}. {Cap(The(fled.Faction))} fell back{(stood.Faction == fled.Faction ? "" : $", leaving it to {The(stood.Faction)}")}.");
                return;
            }
            bool aDead = !combatantsA.Any(x => x.IsAlive()), bDead = !combatantsB.Any(x => x.IsAlive()), spoils = false;
            if (aDead)
            {
                GameLog.Write($"[OFFSCREEN WAR] {a.Faction} squad has been destroyed.");
                a.Members.Clear();
                spoils |= DropWarSpoils(a.ZoneId, a.Faction);
            }
            if (bDead)
            {
                GameLog.Write($"[OFFSCREEN WAR] {b.Faction} squad has been destroyed.");
                b.Members.Clear();
                spoils |= DropWarSpoils(b.ZoneId, b.Faction);
            }
            // Batch 38: only a fight that ended a squad is news (a standoff is fought again next hour)
            if (!aDead && !bDead) return;
            string where = MacroMapDatabase.Find(a.ZoneId)?.Name ?? a.ZoneId;
            string end = aDead && bDead ? "Neither side walked away." : $"{Cap(The(aDead ? a.Faction : b.Faction))} {((aDead ? a.Faction : b.Faction) == "Mutant" ? "was" : "were")} wiped out.";
            WorldEvents.News('F', a.ZoneId, $"{Cap(Who(a.Faction))} and {Who(b.Faction)} fought in {where}. {end}{(spoils ? " Their gear is still lying there." : "")}");
        }

        /// <summary>Batch 54: a beaten squad heads for its outpost (mutants: the nearest nest); one already there, or with no
        /// way home, falls back to a neighbouring zone. It doesn't fight again until it gets there, and moves every hour.</summary>
        public static void Retreat(OfflineSquad squad)
        {
            var home = (Homes.TryGetValue(squad.Faction, out var h) ? h : new string[0])
                .Where(id => id != squad.ZoneId && InBand(squad, id)).Select(id => (id, r: Route(squad.ZoneId, id, squad.Faction, true))).Where(t => t.r != null)
                .OrderBy(t => t.r.Count).Select(t => t.id).FirstOrDefault();   // Batch 83: a northern squad doesn't fall back to a southern outpost
            var near = MacroMapDatabase.Find(squad.ZoneId)?.ConnectedZones.OrderBy(_ => Random.value).ToList() ?? new List<string>();
            squad.Heading = home ?? near.FirstOrDefault(z => InBand(squad, z)) ?? near.FirstOrDefault();
            squad.Job = squad.Heading == null ? null : "retreat";
        }

        static string Who(string faction) => faction == "Mutant" ? "a pack of mutants" : $"{("AEIOU".IndexOf(faction[0]) >= 0 ? "an" : "a")} {faction} squad";
        static string The(string faction) => faction == "Mutant" ? "the mutant pack" : $"the {faction}";
        static string Cap(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

        /// <summary>New (Batch 10): an unwitnessed squad death used to just vanish - no loot, no
        /// trace. Lower fidelity than a direct kill (one pooled "war spoils" stash rather than each
        /// dead member's exact gear - see CorpseLootUI for the full-fidelity version): reuses
        /// InteractionEngine's existing "highTierLoot" special case (a real tier-scaled weapon or
        /// armor roll) plus a cash band, dropped as an ordinary InteractiveObjectInstance. Only
        /// runs for zones already in PersistedZoneTerrain - a never-visited zone's terrain hasn't
        /// been generated yet, and BuildTerrainForZone's first-visit branch would silently
        /// overwrite (not append to) whatever object list was seeded here (see
        /// TerrainMap.PersistZoneTerrain's reference-sharing note).</summary>
        private static bool DropWarSpoils(string zoneId, string loserFaction)
        {
            if (string.IsNullOrEmpty(zoneId)) return false;
            string mapKey = MacroMapDatabase.MapOf(zoneId);   // the map overhaul: a region's things live on its tier map
            if (!MacroState.Current.PersistedZoneTerrain.TryGetValue(mapKey, out var tiles)) return false;

            if (!MacroState.Current.PersistedZoneInteractiveObjects.TryGetValue(mapKey, out var objects) || objects == null)
            {
                objects = new List<InteractiveObjectInstance>();
                MacroState.Current.PersistedZoneInteractiveObjects[mapKey] = objects;
            }

            TryFindWalkableTile(tiles, out int x, out int y);
            int tier = MacroMapDatabase.Find(zoneId)?.Tier ?? 1;
            objects.Add(new InteractiveObjectInstance
            {
                X = x,
                Y = y,
                Name = "War Spoils",
                Stat = new[] { "P" },
                Dc = 8, // an abandoned battlefield, not a real lock - low DC just to represent picking through the wreckage
                Reward = new InteractiveReward
                {
                    Ru = HumanLootSystem.CashBandForTier(tier),
                    Loot = new LootItem { Id = "highTierLoot", Name = "abandoned gear", WeightKg = 0, Qty = 1, Type = "loot" },
                },
                Description = $"The remains of a {loserFaction} squad, picked over but not yet emptied.",
            });
            GameLog.Write($"[OFFSCREEN WAR] Salvage left behind in {MacroMapDatabase.Find(zoneId)?.Name ?? zoneId}.");
            return true;
        }

        /// <summary>New (Batch 10): a random-attempt walkable-tile search against an arbitrary
        /// persisted terrain grid, mirroring TerrainMap.SpawnInteractiveObjects's loop - that one
        /// only ever operates on the currently-loaded zone's static Tiles, but a squad death is
        /// usually happening in a zone nobody's standing in right now.</summary>
        private static bool TryFindWalkableTile(TerrainTile[,] tiles, out int fx, out int fy)
        {
            int height = tiles.GetLength(0);
            int width = tiles.GetLength(1);
            for (int attempt = 0; attempt < 60; attempt++)
            {
                int x = Random.Range(0, width);
                int y = Random.Range(0, height);
                if (tiles[y, x] != null && !TileRules.BlocksMovement(tiles[y, x].Type))
                {
                    fx = x;
                    fy = y;
                    return true;
                }
            }
            fx = width / 2;
            fy = height / 2;
            return false;
        }

        private static void PerformOffscreenAttack(ICombatant attacker, ICombatant target)
        {
            var combat = GameServices.RequireCombat();
            int modifier = (int)attacker.AgilityMod;
            var roll = combat.RollAttack("standard", modifier, target.Ac);
            if (!roll.Hit && roll.ChosenRoll != CombatRules.Headshot) return;
            float tierMult = CombatRules.HitMultiplier(roll.ChosenRoll, roll.ChosenRoll + modifier, target.Ac);   // Batch 56
            float baseDamage = attacker is HumanEnemy human && human.EquippedWeapon != null
                ? combat.CalculateDamage(human.EquippedWeapon.Db.BaseDamage, human.EquippedWeapon.CurrentAmmoType, target.PhysDR, target.AnomDR)
                : combat.CalculateDamage(attacker is Mutant m ? m.AttackDamage : 40, "Standard", target.PhysDR, target.AnomDR);

            // Scale effective offscreen strikes by AP to approximate multi-action lethality without running full AI.
            int attackCount = System.Math.Max(1, attacker.Ap / 2); // ~2 attacks for 4 AP, 3 for 6 AP, etc.
            int applied = System.Math.Max(1, (int)(baseDamage * tierMult * attackCount));
            target.Hp = System.Math.Max(0, target.Hp - applied);
            if (!target.IsAlive() && attacker is HumanEnemy winner)   // Batch 53: the killer learns from it and takes the better gear
            {
                winner.GainXp(HumanEnemy.Worth(target));
                if (target is HumanEnemy loser) winner.LootFrom(loser);
            }
            GameLog.Write($"[OFFSCREEN WAR] {attacker.Name} performs {attackCount} simulated strike(s) for {applied} total damage on {target.Name}. {target.Name} HP now {target.Hp}.");
        }

        // -------------------- Persistence --------------------
        private enum UnitType
        {
            Human,
            Mutant,
        }

        private static string GetSavePath()
        {
            try
            {
                return Path.Combine(Application.persistentDataPath, "offline_squads.json");
            }
            catch
            {
                // Fallback to current directory if Application.persistentDataPath isn't available
                return Path.Combine(Directory.GetCurrentDirectory(), "offline_squads.json");
            }
        }

        [System.Serializable]
        private class PersistedSquad
        {
            public string ZoneId;
            public string Faction;
            public int WorldX;
            public int WorldY;
            public int Count;
            public int LastUpdatedTick;
            public string Heading, QuestId, Job;
            public bool IsActiveCombat;
            public bool PersistFullState;
            public bool ShakedownDone, BossRolled;
            public int TruceUntil;
            public List<PersistedMember> Members;
        }

        [System.Serializable]
        private class PersistedMember
        {
            public UnitType Type;
            public string LegacyType;
            public string DbName; // MutantDef.Name or Weapon/Armor names for lookup
            public string Name;
            public int X;
            public int Y;
            public int Hp;
            public int MaxHp;
            public int Ap;
            public float Mp;
            public int Ac;
            public int AttackDamage;
            public bool IsAggroed;

            // Human-specific
            public string Faction;
            public string Rank;
            public string Card;   // Batch 53
            public bool Named, ApRounds;   // Batch 80
            public int Xp;
            public string EquippedWeaponName;
            public string EquippedArmorName;
        }

        [System.Serializable]
        private class PersistedWrapper
        {
            public List<PersistedSquad> Squads = new List<PersistedSquad>();
        }

        private static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            LoadOfflineSquadsFromDisk();
        }

        public static void SaveOfflineSquadsToDisk()
        {
            try
            {
                var wrapper = new PersistedWrapper();
                foreach (var s in MacroState.Current.OfflineSquads)
                {
                    var ps = new PersistedSquad
                    {
                        ZoneId = s.ZoneId,
                        Faction = s.Faction,
                        WorldX = s.WorldX,
                        WorldY = s.WorldY,
                        Count = s.Members?.Count ?? 0,
                        LastUpdatedTick = s.LastUpdatedTick,
                        Heading = s.Heading, QuestId = s.QuestId, Job = s.Job,
                        IsActiveCombat = s.IsActiveCombat,
                        PersistFullState = s.PersistFullState,
                        ShakedownDone = s.ShakedownDone, TruceUntil = s.TruceUntil, BossRolled = s.BossRolled,
                        Members = null
                    };

                    if (s.PersistFullState || s.IsActiveCombat)
                    {
                        ps.Members = new List<PersistedMember>();
                        foreach (var m in s.Members)
                        {
                            if (m == null) continue;
                            var pm = new PersistedMember
                            {
                                Type = m is HumanEnemy ? UnitType.Human : UnitType.Mutant,
                                LegacyType = m.GetType().Name,
                                Name = m.Name,
                                X = m.X,
                                Y = m.Y,
                                Hp = m.Hp,
                                MaxHp = m.MaxHp,
                                Ap = m.Ap,
                                Mp = m.Mp,
                                Ac = m.Ac,
                                AttackDamage = m.AttackDamage,
                                IsAggroed = m.IsAggroed,
                            };

                            if (m is HumanEnemy he)
                            {
                                pm.Faction = he.FactionName;
                                pm.Rank = he.Rank; pm.Card = he.Card; pm.Xp = he.Xp; pm.Named = he.Named; pm.ApRounds = he.ApRounds;
                                pm.EquippedWeaponName = he.EquippedWeapon?.Db?.Name;
                                pm.EquippedArmorName = he.EquippedArmor?.Name;
                            }
                            else if (m.Db != null)
                            {
                                pm.DbName = m.Db.Name;
                            }

                            ps.Members.Add(pm);
                        }
                    }

                    wrapper.Squads.Add(ps);
                }

                string json = JsonUtility.ToJson(wrapper, true);
                File.WriteAllText(GetSavePath(), json);
            }
            catch (System.Exception ex)
            {
                GameLog.Write($"[PERSIST] Failed to save offline squads: {ex.Message}");
            }
        }

        public static void LoadOfflineSquadsFromDisk()
        {
            try
            {
                var path = GetSavePath();
                if (!File.Exists(path)) return;

                MacroState.Current.OfflineSquads.Clear();

                var json = NameMigration.Apply(File.ReadAllText(path));
                var wrapper = JsonUtility.FromJson<PersistedWrapper>(json);
                if (wrapper?.Squads == null) return;

                foreach (var ps in wrapper.Squads)
                {
                    OfflineSquad squad = new OfflineSquad
                    {
                        ZoneId = ps.ZoneId,
                        Faction = ps.Faction,
                        WorldX = ps.WorldX,
                        WorldY = ps.WorldY,
                        IsActiveCombat = ps.IsActiveCombat,
                        LastUpdatedTick = ps.LastUpdatedTick, Heading = ps.Heading, QuestId = ps.QuestId, Job = ps.Job,
                        PersistFullState = ps.PersistFullState,
                        ShakedownDone = ps.ShakedownDone, TruceUntil = ps.TruceUntil, BossRolled = ps.BossRolled,
                        Members = new List<Mutant>(),
                    };

                    if (ps.Members != null && ps.Members.Count > 0)
                    {
                        foreach (var pm in ps.Members)
                        {
                            if (pm == null) continue;

                        var memberType = pm.Type;
                        if (!string.IsNullOrEmpty(pm.LegacyType))
                        {
                            if (pm.LegacyType.Contains("Human")) memberType = UnitType.Human;
                            else if (pm.LegacyType.Contains("Mutant")) memberType = UnitType.Mutant;
                        }

                        if (memberType == UnitType.Human)
                        {
                            var wdb = FindWeaponDefByName(pm.EquippedWeaponName);
                            var adb = FindArmorDefByName(pm.EquippedArmorName);
                            var he = new HumanEnemy(string.IsNullOrEmpty(pm.Name) ? pm.Faction + " Operative" : pm.Name, pm.X, pm.Y, pm.Faction ?? ps.Faction, pm.Rank ?? "Rookie", wdb, adb);
                            he.Hp = pm.Hp; he.Card = pm.Card; he.Xp = pm.Xp;
                            he.Named = pm.Named; he.ApRounds = pm.ApRounds; he.ArmRolled = true; if (he.ApRounds) he.LoadApRounds(); if (he.Named) he.BossPerks();   // Batch 80
                            he.Ap = pm.Ap;
                            he.Mp = pm.Mp;
                            he.IsAggroed = pm.IsAggroed;
                            // ponytail: the disk format (PersistedMember) never serialized
                            // Inventory/Ru in the first place - a fresh roll on reload is the
                            // closest approximation available, same tradeoff the rest of this
                            // fallback already makes (weapon/armor looked up by name, not fully
                            // round-tripped either).
                            HumanLootSystem.RollSpawnStash(he, MacroMapDatabase.Find(ps.ZoneId)?.Tier ?? 1);
                            squad.Members.Add(he);
                        }
                        else
                        {
                            MutantDef mdb = FindMutantDefByName(pm.DbName ?? pm.Name);
                            var m = EncounterGeneration.CreateMutantFromDb(mdb, pm.X, pm.Y);
                            m.Hp = pm.Hp;
                            m.Ap = pm.Ap;
                            m.Mp = pm.Mp;
                            m.IsAggroed = pm.IsAggroed;
                            squad.Members.Add(m);
                        }
                        }
                        MacroState.Current.OfflineSquads.Add(squad);
                    }
                    else
                    {
                        // Fallback: regenerate squad compactly. Fix: CreateOfflineSquad already
                        // adds the squad to MacroState.Current.OfflineSquads (and saves to disk)
                        // itself - this branch used to add it a second time here, duplicating every
                        // squad loaded via the fallback path (the common case, since PersistFullState
                        // defaults false for anything that wasn't mid-combat when saved) on every load.
                        var created = CreateOfflineSquad(ps.ZoneId, ps.Faction, System.Math.Max(1, ps.Count));
                        if (created == null) continue;   // Batch 52: its faction's deck is empty
                        created.WorldX = ps.WorldX;
                        created.WorldY = ps.WorldY;
                        created.IsActiveCombat = ps.IsActiveCombat;
                        created.LastUpdatedTick = ps.LastUpdatedTick; created.Heading = ps.Heading; created.QuestId = ps.QuestId; created.Job = ps.Job;
                        created.ShakedownDone = ps.ShakedownDone; created.TruceUntil = ps.TruceUntil; created.BossRolled = ps.BossRolled;
                    }
                }
                GameLog.Write($"[PERSIST] Loaded {wrapper.Squads.Count} offline squad(s) from disk.");
            }
            catch (System.Exception ex)
            {
                GameLog.Write($"[PERSIST] Failed to load offline squads: {ex.Message}");
            }
        }

        // Helper lookups for DB entries by human-facing name
        private static WeaponDef FindWeaponDefByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            foreach (var cat in WeaponDatabase.Categories.Values)
                foreach (var w in cat.Values)
                    if (w.Name == name) return w;
            return null;
        }

        private static ArmorDef FindArmorDefByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            foreach (var tier in ArmorDatabase.Tiers.Values)
                foreach (var a in tier.Values)
                    if (a.Name == name) return a;
            return null;
        }

        private static MutantDef FindMutantDefByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            foreach (var tier in MutantDatabase.Tiers.Values)
                foreach (var m in tier.Values)
                    if (m.Name == name) return m;
            return null;
        }

    }
}
