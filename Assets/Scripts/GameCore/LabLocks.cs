// Boss 2 (Yan, 2026-11-03: "right now it looks very even in space, it doesn't have a linear progression ... most rooms are locked,
// the few that are open have either a key, a hole in the wall going into the other room ... i'd like them to be engaging to explore
// and figure out"): the dungeons' locks (ArtSource/plans/lab_plans.py alpha_plan / depths_plan, each a mission graph checked on the
// plan grid). One lock type, one look, everywhere:
//   staff badge - blue-lit doors (plan '1'); the badge is in a locker ('3')
//   power       - dead hazard-striped blast doors (plan '2'); a switch ('5') that needs a fuse from a crate ('4')
//   drain       - black deep water (plan 'W', Sub-Station Depths): over your head until the pump switch ('6', needs power) runs
// Keys live in MacroState.Keys (saved with the world), one set per dungeon ("alpha_staff", "depths_power"...): a lock is a wall to
// everyone until the player holds that dungeon's key, then it's a door like any other (or knee-deep water). Nothing is spent, so
// nothing can strand you.
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class LabLocks
    {
        // Secret Lab Alpha's keys by name (the harness and older code use these)
        public const string Staff = "alpha_staff", Fuse = "alpha_fuse", Power = "alpha_power";
        public const string Locker = "Security Locker", Crate = "Fuse Crate", Switch = "Generator Switch";
        public const string Depths = "sub_station_depths", Depths1 = "sub_station_depths_1", Depths2 = "sub_station_depths_2", Depths3 = "sub_station_depths_3";

        /// <summary>A key-giving object in a plan: its name, look, the key kind it gives, the kind it needs, what it says.</summary>
        class Thing { public string Name, Look, Kind, Needs, Desc, Says; public int Ru; }

        static readonly Dictionary<string, string> Prefix = new Dictionary<string, string> { ["secret_lab_alpha"] = "alpha", [Depths] = "depths", [Depths1] = "depths", [Depths2] = "depths", [Depths3] = "depths", ["pumping_basement"] = "pump", ["secret_cave"] = "cave", ["smugglers_tunnel"] = "smug", ["chapel_crypt"] = "crypt", ["valley_mine"] = "mine", ["crusher_pit"] = "crusher", ["flooded_bunker"] = "bunker" };

        /// <summary>Which drain key a map's deep water answers to. The Depths' flooded levels each drain with their own pump; the station's
        /// basin only empties when the last level is drained (the eel boss, ACTION_DESIGN "Dodge bosses"). Any other map: "drain".</summary>
        static readonly Dictionary<string, string> Water = new Dictionary<string, string>
            { [Depths] = "drain3", [Depths1] = "drain1", [Depths2] = "drain2", [Depths3] = "drain3" };
        static string WaterKind(string zone) => Water.TryGetValue(zone ?? "", out var k) ? k : "drain";

        static readonly Dictionary<string, Dictionary<char, Thing>> Things = new Dictionary<string, Dictionary<char, Thing>>
        {
            ["secret_lab_alpha"] = new Dictionary<char, Thing>
            {
                ['3'] = new Thing { Name = Locker, Look = "stash_footlocker", Kind = "staff", Ru = 60, Desc = "A dented steel locker, SECURITY stencilled on the door.",
                                    Says = "[LAB] A staff badge on a blue lanyard, still warm from nobody's neck. The card readers will know it." },
                ['4'] = new Thing { Name = Crate, Look = "stash_toolbox", Kind = "fuse", Desc = "A crate of electrical spares.",
                                    Says = "[LAB] A heavy fuse in a greased sleeve, stamped GEN-2. It would fit a generator." },
                ['5'] = new Thing { Name = Switch, Look = "labswitch", Kind = "power", Needs = "fuse",
                                    Desc = "The generator's main switch. The fuse slot is empty: you'd need a fuse from somewhere in the labs.",
                                    Says = "[LAB] The generator coughs, catches. Somewhere deep in the walls a lot of locks let go at once." },
            },
            // Sub-Station Depths (Tier 3, under the Dried Lake): a flooded switching station. Badge in the dispatcher's locker -> the cable
            // gallery -> the battery room's breaker (needs the fuse from the workers' quarters) -> the pump room -> the basin drains -> the vault.
            [Depths] = new Dictionary<char, Thing>
            {
                ['3'] = new Thing { Name = "Dispatcher's Locker", Look = "stash_footlocker", Kind = "staff", Ru = 80, Desc = "A grey locker, the shift roster still taped to it.",
                                    Says = "[DEPTHS] A staff badge clipped inside the door, the photo bleached to nothing. The card readers down here still take it." },
                ['4'] = new Thing { Name = "Spares Crate", Look = "stash_toolbox", Kind = "fuse", Desc = "A crate of electrical spares under a bunk.",
                                    Says = "[DEPTHS] A cartridge fuse as long as your forearm, BATTERY BANK B on the tag." },
                ['5'] = new Thing { Name = "Battery Breaker", Look = "labswitch", Kind = "power", Needs = "fuse",
                                    Desc = "The battery bank's breaker. One fuse holder is empty and scorched.",
                                    Says = "[DEPTHS] The breaker slams home. The batteries hum, and down the halls the blast doors' lights wake up." },
            },
            // Pumping Station Basement (Tier 1, under the marshes; the lock-and-key redo of 2026-11-10, dungeons/pumping_basement.mmd): spares crate in the valve
            // gallery -> the breaker in the control room -> power opens the pump panel's door -> the pump -> the culvert's black water drains -> the outfall.
            ["pumping_basement"] = new Dictionary<char, Thing>
            {
                ['4'] = new Thing { Name = "Valve Spares Cabinet", Look = "stash_toolbox", Kind = "fuse", Desc = "A steel cabinet of spare valves and fittings, one drawer hanging open.",
                                    Says = "[PUMPS] A cartridge fuse in a rag, PUMP STN 2 on the tag. It would fit the control room's breaker." },
                ['5'] = new Thing { Name = "Pump Station Breaker", Look = "labswitch", Kind = "power", Needs = "fuse",
                                    Desc = "The station's breaker panel. One fuse holder is empty and scorched.",
                                    Says = "[PUMPS] The breaker slams home. Somewhere below the floor a relay clicks and the blast door's light turns amber." },
                ['6'] = new Thing { Name = "Culvert Pump Panel", Look = "labswitch", Kind = "drain", Needs = "power",
                                    Desc = "The pump panel for the drainage culvert, its lamps dark: no power reaches it.",
                                    Says = "[PUMPS] The pump shudders, then thumps. Down the culvert the black water starts to slide away." },
            },
            // Secret Cave (Tier 1, under the perimeter fields; dungeons/secret_cave.mmd): the smugglers dropped a slab across the tunnel behind them; a pry bar from their camp lifts it.
            ["secret_cave"] = new Dictionary<char, Thing>
            {
                ['4'] = new Thing { Name = "Smugglers' Crate", Look = "stash_toolbox", Kind = "pry", Desc = "A split wooden crate in the corner of the old camp, a bent tool sticking out of the straw.",
                                    Says = "[CAVE] A steel pry bar, bent and rust-red, still sharp at one end. It would lever a fallen slab." },
            },
            // Smugglers' Tunnel (Tier 1, from the depot's loading office; dungeons/smugglers_tunnel.mmd): an iron gate across the tunnel, padlocked; the key is in the guard post's footlocker.
            ["smugglers_tunnel"] = new Dictionary<char, Thing>
            {
                ['4'] = new Thing { Name = "Guard Post Footlocker", Look = "stash_footlocker", Kind = "gate", Desc = "A dented footlocker under the guard's bunk, the lid wired shut with a loop that has long since rusted through.",
                                    Says = "[TUNNEL] A heavy iron key on a loop of wire, a smeared GATE scratched in the bow. It would fit the padlock down the tunnel." },
            },
            // Chapel Crypt (Tier 1, under the chapel; dungeons/chapel_crypt.mmd): an iron gate across the burial passage (the key is in the priest's cell) and, beyond it, a sealed slab over the family
            // vault whose combination the ossuary's ledger gives.
            ["chapel_crypt"] = new Dictionary<char, Thing>
            {
                ['3'] = new Thing { Name = "Burial Ledger", Look = "stash_pda", Kind = "code", Desc = "A water-stained ledger chained to the ossuary wall, open to its last page.",
                                    Says = "[CRYPT] The last page is a column of dates, three of them ringed in the priest's hand. Dial them on the vault's slab and it would turn." },
                ['4'] = new Thing { Name = "Priest's Strongbox", Look = "stash_footlocker", Kind = "gate", Desc = "A small iron strongbox under the priest's cot, its hasp snapped.",
                                    Says = "[CRYPT] An iron key as long as a hand, the bow worn smooth by thumbs. It would fit the gate across the passage." },
            },
            // Valley Mine (Tier 2, off Black Valley's rim; dungeons/valley_mine.mmd): timbers fell across the main gallery; a pry bar from the tool shed levers them aside.
            ["valley_mine"] = new Dictionary<char, Thing>
            {
                ['4'] = new Thing { Name = "Tool Shed Rack", Look = "stash_toolbox", Kind = "pry", Desc = "A rack of mining tools on the shed wall, most of them rusted solid.",
                                    Says = "[MINE] A long steel pry bar, the end worn to a chisel edge. It would shift fallen timbers." },
            },
            // Crusher Pit (Tier 2, under Crusher Row; dungeons/crusher_pit.mmd): the Alpha locks. The foreman's locker in the oil sump holds the staff badge (the service tunnel's blue door), the spares crate in the cage the
            // fuse, the ram breaker in the control booth needs it; power wakes the dead blast door onto the pay vault.
            ["crusher_pit"] = new Dictionary<char, Thing>
            {
                ['3'] = new Thing { Name = "Foreman's Locker", Look = "stash_footlocker", Kind = "staff", Ru = 70, Desc = "A dented steel locker at the sump's far end, FOREMAN stencilled on the door in oil.",
                                    Says = "[PIT] A staff badge on a greasy lanyard, the photo gone to black. The card readers down here would still know it." },
                ['4'] = new Thing { Name = "Ram Spares Crate", Look = "stash_toolbox", Kind = "fuse", Desc = "A crate of hydraulic spares under the foreman's desk.",
                                    Says = "[PIT] A heavy cartridge fuse in a greased sleeve, RAM FEED 3 on the tag." },
                ['5'] = new Thing { Name = "Ram Breaker", Look = "labswitch", Kind = "power", Needs = "fuse",
                                    Desc = "The rams' breaker panel. One fuse holder is empty and scorched.",
                                    Says = "[PIT] The breaker slams home. Far off, the rams shudder once, and the blast door's lamp turns amber." },
            },
            // Flooded Bunker (Tier 1, behind the marsh blast door; dungeons/flooded_bunker.mmd): the Alpha locks. The duty locker in the barracks holds the staff badge (the armory's blue door), the armory the
            // fuse, the generator room's switch needs it; power wakes the dead door onto the flooded command level (and the hatch ladder's). The way INTO the bunker is the zone key "flooded_bunker" (Tier1Story).
            ["flooded_bunker"] = new Dictionary<char, Thing>
            {
                ['3'] = new Thing { Name = "Duty Locker", Look = "stash_footlocker", Kind = "staff", Ru = 50, Desc = "A dented steel locker at the head of a bunk, DUTY OFFICER stencilled on it.",
                                    Says = "[BUNKER] A staff badge on a faded lanyard, a face scratched out of the photo. The card readers still know it." },
                ['4'] = new Thing { Name = "Armory Spares Crate", Look = "stash_toolbox", Kind = "fuse", Desc = "A crate of electrical spares in the armory's back corner.",
                                    Says = "[BUNKER] A heavy fuse in a greased sleeve, GEN 1 on the tag. It would fit the generator's switch." },
                ['5'] = new Thing { Name = "Generator Switch", Look = "labswitch", Kind = "power", Needs = "fuse",
                                    Desc = "The generator's main switch. The fuse slot is empty and scorched.",
                                    Says = "[BUNKER] The generator coughs, catches. Somewhere above the flooded level a lot of locks let go at once." },
            },
            // the flooded levels under it (Yan, 2026-11-04): each level's pump drops its water and opens the stairs further down.
            // the last pump is gated by the eel (Stashes.Blocked: Story flag eel_fled)
            [Depths1] = new Dictionary<char, Thing> { ['6'] = new Thing { Name = "Cistern Pump", Look = "labswitch", Kind = "drain1", Needs = "power",
                Desc = "A sump pump's panel, its lamps dark: no power reaches this level.",
                Says = "[DEPTHS] The pump bangs into life. The black water slides off the stairs going down." } },
            [Depths2] = new Dictionary<char, Thing> { ['6'] = new Thing { Name = "Lower Pump", Look = "labswitch", Kind = "drain2", Needs = "drain1",
                Desc = "The lower pump's panel.",
                Says = "[DEPTHS] Somewhere below, a second pump answers. The water drops again." } },
            [Depths3] = new Dictionary<char, Thing> { ['6'] = new Thing { Name = "Main Sump Pump", Look = "labswitch", Kind = "drain3", Needs = "drain2",
                Desc = "The last pump, at the very bottom of the station.",
                Says = "[DEPTHS] The last pump screams, then settles. The whole station drains, all the way up to the basin." } },
        };

        static string Zone => TerrainMap.ZoneId;
        static string KeyIn(string zone, string kind) => Prefix.TryGetValue(zone ?? "", out var p) ? p + "_" + kind : null;
        /// <summary>This map's key of a kind ("staff", "fuse", "power", "drain"), or null if the map has no locks.</summary>
        public static string Key(string kind) => KeyIn(Zone, kind);
        /// <summary>Has this flooded level's pump run (its water is gone)?</summary>
        public static bool Drained(string zone) => Has(KeyIn(zone, WaterKind(zone)));
        static bool Has(string key) => key != null && MacroState.Current != null && MacroState.Current.Keys.Contains(key);

        public static bool Is(string key) => key != null && Prefix.Values.Any(p => key.StartsWith(p + "_"));

        /// <summary>The key a lock's look asks for, or null for an ordinary door (and for one already unlocked, "_on").</summary>
        public static string KeyOf(string look) => look == "rockfall" ? Key("pry") : look == "irongate" ? Key("gate") : look == "sealslab" ? Key("code") : look == "door_badge" ? Key("staff") : look == "door_power" ? Key("power") : look == "deepwater" ? Key(WaterKind(Zone)) : null;

        /// <summary>A lock nobody can pass yet: it has a lock and the player doesn't hold the key.</summary>
        public static bool Locked(TerrainTile t)
        {
            if (t == null || t.Look == null || !(t.Type == TileType.Door || t.Type == TileType.Difficult)) return false;
            var k = KeyOf(t.Look);
            return k != null && !Has(k);
        }

        static float _nagAt = -99f;
        /// <summary>Bumping into a lock says what it wants (not more than every 2 seconds). True if (x, y) was one.</summary>
        public static bool Nag(int x, int y)
        {
            var t = x >= 0 && y >= 0 && x < TerrainMap.Width && y < TerrainMap.Height ? TerrainMap.GetTile(x, y) : null;
            if (!Locked(t)) return false;
            if (Time.time - _nagAt > 2f)
            {
                _nagAt = Time.time;
                GameLog.Write(t.Look == "sealslab" ? "A great stone slab seals the way, a carved dial in its face. Somewhere in the crypt the numbers must be written down."
                            : t.Look == "irongate" ? "An iron gate across the tunnel, padlocked. Whoever kept the guard post will have had the key."
                            : t.Look == "rockfall" ? "A slab of fallen rock fills the tunnel, too heavy to shift by hand. Something to lever it with, maybe."
                            : t.Look == "door_badge" ? "The door is locked. A card reader blinks blue: STAFF BADGE REQUIRED."
                            : t.Look == "door_power" ? "The blast door is dead. A hazard light: NO POWER ON THIS LEVEL."
                            : "The water is black and over your head. Somewhere a pump would have to run to clear it.");
            }
            return true;
        }

        /// <summary>What picking a key up says and does: the badge and the power relight their doors green, the drain empties the deep water.</summary>
        public static void Granted(string key)
        {
            var thing = Things.SelectMany(z => z.Value.Values.Select(v => (z.Key, v))).FirstOrDefault(p => KeyIn(p.Key, p.v.Kind) == key);
            if (thing.v != null) GameLog.Write(thing.v.Says);
            if (key == Key("pry")) { Sfx.Play("explosion", null, 0.4f, 0.05f, 0.25f); Relight("rockfall"); }
            if (key == Key("gate")) { Sfx.Play("explosion", null, 0.25f, 0.05f, 0.15f); Relight("irongate"); }
            if (key == Key("code")) { Sfx.Play("explosion", null, 0.45f, 0.05f, 0.3f); Relight("sealslab"); }
            if (key == Key("staff")) Relight("door_badge");
            if (key == Key("power")) { Sfx.Play("explosion", null, 0.5f, 0.05f, 0.3f); Relight("door_power"); }
            if (key == Key(WaterKind(Zone))) { Sfx.Play("explosion", null, 0.35f, 0.05f, 0.2f); Whirlpools(key); Drain(); }
        }

        /// <summary>The doors of one kind stop looking locked (their look gets "_on": a green light) - they're ordinary doors now.</summary>
        static void Relight(string look)
        {
            for (int y = 0; y < TerrainMap.Height; y++)
                for (int x = 0; x < TerrainMap.Width; x++)
                {
                    var t = TerrainMap.GetTile(x, y);
                    if ((t.Type == TileType.Door || (t.Type == TileType.Difficult && (look == "rockfall" || look == "irongate" || look == "sealslab"))) && t.Look == look) { t.Look = look + "_on"; TerrainMap.DirtyTiles.Add((x, y)); }
                }
        }

        /// <summary>RT fx 4: as a pump starts, up to three whirlpools turn in the deep water round it (nearest first, 3+ tiles apart).</summary>
        static void Whirlpools(string key)
        {
            var pump = TerrainMap.InteractiveObjects.FirstOrDefault(o => o.GrantsKey == key);
            if (pump == null) return;
            var spots = new List<(int x, int y)>();
            for (int r = 1; r <= 7 && spots.Count < 3; r++)
                for (int dy = -r; dy <= r && spots.Count < 3; dy++)
                    for (int dx = -r; dx <= r && spots.Count < 3; dx++)
                    {
                        int x = pump.X + dx, y = pump.Y + dy;
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r || x < 0 || y < 0 || x >= TerrainMap.Width || y >= TerrainMap.Height) continue;
                        var t = TerrainMap.GetTile(x, y);
                        if (t.Type != TileType.Difficult || t.Look != "deepwater") continue;
                        if (spots.Any(s => Mathf.Max(Mathf.Abs(s.x - x), Mathf.Abs(s.y - y)) < 3)) continue;
                        spots.Add((x, y));
                    }
            foreach (var s in spots) CombatFx.PxAt("whirlpool", s.x, s.y);
        }

        /// <summary>The deep water drops to knee height: plain flooded floor (slow, like any 'w').</summary>
        static void Drain()
        {
            for (int y = 0; y < TerrainMap.Height; y++)
                for (int x = 0; x < TerrainMap.Width; x++)
                {
                    var t = TerrainMap.GetTile(x, y);
                    if (t.Type == TileType.Difficult && t.Look == "deepwater") { t.Look = null; TerrainMap.DirtyTiles.Add((x, y)); }
                }
        }

        /// <summary>Called when a map is built: locks whose key the player already holds start open (a save, a second visit).</summary>
        public static void Refresh()
        {
            if (Has(Key("pry"))) Relight("rockfall");
            if (Has(Key("gate"))) Relight("irongate");
            if (Has(Key("code"))) Relight("sealslab");
            if (Has(Key("staff"))) Relight("door_badge");
            if (Has(Key("power"))) Relight("door_power");
            if (Has(Key(WaterKind(Zone)))) Drain();
        }

        /// <summary>Every key a dungeon's locks ask for (tests: walk the whole map as someone who found them all).</summary>
        public static IEnumerable<string> KeysOf(string zone) =>
            !Prefix.ContainsKey(zone ?? "") ? Enumerable.Empty<string>()
            : (Things.TryGetValue(zone, out var d) ? d.Values.Select(v => KeyIn(zone, v.Kind)) : Enumerable.Empty<string>())
              .Concat(new[] { KeyIn(zone, "power"), KeyIn(zone, WaterKind(zone)) }).Distinct();   // keys found on another level count too

        /// <summary>The plan letters of a dungeon's key objects, as stash names for FloorPlan (null if it has none).</summary>
        public static Dictionary<char, string> StashesOf(string zone) =>
            Things.TryGetValue(zone, out var d) ? d.ToDictionary(kv => kv.Key, kv => kv.Value.Name) : null;

        /// <summary>The map's interactive object for a named key object of this dungeon; false for any other name.</summary>
        public static bool Spawn(string name, int x, int y)
        {
            if (Zone == null || !Things.TryGetValue(Zone, out var d)) return false;
            var th = d.Values.FirstOrDefault(v => v.Name == name);
            if (th == null) return false;
            TerrainMap.InteractiveObjects.Add(new InteractiveObjectInstance
            {
                X = x, Y = y, Name = th.Name, Stat = new[] { "Strength" }, Dc = 0, Reward = th.Ru > 0 ? new InteractiveReward { Ru = th.Ru } : null,
                Description = th.Desc, GrantsKey = Key(th.Kind), Requires = th.Needs == null ? null : Key(th.Needs),
            });
            var t = TerrainMap.Tiles[y, x];
            if (t.Look == null) { t.Look = th.Look; if (th.Look != "labswitch") t.Type = TileType.Difficult; }   // boxes are climbed over; a switch is a panel on the floor
            return true;
        }
    }
}
