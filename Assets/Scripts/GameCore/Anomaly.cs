// Ported from script.js: class Anomaly (an instance placed on the grid, distinct from
// GameData.AnomalyDef which is the database entry it's created from).
using GameData;

namespace GameCore
{
    public class Anomaly
    {
        public string Name;
        public int X;
        public int Y;
        public int? Dc;
        public float Damage;
        public string AnomalyType;
        public AnomalyEffects Effects;
        public bool Revealed;
        public bool Searched;      // artifact search done here (doc 03) - one try per anomaly
        public int SearchBonus;    // unused since Batch 49 (kept so old code paths compile); fields now hold real artifacts
        public InventoryItem Artifact;   // Batch 49: what's actually lying in this field (null = nothing); search to take it

        public Anomaly(string name, int x, int y, int? dc, float damage, string anomalyType, AnomalyEffects effects)
        {
            Name = name;
            X = x;
            Y = y;
            Dc = dc;
            Damage = damage;
            AnomalyType = anomalyType;
            Effects = effects ?? new AnomalyEffects();
            Revealed = false;
        }

        /// <summary>Ported from the `get displayName()` accessor.</summary>
        public string DisplayName => $"{Name} ({AnomalyType})";

        /// <summary>Ported from createAnomalyFromDb(dbEntry, x, y).</summary>
        public static Anomaly FromDb(AnomalyDef dbEntry, int x, int y)
        {
            if (dbEntry == null) return null;
            return new Anomaly(dbEntry.Name, x, y, dbEntry.Dc, dbEntry.Damage, dbEntry.AnomalyType, dbEntry.Effects);
        }
    }
}
