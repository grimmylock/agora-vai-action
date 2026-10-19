// New: shows a "YOU DIED" overlay with a Restart button when the player dies. Previously
// TurnLoop.CheckCombatEnd's death branch just left CurrentTurn stuck at Turn.CombatOver forever
// with zero on-screen feedback and no way to continue - PlayerController blocks all input outside
// Turn.Player, so a death silently froze the game. CheckCombatEnd's victory branch already resets
// CurrentTurn back to Player in the same call, so CombatOver can ONLY mean death by the time
// anything else observes it - no separate "why are we CombatOver" check needed here.
//
// Built at runtime under the existing Canvas, same convention as RuntimeGameLog - no scene wiring
// required. Added automatically by GameBootstrap.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore
{
    public class DeathScreen : MonoBehaviour
    {
        private GameObject _panel;
        private Button _loadButton;
        private bool _shown;

        private void Update()
        {
            bool dead = GameState.Current.CurrentTurn == Turn.CombatOver;
            if (dead && !_shown) Show();
            else if (!dead && _shown) Hide();
        }

        private void Show()
        {
            _shown = true;
            GameState.Current.ModalOpen = true;

            if (_panel != null)
            {
                _panel.SetActive(true);
                _panel.transform.SetAsLastSibling();
                if (_loadButton != null) _loadButton.interactable = SaveSystem.HasSave();
                return;
            }

            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("DeathScreen: no Canvas found in the scene - can't show the death overlay.");
                return;
            }

            _panel = UIBuildUtil.CreatePanel(canvas.transform, "DeathScreen", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.85f));

            UIBuildUtil.CreateText(_panel.transform, "DeathText", new Vector2(0.15f, 0.55f), new Vector2(0.85f, 0.75f),
                "YOU DIED", 48, new Color(0.85f, 0.2f, 0.15f), TextAlignmentOptions.Center);

            UIBuildUtil.CreateButton(_panel.transform, "RestartButton", new Vector2(0.4f, 0.35f), new Vector2(0.6f, 0.45f),
                "Restart", Restart);

            // New: a death used to only offer starting over from scratch - if the player had a
            // save from earlier in the run, there was no way back to it short of quitting out to
            // the (nonexistent, at the time) main menu. Same SaveSystem.HasSave()/Load() pattern
            // MainMenuUI's own Load Game button already uses.
            _loadButton = UIBuildUtil.CreateButton(_panel.transform, "LoadLastSaveButton", new Vector2(0.4f, 0.25f), new Vector2(0.6f, 0.35f),
                "Load Last Save", LoadLastSave);
            _loadButton.interactable = SaveSystem.HasSave();
        }

        private void Hide()
        {
            _shown = false;
            GameState.Current.ModalOpen = false;
            if (_panel != null) _panel.SetActive(false);
        }

        private void Restart()
        {
            Hide();
            CharacterUI.ShowCreation();
        }

        private void LoadLastSave()
        {
            if (SaveSystem.Load()) Hide(); // SaveSystem.Load already logs the failure reason on false
        }
    }
}
