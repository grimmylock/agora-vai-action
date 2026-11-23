using System.Collections.Generic;
using UnityEngine;

// 2026-11-23 (Yan: "we'll need to add a new system for the clarity in the attacks ... a kinda 'counter' on top of their heads signaling the combo ... the space between each dot would be relative to when the move comes out, so 2 attacks / mini delay /
// 2 attacks / mini delay / 1 attack ... kinda like a rhythm game"): while a boss is in a move, a row of marks floats over his head: one per blow still to come, laid out to the RIGHT of a notch above his head at 0.55 tiles per second, so the gap between
// two marks IS the real gap between the two blows (Boss.BeatsOf). They slide toward the notch and are spent as each blow lands. White = parry it, red = dive it. The old single red flash at the glint stays as the "now" cue.
// View only, host-side: the clients do not see a boss's moves yet (the networking carries no boss state), so no counter there.
namespace GameCore
{
    public class BossBeatsView
    {
        const float PerSecond = 0.55f, LookAhead = 3.4f, HeadHeight = 1.15f, MarkW = 0.2f, MarkH = 0.07f;
        const int MaxMarks = 8;
        readonly Transform _root; readonly System.Func<ICombatant, SpriteRenderer> _markerOf;
        readonly Dictionary<Boss.Fight, List<SpriteRenderer>> _marks = new Dictionary<Boss.Fight, List<SpriteRenderer>>();
        readonly List<Boss.Beat> _beats = new List<Boss.Beat>(); readonly List<(float dx, bool red, float alpha)> _layout = new List<(float, bool, float)>(); static Sprite _px;

        /// <summary>The layout, pure (tested): where each mark sits to the right of the notch (tiles) and how opaque it is.</summary>
        public static void Layout(List<Boss.Beat> beats, List<(float dx, bool red, float alpha)> into)
        {
            into.Clear();
            foreach (var b in beats) { if (b.In > LookAhead || into.Count >= MaxMarks) continue; into.Add((Mathf.Max(0f, b.In) * PerSecond, b.Red, Mathf.Clamp01(1f - Mathf.Max(0f, b.In - (LookAhead - 0.5f)) / 0.5f))); }
        }
        public BossBeatsView(Transform root, System.Func<ICombatant, SpriteRenderer> markerOf) { _root = root; _markerOf = markerOf; }
        static Sprite Px() { if (_px != null) return _px; var t = new Texture2D(1, 1, TextureFormat.RGBA32, false); t.SetPixel(0, 0, Color.white); t.Apply(); t.filterMode = FilterMode.Point; return _px = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect); }

        SpriteRenderer Mark(List<SpriteRenderer> list, int i)
        {
            while (list.Count <= i) { var go = new GameObject("BossBeat"); go.transform.SetParent(_root, false); var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = Px(); sr.sortingOrder = WorldArt.SortTop + 14; list.Add(sr); }
            return list[i];
        }
        public void Tick()
        {
            foreach (var f in Boss.Fights)
            {
                if (!_marks.TryGetValue(f, out var list)) { list = new List<SpriteRenderer>(); _marks[f] = list; }
                var marker = f.Unit != null ? _markerOf(f.Unit) : null; Boss.BeatsOf(f, _beats);
                int shown = 0;
                if (marker != null && marker.gameObject.activeInHierarchy && _beats.Count > 0 && !f.Gone)
                {
                    var head = marker.transform.position + new Vector3(0f, HeadHeight, 0f);
                    var notch = Mark(list, 0); notch.transform.position = head; notch.transform.localScale = new Vector3(0.05f, 0.2f, 1f); notch.color = new Color(1f, 1f, 1f, 0.55f); notch.gameObject.SetActive(true); shown = 1;   // the notch: where a blow is "now"
                    Layout(_beats, _layout);
                    foreach (var (dx, red, alpha) in _layout)
                    {
                        var m = Mark(list, shown++); m.transform.position = head + new Vector3(dx, 0f, 0f); m.transform.localScale = new Vector3(MarkW, MarkH, 1f);
                        m.color = red ? new Color(1f, 0.22f, 0.16f, alpha) : new Color(1f, 1f, 0.92f, alpha); m.gameObject.SetActive(true);
                    }
                }
                for (int i = shown; i < list.Count; i++) list[i].gameObject.SetActive(false);
            }
        }
    }
}
