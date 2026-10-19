// New: a static "keybindings" panel in the bottom-right corner, since the controls were only ever
// mentioned in a scrolling GameLog line at boot (easy to miss/lose once the log scrolls past it).
// Built once and never updated - the keybinds don't change at runtime, so unlike the other panels
// this doesn't need an Update() loop. Built at runtime under the existing Canvas, same convention
// as RuntimeGameLog/UIManager/InventoryUI (no scene wiring required). Added automatically by
// GameBootstrap.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore
{
    public class ControlsHint : MonoBehaviour
    {
        // Public (Batch 11): reused as-is by MainMenuUI's Options panel so the main menu's
        // "controls" screen and this in-play corner hint can't drift out of sync.
        public const string Text =
            "--- CONTROLS (real-time) ---\n" +
            "WASD: move   Shift: sprint   Ctrl: crouch\n" +
            "Space: dive + bullet time (40 Focus)\n" +
            "Right mouse: aim (steadier, look further)\n" +
            "Left mouse: fire (hold for automatics)\n" +
            "Crosshair height on a body = where you hit\n" +
            "R: reload / clear jam   Y: swap weapon\n" +
            "E: interact / loot body / pick up\n" +
            "F: search anomaly / area / quest\n" +
            "Q: switch ammo   U: flashlight\n" +
            "Z: rest (safe zone/campfire)   B: shop\n" +
            "J: quests  M: the Zone  P: character  I: pack\n" +
            "Mouse wheel: zoom   ESC: pause\n" +
            "F5: save / F9: load";

        private void Awake()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var panel = UIBuildUtil.CreatePanel(canvas.transform, "ControlsPanel", new Vector2(0.78f, 0f), new Vector2(1f, 0.36f),
                new Color(0f, 0f, 0f, 0.6f));
            panel.AddComponent<RectMask2D>();
            panel.GetComponent<Image>().raycastTarget = false;   // HUD: clicks reach the map
            UIBuildUtil.CreateText(panel.transform, "ControlsText", Vector2.zero, Vector2.one,
                Text, 11, Color.white, TextAlignmentOptions.TopLeft).raycastTarget = false;
        }
    }
}
