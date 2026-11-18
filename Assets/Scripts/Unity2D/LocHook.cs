// Unity-only (left out of the offline harness, like Net/ and Editor/): the language for every TextMeshPro label (2026-11-18, Yan: "add a pt-br translation"). Loc.cs has the rules and the table; this
//   - loads Resources/Lang/<language>.txt, remembers the choice (PlayerPrefs "lang"), and starts in it;
//   - every frame, in LateUpdate (after the game wrote its English, before anything is drawn), passes every label whose text changed through Loc.Text - so no screen needs its own code;
//   - remembers each label's English, so a language change (Options) redoes everything on screen at once.
// Labels are found by a scan every third frame, and the common ways to make a label (HudView.Txt, UIBuildUtil.CreateText, the shop and HUD helpers) translate it at birth, so a label should never be seen in English; the ones that change every frame (the HUD) are cheap: only a changed string is looked up.
// English mode costs nothing (the scan and the loop do not run).
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GameCore
{
    public sealed class LocHook : MonoBehaviour
    {
        static LocHook _me;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            string lang = Loc.English; try { lang = PlayerPrefs.GetString("lang", Loc.English); } catch { }
            if (System.Array.IndexOf(Loc.Languages, lang) < 0) lang = Loc.English;
            LoadTable(lang); Loc.Lang = lang;
            Loc.Chooser = Choose;
            var go = new GameObject("LocHook"); DontDestroyOnLoad(go); _me = go.AddComponent<LocHook>();
        }

        static void LoadTable(string lang)
        {
            if (lang == Loc.English) { Loc.LoadTable(null); return; }
            var ta = Resources.Load<TextAsset>("Lang/" + lang);
            Loc.LoadTable(ta != null ? ta.text : null);
        }

        /// <summary>The Options button: switch language now (everything on screen is redone), and remember it.</summary>
        public static void Choose(string lang)
        {
            if (lang == Loc.Lang) return;
            LoadTable(lang); try { PlayerPrefs.SetString("lang", lang); PlayerPrefs.Save(); } catch { }
            Loc.Set(lang);
        }

        class Seen { public string src, shown; }
        readonly Dictionary<TMP_Text, Seen> _seen = new Dictionary<TMP_Text, Seen>();
        float _scanAt; readonly List<TMP_Text> _dead = new List<TMP_Text>();

        // Editor: every piece of text with no table line is collected (about every 10 s written to <project>/Logs/untranslated_pt-BR.txt, one per line, newest session first): send that file to get the next lines translated.
        static readonly HashSet<string> _missed = new HashSet<string>(); static float _writeAt; static bool _dirty;
        static void OnMissed(string s) { if (_missed.Add(s)) _dirty = true; }
        void WriteMissed()
        {
#if UNITY_EDITOR
            if (!_dirty || Time.unscaledTime < _writeAt) return; _writeAt = Time.unscaledTime + 10f; _dirty = false;
            try { var dir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), "Logs"); System.IO.Directory.CreateDirectory(dir); var list = new List<string>(_missed); list.Sort(System.StringComparer.Ordinal); System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "untranslated_pt-BR.txt"), string.Join("\n", list)); } catch { }
#endif
        }

        void OnEnable() { Loc.Changed += Redo; Loc.Missed = OnMissed; }
        void OnDisable() { Loc.Changed -= Redo; Loc.Missed = null; }

        void Redo()
        {
            Scan();
            foreach (var kv in _seen) { var t = kv.Key; if (t == null) continue; kv.Value.shown = Loc.Pt ? Loc.Text(kv.Value.src) : kv.Value.src; if (t.text != kv.Value.shown) t.text = kv.Value.shown; }
        }

        void Scan()
        {
            foreach (var t in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t != null && !_seen.ContainsKey(t)) { string cur = t.text; _seen[t] = new Seen { src = Loc.Original(cur), shown = cur }; }   // (Original: a label made already in Portuguese remembers its English)
        }

        void LateUpdate()
        {
            WriteMissed();
            if (!Loc.Pt && _seen.Count == 0) return;                          // English and nothing remembered: nothing to do
            if (Time.frameCount % 3 == 0) Scan();                              // a label made without Loc.Text shows English for at most three frames
            _dead.Clear();
            foreach (var kv in _seen)
            {
                var t = kv.Key; if (t == null) { _dead.Add(t); continue; }
                var e = kv.Value; string cur = t.text;
                if (cur == e.shown) continue;                                    // still what we put there
                e.src = cur;                                                      // the game wrote something new (English, or already Portuguese from a hot path)
                e.shown = Loc.Pt ? Loc.Text(cur) : cur;
                if (e.shown != cur) t.text = e.shown;
            }
            foreach (var d in _dead) _seen.Remove(d);
            if (!Loc.Pt && _seen.Count > 0) _seen.Clear();                        // back in English: everything was restored by Redo
        }
    }
}
