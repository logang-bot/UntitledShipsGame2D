using System.Collections;
using UnityEngine;

public class MarauderBossShockwave
{
    private const int RingSegments = 32;

    private readonly MarauderBoss boss;
    private LineRenderer ring;
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

    public void ResetCooldown(float until)
    {
        nextCheckTime = until;
    }

    public void SetVisible(bool visible)
    {
        if (ring != null) ring.gameObject.SetActive(visible);
    }

    /// <summary>
    /// World-space ring around the boss showing the shockwave's danger radius
    /// - dim and always visible, pulses brighter during the telegraph
    /// wind-up, then flashes on the frame it actually hits. Built the same
    /// way PlayerAbility.cs's Medic aura ring is (procedural LineRenderer,
    /// Sprites/Default shader, no art asset).
    /// </summary>
    public void CreateRing()
    {
        GameObject ringObj = new GameObject("ShockwaveRing");
        ringObj.transform.SetParent(boss.transform, false);
        ring = ringObj.AddComponent<LineRenderer>();
        ring.useWorldSpace = true;
        ring.loop = true;
        ring.positionCount = RingSegments;
        ring.material = new Material(Shader.Find("Sprites/Default"));
        ring.sortingLayerName = "Default";
        ring.sortingOrder = -1; // behind the boss sprite
    }

    public void UpdateRing()
    {
        if (ring == null) return;

        SelectRingColorAndWidth(out Color color, out float width);
        ring.startColor = color;
        ring.endColor = color;
        ring.startWidth = width;
        ring.endWidth = width;

        UpdateRingGeometry(boss.transform.position);
    }

    private void SelectRingColorAndWidth(out Color color, out float width)
    {
        if (isTelegraphing)
        {
            float pulse = (Mathf.Sin(Time.time * boss.shockwaveTelegraphPulseSpeed) + 1f) * 0.5f;
            color = Color.Lerp(boss.shockwaveRingColor, boss.shockwaveRingTelegraphColor, pulse);
            width = Mathf.Lerp(boss.shockwaveRingWidth, boss.shockwaveRingTelegraphWidth, pulse);
        }
        else if (Time.time < impactFlashUntil)
        {
            color = boss.shockwaveRingImpactColor;
            width = boss.shockwaveRingTelegraphWidth;
        }
        else
        {
            color = boss.shockwaveRingColor;
            width = boss.shockwaveRingWidth;
        }
    }

    private void UpdateRingGeometry(Vector3 center)
    {
        for (int i = 0; i < RingSegments; i++)
        {
            float angle = (i / (float)RingSegments) * Mathf.PI * 2f;
            ring.SetPosition(i, center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * boss.shockwaveRadius);
        }
    }

    public void CheckShockwave()
    {
        if (Time.time < nextCheckTime) return;

        foreach (GameObject t in boss.targets)
        {
            if (t == null || !t.activeInHierarchy) continue;
            if (Vector2.Distance(t.transform.position, boss.transform.position) <= boss.shockwaveRadius)
            {
                nextCheckTime = Time.time + boss.shockwaveCooldown;
                boss.StartCoroutine(ShockwaveRoutine());
                return;
            }
        }
    }

    private IEnumerator ShockwaveRoutine()
    {
        isTelegraphing = true;
        yield return new WaitForSeconds(boss.shockwaveTelegraphTime);
        isTelegraphing = false;
        impactFlashUntil = Time.time + boss.shockwaveImpactFlashDuration;

        foreach (GameObject t in boss.targets)
        {
            if (t == null || !t.activeInHierarchy) continue;
            ApplyShockwaveEffect(t);
        }
    }

    private void ApplyShockwaveEffect(GameObject target)
    {
        Vector2 toTarget = (Vector2)target.transform.position - (Vector2)boss.transform.position;
        if (toTarget.magnitude > boss.shockwaveRadius) return;

        PlayerHealth health = target.GetComponent<PlayerHealth>();
        if (health != null) health.TakeDamage(Mathf.RoundToInt(boss.bulletDamage * boss.shockwaveDamageMultiplier));

        PlayerController pc = target.GetComponent<PlayerController>();
        if (pc != null)
        {
            Vector2 pushDir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.up;
            pc.AddRecoil(pushDir * boss.shockwaveKnockback);
        }
    }
}
