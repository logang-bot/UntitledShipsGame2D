using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Level 3's boss: HP/phases/body-contact-damage/taunt-window core only.
// Movement, Shockwave, Lockdown Volley, and the arms are sibling components
// on this same GameObject - see
// docs/superpowers/specs/2026-09-04-warden-boss-design.md.
public class WardenBoss : MonoBehaviour, IBoss
{
    [Header("Health / Phases")]
    public int maxHealth = 130;
    public UnityEvent OnPhase2;
    public UnityEvent OnDefeated;

    [Header("Combat")]
    public float bulletDamage = 1f;

    [Header("Body contact")]
    public float bodyContactDamageMultiplier = 2f;
    public float contactDamageCooldown = 1f;

    [Header("Taunt")]
    // How long a Taunt biases arm re-picks toward the taunter - Warden has no
    // aggro table to hard-redirect, so Taunt instead weights the next few
    // random re-picks (see WardenArm.PickWeighted).
    public float tauntWindowDuration = 3f;

    [Header("Ships")]
    public GameObject[] ships; // drag Player + 3 Teammates - proximity/targeting data only, no aggro

    [Header("Arms")]
    public WardenArm armA;
    public WardenArm armB;
    public WardenArm armC; // Phase 2 only - its GameObject starts inactive in the scene

    public int CurrentHealth { get; private set; }
    public bool IsPhase2 { get; private set; }
    public GameObject TauntedShip { get; private set; }
    public float TauntActiveUntil { get; private set; }
    public bool IsShielded => ArmShields(armA) || ArmShields(armB) || ArmShields(armC);

    private readonly Dictionary<GameObject, float> lastContactDamageTime = new Dictionary<GameObject, float>();
    private BossModel model;
    private BossHitFlash hitFlash;
    private Collider2D col;
    private WardenShockwave shockwave;
    private Behaviour[] mechanics;

    void Awake()
    {
        CurrentHealth = maxHealth;
        CacheComponents();
        DisableMechanics();
    }

    private void CacheComponents()
    {
        model = GetComponentInChildren<BossModel>(true);
        hitFlash = GetComponent<BossHitFlash>();
        col = GetComponent<Collider2D>();
        shockwave = GetComponent<WardenShockwave>();
        mechanics = new Behaviour[] { GetComponent<WardenMovement>(), shockwave, GetComponent<WardenLockdownVolley>(), GetComponent<WardenBigShot>(), armA, armB };
    }

    private void DisableMechanics()
    {
        foreach (Behaviour mechanic in mechanics)
            if (mechanic != null) mechanic.enabled = false;
    }

    /// <summary>
    /// Fires when LevelSequencer flips this on at BossCombat - the moment
    /// every sibling mechanic (except armC, which is Phase 2-gated) is
    /// allowed to start acting.
    /// </summary>
    void OnEnable()
    {
        DamageLedger.BeginCombat();
        foreach (Behaviour mechanic in mechanics)
            if (mechanic != null) mechanic.enabled = true;
    }

    public void SetVisible(bool visible)
    {
        if (model != null) model.SetVisible(visible);
        if (col != null) col.enabled = visible;
        if (shockwave != null) shockwave.SetVisible(visible);
        if (armA != null) armA.SetVisible(visible);
        if (armB != null) armB.SetVisible(visible);
    }

    public void ApplyContactDamage(GameObject ship)
    {
        if (ship == null) return;
        PlayerHealth health = ship.GetComponent<PlayerHealth>();
        if (health == null) return;
        if (lastContactDamageTime.TryGetValue(ship, out float t) && Time.time - t < contactDamageCooldown) return;

        lastContactDamageTime[ship] = Time.time;
        health.TakeDamage(Mathf.RoundToInt(bulletDamage * bodyContactDamageMultiplier));
    }

    /// <summary>
    /// Arm hits are recorded here too (see WardenArmHealth.TakeDamage): while
    /// the core is shielded the arms are the fight's only damage target.
    /// </summary>
    public BossDamageLedger DamageLedger { get; } = new BossDamageLedger();

    public void TakeDamage(float amount, GameObject source = null)
    {
        if (IsShielded) return;
        CurrentHealth -= Mathf.RoundToInt(amount);
        DamageLedger.Record(source, amount);
        if (hitFlash != null) hitFlash.Flash();
        if (!IsPhase2 && CurrentHealth <= maxHealth / 2) EnterPhase2();
        if (CurrentHealth <= 0) Die();
    }

    /// <summary>
    /// Persistent listener for every ship's PlayerAbility.OnTaunt. Unlike
    /// MarauderBoss.TauntedBy (an instant aggro overwrite), Warden has no
    /// single-target aggro to redirect - this just opens a biasing window
    /// WardenArm.PickWeighted reads on its next re-pick.
    /// </summary>
    public void TauntedBy(GameObject taunter)
    {
        TauntedShip = taunter;
        TauntActiveUntil = Time.time + tauntWindowDuration;
    }

    /// <summary>
    /// An arm shields only while its GameObject is active (so ArmC counts
    /// from Phase 2 on) and its WardenArmHealth isn't knocked down. An arm
    /// without WardenArmHealth never shields.
    /// </summary>
    public static bool ArmShields(WardenArm arm)
    {
        if (arm == null || !arm.gameObject.activeInHierarchy) return false;
        return arm.TryGetComponent(out WardenArmHealth health) && !health.IsDown;
    }

    private void EnterPhase2()
    {
        IsPhase2 = true;
        if (armC != null) armC.gameObject.SetActive(true);
        OnPhase2?.Invoke();
    }

    private void Die()
    {
        DamageLedger.EndCombat();
        OnDefeated?.Invoke();
        Destroy(gameObject);
    }
}
