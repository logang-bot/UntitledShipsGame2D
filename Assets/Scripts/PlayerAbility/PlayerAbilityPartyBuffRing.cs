using UnityEngine;

/// <summary>
/// Generic, initially-hidden ring on every ship (not role-gated) that
/// Support's party-wide speed boost toggles on/off, tinted to match
/// whichever role cast it. Local-space and built once - a fixed-radius
/// circle has nothing to recompute per frame.
/// </summary>
public class PlayerAbilityPartyBuffRing
{
    private const int Segments = 24;

    private readonly PlayerAbility ability;
    private LineRenderer ring;

    public PlayerAbilityPartyBuffRing(PlayerAbility ability)
    {
        this.ability = ability;
    }

    public void Create()
    {
        GameObject ringObj = new GameObject("PartyBuffRing");
        ringObj.transform.SetParent(ability.transform, false);
        ring = ringObj.AddComponent<LineRenderer>();
        ConfigureRenderer();
        PlaceCirclePoints();
        ringObj.SetActive(false);
    }

    public void Show(bool active, Color color)
    {
        if (ring == null) return;
        if (active)
        {
            ring.startColor = color;
            ring.endColor = color;
        }
        ring.gameObject.SetActive(active);
    }

    private void ConfigureRenderer()
    {
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = Segments;
        ring.material = new Material(Shader.Find("Sprites/Default"));
        ring.sortingLayerName = "Default";
        ring.sortingOrder = -1;
        ring.startWidth = ability.partyBuffRingWidth;
        ring.endWidth = ability.partyBuffRingWidth;
    }

    private void PlaceCirclePoints()
    {
        for (int i = 0; i < Segments; i++)
        {
            float angle = (i / (float)Segments) * Mathf.PI * 2f;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * ability.partyBuffRingRadius);
        }
    }
}
