// Language (2026-11-18, Yan: "add a pt-br translation"). The game's text is English in the code; this turns it into Portuguese (Brazil) on its way to the screen, from a table of lines
// (Assets/Resources/Lang/pt-BR.txt, one `english<TAB>português` per line), so nothing in the game logic changes and a missing line simply stays English:
//   - `Loc.T(s)`: the whole string: an exact line (case-insensitive; an ALL-CAPS source gives an ALL-CAPS translation), else a pattern line - an English line with `{}` for each piece that changes
//     ("Looted everything from {}'s body.") matches the finished text and its Portuguese side puts the pieces back by number ("Você saqueou tudo do corpo de {1}.").
//   - `Loc.Text(s)`: for UI text: the whole string first, else line by line and between rich-text tags (`<color=..>PAUSED</color>` / `A\nB`), each piece through `T`.
//   - Names that are the game's own (weapons, factions, places, mutants) are proper nouns and stay as they are; the pieces a pattern captures are not translated again.
//   - Sinks: `GameLog.Write` (the log) and, in Unity, every TextMeshPro label (Unity2D/LocHook.cs, which also owns the language option and the table loading). The harness loads the table from disk.
// Adding a language = a new table and a name in `Languages`.
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace GameCore
{
    public static class Loc
    {
        public const string English = "en", Portuguese = "pt-BR";
        public static readonly string[] Languages = { English, Portuguese };
        public static string Label(string lang) => lang == Portuguese ? "Português (Brasil)" : "English";
        public static string Lang = English;
        public static bool Pt => Lang == Portuguese;
        public static event Action Changed;
        /// <summary>Set by Unity2D/LocHook (loads the table and remembers the choice); the harness just switches.</summary>
        public static Action<string> Chooser;
        public static void Cycle() { int i = Array.IndexOf(Languages, Lang); string next = Languages[(i + 1) % Languages.Length]; if (Chooser != null) Chooser(next); else Set(next); }
        public static void Set(string lang) { if (lang == Lang) return; Lang = lang; Changed?.Invoke(); }

        static readonly Dictionary<string, string> Exact = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        static readonly List<(Regex re, string pt)> Patterns = new List<(Regex, string)>();
        static readonly Dictionary<string, string> Cache = new Dictionary<string, string>();   // results of pattern searches, hits and misses (bounded)
        /// <summary>Editor aid: told about every distinct piece of text that had no line in the table (LocHook writes them to a file to translate next).</summary>
        public static Action<string> Missed;
        static readonly HashSet<string> _reported = new HashSet<string>();
        public static void Miss(string s) { if (Missed == null || string.IsNullOrWhiteSpace(s) || _reported.Contains(s)) return; if (_reported.Count > 20000) _reported.Clear(); _reported.Add(s); if (Regex.Matches(s, "[A-Za-z]").Count >= 3) Missed(s.Trim()); }
        public static int Count => Exact.Count + Patterns.Count;
        public static int PatternCount => Patterns.Count;

        /// <summary>Loads a table: `english\tportuguese` per line, `#` comments, `\n` for a line break. An English side with `{}` is a pattern (each `{}` captures a piece; the Portuguese side uses {1}, {2}, ...).</summary>
        public static void LoadTable(string tsv)
        {
            Exact.Clear(); Patterns.Clear(); Cache.Clear();
            if (tsv == null) return;
            foreach (var raw in tsv.Split('\n'))
            {
                var line = raw.TrimEnd('\r'); if (line.Length == 0 || line[0] == '#') continue;
                int tab = line.IndexOf('\t'); if (tab <= 0) continue;
                string en = Unescape(line.Substring(0, tab)), pt = Unescape(line.Substring(tab + 1));
                if (pt.Length == 0) continue;
                if (en.Contains("{}"))
                {
                    var sb = new StringBuilder("^"); var parts = en.Split(new[] { "{}" }, StringSplitOptions.None);
                    for (int i = 0; i < parts.Length; i++) { sb.Append(Regex.Escape(parts[i])); if (i < parts.Length - 1) sb.Append("(.+?)"); }
                    sb.Append('$');
                    try { Patterns.Add((new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant), pt)); } catch { }
                }
                else Exact[en.Trim()] = pt;
            }
        }
        static string Unescape(string s) => s.Replace("\\n", "\n").Replace("\\t", "\t");

        static bool IsUpper(string s) { bool any = false; foreach (var c in s) { if (char.IsLower(c)) return false; if (char.IsUpper(c)) any = true; } return any; }

        /// <summary>The whole string through the table (exact, then pattern). Unchanged when there is no line for it or the language is English.</summary>
        public static string T(string s)
        {
            if (!Pt || string.IsNullOrEmpty(s)) return s;
            string key = s.Trim();
            if (Exact.TryGetValue(key, out var r)) return Wrap(s, key, IsUpper(key) && !IsUpper(r) ? r.ToUpperInvariant() : r);
            if (Cache.TryGetValue(s, out var c)) return c;
            string res = s;
            foreach (var (re, pt) in Patterns)
            {
                var m = re.Match(key); if (!m.Success) continue;
                var sb = new StringBuilder(pt);
                for (int g = m.Groups.Count - 1; g >= 1; g--) sb.Replace("{" + g + "}", m.Groups[g].Value);
                res = Wrap(s, key, IsUpper(key) ? sb.ToString().ToUpperInvariant() : sb.ToString()); break;
            }
            if (Cache.Count > 6000) Cache.Clear();
            Cache[s] = res; return res;
        }
        static string Wrap(string original, string key, string translated)
        {
            int lead = original.Length - original.TrimStart().Length, trail = original.Length - original.TrimEnd().Length;
            return (lead > 0 ? original.Substring(0, lead) : "") + translated + (trail > 0 ? original.Substring(original.Length - trail) : "");
        }

        static readonly Regex Splitter = new Regex("(<[^>]*>|\\n| {3,})", RegexOptions.Compiled);
        /// <summary>UI text: the whole string, else piece by piece between rich-text tags and line breaks.</summary>
        public static string Text(string s)
        {
            if (!Pt || string.IsNullOrEmpty(s)) return s;
            string whole = T(s); if (!ReferenceEquals(whole, s) && whole != s) return whole;
            if (s.IndexOf('<') < 0 && s.IndexOf('\n') < 0 && s.IndexOf("   ", StringComparison.Ordinal) < 0) { Miss(s); return s; }
            var parts = Splitter.Split(s); var sb = new StringBuilder(); bool changed = false;
            foreach (var p in parts)
            {
                if (p.Length == 0 || p[0] == '<' || p == "\n" || p.Trim().Length == 0) { sb.Append(p); continue; }
                string t = T(p); if (t != p) changed = true; else Miss(p); sb.Append(t);
            }
            return changed ? sb.ToString() : s;
        }
    }
}
