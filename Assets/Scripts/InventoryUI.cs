using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameCore;

public class InventoryUI : MonoBehaviour
{
    // ponytail: fixed VerticalLayoutGroup list, no ScrollRect/row cap - matches ShopUI's existing
    // approach and is fine for the item counts this game produces. Add a ScrollRect if the
    // inventory regularly grows past what fits in the panel.
    private const float RowHeight = 26f;

    [SerializeField] private TextMeshProUGUI equipmentStatusText;
    private Transform _itemList;
    private CanvasGroup _group;
    private string _lastSignature;

    private void Awake()
    {
        // Legacy scene-wired field - same fix as UIManager: self-builds now instead of relying on
        // an Inspector-assigned Text object with no fixed layout.
        if (equipmentStatusText != null && equipmentStatusText.gameObject != gameObject) Destroy(equipmentStatusText.gameObject);

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        var equipPanel = UIBuildUtil.CreatePanel(canvas.transform, "EquipmentPanel", new Vector2(0.8f, 0.8f), new Vector2(1f, 1f),
            new Color(0f, 0f, 0f, 0.6f));
        equipPanel.AddComponent<RectMask2D>();
        equipmentStatusText = UIBuildUtil.CreateText(equipPanel.transform, "EquipmentText", Vector2.zero, Vector2.one,
            "", 11, Color.white, TextAlignmentOptions.TopLeft);
        equipPanel.GetComponent<Image>().raycastTarget = false;   // HUD: clicks reach the map
        equipmentStatusText.raycastTarget = false;

        var invPanel = UIBuildUtil.CreatePanel(canvas.transform, "InventoryPanel", new Vector2(0.8f, 0.46f), new Vector2(1f, 0.78f),
            new Color(0f, 0f, 0f, 0.6f));
        invPanel.AddComponent<RectMask2D>();
        invPanel.GetComponent<Image>().raycastTarget = false;     // only the row buttons catch clicks
        _group = invPanel.AddComponent<CanvasGroup>();
        _itemList = CreateList(invPanel.transform);
    }

