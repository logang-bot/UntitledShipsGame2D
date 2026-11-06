using UnityEngine;

// How far a ship's flames are into BOOST (0 = cruise, 1 = full boost),
// easing toward a target: quick to flare up, slower to die down, as in the
// design's thrusters(). Plain class, unit-tested (ThrustBoostTests).
public class ThrustBoost
{
    private const float RiseRate = 7f;
    private const float FallRate = 3.5f;
    private const float MaxStepSeconds = 0.1f;

    public float Current { get; private set; }

    /// <summary>
    /// Frame-rate independent exponential approach: closes 1 − e^(−rate·dt)
    /// of the gap, with dt capped at 0.1 s so a hitch can't jump the flame.
    /// </summary>
    public void Step(float target, float deltaSeconds)
    {
        float rate = target > Current ? RiseRate : FallRate;
        float dt = Mathf.Min(MaxStepSeconds, deltaSeconds);
        Current += (target - Current) * (1f - Mathf.Exp(-rate * dt));
    }
}
