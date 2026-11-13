using System.Collections;
using UnityEngine;

// The Attacker's Nova Beam cast: invulnerable and locked in place from the
// E press, the beam after the wind-up, unlocked when the beam ends. Elapsed
// is the clock the HUD's cut-in card plays from.
public class AttackerBeamCast
{
    private readonly PlayerAbility ability;
    private Coroutine routine;
    private float startTime;

    public AttackerBeamCast(PlayerAbility ability)
    {
        this.ability = ability;
    }

    public bool IsCasting => routine != null;
    public float Elapsed => IsCasting ? Time.time - startTime : -1f;

    public void Begin()
    {
        if (IsCasting) return;
        startTime = Time.time;
        routine = ability.StartCoroutine(CastRoutine(ability.beam));
    }

    private IEnumerator CastRoutine(AttackerBeamSettings beam)
    {
        ability.PlayerController.castLocked = true;
        ability.Health.GrantInvulnerability(beam.TotalSeconds);
        yield return new WaitForSeconds(beam.windupSeconds);
        if (ability.Fx != null) ability.Fx.SpawnBeam(ShotFor(beam));
        yield return new WaitForSeconds(beam.beamSeconds);
        ability.PlayerController.castLocked = false;
        routine = null;
    }

    private AttackerBeamShot ShotFor(AttackerBeamSettings beam)
    {
        PlayerController controller = ability.PlayerController;
        return new AttackerBeamShot
        {
            source = ability.gameObject,
            muzzle = controller.firePoint,
            width = controller.ShipWidth * beam.widthOfShip,
            seconds = beam.beamSeconds,
            tickInterval = beam.tickInterval,
            damagePerTick = Mathf.Max(1f, controller.fireDamage * beam.tickDamageMultiplier),
        };
    }
}
