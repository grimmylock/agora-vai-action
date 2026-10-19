// Ported from database.js: DETECTOR_DB
using System.Collections.Generic;

namespace GameData
{
    public class DetectorDef
    {
        public string Name;
        public int RangeCm;
        public int Ru;
        public int Modifier;
        public bool PassiveReveal;
        public string Description;
    }

    public static class DetectorDatabase
    {
        public static readonly Dictionary<string, DetectorDef> All = new Dictionary<string, DetectorDef>
        {
            ["Clicker"] = new DetectorDef { Name = "Clicker", RangeCm = 3, Ru = 1000, Modifier = 1, Description = "Short-range anomaly detector" },
            ["Sniffer"] = new DetectorDef { Name = "Sniffer", RangeCm = 6, Ru = 3000, Modifier = 2, Description = "+2 anomaly checks" },
            ["Dowser"] = new DetectorDef { Name = "Dowser", RangeCm = 9, Ru = 10000, Modifier = 3, Description = "Long-range anomaly detector" },
            ["Seer"] = new DetectorDef { Name = "Seer", RangeCm = 15, Ru = 15000, Modifier = 4, PassiveReveal = true, Description = "Passively reveals anomalies within 15cm" },
        };
    }
}
