using System.Collections;
using UnityEngine;

// The Medic's Sanctuary cast: on the Q press a dome is left where the Medic
// stands for timing.seconds, then it collapses. The Medic keeps moving and
// can still be hit; if it dies, the dome goes with it. Elapsed is the clock
// the HUD's cut-in card plays from.
public class MedicSanctuary
{
    private readonly PlayerAbility ability;
    private readonly MedicSfx sfx;
    private Coroutine routine;
    private SanctuaryZone zone;
    private float startTime;
    private ZoneFrame? shownFrame;

    public MedicSanctuary(PlayerAbility ability)
    {
        this.ability = ability;
        sfx = ability.GetComponent<MedicSfx>();
    }

    public bool IsActive => zone != null;
    public float Elapsed => IsActive ? Time.time - startTime : -1f;
    public float Remaining => IsActive ? Mathf.Max(0f, Timing.seconds - Elapsed) : 0f;

    private ZonePhaseTiming Timing => ability.medicPowers.timing;

    public void Begin()
    {
        if (IsActive) return;
        startTime = Time.time;
        zone = SanctuaryZone.Spawn(ability);
        ability.Health.OnDeath.AddListener(End);
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
        End();
    }

    private void ShowMoment(float elapsed)
    {
        ZoneFrame frame = ZonePowerTimeline.FrameAt(Timing, elapsed);
        zone.Show(frame, ZonePowerTimeline.ScaleAt(Timing, elapsed));
        if (frame == shownFrame) return;
        shownFrame = frame;
        if (sfx != null) sfx.OnFrame(frame);
    }

    private void End()
    {
        ability.Health.OnDeath.RemoveListener(End);
        StopRoutine();
        shownFrame = null;
        if (sfx != null) sfx.OnEnd();
        zone.Close();
        zone = null;
    }

    private void StopRoutine()
    {
        if (routine != null) ability.StopCoroutine(routine);
        routine = null;
    }
}
