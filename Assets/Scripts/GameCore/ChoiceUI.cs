// New: a small modal "pick one" dialog (story choices, endings, quest turn-ins). Requests that
// arrive while one is open wait in a queue, so two quests finishing on the same kill both get
// asked. A full-screen dimmer under the dialog stops clicks reaching panels below it. Without a
// scene/Canvas (tests) the first option is taken automatically.
// Batch 123 (Yan: "maybe we need to take a look at the UI next, for dialogue and interactions"): the dialog is a
// conversation panel at the bottom of the screen in the HUD's look - who's talking (their portrait, name, faction and rank),
// the words typed out, the answers numbered (keys 1-9 or a click). The world stays visible over a light dim, and when
// someone on the map is talking the camera frames you both and marks them (TacticalGridView reads Speaker).
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameCore
{
    public class ChoiceUI : MonoBehaviour
    {
        private static ChoiceUI _instance;
        private GameObject _panel;
        private bool _prevModal;   // whatever was open before the first dialog (the quest journal, say)
        private readonly Queue<(string title, string body, List<string> options, Action<int> onPick, ICombatant speaker)> _queue
            = new Queue<(string, string, List<string>, Action<int>, ICombatant)>();

        public static bool IsOpen => _instance != null && _instance._panel != null;
        public static bool Available => _instance != null && FindAnyObjectByType<Canvas>() != null;

        /// <summary>Who is talking in the open dialog (null: nobody on the map - a story card, a quest turn-in).</summary>
        public static ICombatant Speaker => IsOpen ? _instance._speaker : null;

        // faction armbands (ArtSource/make_faction_armors.py), for the speaker's tag
        static readonly Dictionary<string, string> Band = new Dictionary<string, string>
        {
            ["Raiders"] = "b02a2a", ["Outcasts"] = "8a6a48", ["Wanderers"] = "a0b04a", ["Prospectors"] = "d4a62a", ["Garrison"] = "9a8a5a",
            ["Sentinels"] = "3a6ab0", ["Liberators"] = "6fb03a", ["Contractors"] = "c8d0d8", ["Obelisk"] = "e6e6e6", ["Cartel"] = "e0c030",
            ["Surveyors"] = "b8c43a", ["IZC"] = "6fb0e0",
        };
        const float W = 1080f, Row = 34f, Pad = 18f, Face = 132f;
        const float CharsPerSecond = 90f;

        ICombatant _speaker;
        string _full; float _shown; TextMeshProUGUI _bodyText;
        readonly List<(RectTransform rect, Image edge)> _rows = new List<(RectTransform, Image)>();
        Action<int> _pick;

        private void Awake() => _instance = this;

        public static void Show(string title, string body, List<string> options, Action<int> onPick, ICombatant speaker = null)
        {
            // Batch 128 (Yan: "it needs to take into account where both parties are talking from... otherwise we could get weird
            // interactions where they are not facing each other"): the speaker and its squadmates nearby turn to you; you turn to
            // the speaker (TacticalGridView.AimPoint, while Speaker is set). The world is paused while the panel is up.
            var me = Player.Current;
            if (speaker is HumanEnemy sh && me != null)
                foreach (var o in EncounterState.Enemies)
                    if (o == sh || o is HumanEnemy oh && oh.IsAlive() && oh.FactionName == sh.FactionName && CombatantUtil.Distance(oh, sh) <= 8)
                        o.FaceToward(me.X, me.Y);
            var canvas = _instance != null ? FindAnyObjectByType<Canvas>() : null;
            if (canvas == null) { onPick(0); return; }
            if (_instance._panel != null) { _instance._queue.Enqueue((title, body, options, onPick, speaker)); return; }
            _instance._prevModal = GameState.Current.ModalOpen;
            _instance.Build(canvas, title, body, options, onPick, speaker);
        }

        static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }

        static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = anchor;
            r.anchoredPosition = pos; r.sizeDelta = size;
            return r;
        }

        static Image Img(Transform parent, string name, Vector2 pos, Vector2 size, Color c, Vector2? anchor = null)
        {
            var img = Rect(parent, name, anchor ?? new Vector2(0, 1), pos, size).gameObject.AddComponent<Image>();
            img.color = c; img.raycastTarget = false;
            return img;
        }

        static TextMeshProUGUI Txt(Transform parent, string name, Vector2 pos, Vector2 size, string s, bool title, float px, Color c, bool wrap = false)
        {
            var t = Rect(parent, name, new Vector2(0, 1), pos, size).gameObject.AddComponent<TextMeshProUGUI>();
            var f = title ? HudView.TitleFont : HudView.BodyFont;
            if (f != null) t.font = f;
            t.text = s; t.fontSize = px; t.color = c; t.alignment = TextAlignmentOptions.TopLeft; t.raycastTarget = false;
            t.enableWordWrapping = wrap;
            if (title) t.characterSpacing = 4;
            return t;
        }

        /// <summary>Rough wrapped height of the body text (no layout pass needed): ~ 9.5 px a character at 18 px.</summary>
        static float BodyHeight(string body, float width)
        {
            int perLine = Mathf.Max(20, (int)(width / 9.5f)), lines = 0;
            foreach (var para in body.Split('\n')) lines += Mathf.Max(1, Mathf.CeilToInt(para.Length / (float)perLine));
            return lines * 23f + 4f;
        }

        private void Build(Canvas canvas, string title, string body, List<string> options, Action<int> onPick, ICombatant speaker)
        {
            GameState.Current.ModalOpen = true;
            _speaker = speaker; _pick = onPick; _full = body ?? ""; _shown = 0f; _rows.Clear();
            Sfx.Play("ui_click");

            // a light dim: the world (and whoever's talking) stays visible; it still eats clicks meant for the map
            _panel = UIBuildUtil.CreatePanel(canvas.transform, "ChoiceDimmer", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.28f));
            _panel.transform.SetAsLastSibling();

            bool face = speaker != null;
            float textX = face ? Pad + Face + 20f : Pad + 6f, textW = W - textX - Pad - 6f;
            float bodyH = BodyHeight(_full, textW);
            float h = Mathf.Max(face ? Face + 2 * Pad + 26f : 0f, 64f + bodyH + 16f + options.Count * Row + Pad);

            // the box: an edge, the fill, a lip along the top (the HUD's panels)
            var edge = Img(_panel.transform, "Dialog", new Vector2(0, 36), new Vector2(W, h), HudView.Edge, new Vector2(0.5f, 0f));
            var box = Img(edge.transform, "Fill", new Vector2(2, -2), new Vector2(W - 4, h - 4), new Color(0.06f, 0.065f, 0.07f, 0.97f)).transform;
            Img(box, "Lip", Vector2.zero, new Vector2(W - 4, 1), HudView.Lip);

            if (face)
            {
                // portrait: the speaker's own sprite, big, on a dark plate; their faction's band under it
                Img(box, "FaceEdge", new Vector2(Pad, -Pad), new Vector2(Face, Face), HudView.Edge);
                Img(box, "FacePlate", new Vector2(Pad + 2, -Pad - 2), new Vector2(Face - 4, Face - 4), Hex("1d2023"));
                var por = Img(box, "Face", new Vector2(Pad + 6, -Pad - 6), new Vector2(Face - 12, Face - 12), Color.white);
                por.sprite = UnitSprites.For(speaker); por.preserveAspect = true;
                if (por.sprite == null) por.color = new Color(0, 0, 0, 0);
                string faction = speaker is HumanEnemy he ? he.FactionName : speaker is Mutant ? "Mutant" : "";
                var band = Band.TryGetValue(faction ?? "", out var hx) ? Hex(hx) : HudView.Dim;
                Img(box, "Band", new Vector2(Pad, -Pad - Face - 6), new Vector2(Face, 6), band);
                Txt(box, "Faction", new Vector2(Pad, -Pad - Face - 16), new Vector2(Face, 16), (faction ?? "").ToUpperInvariant(), true, 11, band);
            }

            Txt(box, "Title", new Vector2(textX, -Pad + 2), new Vector2(textW, 26), title.ToUpperInvariant(), true, 20, HudView.Yellow);
            string who = speaker == null ? "" : speaker is HumanEnemy hs ? $"{hs.Name}  ·  {hs.Rank}" : speaker.Name;
            if (who.Length > 0) Txt(box, "Who", new Vector2(textX, -Pad - 26), new Vector2(textW, 18), who.ToUpperInvariant(), true, 12, HudView.Dim);
            _bodyText = Txt(box, "Body", new Vector2(textX, -64f), new Vector2(textW, bodyH), "", false, 18, HudView.Cream, true);

            // the answers: a key chip and the line; hovering lights the row's edge (Update)
            float y0 = -64f - bodyH - 16f;
            for (int i = 0; i < options.Count; i++)
            {
                int pick = i;
                var rowEdge = Img(box, "Option", new Vector2(textX - 4, y0 - i * Row), new Vector2(textW + 8, Row - 4), HudView.Lip);
                rowEdge.raycastTarget = true;
                Img(rowEdge.transform, "In", new Vector2(1, -1), new Vector2(textW + 6, Row - 6), Hex("17191c"));
                Img(rowEdge.transform, "Key", new Vector2(6, -5), new Vector2(20, 20), HudView.Edge);
                Txt(rowEdge.transform, "K", new Vector2(6, -7), new Vector2(20, 20), (i + 1).ToString(), true, 12, HudView.Yellow).alignment = TextAlignmentOptions.Top;
                Txt(rowEdge.transform, "Line", new Vector2(36, -5), new Vector2(textW - 40, Row - 8), options[i], false, 17, HudView.Cream);
                var btn = rowEdge.gameObject.AddComponent<Button>();
                btn.targetGraphic = rowEdge;
                btn.onClick.AddListener(() => Choose(pick));
                _rows.Add((rowEdge.rectTransform, rowEdge));
            }
        }

        private void Update()
        {
            if (_panel == null) return;
            var kb = Keyboard.current; var mouse = Mouse.current;
            bool typing = _shown < _full.Length;
            if (typing)
            {
                _shown = Mathf.Min(_full.Length, _shown + Time.unscaledDeltaTime * CharsPerSecond);
                bool skip = kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame) || mouse != null && mouse.leftButton.wasPressedThisFrame;
                if (skip) _shown = _full.Length;
                _bodyText.text = _full.Substring(0, (int)_shown);
                if (skip) return;   // that press only finished the line
            }
            if (mouse != null)
            {
                var at = mouse.position.ReadValue();
                foreach (var (rect, edge) in _rows)
                    edge.color = RectTransformUtility.RectangleContainsScreenPoint(rect, at, null) ? HudView.Yellow : HudView.Lip;
            }
            if (kb == null) return;
            var keys = new[] { kb.digit1Key, kb.digit2Key, kb.digit3Key, kb.digit4Key, kb.digit5Key };
            for (int i = 0; i < keys.Length && i < _rows.Count; i++)
                if (keys[i] != null && keys[i].wasPressedThisFrame) { _shown = _full.Length; Choose(i); return; }
            if (_rows.Count == 1 && kb.enterKey.wasPressedThisFrame) Choose(0);
        }

        private void Choose(int pick)
        {
            if (_panel == null) return;
            Sfx.Play("ui_click");
            var onPick = _pick;
            Destroy(_panel);
            _panel = null; _speaker = null; _rows.Clear();
            GameState.Current.ModalOpen = _prevModal || CharacterUI.IsOpen;   // whatever was under the dialog stays modal
            onPick(pick);
            if (_panel == null && _queue.Count > 0)
            {
                var next = _queue.Dequeue();
                var c = FindAnyObjectByType<Canvas>();
                if (c != null) Build(c, next.title, next.body, next.options, next.onPick, next.speaker); else next.onPick(0);
            }
        }
    }
}
