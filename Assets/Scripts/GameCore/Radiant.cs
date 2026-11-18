// 2026-11-18 RADIANT JOBS: repeatable jobs made at play time (Yan's rules). They come from three places:
//   * the physical Job Board at a safe zone (up to 4 notes, replaced every few days),
//   * traders (one job each: RU + reputation + sometimes a piece of gear),
//   * random friendly stalkers (only some have a job, one each, RU only).
// The PDA (J) lists the jobs of people near you; or walk up and talk (E). Targets are real squads placed
// by QuestSystem.SpawnTargets, like any job (rule 7). A job is a QuestDef (Radiant = true) rebuilt from the
// stored RadiantJob through QuestDatabase.Dynamic, so QuestSystem accepts / tracks / completes it as usual,
// but pays the fixed amount rolled when it was made and is NOT added to CompletedQuestIds (it can come again).
using System;
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    [Serializable]
    public class RadiantJob
    {
        public string Id;            // "rad:N"
        public string Source;        // board | trader | stalker
        public string GiverKey;      // board:zone | qm:zone | npc:Name|Faction
        public string GiverName, Faction, ZoneId;   // ZoneId = where the targets are
        public string HomeZone;      // where the job was posted
        public int Tier, KillCount;
        public bool Elimination;     // else a kill quota of mutants
        public int TargetTier;
        public string Hook, Line;
        public int Ru, Rep;
        public string GearKey;
        public int PostedDay, ExpiresDay;
        public bool Taken, NoJob;    // NoJob: this stalker was asked and has nothing
        public int X = -1, Y = -1;
    }

    public static class Radiant
    {
        public const int BoardSlots = 4, BoardLife = 3, TraderCooldown = 2, NearRange = 70;
        static List<RadiantJob> Jobs => MacroState.Current.Radiant;

        public static void Init() { QuestDatabase.Dynamic = id => { var j = Find(id); return j == null ? null : ToQuest(j); }; }

        public static RadiantJob Find(string id) => id == null || MacroState.Current == null ? null : Jobs.FirstOrDefault(j => j.Id == id);

        static QuestDef ToQuest(RadiantJob j) => new QuestDef
        {
            Id = j.Id, Hook = j.Hook, ObjectiveText = new[] { j.Line }, ZoneId = j.ZoneId, ZoneTier = j.Tier,
            Category = j.Elimination ? QuestCategory.Elimination : QuestCategory.KillQuota,
            TargetKind = j.Elimination ? "NPC" : "Mutant", TargetTier = j.TargetTier, KillCount = j.KillCount,
            Faction = j.Faction, Radiant = true, RewardRu = j.Ru, RepReward = j.Rep, GearKey = j.GearKey, GiverName = j.GiverName,
        };

        // ---- making jobs ----

        static string NextId() { var m = MacroState.Current; m.RadiantSeq++; return "rad:" + m.RadiantSeq; }

        static string PickTargetZone(string home)
        {
            var regs = MacroMapDatabase.RegionsOf(MacroMapDatabase.MapOf(home)).Where(z => !z.IsSafeZone && !z.IsDungeon && !z.Upstairs).ToList();
            if (regs.Count == 0) return null;
            return regs[UnityEngine.Random.Range(0, regs.Count)].Id;
        }

        static readonly string[] MutantHooks = { "Thin out the {0}", "Something is breeding in the {0}", "Clear the {0} of mutants", "The {0} is no place to walk" };
        static readonly string[] ManHooks = { "A bad man in the {0}", "Settle a debt in the {0}", "Remove a problem in the {0}" };

        /// <summary>Makes a job for a giver at `home`; null when the map has nowhere to send you.</summary>
        public static RadiantJob Make(string source, string giverKey, string giverName, string faction, string home)
        {
            var hz = MacroMapDatabase.Find(home); if (hz == null) return null;
            string tz = PickTargetZone(home); if (tz == null) return null;
            var tzd = MacroMapDatabase.Find(tz);
            int tier = Math.Max(1, tzd.Tier);
            bool man = UnityEngine.Random.value < 0.35f;
            var j = new RadiantJob
            {
                Id = NextId(), Source = source, GiverKey = giverKey, GiverName = giverName, Faction = faction, ZoneId = tz, HomeZone = home,
                Tier = tier, Elimination = man, PostedDay = MacroState.Current.Day, ExpiresDay = MacroState.Current.Day + (source == "board" ? BoardLife : 6),
                TargetTier = man ? Math.Min(3, (tier - 1) / 2) : tier,
                KillCount = man ? 1 : UnityEngine.Random.Range(3, 5 + Math.Min(2, tier / 2)),
            };
            string place = tzd.Name;
            j.Hook = string.Format((man ? ManHooks : MutantHooks)[UnityEngine.Random.Range(0, (man ? ManHooks : MutantHooks).Length)], place);
            j.Line = man ? $"Find and kill the gang boss in {place}." : $"Kill {j.KillCount} mutants in {place}.";
            var q = ToQuest(j);
            int ru = QuestSystem.ComputeQuestReward(q);
            j.Ru = Math.Max(20, ru);
            if (source == "trader")
            {
                j.Rep = UnityEngine.Random.Range(1, 3);
                if (UnityEngine.Random.value < 0.25f)
                {
                    var pool = ShopDatabase.Items.Where(i => i.Type == "consumable" && i.Price > 0 && i.Price <= 120 * tier).ToList();
                    if (pool.Count > 0) j.GearKey = pool[UnityEngine.Random.Range(0, pool.Count)].Id;
                }
            }
            Jobs.Add(j);
            return j;
        }

        /// <summary>Drops jobs nobody took that ran out of days. Cheap; called whenever the lists are read.</summary>
        public static void Expire()
        {
            int day = MacroState.Current.Day;
            Jobs.RemoveAll(j => !j.Taken && j.ExpiresDay < day && !j.NoJob);
            Jobs.RemoveAll(j => j.NoJob && j.ExpiresDay < day);
            // a taken job whose quest is gone (abandoned / failed) frees its giver
            var p = Player.Current;
            if (p != null) Jobs.RemoveAll(j => j.Taken && !p.ActiveQuests.Any(a => a.QuestId == j.Id));
        }

        static IEnumerable<RadiantJob> OfGiver(string key) => Jobs.Where(j => j.GiverKey == key);

        // ---- the board ----

        public static bool HasBoard(string zone) => TerrainMap.Landmarks.Any(l => l.Name == "Job Board") && MacroState.Current.CurrentZoneId == zone;

        public static List<RadiantJob> Board(string zone)
        {
            Expire();
            string key = "board:" + zone;
            var zd = MacroMapDatabase.Find(zone);
            var have = OfGiver(key).Count(j => !j.Taken);
            for (int i = have; i < BoardSlots && zd != null; i++)
            {
                var f = zd.AllowedFactions?.FirstOrDefault(x => x != "Mutant" && x != "Raiders" && x != "Zombie") ?? "Wanderers";
                if (Make("board", key, "A note on the board", f, zone) == null) break;
            }
            return OfGiver(key).Where(j => !j.Taken).ToList();
        }

        // ---- traders ----

        public static RadiantJob TraderJob(string zone)
        {
            var qm = QuartermasterDatabase.Find(zone); if (qm == null) return null;
            Expire();
            string key = "qm:" + zone;
            var cur = OfGiver(key).FirstOrDefault();
            if (cur != null) return cur.NoJob ? null : cur;
            var p = Player.Current;
            if (p == null || !QuartermasterDatabase.CanTrade(qm, qm.Faction == null ? "Neutral" : p.GetRepLevel(qm.Faction))) return null;
            var j = Make("trader", key, qm.Name, qm.Faction ?? "Wanderers", zone);
            return j;
        }

        // ---- stalkers ----

        public static string KeyOf(HumanEnemy h) => "npc:" + h.Name + "|" + h.FactionName;

        /// <summary>The job a friendly stalker has (rolled once: about a third of them have one).</summary>
        public static RadiantJob StalkerJob(HumanEnemy h)
        {
            Expire();
            string key = KeyOf(h);
            var cur = OfGiver(key).FirstOrDefault();
            if (cur != null) return cur.NoJob ? null : cur;
            string zone = MacroState.Current.CurrentZoneId;
            if (UnityEngine.Random.value >= 0.35f)
            {
                Jobs.Add(new RadiantJob { Id = NextId(), Source = "stalker", GiverKey = key, NoJob = true, ExpiresDay = MacroState.Current.Day + 3 });
                return null;
            }
            var j = Make("stalker", key, h.Name, h.FactionName, zone);
            if (j != null) { j.GearKey = null; j.Rep = 0; j.X = h.X; j.Y = h.Y; }
            return j;
        }

        /// <summary>Friendly people within PDA range who have a job for you (rolls the unrolled ones).</summary>
        public static List<(RadiantJob job, int dist)> Nearby(Player p)
        {
            var list = new List<(RadiantJob, int)>();
            if (p == null || MacroState.Current == null) return list;
            foreach (var h in EncounterState.Enemies.OfType<HumanEnemy>())
            {
                if (!h.IsAlive() || !EnemyAI.FriendlyToPlayer(h) || Companion.Is(h)) continue;
                int d = (int)CombatantUtil.Distance(p, h);
                if (d > NearRange) continue;
                var j = StalkerJob(h);
                if (j != null && !j.Taken) { j.X = h.X; j.Y = h.Y; list.Add((j, d)); }
            }
            var qm = TraderNear(p);
            if (qm != null) { var j = TraderJob(qm); if (j != null && !j.Taken) list.Add((j, 0)); }
            return list.OrderBy(t => t.Item2).ToList();
        }

        static string TraderNear(Player p) => MacroState.Current != null && QuartermasterDatabase.Find(MacroState.Current.CurrentZoneId) != null ? MacroState.Current.CurrentZoneId : null;

        // ---- taking, talking ----

        public static string Describe(RadiantJob j)
        {
            string pay = j.Source == "stalker" ? $"{j.Ru} RU"
                : $"{j.Ru} RU" + (j.Rep > 0 ? $", +{j.Rep} rep {j.Faction}" : "") + (j.GearKey != null ? $", and {ShopDatabase.Items.FirstOrDefault(i => i.Id == j.GearKey)?.Name ?? "gear"}" : "");
            return $"{j.Hook}\n{j.Line}\nPays: {pay}";
        }

        public static bool Take(RadiantJob j)
        {
            if (j == null || j.Taken || Player.Current == null) return false;
            if (!QuestSystem.AcceptQuest(j.Id)) return false;
            j.Taken = true;
            return true;
        }

        /// <summary>Walk-up conversation: E next to a friendly stalker, or at the board.</summary>
        public static bool TryTalk(Player p)
        {
            if (p == null || ChoiceUI.IsOpen) return false;
            foreach (var l in TerrainMap.Landmarks)
                if (l.Name == "Job Board" && Math.Abs(l.X - p.X) <= 2 && Math.Abs(l.Y - p.Y) <= 2)
                {
                    var board = Board(MacroState.Current.CurrentZoneId);
                    if (board.Count == 0) { GameLog.Write("The board is bare."); return true; }
                    var opts = board.Select(j => $"{j.Hook}: {j.Line} ({j.Ru} RU)").ToList(); opts.Add("Leave them");
                    ChoiceUI.Show("JOB BOARD", "Notes pinned up by the fire.", opts, i => { if (i < board.Count) Take(board[i]); });
                    return true;
                }
            var h = NearFriend(p);
            if (h != null)
            {
                var j = StalkerJob(h);
                if (j == null || j.Taken) { GameLog.Write($"{h.Name} has nothing for you."); return true; }
                ChoiceUI.Show(h.Name.ToUpperInvariant(), Describe(j), new List<string> { "Take it", "Not now" }, i => { if (i == 0) Take(j); }, h);
                return true;
            }
            return false;
        }

        /// <summary>True if this stalker is known to have a job (rolled and not taken): the E prompt only shows for them.</summary>
        public static bool Known(HumanEnemy h) { var j = OfGiver(KeyOf(h)).FirstOrDefault(); return j == null ? true : !j.NoJob && !j.Taken; }

        public static HumanEnemy NearFriend(Player p) => EncounterState.Enemies.OfType<HumanEnemy>()
            .Where(h => h.IsAlive() && EnemyAI.FriendlyToPlayer(h) && !Companion.Is(h) && CombatantUtil.Distance(p, h) <= 2)
            .OrderBy(h => CombatantUtil.Distance(p, h)).FirstOrDefault();

        // ---- paying out (QuestSystem.CompleteQuest calls this) ----

        public static void Pay(Player p, QuestDef q)
        {
            p.Ru += q.RewardRu;
            string text = $"Job done: {q.Hook} (+{q.RewardRu} RU";
            if (q.RepReward > 0 && q.Faction != null) { p.AdjustReputation(q.Faction, q.RepReward * Player.RepPoint); text += $", +{q.RepReward} reputation with the {q.Faction}"; }
            var gear = q.GearKey == null ? null : ShopDatabase.Items.FirstOrDefault(i => i.Id == q.GearKey);
            if (gear != null) { p.AddItem(TestingGround.ItemOf(gear)); text += $", {gear.Name}"; }
            GameLog.Event(text + ")");
            var j = Find(q.Id);
            if (j != null) { Jobs.Remove(j); if (j.Source != "board") Jobs.Add(new RadiantJob { Id = NextId(), Source = j.Source, GiverKey = j.GiverKey, NoJob = true, ExpiresDay = MacroState.Current.Day + TraderCooldown }); }
        }
    }
}
