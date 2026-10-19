// New: a minimal on-screen combat log, built at runtime (no scene editing required) and wired up
// as GameLog.Target so every GameLog.Write(...) call already threaded through the ported engine
// (move results, attack rolls, damage, turn changes, etc.) shows up on screen instead of only in
// the Console. Added automatically by GameBootstrap.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore
{
    public class RuntimeGameLog : MonoBehaviour, IGameLogTarget
    {
        private const int MaxLines = 16;

        private readonly List<string> _lines = new List<string>();
        private TextMeshProUGUI _text;

        private void Awake()
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("RuntimeGameLog: no Canvas found in the scene - combat log will only print to the Console.");
            }
            else
            {
                var backgroundGo = new GameObject("CombatLogBackground", typeof(RectTransform));
                backgroundGo.transform.SetParent(canvas.transform, false);
                var backgroundRect = backgroundGo.GetComponent<RectTransform>();
                backgroundRect.anchorMin = new Vector2(0f, 0f);
                backgroundRect.anchorMax = new Vector2(0.4f, 0.22f);
                backgroundRect.offsetMin = new Vector2(16, 16);
                backgroundRect.offsetMax = new Vector2(-8, -8);
                var backgroundImage = backgroundGo.AddComponent<Image>();
                backgroundImage.color = new Color(0f, 0f, 0f, 0.6f);
                backgroundImage.raycastTarget = false;
                // Without a mask, TextMeshProUGUI doesn't clip to its RectTransform - once the log
                // filled up with 16 lines it overflowed way past this panel and sprawled across the
                // whole screen, on top of every other UI element. Clip it to the panel; oldest
                // (top-most, since text is bottom-anchored) lines just scroll out of view instead.
                backgroundGo.AddComponent<RectMask2D>();

                var textGo = new GameObject("CombatLogText", typeof(RectTransform));
                textGo.transform.SetParent(backgroundGo.transform, false);
                var textRect = textGo.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(8, 8);
                textRect.offsetMax = new Vector2(-8, -8);

                _text = textGo.AddComponent<TextMeshProUGUI>();
                _text.fontSize = 12;
                _text.color = Color.white;
                _text.alignment = TextAlignmentOptions.BottomLeft;
                _text.raycastTarget = false;
            }

            GameLog.Target = this;
        }

        public void AppendLine(string message)
        {
            // Still mirror to the Console so it also shows up in the Editor's Console window / a
            // built player's log file.
            Debug.Log(message);

            _lines.Add(message);
            if (_lines.Count > MaxLines) _lines.RemoveAt(0);
            if (_text != null) _text.text = string.Join("\n", _lines);
        }
    }
}
