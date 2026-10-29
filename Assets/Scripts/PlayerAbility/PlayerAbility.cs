using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerAbility : MonoBehaviour
{
    [Header("Tank - Taunt")]
    public float tauntCooldown = 5f;
    public UnityEvent OnTaunt;

    [Header("Medic - Aura")]
    /// <summary>
    /// All 4 ship transforms (self included - filtered out at runtime), so
    /// the aura can reach the human Player too, unlike AIController's
    /// teammates[] which deliberately excludes Player.
    /// </summary>
    public Transform[] allies;
    /// <summary>
    /// Ship colliders are ~0.6 world units across, and ship-vs-ship overlap
    /// resolution (PlayerController.ResolveShipCollisions) never lets two
    /// ships get closer than roughly that - so this must stay comfortably
    /// above ~0.6 or the Medic can never physically get an ally in range.
    /// </summary>
    public float auraRadius = 1.0f;
    public float auraTickInterval = 1f;
    public int auraHealPerTick = 1;
    public int auraShieldPerTick = 1;

    [Header("Medic - Aura Boost (E ability)")]
    /// <summary>Drastically larger while active - halved from 3, was flagged overpowered.</summary>
    public float auraBoostRadius = 2.0f;
    /// <summary>Much faster while active.</summary>
    public float auraBoostTickInterval = 0.25f;
    public float auraBoostDuration = 4f;
    /// <summary>Must stay >= duration, same constraint as Support's buff.</summary>
    public float auraBoostCooldown = 10f;

    [Header("Medic - Aura Visual")]
    public PaletteSwatch auraRingColor = new PaletteSwatch(PaletteColor.Medic, 0.35f);
    public PaletteSwatch auraRingBoostedColor = new PaletteSwatch(PaletteColor.Medic, 0.7f);
    public float auraRingWidth = 0.05f;
    public float auraRingBoostedWidth = 0.12f;
    public PaletteSwatch healFlashColor = new PaletteSwatch(PaletteColor.MedicSoft);

    [Header("Support - Speed Boost")]
    /// <summary>Up from 8s - flagged overpowered, round placeholder, tunable.</summary>
    public float speedBoostCooldown = 15f;
    public float speedBoostDuration = 4f;
    /// <summary>Applies to move speed AND fire rate, all 4 allies.</summary>
    public float speedBoostMultiplier = 1.5f;

    [Header("Attacker - Big Shot")]
    public float bigShotCooldown = 3f;
    public float bigShotWidthMultiplier = 3f;
    /// <summary>Live multiplier of the caster's current fireDamage, not a flat number.</summary>
    public float bigShotDamageMultiplier = 2f;
    public float recoilForce = 6f;

    [Header("Attacker - Combo")]
    /// <summary>
    /// Per-slot cooldown on Combo1/2/3 - independent of bigShotCooldown
    /// above. This is what stops mashing a single key instead of actually
    /// playing the 1->2->3 rotation, not a timing/order requirement.
    /// </summary>
    public float comboAttackCooldown = 0.5f;
    /// <summary>
    /// Damage multiplier for each step of a correctly-played rotation, index
    /// 0 = combo slot 1. The 3rd entry is the finisher bonus for actually
    /// completing the sequence. Live multiplier of the caster's current
    /// fireDamage, same convention as bigShotDamageMultiplier.
    /// </summary>
    public float[] comboStepDamageMultipliers = { 1f, 1.3f, 1.8f };
    /// <summary>Bullet width per correct combo step, purely visual escalation toward the finisher.</summary>
    public float[] comboStepWidthMultipliers = { 1f, 1.5f, 2.5f };
    /// <summary>
    /// Applied instead of the step multiplier when the wrong slot is
    /// pressed - the "bad execution" penalty. Deliberately still deals some
    /// damage rather than nothing, and resets the rotation to slot 1.
    /// </summary>
    public float comboBreakDamageMultiplier = 0.5f;

    [Header("Tank - Shield Arc")]
    /// <summary>Arc width = Tank's own collider width x this.</summary>
    public float shieldArcWidthMultiplier = 3f;
    /// <summary>Bulge/thickness of the curve.</summary>
    public float shieldArcHeight = 0.4f;
    /// <summary>Local offset above the ship.</summary>
    public float shieldArcYOffset = 0.3f;
    public PaletteSwatch shieldArcColor = new PaletteSwatch(PaletteColor.Tank, 0.5f);
    public float shieldArcLineWidth = 0.08f;

    [Header("Party Buff Visual")]
    public float partyBuffRingRadius = 0.6f;
    public float partyBuffRingWidth = 0.08f;

    private PlayerRoleComponent roleComponent;
    private PlayerController playerController;
    private PlayerHealth playerHealth;
    private float nextAbilityTime;
    private PlayerAbilityPartyBuffRing partyBuffRing;
    private PlayerAbilityTank tank;
    private PlayerAbilityMedic medic;
    private PlayerAbilitySupport support;
    private PlayerAbilityAttacker attacker;

    public float CooldownRemaining => Mathf.Max(0f, nextAbilityTime - Time.time);
    public bool IsSpeedBoostActive => support.IsActive;
    public float SpeedBoostRemaining => support.Remaining;
    public bool IsAuraBoosted => medic.IsBoosted;
    public float AuraBoostRemaining => medic.BoostRemaining;
    public PlayerController PlayerController => playerController;
    public Color TintColor => roleComponent.Stats.tintColor;
    // Which combo key (1/2/3) continues the rotation correctly right now -
    // read by the HUD and by AIController so Attacker bots always feed
    // TryComboAttack() the right slot.
    public int AttackerComboExpectedSlot => attacker.ExpectedSlot;

    public string AbilityName => PlayerAbilityLabels.NameFor(roleComponent.role);
    public string StatusText => PlayerAbilityLabels.StatusFor(this, roleComponent.role);

    void Awake()
    {
        CacheComponents();
        CreateHelpers();
        partyBuffRing.Create();
    }

    private void CacheComponents()
    {
        roleComponent = GetComponent<PlayerRoleComponent>();
        playerController = GetComponent<PlayerController>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    private void CreateHelpers()
    {
        tank = new PlayerAbilityTank(this);
        medic = new PlayerAbilityMedic(this);
        support = new PlayerAbilitySupport(this);
        attacker = new PlayerAbilityAttacker(this);
        partyBuffRing = new PlayerAbilityPartyBuffRing(this);
    }

    /// <summary>
    /// Start, not Awake: see PlayerRoleComponent.cs's Start() comment - the
    /// role-dependent setup below needs the real assigned role, which a
    /// dynamically-Instantiate()'d ship (co-op spawner) only has after
    /// Instantiate() returns, i.e. after Awake() already ran. The ref
    /// caching and party-buff ring above stay in Awake since they're
    /// role-agnostic.
    /// </summary>
    void Start()
    {
        if (roleComponent.role == PlayerRole.Medic) medic.CreateAuraRing();
        if (roleComponent.role == PlayerRole.Tank) tank.CreateShieldArc();
        nextAbilityTime = Time.time + CooldownFor(roleComponent.role);
    }

    /// <summary>
    /// The per-role cooldown duration - used both to seed nextAbilityTime on
    /// spawn (see Start()) and to reset it each time TryUseAbility() actually
    /// triggers an effect.
    /// </summary>
    float CooldownFor(PlayerRole role)
    {
        switch (role)
        {
            case PlayerRole.Tank: return tauntCooldown;
            case PlayerRole.Medic: return auraBoostCooldown;
            case PlayerRole.Support: return speedBoostCooldown;
            case PlayerRole.Attacker: return bigShotCooldown;
            default: return 0f;
        }
    }

    void Update()
    {
        if (roleComponent.role != PlayerRole.Medic) return;
        medic.UpdateRing();
        medic.CheckTick();
    }

    /// <summary>
    /// Called on every ally by Support's speed boost - shows/hides this
    /// ship's party-buff ring in the caster's tint color.
    /// </summary>
    public void SetPartyBuffVisual(bool active, Color color) => partyBuffRing?.Show(active, color);

    public void OnAbility(InputValue value)
    {
        if (!value.isPressed) return;
        TryUseAbility();
    }

    /// <summary>
    /// Non-input entry point so AIController can trigger abilities directly
    /// through the same cooldown gate.
    /// </summary>
    public void TryUseAbility()
    {
        if (Time.time < nextAbilityTime) return;
        nextAbilityTime = Time.time + CooldownFor(roleComponent.role);
        switch (roleComponent.role)
        {
            case PlayerRole.Tank: tank.Trigger(); break;
            case PlayerRole.Medic: medic.Trigger(); break;
            case PlayerRole.Support: support.Trigger(); break;
            case PlayerRole.Attacker: attacker.Trigger(); break;
        }
    }

    public void OnCombo1(InputValue value)
    {
        if (value.isPressed) TryComboAttack(1);
    }

    public void OnCombo2(InputValue value)
    {
        if (value.isPressed) TryComboAttack(2);
    }

    public void OnCombo3(InputValue value)
    {
        if (value.isPressed) TryComboAttack(3);
    }

    /// <summary>
    /// Non-input entry point so AIController can drive the Attacker's combo
    /// directly, same pattern as TryUseAbility(). Attacker-only: the other
    /// roles' PlayerAbility instances still create an (unused) attacker
    /// helper - see CreateHelpers() - so this needs its own role guard
    /// rather than relying on the switch in TryUseAbility().
    /// </summary>
    public void TryComboAttack(int slot)
    {
        if (roleComponent.role != PlayerRole.Attacker) return;
        attacker.TryComboAttack(slot);
    }
}
