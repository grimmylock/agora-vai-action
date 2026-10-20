// Batch 123 (Yan: "maybe we need to take a look at the UI next, for dialogue and interactions"): what you can do right
// here, shown as small key chips over the thing itself (HudView.Prompts draws them). Before this, E/F only said what was
// nearby after you pressed them, and bodies, items and props had no hint that they could be clicked at all.
using System.Collections.Generic;
using System.Linq;
using GameData;

namespace GameCore
{
    public static class InteractionPrompts
    {
        public struct Prompt { public int X, Y; public string Key, Label; }

        static int D(Player p, int x, int y) => System.Math.Max(System.Math.Abs(p.X - x), System.Math.Abs(p.Y - y));

        /// <summary>The prompts for where the player stands (nearest first, at most `max`); `hover` is the tile under the
        /// mouse (props only prompt when you point at them, or a room full of crates would be all chips).</summary>
        public static List<Prompt> Near(Player p, (int x, int y)? hover = null, int max = 3)
        {
            var list = new List<Prompt>();
            if (p == null || TerrainMap.Tiles == null) return list;
            void Add(int x, int y, string key, string label) { if (!list.Any(q => q.X == x && q.Y == y)) list.Add(new Prompt { X = x, Y = y, Key = key, Label = label }); }

            // E: containers, locks, stashes (InteractionEngine; they're within one tile), and the two special machines
            var friend = Rt.DownedNear(p);
            if (friend != null) Add(friend.X, friend.Y, "HOLD E", "Revive");                          // co-op: a downed friend
            foreach (var o in TerrainMap.InteractiveObjects) if (D(p, o.X, o.Y) <= 1) Add(o.X, o.Y, "E", o.Name);
            foreach (var l in TerrainMap.Landmarks)
            {
                if (D(p, l.X, l.Y) > 2) continue;
                if (l.Name == GarrisonRaid.Armory && GarrisonRaid.InBase(p)) Add(l.X, l.Y, "E", "Armory");
                if (TestingGround.Here && l.Name == TestingGround.Armory) Add(l.X, l.Y, "E", "Armory: any gear");
                if (TestingGround.Here && l.Name == TestingGround.Spawner) Add(l.X, l.Y, "E", "Spawn enemies");
                if (l.Name == G7Lab.Machine && MacroState.Current.CurrentZoneId == G7Lab.Zone) Add(l.X, l.Y, "E", "Feed the synthesizer");
            }
            // F: an anomaly you can comb for an artifact
            var an = AnomalyEngine.SearchableNear(p);
            if (an != null) Add(an.X, an.Y, "F", "Search the " + an.DisplayName);
            // click: bodies with something on them, things on the ground
            foreach (var e in EncounterState.Enemies.OfType<HumanEnemy>())
                if (!e.IsAlive() && D(p, e.X, e.Y) <= 2 && e.Inventory.Items.Count > 0) Add(e.X, e.Y, "CLICK", "Search the body");
            foreach (var l in Melee.InCover) if (D(p, l.X, l.Y) <= 1) Add(l.X, l.Y, "E", "Pull out the " + l.Item.Name);   // RT 7a
            foreach (var g in TerrainMap.GroundItems)
                if (D(p, g.X, g.Y) <= 2) Add(g.X, g.Y, "CLICK", "Pick up " + (g.Item?.Name ?? "item"));
            // the way out: walk onto it
            foreach (var x in TerrainMap.Exits)
                if (D(p, x.X, x.Y) <= 2)
                {
                    var z = MacroMapDatabase.Find(x.TargetZoneId);
                    bool stairs = z != null && (z.Upstairs || ZoneLayoutRecipes.IsUpstairs(MacroState.Current.CurrentZoneId));
                    Add(x.X, x.Y, "WALK", stairs ? "Stairs" : "To " + (z?.Name ?? x.TargetZoneId));
                }
            // a prop you point at next to you
            if (hover.HasValue && D(p, hover.Value.x, hover.Value.y) == 1 && Props.Movable(hover.Value.x, hover.Value.y))
            {
                var t = TerrainMap.GetTile(hover.Value.x, hover.Value.y);
                string what = t.Look == "desk" ? "Tip the desk over" : t.Type == TileType.Barrel || t.Look == "rustbarrel" ? "Kick the barrel" : "Shove it";
                Add(hover.Value.x, hover.Value.y, "CLICK", what);
            }
            return list.OrderBy(q => D(p, q.X, q.Y)).Take(max).ToList();
        }
    }
}
