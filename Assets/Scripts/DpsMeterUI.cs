using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A Recount-style damage/DPS meter for the boss fight, shown in the left HUD
// sidebar. Tracks damage dealt to the main boss only - minion and wave-enemy
// damage is deliberately excluded, since the meter exists to compare how much
// each role actually contributes to the encounter that matters.
//
// Built procedurally rather than from a prefab + Inspector wiring (the
// convention every other UI script here follows - see PartyFrameUI.cs). Two
// reasons this one is the exception: the row count is genuinely variable (it
// mirrors however many ships the party ended up with, 1-4), and a meter is
// telemetry rather than authored layout - there's nothing to art-direct. The
// only Inspector reference it needs is the boss itself, exactly like
// BossPanelUI.cs.
//
// Attach to an empty GameObject under HUDCanvas/LeftSidebar and drag the
// scene's Boss into `boss`. Everything else builds itself on Start().
public class DpsMeterUI : MonoBehaviour
{
    public Level1Boss boss; // direct scene reference, same pattern as BossPanelUI.boss

    [Header("Layout")]
    public float titleHeight = 20f;
    public float rowHeight = 20f;
    public float rowSpacing = 3f;
    public int padding = 8;

    [Header("Type")]
    public float titleFontSize = 12f;
    public float rowFontSize = 12f;

    [Header("Refresh")]
    // Recount-ish cadence. Repainting 4 rows of formatted text every frame is
    // pure string garbage for no readability gain, so this throttles it.
    public float refreshInterval = 0.2f;

    [Header("Colors")]
    public Color panelColor = new Color(0.05f, 0.05f, 0.08f, 0.85f); // matches PartyFrame.prefab's root Image
    public Color titleColor = new Color(0.66f, 0.66f, 0.72f);
    public Color barTrackColor = new Color(1f, 1f, 1f, 0.07f);
    public Color rowTextColor = Color.white;
    // Bars are tinted per role (PlayerRoleStats.tintColor) so a row reads as
    // the same ship as its party frame and its sprite. This is the fallback
    // for a row whose ship has no PlayerRoleComponent.
    public Color fallbackBarColor = new Color(0.5f, 0.55f, 0.65f);

    private class Row
    {
        public GameObject root;
        public RectTransform fill;
        public Image fillImage;
        public TextMeshProUGUI label;
        public TextMeshProUGUI value;
        public GameObject ship;
        public float damage;
    }

    private Row[] rows;
    private TextMeshProUGUI titleText;
    private LayoutElement selfLayout;
    private float nextRefreshTime;
    private bool built;
    private bool frozen; // boss destroyed - hold the final numbers, like Recount does after a kill

    void Start()
    {
        if (boss == null)
        {
            Debug.LogWarning("DpsMeterUI: no boss assigned - drag the scene's Boss into the `boss` field. Disabling the meter.", this);
            enabled = false;
            return;
        }

        Build();
        Refresh();
    }

    void Update()
    {
        if (!built || frozen) return;

        // Level1Boss.Die() destroys its GameObject, so this goes null the
        // moment the fight is won. Freeze on the last painted values rather
        // than blanking the meter - the end-of-fight numbers are the whole
        // point of having one.
        if (boss == null)
        {
            frozen = true;
            if (titleText != null) titleText.text = "DAMAGE · BOSS DOWN";
            return;
        }

        if (Time.time < nextRefreshTime) return;
        nextRefreshTime = Time.time + refreshInterval;
        Refresh();
    }

    // ---------------------------------------------------------------- build

    private void Build()
    {
        // The sidebar's own VerticalLayoutGroup positions this panel; these
        // components make it a self-sizing block inside it.
        Image panel = GetComponent<Image>();
        if (panel == null) panel = gameObject.AddComponent<Image>();
        panel.color = panelColor;
        panel.raycastTarget = false; // purely a backdrop - must not eat clicks meant for the party frames above it

        VerticalLayoutGroup layout = GetComponent<VerticalLayoutGroup>();
        if (layout == null) layout = gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(padding, padding, padding, padding);
        layout.spacing = rowSpacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        selfLayout = GetComponent<LayoutElement>();
        if (selfLayout == null) selfLayout = gameObject.AddComponent<LayoutElement>();

        titleText = CreateText("Title", transform, titleFontSize, titleColor, TextAlignmentOptions.Left);
        AddHeight(titleText.gameObject, titleHeight);
        titleText.text = "DAMAGE · BOSS";

        GameObject[] ships = boss.targets;
        int count = ships != null ? ships.Length : 0;
        rows = new Row[count];
        for (int i = 0; i < count; i++) rows[i] = CreateRow(ships[i], i);

        // Height has to be declared explicitly: the parent VerticalLayoutGroup
        // sizes children by their LayoutElement, and nothing here would
        // otherwise report a height.
        selfLayout.preferredHeight =
            (padding * 2f) + titleHeight + (count * rowHeight) + (count * rowSpacing);

        built = true;
    }

