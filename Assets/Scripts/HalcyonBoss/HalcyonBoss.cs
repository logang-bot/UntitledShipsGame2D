using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Level 2's boss: HP/phases/body-contact-damage core only. Roam, Surge and
// Static Field are sibling components on this same GameObject - see
// docs/superpowers/specs/2026-09-04-halcyon-boss-design.md. Unlike
// MarauderBoss, Halcyon has no aggro/threat table and never fires a bullet.
public class HalcyonBoss : MonoBehaviour, IBoss
{
    [Header("Health / Phases")]
    public int maxHealth = 110;
    public UnityEvent OnPhase2;
    public UnityEvent OnDefeated;

    [Header("Combat")]
    // Reference unit only - Halcyon never fires a bullet. Contact damage and
    // HalcyonStaticField's pulse damage both multiply this, same convention
    // MarauderBoss.bulletDamage uses.
    public float bulletDamage = 1f;

    [Header("Body contact")]
    public float bodyContactDamageMultiplier = 2f;
    public float contactDamageCooldown = 1f;

    public int CurrentHealth { get; private set; }
    public bool IsPhase2 { get; private set; }

    private readonly Dictionary<GameObject, float> lastContactDamageTime = new Dictionary<GameObject, float>();
    private SpriteRenderer sr;
    private Collider2D col;
    private HalcyonStaticField staticField;
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
        staticField = GetComponent<HalcyonStaticField>();
        mechanics = new Behaviour[] { GetComponent<HalcyonRoam>(), GetComponent<HalcyonSurge>(), staticField };
    }

    private void DisableMechanics()
    {
        foreach (Behaviour mechanic in mechanics)
            if (mechanic != null) mechanic.enabled = false;
    }

    /// <summary>
    /// Fires when LevelSequencer flips this on, right after the boss's own
    /// entrance glide completes - the moment Roam/Surge/Static Field are
    /// allowed to start acting.
    /// </summary>
    void OnEnable()
    {
        foreach (Behaviour mechanic in mechanics)
            if (mechanic != null) mechanic.enabled = true;
    }

    /// <summary>
    /// Hides/shows sprite + collider + the Static Field ring together,
    /// without touching the GameObject's active state - same reasoning as
    /// MarauderBoss.SetVisible (see docs/systems/level-sequencing.md).
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (sr != null) sr.enabled = visible;
        if (col != null) col.enabled = visible;
        if (staticField != null) staticField.SetRingVisible(visible);
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

    private void EnterPhase2()
    {
        IsPhase2 = true;
        OnPhase2?.Invoke();
    }

    private void Die()
    {
        OnDefeated?.Invoke();
        Destroy(gameObject);
    }
}
