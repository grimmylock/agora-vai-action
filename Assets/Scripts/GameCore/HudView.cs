// Batch 30: the in-game HUD from the approved mockup (outputs/ui_hud_mock.png), replacing the
// debug text panels: zone card (top-left), turn banner + hovered-enemy card (top-centre), player
// card with HP / AC / status chips / AP+MP pips / food-water-rest (bottom-left), weapon card with
// magazine, ammo, belt and artifacts (bottom-right), end-turn button, key strip. The combat log
// stays RuntimeGameLog's, restyled. The old status / equipment / inventory / controls panels are
// hidden: I toggles the inventory (gear), F1 the controls list. 1-4 use the first four
// consumables shown on the belt. Pixel fonts: Resources/Fonts (Silkscreen + Pixelify Sans, OFL).
// Self-building, added by GameBootstrap. Layout in 1920x1080 reference pixels (CanvasScaler).
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameCore
{
    public partial class HudView : MonoBehaviour
    {
        // palette (mockup)
        internal static readonly Color Fill = C("15171a", .94f), Edge = C("3a3e41"), Lip = C("2b2f33");
        internal static readonly Color Cream = C("e6dcc0"), Dim = C("7d7a70"), Yellow = C("e8c547"), Red = C("c8443a"),
            Cyan = C("4fb6c9"), Purple = C("9d7bd8"), Green = C("62b25a"), Empty = C("2a2c2e");

        static Color C(string h, float a = 1f) { ColorUtility.TryParseHtmlString("#" + h, out var c); c.a = a; return c; }

        static TMP_FontAsset _title, _body;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCaches() { _title = _body = null; }

        /// <summary>Batch 123: the HUD's pixel fonts for the other panels (dialogue, main menu).</summary>
        internal static TMP_FontAsset TitleFont => _title ?? (_title = Font("Silkscreen-Regular"));
        internal static TMP_FontAsset BodyFont => _body ?? (_body = Font("PixelifySans-Regular"));

        static TMP_FontAsset Font(string name)
        {
            var f = Resources.Load<Font>("Fonts/" + name);
            return f != null ? TMP_FontAsset.CreateFontAsset(f) : null;
        }

        Transform _canvas;
        GameObject _root, _target, _endTurn;
        TextMeshProUGUI _tgtHint;
        TextMeshProUGUI _zone, _zoneSub, _turn, _targetName, _targetState, _hpNum, _ac, _wName, _wMag, _wCap, _wAmmo, _hint;
        readonly List<Image> _hpSeg = new List<Image>(), _tgtSeg = new List<Image>(), _ap = new List<Image>(), _mp = new List<Image>();
        TextMeshProUGUI _apL, _mpL;   // hidden in real time
        readonly List<(GameObject go, TextMeshProUGUI t, Image edge)> _chips = new List<(GameObject, TextMeshProUGUI, Image)>();
        readonly List<(TextMeshProUGUI t, Image edge)> _wChips = new List<(TextMeshProUGUI, Image)>();
        readonly List<TextMeshProUGUI> _belt = new List<TextMeshProUGUI>(), _arts = new List<TextMeshProUGUI>();
        Image _portrait, _wImage, _food, _water, _rest;
        string _weaponKey = "?";
        readonly List<string> _beltIds = new List<string>();

        void Start()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;
            _canvas = canvas.transform;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
            if (_title == null) _title = Font("Silkscreen-Regular");
            if (_body == null) _body = Font("PixelifySans-Regular");
            Build();
            Restyle();
        }

        // ------------------------------------------------------------------ building blocks
        static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = anchor;
            r.anchoredPosition = pos; r.sizeDelta = size;
            return r;
        }

        static Image Img(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color c)
        {
            var img = Rect(parent, name, anchor, pos, size).gameObject.AddComponent<Image>();
            img.color = c; img.raycastTarget = false;
            return img;
        }

        /// <summary>A bordered dark panel; returns the inner area (top-left anchored children).</summary>
        static RectTransform Box(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var edge = Img(parent, name, anchor, pos, size, Edge);
            var fill = Img(edge.transform, "Fill", new Vector2(0, 1), new Vector2(2, -2), size - new Vector2(4, 4), Fill);
            Img(fill.transform, "Lip", new Vector2(0, 1), Vector2.zero, new Vector2(size.x - 4, 1), Lip);
            return fill.rectTransform;
        }

        TextMeshProUGUI Txt(Transform parent, string name, Vector2 pos, Vector2 size, string s, bool title, float px, Color c,
            TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var t = Rect(parent, name, new Vector2(0, 1), pos, size).gameObject.AddComponent<TextMeshProUGUI>();
            var f = title ? _title : _body;
            if (f != null) t.font = f;
            t.text = s; t.fontSize = px; t.color = c; t.alignment = align; t.raycastTarget = false;
            t.enableWordWrapping = false;
            if (title) t.characterSpacing = 6;
            return t;
        }

        static List<Image> Segments(Transform parent, Vector2 pos, int n, float w, float h, float gap)
        {
            var list = new List<Image>();
            for (int i = 0; i < n; i++) list.Add(Img(parent, "Seg" + i, new Vector2(0, 1), pos + new Vector2(i * (w + gap), 0), new Vector2(w, h), Empty));
            return list;
        }

        (GameObject, TextMeshProUGUI, Image) Chip(Transform parent, Vector2 pos, float w)
        {
            var edge = Img(parent, "Chip", new Vector2(0, 1), pos, new Vector2(w, 24), Dim);
            Img(edge.transform, "In", new Vector2(0, 1), new Vector2(2, -2), new Vector2(w - 4, 20), Fill);
            var t = Txt(edge.transform, "T", new Vector2(0, -4), new Vector2(w, 22), "", true, 12, Dim, TextAlignmentOptions.Center);
            t.characterSpacing = 3;
            return (edge.gameObject, t, edge);
        }

        Image Meter(Transform parent, Vector2 pos, string label, Color c)
        {
            Txt(parent, "L", pos + new Vector2(0, 2), new Vector2(60, 20), label, true, 12, c);
            Img(parent, "Back", new Vector2(0, 1), pos + new Vector2(62, -2), new Vector2(92, 14), Empty);
            var bar = Img(parent, "Bar", new Vector2(0, 1), pos + new Vector2(62, -2), new Vector2(92, 14), c);
            return bar;
        }

        void Build()
        {
            _root = Rect(_canvas, "HUD", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject;
            var rt = _root.GetComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            var root = _root.transform;

            // zone card
            var z = Box(root, "ZoneCard", new Vector2(0, 1), new Vector2(20, -20), new Vector2(420, 78));
            _zone = Txt(z, "Name", new Vector2(16, -10), new Vector2(400, 30), "", true, 24, Cream);
            _zoneSub = Txt(z, "Sub", new Vector2(16, -46), new Vector2(400, 20), "", true, 12, Dim);
            _zoneSub.characterSpacing = 2;

            // turn banner + target card
            var tb = Box(root, "TurnBanner", new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(320, 42));
            _turn = Txt(tb, "Turn", new Vector2(0, -9), new Vector2(316, 24), "", true, 16, Yellow, TextAlignmentOptions.Center);
            var tc = Box(root, "TargetCard", new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(390, 58));
            _target = tc.parent.gameObject;
            _targetName = Txt(tc, "Name", new Vector2(14, -8), new Vector2(260, 22), "", true, 15, Cream);
            _targetState = Txt(tc, "State", new Vector2(250, -8), new Vector2(122, 22), "", true, 15, Red, TextAlignmentOptions.TopRight);
            _tgtSeg.AddRange(Segments(tc, new Vector2(14, -34), 10, 33.4f, 12, 2));

            // player card
            var pc = Box(root, "PlayerCard", new Vector2(0, 0), new Vector2(20, 20), new Vector2(520, 180));
            var pf = Img(pc, "PortraitFrame", new Vector2(0, 1), new Vector2(10, -10), new Vector2(104, 104), Edge);
            Img(pf.transform, "Back", new Vector2(0, 1), new Vector2(2, -2), new Vector2(100, 100), C("0e0f11"));
            _portrait = Img(pf.transform, "Portrait", new Vector2(0, 1), new Vector2(4, -4), new Vector2(96, 96), Color.white);
            _portrait.preserveAspect = true;
            Txt(pc, "HPL", new Vector2(128, -12), new Vector2(40, 20), "HP", true, 14, Dim);
            _hpSeg.AddRange(Segments(pc, new Vector2(168, -12), 10, 26, 18, 2));
            _hpNum = Txt(pc, "HPN", new Vector2(452, -10), new Vector2(60, 24), "", true, 16, Cream);
            _ac = Txt(pc, "AC", new Vector2(128, -46), new Vector2(120, 22), "", true, 14, Cream);
            for (int i = 0; i < 3; i++) { var ch = Chip(pc, new Vector2(232 + i * 96, -44), 92); _chips.Add(ch); ch.Item1.SetActive(false); }
            _apL = Txt(pc, "APL", new Vector2(128, -82), new Vector2(40, 20), "AP", true, 14, Dim);
            _ap.AddRange(Segments(pc, new Vector2(168, -84), 6, 16, 16, 8));
            _mpL = Txt(pc, "MPL", new Vector2(320, -82), new Vector2(40, 20), "MP", true, 14, Dim);
            _mp.AddRange(Segments(pc, new Vector2(360, -84), 6, 16, 16, 8));
            _food = Meter(pc, new Vector2(12, -132), "FOOD", Yellow);
            _water = Meter(pc, new Vector2(184, -132), "WATER", Cyan);
            _rest = Meter(pc, new Vector2(356, -132), "REST", Purple);

            // weapon card
            var wc = Box(root, "WeaponCard", new Vector2(1, 0), new Vector2(-20, 20), new Vector2(560, 180));
            var wf = Img(wc, "WeaponFrame", new Vector2(0, 1), new Vector2(10, -10), new Vector2(196, 104), C("0e0f11"));
            _wImage = Img(wf.transform, "Weapon", new Vector2(0, 1), new Vector2(6, -6), new Vector2(184, 92), Color.white);
            _wImage.preserveAspect = true;
            _wName = Txt(wc, "Name", new Vector2(220, -10), new Vector2(330, 28), "", true, 20, Cream);
            for (int i = 0; i < 2; i++) { var ch = Chip(wc, new Vector2(220 + i * 100, -42), 96); _wChips.Add((ch.Item2, ch.Item3)); }
            _wMag = Txt(wc, "Mag", new Vector2(220, -72), new Vector2(90, 44), "", true, 38, Cream);
            _wCap = Txt(wc, "Cap", new Vector2(306, -84), new Vector2(110, 30), "", true, 18, Dim);
            _wAmmo = Txt(wc, "Ammo", new Vector2(410, -78), new Vector2(140, 44), "", true, 12, Dim, TextAlignmentOptions.TopRight);
            for (int i = 0; i < 4; i++)
            {
                var s = Img(wc, "Belt" + i, new Vector2(0, 1), new Vector2(10 + i * 62, -122), new Vector2(56, 48), Edge);
                Img(s.transform, "In", new Vector2(0, 1), new Vector2(2, -2), new Vector2(52, 44), C("0e0f11"));
                Txt(s.transform, "K", new Vector2(4, -2), new Vector2(14, 14), (i + 1).ToString(), true, 10, Dim);
                _belt.Add(Txt(s.transform, "T", new Vector2(2, -16), new Vector2(52, 30), "", false, 13, Cream, TextAlignmentOptions.Center));
            }
            Txt(wc, "BeltL", new Vector2(264, -138), new Vector2(60, 20), "BELT", true, 12, Dim);
            for (int i = 0; i < 4; i++)
            {
                var s = Img(wc, "Art" + i, new Vector2(0, 1), new Vector2(320 + i * 56, -122), new Vector2(50, 48), Edge);
                Img(s.transform, "In", new Vector2(0, 1), new Vector2(2, -2), new Vector2(46, 44), C("0e0f11"));
                _arts.Add(Txt(s.transform, "T", new Vector2(2, -6), new Vector2(46, 36), "", false, 11, Cyan, TextAlignmentOptions.Center));
            }

            // end turn + key strip
            var et = Img(root, "EndTurn", new Vector2(1, 0), new Vector2(-20, 210), new Vector2(240, 44), Edge);
            Img(et.transform, "In", new Vector2(0, 1), new Vector2(2, -2), new Vector2(236, 40), Fill);
            Txt(et.transform, "T", new Vector2(0, -11), new Vector2(240, 24), "SPACE  END TURN", true, 15, Yellow, TextAlignmentOptions.Center);
            et.raycastTarget = true;
            et.gameObject.AddComponent<Button>().onClick.AddListener(() => { if (GameState.Current.CurrentTurn == Turn.Player && !GameState.Current.ModalOpen) TurnLoop.PlayerEndTurn(); });
            _endTurn = et.gameObject;
            _hint = Txt(root, "Keys", new Vector2(0, 0), new Vector2(900, 20), "I GEAR    J QUESTS    P CHARACTER    M ZONE    F1 CONTROLS", true, 12, Dim, TextAlignmentOptions.Center);
            var hr = _hint.rectTransform; hr.anchorMin = hr.anchorMax = hr.pivot = new Vector2(0.5f, 0); hr.anchoredPosition = new Vector2(0, 8);
            BuildActionBar(root);   // Batch 72
            BuildTargeting(root);   // Batch 77
            _tgtHint = Txt(tc, "Hint", new Vector2(14, -58), new Vector2(362, 18), "CLICK TO TARGET    TAB EXAMINE", true, 10, Dim, TextAlignmentOptions.TopRight);
        }

        /// <summary>Old debug panels off; the log moves above the player card without a background.</summary>
        void Restyle()
        {
            foreach (var n in new[] { "StatusPanel", "EquipmentPanel", "InventoryPanel", "ControlsPanel" })
            {
                var go = GameObject.Find(n);
                if (go != null) { _old[n] = go; go.SetActive(false); }
            }
            var log = GameObject.Find("CombatLogBackground");
            if (log != null)
            {
                var r = log.GetComponent<RectTransform>();
                r.anchorMin = r.anchorMax = r.pivot = Vector2.zero;
                r.anchoredPosition = new Vector2(24, 212); r.sizeDelta = new Vector2(760, 150);
                var img = log.GetComponent<Image>(); if (img != null) img.color = new Color(0, 0, 0, 0);
                var t = log.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null)
                {
                    if (_body != null) t.font = _body;
                    t.fontSize = 18; t.color = Cream;
                    t.outlineWidth = 0.25f; t.outlineColor = new Color32(0, 0, 0, 255);
                }
            }
        }

        static readonly Dictionary<string, GameObject> _old = new Dictionary<string, GameObject>();   // the hidden debug panels (one HUD)
        /// <summary>Yan: "after opening and closing the inventory my character froze" - ESC paused the game under the open inventory.
        /// Now ESC closes the inventory (and the other old panels) first; true if it closed one.</summary>
        public static bool CloseOldPanels()
        {
            bool any = false;
            foreach (var go in _old.Values) if (go != null && go.activeSelf) { go.SetActive(false); any = true; }
            return any;
        }
        TextMeshProUGUI _paused;
        void Toggle(string name)
        {
            if (_old.TryGetValue(name, out var go) && go != null) go.SetActive(!go.activeSelf);
        }

        // ------------------------------------------------------------------ per frame
        void Update()
        {
            if (_root == null) return;
            var p = Player.Current;
            bool show = p != null && GameState.Current.Mode == GameMode.Tactical;
            _root.SetActive(show);
            if (!show) return;

            var k = Keyboard.current;
            UpdatePack();                                                             // Pack 1: I - the pack screen
            if (k != null && !GameState.Current.ModalOpen)
            {
                // (I opens the pack screen now - HudView.Pack.cs; the old text panels stay hidden)
                if (k.f1Key.wasPressedThisFrame) Toggle("ControlsPanel");
                var digits = new[] { k.digit1Key, k.digit2Key, k.digit3Key, k.digit4Key };
                for (int i = 0; i < 4; i++)
                    if (digits[i] != null && digits[i].wasPressedThisFrame && i < _beltIds.Count) p.UseConsumable(_beltIds[i]);
            }

            var m = MacroState.Current;
            var zone = GameData.MacroMapDatabase.Find(m.CurrentZoneId);
            _zone.text = (zone?.Name ?? m.CurrentZoneId ?? "").ToUpperInvariant();
            _zoneSub.text = $"DAY {m.Day}  {Atmosphere.Clock()}  {(ZoneLayoutRecipes.IsUpstairs(m.CurrentZoneId) ? "INDOORS" : ZoneLayoutRecipes.IsInterior(m.CurrentZoneId) ? "UNDERGROUND" : Atmosphere.Label(Atmosphere.Current))}   TIER {zone?.Tier ?? 1}" + (zone != null && zone.IsSafeZone ? "   SAFE ZONE" : "");

            bool exploring = PlayerController.Exploring();
            string turn = GameState.Current.CurrentTurn == Turn.Player ? (exploring ? "<color=#62b25a>EXPLORING</color>" : "YOUR TURN") : "<color=#c8443a>ENEMY TURN</color>";
            if (m.SurgeRounds > 0) turn += $"   <color=#c8443a>SURGE IN {m.SurgeRounds}</color>";
            if (Rt.On)   // real-time fork: no turns - fighting or not, and your stamina
                turn = Rt.Paused ? "<color=#e0c050>PAUSED - ESC</color>" : (EncounterState.InCombat ? "<color=#c8443a>FIGHT</color>" : "<color=#62b25a>EXPLORING</color>")
                    + $"   STAMINA {Mathf.RoundToInt(Rt.Me.Stamina)}   FOCUS {Mathf.FloorToInt(Rt.Me.Focus)}" + (Rt.SlowMo ? "   <color=#62c0e0>BULLET TIME</color>" : "") + (m.SurgeRounds > 0 ? $"   <color=#c8443a>SURGE IN {m.SurgeRounds}</color>" : "");
            if (Survival.On) turn = $"<color=#e0c050>{Survival.Status()}</color>   " + turn;   // Survival: the wave line
            _turn.text = turn;
            // a paused game says so in the middle of the screen, not only in the top line
            if (_paused == null)
            {
                _paused = Txt(_root.transform, "Paused", Vector2.zero, new Vector2(900f, 120f), "PAUSED\n<size=50%>ESC to resume</size>", true, 56, Cream, TextAlignmentOptions.Center);
                Pin(_paused.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f));
                _paused.outlineWidth = 0.25f; _paused.outlineColor = new Color32(0, 0, 0, 255);
            }
            _paused.gameObject.SetActive(Rt.On && Rt.Paused && !GameState.Current.ModalOpen);
            _endTurn.SetActive(!Rt.On && !exploring && GameState.Current.CurrentTurn == Turn.Player);

            UpdateTarget(p);
            UpdateActionBar(p);   // Batch 72
            UpdatePrompts(p);     // Batch 123
            UpdateBarks();        // Batch 129
            UpdateNumbers();      // the testing ground's damage numbers
            UpdateBoss();         // Boss 1: the name card, his health bar
            UpdateTargeting(p);   // Batch 77

            // player card
            _portrait.sprite = UnitSprites.For(p);
            FillSegs(_hpSeg, p.Hp / (float)Mathf.Max(1, p.MaxHp), Red);
            _hpNum.text = p.Hp.ToString();
            _ac.text = Rt.On ? Body.Summary(p) : $"AC {p.Ac}";   // RT batch 2: each part's health
            var st = new List<(string, Color)>();
            if (Rt.On)
            {
                var bs = Body.Of(p);
                if (bs.Down) st.Add(($"DOWN {Mathf.CeilToInt(bs.BleedOut)}s", Red));
                foreach (var f in bs.Fractured) st.Add(((Body.Name(f) + (bs.Broken.Contains(f) ? " broken" : " fractured")).ToUpperInvariant(), Red));
                if (Body.Concussed(p)) st.Add(("CONCUSSED", Yellow));
                if (Rt.Me.UsingId != null) st.Add(($"USING {Mathf.Max(0f, Rt.Me.UseDone - Time.time):0.0}s", Cyan));
                if (Rt.Me.Reviving > 0f) st.Add(($"REVIVING {Rt.ReviveSeconds - Rt.Me.Reviving:0.0}s", Cyan));
            }
            if (p.StatusEffects.Bleeding) st.Add(("BLEEDING", Red));
            if (p.StatusEffects.ToxicBleed) st.Add(("TOXIC", Green));
            if (p.StatusEffects.Irradiated) st.Add(("RAD", Green));
            if (p.StatusEffects.Prone) st.Add(("PRONE", Yellow));
            if (p.StatusEffects.Suppressed) st.Add(("PINNED", Yellow));
            if (p.StatusEffects.Wound != null) st.Add(("WOUND", Red));
            if (p.StatusEffects.AimBonus > 0) st.Add(("AIMING", Cyan));
            if (p.StatusEffects.TakeCoverAc > 0) st.Add((p.StatusEffects.EvasiveMelee ? "EVASIVE" : "COVER", Cyan));
            if (p.StatusEffects.Hunkered) st.Add(("HUNKERED", Cyan));
            if (p.OverwatchHeld) st.Add(("OVERWATCH", Yellow));
            for (int i = 0; i < _chips.Count; i++)
            {
                bool on = i < st.Count;
                _chips[i].go.SetActive(on);
                if (on) { _chips[i].t.text = st[i].Item1; _chips[i].t.color = st[i].Item2; _chips[i].edge.color = st[i].Item2; }
            }
            Pips(_ap, p.Ap, p.MaxAp, Yellow);
            Pips(_mp, (int)p.Mp, p.MaxMp, Cyan);
            // real time has no turns: AP and MP mean nothing (Yan: "it also keeps updating the MP values but we got rid of that")
            _apL.gameObject.SetActive(!Rt.On); _mpL.gameObject.SetActive(!Rt.On);
            foreach (var seg in _ap) seg.gameObject.SetActive(!Rt.On);
            foreach (var seg in _mp) seg.gameObject.SetActive(!Rt.On);
            Meter(_food, p.Hunger / 10f); Meter(_water, p.Thirst / 10f); Meter(_rest, p.Tiredness / 10f);

            // weapon card
            var w = p.EquippedWeapon;
            string key = w?.Db != null ? WeaponArt.Identify(w.Db).key : null;
            if (key != _weaponKey)
            {
                _weaponKey = key;
                var tex = key != null ? Resources.Load<Texture2D>("Sprites/Weapons/weapon_" + key + "_hd") : null;
                if (tex != null) tex.filterMode = FilterMode.Point;
                _wImage.sprite = tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 16) : null;
                _wImage.color = tex != null ? Color.white : new Color(0, 0, 0, 0);
            }
            _wName.text = (w?.Name ?? "UNARMED").ToUpperInvariant();
            bool melee = w != null && CombatRules.IsMelee(w);
            var cond = w == null ? "" : w.IsJammed ? "JAMMED" : (w.Condition ?? ItemCondition.Working).ToUpperInvariant();
            SetChip(_wChips[0], cond, w == null ? Dim : w.IsJammed || cond == "BROKEN" ? Red : cond == "WORKING" ? Dim : Yellow);
            SetChip(_wChips[1], w == null || melee ? "" : (CombatRules.CanBurst(w) ? p.FireMode.ToUpperInvariant() : "SINGLE") + (p.AimedPart != null && p.FireMode != "Burst" ? " " + p.AimedPart.ToUpperInvariant() : ""), p.AimedPart != null ? Yellow : Dim);   // Batch 57: the aimed part
            _wMag.text = w == null || melee ? "-" : w.CurrentMagazine.ToString();
            _wCap.text = w == null || melee ? "" : $"/ {w.MagCapacity}";
            string ammo = w?.CurrentAmmoType ?? "Standard";
            int spare = w == null || melee ? 0 : Ammo.Get(p.Inventory, Ammo.Key(w.Db, ammo));
            _wAmmo.text = w == null || melee ? "" : $"{ammo.ToUpperInvariant()}\n+{spare} SPARE";

            // belt: the first four kinds of consumable; artifacts in the belt slots
            _beltIds.Clear();
            foreach (var it in p.Inventory.Items.Where(i => i.Type == "consumable").Take(4)) _beltIds.Add(it.Id);
            for (int i = 0; i < 4; i++)
            {
                var it = i < _beltIds.Count ? p.Inventory.Items.FirstOrDefault(x => x.Id == _beltIds[i]) : null;
                _belt[i].text = it == null ? "" : $"{Short(it.Name)}\n<size=11>x{it.Qty}</size>";
            }
            int slots = Mathf.Clamp(p.TotalBeltSlots, 1, 4);
            for (int i = 0; i < 4; i++)
            {
                string id = i < p.ArtifactSlots.Length ? p.ArtifactSlots[i] : null;
                _arts[i].text = i >= slots ? "<color=#3a3e41>X</color>" : id != null && GameData.ArtifactDatabase.All.TryGetValue(id, out var a) ? Short(a.Name) : "";
            }
        }

        void UpdateTarget(Player p)
        {
            Mutant e = null;
            var mouse = Mouse.current;
            if (mouse != null && Camera.main != null && !GameState.Current.ModalOpen)
            {
                var wpos = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());
                if (TacticalGridView.WorldToGrid(wpos, out int x, out int y))
                    e = EncounterState.Enemies.FirstOrDefault(u => u.X == x && u.Y == y && u.IsAlive() && Vision.Shows(u));
            }
            if (e == null) e = EncounterState.Enemies.FirstOrDefault(u => u.IsAlive() && u.IsAggroed && Vision.Shows(u));
            _target.SetActive(e != null);
            if (e == null) return;
            string rank = e is HumanEnemy h ? h.Rank : $"TIER {e.Db?.Tier ?? 1}";
            _targetName.text = $"{e.Name}  {rank}".ToUpperInvariant();
            bool friendly = e is HumanEnemy hf && EnemyAI.FriendlyToPlayer(hf);
            _targetState.text = friendly ? "FRIENDLY" : e.IsAggroed ? "HOSTILE" : "UNAWARE";
            _targetState.color = friendly ? Green : e.IsAggroed ? Red : Yellow;
            FillSegs(_tgtSeg, e.Hp / (float)Mathf.Max(1, e.MaxHp), Red);
        }

        static string Short(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            var words = name.Split(' ');
            return (words.Length > 1 ? words[words.Length - 1] : name).ToUpperInvariant().Substring(0, Mathf.Min(6, (words.Length > 1 ? words[words.Length - 1] : name).Length));
        }

        static void FillSegs(List<Image> seg, float f, Color c)
        {
            float n = Mathf.Clamp01(f) * seg.Count;
            for (int i = 0; i < seg.Count; i++)
                seg[i].color = i + 1 <= n ? c : i < n ? Color.Lerp(Empty, c, 0.45f) : Empty;
        }

        static void Pips(List<Image> pips, int have, int max, Color c)
        {
            for (int i = 0; i < pips.Count; i++)
            {
                pips[i].gameObject.SetActive(i < max);
                pips[i].color = i < have ? c : Empty;
            }
        }

        static void Meter(Image bar, float f)
        {
            var r = bar.rectTransform; r.sizeDelta = new Vector2(92 * Mathf.Clamp01(f), r.sizeDelta.y);
        }

        static void SetChip((TextMeshProUGUI t, Image edge) chip, string s, Color c)
        {
            chip.edge.gameObject.SetActive(!string.IsNullOrEmpty(s));
            chip.t.text = s; chip.t.color = c; chip.edge.color = c;
        }
    }
}
