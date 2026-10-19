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
            "--- CONTROLS ---\n" +
            "WASD / arrows: walk (out of combat)\n" +
            "Click: move / attack\n" +
            "Right-click: shoot barrel / door / cover\n" +
            "E: interact / story landmarks\n" +
            "F: search anomaly / area / quest\n" +
            "R: reload / clear jam\n" +
            "X: single / burst fire\n" +
            "L: aim for legs / arms / body / head / armor / grenade pouch\n" +
            "N: sneak (half speed, seen at half range)\n" +
            "In combat - A: aim (+2)  C: cover  V: evasive\n" +
            "K: hunker down  O: overwatch (end your turn)\n" +
            "G: ready grenade, then click a tile\n" +
            "T: intimidate nearest enemy\n" +
            "Q: switch ammo type  Z: rest (safe zone/campfire)\n" +
            "B: shop (Safe Zone)\n" +
            "J: quests  M: the Zone  P: character\n" +
            "S: suppressive fire  H: run and gun (perks)\n" +
            "Mouse wheel: zoom in / out\n" +
            "Space: end turn\n" +
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
