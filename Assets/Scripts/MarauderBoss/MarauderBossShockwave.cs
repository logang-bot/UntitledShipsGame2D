using System.Collections;
using UnityEngine;

public class MarauderBossShockwave
{
    private readonly MarauderBoss boss;
    private bool isTelegraphing;
    private float impactFlashUntil;
    private float nextCheckTime;

    public MarauderBossShockwave(MarauderBoss boss)
    {
        this.boss = boss;
    }

    /// <summary>
    /// Proximity-triggered, not a fixed auto-cast - reads "Ready" whenever no
    /// ship has gotten close enough to trigger it yet, not just after cooldown elapses.
    /// </summary>
    public float CooldownRemaining => Mathf.Max(0f, nextCheckTime - Time.time);

    public bool IsTelegraphing => isTelegraphing;

    public void ResetCooldown(float until)
    {
        nextCheckTime = until;
    }

    public void SetVisible(bool visible)
    {
        if (boss.shockwaveSettings.visual != null) boss.shockwaveSettings.visual.gameObject.SetActive(visible);
    }

    /// <summary>
    /// Danger-zone ring around the boss at the shockwave's radius (a
    /// DangerZoneVisual child wired on the prefab): dim and always visible,
    /// alternating warning frames during the telegraph wind-up, then the
    /// impact flash on the frame it hits.
    /// </summary>
    public void UpdateRing()
    {
        DangerZoneVisual visual = boss.shockwaveSettings.visual;
        if (visual != null) visual.Show(CurrentZoneState(), boss.shockwaveSettings.radius);
    }

    private DangerZoneState CurrentZoneState()
    {
        if (isTelegraphing) return DangerZoneState.Telegraph;
        return Time.time < impactFlashUntil ? DangerZoneState.Impact : DangerZoneState.Idle;
    }

    public void CheckShockwave()
    {
        if (Time.time < nextCheckTime) return;

        foreach (GameObject t in boss.targets)
        {
            if (t == null || !t.activeInHierarchy) continue;
            if (Vector2.Distance(t.transform.position, boss.transform.position) <= boss.shockwaveSettings.radius)
            {
                nextCheckTime = Time.time + boss.shockwaveSettings.cooldown;
                boss.StartCoroutine(ShockwaveRoutine());
                return;
            }
        }
    }

    private IEnumerator ShockwaveRoutine()
    {
        isTelegraphing = true;
        yield return new WaitForSeconds(boss.shockwaveSettings.telegraphTime);
        isTelegraphing = false;
        impactFlashUntil = Time.time + boss.shockwaveSettings.impactFlashDuration;

        foreach (GameObject t in boss.targets)
        {
            if (t == null || !t.activeInHierarchy) continue;
            ApplyShockwaveEffect(t);
        }
    }

    private void ApplyShockwaveEffect(GameObject target)
    {
        Vector2 toTarget = (Vector2)target.transform.position - (Vector2)boss.transform.position;
        if (toTarget.magnitude > boss.shockwaveSettings.radius) return;

        PlayerHealth health = target.GetComponent<PlayerHealth>();
        if (health != null) health.TakeDamage(Mathf.RoundToInt(boss.bulletDamage * boss.shockwaveSettings.damageMultiplier));

        PlayerController pc = target.GetComponent<PlayerController>();
        if (pc != null)
        {
            Vector2 pushDir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.up;
            pc.AddRecoil(pushDir * boss.shockwaveSettings.knockback);
        }
    }
}