    private Row CreateRow(GameObject ship, int index)
    {
        Row row = new Row { ship = ship };

        row.root = new GameObject("Row_" + (index + 1), typeof(RectTransform));
        row.root.transform.SetParent(transform, false);
        AddHeight(row.root, rowHeight);

        // Bar track. Deliberately NOT a layout group - the fill and the two
        // labels are anchor-positioned on top of each other inside it.
        Image track = row.root.AddComponent<Image>();
        track.color = barTrackColor;
        track.raycastTarget = false;

        GameObject fillGO = new GameObject("Fill", typeof(RectTransform));
        fillGO.transform.SetParent(row.root.transform, false);
        row.fill = (RectTransform)fillGO.transform;
        // Anchor-driven width rather than Image.fillAmount: fillAmount needs a
        // real sprite to behave, and these Images are sprite-less colored
        // rects. Stretching anchorMax.x is exact and needs no asset.
        row.fill.anchorMin = new Vector2(0f, 0f);
        row.fill.anchorMax = new Vector2(0f, 1f);
        row.fill.offsetMin = Vector2.zero;
        row.fill.offsetMax = Vector2.zero;
        row.fillImage = fillGO.AddComponent<Image>();
        row.fillImage.color = RoleColor(ship);
        row.fillImage.raycastTarget = false;

        row.label = CreateText("Label", row.root.transform, rowFontSize, rowTextColor, TextAlignmentOptions.Left);
        Stretch(row.label.rectTransform, 6f, 6f);

        row.value = CreateText("Value", row.root.transform, rowFontSize, rowTextColor, TextAlignmentOptions.Right);
        Stretch(row.value.rectTransform, 6f, 6f);

        row.label.text = RoleName(ship);
        row.value.text = "0";
        return row;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, float size, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        return t;
    }

    private static void Stretch(RectTransform rt, float leftPad, float rightPad)
    {
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(leftPad, 0f);
        rt.offsetMax = new Vector2(-rightPad, 0f);
    }

    private static void AddHeight(GameObject go, float height)
    {
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;
        le.flexibleHeight = 0f;
    }

    private Color RoleColor(GameObject ship)
    {
        if (ship == null) return fallbackBarColor;
        PlayerRoleComponent role = ship.GetComponent<PlayerRoleComponent>();
        if (role == null) return fallbackBarColor;
        Color c = role.Stats.tintColor;
        c.a = 0.55f; // sits under the row text, so it can't fight it for contrast
        return c;
    }

    private static string RoleName(GameObject ship)
    {
        if (ship == null) return "--";
        PlayerRoleComponent role = ship.GetComponent<PlayerRoleComponent>();
        return role != null ? role.role.ToString() : ship.name;
    }

    // -------------------------------------------------------------- refresh

    private void Refresh()
    {
        if (rows == null) return;

        float total = 0f;
        float best = 0f;
        for (int i = 0; i < rows.Length; i++)
        {
            float d = boss.GetDamageDealt(rows[i].ship);
            rows[i].damage = d;
            total += d;
            if (d > best) best = d;
        }

        float elapsed = boss.CombatElapsed;

        titleText.text = elapsed > 0f
            ? $"DAMAGE · BOSS   {total:0.#}  ({total / elapsed:0.0}/s)"
            : "DAMAGE · BOSS";

        SortRowsByDamage();

        for (int i = 0; i < rows.Length; i++)
        {
            Row row = rows[i];

            // Bar length is relative to the top damage dealer, not to the
            // total - that's what makes a Recount bar readable at a glance.
            float frac = best > 0f ? row.damage / best : 0f;
            Vector2 max = row.fill.anchorMax;
            max.x = Mathf.Clamp01(frac);
            row.fill.anchorMax = max;

            float dps = elapsed > 0f ? row.damage / elapsed : 0f;
            float pct = total > 0f ? (row.damage / total) * 100f : 0f;
            // 0.# not 0: role fire damage is fractional (Medic deals 0.7 a shot), so
            // integer rounding would show a freshly-firing Medic as "0".
            row.value.text = $"{row.damage:0.#}  {dps:0.0}/s  {pct:0}%";

            row.root.transform.SetSiblingIndex(i + 1); // +1: the title holds index 0
        }
    }

    // Insertion sort, descending. The array is 4 entries and nearly always
    // already ordered between refreshes, so this is cheaper and allocates
    // nothing compared to List.Sort with a comparer.
    private void SortRowsByDamage()
    {
        for (int i = 1; i < rows.Length; i++)
        {
            Row key = rows[i];
            int j = i - 1;
            while (j >= 0 && rows[j].damage < key.damage)
            {
                rows[j + 1] = rows[j];
                j--;
            }
            rows[j + 1] = key;
        }
    }
}
