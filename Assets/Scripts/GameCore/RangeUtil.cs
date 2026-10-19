// Ported from script.js: function parseRangeToTiles(rangeStr)
// Pure string-parsing helper (converts DB range strings like "45cm" or "Adjacent" into a tile
// count), so it's ported now rather than deferred — no grid/combat dependency.
using System.Text.RegularExpressions;

namespace GameCore
{
    public static class RangeUtil
    {
        private static readonly Regex CmPattern = new Regex(@"([0-9]+\.?[0-9]*)\s*cm");

        public static int ParseRangeToTiles(string rangeStr)
        {
            if (string.IsNullOrEmpty(rangeStr)) return 1;

            string s = rangeStr.ToLowerInvariant();
            if (s.Contains("adjacent")) return 1;

            var m = CmPattern.Match(s);
            if (m.Success)
            {
                float cm = float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                return System.Math.Max(1, (int)System.Math.Ceiling(cm / 2.5f));
            }

            return 1;
        }
    }
}
