using UnityEngine;

// Where one Medic heal pulse lands: the passive aura around the Medic, or
// the Sanctuary dome, which also heals the Medic itself.
public struct HealArea
{
    public Vector2 centre;
    public float radius;
    public bool includesCaster;

    public HealArea(Vector2 centre, float radius, bool includesCaster)
    {
        this.centre = centre;
        this.radius = radius;
        this.includesCaster = includesCaster;
    }
}
