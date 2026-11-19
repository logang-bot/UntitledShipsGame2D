using System.Collections;
using UnityEngine;

// The Tank's Super Shield cast: on the E press the shield refills and the
// arc grows to superShieldScale, its hits free, for superShieldSeconds, then
// it shrinks back. The Tank keeps moving and can still be hit on its body.
// Elapsed is the clock the HUD's cut-in card plays from.
public class TankSuperShield
{
    private readonly PlayerAbility ability;
    private readonly TankShieldArc arc;
    private readonly SuperShieldSfx sfx;
    private Coroutine routine;
    private float startTime;
    private SuperShieldFrame? shownFrame;

    public TankSuperShield(PlayerAbility ability, TankShieldArc arc)
    {
        this.ability = ability;
        this.arc = arc;
        sfx = ability.GetComponent<SuperShieldSfx>();
    }

    public bool IsActive => routine != null;
    public float Elapsed => IsActive ? Time.time - startTime : -1f;
    public float Remaining => IsActive ? Mathf.Max(0f, ability.tankPowers.superShieldSeconds - Elapsed) : 0f;

    public void Begin()
    {
        if (IsActive) return;
        startTime = Time.time;
        ability.Health.RestoreShield(ability.Health.maxShield);
        routine = ability.StartCoroutine(CastRoutine(ability.tankPowers));
    }

    private IEnumerator CastRoutine(TankAbilitySettings settings)
    {
        arc.FreeBlock = true;
        for (float t = 0f; t < settings.superShieldSeconds; t = Time.time - startTime)
        {
            ShowMoment(settings, t);
            yield return null;
        }
        End();
    }

    private void ShowMoment(TankAbilitySettings settings, float elapsed)
    {
        SuperShieldFrame frame = SuperShieldTimeline.FrameAt(settings, elapsed);
        arc.SetScale(SuperShieldTimeline.ScaleAt(settings, elapsed));
        arc.ShowSuperFrame(frame);
        CueSoundOnFrameChange(frame);
    }

    private void CueSoundOnFrameChange(SuperShieldFrame frame)
    {
        if (frame == shownFrame) return;
        shownFrame = frame;
        if (sfx != null) sfx.OnFrame(frame);
    }

    private void End()
    {
        shownFrame = null;
        if (sfx != null) sfx.OnEnd();
        arc.FreeBlock = false;
        arc.SetScale(1f);
        arc.ShowNormalLook();
        routine = null;
    }
}
