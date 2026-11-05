using UnityEngine;
using UnityEngine.UI;

// One squad frame on the left HUD rail (design: HUD Party Frame). Bound to a
// ship by PartyFrameManager, then each frame it hands that ship's live state
// to its child views. Like the rest of the HUD it only reads game state:
// PlayerHealth, PlayerController, PlayerAbility and the boss's
// BossDamageLedger (for DPS) all stay the source of truth.
public class PartyFrameUI : MonoBehaviour
{
    private const float CpuPortraitAlpha = 0.6f;

    public NeonFrame frame;
    public Image accentEdge;
    public Image portrait;
    public Sprite[] roleIcons;
    public FrameIdentityView identity;
    public FrameVitalsView vitals;
    public FrameStatsView stats;
    public GameObject abilityRow;
    public AbilityCellView ability;
    public ComboKeysView combo;
    public RogueFrameView rogue;
    public GameObject downOverlay;
    public CanvasGroup content;

    private GameObject ship;
    private PlayerHealth health;
    private PlayerController controller;
    private PlayerAbility shipAbility;
    private PlayerRole role;
    private FrameSeat seat;
    private bool isDown;

    public void Initialize(GameObject player, FrameSeat frameSeat)
    {
        CacheShip(player);
        seat = frameSeat;
        ShowSeat();
        ability.Bind(role, !seat.isHuman, shipAbility);
        combo.Hide();
        rogue.gameObject.SetActive(false);
        downOverlay.SetActive(false);
    }

    public PlayerRole Role => role;
    public bool IsBound => ship != null;

    /// <summary>
    /// Roguelike Attacker test: the Attack/Crit cells and XP row (or the CPU
    /// note) take the place of the ability cell and combo box.
    /// </summary>
    public void BindRogue(AttackerProgression progression)
    {
        abilityRow.SetActive(false);
        rogue.Bind(progression, seat.isHuman);
    }

    /// <summary>
    /// One last paint before Update() stops: OnDeath fires inside the killing
    /// TakeDamage(), before this frame's Update(), so without it the pips
    /// would freeze on the previous frame's values.
    /// </summary>
    public void OnPlayerDied()
    {
        isDown = true;
        Paint();
        ShowDown();
    }

    void Update()
    {
        if (health == null || isDown) return;
        Paint();
    }

    private void CacheShip(GameObject player)
    {
        ship = player;
        health = player.GetComponent<PlayerHealth>();
        controller = player.GetComponent<PlayerController>();
        shipAbility = player.GetComponent<PlayerAbility>();
        role = player.GetComponent<PlayerRoleComponent>().role;
    }

    private void ShowSeat()
    {
        RoleColors colors = RolePalette.For(role);
        frame.SetLook(PartyFrameLooks.Frame(role, seat));
        accentEdge.enabled = seat.isLocal;
        accentEdge.color = colors.main;
        portrait.sprite = roleIcons[(int)role];
        portrait.color = seat.isHuman ? colors.main : NeonPalette.Get(RolePalette.TokenFor(role), CpuPortraitAlpha);
        identity.Show(role, seat);
    }

    private void Paint()
    {
        vitals.Show(health);
        identity.SetConditions(FrameVitalsView.IsLow(health), controller.speedBuffMultiplier > 1f);
        stats.Show(controller, Dps(), !seat.isHuman);
        if (abilityRow.activeSelf) ability.Show(AbilityCellState.From(AbilityTimes.Read(shipAbility, role, isDown)));
        else PaintRogue();
    }

    private void PaintRogue()
    {
        rogue.Show();
        identity.SetLevelUpPending(rogue.LevelUpPending);
    }

    private float Dps() => controller.bossObject is IBoss boss ? boss.DamageLedger.Dps(ship) : 0f;

    private void ShowDown()
    {
        downOverlay.SetActive(true);
        content.alpha = 0.4f;
        frame.SetLook(PartyFrameLooks.Down);
        accentEdge.enabled = false;
        identity.ShowDown();
        stats.ShowDown();
        combo.Hide();
        rogue.gameObject.SetActive(false);
    }
}
