using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The freeze-and-pick screen (design: Neon Fleet HUD v2, section 10). Opens
// over the playfield whenever AttackerProgression has a pick banked and no
// pause or end screen is up; banked picks open back to back. Keys 1 / 2 / 3,
// a click, or focus + Enter / A pick a card. See
// docs/systems/roguelike-progression.md.
public class LevelUpUI : MonoBehaviour
{
    private const float SlideDistance = 12f;
    private static readonly UpgradeCard[] Cards = { UpgradeCard.AttackDamage, UpgradeCard.CritStrike, UpgradeCard.Multishot };

    public AttackerProgression progression;
    // Same guards as PauseUI: never open on top of an end screen.
    public GameObject gameOverPanelRoot;
    public GameObject victoryPanelRoot;
    public GameObject panel;
    public LevelUpHeaderView header;
    public UpgradeCardView[] cards;
    public CanvasGroup cardStack;
    public VerticalLayoutGroup cardStackLayout;
    public HudFreezeDimmer dimmer;

    public bool IsOpen { get; private set; }

    private readonly LevelUpFlow flow = new LevelUpFlow();
    private readonly PickStreak streak = new PickStreak();
    private readonly UpgradeCardCopy[] copies = new UpgradeCardCopy[Cards.Length];
    private LevelUpKeys keys;
    private int focused = -1;

    void Awake()
    {
        keys = new LevelUpKeys(Pick);
        for (int i = 0; i < cards.Length; i++) BindCard(i);
        panel.SetActive(false);
    }

    void OnDisable() => keys?.Disable();

    void Update()
    {
        if (IsOpen) Tick(Time.unscaledTime);
        else if (ShouldOpen()) OpenStreak();
    }

    private void BindCard(int index)
    {
        cards[index].Bind(index, Cards[index]);
        cards[index].Picked += Pick;
        cards[index].Focused += focusedIndex => focused = focusedIndex;
    }

    private bool ShouldOpen()
    {
        if (progression == null || progression.PendingPicks <= 0) return false;
        if (!progression.Ship.gameObject.activeInHierarchy) return false;
        if (Time.timeScale == 0f) return false;
        if (gameOverPanelRoot != null && gameOverPanelRoot.activeSelf) return false;
        return victoryPanelRoot == null || !victoryPanelRoot.activeSelf;
    }

    private void OpenStreak()
    {
        streak.Start(progression.PendingPicks);
        IsOpen = true;
        Time.timeScale = 0f;
        dimmer.SetFrozen(true);
        keys.Enable();
        panel.SetActive(true);
        OpenScreen();
    }

    private void OpenScreen()
    {
        flow.Open(Time.unscaledTime);
        Select(null);
        header.Show(LevelThisPickReaches(), streak, PilotLabel());
        for (int i = 0; i < Cards.Length; i++) copies[i] = UpgradeCardCopy.From(ContextFor(Cards[i]));
        Tick(Time.unscaledTime);
    }

    private void Tick(float now)
    {
        if (flow.IsConfirmDone(now)) { FinishPick(); return; }
        if (!flow.IsLocked(now) && focused < 0 && !flow.HasPicked) Select(cards[0]);
        header.ShowLock(flow, now);
        Slide(flow.SlideIn(now));
        for (int i = 0; i < cards.Length; i++) cards[i].Show(copies[i], flow.StateOf(i, focused, now), Taken(i));
    }

    private void Pick(int index)
    {
        if (IsOpen && flow.TryPick(index, Time.unscaledTime)) progression.ApplyCard(Cards[index]);
    }

    private void FinishPick()
    {
        if (!streak.HasMore) { Close(); return; }
        streak.Advance();
        OpenScreen();
    }

    private void Close()
    {
        Select(null);
        keys.Disable();
        panel.SetActive(false);
        dimmer.SetFrozen(false);
        Time.timeScale = 1f;
        IsOpen = false;
    }

    private void Select(UpgradeCardView card)
    {
        focused = card == null ? -1 : System.Array.IndexOf(cards, card);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(card == null ? null : card.gameObject);
    }

    private void Slide(float shown)
    {
        int offset = Mathf.RoundToInt(SlideDistance * (1f - shown));
        cardStack.alpha = shown;
        if (cardStackLayout.padding.top != offset) cardStackLayout.padding = new RectOffset(0, 0, offset, -offset);
    }

    private int Taken(int index) => progression.Upgrades.PicksOf(Cards[index]);

    private int LevelThisPickReaches() => progression.Xp.Level - progression.PendingPicks + 1;

    private string PilotLabel() =>
        RoleCardText.ShipName(PlayerRole.Attacker) + " · " + progression.partyFrameManager.SeatOf(progression.Ship.gameObject).label;

    private UpgradeCardContext ContextFor(UpgradeCard card) => new UpgradeCardContext
    {
        card = card, taken = progression.Upgrades.PicksOf(card), damage = progression.Ship.CurrentDamage, critInterval = progression.critInterval,
    };
}
