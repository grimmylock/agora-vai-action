// Batch 129: what people say (Banter) as small typed lines over their heads - cream for talk, yellow when they notice
// something, red when they shout that they've spotted you. Only for people you can see; hidden while a panel is up.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore
{
    public partial class HudView
    {
        readonly List<(RectTransform r, TextMeshProUGUI t)> _barks = new List<(RectTransform, TextMeshProUGUI)>();

        void UpdateBarks()
        {
            var cam = Camera.main;
            int n = 0;
            if (cam != null && !GameState.Current.ModalOpen && !ChoiceUI.IsOpen)
            {
                if (_canvasC == null && _canvas != null) _canvasC = _canvas.GetComponent<Canvas>();
                float scale = _canvasC != null && _canvasC.scaleFactor > 0f ? _canvasC.scaleFactor : 1f, now = Time.time;
                foreach (var b in Banter.Active)
                {
                    if (b.Start > now || b.Until < now || !Vision.Shows(b.Who)) continue;
                    var at = TacticalGridView.UnitPos(b.Who);
                    if (!at.HasValue) continue;
                    if (n == _barks.Count) _barks.Add(MakeBark());
                    var (r, t) = _barks[n++];
                    int shown = Mathf.Min(b.Text.Length, 1 + (int)((now - b.Start) * 45f));   // typed out
                    t.text = b.Text.Substring(0, shown);
                    t.color = b.Kind == 2 ? C("e0524a") : b.Kind == 1 ? Yellow : Cream;
                    float w = 16 + b.Text.Length * 8.2f;
                    r.sizeDelta = new Vector2(w, 24); t.rectTransform.sizeDelta = new Vector2(w - 8, 20);
                    var sp = cam.WorldToScreenPoint(at.Value + new Vector3(0f, 1.3f, 0f));
                    r.anchoredPosition = new Vector2(Mathf.Round(sp.x / scale - w / 2f), Mathf.Round(sp.y / scale));
                    r.gameObject.SetActive(true);
                }
            }
            for (int i = n; i < _barks.Count; i++) _barks[i].r.gameObject.SetActive(false);
        }

        (RectTransform, TextMeshProUGUI) MakeBark()
        {
            var box = Img(_root.transform, "Bark", new Vector2(0, 0), Vector2.zero, new Vector2(120, 24), new Color(0.06f, 0.065f, 0.07f, 0.78f));
            var r = box.rectTransform; r.pivot = new Vector2(0, 0); box.raycastTarget = false;
            var t = Txt(r, "T", new Vector2(4, -3), new Vector2(112, 20), "", false, 14, Cream, TextAlignmentOptions.Center);
            t.raycastTarget = false;
            return (r, t);
        }
    }
}
