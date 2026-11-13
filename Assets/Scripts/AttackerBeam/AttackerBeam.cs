using UnityEngine;

// The Attacker's Nova Beam once it fires: a column from the ship's muzzle to
// the top of the screen that strikes everything inside it every tick, then
// removes itself. Spawned by ShipCombatFx.SpawnBeam from AttackerBeamCast.
public class AttackerBeam : MonoBehaviour
{
    private const float RampInSeconds = 0.12f;
    private const float FadeOutSeconds = 0.25f;
    private const float TopOvershoot = 0.3f;

    public AttackerBeamVisual visual;
    public BeamLightning lightning;
    public ParticleSystem impactSparks;

    private readonly BeamColumnDamage damage = new BeamColumnDamage();
    private AttackerBeamShot shot;
    private Camera cam;
    private float elapsed;
    private float nextTick;
    private float length;

    public void Fire(AttackerBeamShot beamShot)
    {
        shot = beamShot;
        cam = Camera.main;
        FollowMuzzle();
        if (cam.TryGetComponent(out CameraShake shake)) shake.Shake();
    }

    void LateUpdate()
    {
        if (shot.muzzle == null || elapsed >= shot.seconds) { Destroy(gameObject); return; }
        elapsed += Time.deltaTime;
        FollowMuzzle();
        visual.SetIntensity(IntensityAt(elapsed));
        if (elapsed >= nextTick) Tick();
    }

    private void FollowMuzzle()
    {
        Vector2 muzzle = shot.muzzle.position;
        float top = cam.ViewportToWorldPoint(Vector3.one).y + TopOvershoot;
        length = Mathf.Max(0.1f, top - muzzle.y);
        transform.position = new Vector3(muzzle.x, muzzle.y, 0f);
        visual.Fit(shot.width, length);
        lightning.Fit(shot.width, length);
    }

    private void Tick()
    {
        nextTick += shot.tickInterval;
        Rect column = new Rect(transform.position.x - shot.width * 0.5f, transform.position.y, shot.width, length);
        foreach (Vector2 impact in damage.Strike(column, shot.damagePerTick, shot.source)) EmitSparks(impact);
    }

    private void EmitSparks(Vector2 impact)
    {
        if (impactSparks == null) return;
        impactSparks.Emit(new ParticleSystem.EmitParams { position = impact, applyShapeToPosition = true }, 4);
    }

    /// <summary>Ramps up over RampInSeconds and back down over the last FadeOutSeconds of the beam.</summary>
    private float IntensityAt(float t)
    {
        float rampIn = Mathf.Clamp01(t / RampInSeconds);
        float fadeOut = Mathf.Clamp01((shot.seconds - t) / FadeOutSeconds);
        return Mathf.Min(rampIn, fadeOut);
    }
}
