// Batch 72 (Yan: "buttons for stuff in the bottom of the screen where the player can choose which action to take similar
// to xcom, with icons"): the action bar, bottom centre between the player and weapon cards. One button per action in
// CombatActions (16x16 pixel icons drawn by ArtSource/make_action_icons.py, shown at 2x), its hotkey in the corner and
// its AP cost underneath. Greyed out when it can't be used; yellow edge while a stance or toggle is on. Hovering shows
// what it does and, if greyed, why. In a fight it shows the combat actions, exploring the walking ones.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameCore
{
    public partial class HudView
    {
        const float BtnSize = 46, BtnGap = 3, BarY = 34;

        class ActBtn
        {
            public ActionDef Def;
            public RectTransform Rect;
            public Image Edge, Icon;
            public TextMeshProUGUI Key, Ap, Alt;
        }

        readonly List<ActBtn> _acts = new List<ActBtn>();
        GameObject _tip;
        TextMeshProUGUI _tipTitle, _tipBody;
        Canvas _cv;

        void BuildActionBar(Transform root)
        {
            _cv = _canvas != null ? _canvas.GetComponent<Canvas>() : null;
            foreach (var def in CombatActions.All)
            {
                var b = new ActBtn { Def = def };
                b.Edge = Img(root, "Act_" + def.Id, new Vector2(0.5f, 0), new Vector2(0, BarY), new Vector2(BtnSize, BtnSize), Edge);
                b.Rect = b.Edge.rectTransform;
                b.Edge.raycastTarget = true;   // clicks on the bar don't fall through to the map (PlayerController checks the EventSystem)
                Img(b.Edge.transform, "In", new Vector2(0, 1), new Vector2(2, -2), new Vector2(BtnSize - 4, BtnSize - 4), Fill);
                b.Icon = Img(b.Edge.transform, "Icon", new Vector2(0, 1), new Vector2(7, -4), new Vector2(32, 32), Color.white);
                var tex = Resources.Load<Texture2D>("Sprites/icon_act_" + def.Icon);
                if (tex != null)
                {
                    tex.filterMode = FilterMode.Point;
                    b.Icon.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 16);
                }
                else b.Icon.color = new Color(0, 0, 0, 0);
                b.Alt = Txt(b.Edge.transform, "Alt", new Vector2(0, -14), new Vector2(BtnSize, 20), tex == null ? def.Name.Substring(0, System.Math.Min(4, def.Name.Length)).ToUpperInvariant() : "", true, 11, Cream, TextAlignmentOptions.Center);
                b.Alt.characterSpacing = 1;
                b.Key = Txt(b.Edge.transform, "Key", new Vector2(3, -2), new Vector2(30, 14), def.Key, true, 9, Dim);
                b.Key.characterSpacing = 1;
                b.Ap = Txt(b.Edge.transform, "Ap", new Vector2(0, -34), new Vector2(BtnSize - 4, 12), "", true, 9, Yellow, TextAlignmentOptions.TopRight);
                b.Ap.characterSpacing = 1;
                var id = def.Id;
                b.Edge.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                {
                    if (Player.Current == null) return;
                    CombatActions.Press(id, Player.Current);
                    Sfx.Play("ui_click");
                });
                _acts.Add(b);
            }

            var tip = Box(root, "ActionTip", new Vector2(0.5f, 0), new Vector2(0, BarY + BtnSize + 8), new Vector2(460, 104));
            _tip = tip.parent.gameObject;
            _tipTitle = Txt(tip, "Title", new Vector2(12, -8), new Vector2(436, 22), "", true, 14, Cream);
            _tipBody = Txt(tip, "Body", new Vector2(12, -34), new Vector2(436, 66), "", false, 15, Cream);
            _tipBody.enableWordWrapping = true;
            _tip.SetActive(false);
        }

        void UpdateActionBar(Player p)
        {
            if (_acts.Count == 0) return;
            var shown = CombatActions.Visible(p);
            float x0 = -(shown.Count - 1) * (BtnSize + BtnGap) / 2f;
            var mouse = Mouse.current;
            Vector2 mp = mouse != null ? mouse.position.ReadValue() : new Vector2(-1, -1);
            Camera cam = _cv != null && _cv.renderMode != RenderMode.ScreenSpaceOverlay ? _cv.worldCamera : null;
            ActBtn hover = null;
            string hoverWhy = null;
            foreach (var b in _acts)
            {
                int i = shown.IndexOf(b.Def);
                b.Edge.gameObject.SetActive(i >= 0);
                if (i < 0) continue;
                b.Rect.anchoredPosition = new Vector2(x0 + i * (BtnSize + BtnGap), BarY);
                string why = CombatActions.WhyNot(b.Def, p);
                bool on = b.Def.On(p), ok = why == null;
                b.Edge.color = on ? Yellow : ok ? Edge : Lip;
                b.Icon.color = b.Icon.sprite == null ? new Color(0, 0, 0, 0) : ok ? Color.white : new Color(1, 1, 1, 0.3f);
                b.Alt.color = ok ? Cream : Dim;
                b.Key.color = on ? Yellow : Dim;
                int ap = b.Def.Ap(p);
                b.Ap.text = ap > 0 ? ap + " AP" : "";
                b.Ap.color = ok ? Yellow : Dim;
                if (RectTransformUtility.RectangleContainsScreenPoint(b.Rect, mp, cam)) { hover = b; hoverWhy = why; }
            }
            _tip.SetActive(hover != null);
            if (hover == null) return;
            var d = hover.Def;
            int cost = d.Ap(p);
            _tipTitle.text = $"{d.Name.ToUpperInvariant()}   <color=#7d7a70>[{d.Key}]</color>" + (cost > 0 ? $"   <color=#e8c547>{cost} AP</color>" : "");
            _tipBody.text = d.Desc + (string.IsNullOrEmpty(hoverWhy) ? "" : $"\n<color=#c8443a>{hoverWhy}</color>");
        }
    }
}
