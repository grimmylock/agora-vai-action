using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameData;

namespace GameCore
{
    /// <summary>2026-11-19 (Yan: "they do not need unique bosses in every dungeon, some can be a cool story, squads occupying the place, visual storytelling, random radiant quest targets"):
    /// Tier 2's points of interest. Papers in furniture near the landmarks (texts in ArtSource/story/tier2_poi_docs.py -> Tier2Poi.Docs.cs), crews that hold some of the places for the whole world
    /// (quest squads "poi:*", put down once), and the spots radiant job targets are sent to. The through-line: a pulse every eleven seconds from the north, the Loading Dock hatch, the ones who walked out silent.
    /// Never spawned next to the player: crews are placed like any quest squad, out of sight in their region.</summary>
    public static partial class Tier2Poi
    {
        // landmark, furniture that may hold the paper, prompt, name, description, doc id
        static readonly (string mark, string[] looks, string what, string name, string desc, string id)[] Papers =
        {
            ("Rusted Bus", new[] { "locker", "lockers", "crate" }, "Search the glovebox", "Survey Field Log", "A waterproof folder wedged under the driver's seat.", "bus"),
            ("Crane Cab", new[] { "desk", "console", "crate" }, "Search the cab", "Watcher's Tally", "A tally book beside a rifle, pages held down with a bolt.", "tally"),
            ("Chemical Dump", new[] { "crate", "locker", "lockers", "desk" }, "Search the site hut", "Disposal Manifest", "A clipboard hung inside a site hut, one line underlined twice.", "manifest"),
            ("Substation", new[] { "console", "terminal", "desk", "locker" }, "Search the control desk", "Transformer Log", "A greasy log pushed under the keyboard.", "transformer"),
            ("Central Plant", new[] { "desk", "desk_n", "desk_s", "desk_e", "desk_w", "locker", "lockers" }, "Search the foreman's desk", "Night Shift Roster", "A roster pinned under a glass paperweight.", "roster"),
            ("Workers' Flats", new[] { "desk", "desk_n", "desk_s", "cabinet" }, "Search the caretaker's desk", "Evacuation Order", "A stack of identical notices under a cold cup of tea.", "notice"),
            ("The Ford", new[] { "crate", "locker", "barrel" }, "Search the gauge hut", "Ford Crossing Gauge", "A gauge sheet nailed inside the hut door.", "ford"),
            ("Burnt Tank", new[] { "crate", "locker" }, "Search the hatch bin", "Orders, 6th Armoured Picket", "Two sheets in a scorched map case.", "tankorders"),
            ("Hanged Man's Tree", new[] { "crate", "barrel" }, "Read the board", "The Placard", "A board nailed to the trunk, scratched over.", "placard"),
            ("Overturned Lorry", new[] { "crate", "locker" }, "Search the cab", "Waybill", "A waybill stuck to the dashboard with something dark.", "waybill"),
            ("Hunter's Shack", new[] { "desk", "locker", "lockers", "crate" }, "Search the table", "Hunter's Log", "A small notebook under a rifle cleaning kit.", "hunterlog"),
            ("Old Mine Works", new[] { "desk", "locker", "lockers", "crate" }, "Search the shift hut", "Miner's Diary", "A diary in a tin, left where the shift lamps hang.", "minediary"),
        };
        static readonly Dictionary<string, (string mark, string[] looks, string what, string name, string desc, string id)> DungeonPapers = new Dictionary<string, (string, string[], string, string, string, string)>
        {
            ["valley_mine"] = ("Miners' Rest", new[] { "locker", "lockers", "crate", "desk" }, "Search the locker", "Unsent Postcard", "A postcard tucked in a locker door, stamped and never posted.", "postcard"),
            ["crusher_pit"] = ("Foreman's Cage", new[] { "desk", "locker", "lockers", "crate" }, "Search the foreman's locker", "Foreman's Logbook", "A logbook in the locker, the last page torn half out.", "foreman"),
        };

        // crews that hold a place for the whole world: id, landmark, faction, how
        static readonly (string id, string mark, string faction, string kind, int n)[] Crews =
        {
            ("plant", "Central Plant", "Prospectors", "crew", 5),
            ("stack", "Smokestack", "Contractors", "crew", 2),
            ("flats", "Workers' Flats", "Husks", "husk", 3),
            ("rail", "Rail Bridge Camp", "Prospectors", "crew", 4),
            ("wreck", "The Wreck Line", "Raiders", "crew", 3),
            ("shack", "Scrapper's Shack", "Outcasts", "crew", 3),
            ("lorry", "Overturned Lorry", "Mutant", "pallid", 3),
        };

        /// <summary>Places radiant job targets can be sent to (x, y, region, name): none of them hold a crew or the story.</summary>
        static readonly (string name, int x, int y, string region)[] Radiant =
        {
            ("the Chemical Dump", 16, 190, "industrial_basin"), ("the Substation", 90, 190, "industrial_basin"), ("the Crane Cab", 30, 186, "industrial_basin"),
            ("the Hanged Man's Tree", 77, 54, "black_valley"), ("the Burnt Tank", 131, 24, "black_valley"), ("the Hunter's Shack", 180, 30, "black_valley"), ("the Ford", 28, 47, "black_valley"),
            ("the Rusted Bus", 148, 180, "scrapyard"),
        };

