using UnityEngine;

// Sibling component on the Boss GameObject: idle -> telegraph -> active
// vulnerability window -> idle, identical timing in both phases. Halcyon is
// only reliably hittable during the active window (it's otherwise always
// moving) - no damage-taken multiplier is applied. See
// docs/superpowers/specs/2026-09-04-halcyon-boss-design.md.
public class HalcyonSurge : MonoBehaviour
{
    public float cooldown = 8f;
    public float telegraphTime = 1f;
    public float activeTime = 2f;

    public bool IsTelegraphing { get; private set; }
    public bool IsActive { get; private set; }
    public float CooldownRemaining => Mathf.Max(0f, nextSurgeTime - Time.time);

    private float nextSurgeTime;
    private float telegraphEndTime;
    private float activeEndTime;

    void OnEnable()
    {
        nextSurgeTime = Time.time + cooldown;
    }

    void Update()
    {
        if (IsActive) { UpdateActive(); return; }
        if (IsTelegraphing) { UpdateTelegraphing(); return; }
        if (Time.time >= nextSurgeTime) BeginTelegraph();
    }

    private void BeginTelegraph()
    {
        IsTelegraphing = true;
        telegraphEndTime = Time.time + telegraphTime;
    }

    private void UpdateTelegraphing()
    {
        if (Time.time < telegraphEndTime) return;
        IsTelegraphing = false;
        IsActive = true;
        activeEndTime = Time.time + activeTime;
    }

    private void UpdateActive()
    {
        if (Time.time < activeEndTime) return;
        IsActive = false;
        nextSurgeTime = Time.time + cooldown;
    }
}
