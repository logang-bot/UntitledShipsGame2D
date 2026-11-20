using UnityEngine;

// The Medic's two healing tools: the always-on aura (tiny: allies must
// nearly touch the Medic) and the Sanctuary superpower on Q. Both live here
// rather than on AIController, so they work the same for a human or CPU
// Medic.
public class PlayerAbilityMedic
{
    private const int AuraSortingOrder = -5;
    private const float IdleRingRadiusPx = 80f;

    private readonly PlayerAbility ability;
    private SpriteRenderer ring;
    private float nextTickTime;

    public PlayerAbilityMedic(PlayerAbility ability)
    {
        this.ability = ability;
        Sanctuary = new MedicSanctuary(ability);
    }

    public MedicSanctuary Sanctuary { get; }

    public void Trigger() => Sanctuary.Begin();

    public void CheckTick()
    {
        if (Time.time < nextTickTime) return;
        nextTickTime = Time.time + ability.auraTickInterval;
        bool healed = MedicHeal.Pulse(ability, new HealArea(ability.transform.position, ability.auraRadius, false));
        if (healed && ability.TryGetComponent(out MedicSfx sfx)) sfx.PlayAuraTick();
    }

    /// <summary>
    /// The dim dashed Combat FX ring around the Medic, scaled so the drawn
    /// ring sits exactly on the aura's heal radius.
    /// </summary>
    public void CreateAuraRing()
    {
        if (ability.Fx == null) return;
        ring = FxSprites.CreateChild(ability.transform, "AuraRing", AuraSortingOrder);
        ring.sprite = ability.Fx.auraIdle;
        FxSprites.FitRingRadius(ring.transform, IdleRingRadiusPx, ability.auraRadius);
    }
}
