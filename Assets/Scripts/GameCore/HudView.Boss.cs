// Boss 1: a boss's HUD - the name card over the middle of the screen when the fight starts (and on phase 2, the break, the
// end), his health bar along the top with his name, and the state under it: STAGGERED (go!), EXECUTE (E), DEFEATED; and
// the perfect parry's cold edge round the screen (Boss.Punch).
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore
{
    public partial class HudView
    {
        Image _edge;
        RectTransform _bossBar; Image _bossFill, _bossGhost; TextMeshProUGUI _bossName, _bossState, _splash, _splashSub;
        float _ghost = 1f;
        const float BossW = 560f;

        void UpdateBoss()
        {
            if (_bossBar == null)
            {
                var edge = Img(_root.transform, "BossBar", new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(BossW, 14f), Edge);
                _bossBar = edge.rectTransform;
                Img(edge.transform, "Back", new Vector2(0f, 1f), new Vector2(2f, -2f), new Vector2(BossW - 4f, 10f), new Color(0.08f, 0.07f, 0.07f, 1f));
                _bossGhost = Img(edge.transform, "Ghost", new Vector2(0f, 1f), new Vector2(2f, -2f), new Vector2(BossW - 4f, 10f), new Color(0.95f, 0.85f, 0.6f, 1f));
                _bossFill = Img(edge.transform, "Fill", new Vector2(0f, 1f), new Vector2(2f, -2f), new Vector2(BossW - 4f, 10f), C("b8302a"));
                _bossName = Txt(_root.transform, "BossName", Vector2.zero, new Vector2(BossW, 26f), "", true, 18, Cream, TextAlignmentOptions.Center);
                Pin(_bossName.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -26f));
                _bossState = Txt(_root.transform, "BossState", Vector2.zero, new Vector2(BossW, 24f), "", true, 16, Yellow, TextAlignmentOptions.Center);
                Pin(_bossState.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -72f));
                _splash = Txt(_root.transform, "Splash", Vector2.zero, new Vector2(1400f, 90f), "", true, 64, Cream, TextAlignmentOptions.Center);
                Pin(_splash.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f));
                _splash.outlineWidth = 0.25f; _splash.outlineColor = new Color32(0, 0, 0, 255);
                _splashSub = Txt(_root.transform, "SplashSub", Vector2.zero, new Vector2(1400f, 40f), "", false, 24, Yellow, TextAlignmentOptions.Center);
                Pin(_splashSub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f));
            }

            // the perfect parry: a cold edge closes in round the screen and fades with the slow motion
            if (_edge == null) _edge = EdgeImage("ParryEdge", 0.74f, 1.18f);
            float punch = Boss.Punch;
            _edge.gameObject.SetActive(punch > 0.01f);
            if (punch > 0.01f) _edge.color = new Color(0.72f, 0.9f, 1f, 0.6f * punch);

            var f = Boss.Current;
            bool on = f != null && !GameState.Current.ModalOpen;
            _bossBar.gameObject.SetActive(on); _bossName.gameObject.SetActive(on); _bossState.gameObject.SetActive(on);
            if (on)
            {
                float hp = Mathf.Clamp01(f.Unit.Hp / (float)f.Def.Hp);
                if (f.Broken || f.Finished) hp = 0f;
                _ghost = Mathf.Max(hp, Mathf.MoveTowards(_ghost, hp, Time.unscaledDeltaTime * 0.35f));   // the chunk you just took drains after it
                _bossFill.rectTransform.sizeDelta = new Vector2((BossW - 4f) * hp, 10f);
                _bossGhost.rectTransform.sizeDelta = new Vector2((BossW - 4f) * _ghost, 10f);
                bool open = Boss.Staggered(f.Unit);
                _bossFill.color = open ? Color.Lerp(C("b8302a"), Color.white, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 30f)) : f.Phase == 2 ? Color.Lerp(C("b8302a"), C("ff5a3a"), 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f)) : C("b8302a");
                _bossName.text = f.Def.Name.ToUpperInvariant() + (f.Phase == 2 ? "  <color=#ff5a3a>II</color>" : "");
                int gd = Boss.GuardDamage(f);                                       // perfect parries toward the stagger
                _bossState.text = f.Finished ? "DEFEATED" : f.Broken ? "EXECUTE - E" : open ? "STAGGERED" : Boss.Open(f.Unit) ? "OPEN" : gd > 0 ? $"GUARD {gd}/{f.Def.ParriesToStagger}" : "";
                _bossState.color = f.Broken ? new Color(1f, 0.35f, 0.3f) : Yellow;
            }
            else _ghost = 1f;

            float age = Time.unscaledTime - Boss.SplashAt;
            bool splash = age >= 0f && age < Boss.SplashSeconds && !GameState.Current.ModalOpen;
            _splash.gameObject.SetActive(splash); _splashSub.gameObject.SetActive(splash);
            if (splash)
            {
                float a = Mathf.Clamp01(age / 0.15f) * Mathf.Clamp01((Boss.SplashSeconds - age) / 0.6f);
                float grow = 1f + 0.06f * age;                                  // it slowly pushes toward you
                _splash.text = Boss.SplashTitle; _splashSub.text = Boss.SplashSub;
                var c = Cream; c.a = a; _splash.color = c; var y = Yellow; y.a = a; _splashSub.color = y;
                _splash.rectTransform.localScale = Vector3.one * grow;
                _splash.characterSpacing = 6f + 10f * Mathf.Clamp01(age / Boss.SplashSeconds);   // and spreads out
            }
        }

        /// <summary>A full-screen vignette (white, tinted by its colour): clear in the middle, solid at the edges from `inner` to `outer`
        /// (radius, 1 = the screen's half-size). Under the HUD's panels.</summary>
        Image EdgeImage(string name, float inner, float outer)
        {
            const int N = 64; var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f, r = Mathf.Sqrt(dx * dx * 0.8f + dy * dy * 0.8f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(inner, outer, r))));
                }
            tex.Apply();
            var img = Img(_root.transform, name, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Color.clear);
            var er = img.rectTransform; er.anchorMin = Vector2.zero; er.anchorMax = Vector2.one; er.sizeDelta = Vector2.zero;
            img.sprite = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            img.transform.SetAsFirstSibling();
            return img;
        }

        // ---- Hurt (Yan: "there's no feedback to getting hit, maybe a little red flash on the corners of the screen / character flashes a
        // little red similar to the visor boss"): read off your health dropping - any blow, bullet, bite or bleed - a red edge closes
        // in round the screen, stronger for a bigger hit, and your body flashes red (TacticalGridView reads PlayerHurt) ----
        Image _hurtEdge; int _lastHp = -1; Player _hurtOf;
        public static float HurtAt = -99f, HurtK;
        public const float HurtFade = 0.5f;
        /// <summary>0..1: how red the local player's body is right now.</summary>
        public static float PlayerHurt => Mathf.Clamp01(1f - (Time.unscaledTime - HurtAt) / 0.3f) * Mathf.Max(0.6f, HurtK);

        void UpdateHurt()
        {
            var p = Player.Current;
            if (p == null) { _lastHp = -1; return; }
            if (p != _hurtOf) { _hurtOf = p; _lastHp = p.Hp; }
            if (p.Hp < _lastHp && _lastHp > 0)
            {
                float k = Mathf.Clamp((_lastHp - p.Hp) / (float)Mathf.Max(1, p.MaxHp) * 6f, 0.3f, 1f);   // a sixth of your health or more: the full flash
                if (Time.unscaledTime - HurtAt > 0.08f || k > HurtK) { HurtAt = Time.unscaledTime; HurtK = k; }
            }
            _lastHp = p.Hp;
            if (_hurtEdge == null) _hurtEdge = EdgeImage("HurtEdge", 0.62f, 1.12f);
            float a = Mathf.Clamp01(1f - (Time.unscaledTime - HurtAt) / HurtFade) * HurtK;
            _hurtEdge.gameObject.SetActive(a > 0.01f);
            if (a > 0.01f) _hurtEdge.color = new Color(0.78f, 0.04f, 0.03f, 0.7f * a);
        }

        static void Pin(RectTransform r, Vector2 anchor, Vector2 pos) { r.anchorMin = r.anchorMax = r.pivot = anchor; r.anchoredPosition = pos; }
    }
}
