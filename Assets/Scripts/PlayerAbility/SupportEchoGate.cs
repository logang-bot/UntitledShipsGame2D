using System.Collections;
using UnityEngine;

// The Support's Echo Gate cast: on the Q press a gate is dropped above the
// Support for timing.seconds, tethered to the ship, then it collapses. The
// Support keeps moving; if it dies, the gate shatters at once. Elapsed is
// the clock the HUD's cut-in card plays from.
public class SupportEchoGate
{
    private readonly PlayerAbility ability;
    private readonly EchoGateSfx sfx;
    private readonly EchoGateHoldVisual hold;
    private Coroutine routine;
    private EchoGateZone gate;
    private float startTime;
    private ZoneFrame? shownFrame;

    public SupportEchoGate(PlayerAbility ability)
    {
        this.ability = ability;
        sfx = ability.GetComponent<EchoGateSfx>();
        hold = new EchoGateHoldVisual(ability);
    }

    public bool IsActive => gate != null;
    public float Elapsed => IsActive ? Time.time - startTime : -1f;
    public float Remaining => IsActive ? Mathf.Max(0f, Timing.seconds - Elapsed) : 0f;

    private ZonePhaseTiming Timing => ability.supportPowers.timing;

    public void Begin()
    {
        if (IsActive) return;
        startTime = Time.time;
        gate = EchoGateZone.Spawn(ability);
        hold.Show();
        ability.Health.OnDeath.AddListener(Shatter);
        routine = ability.StartCoroutine(CastRoutine());
    }

    private IEnumerator CastRoutine()
    {
        for (float t = 0f; t < Timing.seconds; t = Time.time - startTime)
        {
            ShowMoment(t);
            yield return null;
        }
        routine = null;
        gate.Close();
        Release();
    }

    private void ShowMoment(float elapsed)
    {
        ZoneFrame frame = ZonePowerTimeline.FrameAt(Timing, elapsed);
        gate.Show(frame, ZonePowerTimeline.ScaleAt(Timing, elapsed));
        hold.Follow(gate.transform.position);
        if (frame == shownFrame) return;
        shownFrame = frame;
        if (sfx != null) sfx.OnFrame(frame);
    }

    private void Shatter()
    {
        gate.Shatter();
        Release();
    }

    private void Release()
    {
        ability.Health.OnDeath.RemoveListener(Shatter);
        if (routine != null) ability.StopCoroutine(routine);
        routine = null;
        shownFrame = null;
        if (sfx != null) sfx.OnEnd();
        hold.Hide();
        gate = null;
    }
}
