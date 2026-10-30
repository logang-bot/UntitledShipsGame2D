using System.Collections;
using UnityEngine;

// Sibling component: Marauder's telegraphed-proximity-pulse-plus-knockback
// mechanic, ported with unchanged numbers/formula. See
// docs/superpowers/specs/2026-09-04-warden-boss-design.md.
public class WardenShockwave : MonoBehaviour
{
    [Header("Shockwave")]
    public float radius = 1.7f;
    public float damageMultiplier = 3f;
    public float knockback = 33f;
    public float cooldown = 3f;
    public float telegraphTime = 0.3f;
    public float impactFlashDuration = 0.15f;

    [Header("Ring visual")]
    // Child on the boss prefab: dim idle zone, alternating warning frames
    // during the telegraph, then the impact flash.
    public DangerZoneVisual visual;

    public float CooldownRemaining => Mathf.Max(0f, nextCheckTime - Time.time);

    private WardenBoss boss;
    private bool isTelegraphing;
    private float impactFlashUntil;
    private float nextCheckTime;

    void Awake()
    {
        boss = GetComponent<WardenBoss>();
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
        if (visual != null) visual.gameObject.SetActive(visible);
    }

    private void UpdateRing()
    {
        if (visual != null) visual.Show(CurrentZoneState(), radius);
    }

    private DangerZoneState CurrentZoneState()
    {
        if (isTelegraphing) return DangerZoneState.Telegraph;
        return Time.time < impactFlashUntil ? DangerZoneState.Impact : DangerZoneState.Idle;
    }
}