        public static bool On(string zoneId) => zoneId == MacroMapDatabase.MapOf("scrapyard") || DungeonPapers.ContainsKey(zoneId ?? "");

        public static void Tick()
        {
            if (Net.Mode == Net.Role.Client || Player.Current == null) return;
            string z = TerrainMap.ZoneId; if (!On(z)) return;
            if (TerrainMap.InteractiveObjects.Any(o => o.Lock == "doc" && DocTexts.ContainsKey(o.Opens ?? ""))) return;
            PlaceDocs(z);
            if (z == MacroMapDatabase.MapOf("scrapyard") && !Story.Has("poi_crews")) { Story.Set("poi_crews"); PlaceCrews(); }
        }

        static LandmarkInstance Mark(string n) => (TerrainMap.Landmarks ?? new List<LandmarkInstance>()).FirstOrDefault(l => l.Name == n);

        internal static void PlaceDocs(string z)
        {
            if (DungeonPapers.TryGetValue(z, out var dp)) { Doc(dp); return; }
            foreach (var p in Papers) Doc(p);
        }

        /// <summary>The nearest piece of the right furniture to the landmark holds the paper (the prompt says what to search); none within 8 tiles: it lies on the floor beside the landmark.</summary>
        static void Doc((string mark, string[] looks, string what, string name, string desc, string id) p)
        {
            var l = Mark(p.mark); if (l == null) return;
            (int x, int y, float d) best = (-1, -1, 1e9f);
            for (int y = l.Y - 8; y <= l.Y + 8; y++) for (int x = l.X - 8; x <= l.X + 8; x++)
            {
                if (x < 0 || y < 0 || x >= TerrainMap.Width || y >= TerrainMap.Height || !p.looks.Contains(TerrainMap.GetTile(x, y).Look ?? "")) continue;
                if (TerrainMap.InteractiveObjects.Any(o => o.X == x && o.Y == y)) continue;
                float d = (x - l.X) * (x - l.X) + (y - l.Y) * (y - l.Y); if (d < best.d) best = (x, y, d);
            }
            if (best.x >= 0 && Stashes.PaperIn(best.x, best.y, p.what, p.desc, p.id) != null) return;
            for (int r = 1; r < 5; r++)
                for (int oy = -r; oy <= r; oy++) for (int ox = -r; ox <= r; ox++)
                {
                    int x = l.X + ox, y = l.Y + oy;
                    if (!TerrainMap.CanEnterTile(x, y) || TerrainMap.Landmarks.Any(m => m.X == x && m.Y == y)) continue;
                    if (Stashes.Paper(x, y, p.name, p.desc, p.id) != null) return;
                }
        }

        static void PlaceCrews()
        {
            foreach (var c in Crews)
            {
                var l = Mark(c.mark); if (l == null) continue;
                List<Mutant> list;
                if (c.kind == "husk") list = Husk.Pack(2, c.n);
                else if (c.kind == "pallid") list = Enumerable.Range(0, c.n).Select(_ => EncounterGeneration.CreateMutantFromDb(MutantDatabase.Tiers["tier2"]["VeilProwler"], 0, 0)).ToList();
                else
                {
                    list = new List<Mutant>();
                    for (int i = 0; i < c.n; i++)
                    {
                        string rank = i == 0 && c.n >= 4 ? "Veteran" : i == 0 ? "Professional" : "Experienced";
                        var (w, a) = EncounterGeneration.PickFactionLoadout(c.faction, rank, 2, isLeader: i == 0);
                        var h = new HumanEnemy(c.faction.TrimEnd('s'), 0, 0, c.faction, rank, w, a);
                        HumanLootSystem.RollSpawnStash(h, 2);
                        list.Add(h);
                    }
                }
                Around(list, l.X, l.Y);
                SquadManager.PlaceQuestSquad(TerrainMap.RegionAt(l.X, l.Y) ?? "industrial_basin", c.faction, list, "poi:" + c.id);
            }
        }

        static void Around(List<Mutant> members, int cx, int cy)
        {
            int k = 0;
            foreach (var m in members) { m.X = Mathf.Clamp(cx + (k % 3) - 1, 0, TerrainMap.Width - 1); m.Y = Mathf.Clamp(cy + (k / 3) + 1, 0, TerrainMap.Height - 1); k++; }
        }

        /// <summary>A radiant job's targets: a random place in the job's region that no crew holds (QuestSystem.SpawnTargets). Their start tiles are put down before the squad is placed, so a loaded map keeps them. Returns the place's name, or null off the Tier 2 map.</summary>
        public static string Radiate(List<Mutant> members, string zoneId)
        {
            if (members == null || members.Count == 0 || MacroMapDatabase.MapOf(zoneId) != MacroMapDatabase.MapOf("scrapyard")) return null;
            var spots = Radiant.Where(s => s.region == zoneId).ToList(); if (spots.Count == 0) return null;
            var s2 = spots[Random.Range(0, spots.Count)];
            Around(members, s2.x, s2.y);
            return s2.name;
        }
    }
}
