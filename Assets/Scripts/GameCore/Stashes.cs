// Batch 126 (Yan: "Fewer, richer stashes: about 8 per map, mostly locked, either requiring solving a puzzle/killing a difficult
// enemy (like a mutant swallowed the key or something) or inside places squads hold. Ordinary loot would come from bodies and quests
// instead ... randomize them every 7 days in game ... dungeons should always have a stash in one of its ends").
// A tier map keeps its hand-placed note stashes (one-time finds with a story) and tops them up to about 8 with rolled ones:
//   post - in a building by one of the map's posts (TerrainMap landmarks), where squads camp: the guards are the lock;
//   key  - locked; the key is in the belly of the toughest mutant on the map (or the pocket of the hardest gang boss if there are no
//          mutants): kill it and you have it. The lock says who has it;
//   code - a keypad; the code is on a note left by another landmark of the map, and the lock says which one: find it, read it (E).
// A dungeon gets one rich stash at its far end. Every 7 game days the rolled stashes (and the dungeon's) are put down afresh.
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class Stashes
    {
        public const int PerMap = 8, MinRolled = 2, Week = 7;
        static readonly Dictionary<string, int> RolledWeek = new Dictionary<string, int>();   // map -> the week its stashes were put down

        static int WeekNow => (MacroState.Current.Day - 1) / Week;
        static int D(int ax, int ay, int bx, int by) => System.Math.Max(System.Math.Abs(ax - bx), System.Math.Abs(ay - by));
        public static string KeyId(InteractiveObjectInstance o) => $"stash:{TerrainMap.ZoneId}:{o.X},{o.Y}";

        static InteractiveReward Rich(int tier) => new InteractiveReward { Ru = 120 * tier, Loot = new LootItem { Id = "richStash", Name = "Stash", Qty = 1, Type = "loot" } };

        static bool BuiltWall(int x, int y)
        {
            if (x < 0 || y < 0 || x >= TerrainMap.Width || y >= TerrainMap.Height) return false;
            var t = TerrainMap.Tiles[y, x];
            return t.Type == TileType.Solid && (t.Look == "wall" || t.Look == "plaster" || t.Look == "concrete" || t.Look == "metal" || t.Look == "plank" || t.Look == "hull");
        }
        static bool Bare(int x, int y) => x > 0 && y > 0 && x < TerrainMap.Width - 1 && y < TerrainMap.Height - 1 && TerrainMap.Tiles[y, x].Type == TileType.Empty && TerrainMap.Tiles[y, x].Look == null;
        static bool Safe(int x, int y) => MacroMapDatabase.Find(TerrainMap.RegionAt(x, y) ?? TerrainMap.ZoneId)?.IsSafeZone == true;

        /// <summary>Indoor floor against a wall (not the south one: it would hide behind the wall face).</summary>
        static List<(int x, int y)> WallSpots()
        {
            var list = new List<(int, int)>();
            for (int y = 1; y < TerrainMap.Height - 1; y++)
                for (int x = 1; x < TerrainMap.Width - 1; x++)
                {
                    if (!Bare(x, y)) continue;
                    var g = TerrainMap.Tiles[y, x].Ground;
                    if (g != GroundType.Floor && g != GroundType.LabTile) continue;
                    if (BuiltWall(x - 1, y) || BuiltWall(x + 1, y) || BuiltWall(x, y - 1)) list.Add((x, y));
                }
            return list;
        }

        static InteractiveObjectInstance Put(int x, int y, string look, string name, InteractiveReward reward, string desc, string lockKind = null)
        {
            var db = InteractiveObjectDatabase.All[look == "stash_safe" ? "ElectronicLock" : "Footlocker"];
            var o = new InteractiveObjectInstance { X = x, Y = y, Name = name, Stat = db.Stat, Dc = db.Dc, Reward = reward, Description = desc, Lock = lockKind, Rolled = true };
            TerrainMap.InteractiveObjects.Add(o);
            var t = TerrainMap.GetTile(x, y);
            t.Look = look; if (look != "stash_pda" && look != "stash_duffel" && look != "papers") t.Type = TileType.Difficult;
            TerrainMap.DirtyTiles.Add((x, y));
            return o;
        }

        /// <summary>Puts this map's rolled stashes down (TerrainMap.SpawnInteractiveObjects, and every 7 days).</summary>
        public static void Roll()
        {
            RolledWeek[TerrainMap.ZoneId ?? ""] = WeekNow;
            var zone = MacroMapDatabase.Find(TerrainMap.ZoneId);
            int tier = zone?.Tier ?? 1;
            if (zone != null && zone.IsDungeon) { DungeonEnd(tier); return; }
            int authored = TerrainMap.InteractiveObjects.Count(o => !o.Rolled);
            int want = System.Math.Max(MinRolled, PerMap - authored);
            var spots = WallSpots().Where(s => !Safe(s.x, s.y)).ToList();
            var posts = TerrainMap.Landmarks.Where(l => !Safe(l.X, l.Y) && !l.Name.StartsWith("stash:")).ToList();
            if (spots.Count == 0 || posts.Count == 0) return;
            bool Free(int x, int y) => Bare(x, y) && !TerrainMap.InteractiveObjects.Any(o => D(o.X, o.Y, x, y) < 12);
            string[] kinds = { "key", "code", "post", "key", "code", "post", "key", "post" };   // mostly locked
            for (int i = 0, tries = 0; i < want && tries < want * 40; tries++)
            {
                string kind = kinds[i % kinds.Length];
                var post = posts[Random.Range(0, posts.Count)];
                // post stashes sit in a building by a post; locked ones anywhere indoors
                var near = kind == "post" ? spots.Where(s => D(s.x, s.y, post.X, post.Y) <= 10).ToList() : spots;
                if (near.Count == 0) continue;
                var (x, y) = near[Random.Range(0, near.Count)];
                if (!Free(x, y)) continue;
                if (kind == "post")
                    Put(x, y, Random.value < 0.5f ? "stash_footlocker" : "stash_chest", Random.value < 0.5f ? "Footlocker" : "Old Chest", Rich(tier), $"Kept by whoever holds {post.Name}.");
                else if (kind == "key")
                    Put(x, y, "stash_chest", "Locked Chest", Rich(tier), "A heavy padlock. Somebody has the key.", "key");
                else
                {
                    var note = posts.Where(l => D(l.X, l.Y, x, y) >= 15).OrderBy(_ => Random.value).FirstOrDefault();
                    if (note == null) continue;
                    var n = TerrainMap.FindNearestEnterable(note.X + Random.Range(-3, 4), note.Y + Random.Range(-3, 4));
                    if (!Bare(n.x, n.y)) continue;
                    var safe = Put(x, y, "stash_safe", "Keypad Safe", Rich(tier + 1), $"A keypad lock. Scratched beside it: \"the code's at {note.Name}\".", "code");
                    safe.Code = Random.Range(100, 1000).ToString();
                    var paper = Put(n.x, n.y, "papers", "Scrawled Note", null, "A note in a plastic sleeve.", "note");
                    paper.Code = safe.Code; paper.Opens = KeyId(safe);
                }
                i++;
            }
        }

        /// <summary>A dungeon's stash: at its far end (the walkable spot farthest from the way in).</summary>
        static void DungeonEnd(int tier)
        {
            // the way in: the map's first exit anchor (exits themselves are worked out after the stashes)
            MapLayoutDatabase.All.TryGetValue(TerrainMap.ZoneId ?? "", out var layout);
            var anchor = layout?.Anchors?.FirstOrDefault(kv => kv.Key.StartsWith("exit:"));
            if (anchor == null || anchor.Value.Key == null) return;
            var start = TerrainMap.FindNearestEnterable(anchor.Value.Value.x, anchor.Value.Value.y);
            var dist = new Dictionary<(int, int), int> { [start] = 0 }; var q = new Queue<(int x, int y)>(); q.Enqueue(start);
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                foreach (var nb in TerrainMap.GetAdjacentCoords(c.x, c.y))
                    if (!dist.ContainsKey(nb) && TerrainMap.CanEnterTile(nb.x, nb.y)) { dist[nb] = dist[c] + 1; q.Enqueue(nb); }
            }
            var far = dist.Where(kv => Bare(kv.Key.Item1, kv.Key.Item2)).OrderByDescending(kv => kv.Value).Take(6).ToList();
            if (far.Count == 0) return;
            var (x, y) = far[Random.Range(0, far.Count)].Key;
            Put(x, y, ZoneLayoutRecipes.IsUpstairs(TerrainMap.ZoneId) ? "stash_chest" : "stash_footlocker", "Stash", Rich(tier + 1), "Left at the very end of the place, where few get to.");
        }

        /// <summary>Every 7 days the rolled stashes move (TurnLoop.TickAwareness). Also hands a key-locked stash's key to a carrier.</summary>
        public static void Tick()
        {
            if (TerrainMap.ZoneId == null || TerrainMap.InteractiveObjects == null) return;
            if (RolledWeek.TryGetValue(TerrainMap.ZoneId, out var w) && w != WeekNow)
            {
                foreach (var o in TerrainMap.InteractiveObjects.Where(o => o.Rolled).ToList())
                {
                    TerrainMap.InteractiveObjects.Remove(o);
                    var t = TerrainMap.GetTile(o.X, o.Y);
                    if (t.Look != null && (t.Look.StartsWith("stash_") || t.Look == "papers")) { t.Look = null; t.Type = TileType.Empty; TerrainMap.DirtyTiles.Add((o.X, o.Y)); }
                }
                foreach (var t in TerrainMap.Tiles) if (t != null && t.Look != null && t.Look.StartsWith("stash_") && t.Look.EndsWith("_open")) { }   // opened authored stashes stay open
                foreach (var e in EncounterState.Enemies) if (e.Carries != null && e.Carries.StartsWith($"stash:{TerrainMap.ZoneId}:")) e.Carries = null;
                Roll();
                return;
            }
            foreach (var o in TerrainMap.InteractiveObjects)
            {
                if (o.Lock != "key") continue;
                string id = KeyId(o);
                if (MacroState.Current.Keys.Contains(id) || EncounterState.Enemies.Any(e => e.IsAlive() && e.Carries == id)) continue;
                var carrier = EncounterState.Enemies.Where(e => e.IsAlive() && e.Carries == null && !(e is HumanEnemy) && !Companion.Is(e)).OrderByDescending(e => e.MaxHp).FirstOrDefault()
                    ?? EncounterState.Enemies.OfType<HumanEnemy>().Where(h => h.IsAlive() && h.Carries == null && !EnemyAI.FriendlyToPlayer(h)).OrderByDescending(h => CombatRules.RankIndex(h.Rank)).FirstOrDefault();
                if (carrier == null) continue;
                carrier.Carries = id;
                o.Description = carrier is HumanEnemy hh
                    ? $"A heavy padlock. Scratched on the lid: \"{hh.FactionName} took the key\"."
                    : $"A heavy padlock. Claw marks round it - and a note: \"the {carrier.Name} swallowed the key\".";
            }
        }

        /// <summary>E on a locked stash: true = handled here (locked, or a note read), nothing to roll.</summary>
        public static bool Blocked(InteractiveObjectInstance o)
        {
            var keys = MacroState.Current.Keys;
            if (o.Lock == "note")
            {
                keys.Add(o.Opens);
                GameLog.Write($"The note has three digits on it, underlined twice: {o.Code}. The keypad safe will open now.");
                TerrainMap.InteractiveObjects.Remove(o);
                return true;
            }
            if ((o.Lock == "key" || o.Lock == "code") && !keys.Contains(KeyId(o))) { GameLog.Write($"{o.Name}: {o.Description}"); return true; }
            if (o.Lock != null) o.Dc = 0;   // you have the key or the code: it just opens
            return false;
        }

        /// <summary>Weapon.Fire's death hook: the key comes out of whoever carried it.</summary>
        public static void OnKill(ICombatant dead)
        {
            if (!(dead is Mutant m) || m.Carries == null) return;
            MacroState.Current.Keys.Add(m.Carries);
            GameLog.Write(m is HumanEnemy
                ? $"[LOOT] A key on a wire tag in {m.Name}'s pocket - it fits a locked chest on this map."
                : $"[LOOT] Something glints in the {m.Name}'s gut: a key on a wire tag. It fits a locked chest on this map.");
            m.Carries = null;
        }
    }
}
