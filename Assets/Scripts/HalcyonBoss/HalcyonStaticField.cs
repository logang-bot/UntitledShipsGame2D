using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Sibling component on the Boss GameObject: Halcyon's actual damage source.
// Every pulseCooldown, every ship near the boss takes damage - light for a
// lone ship, heavy for ships also clustered near each other - reuses MarauderBoss's shockwave damage convention
// (3x bulletDamage) and ring-telegraph idiom. See
// docs/superpowers/specs/2026-09-04-halcyon-boss-design.md.
public class HalcyonStaticField : MonoBehaviour
{
    [Header("Ships")]
    public GameObject[] ships; // drag Player + 3 Teammates - proximity data only, no aggro

    [Header("Pulse")]
    public float pulseCooldown = 6f;
    public float pulseCooldownPhase2 = 4f;
    public float bossRange = 1.8f;
    public float clusterRange = 0.6f;
    public float damageMultiplier = 3f; // per clustered partner
    public float soloDamageMultiplier = 1f; // a ship in range with no partner nearby
    public float telegraphTime = 0.3f;
    public float impactFlashDuration = 0.15f;

    [Header("Ring visual")]
    // Child on the boss prefab: charge ring idle and while telegraphing,
    // the pulse frame on impact, then the fade frame for fadeDuration.
    public DangerZoneVisual visual;
    public float fadeDuration = 0.3f;

    public float CooldownRemaining => Mathf.Max(0f, nextPulseTime - Time.time);

    private HalcyonBoss boss;
    private bool isTelegraphing;
    private float impactFlashUntil;
    private float nextPulseTime;

    void Awake()
    {
        boss = GetComponent<HalcyonBoss>();
    }

    void OnEnable()
    {
        nextPulseTime = Time.time + CurrentCooldown();
    }

    void Update()
    {
        UpdateRing();
        if (Time.time >= nextPulseTime) StartCoroutine(PulseRoutine());
    }

    private float CurrentCooldown()
    {
        return boss != null && boss.IsPhase2 ? pulseCooldownPhase2 : pulseCooldown;
    }

    private IEnumerator PulseRoutine()
    {
        nextPulseTime = Time.time + CurrentCooldown();
        isTelegraphing = true;
        yield return new WaitForSeconds(telegraphTime);
        isTelegraphing = false;
        impactFlashUntil = Time.time + impactFlashDuration;
        ApplyPulseDamage();
    }

    /// <summary>
    /// A ship with no clustered partner takes soloDamageMultiplier; a ship
    /// with N partners within clusterRange takes N x damageMultiplier - the
    /// same total the original pairwise (each pair hits both ships) rule gave.
    /// </summary>
    private void ApplyPulseDamage()
    {
        List<GameObject> nearBoss = ShipsNearBoss();
        foreach (GameObject ship in nearBoss)
        {
            int partners = CountClusterPartners(ship, nearBoss);
            DamageShip(ship, partners > 0 ? partners * damageMultiplier : soloDamageMultiplier);
        }
    }

    private int CountClusterPartners(GameObject ship, List<GameObject> nearBoss)
    {
        int partners = 0;
        foreach (GameObject other in nearBoss)
            if (other != ship && Vector2.Distance(ship.transform.position, other.transform.position) <= clusterRange)
                partners++;
        return partners;
    }

    private void DamageShip(GameObject ship, float multiplier)
    {
        PlayerHealth health = ship.GetComponent<PlayerHealth>();
        if (health != null) health.TakeDamage(Mathf.RoundToInt(boss.bulletDamage * multiplier));
    }

    private List<GameObject> ShipsNearBoss()
    {
        List<GameObject> result = new List<GameObject>();
        foreach (GameObject ship in ships)
        {
            if (ship == null || !ship.activeInHierarchy) continue;
            if (Vector2.Distance(ship.transform.position, transform.position) <= bossRange) result.Add(ship);
        }
        return result;
    }

    public void SetRingVisible(bool visible)
    {
        if (visual != null) visual.gameObject.SetActive(visible);
    }

    private void UpdateRing()
    {
        if (visual != null) visual.Show(CurrentZoneState(), bossRange);
    }

    private DangerZoneState CurrentZoneState()
    {
        if (isTelegraphing) return DangerZoneState.Telegraph;
        if (Time.time < impactFlashUntil) return DangerZoneState.Impact;
        return Time.time < impactFlashUntil + fadeDuration ? DangerZoneState.AfterImpact : DangerZoneState.Idle;
    }
}
