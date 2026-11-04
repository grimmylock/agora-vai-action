// Boss 2 (Yan, 2026-11-03: "right now it looks very even in space, it doesn't have a linear progression ... most rooms are locked,
// the few that are open have either a key, a hole in the wall going into the other room ... i'd like them to be engaging to explore
// and figure out"): the locks of Secret Lab Alpha's redesign (ArtSource/plans/lab_plans.py alpha_plan, a mission graph checked with
// the level-design skill's graph_check.py). One lock type, one look, everywhere:
//   staff badge - blue-lit doors (plan '1'); the badge is in the Security Office's locker ('3'), open from the start
//   power       - dead hazard-striped blast doors (plan '2'); the generator switch ('5') in the generator room needs a fuse, and the
//                 fuse is in a crate ('4') inside the badge-locked Labs wing
// Keys live in MacroState.Keys (saved with the world); a locked door is a wall to everyone until the player holds its key, then it's
// a door like any other (walk into it, it opens). Nothing is spent, so nothing can strand you.
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class LabLocks
    {
        public const string Staff = "alpha_staff", Fuse = "alpha_fuse", Power = "alpha_power";
        public const string Locker = "Security Locker", Crate = "Fuse Crate", Switch = "Generator Switch";

        public static bool Is(string key) => key == Staff || key == Fuse || key == Power;

        /// <summary>The key a door's look asks for, or null for an ordinary door (and for one already unlocked, "_on").</summary>
        public static string KeyOf(string look) => look == "door_badge" ? Staff : look == "door_power" ? Power : null;

        /// <summary>A door nobody can pass yet: it has a lock and the player doesn't hold the key.</summary>
        public static bool Locked(TerrainTile t)
        {
            if (t == null || t.Type != TileType.Door || t.Look == null) return false;
            var k = KeyOf(t.Look);
            return k != null && !(MacroState.Current != null && MacroState.Current.Keys.Contains(k));
        }

        static float _nagAt = -99f;
        /// <summary>Bumping into a locked door says what it wants (not more than every 2 seconds). True if (x, y) was one.</summary>
        public static bool Nag(int x, int y)
        {
            var t = x >= 0 && y >= 0 && x < TerrainMap.Width && y < TerrainMap.Height ? TerrainMap.GetTile(x, y) : null;
            if (!Locked(t)) return false;
            if (Time.time - _nagAt > 2f)
            {
                _nagAt = Time.time;
                GameLog.Write(t.Look == "door_badge" ? "The door is locked. A card reader blinks blue: STAFF BADGE REQUIRED."
                                                      : "The blast door is dead. A hazard light: NO POWER ON THIS LEVEL.");
            }
            return true;
        }

        /// <summary>What picking a key up says and does. Power turns every dead door's light green; the badge does the same for the card doors.</summary>
        public static void Granted(string key)
        {
            switch (key)
            {
                case Staff: GameLog.Write("[LAB] A staff badge on a blue lanyard, still warm from nobody's neck. The card readers will know it."); Relight("door_badge"); break;
                case Fuse: GameLog.Write("[LAB] A heavy fuse in a greased sleeve, stamped GEN-2. It would fit a generator."); break;
                case Power:
                    GameLog.Write("[LAB] The generator coughs, catches. Somewhere deep in the walls a lot of locks let go at once.");
                    Sfx.Play("explosion", null, 0.5f, 0.05f, 0.3f); Relight("door_power"); break;
            }
        }

        /// <summary>The doors of one kind stop looking locked (their look gets "_on": a green light) - they're ordinary doors now.</summary>
        static void Relight(string look)
        {
            for (int y = 0; y < TerrainMap.Height; y++)
                for (int x = 0; x < TerrainMap.Width; x++)
                {
                    var t = TerrainMap.GetTile(x, y);
                    if (t.Type == TileType.Door && t.Look == look) { t.Look = look + "_on"; TerrainMap.DirtyTiles.Add((x, y)); }
                }
        }

        /// <summary>Called when a map is built: doors whose key the player already holds start unlocked (a save, a second visit).</summary>
        public static void Refresh()
        {
            if (MacroState.Current == null) return;
            foreach (var key in new[] { Staff, Power })
                if (MacroState.Current.Keys.Contains(key)) Relight(key == Staff ? "door_badge" : "door_power");
        }

        static readonly InteractiveObjectDef SwitchDef = new InteractiveObjectDef { Name = Switch, Stat = new[] { "Strength" }, Dc = 0,
            Description = "The generator's main switch. The fuse slot is empty: you'd need a fuse from somewhere in the labs." };
        static readonly InteractiveObjectDef LockerDef = new InteractiveObjectDef { Name = Locker, Stat = new[] { "Strength" }, Dc = 0,
            Reward = new InteractiveReward { Ru = 60 }, Description = "A dented steel locker, SECURITY stencilled on the door." };
        static readonly InteractiveObjectDef CrateDef = new InteractiveObjectDef { Name = Crate, Stat = new[] { "Strength" }, Dc = 0,
            Description = "A crate of electrical spares." };

        /// <summary>The map's interactive objects for the three named stashes; false for any other name.</summary>
        public static bool Spawn(string name, int x, int y)
        {
            InteractiveObjectDef db; string look, grants, requires = null;
            if (name == Locker) { db = LockerDef; look = "stash_footlocker"; grants = Staff; }
            else if (name == Crate) { db = CrateDef; look = "stash_toolbox"; grants = Fuse; }
            else if (name == Switch) { db = SwitchDef; look = "labswitch"; grants = Power; requires = Fuse; }
            else return false;
            TerrainMap.InteractiveObjects.Add(new InteractiveObjectInstance
            {
                X = x, Y = y, Name = db.Name, Stat = db.Stat, Dc = 0, Reward = db.Reward, Description = db.Description, GrantsKey = grants, Requires = requires,
            });
            var t = TerrainMap.Tiles[y, x];
            if (t.Look == null) { t.Look = look; if (look != "labswitch") t.Type = TileType.Difficult; }   // boxes are climbed over; the switch is a panel on the floor
            return true;
        }
    }
}
