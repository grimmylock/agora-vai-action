// The trader (B in a Safe Zone, at a quartermaster, or at a wandering trader). 2026-11-18 (Yan: "something similar to Resident Evil 4"): rebuilt as the merchant screen of RE4 - a dim room, a dark-leather panel with a brass rim, the trader's
// name and your RU at the top, TABS down the left (BUY / SELL / REPAIR / UPGRADE / HIRE, only the ones this trader does), the list in the middle (a row each: name, price; the selected one lit, what you cannot afford in red; the wheel turns
// the page), and on the right the PREVIEW of the selected thing - its picture, what it is, its weight and price - and one big button to do it. Keys: Up / Down pick, Enter does, Q / E change tab, wheel pages, Esc or B leaves.
// The trading rules are unchanged (ShopSystem / Upgrades / Companion / Economy / RolledStockSystem): this only decides what is shown and calls them. The old list-in-a-panel version is kept in git (before 2026-11-18).
// Added to the scene by GameBootstrap, built at runtime under the Canvas. Unity-only drawing, never run when written.
using System.Collections.Generic;
using System.Linq;
using GameData;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameCore
{
    public class ShopUI : MonoBehaviour
    {
        private const int RowsPerPage = 13;
        private const float RowH = 46f;
        private static ShopUI _instance;

        private GameObject _dim, _box;
        private bool _open;
        private ZoneDef _currentZone;
        private QuartermasterDef _currentQuartermaster;
        enum Tab { Buy, Sell, Repair, Upgrade, Hire }
        Tab _tab; int _page, _sel;
        class Row { public string Label, Price, Desc, DoLabel; public InventoryItem Preview; public System.Action Do; public bool Header, Afford = true; }
        readonly List<Row> _rows = new List<Row>();
        readonly List<(RectTransform rect, Image img, int index)> _rowViews = new List<(RectTransform, Image, int)>();

        static readonly Color Brass = new Color(0.55f, 0.43f, 0.24f), BrassDim = new Color(0.32f, 0.26f, 0.16f), Leather = new Color(0.075f, 0.062f, 0.052f, 0.98f), Ink = new Color(0.04f, 0.035f, 0.03f, 1f);
        static readonly Color Bone = new Color(0.92f, 0.9f, 0.84f), Dimc = new Color(0.62f, 0.6f, 0.54f), Gold = new Color(0.93f, 0.78f, 0.28f), Bad = new Color(0.9f, 0.3f, 0.25f);

        private void Awake() => _instance = this;

        /// <summary>Called from PlayerController's B keybind - static so PlayerController doesn't need to hold a reference to the component GameBootstrap added.</summary>
        public static void Toggle() => _instance?.ToggleInternal();

        /// <summary>Safe Zone = has a shop; a named Quartermaster (QuartermasterDatabase) trades by reputation; a Wandering Trader is a roll-gated stand-in (MacroState.Current.WanderingTraderZoneId).</summary>
        private void ToggleInternal()
        {
            if (_open) { Close(); return; }
            var zone = MacroMapDatabase.Find(MacroState.Current.CurrentZoneId);
            if (zone == null) { GameLog.Write("No shop here."); return; }
            var qm = QuartermasterDatabase.Find(zone.Id);
            if (qm != null)
            {
                var player = Player.Current;
                if (!QuartermasterDatabase.CanTrade(qm, player.GetRepLevel(qm.Faction)))
                {
                    GameLog.Write(string.IsNullOrEmpty(qm.Faction)
                        ? $"{qm.Name} isn't interested in trading right now."
                        : $"{qm.Name} won't trade with you - your reputation with the {qm.Faction} isn't good enough.");
                    return;
                }
                _currentZone = zone; _currentQuartermaster = qm; Open(); return;
            }
            if (MacroState.Current.WanderingTraderZoneId == zone.Id || zone.IsSafeZone) { _currentZone = zone; _currentQuartermaster = null; Open(); return; }
            GameLog.Write("No shop here.");
        }

        private void Open()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) { Debug.LogWarning("ShopUI: no Canvas found in the scene - can't show the shop panel."); return; }
            _open = true; GameState.Current.ModalOpen = true; _tab = Tab.Buy; _page = 0; _sel = 0;
            _dim = Px(canvas.transform, "ShopDim", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.66f)).gameObject;
            var dr = (RectTransform)_dim.transform; dr.anchorMin = Vector2.zero; dr.anchorMax = Vector2.one; dr.sizeDelta = Vector2.zero; _dim.GetComponent<Image>().raycastTarget = true;
            Rebuild();
        }

        private void Close()
        {
            _open = false; GameState.Current.ModalOpen = false;
            HudView.PackClosedFrame = Time.frameCount;                      // the Esc that closed it does not also pause
            if (_box != null) Destroy(_box); if (_dim != null) Destroy(_dim); _box = _dim = null;
        }

        // ---------------------------------------------------------------- what is on offer
        private (int tier, string name) Trader()
        {
            if (_currentQuartermaster != null) return (_currentQuartermaster.MaxTier, _currentQuartermaster.Name);
            if (_currentZone != null && MacroState.Current.WanderingTraderZoneId == _currentZone.Id) return (MacroState.Current.WanderingTraderTier, "Wandering Trader");
            return (_currentZone?.Tier ?? 1, _currentZone?.Name ?? "Trader");
        }

        List<Tab> Tabs()
        {
            var t = new List<Tab> { Tab.Buy, Tab.Sell };
            if (ShopSystem.TechnicianHere()) t.Add(Tab.Repair);
            if (Upgrades.TechnicianHere()) t.Add(Tab.Upgrade);
            if (_currentQuartermaster?.Faction == "Wanderers") t.Add(Tab.Hire);
            return t;
        }

        static InventoryItem AsItem(ShopItemDef d) => new InventoryItem { Id = d.Id, Name = d.Name, Type = d.Type, Category = d.Category, Key = d.Key, Qty = 1, WeightKg = d.WeightKg };

        void BuildRows(Player player)
        {
            _rows.Clear();
            var (traderTier, _) = Trader();
            if (_tab == Tab.Buy)
            {
                var stock = ShopDatabase.Items.Where(i => i.Baseline).ToList();
                stock.AddRange(ShopSystem.AmmoStock(player, traderTier));                      // ammo by caliber, for the guns you carry
                foreach (var item in stock)
                {
                    var it = item; int price = ShopSystem.GetBuyPrice(it);
                    _rows.Add(new Row { Label = it.Type == "ammo" ? AmmoLabel(it, player) : it.Name, Price = price + " RU", Afford = player.Ru >= price, Preview = AsItem(it), DoLabel = "PURCHASE", Desc = Describe(AsItem(it), it), Do = () => ShopSystem.BuyItem(it.Id) });
                }
                // the rolled one-off stock of this trader (RolledStockSystem), gone once bought
                var zoneId = _currentZone?.Id;
                if (!string.IsNullOrEmpty(zoneId) && !MacroState.Current.RolledShopStock.ContainsKey(zoneId))
                {
                    bool wandering = _currentQuartermaster == null && !_currentZone.IsSafeZone;
                    var fresh = Economy.RollTraderStock(traderTier, wandering);
                    if (MacroState.Current.CaravanZoneId == zoneId) fresh.AddRange(Economy.RollTraderStock(traderTier, true).Take(2));
                    MacroState.Current.RolledShopStock[zoneId] = fresh;
                }
                var special = !string.IsNullOrEmpty(zoneId) && MacroState.Current.RolledShopStock.TryGetValue(zoneId, out var rolled) ? rolled : null;
                if (special != null && special.Count > 0)
                {
                    _rows.Add(new Row { Header = true, Label = $"-- Today's stock (Day {MacroState.Current.Day}) --" });
                    foreach (var item in special)
                    {
                        var it = item; int price = ShopSystem.GetBuyPrice(it);
                        _rows.Add(new Row { Label = it.Type == "ammo" ? AmmoLabel(it, player) : it.Name, Price = price + " RU", Afford = player.Ru >= price, Preview = AsItem(it), DoLabel = "PURCHASE", Desc = Describe(AsItem(it), it), Do = () => ShopSystem.BuyRolledItem(zoneId, it) });
                    }
                }
            }
            else if (_tab == Tab.Sell)
            {
                foreach (var item in player.Inventory.Items.ToList())
                {
                    var it = item;
                    string cond = (it.Type == "weapon" || it.Type == "armor") && !string.IsNullOrEmpty(it.Condition) && it.Condition != ItemCondition.Working ? $" ({it.Condition})" : "";
                    _rows.Add(new Row { Label = $"{it.Name}{cond}" + (it.Qty > 1 ? $"  x{it.Qty}" : ""), Price = ShopSystem.GetSellValuePerUnit(it) + " RU", Preview = it, DoLabel = "SELL", Desc = HudView.Describe(it), Do = () => ShopSystem.SellItem(it.Id, 1) });
                }
            }
            else if (_tab == Tab.Repair)
            {
                void Add(string label, string price, InventoryItem prev, System.Action go) => _rows.Add(new Row { Label = label, Price = price, Preview = prev, DoLabel = "REPAIR", Do = go, Desc = "The technician brings it back to Working." });
                if (player.EquippedWeapon != null && player.EquippedWeapon.Condition != ItemCondition.Working) Add($"Repair {player.EquippedWeapon.Name} ({player.EquippedWeapon.Condition})", "", null, () => ShopSystem.RepairEquipped(true));
                if (player.EquippedArmor != null && player.EquippedArmorCondition != ItemCondition.Working) Add($"Repair {player.EquippedArmor.Name} ({player.EquippedArmorCondition})", "", null, () => ShopSystem.RepairEquipped(false));
                foreach (var damaged in player.Inventory.Items.Where(i => (i.Type == "weapon" || i.Type == "armor") && Economy.ConditionSteps(i.Condition) > 0).Take(8).ToList())
                { var d = damaged; Add($"Repair {d.Name} ({d.Condition})", Economy.RepairCost(d) + " RU", d, () => ShopSystem.Repair(d)); }
            }
            else if (_tab == Tab.Upgrade)
            {
                foreach (bool forWeapon in new[] { true, false })
                {
                    var eq = ShopSystem.EquippedAsItem(forWeapon); if (eq == null) continue;
                    var have = forWeapon ? player.EquippedWeapon.Upgrades : player.EquippedArmorUpgrades; if (have.Count >= 3) continue;
                    foreach (var kv in forWeapon ? Upgrades.Weapon : Upgrades.Armor)
                    {
                        if (have.Contains(kv.Key)) continue;
                        if (forWeapon && kv.Key == "Extended Mag" && CombatRules.IsMelee(player.EquippedWeapon)) continue;
                        string up = kv.Key; bool fw = forWeapon;
                        _rows.Add(new Row { Label = $"{eq.Name}: {up}", Price = Upgrades.CostText(player, eq, have.Count + 1), Preview = eq, DoLabel = "UPGRADE", Desc = kv.Value, Do = () => Upgrades.Buy(player, fw, up) });
                    }
                }
            }
            else if (_tab == Tab.Hire)
            {
                bool has = Companion.Alive;
                _rows.Add(new Row { Label = has ? $"{Companion.Current.Name} walks with you ({Companion.Current.Hp} HP)" : "Hire a Wanderer to walk with you", Price = has ? "" : Companion.HireCost + " RU", Afford = has || player.Ru >= Companion.HireCost, DoLabel = has ? "DISMISS" : "HIRE",
                    Desc = "A Wanderer walks with you, holds fire while you sneak and fights beside you.", Do = () => { if (has) Companion.Dismiss(); else Companion.Hire(player); } });
            }
            if (_sel >= _rows.Count) _sel = Mathf.Max(0, _rows.Count - 1);
            if (_rows.Count > 0 && _rows[_sel].Header) _sel = Mathf.Min(_rows.Count - 1, _sel + 1);
        }

        static string Describe(InventoryItem prev, ShopItemDef def)
        {
            string d = HudView.Describe(prev);
            if (def.Type == "ammo") d = (def.AmmoType ?? "Standard") + " rounds" + (def.Rounds.HasValue ? $", {def.Rounds} to a box" : "");
            if (def.Type == "backpack" && def.CapacityBonus.HasValue) d = $"A bigger bag: +{def.CapacityBonus} kg and more room in the case.";
            return d;
        }

        static string AmmoLabel(ShopItemDef item, Player player)
        {
            string key = ShopSystem.AmmoKey(item);
            if (key == null) return $"{item.Name} (draw a gun first)";
            return $"{Ammo.Label(key)} x{item.Rounds ?? ShopSystem.RoundsPerBox(player.EquippedWeapon)}";
        }

        // ---------------------------------------------------------------- drawing
        static Image Px(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = r.pivot = anchor; r.anchoredPosition = pos; r.sizeDelta = size;
            var im = go.AddComponent<Image>(); im.color = c; im.raycastTarget = false; return im;
        }
        static TextMeshProUGUI Lbl(Transform parent, string name, Vector2 pos, Vector2 size, string s, bool title, float px, Color c, TextAlignmentOptions align)
        {
            var t = Px(parent, name, new Vector2(0, 1), pos, size, Color.clear).gameObject.AddComponent<TextMeshProUGUI>();
            var f = title ? HudView.TitleFont : HudView.BodyFont; if (f != null) t.font = f;
            t.text = s; t.fontSize = px; t.color = c; t.alignment = align; t.raycastTarget = false; t.enableWordWrapping = false; if (title) t.characterSpacing = 4;
            return t;
        }
        Button Clickable(Image im, System.Action act) { im.raycastTarget = true; var b = im.gameObject.AddComponent<Button>(); b.onClick.AddListener(() => { Sfx.Play("ui_click"); act(); }); return b; }

        private void Rebuild()
        {
            if (!_open) return;
            var player = Player.Current; if (player == null) return;
            var tabs = Tabs(); if (!tabs.Contains(_tab)) _tab = Tab.Buy;
            BuildRows(player);
            if (_box != null) Destroy(_box);
            var canvas = FindAnyObjectByType<Canvas>(); if (canvas == null) return;
            float W = 1500f, H = 840f;
            var edge = Px(canvas.transform, "Shop", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(W, H), Brass); _box = edge.gameObject;
            var box = Px(edge.transform, "Fill", new Vector2(0, 1), new Vector2(3, -3), new Vector2(W - 6, H - 6), Leather).transform;
            // header
            var (tier, name) = Trader();
            Lbl(box, "Name", new Vector2(32, -22), new Vector2(800, 40), name.ToUpperInvariant(), true, 28, Bone, TextAlignmentOptions.TopLeft);
            Px(box, "Rule", new Vector2(0, 1), new Vector2(32, -66), new Vector2(150, 2), Gold);
            Lbl(box, "Tier", new Vector2(32, -74), new Vector2(500, 22), $"Tier {tier}", true, 12, Dimc, TextAlignmentOptions.TopLeft);
            Lbl(box, "Ru", new Vector2(W - 6 - 32 - 420, -20), new Vector2(420, 50), $"{player.Ru}", false, 42, Gold, TextAlignmentOptions.TopRight);
            Lbl(box, "RuL", new Vector2(W - 6 - 32 - 420, -70), new Vector2(420, 20), "RU", true, 12, Dimc, TextAlignmentOptions.TopRight);
            // tabs
            float ty = -120f;
            foreach (var t in tabs)
            {
                bool active = t == _tab; var tab = t;
                var b = Px(box, "Tab", new Vector2(0, 1), new Vector2(24, ty), new Vector2(200, 56), active ? Brass : BrassDim);
                var inn = Px(b.transform, "In", new Vector2(0, 1), new Vector2(2, -2), new Vector2(196, 52), active ? new Color(0.16f, 0.125f, 0.085f, 1f) : Ink);
                Lbl(inn.transform, "T", new Vector2(0, -14), new Vector2(196, 26), TabName(t), true, 16, active ? Bone : Dimc, TextAlignmentOptions.Center);
                Clickable(b, () => { _tab = tab; _page = 0; _sel = 0; Rebuild(); });
                ty -= 62f;
            }
            Lbl(box, "Keys", new Vector2(24, -H + 6 + 40), new Vector2(240, 36), "Q / E  TAB\nUP / DOWN  PICK\nENTER  DO    ESC  LEAVE", true, 10, Dimc, TextAlignmentOptions.TopLeft);
            // list
            int pages = Mathf.Max(1, Mathf.CeilToInt(_rows.Count / (float)RowsPerPage)); _page = Mathf.Clamp(_page, 0, pages - 1);
            if (_rows.Count > 0) _page = Mathf.Clamp(_sel / RowsPerPage, 0, pages - 1) == _page ? _page : Mathf.Clamp(_sel / RowsPerPage, 0, pages - 1);
            _rowViews.Clear();
            float lx = 252f, ly = -120f, lw = 760f;
            Px(box, "ListBack", new Vector2(0, 1), new Vector2(lx - 6, ly + 6), new Vector2(lw + 12, RowsPerPage * RowH + 12), Ink);
            if (_rows.Count == 0) Lbl(box, "Empty", new Vector2(lx + 12, ly - 14), new Vector2(lw, 30), "Nothing here.", false, 18, Dimc, TextAlignmentOptions.TopLeft);
            for (int i = _page * RowsPerPage, n = 0; i < _rows.Count && n < RowsPerPage; i++, n++)
            {
                var r = _rows[i]; int idx = i; float y = ly - n * RowH;
                var row = Px(box, "Row", new Vector2(0, 1), new Vector2(lx, y), new Vector2(lw, RowH - 4), r.Header ? Color.clear : new Color(1, 1, 1, 0.04f));
                _rowViews.Add((row.rectTransform, row, i));
                if (r.Header) { Lbl(row.transform, "L", new Vector2(12, -12), new Vector2(lw - 24, 24), r.Label, true, 13, Gold, TextAlignmentOptions.TopLeft); continue; }
                Lbl(row.transform, "L", new Vector2(14, -10), new Vector2(lw - 200, 28), r.Label, false, 19, r.Afford || _tab == Tab.Sell ? Bone : Dimc, TextAlignmentOptions.TopLeft);
                Lbl(row.transform, "P", new Vector2(lw - 190, -10), new Vector2(176, 28), r.Price ?? "", false, 19, r.Afford ? Gold : Bad, TextAlignmentOptions.TopRight);
                Clickable(row, () => { _sel = idx; Rebuild(); });
            }
            Lbl(box, "Page", new Vector2(lx, ly - RowsPerPage * RowH - 12), new Vector2(lw, 20), pages > 1 ? $"< {_page + 1} / {pages} >   (wheel)" : "", true, 11, Dimc, TextAlignmentOptions.TopRight);
            // preview
            float px = 1042f, pw = W - 6 - 32 - px;
            Px(box, "PrevBack", new Vector2(0, 1), new Vector2(px, -120 + 6), new Vector2(pw, H - 6 - 120 - 36), Ink);
            if (_rows.Count > 0 && !_rows[_sel].Header)
            {
                var r = _rows[_sel];
                var plate = Px(box, "Plate", new Vector2(0, 1), new Vector2(px + 20, -140), new Vector2(pw - 40, 170), new Color(0.12f, 0.1f, 0.08f, 1f));
                if (r.Preview != null) { var ic = Px(plate.transform, "Icon", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(pw - 80, 150), Color.white); ic.sprite = HudView.Icon(r.Preview); ic.preserveAspect = true; if (ic.sprite == null) ic.color = Color.clear; }
                Lbl(box, "PName", new Vector2(px + 20, -330), new Vector2(pw - 40, 60), (r.Preview?.Name ?? r.Label).ToUpperInvariant(), true, 20, Bone, TextAlignmentOptions.TopLeft).enableWordWrapping = true;
                Lbl(box, "PDesc", new Vector2(px + 20, -396), new Vector2(pw - 40, 220), r.Desc ?? "", false, 17, Dimc, TextAlignmentOptions.TopLeft).enableWordWrapping = true;
                if (r.Preview != null && r.Preview.WeightKg > 0f) Lbl(box, "PW", new Vector2(px + 20, -H + 6 + 170), new Vector2(pw - 40, 22), $"{r.Preview.WeightKg:0.0} kg   {Pack.Size(r.Preview).w}x{Pack.Size(r.Preview).h}", true, 11, Dimc, TextAlignmentOptions.TopLeft);
                var go = Px(box, "Do", new Vector2(0, 1), new Vector2(px + 20, -H + 6 + 120), new Vector2(pw - 40, 62), r.Afford ? Brass : BrassDim);
                var goIn = Px(go.transform, "In", new Vector2(0, 1), new Vector2(2, -2), new Vector2(pw - 44, 58), r.Afford ? new Color(0.2f, 0.155f, 0.09f, 1f) : Ink);
                Lbl(goIn.transform, "T", new Vector2(0, -16), new Vector2(pw - 44, 28), $"{r.DoLabel}" + (string.IsNullOrEmpty(r.Price) ? "" : "   " + r.Price), true, 17, r.Afford ? Bone : Dimc, TextAlignmentOptions.Center);
                Clickable(go, DoSelected);
            }
            Px(box, "CloseEdge", new Vector2(0, 1), new Vector2(px + 20, -H + 6 + 60), new Vector2(pw - 40, 44), BrassDim);
            var closeBtn = Px(box, "CloseHit", new Vector2(0, 1), new Vector2(px + 22, -H + 6 + 58), new Vector2(pw - 44, 40), new Color(0.06f, 0.05f, 0.04f, 1f));
            Lbl(closeBtn.transform, "T", new Vector2(0, -10), new Vector2(pw - 40, 24), "LEAVE", true, 14, Dimc, TextAlignmentOptions.Center);
            Clickable(closeBtn, Close);
        }

        static string TabName(Tab t) => t == Tab.Buy ? "BUY" : t == Tab.Sell ? "SELL" : t == Tab.Repair ? "REPAIR" : t == Tab.Upgrade ? "UPGRADE" : "HIRE";

        void DoSelected()
        {
            if (_rows.Count == 0 || _rows[_sel].Header || _rows[_sel].Do == null) return;
            _rows[_sel].Do(); Rebuild();
        }

        private void Update()
        {
            if (!_open) return;
            var k = Keyboard.current; var m = Mouse.current;
            if (k != null)
            {
                if (k.escapeKey.wasPressedThisFrame || k.bKey.wasPressedThisFrame) { Close(); return; }
                var tabs = Tabs(); int ti = tabs.IndexOf(_tab);
                if (k.qKey.wasPressedThisFrame) { _tab = tabs[(ti + tabs.Count - 1) % tabs.Count]; _page = 0; _sel = 0; Rebuild(); return; }
                if (k.eKey.wasPressedThisFrame) { _tab = tabs[(ti + 1) % tabs.Count]; _page = 0; _sel = 0; Rebuild(); return; }
                if (k.downArrowKey.wasPressedThisFrame) { Step(1); return; }
                if (k.upArrowKey.wasPressedThisFrame) { Step(-1); return; }
                if (k.enterKey.wasPressedThisFrame) { DoSelected(); return; }
            }
            if (m != null)
            {
                float wheel = m.scroll.ReadValue().y;
                if (wheel != 0f) { int pages = Mathf.Max(1, Mathf.CeilToInt(_rows.Count / (float)RowsPerPage)); int np = Mathf.Clamp(_page + (wheel > 0f ? -1 : 1), 0, pages - 1); if (np != _page) { _sel = Mathf.Min(_rows.Count - 1, np * RowsPerPage); Rebuild(); return; } }
                var at = m.position.ReadValue();
                foreach (var (rect, img, index) in _rowViews)
                {
                    if (rect == null || img == null || _rows[index].Header) continue;
                    bool sel = index == _sel, hot = RectTransformUtility.RectangleContainsScreenPoint(rect, at, null);
                    img.color = sel ? new Color(Brass.r, Brass.g, Brass.b, 0.55f) : hot ? new Color(1f, 1f, 1f, 0.11f) : new Color(1f, 1f, 1f, 0.04f);
                }
            }
        }

        void Step(int d)
        {
            if (_rows.Count == 0) return;
            int i = _sel;
            do { i = (i + d + _rows.Count) % _rows.Count; } while (_rows[i].Header && i != _sel);
            _sel = i; Rebuild();
        }
    }
}
