// The siege's bar (2026-11-18, Yan: "the barrier itself needs an HP bar"): along the top while the Barrier is under attack (Tier4Story.SiegeOn): what is left of the wall (Barrier.Fraction)
// and how many of the horde are down. Green while it is whole, amber under 60 %, red under 30 %.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore
{
    public partial class HudView
    {
        RectTransform _sgBar; Image _sgFill, _sgGhost; TextMeshProUGUI _sgText; float _sgShown = 1f;

        void UpdateSiege()
        {
            bool on = Tier4Story.SiegeOn && !GameState.Current.ModalOpen;
            if (_sgBar == null)
            {
                if (!on) return;
                const float W = 560f;
                var edge = Img(_root.transform, "SiegeBar", new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(W, 14f), Edge);
                _sgBar = edge.rectTransform;
                Img(edge.transform, "Back", new Vector2(0f, 1f), new Vector2(2f, -2f), new Vector2(W - 4f, 10f), new Color(0.08f, 0.07f, 0.07f, 1f));
                _sgGhost = Img(edge.transform, "Ghost", new Vector2(0f, 1f), new Vector2(2f, -2f), new Vector2(W - 4f, 10f), new Color(0.95f, 0.85f, 0.6f, 1f));
                _sgFill = Img(edge.transform, "Fill", new Vector2(0f, 1f), new Vector2(2f, -2f), new Vector2(W - 4f, 10f), C("6aa04a"));
                _sgText = Txt(_root.transform, "SiegeText", Vector2.zero, new Vector2(W, 24f), "", true, 16, Cream, TextAlignmentOptions.Center);
                Pin(_sgText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -4f));
            }
            _sgBar.gameObject.SetActive(on); _sgText.gameObject.SetActive(on);
            if (!on) return;
            float f = Mathf.Clamp01(Barrier.Fraction);
            _sgShown = Mathf.MoveTowards(_sgShown, f, Time.unscaledDeltaTime * 0.35f);                 // the pale ghost trails the loss
            const float inner = 556f;
            _sgFill.rectTransform.sizeDelta = new Vector2(inner * f, 10f); _sgGhost.rectTransform.sizeDelta = new Vector2(inner * Mathf.Max(f, _sgShown), 10f);
            _sgFill.color = f > 0.6f ? C("6aa04a") : f > 0.3f ? C("d0a040") : C("b8302a");
            _sgText.text = $"THE BARRIER  {Mathf.CeilToInt(f * 100f)}%    HORDE  {Tier4Story.HordeDown} / {Tier4Story.Total}";
        }
    }
}
