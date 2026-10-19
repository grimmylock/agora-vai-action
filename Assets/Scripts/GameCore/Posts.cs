// Batch 110: A-Life posts, first step (CLAUDE.md "A-Life for big maps", Yan: it "makes sense"). Squads on your map used to walk
// to a random spot 8-20 tiles off; now the map's landmarks are posts and every squad has a home post and a task it keeps for a
// few hours: hold (stay round home, rest long), patrol (a loop of 2-3 nearby posts), scavenge (go and look over a post nobody
// holds) or attack (march on a post a weaker hostile squad holds; at most one attack per faction per day). Faction leanings
// decide the odds; mutants keep to their nest by day and roam at night. A post's owner is simply whoever is camped round it.
// Not yet: faction bases, territory along roads, nests that breed, and far squads hopping between posts off-screen.
using System.Collections.Generic;
using System.Linq;

namespace GameCore
{
    public static class Posts
    {
        public const int HoldRadius = 6, OwnRadius = 10, PatrolReach = 60;

        class Plan { public string Home, Task; public int Until; public List<(int x, int y)> Stops = new List<(int, int)>(); public int Next; }
        static readonly Dictionary<OfflineSquad, Plan> Plans = new Dictionary<OfflineSquad, Plan>();
        static readonly Dictionary<string, int> AttackDay = new Dictionary<string, int>();   // faction -> the day it last attacked

        static bool Mutantish(string f) => f == "Mutant" || f == "Zombie";

        public static LandmarkInstance Find(string name) => TerrainMap.Landmarks?.FirstOrDefault(l => l.Name == name);

        static LandmarkInstance Nearest(int x, int y, System.Func<LandmarkInstance, bool> ok = null) =>
            TerrainMap.Landmarks?.Where(l => ok == null || ok(l)).OrderBy(l => System.Math.Max(System.Math.Abs(l.X - x), System.Math.Abs(l.Y - y))).FirstOrDefault();

        /// <summary>The squads camped round a post (their leader within OwnRadius): its holders.</summary>
        public static List<OfflineSquad> Holders(LandmarkInstance p) =>
            MacroState.Current.OfflineSquads.Where(q => q.Members.Any(m => m.IsAlive() && EncounterState.Enemies.Contains(m)
                && CombatantUtil.Distance(m, p.X, p.Y) <= OwnRadius)).ToList();

        static int Strength(IEnumerable<OfflineSquad> qs) => qs.Sum(q => q.Members.Count(m => m.IsAlive()));

        /// <summary>The squad's task right now, for the harness and the examine panel ("hold", "patrol", "scavenge", "attack").</summary>
        public static string TaskOf(OfflineSquad q) => q != null && Plans.TryGetValue(q, out var p) ? p.Task : null;
        public static string HomeOf(OfflineSquad q) => q != null && Plans.TryGetValue(q, out var p) ? p.Home : null;

        /// <summary>Where the squad's leader walks next, or null to fall back to a random stroll (no landmarks on this map).
        /// Also tells how long to rest on arrival.</summary>
        public static (int x, int y)? NextStop(OfflineSquad q, Mutant lead, int minute, out int rest)
        {
            rest = UnityEngine.Random.Range(20, 91);
            if (q == null || TerrainMap.Landmarks == null || TerrainMap.Landmarks.Count == 0) return null;
            if (!Plans.TryGetValue(q, out var plan) || Find(plan.Home) == null)
                Plans[q] = plan = new Plan { Home = Nearest(lead.X, lead.Y, l => SquadManager.MayWalkInto(q.Faction, l.X, l.Y))?.Name };
            if (plan.Home == null) return null;
            if (minute >= plan.Until || plan.Next >= plan.Stops.Count) Choose(q, plan, lead, minute);
            if (plan.Task == "hold")
            {
                var h = Find(plan.Home); rest = UnityEngine.Random.Range(60, 181);
                double a = UnityEngine.Random.value * 6.283; int d = UnityEngine.Random.Range(1, HoldRadius + 1);
                return (h.X + (int)(System.Math.Cos(a) * d), h.Y + (int)(System.Math.Sin(a) * d));
            }
            var stop = plan.Stops[plan.Next++];
            if (plan.Task == "patrol" && plan.Next >= plan.Stops.Count) plan.Next = 0;   // a loop, until the task runs out
            return stop;
        }

        static void Choose(OfflineSquad q, Plan plan, Mutant lead, int minute)
        {
            var home = Find(plan.Home); var M = MacroState.Current;
            plan.Stops.Clear(); plan.Next = 0; plan.Until = minute + UnityEngine.Random.Range(180, 361);   // a task lasts 3-6 hours
            bool night = M.Hour < 6f || M.Hour >= 21f;
            var near = TerrainMap.Landmarks.Where(l => l != home && SquadManager.MayWalkInto(q.Faction, l.X, l.Y)
                && System.Math.Max(System.Math.Abs(l.X - home.X), System.Math.Abs(l.Y - home.Y)) <= PatrolReach).ToList();
            // faction leanings: (hold, patrol, scavenge, attack)
            var (h, p, s, a) = Mutantish(q.Faction) ? (night ? (10, 70, 20, 0) : (85, 15, 0, 0))
                : q.Faction == "Raiders" ? (25, 50, 15, 10) : q.Faction == "Wanderers" ? (50, 10, 40, 0)
                : q.Faction == "Obelisk" ? (55, 35, 0, 10) : (40, 45, 5, 10);
            if (q.QuestId != null || q.Job != null) (h, p, s, a) = (100, 0, 0, 0);   // quest crews and caravans keep their own business
            float r = UnityEngine.Random.value * (h + p + s + a);
            string task = r < h ? "hold" : r < h + p ? "patrol" : r < h + p + s ? "scavenge" : "attack";

            if (task == "attack")
            {
                var mine = Strength(new[] { q });
                var target = AttackDay.TryGetValue(q.Faction, out var day) && day == M.Day ? null
                    : near.Where(l => { var hs = Holders(l); return hs.Count > 0 && hs.All(o => o != q && Hostile(q.Faction, o.Faction)) && Strength(hs) < mine; })
                          .OrderBy(l => System.Math.Max(System.Math.Abs(l.X - home.X), System.Math.Abs(l.Y - home.Y))).FirstOrDefault();
                if (target == null) task = "patrol";
                else { AttackDay[q.Faction] = M.Day; plan.Stops.Add((target.X, target.Y)); }   // no log line: no on-screen enemy intent (Yan's rule)
            }
            if (task == "scavenge")
            {
                var spot = near.Where(l => Holders(l).Count == 0).OrderBy(_ => UnityEngine.Random.value).FirstOrDefault();
                if (spot == null) task = "patrol"; else { plan.Stops.Add((spot.X, spot.Y)); plan.Stops.Add((home.X, home.Y)); }
            }
            if (task == "patrol")
            {
                var loop = near.OrderBy(_ => UnityEngine.Random.value).Take(UnityEngine.Random.Range(2, 4)).ToList();
                if (loop.Count == 0) task = "hold";
                else { foreach (var l in loop) plan.Stops.Add((l.X, l.Y)); plan.Stops.Add((home.X, home.Y)); }
            }
            plan.Task = task;
        }

        static bool Hostile(string a, string b) => GameData.FactionDatabase.AreHostile(a, b) || GameData.FactionDatabase.AreHostile(b, a);

        /// <summary>A new map: plans belong to the old one.</summary>
        public static void Clear() { Plans.Clear(); }
    }
}
