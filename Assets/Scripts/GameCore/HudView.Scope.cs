// 2026-11-16 (Yan: scopes "with an overlay to show we're looking through the scope"): while you aim through a scope (RMB, Optics.Scoped) the screen darkens round a circle that follows
// the crosshair, with a crosshair drawn in it (a plain cross for the PU, ticks for the ACOG, mil-dots for the 8x). It fades in as the aim steadies. Local screen only: nothing to sync.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameCore
{
    public partial class HudView
    {
        RectTransform _scopeRoot;
        Image _scTop, _scBottom, _scLeft, _scRight, _scRing;
        readonly List<Image> _scLines = new List<Image>();
        float _scopeA;
        static Sprite _ringSprite;
        const float ScopeDark = 0.9f;

        static Sprite RingSprite()
        {
            if (_ringSprite != null) return _ringSprite;
            const int N = 256; var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[N * N]; float c = (N - 1) * 0.5f, R = N * 0.5f;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / R;                 // 0 centre .. 1 edge of the square
                    float a = Mathf.Clamp01((d - 0.93f) / 0.07f);                                       // clear inside, the edge fades to black by the rim
                    if (d > 0.985f) a = 1f;
                    px[y * N + x] = new Color32(0, 0, 0, (byte)(a * 255f));
                }
            tex.SetPixels32(px); tex.Apply();
            return _ringSprite = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
        }
        Image ScopeImg(string name, Sprite s = null)
        {
            var img = Img(_scopeRoot, name, Vector2.zero, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 1f));
            img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            if (s != null) img.sprite = s;
            return img;
        }
        static void Place(Image img, float cx, float cy, float w, float h) { var r = img.rectTransform; r.anchoredPosition = new Vector2(cx, cy); r.sizeDelta = new Vector2(Mathf.Max(0f, w), Mathf.Max(0f, h)); img.gameObject.SetActive(w > 0.5f && h > 0.5f); }

        void UpdateScope(Player p)
        {
            var w = p?.EquippedWeapon; var def = Optics.OpticOf(w);
            var mouse = Mouse.current;
            bool want = p != null && p.IsAlive() && Rt.On && mouse != null && Rt.Me.Aiming && def != null && def.Kind == "scope" && !GameState.Current.ModalOpen && !ChoiceUI.IsOpen;
            float steady = want ? Gunplay.AimOf(p).Steady : 0f;
            _scopeA = Mathf.MoveTowards(_scopeA, want ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((steady - 0.1f) / 0.55f)) : 0f, Time.unscaledDeltaTime * 4f);
            if (_scopeA <= 0.01f) { if (_scopeRoot != null) _scopeRoot.gameObject.SetActive(false); return; }
            if (_scopeRoot == null)
            {
                _scopeRoot = Rect(_root.transform, "ScopeOverlay", new Vector2(0f, 0f), Vector2.zero, Vector2.zero);
                _scopeRoot.anchorMin = Vector2.zero; _scopeRoot.anchorMax = Vector2.one; _scopeRoot.offsetMin = _scopeRoot.offsetMax = Vector2.zero; _scopeRoot.pivot = new Vector2(0.5f, 0.5f);
                _scopeRoot.SetAsFirstSibling();                                                         // under the HUD text, over the world
                _scTop = ScopeImg("Top"); _scBottom = ScopeImg("Bottom"); _scLeft = ScopeImg("Left"); _scRight = ScopeImg("Right"); _scRing = ScopeImg("Ring", RingSprite());
                for (int i = 0; i < 16; i++) _scLines.Add(ScopeImg("Line" + i));
                _scopeRoot.anchoredPosition = Vector2.zero;
            }
            if (_canvasC == null && _canvas != null) _canvasC = _canvas.GetComponent<Canvas>();
            float sc = _canvasC != null && _canvasC.scaleFactor > 0f ? _canvasC.scaleFactor : 1f;
            float W = Screen.width / sc, H = Screen.height / sc; var mp = mouse.position.ReadValue(); float mx = mp.x / sc, my = mp.y / sc;
            float R = def.View * H; mx = Mathf.Clamp(mx, R * 0.3f, W - R * 0.3f); my = Mathf.Clamp(my, R * 0.3f, H - R * 0.3f);
            var dark = new Color(0f, 0f, 0f, ScopeDark * _scopeA);
            foreach (var im in new[] { _scTop, _scBottom, _scLeft, _scRight, _scRing }) im.color = dark;
            _scRing.rectTransform.anchorMin = _scRing.rectTransform.anchorMax = Vector2.zero; Place(_scRing, mx, my, 2f * R, 2f * R);
            foreach (var im in new[] { _scTop, _scBottom, _scLeft, _scRight }) { im.rectTransform.anchorMin = im.rectTransform.anchorMax = Vector2.zero; }
            Place(_scTop, W * 0.5f, (my + R + H) * 0.5f, W + 4f, H - (my + R));
            Place(_scBottom, W * 0.5f, (my - R) * 0.5f, W + 4f, my - R);
            Place(_scLeft, (mx - R) * 0.5f, my, mx - R, 2f * R);
            Place(_scRight, (mx + R + W) * 0.5f, my, W - (mx + R), 2f * R);
            // the reticle in the glass
            var lc = new Color(0f, 0f, 0f, 0.85f * _scopeA); int n = 0;
            void Line(float cx, float cy, float lw, float lh) { if (n < _scLines.Count) { var im = _scLines[n++]; im.rectTransform.anchorMin = im.rectTransform.anchorMax = Vector2.zero; im.color = lc; Place(im, mx + cx, my + cy, lw, lh); } }
            float gap = 0.07f * R, len = 0.92f * R;
            if (def.Id == "pu2") { Line(-(gap + len) * 0.5f, 0, len - gap, 2f); Line((gap + len) * 0.5f, 0, len - gap, 2f); Line(0, -(gap + len) * 0.5f, 2f, len - gap); Line(0, (gap + len) * 0.5f, 2f, 0.5f * (len - gap)); }
            else if (def.Id == "acog") { Line(-0.45f * R, 0, 0.8f * R, 2f); Line(0.45f * R, 0, 0.8f * R, 2f); Line(0, -0.5f * R, 2f, R); for (int k = 1; k <= 3; k++) Line(0, -0.1f * R * k - gap, 0.1f * R * (4 - k) + 6f, 2f); }
            else { Line(-(gap + len) * 0.5f, 0, len - gap, 2f); Line((gap + len) * 0.5f, 0, len - gap, 2f); Line(0, -(gap + len) * 0.5f, 2f, len - gap); Line(0, (gap + len) * 0.5f, 2f, len - gap); for (int k = 1; k <= 4; k++) { Line(k * 0.18f * R, 0, 3f, 7f); Line(-k * 0.18f * R, 0, 3f, 7f); } }   // 8x: mil-dots
            for (; n < _scLines.Count; n++) _scLines[n].gameObject.SetActive(false);
            _scopeRoot.gameObject.SetActive(true);
        }
    }
}
