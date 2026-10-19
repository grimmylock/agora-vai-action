// The living world (file 10): every new Day rolls one d20 Zone Event and one d6 Faction Goal.
//   * Zone Events: quiet day (extra rumor), caravan, mutant migration, faction push, a Named NPC
//     makes contact, anomaly shift, and the Surge (3 rounds to reach shelter or take 60
//     anomalous damage + Irradiated; afterwards every anomaly can be searched again at +3).
//   * Faction Goals: six 6-box tracks; a full track changes the world for good (hazard levels,
//     who patrols where, the Black Valley toll, artifact search bonuses...).
//   * Rumors (1d3 on visiting a Safe Zone, drawn from real state; some are wrong) and the Named
//     NPCs list.
// Zone hazard and allowed factions are read through Hazard()/Factions() so goal effects apply
// without mutating the static zone data.
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public class FactionGoal
    {
        public string Faction, Goal, Result;
    }

    public static class WorldEvents
    {
        public static readonly FactionGoal[] Goals =
        {
            new FactionGoal { Faction = "Obelisk", Goal = "Seize The Terraces (Quarry Basin)", Result = "The Contractor outpost at Quarry Basin falls. Obelisk now patrols Quarry Basin and the Ship Graveyard." },
            new FactionGoal { Faction = "Contractors", Goal = "Fortify Ghost Town", Result = "Ghost Town is fortified: its hazard drops by 1 and the Raiders leave." },
            new FactionGoal { Faction = "Raiders", Goal = "Hold the Chokepoint Bridge (Black Valley)", Result = "Raiders hold the bridge: crossing into or out of Black Valley costs 200 RU or a fight, and its hazard rises by 1." },
            new FactionGoal { Faction = "Sentinels", Goal = "Hold Vanguard Barracks", Result = "The Sentinels take the Command Post at Vanguard Barracks for good." },
            new FactionGoal { Faction = "Liberators", Goal = "Hold Vanguard Barracks", Result = "The Liberators take the Command Post at Vanguard Barracks for good." },
            new FactionGoal { Faction = "Garrison", Goal = "Break the Siege Ward", Result = "The Siege Ward's hazard drops by 1 - Garrison escorts through it are available." },
            new FactionGoal { Faction = "Surveyors", Goal = "Map the Anomalous Epicenter", Result = "The Epicenter is mapped: artifact searches there get +2." },
        };

        public static int Boxes(string faction) => MacroState.Current.GoalBoxes.TryGetValue(faction, out var b) ? b : 0;
        public static bool Done(string faction) => MacroState.Current.CompletedGoals.Contains(faction);

        // ------------------------------------------------------------------ a new Day

        public static void OnNewDay()
        {
            var m = MacroState.Current;
            m.CaravanZoneId = null;               // Batch 45: a caravan's extra stock is for the day it arrives
            m.EventOfDay = ZoneEvent();
            TallyGoals();
            if (m.MigrationDaysLeft > 0 && --m.MigrationDaysLeft == 0) m.MigrationZoneId = null;
        }

        private static string ZoneEvent()
        {
            var m = MacroState.Current;
            int d = Random.Range(1, 21);
            var here = MacroMapDatabase.Find(m.CurrentZoneId);
            if (d <= 8)
            {
                m.ExtraRumors++;
                return "Quiet day.";
            }
            if (d <= 10)
            {
                // Batch 45: a real caravan sets out from the Zone's edge; the hub gets its stock only if it arrives
                var hub = NearestSafeZone(m.CurrentZoneId);
                if (SquadManager.SendCaravan(hub) == null) return "Quiet day.";
                GameLog.Write($"[ZONE EVENT] Radio chatter: a trade caravan is on its way to {MacroMapDatabase.Find(hub)?.Name ?? "the hub"}.");
                return $"Caravan heading for {MacroMapDatabase.Find(hub)?.Name}.";
            }
            if (d <= 12)
            {
                var near = here?.ConnectedZones?.Where(z => !(MacroMapDatabase.Find(z)?.IsSafeZone ?? true)).ToList();
                if (near != null && near.Count > 0)
                {
                    // Batch 45: real packs leave a nest and cross that zone (it used to just add +1 hazard and "Mutant" to its list)
                    var through = near[Random.Range(0, near.Count)];
                    if (SquadManager.SendMigration(through).Count == 0) return "Quiet day.";
                    m.MigrationZoneId = through;
                    m.MigrationDaysLeft = 2;
                    GameLog.Write($"[ZONE EVENT] Radio chatter: mutants are migrating through {MacroMapDatabase.Find(m.MigrationZoneId)?.Name}.");
                    return $"Mutant migration: {MacroMapDatabase.Find(m.MigrationZoneId)?.Name}.";
                }
                return "Quiet day.";
            }
            if (d <= 14)
            {
                FactionPush();
                return "Faction push.";
            }
            if (d <= 16)
            {
                if (m.NamedNpcs.Count > 0)
                {
                    string who = m.NamedNpcs[Random.Range(0, m.NamedNpcs.Count)];
                    GameLog.Write($"[ZONE EVENT] Word reaches you from someone you know: {who.Split('|')[0].Trim()} wants to talk.");
                    return "A Named NPC makes contact.";
                }
                return "Quiet day.";
            }
            if (d <= 18)
            {
                ShiftAnomalies(MacroMapDatabase.MapOf(m.CurrentZoneId));
                GameLog.Write("[ZONE EVENT] The air ripples - the anomalies around you have shifted. Searched anomalies can be searched again.");
                return "Anomaly shift.";
            }
            if (StorySystem.ContainPrices && d < 20) return "Quiet day.";   // Contain ending: Surges only on a 20
            StartSurge();
            return "SURGE.";
        }

        private static string NearestSafeZone(string from)
        {
            var seen = new HashSet<string> { from };
            var q = new Queue<string>();
            q.Enqueue(from);
            while (q.Count > 0)
            {
                var z = MacroMapDatabase.Find(q.Dequeue());
                if (z == null) continue;
                if (z.IsSafeZone) return z.Id;
                foreach (var n in z.ConnectedZones) if (seen.Add(n)) q.Enqueue(n);
            }
            return "scavengers_hub";
        }

        private static void ShiftAnomalies(string zoneId)
        {
            if (!MacroState.Current.PersistedZoneAnomalies.TryGetValue(zoneId, out var list)) return;
            // Batch 49: fields drift a few tiles (they used to teleport anywhere on the map) and may bring up a new artifact
            int tier = MacroMapDatabase.Find(zoneId)?.Tier ?? 1;
            foreach (var a in list)
            {
                a.Searched = false;
                a.Revealed = false;
                AnomalyEngine.SeedArtifact(a, tier, zoneId);
                if (zoneId != TerrainMap.ZoneId) continue;   // only the loaded map can be walked
                var spot = TerrainMap.FindNearestEnterable(Mathf.Clamp(a.X + Random.Range(-3, 4), 1, TerrainMap.Width - 2), Mathf.Clamp(a.Y + Random.Range(-3, 4), 1, TerrainMap.Height - 2));
                a.X = spot.x; a.Y = spot.y;
            }
        }

        // ------------------------------------------------------------------ Surge

        public static void StartSurge()
        {
            var m = MacroState.Current;
            m.SurgeRounds = 3;
            m.SurgeZoneId = MacroMapDatabase.MapOf(m.CurrentZoneId);
            GameLog.Write("[SURGE] The sky turns red and sirens wail. 3 rounds to get indoors, underground or into a Safe Zone!");
        }

        /// <summary>Indoors (a building floor), a hidden sub-node, or a Safe Zone.</summary>
        public static bool Sheltered(Player p)
        {
            var zone = MacroMapDatabase.Find(MacroState.Current.CurrentZoneId);
            if (zone == null || zone.IsSafeZone || zone.AccessCondition != null || zone.IsDungeon) return true;
            var g = TerrainMap.GetTile(p.X, p.Y).Ground;
            return g == GroundType.Floor || g == GroundType.LabTile;
        }

        /// <summary>Ticked at the end of each player turn (and on a zone crossing, which ends it).</summary>
        public static void TickSurge(bool crossing = false)
        {
            var m = MacroState.Current;
            var p = Player.Current;
            if (m.SurgeRounds <= 0 || p == null) return;
            if (!crossing && --m.SurgeRounds > 0)
            {
                GameLog.Write($"[SURGE] {m.SurgeRounds} round{(m.SurgeRounds > 1 ? "s" : "")} left to find shelter{(Sheltered(p) ? " (you're under cover)" : "")}.");
                return;
            }
            m.SurgeRounds = 0;
            if (Sheltered(p)) GameLog.Write("[SURGE] The wave passes overhead. You're safe in here.");
            else
            {
                int dmg = CombatRules.AnomalousDamage(60, p.AnomDR);
                p.Hp = System.Math.Max(0, p.Hp - dmg);
                p.StatusEffects.Irradiated = true;
                GameLog.Write($"[SURGE] Caught in the open! {dmg} anomalous damage and you're Irradiated.");
                TurnLoop.CheckCombatEnd();
            }
            // the zone's anomalies are fresh again, +3 to the next search
            if (m.PersistedZoneAnomalies.TryGetValue(m.SurgeZoneId ?? "", out var list))
                foreach (var a in list) { a.Searched = false; AnomalyEngine.SeedArtifact(a, MacroMapDatabase.Find(m.SurgeZoneId)?.Tier ?? 1, m.SurgeZoneId, 0.6f); }   // Batch 49: the Surge leaves fresh artifacts
        }

        // ------------------------------------------------------------------ Faction Goals

        // Batch 46: goals move on what's on the ground, not a daily d6. Each goal is a zone to hold against named rivals.
        // Every new Day a faction with more squads there than its rivals fills a box, one with fewer loses one, and one
        // that isn't winning sends a squad from home to take it (it walks there and digs in).
        public static readonly Dictionary<string, (string zone, string[] rivals)> GoalGround = new Dictionary<string, (string, string[])>
        {
            ["Obelisk"] = ("quarry_basin", new[] { "Contractors" }),
            ["Contractors"] = ("ghost_town", new[] { "Raiders" }),
            ["Raiders"] = ("black_valley", new[] { "Liberators" }),
            ["Sentinels"] = ("vanguard_barracks", new[] { "Liberators" }),
            ["Liberators"] = ("vanguard_barracks", new[] { "Sentinels" }),
            ["Garrison"] = ("hospital", new[] { "Obelisk", "Mutant" }),
            ["Surveyors"] = ("outskirts", new[] { "Obelisk", "Mutant" }),
        };

        static int Strength(string faction, string zoneId) =>
            MacroState.Current.OfflineSquads.Count(s => s.Faction == faction && s.ZoneId == zoneId && s.Heading == null && s.Members.Any(x => x.IsAlive()));

        public static void TallyGoals()
        {
            foreach (var g in Goals)
            {
                if (Done(g.Faction) || !GoalGround.TryGetValue(g.Faction, out var ground)) continue;
                int own = Strength(g.Faction, ground.zone), rival = ground.rivals.Sum(r => Strength(r, ground.zone));
                if (own > rival) AddBox(g.Faction, +1, "holding " + ground.zone);
                else if (own < rival) AddBox(g.Faction, -1, "pushed back");
                if (own <= rival) SquadManager.SendGoalSquad(g.Faction, ground.zone);
            }
        }

        /// <summary>Zone event "faction push": one faction sends an extra squad for its goal.</summary>
        private static void FactionPush()
        {
            var open = Goals.Where(g => !Done(g.Faction) && GoalGround.ContainsKey(g.Faction)).ToList();
            if (open.Count == 0) return;
            var g = open[Random.Range(0, open.Count)];
            var sent = SquadManager.SendGoalSquad(g.Faction, GoalGround[g.Faction].zone);
            if (sent != null) GameLog.Write($"[ZONE EVENT] The {g.Faction} are pushing for {MacroMapDatabase.Find(GoalGround[g.Faction].zone)?.Name}.");
        }

        /// <summary>Moves a faction's track. Quests for a faction fill a box; quests against it or
        /// killing several of its members empty one (file 10 §3).</summary>
        public static void AddBox(string faction, int delta, string why)
        {
            faction = FactionDatabase.NormalizeFactionName(faction);
            var goal = Goals.FirstOrDefault(g => g.Faction == faction);
            if (goal == null || Done(faction)) return;
            if (faction == "Sentinels" && Done("Liberators") || faction == "Liberators" && Done("Sentinels")) return;
            var m = MacroState.Current;
            int boxes = Mathf.Clamp(Boxes(faction) + delta, 0, 6);
            m.GoalBoxes[faction] = boxes;
            if (delta > 0 && !why.StartsWith("holding")) GameLog.Write($"[FACTION] {faction}: \"{goal.Goal}\" {Track(faction)}");
            if (boxes >= 6)
            {
                m.CompletedGoals.Add(faction);
                GameLog.Write($"[FACTION] {faction} achieve their goal! {goal.Result}");
            }
        }

        public static string Track(string faction) => "[" + new string('#', Boxes(faction)) + new string('.', 6 - Boxes(faction)) + "]";

        /// <summary>Several kills of one faction in a day cost that faction a box (once a day).</summary>
        public static void OnFactionMemberKilled(string faction)
        {
            var m = MacroState.Current;
            faction = FactionDatabase.NormalizeFactionName(faction);
            string key = $"{m.Day}:{faction}";
            m.KillsToday[key] = (m.KillsToday.TryGetValue(key, out var k) ? k : 0) + 1;
            if (m.KillsToday[key] == 3) AddBox(faction, -1, "losses");
        }

        public static int Hazard(ZoneDef z)
        {
            if (z == null) return 0;
            int h = z.BaseHazardModifier;
            if (z.Id == "ghost_town" && Done("Contractors")) h -= 1;
            if (z.Id == "black_valley" && Done("Raiders")) h += 1;
            if (z.Id == "hospital" && Done("Garrison")) h -= 1;
            if (StorySystem.Escorted(z.Id)) h -= 1;
            return System.Math.Max(0, h);
        }

        public static List<string> Factions(ZoneDef z)
        {
            var list = z?.AllowedFactions != null ? new List<string>(z.AllowedFactions) : new List<string>();
            if (z == null) return list;
            if ((z.Id == "quarry_basin" || z.Id == "ship_graveyard") && Done("Obelisk")) { list.Remove("Contractors"); if (!list.Contains("Obelisk")) list.Add("Obelisk"); }
            if (z.Id == "ghost_town" && Done("Contractors")) list.Remove("Raiders");
            if (z.Id == "vanguard_barracks" && (Done("Sentinels") || Done("Liberators")))
                list = new List<string> { Done("Sentinels") ? "Sentinels" : "Liberators" };
            return list;
        }

        public static int ArtifactSearchBonus(string zoneId) => (zoneId == "outskirts" && Done("Surveyors") ? 2 : 0) + StorySystem.ArtifactSearchMod;

        /// <summary>The Raiders' bridge toll in and out of Black Valley once they hold it.</summary>
        public static void OnCrossing(string fromZone, string toZone)
        {
            TickSurge(crossing: true);
            if (!Done("Raiders") || (fromZone != "black_valley" && toZone != "black_valley")) return;
            var p = Player.Current;
            if (p.Ru >= 200) { p.Ru -= 200; GameLog.Write("Raiders at the Chokepoint Bridge take their 200 RU toll."); }
            else
            {
                GameLog.Write("You can't pay the Raiders' bridge toll. They come for you.");
                foreach (var e in EncounterState.Enemies.OfType<HumanEnemy>().Where(e => e.FactionName == "Raiders")) e.IsAggroed = true;
                EncounterState.InCombat = EncounterState.Enemies.Any(e => e.IsAggroed && e.IsAlive());
            }
        }

        // ------------------------------------------------------------------ Batch 38: news from the A-Life tick
        // Yan: the off-screen news "need to be connected to actual world events". Only SquadManager calls News(),
        // and only for things that just happened: a squad wiped out in a fight (kind F), a squad moving (kind M).
        static int _lastRadioTick = -1;

        /// <summary>Records it for the rumor mill (about 30 kept). Gunfire in a zone next to yours also comes
        /// over the radio right away, at most once a game hour.</summary>
        public static void News(char kind, string zoneId, string text)
        {
            var m = MacroState.Current;
            m.WorldNews.Insert(0, $"{m.Day}|{kind}|{zoneId}|{text}");
            while (m.WorldNews.Count > 30) m.WorldNews.RemoveAt(m.WorldNews.Count - 1);
            // the map overhaul: next door to any region of the map you're on (but not on it - that you'd hear for yourself)
            bool nextDoor = m.CurrentZoneId != null && !MacroMapDatabase.SameMap(zoneId, m.CurrentZoneId)
                && MacroMapDatabase.RegionsOf(m.CurrentZoneId).Any(r => r.ConnectedZones != null && r.ConnectedZones.Contains(zoneId));
            if (kind != 'F' || !nextDoor || _lastRadioTick == m.WorldTickCounter) return;
            _lastRadioTick = m.WorldTickCounter;
            bool quiet = GameLog.Quiet; GameLog.Quiet = false;
            GameLog.Write($"[RADIO] Gunfire next door. {text}");
            GameLog.Quiet = quiet;
        }

        /// <summary>News as rumors: fights from the last 3 days, and the latest squad seen moving (last day).</summary>
        static IEnumerable<string> NewsRumors(MacroState m)
        {
            var items = m.WorldNews.Select(n => n.Split(new[] { '|' }, 4)).Where(p => p.Length == 4 && int.TryParse(p[0], out _)).ToList();
            foreach (var p in items.Where(p => p[1] == "F" && int.Parse(p[0]) >= m.Day - 3)) yield return p[3];
            var move = items.FirstOrDefault(p => p[1] == "M" && int.Parse(p[0]) >= m.Day - 1);
            if (move != null) yield return move[3];
        }

        // ------------------------------------------------------------------ rumors & named NPCs

        /// <summary>1d3 rumors (plus any extra from a quiet day) when you reach a Safe Zone. Drawn
        /// from real state; a rumor has a 1 in 3 chance to be wrong (file 10 §4).</summary>
        public static void TellRumors()
        {
            var m = MacroState.Current;
            if (m.LastRumorDay == m.Day) return;
            m.LastRumorDay = m.Day;
            int count = Random.Range(1, 4) + m.ExtraRumors;
            m.ExtraRumors = 0;
            var pool = new List<string>();
            foreach (var g in Goals) if (Boxes(g.Faction) >= 4 && !Done(g.Faction)) pool.Add($"The {g.Faction} are close to it: \"{g.Goal}\".");
            var hidden = MacroMapDatabase.Zones.Where(z => z.AccessCondition != null && !m.DiscoveredHiddenNodes.Contains(z.Id)).ToList();
            if (hidden.Count > 0)   // one hidden-way rumor per telling, not a flood of them
            {
                var z = hidden[Random.Range(0, hidden.Count)];
                var parent = MacroMapDatabase.Zones.FirstOrDefault(p => p.HiddenConnections != null && p.HiddenConnections.Contains(z.Id));
                var shown = Random.Range(1, 7) >= 5 ? MacroMapDatabase.Zones[Random.Range(0, MacroMapDatabase.Zones.Count)] : parent;  // 5-6: wrong
                if (parent != null) pool.Add($"Someone swears there's a hidden way into {z.Name} somewhere in {shown.Name}.");
            }
            if (m.CaravanZoneId != null) pool.Add($"A trade caravan has rolled into {MacroMapDatabase.Find(m.CaravanZoneId)?.Name} - extra stock today.");
            if (m.MigrationZoneId != null) pool.Add($"Mutants are on the move through {MacroMapDatabase.Find(m.MigrationZoneId)?.Name}.");
            if (m.SurgeZoneId != null) pool.Add($"After the last Surge, the anomalies in {MacroMapDatabase.Find(m.SurgeZoneId)?.Name} are supposed to be full of fresh artifacts.");
            pool.AddRange(NewsRumors(m));                                                    // Batch 38: real A-Life events
            if (m.NamedNpcs.Count > 0) pool.Add($"People have been asking about {m.NamedNpcs[Random.Range(0, m.NamedNpcs.Count)].Split('|')[0].Trim()}.");
            pool.RemoveAll(r => m.Rumors.Any(old => old.EndsWith(r)));   // don't repeat what you've already heard
            if (pool.Count == 0) return;
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int pick = Random.Range(0, pool.Count);
                string r = pool[pick];
                pool.RemoveAt(pick);
                m.Rumors.Insert(0, $"Day {m.Day}: {r}");
                GameLog.Write($"[RUMOR] {r}");
            }
            while (m.Rumors.Count > 10) m.Rumors.RemoveAt(m.Rumors.Count - 1);
        }

        /// <summary>Named NPCs (file 10 §5): one line each, about 10 kept.</summary>
        public static void NoteNpc(string name, string attitude, string status)
        {
            var m = MacroState.Current;
            string where = MacroMapDatabase.Find(m.CurrentZoneId)?.Name ?? m.CurrentZoneId;
            m.NamedNpcs.RemoveAll(n => n.StartsWith(name + " |"));
            m.NamedNpcs.Insert(0, $"{name} | {where} | {attitude} | {status}");
            while (m.NamedNpcs.Count > 10) m.NamedNpcs.RemoveAt(m.NamedNpcs.Count - 1);
        }
    }
}
