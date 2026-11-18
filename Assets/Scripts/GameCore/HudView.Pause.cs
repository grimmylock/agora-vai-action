// 2026-11-18 (Yan: "something similar to Resident Evil 4"): the pause menu. RE4's is a quiet column of words down the left over the dimmed game, one lit; this is that: a dark band on the left with PAUSED at the top and RESUME / CASE /
// CHARACTER / QUESTS / MAP / SAVE / LOAD / HUD / MAIN MENU under it. Up / Down and Enter, or the mouse; Esc resumes (PlayerController already does that). Replaces the big word in the middle of the screen. Unity-only, never run when written.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameCore
{
    public partial class HudView
    {
        GameObject _pauseUI; int _pauseSel; readonly List<(string label, System.Action go)> _pauseItems = new List<(string, System.Action)>(); readonly List<(RectTransform rect, TextMeshProUGUI t)> _pauseViews = new List<(RectTransform, TextMeshProUGUI)>();

        static void Unpause() { Rt.Paused = false; Time.timeScale = Rt.SlowMo ? Rt.SlowScale : 1f; }

        /// <summary>Every frame: show the menu while the game is paused (and no panel is up), hide it otherwise. True while it is showing (the old middle-of-the-screen word then stays hidden).</summary>
        bool UpdatePauseMenu(Player p)
        {
            bool want = _re4Ok && Rt.On && Rt.Paused && !GameState.Current.ModalOpen;
            if (!want) { if (_pauseUI != null) { Destroy(_pauseUI); _pauseUI = null; _pauseViews.Clear(); } return false; }
            if (_pauseUI == null) { try { BuildPauseMenu(); } catch (System.Exception e) { Debug.LogException(e); if (_pauseUI != null) Destroy(_pauseUI); _pauseUI = null; return false; } }
            var k = Keyboard.current; var m = Mouse.current;
            if (k != null)
            {
                if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) { _pauseSel = (_pauseSel + 1) % _pauseItems.Count; Sfx.Play("ui_click"); }
                if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) { _pauseSel = (_pauseSel + _pauseItems.Count - 1) % _pauseItems.Count; Sfx.Play("ui_click"); }
                if (k.enterKey.wasPressedThisFrame) { Sfx.Play("ui_click"); _pauseItems[_pauseSel].go(); return true; }
            }
            if (m != null) { var at = m.position.ReadValue(); for (int i = 0; i < _pauseViews.Count; i++) if (_pauseViews[i].rect != null && RectTransformUtility.RectangleContainsScreenPoint(_pauseViews[i].rect, at, null)) _pauseSel = i; }
            for (int i = 0; i < _pauseViews.Count; i++)
            {
                var t = _pauseViews[i].t; if (t == null) continue; bool on = i == _pauseSel;
                string want2 = Loc.Text(_pauseItems[i].label); string shown = (on ? "> " : "  ") + want2; if (t.text != shown) t.text = shown; t.color = on ? Yellow : Dim;
            }
            return true;
        }

        void BuildPauseMenu()
        {
            _pauseItems.Clear(); _pauseViews.Clear(); _pauseSel = 0;
            _pauseItems.Add(("RESUME", () => Unpause()));
            _pauseItems.Add(("CASE", () => { Unpause(); if (!PackOpen) TogglePack(); }));
            _pauseItems.Add(("CHARACTER", () => { Unpause(); CharacterUI.ShowSheet(); }));
            _pauseItems.Add(("QUESTS", () => { Unpause(); QuestUI.Toggle(); }));
            _pauseItems.Add(("MAP", () => { Unpause(); WorldUI.Toggle(); }));
            _pauseItems.Add(("SAVE", () => SaveSystem.Save()));
            if (SaveSystem.HasSave()) _pauseItems.Add(("LOAD", () => { Unpause(); SaveSystem.Load(); }));
            _pauseItems.Add(("HUD STYLE", () => ToggleRe4()));
            _pauseItems.Add(("MAIN MENU", () => { SaveSystem.Save(); Unpause(); MainMenuUI.Show(); }));

            _pauseUI = Rect(_root.transform, "PauseMenu", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject;
            var rr = (RectTransform)_pauseUI.transform; rr.anchorMin = Vector2.zero; rr.anchorMax = Vector2.one; rr.sizeDelta = Vector2.zero; rr.SetAsLastSibling();
            var band = Img(rr, "Band", new Vector2(0, 0.5f), Vector2.zero, new Vector2(620, 0), new Color(0.02f, 0.02f, 0.02f, 0.82f));
            var br = band.rectTransform; br.anchorMin = new Vector2(0, 0); br.anchorMax = new Vector2(0, 1); br.pivot = new Vector2(0, 0.5f); br.sizeDelta = new Vector2(620, 0);
            var line = Img(rr, "Line", new Vector2(0, 0.5f), Vector2.zero, new Vector2(2, 0), new Color(0.55f, 0.43f, 0.24f, 0.9f));
            var lr = line.rectTransform; lr.anchorMin = new Vector2(0, 0); lr.anchorMax = new Vector2(0, 1); lr.pivot = new Vector2(0, 0.5f); lr.anchoredPosition = new Vector2(620, 0); lr.sizeDelta = new Vector2(2, 0);
            var title = Txt(rr, "Title", Vector2.zero, new Vector2(520, 60), Loc.Text("PAUSED"), true, 44, Cream); Pin(title.rectTransform, new Vector2(0, 1), new Vector2(72, -80)); title.rectTransform.pivot = new Vector2(0, 1);
            Img(rr, "Rule", new Vector2(0, 1), new Vector2(72, -150), new Vector2(150, 2), Yellow);
            for (int i = 0; i < _pauseItems.Count; i++)
            {
                int idx = i;
                var row = Img(rr, "Item", new Vector2(0, 1), new Vector2(64, -210 - i * 64), new Vector2(480, 56), Color.clear); row.raycastTarget = true;
                var t = Txt(row.transform, "T", new Vector2(8, -12), new Vector2(460, 36), "", true, 26, Dim);
                var b = row.gameObject.AddComponent<Button>(); b.onClick.AddListener(() => { Sfx.Play("ui_click"); _pauseItems[idx].go(); });
                _pauseViews.Add((row.rectTransform, t));
            }
            Txt(rr, "Hint", Vector2.zero, new Vector2(560, 20), Loc.Text("UP / DOWN   ENTER   ESC RESUME"), true, 11, Dim); var hr = _pauseUI.transform.Find("Hint") as RectTransform; if (hr != null) { hr.anchorMin = hr.anchorMax = hr.pivot = new Vector2(0, 0); hr.anchoredPosition = new Vector2(72, 48); }
        }
    }
}
