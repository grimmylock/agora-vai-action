// New (Batch 6): the tracker/engine for QuestDatabase.cs. Collapses every source quest verb down
// to 3 real mechanics (see QuestDatabase.cs's header note): Kill (a tiered target, in-zone),
// SkillCheck (find a clue spot on the zone map, Batch 50, or a tie to an already-existing MacroMapDatabase hidden
// node), and ClearZone (every hostile squad in the zone is dead - the Escort/Protect stand-in).
//
// A quest's full source checklist (QuestDef.ObjectiveText) is kept for display, but only the
// runtime Objectives list below is ever actually checked - a two-line source checklist like
// "Investigate X / Report back" becomes one real SkillCheck objective, and both display lines
// check off together when it resolves. There's no dialogue/turn-in system to model "report back"
// as a separate step, so treating it as bundled flavor rather than a second mechanic is the honest
// simplification here, not a missed feature.
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    // New (Batch 9): [Serializable] on both - plain public fields only, so JsonUtility can embed
    // List<ActiveQuest> directly in a save file (see SaveData.cs) with no separate mirror DTO.
    [System.Serializable]
    public class QuestObjectiveRuntime
    {
        public string Kind; // "Kill" | "SkillCheck" | "ClearZone"
        public string TargetKind;
        public int TargetTier;
        public int NeededCount = 1;
        public int CurrentCount;
        public string HiddenNodeId;
        public int Dc;
        public bool Done;
        public string ClueZone;   // Batch 50: an investigate objective's clue is a spot on that zone's map
        public int ClueX, ClueY;
    }

    [System.Serializable]
    public class ActiveQuest
    {
        public string QuestId;
        public string ZoneId; // resolved at accept time - the literal zone for "Any Tier N zone" quests
        public string[] DisplayText;
        public List<QuestObjectiveRuntime> Objectives;
        // file 09 quest rule 2: the d6 complication rolled at accept time
        public int Complication;
        public int AcceptedDay;
        public bool RivalSpawned;
        public bool TargetsSpawned;   // the quest's own targets have been put on the map
        public bool AllDone => Objectives.Count > 0 && Objectives.All(o => o.Done);
    }

    public static class QuestSystem
    {
        private static readonly string[] RankOrder = { "Rookie", "Experienced", "Professional", "Veteran", "Expert", "Master", "Legend" };

        /// <summary>Maps an NPC rank to the same 0-3 bucket the Economy file's Target Difficulty
        /// Bonus table already pairs ranks into (Rookie/Experienced, Professional/Veteran,
        /// Expert/Master, Legend) - reused both to filter Kill objectives and to look up the
        /// reward bonus, so there's one source of truth for "how tough is this NPC" instead of two.</summary>
        public static int NpcRankBucket(string rank)
        {
            int idx = System.Array.IndexOf(RankOrder, rank);
            if (idx < 0) idx = 0;
            return idx / 2;
        }

        // ---- Offering quests ----

        /// <summary>Standalone quests whose zone matches (exact zone, or "any zone at this tier"),
        /// aren't already completed/active, plus the current zone's NPC storyline stage (if any) -
        /// the one that's next in sequence for that Quartermaster, gated on Friendly rep if the
        /// stage requires it. There's no job board/dialogue system, so "the Quartermaster whose
        /// shop you're standing next to" is this game's stand-in for a quest-giving NPC.</summary>
        public static List<QuestDef> AvailableInZone(string zoneId)
        {
            var player = Player.Current;
            var zone = MacroMapDatabase.Find(zoneId);
            var result = new List<QuestDef>();
            if (player == null || zone == null) return result;
            if (zone.IsDungeon) return result;   // Batch 125: no jobs handed out in a cave or upstairs (their targets were dropped into the room with you)

            foreach (var q in QuestDatabase.All)
            {
                if (q.Stage > 0) continue;
                if (player.CompletedQuestIds.Contains(q.Id)) continue;
                if (player.ActiveQuests.Any(a => a.QuestId == q.Id)) continue;
                bool zoneMatches = q.ZoneId != null ? MacroMapDatabase.SameMap(q.ZoneId, zoneId) : q.ZoneTier == zone.Tier;   // the map overhaul: every job on this map
                if (zoneMatches) result.Add(q);
            }

            var qm = QuartermasterDatabase.Find(zoneId);
            if (qm != null)
            {
                int nextStage = player.NpcArcStage.TryGetValue(zoneId, out var s) ? s : 1;
                var stageDef = QuestDatabase.All.FirstOrDefault(q => q.NpcId == zoneId && q.Stage == nextStage);
                if (stageDef != null && !player.ActiveQuests.Any(a => a.QuestId == stageDef.Id)
                    && (!stageDef.FriendlyGate || player.GetRepLevel(stageDef.Faction) == "Friendly"))
                    result.Add(stageDef);
            }
            return result;
        }

        // ---- Accepting ----

        public static bool AcceptQuest(string questId)
        {
            var q = QuestDatabase.Find(questId);
            var player = Player.Current;
            if (q == null || player == null || player.ActiveQuests.Any(a => a.QuestId == questId)) return false;

            string zoneId = q.ZoneId ?? MacroState.Current.CurrentZoneId;
            var objectives = new List<QuestObjectiveRuntime>();
            int investigateDc = 12 + q.ZoneTier; // no fixed source DC for a generic investigate - matches the hidden-node DC curve minus 1

            switch (q.Category)
            {
                case QuestCategory.Elimination:
                    objectives.Add(new QuestObjectiveRuntime { Kind = "Kill", TargetKind = q.TargetKind, TargetTier = q.TargetTier, NeededCount = 1 });
                    break;
                case QuestCategory.KillQuota:
                    objectives.Add(new QuestObjectiveRuntime { Kind = "Kill", TargetKind = q.TargetKind, TargetTier = q.TargetTier, NeededCount = q.KillCount });
                    break;
                case QuestCategory.Retrieve:
                    if (q.Guarded)
                        objectives.Add(new QuestObjectiveRuntime { Kind = "Kill", TargetKind = q.TargetKind, TargetTier = q.TargetTier, NeededCount = 1 });
                    objectives.Add(new QuestObjectiveRuntime { Kind = "SkillCheck", HiddenNodeId = q.HiddenNodeId, Dc = investigateDc });
                    break;
                case QuestCategory.Investigate:
                    objectives.Add(new QuestObjectiveRuntime { Kind = "SkillCheck", HiddenNodeId = q.HiddenNodeId, Dc = investigateDc });
                    break;
                case QuestCategory.Escort:
                    objectives.Add(new QuestObjectiveRuntime { Kind = "ClearZone" });
                    break;
            }

            var aqNew = new ActiveQuest { QuestId = questId, ZoneId = zoneId, DisplayText = q.ObjectiveText, Objectives = objectives,
                Complication = Random.Range(1, 7), AcceptedDay = MacroState.Current.Day };
            if (aqNew.Complication == 3)   // Not as described: a second target
                foreach (var o in objectives.Where(o => o.Kind == "Kill")) o.NeededCount += 1;
            player.ActiveQuests.Add(aqNew);
            // accepted where the job is (or for an already-found hidden node): set it up now
            foreach (var o in objectives.Where(o => o.Kind == "SkillCheck" && o.HiddenNodeId != null && MacroState.Current.DiscoveredHiddenNodes.Contains(o.HiddenNodeId))) o.Done = true;
            GameLog.Write($"Quest accepted: {q.Hook}");
            GameLog.Write($"  Complication (d6 = {aqNew.Complication}): {ComplicationText(aqNew.Complication)}");
            var (g, alt) = Sides(q, zoneId);
            GameLog.Write($"  Two ways to finish: turn in to the {g}, or take it to the {alt} instead.");
            if (objectives.Any(o => o.Kind == "Kill" || o.Kind == "ClearZone")) SpawnTargets(aqNew, MacroMapDatabase.Find(zoneId));   // Batch 44: they're out there now
            if (aqNew.Complication == 2) SendRival(aqNew);
            if (aqNew.AllDone) CompleteQuest(player, aqNew);
            return true;
        }

        // ---- Progress hooks ----

        /// <summary>Called from Weapon.Fire's death hooks for every kill, human or mutant. Advances
        /// any active Kill objective bound to the zone the kill happened in, whose kind matches and
        /// whose tier requirement is met (kill something at or above what's asked - more forgiving
        /// than an exact match, since there's no GM to adjudicate "close enough"). Also re-checks
        /// ClearZone objectives for that same zone regardless of what was killed - any kill can be
        /// the one that empties it.</summary>
        public static void OnKill(string targetKind, int targetTier, string zoneId)
        {
            var player = Player.Current;
            if (player == null) return;

            var completed = new List<ActiveQuest>();
            foreach (var aq in player.ActiveQuests)
            {
                if (!MacroMapDatabase.SameMap(aq.ZoneId, zoneId)) continue;   // the map overhaul: anywhere on the quest's map
                bool zoneCleared = MacroMapDatabase.SameMap(zoneId, MacroState.Current.CurrentZoneId) && ZoneClear(aq.ZoneId);
                foreach (var obj in aq.Objectives.Where(o => !o.Done))
                {
                    if (obj.Kind == "Kill" && obj.TargetKind == targetKind && targetTier >= obj.TargetTier)
                    {
                        obj.CurrentCount++;
                        if (obj.CurrentCount >= obj.NeededCount) obj.Done = true;
                    }
                    else if (obj.Kind == "ClearZone" && zoneCleared)
                    {
                        obj.Done = true;
                    }
                }
                if (aq.AllDone) completed.Add(aq);
            }
            foreach (var aq in completed) CompleteQuest(player, aq);
        }

        /// <summary>Called from ZoneTravel.SearchArea's discovery loop - completes any SkillCheck
        /// objective tied to this exact hidden node. Hidden node ids are unique across
        /// MacroMapDatabase, so no zone filter is needed here.</summary>
        public static void OnHiddenNodeDiscovered(string nodeId)
        {
            var player = Player.Current;
            if (player == null) return;

            var completed = new List<ActiveQuest>();
            foreach (var aq in player.ActiveQuests)
            {
                foreach (var obj in aq.Objectives.Where(o => !o.Done && o.Kind == "SkillCheck" && o.HiddenNodeId == nodeId))
                    obj.Done = true;
                if (aq.AllDone) completed.Add(aq);
            }
            foreach (var aq in completed) CompleteQuest(player, aq);
        }

        // Batch 50: an investigate job is a real place in the quest zone (a clue spot put down the first time you're
        // there), not a Perception roll from anywhere. Walk within 2 + Perception tiles of it and you find it; F looks
        // twice as far and otherwise says which way to go, if it's within 30 tiles.
        static IEnumerable<(ActiveQuest aq, QuestObjectiveRuntime o)> OpenClues()
        {
            var p = Player.Current;
            if (p == null) yield break;
            string zoneId = MacroState.Current.CurrentZoneId;
            foreach (var aq in p.ActiveQuests.Where(a => MacroMapDatabase.SameMap(a.ZoneId, zoneId) && MacroMapDatabase.SameMap(a.ZoneId, TerrainMap.ZoneId)).ToList())
                foreach (var o in aq.Objectives.Where(o => !o.Done && o.Kind == "SkillCheck" && o.HiddenNodeId == null))
                {
                    if (o.ClueZone != aq.ZoneId) PlaceClue(o, aq.ZoneId);
                    yield return (aq, o);
                }
        }

        static void PlaceClue(QuestObjectiveRuntime o, string zoneId)
        {
            var p = Player.Current;
            for (int i = 0; i < 400; i++)
            {
                int x = Random.Range(3, TerrainMap.Width - 3), y = Random.Range(3, TerrainMap.Height - 3);
                if (TileRules.BlocksMovement(TerrainMap.GetTile(x, y).Type) || Dist(p, x, y) < 15 && i < 300) continue;
                if (i < 380 && TerrainMap.RegionAt(x, y) != zoneId && TerrainMap.RegionAt(x, y) != TerrainMap.ZoneId) continue;   // the map overhaul: in the quest's own region
                o.ClueZone = zoneId; o.ClueX = x; o.ClueY = y;
                return;
            }
            o.ClueZone = zoneId; o.ClueX = p.X; o.ClueY = p.Y;   // nowhere to put it: it's right here
        }

        static int Dist(Player p, int x, int y) => System.Math.Max(System.Math.Abs(x - p.X), System.Math.Abs(y - p.Y));
        public static int ClueRange() => System.Math.Max(1, 2 + (Player.Current?.PerceptionMod ?? 0));

        static void FindClue(ActiveQuest aq, QuestObjectiveRuntime o)
        {
            o.Done = true;
            GameLog.Write($"[QUEST] You found what you were looking for: \"{QuestDatabase.Find(aq.QuestId)?.Hook}\".");
            if (aq.AllDone) CompleteQuest(Player.Current, aq);
        }

        /// <summary>Every step (TurnLoop.TickAwareness).</summary>
        public static void SenseClues()
        {
            foreach (var (aq, o) in OpenClues().ToList())
                if (Dist(Player.Current, o.ClueX, o.ClueY) <= ClueRange()) FindClue(aq, o);
        }

        /// <summary>F: a careful look around for the current zone's investigate objective.</summary>
        public static void TryInvestigate()
        {
            var open = OpenClues().ToList();
            if (open.Count == 0) return;
            var p = Player.Current;
            var (aq, o) = open.OrderBy(c => Dist(p, c.o.ClueX, c.o.ClueY)).First();
            int d = Dist(p, o.ClueX, o.ClueY);
            if (d <= 2 * ClueRange()) FindClue(aq, o);
            else if (d <= 30) GameLog.Write($"[QUEST] Signs point {ZoneTravel.Compass(o.ClueX - p.X, o.ClueY - p.Y)} - what you're after is that way.");
            else GameLog.Write("[QUEST] Nothing here. What you're after is somewhere else in this zone.");
        }

        // ---- Completion & reward ----

        public static string ComplicationText(int c) => c switch
        {
            0 or 1 => "none - a clean job.",
            2 => "Rival - someone else is after the same thing, and they'll show up.",
            3 => "Not as described - there's a second target.",
            4 => "Double-cross - watch the giver at turn-in.",
            5 => "Clock - finish within 2 Days or the pay is halved.",
            _ => "Better offer - someone else may bid for what you bring back.",
        };

        /// <summary>Rival complication: the first time you walk into the quest's zone, a hostile
        /// squad after the same objective turns up.</summary>
        /// <summary>No one left who'd fight you: dead, fled, surrendered and Friendly don't count.</summary>
        public static bool ZoneClear(string zoneId = null) =>
            !EncounterState.Enemies.Any(e => EncounterState.IsActive(e) && !EnemyAI.FriendlyToPlayer(e) && InRegion(e, zoneId));

        /// <summary>The map overhaul: "clear the zone" on a tier map means the quest's own region - whoever belongs to a
        /// squad there or stands in it - not every squad on the whole map.</summary>
        static bool InRegion(Mutant e, string zoneId)
        {
            if (zoneId == null || !MacroMapDatabase.SameMap(zoneId, TerrainMap.ZoneId) || MacroMapDatabase.RegionsOf(TerrainMap.ZoneId).Count() <= 1) return true;
            var squad = MacroState.Current.OfflineSquads.FirstOrDefault(s => s.Members.Contains(e));
            return squad?.ZoneId == zoneId || TerrainMap.RegionAt(e.X, e.Y) == zoneId;
        }

        /// <summary>Re-checks "clear the zone" objectives - after a surrender or a fight ends, not
        /// only on a kill.</summary>
        public static void CheckClearZone()
        {
            var player = Player.Current;
            string zoneId = MacroState.Current.CurrentZoneId;
            if (player == null) return;
            var completed = new List<ActiveQuest>();
            foreach (var aq in player.ActiveQuests.Where(a => MacroMapDatabase.SameMap(a.ZoneId, zoneId) && a.TargetsSpawned && ZoneClear(a.ZoneId)))
            {
                foreach (var o in aq.Objectives.Where(o => !o.Done && o.Kind == "ClearZone")) o.Done = true;
                if (aq.AllDone) completed.Add(aq);
            }
            foreach (var aq in completed) CompleteQuest(player, aq);
        }

        /// <summary>The first time you enter a quest's zone, its targets are there - even in a Safe
        /// Zone, where nothing else spawns (YEFIM-2/3, KADE-1, T4-01 had nothing to kill).</summary>
        /// <summary>Batch 44: a quest's targets are a real squad in the quest zone, placed when you take the job (an old
        /// save's quest gets them on entering the zone). They hold their ground there; if you're in that zone they walk in
        /// through an exit. Before this they appeared 10+ tiles from you the moment you walked in.</summary>
        private static void SpawnTargets(ActiveQuest aq, ZoneDef zone)
        {
            aq.TargetsSpawned = true;
            if (zone == null) return;
            int tier = zone.Tier;
            string faction = (zone.AllowedFactions ?? new List<string>()).Where(f => f != "Mutant" && f != "Zombie" && Player.Current.GetRepLevel(f) != "Friendly")
                .OrderBy(f => Player.Current.Reputation.TryGetValue(f, out var r) ? r : 0).FirstOrDefault() ?? "Raiders";   // Batch 54: the local faction you get on worst with (bandits before loners)
            var humans = new List<Mutant>(); var mutants = new List<Mutant>();
            foreach (var o in aq.Objectives.Where(o => !o.Done))
            {
                int count = o.Kind == "ClearZone" ? 3 : System.Math.Max(0, o.NeededCount - o.CurrentCount);
                for (int i = 0; i < count; i++)
                {
                    if (o.Kind == "Kill" && o.TargetKind == "Mutant")
                    {
                        var def = EncounterGeneration.PickRandomMutantByTier(o.TargetTier, o.TargetTier) ?? EncounterGeneration.PickRandomMutantByTier(1, 5);
                        mutants.Add(EncounterGeneration.CreateMutantFromDb(def, 0, 0));
                    }
                    else if (o.Kind == "Kill" || o.Kind == "ClearZone")
                    {
                        string rank = o.Kind == "ClearZone" ? (i == 0 ? "Professional" : "Experienced")
                            : o.TargetTier switch { 0 => "Experienced", 1 => "Veteran", 2 => "Expert", _ => "Legend" };
                        var (w, a) = EncounterGeneration.PickFactionLoadout(faction, rank, tier, i == 0);
                        var h = new HumanEnemy($"{faction} {(humans.Count == 0 ? "Boss" : "Gunman")}", 0, 0, faction, rank, w, a);
                        if (humans.Count == 0) h.Frags = 1;   // Batch 56: a boss always has one grenade to surprise you with
                        humans.Add(h);
                    }
                }
            }
            if (humans.Count > 0) SquadManager.PlaceQuestSquad(zone.Id, faction, humans, aq.QuestId);
            if (mutants.Count > 0) SquadManager.PlaceQuestSquad(zone.Id, "Mutant", mutants, aq.QuestId);
            if (humans.Count + mutants.Count > 0) GameLog.Write($"[QUEST] Your targets for \"{QuestDatabase.Find(aq.QuestId)?.Hook}\" are in {zone.Name}.");
        }

        public static void OnEnterZone(string zoneId)
        {
            var player = Player.Current;
            if (player == null) return;
            var zoneDef = MacroMapDatabase.Find(zoneId);
            foreach (var aq in player.ActiveQuests.Where(a => !a.TargetsSpawned && a.ZoneId == zoneId && a.Objectives.Any(o => !o.Done && (o.Kind == "Kill" || o.Kind == "ClearZone"))))
                SpawnTargets(aq, zoneDef);   // a quest from a save made before Batch 44
        }

        /// <summary>Batch 44: the Rival complication is a real crew sent from its faction's home to the quest zone. It
        /// travels like any squad (news, fights, in through an exit) and holds there once it arrives. Rivals don't come for
        /// jobs in a Safe Zone.</summary>
        public static void SendRival(ActiveQuest aq)
        {
            aq.RivalSpawned = true;
            if (MacroState.Current.OfflineSquads.Any(q => q.QuestId == "rival:" + aq.QuestId)) return;   // Batch 82: one rival crew per job
            var zone = MacroMapDatabase.Find(aq.ZoneId);
            if (zone == null || zone.IsSafeZone) return;
            var local = WorldEvents.Factions(zone).Where(f => f != "Mutant" && f != "Zombie");
            var crew = SquadManager.SendQuestSquad(new[] { "Raiders", "Wanderers" }, zone.Id, "rival:" + aq.QuestId) ?? SquadManager.SendQuestSquad(local, zone.Id, "rival:" + aq.QuestId);   // locals may already be there (their base)
            if (crew == null) { GameLog.Write("[QUEST] Word is someone else wanted this job, but nobody could get there."); return; }
            GameLog.Write($"[QUEST] You're not the only one on this job - a {crew.Faction} crew is on its way to {zone.Name} for the same thing.");
        }

        private static readonly string[] Bidders = { "Contractors", "Surveyors", "Garrison", "Wanderers", "Prospectors", "Cartel" };

        /// <summary>File 09 rule 3: every quest can be finished two ways - turn it in to whoever
        /// it's for, or hand the result to a rival faction instead. Both named up front.</summary>
        public static (string giver, string alt) Sides(QuestDef q, string zoneId)
        {
            string giver = q.Faction ?? QuartermasterDatabase.Find(zoneId)?.Faction
                ?? MacroMapDatabase.Find(zoneId)?.AllowedFactions?.FirstOrDefault(f => f != "Mutant" && f != "Raiders" && f != "Zombie")
                ?? "Wanderers";
            giver = FactionDatabase.NormalizeFactionName(giver);
            var alts = Bidders.Where(f => f != giver).ToList();
            string alt = alts[System.Math.Abs(ZoneExitPlacer.StableHash(q.Id ?? "")) % alts.Count];
            return (giver, alt);
        }

        private static void CompleteQuest(Player player, ActiveQuest aq)
        {
            player.ActiveQuests.Remove(aq);
            SquadManager.ReleaseQuestSquads(aq.QuestId);
            var q = QuestDatabase.Find(aq.QuestId);
            if (q == null) return;

            int reward = ComputeQuestReward(q);
            if (aq.Complication == 5 && MacroState.Current.Day - aq.AcceptedDay > 2)
            {
                reward /= 2;
                GameLog.Write("[QUEST] You took too long - the pay is halved.");
            }

            player.NoteQuestDone(q);
            if (q.Stage > 0)
            {
                player.NpcArcStage[q.NpcId] = q.Stage + 1;
                var qm = QuartermasterDatabase.Find(q.NpcId);
                if (qm != null) WorldEvents.NoteNpc($"{qm.Name} ({qm.Faction ?? "independent"})", "Friendly", $"you finished job {q.Stage} for them");
            }
            else player.CompletedQuestIds.Add(q.Id);
            // file 09 rule 4: one non-Ru reward - storyline quests give reputation, standalone jobs a true rumor
            if (q.Stage == 0) TrueRumor(aq.ZoneId);

            var (giver, alt) = Sides(q, aq.ZoneId);
            int altPay = aq.Complication == 6 ? reward * 3 / 2 : reward;   // complication 6: the rival bids higher
            int giverRep = q.Stage > 0 ? 2 : 1;
            int pay = reward;
            ChoiceUI.Show($"JOB DONE: {q.Hook}",
                aq.Complication == 6 ? $"The {alt} heard what you've got and bid higher than the {giver}." : $"Turn it in to the {giver} as agreed - or take it to the {alt} instead.",
                new List<string>
                {
                    $"Turn in to the {giver}: {pay} RU, +{giverRep} rep {giver}",
                    $"Take it to the {alt}: {altPay} RU, +1 rep {alt}, -1 rep {giver}",
                }, i =>
                {
                    if (i == 0)
                    {
                        if (aq.Complication == 4)   // the double-cross is the giver's. Batch 48: your standing decides it, not a d20
                        {
                            if (player.GetRepLevel(giver) == "Friendly") GameLog.Write($"[QUEST] The giver starts to short you, then thinks better of crossing a friend of the {giver}. Full pay.");
                            else { pay /= 2; GameLog.Write($"[QUEST] The giver shorts you - you're nobody to the {giver}. Half pay."); }
                        }
                        player.Ru += pay;
                        player.AdjustReputation(giver, giverRep * Player.RepPoint);
                        WorldEvents.AddBox(giver, 1, "your work");
                        GameLog.Write($"Quest complete: {q.Hook} (+{pay} RU, +{giverRep} reputation with the {giver})");
                    }
                    else
                    {
                        player.Ru += altPay;
                        player.AdjustReputation(alt, Player.RepPoint);
                        player.AdjustReputation(giver, -Player.RepPoint);
                        WorldEvents.AddBox(alt, 1, "your work");
                        GameLog.Write($"Quest complete: {q.Hook} - you sold it to the {alt} (+{altPay} RU). The {giver} won't forget.");
                    }
                });
        }

        private static void TrueRumor(string zoneId)
        {
            var m = MacroState.Current;
            var hidden = MacroMapDatabase.Zones.Where(z => z.HiddenConnections != null)
                .SelectMany(z => z.HiddenConnections.Select(h => (parent: z, id: h)))
                .Where(x => !m.DiscoveredHiddenNodes.Contains(x.id)).OrderBy(x => x.parent.Id == zoneId ? 0 : 1).ThenBy(_ => Random.value).FirstOrDefault();
            if (hidden.parent == null) return;
            string r = $"Your contact swears there's a hidden way into {MacroMapDatabase.Find(hidden.id)?.Name} from {hidden.parent.Name}. (true)";
            m.Rumors.Insert(0, $"Day {m.Day}: {r}");
            GameLog.Write($"[RUMOR] {r}");
        }

        /// <summary>Economy file's formula: (Zone Base Reward + Target Difficulty Bonus) × Quest
        /// Type Multiplier. Zone Base is rolled from its band per the "roll everything" rule rather
        /// than picking a fixed number. New (Batch 9): the source's further ±10-30%
        /// reputation/remoteness adjustment (ReputationPricing.cs, same formula ShopSystem now
        /// uses) is applied here too - remoteness by the quest's own ZoneTier, reputation only for
        /// NPC storyline quests (q.Faction set; standalone quests have no faction to be Friendly
        /// with).</summary>
        public static int ComputeQuestReward(QuestDef q)
        {
            var (min, max) = ZoneBaseBand(q.ZoneTier);
            int zoneBase = Random.Range(min, max + 1);
            int bonus = TargetDifficultyBonus(q.TargetKind, q.TargetTier);
            if (q.Category == QuestCategory.KillQuota && q.KillCount > 1)
                bonus += (int)(0.15f * bonus * (q.KillCount - 1));
            float repMult = !string.IsNullOrEmpty(q.Faction) && Player.Current != null
                ? ReputationPricing.FriendlyMultiplier(Player.Current.GetRepLevel(q.Faction), isSelling: true)
                : 1f;
            float total = (zoneBase + bonus) * CategoryMultiplier(q.Category) * ReputationPricing.RemotenessMarkup(q.ZoneTier) * repMult;
            return (int)total;
        }

        private static (int min, int max) ZoneBaseBand(int tier) => tier switch
        {
            1 => (150, 300),
            2 => (300, 600),
            3 => (600, 1200),
            4 => (1200, 2500),
            5 => (2500, 5000),
            6 => (5000, 10000),
            _ => (10000, 20000),
        };

        private static int TargetDifficultyBonus(string kind, int tierOrBucket)
        {
            if (kind == "Mutant") return tierOrBucket switch { 1 => 0, 2 => 200, 3 => 500, 4 => 1000, 5 => 2000, _ => 0 };
            if (kind == "NPC") return tierOrBucket switch { 0 => 0, 1 => 300, 2 => 800, 3 => 1500, _ => 0 };
            return 0;
        }

        private static float CategoryMultiplier(QuestCategory cat) => cat switch
        {
            QuestCategory.Elimination => 1.0f,
            QuestCategory.KillQuota => 0.8f,
            QuestCategory.Retrieve => 0.7f,
            QuestCategory.Escort => 1.2f,
            QuestCategory.Investigate => 0.6f,
            _ => 1.0f,
        };
    }
}
