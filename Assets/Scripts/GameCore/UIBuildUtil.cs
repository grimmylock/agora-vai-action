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
            img.color = new Color(0.25f, 0.25f, 0.25f, 1f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => { Sfx.Play("ui_click"); onClick(); });

            CreateText(go.transform, "Label", Vector2.zero, Vector2.one, label, 16, Color.white, TextAlignmentOptions.Center);
            return btn;
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
