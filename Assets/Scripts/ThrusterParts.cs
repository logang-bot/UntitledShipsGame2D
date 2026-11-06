using UnityEngine;

// The named nodes of one engine in a ship GLB, as the design's engine()
// builds them: "<name>_plume" (pivot at the nozzle, local +Y = flame axis)
// holding "<name>_plume_outer" and "<name>_plume_core", next to the glowing
// "<name>_glow" disc.
public struct ThrusterParts
{
    private const string PlumeSuffix = "_plume";

    public Transform pivot;
    public Transform outer;
    public Transform core;
    public Renderer disc;

    public static bool IsPlume(Transform node) => node.name.EndsWith(PlumeSuffix);

    public static bool TryFind(Transform plume, out ThrusterParts parts)
    {
        parts = new ThrusterParts
        {
            pivot = plume, outer = plume.Find(plume.name + "_outer"), core = plume.Find(plume.name + "_core"), disc = FindDisc(plume),
        };
        return parts.outer != null && parts.core != null;
    }

    private static Renderer FindDisc(Transform plume)
    {
        string engineName = plume.name.Substring(0, plume.name.Length - PlumeSuffix.Length);
        Transform disc = plume.parent != null ? plume.parent.Find(engineName + "_glow") : null;
        return disc != null ? disc.GetComponent<Renderer>() : null;
    }
}
