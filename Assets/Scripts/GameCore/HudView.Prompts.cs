// Batch 123: the interaction prompts (InteractionPrompts) as small chips over the things themselves: a key box
// (E, F, CLICK, WALK) and what it does, in the HUD's colours. Hidden while a panel or dialog is open.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameCore
{
    public partial class HudView
    {
        readonly List<(RectTransform r, Image key, TextMeshProUGUI k, TextMeshProUGUI t)> _prompts = new List<(RectTransform, Image, TextMeshProUGUI, TextMeshProUGUI)>();
        Canvas _canvasC;

        void UpdatePrompts(Player p)
        {
            var cam = Camera.main;
            List<InteractionPrompts.Prompt> list = null;
            if (cam != null && !GameState.Current.ModalOpen && !ChoiceUI.IsOpen)
            {
                (int, int)? hover = null;
                var mouse = Mouse.current;
                if (mouse != null && TacticalGridView.WorldToGrid(cam.ScreenToWorldPoint(mouse.position.ReadValue()), out int hx, out int hy)) hover = (hx, hy);
                list = InteractionPrompts.Near(p, hover);
            }
            if (_canvasC == null && _canvas != null) _canvasC = _canvas.GetComponent<Canvas>();
            float scale = _canvasC != null && _canvasC.scaleFactor > 0f ? _canvasC.scaleFactor : 1f;
            int n = list?.Count ?? 0;
            for (int i = 0; i < n; i++)
            {
                if (i == _prompts.Count) _prompts.Add(MakePrompt());
                var (r, key, k, t) = _prompts[i];
                var q = list[i];
                r.gameObject.SetActive(true);
                bool word = q.Key.Length > 1;
                float kw = word ? 14 + q.Key.Length * 9 : 22, w = kw + 14 + q.Label.Length * 8.5f;
                r.sizeDelta = new Vector2(w, 26);
                key.rectTransform.sizeDelta = new Vector2(kw, 20); k.rectTransform.sizeDelta = new Vector2(kw, 20);
                k.text = q.Key; t.text = q.Label;
                t.rectTransform.anchoredPosition = new Vector2(kw + 9, -5);
                var sp = cam.WorldToScreenPoint(TacticalGridView.GridToWorld(q.X, q.Y) + new Vector3(0f, 0.95f, 0f));
                r.anchoredPosition = new Vector2(Mathf.Round(sp.x / scale - w / 2f), Mathf.Round(sp.y / scale));
            }
            for (int i = n; i < _prompts.Count; i++) _prompts[i].r.gameObject.SetActive(false);
        }

        (RectTransform, Image, TextMeshProUGUI, TextMeshProUGUI) MakePrompt()
        {
            var edge = Img(_root.transform, "Prompt", new Vector2(0, 0), Vector2.zero, new Vector2(120, 26), Edge);
            var r = edge.rectTransform; r.pivot = new Vector2(0, 0);
            var fill = Img(r, "In", new Vector2(0, 1), new Vector2(2, -2), new Vector2(116, 22), Fill);
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = new Vector2(2, 2); fill.rectTransform.offsetMax = new Vector2(-2, -2);
            var key = Img(r, "Key", new Vector2(0, 1), new Vector2(3, -3), new Vector2(22, 20), Yellow);
            var k = Txt(r, "K", new Vector2(3, -6), new Vector2(22, 20), "", true, 11, C("15171a"), TextAlignmentOptions.Top);
            k.characterSpacing = 1;
            var t = Txt(r, "T", new Vector2(34, -5), new Vector2(400, 20), "", false, 15, Cream);
            return (r, key, k, t);
        }
    }
}
