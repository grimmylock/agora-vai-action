// 2026-11-18 (Yan: "something similar to Resident Evil 4"): the attaché case look for the pack screen (HudView.Pack.cs): a dimmed room behind, a dark-leather case with a brass rim and corner brackets, the tab row on top (CASE / CHARACTER / QUESTS / MAP,
// as RE4's menu tabs; the others close the case and open that screen), a SORT button (repack everything, biggest first), an item description under the grid's right-hand column, and a small context menu on right-click (USE / EQUIP / FIT / DROP ...)
// instead of acting at once. All of it hangs off what BuildPack already builds; nothing in the grid logic changed. Unity-only drawing, never run when written.
using System.Collections.Generic;
using System.Linq;
using GameData;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameCore
{
    public partial class HudView
    {
        GameObject _packDim, _ctx; RectTransform _ctxRect;
        static readonly Color Brass = new Color(0.55f, 0.43f, 0.24f), Leather = new Color(0.075f, 0.062f, 0.052f, 0.97f), BrassDim = new Color(0.32f, 0.26f, 0.16f);

        /// <summary>A dim over the world while the case is open (clicks do not reach what is under it); made once, destroyed with the case.</summary>
        void EnsureDim()
        {
            if (_packDim != null) return;
            var im = Img(_root.transform, "PackDim", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.62f));
            var r = im.rectTransform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.sizeDelta = Vector2.zero; im.raycastTarget = true;
            _packDim = im.gameObject;
        }
        void KillDim() { if (_packDim != null) Destroy(_packDim); _packDim = null; CloseContext(); }

        /// <summary>Called at the end of BuildPack: re-colours the box into a case, adds the brackets, the tabs and the SORT button, and gives the description room.</summary>
        void DecorateCase(Player p, float W, float H, float bw)
        {
            var edge = _pack.parent; var eim = edge.GetComponent<Image>(); if (eim != null) eim.color = Brass;
            var fim = _pack.GetComponent<Image>(); if (fim != null) fim.color = Leather;
            // brass corner brackets
            foreach (var (ax, ay) in new[] { (0, 0), (1, 0), (0, 1), (1, 1) })
            {
                float x = ax == 0 ? 6 : W - 6 - 26, y = ay == 0 ? -6 : -(H - 6 - 3);
                Img(_pack, "BrH", new Vector2(0, 1), new Vector2(x, ay == 0 ? -6 : -(H - 9)), new Vector2(26, 3), Brass);
                Img(_pack, "BrV", new Vector2(0, 1), new Vector2(ax == 0 ? 6 : W - 9, ay == 0 ? -6 : -(H - 32)), new Vector2(3, 26), Brass);
            }
            // tabs above the box
            var tabs = new (string label, System.Action go)[] {
                ("CASE", null),
                ("CHARACTER", () => { CloseCaseFor(); CharacterUI.ShowSheet(); }),
                ("QUESTS", () => { CloseCaseFor(); QuestUI.Toggle(); }),
                ("MAP", () => { CloseCaseFor(); WorldUI.Toggle(); }) };
            float tx = 0f;
            foreach (var (label, go) in tabs)
            {
                bool active = go == null; float w = 36f + label.Length * 13f;
                var tab = Img(edge, "Tab", new Vector2(0, 1), new Vector2(tx, 30), new Vector2(w, 30), active ? Brass : BrassDim);
                var inner = Img(tab.transform, "In", new Vector2(0, 1), new Vector2(2, -2), new Vector2(w - 4, 28), active ? new Color(0.13f, 0.105f, 0.075f, 1f) : new Color(0.06f, 0.05f, 0.04f, 1f));
                var t = Txt(inner.transform, "T", new Vector2(0, -6), new Vector2(w - 4, 20), Loc.Text(label), true, 13, active ? Cream : Dim, TextAlignmentOptions.Center);
                if (go != null) { tab.raycastTarget = true; var b = tab.gameObject.AddComponent<Button>(); var action = go; b.onClick.AddListener(() => { action(); Sfx.Play("ui_click"); }); }
                tx += w + 4f;
            }
            var sort = Btn(_pack, "Sort", new Vector2(bw - 96, -12), new Vector2(100, 28), Loc.Text("SORT"), 13, () => { Pack.Refit(p); _packStamp = null; Sfx.Play("ui_click"); });
            if (_packInfo != null) { var ir = _packInfo.rectTransform; ir.sizeDelta = new Vector2(400f, Mathf.Max(120f, H - 346f - 130f)); _packInfo.enableWordWrapping = true; }
        }

        void CloseCaseFor() { if (_beside) { CorpseLootUI.CloseIfOpen(); return; } if (PackOpen) TogglePack(); }

        // ---- descriptions ----
        static string CategoryLabel(string c) => Loc.T(CategoryLabelEn(c));
        static string CategoryLabelEn(string c) => c == "sidearm" ? "sidearm" : c == "smg" ? "submachine gun" : c == "assault" ? "assault rifle" : c == "shotgun" ? "shotgun" : c == "sniper" ? "sniper rifle" : c == "heavy" ? "heavy weapon" : c == "melee" ? "melee" : c ?? "";
        public static string Describe(InventoryItem it)
        {
            try
            {
                if (it == null) return "";
                switch (it.Type)
                {
                    case "consumable": return ConsumableDatabase.All.TryGetValue((it.Id ?? "").Split('~')[0], out var cd) ? cd.Description ?? "" : "";
                    case "weapon":
                        if (it.Category != null && it.Key != null && WeaponDatabase.Categories.TryGetValue(it.Category, out var cat) && cat.TryGetValue(it.Key, out var wd))
                            return CategoryLabel(it.Category) + " · tier " + wd.Tier + (wd.MagCapacity.HasValue ? "\n" + (wd.AmmoType ?? "") + " · magazine " + wd.MagCapacity : wd.AmmoType != null ? "\n" + wd.AmmoType : "");
                        return CategoryLabel(it.Category);
                    case "armor":
                        var ad = ArmorDatabase.Tiers.Values.SelectMany(d => d.Values).FirstOrDefault(a => a.Name == it.Name);
                        return ad == null ? "" : $"armor {ad.PhysDR * 100f:0}% · anomaly {ad.AnomDR * 100f:0}% · belt {ad.BeltSlots} · mods {ad.ModSlots}";
                    case "backpack": return "A bigger bag: more room in the case.";
                    case "artifact": return "Wear it on the belt: it works while it hangs there.";
                    case "attachment": return "Drag it onto the weapon's slot to fit it.";
                    case "detector": return "Shows the anomalies near you.";
                }
            }
            catch { }
            return "";
        }

        // ---- the context menu ----
        void OpenContext(Player p, InventoryItem it, Vector2 screen)
        {
            CloseContext();
            var acts = new List<(string label, System.Action go)>();
            switch (it.Type)
            {
                case "weapon": acts.Add(("EQUIP", () => p.EquipWeaponFromInventory(it.Id))); acts.Add(("SECOND", () => p.EquipSecondaryFromInventory(it.Id))); break;
                case "armor": acts.Add(("WEAR", () => p.EquipArmorFromInventory(it.Id))); break;
                case "consumable": acts.Add(("USE", () => p.UseConsumable(it.Id))); break;
                case "detector": acts.Add(("EQUIP", () => p.EquipDetectorFromInventory(it.Id))); break;
                case "artifact": acts.Add(("BELT", () => p.EquipArtifactFromInventory(it.Id))); break;
                case "armorMod": acts.Add(("FIT", () => p.FitArmorMod(it.Id))); break;
                case "attachment": acts.Add(("FIT", () => { if (!Optics.FitFromInventory(p, it, p.EquippedWeapon)) Optics.FitFromInventory(p, it, p.SecondaryWeapon); })); break;
                case "backpack": acts.Add(("EQUIP", () => p.EquipBackpackFromInventory(it.Id))); break;
            }
            acts.Add(("DROP", () => p.DropItem(it.Id, System.Math.Max(1, it.Qty))));
            float w = 170f, rowH = 34f, h = 12f + acts.Count * rowH;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)_root.transform, screen, null, out var lp);
            var box = Box(_root.transform, "Ctx", new Vector2(0.5f, 0.5f), lp + new Vector2(w / 2f, -h / 2f), new Vector2(w, h));
            _ctxRect = (RectTransform)box.parent; _ctx = box.parent.gameObject;
            var edge = _ctx.GetComponent<Image>(); if (edge != null) { edge.color = Brass; edge.raycastTarget = true; }
            var fill = box.GetComponent<Image>(); if (fill != null) fill.color = Leather;
            for (int i = 0; i < acts.Count; i++)
            {
                var (label, go) = acts[i]; var action = go;
                Btn(box, "Act", new Vector2(6, -6 - i * rowH), new Vector2(w - 12, rowH - 4), Loc.Text(label), 14, () => { action(); CloseContext(); _packStamp = null; });
            }
        }
        void CloseContext() { if (_ctx != null) Destroy(_ctx); _ctx = null; _ctxRect = null; }
        /// <summary>While the menu is open: a click outside it closes it, and nothing else in the case reacts. True = the mouse was spent.</summary>
        bool HandleContext(Mouse m)
        {
            if (_ctx == null) return false;
            var screen = m.position.ReadValue();
            bool over = _ctxRect != null && RectTransformUtility.RectangleContainsScreenPoint(_ctxRect, screen, null);
            if ((m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame) && !over) CloseContext();
            return true;
        }
    }
}
