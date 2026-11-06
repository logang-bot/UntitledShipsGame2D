using System.Collections.Generic;
using UnityEngine;

// One animated engine of a ship model: applies a ThrusterPose to its plume
// pivot (absolute scale, so whatever scale the GLB was exported at doesn't
// matter), its white core, and the emission of the outer cone, the core and
// the nozzle disc.
public class ThrusterEngine
{
    private const float SeedSpacing = 1.7f;

    private readonly ThrusterParts parts;
    private readonly EmissionDriver outerGlow;
    private readonly EmissionDriver coreGlow;
    private readonly EmissionDriver discGlow;

    public float Seed { get; }

    private ThrusterEngine(ThrusterParts parts, int index)
    {
        this.parts = parts;
        Seed = index * SeedSpacing;
        outerGlow = new EmissionDriver(parts.outer.GetComponent<Renderer>());
        coreGlow = new EmissionDriver(parts.core.GetComponent<Renderer>());
        discGlow = parts.disc != null ? new EmissionDriver(parts.disc) : null;
    }

    /// <summary>Every engine under root whose GLB nodes carry the plume parts; engines without them are left static.</summary>
    public static List<ThrusterEngine> FindAll(Transform root)
    {
        var engines = new List<ThrusterEngine>();
        foreach (Transform node in root.GetComponentsInChildren<Transform>())
            if (ThrusterParts.IsPlume(node) && ThrusterParts.TryFind(node, out ThrusterParts parts)) engines.Add(new ThrusterEngine(parts, engines.Count));
        return engines;
    }

    public void Apply(ThrusterPose pose)
    {
        parts.pivot.localScale = new Vector3(pose.width, pose.length, pose.width);
        parts.core.localScale = new Vector3(1f, pose.coreLength, 1f);
        outerGlow.Scale(pose.outerOpacity);
        coreGlow.Scale(pose.coreOpacity);
        discGlow?.Scale(pose.glowFactor);
    }
}
