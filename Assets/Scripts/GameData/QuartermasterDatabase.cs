// New: named static traders, ported from the Zone Map file's "Faction Outposts" table. Zone ids
// below use this project's own MacroMapDatabase ids, matched to the reference's zone names 1:1
// (Ghost Town == ghost_town, Crimson Woods == crimson_woods, Anomalous Epicenter == outskirts, The
// Siege Ward == hospital - this project renamed a few zones, the rest match verbatim).
//
// New (Batch 9): ShopSystem now applies the ±10-30% remoteness/reputation price adjustment
// (ReputationPricing.cs), keyed off each entry's MaxTier for remoteness and its Faction for the
// Friendly discount. ponytail: the source's exact per-outpost calibration (e.g. "Ghost Town's
// squatter always sitting at the high end of that band") wasn't available to re-derive this
// session - see ReputationPricing.cs's own header note for the approximation used instead. Access
// (who can trade at all) was already real.
using System.Collections.Generic;
using System.Linq;

namespace GameData
{
    public class QuartermasterDef
    {
        public string ZoneId;
        public string Name;
        public string Faction;       // null = independent, trades with anyone regardless of reputation
        public string AccessLevel;   // "Open" (no gate), "Neutral" (Neutral+ required), "Friendly" (Friendly required)
        public int MaxTier;          // stock cap - separate from the zone's own Tier since a few outposts stock below it
        // New (Batch 7): the named landmark (LandmarkDatabase) this Quartermaster actually trades
        // out of, matching the Zone Map's own Faction Outposts table 1:1 - flavor only, shown in ShopUI.
        public string Landmark;
    }

    public static class QuartermasterDatabase
    {
        public static readonly List<QuartermasterDef> All = new List<QuartermasterDef>
        {
            new QuartermasterDef { ZoneId = "bonfire_village", Name = "Old Man Yefim", Faction = "Wanderers", AccessLevel = "Open", MaxTier = 1, Landmark = "The Bonfire Camp" },
            new QuartermasterDef { ZoneId = "smugglers_run", Name = "The Runner", Faction = "Wanderers", AccessLevel = "Neutral", MaxTier = 1, Landmark = "The Cutting" },
            new QuartermasterDef { ZoneId = "scrapyard", Name = "The Scrap King", Faction = "Raiders", AccessLevel = "Neutral", MaxTier = 2, Landmark = "Scrap King's Nest" },
            new QuartermasterDef { ZoneId = "rail_yards", Name = "Contractor Quartermaster", Faction = "Contractors", AccessLevel = "Neutral", MaxTier = 3, Landmark = "Roundhouse" },
            new QuartermasterDef { ZoneId = "dried_lake", Name = "Surveyor Field Cache", Faction = "Surveyors", AccessLevel = "Neutral", MaxTier = 3, Landmark = "Sub-Station House" },
            new QuartermasterDef { ZoneId = "vanguard_barracks", Name = "Garrison Quartermaster", Faction = "Sentinels", AccessLevel = "Friendly", MaxTier = 4, Landmark = "Command Post" },
            new QuartermasterDef { ZoneId = "ghost_town", Name = "Unaffiliated Squatter", Faction = null, AccessLevel = "Open", MaxTier = 3, Landmark = "Main Street" },
            new QuartermasterDef { ZoneId = "quarry_basin", Name = "Contractor Mining-Ops Quartermaster", Faction = "Contractors", AccessLevel = "Friendly", MaxTier = 4, Landmark = "The Terraces" },
            new QuartermasterDef { ZoneId = "ship_graveyard", Name = "Wanderer Squatter Stash", Faction = "Wanderers", AccessLevel = "Neutral", MaxTier = 4, Landmark = "Captain's Row" },
            new QuartermasterDef { ZoneId = "antenna_grid", Name = "Obelisk Outpost", Faction = "Obelisk", AccessLevel = "Friendly", MaxTier = 4, Landmark = "Relay Station" },
            new QuartermasterDef { ZoneId = "abandoned_city", Name = "Garrison Field Quartermaster", Faction = "Garrison", AccessLevel = "Friendly", MaxTier = 5, Landmark = "Garrison Barricade" },
            // Deliberately no entry for scavengers_hub: it already works via the zone.IsSafeZone
            // fallback in ShopUI, which correctly caps its stock at its own zone.Tier (3) - giving
            // it an explicit MaxTier here risked overriding that and reopening the "Tier 5 gear
            // buyable at Tier 3" hole the tier/trader system was built to close in the first place.
        };

        public static QuartermasterDef Find(string zoneId) => All.FirstOrDefault(q => q.ZoneId == zoneId);

        /// <summary>True if a player at the given reputation level ("Hostile"/"Neutral"/"Friendly",
        /// see Player.GetRepLevel) can trade with this Quartermaster. "Open" and independent (no
        /// Faction) traders always allow it; Hostile reputation always refuses regardless of the
        /// outpost's own AccessLevel, matching the source's "Hostile factions attack on sight - no
        /// trade window exists" rule. Takes the rep string rather than a Player so this stays pure
        /// data (GameData doesn't otherwise reference GameCore types).</summary>
        public static bool CanTrade(QuartermasterDef qm, string playerRepLevel)
        {
            if (qm == null) return false;
            if (string.IsNullOrEmpty(qm.Faction) || qm.AccessLevel == "Open") return true;
            if (playerRepLevel == "Hostile") return false;
            if (qm.AccessLevel == "Friendly") return playerRepLevel == "Friendly";
            return true; // "Neutral" access level: Neutral or Friendly both pass
        }
    }
}
