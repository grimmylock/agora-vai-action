// Ported from database.js: AMMO_DB
using System.Collections.Generic;

namespace GameData
{
    public class AmmoDef
    {
        public string Name;
        public string Effect;
    }

    public static class AmmoDatabase
    {
        public static readonly Dictionary<string, AmmoDef> All = new Dictionary<string, AmmoDef>
        {
            ["Standard"] = new AmmoDef
            {
                Name = "Standard",
                Effect = "Mitigated normally by target Phys-DR.",
            },
            ["HollowPoint"] = new AmmoDef
            {
                Name = "Hollow Point",
                Effect = "+50% raw damage if target Phys-DR == 0%; -50% raw damage if target Phys-DR > 0%.",
            },
            ["ArmorPiercing"] = new AmmoDef
            {
                Name = "Armor Piercing",
                Effect = "Target's Phys-DR is calculated at 50% value during damage mitigation.",
            },
        };
    }
}
