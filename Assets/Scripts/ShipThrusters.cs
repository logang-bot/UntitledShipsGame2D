using System.Collections.Generic;
using UnityEngine;

// Animates a ship model's engine flames (on each ship prefab in
// Assets/Art/Ships/Prefabs): a flicker at cruise, eased into the design's
// BOOST look while BoostTarget is 1. ShipBoostFeed sets the target in a
// level; in the menus the ships just cruise. Holds still while the game is
// paused or frozen.
public class ShipThrusters : MonoBehaviour
{
    private readonly ThrustBoost boost = new ThrustBoost();
    private List<ThrusterEngine> engines = new List<ThrusterEngine>();

    public float BoostTarget { get; set; }

    void Awake() => engines = ThrusterEngine.FindAll(transform);

    void Update()
    {
        if (Time.deltaTime <= 0f) return;
        boost.Step(BoostTarget, Time.deltaTime);
        foreach (ThrusterEngine engine in engines) engine.Apply(PoseFor(engine.Seed));
    }

    private ThrusterPose PoseFor(float seed)
    {
        float noise = Random.Range(-1f, 1f) * ThrusterPose.NoiseAmplitude(boost.Current);
        return ThrusterPose.From(ThrusterPose.Flicker(Time.time, seed, boost.Current) + noise, boost.Current);
    }
}
