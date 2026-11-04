using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The freeze-and-pick screen. Opens whenever AttackerProgression has a pick
// banked, one screen per pick. Built procedurally like DpsMeterUI: placeholder
// layout with nothing to art-direct yet. Attach under HUDCanvas.
public class LevelUpUI : MonoBehaviour
{
    private static readonly UpgradeCard[] Cards = { UpgradeCard.AttackDamage, UpgradeCard.CritStrike, UpgradeCard.Multishot };

    public AttackerProgression progression;
    // Same guards as PauseUI: never open on top of an end screen.
    public GameObject gameOverPanelRoot;
    public GameObject victoryPanelRoot;

    [Header("Layout")]
    public Vector2 cardSize = new Vector2(210f, 280f);
    public float cardSpacing = 16f;
    // Stops a key or click already on its way when the screen opens from
    // picking a card the player never read.
    public float pickLockSeconds = 0.3f;

    [Header("Colors")]
    public Color dimColor = new Color(0f, 0f, 0f, 0.6f);
    public Color cardColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
    public Color cardFocusColor = new Color(0.25f, 0.25f, 0.38f, 1f);

    public bool IsOpen { get; private set; }

    private GameObject panel;
    private Button[] buttons;
    private TextMeshProUGUI[] cardTexts;
    private InputAction[] pickKeys;
    private float pickableAt;

    void Awake()
    {
        BuildPanel();
        panel.SetActive(false);
        pickKeys = new InputAction[Cards.Length];
        for (int i = 0; i < Cards.Length; i++)
        {
            int index = i;
            pickKeys[i] = new InputAction("Pick" + (i + 1), InputActionType.Button, "<Keyboard>/" + (i + 1));
            pickKeys[i].performed += _ => Pick(index);
        }
    }

    void OnDisable()
    {
        foreach (InputAction key in pickKeys) key.Disable();
    }

    void Update()
    {
        if (!IsOpen && ShouldOpen()) Open();
    }

    private bool ShouldOpen()
    {
        if (progression == null || progression.PendingPicks <= 0) return false;
        if (!progression.Ship.gameObject.activeInHierarchy) return false;
        if (Time.timeScale == 0f) return false; // paused - wait for Resume
        if (gameOverPanelRoot != null && gameOverPanelRoot.activeSelf) return false;
        return victoryPanelRoot == null || !victoryPanelRoot.activeSelf;
    }

    private void Open()
    {
        IsOpen = true;
        Time.timeScale = 0f;
        pickableAt = Time.unscaledTime + pickLockSeconds;
        for (int i = 0; i < Cards.Length; i++) cardTexts[i].text = Describe(Cards[i], i);
        panel.SetActive(true);
        foreach (InputAction key in pickKeys) key.Enable();
        // Gives keyboard/gamepad a starting card to navigate from.
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);
    }

    private void Pick(int index)
    {
        if (!IsOpen || Time.unscaledTime < pickableAt) return;
        progression.ApplyCard(Cards[index]);
        foreach (InputAction key in pickKeys) key.Disable();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        panel.SetActive(false);
        Time.timeScale = 1f;
        IsOpen = false;
    }

    private string Describe(UpgradeCard card, int index)
    {
        AttackerUpgrades upgrades = progression.Upgrades;
        string body;
        switch (card)
        {
            case UpgradeCard.AttackDamage:
                float damage = progression.Ship.CurrentDamage;
                body = $"<b>Attack Damage</b>\n\n+1 damage per shot\n\nDamage {damage:0} to {damage + 1f:0}";
                break;
            case UpgradeCard.CritStrike:
                body = upgrades.CritUnlocked
                    ? $"<b>Crit Strike</b>\n\n+100% crit damage\n\nBonus +{upgrades.CritBonusFraction * 100f:0}% to +{(upgrades.CritBonusFraction + 1f) * 100f:0}%"
                    : $"<b>Crit Strike</b>\n\nFires a red shot every {progression.critInterval:0} seconds for +100% damage";
                break;
            default:
                body = upgrades.ShotCount == 1
                    ? "<b>Multishot</b>\n\nYour attack fires 3 shots in a cone"
                    : $"<b>Multishot</b>\n\n+2 shots\n\nShots {upgrades.ShotCount} to {upgrades.ShotCount + 2}";
                break;
        }
        return $"{body}\n\n<size=70%>[{index + 1}]</size>";
    }

    private void BuildPanel()
    {
        panel = new GameObject("LevelUpPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        panel.GetComponent<Image>().color = dimColor;
        Stretch(panel.GetComponent<RectTransform>());

        TextMeshProUGUI title = NewText("Title", panel.transform, 30f);
        title.text = "<b>LEVEL UP</b>";
        RectTransform titleRect = title.rectTransform;
        titleRect.sizeDelta = new Vector2(600f, 50f);
        titleRect.anchoredPosition = new Vector2(0f, cardSize.y * 0.5f + 40f);

        buttons = new Button[Cards.Length];
        cardTexts = new TextMeshProUGUI[Cards.Length];
        float step = cardSize.x + cardSpacing;
        float firstX = -step * (Cards.Length - 1) * 0.5f;
        for (int i = 0; i < Cards.Length; i++) BuildCard(i, firstX + step * i);
    }

    // Listener added at runtime because the buttons themselves only exist at runtime.
    private void BuildCard(int index, float x)
    {
        GameObject card = new GameObject("Card" + (index + 1), typeof(RectTransform), typeof(Image), typeof(Button));
        card.transform.SetParent(panel.transform, false);
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.sizeDelta = cardSize;
        rect.anchoredPosition = new Vector2(x, 0f);

        Button button = card.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = cardColor;
        colors.highlightedColor = cardFocusColor;
        colors.selectedColor = cardFocusColor;
        colors.pressedColor = cardFocusColor;
        colors.fadeDuration = 0f;
        button.colors = colors;
        button.onClick.AddListener(() => Pick(index));
        buttons[index] = button;

        TextMeshProUGUI text = NewText("Text", card.transform, 22f);
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(12f, 12f);
        text.rectTransform.offsetMax = new Vector2(-12f, -12f);
        cardTexts[index] = text;
    }

    private static TextMeshProUGUI NewText(string name, Transform parent, float size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
