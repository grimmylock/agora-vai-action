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
        RectTransform _pack, _bagRect, _outRect; TextMeshProUGUI _packInfo, _packHint;
        string _packStamp;
        InventoryItem _drag; bool _dragRot; int _dragDx, _dragDy; Image _dragImg;
        static readonly Dictionary<string, Sprite> Icons = new Dictionary<string, Sprite>();

        void TogglePack()
        {
            PackOpen = !PackOpen;
            GameState.Current.ModalOpen = PackOpen;
            if (PackOpen) { Pack.Settle(Player.Current); _packStamp = null; } else { PackClosedFrame = Time.frameCount; _drag = null; if (_pack != null) Destroy(_pack.gameObject); _pack = null; }
        }

        void UpdatePack()
        {
            var p = Player.Current; var k = Keyboard.current;
            if (k != null && k.iKey.wasPressedThisFrame && (PackOpen || !GameState.Current.ModalOpen)) { TogglePack(); return; }
            if (!PackOpen || p == null) return;
            if (k != null && k.escapeKey.wasPressedThisFrame) { TogglePack(); return; }
            string stamp = Stamp(p);
            if (stamp != _packStamp) { _packStamp = stamp; BuildPack(p); }
            HandlePackMouse(p, k);
        }

        static string Stamp(Player p) => (p.Inventory.Backpack ?? "") + "|" + p.EquippedWeapon?.Name + "|" + p.SecondaryWeapon?.Name + "|" + p.EquippedArmor?.Name + "|"
            + string.Join(";", p.Inventory.Items.Select(i => $"{i.Id}:{i.Qty}:{i.GridX}:{i.GridY}:{i.Rot}"));

        void BuildPack(Player p)
        {
            if (_pack != null) Destroy(_pack.gameObject);
            var bag = Pack.Bag(p);
            float bw = bag.Cols * Cell, bh = bag.Rows * Cell, W = bw + 48f + 420f, H = Mathf.Max(bh + 48f + 150f, 560f);
            _pack = Box(_root.transform, "Pack", new Vector2(0.5f, 0.5f), new Vector2(-W / 2f, H / 2f), new Vector2(W, H));
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
                var slot = Img(_pack, "Slot", new Vector2(0, 1), new Vector2(rx, y), new Vector2(150, 64), new Color(0, 0, 0, 0.3f));
                if (eq[i].item != null) { var im = Img(slot.transform, "Icon", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(142, 58), Color.white); im.sprite = Icon(eq[i].item); im.preserveAspect = true; Center(im.rectTransform); }
                Txt(_pack, "SlotName", new Vector2(rx + 160, y - 6), new Vector2(260, 20), eq[i].slot, true, 12, Dim);
                Txt(_pack, "SlotItem", new Vector2(rx + 160, y - 28), new Vector2(260, 24), eq[i].item?.Name ?? "-", false, 15, Cream);
            }
            _packInfo = Txt(_pack, "Info", new Vector2(rx, -346), new Vector2(400, 120), "", false, 15, Cream);
            _packInfo.enableWordWrapping = true;
            _packHint = Txt(_pack, "Hint", new Vector2(rx, -H + 70), new Vector2(400, 60), "DRAG to move    R turn\nRIGHT-CLICK use / equip    X drop    I close", true, 11, Dim);
            _packHint.enableWordWrapping = true;
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
        static Sprite Icon(InventoryItem it)
        {
            string id = (it.Id ?? "").Split('~')[0];
            string key = it.Type == "weapon" ? "w:" + it.Key : it.Type == "armor" ? "a:" + it.Name : it.Type + ":" + id;
            if (Icons.TryGetValue(key, out var s) && s != null) return s;
            Texture2D tex = null; Rect r = default;
            if (it.Type == "weapon")
            {
                tex = Resources.Load<Texture2D>("Sprites/Weapons/weapon_" + it.Key + "_hd") ?? Resources.Load<Texture2D>("Sprites/Weapons/weapon_" + it.Key);
            }
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

        void HandlePackMouse(Player p, Keyboard k)
        {
            var m = Mouse.current; if (m == null) return;
            var screen = m.position.ReadValue();
            var c = CellAt(_bagRect, screen);
            var bag = Pack.Bag(p);
            bool inBag = c.HasValue && c.Value.x >= 0 && c.Value.y >= 0 && c.Value.x < bag.Cols && c.Value.y < bag.Rows;
            var hover = _drag ?? ItemAt(p, inBag ? c : (-1, -1));
            _packInfo.text = hover == null ? $"{p.Inventory.Items.Sum(i => i.WeightKg * System.Math.Max(1, i.Qty)):0.0} kg carried"
                : $"<b>{hover.Name}</b>{(hover.Qty > 1 ? $"  x{hover.Qty}" : "")}\n{hover.WeightKg * System.Math.Max(1, hover.Qty):0.0} kg    {Pack.Size(hover).w}x{Pack.Size(hover).h}" + (string.IsNullOrEmpty(hover.Condition) ? "" : "\n" + hover.Condition);

            if (_drag != null)
            {
                if (k != null && k.rKey.wasPressedThisFrame) { _dragRot = !_dragRot; (_dragDx, _dragDy) = (_dragDy, _dragDx); }
                if (_dragImg != null)                                                        // the held icon follows the mouse
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)_root.transform, screen, null, out var lp);
                    _dragImg.rectTransform.parent.localPosition = new Vector3(lp.x, lp.y, 0f);
                    _dragImg.rectTransform.localRotation = Quaternion.Euler(0f, 0f, _dragRot ? 90f : 0f);
                }
                if (m.leftButton.wasReleasedThisFrame)
                {
                    bool ok = inBag && Pack.Move(p, _drag, c.Value.x - _dragDx, c.Value.y - _dragDy, _dragRot);
                    if (!ok && !inBag && CellAt(_outRect, screen) is var oc && oc.HasValue && oc.Value.y >= -1 && oc.Value.y <= 2) _drag.GridX = -1;   // put it aside
                    _drag = null; if (_dragImg != null) Destroy(_dragImg.transform.parent.gameObject); _dragImg = null; _packStamp = null;
                }
                return;
            }
            if (hover == null) return;
            if (m.leftButton.wasPressedThisFrame)
            {
                _drag = hover; _dragRot = hover.Rot;
                _dragDx = hover.GridX >= 0 && inBag ? c.Value.x - hover.GridX : 0; _dragDy = hover.GridX >= 0 && inBag ? c.Value.y - hover.GridY : 0;
                var (w, h) = Pack.Size(hover);
                var holder = Rect(_root.transform, "Held", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w * Cell, h * Cell));
                holder.pivot = new Vector2((_dragDx + 0.5f) / (hover.Rot ? h : w), 1f - (_dragDy + 0.5f) / (hover.Rot ? w : h));
                _dragImg = Img(holder, "Icon", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w * Cell - 8, h * Cell - 8), new Color(1f, 1f, 1f, 0.85f));
                Center(_dragImg.rectTransform); _dragImg.sprite = Icon(hover); _dragImg.preserveAspect = true;
            }
            else if (m.rightButton.wasPressedThisFrame) Act(p, hover);
            else if (k != null && k.xKey.wasPressedThisFrame) { p.DropItem(hover.Id, System.Math.Max(1, hover.Qty)); _packStamp = null; }
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
                case "backpack": p.EquipBackpackFromInventory(it.Id); break;
            }
        }
    }
}
