using System.Collections;
using UnityEngine;

// Sibling component: Marauder's telegraphed-proximity-pulse-plus-knockback
// mechanic, ported with unchanged numbers/formula. See
// docs/superpowers/specs/2026-09-04-warden-boss-design.md.
public class WardenShockwave : MonoBehaviour
{
    private const int RingSegments = 32;

    [Header("Shockwave")]
    public float radius = 1.7f;
    public float damageMultiplier = 3f;
    public float knockback = 33f;
    public float cooldown = 3f;
    public float telegraphTime = 0.3f;
    public float impactFlashDuration = 0.15f;

    [Header("Ring visual")]
    public Color ringColor = new Color(1f, 0.4f, 0.1f, 0.25f);
    public Color ringTelegraphColor = new Color(1f, 0.15f, 0.1f, 0.85f);
    public Color ringImpactColor = new Color(1f, 0.9f, 0.3f, 1f);
    public float ringWidth = 0.06f;
    public float ringTelegraphWidth = 0.14f;
    public float telegraphPulseSpeed = 12f;

    public float CooldownRemaining => Mathf.Max(0f, nextCheckTime - Time.time);

    private WardenBoss boss;
    private LineRenderer ring;
    private bool isTelegraphing;
    private float impactFlashUntil;
    private float nextCheckTime;

    void Awake()
    {
        boss = GetComponent<WardenBoss>();
        CreateRing();
    }

    void OnEnable()
    {
        nextCheckTime = Time.time + cooldown;
    }

    void Update()
    {
        UpdateRing();
        CheckShockwave();
    }

    private void CheckShockwave()
    {
        if (Time.time < nextCheckTime) return;
        foreach (GameObject ship in boss.ships)
        {
            if (ship == null || !ship.activeInHierarchy) continue;
            if (Vector2.Distance(ship.transform.position, transform.position) <= radius)
            {
                nextCheckTime = Time.time + cooldown;
                StartCoroutine(ShockwaveRoutine());
                return;
            }
        }
    }

    private IEnumerator ShockwaveRoutine()
    {
        isTelegraphing = true;
        yield return new WaitForSeconds(telegraphTime);
        isTelegraphing = false;
        impactFlashUntil = Time.time + impactFlashDuration;
        foreach (GameObject ship in boss.ships)
        {
            if (ship == null || !ship.activeInHierarchy) continue;
            ApplyShockwaveEffect(ship);
        }
    }

    private void ApplyShockwaveEffect(GameObject ship)
    {
        Vector2 toShip = (Vector2)ship.transform.position - (Vector2)transform.position;
        if (toShip.magnitude > radius) return;

        PlayerHealth health = ship.GetComponent<PlayerHealth>();
        if (health != null) health.TakeDamage(Mathf.RoundToInt(boss.bulletDamage * damageMultiplier));

        PlayerController pc = ship.GetComponent<PlayerController>();
        if (pc != null) pc.AddRecoil(RecoilDirection(toShip) * knockback);
    }

    private Vector2 RecoilDirection(Vector2 toShip)
    {
        return toShip.sqrMagnitude > 0.0001f ? toShip.normalized : Vector2.up;
    }

    public void SetVisible(bool visible)
    {
        if (ring != null) ring.gameObject.SetActive(visible);
    }

    private void CreateRing()
    {
        GameObject ringObj = new GameObject("ShockwaveRing");
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
        color = flashing ? ringImpactColor : ringColor;
        width = flashing ? ringTelegraphWidth : ringWidth;
    }

    private void TelegraphColorAndWidth(out Color color, out float width)
    {
        float pulse = (Mathf.Sin(Time.time * telegraphPulseSpeed) + 1f) * 0.5f;
        color = Color.Lerp(ringColor, ringTelegraphColor, pulse);
        width = Mathf.Lerp(ringWidth, ringTelegraphWidth, pulse);
    }

    private void UpdateRingGeometry()
    {
        Vector3 center = transform.position;
        for (int i = 0; i < RingSegments; i++)
        {
            float angle = (i / (float)RingSegments) * Mathf.PI * 2f;
            ring.SetPosition(i, center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
        }
    }
}
