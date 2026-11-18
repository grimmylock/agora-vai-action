// New: small shared helpers for building runtime uGUI elements (panels/text/buttons), anchored by
// normalized rect (0-1 across the parent), used by DeathScreen and ShopUI so neither hand-rolls
// the same RectTransform/Image/TextMeshProUGUI/Button setup twice. Not a general UI framework -
// just the handful of primitives those two panels both need.
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore
{
    public static class UIBuildUtil
    {
        public static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        public static TextMeshProUGUI CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            string content, int fontSize, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = align;
            var f = HudView.BodyFont; if (f != null) text.font = f;        // 2026-11-18: the game's pixel font on every panel, not TextMeshPro's default
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            string label, Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.5f, 0.4f, 0.23f, 1f);                                      // 2026-11-18: a brass edge round a dark inset, as the case and the merchant screens
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => { Sfx.Play("ui_click"); onClick(); });
            var inset = CreatePanel(go.transform, "Inset", Vector2.zero, Vector2.one, new Color(0.1f, 0.085f, 0.07f, 1f));
            var ir = inset.GetComponent<RectTransform>(); ir.offsetMin = new Vector2(2, 2); ir.offsetMax = new Vector2(-2, -2); inset.GetComponent<Image>().raycastTarget = false;
            var lab = CreateText(go.transform, "Label", Vector2.zero, Vector2.one, label, 16, new Color(0.92f, 0.9f, 0.84f), TextAlignmentOptions.Center);
            var tf = HudView.TitleFont; if (tf != null) lab.font = tf; lab.characterSpacing = 2;
            return btn;
        }

        // ---- the case look for the main screens (2026-11-18, Yan: "something similar to Resident Evil 4") ----
        static readonly Color Brass = new Color(0.55f, 0.43f, 0.24f), BrassDim = new Color(0.32f, 0.26f, 0.16f), Leather = new Color(0.075f, 0.062f, 0.052f, 0.98f);

        /// <summary>Dresses a full-size panel (quests, the Zone, the character sheet) as a page of the case: dark leather, a brass rim, a dimmed room behind it, and the tab row above it (CASE / CHARACTER / QUESTS / MAP) so the
        /// four screens read as one menu. `active` is the tab this panel is; `close` closes it (the other tabs call it, then open theirs).</summary>
        public static void Dress(GameObject panel, string active, System.Action close)
        {
            var img = panel.GetComponent<Image>(); if (img != null) img.color = Leather;
            foreach (var (amin, amax) in new[] { (new Vector2(0, 0), new Vector2(1, 0)), (new Vector2(0, 1), new Vector2(1, 1)), (new Vector2(0, 0), new Vector2(0, 1)), (new Vector2(1, 0), new Vector2(1, 1)) })
            {
                var e = CreatePanel(panel.transform, "Rim", amin, amax, Brass); var r = e.GetComponent<RectTransform>();
                bool horizontal = amin.y == amax.y; r.offsetMin = new Vector2(horizontal ? 0 : (amin.x == 0 ? 0 : -3), horizontal ? (amin.y == 0 ? 0 : -3) : 0); r.offsetMax = new Vector2(horizontal ? 0 : (amin.x == 0 ? 3 : 0), horizontal ? (amin.y == 0 ? 3 : 0) : 0);
                e.GetComponent<Image>().raycastTarget = false;
            }
            var tabs = new (string label, System.Action go)[] { ("CASE", () => { close(); HudView.OpenCase(); }), ("CHARACTER", () => { close(); CharacterUI.ShowSheet(); }), ("QUESTS", () => { close(); QuestUI.Toggle(); }), ("MAP", () => { close(); WorldUI.Toggle(); }) };
            float x = 0f;
            foreach (var (label, go) in tabs)
            {
                bool on = label == active; float w = 0.045f + label.Length * 0.0105f;
                var b = CreatePanel(panel.transform, "Tab", new Vector2(x, 1.004f), new Vector2(x + w, 1.056f), on ? Brass : BrassDim);
                var inner = CreatePanel(b.transform, "In", Vector2.zero, Vector2.one, on ? new Color(0.16f, 0.125f, 0.085f, 1f) : new Color(0.06f, 0.05f, 0.04f, 1f));
                var ir = inner.GetComponent<RectTransform>(); ir.offsetMin = new Vector2(2, 2); ir.offsetMax = new Vector2(-2, -2); inner.GetComponent<Image>().raycastTarget = false;
                var t = CreateText(b.transform, "T", Vector2.zero, Vector2.one, Loc.Text(label), 14, on ? new Color(0.92f, 0.9f, 0.84f) : new Color(0.62f, 0.6f, 0.54f), TextAlignmentOptions.Center);
                var tf = HudView.TitleFont; if (tf != null) t.font = tf; t.characterSpacing = 3;
                if (!on) { var btn = b.AddComponent<Button>(); var action = go; btn.onClick.AddListener(() => { Sfx.Play("ui_click"); action(); }); }
                else b.GetComponent<Image>().raycastTarget = false;
                x += w + 0.006f;
            }
            if (panel.GetComponent<PanelDim>() == null) panel.AddComponent<PanelDim>();
        }

        /// <summary>The dimmed room behind a dressed panel: made when it shows, hidden when it hides, gone when it is destroyed.</summary>
        public class PanelDim : MonoBehaviour
        {
            GameObject _dim;
            void OnEnable()
            {
                if (_dim == null)
                {
                    var canvas = transform.parent; if (canvas == null) return;
                    _dim = CreatePanel(canvas, "PanelDim", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.62f));
                    _dim.transform.SetSiblingIndex(transform.GetSiblingIndex());          // just behind the panel
                }
                _dim.SetActive(true);
            }
            void OnDisable() { if (_dim != null) _dim.SetActive(false); }
            void OnDestroy() { if (_dim != null) Destroy(_dim); }
        }

        /// <summary>A row inside a VerticalLayoutGroup needs a LayoutElement to get a sane height -
        /// without one the layout group gives it zero/undefined height.</summary>
        public static GameObject CreateRow(Transform parent, string name, float height, Color background)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = background;
            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = height;
            return go;
        }
    }
}
