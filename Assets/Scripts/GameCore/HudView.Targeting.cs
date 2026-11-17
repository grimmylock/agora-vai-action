// The examine panel (Tab, "add an 'examine' prompt similar to BG3", Batch 77): whoever is under the mouse or the nearest enemy you can see - their gear, resistances, state and, for a boss, perks (Examine.cs).
// Esc closes it. (2026-11-17: the XCOM-style targeting panel that shared this file - click a body part, AP costs, aimed shots - was turn-based and is gone.)
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameCore
{
    public partial class HudView
    {
        public static HudView Instance;

        Mutant _exTarget;
        GameObject _exPanel;
        TextMeshProUGUI _exTitle, _exBody;

        (Image img, TextMeshProUGUI t) Btn(Transform parent, string name, Vector2 pos, Vector2 size, string label, float px, System.Action onClick)
        {
            var edge = Img(parent, name, new Vector2(0, 1), pos, size, Edge);
            edge.raycastTarget = true;
            Img(edge.transform, "In", new Vector2(0, 1), new Vector2(2, -2), size - new Vector2(4, 4), Fill);
            var t = Txt(edge.transform, "T", new Vector2(0, -(size.y - px - 6) / 2f), new Vector2(size.x, px + 8), label, true, px, Cream, TextAlignmentOptions.Center);
            t.characterSpacing = 2;
            edge.gameObject.AddComponent<Button>().onClick.AddListener(() => { onClick(); Sfx.Play("ui_click"); });
            return (edge, t);
        }

        void BuildExamine(Transform root)
        {
            Instance = this;
            var ex = Box(root, "ExaminePanel", new Vector2(1, 0.5f), new Vector2(-20, 60), new Vector2(430, 520));
            _exPanel = ex.parent.gameObject;
            ex.parent.GetComponent<Image>().raycastTarget = true;
            _exTitle = Txt(ex, "Title", new Vector2(14, -12), new Vector2(400, 26), "", true, 15, Cream);
            _exBody = Txt(ex, "Body", new Vector2(14, -48), new Vector2(400, 420), "", false, 16, Cream);
            _exBody.enableWordWrapping = true;
            Btn(ex, "Close", new Vector2(14, -472), new Vector2(160, 34), "CLOSE  ESC", 12, CloseExamine);
            _exPanel.SetActive(false);
        }

        public static void OpenExamine(Mutant e)
        {
            if (Instance == null || Instance._exPanel == null || e == null) return;
            Instance._exTarget = e;
            Instance._exPanel.SetActive(true);
        }

        public static void CloseExamine() { if (Instance != null && Instance._exPanel != null) { Instance._exPanel.SetActive(false); Instance._exTarget = null; } }

        /// <summary>Tab: examine whoever is under the mouse, or the nearest enemy you can see.</summary>
        public static void ExamineHovered()
        {
            if (Instance == null) return;
            if (Instance._exPanel != null && Instance._exPanel.activeSelf) { CloseExamine(); return; }
            var p = Player.Current;
            Mutant e = Instance.Hovered();
            if (e == null && p != null)
                e = EncounterState.Enemies.Where(u => u.IsAlive() && !Companion.Is(u) && Vision.Shows(u)).OrderBy(u => CombatantUtil.Distance(p, u)).FirstOrDefault();
            OpenExamine(e);
        }

        Mutant Hovered()
        {
            var mouse = Mouse.current;
            if (mouse == null || Camera.main == null) return null;
            var wpos = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());
            return TacticalGridView.WorldToGrid(wpos, out int x, out int y)
                ? EncounterState.Enemies.FirstOrDefault(u => u.X == x && u.Y == y && u.IsAlive() && Vision.Shows(u)) : null;
        }

        void UpdateExamine(Player p)
        {
            var k = Keyboard.current;
            if (k != null && k.escapeKey.wasPressedThisFrame) CloseExamine();
            if (k != null && k.tabKey.wasPressedThisFrame && !GameState.Current.ModalOpen) ExamineHovered();
            if (_exPanel != null && _exPanel.activeSelf)
            {
                var ex = _exTarget;
                if (ex == null || !ex.IsAlive() || !Vision.Shows(ex)) CloseExamine();
                else { _exTitle.text = Examine.Title(ex).ToUpperInvariant(); _exBody.text = Examine.Body(ex, p); }
            }
        }
    }
}
