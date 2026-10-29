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
    private const int RingSegments = 32;

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
    public Color ringColor = new Color(0.3f, 0.6f, 1f, 0.25f);
    public Color telegraphColor = new Color(0.5f, 0.85f, 1f, 0.85f);
    public Color impactColor = new Color(0.9f, 0.95f, 1f, 1f);
    public float ringWidth = 0.06f;
    public float telegraphWidth = 0.14f;
    public float telegraphPulseSpeed = 12f;

    public float CooldownRemaining => Mathf.Max(0f, nextPulseTime - Time.time);

    private HalcyonBoss boss;
    private LineRenderer ring;
    private bool isTelegraphing;
    private float impactFlashUntil;
    private float nextPulseTime;

    void Awake()
    {
        boss = GetComponent<HalcyonBoss>();
        CreateRing();
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
        if (ring != null) ring.gameObject.SetActive(visible);
    }

    private void CreateRing()
    {
        GameObject ringObj = new GameObject("StaticFieldRing");
        ringObj.transform.SetParent(transform, false);
        ring = ringObj.AddComponent<LineRenderer>();
        ring.useWorldSpace = true;
        ring.loop = true;
        ring.positionCount = RingSegments;
        ring.material = new Material(Shader.Find("Sprites/Default"));
        ring.sortingLayerName = "Default";
        ring.sortingOrder = -1;
    }

    private void UpdateRing()
    {
        if (ring == null) return;
        SelectRingColorAndWidth(out Color color, out float width);
        ring.startColor = color;
        ring.endColor = color;
        ring.startWidth = width;
        ring.endWidth = width;
        UpdateRingGeometry();
    }

    private void SelectRingColorAndWidth(out Color color, out float width)
    {
        if (isTelegraphing) { TelegraphColorAndWidth(out color, out width); return; }
        bool flashing = Time.time < impactFlashUntil;
        color = flashing ? impactColor : ringColor;
        width = flashing ? telegraphWidth : ringWidth;
    }

    private void TelegraphColorAndWidth(out Color color, out float width)
    {
        float pulse = (Mathf.Sin(Time.time * telegraphPulseSpeed) + 1f) * 0.5f;
        color = Color.Lerp(ringColor, telegraphColor, pulse);
        width = Mathf.Lerp(ringWidth, telegraphWidth, pulse);
    }

    private void UpdateRingGeometry()
    {
        Vector3 center = transform.position;
        for (int i = 0; i < RingSegments; i++)
        {
            float angle = (i / (float)RingSegments) * Mathf.PI * 2f;
            ring.SetPosition(i, center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * bossRange);
        }
    }
}
