// RT 7y: the networking test's screen - main menu > Co-op: host with your last character (a join code to give friends), or
// type a friend's code and join their game. While connected, a line in the top-right corner says who you are and the code.
// The look follows MainMenuUI's option panels.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore
{
    public class CoopUI : MonoBehaviour
    {
        static CoopUI _instance;
        GameObject _panel, _corner;
        TextMeshProUGUI _status, _cornerText;
        string _code = "";

        void Awake() => _instance = this;

        public static void Open(Transform parent, System.Action hideMenu) => _instance?.Build(parent, hideMenu);

        void Build(Transform parent, System.Action hideMenu)
        {
            if (_panel != null) { Destroy(_panel); _panel = null; return; }   // the menu item toggles it
            _panel = UIBuildUtil.CreatePanel(parent, "CoopPanel", new Vector2(0.34f, 0.25f), new Vector2(0.8f, 0.8f), HudView.Edge);
            var fill = UIBuildUtil.CreatePanel(_panel.transform, "Fill", Vector2.zero, Vector2.one, new Color(0.06f, 0.065f, 0.07f, 0.96f));
            var fr = fill.GetComponent<RectTransform>(); fr.offsetMin = new Vector2(2, 2); fr.offsetMax = new Vector2(-2, -2);
            var t = fill.transform;
            UIBuildUtil.CreateText(t, "Title", new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.96f), "CO-OP (TEST)", 22, HudView.Yellow, TextAlignmentOptions.Left);
            UIBuildUtil.CreateText(t, "Host", new Vector2(0.05f, 0.62f), new Vector2(0.6f, 0.8f),
                "Host: your last character's game, with a code for friends.", 15, HudView.Cream, TextAlignmentOptions.Left);
            var host = UIBuildUtil.CreateButton(t, "HostButton", new Vector2(0.63f, 0.64f), new Vector2(0.95f, 0.78f), "Host", () =>
            {
                var last = SaveSystem.Slots();
                if (last.Count == 0) { Net.Status = "Make a character first (New Game)."; return; }
                if (!SaveSystem.Load(last[0].CharacterId)) return;
                hideMenu(); Close();
                Net.Host();
            });
            host.GetComponent<Image>().color = HudView.Lip;

            UIBuildUtil.CreateText(t, "Join", new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.56f), "Join: type the host's code.", 15, HudView.Cream, TextAlignmentOptions.Left);
            var box = UIBuildUtil.CreatePanel(t, "CodeField", new Vector2(0.05f, 0.26f), new Vector2(0.6f, 0.4f), new Color(0.14f, 0.14f, 0.13f, 1f));
            var area = UIBuildUtil.CreatePanel(box.transform, "TextArea", Vector2.zero, Vector2.one, new Color(0, 0, 0, 0));
            var ar = area.GetComponent<RectTransform>(); ar.offsetMin = new Vector2(10, 4); ar.offsetMax = new Vector2(-10, -4);
            var text = UIBuildUtil.CreateText(area.transform, "Text", Vector2.zero, Vector2.one, "", 20, Color.white, TextAlignmentOptions.Left);
            var hint = UIBuildUtil.CreateText(area.transform, "Placeholder", Vector2.zero, Vector2.one, "CODE", 20, new Color(1f, 1f, 1f, 0.35f), TextAlignmentOptions.Left);
            if (HudView.BodyFont != null) { text.font = HudView.BodyFont; hint.font = HudView.BodyFont; }
            var field = box.AddComponent<TMP_InputField>();
            field.textViewport = ar; field.textComponent = text; field.placeholder = hint; field.characterLimit = 12;
            field.text = _code;
            field.onValueChanged.AddListener(v => _code = v);
            var join = UIBuildUtil.CreateButton(t, "JoinButton", new Vector2(0.63f, 0.26f), new Vector2(0.95f, 0.4f), "Join", () =>
                Net.Join(_code, () => { hideMenu(); Close(); GameBootstrap.StartNewGame(new CharacterSpec(), null, save: false); }));
            join.GetComponent<Image>().color = HudView.Lip;

            _status = UIBuildUtil.CreateText(t, "Status", new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.2f), "", 14, HudView.Dim, TextAlignmentOptions.Left);
        }

        void Close() { if (_panel != null) Destroy(_panel); _panel = null; _status = null; }

        void Update()
        {
            if (_status != null) _status.text = Net.Link == null ? "Networking isn't installed (open the project once so Unity adds the packages)." : Net.Status;
            bool on = Net.Mode != Net.Role.Off && Player.Current != null;
            if (on && _corner == null)
            {
                var canvas = FindAnyObjectByType<Canvas>();
                if (canvas == null) return;
                _corner = UIBuildUtil.CreatePanel(canvas.transform, "CoopCorner", new Vector2(0.72f, 0.955f), new Vector2(0.995f, 0.995f), new Color(0.06f, 0.065f, 0.07f, 0.8f));
                _corner.GetComponent<Image>().raycastTarget = false;
                _cornerText = UIBuildUtil.CreateText(_corner.transform, "Text", Vector2.zero, Vector2.one, "", 14, HudView.Cream, TextAlignmentOptions.Center);
                _cornerText.raycastTarget = false;
            }
            if (_corner != null) _corner.SetActive(on);
            if (on && _cornerText != null) _cornerText.text = (Net.Mode == Net.Role.Host ? $"HOST - code {Net.Code}" : "CLIENT") + $" - {Player.All.Count} player{(Player.All.Count == 1 ? "" : "s")}";
        }
    }
}
