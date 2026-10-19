// New: minimal exploration screen. Shows the current zone and a button per
// reachable destination (connected zones, plus any discovered hidden ones); clicking travels
// there via the already-ported ZoneTravel.TravelToSector — which itself decides whether you land
// safely or a tactical encounter starts (see ZoneTravel.cs / TriggerZoneEncounter). This view just
// exposes that existing logic, it doesn't reimplement any of it. Built at runtime under the scene's
// Canvas, no scene editing. Added automatically by GameBootstrap; visible only while
// GameState.Current.Mode is Overworld.
using System.Collections.Generic;
using GameData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore
{
    public class OverworldMapView : MonoBehaviour
    {
        private RectTransform _panel;
        private TextMeshProUGUI _zoneInfoText;
        private readonly List<GameObject> _buttons = new List<GameObject>();
        private string _lastZoneId;

        private void Awake()
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("OverworldMapView: no Canvas found in the scene - exploration screen disabled.");
                enabled = false;
                return;
            }

            var panelGo = new GameObject("OverworldPanel", typeof(RectTransform));
            panelGo.transform.SetParent(canvas.transform, false);
            _panel = panelGo.GetComponent<RectTransform>();
            _panel.anchorMin = new Vector2(0.5f, 0.5f);
            _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.pivot = new Vector2(0.5f, 0.5f);
            _panel.sizeDelta = new Vector2(420, 400);
            _panel.anchoredPosition = Vector2.zero;
            panelGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            var infoGo = new GameObject("ZoneInfo", typeof(RectTransform));
            infoGo.transform.SetParent(_panel, false);
            var infoRect = infoGo.GetComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0f, 1f);
            infoRect.anchorMax = new Vector2(1f, 1f);
            infoRect.pivot = new Vector2(0.5f, 1f);
            infoRect.sizeDelta = new Vector2(-16, 70);
            infoRect.anchoredPosition = new Vector2(0, -8);
            _zoneInfoText = infoGo.AddComponent<TextMeshProUGUI>();
            _zoneInfoText.fontSize = 18;
            _zoneInfoText.color = Color.white;
            _zoneInfoText.alignment = TextAlignmentOptions.TopLeft;

            // Search Area (ZoneTravel.SearchArea — already-ported hidden-route discovery) anchored
            // to the panel's bottom so it doesn't need repositioning as the destination list grows.
            var searchGo = new GameObject("SearchAreaButton", typeof(RectTransform));
            searchGo.transform.SetParent(_panel, false);
            var searchRect = searchGo.GetComponent<RectTransform>();
            searchRect.anchorMin = new Vector2(0f, 0f);
            searchRect.anchorMax = new Vector2(1f, 0f);
            searchRect.pivot = new Vector2(0.5f, 0f);
            searchRect.sizeDelta = new Vector2(-32, 36);
            searchRect.anchoredPosition = new Vector2(0, 8);
            var searchImage = searchGo.AddComponent<Image>();
            searchImage.color = new Color(0.25f, 0.22f, 0.1f, 0.9f);
            var searchButton = searchGo.AddComponent<Button>();
            searchButton.targetGraphic = searchImage;
            searchButton.onClick.AddListener(() =>
            {
                ZoneTravel.SearchArea();
                Rebuild(MacroState.Current.CurrentZoneId); // pick up any newly discovered hidden route
            });

            var searchTextGo = new GameObject("Label", typeof(RectTransform));
            searchTextGo.transform.SetParent(searchGo.transform, false);
            var searchTextRect = searchTextGo.GetComponent<RectTransform>();
            searchTextRect.anchorMin = Vector2.zero;
            searchTextRect.anchorMax = Vector2.one;
            searchTextRect.offsetMin = new Vector2(8, 2);
            searchTextRect.offsetMax = new Vector2(-8, -2);
            var searchText = searchTextGo.AddComponent<TextMeshProUGUI>();
            searchText.text = "Search Area (find hidden routes)";
            searchText.fontSize = 15;
            searchText.color = Color.white;
            searchText.alignment = TextAlignmentOptions.Midline;
        }

        private void Update()
        {
            bool shouldShow = GameState.Current.Mode == GameMode.Overworld;
            if (_panel.gameObject.activeSelf != shouldShow)
                _panel.gameObject.SetActive(shouldShow);
            if (!shouldShow) return;

            string zoneId = MacroState.Current.CurrentZoneId;
            if (zoneId != _lastZoneId)
            {
                Rebuild(zoneId);
                _lastZoneId = zoneId;
            }
        }

        private void Rebuild(string zoneId)
        {
            foreach (var b in _buttons) Destroy(b);
            _buttons.Clear();

            var zone = MacroMapDatabase.Find(zoneId);
            if (zone == null)
            {
                _zoneInfoText.text = $"Unknown zone: {zoneId}";
                return;
            }

            _zoneInfoText.text = $"{zone.Name}  (Tier {zone.Tier}{(zone.IsSafeZone ? ", Safe" : "")})\nSelect a destination:";

            var destinations = new List<string>(zone.ConnectedZones ?? new List<string>());
            if (zone.HiddenConnections != null)
                foreach (var hidden in zone.HiddenConnections)
                    if (MacroState.Current.DiscoveredHiddenNodes.Contains(hidden))
                        destinations.Add(hidden);

            float y = -88;
            foreach (var destId in destinations)
            {
                var destZone = MacroMapDatabase.Find(destId);
                string label = destZone != null ? $"{destZone.Name} (T{destZone.Tier})" : destId;
                _buttons.Add(CreateZoneButton(label, destId, y));
                y -= 44;
            }

            if (destinations.Count == 0)
            {
                var deadEnd = new GameObject("NoDestinations", typeof(RectTransform));
                deadEnd.transform.SetParent(_panel, false);
                var rect = deadEnd.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(-16, 30);
                rect.anchoredPosition = new Vector2(0, y);
                var t = deadEnd.AddComponent<TextMeshProUGUI>();
                t.text = "No known routes from here.";
                t.fontSize = 16;
                t.color = Color.gray;
                _buttons.Add(deadEnd);
            }
        }

        private GameObject CreateZoneButton(string label, string destinationZoneId, float yOffset)
        {
            var go = new GameObject($"Zone_{destinationZoneId}", typeof(RectTransform));
            go.transform.SetParent(_panel, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(-32, 36);
            rect.anchoredPosition = new Vector2(0, yOffset);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.25f, 0.9f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => ZoneTravel.TravelToSector(destinationZoneId));

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8, 2);
            textRect.offsetMax = new Vector2(-8, -2);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.MidlineLeft;

            return go;
        }
    }
}
