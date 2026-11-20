using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerAbility : MonoBehaviour
{
    [Header("Tank - Taunt (E), Super Shield (Q) and shield arc")]
    public TankAbilitySettings tankPowers = new TankAbilitySettings();
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
    /// resolution (ShipCollisionResolver) never lets two
    /// ships get closer than roughly that - so this must stay comfortably
    /// above ~0.6 or the Medic can never physically get an ally in range.
    /// </summary>
    public float auraRadius = 1.0f;
    public float auraTickInterval = 1f;
    public int auraHealPerTick = 1;
    public int auraShieldPerTick = 1;

    public PaletteSwatch healFlashColor = new PaletteSwatch(PaletteColor.MedicSoft);

    [Header("Medic - Sanctuary (Q)")]
    public MedicSanctuarySettings medicPowers = new MedicSanctuarySettings();

    [Header("Support - Echo Gate (Q)")]
    public SupportEchoGateSettings supportPowers = new SupportEchoGateSettings();

    [Header("Attacker - Nova Beam")]
    public AttackerBeamSettings beam = new AttackerBeamSettings();

    [Header("Attacker - Combo")]
    /// <summary>
    /// Per-slot cooldown on Combo1/2/3 - independent of the Nova Beam's
    /// cooldown above. This is what stops mashing a single key instead of actually
    /// playing the 1->2->3 rotation, not a timing/order requirement.
    /// </summary>
    public float comboAttackCooldown = 0.5f;
    /// <summary>
    /// Damage multiplier for each step of a correctly-played rotation, index
    /// 0 = combo slot 1. The 3rd entry is the finisher bonus for actually
    /// completing the sequence. Live multiplier of the caster's current
    /// fireDamage, same convention as beam.tickDamageMultiplier.
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

    private PlayerRoleComponent roleComponent;
    private PlayerController playerController;
    private PlayerHealth playerHealth;
    private float nextAbilityTime;
    private PlayerAbilityTank tank;
    private PlayerAbilityMedic medic;
    private PlayerAbilitySupport support;
    private PlayerAbilityAttacker attacker;
    private ShipCombatFx fx;

    public float CooldownRemaining => Mathf.Max(0f, nextAbilityTime - Time.time);
    public PlayerController PlayerController => playerController;
    public PlayerHealth Health => playerHealth;
    public ShipCombatFx Fx => fx;
    public Color TintColor => roleComponent.Stats.tintColor;
    // Which combo slot (1/2/3, keys E/R/T) continues the rotation correctly right now -
    // read by the HUD and by AIController so Attacker bots always feed
    // TryComboAttack() the right slot.
    public int AttackerComboExpectedSlot => attacker.ExpectedSlot;
    public float AttackerComboBrokenAt => attacker.LastBreakTime;
    public float AttackerComboFinishedAt => attacker.LastFinishTime;
    /// <summary>Seconds into the Nova Beam cast, or -1 when idle.</summary>
    public float AttackerCastElapsed => attacker.CastElapsed;
    public PlayerAbilityTank Tank => tank;
    public PlayerAbilityMedic Medic => medic;
    public PlayerAbilitySupport Support => support;

    /// <summary>Seconds into this ship's superpower (the cut-in card's clock), or -1 when idle.</summary>
    public float SuperpowerElapsed => roleComponent.role switch
    {
        PlayerRole.Tank => tank.SuperShieldElapsed,
        PlayerRole.Medic => medic.Sanctuary.Elapsed,
        PlayerRole.Support => support.Gate.Elapsed,
        _ => attacker.CastElapsed,
    };

    /// <summary>When the superpower lands, for the cut-in's shout: the beam firing, or the wall, dome or gate at full size.</summary>
    public float SuperpowerSnapSeconds => roleComponent.role switch
    {
        PlayerRole.Tank => tankPowers.growSeconds,
        PlayerRole.Medic => medicPowers.timing.openSeconds,
        PlayerRole.Support => supportPowers.timing.openSeconds,
        _ => beam.windupSeconds,
    };

    public string AbilityName => PlayerAbilityLabels.NameFor(roleComponent.role);
    public string StatusText => PlayerAbilityLabels.StatusFor(this, roleComponent.role);

    void Awake()
    {
        CacheComponents();
        CreateHelpers();
    }

    private void CacheComponents()
    {
        roleComponent = GetComponent<PlayerRoleComponent>();
        playerController = GetComponent<PlayerController>();
        playerHealth = GetComponent<PlayerHealth>();
        fx = GetComponent<ShipCombatFx>();
    }

    private void CreateHelpers()
    {
        tank = new PlayerAbilityTank(this);
        medic = new PlayerAbilityMedic(this);
        support = new PlayerAbilitySupport(this);
        attacker = new PlayerAbilityAttacker(this);
    }

    /// <summary>
    /// Start, not Awake: see PlayerRoleComponent.cs's Start() comment - the
    /// role-dependent setup below needs the real assigned role, which a
    /// dynamically-Instantiate()'d ship (co-op spawner) only has after
    /// Instantiate() returns, i.e. after Awake() already ran. The ref
    /// caching and helpers above stay in Awake since they're role-agnostic.
    /// </summary>
    void Start()
    {
        if (roleComponent.role == PlayerRole.Medic) medic.CreateAuraRing();
        if (roleComponent.role == PlayerRole.Tank) tank.Build();
        nextAbilityTime = Time.time + CooldownFor(roleComponent.role);
    }

    /// <summary>
    /// The per-role cooldown duration - used both to seed nextAbilityTime on
    /// spawn (see Start()) and to reset it each time TryUseAbility() actually
    /// triggers an effect.
    /// </summary>
    public float CooldownFor(PlayerRole role)
    {
        switch (role)
        {
            case PlayerRole.Tank: return tankPowers.superShieldCooldown;
            case PlayerRole.Medic: return medicPowers.cooldown;
            case PlayerRole.Support: return supportPowers.cooldown;
            case PlayerRole.Attacker: return beam.cooldown;
            default: return 0f;
        }
    }

    void Update()
    {
        if (roleComponent.role == PlayerRole.Medic) medic.CheckTick();
    }

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
        TriggerRoleAbility();
        if (fx != null) fx.PlayAbility(roleComponent.role);
    }

    private void TriggerRoleAbility()
    {
        switch (roleComponent.role)
        {
            case PlayerRole.Tank: tank.Trigger(); break;
            case PlayerRole.Medic: medic.Trigger(); break;
            case PlayerRole.Support: support.Trigger(); break;
            case PlayerRole.Attacker: attacker.Trigger(); break;
        }
    }

    /// <summary>Combo1 (key E) is the Attacker's first combo slot and the Tank's Taunt; each call is role-guarded.</summary>
    public void OnCombo1(InputValue value)
    {
        if (!value.isPressed) return;
        TryComboAttack(1);
        TryTaunt();
    }

    /// <summary>Non-input entry point for the Tank's Taunt, so AIController uses the same cooldown gate.</summary>
    public void TryTaunt()
    {
        if (roleComponent.role == PlayerRole.Tank) tank.Taunt.TryTaunt();
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
