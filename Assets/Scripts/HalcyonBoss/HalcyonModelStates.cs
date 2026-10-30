using UnityEngine;

// Shows Halcyon's phase and Static Field on its model (Claude Design "Boss
// State Wiring"): phase 2 adds the outer ring, scorch marks, core halo and
// wingtip arcs, and spins the coil ring 2.2x faster; arcs jump coil to coil
// while the Static Field winds up, unless Halcyon is exposed. The ring slows during the Opening's
// telegraph and stops while Halcyon is exposed. The field's radius stays
// the StaticFieldRing's job. Lives on the boss root beside HalcyonBoss and
// only reads it; HalcyonModelOpening shows the Opening itself.
public class HalcyonModelStates : MonoBehaviour
{
    private const float RingDegreesPerSecond = 28.65f;

    public BossModel model;

    private HalcyonBoss boss;
    private HalcyonSurge surge;
    private HalcyonStaticField staticField;
    private bool? shownPhase2;
    private float ringAngle;

    void Awake()
    {
        boss = GetComponent<HalcyonBoss>();
        surge = GetComponent<HalcyonSurge>();
        staticField = GetComponent<HalcyonStaticField>();
    }

    void LateUpdate()
    {
        if (shownPhase2 != boss.IsPhase2) ShowPhase(boss.IsPhase2);
        model.Show("halcyon_static_arcs", staticField.IsTelegraphing && !surge.IsActive);
        SpinRing();
    }

    private void ShowPhase(bool phase2)
    {
        shownPhase2 = phase2;
        model.Show("halcyon_phase2_overcharge", phase2);
    }

    private void SpinRing()
    {
        if (surge.IsActive) return;
        float speed = (boss.IsPhase2 ? 2.2f : 1f) * (surge.IsTelegraphing ? 0.4f : 1f);
        ringAngle += RingDegreesPerSecond * speed * Time.deltaTime;
        model.Node("halcyon_coil_ring").localRotation = Quaternion.Euler(0f, ringAngle, 0f);
    }
}
