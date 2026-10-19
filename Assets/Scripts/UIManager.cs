using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameCore; // Requisitado para acessar sua engine

public class UIManager : MonoBehaviour
{
    // Legacy scene-wired field: whatever "New Text" object got dragged in here in the Editor had
    // no fixed layout and overlapped the runtime combat log / shop / equipment panels. Now builds
    // its own panel at a fixed screen corner instead, same convention as
    // RuntimeGameLog/DeathScreen/ShopUI (no scene wiring required) - the old field is only read
    // once, to clean up the leftover object if one was assigned.
    [SerializeField] private TextMeshProUGUI statusText;

    private void Awake()
    {
        if (statusText != null && statusText.gameObject != gameObject) Destroy(statusText.gameObject);

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        var panel = UIBuildUtil.CreatePanel(canvas.transform, "StatusPanel", new Vector2(0f, 0.84f), new Vector2(0.26f, 1f),
            new Color(0f, 0f, 0f, 0.6f));
        panel.AddComponent<RectMask2D>();
        statusText = UIBuildUtil.CreateText(panel.transform, "StatusText", Vector2.zero, Vector2.one,
            "", 12, Color.white, TextAlignmentOptions.TopLeft);
        // HUD only - clicks pass through to the map underneath
        panel.GetComponent<Image>().raycastTarget = false;
        statusText.raycastTarget = false;
    }

    void Update()
    {
        // Trava de segurança: impede leitura antes do Bootstrap inicializar a engine
        if (statusText == null || Player.Current == null || MacroState.Current == null) return;

        // Renderiza o estado base da simulação em tempo real
        var p = Player.Current;
        var w = p.EquippedWeapon;
        string weapon = w == null ? "Weapon: none"
            : $"{w.Name}{(w.Condition != ItemCondition.Working ? $" ({w.Condition})" : "")}: " +
              (CombatRules.IsMelee(w) ? "melee" : $"{w.CurrentMagazine}/{w.MagCapacity} {w.CurrentAmmoType}") +
              (w.IsJammed ? " JAMMED" : "") + (CombatRules.CanBurst(w) ? $" [{p.FireMode}]" : "");
        statusText.text = $"ZONA ATUAL: {MacroState.Current.CurrentZoneId}\n" +
                          $"HP: {p.Hp} / {p.MaxHp}  AC: {p.Ac}\n" +
                          $"AP: {p.Ap}/{p.MaxAp} | MP: {p.Mp}/{p.MaxMp}\n" +
                          weapon + "\n" +
                          $"Hunger: {p.Hunger:0.0} | Thirst: {p.Thirst:0.0} | Tiredness: {p.Tiredness:0.0}\n" +
                          $"Status: {p.StatusText}\n" +
                          $"Day {MacroState.Current.Day}" + (MacroState.Current.SurgeRounds > 0 ? $"  <color=#ff5040>SURGE IN {MacroState.Current.SurgeRounds}</color>" : "");
    }
}
