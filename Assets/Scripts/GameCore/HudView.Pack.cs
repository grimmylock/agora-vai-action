// Pack 1: the pack screen (I) - the inside of your backpack (Pack.cs): its grid in the bag's own cloth colour, sized by the bag,
// every item an icon over its footprint (Sprites/Items, the weapons' _hd sprites, armor_icon_atlas). Drag to move, R turns the
// one you're holding, right-click uses / equips / wears / belts it, X drops it at your feet. What doesn't fit waits on the strip
// under the bag until you make room. Your equipped gear and the weight sit on the right. The world keeps running (like the other
// panels: your hands stop, Yan's co-op rule); I or ESC closes it.
// ponytail: rebuilt from scratch whenever the pack changes (a few dozen Images), not pooled.
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
        public static bool PackOpen;
        public static int PackClosedFrame = -1;          // the ESC that closed it doesn't also pause (PlayerController)
        const float Cell = 56f;
        RectTransform _pack, _bagRect, _outRect; GameObject _packBox; readonly RectTransform[] _slots = new RectTransform[4]; TextMeshProUGUI _packInfo, _packHint;
        HumanEnemy _loot; RectTransform _lootRect; GameObject _lootBox; bool _dragBody;   // the body being looted (CorpseLootUI), its grid, its box; _dragBody: the item in the hand lies on the body
        string _packStamp; bool _beside;   // _beside: the pack is open next to a loot panel (CorpseLootUI), on the left
        InventoryItem _drag; bool _dragRot; int _dragDx, _dragDy; Image _dragImg;
        static readonly Dictionary<string, Sprite> Icons = new Dictionary<string, Sprite>();

        /// <summary>2026-11-18 (Yan: "add the player inventory screen when looting npcs"): looting a body opens your pack screen on the left beside the loot panel, so you see what fits before you take it (CorpseLootUI).
        /// I or ESC closes both. Takes go through Player.AddItem (Pack.Place), the pack redraws itself.</summary>
        public void PackForLoot(bool on)
        {
            if (on) { if (!PackOpen) { PackOpen = true; Pack.Settle(Player.Current); } _beside = true; _packStamp = null; }
            else
            {
                _beside = false; if (_lootBox != null) Destroy(_lootBox); _lootBox = null; _lootRect = null;
                if (PackOpen) { PackOpen = false; PackClosedFrame = Time.frameCount; DropDrag(); KillDim(); if (_packBox != null) Destroy(_packBox); _packBox = null; _pack = null; }
            }
        }

        void TogglePack()
        {
            if (_beside) { CorpseLootUI.CloseIfOpen(); return; }   // looting: the loot panel closes the pack with it

            PackOpen = !PackOpen;
            GameState.Current.ModalOpen = PackOpen;
            if (PackOpen) { Pack.Settle(Player.Current); _packStamp = null; }
            else { PackClosedFrame = Time.frameCount; DropDrag(); if (_packBox != null) Destroy(_packBox); _packBox = null; _pack = null; KillDim(); }   // the whole box (Yan: "it stays a grey screen")
        }

        void UpdatePack()
        {
            var p = Player.Current; var k = Keyboard.current;
            if (k != null && k.iKey.wasPressedThisFrame && (PackOpen || !GameState.Current.ModalOpen)) { TogglePack(); return; }
            if (!PackOpen || p == null) return;
            if (k != null && k.escapeKey.wasPressedThisFrame) { TogglePack(); return; }
            string stamp = Stamp(p) + (_loot != null ? "|L" + LootStamp() : "");
            if (stamp != _packStamp) { _packStamp = stamp; BuildPack(p); if (_loot != null) BuildLoot(); }
            HandlePackMouse(p, k);
        }

        static string Stamp(Player p) => (p.Inventory.Backpack ?? "") + "|" + p.EquippedWeapon?.Name + "|" + p.SecondaryWeapon?.Name + "|" + p.EquippedArmor?.Name + "|"
            + string.Join(";", p.Inventory.Items.Select(i => $"{i.Id}:{i.Qty}:{i.GridX}:{i.GridY}:{i.Rot}"));

        // ---- looting a body (2026-11-18, Yan: "add drag from the body's list into the grid, also add the grid as the looting screen for the enemies stuff") ----
        // The body's things lie in a grid of their own (Pack.LayOut, 8 wide) to the right of your bag; drag from it into your bag (a free spot of your choice), from your bag onto it, or around inside it;
        // right-click on one takes it; below the grid: the body's RU and rounds (Take), TAKE ALL, CLOSE. Opened by CorpseLootUI.
        public void LootStart(HumanEnemy c) { _loot = c; Pack.LayOut(c.Inventory.Items, Pack.BodyRows(c.Inventory.Items)); PackForLoot(true); }
        public void LootStop() { _loot = null; _dragBody = false; PackForLoot(false); }
        int LootRows() => Pack.BodyRows(_loot.Inventory.Items);
        string LootStamp() => _loot.Ru + "|" + string.Join(";", _loot.Inventory.AmmoReserve.Select(kv => kv.Key + kv.Value)) + "|" + string.Join(";", _loot.Inventory.Items.Select(i => $"{i.Id}:{i.Qty}:{i.GridX}:{i.GridY}:{i.Rot}"));

        void LootTakeAll(Player p)
        {
            foreach (var it in _loot.Inventory.Items.ToList()) p.AddItem(it);
            _loot.Inventory.Items.Clear();
            foreach (var kv in _loot.Inventory.AmmoReserve.ToList()) Ammo.Add(p.Inventory, Ammo.Normalize(p, kv.Key), kv.Value);
            _loot.Inventory.AmmoReserve.Clear();
            if (_loot.Ru > 0) { p.Ru += _loot.Ru; _loot.Ru = 0; }
            GameLog.Write($"Looted everything from {_loot.Name}'s body.");
            _packStamp = null;
        }

        /// <summary>One thing from the body into your pack: at the cell you dropped it on if that is free there, else the first free spot (what doesn't fit waits on the strip under the bag).</summary>
        void LootTake(Player p, InventoryItem it, (int x, int y)? cell, bool rot)
        {
            _loot.Inventory.Items.Remove(it);
            var before = p.Inventory.Items.ToList();
            p.AddItem(it);
            var added = p.Inventory.Items.FirstOrDefault(i => !before.Contains(i));      // null: it joined a stack (or went to the ammo stash)
            if (added != null && cell.HasValue) Pack.Move(p, added, cell.Value.x, cell.Value.y, rot);
            _packStamp = null;
        }

        /// <summary>One thing from your bag onto the body, at the dropped cell if free there, else the first free spot; no room: it stays with you.</summary>
        bool LootPut(Player p, InventoryItem it, (int x, int y)? cell, bool rot)
        {
            int rows = LootRows(); var items = _loot.Inventory.Items;
            it.Rot = rot; var (w, h) = Pack.Footprint(it);
            bool spot = cell.HasValue && Pack.FreeIn(Pack.BodyCols, rows, items, null, cell.Value.x, cell.Value.y, w, h);
            if (!p.RemoveItem(it.Id, System.Math.Max(1, it.Qty))) return false;
            items.Add(it);
            if (spot) { it.GridX = cell.Value.x; it.GridY = cell.Value.y; }
            else if (!Pack.PlaceIn(Pack.BodyCols, rows, items, it)) { items.Remove(it); p.AddItem(it); GameLog.Write("There is no room on the body."); return false; }
            _packStamp = null; return true;
        }

        void BuildLoot()
        {
            if (_lootBox != null) Destroy(_lootBox);
            if (_loot == null) return;
            int cols = Pack.BodyCols, rows = LootRows(); float bw = cols * Cell, bh = rows * Cell;
            var ammo = _loot.Inventory.AmmoReserve.Where(kv => kv.Value > 0).ToList(); int extra = (_loot.Ru > 0 ? 1 : 0) + ammo.Count;
            float W = bw + 48f, H = 48f + bh + 24f + extra * 34f + 56f;
            var box = Box(_root.transform, "Loot", new Vector2(0.82f, 0.5f), Vector2.zero, new Vector2(W, H));
            _lootBox = box.parent.gameObject;
            Txt(box, "Title", new Vector2(18, -12), new Vector2(W - 36, 30), (_loot.Name + "'S BODY").ToUpperInvariant(), true, 20, Cream);
            Color cloth = new Color(0.24f, 0.21f, 0.19f);
            var rim = Img(box, "Body", new Vector2(0, 1), new Vector2(18, -48), new Vector2(bw + 12, bh + 12), cloth * 0.55f);
            var inner = Img(rim.transform, "Inside", new Vector2(0, 1), new Vector2(6, -6), new Vector2(bw, bh), cloth);
            _lootRect = inner.rectTransform;
            for (int y = 0; y < rows; y++) for (int x = 0; x < cols; x++)
                Img(inner.transform, "Cell", new Vector2(0, 1), new Vector2(x * Cell + 2, -y * Cell - 2), new Vector2(Cell - 4, Cell - 4), new Color(cloth.r * 0.82f, cloth.g * 0.82f, cloth.b * 0.82f, 1f));
            foreach (var it in _loot.Inventory.Items.Where(i => i.GridX >= 0)) ItemImage(inner.transform, it, it.GridX * Cell, -it.GridY * Cell, it.Rot);
            float y0 = -48f - bh - 24f;
            var p = Player.Current;
            if (_loot.Ru > 0) { Txt(box, "Ru", new Vector2(18, y0 - 4), new Vector2(300, 24), $"{_loot.Ru} RU", false, 15, Cream); Btn(box, "TakeRu", new Vector2(W - 118, y0), new Vector2(100, 28), "TAKE", 13, () => { p.Ru += _loot.Ru; _loot.Ru = 0; _packStamp = null; }); y0 -= 34f; }
            foreach (var kv in ammo)
            {
                string type = kv.Key;
                Txt(box, "Ammo", new Vector2(18, y0 - 4), new Vector2(300, 24), $"{Ammo.Label(Ammo.Normalize(p, type))} x{kv.Value}", false, 15, Cream);
                Btn(box, "TakeAmmo", new Vector2(W - 118, y0), new Vector2(100, 28), "TAKE", 13, () => { Ammo.Add(p.Inventory, Ammo.Normalize(p, type), _loot.Inventory.AmmoReserve[type]); _loot.Inventory.AmmoReserve[type] = 0; _packStamp = null; });
                y0 -= 34f;
            }
            Btn(box, "TakeAll", new Vector2(18, -H + 46), new Vector2(150, 32), "TAKE ALL", 14, () => LootTakeAll(p));
            Btn(box, "Close", new Vector2(W - 168, -H + 46), new Vector2(150, 32), "CLOSE", 14, () => CorpseLootUI.CloseIfOpen());
        }

        void BuildPack(Player p)
        {
            if (_packBox != null) Destroy(_packBox);
            EnsureDim();
            var bag = Pack.Bag(p);
            float bw = bag.Cols * Cell, bh = bag.Rows * Cell, W = bw + 48f + 420f, H = Mathf.Max(bh + 48f + 150f, 560f);
            _pack = Box(_root.transform, "Pack", new Vector2(_beside ? 0.3f : 0.5f, 0.5f), Vector2.zero, new Vector2(W, H));   // centred (left of the loot panel while looting) (Rect's pivot is its anchor)
            _packBox = _pack.parent.gameObject;
            Txt(_pack, "Title", new Vector2(18, -12), new Vector2(600, 30), (bag.Id == "none" ? "POCKETS" : bag.Name.Split('(')[0].Trim().ToUpperInvariant()), true, 20, Cream);
            // the bag: its cloth, a darker seam round the opening, the cells sewn in
            Color cloth = C(bag.Colour.TrimStart('#'));
            var rim = Img(_pack, "Bag", new Vector2(0, 1), new Vector2(18, -48), new Vector2(bw + 12, bh + 12), cloth * 0.55f);
            var inner = Img(rim.transform, "Inside", new Vector2(0, 1), new Vector2(6, -6), new Vector2(bw, bh), cloth);
            _bagRect = inner.rectTransform;
            for (int y = 0; y < bag.Rows; y++)
                for (int x = 0; x < bag.Cols; x++)
                    Img(inner.transform, "Cell", new Vector2(0, 1), new Vector2(x * Cell + 2, -y * Cell - 2), new Vector2(Cell - 4, Cell - 4), new Color(cloth.r * 0.82f, cloth.g * 0.82f, cloth.b * 0.82f, 1f));
            foreach (var it in p.Inventory.Items.Where(i => i.GridX >= 0)) ItemImage(inner.transform, it, it.GridX * Cell, -it.GridY * Cell, it.Rot);
            // what doesn't fit
            var outside = Pack.Outside(p).ToList();
            _outRect = Img(_pack, "Outside", new Vector2(0, 1), new Vector2(18, -66 - bh - 12), new Vector2(bw + 12, 140), new Color(0, 0, 0, 0.25f)).rectTransform;
            Txt(_outRect, "Label", new Vector2(8, -4), new Vector2(bw, 20), outside.Count > 0 ? "DOESN'T FIT - MAKE ROOM, OR X TO DROP" : "", false, 14, Yellow);
            float ox = 8f;
            foreach (var it in outside) { var (w, h) = Pack.Size(it); ItemImage(_outRect, it, ox, -26f, false); ox += w * Cell + 8f; }
            // equipped gear and weight
            float rx = bw + 48f;
            Txt(_pack, "EqTitle", new Vector2(rx, -12), new Vector2(380, 26), "EQUIPPED", true, 16, Cream);
            var eq = new (string slot, InventoryItem item)[] {
                ("WEAPON", p.EquippedWeapon != null ? AsItem(p.EquippedWeapon) : null), ("SECOND", p.SecondaryWeapon != null ? AsItem(p.SecondaryWeapon) : null),
                ("ARMOR", p.EquippedArmor != null ? new InventoryItem { Type = "armor", Name = p.EquippedArmor.Name } : null),
                ("BACKPACK", bag.Id != "none" ? new InventoryItem { Type = "backpack", Id = bag.Id, Name = bag.Name } : null) };
            for (int i = 0; i < eq.Length; i++)
            {
                float y = -46 - i * 72;
                var slot = Img(_pack, "Slot", new Vector2(0, 1), new Vector2(rx, y), new Vector2(150, 64), new Color(0, 0, 0, 0.3f)); _slots[i] = slot.rectTransform;
                if (eq[i].item != null) { var im = Img(slot.transform, "Icon", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(142, 58), Color.white); im.sprite = Icon(eq[i].item); im.preserveAspect = true; Center(im.rectTransform); }
                Txt(_pack, "SlotName", new Vector2(rx + 160, y - 6), new Vector2(260, 20), eq[i].slot, true, 12, Dim);
                Txt(_pack, "SlotItem", new Vector2(rx + 160, y - 28), new Vector2(260, 24), eq[i].item?.Name ?? "-", false, 15, Cream);
            }
            _packInfo = Txt(_pack, "Info", new Vector2(rx, -346), new Vector2(400, 120), "", false, 15, Cream);
            _packInfo.enableWordWrapping = true;
            _packHint = Txt(_pack, "Hint", new Vector2(rx, -H + 84), new Vector2(400, 80), "DRAG to move, onto a slot to equip, out of a slot to unequip    R turn\nRIGHT-CLICK use / equip / unequip    X drop    sights: onto the gun slot, C / V off    I close", true, 11, Dim);
            _packHint.enableWordWrapping = true;
            try { DecorateCase(p, W, H, bw); } catch (System.Exception e) { Debug.LogException(e); }   // the look is a layer: if it fails the grid still works
        }

        static InventoryItem AsItem(Weapon w) => new InventoryItem { Type = "weapon", Name = w.Name, Category = CombatRules.Category(w.Db), Key = WeaponArt.Identify(w.Db).key };
        static void Center(RectTransform r) { r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f); r.anchoredPosition = Vector2.zero; }

        Image ItemImage(Transform parent, InventoryItem it, float x, float y, bool rot)
        {
            var (w, h) = Pack.Size(it);
            // a turned item: laid out flat in a holder, the holder turned a quarter about the footprint's corner
            var holder = Rect(parent, "Item", new Vector2(0, 1), new Vector2(x, y), new Vector2((rot ? h : w) * Cell, (rot ? w : h) * Cell));
            var img = Img(holder, "Icon", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w * Cell - 8, h * Cell - 8), Color.white);
            Center(img.rectTransform);
            if (rot) img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            img.sprite = Icon(it); img.preserveAspect = true;
            if (it.Qty > 1) Txt(holder, "Qty", new Vector2((rot ? h : w) * Cell - 34, -(rot ? w : h) * Cell + 22), new Vector2(30, 20), "x" + it.Qty, true, 12, Cream, TextAlignmentOptions.Right);
            return img;
        }

        /// <summary>The icon for an item: a weapon's detailed sprite, a suit from armor_icon_atlas, else Sprites/Items/icon_*.</summary>
        internal static Sprite Icon(InventoryItem it)
        {
            string id = (it.Id ?? "").Split('~')[0];
            string key = it.Type == "weapon" ? "w:" + it.Key : it.Type == "armor" ? "a:" + it.Name : it.Type + ":" + id;
            if (Icons.TryGetValue(key, out var s) && s != null) return s;
            Texture2D tex = null; Rect r = default;
            if (it.Type == "weapon")
            {
                tex = Resources.Load<Texture2D>("Sprites/Weapons/weapon_" + it.Key + "_hd") ?? Resources.Load<Texture2D>("Sprites/Weapons/weapon_" + it.Key);
            }
            else if (it.Type == "attachment") tex = Resources.Load<Texture2D>("Sprites/Attachments/att_" + it.Key);
            else if (it.Type == "armor")
            {
                int i = System.Array.IndexOf(SpriteAtlasIndex.Armor, it.Name);
                tex = i >= 0 ? Resources.Load<Texture2D>("Sprites/armor_icon_atlas") : null;
                if (tex != null) r = new Rect((i % 16) * 48, tex.height - (i / 16 + 1) * 48, 48, 48);
            }
            else
            {
                string name = it.Type == "consumable" ? "icon_" + id
                    : it.Type == "backpack" ? "icon_backpack_" + id
                    : it.Type == "artifact" ? "icon_artifact_" + (ArtifactDatabase.All.TryGetValue(id, out var ad) ? ad.Type : "Gravitational")
                    : "icon_" + it.Type;
                tex = Resources.Load<Texture2D>("Sprites/Items/" + name) ?? Resources.Load<Texture2D>("Sprites/Items/icon_loot");
            }
            if (tex == null) return null;
            if (r.width == 0) r = new Rect(0, 0, tex.width, tex.height);
            tex.filterMode = FilterMode.Point;
            return Icons[key] = Sprite.Create(tex, r, new Vector2(0.5f, 0.5f), 16f);
        }

        // ---- the mouse: pick up, carry (R turns), put down; right-click acts; X drops ----
        (int x, int y)? CellAt(RectTransform rect, Vector2 screen)
        {
            if (rect == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen, null, out var local)) return null;
            int cx = Mathf.FloorToInt(local.x / Cell), cy = Mathf.FloorToInt(-local.y / Cell);   // the bag's pivot is its top-left
            return (cx, cy);
        }

        InventoryItem ItemAt(Player p, (int x, int y)? c)
        {
            if (!c.HasValue) return null;
            foreach (var it in p.Inventory.Items.Where(i => i.GridX >= 0))
            {
                var (w, h) = Pack.Footprint(it);
                if (c.Value.x >= it.GridX && c.Value.x < it.GridX + w && c.Value.y >= it.GridY && c.Value.y < it.GridY + h) return it;
            }
            // the strip outside: items in a row, each its own width
            var oc = CellAt(_outRect, Mouse.current.position.ReadValue());
            if (oc.HasValue && oc.Value.y >= 0 && oc.Value.y <= 2)
            {
                float x = 8f, mx = oc.Value.x * Cell;
                foreach (var it in Pack.Outside(p)) { var (w, _) = Pack.Size(it); if (mx >= x && mx < x + w * Cell) return it; x += w * Cell + 8f; }
            }
            return null;
        }

        InventoryItem BodyItemAt((int x, int y)? c)
        {
            if (_loot == null || !c.HasValue) return null;
            foreach (var it in _loot.Inventory.Items.Where(i => i.GridX >= 0))
            {
                var (w, h) = Pack.Footprint(it);
                if (c.Value.x >= it.GridX && c.Value.x < it.GridX + w && c.Value.y >= it.GridY && c.Value.y < it.GridY + h) return it;
            }
            return null;
        }

        void HandlePackMouse(Player p, Keyboard k)
        {
            var m = Mouse.current; if (m == null) return;
            if (HandleContext(m)) return;                                                  // a context menu is open: it has the mouse
            var screen = m.position.ReadValue();
            var c = CellAt(_bagRect, screen);
            var bag = Pack.Bag(p);
            bool inBag = c.HasValue && c.Value.x >= 0 && c.Value.y >= 0 && c.Value.x < bag.Cols && c.Value.y < bag.Rows;
            var lc = _loot != null ? CellAt(_lootRect, screen) : null;
            bool inLoot = lc.HasValue && lc.Value.x >= 0 && lc.Value.y >= 0 && lc.Value.x < Pack.BodyCols && lc.Value.y < LootRows();
            var hover = _drag ?? (inLoot ? BodyItemAt(lc) : ItemAt(p, inBag ? c : (-1, -1)));
            _packInfo.text = hover == null ? $"{p.Inventory.Items.Sum(i => i.WeightKg * System.Math.Max(1, i.Qty)):0.0} kg carried"
                : $"<b>{hover.Name}</b>{(hover.Qty > 1 ? $"  x{hover.Qty}" : "")}\n{hover.WeightKg * System.Math.Max(1, hover.Qty):0.0} kg    {Pack.Size(hover).w}x{Pack.Size(hover).h}" + (string.IsNullOrEmpty(hover.Condition) ? "" : "\n" + hover.Condition) + (Describe(hover) is string dsc && dsc != "" ? "\n\n<color=#9a9f94>" + Loc.Text(dsc) + "</color>" : "");

            if (_drag != null)
            {
                if (k != null && k.rKey.wasPressedThisFrame) { _dragRot = !_dragRot; (_dragDx, _dragDy) = (_dragDy, _dragDx); }
                if (_dragImg != null)                                                        // the held icon follows the mouse
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)_root.transform, screen, null, out var lp);
                    _dragImg.rectTransform.parent.localPosition = new Vector3(lp.x, lp.y, 0f);
                    _dragImg.rectTransform.localRotation = Quaternion.Euler(0f, 0f, _dragRot ? 90f : 0f);
                }
                if (m.leftButton.wasReleasedThisFrame && _loot != null && (_dragBody || (inLoot && _dragSlot < 0 && SlotAt(screen) < 0)))
                {
                    // looting: from the body into your bag, around the body, or from your bag onto the body
                    if (_dragBody)
                    {
                        if (inBag) LootTake(p, _drag, (c.Value.x - _dragDx, c.Value.y - _dragDy), _dragRot);
                        else if (inLoot) Pack.MoveIn(Pack.BodyCols, LootRows(), _loot.Inventory.Items, _drag, lc.Value.x - _dragDx, lc.Value.y - _dragDy, _dragRot);
                    }
                    else LootPut(p, _drag, inLoot ? (lc.Value.x - _dragDx, lc.Value.y - _dragDy) : ((int, int)?)null, _dragRot);
                    DropDrag(); _packStamp = null; return;
                }
                if (m.leftButton.wasReleasedThisFrame)
                {
                    int slot = SlotAt(screen);
                    if (_dragSlot >= 0)                                                       // out of a slot: into the bag = unequip
                    {
                        if (slot != _dragSlot && (inBag || slot < 0)) Unequip(p, _dragSlot, inBag ? c : null);
                    }
                    else if (slot >= 0) EquipTo(p, _drag, slot);                              // onto a slot: equip it there
                    else
                    {
                        bool ok = inBag && Pack.Move(p, _drag, c.Value.x - _dragDx, c.Value.y - _dragDy, _dragRot);
                        if (!ok && !inBag && CellAt(_outRect, screen) is var oc && oc.HasValue && oc.Value.y >= -1 && oc.Value.y <= 2) _drag.GridX = -1;   // put it aside
                    }
                    DropDrag(); _packStamp = null;
                }
                return;
            }
            int over = SlotAt(screen);
            if (over >= 0 && hover == null)                                                   // an equipped slot: drag it out or right-click to unequip
            {
                var sw = over == 0 ? p.EquippedWeapon : over == 1 ? p.SecondaryWeapon : null;
                if (sw != null) _packInfo.text = $"<b>{sw.Name}</b>\n{Optics.Describe(sw)}" + (sw.Optic != null || sw.Under != null ? "\nC takes the sight off, V the laser" : "");
                if (sw != null && k != null && k.cKey.wasPressedThisFrame) { Optics.TakeOff(p, sw, false); _packStamp = null; }
                else if (sw != null && k != null && k.vKey.wasPressedThisFrame) { Optics.TakeOff(p, sw, true); _packStamp = null; }
                else if (m.rightButton.wasPressedThisFrame) { Unequip(p, over, null); _packStamp = null; }
                else if (m.leftButton.wasPressedThisFrame && SlotItem(p, over) is InventoryItem si && over < 2) { StartDrag(si, 0, 0); _dragSlot = over; }
                return;
            }
            if (hover == null) return;
            bool onBody = _loot != null && _loot.Inventory.Items.Contains(hover);
            if (m.leftButton.wasPressedThisFrame)
            {
                StartDrag(hover, onBody ? (inLoot ? lc.Value.x - hover.GridX : 0) : hover.GridX >= 0 && inBag ? c.Value.x - hover.GridX : 0, onBody ? (inLoot ? lc.Value.y - hover.GridY : 0) : hover.GridX >= 0 && inBag ? c.Value.y - hover.GridY : 0);
                _dragBody = onBody;
            }
            else if (m.rightButton.wasPressedThisFrame) { if (onBody) LootTake(p, hover, null, hover.Rot); else OpenContext(p, hover, screen); }
            else if (!onBody && k != null && k.xKey.wasPressedThisFrame) { p.DropItem(hover.Id, System.Math.Max(1, hover.Qty)); _packStamp = null; }
        }

        int _dragSlot = -1;
        void StartDrag(InventoryItem it, int dx, int dy)
        {
            _drag = it; _dragRot = it.Rot; _dragDx = dx; _dragDy = dy; _dragSlot = -1;
            var (w, h) = Pack.Size(it);
            var holder = Rect(_root.transform, "Held", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w * Cell, h * Cell));
            holder.pivot = new Vector2((dx + 0.5f) / (it.Rot ? h : w), 1f - (dy + 0.5f) / (it.Rot ? w : h));
            _dragImg = Img(holder, "Icon", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w * Cell - 8, h * Cell - 8), new Color(1f, 1f, 1f, 0.85f));
            Center(_dragImg.rectTransform); _dragImg.sprite = Icon(it); _dragImg.preserveAspect = true;
        }
        void DropDrag() { _drag = null; _dragBody = false; _dragSlot = -1; if (_dragImg != null) Destroy(_dragImg.transform.parent.gameObject); _dragImg = null; }

        /// <summary>Which equipped slot (0 weapon, 1 second, 2 armor, 3 backpack) the mouse is over, or -1.</summary>
        int SlotAt(Vector2 screen)
        {
            for (int i = 0; i < _slots.Length; i++)
                if (_slots[i] != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(_slots[i], screen, null, out var l) && l.x >= 0 && l.x <= 150 && l.y <= 0 && l.y >= -64) return i;
            return -1;
        }
        static InventoryItem SlotItem(Player p, int slot) =>
            slot == 0 ? (p.EquippedWeapon != null ? AsItem(p.EquippedWeapon) : null) : slot == 1 ? (p.SecondaryWeapon != null ? AsItem(p.SecondaryWeapon) : null) : null;

        /// <summary>Dropped on a slot: the weapon slots take weapons (second: Second), armor takes a suit, the backpack slot a pack.</summary>
        static void EquipTo(Player p, InventoryItem it, int slot)
        {
            if (slot == 0 && it.Type == "weapon") p.EquipWeaponFromInventory(it.Id);
            else if (slot == 1 && it.Type == "weapon") p.EquipSecondaryFromInventory(it.Id);
            else if (slot == 2 && it.Type == "armor") p.EquipArmorFromInventory(it.Id);
            else if (slot == 3 && it.Type == "backpack") p.EquipBackpackFromInventory(it.Id);
            else if (slot < 2 && it.Type == "attachment") Optics.FitFromInventory(p, it, slot == 0 ? p.EquippedWeapon : p.SecondaryWeapon);
            else GameLog.Write($"The {it.Name} doesn't go there.");
        }

        /// <summary>Out of a slot into the pack (at `cell` if there's room there). A suit or a backpack only comes off by putting another on.</summary>
        static void Unequip(Player p, int slot, (int x, int y)? cell)
        {
            int before = p.Inventory.Items.Count;
            if (slot == 0) p.UnequipWeapon();
            else if (slot == 1) p.StowSecondary();
            else { GameLog.Write(slot == 2 ? "Put another suit on to take this one off (drag it onto ARMOR)." : "Put another pack on to swap (drag it onto BACKPACK)."); return; }
            var it = p.Inventory.Items.Count > before ? p.Inventory.Items[p.Inventory.Items.Count - 1] : null;
            if (it != null && cell.HasValue) Pack.Move(p, it, cell.Value.x, cell.Value.y, it.Rot);
        }

        /// <summary>Right-click: what the old panel's button did - equip a weapon or suit, use a consumable, wear a pack, belt an artifact.</summary>
        static void Act(Player p, InventoryItem it)
        {
            switch (it.Type)
            {
                case "weapon": p.EquipWeaponFromInventory(it.Id); break;
                case "armor": p.EquipArmorFromInventory(it.Id); break;
                case "consumable": p.UseConsumable(it.Id); break;
                case "detector": p.EquipDetectorFromInventory(it.Id); break;
                case "artifact": p.EquipArtifactFromInventory(it.Id); break;
                case "armorMod": p.FitArmorMod(it.Id); break;
                case "attachment": if (!Optics.FitFromInventory(p, it, p.EquippedWeapon)) Optics.FitFromInventory(p, it, p.SecondaryWeapon); break;   // main gun first, then the second
                case "backpack": p.EquipBackpackFromInventory(it.Id); break;
            }
        }
    }
}
