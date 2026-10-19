// Batch 77 (Yan: "i want the targetting system to be similar to xcom, where you click the enemy bodypart to select the
// target during combat" and "add an 'examine' prompt similar to BG3"): clicking an enemy opens the targeting panel
// (bottom centre, above the action bar): a body figure you click - head, arms, torso, legs - each coloured and labelled
// with its hit chance from the real roll (Player.PreviewShot -> Weapon.PreviewHit), plus SHOOT for a plain centre-mass
// shot, the armor plates and the grenade pouch. Burst and melee have no aimed parts: only SHOOT. Tab (or EXAMINE)
// opens the examine panel on the right: their gear, resistances, state and, for a boss, perks (Examine.cs).
// Esc, right-click or a click on the map closes them.
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
        public static bool TargetingOpen => Instance != null && Instance._tgt != null && Instance._tgtPanel != null && Instance._tgtPanel.activeSelf;

        Mutant _tgt, _exTarget;
        GameObject _tgtPanel, _exPanel;
        TextMeshProUGUI _tTitle, _tInfo, _tDesc, _tShootT, _exTitle, _exBody;
        Image _tShoot;
        readonly List<(string part, Image img, TextMeshProUGUI label)> _parts = new List<(string, Image, TextMeshProUGUI)>();
        readonly List<(string part, Image img, TextMeshProUGUI label)> _extras = new List<(string, Image, TextMeshProUGUI)>();
        string _hoverPart;

        static readonly Dictionary<string, string> PartText = new Dictionary<string, string>
        {
            [""] = "Centre mass: a plain shot.",
            ["Head"] = "Head: one hit tier higher, dazes them; a kill shakes their squad.",
            ["Torso"] = "Body: they bleed.",
            ["Arms"] = "Arms: worse aim for them; a vital hit knocks the gun out of their hands.",
            ["Legs"] = "Legs: hobbles them; a vital hit knocks them down.",
            ["Armor"] = "Armor plates: breaks their armor down a step.",
            ["Pouch"] = "Grenade pouch: it goes off on them.",
        };

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

        void BuildTargeting(Transform root)
        {
            Instance = this;
            // ---- targeting panel
            var tp = Box(root, "TargetingPanel", new Vector2(0.5f, 0), new Vector2(0, 92), new Vector2(600, 290));
            _tgtPanel = tp.parent.gameObject;
            tp.parent.GetComponent<Image>().raycastTarget = true;
            var fig = Img(tp, "Figure", new Vector2(0, 1), new Vector2(12, -12), new Vector2(190, 266), C("0e0f11"));
            void Part(string part, Vector2 pos, Vector2 size)
            {
                var img = Img(fig.transform, "P_" + part, new Vector2(0, 1), pos, size, Empty);
                img.raycastTarget = true;
                var label = Txt(img.transform, "L", new Vector2(0, -(size.y - 20) / 2f), new Vector2(size.x, 22), "", true, 13, Cream, TextAlignmentOptions.Center);
                label.outlineWidth = 0.2f; label.outlineColor = new Color32(0, 0, 0, 255);
                var p = part;
                img.gameObject.AddComponent<Button>().onClick.AddListener(() => FireAt(p));
                _parts.Add((part, img, label));
            }
            Part("Head", new Vector2(70, -10), new Vector2(50, 46));
            Part("Arms", new Vector2(14, -62), new Vector2(34, 96));
            Part("Torso", new Vector2(52, -62), new Vector2(86, 96));
            Part("Arms", new Vector2(142, -62), new Vector2(34, 96));
            Part("Legs", new Vector2(54, -164), new Vector2(39, 96));
            Part("Legs", new Vector2(97, -164), new Vector2(39, 96));

            _tTitle = Txt(tp, "Title", new Vector2(216, -10), new Vector2(370, 24), "", true, 15, Cream);
            _tInfo = Txt(tp, "Info", new Vector2(216, -38), new Vector2(370, 20), "", false, 15, Dim);
            _tDesc = Txt(tp, "Desc", new Vector2(216, -64), new Vector2(370, 60), "", false, 15, Cream);
            _tDesc.enableWordWrapping = true;
            var shoot = Btn(tp, "Shoot", new Vector2(216, -130), new Vector2(370, 48), "", 16, () => FireAt(null));
            _tShoot = shoot.img; _tShootT = shoot.t;
            var ar = Btn(tp, "Armor", new Vector2(216, -186), new Vector2(182, 36), "", 12, () => FireAt("Armor"));
            _extras.Add(("Armor", ar.img, ar.t));
            var pouch = Btn(tp, "Pouch", new Vector2(404, -186), new Vector2(182, 36), "", 12, () => FireAt("Pouch"));
            _extras.Add(("Pouch", pouch.img, pouch.t));
            Btn(tp, "Examine", new Vector2(216, -232), new Vector2(182, 36), "EXAMINE  TAB", 12, () => OpenExamine(_tgt));
            Btn(tp, "Close", new Vector2(404, -232), new Vector2(182, 36), "CLOSE  ESC", 12, CloseTargeting);
            _tgtPanel.SetActive(false);

            // ---- examine panel
            var ex = Box(root, "ExaminePanel", new Vector2(1, 0.5f), new Vector2(-20, 60), new Vector2(430, 520));
            _exPanel = ex.parent.gameObject;
            ex.parent.GetComponent<Image>().raycastTarget = true;
            _exTitle = Txt(ex, "Title", new Vector2(14, -12), new Vector2(400, 26), "", true, 15, Cream);
            _exBody = Txt(ex, "Body", new Vector2(14, -48), new Vector2(400, 420), "", false, 16, Cream);
            _exBody.enableWordWrapping = true;
            Btn(ex, "Close", new Vector2(14, -472), new Vector2(160, 34), "CLOSE  ESC", 12, CloseExamine);
            _exPanel.SetActive(false);
        }

        // ------------------------------------------------------------------ open / close
        /// <summary>Opens the targeting panel on an enemy you can see. False without a HUD (tests): the caller just shoots.</summary>
        public static bool OpenTargeting(Mutant e)
        {
            if (Instance == null || Instance._tgtPanel == null || e == null) return false;
            Instance._tgt = e;
            Instance._tgtPanel.SetActive(true);
            Instance._hoverPart = null;
            return true;
        }

        public static void CloseTargeting() { if (Instance != null && Instance._tgtPanel != null) { Instance._tgtPanel.SetActive(false); Instance._tgt = null; } }

        public static void OpenExamine(Mutant e)
        {
            if (Instance == null || Instance._exPanel == null || e == null) return;
            Instance._exTarget = e;
            Instance._exPanel.SetActive(true);
        }

        public static void CloseExamine() { if (Instance != null && Instance._exPanel != null) { Instance._exPanel.SetActive(false); Instance._exTarget = null; } }

        /// <summary>Tab: examine whoever is under the mouse, being targeted, or the nearest enemy you can see.</summary>
        public static void ExamineHovered()
        {
            if (Instance == null) return;
            if (Instance._exPanel != null && Instance._exPanel.activeSelf) { CloseExamine(); return; }
            var p = Player.Current;
            Mutant e = Instance.Hovered() ?? Instance._tgt;
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

        void FireAt(string part)
        {
            var p = Player.Current; var e = _tgt;
            if (p == null || e == null) return;
            var (hit, ap, _, why) = p.PreviewShot(e, part);
            if (why != null) { GameLog.Write(why); return; }
            p.AttackPart(e, part);
            TurnLoop.CheckCombatEnd();
            // XCOM: the panel stays up while they're still standing and you can still afford a shot
            if (!e.IsAlive() || (e is HumanEnemy h && EnemyAI.IsOut(h)) || p.PreviewShot(e, null).why != null) CloseTargeting();
        }

        // ------------------------------------------------------------------ per frame
        void UpdateTargeting(Player p)
        {
            var k = Keyboard.current;
            if (k != null && k.escapeKey.wasPressedThisFrame) { CloseTargeting(); CloseExamine(); }
            if (k != null && k.tabKey.wasPressedThisFrame && !GameState.Current.ModalOpen) ExamineHovered();

            if (_exPanel != null && _exPanel.activeSelf)
            {
                var ex = _exTarget;
                if (ex == null || !ex.IsAlive() || !Vision.Shows(ex)) CloseExamine();
                else { _exTitle.text = Examine.Title(ex).ToUpperInvariant(); _exBody.text = Examine.Body(ex, p); }
            }

            if (!TargetingOpen) return;
            var e = _tgt;
            if (!e.IsAlive() || !Vision.Shows(e) || GameState.Current.CurrentTurn != Turn.Player || (e is HumanEnemy hh && EnemyAI.IsOut(hh))) { CloseTargeting(); return; }

            var w = p.EquippedWeapon;
            bool burst = w != null && CombatRules.CanBurst(w) && p.FireMode == "Burst";
            bool melee = w != null && CombatRules.IsMelee(w);
            bool partsOk = !burst && !melee;
            var (plain, apPlain, ambush, whyPlain) = p.PreviewShot(e, null);
            _tTitle.text = Examine.Title(e).ToUpperInvariant() + (ambush ? "  <color=#62b25a>AMBUSH</color>" : "");
            var cover = plain?.Cover ?? default;
            _tInfo.text = $"{CombatantUtil.Distance(p, e)} tiles   " + (cover.Hunkered ? "hunkered" : cover.AcBonus >= 2 ? "hard cover" : cover.AcBonus > 0 ? "light cover" : cover.Flanked ? "<color=#62b25a>flanked</color>" : "in the open")
                + (plain != null && plain.Range != 0 ? "   <color=#c8443a>beyond range</color>" : "") + $"   {p.Ap} AP left";

            var mouse = Mouse.current;
            Vector2 mp = mouse != null ? mouse.position.ReadValue() : new Vector2(-1, -1);
            Camera cam = _cv != null && _cv.renderMode != RenderMode.ScreenSpaceOverlay ? _cv.worldCamera : null;
            _hoverPart = null;
            foreach (var (part, img, label) in _parts.Concat(_extras))
            {
                bool pouchless = part == "Pouch" && !(e is HumanEnemy { Frags: > 0 });
                bool available = partsOk && !pouchless;
                var (hp, ap, _, why) = p.PreviewShot(e, part);
                bool ok = available && why == null && hp != null;
                float c = hp?.Chance ?? 0f;
                img.color = !available ? Lip : ok ? Color.Lerp(C("7a2a24"), C("3f7a3a"), c) : Color.Lerp(Lip, Edge, 0.5f);
                string name = part == "Pouch" ? "POUCH" : part == "Armor" ? "ARMOR" : "";
                label.text = !available ? (name.Length > 0 ? name + " -" : "") : $"{name}{(name.Length > 0 ? " " : "")}{Mathf.RoundToInt(c * 100)}%" + (name.Length > 0 ? $"  {ap} AP" : "");
                label.color = ok ? Cream : Dim;
                if (RectTransformUtility.RectangleContainsScreenPoint(img.rectTransform, mp, cam)) _hoverPart = part;
            }
            bool shootOk = whyPlain == null && plain != null;
            _tShoot.color = shootOk ? Yellow : Lip;
            _tShootT.text = (melee ? "STRIKE" : burst ? "BURST" : "SHOOT") + (plain != null ? $"   {Mathf.RoundToInt(plain.Chance * 100)}%   {apPlain} AP" : "");
            _tShootT.color = shootOk ? Cream : Dim;

            string hover = _hoverPart;
            if (hover != null)
            {
                var (hp, ap, _, why) = p.PreviewShot(e, hover);
                _tDesc.text = PartText[hover] + (hp != null ? $"\n{CombatRules.AimedPenalty(hover)} to hit, +1 AP: {Mathf.RoundToInt(hp.Chance * 100)}%" : "") + (why != null ? $"\n<color=#c8443a>{why}</color>" : "");
            }
            else
                _tDesc.text = (partsOk ? "Click a body part to aim for it, or SHOOT for a plain shot." : burst ? "A burst sprays: no aimed parts (X for single shots)." : "A blade has no aimed parts.")
                    + (plain != null ? "\n" + string.Join("  ", plain.Parts.Where(x => x.v != 0).Select(x => $"{x.v:+0;-0} {x.what}")) + $"  vs AC {plain.TargetAc}" : "")
                    + (whyPlain != null ? $"\n<color=#c8443a>{whyPlain}</color>" : "");
        }
    }
}
