using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The small row at the bottom of the playfield: an icon per attack, with a wipe
// and timer for its cooldown and a flash when it fires, plus the XP bar above
// them. Built procedurally like DpsMeterUI, since the icons are placeholders.
// Attach under GameplayCanvas so it stays inside the playfield. Never blocks
// clicks. Hidden when no human is playing Attacker.
public class CooldownBarUI : MonoBehaviour
{
    public AttackerProgression progression;

    [Header("Layout")]
    public float iconSize = 40f;
    public float iconSpacing = 6f;
    public float bottomMargin = 8f;
    public Vector2 xpBarSize = new Vector2(140f, 6f);

    [Header("Colors")]
    public Color attackColor = new Color32(0xFF, 0x8A, 0x3D, 0xB0);
    public Color critColor = new Color(1f, 0.15f, 0.15f, 0.7f);
    public Color wipeColor = new Color(0f, 0f, 0f, 0.65f);
    public Color flashColor = new Color(1f, 1f, 1f, 0.9f);
    public Color xpTrackColor = new Color(1f, 1f, 1f, 0.15f);
    public Color xpFillColor = new Color(0.45f, 0.85f, 1f, 0.9f);
    public float flashSeconds = 0.12f;

    private class Slot
    {
        public GameObject root;
        public Image icon;
        public Color baseColor;
        public RectTransform wipe;
        public TextMeshProUGUI label;
        public TextMeshProUGUI timer;
        public int shownTenths = -1;
    }

    private Slot attackSlot;
    private Slot critSlot;
    private RectTransform xpFill;
    private TextMeshProUGUI levelText;
    private int shownXp = -1;
    private int shownLevel = -1;
    private int shownShots;

    void Start()
    {
        if (progression == null || progression.Ship == null)
        {
            gameObject.SetActive(false);
            return;
        }
        Build();
    }

    void Update()
    {
        PlayerController ship = progression.Ship;
        Refresh(attackSlot, ship.AttackCooldownRemaining, ship.FireInterval, ship.LastAttackTime);
        RefreshAttackLabel(ship.shotsPerAttack);

        bool critUnlocked = progression.Upgrades.CritUnlocked;
        if (critUnlocked != critSlot.root.activeSelf)
        {
            critSlot.root.SetActive(critUnlocked);
            float offset = critUnlocked ? (iconSize + iconSpacing) * 0.5f : 0f;
            attackSlot.root.GetComponent<RectTransform>().anchoredPosition = new Vector2(-offset, 0f);
            critSlot.root.GetComponent<RectTransform>().anchoredPosition = new Vector2(offset, 0f);
        }
        if (critUnlocked) Refresh(critSlot, progression.CritCooldownRemaining, progression.critInterval, progression.LastCritTime);

        RefreshXp();
    }

    private void Refresh(Slot slot, float remaining, float duration, float lastFired)
    {
        slot.wipe.anchorMax = new Vector2(1f, Mathf.Clamp01(remaining / duration));
        slot.icon.color = Time.time - lastFired < flashSeconds ? flashColor : slot.baseColor;

        // Text only rebuilds when the shown value changes - see DpsMeterUI's refreshInterval.
        int tenths = Mathf.CeilToInt(remaining * 10f);
        if (tenths == slot.shownTenths) return;
        slot.shownTenths = tenths;
        slot.timer.text = tenths > 0 ? (tenths / 10f).ToString("0.0") : "";
    }

    private void RefreshAttackLabel(int shots)
    {
        if (shots == shownShots) return;
        shownShots = shots;
        attackSlot.label.text = shots > 1 ? "A x" + shots : "A";
    }

    private void RefreshXp()
    {
        AttackerXpTrack xp = progression.Xp;
        if (xp.XpIntoLevel == shownXp && xp.Level == shownLevel) return;
        shownXp = xp.XpIntoLevel;
        shownLevel = xp.Level;
        xpFill.anchorMax = new Vector2(xp.IsMaxed ? 1f : (float)xp.XpIntoLevel / xp.XpToNextLevel, 1f);
        levelText.text = xp.IsMaxed ? $"Lv {xp.Level} MAX" : $"Lv {xp.Level}  {xp.XpIntoLevel}/{xp.XpToNextLevel}";
    }

    private void Build()
    {
        RectTransform root = (RectTransform)transform;
        root.anchorMin = new Vector2(0.5f, 0f);
        root.anchorMax = new Vector2(0.5f, 0f);
        root.pivot = new Vector2(0.5f, 0f);
        root.anchoredPosition = new Vector2(0f, bottomMargin);
        root.sizeDelta = new Vector2(xpBarSize.x, iconSize);

        attackSlot = BuildSlot("AttackIcon", attackColor, "A");
        critSlot = BuildSlot("CritIcon", critColor, "C");
        critSlot.root.SetActive(false);
        BuildXpBar();
    }

    private Slot BuildSlot(string name, Color color, string letter)
    {
        Image icon = NewImage(name, transform, color);
        RectTransform rect = icon.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = Vector2.one * iconSize;

        Image wipe = NewImage("Wipe", icon.transform, wipeColor);
        Fill(wipe.rectTransform);

        TextMeshProUGUI label = NewText("Letter", icon.transform, 14f);
        Fill(label.rectTransform);
        label.alignment = TextAlignmentOptions.Top;
        label.text = letter;

        TextMeshProUGUI timer = NewText("Timer", icon.transform, 13f);
        Fill(timer.rectTransform);
        timer.alignment = TextAlignmentOptions.Bottom;

        return new Slot { root = icon.gameObject, icon = icon, baseColor = color, wipe = wipe.rectTransform, label = label, timer = timer };
    }

    private void BuildXpBar()
    {
        Image track = NewImage("XpTrack", transform, xpTrackColor);
        RectTransform trackRect = track.rectTransform;
        trackRect.anchorMin = trackRect.anchorMax = trackRect.pivot = new Vector2(0.5f, 0f);
        trackRect.sizeDelta = xpBarSize;
        trackRect.anchoredPosition = new Vector2(0f, iconSize + iconSpacing);

        Image fill = NewImage("XpFill", track.transform, xpFillColor);
        xpFill = fill.rectTransform;
        Fill(xpFill);
        xpFill.anchorMax = new Vector2(0f, 1f);

        levelText = NewText("Level", track.transform, 13f);
        RectTransform levelRect = levelText.rectTransform;
        levelRect.anchorMin = new Vector2(0f, 1f);
        levelRect.anchorMax = Vector2.one;
        levelRect.pivot = new Vector2(0.5f, 0f);
        levelRect.sizeDelta = new Vector2(0f, 18f);
        levelRect.anchoredPosition = Vector2.zero;
    }

    private static Image NewImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
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

    private static void Fill(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
