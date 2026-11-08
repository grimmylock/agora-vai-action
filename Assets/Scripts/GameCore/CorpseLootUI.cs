// New (Batch 10 - living-world NPCs): the loot screen for a dead HumanEnemy's own stash
// (HumanLootSystem.RollSpawnStash/TakeSpoils/GrantDrop populate npc.Inventory/npc.Ru - see that
// file). Opens when the player clicks a corpse on the tactical grid (PlayerController). Built at
// runtime under the existing Canvas, same convention as ShopUI/DeathScreen - no scene wiring
// required. Added automatically by GameBootstrap.
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore
{
    public class CorpseLootUI : MonoBehaviour
    {
        // ponytail: fixed-height list, no ScrollRect - same tradeoff ShopUI already makes, capped
        // instead of scrollable.
        private const int MaxRowsShown = 20;
        private const float RowHeight = 30f;

        private static CorpseLootUI _instance;

        private GameObject _panel;
        private Transform _list;
        private TextMeshProUGUI _headerText;
        private bool _open;
        private HumanEnemy _corpse;

        private void Awake() => _instance = this;

        /// <summary>Called from PlayerController when the player clicks a dead HumanEnemy.</summary>
        public static void Open(HumanEnemy corpse) => _instance?.OpenInternal(corpse);

        private void OpenInternal(HumanEnemy corpse)
        {
            if (corpse == null) return;
            Boss.Looted(corpse);   // Boss 1: a few seconds later, his smoke grenade
            _corpse = corpse;
            BuildPanelIfNeeded();
            if (_panel == null) return; // no Canvas in the scene - already warned in BuildPanelIfNeeded

            _open = true;
            GameState.Current.ModalOpen = true;
            _panel.SetActive(true);
            Rebuild();
        }

        private void Close()
        {
            _open = false;
            _corpse = null;
            GameState.Current.ModalOpen = false;
            if (_panel != null) _panel.SetActive(false);
        }

        private void BuildPanelIfNeeded()
        {
            if (_panel != null) return;

            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("CorpseLootUI: no Canvas found in the scene - can't show the loot panel.");
                return;
            }

            _panel = UIBuildUtil.CreatePanel(canvas.transform, "CorpseLootPanel", new Vector2(0.3f, 0.2f), new Vector2(0.7f, 0.8f),
                new Color(0.08f, 0.08f, 0.08f, 0.95f));

            _headerText = UIBuildUtil.CreateText(_panel.transform, "HeaderText", new Vector2(0.02f, 0.9f), new Vector2(0.7f, 0.99f),
                "", 18, Color.white, TextAlignmentOptions.Left);
            UIBuildUtil.CreateButton(_panel.transform, "CloseButton", new Vector2(0.8f, 0.9f), new Vector2(0.98f, 0.99f),
                "Close", Close);
            UIBuildUtil.CreateButton(_panel.transform, "TakeAllButton", new Vector2(0.02f, 0.81f), new Vector2(0.3f, 0.89f),
                "Take All", TakeAll);

            _list = CreateList(_panel.transform, "LootList", new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.8f));
        }

        private static Transform CreateList(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 3;
            return go.transform;
        }

        private void TakeAll()
        {
            if (_corpse == null) return;
            var player = Player.Current;
            if (player == null) return;

            foreach (var item in _corpse.Inventory.Items.ToList())
                player.AddItem(item);
            _corpse.Inventory.Items.Clear();

            foreach (var kvp in _corpse.Inventory.AmmoReserve.ToList())
            {
                Ammo.Add(player.Inventory, Ammo.Normalize(player, kvp.Key), kvp.Value);
            }
            _corpse.Inventory.AmmoReserve.Clear();

            if (_corpse.Ru > 0)
            {
                player.Ru += _corpse.Ru;
                _corpse.Ru = 0;
            }

            GameLog.Write($"Looted everything from {_corpse.Name}'s body.");
            Rebuild();
        }

        /// <summary>Rebuilds the list from scratch - same "rebuild-on-action" convention ShopUI
        /// already uses, cheap enough at this list size.</summary>
        private void Rebuild()
        {
            if (_corpse == null) { Close(); return; }
            var player = Player.Current;

            _headerText.text = $"{_corpse.Name}'s Body — RU: {_corpse.Ru}";

            foreach (Transform child in _list) Destroy(child.gameObject);

            if (_corpse.Ru > 0)
            {
                var ruRow = UIBuildUtil.CreateRow(_list, "TakeRu", RowHeight, new Color(1f, 0.85f, 0.3f, 0.08f));
                UIBuildUtil.CreateText(ruRow.transform, "Label", new Vector2(0.02f, 0f), new Vector2(0.68f, 1f),
                    $"{_corpse.Ru} RU", 14, Color.white, TextAlignmentOptions.Left);
                UIBuildUtil.CreateButton(ruRow.transform, "TakeBtn", new Vector2(0.7f, 0.1f), new Vector2(0.98f, 0.9f),
                    "Take", () =>
                    {
                        player.Ru += _corpse.Ru;
                        _corpse.Ru = 0;
                        Rebuild();
                    });
            }

            foreach (var kvp in _corpse.Inventory.AmmoReserve.Where(kv => kv.Value > 0).ToList())
            {
                string ammoType = kvp.Key;
                var row = UIBuildUtil.CreateRow(_list, $"Ammo_{ammoType}", RowHeight, new Color(1f, 1f, 1f, 0.05f));
                UIBuildUtil.CreateText(row.transform, "Label", new Vector2(0.02f, 0f), new Vector2(0.68f, 1f),
                    $"{Ammo.Label(Ammo.Normalize(player, ammoType))} x{kvp.Value}", 14, Color.white, TextAlignmentOptions.Left);
                UIBuildUtil.CreateButton(row.transform, "TakeBtn", new Vector2(0.7f, 0.1f), new Vector2(0.98f, 0.9f),
                    "Take", () =>
                    {
                        int amount = _corpse.Inventory.AmmoReserve.TryGetValue(ammoType, out var v) ? v : 0;
                        Ammo.Add(player.Inventory, Ammo.Normalize(player, ammoType), amount);
                        _corpse.Inventory.AmmoReserve[ammoType] = 0;
                        Rebuild();
                    });
            }

            var items = _corpse.Inventory.Items;
            foreach (var item in items.Take(MaxRowsShown))
            {
                string condSuffix = (item.Type == "weapon" || item.Type == "armor") && !string.IsNullOrEmpty(item.Condition) && item.Condition != ItemCondition.Working
                    ? $" ({item.Condition})" : "";
                var row = UIBuildUtil.CreateRow(_list, $"Item_{item.Id}", RowHeight, new Color(1f, 1f, 1f, 0.05f));
                UIBuildUtil.CreateText(row.transform, "Label", new Vector2(0.02f, 0f), new Vector2(0.68f, 1f),
                    $"{item.Name}{condSuffix} x{item.Qty}", 14, Color.white, TextAlignmentOptions.Left);
                UIBuildUtil.CreateButton(row.transform, "TakeBtn", new Vector2(0.7f, 0.1f), new Vector2(0.98f, 0.9f),
                    "Take", () =>
                    {
                        player.AddItem(item);
                        _corpse.Inventory.Items.Remove(item);
                        Rebuild();
                    });
            }
            if (items.Count > MaxRowsShown)
            {
                var moreRow = UIBuildUtil.CreateRow(_list, "More", RowHeight, Color.clear);
                UIBuildUtil.CreateText(moreRow.transform, "Label", Vector2.zero, Vector2.one,
                    $"+{items.Count - MaxRowsShown} more not shown", 12, new Color(0.7f, 0.7f, 0.7f), TextAlignmentOptions.Left);
            }

            if (_corpse.Ru <= 0 && items.Count == 0 && _corpse.Inventory.AmmoReserve.All(kv => kv.Value <= 0))
            {
                var emptyRow = UIBuildUtil.CreateRow(_list, "Empty", RowHeight, Color.clear);
                UIBuildUtil.CreateText(emptyRow.transform, "Label", Vector2.zero, Vector2.one,
                    "Nothing left to loot.", 14, new Color(0.7f, 0.7f, 0.7f), TextAlignmentOptions.Center);
            }
        }
    }
}
