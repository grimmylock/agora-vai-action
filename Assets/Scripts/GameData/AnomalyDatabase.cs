// Ported from database.js: ANOMALY_DB
using System.Collections.Generic;

namespace GameData
{
    public class AnomalyEffects
    {
        public bool Burn;
        public int? ApDrain;
        public bool ToxicBleed;
        public bool Prone;
        public bool Radiation;
    }

    public class AnomalyDef
    {
        public string Name;
        public int? Dc;                // null for RadiationPocket, matching the JS (dc: null)
        public int Damage;
        public string AnomalyType;
        public string DamageKind;      // "Anom" or "Phys"
        public AnomalyEffects Effects;
        public string Description;
    }

    public static class AnomalyDatabase
    {
        public static readonly Dictionary<string, AnomalyDef> All = new Dictionary<string, AnomalyDef>
        {
            ["Pyre"] = new AnomalyDef
            {
                Name = "Pyre", Dc = 12, Damage = 50, AnomalyType = "Pyre", DamageKind = "Anom",
                Effects = new AnomalyEffects { Burn = true },
                Description = "Pyre anomaly that deals raw anomalous damage on failure.",
            },
            ["Livewire"] = new AnomalyDef
            {
                Name = "Livewire", Dc = 14, Damage = 60, AnomalyType = "Livewire", DamageKind = "Anom",
                Effects = new AnomalyEffects { ApDrain = 2 },
                Description = "Livewire anomaly that drains AP on failure.",
            },
            ["AcidBloom"] = new AnomalyDef
            {
                Name = "Acid Bloom", Dc = 11, Damage = 30, AnomalyType = "Acid Bloom", DamageKind = "Anom",
                Effects = new AnomalyEffects { ToxicBleed = true },
                Description = "Acid Bloom anomaly that inflicts toxic bleeding on failure.",
            },
            ["Spindle"] = new AnomalyDef
            {
                Name = "Spindle", Dc = 15, Damage = 80, AnomalyType = "Spindle", DamageKind = "Phys",
                Effects = new AnomalyEffects { Prone = true },
                Description = "Spindle anomaly that throws the player and causes prone on failure.",
            },
            ["RadiationPocket"] = new AnomalyDef
            {
                Name = "Radiation Pocket", Dc = null, Damage = 10, AnomalyType = "Radiation", DamageKind = "Anom",
                Effects = new AnomalyEffects { Radiation = true },
                Description = "Radiation pocket that deals damage unless closed breathing is active.",
            },
        };
    }
}