    private static Transform CreateList(Transform parent)
    {
        var go = new GameObject("ItemList", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = 2;
        return go.transform;
    }

    void Update()
    {
        if (Player.Current == null) return;
        var p = Player.Current;
        if (_group != null) _group.blocksRaycasts = !GameState.Current.ModalOpen;   // no Use/Drop under the shop

        string weaponName = p.EquippedWeapon != null ? p.EquippedWeapon.Db.Name : "Nenhuma";
        string armorName = p.EquippedArmor != null ? p.EquippedArmor.Name : "Nenhuma";
        // New (Batch 8): detectors previously had no equip UI at all, so there was nothing to show here.
        string detectorName = p.ActiveDetector is GameData.DetectorDef det ? det.Name : "Nenhum";
        float totalWeight = p.CalculateCarryWeight();

        if (equipmentStatusText != null)
        {
            equipmentStatusText.text = $"--- EQUIPAMENTOS ---\n" +
                                       $"Arma: {weaponName}{(p.OffhandWeapon != null ? " + " + p.OffhandWeapon.Name : "")}\n" +
                                       $"Armadura: {armorName}\n" +
                                       $"Detector: {detectorName}\n" +
                                       $"Peso: {totalWeight:F1} kg\n\n" +
                                       $"--- ARTEFATOS ({System.Math.Max(1, p.TotalBeltSlots)}) ---\n" +
                                       string.Join("\n", Enumerable.Range(0, System.Math.Min(p.ArtifactSlots.Length, System.Math.Max(1, p.TotalBeltSlots)))
                                           .Select(i => $"[{i + 1}]: {(p.ArtifactSlots[i] != null && GameData.ArtifactDatabase.All.TryGetValue(p.ArtifactSlots[i], out var a) ? a.Name : p.ArtifactSlots[i] ?? "Vazio")}"));
        }

        // Rebuild the row list (Equip/Use/Drop buttons per item) only when its content actually
        // changed, instead of destroying/recreating N GameObjects every single Update() frame.
        string signature = string.Join("|", p.Inventory.Items.Select(i => $"{i.Id}:{i.Qty}:{i.Condition}")) + $"#{weaponName}#{armorName}#{p.OffhandWeapon?.Name}#{CombatantUtil.HasPerk(p, "Gunslinger")}";
        if (signature == _lastSignature) return;
        _lastSignature = signature;
        RebuildList(p);
    }

    private void RebuildList(Player p)
    {
        if (_itemList == null) return;
        foreach (Transform child in _itemList) Destroy(child.gameObject);

        if (p.Inventory.Items.Count == 0)
        {
            var emptyRow = UIBuildUtil.CreateRow(_itemList, "EmptyRow", RowHeight, Color.clear);
            UIBuildUtil.CreateText(emptyRow.transform, "Label", Vector2.zero, Vector2.one,
                "Inventário vazio.", 11, Color.white, TextAlignmentOptions.Left);
            return;
        }

        foreach (var item in p.Inventory.Items)
        {
            var row = UIBuildUtil.CreateRow(_itemList, $"Item_{item.Id}", RowHeight, new Color(1f, 1f, 1f, 0.05f));
            // New (Batch 5): show weapon/armor condition here too, not just in the shop's sell list.
            string condSuffix = (item.Type == "weapon" || item.Type == "armor") && !string.IsNullOrEmpty(item.Condition) && item.Condition != ItemCondition.Working
                ? $" ({item.Condition})" : "";
            UIBuildUtil.CreateText(row.transform, "Label", new Vector2(0.02f, 0f), new Vector2(0.55f, 1f),
                $"{item.Name}{condSuffix} x{item.Qty}", 10, Color.white, TextAlignmentOptions.Left);

            string itemId = item.Id;
            switch (item.Type)
            {
                case "weapon":
                    bool offhand = item.Category == "sidearm" && CombatantUtil.HasPerk(p, "Gunslinger") && CombatRules.Category(p.EquippedWeapon?.Db) == "sidearm";
                    UIBuildUtil.CreateButton(row.transform, "ActionBtn", new Vector2(0.57f, 0.1f), new Vector2(0.67f, 0.9f),
                        "Equip", () => Player.Current.EquipWeaponFromInventory(itemId));
                    UIBuildUtil.CreateButton(row.transform, "SecondBtn", new Vector2(0.68f, 0.1f), new Vector2(0.78f, 0.9f),
                        "Second", () => Player.Current.EquipSecondaryFromInventory(itemId));   // Batch 80: the second weapon slot (Y swaps)
                    if (offhand)
                        UIBuildUtil.CreateButton(row.transform, "OffBtn", new Vector2(0.79f, 0.1f), new Vector2(0.89f, 0.9f),
                            "Off-hand", () => Player.Current.EquipOffhandFromInventory(itemId));
                    break;
                case "armor":
                    UIBuildUtil.CreateButton(row.transform, "ActionBtn", new Vector2(0.57f, 0.1f), new Vector2(0.78f, 0.9f),
                        "Equip", () => Player.Current.EquipArmorFromInventory(itemId));
                    break;
                case "consumable":
                    UIBuildUtil.CreateButton(row.transform, "ActionBtn", new Vector2(0.57f, 0.1f), new Vector2(0.78f, 0.9f),
                        "Use", () => Player.Current.UseConsumable(itemId));
                    break;
                case "detector":
                    UIBuildUtil.CreateButton(row.transform, "ActionBtn", new Vector2(0.57f, 0.1f), new Vector2(0.78f, 0.9f),
                        "Equip", () => Player.Current.EquipDetectorFromInventory(itemId));
                    break;
                case "artifact":
                    UIBuildUtil.CreateButton(row.transform, "ActionBtn", new Vector2(0.57f, 0.1f), new Vector2(0.78f, 0.9f),
                        "Belt", () => Player.Current.EquipArtifactFromInventory(itemId));
                    break;
                case "armorMod":
                    UIBuildUtil.CreateButton(row.transform, "ActionBtn", new Vector2(0.57f, 0.1f), new Vector2(0.78f, 0.9f),
                        "Fit", () => Player.Current.FitArmorMod(itemId));
                    break;
                case "backpack":
                    UIBuildUtil.CreateButton(row.transform, "ActionBtn", new Vector2(0.57f, 0.1f), new Vector2(0.78f, 0.9f),
                        "Wear", () => Player.Current.EquipBackpackFromInventory(itemId));
                    break;
            }

            // New (Batch 12): dropped items land on the ground as a real GroundItem instead of
            // vanishing - see Player.DropItem/TerrainMap.DropItem.
            UIBuildUtil.CreateButton(row.transform, "DropBtn", new Vector2(0.8f, 0.1f), new Vector2(0.98f, 0.9f),
                "Drop", () => Player.Current.DropItem(itemId, 1));
        }
    }
}
