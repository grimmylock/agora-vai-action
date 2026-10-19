// Batch 105 (Yan's plan: "prop redraws with a material each (impact effect, sound, toughness; bullets go through wood/tin,
// not concrete)"). Every prop look has a material; it decides how much shooting the cover takes (TileRules.MaxIntegrity),
// whether a bullet the cover stopped goes on through it (Weapon.Fire), and what a hit on it looks and sounds like
// (CombatFxView). Gameplay still reads TileType for cover AC; the material only changes toughness and pass-through.
using System.Collections.Generic;

namespace GameCore
{
    public enum Material { None, Wood, Tin, Steel, Concrete, Stone, Earth, Plant, Cloth }

    public static class Materials
    {
        static readonly Dictionary<string, Material> ByLook = new Dictionary<string, Material>();
        static void Add(Material m, params string[] looks) { foreach (var l in looks) ByLook[l] = m; }
        static Materials()
        {
            Add(Material.Wood, "crate", "desk", "desk_n", "desk_s", "desk_e", "desk_w", "logs", "logs_v", "logs_c", "fence", "fence_torn",
                "fence_v", "watchtower", "pole", "tree", "pine", "deadtree", "redtree", "plank");
            Add(Material.Tin, "lockers", "rustbarrel", "scrap", "pipes", "dumpster");
            Add(Material.Steel, "machine", "engine", "wreck", "sunktruck", "bus", "tank", "flatcar", "container", "pipe", "pipepost",
                "hatch", "barrel", "metal", "hull");
            Add(Material.Concrete, "block", "block_v", "block_c", "jersey", "jersey_v", "jersey_c", "debris", "pillar", "culvert",
                "fountain", "window", "concrete", "wall", "plaster");
            Add(Material.Stone, "rock", "stones", "rubble", "campfire");
            Add(Material.Earth, "sandbags", "sandbags_v", "sandbags_c", "tires");
            Add(Material.Plant, "bush", "redbush", "brush", "reeds", "roots");
            Add(Material.Cloth, "armchair", "trash");
        }

        public static Material Of(TerrainTile t)
        {
            if (t == null) return Material.None;
            if (t.Look != null && ByLook.TryGetValue(t.Look, out var m)) return m;
            switch (t.Type)
            {
                case TileType.HardCover: return Material.Concrete;
                case TileType.ScatterCover: return Material.Wood;
                case TileType.Barrel: return Material.Steel;
                case TileType.Door: return Material.Wood;
                default: return Material.None;
            }
        }

        public static Material At(int x, int y) =>
            x < 0 || y < 0 || x >= TerrainMap.Width || y >= TerrainMap.Height ? Material.None : Of(TerrainMap.GetTile(x, y));

        /// <summary>Hits a piece of cover takes before it breaks. Light cover breaks quickly, earth and stone soak a lot.</summary>
        public static int Toughness(Material m, int byType)
        {
            switch (m)
            {
                case Material.Plant: return 1;
                case Material.Cloth: case Material.Tin: return 2;
                case Material.Wood: return 3;
                case Material.Earth: case Material.Concrete: return byType >= 6 ? 6 : 4;
                case Material.Steel: return byType >= 6 ? 8 : 4;
                case Material.Stone: return 10;
                default: return byType;
            }
        }

        /// <summary>A bullet the cover stopped goes on through it (at reduced damage).</summary>
        public static bool Pierced(Material m) => m == Material.Wood || m == Material.Tin || m == Material.Plant || m == Material.Cloth;

        /// <summary>Damage left after punching through.</summary>
        public const float ThroughDamage = 0.5f;

        public static string Sound(Material m)
        {
            switch (m)
            {
                case Material.Wood: return "hit_wood";
                case Material.Tin: return "hit_tin";
                case Material.Concrete: case Material.Stone: return "hit_concrete";
                case Material.Earth: case Material.Plant: case Material.Cloth: return "hit_dirt";
                default: return "hit_hard";
            }
        }

        /// <summary>The colour of what flies off: splinters, dust, sparks, earth, leaves.</summary>
        public static (float r, float g, float b) Chip(Material m)
        {
            switch (m)
            {
                case Material.Wood: return (0.62f, 0.45f, 0.28f);
                case Material.Tin: return (0.55f, 0.36f, 0.24f);
                case Material.Concrete: return (0.66f, 0.65f, 0.6f);
                case Material.Stone: return (0.55f, 0.55f, 0.5f);
                case Material.Earth: return (0.6f, 0.53f, 0.36f);
                case Material.Plant: return (0.38f, 0.48f, 0.24f);
                case Material.Cloth: return (0.45f, 0.33f, 0.3f);
                default: return (1f, 0.8f, 0.3f);
            }
        }

        /// <summary>Metal throws sparks; everything else throws chips and dust.</summary>
        public static bool Sparks(Material m) => m == Material.Steel || m == Material.Tin || m == Material.None;
    }
}
