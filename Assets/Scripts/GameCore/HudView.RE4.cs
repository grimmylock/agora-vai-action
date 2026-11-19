// 2026-11-18 (Yan: "work on the UI ... something similar to Resident Evil 4"): the survival-horror HUD. What RE4 does and what is taken from it:
//   - almost nothing on screen while all is well: the condition gauge, the ammo and the quick slots fade down when you are healthy, calm and not shooting, and come back the moment that changes;
//   - the condition gauge is a RING round your portrait that drains as you are hurt and goes green (FINE) -> yellow (CAUTION) -> red and pulsing (DANGER), a white trail showing what the last hit took;
//   - ammo bottom right as one big number (rounds in the magazine), the reserve beside it small, the weapon above in outline, the fire mode and load named under it;
//   - four quick slots in a DIAMOND (keys 1-4) for the healing things, as the item dial / d-pad of RE4;
//   - the place name top left with a thin rule, quiet; no boxed panels, no key strip (the pack screen and F1 say the keys).
// It is a layer over the old HUD, not a rewrite: the old cards stay built (and keep being filled, hidden), the new widgets copy from them and from the player each frame. F4 switches between this and the old HUD; any exception while building or updating
// switches it off for the session and logs why, so a bug here can never take the HUD away. Co-op: it shows the local player (Player.Current) like the old one. Unity-only drawing, never run when written.
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
        static bool Re4Wanted() { try { return PlayerPrefs.GetInt("ui_re4", 1) == 1; } catch { return true; } }
        static readonly Color Fine = new Color(0.55f, 0.82f, 0.45f), Caution = new Color(0.95f, 0.78f, 0.25f), Danger = new Color(0.9f, 0.25f, 0.2f), Bone = new Color(0.92f, 0.9f, 0.84f);

        bool _re4Ok; GameObject _re4Root;
        CanvasGroup _gGauge, _gAmmo, _gQuick;
        Image _rRing, _rGhost, _rPortrait, _rWeapon, _rStamina, _rFocus; TextMeshProUGUI _rCond, _rHp, _rStatus, _rNeed, _rMag, _rReserve, _rLoad, _rWName, _rLoc, _rSub, _rAlert, _rArts;
        readonly List<TextMeshProUGUI> _rSlot = new List<TextMeshProUGUI>(); readonly List<Image> _rSlotBack = new List<Image>();
        float _rGhostV = 1f, _shown = 1f; float _ammoStampAt = -9f, _hpStampAt = -9f; string _ammoStamp; int _rLastHp = -1; float _gaugeA = 1f, _ammoA = 1f, _quickA = 1f;
        static Sprite _sRing, _sDisc, _sSoft, _sVig;
        TextMeshProUGUI _rSecond; Image _rVig; readonly List<(string text, float until)> _toasts = new List<(string, float)>(); readonly List<TextMeshProUGUI> _rToast = new List<TextMeshProUGUI>();

        // ---- generated sprites (a ring that a filled Image can sweep, a disc to mask the portrait, a soft dark patch behind the clusters) ----
        static Sprite MakeSprite(System.Func<float, float, float> alphaAt, int n = 128)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) { float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f; tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(alphaAt(u, v)))); }
            tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }
        static Sprite RingSpr() => _sRing ?? (_sRing = MakeSprite((u, v) => { float d = Mathf.Sqrt(u * u + v * v); float edge = 0.02f; return Mathf.Clamp01((d - 0.86f) / edge) * Mathf.Clamp01((1f - d) / edge); }, 256));
        static Sprite DiscSpr() => _sDisc ?? (_sDisc = MakeSprite((u, v) => { float d = Mathf.Sqrt(u * u + v * v); return Mathf.Clamp01((1f - d) / 0.03f); }, 128));
        static Sprite VigSpr() => _sVig ?? (_sVig = MakeSprite((u, v) => { float d = Mathf.Sqrt(u * u + v * v) / 1.25f; return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.55f) / 0.5f)); }, 64));
        static Sprite SoftSpr() => _sSoft ?? (_sSoft = MakeSprite((u, v) => { float d = Mathf.Sqrt(u * u + v * v); return Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d) * 0.9f; }, 64));

        TextMeshProUGUI T2(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, string s, bool title, float px, Color c, TextAlignmentOptions align)
        {
            var t = Rect(parent, name, anchor, pos, size).gameObject.AddComponent<TextMeshProUGUI>();
            var f = title ? _title : _body; if (f != null) t.font = f;
            t.text = Loc.Text(s); t.fontSize = px; t.color = c; t.alignment = align; t.raycastTarget = false; t.enableWordWrapping = false;
            t.outlineWidth = 0.22f; t.outlineColor = new Color32(0, 0, 0, 255);                   // legible on any ground without a panel
            if (title) t.characterSpacing = 4;
            return t;
        }

        void BuildRE4(Transform root)
        {
            if (!Re4Wanted()) return;
            try { BuildRE4Inner(root); _re4Ok = true; ApplyRe4(true); }
            catch (System.Exception e) { Debug.LogException(e); _re4Ok = false; if (_re4Root != null) Destroy(_re4Root); _re4Root = null; ApplyRe4(false); GameLog.Write("[DEV] The new HUD failed to build (" + e.Message + "): back to the old one. F4 tries again."); }
        }

        void ApplyRe4(bool on)
        {
            foreach (var n in new[] { "ZoneCard", "TurnBanner", "PlayerCard", "WeaponCard", "Keys" }) { var t = _root != null ? _root.transform.Find(n) : null; if (t != null) t.gameObject.SetActive(!on); }
            if (_re4Root != null) _re4Root.SetActive(on);
            var log = GameObject.Find("CombatLogBackground"); if (log != null) { var r = log.GetComponent<RectTransform>(); if (r != null) r.anchoredPosition = on ? new Vector2(56, 268) : new Vector2(24, 212); }   // above the condition ring
        }

        void ToggleRe4()
        {
            bool want = !(_re4Ok && _re4Root != null && _re4Root.activeSelf);
            try { PlayerPrefs.SetInt("ui_re4", want ? 1 : 0); PlayerPrefs.Save(); } catch { }
            if (want && _re4Root == null) { BuildRE4(_root.transform); } else { _re4Ok = want && _re4Root != null; ApplyRe4(_re4Ok); }
            GameLog.Write("[DEV] HUD: " + (want ? "survival-horror" : "classic") + ".");
        }

        void BuildRE4Inner(Transform root)
        {
            _re4Root = Rect(root, "Re4", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject;
            var rr = (RectTransform)_re4Root.transform; rr.anchorMin = Vector2.zero; rr.anchorMax = Vector2.one; rr.sizeDelta = Vector2.zero; rr.SetAsFirstSibling();
            var A = Vector2.zero; var BR = new Vector2(1, 0); var TL = new Vector2(0, 1);
            // the red edge of RE4 when you are in danger (a screen-wide ring of colour that pulses), and the pick-up toasts on the right
            _rVig = Img(_re4Root.transform, "Vignette", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.8f, 0.08f, 0.05f, 0f)); _rVig.sprite = VigSpr(); { var vr = _rVig.rectTransform; vr.anchorMin = Vector2.zero; vr.anchorMax = Vector2.one; vr.sizeDelta = Vector2.zero; }
            for (int i = 0; i < 4; i++) _rToast.Add(T2(_re4Root.transform, "Toast" + i, BR, new Vector2(-56, 372 + i * 30), new Vector2(620, 28), "", false, 19, Bone, TextAlignmentOptions.BottomRight));
            GameLog.Observer = OnLogLine;

            // ---- condition cluster, bottom left ----
            var g = Rect(_re4Root.transform, "Gauge", A, new Vector2(56, 52), new Vector2(520, 190)); _gGauge = g.gameObject.AddComponent<CanvasGroup>();
            var soft = Img(g, "Soft", A, new Vector2(-120, -100), new Vector2(880, 400), new Color(0, 0, 0, 0f)); soft.sprite = SoftSpr();
            var back = Img(g, "RingBack", A, new Vector2(0, 8), new Vector2(172, 172), new Color(0.04f, 0.045f, 0.05f, 0f)); back.sprite = DiscSpr();
            var track = Img(g, "RingTrack", A, new Vector2(0, 8), new Vector2(172, 172), new Color(1, 1, 1, 0.12f)); track.sprite = RingSpr();
            _rGhost = RingFill(g, "RingGhost", new Vector2(0, 8), 172, new Color(1, 1, 1, 0.65f));
            _rRing = RingFill(g, "Ring", new Vector2(0, 8), 172, Fine);
            var mask = Img(g, "PortraitMask", A, new Vector2(16, 24), new Vector2(140, 140), Color.white); mask.sprite = DiscSpr(); var mk = mask.gameObject.AddComponent<Mask>(); mk.showMaskGraphic = false;
            var pbg = Img(mask.transform, "Back", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140, 140), new Color(0.06f, 0.065f, 0.07f, 0f)); Center(pbg.rectTransform);
            _rPortrait = Img(mask.transform, "Portrait", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 150), Color.white); _rPortrait.preserveAspect = true; Center(_rPortrait.rectTransform);
            _rCond = T2(g, "Cond", A, new Vector2(196, 128), new Vector2(300, 28), "", true, 20, Fine, TextAlignmentOptions.BottomLeft);
            _rHp = T2(g, "Hp", A, new Vector2(196, 66), new Vector2(300, 60), "", false, 54, Bone, TextAlignmentOptions.BottomLeft);
            _rStatus = T2(g, "Status", A, new Vector2(196, 40), new Vector2(320, 22), "", true, 13, Danger, TextAlignmentOptions.BottomLeft);
            Img(g, "StamBack", A, new Vector2(196, 26), new Vector2(230, 6), new Color(1, 1, 1, 0.14f)); _rStamina = Img(g, "Stamina", A, new Vector2(196, 26), new Vector2(230, 6), Bone);
            Img(g, "FocusBack", A, new Vector2(196, 16), new Vector2(230, 3), new Color(1, 1, 1, 0.10f)); _rFocus = Img(g, "Focus", A, new Vector2(196, 16), new Vector2(230, 3), Cyan);
            _rNeed = T2(g, "Need", A, new Vector2(196, 152), new Vector2(320, 20), "", true, 12, Yellow, TextAlignmentOptions.BottomLeft);

            // ---- ammo, bottom right ----
            var a = Rect(_re4Root.transform, "Ammo", BR, new Vector2(-56, 52), new Vector2(520, 270)); _gAmmo = a.gameObject.AddComponent<CanvasGroup>();
            var soft2 = Img(a, "Soft", BR, new Vector2(120, -100), new Vector2(880, 520), new Color(0, 0, 0, 0f)); soft2.sprite = SoftSpr();
            _rWName = T2(a, "WName", BR, new Vector2(0, 232), new Vector2(500, 26), "", true, 18, Bone, TextAlignmentOptions.BottomRight);
            _rWeapon = Img(a, "Weapon", BR, new Vector2(0, 118), new Vector2(260, 110), new Color(1, 1, 1, 0.9f)); _rWeapon.preserveAspect = true;
            _rLoad = T2(a, "Load", BR, new Vector2(0, 92), new Vector2(500, 20), "", true, 12, Dim, TextAlignmentOptions.BottomRight);
            _rReserve = T2(a, "Reserve", BR, new Vector2(0, 14), new Vector2(130, 48), "", false, 34, Dim, TextAlignmentOptions.BottomRight);
            _rSecond = T2(a, "Second", BR, new Vector2(0, -34), new Vector2(500, 20), "", true, 11, Dim, TextAlignmentOptions.BottomRight);
            _rMag = T2(a, "Mag", BR, new Vector2(-124, 6), new Vector2(230, 90), "", false, 88, Bone, TextAlignmentOptions.BottomRight);

            // ---- quick slots, a diamond left of the ammo ----
            var q = Rect(_re4Root.transform, "Quick", BR, new Vector2(-470, 60), new Vector2(190, 190)); _gQuick = q.gameObject.AddComponent<CanvasGroup>();
            var offs = new[] { new Vector2(0, 66), new Vector2(66, 0), new Vector2(0, -66), new Vector2(-66, 0) };    // keys 1 (up), 2 (right), 3 (down), 4 (left)
            for (int i = 0; i < 4; i++)
            {
                var c = new Vector2(95, 95) + offs[i];
                var s = Img(q, "Slot" + i, A, c - new Vector2(30, 30), new Vector2(60, 60), new Color(0, 0, 0, 0f)); _rSlotBack.Add(s);
                Img(s.transform, "EdgeT", TL, Vector2.zero, new Vector2(60, 1), new Color(1, 1, 1, 0f)); Img(s.transform, "EdgeB", A, Vector2.zero, new Vector2(60, 1), new Color(1, 1, 1, 0f));
                Img(s.transform, "EdgeL", A, Vector2.zero, new Vector2(1, 60), new Color(1, 1, 1, 0f)); Img(s.transform, "EdgeR", A, new Vector2(59, 0), new Vector2(1, 60), new Color(1, 1, 1, 0f));
                T2(s.transform, "K", TL, new Vector2(4, -2), new Vector2(16, 14), (i + 1).ToString(), true, 10, Dim, TextAlignmentOptions.TopLeft);
                _rSlot.Add(T2(s.transform, "T", A, new Vector2(0, 6), new Vector2(60, 46), "", false, 12, Bone, TextAlignmentOptions.Center));
            }
            _rArts = T2(q, "Arts", A, new Vector2(0, -8), new Vector2(190, 18), "", true, 10, Cyan, TextAlignmentOptions.Center);

            // ---- place, top left; alert, top centre ----
            var l = Rect(_re4Root.transform, "Place", TL, new Vector2(56, -46), new Vector2(700, 90));
            _rLoc = T2(l, "Name", TL, Vector2.zero, new Vector2(700, 38), "", true, 28, Bone, TextAlignmentOptions.TopLeft);
            Img(l, "Rule", TL, new Vector2(0, -42), new Vector2(150, 2), new Color(Yellow.r, Yellow.g, Yellow.b, 0.9f));
            _rSub = T2(l, "Sub", TL, new Vector2(0, -52), new Vector2(700, 22), "", true, 12, Dim, TextAlignmentOptions.TopLeft);
            _rAlert = T2(_re4Root.transform, "Alert", new Vector2(0.5f, 1f), new Vector2(0, -46), new Vector2(900, 30), "", true, 16, Yellow, TextAlignmentOptions.Top);
        }

        Image RingFill(Transform parent, string name, Vector2 pos, float size, Color c)
        {
            var im = Img(parent, name, Vector2.zero, pos, new Vector2(size, size), c); im.sprite = RingSpr();
            im.type = Image.Type.Filled; im.fillMethod = Image.FillMethod.Radial360; im.fillOrigin = (int)Image.Origin360.Top; im.fillClockwise = true; im.fillAmount = 1f;
            return im;
        }

        /// <summary>The lines that are about getting something become toasts, right side, for a few seconds (RE4's "picked up" lines).</summary>
        void OnLogLine(string english, string shown, bool ev)
        {
            if (english == null) return;
            string e = english.TrimStart();
            bool pickup = e.StartsWith("+") || e.StartsWith("Gained ") || e.StartsWith("Bought ") || e.StartsWith("Sold ") || e.StartsWith("Recovered ") || e.StartsWith("Looted ") || e.StartsWith("Equipped ") || e.StartsWith("Perk gained") || e.StartsWith("Quest accepted") || e.StartsWith("Quest complete") || e.StartsWith("[RANK UP]") || e.StartsWith("[MILESTONE]") || e.StartsWith("Game saved") || e.StartsWith("Restored ") || e.StartsWith("[ARTIFACT]") || e.StartsWith("Fitted ") || e.StartsWith("Dropped ");
            if (!pickup) return;
            _toasts.Insert(0, (shown.Trim(), Time.unscaledTime + 4.5f)); if (_toasts.Count > 4) _toasts.RemoveAt(4);
        }

        void UpdateRE4(Player p)
        {
            var k = Keyboard.current;
            if (k != null && k.f4Key.wasPressedThisFrame && !GameState.Current.ModalOpen) { ToggleRe4(); return; }
            if (!_re4Ok || _re4Root == null || !_re4Root.activeSelf || p == null) return;
            try { TickRE4(p); }
            catch (System.Exception e) { Debug.LogException(e); _re4Ok = false; ApplyRe4(false); GameLog.Write("[DEV] The new HUD failed (" + e.Message + "): back to the old one."); }
        }

        void TickRE4(Player p)
        {
            float dt = Time.unscaledDeltaTime; var m = MacroState.Current; bool combat = EncounterState.InCombat;
            // ---- condition ----
            float hpf = Mathf.Clamp01(p.Hp / (float)Mathf.Max(1, p.MaxHp));
            bool down = Rt.On && Body.Of(p).Down;
            _shown = Mathf.MoveTowards(_shown, hpf, dt * 1.6f); if (hpf > _rGhostV) _rGhostV = hpf; else _rGhostV = Mathf.MoveTowards(_rGhostV, hpf, dt * 0.18f);
            if (_rLastHp != p.Hp) { if (_rLastHp >= 0) _hpStampAt = Time.unscaledTime; _rLastHp = p.Hp; }
            var col = down || hpf < 0.34f ? Danger : hpf < 0.67f ? Caution : Fine;
            float pulse = col == Danger ? 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * (down ? 7f : 4f)) : 1f;
            _rRing.fillAmount = _shown; _rRing.color = new Color(col.r, col.g, col.b, pulse); _rGhost.fillAmount = _rGhostV;
            _rCond.text = Loc.Text(down ? "DYING" : hpf < 0.34f ? "DANGER" : hpf < 0.67f ? "CAUTION" : "FINE"); _rCond.color = col;
            _rHp.text = p.Hp.ToString(); _rHp.color = col == Fine ? Bone : col;
            _rPortrait.sprite = _portrait != null ? _portrait.sprite : null;
            var st = new List<string>(); foreach (var c in _chips) if (c.go != null && c.go.activeSelf) st.Add(c.t.text);
            _rStatus.text = string.Join("   ", st); _rStatus.color = down ? Danger : Yellow;
            _rStamina.rectTransform.sizeDelta = new Vector2(230f * Mathf.Clamp01(Rt.Me.Stamina / Rt.StaminaMax), 6f);
            _rFocus.rectTransform.sizeDelta = new Vector2(230f * Mathf.Clamp01(Rt.Me.Focus / Rt.FocusMax), 3f); _rFocus.color = Rt.SlowMo ? Color.white : Cyan;
            var need = new List<string>(); if (p.Hunger / 10f < 0.3f) need.Add("HUNGRY"); if (p.Thirst / 10f < 0.3f) need.Add("THIRSTY"); if (p.Tiredness / 10f < 0.3f) need.Add("TIRED");
            _rNeed.text = need.Count == 0 ? "" : Loc.Text(string.Join("   ", need));

            // ---- ammo ----
            var w = p.EquippedWeapon; bool melee = w != null && CombatRules.IsMelee(w);
            _rWName.text = Loc.Text((w?.Name ?? "UNARMED").ToUpperInvariant());
            _rWeapon.sprite = _wImage != null ? _wImage.sprite : null; _rWeapon.color = _wImage != null ? _wImage.color : Color.clear;
            string mag = w == null || melee ? "" : w.CurrentMagazine.ToString(); string res = w == null || melee ? "" : Ammo.Get(p.Inventory, Ammo.Key(w.Db, w.CurrentAmmoType)).ToString();
            bool dry = w != null && !melee && (w.CurrentMagazine <= 0 || w.IsJammed); bool low = w != null && !melee && w.MagCapacity > 0 && w.CurrentMagazine <= Mathf.Max(1, w.MagCapacity / 4);
            _rMag.text = mag; _rMag.color = dry ? Danger : low ? Caution : Bone; _rReserve.text = res == "" ? "" : "/ " + res;
            string mode = w == null || melee ? "" : Rt.On ? Gunplay.ModeLabel(w, Rt.Of(p).FireMode) : "";
            string cond = w == null ? "" : w.IsJammed ? "JAMMED" : (w.Condition ?? ItemCondition.Working).ToUpperInvariant();
            _rLoad.text = Loc.Text(w == null || melee ? "" : (Ammo.LoadName(w).ToUpperInvariant() + (mode != "" ? "  ·  " + mode : "") + (cond != "" && cond != "WORKING" ? "  ·  " + cond : "")));
            _rSecond.text = p.SecondaryWeapon != null ? "Y  " + p.SecondaryWeapon.Name.ToUpperInvariant() : "";
            string stamp = mag + "|" + res + "|" + (w?.Name ?? ""); if (stamp != _ammoStamp) { _ammoStamp = stamp; _ammoStampAt = Time.unscaledTime; }

            // ---- quick slots ----
            for (int i = 0; i < 4; i++) { _rSlot[i].text = _belt.Count > i ? _belt[i].text : ""; _rSlotBack[i].color = Color.clear; }
            var arts = new List<string>(); foreach (var t in _arts) if (t != null && !string.IsNullOrEmpty(t.text) && !t.text.Contains("X</color>")) arts.Add(t.text);
            _rArts.text = string.Join(" ", arts);

            // ---- place and alert ----
            var zone = GameData.MacroMapDatabase.Find(m.CurrentZoneId);
            _rLoc.text = (zone?.Name ?? m.CurrentZoneId ?? "").ToUpperInvariant(); _rSub.text = _zoneSub != null ? _zoneSub.text : "";
            string alert = Rt.Paused ? "<color=#e0c050>PAUSED - ESC</color>" : combat ? "<color=#c8443a>FIGHT</color>" : "";
            if (Rt.SlowMo) alert += (alert != "" ? "   " : "") + "<color=#62c0e0>BULLET TIME</color>";
            if (m.SurgeRounds > 0) alert += (alert != "" ? "   " : "") + $"<color=#c8443a>SURGE IN {m.SurgeRounds}</color>";
            if (p.ArmedThrowable != null && GameData.ConsumableDatabase.All.TryGetValue(p.ArmedThrowable, out var armed)) alert += (alert != "" ? "   " : "") + $"<color=#e0c050>{armed.Name.ToUpperInvariant()} READY - LMB THROW, RMB CANCEL, T NEXT</color>";
            if (Survival.On) alert = $"<color=#e0c050>{Survival.Status()}</color>   " + alert;
            _rAlert.text = Loc.Text(alert);

            // ---- what is worth seeing right now: the rest fades down (RE4 hides what you do not need) ----
            bool calm = !combat && hpf >= 0.995f && st.Count == 0 && need.Count == 0 && Rt.Me.Stamina >= Rt.StaminaMax - 1f && Time.unscaledTime - _hpStampAt > 3.5f && !Rt.SlowMo;
            bool shooting = combat || Rt.Me.Aiming || dry || Time.unscaledTime - _ammoStampAt < 4f || Rt.Me.UsingId != null || GameState.Current.ModalOpen;
            float tg = calm ? 0.22f : 1f, ta = w == null || melee ? (combat ? 0.8f : 0.0f) : shooting ? 1f : 0.28f, tq = combat || hpf < 0.7f || p.ArmedThrowable != null || st.Count > 0 ? 1f : 0.4f;
            // the danger edge and the toasts
            float vig = down ? 0.5f + 0.2f * Mathf.Sin(Time.unscaledTime * 7f) : hpf < 0.34f ? (0.28f + 0.14f * Mathf.Sin(Time.unscaledTime * 4f)) * (1f - hpf / 0.34f * 0.5f) : 0f;
            if (!p.IsAlive() || Rt.On && Rt.Me != null && Rt.Me.Dead) vig = 0f;                   // 2026-11-19 (Yan: "after dying the screen still has the overlay"): no danger edge for the dead, and it is recomputed from scratch every frame
            _rVig.color = new Color(0.8f, 0.08f, 0.05f, Mathf.Clamp01(vig));
            for (int i = 0; i < _rToast.Count; i++)
            {
                if (i < _toasts.Count && Time.unscaledTime < _toasts[i].until) { _rToast[i].text = _toasts[i].text; float left = _toasts[i].until - Time.unscaledTime; _rToast[i].color = new Color(Bone.r, Bone.g, Bone.b, Mathf.Clamp01(left / 0.7f)); }
                else if (_rToast[i].text != "") _rToast[i].text = "";
            }
            _gaugeA = Mathf.MoveTowards(_gaugeA, tg, dt * 3f); _ammoA = Mathf.MoveTowards(_ammoA, ta, dt * 3f); _quickA = Mathf.MoveTowards(_quickA, tq, dt * 3f);
            _gGauge.alpha = _gaugeA; _gAmmo.alpha = _ammoA; _gQuick.alpha = _quickA;
        }
    }
}
