// The testing ground's damage numbers (TestingGround.Numbers): each hit rises off the target and fades - cream for the body,
// yellow for the head, red for the kill. Drawn like the barks (HudView.Barks), only on the testing ground.
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GameCore
{
    public partial class HudView
    {
        readonly List<TextMeshProUGUI> _numbers = new List<TextMeshProUGUI>();

        void UpdateNumbers()
        {
            var cam = Camera.main;
            int n = 0;
            if (cam != null && TestingGround.Here && !GameState.Current.ModalOpen)
            {
                if (_canvasC == null && _canvas != null) _canvasC = _canvas.GetComponent<Canvas>();
                float scale = _canvasC != null && _canvasC.scaleFactor > 0f ? _canvasC.scaleFactor : 1f, now = Time.time;
                for (int i = 0; i < TestingGround.Numbers.Count; i++)
                {
                    var d = TestingGround.Numbers[i];
                    var at = TacticalGridView.UnitPos(d.Who);
                    if (!at.HasValue) continue;
                    float age = (now - d.At) / TestingGround.NumberSeconds;
                    if (n == _numbers.Count)
                    {
                        var t0 = Txt(_root.transform, "Dmg", Vector2.zero, new Vector2(160, 24), "", true, 18, Cream, TextAlignmentOptions.Center);
                        t0.raycastTarget = false; t0.rectTransform.pivot = new Vector2(0.5f, 0f); t0.outlineWidth = 0.25f; t0.outlineColor = new Color32(0, 0, 0, 255);
                        _numbers.Add(t0);
                    }
                    var t = _numbers[n++];
                    t.text = d.Text;
                    var c = d.Kind == 2 ? C("e0524a") : d.Kind == 1 ? Yellow : Cream;
                    c.a = Mathf.Clamp01((1f - age) / 0.4f);
                    t.color = c;
                    var sp = cam.WorldToScreenPoint(at.Value + new Vector3(0f, 1.2f + 0.9f * age, 0f));
                    float jitter = (i % 3 - 1) * 14f;                              // quick hits side by side, not on top of each other
                    t.rectTransform.anchorMin = t.rectTransform.anchorMax = Vector2.zero;
                    t.rectTransform.anchoredPosition = new Vector2(Mathf.Round(sp.x / scale + jitter), Mathf.Round(sp.y / scale));
                    t.gameObject.SetActive(true);
                }
            }
            for (int i = n; i < _numbers.Count; i++) _numbers[i].gameObject.SetActive(false);
        }
    }
}
