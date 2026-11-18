using UnityEngine;

// The Tank's Taunt on key 1: pulls the boss onto the Tank for tauntSeconds
// (through PlayerAbility.OnTaunt, which PartyBossWiring routes to the boss)
// and shows the magnet for as long. Its cooldown is its own, separate from
// the Super Shield's on E.
public class TankTaunt
{
    private readonly PlayerAbility ability;
    private TauntMagnetVisual magnet;
    private float readyAt;
    private float activeUntil = float.NegativeInfinity;

    public TankTaunt(PlayerAbility ability)
    {
        this.ability = ability;
    }

    public float ActiveRemaining => Mathf.Max(0f, activeUntil - Time.time);
    public float CooldownRemaining => Mathf.Max(0f, readyAt - Time.time);

    public void CreateMagnet()
    {
        if (ability.Fx != null && ability.Fx.HasTauntMagnetArt)
            magnet = TauntMagnetVisual.Create(ability.transform, ability.Fx.tauntMagnetFrames);
    }

    public void TryTaunt()
    {
        if (Time.time < readyAt) return;
        TankAbilitySettings settings = ability.tankPowers;
        readyAt = Time.time + settings.tauntCooldown;
        activeUntil = Time.time + settings.tauntSeconds;
        ability.OnTaunt?.Invoke();
        if (magnet != null) magnet.Play(settings.tauntSeconds);
    }
}
