// New: ShopSystem.cs (BuyItem/ModifyAmmo) already existed, but nothing ever opened it - no
// in-zone way to actually trade at Scavenger's Hub, the one Safe Zone. Opens on B while standing
// in a zone with ZoneDef.IsSafeZone true (the only existing "friendly/tradeable" flag on zone
// data - ponytail: this means "Safe Zone = has a shop" rather than a separate per-outpost shop
// flag; add one if you want vendors that aren't Safe Zones). Built at runtime under the existing
// Canvas, same convention as RuntimeGameLog/DeathScreen - no scene wiring required. Added
// automatically by GameBootstrap.
using System.Linq;
using GameData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore
{
    public class ShopUI : MonoBehaviour
    {
        // ponytail: fixed-height list, no ScrollRect - both lists are capped instead (see
        // MaxBuyRowsShown/MaxSellRowsShown + the "+N more" rows below). Add a real ScrollRect if
        // you want every tier-appropriate item visible at once instead of a capped preview.
        private const int MaxBuyRowsShown = 20;
        private const int MaxSellRowsShown = 12;
        private const float RowHeight = 30f;

        private static ShopUI _instance;

        private GameObject _panel;
        private Transform _buyList;
        private Transform _sellList;
        private TextMeshProUGUI _ruText;
        private bool _open;
        // New: which zone's trader is currently open, so Rebuild() can cap the buy list to what
        // that outpost actually stocks (see the Rebuild() filter below).
        private ZoneDef _currentZone;
        // New: set when the open trader is a named Quartermaster (QuartermasterDatabase) rather
        // than a blanket Safe Zone bazaar - null for both the old Safe Zone case and a Wandering
        // Trader, which Rebuild() tells apart via MacroState.Current.WanderingTraderZoneId instead.
        private QuartermasterDef _currentQuartermaster;

        private void Awake() => _instance = this;

        /// <summary>Called from PlayerController's B keybind - static so PlayerController doesn't
        /// need to hold a reference to the component GameBootstrap added.</summary>
        public static void Toggle() => _instance?.ToggleInternal();

        /// <summary>New: used to gate on zone.IsSafeZone alone (one flag meaning both "no hostile
        /// spawns" and "has a shop"). Named Quartermasters (QuartermasterDatabase) and Wandering
        /// Traders (MacroState.Current.WanderingTraderZoneId) are separate, reputation/roll-gated
        /// trade sources layered on top - IsSafeZone still governs combat spawning by itself
        /// (unchanged), it just isn't the only way into a shop anymore.</summary>
        private void ToggleInternal()
        {
            if (_open) { Close(); return; }

            var zone = MacroMapDatabase.Find(MacroState.Current.CurrentZoneId);
            if (zone == null)
            {
                GameLog.Write("No shop here.");
                return;
            }

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
                _currentZone = zone;
                _currentQuartermaster = qm;
                Open();
                return;
            }

            if (MacroState.Current.WanderingTraderZoneId == zone.Id || zone.IsSafeZone)
            {
                _currentZone = zone;
                _currentQuartermaster = null;
                Open();
                return;
            }

            GameLog.Write("No shop here.");
        }

        private void Open()
        {
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
            GameState.Current.ModalOpen = false;
            if (_panel != null) _panel.SetActive(false);
        }

        private void BuildPanelIfNeeded()
        {
            if (_panel != null) return;

            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("ShopUI: no Canvas found in the scene - can't show the shop panel.");
                return;
            }

            _panel = UIBuildUtil.CreatePanel(canvas.transform, "ShopPanel", new Vector2(0.15f, 0.1f), new Vector2(0.85f, 0.9f),
                new Color(0.08f, 0.08f, 0.08f, 0.95f));

            _ruText = UIBuildUtil.CreateText(_panel.transform, "RuText", new Vector2(0.02f, 0.92f), new Vector2(0.4f, 0.99f),
                "", 20, Color.white, TextAlignmentOptions.Left);
            UIBuildUtil.CreateButton(_panel.transform, "CloseButton", new Vector2(0.86f, 0.92f), new Vector2(0.98f, 0.99f),
                "Close", Close);
            UIBuildUtil.CreateText(_panel.transform, "BuyHeader", new Vector2(0.02f, 0.85f), new Vector2(0.48f, 0.91f),
                "BUY", 18, Color.white, TextAlignmentOptions.Left);
            UIBuildUtil.CreateText(_panel.transform, "SellHeader", new Vector2(0.52f, 0.85f), new Vector2(0.98f, 0.91f),
                "SELL", 18, Color.white, TextAlignmentOptions.Left);

            _buyList = CreateList(_panel.transform, "BuyList", new Vector2(0.02f, 0.02f), new Vector2(0.48f, 0.84f));
            _sellList = CreateList(_panel.transform, "SellList", new Vector2(0.52f, 0.02f), new Vector2(0.98f, 0.84f));
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

        /// <summary>Batch 69: Wanderer traders can send someone with you (Companion).</summary>
        private void AddHireRow(Player player)
        {
            var row = UIBuildUtil.CreateRow(_buyList, "Hire", RowHeight, new Color(0.45f, 1f, 0.55f, 0.10f));
            bool has = Companion.Alive;
            string label = has ? $"{Companion.Current.Name} walks with you ({Companion.Current.Hp} HP)" : $"Hire a Wanderer to walk with you - {Companion.HireCost} RU";
            UIBuildUtil.CreateText(row.transform, "Label", new Vector2(0.02f, 0f), new Vector2(0.68f, 1f), label, 14, Color.white, TextAlignmentOptions.Left);
            UIBuildUtil.CreateButton(row.transform, "HireBtn", new Vector2(0.7f, 0.1f), new Vector2(0.98f, 0.9f), has ? "Dismiss" : "Hire",
                () => { if (has) Companion.Dismiss(); else Companion.Hire(player); Rebuild(); });
        }

        private void AddRepairRow(string label, System.Action repair)
        {
            var row = UIBuildUtil.CreateRow(_sellList, "Repair", RowHeight, new Color(0.3f, 0.6f, 0.9f, 0.10f));
            UIBuildUtil.CreateText(row.transform, "Label", new Vector2(0.02f, 0f), new Vector2(0.68f, 1f), label, 13, Color.white, TextAlignmentOptions.Left);
            UIBuildUtil.CreateButton(row.transform, "RepairBtn", new Vector2(0.7f, 0.1f), new Vector2(0.98f, 0.9f), "Repair", () => { repair(); Rebuild(); });
        }

        /// <summary>Rebuilds both lists from scratch - simplest correct way to keep Ru/quantities in
        /// sync after every buy/sell, and cheap enough at this list size (rebuild-on-action, not
        /// every frame, unlike TacticalGridView's per-frame marker sync which has to track
        /// mid-combat movement).</summary>
        private void Rebuild()
        {
            var player = Player.Current;
            int traderTier;
            string traderName;
            if (_currentQuartermaster != null)
            {
                traderTier = _currentQuartermaster.MaxTier;
                traderName = _currentQuartermaster.Name;
            }
            else if (_currentZone != null && MacroState.Current.WanderingTraderZoneId == _currentZone.Id)
            {
                traderTier = MacroState.Current.WanderingTraderTier;
                traderName = "Wandering Trader";
            }
            else
            {
                traderTier = _currentZone?.Tier ?? 1;
                traderName = _currentZone?.Name ?? "Trader";
            }
            _ruText.text = $"{traderName} (Tier {traderTier}) — RU: {player.Ru}";

            foreach (Transform child in _buyList) Destroy(child.gameObject);
            if (_currentQuartermaster?.Faction == "Wanderers") AddHireRow(player);   // Batch 69
            // New: every weapon/armor piece used to be buyable at the one and only shop regardless
            // of its own tier - a Tier 1 outpost selling Tier 5 rifles on day one, which is why
            // tiers/traders existed in ZoneDef in the first place but were never actually enforced
            // anywhere. A trader now only stocks weapons/armor at or below its own zone's tier;
            // untiered items (consumables/ammo/backpacks) are assumed to be sold everywhere.
            // ~90 total items across ShopDatabase means even a tier-capped list can badly overflow
            // this fixed-height panel - capped the same way the sell list already was.
            // File 05: Layer 1 is the fixed baseline every trader carries; weapons, armor, artifacts
            // and the rest are this trader's rolled stock below (file 07).
            var stock = ShopDatabase.Items.Where(i => i.Baseline).ToList();
            foreach (var item in stock.Take(MaxBuyRowsShown))
            {
                var row = UIBuildUtil.CreateRow(_buyList, $"Buy_{item.Id}", RowHeight, new Color(1f, 1f, 1f, 0.05f));
                string label = item.Type == "ammo" ? AmmoLabel(item, player) : item.Name;
                UIBuildUtil.CreateText(row.transform, "Label", new Vector2(0.02f, 0f), new Vector2(0.68f, 1f),
                    $"{label} - {ShopSystem.GetBuyPrice(item)} RU", 14, Color.white, TextAlignmentOptions.Left);
                UIBuildUtil.CreateButton(row.transform, "BuyBtn", new Vector2(0.7f, 0.1f), new Vector2(0.98f, 0.9f),
                    "Buy", () => { ShopSystem.BuyItem(item.Id); Rebuild(); });
            }
            if (stock.Count > MaxBuyRowsShown)
            {
                var moreRow = UIBuildUtil.CreateRow(_buyList, "BuyMore", RowHeight, Color.clear);
                UIBuildUtil.CreateText(moreRow.transform, "Label", Vector2.zero, Vector2.one,
                    $"+{stock.Count - MaxBuyRowsShown} more not shown", 12, new Color(0.7f, 0.7f, 0.7f), TextAlignmentOptions.Left);
            }

            // New: Layer 2 of the two-layer shop system (RolledStockSystem.cs) - a handful of
            // one-off special items rolled per zone-with-a-trader, cached in
            // MacroState.Current.RolledShopStock keyed by zone id. These never make it into
            // ShopDatabase (freshly generated ids - see RolledStockSystem.RollOne), so they buy
            // through ShopSystem.BuyRolledItem rather than the by-id BuyItem the flat catalog above
            // uses, and disappear from the list once bought (one-off, not restocked until the next
            // zone entry re-rolls them).
            var zoneId = _currentZone?.Id;
            if (!string.IsNullOrEmpty(zoneId) && !MacroState.Current.RolledShopStock.ContainsKey(zoneId))
            {
                bool wandering = _currentQuartermaster == null && !_currentZone.IsSafeZone;
                var fresh = Economy.RollTraderStock(traderTier, wandering);
                // file 10: a caravan in town brings 2 extra rolled items
                if (MacroState.Current.CaravanZoneId == zoneId) fresh.AddRange(Economy.RollTraderStock(traderTier, true).Take(2));
                MacroState.Current.RolledShopStock[zoneId] = fresh;
            }
            var special = !string.IsNullOrEmpty(zoneId) && MacroState.Current.RolledShopStock.TryGetValue(zoneId, out var rolled)
                ? rolled : null;
            if (special != null && special.Count > 0)
            {
                var header = UIBuildUtil.CreateRow(_buyList, "SpecialHeader", RowHeight, Color.clear);
                UIBuildUtil.CreateText(header.transform, "Label", Vector2.zero, Vector2.one,
                    $"-- Today's stock (Day {MacroState.Current.Day}) --", 13, new Color(0.9f, 0.75f, 0.3f), TextAlignmentOptions.Left);
                foreach (var item in special)
                {
                    var row = UIBuildUtil.CreateRow(_buyList, $"Special_{item.Id}", RowHeight, new Color(0.9f, 0.75f, 0.3f, 0.08f));
                    UIBuildUtil.CreateText(row.transform, "Label", new Vector2(0.02f, 0f), new Vector2(0.68f, 1f),
                        $"{(item.Type == "ammo" ? AmmoLabel(item, player) : item.Name)} - {ShopSystem.GetBuyPrice(item)} RU", 14, Color.white, TextAlignmentOptions.Left);
                    UIBuildUtil.CreateButton(row.transform, "BuyBtn", new Vector2(0.7f, 0.1f), new Vector2(0.98f, 0.9f),
                        "Buy", () => { ShopSystem.BuyRolledItem(zoneId, item); Rebuild(); });
                }
            }

            foreach (Transform child in _sellList) Destroy(child.gameObject);

            // Technician (Hub Repair Yard / faction quartermasters): repair to Working (file 08 §5).
            if (ShopSystem.TechnicianHere())
            {
                if (player.EquippedWeapon != null && player.EquippedWeapon.Condition != ItemCondition.Working)
                    AddRepairRow($"Repair {player.EquippedWeapon.Name} ({player.EquippedWeapon.Condition})", () => ShopSystem.RepairEquipped(true));
                if (player.EquippedArmor != null && player.EquippedArmorCondition != ItemCondition.Working)
                    AddRepairRow($"Repair {player.EquippedArmor.Name} ({player.EquippedArmorCondition})", () => ShopSystem.RepairEquipped(false));
                foreach (var damaged in player.Inventory.Items.Where(i => (i.Type == "weapon" || i.Type == "armor") && Economy.ConditionSteps(i.Condition) > 0).Take(4).ToList())
                    AddRepairRow($"Repair {damaged.Name} ({damaged.Condition}) - {Economy.RepairCost(damaged)} RU", () => ShopSystem.Repair(damaged));
            }

            // Repair Yard upgrades (file 08 §5): up to 3 per equipped weapon / armor, paid in Mutant Parts + RU.
            if (Upgrades.TechnicianHere())
            {
                foreach (bool forWeapon in new[] { true, false })
                {
                    var eq = ShopSystem.EquippedAsItem(forWeapon);
                    if (eq == null) continue;
                    var have = forWeapon ? player.EquippedWeapon.Upgrades : player.EquippedArmorUpgrades;
                    if (have.Count >= 3) continue;
                    foreach (var kv in forWeapon ? Upgrades.Weapon : Upgrades.Armor)
                    {
                        if (have.Contains(kv.Key)) continue;
                        if (forWeapon && kv.Key == "Extended Mag" && CombatRules.IsMelee(player.EquippedWeapon)) continue;
                        string up = kv.Key;
                        var row = UIBuildUtil.CreateRow(_sellList, "Upgrade", RowHeight, new Color(0.9f, 0.6f, 0.2f, 0.10f));
                        UIBuildUtil.CreateText(row.transform, "Label", new Vector2(0.02f, 0f), new Vector2(0.68f, 1f),
                            $"{eq.Name}: {up} ({kv.Value}) - {Upgrades.CostText(player, eq, have.Count + 1)}", 12, Color.white, TextAlignmentOptions.Left);
                        UIBuildUtil.CreateButton(row.transform, "UpgradeBtn", new Vector2(0.7f, 0.1f), new Vector2(0.98f, 0.9f),
                            "Upgrade", () => { Upgrades.Buy(player, forWeapon, up); Rebuild(); });
                    }
                }
            }

            var items = player.Inventory.Items;
            foreach (var item in items.Take(MaxSellRowsShown))
            {
                // New: same condition-suffix convention CorpseLootUI/InventoryUI already use -
                // Working is the implied default (no suffix), Dirty/Damaged shown so a sell price
                // that's taken a condition cut (see ShopSystem.GetSellValuePerUnit) isn't a mystery.
                string condSuffix = (item.Type == "weapon" || item.Type == "armor") && !string.IsNullOrEmpty(item.Condition) && item.Condition != ItemCondition.Working
                    ? $" ({item.Condition})" : "";
                var row = UIBuildUtil.CreateRow(_sellList, $"Sell_{item.Id}", RowHeight, new Color(1f, 1f, 1f, 0.05f));
                UIBuildUtil.CreateText(row.transform, "Label", new Vector2(0.02f, 0f), new Vector2(0.68f, 1f),
                    $"{item.Name}{condSuffix} x{item.Qty} - {ShopSystem.GetSellValuePerUnit(item)} RU", 14, Color.white, TextAlignmentOptions.Left);
                UIBuildUtil.CreateButton(row.transform, "SellBtn", new Vector2(0.7f, 0.1f), new Vector2(0.98f, 0.9f),
                    "Sell", () => { ShopSystem.SellItem(item.Id, 1); Rebuild(); });
            }
            if (items.Count > MaxSellRowsShown)
            {
                var moreRow = UIBuildUtil.CreateRow(_sellList, "SellMore", RowHeight, Color.clear);
                UIBuildUtil.CreateText(moreRow.transform, "Label", Vector2.zero, Vector2.one,
                    $"+{items.Count - MaxSellRowsShown} more not shown", 12, new Color(0.7f, 0.7f, 0.7f), TextAlignmentOptions.Left);
            }
        }

    /// <summary>Batch 80: "T3 rifle rounds (Standard) x30", and a warning when your gun takes another tier.</summary>
    static string AmmoLabel(ShopItemDef item, Player player)
    {
        string key = ShopSystem.AmmoKeyHere(item.AmmoType);
        if (key == null) return $"{item.Name} (draw a gun first)";
        string mine = Ammo.Key(player.EquippedWeapon.Db, item.AmmoType);
        return $"{Ammo.Label(key)} x{item.Rounds ?? ShopSystem.RoundsPerBox(player.EquippedWeapon)}" + (key != mine ? $" <color=#e8c547>- your gun takes {Ammo.Label(mine).Split(' ')[0]}</color>" : "");
    }
    }
}
