// New (Batch 11): a real main menu instead of GameBootstrap silently auto-starting/auto-loading a
// game the instant the scene opens - New Game, Load Game (grayed out with no save), Options
// (controls only, for now), Quit. GameState.Mode defaults to GameMode.MainMenu (see GameState.cs)
// and PlayerController/TurnLoop already gate all world input/simulation on
// "Mode == GameMode.Tactical", so simply not starting a game yet is enough to keep everything else
// idle behind this panel - no separate pause/blocking flag needed. Built at runtime under the
// existing Canvas, same convention as ShopUI/DeathScreen. Added automatically by GameBootstrap.
// Batch 123 (Yan: "a main menu screen of someplace in the map showcasing the passage of time"): the menu is a column on
// the left over the live village (TacticalGridView backdrop, MenuBackdrop); the place, the day, the clock and the weather
// sit in the bottom corner and tick along with it. The HUD's fonts and colours.
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameCore
{
    public class MainMenuUI : MonoBehaviour
    {
        private static MainMenuUI _instance;

        private GameObject _panel;
        private GameObject _optionsPanel;
        private Button _loadButton, _continueButton;
        private GameObject _slotsPanel;
        private string _confirmDelete;
        private TextMeshProUGUI _clock;
        private readonly List<(RectTransform r, TextMeshProUGUI t, Button b)> _items = new List<(RectTransform, TextMeshProUGUI, Button)>();

        private void Awake() => _instance = this;

        /// <summary>Called from GameBootstrap at boot, and from PlayerController's future "back to
        /// menu" path if one is ever added - static so callers don't need to hold the component
        /// reference GameBootstrap added.</summary>
        public static void Show() => _instance?.ShowInternal();

        private void ShowInternal()
        {
            BuildPanelIfNeeded();
            if (_panel == null) return; // no Canvas in the scene - already warned in BuildPanelIfNeeded

            _panel.SetActive(true);
            _panel.transform.SetAsLastSibling();
            if (_optionsPanel != null) _optionsPanel.SetActive(false);
            CloseSlots();
            bool any = SaveSystem.HasSave();
            if (_loadButton != null) _loadButton.interactable = any;
            if (_continueButton != null) _continueButton.interactable = any;
        }

        private GameObject _log;   // the game log: hidden while the menu is up (it was under the old opaque menu)

        private void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
            if (_log != null) _log.SetActive(true);
        }

        static TextMeshProUGUI Text(Transform parent, string name, Vector2 min, Vector2 max, string s, bool title, int px, Color c, TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var t = UIBuildUtil.CreateText(parent, name, min, max, s, px, c, align);
            var f = title ? HudView.TitleFont : HudView.BodyFont;
            if (f != null) t.font = f;
            if (title) t.characterSpacing = 4;
            t.raycastTarget = false;
            return t;
        }

        private Button Item(Transform parent, int row, string label, System.Action onClick)
        {
            float y1 = 0.555f - row * 0.07f, y0 = y1 - 0.055f;
            var go = UIBuildUtil.CreatePanel(parent, label + "Button", new Vector2(0.1f, y0), new Vector2(0.92f, y1), new Color(0, 0, 0, 0));
            var b = go.AddComponent<Button>();
            b.targetGraphic = go.GetComponent<Image>();
            b.onClick.AddListener(() => { Sfx.Play("ui_click"); onClick(); });
            var t = Text(go.transform, "Label", Vector2.zero, Vector2.one, label.ToUpperInvariant(), true, 24, HudView.Cream);
            _items.Add((go.GetComponent<RectTransform>(), t, b));
            return b;
        }

        private void BuildPanelIfNeeded()
        {
            if (_panel != null) return;

            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("MainMenuUI: no Canvas found in the scene - can't show the main menu.");
                return;
            }

            // a see-through root: the village shows everywhere but under the column
            _panel = UIBuildUtil.CreatePanel(canvas.transform, "MainMenuPanel", Vector2.zero, Vector2.one, new Color(0, 0, 0, 0));
            _panel.GetComponent<Image>().raycastTarget = false;
            var col = UIBuildUtil.CreatePanel(_panel.transform, "Column", Vector2.zero, new Vector2(0.3f, 1f), new Color(0.06f, 0.065f, 0.07f, 0.84f));
            UIBuildUtil.CreatePanel(_panel.transform, "ColumnEdge", new Vector2(0.3f, 0f), new Vector2(0.3015f, 1f), HudView.Edge);

            Text(col.transform, "Title", new Vector2(0.1f, 0.7f), new Vector2(0.95f, 0.8f), "AGORA VAI", true, 56, HudView.Cream);
            UIBuildUtil.CreatePanel(col.transform, "Rule", new Vector2(0.1f, 0.685f), new Vector2(0.62f, 0.688f), HudView.Yellow);

            Item(col.transform, 0, "New Game", OnNewGame);
            _continueButton = Item(col.transform, 1, "Continue", () => { if (SaveSystem.Load(SaveSystem.Slots().FirstOrDefault()?.CharacterId)) Hide(); });
            _loadButton = Item(col.transform, 2, "Load Game", OnLoadGame);
            Item(col.transform, 3, "Testing Ground", () => { Hide(); TestingGround.Start(); });
            Item(col.transform, 4, "Options", ToggleOptions);
            Item(col.transform, 5, "Quit", OnQuit);

            // where you are and when: the menu's own clock (MenuBackdrop) ticking along
            Text(col.transform, "Place", new Vector2(0.1f, 0.09f), new Vector2(0.95f, 0.125f), "BONFIRE VILLAGE", true, 16, HudView.Cream);
            _clock = Text(col.transform, "Clock", new Vector2(0.1f, 0.055f), new Vector2(0.95f, 0.09f), "", true, 13, HudView.Dim);

            BuildOptionsPanel();
        }

        /// <summary>ponytail: one screen, controls only (the ask) - a settings list/tabs can be
        /// added here later if more than one option category ever shows up.</summary>
        private void BuildOptionsPanel()
        {
            _optionsPanel = UIBuildUtil.CreatePanel(_panel.transform, "OptionsPanel", new Vector2(0.34f, 0.1f), new Vector2(0.78f, 0.9f), HudView.Edge);
            var fill = UIBuildUtil.CreatePanel(_optionsPanel.transform, "Fill", Vector2.zero, Vector2.one, new Color(0.06f, 0.065f, 0.07f, 0.96f));
            var fr = fill.GetComponent<RectTransform>(); fr.offsetMin = new Vector2(2, 2); fr.offsetMax = new Vector2(-2, -2);

            Text(fill.transform, "OptionsTitle", new Vector2(0.06f, 0.88f), new Vector2(0.94f, 0.96f), "CONTROLS", true, 22, HudView.Yellow);
            Text(fill.transform, "OptionsText", new Vector2(0.06f, 0.14f), new Vector2(0.94f, 0.86f), ControlsHint.Text, false, 16, HudView.Cream, TextAlignmentOptions.TopLeft);
            var back = UIBuildUtil.CreateButton(fill.transform, "BackButton", new Vector2(0.38f, 0.03f), new Vector2(0.62f, 0.11f), "Back", () => _optionsPanel.SetActive(false));
            back.GetComponent<Image>().color = HudView.Lip;

            _optionsPanel.SetActive(false);
        }

        private void Update()
        {
            if (_panel == null || !_panel.activeSelf) return;
            if (_log == null) _log = GameObject.Find("CombatLogBackground");
            if (_log != null && _log.activeSelf) _log.SetActive(false);
            if (_clock != null) _clock.text = MenuBackdrop.Ready ? MenuBackdrop.Label() : "";
            var mouse = Mouse.current;
            var at = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
            foreach (var (r, t, b) in _items)
            {
                bool hot = b.interactable && mouse != null && RectTransformUtility.RectangleContainsScreenPoint(r, at, null);
                t.color = !b.interactable ? HudView.Dim : hot ? HudView.Yellow : HudView.Cream;
                string bare = t.text.TrimStart('>', ' ');
                t.text = hot ? "> " + bare : bare;
            }
        }

        private void ToggleOptions() { CloseSlots(); _optionsPanel.SetActive(!_optionsPanel.activeSelf); }

        private void CloseSlots() { if (_slotsPanel != null) Destroy(_slotsPanel); _slotsPanel = null; }

        /// <summary>RT 7x: the save slots - one per character, the last played first: load or delete (a second click confirms).
        /// ponytail: shows the newest 9; a scroll list when someone has more.</summary>
        private void ShowSlots()
        {
            CloseSlots();
            if (_optionsPanel != null) _optionsPanel.SetActive(false);
            _slotsPanel = UIBuildUtil.CreatePanel(_panel.transform, "SlotsPanel", new Vector2(0.34f, 0.1f), new Vector2(0.9f, 0.9f), HudView.Edge);
            var fill = UIBuildUtil.CreatePanel(_slotsPanel.transform, "Fill", Vector2.zero, Vector2.one, new Color(0.06f, 0.065f, 0.07f, 0.96f));
            var fr = fill.GetComponent<RectTransform>(); fr.offsetMin = new Vector2(2, 2); fr.offsetMax = new Vector2(-2, -2);
            Text(fill.transform, "SlotsTitle", new Vector2(0.04f, 0.9f), new Vector2(0.96f, 0.97f), "CHARACTERS", true, 22, HudView.Yellow);
            var slots = SaveSystem.Slots();
            for (int i = 0; i < slots.Count && i < 9; i++)
            {
                var s = slots[i];
                float y1 = 0.88f - i * 0.083f, y0 = y1 - 0.07f;
                string when = System.DateTime.TryParse(s.SavedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt) ? dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : "";
                Text(fill.transform, "Slot", new Vector2(0.04f, y0), new Vector2(0.7f, y1),
                    $"<b>{s.Name}</b>  <color=#9a9f94>{s.Rank} - day {s.Day}, {s.Place} - {when}</color>", false, 16, HudView.Cream, TextAlignmentOptions.MidlineLeft);
                var load = UIBuildUtil.CreateButton(fill.transform, "Load", new Vector2(0.71f, y0 + 0.008f), new Vector2(0.84f, y1 - 0.008f), "Load", () => { if (SaveSystem.Load(s.CharacterId)) { CloseSlots(); Hide(); } });
                load.GetComponent<Image>().color = HudView.Lip;
                bool sure = _confirmDelete == s.CharacterId;
                var del = UIBuildUtil.CreateButton(fill.transform, "Delete", new Vector2(0.85f, y0 + 0.008f), new Vector2(0.97f, y1 - 0.008f), sure ? "Sure?" : "Delete", () =>
                {
                    if (_confirmDelete == s.CharacterId) { SaveSystem.Delete(s.CharacterId); _confirmDelete = null; }
                    else _confirmDelete = s.CharacterId;
                    ShowInternal(); ShowSlots();
                });
                del.GetComponent<Image>().color = sure ? new Color(0.5f, 0.12f, 0.1f, 1f) : new Color(0.2f, 0.2f, 0.2f, 1f);
            }
            if (slots.Count == 0) Text(fill.transform, "None", new Vector2(0.04f, 0.7f), new Vector2(0.96f, 0.85f), "No characters yet.", false, 16, HudView.Dim);
            var back = UIBuildUtil.CreateButton(fill.transform, "BackButton", new Vector2(0.38f, 0.03f), new Vector2(0.62f, 0.1f), "Back", () => { _confirmDelete = null; CloseSlots(); });
            back.GetComponent<Image>().color = HudView.Lip;
        }

        private void OnNewGame()
        {
            Hide();
            CharacterUI.ShowCreation();
        }

        private void OnLoadGame() { if (_slotsPanel != null) CloseSlots(); else ShowSlots(); }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
