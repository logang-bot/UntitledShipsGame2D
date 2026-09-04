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

    private readonly Dictionary<GameObject, float> lastContactDamageTime = new Dictionary<GameObject, float>();
    private SpriteRenderer sr;
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
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        shockwave = GetComponent<WardenShockwave>();
        mechanics = new Behaviour[] { GetComponent<WardenMovement>(), shockwave, GetComponent<WardenLockdownVolley>(), armA, armB };
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
        foreach (Behaviour mechanic in mechanics)
            if (mechanic != null) mechanic.enabled = true;
    }

    public void SetVisible(bool visible)
    {
        if (sr != null) sr.enabled = visible;
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

    public void TakeDamage(float amount)
    {
        CurrentHealth -= Mathf.RoundToInt(amount);
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

    private void EnterPhase2()
    {
        IsPhase2 = true;
        if (armC != null) armC.gameObject.SetActive(true);
        OnPhase2?.Invoke();
    }

    private void Die()
    {
        OnDefeated?.Invoke();
        Destroy(gameObject);
    }
}
