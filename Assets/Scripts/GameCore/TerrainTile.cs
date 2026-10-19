// Ported from script.js: tile `{ type: 'Empty' | 'Solid' | 'ScatterCover' }` objects used
// throughout terrainMap, extended with the rest of the design legend (06_Zone_Map.md):
//   #  Solid        wall / solid obstruction - blocks movement and LoS
//   C  ScatterCover light cover (+1 AC, Integrity 3)
//   =  HardCover    hard cover (+2 AC, Integrity 6)
//   ~  Difficult    difficult terrain / rubble (2 MP, no cover) - destroyed cover becomes this
//   D  Door         closed door - blocks LoS; walking into it opens it (+1 MP), shooting it
//                   down (Integrity 3) leaves it open for good
//      DoorOpen     open or broken door - plain floor
//      Barrel       explosive barrel - light cover you can't stand on; blows up when its
//                   Integrity runs out (see TerrainMap.DamageTile / Explosions)
namespace GameCore
{
    public enum TileType
    {
        Empty,
        Solid,
        ScatterCover,
        HardCover,
        Difficult,
        Door,
        DoorOpen,
        Barrel,
    }

    /// <summary>Cosmetic floor material, drawn under whatever stands on the tile. Never affects
    /// rules - Difficult terrain is a TileType, Ground only says what it looks like.</summary>
    public enum GroundType
    {
        Grass,
        DarkGrass,
        Dirt,
        Mud,
        Asphalt,
        Concrete,
        Floor,
        LabTile,
        Sand,
        Gravel,
        Water,
        CaveFloor,
    }

    public class TerrainTile
    {
        public TileType Type = TileType.Empty;

        // Real per-tile elevation (not a flat bonus type) - used for climb move cost,
        // height-interpolated line of sight, and a high-ground accuracy bonus. 0 = ground level;
        // negative values are valid (pits, trenches, sunken lakebeds).
        public int Height = 0;

        /// <summary>Cover Integrity lost so far (file 00). The tile breaks when this reaches
        /// TileRules.MaxIntegrity(Type).</summary>
        public int Damage;

        /// <summary>Optional art key set by the zone recipe ("tree", "fence", "car", "sandbags"...);
        /// null lets TacticalGridView pick a look from the type and zone.</summary>
        public string Look;

        public GroundType Ground = GroundType.Grass;

        public TerrainTile Clone() => new TerrainTile { Type = Type, Height = Height, Damage = Damage, Look = Look, Ground = Ground };
    }

    public static class TileRules
    {
        public static bool BlocksMovement(TileType t) => t == TileType.Solid || t == TileType.Barrel;

        /// <summary>Only walls and closed doors block LoS (file 00 §6) - cover never does.</summary>
        public static bool BlocksSight(TileType t) => t == TileType.Solid || t == TileType.Door;

        public static int CoverAc(TileType t) =>
            t == TileType.HardCover ? 2 : (t == TileType.ScatterCover || t == TileType.Barrel) ? 1 : 0;

        /// <summary>Batch 105: what the tile is made of decides how much it takes (Materials.Toughness).</summary>
        public static int MaxIntegrity(TerrainTile t)
        {
            int byType = MaxIntegrity(t.Type);
            return byType <= 0 || t.Type == TileType.Door || t.Type == TileType.Barrel ? byType : Materials.Toughness(Materials.Of(t), byType);
        }

        public static int MaxIntegrity(TileType t)
        {
            switch (t)
            {
                case TileType.ScatterCover: return 3;
                case TileType.HardCover: return 6;
                case TileType.Door: return 3;
                case TileType.Barrel: return 1;
                default: return 0; // indestructible (walls) or nothing to destroy
            }
        }
    }
}
