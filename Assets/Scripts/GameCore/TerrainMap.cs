// Ported from script.js: `let terrainMap = [];` plus createEmptyTerrainMap, getTerrainTile,
// isSolidTile, isScatterCoverTile, canEnterTile, getAdjacentCoords, applyAuthoredLayout,
// applyProceduralObstacles, buildTerrainForZone, generateTerrainMap, spawnInteractiveObjects,
// getInteractiveObjectAt, getNearbyInteractiveObject.
//
// Kept as a static class (mirroring the bare `terrainMap` global) rather than an instance, since
// script.js only ever had one active tactical grid at a time. If Unity ends up wanting multiple
// concurrent grids (e.g. previews), this can be instantiated instead.
//
// Grid size is now per-zone (Width/Height), not a fixed constant - see GameConstants.GridWidth
// for the fallback used when a zone has no authored MapLayoutDatabase entry.
using System;
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class TerrainMap
    {
        public static int Width = GameConstants.GridWidth;
        public static int Height = GameConstants.GridHeight;

        public static TerrainTile[,] Tiles; // [y, x], matches terrainMap[y][x] indexing in the JS
        public static List<InteractiveObjectInstance> InteractiveObjects = new List<InteractiveObjectInstance>();
        public static List<ZoneExit> Exits = new List<ZoneExit>();
        public static string ZoneId;          // Batch 37: whose map is loaded (the A-Life tick only touches it when it's the current zone)
        // New (Batch 12): items dropped on the ground - see GroundItem.cs/DropItem below.
        public static List<GroundItem> GroundItems = new List<GroundItem>();
        // New (Batch 7): this zone's named landmarks (LandmarkDatabase), placed once and persisted
        // like InteractiveObjects/Tiles - see SpawnLandmarks below.
        public static List<LandmarkInstance> Landmarks = new List<LandmarkInstance>();

        /// <summary>Recomputes the current zone's walkable exit points (see ZoneExits.cs) against
        /// whatever terrain is currently loaded. Called at the end of BuildTerrainForZone, and
        /// again by ZoneTravel.SearchArea() right after discovering a hidden connection so a
        /// newly-found route becomes walkable immediately instead of only on next zone entry.</summary>
        public static void RecomputeExits(string zoneId = null)
        {
            var zone = MacroMapDatabase.Find(MacroMapDatabase.MapOf(zoneId ?? ZoneId ?? MacroState.Current.CurrentZoneId));
            Exits = zone != null ? ZoneExitPlacer.ComputeAndCarveExits(zone) : new List<ZoneExit>();
        }

        // ------------------------------------------------------------------ the map overhaul: regions of a tier map

        static MapLayout Layout(string mapId) => mapId != null && MapLayoutDatabase.All.TryGetValue(mapId, out var l) ? l : null;

        /// <summary>The region (a zone id) a tile of the loaded map belongs to: on a tier map, one of its old zones;
        /// on any other map, the map's own zone.</summary>
        public static string RegionAt(int x, int y)
        {
            var l = Layout(ZoneId);
            if (l?.Regions == null || y < 0 || x < 0 || y >= l.Regions.Length || x >= l.Regions[y].Length) return ZoneId;
            return l.Regions[y][x] ?? ZoneId;
        }

        static readonly Dictionary<string, int> _regionArea = new Dictionary<string, int>();
        static readonly Dictionary<string, (int x, int y)> _regionCenter = new Dictionary<string, (int x, int y)>();

        /// <summary>How many tiles a region covers on its map (a whole map's area for an ordinary zone).</summary>
        public static int RegionArea(string regionId)
        {
            if (regionId == null) return 0;
            if (_regionArea.TryGetValue(regionId, out var a)) return a;
            var l = Layout(MacroMapDatabase.MapOf(regionId));
            if (l?.Tiles == null) return _regionArea[regionId] = 0;
            int n = 0;
            if (l.Regions == null) n = l.Tiles.Length * l.Tiles[0].Length;
            else foreach (var row in l.Regions) foreach (var r in row) if (r == regionId || r == null && regionId == MacroMapDatabase.MapOf(regionId)) n++;
            return _regionArea[regionId] = n;
        }

        /// <summary>A walkable tile near the middle of a region of the loaded map (where squads walk to when they
        /// move from one region to the next).</summary>
        public static (int x, int y) RegionCenter(string regionId)
        {
            string key = ZoneId + "/" + regionId;
            if (_regionCenter.TryGetValue(key, out var c)) return c;
            long sx = 0, sy = 0; int n = 0;
            for (int y = 0; y < Height; y++) for (int x = 0; x < Width; x++) if (RegionAt(x, y) == regionId) { sx += x; sy += y; n++; }
            if (n == 0) return FindNearestEnterable(Width / 2, Height / 2);
            int cx = (int)(sx / n), cy = (int)(sy / n), best = int.MaxValue; var pick = (cx, cy);
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    if (RegionAt(x, y) != regionId || !CanEnterTile(x, y)) continue;
                    int d = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                    if (d < best) { best = d; pick = (x, y); }
                }
            return _regionCenter[key] = pick;
        }

        /// <summary>A random walkable tile inside a region of the loaded map, at least `minDist` from (fx, fy) when it can.</summary>
        public static (int x, int y)? RandomTileIn(string regionId, int fx = -1, int fy = -1, int minDist = 0)
        {
            for (int tries = 0; tries < 400; tries++)
            {
                int x = UnityEngine.Random.Range(0, Width), y = UnityEngine.Random.Range(0, Height);
                if (!CanEnterTile(x, y) || RegionAt(x, y) != regionId) continue;
                if (tries < 300 && fx >= 0 && Math.Max(Math.Abs(x - fx), Math.Abs(y - fy)) < minDist) continue;
                return (x, y);
            }
            return null;
        }

        /// <summary>A pinned arrival spot for a region (the "arrive:region" anchor), for New Game and loads.</summary>
        public static (int x, int y)? ArrivalAnchor(string regionId)
        {
            var l = Layout(ZoneId);
            if (l?.Anchors != null && regionId != null && l.Anchors.TryGetValue("arrive:" + regionId, out var a)) return a;
            return null;
        }

        /// <summary>Ported from createEmptyTerrainMap(). Uses the current Width/Height - set those
        /// first (see BuildTerrainForZone) if a specific zone's size is needed.</summary>
        public static void CreateEmpty()
        {
            Tiles = new TerrainTile[Height, Width];
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    Tiles[y, x] = new TerrainTile();
        }

        /// <summary>Ported from getTerrainTile(x, y). Off-grid coordinates report as Solid, matching the JS.
        /// Lazily initializes Tiles if queried before CreateEmpty()/BuildTerrainForZone() has run —
        /// Unity doesn't guarantee MonoBehaviour init order the way the original's synchronous
        /// top-to-bottom script execution did, so a caller could otherwise hit this before the grid exists.</summary>
        public static TerrainTile GetTile(int x, int y)
        {
            if (Tiles == null) CreateEmpty();

            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return new TerrainTile { Type = TileType.Solid };
            return Tiles[y, x] ?? new TerrainTile();
        }

        public static bool IsSolidTile(int x, int y) => GetTile(x, y).Type == TileType.Solid;
        public static int GetHeight(int x, int y) => GetTile(x, y).Height;
        public static bool BlocksSight(int x, int y)
        {
            var t = GetTile(x, y);
            if (t.Type == TileType.Solid && t.Look == "window") return Smoke.ContainsKey((x, y));   // Batch 32: you can see (and shoot) through windows
            return TileRules.BlocksSight(t.Type) || Smoke.ContainsKey((x, y));
        }

        /// <summary>Smoke grenade clouds: tile -> player turns left. Blocks LoS into, out of and
        /// through it; ticked down at the end of each player turn (Player.EndTurn).</summary>
        public static readonly Dictionary<(int x, int y), int> Smoke = new Dictionary<(int x, int y), int>();

        public static void AddSmoke(int cx, int cy, int radius, int turns)
        {
            for (int y = cy - radius; y <= cy + radius; y++)
                for (int x = cx - radius; x <= cx + radius; x++)
                    if (x >= 0 && y >= 0 && x < Width && y < Height) Smoke[(x, y)] = turns;
        }

        public static void TickSmoke()
        {
            foreach (var k in new List<(int x, int y)>(Smoke.Keys))
                if (--Smoke[k] <= 0) Smoke.Remove(k);
        }
        public static int CoverAcAt(int x, int y)
        {
            var t = GetTile(x, y);
            return t.Type == TileType.Solid && t.Look == "window" ? 2 : TileRules.CoverAc(t.Type);   // a window frame is hard cover
        }

        /// <summary>Ported from canEnterTile(x, y). Closed doors are enterable - stepping in opens
        /// them (TacticalGridSystem.AttemptMove) - so A* routes through buildings normally.</summary>
        public static bool CanEnterTile(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height && !TileRules.BlocksMovement(GetTile(x, y).Type);
        }

        /// <summary>The living unit standing on a tile (player or enemy), ignoring `except`.
        /// Corpses don't block.</summary>
        public static ICombatant UnitAt(int x, int y, ICombatant except = null)
        {
            var p = Player.Current;
            if (p != null && p != except && p.IsAlive() && p.X == x && p.Y == y) return p;
            foreach (var e in EncounterState.Enemies)
                if (e != except && e.IsAlive() && e.X == x && e.Y == y) return e;
            return null;
        }

        /// <summary>Tiles whose look changed since TacticalGridView last drew them (doors opened,
        /// cover shot to rubble, barrels blown). The view drains this every frame.</summary>
        public static readonly HashSet<(int x, int y)> DirtyTiles = new HashSet<(int x, int y)>();

        /// <summary>Changes a live tile's type (clearing its damage) and flags it for redraw.</summary>
        public static void SetTileType(int x, int y, TileType type, string look = null)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return;
            var t = GetTile(x, y);
            t.Type = type;
            t.Damage = 0;
            t.Look = look;
            DirtyTiles.Add((x, y));
        }

        /// <summary>Cover Integrity (file 00 §6): takes `amount` off a destructible tile. At 0,
        /// cover becomes rubble (difficult terrain, no cover), a door is left hanging open, and a
        /// barrel explodes. Returns true if the tile broke. `attacker` is credited with anything
        /// an explosion kills.</summary>
        public static bool DamageTile(int x, int y, int amount, ICombatant attacker = null)
        {
            var t = GetTile(x, y);
            int max = TileRules.MaxIntegrity(t);
            if (max <= 0 || amount <= 0 || x < 0 || x >= Width || y < 0 || y >= Height) return false;
            t.Damage += amount;
            DirtyTiles.Add((x, y));
            if (t.Damage < max)
            {
                GameLog.Write($"Cover at ({x}, {y}) takes a hit ({max - t.Damage}/{max} left).");
                return false;
            }

            var was = t.Type;
            switch (was)
            {
                case TileType.Door:
                    SetTileType(x, y, TileType.DoorOpen, "broken");
                    Sfx.Play("door_break", TacticalGridView.GridToWorld(x, y));
                    GameLog.Write($"The door at ({x}, {y}) is blown off its hinges.");
                    break;
                case TileType.Barrel:
                    SetTileType(x, y, TileType.Difficult, "rubble");
                    GameLog.Write($"The barrel at ({x}, {y}) explodes!");
                    Explosions.Blast(x, y, Explosions.BarrelRadius, Explosions.BarrelDamage, attacker, "barrel blast");
                    break;
                default:
                    SetTileType(x, y, TileType.Difficult, "rubble");
                    GameLog.Write($"The cover at ({x}, {y}) is shot to rubble.");
                    break;
            }
            return true;
        }

        /// <summary>Not from the JS - added now that authored layouts (see MapLayoutDatabase) can
        /// wall off a map's border/buildings/hulls, unlike the old ~3.6%-density random scatter
        /// which almost never blocked a computed spawn point. Every enemy spawn point ultimately
        /// funnels through Mutant's constructor (HumanEnemy : Mutant), so snapping there - the one
        /// shared choke point - fixes every scattered spawn-position formula in EncounterGeneration/
        /// TurnLoop/AnomalyEngine at once rather than patching each call site.</summary>
        public static (int x, int y) FindNearestEnterable(int x, int y, int maxRadius = 6)
        {
            if (CanEnterTile(x, y)) return (x, y);
            for (int r = 1; r <= maxRadius; r++)
            {
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) continue; // ring only
                        if (CanEnterTile(x + dx, y + dy)) return (x + dx, y + dy);
                    }
            }
            return (x, y); // nothing enterable nearby - best effort, leave caller's original spot
        }

        /// <summary>Ported from getAdjacentCoords(x, y) — the 8 surrounding cells.</summary>
        public static List<(int x, int y)> GetAdjacentCoords(int x, int y)
        {
            var coords = new List<(int x, int y)>();
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    coords.Add((x + dx, y + dy));
                }
            return coords;
        }

        /// <summary>
        /// Ported from applyAuthoredLayout(zoneId), extended to size the grid to the layout's own
        /// dimensions (per-zone, no longer a fixed 32x24) and to apply per-tile Height when the
        /// layout provides one. Returns true if a matching authored layout from
        /// GameData.MapLayoutDatabase was applied; false if the caller should fall back to
        /// procedural generation. Caller must NOT call CreateEmpty() first - this method sizes and
        /// creates the grid itself since the authored layout dictates the dimensions.
        /// </summary>
        public static bool ApplyAuthoredLayout(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId)) return false;
            if (!MapLayoutDatabase.All.TryGetValue(zoneId, out var layout) || layout?.Tiles == null || layout.Tiles.Length == 0) return false;

            int h = layout.Tiles.Length;
            int w = layout.Tiles[0].Length;
            if (!layout.Tiles.All(row => row.Length == w))
            {
                Debug.LogWarning($"[MAP] Authored layout for zone \"{zoneId}\" has ragged rows. Falling back to procedural terrain.");
                return false;
            }

            Width = w;
            Height = h;
            Tiles = new TerrainTile[Height, Width];
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    int height = layout.Heights != null && y < layout.Heights.Length && x < layout.Heights[y].Length
                        ? layout.Heights[y][x]
                        : 0;
                    Tiles[y, x] = new TerrainTile
                    {
                        Type = ParseTileType(layout.Tiles[y][x]),
                        Height = height,
                        Look = layout.Looks?[y][x],
                        Ground = layout.Grounds?[y][x] != null && Enum.TryParse<GroundType>(layout.Grounds[y][x], out var g) ? g : ZoneThemes.GroundAt(zoneId, x, y),
                    };
                }
            return true;
        }

        private static TileType ParseTileType(string s)
        {
            if (Enum.TryParse<TileType>(s, out var t)) return t;
            return TileType.Empty;
        }

        /// <summary>Ported from applyProceduralObstacles(reserved) — random Solid/ScatterCover scatter.
        /// Obstacle counts now scale with grid area (the original's 10/18 counts were tuned for the
        /// fixed 32x24=768-tile board) so bigger procedural fallback maps don't end up almost
        /// entirely empty. Attempts are capped (not in the original) since Unity's grid
        /// dimensions/reserved-tile counts aren't guaranteed to leave as much free space as the
        /// original's fixed board — without a cap, a sufficiently crowded grid would hang the main
        /// thread forever.</summary>
        public static void ApplyProceduralObstacles(List<(int x, int y)> reserved)
        {
            System.Func<(int x, int y)> randomCoord = () => (UnityEngine.Random.Range(0, Width), UnityEngine.Random.Range(0, Height));

            float areaScale = (Width * Height) / 768f;
            int solidTarget = Mathf.Max(10, Mathf.RoundToInt(10 * areaScale));
            int coverTarget = Mathf.Max(18, Mathf.RoundToInt(18 * areaScale));

            int count = 0;
            int attempts = 0;
            int maxAttempts = Mathf.Max(200, solidTarget * 20);
            while (count < solidTarget && attempts < maxAttempts)
            {
                attempts++;
                var coord = randomCoord();
                if (reserved.Any(pos => pos.x == coord.x && pos.y == coord.y) || !CanEnterTile(coord.x, coord.y))
                    continue;
                Tiles[coord.y, coord.x].Type = TileType.Solid;
                count++;
            }

            count = 0;
            attempts = 0;
            maxAttempts = Mathf.Max(300, coverTarget * 20);
            while (count < coverTarget && attempts < maxAttempts)
            {
                attempts++;
                var coord = randomCoord();
                if (reserved.Any(pos => pos.x == coord.x && pos.y == coord.y) || !CanEnterTile(coord.x, coord.y))
                    continue;
                Tiles[coord.y, coord.x].Type = TileType.ScatterCover;
                count++;
            }
        }

        /// <summary>Ported from buildTerrainForZone(zoneId).
        /// Interactive objects are now cached per-zone alongside the tiles (see
        /// PersistedZoneInteractiveObjects) — previously only Tiles was cached, so revisiting a
        /// previously-generated zone left InteractiveObjects holding whatever the *last visited*
        /// zone had spawned instead of that zone's own objects (or an empty list).</summary>
        public static void BuildTerrainForZone(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId)) zoneId = MacroState.Current.CurrentZoneId;
            zoneId = MacroMapDatabase.MapOf(zoneId);   // a region loads its tier's map
            ZoneId = zoneId;

            if (MacroState.Current.PersistedZoneTerrain.TryGetValue(zoneId, out var persistedTiles))
            {
                Tiles = persistedTiles;
                Height = Tiles.GetLength(0);
                Width = Tiles.GetLength(1);
                InteractiveObjects = MacroState.Current.PersistedZoneInteractiveObjects.TryGetValue(zoneId, out var persistedObjects)
                    ? persistedObjects
                    : new List<InteractiveObjectInstance>();
                Landmarks = MacroState.Current.PersistedZoneLandmarks.TryGetValue(zoneId, out var persistedLandmarks)
                    ? persistedLandmarks
                    : new List<LandmarkInstance>();
                GroundItems = MacroState.Current.PersistedZoneGroundItems.TryGetValue(zoneId, out var persistedGroundItems)
                    ? persistedGroundItems
                    : new List<GroundItem>();
            }
            else
            {
                if (!ApplyAuthoredLayout(zoneId))
                {
                    Width = GameConstants.GridWidth;
                    Height = GameConstants.GridHeight;
                    CreateEmpty();
                    for (int y = 0; y < Height; y++)
                        for (int x = 0; x < Width; x++)
                            Tiles[y, x].Ground = ZoneThemes.GroundAt(zoneId, x, y);

                    var reserved = new List<(int x, int y)>();
                    if (Player.Current != null) reserved.Add((Player.Current.X, Player.Current.Y));
                    foreach (var enemy in EncounterState.Enemies) reserved.Add((enemy.X, enemy.Y));

                    ApplyProceduralObstacles(reserved);
                }

                // Bigger authored maps warrant more than the original's flat 3 interactive objects.
                // Batch 30: maps are 3x3 bigger now; about twice the old loot per zone, not nine times.
                Props.Settle();   // Batch 103: standing desks are no cover
                int objectCount = Mathf.Max(3, Mathf.RoundToInt(3 * (Width * Height) / 768f / 4.5f));
                SpawnInteractiveObjects(objectCount);
                SpawnLandmarks(zoneId);
                Checkpoints.Dress(zoneId);   // Batch 126
                GroundItems = new List<GroundItem>(); // a freshly generated zone has nothing dropped on it yet
                PersistZoneTerrain(zoneId);
                // Play on the persisted copy itself, so doors opened and cover destroyed on this
                // first visit are still that way when you come back (revisits already do this).
                Tiles = MacroState.Current.PersistedZoneTerrain[zoneId];
            }

            DirtyTiles.Clear();
            Smoke.Clear();
            Posts.Clear();   // Batch 110: squads' plans belong to the old map
            RecomputeExits(zoneId);
        }

        public static void PersistZoneTerrain(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId) || Tiles == null) return;
            var copy = new TerrainTile[Height, Width];
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    copy[y, x] = Tiles[y, x].Clone();
            MacroState.Current.PersistedZoneTerrain[zoneId] = copy;
            MacroState.Current.PersistedZoneInteractiveObjects[zoneId] = InteractiveObjects;
            MacroState.Current.PersistedZoneLandmarks[zoneId] = Landmarks;
            MacroState.Current.PersistedZoneGroundItems[zoneId] = GroundItems;
        }

        /// <summary>New (Batch 12): drops an item at (x,y) on whatever zone is currently loaded -
        /// stacks onto an existing ground item with the same Id at that exact tile (mirrors
        /// HumanLootSystem/Player's own id-based stacking) instead of piling up duplicate entries.</summary>
        public static void DropItem(int x, int y, InventoryItem item)
        {
            var existing = GroundItems.FirstOrDefault(g => g.X == x && g.Y == y && g.Item.Id == item.Id);
            if (existing != null) existing.Item.Qty += item.Qty > 0 ? item.Qty : 1;
            else GroundItems.Add(new GroundItem { X = x, Y = y, Item = item });
        }

        /// <summary>Ported convention from GetInteractiveObjectAt(x, y) - everything dropped on this
        /// exact tile, picked up all at once (see PlayerController).</summary>
        public static List<GroundItem> GetGroundItemsAt(int x, int y) =>
            GroundItems.Where(g => g.X == x && g.Y == y).ToList();

        /// <summary>Ported from generateTerrainMap(zoneId = null).</summary>
        public static void GenerateTerrainMap(string zoneId = null) => BuildTerrainForZone(zoneId);

        /// <summary>Ported from getInteractiveObjectAt(x, y).</summary>
        public static InteractiveObjectInstance GetInteractiveObjectAt(int x, int y) =>
            InteractiveObjects.FirstOrDefault(obj => obj.X == x && obj.Y == y);

        /// <summary>Ported from getNearbyInteractiveObject(unit).</summary>
        public static InteractiveObjectInstance GetNearbyInteractiveObject(ICombatant unit) =>
            InteractiveObjects.FirstOrDefault(obj => Math.Max(Math.Abs(unit.X - obj.X), Math.Abs(unit.Y - obj.Y)) <= 1);

        /// <summary>Ported from spawnInteractiveObjects(count = 3).
        /// Reassigns InteractiveObjects to a fresh list (rather than mutating in place via
        /// .Clear()) because PersistZoneTerrain now stores this list by reference per zone —
        /// clearing the existing list in place would corrupt whichever previously-visited zone
        /// still holds a reference to it.</summary>
        public static void SpawnInteractiveObjects(int count = 3)
        {
            InteractiveObjects = new List<InteractiveObjectInstance>();
            // Batch 124: stashes are drawn as what they are (Look "stash_<kind>", "_open" once looted); Batch 126: the scattered ones are
            // Stashes.Roll's (about 8 a map, mostly locked, moved weekly) - the hand-placed note finds below stay where the recipe put them
            void Put(int x, int y, InteractiveObjectDef db, string look, string name = null, string desc = null, string key = null)
            {
                InteractiveObjects.Add(new InteractiveObjectInstance
                {
                    X = x, Y = y, Name = name ?? db.Name, Stat = db.Stat, Dc = db.Dc, Reward = db.Reward, Description = desc ?? db.Description, GrantsKey = key,
                });
                var t = Tiles[y, x];
                if (t.Look == null) { t.Look = look; if (look != "stash_pda" && look != "stash_duffel") t.Type = TileType.Difficult; }   // boxes are climbed over
            }

            // the map overhaul: a small find's stash (a note led you here) is always where the recipe put it
            if (ZoneId != null && MapLayoutDatabase.All.TryGetValue(ZoneId, out var authored) && authored?.Anchors != null)
                foreach (var kv in authored.Anchors)
                {
                    if (!kv.Key.StartsWith("stash:")) continue;
                    string name = kv.Key.Substring(6);
                    var (sx0, sy0) = FindNearestEnterable(kv.Value.x, kv.Value.y);
                    for (int r = 0, found = 0; r <= 5 && found == 0; r++)            // the nearest bare floor, not on top of a crate or rubble
                        for (int dy = -r; dy <= r && found == 0; dy++)
                            for (int dx = -r; dx <= r; dx++)
                            {
                                int qx = kv.Value.x + dx, qy = kv.Value.y + dy;
                                if (qx < 1 || qy < 1 || qx >= Width - 1 || qy >= Height - 1 || Tiles[qy, qx].Type != TileType.Empty || Tiles[qy, qx].Look != null) continue;
                                sx0 = qx; sy0 = qy; found = 1; break;
                            }
                    string look = name.Contains("Footlocker") ? "stash_footlocker" : new[] { "stash_cache", "stash_chest", "stash_duffel" }[StableIndex(name, 3)];
                    Put(sx0, sy0, InteractiveObjectDatabase.All["Footlocker"], look, name, "Somebody hid this here on purpose.", StashKeys.TryGetValue(name, out var k) ? k : null);
                }
            Stashes.Roll();   // Batch 126: about 8 per map in all, most of them locked, moved every 7 days
        }

        /// <summary>What the prompt and the log call a scattered stash: what it looks like.</summary>
        public static string StashName(string look) => look switch
        {
            "stash_footlocker" => "Footlocker", "stash_chest" => "Old Chest", "stash_duffel" => "Duffel Bag", "stash_toolbox" => "Toolbox",
            "stash_cache" => "Tarp Cache", "stash_safe" => "Keypad Safe", "stash_pda" => "Encrypted PDA", _ => null,
        };

        static int StableIndex(string s, int n) { unchecked { uint h = 2166136261; foreach (char c in s) { h ^= c; h *= 16777619; } return (int)(h % (uint)n); } }

        /// <summary>Batch 124: a stash you've been through shows open (its tile's "_open" look).</summary>
        public static void MarkOpened(InteractiveObjectInstance obj)
        {
            var t = GetTile(obj.X, obj.Y);
            if (t.Look == null || !t.Look.StartsWith("stash_") || t.Look.EndsWith("_open")) return;
            t.Look += "_open";
            DirtyTiles.Add((obj.X, obj.Y));
        }

        /// <summary>New (Batch 7): places this zone's named landmarks (LandmarkDatabase) on the
        /// grid - one per named place, scattered the same way SpawnInteractiveObjects already
        /// scatters loot (random walkable tile, not already occupied), just without a spawn count
        /// to roll since the zone's own landmark list already fixes how many there are. Landmarks
        /// with no matching database entry (a zone LandmarkDatabase doesn't cover) simply get none -
        /// not every internal zone id needs an authored entry to function.</summary>
        public static void SpawnLandmarks(string zoneId)
        {
            Landmarks = new List<LandmarkInstance>();
            var defs = MacroMapDatabase.RegionsOf(zoneId).SelectMany(r => LandmarkDatabase.ForZone(r.Id)).ToList();
            if (defs.Count == 0) defs = LandmarkDatabase.ForZone(zoneId);
            int attempts = 0;
            int maxAttempts = Mathf.Max(200, defs.Count * 40);
            MapLayoutDatabase.All.TryGetValue(zoneId ?? "", out var layout);
            foreach (var def in defs)
            {
                int x = 0, y = 0;
                bool placed = false;
                if (layout?.Anchors != null && layout.Anchors.TryGetValue(def.Name, out var anchor))
                {
                    (x, y) = FindNearestEnterable(anchor.x, anchor.y);
                    Landmarks.Add(new LandmarkInstance { X = x, Y = y, Name = def.Name, Description = def.Description, HiddenNodeId = def.HiddenNodeId });
                    continue;
                }
                while (attempts < maxAttempts)
                {
                    attempts++;
                    x = UnityEngine.Random.Range(0, Width);
                    y = UnityEngine.Random.Range(0, Height);
                    bool onPlayer = Player.Current != null && x == Player.Current.X && y == Player.Current.Y;
                    if (onPlayer || !CanEnterTile(x, y) || GetInteractiveObjectAt(x, y) != null || GetLandmarkAt(x, y) != null)
                        continue;
                    placed = true;
                    break;
                }
                if (!placed) continue; // grid too crowded - drop this one rather than overlap something
                Landmarks.Add(new LandmarkInstance { X = x, Y = y, Name = def.Name, Description = def.Description, HiddenNodeId = def.HiddenNodeId });
            }
        }

        /// <summary>Stashes that hold a key as well as their loot (MacroState.Keys opens ZoneDef.RequiresKey doors).</summary>
        public static readonly Dictionary<string, string> StashKeys = new Dictionary<string, string>
        {
            ["Headman's Strongbox"] = "flooded_bunker",
        };

        public static LandmarkInstance GetLandmarkAt(int x, int y) => Landmarks.FirstOrDefault(l => l.X == x && l.Y == y);

        public static LandmarkInstance GetNearbyLandmark(ICombatant unit) =>
            Landmarks.FirstOrDefault(l => Math.Max(Math.Abs(unit.X - l.X), Math.Abs(unit.Y - l.Y)) <= 1);
    }
}
